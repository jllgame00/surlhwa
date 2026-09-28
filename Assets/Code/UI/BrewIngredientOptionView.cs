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

    [Header("Selection Visual")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.12f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.82f, 0.38f, 0.45f);
    
    public IngredientData Ingredient => ingredient;
    
    private IngredientData ingredient;
    private float currentAmountMl;

    private Action<IngredientData> onSelected;

    private void Awake()
    {
        if (selectButton == null)
            selectButton = GetComponent<Button>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
    }

    public void Bind(
        IngredientData ingredientData,
        float initialAmountMl,
        Action<IngredientData> selectedCallback = null)
    {
        ingredient = ingredientData;
        currentAmountMl = Mathf.Max(0f, initialAmountMl);

        onSelected = selectedCallback;

        if (nameText != null)
        {
            nameText.text = ingredient != null
                ? ingredient.displayName
                : "미확인";
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
        SetSelectedVisual(false);
    }

    public void SetAmountMl(float amountMl)
    {
        currentAmountMl = Mathf.Max(0f, amountMl);
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
            amountText.text = currentAmountMl <= 0f
                ? "0 ml"
                : $"{currentAmountMl:0.0} ml ({BartendingVolume.MillilitersToOunces(currentAmountMl):0.##} oz)";
        }
    }
    
    public void SetSelectedVisual(bool selected)
    {
        if (backgroundImage == null)
            return;

        backgroundImage.color = selected ? selectedColor : normalColor;
    }
}
