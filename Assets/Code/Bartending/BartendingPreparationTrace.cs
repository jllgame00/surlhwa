using System;
using System.Collections.Generic;

/// <summary>
/// Result-bound evidence of actions the player performed during one
/// preparation. It never owns liquid quantity: BartendingContainerState does.
/// </summary>
public sealed class BartendingPreparationTrace
{
    private readonly List<ActualPreparationAction> actions = new List<ActualPreparationAction>();

    public IReadOnlyList<ActualPreparationAction> Actions => actions;

    // Measurement is a way to perform one authored Pour, not a new recipe step.
    // Repeated measures of the same source into the same destination coalesce
    // only when no intervening preparation action has occurred.
    public void RecordMeasuredPour(IngredientData ingredient, float amountMl, BartendingContainerType target)
    {
        if (amountMl <= 0f) return;
        if (actions.Count > 0)
        {
            var last = actions[actions.Count - 1];
            if (last.Action == RecipePreparationAction.Pour && last.Ingredient == ingredient && last.Target == target)
            {
                actions[actions.Count - 1] = new ActualPreparationAction(last.Action, ingredient,
                    last.ActualMl + amountMl, null, target, 1, null);
                return;
            }
        }
        Record(RecipePreparationAction.Pour, ingredient, amountMl, null, target);
    }

    public void Record(
        RecipePreparationAction action,
        IngredientData ingredient = null,
        float actualMl = 0f,
        BartendingContainerType? source = null,
        BartendingContainerType? target = null,
        int count = 1,
        BartendingGarnishState garnish = null)
    {
        actions.Add(new ActualPreparationAction(
            action,
            ingredient,
            actualMl,
            source,
            target,
            Math.Max(1, count),
            garnish));
    }

    /// <summary>
    /// Garnish replacement is a correction to the one active garnish. Retain
    /// only the final actual garnish evidence so an earlier corrected choice
    /// cannot make a final drink fail its preparation expectation.
    /// </summary>
    public void RecordActiveGarnish(BartendingGarnishState garnish, BartendingContainerType target)
    {
        actions.RemoveAll(action => action.Action == RecipePreparationAction.Garnish);
        if (garnish != null)
            Record(RecipePreparationAction.Garnish, null, 0f, null, target, 1, garnish);
    }

    public void ClearActiveGarnish()
    {
        actions.RemoveAll(action => action.Action == RecipePreparationAction.Garnish);
    }
}

/// <summary>
/// Immutable action evidence. ActualMl records only liquid accepted by the
/// authoritative container operation; it is informational for diagnostics,
/// while the matcher retains normal fixed-volume evaluation ownership.
/// </summary>
public sealed class ActualPreparationAction
{
    public RecipePreparationAction Action { get; }
    public IngredientData Ingredient { get; }
    public float ActualMl { get; }
    public BartendingContainerType? Source { get; }
    public BartendingContainerType? Target { get; }
    public int Count { get; }
    public BartendingGarnishState Garnish { get; }

    public ActualPreparationAction(
        RecipePreparationAction action,
        IngredientData ingredient,
        float actualMl,
        BartendingContainerType? source,
        BartendingContainerType? target,
        int count,
        BartendingGarnishState garnish)
    {
        Action = action;
        Ingredient = ingredient;
        ActualMl = Math.Max(0f, actualMl);
        Source = source;
        Target = target;
        Count = Math.Max(1, count);
        Garnish = garnish;
    }
}
