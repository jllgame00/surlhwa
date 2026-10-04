#if UNITY_EDITOR || SURLHWA_RC_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Explicit RC instrumentation only. Drives enabled production Update handlers;
/// owns no liquid, recipe, customer, knowledge, or progression state.
/// </summary>
public sealed class SurlhwaRCGameplayQA : MonoBehaviour
{
    public sealed class InputSource : SurlhwaGameplayInput.ISource
    {
        readonly HashSet<KeyCode> held = new();
        readonly Dictionary<KeyCode, int> down = new(), up = new();
        public Vector3 MousePosition { get; set; }
        public int DownObservations;
        public string LastObservation = "none";
        public bool GetKey(KeyCode key) => held.Contains(key);
        public bool GetKeyDown(KeyCode key)
        {
            bool value = down.TryGetValue(key, out int frame) && frame == Time.frameCount;
            if (value) { DownObservations++; LastObservation = key + " down frame=" + frame; }
            return value;
        }
        public bool GetKeyUp(KeyCode key) => up.TryGetValue(key, out int frame) && frame == Time.frameCount;
        public void Set(params KeyCode[] keys)
        {
            var next = new HashSet<KeyCode>(keys);
            foreach (var key in held) if (!next.Contains(key)) up[key] = Time.frameCount + 1;
            foreach (var key in next) if (!held.Contains(key)) down[key] = Time.frameCount + 1;
            held.Clear(); held.UnionWith(next);
        }
    }
    [Serializable] public sealed class CaseResult
    {
        public string scenario, status, evidence, detail;
        public int inputCases, submissions;
    }
    [Serializable] sealed class Report
    {
        public string utc, unity, boundary = "SIMULATED_PRODUCTION_INPUT; UI PRODUCTION_HANDLER (Submit), not native delivery";
        public List<CaseResult> scenarios = new();
    }
    static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    public static readonly int[] Seeds = { 92, 49524, 20261004 };
    // The two retained legacy catalog members have no authored campaign route.
    // Data audit reports their missing metadata explicitly as reserve debt.
    public static bool IsCampaignRecipe(CocktailRecipeData recipe) => recipe != null &&
        recipe.recipeId != "recipe_janseol" && recipe.recipeId != "recipe_test_shake";
    readonly Report report = new();
    readonly InputSource input = new();
    readonly List<string> trace = new();
    readonly HashSet<string> captures = new();
    public Action<string, int, int> Capture;
    IDisposable inputLease;
    CaseResult current;
    float oldCapture;
    int oldRate;
    string lastTransition = "start";
    string lastState;
    string reportName = "focused";
    MiniGameResult result;
    int completions;
    const string Folder = "Logs/Review/RCAutomatedQA";
    public static T Find<T>() where T : UnityEngine.Object => FindFirstObjectByType<T>();
    public static object Get(object owner, string name)
    {
        if (owner == null) return null;
        var field = owner.GetType().GetField(name, Fields);
        return field?.GetValue(owner);
    }
    public static Button FieldButton(object owner, string field) => Get(owner, field) as Button;
    public static Button Named(string name) => FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == name);
    public static MonoBehaviour Method()
    {
        return (MonoBehaviour)Find<StirMiniGame>() ?? (MonoBehaviour)Find<ShakeMiniGame>() ??
            (MonoBehaviour)Find<BuildMiniGame>() ?? Find<FloatMiniGame>();
    }
    public static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    void Record(string message)
    {
        if (trace.Count < 20000) trace.Add(Time.frameCount + " " + message);
    }
    void Update()
    {
        var method = Method();
        string state = method != null ? method.GetType().Name + "/" + Get(method, "state") : "bar";
        if (state != lastState) { lastTransition = lastState + " -> " + state + " @" + Time.frameCount; Record(lastTransition); lastState = state; }
    }
    IEnumerator Start()
    {
        Require(Application.companyName == "SURLHWA_RC_AUTOMATED_QA", "Use an isolated save namespace for RC fixtures.");
        DontDestroyOnLoad(gameObject);
        Directory.CreateDirectory(Folder);
        Directory.CreateDirectory(Folder + "/screens");
        report.utc = DateTime.UtcNow.ToString("O"); report.unity = Application.unityVersion;
        oldCapture = Time.captureDeltaTime; oldRate = Application.targetFrameRate;
        Time.captureDeltaTime = 1f / 60f; Application.targetFrameRate = 120;
        Application.runInBackground = true;
        inputLease = SurlhwaGameplayInput.UseForValidation(input);
        yield return null;
#if UNITY_EDITOR
        bool stressOnly = UnityEditor.SessionState.GetString("SURLHWA_RC_QA_Command", "") == "stress";
#else
        bool stressOnly = Environment.GetCommandLineArgs().Contains("--surlhwa-rc-stress");
#endif
        if (stressOnly)
        {
            reportName = "stress";
            foreach (string id in new[] { "recipe_dry_martini", "recipe_gin_fizz", "recipe_negroni", "recipe_manwol" })
            {
                yield return Case("reload-active-measure/" + id, ReloadMeasured(id));
                yield return Reload();
            }
        }
        else
        {
        foreach (string recipe in new[] { "recipe_dry_martini", "recipe_manhattan", "recipe_negroni", "recipe_gin_fizz" })
        {
            yield return Case("measurement-shortcuts/" + recipe, Measurement(recipe));
            yield return Reload();
        }
#if UNITY_EDITOR
        bool onlyMeasurement = UnityEditor.SessionState.GetString("SURLHWA_RC_QA_Command", "") == "measurement";
#else
        bool onlyMeasurement = false;
#endif
        if (!onlyMeasurement)
        {
            yield return Case("disabled-settings-owner", DisabledSettings());
            yield return Reload();
            yield return Case("held-key-modal-reentry", HeldModal());
            yield return Reload();
            var ids = Find<RecipeUnlockManager>().GetAllRecipes().Where(r => IsCampaignRecipe(r) && r.discoverable)
                .Select(r => r.recipeId).Distinct().OrderBy(id => id).ToArray();
            foreach (string id in ids)
            {
                yield return Case("recipe-input/" + id, RecipeCase(id));
                yield return Reload();
            }
            foreach (int seed in Seeds)
                foreach (string id in new[] { "recipe_dry_martini", "recipe_gin_fizz", "recipe_negroni", "recipe_manwol" })
                {
                    yield return Case("fuzz/" + seed + "/" + id, Fuzz(id, seed));
                    yield return Reload();
                }
        }
        }
        SaveEvidence();
        bool passed = report.scenarios.All(c => c.status == "PASS");
        Debug.Log("[RC QA] " + (passed ? "PASS" : "FAIL") + " scenarios=" + report.scenarios.Count +
            " cases=" + report.scenarios.Sum(c => c.inputCases) + " submissions=" + report.scenarios.Sum(c => c.submissions));
        Cleanup();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#else
        Application.Quit(passed ? 0 : 1);
#endif
    }
    IEnumerator Case(string name, IEnumerator body)
    {
        current = new CaseResult { scenario = name, status = "RUNNING", evidence = "SIMULATED_PRODUCTION_INPUT" };
        report.scenarios.Add(current); Record("CASE " + name);
        var stack = new Stack<IEnumerator>(); stack.Push(body);
        bool failed = false;
        while (stack.Count > 0 && !failed)
        {
            object wait = null;
            try
            {
                if (!stack.Peek().MoveNext()) { stack.Pop().DisposeIfNeeded(); continue; }
                wait = stack.Peek().Current;
                if (wait is IEnumerator nested) { stack.Push(nested); continue; }
            }
            catch (Exception e)
            {
                failed = true; current.status = "FAIL"; current.detail = e.ToString();
                File.WriteAllText(Folder + "/softlock-" + name.Replace('/', '-') + ".txt", Snapshot(e.Message));
                Debug.LogError("[RC QA] " + name + " FAIL: " + e.Message);
            }
            if (!failed) yield return wait;
        }
        while (stack.Count > 0) stack.Pop().DisposeIfNeeded();
        input.Set();
        if (!failed) { current.status = "PASS"; Debug.Log("[RC QA] " + name + " PASS"); }
        SaveEvidence();
    }
    void SaveEvidence()
    {
        File.WriteAllText(Folder + "/" + reportName + "-results.json", JsonUtility.ToJson(report, true));
        File.WriteAllLines(Folder + "/" + reportName + "-input-callback-trace.txt", trace);
    }
    IEnumerator Reload()
    {
        input.Set();
        var previous = Find<GameManager>();
        GameManager.ReturnToDemoEntry();
        yield return null;
        float until = Time.realtimeSinceStartup + 20;
        while (Find<GameManager>() == null || Find<GameManager>() == previous)
        { Require(Time.realtimeSinceStartup < until, "RC cleanup reload did not finish."); yield return null; }
        yield return null;
        Require(!Find<BartendingFlowCoordinator>().IsBartendingActive && !SurlhwaModalSurface.IsPaused, "Reload retained route/modal ownership.");
    }
    IEnumerator Wait(Func<bool> condition, string label, float seconds = 12)
    {
        float until = Time.realtimeSinceStartup + seconds;
        while (!condition())
        {
            Require(Time.realtimeSinceStartup < until, "SOFT_LOCK: " + label + "; " + input.LastObservation);
            yield return null;
        }
    }
    IEnumerator Pulse(KeyCode key)
    {
        current.inputCases++;
        Record("input.down " + key + " state=" + lastState);
        input.Set(key); yield return null;
        input.Set(); yield return null;
        Record("input.end " + key + " observed=" + input.LastObservation + " state=" + lastState);
    }
    IEnumerator Submit(Button button, Action afterDispatch = null)
    {
        Require(button != null && button.IsActive() && button.IsInteractable(), "Unreachable production UI submit: " + (button != null ? button.name : "missing"));
        int callbacks = 0;
        UnityEngine.Events.UnityAction observe = () => callbacks++;
        button.onClick.AddListener(observe);
        EventSystem.current.SetSelectedGameObject(button.gameObject);
        ExecuteEvents.Execute(button.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        Record("PRODUCTION_HANDLER submit " + button.name + " callbacks=" + callbacks);
        if (button != null) button.onClick.RemoveListener(observe);
        Require(callbacks == 1, "Submit callback must fire once.");
        current.submissions++;
        afterDispatch?.Invoke();
        yield return null;
    }
    CocktailRecipeData Recipe(string id) => Find<RecipeUnlockManager>().GetAllRecipes().FirstOrDefault(r => r != null && r.recipeId == id)
        ?? throw new InvalidOperationException("Missing catalog recipe: " + id);
    IEnumerator Open(CocktailRecipeData recipe)
    {
        // Explicit route fixture. Campaign runner separately proves learning and customer entry.
        var entry = Get(Find<GameManager>(), "entryPanel") as GameObject;
        if (entry != null) entry.SetActive(false);
        var knowledge = Find<RecipeUnlockManager>(); knowledge.MarkKnown(recipe);
        completions = 0;
        Require(Find<BartendingFlowCoordinator>().BeginBartending(new BartendingSession(
            new BartendingRequestContext(recipe.method, "RC route fixture", recipe, 18),
            new RecipeGuideContext(recipe.method, new[] { recipe }, knowledge.GetKnownFacts, false)),
            value => { completions++; result = value; Record("completion=" + completions); },
            failure => { throw new InvalidOperationException("Route failed: " + failure); }), "Route start rejected.");
        yield return Wait(() => Find<IngredientShelfController>() != null, "route loading");
        yield return null;
        Require(Method().enabled, "Production Update must remain enabled.");
        Require(FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Count(e => e.isActiveAndEnabled) == 1, "Duplicate EventSystems.");
        Require(FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(a => a.isActiveAndEnabled) == 1, "Audio listener continuity.");
        Require(FindObjectsByType<SurlhwaSoundscape>(FindObjectsSortMode.None).Length == 1, "Duplicate soundscape owners.");
        CaptureScreen("route-" + recipe.method);
    }
    IEnumerator Select(IngredientData ingredient)
    {
        var option = FindObjectsByType<BrewIngredientOptionView>(FindObjectsSortMode.None).FirstOrDefault(o => o.Ingredient == ingredient);
        Require(option != null, "Ingredient is not supplied on live shelf: " + ingredient.ingredientId);
        yield return Submit(FieldButton(option, "selectButton"));
        Require(Find<IngredientShelfController>().SelectedIngredient == ingredient, "Ingredient selection failed.");
    }
    IEnumerator Measure(float ml)
    {
        var shelf = Find<IngredientShelfController>();
        if (!shelf.Measuring) yield return Pulse(KeyCode.M);
        Require(shelf.Measuring, "M did not reach production measurement handler.");
        input.Set(KeyCode.Space);
        yield return Wait(() => shelf.Jigger.CurrentLiquidVolumeMl >= ml - .01f, "Space -> Jigger fill");
        if (ml >= shelf.Jigger.CapacityMl)
        {
            for (int frame = 0; frame < 10; frame++) yield return null;
            Require(shelf.Jigger.CurrentLiquidVolumeMl == shelf.Jigger.CapacityMl, "Overflow must clamp the finite Jigger.");
        }
        input.Set(); yield return null;
        yield return Wait(() => shelf.PhysicalPresentation.IsResting, "bottle return");
        float measured = shelf.Jigger.CurrentLiquidVolumeMl, before = shelf.TargetContainer.CurrentLiquidVolumeMl;
        yield return Pulse(KeyCode.T);
        yield return Wait(() => !shelf.PhysicalTransferActive, "measured transfer");
        Require(!shelf.HasPendingMeasurement && Mathf.Abs(shelf.TargetContainer.CurrentLiquidVolumeMl - before - measured) < .02f,
            "Measured transfer must conserve actual liquid.");
        Require(shelf.Measuring, "Transfer unexpectedly changed M mode.");
        yield return Pulse(KeyCode.T);
        Require(Mathf.Abs(shelf.TargetContainer.CurrentLiquidVolumeMl - before - measured) < .02f, "Empty/repeated T duplicated transfer.");
    }
    IEnumerator Measurement(string id)
    {
        var recipe = Recipe(id); yield return Open(recipe);
        var shelf = Find<IngredientShelfController>();
        yield return Pulse(KeyCode.M); yield return Pulse(KeyCode.T);
        Require(shelf.Measuring && shelf.TargetContainer.CurrentLiquidVolumeMl == 0 && !shelf.PhysicalTransferActive, "Empty transfer altered state.");
        yield return Pulse(KeyCode.M); Require(!shelf.Measuring, "M toggle off failed.");
        yield return Select(recipe.ingredients[0].ingredient);
        yield return Measure(5);
        yield return Pulse(KeyCode.I);
        yield return Wait(() => !shelf.PhysicalTransferActive, "ice placement");
        Require(shelf.TargetContainer.HasIce, "M -> measure -> T -> I was swallowed.");
        int ice = shelf.PreparationTrace.Actions.Count(a => a.Action == RecipePreparationAction.AddIce);
        yield return Pulse(KeyCode.I); yield return Pulse(KeyCode.I);
        Require(shelf.PreparationTrace.Actions.Count(a => a.Action == RecipePreparationAction.AddIce) == ice && ice == 1, "Repeated I duplicated ice.");
        if (recipe.method == MiniGameType.Build)
        {
            yield return Pulse(KeyCode.J);
            Require(Get(Method(), "state").ToString() == "GentleStirring", "M -> T -> J was swallowed.");
        }
        else if (id == "recipe_manhattan")
        {
            var dash = recipe.expectedPreparation.First(s => s.action == RecipePreparationAction.Dash);
            yield return Select(dash.ingredient);
            yield return Pulse(KeyCode.D);
            yield return Wait(() => !shelf.PhysicalTransferActive, "dash placement");
            Require(shelf.TargetContainer.GetDashCount(dash.ingredient) == 1, "M -> T -> D was swallowed.");
        }
        else
        {
            yield return Submit(FieldButton(Method(), "startButton"));
            Require(Get(Method(), "state").ToString() == (recipe.method == MiniGameType.Stir ? "Stirring" : "Rhythm"),
                "Technique unavailable after measured transfer and ice.");
        }
    }
    IEnumerator RecipeCase(string id)
    {
        var recipe = Recipe(id); yield return Open(recipe);
        var shelf = Find<IngredientShelfController>();
        Require(recipe.expectedPreparation != null && recipe.expectedPreparation.Length > 0, "Missing preparation metadata: " + id);
        foreach (var step in recipe.expectedPreparation)
        {
            if (step == null || step.optional) continue;
            if (step.ingredient != null) yield return Select(step.ingredient);
            switch (step.action)
            {
                case RecipePreparationAction.Pour:
                case RecipePreparationAction.TopUp:
                    float ml = BartendingVolume.OuncesToMilliliters(recipe.ingredients.First(i => i.ingredient == step.ingredient).ratio);
                    if (recipe.method == MiniGameType.Stir)
                    {
                        while (ml > .1f) { float dose = Mathf.Min(60, ml); yield return Measure(dose); ml -= dose; }
                    }
                    else
                    {
                        float before = shelf.TargetContainer.GetLiquidAmountMl(step.ingredient);
                        input.Set(recipe.method == MiniGameType.Float ? new[] { KeyCode.Space, KeyCode.LeftShift } : new[] { KeyCode.Space });
                        yield return Wait(() => shelf.TargetContainer.GetLiquidAmountMl(step.ingredient) >= before + ml - .01f, "pour " + step.ingredient.ingredientId, 20);
                        input.Set(); yield return null;
                        yield return Wait(() => shelf.PhysicalPresentation.IsResting, "pour return");
                    }
                    break;
                case RecipePreparationAction.AddIce:
                    yield return Pulse(KeyCode.I);
                    yield return Wait(() => !shelf.PhysicalTransferActive, "ice");
                    Require(shelf.TargetContainer.HasIce, "Ice input rejected."); break;
                case RecipePreparationAction.Dash:
                    for (int n = 0; n < step.repeatCount; n++)
                    {
                        int before = shelf.TargetContainer.GetDashCount(step.ingredient);
                        yield return Pulse(KeyCode.D);
                        yield return Wait(() => !shelf.PhysicalTransferActive, "dash");
                        Require(shelf.TargetContainer.GetDashCount(step.ingredient) == before + 1, "D did not add exactly one dash.");
                    }
                    break;
                case RecipePreparationAction.Stir:
                    yield return Submit(FieldButton(Method(), "startButton"));
                    yield return Submit(FieldButton(Method(), "startButton"));
                    Require(Get(Method(), "state").ToString() == "Stirring", "Premature finish advanced Stir.");
                    float stirringVolume = shelf.TargetContainer.CurrentLiquidVolumeMl;
                    foreach (var key in new[] { KeyCode.I, KeyCode.T, KeyCode.D, KeyCode.M }) yield return Pulse(key);
                    Require(shelf.TargetContainer.CurrentLiquidVolumeMl == stirringVolume, "Preparation input mutated liquid during Stir.");
                    yield return Orbit(false);
                    yield return Submit(FieldButton(Method(), "startButton")); break;
                case RecipePreparationAction.Shake:
                    yield return Submit(FieldButton(Method(), "startButton"));
                    var shake = Find<ShakeMiniGame>();
                    for (int n = 0; n < shake.NoteCount; n++)
                    {
                        yield return Wait(() => Time.unscaledTime >= shake.GetExpectedNoteTime(n) - Time.unscaledDeltaTime, "shake note");
                        yield return Pulse(n % 2 == 0 ? KeyCode.LeftArrow : KeyCode.RightArrow);
                    }
                    yield return Wait(() => Get(shake, "state").ToString() == "AwaitingStrain", "rhythm end"); break;
                case RecipePreparationAction.Strain:
                    yield return Submit(FieldButton(Method(), "startButton"));
                    yield return Wait(() => !shelf.PhysicalTransferActive && (Find<ShakeMiniGame>() == null || !Find<ShakeMiniGame>().IsPhysicalTransferPending), "strain"); break;
                case RecipePreparationAction.PourFromContainer:
                    yield return Pulse(KeyCode.P);
                    yield return Wait(() => !shelf.PhysicalTransferActive && !Find<ShakeMiniGame>().IsPhysicalTransferPending, "unstrained transfer"); break;
                case RecipePreparationAction.Splash:
                    yield return Pulse(KeyCode.S);
                    yield return Wait(() => !shelf.PhysicalTransferActive, "splash"); break;
                case RecipePreparationAction.GentleStir:
                    yield return Pulse(KeyCode.J);
                    Require(Get(Method(), "state").ToString() == "GentleStirring", "J did not start gentle stir.");
                    yield return Orbit(true);
                    yield return Submit(FieldButton(Method(), "startButton")); break;
                case RecipePreparationAction.Garnish:
                    if (recipe.method == MiniGameType.Build || recipe.method == MiniGameType.Float)
                        yield return Submit(FieldButton(Method(), "startButton"));
                    var garnish = step.acceptedGarnishes[0];
                    var keys = new Dictionary<BartendingGarnishId, KeyCode> {
                        [BartendingGarnishId.LemonTwist] = KeyCode.Alpha1, [BartendingGarnishId.Cherry] = KeyCode.Alpha2,
                        [BartendingGarnishId.Olive] = KeyCode.Alpha3, [BartendingGarnishId.OrangeSlice] = KeyCode.Alpha4,
                        [BartendingGarnishId.LemonSlice] = KeyCode.Alpha5, [BartendingGarnishId.OrangeZest] = KeyCode.Alpha6 };
                    yield return Pulse(keys[garnish.garnish]);
                    yield return Pulse(garnish.placement == BartendingGarnishPlacement.Rim ? KeyCode.Q :
                        garnish.placement == BartendingGarnishPlacement.InGlass ? KeyCode.W : KeyCode.E);
                    yield return Pulse(KeyCode.G);
                    yield return Wait(() => !Find<ServingGlassGarnishController>().IsPlacing, "garnish");
                    Require(shelf.ServingGlass.Garnish != null && shelf.ServingGlass.Garnish.GarnishId == garnish.garnish, "Garnish key failed."); break;
                default: throw new InvalidOperationException("Uncovered production action: " + step.action);
            }
        }
        if (Method() != null && Get(Method(), "state").ToString() == "Preparing") yield return Submit(FieldButton(Method(), "startButton"));
        if (Method() != null) yield return Submit(FieldButton(Method(), "startButton"));
        yield return Wait(() => !Find<BartendingFlowCoordinator>().IsBartendingActive, "result and unload");
        Require(completions == 1, "Exactly one completion required.");
        var match = RecipeMatchService.FindBestMatch(result.brewedComposition, Find<RecipeUnlockManager>().GetAllRecipes());
        Require(match.success && match.matchedRecipe == recipe, "Actual input composition did not match " + id);
        Require(result.preparationEvaluation != null && result.preparationEvaluation.Status == RecipePreparationEvaluationStatus.Satisfied,
            "Actual input preparation did not satisfy " + id + ": " + result.preparationEvaluation?.Detail);
        foreach (var key in new[] { KeyCode.I, KeyCode.T, KeyCode.D, KeyCode.G, KeyCode.Space }) yield return Pulse(key);
        Require(completions == 1 && Find<IngredientShelfController>() == null, "Input after completion re-entered or duplicated the route.");
        yield return Wait(() => !Find<DemoAudioController>().Sounds.PourSource.isPlaying, "route-local audio cleanup");
        Record("RECIPE " + id + " recognized once");
    }
    IEnumerator Orbit(bool gentle)
    {
        var method = Method(); RectTransform center;
        if (!gentle) center = (RectTransform)method.GetType().GetMethod("GetSpoonCenter", Fields).Invoke(method, null);
        else center = (RectTransform)Get(Get(method, "gentleStirInteraction"), "center");
        var canvas = center.GetComponentInParent<Canvas>();
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, center.position);
        float elapsed = 0;
        while (elapsed < (gentle ? 3.5f : 6.2f))
        {
            float angle = elapsed * 180f * Mathf.Deg2Rad;
            input.MousePosition = point + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 100;
            yield return null;
            elapsed += Time.deltaTime;
        }
    }
    IEnumerator Fuzz(string id, int seed)
    {
        var recipe = Recipe(id); yield return Open(recipe);
        var shelf = Find<IngredientShelfController>();
        var random = new System.Random(seed);
        Time.captureDeltaTime = seed == 92 ? 1f / 30f : seed == 49524 ? 1f / 144f : 1f / 60f;
        KeyCode[] invalid = { KeyCode.T, KeyCode.G, KeyCode.X, KeyCode.P, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.Alpha6 };
        string knowledge = RecipeProgressPersistence.LoadRecipeKnowledgeRaw();
        for (int n = 0; n < 28; n++)
        {
            yield return Pulse(invalid[random.Next(invalid.Length)]);
            Require(shelf.TargetContainer.CurrentLiquidVolumeMl == 0 && shelf.PreparationTrace.Actions.Count == 0,
                "Invalid key changed empty preparation; seed=" + seed + " index=" + n);
        }
        for (int n = 0; n < 2; n++)
        {
            yield return Submit(FieldButton(Find<RecipeGuidePanelController>(), "openButton"));
            yield return Pulse(KeyCode.I);
            Require(!shelf.TargetContainer.HasIce, "Guide accepted underlying I.");
            yield return Pulse(KeyCode.Escape);
            Require(!RecipeGuidePanelController.IsTechniqueReadStateActive, "Escape did not close Guide.");
            yield return Pulse(KeyCode.Escape);
            Require(DemoAudioController.IsSettingsOpen, "Escape did not open Settings.");
            yield return Pulse(KeyCode.I);
            Require(!shelf.TargetContainer.HasIce, "Settings accepted underlying I.");
            yield return Pulse(KeyCode.Escape);
            Require(!DemoAudioController.IsSettingsOpen && !SurlhwaModalSurface.IsPaused, "Settings retained modal ownership.");
        }
        Require(knowledge == RecipeProgressPersistence.LoadRecipeKnowledgeRaw(), "UI changed recipe knowledge.");
        yield return Select(recipe.ingredients[0].ingredient);
        yield return Measure(60);
        Require(Mathf.Abs(shelf.TargetContainer.CurrentLiquidVolumeMl - 60f) < .001f,
            "Finite measurement conservation: actual=" + shelf.TargetContainer.CurrentLiquidVolumeMl.ToString("R"));
        input.Set(KeyCode.I);
        for (int n = 0; n < 25; n++) yield return null;
        input.Set(); yield return null;
        yield return Wait(() => !shelf.PhysicalTransferActive, "held ice");
        Require(shelf.TargetContainer.HasIce && shelf.PreparationTrace.Actions.Count(a => a.Action == RecipePreparationAction.AddIce) == 1,
            "Held I must apply once after invalid inputs/modals.");
        Time.captureDeltaTime = 1f / 60f;
    }
    IEnumerator DisabledSettings()
    {
        yield return Open(Recipe("recipe_dry_martini"));
        yield return Pulse(KeyCode.Escape);
        var audio = Find<DemoAudioController>();
        Require(DemoAudioController.IsSettingsOpen && SurlhwaModalSurface.IsPaused, "Settings did not acquire pause.");
        CaptureScreen("settings");
        audio.gameObject.SetActive(false);
        yield return null;
        Require(!DemoAudioController.IsSettingsOpen && !SurlhwaModalSurface.IsPaused,
            "Disabled settings owner retained the global reading/input gate.");
        audio.gameObject.SetActive(true);
        yield return Pulse(KeyCode.Escape);
        Require(DemoAudioController.IsSettingsOpen, "Re-enabled Settings cannot reopen.");
        yield return Pulse(KeyCode.Escape);
    }
    IEnumerator ReloadMeasured(string id)
    {
        for (int cycle = 0; cycle < 2; cycle++)
        {
            var recipe = Recipe(id); yield return Open(recipe);
            var shelf = Find<IngredientShelfController>();
            Require(!shelf.Measuring && !shelf.HasPendingMeasurement && shelf.TargetContainer.CurrentLiquidVolumeMl == 0,
                "Re-entry retained measurement or liquid.");
            yield return Select(recipe.ingredients[0].ingredient);
            yield return Pulse(KeyCode.M);
            input.Set(KeyCode.Space);
            yield return Wait(() => shelf.Jigger.CurrentLiquidVolumeMl >= 5, "partial measure before reload");
            input.Set(); yield return null;
            yield return Wait(() => shelf.PhysicalPresentation.IsResting, "partial measure bottle return");
            float pending = shelf.Jigger.CurrentLiquidVolumeMl;
            string knowledge = RecipeProgressPersistence.LoadRecipeKnowledgeRaw();
            foreach (var key in new[] { KeyCode.M, KeyCode.I, KeyCode.D, KeyCode.J }) yield return Pulse(key);
            Require(shelf.Measuring && shelf.Jigger.CurrentLiquidVolumeMl == pending &&
                shelf.TargetContainer.CurrentLiquidVolumeMl == 0 && shelf.PreparationTrace.Actions.Count == 0,
                "Pending measure accepted an incompatible preparation action.");
            yield return Pulse(KeyCode.Escape);
            yield return Pulse(KeyCode.Space);
            Require(shelf.Jigger.CurrentLiquidVolumeMl == pending, "Settings allowed measuring.");
            yield return Pulse(KeyCode.Escape);
            input.Set(KeyCode.Space); yield return null;
            yield return Reload(); // Production scene restart, not an unsupported in-route Cancel binding.
            Require(shelf == null && completions == 0 && knowledge == RecipeProgressPersistence.LoadRecipeKnowledgeRaw(),
                "Aborted route survived, completed service, or changed knowledge.");
            Record("active-measure reload " + id + " cycle=" + cycle + " completions=0");
        }
        yield return RecipeCase(id);
    }
    IEnumerator HeldModal()
    {
        yield return Open(Recipe("recipe_dry_martini"));
        var shelf = Find<IngredientShelfController>();
        yield return Select(Recipe("recipe_dry_martini").ingredients[0].ingredient);
        input.Set(KeyCode.Space); yield return null;
        yield return Submit(FieldButton(Find<RecipeGuidePanelController>(), "openButton"));
        CaptureScreen("guide");
        float before = shelf.TargetContainer.CurrentLiquidVolumeMl;
        input.Set(); yield return null; // Release while the production route is paused.
        yield return Submit(FieldButton(Find<RecipeGuidePanelController>(), "closeButton"), () => input.Set(KeyCode.Space));
        yield return Wait(() => shelf.TargetContainer.CurrentLiquidVolumeMl > before, "first fresh Space after reading", 3);
        input.Set(); yield return null;
        yield return Wait(() => shelf.PhysicalPresentation.IsResting, "modal resume bottle return");
    }
    void CaptureScreen(string name)
    {
        if (!captures.Add(name)) return;
        Canvas.ForceUpdateCanvases();
        foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            var canvas = button.GetComponentInParent<Canvas>();
            if (!button.IsActive() || !button.IsInteractable() || canvas == null || !canvas.rootCanvas.isActiveAndEnabled ||
                !SurlhwaModalSurface.Allows(button.transform)) continue;
            var rect = (RectTransform)button.transform;
            Require(rect.rect.width > 0 && rect.rect.height > 0, "Zero-size interactable: " + button.name);
            // Scroll content can legitimately be outside its viewport. Its
            // delivery/scroll behavior is exercised by the existing UI suite.
            if (button.GetComponentInParent<ScrollRect>() != null) continue;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (camera != null && !camera.isActiveAndEnabled) continue;
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var points = corners.Select(p => RectTransformUtility.WorldToScreenPoint(camera, p)).ToArray();
            Require(points.Max(p => p.x) > 0 && points.Min(p => p.x) < Screen.width &&
                points.Max(p => p.y) > 0 && points.Min(p => p.y) < Screen.height,
                "Non-scroll interactable fully offscreen: " + button.name);
            Record("layout.control " + name + "/" + button.name + " size=" + rect.rect.size);
        }
        var primary = FieldButton(Method(), "startButton");
        if (primary != null && primary.IsActive())
        {
            var rect = (RectTransform)primary.transform;
            Require(rect.rect.width > 0 && rect.rect.height > 0, "Zero-size primary interactable.");
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var canvas = rect.GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var points = corners.Select(p => RectTransformUtility.WorldToScreenPoint(camera, p)).ToArray();
            Require(points.Max(p => p.x) > 0 && points.Min(p => p.x) < Screen.width &&
                points.Max(p => p.y) > 0 && points.Min(p => p.y) < Screen.height, "Primary interactable fully offscreen.");
        }
        Capture?.Invoke("RCAutomatedQA/screens/" + name, 1280, 720);
        Capture?.Invoke("RCAutomatedQA/screens/" + name, 1920, 1080);
        Record("layout " + name + " viewport=" + Screen.width + "x" + Screen.height);
    }
    string Snapshot(string reason)
    {
        var shelf = Find<IngredientShelfController>();
        var focus = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        var customer = Find<CustomerController>();
        var seats = FindObjectsByType<SeatPoint>(FindObjectsSortMode.None);
        var hits = new List<RaycastResult>();
        if (EventSystem.current != null) EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = input.MousePosition }, hits);
        var pending = Get(Method(), "onDone") as Delegate;
        return reason + "\nscene=" + SceneManager.GetActiveScene().path + "\nroute=" + lastState +
            "\ncustomer=" + customer + "/" + customer?.CurrentState + "\nmodal=" + SurlhwaModalSurface.Top + "\nfocus=" + focus +
            "\nraycast(synthetic cursor)=" + (hits.Count > 0 ? hits[0].gameObject.name : "none") +
            "\nseats=" + string.Join(",", seats.Select(s => s.name + ":" + s.IsOccupied + ":" + s.Occupant)) +
            "\norder=" + (customer != null && customer.TryGetDedicatedRequest(out var order) ? order.name : "fixture") +
            "\nrecipe=" + Get(Method(), "expectedRecipe") + "\ncompletionSubscribers=" + (pending?.GetInvocationList().Length ?? 0) +
            "\ninputOwner=" + SurlhwaGameplayInput.SourceName + "\nlastInput=" + input.LastObservation +
            "\nlastTransition=" + lastTransition + "\nmethodEnabled=" + (Method() != null && Method().enabled) +
            "\ncontainer=" + shelf?.TargetContainer?.ContainerType + " ml=" + shelf?.TargetContainer?.CurrentLiquidVolumeMl +
            " ice=" + shelf?.TargetContainer?.HasIce + "\nmeasuring=" + shelf?.Measuring + " jigger=" + shelf?.Jigger?.CurrentLiquidVolumeMl +
            " transfer=" + shelf?.PhysicalTransferActive + " toolsReady=" + shelf?.ToolsReady + "\nactions=" + shelf?.PreparationTrace?.Actions.Count +
            "\npendingCompletion=" + completions + "\ntraceTail=\n" + string.Join("\n", trace.Skip(Math.Max(0, trace.Count - 40)));
    }
    void Cleanup() { inputLease?.Dispose(); inputLease = null; Time.captureDeltaTime = oldCapture; Application.targetFrameRate = oldRate; }
    void OnDestroy() => Cleanup();
}
static class SurlhwaRCEnumeratorExtensions
{
    public static void DisposeIfNeeded(this IEnumerator iterator) { (iterator as IDisposable)?.Dispose(); }
}
#endif
