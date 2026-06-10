using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeGuidePanelController : MonoBehaviour
{
    private const string FirstRecipeRevealShownKey = "SURLHWA_FIRST_RECIPE_REVEAL_SHOWN";
    private const string FirstRecipeRevealTitle = "레시피북의 첫 장";
    private const string FirstRecipeRevealMessage = "차가운 밤을 데울 한 잔이 조용히 기록되었습니다.";
    private const float RecipeButtonWidth = 240f;
    private const float RecipeButtonHeight = 54f;
    private static readonly Color RecipeButtonNormalColor = new(1f, 0.96f, 0.86f, 0.18f);
    private static readonly Color RecipeButtonHighlightedColor = new(1f, 0.93f, 0.72f, 0.28f);
    private static readonly Color RecipeButtonPressedColor = new(1f, 0.86f, 0.55f, 0.36f);
    private static readonly Color RecipeButtonSelectedColor = new(1f, 0.88f, 0.55f, 0.38f);
    private static readonly Color RecipeButtonNormalTextColor = new(0.22f, 0.18f, 0.13f, 1f);
    private static readonly Color RecipeButtonSelectedTextColor = new(0.1f, 0.08f, 0.05f, 1f);

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
    private readonly Dictionary<Button, CocktailRecipeData> buttonRecipes = new();
    private MiniGameType currentMethod;
    private CocktailRecipeData selectedRecipe;
    private bool missingTitleWarningLogged;

    private void Awake()
    {
        ResolveDetailTextReferences();

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
        {
            Debug.LogWarning(
                $"[RecipeGuidePanel] Cannot refresh list. " +
                $"UnlockManager={(recipeUnlockManager != null ? "OK" : "NULL")} / " +
                $"ListParent={(recipeListParent != null ? "OK" : "NULL")} / " +
                $"ButtonPrefab={(recipeButtonPrefab != null ? "OK" : "NULL")}"
            );
            return;
        }

        List<CocktailRecipeData> allRecipes = recipeUnlockManager.GetAllRecipes();
        Debug.Log($"[RecipeGuidePanel] RefreshRecipeList -> method={currentMethod}, totalRecipes={allRecipes.Count}");

        foreach (CocktailRecipeData recipe in allRecipes)
        {
            if (recipe == null)
            {
                Debug.Log("[RecipeGuidePanel] Recipe=NULL");
                continue;
            }

            bool unlocked = recipeUnlockManager.IsUnlocked(recipe);
            Debug.Log(
                $"[RecipeGuidePanel] Recipe Candidate -> " +
                $"name={recipe.displayName}, id={recipe.recipeId}, method={recipe.method}, " +
                $"unlocked={unlocked}, methodMatch={recipe.method == currentMethod}"
            );
        }

        List<CocktailRecipeData> recipes = recipeUnlockManager.GetUnlockedRecipesByMethod(currentMethod);
        int instantiatedCount = 0;

        foreach (var recipe in recipes)
        {
            if (recipe == null) continue;

            Button btn = Instantiate(recipeButtonPrefab, recipeListParent);
            spawnedButtons.Add(btn);

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = recipe.displayName;
                ConfigureRecipeButtonText(label);
            }

            ConfigureRecipeButtonFrame(btn);
            buttonRecipes[btn] = recipe;

            CocktailRecipeData capturedRecipe = recipe;
            btn.onClick.AddListener(() => SelectRecipe(capturedRecipe));
            instantiatedCount++;
        }

        RefreshButtonVisuals();

        Debug.Log(
            $"[RecipeGuidePanel] RefreshRecipeList complete -> " +
            $"method={currentMethod}, unlockedForMethod={recipes.Count}, buttons={instantiatedCount}, " +
            $"parent={recipeListParent.name}, parentActive={recipeListParent.gameObject.activeInHierarchy}"
        );
    }

    private void SelectRecipe(CocktailRecipeData recipe)
    {
        selectedRecipe = recipe;
        RefreshDetail();
        RefreshButtonVisuals();
    }

    private void RefreshDetail()
    {
        ResolveDetailTextReferences();

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
        if (recipe == null)
            return "재료 정보 없음";

        System.Text.StringBuilder sb = new();

        if (!string.IsNullOrWhiteSpace(recipe.description))
        {
            sb.AppendLine(recipe.description.Trim());
            sb.AppendLine();
        }

        if (recipe.ingredients == null || recipe.ingredients.Length == 0)
        {
            if (sb.Length > 0)
                return sb.ToString().TrimEnd();

            return "재료 정보 없음";
        }

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

            bool selected = selectedRecipe != null
                && buttonRecipes.TryGetValue(btn, out CocktailRecipeData recipe)
                && recipe == selectedRecipe;

            if (btn.image != null)
                btn.image.color = selected ? RecipeButtonSelectedColor : RecipeButtonNormalColor;

            label.color = selected ? RecipeButtonSelectedTextColor : RecipeButtonNormalTextColor;
            label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
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
        buttonRecipes.Clear();
    }

    private void ConfigureRecipeButtonFrame(Button btn)
    {
        if (btn == null)
            return;

        RectTransform rect = btn.GetComponent<RectTransform>();
        if (rect != null)
            rect.sizeDelta = new Vector2(RecipeButtonWidth, RecipeButtonHeight);

        LayoutElement layout = btn.GetComponent<LayoutElement>();
        if (layout == null)
            layout = btn.gameObject.AddComponent<LayoutElement>();

        layout.minWidth = RecipeButtonWidth;
        layout.preferredWidth = RecipeButtonWidth;
        layout.minHeight = RecipeButtonHeight;
        layout.preferredHeight = RecipeButtonHeight;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;

        if (btn.image != null)
            btn.image.color = RecipeButtonNormalColor;

        ColorBlock colors = btn.colors;
        colors.normalColor = RecipeButtonNormalColor;
        colors.highlightedColor = RecipeButtonHighlightedColor;
        colors.pressedColor = RecipeButtonPressedColor;
        colors.selectedColor = RecipeButtonSelectedColor;
        colors.colorMultiplier = 1f;
        btn.colors = colors;
    }

    private static void ConfigureRecipeButtonText(TMP_Text label)
    {
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.margin = new Vector4(16f, 0f, 16f, 0f);
        label.enableAutoSizing = true;
        label.fontSizeMin = 16f;
        label.fontSizeMax = 24f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.color = RecipeButtonNormalTextColor;
    }

    private void ResolveDetailTextReferences()
    {
        if (recipeNameText != null && recipeNameText != recipeMethodText)
            return;

        TMP_Text fallback = FindChildTextByName("RecipeNameText");
        if (fallback != null)
        {
            recipeNameText = fallback;
            return;
        }

        if (!missingTitleWarningLogged)
        {
            Debug.LogWarning("[RecipeGuidePanel] RecipeNameText reference is missing or points to the method text.");
            missingTitleWarningLogged = true;
        }
    }

    private TMP_Text FindChildTextByName(string objectName)
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text != null && text.gameObject.name == objectName)
                return text;
        }

        return null;
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
