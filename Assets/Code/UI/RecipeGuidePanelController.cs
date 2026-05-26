using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeGuidePanelController : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private RecipeUnlockManager recipeUnlockManager;

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    [Header("Recipe List")]
    [SerializeField] private Transform recipeListParent;
    [SerializeField] private Button recipeButtonPrefab;

    [Header("Recipe Detail")]
    [SerializeField] private TMP_Text recipeNameText;
    [SerializeField] private TMP_Text recipeMethodText;
    [SerializeField] private TMP_Text recipeIngredientText;

    private readonly List<Button> spawnedButtons = new();
    private MiniGameType currentMethod;
    private CocktailRecipeData selectedRecipe;

    private void Awake()
    {
        if (recipeUnlockManager == null)
            recipeUnlockManager = FindFirstObjectByType<RecipeUnlockManager>();

        if (recipeUnlockManager == null)
            Debug.LogWarning("[RecipeGuidePanel] RecipeUnlockManager not found in scene");

        if (openButton != null) openButton.onClick.AddListener(OpenPanel);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void SetMethod(MiniGameType method)
    {
        currentMethod = method;
        selectedRecipe = null;

        RefreshRecipeList();
        ClearDetail();
    }

    public CocktailRecipeData GetSelectedRecipe()
    {
        return selectedRecipe;
    }

    private void OpenPanel()
    {
        if (recipeUnlockManager == null)
            recipeUnlockManager = FindFirstObjectByType<RecipeUnlockManager>();

        if (recipeUnlockManager == null)
        {
            Debug.LogWarning("[RecipeGuidePanel] Cannot open: RecipeUnlockManager is null");
            return;
        }

        if (panelRoot != null)
            panelRoot.SetActive(true);

        RefreshRecipeList();
    }

    private void ClosePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void RefreshRecipeList()
    {
        ClearRecipeButtons();

        if (recipeUnlockManager == null || recipeListParent == null || recipeButtonPrefab == null)
            return;

        List<CocktailRecipeData> recipes = recipeUnlockManager.GetUnlockedRecipesByMethod(currentMethod);

        foreach (var recipe in recipes)
        {
            if (recipe == null) continue;

            Button btn = Instantiate(recipeButtonPrefab, recipeListParent);
            spawnedButtons.Add(btn);

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = recipe.displayName;

            CocktailRecipeData capturedRecipe = recipe;
            btn.onClick.AddListener(() => SelectRecipe(capturedRecipe));
        }
    }

    private void SelectRecipe(CocktailRecipeData recipe)
    {
        selectedRecipe = recipe;
        RefreshDetail();
        RefreshButtonVisuals();
    }

    private void RefreshDetail()
    {
        if (selectedRecipe == null)
        {
            ClearDetail();
            return;
        }

        if (recipeNameText != null)
            recipeNameText.text = selectedRecipe.displayName;

        if (recipeMethodText != null)
            recipeMethodText.text = $"Method: {selectedRecipe.method}";

        if (recipeIngredientText != null)
            recipeIngredientText.text = BuildIngredientText(selectedRecipe);
    }

    private string BuildIngredientText(CocktailRecipeData recipe)
    {
        if (recipe == null || recipe.ingredients == null || recipe.ingredients.Length == 0)
            return "재료 정보 없음";

        System.Text.StringBuilder sb = new();

        for (int i = 0; i < recipe.ingredients.Length; i++)
        {
            RecipeIngredientEntry entry = recipe.ingredients[i];
            if (entry == null) continue;

            sb.AppendLine($"{entry.category} / {entry.ingredientId} / ratio {entry.ratio}");
        }

        return sb.ToString();
    }

    private void ClearDetail()
    {
        if (recipeNameText != null)
            recipeNameText.text = "레시피 선택";

        if (recipeMethodText != null)
            recipeMethodText.text = "";

        if (recipeIngredientText != null)
            recipeIngredientText.text = "선택한 레시피의 기주 / 리큐르 / 음료 비율이 표시됩니다.";
    }

    private void RefreshButtonVisuals()
    {
        foreach (var btn in spawnedButtons)
        {
            if (btn == null) continue;

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label == null) continue;

            bool selected = selectedRecipe != null && label.text == selectedRecipe.displayName;
            btn.image.color = selected ? new Color(1f, 0.9f, 0.6f, 1f) : Color.white;
        }
    }

    private void ClearRecipeButtons()
    {
        foreach (var btn in spawnedButtons)
        {
            if (btn != null)
                Destroy(btn.gameObject);
        }

        spawnedButtons.Clear();
    }
}