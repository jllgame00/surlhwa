using System;

public static class RequestEvaluation
{
    public static RequestEvaluationResult Evaluate(
        OrderData request,
        RecipeMatchResult matchResult)
    {
        if (request == null)
            return new RequestEvaluationResult(
                GuestRequestType.ExactCocktail,
                RequestEvaluationStatus.InvalidData);

        return request.RequestType switch
        {
            GuestRequestType.ExactCocktail => EvaluateExactCocktail(request, matchResult),
            GuestRequestType.Recommendation => EvaluateRecommendation(request, matchResult),
            _ => new RequestEvaluationResult(request.RequestType, RequestEvaluationStatus.InvalidData)
        };
    }

    private static RequestEvaluationResult EvaluateExactCocktail(
        OrderData request,
        RecipeMatchResult matchResult)
    {
        CocktailData requestedCocktail = request.requestedCocktail;
        if (requestedCocktail == null)
            return new RequestEvaluationResult(
                GuestRequestType.ExactCocktail,
                RequestEvaluationStatus.NotApplicable);

        if (string.IsNullOrWhiteSpace(requestedCocktail.cocktailId))
            return new RequestEvaluationResult(
                GuestRequestType.ExactCocktail,
                RequestEvaluationStatus.InvalidData);

        if (!matchResult.success)
            return new RequestEvaluationResult(
                GuestRequestType.ExactCocktail,
                RequestEvaluationStatus.Unsatisfied);

        CocktailRecipeData matchedRecipe = matchResult.matchedRecipe;
        if (matchedRecipe == null || matchedRecipe.targetCocktail == null)
            return new RequestEvaluationResult(
                GuestRequestType.ExactCocktail,
                RequestEvaluationStatus.InvalidData);

        string actualCocktailId = matchedRecipe.targetCocktail.cocktailId;
        if (string.IsNullOrWhiteSpace(actualCocktailId))
            return new RequestEvaluationResult(
                GuestRequestType.ExactCocktail,
                RequestEvaluationStatus.InvalidData);

        RequestEvaluationStatus status = string.Equals(
            requestedCocktail.cocktailId,
            actualCocktailId,
            StringComparison.Ordinal)
            ? RequestEvaluationStatus.Satisfied
            : RequestEvaluationStatus.Unsatisfied;

        return new RequestEvaluationResult(GuestRequestType.ExactCocktail, status);
    }

    private static RequestEvaluationResult EvaluateRecommendation(
        OrderData request,
        RecipeMatchResult matchResult)
    {
        EmotionTag[] desiredTags = request.DesiredEmotionTags;
        if (desiredTags.Length == 0)
        {
            return new RequestEvaluationResult(
                GuestRequestType.Recommendation,
                RequestEvaluationStatus.InvalidData);
        }

        if (!matchResult.success || matchResult.matchedRecipe == null ||
            matchResult.matchedRecipe.targetCocktail == null)
        {
            // Raw or unrecognized compositions receive no inferred emotional
            // meaning in I12, so they cannot satisfy a recommendation.
            return new RequestEvaluationResult(
                GuestRequestType.Recommendation,
                RequestEvaluationStatus.Unsatisfied);
        }

        EmotionTag[] cocktailTags = matchResult.matchedRecipe.targetCocktail.emotionTags;
        bool compatible = HasAnySharedTag(desiredTags, cocktailTags);
        return new RequestEvaluationResult(
            GuestRequestType.Recommendation,
            compatible
                ? RequestEvaluationStatus.Satisfied
                : RequestEvaluationStatus.Unsatisfied);
    }

    private static bool HasAnySharedTag(EmotionTag[] desiredTags, EmotionTag[] cocktailTags)
    {
        if (cocktailTags == null || cocktailTags.Length == 0)
            return false;

        foreach (EmotionTag desired in desiredTags)
        {
            foreach (EmotionTag cocktailTag in cocktailTags)
            {
                if (desired == cocktailTag)
                    return true;
            }
        }

        return false;
    }
}
