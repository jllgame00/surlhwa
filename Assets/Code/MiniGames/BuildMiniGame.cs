using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// I4's direct Build route. The Serving Glass records actual Pour Core state;
/// confirming preparation submits that state without a second timing score.
/// </summary>
public class BuildMiniGame : MonoBehaviour, IMiniGame, IRequestDisplayTitleReceiver,
    IRecipePreparationExpectationReceiver, IIngredientAvailabilityContextReceiver
{
    public enum BuildState
    {
        Preparing,
        GentleStirring,
        AwaitingGarnish,
        Result
    }

    [Header("UI")]
    [SerializeField] private TMP_Text ingredientText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private TMP_Text resultLabel;

    [Header("Completion UI")]
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text startHintText;

    [Header("Ingredient Shelf")]
    [SerializeField] private IngredientShelfController ingredientShelf;

    [Header("Pour Core")]
    [SerializeField] private float previewPourRateMlPerSecond = 30f;

    private BuildState state = BuildState.Preparing;
    private Action<MiniGameResult> onDone;
    private string requestDisplayTitle;
    private bool isPouring;
    private ServingGlassGarnishController garnishController;
    private CocktailRecipeData expectedRecipe;
    private ServingGlassGentleStirInteraction gentleStirInteraction;
    private IngredientAvailabilityContext ingredientAvailabilityContext;

    // I2 runtime guidance only. It never inserts liquid or completes Build.
    private BartendingTopUpGuidance topUpGuidance;

    public void SetRequestDisplayTitle(string displayTitle)
    {
        requestDisplayTitle = displayTitle;
    }

    public void SetExpectedRecipe(CocktailRecipeData recipe)
    {
        expectedRecipe = recipe;
    }

    public void SetIngredientAvailabilityContext(IngredientAvailabilityContext context)
    {
        ingredientAvailabilityContext = context;
    }

    public void StartGame(Action<MiniGameResult> done)
    {
        onDone = done;

        AutoBindIfMissing();
        SetLegacyScorePresentationVisible(false);

        if (ingredientShelf == null)
            ingredientShelf = GetComponentInChildren<IngredientShelfController>(true);

        if (ingredientShelf == null)
            throw new InvalidOperationException("[BuildMiniGame] IngredientShelfController is required.");

        // A Build starts with a fresh preparation session whose only active
        // target is the final Serving Glass. I3's other container APIs remain
        // available for later technique routes but are not Build controls.
        ingredientShelf.Initialize(
            MiniGameType.Build,
            BartendingContainerType.ServingGlass,
            ingredientAvailabilityContext);
        ConfigureGarnishController();

        state = BuildState.Preparing;
        isPouring = false;
        topUpGuidance = null;
        gentleStirInteraction = new ServingGlassGentleStirInteraction();

        if (ingredientText != null)
            ingredientText.text = GetDisplayTitle();

        if (resultLabel != null)
            resultLabel.text = string.Empty;

        if (startHintText != null)
        {
            startHintText.gameObject.SetActive(true);
            startHintText.text = "Build directly in the Serving Glass. Finish Build when the liquid is ready.";
        }

        if (startButton != null)
        {
            startButton.gameObject.SetActive(true);
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(CompleteBuild);
            SetPrimaryActionLabel("Finish Build");
        }

        UpdateProgressText();
        SetHint("Select an ingredient and hold SPACE to pour. Press I for ice, D for one dash, or J for a gentle serving-glass stir.");

        Debug.Log(
            "[BuildMiniGame] Direct Build ready / " +
            "Target=ServingGlass / Completion=manual");
    }

    /// <summary>
    /// The player, not recipe expectation data, chooses when liquid preparation
    /// ends. I8 then gives the player one optional non-liquid garnish step
    /// before final submission.
    /// </summary>
    public void CompleteBuild()
    {
        if (state != BuildState.Preparing)
        {
            Debug.LogWarning($"[BuildMiniGame] Complete ignored. state={state}");
            return;
        }

        if (ingredientShelf == null || !ingredientShelf.HasServingGlassContents())
        {
            SetHint("Pour at least one ingredient into the Serving Glass before completing.");
            Debug.LogWarning("[BuildMiniGame] Cannot complete. Serving Glass has no liquid.");
            return;
        }

        StopDirectPour("Build liquid preparation finished");
        state = BuildState.AwaitingGarnish;

        if (resultLabel != null)
            resultLabel.text = "Liquid Ready";

        if (startHintText != null)
            startHintText.gameObject.SetActive(false);

        ingredientShelf.SetVisible(false);
        garnishController?.BeginSelection();
        SetPrimaryAction(ServeBuild, "Serve Build", true);
        SetHint(garnishController != null && garnishController.IsExpectedGarnishRequired
            ? "Serving Glass ready. Garnish is required before serving."
            : "Serving Glass ready. Garnish is optional; choose one or serve the drink as made.");
        UpdateProgressText();

        Debug.Log($"[BuildMiniGame] Liquid preparation ready / ServingMl={ingredientShelf.ServingGlass.CurrentLiquidVolumeMl:0.###}");
    }

    /// <summary>
    /// Final manual confirmation after the optional garnish decision. The
    /// Serving Glass remains the only source of both liquid and garnish truth.
    /// </summary>
    public void ServeBuild()
    {
        if (state != BuildState.AwaitingGarnish)
        {
            Debug.LogWarning($"[BuildMiniGame] Serve ignored. state={state}");
            return;
        }

        BrewComposition composition = ingredientShelf.BuildServingGlassComposition();
        BartendingGarnishState garnish = ingredientShelf.ServingGlass != null
            ? ingredientShelf.ServingGlass.Garnish
            : null;
        state = BuildState.Result;
        garnishController?.EndSelection();

        if (resultLabel != null)
            resultLabel.text = "Complete";

        SetPrimaryAction(null, string.Empty, false);

        Debug.Log(
            $"[BuildMiniGame] Direct Build complete / " +
            $"Method={composition.method} / " +
            $"Count={(composition.ingredients != null ? composition.ingredients.Length : 0)} / " +
            $"ServingMl={ingredientShelf.ServingGlass.CurrentLiquidVolumeMl:0.###} / " +
            $"Garnish={(garnish != null ? garnish.DisplayName + "/" + garnish.Placement : "none")}");

        // The shared result contract currently has no distinct
        // TechniqueCompleted state. success therefore means the player
        // completed direct preparation, not that a timing grade was awarded.
        // Recognition/request correctness remains downstream in OrderManager.
        MiniGameResult result = new MiniGameResult
        {
            success = true,
            repDelta = 0,
            grade = "Complete",
            brewedComposition = composition,
            garnish = garnish
        };
        BartendingPreparationResultFactory.Apply(
            ref result,
            expectedRecipe,
            ingredientShelf.PreparationTrace);
        onDone?.Invoke(result);
    }

    public bool TryApplyGarnish(BartendingGarnishId garnishId, BartendingGarnishPlacement placement)
    {
        return state == BuildState.AwaitingGarnish &&
               garnishController != null &&
               garnishController.TryApplyGarnish(garnishId, placement);
    }

    public bool ClearGarnish()
    {
        return state == BuildState.AwaitingGarnish &&
               garnishController != null &&
               garnishController.ClearGarnish();
    }

    private void Update()
    {
        if (RecipeGuidePanelController.IsTechniqueReadStateActive)
            return;

        if (state == BuildState.Preparing)
            UpdateDirectPour();
        else if (state == BuildState.GentleStirring)
            UpdateGentleStir();
    }

    private void UpdateDirectPour()
    {
        if (ingredientShelf == null || !ingredientShelf.isActiveAndEnabled)
        {
            StopDirectPour("Serving Glass unavailable");
            return;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            TryDispenseDash();
            return;
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            AddIceToServingGlass();
            return;
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            BeginGentleStir();
            return;
        }

        if (!Input.GetKey(KeyCode.Space))
        {
            StopDirectPour("input released");
            ShowTopUpGuidance(ingredientShelf.SelectedIngredient);
            return;
        }

        IngredientData ingredient = ingredientShelf.SelectedIngredient;
        if (ingredient == null)
        {
            StopDirectPour("no ingredient selected");
            SetHint("Select an ingredient before pouring.");
            return;
        }

        float requestedMl = Mathf.Max(0f, previewPourRateMlPerSecond) * Time.deltaTime;
        float transferredMl = ingredientShelf.PourSelectedIngredient(requestedMl);
        if (transferredMl <= 0f)
        {
            StopDirectPour("transfer rejected");
            return;
        }

        if (!isPouring)
        {
            isPouring = true;
            Debug.Log(
                $"[Pour] Start ingredient={ingredient.ingredientId} target=ServingGlass " +
                $"rate={previewPourRateMlPerSecond:0.##} ml/s");
        }

        float ingredientMl = ingredientShelf.ServingGlass.GetLiquidAmountMl(ingredient);
        if (!ShowTopUpGuidance(ingredient))
            SetHint($"Pouring {ingredient.displayName}: {ingredientMl:0.0} ml in Serving Glass.");

        UpdateProgressText();
    }

    /// <summary>
    /// Narrow I2 seam for future recipe instruction data and runtime validation.
    /// Guidance is not a completion condition and never creates liquid itself.
    /// </summary>
    public bool ConfigureTopUpGuidance(IngredientData ingredient, float targetFinalVolumeMl)
    {
        BartendingTopUpGuidance guidance = new BartendingTopUpGuidance(ingredient, targetFinalVolumeMl);
        if (!guidance.IsValid)
        {
            Debug.LogWarning("[BuildMiniGame] Ignored invalid TopUp guidance.");
            return false;
        }

        topUpGuidance = guidance;
        ShowTopUpGuidance(ingredientShelf != null ? ingredientShelf.SelectedIngredient : null);
        return true;
    }

    public void ClearTopUpGuidance()
    {
        topUpGuidance = null;
    }

    public float GetTopUpSignedRemainingMl()
    {
        float currentVolumeMl = ingredientShelf != null && ingredientShelf.ServingGlass != null
            ? ingredientShelf.ServingGlass.CurrentLiquidVolumeMl
            : 0f;

        return topUpGuidance != null ? topUpGuidance.GetSignedRemainingMl(currentVolumeMl) : 0f;
    }

    private void TryDispenseDash()
    {
        if (ingredientShelf == null || ingredientShelf.SelectedIngredient == null)
        {
            SetHint("Select an ingredient before adding a dash.");
            return;
        }

        if (!ingredientShelf.TryDashSelectedIngredient(out int totalDashCount))
        {
            SetHint("That dash could not be added to the Serving Glass.");
            return;
        }

        IngredientData ingredient = ingredientShelf.SelectedIngredient;
        float ingredientMl = ingredientShelf.ServingGlass.GetLiquidAmountMl(ingredient);
        SetHint($"{ingredient.displayName} — {totalDashCount} dash ({ingredientMl:0.0} ml).");
        UpdateProgressText();

        Debug.Log(
            $"[Dash] Ingredient={ingredient.ingredientId} count={totalDashCount} " +
            $"compatibilityMl={ingredientMl:0.###}");
    }

    public bool AddIceToServingGlass()
    {
        if (state != BuildState.Preparing || ingredientShelf == null ||
            !ingredientShelf.TryAddIceToActiveTarget())
        {
            SetHint("The Serving Glass already has ice or is unavailable.");
            return false;
        }

        SetHint("Ice added to the Serving Glass.");
        UpdateProgressText();
        return true;
    }

    /// <summary>
    /// A short optional serving-glass action for Build recipes such as Negroni.
    /// It is never auto-started by expected recipe data, so omitting it remains
    /// a valid submitted preparation mistake.
    /// </summary>
    public void BeginGentleStir()
    {
        if (state != BuildState.Preparing || ingredientShelf == null ||
            !ingredientShelf.HasServingGlassContents())
        {
            SetHint("Pour into the Serving Glass before gently stirring.");
            return;
        }

        StopDirectPour("Gentle Stir started");
        RectTransform center = transform.Find("UI/GlassImage")?.GetComponent<RectTransform>();
        Transform visualParent = transform.Find("UI") ?? transform;
        if (gentleStirInteraction == null || !gentleStirInteraction.Begin(center, visualParent))
        {
            SetHint("Gentle Stir visual could not start.");
            return;
        }

        state = BuildState.GentleStirring;
        ingredientShelf.SetVisible(false);
        SetPrimaryAction(FinishGentleStir, "Finish Gentle Stir", true);
        SetHint("Move the mouse in a light circle around the Serving Glass.");
        UpdateProgressText();
    }

    private void UpdateGentleStir()
    {
        gentleStirInteraction?.Tick();
        UpdateProgressText();
    }

    public bool TryRecordGentleStirAngle(float angleDeg, float deltaTime)
    {
        return state == BuildState.GentleStirring &&
               gentleStirInteraction != null &&
               gentleStirInteraction.TryRecordAngle(angleDeg, deltaTime);
    }

    public void FinishGentleStir()
    {
        if (state != BuildState.GentleStirring || gentleStirInteraction == null)
            return;

        if (!gentleStirInteraction.TryFinish())
        {
            SetHint("Keep gently stirring for a little longer.");
            return;
        }

        ingredientShelf.RecordTechniqueAction(
            RecipePreparationAction.GentleStir,
            BartendingContainerType.ServingGlass);
        state = BuildState.Preparing;
        ingredientShelf.SetVisible(true);
        SetPrimaryAction(CompleteBuild, "Finish Build", true);
        SetHint("Gentle Stir complete. Continue pouring or finish the Build.");
        UpdateProgressText();
        Debug.Log("[GentleStir] Build ServingGlass complete");
    }

    private bool ShowTopUpGuidance(IngredientData selectedIngredient)
    {
        if (topUpGuidance == null || !topUpGuidance.AppliesTo(selectedIngredient) ||
            ingredientShelf == null || ingredientShelf.ServingGlass == null)
            return false;

        float currentMl = ingredientShelf.ServingGlass.CurrentLiquidVolumeMl;
        float signedRemainingMl = topUpGuidance.GetSignedRemainingMl(currentMl);
        string status = signedRemainingMl > 0f
            ? $"{signedRemainingMl:0.0} ml remaining"
            : signedRemainingMl < 0f
                ? $"{-signedRemainingMl:0.0} ml over"
                : "at target";

        SetHint(
            $"Top up {selectedIngredient.displayName}: {currentMl:0.0} / " +
            $"{topUpGuidance.TargetFinalVolumeMl:0.0} ml ({status}).");

        if (progressText != null)
            progressText.text = $"Top Up {currentMl:0.0} / {topUpGuidance.TargetFinalVolumeMl:0.0} ml";

        return true;
    }

    private void StopDirectPour(string reason)
    {
        if (!isPouring)
        {
            ingredientShelf?.EndSelectedPour();
            return;
        }

        isPouring = false;
        ingredientShelf?.EndSelectedPour();

        IngredientData ingredient = ingredientShelf != null ? ingredientShelf.SelectedIngredient : null;
        float ingredientMl = ingredientShelf != null && ingredientShelf.ServingGlass != null && ingredient != null
            ? ingredientShelf.ServingGlass.GetLiquidAmountMl(ingredient)
            : 0f;
        float totalMl = ingredientShelf != null && ingredientShelf.ServingGlass != null
            ? ingredientShelf.ServingGlass.CurrentLiquidVolumeMl
            : 0f;

        Debug.Log(
            $"[Pour] Stop reason={reason} ingredient={(ingredient != null ? ingredient.ingredientId : "NULL")} " +
            $"ingredientMl={ingredientMl:0.0} totalMl={totalMl:0.0}");
    }

    private void UpdateProgressText()
    {
        if (progressText == null)
            return;

        BartendingContainerState servingGlass = ingredientShelf != null
            ? ingredientShelf.ServingGlass
            : null;

        if (servingGlass == null)
        {
            progressText.text = "Serving Glass 0.0 ml";
            return;
        }

        string iceText = servingGlass.HasIce ? " / ice" : string.Empty;
        progressText.text = $"Serving Glass {servingGlass.CurrentLiquidVolumeMl:0.0} ml{iceText}";
    }

    private void SetPrimaryAction(Action action, string label, bool visible)
    {
        if (startButton == null)
            return;

        startButton.onClick.RemoveAllListeners();
        if (action != null)
            startButton.onClick.AddListener(() => action());

        startButton.gameObject.SetActive(visible);
        SetPrimaryActionLabel(label);
    }

    private void SetPrimaryActionLabel(string label)
    {
        TMP_Text buttonText = startButton != null ? startButton.GetComponentInChildren<TMP_Text>(true) : null;

        if (buttonText != null)
            buttonText.text = label;
    }

    // The prefab retains the old score-gauge hierarchy while Stir, Shake, and
    // Float are still legacy routes. Build must not present that detached
    // target zone as an active second test.
    private void SetLegacyScorePresentationVisible(bool visible)
    {
        Transform liquidArea = transform.Find("UI/LiquidArea");
        if (liquidArea != null)
            liquidArea.gameObject.SetActive(visible);
    }

    private void SetHint(string message)
    {
        if (hintText != null)
            hintText.text = message;
    }

    private string GetDisplayTitle()
    {
        return !string.IsNullOrEmpty(requestDisplayTitle)
            ? requestDisplayTitle
            : "Build";
    }

    private void OnDisable()
    {
        StopDirectPour("BuildMiniGame disabled");
        gentleStirInteraction?.End();
    }

    private void ConfigureGarnishController()
    {
        garnishController = GetComponent<ServingGlassGarnishController>();
        if (garnishController == null)
            garnishController = gameObject.AddComponent<ServingGlassGarnishController>();

        RectTransform servingGlassVisual = transform.Find("UI/GlassImage")?.GetComponent<RectTransform>();
        garnishController.Initialize(
            ingredientShelf != null ? ingredientShelf.ServingGlass : null,
            servingGlassVisual,
            hintText,
            ingredientShelf != null ? ingredientShelf.PreparationTrace : null);
        garnishController.SetExpectedRecipe(expectedRecipe);
    }

    private void AutoBindIfMissing()
    {
        if (ingredientText == null)
            ingredientText = transform.Find("UI/IngredientText")?.GetComponent<TMP_Text>();

        if (progressText == null)
            progressText = transform.Find("UI/ProgressText")?.GetComponent<TMP_Text>();

        if (hintText == null)
            hintText = transform.Find("UI/HintText")?.GetComponent<TMP_Text>();

        if (resultLabel == null)
            resultLabel = transform.Find("UI/ResultLabel")?.GetComponent<TMP_Text>();

        if (startButton == null)
            startButton = transform.Find("UI/StartButton")?.GetComponent<Button>();

        if (startButton == null)
            startButton = transform.Find("UI/StartupButton")?.GetComponent<Button>();

        if (startHintText == null)
            startHintText = transform.Find("UI/StartHintText")?.GetComponent<TMP_Text>();
    }
}
