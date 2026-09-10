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
    IIngredientAvailabilityContextReceiver
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
        ConfigureGarnishController();

        state = State.Preparing;
        layeredDrinkState = new BartendingLayeredDrinkState();
        completedGrade = FloatGrade.Poor;
        ResetActiveLayerMetrics();
        activePourIntensity = FloatPourIntensity.Normal;
        ResetLayerVisuals();

        if (spoon != null)
        {
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
            startHintText.gameObject.SetActive(true);
            startHintText.text = "Build layers in order. Hold SPACE; Shift is gentle and Ctrl is aggressive.";
        }

        SetPrimaryAction(CompleteFloat, "Complete Float", true);
        SetHint("Select an ingredient. Hold SPACE to pour Layer 0; later layers use the spoon-assisted flow.");
        UpdateLayerVisuals();
        UpdateProgressText();

        Debug.Log("[Float] Preparation ready / Target=ServingGlass / OrderedLayers=enabled");
    }

    private void Update()
    {
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
            SetHint("Select an ingredient before building a layer.");
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
            ? "Pouring bottom layer: " + ingredient.displayName + ". Flow: " + DescribeFlow(intensity) + "."
            : "Pouring Layer " + (layeredDrinkState.ActiveLayer.OrderIndex + 1) +
              ": " + DescribeFlow(intensity) + " spoon-assisted flow.");
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

        layeredDrinkState.AddToActiveLayer(transferredMl);
        activeLayerDuration += safeDuration;
        activeLayerTransferredMl += transferredMl;
        if (activeLayer.OrderIndex > 0 && intensity == FloatPourIntensity.Aggressive)
            activeLayerAggressiveDuration += safeDuration;

        UpdateLayerVisuals();
        UpdateProgressText();
        return transferredMl;
    }

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
            string boundaryLabel = boundaryQuality.HasValue ? boundaryQuality.Value.ToString() : "Base";
            SetHint("Layer " + (completedLayer.OrderIndex + 1) + " complete: " +
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
                " dash (" + afterMl.ToString("0.0") + " ml total).");
        return acceptedMl > 0f;
    }

    /// <summary>
    /// Manually submits exactly the drink the player made. It deliberately does
    /// not consult an expected recipe, target layer count, or old timing gauge.
    /// </summary>
    public void CompleteFloat()
    {
        if (state != State.Preparing)
            return;

        if (layeredDrinkState != null && layeredDrinkState.HasActiveLayer)
            EndActiveLayerPour();

        if (layeredDrinkState == null || layeredDrinkState.Layers.Count == 0)
        {
            SetHint("Add at least one actual layer before completing Float.");
            return;
        }

        completedGrade = EvaluateTechnique();
        state = State.AwaitingGarnish;

        if (resultLabel != null)
            resultLabel.text = completedGrade.ToString();

        ingredientShelf.SetVisible(false);
        garnishController?.BeginSelection();
        SetPrimaryAction(ServeFloat, "Serve Float", true);
        SetHint("Float layers complete. Garnish is optional; choose one or serve the drink as made.");
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
            null,
            ingredientShelf.PreparationTrace);
        state = State.Result;
        garnishController?.EndSelection();

        SetPrimaryAction(null, string.Empty, false);
        SetHint("Float complete. The Serving Glass composition is being evaluated.");
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
        if (ingredientShelf == null || !ingredientShelf.isActiveAndEnabled)
        {
            EndActiveLayerPour();
            return;
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            TryAddIceToServingGlass();
            return;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            TryDispenseDashAsLayer();
            return;
        }

        FloatPourIntensity intensity = ResolveInputIntensity();
        if (Input.GetKeyDown(KeyCode.Space))
            BeginLayerPour(intensity);

        if (Input.GetKey(KeyCode.Space))
            PourActiveLayerForDuration(Time.deltaTime, intensity);

        if (Input.GetKeyUp(KeyCode.Space))
            EndActiveLayerPour();
    }

    private bool TryAddIceToServingGlass()
    {
        if (state != State.Preparing || ingredientShelf == null)
            return false;

        if (!ingredientShelf.TryAddIceToActiveTarget())
        {
            SetHint("The Serving Glass already has ice.");
            return false;
        }

        SetHint("Ice added to the Serving Glass.");
        UpdateProgressText();
        Debug.Log("[Container] AddIce target=ServingGlass");
        return true;
    }

    private FloatPourIntensity ResolveInputIntensity()
    {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            return FloatPourIntensity.Aggressive;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
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
            float layerHeight = totalMl > 0f ? height * (Mathf.Max(0f, layer.AmountMl) / totalMl) : 0f;
            LayoutLiquidVisual(layerVisuals[index], currentY, layerHeight, GetLayerColor(index));
            currentY += layerHeight;
        }

        for (int index = 0; index < layeredDrinkState.Boundaries.Count && index < boundaryVisuals.Count; index++)
        {
            BartendingLayerBoundary boundary = layeredDrinkState.Boundaries[index];
            float boundaryY = GetLayerSurfaceY(boundary.LowerLayerIndex, totalMl, height);
            LayoutBoundaryVisual(boundaryVisuals[index].GetComponent<RectTransform>(), boundaryY, boundary.Quality);
        }

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
                color = new Color(1f, 1f, 1f, 0.9f);
                break;
            case BartendingLayerBoundaryQuality.Partial:
                thickness = 5f;
                color = new Color(1f, 0.78f, 0.38f, 0.9f);
                break;
            default:
                thickness = 9f;
                color = new Color(0.48f, 0.25f, 0.4f, 0.9f);
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

        spoon.anchoredPosition = spoonStartPosition + new Vector2(0f, currentSurfaceY + spoonOffsetY);
    }

    private static Color GetLayerColor(int index)
    {
        Color[] colors =
        {
            new Color(0.35f, 0.18f, 0.08f, 0.95f),
            new Color(0.96f, 0.88f, 0.67f, 0.95f),
            new Color(0.46f, 0.24f, 0.52f, 0.95f),
            new Color(0.2f, 0.52f, 0.7f, 0.95f)
        };
        return colors[Mathf.Abs(index) % colors.Length];
    }

    private void UpdateProgressText()
    {
        if (progressText == null)
            return;

        BartendingContainerState servingGlass = GetServingGlass();
        int layerCount = layeredDrinkState != null ? layeredDrinkState.Layers.Count : 0;
        int boundaryCount = layeredDrinkState != null ? layeredDrinkState.Boundaries.Count : 0;
        string active = layeredDrinkState != null && layeredDrinkState.HasActiveLayer
            ? " / Layer " + (layeredDrinkState.ActiveLayer.OrderIndex + 1) +
              " " + activeLayerTransferredMl.ToString("0.0") + " ml" +
              " / Flow " + DescribeFlow(activePourIntensity)
            : string.Empty;

        progressText.text = "Serving Glass " +
            (servingGlass != null ? servingGlass.CurrentLiquidVolumeMl : 0f).ToString("0.0") +
            " ml / Layers " + layerCount + " / Boundaries " + boundaryCount + active;
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
                return "Gentle [Shift]";
            case FloatPourIntensity.Aggressive:
                return "Aggressive [Ctrl]";
            default:
                return "Normal";
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
