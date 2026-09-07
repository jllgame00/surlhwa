using UnityEngine;

[CreateAssetMenu(menuName = "SURLHWA/Cocktail Recipe")]
public class CocktailRecipeData : ScriptableObject
{
    [Header("Identity")]
    public string recipeId;

    [Header("Link")]
    public MiniGameType method;
    public CocktailData targetCocktail;

    [Header("Recipe")]
    public RecipeIngredientEntry[] ingredients;

    [Header("Expected Preparation (I9B)")]
    [Tooltip("Typed, recipe-owned action order. Liquid amount matching remains owned by RecipeMatchService.")]
    public RecipePreparationStep[] expectedPreparation;

    [Header("Discovery")]
    [Range(0f, 1f)] public float matchThreshold = 0.85f;
    public bool discoverable = true;
}

[System.Serializable]
public class RecipeIngredientEntry
{
    public IngredientData ingredient;
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
