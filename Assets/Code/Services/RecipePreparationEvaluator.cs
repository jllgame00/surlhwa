using System;
using System.Collections.Generic;

public enum RecipePreparationEvaluationStatus
{
    NotEvaluated,
    Satisfied,
    Unsatisfied
}

/// <summary>
/// Separate result axis for recipe-owned action/order expectations. It does
/// not compare normal liquid volumes (RecipeMatchService remains the single
/// matcher authority) and it never changes preparation state.
/// </summary>
public sealed class RecipePreparationEvaluationResult
{
    public RecipePreparationEvaluationStatus Status { get; }
    public int ExpectedStepCount { get; }
    public int MatchedActionCount { get; }
    public string Detail { get; }

    public RecipePreparationEvaluationResult(
        RecipePreparationEvaluationStatus status,
        int expectedStepCount,
        int matchedActionCount,
        string detail)
    {
        Status = status;
        ExpectedStepCount = expectedStepCount;
        MatchedActionCount = matchedActionCount;
        Detail = detail ?? string.Empty;
    }
}

public static class RecipePreparationEvaluator
{
    public static RecipePreparationEvaluationResult Evaluate(
        CocktailRecipeData recipe,
        BartendingPreparationTrace trace)
    {
        if (recipe == null || recipe.expectedPreparation == null || recipe.expectedPreparation.Length == 0)
        {
            return new RecipePreparationEvaluationResult(
                RecipePreparationEvaluationStatus.NotEvaluated,
                0,
                0,
                "Recipe has no expected preparation steps.");
        }

        IReadOnlyList<ActualPreparationAction> actions = trace != null
            ? trace.Actions
            : Array.Empty<ActualPreparationAction>();

        int actionIndex = 0;
        int matched = 0;
        for (int stepIndex = 0; stepIndex < recipe.expectedPreparation.Length; stepIndex++)
        {
            RecipePreparationStep step = recipe.expectedPreparation[stepIndex];
            if (step == null)
                continue;

            int repetitions = Math.Max(1, step.repeatCount);
            for (int repetition = 0; repetition < repetitions; repetition++)
            {
                if (actionIndex >= actions.Count)
                {
                    if (step.optional)
                        break;

                    return Unsatisfied(recipe, matched,
                        $"Missing {DescribeStep(step)} at expected step {stepIndex + 1}.");
                }

                ActualPreparationAction actual = actions[actionIndex];
                if (Matches(step, actual))
                {
                    actionIndex++;
                    matched++;
                    continue;
                }

                if (step.optional)
                    break;

                return Unsatisfied(recipe, matched,
                    $"Expected {DescribeStep(step)} at step {stepIndex + 1}, " +
                    $"received {DescribeAction(actual)}.");
            }
        }

        if (actionIndex < actions.Count)
        {
            return Unsatisfied(recipe, matched,
                $"Unexpected additional action {DescribeAction(actions[actionIndex])}.");
        }

        return new RecipePreparationEvaluationResult(
            RecipePreparationEvaluationStatus.Satisfied,
            recipe.expectedPreparation.Length,
            matched,
            "Expected preparation sequence completed.");
    }

    private static RecipePreparationEvaluationResult Unsatisfied(
        CocktailRecipeData recipe,
        int matched,
        string detail)
    {
        return new RecipePreparationEvaluationResult(
            RecipePreparationEvaluationStatus.Unsatisfied,
            recipe != null && recipe.expectedPreparation != null ? recipe.expectedPreparation.Length : 0,
            matched,
            detail);
    }

    private static bool Matches(RecipePreparationStep expected, ActualPreparationAction actual)
    {
        if (expected == null || actual == null || expected.action != actual.Action)
            return false;

        if (expected.ingredient != null && expected.ingredient != actual.Ingredient)
            return false;

        if (!MatchesContainer(expected.source, actual.Source) ||
            !MatchesContainer(expected.target, actual.Target))
            return false;

        if (expected.action != RecipePreparationAction.Garnish)
            return true;

        if (expected.acceptedGarnishes == null || expected.acceptedGarnishes.Length == 0)
            return actual.Garnish == null;

        if (actual.Garnish == null)
            return false;

        foreach (RecipeGarnishExpectation accepted in expected.acceptedGarnishes)
        {
            if (accepted != null &&
                accepted.garnish == actual.Garnish.GarnishId &&
                accepted.placement == actual.Garnish.Placement)
                return true;
        }

        return false;
    }

    private static bool MatchesContainer(
        RecipePreparationContainer expected,
        BartendingContainerType? actual)
    {
        if (expected == RecipePreparationContainer.Any)
            return true;

        return actual.HasValue && (int)expected - 1 == (int)actual.Value;
    }

    private static string DescribeStep(RecipePreparationStep step)
    {
        if (step == null)
            return "unknown step";

        string ingredient = step.ingredient != null ? " " + step.ingredient.ingredientId : string.Empty;
        return step.action + ingredient;
    }

    private static string DescribeAction(ActualPreparationAction action)
    {
        if (action == null)
            return "none";

        string ingredient = action.Ingredient != null ? " " + action.Ingredient.ingredientId : string.Empty;
        return action.Action + ingredient;
    }
}
