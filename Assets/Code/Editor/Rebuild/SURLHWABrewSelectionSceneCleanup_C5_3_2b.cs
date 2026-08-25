using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Removes the runtime-detached BrewSelectionController component from the two
/// Bar scenes while retaining BrewPanel and its current interaction UI.
/// </summary>
public static class SURLHWABrewSelectionSceneCleanup_C5_3_2b
{
    private const string ProductionScenePath = "Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity";
    private const string LayoutTestScenePath = "Assets/BarScene_LayoutTest.unity";
    private const string BrewPanelPath = "Canvas/BrewPanel";
    private const string ScriptPath = "Assets/Code/UI/BrewSelectionController.cs";
    private const string ScriptGuid = "bd0da268c07346a44ae16f86597c5b71";

    private const string MigrationMenuPath =
        "SURLHWA/Rebuild/Migrate/C5.3.2b Remove BrewSelection Components";
    private const string VerificationMenuPath =
        "SURLHWA/Rebuild/Verify/C5.3.2b BrewSelection Components";

    private static readonly string[] TargetScenePaths =
    {
        ProductionScenePath,
        LayoutTestScenePath
    };

    private static readonly string[] RequiredBrewPanelChildren =
    {
        "MethodGroup/StirButton",
        "MethodGroup/ShakeButton",
        "MethodGroup/BuildButton",
        "MethodGroup/FloatButton",
        "ConfirmButton"
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

    /// <summary>Batch-mode entry point used to execute the checked-in migration.</summary>
    public static void MigrateBatch()
    {
        RunMigration();
    }

    /// <summary>Batch-mode entry point used to verify the saved final state.</summary>
    public static void VerifyBatch()
    {
        RunVerification();
    }

    private static bool CanRunFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SURLHWA C5.3.2b] Scene cleanup is available only in Edit Mode.");
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
            Debug.LogError(
                "[SURLHWA C5.3.2b] BrewSelection component " + operation +
                " failed: " + exception);
        }
    }

    private static void RunMigration()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,
            "Scene cleanup cannot run while entering or using Play Mode.");
        ValidateScriptAssetState();

        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        bool removedAny = false;

        try
        {
            // Validate both targets before the first write so an unexpected scene
            // shape cannot leave a newly started migration half-applied.
            foreach (string scenePath in TargetScenePaths)
            {
                Scene scene = OpenTargetScene(scenePath);
                ValidateScene(scene, true);
            }

            foreach (string scenePath in TargetScenePaths)
            {
                Scene scene = OpenTargetScene(scenePath);
                GameObject brewPanel = ValidateScene(scene, true);
                List<MonoBehaviour> components =
                    FindSceneComponentsWithScriptGuid(scene, ScriptGuid);

                if (components.Count == 0)
                {
                    Debug.Log(
                        "[SURLHWA C5.3.2b][ALREADY_MIGRATED] " + scenePath +
                        " already has no BrewSelectionController component.");
                    continue;
                }

                string semanticSnapshot = CaptureRetainedSemanticSnapshot(scene);

                UnityEngine.Object.DestroyImmediate(components[0]);

                ValidateScene(scene, false);
                Require(semanticSnapshot == CaptureRetainedSemanticSnapshot(scene),
                    "Retained Scene semantics changed while removing the component from " +
                    scenePath + ".");

                EditorSceneManager.MarkSceneDirty(scene);
                Require(EditorSceneManager.SaveScene(scene),
                    "Unity failed to save migrated Scene: " + scenePath);

                Scene savedScene = OpenTargetScene(scenePath);
                ValidateScene(savedScene, false);
                removedAny = true;

                Debug.Log(
                    "[SURLHWA C5.3.2b][REMOVED] Removed one BrewSelectionController " +
                    "component from " + scenePath + " at " + BrewPanelPath + ".");
            }
        }
        finally
        {
            RestoreSceneSetup(originalSetup);
        }

        Debug.Log(
            removedAny
                ? "[SURLHWA C5.3.2b][PASS] Both target Scenes have zero BrewSelectionController components."
                : "[SURLHWA C5.3.2b][ALREADY_MIGRATED] Both target Scenes already match the desired final state.");
    }

    private static void RunVerification()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,
            "Scene verification cannot run while entering or using Play Mode.");
        ValidateScriptAssetState();

        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

        try
        {
            foreach (string scenePath in TargetScenePaths)
            {
                Scene scene = OpenTargetScene(scenePath);
                ValidateScene(scene, false);
                Debug.Log(
                    "[SURLHWA C5.3.2b][VERIFIED] " + scenePath +
                    " retains " + BrewPanelPath +
                    " and has zero BrewSelectionController components.");
            }
        }
        finally
        {
            RestoreSceneSetup(originalSetup);
        }

        Debug.Log("[SURLHWA C5.3.2b][PASS] BrewSelection Scene component verification passed.");
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

    private static GameObject ValidateScene(Scene scene, bool allowLegacyComponent)
    {
        Require(scene.IsValid() && scene.isLoaded,
            "Scene is not valid and loaded: " + scene.path);

        List<GameObject> brewPanels = FindSceneGameObjectsNamed(scene, "BrewPanel");
        Require(brewPanels.Count == 1,
            "Expected exactly one BrewPanel in " + scene.path +
            ". Actual: " + brewPanels.Count);

        GameObject brewPanel = brewPanels[0];
        Require(GetHierarchyPath(brewPanel.transform) == BrewPanelPath,
            "BrewPanel hierarchy changed in " + scene.path + ". Actual: " +
            GetHierarchyPath(brewPanel.transform));

        List<MonoBehaviour> sceneComponents =
            FindSceneComponentsWithScriptGuid(scene, ScriptGuid);
        List<MonoBehaviour> panelComponents = sceneComponents.FindAll(
            component => component.gameObject == brewPanel);

        Require(sceneComponents.Count == panelComponents.Count,
            "A BrewSelectionController exists outside " + BrewPanelPath + " in " +
            scene.path + ".");
        Require(panelComponents.Count <= 1,
            "Expected at most one BrewSelectionController on " + BrewPanelPath +
            " in " + scene.path + ". Actual: " + panelComponents.Count);

        if (!allowLegacyComponent)
        {
            Require(panelComponents.Count == 0,
                "BrewSelectionController remains on " + BrewPanelPath + " in " +
                scene.path + ".");
        }

        List<InteractionUIController> interactionControllers =
            FindSceneComponents<InteractionUIController>(scene);
        Require(interactionControllers.Count == 1,
            "Expected exactly one InteractionUIController in " + scene.path +
            ". Actual: " + interactionControllers.Count);

        foreach (string relativePath in RequiredBrewPanelChildren)
        {
            Transform child = brewPanel.transform.Find(relativePath);
            Require(child != null,
                "Required BrewPanel child is missing in " + scene.path + ": " +
                BrewPanelPath + "/" + relativePath);
            Require(child.GetComponent<Button>() != null,
                "Required BrewPanel Button component is missing in " + scene.path + ": " +
                BrewPanelPath + "/" + relativePath);
        }

        ValidateNoMissingScripts(scene);
        return brewPanel;
    }

    private static void ValidateScriptAssetState()
    {
        bool sourceExists = File.Exists(ScriptPath);
        bool metaExists = File.Exists(ScriptPath + ".meta");
        Require(sourceExists == metaExists,
            "BrewSelectionController source and meta must exist or be removed together.");

        MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(ScriptPath);
        string guidAssetPath = AssetDatabase.GUIDToAssetPath(ScriptGuid);

        if (!sourceExists)
        {
            Require(script == null,
                "Deleted BrewSelectionController source still resolves as a MonoScript asset.");
            Require(string.IsNullOrEmpty(guidAssetPath) ||
                    AssetDatabase.LoadMainAssetAtPath(guidAssetPath) == null,
                "Deleted BrewSelectionController GUID still resolves to a loadable asset: " +
                guidAssetPath);
            Debug.Log(
                "[SURLHWA C5.3.2b][SCRIPT_REMOVED] BrewSelectionController source and meta " +
                "are absent as expected by C5.3.2c.");
            return;
        }

        Require(script != null, "BrewSelectionController source could not be loaded at " + ScriptPath + ".");
        Require(AssetDatabase.AssetPathToGUID(ScriptPath) == ScriptGuid,
            "BrewSelectionController script GUID changed unexpectedly.");
        Require(guidAssetPath == ScriptPath,
            "BrewSelectionController GUID resolves to an unexpected asset: " + guidAssetPath);
    }

    private static void ValidateNoMissingScripts(Scene scene)
    {
        int missingScriptCount = 0;
        foreach (GameObject gameObject in FindSceneGameObjects(scene))
            missingScriptCount += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);

        Require(missingScriptCount == 0,
            "Scene contains Missing Script components: " + scene.path +
            ". Actual: " + missingScriptCount);
    }

    private static string CaptureRetainedSemanticSnapshot(Scene scene)
    {
        StringBuilder snapshot = new StringBuilder(65536);

        foreach (GameObject gameObject in FindSceneGameObjects(scene))
        {
            snapshot.Append(GetHierarchyPathWithSiblingIndices(gameObject.transform));
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

                if (ComponentHasScriptGuid(component, ScriptGuid))
                    continue;

                snapshot.Append(component.GetType().AssemblyQualifiedName);
                snapshot.Append('|').Append(EditorJsonUtility.ToJson(component, false));
                snapshot.AppendLine();
            }
        }

        return snapshot.ToString();
    }

    private static List<T> FindSceneComponents<T>(Scene scene) where T : Component
    {
        List<T> matches = new List<T>();
        foreach (GameObject gameObject in FindSceneGameObjects(scene))
        {
            foreach (T component in gameObject.GetComponents<T>())
            {
                if (component != null && component.gameObject.scene == scene)
                    matches.Add(component);
            }
        }

        return matches;
    }

    private static List<MonoBehaviour> FindSceneComponentsWithScriptGuid(
        Scene scene,
        string scriptGuid)
    {
        List<MonoBehaviour> matches = new List<MonoBehaviour>();
        foreach (GameObject gameObject in FindSceneGameObjects(scene))
        {
            foreach (MonoBehaviour component in gameObject.GetComponents<MonoBehaviour>())
            {
                if (ComponentHasScriptGuid(component, scriptGuid))
                    matches.Add(component);
            }
        }

        return matches;
    }

    private static bool ComponentHasScriptGuid(Component component, string scriptGuid)
    {
        MonoBehaviour monoBehaviour = component as MonoBehaviour;
        if (monoBehaviour == null)
            return false;

        MonoScript monoScript = MonoScript.FromMonoBehaviour(monoBehaviour);
        if (monoScript == null)
            return false;

        string scriptPath = AssetDatabase.GetAssetPath(monoScript);
        return !string.IsNullOrEmpty(scriptPath) &&
            AssetDatabase.AssetPathToGUID(scriptPath) == scriptGuid;
    }

    private static List<GameObject> FindSceneGameObjectsNamed(Scene scene, string name)
    {
        List<GameObject> matches = new List<GameObject>();
        foreach (GameObject gameObject in FindSceneGameObjects(scene))
        {
            if (gameObject.name == name)
                matches.Add(gameObject);
        }

        return matches;
    }

    private static List<GameObject> FindSceneGameObjects(Scene scene)
    {
        List<GameObject> gameObjects = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
            AddHierarchyDepthFirst(root.transform, gameObjects);

        return gameObjects;
    }

    private static void AddHierarchyDepthFirst(Transform transform, List<GameObject> gameObjects)
    {
        gameObjects.Add(transform.gameObject);
        for (int childIndex = 0; childIndex < transform.childCount; childIndex++)
            AddHierarchyDepthFirst(transform.GetChild(childIndex), gameObjects);
    }

    private static string GetHierarchyPath(Transform transform)
    {
        return transform.parent == null
            ? transform.name
            : GetHierarchyPath(transform.parent) + "/" + transform.name;
    }

    private static string GetHierarchyPathWithSiblingIndices(Transform transform)
    {
        string current = transform.name + "[" + transform.GetSiblingIndex() + "]";
        return transform.parent == null
            ? current
            : GetHierarchyPathWithSiblingIndices(transform.parent) + "/" + current;
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
