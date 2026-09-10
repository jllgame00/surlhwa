#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// I11B edit-mode proof for authored hint facts and actual accepted shelf
/// liquid actions. It uses the same reveal and ingredient-use bridge used by
/// production, while preserving the player's exact prior recipe progress.
/// </summary>
public static class SURLHWARecipeKnowledgeDiscoveryValidation
{
    private const string CatalogPath = "Assets/_SURLHWA/Data/Recipes/RecipeCatalog.asset";
    private const string TestStirOrderPath = "Assets/_SURLHWA/Data/Orders/TestStir.asset";
    private const string RumPath = "Assets/_SURLHWA/Data/Ingredients/Ingredient_Rum.asset";
    private const string ShelfPrefabPath = "Assets/_SURLHWA/Prefabs/UI/IngredientShelfPanel.prefab";

    [MenuItem("SURLHWA/Validation/Run I11B Production Discovery")]
    public static void Run()
    {
        RecipeProgressSnapshot snapshot = RecipeProgressSnapshot.Capture();
        try
        {
            RecipeCatalogData catalog = AssetDatabase.LoadAssetAtPath<RecipeCatalogData>(CatalogPath);
            OrderData testStirOrder = AssetDatabase.LoadAssetAtPath<OrderData>(TestStirOrderPath);
            IngredientData rum = AssetDatabase.LoadAssetAtPath<IngredientData>(RumPath);
            GameObject shelfPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShelfPrefabPath);
            Require(catalog != null, "RecipeCatalogData is missing.");
            Require(testStirOrder != null, "TestStir order is missing.");
            Require(rum != null, "Rum ingredient is missing.");
            Require(shelfPrefab != null, "Ingredient shelf prefab is missing.");

            CocktailRecipeData dryMartini = FindRecipe(catalog, "recipe_dry_martini");
            IngredientData gin = FindIngredient(dryMartini, "gin");
            ValidateAuthoredHint(testStirOrder, dryMartini);
            ValidateProductionFlow(catalog, testStirOrder, dryMartini, gin, rum, shelfPrefab);

            Debug.Log(
                "[I11B Validation] PASS: consumed authored hint, correct player-used ingredient, " +
                "wrong-ingredient safety, idempotency, reload, and matched discovery are preserved.");
        }
        finally
        {
            snapshot.Restore();
        }
    }

    private static void ValidateAuthoredHint(OrderData order, CocktailRecipeData dryMartini)
    {
        Require(order.miniGameType == MiniGameType.Stir,
            "The tutorial discovery order must remain a Stir request.");
        Require(order.requestedCocktail == dryMartini.targetCocktail,
            "The tutorial discovery order must explicitly target Dry Martini.");
        Require(order.HasRecipeKnowledgeHint,
            "The tutorial discovery order must have a consumed authored hint.");
        Require(order.RecipeKnowledgeHintReveals.Length == 1,
            "The initial tutorial hint must have exactly one bounded reveal.");

        RecipeKnowledgeReveal reveal = order.RecipeKnowledgeHintReveals[0];
        Require(reveal != null && reveal.Recipe == dryMartini &&
                reveal.Fact == RecipeKnowledgeFact.Method,
            "The initial tutorial hint must explicitly teach only Dry Martini's method.");
    }

    private static void ValidateProductionFlow(
        RecipeCatalogData catalog,
        OrderData order,
        CocktailRecipeData dryMartini,
        IngredientData gin,
        IngredientData rum,
        GameObject shelfPrefab)
    {
        ClearKnowledgeForTest();
        RecipeUnlockManager manager = CreateKnowledgeManager(catalog);
        GameObject shelfHost = UnityEngine.Object.Instantiate(shelfPrefab);
        shelfHost.name = "I11B Ingredient Use Validation";
        shelfHost.hideFlags = HideFlags.HideAndDontSave;
        IngredientShelfController shelf = shelfHost.GetComponentInChildren<IngredientShelfController>(true);
        Require(shelf != null, "Ingredient shelf prefab has no IngredientShelfController.");

        try
        {
            foreach (RecipeKnowledgeReveal reveal in order.RecipeKnowledgeHintReveals)
                Require(reveal.TryApply(manager), "Consumed authored hint did not learn its fact.");

            Require(!order.RecipeKnowledgeHintReveals[0].TryApply(manager),
                "Consuming the same authored hint twice must be idempotent.");

            RecipeKnowledgeFacts afterHint = manager.GetKnownFacts(dryMartini);
            Require(afterHint.State == RecipeKnowledgeState.Partial && afterHint.MethodKnown &&
                    !afterHint.IdentityKnown && !afterHint.IsIngredientKnown(gin),
                "The consumed hint must reveal only Dry Martini's method.");

            BartendingRequestContext request = new(
                order.miniGameType,
                order.orderName,
                dryMartini,
                1);
            shelf.Initialize(request.Route, BartendingContainerType.MixingGlass, request.AvailabilityContext);
            RecipeKnowledgeIngredientUseDiscovery.Bind(shelf, request.ExpectedRecipe, manager);

            Require(shelf.TrySelectIngredient(rum), "Could not select wrong ingredient for production proof.");
            Require(shelf.AddLiquidMl(rum, 15f) > 0f,
                "Wrong ingredient action was not recorded by the actual shelf.");
            Require(shelf.PreparationTrace.Actions.Count == 1 &&
                    shelf.PreparationTrace.Actions[0].Ingredient == rum,
                "Wrong ingredient must remain actual preparation/action-trace truth.");
            Require(!manager.GetKnownFacts(dryMartini).IsIngredientKnown(rum),
                "Wrong player ingredient became a false recipe fact.");

            Require(shelf.TrySelectIngredient(gin), "Could not select correct Gin for production proof.");
            Require(shelf.AddLiquidMl(gin, 15f) > 0f,
                "Correct ingredient action was not accepted by the actual shelf.");
            Require(shelf.AddLiquidMl(gin, 15f) > 0f,
                "Repeated correct ingredient action was not accepted by the actual shelf.");

            RecipeKnowledgeFacts afterGin = manager.GetKnownFacts(dryMartini);
            Require(afterGin.State == RecipeKnowledgeState.Partial && afterGin.MethodKnown &&
                    afterGin.IsIngredientKnown(gin) && !afterGin.IsIngredientAmountKnown(gin),
                "Correct player use must reveal Gin but never its amount.");
            Require(afterGin.KnownIngredientIds.Count == 1,
                "Repeated correct ingredient use duplicated the knowledge fact.");

            DestroyKnowledgeManager(manager);
            manager = CreateKnowledgeManager(catalog);
            RecipeKnowledgeFacts reloaded = manager.GetKnownFacts(dryMartini);
            Require(reloaded.MethodKnown && reloaded.IsIngredientKnown(gin) &&
                    !reloaded.IsIngredientAmountKnown(gin),
                "Production clue and player-used ingredient facts did not survive reload.");

            RecipeMatchResult match = RecipeMatchService.FindBestMatch(
                BuildExactComposition(dryMartini),
                manager.GetAllRecipesByMethod(dryMartini.method));
            Require(match.success && match.matchedRecipe == dryMartini,
                "Exact Dry Martini composition did not reach the existing matcher boundary.");
            Require(manager.MarkKnown(match.matchedRecipe),
                "Successful recipe discovery did not promote Partial Dry Martini to Known.");
            Require(!manager.MarkKnown(match.matchedRecipe),
                "Repeated successful discovery must not create a second Known transition.");
            Require(manager.GetKnowledgeState(dryMartini) == RecipeKnowledgeState.Known,
                "Matched recipe did not become Known.");
        }
        finally
        {
            if (shelfHost != null)
                UnityEngine.Object.DestroyImmediate(shelfHost);
            DestroyKnowledgeManager(manager);
        }
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

        return new BrewComposition
        {
            method = recipe.method,
            ingredients = ingredients.ToArray()
        };
    }

    private static RecipeUnlockManager CreateKnowledgeManager(RecipeCatalogData catalog)
    {
        GameObject host = new("I11B Recipe Knowledge Validation")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        RecipeUnlockManager manager = host.AddComponent<RecipeUnlockManager>();
        SerializedObject serializedManager = new(manager);
        SerializedProperty catalogProperty = serializedManager.FindProperty("recipeCatalog");
        Require(catalogProperty != null, "RecipeUnlockManager.recipeCatalog field is missing.");
        catalogProperty.objectReferenceValue = catalog;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();

        MethodInfo awake = typeof(RecipeUnlockManager).GetMethod(
            "Awake",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(awake != null, "RecipeUnlockManager.Awake is missing.");
        awake.Invoke(manager, null);
        return manager;
    }

    private static void DestroyKnowledgeManager(RecipeUnlockManager manager)
    {
        if (manager != null)
            UnityEngine.Object.DestroyImmediate(manager.gameObject);
    }

    private static void ClearKnowledgeForTest()
    {
        RecipeProgressPersistence.ClearRecipeKnowledge();
        RecipeProgressPersistence.ClearLegacyUnlockedRecipeIds();
    }

    private static CocktailRecipeData FindRecipe(RecipeCatalogData catalog, string recipeId)
    {
        foreach (RecipeCatalogEntry entry in catalog.Entries)
        {
            if (entry != null && entry.Recipe != null && entry.Recipe.recipeId == recipeId)
                return entry.Recipe;
        }

        throw new InvalidOperationException("[I11B Validation] Missing recipe '" + recipeId + "'.");
    }

    private static IngredientData FindIngredient(CocktailRecipeData recipe, string ingredientId)
    {
        foreach (RecipeIngredientEntry entry in recipe.ingredients)
        {
            if (entry != null && entry.ingredient != null && entry.ingredient.ingredientId == ingredientId)
                return entry.ingredient;
        }

        throw new InvalidOperationException(
            "[I11B Validation] Missing ingredient '" + ingredientId + "' in " + recipe.recipeId + ".");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("[I11B Validation] " + message);
    }

    private readonly struct RecipeProgressSnapshot
    {
        private readonly bool hadKnowledge;
        private readonly string knowledgeRaw;
        private readonly bool hadLegacyUnlocks;
        private readonly string legacyUnlocksRaw;

        private RecipeProgressSnapshot(
            bool hadKnowledge,
            string knowledgeRaw,
            bool hadLegacyUnlocks,
            string legacyUnlocksRaw)
        {
            this.hadKnowledge = hadKnowledge;
            this.knowledgeRaw = knowledgeRaw;
            this.hadLegacyUnlocks = hadLegacyUnlocks;
            this.legacyUnlocksRaw = legacyUnlocksRaw;
        }

        public static RecipeProgressSnapshot Capture()
        {
            return new RecipeProgressSnapshot(
                RecipeProgressPersistence.HasRecipeKnowledge(),
                RecipeProgressPersistence.LoadRecipeKnowledgeRaw(),
                RecipeProgressPersistence.HasLegacyUnlockedRecipeIds(),
                RecipeProgressPersistence.LoadLegacyUnlockedRecipeIdsRaw());
        }

        public void Restore()
        {
            if (hadKnowledge)
                RecipeProgressPersistence.SaveRecipeKnowledgeRaw(knowledgeRaw);
            else
                RecipeProgressPersistence.ClearRecipeKnowledge();

            if (hadLegacyUnlocks)
                RecipeProgressPersistence.SaveUnlockedRecipeIdsRaw(legacyUnlocksRaw);
            else
                RecipeProgressPersistence.ClearLegacyUnlockedRecipeIds();
        }
    }
}
#endif

#if UNITY_EDITOR
/// <summary>
/// Real Play Mode companion for I11B. It uses the shipped TestStir order and
/// Stir prefab, then exercises the exact authored reveal and accepted-shelf
/// bridge used during normal bartending.
/// </summary>
[InitializeOnLoad]
public static class SURLHWARecipeKnowledgeDiscoveryPlayModeValidation
{
    private const string PendingKey = "SURLHWA_I11B_PLAYMODE_PENDING";
    private const string CompleteKey = "SURLHWA_I11B_PLAYMODE_COMPLETE";
    private const string SuccessKey = "SURLHWA_I11B_PLAYMODE_SUCCESS";
    private const string CatalogPath = "Assets/_SURLHWA/Data/Recipes/RecipeCatalog.asset";
    private const string TestStirOrderPath = "Assets/_SURLHWA/Data/Orders/TestStir.asset";
    private const string StirPrefabPath = "Assets/_SURLHWA/Prefabs/Bartending/StirMiniGame.prefab";

    static SURLHWARecipeKnowledgeDiscoveryPlayModeValidation()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    [MenuItem("SURLHWA/Validation/Run I11B Production Discovery Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[I11B PlayMode Validation] Exit Play Mode before starting this validation.");
            return;
        }

        SessionState.SetBool(PendingKey, true);
        SessionState.SetBool(CompleteKey, false);
        SessionState.SetBool(SuccessKey, false);
        EditorApplication.EnterPlaymode();
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
        {
            RunInPlayMode();
            return;
        }

        if (state != PlayModeStateChange.EnteredEditMode ||
            !SessionState.GetBool(CompleteKey, false))
        {
            return;
        }

        int exitCode = SessionState.GetBool(SuccessKey, false) ? 0 : 1;
        SessionState.EraseBool(PendingKey);
        SessionState.EraseBool(CompleteKey);
        SessionState.EraseBool(SuccessKey);
        EditorApplication.Exit(exitCode);
    }

    private static void RunInPlayMode()
    {
        DiscoveryProgressSnapshot snapshot = DiscoveryProgressSnapshot.Capture();
        RecipeUnlockManager manager = null;
        GameObject stirInstance = null;
        bool success = false;

        try
        {
            RecipeCatalogData catalog = AssetDatabase.LoadAssetAtPath<RecipeCatalogData>(CatalogPath);
            OrderData order = AssetDatabase.LoadAssetAtPath<OrderData>(TestStirOrderPath);
            GameObject stirPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StirPrefabPath);
            Require(catalog != null && order != null && stirPrefab != null,
                "Production TestStir assets are missing.");

            CocktailRecipeData dryMartini = FindRecipe(catalog, "recipe_dry_martini");
            IngredientData gin = FindIngredient(dryMartini, "gin");
            Require(order.requestedCocktail == dryMartini.targetCocktail &&
                    order.RecipeKnowledgeHintReveals.Length == 1,
                "TestStir must retain its authored Dry Martini hint context.");

            RecipeProgressPersistence.ClearRecipeKnowledge();
            RecipeProgressPersistence.ClearLegacyUnlockedRecipeIds();
            manager = CreateKnowledgeManager(catalog);

            // This is the same data dispatch InteractionUI performs after its
            // displayed request-hint line has been consumed.
            Require(order.RecipeKnowledgeHintReveals[0].TryApply(manager),
                "Consumed TestStir hint did not produce Partial knowledge.");

            BartendingRequestContext request = new(
                order.miniGameType,
                order.orderName,
                dryMartini,
                1);
            stirInstance = UnityEngine.Object.Instantiate(stirPrefab);
            IngredientShelfController shelf =
                stirInstance.GetComponentInChildren<IngredientShelfController>(true);
            Require(shelf != null, "Stir prefab has no IngredientShelfController.");
            shelf.Initialize(request.Route, BartendingContainerType.MixingGlass, request.AvailabilityContext);
            RecipeKnowledgeIngredientUseDiscovery.Bind(shelf, request.ExpectedRecipe, manager);

            Require(shelf.TrySelectIngredient(gin) && shelf.AddLiquidMl(gin, 15f) > 0f,
                "Actual Stir shelf did not accept Gin.");
            RecipeKnowledgeFacts partial = manager.GetKnownFacts(dryMartini);
            Require(partial.State == RecipeKnowledgeState.Partial && partial.MethodKnown &&
                    partial.IsIngredientKnown(gin) && !partial.IsIngredientAmountKnown(gin),
                "Play Mode path did not project method-only clue plus Gin fact.");

            RecipeMatchResult match = RecipeMatchService.FindBestMatch(
                BuildExactComposition(dryMartini),
                manager.GetAllRecipesByMethod(dryMartini.method));
            Require(match.success && manager.MarkKnown(match.matchedRecipe),
                "Successful production matcher path did not promote Dry Martini to Known.");
            Require(manager.GetKnowledgeState(dryMartini) == RecipeKnowledgeState.Known,
                "Play Mode successful discovery did not produce Known.");

            success = true;
            Debug.Log(
                "[I11B PlayMode Validation] PASS: TestStir consumed clue, actual shelf Gin use, and successful discovery reached Known.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            if (stirInstance != null)
                UnityEngine.Object.Destroy(stirInstance);
            if (manager != null)
                UnityEngine.Object.Destroy(manager.gameObject);
            snapshot.Restore();
            SessionState.SetBool(PendingKey, false);
            SessionState.SetBool(SuccessKey, success);
            SessionState.SetBool(CompleteKey, true);
            EditorApplication.ExitPlaymode();
        }
    }

    private static RecipeUnlockManager CreateKnowledgeManager(RecipeCatalogData catalog)
    {
        GameObject host = new("I11B PlayMode Recipe Knowledge Validation");
        RecipeUnlockManager manager = host.AddComponent<RecipeUnlockManager>();
        FieldInfo catalogField = typeof(RecipeUnlockManager).GetField(
            "recipeCatalog",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(catalogField != null, "RecipeUnlockManager.recipeCatalog field is missing.");
        catalogField.SetValue(manager, catalog);
        MethodInfo awake = typeof(RecipeUnlockManager).GetMethod(
            "Awake",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(awake != null, "RecipeUnlockManager.Awake is missing.");
        awake.Invoke(manager, null);
        return manager;
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

    private static CocktailRecipeData FindRecipe(RecipeCatalogData catalog, string recipeId)
    {
        foreach (RecipeCatalogEntry entry in catalog.Entries)
        {
            if (entry != null && entry.Recipe != null && entry.Recipe.recipeId == recipeId)
                return entry.Recipe;
        }

        throw new InvalidOperationException("[I11B PlayMode Validation] Missing recipe '" + recipeId + "'.");
    }

    private static IngredientData FindIngredient(CocktailRecipeData recipe, string ingredientId)
    {
        foreach (RecipeIngredientEntry entry in recipe.ingredients)
        {
            if (entry != null && entry.ingredient != null && entry.ingredient.ingredientId == ingredientId)
                return entry.ingredient;
        }

        throw new InvalidOperationException(
            "[I11B PlayMode Validation] Missing ingredient '" + ingredientId + "'.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("[I11B PlayMode Validation] " + message);
    }

    private readonly struct DiscoveryProgressSnapshot
    {
        private readonly bool hadKnowledge;
        private readonly string knowledgeRaw;
        private readonly bool hadLegacy;
        private readonly string legacyRaw;

        private DiscoveryProgressSnapshot(bool hadKnowledge, string knowledgeRaw, bool hadLegacy, string legacyRaw)
        {
            this.hadKnowledge = hadKnowledge;
            this.knowledgeRaw = knowledgeRaw;
            this.hadLegacy = hadLegacy;
            this.legacyRaw = legacyRaw;
        }

        public static DiscoveryProgressSnapshot Capture()
        {
            return new DiscoveryProgressSnapshot(
                RecipeProgressPersistence.HasRecipeKnowledge(),
                RecipeProgressPersistence.LoadRecipeKnowledgeRaw(),
                RecipeProgressPersistence.HasLegacyUnlockedRecipeIds(),
                RecipeProgressPersistence.LoadLegacyUnlockedRecipeIdsRaw());
        }

        public void Restore()
        {
            if (hadKnowledge)
                RecipeProgressPersistence.SaveRecipeKnowledgeRaw(knowledgeRaw);
            else
                RecipeProgressPersistence.ClearRecipeKnowledge();

            if (hadLegacy)
                RecipeProgressPersistence.SaveUnlockedRecipeIdsRaw(legacyRaw);
            else
                RecipeProgressPersistence.ClearLegacyUnlockedRecipeIds();
        }
    }
}
#endif
