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

    [Header("Select")]
    [SerializeField] private Button selectButton;

    private IngredientData ingredient;
    private float currentAmount;

    private Action<IngredientData> onSelected;

    private void Awake()
    {
        if (selectButton == null)
            selectButton = GetComponent<Button>();
    }

    public void Bind(
        IngredientData ingredientData,
        float initialAmount,
        Action<IngredientData> selectedCallback = null)
    {
        ingredient = ingredientData;
        currentAmount = Mathf.Max(0f, initialAmount);

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
