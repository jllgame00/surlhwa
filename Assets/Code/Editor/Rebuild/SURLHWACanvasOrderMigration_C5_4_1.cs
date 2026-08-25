using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Normalizes the direct Canvas on each production Bartending prefab to the
/// C5.4 Bartending sorting tier without changing runtime Canvas ownership.
/// </summary>
public static class SURLHWACanvasOrderMigration_C5_4_1
{
    private const int TargetSortingOrder = 10;
    private const string MigrationMenuPath =
        "SURLHWA/Rebuild/Migrate/C5.4.1 Normalize Bartending Canvas Order";
    private const string VerificationMenuPath =
        "SURLHWA/Rebuild/Verify/C5.4.1 Bartending Canvas Order";

    private static readonly TargetPrefab[] Targets =
    {
        new TargetPrefab(
            "Assets/_SURLHWA/Prefabs/Bartending/StirMiniGame.prefab",
            "StirMiniGame/UI",
            100),
        new TargetPrefab(
            "Assets/_SURLHWA/Prefabs/Bartending/ShakeMiniGame.prefab",
            "ShakeMiniGame/UI",
            0),
        new TargetPrefab(
            "Assets/_SURLHWA/Prefabs/Bartending/BuildMiniGame.prefab",
            "BuildMiniGame/UI",
            0),
        new TargetPrefab(
            "Assets/_SURLHWA/Prefabs/Bartending/FloatMiniGame.prefab",
            "FloatMiniGame/UI",
            0)
    };

    [MenuItem(MigrationMenuPath)]
    private static void MigrateFromMenu()
    {
        RunWithMenuErrorHandling(RunMigration, "migration");
    }

    [MenuItem(VerificationMenuPath)]
    private static void VerifyFromMenu()
    {
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

    private static void RunWithMenuErrorHandling(Action action, string operation)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            Debug.LogError("[SURLHWA C5.4.1] Canvas order " + operation + " failed: " + exception);
        }
    }

    private static void RunMigration()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,
            "Canvas order migration cannot run while entering or using Play Mode.");

        bool changedAny = false;
        foreach (TargetPrefab target in Targets)
        {
            GameObject prefabAsset = LoadPrefabAsset(target);
            Canvas canvas = ValidateTargetCanvas(prefabAsset, target, true);
            CanvasSettingsSnapshot retainedSettings = CanvasSettingsSnapshot.Capture(canvas);

            if (canvas.sortingOrder == TargetSortingOrder)
            {
                Debug.Log("[SURLHWA C5.4.1][ALREADY_MIGRATED] " +
                          target.PrefabPath + " " + target.HierarchyPath +
                          " already has sortingOrder " + TargetSortingOrder + ".");
                continue;
            }

            SerializedObject canvasObject = new SerializedObject(canvas);
            SerializedProperty sortingOrder = canvasObject.FindProperty("m_SortingOrder");
            Require(sortingOrder != null,
                "Canvas sorting order property is unavailable in " + target.PrefabPath + ".");
            sortingOrder.intValue = TargetSortingOrder;
            Require(canvasObject.ApplyModifiedPropertiesWithoutUndo(),
                "Unity did not apply the Canvas sorting order update in " + target.PrefabPath + ".");

            Require(canvas.sortingOrder == TargetSortingOrder,
                "Canvas sorting order did not update in " + target.PrefabPath + ".");
            Require(retainedSettings.Equals(CanvasSettingsSnapshot.Capture(canvas)),
                "A retained Canvas setting changed while migrating " + target.PrefabPath + ".");

            // Only the Canvas component is marked dirty. Unity may still
            // synchronize pre-existing legacy MonoBehaviour schema fields
            // while writing the shared prefab asset.
            EditorUtility.SetDirty(canvas);
            AssetDatabase.SaveAssetIfDirty(canvas);
            changedAny = true;

            Debug.Log("[SURLHWA C5.4.1][MIGRATED] " + target.PrefabPath + " " +
                      target.HierarchyPath + " sortingOrder " + target.LegacySortingOrder +
                      " -> " + TargetSortingOrder + ".");
        }

        RunVerification();
        Debug.Log(changedAny
            ? "[SURLHWA C5.4.1][PASS] All production Bartending Canvas orders are normalized to 10."
            : "[SURLHWA C5.4.1][ALREADY_MIGRATED] All production Bartending Canvas orders already match 10.");
    }

    private static void RunVerification()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,
            "Canvas order verification cannot run while entering or using Play Mode.");

        foreach (TargetPrefab target in Targets)
        {
            GameObject prefabAsset = LoadPrefabAsset(target);
            Canvas canvas = ValidateTargetCanvas(prefabAsset, target, false);
            Require(canvas.sortingOrder == TargetSortingOrder,
                "Expected sortingOrder " + TargetSortingOrder + " in " + target.PrefabPath +
                ". Actual: " + canvas.sortingOrder + ".");
            Debug.Log("[SURLHWA C5.4.1][VERIFIED] " + target.PrefabPath + " " +
                      target.HierarchyPath + " sortingOrder=" + canvas.sortingOrder + ".");
        }

        Debug.Log("[SURLHWA C5.4.1][PASS] Bartending Canvas order verification passed.");
    }

    private static GameObject LoadPrefabAsset(TargetPrefab target)
    {
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(target.PrefabPath);
        Require(prefabAsset != null,
            "Target prefab is missing: " + target.PrefabPath);
        Require(PrefabUtility.GetPrefabAssetType(prefabAsset) != PrefabAssetType.NotAPrefab,
            "Target asset is not a prefab: " + target.PrefabPath);
        return prefabAsset;
    }

    private static Canvas ValidateTargetCanvas(
        GameObject prefabContents,
        TargetPrefab target,
        bool allowLegacySortingOrder)
    {
        Canvas[] canvases = prefabContents.GetComponentsInChildren<Canvas>(true);
        Require(canvases.Length == 1,
            "Expected exactly one Canvas in " + target.PrefabPath + ". Actual: " + canvases.Length);

        Canvas canvas = canvases[0];
        Require(GetHierarchyPath(canvas.transform) == target.HierarchyPath,
            "Unexpected Canvas hierarchy in " + target.PrefabPath + ". Actual: " +
            GetHierarchyPath(canvas.transform));
        Require(canvas.renderMode == RenderMode.ScreenSpaceOverlay,
            "Canvas render mode changed in " + target.PrefabPath + ". Actual: " + canvas.renderMode);
        Require(canvas.sortingLayerName == "Default",
            "Canvas sorting layer changed in " + target.PrefabPath + ". Actual: " +
            canvas.sortingLayerName);
        Require(!canvas.overrideSorting,
            "Canvas overrideSorting changed in " + target.PrefabPath + ".");

        GraphicRaycaster[] raycasters = canvas.GetComponents<GraphicRaycaster>();
        Require(raycasters.Length == 1 && raycasters[0].enabled,
            "Expected one enabled GraphicRaycaster on " + target.HierarchyPath + " in " +
            target.PrefabPath + ".");
        Require(CountMissingScripts(prefabContents) == 0,
            "Prefab contains Missing Script components: " + target.PrefabPath);

        if (allowLegacySortingOrder)
        {
            Require(canvas.sortingOrder == target.LegacySortingOrder ||
                    canvas.sortingOrder == TargetSortingOrder,
                "Unexpected Canvas sortingOrder in " + target.PrefabPath + ". Actual: " +
                canvas.sortingOrder + ".");
        }

        return canvas;
    }

    private static int CountMissingScripts(GameObject root)
    {
        int count = 0;
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
        return count;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        return transform.parent == null
            ? transform.name
            : GetHierarchyPath(transform.parent) + "/" + transform.name;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class TargetPrefab
    {
        public readonly string PrefabPath;
        public readonly string HierarchyPath;
        public readonly int LegacySortingOrder;

        public TargetPrefab(string prefabPath, string hierarchyPath, int legacySortingOrder)
        {
            PrefabPath = prefabPath;
            HierarchyPath = hierarchyPath;
            LegacySortingOrder = legacySortingOrder;
        }
    }

    private struct CanvasSettingsSnapshot : IEquatable<CanvasSettingsSnapshot>
    {
        private readonly RenderMode renderMode;
        private readonly int sortingLayerId;
        private readonly bool overrideSorting;
        private readonly bool pixelPerfect;
        private readonly int targetDisplay;
        private readonly AdditionalCanvasShaderChannels additionalShaderChannels;
        private readonly bool graphicRaycasterEnabled;

        private CanvasSettingsSnapshot(
            Canvas canvas,
            GraphicRaycaster graphicRaycaster)
        {
            renderMode = canvas.renderMode;
            sortingLayerId = canvas.sortingLayerID;
            overrideSorting = canvas.overrideSorting;
            pixelPerfect = canvas.pixelPerfect;
            targetDisplay = canvas.targetDisplay;
            additionalShaderChannels = canvas.additionalShaderChannels;
            graphicRaycasterEnabled = graphicRaycaster.enabled;
        }

        public static CanvasSettingsSnapshot Capture(Canvas canvas)
        {
            GraphicRaycaster graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
            Require(graphicRaycaster != null,
                "Canvas is missing its required GraphicRaycaster: " + GetHierarchyPath(canvas.transform));
            return new CanvasSettingsSnapshot(canvas, graphicRaycaster);
        }

        public bool Equals(CanvasSettingsSnapshot other)
        {
            return renderMode == other.renderMode &&
                   sortingLayerId == other.sortingLayerId &&
                   overrideSorting == other.overrideSorting &&
                   pixelPerfect == other.pixelPerfect &&
                   targetDisplay == other.targetDisplay &&
                   additionalShaderChannels == other.additionalShaderChannels &&
                   graphicRaycasterEnabled == other.graphicRaycasterEnabled;
        }
    }
}
