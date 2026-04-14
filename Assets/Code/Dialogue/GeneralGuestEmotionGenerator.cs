using System.Collections.Generic;
using UnityEngine;

public static class GeneralGuestEmotionGenerator
{
    private static readonly EmotionTag[] PrimaryPool =
    {
        EmotionTag.Loneliness,
        EmotionTag.Comfort,
        EmotionTag.Awakening,
        EmotionTag.Softness,
        EmotionTag.Bitterness,
        EmotionTag.Heavy,
        EmotionTag.Clarity,
        EmotionTag.Warmth,
        EmotionTag.Regret,
        EmotionTag.Longing
    };

    private static readonly Dictionary<EmotionTag, EmotionTag[]> SecondaryPoolMap =
        new Dictionary<EmotionTag, EmotionTag[]>
        {
            { EmotionTag.Loneliness, new[] { EmotionTag.Comfort, EmotionTag.Softness, EmotionTag.Longing } },
            { EmotionTag.Comfort,    new[] { EmotionTag.Loneliness, EmotionTag.Softness, EmotionTag.Warmth } },
            { EmotionTag.Awakening,  new[] { EmotionTag.Clarity, EmotionTag.Bitterness, EmotionTag.Heavy } },
            { EmotionTag.Softness,   new[] { EmotionTag.Comfort, EmotionTag.Warmth, EmotionTag.Loneliness } },
            { EmotionTag.Bitterness, new[] { EmotionTag.Regret, EmotionTag.Heavy, EmotionTag.Awakening } },
            { EmotionTag.Heavy,      new[] { EmotionTag.Warmth, EmotionTag.Bitterness, EmotionTag.Regret } },
            { EmotionTag.Clarity,    new[] { EmotionTag.Awakening, EmotionTag.Softness, EmotionTag.Comfort } },
            { EmotionTag.Warmth,     new[] { EmotionTag.Comfort, EmotionTag.Heavy, EmotionTag.Softness } },
            { EmotionTag.Regret,     new[] { EmotionTag.Bitterness, EmotionTag.Longing, EmotionTag.Comfort } },
            { EmotionTag.Longing,    new[] { EmotionTag.Loneliness, EmotionTag.Regret, EmotionTag.Softness } },
        };

    public static void FillRandom(GuestEmotionRequest target, EmotionTag previousPrimaryTag)
    {
        if (target == null) return;

        EmotionTag primary = GetRandomPrimary(previousPrimaryTag);
        EmotionTag secondary = GetRandomSecondary(primary);

        target.Set(primary, secondary);
    }

    private static EmotionTag GetRandomPrimary(EmotionTag previousPrimaryTag)
    {
        List<EmotionTag> candidates = new List<EmotionTag>();

        for (int i = 0; i < PrimaryPool.Length; i++)
        {
            EmotionTag tag = PrimaryPool[i];

            if (tag == previousPrimaryTag)
                continue;

            candidates.Add(tag);
        }

        if (candidates.Count == 0)
            candidates.AddRange(PrimaryPool);

        int index = Random.Range(0, candidates.Count);
        return candidates[index];
    }

    private static EmotionTag GetRandomSecondary(EmotionTag primary)
    {
        if (!SecondaryPoolMap.ContainsKey(primary))
            return primary;

        EmotionTag[] pool = SecondaryPoolMap[primary];

        if (pool == null || pool.Length == 0)
            return primary;

        int index = Random.Range(0, pool.Length);
        return pool[index];
    }
}