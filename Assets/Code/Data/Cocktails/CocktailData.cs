using UnityEngine;

[CreateAssetMenu(menuName = "Surlhwa/Cocktail")]
public class CocktailData : ScriptableObject
{
    [Header("Basic Info")]
    public string cocktailName;

    [TextArea(2, 5)]
    public string description;

    [Header("Recipe / Play")]
    public MiniGameType miniGameType;
    public RecipeStep[] steps;

    [Header("Emotion Tags")]
    public EmotionTag[] emotionTags;

    [Header("Visual")]
    public Sprite cocktailSprite;
}