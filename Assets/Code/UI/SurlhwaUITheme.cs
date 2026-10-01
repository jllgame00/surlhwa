using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>First-pass visual tokens. No gameplay or saved state.</summary>
public static class SurlhwaUITheme
{
    public static readonly Color Surface = Hex(0x20272B), Raised = Hex(0x303A3E);
    public static readonly Color Ink = Hex(0xF1E8D5), Muted = Hex(0xC4C1B6);
    public static readonly Color Brass = Hex(0xB9A06C), Edge = Hex(0x686252);
    public static readonly Color Shade = new Color(.025f, .03f, .04f, .48f);
    static Color Hex(uint c) => new Color(((c >> 16) & 255) / 255f, ((c >> 8) & 255) / 255f, (c & 255) / 255f);

    public static void Text(TMP_Text text, float size = 20, bool muted = false)
    {
        if (text == null) return;
        text.fontSize = size;
        text.enableAutoSizing = false;
        text.color = muted ? Muted : Ink;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
    }
    public static void Panel(GameObject panel)
    {
        if (panel == null) return;
        var image = panel.GetComponent<Image>();
        if (image != null) image.color = Surface;
        var edge = panel.GetComponent<Outline>() ?? panel.AddComponent<Outline>();
        edge.effectColor = Edge;
        edge.effectDistance = new Vector2(1, -1);
        edge.useGraphicAlpha = false;
    }
    public static void ScaleReadingCanvas(Transform content)
    {
        var canvas = content.GetComponentInParent<Canvas>(true)?.rootCanvas;
        var scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
        if (scaler == null) return;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;
    }
    public static void Button(Button button, bool primary = false)
    {
        if (button == null) return;
        if (button.image != null) button.image.color = Color.white;
        Color normal = primary ? Brass : Raised;
        var colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = Color.Lerp(normal, Ink, .18f);
        colors.selectedColor = Color.Lerp(normal, Brass, .36f);
        colors.pressedColor = Color.Lerp(normal, Color.black, .22f);
        colors.disabledColor = new Color(.23f, .25f, .26f, .6f);
        colors.colorMultiplier = 1;
        colors.fadeDuration = .1f;
        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;
        if (button.GetComponent<SurlhwaControlFocus>() == null) button.gameObject.AddComponent<SurlhwaControlFocus>();
        foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
        {
            Text(text);
            text.color = !button.interactable ? Muted : primary ? Surface : Ink;
            text.margin = new Vector4(8, 2, 8, 2);
        }
    }
    public static void Tree(GameObject root)
    {
        if (root == null) return;
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true)) Text(text, Mathf.Max(18, text.fontSize));
        foreach (var button in root.GetComponentsInChildren<Button>(true)) Button(button);
        foreach (var scroll in root.GetComponentsInChildren<Scrollbar>(true))
        {
            var track = scroll.GetComponent<Image>(); if (track != null) track.color = Raised;
            if (scroll.targetGraphic != null) scroll.targetGraphic.color = Brass;
        }
    }
    public static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        Place(rect, size, position);
        return rect;
    }
    public static void Place(RectTransform rect, Vector2 size, Vector2 position)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.localScale = Vector3.one;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }
    public static TMP_Text Label(string name, Transform parent, string value, float size, Vector2 area, Vector2 position)
    {
        var label = Rect(name, parent, area, position).gameObject.AddComponent<TextMeshProUGUI>();
        Text(label, size);
        label.text = value;
        return label;
    }
    public static void LabelButton(Button button, string label)
    {
        if (button == null) return;
        var text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.text = label;
    }
    public static void CornerButton(Button button, bool right, System.Func<bool> isOpen)
    {
        if (button == null) return;
        var rect = button.GetComponent<RectTransform>();
        Place(rect, new Vector2(64, 64), Vector2.zero);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(right ? 1 : 0, 1);
        rect.anchoredPosition = new Vector2(right ? -24 : 24, -20);
        if (button.GetComponent<SurlhwaUtilityControl>() != null) return;
        foreach (var image in button.GetComponentsInChildren<Image>(true))
            if (image.transform != button.transform) { image.enabled = false; image.raycastTarget = false; }
        foreach (var text in button.GetComponentsInChildren<TMP_Text>(true)) { text.enabled = false; text.raycastTarget = false; }
        var oldFocus = button.GetComponent<SurlhwaControlFocus>();
        if (oldFocus != null) Object.Destroy(oldFocus);
        var oldEdge = button.GetComponent<Outline>();
        if (oldEdge != null) Object.Destroy(oldEdge);
        button.transition = Selectable.Transition.None;
        button.image.sprite = null; button.image.color = Color.clear;
        button.gameObject.AddComponent<SurlhwaUtilityControl>().Configure(button, right, isOpen);
    }
    public static string Method(MiniGameType method) => method switch {
        MiniGameType.Stir => "젓기", MiniGameType.Shake => "흔들기",
        MiniGameType.Build => "잔에 만들기", MiniGameType.Float => "층 쌓기", _ => "조주" };
    public static string Container(BartendingContainerType type) => type switch {
        BartendingContainerType.MixingGlass => "믹싱 글라스", BartendingContainerType.Shaker => "셰이커",
        BartendingContainerType.Jigger => "지거", _ => "서빙 잔" };
}

/// <summary>Local utility chrome; observes the controller's open state and the
/// existing Button availability. Never registers a click or owns modal state.</summary>
internal sealed class SurlhwaUtilityControl : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    Button button;
    Image backing, activeMark;
    SurlhwaUtilityGlyph glyph;
    Outline edge;
    TMP_Text caption;
    System.Func<bool> isOpen;
    bool hovered, focused, pressed;
    public void Configure(Button target, bool settings, System.Func<bool> open)
    {
        button = target; isOpen = open;
        backing = SurlhwaUITheme.Rect("UtilitySurface", transform, new Vector2(48, 48), Vector2.zero).gameObject.AddComponent<Image>();
        backing.raycastTarget = false;
        edge = backing.gameObject.AddComponent<Outline>(); edge.effectDistance = new Vector2(1, -1);
        glyph = SurlhwaUITheme.Rect("UtilityGlyph", transform, new Vector2(28, 28), Vector2.zero).gameObject.AddComponent<SurlhwaUtilityGlyph>();
        glyph.Settings = settings; glyph.raycastTarget = false; glyph.SetVerticesDirty();
        activeMark = SurlhwaUITheme.Rect("UtilityOpenMark", transform, new Vector2(12, 2), new Vector2(0, -19)).gameObject.AddComponent<Image>();
        activeMark.raycastTarget = false;
        caption = SurlhwaUITheme.Label("UtilityCaption", transform, settings ? "설정" : "도감", 14, new Vector2(64, 20), new Vector2(0, -39));
        caption.alignment = TextAlignmentOptions.Center;
        var shadow = caption.gameObject.AddComponent<Shadow>(); shadow.effectColor = SurlhwaUITheme.Surface;
        shadow.effectDistance = new Vector2(1, -1);
        Update();
    }
    bool Available => button != null && button.IsInteractable() && SurlhwaModalSurface.Allows(transform);
    void Update()
    {
        if (button == null) return;
        bool open = isOpen != null && isOpen();
        bool available = Available;
        focused = available && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        if (!available) hovered = focused = pressed = false;
        var normal = SurlhwaUITheme.Surface;
        backing.color = pressed ? Color.Lerp(normal, Color.black, .25f) : hovered || focused ? SurlhwaUITheme.Raised : normal;
        glyph.color = !available && !open ? new Color(.52f, .54f, .53f) : SurlhwaUITheme.Ink;
        edge.effectColor = focused ? SurlhwaUITheme.Ink : open ? SurlhwaUITheme.Brass : SurlhwaUITheme.Edge;
        edge.enabled = available && (hovered || focused) || open;
        activeMark.color = open ? SurlhwaUITheme.Brass : Color.clear;
        caption.enabled = available && (hovered || focused);
    }
    public void OnPointerEnter(PointerEventData _) { hovered = Available; }
    public void OnPointerExit(PointerEventData _) { hovered = false; pressed = false; }
    public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) pressed = Available; }
    public void OnPointerUp(PointerEventData _) { pressed = false; }
    public void OnSelect(BaseEventData _) { focused = Available; }
    public void OnDeselect(BaseEventData _) { focused = pressed = false; }
    void OnDisable() { hovered = focused = pressed = false; }
}

/// <summary>Project-owned geometric book/gear, authored directly as uGUI mesh.
/// No image-generation, font glyph, texture import or external art dependency.</summary>
[RequireComponent(typeof(CanvasRenderer))]
internal sealed class SurlhwaUtilityGlyph : MaskableGraphic
{
    public bool Settings;
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (Settings)
        {
            // Eight quiet teeth, a continuous ring and a transparent central bore.
            const int segments = 64;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                float radius = i % 8 >= 2 && i % 8 <= 5 ? 13 : 10;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Add(mesh, direction * 6.5f); Add(mesh, direction * radius);
            }
            for (int i = 0; i < segments; i++)
            {
                int a = i * 2, b = ((i + 1) % segments) * 2;
                mesh.AddTriangle(a, b, b + 1); mesh.AddTriangle(a, b + 1, a + 1);
            }
        }
        else
        {
            // Open pages and spine, with two spare strokes that read at 720p.
            Line(mesh, new Vector2(0,-10), new Vector2(0,9));
            foreach (int side in new[] { -1, 1 })
            {
                Line(mesh, new Vector2(0,9), new Vector2(side*12,11));
                Line(mesh, new Vector2(side*12,11), new Vector2(side*12,-8));
                Line(mesh, new Vector2(side*12,-8), new Vector2(0,-10));
                Line(mesh, new Vector2(side*4,4), new Vector2(side*9,5), 1.5f);
                Line(mesh, new Vector2(side*4,-1), new Vector2(side*9,0), 1.5f);
            }
        }
    }
    void Add(VertexHelper mesh, Vector2 p)
    {
        var r = rectTransform.rect;
        mesh.AddVert(new Vector3(r.center.x + p.x*r.width/28, r.center.y + p.y*r.height/28), color, Vector2.zero);
    }
    void Line(VertexHelper mesh, Vector2 a, Vector2 b, float width = 2)
    {
        var d = (b-a).normalized; var n = new Vector2(-d.y,d.x)*width*.5f;
        int first = mesh.currentVertCount;
        Add(mesh,a-n); Add(mesh,a+n); Add(mesh,b+n); Add(mesh,b-n);
        mesh.AddTriangle(first,first+1,first+2); mesh.AddTriangle(first,first+2,first+3);
    }
}

internal sealed class SurlhwaControlFocus : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    Outline edge;
    public void OnSelect(BaseEventData data)
    {
        if (edge == null) { edge = gameObject.AddComponent<Outline>(); edge.effectDistance = new Vector2(2, -2); edge.effectColor = SurlhwaUITheme.Ink; }
        edge.enabled = true;
    }
    public void OnDeselect(BaseEventData data) { if (edge != null) edge.enabled = false; }
    void OnDisable() { if (edge != null) edge.enabled = false; }
}
