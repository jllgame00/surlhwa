using UnityEngine;

[CreateAssetMenu(menuName = "SURLHWA/Campaign Encounter")]
public sealed class CampaignEncounterData : ScriptableObject
{
    public CampaignVisitId visit;
    public string guestName;
    public OrderData order;
    public GameObject customerPrefab;
    // onEnterLines are the common departure beat; success/fail lines respond to service.
    public DialogueData afterService;
    public bool stillness;
    public bool sharedGlass;
    public bool IsSpecial => visit != CampaignVisitId.None;

    public string[] GetDepartureLines(bool successfulService)
    {
        if (afterService == null) return System.Array.Empty<string>();
        var response = successfulService ? afterService.onSuccessLines : afterService.onFailLines;
        var common = afterService.onEnterLines;
        var lines = new System.Collections.Generic.List<string>();
        if (response != null) lines.AddRange(response);
        if (common != null) lines.AddRange(common);
        return lines.ToArray();
    }
}
