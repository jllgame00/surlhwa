using System.Collections;
using TMPro;
using UnityEngine;

public class ResultToastController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;

    [Header("Timing")]
    [SerializeField] private float displayTime = 2f;
    [SerializeField] private float fadeDuration = 0.25f;

    private Coroutine showRoutine;

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

        if (titleText != null)
            titleText.text = title;

        if (messageText != null)
            messageText.text = message;

        if (showRoutine != null)
            StopCoroutine(showRoutine);

        showRoutine = StartCoroutine(ShowRoutine());
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

    public void ShowUnknownCocktail()
    {
        Show(
            "이름 붙이기 어려운 한 잔",
            "레시피를 다시 확인해보세요."
        );
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
