using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Narrow I8 UI adapter for choosing and visibly placing one garnish on the
/// actual Serving Glass. It creates only runtime proof UI; no prefab, garnish
/// inventory, recipe requirement, or persistent decoration data is introduced.
/// </summary>
public sealed class ServingGlassGarnishController : MonoBehaviour
{
    [SerializeField, Min(.01f)] private float placementSeconds = .35f;
    [SerializeField] private Vector2 pickupOffset = new Vector2(-180f, 110f);
    public bool IsPlacing { get; private set; }
    private BartendingContainerState servingGlass;
    private BartendingPreparationTrace preparationTrace;
    private RectTransform visualAnchor;
    private TMP_FontAsset fontAsset;

    private RectTransform fallbackServingGlass;
    private RectTransform garnishMarker;
    private Image garnishMarkerImage;
    private TMP_Text garnishMarkerText;
    private GameObject selectionPanel;
    private TMP_Text selectionText;

    private bool selectionActive;
    private bool expectedGarnishRequired;
    private BartendingGarnishId selectedGarnish = BartendingGarnishId.LemonTwist;
    private BartendingGarnishPlacement selectedPlacement = BartendingGarnishPlacement.Rim;

    public BartendingGarnishState CurrentGarnish => servingGlass != null ? servingGlass.Garnish : null;
    public bool IsSelectionActive => selectionActive;
    public bool IsExpectedGarnishRequired => expectedGarnishRequired;

    /// <summary>
    /// The controller owns only the visible garnish affordance. The existing
    /// preparation evaluator remains authoritative for actual success.
    /// </summary>
    public void SetExpectedRecipe(CocktailRecipeData recipe)
    {
        expectedGarnishRequired = RecipePreparationEvaluator.HasRequiredAction(
            recipe,
            RecipePreparationAction.Garnish);
        RefreshSelectionText();
    }

    /// <summary>
    /// Binds this short-lived view to the per-session Serving Glass. A supplied
    /// visual anchor is used when a route has a Serving Glass graphic; otherwise
    /// a clearly labelled runtime placeholder makes the final-glass state
    /// inspectable without prefab reauthoring.
    /// </summary>
    public void Initialize(
        BartendingContainerState servingGlassState,
        RectTransform servingGlassVisualAnchor,
        TMP_Text fontSource,
        BartendingPreparationTrace trace = null)
    {
        servingGlass = servingGlassState;
        preparationTrace = trace;
        fontAsset = fontSource != null ? fontSource.font : TMP_Settings.defaultFontAsset;
        visualAnchor = servingGlassVisualAnchor != null
            ? servingGlassVisualAnchor
            : EnsureFallbackServingGlass();

        EnsurePresentationObjects();
        selectionActive = false;
        SetSelectionPanelVisible(false);
        RefreshPresentation();
    }

    public void BeginSelection()
    {
        if (servingGlass == null)
            return;

        EnsurePresentationObjects();
        selectionActive = true;
        SetSelectionPanelVisible(true);
        RefreshSelectionText();
        RefreshPresentation();
    }

    public void EndSelection()
    {
        selectionActive = false;
        SetSelectionPanelVisible(false);
        RefreshPresentation();
    }

    /// <summary>
    /// The direct API used by route-local proof seams and by the keyboard UI.
    /// BartendingContainerState remains the only owner of the resulting state.
    /// </summary>
    public bool TryApplyGarnish(
        BartendingGarnishId garnishId,
        BartendingGarnishPlacement placement)
    {
        if (IsPlacing || servingGlass == null || servingGlass.CurrentLiquidVolumeMl <= 0f ||
            !BartendingGarnishState.IsValid(garnishId) || !BartendingGarnishState.IsValid(placement)) return false;
        var shelf = GetComponentInChildren<IngredientShelfController>(true);
        if (shelf != null && !shelf.ToolsReady) return false;
        if (Application.isPlaying && visualAnchor != null)
        {
            IsPlacing = true;
            StartCoroutine(PlaceGarnish(garnishId, placement));
            return true;
        }
        return ApplyPlacedGarnish(garnishId, placement);
    }

    private IEnumerator PlaceGarnish(BartendingGarnishId garnishId, BartendingGarnishPlacement placement)
    {
        var held = BartendingPhysicalPresentation.Shape("HeldGarnish", visualAnchor,
            GetGarnishSize(garnishId), GetGarnishColor(garnishId));
        PixelBartendingArt.Apply(held, garnishId.ToString());
        PixelBartendingArt.Grip(held, new Vector2(18f, 10f), .8f);
        Vector2 destination = GetPlacementPosition(placement);
        for (float t = 0f; t < placementSeconds; t += BartendingPhysicalPresentation.MotionDeltaTime)
        {
            held.anchoredPosition = Vector2.Lerp(destination + pickupOffset, destination,
                Mathf.SmoothStep(0f, 1f, t / placementSeconds));
            yield return null;
        }
        held.gameObject.SetActive(false);
        Destroy(held.gameObject);
        ApplyPlacedGarnish(garnishId, placement);
        IsPlacing = false;
    }

    private bool ApplyPlacedGarnish(BartendingGarnishId garnishId, BartendingGarnishPlacement placement)
    {
        if (servingGlass == null || servingGlass.CurrentLiquidVolumeMl <= 0f ||
            !servingGlass.TryApplyGarnish(garnishId, placement))
            return false;

        selectedGarnish = garnishId;
        selectedPlacement = placement;
        preparationTrace?.RecordActiveGarnish(servingGlass.Garnish, BartendingContainerType.ServingGlass);
        RefreshPresentation();
        RefreshSelectionText();

        Debug.Log($"[Garnish] Apply id={garnishId} placement={placement} servingMl={servingGlass.CurrentLiquidVolumeMl:0.###}");
        return true;
    }

    public bool ClearGarnish()
    {
        if (IsPlacing) return false;
        if (servingGlass == null || !servingGlass.ClearGarnish())
            return false;

        preparationTrace?.ClearActiveGarnish();
        RefreshPresentation();
        RefreshSelectionText();
        Debug.Log("[Garnish] Cleared ServingGlass garnish");
        return true;
    }

    private void Update()
    {
        if (RecipeGuidePanelController.IsTechniqueReadStateActive || DemoAudioController.IsSettingsOpen)
            return;

        if (!selectionActive)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            selectedGarnish = BartendingGarnishId.LemonTwist;
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            selectedGarnish = BartendingGarnishId.Cherry;
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            selectedGarnish = BartendingGarnishId.Olive;
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            selectedGarnish = BartendingGarnishId.OrangeSlice;
        else if (Input.GetKeyDown(KeyCode.Alpha5))
            selectedGarnish = BartendingGarnishId.LemonSlice;
        else if (Input.GetKeyDown(KeyCode.Alpha6))
            selectedGarnish = BartendingGarnishId.OrangeZest;
        else if (Input.GetKeyDown(KeyCode.Q))
            selectedPlacement = BartendingGarnishPlacement.Rim;
        else if (Input.GetKeyDown(KeyCode.W))
            selectedPlacement = BartendingGarnishPlacement.InGlass;
        else if (Input.GetKeyDown(KeyCode.E))
            selectedPlacement = BartendingGarnishPlacement.Surface;
        else if (Input.GetKeyDown(KeyCode.G))
            TryApplyGarnish(selectedGarnish, selectedPlacement);
        else if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Backspace))
            ClearGarnish();

        RefreshSelectionText();
    }

    private RectTransform EnsureFallbackServingGlass()
    {
        if (fallbackServingGlass != null)
            return fallbackServingGlass;

        RectTransform parent = transform.Find("UI") as RectTransform;
        if (parent == null)
            parent = transform as RectTransform;

        if (parent == null)
            return null;

        GameObject fallback = CreateUiObject("RuntimeServingGlassPlaceholder", parent);
        fallbackServingGlass = fallback.GetComponent<RectTransform>();
        fallbackServingGlass.anchorMin = new Vector2(0.77f, 0.5f);
        fallbackServingGlass.anchorMax = new Vector2(0.77f, 0.5f);
        fallbackServingGlass.pivot = new Vector2(0.5f, 0.5f);
        fallbackServingGlass.sizeDelta = new Vector2(132f, 156f);

        Image background = fallback.AddComponent<Image>();
        background.color = new Color(0.08f, 0.12f, 0.16f, 0.72f);

        TMP_Text title = CreateText("Title", fallbackServingGlass, "서빙 잔", 15f);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -8f);
        titleRect.sizeDelta = new Vector2(124f, 26f);

        return fallbackServingGlass;
    }

    private void EnsurePresentationObjects()
    {
        if (visualAnchor == null || garnishMarker != null)
            return;

        GameObject marker = CreateUiObject("RuntimeGarnishMarker", visualAnchor);
        garnishMarker = marker.GetComponent<RectTransform>();
        garnishMarker.anchorMin = new Vector2(0.5f, 0.5f);
        garnishMarker.anchorMax = new Vector2(0.5f, 0.5f);
        garnishMarker.pivot = new Vector2(0.5f, 0.5f);
        garnishMarkerImage = marker.AddComponent<Image>();

        garnishMarkerText = CreateText("Label", garnishMarker, string.Empty, 11f);
        RectTransform labelRect = garnishMarkerText.rectTransform;
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.sizeDelta = new Vector2(96f, 34f);

        RectTransform panelParent = transform.Find("UI") as RectTransform;
        if (panelParent == null)
            panelParent = transform as RectTransform;
        if (panelParent == null)
            return;

        selectionPanel = CreateUiObject("RuntimeGarnishSelection", panelParent);
        RectTransform panelRect = selectionPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 18f);
        panelRect.sizeDelta = new Vector2(510f, 102f);
        Image panelImage = selectionPanel.AddComponent<Image>();
        panelImage.color = new Color(0.04f, 0.06f, 0.08f, 0.85f);

        selectionText = CreateText("Instructions", panelRect, string.Empty, 14f);
        RectTransform selectionRect = selectionText.rectTransform;
        selectionRect.anchorMin = Vector2.zero;
        selectionRect.anchorMax = Vector2.one;
        selectionRect.offsetMin = new Vector2(10f, 6f);
        selectionRect.offsetMax = new Vector2(-10f, -6f);
        selectionText.alignment = TextAlignmentOptions.Center;
    }

    private void RefreshPresentation()
    {
        if (garnishMarker == null || garnishMarkerImage == null || garnishMarkerText == null)
            return;

        BartendingGarnishState garnish = CurrentGarnish;
        garnishMarker.gameObject.SetActive(garnish != null);
        if (garnish == null)
            return;

        garnishMarker.anchoredPosition = GetPlacementPosition(garnish.Placement);
        garnishMarker.sizeDelta = GetGarnishSize(garnish.GarnishId);
        garnishMarkerImage.color = GetGarnishColor(garnish.GarnishId);
        PixelBartendingArt.Apply(garnishMarker, garnish.GarnishId.ToString());
        // The existing selection panel supplies the label; keep text off the drink.
        garnishMarkerText.text = string.Empty;
    }

    private void RefreshSelectionText()
    {
        if (selectionText == null)
            return;

        string current = CurrentGarnish == null
            ? "none"
            : CurrentGarnish.DisplayName + " / " + GetPlacementLabel(CurrentGarnish.Placement);
        string requirement = expectedGarnishRequired ? "필수" : "선택";
        selectionText.text =
            "가니시 (" + requirement + ") 1: 레몬 트위스트 · 2: 체리 · 3: 올리브\n4: 오렌지 슬라이스 · 5: 레몬 슬라이스 · 6: 오렌지 제스트\n" +
            "Q: 테두리 · W: 잔 안 · E: 표면 · G: 올리기 · X: 빼기\n" +
            "고른 가니시: " + BartendingGarnishState.GetDisplayName(selectedGarnish) +
            " / " + GetPlacementLabel(selectedPlacement) + "   올린 가니시: " + current;
    }

    private void SetSelectionPanelVisible(bool visible)
    {
        if (selectionPanel != null)
            selectionPanel.SetActive(visible);
    }

    private TMP_Text CreateText(string name, RectTransform parent, string value, float fontSize)
    {
        GameObject textObject = CreateUiObject(name, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = fontAsset != null ? fontAsset : TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject value = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        value.transform.SetParent(parent, false);
        return value;
    }

    private Vector2 GetPlacementPosition(BartendingGarnishPlacement placement)
    {
        float height = visualAnchor != null ? Mathf.Max(60f, visualAnchor.rect.height) : 120f;
        var physical = GetComponent<BartendingPhysicalPresentation>();
        if (physical != null && servingGlass != null && placement != BartendingGarnishPlacement.Rim)
        {
            Vector2 surface = visualAnchor.InverseTransformPoint(physical.GetLiquidSurfaceWorld(servingGlass));
            return placement == BartendingGarnishPlacement.Surface ? surface :
                Vector2.Lerp(new Vector2(0f, -height * .35f), surface, .45f);
        }
        switch (placement)
        {
            case BartendingGarnishPlacement.Rim:
                return new Vector2(visualAnchor.rect.width * .36f, height * .45f);
            case BartendingGarnishPlacement.Surface:
                return new Vector2(0f, height * 0.12f);
            default:
                return new Vector2(0f, -height * 0.08f);
        }
    }

    private static Vector2 GetGarnishSize(BartendingGarnishId garnishId)
    {
        var sprite = PixelBartendingArt.Get(garnishId.ToString());
        if (sprite != null) return sprite.rect.size * 1.4f;
        switch (garnishId)
        {
            case BartendingGarnishId.LemonTwist:
                return new Vector2(34f, 12f);
            case BartendingGarnishId.Cherry:
                return new Vector2(20f, 20f);
            case BartendingGarnishId.OrangeSlice:
                return new Vector2(30f, 20f);
            case BartendingGarnishId.LemonSlice:
                return new Vector2(28f, 18f);
            case BartendingGarnishId.OrangeZest:
                return new Vector2(36f, 10f);
            default:
                return new Vector2(22f, 18f);
        }
    }

    private static Color GetGarnishColor(BartendingGarnishId garnishId)
    {
        switch (garnishId)
        {
            case BartendingGarnishId.LemonTwist:
                return new Color(1f, 0.84f, 0.18f, 1f);
            case BartendingGarnishId.Cherry:
                return new Color(0.75f, 0.08f, 0.14f, 1f);
            case BartendingGarnishId.OrangeSlice:
            case BartendingGarnishId.OrangeZest:
                return new Color(1f, 0.46f, 0.08f, 1f);
            case BartendingGarnishId.LemonSlice:
                return new Color(1f, 0.9f, 0.25f, 1f);
            default:
                return new Color(0.42f, 0.58f, 0.16f, 1f);
        }
    }

    private static string GetPlacementLabel(BartendingGarnishPlacement placement)
    {
        switch (placement)
        {
            case BartendingGarnishPlacement.InGlass:
                return "In Glass";
            default:
                return placement.ToString();
        }
    }
}
