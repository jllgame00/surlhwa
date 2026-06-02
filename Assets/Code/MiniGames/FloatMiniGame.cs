using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FloatMiniGame : MonoBehaviour, IMiniGame
{
    public enum FloatState { Preview, Pouring, Result }

    [Header("UI")]
    [SerializeField] private RectTransform liquidArea;
    [SerializeField] private RectTransform baseLiquid;
    [SerializeField] private RectTransform topLiquid;
    [SerializeField] private RectTransform targetZone;
    [SerializeField] private RectTransform spoon;

    [SerializeField] private TMP_Text ingredientText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private TMP_Text resultLabel;

    [Header("Start UI")]
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text startHintText;

    [Header("Rules")]
    [SerializeField] private float timeLimit = 5f;
    [SerializeField] private float fillSpeed = 90f;
    [SerializeField] private float perfectTolerance = 6f;
    [SerializeField] private float goodTolerance = 14f;
    [SerializeField] private float resultDelay = 1f;
    [SerializeField] private float overflowGrace = 4f;

    [Header("Display")]
    [SerializeField] private string defaultDisplayName = "Float";
    [SerializeField] private float spoonOffsetY = 6f;

    private FloatState state = FloatState.Preview;

    private Action<MiniGameResult> onDone;
    private OrderData currentOrder;

    [Header("Recipe Guide")]
    [SerializeField] private RecipeGuidePanelController recipeGuide;

    private float remainTime;
    private float currentTopHeight;
    private float maxTopHeight;

    private bool releasedOnce = false;

    private Vector2 spoonStartPos;
    private float topLiquidStartY;
    private float liquidAreaHeight;

    public void StartGame(OrderData order, Action<MiniGameResult> callback)
    {
        onDone = callback;
        currentOrder = order;

        AutoBindIfMissing();
        
        if (recipeGuide == null)
            recipeGuide = GetComponentInChildren<RecipeGuidePanelController>(true);

        state = FloatState.Preview;
        remainTime = timeLimit;
        currentTopHeight = 0f;
        releasedOnce = false;

        liquidAreaHeight = liquidArea != null ? liquidArea.rect.height : 100f;
        topLiquidStartY = topLiquid != null ? topLiquid.anchoredPosition.y : 0f;

        if (spoon != null)
            spoonStartPos = spoon.anchoredPosition;

        maxTopHeight = Mathf.Max(0f, liquidAreaHeight - topLiquidStartY);

        ResetUIForPreview();

        if (startHintText != null)
        {
            startHintText.gameObject.SetActive(true);
            startHintText.text = "레시피를 확인한 뒤 시작하세요";
        }

        if (startButton != null)
        {
            startButton.gameObject.SetActive(true);
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(BeginGameplay);
        }

        Debug.Log(
            $"[FloatMiniGame] Preview ready / " +
            $"startButton={(startButton != null ? startButton.name : "NULL")} / " +
            $"startHintText={(startHintText != null ? startHintText.name : "NULL")}"
        );
    }

    public void BeginGameplay()
    {
        Debug.Log("[FloatMiniGame] BeginGameplay clicked");

        if (state != FloatState.Preview)
        {
            Debug.LogWarning($"[FloatMiniGame] Begin ignored. state={state}");
            return;
        }

        state = FloatState.Pouring;

        if (startHintText != null)
            startHintText.gameObject.SetActive(false);

        if (startButton != null)
            startButton.gameObject.SetActive(false);

        if (resultLabel != null)
            resultLabel.text = "";

        SetHint("SPACE를 눌러 천천히 띄우고, 적정선에서 떼세요");

        Debug.Log("[FloatMiniGame] BeginGameplay -> Pouring");
    }

    private void Update()
    {
        if (state == FloatState.Preview)
            return;

        if (state != FloatState.Pouring)
            return;

        remainTime -= Time.deltaTime;
        if (remainTime <= 0f)
        {
            ResolveFail("FAIL");
            return;
        }

        if (Input.GetKey(KeyCode.Space))
        {
            currentTopHeight += fillSpeed * Time.deltaTime;
            ApplyLiquidAndSpoon();

            if (currentTopHeight > maxTopHeight + overflowGrace)
            {
                SetHint("너무 빨리 넘쳤다!");
                ResolveFail("FAIL");
                return;
            }

            UpdateProgressText();
        }

        if (!releasedOnce && Input.GetKeyUp(KeyCode.Space))
        {
            releasedOnce = true;
            JudgeFloat();
        }
    }

    private void ResetUIForPreview()
    {
        SetTopLiquidHeight(0f);
        UpdateSpoonPosition();

        if (ingredientText != null)
            ingredientText.text = GetDisplayTitle();

        if (resultLabel != null)
            resultLabel.text = "";

        UpdateProgressText();
        SetHint("시작 버튼을 누르면 플로트가 시작됩니다");
    }

    private void ApplyLiquidAndSpoon()
    {
        SetTopLiquidHeight(currentTopHeight);
        UpdateSpoonPosition();
    }

    private void SetTopLiquidHeight(float height)
    {
        if (topLiquid == null) return;

        float clamped = Mathf.Clamp(height, 0f, maxTopHeight + overflowGrace);

        Vector2 size = topLiquid.sizeDelta;
        size.y = clamped;
        topLiquid.sizeDelta = size;
    }

    private void UpdateSpoonPosition()
    {
        if (spoon == null) return;

        Vector2 pos = spoonStartPos;
        pos.y = spoonStartPos.y + currentTopHeight + spoonOffsetY;
        spoon.anchoredPosition = pos;
    }

    private void JudgeFloat()
    {
        float targetBottom = targetZone != null ? targetZone.anchoredPosition.y : 60f;
        float targetHeight = targetZone != null ? targetZone.rect.height : 20f;
        float targetCenter = targetBottom + (targetHeight * 0.5f);

        float currentSurface = topLiquidStartY + currentTopHeight;
        float diff = Mathf.Abs(currentSurface - targetCenter);

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
        if (state == FloatState.Result) return;
        state = FloatState.Result;

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
        if (state == FloatState.Result) return;
        state = FloatState.Result;

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

    private BrewComposition CreateBrewComposition()
    {
        return BrewCompositionFactory.FromSession(
            currentOrder != null ? currentOrder.defaultBrewSession : null,
            MiniGameType.Float
        );
    }

    private IEnumerator FinishAfterDelay(MiniGameResult result)
    {
        yield return new WaitForSeconds(resultDelay);
        onDone?.Invoke(result);
    }

    private void UpdateProgressText()
    {
        if (progressText == null) return;

        float percent = maxTopHeight > 0f
            ? Mathf.Clamp01(currentTopHeight / maxTopHeight) * 100f
            : 0f;

        progressText.text = $"Float {percent:0}%   Time {remainTime:0.0}";
    }

    private void SetHint(string msg)
    {
        if (hintText != null)
            hintText.text = msg;
    }

    private string GetDisplayTitle()
    {
        if (currentOrder == null) return defaultDisplayName;

        if (!string.IsNullOrEmpty(currentOrder.orderName))
            return currentOrder.orderName;

        return defaultDisplayName;
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