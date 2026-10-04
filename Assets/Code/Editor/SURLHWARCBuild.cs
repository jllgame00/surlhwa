#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Explicit instrumented Windows QA package, with isolated preferences.</summary>
public static class SURLHWARCBuild
{
    public static void Run()
    {
        if (Application.companyName != "SURLHWA_RC_AUTOMATED_QA" || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("RC build requires the idle isolated Editor.");
        string root = Path.GetFullPath(File.ReadAllText("Logs/Review/RCAutomatedQA/source-root.txt").Trim());
        string revision = Git(root, "rev-parse HEAD").Trim();
        string provenance = Path.Combine(root, "Logs/Review/RCAutomatedQA/source-inputs.json");
        if (!File.Exists(provenance)) throw new InvalidOperationException("Record the isolated asset/settings input hashes before building.");
        if (!string.IsNullOrWhiteSpace(Git(root, "status --porcelain -- Assets/Code Tools")))
            throw new InvalidOperationException("Commit the QA implementation before building a known revision.");
        // Check every tracked code input, including metas, against the known source.
        using var hash = SHA256.Create();
        foreach (string relative in Git(root, "ls-files Assets/Code").Split('\n').Select(s => s.Trim()).Where(s => s.Length > 0))
        {
            byte[] expected = hash.ComputeHash(File.ReadAllBytes(Path.Combine(root, relative)));
            byte[] actual = hash.ComputeHash(File.ReadAllBytes(relative));
            if (!expected.SequenceEqual(actual)) throw new InvalidOperationException("Stale validation source: " + relative);
        }
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        string[] required = { "Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity", "Assets/_SURLHWA/Scenes/Gameplay/BartendingScene.unity" };
        if (!scenes.SequenceEqual(required)) throw new InvalidOperationException("Unexpected RC scene list.");
        string output = Path.Combine(root, "Builds", "Windows-RC-" + revision.Substring(0, 7));
        if (Directory.Exists(output)) throw new InvalidOperationException("Preserve the existing build; use a fresh source revision.");
        Directory.CreateDirectory(output);
        File.Copy(provenance, Path.Combine(output, "source-inputs.json"));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = scenes, target = BuildTarget.StandaloneWindows64,
            locationPathName = Path.Combine(output, "SURLHWA.exe"),
            options = BuildOptions.Development,
            extraScriptingDefines = new[] { "SURLHWA_RC_QA" }
        });
        foreach (var step in report.steps)
            foreach (var message in step.messages)
                if (message.type == LogType.Warning) Debug.Log("[RC build warning] " + message.content);
        File.WriteAllText(Path.Combine(output, "BUILD_INFO.txt"),
            "SURLHWA RC automated QA build\nSource commit: " + revision + "\nUnity: " + Application.unityVersion +
            "\nUTC: " + DateTime.UtcNow.ToString("O") + "\nResult: " + report.summary.result +
            "\nWarnings: " + report.summary.totalWarnings + "\nDuration: " + report.summary.totalTime +
            "\nPreference namespace: " + Application.companyName + "/" + Application.productName +
            "\nDevelopment build; explicit SURLHWA_RC_QA define. QA is opt-in by command line." +
            "\n--surlhwa-rc-native: observe actual input; --surlhwa-rc-focused: simulated production input." +
            "\nAsset provenance: see source-inputs.json in the local RC evidence directory.\n");
        if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("RC build failed: " + report.summary.result);
        Debug.Log("[RC build] PASS " + output);
    }
    static string Git(string root, string args)
    {
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git", args) {
            WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true });
        string output = process.StandardOutput.ReadToEnd(), error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new InvalidOperationException(error);
        return output;
    }
}
#endif
