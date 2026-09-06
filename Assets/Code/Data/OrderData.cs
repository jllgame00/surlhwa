using UnityEngine;

[CreateAssetMenu(menuName = "Surlhwa/Order")]
public class OrderData : ScriptableObject
{
    public string orderName;
    public int reputationReward = 1;

    [Header("MiniGame")]
    public MiniGameType miniGameType;

    [Header("Cocktail")]
    public CocktailData targetCocktail;

}
