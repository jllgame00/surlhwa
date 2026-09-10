using System.Collections.Generic;

/// <summary>
/// Immutable runtime snapshot used by a Bartending RecipeGuide.
/// It contains display data only and no Bar Scene object references.
/// </summary>
public sealed class RecipeGuideContext
{
    private readonly IReadOnlyList<CocktailRecipeData> availableRecipes;
    private readonly Dictionary<CocktailRecipeData, RecipeKnowledgeFacts> knowledgeByRecipe;

    public MiniGameType Method { get; }
    public IReadOnlyList<CocktailRecipeData> AvailableRecipes => availableRecipes;
    public bool IsForcedFirstCustomerRouteActive { get; }

    public RecipeGuideContext(
        MiniGameType method,
        IEnumerable<CocktailRecipeData> availableRecipes,
        System.Func<CocktailRecipeData, RecipeKnowledgeFacts> knowledgeProvider,
        bool isForcedFirstCustomerRouteActive)
    {
        Method = method;
        IsForcedFirstCustomerRouteActive = isForcedFirstCustomerRouteActive;

        List<CocktailRecipeData> snapshot = new();
        knowledgeByRecipe = new Dictionary<CocktailRecipeData, RecipeKnowledgeFacts>();
        if (availableRecipes != null)
        {
            foreach (CocktailRecipeData recipe in availableRecipes)
            {
                if (recipe != null && !knowledgeByRecipe.ContainsKey(recipe))
                {
                    snapshot.Add(recipe);
                    knowledgeByRecipe.Add(
                        recipe,
                        knowledgeProvider != null
                            ? knowledgeProvider.Invoke(recipe)
                            : null);
                }
            }
        }

        this.availableRecipes = snapshot.AsReadOnly();
    }

    /// <summary>
    /// The Guide consumes this immutable knowledge snapshot. It never reads
    /// authored recipe details to infer a fact that the player has not learned.
    /// A new bartending session recreates the context after knowledge changes.
    /// </summary>
    public RecipeKnowledgeFacts GetKnownFacts(CocktailRecipeData recipe)
    {
        if (recipe != null &&
            knowledgeByRecipe.TryGetValue(recipe, out RecipeKnowledgeFacts facts) &&
            facts != null)
        {
            return facts;
        }

        return new RecipeKnowledgeFacts(false, false, false, false, false, null, null);
    }
}
