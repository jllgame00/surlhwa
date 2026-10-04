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
public static class SURLHWACampaignPlayModeValidation
{
    private const string Prefix = "SURLHWA_CAMPAIGN_PROBE_";
    private static readonly string[] ExtraKeys = { "currentDay", "SURLHWA_DEMO_COMPLETE_V1", "SURLHWA_FIRST_RECIPE_REVEAL_SHOWN" };
    static SURLHWACampaignPlayModeValidation()
    {
        EditorApplication.playModeStateChanged -= Changed;
        EditorApplication.playModeStateChanged += Changed;
    }
    [MenuItem("SURLHWA/Validation/Run Full Campaign Play Mode")]
    public static void Run() => Start(false);
    [MenuItem("SURLHWA/Validation/Run Full Campaign Quiet Prince Route")]
    public static void RunQuiet() => Start(true);
    public static void RunEnding() => Start(false, true);
    internal static bool QuietPrince => SessionState.GetBool(Prefix + "QuietPrince", false);
    internal static bool EndingFixture => SessionState.GetBool(Prefix + "EndingFixture", false);
    private static void Start(bool quietPrince, bool endingFixture = false)
    {
        SessionState.SetBool(Prefix + "QuietPrince", quietPrince);
        SessionState.SetBool(Prefix + "EndingFixture", endingFixture);
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
        if (endingFixture)
        {
            BusinessDayPersistence.SaveCurrentDay(18);
            CampaignProgressPersistence.RecordDeparture(18, 2, CampaignVisitId.OdysseusStay);
        }
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
            new GameObject("Campaign Runtime Validation").AddComponent<SURLHWACampaignRunner>();
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
        foreach (string key in new[] { "HadKnowledge", "HadLegacy", "Pending", "Complete", "Success", "QuietPrince", "EndingFixture" }) SessionState.EraseBool(Prefix + key);
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


internal sealed class SURLHWACampaignRunner : MonoBehaviour
{
    private float began;
    private bool finished;
    private bool previousBackground;
    private bool capturedService, capturedDiscovery;
    private readonly System.Collections.Generic.Stack<IEnumerator> work = new();
    private readonly System.Collections.Generic.HashSet<MiniGameType> methods = new();
    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        began = Time.realtimeSinceStartup;
        previousBackground = Application.runInBackground;
        Application.runInBackground = true;
        work.Push(Run());
        StartCoroutine(Guarded());
    }
    private void LateUpdate()
    {
        // Observe real scheduled service; never manufacture a result or knowledge event.
        var toast = Find<ResultToastController>();
        if (!capturedService && toast != null && ((CanvasGroup)Get(toast, "canvasGroup")).alpha >= .999f)
        {
            capturedService = true;
            SURLHWAReviewCapture.Save("phase-m-normal-service", 1280, 720);
            SURLHWAReviewCapture.Save("phase-m-normal-service", 1920, 1080);
        }
        var popup = Find<RecipeUnlockPopupController>();
        if (!capturedDiscovery && popup != null)
        {
            var root = (RectTransform)Get(popup, "popupRoot");
            if (root.gameObject.activeInHierarchy && Vector2.Distance(root.anchoredPosition, (Vector2)Get(popup, "shownPosition")) < .1f)
            {
                if (toast != null && ((CanvasGroup)Get(toast, "canvasGroup")).alpha > .01f)
                {
                    Debug.LogError("[Phase M sequence] Discovery overlaps service feedback.");
                    Finish(false); return;
                }
                capturedDiscovery = true;
                SURLHWAReviewCapture.Save("phase-m-normal-discovery", 1280, 720);
                SURLHWAReviewCapture.Save("phase-m-normal-discovery", 1920, 1080);
            }
        }
    }
    private IEnumerator Guarded()
    {
        while (work.Count > 0 && !finished)
        {
            object current = null;
            try
            {
                if (Time.realtimeSinceStartup - began > 1800f)
                    throw new InvalidOperationException("Campaign validation timed out.");
                if (!work.Peek().MoveNext()) { work.Pop(); continue; }
                current = work.Peek().Current;
                if (current is IEnumerator nested) { work.Push(nested); continue; }
            }
            catch (Exception e) { Debug.LogException(e); Finish(false); yield break; }
            yield return current;
        }
    }
    private IEnumerator Wait(Func<bool> ready, string label, float seconds = 35f)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!ready())
        {
            Require(Time.realtimeSinceStartup < deadline, "Timed out: " + label);
            yield return null;
        }
    }
    private static T Find<T>() where T : UnityEngine.Object => FindFirstObjectByType<T>();
    private static void Click(Button button)
    {
        Require(button != null && button.gameObject.activeInHierarchy && button.interactable,
            "Expected active/interactable production button.");
        button.onClick.Invoke();
    }
    private static void ClickField(object owner, string name) => Click((Button)Get(owner, name));
    private static Button NamedButton(string name)
    {
        foreach (var b in FindObjectsByType<Button>(FindObjectsSortMode.None))
            if (b.name == name) return b;
        return null;
    }
    private CocktailRecipeData Recipe(string id)
    {
        foreach (var r in Find<RecipeUnlockManager>().GetAllRecipes()) if (r.recipeId == id) return r;
        throw new InvalidOperationException("Missing recipe " + id);
    }
    private IEnumerator ContinueFromTitle(int night, int slot)
    {
        GameManager.ReturnToDemoEntry();
        yield return null;
        yield return Wait(() => NamedButton("ContinueButton") != null, "Continue entry");
        Click(NamedButton("ContinueButton"));
        yield return null;
        yield return Wait(() => Find<GameManager>() != null && Find<GameManager>().IsGameplaySessionStarted,
            "Continue gameplay");
        Require(Find<BusinessDayManager>().CurrentDay == night && Find<BusinessDayManager>().ServedCustomerCount == slot,
            "Continue lost the departure checkpoint.");
        ValidateMemories();
    }
    private void ValidateMemories()
    {
        var presentation = Find<CampaignBarMemories>();
        Require(presentation != null, "Missing scene-bound bar memory presenter.");
        presentation.Refresh();
        int count = LittlePrinceVisitPersistence.IsClosingVisitCompleted() ? 1 : 0;
        foreach (var visit in new[] { CampaignVisitId.LoversReunion, CampaignVisitId.QuixoteAlone,
            CampaignVisitId.HamletStillness, CampaignVisitId.FaustCost, CampaignVisitId.SimCheongReturn, CampaignVisitId.OdysseusStay })
            if (CampaignProgressPersistence.IsCompleted(visit)) count++;
        Require(presentation.VisibleMemoryCount == count, "Bar memory projection differs from departed ACTs.");
        foreach (var line in presentation.GetComponentsInChildren<LineRenderer>())
            Require(line.sharedMaterial != null && line.sharedMaterial.shader != null, "Unrenderable memory contour.");
    }
    private CocktailRecipeData ChooseAnswer(OrderData order, int night, int slot)
    {
        var knowledge = Find<RecipeUnlockManager>();
        if (night == 1 && slot <= 3) return Recipe("recipe_test_stir");
        if (order.RequestType == GuestRequestType.ExactCocktail)
            return knowledge.GetAllRecipes().Find(r => r.targetCocktail == order.requestedCocktail);
        var availability = (IngredientAvailabilityData)Get(Find<CustomerSpawner>(), "ingredientAvailability");
        var candidates = new List<CocktailRecipeData>();
        foreach (var recipe in knowledge.GetAllRecipes())
        {
            if (!knowledge.IsKnown(recipe)) continue;
            bool supplied = true;
            foreach (var entry in recipe.ingredients)
                supplied &= availability.IsIngredientAvailable(entry.ingredient, new IngredientAvailabilityContext(night));
            if (supplied && RequestEvaluation.Evaluate(order, new RecipeMatchResult {
                success = true, matchedRecipe = recipe, similarity = 1f }).Status == RequestEvaluationStatus.Satisfied)
                candidates.Add(recipe);
        }
        Require(candidates.Count >= (night >= 5 ? 2 : 1), "Recommendation lacks learned/supplied alternatives.");
        return candidates[(night + slot) % candidates.Count];
    }
    private IEnumerator Run()
    {
        yield return null;
        if (SURLHWACampaignPlayModeValidation.EndingFixture)
        {
            yield return ContinueFromTitle(18, 2);
            var fixturePanel = Find<DayResultPanelController>();
            Require(fixturePanel != null, "Ending panel closed itself during first activation.");
            ClickField(fixturePanel, "restartButton");
            Require(Find<BusinessDayManager>().CurrentDay == 18 && !CampaignProgressPersistence.EndingRead,
                "First ending click started gameplay or completed unread text.");
            yield return ContinueFromTitle(18, 2);
            fixturePanel = Find<DayResultPanelController>();
            Require((int)Get(fixturePanel, "endingLineIndex") == 0, "Interrupted ending must replay.");
            for (int i = 0; i < Find<BusinessDayManager>().Campaign.ending.onEnterLines.Length; i++)
            {
                var body = (TMP_Text)Get(fixturePanel, "customerCountText");
                body.ForceMeshUpdate();
                Require(body.preferredHeight <= body.rectTransform.rect.height, "Ending text exceeds reading area.");
                if (i == 5) SURLHWAReviewCapture.Save("campaign-ending-layout",1280,720);
                ClickField(fixturePanel, "restartButton");
            }
            Require(CampaignProgressPersistence.EndingRead, "Ending read not saved.");
            yield return ContinueFromTitle(18, 2);
            Require(!Find<BusinessDayManager>().CanSpawnMoreCustomers(), "Completed ending spawned another night.");
            Debug.Log("[Campaign Ending Fixture] PASS: inactive serialized panel activation, every line fits, interrupted replay, completed Continue and navigation. Seeded endpoint fixture; not a full-route claim.");
            Finish(true);
            yield break;
        }
        Click(NamedButton("NewGameButton"));
        yield return null;
        yield return Wait(() => Find<GameManager>() != null && Find<GameManager>().IsGameplaySessionStarted, "New Game");
        Require(!Find<RecipeUnlockManager>().IsKnown(Recipe("recipe_hanky_panky")), "New Game pre-granted reserve knowledge.");
        int departures = 0;
        for (int night = 1; night <= 18; night++)
        {
            var day = Find<BusinessDayManager>();
            Require(day.CurrentDay == night && day.HasCampaign, "Wrong campaign night.");
            int count = day.MaxCustomersPerDay;
            for (int slot = 1; slot <= count; slot++)
            {
                var ui = Find<InteractionUIController>();
                yield return Wait(() => Get(ui,"activeCustomer") != null &&
                    ((Button)Get(ui,"ellipsisButton")).gameObject.activeInHierarchy, "guest arrival");
                var guest = (CustomerController)Get(ui,"activeCustomer");
                if (night == 1 && slot == 1) SURLHWAReviewCapture.Save("restart-normal-play-ordinary",1280,720);
                if (guest.LittlePrinceVisitNumber == 1) SURLHWAReviewCapture.Save("restart-normal-play-prince",1280,720);
                if (guest.CampaignEncounter != null && guest.CampaignEncounter.visit == CampaignVisitId.ChunhyangMeeting)
                    SURLHWAReviewCapture.Save("restart-normal-play-chunhyang",1280,720);
                var encounter = guest.CampaignEncounter;
                Require(guest.TryGetDedicatedRequest(out var order), "Missing authored order.");
                if (encounter != null)
                {
                    Require(encounter == day.Campaign.GetEncounter(night,slot-1), "Wrong schedule slot.");
                    if (encounter.IsSpecial) Require(!CampaignProgressPersistence.IsCompleted(encounter.visit), "Completed ACT repeated.");
                    var staging = guest.GetComponent<CampaignGuestPresentation>();
                    Require(!encounter.sharedGlass || staging.HasCompanion, "Reunion has no second person.");
                    Require(!encounter.stillness || (staging.HasStillness && Time.timeScale == 1f), "Hamlet changed global time.");
                }
                ClickField(ui,"ellipsisButton");
                while ((bool)Get(ui,"isPreRequestDialogueActive")) ClickField(ui,"nextButton");
                if ((bool)Get(ui,"isLittlePrinceChoiceActive")) ClickField(ui, SURLHWACampaignPlayModeValidation.QuietPrince ? "brewButton" : "nextButton");
                if (night == 8 && slot == 2)
                {
                    yield return ContinueFromTitle(night,slot-1);
                    day = Find<BusinessDayManager>(); ui = Find<InteractionUIController>();
                    yield return Wait(() => Get(ui,"activeCustomer") != null &&
                        ((Button)Get(ui,"ellipsisButton")).gameObject.activeInHierarchy,"interrupted encounter resumes");
                    guest = (CustomerController)Get(ui,"activeCustomer");
                    Require(guest.CampaignEncounter == encounter, "Mid-dialogue Continue skipped encounter.");
                    ClickField(ui,"ellipsisButton");
                    while ((bool)Get(ui,"isPreRequestDialogueActive")) ClickField(ui,"nextButton");
                }
                var recipe = ChooseAnswer(order,night,slot);
                bool weakHamlet = encounter != null && encounter.visit == CampaignVisitId.HamletQuestion;
                if (weakHamlet) recipe = Recipe("recipe_test_stir"); // Actual mismatched interpretation, no result injection.
                bool failPrince = SURLHWACampaignPlayModeValidation.QuietPrince && night == 2 && slot == 1;
                bool retry = night == 6 && order.RequiresSuccessfulDiscovery;
                for (int attempt = 0; attempt < (retry ? 2 : 1); attempt++)
                {
                    if (attempt > 0)
                    {
                        yield return Wait(() => ((Button)Get(ui,"brewButton")).gameObject.activeInHierarchy,"discovery retry");
                        Require(ReferenceEquals(Get(ui,"activeCustomer"),guest) && day.ServedCustomerCount == slot-1 &&
                            CampaignProgressPersistence.LoadSlot(night,count) == slot-1,"Failed discovery consumed its slot.");
                        Require(!Find<RecipeUnlockManager>().IsKnown(recipe), "Missing garnish wrongly taught Known.");
                    }
                    ClickField(ui,"brewButton");
                    if (order.RequestType == GuestRequestType.Recommendation)
                    {
                        ClickField(ui,recipe.method.ToString().ToLowerInvariant()+"Button");
                        ClickField(ui,"confirmButton");
                    }
                    yield return Wait(() => Find<IngredientShelfController>() != null &&
                        Find<BartendingFlowCoordinator>().IsBartendingActive,"bartending entry");
                    if (night == 1 && slot == 1) SURLHWAReviewCapture.Save("restart-normal-play-workspace",1280,720);
                    methods.Add(recipe.method);
                    yield return SURLHWAFourNightRunner.ExecuteDrink(recipe,failPrince,retry && attempt == 0);
                    yield return Wait(() => !Find<BartendingFlowCoordinator>().IsBartendingActive,"actual result and unload");
                }
                if (encounter != null || (night == 4 && slot == 4))
                {
                    yield return Wait(() => Get(ui,"farewellCompleted") != null,"departure reading");
                    Require(day.ServedCustomerCount == slot-1 && !day.IsDayEnded,"Visit completed before departure reading.");
                    if (encounter != null && encounter.IsSpecial)
                        Require(!CampaignProgressPersistence.IsCompleted(encounter.visit),"Reading entry completed ACT.");
                    if (weakHamlet)
                    {
                        var lines = (string[])Get(ui,"farewellLines");
                        Require(lines[0] == encounter.afterService.onFailLines[0],"Weak service used success dialogue.");
                    }
                    if (encounter != null && (encounter.sharedGlass || encounter.stillness))
                        SURLHWAReviewCapture.Save("campaign-"+encounter.name,1280,720);
                    yield return new WaitForSecondsRealtime(.1f);
                    while (Get(ui,"farewellCompleted") != null) ClickField(ui,"nextButton");
                }
                yield return Wait(() => guest == null,"natural departure");
                Require(day.ServedCustomerCount == slot,"Departure count differs.");
                if (encounter != null && encounter.IsSpecial)
                    Require(CampaignProgressPersistence.IsCompleted(encounter.visit),"Departure not persisted.");
                if (!failPrince) Require(Find<RecipeUnlockManager>().IsKnown(recipe),"Actual discovery not retained.");
                ValidateMemories(); departures++;
                Debug.Log($"[Full Campaign] night={night} slot={slot} drink={recipe.recipeId} departures={departures} PASS");
                if ((night == 6 || night == 10 || night == 15) && slot == 1)
                {
                    yield return ContinueFromTitle(night,slot); day = Find<BusinessDayManager>();
                    Require(Find<RecipeUnlockManager>().IsKnown(recipe),"Continue lost knowledge.");
                }
            }
            yield return Wait(() => day.IsDayEnded,"night result");
            if (night < 18)
            {
                Require(BusinessDayPersistence.LoadCurrentDay(1) == night+1,"Wrong next-night save.");
                var result = Find<DayResultPanelController>();
                Require(((TMP_Text)Get(result,"customerCountText")).text.EndsWith(count+"/"+count),"Wrong night total.");
                ClickField(result,"restartButton"); yield return null;
                yield return Wait(() => Find<BusinessDayManager>() != null && Find<BusinessDayManager>().CurrentDay == night+1,"next night");
                if (night == 2) yield return ContinueFromTitle(3,0);
            }
        }
        Require(LittlePrinceVisitPersistence.IsStatueEligible() == !SURLHWACampaignPlayModeValidation.QuietPrince, "Prince farewell differs from actual services.");
        Require(departures == 47 && methods.Count == 4,"Incomplete campaign route/method coverage.");
        Require(Find<CampaignBarMemories>().VisibleMemoryCount == 7,"Missing completed ACT memory.");
        // Earned through all 47 departures; briefly hide only UI for the display review.
        var reviewCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        var enabledCanvases = new System.Collections.Generic.List<Canvas>();
        foreach (var canvas in reviewCanvases)
            if (canvas.enabled) { enabledCanvases.Add(canvas); canvas.enabled = false; }
        SURLHWAReviewCapture.Save("restart-earned-memories",1280,720);
        foreach (var canvas in enabledCanvases) canvas.enabled = true;
        Require(!CampaignProgressPersistence.EndingRead,"Ending completed before reading.");
        var endingPanel = Find<DayResultPanelController>();
        ClickField(endingPanel,"restartButton"); ClickField(endingPanel,"restartButton");
        yield return ContinueFromTitle(18,2);
        endingPanel = Find<DayResultPanelController>();
        Require((int)Get(endingPanel,"endingLineIndex") == 0 && !CampaignProgressPersistence.EndingRead,
            "Interrupted ending did not replay.");
        SURLHWAReviewCapture.Save("campaign-ending",1280,720);
        for (int i=0;i<Find<BusinessDayManager>().Campaign.ending.onEnterLines.Length;i++) ClickField(endingPanel,"restartButton");
        Require(CampaignProgressPersistence.EndingRead,"Ending completion not persisted.");
        yield return ContinueFromTitle(18,2);
        Require(!Find<BusinessDayManager>().CanSpawnMoreCustomers() && Find<BusinessDayManager>().IsDayEnded,
            "Post-ending Continue started another night.");
        SURLHWAReviewCapture.Save("campaign-coda",1280,720);
        ClickField(Find<DayResultPanelController>(),"restartButton"); yield return null;
        yield return Wait(() => NamedButton("NewGameButton") != null,"completed title");
        Click(NamedButton("NewGameButton")); yield return null;
        Require(NamedButton("ConfirmNewGameButton") != null && CampaignProgressPersistence.HasProgress,
            "Completed progress must survive until New Game confirmation.");
        Click(NamedButton("ConfirmNewGameButton")); yield return null;
        yield return Wait(() => Find<GameManager>() != null && Find<GameManager>().IsGameplaySessionStarted,"reset gameplay");
        ValidateMemories();
        Require(!CampaignProgressPersistence.HasProgress && Find<CampaignBarMemories>().VisibleMemoryCount == 0 &&
            !Find<RecipeUnlockManager>().IsKnown(Recipe("recipe_last_word")),"New Game failed to reset campaign.");
        // Compatibility fixture only: a completed old demo never invents successes/knowledge.
        SurlhwaGameProgress.ResetGameProgress(); BusinessDayPersistence.SaveCurrentDay(4); DemoCompletionPersistence.MarkDemoComplete();
        yield return ContinueFromTitle(5,0);
        Require(!LittlePrinceVisitPersistence.IsStatueEligible() &&
            !Find<RecipeUnlockManager>().IsKnown(Recipe("recipe_hanky_panky")),"Legacy conversion invented history.");
        Debug.Log("[Full Campaign] PASS: New Game through 47 natural departures and ending; 16 new special visits, ordinary voices, actual discovery/retry/alternatives, weak-service ACT continuation, checkpoint/mid-dialogue/ending Continue, seven memories, reset and legacy handoff. Automated input seams; physical input and subjective feel remain human review.");
        Finish(true);
    }
    private void Finish(bool success)
    {
        if (finished) return;
        finished = true;
        Application.runInBackground = previousBackground;
        SURLHWACampaignPlayModeValidation.Complete(success);
    }
}
#endif
