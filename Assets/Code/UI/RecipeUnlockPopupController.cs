using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeUnlockPopupController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform popupRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Image cocktailImage;

    [Header("Position")]
    [SerializeField] private Vector2 hiddenPosition = new Vector2(500f, 0f);
    [SerializeField] private Vector2 shownPosition = new Vector2(-40f, 0f);

    [Header("Timing")]
    [SerializeField] private float slideDuration = 0.25f;
    [SerializeField] private float showDuration = 2.5f;

    private Coroutine popupRoutine;

    private void Awake()
    {
        if (popupRoot != null)
        {
            popupRoot.anchoredPosition = hiddenPosition;
            popupRoot.gameObject.SetActive(false);
        }
    }

    public void Show(CocktailRecipeData recipe)
    {
        if (recipe == null || popupRoot == null)
            return;

        if (popupRoutine != null)
            StopCoroutine(popupRoutine);

        popupRoutine = StartCoroutine(ShowRoutine(recipe));
    }

    private IEnumerator ShowRoutine(CocktailRecipeData recipe)
    {
        SetupContent(recipe);

        popupRoot.gameObject.SetActive(true);
        popupRoot.anchoredPosition = hiddenPosition;

        yield return SlideTo(shownPosition);

        yield return new WaitForSeconds(showDuration);

        yield return SlideTo(hiddenPosition);

        popupRoot.gameObject.SetActive(false);
        popupRoutine = null;
    }

    private void SetupContent(CocktailRecipeData recipe)
    {
        if (titleText != null)
            titleText.text = recipe.displayName;

        if (messageText != null)
            messageText.text = "이 해금되었습니다!";

        if (cocktailImage != null)
        {
            Sprite sprite = null;

            if (recipe.targetCocktail != null)
                sprite = recipe.targetCocktail.cocktailSprite;

            cocktailImage.sprite = sprite;
            cocktailImage.gameObject.SetActive(sprite != null);
        }
    }

    private IEnumerator SlideTo(Vector2 targetPosition)
    {
        Vector2 startPosition = popupRoot.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);

            // 부드러운 ease-out
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            popupRoot.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, eased);
            yield return null;
        }

        popupRoot.anchoredPosition = targetPosition;
    }
}