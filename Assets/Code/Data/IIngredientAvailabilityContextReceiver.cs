/// <summary>
/// Receives the transient Bar-side availability context before a Bartending
/// minigame starts. It does not expose recipe knowledge to the shelf.
/// </summary>
public interface IIngredientAvailabilityContextReceiver
{
    void SetIngredientAvailabilityContext(IngredientAvailabilityContext context);
}
