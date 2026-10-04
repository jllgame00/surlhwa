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
public static class SURLHWAJ5PrinceClosurePlayModeValidation
{
    private const string Prefix = "SURLHWA_J5_PROBE_";
    private static readonly string[] ExtraKeys = { "currentDay", "SURLHWA_DEMO_COMPLETE_V1", "SURLHWA_FIRST_RECIPE_REVEAL_SHOWN" };
    static SURLHWAJ5PrinceClosurePlayModeValidation()
    {
        EditorApplication.playModeStateChanged -= Changed;
        EditorApplication.playModeStateChanged += Changed;
    }
    [MenuItem("SURLHWA/Validation/Run J5 Prince Closure Play Mode")]
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
        BusinessDayPersistence.SaveCurrentDay(2);
        SessionState.SetBool(Prefix + "Pending", true);
        EditorSceneManager.OpenScene("Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity");
        EditorApplication.EnterPlaymode();
    }
    private static IEnumerable<string> AllKeys()
    {
        foreach (var key in Keys) yield return key;
        foreach (var key in ExtraKeys) yield return key;
        foreach (var key in SURLHWACampaignValidation.ProgressKeys()) yield return key;
    }
    private static void Changed(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Prefix + "Pending", false))
            new GameObject("J5 Runtime Validation").AddComponent<SURLHWAJ5PrinceClosureRunner>();
        if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Prefix + "Complete", false)) return;
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

internal sealed class SURLHWAJ5PrinceClosureRunner : MonoBehaviour
{
    private int previousFrameRate, previousVSync;
    private SurlhwaSoundscape sounds;
    private AudioClip originalDiscovery, longDiscovery;
    private SurlhwaAudioLibrary audioLibrary;
    private int discoverySounds;
    private int scenarioNumber, visitNumber, popupOpens, popupCompletions, toastCompletions, farewellEntries, discoveries, serviceCalls, departures;
    private bool observedPopup, observedFarewell, overlap;
    private bool capturePopup;
    private string lastTimeline;
    private string runtimeFailure;
    private RecipeUnlockPopupController popup;
    private ResultToastController toast;
    private RectTransform popupRoot;
    private SeatPoint closingSeat;
    private readonly List<string> timeline = new();
    private bool finished;
    private float began;
    private bool previousBackground;
    private CustomerController customer;
    private CustomerSpawner spawner;
    private BusinessDayManager day;
    private InteractionUIController interaction;
    private OrderManager orders;
    private RecipeUnlockManager knowledge;
    private LittlePrinceNarrativeData narrative;
    private void Start()
    {
        began = Time.realtimeSinceStartup;
        previousBackground = Application.runInBackground;
        previousFrameRate = Application.targetFrameRate; previousVSync = QualitySettings.vSyncCount;
        QualitySettings.vSyncCount = 0; Application.targetFrameRate = 30;
        Application.logMessageReceived += ObserveLog;
        System.IO.Directory.CreateDirectory("Logs/Review/J5FarewellDiscovery");
        Application.runInBackground = true;
        StartCoroutine(Guarded());
    }
    private void Update()
    {
        if (!finished && Time.realtimeSinceStartup - began > 300f)
        {
            Debug.LogError("[J5 PlayMode] Timed out.");
            Finish(false);
        }
    }
    private void ObserveLog(string message, string stack, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error) runtimeFailure = message;
        if (message.Contains("[OrderManager] RECIPE BECAME KNOWN")) discoveries++;
        if (message.StartsWith("[Customer] ServedCocktail=")) serviceCalls++;
        if (message.StartsWith("[CustomerSpawner] Customer left.")) departures++;
    }
    private void Record(string message)
    {
        string line = $"t={Time.time:F3} scenario={scenarioNumber} visit={visitNumber} {message}";
        timeline.Add(line); Debug.Log("[J5 Sequence] " + line);
    }
    private void Capture(string name)
    {
        SURLHWAReviewCapture.Save("J5FarewellDiscovery/" + name, 1280, 720);
        SURLHWAReviewCapture.Save("J5FarewellDiscovery/" + name, 1920, 1080);
    }
    private void LateUpdate()
    {
        if (finished || popupRoot == null || interaction == null) return;
        bool visible = popupRoot.gameObject.activeInHierarchy;
        bool farewell = Get(interaction, "farewellCompleted") != null;
        // A bounded cadence change exposes frame-rounded completion errors.
        // This is test scheduling only; production timing/timeScale is untouched.
        Application.targetFrameRate = visible ? 10 : 30;
        if (visible && !observedPopup)
        {
            popupOpens++;
            var original = (Action<bool>)Get(popup, "presentationCompleted");
            Set(popup, "presentationCompleted", new Action<bool>(completed => {
                popupCompletions++;
                Record("popup-complete=" + completed + " hidden=" + !popupRoot.gameObject.activeInHierarchy);
                original?.Invoke(completed);
                if (scenarioNumber == 0 && visitNumber == 3) Capture("popup-closed");
            }));
            Record("popup-open recipe=" + ((TMP_Text)Get(popup, "titleText")).text);
            capturePopup = scenarioNumber == 0 && visitNumber == 3;
        }
        if (capturePopup && popupRoot.anchoredPosition.x < 0)
        {
            capturePopup = false;
            var hits = new List<UnityEngine.EventSystems.RaycastResult>();
            var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            { position = RectTransformUtility.WorldToScreenPoint(null, popupRoot.TransformPoint(popupRoot.rect.center)) };
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
            foreach (var hit in hits)
                Require(!hit.gameObject.transform.IsChildOf(popupRoot), "Discovery decoration must not intercept pointer raycasts.");
            Record("popup raycast hits=" + hits.Count + "; no discovery blocker");
            Capture("popup-open");
        }
        if (farewell && !observedFarewell)
        {
            farewellEntries++;
            Record("farewell-entry popup=" + visible + " toast-alpha=" + ((CanvasGroup)Get(toast, "canvasGroup")).alpha);
            if (scenarioNumber == 0 && visitNumber == 3) Capture("farewell-entry");
        }
        overlap |= farewell && visible;
        string state = "popup=" + visible + " farewell=" + farewell + " customer=" + (customer != null ? customer.CurrentState.ToString() : "none") +
            " modal=" + (SurlhwaModalSurface.Top != null ? SurlhwaModalSurface.Top.name : "none");
        if (state != lastTimeline) { Record(state); lastTimeline = state; }
        observedPopup = visible; observedFarewell = farewell;
    }
    private IEnumerator Guarded()
    {
        IEnumerator run = RunScenarios();
        while (!finished)
        {
            object wait = null;
            bool next = false;
            try { next = run.MoveNext(); if (next) wait = run.Current; }
            catch (Exception e) { Debug.LogException(e); Finish(false); }
            if (!next || finished) yield break;
            yield return wait;
        }
    }
    private IEnumerator RunScenarios()
    {
        yield return null;
        spawner = FindFirstObjectByType<CustomerSpawner>();
        day = FindFirstObjectByType<BusinessDayManager>();
        interaction = FindFirstObjectByType<InteractionUIController>();
        orders = FindFirstObjectByType<OrderManager>();
        knowledge = FindFirstObjectByType<RecipeUnlockManager>();
        sounds = FindFirstObjectByType<DemoAudioController>().Sounds;
        sounds.SetMix(.7f, .35f, .6f, false); sounds.Trace = true;
        audioLibrary = sounds.Library;
        originalDiscovery = audioLibrary.Get(BarSound.Discovery).clip;
        longDiscovery = AudioClip.Create("J5 long audio duration fixture", 44100 * 30, 1, 44100, false);
        sounds.Library.Get(BarSound.Discovery).clip = longDiscovery;
        sounds.CueChanged += CountDiscoveryAudio;
        popup = FindFirstObjectByType<RecipeUnlockPopupController>();
        toast = FindFirstObjectByType<ResultToastController>();
        popupRoot = (RectTransform)Get(popup, "popupRoot");
        narrative = Load<LittlePrinceNarrativeData>("Narrative/LittlePrinceNarrative.asset");
        Require(spawner != null && day != null && interaction != null && orders != null && knowledge != null, "Production controllers missing.");
        spawner.enabled = false;
        // This focused fixture explicitly exercises the retained standalone demo endpoint.
        Set(day, "campaign", null);
        Set(day, "endDayWhenMaxCustomersServed", false);
        var gm = FindFirstObjectByType<GameManager>();
        ((GameObject)Get(gm, "entryPanel"))?.SetActive(false);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_SURLHWA/Prefabs/Customers/SpecialGuests/LittlePrinceCustomer.prefab");
        var first = Load<OrderData>("Orders/LittlePrinceFirstVisitOrder.asset");
        var follow = Load<OrderData>("Orders/LittlePrinceFollowupOrder.asset");
        var closing = Load<OrderData>("Orders/LittlePrinceClosingOrder.asset");
        var manwol = Recipe("recipe_manwol");
        var seolya = Recipe("recipe_test_stir");
        // Seolya is the selected Known baseline, not the reserve/compatibility alternatives.
        Require(seolya.targetCocktail.cocktailName == "설야", "Expected authored Seolya baseline.");
        knowledge.MarkKnown(manwol);
        knowledge.MarkKnown(seolya);
        var nextButton = (Button)Get(interaction, "nextButton");
        var brewButton = (Button)Get(interaction, "brewButton");
        var cancelButton = (Button)Get(interaction, "cancelButton");
        for (int scenario = 0; scenario < 6; scenario++)
        {
            scenarioNumber = scenario;
            ClearVisits();
            DemoCompletionPersistence.ClearDemoCompletion();
            var choice = (LittlePrinceNarrativeChoice)(scenario % 3);
            if (choice != LittlePrinceNarrativeChoice.None) LittlePrinceNarrativePersistence.TryCommitChoice(choice);
            for (int visit = 1; visit <= 3; visit++)
            {
                visitNumber = visit;
                popupOpens = popupCompletions = toastCompletions = farewellEntries = discoveries = serviceCalls = departures = discoverySounds = 0;
                overlap = false;
                Set(day, "currentDay", visit + 1);
                Set(day, "servedCustomerCount", visit == 3 ? 3 : 0);
                Set(day, "dayEnded", false);
                Set(spawner, "spawnedCount", visit == 3 ? 4 : 1);
                customer = Instantiate(prefab).GetComponent<CustomerController>();
                customer.enabled = false; // Earlier visits retain the focused service fixture.
                customer.SetLoopManager(spawner);
                OrderData order = visit == 1 ? first : visit == 2 ? follow : closing;
                if (visit == 1) customer.ConfigureLittlePrinceFirstVisitNarrative();
                else if (visit == 2) customer.ConfigureLittlePrinceFollowupEncounter(order);
                else customer.ConfigureLittlePrinceClosingEncounter(order);
                if (visit == 3)
                {
                    closingSeat = FindFirstObjectByType<SeatManager>().GetFreeSeat(customer);
                    Require(closingSeat != null, "Closing fixture needs a real reserved seat.");
                    customer.AssignSeat(closingSeat);
                    customer.transform.position = closingSeat.sitMarker.position;
                }
                var stateField = typeof(CustomerController).GetField("state", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                stateField.SetValue(customer, Enum.Parse(stateField.FieldType, "Waiting"));
                // The closing visit uses normal sorting and departure Update;
                // only its already-seated starting position is a fixture shortcut.
                if (visit == 3) customer.enabled = true;
                Set(spawner, "activeCustomer", customer);
                Set(spawner, "activeCustomerIsLittlePrinceFirstVisit", visit == 1);
                Set(spawner, "activeCustomerIsLittlePrinceFollowupVisit", visit == 2);
                Set(spawner, "activeCustomerIsLittlePrinceClosingVisit", visit == 3);
                interaction.RegisterCustomer(customer);
                if (visit > 1)
                {
                    Require((bool)SURLHWAJ5PrinceClosureValidation.Invoke(interaction, "TryOpenDedicatedPreRequestDialogue"), "Later visit must expose its authored request.");
                    customer.TryGetDialogueData(out var requestDialogue);
                    for (int line = 0; line < requestDialogue.onEnterLines.Length; line++) nextButton.onClick.Invoke();
                    brewButton.onClick.Invoke();
                    Require(((GameObject)Get(interaction, "brewPanel")).activeSelf, "Recommendation must expose real method choice.");
                }
                CocktailRecipeData recipe = visit == 2 ? seolya : manwol;
                if (visit == 3 && scenario == 0) recipe = Recipe("recipe_dry_martini");
                if (visit == 3 && scenario == 2) recipe = Recipe("recipe_gin_fizz");
                if (scenario == 5 && visit == 3) recipe = seolya;
                MiniGameResult result = Result(recipe);
                if (scenario == visit) { result.success = false; result.grade = "Poor"; }
                if (scenario == 4 && visit == 3) result.preparationTrace = new BartendingPreparationTrace();
                // Later Recommendation evaluation must use actual recognition, with no expected answer template.
                if (visit > 1) { result.expectedRecipe = null; result.preparationEvaluation = null; }
                Set(orders, "_currentCustomer", customer);
                Set(orders, "currentOrder", order);
                Set(orders, "currentGuestRequest", GuestRequestContext.FromOrder(order));
                bool wasKnown = knowledge.IsKnown(recipe);
                Record("service-result recipe=" + recipe.recipeId + " already-known=" + wasKnown);
                SURLHWAJ5PrinceClosureValidation.Invoke(orders, "OnMiniGameDone", result);
                if (visit == 3)
                {
                    var completed = (Action<bool>)Get(toast, "presentationCompleted");
                    Set(toast, "presentationCompleted", new Action<bool>(ok => {
                        toastCompletions++;
                        Record("toast-complete=" + ok + " alpha=" + ((CanvasGroup)Get(toast, "canvasGroup")).alpha);
                        completed?.Invoke(ok);
                    }));
                }
                string savedKnowledge = RecipeProgressPersistence.LoadRecipeKnowledgeRaw();
                bool successful = !(scenario == visit || (visit == 3 && scenario >= 4));
                Require(LittlePrinceVisitPersistence.GetServiceOutcome(visit) == (successful ? 1 : 2),
                    "Actual OrderManager service did not persist FIT + technique + preparation truth.");
                Require(Get(orders, "_currentCustomer") == null, "Failed narrative service must resolve rather than retain a retry order.");
                if (visit == 3)
                {
                    float deadline = Time.realtimeSinceStartup + 15f;
                    while (Get(interaction, "farewellCompleted") == null && Time.realtimeSinceStartup < deadline) yield return null;
                    Require(Get(interaction, "farewellCompleted") != null, "Final farewell did not appear after feedback.");
                    var toast = FindFirstObjectByType<ResultToastController>();
                    Require(((CanvasGroup)Get(toast, "canvasGroup")).alpha <= .001f, "Farewell must wait for diagnostics to close.");
                    var popup = FindFirstObjectByType<RecipeUnlockPopupController>();
                    Require(!((RectTransform)Get(popup, "popupRoot")).gameObject.activeInHierarchy,
                        "Farewell must wait for a newly discovered recipe popup to close.");
                    yield return null; // Let the frame observer count actual UI entry.
                    Require(!overlap && farewellEntries == 1, "Farewell must enter once without popup overlap.");
                    Require(serviceCalls == 1 && discoveries == (knowledge.IsKnown(recipe) && !wasKnown ? 1 : 0), "Service/discovery must resolve exactly once.");
                    Require(popupOpens == discoveries && popupCompletions == discoveries, "Each discovery must open and complete once.");
                    Require(discoverySounds == discoveries, "Discovery audio did not start once per popup.");
                    yield return new WaitForSecondsRealtime(.12f);
                    Require(!sounds.DiscoverySource.isPlaying, "Long discovery audio survived visual completion.");
                    Record("audio starts=" + discoverySounds + "; 30-second audio does not delay farewell; stopped after visual completion");
                    Require(toastCompletions == 1, "Farewell must follow exactly one diagnostic completion.");
                    Require(RecipeProgressPersistence.LoadRecipeKnowledgeRaw() == savedKnowledge, "Presentation must not rewrite recipe persistence.");
                    foreach (var graphic in popupRoot.GetComponentsInChildren<Graphic>(true))
                        Require(!graphic.raycastTarget, "Transient discovery must remain nonblocking.");
                    Require(SurlhwaModalSurface.Top == null, "Farewell must not inherit an invisible modal owner.");
                    Record($"counts service={serviceCalls} discovery={discoveries} toast-complete={toastCompletions} popup-open={popupOpens} popup-complete={popupCompletions} farewell={farewellEntries}; knowledge unchanged");
                    string[] lines = narrative.GetFarewellLines(choice, scenario == 0);
                    var text = (TMP_Text)Get(interaction, "dialogueText");
                    Require(text.text == lines[0] && !LittlePrinceVisitPersistence.IsClosingVisitCompleted() && !day.IsDayEnded,
                        "Farewell must hold the guest and final night until the player finishes reading.");
                    yield return new WaitForSeconds(.3f);
                    brewButton.onClick.Invoke(); cancelButton.onClick.Invoke();
                    Require(Get(interaction, "farewellCompleted") != null && text.text == lines[0], "Brew/cancel cannot bypass the farewell.");
                    for (int line = 0; line < lines.Length; line++)
                    {
                        Require(text.text == lines[line], "Wrong authored farewell line for the actual service history.");
                        nextButton.onClick.Invoke();
                    }
                    yield return null;
                    if (scenario == 5)
                    {
                        Set(day, "endDayWhenMaxCustomersServed", true);
                        BusinessDayPersistence.SaveCurrentDay(4);
                    }
                }
                else yield return new WaitForSeconds(4.4f);
                // This fixture disables customer Update above. Phase N's finite
                // stand interval belongs to that Update, not an animation callback.
                Require(customer.CurrentState == CustomerController.State.Standing,
                    "Service/reading must request standing departure, even on failure.");
                if (visit == 3)
                {
                    customer.enabled = true;
                    bool sawLeaving = false;
                    float exitDeadline = Time.realtimeSinceStartup + 15f;
                    while (customer != null && Time.realtimeSinceStartup < exitDeadline)
                    { sawLeaving |= customer.CurrentState == CustomerController.State.Leaving; yield return null; }
                    Require(customer == null && sawLeaving && !closingSeat.IsOccupied && closingSeat.Occupant == null,
                        "Closing guest must stand, walk out naturally, release its seat and be destroyed.");
                    Require(departures == 1 && Get(spawner, "activeCustomer") == null,
                        "Closing departure and spawner progression must happen once.");
                    Require(FindObjectsByType<CustomerMotionPresentation>(FindObjectsSortMode.None).Length == 0,
                        "No customer presentation may remain after departure.");
                    Record("natural-departure=1 seat-released=true visit-complete=" + LittlePrinceVisitPersistence.IsClosingVisitCompleted());
                }
                else
                {
                    yield return new WaitForSeconds(customer.standDuration + .02f);
                    SURLHWAJ5PrinceClosureValidation.Invoke(customer, "Update");
                    Require(customer.CurrentState == CustomerController.State.Leaving,
                        "The bounded stand interval must finish without an animation completion event.");
                    SURLHWAJ5PrinceClosureValidation.Invoke(customer, "LeaveAndDestroy");
                }
                customer = null;
                spawner.StopAllCoroutines();
                if (visit == 1) Require(LittlePrinceVisitPersistence.IsFirstVisitCompleted(), "First departure did not complete visit.");
                if (visit == 2) Require(LittlePrinceVisitPersistence.IsFollowupVisitCompleted(), "Return departure did not complete visit.");
                yield return null;
            }
            Require(LittlePrinceVisitPersistence.IsClosingVisitCompleted() &&
                    LittlePrinceVisitPersistence.IsStatueEligible() == (scenario == 0), "Closure/eligibility differs from actual history.");
            Debug.Log("[J5 PlayMode] Scenario " + scenario + " PASS (0=all good; 1-3=technique failure by visit; 4=preparation; 5=wrong FIT).");
        }
        Require(day.IsDemoComplete && day.IsDayEnded && BusinessDayPersistence.LoadCurrentDay(1) == 4 &&
                !day.CanSpawnMoreCustomers(), "Final failed service must still close the demo without Day 5.");
        // Presentation-only cancellation fixtures never rediscover a recipe.
        string knowledgeBeforeCancellation = RecipeProgressPersistence.LoadRecipeKnowledgeRaw();
        int replaced = 0, disabled = 0, missing = 0;
        popup.ShowDelayed(manwol, 0f, completed: ok => { Require(!ok, "Replaced popup must cancel."); replaced++; });
        popup.ShowDelayed(seolya, 0f, completed: ok => { Require(!ok, "Disabled popup must cancel."); disabled++; });
        popup.enabled = false;
        popup.enabled = true;
        popup.enabled = false;
        popup.ShowDelayed(null, 0f, completed: ok => { Require(!ok, "Missing presentation must release its waiter."); missing++; });
        popup.enabled = true;
        Require(replaced == 1 && disabled == 1 && missing == 1 && !popupRoot.gameObject.activeSelf,
            "Replacement, disable and missing-art cancellation must hide and signal exactly once.");
        int toastCancelled = 0;
        toast.ShowDelayed("Fixture", "Cancellation", 0f, ok => { Require(!ok, "Disabled toast must cancel."); toastCancelled++; });
        toast.enabled = false;
        toast.enabled = true;
        toast.enabled = false;
        toast.enabled = true;
        Require(toastCancelled == 1 && ((CanvasGroup)Get(toast, "canvasGroup")).alpha == 0f,
            "Disabled toast must hide and signal once.");
        Require(knowledgeBeforeCancellation == RecipeProgressPersistence.LoadRecipeKnowledgeRaw(),
            "Presentation cancellation must not change recipe persistence.");
        Record("cancellation counts replaced=1 disabled=1 missing=1 toast=1; no knowledge change");
        SurlhwaGameProgress.ResetGameProgress();
        Require(!LittlePrinceVisitPersistence.HasSavedClosureState() && !LittlePrinceVisitPersistence.IsStatueEligible(), "New Game failed to reset J5 history.");
        Debug.Log("[J5 PlayMode] PASS: production service handoff, all three failure positions, preparation/fit negatives, read-to-advance farewell, departure persistence, final-night completion and New Game reset. This is not normal-input bartending validation.");
        Require(runtimeFailure == null, "Runtime error during J5 sequence: " + runtimeFailure);
        Finish(true);
    }
    private CocktailRecipeData Recipe(string id)
    {
        foreach (var recipe in knowledge.GetAllRecipes()) if (recipe.recipeId == id) return recipe;
        throw new InvalidOperationException("Missing " + id);
    }
    private MiniGameResult Result(CocktailRecipeData recipe)
    {
        var trace = new BartendingPreparationTrace();
        foreach (var step in recipe.expectedPreparation)
        {
            if (step == null || step.optional) continue;
            for (int n = 0; n < Mathf.Max(1, step.repeatCount); n++)
                trace.Record(step.action, step.ingredient, 0f,
                    step.source == RecipePreparationContainer.Any ? null : (BartendingContainerType)((int)step.source - 1),
                    step.target == RecipePreparationContainer.Any ? null : (BartendingContainerType)((int)step.target - 1));
        }
        var ingredients = new List<BrewIngredientEntry>();
        foreach (var item in recipe.ingredients) ingredients.Add(new BrewIngredientEntry { ingredientId = item.ingredient.ingredientId, amount = item.ratio });
        return new MiniGameResult { success = true, grade = "Good", expectedRecipe = recipe,
            brewedComposition = new BrewComposition { method = recipe.method, ingredients = ingredients.ToArray() },
            preparationTrace = trace, preparationEvaluation = RecipePreparationEvaluator.Evaluate(recipe, trace) };
    }
    private void CountDiscoveryAudio(BarSound cue, bool start, string channel)
    {
        if (cue == BarSound.Discovery && start) discoverySounds++;
    }
    private void OnDestroy()
    {
        if (sounds != null)
        {
            sounds.CueChanged -= CountDiscoveryAudio;

        }
        if (audioLibrary != null) audioLibrary.Get(BarSound.Discovery).clip = originalDiscovery;
        if (longDiscovery != null) Destroy(longDiscovery);
    }
    private void Finish(bool success)
    {
        if (finished) return;
        finished = true;
        Application.runInBackground = previousBackground;
        Application.targetFrameRate = previousFrameRate; QualitySettings.vSyncCount = previousVSync;
        Application.logMessageReceived -= ObserveLog;
        System.IO.File.WriteAllLines("Logs/Review/J5FarewellDiscovery/after-timeline.txt", timeline);
        if (customer != null) Destroy(customer.gameObject);
        spawner?.StopAllCoroutines();
        SURLHWAJ5PrinceClosurePlayModeValidation.Complete(success);
    }
}
#endif
