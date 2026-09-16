#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bounded I10 data, shelf, and constructibility proof. It is deliberately an
/// editor-only validation command: it does not write PlayerPrefs or alter game
/// progression while checking the day-derived runtime policy.
/// </summary>
public static class SURLHWAIngredientAvailabilityValidation
{
    private const string AvailabilityPath =
        "Assets/_SURLHWA/Data/Ingredients/IngredientAvailability.asset";
    private const string CatalogPath = "Assets/_SURLHWA/Data/Recipes/RecipeCatalog.asset";
    private const string ShelfPath = "Assets/_SURLHWA/Prefabs/UI/IngredientShelfPanel.prefab";

    [MenuItem("SURLHWA/Validation/Run I10 Ingredient Availability")]
    public static void Run()
    {
        IngredientAvailabilityData availability =
            AssetDatabase.LoadAssetAtPath<IngredientAvailabilityData>(AvailabilityPath);
        RecipeCatalogData catalog = AssetDatabase.LoadAssetAtPath<RecipeCatalogData>(CatalogPath);

        Require(availability != null, "Ingredient availability asset is missing.");
        Require(catalog != null, "Recipe catalog asset is missing.");

        GameObject shelfRoot = PrefabUtility.LoadPrefabContents(ShelfPath);
        try
        {
            IngredientShelfController shelf = shelfRoot.GetComponentInChildren<IngredientShelfController>(true);
            Require(shelf != null, "Ingredient shelf controller is missing from its shared prefab.");
            Require(shelf.AvailabilityData == availability,
                "Shared shelf prefab does not reference IngredientAvailability.");
            Require(shelf.ConfiguredIngredients.Count == 26,
                $"Expected 26 configured shelf ingredients, found {shelf.ConfiguredIngredients.Count}.");
            Require(availability.Entries.Count == shelf.ConfiguredIngredients.Count,
                "Availability asset must explicitly classify every configured shelf ingredient.");

            ValidateTier(shelf, availability, catalog, 1, 13,
                new[] { "recipe_dry_martini" },
                new[] { "recipe_gin_fizz", "recipe_french_75", "recipe_hanky_panky", "recipe_last_word", "recipe_manhattan", "recipe_negroni" });
            ValidateTier(shelf, availability, catalog, 2, 15,
                new[] { "recipe_dry_martini", "recipe_negroni" },
                new[] { "recipe_gin_fizz", "recipe_french_75", "recipe_hanky_panky", "recipe_last_word", "recipe_manhattan" });
            ValidateTier(shelf, availability, catalog, 3, 17,
                new[] { "recipe_dry_martini", "recipe_negroni", "recipe_manhattan" },
                new[] { "recipe_gin_fizz", "recipe_french_75", "recipe_hanky_panky", "recipe_last_word" });
            ValidateTier(shelf, availability, catalog, 4, 20,
                new[] { "recipe_dry_martini", "recipe_negroni", "recipe_manhattan", "recipe_gin_fizz" },
                new[] { "recipe_french_75", "recipe_hanky_panky", "recipe_last_word" });
            ValidateTier(shelf, availability, catalog, 5, 26,
                new[] { "recipe_dry_martini", "recipe_negroni", "recipe_manhattan", "recipe_hanky_panky", "recipe_gin_fizz", "recipe_french_75", "recipe_last_word" },
                Array.Empty<string>());

            ValidateSelectionReset(shelf, availability);
            ValidatePrototypeAndSpecialSafety(availability, catalog, shelf.ConfiguredIngredients);
            ValidateRecipeKnowledgeIndependence(availability, catalog, shelf.ConfiguredIngredients);

            Debug.Log("[I10 Validation] PASS: availability is day-derived, shelf-filtered, and recipe-unlock independent.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(shelfRoot);
        }
    }

    private static void ValidateTier(
        IngredientShelfController shelf,
        IngredientAvailabilityData availability,
        RecipeCatalogData catalog,
        int day,
        int expectedVisibleCount,
        IEnumerable<string> expectedRepresentativeRecipeIds,
        IEnumerable<string> expectedUnavailableRepresentativeRecipeIds)
    {
        IngredientAvailabilityContext context = new(day);
        shelf.Initialize(MiniGameType.Build, BartendingContainerType.ServingGlass, context);

        List<IngredientData> available = availability.GetAvailableIngredients(
            shelf.ConfiguredIngredients,
            context);
        Require(available.Count == expectedVisibleCount,
            $"Day {day}: expected {expectedVisibleCount} available ingredients, found {available.Count}.");
        Require(shelf.VisibleIngredientCount == expectedVisibleCount,
            $"Day {day}: shelf showed {shelf.VisibleIngredientCount} entries instead of {expectedVisibleCount}.");

        HashSet<string> constructibleIds = RecipeConstructibilityService
            .GetLiquidConstructibleRecipes(catalog, available)
            .Select(recipe => recipe.recipeId)
            .ToHashSet();

        foreach (string recipeId in expectedRepresentativeRecipeIds)
        {
            Require(constructibleIds.Contains(recipeId),
                $"Day {day}: expected representative recipe '{recipeId}' to be constructible.");
        }

        foreach (string recipeId in expectedUnavailableRepresentativeRecipeIds)
        {
            Require(!constructibleIds.Contains(recipeId),
                $"Day {day}: representative recipe '{recipeId}' became constructible too early.");
        }

        Debug.Log(
            $"[I10 Validation] Day {day}: available={available.Count}, shelf={shelf.VisibleIngredientCount}, " +
            $"representative={string.Join(", ", expectedRepresentativeRecipeIds)}");
    }

    private static void ValidateSelectionReset(
        IngredientShelfController shelf,
        IngredientAvailabilityData availability)
    {
        IngredientData greenChartreuse = FindIngredient(shelf.ConfiguredIngredients, "green_chartreuse");
        shelf.Initialize(
            MiniGameType.Build,
            BartendingContainerType.ServingGlass,
            new IngredientAvailabilityContext(5));
        Require(shelf.TrySelectIngredient(greenChartreuse), "Could not select an available Day 5 ingredient.");

        shelf.SetAvailabilityContext(new IngredientAvailabilityContext(1));
        Require(shelf.SelectedIngredient == null,
            "Shelf retained a selection after the ingredient became unavailable.");
        Require(!shelf.IsIngredientVisible(greenChartreuse),
            "Shelf retained an unavailable ingredient after refresh.");
        Require(!availability.IsIngredientAvailable(greenChartreuse, new IngredientAvailabilityContext(1)),
            "Day 1 policy unexpectedly makes Green Chartreuse available.");
    }

    private static void ValidatePrototypeAndSpecialSafety(
        IngredientAvailabilityData availability,
        RecipeCatalogData catalog,
        IReadOnlyList<IngredientData> shelfIngredients)
    {
        IngredientAvailabilityContext dayOne = new(1);
        List<IngredientData> dayOneIngredients = availability.GetAvailableIngredients(shelfIngredients, dayOne);

        foreach (string recipeId in new[]
                 {
                     "recipe_janseol", "recipe_test_stir", "recipe_test_shake", "recipe_manwol"
                 })
        {
            CocktailRecipeData recipe = FindRecipe(catalog, recipeId);
            Require(RecipeConstructibilityService.IsLiquidConstructible(recipe, dayOneIngredients),
                $"Legacy/prototype recipe '{recipeId}' is not constructible on Day 1.");
        }

        CocktailRecipeData manwol = FindRecipe(catalog, "recipe_manwol");
        foreach (RecipeIngredientEntry requirement in manwol.ingredients)
        {
            IngredientAvailabilityEntry entry = availability.FindEntry(requirement.ingredient);
            Require(entry != null && entry.Policy == IngredientAvailabilityPolicy.AlwaysAvailable,
                "Manwol must remain supported by legacy/test availability for the Little Prince route.");
        }

        CocktailRecipeData hankyPanky = FindRecipe(catalog, "recipe_hanky_panky");
        Require(!RecipeConstructibilityService.IsLiquidConstructible(hankyPanky, dayOneIngredients),
            "Hanky Panky should not be normally constructible on Day 1.");
        IngredientAvailabilityContext temporaryRequest =
            IngredientAvailabilityContext.ForRequest(1, hankyPanky);
        List<IngredientData> requestIngredients = availability.GetAvailableIngredients(
            shelfIngredients,
            temporaryRequest);
        Require(RecipeConstructibilityService.IsLiquidConstructible(hankyPanky, requestIngredients),
            "A request-local override did not provide its required liquid ingredients.");
        Require(!RecipeConstructibilityService.IsLiquidConstructible(hankyPanky, dayOneIngredients),
            "Request-local availability leaked into normal availability.");
    }

    private static void ValidateRecipeKnowledgeIndependence(
        IngredientAvailabilityData availability,
        RecipeCatalogData catalog,
        IReadOnlyList<IngredientData> shelfIngredients)
    {
        CocktailRecipeData dryMartini = FindRecipe(catalog, "recipe_dry_martini");
        List<IngredientData> dayOneIngredients = availability.GetAvailableIngredients(
            shelfIngredients,
            new IngredientAvailabilityContext(1));

        Require(!catalog.IsUnlockedByDefault(dryMartini),
            "Dry Martini must start recipe-locked for the I10 independence proof.");
        Require(RecipeConstructibilityService.IsLiquidConstructible(dryMartini, dayOneIngredients),
            "A recipe-locked Dry Martini must still be physically constructible on Day 1.");
    }

    private static IngredientData FindIngredient(IEnumerable<IngredientData> ingredients, string ingredientId)
    {
        IngredientData ingredient = ingredients.FirstOrDefault(item =>
            item != null && item.ingredientId == ingredientId);
        Require(ingredient != null, $"Ingredient '{ingredientId}' is missing.");
        return ingredient;
    }

    private static CocktailRecipeData FindRecipe(RecipeCatalogData catalog, string recipeId)
    {
        CocktailRecipeData recipe = catalog.Entries
            .Select(entry => entry != null ? entry.Recipe : null)
            .FirstOrDefault(item => item != null && item.recipeId == recipeId);
        Require(recipe != null, $"Recipe '{recipeId}' is missing from RecipeCatalogData.");
        return recipe;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("[I10 Validation] " + message);
    }
}
#endif
