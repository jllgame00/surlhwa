using System;

/// <summary>
/// Immutable, session-scoped guest request semantics. It deliberately separates
/// what a guest wants from the OrderData template that selects a bartending
/// method. Authored exact orders project their existing data here; generic
/// guests project their generated emotion request without mutating an asset.
/// </summary>
public sealed class GuestRequestContext
{
    public GuestRequestType RequestType { get; }
    public CocktailData RequestedCocktail { get; }
    public EmotionTag[] DesiredEmotionTags { get; }
    public int ReputationReward { get; }
    public string DisplayTitle { get; }

    private GuestRequestContext(
        GuestRequestType requestType,
        CocktailData requestedCocktail,
        EmotionTag[] desiredEmotionTags,
        int reputationReward,
        string displayTitle)
    {
        RequestType = requestType;
        RequestedCocktail = requestedCocktail;
        DesiredEmotionTags = desiredEmotionTags ?? Array.Empty<EmotionTag>();
        ReputationReward = reputationReward;
        DisplayTitle = displayTitle ?? string.Empty;
    }

    public static GuestRequestContext FromOrder(OrderData order)
    {
        if (order == null)
            return null;

        EmotionTag[] sourceTags = order.DesiredEmotionTags;
        EmotionTag[] copiedTags = sourceTags.Length > 0
            ? (EmotionTag[])sourceTags.Clone()
            : Array.Empty<EmotionTag>();

        return new GuestRequestContext(
            order.RequestType,
            order.requestedCocktail,
            copiedTags,
            order.reputationReward,
            order.orderName);
    }

    public static GuestRequestContext CreateRecommendation(
        EmotionTag[] desiredEmotionTags,
        string displayTitle,
        int reputationReward)
    {
        EmotionTag[] copiedTags = desiredEmotionTags != null && desiredEmotionTags.Length > 0
            ? (EmotionTag[])desiredEmotionTags.Clone()
            : Array.Empty<EmotionTag>();

        return new GuestRequestContext(
            GuestRequestType.Recommendation,
            null,
            copiedTags,
            reputationReward,
            displayTitle);
    }
}
