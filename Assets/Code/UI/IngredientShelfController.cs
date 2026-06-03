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

    [Header("Amount")]
    [SerializeField] private float amountStep = 0.5f;

    private JiggerPanelController linkedJiggerPanel;

    private MiniGameType currentMethod;
    private readonly Dictionary<IngredientData, float> selectedAmounts = new Dictionary<IngredientData, float>();

    public void Initialize(MiniGameType method, BrewSessionData initialSession)
    {
        currentMethod = method;
        selectedAmounts.Clear();

        SeedFromSession(initialSession);
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

    private void SeedFromSession(BrewSessionData session)
    {
        if (session == null || session.ingredients == null)
            return;

        foreach (BrewSessionIngredient entry in session.ingredients)
        {
            if (entry == null || entry.ingredient == null)
                continue;

            selectedAmounts[entry.ingredient] = Mathf.Max(0f, entry.amount);
        }
    }

    private void RebuildIngredientList()
    {
        if (ingredientListRoot == null || ingredientOptionPrefab == null)
        {
            Debug.LogWarning("[IngredientShelfController] List root or option prefab is missing.");
            return;
        }

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
                amountStep,
                OnIngredientAmountChanged,
                OnIngredientSelected
            );
        }
    }

    private void OnIngredientAmountChanged(IngredientData ingredient, float amount)
    {
        if (ingredient == null)
            return;

        if (amount <= 0f)
            selectedAmounts.Remove(ingredient);
        else
            selectedAmounts[ingredient] = amount;

        RefreshTexts();
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

            parts.Add($"{pair.Key.displayName} x {pair.Value:0.##}");
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

        if (linkedJiggerPanel == null)
        {
            Debug.LogWarning($"[IngredientShelf] Selected {ingredient.displayName}, but linkedJiggerPanel is NULL");
            return;
        }

        linkedJiggerPanel.SelectIngredient(ingredient);

        Debug.Log($"[IngredientShelf] Selected ingredient -> {ingredient.displayName}");
    }
}