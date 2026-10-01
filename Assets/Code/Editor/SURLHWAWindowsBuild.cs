#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class SURLHWAWindowsBuild
{
    [MenuItem("SURLHWA/Build/Windows Review Campaign")]
    public static void Run() => Build("Builds/Windows", false);
    [MenuItem("SURLHWA/Build/Windows Phase M Review")]
    public static void RunPhaseM() => Build("Builds/Windows-PhaseM-" + Git("rev-parse --short HEAD").Trim(), true);
    [MenuItem("SURLHWA/Build/Windows Phase N Review")]
    public static void RunPhaseN() => Build("Builds/Windows-PhaseN-" + Git("rev-parse --short HEAD").Trim(), true, "Phase N");
    [MenuItem("SURLHWA/Build/Windows Phase O Review")]
    public static void RunPhaseO() => Build("Builds/Windows-PhaseO-" + Git("rev-parse --short HEAD").Trim(), true, "Phase O");
    private static void Build(string output, bool phaseM, string phase = "Phase M")
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before building.");
        var setup = UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Preserve unsaved scene edits before building.");
        string revision = Git("rev-parse HEAD").Trim();
        string dirty = Git("status --short"); // File names only; never read protected configuration.
        if (phaseM && dirty.Split('\n').Any(line => line.Contains("Assets/Code/") || line.Contains(".unity") || line.Contains(".prefab") ||
            (phase != "Phase N" && phase != "Phase O" && line.Contains("ProjectSettings/"))))
            throw new InvalidOperationException("Commit intended implementation before a versioned review build.");
        // Phase N builds in the existing isolated project using committed settings.
        // The original repository's unresolved batching choice remains untouched.
        if (phase == "Phase N" || phase == "Phase O")
        {
            string expected = Git("show HEAD:ProjectSettings/ProjectSettings.asset").Replace("\r\n", "\n");
            if (File.ReadAllText("ProjectSettings/ProjectSettings.asset").Replace("\r\n", "\n") != expected)
                throw new InvalidOperationException("Character/audio review requires committed PlayerSettings in the isolated build project.");
        }
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        string[] required = { "Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity", "Assets/_SURLHWA/Scenes/Gameplay/BartendingScene.unity" };
        if (!scenes.SequenceEqual(required)) throw new InvalidOperationException("Production scene order differs from the audited two-scene contract.");
        try
        {
            // Legacy static validators borrow/open scenes. Give them an isolated empty scene.
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
            SURLHWACampaignValidation.RunIntegrated();
            SURLHWAPresentationValidation.RunCharacters();
            ValidateSceneDependencies(scenes);
        }
        finally { UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup); }
        Directory.CreateDirectory(output);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = output + "/SURLHWA.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        foreach (var step in report.steps)
            foreach (var message in step.messages)
                if (message.type == LogType.Warning) Debug.Log("[Build Warning] " + message.content);
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Windows build failed: " + report.summary.result);
        File.Copy("README.md", output + "/README.md", true);
        Directory.CreateDirectory(output + "/Review");
        foreach (string name in new[] { "bar-night-1-1280x720.png", "stir-preparation-1280x720.png", "stir-guide-1280x720.png", "recipe-guide-1280x720.png", "recipe-guide-bottom-1280x720.png", "sound-settings-1280x720.png", "campaign-LoversReunion-1280x720.png", "campaign-HamletStillness-1280x720.png", "campaign-ending-1280x720.png", "campaign-coda-1280x720.png" })
            if (File.Exists("Logs/Review/" + name)) File.Copy("Logs/Review/" + name, output + "/Review/" + name, true);
        Directory.CreateDirectory(output + "/Docs/Licenses");
        foreach (string name in new[] { "CREDITS.md", "COCKTAIL_ART_DRAFTS.md", "CHARACTER_ART_DRAFTS.md", "AUDIO_DRAFTS.md", "ENVIRONMENT_ART_DRAFT.md", "RELEASE_REVIEW.md", "13_POST_I16_PRODUCTION_GAP_AUDIT.md", "00_PROJECT_VISION.md", "16_FIRST_PASS_CAMPAIGN.md", "14_SPECIAL_GUEST_ROADMAP.md", "15_IBA_COCKTAIL_CATALOG.md" })
            File.Copy("Docs/" + name, output + "/Docs/" + name, true);
        foreach (string file in Directory.GetFiles("Docs/Licenses"))
            File.Copy(file, output + "/Docs/Licenses/" + Path.GetFileName(file), true);
        if (phaseM)
        {
            foreach (string name in new[] { "22_UI_UX_DESIGN_SYSTEM.md", "23_UI_UX_IMPLEMENTATION_AND_REVIEW.md" })
                File.Copy("Docs/" + name, output + "/Docs/" + name, true);
            foreach (string file in Directory.GetFiles("Logs/Review", "phase-m-*.png").Where(f => !Path.GetFileName(f).Contains("baseline")))
                File.Copy(file, output + "/Review/" + Path.GetFileName(file), true);
        }
        if (phase == "Phase N" || phase == "Phase O")
        {
            File.Copy("Docs/25_CHARACTER_ANIMATION_AND_SERVICE_PRESENTATION.md", output + "/Docs/25_CHARACTER_ANIMATION_AND_SERVICE_PRESENTATION.md", true);
            if (Directory.Exists("Logs/Review/PhaseN"))
                foreach (string file in Directory.GetFiles("Logs/Review/PhaseN", "*-seated-1920x1080.png"))
                    File.Copy(file, output + "/Review/" + Path.GetFileName(file), true);
        }
        if (phase == "Phase O")
        {
            File.Copy("Docs/26_AUDIO_AND_AMBIENT_PRESENTATION.md", output + "/Docs/26_AUDIO_AND_AMBIENT_PRESENTATION.md", true);
            File.Copy("ArtSource/Audio/manifest.json", output + "/Docs/PhaseO-Audio-Provenance.json", true);
            File.Copy("ArtSource/Audio/README.md", output + "/Docs/PhaseO-Audio-Provenance.md", true);
        }
        File.WriteAllText(output + "/BUILD_INFO.txt", "SURLHWA first-pass full campaign (18 nights / 47 encounters)\nUnity " + Application.unityVersion +
            "\nReview: " + (phaseM ? phase : "Campaign") + "\nSource commit: " + revision +
            "\nTarget: StandaloneWindows64\nResult: " + report.summary.result + "\nDuration: " + report.summary.totalTime +
            "\nWarnings: " + report.summary.totalWarnings + "\nOutput: " + Path.GetFullPath(output) +
            "\nBuilt UTC: " + DateTime.UtcNow.ToString("O") + "\nTracked worktree at build (paths only):\n" + dirty +
            "\nNot a claim of final art, audio, feel or public-release approval.\n");
        Debug.Log("[Windows Build] PASS: " + Path.GetFullPath(output + "/SURLHWA.exe") +
            " / bytes=" + report.summary.totalSize + " / warnings=" + report.summary.totalWarnings);
    }
    private static string Git(string arguments)
    {
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git", arguments) {
            WorkingDirectory = Directory.GetCurrentDirectory(), UseShellExecute = false,
            CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true });
        string result = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new InvalidOperationException("Build identity lookup failed: " + error);
        return result;
    }
    private static void ValidateSceneDependencies(string[] scenes)
    {
        foreach (string path in AssetDatabase.GetDependencies(scenes, true).Where(p => p.EndsWith(".prefab")))
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null) throw new InvalidOperationException("Missing prefab: " + path);
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                    throw new InvalidOperationException("Missing script in dependency: " + path);
        }
        foreach (string path in scenes)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                    foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                            throw new InvalidOperationException("Missing script in build scene: " + path);
            }
            finally { if (opened) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true); }
        }
        Debug.Log("[Release References] PASS: both enabled scenes and their prefab dependencies have no missing scripts.");
    }
}
#endif
