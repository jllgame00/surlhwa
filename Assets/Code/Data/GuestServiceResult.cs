using System.Collections.Generic;

/// <summary>
/// Result handoff from OrderManager to guest-facing presentation. It carries
/// separate references/results for WHAT was recognized, HOW/QUALITY from the
/// bartending result, FIT from RequestEvaluation, and the final reward delta.
/// It is transient and never a second gameplay authority or persistence model.
/// </summary>
public readonly struct GuestServiceResult
{
    public CocktailData MatchedCocktail { get; }
    public MiniGameResult BartendingResult { get; }
    public RequestEvaluationResult GuestFit { get; }
    public GuestReactionResolver.GuestResponseOutcome GuestResponse { get; }
    public int ReputationDelta { get; }
    public int MechanicalReputationDelta { get; }
    public int RequestReputationReward { get; }
    public bool RequestRewardWithheld { get; }
    public bool CanRevealMatchedCocktail { get; }
    public bool NewlyKnownRecipe { get; }

    public GuestServiceResult(
        CocktailData matchedCocktail,
        MiniGameResult bartendingResult,
        RequestEvaluationResult guestFit,
        GuestReactionResolver.GuestResponseOutcome guestResponse,
        int reputationDelta,
        int mechanicalReputationDelta,
        int requestReputationReward,
        bool requestRewardWithheld,
        bool canRevealMatchedCocktail,
        bool newlyKnownRecipe)
    {
        MatchedCocktail = matchedCocktail;
        BartendingResult = bartendingResult;
        GuestFit = guestFit;
        GuestResponse = guestResponse;
        ReputationDelta = reputationDelta;
        MechanicalReputationDelta = mechanicalReputationDelta;
        RequestReputationReward = requestReputationReward;
        RequestRewardWithheld = requestRewardWithheld;
        CanRevealMatchedCocktail = canRevealMatchedCocktail;
        NewlyKnownRecipe = newlyKnownRecipe;
    }
}

/// <summary>
/// Read-only text projection of an already-resolved service result. This is
/// presentation formatting only: it never evaluates fit, recipe matching,
/// preparation, technique, discovery, or rewards.
/// </summary>
public readonly struct ServiceFeedbackPresentation
{
    public string Title { get; }
    public string Message { get; }

    public ServiceFeedbackPresentation(string title, string message)
    {
        Title = title ?? string.Empty;
        Message = message ?? string.Empty;
    }
}

public static class GuestServiceFeedbackFormatter
{
    private const string UnknownCocktailLabel = "Unknown Cocktail";

    public static ServiceFeedbackPresentation Build(GuestServiceResult result)
    {
        List<string> lines = new();
        lines.Add("Drink: " + GetDrinkLabel(result));

        RecipePreparationEvaluationResult preparation = result.BartendingResult.preparationEvaluation;
        if (preparation != null &&
            preparation.Status == RecipePreparationEvaluationStatus.Unsatisfied)
        {
            lines.Add("Preparation: Order differed from the recipe.");
        }

        if (!string.IsNullOrWhiteSpace(result.BartendingResult.grade))
            lines.Add("Technique: " + result.BartendingResult.grade);

        lines.Add(BuildRewardLine(result));

        return new ServiceFeedbackPresentation(
            GetTitle(result.GuestResponse),
            string.Join("\n", lines));
    }

    private static string GetDrinkLabel(GuestServiceResult result)
    {
        if (!result.CanRevealMatchedCocktail || result.MatchedCocktail == null)
            return UnknownCocktailLabel;

        return string.IsNullOrWhiteSpace(result.MatchedCocktail.cocktailName)
            ? UnknownCocktailLabel
            : result.MatchedCocktail.cocktailName;
    }

    private static string GetTitle(GuestReactionResolver.GuestResponseOutcome response)
    {
        if (!response.HasGuestFitFeedback)
            return "Service Result";

        return response.Tone switch
        {
            GuestReactionResolver.GuestResponseTone.GoodFitGoodExecution =>
                "Guest Fit: Good",
            GuestReactionResolver.GuestResponseTone.GoodFitWeakExecution =>
                "Guest Fit: Good - Execution Needs Work",
            GuestReactionResolver.GuestResponseTone.PoorFitGoodExecution =>
                "Guest Fit: Poor - Drink Well Made",
            GuestReactionResolver.GuestResponseTone.PoorFitWeakExecution =>
                "Guest Fit: Poor",
            _ => "Service Result"
        };
    }

    private static string BuildRewardLine(GuestServiceResult result)
    {
        string total = FormatSigned(result.ReputationDelta);

        if (result.RequestReputationReward != 0)
        {
            return "Reputation: " + total +
                   " (service " + FormatSigned(result.MechanicalReputationDelta) +
                   ", request " + FormatSigned(result.RequestReputationReward) + ")";
        }

        if (result.RequestRewardWithheld)
            return "Reputation: " + total + " (request reward withheld)";

        return "Reputation: " + total;
    }

    private static string FormatSigned(int value)
    {
        return value > 0 ? "+" + value : value.ToString();
    }
}
