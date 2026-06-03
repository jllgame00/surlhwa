using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            selectedIngredientText.text = "Selected: None";
            return;
        }

        selectedIngredientText.text = $"Selected: {selectedIngredient.displayName}";
    }
}