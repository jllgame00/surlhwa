using System;
using UnityEngine;

/// <summary>
/// Bounded authored evidence attached to a consumed dialogue or request hint.
/// It carries only typed recipe facts; it is not a general event or dialogue
/// scripting system. RecipeUnlockManager remains the knowledge authority.
/// </summary>
[Serializable]
public sealed class RecipeKnowledgeReveal
{
    [SerializeField] private CocktailRecipeData recipe;
    [SerializeField] private RecipeKnowledgeFact fact;
    [SerializeField] private IngredientData ingredient;

    public CocktailRecipeData Recipe => recipe;
    public RecipeKnowledgeFact Fact => fact;
    public IngredientData Ingredient => ingredient;

    public bool TryApply(RecipeUnlockManager knowledgeManager)
    {
        if (knowledgeManager == null || recipe == null)
            return false;

        return fact switch
        {
            RecipeKnowledgeFact.Identity => knowledgeManager.LearnIdentity(recipe),
            RecipeKnowledgeFact.Method => knowledgeManager.LearnMethod(recipe),
            RecipeKnowledgeFact.Ingredient => knowledgeManager.LearnIngredient(recipe, ingredient),
            RecipeKnowledgeFact.IngredientAmount => knowledgeManager.LearnIngredientAmount(recipe, ingredient),
            RecipeKnowledgeFact.Garnish => knowledgeManager.LearnGarnish(recipe),
            RecipeKnowledgeFact.Preparation => knowledgeManager.LearnPreparation(recipe),
            _ => ReportUnsupportedFact()
        };
    }

    private bool ReportUnsupportedFact()
    {
        Debug.LogWarning(
            "[RecipeKnowledge] Ignored authored reveal with an unsupported fact for '" +
            (recipe != null ? recipe.recipeId : "NULL") + "'.");
        return false;
    }
}

/// <summary>
/// Associates one or more bounded recipe facts with an exact consumed
/// DialogueData.onEnterLines index.
/// </summary>
[Serializable]
public sealed class DialogueRecipeKnowledgeReveal
{
    [Min(0)] [SerializeField] private int lineIndex;
    [SerializeField] private RecipeKnowledgeReveal[] reveals;

    public int LineIndex => lineIndex;
    public RecipeKnowledgeReveal[] Reveals => reveals ?? Array.Empty<RecipeKnowledgeReveal>();
}

/// <summary>
/// Session-local bridge from accepted shelf liquid actions to the single
/// knowledge authority. It is deliberately inert without a request-owned
/// target recipe and never owns preparation or composition state.
/// </summary>
public static class RecipeKnowledgeIngredientUseDiscovery
{
    public static void Bind(
        IngredientShelfController shelf,
        CocktailRecipeData targetRecipe,
        RecipeUnlockManager knowledgeManager)
    {
        if (shelf == null || targetRecipe == null || knowledgeManager == null)
            return;

        shelf.IngredientUsed += ingredient =>
            knowledgeManager.LearnIngredientFromPlayerUse(targetRecipe, ingredient);
    }
}
