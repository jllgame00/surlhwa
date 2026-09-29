#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SURLHWAPixelArtValidation
{
    [MenuItem("SURLHWA/Validation/Phase L Pixel Presentation")]
    public static void Run()
    {
        var art = Resources.Load<PixelBartendingArt>("PixelBartendingArt");
        Require(art != null && art.sprites.Length == 31, "Sprite library incomplete.");
        foreach (var sprite in art.sprites)
        {
            Require(sprite != null && sprite.rect.width > 0 && sprite.rect.height > 0, "Missing/zero sprite.");
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));
            Require(importer.filterMode == FilterMode.Point && !importer.mipmapEnabled &&
                importer.textureCompression == TextureImporterCompression.Uncompressed &&
                importer.spritePixelsPerUnit == 100f && importer.alphaIsTransparency &&
                importer.spriteImportMode == SpriteImportMode.Single, "Import drift: " + sprite.name);
            Require(importer.spritePivot.x >= 0f && importer.spritePivot.x <= 1f &&
                importer.spritePivot.y >= 0f && importer.spritePivot.y <= 1f, "Invalid pivot.");
        }
        var liquid = AssetDatabase.LoadAssetAtPath<IngredientData>("Assets/_SURLHWA/Data/Ingredients/Ingredient_Cream.asset");
        foreach (string method in new[] { "Build", "Stir", "Shake", "Float" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_SURLHWA/Prefabs/Bartending/" + method + "MiniGame.prefab");
            var root = UnityEngine.Object.Instantiate(prefab);
            try
            {
                Sprite original = root.transform.Find("UI/Shaker")?.GetComponent<Image>()?.sprite;
                root.GetComponent<IMiniGame>().StartGame(_ => { });
                var shelf = root.GetComponentInChildren<IngredientShelfController>(true);
                var view = shelf.PhysicalPresentation;
                // Shelf creates the view only in Play Mode. Exercise the same
                // initializer explicitly for this Editor-only contract check.
                if (view == null)
                {
                    view = root.AddComponent<BartendingPhysicalPresentation>();
                    view.Initialize(root.transform, shelf.PreparationState);
                }
                var jigger = view.GetVessel(shelf.Jigger);
                var shaker = view.GetVessel(shelf.PreparationState.GetContainer(BartendingContainerType.Shaker));
                Require(jigger.rect.height < shaker.rect.height * .5f, "Jigger has working-vessel scale.");
                if (method == "Shake") Require(shaker.Find("PixelShell").GetComponent<Image>().sprite == original,
                    "Approved Shaker was replaced.");
                var fill = view.GetLiquidMask(shelf.Jigger).Find("StateLiquid") as RectTransform;
                Require(!fill.gameObject.activeSelf, "Empty Jigger shows liquid.");
                shelf.Jigger.AddLiquid(liquid, 30f); view.RefreshState();
                float partial = fill.anchorMax.y;
                shelf.Jigger.AddLiquid(liquid, 30f); view.RefreshState();
                Require(fill.anchorMax.y > partial && fill.GetComponent<Image>().color == liquid.liquidColor,
                    "Jigger does not follow measured amount/ingredient tint.");
                view.ObserveSpill(5f);
                Require(jigger.Find("JiggerOverflow").gameObject.activeSelf, "Overflow absent.");
                Require(shelf.Jigger.CurrentLiquidVolumeMl == 60f, "Pixel view changed measurement.");
                foreach (CocktailGlassware family in new[] { CocktailGlassware.CocktailGlass,
                    CocktailGlassware.OldFashionedGlass, CocktailGlassware.TallTumbler, CocktailGlassware.ChampagneFlute })
                {
                    view.SetServingGlass(family);
                    var vessel = view.GetVessel(shelf.ServingGlass);
                    Require(vessel.Find("PixelShell").GetComponent<Image>().sprite != null &&
                        view.GetLiquidMask(shelf.ServingGlass).GetComponent<Mask>() != null,
                        "Glass shell/mask missing.");
                    Require(vessel.rect.width > 0 && vessel.rect.height > 0, "Zero-size glass.");
                }
                Require(!view.Bottle.GetComponent<Image>().raycastTarget, "Art intercepts controls.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        SURLHWAPhysicalBartendingValidation.RunRegression();
        Debug.Log("[Phase L] PASS: sprite/import/pivot contracts, preserved Shaker, proportional Jigger, state fill/overflow, glass masks; existing physical and campaign Editor regressions.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("[Phase L] " + message);
    }
}
#endif
