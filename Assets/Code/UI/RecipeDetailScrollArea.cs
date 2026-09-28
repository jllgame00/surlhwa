using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Presentation-only scrolling for authored recipe text; knowledge
/// filtering remains with the existing formatter and guide/codex owners.</summary>
public static class RecipeDetailScrollArea
{
    public static void Ensure(TMP_Text text)
    {
        if (!Application.isPlaying || text == null || text.transform.parent.GetComponent<ScrollRect>() != null) return;
        RectTransform original = text.rectTransform;
        var viewport = new GameObject(text.name + "Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        var rect = viewport.GetComponent<RectTransform>();
        rect.SetParent(original.parent, false);
        rect.SetSiblingIndex(original.GetSiblingIndex());
        rect.anchorMin = original.anchorMin; rect.anchorMax = original.anchorMax;
        rect.pivot = original.pivot; rect.anchoredPosition = original.anchoredPosition;
        rect.sizeDelta = original.sizeDelta;
        viewport.GetComponent<Image>().color = new Color(0, 0, 0, .01f);
        original.SetParent(rect, false);
        original.anchorMin = new Vector2(0, 1); original.anchorMax = Vector2.one;
        original.pivot = new Vector2(.5f, 1);
        original.anchoredPosition = new Vector2(-10, 0);
        original.sizeDelta = new Vector2(-20, 0);
        text.overflowMode = TextOverflowModes.Overflow;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        var fit = text.GetComponent<ContentSizeFitter>() ?? text.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var track = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        var trackRect = track.GetComponent<RectTransform>();
        trackRect.SetParent(rect, false);
        trackRect.anchorMin = new Vector2(1, 0); trackRect.anchorMax = Vector2.one;
        trackRect.pivot = new Vector2(1, .5f);
        trackRect.sizeDelta = new Vector2(12, 0);
        track.GetComponent<Image>().color = new Color(.3f, .28f, .24f);
        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.SetParent(trackRect, false);
        handleRect.anchorMin = Vector2.zero; handleRect.anchorMax = Vector2.one;
        handleRect.sizeDelta = Vector2.zero;
        var image = handle.GetComponent<Image>();
        image.color = new Color(.78f, .66f, .44f);
        var scrollbar = track.GetComponent<Scrollbar>();
        scrollbar.handleRect = handleRect; scrollbar.targetGraphic = image;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        var scroll = viewport.GetComponent<ScrollRect>();
        scroll.viewport = rect; scroll.content = original;
        scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 32;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
    }

    public static void ResetToTop(TMP_Text text)
    {
        if (text == null) return;
        var scroll = text.transform.parent.GetComponent<ScrollRect>();
        if (scroll == null) return;
        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = 1;
    }
}
