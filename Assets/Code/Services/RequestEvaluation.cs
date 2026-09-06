using System;

public static class RequestEvaluation
{
    public static RequestEvaluationResult Evaluate(
        OrderData request,
        RecipeMatchResult matchResult)
    {
        if (request == null)
            return new RequestEvaluationResult(RequestEvaluationStatus.InvalidData);

        CocktailData requestedCocktail = request.requestedCocktail;
        if (requestedCocktail == null)
            return new RequestEvaluationResult(RequestEvaluationStatus.NotApplicable);

        if (string.IsNullOrWhiteSpace(requestedCocktail.cocktailId))
            return new RequestEvaluationResult(RequestEvaluationStatus.InvalidData);

        if (!matchResult.success)
            return new RequestEvaluationResult(RequestEvaluationStatus.Unsatisfied);

        CocktailRecipeData matchedRecipe = matchResult.matchedRecipe;
        if (matchedRecipe == null || matchedRecipe.targetCocktail == null)
            return new RequestEvaluationResult(RequestEvaluationStatus.InvalidData);

        string actualCocktailId = matchedRecipe.targetCocktail.cocktailId;
        if (string.IsNullOrWhiteSpace(actualCocktailId))
            return new RequestEvaluationResult(RequestEvaluationStatus.InvalidData);

        RequestEvaluationStatus status = string.Equals(
            requestedCocktail.cocktailId,
            actualCocktailId,
            StringComparison.Ordinal)
            ? RequestEvaluationStatus.Satisfied
            : RequestEvaluationStatus.Unsatisfied;

        return new RequestEvaluationResult(status);
    }
}
