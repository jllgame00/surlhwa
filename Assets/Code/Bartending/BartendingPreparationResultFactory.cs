/// <summary>
/// Applies I9B's optional preparation evidence to an existing MiniGameResult
/// without changing each route's technique-quality contract.
/// </summary>
public static class BartendingPreparationResultFactory
{
    public static void Apply(
        ref MiniGameResult result,
        CocktailRecipeData expectedRecipe,
        BartendingPreparationTrace trace)
    {
        result.expectedRecipe = expectedRecipe;
        result.preparationTrace = trace;
        result.preparationEvaluation = RecipePreparationEvaluator.Evaluate(expectedRecipe, trace);
    }
}
