/// <summary>
/// Minimal Bar-to-Bartending execution contract.
/// It intentionally contains only Bartending input data and no Bar Scene object references.
/// </summary>
using System;

public sealed class BartendingSession
{
    public BartendingRequestContext Request { get; }
    public RecipeGuideContext RecipeGuideContext { get; }
    public Action<string, string> FirstRecipeRevealRequested { get; }

    public BartendingSession(
        BartendingRequestContext request,
        RecipeGuideContext recipeGuideContext = null,
        Action<string, string> firstRecipeRevealRequested = null)
    {
        Request = request;
        RecipeGuideContext = recipeGuideContext;
        FirstRecipeRevealRequested = firstRecipeRevealRequested;
    }
}
