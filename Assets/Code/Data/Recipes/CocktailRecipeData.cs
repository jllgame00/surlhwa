using UnityEngine;

[CreateAssetMenu(menuName = "SURLHWA/Cocktail Recipe")]
public class CocktailRecipeData : ScriptableObject
{
    [Header("Identity")]
    public string recipeId;
    public string displayName;
    [TextArea(2, 4)] public string description;

    [Header("Link")]
    public MiniGameType method;
    public CocktailData targetCocktail;

    [Header("Recipe")]
    public RecipeIngredientEntry[] ingredients;

    [Header("Discovery")]
    [Range(0f, 1f)] public float matchThreshold = 0.85f;
    public bool unlockedByDefault = false;
    public bool discoverable = true;
}

[System.Serializable]
public class RecipeIngredientEntry
{
    public string ingredientId;
    public IngredientCategory category;
    [Tooltip("Displayed to players as ounces (oz). Recipe matching may normalize this value and compare relative proportions.")]
    public float ratio;
}

public enum IngredientCategory
{
    BaseSpirit,
    Liqueur,
    Mixer,
    Garnish
}
