using System;

/// <summary>
/// Additive PlayerPrefs integers only. No replacement recipe/save format.
/// Checkpoints commit on departure, never on dialogue entry or drink selection.
/// </summary>
public static class CampaignProgressPersistence
{
    private const string Prefix = "SURLHWA_CAMPAIGN_";
    public const string DayKey = Prefix + "CHECKPOINT_DAY";
    public const string SlotKey = Prefix + "CHECKPOINT_SLOT";
    public const string EndingKey = Prefix + "ENDING_READ";
    public static string VisitKey(CampaignVisitId visit) => Prefix + "VISIT_" + (int)visit;

    public static bool IsCompleted(CampaignVisitId visit) => visit != CampaignVisitId.None &&
        SurlhwaPlayerPrefsStore.GetInt(VisitKey(visit), 0) == 1;
    public static bool EndingRead => SurlhwaPlayerPrefsStore.GetInt(EndingKey, 0) == 1;
    public static bool HasProgress => SurlhwaPlayerPrefsStore.HasKey(DayKey) ||
        SurlhwaPlayerPrefsStore.HasKey(EndingKey);

    public static int LoadSlot(int day, int count)
    {
        if (SurlhwaPlayerPrefsStore.GetInt(DayKey, 0) != day) return 0;
        return Math.Clamp(SurlhwaPlayerPrefsStore.GetInt(SlotKey, 0), 0, count);
    }

    public static void RecordDeparture(int day, int nextSlot, CampaignVisitId visit)
    {
        if (day < CampaignData.FirstExtendedNight || nextSlot < 1) return;
        if (visit != CampaignVisitId.None)
            SurlhwaPlayerPrefsStore.SetInt(VisitKey(visit), 1);
        SurlhwaPlayerPrefsStore.SetInt(DayKey, day);
        SurlhwaPlayerPrefsStore.SetInt(SlotKey, nextSlot);
        SurlhwaPlayerPrefsStore.Save();
    }

    public static void MarkEndingRead()
    {
        SurlhwaPlayerPrefsStore.SetInt(EndingKey, 1);
        SurlhwaPlayerPrefsStore.Save();
    }

    public static void Clear()
    {
        SurlhwaPlayerPrefsStore.DeleteKey(DayKey);
        SurlhwaPlayerPrefsStore.DeleteKey(SlotKey);
        SurlhwaPlayerPrefsStore.DeleteKey(EndingKey);
        foreach (CampaignVisitId visit in Enum.GetValues(typeof(CampaignVisitId)))
            if (visit != CampaignVisitId.None) SurlhwaPlayerPrefsStore.DeleteKey(VisitKey(visit));
    }
}
