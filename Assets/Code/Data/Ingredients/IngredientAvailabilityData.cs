using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static progression policy for ingredients. IngredientData keeps identity and
/// presentation; this asset owns when an ingredient is normally stocked.
/// </summary>
[CreateAssetMenu(menuName = "SURLHWA/Ingredient Availability", fileName = "IngredientAvailability")]
public sealed class IngredientAvailabilityData : ScriptableObject
{
    [SerializeField] private IngredientAvailabilityEntry[] entries = Array.Empty<IngredientAvailabilityEntry>();

    [Tooltip("Compatibility fallback for shelf ingredients not yet authored in this policy asset.")]
    [SerializeField] private bool unlistedIngredientsAreAvailable = true;

    public IReadOnlyList<IngredientAvailabilityEntry> Entries => entries ?? Array.Empty<IngredientAvailabilityEntry>();

    public bool IsIngredientAvailable(IngredientData ingredient, IngredientAvailabilityContext context)
    {
        if (ingredient == null)
            return false;

        if (context != null && context.IsTemporarilyAvailable(ingredient))
            return true;

        IngredientAvailabilityEntry entry = FindEntry(ingredient);
        if (entry == null)
            return unlistedIngredientsAreAvailable;

        return entry.IsAvailableAtDay(context != null ? context.CurrentBusinessDay : 1);
    }

    public List<IngredientData> GetAvailableIngredients(
        IEnumerable<IngredientData> candidates,
        IngredientAvailabilityContext context)
    {
        List<IngredientData> available = new();
        if (candidates == null)
            return available;

        foreach (IngredientData ingredient in candidates)
        {
            if (IsIngredientAvailable(ingredient, context))
                available.Add(ingredient);
        }

        return available;
    }

    public IngredientAvailabilityEntry FindEntry(IngredientData ingredient)
    {
        if (ingredient == null)
            return null;

        foreach (IngredientAvailabilityEntry entry in Entries)
        {
            if (entry != null && entry.Ingredient == ingredient)
                return entry;
        }

        return null;
    }

    private void OnValidate()
    {
        HashSet<IngredientData> seenIngredients = new();

        for (int index = 0; index < Entries.Count; index++)
        {
            IngredientAvailabilityEntry entry = Entries[index];
            if (entry == null || entry.Ingredient == null)
            {
                Debug.LogWarning($"[IngredientAvailabilityData] Missing ingredient at entry {index}.", this);
                continue;
            }

            if (!seenIngredients.Add(entry.Ingredient))
            {
                Debug.LogWarning(
                    $"[IngredientAvailabilityData] Duplicate ingredient entry at index {index}: " +
                    entry.Ingredient.ingredientId,
                    this);
            }
        }
    }
}

public enum IngredientAvailabilityPolicy
{
    MinimumBusinessDay,
    AlwaysAvailable
}

[Serializable]
public sealed class IngredientAvailabilityEntry
{
    [SerializeField] private IngredientData ingredient;
    [SerializeField] private IngredientAvailabilityPolicy policy = IngredientAvailabilityPolicy.MinimumBusinessDay;
    [SerializeField, Min(1)] private int minimumBusinessDay = 1;

    public IngredientData Ingredient => ingredient;
    public IngredientAvailabilityPolicy Policy => policy;
    public int MinimumBusinessDay => Mathf.Max(1, minimumBusinessDay);

    public bool IsAvailableAtDay(int currentBusinessDay)
    {
        return policy == IngredientAvailabilityPolicy.AlwaysAvailable ||
               Mathf.Max(1, currentBusinessDay) >= MinimumBusinessDay;
    }
}
