using System;
using UnityEngine;

// Append only: values are persisted identities, independent of asset names/order.
public enum CampaignVisitId
{
    None = 0,
    ChunhyangMeeting = 1, MongryongMeeting = 2, LoversDeparture = 3,
    ChunhyangSilence = 4, LoversReunion = 5,
    QuixoteDeclaration = 6, QuixoteAlone = 7,
    HamletQuestion = 8, HamletStillness = 9,
    FaustContract = 10, FaustCost = 11,
    SimCheongBefore = 12, SimCheongResolve = 13, SimCheongReturn = 14,
    OdysseusReturn = 15, OdysseusStay = 16
}

[CreateAssetMenu(menuName = "SURLHWA/Campaign Schedule")]
public sealed class CampaignData : ScriptableObject
{
    public const int FirstExtendedNight = 5;
    public CampaignNight[] nights = Array.Empty<CampaignNight>();
    public DialogueData ending;
    public int FinalNight => FirstExtendedNight + nights.Length - 1;

    public CampaignNight GetNight(int day)
    {
        int index = day - FirstExtendedNight;
        return index >= 0 && index < nights.Length ? nights[index] : null;
    }

    public CampaignEncounterData GetEncounter(int day, int slot)
    {
        var night = GetNight(day);
        return night != null && slot >= 0 && slot < night.encounters.Length
            ? night.encounters[slot] : null;
    }
}

[Serializable]
public sealed class CampaignNight
{
    public CampaignEncounterData[] encounters = Array.Empty<CampaignEncounterData>();
}
