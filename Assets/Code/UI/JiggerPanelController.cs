using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class JiggerPanelController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image jiggerImage;
    [SerializeField] private TMP_Text selectedIngredientText;
    [SerializeField] private Button addHalfButton;
    [SerializeField] private Button addOneButton;
    [SerializeField] private Button clearButton;

    private IngredientShelfController shelf;
    private IngredientData selectedIngredient;

    [SerializeField] private RectTransform jiggerVisualRoot;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private float popScale = 1.12f;
    [SerializeField] private float popDuration = 0.08f;

    private Coroutine popRoutine;

    private void Awake()
    {
        if (addHalfButton != null)
            addHalfButton.onClick.AddListener(() => AddAmount(0.5f));

        if (addOneButton != null)
            addOneButton.onClick.AddListener(() => AddAmount(1f));

        if (clearButton != null)
            clearButton.onClick.AddListener(ClearSelectedAmount);

        Refresh();
    }

    public void Initialize(IngredientShelfController targetShelf)
    {
        shelf = targetShelf;
        selectedIngredient = null;
        Refresh();
        gameObject.SetActive(true);
    }

    public void SelectIngredient(IngredientData ingredient)
    {
        selectedIngredient = ingredient;
        Refresh();
    }

    private void AddAmount(float amount)
    {
        if (shelf == null || selectedIngredient == null)
        {
            Debug.LogWarning("[JiggerPanel] No ingredient selected.");
            return;
        }

        shelf.AddAmount(selectedIngredient, amount);

        if (feedbackText != null)
            feedbackText.text = $"{selectedIngredient.displayName} +{amount:0.##}";

        PlayJiggerPop();

        Debug.Log($"[JiggerPanel] Add {amount:0.##} -> {selectedIngredient.displayName}");
        Refresh();
    }

    private void ClearSelectedAmount()
    {
        if (shelf == null || selectedIngredient == null)
            return;

        shelf.SetAmount(selectedIngredient, 0f);

        Debug.Log($"[JiggerPanel] Clear -> {selectedIngredient.displayName}");
        Refresh();
    }

    private void Refresh()
    {
        if (selectedIngredientText == null)
            return;

        if (selectedIngredient == null)
        {
            selectedIngredientText.text = "Select ingredient";
            return;
        }

        selectedIngredientText.text = selectedIngredient.displayName;
    }

    private void PlayJiggerPop()
    {
        if (jiggerVisualRoot == null)
            return;

        if (popRoutine != null)
            StopCoroutine(popRoutine);

        popRoutine = StartCoroutine(JiggerPopRoutine());
    }

    private IEnumerator JiggerPopRoutine()
    {
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = Vector3.one * popScale;

        float t = 0f;

        while (t < popDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / popDuration);
            jiggerVisualRoot.localScale = Vector3.Lerp(originalScale, targetScale, p);
            yield return null;
        }

        t = 0f;

        while (t < popDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / popDuration);
            jiggerVisualRoot.localScale = Vector3.Lerp(targetScale, originalScale, p);
            yield return null;
        }

        jiggerVisualRoot.localScale = originalScale;
    }
}