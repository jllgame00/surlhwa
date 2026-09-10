using UnityEngine;
using System;

[CreateAssetMenu(menuName = "SURLHWA/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [TextArea]
    public string[] onEnterLines;

    [TextArea]
    public string[] onSuccessLines;

    [TextArea]
    public string[] onFailLines;

    [Header("Recipe Knowledge Reveals")]
    [Tooltip("Facts reveal only when the matching onEnterLines index is displayed to the player.")]
    [SerializeField] private DialogueRecipeKnowledgeReveal[] onEnterKnowledgeReveals;

    public RecipeKnowledgeReveal[] GetOnEnterKnowledgeReveals(int lineIndex)
    {
        if (lineIndex < 0 || onEnterKnowledgeReveals == null)
            return Array.Empty<RecipeKnowledgeReveal>();

        foreach (DialogueRecipeKnowledgeReveal entry in onEnterKnowledgeReveals)
        {
            if (entry != null && entry.LineIndex == lineIndex)
                return entry.Reveals;
        }

        return Array.Empty<RecipeKnowledgeReveal>();
    }
}
