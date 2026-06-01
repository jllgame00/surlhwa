using System.Collections.Generic;
using UnityEngine;

public static class RecipeMatchService
{
    public static RecipeMatchResult FindBestMatch(
        BrewComposition brew,
        IEnumerable<CocktailRecipeData> candidates)
    {
        CocktailRecipeData bestRecipe = null;
        float bestScore = 0f;

        if (brew == null || candidates == null)
        {
            return new RecipeMatchResult
            {
                matchedRecipe = null,
                similarity = 0f,
                success = false
            };
        }

        foreach (var recipe in candidates)
        {
            if (recipe == null) continue;
            if (!recipe.discoverable) continue;
            if (recipe.method != brew.method) continue;

            float score = CalculateSimilarity(brew, recipe);

            if (score > bestScore)
            {
                bestScore = score;
                bestRecipe = recipe;
            }
        }

        bool success = bestRecipe != null && bestScore >= bestRecipe.matchThreshold;

        return new RecipeMatchResult
        {
            matchedRecipe = bestRecipe,
            similarity = bestScore,
            success = success
        };
    }

    private static float CalculateSimilarity(BrewComposition brew, CocktailRecipeData recipe)
    {
        if (brew == null || recipe == null) return 0f;
        if (brew.ingredients == null || recipe.ingredients == null) return 0f;
        if (brew.ingredients.Length == 0 || recipe.ingredients.Length == 0) return 0f;
        if (brew.method != recipe.method) return 0f;

        float ingredientScore = CalculateIngredientMatchScore(brew, recipe);
        float ratioScore = CalculateRatioScore(brew, recipe);

        // 초반 판정 기준:
        // 재료가 맞는지 50%, 비율이 가까운지 50%
        return (ingredientScore * 0.5f) + (ratioScore * 0.5f);
    }

    private static float CalculateIngredientMatchScore(BrewComposition brew, CocktailRecipeData recipe)
    {
        int matchedCount = 0;
        int recipeCount = recipe.ingredients.Length;

        foreach (var recipeEntry in recipe.ingredients)
        {
            if (recipeEntry == null) continue;

            foreach (var brewEntry in brew.ingredients)
            {
                if (brewEntry == null) continue;

                if (brewEntry.ingredientId == recipeEntry.ingredientId)
                {
                    matchedCount++;
                    break;
                }
            }
        }

        if (recipeCount <= 0) return 0f;
        return Mathf.Clamp01((float)matchedCount / recipeCount);
    }

    private static float CalculateRatioScore(BrewComposition brew, CocktailRecipeData recipe)
    {
        float recipeTotal = GetRecipeTotalRatio(recipe);
        float brewTotal = GetBrewTotalAmount(brew);

        if (recipeTotal <= 0f || brewTotal <= 0f)
            return 0f;

        float totalDifference = 0f;
        int comparedCount = 0;

        foreach (var recipeEntry in recipe.ingredients)
        {
            if (recipeEntry == null) continue;

            float recipeNormalized = recipeEntry.ratio / recipeTotal;
            float brewNormalized = GetNormalizedBrewAmount(brew, recipeEntry.ingredientId, brewTotal);

            float diff = Mathf.Abs(recipeNormalized - brewNormalized);
            totalDifference += diff;
            comparedCount++;
        }

        if (comparedCount <= 0)
            return 0f;

        float averageDifference = totalDifference / comparedCount;

        // 차이가 0이면 1점, 차이가 커질수록 0점
        return Mathf.Clamp01(1f - averageDifference);
    }

    private static float GetRecipeTotalRatio(CocktailRecipeData recipe)
    {
        float total = 0f;

        foreach (var entry in recipe.ingredients)
        {
            if (entry == null) continue;
            total += Mathf.Max(0f, entry.ratio);
        }

        return total;
    }

    private static float GetBrewTotalAmount(BrewComposition brew)
    {
        float total = 0f;

        foreach (var entry in brew.ingredients)
        {
            if (entry == null) continue;
            total += Mathf.Max(0f, entry.amount);
        }

        return total;
    }

    private static float GetNormalizedBrewAmount(BrewComposition brew, string ingredientId, float brewTotal)
    {
        if (brewTotal <= 0f)
            return 0f;

        float amount = 0f;

        foreach (var entry in brew.ingredients)
        {
            if (entry == null) continue;

            if (entry.ingredientId == ingredientId)
                amount += entry.amount;
        }

        return amount / brewTotal;
    }
}

public struct RecipeMatchResult
{
    public CocktailRecipeData matchedRecipe;
    public float similarity;
    public bool success;
}