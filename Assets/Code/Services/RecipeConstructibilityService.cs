using System.Collections.Generic;

/// <summary>
/// Pure physical-availability query. It deliberately ignores recipe unlock
/// state, Guide visibility, preparation quality, and garnish presentation.
/// </summary>
public static class RecipeConstructibilityService
{
    public static bool IsLiquidConstructible(
        CocktailRecipeData recipe,
        IEnumerable<IngredientData> availableIngredients)
    {
        if (recipe == null)
            return false;

        HashSet<IngredientData> available = new();
        if (availableIngredients != null)
        {
            foreach (IngredientData ingredient in availableIngredients)
            {
                if (ingredient != null)
                    available.Add(ingredient);
            }
        }

        if (recipe.ingredients == null)
            return true;

        foreach (RecipeIngredientEntry requirement in recipe.ingredients)
        {
            // RecipeIngredientEntry is the current liquid recipe surface.
            // Garnish expectations live in expectedPreparation and therefore
            // cannot block this physical liquid-constructibility query.
            if (requirement == null || requirement.ingredient == null ||
                !available.Contains(requirement.ingredient))
            {
                return false;
            }
        }

        return true;
    }

    public static List<CocktailRecipeData> GetLiquidConstructibleRecipes(
        RecipeCatalogData catalog,
        IEnumerable<IngredientData> availableIngredients)
    {
        List<CocktailRecipeData> constructible = new();
        if (catalog == null)
            return constructible;

        foreach (RecipeCatalogEntry entry in catalog.Entries)
        {
            CocktailRecipeData recipe = entry != null ? entry.Recipe : null;
            if (recipe != null && IsLiquidConstructible(recipe, availableIngredients))
                constructible.Add(recipe);
        }

        return constructible;
    }
}
