using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Assigns the existing inactive DayResultPanelController to the active
/// CustomerSpawner-owned BusinessDayManager in the two Bar scenes. This is a
/// serialized-wiring migration only; it does not change DayResult runtime code.
/// </summary>
public static class SURLHWADayResultWiringMigration_C5_5_5A
{
    private const string BarScenePath = "Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity";
    private const string LayoutTestScenePath = "Assets/BarScene_LayoutTest.unity";
    private const string CustomerSpawnerPath = "Manager/CustomerSpawner";
    private const string RootCanvasPath = "Canvas";
    private const string DayResultPath = "Canvas/DayResultPanel";
    private const string LayoutTestExistingManagerPath = "Manager/BusinessDayManager";
    private const string ResultPanelPropertyName = "resultPanel";
    private const int DayResultSortingOrder = 30;

    private const string MigrationMenuPath =
        "SURLHWA/Rebuild/Migrate/C5.5.5A Wire Serialized DayResult Panel";
    private const string VerificationMenuPath =
        "SURLHWA/Rebuild/Verify/C5.5.5A DayResult Serialized Wiring";

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
            Debug.LogError("[SURLHWA C5.5.5A] DayResult wiring migration is available only in Edit Mode.");
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
            Debug.LogError("[SURLHWA C5.5.5A] DayResult wiring " + operation + " failed: " + exception);
        }
    }

    private static void RunMigration()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,
            "DayResult wiring migration cannot run while entering or using Play Mode.");

        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        bool changedAny = false;

        try
        {
            foreach (string scenePath in TargetScenePaths)
            {
                Scene scene = OpenTargetScene(scenePath);
                SceneTargets targets = ResolveSceneTargets(scene, scenePath == LayoutTestScenePath);

                if (!AssignExpectedResultPanel(targets.CustomerSpawnerManager, targets.DayResultController))
                {
                    Debug.Log("[SURLHWA C5.5.5A][ALREADY_MIGRATED] " + scenePath +
                              " already serializes CustomerSpawner.resultPanel to " + DayResultPath + ".");
                    ValidateFinalSceneState(scene, scenePath == LayoutTestScenePath);
                    continue;
                }

                EditorSceneManager.MarkSceneDirty(scene);
                Require(EditorSceneManager.SaveScene(scene),
                    "Unity failed to save DayResult wiring migration: " + scenePath);

                Scene savedScene = OpenTargetScene(scenePath);
                ValidateFinalSceneState(savedScene, scenePath == LayoutTestScenePath);
                changedAny = true;
                Debug.Log("[SURLHWA C5.5.5A][MIGRATED] " + scenePath +
                          " serialized CustomerSpawner.resultPanel to " + DayResultPath + ".");
            }

            VerifyFinalState();
        }
        finally
        {
            RestoreSceneSetup(originalSetup);
        }

        Debug.Log(changedAny
            ? "[SURLHWA C5.5.5A][PASS] DayResult serialized wiring migration completed."
            : "[SURLHWA C5.5.5A][ALREADY_MIGRATED] DayResult serialized wiring already matches the final state.");
    }

    private static void RunVerification()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,
            "DayResult wiring verification cannot run while entering or using Play Mode.");

        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            VerifyFinalState();
        }
        finally
        {
            RestoreSceneSetup(originalSetup);
        }

        Debug.Log("[SURLHWA C5.5.5A][PASS] DayResult serialized wiring verification passed.");
    }

    private static void VerifyFinalState()
    {
        foreach (string scenePath in TargetScenePaths)
        {
            Scene scene = OpenTargetScene(scenePath);
            ValidateFinalSceneState(scene, scenePath == LayoutTestScenePath);
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

    private static SceneTargets ResolveSceneTargets(Scene scene, bool isLayoutTest)
    {
        GameObject customerSpawner = FindExactGameObject(scene, CustomerSpawnerPath);
        BusinessDayManager customerSpawnerManager = RequireSingleComponent<BusinessDayManager>(
            customerSpawner,
            CustomerSpawnerPath);

        GameObject dayResult = FindExactGameObject(scene, DayResultPath);
        DayResultPanelController dayResultController = RequireSingleComponent<DayResultPanelController>(
            dayResult,
            DayResultPath);

        ValidateDayResultPanel(dayResult, dayResultController, scene, isLayoutTest);

        BusinessDayManager existingLayoutTestManager = null;
        if (isLayoutTest)
        {
            GameObject layoutTestManagerObject = FindExactGameObject(scene, LayoutTestExistingManagerPath);
            existingLayoutTestManager = RequireSingleComponent<BusinessDayManager>(
                layoutTestManagerObject,
                LayoutTestExistingManagerPath);
            RequireAssignedResultPanel(existingLayoutTestManager, dayResultController,
                LayoutTestExistingManagerPath + "/BusinessDayManager");
        }

        ValidateAssignableResultPanel(customerSpawnerManager, dayResultController,
            CustomerSpawnerPath + "/BusinessDayManager");
        ValidateNoMissingScripts(scene);

        return new SceneTargets(customerSpawnerManager, dayResultController, existingLayoutTestManager);
    }

    private static bool AssignExpectedResultPanel(
        BusinessDayManager customerSpawnerManager,
        DayResultPanelController dayResultController)
    {
        SerializedObject serializedManager = new SerializedObject(customerSpawnerManager);
        SerializedProperty resultPanel = serializedManager.FindProperty(ResultPanelPropertyName);
        Require(resultPanel != null && resultPanel.propertyType == SerializedPropertyType.ObjectReference,
            "BusinessDayManager source no longer exposes the serialized resultPanel field.");

        if (resultPanel.objectReferenceValue == dayResultController)
            return false;

        Require(resultPanel.objectReferenceValue == null,
            "CustomerSpawner BusinessDayManager has an unexpected non-null resultPanel reference.");

        resultPanel.objectReferenceValue = dayResultController;
        Require(serializedManager.ApplyModifiedProperties(),
            "Unity did not apply the expected BusinessDayManager.resultPanel assignment.");
        return true;
    }

    private static void ValidateFinalSceneState(Scene scene, bool isLayoutTest)
    {
        SceneTargets targets = ResolveSceneTargets(scene, isLayoutTest);
        RequireAssignedResultPanel(targets.CustomerSpawnerManager, targets.DayResultController,
            CustomerSpawnerPath + "/BusinessDayManager");

        if (isLayoutTest)
        {
            Require(targets.ExistingLayoutTestManager != null,
                "LayoutTest existing BusinessDayManager is missing.");
            RequireAssignedResultPanel(targets.ExistingLayoutTestManager, targets.DayResultController,
                LayoutTestExistingManagerPath + "/BusinessDayManager");
        }
    }

    private static void ValidateDayResultPanel(
        GameObject dayResult,
        DayResultPanelController dayResultController,
        Scene scene,
        bool isLayoutTest)
    {
        Require(!dayResult.activeSelf,
            DayResultPath + " must remain initially inactive.");

        if (isLayoutTest)
        {
            Require(dayResult.GetComponentsInChildren<Canvas>(true).Length == 0 &&
                    dayResult.GetComponentsInChildren<GraphicRaycaster>(true).Length == 0,
                DayResultPath + " in LayoutTest must retain its inherited root-Canvas ownership.");

            GameObject rootCanvas = FindExactGameObject(scene, RootCanvasPath);
            Canvas canvas = RequireSingleComponent<Canvas>(rootCanvas, RootCanvasPath);
            Require(canvas.renderMode == RenderMode.ScreenSpaceOverlay &&
                    !canvas.overrideSorting &&
                    canvas.sortingLayerName == "Default" &&
                    canvas.sortingOrder == 0 &&
                    rootCanvas.GetComponents<GraphicRaycaster>().Length == 1,
                "LayoutTest root Canvas ownership changed unexpectedly.");
        }
        else
        {
            Canvas canvas = RequireSingleComponent<Canvas>(dayResult, DayResultPath);
            Require(canvas.renderMode == RenderMode.ScreenSpaceOverlay &&
                    canvas.overrideSorting &&
                    canvas.sortingLayerName == "Default" &&
                    canvas.sortingOrder == DayResultSortingOrder,
                DayResultPath + " Canvas ownership policy changed unexpectedly.");
            Require(dayResult.GetComponents<GraphicRaycaster>().Length == 1,
                DayResultPath + " must retain exactly one GraphicRaycaster.");
        }

        SerializedObject serializedController = new SerializedObject(dayResultController);
        Require(serializedController.FindProperty("panelRoot")?.objectReferenceValue == dayResult,
            "DayResultPanelController.panelRoot must remain wired to " + DayResultPath + ".");
        Require(serializedController.FindProperty("titleText")?.objectReferenceValue != null &&
                serializedController.FindProperty("reputationText")?.objectReferenceValue != null &&
                serializedController.FindProperty("customerCountText")?.objectReferenceValue != null &&
                serializedController.FindProperty("unlockedRecipesText")?.objectReferenceValue != null &&
                serializedController.FindProperty("restartButton")?.objectReferenceValue != null &&
                serializedController.FindProperty("closeButton")?.objectReferenceValue != null &&
                serializedController.FindProperty("codexPanel")?.objectReferenceValue != null,
            "DayResultPanelController serialized UI references must remain valid.");
    }

    private static void ValidateAssignableResultPanel(
        BusinessDayManager manager,
        DayResultPanelController expectedPanel,
        string ownerPath)
    {
        SerializedObject serializedManager = new SerializedObject(manager);
        SerializedProperty resultPanel = serializedManager.FindProperty(ResultPanelPropertyName);
        Require(resultPanel != null && resultPanel.propertyType == SerializedPropertyType.ObjectReference,
            ownerPath + " no longer exposes serialized resultPanel.");
        Require(resultPanel.objectReferenceValue == null || resultPanel.objectReferenceValue == expectedPanel,
            ownerPath + " has an unexpected resultPanel reference.");
    }

    private static void RequireAssignedResultPanel(
        BusinessDayManager manager,
        DayResultPanelController expectedPanel,
        string ownerPath)
    {
        SerializedObject serializedManager = new SerializedObject(manager);
        SerializedProperty resultPanel = serializedManager.FindProperty(ResultPanelPropertyName);
        Require(resultPanel != null && resultPanel.objectReferenceValue == expectedPanel,
            ownerPath + " resultPanel must reference " + DayResultPath + ".");
    }

    private static GameObject FindExactGameObject(Scene scene, string hierarchyPath)
    {
        GameObject result = null;
        foreach (GameObject gameObject in FindSceneGameObjects(scene))
        {
            if (GetHierarchyPath(gameObject.transform) != hierarchyPath)
                continue;

            Require(result == null,
                "Expected one GameObject at " + hierarchyPath + " in " + scene.path + ".");
            result = gameObject;
        }

        Require(result != null,
            "Expected GameObject is missing at " + hierarchyPath + " in " + scene.path + ".");
        return result;
    }

    private static T RequireSingleComponent<T>(GameObject gameObject, string hierarchyPath)
        where T : Component
    {
        T[] components = gameObject.GetComponents<T>();
        Require(components.Length == 1 && components[0] != null,
            "Expected exactly one " + typeof(T).Name + " at " + hierarchyPath +
            ". Actual: " + components.Length);
        return components[0];
    }

    private static List<GameObject> FindSceneGameObjects(Scene scene)
    {
        List<GameObject> result = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
            CollectHierarchy(root.transform, result);
        return result;
    }

    private static void CollectHierarchy(Transform transform, List<GameObject> result)
    {
        result.Add(transform.gameObject);
        for (int index = 0; index < transform.childCount; index++)
            CollectHierarchy(transform.GetChild(index), result);
    }

    private static string GetHierarchyPath(Transform transform)
    {
        Stack<string> parts = new Stack<string>();
        while (transform != null)
        {
            parts.Push(transform.name);
            transform = transform.parent;
        }

        return string.Join("/", parts);
    }

    private static void ValidateNoMissingScripts(Scene scene)
    {
        foreach (GameObject gameObject in FindSceneGameObjects(scene))
        {
            foreach (Component component in gameObject.GetComponents<Component>())
            {
                Require(component != null,
                    "Missing Script found at " + GetHierarchyPath(gameObject.transform) +
                    " in " + scene.path + ".");
            }
        }
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

    private sealed class SceneTargets
    {
        public readonly BusinessDayManager CustomerSpawnerManager;
        public readonly DayResultPanelController DayResultController;
        public readonly BusinessDayManager ExistingLayoutTestManager;

        public SceneTargets(
            BusinessDayManager customerSpawnerManager,
            DayResultPanelController dayResultController,
            BusinessDayManager existingLayoutTestManager)
        {
            CustomerSpawnerManager = customerSpawnerManager;
            DayResultController = dayResultController;
            ExistingLayoutTestManager = existingLayoutTestManager;
        }
    }
}
