using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Immutable, transient drawing data captured before the bartending view unloads.
/// It cannot transfer liquid, recognize recipes, grant knowledge or restore state.
/// </summary>
public sealed class ServedDrinkAppearance
{
    public readonly struct Band
    {
        public Color Color { get; }
        public float Height { get; }
        public Band(Color color, float height) { Color = color; Height = height; }
    }

    public IReadOnlyList<Band> Bands { get; }
    public bool HasIce { get; }
    public BartendingGarnishState Garnish { get; }
    public string Glass { get; }

    public ServedDrinkAppearance(BartendingContainerState serving, BartendingLayeredDrinkState layers, string glass)
    {
        Glass = glass;
        HasIce = serving.HasIce;
        Garnish = serving.Garnish;
        var bands = new List<Band>();
        float denominator = Mathf.Max(240f, serving.CurrentLiquidVolumeMl);
        if (layers != null && layers.Layers.Count > 0)
            foreach (var layer in layers.Layers)
                bands.Add(new Band(layer.Ingredient.liquidColor, layer.AmountMl / denominator));
        else
            bands.Add(new Band(PixelBartendingArt.Liquid(serving), serving.CurrentLiquidVolumeMl / denominator));
        Bands = bands.AsReadOnly();
    }

    public static ServedDrinkAppearance Capture(GameObject method)
    {
        var shelf = method.GetComponentInChildren<IngredientShelfController>(true);
        if (shelf?.PhysicalPresentation == null) return null;
        shelf.PhysicalPresentation.RefreshState();
        var shell = shelf.PhysicalPresentation.GetVessel(shelf.ServingGlass).Find("PixelShell").GetComponent<Image>();
        return new ServedDrinkAppearance(shelf.ServingGlass, method.GetComponent<FloatMiniGame>()?.LayeredDrinkState,
            shell.sprite != null ? shell.sprite.name : "RocksGlass");
    }

    /// <summary>Consumes WHAT already resolved by OrderManager; never invokes a matcher.</summary>
    public RectTransform Draw(Transform parent, CocktailData recognized)
    {
        string family = recognized == null ? Glass : recognized.glassware switch
        {
            CocktailGlassware.CocktailGlass => "CocktailGlass",
            CocktailGlassware.TallTumbler => "TallGlass",
            CocktailGlassware.ChampagneFlute => "Flute",
            _ => "RocksGlass"
        };
        var root = new GameObject("ServedPixelDrink", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(parent, false);
        var sprite = PixelBartendingArt.Get(family);
        root.sizeDelta = sprite.rect.size;
        float bottom = family == "CocktailGlass" ? .48f : family == "Flute" ? .38f : .09f;
        float top = family == "Flute" ? .95f : .9f;
        var mask = BartendingPhysicalPresentation.Shape("Bowl", root, Vector2.zero, Color.white);
        mask.anchorMin = Vector2.zero; mask.anchorMax = Vector2.one;
        mask.offsetMin = mask.offsetMax = Vector2.zero;
        PixelBartendingArt.Apply(mask, family + "Mask");
        mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        float height = bottom;
        foreach (var band in Bands)
        {
            var fill = BartendingPhysicalPresentation.Shape("ActualLiquid", mask, Vector2.zero,
                Bands.Count == 1 && recognized != null ? recognized.liquidColor : band.Color);
            fill.anchorMin = new Vector2(0f, height);
            height += band.Height * (top - bottom);
            fill.anchorMax = new Vector2(1f, height);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
        }
        var surface = BartendingPhysicalPresentation.Shape("Surface", mask, new Vector2(0f, 1f), new Color(.93f,.9f,.8f,.6f));
        surface.anchorMin = new Vector2(0f,height); surface.anchorMax = new Vector2(1f,height);
        if (HasIce)
        {
            var ice = BartendingPhysicalPresentation.Shape("ActualIce", mask, new Vector2(26f,24f), Color.white);
            PixelBartendingArt.Apply(ice,"IceCluster");
            ice.anchorMin = ice.anchorMax = new Vector2(.5f,bottom+.18f);
        }
        var shell = BartendingPhysicalPresentation.Shape("Glass",root,Vector2.zero,Color.white);
        shell.anchorMin=Vector2.zero; shell.anchorMax=Vector2.one;
        shell.offsetMin=shell.offsetMax=Vector2.zero;
        PixelBartendingArt.Apply(shell,family);
        if (Garnish != null)
        {
            var garnish = BartendingPhysicalPresentation.Shape("ActualGarnish",root,new Vector2(14f,14f),Color.white);
            PixelBartendingArt.Apply(garnish,Garnish.GarnishId.ToString());
            garnish.anchorMin=garnish.anchorMax=Garnish.Placement==BartendingGarnishPlacement.Rim
                ? new Vector2(.83f,.92f) : new Vector2(.5f,Garnish.Placement==BartendingGarnishPlacement.Surface
                    ? height : Mathf.Lerp(bottom,height,.45f));
        }
        return root;
    }
}
