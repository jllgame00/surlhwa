#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Narrow I16 production-data and progression proof. This intentionally uses
/// the shipped BarScene, its assigned controllers, and the existing I10-I13
/// authorities. It does not introduce a second day, knowledge, request, or
/// Little Prince implementation. The dedicated I13/I14 Play Mode validators
/// remain the coroutine/UI counterparts for their respective interaction
/// surfaces.
/// </summary>
public static class SURLHWATwoNightVerticalSliceValidation
{
    private const string BarScenePath = "Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity";
    private const string CatalogPath = "Assets/_SURLHWA/Data/Recipes/RecipeCatalog.asset";
    private const string AvailabilityPath =
        "Assets/_SURLHWA/Data/Ingredients/IngredientAvailability.asset";
    private const string NarrativePath =
        "Assets/_SURLHWA/Data/Narrative/LittlePrinceNarrative.asset";
    private const string LittlePrincePrefabPath =
        "Assets/_SURLHWA/Prefabs/Customers/SpecialGuests/LittlePrinceCustomer.prefab";

    private static readonly string[] DiscoveryOrderPaths =
    {
        "Assets/_SURLHWA/Data/Orders/DryMartiniMethodClueOrder.asset",
        "Assets/_SURLHWA/Data/Orders/DryMartiniGinClueOrder.asset",
        "Assets/_SURLHWA/Data/Orders/DryMartiniVermouthClueOrder.asset",
        "Assets/_SURLHWA/Data/Orders/DryMartiniAttemptOrder.asset"
    };

    private const string RecommendationOrderPath =
        "Assets/_SURLHWA/Data/Orders/ClarityRecommendationOrder.asset";
    private const string LittlePrinceFirstOrderPath =
        "Assets/_SURLHWA/Data/Orders/LittlePrinceFirstVisitOrder.asset";
    private const string LittlePrinceFollowupOrderPath =
        "Assets/_SURLHWA/Data/Orders/LittlePrinceFollowupOrder.asset";

    [MenuItem("SURLHWA/Validation/Run I16 Two-Night Vertical Slice")]
    public static void Run()
    {
        SliceProgressSnapshot snapshot = SliceProgressSnapshot.Capture();
        Scene scene = default;

        try
        {
            RecipeCatalogData catalog = LoadRequired<RecipeCatalogData>(CatalogPath);
            IngredientAvailabilityData availability =
                LoadRequired<IngredientAvailabilityData>(AvailabilityPath);
            LittlePrinceNarrativeData narrative =
                LoadRequired<LittlePrinceNarrativeData>(NarrativePath);
            GameObject littlePrincePrefab = LoadRequired<GameObject>(LittlePrincePrefabPath);
            OrderData[] discoveryOrders = LoadOrders(DiscoveryOrderPaths);
            OrderData recommendation = LoadRequired<OrderData>(RecommendationOrderPath);
            OrderData littlePrinceFirst = LoadRequired<OrderData>(LittlePrinceFirstOrderPath);
            OrderData littlePrinceFollowup = LoadRequired<OrderData>(LittlePrinceFollowupOrderPath);

            CocktailRecipeData dryMartini = FindRecipe(catalog, "recipe_dry_martini");
            CocktailRecipeData manwol = FindRecipe(catalog, "recipe_manwol");
            CocktailRecipeData incompatibleStirRecipe = FindRecipe(catalog, "recipe_test_stir");

            scene = EditorSceneManager.OpenScene(BarScenePath, OpenSceneMode.Additive);
            ValidateSceneWiring(
                scene,
                catalog,
                discoveryOrders,
                recommendation,
                littlePrincePrefab,
                littlePrinceFollowup);
            ValidateDayOneKnowledgeAndAvailability(
                catalog,
                availability,
                dryMartini,
                discoveryOrders);
            ValidateRecommendationBoundary(recommendation, dryMartini, incompatibleStirRecipe, catalog);
            ValidateDayTransitionPersistence(dryMartini, catalog);
            ValidateLittlePrinceDayTwoAndFollowup(
                scene,
                narrative,
                littlePrinceFirst,
                littlePrinceFollowup,
                manwol);

            Debug.Log(
                "[I16 Validation] PASS: shipped BarScene wiring, Day-1 Dry Martini " +
                "Unknown -> Partial -> Known progression, recommendation FIT separation, " +
                "Day-2 persistence, Little Prince choice/Manwol context, and Day-3 branch " +
                "eligibility are coherent. Existing I13/I14 Play Mode validators own their " +
                "actual coroutine/UI interaction proofs.");
        }
        finally
        {
            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);

            snapshot.Restore();
        }
    }

    private static void ValidateSceneWiring(
        Scene scene,
        RecipeCatalogData catalog,
        OrderData[] discoveryOrders,
        OrderData recommendation,
        GameObject littlePrincePrefab,
        OrderData littlePrinceFollowup)
    {
        Require(scene.IsValid() && scene.isLoaded, "The shipped BarScene could not be opened.");

        CustomerSpawner spawner = FindSingleInScene<CustomerSpawner>(scene);
        BusinessDayManager dayManager = FindSingleInScene<BusinessDayManager>(scene);
        OrderManager orderManager = FindSingleInScene<OrderManager>(scene);
        BartendingFlowCoordinator flow = FindSingleInScene<BartendingFlowCoordinator>(scene);
        InteractionUIController interactionUi = FindSingleInScene<InteractionUIController>(scene);
        RecipeUnlockManager knowledge = FindSingleInScene<RecipeUnlockManager>(scene);
        ResultToastController toast = FindSingleInScene<ResultToastController>(scene);
        RecipeUnlockPopupController popup = FindSingleInScene<RecipeUnlockPopupController>(scene);
        DayResultPanelController dayResult = FindSingleInScene<DayResultPanelController>(scene);

        Require(spawner != null && dayManager != null && orderManager != null && flow != null &&
                interactionUi != null && knowledge != null && toast != null &&
                popup != null && dayResult != null,
            "BarScene is missing a required production controller for the two-night slice.");

        Require(GetPrivateField<RecipeCatalogData>(knowledge, "recipeCatalog") == catalog,
            "BarScene RecipeUnlockManager no longer references the authoritative recipe catalog.");
        Require(GetPrivateField<BusinessDayManager>(spawner, "businessDayManager") == dayManager,
            "CustomerSpawner no longer references the authoritative BusinessDayManager.");
        Require(GetPrivateField<DayResultPanelController>(dayManager, "resultPanel") == dayResult,
            "BusinessDayManager no longer references the shipped Day Result panel.");
        Require(GetPrivateField<int>(dayManager, "maxCustomersPerDay") == 8,
            "The shipped day length changed; I16 expects the authored five-customer arc plus three ordinary services.");

        OrderData[] assignedDiscoveryOrders = GetPrivateField<OrderData[]>(spawner, "dayOneDiscoveryOrders");
        Require(ReferenceSequenceEquals(assignedDiscoveryOrders, discoveryOrders),
            "CustomerSpawner no longer assigns the complete ordered Day-1 discovery arc.");
        Require(GetPrivateField<OrderData>(spawner, "dayOneRecommendationOrder") == recommendation,
            "CustomerSpawner no longer schedules the authored Day-1 Clarity recommendation after the discovery arc.");
        Require(GetPrivateField<GameObject>(spawner, "littlePrincePrefab") == littlePrincePrefab &&
                GetPrivateField<int>(spawner, "littlePrinceFollowupDay") == 3 &&
                GetPrivateField<OrderData>(spawner, "littlePrinceFollowupOrder") == littlePrinceFollowup,
            "CustomerSpawner Little Prince first/follow-up production scheduling drifted from I13.");
    }

    private static void ValidateDayOneKnowledgeAndAvailability(
        RecipeCatalogData catalog,
        IngredientAvailabilityData availability,
        CocktailRecipeData dryMartini,
        OrderData[] discoveryOrders)
    {
        foreach (RecipeIngredientEntry entry in dryMartini.ingredients)
        {
            Require(entry != null && entry.ingredient != null &&
                    availability.IsIngredientAvailable(entry.ingredient, new IngredientAvailabilityContext(1)),
                "Dry Martini must remain physically constructible from the Day-1 shelf.");
        }

        ValidateDiscoveryOrder(discoveryOrders[0], dryMartini, RecipeKnowledgeFact.Method, null);
        ValidateDiscoveryOrder(discoveryOrders[1], dryMartini, RecipeKnowledgeFact.Ingredient,
            FindIngredient(dryMartini, "gin"));
        ValidateDiscoveryOrder(discoveryOrders[2], dryMartini, RecipeKnowledgeFact.Ingredient,
            FindIngredient(dryMartini, "dry_vermouth"));
        Require(discoveryOrders[3].PreBrewDialogue != null &&
                discoveryOrders[3].PreBrewDialogue.GetOnEnterKnowledgeReveals(0).Length == 0,
            "The final Dry Martini attempt must be a discovery opportunity, not another automatic fact reveal.");

        RecipeProgressPersistence.ClearRecipeProgress();
        RecipeUnlockManager manager = CreateKnowledgeManager(catalog);
        try
        {
            IngredientData gin = FindIngredient(dryMartini, "gin");
            IngredientData dryVermouth = FindIngredient(dryMartini, "dry_vermouth");
            RecipeKnowledgeFacts unknown = manager.GetKnownFacts(dryMartini);
            Require(unknown.State == RecipeKnowledgeState.Unknown &&
                    RecipeKnowledgeGuideFormatter.GetCardLabel(dryMartini, unknown) == "???",
                "Dry Martini must begin the controlled Day-1 slice Unknown without a Guide identity leak.");

            ApplyFirstReveal(discoveryOrders[0], manager);
            RecipeKnowledgeFacts afterMethod = manager.GetKnownFacts(dryMartini);
            Require(afterMethod.State == RecipeKnowledgeState.Partial && afterMethod.MethodKnown &&
                    !afterMethod.IdentityKnown && !afterMethod.IsIngredientKnown(gin),
                "Encounter 1 did not produce only the authored Stir Partial fact.");

            ApplyFirstReveal(discoveryOrders[1], manager);
            RecipeKnowledgeFacts afterGin = manager.GetKnownFacts(dryMartini);
            Require(afterGin.MethodKnown && afterGin.IsIngredientKnown(gin) &&
                    !afterGin.IsIngredientAmountKnown(gin) && !afterGin.IdentityKnown,
                "Encounter 2 did not add Gin without amount or identity leakage.");

            ApplyFirstReveal(discoveryOrders[2], manager);
            RecipeKnowledgeFacts richerPartial = manager.GetKnownFacts(dryMartini);
            Require(richerPartial.State == RecipeKnowledgeState.Partial && richerPartial.MethodKnown &&
                    richerPartial.IsIngredientKnown(gin) && richerPartial.IsIngredientKnown(dryVermouth) &&
                    !richerPartial.IdentityKnown && !richerPartial.IsIngredientAmountKnown(gin) &&
                    !richerPartial.IsIngredientAmountKnown(dryVermouth),
                "Encounter 3 did not leave the expected richer Partial knowledge state.");

            RecipeMatchResult failedMatch = RecipeMatchService.FindBestMatch(
                new BrewComposition
                {
                    method = MiniGameType.Stir,
                    ingredients = new[]
                    {
                        new BrewIngredientEntry { ingredientId = gin.ingredientId, amount = 2.03f }
                    }
                },
                manager.GetAllRecipesByMethod(MiniGameType.Stir));
            Require(!failedMatch.success && manager.GetKnowledgeState(dryMartini) == RecipeKnowledgeState.Partial,
                "A failed Day-1 attempt discovered or erased Dry Martini knowledge.");

            RecipeMatchResult exactMatch = RecipeMatchService.FindBestMatch(
                BuildExactComposition(dryMartini), manager.GetAllRecipesByMethod(MiniGameType.Stir));
            Require(exactMatch.success && exactMatch.matchedRecipe == dryMartini &&
                    manager.MarkKnown(dryMartini) && manager.IsKnown(dryMartini),
                "The exact Day-1 Dry Martini composition did not promote Partial knowledge to Known.");

            DestroyKnowledgeManager(manager);
            manager = CreateKnowledgeManager(catalog);
            Require(manager.IsKnown(dryMartini),
                "Known Dry Martini did not survive the Day-1 reload boundary.");
        }
        finally
        {
            DestroyKnowledgeManager(manager);
        }
    }

    private static void ValidateRecommendationBoundary(
        OrderData recommendation,
        CocktailRecipeData dryMartini,
        CocktailRecipeData incompatibleStirRecipe,
        RecipeCatalogData catalog)
    {
        Require(recommendation.RequestType == GuestRequestType.Recommendation &&
                recommendation.requestedCocktail == null &&
                Contains(recommendation.DesiredEmotionTags, EmotionTag.Clarity),
            "The Day-1 recommendation request must remain a non-leaking authored Clarity request.");

        RecipeMatchResult dryMartiniMatch = RecipeMatchService.FindBestMatch(
            BuildExactComposition(dryMartini), GetRecipesByMethod(catalog, MiniGameType.Stir));
        RecipeMatchResult incompatibleMatch = RecipeMatchService.FindBestMatch(
            BuildExactComposition(incompatibleStirRecipe), GetRecipesByMethod(catalog, MiniGameType.Stir));
        Require(dryMartiniMatch.success && incompatibleMatch.success,
            "Representative recommendation compositions must still reach RecipeMatchService.");
        Require(RequestEvaluation.Evaluate(recommendation, dryMartiniMatch).Status ==
                    RequestEvaluationStatus.Satisfied &&
                RequestEvaluation.Evaluate(recommendation, incompatibleMatch).Status ==
                    RequestEvaluationStatus.Unsatisfied,
            "Recommendation FIT no longer distinguishes the fitting Dry Martini from the recognized incompatible Stir drink.");
    }

    private static void ValidateDayTransitionPersistence(
        CocktailRecipeData dryMartini,
        RecipeCatalogData catalog)
    {
        BusinessDayPersistence.SaveCurrentDay(1);
        Require(BusinessDayPersistence.LoadCurrentDay(1) == 1,
            "Controlled vertical-slice state could not begin on Day 1.");
        BusinessDayPersistence.SaveCurrentDay(2);
        Require(BusinessDayPersistence.LoadCurrentDay(1) == 2,
            "The production current-day persistence boundary did not retain Day 2.");

        RecipeUnlockManager reloadedKnowledge = CreateKnowledgeManager(catalog);
        try
        {
            Require(reloadedKnowledge.IsKnown(dryMartini),
                "Day transition persistence reset Dry Martini knowledge.");
        }
        finally
        {
            DestroyKnowledgeManager(reloadedKnowledge);
        }
    }

    private static void ValidateLittlePrinceDayTwoAndFollowup(
        Scene scene,
        LittlePrinceNarrativeData narrative,
        OrderData littlePrinceFirst,
        OrderData littlePrinceFollowup,
        CocktailRecipeData manwol)
    {
        Require(littlePrinceFirst.RequestType == GuestRequestType.ExactCocktail &&
                littlePrinceFirst.requestedCocktail == manwol.targetCocktail &&
                littlePrinceFirst.miniGameType == MiniGameType.Float,
            "Little Prince first visit no longer enters the exact Manwol Float route.");

        BartendingRequestContext manwolContext = new(
            littlePrinceFirst.miniGameType,
            littlePrinceFirst.orderName,
            manwol,
            2);
        foreach (RecipeIngredientEntry entry in manwol.ingredients)
        {
            Require(entry != null && entry.ingredient != null &&
                    manwolContext.AvailabilityContext.IsTemporarilyAvailable(entry.ingredient),
                "The Day-2 Manwol request no longer receives its request-local liquid availability override.");
        }

        LittlePrinceVisitPersistence.ClearFirstVisitCompleted();
        LittlePrinceVisitPersistence.ClearFollowupVisitCompleted();
        LittlePrinceNarrativePersistence.ClearChoice();
        Require(LittlePrinceNarrativePersistence.LoadChoice() == LittlePrinceNarrativeChoice.None &&
                !LittlePrinceVisitPersistence.IsFirstVisitCompleted(),
            "Controlled Day-2 Little Prince state did not start fresh.");

        CustomerSpawner spawner = FindSingleInScene<CustomerSpawner>(scene);
        BusinessDayManager dayManager = FindSingleInScene<BusinessDayManager>(scene);
        Require(spawner != null && dayManager != null,
            "BarScene is missing the production scheduling controllers for the Little Prince proof.");
        SetPrivateField(dayManager, "currentDay", 2);
        SetPrivateField(spawner, "spawnedCount", 0);
        Require(InvokePrivateBool(spawner, "ShouldSpawnLittlePrinceFirstVisit"),
            "Day-2 Little Prince first visit is not eligible from the controlled fresh state.");

        Require(LittlePrinceNarrativePersistence.TryCommitChoice(LittlePrinceNarrativeChoice.KeepTheLight) &&
                LittlePrinceNarrativePersistence.LoadChoice() == LittlePrinceNarrativeChoice.KeepTheLight &&
                !LittlePrinceVisitPersistence.IsFirstVisitCompleted(),
            "Little Prince choice was not persisted independently of first-visit completion.");

        LittlePrinceVisitPersistence.MarkFirstVisitCompleted();
        Require(LittlePrinceVisitPersistence.IsFirstVisitCompleted() &&
                narrative.GetFollowupDialogue(LittlePrinceNarrativePersistence.LoadChoice()) ==
                    narrative.GetFollowupDialogue(LittlePrinceNarrativeChoice.KeepTheLight),
            "Completed Day-2 Little Prince visit did not retain the authored KeepTheLight follow-up branch.");

        SetPrivateField(dayManager, "currentDay", 3);
        SetPrivateField(spawner, "spawnedCount", 0);
        bool dayThreeFollowupEligible = InvokePrivateBool(spawner, "ShouldSpawnLittlePrinceFollowupVisit");
        Require(dayThreeFollowupEligible,
            "Day-3 Little Prince follow-up is not eligible after the persisted Day-2 first visit.");

        Require(littlePrinceFollowup.RequestType == GuestRequestType.ExactCocktail &&
                littlePrinceFollowup.requestedCocktail == manwol.targetCocktail,
            "The Little Prince follow-up no longer preserves the exact Manwol request boundary.");
    }

    private static void ValidateDiscoveryOrder(
        OrderData order,
        CocktailRecipeData expectedRecipe,
        RecipeKnowledgeFact expectedFact,
        IngredientData expectedIngredient)
    {
        Require(order != null && order.miniGameType == MiniGameType.Stir &&
                order.requestedCocktail == expectedRecipe.targetCocktail && order.PreBrewDialogue != null,
            "A Day-1 discovery order no longer retains its authored Stir context.");
        Require(!Contains(order.orderName, "Dry Martini") &&
                !ContainsDialogue(order.PreBrewDialogue, "Dry Martini"),
            "A player-visible Day-1 discovery surface leaked Dry Martini before Known.");

        RecipeKnowledgeReveal[] reveals = order.PreBrewDialogue.GetOnEnterKnowledgeReveals(0);
        Require(reveals.Length == 1 && reveals[0] != null && reveals[0].Recipe == expectedRecipe &&
                reveals[0].Fact == expectedFact && reveals[0].Ingredient == expectedIngredient,
            "A Day-1 discovery clue no longer contains exactly its authored typed fact.");
    }

    private static void ApplyFirstReveal(OrderData order, RecipeUnlockManager manager)
    {
        RecipeKnowledgeReveal[] reveals = order.PreBrewDialogue.GetOnEnterKnowledgeReveals(0);
        Require(reveals.Length == 1 && reveals[0].TryApply(manager),
            "A consumed Day-1 dialogue line did not apply its authored fact through RecipeUnlockManager.");
    }

    private static BrewComposition BuildExactComposition(CocktailRecipeData recipe)
    {
        List<BrewIngredientEntry> ingredients = new();
        foreach (RecipeIngredientEntry entry in recipe.ingredients)
        {
            if (entry != null && entry.ingredient != null)
            {
                ingredients.Add(new BrewIngredientEntry
                {
                    ingredientId = entry.ingredient.ingredientId,
                    amount = entry.ratio
                });
            }
        }

        return new BrewComposition { method = recipe.method, ingredients = ingredients.ToArray() };
    }

    private static List<CocktailRecipeData> GetRecipesByMethod(
        RecipeCatalogData catalog,
        MiniGameType method)
    {
        List<CocktailRecipeData> recipes = new();
        foreach (RecipeCatalogEntry entry in catalog.Entries)
        {
            if (entry?.Recipe != null && entry.Recipe.method == method)
                recipes.Add(entry.Recipe);
        }

        return recipes;
    }

    private static RecipeUnlockManager CreateKnowledgeManager(RecipeCatalogData catalog)
    {
        GameObject host = new("I16 Recipe Knowledge Validation")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        RecipeUnlockManager manager = host.AddComponent<RecipeUnlockManager>();
        SerializedObject serializedManager = new(manager);
        serializedManager.FindProperty("recipeCatalog").objectReferenceValue = catalog;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();

        MethodInfo awake = typeof(RecipeUnlockManager).GetMethod(
            "Awake", BindingFlags.Instance | BindingFlags.NonPublic);
        Require(awake != null, "RecipeUnlockManager.Awake is missing.");
        awake.Invoke(manager, null);
        return manager;
    }

    private static void DestroyKnowledgeManager(RecipeUnlockManager manager)
    {
        if (manager != null)
            UnityEngine.Object.DestroyImmediate(manager.gameObject);
    }

    private static CocktailRecipeData FindRecipe(RecipeCatalogData catalog, string recipeId)
    {
        foreach (RecipeCatalogEntry entry in catalog.Entries)
        {
            if (entry?.Recipe != null && entry.Recipe.recipeId == recipeId)
                return entry.Recipe;
        }

        throw new InvalidOperationException("[I16 Validation] Missing recipe '" + recipeId + "'.");
    }

    private static IngredientData FindIngredient(CocktailRecipeData recipe, string ingredientId)
    {
        foreach (RecipeIngredientEntry entry in recipe.ingredients)
        {
            if (entry?.ingredient != null && entry.ingredient.ingredientId == ingredientId)
                return entry.ingredient;
        }

        throw new InvalidOperationException(
            "[I16 Validation] Missing ingredient '" + ingredientId + "' in '" + recipe.recipeId + "'.");
    }

    private static OrderData[] LoadOrders(string[] paths)
    {
        OrderData[] orders = new OrderData[paths.Length];
        for (int index = 0; index < paths.Length; index++)
            orders[index] = LoadRequired<OrderData>(paths[index]);

        return orders;
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
                Require(found == null, "BarScene contains more than one active or inactive " + typeof(T).Name + ".");
                found = candidate;
            }
        }

        return found;
    }

    private static bool ReferenceSequenceEquals<T>(T[] actual, T[] expected) where T : UnityEngine.Object
    {
        if (actual == null || expected == null || actual.Length != expected.Length)
            return false;

        for (int index = 0; index < actual.Length; index++)
        {
            if (actual[index] != expected[index])
                return false;
        }

        return true;
    }

    private static bool Contains<T>(IEnumerable<T> values, T expected)
    {
        foreach (T value in values)
        {
            if (EqualityComparer<T>.Default.Equals(value, expected))
                return true;
        }

        return false;
    }

    private static bool ContainsDialogue(DialogueData dialogue, string value)
    {
        if (dialogue == null || dialogue.onEnterLines == null)
            return false;

        foreach (string line in dialogue.onEnterLines)
        {
            if (Contains(line, value))
                return true;
        }

        return false;
    }

    private static bool Contains(string value, string expected)
    {
        return !string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(expected) &&
               value.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        FieldInfo field = instance.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(field != null, instance.GetType().Name + " is missing private field '" + fieldName + "'.");
        return (T)field.GetValue(instance);
    }

    private static void SetPrivateField<T>(object instance, string fieldName, T value)
    {
        FieldInfo field = instance.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(field != null, instance.GetType().Name + " is missing private field '" + fieldName + "'.");
        field.SetValue(instance, value);
    }

    private static bool InvokePrivateBool(object instance, string methodName)
    {
        MethodInfo method = instance.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(method != null, instance.GetType().Name + " is missing private method '" + methodName + "'.");
        return method.Invoke(instance, null) is bool result && result;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("[I16 Validation] " + message);
    }

    private readonly struct SliceProgressSnapshot
    {
        private const string FirstRecipeRevealKey = "SURLHWA_FIRST_RECIPE_REVEAL_SHOWN";

        private readonly bool hadCurrentDay;
        private readonly int currentDay;
        private readonly bool hadRecipeKnowledge;
        private readonly string recipeKnowledge;
        private readonly bool hadLegacyUnlocks;
        private readonly string legacyUnlocks;
        private readonly bool hadFirstRecipeReveal;
        private readonly int firstRecipeReveal;
        private readonly bool hadFirstVisit;
        private readonly bool firstVisitCompleted;
        private readonly bool hadFollowupVisit;
        private readonly bool followupVisitCompleted;
        private readonly bool hadNarrativeChoice;
        private readonly LittlePrinceNarrativeChoice narrativeChoice;

        private SliceProgressSnapshot(
            bool hadCurrentDay,
            int currentDay,
            bool hadRecipeKnowledge,
            string recipeKnowledge,
            bool hadLegacyUnlocks,
            string legacyUnlocks,
            bool hadFirstRecipeReveal,
            int firstRecipeReveal,
            bool hadFirstVisit,
            bool firstVisitCompleted,
            bool hadFollowupVisit,
            bool followupVisitCompleted,
            bool hadNarrativeChoice,
            LittlePrinceNarrativeChoice narrativeChoice)
        {
            this.hadCurrentDay = hadCurrentDay;
            this.currentDay = currentDay;
            this.hadRecipeKnowledge = hadRecipeKnowledge;
            this.recipeKnowledge = recipeKnowledge;
            this.hadLegacyUnlocks = hadLegacyUnlocks;
            this.legacyUnlocks = legacyUnlocks;
            this.hadFirstRecipeReveal = hadFirstRecipeReveal;
            this.firstRecipeReveal = firstRecipeReveal;
            this.hadFirstVisit = hadFirstVisit;
            this.firstVisitCompleted = firstVisitCompleted;
            this.hadFollowupVisit = hadFollowupVisit;
            this.followupVisitCompleted = followupVisitCompleted;
            this.hadNarrativeChoice = hadNarrativeChoice;
            this.narrativeChoice = narrativeChoice;
        }

        public static SliceProgressSnapshot Capture()
        {
            return new SliceProgressSnapshot(
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
                LittlePrinceNarrativePersistence.LoadChoice());
        }

        public void Restore()
        {
            RestoreInt(
                hadCurrentDay,
                currentDay,
                "currentDay");

            if (hadRecipeKnowledge)
                RecipeProgressPersistence.SaveRecipeKnowledgeRaw(recipeKnowledge);
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
            if (hadNarrativeChoice && narrativeChoice != LittlePrinceNarrativeChoice.None)
                LittlePrinceNarrativePersistence.TryCommitChoice(narrativeChoice);

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
