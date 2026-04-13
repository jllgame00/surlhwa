using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class FloatMiniGame : MonoBehaviour, IMiniGame
{
    public enum FloatState { Ready, Pouring, Result }

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

    private FloatState state = FloatState.Ready;

    private Action<MiniGameResult> onDone;
    private OrderData currentOrder;

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

        state = FloatState.Ready;
        remainTime = timeLimit;
        currentTopHeight = 0f;
        releasedOnce = false;

        liquidAreaHeight = liquidArea != null ? liquidArea.rect.height : 100f;
        topLiquidStartY = topLiquid != null ? topLiquid.anchoredPosition.y : 0f;

        if (spoon != null)
            spoonStartPos = spoon.anchoredPosition;

        // TopLiquid가 현재 시작 위치에서 얼마나 더 올라갈 수 있는지 계산
        maxTopHeight = Mathf.Max(0f, liquidAreaHeight - topLiquidStartY);

        ResetUI();
        state = FloatState.Pouring;
    }

    private void Update()
    {
        if (state != FloatState.Pouring) return;

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

    private void ResetUI()
    {
        SetTopLiquidHeight(0f);
        UpdateSpoonPosition();

        if (ingredientText != null)
            ingredientText.text = GetDisplayTitle();

        if (resultLabel != null)
            resultLabel.text = "";

        UpdateProgressText();
        SetHint("SPACE를 눌러 천천히 띄우고, 적정선에서 떼세요");
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

        // spoon의 inspector 시작 위치를 기준으로,
        // currentTopHeight 만큼만 추가로 올린다.
        Vector2 pos = spoonStartPos;
        pos.y = spoonStartPos.y + currentTopHeight + spoonOffsetY;
        spoon.anchoredPosition = pos;
    }

    private void JudgeFloat()
    {
        float targetBottom = targetZone != null ? targetZone.anchoredPosition.y : 60f;
        float targetHeight = targetZone != null ? targetZone.rect.height : 20f;
        float targetCenter = targetBottom + (targetHeight * 0.5f);

        // 실제로 보이는 topLiquid 표면 기준
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
            grade = grade
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
            grade = "Fail"
        }));
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
}