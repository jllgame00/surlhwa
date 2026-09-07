/// <summary>
/// PlayerPrefs-backed storage for recipe progression compatibility values.
/// Recipe unlock rules and catalog defaults remain owned by RecipeUnlockManager.
/// </summary>
public static class RecipeProgressPersistence
{
    private const string UnlockedRecipesKey = "SURLHWA_UNLOCKED_RECIPES";
    private const string FirstRecipeRevealShownKey = "SURLHWA_FIRST_RECIPE_REVEAL_SHOWN";

    public static string LoadUnlockedRecipeIdsRaw()
    {
        return SurlhwaPlayerPrefsStore.GetString(UnlockedRecipesKey, "");
    }

    public static void SaveUnlockedRecipeIdsRaw(string serializedRecipeIds)
    {
        SurlhwaPlayerPrefsStore.SetString(UnlockedRecipesKey, serializedRecipeIds);
        SurlhwaPlayerPrefsStore.Save();
    }

    public static bool HasShownFirstRecipeReveal()
    {
        return SurlhwaPlayerPrefsStore.GetInt(FirstRecipeRevealShownKey, 0) == 1;
    }

    public static bool TryMarkFirstRecipeRevealShown()
    {
        if (HasShownFirstRecipeReveal())
            return false;

        SurlhwaPlayerPrefsStore.SetInt(FirstRecipeRevealShownKey, 1);
        SurlhwaPlayerPrefsStore.Save();
        return true;
    }

    public static void ClearRecipeProgress()
    {
        SurlhwaPlayerPrefsStore.DeleteKey(UnlockedRecipesKey);
        SurlhwaPlayerPrefsStore.DeleteKey(FirstRecipeRevealShownKey);
        SurlhwaPlayerPrefsStore.Save();
    }
}
