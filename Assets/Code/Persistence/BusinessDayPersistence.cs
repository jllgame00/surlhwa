/// <summary>
/// PlayerPrefs-backed storage for the current business day compatibility value.
/// Day defaults, validation, and progression rules remain owned by BusinessDayManager.
/// </summary>
public static class BusinessDayPersistence
{
    private const string CurrentDayKey = "currentDay";

    public static bool HasSavedCurrentDay()
    {
        return SurlhwaPlayerPrefsStore.HasKey(CurrentDayKey);
    }

    public static int LoadCurrentDay(int defaultDay)
    {
        return SurlhwaPlayerPrefsStore.GetInt(CurrentDayKey, defaultDay);
    }

    public static void SaveCurrentDay(int day)
    {
        SurlhwaPlayerPrefsStore.SetInt(CurrentDayKey, day);
        SurlhwaPlayerPrefsStore.Save();
    }

    public static void ClearCurrentDay()
    {
        SurlhwaPlayerPrefsStore.DeleteKey(CurrentDayKey);
        SurlhwaPlayerPrefsStore.Save();
    }
}
