using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class GuestReactionLine
{
    [TextArea(2, 4)]
    public string text;

    public ReactionPhase phase;

    [Range(0, 3)]
    public int heartCount;

    public EmotionTag[] tags;
}

[System.Serializable]
public class GuestFitReactionLine
{
    [TextArea(2, 4)]
    public string text;

    public GuestReactionResolver.GuestResponseTone tone;
}

[CreateAssetMenu(menuName = "Surlhwa/Dialogue/Guest Reaction Database")]
public class GuestReactionDatabase : ScriptableObject
{
    public List<GuestReactionLine> lines = new();
    public List<GuestFitReactionLine> guestFitLines = new();

    public string GetRandomLine(EmotionTag[] desiredTags, int heartCount, ReactionPhase phase)
    {
        if (lines == null || lines.Count == 0)
            return "...";

        desiredTags ??= System.Array.Empty<EmotionTag>();

        var exact = lines.Where(line =>
            line.phase == phase &&
            line.heartCount == heartCount &&
            line.tags != null &&
            line.tags.Any(tag => desiredTags.Contains(tag))
        ).ToList();

        if (exact.Count > 0)
            return exact[Random.Range(0, exact.Count)].text;

        var sameHeart = lines.Where(line =>
            line.phase == phase &&
            line.heartCount == heartCount
        ).ToList();

        if (sameHeart.Count > 0)
            return sameHeart[Random.Range(0, sameHeart.Count)].text;

        var samePhase = lines.Where(line =>
            line.phase == phase
        ).ToList();

        if (samePhase.Count > 0)
            return samePhase[Random.Range(0, samePhase.Count)].text;

        return "...";
    }

    public string GetGuestFitLine(GuestReactionResolver.GuestResponseTone tone)
    {
        if (guestFitLines == null)
            return string.Empty;

        List<GuestFitReactionLine> matches = guestFitLines.Where(line =>
            line != null && line.tone == tone && !string.IsNullOrWhiteSpace(line.text)).ToList();
        if (matches.Count == 0)
            return string.Empty;

        return matches[Random.Range(0, matches.Count)].text;
    }
}
