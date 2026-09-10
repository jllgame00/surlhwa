using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PlayerPrefs-backed storage for recipe progression. The legacy binary unlock
/// list remains readable as a migration source; current knowledge is stored as
/// a versioned, bounded set of recipe and ingredient IDs.
/// </summary>
public static class RecipeProgressPersistence
{
    private const string UnlockedRecipesKey = "SURLHWA_UNLOCKED_RECIPES";
    private const string FirstRecipeRevealShownKey = "SURLHWA_FIRST_RECIPE_REVEAL_SHOWN";
    private const string RecipeKnowledgeKey = "SURLHWA_RECIPE_KNOWLEDGE_V1";
    private const int RecipeKnowledgeVersion = 1;

    public static string LoadUnlockedRecipeIdsRaw()
    {
        return SurlhwaPlayerPrefsStore.GetString(UnlockedRecipesKey, "");
    }

    public static void SaveUnlockedRecipeIdsRaw(string serializedRecipeIds)
    {
        SurlhwaPlayerPrefsStore.SetString(UnlockedRecipesKey, serializedRecipeIds);
        SurlhwaPlayerPrefsStore.Save();
    }

    /// <summary>
    /// Reads the old pipe-delimited key only for I11A migration. New recipe
    /// knowledge writes deliberately do not maintain a second binary authority.
    /// </summary>
    public static string LoadLegacyUnlockedRecipeIdsRaw()
    {
        return LoadUnlockedRecipeIdsRaw();
    }

    public static bool HasLegacyUnlockedRecipeIds()
    {
        return SurlhwaPlayerPrefsStore.HasKey(UnlockedRecipesKey);
    }

    public static void ClearLegacyUnlockedRecipeIds()
    {
        SurlhwaPlayerPrefsStore.DeleteKey(UnlockedRecipesKey);
        SurlhwaPlayerPrefsStore.Save();
    }

    public static bool HasRecipeKnowledge()
    {
        return SurlhwaPlayerPrefsStore.HasKey(RecipeKnowledgeKey);
    }

    public static string LoadRecipeKnowledgeRaw()
    {
        return SurlhwaPlayerPrefsStore.GetString(RecipeKnowledgeKey, string.Empty);
    }

    public static void SaveRecipeKnowledgeRaw(string serializedKnowledge)
    {
        SurlhwaPlayerPrefsStore.SetString(RecipeKnowledgeKey, serializedKnowledge ?? string.Empty);
        SurlhwaPlayerPrefsStore.Save();
    }

    public static void ClearRecipeKnowledge()
    {
        SurlhwaPlayerPrefsStore.DeleteKey(RecipeKnowledgeKey);
        SurlhwaPlayerPrefsStore.Save();
    }

    public static RecipeKnowledgeSaveData LoadRecipeKnowledge()
    {
        string raw = SurlhwaPlayerPrefsStore.GetString(RecipeKnowledgeKey, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
            return RecipeKnowledgeSaveData.CreateEmpty();

        try
        {
            RecipeKnowledgeSaveData data = JsonUtility.FromJson<RecipeKnowledgeSaveData>(raw);
            if (data == null || data.version != RecipeKnowledgeVersion)
            {
                Debug.LogWarning(
                    "[RecipeKnowledge] Ignoring unsupported recipe-knowledge save data. " +
                    "Legacy recipe unlock data will remain available for migration.");
                return RecipeKnowledgeSaveData.CreateEmpty();
            }

            data.entries ??= new List<RecipeKnowledgeSaveEntry>();
            return data;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[RecipeKnowledge] Could not read recipe-knowledge save data; " +
                "legacy recipe unlock data will remain available for migration. " +
                exception.Message);
            return RecipeKnowledgeSaveData.CreateEmpty();
        }
    }

    public static void SaveRecipeKnowledge(RecipeKnowledgeSaveData data)
    {
        RecipeKnowledgeSaveData safeData = data ?? RecipeKnowledgeSaveData.CreateEmpty();
        safeData.version = RecipeKnowledgeVersion;
        safeData.entries ??= new List<RecipeKnowledgeSaveEntry>();

        SurlhwaPlayerPrefsStore.SetString(RecipeKnowledgeKey, JsonUtility.ToJson(safeData));
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
        SurlhwaPlayerPrefsStore.DeleteKey(RecipeKnowledgeKey);
        SurlhwaPlayerPrefsStore.DeleteKey(FirstRecipeRevealShownKey);
        SurlhwaPlayerPrefsStore.Save();
    }
}

/// <summary>
/// Serialized with JsonUtility as SURLHWA_RECIPE_KNOWLEDGE_V1. Fields are
/// intentionally stable IDs and bounded booleans only; authored recipe data is
/// never copied into player progress.
/// </summary>
[Serializable]
public sealed class RecipeKnowledgeSaveData
{
    public int version;
    public List<RecipeKnowledgeSaveEntry> entries = new();

    public static RecipeKnowledgeSaveData CreateEmpty()
    {
        return new RecipeKnowledgeSaveData
        {
            version = 1,
            entries = new List<RecipeKnowledgeSaveEntry>()
        };
    }
}

[Serializable]
public sealed class RecipeKnowledgeSaveEntry
{
    public string recipeId;
    public bool known;
    public bool identityKnown;
    public bool methodKnown;
    public bool garnishKnown;
    public bool preparationKnown;
    public List<string> knownIngredientIds = new();
    public List<string> knownIngredientAmountIds = new();
}
