using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// I6's direct Shake route. Actual liquid remains in the Shaker until the
/// player explicitly strains it into the Serving Glass for result submission.
/// </summary>
public class ShakeMiniGame : MonoBehaviour, IMiniGame
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

    public int NoteCount => Mathf.Max(1, defaultPatternNoteCount);
    public int PerfectHitCount => perfectHitCount;
    public int GoodHitCount => goodHitCount;
    public int MissCount => missCount;
    public int WrongDirectionCount => wrongDirectionCount;
    public int ExtraInputCount => extraInputCount;
    public int CorrectHitCount => perfectHitCount + goodHitCount;
    public float DirectionAccuracy => NoteCount > 0 ? (float)CorrectHitCount / NoteCount : 0f;

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
        ingredientShelf.Initialize(MiniGameType.Shake, BartendingContainerType.Shaker);

        state = State.Preparing;
        isPouring = false;
        ResetRhythmState();
        CaptureShakerOrigin();
        ResetShakerToOrigin();

        if (resultLabel != null)
        {
            resultLabel.gameObject.SetActive(true);
            resultLabel.text = string.Empty;
        }

        if (startHintText != null)
        {
            startHintText.gameObject.SetActive(true);
            startHintText.text = "Prepare in the Shaker. Press Begin Shake after adding ice.";
        }

        SetPrimaryAction(BeginShake, "Begin Shake", true);
        SetHint("Select an ingredient and hold SPACE to pour into the Shaker. Press I to add ice.");
        UpdateProgressText();

        Debug.Log("[Shake] Preparation ready / Target=Shaker / Ice=required");
    }

    private void Update()
    {
        switch (state)
        {
            case State.Preparing:
                UpdatePreparationInput();
                break;

            case State.Rhythm:
                UpdateRhythmInput();
                break;
        }
    }

    /// <summary>
    /// Begins the rhythm only after the player has made a physically shakeable
    /// state. It deliberately does not validate recipe identity or amounts.
    /// </summary>
    public void BeginShake()
    {
        if (state != State.Preparing)
        {
            Debug.LogWarning($"[Shake] Begin ignored. state={state}");
            return;
        }

        BartendingContainerState shakerState = GetShaker();
        if (shakerState == null || shakerState.CurrentLiquidVolumeMl <= 0f)
        {
            SetHint("Pour at least one ingredient into the Shaker first.");
            return;
        }

        if (!shakerState.HasIce)
        {
            SetHint("Add ice to the Shaker before shaking.");
            return;
        }

        StopDirectPour("Shake started");
        state = State.Rhythm;
        ResetRhythmState();
        rhythmStartTime = Time.unscaledTime;

        ingredientShelf.SetVisible(false);
        SetPrimaryAction(null, string.Empty, false);
        SetHint("Follow the LEFT / RIGHT rhythm. Use A or Left Arrow, and D or Right Arrow.");
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
            SetHint("Rhythm already complete. Strain the drink.");
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
            SetHint("Off beat. Follow the next direction at the hit time.");
            UpdateProgressText();
            return ShakeJudgement.Extra;
        }

        ShakeDirection expectedDirection = GetExpectedDirection(nextNoteIndex);
        if (direction != expectedDirection)
        {
            wrongDirectionCount++;
            ConsumeMiss();
            SetHint($"Wrong direction. Expected {DirectionLabel(expectedDirection)}.");
            UpdateProgressText();
            TryCompleteRhythm();
            return ShakeJudgement.Miss;
        }

        if (timingOffset <= Mathf.Max(0f, perfectWindowSeconds))
        {
            perfectHitCount++;
            nextNoteIndex++;
            SetHint("Perfect shake.");
            UpdateProgressText();
            TryCompleteRhythm();
            return ShakeJudgement.Perfect;
        }

        goodHitCount++;
        nextNoteIndex++;
        SetHint("Good shake.");
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
            SetHint("The Shaker already has ice.");
            return false;
        }

        SetHint("Ice added to the Shaker. Continue pouring or begin Shake.");
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

        if (ingredientShelf == null ||
            !ingredientShelf.TryStrainActiveTargetToServingGlass(out float transferredMl))
        {
            SetHint("Strain requires liquid in the Shaker.");
            return;
        }

        BrewComposition composition = ingredientShelf.BuildServingGlassComposition();
        MiniGameResult result = CreateTechniqueResult(composition);
        state = State.Result;

        SetPrimaryAction(null, string.Empty, false);
        SetHint("Shake complete.");
        UpdateProgressText();

        BartendingContainerState shakerState = GetShaker();
        Debug.Log(
            $"[Shake] Strain Shaker -> ServingGlass volumeMl={transferredMl:0.###} " +
            $"iceRetained={(shakerState != null && shakerState.HasIce)}");

        onDone?.Invoke(result);
    }

    private void UpdatePreparationInput()
    {
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
            SetHint("Select an ingredient before pouring.");
            return;
        }

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
    }

    private bool TryCompleteRhythm()
    {
        if (state != State.Rhythm || nextNoteIndex < NoteCount)
            return false;

        completedGrade = EvaluateTechnique();
        state = State.AwaitingStrain;

        if (resultLabel != null)
            resultLabel.text = completedGrade.ToString();

        SetPrimaryAction(StrainAndSubmit, "Strain", true);
        SetHint("Rhythm complete. Press Strain to move the actual Shaker contents into the Serving Glass.");
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
            SetHint("Select an ingredient before adding a dash.");
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
            return;

        isPouring = false;
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
                    progressText.text = "Rhythm complete";
                    break;
                }

                float untilNext = GetExpectedNoteTime(nextNoteIndex) - Time.unscaledTime;
                progressText.text =
                    $"{DirectionLabel(GetExpectedDirection(nextNoteIndex))}  " +
                    $"{Mathf.Max(0f, untilNext):0.00}s  " +
                    $"{nextNoteIndex + 1}/{NoteCount}  P{perfectHitCount} G{goodHitCount} M{missCount}";
                break;

            case State.AwaitingStrain:
                progressText.text =
                    $"Rhythm {CorrectHitCount}/{NoteCount} correct — Shaker " +
                    $"{shakerState?.CurrentLiquidVolumeMl ?? 0f:0.0} ml ready to strain";
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
        ResetShakerToOrigin();
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
