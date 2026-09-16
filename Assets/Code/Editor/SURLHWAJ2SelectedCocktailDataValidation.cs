#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Verifies the approved six-drink demo data contract. This is a data
/// regression proof over existing CocktailData, recipe, catalog, and
/// availability authorities; it creates no parallel catalog or progression
/// model.
/// </summary>
public static class SURLHWAJ2SelectedCocktailDataValidation
{
    private const string CatalogPath = "Assets/_SURLHWA/Data/Recipes/RecipeCatalog.asset";
    private const string AvailabilityPath = "Assets/_SURLHWA/Data/Ingredients/IngredientAvailability.asset";
    private const string ShelfPath = "Assets/_SURLHWA/Prefabs/UI/IngredientShelfPanel.prefab";
    private const string LittlePrinceFirstOrderPath =
        "Assets/_SURLHWA/Data/Orders/LittlePrinceFirstVisitOrder.asset";
    private const string LittlePrinceFollowupOrderPath =
        "Assets/_SURLHWA/Data/Orders/LittlePrinceFollowupOrder.asset";

    private static readonly SelectedCocktailSpec[] SelectedCocktails =
    {
        new(
            "Seolya",
            "Assets/_SURLHWA/Data/Cocktails/TestStirCocktail.asset",
            "Assets/_SURLHWA/Data/Recipes/Recipe_TestStir.asset",
            "cocktail_seolya",
            "recipe_test_stir",
            true,
            CocktailGlassware.OldFashionedGlass,
            MiniGameType.Stir,
            new[] { EmotionTag.Softness, EmotionTag.Warmth }),
        new(
            "Manwol",
            "Assets/_SURLHWA/Data/Cocktails/TestFloatCocktail.asset",
            "Assets/_SURLHWA/Data/Recipes/Recipe_TestFloat.asset",
            "cocktail_manwol",
            "recipe_manwol",
            false,
            CocktailGlassware.OldFashionedGlass,
            MiniGameType.Float,
            new[] { EmotionTag.Longing, EmotionTag.Clarity }),
        new(
            "Dry Martini",
            "Assets/_SURLHWA/Data/Cocktails/Cocktail_DryMartini.asset",
            "Assets/_SURLHWA/Data/Recipes/Recipe_DryMartini.asset",
            "cocktail_dry_martini",
            "recipe_dry_martini",
            false,
            CocktailGlassware.CocktailGlass,
            MiniGameType.Stir,
            new[] { EmotionTag.Clarity }),
        new(
            "Negroni",
            "Assets/_SURLHWA/Data/Cocktails/Cocktail_Negroni.asset",
            "Assets/_SURLHWA/Data/Recipes/Recipe_Negroni.asset",
            "cocktail_negroni",
            "recipe_negroni",
            false,
            CocktailGlassware.OldFashionedGlass,
            MiniGameType.Build,
            new[] { EmotionTag.Bitterness }),
        new(
            "Manhattan",
            "Assets/_SURLHWA/Data/Cocktails/Cocktail_Manhattan.asset",
            "Assets/_SURLHWA/Data/Recipes/Recipe_Manhattan.asset",
            "cocktail_manhattan",
            "recipe_manhattan",
            false,
            CocktailGlassware.CocktailGlass,
            MiniGameType.Stir,
            new[] { EmotionTag.Warmth, EmotionTag.Bitterness }),
        new(
            "Gin Fizz",
            "Assets/_SURLHWA/Data/Cocktails/Cocktail_GinFizz.asset",
            "Assets/_SURLHWA/Data/Recipes/Recipe_GinFizz.asset",
            "cocktail_gin_fizz",
            "recipe_gin_fizz",
            false,
            CocktailGlassware.TallTumbler,
            MiniGameType.Shake,
            new[] { EmotionTag.Awakening, EmotionTag.Clarity })
    };

    private static readonly string[] ReserveRecipePaths =
    {
        "Assets/_SURLHWA/Data/Recipes/Recipe_HankyPanky.asset",
        "Assets/_SURLHWA/Data/Recipes/Recipe_French75.asset",
        "Assets/_SURLHWA/Data/Recipes/Recipe_LastWord.asset"
    };

    private static readonly string[] CompatibilityRecipePaths =
    {
        "Assets/_SURLHWA/Data/Recipes/Recipe_TestShake.asset",
        "Assets/_SURLHWA/Data/Recipes/Recipe_TestBuild.asset"
    };

    private static readonly string[][] ExpectedAvailableIngredientIdsByDay =
    {
        new[]
        {
            "whiskey", "vermouth", "water", "rum", "lime", "syrup", "tonic", "lemon",
            "vodka", "coffee_liqueur", "cream", "gin", "dry_vermouth"
        },
        new[]
        {
            "whiskey", "vermouth", "water", "rum", "lime", "syrup", "tonic", "lemon",
            "vodka", "coffee_liqueur", "cream", "gin", "dry_vermouth", "sweet_red_vermouth", "campari"
        },
        new[]
        {
            "whiskey", "vermouth", "water", "rum", "lime", "syrup", "tonic", "lemon",
            "vodka", "coffee_liqueur", "cream", "gin", "dry_vermouth", "sweet_red_vermouth", "campari",
            "rye_whiskey", "angostura_bitters"
        },
        new[]
        {
            "whiskey", "vermouth", "water", "rum", "lime", "syrup", "tonic", "lemon",
            "vodka", "coffee_liqueur", "cream", "gin", "dry_vermouth", "sweet_red_vermouth", "campari",
            "rye_whiskey", "angostura_bitters", "fresh_lemon_juice", "simple_syrup", "soda_water"
        },
        new[]
        {
            "whiskey", "vermouth", "water", "rum", "lime", "syrup", "tonic", "lemon",
            "vodka", "coffee_liqueur", "cream", "gin", "dry_vermouth", "sweet_red_vermouth", "campari",
            "rye_whiskey", "angostura_bitters", "fresh_lemon_juice", "simple_syrup", "soda_water",
            "champagne", "fernet", "london_dry_gin", "fresh_lime_juice", "green_chartreuse", "maraschino_luxardo"
        }
    };

    [MenuItem("SURLHWA/Validation/Run J2 Selected Cocktail Data")]
    public static void Run()
    {
        RecipeCatalogData catalog = LoadRequired<RecipeCatalogData>(CatalogPath);
        IngredientAvailabilityData availability = LoadRequired<IngredientAvailabilityData>(AvailabilityPath);
        GameObject shelfRoot = PrefabUtility.LoadPrefabContents(ShelfPath);

        try
        {
            IngredientShelfController shelf = shelfRoot.GetComponentInChildren<IngredientShelfController>(true);
            Require(shelf != null, "Shared ingredient shelf is missing its controller.");
            Require(shelf.AvailabilityData == availability,
                "Shared ingredient shelf no longer references the availability authority.");

            Dictionary<string, IngredientData> ingredients = BuildIngredientMap(shelf.ConfiguredIngredients);
            ValidateSelectedCocktailData(catalog);
            ValidateSelectedPreparationContracts(catalog);
            ValidateOriginalPreparationContracts(catalog);
            ValidateDemoAvailability(availability, shelf.ConfiguredIngredients, ingredients, catalog);
            ValidateIdentityBoundaries(ingredients);
            ValidateCatalogAndCompatibility(catalog, availability, shelf.ConfiguredIngredients);
            ValidateDryMartiniAndManwol(catalog, availability, ingredients);
            ValidateArtAssignments();

            Debug.Log(
                "[J2 Validation] PASS: selected demo cocktail identity, emotion tags, player-facing " +
                "descriptions, existing preparation metadata, staged ordinary supply, reserve exclusion, " +
                "and Dry Martini/Manwol compatibility are coherent.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(shelfRoot);
        }
    }

    private static void ValidateSelectedCocktailData(RecipeCatalogData catalog)
    {
        HashSet<string> cocktailIds = new();
        HashSet<string> recipeIds = new();

        foreach (SelectedCocktailSpec spec in SelectedCocktails)
        {
            CocktailData cocktail = LoadRequired<CocktailData>(spec.CocktailPath);
            CocktailRecipeData recipe = LoadRequired<CocktailRecipeData>(spec.RecipePath);

            Require(cocktail.cocktailId == spec.CocktailId && cocktailIds.Add(cocktail.cocktailId),
                spec.Label + " must retain its unique stable cocktail ID.");
            Require(recipe.recipeId == spec.RecipeId && recipeIds.Add(recipe.recipeId),
                spec.Label + " must retain its unique stable recipe ID.");
            Require(recipe.targetCocktail == cocktail,
                spec.Label + " recipe no longer resolves to its CocktailData authority.");
            Require(recipe.method == spec.Method,
                spec.Label + " method drifted from its approved demo role.");
            Require(cocktail.glassware == spec.Glassware,
                spec.Label + " glassware metadata drifted from the approved data baseline.");
            Require(catalog.Entries.Any(entry => entry != null && entry.Recipe == recipe),
                spec.Label + " is missing from RecipeCatalogData.");
            Require(catalog.IsUnlockedByDefault(recipe) == spec.UnlockedByDefault,
                spec.Label + " knowledge default changed unexpectedly.");
            Require(recipe.ingredients != null && recipe.ingredients.Length > 0 &&
                    recipe.ingredients.All(entry => entry != null && entry.ingredient != null),
                spec.Label + " has an unresolved ingredient reference.");
            Require(TagSequenceEquals(cocktail.emotionTags, spec.EmotionTags),
                spec.Label + " emotion tags do not match the approved J2 matrix.");

            if (spec.IsIba)
                ValidatePlayerFacingDescription(cocktail.description, spec.Label);
        }
    }

    private static void ValidateSelectedPreparationContracts(RecipeCatalogData catalog)
    {
        ValidateActionSequence(FindRecipe(catalog, "recipe_dry_martini"),
            RecipePreparationAction.Pour,
            RecipePreparationAction.Pour,
            RecipePreparationAction.AddIce,
            RecipePreparationAction.Stir,
            RecipePreparationAction.Strain,
            RecipePreparationAction.Garnish);
        ValidateGarnish(
            FindRecipe(catalog, "recipe_dry_martini"),
            true,
            new RecipeGarnishExpectation { garnish = BartendingGarnishId.LemonTwist, placement = BartendingGarnishPlacement.Rim },
            new RecipeGarnishExpectation { garnish = BartendingGarnishId.Olive, placement = BartendingGarnishPlacement.InGlass });

        ValidateActionSequence(FindRecipe(catalog, "recipe_negroni"),
            RecipePreparationAction.Pour,
            RecipePreparationAction.Pour,
            RecipePreparationAction.Pour,
            RecipePreparationAction.AddIce,
            RecipePreparationAction.GentleStir,
            RecipePreparationAction.Garnish);
        ValidateGarnish(
            FindRecipe(catalog, "recipe_negroni"),
            false,
            new RecipeGarnishExpectation { garnish = BartendingGarnishId.OrangeSlice, placement = BartendingGarnishPlacement.Rim });

        ValidateActionSequence(FindRecipe(catalog, "recipe_manhattan"),
            RecipePreparationAction.Pour,
            RecipePreparationAction.Pour,
            RecipePreparationAction.Dash,
            RecipePreparationAction.AddIce,
            RecipePreparationAction.Stir,
            RecipePreparationAction.Strain,
            RecipePreparationAction.Garnish);
        ValidateGarnish(
            FindRecipe(catalog, "recipe_manhattan"),
            false,
            new RecipeGarnishExpectation { garnish = BartendingGarnishId.Cherry, placement = BartendingGarnishPlacement.InGlass });

        ValidateActionSequence(FindRecipe(catalog, "recipe_gin_fizz"),
            RecipePreparationAction.Pour,
            RecipePreparationAction.Pour,
            RecipePreparationAction.Pour,
            RecipePreparationAction.AddIce,
            RecipePreparationAction.Shake,
            RecipePreparationAction.PourFromContainer,
            RecipePreparationAction.Splash,
            RecipePreparationAction.Garnish);
        ValidateGarnish(
            FindRecipe(catalog, "recipe_gin_fizz"),
                true,
            new RecipeGarnishExpectation { garnish = BartendingGarnishId.LemonSlice, placement = BartendingGarnishPlacement.Rim },
            new RecipeGarnishExpectation { garnish = BartendingGarnishId.LemonTwist, placement = BartendingGarnishPlacement.Rim });
    }

    private static void ValidateOriginalPreparationContracts(RecipeCatalogData catalog)
    {
        CocktailRecipeData seolya = FindRecipe(catalog, "recipe_test_stir");
        CocktailRecipeData manwol = FindRecipe(catalog, "recipe_manwol");

        ValidateIngredient(seolya, 0, "whiskey", 2f);
        ValidateIngredient(seolya, 1, "vermouth", 1f);
        ValidateIngredient(seolya, 2, "water", 1f);
        ValidateActionSequence(seolya,
            RecipePreparationAction.Pour,
            RecipePreparationAction.Pour,
            RecipePreparationAction.Pour,
            RecipePreparationAction.AddIce,
            RecipePreparationAction.Stir,
            RecipePreparationAction.Strain);
        ValidateStep(seolya, 0, RecipePreparationAction.Pour, seolya.ingredients[0].ingredient,
            RecipePreparationContainer.Any, RecipePreparationContainer.MixingGlass);
        ValidateStep(seolya, 1, RecipePreparationAction.Pour, seolya.ingredients[1].ingredient,
            RecipePreparationContainer.Any, RecipePreparationContainer.MixingGlass);
        ValidateStep(seolya, 2, RecipePreparationAction.Pour, seolya.ingredients[2].ingredient,
            RecipePreparationContainer.Any, RecipePreparationContainer.MixingGlass);
        ValidateStep(seolya, 3, RecipePreparationAction.AddIce, null,
            RecipePreparationContainer.Any, RecipePreparationContainer.MixingGlass);
        ValidateStep(seolya, 4, RecipePreparationAction.Stir, null,
            RecipePreparationContainer.Any, RecipePreparationContainer.MixingGlass);
        ValidateStep(seolya, 5, RecipePreparationAction.Strain, null,
            RecipePreparationContainer.MixingGlass, RecipePreparationContainer.ServingGlass);
        RequireNoGarnish(seolya);
        Require(seolya.targetCocktail.glassware == CocktailGlassware.OldFashionedGlass,
            "Seolya must use the approved Old-fashioned glass metadata.");

        ValidateIngredient(manwol, 0, "vodka", 2f);
        ValidateIngredient(manwol, 1, "coffee_liqueur", 1f);
        ValidateIngredient(manwol, 2, "cream", 1f);
        ValidateActionSequence(manwol,
            RecipePreparationAction.Pour,
            RecipePreparationAction.Pour,
            RecipePreparationAction.Pour);
        ValidateStep(manwol, 0, RecipePreparationAction.Pour, manwol.ingredients[0].ingredient,
            RecipePreparationContainer.Any, RecipePreparationContainer.ServingGlass);
        ValidateStep(manwol, 1, RecipePreparationAction.Pour, manwol.ingredients[1].ingredient,
            RecipePreparationContainer.Any, RecipePreparationContainer.ServingGlass);
        ValidateStep(manwol, 2, RecipePreparationAction.Pour, manwol.ingredients[2].ingredient,
            RecipePreparationContainer.Any, RecipePreparationContainer.ServingGlass);
        RequireNoGarnish(manwol);
        Require(manwol.method == MiniGameType.Float &&
                manwol.targetCocktail.glassware == CocktailGlassware.OldFashionedGlass,
            "Manwol must retain Float routing and use the approved Old-fashioned glass metadata.");
        ValidateManwolLayerOrder(manwol);
    }

    private static void ValidateDemoAvailability(
        IngredientAvailabilityData availability,
        IReadOnlyList<IngredientData> shelfIngredients,
        IReadOnlyDictionary<string, IngredientData> ingredients,
        RecipeCatalogData catalog)
    {
        Require(availability.Entries.Count == shelfIngredients.Count && shelfIngredients.Count == 26,
            "All configured shelf ingredients must retain one explicit availability entry.");

        for (int day = 1; day <= ExpectedAvailableIngredientIdsByDay.Length; day++)
        {
            HashSet<string> actual = availability.GetAvailableIngredients(
                    shelfIngredients,
                    new IngredientAvailabilityContext(day))
                .Select(ingredient => ingredient.ingredientId)
                .ToHashSet();
            HashSet<string> expected = ExpectedAvailableIngredientIdsByDay[day - 1].ToHashSet();
            Require(actual.SetEquals(expected),
                "Day " + day + " ordinary supply does not match the approved selected-demo curation.");
        }

        foreach (string reserveOnlyId in new[] { "london_dry_gin", "fernet", "champagne" })
        {
            IngredientData reserveIngredient = FindIngredient(ingredients, reserveOnlyId);
            for (int day = 1; day <= 4; day++)
            {
                Require(!availability.IsIngredientAvailable(reserveIngredient, new IngredientAvailabilityContext(day)),
                    reserveOnlyId + " leaked into ordinary Day " + day + " demo supply.");
            }
        }

        IngredientData unlisted = ScriptableObject.CreateInstance<IngredientData>();
        try
        {
            unlisted.ingredientId = "j2_unlisted_probe";
            Require(!availability.IsIngredientAvailable(unlisted, new IngredientAvailabilityContext(4)),
                "An unlisted ingredient leaked into ordinary demo supply through the compatibility fallback.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unlisted);
        }

        CocktailRecipeData hankyPanky = FindRecipe(catalog, "recipe_hanky_panky");
        IngredientAvailabilityContext requestContext = IngredientAvailabilityContext.ForRequest(1, hankyPanky);
        Require(RecipeConstructibilityService.IsLiquidConstructible(
                    hankyPanky,
                    availability.GetAvailableIngredients(shelfIngredients, requestContext)),
            "Request-scoped availability no longer supports a reserve exact request.");
    }

    private static void ValidateIdentityBoundaries(IReadOnlyDictionary<string, IngredientData> ingredients)
    {
        RequireDistinct(ingredients, "gin", "london_dry_gin");
        RequireDistinct(ingredients, "dry_vermouth", "sweet_red_vermouth", "vermouth");
        RequireDistinct(ingredients, "whiskey", "rye_whiskey");
        RequireDistinct(ingredients, "syrup", "simple_syrup");
        RequireDistinct(ingredients, "lemon", "fresh_lemon_juice");
        RequireDistinct(ingredients, "lime", "fresh_lime_juice");
    }

    private static void ValidateCatalogAndCompatibility(
        RecipeCatalogData catalog,
        IngredientAvailabilityData availability,
        IReadOnlyList<IngredientData> shelfIngredients)
    {
        foreach (string path in ReserveRecipePaths.Concat(CompatibilityRecipePaths))
        {
            CocktailRecipeData recipe = LoadRequired<CocktailRecipeData>(path);
            Require(recipe.targetCocktail != null &&
                    catalog.Entries.Any(entry => entry != null && entry.Recipe == recipe),
                "Reserve or compatibility recipe was removed from the authoritative catalog: " + path);
        }

        List<IngredientData> dayOne = availability.GetAvailableIngredients(
            shelfIngredients,
            new IngredientAvailabilityContext(1));
        foreach (string recipeId in new[] { "recipe_test_stir", "recipe_test_shake", "recipe_janseol", "recipe_manwol" })
        {
            Require(RecipeConstructibilityService.IsLiquidConstructible(FindRecipe(catalog, recipeId), dayOne),
                "Active original/compatibility recipe is no longer constructible on Day 1: " + recipeId);
        }
    }

    private static void ValidateDryMartiniAndManwol(
        RecipeCatalogData catalog,
        IngredientAvailabilityData availability,
        IReadOnlyDictionary<string, IngredientData> ingredients)
    {
        CocktailRecipeData dryMartini = FindRecipe(catalog, "recipe_dry_martini");
        foreach (RecipeIngredientEntry entry in dryMartini.ingredients)
        {
            Require(availability.IsIngredientAvailable(entry.ingredient, new IngredientAvailabilityContext(1)),
                "Dry Martini ingredient is not available on Day 1: " + entry.ingredient.ingredientId);
        }
        Require(dryMartini.targetCocktail.emotionTags.Length == 1 &&
                dryMartini.targetCocktail.emotionTags[0] == EmotionTag.Clarity,
            "Dry Martini must retain its Day-1 Clarity reference role.");

        CocktailRecipeData manwol = FindRecipe(catalog, "recipe_manwol");
        OrderData firstOrder = LoadRequired<OrderData>(LittlePrinceFirstOrderPath);
        OrderData followupOrder = LoadRequired<OrderData>(LittlePrinceFollowupOrderPath);
        Require(firstOrder.RequestType == GuestRequestType.ExactCocktail &&
                followupOrder.RequestType == GuestRequestType.ExactCocktail &&
                firstOrder.requestedCocktail == manwol.targetCocktail &&
                followupOrder.requestedCocktail == manwol.targetCocktail &&
                firstOrder.miniGameType == MiniGameType.Float && followupOrder.miniGameType == MiniGameType.Float,
            "Little Prince requests must remain exact Manwol Float routes.");

        IngredientAvailabilityContext ordinaryDayTwo = new(2);
        IngredientAvailabilityContext requestDayTwo = IngredientAvailabilityContext.ForRequest(2, manwol);
        foreach (RecipeIngredientEntry entry in manwol.ingredients)
        {
            Require(availability.IsIngredientAvailable(entry.ingredient, ordinaryDayTwo) &&
                    availability.IsIngredientAvailable(entry.ingredient, requestDayTwo),
                "Manwol ingredient is unavailable to the Day-2 Little Prince route: " + entry.ingredient.ingredientId);
        }

        Require(ingredients["vodka"].ingredientId != ingredients["gin"].ingredientId,
            "Manwol identity was conflated with Gin-based selected-demo recipes.");
    }

    private static void ValidateArtAssignments()
    {
        CocktailData seolya = LoadRequired<CocktailData>(SelectedCocktails[0].CocktailPath);
        CocktailData manwol = LoadRequired<CocktailData>(SelectedCocktails[1].CocktailPath);
        Require(seolya.cocktailSprite != null && manwol.cocktailSprite != null,
            "Seolya and Manwol must retain their existing assigned presentation art.");

        foreach (SelectedCocktailSpec spec in SelectedCocktails.Skip(2))
        {
            CocktailData cocktail = LoadRequired<CocktailData>(spec.CocktailPath);
            Require(cocktail.cocktailSprite == null,
                spec.Label + " must not be presented as if missing final art were complete.");
        }
    }

    private static void ValidatePlayerFacingDescription(string description, string label)
    {
        Require(!string.IsNullOrWhiteSpace(description), label + " has no player-facing description.");
        foreach (string disallowedPhrase in new[]
                 {
                     "official iba", "validation", "gameplay tuning", "test", "prototype"
                 })
        {
            Require(description.IndexOf(disallowedPhrase, StringComparison.OrdinalIgnoreCase) < 0,
                label + " still exposes development prose: " + disallowedPhrase);
        }
    }

    private static void ValidateActionSequence(CocktailRecipeData recipe, params RecipePreparationAction[] expected)
    {
        Require(recipe.expectedPreparation != null && recipe.expectedPreparation.Length == expected.Length,
            recipe.recipeId + " preparation step count drifted from the approved I9B baseline.");

        for (int index = 0; index < expected.Length; index++)
        {
            Require(recipe.expectedPreparation[index] != null && recipe.expectedPreparation[index].action == expected[index],
                recipe.recipeId + " preparation action " + index + " drifted from the approved I9B baseline.");
        }
    }

    private static void ValidateGarnish(
        CocktailRecipeData recipe,
        bool optional,
        params RecipeGarnishExpectation[] expected)
    {
        RecipePreparationStep garnishStep = recipe.expectedPreparation
            .FirstOrDefault(step => step != null && step.action == RecipePreparationAction.Garnish);
        Require(garnishStep != null && garnishStep.optional == optional &&
                garnishStep.acceptedGarnishes != null &&
                garnishStep.acceptedGarnishes.Length == expected.Length,
            recipe.recipeId + " garnish metadata drifted from the approved I9B baseline.");

        for (int index = 0; index < expected.Length; index++)
        {
            RecipeGarnishExpectation actual = garnishStep.acceptedGarnishes[index];
            Require(actual != null && actual.garnish == expected[index].garnish &&
                    actual.placement == expected[index].placement,
                recipe.recipeId + " garnish expectation " + index + " drifted from the approved I9B baseline.");
        }
    }

    private static void ValidateIngredient(
        CocktailRecipeData recipe,
        int index,
        string ingredientId,
        float ratio)
    {
        Require(recipe.ingredients != null && recipe.ingredients.Length > index &&
                recipe.ingredients[index] != null && recipe.ingredients[index].ingredient != null &&
                recipe.ingredients[index].ingredient.ingredientId == ingredientId &&
                Mathf.Approximately(recipe.ingredients[index].ratio, ratio),
            recipe.recipeId + " ingredient " + index + " drifted from the approved original-drink contract.");
    }

    private static void ValidateStep(
        CocktailRecipeData recipe,
        int index,
        RecipePreparationAction action,
        IngredientData ingredient,
        RecipePreparationContainer source,
        RecipePreparationContainer target)
    {
        Require(recipe.expectedPreparation != null && recipe.expectedPreparation.Length > index,
            recipe.recipeId + " is missing preparation step " + index + ".");

        RecipePreparationStep step = recipe.expectedPreparation[index];
        Require(step != null && step.action == action && step.ingredient == ingredient &&
                step.repeatCount == 1 && step.source == source && step.target == target && !step.optional,
            recipe.recipeId + " preparation step " + index + " does not match the approved contract.");
    }

    private static void RequireNoGarnish(CocktailRecipeData recipe)
    {
        Require(recipe.expectedPreparation != null &&
                !recipe.expectedPreparation.Any(step => step != null && step.action == RecipePreparationAction.Garnish),
            recipe.recipeId + " must not require or author a garnish.");
    }

    private static void ValidateManwolLayerOrder(CocktailRecipeData manwol)
    {
        BartendingLayeredDrinkState layers = new();
        for (int index = 0; index < manwol.ingredients.Length; index++)
        {
            IngredientData ingredient = manwol.ingredients[index].ingredient;
            Require(layers.TryBeginLayer(ingredient),
                "Manwol Float could not begin authored layer " + index + ".");
            Require(Mathf.Approximately(layers.AddToActiveLayer(1f), 1f) &&
                    layers.TryCompleteActiveLayer(
                        index == 0 ? (BartendingLayerBoundaryQuality?)null : BartendingLayerBoundaryQuality.Clean,
                        out BartendingLayer completed) &&
                    completed.OrderIndex == index && completed.Ingredient == ingredient,
                "Manwol Float did not preserve authored layer " + index + ".");
        }

        Require(layers.Layers.Count == 3 &&
                layers.Layers[0].Ingredient == manwol.ingredients[0].ingredient &&
                layers.Layers[1].Ingredient == manwol.ingredients[1].ingredient &&
                layers.Layers[2].Ingredient == manwol.ingredients[2].ingredient &&
                layers.Layers[2].Ingredient.ingredientId == "cream",
            "Manwol Cream must remain the final upper layer in the existing Float layer authority.");
    }

    private static Dictionary<string, IngredientData> BuildIngredientMap(IReadOnlyList<IngredientData> ingredients)
    {
        Dictionary<string, IngredientData> result = new(StringComparer.Ordinal);
        foreach (IngredientData ingredient in ingredients)
        {
            Require(ingredient != null && !string.IsNullOrWhiteSpace(ingredient.ingredientId),
                "Shelf contains an ingredient with no stable ID.");
            Require(result.TryAdd(ingredient.ingredientId, ingredient),
                "Shelf contains duplicate ingredient ID: " + ingredient.ingredientId);
        }

        return result;
    }

    private static IngredientData FindIngredient(IReadOnlyDictionary<string, IngredientData> ingredients, string ingredientId)
    {
        Require(ingredients.TryGetValue(ingredientId, out IngredientData ingredient),
            "Missing ingredient: " + ingredientId);
        return ingredient;
    }

    private static void RequireDistinct(
        IReadOnlyDictionary<string, IngredientData> ingredients,
        params string[] ingredientIds)
    {
        HashSet<IngredientData> values = new();
        foreach (string ingredientId in ingredientIds)
            Require(values.Add(FindIngredient(ingredients, ingredientId)),
                "Distinct ingredient IDs were merged: " + string.Join(", ", ingredientIds));
    }

    private static CocktailRecipeData FindRecipe(RecipeCatalogData catalog, string recipeId)
    {
        CocktailRecipeData recipe = catalog.Entries
            .Select(entry => entry != null ? entry.Recipe : null)
            .FirstOrDefault(entry => entry != null && entry.recipeId == recipeId);
        Require(recipe != null, "RecipeCatalogData is missing recipe: " + recipeId);
        return recipe;
    }

    private static bool TagSequenceEquals(EmotionTag[] actual, EmotionTag[] expected)
    {
        return actual != null && actual.SequenceEqual(expected);
    }

    private static T LoadRequired<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Require(asset != null, "Missing required asset: " + path);
        return asset;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("[J2 Validation] " + message);
    }

    private sealed class SelectedCocktailSpec
    {
        public string Label { get; }
        public string CocktailPath { get; }
        public string RecipePath { get; }
        public string CocktailId { get; }
        public string RecipeId { get; }
        public bool UnlockedByDefault { get; }
        public CocktailGlassware Glassware { get; }
        public MiniGameType Method { get; }
        public EmotionTag[] EmotionTags { get; }
        public bool IsIba => CocktailId != "cocktail_seolya" && CocktailId != "cocktail_manwol";

        public SelectedCocktailSpec(
            string label,
            string cocktailPath,
            string recipePath,
            string cocktailId,
            string recipeId,
            bool unlockedByDefault,
            CocktailGlassware glassware,
            MiniGameType method,
            EmotionTag[] emotionTags)
        {
            Label = label;
            CocktailPath = cocktailPath;
            RecipePath = recipePath;
            CocktailId = cocktailId;
            RecipeId = recipeId;
            UnlockedByDefault = unlockedByDefault;
            Glassware = glassware;
            Method = method;
            EmotionTags = emotionTags;
        }
    }
}
#endif
