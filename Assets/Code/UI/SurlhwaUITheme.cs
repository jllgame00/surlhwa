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
            text.color = primary ? Surface : Ink;
            text.margin = new Vector4(8, 2, 8, 2);
        }
    }
    public static void Tree(GameObject root)
    {
        if (root == null) return;
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true)) Text(text, Mathf.Max(18, text.fontSize));
        foreach (var button in root.GetComponentsInChildren<Button>(true)) Button(button);
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
    public static void CornerButton(Button button, bool right)
    {
        if (button == null) return;
        var rect = button.GetComponent<RectTransform>();
        Place(rect, new Vector2(64, 64), Vector2.zero);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(right ? 1 : 0, 1);
        rect.anchoredPosition = new Vector2(right ? -24 : 24, -20);
        foreach (var image in button.GetComponentsInChildren<Image>(true))
            if (image.transform != button.transform) { Place(image.rectTransform, new Vector2(64, 64), Vector2.zero); image.raycastTarget = false; }
        foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
        {
            Text(text, 18); text.margin = Vector4.zero;
            Place(text.rectTransform, new Vector2(64, 32), Vector2.zero);
        }
        if (button.GetComponent<SurlhwaControlFocus>() == null) button.gameObject.AddComponent<SurlhwaControlFocus>();
    }
    public static string Method(MiniGameType method) => method switch {
        MiniGameType.Stir => "젓기", MiniGameType.Shake => "흔들기",
        MiniGameType.Build => "잔에 만들기", MiniGameType.Float => "층 쌓기", _ => "조주" };
    public static string Container(BartendingContainerType type) => type switch {
        BartendingContainerType.MixingGlass => "믹싱 글라스", BartendingContainerType.Shaker => "셰이커",
        BartendingContainerType.Jigger => "지거", _ => "서빙 잔" };
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
