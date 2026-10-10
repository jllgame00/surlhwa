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
using Surlhwa.Application;
namespace Surlhwa.Presentation
{
    // Automated review harness, separate from gameplay. Captures always come from the built Player.
    public sealed class ReviewRun : MonoBehaviour
    {
        private readonly List<string> errors = new List<string>();
        private IEnumerator Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (!args.Contains("-reviewCapture")) yield break;
            UnityEngine.Application.logMessageReceived += OnLog;
            string output = args.SkipWhile(x => x != "-reviewOutput").Skip(1).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(output)) { Debug.LogError("-reviewOutput is required."); UnityEngine.Application.Quit(1); yield break; }
            Directory.CreateDirectory(output);
            var stage = GetComponent<ReviewStage>();
            UnityEngine.Application.targetFrameRate = -1; QualitySettings.vSyncCount = 0;
            var actors = new[] { stage.Bartender, stage.Ordinary, stage.Quixote };
            var roots = actors.Select(x => x.transform.position).ToArray();
            var feet = actors.Select(x => x.FeetAnchor.position).ToArray();
            var captures = new List<Capture>(); var measurements = new List<Measurement>();
            var performanceTarget = RenderTexture.GetTemporary(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var performanceRequest = new UniversalRenderPipeline.SingleCameraRequest { destination = performanceTarget };
            foreach (CameraVariant variant in Enum.GetValues(typeof(CameraVariant)))
            {
                stage.Select(variant, ReviewFixture.Composition);
                for (int i = 0; i < 90; i++) { RenderPipeline.SubmitRenderRequest(stage.ActiveCamera, performanceRequest); yield return null; }
                var frameMs = new List<double>(200000);
                RenderPipeline.SubmitRenderRequest(stage.ActiveCamera, performanceRequest); yield return null;
                using (var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame"))
                using (var batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count"))
                using (var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count"))
                using (var main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread"))
                {
                    long gcSum = 0, gcMax = 0, batchSum = 0, drawSum = 0, mainSum = 0;
                    float started = Time.realtimeSinceStartup;
                    while (Time.realtimeSinceStartup - started < 60) {
                        RenderPipeline.SubmitRenderRequest(stage.ActiveCamera, performanceRequest);
                        yield return null; frameMs.Add(Time.unscaledDeltaTime * 1000);
                        if (gc.Valid) { gcSum += gc.LastValue; gcMax = Math.Max(gcMax, gc.LastValue); }
                        if (batches.Valid) batchSum += batches.LastValue;
                        if (draws.Valid) drawSum += draws.LastValue;
                        if (main.Valid) mainSum += main.LastValue;
                    }
                    float duration = Time.realtimeSinceStartup - started;
                    frameMs.Sort(); int samples = frameMs.Count;
                    var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    measurements.Add(new Measurement { variant = variant.ToString(), samples = samples, durationSeconds = duration,
                        frameMeanMs = frameMs.Average(), frameP50Ms = frameMs[samples / 2], frameP95Ms = frameMs[(int)(samples * .95)],
                        frameP99Ms = frameMs[(int)(samples * .99)], frameMaxMs = frameMs[samples - 1],
                        mainThreadMeanMs = main.Valid ? mainSum / (double)samples / 1000000 : -1,
                        idleGCMeanBytes = gc.Valid ? gcSum / (double)samples : -1, idleGCMaxBytes = gc.Valid ? gcMax : -1,
                        batchesMean = batches.Valid ? batchSum / (double)samples : -1, drawsMean = draws.Valid ? drawSum / (double)samples : -1,
                        allocatedBytes = Profiler.GetTotalAllocatedMemoryLong(), reservedBytes = Profiler.GetTotalReservedMemoryLong(),
                        monoUsedBytes = Profiler.GetMonoUsedSizeLong(), processWorkingSetBytes = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64,
                        objects = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                        renderers = renderers.Length, enabledRenderers = renderers.Count(x => x.enabled),
                        materials = renderers.SelectMany(x => x.sharedMaterials).Distinct().Count(),
                        textureBytes = renderers.SelectMany(x => x.sharedMaterials).Select(x => x.mainTexture).Where(x => x != null).Distinct().Sum(x => Profiler.GetRuntimeMemorySizeLong(x)),
                        gcRecorder = gc.Valid, batchRecorder = batches.Valid, drawRecorder = draws.Valid, mainRecorder = main.Valid });
                }
                foreach (ReviewFixture fixture in Enum.GetValues(typeof(ReviewFixture)))
                {
                    stage.Select(variant, fixture);
                    for (int warm = 0; warm < 5; warm++) yield return null;
                    foreach (int width in new[] { 1280, 1920 }) {
                        int height = width * 9 / 16; string filename = variant + "_" + fixture + "_" + width + "x" + height + ".png";
                        CaptureFrame(stage.ActiveCamera, Path.Combine(output, filename), width, height);
                        captures.Add(new Capture { file = filename, camera = variant.ToString(), fixture = fixture.ToString(),
                            width = width, height = height, position = stage.ActiveCamera.transform.position,
                            bartenderViewportQuad = stage.Bartender.Plane.sharedMesh.vertices.Select(v => stage.ActiveCamera.WorldToViewportPoint(stage.Bartender.Plane.transform.TransformPoint(v))).ToArray(),
                            rotationEuler = stage.ActiveCamera.transform.eulerAngles, orthographicSize = stage.ActiveCamera.orthographicSize,
                            fieldOfView = stage.ActiveCamera.fieldOfView, nearClip = stage.ActiveCamera.nearClipPlane, farClip = stage.ActiveCamera.farClipPlane });
                    }
                    for (int i = 0; i < actors.Length; i++)
                        if (actors[i].transform.position != roots[i] || actors[i].FeetAnchor.position != feet[i]) Debug.LogError("Static actor drift: " + actors[i].name);
                }
                stage.Select(variant, ReviewFixture.Composition);
                var counters = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(x => x.name.StartsWith("Counter")).ToArray();
                foreach (var counter in counters) counter.enabled = false;
                CaptureFrame(stage.ActiveCamera, Path.Combine(output, variant + "_CounterRemovedDiagnostic_1920x1080.png"), 1920, 1080);
                foreach (var counter in counters) counter.enabled = true;
                // Deterministic spatial replay, not native gameplay input or final locomotion animation.
                foreach (int width in new[] { 1280, 1920 }) {
                    stage.Select(variant, ReviewFixture.Standing);
                    string clipDirectory = Path.Combine(output, variant + "_motion_" + width); Directory.CreateDirectory(clipDirectory);
                    var ordinary = stage.Ordinary; Vector3 seat = ordinary.transform.position;
                    Vector3 entry = new Vector3(-3.9f, 0, 2.5f), front = new Vector3(-3.75f, 0, -1.15f);
                    for (int frame = 0; frame <= 120; frame++) {
                        if (frame < 24) ordinary.transform.position = Vector3.Lerp(entry, front, Mathf.SmoothStep(0, 1, frame / 24f));
                        else if (frame < 48) ordinary.transform.position = Vector3.Lerp(front, seat, Mathf.SmoothStep(0, 1, (frame - 24) / 24f));
                        else if (frame <= 72) ordinary.transform.position = seat;
                        else if (frame <= 96) ordinary.transform.position = Vector3.Lerp(seat, front, Mathf.SmoothStep(0, 1, (frame - 72) / 24f));
                        else ordinary.transform.position = Vector3.Lerp(front, entry, Mathf.SmoothStep(0, 1, (frame - 96) / 24f));
                        if (frame >= 48 && frame <= 72) ordinary.Show(true, true); else ordinary.ShowWalkFrame(frame / 2);
                        yield return null;
                        CaptureFrame(stage.ActiveCamera, Path.Combine(clipDirectory, frame.ToString("D4") + ".png"), width, width * 9 / 16);
                    }
                    ordinary.transform.position = seat; stage.Select(variant, ReviewFixture.Composition);
                }
            }
            RenderTexture.ReleaseTemporary(performanceTarget);
            var evidence = new RunEvidence { utc = DateTime.UtcNow.ToString("O"), unity = UnityEngine.Application.unityVersion,
                tier = "Windows Development Player / native URP render requests; offscreen 1080p render each frame, uncapped 60s per camera; batchmode", gpu = SystemInfo.graphicsDeviceName,
                graphicsAPI = SystemInfo.graphicsDeviceType.ToString(), cpu = SystemInfo.processorType,
                os = SystemInfo.operatingSystem, memoryMB = SystemInfo.systemMemorySize, screenWidth = Screen.width, screenHeight = Screen.height,
                quality = QualitySettings.names[QualitySettings.GetQualityLevel()], measurements = measurements.ToArray(),
                captures = captures.ToArray(), errors = errors.ToArray(), staticRootAndFeetDrift = errors.Any(x => x.Contains("drift")),
                source = "Shared Bar.unity, generated from SpatialProofBuilder.cs; ARCH_SLICE_PROVISIONAL" };
            File.WriteAllText(Path.Combine(output, "player-run.json"), JsonUtility.ToJson(evidence, true));
            Debug.Log("SPATIAL_CAPTURE_FINISHED " + captures.Count + " files; errors " + errors.Count);
            UnityEngine.Application.Quit(errors.Count == 0 ? 0 : 1);
        }
        private void OnLog(string condition, string trace, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(condition + "\n" + trace); }
        private static void CaptureFrame(Camera camera, string path, int width, int height)
        {
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active; float aspect = camera.aspect;
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            try {
                camera.aspect = (float)width / height;
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            } finally { camera.aspect = aspect; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.Destroy(pixels); }
        }
        [Serializable] private sealed class Capture { public string file, camera, fixture; public int width, height; public Vector3 position, rotationEuler; public Vector3[] bartenderViewportQuad; public float orthographicSize, fieldOfView, nearClip, farClip; }
        [Serializable] private sealed class Measurement {
            public string variant; public int samples, objects, renderers, enabledRenderers, materials;
            public double durationSeconds, frameMeanMs, frameP50Ms, frameP95Ms, frameP99Ms, frameMaxMs, mainThreadMeanMs, idleGCMeanBytes, batchesMean, drawsMean;
            public long idleGCMaxBytes, allocatedBytes, reservedBytes, monoUsedBytes, processWorkingSetBytes, textureBytes;
            public bool gcRecorder, batchRecorder, drawRecorder, mainRecorder;
        }
        [Serializable] private sealed class RunEvidence {
            public string utc, unity, tier, gpu, graphicsAPI, cpu, os, quality, source;
            public int memoryMB, screenWidth, screenHeight; public bool staticRootAndFeetDrift;
            public Capture[] captures; public Measurement[] measurements; public string[] errors;
        }
    }
}
