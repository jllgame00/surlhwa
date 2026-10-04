using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scene-owned presentation only. Reads accepted preparation actions through
/// the coordinator's explicit runner handoff; never evaluates or changes drinks.
/// </summary>
public sealed class DemoAudioController : MonoBehaviour
{
    public const string MasterKey = "SURLHWA_AUDIO_MASTER";
    public const string MusicKey = "SURLHWA_AUDIO_MUSIC";
    public const string EffectsKey = "SURLHWA_AUDIO_EFFECTS";
    public const string MuteKey = "SURLHWA_AUDIO_MUTED";
    [SerializeField] private AudioClip musicClip, ambienceClip, pourClip, iceClip, glassClip, doorClip, pageClip;
    [SerializeField] private Button settingsButton;
    public static DemoAudioController Current { get; private set; }
    public SurlhwaSoundscape Sounds { get; private set; }
    private InteractionUIController readingUI;
    private BartendingAudioPresentation preparationAudio;
    private GameObject settingsRoot;
    private TMP_Text muteLabel;
    private bool settingsOpen;
    private static DemoAudioController settingsOwner;
    public static bool IsSettingsOpen => settingsOwner != null && settingsOwner.settingsOpen;
    public bool Muted => SurlhwaPlayerPrefsStore.GetInt(MuteKey, 0) != 0;

    private void Awake()
    {
        Current = this;
        Sounds = gameObject.AddComponent<SurlhwaSoundscape>();
        readingUI = FindFirstObjectByType<InteractionUIController>();
        ApplyVolumes();
        if (settingsButton != null) settingsButton.onClick.AddListener(ToggleSettings);
    }
    private void Start()
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects()) BindButtons(root);
        SurlhwaUITheme.CornerButton(settingsButton, true, () => IsSettingsOpen);
        var codex = FindFirstObjectByType<CocktailCodexPanelController>();
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
                if (button.name == "CodexButton") SurlhwaUITheme.CornerButton(button, false, () => codex != null && codex.IsOpen);
    }
    public void ObserveBartending(MiniGameManager runner)
    {
        if (preparationAudio != null) preparationAudio.Unbind();
        preparationAudio = null;
        Sounds.StopPhysical();
        if (runner != null)
        {
            preparationAudio = runner.GetComponent<BartendingAudioPresentation>();
            if (preparationAudio == null) preparationAudio = runner.gameObject.AddComponent<BartendingAudioPresentation>();
            preparationAudio.Bind(Sounds);
        }
        if (runner != null)
        {
            SurlhwaBartendingPresentation.AddSettings(runner.gameObject, ToggleSettings);
            BindButtons(runner.gameObject);
        }
    }
    private void BindButtons(GameObject root)
    {
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            // Physical actions and dedicated utility owners already emit their own cues.
            if (button.name.Contains("Sound") || button.name.Contains("Settings") || button.name.Contains("Codex") ||
                button.GetComponentInParent<CocktailCodexPanelController>() != null ||
                button.GetComponentInParent<MiniGameManager>() != null) continue;
            var feedback = button.GetComponent<DemoAudioClickFeedback>();
            if (feedback == null) feedback = button.gameObject.AddComponent<DemoAudioClickFeedback>();
            feedback.Bind(this, button);
        }
    }
    public void PlayPage() => Sounds?.Play(BarSound.Select);
    public void PlayDoor() => Sounds?.Play(BarSound.EntranceAir);

    private void Update()
    {
        Sounds.Reading = readingUI != null && readingUI.IsReadingDialogue;
        if (Input.GetKeyDown(KeyCode.Escape) && !SurlhwaModalSurface.Back()) ToggleSettings();
    }
    public void SetVolume(string key, float value)
    {
        if (key != MasterKey && key != MusicKey && key != EffectsKey) return;
        SurlhwaPlayerPrefsStore.SetInt(key, Mathf.RoundToInt(Mathf.Clamp(value, 0, 100)));
        SurlhwaPlayerPrefsStore.Save();
        ApplyVolumes();
    }
    public void ToggleMute()
    {
        SurlhwaPlayerPrefsStore.SetInt(MuteKey, Muted ? 0 : 1);
        SurlhwaPlayerPrefsStore.Save();
        ApplyVolumes();
        if (muteLabel != null) muteLabel.text = Muted ? "소리 켜기" : "모든 소리 끄기";
    }
    private float Volume(string key, int fallback) =>
        Mathf.Clamp(SurlhwaPlayerPrefsStore.GetInt(key, fallback), 0, 100) / 100f;
    private void ApplyVolumes()
    {
        Sounds?.SetMix(Volume(MasterKey, 70), Volume(MusicKey, 35), Volume(EffectsKey, 60), Muted);
    }
    public void ToggleSettings()
    {
        SurlhwaModalSurface.Trace("settings.toggle.entry open=" + settingsOpen);
        if (settingsOpen) { CloseSettings(); return; }
        if (settingsRoot == null) BuildSettings();
        Sounds?.Play(BarSound.Select);
        settingsOpen = true;
        settingsOwner = this;
        settingsRoot.SetActive(true);
        SurlhwaModalSurface.Trace("settings.open.complete active=" + settingsRoot.activeInHierarchy);
    }
    private void CloseSettings()
    {
        SurlhwaModalSurface.Trace("settings.close.entry open=" + settingsOpen);
        if (!settingsOpen) return;
        Sounds?.Play(BarSound.Back);
        settingsOpen = false;
        if (settingsRoot != null) settingsRoot.SetActive(false);
        if (settingsOwner == this) settingsOwner = null;
        SurlhwaModalSurface.Trace("settings.close.complete active=" + (settingsRoot != null && settingsRoot.activeInHierarchy));
    }
    private void OnDisable() => CloseSettings();
    private void OnDestroy()
    {
        CloseSettings();
        if (Current == this) Current = null;
        if (preparationAudio != null) preparationAudio.Unbind();
        if (settingsButton != null) settingsButton.onClick.RemoveListener(ToggleSettings);
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 size)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        return rect;
    }
    private static TMP_Text Text(string name, Transform parent, string value, float y)
    {
        var rect = Rect(name, parent, new Vector2(420, 40));
        rect.anchoredPosition = new Vector2(0, y);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = 22;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(.96f, .92f, .84f);
        text.raycastTarget = false;
        return text;
    }
    private void BuildSettings()
    {
        var canvas = new GameObject("SoundSettingsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.transform.SetParent(transform, false);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.GetComponent<Canvas>().sortingOrder = 2500;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;
        var backdrop = Rect("SoundSettings", canvas.transform, Vector2.zero);
        backdrop.anchorMin = Vector2.zero; backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
        backdrop.gameObject.AddComponent<Image>().color = Color.clear;
        settingsRoot = backdrop.gameObject;
        var panel = Rect("SoundPanel", backdrop, new Vector2(520, 490));
        panel.gameObject.AddComponent<Image>().color = new Color(.12f, .10f, .09f, 1);
        SurlhwaUITheme.Text(Text("Title", panel, "소리 설정", 210), 26);
        Slider(panel, "전체", MasterKey, 70, 115);
        Slider(panel, "음악", MusicKey, 35, 20);
        Slider(panel, "효과와 바람", EffectsKey, 60, -75);
        muteLabel = Button(panel, "Mute", Muted ? "소리 켜기" : "모든 소리 끄기", -155, ToggleMute);
        Button(panel, "CloseSoundSettings", "돌아가기 · Esc", -210, CloseSettings);
        SurlhwaUITheme.Panel(panel.gameObject); SurlhwaUITheme.Tree(panel.gameObject);
        settingsRoot.SetActive(false);
        SurlhwaModalSurface.Configure(settingsRoot, 3000, CloseSettings, true);
    }
    private void Slider(Transform parent, string label, string key, int fallback, float y)
    {
        var valueLabel = Text(key + "Label", parent, label, y + 44);
        var track = Rect(key, parent, new Vector2(380, 32));
        track.anchoredPosition = new Vector2(0, y);
        track.gameObject.AddComponent<Image>().color = SurlhwaUITheme.Raised;
        var handle = Rect("Handle", track, new Vector2(24, 8));
        var image = handle.gameObject.AddComponent<Image>();
        image.color = new Color(.82f, .68f, .43f);
        var slider = track.gameObject.AddComponent<UnityEngine.UI.Slider>();
        slider.minValue = 0; slider.maxValue = 100; slider.wholeNumbers = true;
        slider.handleRect = handle; slider.targetGraphic = image;
        slider.SetValueWithoutNotify(Volume(key, fallback) * 100);
        valueLabel.text = label + " · " + slider.value.ToString("0") + "%";
        slider.onValueChanged.AddListener(value => { SetVolume(key, value); valueLabel.text = label + " · " + value.ToString("0") + "%"; });
    }
    private TMP_Text Button(Transform parent, string name, string label, float y, UnityEngine.Events.UnityAction action)
    {
        var rect = Rect(name, parent, new Vector2(380, 44));
        rect.anchoredPosition = new Vector2(0, y);
        rect.gameObject.AddComponent<Image>().color = new Color(.28f, .22f, .14f);
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.onClick.AddListener(action);
        return Text("Label", rect, label, 0);
    }
}

internal sealed class DemoAudioClickFeedback : MonoBehaviour
{
    private DemoAudioController audioOwner;
    private Button button;
    public void Bind(DemoAudioController owner, Button target)
    {
        if (button != null) button.onClick.RemoveListener(Play);
        audioOwner = owner; button = target;
        button.onClick.AddListener(Play);
    }
    private void Play()
    {
        if (button == null || !button.IsInteractable()) return;
        bool back = button.name.Contains("Back") || button.name.Contains("Close");
        audioOwner?.Sounds?.Play(back ? BarSound.Back : BarSound.Select);
    }
    private void OnDestroy() { if (button != null) button.onClick.RemoveListener(Play); }
}
