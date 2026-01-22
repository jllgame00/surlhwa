using UnityEngine;

[CreateAssetMenu(menuName = "Surlhwa/Order")]
public class OrderData : ScriptableObject
{
    public string orderName;
    public int reputationReward = 1;

    public MiniGameType miniGameType;
    public RecipeStep[] steps;
}
