using UnityEngine;

/// <summary>
/// Unit conversion at the temporary Bartending 2.0-to-legacy matching boundary.
/// Bartending container state is always stored in milliliters. The existing recipe
/// matcher still consumes authored ounce amounts until recipe data is migrated by
/// a later, separately validated phase.
/// </summary>
public static class BartendingVolume
{
    public const float MillilitersPerFluidOunce = 29.57353f;

    public static float OuncesToMilliliters(float ounces)
    {
        if (float.IsNaN(ounces) || float.IsInfinity(ounces))
            return 0f;

        return Mathf.Max(0f, ounces) * MillilitersPerFluidOunce;
    }

    public static float MillilitersToOunces(float milliliters)
    {
        if (float.IsNaN(milliliters) || float.IsInfinity(milliliters))
            return 0f;

        return Mathf.Max(0f, milliliters) / MillilitersPerFluidOunce;
    }
}
