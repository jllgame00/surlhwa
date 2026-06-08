using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JiggerPanelController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image jiggerImage;
    [SerializeField] private TMP_Text selectedIngredientText;
    [SerializeField] private TMP_Text measureText;
    [SerializeField] private TMP_Text feedbackText;

    [SerializeField] private Button addHalfButton;
    [SerializeField] private Button addOneButton;
    [SerializeField] private Button pourButton;
    [SerializeField] private Button clearButton;

    [Header("Feedback")]
    [SerializeField] private RectTransform jiggerVisualRoot;
    [SerializeField] private float popScale = 1.18f;
    [SerializeField] private float popDuration = 0.07f;
    [SerializeField] private float shakeDistance = 8f;

    private IngredientShelfController shelf;
    private IngredientData selectedIngredient;

    private float measuredAmount;

    private Coroutine popRoutine;
    private Vector3 originalJiggerScale = Vector3.one;
    private Vector2 originalJiggerPosition;

    private void Awake()
    {
        if (addHalfButton != null)
            addHalfButton.onClick.AddListener(() => AddToJigger(0.5f));

        if (addOneButton != null)
            addOneButton.onClick.AddListener(() => AddToJigger(1f));

        if (pourButton != null)
            pourButton.onClick.AddListener(PourToShelf);

        if (clearButton != null)
            clearButton.onClick.AddListener(ClearJigger);

        Refresh();
    }

    public void Initialize(IngredientShelfController targetShelf)
    {
        shelf = targetShelf;
        selectedIngredient = null;
        measuredAmount = 0f;

        if (jiggerVisualRoot == null && jiggerImage != null)
            jiggerVisualRoot = jiggerImage.GetComponent<RectTransform>();

        if (jiggerVisualRoot != null)
        {
            originalJiggerScale = jiggerVisualRoot.localScale;
            originalJiggerPosition = jiggerVisualRoot.anchoredPosition;
        }

        Refresh();
        gameObject.SetActive(true);
    }

    public void SelectIngredient(IngredientData ingredient)
    {
        if (selectedIngredient != ingredient)
            measuredAmount = 0f;

        selectedIngredient = ingredient;

        Debug.Log(
            $"[JiggerPanel] Selected ingredient -> " +
            $"{(selectedIngredient != null ? selectedIngredient.displayName : "NULL")}"
        );

        if (feedbackText != null)
        {
            feedbackText.text = selectedIngredient != null
                ? $"{selectedIngredient.displayName} selected"
                : "";
        }

        Refresh();
    }

    private void AddToJigger(float amount)
    {
        if (selectedIngredient == null)
        {
            Debug.LogWarning("[JiggerPanel] No ingredient selected.");
            return;
        }

        measuredAmount += amount;

        if (feedbackText != null)
            feedbackText.text = $"{selectedIngredient.displayName} +{amount:0.##} oz";

        PlayJiggerPop();

        Debug.Log($"[JiggerPanel] Measure +{amount:0.##} -> {selectedIngredient.displayName} / total={measuredAmount:0.##}");

        Refresh();
    }

    private void PourToShelf()
    {
        if (shelf == null || selectedIngredient == null)
        {
            Debug.LogWarning("[JiggerPanel] Cannot pour. Shelf or ingredient is NULL.");
            return;
        }

        if (measuredAmount <= 0f)
        {
            Debug.LogWarning("[JiggerPanel] Cannot pour. Measured amount is 0.");
            return;
        }

        shelf.AddAmount(selectedIngredient, measuredAmount);

        if (feedbackText != null)
            feedbackText.text = $"Poured {measuredAmount:0.##} oz {selectedIngredient.displayName}";

        Debug.Log($"[JiggerPanel] Pour {measuredAmount:0.##} -> {selectedIngredient.displayName}");

        measuredAmount = 0f;
        PlayJiggerPop();
        Refresh();
    }

    private void ClearJigger()
    {
        measuredAmount = 0f;

        if (feedbackText != null)
            feedbackText.text = "Jigger cleared";

        Debug.Log("[JiggerPanel] Clear measured amount");

        Refresh();
    }

    private void Refresh()
    {
        if (selectedIngredientText != null)
        {
            selectedIngredientText.text = selectedIngredient != null
                ? selectedIngredient.displayName
                : "Select ingredient";
        }

        measureText.text = measuredAmount <= 0f
            ? "Empty"
            : $"{measuredAmount:0.##} oz";
    }

    private void PlayJiggerPop()
    {
        if (jiggerVisualRoot == null)
        {
            Debug.LogWarning("[JiggerPanel] Jigger Visual Root is NULL");
            return;
        }

        if (popRoutine != null)
            StopCoroutine(popRoutine);

        popRoutine = StartCoroutine(JiggerPopRoutine());
    }

    private IEnumerator JiggerPopRoutine()
    {
        Vector3 baseScale = originalJiggerScale == Vector3.zero
            ? Vector3.one
            : originalJiggerScale;

        Vector2 basePos = originalJiggerPosition;
        Vector3 targetScale = baseScale * popScale;

        float t = 0f;

        while (t < popDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / popDuration);

            jiggerVisualRoot.localScale = Vector3.Lerp(baseScale, targetScale, p);

            float shake = Mathf.Sin(p * Mathf.PI * 4f) * shakeDistance;
            jiggerVisualRoot.anchoredPosition = basePos + new Vector2(shake, 0f);

            yield return null;
        }

        t = 0f;

        while (t < popDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / popDuration);

            jiggerVisualRoot.localScale = Vector3.Lerp(targetScale, baseScale, p);

            float shake = Mathf.Sin((1f - p) * Mathf.PI * 4f) * shakeDistance * 0.5f;
            jiggerVisualRoot.anchoredPosition = basePos + new Vector2(shake, 0f);

            yield return null;
        }

        jiggerVisualRoot.localScale = baseScale;
        jiggerVisualRoot.anchoredPosition = basePos;
    }
}