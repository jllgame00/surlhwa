using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeGuidePanelController : MonoBehaviour
{
    private const string FirstRecipeRevealShownKey = "SURLHWA_FIRST_RECIPE_REVEAL_SHOWN";
    private const string FirstRecipeRevealTitle = "레시피북의 첫 장";
    private const string FirstRecipeRevealMessage = "차가운 밤을 데울 한 잔이 조용히 기록되었습니다.";

    [Header("Managers")]
    [SerializeField] private RecipeUnlockManager recipeUnlockManager;
    [SerializeField] private InteractionUIController interactionUIController;
    [SerializeField] private ResultToastController resultToast;

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
        TryShowFirstRecipeReveal();
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

            sb.AppendLine($"{FormatIngredientName(entry.ingredientId)} {entry.ratio:0.##} oz");
        }

        return sb.ToString();
    }

    private static string FormatIngredientName(string ingredientId)
    {
        if (string.IsNullOrWhiteSpace(ingredientId))
            return "Unknown";

        string[] words = ingredientId.Split(new[] { '_', '-' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length == 0)
                continue;

            string lower = words[i].ToLowerInvariant();
            words[i] = char.ToUpperInvariant(lower[0]) + lower.Substring(1);
        }

        return string.Join(" ", words);
    }

    private void ClearDetail()
    {
        if (recipeNameText != null)
            recipeNameText.text = "레시피 선택";

        if (recipeMethodText != null)
            recipeMethodText.text = "";

        if (recipeIngredientText != null)
            recipeIngredientText.text = "선택한 레시피의 재료 용량(oz)이 표시됩니다.";
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

    private void TryShowFirstRecipeReveal()
    {
        if (PlayerPrefs.GetInt(FirstRecipeRevealShownKey, 0) == 1)
            return;

        if (!IsForcedFirstCustomerRouteActive())
            return;

        if (resultToast == null)
            resultToast = FindFirstObjectByType<ResultToastController>();

        if (resultToast == null)
        {
            Debug.LogWarning("[RecipeGuidePanel] Cannot show first recipe reveal: ResultToastController not found");
            return;
        }

        resultToast.Show(FirstRecipeRevealTitle, FirstRecipeRevealMessage);

        PlayerPrefs.SetInt(FirstRecipeRevealShownKey, 1);
        PlayerPrefs.Save();

        Debug.Log("[RecipeGuidePanel] First recipe reveal shown");
    }

    private bool IsForcedFirstCustomerRouteActive()
    {
        if (interactionUIController == null)
            interactionUIController = FindFirstObjectByType<InteractionUIController>();

        return interactionUIController != null && interactionUIController.IsForcedFirstCustomerRouteActive;
    }

    public static void ClearSavedFirstRecipeRevealFlag()
    {
        PlayerPrefs.DeleteKey(FirstRecipeRevealShownKey);
        PlayerPrefs.Save();

        Debug.Log("[RecipeGuidePanel] Cleared first recipe reveal flag");
    }

    [ContextMenu("Debug/Clear First Recipe Reveal Flag")]
    private void ClearFirstRecipeRevealFlagForDebug()
    {
        ClearSavedFirstRecipeRevealFlag();
    }
}
