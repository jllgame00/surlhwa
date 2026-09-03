using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ShakeMiniGame;

public class BuildMiniGame : MonoBehaviour, IMiniGame
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

    private OrderData currentOrder;

    private float remainTime;
    private float currentFillHeight;
    private float maxFillHeight;

    private bool releasedOnce = false;
    private int stepCount = 0;

    private BrewSessionData currentBrewSession;

    public void StartGame(OrderData order, Action<MiniGameResult> done)
    {
        onDone = done;

        AutoBindIfMissing();

        currentOrder = order;

        if (ingredientShelf == null)
            ingredientShelf = GetComponentInChildren<IngredientShelfController>(true);

        currentBrewSession = null;

        if (ingredientShelf == null)
            throw new InvalidOperationException("[BuildMiniGame] IngredientShelfController is required.");

        ingredientShelf.Initialize(MiniGameType.Build);

        if (jiggerPanel == null)
            jiggerPanel = GetComponentInChildren<JiggerPanelController>(true);

        if (jiggerPanel != null)
        {
            jiggerPanel.Initialize(ingredientShelf);
            ingredientShelf.SetJiggerPanel(jiggerPanel);
            jiggerPanel.gameObject.SetActive(true);
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

        stepCount = (currentOrder != null && currentOrder.steps != null)
            ? currentOrder.steps.Length
            : 0;

        SetLiquidHeight(0f);

        if (ingredientText != null)
            ingredientText.text = GetDisplayTitle();

        if (resultLabel != null)
            resultLabel.text = "";

        if (startHintText != null)
        {
            startHintText.gameObject.SetActive(true);
            startHintText.text = "Check the recipe, then start.";
        }

        if (startButton != null)
        {
            startButton.gameObject.SetActive(true);
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(BeginGameplay);
        }

        UpdateProgressText();
        SetHint("Press Start to begin building.");

        Debug.Log(
            $"[BuildMiniGame] Preview ready / " +
            $"BrewSession={(currentBrewSession != null ? currentBrewSession.method.ToString() : "NULL")} / " +
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

        if (!ingredientShelf.HasAnySelectedIngredient())
        {
            SetHint("Pour at least one ingredient first.");
            Debug.LogWarning("[BuildMiniGame] Cannot start. No ingredients poured.");
            return;
        }

        currentBrewSession = ingredientShelf.BuildSession();

        Debug.Log(
            $"[{nameof(BuildMiniGame)}] BrewSession built from ingredient shelf / " +
            $"Method={currentBrewSession.method} / " +
            $"Count={(currentBrewSession.ingredients != null ? currentBrewSession.ingredients.Length : 0)}"
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
            return;

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
        return BrewCompositionFactory.FromSession(currentBrewSession, MiniGameType.Build);
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
        if (currentOrder == null)
            return "Build";

        if (!string.IsNullOrEmpty(currentOrder.orderName))
        {
            if (stepCount > 0)
                return $"{currentOrder.orderName} ({stepCount} step)";
            return currentOrder.orderName;
        }

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
