using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// The player-facing amount of authored recipe truth currently learned for one
/// recipe. This is progression/presentation state, never a replacement for
/// CocktailRecipeData or the live bartending preparation state.
/// </summary>
public enum RecipeKnowledgeState
{
    Unknown,
    Partial,
    Known
}

/// <summary>
/// Bounded non-ingredient fact categories. Ingredient identity and amount
/// facts are represented by stable IngredientData.ingredientId collections so
/// a partial recipe can identify exactly what the player has learned.
/// </summary>
[Flags]
public enum RecipeKnowledgeFact
{
    None = 0,
    Identity = 1 << 0,
    Method = 1 << 1,
    Ingredient = 1 << 2,
    IngredientAmount = 1 << 3,
    Garnish = 1 << 4,
    Preparation = 1 << 5
}

/// <summary>
/// Immutable query result returned by RecipeUnlockManager. The historical
/// component name is retained because it is serialized in the shipped scenes;
/// its current responsibility is recipe knowledge, not a binary unlock list.
/// </summary>
public sealed class RecipeKnowledgeFacts
{
    private static readonly IReadOnlyCollection<string> EmptyIngredientIds =
        new ReadOnlyCollection<string>(Array.Empty<string>());

    public RecipeKnowledgeState State { get; }
    public bool IdentityKnown { get; }
    public bool MethodKnown { get; }
    public bool GarnishKnown { get; }
    public bool PreparationKnown { get; }
    public IReadOnlyCollection<string> KnownIngredientIds { get; }
    public IReadOnlyCollection<string> KnownIngredientAmountIds { get; }

    public RecipeKnowledgeFact KnownFactCategories
    {
        get
        {
            RecipeKnowledgeFact facts = RecipeKnowledgeFact.None;

            if (IdentityKnown) facts |= RecipeKnowledgeFact.Identity;
            if (MethodKnown) facts |= RecipeKnowledgeFact.Method;
            if (KnownIngredientIds.Count > 0) facts |= RecipeKnowledgeFact.Ingredient;
            if (KnownIngredientAmountIds.Count > 0) facts |= RecipeKnowledgeFact.IngredientAmount;
            if (GarnishKnown) facts |= RecipeKnowledgeFact.Garnish;
            if (PreparationKnown) facts |= RecipeKnowledgeFact.Preparation;

            return facts;
        }
    }

    internal RecipeKnowledgeFacts(
        bool isKnown,
        bool identityKnown,
        bool methodKnown,
        bool garnishKnown,
        bool preparationKnown,
        IEnumerable<string> knownIngredientIds,
        IEnumerable<string> knownIngredientAmountIds)
    {
        IdentityKnown = identityKnown;
        MethodKnown = methodKnown;
        GarnishKnown = garnishKnown;
        PreparationKnown = preparationKnown;
        KnownIngredientIds = CreateStableSnapshot(knownIngredientIds);
        KnownIngredientAmountIds = CreateStableSnapshot(knownIngredientAmountIds);

        State = isKnown
            ? RecipeKnowledgeState.Known
            : HasAnyFacts
                ? RecipeKnowledgeState.Partial
                : RecipeKnowledgeState.Unknown;
    }

    public bool IsIngredientKnown(IngredientData ingredient)
    {
        return ingredient != null &&
               !string.IsNullOrWhiteSpace(ingredient.ingredientId) &&
               ContainsId(KnownIngredientIds, ingredient.ingredientId);
    }

    public bool IsIngredientAmountKnown(IngredientData ingredient)
    {
        return ingredient != null &&
               !string.IsNullOrWhiteSpace(ingredient.ingredientId) &&
               ContainsId(KnownIngredientAmountIds, ingredient.ingredientId);
    }

    private bool HasAnyFacts =>
        IdentityKnown ||
        MethodKnown ||
        GarnishKnown ||
        PreparationKnown ||
        KnownIngredientIds.Count > 0 ||
        KnownIngredientAmountIds.Count > 0;

    private static IReadOnlyCollection<string> CreateStableSnapshot(IEnumerable<string> ids)
    {
        if (ids == null)
            return EmptyIngredientIds;

        SortedSet<string> uniqueIds = new(StringComparer.Ordinal);
        foreach (string id in ids)
        {
            if (!string.IsNullOrWhiteSpace(id))
                uniqueIds.Add(id);
        }

        return uniqueIds.Count == 0
            ? EmptyIngredientIds
            : new ReadOnlyCollection<string>(new List<string>(uniqueIds));
    }

    private static bool ContainsId(IEnumerable<string> ids, string id)
    {
        foreach (string candidate in ids)
        {
            if (string.Equals(candidate, id, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
