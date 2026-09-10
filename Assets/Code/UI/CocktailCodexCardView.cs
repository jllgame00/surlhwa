using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CocktailCodexCardView : MonoBehaviour
{
    private static readonly Vector2 DefaultCardSize = new(120f, 150f);
    private static readonly Vector2 DefaultIconSize = new(86f, 86f);
    private static readonly Vector2 DefaultNameSize = new(108f, 42f);
    private static readonly Color CardBackgroundColor = new(0.16f, 0.13f, 0.09f, 0.88f);
    private static readonly Color NameTextColor = new(0.96f, 0.91f, 0.82f, 1f);

    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject lockedOverlay;

    [Header("Fallback Visual")]
    [SerializeField] private Sprite placeholderSprite;
    [SerializeField] private Color placeholderColor = new Color(0.18f, 0.17f, 0.15f, 1f);

    private CocktailRecipeData recipe;
    private Action<CocktailRecipeData> onClick;
    private Image rootImage;

    private void Awake()
    {
        ResolveReferences();
        ApplyDefaultLayout();
    }

    public void Bind(
        CocktailRecipeData recipeData,
        RecipeKnowledgeFacts facts,
        Action<CocktailRecipeData> clickCallback)
    {
        recipe = recipeData;
        onClick = clickCallback;
        facts ??= new RecipeKnowledgeFacts(false, false, false, false, false, null, null);

        ResolveReferences();
        ApplyDefaultLayout();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }

        if (nameText != null)
            nameText.text = RecipeKnowledgeGuideFormatter.GetCardLabel(recipe, facts);

        if (lockedOverlay != null)
            lockedOverlay.SetActive(facts.State == RecipeKnowledgeState.Unknown);

        Sprite sprite = RecipeKnowledgeGuideFormatter.IsFullyKnown(facts)
            ? GetRecipeSprite(recipe)
            : null;
        ApplyIcon(sprite);
        ApplyRootBackground();
        LogBindState(facts);
    }

    private void HandleClick()
    {
        onClick?.Invoke(recipe);
    }

    private void ApplyIcon(Sprite sprite)
    {
        if (iconImage == null)
            return;

        if (sprite != null)
        {
            iconImage.enabled = true;
            iconImage.sprite = sprite;
            iconImage.color = Color.white;
            return;
        }

        iconImage.enabled = true;
        iconImage.sprite = placeholderSprite;
        iconImage.color = placeholderColor;
    }

    private void ResolveReferences()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (rootImage == null)
            rootImage = GetComponent<Image>();

        if (rootImage == null)
        {
            rootImage = gameObject.AddComponent<Image>();
            Debug.LogWarning("[CocktailCodexCard] Root Image was missing and has been added at runtime.");
        }

        if (button != null && button.targetGraphic == null)
            button.targetGraphic = rootImage;

        if (iconImage == null)
            iconImage = FindChildComponentByName<Image>("IconImage");

        if (iconImage == null)
            iconImage = CreateIconImage();

        if (nameText == null)
            nameText = FindChildComponentByName<TMP_Text>("NameText");

        if (nameText == null)
            nameText = CreateNameText();

        if (lockedOverlay == null)
            lockedOverlay = FindChildTransformByName("LockedOverlay")?.gameObject;
    }

    private void ApplyDefaultLayout()
    {
        RectTransform rootRect = GetComponent<RectTransform>();
        EnsureRectSize(rootRect, DefaultCardSize);

        LayoutElement layout = GetComponent<LayoutElement>();
        if (layout == null)
            layout = gameObject.AddComponent<LayoutElement>();

        layout.minWidth = DefaultCardSize.x;
        layout.preferredWidth = DefaultCardSize.x;
        layout.minHeight = DefaultCardSize.y;
        layout.preferredHeight = DefaultCardSize.y;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;

        ApplyRootBackground();

        if (iconImage != null)
        {
            iconImage.enabled = true;
            iconImage.raycastTarget = false;
            RectTransform iconRect = iconImage.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -10f);
            iconRect.sizeDelta = DefaultIconSize;
        }

        if (nameText != null)
        {
            nameText.raycastTarget = false;
            nameText.color = WithAlpha(NameTextColor, 1f);
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 12f;
            nameText.fontSizeMax = 17f;
            nameText.overflowMode = TextOverflowModes.Ellipsis;

            RectTransform nameRect = nameText.rectTransform;
            nameRect.anchorMin = new Vector2(0.5f, 0f);
            nameRect.anchorMax = new Vector2(0.5f, 0f);
            nameRect.pivot = new Vector2(0.5f, 0f);
            nameRect.anchoredPosition = new Vector2(0f, 8f);
            nameRect.sizeDelta = DefaultNameSize;
        }

        if (lockedOverlay != null)
        {
            foreach (Graphic graphic in lockedOverlay.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        }
    }

    private void ApplyRootBackground()
    {
        if (rootImage == null)
            return;

        rootImage.enabled = true;
        rootImage.color = CardBackgroundColor;
        rootImage.raycastTarget = true;

        if (rootImage.sprite == null)
            rootImage.type = Image.Type.Simple;
    }

    private Image CreateIconImage()
    {
        GameObject iconObject = new("IconImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(transform, false);
        return iconObject.GetComponent<Image>();
    }

    private TMP_Text CreateNameText()
    {
        GameObject textObject = new("NameText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(transform, false);
        return textObject.GetComponent<TMP_Text>();
    }

    private T FindChildComponentByName<T>(string childName) where T : Component
    {
        Transform child = FindChildTransformByName(childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private Transform FindChildTransformByName(string childName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child != null && child.name == childName)
                return child;
        }

        return null;
    }

    private static void EnsureRectSize(RectTransform rect, Vector2 defaultSize)
    {
        if (rect == null)
            return;

        Vector2 size = rect.rect.size;
        if (size.x > 0f && size.y > 0f)
            return;

        rect.sizeDelta = defaultSize;
    }

    private void LogBindState(RecipeKnowledgeFacts facts)
    {
        RectTransform rootRect = GetComponent<RectTransform>();
        RectTransform iconRect = iconImage != null ? iconImage.rectTransform : null;
        RectTransform nameRect = nameText != null ? nameText.rectTransform : null;

        string recipeName = RecipeKnowledgeGuideFormatter.GetCardLabel(recipe, facts);
        string recipeId = recipe != null ? recipe.recipeId : "NULL";
        string finalName = nameText != null ? nameText.text : "NULL";
        float nameAlpha = nameText != null ? nameText.color.a : -1f;
        float iconAlpha = iconImage != null ? iconImage.color.a : -1f;
        bool lockedActive = lockedOverlay != null && lockedOverlay.activeSelf;

        Debug.Log(
            $"[CocktailCodexCard] Bind / " +
            $"recipe={recipeName} / recipeId={recipeId} / knowledge={facts.State} / " +
            $"button={(button != null ? "OK" : "NULL")} / " +
            $"iconImage={(iconImage != null ? "OK" : "NULL")} / " +
            $"nameText={(nameText != null ? "OK" : "NULL")} / " +
            $"lockedOverlay={(lockedOverlay != null ? "OK" : "NULL")} / " +
            $"rootSize={FormatRectSize(rootRect)} / " +
            $"iconSize={FormatRectSize(iconRect)} / " +
            $"nameSize={FormatRectSize(nameRect)} / " +
            $"finalName='{finalName}' / nameAlpha={nameAlpha:0.###} / " +
            $"iconAlpha={iconAlpha:0.###} / lockedOverlayActive={lockedActive}"
        );
    }

    private static string FormatRectSize(RectTransform rect)
    {
        if (rect == null)
            return "NULL";

        Vector2 size = rect.rect.size;
        return $"{size.x:0.#}x{size.y:0.#}";
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
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

    private static CocktailData GetTargetCocktail(CocktailRecipeData recipe)
    {
        if (recipe != null && recipe.targetCocktail != null)
            return recipe.targetCocktail;

        Debug.LogError($"[CocktailCodexCard] Recipe '{recipe?.recipeId ?? "NULL"}' has no targetCocktail.");
        return null;
    }
}
