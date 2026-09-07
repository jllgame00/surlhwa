using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SURLHWA/Recipe Catalog", fileName = "RecipeCatalog")]
public sealed class RecipeCatalogData : ScriptableObject
{
    [SerializeField] private RecipeCatalogEntry[] entries = Array.Empty<RecipeCatalogEntry>();

    public IReadOnlyList<RecipeCatalogEntry> Entries => entries ?? Array.Empty<RecipeCatalogEntry>();

    public bool IsUnlockedByDefault(CocktailRecipeData recipe)
    {
        if (recipe == null)
            return false;

        foreach (RecipeCatalogEntry entry in Entries)
        {
            if (entry != null && entry.Recipe == recipe)
                return entry.UnlockedByDefault;
        }

        return false;
    }

    /// <summary>
    /// Resolves an explicit order request through the authoritative catalog.
    /// Generic orders remain recipe-agnostic and therefore receive no I9B
    /// preparation expectation.
    /// </summary>
    public CocktailRecipeData FindRecipeForCocktail(CocktailData cocktail, MiniGameType method)
    {
        if (cocktail == null)
            return null;

        foreach (RecipeCatalogEntry entry in Entries)
        {
            CocktailRecipeData recipe = entry != null ? entry.Recipe : null;
            if (recipe != null && recipe.method == method && recipe.targetCocktail == cocktail)
                return recipe;
        }

        return null;
    }

    private void OnValidate()
    {
        HashSet<CocktailRecipeData> seenRecipes = new();
        HashSet<string> seenRecipeIds = new();

        for (int index = 0; index < Entries.Count; index++)
        {
            RecipeCatalogEntry entry = Entries[index];
            if (entry == null)
            {
                Debug.LogWarning($"[RecipeCatalogData] Null entry at index {index}.", this);
                continue;
            }

            CocktailRecipeData recipe = entry.Recipe;
            if (recipe == null)
            {
                Debug.LogWarning($"[RecipeCatalogData] Null recipe at entry {index}.", this);
                continue;
            }

            if (!seenRecipes.Add(recipe))
            {
                Debug.LogWarning(
                    $"[RecipeCatalogData] Duplicate recipe reference at entry {index}: {recipe.name}.",
                    this);
            }

            if (string.IsNullOrWhiteSpace(recipe.recipeId))
            {
                Debug.LogWarning(
                    $"[RecipeCatalogData] Empty recipeId at entry {index}: {recipe.name}.",
                    this);
                continue;
            }

            if (!seenRecipeIds.Add(recipe.recipeId))
            {
                Debug.LogWarning(
                    $"[RecipeCatalogData] Duplicate recipeId at entry {index}: {recipe.recipeId}.",
                    this);
            }
        }
    }
}

[Serializable]
public sealed class RecipeCatalogEntry
{
    [SerializeField] private CocktailRecipeData recipe;
    [SerializeField] private bool unlockedByDefault;

    public CocktailRecipeData Recipe => recipe;
    public bool UnlockedByDefault => unlockedByDefault;
}
