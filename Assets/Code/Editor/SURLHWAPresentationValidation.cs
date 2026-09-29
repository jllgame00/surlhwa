#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public static class SURLHWAPresentationValidation
{
    [MenuItem("SURLHWA/Validation/Run Character Presentation References")]
    public static void RunCharacters()
    {
        string[] prefabs =
        {
            "Assets/_SURLHWA/Prefabs/Customers/SpecialGuests/LittlePrinceCustomer.prefab",
            "Assets/_SURLHWA/Prefabs/Customers/General/Customer_A.prefab",
            "Assets/Assets/Prefab/Character/Customer_B.prefab"
        };
        string[] names = { "LittlePrince", "OrdinaryCoat", "OrdinaryPonytail" };
        for (int i = 0; i < prefabs.Length; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[i]);
            Require(prefab != null, "Missing existing guest prefab.");
            var guest = prefab.GetComponent<CustomerController>();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                SURLHWAModularBarImport.CharacterRoot + names[i] + ".png");
            Require(sprite != null && guest != null && guest.standSprite == sprite &&
                guest.sitSprite == sprite && prefab.GetComponent<SpriteRenderer>().sprite == sprite,
                "Character is not connected to both movement and seating presentation.");
            Require(sprite.bounds.size.y <= 2.76f && sprite.bounds.size.y >= 2.6f &&
                sprite.pixelsPerUnit == 32 && sprite.pivot.y == 0,
                "Character must use the reference-derived furniture scale and feet pivot.");
            var serialized = new SerializedObject(guest);
            Require(serialized.FindProperty("interactionAnchor").objectReferenceValue != null,
                "Guest interaction anchor is missing.");
            Texture2D png = new(2, 2);
            try
            {
                Require(ImageConversion.LoadImage(png, System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite))),
                    "Draft PNG cannot decode.");
                Require(png.GetPixel(0, 0).a == 0 && png.GetPixel(png.width - 1, png.height - 1).a == 0,
                    "Draft must retain its transparent background.");
            }
            finally { UnityEngine.Object.DestroyImmediate(png); }
        }
        foreach (var guid in AssetDatabase.FindAssets("t:CampaignEncounterData"))
        {
            var encounter = AssetDatabase.LoadAssetAtPath<CampaignEncounterData>(AssetDatabase.GUIDToAssetPath(guid));
            if (!encounter.IsSpecial) continue;
            Require(encounter.characterSprite != null, "Current special guest has no authored character sprite.");
            if (encounter.sharedGlass || encounter.visit == CampaignVisitId.LoversDeparture)
                Require(encounter.companionSprite != null && encounter.companionSprite != encounter.characterSprite,
                    "Lovers must retain distinct character silhouettes.");
        }
        Debug.Log("[Character Presentation Validation] PASS: three distinct prefab sprites, current campaign cast, " +
            "world scale, interaction anchors and source alpha. Final art/pose approval remains human-owned.");
    }
    private static void Require(bool condition, string detail)
    {
        if (!condition) throw new InvalidOperationException(detail);
    }
}
#endif
