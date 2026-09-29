using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Layout around existing tools; reads no expected recipe or FIT data.</summary>
public static class SurlhwaBartendingPresentation
{
    public static void Apply(GameObject root, string requestTitle)
    {
        var ui = root.transform.Find("UI");
        if (ui == null) return;
        SurlhwaUITheme.Tree(ui.gameObject);
        var shelf = root.GetComponentInChildren<IngredientShelfController>(true);
        foreach (var option in root.GetComponentsInChildren<BrewIngredientOptionView>(true))
            option.SetSelectedVisual(shelf != null && option.Ingredient == shelf.SelectedIngredient);
        var request = SurlhwaUITheme.Label("RequestContext", ui, "손님의 부탁 · " + requestTitle,
            22, new Vector2(720, 44), new Vector2(-120, 316));
        request.alignment = TextAlignmentOptions.MidlineLeft;
        foreach (var name in new[] { "HintText", "StartHintText" })
        {
            var text = ui.Find(name)?.GetComponent<TMP_Text>();
            if (text == null) continue;
            SurlhwaUITheme.Text(text, 20);
            SurlhwaUITheme.Place(text.rectTransform, new Vector2(800, 72), new Vector2(170, 242));
        }
        var progress = ui.Find("ProgressText")?.GetComponent<TMP_Text>();
        if (progress != null)
        {
            SurlhwaUITheme.Text(progress, 20, true);
            SurlhwaUITheme.Place(progress.rectTransform, new Vector2(800, 48), new Vector2(170, 178));
        }
        foreach (var name in new[] { "StartupButton", "StartButton", "FinishButton" })
        {
            var button = ui.Find(name)?.GetComponent<Button>();
            if (button != null) SurlhwaUITheme.Button(button, true);
        }
    }
    public static void AddSettings(GameObject root, Action open)
    {
        var ui = root.GetComponentInChildren<IngredientShelfController>(true)?.transform.parent;
        if (ui == null || ui.Find("BartendingSettings") != null) return;
        var rect = SurlhwaUITheme.Rect("BartendingSettings", ui, new Vector2(104, 44), new Vector2(330, 308));
        rect.gameObject.AddComponent<Image>();
        var button = rect.gameObject.AddComponent<Button>();
        var label = SurlhwaUITheme.Label("Label", rect, "설정", 20, new Vector2(104, 44), Vector2.zero);
        label.alignment = TextAlignmentOptions.Center;
        button.onClick.AddListener(() => open());
        SurlhwaUITheme.Button(button);
    }
}
