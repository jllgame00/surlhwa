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
public static class SURLHWAFourNightPlayModeValidation
{
    private const string Prefix = "SURLHWA_J8_PROBE_";
    private static readonly string[] ExtraKeys = { "currentDay", "SURLHWA_DEMO_COMPLETE_V1", "SURLHWA_FIRST_RECIPE_REVEAL_SHOWN" };
    static SURLHWAFourNightPlayModeValidation()
    {
        EditorApplication.playModeStateChanged -= Changed;
        EditorApplication.playModeStateChanged += Changed;
    }
    [MenuItem("SURLHWA/Validation/Run Four Night Production Play Mode")]
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
    }
    private static void Changed(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Prefix + "Pending", false))
            EditorApplication.delayCall += () => new GameObject("Four Night Runtime Validation").AddComponent<SURLHWAFourNightRunner>();
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


internal sealed class SURLHWAFourNightRunner : MonoBehaviour
{
    private float began;
    private bool finished;
    private bool previousBackground;
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
    private IEnumerator Guarded()
    {
        while (work.Count > 0 && !finished)
        {
            object current = null;
            try
            {
                if (Time.realtimeSinceStartup - began > 900f)
                    throw new InvalidOperationException("Four-night validation timed out.");
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
    private IEnumerator Run()
    {
        yield return null;
        for (int scenario = 0; scenario < 2; scenario++)
        {
            Require(NamedButton("NewGameButton") != null, "Missing production entry.");
            Click(NamedButton("NewGameButton"));
            yield return null;
            yield return Wait(() => Find<GameManager>() != null && Find<GameManager>().IsGameplaySessionStarted,
                "New Game scene reload");
            Require(!Find<RecipeUnlockManager>().IsKnown(Recipe("recipe_dry_martini")),
                "New Game must not pre-grant discovery.");
            for (int night = 1; night <= 4; night++)
            {
                var day = Find<BusinessDayManager>();
                Require(day.CurrentDay == night, "Wrong persisted night.");
                int count = night == 1 ? 5 : 4;
                for (int slot = 1; slot <= count; slot++)
                {
                    var ui = Find<InteractionUIController>();
                    yield return Wait(() => Get(ui, "activeCustomer") != null &&
                        ((Button)Get(ui, "ellipsisButton")).gameObject.activeInHierarchy, "guest arrival");
                    var guest = (CustomerController)Get(ui, "activeCustomer");
                    Require(guest.TryGetDedicatedRequest(out var order), "Production slot has no authored request.");
                    ClickField(ui, "ellipsisButton");
                    while ((bool)Get(ui, "isPreRequestDialogueActive")) ClickField(ui, "nextButton");
                    if ((bool)Get(ui, "isLittlePrinceChoiceActive"))
                        ClickField(ui, scenario == 0 ? "nextButton" : "brewButton");

                    CocktailRecipeData recipe;
                    if (order.RequestType == GuestRequestType.ExactCocktail)
                    {
                        recipe = Find<RecipeUnlockManager>().GetAllRecipes().Find(r => r.targetCocktail == order.requestedCocktail);
                        Require(recipe != null, "No exact recipe.");
                    }
                    else
                    {
                        string id = order.DesiredEmotionTags[0] switch
                        {
                            EmotionTag.Warmth => night >= 3 && slot > 1 && scenario == 1 ? "recipe_manhattan" : "recipe_test_stir",
                            EmotionTag.Bitterness => night >= 3 && scenario == 1 ? "recipe_manhattan" : "recipe_negroni",
                            EmotionTag.Awakening => "recipe_gin_fizz",
                            _ => night == 1 ? "recipe_dry_martini" :
                                slot == 4 ? (scenario == 0 ? "recipe_dry_martini" : "recipe_manwol") :
                                scenario == 0 ? "recipe_manwol" : "recipe_gin_fizz"
                        };
                        recipe = Recipe(id);
                        Require(Find<RecipeUnlockManager>().IsKnown(recipe),
                            "Recommendation chosen answer is not learned in actual play: " + id);
                    }
                    if (night == 1 && slot <= 3) recipe = Recipe("recipe_test_stir");
                    bool failPrince = scenario == 1 && night == 2 && slot == 1;
                    bool retry = scenario == 1 && night == 2 && slot == 2;
                    for (int attempt = 0; attempt < (retry ? 2 : 1); attempt++)
                    {
                        if (attempt > 0)
                        {
                            yield return Wait(() => ((Button)Get(ui, "brewButton")).gameObject.activeInHierarchy,
                                "required discovery retry");
                            Require(ReferenceEquals(Get(ui, "activeCustomer"), guest) && day.ServedCustomerCount == slot - 1,
                                "Failed required practice consumed the guest or slot.");
                        }
                        ClickField(ui, "brewButton");
                        if (order.RequestType == GuestRequestType.Recommendation)
                        {
                            ClickField(ui, recipe.method.ToString().ToLowerInvariant() + "Button");
                            ClickField(ui, "confirmButton");
                        }
                        yield return Wait(() => Find<IngredientShelfController>() != null &&
                            Find<BartendingFlowCoordinator>().IsBartendingActive, "additive bartending entry");
                        yield return ExecuteDrink(recipe, failPrince, retry && attempt == 0);
                        yield return Wait(() => !Find<BartendingFlowCoordinator>().IsBartendingActive,
                            "actual result and additive unload");
                    }
                    if (night == 4 && slot == 4)
                    {
                        yield return Wait(() => Get(ui, "farewellCompleted") != null, "readable farewell");
                        Require(!day.IsDayEnded, "Final day ended before farewell reading.");
                        while (Get(ui, "farewellCompleted") != null) ClickField(ui, "nextButton");
                    }
                    yield return Wait(() => guest == null, "feedback and natural departure");
                    Require(day.ServedCustomerCount == slot, "Departure count mismatch.");
                    if (!failPrince)
                        Require(Find<RecipeUnlockManager>().IsKnown(recipe), "Actual service did not retain recipe knowledge.");
                    if (night == 2 && slot == 1)
                        Require(LittlePrinceVisitPersistence.GetServiceOutcome(1) == (failPrince ? 2 : 1),
                            "First-visit outcome ignored actual technique.");
                    Debug.Log($"[Four Night] scenario={scenario} night={night} slot={slot} drink={recipe.recipeId} PASS");
                }
                yield return Wait(() => day.IsDayEnded, "night completion");
                var result = Find<DayResultPanelController>();
                Require(((TMP_Text)Get(result, "customerCountText")).text.EndsWith(count + "/" + count),
                    "Night summary used the next night's guest target.");
                Require(BusinessDayPersistence.LoadCurrentDay(1) == (night == 4 ? 4 : night + 1),
                    "Wrong save boundary or unintended Day 5.");
                ClickField(result, "restartButton");
                yield return null;
                if (night < 4)
                {
                    yield return Wait(() => Find<BusinessDayManager>() != null &&
                        Find<BusinessDayManager>().CurrentDay == night + 1, "next night reload");
                    if (night == 2)
                    {
                        // Real entry/Continue reload, without granting any facts.
                        GameManager.ReturnToDemoEntry();
                        yield return null;
                        yield return Wait(() => NamedButton("ContinueButton") != null, "Continue entry");
                        Require(LittlePrinceVisitPersistence.IsFirstVisitCompleted(), "Visit lost on reload.");
                        Click(NamedButton("ContinueButton"));
                        yield return null;
                        yield return Wait(() => Find<GameManager>() != null && Find<GameManager>().IsGameplaySessionStarted,
                            "Continue gameplay");
                        Require(Find<RecipeUnlockManager>().IsKnown(Recipe("recipe_negroni")) &&
                            Find<RecipeUnlockManager>().IsKnown(Recipe("recipe_manwol")),
                            "Required knowledge lost on Continue.");
                    }
                }
                else
                {
                    yield return Wait(() => NamedButton("ContinueButton") != null, "completed entry");
                    Require(DemoCompletionPersistence.IsDemoComplete() &&
                        LittlePrinceVisitPersistence.IsStatueEligible() == (scenario == 0),
                        "Endpoint/history disagrees with three actual services.");
                    Click(NamedButton("ContinueButton"));
                    yield return null;
                    yield return Wait(() => Find<BusinessDayManager>() != null && Find<BusinessDayManager>().IsDayEnded,
                        "completed Continue endpoint");
                    Require(!Find<BusinessDayManager>().CanSpawnMoreCustomers(), "Completed Continue spawned Day 5.");
                    ClickField(Find<DayResultPanelController>(), "restartButton");
                    yield return null;
                    yield return Wait(() => NamedButton("NewGameButton") != null, "return after completed Continue");
                }
            }
        }
        Require(methods.Count == 4, "All four methods must execute in production context.");
        Debug.Log("[Four Night] PASS: two complete New Game -> 5/4/4/4 -> Demo Complete routes; " +
            "actual containers/technique/preparation/recognition, discovery/retry, A/B history, cross-method alternatives, " +
            "night reload/Continue and no Day 5. Input seams are automated; physical mouse/keyboard and subjective feel remain human checks.");
        Finish(true);
    }
    private IEnumerator ExecuteDrink(CocktailRecipeData recipe, bool failTechnique, bool omitRequiredGarnish)
    {
        var shelf = Find<IngredientShelfController>();
        var stir = Find<StirMiniGame>();
        var build = Find<BuildMiniGame>();
        var shake = Find<ShakeMiniGame>();
        var floating = Find<FloatMiniGame>();
        methods.Add(recipe.method);
        Require(shelf != null, "No actual shelf.");
        // This driver reads an authored plan only after the same facts are visible
        // in consumed teaching or Known Guide knowledge. It never writes a result,
        // trace, known flag, technique grade, guest state or visit/day outcome.
        var facts = Find<RecipeUnlockManager>().GetKnownFacts(recipe);
        Require(facts.State == RecipeKnowledgeState.Known || facts.PreparationKnown,
            "Attempt requires player-visible preparation knowledge: " + recipe.recipeId);
        foreach (var step in recipe.expectedPreparation)
        {
            if (step == null || step.optional || (omitRequiredGarnish && step.action == RecipePreparationAction.Garnish)) continue;
            if (step.ingredient != null)
                Require(shelf.IsIngredientVisible(step.ingredient) && shelf.TrySelectIngredient(step.ingredient),
                    "Required ingredient not accessible on the real shelf.");
            switch (step.action)
            {
                case RecipePreparationAction.Pour:
                    float ml = 0f;
                    foreach (var item in recipe.ingredients)
                        if (item.ingredient == step.ingredient) ml = BartendingVolume.OuncesToMilliliters(item.ratio);
                    Require(ml > 0, "No authored quantity.");
                    if (floating != null)
                    {
                        var intensity = failTechnique ? FloatMiniGame.FloatPourIntensity.Aggressive : FloatMiniGame.FloatPourIntensity.Gentle;
                        int index = floating.LayeredDrinkState.Layers.Count;
                        Require(floating.BeginLayerPour(intensity), "Layer could not begin.");
                        float rate = index == 0 ? 30f : failTechnique ? 60f : 15f;
                        Require(Mathf.Abs(floating.PourActiveLayerForDuration(ml / rate, intensity) - ml) < .01f, "Layer transfer mismatch.");
                        Require(floating.EndActiveLayerPour(), "Layer did not complete.");
                    }
                    else
                    {
                        Require(shelf.BeginSelectedPour(), "Pour could not begin.");
                        float remaining = ml;
                        while (remaining > .001f)
                        {
                            float amount = Mathf.Min(1f, remaining);
                            Require(Mathf.Abs(shelf.PourSelectedIngredient(amount) - amount) < .001f, "Actual transfer rejected.");
                            remaining -= amount;
                        }
                        shelf.EndSelectedPour();
                    }
                    break;
                case RecipePreparationAction.Dash:
                    for (int i = 0; i < step.repeatCount; i++)
                        Require(shelf.TryDashSelectedIngredient(out _), "Dash rejected.");
                    break;
                case RecipePreparationAction.AddIce:
                    Require(stir != null ? stir.AddIceToMixingGlass() : shake != null ? shake.AddIceToShaker() :
                        build != null ? build.AddIceToServingGlass() : shelf.TryAddIceToActiveTarget(), "Ice rejected.");
                    break;
                case RecipePreparationAction.Stir:
                    ClickField(stir, "startButton");
                    for (int i = 0; i <= 360; i++) stir.TryRecordStirAngle(i * 3f, 1f / 60f);
                    ClickField(stir, "startButton");
                    break;
                case RecipePreparationAction.GentleStir:
                    build.BeginGentleStir();
                    for (int i = 0; i <= 180; i++) build.TryRecordGentleStirAngle(i * 3f, 1f / 60f);
                    build.FinishGentleStir();
                    break;
                case RecipePreparationAction.Shake:
                    ClickField(shake, "startButton");
                    for (int i = 0; i < shake.NoteCount; i++)
                    {
                        while (Time.unscaledTime < shake.GetExpectedNoteTime(i)) yield return null;
                        Require(shake.TryJudgeShakeInput(i % 2 == 0 ? ShakeMiniGame.ShakeDirection.Left :
                            ShakeMiniGame.ShakeDirection.Right, Time.unscaledTime) != ShakeMiniGame.ShakeJudgement.Miss,
                            "Real-time Shake input missed.");
                    }
                    break;
                case RecipePreparationAction.Strain:
                    ClickField(stir != null ? (object)stir : shake, "startButton");
                    break;
                case RecipePreparationAction.PourFromContainer:
                    shake.PourFromContainerAndContinue();
                    break;
                case RecipePreparationAction.Splash:
                    Require(shake.TryAddSplashToServingGlass(), "Splash rejected.");
                    break;
                case RecipePreparationAction.Garnish:
                    if (build != null) ClickField(build, "startButton");
                    var garnish = step.acceptedGarnishes[0];
                    Require(stir != null ? stir.TryApplyGarnish(garnish.garnish, garnish.placement) :
                        build != null ? build.TryApplyGarnish(garnish.garnish, garnish.placement) :
                        shake != null ? shake.TryApplyGarnish(garnish.garnish, garnish.placement) :
                        floating.TryApplyGarnish(garnish.garnish, garnish.placement), "Garnish rejected.");
                    break;
                default: throw new InvalidOperationException("Unsupported production step " + step.action);
            }
        }
        if (build != null && Get(build, "state").ToString() == "Preparing") ClickField(build, "startButton");
        if (floating != null) floating.CompleteFloat();
        if (shake != null) shake.ServeShake();
        else if (stir != null) ClickField(stir, "startButton");
        else if (build != null) ClickField(build, "startButton");
        else floating.ServeFloat();
    }
    private void Finish(bool success)
    {
        if (finished) return;
        finished = true;
        Application.runInBackground = previousBackground;
        SURLHWAFourNightPlayModeValidation.Complete(success);
    }
}
#endif
