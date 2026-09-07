using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ShakeMiniGame;

public class BuildMiniGame : MonoBehaviour, IMiniGame, IRequestDisplayTitleReceiver
{
    public enum BuildState { Preview, Pouring, Result }

    [Header("UI")]
    [SerializeField] private RectTransform liquidArea;
    [SerializeField] private RectTransform liquidFill;
    [SerializeField] private RectTransform targetZone;
    [SerializeField] private TMP_Text ingredientText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private TMP_Text resultLabel;

    [Header("Start UI")]
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text startHintText;

    [Header("Rules")]
    [SerializeField] private float timeLimit = 4.5f;
    [SerializeField] private float fillSpeed = 180f;
    [SerializeField] private float overflowGrace = 6f;
    [SerializeField] private float perfectTolerance = 8f;
    [SerializeField] private float goodTolerance = 18f;

    private BuildState state = BuildState.Preview;

    private Action<MiniGameResult> onDone;

    [Header("Recipe Guide")]
    [SerializeField] private RecipeGuidePanelController recipeGuide;

    [Header("Ingredient Shelf")]
    [SerializeField] private IngredientShelfController ingredientShelf;

    [Header("Jigger Panel")]
    [SerializeField] private JiggerPanelController jiggerPanel;

    [Header("Pour Core Proof")]
    [SerializeField] private float previewPourRateMlPerSecond = 30f;

    private string requestDisplayTitle;

    private float remainTime;
    private float currentFillHeight;
    private float maxFillHeight;

    private bool releasedOnce = false;

    // The Build I1 proof snapshots the real serving-glass contents directly at
    // the existing matching boundary. I4 will replace this prototype Build
    // technique score; it is intentionally retained as a temporary fallback.
    private BrewComposition currentBrewComposition;
    private bool isPreviewPouring;

    // I2 runtime guidance only. Future recipe preparation data may supply this
    // instruction, but a TopUp never creates liquid by itself.
    private BartendingTopUpGuidance topUpGuidance;

    public void SetRequestDisplayTitle(string displayTitle)
    {
        requestDisplayTitle = displayTitle;
    }

    public void StartGame(Action<MiniGameResult> done)
    {
        onDone = done;

        AutoBindIfMissing();

        if (ingredientShelf == null)
            ingredientShelf = GetComponentInChildren<IngredientShelfController>(true);

        currentBrewComposition = null;
        isPreviewPouring = false;
        topUpGuidance = null;

        if (ingredientShelf == null)
            throw new InvalidOperationException("[BuildMiniGame] IngredientShelfController is required.");

        ingredientShelf.Initialize(MiniGameType.Build, BartendingContainerType.ServingGlass);

        if (jiggerPanel == null)
            jiggerPanel = GetComponentInChildren<JiggerPanelController>(true);

        if (jiggerPanel != null)
        {
            // Fixed 0.5/1 oz measurement remains the legacy path for the other
            // prototype techniques. The I1 Build proof instead pours directly
            // into the serving-glass state over real input duration.
            ingredientShelf.SetJiggerPanel(null);
            jiggerPanel.gameObject.SetActive(false);
        }

        if (recipeGuide == null)
            recipeGuide = GetComponentInChildren<RecipeGuidePanelController>(true);

        state = BuildState.Preview;
        remainTime = timeLimit;
        currentFillHeight = 0f;
        releasedOnce = false;

        if (liquidArea != null)
            maxFillHeight = liquidArea.rect.height;
        else
            maxFillHeight = 180f;

        SetLiquidHeight(0f);

        if (ingredientText != null)
            ingredientText.text = GetDisplayTitle();

        if (resultLabel != null)
            resultLabel.text = "";

        if (startHintText != null)
        {
            startHintText.gameObject.SetActive(true);
            startHintText.text = "[I3 proof] 1/2/3 target, I ice, T transfer, R strain. Hold SPACE to pour; D adds one dash.";
        }

        if (startButton != null)
        {
            startButton.gameObject.SetActive(true);
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(BeginGameplay);
        }

        UpdateProgressText();
        SetHint("[I3 proof] Target Serving/Mixing/Shaker with 1/2/3. I adds ice; T transfers; R strains. SPACE pours; D dashes.");

        Debug.Log(
            $"[BuildMiniGame] Preview ready / " +
            $"Container={(ingredientShelf.TargetContainer != null ? ingredientShelf.TargetContainer.ContainerType.ToString() : "NULL")} / " +
            $"startButton={(startButton != null ? startButton.name : "NULL")} / " +
            $"startHintText={(startHintText != null ? startHintText.name : "NULL")}"
        );
    }

    public void BeginGameplay()
    {
        Debug.Log("[BuildMiniGame] BeginGameplay clicked");

        if (state != BuildState.Preview)
        {
            Debug.LogWarning($"[BuildMiniGame] Begin ignored. state={state}");
            return;
        }

        if (!ingredientShelf.HasServingGlassContents())
        {
            SetHint("Pour into, transfer to, or strain into the Serving Glass before starting.");
            Debug.LogWarning("[BuildMiniGame] Cannot start. Serving Glass has no liquid.");
            return;
        }

        StopPreviewPour("Build technique started");
        currentBrewComposition = ingredientShelf.BuildServingGlassComposition();

        Debug.Log(
            $"[{nameof(BuildMiniGame)}] Authoritative serving-glass composition captured / " +
            $"Method={currentBrewComposition.method} / " +
            $"Count={(currentBrewComposition.ingredients != null ? currentBrewComposition.ingredients.Length : 0)}"
        );

        ingredientShelf.SetVisible(false);

        if (jiggerPanel != null)
            jiggerPanel.gameObject.SetActive(false);

        state = BuildState.Pouring;

        if (startHintText != null)
            startHintText.gameObject.SetActive(false);

        if (startButton != null)
            startButton.gameObject.SetActive(false);

        if (resultLabel != null)
            resultLabel.text = "";

        SetHint("Hold SPACE to pour, release to stop.");

        Debug.Log("[BuildMiniGame] BeginGameplay -> Pouring");
    }

    private void Update()
    {
        if (state == BuildState.Preview)
        {
            UpdatePreviewPour();
            return;
        }

        if (state != BuildState.Pouring)
            return;

        remainTime -= Time.deltaTime;
        if (remainTime <= 0f)
        {
            ResolveFail("FAIL");
            return;
        }

        if (Input.GetKey(KeyCode.Space))
        {
            currentFillHeight += fillSpeed * Time.deltaTime;
            SetLiquidHeight(currentFillHeight);

            if (currentFillHeight > maxFillHeight + overflowGrace)
            {
                SetHint("Too much poured!");
                ResolveFail("FAIL");
                return;
            }

            UpdateProgressText();
        }

        if (!releasedOnce && Input.GetKeyUp(KeyCode.Space))
        {
            releasedOnce = true;
            JudgeFill();
        }
    }

    private void JudgeFill()
    {
        float targetBottom = targetZone != null ? targetZone.anchoredPosition.y : 100f;
        float targetHeight = targetZone != null ? targetZone.rect.height : 24f;
        float targetCenter = targetBottom + (targetHeight * 0.5f);

        float diff = Mathf.Abs(currentFillHeight - targetCenter);

        if (diff <= perfectTolerance)
        {
            ResolveSuccess("Perfect", 2);
        }
        else if (diff <= goodTolerance)
        {
            ResolveSuccess("Good", 1);
        }
        else
        {
            ResolveFail("FAIL");
        }
    }

    private void ResolveSuccess(string grade, int repDelta)
    {
        state = BuildState.Result;

        if (resultLabel != null)
            resultLabel.text = grade;

        StartCoroutine(FinishAfterDelay(new MiniGameResult
        {
            success = true,
            repDelta = repDelta,
            grade = grade,
            brewedComposition = CreateBrewComposition()
        }));
    }

    private void ResolveFail(string label)
    {
        state = BuildState.Result;

        if (resultLabel != null)
            resultLabel.text = label;

        StartCoroutine(FinishAfterDelay(new MiniGameResult
        {
            success = false,
            repDelta = -1,
            grade = "Fail",
            brewedComposition = CreateBrewComposition()
        }));
    }

    private IEnumerator FinishAfterDelay(MiniGameResult result)
    {
        yield return new WaitForSeconds(1f);
        onDone?.Invoke(result);
    }

    private BrewComposition CreateBrewComposition()
    {
        return currentBrewComposition ?? ingredientShelf?.BuildServingGlassComposition() ?? new BrewComposition
        {
            method = MiniGameType.Build,
            ingredients = Array.Empty<BrewIngredientEntry>()
        };
    }

    private void UpdatePreviewPour()
    {
        if (ingredientShelf == null || !ingredientShelf.isActiveAndEnabled)
        {
            StopPreviewPour("target container unavailable");
            return;
        }

        if (HandleContainerProofInput())
            return;

        if (Input.GetKeyDown(KeyCode.D))
        {
            TryDispenseDash();
            return;
        }

        if (!Input.GetKey(KeyCode.Space))
        {
            StopPreviewPour("input released");
            ShowTopUpGuidance(ingredientShelf.SelectedIngredient);
            return;
        }

        IngredientData ingredient = ingredientShelf.SelectedIngredient;
        if (ingredient == null)
        {
            StopPreviewPour("no ingredient selected");
            SetHint("Select an ingredient before pouring.");
            return;
        }

        float requestedMl = Mathf.Max(0f, previewPourRateMlPerSecond) * Time.deltaTime;
        float transferredMl = ingredientShelf.PourSelectedIngredient(requestedMl);
        if (transferredMl <= 0f)
        {
            StopPreviewPour("transfer rejected");
            return;
        }

        if (!isPreviewPouring)
        {
            isPreviewPouring = true;
            Debug.Log(
                $"[Pour] Start ingredient={ingredient.ingredientId} target={ingredientShelf.TargetContainer.ContainerType} " +
                $"rate={previewPourRateMlPerSecond:0.##} ml/s");
        }

        float ingredientMl = ingredientShelf.TargetContainer.GetLiquidAmountMl(ingredient);
        float totalMl = ingredientShelf.TargetContainer.CurrentLiquidVolumeMl;
        if (!ShowTopUpGuidance(ingredient))
        {
            SetHint(
                $"Pouring {ingredient.displayName}: {ingredientMl:0.0} ml in " +
                $"{ingredientShelf.TargetContainer.ContainerType}.");

            if (progressText != null)
                progressText.text =
                    $"{ingredientShelf.TargetContainer.ContainerType} {totalMl:0.0} ml";
        }
    }

    // Temporary I3 proof controls. They intentionally do not define final
    // bartending UX; later Build/Stir/Shake phases will own adopted interaction.
    private bool HandleContainerProofInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            return SelectProofTarget(BartendingContainerType.ServingGlass);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            return SelectProofTarget(BartendingContainerType.MixingGlass);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            return SelectProofTarget(BartendingContainerType.Shaker);

        if (Input.GetKeyDown(KeyCode.I))
        {
            if (!ingredientShelf.TryAddIceToActiveTarget())
                SetHint("Ice is already present or no target container is active.");
            else
                SetHint($"Ice added to {ingredientShelf.TargetContainer.ContainerType}.");

            UpdateProgressText();
            return true;
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            BartendingContainerType sourceType = ingredientShelf.TargetContainer != null
                ? ingredientShelf.TargetContainer.ContainerType
                : BartendingContainerType.ServingGlass;

            if (!ingredientShelf.TryTransferActiveTargetToServingGlass(out float transferredMl))
                SetHint("Transfer requires liquid in Mixing Glass or Shaker.");
            else
                SetHint($"Transferred {transferredMl:0.0} ml from {sourceType} to Serving Glass.");

            UpdateProgressText();
            return true;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            BartendingContainerType sourceType = ingredientShelf.TargetContainer != null
                ? ingredientShelf.TargetContainer.ContainerType
                : BartendingContainerType.ServingGlass;

            if (!ingredientShelf.TryStrainActiveTargetToServingGlass(out float transferredMl))
                SetHint("Strain requires liquid in the Mixing Glass or Shaker.");
            else
                SetHint($"Strained {transferredMl:0.0} ml from {sourceType}; source ice remains.");

            UpdateProgressText();
            return true;
        }

        return false;
    }

    private bool SelectProofTarget(BartendingContainerType containerType)
    {
        StopPreviewPour("container target changed");

        if (!ingredientShelf.TrySetActiveTarget(containerType))
        {
            SetHint("That container target is unavailable.");
            return true;
        }

        SetHint($"Target: {containerType}. Select an ingredient, then hold SPACE to pour.");
        UpdateProgressText();
        return true;
    }

    /// <summary>
    /// Narrow I2 seam for future recipe instruction data and runtime validation.
    /// It supplies player guidance only; liquid is still added only while the
    /// player holds the existing Pour Core input.
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
        float currentVolumeMl = ingredientShelf != null && ingredientShelf.TargetContainer != null
            ? ingredientShelf.TargetContainer.CurrentLiquidVolumeMl
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
            SetHint("That dash could not be added to the active container.");
            return;
        }

        IngredientData ingredient = ingredientShelf.SelectedIngredient;
        float ingredientMl = ingredientShelf.TargetContainer.GetLiquidAmountMl(ingredient);
        SetHint($"{ingredient.displayName} — {totalDashCount} dash ({ingredientMl:0.0} ml).");

        if (progressText != null)
            progressText.text =
                $"{ingredientShelf.TargetContainer.ContainerType} " +
                $"{ingredientShelf.TargetContainer.CurrentLiquidVolumeMl:0.0} ml";

        Debug.Log(
            $"[Dash] Ingredient={ingredient.ingredientId} count={totalDashCount} " +
            $"compatibilityMl={ingredientMl:0.###}");
    }

    private bool ShowTopUpGuidance(IngredientData selectedIngredient)
    {
        if (topUpGuidance == null || !topUpGuidance.AppliesTo(selectedIngredient) ||
            ingredientShelf == null || ingredientShelf.TargetContainer == null)
            return false;

        float currentMl = ingredientShelf.TargetContainer.CurrentLiquidVolumeMl;
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

    private void StopPreviewPour(string reason)
    {
        if (!isPreviewPouring)
            return;

        isPreviewPouring = false;

        IngredientData ingredient = ingredientShelf != null ? ingredientShelf.SelectedIngredient : null;
        float ingredientMl = ingredientShelf != null && ingredientShelf.TargetContainer != null && ingredient != null
            ? ingredientShelf.TargetContainer.GetLiquidAmountMl(ingredient)
            : 0f;
        float totalMl = ingredientShelf != null && ingredientShelf.TargetContainer != null
            ? ingredientShelf.TargetContainer.CurrentLiquidVolumeMl
            : 0f;

        Debug.Log(
            $"[Pour] Stop reason={reason} ingredient={(ingredient != null ? ingredient.ingredientId : "NULL")} " +
            $"ingredientMl={ingredientMl:0.0} totalMl={totalMl:0.0}");
    }

    private void OnDisable()
    {
        StopPreviewPour("BuildMiniGame disabled");
    }

    private void SetLiquidHeight(float height)
    {
        if (liquidFill == null) return;

        float clamped = Mathf.Clamp(height, 0f, maxFillHeight + overflowGrace);

        Vector2 size = liquidFill.sizeDelta;
        size.y = clamped;
        liquidFill.sizeDelta = size;
    }

    private void UpdateProgressText()
    {
        if (progressText == null) return;

        if (state == BuildState.Preview && ingredientShelf != null && ingredientShelf.TargetContainer != null)
        {
            string iceText = ingredientShelf.TargetContainer.HasIce ? " / ice" : string.Empty;
            progressText.text =
                $"{ingredientShelf.TargetContainer.ContainerType} " +
                $"{ingredientShelf.TargetContainer.CurrentLiquidVolumeMl:0.0} ml{iceText}";
            return;
        }

        float percent = maxFillHeight > 0f
            ? Mathf.Clamp01(currentFillHeight / maxFillHeight) * 100f
            : 0f;

        progressText.text = $"Build {percent:0}%   Time {remainTime:0.0}";
    }

    private void SetHint(string msg)
    {
        if (hintText != null)
            hintText.text = msg;
    }

    private string GetDisplayTitle()
    {
        if (!string.IsNullOrEmpty(requestDisplayTitle))
            return requestDisplayTitle;

        return "Build";
    }

    private void AutoBindIfMissing()
    {
        if (liquidArea == null)
            liquidArea = transform.Find("UI/LiquidArea")?.GetComponent<RectTransform>();

        if (liquidFill == null)
            liquidFill = transform.Find("UI/LiquidArea/LiquidFill")?.GetComponent<RectTransform>();

        if (targetZone == null)
            targetZone = transform.Find("UI/LiquidArea/TargetZone")?.GetComponent<RectTransform>();

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
