/// <summary>
/// PlayerPrefs-backed storage for the Little Prince first-visit compatibility value.
/// First-visit eligibility and departure timing remain owned by CustomerSpawner.
/// </summary>
public static class LittlePrinceVisitPersistence
{
    private const string FirstVisitCompletedKey = "littlePrinceFirstVisitCompleted";

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
}
