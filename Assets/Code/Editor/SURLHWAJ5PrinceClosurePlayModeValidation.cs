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
            EditorApplication.delayCall += () => new GameObject("J5 Runtime Validation").AddComponent<SURLHWAJ5PrinceClosureRunner>();
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
        EditorApplication.Exit(code);
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
        Application.runInBackground = true;
        StartCoroutine(Guarded());
    }
    private void Update()
    {
        if (!finished && Time.realtimeSinceStartup - began > 180f)
        {
            Debug.LogError("[J5 PlayMode] Timed out.");
            Finish(false);
        }
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
            ClearVisits();
            DemoCompletionPersistence.ClearDemoCompletion();
            var choice = (LittlePrinceNarrativeChoice)(scenario % 3);
            if (choice != LittlePrinceNarrativeChoice.None) LittlePrinceNarrativePersistence.TryCommitChoice(choice);
            for (int visit = 1; visit <= 3; visit++)
            {
                Set(day, "currentDay", visit + 1);
                Set(day, "servedCustomerCount", visit == 3 ? 3 : 0);
                Set(day, "dayEnded", false);
                Set(spawner, "spawnedCount", visit == 3 ? 4 : 1);
                customer = Instantiate(prefab).GetComponent<CustomerController>();
                customer.enabled = false; // Keep automatic movement out of this focused service/reading probe.
                customer.SetLoopManager(spawner);
                OrderData order = visit == 1 ? first : visit == 2 ? follow : closing;
                if (visit == 1) customer.ConfigureLittlePrinceFirstVisitNarrative();
                else if (visit == 2) customer.ConfigureLittlePrinceFollowupEncounter(order);
                else customer.ConfigureLittlePrinceClosingEncounter(order);
                var stateField = typeof(CustomerController).GetField("state", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                stateField.SetValue(customer, Enum.Parse(stateField.FieldType, "Waiting"));
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
                SURLHWAJ5PrinceClosureValidation.Invoke(orders, "OnMiniGameDone", result);
                bool successful = !(scenario == visit || (visit == 3 && scenario >= 4));
                Require(LittlePrinceVisitPersistence.GetServiceOutcome(visit) == (successful ? 1 : 2),
                    "Actual OrderManager service did not persist FIT + technique + preparation truth.");
                Require(Get(orders, "_currentCustomer") == null, "Failed narrative service must resolve rather than retain a retry order.");
                if (visit == 3)
                {
                    float deadline = Time.realtimeSinceStartup + 12f;
                    while (Get(interaction, "farewellCompleted") == null && Time.realtimeSinceStartup < deadline) yield return null;
                    Require(Get(interaction, "farewellCompleted") != null, "Final farewell did not appear after feedback.");
                    var toast = FindFirstObjectByType<ResultToastController>();
                    Require(((CanvasGroup)Get(toast, "canvasGroup")).alpha <= .001f, "Farewell must wait for diagnostics to close.");
                    var popup = FindFirstObjectByType<RecipeUnlockPopupController>();
                    Require(!((RectTransform)Get(popup, "popupRoot")).gameObject.activeInHierarchy,
                        "Farewell must wait for a newly discovered recipe popup to close.");
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
                Require(Get(customer, "state").ToString() == "Leaving", "Service/reading must finish with departure, even on failure.");
                SURLHWAJ5PrinceClosureValidation.Invoke(customer, "LeaveAndDestroy");
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
        SurlhwaGameProgress.ResetGameProgress();
        Require(!LittlePrinceVisitPersistence.HasSavedClosureState() && !LittlePrinceVisitPersistence.IsStatueEligible(), "New Game failed to reset J5 history.");
        Debug.Log("[J5 PlayMode] PASS: production service handoff, all three failure positions, preparation/fit negatives, read-to-advance farewell, departure persistence, final-night completion and New Game reset. This is not normal-input bartending validation.");
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
    private void Finish(bool success)
    {
        if (finished) return;
        finished = true;
        Application.runInBackground = previousBackground;
        if (customer != null) Destroy(customer.gameObject);
        spawner?.StopAllCoroutines();
        SURLHWAJ5PrinceClosurePlayModeValidation.Complete(success);
    }
}
#endif
