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

    // Gameplay compatibility value for the first Dash proof. This is not a
    // claim about a universal real-world dash size; later authored content may
    // tune it through a narrow recipe/preparation seam without touching the
    // legacy matcher.
    public const float DefaultDashMilliliters = 1f;

    // I9B gameplay tuning for Gin Fizz. This deliberately is not an official
    // IBA conversion: the official recipe calls for a qualitative splash.
    public const float MinimumSplashMilliliters = 10f;
    public const float DefaultSplashMilliliters = 15f;
    public const float MaximumSplashMilliliters = 20f;

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

    public static float DashesToMilliliters(int dashCount, float millilitersPerDash = DefaultDashMilliliters)
    {
        if (dashCount <= 0 || float.IsNaN(millilitersPerDash) || float.IsInfinity(millilitersPerDash))
            return 0f;

        return dashCount * Mathf.Max(0f, millilitersPerDash);
    }

    public static bool IsValidSplashMilliliters(float milliliters)
    {
        return !float.IsNaN(milliliters) && !float.IsInfinity(milliliters) &&
               milliliters >= MinimumSplashMilliliters &&
               milliliters <= MaximumSplashMilliliters;
    }
}
