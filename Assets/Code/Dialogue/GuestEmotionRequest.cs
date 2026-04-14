using UnityEngine;

[System.Serializable]
public class GuestEmotionRequest
{
    public EmotionTag primaryTag;
    public EmotionTag secondaryTag;

    public EmotionTag[] ToArray()
    {
        return new[] { primaryTag, secondaryTag };
    }

    public void Set(EmotionTag primary, EmotionTag secondary)
    {
        primaryTag = primary;
        secondaryTag = secondary;
    }
}