using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace Surlhwa.Editor
{
    public static class SpatialProofAutomation
    {
        public static void BuildPlayer()
        {
            SpatialProofBuilder.Build();
            string output = Path.GetFullPath("../Validation/Builds/Windows/SurlhwaSpatialProof.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/Surlhwa/Scenes/Bootstrap.unity", "Assets/Surlhwa/Scenes/Bar.unity" },
                locationPathName = output, target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development });
            string json = JsonUtility.ToJson(new BuildEvidence { result = report.summary.result.ToString(),
                errors = report.summary.totalErrors, warnings = report.summary.totalWarnings,
                bytes = report.summary.totalSize, seconds = report.summary.totalTime.TotalSeconds,
                unity = UnityEngine.Application.unityVersion, output = output }, true);
            Directory.CreateDirectory("../Validation/Manifests"); File.WriteAllText("../Validation/Manifests/build.json", json);
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Production Player build failed.");
            Debug.Log("PRODUCTION_PLAYER_BUILD_SUCCEEDED: " + output);
        }
        [Serializable] private sealed class BuildEvidence { public string result, unity, output; public int errors, warnings; public ulong bytes; public double seconds; }
    }
}
