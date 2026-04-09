using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class BuildMiniGame : MonoBehaviour, IMiniGame
{
    public enum BuildState { Ready, Pouring, Result }

    [Header("UI")]
    [SerializeField] private RectTransform liquidArea;
    [SerializeField] private RectTransform liquidFill;
    [SerializeField] private RectTransform targetZone;
    [SerializeField] private TMP_Text ingredientText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private TMP_Text resultLabel;

    [Header("Rules")]
    [SerializeField] private float timeLimit = 4.5f;
    [SerializeField] private float fillSpeed = 180f;
    [SerializeField] private float overflowGrace = 6f;
    [SerializeField] private float perfectTolerance = 8f;
    [SerializeField] private float goodTolerance = 18f;

    private BuildState state = BuildState.Ready;

    private Action<MiniGameResult> onDone;
    private OrderData currentOrder;

    private float remainTime;
    private float currentFillHeight;
    private float maxFillHeight;

    private bool releasedOnce = false;
    private int stepCount = 0;

    public void StartGame(OrderData order, Action<MiniGameResult> callback)
    {
        onDone = callback;
        currentOrder = order;

        state = BuildState.Ready;
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

        UpdateProgressText();
        SetHint("SPACE를 누르고 있다가 적정선에서 떼세요");

        state = BuildState.Pouring;
    }

    private void Update()
    {
        if (state != BuildState.Pouring) return;

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
                SetHint("너무 많이 따랐다!");
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
            grade = grade
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
            grade = "Fail"
        }));
    }

    private IEnumerator FinishAfterDelay(MiniGameResult result)
    {
        yield return new WaitForSeconds(1f);
        onDone?.Invoke(result);
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
}