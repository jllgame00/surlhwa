using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// I6's direct Shake route. Actual liquid remains in the Shaker until the
/// player explicitly strains it into the Serving Glass for result submission.
/// </summary>
public class ShakeMiniGame : MonoBehaviour, IMiniGame, IRecipePreparationExpectationReceiver,
    IIngredientAvailabilityContextReceiver
{
    public enum ShakeDirection
    {
        Left,
        Right
    }

    public enum ShakeJudgement
    {
        Ignored,
        Perfect,
        Good,
        Miss,
        Extra
    }

    private enum State
    {
        Preparing,
        Rhythm,
        AwaitingStrain,
        AwaitingGarnish,
        GentleStirring,
        Result
    }

    private enum ShakeGrade
    {
        Good,
        Acceptable,
        Poor
    }

    [Header("UI")]
    [SerializeField] private RectTransform shaker;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private TMP_Text resultLabel;

    [Header("Primary Action UI")]
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text startHintText;

    [Header("Rhythm Pattern")]
    [SerializeField] private int defaultPatternNoteCount = 8;
    [SerializeField] private float leadInSeconds = 0.6f;
    [SerializeField] private float noteIntervalSeconds = 0.5f;
    [SerializeField] private float perfectWindowSeconds = 0.09f;
    [SerializeField] private float goodWindowSeconds = 0.20f;

    [Header("Rhythm Grade Tuning")]
    [SerializeField] private float goodAccuracyThreshold = 0.75f;
    [SerializeField] private float acceptableAccuracyThreshold = 0.40f;
    [SerializeField] private int maximumGoodInputErrors = 2;

    [Header("Shaker Motion")]
    [SerializeField] private float shakeMoveX = 80f;
    [SerializeField] private float shakeDuration = 0.08f;
    [SerializeField] private float returnDuration = 0.08f;

    [Header("Ingredient Shelf")]
    [SerializeField] private IngredientShelfController ingredientShelf;

    [Header("Pour Core")]
    [SerializeField] private float pourRateMlPerSecond = 30f;

    [Header("I9B Splash")]
    [SerializeField] private float splashMilliliters = BartendingVolume.DefaultSplashMilliliters;

    private State state = State.Preparing;
    private Action<MiniGameResult> onDone;
    private Coroutine shakerMotion;
    private Vector2 shakerOrigin;
    private ShakeGrade completedGrade;
    private bool isPouring;
    private float rhythmStartTime;
    private int nextNoteIndex;
    private int perfectHitCount;
    private int goodHitCount;
    private int missCount;
    private int wrongDirectionCount;
    private int extraInputCount;
    private ServingGlassGarnishController garnishController;
    private CocktailRecipeData expectedRecipe;
    private ServingGlassGentleStirInteraction gentleStirInteraction;
    private bool isServingPouring;
    private IngredientAvailabilityContext ingredientAvailabilityContext;

    public int NoteCount => Mathf.Max(1, defaultPatternNoteCount);
    public int PerfectHitCount => perfectHitCount;
    public int GoodHitCount => goodHitCount;
    public int MissCount => missCount;
    public int WrongDirectionCount => wrongDirectionCount;
    public int ExtraInputCount => extraInputCount;
    public int CorrectHitCount => perfectHitCount + goodHitCount;
    public float DirectionAccuracy => NoteCount > 0 ? (float)CorrectHitCount / NoteCount : 0f;

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

        if (ingredientShelf == null)
            ingredientShelf = GetComponentInChildren<IngredientShelfController>(true);

        if (ingredientShelf == null)
            throw new InvalidOperationException("[ShakeMiniGame] IngredientShelfController is required.");

        // Shake owns direct preparation in the Shaker. I3's preparation state
        // still creates the Serving Glass for the later explicit Strain.
        ingredientShelf.Initialize(
            MiniGameType.Shake,
            BartendingContainerType.Shaker,
            ingredientAvailabilityContext);
        ConfigureGarnishController();

        state = State.Preparing;
        isPouring = false;
        ResetRhythmState();
        gentleStirInteraction = new ServingGlassGentleStirInteraction();
        isServingPouring = false;
        CaptureShakerOrigin();
        ResetShakerToOrigin();

        if (resultLabel != null)
        {
            resultLabel.gameObject.SetActive(true);
            resultLabel.text = string.Empty;
            resultLabel.color = Color.white;
        }

        if (startHintText != null)
        {
            startHintText.gameObject.SetActive(hintText == null);
            startHintText.text = "셰이커에 재료와 얼음을 넣고 흔들기를 시작하세요.";
        }

        SetPrimaryAction(BeginShake, "흔들기 시작", true);
        SetHint("재료를 고르고 Space를 눌러 셰이커에 붓습니다. I: 얼음");
        UpdateProgressText();

        Debug.Log("[Shake] Preparation ready / Target=Shaker / Ice=required");
    }

    private void Update()
    {
        if (RecipeGuidePanelController.IsTechniqueReadStateActive || DemoAudioController.IsSettingsOpen)
        {
            // Input and the unscaled rhythm timeline must pause together.
            if (state == State.Rhythm) rhythmStartTime += Time.unscaledDeltaTime;
            return;
        }

        switch (state)
        {
            case State.Preparing:
                UpdatePreparationInput();
                break;

            case State.Rhythm:
                UpdateRhythmInput();
                break;

            case State.AwaitingStrain:
                UpdatePostRhythmInput();
                break;

            case State.AwaitingGarnish:
                UpdateServingGlassInput();
                break;

            case State.GentleStirring:
                UpdateGentleStir();
                break;
        }
    }

    /// <summary>
    /// Begins the rhythm only after the player has made a physically shakeable
    /// state. It deliberately does not validate recipe identity or amounts.
    /// </summary>
    public void BeginShake()
    {
        if (ingredientShelf != null && !ingredientShelf.ToolsReady) return;
        if (state != State.Preparing)
        {
            Debug.LogWarning($"[Shake] Begin ignored. state={state}");
            return;
        }

        BartendingContainerState shakerState = GetShaker();
        if (shakerState == null || shakerState.CurrentLiquidVolumeMl <= 0f)
        {
            SetHint("셰이커에 재료를 먼저 부어 주세요.");
            return;
        }

        if (!shakerState.HasIce)
        {
            SetHint("흔들기 전에 셰이커에 얼음을 넣어 주세요.");
            return;
        }

        StopDirectPour("Shake started");
        state = State.Rhythm;
        ResetRhythmState();
        rhythmStartTime = Time.unscaledTime;

        ingredientShelf.SetVisible(false);
        SetPrimaryAction(null, string.Empty, false);
        SetHint("왼쪽·오른쪽 박자를 따라 눌러 주세요. 왼쪽: A / ← · 오른쪽: D / →");
        SetRhythmFeedback("Ready", new Color(0.8f, 0.86f, 0.92f, 1f));
        UpdateProgressText();

        Debug.Log(
            $"[Shake] Begin notes={NoteCount} interval={noteIntervalSeconds:0.###} " +
            $"leadIn={leadInSeconds:0.###} shakerMl={shakerState.CurrentLiquidVolumeMl:0.###} ice={shakerState.HasIce}");
    }

    /// <summary>
    /// Narrow production/probe seam. Keyboard input and deterministic runtime
    /// validation share this exact judgement path; this is not a beatmap API.
    /// </summary>
    public ShakeJudgement TryJudgeShakeInput(ShakeDirection direction, float inputTime)
    {
        if (state != State.Rhythm)
            return ShakeJudgement.Ignored;

        AdvanceExpiredNotes(inputTime);
        PlayShakerMotion(direction);

        if (nextNoteIndex >= NoteCount)
        {
            TryCompleteRhythm();
            extraInputCount++;
            SetHint("흔들기를 마쳤습니다. 잔으로 옮겨 주세요.");
            SetRhythmFeedback("완료", new Color(0.8f, 0.86f, 0.92f, 1f));
            UpdateProgressText();
            return ShakeJudgement.Extra;
        }

        float expectedTime = GetExpectedNoteTime(nextNoteIndex);
        float timingOffset = Mathf.Abs(inputTime - expectedTime);

        // Early or late inputs outside the eligible timing window leave the
        // note available for its proper time. They do not earn progress.
        if (timingOffset > Mathf.Max(0f, goodWindowSeconds))
        {
            extraInputCount++;
            SetHint("다음 방향과 박자를 확인해 주세요.");
            SetRhythmFeedback("Off Beat", new Color(0.95f, 0.65f, 0.32f, 1f));
            UpdateProgressText();
            return ShakeJudgement.Extra;
        }

        ShakeDirection expectedDirection = GetExpectedDirection(nextNoteIndex);
        if (direction != expectedDirection)
        {
            wrongDirectionCount++;
            ConsumeMiss();
            SetHint($"다음 방향은 {DirectionLabel(expectedDirection)}입니다.");
            SetRhythmFeedback("Miss", new Color(0.88f, 0.42f, 0.42f, 1f));
            UpdateProgressText();
            TryCompleteRhythm();
            return ShakeJudgement.Miss;
        }

        if (timingOffset <= Mathf.Max(0f, perfectWindowSeconds))
        {
            perfectHitCount++;
            nextNoteIndex++;
            SetHint($"정확하게 · {DirectionLabel(expectedDirection)}");
            SetRhythmFeedback("Perfect", new Color(0.78f, 0.93f, 0.72f, 1f));
            UpdateProgressText();
            TryCompleteRhythm();
            return ShakeJudgement.Perfect;
        }

        goodHitCount++;
        nextNoteIndex++;
        SetHint($"안정적으로 · {DirectionLabel(expectedDirection)}");
        SetRhythmFeedback("Good", new Color(0.8f, 0.86f, 0.92f, 1f));
        UpdateProgressText();
        TryCompleteRhythm();
        return ShakeJudgement.Good;
    }

    /// <summary>
    /// Returns one fixed timing point from I6's bounded alternating pattern.
    /// It exists so runtime probes can use the same times as keyboard input.
    /// </summary>
    public float GetExpectedNoteTime(int noteIndex)
    {
        return rhythmStartTime + Mathf.Max(0f, leadInSeconds) +
            (Mathf.Max(0, noteIndex) * Mathf.Max(0.01f, noteIntervalSeconds));
    }

    /// <summary>
    /// Explicit I6 preparation action. Ice belongs to the Shaker and is never
    /// silently inserted by BeginShake.
    /// </summary>
    public bool AddIceToShaker()
    {
        if (state != State.Preparing || ingredientShelf == null)
            return false;

        if (!ingredientShelf.TryAddIceToActiveTarget())
        {
            SetHint("셰이커에 이미 얼음이 있습니다.");
            return false;
        }

        SetHint("셰이커에 얼음을 넣었습니다. 재료를 더하거나 흔들기를 시작하세요.");
        UpdateProgressText();
        Debug.Log("[Container] AddIce target=Shaker");
        return true;
    }

    /// <summary>
    /// Reuses I3's non-scored Strain. The Serving Glass snapshot is the only
    /// composition submitted to the existing matching/result boundary.
    /// </summary>
    public void StrainAndSubmit()
    {
        if (state != State.AwaitingStrain)
        {
            Debug.LogWarning($"[Shake] Strain ignored. state={state}");
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
            SetHint("걸러 따르려면 셰이커에 액체가 있어야 합니다.");
            return;
        }

        FinishPhysicalStrain(transferredMl);
    }

    private void FinishPhysicalStrain(float transferredMl)
    {
        state = State.AwaitingGarnish;

        ingredientShelf.SetVisible(true);
        garnishController?.BeginSelection();
        SetPrimaryAction(ServeShake, "내어주기", true);
        SetHint("서빙 잔으로 옮겼습니다. S: 스플래시 · J: 가볍게 젓기 · 가니시를 더하거나 내어주세요.");
        UpdateProgressText();

        BartendingContainerState shakerState = GetShaker();
        Debug.Log(
            $"[Shake] Strain Shaker -> ServingGlass volumeMl={transferredMl:0.###} " +
            $"iceRetained={(shakerState != null && shakerState.HasIce)}");

    }

    /// <summary>
    /// I9B's Gin Fizz path. It shares I3's liquid-only transfer primitive with
    /// Strain but records the official semantic as PourFromContainer.
    /// </summary>
    public void PourFromContainerAndContinue()
    {
        if (state != State.AwaitingStrain)
        {
            Debug.LogWarning($"[Shake] PourFromContainer ignored. state={state}");
            return;
        }

        if (ingredientShelf != null && ingredientShelf.PhysicalPresentation != null)
        {
            ingredientShelf.BeginPhysicalContainerTransfer(RecipePreparationAction.PourFromContainer, FinishPhysicalPourFromContainer);
            return;
        }
        if (ingredientShelf == null ||
            !ingredientShelf.TryPourActiveTargetToServingGlass(out float transferredMl))
        {
            SetHint("옮겨 부으려면 셰이커에 액체가 있어야 합니다.");
            return;
        }

        FinishPhysicalPourFromContainer(transferredMl);
    }

    private void FinishPhysicalPourFromContainer(float transferredMl)
    {
        state = State.AwaitingGarnish;
        ingredientShelf.SetVisible(true);
        garnishController?.BeginSelection();
        SetPrimaryAction(ServeShake, "내어주기", true);
        SetHint(RequiresExpectedSplash()
            ? "셰이커에서 옮겨 부었습니다. S로 필요한 스플래시를 더해 주세요."
            : "Poured from the Shaker. Add the optional Splash with S, garnish, or serve.");
        UpdateProgressText();
        Debug.Log($"[Shake] PourFromContainer Shaker -> ServingGlass volumeMl={transferredMl:0.###}");
    }

    /// <summary>
    /// Final manual confirmation after the optional I8 garnish step. Rhythm
    /// quality remains the existing technique result and is not recalculated.
    /// </summary>
    public void ServeShake()
    {
        if (ingredientShelf != null && !ingredientShelf.ToolsReady) return;
        if (state != State.AwaitingGarnish)
        {
            Debug.LogWarning($"[Shake] Serve ignored. state={state}");
            return;
        }

        BrewComposition composition = ingredientShelf.BuildServingGlassComposition();
        MiniGameResult result = CreateTechniqueResult(composition);
        result.garnish = ingredientShelf.ServingGlass != null
            ? ingredientShelf.ServingGlass.Garnish
            : null;
        StopServingPour("Shake served");
        BartendingPreparationResultFactory.Apply(
            ref result,
            expectedRecipe,
            ingredientShelf.PreparationTrace);
        state = State.Result;
        garnishController?.EndSelection();

        SetPrimaryAction(null, string.Empty, false);
        SetHint("조주를 마쳤습니다.");
        UpdateProgressText();

        Debug.Log($"[Shake] Serve garnish={(result.garnish != null ? result.garnish.DisplayName + "/" + result.garnish.Placement : "none")}");
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
        if (ingredientShelf != null && ingredientShelf.HandlePhysicalTools(pourRateMlPerSecond)) return;
        if (ingredientShelf == null || !ingredientShelf.isActiveAndEnabled)
        {
            StopDirectPour("Shaker unavailable");
            return;
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            AddIceToShaker();
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
            Debug.Log(
                $"[Pour] Start ingredient={ingredient.ingredientId} target=Shaker " +
                $"rate={pourRateMlPerSecond:0.##} ml/s");
        }

        float ingredientMl = GetShaker().GetLiquidAmountMl(ingredient);
        SetHint($"Pouring {ingredient.displayName}: {ingredientMl:0.0} ml in Shaker.");
        UpdateProgressText();
    }

    private void UpdatePostRhythmInput()
    {
        if (Input.GetKeyDown(KeyCode.P))
            PourFromContainerAndContinue();
    }

    private void UpdateServingGlassInput()
    {
        if (ingredientShelf != null && ingredientShelf.HandlePhysicalTools(pourRateMlPerSecond)) return;
        if (ingredientShelf == null || !ingredientShelf.isActiveAndEnabled)
        {
            StopServingPour("Serving Glass unavailable");
            return;
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            BeginGentleStir();
            return;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            TryAddSplashToServingGlass();
            return;
        }

        if (!Input.GetKey(KeyCode.Space))
        {
            StopServingPour("input released");
            return;
        }

        IngredientData ingredient = ingredientShelf.SelectedIngredient;
        if (ingredient == null)
        {
            StopServingPour("no ingredient selected");
            SetHint("서빙 잔에 부을 재료를 먼저 골라 주세요.");
            return;
        }

        if (!ingredientShelf.PreparePhysicalPour()) return;
        float transferredMl = ingredientShelf.PourSelectedIngredient(
            Mathf.Max(0f, pourRateMlPerSecond) * Time.deltaTime);
        if (transferredMl <= 0f)
        {
            StopServingPour("transfer rejected");
            return;
        }

        if (!isServingPouring)
        {
            isServingPouring = true;
            Debug.Log($"[Pour] Start ingredient={ingredient.ingredientId} target=ServingGlass rate={pourRateMlPerSecond:0.##} ml/s");
        }

        SetHint($"{ingredient.displayName} 붓는 중 · J: 가볍게 젓기 · 준비되면 내어주세요.");
        UpdateProgressText();
    }

    public bool TryAddSplashToServingGlass()
    {
        if (ingredientShelf == null || ingredientShelf.SelectedIngredient == null ||
            !ingredientShelf.TrySplashSelectedIngredient(splashMilliliters, out float transferredMl))
        {
            SetHint($"재료를 고르고 {splashMilliliters:0.#} ml 스플래시를 더해 주세요.");
            return false;
        }

        SetHint($"스플래시 {transferredMl:0.#} ml를 더했습니다.");
        UpdateProgressText();
        return true;
    }

    public void BeginGentleStir()
    {
        if (ingredientShelf != null && !ingredientShelf.ToolsReady) return;
        if (state != State.AwaitingGarnish || ingredientShelf == null ||
            !ingredientShelf.HasServingGlassContents())
        {
            SetHint("가볍게 젓기 전에 서빙 잔에 액체가 있어야 합니다.");
            return;
        }

        StopServingPour("Gentle Stir started");
        RectTransform center = transform.Find("UI/GlassImage")?.GetComponent<RectTransform>();
        if (center == null) center = ingredientShelf.PhysicalPresentation?.GetVessel(ingredientShelf.ServingGlass);
        Transform visualParent = transform.Find("UI") ?? transform;
        if (gentleStirInteraction == null || !gentleStirInteraction.Begin(center, visualParent))
        {
            SetHint("가볍게 젓기를 시작할 수 없습니다.");
            return;
        }

        garnishController?.EndSelection();
        ingredientShelf.SetVisible(false);
        state = State.GentleStirring;
        SetPrimaryAction(FinishGentleStir, "젓기 마치기", true);
        SetHint("서빙 잔 둘레로 마우스를 가볍게 원을 그리며 움직여 주세요.");
        UpdateProgressText();
    }

    private void UpdateGentleStir()
    {
        gentleStirInteraction?.Tick();
        UpdateProgressText();
    }

    public bool TryRecordGentleStirAngle(float angleDeg, float deltaTime)
    {
        return state == State.GentleStirring &&
               gentleStirInteraction != null &&
               gentleStirInteraction.TryRecordAngle(angleDeg, deltaTime);
    }

    public void FinishGentleStir()
    {
        if (state != State.GentleStirring || gentleStirInteraction == null)
            return;

        if (!gentleStirInteraction.TryFinish())
        {
            SetHint("조금만 더 가볍게 저어 주세요.");
            return;
        }

        ingredientShelf.RecordTechniqueAction(
            RecipePreparationAction.GentleStir,
            BartendingContainerType.ServingGlass);
        state = State.AwaitingGarnish;
        ingredientShelf.SetVisible(true);
        garnishController?.BeginSelection();
        SetPrimaryAction(ServeShake, "내어주기", true);
        SetHint("가볍게 젓기를 마쳤습니다. 가니시나 재료를 더하거나 내어주세요.");
        UpdateProgressText();
        Debug.Log("[GentleStir] Shake route ServingGlass complete");
    }

    private void UpdateRhythmInput()
    {
        float now = Time.unscaledTime;
        AdvanceExpiredNotes(now);
        if (TryCompleteRhythm())
            return;

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            TryJudgeShakeInput(ShakeDirection.Left, now);
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            TryJudgeShakeInput(ShakeDirection.Right, now);
    }

    private void AdvanceExpiredNotes(float now)
    {
        while (nextNoteIndex < NoteCount &&
               now > GetExpectedNoteTime(nextNoteIndex) + Mathf.Max(0f, goodWindowSeconds))
        {
            ConsumeMiss();
        }
    }

    private void ConsumeMiss()
    {
        if (nextNoteIndex >= NoteCount)
            return;

        missCount++;
        nextNoteIndex++;
        SetRhythmFeedback("Miss", new Color(0.88f, 0.42f, 0.42f, 1f));
    }

    private bool TryCompleteRhythm()
    {
        if (state != State.Rhythm || nextNoteIndex < NoteCount)
            return false;

        completedGrade = EvaluateTechnique();
        ingredientShelf.RecordTechniqueAction(
            RecipePreparationAction.Shake,
            BartendingContainerType.Shaker);
        state = State.AwaitingStrain;

        if (resultLabel != null)
        {
            resultLabel.text = completedGrade.ToString();
            resultLabel.color = Color.white;
        }

        bool usePourFromContainer = UsesExpectedPourFromContainerTransfer();
        SetPrimaryAction(
            usePourFromContainer ? PourFromContainerAndContinue : StrainAndSubmit,
            usePourFromContainer ? "잔으로 붓기" : "Strain",
            true);
        SetHint(usePourFromContainer
            ? "흔들기를 마쳤습니다. 셰이커의 내용물을 서빙 잔으로 부어 주세요."
            : "흔들기를 마쳤습니다. 걸러 따르기 또는 P로 서빙 잔에 부어 주세요.");
        UpdateProgressText();

        Debug.Log(
            $"[Shake] Complete perfect={perfectHitCount} good={goodHitCount} miss={missCount} " +
            $"wrongDirection={wrongDirectionCount} extra={extraInputCount} " +
            $"accuracy={DirectionAccuracy:0.##} grade={completedGrade}");
        return true;
    }

    private ShakeGrade EvaluateTechnique()
    {
        int inputErrors = missCount + extraInputCount;
        if (DirectionAccuracy >= goodAccuracyThreshold && inputErrors <= maximumGoodInputErrors)
            return ShakeGrade.Good;

        if (DirectionAccuracy >= acceptableAccuracyThreshold)
            return ShakeGrade.Acceptable;

        return ShakeGrade.Poor;
    }

    private MiniGameResult CreateTechniqueResult(BrewComposition composition)
    {
        switch (completedGrade)
        {
            case ShakeGrade.Good:
                return new MiniGameResult
                {
                    success = true,
                    repDelta = 1,
                    grade = "Good",
                    brewedComposition = composition
                };

            case ShakeGrade.Acceptable:
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

    private void PlayShakerMotion(ShakeDirection direction)
    {
        if (shaker == null)
            return;

        if (shakerMotion != null)
            StopCoroutine(shakerMotion);

        shakerMotion = StartCoroutine(PlayShakerMotionRoutine(direction));
    }

    private IEnumerator PlayShakerMotionRoutine(ShakeDirection direction)
    {
        Vector2 start = shaker.anchoredPosition;
        float directionMultiplier = direction == ShakeDirection.Left ? -1f : 1f;
        Vector2 target = shakerOrigin + new Vector2(directionMultiplier * Mathf.Max(0f, shakeMoveX), 0f);

        yield return LerpShakerPosition(start, target, shakeDuration);
        yield return LerpShakerPosition(target, shakerOrigin, returnDuration);
        shakerMotion = null;
    }

    private IEnumerator LerpShakerPosition(Vector2 from, Vector2 to, float duration)
    {
        if (shaker == null)
            yield break;

        float safeDuration = Mathf.Max(0.001f, duration);
        float elapsed = 0f;
        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            shaker.anchoredPosition = Vector2.Lerp(from, to, Mathf.Clamp01(elapsed / safeDuration));
            yield return null;
        }

        if (shaker != null)
            shaker.anchoredPosition = to;
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
            SetHint("That dash could not be added to the Shaker.");
            return;
        }

        IngredientData ingredient = ingredientShelf.SelectedIngredient;
        float ingredientMl = GetShaker().GetLiquidAmountMl(ingredient);
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
        BartendingContainerState shakerState = GetShaker();
        float ingredientMl = ingredient != null && shakerState != null
            ? shakerState.GetLiquidAmountMl(ingredient)
            : 0f;
        float totalMl = shakerState != null ? shakerState.CurrentLiquidVolumeMl : 0f;

        Debug.Log(
            $"[Pour] Stop reason={reason} ingredient={(ingredient != null ? ingredient.ingredientId : "NULL")} " +
            $"ingredientMl={ingredientMl:0.0} totalMl={totalMl:0.0}");
    }

    private void StopServingPour(string reason)
    {
        if (!isServingPouring)
        {
            ingredientShelf?.EndSelectedPour();
            return;
        }

        isServingPouring = false;
        ingredientShelf?.EndSelectedPour();
        Debug.Log($"[Pour] Stop reason={reason} target=ServingGlass");
    }

    private ShakeDirection GetExpectedDirection(int noteIndex)
    {
        return noteIndex % 2 == 0 ? ShakeDirection.Left : ShakeDirection.Right;
    }

    private void ResetRhythmState()
    {
        rhythmStartTime = 0f;
        nextNoteIndex = 0;
        perfectHitCount = 0;
        goodHitCount = 0;
        missCount = 0;
        wrongDirectionCount = 0;
        extraInputCount = 0;
    }

    private BartendingContainerState GetShaker()
    {
        return ingredientShelf != null && ingredientShelf.PreparationState != null
            ? ingredientShelf.PreparationState.GetContainer(BartendingContainerType.Shaker)
            : null;
    }

    private void UpdateProgressText()
    {
        if (progressText == null)
            return;

        BartendingContainerState shakerState = GetShaker();
        BartendingContainerState servingGlass = ingredientShelf != null ? ingredientShelf.ServingGlass : null;

        switch (state)
        {
            case State.Preparing:
                progressText.text = shakerState == null
                    ? "Shaker 0.0 ml"
                    : $"Shaker {shakerState.CurrentLiquidVolumeMl:0.0} ml{(shakerState.HasIce ? " / ice" : string.Empty)}";
                break;

            case State.Rhythm:
                if (nextNoteIndex >= NoteCount)
                {
                    progressText.text = "흔들기 완료";
                    break;
                }

                float untilNext = GetExpectedNoteTime(nextNoteIndex) - Time.unscaledTime;
                progressText.text =
                    $"Next: {DirectionLabel(GetExpectedDirection(nextNoteIndex))} " +
                    (GetExpectedDirection(nextNoteIndex) == ShakeDirection.Left ? "[A / ←]" : "[D / →]") + "  " +
                    $"{Mathf.Max(0f, untilNext):0.00}s  " +
                    $"{nextNoteIndex + 1}/{NoteCount}  P{perfectHitCount} G{goodHitCount} M{missCount}";
                break;

            case State.AwaitingStrain:
                progressText.text =
                    $"Rhythm {CorrectHitCount}/{NoteCount} correct — Shaker " +
                    $"{shakerState?.CurrentLiquidVolumeMl ?? 0f:0.0} ml ready to " +
                    (UsesExpectedPourFromContainerTransfer() ? "pour to glass" : "strain");
                break;

            case State.AwaitingGarnish:
                progressText.text =
                    $"Serving Glass {servingGlass?.CurrentLiquidVolumeMl ?? 0f:0.0} ml / " +
                    (garnishController != null && garnishController.IsExpectedGarnishRequired
                        ? "가니시 필수"
                        : "가니시·가볍게 젓기 선택");
                break;

            case State.GentleStirring:
                progressText.text = $"Gentle Stir {gentleStirInteraction?.Revolutions ?? 0f:0.00} rotations";
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

    private void SetRhythmFeedback(string message, Color color)
    {
        if (resultLabel == null)
            return;

        resultLabel.text = message;
        resultLabel.color = color;
    }

    private void ResetShakerToOrigin()
    {
        if (shaker == null)
            return;

        if (shakerMotion != null)
        {
            StopCoroutine(shakerMotion);
            shakerMotion = null;
        }

        shaker.anchoredPosition = shakerOrigin;
    }

    private void CaptureShakerOrigin()
    {
        if (shaker != null)
            shakerOrigin = shaker.anchoredPosition;
    }

    private static string DirectionLabel(ShakeDirection direction)
    {
        return direction == ShakeDirection.Left ? "LEFT" : "RIGHT";
    }

    private void OnDisable()
    {
        StopDirectPour("ShakeMiniGame disabled");
        StopServingPour("ShakeMiniGame disabled");
        gentleStirInteraction?.End();
        ResetShakerToOrigin();
    }

    private void ConfigureGarnishController()
    {
        garnishController = GetComponent<ServingGlassGarnishController>();
        if (garnishController == null)
            garnishController = gameObject.AddComponent<ServingGlassGarnishController>();

        // The current Shake prefab contains a Shaker but no final-glass visual.
        // Use I8's clearly labelled runtime Serving Glass placeholder rather
        // than showing a strained drink's garnish on the source Shaker.
        garnishController.Initialize(
            ingredientShelf != null ? ingredientShelf.ServingGlass : null,
            ingredientShelf?.PhysicalPresentation?.GetVessel(ingredientShelf.ServingGlass),
            hintText,
            ingredientShelf != null ? ingredientShelf.PreparationTrace : null);
        garnishController.SetExpectedRecipe(expectedRecipe);
    }

    private bool UsesExpectedPourFromContainerTransfer()
    {
        return RecipePreparationEvaluator.HasRequiredAction(
            expectedRecipe,
            RecipePreparationAction.PourFromContainer);
    }

    private bool RequiresExpectedSplash()
    {
        return RecipePreparationEvaluator.HasRequiredAction(
            expectedRecipe,
            RecipePreparationAction.Splash);
    }

    private void AutoBindIfMissing()
    {
        if (shaker == null)
            shaker = transform.Find("UI/Shaker")?.GetComponent<RectTransform>();

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
