using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// C4.2 authoring and verification utility. It wires only the BarScene camera
/// reference required by the BAR_LOCAL coordinator; it never wires a Scene-local
/// Bartending runner through a serialized cross-scene reference.
/// </summary>
internal static class SURLHWABartendingCoordinatorPreparation_C4_2
{
    private const string BarScenePath = "Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity";
    private const string BartendingScenePath = "Assets/_SURLHWA/Scenes/Gameplay/BartendingScene.unity";

    private const string MigrationMenuPath = "SURLHWA/Rebuild/Migrate/C4.2 Coordinator Preparation";
    private const string VerificationMenuPath = "SURLHWA/Rebuild/Verify/C4.2 Coordinator Preparation";
    private const string ProbeMenuPath = "SURLHWA/Rebuild Tests/C4.2 Coordinator Additive Smoke";

    [MenuItem(MigrationMenuPath)]
    private static void PrepareCoordinator()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[SURLHWA C4.2] Coordinator preparation is blocked while Play Mode is active.");
            return;
        }

        if (!TryGetActiveBarScene(out Scene barScene))
            return;

        if (!EditorUtility.DisplayDialog(
                "SURLHWA C4.2 - Coordinator Preparation",
                "Wire the existing BartendingFlowCoordinator to the existing BarScene Main Camera?\n\n" +
                "This changes one serialized camera reference only. It does not create objects, change hierarchy, " +
                "modify MiniGameManager, or connect to the BartendingScene runner.",
                "Wire Bar Camera",
                "Cancel"))
        {
            return;
        }

        try
        {
            BartendingFlowCoordinator coordinator = FindSingleSceneComponent<BartendingFlowCoordinator>(barScene);
            Camera barMainCamera = FindSingleBarMainCamera(barScene);
            ValidateBarSceneCoordinator(barScene, coordinator, barMainCamera, false);

            if (GetObjectReference(coordinator, "barMainCamera") != barMainCamera)
            {
                SetObjectReference(coordinator, "barMainCamera", barMainCamera);
                EditorUtility.SetDirty(coordinator);
                EditorSceneManager.MarkSceneDirty(barScene);
                EditorSceneManager.SaveScene(barScene);
            }

            ValidateBarSceneCoordinator(barScene, coordinator, barMainCamera, true);
            ValidateBartendingSceneRunnerAsset();

            Debug.Log(
                "[SURLHWA C4.2] Coordinator preparation passed. " +
                "BartendingFlowCoordinator now references the existing BarScene Main Camera only."
            );
        }
        catch (Exception exception)
        {
            Debug.LogError("[SURLHWA C4.2] Coordinator preparation failed: " + exception.Message);
        }
    }

    [MenuItem(VerificationMenuPath)]
    private static void VerifyCoordinatorPreparation()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[SURLHWA C4.2] Coordinator verification is available only in Edit Mode.");
            return;
        }

        if (!TryGetActiveBarScene(out Scene barScene))
            return;

        try
        {
            BartendingFlowCoordinator coordinator = FindSingleSceneComponent<BartendingFlowCoordinator>(barScene);
            Camera barMainCamera = FindSingleBarMainCamera(barScene);
            ValidateBarSceneCoordinator(barScene, coordinator, barMainCamera, true);
            ValidateBartendingSceneRunnerAsset();

            Debug.Log("[SURLHWA C4.2] Coordinator preparation verification passed.");
        }
        catch (Exception exception)
        {
            Debug.LogError("[SURLHWA C4.2] Coordinator preparation verification failed: " + exception.Message);
        }
    }

    [MenuItem(ProbeMenuPath)]
    private static void RunCoordinatorAdditiveSmoke()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[SURLHWA C4.2][NOT_IN_PLAY_MODE] Enter Play Mode with BarScene active before running the probe.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != BarScenePath)
        {
            Debug.LogError(
                "[SURLHWA C4.2][WRONG_ACTIVE_SCENE] Expected BarScene active. Actual: " +
                activeScene.path);
            return;
        }

        try
        {
            BartendingFlowCoordinator coordinator = FindSingleSceneComponent<BartendingFlowCoordinator>(activeScene);
            if (coordinator.IsBartendingActive)
            {
                Debug.LogError("[SURLHWA C4.2][COORDINATOR_ALREADY_ACTIVE] Probe was not started.");
                return;
            }

            bool accepted = coordinator.BeginAdditiveLifecycleProbe((success, resultCode) =>
            {
                if (success)
                {
                    Debug.Log("[SURLHWA C4.2][PASS] Coordinator additive smoke completed with active state restored.");
                }
                else
                {
                    Debug.LogError("[SURLHWA C4.2][" + resultCode + "] Coordinator additive smoke failed.");
                }
            });

            if (!accepted)
                Debug.LogError("[SURLHWA C4.2] Coordinator rejected the additive smoke probe.");
        }
        catch (Exception exception)
        {
            Debug.LogError("[SURLHWA C4.2] Coordinator additive smoke could not start: " + exception.Message);
        }
    }

    private static bool TryGetActiveBarScene(out Scene barScene)
    {
        barScene = SceneManager.GetActiveScene();
        if (barScene.path == BarScenePath && barScene.isLoaded)
            return true;

        Debug.LogError(
            "[SURLHWA C4.2] Open the canonical BarScene before running this menu. Active Scene: " +
            barScene.path);
        return false;
    }

    private static void ValidateBarSceneCoordinator(
        Scene barScene,
        BartendingFlowCoordinator coordinator,
        Camera barMainCamera,
        bool requireCameraReference)
    {
        Require(barScene.path == BarScenePath && barScene.isLoaded,
            "Active Scene is not the loaded canonical BarScene.");
        Require(coordinator.gameObject.scene == barScene,
            "BartendingFlowCoordinator must belong to BarScene.");
        Require(coordinator.GetComponent<OrderManager>() != null,
            "BartendingFlowCoordinator must remain on the existing OrderManager GameObject.");
        Require(barMainCamera.gameObject.scene == barScene,
            "Bar Main Camera must belong to BarScene.");
        Require(barMainCamera.CompareTag("MainCamera"),
            "Bar Main Camera must retain the MainCamera tag.");
        Require(barMainCamera.GetComponents<AudioListener>().Length == 1,
            "Bar Main Camera must have exactly one AudioListener.");

        MiniGameManager legacyRunner = FindSingleSceneComponent<MiniGameManager>(barScene);
        Require(legacyRunner.gameObject.scene == barScene,
            "Legacy MiniGameManager must belong to BarScene.");
        Require(GetObjectReference(coordinator, "miniGameManager") == legacyRunner,
            "Coordinator legacy MiniGameManager reference is not the BarScene runner.");

        if (requireCameraReference)
        {
            Require(GetObjectReference(coordinator, "barMainCamera") == barMainCamera,
                "Coordinator barMainCamera reference is not wired to the BarScene Main Camera.");
        }
    }

    private static void ValidateBartendingSceneRunnerAsset()
    {
        Scene existingScene = SceneManager.GetSceneByPath(BartendingScenePath);
        bool closeAfterValidation = !existingScene.isLoaded;
        Scene bartendingScene = existingScene;

        try
        {
            if (closeAfterValidation)
                bartendingScene = EditorSceneManager.OpenScene(BartendingScenePath, OpenSceneMode.Additive);

            Require(bartendingScene.IsValid() && bartendingScene.isLoaded,
                "BartendingScene could not be opened for validation.");

            MiniGameManager runner = FindSingleSceneComponent<MiniGameManager>(bartendingScene);
            Require(runner.gameObject.scene == bartendingScene,
                "BartendingScene runner does not belong to BartendingScene.");
        }
        finally
        {
            if (closeAfterValidation && bartendingScene.IsValid() && bartendingScene.isLoaded)
                EditorSceneManager.CloseScene(bartendingScene, true);
        }
    }

    private static Camera FindSingleBarMainCamera(Scene scene)
    {
        List<Camera> matches = new List<Camera>();
        foreach (Camera camera in FindSceneComponents<Camera>(scene))
        {
            if (camera.CompareTag("MainCamera"))
                matches.Add(camera);
        }

        Require(matches.Count == 1,
            "Expected exactly one BarScene MainCamera-tagged Camera. Actual: " + matches.Count);
        return matches[0];
    }

    private static T FindSingleSceneComponent<T>(Scene scene) where T : Component
    {
        List<T> matches = FindSceneComponents<T>(scene);
        Require(matches.Count == 1,
            "Expected exactly one " + typeof(T).Name + " in " + scene.path + ". Actual: " + matches.Count);
        return matches[0];
    }

    private static List<T> FindSceneComponents<T>(Scene scene) where T : Component
    {
        List<T> matches = new List<T>();
        if (!scene.IsValid() || !scene.isLoaded)
            return matches;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (T component in root.GetComponentsInChildren<T>(true))
            {
                if (component != null && component.gameObject.scene == scene)
                    matches.Add(component);
            }
        }

        return matches;
    }

    private static UnityEngine.Object GetObjectReference(UnityEngine.Object target, string propertyName)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        Require(property != null,
            "Serialized property was not found: " + target.GetType().Name + "." + propertyName);
        return property.objectReferenceValue;
    }

    private static void SetObjectReference(
        UnityEngine.Object target,
        string propertyName,
        UnityEngine.Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        Require(property != null,
            "Serialized property was not found: " + target.GetType().Name + "." + propertyName);
        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
