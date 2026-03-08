using UnityEngine;

[CreateAssetMenu(menuName = "SURLHWA/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [TextArea]
    public string[] onEnterLines;

    [TextArea]
    public string[] onSuccessLines;

    [TextArea]
    public string[] onFailLines;
}