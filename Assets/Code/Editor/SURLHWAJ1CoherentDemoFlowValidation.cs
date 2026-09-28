#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bounded J1 proof for the portfolio-demo lifecycle. It uses the existing
/// business, recipe, narrative, and FIT owners; no alternate progression or
/// request authority is introduced for validation.
/// </summary>
public static class SURLHWAJ1CoherentDemoFlowValidation
{
    private const string BarScenePath = "Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity";
    private const string CatalogPath = "Assets/_SURLHWA/Data/Recipes/RecipeCatalog.asset";
    private const string TestStirOrderPath = "Assets/_SURLHWA/Data/Orders/TestStir.asset";
    private const string DryMartiniAttemptOrderPath =
        "Assets/_SURLHWA/Data/Orders/DryMartiniAttemptOrder.asset";
    private const string LittlePrinceFirstOrderPath =
        "Assets/_SURLHWA/Data/Orders/LittlePrinceFirstVisitOrder.asset";

    [MenuItem("SURLHWA/Validation/Run J1 Coherent Demo Flow")]
    public static void Run()
    {
        ProgressSnapshot snapshot = ProgressSnapshot.Capture();
        Scene scene = default;

        try
        {
            RecipeCatalogData catalog = LoadRequired<RecipeCatalogData>(CatalogPath);
            OrderData testStirTemplate = LoadRequired<OrderData>(TestStirOrderPath);
            OrderData dryMartiniAttempt = LoadRequired<OrderData>(DryMartiniAttemptOrderPath);
            OrderData littlePrinceFirst = LoadRequired<OrderData>(LittlePrinceFirstOrderPath);
            CocktailRecipeData dryMartini = FindRecipe(catalog, "recipe_dry_martini");
            CocktailRecipeData incompatibleStir = FindRecipe(catalog, "recipe_test_stir");

            scene = EditorSceneManager.OpenScene(BarScenePath, OpenSceneMode.Additive);
            ValidateEntryAndDaySchedule(scene, dryMartiniAttempt);
            ValidateLifecycleReset();
            ValidateGenericRecommendationContext(
                testStirTemplate,
                dryMartini,
                incompatibleStir);
            ValidateExactRequestProjection(dryMartiniAttempt, littlePrinceFirst, dryMartini);
            ValidateAuthoredRecommendationSelection();
            ValidateNightDiscoveryDelta(catalog, dryMartini);
            ValidateFinalNightCompletion(scene);
            ValidateReadStateContract();

            Debug.Log(
                "[J1 Validation] PASS: bounded New Game/reset state, five-anchor Day 1, " +
                "generic Recommendation request preservation, exact-request isolation, " +
                "night-local recipe discovery reporting, four-night completion, and " +
                "technique Guide read-state wiring are coherent.");
        }
        finally
        {
            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);

            snapshot.Restore();
        }
    }

    /// <summary>
    /// Static/edit-mode regression set for the I10-I16 authority contracts
    /// touched by J1. Existing Play Mode and human-feel commands remain their
    /// own evidence routes and are intentionally not folded into this menu.
    /// </summary>
    [MenuItem("SURLHWA/Validation/Run J1 + I10-I16 Static Regression Set")]
    public static void RunStaticRegressionSet()
    {
        SURLHWAIngredientAvailabilityValidation.Run();
        SURLHWARecipeKnowledgeValidation.Run();
        SURLHWARecipeKnowledgeDiscoveryValidation.Run();
        SURLHWADryMartiniDiscoveryArcValidation.Run();
        SURLHWAGuestFitValidation.Run();
        SURLHWALittlePrinceNarrativeValidation.Run();
        SURLHWAServiceFeedbackValidation.Run();
        SURLHWABartendingFeelValidation.Run();
        SURLHWATwoNightVerticalSliceValidation.Run();
        Run();

        Debug.Log(
            "[J1 Regression Set] PASS: I10-I16 static authority checks and the J1 coherent-demo flow check passed.");
    }

    private static void ValidateEntryAndDaySchedule(Scene scene, OrderData dryMartiniAttempt)
    {
        Require(scene.IsValid() && scene.isLoaded, "The shipped BarScene could not be opened.");

        GameManager gameManager = FindSingleInScene<GameManager>(scene);
        BusinessDayManager dayManager = FindSingleInScene<BusinessDayManager>(scene);
        CustomerSpawner spawner = FindSingleInScene<CustomerSpawner>(scene);
        InteractionUIController interaction = FindSingleInScene<InteractionUIController>(scene);

        Require(gameManager != null && dayManager != null && spawner != null && interaction != null,
            "BarScene is missing a J1 lifecycle or customer-routing controller.");
        Require(typeof(GameManager).GetMethod("ReturnToDemoEntry", BindingFlags.Static | BindingFlags.Public) != null,
            "GameManager has no bounded return-to-entry operation.");
        Require(typeof(GameManager).GetMethod("ContinueDemoGameplay", BindingFlags.Static | BindingFlags.Public) != null,
            "A normal Day Result cannot resume the selected demo run.");
        // The static suite may follow I16, which correctly restores an
        // arbitrary local save day. Inspect Day-1 authoring directly without
        // writing PlayerPrefs or assuming the caller's current progress.
        SetPrivateField(dayManager, "currentDay", 1);
        Require(dayManager.DemoNightCount == 4,
            "The bounded production demo must configure four nights.");
        Require(dayManager.MaxCustomersPerDay == 5,
            "Day 1 must end after its five authored anchors, not enter the legacy tutorial tail.");

        OrderData[] discoveryOrders = GetPrivateField<OrderData[]>(spawner, "dayOneDiscoveryOrders");
        OrderData recommendation = GetPrivateField<OrderData>(spawner, "dayOneRecommendationOrder");
        Require(discoveryOrders != null && discoveryOrders.Length == 4 &&
                discoveryOrders[0] != null && discoveryOrders[1] != null &&
                discoveryOrders[2] != null && discoveryOrders[3] == dryMartiniAttempt &&
                recommendation != null && recommendation.RequestType == GuestRequestType.Recommendation,
            "Day 1 must preserve its three clue encounters, exact experiment, and Clarity recommendation.");

        Require(typeof(OrderManager).GetMethod(
                    "PlayGenericRecommendationOrder",
                    BindingFlags.Instance | BindingFlags.Public) != null,
            "Interaction routing has no generic Recommendation entry point.");
    }

    private static void ValidateLifecycleReset()
    {
        SurlhwaGameProgress.ResetGameProgress();
        Require(!SurlhwaGameProgress.HasMeaningfulProgress(),
            "Fresh state must not be reported as a meaningful Continue save.");

        BusinessDayPersistence.SaveCurrentDay(3);
        RecipeProgressPersistence.SaveRecipeKnowledgeRaw("{\"version\":1,\"entries\":[]}");
        RecipeProgressPersistence.SaveUnlockedRecipeIdsRaw("recipe_dry_martini");
        Require(RecipeProgressPersistence.TryMarkFirstRecipeRevealShown(),
            "Controlled lifecycle setup could not write first-reveal state.");
        LittlePrinceVisitPersistence.SetFirstVisitCompleted(true);
        LittlePrinceVisitPersistence.SetFollowupVisitCompleted(true);
        Require(LittlePrinceNarrativePersistence.TryCommitChoice(LittlePrinceNarrativeChoice.KeepTheLight),
            "Controlled lifecycle setup could not write Little Prince choice.");
        DemoCompletionPersistence.MarkDemoComplete();

        Require(SurlhwaGameProgress.HasMeaningfulProgress(),
            "A populated game progression state did not enable Continue.");

        SurlhwaGameProgress.ResetGameProgress();
        Require(!BusinessDayPersistence.HasSavedCurrentDay() &&
                !RecipeProgressPersistence.HasRecipeKnowledge() &&
                !RecipeProgressPersistence.HasLegacyUnlockedRecipeIds() &&
                !RecipeProgressPersistence.HasShownFirstRecipeReveal() &&
                !LittlePrinceVisitPersistence.HasSavedFirstVisitState() &&
                !LittlePrinceVisitPersistence.HasSavedFollowupVisitState() &&
                !LittlePrinceNarrativePersistence.HasSavedChoice() &&
                !DemoCompletionPersistence.HasSavedDemoCompletionState() &&
                !SurlhwaGameProgress.HasMeaningfulProgress(),
            "New Game reset left a SURLHWA-owned gameplay progression key behind.");

        BusinessDayPersistence.SaveCurrentDay(1);
        Require(BusinessDayPersistence.LoadCurrentDay(1) == 1 &&
                SurlhwaGameProgress.HasMeaningfulProgress(),
            "New Game must establish Day 1 as a valid Continue state.");
    }

    private static void ValidateGenericRecommendationContext(
        OrderData testStirTemplate,
        CocktailRecipeData dryMartini,
        CocktailRecipeData incompatibleStir)
    {
        Require(testStirTemplate.RequestType == GuestRequestType.ExactCocktail &&
                testStirTemplate.requestedCocktail == dryMartini.targetCocktail,
            "The retained TestStir method template no longer exposes the exact-target contamination this test guards.");

        GuestRequestContext request = GuestRequestContext.CreateRecommendation(
            new[] { EmotionTag.Clarity },
            "generic clarity request",
            testStirTemplate.reputationReward);

        Require(request.RequestType == GuestRequestType.Recommendation &&
                request.RequestedCocktail == null &&
                request.ReputationReward == testStirTemplate.reputationReward,
            "Generic method selection did not retain a standalone Recommendation request context.");
        Require(testStirTemplate.requestedCocktail == dryMartini.targetCocktail,
            "Generic request preservation must not mutate the shared TestStir asset.");

        RequestEvaluationResult fitting = RequestEvaluation.Evaluate(request, Matched(dryMartini));
        RequestEvaluationResult incompatible = RequestEvaluation.Evaluate(request, Matched(incompatibleStir));
        Require(fitting.Status == RequestEvaluationStatus.Satisfied &&
                incompatible.Status == RequestEvaluationStatus.Unsatisfied,
            "Generic Clarity FIT did not survive Stir method selection as a Recommendation.");
    }

    private static void ValidateExactRequestProjection(
        OrderData dryMartiniAttempt,
        OrderData littlePrinceFirst,
        CocktailRecipeData dryMartini)
    {
        GuestRequestContext dryMartiniContext = GuestRequestContext.FromOrder(dryMartiniAttempt);
        GuestRequestContext princeContext = GuestRequestContext.FromOrder(littlePrinceFirst);

        Require(dryMartiniContext.RequestType == GuestRequestType.ExactCocktail &&
                dryMartiniContext.RequestedCocktail == dryMartini.targetCocktail,
            "Dry Martini experiment must remain an authored ExactCocktail request.");
        Require(princeContext.RequestType == GuestRequestType.ExactCocktail &&
                princeContext.RequestedCocktail == littlePrinceFirst.requestedCocktail,
            "Little Prince Manwol request must remain an authored ExactCocktail request.");
    }

    private static void ValidateAuthoredRecommendationSelection()
    {
        OrderData request = LoadRequired<OrderData>(
            "Assets/_SURLHWA/Data/Orders/ClarityRecommendationOrder.asset");
        GameObject host = new("J3 Authored Recommendation Routing Validation")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        try
        {
            CustomerController customer = host.AddComponent<CustomerController>();
            customer.ConfigureDedicatedEncounter(request);
            // An unrelated generated mood must never replace authored intent.
            GetPrivateField<GuestEmotionRequest>(customer, "emotionRequest")
                .Set(EmotionTag.Bitterness, EmotionTag.Warmth);
            EmotionTag[] tags = customer.GetDesiredEmotionTags();
            Require(tags.Length == 1 && tags[0] == EmotionTag.Clarity &&
                    customer.GetInteractionHintLine() == request.orderName,
                "Authored Recommendation intent was replaced by generic customer mood.");

            InteractionUIController interaction = host.AddComponent<InteractionUIController>();
            SetPrivateField(interaction, "activeCustomer", customer);
            Require(!(bool)InvokePrivate(interaction, "TryStartDedicatedRequest", null),
                "A dedicated Recommendation must open method selection, not force its template method.");
            Require(typeof(OrderManager).GetMethod("PlayAuthoredRecommendationOrder") != null,
                "Authored requests need a method-independent OrderManager entry point.");
            GuestRequestContext context = GuestRequestContext.FromOrder(request);
            Require(context.RequestType == GuestRequestType.Recommendation &&
                    context.RequestedCocktail == null &&
                    context.DisplayTitle == request.orderName &&
                    context.ReputationReward == request.reputationReward,
                "Authored Recommendation projection lost request semantics.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    private static void ValidateNightDiscoveryDelta(
        RecipeCatalogData catalog,
        CocktailRecipeData dryMartini)
    {
        SurlhwaGameProgress.ResetGameProgress();
        RecipeUnlockManager knowledge = CreateKnowledgeManager(catalog);
        GameObject dayHost = null;

        try
        {
            dayHost = new GameObject("J1 Day Result Delta Validation")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            BusinessDayManager dayManager = dayHost.AddComponent<BusinessDayManager>();
            SetPrivateField(dayManager, "recipeUnlockManager", knowledge);

            InvokePrivate(dayManager, "CaptureKnownRecipesAtNightStart", null);
            Require(knowledge.MarkKnown(dryMartini),
                "Controlled Day Result proof could not discover Dry Martini during the night.");

            List<string> discovered = (List<string>)InvokePrivate(
                dayManager,
                "GetNewlyKnownRecipeNames",
                null);
            Require(discovered.Count == 1 && discovered[0] == dryMartini.targetCocktail.cocktailName,
                "Day Result must list exactly the recipe made Known during this night.");

            InvokePrivate(dayManager, "CaptureKnownRecipesAtNightStart", null);
            List<string> nextNightDelta = (List<string>)InvokePrivate(
                dayManager,
                "GetNewlyKnownRecipeNames",
                null);
            Require(nextNightDelta.Count == 0,
                "A recipe Known before the next night must not be reported as newly discovered again.");
        }
        finally
        {
            if (dayHost != null)
                UnityEngine.Object.DestroyImmediate(dayHost);

            DestroyKnowledgeManager(knowledge);
        }
    }

    private static void ValidateFinalNightCompletion(Scene scene)
    {
        SurlhwaGameProgress.ResetGameProgress();
        BusinessDayPersistence.SaveCurrentDay(4);

        GameObject host = new GameObject("J1 Final Night Validation")
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        try
        {
            BusinessDayManager dayManager = host.AddComponent<BusinessDayManager>();
            InvokePrivate(dayManager, "Awake", null);
            Require(dayManager.IsConfiguredFinalDemoNight && dayManager.MaxCustomersPerDay >= 1,
                "Day 4 must remain the configured final demo night with a valid authorable customer target.");

            for (int index = 0; index < dayManager.MaxCustomersPerDay; index++)
                dayManager.NotifyCustomerServedOrLeft();

            Require(DemoCompletionPersistence.IsDemoComplete() &&
                    BusinessDayPersistence.LoadCurrentDay(1) == 4 &&
                    !dayManager.CanSpawnMoreCustomers(),
                "Completing Day 4 must set DemoComplete and block normal Day-5 progression.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    private static void ValidateReadStateContract()
    {
        PropertyInfo isReading = typeof(RecipeGuidePanelController).GetProperty(
            "IsReading",
            BindingFlags.Instance | BindingFlags.Public);
        PropertyInfo readStateActive = typeof(RecipeGuidePanelController).GetProperty(
            "IsTechniqueReadStateActive",
            BindingFlags.Static | BindingFlags.Public);
        MethodInfo beginPause = typeof(RecipeGuidePanelController).GetMethod(
            "BeginReadStatePause",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo endPause = typeof(RecipeGuidePanelController).GetMethod(
            "EndReadStatePause",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Require(isReading != null && readStateActive != null && beginPause != null && endPause != null,
            "Technique Guide no longer exposes the bounded read-state pause contract.");
    }

    private static RecipeUnlockManager CreateKnowledgeManager(RecipeCatalogData catalog)
    {
        GameObject host = new GameObject("J1 Recipe Knowledge Validation")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        RecipeUnlockManager manager = host.AddComponent<RecipeUnlockManager>();
        SetPrivateField(manager, "recipeCatalog", catalog);
        InvokePrivate(manager, "Awake", null);
        return manager;
    }

    private static void DestroyKnowledgeManager(RecipeUnlockManager manager)
    {
        if (manager != null)
            UnityEngine.Object.DestroyImmediate(manager.gameObject);
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
            if (entry?.Recipe != null && entry.Recipe.recipeId == recipeId)
                return entry.Recipe;
        }

        throw new InvalidOperationException("[J1 Validation] Missing recipe '" + recipeId + "'.");
    }

    private static T LoadRequired<T>(string path) where T : UnityEngine.Object
    {
        T value = AssetDatabase.LoadAssetAtPath<T>(path);
        Require(value != null, "Missing required production asset at " + path + ".");
        return value;
    }

    private static T FindSingleInScene<T>(Scene scene) where T : Component
    {
        T found = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (T candidate in root.GetComponentsInChildren<T>(true))
            {
                Require(found == null,
                    "BarScene contains more than one active or inactive " + typeof(T).Name + ".");
                found = candidate;
            }
        }

        return found;
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        FieldInfo field = instance.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(field != null,
            instance.GetType().Name + " is missing private field '" + fieldName + "'.");
        return (T)field.GetValue(instance);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(field != null,
            instance.GetType().Name + " is missing private field '" + fieldName + "'.");
        field.SetValue(instance, value);
    }

    private static object InvokePrivate(object instance, string methodName, object[] arguments)
    {
        MethodInfo method = instance.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(method != null,
            instance.GetType().Name + " is missing private method '" + methodName + "'.");
        return method.Invoke(instance, arguments);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("[J1 Validation] " + message);
    }

    private readonly struct ProgressSnapshot
    {
        private const string FirstRecipeRevealKey = "SURLHWA_FIRST_RECIPE_REVEAL_SHOWN";

        private readonly bool hadCurrentDay;
        private readonly int currentDay;
        private readonly bool hadKnowledge;
        private readonly string knowledge;
        private readonly bool hadLegacyUnlocks;
        private readonly string legacyUnlocks;
        private readonly bool hadFirstRecipeReveal;
        private readonly int firstRecipeReveal;
        private readonly bool hadFirstVisit;
        private readonly bool firstVisitCompleted;
        private readonly bool hadFollowupVisit;
        private readonly bool followupVisitCompleted;
        private readonly bool hadChoice;
        private readonly LittlePrinceNarrativeChoice choice;
        private readonly bool hadDemoCompletion;

        private ProgressSnapshot(
            bool hadCurrentDay,
            int currentDay,
            bool hadKnowledge,
            string knowledge,
            bool hadLegacyUnlocks,
            string legacyUnlocks,
            bool hadFirstRecipeReveal,
            int firstRecipeReveal,
            bool hadFirstVisit,
            bool firstVisitCompleted,
            bool hadFollowupVisit,
            bool followupVisitCompleted,
            bool hadChoice,
            LittlePrinceNarrativeChoice choice,
            bool hadDemoCompletion)
        {
            this.hadCurrentDay = hadCurrentDay;
            this.currentDay = currentDay;
            this.hadKnowledge = hadKnowledge;
            this.knowledge = knowledge;
            this.hadLegacyUnlocks = hadLegacyUnlocks;
            this.legacyUnlocks = legacyUnlocks;
            this.hadFirstRecipeReveal = hadFirstRecipeReveal;
            this.firstRecipeReveal = firstRecipeReveal;
            this.hadFirstVisit = hadFirstVisit;
            this.firstVisitCompleted = firstVisitCompleted;
            this.hadFollowupVisit = hadFollowupVisit;
            this.followupVisitCompleted = followupVisitCompleted;
            this.hadChoice = hadChoice;
            this.choice = choice;
            this.hadDemoCompletion = hadDemoCompletion;
        }

        public static ProgressSnapshot Capture()
        {
            return new ProgressSnapshot(
                BusinessDayPersistence.HasSavedCurrentDay(),
                BusinessDayPersistence.LoadCurrentDay(1),
                RecipeProgressPersistence.HasRecipeKnowledge(),
                RecipeProgressPersistence.LoadRecipeKnowledgeRaw(),
                RecipeProgressPersistence.HasLegacyUnlockedRecipeIds(),
                RecipeProgressPersistence.LoadLegacyUnlockedRecipeIdsRaw(),
                SurlhwaPlayerPrefsStore.HasKey(FirstRecipeRevealKey),
                SurlhwaPlayerPrefsStore.GetInt(FirstRecipeRevealKey, 0),
                LittlePrinceVisitPersistence.HasSavedFirstVisitState(),
                LittlePrinceVisitPersistence.IsFirstVisitCompleted(),
                LittlePrinceVisitPersistence.HasSavedFollowupVisitState(),
                LittlePrinceVisitPersistence.IsFollowupVisitCompleted(),
                LittlePrinceNarrativePersistence.HasSavedChoice(),
                LittlePrinceNarrativePersistence.LoadChoice(),
                DemoCompletionPersistence.HasSavedDemoCompletionState());
        }

        public void Restore()
        {
            RestoreInt(hadCurrentDay, currentDay, "currentDay");

            if (hadKnowledge)
                RecipeProgressPersistence.SaveRecipeKnowledgeRaw(knowledge);
            else
                RecipeProgressPersistence.ClearRecipeKnowledge();

            if (hadLegacyUnlocks)
                RecipeProgressPersistence.SaveUnlockedRecipeIdsRaw(legacyUnlocks);
            else
                RecipeProgressPersistence.ClearLegacyUnlockedRecipeIds();

            RestoreInt(hadFirstRecipeReveal, firstRecipeReveal, FirstRecipeRevealKey);

            if (hadFirstVisit)
                LittlePrinceVisitPersistence.SetFirstVisitCompleted(firstVisitCompleted);
            else
                LittlePrinceVisitPersistence.ClearFirstVisitCompleted();

            if (hadFollowupVisit)
                LittlePrinceVisitPersistence.SetFollowupVisitCompleted(followupVisitCompleted);
            else
                LittlePrinceVisitPersistence.ClearFollowupVisitCompleted();

            LittlePrinceNarrativePersistence.ClearChoice();
            if (hadChoice && choice != LittlePrinceNarrativeChoice.None)
                LittlePrinceNarrativePersistence.TryCommitChoice(choice);

            if (hadDemoCompletion)
                DemoCompletionPersistence.MarkDemoComplete();
            else
                DemoCompletionPersistence.ClearDemoCompletion();

            SurlhwaPlayerPrefsStore.Save();
        }

        private static void RestoreInt(bool hadValue, int value, string key)
        {
            if (hadValue)
                SurlhwaPlayerPrefsStore.SetInt(key, value);
            else
                SurlhwaPlayerPrefsStore.DeleteKey(key);
        }
    }
}
#endif
