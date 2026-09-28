using UnityEngine;

/// <summary>
/// The one authored narrative branch in I13. It is intentionally Little
/// Prince-specific rather than a generic dialogue graph or customer-memory
/// system.
/// </summary>
[CreateAssetMenu(menuName = "SURLHWA/Narrative/Little Prince Narrative")]
public sealed class LittlePrinceNarrativeData : ScriptableObject
{
    [Header("First Visit Choice")]
    [TextArea]
    [SerializeField] private string choicePrompt;
    [TextArea]
    [SerializeField] private string keepTheLightLabel;
    [TextArea]
    [SerializeField] private string seekTheStarLabel;
    [TextArea]
    [SerializeField] private string keepTheLightImmediateResponse;
    [TextArea]
    [SerializeField] private string seekTheStarImmediateResponse;

    [Header("Later Visit")]
    [SerializeField] private DialogueData keepTheLightFollowupDialogue;
    [SerializeField] private DialogueData seekTheStarFollowupDialogue;
    [SerializeField] private DialogueData legacyFollowupDialogue;

    [Header("Closing Visit")]
    [SerializeField] private DialogueData closingVisitDialogue;
    [SerializeField] private DialogueData openFarewellDialogue;
    [SerializeField] private DialogueData quietFarewellDialogue;

    [TextArea] [SerializeField] private string keepTheLightClosingRecall;
    [TextArea] [SerializeField] private string seekTheStarClosingRecall;
    [TextArea] [SerializeField] private string legacyClosingRecall;

    public string[] GetFarewellLines(LittlePrinceNarrativeChoice choice, bool open)
    {
        DialogueData dialogue = GetFarewellDialogue(open);
        if (dialogue == null || dialogue.onEnterLines == null || dialogue.onEnterLines.Length == 0)
            return System.Array.Empty<string>();
        string recall = choice switch
        {
            LittlePrinceNarrativeChoice.KeepTheLight => keepTheLightClosingRecall,
            LittlePrinceNarrativeChoice.SeekTheStar => seekTheStarClosingRecall,
            _ => legacyClosingRecall
        };
        string[] lines = new string[dialogue.onEnterLines.Length + 1];
        lines[0] = recall;
        System.Array.Copy(dialogue.onEnterLines, 0, lines, 1, dialogue.onEnterLines.Length);
        return lines;
    }

    public DialogueData ClosingVisitDialogue => closingVisitDialogue;
    public DialogueData GetFarewellDialogue(bool open) => open ? openFarewellDialogue : quietFarewellDialogue;

    public string ChoicePrompt => choicePrompt ?? string.Empty;

    public string GetChoiceLabel(LittlePrinceNarrativeChoice choice)
    {
        return choice switch
        {
            LittlePrinceNarrativeChoice.KeepTheLight => keepTheLightLabel ?? string.Empty,
            LittlePrinceNarrativeChoice.SeekTheStar => seekTheStarLabel ?? string.Empty,
            _ => string.Empty
        };
    }

    public string GetImmediateResponse(LittlePrinceNarrativeChoice choice)
    {
        return choice switch
        {
            LittlePrinceNarrativeChoice.KeepTheLight =>
                keepTheLightImmediateResponse ?? string.Empty,
            LittlePrinceNarrativeChoice.SeekTheStar =>
                seekTheStarImmediateResponse ?? string.Empty,
            _ => string.Empty
        };
    }

    public DialogueData GetFollowupDialogue(LittlePrinceNarrativeChoice choice)
    {
        return choice switch
        {
            LittlePrinceNarrativeChoice.KeepTheLight => keepTheLightFollowupDialogue,
            LittlePrinceNarrativeChoice.SeekTheStar => seekTheStarFollowupDialogue,
            _ => legacyFollowupDialogue
        };
    }
}
