#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Only the new modular world and chibi sprites use this world pixel grid.</summary>
public sealed class SURLHWAModularBarImport : AssetPostprocessor
{
    public const string EnvironmentRoot = "Assets/_SURLHWA/Art/Environment/ModularBar/";
    public const string CharacterRoot = "Assets/_SURLHWA/Art/Characters/Chibi/";
    private void OnPreprocessTexture()
    {
        bool character = assetPath.StartsWith(CharacterRoot, System.StringComparison.Ordinal);
        if (!character && !assetPath.StartsWith(EnvironmentRoot, System.StringComparison.Ordinal)) return;
        var texture = (TextureImporter)assetImporter;
        texture.textureType = TextureImporterType.Sprite;
        texture.spriteImportMode = SpriteImportMode.Single;
        texture.spritePixelsPerUnit = 32;
        texture.filterMode = FilterMode.Point;
        texture.textureCompression = TextureImporterCompression.Uncompressed;
        texture.mipmapEnabled = false;
        texture.alphaIsTransparency = true;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.npotScale = TextureImporterNPOTScale.None;
        var settings = new TextureImporterSettings();
        texture.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(.5f, character ? 0 : .5f);
        texture.SetTextureSettings(settings);
    }
}
#endif
