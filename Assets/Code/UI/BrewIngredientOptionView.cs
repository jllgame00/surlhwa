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

    private IngredientData ingredient;
    private float currentAmount;
    private float amountStep;
    private Action<IngredientData, float> onAmountChanged;

    public void Bind(
        IngredientData ingredientData,
        float initialAmount,
        float step,
        Action<IngredientData, float> changedCallback)
    {
        ingredient = ingredientData;
        currentAmount = Mathf.Max(0f, initialAmount);
        amountStep = Mathf.Max(0.1f, step);
        onAmountChanged = changedCallback;

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

        if (plusButton != null)
        {
            plusButton.onClick.RemoveAllListeners();
            plusButton.onClick.AddListener(IncreaseAmount);
        }

        if (minusButton != null)
        {
            minusButton.onClick.RemoveAllListeners();
            minusButton.onClick.AddListener(DecreaseAmount);
        }

        Refresh();
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
        if (amountText == null)
            return;

        amountText.text = currentAmount <= 0f
            ? "0"
            : currentAmount.ToString("0.##");
    }
}