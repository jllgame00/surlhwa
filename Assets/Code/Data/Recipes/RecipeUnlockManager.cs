using System.Collections.Generic;
using UnityEngine;

public class RecipeUnlockManager : MonoBehaviour
{
    [Header("All Recipe Data")]
    [SerializeField] private CocktailRecipeData[] allRecipes;

    private readonly HashSet<string> unlockedRecipeIds = new();

    private void Awake()
    {
        InitializeDefaultUnlocks();
    }

    private void InitializeDefaultUnlocks()
    {
        unlockedRecipeIds.Clear();

        if (allRecipes == null) return;

        foreach (var recipe in allRecipes)
        {
            if (recipe == null) continue;

            if (recipe.unlockedByDefault)
                unlockedRecipeIds.Add(recipe.recipeId);
        }
    }

    public bool IsUnlocked(CocktailRecipeData recipe)
    {
        if (recipe == null) return false;
        return unlockedRecipeIds.Contains(recipe.recipeId);
    }

    public bool UnlockRecipe(CocktailRecipeData recipe)
    {
        if (recipe == null) return false;
        return unlockedRecipeIds.Add(recipe.recipeId);
    }

    public List<CocktailRecipeData> GetUnlockedRecipesByMethod(MiniGameType method)
    {
        List<CocktailRecipeData> result = new();

        if (allRecipes == null) return result;

        foreach (var recipe in allRecipes)
        {
            if (recipe == null) continue;
            if (recipe.method != method) continue;
            if (!unlockedRecipeIds.Contains(recipe.recipeId)) continue;

            result.Add(recipe);
        }

        return result;
    }

    public List<CocktailRecipeData> GetAllRecipes()
    {
        List<CocktailRecipeData> result = new();

        if (allRecipes == null) return result;

        foreach (var recipe in allRecipes)
        {
            if (recipe == null) continue;
            result.Add(recipe);
        }

        return result;
    }
}