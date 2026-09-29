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
    static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static T Find<T>() where T : UnityEngine.Object => FindFirstObjectByType<T>();
    static object Get(object owner, string field) => owner.GetType().GetField(field, Fields).GetValue(owner);
    static Button ButtonField(object owner, string field) => (Button)Get(owner, field);
    static Button Named(string name) => FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == name);
    static void Call(object owner, string method) => owner.GetType().GetMethod(method, Fields).Invoke(owner, null);
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
        previousBackground = Application.runInBackground;
        Application.runInBackground = true;
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
                if (Time.realtimeSinceStartup - began > 300) throw new TimeoutException("Phase M UI probe");
                if (!work.Peek().MoveNext()) { work.Pop(); continue; }
                next = work.Peek().Current;
                if (next is IEnumerator nested) { work.Push(nested); continue; }
            }
            catch (Exception e) { Debug.LogException(e); Finish(false); yield break; }
            yield return next;
        }
        Finish(true);
    }
    void Finish(bool success)
    {
        Debug.Log("[Phase M UI] " + SURLHWAPhaseMUIValidation.Mode + " " + (success ? "COMPLETE" : "FAILED") + ": staged production-screen fixtures; callback invocation and raycast observations, not human input.");
        Application.runInBackground = previousBackground;
        EditorApplication.ExitPlaymode();
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
            yield return null;
        }
    }
    IEnumerator Run()
    {
        yield return Capture("entry");
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
        codex.SelectRecipe(recipe); yield return Capture("codex-partial"); codex.Close();
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
        foreach (string method in new[] { "stirButton", "shakeButton", "buildButton", "floatButton" })
        {
            yield return SeedNight(5); yield return Guest(); yield return ReadToRequest();
            var ui = Find<InteractionUIController>();
            Click(ButtonField(ui, "brewButton")); yield return null; Click(ButtonField(ui, method));
            yield return Capture("method-" + method);
            Click(ButtonField(ui, "cancelButton")); yield return null;
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
        yield return SeedNight(18); yield return Capture("ending");
        panel = Find<DayResultPanelController>();
        int lines = Find<BusinessDayManager>().Campaign.ending.onEnterLines.Length;
        for (int i = 0; i < lines; i++) { Click(ButtonField(panel, "restartButton")); yield return null; }
        yield return Capture("coda");
    }
}
#endif
