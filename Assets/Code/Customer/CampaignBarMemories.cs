using System.Collections.Generic;
using UnityEngine;

/// <summary>Small spatial drafts derived from existing departure records; no unlock authority or saved state.</summary>
public sealed class CampaignBarMemories : MonoBehaviour
{
    [SerializeField] private BusinessDayManager businessDayManager;
    [Tooltip("Presentation anchors in existing memory order; no unlock or persistence state.")]
    [SerializeField] private Transform[] displayAnchors;
    private readonly Dictionary<string, GameObject> memories = new();
    private Material material;
    private GameObject star;
    private Transform ripple;
    private int previousMask = -1;
    public int VisibleMemoryCount { get; private set; }
    private static readonly CampaignVisitId[] FinalVisits = {
        CampaignVisitId.LoversReunion, CampaignVisitId.QuixoteAlone, CampaignVisitId.HamletStillness,
        CampaignVisitId.FaustCost, CampaignVisitId.SimCheongReturn, CampaignVisitId.OdysseusStay
    };

    private void Start()
    {
        material = new Material(Shader.Find("Sprites/Default"));
        BuildDrafts();
        Refresh();
    }
    private void Update()
    {
        Refresh();
        if (ripple != null && ripple.gameObject.activeInHierarchy)
            ripple.localScale = new Vector3(1 + .025f * Mathf.Sin(Time.unscaledTime * .4f), 1, 1);
    }
    public void Refresh()
    {
        if (memories.Count == 0) return;
        int mask = 0;
        if (businessDayManager != null && businessDayManager.HasCampaign)
        {
            if (LittlePrinceVisitPersistence.IsClosingVisitCompleted()) mask |= 1;
            for (int i = 0; i < FinalVisits.Length; i++)
                if (CampaignProgressPersistence.IsCompleted(FinalVisits[i])) mask |= 1 << (i + 1);
            if (LittlePrinceVisitPersistence.IsStatueEligible()) mask |= 128;
        }
        if (mask == previousMask) return;
        previousMask = mask;
        VisibleMemoryCount = 0;
        int index = 0;
        foreach (var pair in memories)
        {
            bool visible = (mask & (1 << index++)) != 0;
            pair.Value.SetActive(visible);
            if (visible) VisibleMemoryCount++;
        }
        star.SetActive((mask & 128) != 0);
    }
    private Transform Root(string title, float x, float y)
    {
        var root = new GameObject(title);
        root.transform.SetParent(transform, false);
        root.transform.position = new Vector3(x, y, 0);
        int index = memories.Count;
        if (displayAnchors != null && index < displayAnchors.Length && displayAnchors[index] != null)
            root.transform.position = displayAnchors[index].position;
        memories.Add(title, root);
        return root.transform;
    }
    private void Line(Transform root, params float[] xy) =>
        DrawLine(root, material, new Color(.64f, .52f, .36f, .92f), .035f, xy);
    public static void DrawLine(Transform root, Material ink, Color color, float width, params float[] xy)
    {
        var line = new GameObject("Contour").AddComponent<LineRenderer>();
        line.transform.SetParent(root, false);
        line.useWorldSpace = false;
        line.sharedMaterial = ink;
        line.startColor = line.endColor = color;
        line.startWidth = line.endWidth = width;
        line.numCapVertices = 3;
        line.numCornerVertices = 3;
        line.sortingOrder = 8;
        line.positionCount = xy.Length / 2;
        for (int i = 0; i < line.positionCount; i++) line.SetPosition(i, new Vector3(xy[2 * i], xy[2 * i + 1], 0));
    }
    private void Oval(Transform root, Color color, float x, float y, float rx, float ry)
    {
        var xy = new float[66];
        for (int i = 0; i <= 32; i++)
        {
            float a = i * Mathf.PI / 16;
            xy[i * 2] = x + Mathf.Cos(a) * rx;
            xy[i * 2 + 1] = y + Mathf.Sin(a) * ry;
        }
        DrawLine(root, material, color, .025f, xy);
    }
    private void BuildDrafts()
    {
        var chair = Root("Empty chair", -5.65f, -1.25f);
        Line(chair, -.28f,-.3f, -.28f,.55f, .28f,.55f, .28f,-.3f);
        Line(chair, -.34f,0, .34f,0);
        star = new GameObject("Small star"); star.transform.SetParent(chair, false);
        DrawLine(star.transform, material, new Color(.88f,.76f,.42f), .025f,
            .48f,.6f, .53f,.45f, .69f,.45f, .56f,.35f, .61f,.2f, .48f,.29f, .35f,.2f, .4f,.35f, .27f,.45f, .43f,.45f, .48f,.6f);
        var swing = Root("Swing beside an empty place", -3.15f, .7f);
        Line(swing, -.42f,0, -.32f,.75f, .32f,.75f, .42f,0);
        Line(swing, -.18f,.72f, -.18f,.18f, .18f,.18f, .18f,.72f);
        var mill = Root("Leaning windmill fragment", -6.0f, 1.0f);
        Line(mill, -.18f,0, -.08f,.48f, .11f,.48f, .22f,0, -.18f,0);
        Line(mill, -.32f,.73f, .03f,.48f, .38f,.23f);
        Line(mill, -.19f,.15f, .03f,.48f, .25f,.81f);
        var stage = Root("Stage and skull", .85f, .68f);
        Line(stage, -.38f,0, .4f,0, .32f,.1f, -.32f,.1f, -.38f,0);
        Oval(stage, new Color(.65f,.63f,.56f), 0,.3f,.16f,.18f);
        Line(stage, -.1f,.26f, -.05f,.26f); Line(stage, .05f,.26f, .1f,.26f);
        DrawLine(stage, material, new Color(.32f,.31f,.3f,.6f), .06f, .18f,.1f, .65f,.02f);
        var glass = Root("Cracked glass within a faint circle", 5.55f, .67f);
        Oval(glass, new Color(.46f,.37f,.3f,.5f),0,0,.4f,.07f);
        Line(glass, -.18f,.48f, -.12f,.04f, .13f,.04f, .18f,.48f);
        Line(glass, -.16f,.46f, .02f,.46f, -.02f,.29f, .07f,.18f);
        DrawLine(glass, material, new Color(.5f,.16f,.12f,.75f), .04f, -.07f,.08f, .08f,.08f);
        var lotus = Root("Half submerged lotus", -1.2f, -1.45f);
        ripple = new GameObject("Slow water").transform; ripple.SetParent(lotus, false);
        Oval(ripple, new Color(.4f,.55f,.57f,.6f),0,0,.52f,.08f);
        DrawLine(lotus,material,new Color(.66f,.47f,.5f),.035f,
            -.26f,.04f, -.34f,.24f, -.12f,.19f, 0,.46f, .12f,.19f, .34f,.24f, .26f,.04f, -.26f,.04f);
        var anchor = Root("Anchor and ship fragment", 7.4f, -1.2f);
        Line(anchor, 0,.5f, 0,-.1f, -.24f,.06f, -.29f,.18f);
        Line(anchor, 0,-.1f, .24f,.06f, .29f,.18f); Line(anchor,-.18f,.3f,.18f,.3f);
        Oval(anchor, new Color(.55f,.57f,.58f),0,.59f,.09f,.09f);
        Line(anchor, -.65f,-.1f, -.5f,-.25f, -.18f,-.25f);
    }
    private void OnDestroy() { if (material != null) Destroy(material); }
}
