/// <summary>
/// Transient request data required to execute a Bartending session.
/// The Bar projects this from its active OrderData at the scene boundary.
/// </summary>
public sealed class BartendingRequestContext
{
    public MiniGameType Route { get; }
    public string DisplayTitle { get; }
    public CocktailRecipeData ExpectedRecipe { get; }

    public BartendingRequestContext(
        MiniGameType route,
        string displayTitle,
        CocktailRecipeData expectedRecipe = null)
    {
        Route = route;
        DisplayTitle = displayTitle ?? string.Empty;
        ExpectedRecipe = expectedRecipe;
    }
}
