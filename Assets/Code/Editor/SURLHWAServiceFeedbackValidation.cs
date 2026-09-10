#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Narrow I14 proof that feedback text projects resolved service truth without
/// recomputing FIT, technique, preparation, discovery, or reward policy.
/// It creates no saves and edits no assets.
/// </summary>
public static class SURLHWAServiceFeedbackValidation
{
    private const string CatalogPath = "Assets/_SURLHWA/Data/Recipes/RecipeCatalog.asset";
    private const string RecommendationOrderPath =
        "Assets/_SURLHWA/Data/Orders/ClarityRecommendationOrder.asset";

    [MenuItem("SURLHWA/Validation/Run I14 Service Feedback")]
    public static void Run()
    {
        RecipeCatalogData catalog = AssetDatabase.LoadAssetAtPath<RecipeCatalogData>(CatalogPath);
        OrderData recommendation = AssetDatabase.LoadAssetAtPath<OrderData>(RecommendationOrderPath);
        Require(catalog != null && recommendation != null,
            "I14 representative catalog or recommendation request is missing.");

        CocktailRecipeData dryMartini = FindRecipe(catalog, "recipe_dry_martini");
        CocktailRecipeData testStir = FindRecipe(catalog, "recipe_test_stir");
        Require(dryMartini.targetCocktail != null && testStir.targetCocktail != null,
            "I14 representative recipes need CocktailData.");

        ValidateGoodFitGoodExecution(dryMartini, recommendation);
        ValidateGoodFitPoorExecution(dryMartini, recommendation);
        ValidatePoorFitGoodExecution(testStir, recommendation);
        ValidatePoorFitPoorExecution(testStir, recommendation);
        ValidateUnknownComposition(recommendation);
        ValidateDiscoveryDoesNotDuplicateFit(testStir, recommendation);

        Debug.Log(
            "[I14 Validation] PASS: resolved WHAT/HOW/QUALITY/FIT/reward feedback, " +
            "unknown safety, and discovery deduplication are preserved.");
    }

    private static void ValidateGoodFitGoodExecution(
        CocktailRecipeData recipe,
        OrderData recommendation)
    {
        ServiceFeedbackPresentation presentation = GuestServiceFeedbackFormatter.Build(
            BuildServiceResult(
                recipe.targetCocktail,
                true,
                "Good",
                RecipePreparationEvaluationStatus.Satisfied,
                new RequestEvaluationResult(
                    GuestRequestType.Recommendation,
                    RequestEvaluationStatus.Satisfied),
                GuestReactionResolver.GuestResponseTone.GoodFitGoodExecution,
                2,
                recommendation.reputationReward,
                false,
                true,
                false));

        Require(presentation.Title == "Guest Fit: Good",
            "Good FIT + good execution must lead with positive FIT.");
        RequireContains(presentation.Message, "Drink: " + recipe.targetCocktail.cocktailName,
            "Recognized known cocktail must be named in the diagnostic summary.");
        RequireContains(presentation.Message, "Technique: Good",
            "Good technique must remain a secondary diagnostic.");
        RequireContains(presentation.Message, "request +" + recommendation.reputationReward,
            "Granted request reward must be visible separately from mechanical reputation.");
    }

    private static void ValidateGoodFitPoorExecution(
        CocktailRecipeData recipe,
        OrderData recommendation)
    {
        ServiceFeedbackPresentation presentation = GuestServiceFeedbackFormatter.Build(
            BuildServiceResult(
                recipe.targetCocktail,
                false,
                "Poor",
                RecipePreparationEvaluationStatus.Unsatisfied,
                new RequestEvaluationResult(
                    GuestRequestType.Recommendation,
                    RequestEvaluationStatus.Satisfied),
                GuestReactionResolver.GuestResponseTone.GoodFitWeakExecution,
                2,
                0,
                false,
                true,
                false));

        RequireContains(presentation.Title, "Guest Fit: Good",
            "Poor execution must not turn a fitting recommendation into poor FIT.");
        RequireContains(presentation.Message, "Preparation: Order differed from the recipe.",
            "Only failed preparation should add the concise HOW diagnostic.");
        RequireContains(presentation.Message, "Technique: Poor",
            "Poor technique must remain visible without replacing FIT.");
    }

    private static void ValidatePoorFitGoodExecution(
        CocktailRecipeData recipe,
        OrderData recommendation)
    {
        ServiceFeedbackPresentation presentation = GuestServiceFeedbackFormatter.Build(
            BuildServiceResult(
                recipe.targetCocktail,
                true,
                "Good",
                RecipePreparationEvaluationStatus.Satisfied,
                new RequestEvaluationResult(
                    GuestRequestType.Recommendation,
                    RequestEvaluationStatus.Unsatisfied),
                GuestReactionResolver.GuestResponseTone.PoorFitGoodExecution,
                2,
                0,
                true,
                true,
                false));

        Require(presentation.Title == "Guest Fit: Poor - Drink Well Made",
            "Poor FIT + good execution must communicate both truths in priority order.");
        RequireContains(presentation.Message, "Technique: Good",
            "Good technique must remain visible when the recommendation was wrong.");
        RequireContains(presentation.Message, "request reward withheld",
            "Unsatisfied FIT must communicate the existing withheld request reward.");
    }

    private static void ValidatePoorFitPoorExecution(
        CocktailRecipeData recipe,
        OrderData recommendation)
    {
        ServiceFeedbackPresentation presentation = GuestServiceFeedbackFormatter.Build(
            BuildServiceResult(
                recipe.targetCocktail,
                false,
                "Poor",
                RecipePreparationEvaluationStatus.Unsatisfied,
                new RequestEvaluationResult(
                    GuestRequestType.Recommendation,
                    RequestEvaluationStatus.Unsatisfied),
                GuestReactionResolver.GuestResponseTone.PoorFitWeakExecution,
                2,
                0,
                false,
                true,
                false));

        Require(presentation.Title == "Guest Fit: Poor",
            "Poor FIT + poor execution must not use positive wording.");
        RequireContains(presentation.Message, "Technique: Poor",
            "Poor FIT + poor execution must still expose technique quality.");
    }

    private static void ValidateUnknownComposition(OrderData recommendation)
    {
        ServiceFeedbackPresentation presentation = GuestServiceFeedbackFormatter.Build(
            BuildServiceResult(
                null,
                false,
                "Poor",
                RecipePreparationEvaluationStatus.NotEvaluated,
                new RequestEvaluationResult(
                    GuestRequestType.Recommendation,
                    RequestEvaluationStatus.Unsatisfied),
                GuestReactionResolver.GuestResponseTone.NotApplicable,
                0,
                0,
                false,
                false,
                false));

        Require(presentation.Title == "Service Result",
            "Unknown composition must preserve the graceful non-FIT reaction path.");
        RequireContains(presentation.Message, "Drink: Unknown Cocktail",
            "Unknown composition must be identified as unknown without a hidden answer.");
        Require(presentation.Message.IndexOf("Recipe", StringComparison.Ordinal) < 0,
            "Unknown composition must not claim a recipe discovery.");
    }

    private static void ValidateDiscoveryDoesNotDuplicateFit(
        CocktailRecipeData recipe,
        OrderData recommendation)
    {
        ServiceFeedbackPresentation presentation = GuestServiceFeedbackFormatter.Build(
            BuildServiceResult(
                recipe.targetCocktail,
                true,
                "Good",
                RecipePreparationEvaluationStatus.Satisfied,
                new RequestEvaluationResult(
                    GuestRequestType.Recommendation,
                    RequestEvaluationStatus.Unsatisfied),
                GuestReactionResolver.GuestResponseTone.PoorFitGoodExecution,
                2,
                0,
                true,
                true,
                true));

        Require(presentation.Title == "Guest Fit: Poor - Drink Well Made",
            "A newly known recipe must not overwrite poor guest FIT.");
        Require(presentation.Message.IndexOf("discovered", StringComparison.OrdinalIgnoreCase) < 0,
            "Discovery belongs to the one popup, not the service summary toast.");
    }

    private static GuestServiceResult BuildServiceResult(
        CocktailData cocktail,
        bool executionSucceeded,
        string grade,
        RecipePreparationEvaluationStatus preparationStatus,
        RequestEvaluationResult fit,
        GuestReactionResolver.GuestResponseTone tone,
        int mechanicalReputation,
        int requestReputation,
        bool requestRewardWithheld,
        bool canRevealCocktail,
        bool newlyKnown)
    {
        MiniGameResult miniGame = new()
        {
            success = executionSucceeded,
            repDelta = mechanicalReputation,
            grade = grade,
            preparationEvaluation = new RecipePreparationEvaluationResult(
                preparationStatus,
                0,
                0,
                string.Empty)
        };

        return new GuestServiceResult(
            cocktail,
            miniGame,
            fit,
            new GuestReactionResolver.GuestResponseOutcome(
                0,
                tone,
                tone != GuestReactionResolver.GuestResponseTone.NotApplicable),
            mechanicalReputation + requestReputation,
            mechanicalReputation,
            requestReputation,
            requestRewardWithheld,
            canRevealCocktail,
            newlyKnown);
    }

    private static CocktailRecipeData FindRecipe(RecipeCatalogData catalog, string recipeId)
    {
        foreach (RecipeCatalogEntry entry in catalog.Entries)
        {
            if (entry != null && entry.Recipe != null && entry.Recipe.recipeId == recipeId)
                return entry.Recipe;
        }

        throw new InvalidOperationException("[I14 Validation] Missing recipe '" + recipeId + "'.");
    }

    private static void RequireContains(string value, string expected, string message)
    {
        Require(!string.IsNullOrEmpty(value) &&
                value.IndexOf(expected, StringComparison.Ordinal) >= 0,
            message);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("[I14 Validation] " + message);
    }
}
#endif
