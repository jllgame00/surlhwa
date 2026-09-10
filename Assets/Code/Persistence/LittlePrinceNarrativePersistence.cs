/// <summary>
/// Stable, bounded narrative memory for the Little Prince vertical slice.
/// This is intentionally separate from visit completion, recipe knowledge,
/// guest fit, and all gameplay progression state.
/// </summary>
public enum LittlePrinceNarrativeChoice
{
    None = 0,
    KeepTheLight = 1,
    SeekTheStar = 2
}

public static class LittlePrinceNarrativePersistence
{
    private const string NarrativeChoiceKey =
        "SURLHWA_LITTLE_PRINCE_NARRATIVE_CHOICE";

    public static bool HasSavedChoice()
    {
        return SurlhwaPlayerPrefsStore.HasKey(NarrativeChoiceKey);
    }

    public static LittlePrinceNarrativeChoice LoadChoice()
    {
        int rawValue = SurlhwaPlayerPrefsStore.GetInt(
            NarrativeChoiceKey,
            (int)LittlePrinceNarrativeChoice.None);

        return rawValue == (int)LittlePrinceNarrativeChoice.KeepTheLight
            ? LittlePrinceNarrativeChoice.KeepTheLight
            : rawValue == (int)LittlePrinceNarrativeChoice.SeekTheStar
                ? LittlePrinceNarrativeChoice.SeekTheStar
                : LittlePrinceNarrativeChoice.None;
    }

    /// <summary>
    /// Commits the first deliberate non-None choice. Repeating the same choice
    /// is idempotent; a conflicting later selection cannot overwrite it.
    /// </summary>
    public static bool TryCommitChoice(LittlePrinceNarrativeChoice choice)
    {
        if (choice != LittlePrinceNarrativeChoice.KeepTheLight &&
            choice != LittlePrinceNarrativeChoice.SeekTheStar)
            return false;

        LittlePrinceNarrativeChoice existing = LoadChoice();
        if (existing != LittlePrinceNarrativeChoice.None)
            return existing == choice;

        SurlhwaPlayerPrefsStore.SetInt(NarrativeChoiceKey, (int)choice);
        SurlhwaPlayerPrefsStore.Save();
        return true;
    }

    public static void ClearChoice()
    {
        SurlhwaPlayerPrefsStore.DeleteKey(NarrativeChoiceKey);
        SurlhwaPlayerPrefsStore.Save();
    }
}
