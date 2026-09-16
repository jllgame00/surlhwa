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
        SurlhwaGameProgress.ResetGameProgress();
        BusinessDayPersistence.SaveCurrentDay(1);
        ContinueDemoGameplay();
    }

    private void ContinueGame()
    {
        if (!SurlhwaGameProgress.HasMeaningfulProgress())
            return;

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

        GameObject canvasObject = new GameObject("DemoEntryCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        entryPanel = CreateRectObject("DemoEntryPanel", canvasObject.transform);
        RectTransform panelRect = entryPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image overlay = entryPanel.AddComponent<Image>();
        overlay.color = new Color(0.04f, 0.035f, 0.05f, 0.96f);
        overlay.raycastTarget = true;

        GameObject content = CreateRectObject("DemoEntryContent", entryPanel.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(460f, 300f);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(36, 36, 32, 32);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Image contentImage = content.AddComponent<Image>();
        contentImage.color = new Color(0.12f, 0.1f, 0.09f, 1f);

        CreateEntryText("Title", "SURLHWA", 42f, content.transform);
        CreateEntryText("Subtitle", "Production Demo", 20f, content.transform);
        CreateEntryButton("NewGameButton", "New Game", BeginNewGame, true, content.transform);
        CreateEntryButton(
            "ContinueButton",
            "Continue",
            ContinueGame,
            SurlhwaGameProgress.HasMeaningfulProgress(),
            content.transform);
    }

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
        return button;
    }
}
