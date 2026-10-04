#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static SURLHWAJ5PrinceClosureValidation;

[InitializeOnLoad]
public static class SURLHWAAudioPlayModeValidation
{
    private const string Prefix = "SURLHWA_J7_PROBE_";
    private static readonly string[] ExtraKeys = { "currentDay", "SURLHWA_DEMO_COMPLETE_V1", "SURLHWA_FIRST_RECIPE_REVEAL_SHOWN" };
    static SURLHWAAudioPlayModeValidation()
    {
        EditorApplication.playModeStateChanged -= Changed;
        EditorApplication.playModeStateChanged += Changed;
    }
    [MenuItem("SURLHWA/Validation/Run Audio And Reading Pause Play Mode")]
    public static void Run()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Exit Play Mode first.");
        foreach (string key in AllKeys())
        {
            SessionState.SetBool(Prefix + "Had" + key, SurlhwaPlayerPrefsStore.HasKey(key));
            SessionState.SetInt(Prefix + key, SurlhwaPlayerPrefsStore.GetInt(key, 0));
        }
        SessionState.SetBool(Prefix + "HadKnowledge", RecipeProgressPersistence.HasRecipeKnowledge());
        SessionState.SetString(Prefix + "Knowledge", RecipeProgressPersistence.LoadRecipeKnowledgeRaw());
        SessionState.SetBool(Prefix + "HadLegacy", RecipeProgressPersistence.HasLegacyUnlockedRecipeIds());
        SessionState.SetString(Prefix + "Legacy", RecipeProgressPersistence.LoadLegacyUnlockedRecipeIdsRaw());
        SurlhwaGameProgress.ResetGameProgress();
        SessionState.SetBool(Prefix + "Pending", true);
        EditorSceneManager.OpenScene("Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity");
        EditorApplication.EnterPlaymode();
    }
    private static IEnumerable<string> AllKeys()
    {
        foreach (var key in Keys) yield return key;
        foreach (var key in ExtraKeys) yield return key;
        foreach (var key in SURLHWACampaignValidation.ProgressKeys()) yield return key;
        foreach (var key in new[] { DemoAudioController.MasterKey, DemoAudioController.MusicKey, DemoAudioController.EffectsKey, DemoAudioController.MuteKey }) yield return key;
    }
    private static void Changed(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Prefix + "Pending", false))
            new GameObject("Audio Runtime Validation").AddComponent<SURLHWAAudioRunner>();
        if (state != PlayModeStateChange.EnteredEditMode ||
            (!SessionState.GetBool(Prefix + "Complete", false) && !SessionState.GetBool(Prefix + "Pending", false))) return;
        foreach (string key in AllKeys())
        {
            if (SessionState.GetBool(Prefix + "Had" + key, false)) SurlhwaPlayerPrefsStore.SetInt(key, SessionState.GetInt(Prefix + key, 0));
            else SurlhwaPlayerPrefsStore.DeleteKey(key);
            SessionState.EraseBool(Prefix + "Had" + key);
            SessionState.EraseInt(Prefix + key);
        }
        if (SessionState.GetBool(Prefix + "HadKnowledge", false)) RecipeProgressPersistence.SaveRecipeKnowledgeRaw(SessionState.GetString(Prefix + "Knowledge", ""));
        else RecipeProgressPersistence.ClearRecipeKnowledge();
        if (SessionState.GetBool(Prefix + "HadLegacy", false)) RecipeProgressPersistence.SaveUnlockedRecipeIdsRaw(SessionState.GetString(Prefix + "Legacy", ""));
        else RecipeProgressPersistence.ClearLegacyUnlockedRecipeIds();
        SurlhwaPlayerPrefsStore.Save();
        int code = SessionState.GetBool(Prefix + "Success", false) ? 0 : 1;
        foreach (string key in new[] { "HadKnowledge", "HadLegacy", "Pending", "Complete", "Success" }) SessionState.EraseBool(Prefix + key);
        SessionState.EraseString(Prefix + "Knowledge");
        SessionState.EraseString(Prefix + "Legacy");
        if (Application.isBatchMode) EditorApplication.Exit(code);
    }
    internal static void Complete(bool success)
    {
        SessionState.SetBool(Prefix + "Pending", false);
        SessionState.SetBool(Prefix + "Complete", true);
        SessionState.SetBool(Prefix + "Success", success);
        EditorApplication.ExitPlaymode();
    }
}



internal sealed class SURLHWAAudioRunner : MonoBehaviour
{
    private readonly Stack<IEnumerator> work = new();
    private bool ended;
    private bool previousBackground;
    private void Start()
    {
        previousBackground = Application.runInBackground;
        Application.runInBackground = true;
        work.Push(RunProbe());
        StartCoroutine(Guarded());
    }
    private IEnumerator Guarded()
    {
        while (work.Count > 0 && !ended)
        {
            object wait = null;
            try
            {
                if (!work.Peek().MoveNext()) { work.Pop(); continue; }
                wait = work.Peek().Current;
                if (wait is IEnumerator nested) { work.Push(nested); continue; }
            }
            catch (Exception e) { Debug.LogException(e); Complete(false); yield break; }
            yield return wait;
        }
    }
    private IEnumerator RunProbe()
    {
        yield return null;
        var audio = FindFirstObjectByType<DemoAudioController>();
        Require(audio != null, "Missing serialized Bar audio controller.");
        Require(ActiveListeners() == 1, "Bar requires exactly one active audio listener.");
        audio.Sounds.Trace = true;
        var cueCounts = new Dictionary<BarSound, int>();
        audio.Sounds.CueChanged += (cue, start, channel) => { if (start) cueCounts[cue] = cueCounts.GetValueOrDefault(cue) + 1; };
        var sources = audio.GetComponents<AudioSource>();
        Require(sources.Length == SurlhwaSoundscape.SourceCount, "Unexpected audio ownership/duplicate sources.");
        Require(audio.Sounds.Library != null && audio.Sounds.Library.cues.Length == 22, "Missing Phase O cue library.");
        foreach (var cue in audio.Sounds.Library.cues)
        {
            var clip = cue.clip;
            Require(clip != null && clip.samples > 0 && clip.length > 0, "Missing audio asset " + cue.id);
        }
        var entry = (GameObject)Get(FindFirstObjectByType<GameManager>(), "entryPanel");
        var settings = entry.transform.Find("DemoEntryContent/SoundSettingsButton")?.GetComponent<Button>();
        Require(settings != null && settings.interactable, "Entry has no sound control.");
        SURLHWAReviewCapture.Save("title", 1280, 720);
        settings.onClick.Invoke();
        Require(DemoAudioController.IsSettingsOpen && Time.timeScale == 0f, "Sound settings did not own a pause.");
        var root = (GameObject)Get(audio, "settingsRoot");
        Slider master = null;
        foreach (var slider in root.GetComponentsInChildren<Slider>(true))
            if (slider.name == DemoAudioController.MasterKey) master = slider;
        Require(master != null, "No master volume slider.");
        master.value = 0;
        foreach (var source in sources) Require(source.volume == 0, "Zero master must silence every source.");
        master.value = 63;
        audio.ToggleMute();
        if (!audio.Muted) audio.ToggleMute();
        foreach (var source in sources) Require(source.volume == 0, "Mute must silence every source.");
        SurlhwaGameProgress.ResetGameProgress();
        Require(audio.Muted && SurlhwaPlayerPrefsStore.GetInt(DemoAudioController.MasterKey, -1) == 63,
            "Gameplay reset erased sound preferences.");
        SURLHWAReviewCapture.Save("sound-settings", 1280, 720);
        SURLHWAReviewCapture.Save("sound-settings", 1920, 1080);
        yield return new WaitForSecondsRealtime(.5f);
        audio.ToggleMute();
        audio.ToggleSettings();
        Require(!DemoAudioController.IsSettingsOpen && Mathf.Approximately(Time.timeScale, 1f), "Settings did not restore timing.");
        entry.SetActive(false); // Focused audio/pause probe, not a normal-play entry claim.

        var recipe = Load<CocktailRecipeData>("Recipes/Recipe_GinFizz.asset");
        var knowledge = FindFirstObjectByType<RecipeUnlockManager>();
        knowledge.MarkKnown(recipe); // Focused presentation fixture only; saved knowledge is restored by the wrapper.
        var guideContext = new RecipeGuideContext(MiniGameType.Shake, new[] { recipe },
            knowledge.GetKnownFacts, false);
        var coordinator = FindFirstObjectByType<BartendingFlowCoordinator>();
        bool completed = false;
        Require(coordinator.BeginBartending(new BartendingSession(
            new BartendingRequestContext(MiniGameType.Shake, "조주 확인", recipe, 4), guideContext),
            _ => completed = true, failure => throw new InvalidOperationException(failure.ToString())),
            "Coordinator rejected actual Shake scene.");
        float deadline = Time.realtimeSinceStartup + 20f;
        while (FindFirstObjectByType<ShakeMiniGame>() == null)
        {
            Require(Time.realtimeSinceStartup < deadline, "Shake load timed out.");
            yield return null;
        }
        yield return null;
        Require(ActiveListeners() == 1, "Bartending lost its audio listener or duplicated the Bar listener.");
        var shake = FindFirstObjectByType<ShakeMiniGame>();
        var shelf = FindFirstObjectByType<IngredientShelfController>();
        Require(ReferenceEquals(Get(Get(audio, "preparationAudio"), "shelf"), shelf), "Audio has no explicit runner shelf handoff.");
        Require(shelf.TrySelectIngredient(recipe.ingredients[0].ingredient), "Gin unavailable.");
        shake.enabled = false; // Explicit physical fixture; do not race unattended mouse polling.
        deadline = Time.realtimeSinceStartup + 4;
        while (!shelf.PreparePhysicalPour())
        {
            Require(Time.realtimeSinceStartup < deadline, "Visible bottle failed to reach pour pose.");
            yield return null;
        }
        for (int i = 0; i < 15; i++)
        {
            shelf.PourSelectedIngredient(1f);
            yield return null;
        }
        shelf.EndSelectedPour();
        Require(audio.Sounds.PourSource.isPlaying, "Accepted continuous pour has no running sound.");
        yield return new WaitForSecondsRealtime(.25f);
        Require(!audio.Sounds.PourSource.isPlaying, "Pour sound did not stop after transfer ended.");
        SURLHWAReviewCapture.Save("shake-preparation", 1280, 720);
        deadline = Time.realtimeSinceStartup + 4;
        while (!shelf.ToolsReady)
        {
            Require(Time.realtimeSinceStartup < deadline, "Bottle did not return before ice placement.");
            yield return null;
        }
        Require(shake.AddIceToShaker(), "Ice preparation failed.");
        while (shelf.PhysicalTransferActive) yield return null;
        shake.enabled = true;
        shake.BeginShake();
        var guide = shake.GetComponentInChildren<RecipeGuidePanelController>(true);
        Require(guide != null, "No real Guide.");
        var open = (Button)Get(guide, "openButton");
        var close = (Button)Get(guide, "closeButton");
        float before = shake.GetExpectedNoteTime(0) - Time.unscaledTime;
        open.onClick.Invoke();
        var cards = (List<Button>)Get(guide, "spawnedButtons");
        Require(cards.Count == 1, "Expected one fixture recipe.");
        cards[0].onClick.Invoke();
        yield return null;
        var detail = (TMP_Text)Get(guide, "recipeIngredientText");
        var scroll = detail.transform.parent.GetComponent<ScrollRect>();
        Require(scroll != null && scroll.content == detail.rectTransform, "Long recipe has no reading scroll area.");
        SURLHWAReviewCapture.Save("recipe-guide", 1280, 720);
        scroll.verticalNormalizedPosition = 0;
        Canvas.ForceUpdateCanvases();
        SURLHWAReviewCapture.Save("recipe-guide-bottom", 1280, 720);
        yield return new WaitForSecondsRealtime(1.1f);
        float after = shake.GetExpectedNoteTime(0) - Time.unscaledTime;
        Require(Mathf.Abs(after - before) < .15f && shake.MissCount == 0,
            "Guide consumed the unscaled Shake note timeline.");
        close.onClick.Invoke();
        before = shake.GetExpectedNoteTime(0) - Time.unscaledTime;
        audio.ToggleSettings();
        yield return new WaitForSecondsRealtime(1.1f);
        after = shake.GetExpectedNoteTime(0) - Time.unscaledTime;
        Require(Mathf.Abs(after - before) < .15f && shake.MissCount == 0,
            "Sound settings consumed the Shake timeline.");
        audio.ToggleSettings();
        for (int i = 0; i < shake.NoteCount; i++)
        {
            while (Time.unscaledTime < shake.GetExpectedNoteTime(i)) yield return null;
            shake.TryJudgeShakeInput(i % 2 == 0 ? ShakeMiniGame.ShakeDirection.Left : ShakeMiniGame.ShakeDirection.Right,
                Time.unscaledTime);
        }
        Require(shake.MissCount == 0 && shake.CorrectHitCount == shake.NoteCount,
            "Resumed rhythm was not playable after both read pauses.");
        yield return new WaitForSecondsRealtime(.3f);
        Require(cueCounts.GetValueOrDefault(BarSound.Shake) > 0 && !audio.Sounds.TechniqueSource.isPlaying,
            "Shake audio must follow actual motion and stop on its return.");
        shake.StrainAndSubmit();
        while (shake.IsPhysicalTransferPending) yield return null;
        Require(cueCounts.GetValueOrDefault(BarSound.Strain) > 0, "Strainer stream produced no audio.");
        Require(cueCounts.GetValueOrDefault(BarSound.Ice) == 1, "One ice placement must produce one ice cue.");
        SURLHWAReviewCapture.Save("garnish", 1280, 720);
        shake.ServeShake();
        deadline = Time.realtimeSinceStartup + 15f;
        while (!completed)
        {
            Require(Time.realtimeSinceStartup < deadline, "Bartending unload timed out.");
            Require(ActiveListeners() == 1, "Audio listener disappeared during asynchronous result cleanup.");
            yield return null;
        }
        Require(ActiveListeners() == 1, "Bar audio listener was not restored after unload.");
        Require(!coordinator.IsBartendingActive && !audio.Sounds.PourSource.isPlaying,
            "Bartending audio/lifecycle did not stop after unload.");
        Debug.Log("[Audio PlayMode] PASS: imported clips, scene-owned playback, one active listener before/during/after bartending, actual volume/mute controls, " +
            "preference reset isolation, explicit additive handoff, accepted pour start/stop, " +
            "Guide/settings pause preserves Shake timing, resume and unload. Auditory mix remains human sign-off.");
        Require(!audio.Sounds.TechniqueSource.isPlaying && audio.GetComponents<AudioSource>().Length == SurlhwaSoundscape.SourceCount,
            "Stale loop or extra voice after additive unload.");
        Debug.Log("[Phase O Audio] synthetic loop cancellation/disabled-owner fixture follows physical test");
        audio.Sounds.SetPour(BarSound.Pour);
        yield return null;
        yield return null;
        Require(audio.Sounds.PourSource.isPlaying, "Cancellation fixture failed to begin its loop.");
        audio.Sounds.StopPhysical();
        yield return new WaitForSecondsRealtime(.12f);
        Require(!audio.Sounds.PourSource.isPlaying, "Cancelled workspace loop survived its short fade.");
        bool disabledSilent = true;
        try
        {
            audio.Sounds.enabled = false;
            audio.Sounds.Play(BarSound.Select);
            audio.Sounds.StartDiscovery();
            foreach (var source in sources) disabledSilent &= !source.isPlaying;
        }
        finally { audio.Sounds.enabled = true; }
        Require(disabledSilent, "Disabled audio owner accepted playback after cleanup.");
        Debug.Log("[Phase O Audio] PASS: Shake motion/return, Strain stream, one ice cue, bounded sources, disabled-owner silence.");
        Complete(true);
    }
    private static int ActiveListeners()
    {
        int count = 0;
        foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            if (listener.enabled && listener.gameObject.activeInHierarchy) count++;
        return count;
    }
    private void Complete(bool success)
    {
        if (ended) return;
        ended = true;
        Application.runInBackground = previousBackground;
        SURLHWAAudioPlayModeValidation.Complete(success);
    }
}
#endif
