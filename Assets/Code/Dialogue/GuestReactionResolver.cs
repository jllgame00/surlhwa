using System.Linq;

public static class GuestReactionResolver
{
    public static int CalculateHearts(
        EmotionTag[] desiredTags,
        EmotionTag[] servedTags,
        bool recipeInTolerance)
    {
        desiredTags ??= System.Array.Empty<EmotionTag>();
        servedTags ??= System.Array.Empty<EmotionTag>();

        int tagMatchCount = desiredTags.Count(tag => servedTags.Contains(tag));

        int score = 0;

        if (recipeInTolerance)
            score += 1;

        score += tagMatchCount;

        return score switch
        {
            <= 0 => 0,
            1 => 1,
            2 => 2,
            _ => 3
        };
    }
}