#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Narrow I12 proof that request fit remains pure, deterministic, and separate
/// from matching and execution quality. It creates no saves and edits no asset.
/// </summary>
public static class SURLHWAGuestFitValidation
{
    private const string CatalogPath = "Assets/_SURLHWA/Data/Recipes/RecipeCatalog.asset";
    private const string AvailabilityPath =
        "Assets/_SURLHWA/Data/Ingredients/IngredientAvailability.asset";
    private const string RecommendationOrderPath =
        "Assets/_SURLHWA/Data/Orders/ClarityRecommendationOrder.asset";
    private const string DryMartiniOrderPath =
        "Assets/_SURLHWA/Data/Orders/DryMartiniAttemptOrder.asset";
    private const string ReactionDatabasePath =
        "Assets/_SURLHWA/Data/Dialogue/GeneralGuestReactionDB.asset";

    [MenuItem("SURLHWA/Validation/Run I12 Guest Fit")]
    public static void Run()
    {
        RecipeCatalogData catalog = AssetDatabase.LoadAssetAtPath<RecipeCatalogData>(CatalogPath);
        IngredientAvailabilityData availability =
            AssetDatabase.LoadAssetAtPath<IngredientAvailabilityData>(AvailabilityPath);
        OrderData recommendation = AssetDatabase.LoadAssetAtPath<OrderData>(RecommendationOrderPath);
        OrderData dryMartiniExact = AssetDatabase.LoadAssetAtPath<OrderData>(DryMartiniOrderPath);
        GuestReactionDatabase reactions =
            AssetDatabase.LoadAssetAtPath<GuestReactionDatabase>(ReactionDatabasePath);

        Require(catalog != null && availability != null && recommendation != null &&
                dryMartiniExact != null && reactions != null,
            "I12 representative assets are missing.");

        CocktailRecipeData dryMartini = FindRecipe(catalog, "recipe_dry_martini");
        CocktailRecipeData testStir = FindRecipe(catalog, "recipe_test_stir");
        ValidateContent(recommendation, dryMartiniExact, dryMartini, testStir, reactions);
        ValidateFitMatrix(recommendation, dryMartiniExact, dryMartini, testStir);
        ValidateAvailability(availability, dryMartini, testStir);
        ValidateRewardPolicy(recommendation, dryMartini, testStir);

        Debug.Log(
            "[I12 Validation] PASS: recommendation/exact fit matrix, unknown safety, " +
            "execution independence, response tiers, and Day 1 content safety are preserved.");
    }

    private static void ValidateContent(
        OrderData recommendation,
        OrderData dryMartiniExact,
        CocktailRecipeData dryMartini,
        CocktailRecipeData testStir,
        GuestReactionDatabase reactions)
    {
        Require(recommendation.RequestType == GuestRequestType.Recommendation &&
                recommendation.requestedCocktail == null &&
                Contains(recommendation.DesiredEmotionTags, EmotionTag.Clarity),
            "The representative request must be an unnamed Clarity recommendation.");
        Require(dryMartiniExact.RequestType == GuestRequestType.ExactCocktail &&
                dryMartiniExact.requestedCocktail == dryMartini.targetCocktail,
            "Existing I11C Dry Martini requests must retain Exact semantics.");
        Require(Contains(dryMartini.targetCocktail.emotionTags, EmotionTag.Clarity),
            "Dry Martini must carry the minimal authored Clarity tag for I12.");
        Require(!Contains(testStir.targetCocktail.emotionTags, EmotionTag.Clarity),
            "The contrast cocktail must remain incompatible with the Clarity request.");
        Require(reactions.guestFitLines != null && reactions.guestFitLines.Count >= 4,
            "Guest reaction data must contain bounded fit/quality response coverage.");
    }

    private static void ValidateFitMatrix(
        OrderData recommendation,
        OrderData dryMartiniExact,
        CocktailRecipeData dryMartini,
        CocktailRecipeData testStir)
    {
        RequestEvaluationResult compatible = RequestEvaluation.Evaluate(
            recommendation, Matched(dryMartini));
        RequestEvaluationResult incompatible = RequestEvaluation.Evaluate(
            recommendation, Matched(testStir));
        RequestEvaluationResult exactMatch = RequestEvaluation.Evaluate(
            dryMartiniExact, Matched(dryMartini));
        RequestEvaluationResult exactMismatch = RequestEvaluation.Evaluate(
            dryMartiniExact, Matched(testStir));
        RequestEvaluationResult unknown = RequestEvaluation.Evaluate(
            recommendation, default);

        Require(compatible.Status == RequestEvaluationStatus.Satisfied,
            "Compatible recommendation must satisfy FIT.");
        Require(incompatible.Status == RequestEvaluationStatus.Unsatisfied,
            "Recognized incompatible cocktail must fail FIT.");
        Require(exactMatch.Status == RequestEvaluationStatus.Satisfied,
            "Exact request plus requested cocktail must satisfy FIT.");
        Require(exactMismatch.Status == RequestEvaluationStatus.Unsatisfied,
            "Exact request plus another cocktail must fail FIT.");
        Require(unknown.Status == RequestEvaluationStatus.Unsatisfied,
            "Unknown composition must never become a positive recommendation fit.");

        GuestReactionResolver.GuestResponseOutcome goodFitGoodExecution =
            GuestReactionResolver.EvaluateOutcome(
                compatible, true, recommendation.DesiredEmotionTags,
                dryMartini.targetCocktail.emotionTags, true);
        GuestReactionResolver.GuestResponseOutcome goodFitWeakExecution =
            GuestReactionResolver.EvaluateOutcome(
                compatible, false, recommendation.DesiredEmotionTags,
                dryMartini.targetCocktail.emotionTags, true);
        GuestReactionResolver.GuestResponseOutcome poorFitGoodExecution =
            GuestReactionResolver.EvaluateOutcome(
                incompatible, true, recommendation.DesiredEmotionTags,
                testStir.targetCocktail.emotionTags, true);
        GuestReactionResolver.GuestResponseOutcome poorFitWeakExecution =
            GuestReactionResolver.EvaluateOutcome(
                incompatible, false, recommendation.DesiredEmotionTags,
                testStir.targetCocktail.emotionTags, true);
        GuestReactionResolver.GuestResponseOutcome unknownOutcome =
            GuestReactionResolver.EvaluateOutcome(
                unknown, false, recommendation.DesiredEmotionTags,
                Array.Empty<EmotionTag>(), false);

        Require(goodFitGoodExecution.Tone ==
                    GuestReactionResolver.GuestResponseTone.GoodFitGoodExecution &&
                goodFitGoodExecution.Hearts == 3,
            "Good fit + good execution response tier is wrong.");
        Require(goodFitWeakExecution.Tone ==
                    GuestReactionResolver.GuestResponseTone.GoodFitWeakExecution &&
                goodFitWeakExecution.Hearts == 2 && compatible.Status == RequestEvaluationStatus.Satisfied,
            "Weak execution must not change good FIT.");
        Require(poorFitGoodExecution.Tone ==
                    GuestReactionResolver.GuestResponseTone.PoorFitGoodExecution &&
                poorFitGoodExecution.Hearts == 1 && incompatible.Status == RequestEvaluationStatus.Unsatisfied,
            "Good execution must not repair poor FIT.");
        Require(poorFitWeakExecution.Tone ==
                    GuestReactionResolver.GuestResponseTone.PoorFitWeakExecution &&
                poorFitWeakExecution.Hearts == 0,
            "Poor fit + weak execution response tier is wrong.");
        Require(!unknownOutcome.HasGuestFitFeedback,
            "Unknown composition must retain the graceful unknown-cocktail response path.");
    }

    private static void ValidateAvailability(
        IngredientAvailabilityData availability,
        CocktailRecipeData dryMartini,
        CocktailRecipeData testStir)
    {
        Require(IngredientsAvailableOnDayOne(availability, dryMartini),
            "Compatible Dry Martini must remain Day 1 constructible.");
        Require(IngredientsAvailableOnDayOne(availability, testStir),
            "Recognized incompatible contrast must remain Day 1 constructible.");
    }

    private static void ValidateRewardPolicy(
        OrderData recommendation,
        CocktailRecipeData dryMartini,
        CocktailRecipeData testStir)
    {
        MethodInfo calculateReward = typeof(OrderManager).GetMethod(
            "CalculateReputationDelta",
            BindingFlags.Static | BindingFlags.NonPublic);
        Require(calculateReward != null,
            "OrderManager.CalculateReputationDelta reward seam is missing.");

        MiniGameResult technicallyGood = new() { success = true, repDelta = 2 };
        MiniGameResult technicallyWeak = new() { success = false, repDelta = 2 };
        RequestEvaluationResult compatible = RequestEvaluation.Evaluate(
            recommendation, Matched(dryMartini));
        RequestEvaluationResult incompatible = RequestEvaluation.Evaluate(
            recommendation, Matched(testStir));

        int goodFitReward = (int)calculateReward.Invoke(
            null, new object[]
            {
                technicallyGood,
                dryMartini,
                GuestRequestContext.FromOrder(recommendation),
                compatible
            });
        int poorFitReward = (int)calculateReward.Invoke(
            null, new object[]
            {
                technicallyGood,
                testStir,
                GuestRequestContext.FromOrder(recommendation),
                incompatible
            });
        int weakExecutionReward = (int)calculateReward.Invoke(
            null, new object[]
            {
                technicallyWeak,
                dryMartini,
                GuestRequestContext.FromOrder(recommendation),
                compatible
            });

        Require(goodFitReward == technicallyGood.repDelta + recommendation.reputationReward,
            "Satisfied recommendation must receive the existing request reward once.");
        Require(poorFitReward == technicallyGood.repDelta,
            "Unsatisfied recommendation must retain repDelta but lose only request reward.");
        Require(weakExecutionReward == technicallyWeak.repDelta,
            "Weak execution must retain mechanical repDelta and not grant request reward.");
    }

    private static bool IngredientsAvailableOnDayOne(
        IngredientAvailabilityData availability,
        CocktailRecipeData recipe)
    {
        foreach (RecipeIngredientEntry entry in recipe.ingredients)
        {
            if (entry == null || entry.ingredient == null || !availability.IsIngredientAvailable(
                    entry.ingredient, new IngredientAvailabilityContext(1)))
            {
                return false;
            }
        }

        return true;
    }

    private static RecipeMatchResult Matched(CocktailRecipeData recipe)
    {
        return new RecipeMatchResult
        {
            success = true,
            matchedRecipe = recipe,
            similarity = 1f
        };
    }

    private static CocktailRecipeData FindRecipe(RecipeCatalogData catalog, string recipeId)
    {
        foreach (RecipeCatalogEntry entry in catalog.Entries)
        {
            if (entry != null && entry.Recipe != null && entry.Recipe.recipeId == recipeId)
                return entry.Recipe;
        }

        throw new InvalidOperationException("[I12 Validation] Missing recipe '" + recipeId + "'.");
    }

    private static bool Contains(EmotionTag[] tags, EmotionTag tag)
    {
        if (tags == null)
            return false;

        foreach (EmotionTag candidate in tags)
        {
            if (candidate == tag)
                return true;
        }

        return false;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("[I12 Validation] " + message);
    }
}
#endif
