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
    public static void Run()
    {
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        string[] required = { "Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity", "Assets/_SURLHWA/Scenes/Gameplay/BartendingScene.unity" };
        if (!scenes.SequenceEqual(required)) throw new InvalidOperationException("Production scene order differs from the audited two-scene contract.");
        SURLHWACampaignValidation.RunIntegrated();
        SURLHWAPresentationValidation.RunCharacters();
        ValidateSceneDependencies(scenes);
        const string output = "Builds/Windows";
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
        File.WriteAllText(output + "/BUILD_INFO.txt", "SURLHWA first-pass full campaign (18 nights / 47 encounters)\nUnity " + Application.unityVersion +
            "\nBuilt UTC: " + DateTime.UtcNow.ToString("O") + "\nNot a claim of final art, audio, feel or public-release approval.\n");
        Debug.Log("[Windows Build] PASS: " + Path.GetFullPath(output + "/SURLHWA.exe") +
            " / bytes=" + report.summary.totalSize + " / warnings=" + report.summary.totalWarnings);
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
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                    foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                            throw new InvalidOperationException("Missing script in build scene: " + path);
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true); }
        }
        Debug.Log("[Release References] PASS: both enabled scenes and their prefab dependencies have no missing scripts.");
    }
}
#endif
