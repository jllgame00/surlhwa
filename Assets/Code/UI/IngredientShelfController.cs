using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class IngredientShelfController : MonoBehaviour
{
    [Header("Ingredient Data")]
    [SerializeField] private IngredientData[] allIngredients;

    [Header("UI")]
    [SerializeField] private Transform ingredientListRoot;
    [SerializeField] private BrewIngredientOptionView ingredientOptionPrefab;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text summaryText;

    private JiggerPanelController linkedJiggerPanel;

    private MiniGameType currentMethod;
    private IngredientData selectedIngredient;

    private readonly Dictionary<IngredientData, float> selectedAmounts = new Dictionary<IngredientData, float>();
    private readonly List<BrewIngredientOptionView> optionViews = new List<BrewIngredientOptionView>();

    public void Initialize(MiniGameType method, BrewSessionData initialSession)
    {
        currentMethod = method;
        selectedIngredient = null;
        selectedAmounts.Clear();

        // Do not seed from the recipe/default session.
        // The shelf represents what the player has actually poured.
        RebuildIngredientList();
        RefreshTexts();

        gameObject.SetActive(true);
    }

    public BrewSessionData BuildSession()
    {
        List<BrewSessionIngredient> entries = new List<BrewSessionIngredient>();

        foreach (KeyValuePair<IngredientData, float> pair in selectedAmounts)
        {
            if (pair.Key == null)
                continue;

            if (pair.Value <= 0f)
                continue;

            entries.Add(new BrewSessionIngredient
            {
                ingredient = pair.Key,
                amount = pair.Value
            });
        }

        return new BrewSessionData
        {
            method = currentMethod,
            ingredients = entries.ToArray()
        };
    }

    public bool HasAnySelectedIngredient()
    {
        return selectedAmounts.Any(pair => pair.Key != null && pair.Value > 0f);
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    private void RebuildIngredientList()
    {
        if (ingredientListRoot == null || ingredientOptionPrefab == null)
        {
            Debug.LogWarning("[IngredientShelfController] List root or option prefab is missing.");
            return;
        }
        
        optionViews.Clear();

        for (int i = ingredientListRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(ingredientListRoot.GetChild(i).gameObject);
        }

        if (allIngredients == null)
            return;

        IEnumerable<IngredientData> sortedIngredients = allIngredients
            .Where(ingredient => ingredient != null)
            .OrderBy(ingredient => ingredient.category)
            .ThenBy(ingredient => ingredient.sortOrder)
            .ThenBy(ingredient => ingredient.displayName);

        foreach (IngredientData ingredient in sortedIngredients)
        {
            float amount = selectedAmounts.TryGetValue(ingredient, out float savedAmount)
                ? savedAmount
                : 0f;

            BrewIngredientOptionView option = Instantiate(ingredientOptionPrefab, ingredientListRoot);

            option.Bind(
                ingredient,
                amount,
                OnIngredientSelected
            );

            optionViews.Add(option);
            option.SetSelectedVisual(ingredient == selectedIngredient);
        }
    }

    private void RefreshTexts()
    {
        if (titleText != null)
            titleText.text = $"Ingredients - {currentMethod}";

        if (summaryText == null)
            return;

        if (!HasAnySelectedIngredient())
        {
            summaryText.text = "No ingredients selected";
            return;
        }

        List<string> parts = new List<string>();

        foreach (KeyValuePair<IngredientData, float> pair in selectedAmounts)
        {
            if (pair.Key == null || pair.Value <= 0f)
                continue;

            parts.Add($"{pair.Key.displayName} {pair.Value:0.##} oz");
        }

        summaryText.text = string.Join(" / ", parts);
    }

    public void SetJiggerPanel(JiggerPanelController jiggerPanel)
    {
        linkedJiggerPanel = jiggerPanel;

        Debug.Log(
            $"[IngredientShelf] JiggerPanel linked -> " +
            $"{(linkedJiggerPanel != null ? linkedJiggerPanel.name : "NULL")}"
        );
    }

    public void AddAmount(IngredientData ingredient, float amount)
    {
        if (ingredient == null)
            return;

        selectedAmounts.TryGetValue(ingredient, out float currentAmount);
        selectedAmounts[ingredient] = Mathf.Max(0f, currentAmount + amount);

        RebuildIngredientList();
        RefreshTexts();
    }

    public void SetAmount(IngredientData ingredient, float amount)
    {
        if (ingredient == null)
            return;

        if (amount <= 0f)
            selectedAmounts.Remove(ingredient);
        else
            selectedAmounts[ingredient] = amount;

        RebuildIngredientList();
        RefreshTexts();
    }

    private void OnIngredientSelected(IngredientData ingredient)
    {
        if (ingredient == null)
            return;

        selectedIngredient = ingredient;
        RefreshSelectedVisuals();

        if (linkedJiggerPanel == null)
        {
            Debug.LogWarning($"[IngredientShelf] Selected {ingredient.displayName}, but linkedJiggerPanel is NULL");
            return;
        }

        linkedJiggerPanel.SelectIngredient(ingredient);

        Debug.Log($"[IngredientShelf] Selected ingredient -> {ingredient.displayName}");
    }
    
    private void RefreshSelectedVisuals()
    {
        foreach (BrewIngredientOptionView option in optionViews)
        {
            if (option == null)
                continue;

            option.SetSelectedVisual(option.Ingredient == selectedIngredient);
        }
    }
    
    public void RemoveAmount(IngredientData ingredient, float amount)
    {
        if (ingredient == null)
            return;

        if (amount <= 0f)
            return;

        selectedAmounts.TryGetValue(ingredient, out float currentAmount);

        float nextAmount = Mathf.Max(0f, currentAmount - amount);

        if (nextAmount <= 0f)
            selectedAmounts.Remove(ingredient);
        else
            selectedAmounts[ingredient] = nextAmount;

        RebuildIngredientList();
        RefreshTexts();
    }
}
