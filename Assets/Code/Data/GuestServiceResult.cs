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
    private const string UnknownCocktailLabel = "이름을 모르는 칵테일";

    public static ServiceFeedbackPresentation Build(GuestServiceResult result)
    {
        List<string> lines = new();
        lines.Add("내어준 잔: " + GetDrinkLabel(result));

        RecipePreparationEvaluationResult preparation = result.BartendingResult.preparationEvaluation;
        if (preparation != null &&
            preparation.Status == RecipePreparationEvaluationStatus.Unsatisfied)
        {
            lines.Add("준비 순서가 레시피와 달랐습니다.");
        }

        if (!string.IsNullOrWhiteSpace(result.BartendingResult.grade))
            lines.Add("조주: " + TechniqueLabel(result.BartendingResult.grade));

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

    public static string TechniqueLabel(string grade) => grade switch {
        "Good" => "안정적", "Acceptable" => "기본", "Poor" => "미흡", "Complete" => "마무리", _ => grade };

    private static string GetTitle(GuestReactionResolver.GuestResponseOutcome response)
    {
        if (!response.HasGuestFitFeedback)
            return "잔을 건넨 뒤";

        return response.Tone switch
        {
            GuestReactionResolver.GuestResponseTone.GoodFitGoodExecution =>
                "마음에 닿은 잔",
            GuestReactionResolver.GuestResponseTone.GoodFitWeakExecution =>
                "어울리는 잔 · 조주는 조금 아쉬웠어요",
            GuestReactionResolver.GuestResponseTone.PoorFitGoodExecution =>
                "잘 만들었지만 다른 마음의 잔",
            GuestReactionResolver.GuestResponseTone.PoorFitWeakExecution =>
                "마음과 조금 다른 잔",
            _ => "잔을 건넨 뒤"
        };
    }

    private static string BuildRewardLine(GuestServiceResult result)
    {
        string total = FormatSigned(result.ReputationDelta);

        if (result.RequestReputationReward != 0)
        {
            return "평판: " + total +
                   " (조주 " + FormatSigned(result.MechanicalReputationDelta) +
                   ", 요청 " + FormatSigned(result.RequestReputationReward) + ")";
        }

        if (result.RequestRewardWithheld)
            return "평판: " + total + " (요청 보상 없음)";

        return "평판: " + total;
    }

    private static string FormatSigned(int value)
    {
        return value > 0 ? "+" + value : value.ToString();
    }
}
