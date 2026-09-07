using UnityEngine;

[CreateAssetMenu(menuName = "Surlhwa/Cocktail")]
public class CocktailData : ScriptableObject
{
    [Header("Identity")]
    public string cocktailId;

    [Header("Basic Info")]
    public string cocktailName;

    [Header("Presentation Metadata")]
    [Tooltip("I9B content metadata only. Players do not choose or fail glassware in this phase.")]
    public CocktailGlassware glassware;

    [TextArea(2, 5)]
    public string description;

    [Header("Emotion Tags")]
    public EmotionTag[] emotionTags;

    [Header("Visual")]
    public Sprite cocktailSprite;
}

/// <summary>
/// Bounded presentation metadata for the initial IBA validation set. It is not
/// a serving-glass gameplay selection or matching axis.
/// </summary>
public enum CocktailGlassware
{
    Unspecified,
    CocktailGlass,
    OldFashionedGlass,
    TallTumbler,
    ChampagneFlute
}
