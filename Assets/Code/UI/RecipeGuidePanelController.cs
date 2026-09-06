using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeGuidePanelController : MonoBehaviour
{
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

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private bool showOpenButtonOnAwake = true;

    [Header("Recipe List")]
    [SerializeField] private Transform recipeListParent;
    [SerializeField] private Button recipeButtonPrefab;

    [Header("Recipe Detail")]
    [SerializeField] private TMP_Text recipeNameText;
    [SerializeField] private TMP_Text recipeMethodText;
    [SerializeField] private TMP_Text recipeIngredientText;

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = false;

    private readonly List<Button> spawnedButtons = new();
    private readonly Dictionary<Button, CocktailRecipeData> buttonRecipes = new();
    private MiniGameType currentMethod;
    private CocktailRecipeData selectedRecipe;
    private bool missingTitleWarningLogged;
    private RecipeGuideContext recipeGuideContext;
    private Action<string, string> firstRecipeRevealRequested;
    private bool missingContextWarningLogged;

    private void Awake()
    {
        ResolveDetailTextReferences();

        if (openButton != null)
        {
            openButton.onClick.AddListener(OpenPanel);
            openButton.gameObject.SetActive(showOpenButtonOnAwake);
        }

        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Initialize(
        RecipeGuideContext context,
        Action<string, string> onFirstRecipeRevealRequested)
    {
        recipeGuideContext = context;
        firstRecipeRevealRequested = onFirstRecipeRevealRequested;
        currentMethod = context != null ? context.Method : MiniGameType.Stir;
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
        if (recipeGuideContext == null)
        {
            if (!missingContextWarningLogged)
            {
                Debug.LogWarning("[RecipeGuidePanel] Cannot open without a RecipeGuideContext.");
                missingContextWarningLogged = true;
            }

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

        if (recipeGuideContext == null || recipeListParent == null || recipeButtonPrefab == null)
        {
            Debug.LogWarning(
                $"[RecipeGuidePanel] Cannot refresh list. " +
                $"Context={(recipeGuideContext != null ? "OK" : "NULL")} / " +
                $"ListParent={(recipeListParent != null ? "OK" : "NULL")} / " +
                $"ButtonPrefab={(recipeButtonPrefab != null ? "OK" : "NULL")}"
            );
            return;
        }

        if (verboseLogging)
        {
            IReadOnlyList<CocktailRecipeData> allRecipes = recipeGuideContext.AvailableRecipes;
            Debug.Log($"[RecipeGuidePanel] RefreshRecipeList -> method={currentMethod}, unlockedRecipes={allRecipes.Count}");

            foreach (CocktailRecipeData recipe in allRecipes)
            {
                if (recipe == null)
                {
                    Debug.Log("[RecipeGuidePanel] Recipe=NULL");
                    continue;
                }

                Debug.Log(
                    $"[RecipeGuidePanel] Recipe Candidate -> " +
                    $"name={GetCocktailDisplayName(recipe)}, id={recipe.recipeId}, method={recipe.method}, " +
                    $"methodMatch={recipe.method == currentMethod}"
                );
            }
        }

        IReadOnlyList<CocktailRecipeData> recipes = recipeGuideContext.AvailableRecipes;
        int instantiatedCount = 0;

        foreach (var recipe in recipes)
        {
            if (recipe == null) continue;

            Button btn = Instantiate(recipeButtonPrefab, recipeListParent);
            spawnedButtons.Add(btn);

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = GetCocktailDisplayName(recipe);
                ConfigureRecipeButtonText(label);
            }

            ConfigureRecipeButtonFrame(btn);
            buttonRecipes[btn] = recipe;

            CocktailRecipeData capturedRecipe = recipe;
            btn.onClick.AddListener(() => SelectRecipe(capturedRecipe));
            instantiatedCount++;
        }

        RefreshButtonVisuals();

        if (verboseLogging)
        {
            Debug.Log(
                $"[RecipeGuidePanel] RefreshRecipeList complete -> " +
                $"method={currentMethod}, unlockedForMethod={recipes.Count}, buttons={instantiatedCount}, " +
                $"parent={recipeListParent.name}, parentActive={recipeListParent.gameObject.activeInHierarchy}"
            );
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
        ResolveDetailTextReferences();

        if (selectedRecipe == null)
        {
            ClearDetail();
            return;
        }

        if (recipeNameText != null)
            recipeNameText.text = GetCocktailDisplayName(selectedRecipe);

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

        CocktailData cocktail = GetTargetCocktail(recipe);
        if (cocktail != null && !string.IsNullOrWhiteSpace(cocktail.description))
        {
            sb.AppendLine(cocktail.description.Trim());
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

            if (entry.ingredient == null)
            {
                Debug.LogError($"[RecipeGuidePanel] Recipe '{recipe.recipeId}' has a null ingredient at index {i}.");
                sb.AppendLine("Missing Ingredient Data");
                continue;
            }

            sb.AppendLine($"{entry.ingredient.displayName} {entry.ratio:0.##} oz");
        }

        return sb.ToString();
    }

    private static CocktailData GetTargetCocktail(CocktailRecipeData recipe)
    {
        if (recipe != null && recipe.targetCocktail != null)
            return recipe.targetCocktail;

        Debug.LogError($"[RecipeGuidePanel] Recipe '{recipe?.recipeId ?? "NULL"}' has no targetCocktail.");
        return null;
    }

    private static string GetCocktailDisplayName(CocktailRecipeData recipe)
    {
        CocktailData cocktail = GetTargetCocktail(recipe);
        return cocktail != null && !string.IsNullOrWhiteSpace(cocktail.cocktailName)
            ? cocktail.cocktailName
            : "Missing Cocktail Data";
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
        if (recipeGuideContext == null ||
            !recipeGuideContext.IsForcedFirstCustomerRouteActive ||
            firstRecipeRevealRequested == null)
            return;

        firstRecipeRevealRequested.Invoke(FirstRecipeRevealTitle, FirstRecipeRevealMessage);
    }
}
