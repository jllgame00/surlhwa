using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Saves only the two Bar scenes through Unity serialization after removal of
/// the dead UIManager.orderManager field and GaugeStopMiniGame source. It does
/// not alter retained UI, gameplay, or Bartending runner ownership.
/// </summary>
public static class SURLHWADeadServeGaugeStopCleanup_C5_5_3
{
    private const string BarScenePath = "Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity";
    private const string LayoutTestScenePath = "Assets/BarScene_LayoutTest.unity";
    private const string GaugeStopScriptPath = "Assets/Code/MiniGames/GaugeStopMiniGame.cs";
    private const string GaugeStopScriptGuid = "0e5e27f1424b60e4794366b6f34a0e2c";
    private const string ObsoleteOrderManagerProperty = "orderManager:";

    private const string MigrationMenuPath =
        "SURLHWA/Rebuild/Migrate/C5.5.3 Remove Dead Serve and GaugeStop";
    private const string VerificationMenuPath =
        "SURLHWA/Rebuild/Verify/C5.5.3 Dead Serve and GaugeStop";

    private static readonly string[] TargetScenePaths =
    {
        BarScenePath,
        LayoutTestScenePath
    };

    [MenuItem(MigrationMenuPath)]
    private static void MigrateFromMenu()
    {
        if (!CanRunFromMenu())
            return;

        RunWithMenuErrorHandling(RunMigration, "migration");
    }

    [MenuItem(VerificationMenuPath)]
    private static void VerifyFromMenu()
    {
        if (!CanRunFromMenu())
            return;

        RunWithMenuErrorHandling(RunVerification, "verification");
    }

    /// <summary>Batch-mode entry point for the retained, idempotent migration.</summary>
    public static void MigrateBatch()
    {
        RunMigration();
    }

    /// <summary>Batch-mode entry point for saved-final-state verification.</summary>
    public static void VerifyBatch()
    {
        RunVerification();
    }

    private static bool CanRunFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SURLHWA C5.5.3] Dead serve cleanup is available only in Edit Mode.");
            return false;
        }

        return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
    }

    private static void RunWithMenuErrorHandling(Action action, string operation)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            Debug.LogError("[SURLHWA C5.5.3] Dead serve cleanup " + operation + " failed: " + exception);
        }
    }

    private static void RunMigration()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,
            "Dead serve cleanup cannot run while entering or using Play Mode.");
        ValidateGaugeStopRemoved();

        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        bool savedAny = false;

        try
        {
            foreach (string scenePath in TargetScenePaths)
            {
                Scene scene = OpenTargetScene(scenePath);
                ValidateScene(scene);

                bool hasObsoleteSerialization = CountObsoleteOrderManagerProperties(scenePath) > 0;
                string retainedSnapshot = CaptureRetainedSemanticSnapshot(scene);

                if (!hasObsoleteSerialization)
                {
                    Debug.Log("[SURLHWA C5.5.3][ALREADY_MIGRATED] " + scenePath +
                              " has no serialized UIManager.orderManager value.");
                    continue;
                }

                EditorSceneManager.MarkSceneDirty(scene);
                Require(EditorSceneManager.SaveScene(scene),
                    "Unity failed to save dead serve cleanup: " + scenePath);

                Require(retainedSnapshot == CaptureRetainedSemanticSnapshot(scene),
                    "A retained Scene semantic changed while saving " + scenePath + ".");
                Require(CountObsoleteOrderManagerProperties(scenePath) == 0,
                    "Serialized UIManager.orderManager remains after Unity save: " + scenePath);

                Scene savedScene = OpenTargetScene(scenePath);
                ValidateScene(savedScene);
                savedAny = true;
                Debug.Log("[SURLHWA C5.5.3][MIGRATED] Unity removed obsolete UIManager.orderManager serialization from " + scenePath + ".");
            }
        }
        finally
        {
            RestoreSceneSetup(originalSetup);
        }

        VerifyFinalState();
        Debug.Log(savedAny
            ? "[SURLHWA C5.5.3][PASS] Dead serve serialization cleanup completed."
            : "[SURLHWA C5.5.3][ALREADY_MIGRATED] Dead serve serialization cleanup already matches the final state.");
    }

    private static void RunVerification()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,
            "Dead serve verification cannot run while entering or using Play Mode.");
        VerifyFinalState();
        Debug.Log("[SURLHWA C5.5.3][PASS] Dead serve and GaugeStop verification passed.");
    }

    private static void VerifyFinalState()
    {
        ValidateGaugeStopRemoved();

        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string scenePath in TargetScenePaths)
            {
                Scene scene = OpenTargetScene(scenePath);
                ValidateScene(scene);
                Require(CountObsoleteOrderManagerProperties(scenePath) == 0,
                    "Serialized UIManager.orderManager remains in " + scenePath + ".");
            }
        }
        finally
        {
            RestoreSceneSetup(originalSetup);
        }
    }

    private static Scene OpenTargetScene(string scenePath)
    {
        Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null,
            "Target Scene asset is missing: " + scenePath);

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Require(scene.IsValid() && scene.isLoaded && scene.path == scenePath,
            "Target Scene could not be opened: " + scenePath);
        return scene;
    }

    private static void ValidateScene(Scene scene)
    {
        Require(scene.IsValid() && scene.isLoaded,
            "Scene is not valid and loaded: " + scene.path);

        List<UIManager> uiManagers = FindSceneComponents<UIManager>(scene);
        Require(uiManagers.Count == 1,
            "Expected exactly one UIManager in " + scene.path + ". Actual: " + uiManagers.Count);

        SerializedObject serializedUiManager = new SerializedObject(uiManagers[0]);
        Require(serializedUiManager.FindProperty("orderManager") == null,
            "UIManager source still exposes the removed orderManager field.");
        ValidateNoMissingScripts(scene);
    }

    private static void ValidateGaugeStopRemoved()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Require(!File.Exists(GaugeStopScriptPath) && !File.Exists(GaugeStopScriptPath + ".meta"),
            "GaugeStopMiniGame source and meta must be removed together.");
        Require(AssetDatabase.LoadAssetAtPath<MonoScript>(GaugeStopScriptPath) == null,
            "GaugeStopMiniGame source still resolves as a MonoScript asset.");
        Require(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(GaugeStopScriptGuid)),
            "GaugeStopMiniGame GUID still resolves to an asset.");
    }

    // This is a read-only post-save serialization audit. Scene mutation occurs
    // only through EditorSceneManager.SaveScene above.
    private static int CountObsoleteOrderManagerProperties(string scenePath)
    {
        string serializedScene = File.ReadAllText(scenePath);
        string token = "\n  " + ObsoleteOrderManagerProperty;
        int count = 0;
        int startIndex = 0;

        while (true)
        {
            int index = serializedScene.IndexOf(token, startIndex, StringComparison.Ordinal);
            if (index < 0)
                return count;

            count++;
            startIndex = index + token.Length;
        }
    }

    private static string CaptureRetainedSemanticSnapshot(Scene scene)
    {
        StringBuilder snapshot = new StringBuilder(65536);
        foreach (GameObject gameObject in FindSceneGameObjects(scene))
        {
            snapshot.Append(GetHierarchyPath(gameObject.transform));
            snapshot.Append('|').Append(gameObject.name);
            snapshot.Append('|').Append(gameObject.activeSelf);
            snapshot.Append('|').Append(gameObject.layer);
            snapshot.Append('|').Append(gameObject.tag);
            snapshot.Append('|').Append((int)gameObject.hideFlags).AppendLine();

            foreach (Component component in gameObject.GetComponents<Component>())
            {
                Require(component != null,
                    "Missing Script found while capturing Scene semantics at " +
                    GetHierarchyPath(gameObject.transform) + " in " + scene.path + ".");
                snapshot.Append(component.GetType().AssemblyQualifiedName);
                snapshot.Append('|').Append(EditorJsonUtility.ToJson(component, false));
                snapshot.AppendLine();
            }
        }

        return snapshot.ToString();
    }

    private static List<T> FindSceneComponents<T>(Scene scene) where T : Component
    {
        List<T> components = new List<T>();
        foreach (GameObject gameObject in FindSceneGameObjects(scene))
        {
            foreach (T component in gameObject.GetComponents<T>())
            {
                if (component != null && component.gameObject.scene == scene)
                    components.Add(component);
            }
        }

        return components;
    }

    private static List<GameObject> FindSceneGameObjects(Scene scene)
    {
        List<GameObject> gameObjects = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
            AddHierarchyDepthFirst(root.transform, gameObjects);
        return gameObjects;
    }

    private static void AddHierarchyDepthFirst(Transform transform, List<GameObject> output)
    {
        output.Add(transform.gameObject);
        for (int index = 0; index < transform.childCount; index++)
            AddHierarchyDepthFirst(transform.GetChild(index), output);
    }

    private static string GetHierarchyPath(Transform transform)
    {
        return transform.parent == null
            ? transform.name
            : GetHierarchyPath(transform.parent) + "/" + transform.name;
    }

    private static void ValidateNoMissingScripts(Scene scene)
    {
        int missingScriptCount = 0;
        foreach (GameObject gameObject in FindSceneGameObjects(scene))
            missingScriptCount += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);

        Require(missingScriptCount == 0,
            "Scene contains Missing Script components: " + scene.path + ". Actual: " + missingScriptCount);
    }

    private static void RestoreSceneSetup(SceneSetup[] originalSetup)
    {
        if (originalSetup != null && originalSetup.Length > 0)
            EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
