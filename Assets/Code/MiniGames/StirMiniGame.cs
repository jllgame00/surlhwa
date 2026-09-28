using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// I5's direct Stir route. Actual liquid stays in the Mixing Glass until the
/// player explicitly strains it into the Serving Glass for result submission.
/// </summary>
public class StirMiniGame : MonoBehaviour, IMiniGame, IRecipePreparationExpectationReceiver,
    IIngredientAvailabilityContextReceiver
{
    private enum State
    {
        Preparing,
        Stirring,
        AwaitingStrain,
        AwaitingGarnish,
        Result
    }

    private enum StirGrade
    {
        Good,
        Acceptable,
        Poor
    }

    [Header("Text (UI)")]
    [SerializeField] private TMP_Text resultLabel;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text hintText;

    [Header("Primary Action UI")]
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text startHintText;

    [Header("Spoon (UI)")]
    [SerializeField] private RectTransform cup;
    [SerializeField] private RectTransform spoon;
    [SerializeField] private RectTransform spoonInCup;
    [SerializeField] private float spoonOrbitRadius = 16f;
    [SerializeField] private float spoonOrbitYScale = 0.65f;
    [SerializeField] private float spoonBaseAngleDeg = 90f;
    [SerializeField] private float spoonShaftTiltDegrees = 8f;
    [SerializeField, Min(0.01f)] private float spoonVisualSmoothingSeconds = 0.08f;

    [Header("Direct Stir Tuning")]
    [SerializeField] private float minimumFinishDurationSeconds = 3f;
    [SerializeField] private float targetStirDurationSeconds = 5f;
    [SerializeField] private float goodMinimumRevolutions = 2f;
    [SerializeField] private float acceptableMinimumRevolutions = 0.75f;
    [SerializeField] private float minimumGoodAngularSpeedDegPerSecond = 60f;
    [SerializeField] private float maximumGoodAngularSpeedDegPerSecond = 540f;
    [SerializeField] private float goodConsistencyThreshold = 0.7f;
    [SerializeField] private float acceptableConsistencyThreshold = 0.35f;
    [SerializeField] private float minimumMouseRadiusPixels = 36f;
    [SerializeField] private float maximumAngularJumpDeg = 110f;

    [Header("Ingredient Shelf")]
    [SerializeField] private IngredientShelfController ingredientShelf;

    [Header("Pour Core")]
    [SerializeField] private float pourRateMlPerSecond = 30f;

    private State state = State.Preparing;
    private Action<MiniGameResult> onDone;
    private StirTechniqueTracker techniqueTracker;
    private StirGrade completedGrade;
    private bool isPouring;
    private bool hasSpoonVisualTarget;
    private float spoonVisualAngleDeg;
    private float spoonVisualAngleVelocity;
    private float targetSpoonVisualAngleDeg;
    private ServingGlassGarnishController garnishController;
    private CocktailRecipeData expectedRecipe;
    private IngredientAvailabilityContext ingredientAvailabilityContext;

    public float TotalStirRotationDegrees => techniqueTracker != null
        ? techniqueTracker.TotalRotationDegrees
        : 0f;

    public float StirRevolutions => techniqueTracker != null
        ? techniqueTracker.Revolutions
        : 0f;

    public float AverageStirAngularSpeedDegPerSecond => techniqueTracker != null
        ? techniqueTracker.AverageAngularSpeedDegPerSecond
        : 0f;

    public float StirConsistency => techniqueTracker != null
        ? techniqueTracker.Consistency
        : 0f;

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
        SetLegacyGaugeVisible(false);

        if (ingredientShelf == null)
            ingredientShelf = GetComponentInChildren<IngredientShelfController>(true);

        if (ingredientShelf == null)
            throw new InvalidOperationException("[StirMiniGame] IngredientShelfController is required.");

        // Stir owns direct preparation in the Mixing Glass. The I3 preparation
        // state still creates the Serving Glass for the later explicit Strain.
        ingredientShelf.Initialize(
            MiniGameType.Stir,
            BartendingContainerType.MixingGlass,
            ingredientAvailabilityContext);
        ConfigureGarnishController();

        state = State.Preparing;
        techniqueTracker = new StirTechniqueTracker(maximumAngularJumpDeg);
        isPouring = false;

        ResetSpoonPose();

        if (resultLabel != null)
        {
            resultLabel.gameObject.SetActive(true);
            resultLabel.text = string.Empty;
        }

        if (startHintText != null)
        {
            startHintText.gameObject.SetActive(hintText == null);
            startHintText.text = "믹싱 글라스에 재료와 얼음을 넣고 젓기를 시작하세요.";
        }

        SetPrimaryAction(BeginStir, "젓기 시작", true);
        UpdateProgressText();
        SetHint("재료를 고르고 Space를 눌러 믹싱 글라스에 붓습니다. I: 얼음");

        Debug.Log("[Stir] Preparation ready / Target=MixingGlass / Ice=required");
    }

    private void Update()
    {
        if (RecipeGuidePanelController.IsTechniqueReadStateActive || DemoAudioController.IsSettingsOpen)
            return;

        switch (state)
        {
            case State.Preparing:
                UpdatePreparationInput();
                break;

            case State.Stirring:
                UpdateMouseStirInput();
                UpdateSpoonVisual(Time.unscaledDeltaTime);
                break;
        }
    }

    /// <summary>
    /// Begins the physical technique only when the player has created a
    /// stirrable state. It does not validate recipe identity or quantities.
    /// </summary>
    public void BeginStir()
    {
        if (ingredientShelf != null && !ingredientShelf.ToolsReady) return;
        if (state != State.Preparing)
        {
            Debug.LogWarning($"[Stir] Begin ignored. state={state}");
            return;
        }

        BartendingContainerState mixingGlass = GetMixingGlass();
        if (mixingGlass == null || mixingGlass.CurrentLiquidVolumeMl <= 0f)
        {
            SetHint("믹싱 글라스에 재료를 먼저 부어 주세요.");
            return;
        }

        if (!mixingGlass.HasIce)
        {
            SetHint("젓기 전에 믹싱 글라스에 얼음을 넣어 주세요.");
            return;
        }

        StopDirectPour("Stir started");
        state = State.Stirring;
        if (spoon != null) spoon.gameObject.SetActive(true);
        techniqueTracker.Reset();

        ingredientShelf.SetVisible(false);

        SetPrimaryAction(FinishStir, "젓기 마치기", true);
        SetHint("잔 중앙을 피해서 스푼 둘레로 마우스를 일정하게 돌려 주세요.");
        UpdateProgressText();

        Debug.Log($"[Stir] Begin mixingGlassMl={mixingGlass.CurrentLiquidVolumeMl:0.###} ice={mixingGlass.HasIce}");
    }

    /// <summary>
    /// The narrow angular-input seam used by mouse input and deterministic
    /// validation. It is not a generalized input framework.
    /// </summary>
    public bool TryRecordStirAngle(float angleDeg, float deltaTime)
    {
        if (state != State.Stirring || techniqueTracker == null)
            return false;

        bool acceptedMotion = techniqueTracker.RecordAngle(angleDeg, deltaTime);
        if ((acceptedMotion || !hasSpoonVisualTarget) && !float.IsNaN(angleDeg) && !float.IsInfinity(angleDeg))
        {
            // Visual following consumes the same valid cursor angle as the
            // tracker, but never writes rotation back into technique truth.
            // A rejected/idle sample cannot create score through smoothing.
            targetSpoonVisualAngleDeg = angleDeg;
            hasSpoonVisualTarget = true;
        }

        UpdateProgressText();
        return acceptedMotion;
    }

    /// <summary>
    /// Ends the active technique after a short interaction window. Poor input
    /// remains submit-capable; it is represented distinctly from recipe match.
    /// </summary>
    public void FinishStir()
    {
        if (state != State.Stirring)
        {
            Debug.LogWarning($"[Stir] Finish ignored. state={state}");
            return;
        }

        if (techniqueTracker.ElapsedSeconds < minimumFinishDurationSeconds)
        {
            float remaining = minimumFinishDurationSeconds - techniqueTracker.ElapsedSeconds;
            SetHint($"{remaining:0.0}초만 더 저은 뒤 마쳐 주세요.");
            return;
        }

        completedGrade = EvaluateTechnique();
        ingredientShelf.RecordTechniqueAction(
            RecipePreparationAction.Stir,
            BartendingContainerType.MixingGlass);
        state = State.AwaitingStrain;
        if (spoon != null) spoon.gameObject.SetActive(false);

        if (resultLabel != null)
            resultLabel.text = completedGrade.ToString();

        SetPrimaryAction(StrainAndSubmit, "걸러 따르기", true);
        SetHint("젓기를 마쳤습니다. 걸러 따르기를 눌러 서빙 잔으로 옮겨 주세요.");
        UpdateProgressText();

        Debug.Log(
            $"[Stir] Complete rotations={StirRevolutions:0.##} " +
            $"avgSpeed={AverageStirAngularSpeedDegPerSecond:0.##} " +
            $"consistency={StirConsistency:0.##} grade={completedGrade}");
    }

    /// <summary>
    /// Reuses I3's non-scored Strain operation. The Serving Glass snapshot is
    /// the only composition submitted to the existing result boundary.
    /// </summary>
    public void StrainAndSubmit()
    {
        if (state != State.AwaitingStrain)
        {
            Debug.LogWarning($"[Stir] Strain ignored. state={state}");
            return;
        }

        if (ingredientShelf != null && ingredientShelf.PhysicalPresentation != null)
        {
            ingredientShelf.BeginPhysicalContainerTransfer(RecipePreparationAction.Strain, FinishPhysicalStrain);
            return;
        }
        if (ingredientShelf == null ||
            !ingredientShelf.TryStrainActiveTargetToServingGlass(out float transferredMl))
        {
            SetHint("걸러 따르려면 믹싱 글라스에 액체가 있어야 합니다.");
            return;
        }

        FinishPhysicalStrain(transferredMl);
    }

    private void FinishPhysicalStrain(float transferredMl)
    {
        state = State.AwaitingGarnish;

        ingredientShelf.SetVisible(false);
        garnishController?.BeginSelection();
        SetPrimaryAction(ServeStir, "내어주기", true);
        SetHint(garnishController != null && garnishController.IsExpectedGarnishRequired
            ? "걸러 따랐습니다. 필요한 가니시를 더해 주세요."
            : "걸러 따랐습니다. 가니시를 더하거나 그대로 내어주세요.");
        UpdateProgressText();

        BartendingContainerState mixingGlass = GetMixingGlass();
        Debug.Log(
            $"[Stir] Strain MixingGlass -> ServingGlass volumeMl={transferredMl:0.###} " +
            $"iceRetained={(mixingGlass != null && mixingGlass.HasIce)}");

    }

    /// <summary>
    /// Final manual confirmation after the optional I8 garnish step. The
    /// technique result remains independent from the garnish state.
    /// </summary>
    public void ServeStir()
    {
        if (ingredientShelf != null && !ingredientShelf.ToolsReady) return;
        if (state != State.AwaitingGarnish)
        {
            Debug.LogWarning($"[Stir] Serve ignored. state={state}");
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
        SetHint("조주를 마쳤습니다.");
        UpdateProgressText();

        Debug.Log($"[Stir] Serve garnish={(result.garnish != null ? result.garnish.DisplayName + "/" + result.garnish.Placement : "none")}");
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

    /// <summary>
    /// Explicit I5 preparation action. Ice belongs to the current Mixing Glass
    /// and is never silently inserted by BeginStir.
    /// </summary>
    public bool AddIceToMixingGlass()
    {
        if (state != State.Preparing || ingredientShelf == null)
            return false;

        if (!ingredientShelf.TryAddIceToActiveTarget())
        {
            SetHint("믹싱 글라스에 이미 얼음이 있습니다.");
            return false;
        }

        SetHint("믹싱 글라스에 얼음을 넣었습니다. 재료를 더하거나 젓기를 시작하세요.");
        UpdateProgressText();
        Debug.Log("[Container] AddIce target=MixingGlass");
        return true;
    }

    private void UpdatePreparationInput()
    {
        if (ingredientShelf != null && ingredientShelf.HandlePhysicalTools(pourRateMlPerSecond)) return;
        if (ingredientShelf == null || !ingredientShelf.isActiveAndEnabled)
        {
            StopDirectPour("Mixing Glass unavailable");
            return;
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            AddIceToMixingGlass();
            return;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            TryDispenseDash();
            return;
        }

        if (!Input.GetKey(KeyCode.Space))
        {
            StopDirectPour("input released");
            return;
        }

        IngredientData ingredient = ingredientShelf.SelectedIngredient;
        if (ingredient == null)
        {
            StopDirectPour("no ingredient selected");
            SetHint("먼저 부을 재료를 골라 주세요.");
            return;
        }

        if (!ingredientShelf.PreparePhysicalPour()) return;
        float requestedMl = Mathf.Max(0f, pourRateMlPerSecond) * Time.deltaTime;
        float transferredMl = ingredientShelf.PourSelectedIngredient(requestedMl);
        if (transferredMl <= 0f)
        {
            StopDirectPour("transfer rejected");
            return;
        }

        if (!isPouring)
        {
            isPouring = true;
            Debug.Log($"[Pour] Start ingredient={ingredient.ingredientId} target=MixingGlass rate={pourRateMlPerSecond:0.##} ml/s");
        }

        float ingredientMl = GetMixingGlass().GetLiquidAmountMl(ingredient);
        SetHint($"Pouring {ingredient.displayName}: {ingredientMl:0.0} ml in Mixing Glass.");
        UpdateProgressText();
    }

    private void UpdateMouseStirInput()
    {
        RectTransform center = GetSpoonCenter();
        if (center == null)
        {
            techniqueTracker.RecordIdle(Time.deltaTime);
            return;
        }

        Camera uiCamera = ResolveUiCamera(center);
        Vector2 centerScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, center.position);
        Vector2 cursorOffset = (Vector2)Input.mousePosition - centerScreen;

        if (cursorOffset.sqrMagnitude < minimumMouseRadiusPixels * minimumMouseRadiusPixels)
        {
            techniqueTracker.RecordIdle(Time.deltaTime);
            return;
        }

        float angleDeg = Mathf.Atan2(cursorOffset.y, cursorOffset.x) * Mathf.Rad2Deg;
        TryRecordStirAngle(angleDeg, Time.deltaTime);
    }

    private StirGrade EvaluateTechnique()
    {
        bool goodSpeed = AverageStirAngularSpeedDegPerSecond >= minimumGoodAngularSpeedDegPerSecond &&
                         AverageStirAngularSpeedDegPerSecond <= maximumGoodAngularSpeedDegPerSecond;

        if (StirRevolutions >= goodMinimumRevolutions &&
            goodSpeed &&
            StirConsistency >= goodConsistencyThreshold)
            return StirGrade.Good;

        if (StirRevolutions >= acceptableMinimumRevolutions &&
            StirConsistency >= acceptableConsistencyThreshold)
            return StirGrade.Acceptable;

        return StirGrade.Poor;
    }

    private MiniGameResult CreateTechniqueResult(BrewComposition composition)
    {
        switch (completedGrade)
        {
            case StirGrade.Good:
                return new MiniGameResult
                {
                    success = true,
                    repDelta = 1,
                    grade = "Good",
                    brewedComposition = composition
                };

            case StirGrade.Acceptable:
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

    private void TryDispenseDash()
    {
        if (ingredientShelf == null || ingredientShelf.SelectedIngredient == null)
        {
            SetHint("먼저 대시로 더할 재료를 골라 주세요.");
            return;
        }

        if (!ingredientShelf.TryDashSelectedIngredient(out int totalDashCount))
        {
            SetHint("That dash could not be added to the Mixing Glass.");
            return;
        }

        IngredientData ingredient = ingredientShelf.SelectedIngredient;
        float ingredientMl = GetMixingGlass().GetLiquidAmountMl(ingredient);
        SetHint($"{ingredient.displayName} — {totalDashCount} dash ({ingredientMl:0.0} ml).");
        UpdateProgressText();
        Debug.Log($"[Dash] Ingredient={ingredient.ingredientId} count={totalDashCount} compatibilityMl={ingredientMl:0.###}");
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
        BartendingContainerState mixingGlass = GetMixingGlass();
        float ingredientMl = ingredient != null && mixingGlass != null
            ? mixingGlass.GetLiquidAmountMl(ingredient)
            : 0f;
        float totalMl = mixingGlass != null ? mixingGlass.CurrentLiquidVolumeMl : 0f;

        Debug.Log(
            $"[Pour] Stop reason={reason} ingredient={(ingredient != null ? ingredient.ingredientId : "NULL")} " +
            $"ingredientMl={ingredientMl:0.0} totalMl={totalMl:0.0}");
    }

    private void UpdateSpoonVisual(float deltaTime)
    {
        if (!hasSpoonVisualTarget)
            return;

        float smoothing = Mathf.Max(0.01f, spoonVisualSmoothingSeconds);
        spoonVisualAngleDeg = Mathf.SmoothDampAngle(
            spoonVisualAngleDeg,
            targetSpoonVisualAngleDeg,
            ref spoonVisualAngleVelocity,
            smoothing,
            Mathf.Infinity,
            Mathf.Max(0f, deltaTime));
        UpdateSpoonPose(spoonVisualAngleDeg);
    }

    private void UpdateSpoonPose(float angleDeg)
    {
        if (spoon == null)
            return;

        RectTransform center = GetSpoonCenter();
        if (center == null)
            return;

        float radians = angleDeg * Mathf.Deg2Rad;
        // The spoon's bowl is the pivot. Its tip orbits inside the vessel;
        // the shaft stays almost upright instead of rotating through 360 degrees.
        float vesselWidth = Mathf.Abs(spoon.parent.InverseTransformVector(
            center.TransformVector(Vector3.right * center.rect.width)).x);
        float radius = Mathf.Min(spoonOrbitRadius, vesselWidth * .35f);
        Vector2 orbit = new Vector2(Mathf.Cos(radians) * radius,
            Mathf.Sin(radians) * radius * spoonOrbitYScale);
        Vector3 origin = center.TransformPoint(center.rect.center + Vector2.up * center.rect.height * .12f);
        spoon.position = origin + spoon.parent.TransformVector(orbit);
        spoon.localRotation = Quaternion.Euler(0f, 0f,
            spoonBaseAngleDeg + Mathf.Cos(radians) * spoonShaftTiltDegrees);
    }

    private void ResetSpoonPose()
    {
        hasSpoonVisualTarget = false;
        spoonVisualAngleDeg = 0f;
        spoonVisualAngleVelocity = 0f;
        targetSpoonVisualAngleDeg = 0f;

        if (spoon == null)
            return;

        spoon.pivot = new Vector2(0f, .5f);
        spoon.gameObject.SetActive(false);
        RectTransform center = GetSpoonCenter();
        if (center != null)
            spoon.anchoredPosition = center.anchoredPosition;

        spoon.localRotation = Quaternion.Euler(0f, 0f, spoonBaseAngleDeg);
    }

    private RectTransform GetSpoonCenter()
    {
        return cup != null ? cup : spoonInCup;
    }

    private static Camera ResolveUiCamera(RectTransform element)
    {
        Canvas canvas = element != null ? element.GetComponentInParent<Canvas>() : null;
        return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
    }

    private BartendingContainerState GetMixingGlass()
    {
        return ingredientShelf != null && ingredientShelf.PreparationState != null
            ? ingredientShelf.PreparationState.GetContainer(BartendingContainerType.MixingGlass)
            : null;
    }

    private void UpdateProgressText()
    {
        if (progressText == null)
            return;

        BartendingContainerState mixingGlass = GetMixingGlass();
        BartendingContainerState servingGlass = ingredientShelf != null ? ingredientShelf.ServingGlass : null;

        switch (state)
        {
            case State.Preparing:
                progressText.text = mixingGlass == null
                    ? "Mixing Glass 0.0 ml"
                    : $"Mixing Glass {mixingGlass.CurrentLiquidVolumeMl:0.0} ml{(mixingGlass.HasIce ? " / ice" : string.Empty)}";
                break;

            case State.Stirring:
                progressText.text =
                    $"Stir {techniqueTracker.ElapsedSeconds:0.0} / {targetStirDurationSeconds:0.0}s  " +
                    $"{StirRevolutions:0.00} rev  {StirConsistency:0.00} consistency";
                break;

            case State.AwaitingStrain:
                progressText.text = $"Mixing Glass {mixingGlass?.CurrentLiquidVolumeMl ?? 0f:0.0} ml → ready to strain";
                break;

            case State.AwaitingGarnish:
                progressText.text =
                    $"Serving Glass {servingGlass?.CurrentLiquidVolumeMl ?? 0f:0.0} ml / garnish " +
                    (garnishController != null && garnishController.IsExpectedGarnishRequired
                        ? "필수"
                        : "선택");
                break;

            case State.Result:
                progressText.text = $"Serving Glass {servingGlass?.CurrentLiquidVolumeMl ?? 0f:0.0} ml";
                break;
        }
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

    private void SetLegacyGaugeVisible(bool visible)
    {
        Transform gaugeRoot = transform.Find("UI/GaugeRoot");
        if (gaugeRoot != null)
            gaugeRoot.gameObject.SetActive(visible);
    }

    private void OnDisable()
    {
        StopDirectPour("StirMiniGame disabled");
    }

    private void ConfigureGarnishController()
    {
        garnishController = GetComponent<ServingGlassGarnishController>();
        if (garnishController == null)
            garnishController = gameObject.AddComponent<ServingGlassGarnishController>();

        // Stir's current prefab has only a Mixing Glass visual. I8 creates a
        // labelled runtime Serving Glass placeholder instead of decorating the
        // Mixing Glass after the actual liquid has been strained out of it.
        garnishController.Initialize(
            ingredientShelf != null ? ingredientShelf.ServingGlass : null,
            ingredientShelf?.PhysicalPresentation?.GetVessel(ingredientShelf.ServingGlass),
            hintText,
            ingredientShelf != null ? ingredientShelf.PreparationTrace : null);
        garnishController.SetExpectedRecipe(expectedRecipe);
    }

    private void AutoBindIfMissing()
    {
        if (resultLabel == null)
            resultLabel = transform.Find("UI/ResultLabel")?.GetComponent<TMP_Text>();

        if (progressText == null)
            progressText = transform.Find("UI/ProgressText")?.GetComponent<TMP_Text>();

        if (hintText == null)
            hintText = transform.Find("UI/HintText")?.GetComponent<TMP_Text>();

        if (startButton == null)
            startButton = transform.Find("UI/StartButton")?.GetComponent<Button>();

        if (startButton == null)
            startButton = transform.Find("UI/StartupButton")?.GetComponent<Button>();

        if (startHintText == null)
            startHintText = transform.Find("UI/StartHintText")?.GetComponent<TMP_Text>();

        if (cup == null)
            cup = transform.Find("UI/Cup")?.GetComponent<RectTransform>();
        if (cup == null)
            cup = transform.Find("UI/CupBack")?.GetComponent<RectTransform>();

        if (spoon == null)
            spoon = transform.Find("UI/Spoon")?.GetComponent<RectTransform>();

        if (spoonInCup == null)
            spoonInCup = transform.Find("UI/SpoonInCup")?.GetComponent<RectTransform>();
    }

}
