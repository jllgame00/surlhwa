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
    public float DisplayCycleDuration => Mathf.Max(0f, showDuration) + Mathf.Max(0f, slideDuration) * 2f;

    private Coroutine popupRoutine;
    private System.Action<bool> presentationCompleted;

    private void Awake()
    {
        if (popupRoot != null)
        {
            SurlhwaUITheme.ScaleReadingCanvas(popupRoot);
            // The serialized background lives below the animated root.
            var background = popupRoot.Find("UnlockPanel");
            SurlhwaUITheme.Panel(background != null ? background.gameObject : popupRoot.gameObject);
            SurlhwaUITheme.Place(popupRoot, new Vector2(440, 164), Vector2.zero);
            popupRoot.anchorMin = popupRoot.anchorMax = popupRoot.pivot = Vector2.one;
            hiddenPosition = new Vector2(500, -100); shownPosition = new Vector2(-24, -100);
            if (titleText != null) { SurlhwaUITheme.Text(titleText, 24); SurlhwaUITheme.Place(titleText.rectTransform, new Vector2(292, 42), new Vector2(48, 44)); }
            if (messageText != null) { SurlhwaUITheme.Text(messageText, 20); SurlhwaUITheme.Place(messageText.rectTransform, new Vector2(292, 90), new Vector2(48, -24)); }
            if (cocktailImage != null) { SurlhwaUITheme.Place(cocktailImage.rectTransform, new Vector2(88, 116), new Vector2(-156, 0)); cocktailImage.preserveAspect = true; }
            foreach (var graphic in popupRoot.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
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
        ShowDelayed(recipe, 0f, titleOverride, messageOverride);
    }

    /// <summary>
    /// Defers one already-authorized discovery presentation so it follows the
    /// guest reaction and result summary. It does not make a recipe Known.
    /// </summary>
    public void ShowDelayed(
        CocktailRecipeData recipe,
        float delay,
        string titleOverride = null,
        string messageOverride = null,
        System.Action<bool> completed = null)
    {
        CancelPresentation();
        if (recipe == null || popupRoot == null || !isActiveAndEnabled)
        {
            completed?.Invoke(false);
            return;
        }
        presentationCompleted = completed;
        popupRoutine = StartCoroutine(ShowDelayedRoutine(
            recipe,
            Mathf.Max(0f, delay),
            titleOverride,
            messageOverride));
    }

    private IEnumerator ShowDelayedRoutine(
        CocktailRecipeData recipe,
        float delay,
        string titleOverride,
        string messageOverride)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        yield return ShowRoutine(recipe, titleOverride, messageOverride);
    }

    private IEnumerator ShowRoutine(CocktailRecipeData recipe, string titleOverride, string messageOverride)
    {
        SetupContent(recipe, titleOverride, messageOverride);

        popupRoot.gameObject.SetActive(true);
        DemoAudioController.Current?.Sounds?.StartDiscovery();
        popupRoot.anchoredPosition = hiddenPosition;

        yield return SlideTo(shownPosition);

        yield return new WaitForSeconds(showDuration);

        yield return SlideTo(hiddenPosition);

        popupRoot.gameObject.SetActive(false);
        popupRoutine = null;
        CompletePresentation(true);
    }

    private void OnDisable() => CancelPresentation();

    private void CancelPresentation()
    {
        if (popupRoutine != null) StopCoroutine(popupRoutine);
        popupRoutine = null;
        if (popupRoot != null) popupRoot.gameObject.SetActive(false);
        CompletePresentation(false);
    }

    // Clear before invoking: natural dismissal, replacement and disable each
    // release the same request at most once, after its UI is hidden.
    private void CompletePresentation(bool completed)
    {
        DemoAudioController.Current?.Sounds?.EndDiscovery();
        var callback = presentationCompleted;
        presentationCompleted = null;
        callback?.Invoke(completed);
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
