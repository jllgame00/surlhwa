using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class BrewSelectionController : MonoBehaviour
{
    [Header("Ingredient Data")]
    [SerializeField] private IngredientData[] allIngredients;

    [Header("UI")]
    [SerializeField] private Transform ingredientListRoot;
    [SerializeField] private BrewIngredientOptionView ingredientOptionPrefab;
    [SerializeField] private TMP_Text selectedMethodText;
    [SerializeField] private TMP_Text summaryText;

    private OrderData currentOrder;
    private MiniGameType selectedMethod;

    private readonly Dictionary<IngredientData, float> selectedAmounts = new Dictionary<IngredientData, float>();

    public MiniGameType SelectedMethod => selectedMethod;

    public void Begin(OrderData order)
    {
        currentOrder = order;

        BrewSessionData defaultSession = order != null
            ? order.defaultBrewSession
            : null;

        MiniGameType initialMethod = defaultSession != null
            ? defaultSession.method
            : MiniGameType.Stir;

        SetMethod(initialMethod);
    }

    public void SetMethod(MiniGameType method)
    {
        selectedMethod = method;
        selectedAmounts.Clear();

        BrewSessionData defaultSession = currentOrder != null
            ? currentOrder.defaultBrewSession
            : null;

        if (defaultSession != null && defaultSession.method == method)
            SeedFromSession(defaultSession);

        RebuildIngredientList();
        RefreshTexts();
    }

    public BrewSessionData BuildSelectedSession()
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
            method = selectedMethod,
            ingredients = entries.ToArray()
        };
    }

    public bool HasAnySelectedIngredient()
    {
        return selectedAmounts.Any(pair => pair.Key != null && pair.Value > 0f);
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
            Debug.LogWarning("[BrewSelectionController] Ingredient list root or option prefab is missing.");
            return;
        }

        for (int i = ingredientListRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(ingredientListRoot.GetChild(i).gameObject);
        }

        if (allIngredients == null)
            return;

        foreach (IngredientData ingredient in allIngredients)
        {
            if (ingredient == null)
                continue;

            float amount = selectedAmounts.TryGetValue(ingredient, out float savedAmount)
                ? savedAmount
                : 0f;

            BrewIngredientOptionView option = Instantiate(ingredientOptionPrefab, ingredientListRoot);

            option.Bind(
                ingredient,
                amount
            );
        }
    }

    private void RefreshTexts()
    {
        if (selectedMethodText != null)
            selectedMethodText.text = $"Method: {selectedMethod}";

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
}
