using System;
using System.Collections.Generic;

/// <summary>
/// Transient evaluation input for ingredient availability. It is intentionally
/// not saved: normal availability is derived from the current business day.
/// </summary>
public sealed class IngredientAvailabilityContext
{
    private readonly HashSet<IngredientData> temporaryIngredients;

    public int CurrentBusinessDay { get; }
    public IReadOnlyCollection<IngredientData> TemporaryIngredients => temporaryIngredients;

    public IngredientAvailabilityContext(
        int currentBusinessDay,
        IEnumerable<IngredientData> temporaryIngredients = null)
    {
        CurrentBusinessDay = Math.Max(1, currentBusinessDay);
        this.temporaryIngredients = new HashSet<IngredientData>();

        if (temporaryIngredients == null)
            return;

        foreach (IngredientData ingredient in temporaryIngredients)
        {
            if (ingredient != null)
                this.temporaryIngredients.Add(ingredient);
        }
    }

    public bool IsTemporarilyAvailable(IngredientData ingredient)
    {
        return ingredient != null && temporaryIngredients.Contains(ingredient);
    }

    public static IngredientAvailabilityContext ForRequest(
        int currentBusinessDay,
        CocktailRecipeData requestedRecipe)
    {
        List<IngredientData> requiredLiquidIngredients = new();

        if (requestedRecipe != null && requestedRecipe.ingredients != null)
        {
            foreach (RecipeIngredientEntry entry in requestedRecipe.ingredients)
            {
                if (entry != null && entry.ingredient != null)
                    requiredLiquidIngredients.Add(entry.ingredient);
            }
        }

        return new IngredientAvailabilityContext(currentBusinessDay, requiredLiquidIngredients);
    }
}
