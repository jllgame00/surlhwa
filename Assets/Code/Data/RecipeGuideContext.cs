using System.Collections.Generic;

/// <summary>
/// Immutable runtime snapshot used by a Bartending RecipeGuide.
/// It contains display data only and no Bar Scene object references.
/// </summary>
public sealed class RecipeGuideContext
{
    private readonly IReadOnlyList<CocktailRecipeData> availableRecipes;

    public MiniGameType Method { get; }
    public IReadOnlyList<CocktailRecipeData> AvailableRecipes => availableRecipes;
    public bool IsForcedFirstCustomerRouteActive { get; }

    public RecipeGuideContext(
        MiniGameType method,
        IEnumerable<CocktailRecipeData> availableRecipes,
        bool isForcedFirstCustomerRouteActive)
    {
        Method = method;
        IsForcedFirstCustomerRouteActive = isForcedFirstCustomerRouteActive;

        List<CocktailRecipeData> snapshot = new();
        if (availableRecipes != null)
        {
            foreach (CocktailRecipeData recipe in availableRecipes)
            {
                if (recipe != null)
                    snapshot.Add(recipe);
            }
        }

        this.availableRecipes = snapshot.AsReadOnly();
    }
}
