using System.Collections;
using TMPro;
using UnityEngine;

public class ResultToastController : MonoBehaviour
{
    private const float SecondRouteUnlockToastDelay = 2.8f;

    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;

    [Header("Timing")]
    [SerializeField] private float displayTime = 2f;
    [SerializeField] private float fadeDuration = 0.25f;

    private Coroutine showRoutine;
    private System.Action<bool> presentationCompleted;
    private RectTransform servedDrink;

    public float DisplayCycleDuration => Mathf.Max(0f, displayTime) +
                                         Mathf.Max(0f, fadeDuration) * 2f;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        SurlhwaUITheme.ScaleReadingCanvas(transform);
        SurlhwaUITheme.Panel(gameObject);
        var rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            SurlhwaUITheme.Place(rect, new Vector2(640, 188), Vector2.zero);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2(0, -24);
        }
        if (titleText != null)
        { SurlhwaUITheme.Text(titleText, 26); SurlhwaUITheme.Place(titleText.rectTransform, new Vector2(592, 38), new Vector2(0, 60)); }
        if (messageText != null)
        { SurlhwaUITheme.Text(messageText, 20); SurlhwaUITheme.Place(messageText.rectTransform, new Vector2(592, 110), new Vector2(0, -20)); }
        foreach (var graphic in GetComponentsInChildren<UnityEngine.UI.Graphic>(true)) graphic.raycastTarget = false;

        HideImmediate();
    }

    public void Show(string title, string message)
    {
        CancelPresentation();
        if (canvasGroup == null || !isActiveAndEnabled)
        {
            if (canvasGroup == null) Debug.LogWarning("[ResultToast] CanvasGroup is missing.");
            return;
        }

        SetContent(title, message);
        showRoutine = StartCoroutine(ShowRoutine());
    }

    public void ShowDelayed(string title, string message, float delay, System.Action<bool> completed = null)
    {
        CancelPresentation();
        if (canvasGroup == null || !isActiveAndEnabled)
        {
            if (canvasGroup == null) Debug.LogWarning("[ResultToast] CanvasGroup is missing.");
            completed?.Invoke(false);
            return;
        }
        presentationCompleted = completed;
        showRoutine = StartCoroutine(ShowDelayedRoutine(title, message, delay));
    }

    /// <summary>
    /// Shows the diagnostic service summary after the guest has reacted. The
    /// input is already resolved by OrderManager; this controller does not
    /// calculate any gameplay result.
    /// </summary>
    public void ShowServiceResult(GuestServiceResult result, float delay, System.Action<bool> completed = null)
    {
        ServiceFeedbackPresentation presentation = GuestServiceFeedbackFormatter.Build(result);
        ShowDelayed(presentation.Title, presentation.Message, delay, completed);
        if (result.BartendingResult.appearance != null)
        {
            servedDrink = result.BartendingResult.appearance.Draw(transform,
                result.CanRevealMatchedCocktail ? result.MatchedCocktail : null);
            servedDrink.anchorMin = servedDrink.anchorMax = new Vector2(0f, .5f);
            servedDrink.pivot = new Vector2(1f, .5f);
            servedDrink.anchoredPosition = new Vector2(-16f, 0f);
        }
    }

    private void ClearDrink()
    {
        if (servedDrink != null) Destroy(servedDrink.gameObject);
        servedDrink = null;
    }

    public void ShowRecipeMatched(string recipeName)
    {
        Show(
            $"{recipeName} 완성",
            "손님이 잔을 조용히 바라봅니다."
        );
    }

    public void ShowRecipeUnlocked(string recipeName)
    {
        Show(
            "새 레시피 발견",
            recipeName
        );
    }

    public void ShowFirstCorrectBrew()
    {
        Show(
            "첫 잔 완성",
            "손님이 잔을 조용히 감싸 쥡니다."
        );
    }

    public void ShowFirstRouteUnknownCocktail(bool miniGameSuccess)
    {
        if (miniGameSuccess)
        {
            Show(
                "이름 없는 한 잔",
                "낯설지만, 어딘가 오래 남는 맛입니다."
            );
            return;
        }

        Show(
            "흐려진 한 잔",
            "아직은 마음이 잔 안에서 흔들리고 있습니다."
        );
    }

    public void ShowSecondRouteRecipeUnlocked()
    {
        ShowDelayed(
            "새로운 한 잔",
            "손님의 감정이 레시피북에 조용히 남았습니다.",
            SecondRouteUnlockToastDelay
        );
    }

    public void ShowUnknownCocktail()
    {
        Show(
            "이름 붙이기 어려운 한 잔",
            "레시피를 다시 확인해보세요."
        );
    }

    public void ShowGuestFitOutcome(GuestReactionResolver.GuestResponseTone tone)
    {
        switch (tone)
        {
            case GuestReactionResolver.GuestResponseTone.GoodFitGoodExecution:
                Show("좋은 선택", "손님이 원한 방향에 맞고, 잔도 잘 완성됐습니다.");
                return;
            case GuestReactionResolver.GuestResponseTone.GoodFitWeakExecution:
                Show("선택은 맞았어요", "손님이 원한 방향이지만, 완성도는 아쉽습니다.");
                return;
            case GuestReactionResolver.GuestResponseTone.PoorFitGoodExecution:
                Show("선택이 빗나갔어요", "잔은 잘 만들었지만 손님이 원한 방향은 아닙니다.");
                return;
            case GuestReactionResolver.GuestResponseTone.PoorFitWeakExecution:
                Show("손님과 맞지 않았어요", "선택과 완성도 모두 다시 살펴보세요.");
                return;
            default:
                return;
        }
    }

    private IEnumerator ShowDelayedRoutine(string title, string message, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        SetContent(title, message);
        yield return ShowRoutine();
    }

    private IEnumerator ShowRoutine()
    {
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        yield return FadeTo(1f);

        yield return new WaitForSeconds(displayTime);

        yield return FadeTo(0f);

        showRoutine = null;
        CompletePresentation(true);
    }

    private void OnDisable() => CancelPresentation();

    private void CancelPresentation()
    {
        if (showRoutine != null) StopCoroutine(showRoutine);
        showRoutine = null;
        HideImmediate();
        ClearDrink();
        CompletePresentation(false);
    }

    private void CompletePresentation(bool completed)
    {
        var callback = presentationCompleted;
        presentationCompleted = null;
        callback?.Invoke(completed);
    }

    private void SetContent(string title, string message)
    {
        if (titleText != null)
            titleText.text = title;

        if (messageText != null)
            messageText.text = message;
        LayoutContent();
    }

    private void LayoutContent()
    {
        if (titleText == null || messageText == null || transform is not RectTransform panel) return;
        const float padding = 12f;
        const float gap = 8f;
        float width = Mathf.Max(1f, panel.rect.width - padding * 2f);
        float titleHeight = titleText.GetPreferredValues(titleText.text, width, Mathf.Infinity).y + 2f;
        float bodyHeight = messageText.GetPreferredValues(messageText.text, width, Mathf.Infinity).y + 2f;
        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, padding * 2f + titleHeight + gap + bodyHeight);
        PlaceText(titleText.rectTransform, padding, titleHeight, padding);
        PlaceText(messageText.rectTransform, padding + titleHeight + gap, bodyHeight, padding);
    }

    private static void PlaceText(RectTransform rect, float top, float height, float padding)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, 1f);
        rect.sizeDelta = new Vector2(-padding * 2f, height);
        rect.anchoredPosition = new Vector2(0f, -top);
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fadeDuration);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private void HideImmediate()
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
}
