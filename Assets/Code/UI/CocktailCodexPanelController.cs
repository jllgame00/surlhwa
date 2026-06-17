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
    [SerializeField] private CocktailRecipeData[] recipes;
    [SerializeField] private RecipeUnlockManager recipeUnlockManager;

    [Header("Fallback Visual")]
    [SerializeField] private Sprite placeholderSprite;
    [SerializeField] private Color placeholderColor = new Color(0.18f, 0.17f, 0.15f, 1f);

    private readonly List<GameObject> spawnedCards = new();
    private CocktailRecipeData selectedRecipe;

    private void Awake()
    {
        ResolveRecipeUnlockManager();

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (backButton != null)
            backButton.onClick.AddListener(BackToGrid);

        if (panelRoot != null)
            panelRoot.SetActive(false);

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
            card.Bind(recipe, IsRecipeUnlocked(recipe), SelectRecipe);
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

        if (!IsRecipeUnlocked(recipe))
        {
            ShowLockedDetail();
            return;
        }

        ShowUnlockedDetail(recipe);
    }

    private void ShowUnlockedDetail(CocktailRecipeData recipe)
    {
        SetLockedDetailVisible(false);
        ApplyLargeImage(GetRecipeSprite(recipe));

        if (nameText != null)
            nameText.text = GetRecipeDisplayName(recipe);

        if (descriptionText != null)
            descriptionText.text = GetDescription(recipe);

        if (methodText != null)
            methodText.text = $"Method: {recipe.method}";

        if (ingredientsText != null)
            ingredientsText.text = BuildIngredientText(recipe);
    }

    private void ShowLockedDetail()
    {
        ApplyLargeImage(null);
        SetLockedDetailVisible(true);

        if (nameText != null)
            nameText.text = "???";

        if (descriptionText != null)
            descriptionText.text = "Unlock this cocktail to view its notes.";

        if (methodText != null)
            methodText.text = "Method: ???";

        if (ingredientsText != null)
            ingredientsText.text = "";

        if (lockedDetailText != null)
            lockedDetailText.text = "Locked";
    }

    private void ClearDetail()
    {
        ApplyLargeImage(null);
        SetLockedDetailVisible(false);

        if (nameText != null)
            nameText.text = "Select a cocktail";

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

        if (recipes != null && recipes.Length > 0)
        {
            foreach (CocktailRecipeData recipe in recipes)
            {
                if (recipe != null)
                    result.Add(recipe);
            }

            return result;
        }

        if (recipeUnlockManager != null)
            return recipeUnlockManager.GetAllRecipes();

        return result;
    }

    private bool IsRecipeUnlocked(CocktailRecipeData recipe)
    {
        if (recipe == null)
            return false;

        if (recipeUnlockManager != null)
            return recipeUnlockManager.IsUnlocked(recipe);

        return recipe.unlockedByDefault;
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
        if (recipe == null || recipe.targetCocktail == null)
            return null;

        return recipe.targetCocktail.cocktailSprite;
    }

    private static string GetRecipeDisplayName(CocktailRecipeData recipe)
    {
        if (recipe == null)
            return "Unknown";

        if (!string.IsNullOrWhiteSpace(recipe.displayName))
            return recipe.displayName;

        if (recipe.targetCocktail != null && !string.IsNullOrWhiteSpace(recipe.targetCocktail.cocktailName))
            return recipe.targetCocktail.cocktailName;

        return recipe.name;
    }

    private static string GetDescription(CocktailRecipeData recipe)
    {
        if (recipe == null)
            return "";

        if (!string.IsNullOrWhiteSpace(recipe.description))
            return recipe.description.Trim();

        if (recipe.targetCocktail != null && !string.IsNullOrWhiteSpace(recipe.targetCocktail.description))
            return recipe.targetCocktail.description.Trim();

        return "No description.";
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

            sb.AppendLine($"{FormatIngredientName(entry.ingredientId)} {entry.ratio:0.##} oz");
        }

        return sb.Length > 0
            ? sb.ToString().TrimEnd()
            : "No ingredient data.";
    }

    private static string FormatIngredientName(string ingredientId)
    {
        if (string.IsNullOrWhiteSpace(ingredientId))
            return "Unknown";

        string[] words = ingredientId.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length == 0)
                continue;

            string lower = words[i].ToLowerInvariant();
            words[i] = char.ToUpperInvariant(lower[0]) + lower.Substring(1);
        }

        return string.Join(" ", words);
    }
}
