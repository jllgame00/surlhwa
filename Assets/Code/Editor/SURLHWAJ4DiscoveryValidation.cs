#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Focused J4 proof for the bounded authored-discovery completion contract.
/// It exercises existing knowledge, matching, preparation, order, and Prince
/// persistence owners without introducing a parallel progression model.
/// </summary>
public static class SURLHWAJ4DiscoveryValidation
{
    private const string BarScenePath = "Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity";
    private const string CatalogPath = "Assets/_SURLHWA/Data/Recipes/RecipeCatalog.asset";
    private const string FirstVisitOrderPath =
        "Assets/_SURLHWA/Data/Orders/LittlePrinceFirstVisitOrder.asset";
    private const string FirstVisitDialoguePath =
        "Assets/_SURLHWA/Data/Dialogue/LittlePrinceFirstVisitDialogue.asset";
    private const string AvailabilityPath =
        "Assets/_SURLHWA/Data/Ingredients/IngredientAvailability.asset";
    private const string NegroniOrderPath =
        "Assets/_SURLHWA/Data/Orders/NegroniDiscoveryOrder.asset";
    private const string NegroniDialoguePath =
        "Assets/_SURLHWA/Data/Dialogue/NegroniDiscoveryDialogue.asset";
    private const string ManhattanOrderPath =
        "Assets/_SURLHWA/Data/Orders/ManhattanDiscoveryOrder.asset";
    private const string ManhattanDialoguePath =
        "Assets/_SURLHWA/Data/Dialogue/ManhattanDiscoveryDialogue.asset";
    private const string GinFizzOrderPath =
        "Assets/_SURLHWA/Data/Orders/GinFizzDiscoveryOrder.asset";
    private const string GinFizzDialoguePath =
        "Assets/_SURLHWA/Data/Dialogue/GinFizzDiscoveryDialogue.asset";
    private const string TestStirOrderPath = "Assets/_SURLHWA/Data/Orders/TestStir.asset";
    private const string RumPath = "Assets/_SURLHWA/Data/Ingredients/Ingredient_Rum.asset";

    private static readonly string[] DryMartiniOrderPaths =
    {
        "Assets/_SURLHWA/Data/Orders/DryMartiniMethodClueOrder.asset",
        "Assets/_SURLHWA/Data/Orders/DryMartiniGinClueOrder.asset",
        "Assets/_SURLHWA/Data/Orders/DryMartiniVermouthClueOrder.asset",
        "Assets/_SURLHWA/Data/Orders/DryMartiniAttemptOrder.asset"
    };

    [MenuItem("SURLHWA/Validation/Run J4 Discovery")]
    public static void Run()
    {
        ProgressSnapshot snapshot = ProgressSnapshot.Capture();
        RecipeUnlockManager knowledge = null;
        GameObject floatHost = null;
        GameObject retryCustomerHost = null;
        GameObject orderManagerHost = null;
        GameObject affordanceHost = null;
        GameObject schedulingProbeCustomerHost = null;
        Scene scene = default;

        try
        {
            RecipeCatalogData catalog = LoadRequired<RecipeCatalogData>(CatalogPath);
            IngredientAvailabilityData availability = LoadRequired<IngredientAvailabilityData>(AvailabilityPath);
            OrderData firstVisitOrder = LoadRequired<OrderData>(FirstVisitOrderPath);
            DialogueData firstVisitDialogue = LoadRequired<DialogueData>(FirstVisitDialoguePath);
            CocktailRecipeData manwol = FindRecipe(catalog, "recipe_manwol");
            IngredientData vodka = FindIngredient(manwol, "vodka");
            IngredientData coffeeLiqueur = FindIngredient(manwol, "coffee_liqueur");
            IngredientData cream = FindIngredient(manwol, "cream");
            IngredientData rum = LoadRequired<IngredientData>(RumPath);

            ValidateDiscoveryFlag(firstVisitOrder);
            ValidateTeachingContent(firstVisitDialogue, manwol, vodka, coffeeLiqueur, cream);

            RecipeProgressPersistence.ClearRecipeProgress();
            LittlePrinceVisitPersistence.ClearFirstVisitCompleted();
            LittlePrinceVisitPersistence.ClearFollowupVisitCompleted();
            knowledge = CreateKnowledgeManager(catalog);

            ValidatePartialKnowledge(
                knowledge,
                firstVisitDialogue,
                manwol,
                vodka,
                coffeeLiqueur,
                cream);
            ValidateFloatExpectationAndPreparation(
                manwol,
                vodka,
                coffeeLiqueur,
                cream,
                ref floatHost);
            ValidateDiscoveryCompletionGate(
                firstVisitOrder,
                manwol,
                vodka,
                coffeeLiqueur,
                cream,
                rum);
            ValidateFailedRuntimeAttemptRetainsCustomer(
                catalog,
                knowledge,
                firstVisitOrder,
                manwol,
                vodka,
                coffeeLiqueur,
                cream,
                ref retryCustomerHost,
                ref orderManagerHost);
            ValidateIbaDiscoveryEncounters(catalog, availability, knowledge, ref affordanceHost);
            scene = EditorSceneManager.OpenScene(BarScenePath, OpenSceneMode.Additive);
            ValidateDiscoveryScheduling(
                scene,
                catalog,
                knowledge,
                ref schedulingProbeCustomerHost);

            Debug.Log(
                "[J4 Validation] PASS: Manwol and IBA teaching facts, expected preparation, " +
                "flagged discovery completion, retry-safe customer state, and J4 normal-play scheduling are coherent.");
        }
        finally
        {
            if (schedulingProbeCustomerHost != null)
                UnityEngine.Object.DestroyImmediate(schedulingProbeCustomerHost);
            if (orderManagerHost != null)
                UnityEngine.Object.DestroyImmediate(orderManagerHost);
            if (retryCustomerHost != null)
                UnityEngine.Object.DestroyImmediate(retryCustomerHost);
            if (floatHost != null)
                UnityEngine.Object.DestroyImmediate(floatHost);
            if (affordanceHost != null)
                UnityEngine.Object.DestroyImmediate(affordanceHost);
            if (knowledge != null)
                UnityEngine.Object.DestroyImmediate(knowledge.gameObject);
            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);

            snapshot.Restore();
        }
    }

    private static void ValidateDiscoveryFlag(OrderData firstVisitOrder)
    {
        Require(firstVisitOrder.RequiresSuccessfulDiscovery,
            "Little Prince first visit must require successful Manwol discovery.");
        Require(firstVisitOrder.RequestType == GuestRequestType.ExactCocktail,
            "Little Prince first visit must remain an exact request.");

        OrderData testStir = LoadRequired<OrderData>(TestStirOrderPath);
        Require(!testStir.RequiresSuccessfulDiscovery,
            "Generic/Test method order unexpectedly became a discovery-practice gate.");

        foreach (string path in DryMartiniOrderPaths)
        {
            OrderData order = LoadRequired<OrderData>(path);
            Require(!order.RequiresSuccessfulDiscovery,
                "Dry Martini reference order unexpectedly changed discovery-completion policy: " + path);
        }
    }

    private static void ValidateTeachingContent(
        DialogueData dialogue,
        CocktailRecipeData manwol,
        IngredientData vodka,
        IngredientData coffeeLiqueur,
        IngredientData cream)
    {
        Require(dialogue.onEnterLines != null && dialogue.onEnterLines.Length == 6,
            "Manwol first visit must preserve four narrative lines and append exactly two teaching lines.");
        Require(!string.IsNullOrWhiteSpace(dialogue.onEnterLines[0]) &&
                !string.IsNullOrWhiteSpace(dialogue.onEnterLines[1]) &&
                !string.IsNullOrWhiteSpace(dialogue.onEnterLines[2]) &&
                !string.IsNullOrWhiteSpace(dialogue.onEnterLines[3]),
            "Existing Little Prince narrative lines were removed.");
        Require(dialogue.onEnterLines[4] ==
                "만월은 이렇게 만든대요. 보드카를 두고, 커피 리큐르를 하나 먼저 담아요. 크림 하나는 맨 위에 천천히 올려 주세요." &&
                dialogue.onEnterLines[5] ==
                "층이 섞이지 않게 조심하면, 달빛이 잔 위에 남는 것처럼 보일 거예요.",
            "Manwol teaching copy drifted from the approved J4B content.");

        Require(HasReveal(dialogue, 4, manwol, RecipeKnowledgeFact.Identity, null) &&
                HasReveal(dialogue, 4, manwol, RecipeKnowledgeFact.Method, null) &&
                HasReveal(dialogue, 4, manwol, RecipeKnowledgeFact.IngredientAmount, vodka) &&
                HasReveal(dialogue, 4, manwol, RecipeKnowledgeFact.IngredientAmount, coffeeLiqueur) &&
                HasReveal(dialogue, 4, manwol, RecipeKnowledgeFact.IngredientAmount, cream) &&
                HasReveal(dialogue, 5, manwol, RecipeKnowledgeFact.Preparation, null),
            "Manwol dialogue must reveal identity, Float, all three amounts, and preparation in the displayed teaching lines.");
    }

    private static void ValidatePartialKnowledge(
        RecipeUnlockManager knowledge,
        DialogueData dialogue,
        CocktailRecipeData manwol,
        IngredientData vodka,
        IngredientData coffeeLiqueur,
        IngredientData cream)
    {
        Require(knowledge.GetKnowledgeState(manwol) == RecipeKnowledgeState.Unknown,
            "Manwol must not start Known.");

        ApplyReveals(dialogue.GetOnEnterKnowledgeReveals(4), knowledge);
        ApplyReveals(dialogue.GetOnEnterKnowledgeReveals(5), knowledge);

        RecipeKnowledgeFacts facts = knowledge.GetKnownFacts(manwol);
        Require(facts.State == RecipeKnowledgeState.Partial &&
                facts.IdentityKnown && facts.MethodKnown && facts.PreparationKnown &&
                facts.IsIngredientAmountKnown(vodka) &&
                facts.IsIngredientAmountKnown(coffeeLiqueur) &&
                facts.IsIngredientAmountKnown(cream) && !facts.GarnishKnown,
            "Manwol teaching facts did not produce the required Partial knowledge state.");
        Require(!knowledge.IsKnown(manwol),
            "Dialogue facts must not directly mark Manwol Known.");

        string detail = RecipeKnowledgeGuideFormatter.BuildDetailText(manwol, facts);
        Require(RecipeKnowledgeGuideFormatter.GetDetailName(manwol, facts) == "만월" &&
                RecipeKnowledgeGuideFormatter.GetMethodText(manwol, facts) == "Method: Float" &&
                ContainsAll(detail, "Vodka", "Coffee Liqueur", "Cream", "2 oz", "1 oz", "Preparation:", "Pour") &&
                !Contains(detail, "Old Fashioned Glass"),
            "Partial Manwol Guide must expose only the authored teaching facts, never Known-only glassware.");
    }

    private static void ValidateFloatExpectationAndPreparation(
        CocktailRecipeData manwol,
        IngredientData vodka,
        IngredientData coffeeLiqueur,
        IngredientData cream,
        ref GameObject floatHost)
    {
        Require(typeof(IRecipePreparationExpectationReceiver).IsAssignableFrom(typeof(FloatMiniGame)),
            "FloatMiniGame must receive an expected recipe through the shared preparation receiver seam.");

        floatHost = new GameObject("J4B Float Expectation Validation");
        FloatMiniGame floatMiniGame = floatHost.AddComponent<FloatMiniGame>();
        floatMiniGame.SetExpectedRecipe(manwol);
        FieldInfo expectedRecipeField = typeof(FloatMiniGame).GetField(
            "expectedRecipe", BindingFlags.Instance | BindingFlags.NonPublic);
        Require(expectedRecipeField != null && expectedRecipeField.GetValue(floatMiniGame) == manwol,
            "FloatMiniGame did not retain the active expected recipe.");

        BartendingPreparationTrace correctTrace = CreateManwolTrace(vodka, coffeeLiqueur, cream);
        RecipePreparationEvaluationResult correct = RecipePreparationEvaluator.Evaluate(manwol, correctTrace);
        Require(correct.Status == RecipePreparationEvaluationStatus.Satisfied,
            "Manwol Vodka -> Coffee Liqueur -> Cream trace must satisfy shared preparation evaluation.");

        BartendingPreparationTrace wrongOrderTrace = CreateManwolTrace(coffeeLiqueur, vodka, cream);
        RecipePreparationEvaluationResult wrongOrder = RecipePreparationEvaluator.Evaluate(manwol, wrongOrderTrace);
        Require(wrongOrder.Status == RecipePreparationEvaluationStatus.Unsatisfied,
            "Wrong Manwol layer order must fail shared preparation evaluation.");
    }

    private static void ValidateDiscoveryCompletionGate(
        OrderData order,
        CocktailRecipeData manwol,
        IngredientData vodka,
        IngredientData coffeeLiqueur,
        IngredientData cream,
        IngredientData rum)
    {
        GuestRequestContext request = GuestRequestContext.FromOrder(order);
        RecipeMatchResult exactMatch = new()
        {
            success = true,
            matchedRecipe = manwol,
            similarity = 1f
        };
        RequestEvaluationResult exactEvaluation = RequestEvaluation.Evaluate(request, exactMatch);
        MiniGameResult correct = new()
        {
            success = true,
            expectedRecipe = manwol,
            preparationEvaluation = RecipePreparationEvaluator.Evaluate(
                manwol,
                CreateManwolTrace(vodka, coffeeLiqueur, cream))
        };

        Require(InvokeDiscoveryGate(order, request, manwol, exactMatch, exactEvaluation, correct),
            "Correct exact Manwol result must pass the flagged discovery gate.");

        MiniGameResult poorTechnique = correct;
        poorTechnique.success = false;
        Require(!InvokeDiscoveryGate(order, request, manwol, exactMatch, exactEvaluation, poorTechnique),
            "Poor Float technique must not pass discovery completion.");

        MiniGameResult wrongOrder = correct;
        wrongOrder.preparationEvaluation = RecipePreparationEvaluator.Evaluate(
            manwol,
            CreateManwolTrace(coffeeLiqueur, vodka, cream));
        Require(!InvokeDiscoveryGate(order, request, manwol, exactMatch, exactEvaluation, wrongOrder),
            "Wrong Float layer order must not pass discovery completion.");

        RecipeMatchResult wrongIngredient = new()
        {
            success = false,
            matchedRecipe = null,
            similarity = 0f
        };
        RequestEvaluationResult failedEvaluation = RequestEvaluation.Evaluate(request, wrongIngredient);
        Require(!InvokeDiscoveryGate(order, request, manwol, wrongIngredient, failedEvaluation, correct),
            "Wrong ingredient attempt must not pass discovery completion.");

        CocktailRecipeData wrongRecipe = ScriptableObject.CreateInstance<CocktailRecipeData>();
        try
        {
            RecipeMatchResult wrongRecognized = new()
            {
                success = true,
                matchedRecipe = wrongRecipe,
                similarity = 1f
            };
            RequestEvaluationResult wrongRecipeEvaluation = new(
                GuestRequestType.ExactCocktail,
                RequestEvaluationStatus.Unsatisfied);
            Require(!InvokeDiscoveryGate(order, request, manwol, wrongRecognized, wrongRecipeEvaluation, correct),
                "Recognized non-target recipe must not pass or promote during a flagged discovery attempt.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(wrongRecipe);
        }

        Require(rum != null,
            "Wrong-ingredient probe must remain a real distinct IngredientData asset.");
    }

    private static void ValidateFailedRuntimeAttemptRetainsCustomer(
        RecipeCatalogData catalog,
        RecipeUnlockManager knowledge,
        OrderData firstVisitOrder,
        CocktailRecipeData manwol,
        IngredientData vodka,
        IngredientData coffeeLiqueur,
        IngredientData cream,
        ref GameObject retryCustomerHost,
        ref GameObject orderManagerHost)
    {
        LittlePrinceVisitPersistence.ClearFirstVisitCompleted();

        retryCustomerHost = new GameObject("J4B Retry Customer")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        CustomerController customer = retryCustomerHost.AddComponent<CustomerController>();
        SetPrivateEnum(customer, "state", "Waiting");

        orderManagerHost = new GameObject("J4B Retry Order Manager")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        OrderManager orderManager = orderManagerHost.AddComponent<OrderManager>();
        SetPrivateField(orderManager, "recipeUnlockManager", knowledge);
        SetPrivateField(orderManager, "_currentCustomer", customer);
        SetPrivateField(orderManager, "currentOrder", firstVisitOrder);
        SetPrivateField(orderManager, "currentGuestRequest", GuestRequestContext.FromOrder(firstVisitOrder));

        MiniGameResult failedPreparation = new()
        {
            success = true,
            expectedRecipe = manwol,
            brewedComposition = BuildComposition(manwol, vodka, coffeeLiqueur, cream),
            preparationEvaluation = RecipePreparationEvaluator.Evaluate(
                manwol,
                CreateManwolTrace(coffeeLiqueur, vodka, cream))
        };

        InvokePrivate(orderManager, "OnMiniGameDone", new object[] { failedPreparation });

        Require(!knowledge.IsKnown(manwol),
            "Unsatisfied Manwol preparation must not mark the target Known at the real OrderManager seam.");
        Require(!LittlePrinceVisitPersistence.IsFirstVisitCompleted(),
            "Failed Manwol practice must not persist Little Prince first-visit completion.");
        Require(GetPrivateField<object>(orderManager, "_currentCustomer") == null &&
                GetPrivateField<object>(orderManager, "currentOrder") == null &&
                GetPrivateField<object>(orderManager, "currentGuestRequest") == null,
            "Failed discovery must clear only OrderManager transient request ownership for retry.");
        Require(GetPrivateEnumName(customer, "state") == "Waiting",
            "Failed discovery must leave the same customer waiting rather than departing.");
    }

    private static void ValidateDiscoveryScheduling(
        Scene scene,
        RecipeCatalogData catalog,
        RecipeUnlockManager knowledge,
        ref GameObject schedulingProbeCustomerHost)
    {
        Require(scene.IsValid() && scene.isLoaded,
            "The production BarScene must load for J4 scheduling validation.");

        CustomerSpawner spawner = FindSingleInScene<CustomerSpawner>(scene);
        BusinessDayManager dayManager = FindSingleInScene<BusinessDayManager>(scene);
        Require(spawner != null && dayManager != null,
            "BarScene is missing the existing CustomerSpawner or BusinessDayManager scheduling owner.");

        OrderData negroniOrder = LoadRequired<OrderData>(NegroniOrderPath);
        OrderData manhattanOrder = LoadRequired<OrderData>(ManhattanOrderPath);
        OrderData ginFizzOrder = LoadRequired<OrderData>(GinFizzOrderPath);
        CocktailRecipeData negroni = FindRecipe(catalog, "recipe_negroni");
        CocktailRecipeData manhattan = FindRecipe(catalog, "recipe_manhattan");
        CocktailRecipeData ginFizz = FindRecipe(catalog, "recipe_gin_fizz");

        Require(GetPrivateField<OrderData>(spawner, "negroniDiscoveryOrder") == negroniOrder &&
                GetPrivateField<OrderData>(spawner, "manhattanDiscoveryOrder") == manhattanOrder &&
                GetPrivateField<OrderData>(spawner, "ginFizzDiscoveryOrder") == ginFizzOrder,
            "CustomerSpawner must serialize exactly the three J4C discovery-order references.");
        Require(negroniOrder.RequestType == GuestRequestType.ExactCocktail &&
                manhattanOrder.RequestType == GuestRequestType.ExactCocktail &&
                ginFizzOrder.RequestType == GuestRequestType.ExactCocktail &&
                negroniOrder.RequiresSuccessfulDiscovery &&
                manhattanOrder.RequiresSuccessfulDiscovery &&
                ginFizzOrder.RequiresSuccessfulDiscovery,
            "J4 scheduled encounters must remain flagged exact discovery practice, not Recommendation content.");
        Require(GetPrivateField<int>(spawner, "littlePrinceFollowupDay") == 3,
            "J4 must preserve the existing Day-3 Little Prince return and schedule no Day-4 closure.");
        RequireDemoCountsRemainJ1Owned(dayManager);

        SetPrivateField(spawner, "recipeUnlockManager", knowledge);
        LittlePrinceVisitPersistence.ClearFirstVisitCompleted();
        LittlePrinceVisitPersistence.ClearFollowupVisitCompleted();

        SetSchedulePosition(dayManager, spawner, 1, 0);
        Require(InvokePrivate(spawner, "GetAuthoredOrderForNextSpawn", null) != null,
            "Day 1's authored anchors must remain available at their existing first position.");

        SetSchedulePosition(dayManager, spawner, 2, 0);
        Require((bool)InvokePrivate(spawner, "ShouldSpawnLittlePrinceFirstVisit", null),
            "Incomplete Little Prince first visit must retain Day-2 first priority.");
        Require(InvokePrivate(spawner, "GetAuthoredOrderForNextSpawn", null) == null,
            "Negroni must not compete with the incomplete Little Prince Day-2 first visit.");

        SetSchedulePosition(dayManager, spawner, 2, 1);
        Require(InvokePrivate(spawner, "GetAuthoredOrderForNextSpawn", null) == negroniOrder,
            "Unknown Negroni must occupy Day-2 slot 2 after the Prince encounter.");

        // Same-day reload after the successfully departed Prince has no
        // persisted spawn counter. The existing visit flag reconstructs only
        // that special slot, leaving Negroni pending immediately.
        LittlePrinceVisitPersistence.MarkFirstVisitCompleted();
        SetSchedulePosition(dayManager, spawner, 2, 0);
        Require(InvokePrivate(spawner, "GetAuthoredOrderForNextSpawn", null) == negroniOrder,
            "Day-2 reload after Prince success must keep unknown Negroni pending.");
        Require(knowledge.MarkKnown(negroni),
            "Controlled Negroni-success setup failed to reach Known.");
        Require(InvokePrivate(spawner, "GetAuthoredOrderForNextSpawn", null) == null &&
                knowledge.GetKnowledgeState(negroni) == RecipeKnowledgeState.Known &&
                !knowledge.MarkKnown(negroni),
            "Known Negroni must skip teaching without regressing knowledge or duplicating promotion.");

        SetSchedulePosition(dayManager, spawner, 3, 0);
        Require((bool)InvokePrivate(spawner, "ShouldSpawnLittlePrinceFollowupVisit", null),
            "Existing branch-aware Little Prince return must retain Day-3 first priority.");
        Require(InvokePrivate(spawner, "GetAuthoredOrderForNextSpawn", null) == null,
            "Manhattan must not replace the Day-3 Little Prince return.");
        SetSchedulePosition(dayManager, spawner, 3, 1);
        Require(InvokePrivate(spawner, "GetAuthoredOrderForNextSpawn", null) == manhattanOrder,
            "Unknown Manhattan must occupy Day-3 slot 2 after the Prince return.");

        LittlePrinceVisitPersistence.MarkFollowupVisitCompleted();
        SetSchedulePosition(dayManager, spawner, 3, 0);
        Require(InvokePrivate(spawner, "GetAuthoredOrderForNextSpawn", null) == manhattanOrder,
            "Day-3 reload after Prince return must keep unknown Manhattan pending.");
        Require(knowledge.MarkKnown(manhattan),
            "Controlled Manhattan-success setup failed to reach Known.");
        Require(InvokePrivate(spawner, "GetAuthoredOrderForNextSpawn", null) == null &&
                knowledge.GetKnowledgeState(manhattan) == RecipeKnowledgeState.Known &&
                !knowledge.MarkKnown(manhattan),
            "Known Manhattan must skip teaching without regressing knowledge or duplicating promotion.");

        SetSchedulePosition(dayManager, spawner, 4, 0);
        Require(InvokePrivate(spawner, "GetAuthoredOrderForNextSpawn", null) == ginFizzOrder,
            "Unknown Gin Fizz must occupy Day-4 slot 1.");
        Require(knowledge.MarkKnown(ginFizz),
            "Controlled Gin Fizz-success setup failed to reach Known.");
        Require(InvokePrivate(spawner, "GetAuthoredOrderForNextSpawn", null) == null &&
                knowledge.GetKnowledgeState(ginFizz) == RecipeKnowledgeState.Known &&
                !knowledge.MarkKnown(ginFizz),
            "Known Gin Fizz must skip teaching without regressing knowledge or duplicating promotion.");

        schedulingProbeCustomerHost = new GameObject("J4 Discovery Active-Customer Scheduling Probe")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        CustomerController retryingCustomer = schedulingProbeCustomerHost.AddComponent<CustomerController>();
        LittlePrinceVisitPersistence.ClearFirstVisitCompleted();
        SetSchedulePosition(dayManager, spawner, 2, 1);
        SetPrivateField(spawner, "activeCustomer", retryingCustomer);
        int spawnedBeforeRetryProbe = GetPrivateField<int>(spawner, "spawnedCount");
        spawner.SpawnNextCustomer();
        Require(GetPrivateField<object>(spawner, "activeCustomer") == retryingCustomer &&
                GetPrivateField<int>(spawner, "spawnedCount") == spawnedBeforeRetryProbe,
            "An active failed discovery customer must block replacement spawning and slot advancement.");
        SetPrivateField(spawner, "activeCustomer", null);
    }

    private static void RequireDemoCountsRemainJ1Owned(BusinessDayManager dayManager)
    {
        int[] counts = GetPrivateField<int[]>(dayManager, "demoCustomerCountsByDay");
        Require(counts != null && counts.Length == 4 &&
                counts[0] == 5 && counts[1] == 8 && counts[2] == 8 && counts[3] == 8,
            "J4 must preserve J1's 5/8/8/8 interim counts; J3D owns 5/4/4/4 finalization.");
        Require(dayManager.DemoNightCount == 4,
            "J4 scheduling must preserve the bounded four-night lifecycle.");
    }

    private static void SetSchedulePosition(
        BusinessDayManager dayManager,
        CustomerSpawner spawner,
        int day,
        int spawnedCount)
    {
        SetPrivateField(dayManager, "currentDay", day);
        SetPrivateField(spawner, "spawnedCount", spawnedCount);
    }

    private static void ValidateIbaDiscoveryEncounters(
        RecipeCatalogData catalog,
        IngredientAvailabilityData availability,
        RecipeUnlockManager knowledge,
        ref GameObject affordanceHost)
    {
        CocktailRecipeData negroni = FindRecipe(catalog, "recipe_negroni");
        CocktailRecipeData manhattan = FindRecipe(catalog, "recipe_manhattan");
        CocktailRecipeData ginFizz = FindRecipe(catalog, "recipe_gin_fizz");

        IngredientData negroniGin = FindIngredient(negroni, "gin");
        IngredientData negroniVermouth = FindIngredient(negroni, "sweet_red_vermouth");
        IngredientData campari = FindIngredient(negroni, "campari");
        IngredientData ryeWhiskey = FindIngredient(manhattan, "rye_whiskey");
        IngredientData manhattanVermouth = FindIngredient(manhattan, "sweet_red_vermouth");
        IngredientData angostura = FindIngredient(manhattan, "angostura_bitters");
        IngredientData fizzGin = FindIngredient(ginFizz, "gin");
        IngredientData lemonJuice = FindIngredient(ginFizz, "fresh_lemon_juice");
        IngredientData simpleSyrup = FindIngredient(ginFizz, "simple_syrup");
        IngredientData sodaWater = FindIngredient(ginFizz, "soda_water");

        OrderData negroniOrder = LoadRequired<OrderData>(NegroniOrderPath);
        OrderData manhattanOrder = LoadRequired<OrderData>(ManhattanOrderPath);
        OrderData ginFizzOrder = LoadRequired<OrderData>(GinFizzOrderPath);
        DialogueData negroniDialogue = LoadRequired<DialogueData>(NegroniDialoguePath);
        DialogueData manhattanDialogue = LoadRequired<DialogueData>(ManhattanDialoguePath);
        DialogueData ginFizzDialogue = LoadRequired<DialogueData>(GinFizzDialoguePath);

        ValidateIbaDiscoveryOrderAndPartial(
            "Negroni", negroniOrder, negroniDialogue, negroni, MiniGameType.Build, 2,
            catalog, availability, knowledge, negroniGin, negroniVermouth, campari);
        ValidateIbaDiscoveryOrderAndPartial(
            "Manhattan", manhattanOrder, manhattanDialogue, manhattan, MiniGameType.Stir, 3,
            catalog, availability, knowledge, ryeWhiskey, manhattanVermouth, angostura);
        ValidateIbaDiscoveryOrderAndPartial(
            "Gin Fizz", ginFizzOrder, ginFizzDialogue, ginFizz, MiniGameType.Shake, 4,
            catalog, availability, knowledge, fizzGin, lemonJuice, simpleSyrup, sodaWater);

        ValidateNegroniPreparation(negroniOrder, negroni, negroniGin, negroniVermouth, campari);
        ValidateManhattanDashPreparation(
            manhattanOrder, manhattan, ryeWhiskey, manhattanVermouth, angostura);
        ValidateGinFizzPreparation(ginFizzOrder, ginFizz, fizzGin, lemonJuice, simpleSyrup, sodaWater);
        ValidateExpectedRecipeAffordances(negroni, manhattan, ginFizz, ref affordanceHost);
    }

    private static void ValidateIbaDiscoveryOrderAndPartial(
        string label,
        OrderData order,
        DialogueData dialogue,
        CocktailRecipeData recipe,
        MiniGameType expectedMethod,
        int expectedDay,
        RecipeCatalogData catalog,
        IngredientAvailabilityData availability,
        RecipeUnlockManager knowledge,
        params IngredientData[] ingredients)
    {
        Require(order != null && dialogue != null && recipe != null,
            label + " discovery assets must be present.");
        Require(order.RequestType == GuestRequestType.ExactCocktail,
            label + " discovery order must remain an exact request.");
        Require(order.requestedCocktail == recipe.targetCocktail,
            label + " discovery target cocktail drifted from its production recipe.");
        Require(order.miniGameType == expectedMethod && recipe.method == expectedMethod,
            label + " discovery method must match the production recipe.");
        Require(order.RequiresSuccessfulDiscovery,
            label + " discovery order must reuse the J4B completion contract.");
        Require(order.PreBrewDialogue == dialogue,
            label + " teaching dialogue must be the order's pre-brew dialogue.");
        Require(!string.IsNullOrWhiteSpace(order.DiscoveryRetryFeedback),
            label + " discovery order must provide concise retry feedback through the shared surface.");
        Require(!catalog.IsUnlockedByDefault(recipe) && knowledge.GetKnowledgeState(recipe) == RecipeKnowledgeState.Unknown,
            label + " must not begin Known.");
        Require(dialogue.onEnterLines != null && dialogue.onEnterLines.Length == 2 &&
                !string.IsNullOrWhiteSpace(dialogue.onEnterLines[0]) &&
                !string.IsNullOrWhiteSpace(dialogue.onEnterLines[1]),
            label + " requires exactly two authored teaching lines before Brew.");

        Require(HasRevealOnAnyLine(dialogue, recipe, RecipeKnowledgeFact.Identity, null) &&
                HasRevealOnAnyLine(dialogue, recipe, RecipeKnowledgeFact.Method, null) &&
                HasRevealOnAnyLine(dialogue, recipe, RecipeKnowledgeFact.Preparation, null) &&
                HasRevealOnAnyLine(dialogue, recipe, RecipeKnowledgeFact.Garnish, null),
            label + " dialogue must reveal identity, method, preparation, and garnish facts.");

        foreach (IngredientData ingredient in ingredients)
        {
            Require(ingredient != null &&
                    HasRevealOnAnyLine(dialogue, recipe, RecipeKnowledgeFact.IngredientAmount, ingredient),
                label + " dialogue must reveal each required ingredient amount.");
            Require(availability.IsIngredientAvailable(
                        ingredient,
                        new IngredientAvailabilityContext(expectedDay)),
                label + " ingredient is not physically supplied on its planned discovery day: " +
                ingredient.ingredientId);
        }

        ApplyAllOnEnterReveals(dialogue, knowledge, label);
        RecipeKnowledgeFacts facts = knowledge.GetKnownFacts(recipe);
        Require(facts.State == RecipeKnowledgeState.Partial &&
                facts.IdentityKnown && facts.MethodKnown && facts.PreparationKnown && facts.GarnishKnown,
            label + " teaching must result in Partial, not Known, recipe knowledge.");
        foreach (IngredientData ingredient in ingredients)
        {
            Require(facts.IsIngredientAmountKnown(ingredient),
                label + " Partial knowledge is missing a taught ingredient amount.");
        }

        string guideDetail = RecipeKnowledgeGuideFormatter.BuildDetailText(recipe, facts);
        Require(!string.IsNullOrWhiteSpace(RecipeKnowledgeGuideFormatter.GetDetailName(recipe, facts)) &&
                RecipeKnowledgeGuideFormatter.GetMethodText(recipe, facts) == "Method: " + expectedMethod &&
                Contains(guideDetail, "Preparation:") &&
                !Contains(guideDetail, "Glassware:"),
            label + " Partial Guide must expose only taught facts, never Known-only glassware.");

    }

    private static void ValidateNegroniPreparation(
        OrderData order,
        CocktailRecipeData recipe,
        IngredientData gin,
        IngredientData vermouth,
        IngredientData campari)
    {
        Require(RecipePreparationEvaluator.HasRequiredAction(recipe, RecipePreparationAction.GentleStir) &&
                RecipePreparationEvaluator.HasRequiredAction(recipe, RecipePreparationAction.Garnish),
            "Negroni must retain its required GentleStir and Orange Slice preparation.");
        RecipePreparationEvaluationResult correct = RecipePreparationEvaluator.Evaluate(
            recipe,
            CreateNegroniTrace(gin, vermouth, campari, true));
        Require(correct.Status == RecipePreparationEvaluationStatus.Satisfied,
            "Complete Negroni Build trace with Orange Slice must satisfy preparation.");
        ValidateCorrectDiscoveryGate(order, recipe, correct);
        Require(RecipePreparationEvaluator.Evaluate(
                    recipe,
                    CreateNegroniTrace(gin, vermouth, campari, false)).Status ==
                RecipePreparationEvaluationStatus.Unsatisfied,
            "Negroni discovery must fail when the required Orange Slice is omitted.");
    }

    private static void ValidateManhattanDashPreparation(
        OrderData order,
        CocktailRecipeData recipe,
        IngredientData ryeWhiskey,
        IngredientData vermouth,
        IngredientData angostura)
    {
        Require(RecipePreparationEvaluator.HasRequiredAction(recipe, RecipePreparationAction.Dash) &&
                RecipePreparationEvaluator.HasRequiredAction(recipe, RecipePreparationAction.Garnish),
            "Manhattan must retain required Dash and Cherry preparation.");
        RecipePreparationEvaluationResult correct = RecipePreparationEvaluator.Evaluate(
            recipe,
            CreateManhattanTrace(ryeWhiskey, vermouth, angostura, true, true));
        Require(correct.Status == RecipePreparationEvaluationStatus.Satisfied,
            "Manhattan Dash trace with Cherry must satisfy preparation.");
        ValidateCorrectDiscoveryGate(order, recipe, correct);
        Require(RecipePreparationEvaluator.Evaluate(
                    recipe,
                    CreateManhattanTrace(ryeWhiskey, vermouth, angostura, false, true)).Status ==
                RecipePreparationEvaluationStatus.Unsatisfied,
            "A pour-equivalent Angostura action must not substitute for Manhattan's authored Dash.");
        Require(RecipePreparationEvaluator.Evaluate(
                    recipe,
                    CreateManhattanTrace(ryeWhiskey, vermouth, angostura, true, false)).Status ==
                RecipePreparationEvaluationStatus.Unsatisfied,
            "Manhattan discovery must fail when the required Cherry is omitted.");
    }

    private static void ValidateGinFizzPreparation(
        OrderData order,
        CocktailRecipeData recipe,
        IngredientData gin,
        IngredientData lemonJuice,
        IngredientData simpleSyrup,
        IngredientData sodaWater)
    {
        Require(RecipePreparationEvaluator.HasRequiredAction(recipe, RecipePreparationAction.PourFromContainer) &&
                RecipePreparationEvaluator.HasRequiredAction(recipe, RecipePreparationAction.Splash) &&
                !RecipePreparationEvaluator.HasRequiredAction(recipe, RecipePreparationAction.Garnish),
            "Gin Fizz must require PourFromContainer and Splash while retaining optional Lemon garnish.");
        RecipePreparationEvaluationResult correct = RecipePreparationEvaluator.Evaluate(
            recipe,
            CreateGinFizzTrace(gin, lemonJuice, simpleSyrup, sodaWater, true, false));
        Require(correct.Status == RecipePreparationEvaluationStatus.Satisfied,
            "Gin Fizz must satisfy preparation without optional Lemon garnish.");
        ValidateCorrectDiscoveryGate(order, recipe, correct);
        Require(RecipePreparationEvaluator.Evaluate(
                    recipe,
                    CreateGinFizzTrace(gin, lemonJuice, simpleSyrup, sodaWater, false, false)).Status ==
                RecipePreparationEvaluationStatus.Unsatisfied,
            "Gin Fizz discovery must fail when the authored Soda Water Splash is omitted.");
        Require(RecipePreparationEvaluator.Evaluate(
                    recipe,
                    CreateGinFizzTrace(gin, lemonJuice, simpleSyrup, sodaWater, true, true)).Status ==
                RecipePreparationEvaluationStatus.Unsatisfied,
            "Gin Fizz Strain must not substitute for the authored PourFromContainer transfer.");
    }

    private static void ValidateExpectedRecipeAffordances(
        CocktailRecipeData negroni,
        CocktailRecipeData manhattan,
        CocktailRecipeData ginFizz,
        ref GameObject affordanceHost)
    {
        affordanceHost = new GameObject("J4 IBA Expected-Recipe Affordance Validation")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        ServingGlassGarnishController garnish = affordanceHost.AddComponent<ServingGlassGarnishController>();
        garnish.SetExpectedRecipe(negroni);
        Require(garnish.IsExpectedGarnishRequired,
            "Build garnish prompt must identify Negroni's Orange Slice as required.");
        garnish.SetExpectedRecipe(manhattan);
        Require(garnish.IsExpectedGarnishRequired,
            "Stir garnish prompt must identify Manhattan's Cherry as required.");
        garnish.SetExpectedRecipe(ginFizz);
        Require(!garnish.IsExpectedGarnishRequired,
            "Gin Fizz optional Lemon garnish must not be presented as required.");

        ShakeMiniGame shake = affordanceHost.AddComponent<ShakeMiniGame>();
        shake.SetExpectedRecipe(ginFizz);
        Require((bool)InvokePrivate(shake, "UsesExpectedPourFromContainerTransfer", null),
            "Gin Fizz expected recipe must select the existing PourFromContainer transfer route.");
        shake.SetExpectedRecipe(negroni);
        Require(!(bool)InvokePrivate(shake, "UsesExpectedPourFromContainerTransfer", null),
            "Other Shake recipes must retain their authored transfer route.");
    }

    private static BartendingPreparationTrace CreateNegroniTrace(
        IngredientData gin,
        IngredientData vermouth,
        IngredientData campari,
        bool includeOrangeSlice)
    {
        BartendingPreparationTrace trace = new();
        trace.Record(RecipePreparationAction.Pour, gin, 30f, null, BartendingContainerType.ServingGlass);
        trace.Record(RecipePreparationAction.Pour, vermouth, 30f, null, BartendingContainerType.ServingGlass);
        trace.Record(RecipePreparationAction.Pour, campari, 30f, null, BartendingContainerType.ServingGlass);
        trace.Record(RecipePreparationAction.AddIce, null, 0f, null, BartendingContainerType.ServingGlass);
        trace.Record(RecipePreparationAction.GentleStir, null, 0f, null, BartendingContainerType.ServingGlass);
        if (includeOrangeSlice)
        {
            trace.RecordActiveGarnish(
                new BartendingGarnishState(BartendingGarnishId.OrangeSlice, BartendingGarnishPlacement.Rim),
                BartendingContainerType.ServingGlass);
        }

        return trace;
    }

    private static BartendingPreparationTrace CreateManhattanTrace(
        IngredientData ryeWhiskey,
        IngredientData vermouth,
        IngredientData angostura,
        bool useDash,
        bool includeCherry)
    {
        BartendingPreparationTrace trace = new();
        trace.Record(RecipePreparationAction.Pour, ryeWhiskey, 50f, null, BartendingContainerType.MixingGlass);
        trace.Record(RecipePreparationAction.Pour, vermouth, 20f, null, BartendingContainerType.MixingGlass);
        trace.Record(
            useDash ? RecipePreparationAction.Dash : RecipePreparationAction.Pour,
            angostura,
            1f,
            null,
            BartendingContainerType.MixingGlass);
        trace.Record(RecipePreparationAction.AddIce, null, 0f, null, BartendingContainerType.MixingGlass);
        trace.Record(RecipePreparationAction.Stir, null, 0f, null, BartendingContainerType.MixingGlass);
        trace.Record(
            RecipePreparationAction.Strain,
            null,
            0f,
            BartendingContainerType.MixingGlass,
            BartendingContainerType.ServingGlass);
        if (includeCherry)
        {
            trace.RecordActiveGarnish(
                new BartendingGarnishState(BartendingGarnishId.Cherry, BartendingGarnishPlacement.InGlass),
                BartendingContainerType.ServingGlass);
        }

        return trace;
    }

    private static BartendingPreparationTrace CreateGinFizzTrace(
        IngredientData gin,
        IngredientData lemonJuice,
        IngredientData simpleSyrup,
        IngredientData sodaWater,
        bool includeSplash,
        bool useStrain)
    {
        BartendingPreparationTrace trace = new();
        trace.Record(RecipePreparationAction.Pour, gin, 45f, null, BartendingContainerType.Shaker);
        trace.Record(RecipePreparationAction.Pour, lemonJuice, 30f, null, BartendingContainerType.Shaker);
        trace.Record(RecipePreparationAction.Pour, simpleSyrup, 10f, null, BartendingContainerType.Shaker);
        trace.Record(RecipePreparationAction.AddIce, null, 0f, null, BartendingContainerType.Shaker);
        trace.Record(RecipePreparationAction.Shake, null, 0f, null, BartendingContainerType.Shaker);
        trace.Record(
            useStrain ? RecipePreparationAction.Strain : RecipePreparationAction.PourFromContainer,
            null,
            0f,
            BartendingContainerType.Shaker,
            BartendingContainerType.ServingGlass);
        if (includeSplash)
        {
            trace.Record(RecipePreparationAction.Splash, sodaWater, 15f, null, BartendingContainerType.ServingGlass);
        }

        return trace;
    }

    private static BartendingPreparationTrace CreateManwolTrace(params IngredientData[] order)
    {
        BartendingPreparationTrace trace = new();
        foreach (IngredientData ingredient in order)
        {
            trace.Record(
                RecipePreparationAction.Pour,
                ingredient,
                30f,
                null,
                BartendingContainerType.ServingGlass);
        }

        return trace;
    }

    private static BrewComposition BuildComposition(
        CocktailRecipeData recipe,
        IngredientData vodka,
        IngredientData coffeeLiqueur,
        IngredientData cream)
    {
        return new BrewComposition
        {
            method = recipe.method,
            ingredients = new[]
            {
                new BrewIngredientEntry { ingredientId = vodka.ingredientId, amount = 2f },
                new BrewIngredientEntry { ingredientId = coffeeLiqueur.ingredientId, amount = 1f },
                new BrewIngredientEntry { ingredientId = cream.ingredientId, amount = 1f }
            }
        };
    }

    private static bool InvokeDiscoveryGate(
        OrderData order,
        GuestRequestContext request,
        CocktailRecipeData targetRecipe,
        RecipeMatchResult matchResult,
        RequestEvaluationResult evaluation,
        MiniGameResult result)
    {
        MethodInfo gate = typeof(OrderManager).GetMethod(
            "MeetsSuccessfulDiscoveryRequirements",
            BindingFlags.Static | BindingFlags.NonPublic);
        Require(gate != null, "OrderManager discovery completion gate is missing.");
        return (bool)gate.Invoke(null, new object[]
        {
            order, request, targetRecipe, matchResult, evaluation, result
        });
    }

    private static void ValidateCorrectDiscoveryGate(
        OrderData order,
        CocktailRecipeData recipe,
        RecipePreparationEvaluationResult preparation)
    {
        GuestRequestContext request = GuestRequestContext.FromOrder(order);
        RecipeMatchResult match = new()
        {
            success = true,
            matchedRecipe = recipe,
            similarity = 1f
        };
        MiniGameResult result = new()
        {
            success = true,
            expectedRecipe = recipe,
            preparationEvaluation = preparation
        };

        Require(InvokeDiscoveryGate(
                    order,
                    request,
                    recipe,
                    match,
                    RequestEvaluation.Evaluate(request, match),
                    result),
            order.orderName + " complete exact preparation must pass the shared discovery gate.");
    }

    private static void ApplyReveals(RecipeKnowledgeReveal[] reveals, RecipeUnlockManager knowledge)
    {
        Require(reveals != null && reveals.Length > 0,
            "Required displayed Manwol teaching line has no knowledge reveals.");
        foreach (RecipeKnowledgeReveal reveal in reveals)
            Require(reveal != null && reveal.TryApply(knowledge),
                "Displayed Manwol teaching reveal did not change knowledge.");
    }

    private static bool HasReveal(
        DialogueData dialogue,
        int lineIndex,
        CocktailRecipeData recipe,
        RecipeKnowledgeFact fact,
        IngredientData ingredient)
    {
        foreach (RecipeKnowledgeReveal reveal in dialogue.GetOnEnterKnowledgeReveals(lineIndex))
        {
            if (reveal != null && reveal.Recipe == recipe && reveal.Fact == fact &&
                reveal.Ingredient == ingredient)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasRevealOnAnyLine(
        DialogueData dialogue,
        CocktailRecipeData recipe,
        RecipeKnowledgeFact fact,
        IngredientData ingredient)
    {
        if (dialogue == null || dialogue.onEnterLines == null)
            return false;

        for (int lineIndex = 0; lineIndex < dialogue.onEnterLines.Length; lineIndex++)
        {
            if (HasReveal(dialogue, lineIndex, recipe, fact, ingredient))
                return true;
        }

        return false;
    }

    private static void ApplyAllOnEnterReveals(
        DialogueData dialogue,
        RecipeUnlockManager knowledge,
        string label)
    {
        Require(dialogue != null && dialogue.onEnterLines != null,
            label + " dialogue is missing on-enter teaching lines.");

        for (int lineIndex = 0; lineIndex < dialogue.onEnterLines.Length; lineIndex++)
        {
            RecipeKnowledgeReveal[] reveals = dialogue.GetOnEnterKnowledgeReveals(lineIndex);
            Require(reveals != null && reveals.Length > 0,
                label + " displayed teaching line has no typed knowledge reveals.");
            foreach (RecipeKnowledgeReveal reveal in reveals)
            {
                Require(reveal != null && reveal.TryApply(knowledge),
                    label + " displayed teaching reveal did not change knowledge.");
            }
        }
    }

    private static RecipeUnlockManager CreateKnowledgeManager(RecipeCatalogData catalog)
    {
        GameObject host = new("J4B Recipe Knowledge Validation")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        RecipeUnlockManager manager = host.AddComponent<RecipeUnlockManager>();
        SetPrivateField(manager, "recipeCatalog", catalog);
        InvokePrivate(manager, "Awake", null);
        return manager;
    }

    private static T FindSingleInScene<T>(Scene scene) where T : Component
    {
        T found = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (T candidate in root.GetComponentsInChildren<T>(true))
            {
                Require(found == null,
                    "BarScene contains more than one " + typeof(T).Name + " for J4 scheduling.");
                found = candidate;
            }
        }

        return found;
    }

    private static CocktailRecipeData FindRecipe(RecipeCatalogData catalog, string recipeId)
    {
        foreach (RecipeCatalogEntry entry in catalog.Entries)
        {
            if (entry != null && entry.Recipe != null && entry.Recipe.recipeId == recipeId)
                return entry.Recipe;
        }

        throw new InvalidOperationException("[J4B Validation] Missing recipe: " + recipeId);
    }

    private static IngredientData FindIngredient(CocktailRecipeData recipe, string ingredientId)
    {
        foreach (RecipeIngredientEntry entry in recipe.ingredients)
        {
            if (entry != null && entry.ingredient != null && entry.ingredient.ingredientId == ingredientId)
                return entry.ingredient;
        }

        throw new InvalidOperationException("[J4B Validation] Missing ingredient: " + ingredientId);
    }

    private static T LoadRequired<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Require(asset != null, "Missing required asset: " + path);
        return asset;
    }

    private static bool ContainsAll(string value, params string[] fragments)
    {
        foreach (string fragment in fragments)
        {
            if (!Contains(value, fragment))
                return false;
        }

        return true;
    }

    private static bool Contains(string value, string fragment)
    {
        return !string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(fragment) &&
               value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(field != null, instance.GetType().Name + " is missing field '" + fieldName + "'.");
        field.SetValue(instance, value);
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        FieldInfo field = instance.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(field != null, instance.GetType().Name + " is missing field '" + fieldName + "'.");
        return (T)field.GetValue(instance);
    }

    private static void SetPrivateEnum(object instance, string fieldName, string valueName)
    {
        FieldInfo field = instance.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(field != null && field.FieldType.IsEnum,
            instance.GetType().Name + " is missing enum field '" + fieldName + "'.");
        field.SetValue(instance, Enum.Parse(field.FieldType, valueName));
    }

    private static string GetPrivateEnumName(object instance, string fieldName)
    {
        return GetPrivateField<object>(instance, fieldName).ToString();
    }

    private static object InvokePrivate(object instance, string methodName, object[] arguments)
    {
        MethodInfo method = instance.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(method != null, instance.GetType().Name + " is missing method '" + methodName + "'.");
        return method.Invoke(instance, arguments);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("[J4 Validation] " + message);
    }

    private readonly struct ProgressSnapshot
    {
        private readonly bool hadKnowledge;
        private readonly string knowledge;
        private readonly bool hadLegacyKnowledge;
        private readonly string legacyKnowledge;
        private readonly bool hadFirstVisit;
        private readonly bool firstVisitCompleted;
        private readonly bool hadFollowupVisit;
        private readonly bool followupVisitCompleted;

        private ProgressSnapshot(
            bool hadKnowledge,
            string knowledge,
            bool hadLegacyKnowledge,
            string legacyKnowledge,
            bool hadFirstVisit,
            bool firstVisitCompleted,
            bool hadFollowupVisit,
            bool followupVisitCompleted)
        {
            this.hadKnowledge = hadKnowledge;
            this.knowledge = knowledge;
            this.hadLegacyKnowledge = hadLegacyKnowledge;
            this.legacyKnowledge = legacyKnowledge;
            this.hadFirstVisit = hadFirstVisit;
            this.firstVisitCompleted = firstVisitCompleted;
            this.hadFollowupVisit = hadFollowupVisit;
            this.followupVisitCompleted = followupVisitCompleted;
        }

        public static ProgressSnapshot Capture()
        {
            return new ProgressSnapshot(
                RecipeProgressPersistence.HasRecipeKnowledge(),
                RecipeProgressPersistence.LoadRecipeKnowledgeRaw(),
                RecipeProgressPersistence.HasLegacyUnlockedRecipeIds(),
                RecipeProgressPersistence.LoadLegacyUnlockedRecipeIdsRaw(),
                LittlePrinceVisitPersistence.HasSavedFirstVisitState(),
                LittlePrinceVisitPersistence.IsFirstVisitCompleted(),
                LittlePrinceVisitPersistence.HasSavedFollowupVisitState(),
                LittlePrinceVisitPersistence.IsFollowupVisitCompleted());
        }

        public void Restore()
        {
            if (hadKnowledge)
                RecipeProgressPersistence.SaveRecipeKnowledgeRaw(knowledge);
            else
                RecipeProgressPersistence.ClearRecipeKnowledge();

            if (hadLegacyKnowledge)
                RecipeProgressPersistence.SaveUnlockedRecipeIdsRaw(legacyKnowledge);
            else
                RecipeProgressPersistence.ClearLegacyUnlockedRecipeIds();

            if (hadFirstVisit)
                LittlePrinceVisitPersistence.SetFirstVisitCompleted(firstVisitCompleted);
            else
                LittlePrinceVisitPersistence.ClearFirstVisitCompleted();

            if (hadFollowupVisit)
                LittlePrinceVisitPersistence.SetFollowupVisitCompleted(followupVisitCompleted);
            else
                LittlePrinceVisitPersistence.ClearFollowupVisitCompleted();
        }
    }
}
#endif
