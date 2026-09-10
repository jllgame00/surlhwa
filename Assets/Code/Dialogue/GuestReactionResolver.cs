using System.Linq;

public static class GuestReactionResolver
{
    /// <summary>
    /// Presentation-facing result composed from the already-evaluated guest
    /// fit and mechanical execution. It does not match recipes or inspect
    /// preparation state.
    /// </summary>
    public readonly struct GuestResponseOutcome
    {
        public int Hearts { get; }
        public GuestResponseTone Tone { get; }
        public bool HasGuestFitFeedback { get; }

        public GuestResponseOutcome(int hearts, GuestResponseTone tone, bool hasGuestFitFeedback)
        {
            Hearts = hearts;
            Tone = tone;
            HasGuestFitFeedback = hasGuestFitFeedback;
        }
    }

    public enum GuestResponseTone
    {
        NotApplicable,
        GoodFitGoodExecution,
        GoodFitWeakExecution,
        PoorFitGoodExecution,
        PoorFitWeakExecution
    }

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

    public static GuestResponseOutcome EvaluateOutcome(
        RequestEvaluationResult guestFit,
        bool executionSucceeded,
        EmotionTag[] desiredTags,
        EmotionTag[] servedTags,
        bool hasRecognizedCocktail)
    {
        // An unknown composition has no authored CocktailData emotional meaning.
        // Preserve its graceful generic reaction rather than interpreting raw ingredients.
        if (!hasRecognizedCocktail || !guestFit.IsApplicable)
        {
            return new GuestResponseOutcome(
                CalculateHearts(desiredTags, servedTags, executionSucceeded),
                GuestResponseTone.NotApplicable,
                false);
        }

        if (guestFit.Status == RequestEvaluationStatus.Satisfied)
        {
            return executionSucceeded
                ? new GuestResponseOutcome(3, GuestResponseTone.GoodFitGoodExecution, true)
                : new GuestResponseOutcome(2, GuestResponseTone.GoodFitWeakExecution, true);
        }

        return executionSucceeded
            ? new GuestResponseOutcome(1, GuestResponseTone.PoorFitGoodExecution, true)
            : new GuestResponseOutcome(0, GuestResponseTone.PoorFitWeakExecution, true);
    }
}
