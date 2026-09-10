#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Narrow I15 regression proof. It exercises the production input/transfer
/// seams without assigning a new score or recipe authority to presentation.
/// It writes no assets and creates no persistent progression state.
/// </summary>
public static class SURLHWABartendingFeelValidation
{
    private const string GinPath = "Assets/_SURLHWA/Data/Ingredients/Ingredient_Gin.asset";
    private const string ShelfPath = "Assets/_SURLHWA/Prefabs/UI/IngredientShelfPanel.prefab";
    private const string StirPath = "Assets/_SURLHWA/Prefabs/Bartending/StirMiniGame.prefab";
    private const string ShakePath = "Assets/_SURLHWA/Prefabs/Bartending/ShakeMiniGame.prefab";
    private const string FloatPath = "Assets/_SURLHWA/Prefabs/Bartending/FloatMiniGame.prefab";

    [MenuItem("SURLHWA/Validation/Run I15 Bartending Feel")]
    public static void Run()
    {
        IngredientData gin = AssetDatabase.LoadAssetAtPath<IngredientData>(GinPath);
        Require(gin != null, "Representative Gin ingredient is missing.");

        ValidateProductionTuning();
        ValidateStirInputSafety();
        ValidateGentleStirPresentation();
        ValidatePourStopAndTransferSemantics(gin);
        ValidateFloatLayerSemantics(gin);
        ValidateGarnishReplacement(gin);

        Debug.Log(
            "[I15 Validation] PASS: tuned stir input guard and visual follower, " +
            "pour-stop/transfer semantics, Shake timing values, Float flow/layer semantics, " +
            "and garnish replacement remain bounded.");
    }

    private static void ValidateProductionTuning()
    {
        GameObject stirRoot = PrefabUtility.LoadPrefabContents(StirPath);
        GameObject shakeRoot = PrefabUtility.LoadPrefabContents(ShakePath);
        GameObject floatRoot = PrefabUtility.LoadPrefabContents(FloatPath);
        try
        {
            StirMiniGame stir = stirRoot.GetComponent<StirMiniGame>();
            ShakeMiniGame shake = shakeRoot.GetComponent<ShakeMiniGame>();
            Require(stir != null && shake != null, "Production Stir or Shake prefab is missing its controller.");
            Require(Mathf.Approximately(GetPrivateFloat(stir, "minimumMouseRadiusPixels"), 36f),
                "Production Stir dead zone must be 36 px.");
            Require(Mathf.Approximately(GetPrivateFloat(stir, "maximumAngularJumpDeg"), 110f),
                "Production Stir jump guard must be 110 degrees.");
            Require(Mathf.Approximately(GetPrivateFloat(stir, "spoonVisualSmoothingSeconds"), 0.08f),
                "Production Stir visual smoothing must be 0.08 seconds.");

            Require(Mathf.Approximately(GetPrivateFloat(shake, "perfectWindowSeconds"), 0.09f) &&
                    Mathf.Approximately(GetPrivateFloat(shake, "goodWindowSeconds"), 0.20f),
                "I15 must not alter Shake timing windows.");

            FloatMiniGame floatMiniGame = floatRoot.GetComponent<FloatMiniGame>();
            Require(floatMiniGame != null, "Production Float prefab is missing its controller.");
            RectTransform floatSpoon = GetPrivateReference<RectTransform>(floatMiniGame, "spoon");
            Require(floatSpoon != null && floatSpoon.name == "SpoonImage",
                "Production Float prefab must retain its authored SpoonImage visual binding.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(stirRoot);
            PrefabUtility.UnloadPrefabContents(shakeRoot);
            PrefabUtility.UnloadPrefabContents(floatRoot);
        }
    }

    private static void ValidateStirInputSafety()
    {
        StirTechniqueTracker tracker = new(110f);
        Require(!tracker.RecordAngle(0f, 0.1f), "First valid angle is a reference, not scored rotation.");
        Require(tracker.RecordAngle(50f, 0.1f), "Normal circular motion must be accepted.");
        float beforeDeadZone = tracker.TotalRotationDegrees;

        // Runtime uses RecordIdle when the cursor is within the 36 px center
        // dead zone. Leaving it must establish a fresh reference, never invent
        // a cross-center reversal or rotation.
        tracker.RecordIdle(0.1f);
        Require(!tracker.RecordAngle(200f, 0.1f), "Leaving the center dead zone must not score a synthetic jump.");
        Require(Mathf.Approximately(tracker.TotalRotationDegrees, beforeDeadZone),
            "Dead-zone transition changed technique rotation.");

        Require(tracker.RecordAngle(300f, 0.1f),
            "A 100 degree continuous circular sample must pass the tuned guard.");
        float beforeRejectedJump = tracker.TotalRotationDegrees;
        Require(!tracker.RecordAngle(90f, 0.1f), "A 150 degree teleport must remain rejected.");
        Require(Mathf.Approximately(tracker.TotalRotationDegrees, beforeRejectedJump) &&
                tracker.RejectedJumpCount == 1,
            "Rejected jump changed score or was not diagnosed.");
    }

    private static void ValidateGentleStirPresentation()
    {
        GameObject root = new("I15 Gentle Stir Visual", typeof(RectTransform), typeof(Canvas));
        try
        {
            RectTransform center = new GameObject("ServingGlass", typeof(RectTransform))
                .GetComponent<RectTransform>();
            center.SetParent(root.transform, false);
            center.sizeDelta = new Vector2(120f, 150f);

            ServingGlassGentleStirInteraction interaction = new();
            Require(interaction.Begin(center, root.transform), "GentleStir visual could not begin.");
            Require(!interaction.TryRecordAngle(0f, 0.1f),
                "GentleStir first angle must remain a reference sample.");
            Require(interaction.TryRecordAngle(45f, 0.1f),
                "GentleStir must share accepted circular input semantics.");

            Transform spoon = center.Find("RuntimeGentleStirSpoon");
            Require(spoon != null && spoon.GetComponent<RectTransform>().anchoredPosition.sqrMagnitude > 0.01f,
                "GentleStir spoon must visibly orbit rather than remain at the center.");
            interaction.End();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void ValidatePourStopAndTransferSemantics(IngredientData gin)
    {
        GameObject shelfRoot = PrefabUtility.LoadPrefabContents(ShelfPath);
        try
        {
            IngredientShelfController shelf = shelfRoot.GetComponentInChildren<IngredientShelfController>(true);
            Require(shelf != null, "Shared IngredientShelfController is missing.");

            shelf.Initialize(MiniGameType.Stir, BartendingContainerType.MixingGlass, new IngredientAvailabilityContext(1));
            Require(shelf.TrySelectIngredient(gin), "Could not select Day 1 Gin for pour proof.");
            Require(shelf.AddLiquidMl(gin, 30f) > 0f, "Actual Shelf pour was not accepted.");
            float beforeStop = shelf.TargetContainer.CurrentLiquidVolumeMl;
            shelf.EndSelectedPour();
            Require(Mathf.Approximately(shelf.TargetContainer.CurrentLiquidVolumeMl, beforeStop),
                "Ending pour added hidden liquid after release.");
            Require(shelf.TryAddIceToActiveTarget(), "Mixing Glass ice was not accepted.");
            Require(shelf.TryStrainActiveTargetToServingGlass(out float strainedMl) && Mathf.Approximately(strainedMl, beforeStop),
                "Strain must move the actual liquid exactly once.");
            Require(shelf.PreparationTrace.Actions.Count == 3 &&
                    shelf.PreparationTrace.Actions[2].Action == RecipePreparationAction.Strain &&
                    shelf.PreparationState.GetContainer(BartendingContainerType.MixingGlass).HasIce,
                "Strain must preserve semantic trace and source ice.");

            shelf.Initialize(MiniGameType.Shake, BartendingContainerType.Shaker, new IngredientAvailabilityContext(1));
            Require(shelf.TrySelectIngredient(gin) && shelf.AddLiquidMl(gin, 20f) > 0f,
                "Shaker transfer proof could not add actual liquid.");
            Require(shelf.TryPourActiveTargetToServingGlass(out float pouredMl) && Mathf.Approximately(pouredMl, 20f),
                "PourFromContainer must transfer actual liquid.");
            Require(shelf.PreparationTrace.Actions.Count == 2 &&
                    shelf.PreparationTrace.Actions[1].Action == RecipePreparationAction.PourFromContainer,
                "PourFromContainer must not be recorded as Strain.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(shelfRoot);
        }
    }

    private static void ValidateFloatLayerSemantics(IngredientData gin)
    {
        BartendingLayeredDrinkState layers = new();
        Require(layers.TryBeginLayer(gin), "Could not begin Float base layer.");
        Require(Mathf.Approximately(layers.AddToActiveLayer(30f), 30f), "Base layer ml changed.");
        Require(layers.TryCompleteActiveLayer(null, out BartendingLayer baseLayer) && baseLayer.OrderIndex == 0,
            "Base layer did not complete as the first ordered layer.");

        Require(layers.TryBeginLayer(gin), "Could not begin second Float layer.");
        Require(Mathf.Approximately(layers.AddToActiveLayer(10f), 10f), "Second layer ml changed.");
        Require(layers.TryCompleteActiveLayer(BartendingLayerBoundaryQuality.Clean, out BartendingLayer topLayer) &&
                topLayer.OrderIndex == 1 && layers.Boundaries.Count == 1 &&
                layers.Boundaries[0].Quality == BartendingLayerBoundaryQuality.Clean,
            "Float must preserve ordered layers and their resolved boundary quality.");
    }

    private static void ValidateGarnishReplacement(IngredientData gin)
    {
        BartendingContainerState servingGlass = new(BartendingContainerType.ServingGlass);
        Require(servingGlass.AddLiquid(gin, 30f) > 0f, "Serving Glass garnish proof needs actual liquid.");
        Require(servingGlass.TryApplyGarnish(BartendingGarnishId.Olive, BartendingGarnishPlacement.InGlass),
            "Initial garnish was not accepted.");
        Require(servingGlass.TryApplyGarnish(BartendingGarnishId.LemonTwist, BartendingGarnishPlacement.Rim) &&
                servingGlass.Garnish.GarnishId == BartendingGarnishId.LemonTwist,
            "Garnish replacement behavior changed.");
    }

    private static float GetPrivateFloat(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Require(field != null && field.FieldType == typeof(float), "Missing float field '" + fieldName + "'.");
        return (float)field.GetValue(target);
    }

    private static T GetPrivateReference<T>(object target, string fieldName) where T : class
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Require(field != null && typeof(T).IsAssignableFrom(field.FieldType),
            "Missing reference field '" + fieldName + "'.");
        return field.GetValue(target) as T;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("[I15 Validation] " + message);
    }
}
#endif
