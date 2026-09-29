using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Counter-anchored presentation of the existing method buttons.
/// Owns hover/focus visuals only; InteractionUIController owns selection/routing.</summary>
public sealed class SurlhwaMethodSetupView : MonoBehaviour
{
    static readonly MiniGameType[] Methods = { MiniGameType.Stir, MiniGameType.Shake, MiniGameType.Build, MiniGameType.Float };
    // World points on the accepted front countertop. Never move the room/camera.
    static readonly float[] CounterX = { -3.36f, -1.32f, 1.8f, 4.56f };
    readonly SurlhwaMethodSetupTarget[] targets = new SurlhwaMethodSetupTarget[4];
    Camera worldCamera;
    RectTransform root;
    TMP_Text requestHeading, requestBody, help;

    public void Configure(Button[] buttons, Button confirm, Button back, Sprite shaker, Camera camera)
    {
        root = (RectTransform)transform;
        worldCamera = camera;
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
        root.pivot = new Vector2(.5f, .5f); root.offsetMin = root.offsetMax = Vector2.zero;
        var background = GetComponent<Image>();
        if (background != null) { background.enabled = false; background.raycastTarget = false; }
        foreach (var button in buttons) button.transform.SetParent(transform, false);
        confirm.transform.SetParent(transform, false); back.transform.SetParent(transform, false);
        foreach (Transform child in transform)
            if (child.GetComponent<Button>() == null) child.gameObject.SetActive(false);

        var note = SurlhwaUITheme.Rect("SetupRequestNote", transform, new Vector2(1040, 136), new Vector2(0, 88));
        note.anchorMin = note.anchorMax = new Vector2(.5f, 0);
        note.gameObject.AddComponent<Image>(); SurlhwaUITheme.Panel(note.gameObject);
        requestHeading = SurlhwaUITheme.Label("RequestHeading", note, "", 18, new Vector2(740, 24), new Vector2(-134, 44));
        requestBody = SurlhwaUITheme.Label("RequestContext", note, "", 20, new Vector2(740, 50), new Vector2(-134, 4));
        help = SurlhwaUITheme.Label("SetupHelp", note, "", 18, new Vector2(740, 28), new Vector2(-134, -43));
        requestHeading.color = SurlhwaUITheme.Muted;
        confirm.transform.SetParent(note, false); back.transform.SetParent(note, false);
        SurlhwaUITheme.Place((RectTransform)confirm.transform, new Vector2(144, 44), new Vector2(420, 27));
        SurlhwaUITheme.Place((RectTransform)back.transform, new Vector2(144, 40), new Vector2(420, -29));
        SurlhwaUITheme.Button(confirm, true); SurlhwaUITheme.Button(back);
        SurlhwaUITheme.LabelButton(confirm, "조주 시작"); SurlhwaUITheme.LabelButton(back, "돌아가기");

        for (int i = 0; i < targets.Length; i++)
        {
            var button = buttons[i];
            foreach (Transform child in button.transform) child.gameObject.SetActive(false);
            SurlhwaUITheme.Place((RectTransform)button.transform, new Vector2(128, 132), Vector2.zero);
            button.transition = Selectable.Transition.None;
            button.image.color = Color.clear; button.image.raycastTarget = true;
            targets[i] = button.gameObject.AddComponent<SurlhwaMethodSetupTarget>();
            targets[i].Configure(this, Methods[i]);
            var stage = targets[i].Stage;
            // Small differences in depth belong to the arrangement, not the hit area.
            stage.anchoredPosition = new Vector2(0, i == 1 ? 2 : i == 2 ? -2 : 0);
            if (i == 0)
            {
                Tool(stage, "Spoon", new Vector2(9, 68), new Vector2(6, 10), -8, false);
                Tool(stage, "MixingGlass", new Vector2(48, 70), new Vector2(-7, 1));
            }
            else if (i == 1)
            {
                Tool(stage, "Jigger", new Vector2(16, 28), new Vector2(25, -17));
                Prop(stage, "Shaker", shaker, new Vector2(40, 80), new Vector2(-7, 6));
            }
            else if (i == 2)
            {
                Tool(stage, "BottleSmall", new Vector2(23, 55), new Vector2(-25, -3), -4);
                Tool(stage, "RocksGlass", new Vector2(50, 50), new Vector2(10, -9));
            }
            else
            {
                Tool(stage, "Jigger", new Vector2(17, 30), new Vector2(-26, -17), -8);
                Tool(stage, "RocksGlass", new Vector2(46, 46), new Vector2(10, -11));
                // Rest the spoon on the glass rim; the small pouring vessel sits beside it.
                Tool(stage, "SpoonHorizontal", new Vector2(58, 8), new Vector2(16, 14), -4, false);
            }
            targets[i].FinishVisuals();
        }
        RefreshHelp();
        LateUpdate();
    }

    static Image Graphic(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var image = SurlhwaUITheme.Rect(name, parent, size, position).gameObject.AddComponent<Image>();
        image.raycastTarget = false;
        return image;
    }
    static void Tool(Transform parent, string name, Vector2 size, Vector2 position, float angle = 0, bool grounded = true)
        => Prop(parent, name, PixelBartendingArt.Get(name), size, position, angle, grounded);
    static void Prop(Transform parent, string name, Sprite sprite, Vector2 size, Vector2 position, float angle = 0, bool grounded = true)
    {
        if (grounded)
            SurlhwaSetupSoftOval.Create(name + "Footprint", parent, new Vector2(size.x * .95f, 9),
                new Vector2(position.x, position.y - size.y / 2 + 1), new Color(.055f, .035f, .018f, .75f));
        var image = Graphic(name, parent, size, position);
        image.sprite = sprite; image.preserveAspect = true;
        image.rectTransform.localEulerAngles = new Vector3(0, 0, angle);
    }
    void LateUpdate()
    {
        if (root == null || worldCamera == null) return;
        var canvas = GetComponentInParent<Canvas>().rootCanvas;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) continue;
            var screen = worldCamera.WorldToScreenPoint(new Vector3(CounterX[i], -.48f, 0));
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, uiCamera, out var local))
                ((RectTransform)targets[i].transform).anchoredPosition = local + new Vector2(0, 34);
        }
    }
    public void ShowRequest(string heading, string body)
    {
        requestHeading.text = heading; requestBody.text = body;
    }
    public void ShowSelection(MiniGameType? method)
    {
        foreach (var target in targets) if (target != null) target.ShowSelected(method == target.Method);
        RefreshHelp();
    }
    internal void RefreshHelp()
    {
        if (help == null) return;
        SurlhwaMethodSetupTarget active = null;
        foreach (var t in targets) if (t != null && t.Chosen) active = t;
        foreach (var t in targets) if (t != null && t.Focused) active = t;
        foreach (var t in targets) if (t != null && t.Hovered) active = t;
        help.text = active == null ? "바 위의 기물을 고른 뒤 조주를 시작하세요." : active.Method switch {
            MiniGameType.Stir => "젓기 · Stir — 믹싱 글라스 안에서 스푼으로 천천히 젓습니다.",
            MiniGameType.Shake => "흔들기 · Shake — 셰이커를 좌우로 흔들어 섞습니다.",
            MiniGameType.Build => "잔에 만들기 · Build — 서빙 잔에 재료를 차례로 붓습니다.",
            _ => "층 쌓기 · Float — 잔 위 스푼을 따라 천천히 부어 층을 쌓습니다." };
    }
}

internal sealed class SurlhwaMethodSetupTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public MiniGameType Method { get; private set; }
    public bool Chosen { get; private set; }
    public bool Hovered { get; private set; }
    public bool Focused { get; private set; }
    public RectTransform Stage { get; private set; }
    SurlhwaMethodSetupView owner;
    Button button;
    TMP_Text label;
    SurlhwaSetupSoftOval counterLight, rimLight, labelShade;
    Image[] props;
    Outline[] outlines;
    float emphasis;
    public void Configure(SurlhwaMethodSetupView view, MiniGameType method)
    {
        owner = view; Method = method; button = GetComponent<Button>();
        Stage = SurlhwaUITheme.Rect("CountertopArrangement", transform, new Vector2(128, 132), Vector2.zero);
        counterLight = SurlhwaSetupSoftOval.Create("CounterLight", Stage, new Vector2(100, 28), new Vector2(0, -32), Color.clear);
        rimLight = SurlhwaSetupSoftOval.Create("WarmToolLight", Stage, new Vector2(82, 96), new Vector2(-2, 3), Color.clear);
        SurlhwaSetupSoftOval.Create("ContactShadow", Stage, new Vector2(82, 18), new Vector2(0, -33), new Color(.065f, .035f, .015f, .48f));
    }
    public void FinishVisuals()
    {
        props = Stage.GetComponentsInChildren<Image>();
        var edges = new System.Collections.Generic.List<Outline>();
        foreach (var image in props)
            {
                var edge = image.gameObject.AddComponent<Outline>();
                edge.effectDistance = new Vector2(1, -1);
                edges.Add(edge);
            }
        outlines = edges.ToArray();
        labelShade = SurlhwaSetupSoftOval.Create("LabelShade", transform, new Vector2(100, 24), new Vector2(0, -53), Color.clear);
        label = SurlhwaUITheme.Label("SetupLabel", transform, "", 16, new Vector2(116, 24), new Vector2(0, -52));
        label.alignment = TextAlignmentOptions.Center;
        var textShadow = label.gameObject.AddComponent<Shadow>();
        textShadow.effectColor = new Color(.035f, .025f, .015f, .9f);
        textShadow.effectDistance = new Vector2(1, -1);
        Refresh();
        ApplyEmphasis();
    }
    public void ShowSelected(bool value) { Chosen = value; Refresh(); }
    void Refresh()
    {
        if (label == null) return;
        label.text = (Chosen ? "✓ " : "") + SurlhwaUITheme.Method(Method);
        // The panel is built inactive, before TMP initializes its font/material.
        // Four fixed short names need no early text-mesh measurement.
        float shadeWidth = Method == MiniGameType.Build ? 100 : Method == MiniGameType.Stir ? 48 : 72;
        labelShade.rectTransform.sizeDelta = new Vector2(shadeWidth + (Chosen ? 12 : 0), 24);
        owner.RefreshHelp();
    }
    void Update()
    {
        float target = Chosen ? 1 : Hovered ? .65f : Focused ? .3f : 0;
        if (Mathf.Approximately(emphasis, target) || label == null) return;
        emphasis = Mathf.MoveTowards(emphasis, target, Time.unscaledDeltaTime / .14f);
        ApplyEmphasis();
    }
    void ApplyEmphasis()
    {
        // Quiet light on the wood and tool edges; no scale pulse, bounce or gameplay timing.
        counterLight.color = new Color(.98f, .73f, .35f, emphasis * .30f);
        rimLight.color = new Color(1, .82f, .48f, emphasis * .13f);
        labelShade.color = new Color(.06f, .035f, .018f, .26f + emphasis * .10f);
        label.color = Color.Lerp(SurlhwaUITheme.Muted, SurlhwaUITheme.Ink, emphasis);
        foreach (var prop in props) prop.color = Color.Lerp(new Color(.87f, .85f, .81f), Color.white, emphasis);
        foreach (var edge in outlines)
        {
            edge.enabled = emphasis > .01f;
            edge.effectColor = new Color(1, .84f, .53f, emphasis * .62f);
        }
    }
    public void OnPointerEnter(PointerEventData data) { Hovered = button.IsInteractable(); Refresh(); }
    public void OnPointerExit(PointerEventData data) { Hovered = false; Refresh(); }
    public void OnSelect(BaseEventData data) { Focused = true; Refresh(); }
    public void OnDeselect(BaseEventData data) { Focused = false; Refresh(); }
    void OnDisable() { Hovered = Focused = false; emphasis = 0; if (label != null) ApplyEmphasis(); }
}

/// <summary>Small deterministic uGUI helper. Four alpha bands form a soft oval;
/// no texture/importer dependency, material glow, raycast or gameplay state.</summary>
[RequireComponent(typeof(CanvasRenderer))]
internal sealed class SurlhwaSetupSoftOval : MaskableGraphic
{
    public static SurlhwaSetupSoftOval Create(string name, Transform parent, Vector2 size, Vector2 position, Color tint)
    {
        var graphic = SurlhwaUITheme.Rect(name, parent, size, position).gameObject.AddComponent<SurlhwaSetupSoftOval>();
        graphic.raycastTarget = false; graphic.color = tint;
        return graphic;
    }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        const int segments = 32;
        var rect = rectTransform.rect;
        float[] radius = { 0, .45f, .72f, 1 };
        float[] opacity = { 1, .72f, .28f, 0 };
        for (int ring = 0; ring < radius.Length; ring++)
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                var tint = color; tint.a *= opacity[ring];
                mesh.AddVert(new Vector3(rect.center.x + Mathf.Cos(angle) * rect.width * .5f * radius[ring],
                    rect.center.y + Mathf.Sin(angle) * rect.height * .5f * radius[ring]), tint, Vector2.zero);
            }
        for (int ring = 0; ring < radius.Length - 1; ring++)
            for (int i = 0; i < segments; i++)
            {
                int a = ring * segments + i, b = ring * segments + (i + 1) % segments;
                mesh.AddTriangle(a, b, b + segments); mesh.AddTriangle(a, b + segments, a + segments);
            }
    }
}
