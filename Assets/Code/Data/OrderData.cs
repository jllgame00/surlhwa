using UnityEngine;

[CreateAssetMenu(menuName = "Surlhwa/Order")]
public class OrderData : ScriptableObject
{
    public string orderName;
    public int reputationReward = 1;

    [Header("MiniGame")]
    public MiniGameType miniGameType;

    [Header("Recipe")]
    public RecipeStep[] steps;

    [Header("Cocktail")]
    public CocktailData targetCocktail;

    [Header("Brew Session")]
    public BrewSessionData defaultBrewSession;
}