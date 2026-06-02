using UnityEngine;

[CreateAssetMenu(menuName = "SURLHWA/Ingredient")]
public class IngredientData : ScriptableObject
{
    [Header("Identity")]
    public string ingredientId;
    public string displayName;

    [Header("Category")]
    public IngredientCategory category;

    [Header("Visual")]
    public Sprite icon;
}