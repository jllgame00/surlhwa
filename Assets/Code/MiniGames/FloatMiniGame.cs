using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// I7's direct Float route. The Serving Glass remains the authoritative liquid
/// state; this class adds only the ordered layer and boundary-quality companion
/// required to make the player's controlled additions visible and judgeable.
/// </summary>
public class FloatMiniGame : MonoBehaviour, IMiniGame, IRequestDisplayTitleReceiver,
    IRecipePreparationExpectationReceiver, IIngredientAvailabilityContextReceiver
{
    public enum FloatPourIntensity
    {
        Gentle,
        Normal,
        Aggressive
    }

    private enum State
    {
        Preparing,
        AwaitingGarnish,
        Result
    }

    private enum FloatGrade
    {
        Good,
        Acceptable,
        Poor
    }

    [Header("Legacy Visual Roots Reused for I7")]
    [SerializeField] private RectTransform liquidArea;
    [SerializeField] private RectTransform baseLiquid;
    [SerializeField] private RectTransform topLiquid;
    [SerializeField] private RectTransform targetZone;
    [SerializeField] private RectTransform spoon;

    [Header("UI")]
    [SerializeField] private TMP_Text ingredientText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private TMP_Text resultLabel;

    [Header("Primary Action")]
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text startHintText;

    [Header("Layer Pour Tuning")]
    [SerializeField] private float firstLayerPourRateMlPerSecond = 30f;
    [SerializeField] private float gentleLayerPourRateMlPerSecond = 15f;
    [SerializeField] private float normalLayerPourRateMlPerSecond = 30f;
    [SerializeField] private float aggressiveLayerPourRateMlPerSecond = 60f;
    [SerializeField] private float cleanMaximumAverageRateMlPerSecond = 22f;
    [SerializeField] private float partialMaximumAverageRateMlPerSecond = 42f;
    [SerializeField] private float cleanMaximumAggressiveRatio = 0.1f;
    [SerializeField] private float partialMaximumAggressiveRatio = 0.5f;

    [Header("Display")]
    [SerializeField] private string defaultDisplayName = "Float";
    [SerializeField] private float spoonOffsetY = 6f;
    [SerializeField, Min(1f)] private float visualVolumeMl = 240f;
    private RectTransform physicalSurface;
    private FloatPourIntensity measuredPourIntensity = FloatPourIntensity.Normal;

    [Header("Ingredient Shelf")]
    [SerializeField] private IngredientShelfController ingredientShelf;

    private readonly List<RectTransform> layerVisuals = new List<RectTransform>();
    private readonly List<GameObject> runtimeLayerVisuals = new List<GameObject>();
    private readonly List<GameObject> boundaryVisuals = new List<GameObject>();

    private State state = State.Preparing;
    private Action<MiniGameResult> onDone;
    private string requestDisplayTitle;
    private BartendingLayeredDrinkState layeredDrinkState;
    private FloatGrade completedGrade;
    private float activeLayerDuration;
    private float activeLayerAggressiveDuration;
    private float activeLayerTransferredMl;
    private FloatPourIntensity activePourIntensity;
    private Vector2 spoonStartPosition;
    private ServingGlassGarnishController garnishController;
    private IngredientAvailabilityContext ingredientAvailabilityContext;
    private CocktailRecipeData expectedRecipe;

    public BartendingLayeredDrinkState LayeredDrinkState => layeredDrinkState;
    public int RenderedLayerCount => layerVisuals.Count;
    public int RenderedBoundaryCount => boundaryVisuals.Count;

    public void SetRequestDisplayTitle(string displayTitle)
    {
        requestDisplayTitle = displayTitle;
    }

    public void SetIngredientAvailabilityContext(IngredientAvailabilityContext context)
    {
        ingredientAvailabilityContext = context;
    }

    /// <summary>
    /// Float keeps its own layered-drink authority. This expectation is only
    /// forwarded with the completed trace to the shared recipe-preparation
    /// evaluator, matching the other production method routes.
    /// </summary>
    public void SetExpectedRecipe(CocktailRecipeData recipe)
    {
        expectedRecipe = recipe;
    }

    public void StartGame(Action<MiniGameResult> done)
    {
        onDone = done;
        AutoBindIfMissing();

        if (ingredientShelf == null)
            ingredientShelf = GetComponentInChildren<IngredientShelfController>(true);

        if (ingredientShelf == null)
            throw new InvalidOperationException("[FloatMiniGame] IngredientShelfController is required.");

        // I7 always prepares in the actual Serving Glass.
        ingredientShelf.Initialize(
            MiniGameType.Float,
            BartendingContainerType.ServingGlass,
            ingredientAvailabilityContext);
        ingredientShelf.PhysicalPresentation?.SetServingGlass(
            expectedRecipe != null && expectedRecipe.targetCocktail != null
                ? expectedRecipe.targetCocktail.glassware : CocktailGlassware.OldFashionedGlass);
        ConfigureGarnishController();

        state = State.Preparing;
        layeredDrinkState = new BartendingLayeredDrinkState();
        completedGrade = FloatGrade.Poor;
        ResetActiveLayerMetrics();
        activePourIntensity = FloatPourIntensity.Normal;
        ResetLayerVisuals();
        if (ingredientShelf.PhysicalPresentation != null && liquidArea != null)
        {
            var vessel = ingredientShelf.PhysicalPresentation.GetLiquidMask(ingredientShelf.ServingGlass);
            liquidArea.SetParent(vessel, false);
            liquidArea.anchorMin = new Vector2(.12f, .08f);
            liquidArea.anchorMax = new Vector2(.88f, .88f);
            liquidArea.offsetMin = liquidArea.offsetMax = Vector2.zero;
            liquidArea.SetAsFirstSibling();
            physicalSurface = new GameObject("FloatLiquidSurface", typeof(RectTransform)).GetComponent<RectTransform>();
            physicalSurface.SetParent(liquidArea, false);
            physicalSurface.anchorMin = physicalSurface.anchorMax = new Vector2(.5f, 0f);
            ingredientShelf.PhysicalPresentation.BindLayerSurface(ingredientShelf.ServingGlass, physicalSurface);
            ingredientShelf.MeasuredPourStarted += _ => BeginLayerPour(measuredPourIntensity);
            ingredientShelf.MeasuredPourAccepted += (ml, seconds) => RecordAcceptedLayer(ml, seconds, measuredPourIntensity);
            ingredientShelf.MeasuredPourEnded += () => EndActiveLayerPour();
            ingredientShelf.MeasuredPourRate = () => GetLayerPourRate(
                layeredDrinkState.HasActiveLayer ? layeredDrinkState.ActiveLayer.OrderIndex : layeredDrinkState.Layers.Count,
                measuredPourIntensity);
        }

        if (spoon != null)
        {
            spoon.localScale = Vector3.one;
            spoon.sizeDelta = new Vector2(200f, 19f);
            spoon.pivot = new Vector2(0f, .5f);
            PixelBartendingArt.Apply(spoon, "SpoonHorizontal");
            PixelBartendingArt.Grip(spoon, new Vector2(155f, -8f));
            spoonStartPosition = spoon.anchoredPosition;
            spoon.gameObject.SetActive(false);
        }

        // This was the detached hold/release scoring target. I7 keeps the
        // serialized object harmlessly dormant rather than causing prefab churn.
        if (targetZone != null)
            targetZone.gameObject.SetActive(false);

        if (ingredientText != null)
            ingredientText.text = GetDisplayTitle();

        if (resultLabel != null)
        {
            resultLabel.gameObject.SetActive(true);
            resultLabel.text = string.Empty;
        }

        if (startHintText != null)
        {
            startHintText.gameObject.SetActive(hintText == null);
            startHintText.text = "순서대로 층을 쌓으세요. Space: 붓기 · Shift: 천천히 · Ctrl: 세게";
        }

        SetPrimaryAction(CompleteFloat, "층 쌓기 마치기", true);
        SetHint("재료를 고르고 Space로 첫 층을 부어 주세요. 다음 층부터는 스푼을 따라 흐릅니다.");
        UpdateLayerVisuals();
        UpdateProgressText();

        Debug.Log("[Float] Preparation ready / Target=ServingGlass / OrderedLayers=enabled");
    }

    private void Update()
    {
        if (RecipeGuidePanelController.IsTechniqueReadStateActive || DemoAudioController.IsSettingsOpen)
        {
            if (ingredientShelf != null && !ingredientShelf.PhysicalTransferActive) EndActiveLayerPour();
            return;
        }

        if (state == State.Preparing)
            UpdatePreparationInput();
    }

    /// <summary>
    /// Begins a real ordered liquid addition. The layer is not accepted until
    /// the same ingredient has transferred canonical liquid into the Serving
    /// Glass through the shared Pour Core.
    /// </summary>
    public bool BeginLayerPour(FloatPourIntensity intensity)
    {
        if (state != State.Preparing || ingredientShelf == null || layeredDrinkState == null)
            return false;

        if (layeredDrinkState.HasActiveLayer)
            return true;

        IngredientData ingredient = ingredientShelf.SelectedIngredient;
        if (ingredient == null)
        {
            SetHint("층을 쌓기 전에 재료를 골라 주세요.");
            return false;
        }

        if (!layeredDrinkState.TryBeginLayer(ingredient))
            return false;

        ingredientShelf.BeginSelectedPour();
        ResetActiveLayerMetrics();
        activePourIntensity = intensity;
        bool isFirstLayer = layeredDrinkState.ActiveLayer.OrderIndex == 0;
        SetSpoonVisible(!isFirstLayer);
        SetHint(isFirstLayer
            ? "첫 층 붓기: " + ingredient.displayName + " · " + DescribeFlow(intensity)
            : "층 " + (layeredDrinkState.ActiveLayer.OrderIndex + 1) +
              " · 스푼을 따라 " + DescribeFlow(intensity));
        UpdateProgressText();

        Debug.Log("[Float] Begin layer=" + layeredDrinkState.ActiveLayer.OrderIndex +
                  " ingredient=" + ingredient.ingredientId + " intensity=" + intensity);
        return true;
    }

    /// <summary>
    /// The shared Pour Core transfer and the layer allocation happen together.
    /// The returned value is exactly the canonical ml accepted by the active
    /// Serving Glass, never a recipe target or decorative amount.
    /// </summary>
    public float PourActiveLayerForDuration(float durationSeconds, FloatPourIntensity intensity)
    {
        if (state != State.Preparing || layeredDrinkState == null || !layeredDrinkState.HasActiveLayer)
            return 0f;

        float safeDuration = Mathf.Max(0f, durationSeconds);
        if (safeDuration <= 0f)
            return 0f;

        BartendingLayer activeLayer = layeredDrinkState.ActiveLayer;
        activePourIntensity = intensity;
        float rate = GetLayerPourRate(activeLayer.OrderIndex, intensity);
        float transferredMl = ingredientShelf.AddLiquidMl(activeLayer.Ingredient, rate * safeDuration);
        if (transferredMl <= 0f)
            return 0f;

        RecordAcceptedLayer(transferredMl, safeDuration, intensity);
        return transferredMl;
    }

    private void RecordAcceptedLayer(float transferredMl, float safeDuration, FloatPourIntensity intensity)
    {
        if (layeredDrinkState == null || !layeredDrinkState.HasActiveLayer || transferredMl <= 0f) return;
        var activeLayer = layeredDrinkState.ActiveLayer;
        layeredDrinkState.AddToActiveLayer(transferredMl);
        activeLayerDuration += safeDuration;
        activeLayerTransferredMl += transferredMl;
        if (activeLayer.OrderIndex > 0 && intensity == FloatPourIntensity.Aggressive)
            activeLayerAggressiveDuration += safeDuration;

        UpdateLayerVisuals();
        UpdateProgressText();
    }

    public void SetMeasuredPourIntensity(FloatPourIntensity intensity) => measuredPourIntensity = intensity;

    /// <summary>
    /// Ends one addition and permanently records its single adjacent boundary
    /// result. A later clean layer cannot overwrite a prior damaged boundary.
    /// </summary>
    public bool EndActiveLayerPour()
    {
        if (state != State.Preparing || layeredDrinkState == null || !layeredDrinkState.HasActiveLayer)
            return false;

        BartendingLayer activeLayer = layeredDrinkState.ActiveLayer;
        BartendingLayerBoundaryQuality? boundaryQuality = activeLayer.OrderIndex == 0
            ? (BartendingLayerBoundaryQuality?)null
            : EvaluateBoundaryQuality();

        bool completed = layeredDrinkState.TryCompleteActiveLayer(boundaryQuality, out BartendingLayer completedLayer);
        ingredientShelf.EndSelectedPour();
        SetSpoonVisible(false);
        UpdateLayerVisuals();
        UpdateProgressText();

        if (completed)
        {
            string boundaryLabel = !boundaryQuality.HasValue ? "첫 층" : boundaryQuality.Value switch {
                BartendingLayerBoundaryQuality.Clean => "선명한 경계", BartendingLayerBoundaryQuality.Partial => "일부 섞인 경계", _ => "섞인 경계" };
            SetHint("층 " + (completedLayer.OrderIndex + 1) + " 완성: " +
                    completedLayer.Ingredient.displayName + " " + completedLayer.AmountMl.ToString("0.0") +
                    " ml / " + boundaryLabel + ".");
            Debug.Log("[Float] Layer complete index=" + completedLayer.OrderIndex +
                      " ingredient=" + completedLayer.Ingredient.ingredientId +
                      " amountMl=" + completedLayer.AmountMl.ToString("0.###") +
                      " boundary=" + boundaryLabel +
                      " avgRate=" + GetActiveAverageRate().ToString("0.###") +
                      " aggressiveRatio=" + GetActiveAggressiveRatio().ToString("0.###"));
        }

        ResetActiveLayerMetrics();
        activePourIntensity = FloatPourIntensity.Normal;
        return completed;
    }

    /// <summary>
    /// I2 compatibility: a dash enters the same Serving Glass as one discrete
    /// canonical transfer and is recorded as a small ordered layer so it cannot
    /// be double counted by composition or later visual state.
    /// </summary>
    public bool TryDispenseDashAsLayer()
    {
        if (state != State.Preparing || ingredientShelf == null || ingredientShelf.SelectedIngredient == null)
            return false;

        if (ingredientShelf.PhysicalPresentation != null)
        {
            var source = ingredientShelf.SelectedIngredient;
            return ingredientShelf.BeginPhysicalDose(RecipePreparationAction.Dash, 0f, accepted =>
            {
                if (!layeredDrinkState.HasActiveLayer) layeredDrinkState.TryBeginLayer(source);
                RecordAcceptedLayer(accepted, .01f, FloatPourIntensity.Gentle);
                EndActiveLayerPour();
            });
        }

        if (!BeginLayerPour(FloatPourIntensity.Gentle))
            return false;

        BartendingLayer activeLayer = layeredDrinkState.ActiveLayer;
        float beforeMl = GetServingGlass() != null
            ? GetServingGlass().GetLiquidAmountMl(activeLayer.Ingredient)
            : 0f;
        if (!ingredientShelf.TryDashSelectedIngredient(out int totalDashCount))
        {
            EndActiveLayerPour();
            return false;
        }

        float afterMl = GetServingGlass() != null
            ? GetServingGlass().GetLiquidAmountMl(activeLayer.Ingredient)
            : beforeMl;
        float acceptedMl = Mathf.Max(0f, afterMl - beforeMl);
        layeredDrinkState.AddToActiveLayer(acceptedMl);
        activeLayerDuration = 0.01f;
        activeLayerTransferredMl = acceptedMl;
        EndActiveLayerPour();

        SetHint(activeLayer.Ingredient.displayName + ": " + totalDashCount +
                " 대시 (총 " + afterMl.ToString("0.0") + " ml)");
        return acceptedMl > 0f;
    }

    /// <summary>
    /// Manually submits exactly the drink the player made. It deliberately does
    /// not consult an expected recipe, target layer count, or old timing gauge.
    /// </summary>
    public void CompleteFloat()
    {
        if (ingredientShelf != null && !ingredientShelf.ToolsReady) return;
        if (state != State.Preparing)
            return;

        if (layeredDrinkState != null && layeredDrinkState.HasActiveLayer)
            EndActiveLayerPour();

        if (layeredDrinkState == null || layeredDrinkState.Layers.Count == 0)
        {
            SetHint("마무리하기 전에 한 층 이상 부어 주세요.");
            return;
        }

        completedGrade = EvaluateTechnique();
        state = State.AwaitingGarnish;

        if (resultLabel != null)
            resultLabel.text = "조주: " + GuestServiceFeedbackFormatter.TechniqueLabel(completedGrade.ToString());

        ingredientShelf.SetVisible(false);
        garnishController?.BeginSelection();
        SetPrimaryAction(ServeFloat, "내어주기", true);
        SetHint("층 쌓기를 마쳤습니다. 가니시를 더하거나 그대로 내어주세요.");
        UpdateProgressText();

        BartendingContainerState servingGlass = GetServingGlass();
        Debug.Log("[Float] Layers ready layers=" + layeredDrinkState.Layers.Count +
                  " boundaries=" + layeredDrinkState.Boundaries.Count +
                  " grade=" + completedGrade +
                  " servingMl=" + (servingGlass != null ? servingGlass.CurrentLiquidVolumeMl : 0f).ToString("0.###"));
    }

    /// <summary>
    /// Final manual confirmation after optional I8 garnish selection. Existing
    /// ordered layer and boundary state remains untouched by this action.
    /// </summary>
    public void ServeFloat()
    {
        if (garnishController != null && garnishController.IsPlacing) return;
        if (ingredientShelf != null && !ingredientShelf.ToolsReady) return;
        if (state != State.AwaitingGarnish)
        {
            Debug.LogWarning("[Float] Serve ignored. state=" + state);
            return;
        }

        BrewComposition composition = ingredientShelf.BuildServingGlassComposition();
        MiniGameResult result = CreateTechniqueResult(composition);
        result.garnish = ingredientShelf.ServingGlass != null
            ? ingredientShelf.ServingGlass.Garnish
            : null;
        BartendingPreparationResultFactory.Apply(
            ref result,
            expectedRecipe,
            ingredientShelf.PreparationTrace);
        state = State.Result;
        garnishController?.EndSelection();

        SetPrimaryAction(null, string.Empty, false);
        SetHint("층 쌓기를 마쳤습니다.");
        UpdateProgressText();

        Debug.Log("[Float] Serve garnish=" +
                  (result.garnish != null ? result.garnish.DisplayName + "/" + result.garnish.Placement : "none"));
        onDone?.Invoke(result);
    }

    public bool TryApplyGarnish(BartendingGarnishId garnishId, BartendingGarnishPlacement placement)
    {
        return state == State.AwaitingGarnish &&
               garnishController != null &&
               garnishController.TryApplyGarnish(garnishId, placement);
    }

    public bool ClearGarnish()
    {
        return state == State.AwaitingGarnish &&
               garnishController != null &&
               garnishController.ClearGarnish();
    }

    private void UpdatePreparationInput()
    {
        SetMeasuredPourIntensity(ResolveInputIntensity());
        if (ingredientShelf != null && ingredientShelf.HandlePhysicalTools(firstLayerPourRateMlPerSecond)) return;
        if (ingredientShelf == null || !ingredientShelf.isActiveAndEnabled)
        {
            EndActiveLayerPour();
            return;
        }

        if (SurlhwaModalSurface.GetKeyDown(KeyCode.I))
        {
            TryAddIceToServingGlass();
            return;
        }

        if (SurlhwaModalSurface.GetKeyDown(KeyCode.D))
        {
            TryDispenseDashAsLayer();
            return;
        }

        FloatPourIntensity intensity = ResolveInputIntensity();
        if (layeredDrinkState.HasActiveLayer && layeredDrinkState.ActiveLayer.Ingredient != ingredientShelf.SelectedIngredient)
            EndActiveLayerPour();
        if (SurlhwaModalSurface.GetKeyDown(KeyCode.Space))
            BeginLayerPour(intensity);

        if (SurlhwaModalSurface.GetKey(KeyCode.Space) && !layeredDrinkState.HasActiveLayer)
            BeginLayerPour(intensity);
        if (SurlhwaModalSurface.GetKey(KeyCode.Space) && ingredientShelf.PreparePhysicalPour())
            PourActiveLayerForDuration(Time.deltaTime, intensity);

        if (SurlhwaModalSurface.GetKeyUp(KeyCode.Space))
            EndActiveLayerPour();
    }

    private bool TryAddIceToServingGlass()
    {
        if (state != State.Preparing || ingredientShelf == null)
            return false;

        if (!ingredientShelf.TryAddIceToActiveTarget())
        {
            SetHint("서빙 잔에 이미 얼음이 있습니다.");
            return false;
        }

        SetHint("서빙 잔에 얼음을 넣었습니다.");
        UpdateProgressText();
        Debug.Log("[Container] AddIce target=ServingGlass");
        return true;
    }

    private FloatPourIntensity ResolveInputIntensity()
    {
        if (SurlhwaModalSurface.GetKey(KeyCode.LeftControl) || SurlhwaModalSurface.GetKey(KeyCode.RightControl))
            return FloatPourIntensity.Aggressive;
        if (SurlhwaModalSurface.GetKey(KeyCode.LeftShift) || SurlhwaModalSurface.GetKey(KeyCode.RightShift))
            return FloatPourIntensity.Gentle;
        return FloatPourIntensity.Normal;
    }

    private float GetLayerPourRate(int layerIndex, FloatPourIntensity intensity)
    {
        if (layerIndex <= 0)
            return Mathf.Max(0f, firstLayerPourRateMlPerSecond);

        switch (intensity)
        {
            case FloatPourIntensity.Gentle:
                return Mathf.Max(0f, gentleLayerPourRateMlPerSecond);
            case FloatPourIntensity.Aggressive:
                return Mathf.Max(0f, aggressiveLayerPourRateMlPerSecond);
            default:
                return Mathf.Max(0f, normalLayerPourRateMlPerSecond);
        }
    }

    private BartendingLayerBoundaryQuality EvaluateBoundaryQuality()
    {
        float averageRate = GetActiveAverageRate();
        float aggressiveRatio = GetActiveAggressiveRatio();

        if (averageRate <= cleanMaximumAverageRateMlPerSecond &&
            aggressiveRatio <= cleanMaximumAggressiveRatio)
            return BartendingLayerBoundaryQuality.Clean;
        if (averageRate <= partialMaximumAverageRateMlPerSecond &&
            aggressiveRatio <= partialMaximumAggressiveRatio)
            return BartendingLayerBoundaryQuality.Partial;
        return BartendingLayerBoundaryQuality.Mixed;
    }

    private float GetActiveAverageRate()
    {
        return activeLayerDuration > 0f ? activeLayerTransferredMl / activeLayerDuration : 0f;
    }

    private float GetActiveAggressiveRatio()
    {
        return activeLayerDuration > 0f
            ? Mathf.Clamp01(activeLayerAggressiveDuration / activeLayerDuration)
            : 0f;
    }

    private FloatGrade EvaluateTechnique()
    {
        if (layeredDrinkState == null || layeredDrinkState.Boundaries.Count == 0)
            return FloatGrade.Good;

        bool hasPartial = false;
        foreach (BartendingLayerBoundary boundary in layeredDrinkState.Boundaries)
        {
            if (boundary.Quality == BartendingLayerBoundaryQuality.Mixed)
                return FloatGrade.Poor;
            if (boundary.Quality == BartendingLayerBoundaryQuality.Partial)
                hasPartial = true;
        }

        return hasPartial ? FloatGrade.Acceptable : FloatGrade.Good;
    }

    private MiniGameResult CreateTechniqueResult(BrewComposition composition)
    {
        switch (completedGrade)
        {
            case FloatGrade.Good:
                return new MiniGameResult
                {
                    success = true,
                    repDelta = 1,
                    grade = "Good",
                    brewedComposition = composition
                };
            case FloatGrade.Acceptable:
                return new MiniGameResult
                {
                    success = true,
                    repDelta = 0,
                    grade = "Acceptable",
                    brewedComposition = composition
                };
            default:
                return new MiniGameResult
                {
                    success = false,
                    repDelta = 0,
                    grade = "Poor",
                    brewedComposition = composition
                };
        }
    }

    private BartendingContainerState GetServingGlass()
    {
        return ingredientShelf != null ? ingredientShelf.ServingGlass : null;
    }

    private void ResetActiveLayerMetrics()
    {
        activeLayerDuration = 0f;
        activeLayerAggressiveDuration = 0f;
        activeLayerTransferredMl = 0f;
    }

    private void ResetLayerVisuals()
    {
        foreach (GameObject visual in runtimeLayerVisuals)
            if (visual != null)
            {
                visual.SetActive(false);
                Destroy(visual);
            }
        foreach (GameObject visual in boundaryVisuals)
            if (visual != null)
            {
                visual.SetActive(false);
                Destroy(visual);
            }

        runtimeLayerVisuals.Clear();
        boundaryVisuals.Clear();
        layerVisuals.Clear();

        if (baseLiquid != null)
            baseLiquid.gameObject.SetActive(false);
        if (topLiquid != null)
            topLiquid.gameObject.SetActive(false);
    }

    private void UpdateLayerVisuals()
    {
        if (layeredDrinkState == null || liquidArea == null)
            return;

        EnsureLayerVisualCount(layeredDrinkState.Layers.Count);
        EnsureBoundaryVisualCount(layeredDrinkState.Boundaries.Count);

        float totalMl = 0f;
        foreach (BartendingLayer layer in layeredDrinkState.Layers)
            totalMl += Mathf.Max(0f, layer.AmountMl);

        float height = Mathf.Max(1f, liquidArea.rect.height);
        float currentY = 0f;
        for (int index = 0; index < layeredDrinkState.Layers.Count && index < layerVisuals.Count; index++)
        {
            BartendingLayer layer = layeredDrinkState.Layers[index];
            float layerHeight = height * (Mathf.Max(0f, layer.AmountMl) / Mathf.Max(visualVolumeMl, totalMl));
            LayoutLiquidVisual(layerVisuals[index], currentY, layerHeight,
                layer.Ingredient != null ? layer.Ingredient.liquidColor : Color.clear);
            currentY += layerHeight;
        }

        for (int index = 0; index < layeredDrinkState.Boundaries.Count && index < boundaryVisuals.Count; index++)
        {
            BartendingLayerBoundary boundary = layeredDrinkState.Boundaries[index];
            float boundaryY = GetLayerSurfaceY(boundary.LowerLayerIndex, Mathf.Max(visualVolumeMl, totalMl), height);
            LayoutBoundaryVisual(boundaryVisuals[index].GetComponent<RectTransform>(), boundaryY, boundary.Quality);
        }

        if (physicalSurface != null) physicalSurface.anchoredPosition = new Vector2(0f, currentY);
        UpdateSpoonPosition(currentY);
    }

    private void EnsureLayerVisualCount(int requiredCount)
    {
        while (layerVisuals.Count < requiredCount)
        {
            int index = layerVisuals.Count;
            RectTransform visual = null;

            if (index == 0 && baseLiquid != null)
                visual = baseLiquid;
            else if (index == 1 && topLiquid != null)
                visual = topLiquid;
            else
            {
                RectTransform template = topLiquid != null ? topLiquid : baseLiquid;
                if (template != null)
                {
                    GameObject clone = Instantiate(template.gameObject, liquidArea);
                    clone.name = "FloatLayer_" + index;
                    runtimeLayerVisuals.Add(clone);
                    visual = clone.GetComponent<RectTransform>();
                }
            }

            if (visual == null)
                return;

            visual.gameObject.SetActive(true);
            layerVisuals.Add(visual);
        }

        while (layerVisuals.Count > requiredCount)
        {
            int index = layerVisuals.Count - 1;
            if (layerVisuals[index] != null)
                layerVisuals[index].gameObject.SetActive(false);
            layerVisuals.RemoveAt(index);
        }
    }

    private void EnsureBoundaryVisualCount(int requiredCount)
    {
        while (boundaryVisuals.Count < requiredCount)
        {
            GameObject boundary = new GameObject("FloatBoundary_" + boundaryVisuals.Count, typeof(RectTransform), typeof(Image));
            boundary.transform.SetParent(liquidArea, false);
            boundary.GetComponent<Image>().raycastTarget = false;
            boundaryVisuals.Add(boundary);
        }

        while (boundaryVisuals.Count > requiredCount)
        {
            int index = boundaryVisuals.Count - 1;
            if (boundaryVisuals[index] != null)
                Destroy(boundaryVisuals[index]);
            boundaryVisuals.RemoveAt(index);
        }
    }

    private static void LayoutLiquidVisual(RectTransform visual, float y, float height, Color color)
    {
        if (visual == null)
            return;

        visual.anchorMin = Vector2.zero;
        visual.anchorMax = new Vector2(1f, 0f);
        visual.pivot = new Vector2(0.5f, 0f);
        visual.anchoredPosition = new Vector2(0f, y);
        visual.sizeDelta = new Vector2(0f, Mathf.Max(0f, height));

        Image image = visual.GetComponent<Image>();
        if (image != null)
            image.color = color;
    }

    private static void LayoutBoundaryVisual(
        RectTransform visual,
        float y,
        BartendingLayerBoundaryQuality quality)
    {
        if (visual == null)
            return;

        float thickness;
        Color color;
        switch (quality)
        {
            case BartendingLayerBoundaryQuality.Clean:
                thickness = 2f;
                color = new Color(.93f, .9f, .8f, .5f);
                break;
            case BartendingLayerBoundaryQuality.Partial:
                thickness = 5f;
                color = new Color(.67f, .64f, .57f, .55f);
                break;
            default:
                thickness = 9f;
                color = new Color(.4f, .42f, .41f, .6f);
                break;
        }

        visual.anchorMin = Vector2.zero;
        visual.anchorMax = new Vector2(1f, 0f);
        visual.pivot = new Vector2(0.5f, 0.5f);
        visual.anchoredPosition = new Vector2(0f, y);
        visual.sizeDelta = new Vector2(0f, thickness);
        visual.GetComponent<Image>().color = color;
    }

    private float GetLayerSurfaceY(int layerIndex, float totalMl, float height)
    {
        if (layeredDrinkState == null || totalMl <= 0f)
            return 0f;

        float lowerMl = 0f;
        for (int i = 0; i <= layerIndex && i < layeredDrinkState.Layers.Count; i++)
            lowerMl += Mathf.Max(0f, layeredDrinkState.Layers[i].AmountMl);
        return height * Mathf.Clamp01(lowerMl / totalMl);
    }

    private void SetSpoonVisible(bool visible)
    {
        if (spoon != null)
            spoon.gameObject.SetActive(visible);
    }

    private void UpdateSpoonPosition(float currentSurfaceY)
    {
        if (spoon == null || !spoon.gameObject.activeSelf)
            return;

        if (physicalSurface != null)
            spoon.position = physicalSurface.position + spoon.parent.TransformVector(Vector3.up * spoonOffsetY);
        else
            spoon.anchoredPosition = spoonStartPosition + new Vector2(0f, currentSurfaceY + spoonOffsetY);
    }

    private void UpdateProgressText()
    {
        if (progressText == null)
            return;

        BartendingContainerState servingGlass = GetServingGlass();
        int layerCount = layeredDrinkState != null ? layeredDrinkState.Layers.Count : 0;
        int boundaryCount = layeredDrinkState != null ? layeredDrinkState.Boundaries.Count : 0;
        string active = layeredDrinkState != null && layeredDrinkState.HasActiveLayer
            ? " / 층 " + (layeredDrinkState.ActiveLayer.OrderIndex + 1) +
              " " + activeLayerTransferredMl.ToString("0.0") + " ml" +
              " / 흐름 " + DescribeFlow(activePourIntensity)
            : string.Empty;

        progressText.text = "서빙 잔 " +
            (servingGlass != null ? servingGlass.CurrentLiquidVolumeMl : 0f).ToString("0.0") +
            " ml / 층 " + layerCount + " / 경계 " + boundaryCount + active;
    }

    private void SetPrimaryAction(UnityEngine.Events.UnityAction action, string label, bool visible)
    {
        if (startButton == null)
            return;

        startButton.onClick.RemoveAllListeners();
        if (action != null)
            startButton.onClick.AddListener(action);

        startButton.gameObject.SetActive(visible);
        TMP_Text buttonText = startButton.GetComponentInChildren<TMP_Text>(true);
        if (buttonText != null)
            buttonText.text = label;
    }

    private void SetHint(string message)
    {
        if (hintText != null)
            hintText.text = message;
        if (startHintText != null && startHintText.gameObject.activeSelf)
            startHintText.text = message;
    }

    private static string DescribeFlow(FloatPourIntensity intensity)
    {
        switch (intensity)
        {
            case FloatPourIntensity.Gentle:
                return "천천히 [Shift]";
            case FloatPourIntensity.Aggressive:
                return "세게 [Ctrl]";
            default:
                return "보통";
        }
    }

    private string GetDisplayTitle()
    {
        return !string.IsNullOrEmpty(requestDisplayTitle) ? requestDisplayTitle : defaultDisplayName;
    }

    private void OnDisable()
    {
        if (layeredDrinkState != null && layeredDrinkState.HasActiveLayer)
            EndActiveLayerPour();
        ingredientShelf?.EndSelectedPour();
        SetSpoonVisible(false);
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
    }

    private void AutoBindIfMissing()
    {
        if (liquidArea == null)
            liquidArea = transform.Find("UI/LiquidArea")?.GetComponent<RectTransform>();
        if (baseLiquid == null)
            baseLiquid = transform.Find("UI/LiquidArea/BaseLiquid")?.GetComponent<RectTransform>();
        if (topLiquid == null)
            topLiquid = transform.Find("UI/LiquidArea/TopLiquid")?.GetComponent<RectTransform>();
        if (targetZone == null)
            targetZone = transform.Find("UI/LiquidArea/TargetZone")?.GetComponent<RectTransform>();
        if (spoon == null)
            spoon = transform.Find("UI/Spoon")?.GetComponent<RectTransform>();
        if (spoon == null)
            spoon = transform.Find("UI/SpoonImage")?.GetComponent<RectTransform>();
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
