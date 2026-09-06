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
        Show(recipe, null, null);
    }

    public void Show(CocktailRecipeData recipe, string titleOverride, string messageOverride)
    {
        if (recipe == null || popupRoot == null)
            return;

        if (popupRoutine != null)
            StopCoroutine(popupRoutine);

        popupRoutine = StartCoroutine(ShowRoutine(recipe, titleOverride, messageOverride));
    }

    private IEnumerator ShowRoutine(CocktailRecipeData recipe, string titleOverride, string messageOverride)
    {
        SetupContent(recipe, titleOverride, messageOverride);

        popupRoot.gameObject.SetActive(true);
        popupRoot.anchoredPosition = hiddenPosition;

        yield return SlideTo(shownPosition);

        yield return new WaitForSeconds(showDuration);

        yield return SlideTo(hiddenPosition);

        popupRoot.gameObject.SetActive(false);
        popupRoutine = null;
    }

    private void SetupContent(CocktailRecipeData recipe, string titleOverride, string messageOverride)
    {
        if (titleText != null)
            titleText.text = !string.IsNullOrEmpty(titleOverride)
                ? titleOverride
                : GetCocktailDisplayName(recipe);

        if (messageText != null)
            messageText.text = !string.IsNullOrEmpty(messageOverride)
                ? messageOverride
                : "새 레시피가 해금되었습니다.";

        if (cocktailImage != null)
        {
            CocktailData cocktail = GetTargetCocktail(recipe);
            Sprite sprite = cocktail != null ? cocktail.cocktailSprite : null;

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

            // Smooth ease-out.
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            popupRoot.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, eased);
            yield return null;
        }

        popupRoot.anchoredPosition = targetPosition;
    }

    private static CocktailData GetTargetCocktail(CocktailRecipeData recipe)
    {
        if (recipe != null && recipe.targetCocktail != null)
            return recipe.targetCocktail;

        Debug.LogError($"[RecipeUnlockPopup] Recipe '{recipe?.recipeId ?? "NULL"}' has no targetCocktail.");
        return null;
    }

    private static string GetCocktailDisplayName(CocktailRecipeData recipe)
    {
        CocktailData cocktail = GetTargetCocktail(recipe);
        return cocktail != null && !string.IsNullOrWhiteSpace(cocktail.cocktailName)
            ? cocktail.cocktailName
            : "Missing Cocktail Data";
    }
}
