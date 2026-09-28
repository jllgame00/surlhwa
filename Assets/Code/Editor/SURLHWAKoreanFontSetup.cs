#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class SURLHWAKoreanFontSetup
{
    [MenuItem("SURLHWA/Presentation/Ensure Korean Fallback")]
    public static void Run()
    {
        const string folder = "Assets/_SURLHWA/Art/Fonts";
        const string path = folder + "/KoreanFallback.asset";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/_SURLHWA/Art", "Fonts");
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (font == null)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Data/NotoSansKR-Regular.ttf");
            if (source == null) throw new System.InvalidOperationException("Existing Korean font missing.");
            font = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            font.name = "KoreanFallback";
            font.isMultiAtlasTexturesEnabled = true;
            AssetDatabase.CreateAsset(font, path);
            foreach (var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
            AssetDatabase.AddObjectToAsset(font.material, font);
            EditorUtility.SetDirty(font);
        }
        var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset");
        var serialized = new SerializedObject(settings);
        var fallback = serialized.FindProperty("m_fallbackFontAssets");
        bool found = false;
        for (int i = 0; i < fallback.arraySize; i++)
            found |= fallback.GetArrayElementAtIndex(i).objectReferenceValue == font;
        if (!found)
        {
            fallback.InsertArrayElementAtIndex(fallback.arraySize);
            fallback.GetArrayElementAtIndex(fallback.arraySize - 1).objectReferenceValue = font;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[Korean Font] Existing Noto source connected as dynamic global fallback.");
    }
}
#endif
