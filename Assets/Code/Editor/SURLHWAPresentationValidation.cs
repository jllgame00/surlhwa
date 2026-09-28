#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public static class SURLHWAPresentationValidation
{
    [MenuItem("SURLHWA/Validation/Run Character Draft References")]
    public static void RunCharacters()
    {
        string[] prefabs =
        {
            "Assets/_SURLHWA/Prefabs/Customers/SpecialGuests/LittlePrinceCustomer.prefab",
            "Assets/_SURLHWA/Prefabs/Customers/General/Customer_A.prefab",
            "Assets/Assets/Prefab/Character/Customer_B.prefab"
        };
        string[] names = { "LittlePrince", "WinterGuestA", "WinterGuestB" };
        for (int i = 0; i < prefabs.Length; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[i]);
            Require(prefab != null, "Missing existing guest prefab.");
            var guest = prefab.GetComponent<CustomerController>();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/_SURLHWA/Art/Characters/" + names[i] + "-Draft.png");
            Require(sprite != null && guest != null && guest.standSprite == sprite &&
                guest.sitSprite == sprite && prefab.GetComponent<SpriteRenderer>().sprite == sprite,
                "Character draft is not connected to both movement and seating presentation.");
            Require(sprite.bounds.size.y <= 2.01f && sprite.bounds.size.y >= 1.5f,
                "Character import changed the established two-unit world scale.");
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
        Debug.Log("[Character Draft Validation] PASS: three distinct sprites, existing prefab references, " +
            "world scale, interaction anchors and source alpha. Final art/pose approval remains human-owned.");
    }
    private static void Require(bool condition, string detail)
    {
        if (!condition) throw new InvalidOperationException(detail);
    }
}
#endif
