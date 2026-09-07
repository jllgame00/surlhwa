using UnityEngine;

/// <summary>
/// Runtime recipe guidance for a bounded TopUp action. It deliberately stores
/// an intended final volume, not a glass capacity or an automatically inserted
/// amount. The selected liquid is still transferred only through Pour Core.
/// </summary>
public sealed class BartendingTopUpGuidance
{
    public IngredientData Ingredient { get; }
    public float TargetFinalVolumeMl { get; }

    public bool IsValid => Ingredient != null && IsFinitePositive(TargetFinalVolumeMl);

    public BartendingTopUpGuidance(IngredientData ingredient, float targetFinalVolumeMl)
    {
        Ingredient = ingredient;
        TargetFinalVolumeMl = IsFinitePositive(targetFinalVolumeMl) ? targetFinalVolumeMl : 0f;
    }

    public bool AppliesTo(IngredientData ingredient)
    {
        return IsValid && ingredient != null && ingredient == Ingredient;
    }

    /// <summary>
    /// Positive means the player remains below the recipe's guidance target;
    /// negative means the player has actually over-poured. No state is changed.
    /// </summary>
    public float GetSignedRemainingMl(float currentLiquidVolumeMl)
    {
        float safeCurrentMl = IsFinitePositive(currentLiquidVolumeMl) ? currentLiquidVolumeMl : 0f;
        return TargetFinalVolumeMl - safeCurrentMl;
    }

    private static bool IsFinitePositive(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
    }
}
