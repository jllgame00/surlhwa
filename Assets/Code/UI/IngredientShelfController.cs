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

    private readonly List<BrewIngredientOptionView> optionViews = new List<BrewIngredientOptionView>();

    // I1 owns actual liquid state here. The selected ingredient is only the
    // current source; the target container records what was really transferred.
    private BartendingContainerState targetContainer;

    public IngredientData SelectedIngredient => selectedIngredient;
    public BartendingContainerState TargetContainer => targetContainer;

    public void Initialize(
        MiniGameType method,
        BartendingContainerType targetContainerType = BartendingContainerType.ServingGlass)
    {
        currentMethod = method;
        selectedIngredient = null;
        targetContainer = new BartendingContainerState(targetContainerType);

        // Do not seed from the recipe/default session.
        // The shelf represents what the player has actually poured.
        RebuildIngredientList();
        RefreshTexts();

        gameObject.SetActive(true);
    }

    public BrewSessionData BuildSession()
    {
        List<BrewSessionIngredient> entries = new List<BrewSessionIngredient>();

        if (targetContainer == null)
            return new BrewSessionData
            {
                method = currentMethod,
                ingredients = entries.ToArray()
            };

        foreach (BartendingLiquidEntry entry in targetContainer.LiquidEntries)
        {
            if (entry == null || entry.Ingredient == null)
                continue;

            float amountOz = BartendingVolume.MillilitersToOunces(entry.AmountMl);
            if (amountOz <= 0f)
                continue;

            entries.Add(new BrewSessionIngredient
            {
                ingredient = entry.Ingredient,
                amount = amountOz
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
        return targetContainer != null && targetContainer.CurrentLiquidVolumeMl > 0f;
    }

    /// <summary>
    /// Returns a result-bound snapshot for the existing recipe matcher. The
    /// target container remains authoritative until the minigame submits it.
    /// </summary>
    public BrewComposition BuildComposition()
    {
        return targetContainer != null
            ? targetContainer.ToLegacyBrewComposition(currentMethod)
            : new BrewComposition
            {
                method = currentMethod,
                ingredients = System.Array.Empty<BrewIngredientEntry>()
            };
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
            float amountMl = targetContainer != null
                ? targetContainer.GetLiquidAmountMl(ingredient)
                : 0f;

            BrewIngredientOptionView option = Instantiate(ingredientOptionPrefab, ingredientListRoot);

            option.Bind(
                ingredient,
                amountMl,
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

        foreach (BartendingLiquidEntry entry in targetContainer.LiquidEntries)
        {
            if (entry == null || entry.Ingredient == null || entry.AmountMl <= 0f)
                continue;

            string dashText = entry.DashCount > 0 ? $" / {entry.DashCount} dash" : string.Empty;
            parts.Add(
                $"{entry.Ingredient.displayName} {entry.AmountMl:0.0} ml " +
                $"({BartendingVolume.MillilitersToOunces(entry.AmountMl):0.##} oz){dashText}");
        }

        summaryText.text =
            $"{targetContainer.ContainerType} {targetContainer.CurrentLiquidVolumeMl:0.0} ml: " +
            string.Join(" / ", parts);
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

        AddLiquidMl(ingredient, BartendingVolume.OuncesToMilliliters(amount));
    }

    public void SetAmount(IngredientData ingredient, float amount)
    {
        if (ingredient == null)
            return;

        if (targetContainer == null)
            return;

        targetContainer.SetLiquidAmount(ingredient, BartendingVolume.OuncesToMilliliters(amount));
        RefreshContents();
    }

    private void OnIngredientSelected(IngredientData ingredient)
    {
        if (ingredient == null)
            return;

        selectedIngredient = ingredient;
        RefreshSelectedVisuals();

        if (linkedJiggerPanel != null)
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

        if (targetContainer == null)
            return;

        targetContainer.RemoveLiquid(ingredient, BartendingVolume.OuncesToMilliliters(amount));
        RefreshContents();
    }

    /// <summary>
    /// Adds the amount actually transferred by a Bartending 2.0 pour action.
    /// Unlike AddAmount, this API accepts canonical milliliters.
    /// </summary>
    public float AddLiquidMl(IngredientData ingredient, float amountMl)
    {
        if (targetContainer == null)
            return 0f;

        float transferredMl = targetContainer.AddLiquid(ingredient, amountMl);
        if (transferredMl > 0f)
            RefreshContents();

        return transferredMl;
    }

    public float PourSelectedIngredient(float amountMl)
    {
        return AddLiquidMl(selectedIngredient, amountMl);
    }

    /// <summary>
    /// Executes one discrete Dash for the currently selected source ingredient.
    /// Its ml compatibility amount and dash count both live in the target
    /// container; this UI controller does not retain a duplicate result.
    /// </summary>
    public bool TryDashSelectedIngredient(out int totalDashCount)
    {
        totalDashCount = 0;

        if (targetContainer == null || selectedIngredient == null)
            return false;

        bool added = targetContainer.TryAddDash(
            selectedIngredient,
            BartendingVolume.DashesToMilliliters(1),
            out totalDashCount);

        if (added)
            RefreshContents();

        return added;
    }

    private void RefreshContents()
    {
        foreach (BrewIngredientOptionView option in optionViews)
        {
            if (option == null)
                continue;

            option.SetAmountMl(targetContainer != null
                ? targetContainer.GetLiquidAmountMl(option.Ingredient)
                : 0f);
        }

        RefreshTexts();
    }
}
