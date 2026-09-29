using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CocktailCodexPanelController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private GameObject gridPage;
    [SerializeField] private GameObject detailPage;

    [Header("Cards")]
    [SerializeField] private Transform cardParent;
    [SerializeField] private GameObject cardPrefab;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backButton;

    [Header("Detail")]
    [SerializeField] private Image largeImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text methodText;
    [SerializeField] private TMP_Text ingredientsText;
    [SerializeField] private TMP_Text lockedDetailText;

    [Header("Data")]
    [SerializeField] private RecipeCatalogData recipeCatalog;
    [SerializeField] private RecipeUnlockManager recipeUnlockManager;

    [Header("Fallback Visual")]
    [SerializeField] private Sprite placeholderSprite;
    [SerializeField] private Color placeholderColor = new Color(0.18f, 0.17f, 0.15f, 1f);

    private readonly List<GameObject> spawnedCards = new();
    private CocktailRecipeData selectedRecipe;

    private void Awake()
    {
        ResolveRecipeUnlockManager();
        RecipeDetailScrollArea.Ensure(ingredientsText);
        ConfigurePresentation();

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (backButton != null)
            backButton.onClick.AddListener(BackToGrid);

        if (panelRoot != null)
            panelRoot.SetActive(false);
        SurlhwaModalSurface.Configure(panelRoot, 1800, BackOrClose);

        ShowGridPage();
        ClearDetail();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        if (backButton != null)
            backButton.onClick.RemoveListener(BackToGrid);
    }

    private void ConfigurePresentation()
    {
        if (panelRoot == null) return;
        SurlhwaUITheme.Panel(panelRoot); SurlhwaUITheme.Tree(panelRoot);
        // The legacy detail page contained a second, unbound close button.
        // Both pages now share the controller's one wired close action.
        foreach (var button in panelRoot.GetComponentsInChildren<Button>(true))
            if (button.name == "CloseButton" && button != closeButton) button.gameObject.SetActive(false);
        SurlhwaUITheme.Place(panelRoot.GetComponent<RectTransform>(), new Vector2(1000, 632), Vector2.zero);
        SurlhwaUITheme.Label("CodexHeading", panelRoot.transform, "기억한 한 잔", 26, new Vector2(400, 40), new Vector2(-210, 270));
        Button[] navigation = { backButton, closeButton };
        for (int i = 0; i < navigation.Length; i++)
        {
            if (navigation[i] == null) continue;
            navigation[i].transform.SetParent(panelRoot.transform, false);
            SurlhwaUITheme.Place(navigation[i].GetComponent<RectTransform>(), new Vector2(104, 44), new Vector2(280 + i * 116, 270));
        }
        SurlhwaUITheme.LabelButton(backButton, "목록으로"); SurlhwaUITheme.LabelButton(closeButton, "닫기");
        if (detailPage != null)
        {
            var oldBacking = detailPage.transform.Find("InfoArea")?.GetComponent<Image>();
            if (oldBacking != null) { oldBacking.enabled = false; oldBacking.raycastTarget = false; }
            SurlhwaUITheme.Place(detailPage.GetComponent<RectTransform>(), new Vector2(944, 510), new Vector2(0, -30));
            if (largeImage != null)
            {
                largeImage.transform.SetParent(detailPage.transform, false);
                SurlhwaUITheme.Place(largeImage.rectTransform, new Vector2(260, 330), new Vector2(-286, 10));
                largeImage.preserveAspect = true; largeImage.raycastTarget = false;
            }
            DetailLabel(nameText, 28, new Vector2(500, 48), new Vector2(188, 218));
            DetailLabel(descriptionText, 20, new Vector2(500, 94), new Vector2(188, 140));
            DetailLabel(methodText, 20, new Vector2(500, 32), new Vector2(188, 70));
            DetailLabel(lockedDetailText, 22, new Vector2(500, 210), new Vector2(188, 20));
            if (ingredientsText != null)
            {
                var viewport = ingredientsText.transform.parent as RectTransform;
                viewport.SetParent(detailPage.transform, false);
                SurlhwaUITheme.Place(viewport, new Vector2(500, 280), new Vector2(188, -94));
                SurlhwaUITheme.Text(ingredientsText, 20);
            }
        }
        if (cardParent != null)
        {
            SurlhwaUITheme.Place(gridPage.GetComponent<RectTransform>(), new Vector2(944, 510), new Vector2(0, -30));
            var oldTitle = gridPage.transform.Find("TitleText");
            if (oldTitle != null) oldTitle.gameObject.SetActive(false);
            var scroll = cardParent.GetComponentInParent<ScrollRect>(true);
            if (scroll != null)
            {
                SurlhwaUITheme.Place(scroll.GetComponent<RectTransform>(), new Vector2(900, 490), Vector2.zero);
                scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.scrollSensitivity = 40;
                if (scroll.viewport != null && scroll.viewport.GetComponent<RectMask2D>() == null) scroll.viewport.gameObject.AddComponent<RectMask2D>();
            }
            var grid = cardParent.GetComponent<GridLayoutGroup>();
            if (grid != null) { grid.cellSize = new Vector2(148, 184); grid.spacing = new Vector2(16, 16); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 5; }
        }
    }
    private void DetailLabel(TMP_Text text, float size, Vector2 area, Vector2 position)
    {
        if (text == null) return;
        text.transform.SetParent(detailPage.transform, false);
        SurlhwaUITheme.Text(text, size); text.enableAutoSizing = false;
        SurlhwaUITheme.Place(text.rectTransform, area, position);
        text.alignment = TextAlignmentOptions.TopLeft;
    }

    public void Open()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
        }
        else
        {
            transform.SetAsLastSibling();
        }

        selectedRecipe = null;
        ShowGridPage();
        ClearDetail();
        Refresh();
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void BackOrClose()
    {
        if (detailPage != null && detailPage.activeSelf) BackToGrid();
        else Close();
    }

    public void BackToGrid()
    {
        ShowGridPage();
    }

    public void Refresh()
    {
        ResolveRecipeUnlockManager();
        ClearCards();

        List<CocktailRecipeData> sourceRecipes = GetRecipeSource();

        foreach (CocktailRecipeData recipe in sourceRecipes)
        {
            if (recipe == null)
                continue;

            if (cardParent == null || cardPrefab == null)
            {
                Debug.LogWarning("[CocktailCodex] Card parent or card prefab is missing.");
                break;
            }

            GameObject cardObject = Instantiate(cardPrefab, cardParent);
            cardObject.SetActive(true);

            CocktailCodexCardView card = cardObject.GetComponent<CocktailCodexCardView>();
            if (card == null)
            {
                Debug.LogWarning("[CocktailCodex] Card prefab is missing CocktailCodexCardView.");
                Destroy(cardObject);
                continue;
            }

            spawnedCards.Add(cardObject);
            card.Bind(recipe, GetRecipeKnowledge(recipe), SelectRecipe);
        }
    }

    public void SelectRecipe(CocktailRecipeData recipe)
    {
        selectedRecipe = recipe;

        if (recipe == null)
        {
            ClearDetail();
            return;
        }

        ShowDetailPage();

        RecipeKnowledgeFacts facts = GetRecipeKnowledge(recipe);
        if (facts.State == RecipeKnowledgeState.Unknown)
        {
            ShowUnknownDetail();
            return;
        }

        if (facts.State == RecipeKnowledgeState.Partial)
        {
            ShowPartialDetail(recipe, facts);
            return;
        }

        ShowKnownDetail(recipe, facts);
    }

    private void ShowKnownDetail(CocktailRecipeData recipe, RecipeKnowledgeFacts facts)
    {
        SetLockedDetailVisible(false);
        ApplyLargeImage(GetRecipeSprite(recipe));

        if (nameText != null)
            nameText.text = GetRecipeDisplayName(recipe);

        if (descriptionText != null)
            descriptionText.text = GetDescription(recipe);

        if (methodText != null)
            methodText.text = RecipeKnowledgeGuideFormatter.GetMethodText(recipe, facts);

        if (ingredientsText != null)
            ingredientsText.text = RecipeKnowledgeGuideFormatter.BuildDetailText(
                recipe,
                facts,
                includeKnownDescription: false);
        RecipeDetailScrollArea.ResetToTop(ingredientsText);
    }

    private void ShowPartialDetail(CocktailRecipeData recipe, RecipeKnowledgeFacts facts)
    {
        SetLockedDetailVisible(false);
        ApplyLargeImage(null);

        if (nameText != null)
            nameText.text = RecipeKnowledgeGuideFormatter.GetDetailName(recipe, facts);

        if (descriptionText != null)
            descriptionText.text = string.Empty;

        if (methodText != null)
            methodText.text = RecipeKnowledgeGuideFormatter.GetMethodText(recipe, facts);

        if (ingredientsText != null)
            ingredientsText.text = RecipeKnowledgeGuideFormatter.BuildDetailText(recipe, facts);
        RecipeDetailScrollArea.ResetToTop(ingredientsText);
    }

    private void ShowUnknownDetail()
    {
        ApplyLargeImage(null);
        SetLockedDetailVisible(true);

        if (nameText != null)
            nameText.text = "???";

        if (descriptionText != null)
            descriptionText.text = "아직 배우지 않은 칵테일입니다.";

        if (methodText != null)
            methodText.text = "조주법: ???";

        if (ingredientsText != null)
            ingredientsText.text = "";

        if (lockedDetailText != null)
            lockedDetailText.text = "미확인";
    }

    private void ClearDetail()
    {
        ApplyLargeImage(null);
        SetLockedDetailVisible(false);

        if (nameText != null)
            nameText.text = "칵테일을 선택하세요";

        if (descriptionText != null)
            descriptionText.text = "";

        if (methodText != null)
            methodText.text = "";

        if (ingredientsText != null)
            ingredientsText.text = "";
    }

    private void ClearCards()
    {
        foreach (GameObject card in spawnedCards)
        {
            if (card != null)
                Destroy(card);
        }

        spawnedCards.Clear();
    }

    private void ShowGridPage()
    {
        if (backButton != null) backButton.gameObject.SetActive(false);
        if (gridPage != null)
            gridPage.SetActive(true);

        if (detailPage != null)
            detailPage.SetActive(false);
    }

    private void ShowDetailPage()
    {
        if (backButton != null) backButton.gameObject.SetActive(true);
        if (gridPage != null)
            gridPage.SetActive(false);

        if (detailPage != null)
            detailPage.SetActive(true);
    }

    private List<CocktailRecipeData> GetRecipeSource()
    {
        List<CocktailRecipeData> result = new();

        if (recipeCatalog != null)
        {
            foreach (RecipeCatalogEntry entry in recipeCatalog.Entries)
            {
                if (entry?.Recipe != null)
                    result.Add(entry.Recipe);
            }

            return result;
        }

        if (recipeUnlockManager != null)
            return recipeUnlockManager.GetAllRecipes();

        return result;
    }

    private RecipeKnowledgeFacts GetRecipeKnowledge(CocktailRecipeData recipe)
    {
        if (recipe == null)
            return new RecipeKnowledgeFacts(false, false, false, false, false, null, null);

        if (recipeUnlockManager != null)
            return recipeUnlockManager.GetKnownFacts(recipe);

        bool knownByDefault = recipeCatalog != null && recipeCatalog.IsUnlockedByDefault(recipe);
        return new RecipeKnowledgeFacts(knownByDefault, false, false, false, false, null, null);
    }

    private void ResolveRecipeUnlockManager()
    {
        if (recipeUnlockManager == null)
            recipeUnlockManager = FindFirstObjectByType<RecipeUnlockManager>();
    }

    private void ApplyLargeImage(Sprite sprite)
    {
        if (largeImage == null)
            return;

        if (sprite != null)
        {
            largeImage.enabled = true;
            largeImage.sprite = sprite;
            largeImage.color = Color.white;
            return;
        }

        largeImage.enabled = true;
        largeImage.sprite = placeholderSprite;
        largeImage.color = placeholderColor;
    }

    private void SetLockedDetailVisible(bool visible)
    {
        if (lockedDetailText != null)
            lockedDetailText.gameObject.SetActive(visible);
    }

    private static Sprite GetRecipeSprite(CocktailRecipeData recipe)
    {
        CocktailData cocktail = GetTargetCocktail(recipe);
        return cocktail != null ? cocktail.cocktailSprite : null;
    }

    private static string GetRecipeDisplayName(CocktailRecipeData recipe)
    {
        CocktailData cocktail = GetTargetCocktail(recipe);
        return cocktail != null && !string.IsNullOrWhiteSpace(cocktail.cocktailName)
            ? cocktail.cocktailName
            : "Missing Cocktail Data";
    }

    private static string GetDescription(CocktailRecipeData recipe)
    {
        CocktailData cocktail = GetTargetCocktail(recipe);
        return cocktail != null && !string.IsNullOrWhiteSpace(cocktail.description)
            ? cocktail.description.Trim()
            : "아직 기록된 이야기가 없습니다.";
    }

    private static CocktailData GetTargetCocktail(CocktailRecipeData recipe)
    {
        if (recipe != null && recipe.targetCocktail != null)
            return recipe.targetCocktail;

        Debug.LogError($"[CocktailCodex] Recipe '{recipe?.recipeId ?? "NULL"}' has no targetCocktail.");
        return null;
    }

    private static string BuildIngredientText(CocktailRecipeData recipe)
    {
        if (recipe == null || recipe.ingredients == null || recipe.ingredients.Length == 0)
            return "No ingredient data.";

        System.Text.StringBuilder sb = new();

        foreach (RecipeIngredientEntry entry in recipe.ingredients)
        {
            if (entry == null)
                continue;

            if (entry.ingredient == null)
            {
                Debug.LogError($"[CocktailCodex] Recipe '{recipe.recipeId}' has a null ingredient reference.");
                sb.AppendLine("Missing Ingredient Data");
                continue;
            }

            sb.AppendLine($"{entry.ingredient.displayName} {entry.ratio:0.##} oz");
        }

        return sb.Length > 0
            ? sb.ToString().TrimEnd()
            : "No ingredient data.";
    }

}
