#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Focused I11C proof for the authored, four-encounter Day 1 Dry Martini arc.
/// It verifies only production data and the established I11A/I11B authority;
/// it does not create a second discovery or persistence implementation.
/// </summary>
public static class SURLHWADryMartiniDiscoveryArcValidation
{
    private const string CatalogPath = "Assets/_SURLHWA/Data/Recipes/RecipeCatalog.asset";
    private const string AvailabilityPath =
        "Assets/_SURLHWA/Data/Ingredients/IngredientAvailability.asset";

    private static readonly string[] OrderPaths =
    {
        "Assets/_SURLHWA/Data/Orders/DryMartiniMethodClueOrder.asset",
        "Assets/_SURLHWA/Data/Orders/DryMartiniGinClueOrder.asset",
        "Assets/_SURLHWA/Data/Orders/DryMartiniVermouthClueOrder.asset",
        "Assets/_SURLHWA/Data/Orders/DryMartiniAttemptOrder.asset"
    };

    [MenuItem("SURLHWA/Validation/Run I11C Dry Martini Discovery Arc")]
    public static void Run()
    {
        RecipeProgressSnapshot snapshot = RecipeProgressSnapshot.Capture();
        try
        {
            RecipeCatalogData catalog =
                AssetDatabase.LoadAssetAtPath<RecipeCatalogData>(CatalogPath);
            IngredientAvailabilityData availability =
                AssetDatabase.LoadAssetAtPath<IngredientAvailabilityData>(AvailabilityPath);
            Require(catalog != null, "RecipeCatalogData is missing.");
            Require(availability != null, "IngredientAvailabilityData is missing.");

            CocktailRecipeData dryMartini = FindRecipe(catalog, "recipe_dry_martini");
            IngredientData gin = FindIngredient(dryMartini, "gin");
            IngredientData dryVermouth = FindIngredient(dryMartini, "dry_vermouth");
            IngredientData rum = AssetDatabase.LoadAssetAtPath<IngredientData>(
                "Assets/_SURLHWA/Data/Ingredients/Ingredient_Rum.asset");
            Require(rum != null, "Rum ingredient is missing.");

            OrderData[] encounters = LoadEncounters();
            ValidateAuthoredArc(encounters, dryMartini);
            ValidateAvailability(availability, dryMartini);
            ValidatePersistentDiscoveryFlow(catalog, dryMartini, gin, dryVermouth, rum, encounters);

            Debug.Log(
                "[I11C Validation] PASS: authored multi-encounter clue pacing, no identity leak, " +
                "partial persistence, failed-attempt retention, false-fact safety, and matched Known transition are preserved.");
        }
        finally
        {
            snapshot.Restore();
        }
    }

    private static OrderData[] LoadEncounters()
    {
        OrderData[] encounters = new OrderData[OrderPaths.Length];
        for (int index = 0; index < OrderPaths.Length; index++)
        {
            encounters[index] = AssetDatabase.LoadAssetAtPath<OrderData>(OrderPaths[index]);
            Require(encounters[index] != null,
                "Missing I11C discovery order at " + OrderPaths[index] + ".");
        }

        return encounters;
    }

    private static void ValidateAuthoredArc(OrderData[] encounters, CocktailRecipeData dryMartini)
    {
        Require(encounters.Length == 4, "The Day 1 discovery arc must have four ordered encounters.");

        for (int index = 0; index < encounters.Length; index++)
        {
            OrderData encounter = encounters[index];
            Require(encounter.miniGameType == MiniGameType.Stir,
                "Every Dry Martini discovery encounter must enter the Stir route.");
            Require(encounter.requestedCocktail == dryMartini.targetCocktail,
                "Every discovery encounter must retain an internal Dry Martini target.");
            Require(encounter.PreBrewDialogue != null,
                "Every discovery encounter needs its player-consumed pre-brew dialogue.");
            Require(!ContainsAny(encounter.orderName, "Dry Martini", "드라이 마티니"),
                "Player-visible order name leaked Dry Martini before Known.");
            Require(!ContainsDialogue(encounter.PreBrewDialogue, "Dry Martini", "드라이 마티니"),
                "Player-visible dialogue leaked Dry Martini before Known.");
        }

        Require(HasSingleReveal(encounters[0].PreBrewDialogue, dryMartini, RecipeKnowledgeFact.Method, null),
            "Encounter 1 must teach only Stir through an authored dialogue fact.");
        Require(HasSingleReveal(encounters[1].PreBrewDialogue, dryMartini, RecipeKnowledgeFact.Ingredient,
                    FindIngredient(dryMartini, "gin")),
            "Encounter 2 must teach only Gin through an authored dialogue fact.");
        Require(HasSingleReveal(encounters[2].PreBrewDialogue, dryMartini, RecipeKnowledgeFact.Ingredient,
                    FindIngredient(dryMartini, "dry_vermouth")),
            "Encounter 3 must teach only Dry Vermouth through an authored dialogue fact.");
        DialogueData teaching = encounters[3].PreBrewDialogue;
        RecipeKnowledgeReveal[] amounts = teaching.GetOnEnterKnowledgeReveals(0);
        RecipeKnowledgeReveal[] preparation = teaching.GetOnEnterKnowledgeReveals(1);
        Require(teaching.onEnterLines.Length == 2 && amounts.Length == 2 && preparation.Length == 2,
            "The final attempt must teach both amounts, preparation and optional garnish through two consumed lines.");
        Require(amounts[0].Recipe == dryMartini && amounts[1].Recipe == dryMartini &&
                amounts[0].Fact == RecipeKnowledgeFact.IngredientAmount &&
                amounts[1].Fact == RecipeKnowledgeFact.IngredientAmount &&
                amounts[0].Ingredient == FindIngredient(dryMartini, "gin") &&
                amounts[1].Ingredient == FindIngredient(dryMartini, "dry_vermouth") &&
                preparation[0].Recipe == dryMartini && preparation[1].Recipe == dryMartini &&
                preparation[0].Fact == RecipeKnowledgeFact.Preparation &&
                preparation[1].Fact == RecipeKnowledgeFact.Garnish,
            "Final teaching must use exact recipe facts without granting identity or Known.");
    }

    private static void ValidateAvailability(
        IngredientAvailabilityData availability,
        CocktailRecipeData dryMartini)
    {
        foreach (RecipeIngredientEntry entry in dryMartini.ingredients)
        {
            Require(entry != null && entry.ingredient != null && availability.IsIngredientAvailable(
                        entry.ingredient, new IngredientAvailabilityContext(1)),
                "Dry Martini must remain physically constructible from the Day 1 shelf.");
        }
    }

    private static void ValidatePersistentDiscoveryFlow(
        RecipeCatalogData catalog,
        CocktailRecipeData dryMartini,
        IngredientData gin,
        IngredientData dryVermouth,
        IngredientData rum,
        OrderData[] encounters)
    {
        ClearKnowledgeForTest();
        RecipeUnlockManager manager = CreateKnowledgeManager(catalog);
        try
        {
            RecipeKnowledgeFacts unknown = manager.GetKnownFacts(dryMartini);
            Require(unknown.State == RecipeKnowledgeState.Unknown &&
                    RecipeKnowledgeGuideFormatter.GetCardLabel(dryMartini, unknown) == "???" &&
                    string.IsNullOrEmpty(RecipeKnowledgeGuideFormatter.BuildDetailText(dryMartini, unknown)),
                "Unknown Dry Martini must retain the mystery Guide projection.");

            ApplyConsumedDialogue(encounters[0].PreBrewDialogue, manager);
            RecipeKnowledgeFacts afterMethod = manager.GetKnownFacts(dryMartini);
            Require(afterMethod.State == RecipeKnowledgeState.Partial && afterMethod.MethodKnown &&
                    !afterMethod.IdentityKnown && !afterMethod.IsIngredientKnown(gin),
                "Encounter 1 must leave Dry Martini as method-only Partial knowledge.");
            Require(RecipeKnowledgeGuideFormatter.GetDetailName(dryMartini, afterMethod) == "이름을 모르는 칵테일" &&
                    RecipeKnowledgeGuideFormatter.GetMethodText(dryMartini, afterMethod) == "조주법: Stir",
                "Method-only Partial Guide projection leaked identity or hid Stir.");

            DestroyKnowledgeManager(manager);
            manager = CreateKnowledgeManager(catalog);
            Require(manager.GetKnownFacts(dryMartini).MethodKnown,
                "Encounter 1 fact did not persist across the next encounter/reload boundary.");

            ApplyConsumedDialogue(encounters[1].PreBrewDialogue, manager);
            RecipeKnowledgeFacts afterGin = manager.GetKnownFacts(dryMartini);
            Require(afterGin.State == RecipeKnowledgeState.Partial && afterGin.MethodKnown &&
                    afterGin.IsIngredientKnown(gin) && !afterGin.IsIngredientAmountKnown(gin) &&
                    !afterGin.IdentityKnown,
                "Encounter 2 must add Gin without identity or amount leakage.");
            Require(!manager.LearnIngredientFromPlayerUse(dryMartini, gin),
                "A repeated correct Gin experiment must be idempotent after the authored clue.");
            Require(!manager.LearnIngredientFromPlayerUse(dryMartini, rum) &&
                    !manager.GetKnownFacts(dryMartini).IsIngredientKnown(rum),
                "A wrong attempted ingredient became a false recipe fact.");

            ApplyConsumedDialogue(encounters[2].PreBrewDialogue, manager);
            RecipeKnowledgeFacts richerPartial = manager.GetKnownFacts(dryMartini);
            Require(richerPartial.State == RecipeKnowledgeState.Partial && richerPartial.MethodKnown &&
                    richerPartial.IsIngredientKnown(gin) && richerPartial.IsIngredientKnown(dryVermouth) &&
                    !richerPartial.IdentityKnown && !richerPartial.IsIngredientAmountKnown(gin) &&
                    !richerPartial.IsIngredientAmountKnown(dryVermouth),
                "Encounter 3 must create a richer Partial state without exact amounts or identity.");

            string partialDetail = RecipeKnowledgeGuideFormatter.BuildDetailText(dryMartini, richerPartial);
            Require(Contains(partialDetail, "Gin") && Contains(partialDetail, "Dry Vermouth") &&
                    !Contains(partialDetail, "Dry Martini") && !Contains(partialDetail, "2.03 oz") &&
                    !Contains(partialDetail, "0.34 oz"),
                "Partial Guide must show only learned ingredients, never identity or unlearned amounts.");

            DialogueData teaching = encounters[3].PreBrewDialogue;
            for (int line = 0; line < teaching.onEnterLines.Length; line++)
                foreach (RecipeKnowledgeReveal reveal in teaching.GetOnEnterKnowledgeReveals(line))
                    Require(reveal.TryApply(manager), "Consumed final teaching did not add its authored fact.");
            RecipeKnowledgeFacts taught = manager.GetKnownFacts(dryMartini);
            Require(taught.State == RecipeKnowledgeState.Partial && !taught.IdentityKnown &&
                    taught.IsIngredientAmountKnown(gin) && taught.IsIngredientAmountKnown(dryVermouth) &&
                    taught.PreparationKnown && taught.GarnishKnown,
                "Teaching must leave an unnamed Partial recipe with actionable quantities and preparation.");
            Require(ContainsAll(RecipeKnowledgeGuideFormatter.BuildDetailText(dryMartini, taught),
                        "2.03 oz", "0.34 oz", "젓기", "걸러 따르기") &&
                    RecipeKnowledgeGuideFormatter.GetDetailName(dryMartini, taught) == "이름을 모르는 칵테일",
                "Guide must retain the taught amounts/steps without revealing the cocktail identity.");

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
            Require(!failedMatch.success,
                "The deliberately incomplete Gin-only attempt unexpectedly discovered a recipe.");
            RecipeKnowledgeFacts afterFailure = manager.GetKnownFacts(dryMartini);
            Require(afterFailure.State == RecipeKnowledgeState.Partial && afterFailure.MethodKnown &&
                    afterFailure.IsIngredientKnown(gin) && afterFailure.IsIngredientKnown(dryVermouth),
                "A failed attempt changed or fully revealed recipe knowledge.");

            DestroyKnowledgeManager(manager);
            manager = CreateKnowledgeManager(catalog);
            RecipeKnowledgeFacts reloadedPartial = manager.GetKnownFacts(dryMartini);
            Require(reloadedPartial.State == RecipeKnowledgeState.Partial && reloadedPartial.MethodKnown &&
                    reloadedPartial.IsIngredientAmountKnown(gin) && reloadedPartial.IsIngredientAmountKnown(dryVermouth) &&
                    reloadedPartial.PreparationKnown && reloadedPartial.GarnishKnown && !reloadedPartial.IdentityKnown,
                "Richer Partial knowledge did not survive reload after the failed attempt.");

            RecipeMatchResult exactMatch = RecipeMatchService.FindBestMatch(
                BuildExactComposition(dryMartini), manager.GetAllRecipesByMethod(MiniGameType.Stir));
            Require(exactMatch.success && exactMatch.matchedRecipe == dryMartini,
                "Exact Dry Martini composition did not reach the existing matcher boundary.");
            Require(manager.MarkKnown(exactMatch.matchedRecipe),
                "Successful discovery did not promote Partial Dry Martini to Known.");
            Require(!manager.MarkKnown(exactMatch.matchedRecipe),
                "Repeated successful discovery must remain idempotent.");
            RecipeKnowledgeFacts known = manager.GetKnownFacts(dryMartini);
            Require(known.State == RecipeKnowledgeState.Known &&
                    RecipeKnowledgeGuideFormatter.GetDetailName(dryMartini, known) == "Dry Martini" &&
                    ContainsAll(RecipeKnowledgeGuideFormatter.BuildDetailText(dryMartini, known),
                        "Gin", "Dry Vermouth", "2.03 oz", "0.34 oz", "젓기", "Cocktail Glass"),
                "Known Dry Martini did not expose the supported full Guide truth.");
            Require(CountOccurrences(RecipeProgressPersistence.LoadRecipeKnowledgeRaw(), "recipe_dry_martini") == 1,
                "Idempotent production progression duplicated the saved recipe entry.");
        }
        finally
        {
            DestroyKnowledgeManager(manager);
        }
    }

    private static bool HasSingleReveal(
        DialogueData dialogue,
        CocktailRecipeData recipe,
        RecipeKnowledgeFact fact,
        IngredientData ingredient)
    {
        RecipeKnowledgeReveal[] reveals = dialogue != null
            ? dialogue.GetOnEnterKnowledgeReveals(0)
            : Array.Empty<RecipeKnowledgeReveal>();
        return reveals.Length == 1 && reveals[0] != null && reveals[0].Recipe == recipe &&
               reveals[0].Fact == fact && reveals[0].Ingredient == ingredient;
    }

    private static void ApplyConsumedDialogue(DialogueData dialogue, RecipeUnlockManager manager)
    {
        RecipeKnowledgeReveal[] reveals = dialogue.GetOnEnterKnowledgeReveals(0);
        Require(reveals.Length == 1 && reveals[0].TryApply(manager),
            "The displayed authored dialogue did not produce its single knowledge fact.");
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

    private static RecipeUnlockManager CreateKnowledgeManager(RecipeCatalogData catalog)
    {
        GameObject host = new("I11C Recipe Knowledge Validation")
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
            if (entry != null && entry.Recipe != null && entry.Recipe.recipeId == recipeId)
                return entry.Recipe;
        }

        throw new InvalidOperationException("[I11C Validation] Missing recipe '" + recipeId + "'.");
    }

    private static IngredientData FindIngredient(CocktailRecipeData recipe, string ingredientId)
    {
        foreach (RecipeIngredientEntry entry in recipe.ingredients)
        {
            if (entry != null && entry.ingredient != null && entry.ingredient.ingredientId == ingredientId)
                return entry.ingredient;
        }

        throw new InvalidOperationException(
            "[I11C Validation] Missing ingredient '" + ingredientId + "'.");
    }

    private static bool ContainsDialogue(DialogueData dialogue, params string[] values)
    {
        if (dialogue == null || dialogue.onEnterLines == null)
            return false;

        foreach (string line in dialogue.onEnterLines)
        {
            foreach (string value in values)
            {
                if (Contains(line, value))
                    return true;
            }
        }

        return false;
    }

    private static bool ContainsAny(string value, params string[] fragments)
    {
        foreach (string fragment in fragments)
        {
            if (Contains(value, fragment))
                return true;
        }

        return false;
    }

    private static bool Contains(string value, string fragment)
    {
        return !string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(fragment) &&
               value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
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

    private static int CountOccurrences(string value, string fragment)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(fragment))
            return 0;

        int count = 0;
        int offset = 0;
        while ((offset = value.IndexOf(fragment, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += fragment.Length;
        }

        return count;
    }

    private static void ClearKnowledgeForTest()
    {
        RecipeProgressPersistence.ClearRecipeKnowledge();
        RecipeProgressPersistence.ClearLegacyUnlockedRecipeIds();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("[I11C Validation] " + message);
    }

    private readonly struct RecipeProgressSnapshot
    {
        private readonly bool hadKnowledge;
        private readonly string knowledgeRaw;
        private readonly bool hadLegacy;
        private readonly string legacyRaw;

        private RecipeProgressSnapshot(bool hadKnowledge, string knowledgeRaw, bool hadLegacy, string legacyRaw)
        {
            this.hadKnowledge = hadKnowledge;
            this.knowledgeRaw = knowledgeRaw;
            this.hadLegacy = hadLegacy;
            this.legacyRaw = legacyRaw;
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

            if (hadLegacy)
                RecipeProgressPersistence.SaveUnlockedRecipeIdsRaw(legacyRaw);
            else
                RecipeProgressPersistence.ClearLegacyUnlockedRecipeIds();
        }
    }
}
#endif
