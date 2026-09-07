using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The limited container vocabulary needed by the Bartending 2.0 Pour Core.
/// I1 actively proves ServingGlass; the other identities reserve no behavior,
/// physics, or contents rules ahead of their adopted phases.
/// </summary>
public enum BartendingContainerType
{
    ServingGlass,
    MixingGlass,
    Shaker
}

/// <summary>
/// One liquid entry in a preparation container. AmountMl is authoritative;
/// ounce values are produced only at the legacy recipe-matching bridge.
/// </summary>
public sealed class BartendingLiquidEntry
{
    public IngredientData Ingredient { get; }
    public float AmountMl { get; private set; }

    internal BartendingLiquidEntry(IngredientData ingredient, float amountMl)
    {
        Ingredient = ingredient;
        AmountMl = Mathf.Max(0f, amountMl);
    }

    internal void SetAmountMl(float amountMl)
    {
        AmountMl = Mathf.Max(0f, amountMl);
    }
}

/// <summary>
/// Runtime-only liquid state for one bartending container.
/// It deliberately excludes ice, temperature, dilution, density, layering, and
/// overflow gameplay. I1 has no capacity rule; a future adopted container phase
/// may supply a positive capacity without changing liquid quantity authority.
/// </summary>
public sealed class BartendingContainerState
{
    private readonly List<BartendingLiquidEntry> liquidEntries = new List<BartendingLiquidEntry>();

    public BartendingContainerType ContainerType { get; }
    public float CapacityMl { get; }
    public float CurrentLiquidVolumeMl { get; private set; }
    public IReadOnlyList<BartendingLiquidEntry> LiquidEntries => liquidEntries;

    public BartendingContainerState(BartendingContainerType containerType, float capacityMl = 0f)
    {
        ContainerType = containerType;
        CapacityMl = IsFinitePositive(capacityMl) ? capacityMl : 0f;
    }

    public float AddLiquid(IngredientData ingredient, float requestedMl)
    {
        if (ingredient == null || !IsFinitePositive(requestedMl))
            return 0f;

        float acceptedMl = requestedMl;
        if (CapacityMl > 0f)
            acceptedMl = Mathf.Min(acceptedMl, Mathf.Max(0f, CapacityMl - CurrentLiquidVolumeMl));

        if (!IsFinitePositive(acceptedMl))
            return 0f;

        BartendingLiquidEntry entry = FindEntry(ingredient);
        if (entry == null)
        {
            entry = new BartendingLiquidEntry(ingredient, acceptedMl);
            liquidEntries.Add(entry);
        }
        else
        {
            entry.SetAmountMl(entry.AmountMl + acceptedMl);
        }

        CurrentLiquidVolumeMl += acceptedMl;
        return acceptedMl;
    }

    public void SetLiquidAmount(IngredientData ingredient, float amountMl)
    {
        if (ingredient == null)
            return;

        float clampedAmountMl = IsFinitePositive(amountMl) ? amountMl : 0f;
        BartendingLiquidEntry entry = FindEntry(ingredient);

        if (entry == null)
        {
            if (clampedAmountMl <= 0f)
                return;

            AddLiquid(ingredient, clampedAmountMl);
            return;
        }

        float otherLiquidMl = Mathf.Max(0f, CurrentLiquidVolumeMl - entry.AmountMl);
        if (CapacityMl > 0f)
            clampedAmountMl = Mathf.Min(clampedAmountMl, Mathf.Max(0f, CapacityMl - otherLiquidMl));

        if (clampedAmountMl <= 0f)
        {
            liquidEntries.Remove(entry);
            CurrentLiquidVolumeMl = otherLiquidMl;
            return;
        }

        entry.SetAmountMl(clampedAmountMl);
        CurrentLiquidVolumeMl = otherLiquidMl + clampedAmountMl;
    }

    public float RemoveLiquid(IngredientData ingredient, float requestedMl)
    {
        if (ingredient == null || !IsFinitePositive(requestedMl))
            return 0f;

        BartendingLiquidEntry entry = FindEntry(ingredient);
        if (entry == null)
            return 0f;

        float removedMl = Mathf.Min(entry.AmountMl, requestedMl);
        float remainingMl = Mathf.Max(0f, entry.AmountMl - removedMl);

        if (remainingMl <= 0f)
            liquidEntries.Remove(entry);
        else
            entry.SetAmountMl(remainingMl);

        CurrentLiquidVolumeMl = Mathf.Max(0f, CurrentLiquidVolumeMl - removedMl);
        return removedMl;
    }

    public float GetLiquidAmountMl(IngredientData ingredient)
    {
        BartendingLiquidEntry entry = FindEntry(ingredient);
        return entry != null ? entry.AmountMl : 0f;
    }

    /// <summary>
    /// Temporary, one-way adapter for the existing ounce-authored recipe matcher.
    /// The container remains the source of truth; the returned BrewComposition is
    /// a result-bound snapshot, not a second editable preparation state.
    /// </summary>
    public BrewComposition ToLegacyBrewComposition(MiniGameType method)
    {
        List<BrewIngredientEntry> ingredients = new List<BrewIngredientEntry>();

        foreach (BartendingLiquidEntry entry in liquidEntries)
        {
            if (entry == null || entry.Ingredient == null || string.IsNullOrEmpty(entry.Ingredient.ingredientId))
                continue;

            float amountOz = BartendingVolume.MillilitersToOunces(entry.AmountMl);
            if (amountOz <= 0f)
                continue;

            ingredients.Add(new BrewIngredientEntry
            {
                ingredientId = entry.Ingredient.ingredientId,
                amount = amountOz
            });
        }

        return new BrewComposition
        {
            method = method,
            ingredients = ingredients.ToArray()
        };
    }

    private BartendingLiquidEntry FindEntry(IngredientData ingredient)
    {
        if (ingredient == null)
            return null;

        foreach (BartendingLiquidEntry entry in liquidEntries)
        {
            if (entry != null && entry.Ingredient == ingredient)
                return entry;
        }

        return null;
    }

    private static bool IsFinitePositive(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
    }
}
