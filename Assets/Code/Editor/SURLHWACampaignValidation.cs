#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SURLHWACampaignValidation
{
    public const string SchedulePath = "Assets/_SURLHWA/Data/Campaign/Campaign.asset";

    public static IEnumerable<string> ProgressKeys()
    {
        yield return CampaignProgressPersistence.DayKey;
        yield return CampaignProgressPersistence.SlotKey;
        yield return CampaignProgressPersistence.EndingKey;
        foreach (CampaignVisitId visit in Enum.GetValues(typeof(CampaignVisitId)))
            if (visit != CampaignVisitId.None) yield return CampaignProgressPersistence.VisitKey(visit);
    }

    [MenuItem("SURLHWA/Validation/Campaign Persistence")]
    public static void RunInfrastructure()
    {
        var saved = new Dictionary<string, int?>();
        foreach (string key in ProgressKeys()) saved[key] = SurlhwaPlayerPrefsStore.HasKey(key)
            ? SurlhwaPlayerPrefsStore.GetInt(key, 0) : null;
        try
        {
            CampaignProgressPersistence.Clear();
            Require(!CampaignProgressPersistence.HasProgress, "Reset must clear campaign checkpoint.");
            Require(!CampaignProgressPersistence.IsCompleted(CampaignVisitId.LoversReunion), "Legacy must not invent ACT completion.");
            CampaignProgressPersistence.RecordDeparture(7, 2, CampaignVisitId.LoversReunion);
            Require(CampaignProgressPersistence.IsCompleted(CampaignVisitId.LoversReunion), "Departure completion lost.");
            Require(CampaignProgressPersistence.LoadSlot(7, 3) == 2 && CampaignProgressPersistence.LoadSlot(8, 3) == 0,
                "Checkpoint must resume the same night only.");
            SurlhwaPlayerPrefsStore.SetInt(CampaignProgressPersistence.SlotKey, -4);
            Require(CampaignProgressPersistence.LoadSlot(7, 3) == 0, "Invalid negative checkpoint.");
            SurlhwaPlayerPrefsStore.SetInt(CampaignProgressPersistence.SlotKey, 999);
            Require(CampaignProgressPersistence.LoadSlot(7, 3) == 3, "Checkpoint exceeds night.");
            Require(!CampaignProgressPersistence.EndingRead, "A visit must not complete the ending.");
            CampaignProgressPersistence.MarkEndingRead();
            Require(CampaignProgressPersistence.EndingRead, "Ending read not retained.");
            CampaignProgressPersistence.Clear();
            foreach (string key in ProgressKeys()) Require(!SurlhwaPlayerPrefsStore.HasKey(key), "Reset left " + key);
            Debug.Log("[Campaign Persistence] PASS: departure checkpoint, legacy defaults, malformed slot, ending/read and reset.");
        }
        finally
        {
            foreach (var pair in saved)
                if (pair.Value.HasValue) SurlhwaPlayerPrefsStore.SetInt(pair.Key, pair.Value.Value);
                else SurlhwaPlayerPrefsStore.DeleteKey(pair.Key);
            SurlhwaPlayerPrefsStore.Save();
        }
    }

    public static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    [MenuItem("SURLHWA/Validation/Campaign Authored ACTs")]
    public static void RunAuthoredActs()
    {
        var ids = new HashSet<CampaignVisitId>();
        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:CampaignEncounterData", new[] { "Assets/_SURLHWA/Data/Campaign" }))
        {
            var encounter = AssetDatabase.LoadAssetAtPath<CampaignEncounterData>(AssetDatabase.GUIDToAssetPath(guid));
            Require(encounter != null && encounter.order != null && !string.IsNullOrWhiteSpace(encounter.guestName), "Incomplete encounter.");
            Require(encounter.order.PreBrewDialogue != null && encounter.order.PreBrewDialogue.onEnterLines.Length >= 2,
                "Missing authored entrance: " + encounter.name);
            if (!encounter.IsSpecial) continue;
            count++;
            Require(ids.Add(encounter.visit), "Duplicate visit identity.");
            Require(encounter.customerPrefab != null && encounter.customerPrefab.GetComponent<CustomerController>() != null,
                "Missing guest runtime: " + encounter.name);
            Require(encounter.order.RequestType == GuestRequestType.Recommendation && encounter.order.requestedCocktail == null &&
                encounter.order.DesiredEmotionTags.Length > 0 && !encounter.order.RequiresSuccessfulDiscovery,
                "New ACT must preserve interpretation without forced success: " + encounter.name);
            Require(encounter.afterService != null && encounter.afterService.onEnterLines.Length >= 2 &&
                encounter.afterService.onSuccessLines.Length > 0 && encounter.afterService.onFailLines.Length > 0,
                "Missing service/departure alternatives: " + encounter.name);
            if (encounter.visit == CampaignVisitId.LoversReunion) Require(encounter.sharedGlass, "Reunion must share one glass.");
        }
        Require(count > 0, "No authored main ACTs found.");
        Debug.Log("[Campaign ACTs] PASS: " + count + " authored visits, unique IDs, prefab/request/dialogue references and both service paths.");
    }
}
#endif
