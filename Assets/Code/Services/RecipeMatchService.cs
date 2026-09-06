using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class RecipeMatchService
{
    private const float IngredientScoreWeight = 0.35f;
    private const float AmountScoreWeight = 0.50f;
    private const float ExtraIngredientScoreWeight = 0.15f;

    private const float AmountFailureToleranceOz = 1.10f;
    private const float ExtraIngredientFailureToleranceOz = 1.00f;

    public static RecipeMatchResult FindBestMatch(
        BrewComposition brew,
        IEnumerable<CocktailRecipeData> candidates)
    {
        CocktailRecipeData bestRecipe = null;
        float bestScore = 0f;
        SimilarityBreakdown bestBreakdown = default;
        bool evaluatedAnyRecipe = false;

        if (brew == null || candidates == null)
        {
            Debug.LogWarning("[RecipeMatch] Cannot match. Brew or candidates is NULL.");

            return new RecipeMatchResult
            {
                matchedRecipe = null,
                similarity = 0f,
                success = false
            };
        }

        Debug.Log(
            $"[RecipeMatch] Start / Method={brew.method} / " +
            $"Brewed=[{FormatBrewIngredients(brew)}]"
        );

        foreach (var recipe in candidates)
        {
            if (recipe == null) continue;
            if (!recipe.discoverable) continue;
            if (recipe.method != brew.method) continue;

            evaluatedAnyRecipe = true;

            SimilarityBreakdown breakdown = CalculateSimilarityBreakdown(brew, recipe);
            float score = breakdown.totalScore;

            Debug.Log(
                $"[RecipeMatch] Candidate={recipe.recipeId} / " +
                $"Target=[{FormatRecipeIngredients(recipe)}] / " +
                $"IngredientScore={breakdown.ingredientScore:0.00} ({breakdown.matchedIngredientCount}/{breakdown.recipeIngredientCount}, extraCount={breakdown.extraIngredientCount}) / " +
                $"AmountScore={breakdown.amountScore:0.00} (maxDiff={breakdown.maxAmountDifference:0.##} oz) / " +
                $"ExtraScore={breakdown.extraIngredientScore:0.00} (extraAmount={breakdown.extraAmount:0.##} oz) / " +
                $"Similarity={score:0.00} / Threshold={recipe.matchThreshold:0.00}"
            );

            if (score > bestScore)
            {
                bestScore = score;
                bestRecipe = recipe;
                bestBreakdown = breakdown;
            }
        }

        if (!evaluatedAnyRecipe)
        {
            Debug.LogWarning($"[RecipeMatch] FAIL / No discoverable recipe candidates for method={brew.method}");
        }

        bool success = bestRecipe != null && bestScore >= bestRecipe.matchThreshold;

        if (success)
        {
            Debug.Log(
                $"[RecipeMatch] SUCCESS / Match={bestRecipe.recipeId} / " +
                $"Similarity={bestScore:0.00} / Threshold={bestRecipe.matchThreshold:0.00}"
            );
        }
        else if (bestRecipe != null)
        {
            Debug.LogWarning(
                $"[RecipeMatch] FAIL / Best={bestRecipe.recipeId} / " +
                $"Similarity={bestScore:0.00} / Threshold={bestRecipe.matchThreshold:0.00} / " +
                $"Reason={BuildFailReason(bestBreakdown, bestRecipe)}"
            );
        }

        return new RecipeMatchResult
        {
            matchedRecipe = bestRecipe,
            similarity = bestScore,
            success = success
        };
    }

    private static float CalculateSimilarity(BrewComposition brew, CocktailRecipeData recipe)
    {
        return CalculateSimilarityBreakdown(brew, recipe).totalScore;
    }

    private static SimilarityBreakdown CalculateSimilarityBreakdown(BrewComposition brew, CocktailRecipeData recipe)
    {
        SimilarityBreakdown breakdown = new SimilarityBreakdown();

        if (brew == null || recipe == null) return breakdown;
        if (brew.ingredients == null || recipe.ingredients == null) return breakdown;
        if (brew.ingredients.Length == 0 || recipe.ingredients.Length == 0) return breakdown;
        if (brew.method != recipe.method) return breakdown;

        Dictionary<string, float> brewAmounts = BuildBrewAmountMap(brew);
        Dictionary<string, float> recipeAmounts = BuildRecipeAmountMap(recipe);

        breakdown.recipeIngredientCount = recipeAmounts.Count;

        if (brewAmounts.Count == 0 || recipeAmounts.Count == 0)
            return breakdown;

        breakdown.ingredientScore = CalculateIngredientMatchScore(
            brewAmounts,
            recipeAmounts,
            out breakdown.matchedIngredientCount,
            out breakdown.extraIngredientCount
        );

        breakdown.amountScore = CalculateAmountScore(
            brewAmounts,
            recipeAmounts,
            out breakdown.maxAmountDifference
        );

        breakdown.extraIngredientScore = CalculateExtraIngredientScore(
            brewAmounts,
            recipeAmounts,
            out breakdown.extraAmount
        );

        breakdown.totalScore =
            (breakdown.ingredientScore * IngredientScoreWeight) +
            (breakdown.amountScore * AmountScoreWeight) +
            (breakdown.extraIngredientScore * ExtraIngredientScoreWeight);

        return breakdown;
    }

    private static float CalculateIngredientMatchScore(
        Dictionary<string, float> brewAmounts,
        Dictionary<string, float> recipeAmounts,
        out int matchedIngredientCount,
        out int extraIngredientCount)
    {
        matchedIngredientCount = 0;
        extraIngredientCount = 0;

        foreach (var recipePair in recipeAmounts)
        {
            if (brewAmounts.ContainsKey(recipePair.Key))
                matchedIngredientCount++;
        }

        foreach (var brewPair in brewAmounts)
        {
            if (!recipeAmounts.ContainsKey(brewPair.Key))
                extraIngredientCount++;
        }

        int denominator = recipeAmounts.Count + extraIngredientCount;

        if (denominator <= 0)
            return 0f;

        return Mathf.Clamp01((float)matchedIngredientCount / denominator);
    }

    private static float CalculateAmountScore(
        Dictionary<string, float> brewAmounts,
        Dictionary<string, float> recipeAmounts,
        out float maxAmountDifference)
    {
        maxAmountDifference = 0f;

        if (recipeAmounts.Count <= 0)
            return 0f;

        float scoreTotal = 0f;

        foreach (var recipePair in recipeAmounts)
        {
            brewAmounts.TryGetValue(recipePair.Key, out float brewedAmount);

            float difference = Mathf.Abs(brewedAmount - recipePair.Value);
            maxAmountDifference = Mathf.Max(maxAmountDifference, difference);

            scoreTotal += Mathf.Clamp01(1f - (difference / AmountFailureToleranceOz));
        }

        float averageIngredientScore = scoreTotal / recipeAmounts.Count;
        float maxDifferenceScore = Mathf.Clamp01(1f - (maxAmountDifference / AmountFailureToleranceOz));

        return Mathf.Clamp01((averageIngredientScore * 0.6f) + (maxDifferenceScore * 0.4f));
    }

    private static float CalculateExtraIngredientScore(
        Dictionary<string, float> brewAmounts,
        Dictionary<string, float> recipeAmounts,
        out float extraAmount)
    {
        extraAmount = 0f;

        foreach (var brewPair in brewAmounts)
        {
            if (!recipeAmounts.ContainsKey(brewPair.Key))
                extraAmount += Mathf.Max(0f, brewPair.Value);
        }

        return Mathf.Clamp01(1f - (extraAmount / ExtraIngredientFailureToleranceOz));
    }

    private static Dictionary<string, float> BuildBrewAmountMap(BrewComposition brew)
    {
        Dictionary<string, float> amounts = new Dictionary<string, float>();

        if (brew == null || brew.ingredients == null)
            return amounts;

        foreach (var entry in brew.ingredients)
        {
            if (entry == null) continue;
            if (string.IsNullOrEmpty(entry.ingredientId)) continue;

            float amount = Mathf.Max(0f, entry.amount);
            if (amount <= 0f) continue;

            if (!amounts.ContainsKey(entry.ingredientId))
                amounts[entry.ingredientId] = 0f;

            amounts[entry.ingredientId] += amount;
        }

        return amounts;
    }

    private static Dictionary<string, float> BuildRecipeAmountMap(CocktailRecipeData recipe)
    {
        Dictionary<string, float> amounts = new Dictionary<string, float>();

        if (recipe == null || recipe.ingredients == null)
            return amounts;

        foreach (var entry in recipe.ingredients)
        {
            if (entry == null) continue;
            if (string.IsNullOrEmpty(entry.ingredientId)) continue;

            float amount = Mathf.Max(0f, entry.ratio);
            if (amount <= 0f) continue;

            if (!amounts.ContainsKey(entry.ingredientId))
                amounts[entry.ingredientId] = 0f;

            amounts[entry.ingredientId] += amount;
        }

        return amounts;
    }

    private static string FormatBrewIngredients(BrewComposition brew)
    {
        if (brew == null || brew.ingredients == null || brew.ingredients.Length == 0)
            return "None";

        StringBuilder sb = new StringBuilder();

        foreach (var entry in brew.ingredients)
        {
            if (entry == null) continue;

            if (sb.Length > 0)
                sb.Append(", ");

            sb.Append(string.IsNullOrEmpty(entry.ingredientId) ? "Unknown" : entry.ingredientId);
            sb.Append("=");
            sb.Append(Mathf.Max(0f, entry.amount).ToString("0.##"));
            sb.Append("oz");
        }

        return sb.Length > 0 ? sb.ToString() : "None";
    }

    private static string FormatRecipeIngredients(CocktailRecipeData recipe)
    {
        if (recipe == null || recipe.ingredients == null || recipe.ingredients.Length == 0)
            return "None";

        StringBuilder sb = new StringBuilder();

        foreach (var entry in recipe.ingredients)
        {
            if (entry == null) continue;

            if (sb.Length > 0)
                sb.Append(", ");

            sb.Append(string.IsNullOrEmpty(entry.ingredientId) ? "Unknown" : entry.ingredientId);
            sb.Append("=");
            sb.Append(Mathf.Max(0f, entry.ratio).ToString("0.##"));
            sb.Append("oz");
        }

        return sb.Length > 0 ? sb.ToString() : "None";
    }

    private static string BuildFailReason(SimilarityBreakdown breakdown, CocktailRecipeData recipe)
    {
        if (recipe == null)
            return "No recipe candidate.";

        if (breakdown.recipeIngredientCount <= 0)
            return "Recipe has no valid ingredients.";

        if (breakdown.matchedIngredientCount < breakdown.recipeIngredientCount)
        {
            return $"Missing recipe ingredient(s). matched={breakdown.matchedIngredientCount}/{breakdown.recipeIngredientCount}";
        }

        if (breakdown.extraIngredientCount > 0)
        {
            return $"Extra ingredient(s). count={breakdown.extraIngredientCount}, amount={breakdown.extraAmount:0.##} oz";
        }

        if (breakdown.maxAmountDifference >= 0.75f)
        {
            return $"Large amount mismatch. maxDiff={breakdown.maxAmountDifference:0.##} oz";
        }

        return "Similarity below recipe threshold.";
    }

    private struct SimilarityBreakdown
    {
        public float ingredientScore;
        public float amountScore;
        public float extraIngredientScore;
        public float totalScore;

        public int matchedIngredientCount;
        public int recipeIngredientCount;
        public int extraIngredientCount;

        public float maxAmountDifference;
        public float extraAmount;
    }
}

public struct RecipeMatchResult
{
    public CocktailRecipeData matchedRecipe;
    public float similarity;
    public bool success;
}
