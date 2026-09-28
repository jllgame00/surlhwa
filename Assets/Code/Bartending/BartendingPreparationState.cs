/// <summary>
/// Runtime-only container set for one preparation session. This is deliberately
/// a small domain value, not a global manager or a recipe scripting framework:
/// I3 needs exactly one Serving Glass, Mixing Glass, and Shaker at a time.
/// </summary>
public sealed class BartendingPreparationState
{
    private readonly BartendingContainerState servingGlass;
    private readonly BartendingContainerState mixingGlass;
    private readonly BartendingContainerState shaker;
    private readonly BartendingContainerState jigger;

    public BartendingContainerType ActiveTargetType { get; private set; }
    public BartendingContainerState ActiveTarget => GetContainer(ActiveTargetType);

    public BartendingPreparationState(
        BartendingContainerType initialTargetType = BartendingContainerType.ServingGlass,
        float jiggerCapacityMl = 60f)
    {
        servingGlass = new BartendingContainerState(BartendingContainerType.ServingGlass);
        mixingGlass = new BartendingContainerState(BartendingContainerType.MixingGlass);
        shaker = new BartendingContainerState(BartendingContainerType.Shaker);
        jigger = new BartendingContainerState(BartendingContainerType.Jigger,
            float.IsNaN(jiggerCapacityMl) || float.IsInfinity(jiggerCapacityMl) || jiggerCapacityMl <= 0f
                ? 60f : jiggerCapacityMl);
        ActiveTargetType = GetContainer(initialTargetType) != null
            ? initialTargetType
            : BartendingContainerType.ServingGlass;
    }

    public BartendingContainerState GetContainer(BartendingContainerType containerType)
    {
        switch (containerType)
        {
            case BartendingContainerType.ServingGlass:
                return servingGlass;
            case BartendingContainerType.MixingGlass:
                return mixingGlass;
            case BartendingContainerType.Shaker:
                return shaker;
            case BartendingContainerType.Jigger:
                return jigger;
            default:
                return null;
        }
    }

    public bool TrySetActiveTarget(BartendingContainerType containerType)
    {
        if (GetContainer(containerType) == null)
            return false;

        ActiveTargetType = containerType;
        return true;
    }

    public bool TryAddIceToActiveTarget()
    {
        BartendingContainerState target = ActiveTarget;
        return target != null && target.TryAddIce();
    }

    public bool TryTransferAllLiquid(
        BartendingContainerType sourceType,
        BartendingContainerType targetType,
        out float transferredMl)
    {
        transferredMl = 0f;
        BartendingContainerState source = GetContainer(sourceType);
        BartendingContainerState target = GetContainer(targetType);
        return source != null && source.TryTransferAllLiquidTo(target, out transferredMl);
    }

    public bool TryStrainToServingGlass(BartendingContainerType sourceType, out float transferredMl)
    {
        transferredMl = 0f;
        BartendingContainerState source = GetContainer(sourceType);
        return source != null && source.TryStrainTo(servingGlass, out transferredMl);
    }
}
