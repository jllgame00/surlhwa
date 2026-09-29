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
        if (gridPage != null)
            gridPage.SetActive(true);

        if (detailPage != null)
            detailPage.SetActive(false);
    }

    private void ShowDetailPage()
    {
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
