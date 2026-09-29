using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DayResultPanelController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI reputationText;
    [SerializeField] private TextMeshProUGUI customerCountText;
    [SerializeField] private TextMeshProUGUI unlockedRecipesText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private CocktailCodexPanelController codexPanel;

    private bool openingResult;
    private bool presentationReady, navigationRequested;
    private bool isDemoCompletionResult;
    private string[] endingLines;
    private int endingLineIndex;
    private const string NextNightButtonLabel = "다음 밤으로";
    private const string ReturnToTitleButtonLabel = "처음 화면으로";
    private const string NewRecipesRecordedPrefix = "오늘 기록한 칵테일";
    private const string RestartButtonLabel = "다음 밤으로";
    private const string CodexButtonLabel = "도감 펼치기";

    private void Awake()
    {
        EnsurePanel();
        BindButtons();
        ConfigurePresentation();

        if (!openingResult)
            Close();
    }

    public static DayResultPanelController CreateRuntimePanel()
    {
        DayResultPanelController existing = FindFirstObjectByType<DayResultPanelController>(FindObjectsInactive.Include);
        if (existing != null)
            return existing;

        GameObject controllerObject = new GameObject("DayResultPanelController");
        return controllerObject.AddComponent<DayResultPanelController>();
    }

    public void OpenResult(
        int reputation,
        int servedCustomerCount,
        int maxCustomersPerDay,
        IReadOnlyList<string> unlockedRecipeNames,
        bool demoComplete = false
    )
    {
        EnsurePanel();
        BindButtons();
        ConfigurePresentation();
        ConfigureReadingLayout(false);
        navigationRequested = false;
        isDemoCompletionResult = demoComplete;
        ConfigureDemoNavigationButton();

        if (titleText != null)
            titleText.text = "밤의 마감";

        if (reputationText != null)
            reputationText.text = $"오늘의 평판: {reputation}";

        if (customerCountText != null)
            customerCountText.text = $"마주한 손님: {servedCustomerCount}/{maxCustomersPerDay}";

        if (unlockedRecipesText != null)
            unlockedRecipesText.text = BuildNewRecipeText(unlockedRecipeNames);

        if (panelRoot != null)
        {
            openingResult = true;
            panelRoot.SetActive(true);
            openingResult = false;
        }

        if (demoComplete && titleText != null)
            titleText.text = "네 번의 밤을 마치며";
    }

    public void OpenDemoComplete()
    {
        EnsurePanel();
        BindButtons();
        isDemoCompletionResult = true;
        ConfigureDemoNavigationButton();

        if (titleText != null)
            titleText.text = "네 번의 밤을 마치며";

        if (reputationText != null)
            reputationText.text = string.Empty;

        if (customerCountText != null)
            customerCountText.text = "설화의 네 번의 밤이 끝났습니다.";

        if (unlockedRecipesText != null)
            unlockedRecipesText.text = "새 게임으로 첫날부터 다시 만날 수 있습니다.";

        if (panelRoot != null)
        {
            openingResult = true;
            panelRoot.SetActive(true);
            openingResult = false;
        }
    }

    public void OpenCampaignEnding(DialogueData ending)
    {
        EnsurePanel();
        // The serialized panel host starts inactive. Awake must run before the
        // ending binds its navigation, and must not close this first opening.
        openingResult = true;
        panelRoot.SetActive(true);
        openingResult = false;
        BindButtons();
        ConfigureCampaignEndingLayout();
        endingLines = ending != null ? ending.onEnterLines : null;
        endingLineIndex = 0;
        isDemoCompletionResult = true;
        ConfigureDemoNavigationButton();
        if (reputationText != null) reputationText.text = string.Empty;
        if (unlockedRecipesText != null) unlockedRecipesText.text = string.Empty;
        if (titleText != null) titleText.text = "불빛이 남은 자리";
        if (CampaignProgressPersistence.EndingRead) ShowCampaignCoda();
        else
        {
            if (endingLines == null || endingLines.Length == 0)
            {
                Debug.LogError("[Campaign] Missing authored ending.");
                return;
            }
            restartButton.onClick.RemoveListener(ReturnToTitle);
            restartButton.onClick.RemoveListener(NextEndingLine);
            restartButton.onClick.AddListener(NextEndingLine);
            SetButtonLabel(restartButton, "계속 읽기");
            if (closeButton != null) closeButton.gameObject.SetActive(false);
            if (customerCountText != null) customerCountText.text = endingLines[0];
        }
    }

    private void ConfigureCampaignEndingLayout()
    {
        ConfigurePresentation();
        ConfigureReadingLayout(true);
    }

    private void ConfigurePresentation()
    {
        if (presentationReady || panelRoot == null) return;
        presentationReady = true;
        var canvas = panelRoot.GetComponentInParent<Canvas>(true)?.rootCanvas;
        var scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
        if (scaler != null) { scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f; }
        SurlhwaUITheme.Tree(panelRoot);
        var backdrop = panelRoot.GetComponent<Image>(); if (backdrop != null) backdrop.color = Color.clear;
        SurlhwaModalSurface.Configure(panelRoot, 1000, null);
        SurlhwaReadingClickBoundary.Bind(restartButton, closeButton);
        ConfigureReadingLayout(false);
    }
    private void ConfigureReadingLayout(bool ending)
    {
        if (customerCountText == null) return;
        var content = customerCountText.transform.parent as RectTransform;
        if (content == null) return;
        var layout = content.GetComponent<LayoutGroup>(); if (layout != null) layout.enabled = false;
        SurlhwaUITheme.Place(content, new Vector2(680, 380), Vector2.zero);
        SurlhwaUITheme.Panel(content.gameObject);
        LayoutText(titleText, new Vector2(624, 44), new Vector2(0, 140), 28);
        LayoutText(reputationText, new Vector2(624, 30), new Vector2(0, 88), 20);
        LayoutText(customerCountText, new Vector2(624, ending ? 160 : 40), new Vector2(0, ending ? 30 : 42), 22);
        LayoutText(unlockedRecipesText, new Vector2(624, ending ? 42 : 100), new Vector2(0, ending ? -76 : -38), 20);
        Button[] buttons = { restartButton, closeButton };
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            buttons[i].transform.SetParent(content, false);
            SurlhwaUITheme.Place(buttons[i].GetComponent<RectTransform>(), new Vector2(240, 48), new Vector2(-132 + i * 264, -144));
            SurlhwaUITheme.Button(buttons[i], i == 0);
        }
    }
    private static void LayoutText(TMP_Text text, Vector2 area, Vector2 position, float size)
    {
        if (text == null) return;
        SurlhwaUITheme.Place(text.rectTransform, area, position); SurlhwaUITheme.Text(text, size);
    }

    private void NextEndingLine()
    {
        endingLineIndex++;
        if (endingLineIndex < endingLines.Length)
        {
            if (customerCountText != null) customerCountText.text = endingLines[endingLineIndex];
            return;
        }
        CampaignProgressPersistence.MarkEndingRead();
        ShowCampaignCoda();
    }

    private void ShowCampaignCoda()
    {
        restartButton.onClick.RemoveListener(NextEndingLine);
        ConfigureDemoNavigationButton();
        if (customerCountText != null) customerCountText.text = "이야기는 이곳에 남아 있습니다.";
        if (unlockedRecipesText != null) unlockedRecipesText.text = "잔을 닦고, 불빛을 남겨 둡니다.";
        if (closeButton != null) closeButton.gameObject.SetActive(true);
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void OpenCodexAndClose()
    {
        CocktailCodexPanelController targetCodexPanel = GetCodexPanel();
        if (targetCodexPanel == null)
        {
            Debug.LogWarning("[DayResultPanel] CocktailCodexPanelController not found. Result panel remains open.");
            return;
        }

        targetCodexPanel.Open();
    }

    private void Restart()
    {
        if (navigationRequested) return;
        navigationRequested = true;
        GameManager.ContinueDemoGameplay();
    }

    private void ReturnToTitle()
    {
        if (navigationRequested) return;
        navigationRequested = true;
        GameManager.ReturnToDemoEntry();
    }

    private string BuildUnlockedRecipeText(IReadOnlyList<string> unlockedRecipeNames)
    {
        if (unlockedRecipeNames == null || unlockedRecipeNames.Count == 0)
            return "새로 기록된 칵테일: 0개";

        return $"새로 기록된 칵테일: {string.Join(", ", unlockedRecipeNames)}";
    }

    private string BuildNewRecipeText(IReadOnlyList<string> newlyKnownRecipeNames)
    {
        if (newlyKnownRecipeNames == null || newlyKnownRecipeNames.Count == 0)
            return NewRecipesRecordedPrefix + ": 0";

        return NewRecipesRecordedPrefix + ": " + string.Join(", ", newlyKnownRecipeNames);
    }

    private void ConfigureDemoNavigationButton()
    {
        if (restartButton == null)
            return;

        SetButtonLabel(
            restartButton,
            isDemoCompletionResult ? ReturnToTitleButtonLabel : NextNightButtonLabel);
        restartButton.onClick.RemoveListener(Restart);
        restartButton.onClick.RemoveListener(ReturnToTitle);
        restartButton.onClick.AddListener(isDemoCompletionResult ? ReturnToTitle : Restart);
    }

    private void BindButtons()
    {
        if (restartButton != null)
        {
            SetButtonLabel(restartButton, RestartButtonLabel);
            restartButton.onClick.RemoveListener(Restart);
            restartButton.onClick.AddListener(Restart);
        }

        if (closeButton != null)
        {
            SetButtonLabel(closeButton, CodexButtonLabel);
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.RemoveListener(OpenCodexAndClose);
            closeButton.onClick.AddListener(OpenCodexAndClose);
        }
    }

    private CocktailCodexPanelController GetCodexPanel()
    {
        if (codexPanel == null)
            codexPanel = FindFirstObjectByType<CocktailCodexPanelController>(FindObjectsInactive.Include);

        return codexPanel;
    }

    private void SetButtonLabel(Button button, string label)
    {
        TextMeshProUGUI labelText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (labelText != null)
            labelText.text = label;
    }

    private void EnsurePanel()
    {
        if (panelRoot != null)
            return;

        if (TryUseAttachedRectTransformAsRoot())
            return;

        BuildRuntimePanel();
    }

    private bool TryUseAttachedRectTransformAsRoot()
    {
        if (GetComponent<RectTransform>() == null)
            return false;

        panelRoot = gameObject;
        return true;
    }

    private void BuildRuntimePanel()
    {
        GameObject canvasObject = new GameObject("DayResultCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        panelRoot = CreateRectObject("DayResultPanel", canvas.transform);
        RectTransform rootRect = panelRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image rootImage = panelRoot.AddComponent<Image>();
        rootImage.color = new Color(0f, 0f, 0f, 0.56f);

        GameObject content = CreateRectObject("ResultContent", panelRoot.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(560f, 360f);

        Image contentImage = content.AddComponent<Image>();
        contentImage.color = new Color(0.12f, 0.1f, 0.08f, 0.96f);

        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(32, 32, 28, 28);
        contentLayout.spacing = 14f;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        titleText = CreateText("Title", content.transform, 34f, TextAlignmentOptions.Center);
        reputationText = CreateText("Reputation", content.transform, 22f, TextAlignmentOptions.Left);
        customerCountText = CreateText("CustomerCount", content.transform, 22f, TextAlignmentOptions.Left);
        unlockedRecipesText = CreateText("UnlockedRecipes", content.transform, 20f, TextAlignmentOptions.Left);
        unlockedRecipesText.textWrappingMode = TextWrappingModes.Normal;

        GameObject buttonRow = CreateRectObject("ButtonRow", content.transform);
        HorizontalLayoutGroup buttonLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
        buttonLayout.spacing = 12f;
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonLayout.childForceExpandWidth = false;
        buttonLayout.childForceExpandHeight = false;

        LayoutElement rowLayout = buttonRow.AddComponent<LayoutElement>();
        rowLayout.minHeight = 52f;
        rowLayout.preferredHeight = 52f;

        restartButton = CreateButton("RestartButton", RestartButtonLabel, buttonRow.transform);
        closeButton = CreateButton("CloseButton", CodexButtonLabel, buttonRow.transform);
    }

    private GameObject CreateRectObject(string objectName, Transform parent)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private TextMeshProUGUI CreateText(string objectName, Transform parent, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject obj = CreateRectObject(objectName, parent);
        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(0.96f, 0.92f, 0.84f, 1f);

        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.minHeight = Mathf.CeilToInt(fontSize * 1.5f);
        layout.preferredHeight = Mathf.CeilToInt(fontSize * 1.8f);

        return text;
    }

    private Button CreateButton(string objectName, string label, Transform parent)
    {
        GameObject obj = CreateRectObject(objectName, parent);
        Image image = obj.AddComponent<Image>();
        image.color = new Color(0.82f, 0.66f, 0.38f, 1f);

        Button button = obj.AddComponent<Button>();
        button.targetGraphic = image;

        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.minWidth = 160f;
        layout.preferredWidth = 180f;
        layout.minHeight = 44f;
        layout.preferredHeight = 44f;

        TextMeshProUGUI labelText = CreateText("Label", obj.transform, 20f, TextAlignmentOptions.Center);
        labelText.text = label;
        labelText.color = new Color(0.08f, 0.06f, 0.04f, 1f);
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return button;
    }
}
