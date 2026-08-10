using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-time C2 Scene wiring tool. It adds the BAR_LOCAL coordinator to the existing
/// OrderManager GameObject without creating or reorganizing any Scene hierarchy.
/// </summary>
internal static class SURLHWABartendingBoundaryMigration_C2
{
    private const string CanonicalBarScenePath = "Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity";
    private const string MigrationMenuPath = "SURLHWA/Rebuild/Migrate/C2 Bartending Boundary";
    private const string VerificationMenuPath = "SURLHWA/Rebuild/Verify/C2 Bartending Boundary";

    [MenuItem(MigrationMenuPath)]
    private static void AttachCoordinator()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[SURLHWA C2] Coordinator wiring is blocked while Play Mode is active.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != CanonicalBarScenePath)
        {
            Debug.LogError(
                "[SURLHWA C2] Open the canonical BarScene before running C2. Active Scene: " +
                activeScene.path);
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "SURLHWA C2 - Bartending Boundary",
                "Add BartendingFlowCoordinator to the existing OrderManager GameObject and wire it to MiniGameManager?\n\n" +
                "No GameObject hierarchy, Prefab, ScriptableObject, or MiniGame implementation will be changed.",
                "Wire Coordinator",
                "Cancel"))
        {
            return;
        }

        try
        {
            OrderManager orderManager = FindSingleActiveObject<OrderManager>("OrderManager");
            MiniGameManager miniGameManager = FindSingleActiveObject<MiniGameManager>("MiniGameManager");

            BartendingFlowCoordinator coordinator = orderManager.GetComponent<BartendingFlowCoordinator>();
            if (coordinator == null)
                coordinator = Undo.AddComponent<BartendingFlowCoordinator>(orderManager.gameObject);

            SetObjectReference(coordinator, "miniGameManager", miniGameManager);
            SetObjectReference(orderManager, "bartendingFlowCoordinator", coordinator);

            EditorUtility.SetDirty(coordinator);
            EditorUtility.SetDirty(orderManager);
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            ValidateCanonicalBarScene(activeScene);
            Debug.Log(
                "[SURLHWA C2] Complete. BartendingFlowCoordinator is attached to '" +
                orderManager.gameObject.name + "' and wired to MiniGameManager.");
        }
        catch (System.Exception exception)
        {
            Debug.LogError("[SURLHWA C2] Coordinator wiring failed: " + exception.Message);
        }
    }

    [MenuItem(VerificationMenuPath)]
    private static void VerifyCoordinator()
    {
        try
        {
            Scene activeScene = SceneManager.GetActiveScene();
            ValidateCanonicalBarScene(activeScene);
            Debug.Log("[SURLHWA C2] Bartending boundary wiring validation passed.");
        }
        catch (System.Exception exception)
        {
            Debug.LogError("[SURLHWA C2] Bartending boundary wiring validation failed: " + exception.Message);
        }
    }

    private static void ValidateCanonicalBarScene(Scene activeScene)
    {
        Require(activeScene.path == CanonicalBarScenePath,
            "Active Scene is not the canonical BarScene. Actual: " + activeScene.path);

        OrderManager orderManager = FindSingleActiveObject<OrderManager>("OrderManager");
        MiniGameManager miniGameManager = FindSingleActiveObject<MiniGameManager>("MiniGameManager");
        BartendingFlowCoordinator[] coordinators = Object.FindObjectsByType<BartendingFlowCoordinator>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        Require(coordinators.Length == 1,
            "Expected exactly one active BartendingFlowCoordinator. Actual: " + coordinators.Length);

        BartendingFlowCoordinator coordinator = coordinators[0];
        Require(coordinator.gameObject == orderManager.gameObject,
            "BartendingFlowCoordinator must share the existing OrderManager GameObject.");
        Require(GetObjectReference(coordinator, "miniGameManager") == miniGameManager,
            "BartendingFlowCoordinator MiniGameManager reference is not wired.");
        Require(GetObjectReference(orderManager, "bartendingFlowCoordinator") == coordinator,
            "OrderManager BartendingFlowCoordinator reference is not wired.");
    }

    private static T FindSingleActiveObject<T>(string displayName) where T : Object
    {
        T[] objects = Object.FindObjectsByType<T>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        Require(objects.Length == 1,
            "Expected exactly one active " + displayName + ". Actual: " + objects.Length);

        return objects[0];
    }

    private static void SetObjectReference(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        Require(property != null,
            "Serialized property was not found: " + target.GetType().Name + "." + propertyName);

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Object GetObjectReference(Object target, string propertyName)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        Require(property != null,
            "Serialized property was not found: " + target.GetType().Name + "." + propertyName);

        return property.objectReferenceValue;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new System.InvalidOperationException(message);
    }
}
