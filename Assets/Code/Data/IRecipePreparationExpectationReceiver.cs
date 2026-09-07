/// <summary>
/// Narrow session setup seam: routes that support an order-specific recipe
/// expectation receive it before StartGame. It does not make a recipe mutate
/// or seed runtime preparation state.
/// </summary>
public interface IRecipePreparationExpectationReceiver
{
    void SetExpectedRecipe(CocktailRecipeData recipe);
}
