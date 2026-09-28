/// <summary>
/// Coordinates only the production New Game lifecycle. Domain persistence
/// remains owned by the existing business, recipe, and narrative helpers.
/// </summary>
public static class SurlhwaGameProgress
{
    public static bool HasMeaningfulProgress()
    {
        return BusinessDayPersistence.HasSavedCurrentDay() ||
               RecipeProgressPersistence.HasRecipeKnowledge() ||
               RecipeProgressPersistence.HasLegacyUnlockedRecipeIds() ||
               RecipeProgressPersistence.HasShownFirstRecipeReveal() ||
               LittlePrinceVisitPersistence.HasSavedFirstVisitState() ||
               LittlePrinceVisitPersistence.HasSavedFollowupVisitState() ||
               LittlePrinceVisitPersistence.HasSavedClosureState() ||
               LittlePrinceNarrativePersistence.HasSavedChoice() ||
               DemoCompletionPersistence.HasSavedDemoCompletionState() ||
               CampaignProgressPersistence.HasProgress;
    }

    /// <summary>
    /// Resets only SURLHWA gameplay progression. It intentionally leaves
    /// unrelated PlayerPrefs, such as future settings/preferences, untouched.
    /// </summary>
    public static void ResetGameProgress()
    {
        BusinessDayPersistence.ClearCurrentDay();
        RecipeProgressPersistence.ClearRecipeProgress();
        LittlePrinceVisitPersistence.ClearFirstVisitCompleted();
        LittlePrinceVisitPersistence.ClearFollowupVisitCompleted();
        LittlePrinceVisitPersistence.ClearClosureProgress();
        LittlePrinceNarrativePersistence.ClearChoice();
        DemoCompletionPersistence.ClearDemoCompletion();
        CampaignProgressPersistence.Clear();
        SurlhwaPlayerPrefsStore.Save();
    }
}
