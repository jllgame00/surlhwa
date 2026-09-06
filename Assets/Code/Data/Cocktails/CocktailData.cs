using UnityEngine;

[CreateAssetMenu(menuName = "Surlhwa/Cocktail")]
public class CocktailData : ScriptableObject
{
    [Header("Identity")]
    public string cocktailId;

    [Header("Basic Info")]
    public string cocktailName;

    [TextArea(2, 5)]
    public string description;

    [Header("Emotion Tags")]
    public EmotionTag[] emotionTags;

    [Header("Visual")]
    public Sprite cocktailSprite;
}
