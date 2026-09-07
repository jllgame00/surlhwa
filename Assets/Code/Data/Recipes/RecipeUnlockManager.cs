using System.Collections.Generic;
using UnityEngine;

public class RecipeUnlockManager : MonoBehaviour
{
    [Header("Recipe Catalog")]
    [SerializeField] private RecipeCatalogData recipeCatalog;

    private readonly HashSet<string> unlockedRecipeIds = new();

    private void Awake()
    {
        LoadUnlocks();
        InitializeDefaultUnlocks();
    }

    private void InitializeDefaultUnlocks()
    {
        if (recipeCatalog == null) return;

        foreach (RecipeCatalogEntry entry in recipeCatalog.Entries)
        {
            if (entry == null) continue;

            CocktailRecipeData recipe = entry.Recipe;
            if (recipe == null) continue;

            if (entry.UnlockedByDefault)
                unlockedRecipeIds.Add(recipe.recipeId);
        }

        SaveUnlocks();
    }

    public bool IsUnlocked(CocktailRecipeData recipe)
    {
        if (recipe == null) return false;
        if (string.IsNullOrEmpty(recipe.recipeId)) return false;

        return unlockedRecipeIds.Contains(recipe.recipeId);
    }

    public bool UnlockRecipe(CocktailRecipeData recipe)
    {
        if (recipe == null) return false;
        if (string.IsNullOrEmpty(recipe.recipeId))
        {
            Debug.LogWarning("[RecipeUnlockManager] Cannot unlock recipe with empty recipeId");
            return false;
        }

        bool added = unlockedRecipeIds.Add(recipe.recipeId);

        if (added)
        {
            SaveUnlocks();
            Debug.Log($"[RecipeUnlockManager] Saved unlocked recipe -> {recipe.recipeId}");
        }

        return added;
    }

    public List<CocktailRecipeData> GetUnlockedRecipesByMethod(MiniGameType method)
    {
        List<CocktailRecipeData> result = new();

        if (recipeCatalog == null) return result;

        foreach (RecipeCatalogEntry entry in recipeCatalog.Entries)
        {
            if (entry == null) continue;

            CocktailRecipeData recipe = entry.Recipe;
            if (recipe == null) continue;
            if (recipe.method != method) continue;
            if (!IsUnlocked(recipe)) continue;

            result.Add(recipe);
        }

        return result;
    }

    public List<CocktailRecipeData> GetAllRecipes()
    {
        List<CocktailRecipeData> result = new();

        if (recipeCatalog == null) return result;

        foreach (RecipeCatalogEntry entry in recipeCatalog.Entries)
        {
            if (entry == null) continue;

            CocktailRecipeData recipe = entry.Recipe;
            if (recipe == null) continue;
            result.Add(recipe);
        }

        return result;
    }

    public List<CocktailRecipeData> GetAllRecipesByMethod(MiniGameType method)
    {
        List<CocktailRecipeData> result = new();

        if (recipeCatalog == null) return result;

        foreach (RecipeCatalogEntry entry in recipeCatalog.Entries)
        {
            if (entry == null) continue;

            CocktailRecipeData recipe = entry.Recipe;
            if (recipe == null) continue;
            if (recipe.method != method) continue;

            result.Add(recipe);
        }

        return result;
    }

    public CocktailRecipeData GetRecipeForCocktail(CocktailData cocktail, MiniGameType method)
    {
        return recipeCatalog != null
            ? recipeCatalog.FindRecipeForCocktail(cocktail, method)
            : null;
    }

    public bool HasShownFirstRecipeReveal()
    {
        return RecipeProgressPersistence.HasShownFirstRecipeReveal();
    }

    public bool TryMarkFirstRecipeRevealShown()
    {
        return RecipeProgressPersistence.TryMarkFirstRecipeRevealShown();
    }

    private void SaveUnlocks()
    {
        string data = string.Join("|", unlockedRecipeIds);
        RecipeProgressPersistence.SaveUnlockedRecipeIdsRaw(data);

        Debug.Log($"[RecipeUnlockManager] SaveUnlocks -> {data}");
    }

    private void LoadUnlocks()
    {
        unlockedRecipeIds.Clear();

        string data = RecipeProgressPersistence.LoadUnlockedRecipeIdsRaw();

        if (string.IsNullOrEmpty(data))
        {
            Debug.Log("[RecipeUnlockManager] No saved unlock data");
            return;
        }

        string[] ids = data.Split('|');

        foreach (string id in ids)
        {
            if (string.IsNullOrEmpty(id)) continue;
            unlockedRecipeIds.Add(id);
        }

        Debug.Log($"[RecipeUnlockManager] LoadUnlocks -> {data}");
    }

    [ContextMenu("Debug/Clear Saved Unlocks")]
    public void ClearSavedUnlocks()
    {
        RecipeProgressPersistence.ClearRecipeProgress();

        unlockedRecipeIds.Clear();
        InitializeDefaultUnlocks();

        Debug.Log("[RecipeUnlockManager] Cleared saved unlocks");
    }
}
