using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Surlhwa.Presentation;
using Object = UnityEngine.Object;
namespace Surlhwa.Editor
{
    public static class PixelCutawayBuilder
    {
        private static bool polish;
        private static string Root => polish ? "Assets/Surlhwa/VariantC1" : "Assets/Surlhwa/VariantC";
        private static readonly Quaternion View = Quaternion.Euler(28, 0, 0);
        private static Transform world;
        private static Mesh boxMesh;
        private static Material darkWood, wood, panel, top, floor, rear, snow, stone, snowCap, metal, brass, amber, contact;
        private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var c); return c; }
        [MenuItem("Surlhwa/Build C pixel cutaway only")]
        public static void Build() => BuildScene(false);
        [MenuItem("Surlhwa/Build C1 bounded polish only")]
        public static void BuildC1() => BuildScene(true);
        private static void BuildScene(bool c1)
        {
            polish = c1;
            Directory.CreateDirectory(Root); AssetDatabase.Refresh(); ContentBundleGenerator.Sync();
            darkWood = Mat("DarkWood", "32231e"); wood = Mat("WoodEdge", "775032");
            panel = PixelMat("CounterPanel", 192, 40, "35231d", "493025", "67452e", 0);
            top = PixelMat("CounterGrain", 192, 48, "70472d", "8b5937", "a06c41", 1);
            floor = PixelMat("FloorPlanks", 384, 192, "3d2b25", "4b352a", "5b3f2e", 2);
            rear = PixelMat("RearPanels", 384, 100, "392a25", "493126", "684730", 3);
            snow = PixelMat("SnowField", 512, 384, "182e47", "203a52", "2d4a60", 4);
            stone = PixelMat("StoneFooting", 96, 24, "28323d", "37404b", "485363", 5);
            snowCap = polish ? PixelMat("SnowCap", 128, 32, "7e909d", "a8b5bb", "cad1d1", 6)
                : PixelMat("SnowCap", 128, 32, "627c8f", "8198a5", "a5b6bb", 6);
            metal = Mat("Iron", "252b32"); brass = Mat("Brass", "b28145"); amber = Mat("Lamplight", "ffd997");
            contact = Mat("Contact", "2c2325");
            boxMesh = MakeBoxMesh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            world = new GameObject(polish ? "C1_PixelCutawayWorld" : "C_PixelCutawayWorld").transform;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = Color.white; RenderSettings.fog = false;
            Box("C_SnowGround", new Vector3(0, -.22f, .8f), new Vector3(24, .2f, 24), snow, snow, snow);
            Box("C_Floor", new Vector3(0, -.09f, .4f), new Vector3(9.6f, .18f, 6.1f), floor, darkWood, darkWood);
            Box("C_RearPlane", new Vector3(0, 1.02f, 3.46f), new Vector3(9.6f, 2.04f, .06f), rear, rear, rear);
            Box("C_BackBeam", new Vector3(0, 2.09f, 3.45f), new Vector3(9.85f, .14f, .2f), wood, darkWood, darkWood);
            Box("C_BackSnow", new Vector3(0, 2.20f, 3.45f), new Vector3(9.95f, .1f, .32f), snowCap, snowCap, snowCap);
            // Only low foundations flank the bar. No enclosing side walls or roof.
            foreach (float x in new[] { -4.88f, 4.88f }) {
                Box("C_PerimeterStone", new Vector3(x, .12f, .4f), new Vector3(.2f, .32f, 6.35f), stone, stone, stone);
                Box("C_PerimeterSnow", new Vector3(x, .31f, .4f), new Vector3(.36f, .09f, 6.5f), snowCap, snowCap, snowCap);
            }
            Box("C_FrontFooting", new Vector3(0, -.01f, -2.72f), new Vector3(9.95f, .12f, .24f), stone, stone, stone);
            foreach (float x in new[] { -3.5f, 3.5f }) Box("C_FrontSnow", new Vector3(x, .08f, -2.77f), new Vector3(2.85f, .08f, .38f), snowCap, snowCap, snowCap);
            // Sparse stepped accumulation makes snow a world perimeter, rather than a blue window.
            var rng = new System.Random(731);
            for (int i = 0; i < 75; i++) {
                bool side = i % 2 == 0; float sign = i % 4 < 2 ? -1 : 1;
                float x = side ? sign * (5.05f + (float)rng.NextDouble() * .85f) : (float)rng.NextDouble() * 10 - 5;
                float z = side ? (float)rng.NextDouble() * 7 - 3 : sign < 0 ? -3.1f - (float)rng.NextDouble() * .5f : 4.2f + (float)rng.NextDouble();
                float size = rng.Next(2, 6) * .06f;
                Box("C_SnowAccumulation", new Vector3(x, -.06f, z), new Vector3(size, .08f, size), snowCap, stone, stone, false);
            }
            Counter(); BackDress();
            float guestPPU = polish ? 70 : 64, seatZ = polish ? -1.83f : -1.42f;
            float seatY = 26f / guestPPU * Mathf.Cos(28 * Mathf.Deg2Rad);
            foreach (float x in new[] { -2.2f, -.55f, 1.15f, 2.4f }) Stool(new Vector3(x, 0, seatZ), seatY, x == -.55f);
            var bartender = Actor("C_Bartender", "guest_bartender", "visual_bartender", new Vector3(.2f, 0, polish ? .55f : .78f), "Bartender", "Bartender", new Vector2(17, 0), new Vector2(17, 0), polish ? 52 : 56, Vector3.zero);
            var ordinary = Actor("C_Ordinary", "guest_ordinary_postman", "visual_ordinary", new Vector3(-.55f - 12f / guestPPU, 0, seatZ - Mathf.Sin(28 * Mathf.Deg2Rad) * 26 / guestPPU), "OrdinaryStanding", "OrdinarySeated", new Vector2(17.5f, 0), new Vector2(38, 15), guestPPU, new Vector3(12f / guestPPU, 26f / guestPPU, 0));
            var special = Actor("C_Quixote", "guest_quixote", "visual_quixote", new Vector3(-.55f - 18f / guestPPU, 0, seatZ - Mathf.Sin(28 * Mathf.Deg2Rad) * 26 / guestPPU), "QuixoteStanding", "QuixoteSeated", new Vector2(20, 0), new Vector2(28, 15), guestPPU, new Vector3(18f / guestPPU, 26f / guestPPU, 0));
            ordinary.WalkMeshes = new Mesh[4]; ordinary.WalkMaterials = new Material[4];
            for (int i = 0; i < 4; i++) { ordinary.WalkMeshes[i] = Plane("OrdinaryWalk" + i, new Vector2(48, i == 0 ? 1 : 0), guestPPU); ordinary.WalkMaterials[i] = Sprite("OrdinaryWalk" + i); }
            var camera = new GameObject("Camera_C_PixelCutaway").AddComponent<Camera>(); camera.transform.rotation = View;
            camera.orthographic = true; camera.aspect = 16f / 9; camera.nearClipPlane = .1f; camera.farClipPlane = 60;
            camera.backgroundColor = Hex("101e30"); camera.clearFlags = CameraClearFlags.SolidColor; camera.allowMSAA = false; camera.allowHDR = false;
            var data = camera.GetUniversalAdditionalCameraData(); data.renderPostProcessing = false; data.antialiasing = AntialiasingMode.None;
            // Sparse static snow marks for capture consistency. These are world quads, not a screen filter.
            var flake = Mat("SnowFlakes", "8ba1b1");
            for (int i = 0; i < 95; i++) {
                float x = (float)rng.NextDouble() * 19 - 9.5f, z = (float)rng.NextDouble() * 17 - 7;
                if (Mathf.Abs(x) < 5.1f && z > -3 && z < 4.4f) continue;
                var mark = GameObject.CreatePrimitive(PrimitiveType.Quad); mark.name = "C_SnowfallMark"; mark.transform.SetParent(world);
                mark.transform.position = new Vector3(x, .7f + (float)rng.NextDouble(), z); mark.transform.rotation = View;
                mark.transform.localScale = Vector3.one * (i % 3 == 0 ? .045f : .025f); mark.GetComponent<Renderer>().sharedMaterial = flake;
                Object.DestroyImmediate(mark.GetComponent<Collider>());
            }
            Anchor("C_WorkbenchAnchor", new Vector3(.85f, 1.015f, polish ? -.6f : -.3f));
            Anchor("C_MemoryDisplayAnchor", new Vector3(0, 1.55f, 3.35f));
            Anchor("C_EntryAnchor", new Vector3(-4.2f, 0, -2.25f)); Anchor("C_ExitAnchor", new Vector3(-5.6f, 0, -3));
            var stage = new GameObject(polish ? "C1_Review" : "C_Review").AddComponent<PixelCutawayStage>(); stage.Camera = camera;
            stage.Bartender = bartender; stage.Ordinary = ordinary; stage.Quixote = special;
            stage.ContentBundle = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/GeneratedContent/spatial-proof.json");
            stage.gameObject.AddComponent<PixelCutawayRun>(); stage.Select(3);
            EditorSceneManager.SaveScene(scene, polish ? "Assets/Surlhwa/Scenes/Bar_C1.unity" : "Assets/Surlhwa/Scenes/Bar_C.unity");
            foreach (string guid in AssetDatabase.FindAssets("", new[] { Root })) AssetDatabase.SaveAssetIfDirty(new GUID(guid));
            Debug.Log("PIXEL_CUTAWAY_" + (polish ? "C1" : "C") + "_BUILT: separate revision assets");
        }
        private static void Counter()
        {
            float frontZ = polish ? -.65f : -.35f;
            Box("C_CounterFront", new Vector3(0, .43f, frontZ), new Vector3(polish ? 7.75f : 6.7f, .86f, polish ? .82f : .72f), wood, panel, darkWood);
            Box("C_CounterTop", new Vector3(0, .925f, frontZ), new Vector3(polish ? 8.0f : 6.95f, .13f, polish ? 1.05f : .94f), top, wood, darkWood);
            Box("C_CounterFootRail", new Vector3(0, .16f, frontZ - (polish ? .55f : .46f)), new Vector3(polish ? 7.2f : 6.15f, .045f, .04f), brass, brass, metal, false);
            foreach (float x in polish ? new[] { -3.45f, 3.45f } : new[] { -3.2f, 3.2f }) {
                Box("C_CounterReturn", new Vector3(x, .43f, polish ? 1 : 1.3f), new Vector3(polish ? .95f : .7f, .86f, polish ? 3.6f : 2.65f), wood, panel, darkWood);
                Box("C_CounterReturnTop", new Vector3(x, .925f, polish ? 1 : 1.3f), new Vector3(polish ? 1.1f : .94f, .13f, polish ? 3.7f : 2.75f), top, wood, darkWood);
            }
            foreach (float x in new[] { -2.4f, -1.2f, 0, 1.2f, 2.4f }) {
                Box("C_CounterPanelTrim", new Vector3(x, .42f, frontZ - (polish ? .42f : .37f)), new Vector3(.045f, .68f, .025f), wood, wood, darkWood, false);
            }
            foreach (float x in new[] { -2.5f, 2.5f }) Bottle(new Vector3(x, .99f, frontZ + .04f), .25f, x < 0 ? Mat("GreenGlass", "354c3e") : Mat("BrownGlass", "785130"));
            Box("C_ReservedWorkbenchMat", new Vector3(.85f, .998f, frontZ + .1f), new Vector3(.72f, .013f, .42f), metal, metal, metal, false);
        }
        private static void BackDress()
        {
            var glass = new[] { Mat("BottleOlive", "5e6845"), Mat("BottleAmber", "9b673b"), Mat("BottleBlue", "526877") };
            foreach (float x in new[] { -2.45f, 2.45f }) {
                Box("C_ShelfBacking", new Vector3(x, 1.22f, 3.38f), new Vector3(2.4f, 1.36f, .035f), darkWood, darkWood, darkWood, false);
                foreach (float y in new[] { .66f, 1.26f }) {
                    Box("C_Shelf", new Vector3(x, y, 3.15f), new Vector3(2.45f, .08f, .4f), top, wood, darkWood);
                    for (int i = 0; i < 6; i++) Bottle(new Vector3(x - 1 + i * .38f, y + .04f, 3.15f), .28f + (i % 3) * .04f, glass[i % 3]);
                }
            }
            // Empty reserved memory shelf, deliberately no statue/campaign assertion from the layout image.
            Box("C_EmptyMemoryRecess", new Vector3(0, 1.35f, 3.4f), new Vector3(1.3f, 1.05f, .025f), darkWood, darkWood, darkWood, false);
            Box("C_MemoryShelf", new Vector3(0, 1.04f, 3.13f), new Vector3(1.45f, .09f, .45f), top, wood, darkWood);
            foreach (float x in new[] { -4.15f, 4.15f }) Lantern(new Vector3(x, 1.5f, 3.18f), false);
            foreach (float x in new[] { -5.25f, 5.25f }) Lantern(new Vector3(x, .26f, -2.5f), true);
            // Small cold pane is secondary; the world exterior carries winter.
            var blue = PixelMat("SmallColdPane", 32, 32, "28435d", "385c79", "7294a6", 4);
            Box("C_WindowFrame", new Vector3(4.1f, .72f, 3.35f), new Vector3(.72f, .8f, .12f), darkWood, wood, darkWood);
            Box("C_WindowPane", new Vector3(4.1f, .72f, 3.275f), new Vector3(.58f, .64f, .025f), blue, blue, blue, false);
            Box("C_WindowMullion", new Vector3(4.1f, .72f, 3.25f), new Vector3(.04f, .65f, .04f), wood, wood, wood, false);
        }
        private static void Lantern(Vector3 at, bool outside)
        {
            Box("C_LampBase", at, new Vector3(.26f, .08f, .22f), brass, metal, metal, false);
            Box("C_LampGlass", at + Vector3.up * .19f, new Vector3(.14f, .3f, .13f), amber, amber, brass, false);
            foreach (float x in new[] { -.105f, .105f }) Box("C_LampFrame", at + new Vector3(x, .19f, -.075f), new Vector3(.035f, .34f, .035f), brass, brass, metal, false);
            Box("C_LampCap", at + Vector3.up * .38f, new Vector3(.29f, .06f, .23f), brass, brass, metal, false);
            if (outside) Box("C_LampSnow", at + Vector3.up * .43f, new Vector3(.3f, .04f, .25f), snowCap, snowCap, snowCap, false);
        }
        private static void Bottle(Vector3 at, float height, Material glass)
        {
            Box("C_Bottle", at + Vector3.up * height * .34f, new Vector3(.15f, height * .68f, .13f), glass, glass, glass, false);
            Box("C_BottleNeck", at + Vector3.up * height * .82f, new Vector3(.065f, height * .28f, .065f), glass, glass, glass, false);
            Box("C_BottleCork", at + Vector3.up * height, new Vector3(.075f, .025f, .07f), brass, brass, darkWood, false);
            Box("C_BottleLabel", at + new Vector3(0, height * .35f, -.068f), new Vector3(.10f, height * .24f, .009f), brass, brass, brass, false);
        }
        private static void Stool(Vector3 at, float topY, bool main)
        {
            Box(main ? "C_Stool" : "C_ExtraStool", at + new Vector3(0, topY - .05f, .232f), new Vector3(.50f, .1f, .46f), top, wood, darkWood);
            foreach (float x in new[] { -.18f, .18f }) foreach (float z in new[] { -.14f, .14f })
                Box("C_StoolLeg", at + new Vector3(x, (topY - .1f) / 2, z + .22f), new Vector3(.06f, topY - .1f, .06f), wood, darkWood, darkWood);
            Box("C_StoolBrace", at + new Vector3(0, .12f, .07f), new Vector3(.4f, .04f, .035f), wood, wood, darkWood);
        }
        private static Transform Anchor(string name, Vector3 at, Transform parent = null)
        { var t = new GameObject(name).transform; t.SetParent(parent ?? world); t.position = at; return t; }
        private static CharacterView Actor(string name, string id, string visual, Vector3 at, string standing, string seated, Vector2 sp, Vector2 sit, float ppu, Vector3 hip)
        {
            var actor = new GameObject(name).AddComponent<CharacterView>(); actor.transform.SetParent(world); actor.transform.position = at;
            actor.GuestId = id; actor.VisualBindingId = visual;
            actor.FeetAnchor = Anchor("FeetAnchor", at, actor.transform); actor.SeatAnchor = Anchor("SeatAnchor", at + View * hip, actor.transform);
            Anchor("StandingAnchor", at, actor.transform); Anchor("CounterHandAnchor", at + View * new Vector3(.2f, .9f, 0), actor.transform);
            Anchor("DialogueAnchor", at + Vector3.up * 1.7f, actor.transform);
            var plane = new GameObject(name + "_Plane"); plane.transform.SetParent(actor.transform, false); plane.transform.localRotation = View;
            actor.Plane = plane.AddComponent<MeshFilter>(); actor.Renderer = plane.AddComponent<MeshRenderer>();
            actor.Renderer.shadowCastingMode = ShadowCastingMode.Off; actor.Renderer.receiveShadows = false;
            actor.StandingMesh = Plane(standing, sp, ppu); actor.SeatedMesh = Plane(seated, sit, ppu);
            actor.StandingMaterial = Sprite(standing); actor.SeatedMaterial = Sprite(seated);
            var blob = Box(name + "_Contact", at + new Vector3(.12f, .006f, .06f), new Vector3(.42f, .008f, .18f), contact, contact, contact, false);
            blob.transform.SetParent(actor.transform, true); actor.Contact = blob.GetComponent<Renderer>(); actor.Show(true, name != "C_Bartender"); return actor;
        }
        private static Mesh Plane(string texture, Vector2 pivot, float ppu)
        {
            var image = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Surlhwa/Art/" + texture + ".png");
            float l = -pivot.x / ppu, r = (image.width - pivot.x) / ppu, b = -pivot.y / ppu, t = (image.height - pivot.y) / ppu;
            var mesh = new Mesh { vertices = new[] { new Vector3(l, b, 0), new Vector3(l, t, 0), new Vector3(r, t, 0), new Vector3(r, b, 0) },
                uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right }, triangles = new[] { 0, 1, 2, 0, 2, 3 } };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return Save(mesh, texture + "Mesh.asset");
        }
        private static Material Sprite(string texture)
        {
            var mat = Mat("Sprite_" + texture, "f4e0c4"); mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Surlhwa/Art/" + texture + ".png"));
            mat.SetFloat("_AlphaClip", 1); mat.SetFloat("_Cutoff", .4f); mat.SetFloat("_Cull", 0); mat.SetFloat("_ZWrite", 1);
            mat.EnableKeyword("_ALPHATEST_ON"); mat.renderQueue = 2450; return mat;
        }
        private static Material Mat(string name, string color)
        { var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); m.SetColor("_BaseColor", Hex(color)); return Save(m, name + ".mat"); }
        private static Material PixelMat(string name, int width, int height, string a, string b, string c, int kind)
        {
            var palette = new[] { Hex(a), Hex(b), Hex(c) }; var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            var pixels = new Color[width * height]; var random = new System.Random(27 + kind);
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) {
                int pick = 1;
                if (kind <= 3) {
                    int band = kind == 3 ? x % 24 : y % (kind == 2 ? 16 : 12);
                    pick = band == 0 || band == 1 ? 0 : band == 2 ? 2 : 1;
                    if (kind == 2 && (x + y / 16 * 47) % 96 < 2) pick = 0;
                    if (kind == 0 && (x % 32 < 2 || y < 3 || y > height - 4)) pick = 0;
                    if (random.Next(200) < 3) pick = random.Next(3);
                    if ((x / 7 + y * 13) % 17 == 0 && band > 3) pick = 0;
                } else if (kind == 4) {
                    // Quiet, coarse patches: actual source pixels, no render-target degradation.
                    pick = ((x / 9 * 7 + y / 7 * 11) % 19 < 4) ? 0 : 1;
                    if (random.Next(100) < 5) pick = 2;
                } else if (kind == 5) pick = y % 9 < 2 || (x + y / 9 * 11) % 24 < 2 ? 0 : random.Next(3);
                else pick = y < 5 ? 0 : random.Next(100) < 17 ? 2 : 1;
                Color pixel = palette[pick];
                if (name == "SnowField") {
                    float wx = (x / (float)(width - 1) - .5f) * 24, wz = (y / (float)(height - 1) - .5f) * 24 + .8f;
                    float distance = Mathf.Max(Mathf.Abs(wx) - 4.8f, Mathf.Abs(wz - .4f) - 3.05f);
                    distance += (((x / 11 + y / 9 * 7) % 9) - 4) * .07f;
                    if (polish) {
                        // Broken snow banks on dark ground, with no nested elevation/colour rings.
                        float patch = SnowNoise(x, y, 23) * .7f + SnowNoise(x + 71, y + 19, 9) * .3f;
                        float bank = distance > 0 && distance < .95f ? .52f : .63f;
                        pixel = patch > bank ? Hex("6d808d") : patch > bank - .13f ? Hex("3b4d5b") : Hex("202b35");
                        if (patch > bank + .09f && (x + y * 7) % 29 < 2) pixel = Hex("9aaab4");
                        else if (patch <= bank - .13f && (x / 3 + y / 2) % 17 == 0) pixel = Hex("293540");
                    } else {
                        pixel = distance < .75f ? Hex("60798d") : distance < 1.35f ? Hex("344d65") : distance < 2 ? Hex("243d55") : Hex("182e47");
                        if (random.Next(180) < 2) pixel = Color.Lerp(pixel, Hex("8fa4b5"), .24f);
                    }
                }
                if (name == "RearPanels") {
                    float dx = Mathf.Min(Mathf.Abs(x - width * .068f), Mathf.Abs(x - width * .932f)) / (width * .11f);
                    float dy = Mathf.Abs(y - height * .73f) / (height * .6f);
                    float d = dx * dx + dy * dy;
                    if (d < 1) pixel = Color.Lerp(pixel, Hex("9b6031"), d < .3f ? .28f : .13f);
                }
                pixels[y * width + x] = pixel;
            }
            tex.SetPixels(pixels); tex.Apply(); string path = Root + "/" + name + ".png";
            WritePixelSourceIfChanged(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path); var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.mipmapEnabled = false; importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None; importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
            var mat = Mat(name, "ffffff"); mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path)); return mat;
        }
        private static void WritePixelSourceIfChanged(string path, byte[] bytes)
        {
            if (File.Exists(path) && System.Linq.Enumerable.SequenceEqual(File.ReadAllBytes(path), bytes)) return;
            // Imported textures may be memory-mapped. Never truncate the existing source in place.
            string temporary = path + ".writing-" + Guid.NewGuid().ToString("N");
            try {
                File.WriteAllBytes(temporary, bytes);
                if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
            } finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        private static float SnowNoise(int x, int y, int cellSize)
        {
            int cx = x / cellSize, cy = y / cellSize;
            float fx = Mathf.SmoothStep(0, 1, x % cellSize / (float)cellSize);
            float fy = Mathf.SmoothStep(0, 1, y % cellSize / (float)cellSize);
            float Sample(int a, int b) => (((a * 73856093) ^ (b * 19349663) ^ 83492791) & 0x7fffffff) % 1000 / 999f;
            return Mathf.Lerp(Mathf.Lerp(Sample(cx, cy), Sample(cx + 1, cy), fx),
                Mathf.Lerp(Sample(cx, cy + 1), Sample(cx + 1, cy + 1), fx), fy);
        }
        private static GameObject Box(string name, Vector3 at, Vector3 scale, Material topMat, Material front, Material sides, bool collider = true)
        {
            var go = new GameObject(name); go.transform.SetParent(world); go.transform.position = at; go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = boxMesh; var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { front, sides, sides, sides, topMat, sides }; renderer.shadowCastingMode = ShadowCastingMode.Off;
            if (collider) go.AddComponent<BoxCollider>(); return go;
        }
        private static Mesh MakeBoxMesh()
        {
            var mesh = new Mesh(); var vertices = new Vector3[24]; var uv = new Vector2[24];
            Vector3[] normals = { Vector3.back, Vector3.forward, Vector3.left, Vector3.right, Vector3.up, Vector3.down };
            for (int face = 0; face < 6; face++) {
                Vector3 normal = normals[face], u = face < 2 ? Vector3.right : face < 4 ? Vector3.forward : Vector3.right;
                Vector3 v = face < 4 ? Vector3.up : Vector3.forward;
                // Double-sided material below makes the authored face winding independent of side orientation.
                int o = face * 4; vertices[o] = (normal - u - v) / 2; vertices[o + 1] = (normal - u + v) / 2;
                vertices[o + 2] = (normal + u + v) / 2; vertices[o + 3] = (normal + u - v) / 2;
                uv[o] = Vector2.zero; uv[o + 1] = Vector2.up; uv[o + 2] = Vector2.one; uv[o + 3] = Vector2.right;
            }
            mesh.vertices = vertices; mesh.uv = uv; mesh.subMeshCount = 6;
            for (int face = 0; face < 6; face++) { int o = face * 4; mesh.SetTriangles(new[] { o, o + 1, o + 2, o, o + 2, o + 3 }, face); }
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return Save(mesh, "FaceBox.asset");
        }
        private static T Save<T>(T value, string filename) where T : Object
        {
            value.name = Path.GetFileNameWithoutExtension(filename);
            if (value is Material material) material.SetFloat("_Cull", 0);
            string path = Root + "/" + filename; var old = AssetDatabase.LoadAssetAtPath<T>(path);
            if (old != null) { EditorUtility.CopySerialized(value, old); Object.DestroyImmediate(value); return old; }
            AssetDatabase.CreateAsset(value, path); return value;
        }
        public static void BuildPlayer() => BuildRevisionPlayer(false);
        public static void BuildC1Player() => BuildRevisionPlayer(true);
        private static void BuildRevisionPlayer(bool c1)
        {
            BuildScene(c1); string revision = c1 ? "C1" : "C";
            string output = Path.GetFullPath("../Validation/Builds/Variant" + revision + "/SurlhwaPixelCutaway.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { "Assets/Surlhwa/Scenes/Bar_" + revision + ".unity" },
                locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            File.WriteAllText("../Validation/Manifests/build-" + revision.ToLowerInvariant() + ".json", JsonUtility.ToJson(new BuildEvidence { result = report.summary.result.ToString(),
                errors = report.summary.totalErrors, warnings = report.summary.totalWarnings, bytes = report.summary.totalSize,
                seconds = report.summary.totalTime.TotalSeconds, unity = UnityEngine.Application.unityVersion, output = output }, true));
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("C Player build failed");
            Debug.Log("PIXEL_CUTAWAY_PLAYER_BUILD_SUCCEEDED: " + output);
        }
        [Serializable] private sealed class BuildEvidence { public string result, unity, output; public int errors, warnings; public ulong bytes; public double seconds; }
    }
}
