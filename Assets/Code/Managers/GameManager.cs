using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
public class GameManager : MonoBehaviour
{
    public int reputation { get; private set; }

    private UIManager ui;
    private static bool launchGameplayOnNextSceneLoad;
    private bool gameplaySessionStarted;
    private GameObject entryPanel;
    private GameObject newGameConfirmation;
    private bool entryTransition;
    private bool entryHadProgress;

    public bool IsGameplaySessionStarted => gameplaySessionStarted;

    private void Awake()
    {
        gameplaySessionStarted = launchGameplayOnNextSceneLoad;
        launchGameplayOnNextSceneLoad = false;

        ui = FindFirstObjectByType<UIManager>();
        SyncUI();

        if (!gameplaySessionStarted)
            BuildEntryPanel();
    }

    public void OnOrderResolved(bool success)
    {
        if (!success) return;

        reputation += 1;

        if (ui != null)
            ui.SetReputation(reputation);
    }

    public void AddReputation(int amount)
    {
        reputation += amount;
        if (ui != null) ui.SetReputation(reputation);
    }


    private void SyncUI()
    {
        if (ui != null)
            ui.SetReputation(reputation);
    }

    /// <summary>
    /// Leaves a completed run at the same minimal entry surface used for New
    /// Game and Continue. No title-screen art or broader menu framework is
    /// introduced here.
    /// </summary>
    public static void ReturnToDemoEntry()
    {
        ReloadCurrentScene(false);
    }

    /// <summary>
    /// Continues an already selected demo run after a normal night result.
    /// This is separate from returning to the entry surface after completion.
    /// </summary>
    public static void ContinueDemoGameplay()
    {
        ReloadCurrentScene(true);
    }

    private void BeginNewGame()
    {
        if (entryTransition) return;
        // Default recipe initialization later in Awake may write its baseline.
        // Confirm only the saved run observed when this entry surface opened.
        if (entryHadProgress) { ShowNewGameConfirmation(); return; }
        ConfirmNewGame();
    }

    private void ConfirmNewGame()
    {
        if (entryTransition) return;
        entryTransition = true;
        SurlhwaGameProgress.ResetGameProgress();
        BusinessDayPersistence.SaveCurrentDay(1);
        ContinueDemoGameplay();
    }

    private void ContinueGame()
    {
        if (entryTransition || !SurlhwaGameProgress.HasMeaningfulProgress())
            return;
        entryTransition = true;
        ReloadCurrentScene(true);
    }

    private static void ReloadCurrentScene(bool startGameplay)
    {
        launchGameplayOnNextSceneLoad = startGameplay;

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
            return;
        }

        if (!string.IsNullOrWhiteSpace(activeScene.path))
            SceneManager.LoadScene(activeScene.path);
    }

    private void BuildEntryPanel()
    {
        if (entryPanel != null)
            return;
        entryHadProgress = SurlhwaGameProgress.HasMeaningfulProgress();

        GameObject canvasObject = new GameObject("DemoEntryCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = .5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        entryPanel = CreateRectObject("DemoEntryPanel", canvasObject.transform);
        RectTransform panelRect = entryPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image overlay = entryPanel.AddComponent<Image>();
        overlay.color = Color.clear;
        overlay.raycastTarget = true;

        GameObject content = CreateRectObject("DemoEntryContent", entryPanel.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(460f, 400f);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(36, 36, 32, 32);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Image contentImage = content.AddComponent<Image>();
        contentImage.color = SurlhwaUITheme.Surface;

        CreateEntryText("Title", "SURLHWA", 42f, content.transform);
        CreateEntryText("Subtitle", "기억이 남는 바", 20f, content.transform);
        CreateEntryButton("NewGameButton", "새 게임", BeginNewGame, true, content.transform);
        CreateEntryButton(
            "ContinueButton",
            !SurlhwaGameProgress.HasMeaningfulProgress() ? "이어하기 · 저장 기록 없음" : CampaignProgressPersistence.EndingRead ? "이어하기 · 이야기 이후" : "이어하기 · " + BusinessDayPersistence.LoadCurrentDay(1) + "번째 밤",
            ContinueGame,
            SurlhwaGameProgress.HasMeaningfulProgress(),
            content.transform);
        DemoAudioController audio = GetComponent<DemoAudioController>();
        if (audio != null)
            CreateEntryButton("SoundSettingsButton", "소리 설정", audio.ToggleSettings, true, content.transform);
        SurlhwaUITheme.Panel(content);
        SurlhwaModalSurface.Configure(entryPanel, 2000, null);
    }

    private void ShowNewGameConfirmation()
    {
        if (newGameConfirmation != null) { newGameConfirmation.SetActive(true); return; }
        newGameConfirmation = CreateRectObject("NewGameConfirmation", entryPanel.transform);
        SurlhwaUITheme.Place(newGameConfirmation.GetComponent<RectTransform>(), new Vector2(480, 340), Vector2.zero);
        newGameConfirmation.AddComponent<Image>(); SurlhwaUITheme.Panel(newGameConfirmation);
        var layout = newGameConfirmation.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 24, 24); layout.spacing = 10;
        layout.childControlHeight = true; layout.childForceExpandHeight = false;
        CreateEntryText("Title", "처음부터 시작할까요?", 26, newGameConfirmation.transform);
        var explanation = CreateEntryText("Explanation", "지금까지의 이야기와 레시피 기록을 지우고\n첫날부터 다시 시작합니다.", 20, newGameConfirmation.transform);
        explanation.GetComponent<LayoutElement>().preferredHeight = 72;
        CreateEntryButton("ConfirmNewGameButton", "기록을 지우고 시작", ConfirmNewGame, true, newGameConfirmation.transform);
        CreateEntryButton("CancelNewGameButton", "돌아가기", CloseNewGameConfirmation, true, newGameConfirmation.transform);
        SurlhwaModalSurface.Configure(newGameConfirmation, 2100, CloseNewGameConfirmation);
    }
    private void CloseNewGameConfirmation() { if (newGameConfirmation != null) newGameConfirmation.SetActive(false); }

    private static GameObject CreateRectObject(string objectName, Transform parent)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static TextMeshProUGUI CreateEntryText(
        string objectName,
        string value,
        float fontSize,
        Transform parent)
    {
        GameObject obj = CreateRectObject(objectName, parent);
        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.96f, 0.92f, 0.84f, 1f);
        SurlhwaUITheme.Text(text, fontSize);

        LayoutElement element = obj.AddComponent<LayoutElement>();
        element.minHeight = Mathf.CeilToInt(fontSize * 1.45f);
        element.preferredHeight = Mathf.CeilToInt(fontSize * 1.65f);
        return text;
    }

    private static Button CreateEntryButton(
        string objectName,
        string label,
        UnityEngine.Events.UnityAction action,
        bool interactable,
        Transform parent)
    {
        GameObject obj = CreateRectObject(objectName, parent);
        Image image = obj.AddComponent<Image>();
        image.color = new Color(0.82f, 0.66f, 0.38f, 1f);

        Button button = obj.AddComponent<Button>();
        button.targetGraphic = image;
        button.interactable = interactable;
        button.onClick.AddListener(action);

        LayoutElement element = obj.AddComponent<LayoutElement>();
        element.minHeight = 52f;
        element.preferredHeight = 52f;

        TextMeshProUGUI buttonLabel = CreateEntryText("Label", label, 22f, obj.transform);
        buttonLabel.color = new Color(0.08f, 0.06f, 0.04f, 1f);
        RectTransform labelRect = buttonLabel.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        SurlhwaUITheme.Button(button, objectName == "ContinueButton" || objectName == "ConfirmNewGameButton" || (objectName == "NewGameButton" && !SurlhwaGameProgress.HasMeaningfulProgress()));
        return button;
    }
}
