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
            var shadow = Graphic("ContactShadow", button.transform, new Vector2(54, 4), new Vector2(0, -34));
            shadow.color = new Color(.08f, .05f, .02f, .45f);
            if (i == 0)
            {
                Tool(button.transform, "MixingGlass", new Vector2(44, 64), new Vector2(-5, -2));
                Tool(button.transform, "Spoon", new Vector2(9, 74), new Vector2(20, 4));
            }
            else if (i == 1)
            {
                var image = Graphic("Shaker", button.transform, new Vector2(38, 76), new Vector2(0, 4));
                image.sprite = shaker; image.preserveAspect = true;
            }
            else if (i == 2)
            {
                Tool(button.transform, "BottleSmall", new Vector2(19, 48), new Vector2(-30, -10));
                Tool(button.transform, "RocksGlass", new Vector2(44, 44), new Vector2(11, -12));
            }
            else
            {
                Tool(button.transform, "RocksGlass", new Vector2(44, 44), new Vector2(0, -12));
                Tool(button.transform, "SpoonHorizontal", new Vector2(68, 9), new Vector2(8, 20));
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
    static void Tool(Transform parent, string name, Vector2 size, Vector2 position)
    {
        var image = Graphic(name, parent, size, position);
        image.sprite = PixelBartendingArt.Get(name); image.preserveAspect = true;
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
    SurlhwaMethodSetupView owner;
    Button button;
    TMP_Text label;
    Image underline;
    Outline[] outlines;
    public void Configure(SurlhwaMethodSetupView view, MiniGameType method) { owner = view; Method = method; button = GetComponent<Button>(); }
    public void FinishVisuals()
    {
        var images = GetComponentsInChildren<Image>();
        var edges = new System.Collections.Generic.List<Outline>();
        foreach (var image in images)
            if (image.transform != transform && image.name != "ContactShadow")
            {
                var edge = image.gameObject.AddComponent<Outline>();
                edge.effectColor = new Color(.9f, .8f, .55f, .65f); edge.effectDistance = new Vector2(1, -1);
                edges.Add(edge);
            }
        outlines = edges.ToArray();
        var plate = SurlhwaUITheme.Rect("SmallLabelPlate", transform, new Vector2(120, 28), new Vector2(0, -52)).gameObject.AddComponent<Image>();
        plate.color = new Color(.12f, .15f, .16f, .86f); plate.raycastTarget = false;
        label = SurlhwaUITheme.Label("SetupLabel", transform, "", 18, new Vector2(120, 28), new Vector2(0, -52));
        label.alignment = TextAlignmentOptions.Center;
        underline = SurlhwaUITheme.Rect("SelectedUnderline", transform, new Vector2(54, 2), new Vector2(0, -35)).gameObject.AddComponent<Image>();
        underline.color = SurlhwaUITheme.Brass; underline.raycastTarget = false;
        Refresh();
    }
    public void ShowSelected(bool value) { Chosen = value; Refresh(); }
    void Refresh()
    {
        if (label == null) return;
        label.text = (Chosen ? "✓ " : "") + SurlhwaUITheme.Method(Method);
        label.color = Chosen ? SurlhwaUITheme.Brass : SurlhwaUITheme.Ink;
        underline.enabled = Chosen;
        foreach (var edge in outlines) edge.enabled = Chosen || Hovered || Focused;
        owner.RefreshHelp();
    }
    public void OnPointerEnter(PointerEventData data) { Hovered = button.IsInteractable(); Refresh(); }
    public void OnPointerExit(PointerEventData data) { Hovered = false; Refresh(); }
    public void OnSelect(BaseEventData data) { Focused = true; Refresh(); }
    public void OnDeselect(BaseEventData data) { Focused = false; Refresh(); }
    void OnDisable() { Hovered = Focused = false; }
}
