/// <summary>
/// Minimal Bar-to-Bartending execution contract.
/// It intentionally contains only Bartending input data and no Bar Scene object references.
/// </summary>
using System;

public sealed class BartendingSession
{
    public OrderData Order { get; }
    public MiniGameType Method { get; }
    public RecipeGuideContext RecipeGuideContext { get; }
    public Action<string, string> FirstRecipeRevealRequested { get; }

    public BartendingSession(
        OrderData order,
        MiniGameType method,
        RecipeGuideContext recipeGuideContext = null,
        Action<string, string> firstRecipeRevealRequested = null)
    {
        Order = order;
        Method = method;
        RecipeGuideContext = recipeGuideContext;
        FirstRecipeRevealRequested = firstRecipeRevealRequested;
    }

    public static BartendingSession FromOrder(
        OrderData order,
        RecipeGuideContext recipeGuideContext = null,
        Action<string, string> firstRecipeRevealRequested = null)
    {
        return new BartendingSession(
            order,
            order != null ? order.miniGameType : MiniGameType.Stir,
            recipeGuideContext,
            firstRecipeRevealRequested);
    }
}
