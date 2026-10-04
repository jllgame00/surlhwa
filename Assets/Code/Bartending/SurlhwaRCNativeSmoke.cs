#if UNITY_EDITOR || SURLHWA_RC_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Standalone observation only. Normal builds contain none of this code.</summary>
public sealed class SurlhwaRCNativeSmoke : MonoBehaviour
{
    const string Folder = "Logs/Review/RCAutomatedQA/standalone-native";
    readonly HashSet<int> observed = new();
    float nextSnapshot;
    static readonly KeyCode[] Keys = { KeyCode.M, KeyCode.T, KeyCode.I, KeyCode.D, KeyCode.J, KeyCode.S,
        KeyCode.P, KeyCode.G, KeyCode.Space, KeyCode.Escape, KeyCode.Return, KeyCode.LeftArrow, KeyCode.RightArrow };
    [Serializable] sealed class Control
    {
        public string name, label;
        public int id;
        public bool interactable;
        public float x, y, width, height;
    }
    [Serializable] sealed class State
    {
        public int frame, day, departures, width, height, iceActions;
        public bool focused, settings, measuring, transferring, ice;
        public string scene, route, customer, modal, keyboardSource, topRaycast, selectedIngredient;
        public float ml, jiggerMl, legacyMouseX, legacyMouseY, uiMouseX, uiMouseY;
        public List<Control> controls = new();
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch()
    {
        var args = Environment.GetCommandLineArgs();
        if (!args.Contains("--surlhwa-rc-native") && !args.Contains("--surlhwa-rc-focused") && !args.Contains("--surlhwa-rc-stress")) return;
        if (Application.companyName != "SURLHWA_RC_AUTOMATED_QA")
            throw new InvalidOperationException("RC flags require isolated build preferences.");
        if (args.Contains("--surlhwa-rc-focused") || args.Contains("--surlhwa-rc-stress"))
            new GameObject("RC input suite").AddComponent<SurlhwaRCGameplayQA>();
        else
        {
            var observer = new GameObject("RC native observation").AddComponent<SurlhwaRCNativeSmoke>();
            if (args.Contains("--surlhwa-rc-ui-device")) observer.gameObject.AddComponent<SurlhwaRCUIDevice>();
        }
    }
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "/delivery.jsonl", "");
        Application.logMessageReceived += Harvest;
    }
    IEnumerator Start()
    {
        for (int n = 0; n < 30; n++) yield return null;
        string corpusPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "korean-required.txt");
        if (File.Exists(corpusPath))
        {
            var font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f => f.name == "KoreanFallback");
            string corpus = File.ReadAllText(corpusPath);
            string missing = font == null ? corpus : new string(corpus.Where(c => !font.HasCharacter(c, true, true)).ToArray());
            string result = "[RC player font] " + (missing.Length == 0 ? "PASS" : "FAIL") +
                " characters=" + corpus.Length + " missing=" + missing + " source=" + font?.sourceFontFile?.name;
            File.WriteAllText(Folder + "/korean-glyphs.txt", result);
            if (missing.Length == 0) Debug.Log(result); else Debug.LogError(result);
        }
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.GetFullPath(Folder + "/startup.png"));
    }
    void Harvest(string message, string stack, LogType kind)
    {
        if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert || kind == LogType.Warning)
            File.AppendAllText(Folder + "/warnings-errors.txt", kind + ": " + message + "\n" + stack + "\n");
    }
    public static void Record(string label)
    {
        File.AppendAllText(Folder + "/delivery.jsonl", "{\"frame\":" + Time.frameCount + ",\"event\":\"" +
            label.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ") + "\"}\n");
    }
    void Update()
    {
        foreach (var key in Keys) if (Input.GetKeyDown(key)) Record("LEGACY_KEY_DOWN " + key + " source=" + SurlhwaGameplayInput.SourceName);
        if (Keyboard.current != null)
            foreach (var key in Keyboard.current.allKeys)
                if (key.wasPressedThisFrame) Record("INPUT_SYSTEM_KEY_DOWN " + key.name);
        if (Input.GetMouseButtonDown(0)) Record("LEGACY_POINTER_DOWN " + Input.mousePosition);
        if (Input.GetMouseButtonUp(0)) Record("LEGACY_POINTER_UP " + Input.mousePosition);
        if (Time.realtimeSinceStartup < nextSnapshot) return;
        nextSnapshot = Time.realtimeSinceStartup + .15f;
        var method = SurlhwaRCGameplayQA.Method();
        var shelf = SurlhwaRCGameplayQA.Find<IngredientShelfController>();
        var day = SurlhwaRCGameplayQA.Find<BusinessDayManager>();
        var guest = SurlhwaRCGameplayQA.Find<CustomerController>();
        var pointer = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        var hits = new List<RaycastResult>();
        if (EventSystem.current != null) EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = pointer }, hits);
        var state = new State {
            frame = Time.frameCount, day = day != null ? day.CurrentDay : 0,
            departures = day != null ? day.ServedCustomerCount : 0, width = Screen.width, height = Screen.height,
            focused = Application.isFocused, scene = SceneManager.GetActiveScene().name,
            route = method != null ? method.GetType().Name + "/" + SurlhwaRCGameplayQA.Get(method, "state") : "Bar",
            customer = guest != null ? guest.name + "/" + guest.CurrentState : "none",
            modal = SurlhwaModalSurface.Top != null ? SurlhwaModalSurface.Top.name : "none",
            settings = DemoAudioController.IsSettingsOpen, keyboardSource = SurlhwaGameplayInput.SourceName,
            measuring = shelf != null && shelf.Measuring, transferring = shelf != null && shelf.PhysicalTransferActive,
            ml = shelf?.TargetContainer?.CurrentLiquidVolumeMl ?? 0, jiggerMl = shelf?.Jigger?.CurrentLiquidVolumeMl ?? 0,
            ice = shelf?.TargetContainer?.HasIce ?? false, legacyMouseX = Input.mousePosition.x, legacyMouseY = Input.mousePosition.y,
            iceActions = shelf?.PreparationTrace?.Actions.Count(a => a.Action == RecipePreparationAction.AddIce) ?? 0,
            selectedIngredient = shelf?.SelectedIngredient?.ingredientId ?? "none",
            uiMouseX = pointer.x, uiMouseY = pointer.y, topRaycast = hits.Count > 0 ? hits[0].gameObject.name : "none" };
        foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            if (observed.Add(button.GetInstanceID()))
            {
                button.gameObject.AddComponent<SurlhwaRCNativeButtonProbe>();
                string name = button.name;
                button.onClick.AddListener(() => Record("BUTTON_CALLBACK " + name));
            }
            var rect = (RectTransform)button.transform;
            var canvas = rect.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            state.controls.Add(new Control { name = button.name, id = button.GetInstanceID(),
                label = button.GetComponentInChildren<TMP_Text>()?.text ?? "", interactable = button.IsInteractable(),
                x = point.x, y = point.y, width = rect.rect.width, height = rect.rect.height });
        }
        File.WriteAllText(Folder + "/state.json", JsonUtility.ToJson(state, true));
        string capture = Folder + "/capture.txt";
        if (File.Exists(capture))
        {
            string label = File.ReadAllText(capture).Trim(); File.Delete(capture);
            if (label.All(c => char.IsLetterOrDigit(c) || c == '-')) ScreenCapture.CaptureScreenshot(Path.GetFullPath(Folder + "/" + label + ".png"));
        }
    }
    void OnDestroy() => Application.logMessageReceived -= Harvest;
}
sealed class SurlhwaRCNativeButtonProbe : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    void Log(string edge, PointerEventData data) => SurlhwaRCNativeSmoke.Record(edge + " " + name +
        " position=" + data.position + " ray=" + data.pointerCurrentRaycast.gameObject +
        " press=" + data.pointerPress + " eligible=" + data.eligibleForClick);
    public void OnPointerDown(PointerEventData data) => Log("POINTER_DOWN", data);
    public void OnPointerUp(PointerEventData data) => Log("POINTER_UP", data);
    public void OnPointerClick(PointerEventData data) => Log("POINTER_CLICK", data);
}
#endif
