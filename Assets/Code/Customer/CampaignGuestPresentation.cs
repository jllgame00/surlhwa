using UnityEngine;

/// <summary>Encounter-local visual staging. Never changes time, input, audio or customer state.</summary>
public sealed class CampaignGuestPresentation : MonoBehaviour
{
    public bool HasCompanion { get; private set; }
    public bool HasStillness { get; private set; }
    private GameObject glass;
    private Material lineMaterial;
    private SpriteRenderer companionBody;
    private SpriteRenderer body;
    private CustomerMotionPresentation companionMotion;
    private GameObject companionStool;
    private CustomerController customer;
    private LineRenderer[] glassContours;
    private LineRenderer[] stillnessContours;

    public void Configure(CampaignEncounterData encounter)
    {
        body = GetComponent<SpriteRenderer>();
        customer = GetComponent<CustomerController>();
        if (body == null || !encounter.IsSpecial) return;
        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        if (encounter.characterSprite != null)
        {
            body.sprite = encounter.characterSprite;
            if (customer != null) customer.standSprite = customer.sitSprite = encounter.characterSprite;
        }
        body.color = encounter.stillness ? new Color(.62f, .65f, .72f) : Color.white;
        if (encounter.sharedGlass || encounter.visit == CampaignVisitId.LoversDeparture)
        {
            var companion = new GameObject("Mongryong companion").AddComponent<SpriteRenderer>();
            companion.transform.SetParent(transform, false);
            companion.transform.localPosition = Vector3.right * 1.5f;
            companion.sprite = encounter.companionSprite != null ? encounter.companionSprite : body.sprite;
            companion.sharedMaterial = body.sharedMaterial;
            companion.color = Color.white;
            companion.sortingOrder = body.sortingOrder;
            companion.flipX = true;
            companionBody = companion;
            companionMotion = companion.gameObject.AddComponent<CustomerMotionPresentation>();
            companionMotion.Configure(customer, companion.sprite);
            var art = Resources.Load<CustomerMotionLibrary>("CustomerMotionLibrary");
            if (art != null && art.companionStool != null)
            {
                var stool = new GameObject("Companion stool").AddComponent<SpriteRenderer>();
                stool.transform.SetParent(transform, false);
                stool.transform.localPosition = new Vector3(1.5f, .75f, 0);
                stool.sprite = art.companionStool;
                stool.sharedMaterial = body.sharedMaterial;
                stool.sortingOrder = 200;
                companionStool = stool.gameObject;
                companionStool.SetActive(false);
            }
            HasCompanion = true;
        }
        if (encounter.stillness)
        {
            var light = new GameObject("Still pool of light");
            light.transform.SetParent(transform, false);
            light.transform.localPosition = Vector3.up * .35f;
            CampaignBarMemories.DrawLine(light.transform, lineMaterial, new Color(.56f, .65f, .78f, .45f), .05f,
                -.6f, -.35f, 0f, -.5f, .6f, -.35f);
            stillnessContours = light.GetComponentsInChildren<LineRenderer>();
            HasStillness = true;
        }
        if (encounter.sharedGlass)
        {
            glass = new GameObject("One shared glass");
            glass.transform.SetParent(transform, false);
            glass.transform.localPosition = new Vector3(.75f, 2.05f, 0);
            CampaignBarMemories.DrawLine(glass.transform, lineMaterial, new Color(.86f, .75f, .58f), .035f,
                -.13f, .22f, -.09f, 0f, .09f, 0f, .13f, .22f, -.13f, .22f);
            glassContours = glass.GetComponentsInChildren<LineRenderer>();
            glass.SetActive(false);
        }
    }

    public void ShowServedGlass() { if (glass != null) glass.SetActive(true); }
    public void ReactToService()
    {
        companionMotion?.React();
        ShowServedGlass();
    }
    private void LateUpdate()
    {
        // The existing customer owns feet-based depth, including during departure.
        if (body == null) return;
        bool atSeat = customer != null && (customer.CurrentState == CustomerController.State.Turning ||
            customer.CurrentState == CustomerController.State.Sitting || customer.CurrentState == CustomerController.State.Ordering ||
            customer.CurrentState == CustomerController.State.Waiting || customer.CurrentState == CustomerController.State.Standing);
        if (companionStool != null && customer != null && customer.AssignedSeat != null)
        {
            var seat = customer.AssignedSeat;
            companionStool.transform.position = (seat.sitMarker != null ? seat.sitMarker.position : seat.transform.position) + new Vector3(1.5f, .75f, 0);
            companionStool.SetActive(true);
        }
        if (glass != null && !atSeat) glass.SetActive(false);
        if (companionBody != null) companionBody.sortingOrder = body.sortingOrder + 1;
        if (glassContours != null)
            foreach (var contour in glassContours) contour.sortingOrder = body.sortingOrder + 2;
        if (stillnessContours != null)
            foreach (var contour in stillnessContours) contour.sortingOrder = body.sortingOrder - 1;
    }
    private void OnDestroy() { if (lineMaterial != null) Destroy(lineMaterial); }
}
