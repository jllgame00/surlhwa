using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Removes the duplicate Bar-side MiniGameManager ownership units after the
/// Coordinator's serialized compatibility seam has been removed. The retained
/// production runner is discovered only inside BartendingScene at runtime.
/// </summary>
public static class SURLHWALegacyBarMiniGameManagerCleanup_C5_5_2
{
    private const string BarScenePath = "Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity";
    private const string LayoutTestScenePath = "Assets/BarScene_LayoutTest.unity";
    private const string BartendingScenePath = "Assets/_SURLHWA/Scenes/Gameplay/BartendingScene.unity";
    private const string LegacyRunnerPath = "Manager/MiniGameManager";
    private const string BartendingRunnerPath = "_GAMEPLAY/MiniGameRoot/MiniGameManager";

    private const string MigrationMenuPath =
        "SURLHWA/Rebuild/Migrate/C5.5.2 Remove Legacy Bar MiniGameManager";
    private const string VerificationMenuPath =
        "SURLHWA/Rebuild/Verify/C5.5.2 Legacy Bar MiniGameManager";

    private static readonly SceneTarget[] BarSceneTargets =
    {
        new SceneTarget(BarScenePath, true),
        new SceneTarget(LayoutTestScenePath, false)
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
            Debug.LogError("[SURLHWA C5.5.2] Legacy runner cleanup is available only in Edit Mode.");
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
            Debug.LogError("[SURLHWA C5.5.2] Legacy runner " + operation + " failed: " + exception);
        }
    }

    private static void RunMigration()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,
            "Legacy runner cleanup cannot run while entering or using Play Mode.");

        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        bool removedAny = false;

        try
        {
            // Reject unexpected target shapes before the first write.
            foreach (SceneTarget target in BarSceneTargets)
            {
                Scene scene = OpenScene(target.Path);
                ValidateBarSceneBeforeOrAfterMigration(scene, target);
            }

            VerifyBartendingSceneRunner();

            foreach (SceneTarget target in BarSceneTargets)
            {
                Scene scene = OpenScene(target.Path);
                List<GameObject> legacyRunners = FindSceneGameObjectsAtPath(scene, LegacyRunnerPath);
                Require(legacyRunners.Count <= 1,
                    "Expected at most one legacy Bar MiniGameManager at " + LegacyRunnerPath +
                    " in " + target.Path + ". Actual: " + legacyRunners.Count);

                if (legacyRunners.Count == 0)
                {
                    Debug.Log("[SURLHWA C5.5.2][ALREADY_MIGRATED] " + target.Path +
                              " already has no Bar-side MiniGameManager.");
                    continue;
                }

                GameObject legacyRunner = legacyRunners[0];
                ValidateLegacyRunner(scene, legacyRunner, target);
                ValidateNoExternalSerializedReferences(scene, legacyRunner);

                string retainedSnapshot = CaptureSceneSnapshot(scene, legacyRunner);
                Undo.DestroyObjectImmediate(legacyRunner);

                Require(FindSceneComponents<MiniGameManager>(scene).Count == 0 &&
                        FindSceneGameObjectsAtPath(scene, LegacyRunnerPath).Count == 0,
                    "Legacy Bar MiniGameManager removal did not persist in " + target.Path + ".");
                Require(retainedSnapshot == CaptureSceneSnapshot(scene, null),
                    "A retained Scene semantic changed while removing the legacy runner from " +
                    target.Path + ".");

                EditorSceneManager.MarkSceneDirty(scene);
                Require(EditorSceneManager.SaveScene(scene),
                    "Unity failed to save legacy runner cleanup: " + target.Path);

                Scene savedScene = OpenScene(target.Path);
                ValidateFinalBarSceneState(savedScene, target);
                removedAny = true;
                Debug.Log("[SURLHWA C5.5.2][REMOVED] " + target.Path +
                          " removed the complete legacy Bar MiniGameManager GameObject.");
            }

            VerifyAllFinalState();
        }
        finally
        {
            RestoreSceneSetup(originalSetup);
        }

        Debug.Log(removedAny
            ? "[SURLHWA C5.5.2][PASS] BarScene and LayoutTest now have zero Bar-side MiniGameManagers."
            : "[SURLHWA C5.5.2][ALREADY_MIGRATED] Both target Scenes already match the desired final state.");
    }

    private static void RunVerification()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,
            "Legacy runner verification cannot run while entering or using Play Mode.");

        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            VerifyAllFinalState();
        }
        finally
        {
            RestoreSceneSetup(originalSetup);
        }

        Debug.Log("[SURLHWA C5.5.2][PASS] Legacy Bar MiniGameManager final-state verification passed.");
    }

    private static void VerifyAllFinalState()
    {
        foreach (SceneTarget target in BarSceneTargets)
        {
            Scene scene = OpenScene(target.Path);
            ValidateFinalBarSceneState(scene, target);
        }

        VerifyBartendingSceneRunner();
    }

    private static Scene OpenScene(string scenePath)
    {
        Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null,
            "Target Scene asset is missing: " + scenePath);

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Require(scene.IsValid() && scene.isLoaded && scene.path == scenePath,
            "Target Scene could not be opened: " + scenePath);
        return scene;
    }

    private static void ValidateBarSceneBeforeOrAfterMigration(Scene scene, SceneTarget target)
    {
        Require(scene.IsValid() && scene.isLoaded,
            "Scene is not valid and loaded: " + target.Path);

        List<GameObject> legacyRunners = FindSceneGameObjectsAtPath(scene, LegacyRunnerPath);
        Require(legacyRunners.Count <= 1,
            "Expected at most one legacy Bar MiniGameManager in " + target.Path +
            ". Actual: " + legacyRunners.Count);

        if (legacyRunners.Count == 1)
            ValidateLegacyRunner(scene, legacyRunners[0], target);
        else
            ValidateFinalBarSceneState(scene, target);
    }

    private static void ValidateLegacyRunner(Scene scene, GameObject legacyRunner, SceneTarget target)
    {
        Require(legacyRunner != null && legacyRunner.scene == scene && legacyRunner.activeSelf,
            "Legacy Bar MiniGameManager must be active in " + target.Path + ".");
        Require(GetHierarchyPath(legacyRunner.transform) == LegacyRunnerPath,
            "Legacy Bar MiniGameManager hierarchy changed in " + target.Path + ". Actual: " +
            GetHierarchyPath(legacyRunner.transform));
        Require(legacyRunner.transform.childCount == 0 &&
                legacyRunner.GetComponents<Component>().Length == 2 &&
                legacyRunner.GetComponents<MiniGameManager>().Length == 1,
            "Legacy Bar MiniGameManager GameObject contains unexpected retained content in " +
            target.Path + ".");

        ValidateRunnerConfiguration(legacyRunner.GetComponent<MiniGameManager>(), target.Path);
        ValidateNoMissingScripts(scene);
    }

    private static void ValidateRunnerConfiguration(MiniGameManager runner, string owner)
    {
        Require(runner != null && runner.enabled,
            "MiniGameManager must be enabled in " + owner + ".");
        SerializedObject serializedRunner = new SerializedObject(runner);
        Require(HasObjectReference(serializedRunner, "stirPrefab") &&
                HasObjectReference(serializedRunner, "shakePrefab") &&
                HasObjectReference(serializedRunner, "buildPrefab") &&
                HasObjectReference(serializedRunner, "floatPrefab"),
            "MiniGameManager prefab references are incomplete in " + owner + ".");
        Require(HasObjectReference(serializedRunner, "mainWorld") &&
                HasObjectReference(serializedRunner, "mainCamera"),
            "Legacy Bar MiniGameManager main-world/camera references changed unexpectedly in " + owner + ".");
        SerializedProperty debugPlayMode = serializedRunner.FindProperty("debugPlayMode");
        Require(debugPlayMode != null && debugPlayMode.enumValueIndex == 0,
            "MiniGameManager DebugPlayMode changed unexpectedly in " + owner + ".");
    }

    private static void ValidateNoExternalSerializedReferences(Scene scene, GameObject legacyRunner)
    {
        foreach (GameObject gameObject in FindSceneGameObjects(scene))
        {
            if (IsInSubtree(gameObject, legacyRunner))
                continue;

            foreach (Component component in gameObject.GetComponents<Component>())
            {
                Require(component != null,
                    "Missing Script found while auditing references in " + scene.path + ".");

                SerializedObject serializedComponent = new SerializedObject(component);
                SerializedProperty property = serializedComponent.GetIterator();
                bool enterChildren = true;
                while (property.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (property.propertyType != SerializedPropertyType.ObjectReference)
                        continue;

                    UnityEngine.Object reference = property.objectReferenceValue;
                    Require(!IsObjectInSubtree(reference, legacyRunner),
                        "BLOCKED_EXTERNAL_BAR_RUNNER_REFERENCE: " +
                        GetHierarchyPath(gameObject.transform) + "/" + component.GetType().Name +
                        " references the legacy runner at property " + property.propertyPath + ".");
                }
            }
        }
    }

    private static void ValidateFinalBarSceneState(Scene scene, SceneTarget target)
    {
        Require(FindSceneComponents<MiniGameManager>(scene).Count == 0 &&
                FindSceneGameObjectsAtPath(scene, LegacyRunnerPath).Count == 0,
            "Bar-side MiniGameManager remains in " + target.Path + ".");
        Require(FindSceneGameObjectsAtPath(scene, "MiniGameCanvas").Count == 0,
            "Legacy MiniGameCanvas must remain absent in " + target.Path + ".");
        ValidateNoMissingScripts(scene);

        if (!target.IsProduction)
            return;

        GameObject rootCanvas = FindExactGameObject(scene, "Canvas");
        Canvas rootCanvasComponent = RequireSingleCanvas(rootCanvas, "Canvas");
        Require(rootCanvasComponent.renderMode == RenderMode.ScreenSpaceOverlay &&
                rootCanvasComponent.sortingLayerName == "Default" && rootCanvasComponent.sortingOrder == 0 &&
                rootCanvas.GetComponents<GraphicRaycaster>().Length == 1,
            "Bar root Canvas ownership policy changed unexpectedly.");
        Require(FindSceneComponents<EventSystem>(scene).Count == 1,
            "BarScene must retain exactly one EventSystem.");
    }

    private static void VerifyBartendingSceneRunner()
    {
        Scene scene = OpenScene(BartendingScenePath);
        List<MiniGameManager> runners = FindSceneComponents<MiniGameManager>(scene);
        Require(runners.Count == 1,
            "BartendingScene must retain exactly one MiniGameManager. Actual: " + runners.Count);

        MiniGameManager runner = runners[0];
        Require(GetHierarchyPath(runner.transform) == BartendingRunnerPath && runner.enabled,
            "BartendingScene MiniGameManager ownership changed unexpectedly.");
        ValidateRunnerPrefabsOnly(runner, BartendingScenePath);
        Require(FindSceneComponents<EventSystem>(scene).Count == 0,
            "BartendingScene must retain zero EventSystems.");
        ValidateNoMissingScripts(scene);
    }

    private static void ValidateRunnerPrefabsOnly(MiniGameManager runner, string owner)
    {
        SerializedObject serializedRunner = new SerializedObject(runner);
        Require(HasObjectReference(serializedRunner, "stirPrefab") &&
                HasObjectReference(serializedRunner, "shakePrefab") &&
                HasObjectReference(serializedRunner, "buildPrefab") &&
                HasObjectReference(serializedRunner, "floatPrefab"),
            "Production MiniGameManager prefab references are incomplete in " + owner + ".");
        Require(serializedRunner.FindProperty("debugPlayMode") != null,
            "Production MiniGameManager DebugPlayMode field is missing in " + owner + ".");
    }

    private static bool HasObjectReference(SerializedObject serializedObject, string propertyName)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        return property != null && property.propertyType == SerializedPropertyType.ObjectReference &&
               property.objectReferenceValue != null;
    }

    private static Canvas RequireSingleCanvas(GameObject target, string targetPath)
    {
        Canvas[] canvases = target.GetComponents<Canvas>();
        Require(canvases.Length == 1,
            "Expected exactly one direct Canvas at " + targetPath + ". Actual: " + canvases.Length);
        return canvases[0];
    }

    private static GameObject FindExactGameObject(Scene scene, string hierarchyPath)
    {
        List<GameObject> matches = FindSceneGameObjectsAtPath(scene, hierarchyPath);
        Require(matches.Count == 1,
            "Expected exactly one GameObject at " + hierarchyPath + " in " + scene.path +
            ". Actual: " + matches.Count);
        return matches[0];
    }

    private static List<GameObject> FindSceneGameObjectsAtPath(Scene scene, string hierarchyPath)
    {
        List<GameObject> matches = new List<GameObject>();
        foreach (GameObject gameObject in FindSceneGameObjects(scene))
        {
            if (GetHierarchyPath(gameObject.transform) == hierarchyPath)
                matches.Add(gameObject);
        }

        return matches;
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

    private static string CaptureSceneSnapshot(Scene scene, GameObject excludedRoot)
    {
        StringBuilder snapshot = new StringBuilder(65536);
        foreach (GameObject gameObject in FindSceneGameObjects(scene))
        {
            if (excludedRoot != null && IsInSubtree(gameObject, excludedRoot))
                continue;

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

    private static bool IsObjectInSubtree(UnityEngine.Object reference, GameObject root)
    {
        if (reference == null || root == null)
            return false;

        GameObject referencedGameObject = reference as GameObject;
        if (referencedGameObject != null)
            return IsInSubtree(referencedGameObject, root);

        Component referencedComponent = reference as Component;
        return referencedComponent != null && IsInSubtree(referencedComponent.gameObject, root);
    }

    private static bool IsInSubtree(GameObject candidate, GameObject root)
    {
        return candidate == root || candidate.transform.IsChildOf(root.transform);
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

    private readonly struct SceneTarget
    {
        public readonly string Path;
        public readonly bool IsProduction;

        public SceneTarget(string path, bool isProduction)
        {
            Path = path;
            IsProduction = isProduction;
        }
    }
}
