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

    public float DisplayCycleDuration => Mathf.Max(0f, displayTime) +
                                         Mathf.Max(0f, fadeDuration) * 2f;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        HideImmediate();
    }

    public void Show(string title, string message)
    {
        if (canvasGroup == null)
        {
            Debug.LogWarning("[ResultToast] CanvasGroup is missing.");
            return;
        }

        if (showRoutine != null)
            StopCoroutine(showRoutine);

        SetContent(title, message);
        showRoutine = StartCoroutine(ShowRoutine());
    }

    public void ShowDelayed(string title, string message, float delay)
    {
        if (canvasGroup == null)
        {
            Debug.LogWarning("[ResultToast] CanvasGroup is missing.");
            return;
        }

        if (showRoutine != null)
            StopCoroutine(showRoutine);

        showRoutine = StartCoroutine(ShowDelayedRoutine(title, message, delay));
    }

    /// <summary>
    /// Shows the diagnostic service summary after the guest has reacted. The
    /// input is already resolved by OrderManager; this controller does not
    /// calculate any gameplay result.
    /// </summary>
    public void ShowServiceResult(GuestServiceResult result, float delay)
    {
        ServiceFeedbackPresentation presentation = GuestServiceFeedbackFormatter.Build(result);
        ShowDelayed(presentation.Title, presentation.Message, delay);
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
    }

    private void SetContent(string title, string message)
    {
        if (titleText != null)
            titleText.text = title;

        if (messageText != null)
            messageText.text = message;
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
