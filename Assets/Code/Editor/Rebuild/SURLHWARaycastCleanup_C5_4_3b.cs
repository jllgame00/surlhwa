using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Removes only the audited visual-only GraphicRaycaster components from the
/// Bar notification Canvas and the two world-space feedback prefabs. This is
/// intentionally idempotent and preserves every retained serialized component.
/// </summary>
public static class SURLHWARaycastCleanup_C5_4_3b
{
    private const string BarScenePath = "Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity";
    private const string RootCanvasPath = "Canvas";
    private const string ResultToastPath = "Canvas/ResultToastPanel";
    private const string DayResultPath = "Canvas/DayResultPanel";
    private const string NotificationCanvasPath = "NotificationCanvas";

    private const string DialogueBubblePrefabPath = "Assets/_SURLHWA/Prefabs/UI/DialogueBubble.prefab";
    private const string DialogueBubbleCanvasPath = "DialogueBubble/Canvas";
    private const string HeartPopupPrefabPath = "Assets/_SURLHWA/Prefabs/UI/HeartPopup.prefab";
    private const string HeartPopupCanvasPath = "HeartPopup/Canvas";

    private const string MigrationMenuPath =
        "SURLHWA/Rebuild/Migrate/C5.4.3b Remove Visual-Only UI Raycasters";
    private const string VerificationMenuPath =
        "SURLHWA/Rebuild/Verify/C5.4.3b Visual-Only UI Raycasters";

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

    /// <summary>Batch-mode entry point for the checked-in migration.</summary>
    public static void MigrateBatch()
    {
        RunMigration();
    }

    /// <summary>Batch-mode entry point for final-state verification.</summary>
    public static void VerifyBatch()
    {
        RunVerification();
    }

    private static bool CanRunFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[SURLHWA C5.4.3b] Raycaster cleanup is available only in Edit Mode.");
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
            Debug.LogError("[SURLHWA C5.4.3b] Raycaster cleanup " + operation + " failed: " + exception);
        }
    }

    private static void RunMigration()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,
            "Raycaster cleanup cannot run while entering or using Play Mode.");

        bool sceneChanged = false;
        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Scene scene = OpenBarScene();
            GameObject notificationCanvas = FindExactGameObject(scene, NotificationCanvasPath);
            ValidateRequiredScenePolicy(scene);
            ValidateNotificationCanvas(notificationCanvas);

            string retainedSnapshot = CaptureSnapshot(FindSceneGameObjects(scene), notificationCanvas);
            sceneChanged = RemoveOnlyRaycaster(notificationCanvas, NotificationCanvasPath);
            Require(retainedSnapshot == CaptureSnapshot(FindSceneGameObjects(scene), notificationCanvas),
                "A retained BarScene semantic changed while removing NotificationCanvas GraphicRaycaster.");

            if (sceneChanged)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Require(EditorSceneManager.SaveScene(scene),
                    "Unity failed to save raycaster cleanup: " + BarScenePath);
            }

            Scene savedScene = OpenBarScene();
            ValidateRequiredScenePolicy(savedScene);
            GameObject savedNotificationCanvas = FindExactGameObject(savedScene, NotificationCanvasPath);
            ValidateNotificationCanvas(savedNotificationCanvas);
            Require(savedNotificationCanvas.GetComponents<GraphicRaycaster>().Length == 0,
                "NotificationCanvas still has a GraphicRaycaster after migration.");
        }
        finally
        {
            RestoreSceneSetup(originalSetup);
        }

        bool dialogueChanged = MigrateWorldSpacePrefab(
            DialogueBubblePrefabPath,
            DialogueBubbleCanvasPath,
            RenderMode.WorldSpace,
            500);
        bool heartChanged = MigrateWorldSpacePrefab(
            HeartPopupPrefabPath,
            HeartPopupCanvasPath,
            RenderMode.WorldSpace,
            0);

        VerifyAll();
        Debug.Log(sceneChanged || dialogueChanged || heartChanged
            ? "[SURLHWA C5.4.3b][MIGRATED] Removed only the three audited visual-only GraphicRaycasters."
            : "[SURLHWA C5.4.3b][ALREADY_MIGRATED] All audited visual-only GraphicRaycasters are already absent.");
    }

    private static void RunVerification()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,
            "Raycaster cleanup verification cannot run while entering or using Play Mode.");

        VerifyAll();
        Debug.Log("[SURLHWA C5.4.3b][PASS] Visual-only raycaster cleanup verification passed.");
    }

    private static void VerifyAll()
    {
        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Scene scene = OpenBarScene();
            ValidateRequiredScenePolicy(scene);
            GameObject notificationCanvas = FindExactGameObject(scene, NotificationCanvasPath);
            ValidateNotificationCanvas(notificationCanvas);
            Require(notificationCanvas.GetComponents<GraphicRaycaster>().Length == 0,
                "NotificationCanvas must have zero GraphicRaycasters.");
        }
        finally
        {
            RestoreSceneSetup(originalSetup);
        }

        VerifyWorldSpacePrefab(
            DialogueBubblePrefabPath,
            DialogueBubbleCanvasPath,
            RenderMode.WorldSpace,
            500);
        VerifyWorldSpacePrefab(
            HeartPopupPrefabPath,
            HeartPopupCanvasPath,
            RenderMode.WorldSpace,
            0);
    }

    private static Scene OpenBarScene()
    {
        Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(BarScenePath) != null,
            "Production BarScene is missing: " + BarScenePath);

        Scene scene = EditorSceneManager.OpenScene(BarScenePath, OpenSceneMode.Single);
        Require(scene.IsValid() && scene.isLoaded && scene.path == BarScenePath,
            "Production BarScene could not be opened: " + BarScenePath);
        return scene;
    }

    private static void ValidateRequiredScenePolicy(Scene scene)
    {
        GameObject rootCanvas = FindExactGameObject(scene, RootCanvasPath);
        Canvas rootCanvasComponent = RequireSingleCanvas(rootCanvas, RootCanvasPath);
        Require(rootCanvasComponent.renderMode == RenderMode.ScreenSpaceOverlay &&
                rootCanvasComponent.sortingLayerName == "Default" &&
                rootCanvasComponent.sortingOrder == 0,
            "Bar root Canvas policy changed unexpectedly.");
        Require(rootCanvas.GetComponents<GraphicRaycaster>().Length == 1,
            "Bar root Canvas must retain exactly one GraphicRaycaster.");

        GameObject resultToast = FindExactGameObject(scene, ResultToastPath);
        Canvas resultToastCanvas = RequireSingleCanvas(resultToast, ResultToastPath);
        Require(CanvasMatches(resultToastCanvas, RenderMode.ScreenSpaceOverlay, 20, true) &&
                resultToast.GetComponents<GraphicRaycaster>().Length == 0,
            "ResultToast Canvas ownership policy changed unexpectedly.");

        GameObject dayResult = FindExactGameObject(scene, DayResultPath);
        Canvas dayResultCanvas = RequireSingleCanvas(dayResult, DayResultPath);
        Require(CanvasMatches(dayResultCanvas, RenderMode.ScreenSpaceOverlay, 30, true) &&
                dayResult.GetComponents<GraphicRaycaster>().Length == 1,
            "DayResult Canvas ownership policy changed unexpectedly.");
        Require(dayResult.GetComponent<Image>() != null && dayResult.GetComponent<Image>().raycastTarget,
            "DayResult full-screen blocking Image must remain a raycast target.");
        Require(dayResult.GetComponentsInChildren<Button>(true).Length >= 2,
            "DayResult must retain its button controls.");

        Require(FindSceneGameObjectsAtPath(scene, "MiniGameCanvas").Count == 0,
            "Legacy MiniGameCanvas must be absent after C5.5.1 cleanup.");

        Require(FindSceneComponents<EventSystem>(scene).Count == 1,
            "BarScene must retain exactly one EventSystem.");
        ValidateNoMissingScripts(scene);
        VerifyBartendingPrefabPolicy();
    }

    private static void VerifyBartendingPrefabPolicy()
    {
        VerifyBartendingCanvas("Assets/_SURLHWA/Prefabs/Bartending/StirMiniGame.prefab", "StirMiniGame/UI");
        VerifyBartendingCanvas("Assets/_SURLHWA/Prefabs/Bartending/ShakeMiniGame.prefab", "ShakeMiniGame/UI");
        VerifyBartendingCanvas("Assets/_SURLHWA/Prefabs/Bartending/BuildMiniGame.prefab", "BuildMiniGame/UI");
        VerifyBartendingCanvas("Assets/_SURLHWA/Prefabs/Bartending/FloatMiniGame.prefab", "FloatMiniGame/UI");
    }

    private static void VerifyBartendingCanvas(string prefabPath, string canvasPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Require(prefab != null, "Bartending prefab is missing: " + prefabPath);

        Canvas[] canvases = prefab.GetComponentsInChildren<Canvas>(true);
        Require(canvases.Length == 1 && GetHierarchyPath(canvases[0].transform) == canvasPath &&
                CanvasMatches(canvases[0], RenderMode.ScreenSpaceOverlay, 10, false) &&
                canvases[0].GetComponents<GraphicRaycaster>().Length == 1,
            "Bartending Canvas policy changed unexpectedly in " + prefabPath + ".");
    }

    private static void ValidateNotificationCanvas(GameObject notificationCanvas)
    {
        Canvas canvas = RequireSingleCanvas(notificationCanvas, NotificationCanvasPath);
        Require(CanvasMatches(canvas, RenderMode.ScreenSpaceOverlay, 0, false),
            "NotificationCanvas Canvas policy changed unexpectedly.");
        Require(notificationCanvas.GetComponent<RecipeUnlockPopupController>() != null,
            "NotificationCanvas RecipeUnlockPopupController is missing.");
        Require(FindDescendantByPath(notificationCanvas, "NotificationCanvas/RecipeUnlockPopup") != null,
            "NotificationCanvas RecipeUnlockPopup subtree is missing.");
        ValidateVisualOnlyInputSurface(notificationCanvas, NotificationCanvasPath);
        Require(notificationCanvas.GetComponents<GraphicRaycaster>().Length <= 1,
            "UNEXPECTED_RAYCASTER_STRUCTURE at NotificationCanvas.");
    }

    private static bool MigrateWorldSpacePrefab(
        string prefabPath,
        string canvasPath,
        RenderMode expectedRenderMode,
        int expectedSortingOrder)
    {
        Require(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null,
            "Production prefab is missing: " + prefabPath);

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            GameObject canvasObject = FindDescendantByPath(prefabRoot, canvasPath);
            Require(canvasObject != null, "Expected Canvas is missing at " + canvasPath + ".");
            Canvas canvas = RequireSingleCanvas(canvasObject, canvasPath);
            ValidateWorldSpaceVisualPrefab(prefabRoot, canvas, prefabPath, canvasPath,
                expectedRenderMode, expectedSortingOrder);

            string retainedSnapshot = CaptureSnapshot(FindPrefabGameObjects(prefabRoot), canvasObject);
            bool changed = RemoveOnlyRaycaster(canvasObject, canvasPath);
            Require(retainedSnapshot == CaptureSnapshot(FindPrefabGameObjects(prefabRoot), canvasObject),
                "A retained prefab semantic changed while removing GraphicRaycaster at " + canvasPath + ".");

            if (changed)
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);

            return changed;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void VerifyWorldSpacePrefab(
        string prefabPath,
        string canvasPath,
        RenderMode expectedRenderMode,
        int expectedSortingOrder)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            GameObject canvasObject = FindDescendantByPath(prefabRoot, canvasPath);
            Require(canvasObject != null, "Expected Canvas is missing at " + canvasPath + ".");
            Canvas canvas = RequireSingleCanvas(canvasObject, canvasPath);
            ValidateWorldSpaceVisualPrefab(prefabRoot, canvas, prefabPath, canvasPath,
                expectedRenderMode, expectedSortingOrder);
            Require(canvasObject.GetComponents<GraphicRaycaster>().Length == 0,
                "Visual-only Canvas must have zero GraphicRaycasters at " + canvasPath + ".");
            ValidateNoMissingScripts(prefabRoot, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void ValidateWorldSpaceVisualPrefab(
        GameObject prefabRoot,
        Canvas canvas,
        string prefabPath,
        string canvasPath,
        RenderMode expectedRenderMode,
        int expectedSortingOrder)
    {
        Require(canvas.renderMode == expectedRenderMode &&
                canvas.sortingLayerName == "Default" &&
                canvas.sortingOrder == expectedSortingOrder,
            "Canvas policy changed unexpectedly at " + canvasPath + " in " + prefabPath + ".");
        Require(canvas.worldCamera == null,
            "World-space feedback Canvas unexpectedly has an event/render camera at " + canvasPath + ".");
        ValidateVisualOnlyInputSurface(prefabRoot, prefabPath);
        Require(canvas.GetComponents<GraphicRaycaster>().Length <= 1,
            "UNEXPECTED_RAYCASTER_STRUCTURE at " + canvasPath + ".");
    }

    private static bool RemoveOnlyRaycaster(GameObject target, string targetPath)
    {
        GraphicRaycaster[] raycasters = target.GetComponents<GraphicRaycaster>();
        Require(raycasters.Length <= 1, "UNEXPECTED_RAYCASTER_STRUCTURE at " + targetPath + ".");
        if (raycasters.Length == 0)
            return false;

        Undo.DestroyObjectImmediate(raycasters[0]);
        Require(target.GetComponents<GraphicRaycaster>().Length == 0,
            "GraphicRaycaster removal did not persist at " + targetPath + ".");
        return true;
    }

    private static void ValidateVisualOnlyInputSurface(GameObject root, string targetName)
    {
        Require(root.GetComponentsInChildren<Selectable>(true).Length == 0,
            "Visual-only target unexpectedly has a Selectable: " + targetName);
        Require(root.GetComponentsInChildren<EventTrigger>(true).Length == 0,
            "Visual-only target unexpectedly has an EventTrigger: " + targetName);
        Require(root.GetComponentsInChildren<ScrollRect>(true).Length == 0,
            "Visual-only target unexpectedly has a ScrollRect: " + targetName);
        Require(root.GetComponentsInChildren<InputField>(true).Length == 0,
            "Visual-only target unexpectedly has an InputField: " + targetName);
        Require(root.GetComponentsInChildren<TMP_InputField>(true).Length == 0,
            "Visual-only target unexpectedly has a TMP_InputField: " + targetName);

        foreach (Component component in root.GetComponentsInChildren<Component>(true))
        {
            Require(component != null,
                "Missing Script found while validating visual-only target: " + targetName);
            Require(!ImplementsPointerOrSelectionInterface(component),
                "Visual-only target unexpectedly implements a pointer/drag/selection interface: " +
                component.GetType().FullName + " at " + targetName);
        }
    }

    private static bool ImplementsPointerOrSelectionInterface(Component component)
    {
        return component is IPointerClickHandler || component is IPointerDownHandler ||
               component is IPointerUpHandler || component is IPointerEnterHandler ||
               component is IPointerExitHandler || component is IBeginDragHandler ||
               component is IDragHandler || component is IEndDragHandler ||
               component is ISubmitHandler || component is ISelectHandler;
    }

    private static Canvas RequireSingleCanvas(GameObject target, string targetPath)
    {
        Canvas[] canvases = target.GetComponents<Canvas>();
        Require(canvases.Length == 1,
            "Expected exactly one direct Canvas at " + targetPath + ". Actual: " + canvases.Length);
        return canvases[0];
    }

    private static bool CanvasMatches(Canvas canvas, RenderMode renderMode, int sortingOrder, bool overrideSorting)
    {
        return canvas.renderMode == renderMode && canvas.sortingLayerName == "Default" &&
               canvas.sortingOrder == sortingOrder && canvas.overrideSorting == overrideSorting;
    }

    private static GameObject FindExactGameObject(Scene scene, string hierarchyPath)
    {
        GameObject match = null;
        foreach (GameObject gameObject in FindSceneGameObjects(scene))
        {
            if (GetHierarchyPath(gameObject.transform) != hierarchyPath)
                continue;

            Require(match == null,
                "Expected one GameObject at " + hierarchyPath + " in " + scene.path + ".");
            match = gameObject;
        }

        Require(match != null,
            "Expected GameObject is missing at " + hierarchyPath + " in " + scene.path + ".");
        return match;
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
        foreach (GameObject gameObject in FindPrefabGameObjects(root))
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

    private static List<GameObject> FindPrefabGameObjects(GameObject root)
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

    private static string CaptureSnapshot(IEnumerable<GameObject> gameObjects, GameObject raycasterTarget)
    {
        StringBuilder snapshot = new StringBuilder(32768);
        foreach (GameObject gameObject in gameObjects)
        {
            snapshot.Append(GetHierarchyPathWithSiblingIndices(gameObject.transform));
            snapshot.Append('|').Append(gameObject.name);
            snapshot.Append('|').Append(gameObject.activeSelf);
            snapshot.Append('|').Append(gameObject.layer);
            snapshot.Append('|').Append(gameObject.tag);
            snapshot.AppendLine();

            foreach (Component component in gameObject.GetComponents<Component>())
            {
                Require(component != null,
                    "Missing Script found while capturing semantic snapshot at " +
                    GetHierarchyPath(gameObject.transform) + ".");
                if (gameObject == raycasterTarget && component is GraphicRaycaster)
                    continue;

                snapshot.Append(component.GetType().AssemblyQualifiedName);
                snapshot.Append('|').Append(EditorJsonUtility.ToJson(component, false));
                snapshot.AppendLine();
            }
        }

        return snapshot.ToString();
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

    private static void ValidateNoMissingScripts(Scene scene)
    {
        int missingScriptCount = 0;
        foreach (GameObject gameObject in FindSceneGameObjects(scene))
            missingScriptCount += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);

        Require(missingScriptCount == 0,
            "BarScene contains Missing Script components. Actual: " + missingScriptCount);
    }

    private static void ValidateNoMissingScripts(GameObject prefabRoot, string prefabPath)
    {
        int missingScriptCount = 0;
        foreach (GameObject gameObject in FindPrefabGameObjects(prefabRoot))
            missingScriptCount += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);

        Require(missingScriptCount == 0,
            "Prefab contains Missing Script components: " + prefabPath + ". Actual: " + missingScriptCount);
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
