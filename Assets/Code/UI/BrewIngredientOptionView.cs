using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BrewIngredientOptionView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;

    [Header("Select")]
    [SerializeField] private Button selectButton;

    [Header("Mode")]
    [SerializeField] private bool useInlineAmountButtons = false;

    private IngredientData ingredient;
    private float currentAmount;
    private float amountStep;

    private Action<IngredientData, float> onAmountChanged;
    private Action<IngredientData> onSelected;

    private void Awake()
    {
        if (selectButton == null)
            selectButton = GetComponent<Button>();
    }

    public void Bind(
        IngredientData ingredientData,
        float initialAmount,
        float step,
        Action<IngredientData, float> changedCallback,
        Action<IngredientData> selectedCallback = null)
    {
        ingredient = ingredientData;
        currentAmount = Mathf.Max(0f, initialAmount);
        amountStep = Mathf.Max(0.1f, step);

        onAmountChanged = changedCallback;
        onSelected = selectedCallback;

        if (nameText != null)
        {
            nameText.text = ingredient != null
                ? ingredient.displayName
                : "Unknown";
        }

        if (iconImage != null)
        {
            bool hasIcon = ingredient != null && ingredient.icon != null;
            iconImage.enabled = hasIcon;

            if (hasIcon)
                iconImage.sprite = ingredient.icon;
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(Select);
        }

        if (plusButton != null)
        {
            plusButton.gameObject.SetActive(useInlineAmountButtons);
            plusButton.onClick.RemoveAllListeners();

            if (useInlineAmountButtons)
                plusButton.onClick.AddListener(IncreaseAmount);
        }

        if (minusButton != null)
        {
            minusButton.gameObject.SetActive(useInlineAmountButtons);
            minusButton.onClick.RemoveAllListeners();

            if (useInlineAmountButtons)
                minusButton.onClick.AddListener(DecreaseAmount);
        }

        Refresh();
    }

    public void Select()
    {
        if (ingredient == null)
        {
            Debug.LogWarning("[BrewIngredientOptionView] Select called but ingredient is NULL");
            return;
        }

        Debug.Log($"[BrewIngredientOptionView] Select -> {ingredient.displayName}");

        onSelected?.Invoke(ingredient);
    }

    private void IncreaseAmount()
    {
        currentAmount += amountStep;
        NotifyChanged();
    }

    private void DecreaseAmount()
    {
        currentAmount -= amountStep;

        if (currentAmount < 0f)
            currentAmount = 0f;

        NotifyChanged();
    }

    private void NotifyChanged()
    {
        Refresh();
        onAmountChanged?.Invoke(ingredient, currentAmount);
    }

    private void Refresh()
    {
        if (amountText != null)
        {
            amountText.text = currentAmount <= 0f
                ? "0 oz"
                : $"{currentAmount:0.##} oz";
        }
    }
}