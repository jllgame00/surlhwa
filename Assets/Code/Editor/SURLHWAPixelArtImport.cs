#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Bounded import policy: never touches existing anchor or unrelated textures.</summary>
public sealed class SURLHWAPixelArtImport : AssetPostprocessor
{
    public const string Root = "Assets/_SURLHWA/Art/Bartending/Pixel/";
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Root, System.StringComparison.Ordinal)) return;
        var texture = (TextureImporter)assetImporter;
        texture.textureType = TextureImporterType.Sprite;
        texture.spriteImportMode = SpriteImportMode.Single;
        texture.spritePixelsPerUnit = 100f;
        texture.filterMode = FilterMode.Point;
        texture.textureCompression = TextureImporterCompression.Uncompressed;
        texture.mipmapEnabled = false;
        texture.alphaIsTransparency = true;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.npotScale = TextureImporterNPOTScale.None;
        var settings = new TextureImporterSettings();
        texture.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        texture.SetTextureSettings(settings);
    }

    [MenuItem("SURLHWA/Art/Rebuild Phase L Sprite References")]
    public static void RebuildReferences()
    {
        const string path = "Assets/Resources/PixelBartendingArt.asset";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        var art = AssetDatabase.LoadAssetAtPath<PixelBartendingArt>(path);
        if (art == null)
        {
            art = ScriptableObject.CreateInstance<PixelBartendingArt>();
            AssetDatabase.CreateAsset(art, path);
        }
        art.sprites = AssetDatabase.FindAssets("t:Sprite", new[] { Root.TrimEnd('/') })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p)
            .Select(AssetDatabase.LoadAssetAtPath<Sprite>).Where(s => s != null).ToArray();
        EditorUtility.SetDirty(art);
        AssetDatabase.SaveAssets();
        System.Type.GetType("UnityEditor.SyncVS,UnityEditor")?.GetMethod("SyncSolution",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.Invoke(null, null);
        Debug.Log("[Phase L] Sprite references rebuilt: " + art.sprites.Length);
    }
}
#endif
