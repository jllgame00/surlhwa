using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Surlhwa.Presentation
{
    public sealed class PixelCutawayRun : MonoBehaviour
    {
        private static readonly List<string> Errors = new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CollectErrors()
        {
            UnityEngine.Application.logMessageReceived -= OnLog; Errors.Clear();
            if (Environment.GetCommandLineArgs().Contains("-reviewCapture")) UnityEngine.Application.logMessageReceived += OnLog;
        }
        private static void OnLog(string condition, string trace, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Errors.Add(condition + "\n" + trace); }
        private IEnumerator Start()
        {
            string[] args = Environment.GetCommandLineArgs(); if (!args.Contains("-reviewCapture")) yield break;
            string output = Value(args, "-reviewOutput"), runId = Value(args, "-reviewRunId");
            bool preview = args.Contains("-cPreview");
            if (string.IsNullOrWhiteSpace(output)) { Debug.LogError("C requires -reviewOutput"); UnityEngine.Application.Quit(1); yield break; }
            Directory.CreateDirectory(output); var stage = GetComponent<PixelCutawayStage>();
            string revision = stage != null ? stage.Revision : "C";
            var evidence = new Evidence { runId = runId, utc = DateTime.UtcNow.ToString("O"), preview = preview,
                unity = UnityEngine.Application.unityVersion, gpu = SystemInfo.graphicsDeviceName, cpu = SystemInfo.processorType,
                graphicsAPI = SystemInfo.graphicsDeviceType.ToString(), source = "Bar_" + revision + ".unity / PixelCutawayBuilder; C_DIRECTION_OWNER_APPROVED; VISUAL_FIXTURE_ONLY",
                tier = "Windows Development Player; uncapped 60s explicit offscreen native 1080p render requests; no display latency/GPU timing claim" };
            if (string.IsNullOrWhiteSpace(runId) || stage == null || !stage.IsInitialized || Errors.Count != 0) {
                Debug.LogError("C_CAPTURE_ABORTED: run identity and initialized bindings required");
                evidence.errors = Errors.ToArray(); Write(output, evidence); UnityEngine.Application.Quit(1); yield break;
            }
            evidence.initialized = true; UnityEngine.Application.targetFrameRate = -1; QualitySettings.vSyncCount = 0;
            var actors = new[] { stage.Bartender, stage.Ordinary, stage.Quixote };
            var roots = actors.Select(a => a.transform.position).ToArray(); var feet = actors.Select(a => a.FeetAnchor.position).ToArray();
            stage.Select(3);
            if (!preview) {
                var target = RenderTexture.GetTemporary(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                for (int i = 0; i < 90; i++) { RenderPipeline.SubmitRenderRequest(stage.Camera, request); yield return null; }
                var frames = new List<double>(200000);
                using (var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame"))
                using (var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count"))
                using (var batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count")) {
                    long gcSum = 0, gcMax = 0, drawSum = 0, batchSum = 0; float start = Time.realtimeSinceStartup;
                    while (Time.realtimeSinceStartup - start < 60) {
                        RenderPipeline.SubmitRenderRequest(stage.Camera, request); yield return null; frames.Add(Time.unscaledDeltaTime * 1000);
                        if (gc.Valid) { gcSum += gc.LastValue; gcMax = Math.Max(gcMax, gc.LastValue); }
                        if (draws.Valid) drawSum += draws.LastValue; if (batches.Valid) batchSum += batches.LastValue;
                    }
                    frames.Sort(); int n = frames.Count;
                    evidence.measurement = new Measurement { durationSeconds = Time.realtimeSinceStartup - start, samples = n,
                        frameMeanMs = frames.Average(), frameP50Ms = frames[n / 2], frameP95Ms = frames[(int)(n * .95)], frameP99Ms = frames[(int)(n * .99)],
                        idleGCMeanBytes = gc.Valid ? gcSum / (double)n : -1, idleGCMaxBytes = gc.Valid ? gcMax : -1,
                        drawsMean = draws.Valid ? drawSum / (double)n : -1, batchesMean = batches.Valid ? batchSum / (double)n : -1,
                        gcRecorder = gc.Valid, drawRecorder = draws.Valid, batchRecorder = batches.Valid,
                        allocatedBytes = Profiler.GetTotalAllocatedMemoryLong(), reservedBytes = Profiler.GetTotalReservedMemoryLong(), monoUsedBytes = Profiler.GetMonoUsedSizeLong(),
                        processWorkingSetBytes = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 };
                }
                RenderTexture.ReleaseTemporary(target);
            }
            var captures = new List<Capture>();
            for (int fixture = 0; fixture < PixelCutawayStage.Fixtures.Length; fixture++) {
                stage.Select(fixture); for (int warm = 0; warm < 5; warm++) yield return null;
                foreach (int width in new[] { 1280, 1920 }) {
                    string file = revision + "_" + PixelCutawayStage.Fixtures[fixture] + "_" + width + "x" + width * 9 / 16 + ".png";
                    CaptureFrame(stage.Camera, Path.Combine(output, file), width);
                    captures.Add(new Capture { file = file, fixture = PixelCutawayStage.Fixtures[fixture], width = width, height = width * 9 / 16,
                        position = stage.Camera.transform.position, rotation = stage.Camera.transform.eulerAngles, orthographicSize = stage.Camera.orthographicSize,
                        nearClip = stage.Camera.nearClipPlane, farClip = stage.Camera.farClipPlane,
                        bartenderViewportQuad = stage.Bartender.Plane.sharedMesh.vertices.Select(v => stage.Camera.WorldToViewportPoint(stage.Bartender.Plane.transform.TransformPoint(v))).ToArray() });
                }
                for (int i = 0; i < actors.Length; i++) if (roots[i] != actors[i].transform.position || feet[i] != actors[i].FeetAnchor.position)
                    Debug.LogError("C static root/feet drift: " + actors[i].name);
            }
            stage.Select(3); var counters = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.name.StartsWith("C_Counter")).ToArray();
            foreach (var renderer in counters) renderer.enabled = false;
            CaptureFrame(stage.Camera, Path.Combine(output, revision + "_CounterRemovedDiagnostic_1920x1080.png"), 1920);
            foreach (var renderer in counters) renderer.enabled = true;
            if (!preview) foreach (int width in new[] { 1280, 1920 }) {
                stage.Select(1); stage.Bartender.Show(true, false);
                var actor = stage.Ordinary; Vector3 seat = actor.transform.position, entry = new Vector3(-4.2f, 0, -2.25f), front = new Vector3(-2.5f, 0, seat.z);
                string directory = Path.Combine(output, revision + "_motion_" + width); Directory.CreateDirectory(directory);
                for (int frame = 0; frame <= 120; frame++) {
                    if (frame < 24) actor.transform.position = Vector3.Lerp(entry, front, Mathf.SmoothStep(0, 1, frame / 24f));
                    else if (frame < 48) actor.transform.position = Vector3.Lerp(front, seat, Mathf.SmoothStep(0, 1, (frame - 24) / 24f));
                    else if (frame <= 72) actor.transform.position = seat;
                    else if (frame <= 96) actor.transform.position = Vector3.Lerp(seat, front, Mathf.SmoothStep(0, 1, (frame - 72) / 24f));
                    else actor.transform.position = Vector3.Lerp(front, entry, Mathf.SmoothStep(0, 1, (frame - 96) / 24f));
                    if (frame >= 48 && frame <= 72) actor.Show(true, true); else actor.ShowWalkFrame(frame / 2);
                    yield return null; CaptureFrame(stage.Camera, Path.Combine(directory, frame.ToString("D4") + ".png"), width);
                }
                actor.transform.position = seat;
            }
            stage.Select(3); evidence.captures = captures.ToArray(); evidence.errors = Errors.ToArray();
            evidence.staticRootAndFeetDrift = Errors.Any(e => e.Contains("drift")); Write(output, evidence);
            Debug.Log(revision + "_CAPTURE_FINISHED " + captures.Count + " files; errors " + Errors.Count);
            UnityEngine.Application.Quit(Errors.Count == 0 ? 0 : 1);
        }
        private static string Value(string[] args, string key) => args.SkipWhile(a => a != key).Skip(1).FirstOrDefault();
        private static void Write(string output, Evidence evidence) => File.WriteAllText(Path.Combine(output, "player-run.json"), JsonUtility.ToJson(evidence, true));
        private static void CaptureFrame(Camera camera, string path, int width)
        {
            int height = width * 9 / 16; var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active; float aspect = camera.aspect; var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            try {
                camera.aspect = (float)width / height; RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply(); File.WriteAllBytes(path, pixels.EncodeToPNG());
            } finally { camera.aspect = aspect; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.Destroy(pixels); }
        }
        [Serializable] private sealed class Capture { public string file, fixture; public int width, height; public Vector3 position, rotation; public Vector3[] bartenderViewportQuad; public float orthographicSize, nearClip, farClip; }
        [Serializable] private sealed class Measurement {
            public int samples; public double durationSeconds, frameMeanMs, frameP50Ms, frameP95Ms, frameP99Ms, idleGCMeanBytes, drawsMean, batchesMean;
            public long idleGCMaxBytes, allocatedBytes, reservedBytes, monoUsedBytes, processWorkingSetBytes; public bool gcRecorder, drawRecorder, batchRecorder;
        }
        [Serializable] private sealed class Evidence {
            public string runId, utc, unity, gpu, cpu, graphicsAPI, source, tier;
            public bool initialized, preview, staticRootAndFeetDrift; public Capture[] captures; public Measurement measurement; public string[] errors;
        }
    }
}
