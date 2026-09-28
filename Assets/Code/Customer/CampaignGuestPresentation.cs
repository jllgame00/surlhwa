using UnityEngine;

/// <summary>Encounter-local draft staging. Never changes time, input, audio or customer state.</summary>
public sealed class CampaignGuestPresentation : MonoBehaviour
{
    public bool HasCompanion { get; private set; }
    public bool HasStillness { get; private set; }
    private GameObject glass;
    private Material lineMaterial;

    public void Configure(CampaignEncounterData encounter)
    {
        var body = GetComponent<SpriteRenderer>();
        if (body == null || !encounter.IsSpecial) return;
        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        // Reuse the approved draft silhouette; authored names/dialogue carry identity.
        body.color = encounter.stillness ? new Color(.62f, .65f, .72f) : Color.white;
        if (encounter.sharedGlass || encounter.visit == CampaignVisitId.LoversDeparture)
        {
            var companion = new GameObject("Mongryong companion").AddComponent<SpriteRenderer>();
            companion.transform.SetParent(transform, false);
            companion.transform.localPosition = Vector3.right * .72f;
            companion.sprite = body.sprite;
            companion.sharedMaterial = body.sharedMaterial;
            companion.color = new Color(.72f, .83f, .88f);
            companion.sortingOrder = body.sortingOrder;
            companion.flipX = true;
            HasCompanion = true;
        }
        if (encounter.stillness)
        {
            var light = new GameObject("Still pool of light");
            light.transform.SetParent(transform, false);
            light.transform.localPosition = Vector3.up * 1.5f;
            CampaignBarMemories.DrawLine(light.transform, lineMaterial, new Color(.56f, .65f, .78f, .45f), .05f,
                -.6f, -.35f, 0f, -.5f, .6f, -.35f);
            HasStillness = true;
        }
        if (encounter.sharedGlass)
        {
            glass = new GameObject("One shared glass");
            glass.transform.SetParent(transform, false);
            glass.transform.localPosition = new Vector3(.36f, 1.05f, 0);
            CampaignBarMemories.DrawLine(glass.transform, lineMaterial, new Color(.86f, .75f, .58f), .035f,
                -.13f, .22f, -.09f, 0f, .09f, 0f, .13f, .22f, -.13f, .22f);
            glass.SetActive(false);
        }
    }

    public void ShowServedGlass() { if (glass != null) glass.SetActive(true); }
    private void OnDestroy() { if (lineMaterial != null) Destroy(lineMaterial); }
}
