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
/// Gives the Bar-owned ResultToast and DayResult panels explicit nested Canvas
/// ownership without changing the Bar root Canvas, runtime UI code, or
/// Bartending prefab Canvas policy.
/// </summary>
public static class SURLHWACanvasOwnershipMigration_C5_4_2
{
    private const string BarScenePath = "Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity";
    private const string RootCanvasPath = "Canvas";
    private const string ResultToastPath = "Canvas/ResultToastPanel";
    private const string DayResultPath = "Canvas/DayResultPanel";
    private const int ResultToastSortingOrder = 20;
    private const int DayResultSortingOrder = 30;

    private const string MigrationMenuPath =
        "SURLHWA/Rebuild/Migrate/C5.4.2 Define Feedback Canvas Ownership";
    private const string VerificationMenuPath =
        "SURLHWA/Rebuild/Verify/C5.4.2 Feedback Canvas Ownership";

    private static readonly PrefabCanvasTarget[] BartendingCanvasTargets =
    {
        new PrefabCanvasTarget(
            "Assets/_SURLHWA/Prefabs/Bartending/StirMiniGame.prefab",
            "StirMiniGame/UI"),
        new PrefabCanvasTarget(
            "Assets/_SURLHWA/Prefabs/Bartending/ShakeMiniGame.prefab",
            "ShakeMiniGame/UI"),
        new PrefabCanvasTarget(
            "Assets/_SURLHWA/Prefabs/Bartending/BuildMiniGame.prefab",
            "BuildMiniGame/UI"),
        new PrefabCanvasTarget(
            "Assets/_SURLHWA/Prefabs/Bartending/FloatMiniGame.prefab",
            "FloatMiniGame/UI")
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
            Debug.LogError("[SURLHWA C5.4.2] Canvas ownership migration is available only in Edit Mode.");
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
            Debug.LogError("[SURLHWA C5.4.2] Canvas ownership " + operation + " failed: " + exception);
        }
    }

    private static void RunMigration()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,
            "Canvas ownership migration cannot run while entering or using Play Mode.");

        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Scene scene = OpenBarScene();
            PanelTargets targets = FindAndValidateTargets(scene);

            bool resultToastFinal = IsResultToastFinal(targets.ResultToast);
            bool dayResultFinal = IsDayResultFinal(targets.DayResult);
            Require(resultToastFinal == dayResultFinal,
                "Canvas ownership migration found a partial ResultToast/DayResult state. No changes were made.");

            if (resultToastFinal)
            {
                VerifyScene(scene);
                Debug.Log("[SURLHWA C5.4.2][ALREADY_MIGRATED] ResultToast and DayResult already match the required Canvas ownership policy.");
                return;
            }

            ValidateUnmigratedState(targets);
            string retainedSnapshot = CaptureRetainedSemanticSnapshot(scene);

            Canvas resultToastCanvas = Undo.AddComponent<Canvas>(targets.ResultToast);
            ConfigureNestedCanvas(resultToastCanvas, targets.RootCanvasComponent, ResultToastSortingOrder);

            Canvas dayResultCanvas = Undo.AddComponent<Canvas>(targets.DayResult);
            ConfigureNestedCanvas(dayResultCanvas, targets.RootCanvasComponent, DayResultSortingOrder);
            GraphicRaycaster dayResultRaycaster = Undo.AddComponent<GraphicRaycaster>(targets.DayResult);
            Require(dayResultRaycaster.enabled,
                "DayResultPanel GraphicRaycaster was not enabled after creation.");

            Require(retainedSnapshot == CaptureRetainedSemanticSnapshot(scene),
                "A retained Scene semantic changed while adding Canvas ownership components.");

            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene),
                "Unity failed to save Canvas ownership migration: " + BarScenePath);

            Scene savedScene = OpenBarScene();
            VerifyScene(savedScene);
            Debug.Log("[SURLHWA C5.4.2][MIGRATED] ResultToastPanel=20 visual-only; DayResultPanel=30 blocking modal.");
        }
        finally
        {
            RestoreSceneSetup(originalSetup);
        }

        VerifyBartendingPrefabPolicy();
        Debug.Log("[SURLHWA C5.4.2][PASS] Feedback Canvas ownership migration passed.");
    }

    private static void RunVerification()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,
            "Canvas ownership verification cannot run while entering or using Play Mode.");

        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Scene scene = OpenBarScene();
            VerifyScene(scene);
        }
        finally
        {
            RestoreSceneSetup(originalSetup);
        }

        VerifyBartendingPrefabPolicy();
        Debug.Log("[SURLHWA C5.4.2][PASS] Feedback Canvas ownership verification passed.");
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

    private static PanelTargets FindAndValidateTargets(Scene scene)
    {
        GameObject rootCanvas = FindExactGameObject(scene, RootCanvasPath);
        Canvas rootCanvasComponent = rootCanvas.GetComponent<Canvas>();
        Require(rootCanvasComponent != null && rootCanvas.GetComponents<Canvas>().Length == 1,
            "Expected exactly one root Canvas at " + RootCanvasPath + ".");
        Require(rootCanvasComponent.renderMode == RenderMode.ScreenSpaceOverlay,
            "Bar root Canvas render mode changed unexpectedly: " + rootCanvasComponent.renderMode);
        Require(rootCanvasComponent.sortingLayerName == "Default" && rootCanvasComponent.sortingOrder == 0,
            "Bar root Canvas sorting policy changed unexpectedly.");
        Require(rootCanvas.GetComponents<GraphicRaycaster>().Length == 1,
            "Expected one root GraphicRaycaster at " + RootCanvasPath + ".");

        GameObject resultToast = FindExactGameObject(scene, ResultToastPath);
        Require(resultToast.activeSelf,
            "ResultToastPanel activeSelf changed unexpectedly.");
        Require(resultToast.GetComponent<ResultToastController>() != null,
            "ResultToastPanel controller is missing.");
        Require(resultToast.GetComponents<CanvasGroup>().Length == 1,
            "Expected one CanvasGroup on ResultToastPanel.");
        Require(resultToast.GetComponent<Image>() != null,
            "ResultToastPanel background Image is missing.");

        GameObject dayResult = FindExactGameObject(scene, DayResultPath);
        Require(!dayResult.activeSelf,
            "DayResultPanel activeSelf changed unexpectedly.");
        Require(dayResult.GetComponent<DayResultPanelController>() != null,
            "DayResultPanel controller is missing.");
        Image dayResultBackground = dayResult.GetComponent<Image>();
        Require(dayResultBackground != null && dayResultBackground.raycastTarget,
            "DayResultPanel full-screen background Image must remain a raycast target.");
        RectTransform dayResultRect = dayResult.GetComponent<RectTransform>();
        Require(dayResultRect != null && dayResultRect.anchorMin == Vector2.zero &&
                dayResultRect.anchorMax == Vector2.one,
            "DayResultPanel must retain its full-screen anchors.");
        Require(dayResult.GetComponentsInChildren<Button>(true).Length >= 2,
            "DayResultPanel must retain its button controls.");

        Require(FindSceneComponents<EventSystem>(scene).Count == 1,
            "BarScene must retain exactly one EventSystem.");
        ValidateNoMissingScripts(scene);
        return new PanelTargets(rootCanvas, rootCanvasComponent, resultToast, dayResult);
    }

    private static void ValidateUnmigratedState(PanelTargets targets)
    {
        Require(targets.ResultToast.GetComponents<Canvas>().Length == 0,
            "ResultToastPanel unexpectedly already has an independent Canvas.");
        Require(targets.ResultToast.GetComponents<GraphicRaycaster>().Length == 0,
            "ResultToastPanel unexpectedly already has a GraphicRaycaster.");
        Require(targets.DayResult.GetComponents<Canvas>().Length == 0,
            "DayResultPanel unexpectedly already has an independent Canvas.");
        Require(targets.DayResult.GetComponents<GraphicRaycaster>().Length == 0,
            "DayResultPanel unexpectedly already has a GraphicRaycaster.");
    }

    private static bool IsResultToastFinal(GameObject resultToast)
    {
        Canvas[] canvases = resultToast.GetComponents<Canvas>();
        return canvases.Length == 1 &&
               CanvasMatchesTarget(canvases[0], ResultToastSortingOrder) &&
               resultToast.GetComponents<GraphicRaycaster>().Length == 0;
    }

    private static bool IsDayResultFinal(GameObject dayResult)
    {
        Canvas[] canvases = dayResult.GetComponents<Canvas>();
        GraphicRaycaster[] raycasters = dayResult.GetComponents<GraphicRaycaster>();
        return canvases.Length == 1 &&
               CanvasMatchesTarget(canvases[0], DayResultSortingOrder) &&
               raycasters.Length == 1 && raycasters[0].enabled;
    }

    private static void ConfigureNestedCanvas(Canvas canvas, Canvas rootCanvas, int sortingOrder)
    {
        Require(canvas != null, "Added Canvas component is missing.");
        SerializedObject canvasObject = new SerializedObject(canvas);
        SerializedProperty renderMode = canvasObject.FindProperty("m_RenderMode");
        SerializedProperty sortingLayer = canvasObject.FindProperty("m_SortingLayerID");
        SerializedProperty overrideSorting = canvasObject.FindProperty("m_OverrideSorting");
        SerializedProperty canvasSortingOrder = canvasObject.FindProperty("m_SortingOrder");
        Require(renderMode != null && sortingLayer != null && overrideSorting != null &&
                canvasSortingOrder != null,
            "Required Canvas serialized properties are unavailable.");

        renderMode.intValue = (int)rootCanvas.renderMode;
        sortingLayer.intValue = rootCanvas.sortingLayerID;
        overrideSorting.boolValue = true;
        canvasSortingOrder.intValue = sortingOrder;
        Require(canvasObject.ApplyModifiedPropertiesWithoutUndo(),
            "Unity did not apply the nested Canvas serialized settings.");
        Require(CanvasMatchesTarget(canvas, sortingOrder),
            "Nested Canvas configuration did not persist in serialization. Actual: " +
            DescribeCanvasSerialization(canvas) + ".");
    }

    private static bool CanvasMatchesTarget(Canvas canvas, int sortingOrder)
    {
        SerializedObject canvasObject = new SerializedObject(canvas);
        SerializedProperty renderMode = canvasObject.FindProperty("m_RenderMode");
        SerializedProperty sortingLayer = canvasObject.FindProperty("m_SortingLayerID");
        SerializedProperty overrideSorting = canvasObject.FindProperty("m_OverrideSorting");
        SerializedProperty canvasSortingOrder = canvasObject.FindProperty("m_SortingOrder");
        return renderMode != null && sortingLayer != null && overrideSorting != null &&
               canvasSortingOrder != null &&
               renderMode.intValue == (int)RenderMode.ScreenSpaceOverlay &&
               sortingLayer.intValue == SortingLayer.NameToID("Default") &&
               overrideSorting.boolValue &&
               canvasSortingOrder.intValue == sortingOrder;
    }

    private static string DescribeCanvasSerialization(Canvas canvas)
    {
        SerializedObject canvasObject = new SerializedObject(canvas);
        return "renderMode=" + canvasObject.FindProperty("m_RenderMode").intValue +
               ", sortingLayerID=" + canvasObject.FindProperty("m_SortingLayerID").intValue +
               ", overrideSorting=" + canvasObject.FindProperty("m_OverrideSorting").boolValue +
               ", sortingOrder=" + canvasObject.FindProperty("m_SortingOrder").intValue;
    }

    private static void VerifyScene(Scene scene)
    {
        PanelTargets targets = FindAndValidateTargets(scene);
        Require(IsResultToastFinal(targets.ResultToast),
            "ResultToastPanel does not match its required Canvas ownership policy.");
        Require(IsDayResultFinal(targets.DayResult),
            "DayResultPanel does not match its required Canvas ownership policy.");

        CanvasGroup toastGroup = targets.ResultToast.GetComponent<CanvasGroup>();
        Require(toastGroup != null,
            "ResultToastPanel CanvasGroup is missing.");

        Debug.Log("[SURLHWA C5.4.2][VERIFIED] " + ResultToastPath +
                  " Canvas=20, overrideSorting=true, GraphicRaycaster=0.");
        Debug.Log("[SURLHWA C5.4.2][VERIFIED] " + DayResultPath +
                  " Canvas=30, overrideSorting=true, GraphicRaycaster=1.");
    }

    private static void VerifyBartendingPrefabPolicy()
    {
        foreach (PrefabCanvasTarget target in BartendingCanvasTargets)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(target.PrefabPath);
            Require(prefab != null, "Bartending prefab is missing: " + target.PrefabPath);

            Canvas[] canvases = prefab.GetComponentsInChildren<Canvas>(true);
            Require(canvases.Length == 1,
                "Expected exactly one Canvas in " + target.PrefabPath + ". Actual: " + canvases.Length);
            Canvas canvas = canvases[0];
            Require(GetHierarchyPath(canvas.transform) == target.CanvasPath &&
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay &&
                    canvas.sortingLayerName == "Default" &&
                    canvas.sortingOrder == 10 &&
                    canvas.GetComponents<GraphicRaycaster>().Length == 1,
                "Bartending Canvas policy changed unexpectedly in " + target.PrefabPath + ".");
        }
    }

    private static string CaptureRetainedSemanticSnapshot(Scene scene)
    {
        StringBuilder snapshot = new StringBuilder(65536);
        GameObject resultToast = FindExactGameObject(scene, ResultToastPath);
        GameObject dayResult = FindExactGameObject(scene, DayResultPath);
        foreach (GameObject gameObject in FindSceneGameObjects(scene))
        {
            snapshot.Append(GetHierarchyPathWithSiblingIndices(gameObject.transform));
            snapshot.Append('|').Append(gameObject.name);
            snapshot.Append('|').Append(gameObject.activeSelf);
            snapshot.Append('|').Append(gameObject.layer);
            snapshot.Append('|').Append(gameObject.tag);
            snapshot.AppendLine();

            bool targetPanel = gameObject == resultToast || gameObject == dayResult;
            foreach (Component component in gameObject.GetComponents<Component>())
            {
                Require(component != null,
                    "Missing Script found while capturing Scene semantics at " +
                    GetHierarchyPath(gameObject.transform) + ".");
                if (targetPanel && (component is Canvas || component is GraphicRaycaster))
                    continue;

                snapshot.Append(component.GetType().AssemblyQualifiedName);
                snapshot.Append('|').Append(EditorJsonUtility.ToJson(component, false));
                snapshot.AppendLine();
            }
        }

        return snapshot.ToString();
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

    private static void ValidateNoMissingScripts(Scene scene)
    {
        int missingScriptCount = 0;
        foreach (GameObject gameObject in FindSceneGameObjects(scene))
            missingScriptCount += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);

        Require(missingScriptCount == 0,
            "BarScene contains Missing Script components. Actual: " + missingScriptCount);
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
        for (int index = 0; index < transform.childCount; index++)
            AddHierarchyDepthFirst(transform.GetChild(index), gameObjects);
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

    private sealed class PanelTargets
    {
        public readonly GameObject RootCanvas;
        public readonly Canvas RootCanvasComponent;
        public readonly GameObject ResultToast;
        public readonly GameObject DayResult;

        public PanelTargets(
            GameObject rootCanvas,
            Canvas rootCanvasComponent,
            GameObject resultToast,
            GameObject dayResult)
        {
            RootCanvas = rootCanvas;
            RootCanvasComponent = rootCanvasComponent;
            ResultToast = resultToast;
            DayResult = dayResult;
        }
    }

    private sealed class PrefabCanvasTarget
    {
        public readonly string PrefabPath;
        public readonly string CanvasPath;

        public PrefabCanvasTarget(string prefabPath, string canvasPath)
        {
            PrefabPath = prefabPath;
            CanvasPath = canvasPath;
        }
    }
}
