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
