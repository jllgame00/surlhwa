#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SURLHWAJ5PrinceClosureValidation
{
    private const string Data = "Assets/_SURLHWA/Data/";
    internal static readonly string[] Keys = {
        "littlePrinceFirstVisitCompleted", "SURLHWA_LITTLE_PRINCE_FOLLOWUP_VISIT_COMPLETED",
        "SURLHWA_LITTLE_PRINCE_CLOSING_VISIT_COMPLETED", "SURLHWA_LITTLE_PRINCE_SERVICE_OUTCOME_1",
        "SURLHWA_LITTLE_PRINCE_SERVICE_OUTCOME_2", "SURLHWA_LITTLE_PRINCE_SERVICE_OUTCOME_3",
        "SURLHWA_LITTLE_PRINCE_NARRATIVE_CHOICE"
    };

    [MenuItem("SURLHWA/Validation/Run J1-J5 Production Regression")]
    public static void RunProductionRegression()
    {
        SURLHWAJ4DiscoveryValidation.RunProductionRegression();
        Run();
    }

    [MenuItem("SURLHWA/Validation/Run J5 Prince Closure")]
    public static void Run()
    {
        Dictionary<string, int?> snapshot = new();
        foreach (string key in Keys)
            snapshot[key] = SurlhwaPlayerPrefsStore.HasKey(key) ? SurlhwaPlayerPrefsStore.GetInt(key, 0) : null;
        Scene scene = default;
        try
        {
            ValidateOutcomeMatrix();
            ValidateExecutionBoundary();
            scene = EditorSceneManager.OpenScene("Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity", OpenSceneMode.Additive);
            ValidateScheduleAndContent(scene);
            Debug.Log("[J5 Validation] PASS: all eight service histories, FIT/technique/preparation independence, " +
                "legacy/invalid state, sticky failure, farewell choice recall, fourth-slot closure and future statue eligibility. " +
                "Actual UI reading and normal-input bartending are separate checks.");
        }
        finally
        {
            if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            foreach (var entry in snapshot)
            {
                if (entry.Value.HasValue) SurlhwaPlayerPrefsStore.SetInt(entry.Key, entry.Value.Value);
                else SurlhwaPlayerPrefsStore.DeleteKey(entry.Key);
            }
            SurlhwaPlayerPrefsStore.Save();
        }
    }

    internal static void ClearVisits()
    {
        LittlePrinceVisitPersistence.ClearFirstVisitCompleted();
        LittlePrinceVisitPersistence.ClearFollowupVisitCompleted();
        LittlePrinceVisitPersistence.ClearClosureProgress();
        LittlePrinceNarrativePersistence.ClearChoice();
    }

    private static void ValidateOutcomeMatrix()
    {
        var narrative = Load<LittlePrinceNarrativeData>("Narrative/LittlePrinceNarrative.asset");
        for (int mask = 0; mask < 8; mask++)
        {
            foreach (LittlePrinceNarrativeChoice choice in Enum.GetValues(typeof(LittlePrinceNarrativeChoice)))
            {
                ClearVisits();
                if (choice != LittlePrinceNarrativeChoice.None) LittlePrinceNarrativePersistence.TryCommitChoice(choice);
                for (int visit = 1; visit <= 3; visit++)
                {
                    bool success = (mask & (1 << (visit - 1))) != 0;
                    LittlePrinceVisitPersistence.RecordServiceOutcome(visit, success);
                    if (!success) LittlePrinceVisitPersistence.RecordServiceOutcome(visit, true);
                    Require(LittlePrinceVisitPersistence.GetServiceOutcome(visit) == (success ? 1 : 2),
                        "Reload/retry must not erase a failed service at visit " + visit);
                    if (visit == 1) LittlePrinceVisitPersistence.MarkFirstVisitCompleted();
                    if (visit == 2) LittlePrinceVisitPersistence.MarkFollowupVisitCompleted();
                }
                Require(LittlePrinceVisitPersistence.HasOpenFarewell() == (mask == 7),
                    "Only three successful services may open the farewell.");
                Require(!LittlePrinceVisitPersistence.IsStatueEligible(), "Statue eligibility cannot precede departure.");
                string[] lines = narrative.GetFarewellLines(choice, mask == 7);
                Require(lines.Length >= 3 && Array.TrueForAll(lines, line => !string.IsNullOrWhiteSpace(line)),
                    "Every A/B/None history needs authored readable closure.");
                LittlePrinceVisitPersistence.MarkClosingVisitCompleted();
                Require(LittlePrinceVisitPersistence.IsStatueEligible() == (mask == 7), "Statue eligibility differs from farewell.");
                LittlePrinceVisitPersistence.RecordServiceOutcome(3, false);
                Require(LittlePrinceVisitPersistence.IsStatueEligible() == (mask == 7), "Completed closure must be immutable.");
            }
        }
        Require(narrative.GetFarewellLines(LittlePrinceNarrativeChoice.KeepTheLight, true)[0] !=
                narrative.GetFarewellLines(LittlePrinceNarrativeChoice.SeekTheStar, true)[0] &&
                narrative.GetFarewellLines(LittlePrinceNarrativeChoice.None, true)[0] !=
                narrative.GetFarewellLines(LittlePrinceNarrativeChoice.KeepTheLight, true)[0],
            "A/B and legacy closing recall must remain distinct.");
        ClearVisits();
        LittlePrinceVisitPersistence.MarkFirstVisitCompleted();
        LittlePrinceVisitPersistence.MarkFollowupVisitCompleted();
        LittlePrinceVisitPersistence.RecordServiceOutcome(3, true);
        Require(!LittlePrinceVisitPersistence.HasOpenFarewell(), "Legacy completion is not evidence of successful service.");
        SurlhwaPlayerPrefsStore.SetInt(Keys[3], 99);
        Require(LittlePrinceVisitPersistence.GetServiceOutcome(1) == 0 && !LittlePrinceVisitPersistence.HasOpenFarewell(),
            "Malformed outcome must not grant the open farewell.");
        LittlePrinceVisitPersistence.ClearClosureProgress();
        Require(!LittlePrinceVisitPersistence.HasSavedClosureState() && !LittlePrinceVisitPersistence.IsStatueEligible(),
            "New Game's closure reset must clear every new narrative state.");
    }

    private static void ValidateExecutionBoundary()
    {
        CocktailRecipeData recipe = null;
        foreach (var entry in Load<RecipeCatalogData>("Recipes/RecipeCatalog.asset").Entries)
            if (entry?.Recipe != null && entry.Recipe.recipeId == "recipe_manwol") recipe = entry.Recipe;
        Require(recipe != null, "Manwol recipe is missing.");
        var request = Load<OrderData>("Orders/LittlePrinceClosingOrder.asset");
        var match = new RecipeMatchResult { success = true, matchedRecipe = recipe, similarity = 1f };
        var fit = RequestEvaluation.Evaluate(request, match);
        Require(fit.Status == RequestEvaluationStatus.Satisfied, "Manwol must fit final Clarity request.");
        var trace = new BartendingPreparationTrace();
        foreach (var step in recipe.expectedPreparation)
            trace.Record(step.action, step.ingredient, 30f, null, BartendingContainerType.ServingGlass);
        var validPreparation = RecipePreparationEvaluator.Evaluate(recipe, trace);
        Require(validPreparation.Status == RecipePreparationEvaluationStatus.Satisfied, "Actual Manwol trace must satisfy recipe.");
        foreach (bool execution in new[] { false, true })
        foreach (bool preparation in new[] { false, true })
        {
            var result = new MiniGameResult { success = execution, preparationEvaluation = preparation
                ? validPreparation : RecipePreparationEvaluator.Evaluate(recipe, new BartendingPreparationTrace()) };
            var service = Service(recipe.targetCocktail, result, fit);
            Require(LittlePrinceVisitPersistence.IsSuccessfulService(service) == (execution && preparation),
                "Fitting drink must fail the story outcome when technique OR preparation fails.");
        }
        var good = new MiniGameResult { success = true, preparationEvaluation = validPreparation };
        var wrongFit = RequestEvaluation.Evaluate(Load<OrderData>("Orders/LittlePrinceFollowupOrder.asset"), match);
        Require(wrongFit.Status == RequestEvaluationStatus.Unsatisfied &&
                !LittlePrinceVisitPersistence.IsSuccessfulService(Service(recipe.targetCocktail, good, wrongFit)),
            "Good preparation cannot make an unfitting drink correct.");
        Require(!LittlePrinceVisitPersistence.IsSuccessfulService(Service(null, good, fit)), "Unknown liquid cannot pass.");
        good.preparationEvaluation = null;
        Require(!LittlePrinceVisitPersistence.IsSuccessfulService(Service(recipe.targetCocktail, good, fit)),
            "Missing preparation evidence cannot pass.");
    }

    internal static GuestServiceResult Service(CocktailData cocktail, MiniGameResult result, RequestEvaluationResult fit)
    {
        var reaction = GuestReactionResolver.EvaluateOutcome(fit, result.success, Array.Empty<EmotionTag>(),
            cocktail != null ? cocktail.emotionTags : Array.Empty<EmotionTag>(), cocktail != null);
        return new GuestServiceResult(cocktail, result, fit, reaction, 0, 0, 0, false, true, false);
    }

    private static void ValidateScheduleAndContent(Scene scene)
    {
        CustomerSpawner spawner = null;
        BusinessDayManager day = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (spawner == null) spawner = root.GetComponentInChildren<CustomerSpawner>(true);
            if (day == null) day = root.GetComponentInChildren<BusinessDayManager>(true);
        }
        Require(spawner != null && day != null, "Production scheduling owners missing.");
        ClearVisits();
        LittlePrinceVisitPersistence.MarkFirstVisitCompleted();
        LittlePrinceVisitPersistence.MarkFollowupVisitCompleted();
        Set(day, "currentDay", 4);
        for (int slot = 0; slot < 4; slot++)
        {
            Set(spawner, "spawnedCount", slot);
            Require((bool)Invoke(spawner, "ShouldSpawnLittlePrinceClosingVisit") == (slot == 3),
                "Closing visit belongs only to Day 4 slot 4.");
        }
        Require(Invoke(spawner, "GetAuthoredOrderForNextSpawn") == null, "Closing Prince must take priority over generic requests.");
        LittlePrinceVisitPersistence.MarkClosingVisitCompleted();
        Require(!(bool)Invoke(spawner, "ShouldSpawnLittlePrinceClosingVisit"), "Completed farewell must not repeat on Continue.");
        var first = Load<OrderData>("Orders/LittlePrinceFirstVisitOrder.asset");
        var follow = Load<OrderData>("Orders/LittlePrinceFollowupOrder.asset");
        var closing = Load<OrderData>("Orders/LittlePrinceClosingOrder.asset");
        var recovery = Load<OrderData>("Orders/ManwolRecoveryOrder.asset");
        Require(!first.RequiresSuccessfulDiscovery && !follow.RequiresSuccessfulDiscovery && !closing.RequiresSuccessfulDiscovery,
            "Narrative visits must accept failure without retry gates.");
        Require(follow.requestedCocktail == null && closing.requestedCocktail == null &&
                follow.RequestType == GuestRequestType.Recommendation && closing.RequestType == GuestRequestType.Recommendation,
            "Later narrative requests must not contain hidden exact targets.");
        Require((OrderData)Get(spawner, "littlePrinceClosingOrder") == closing && (OrderData)Get(spawner, "manwolRecoveryOrder") == recovery &&
                recovery.RequiresSuccessfulDiscovery && recovery.requestedCocktail == first.requestedCocktail &&
                recovery.PreBrewDialogue != null && recovery.PreBrewDialogue.onEnterLines.Length == 2,
            "Independent Manwol recovery must remain wired and teach before retry-safe practice.");
        for (int line = 0; line < 2; line++)
            Require(recovery.PreBrewDialogue.GetOnEnterKnowledgeReveals(line).Length > 0, "Recovery teaching must retain recipe facts.");
    }

    internal static T Load<T>(string path) where T : UnityEngine.Object
    {
        T value = AssetDatabase.LoadAssetAtPath<T>(Data + path);
        Require(value != null, "Missing asset " + path);
        return value;
    }
    internal static object Get(object instance, string field) => instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
    internal static void Set(object instance, string field, object value) => instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, value);
    internal static object Invoke(object instance, string method, params object[] args) => instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, args);
    internal static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("[J5 Validation] " + message);
    }
}
#endif
