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

    public GuestServiceResult(
        CocktailData matchedCocktail,
        MiniGameResult bartendingResult,
        RequestEvaluationResult guestFit,
        GuestReactionResolver.GuestResponseOutcome guestResponse,
        int reputationDelta)
    {
        MatchedCocktail = matchedCocktail;
        BartendingResult = bartendingResult;
        GuestFit = guestFit;
        GuestResponse = guestResponse;
        ReputationDelta = reputationDelta;
    }
}
