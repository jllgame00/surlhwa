using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The limited container vocabulary currently adopted by Bartending 2.0.
/// I3 gives each identity separate runtime liquid/ice state; it deliberately
/// does not add physical simulation or technique-specific quality rules.
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
    public int DashCount { get; private set; }

    internal BartendingLiquidEntry(IngredientData ingredient, float amountMl)
    {
        Ingredient = ingredient;
        AmountMl = Mathf.Max(0f, amountMl);
    }

    internal void SetAmountMl(float amountMl)
    {
        AmountMl = Mathf.Max(0f, amountMl);
    }

    internal void AddDash()
    {
        if (DashCount < int.MaxValue)
            DashCount++;
    }
}

/// <summary>
/// Runtime-only preparation state for one bartending container.
/// I3 adds bounded ice presence and full liquid transfer. It deliberately still
/// excludes temperature, dilution, density, layering, and overflow gameplay.
/// </summary>
public sealed class BartendingContainerState
{
    private readonly List<BartendingLiquidEntry> liquidEntries = new List<BartendingLiquidEntry>();

    public BartendingContainerType ContainerType { get; }
    public float CapacityMl { get; }
    public float CurrentLiquidVolumeMl { get; private set; }
    public bool HasIce { get; private set; }
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
    /// Adds one discrete Dash while recording its deterministic gameplay-ml
    /// equivalent in the same authoritative liquid entry used by the legacy
    /// composition bridge. The dash count is retained for player feedback and
    /// future recipe instruction data; it is not a second quantity store.
    /// </summary>
    public bool TryAddDash(IngredientData ingredient, float millilitersPerDash, out int totalDashCount)
    {
        totalDashCount = GetDashCount(ingredient);

        if (ingredient == null || !IsFinitePositive(millilitersPerDash))
            return false;

        // I1 does not enforce capacity, but reject an incomplete discrete dash
        // if a future adopted container supplies one rather than recording a
        // misleading full dash for a partial transfer.
        if (CapacityMl > 0f && CurrentLiquidVolumeMl + millilitersPerDash > CapacityMl)
            return false;

        float transferredMl = AddLiquid(ingredient, millilitersPerDash);
        if (!Mathf.Approximately(transferredMl, millilitersPerDash))
            return false;

        BartendingLiquidEntry entry = FindEntry(ingredient);
        if (entry == null)
            return false;

        entry.AddDash();
        totalDashCount = entry.DashCount;
        return true;
    }

    public int GetDashCount(IngredientData ingredient)
    {
        BartendingLiquidEntry entry = FindEntry(ingredient);
        return entry != null ? entry.DashCount : 0;
    }

    /// <summary>
    /// Adds bounded ice presence to this specific container. I3 does not model
    /// individual cubes, melting, temperature, dilution, or ice volume.
    /// </summary>
    public bool TryAddIce()
    {
        if (HasIce)
            return false;

        HasIce = true;
        return true;
    }

    /// <summary>
    /// Moves every actual liquid entry to a different container. Ingredient
    /// amounts and DashCount move together; ice deliberately remains with the
    /// source because this is a liquid-only operation.
    /// </summary>
    public bool TryTransferAllLiquidTo(BartendingContainerState target, out float transferredMl)
    {
        transferredMl = 0f;

        if (target == null || ReferenceEquals(this, target) || CurrentLiquidVolumeMl <= 0f)
            return false;

        if (target.CapacityMl > 0f && target.CurrentLiquidVolumeMl + CurrentLiquidVolumeMl > target.CapacityMl)
            return false;

        foreach (BartendingLiquidEntry entry in liquidEntries)
        {
            if (entry == null || entry.Ingredient == null || !IsFinitePositive(entry.AmountMl))
                return false;
        }

        foreach (BartendingLiquidEntry entry in liquidEntries)
        {
            float acceptedMl = target.AddLiquid(entry.Ingredient, entry.AmountMl);
            if (!Mathf.Approximately(acceptedMl, entry.AmountMl))
                return false;

            target.AddDashCount(entry.Ingredient, entry.DashCount);
        }

        transferredMl = CurrentLiquidVolumeMl;
        liquidEntries.Clear();
        CurrentLiquidVolumeMl = 0f;
        return true;
    }

    /// <summary>
    /// I3's non-scored strain operation. Only a Shaker or Mixing Glass may
    /// strain into a Serving Glass. It transfers liquid while retaining source
    /// ice, including after the source becomes liquid-empty.
    /// </summary>
    public bool TryStrainTo(BartendingContainerState servingGlass, out float transferredMl)
    {
        transferredMl = 0f;

        if ((ContainerType != BartendingContainerType.Shaker &&
             ContainerType != BartendingContainerType.MixingGlass) ||
            servingGlass == null ||
            servingGlass.ContainerType != BartendingContainerType.ServingGlass)
            return false;

        return TryTransferAllLiquidTo(servingGlass, out transferredMl);
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

    private void AddDashCount(IngredientData ingredient, int dashCount)
    {
        if (dashCount <= 0)
            return;

        BartendingLiquidEntry entry = FindEntry(ingredient);
        if (entry == null)
            return;

        for (int i = 0; i < dashCount && entry.DashCount < int.MaxValue; i++)
            entry.AddDash();
    }

    private static bool IsFinitePositive(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
    }
}
