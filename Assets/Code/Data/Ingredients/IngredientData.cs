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
    [Tooltip("Presentation tint only; never ingredient identity or recipe truth.")]
    public Color liquidColor = new Color(.78f, .81f, .76f, .85f);
    public IngredientBottleShape bottleShape;

    [Header("Sorting")]
    public int sortOrder;
}

public enum IngredientBottleShape { Tall, Broad, Small }
