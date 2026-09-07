using System;

/// <summary>
/// The small I8 garnish identity set used by the runtime proof controls. This
/// is deliberately separate from IngredientData: garnish is not liquid input,
/// does not enter BrewComposition, and is not selected through the ingredient
/// shelf. I9 may extend this bounded vocabulary once adopted recipes require
/// it; it is not a garnish inventory or catalog system.
/// </summary>
public enum BartendingGarnishId
{
    LemonTwist,
    Cherry,
    Olive,
    OrangeSlice,
    LemonSlice,
    OrangeZest
}

/// <summary>
/// The only placement distinctions currently needed by the I8 foundation.
/// Rim treatments such as salt or sugar are intentionally not garnish
/// placements and remain deferred.
/// </summary>
public enum BartendingGarnishPlacement
{
    Rim,
    InGlass,
    Surface
}

/// <summary>
/// Immutable runtime truth for the one active garnish on a Serving Glass.
/// A later garnish replaces this state rather than accumulating an unbounded
/// decoration list. It carries no liquid quantity and has no persistence role.
/// </summary>
[Serializable]
public sealed class BartendingGarnishState
{
    public BartendingGarnishId GarnishId { get; }
    public BartendingGarnishPlacement Placement { get; }

    public string DisplayName => GetDisplayName(GarnishId);

    public BartendingGarnishState(
        BartendingGarnishId garnishId,
        BartendingGarnishPlacement placement)
    {
        GarnishId = garnishId;
        Placement = placement;
    }

    public static bool IsValid(BartendingGarnishId garnishId)
    {
        return garnishId >= BartendingGarnishId.LemonTwist &&
               garnishId <= BartendingGarnishId.OrangeZest;
    }

    public static bool IsValid(BartendingGarnishPlacement placement)
    {
        return placement >= BartendingGarnishPlacement.Rim &&
               placement <= BartendingGarnishPlacement.Surface;
    }

    public static string GetDisplayName(BartendingGarnishId garnishId)
    {
        switch (garnishId)
        {
            case BartendingGarnishId.LemonTwist:
                return "Lemon Twist";
            case BartendingGarnishId.Cherry:
                return "Cherry";
            case BartendingGarnishId.Olive:
                return "Olive";
            case BartendingGarnishId.OrangeSlice:
                return "Orange Slice";
            case BartendingGarnishId.LemonSlice:
                return "Lemon Slice";
            case BartendingGarnishId.OrangeZest:
                return "Orange Zest";
            default:
                return "Unknown Garnish";
        }
    }
}
