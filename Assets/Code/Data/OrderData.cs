using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Surlhwa/Order")]
public class OrderData : ScriptableObject
{
    public string orderName;
    public int reputationReward = 1;

    [Header("MiniGame")]
    public MiniGameType miniGameType;

    [Header("Cocktail Request Constraint")]
    public CocktailData requestedCocktail;

    [Header("Pre-Brew Dialogue")]
    [Tooltip("Optional authored dialogue consumed before this dedicated request begins.")]
    [SerializeField] private DialogueData preBrewDialogue;

    [Header("Recipe Knowledge Hint")]
    [TextArea]
    [Tooltip("Optional authored hint shown before this request begins. Facts apply only when this line is shown to the player.")]
    [SerializeField] private string recipeKnowledgeHintLine;
    [SerializeField] private RecipeKnowledgeReveal[] recipeKnowledgeHintReveals;

    public bool HasRecipeKnowledgeHint => !string.IsNullOrWhiteSpace(recipeKnowledgeHintLine);
    public DialogueData PreBrewDialogue => preBrewDialogue;
    public string RecipeKnowledgeHintLine => recipeKnowledgeHintLine ?? string.Empty;
    public RecipeKnowledgeReveal[] RecipeKnowledgeHintReveals =>
        recipeKnowledgeHintReveals ?? Array.Empty<RecipeKnowledgeReveal>();

}
