#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Rebuilds one visual prefab. Never saves scenes or changes game state.</summary>
public static class SURLHWAModularBarBuilder
{
    public const string PrefabPath = "Assets/_SURLHWA/Prefabs/Environment/ModularWinterBar.prefab";
    [MenuItem("SURLHWA/Art/Rebuild Modular Winter Bar")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before rebuilding art.");
        var scene = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("ModularWinterBar");
        SceneManager.MoveGameObjectToScene(root, scene);
        var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("a97c105638bdf8b4a8650670310a4cd3"));
        try
        {
            // Furniture uses its authored dimensions at 32 PPU. No camera/UI scaling.
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    Piece(root, "Floor", x * 8, y * 4 - 1, -100, material);
            for (int i = 0; i < 4; i++) Piece(root, "WallPanel", -7.5f + i * 5, 3.5f, -90, material);
            foreach (float x in new[] { -8f, 8f }) Piece(root, "WinterWindow", x, 3.35f, -80, material);
            foreach (float x in new[] { -4.9f, 4.9f })
            {
                Piece(root, "Shelf", x, 3.25f, -70, material);
                Piece(root, "ShelfBottles", x, 3.8f, -65, material);
                Piece(root, "ShelfBottles", x, 2.85f, -65, material);
            }
            foreach (float x in new[] { -2.2f, 0, 2.2f }) Piece(root, "DisplayNiche", x, 3.45f, -65, material);
            Piece(root, "CounterArms", 0, -.125f, 20, material);
            Piece(root, "Bartender", 0, -1.125f, 40, material, true);
            Piece(root, "CounterTop", 0, -.125f, 100, material);
            Piece(root, "CounterFace", 0, -.125f, 110, material);
            Piece(root, "CounterDressing", -4.5f, -.55f, 120, material);
            foreach (float x in new[] { -3.2f, 0, 3.2f }) Piece(root, "Stool", x, -2.55f, 200, material);
            foreach (float x in new[] { -8f, 8f }) Piece(root, "SidePlant", x, -.2f, 15, material);
            foreach (float x in new[] { -5.9f, 5.9f }) Piece(root, "Lantern", x, 3.85f, 150, material);
            // Seven furniture/display anchors correspond to the existing memory order.
            var displays = new (string name, float x, float y)[] {
                ("Prince", -7.8f, 1.2f), ("Lovers", -2.1f, 1.8f),
                ("Quixote", -2.2f, 3.05f), ("Hamlet", 0, 3.05f),
                ("Faust", 2.2f, 3.05f), ("SimCheong", 2.1f, 1.8f),
                ("Odysseus", 7.8f, 1.2f)
            };
            for (int i = 0; i < displays.Length; i++)
            {
                var d = displays[i];
                if (i != 2 && i != 3 && i != 4) Piece(root, "Pedestal", d.x, d.y - .65f, -30, material);
                var anchor = new GameObject("Memory_" + i + "_" + d.name).transform;
                anchor.SetParent(root.transform, false);
                anchor.localPosition = new Vector3(d.x, d.y, 0);
            }
            System.IO.Directory.CreateDirectory("Assets/_SURLHWA/Prefabs/Environment");
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("[Modular Bar] Built independent furniture, central bartender and seven memory anchors.");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
    private static void Piece(GameObject root, string asset, float x, float y, int order, Material material, bool character = false)
    {
        string path = (character ? SURLHWAModularBarImport.CharacterRoot : SURLHWAModularBarImport.EnvironmentRoot) + asset + ".png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new InvalidOperationException("Missing modular sprite: " + path);
        // Unique deterministic names let Unity preserve child file IDs on rebuild.
        string name = character ? asset : asset + "_" + x.ToString(System.Globalization.CultureInfo.InvariantCulture) +
            "_" + y.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
        renderer.transform.SetParent(root.transform, false);
        renderer.transform.localPosition = new Vector3(x, y, 0);
        renderer.sprite = sprite;
        renderer.sharedMaterial = material;
        renderer.sortingOrder = order;
    }
}
#endif
