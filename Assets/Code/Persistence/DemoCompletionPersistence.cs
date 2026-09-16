/// <summary>
/// Bounded lifecycle state for the configured portfolio demo. This is not a
/// narrative flag and does not replace the owners of business, recipe, or
/// Little Prince progression.
/// </summary>
public static class DemoCompletionPersistence
{
    private const string DemoCompleteKey = "SURLHWA_DEMO_COMPLETE_V1";

    public static bool HasSavedDemoCompletionState()
    {
        return SurlhwaPlayerPrefsStore.HasKey(DemoCompleteKey);
    }

    public static bool IsDemoComplete()
    {
        return SurlhwaPlayerPrefsStore.GetInt(DemoCompleteKey, 0) != 0;
    }

    public static void MarkDemoComplete()
    {
        SurlhwaPlayerPrefsStore.SetInt(DemoCompleteKey, 1);
        SurlhwaPlayerPrefsStore.Save();
    }

    public static void ClearDemoCompletion()
    {
        SurlhwaPlayerPrefsStore.DeleteKey(DemoCompleteKey);
        SurlhwaPlayerPrefsStore.Save();
    }
}
