using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.SceneManagement;
using Surlhwa.Domain;

namespace Surlhwa.Presentation
{
    // Opt-in evidence only. No game commands, settings changes, screenshots or per-frame file I/O.
    [DefaultExecutionOrder(1200)]
    public sealed class CampaignPerformanceProbe : MonoBehaviour
    {
        public static bool Requested => Argument("-campaignPerformanceRoot") != null;
        public static string Scenario { get; set; } = "unlabelled";
        private static CampaignPerformanceProbe active;
        private static readonly string[] PhaseNames = Enum.GetNames(typeof(CampaignPhase));
        private static readonly string[] MethodNames = { "stir", "build", "shake", "float" };
        private static readonly FieldInfo MethodPanels = typeof(CampaignUi).GetField("methodPanels", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly string[] Arguments = Environment.GetCommandLineArgs();
        private const int CaptureGuardFrames = 8;
        private const double WarmupSeconds = 5;
        private CampaignController owner;
        private CampaignUi observedUi;
        private CampaignCheckpoint observedState;
        private string observedText = "";
        private List<GameObject> methodPanels;
        private Frame[] samples;
        private int count, dropped, stable;
        private Frame previous;
        private long started, lastTick;
        private bool finished, timingEnabled;
        private string output;
        private Metadata metadata;
        private readonly List<Interval> captures = new List<Interval>(128);
        private readonly Stack<Interval> openCaptures = new Stack<Interval>();
        private readonly FrameTiming[] timing = new FrameTiming[1];
        private readonly Counter[] counters = {
            new Counter("Main Thread", "TimeNanoseconds"), new Counter("GC Allocated In Frame", "Bytes"),
            new Counter("Total Used Memory", "Bytes"), new Counter("System Used Memory", "Bytes"),
            new Counter("Gfx Used Memory", "Bytes") };
        private Process process;
        private double nextWorkingSet, workingSetAt = -1;
        private long workingSet = -1;

        private static string Argument(string name)
        {
            int i = Array.IndexOf(Arguments, name);
            return i >= 0 && i + 1 < Arguments.Length ? Arguments[i + 1] : null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            active = null; Scenario = "unlabelled";
            SceneManager.sceneLoaded -= Attach;
            if (Requested) SceneManager.sceneLoaded += Attach;
        }
        private static void Attach(Scene scene, LoadSceneMode mode)
        {
            if (!Requested || active != null) return;
            var controller = FindFirstObjectByType<CampaignController>();
            if (controller != null && controller.GetComponent<CampaignPerformanceProbe>() == null)
                controller.gameObject.AddComponent<CampaignPerformanceProbe>();
        }

        [Serializable] private sealed class CounterInfo
        {
            public string name, category, unit, reason;
            public bool available;
        }
        private sealed class Counter
        {
            public readonly CounterInfo Info;
            private readonly string unit;
            private ProfilerRecorder recorder;
            public Counter(string name, string expectedUnit) { Info = new CounterInfo { name = name, reason = "counter not advertised by Player" }; unit = expectedUnit; }
            public void Start(List<ProfilerRecorderHandle> handles)
            {
                foreach (var handle in handles) {
                    var description = ProfilerRecorderHandle.GetDescription(handle);
                    if (description.Name != Info.name) continue;
                    Info.category = description.Category.Name; Info.unit = description.UnitType.ToString();
                    if (Info.unit != unit) { Info.reason = "unexpected counter unit"; return; }
                    try {
                        recorder = new ProfilerRecorder(handle, 1, ProfilerRecorderOptions.Default | ProfilerRecorderOptions.StartImmediately);
                        Info.available = recorder.Valid; Info.reason = recorder.Valid ? "" : "recorder unavailable";
                    } catch (Exception e) { Info.reason = e.GetType().Name; }
                    return;
                }
            }
            public double Read() => Info.available && recorder.Valid && recorder.Count > 0 ? recorder.LastValue : -1;
            public void Dispose() { if (recorder.Valid) recorder.Dispose(); }
        }
        [Serializable] private sealed class Metadata
        {
            public int schemaVersion = 1, captureGuardFrames = CaptureGuardFrames, capacity, samples, droppedSamples;
            public int width, height, vSyncCount, targetFrameRate, cpuCores, systemMemoryMb, gpuMemoryMb, processId;
            public string startedUtc, finishedUtc, deviceName, operatingSystem, cpu, gpuName, graphicsApi, unityVersion, buildGuid, scene;
            public string inputTier, renderPath = "normal camera", workingSetError, timingError, outputError;
            public string gpuAvailability = "not yet observed", driverSource = "performance-host.json; graphicsApi is not an OS driver version";
            public string intervalClock = "Stopwatch Update-to-Update, previous frame/context; wall interval includes cap/vsync/wait and probe overhead";
            public string timingAttribution = "FrameTimingManager delayed up to four frames; unique timestamp, workload attribution only after eight stable frames";
            public string overhead = "fixed sample buffer; ProfilerRecorder and FrameTimingManager; process working set sampled at 1 Hz; serialization only at startup/Finish";
            public string capturePolicy = "raw intervals retained; exclude startFrame-1 through endFrame+8; unfinished intervals excluded through end";
            public bool finalized, developmentBuild, batchMode, continuousCapture, evidenceProbeEnabledAtStart, frameTimingFeatureEnabled;
            public long managedInitializationBytes, sampleBufferManagedBytes, rawTelemetryBytes, captureTelemetryBytes, stopwatchFrequency;
            public int observedGpuSamples;
            public double warmupSeconds = WarmupSeconds, initializationMs, durationSeconds, serializationMs;
            public CounterInfo[] counters;
        }
        [Serializable] private struct Frame
        {
            public int frame, slot, line, memories, services, departures, contextStableFrames, width, height, vSyncCount, targetFrameRate;
            public string scenario, phase, modal, method, tool, textId;
            public bool paired, focused, saving, warmup, largeText, highContrast;
            public double timeSeconds, frameMs, unityDeltaMs, mainThreadMs, gcAllocatedBytes, totalUsedBytes, systemUsedBytes, gfxUsedBytes;
            public double workingSetBytes, workingSetAgeSeconds, probeMs, cpuFrameMs, timingMainMs, presentWaitMs, gpuMs;
            public ulong timingStamp;
        }
        [Serializable] private sealed class Interval
        {
            public string kind = "capture", label;
            public int startFrame, endFrame = -1;
            public double startSeconds, endSeconds = -1;
            public bool finished;
        }
        [Serializable] private sealed class IntervalFile { public List<Interval> intervals; }

        private void Awake()
        {
            if (!Requested || active != null) { enabled = false; return; }
            active = this; owner = GetComponent<CampaignController>(); started = Stopwatch.GetTimestamp();
            long initialMemory = GC.GetTotalMemory(false);
            try {
                output = Path.GetFullPath(Argument("-campaignPerformanceRoot")); Directory.CreateDirectory(output);
                const int capacity = 216000; // One hour at 60 fps; exhaustion remains explicit rather than overwriting history.
                long bufferBefore = GC.GetTotalMemory(false); samples = new Frame[capacity];
                long bufferBytes = Math.Max(0, GC.GetTotalMemory(false) - bufferBefore);
                metadata = new Metadata {
                    capacity = capacity, sampleBufferManagedBytes = bufferBytes, startedUtc = DateTime.UtcNow.ToString("o"), deviceName = SystemInfo.deviceName,
                    operatingSystem = SystemInfo.operatingSystem, cpu = SystemInfo.processorType, cpuCores = SystemInfo.processorCount,
                    systemMemoryMb = SystemInfo.systemMemorySize, gpuName = SystemInfo.graphicsDeviceName, gpuMemoryMb = SystemInfo.graphicsMemorySize,
                    graphicsApi = SystemInfo.graphicsDeviceVersion, unityVersion = UnityEngine.Application.unityVersion,
                    buildGuid = UnityEngine.Application.buildGUID, scene = gameObject.scene.name, developmentBuild = UnityEngine.Debug.isDebugBuild, batchMode = UnityEngine.Application.isBatchMode,
                    width = Screen.width, height = Screen.height, vSyncCount = QualitySettings.vSyncCount, targetFrameRate = UnityEngine.Application.targetFrameRate,
                    inputTier = Argument("-campaignProof") != null ? "simulated production InputSystem" : "unclassified external/human input",
                    continuousCapture = Argument("-campaignMedia") == "1" || Argument("-campaignDetailedMedia") == "1",
                    evidenceProbeEnabledAtStart = GetComponent<CampaignEvidenceProbe>()?.enabled == true,
                    stopwatchFrequency = Stopwatch.Frequency, counters = new CounterInfo[counters.Length]
                };
                var handles = new List<ProfilerRecorderHandle>(); ProfilerRecorderHandle.GetAvailable(handles);
                for (int i = 0; i < counters.Length; i++) { counters[i].Start(handles); metadata.counters[i] = counters[i].Info; }
                timingEnabled = metadata.frameTimingFeatureEnabled = FrameTimingManager.IsFeatureEnabled();
                if (!timingEnabled) metadata.gpuAvailability = "FrameTimingManager feature disabled or unsupported; no settings changed";
                try { process = Process.GetCurrentProcess(); metadata.processId = process.Id; }
                catch (Exception e) { metadata.workingSetError = e.GetType().Name; }
                metadata.managedInitializationBytes = Math.Max(0, GC.GetTotalMemory(false) - initialMemory);
                metadata.initializationMs = SecondsSince(started) * 1000;
                WriteMetadata(); // A crash leaves explicit finalized=false rather than a successful summary.
                lastTick = 0; // Prime at the first Update; Awake may run in that same frame.
            } catch (Exception e) {
                UnityEngine.Debug.LogWarning("Campaign performance probe unavailable: " + e.GetType().Name + ": " + e.Message);
                finished = true;
            }
        }
        private static double SecondsSince(long tick) => (Stopwatch.GetTimestamp() - tick) / (double)Stopwatch.Frequency;
        private Frame Snapshot()
        {
            if (observedUi != owner.Ui) {
                observedUi = owner.Ui;
                methodPanels = observedUi != null ? MethodPanels?.GetValue(observedUi) as List<GameObject> : null;
            }
            var session = owner.Session; var state = session?.State;
            // CurrentText resolves authored arrays in some phases. Read only when the immutable checkpoint changes.
            if (!ReferenceEquals(observedState, state)) { observedState = state; observedText = session?.CurrentText?.Id ?? ""; }
            string method = "";
            if (state?.Phase == CampaignPhase.Brewing && methodPanels != null)
                for (int i = 0; i < methodPanels.Count && i < MethodNames.Length; i++)
                    if (methodPanels[i] != null && methodPanels[i].activeSelf) { method = MethodNames[i]; break; }
            return new Frame {
                frame = Time.frameCount, scenario = Scenario ?? "unlabelled", phase = state == null ? "menu" : PhaseNames[(int)state.Phase],
                slot = state?.SlotIndex ?? -1, line = state?.LineIndex ?? -1, memories = state?.Memories.Count ?? 0,
                services = state?.Services.Count ?? 0, departures = state?.CompletedOccurrences.Count ?? 0,
                modal = owner.Ui?.VisibleModal ?? "", method = method, tool = owner.Input?.ActiveTool ?? "",
                textId = observedText, paired = session != null && session.CurrentEncounter.GuestIds.Count > 1,
                focused = UnityEngine.Application.isFocused && (session?.Focused ?? true), saving = owner.Busy || session?.IsSaving == true,
                largeText = owner.LargeText, highContrast = owner.HighContrast,
                width = Screen.width, height = Screen.height, vSyncCount = QualitySettings.vSyncCount, targetFrameRate = UnityEngine.Application.targetFrameRate
            };
        }
        private static bool SameContext(Frame a, Frame b) => a.scenario == b.scenario && a.phase == b.phase && a.slot == b.slot &&
            a.line == b.line && a.memories == b.memories && a.modal == b.modal && a.method == b.method && a.tool == b.tool &&
            a.paired == b.paired && a.focused == b.focused && a.saving == b.saving && a.largeText == b.largeText && a.highContrast == b.highContrast &&
            a.width == b.width && a.height == b.height && a.vSyncCount == b.vSyncCount && a.targetFrameRate == b.targetFrameRate;
        private void Update()
        {
            if (finished || metadata == null || owner == null) return;
            long tick = Stopwatch.GetTimestamp();
            if (lastTick == 0) {
                previous = Snapshot(); lastTick = tick;
                metadata.width = Screen.width; metadata.height = Screen.height; metadata.vSyncCount = QualitySettings.vSyncCount;
                metadata.targetFrameRate = UnityEngine.Application.targetFrameRate;
                metadata.evidenceProbeEnabledAtStart = GetComponent<CampaignEvidenceProbe>()?.enabled == true;
                return;
            }
            var current = Snapshot(); stable = SameContext(previous, current) ? stable + 1 : 0;
            var row = previous; row.timeSeconds = (tick - started) / (double)Stopwatch.Frequency;
            row.frameMs = (tick - lastTick) * 1000.0 / Stopwatch.Frequency; row.unityDeltaMs = Time.unscaledDeltaTime * 1000.0;
            row.contextStableFrames = stable; row.warmup = row.timeSeconds < WarmupSeconds;
            double main = counters[0].Read(); row.mainThreadMs = main < 0 ? -1 : main / 1000000.0;
            row.gcAllocatedBytes = counters[1].Read(); row.totalUsedBytes = counters[2].Read();
            row.systemUsedBytes = counters[3].Read(); row.gfxUsedBytes = counters[4].Read();
            if (process != null && row.timeSeconds >= nextWorkingSet) {
                nextWorkingSet = row.timeSeconds + 1;
                try { process.Refresh(); workingSet = process.WorkingSet64; workingSetAt = row.timeSeconds; }
                catch (Exception e) { metadata.workingSetError = e.GetType().Name; process.Dispose(); process = null; workingSet = -1; }
            }
            row.workingSetBytes = workingSet; row.workingSetAgeSeconds = workingSetAt < 0 ? -1 : row.timeSeconds - workingSetAt;
            row.cpuFrameMs = row.timingMainMs = row.presentWaitMs = row.gpuMs = -1;
            if (timingEnabled) {
                try {
                    if (FrameTimingManager.GetLatestTimings(1, timing) > 0) {
                        row.timingStamp = timing[0].frameStartTimestamp;
                        row.cpuFrameMs = Positive(timing[0].cpuFrameTime); row.timingMainMs = Positive(timing[0].cpuMainThreadFrameTime);
                        double wait = timing[0].cpuMainThreadPresentWaitTime;
                        row.presentWaitMs = wait >= 0 && !double.IsNaN(wait) && !double.IsInfinity(wait) ? wait : -1;
                        row.gpuMs = Positive(timing[0].gpuFrameTime);
                        if (row.gpuMs > 0) metadata.observedGpuSamples++;
                    }
                    FrameTimingManager.CaptureFrameTimings();
                } catch (Exception e) { metadata.timingError = e.GetType().Name; timingEnabled = false; }
            }
            // The observer's CPU work is disclosed, not subtracted from measured frame time.
            row.probeMs = SecondsSince(tick) * 1000;
            if (count < samples.Length) samples[count++] = row; else dropped++;
            lastTick = tick; previous = current;
        }
        private static double Positive(double value) => value > 0 && !double.IsNaN(value) && !double.IsInfinity(value) ? value : -1;

        public static void BeginCapture(string label)
        {
            if (active == null || active.finished || active.metadata == null) return;
            var interval = new Interval { label = label ?? "unlabelled", startFrame = Time.frameCount, startSeconds = SecondsSince(active.started) };
            active.captures.Add(interval); active.openCaptures.Push(interval);
        }
        public static void EndCapture()
        {
            if (active == null || active.finished || active.openCaptures.Count == 0) return;
            var interval = active.openCaptures.Pop(); interval.endFrame = Time.frameCount;
            interval.endSeconds = SecondsSince(active.started); interval.finished = true;
        }
        public static void Finish() { if (active != null) active.Flush(); }
        private void WriteMetadata() => File.WriteAllText(Path.Combine(output, "performance-metadata.json"), JsonUtility.ToJson(metadata, true));
        private void Flush()
        {
            if (finished || metadata == null) return;
            finished = true; metadata.samples = count; metadata.droppedSamples = dropped;
            metadata.durationSeconds = SecondsSince(started); metadata.finishedUtc = DateTime.UtcNow.ToString("o");
            metadata.gpuAvailability = metadata.observedGpuSamples > 0 ? "positive delayed GPU timings observed; deduplicate raw timestamp in summary" :
                metadata.frameTimingFeatureEnabled ? "no positive GPU timing observed; unavailable, never zero milliseconds" : metadata.gpuAvailability;
            long tick = Stopwatch.GetTimestamp();
            try {
                using (var writer = new StreamWriter(Path.Combine(output, "performance-frames.jsonl"), false))
                    for (int i = 0; i < count; i++) writer.WriteLine(JsonUtility.ToJson(samples[i]));
                File.WriteAllText(Path.Combine(output, "performance-captures.json"), JsonUtility.ToJson(new IntervalFile { intervals = captures }, true));
                metadata.rawTelemetryBytes = new FileInfo(Path.Combine(output, "performance-frames.jsonl")).Length;
                metadata.captureTelemetryBytes = new FileInfo(Path.Combine(output, "performance-captures.json")).Length;
                metadata.serializationMs = SecondsSince(tick) * 1000; metadata.finalized = true; WriteMetadata();
            } catch (Exception e) {
                metadata.finalized = false; metadata.outputError = e.GetType().Name + ": " + e.Message;
                UnityEngine.Debug.LogWarning("Campaign performance evidence incomplete: " + metadata.outputError);
                try { WriteMetadata(); } catch (IOException) { }
            }
            DisposeCounters();
        }
        private void DisposeCounters()
        {
            foreach (var counter in counters) counter.Dispose();
            process?.Dispose(); process = null;
        }
        private void OnApplicationQuit() { Flush(); }
        private void OnDestroy()
        {
            Flush(); DisposeCounters();
            if (active == this) active = null;
        }
    }
}
