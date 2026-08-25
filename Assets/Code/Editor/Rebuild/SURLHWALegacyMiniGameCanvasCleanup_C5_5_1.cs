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
/// Removes the isolated, Bar-side legacy MiniGameCanvas host from the two Bar
/// scenes. Production RecipeGuide ownership remains prefab-local to each
/// Bartending mini-game and is verified here without changing those prefabs.
/// </summary>
public static class SURLHWALegacyMiniGameCanvasCleanup_C5_5_1
{
    private const string ProductionScenePath = "Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity";
    private const string LayoutTestScenePath = "Assets/BarScene_LayoutTest.unity";
    private const string BartendingScenePath = "Assets/_SURLHWA/Scenes/Gameplay/BartendingScene.unity";
    private const string LegacyCanvasPath = "MiniGameCanvas";

    private const string MigrationMenuPath =
        "SURLHWA/Rebuild/Migrate/C5.5.1 Remove Legacy MiniGameCanvas";
    private const string VerificationMenuPath =
        "SURLHWA/Rebuild/Verify/C5.5.1 Legacy MiniGameCanvas";

    private static readonly SceneTarget[] TargetScenes =
    {
        new SceneTarget(ProductionScenePath, true),
        new SceneTarget(LayoutTestScenePath, false)
    };

    private static readonly string[] BartendingPrefabPaths =
    {
        "Assets/_SURLHWA/Prefabs/Bartending/StirMiniGame.prefab",
        "Assets/_SURLHWA/Prefabs/Bartending/ShakeMiniGame.prefab",
        "Assets/_SURLHWA/Prefabs/Bartending/BuildMiniGame.prefab",
        "Assets/_SURLHWA/Prefabs/Bartending/FloatMiniGame.prefab"
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
            Debug.LogError("[SURLHWA C5.5.1] Legacy canvas cleanup is available only in Edit Mode.");
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
            Debug.LogError("[SURLHWA C5.5.1] Legacy canvas " + operation + " failed: " + exception);
        }
    }

    private static void RunMigration()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,
            "Legacy canvas cleanup cannot run while entering or using Play Mode.");

        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        bool removedAny = false;

        try
        {
            // Validate both scene shapes before the first write.
            foreach (SceneTarget target in TargetScenes)
            {
                Scene scene = OpenScene(target.Path);
                ValidateSceneBeforeOrAfterMigration(scene, target);
            }

            foreach (SceneTarget target in TargetScenes)
            {
                Scene scene = OpenScene(target.Path);
                List<GameObject> legacyRoots = FindSceneGameObjectsAtPath(scene, LegacyCanvasPath);
                Require(legacyRoots.Count <= 1,
                    "Expected at most one " + LegacyCanvasPath + " in " + target.Path +
                    ". Actual: " + legacyRoots.Count);

                if (legacyRoots.Count == 0)
                {
                    Debug.Log("[SURLHWA C5.5.1][ALREADY_MIGRATED] " + target.Path +
                              " already has no legacy MiniGameCanvas.");
                    continue;
                }

                GameObject legacyRoot = legacyRoots[0];
                ValidateLegacyRoot(scene, legacyRoot, target);
                ValidateNoExternalSerializedReferences(scene, legacyRoot);

                string retainedSnapshot = CaptureSceneSnapshot(scene, legacyRoot);
                Undo.DestroyObjectImmediate(legacyRoot);

                Require(FindSceneGameObjectsAtPath(scene, LegacyCanvasPath).Count == 0,
                    "Legacy MiniGameCanvas removal did not persist in " + target.Path + ".");
                Require(retainedSnapshot == CaptureSceneSnapshot(scene, null),
                    "A retained Scene semantic changed while removing " + LegacyCanvasPath +
                    " from " + target.Path + ".");

                EditorSceneManager.MarkSceneDirty(scene);
                Require(EditorSceneManager.SaveScene(scene),
                    "Unity failed to save legacy canvas cleanup: " + target.Path);

                Scene savedScene = OpenScene(target.Path);
                ValidateFinalSceneState(savedScene, target);
                removedAny = true;
                Debug.Log("[SURLHWA C5.5.1][REMOVED] " + target.Path +
                          " removed the complete legacy MiniGameCanvas tree.");
            }

            VerifyAllFinalState();
        }
        finally
        {
            RestoreSceneSetup(originalSetup);
        }

        Debug.Log(removedAny
            ? "[SURLHWA C5.5.1][PASS] Both target Scenes now have zero legacy MiniGameCanvas roots."
            : "[SURLHWA C5.5.1][ALREADY_MIGRATED] Both target Scenes already match the desired final state.");
    }

    private static void RunVerification()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,
            "Legacy canvas verification cannot run while entering or using Play Mode.");

        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            VerifyAllFinalState();
        }
        finally
        {
            RestoreSceneSetup(originalSetup);
        }

        Debug.Log("[SURLHWA C5.5.1][PASS] Legacy MiniGameCanvas final-state verification passed.");
    }

    private static void VerifyAllFinalState()
    {
        foreach (SceneTarget target in TargetScenes)
        {
            Scene scene = OpenScene(target.Path);
            ValidateFinalSceneState(scene, target);
        }

        VerifyBartendingSceneEventSystemPolicy();
        VerifyProductionRecipeGuides();
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

    private static void ValidateSceneBeforeOrAfterMigration(Scene scene, SceneTarget target)
    {
        Require(scene.IsValid() && scene.isLoaded,
            "Scene is not valid and loaded: " + target.Path);
        List<GameObject> legacyRoots = FindSceneGameObjectsAtPath(scene, LegacyCanvasPath);
        Require(legacyRoots.Count <= 1,
            "Expected at most one " + LegacyCanvasPath + " in " + target.Path +
            ". Actual: " + legacyRoots.Count);

        if (legacyRoots.Count == 1)
            ValidateLegacyRoot(scene, legacyRoots[0], target);
        else
            ValidateFinalSceneState(scene, target);
    }

    private static void ValidateLegacyRoot(Scene scene, GameObject legacyRoot, SceneTarget target)
    {
        Require(legacyRoot != null && legacyRoot.scene == scene,
            "Legacy MiniGameCanvas is not a member of " + target.Path + ".");
        Require(GetHierarchyPath(legacyRoot.transform) == LegacyCanvasPath,
            "Legacy MiniGameCanvas hierarchy changed in " + target.Path + ". Actual: " +
            GetHierarchyPath(legacyRoot.transform));
        Require(legacyRoot.activeSelf == target.ExpectedActiveSelf,
            "Legacy MiniGameCanvas activeSelf changed unexpectedly in " + target.Path + ".");

        Require(legacyRoot.GetComponents<Component>().Length == 5 &&
                legacyRoot.GetComponents<Canvas>().Length == 1 &&
                legacyRoot.GetComponents<CanvasScaler>().Length == 1 &&
                legacyRoot.GetComponents<GraphicRaycaster>().Length == 1 &&
                legacyRoot.GetComponents<RecipeGuidePanelController>().Length == 1,
            "Legacy MiniGameCanvas component structure changed unexpectedly in " + target.Path + ".");

        Canvas canvas = legacyRoot.GetComponent<Canvas>();
        Require(canvas.renderMode == RenderMode.ScreenSpaceOverlay &&
                canvas.sortingLayerName == "Default" && canvas.sortingOrder == 0 &&
                !canvas.overrideSorting,
            "Legacy MiniGameCanvas Canvas policy changed unexpectedly in " + target.Path + ".");

        GameObject recipeGuidePanel = FindDescendantByPath(legacyRoot, LegacyCanvasPath + "/RecipeGuidePanel");
        GameObject recipeBookButton = FindDescendantByPath(legacyRoot, LegacyCanvasPath + "/RecipeBookButton");
        Require(recipeGuidePanel != null && !recipeGuidePanel.activeSelf,
            "Legacy RecipeGuidePanel must remain present and inactive until its parent is removed.");
        Require(recipeBookButton != null && !recipeBookButton.activeSelf,
            "Legacy RecipeBookButton must remain present and inactive until its parent is removed.");

        foreach (Button button in legacyRoot.GetComponentsInChildren<Button>(true))
        {
            Require(button.onClick.GetPersistentEventCount() == 0,
                "Legacy MiniGameCanvas has a persistent Button UnityEvent in " + target.Path + ".");
        }

        ValidateNoMissingScripts(scene);
    }

    private static void ValidateNoExternalSerializedReferences(Scene scene, GameObject legacyRoot)
    {
        foreach (GameObject gameObject in FindSceneGameObjects(scene))
        {
            if (IsInSubtree(gameObject, legacyRoot))
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
                    Require(!IsObjectInSubtree(reference, legacyRoot),
                        "BLOCKED_EXTERNAL_SERIALIZED_REFERENCE: " +
                        GetHierarchyPath(gameObject.transform) + "/" + component.GetType().Name +
                        " references legacy MiniGameCanvas at property " + property.propertyPath + ".");
                }
            }
        }
    }

    private static void ValidateFinalSceneState(Scene scene, SceneTarget target)
    {
        Require(FindSceneGameObjectsAtPath(scene, LegacyCanvasPath).Count == 0,
            "Legacy MiniGameCanvas remains in " + target.Path + ".");
        ValidateNoMissingScripts(scene);

        if (!target.IsProduction)
            return;

        GameObject rootCanvas = FindExactGameObject(scene, "Canvas");
        Canvas rootCanvasComponent = RequireSingleCanvas(rootCanvas, "Canvas");
        Require(rootCanvasComponent.renderMode == RenderMode.ScreenSpaceOverlay &&
                rootCanvasComponent.sortingLayerName == "Default" && rootCanvasComponent.sortingOrder == 0 &&
                rootCanvas.GetComponents<GraphicRaycaster>().Length == 1,
            "Bar root Canvas ownership policy changed unexpectedly.");

        GameObject notificationCanvas = FindExactGameObject(scene, "NotificationCanvas");
        Require(RequireSingleCanvas(notificationCanvas, "NotificationCanvas").sortingOrder == 0 &&
                notificationCanvas.GetComponents<GraphicRaycaster>().Length == 0,
            "NotificationCanvas raycast policy changed unexpectedly.");

        GameObject resultToast = FindExactGameObject(scene, "Canvas/ResultToastPanel");
        Canvas resultToastCanvas = RequireSingleCanvas(resultToast, "Canvas/ResultToastPanel");
        Require(resultToastCanvas.overrideSorting && resultToastCanvas.sortingOrder == 20 &&
                resultToast.GetComponents<GraphicRaycaster>().Length == 0,
            "ResultToast Canvas ownership policy changed unexpectedly.");

        GameObject dayResult = FindExactGameObject(scene, "Canvas/DayResultPanel");
        Canvas dayResultCanvas = RequireSingleCanvas(dayResult, "Canvas/DayResultPanel");
        Require(dayResultCanvas.overrideSorting && dayResultCanvas.sortingOrder == 30 &&
                dayResult.GetComponents<GraphicRaycaster>().Length == 1,
            "DayResult Canvas ownership policy changed unexpectedly.");
        Require(dayResult.GetComponent<Image>() != null && dayResult.GetComponent<Image>().raycastTarget &&
                dayResult.GetComponentsInChildren<Button>(true).Length >= 2,
            "DayResult input surface changed unexpectedly.");

        Require(FindSceneComponents<EventSystem>(scene).Count == 1,
            "BarScene must retain exactly one EventSystem.");
    }

    private static void VerifyBartendingSceneEventSystemPolicy()
    {
        Scene scene = OpenScene(BartendingScenePath);
        Require(FindSceneComponents<EventSystem>(scene).Count == 0,
            "BartendingScene must retain zero EventSystems.");
        ValidateNoMissingScripts(scene);
    }

    private static void VerifyProductionRecipeGuides()
    {
        foreach (string prefabPath in BartendingPrefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Require(prefab != null, "Production Bartending prefab is missing: " + prefabPath);

            RecipeGuidePanelController[] guides = prefab.GetComponentsInChildren<RecipeGuidePanelController>(true);
            Require(guides.Length == 1,
                "Expected exactly one prefab-local RecipeGuidePanelController in " + prefabPath +
                ". Actual: " + guides.Length);

            SerializedObject serializedGuide = new SerializedObject(guides[0]);
            Require(HasObjectReference(serializedGuide, "panelRoot") &&
                    HasObjectReference(serializedGuide, "openButton") &&
                    HasObjectReference(serializedGuide, "closeButton") &&
                    HasObjectReference(serializedGuide, "recipeListParent") &&
                    HasObjectReference(serializedGuide, "recipeButtonPrefab"),
                "Prefab-local RecipeGuide references are incomplete in " + prefabPath + ".");
            SerializedProperty showOpenButtonOnAwake = serializedGuide.FindProperty("showOpenButtonOnAwake");
            Require(showOpenButtonOnAwake != null && showOpenButtonOnAwake.boolValue,
                "Prefab-local RecipeGuide open-button policy changed in " + prefabPath + ".");
        }
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

    private static GameObject FindDescendantByPath(GameObject root, string hierarchyPath)
    {
        foreach (GameObject gameObject in FindHierarchyGameObjects(root))
        {
            if (GetHierarchyPath(gameObject.transform) == hierarchyPath)
                return gameObject;
        }

        return null;
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

    private static List<GameObject> FindHierarchyGameObjects(GameObject root)
    {
        List<GameObject> gameObjects = new List<GameObject>();
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

            // Removing a root shifts the absolute sibling indices of later roots.
            // Preserve retained hierarchy paths and their traversal order instead,
            // so that expected index compaction cannot mask or falsely report a
            // retained-object semantic change.
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
        public readonly bool ExpectedActiveSelf;

        public SceneTarget(string path, bool isProduction)
        {
            Path = path;
            IsProduction = isProduction;
            ExpectedActiveSelf = isProduction;
        }
    }
}
