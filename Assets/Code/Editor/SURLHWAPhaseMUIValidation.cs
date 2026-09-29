#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Production UI fixtures. Seeded screens are not normal-play evidence.
/// Progress is restored on every exit, including an interrupted probe.</summary>
[InitializeOnLoad]
public static class SURLHWAPhaseMUIValidation
{
    const string Pending = "SURLHWA_PHASE_M_UI_PENDING";
    const string SnapshotPath = "Temp/PhaseM/progress.json";
    [Serializable] public class SavedInt { public string key; public bool exists; public int value; }
    [Serializable] public class Snapshot
    {
        public SavedInt[] integers;
        public bool hadKnowledge, hadLegacy;
        public string knowledge, legacy;
    }
    static SURLHWAPhaseMUIValidation() { EditorApplication.playModeStateChanged += Changed; }
    public static void RunBaseline() => Start("baseline");
    public static void Run() => Start("review");
    public static void RunNavigation() => Start("navigation");
    public static void RunInputModule() => Start("input-module");
    public static void RunModalSequence() => Start("modal-sequence");
    public static void RunPointerHandoffDiagnostic() => Start("pointer-handoff");
    public static void RunDiegetic() => Start("diegetic");
    public static void RunStagingBefore() => Start("staging-before");
    public static void RunStagingAfter() => Start("staging-after");
    public static void RunNotificationLayout() => Start("notification-layout");
    static void Start(string mode)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(Pending, false))
            throw new InvalidOperationException("Finish the existing probe first.");
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
            .Any(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Unsaved scene changes must be preserved.");
        var keys = SURLHWAJ5PrinceClosureValidation.Keys.Concat(SURLHWACampaignValidation.ProgressKeys())
            .Concat(new[] { "currentDay", "SURLHWA_DEMO_COMPLETE_V1", "SURLHWA_FIRST_RECIPE_REVEAL_SHOWN",
                DemoAudioController.MasterKey, DemoAudioController.MusicKey, DemoAudioController.EffectsKey, DemoAudioController.MuteKey }).Distinct();
        var save = new Snapshot {
            integers = keys.Select(k => new SavedInt { key = k, exists = SurlhwaPlayerPrefsStore.HasKey(k), value = SurlhwaPlayerPrefsStore.GetInt(k, 0) }).ToArray(),
            hadKnowledge = RecipeProgressPersistence.HasRecipeKnowledge(), knowledge = RecipeProgressPersistence.LoadRecipeKnowledgeRaw(),
            hadLegacy = RecipeProgressPersistence.HasLegacyUnlockedRecipeIds(), legacy = RecipeProgressPersistence.LoadLegacyUnlockedRecipeIdsRaw()
        };
        Directory.CreateDirectory("Temp/PhaseM");
        File.WriteAllText(SnapshotPath, JsonUtility.ToJson(save, true));
        SessionState.SetString(Pending + "Mode", mode);
        SessionState.SetBool(Pending, true);
        SurlhwaGameProgress.ResetGameProgress();
        EditorSceneManager.OpenScene("Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity");
        if (mode == "input-module" || mode == "modal-sequence" || mode == "pointer-handoff" || mode == "diegetic" || mode.StartsWith("staging-")) EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Pending, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
            EditorApplication.delayCall += () => new GameObject("Phase M UI probe").AddComponent<SURLHWAPhaseMUIRunner>();
        if (state != PlayModeStateChange.EnteredEditMode) return;
        var save = JsonUtility.FromJson<Snapshot>(File.ReadAllText(SnapshotPath));
        foreach (var entry in save.integers)
            if (entry.exists) SurlhwaPlayerPrefsStore.SetInt(entry.key, entry.value); else SurlhwaPlayerPrefsStore.DeleteKey(entry.key);
        if (save.hadKnowledge) RecipeProgressPersistence.SaveRecipeKnowledgeRaw(save.knowledge); else RecipeProgressPersistence.ClearRecipeKnowledge();
        if (save.hadLegacy) RecipeProgressPersistence.SaveUnlockedRecipeIdsRaw(save.legacy); else RecipeProgressPersistence.ClearLegacyUnlockedRecipeIds();
        SurlhwaPlayerPrefsStore.Save();
        bool restored = save.integers.All(e => e.exists == SurlhwaPlayerPrefsStore.HasKey(e.key) && (!e.exists || e.value == SurlhwaPlayerPrefsStore.GetInt(e.key, 0)))
            && save.hadKnowledge == RecipeProgressPersistence.HasRecipeKnowledge()
            && save.hadLegacy == RecipeProgressPersistence.HasLegacyUnlockedRecipeIds()
            && (!save.hadKnowledge || save.knowledge == RecipeProgressPersistence.LoadRecipeKnowledgeRaw())
            && (!save.hadLegacy || save.legacy == RecipeProgressPersistence.LoadLegacyUnlockedRecipeIdsRaw());
        if (!restored) throw new InvalidOperationException("Phase M progress restoration mismatch; snapshot retained.");
        SessionState.SetBool(Pending, false);
        Debug.Log("[Phase M UI] Progress and sound preferences restored and verified.");
    }
    internal static string Mode => SessionState.GetString(Pending + "Mode", "baseline");
}

internal sealed class SURLHWAPhaseMUIRunner : MonoBehaviour
{
    readonly Stack<IEnumerator> work = new();
    float began;
    bool previousBackground;
    bool runtimeError;
    UnityEngine.InputSystem.Mouse probeMouse;
    UnityEngine.InputSystem.Mouse handoffMouse;
    readonly List<string> pointerEvents = new();
    bool tracingPointer;
    readonly Queue<string> eventTrace = new();
    string traceRun, correlation = "setup";
    int attempt, droppedTraceEvents;
    bool traceEnabled;
    bool inputObservationPending;
    string lastModuleObservation;
    Button traceTarget;
    UnityEngine.InputSystem.UI.InputSystemUIInputModule traceModule;
    string TraceState()
    {
        var module = EventSystem.current != null ? EventSystem.current.currentInputModule : null;
        var input = module as UnityEngine.InputSystem.UI.InputSystemUIInputModule;
        var ui = Find<InteractionUIController>();
        var panel = ui != null ? Get(ui, "brewPanel") as GameObject : null;
        return " module=" + Identity(module) + "/" + (module != null ? module.GetType().Name : "NONE") +
            " pointEnabled=" + (input != null && input.point?.action?.enabled == true) +
            " clickEnabled=" + (input != null && input.leftClick?.action?.enabled == true) +
            " focus=" + Identity(EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null) +
            " appFocus=" + Application.isFocused + " editorFocus=" + EditorWindow.focusedWindow?.GetType().Name +
            " target=" + Identity(traceTarget) + " active=" + (traceTarget != null && traceTarget.IsActive()) +
            " interactable=" + (traceTarget != null && traceTarget.IsInteractable()) +
            " allowed=" + (traceTarget != null && SurlhwaModalSurface.Allows(traceTarget.transform)) +
            " scale=" + Time.timeScale + " paused=" + SurlhwaModalSurface.IsPaused +
            " top=" + Identity(SurlhwaModalSurface.Top) + " settings=" + DemoAudioController.IsSettingsOpen +
            " routeBusy=" + Find<BartendingFlowCoordinator>()?.IsBartendingActive +
            " selection=" + Identity(panel) + " visible=" + (panel != null && panel.activeInHierarchy);
    }
    static string Identity(UnityEngine.Object value) => value != null ? value.name + "#" + value.GetInstanceID() : "NONE";
    void Record(string boundary)
    {
        if (!traceEnabled) return;
        if (eventTrace.Count == 8192) { eventTrace.Dequeue(); droppedTraceEvents++; }
        eventTrace.Enqueue(correlation + " frame=" + Time.frameCount + " mono=" + Time.realtimeSinceStartupAsDouble.ToString("F6") +
            " unscaled=" + Time.unscaledTimeAsDouble.ToString("F6") + " " + boundary + TraceState());
    }
    void LateUpdate()
    {
        if (traceEnabled && tracingPointer && traceModule != null && probeMouse != null)
        {
            string observation = "module.afterProcess ray=" + Identity(traceModule.GetLastRaycastResult(probeMouse.deviceId).gameObject) +
                " pointers=" + ModulePointers() +
                " device=" + probeMouse.deviceId + " position=" + probeMouse.position.ReadValue() + " left=" + probeMouse.leftButton.ReadValue();
            if (observation != lastModuleObservation) Record(observation);
            lastModuleObservation = observation;
        }
    }
    void TraceAction(InputAction.CallbackContext context)
    {
        if (tracingPointer) Record("ui.action " + context.action.name + " phase=" + context.phase +
            " device=" + context.control?.device.deviceId + " control=" + context.control?.name);
    }
    string ModulePointers()
    {
        // Read the installed Input System 1.18.0 pointer records, never mutate them.
        var states = Get(traceModule, "m_PointerStates") as IEnumerable;
        if (states == null) return "UNAVAILABLE";
        var values = new List<string>();
        foreach (object state in states)
        {
            var data = (UnityEngine.InputSystem.UI.ExtendedPointerEventData)Get(state, "eventData");
            values.Add("id=" + data.pointerId + "/device=" + data.device?.deviceId + "/pos=" + data.position +
                "/ray=" + Identity(data.pointerCurrentRaycast.gameObject));
        }
        return string.Join(";", values);
    }
    void TraceInputProcessed()
    {
        if (traceEnabled && tracingPointer && inputObservationPending && probeMouse != null)
        {
            inputObservationPending = false;
            Record("input.afterUpdate device=" + probeMouse.deviceId + " position=" + probeMouse.position.ReadValue() +
                " left=" + probeMouse.leftButton.ReadValue() + " update=" + InputState.currentUpdateType);
        }
    }
    void BindTraceActions(bool bind)
    {
        if (traceModule == null) return;
        foreach (var action in new[] { traceModule.point?.action, traceModule.leftClick?.action })
        {
            if (action == null) continue;
            if (bind) { action.performed += TraceAction; action.canceled += TraceAction; }
            else { action.performed -= TraceAction; action.canceled -= TraceAction; }
        }
    }
    void QueuePointer(Vector2 point, ushort buttons)
    {
        Record("input.queue device=" + probeMouse.deviceId + " position=" + point + " left=" + buttons);
        InputSystem.QueueStateEvent(probeMouse, new MouseState { position = point, buttons = buttons });
    }
    static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static T Find<T>() where T : UnityEngine.Object => FindFirstObjectByType<T>();
    static object Get(object owner, string field) => owner.GetType().GetField(field, Fields).GetValue(owner);
    static Button ButtonField(object owner, string field) => (Button)Get(owner, field);
    static Button Named(string name) => FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == name);
    static void Call(object owner, string method) => owner.GetType().GetMethod(method, Fields).Invoke(owner, null);
    static bool CanPointAt(Button button)
    {
        Canvas.ForceUpdateCanvases();
        var rect = (RectTransform)button.transform;
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) {
            position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) }, hits);
        return hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button && button.IsInteractable();
    }
    static void Click(Button button)
    {
        if (button == null || !button.gameObject.activeInHierarchy || !button.interactable) throw new InvalidOperationException("Missing active button");
        if (SURLHWAPhaseMUIValidation.Mode == "baseline") { button.onClick.Invoke(); return; }
        Canvas.ForceUpdateCanvases();
        var rect = (RectTransform)button.transform;
        var data = new PointerEventData(EventSystem.current) {
            position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
            button = PointerEventData.InputButton.Left, clickCount = 1, eligibleForClick = true };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, hits);
        if (hits.Count == 0 || hits[0].gameObject.GetComponentInParent<Button>() != button || !button.IsInteractable())
            throw new InvalidOperationException("Pointer cannot reach " + button.name + "; top=" + (hits.Count > 0 ? hits[0].gameObject.name : "NONE"));
        data.pointerPressRaycast = data.pointerCurrentRaycast = hits[0];
        data.pressPosition = data.position;
        data.pointerPress = ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, data, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
        Debug.Log("[Phase M Pointer] " + button.name + ": top raycast + pointer down/up/click. Programmatic input.");
    }
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        Application.logMessageReceived += OnLog;
        InputSystem.onEvent += TracePointerEvent;
        InputSystem.onAfterUpdate += TraceInputProcessed;
        previousBackground = Application.runInBackground;
        Application.runInBackground = true;
        traceEnabled = SessionState.GetBool("SURLHWA_UI_EVENT_TRACE", false);
        traceRun = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + "-" + SURLHWAPhaseMUIValidation.Mode;
        if (traceEnabled) SurlhwaModalSurface.DiagnosticTrace = Record;
        began = Time.realtimeSinceStartup;
        work.Push(Run());
        StartCoroutine(Guarded());
    }
    IEnumerator Guarded()
    {
        while (work.Count > 0)
        {
            object next = null;
            try
            {
                if (Time.realtimeSinceStartup - began > (SURLHWAPhaseMUIValidation.Mode == "modal-sequence" ? 600 : 300)) throw new TimeoutException("Phase M UI probe");
                if (!work.Peek().MoveNext()) { work.Pop(); continue; }
                next = work.Peek().Current;
                if (next is IEnumerator nested) { work.Push(nested); continue; }
            }
            catch (Exception e) { Record("probe.exception " + e.GetType().Name + " " + e.Message); Debug.LogException(e); Finish(false); yield break; }
            yield return next;
        }
        Finish(true);
    }
    void Finish(bool success)
    {
        success &= !runtimeError;
        Record("probe.finish success=" + success);
        if (traceEnabled)
        {
            Directory.CreateDirectory("Temp/PhaseM/EventTraces");
            File.WriteAllLines("Temp/PhaseM/EventTraces/" + traceRun + (success ? "-pass" : "-FAIL") + ".txt",
                new[] { "UTC run=" + traceRun + " screen=" + Screen.width + "x" + Screen.height +
                    " boundedEvents=8192 dropped=" + droppedTraceEvents + " input=PROGRAMMATIC" }.Concat(eventTrace));
        }
        Debug.Log("[Phase M UI] " + SURLHWAPhaseMUIValidation.Mode + " " + (success ? "COMPLETE" : "FAILED") + ": staged production-screen fixtures; callback invocation and raycast observations, not human input.");
        File.AppendAllText("Temp/PhaseM/validation-results.txt", DateTime.UtcNow.ToString("O") + " UI " + SURLHWAPhaseMUIValidation.Mode + " " + success + "\n");
        Application.runInBackground = previousBackground;
        EditorApplication.ExitPlaymode();
    }
    void OnLog(string message, string stackTrace, LogType type)
    { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) runtimeError = true; }
    void OnDestroy()
    {
        Application.logMessageReceived -= OnLog;
        InputSystem.onEvent -= TracePointerEvent;
        InputSystem.onAfterUpdate -= TraceInputProcessed;
        BindTraceActions(false);
        if (traceEnabled) SurlhwaModalSurface.DiagnosticTrace = null;
        if (probeMouse != null && probeMouse.added) UnityEngine.InputSystem.InputSystem.RemoveDevice(probeMouse);
        if (handoffMouse != null && handoffMouse.added) UnityEngine.InputSystem.InputSystem.RemoveDevice(handoffMouse);
    }
    IEnumerator Wait(Func<bool> ready)
    {
        float until = Time.realtimeSinceStartup + 30;
        while (!ready()) { if (Time.realtimeSinceStartup > until) throw new TimeoutException("UI arrival"); yield return null; }
    }
    IEnumerator Capture(string name)
    {
        yield return new WaitForSecondsRealtime(.15f);
        string prefix = "phase-m-" + SURLHWAPhaseMUIValidation.Mode + "-" + name;
        SURLHWAReviewCapture.Save(prefix, 1280, 720);
        SURLHWAReviewCapture.Save(prefix, 1920, 1080);
        // Offscreen capture temporarily changes canvas dimensions. Let the real
        // Game View scaler/projected targets settle before the next input event.
        yield return null;
        Canvas.ForceUpdateCanvases();
        var report = new List<string> { "Production screen fixture: " + name, "Button center raycasts; not physical input." };
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            report.Add("CANVAS\t" + canvas.name + "\tparent=" + canvas.transform.parent?.name + "\troot=" + canvas.rootCanvas.name + "\torder=" + canvas.sortingOrder + "\toverride=" + canvas.overrideSorting);
        foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            var point = RectTransformUtility.WorldToScreenPoint(null, ((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center));
            var data = new PointerEventData(EventSystem.current) { position = point };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            var top = hits.Count > 0 ? hits[0].gameObject : null;
            report.Add(button.name + "\t" + point + "\t" + button.interactable + "\tTOP=" + (top != null ? top.name : "NONE") + "\tMATCH=" + (top != null && top.GetComponentInParent<Button>() == button));
        }
        foreach (var text in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            report.Add("TEXT\t" + text.name + "\t" + text.fontSize + "\t" + text.rectTransform.rect + "\t" + text.text.Replace('\n', '|'));
        File.WriteAllLines("Logs/Review/" + prefix + ".txt", report);
    }
    IEnumerator SeedNight(int night)
    {
        SurlhwaGameProgress.ResetGameProgress();
        BusinessDayPersistence.SaveCurrentDay(night);
        if (night == 18) CampaignProgressPersistence.RecordDeparture(18, 2, CampaignVisitId.OdysseusStay);
        GameManager.ReturnToDemoEntry();
        yield return null;
        yield return Wait(() => Named("ContinueButton") != null);
        yield return new WaitForSecondsRealtime(.2f); // Let the new scene's CanvasScaler/layout tick.
        Click(Named("ContinueButton"));
        yield return null;
        yield return Wait(() => Find<GameManager>() != null && Find<GameManager>().IsGameplaySessionStarted);
    }
    IEnumerator Guest()
    {
        yield return Wait(() => Find<InteractionUIController>() != null && Get(Find<InteractionUIController>(), "activeCustomer") != null && ButtonField(Find<InteractionUIController>(), "ellipsisButton").gameObject.activeInHierarchy);
        yield return new WaitForSecondsRealtime(.2f); // Guest follow runs in LateUpdate, before the visible frame.
    }
    IEnumerator ReadToRequest()
    {
        var ui = Find<InteractionUIController>();
        Click(ButtonField(ui, "ellipsisButton"));
        yield return Capture("dialogue");
        for (int i = 0; i < 50 && ButtonField(ui, "nextButton").gameObject.activeInHierarchy; i++)
        {
            if ((bool)Get(ui, "isLittlePrinceChoiceActive")) yield return Capture("choice");
            Click(ButtonField(ui, "nextButton"));
            if (SURLHWAPhaseMUIValidation.Mode != "baseline" && ButtonField(ui, "nextButton").gameObject.activeInHierarchy && CanPointAt(ButtonField(ui, "nextButton")))
                throw new InvalidOperationException("Reading beat accepted an immediate second pointer click");
            yield return new WaitForSecondsRealtime(.26f);
        }
    }
    IEnumerator Run()
    {
        if (SURLHWAPhaseMUIValidation.Mode.StartsWith("staging-")) { yield return StagingFlow(); yield break; }
        if (SURLHWAPhaseMUIValidation.Mode == "diegetic") { yield return DiegeticFlow(); yield break; }
        if (SURLHWAPhaseMUIValidation.Mode == "input-module") { yield return InputModuleFlow(); yield break; }
        if (SURLHWAPhaseMUIValidation.Mode == "modal-sequence") { yield return ModalSequence(); yield break; }
        if (SURLHWAPhaseMUIValidation.Mode == "pointer-handoff") { yield return PointerHandoffDiagnostic(); yield break; }
        if (SURLHWAPhaseMUIValidation.Mode == "notification-layout") { yield return NotificationLayout(); yield break; }
        if (SURLHWAPhaseMUIValidation.Mode == "navigation") { yield return Navigation(); yield break; }
        yield return Capture("entry");
        if (Named("ContinueButton").IsInteractable()) throw new InvalidOperationException("Fresh entry allowed Continue");
        Find<DemoAudioController>().ToggleSettings();
        yield return Capture("settings");
        Find<DemoAudioController>().ToggleSettings();
        Click(Named("NewGameButton")); yield return null;
        yield return Guest();
        yield return Capture("idle-bar");
        yield return ReadToRequest();
        yield return Capture("request");
        var codex = Find<CocktailCodexPanelController>();
        codex.Open(); yield return Capture("codex-grid");
        var knowledge = Find<RecipeUnlockManager>();
        var recipe = knowledge.GetAllRecipes().First(r => r.recipeId == "recipe_dry_martini");
        codex.SelectRecipe(recipe); yield return Capture("codex-partial");
        codex.SelectRecipe(knowledge.GetAllRecipes().First(knowledge.IsKnown)); yield return Capture("codex-known");
        codex.Close();
        Click(ButtonField(Find<InteractionUIController>(), "brewButton"));
        yield return Wait(() => Find<IngredientShelfController>() != null);
        yield return Capture("bartending-stir");
        var guide = Find<RecipeGuidePanelController>();
        Click(ButtonField(guide, "openButton")); yield return Capture("guide-partial");
        if (SURLHWAPhaseMUIValidation.Mode != "baseline")
        {
            if (!guide.IsReading || Time.timeScale != 0) throw new InvalidOperationException("First Guide open must pause");
            Find<DemoAudioController>().ToggleSettings();
            yield return Capture("guide-settings");
            SurlhwaModalSurface.Back();
            if (Time.timeScale != 0 || !guide.IsReading) throw new InvalidOperationException("Child settings lost Guide pause");
        }
        Click(ButtonField(guide, "closeButton"));
        if (Time.timeScale != 1) throw new InvalidOperationException("Guide close did not restore original time");
        if (SURLHWAPhaseMUIValidation.Mode != "baseline")
        {
            for (int cycle = 0; cycle < 3; cycle++)
            {
                Time.timeScale = .5f;
                Click(Named("BartendingSettings")); yield return null;
                if (Time.timeScale != 0 || CanPointAt(ButtonField(guide, "openButton")))
                    throw new InvalidOperationException("Settings failed to shield/pause workspace");
                var slider = FindObjectsByType<Slider>(FindObjectsSortMode.None).First(s => s.name == DemoAudioController.MasterKey);
                var sliderRect = (RectTransform)slider.transform;
                var pointer = new PointerEventData(EventSystem.current) {
                    position = RectTransformUtility.WorldToScreenPoint(null, sliderRect.TransformPoint(new Vector2(sliderRect.rect.xMin + sliderRect.rect.width * .25f, 0))),
                    button = PointerEventData.InputButton.Left };
                var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
                if (hits.Count == 0 || hits[0].gameObject.GetComponentInParent<Slider>() != slider)
                    throw new InvalidOperationException("Volume slider cannot receive pointer");
                pointer.pointerCurrentRaycast = pointer.pointerPressRaycast = hits[0];
                ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(slider.gameObject, pointer, ExecuteEvents.pointerUpHandler);
                if (Mathf.Abs(slider.value - 25) > 1 || SurlhwaPlayerPrefsStore.GetInt(DemoAudioController.MasterKey, -1) != Mathf.RoundToInt(slider.value))
                    throw new InvalidOperationException("Pointer volume change was not saved");
                Click(Named("CloseSoundSettings")); yield return null;
                if (Time.timeScale != .5f || SurlhwaModalSurface.Top != null || SurlhwaModalSurface.IsPaused)
                    throw new InvalidOperationException("Repeated Settings left a lock or lost prior time scale");
                Time.timeScale = 1;
            }
            Debug.Log("[Phase M Pointer] Repeated settings, shield, prior .5 time scale restoration and reading double-click boundary PASS.");
        }
        foreach (string method in new[] { "stirButton", "shakeButton", "buildButton", "floatButton" })
        {
            yield return SeedNight(5); yield return Guest(); yield return ReadToRequest();
            var ui = Find<InteractionUIController>();
            Click(ButtonField(ui, "brewButton")); yield return null; Click(ButtonField(ui, method));
            yield return Capture("method-" + method);
            Click(ButtonField(ui, "cancelButton")); yield return new WaitForSecondsRealtime(.26f);
            Click(ButtonField(ui, "brewButton")); yield return null; Click(ButtonField(ui, method));
            Click(ButtonField(ui, "confirmButton"));
            yield return Wait(() => Find<IngredientShelfController>() != null);
            yield return Capture("bartending-" + method);
            guide = Find<RecipeGuidePanelController>();
            Click(ButtonField(guide, "openButton")); yield return Capture("guide-" + method);
            Click(ButtonField(guide, "closeButton"));
        }
        yield return SeedNight(2); yield return Guest(); yield return ReadToRequest();
        var panel = FindFirstObjectByType<DayResultPanelController>(FindObjectsInactive.Include);
        panel.OpenResult(1, 4, 4, new[] { "네그로니" }); yield return Capture("night-summary");
        panel.OpenCodexAndClose(); yield return Capture("summary-codex");
        Find<CocktailCodexPanelController>().Close(); yield return Capture("summary-return");
        yield return Navigation();
    }
    IEnumerator ModuleClick(Button button, bool disabledProbe = false)
    {
        correlation = traceRun + "/" + (++attempt) + "/" + SafeName(button);
        traceTarget = button;
        Record("attempt.begin");
        TraceModal("before " + (button != null ? button.name : "MISSING"), button);
        if (button == null || (!disabledProbe && !CanPointAt(button))) throw new InvalidOperationException("Input-module target is blocked");
        string controlName = button.name;
        pointerEvents.Clear(); tracingPointer = true;
        inputObservationPending = false; lastModuleObservation = null;
        SURLHWAButtonEventObservation observer = null;
        if (traceEnabled)
        {
            // Button already implements these three interfaces. An observer on
            // that same object cannot introduce a new ancestor/child target.
            var down = ExecuteEvents.GetEventHandler<IPointerDownHandler>(button.gameObject);
            var up = ExecuteEvents.GetEventHandler<IPointerUpHandler>(button.gameObject);
            var click = ExecuteEvents.GetEventHandler<IPointerClickHandler>(button.gameObject);
            observer = button.gameObject.AddComponent<SURLHWAButtonEventObservation>();
            observer.record = Record;
            if (down != ExecuteEvents.GetEventHandler<IPointerDownHandler>(button.gameObject) ||
                up != ExecuteEvents.GetEventHandler<IPointerUpHandler>(button.gameObject) ||
                click != ExecuteEvents.GetEventHandler<IPointerClickHandler>(button.gameObject))
                throw new InvalidOperationException("Observation changed event target resolution");
            traceModule = EventSystem.current.currentInputModule as UnityEngine.InputSystem.UI.InputSystemUIInputModule;
            BindTraceActions(true);
        }
        int delivered = 0;
        UnityEngine.Events.UnityAction observed = () => { delivered++; Record("button.onClick.observer count=" + delivered); };
        button.onClick.AddListener(observed);
        var rect = (RectTransform)button.transform;
        var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        QueuePointer(point, 0);
        yield return null;
        QueuePointer(point, 1);
        yield return null;
        QueuePointer(point, 0);
        yield return new WaitForSecondsRealtime(.3f);
        Record("attempt.end callbacks=" + delivered);
        tracingPointer = false;
        BindTraceActions(false);
        if (observer != null) { observer.record = null; Destroy(observer); }
        if (button != null) button.onClick.RemoveListener(observed);
        Debug.Log("[Pointer delivery] " + controlName + " callbacks=" + delivered + " events=" + string.Join(";", pointerEvents));
        Debug.Log("[Phase M Input Module] virtual mouse down/up via existing InputSystemUIInputModule: " + controlName);
        TraceModal("after " + controlName, button);
        if (delivered != (disabledProbe ? 0 : 1))
            throw new InvalidOperationException("Input-module callback delivery mismatch: " + correlation + " callbacks=" + delivered);
    }
    void TracePointerEvent(InputEventPtr data, InputDevice device)
    {
        if (!tracingPointer || device is not Mouse mouse || pointerEvents.Count >= 16 ||
            (!data.IsA<StateEvent>() && !data.IsA<DeltaStateEvent>())) return;
        bool position = mouse.position.ReadValueFromEvent(data, out Vector2 point);
        bool button = mouse.leftButton.ReadValueFromEvent(data, out float pressed);
        inputObservationPending = true;
        Record("input.received.beforeApply device=" + device.deviceId + (device == probeMouse ? " virtual" : " external") +
            " eventTime=" + data.time.ToString("F6") + " position=" + (position ? point.ToString() : "unchanged") +
            " left=" + (button ? pressed.ToString() : "unchanged"));
        pointerEvents.Add("frame=" + Time.frameCount + " device=" + device.deviceId +
            (device == probeMouse ? " virtual" : " external") + " pos=" + (position ? point.ToString() : "unchanged") +
            " left=" + (button ? pressed.ToString() : "unchanged"));
    }
    static void TraceModal(string stage, Button target = null)
    {
        var modalType = typeof(SurlhwaModalSurface);
        var statics = BindingFlags.Static | BindingFlags.NonPublic;
        var surfaces = FindObjectsByType<SurlhwaModalSurface>(FindObjectsSortMode.None);
        var hits = new List<RaycastResult>();
        var calls = target != null ? typeof(UnityEngine.Events.UnityEventBase).GetField("m_Calls", Fields)?.GetValue(target.onClick) : null;
        var listeners = calls != null ? (ICollection)calls.GetType().GetField("m_RuntimeCalls", Fields)?.GetValue(calls) : null;
        var shelf = Find<IngredientShelfController>();
        var liquidObservers = shelf != null ? (Delegate)Get(shelf, "IngredientUsed") : null;
        if (target != null && EventSystem.current != null)
        {
            var rect = (RectTransform)target.transform;
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) {
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) }, hits);
        }
        Debug.Log("[Modal trace] " + stage + " frame=" + Time.frameCount + " time=" + Time.timeScale +
            " owners=" + modalType.GetField("pauseOwners", statics).GetValue(null) +
            " previous=" + modalType.GetField("previousTimeScale", statics).GetValue(null) +
            " top=" + SurlhwaModalSurface.Top?.name + " settings=" + DemoAudioController.IsSettingsOpen +
            " guide=" + RecipeGuidePanelController.IsTechniqueReadStateActive +
            " focus=" + SafeName(EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null) +
            " ray=" + (hits.Count > 0 ? hits[0].gameObject.name : "NONE") +
            " route=" + Find<BartendingFlowCoordinator>()?.IsBartendingActive +
            " buttonListeners=" + listeners?.Count + " liquidObservers=" + (liquidObservers?.GetInvocationList().Length ?? 0) +
            " surfaces=" + string.Join(",", surfaces.Select(s => s.name + "#" + s.GetInstanceID())) +
            " appFocus=" + Application.isFocused + " editorFocus=" + EditorWindow.focusedWindow?.GetType().Name);
    }
    static string SafeName(UnityEngine.Object value) => value != null ? value.name : "NONE";
    static void ExpectSurface(string name, bool paused)
    {
        var surfaces = FindObjectsByType<SurlhwaModalSurface>(FindObjectsSortMode.None);
        var shields = FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None).Count(r => r.name == "ModalInputShield");
        if ((SurlhwaModalSurface.Top != null ? SurlhwaModalSurface.Top.name : null) != name ||
            SurlhwaModalSurface.IsPaused != paused || Time.timeScale != (paused ? 0 : 1) ||
            surfaces.Length != (name == null ? 0 : 1) || shields != surfaces.Length)
        {
            TraceModal("unexpected surface; expected=" + name);
            throw new InvalidOperationException("Modal sequence surface/pause/shield mismatch");
        }
        if (name != null && (EventSystem.current.currentSelectedGameObject == null ||
            !SurlhwaModalSurface.Allows(EventSystem.current.currentSelectedGameObject.transform)))
            throw new InvalidOperationException("Focus escaped the active surface");
    }
    IEnumerator ModalSequence()
    {
        probeMouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("Same-session modal mouse");
        // One initial checkpoint only. Subsequent guests, service, departure and
        // next-night entry use the production flow; no lock/state reset in the loop.
        yield return SeedNight(5);
        int services = 0;
        foreach (var method in new[] { MiniGameType.Stir, MiniGameType.Shake, MiniGameType.Build, MiniGameType.Float })
        {
            yield return Guest();
            for (int repeat = 0; repeat < 3; repeat++)
            {
                yield return ModuleClick(Named("CodexButton")); ExpectSurface("CodexPanel", false);
                yield return ModuleClick(ButtonField(Find<CocktailCodexPanelController>(), "closeButton")); ExpectSurface(null, false);
                yield return SettingsCycle(Named("SettingsButton")); ExpectSurface(null, false);
            }
            yield return ReadToRequest();
            var ui = Find<InteractionUIController>();
            var guest = (CustomerController)Get(ui, "activeCustomer");
            if (!guest.TryGetDedicatedRequest(out var authored) || authored.RequestType != GuestRequestType.Recommendation)
                throw new InvalidOperationException("Expected scheduled Recommendation");
            for (int repeat = 0; repeat < 3; repeat++)
            {
                yield return ModuleClick(ButtonField(ui, "brewButton")); ExpectSurface("BrewPanel", false);
                if (CanPointAt(Named("CodexButton")) || CanPointAt(Named("SettingsButton")))
                    throw new InvalidOperationException("Bar overlay control leaks through method selection");
                foreach (string field in new[] { "stirButton", "shakeButton", "buildButton", "floatButton" })
                {
                    var target = ButtonField(ui, field);
                    yield return ModuleClick(target);
                    var state = target.GetComponent("SurlhwaMethodSetupTarget");
                    if (!(bool)state.GetType().GetProperty("Chosen").GetValue(state) ||
                        !(bool)state.GetType().GetProperty("Hovered").GetValue(state))
                        throw new InvalidOperationException("Missing selected/hovered tool in repeated flow");
                }
                // Both the selector and request dialogue block corner controls.
                // Their legal pointer access was exercised at the idle bar above.
                yield return ModuleClick(ButtonField(ui, "cancelButton")); ExpectSurface(null, false);
            }
            yield return ModuleClick(ButtonField(ui, "brewButton"));
            if ((bool)Get(ui, "hasMethodSelected")) throw new InvalidOperationException("Reopen kept stale method");
            yield return ModuleClick(ButtonField(ui, method.ToString().ToLowerInvariant() + "Button"));
            yield return ModuleClick(ButtonField(ui, "confirmButton"));
            yield return Wait(() => Find<IngredientShelfController>() != null);
            var order = (OrderData)Get(Find<OrderManager>(), "currentOrder");
            var request = (GuestRequestContext)Get(Find<OrderManager>(), "currentGuestRequest");
            if (order.miniGameType != method || request.RequestType != authored.RequestType ||
                request.RequestedCocktail != authored.requestedCocktail || !request.DesiredEmotionTags.SequenceEqual(authored.DesiredEmotionTags))
                throw new InvalidOperationException("Repeated navigation changed route/request");
            var guide = Find<RecipeGuidePanelController>();
            for (int repeat = 0; repeat < 3; repeat++)
            {
                yield return ModuleClick(ButtonField(guide, "openButton")); ExpectSurface("RecipeGuidePanel", true);
                yield return ModuleClick(ButtonField(guide, "closeButton")); ExpectSurface(null, false);
                yield return SettingsCycle(Named("BartendingSettings")); ExpectSurface(null, false);
            }
            yield return ServeSimplePreparation(method);
            yield return Wait(() => !Find<BartendingFlowCoordinator>().IsBartendingActive);
            ExpectSurface(null, false);
            yield return Wait(() => Get(ui, "farewellCompleted") != null);
            while (Get(ui, "farewellCompleted") != null) yield return ModuleClick(ButtonField(ui, "nextButton"));
            yield return Wait(() => guest == null);
            services++;
            Debug.Log("[Modal sequence] PASS service " + services + " " + method + "; three bar and three workspace overlay cycles, natural result/unload/departure.");
            if (Find<BusinessDayManager>().IsDayEnded)
            {
                yield return Wait(() => SurlhwaModalSurface.Top != null);
                yield return null; // newly enabled summary Canvas must render first
                var summary = Find<DayResultPanelController>();
                yield return ModuleClick(ButtonField(summary, "restartButton"));
            }
        }
        Debug.Log("[Modal sequence] PASS four consecutive services / 12 idle Codex-Settings cycles / 12 selection-Back cycles / 12 Guide-Settings cycles at " + Screen.width + "x" + Screen.height);
    }
    IEnumerator ServeSimplePreparation(MiniGameType method)
    {
        // Deliberately imperfect, supplied single-ingredient drink. This exercises
        // real preparation/result/unload without injecting completion or knowledge.
        // Preparation uses public action seams; UI navigation uses pointer events.
        var shelf = Find<IngredientShelfController>();
        MonoBehaviour game = method == MiniGameType.Stir ? (MonoBehaviour)Find<StirMiniGame>() : method == MiniGameType.Shake ? Find<ShakeMiniGame>() : method == MiniGameType.Build ? Find<BuildMiniGame>() : Find<FloatMiniGame>();
        // As in the existing campaign driver, suppress keyboard polling only
        // while its action seam is driven. Otherwise "Space not held" ends the
        // programmatic bottle pickup every frame. Restore before technique/UI.
        game.enabled = false;
        var ingredient = shelf.ConfiguredIngredients.First(shelf.IsIngredientVisible);
        if (!shelf.TrySelectIngredient(ingredient)) throw new InvalidOperationException("No supplied ingredient");
        var floating = Find<FloatMiniGame>();
        if (floating != null && !floating.BeginLayerPour(FloatMiniGame.FloatPourIntensity.Gentle))
            throw new InvalidOperationException("Layer rejected");
        if (method == MiniGameType.Stir)
        {
            yield return Wait(() => shelf.PhysicalPresentation.PrepareBottle(ingredient, shelf.Jigger));
            if (shelf.FillJigger(10) != 10) throw new InvalidOperationException("Measurement rejected");
            shelf.EndSelectedPour(); yield return Wait(() => shelf.PhysicalPresentation.IsResting);
            if (!shelf.BeginMeasuredTransfer()) throw new InvalidOperationException("Transfer rejected");
        }
        else
        {
            if (floating == null && !shelf.BeginSelectedPour()) throw new InvalidOperationException("Pour rejected");
            yield return Wait(() => shelf.PreparePhysicalPour());
            if (floating != null) { floating.PourActiveLayerForDuration(1f / 3, FloatMiniGame.FloatPourIntensity.Gentle); floating.EndActiveLayerPour(); }
            else { shelf.PourSelectedIngredient(10); shelf.EndSelectedPour(); }
        }
        yield return Wait(() => shelf.ToolsReady);
        game.enabled = true;
        if (method == MiniGameType.Stir || method == MiniGameType.Shake)
        {
            if (!shelf.TryAddIceToActiveTarget()) throw new InvalidOperationException("Ice rejected");
            yield return Wait(() => shelf.ToolsReady);
            yield return ModuleClick(ButtonField(game, "startButton"));
            if (method == MiniGameType.Stir) { yield return new WaitForSecondsRealtime(3); yield return ModuleClick(ButtonField(game, "startButton")); }
            else yield return Wait(() => Get(game, "state").ToString() == "AwaitingStrain");
            // A technique can reveal its next control during Update. Let its
            // visible Canvas frame settle before asking the pointer to hit it.
            yield return null;
            yield return ModuleClick(ButtonField(game, "startButton"));
            yield return Wait(() => Get(game, "state").ToString() == "AwaitingGarnish" && shelf.ToolsReady);
            yield return null;
        }
        else yield return ModuleClick(ButtonField(game, "startButton"));
        yield return ModuleClick(ButtonField(game, "startButton"));
    }
    IEnumerator NotificationLayout()
    {
        yield return null;
        Click(Named("NewGameButton")); yield return null; yield return Guest();
        // Explicit layout fixture, not a synthesized gameplay service or discovery.
        var toast = Find<ResultToastController>();
        toast.Show("잔을 건넨 뒤", "내어준 잔: 설야\n준비 순서가 레시피와 달랐습니다.\n조주: 안정적\n평판: +1 (요청 보상 없음)");
        yield return new WaitForSecondsRealtime(.3f); yield return Capture("service-fixture");
        yield return new WaitForSecondsRealtime(toast.DisplayCycleDuration);
        var knowledge = Find<RecipeUnlockManager>();
        Find<RecipeUnlockPopupController>().Show(knowledge.GetAllRecipes().First(knowledge.IsKnown));
        yield return new WaitForSecondsRealtime(.3f); yield return Capture("discovery-fixture");
    }
    IEnumerator PointerHandoffDiagnostic()
    {
        probeMouse = InputSystem.AddDevice<Mouse>("Trace comparison mouse");
        yield return SeedNight(5); yield return Guest(); yield return ReadToRequest();
        var ui = Find<InteractionUIController>();
        // Positive comparison: same request and ordinary method-selection owner.
        yield return ModuleClick(ButtonField(ui, "brewButton"));
        ExpectSurface("BrewPanel", false);
        yield return ModuleClick(ButtonField(ui, "cancelButton"));
        var brew = ButtonField(ui, "brewButton");
        var rect = (RectTransform)brew.transform;
        var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        QueuePointer(point, 0); yield return null;
        // Explicit negative fixture: reproduce the observed second-device Point
        // handoff, not human input or a new production navigation stack.
        handoffMouse = InputSystem.AddDevice<Mouse>("Trace competing mouse");
        Record("fixture.point-handoff device=" + handoffMouse.deviceId + " position=(-100,-100); expect first lost click to stop");
        InputSystem.QueueStateEvent(handoffMouse, new MouseState { position = new Vector2(-100, -100) });
        yield return null;
        yield return ModuleClick(brew);
        throw new InvalidOperationException("Pointer handoff did not reproduce; do not infer a cause from this fixture");
    }
    IEnumerator SettingsCycle(Button opener)
    {
        float expectedScale = Time.timeScale;
        var expectedTop = SurlhwaModalSurface.Top;
        bool expectedPause = SurlhwaModalSurface.IsPaused;
        bool expectedAllowed = SurlhwaModalSurface.Allows(opener.transform);
        bool expectedInteractable = opener.IsInteractable();
        int expectedSurfaces = FindObjectsByType<SurlhwaModalSurface>(FindObjectsSortMode.None).Length;
        int expectedShields = FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None).Count(r => r.name == "ModalInputShield");
        Record("settings.prior scale=" + expectedScale + " top=" + Identity(expectedTop) +
            " paused=" + expectedPause + " allowed=" + expectedAllowed + " interactable=" + expectedInteractable);
        yield return ModuleClick(opener);
        if (!DemoAudioController.IsSettingsOpen || SurlhwaModalSurface.Top == null || SurlhwaModalSurface.Top.name != "SoundSettings")
            throw new InvalidOperationException("Settings open did not complete");
        // Retain the original open-state pause, shield and focus assertions,
        // relative to the legitimate parent rather than assuming an empty stack.
        var focus = EventSystem.current.currentSelectedGameObject;
        if (Time.timeScale != 0 || !SurlhwaModalSurface.IsPaused ||
            FindObjectsByType<SurlhwaModalSurface>(FindObjectsSortMode.None).Length != expectedSurfaces + 1 ||
            FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None).Count(r => r.name == "ModalInputShield") != expectedShields + 1 ||
            focus == null || !SurlhwaModalSurface.Allows(focus.transform))
            throw new InvalidOperationException("Settings open pause/shield/focus mismatch");
        yield return ModuleClick(Named("CloseSoundSettings"));
        // Owner closes synchronously today. This bounded readiness observation
        // can expire while paused and never supplies input or alters a lock.
        float until = Time.realtimeSinceStartup + 1;
        while (DemoAudioController.IsSettingsOpen && Time.realtimeSinceStartup < until) yield return null;
        bool complete = !DemoAudioController.IsSettingsOpen &&
            !FindObjectsByType<SurlhwaModalSurface>(FindObjectsSortMode.None).Any(s => s.name == "SoundSettings") &&
            FindObjectsByType<SurlhwaModalSurface>(FindObjectsSortMode.None).Length == expectedSurfaces &&
            FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None).Count(r => r.name == "ModalInputShield") == expectedShields;
        bool scaleOK = Time.timeScale == expectedScale, topOK = SurlhwaModalSurface.Top == expectedTop;
        bool inputOK = SurlhwaModalSurface.IsPaused == expectedPause && SurlhwaModalSurface.Allows(opener.transform) == expectedAllowed && opener.IsInteractable() == expectedInteractable;
        Record("settings.restore closeComplete=" + complete + " scaleExpected=" + expectedScale + " scaleActual=" + Time.timeScale +
            " topExpected=" + Identity(expectedTop) + " topActual=" + Identity(SurlhwaModalSurface.Top) +
            " pauseExpected=" + expectedPause + " pauseActual=" + SurlhwaModalSurface.IsPaused +
            " allowedExpected=" + expectedAllowed + " allowedActual=" + SurlhwaModalSurface.Allows(opener.transform) +
            " interactableExpected=" + expectedInteractable + " interactableActual=" + opener.IsInteractable());
        if (!complete || !scaleOK || !topOK || !inputOK)
            throw new InvalidOperationException("Settings restoration: closeComplete=" + complete + " scaleOK=" + scaleOK + " topOK=" + topOK + " inputOK=" + inputOK);
    }
    IEnumerator InputModuleFlow()
    {
        yield return null;
        if (EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
            throw new InvalidOperationException("Unexpected production input module");
        probeMouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("Phase M validation mouse");
        if (Named("ContinueButton").IsInteractable()) throw new InvalidOperationException("Fresh Continue is enabled");
        yield return ModuleClick(Named("ContinueButton"), true);
        if (Find<GameManager>().IsGameplaySessionStarted) throw new InvalidOperationException("Disabled Continue started gameplay");
        yield return ModuleClick(Named("SoundSettingsButton"));
        if (!DemoAudioController.IsSettingsOpen) throw new InvalidOperationException("Module click did not open Settings");
        if (CanPointAt(Named("NewGameButton"))) throw new InvalidOperationException("Settings click leaks into entry");
        yield return ModuleClick(Named("CloseSoundSettings"));
        if (DemoAudioController.IsSettingsOpen) throw new InvalidOperationException("Module click did not close Settings");
        yield return ModuleClick(Named("NewGameButton"));
        yield return Guest();
        yield return ModuleClick(Named("CodexButton"));
        var codex = Find<CocktailCodexPanelController>();
        var cards = (Transform)Get(codex, "cardParent");
        var scroll = cards.GetComponentInParent<ScrollRect>();
        var lastCard = cards.GetChild(cards.childCount - 1).GetComponent<Button>();
        if (CanPointAt(lastCard)) throw new InvalidOperationException("Masked final Codex card receives a pointer before scrolling");
        float initialScroll = scroll.verticalNormalizedPosition;
        var wheelPoint = RectTransformUtility.WorldToScreenPoint(null, scroll.viewport.TransformPoint(scroll.viewport.rect.center));
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(probeMouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = wheelPoint, scroll = new Vector2(0, -120) });
        yield return null;
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(probeMouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = wheelPoint });
        yield return new WaitForSecondsRealtime(.3f);
        if (scroll.verticalNormalizedPosition >= initialScroll || !CanPointAt(lastCard))
            throw new InvalidOperationException("Input-module wheel did not expose final Codex card");
        yield return ModuleClick(lastCard);
        yield return ModuleClick(ButtonField(codex, "backButton"));
        var returnedFocus = EventSystem.current.currentSelectedGameObject;
        if (returnedFocus == null || !returnedFocus.activeInHierarchy || !returnedFocus.GetComponent<Selectable>().IsInteractable())
            throw new InvalidOperationException("Codex Back leaves focus on a hidden or disabled control");
        yield return ModuleClick(ButtonField(codex, "closeButton"));
        var ui = Find<InteractionUIController>();
        yield return ModuleClick(ButtonField(ui, "ellipsisButton"));
        for (int i = 0; i < 50 && ButtonField(ui, "nextButton").gameObject.activeInHierarchy; i++)
            yield return ModuleClick(ButtonField(ui, "nextButton"));
        yield return ModuleClick(ButtonField(ui, "brewButton"));
        yield return Wait(() => Find<IngredientShelfController>() != null);
        var guide = Find<RecipeGuidePanelController>();
        yield return ModuleClick(ButtonField(guide, "openButton"));
        if (!guide.IsReading || Time.timeScale != 0) throw new InvalidOperationException("Module click did not open/pause Guide");
        yield return ModuleClick(ButtonField(guide, "closeButton"));
        if (guide.IsReading || Time.timeScale != 1) throw new InvalidOperationException("Module click did not resume workspace");
        yield return SettingsCycle(Named("BartendingSettings"));
        Debug.Log("[Phase M Input Module] PASS: entry/settings/New Game/Codex masked cards/wheel/detail/back focus/guest/dialogue/bartending/Guide/settings through queued virtual mouse states. PROGRAMMATIC INPUT, not OS or human mouse.");
    }
    IEnumerator StagingFlow()
    {
        UnityEngine.Random.InitState(42); // Comparable checkpoint fixtures, not a production random policy.
        probeMouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("Staging review mouse");
        yield return SeedNight(5); yield return Guest(); yield return ReadToRequest();
        var ui = Find<InteractionUIController>();
        foreach (var method in new[] { MiniGameType.Stir, MiniGameType.Shake, MiniGameType.Build, MiniGameType.Float })
        {
            yield return ModuleClick(ButtonField(ui, "brewButton"));
            var target = ButtonField(ui, method.ToString().ToLowerInvariant() + "Button");
            var rect = (RectTransform)target.transform;
            var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            QueuePointer(point, 0);
            yield return new WaitForSecondsRealtime(.25f);
            var state = target.GetComponent("SurlhwaMethodSetupTarget");
            if (!CanPointAt(target) || !(bool)state.GetType().GetProperty("Hovered").GetValue(state) || (bool)Get(ui, "hasMethodSelected"))
                throw new InvalidOperationException("Expected reachable, hovered, unselected setup");
            if (SURLHWAPhaseMUIValidation.Mode == "staging-after")
            {
                var shadow = target.transform.Find("CountertopArrangement/ContactShadow").GetComponent<Graphic>();
                if (shadow.canvasRenderer == null) throw new InvalidOperationException("Contact shadow has no renderer");
                var mesh = shadow.canvasRenderer.GetMesh();
                if (mesh == null || mesh.vertexCount == 0 || shadow.raycastTarget)
                    throw new InvalidOperationException("Contact shadow is empty or blocks input");
                foreach (var graphic in target.GetComponentsInChildren<Graphic>())
                    if (graphic.transform != target.transform && graphic.raycastTarget)
                        throw new InvalidOperationException("Setup decoration blocks the hit area: " + graphic.name);
            }
            yield return Capture(method + "-hover");
            yield return ModuleClick(target);
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(probeMouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = new Vector2(40, Screen.height / 2) });
            yield return new WaitForSecondsRealtime(.25f);
            if (!(bool)state.GetType().GetProperty("Chosen").GetValue(state) || (bool)state.GetType().GetProperty("Hovered").GetValue(state))
                throw new InvalidOperationException("Expected selected setup after pointer exit");
            yield return Capture(method + "-selected");
            yield return ModuleClick(ButtonField(ui, "cancelButton"));
        }
        Debug.Log("[Staging] PASS: four hover and selected states, selected emphasis after pointer exit, at " + Screen.width + "x" + Screen.height);
    }
    IEnumerator DiegeticFlow()
    {
        yield return null;
        probeMouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("Diegetic validation mouse");
        foreach (var method in new[] { MiniGameType.Stir, MiniGameType.Shake, MiniGameType.Build, MiniGameType.Float })
        {
            yield return SeedNight(5); yield return Guest(); yield return ReadToRequest();
            var ui = Find<InteractionUIController>();
            var customer = (CustomerController)Get(ui, "activeCustomer");
            if (!customer.TryGetDedicatedRequest(out var authored) || authored.RequestType != GuestRequestType.Recommendation)
                throw new InvalidOperationException("Expected normal Night 5 Recommendation");
            string requestText = ((TMP_Text)Get(ui, "dialogueText")).text;
            yield return ModuleClick(ButtonField(ui, "brewButton"));
            var panel = (GameObject)Get(ui, "brewPanel");
            if (!panel.activeInHierarchy || (bool)Get(ui, "hasMethodSelected") || ButtonField(ui, "confirmButton").IsInteractable())
                throw new InvalidOperationException("Selection entry: visible=" + panel.activeInHierarchy +
                    ", selected=" + Get(ui, "hasMethodSelected") + ", confirm=" + ButtonField(ui, "confirmButton").IsInteractable() +
                    ", appFocused=" + Application.isFocused + ", editorFocus=" + EditorWindow.focusedWindow?.GetType().Name);
            if (panel.GetComponent<Image>().enabled || GameObject.Find("RequestContext").GetComponent<TMP_Text>().text != requestText)
                throw new InvalidOperationException("Legacy panel or request changed");
            yield return ModuleClick(ButtonField(ui, "confirmButton"), true);
            if (!panel.activeInHierarchy || (bool)Get(ui, "hasMethodSelected"))
                throw new InvalidOperationException("Disabled confirm activated or clicked through");
            var backgroundHits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = new Vector2(40, Screen.height / 2) }, backgroundHits);
            if (backgroundHits.Count == 0 || backgroundHits[0].gameObject.name != "ModalInputShield")
                throw new InvalidOperationException("Transparent selection shield does not block the bar");
            foreach (string field in new[] { "stirButton", "shakeButton", "buildButton", "floatButton" })
            {
                var target = ButtonField(ui, field);
                var rect = (RectTransform)target.transform;
                var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
                QueuePointer(point, 0);
                yield return new WaitForSecondsRealtime(.15f);
                var state = target.GetComponent("SurlhwaMethodSetupTarget");
                if (!(bool)state.GetType().GetProperty("Hovered").GetValue(state) || !CanPointAt(target))
                    throw new InvalidOperationException("Hover did not reach " + field);
                // Sample the generous hit area, not just the tool's opaque pixels.
                foreach (var offset in new[] { new Vector2(-54, 0), new Vector2(54, 0), new Vector2(0, 56), new Vector2(0, -58) })
                {
                    var hits = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(offset)) }, hits);
                    if (hits.Count == 0 || hits[0].gameObject.GetComponentInParent<Button>() != target)
                        throw new InvalidOperationException("Setup hit area obstructed: " + field);
                }
                yield return ModuleClick(target);
                if (!(bool)state.GetType().GetProperty("Chosen").GetValue(state)) throw new InvalidOperationException("Missing selected marker");
            }
            yield return Capture("all-setups-float-selected-" + method);
            var chosen = ButtonField(ui, method.ToString().ToLowerInvariant() + "Button");
            yield return ModuleClick(chosen);
            var codex = Find<CocktailCodexPanelController>();
            codex.Open(); yield return null;
            if (CanPointAt(chosen)) throw new InvalidOperationException("Codex leaks to setup");
            yield return ModuleClick(ButtonField(codex, "closeButton"));
            if (!panel.activeInHierarchy || (MiniGameType)Get(ui, "selectedMethod") != method)
                throw new InvalidOperationException("Child overlay lost selection");
            Find<DemoAudioController>().ToggleSettings(); yield return null;
            if (CanPointAt(chosen) || Time.timeScale != 0) throw new InvalidOperationException("Settings failed to block/pause");
            yield return ModuleClick(Named("CloseSoundSettings"));
            if (Time.timeScale != 1 || !CanPointAt(chosen)) throw new InvalidOperationException("Settings did not return to setup");
            yield return ModuleClick(ButtonField(ui, "cancelButton"));
            yield return ModuleClick(ButtonField(ui, "brewButton"));
            if ((bool)Get(ui, "hasMethodSelected")) throw new InvalidOperationException("Reopened selection retained stale choice");
            yield return ModuleClick(chosen);
            yield return Capture("selected-" + method);
            yield return ModuleClick(ButtonField(ui, "confirmButton"));
            yield return Wait(() => Find<IngredientShelfController>() != null);
            var manager = Find<OrderManager>();
            var request = (GuestRequestContext)Get(manager, "currentGuestRequest");
            var order = (OrderData)Get(manager, "currentOrder");
            if (order.miniGameType != method || request.RequestType != authored.RequestType || request.RequestedCocktail != authored.requestedCocktail ||
                !request.DesiredEmotionTags.SequenceEqual(authored.DesiredEmotionTags))
                throw new InvalidOperationException("Wrong route or request semantics changed");
            var guide = Find<RecipeGuidePanelController>();
            yield return ModuleClick(ButtonField(guide, "openButton"));
            yield return ModuleClick(ButtonField(guide, "closeButton"));
            if (guide.IsReading || Time.timeScale != 1) throw new InvalidOperationException("Guide did not resume route");
            yield return Capture("workspace-" + method);
            Debug.Log("[Diegetic] PASS " + method + ": hover/hit area/selection/change/back/reopen/Codex/Settings/route/request/Guide. Virtual InputSystem mouse at " + Screen.width + "x" + Screen.height);
        }
    }
    IEnumerator Navigation()
    {
        yield return SeedNight(18); yield return Capture("ending");
        var panel = Find<DayResultPanelController>();
        int lines = Find<BusinessDayManager>().Campaign.ending.onEnterLines.Length;
        for (int i = 0; i < lines; i++) { Click(ButtonField(panel, "restartButton")); yield return new WaitForSecondsRealtime(.26f); }
        yield return Capture("coda");
        Click(ButtonField(panel, "restartButton")); yield return null;
        yield return Wait(() => Named("NewGameButton") != null);
        yield return new WaitForSecondsRealtime(.2f);
        if (Find<DayResultPanelController>() != null) throw new InvalidOperationException("Ending opened underneath entry");
        yield return Capture("post-ending-entry");
        Click(Named("NewGameButton")); yield return Capture("new-game-confirmation");
        if (!SurlhwaGameProgress.HasMeaningfulProgress()) throw new InvalidOperationException("New Game erased progress before confirmation");
        Click(Named("CancelNewGameButton")); yield return null;
        if (!SurlhwaGameProgress.HasMeaningfulProgress()) throw new InvalidOperationException("Cancel erased progress");
        Click(Named("NewGameButton")); yield return null;
        Click(Named("ConfirmNewGameButton")); yield return null;
        yield return Wait(() => Find<GameManager>() != null && Find<GameManager>().IsGameplaySessionStarted);
        if (CampaignProgressPersistence.HasProgress || BusinessDayPersistence.LoadCurrentDay(1) != 1)
            throw new InvalidOperationException("Confirmed reset failed");
    }
}
// Attached only to an existing Button for the duration of an opted-in probe.
// Never calls Use(), changes focus, forwards an event, or adds a new raycast target.
internal sealed class SURLHWAButtonEventObservation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public Action<string> record;
    static string Id(GameObject value) => value != null ? value.name + "#" + value.GetInstanceID() : "NONE";
    void Observe(string edge, PointerEventData data) => record?.Invoke(edge + " receiver=" + Id(gameObject) +
        " pointer=" + data.pointerId + " position=" + data.position + " button=" + data.button +
        " currentRay=" + Id(data.pointerCurrentRaycast.gameObject) + " pressRay=" + Id(data.pointerPressRaycast.gameObject) +
        " pointerPress=" + Id(data.pointerPress) + " pointerClick=" + Id(data.pointerClick) +
        " eligible=" + data.eligibleForClick + " dragging=" + data.dragging + " used=" + data.used);
    public void OnPointerDown(PointerEventData data) => Observe("PointerDown", data);
    public void OnPointerUp(PointerEventData data) => Observe("PointerUp", data);
    public void OnPointerClick(PointerEventData data) => Observe("PointerClick", data);
}
#endif
