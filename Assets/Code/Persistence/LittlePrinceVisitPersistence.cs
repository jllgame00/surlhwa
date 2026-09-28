/// <summary>
/// PlayerPrefs-backed storage for Little Prince visit scheduling. Narrative
/// choice is deliberately owned by LittlePrinceNarrativePersistence instead.
/// Departure timing remains owned by CustomerSpawner.
/// </summary>
public static class LittlePrinceVisitPersistence
{
    private const string FirstVisitCompletedKey = "littlePrinceFirstVisitCompleted";
    private const string FollowupVisitCompletedKey =
        "SURLHWA_LITTLE_PRINCE_FOLLOWUP_VISIT_COMPLETED";

    private const string ClosingVisitCompletedKey = "SURLHWA_LITTLE_PRINCE_CLOSING_VISIT_COMPLETED";
    private const string OutcomePrefix = "SURLHWA_LITTLE_PRINCE_SERVICE_OUTCOME_";

    // Additive keys: absent legacy outcomes are Unknown, never invented successes.
    public static int GetServiceOutcome(int visit)
    {
        if (visit < 1 || visit > 3) return 0;
        int value = SurlhwaPlayerPrefsStore.GetInt(OutcomePrefix + visit, 0);
        return value == 1 || value == 2 ? value : 0;
    }

    public static bool IsSuccessfulService(GuestServiceResult result)
    {
        return result.MatchedCocktail != null &&
               result.GuestFit.Status == RequestEvaluationStatus.Satisfied &&
               result.BartendingResult.success &&
               result.BartendingResult.preparationEvaluation != null &&
               result.BartendingResult.preparationEvaluation.Status ==
                   RecipePreparationEvaluationStatus.Satisfied;
    }

    public static void RecordServiceOutcome(int visit, bool succeeded)
    {
        if (visit < 1 || visit > 3 ||
            (visit == 1 && IsFirstVisitCompleted()) ||
            (visit == 2 && IsFollowupVisitCompleted()) ||
            (visit == 3 && IsClosingVisitCompleted())) return;

        // A failed served drink cannot be repaired by reloading before departure.
        if (GetServiceOutcome(visit) == 2) return;
        SurlhwaPlayerPrefsStore.SetInt(OutcomePrefix + visit, succeeded ? 1 : 2);
        SurlhwaPlayerPrefsStore.Save();
    }

    public static bool HasOpenFarewell() => IsFirstVisitCompleted() &&
        IsFollowupVisitCompleted() && GetServiceOutcome(1) == 1 &&
        GetServiceOutcome(2) == 1 && GetServiceOutcome(3) == 1;

    // Future statue presentation may consume this; no immediate reward is granted.
    public static bool IsStatueEligible() => IsClosingVisitCompleted() && HasOpenFarewell();

    public static bool IsClosingVisitCompleted() =>
        SurlhwaPlayerPrefsStore.GetInt(ClosingVisitCompletedKey, 0) == 1;

    public static void MarkClosingVisitCompleted()
    {
        SurlhwaPlayerPrefsStore.SetInt(ClosingVisitCompletedKey, 1);
        SurlhwaPlayerPrefsStore.Save();
    }

    public static bool HasSavedClosureState() =>
        SurlhwaPlayerPrefsStore.HasKey(ClosingVisitCompletedKey) ||
        SurlhwaPlayerPrefsStore.HasKey(OutcomePrefix + 1) ||
        SurlhwaPlayerPrefsStore.HasKey(OutcomePrefix + 2) ||
        SurlhwaPlayerPrefsStore.HasKey(OutcomePrefix + 3);

    public static void ClearClosureProgress()
    {
        SurlhwaPlayerPrefsStore.DeleteKey(ClosingVisitCompletedKey);
        for (int visit = 1; visit <= 3; visit++)
            SurlhwaPlayerPrefsStore.DeleteKey(OutcomePrefix + visit);
        SurlhwaPlayerPrefsStore.Save();
    }

    public static bool HasSavedFirstVisitState()
    {
        return SurlhwaPlayerPrefsStore.HasKey(FirstVisitCompletedKey);
    }

    public static bool IsFirstVisitCompleted()
    {
        return SurlhwaPlayerPrefsStore.GetInt(FirstVisitCompletedKey, 0) != 0;
    }

    public static void MarkFirstVisitCompleted()
    {
        SetFirstVisitCompleted(true);
    }

    public static void SetFirstVisitCompleted(bool completed)
    {
        SurlhwaPlayerPrefsStore.SetInt(FirstVisitCompletedKey, completed ? 1 : 0);
        SurlhwaPlayerPrefsStore.Save();
    }

    public static void ClearFirstVisitCompleted()
    {
        SurlhwaPlayerPrefsStore.DeleteKey(FirstVisitCompletedKey);
        SurlhwaPlayerPrefsStore.Save();
    }

    public static bool HasSavedFollowupVisitState()
    {
        return SurlhwaPlayerPrefsStore.HasKey(FollowupVisitCompletedKey);
    }

    public static bool IsFollowupVisitCompleted()
    {
        return SurlhwaPlayerPrefsStore.GetInt(FollowupVisitCompletedKey, 0) != 0;
    }

    public static void MarkFollowupVisitCompleted()
    {
        SetFollowupVisitCompleted(true);
    }

    public static void SetFollowupVisitCompleted(bool completed)
    {
        SurlhwaPlayerPrefsStore.SetInt(FollowupVisitCompletedKey, completed ? 1 : 0);
        SurlhwaPlayerPrefsStore.Save();
    }

    public static void ClearFollowupVisitCompleted()
    {
        SurlhwaPlayerPrefsStore.DeleteKey(FollowupVisitCompletedKey);
        SurlhwaPlayerPrefsStore.Save();
    }
}
