using System;
using UnityEngine;

/// <summary>
/// The small preparation vocabulary adopted for I9B's seven IBA validation
/// recipes. This is recipe content, not a general cocktail scripting language.
/// </summary>
public enum RecipePreparationAction
{
    Pour,
    Dash,
    Splash,
    AddIce,
    Stir,
    GentleStir,
    Shake,
    Strain,
    PourFromContainer,
    TopUp,
    Garnish
}

/// <summary>
/// Expected-container vocabulary includes Any only for a recipe action whose
/// destination is intentionally irrelevant. Runtime actions continue to use
/// the concrete BartendingContainerType.
/// </summary>
public enum RecipePreparationContainer
{
    Any,
    ServingGlass,
    MixingGlass,
    Shaker
}

/// <summary>
/// One accepted garnish alternative. I9B keeps one actual garnish on the
/// Serving Glass, while recipe content may accept a small explicit set.
/// </summary>
[Serializable]
public sealed class RecipeGarnishExpectation
{
    public BartendingGarnishId garnish;
    public BartendingGarnishPlacement placement;
}

/// <summary>
/// A typed, bounded expected preparation step owned by a CocktailRecipeData.
/// Normal liquid amount fidelity remains RecipeMatchService's existing
/// responsibility; steps establish action semantics, containers, and order.
/// </summary>
[Serializable]
public sealed class RecipePreparationStep
{
    public RecipePreparationAction action;
    public IngredientData ingredient;
    [Min(1)] public int repeatCount = 1;
    public RecipePreparationContainer source = RecipePreparationContainer.Any;
    public RecipePreparationContainer target = RecipePreparationContainer.Any;
    public bool optional;
    public RecipeGarnishExpectation[] acceptedGarnishes = Array.Empty<RecipeGarnishExpectation>();
}
