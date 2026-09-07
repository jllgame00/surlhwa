using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// I4's direct Build route. The Serving Glass records actual Pour Core state;
/// confirming preparation submits that state without a second timing score.
/// </summary>
public class BuildMiniGame : MonoBehaviour, IMiniGame, IRequestDisplayTitleReceiver
{
    public enum BuildState
    {
        Preparing,
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

    // I2 runtime guidance only. It never inserts liquid or completes Build.
    private BartendingTopUpGuidance topUpGuidance;

    public void SetRequestDisplayTitle(string displayTitle)
    {
        requestDisplayTitle = displayTitle;
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
        ingredientShelf.Initialize(MiniGameType.Build, BartendingContainerType.ServingGlass);

        state = BuildState.Preparing;
        isPouring = false;
        topUpGuidance = null;

        if (ingredientText != null)
            ingredientText.text = GetDisplayTitle();

        if (resultLabel != null)
            resultLabel.text = string.Empty;

        if (startHintText != null)
        {
            startHintText.gameObject.SetActive(true);
            startHintText.text = "Build directly in the Serving Glass. Press Complete Build when ready.";
        }

        if (startButton != null)
        {
            startButton.gameObject.SetActive(true);
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(CompleteBuild);
            SetCompleteButtonLabel();
        }

        UpdateProgressText();
        SetHint("Select an ingredient and hold SPACE to pour. Press D for one dash.");

        Debug.Log(
            "[BuildMiniGame] Direct Build ready / " +
            "Target=ServingGlass / Completion=manual");
    }

    /// <summary>
    /// The player, not recipe expectation data, chooses when preparation ends.
    /// The captured composition is the actual Serving Glass snapshot.
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

        StopDirectPour("Build completed");
        BrewComposition composition = ingredientShelf.BuildServingGlassComposition();
        state = BuildState.Result;

        if (resultLabel != null)
            resultLabel.text = "Complete";

        if (startHintText != null)
            startHintText.gameObject.SetActive(false);

        if (startButton != null)
            startButton.gameObject.SetActive(false);

        ingredientShelf.SetVisible(false);

        Debug.Log(
            $"[BuildMiniGame] Direct Build complete / " +
            $"Method={composition.method} / " +
            $"Count={(composition.ingredients != null ? composition.ingredients.Length : 0)} / " +
            $"ServingMl={ingredientShelf.ServingGlass.CurrentLiquidVolumeMl:0.###}");

        // The shared result contract currently has no distinct
        // TechniqueCompleted state. success therefore means the player
        // completed direct preparation, not that a timing grade was awarded.
        // Recognition/request correctness remains downstream in OrderManager.
        onDone?.Invoke(new MiniGameResult
        {
            success = true,
            repDelta = 0,
            grade = "Complete",
            brewedComposition = composition
        });
    }

    private void Update()
    {
        if (state != BuildState.Preparing)
            return;

        UpdateDirectPour();
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
            return;

        isPouring = false;

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

    private void SetCompleteButtonLabel()
    {
        TMP_Text buttonText = startButton != null
            ? startButton.GetComponentInChildren<TMP_Text>(true)
            : null;

        if (buttonText != null)
            buttonText.text = "Complete Build";
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
