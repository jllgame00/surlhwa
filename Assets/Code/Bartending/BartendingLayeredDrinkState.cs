using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The bounded visual/technique companion to a Serving Glass's authoritative
/// liquid contents. Amounts enter this state only after the same transfer has
/// been accepted by BartendingContainerState; it records ordered additions and
/// adjacent-boundary quality, not a second cocktail composition authority.
/// </summary>
public sealed class BartendingLayeredDrinkState
{
    private readonly List<BartendingLayer> layers = new List<BartendingLayer>();
    private readonly List<BartendingLayerBoundary> boundaries = new List<BartendingLayerBoundary>();
    private BartendingLayer activeLayer;

    public IReadOnlyList<BartendingLayer> Layers => layers;
    public IReadOnlyList<BartendingLayerBoundary> Boundaries => boundaries;
    public BartendingLayer ActiveLayer => activeLayer;
    public bool HasActiveLayer => activeLayer != null;

    public bool TryBeginLayer(IngredientData ingredient)
    {
        if (ingredient == null || activeLayer != null)
            return false;

        activeLayer = new BartendingLayer(ingredient, layers.Count);
        layers.Add(activeLayer);
        return true;
    }

    public float AddToActiveLayer(float acceptedMl)
    {
        if (activeLayer == null || !IsFinitePositive(acceptedMl))
            return 0f;

        activeLayer.AddAmount(acceptedMl);
        return acceptedMl;
    }

    public bool TryCompleteActiveLayer(
        BartendingLayerBoundaryQuality? boundaryQuality,
        out BartendingLayer completedLayer)
    {
        completedLayer = activeLayer;
        if (activeLayer == null)
            return false;

        activeLayer = null;

        // A press that never transferred liquid is not a layer and cannot
        // create a boundary or a false visual record.
        if (!IsFinitePositive(completedLayer.AmountMl))
        {
            layers.Remove(completedLayer);
            completedLayer = null;
            return false;
        }

        if (completedLayer.OrderIndex > 0 && boundaryQuality.HasValue)
        {
            boundaries.Add(new BartendingLayerBoundary(
                completedLayer.OrderIndex - 1,
                completedLayer.OrderIndex,
                boundaryQuality.Value));
        }

        return true;
    }

    private static bool IsFinitePositive(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
    }
}

/// <summary>
/// One actual ordered liquid addition. Its amount is a per-layer allocation of
/// already accepted Serving Glass liquid, which is required to distinguish
/// repeated ingredients in separate layers without replacing container truth.
/// </summary>
public sealed class BartendingLayer
{
    public IngredientData Ingredient { get; }
    public int OrderIndex { get; }
    public float AmountMl { get; private set; }

    internal BartendingLayer(IngredientData ingredient, int orderIndex)
    {
        Ingredient = ingredient;
        OrderIndex = orderIndex;
    }

    internal void AddAmount(float amountMl)
    {
        AmountMl += Mathf.Max(0f, amountMl);
    }
}

/// <summary>
/// A boundary belongs to exactly one adjacent lower/upper layer pair. Later
/// clean additions therefore cannot overwrite an earlier damaged boundary.
/// </summary>
public sealed class BartendingLayerBoundary
{
    public int LowerLayerIndex { get; }
    public int UpperLayerIndex { get; }
    public BartendingLayerBoundaryQuality Quality { get; }

    internal BartendingLayerBoundary(
        int lowerLayerIndex,
        int upperLayerIndex,
        BartendingLayerBoundaryQuality quality)
    {
        LowerLayerIndex = lowerLayerIndex;
        UpperLayerIndex = upperLayerIndex;
        Quality = quality;
    }
}

public enum BartendingLayerBoundaryQuality
{
    Clean,
    Partial,
    Mixed
}
