using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Surlhwa.ContentIO;
using Surlhwa.Presentation;
namespace Surlhwa.Editor
{
    public static class SpatialProofBuilder
    {
        private const string Root = "Assets/Surlhwa";
        private static Transform room;
        private static Material wood, edge, wall, metal, cool, lamp, contact;
        private static readonly Quaternion PlaneRotation = Quaternion.Euler(18, 0, 0);
        [MenuItem("Surlhwa/Rebuild spatial proof")]
        public static void Build()
        {
            foreach (string directory in new[] { "Scenes", "Settings", "Materials", "Meshes" }) Directory.CreateDirectory(Root + "/" + directory);
            ContentBundleGenerator.Sync();
            AssetDatabase.Refresh(); ConfigurePipeline(); ImportArt();
            wood = Material("Wood", new Color(.24f, .115f, .065f));
            edge = Material("CounterEdge", new Color(.38f, .205f, .102f));
            wall = Material("Plaster", new Color(.245f, .225f, .203f));
            metal = Material("DarkMetal", new Color(.095f, .105f, .12f));
            cool = Material("WindowWinter", new Color(.31f, .47f, .62f), true);
            lamp = Material("LampWarm", new Color(1f, .67f, .32f), true);
            contact = Material("Contact", new Color(.055f, .035f, .023f, .32f), true);
            contact.SetFloat("_Surface", 1); contact.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            contact.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); contact.SetFloat("_ZWrite", 0);
            contact.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); contact.renderQueue = 3000;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            room = new GameObject("WinterBar_Blockout").transform;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.34f, .32f, .30f);
            RenderSettings.ambientIntensity = 1; RenderSettings.skybox = null; RenderSettings.fog = false;
            Cube("Floor", new Vector3(0, -.125f, 0), new Vector3(10, .25f, 7), wood);
            for (int i = 0; i < 17; i++) Cube("FloorBoardSeam_" + i, new Vector3(-4.8f + i * .6f, .001f, 0), new Vector3(.012f, .006f, 7), metal, false);
            Cube("RearWall", new Vector3(0, 1.7f, 3.6f), new Vector3(10, 3.4f, .3f), wall);
            Cube("RearBaseboard", new Vector3(0, .15f, 3.39f), new Vector3(10, .3f, .15f), edge);
            Cube("RearBeam", new Vector3(0, 3.3f, 3.35f), new Vector3(10.2f, .22f, .3f), wood);
            Cube("LeftCutaway", new Vector3(-5, .35f, 0), new Vector3(.18f, .7f, 7), wood);
            Cube("RightCutaway", new Vector3(5, .35f, 0), new Vector3(.18f, .7f, 7), wood);
            Cube("Counter", new Vector3(0, .62f, .15f), new Vector3(5.5f, 1.24f, .66f), wood);
            Cube("CounterTop", new Vector3(0, 1.29f, .15f), new Vector3(5.75f, .13f, .9f), edge);
            foreach (float x in new[] { -2.65f, 2.65f }) {
                Cube("CounterReturn", new Vector3(x, .62f, 1.3f), new Vector3(.35f, 1.24f, 2.2f), wood);
                Cube("CounterReturnTop", new Vector3(x, 1.29f, 1.3f), new Vector3(.52f, .13f, 2.3f), edge);
            }
            Cube("CounterFootRail", new Vector3(0, .27f, -.46f), new Vector3(5.35f, .045f, .045f), metal, false);
            Anchor("WorkbenchAnchor", new Vector3(.75f, 1.36f, .4f));
            Anchor("MemoryDisplayAnchor", new Vector3(.4f, 2.3f, 3.25f));
            Cube("MemoryShelf_Empty", new Vector3(.4f, 2.02f, 3.1f), new Vector3(1.25f, .12f, .45f), edge);
            Cube("Shelving", new Vector3(-1.7f, 1.95f, 3.15f), new Vector3(1.8f, 1.8f, .22f), wood);
            foreach (float y in new[] { 1.12f, 1.9f, 2.78f }) Cube("Shelf", new Vector3(-1.7f, y, 2.94f), new Vector3(1.95f, .1f, .55f), edge);
            for (int i = 0; i < 5; i++) Cube("ShelfMass_" + i, new Vector3(-2.35f + i * .31f, 1.4f, 2.98f), new Vector3(.18f, .4f + i % 2 * .1f, .16f), metal, false);
            Cube("DoorFrame", new Vector3(-3.9f, 1.2f, 3.22f), new Vector3(1.35f, 2.4f, .28f), wood);
            Cube("Door", new Vector3(-3.9f, 1.15f, 3.03f), new Vector3(1.05f, 2.25f, .08f), metal);
            Cube("DoorWinterPane", new Vector3(-3.9f, 1.8f, 2.97f), new Vector3(.68f, .65f, .04f), cool, false);
            Anchor("EntryAnchor", new Vector3(-3.9f, 0, 2.5f)); Anchor("ExitAnchor", new Vector3(-3.9f, 0, 2.5f));
            Cube("WinterWindow", new Vector3(3.25f, 2.12f, 3.31f), new Vector3(2.42f, 1.7f, .07f), cool, false);
            var snow = Material("Snow", new Color(.72f, .83f, .9f), true);
            for (int i = 0; i < 27; i++) Cube("Snow_" + i, new Vector3(2.15f + (i * 37 % 23) * .095f, 1.4f + (i * 19 % 13) * .11f, 3.25f), new Vector3(.026f, .026f, .02f), snow, false);
            foreach (float x in new[] { 2f, 3.25f, 4.5f }) Cube("WindowUpright", new Vector3(x, 2.12f, 3.18f), new Vector3(.08f, 1.85f, .12f), wood);
            foreach (float y in new[] { 1.25f, 2.1f, 3f }) Cube("WindowCrossbar", new Vector3(3.25f, y, 3.18f), new Vector3(2.6f, .075f, .12f), wood);
            Cube("WindowSill", new Vector3(3.25f, 1.2f, 3.05f), new Vector3(2.7f, .12f, .45f), edge);
            foreach (float x in new[] { -1.9f, 1.55f }) {
                Cube("LampStem", new Vector3(x, 2.91f, 1), new Vector3(.035f, .6f, .035f), metal, false);
                Cube("LampShade", new Vector3(x, 2.59f, 1), new Vector3(.5f, .11f, .34f), metal, false);
                Cube("Lamp", new Vector3(x, 2.52f, 1), new Vector3(.36f, .05f, .24f), lamp, false);
                LightAt("WarmFill", LightType.Point, new Vector3(x, 2.35f, .7f), new Color(1, .68f, .38f), 2.1f, 4.2f, false);
            }
            var key = LightAt("WarmKey", LightType.Directional, new Vector3(0, 4, -2), new Color(1, .83f, .65f), .85f, 10, true);
            key.transform.rotation = Quaternion.Euler(42, -28, 0);
            LightAt("WindowCoolFill", LightType.Point, new Vector3(3.1f, 2.1f, 2.8f), new Color(.45f, .65f, 1), 1.6f, 3.2f, false);
            Stool(new Vector3(-.7f, 0, -.69f));
            Anchor("StandingAnchor", new Vector3(-.95f, 0, -1.15f));
            Anchor("CounterHandAnchor", new Vector3(-.6f, 1.37f, -.2f)); Anchor("DialogueAnchor", new Vector3(-.9f, 2.15f, -1.15f));
            var stage = new GameObject("SpatialProofReview").AddComponent<ReviewStage>();
            stage.gameObject.AddComponent<ReviewRun>();
            stage.Bartender = Actor("Bartender", "guest_bartender", "visual_bartender", new Vector3(.3f, 0, 1.3f), "Bartender", "Bartender", new Vector2(17, 0), new Vector2(17, 0), Vector3.zero);
            stage.Ordinary = Actor("Ordinary", "guest_ordinary_postman", "visual_ordinary", new Vector3(-.973f, 0, -1.15f), "OrdinaryStanding", "OrdinarySeated", new Vector2(17.5f, 0), new Vector2(38, 15), new Vector3(12f / 44, 26f / 44, 0));
            stage.Quixote = Actor("Quixote", "guest_quixote", "visual_quixote", new Vector3(-1.109f, 0, -1.15f), "QuixoteStanding", "QuixoteSeated", new Vector2(20, 0), new Vector2(28, 15), new Vector3(18f / 44, 26f / 44, 0));
            stage.Orthographic = Camera("Camera_A_Orthographic", true); stage.Perspective = Camera("Camera_B_Perspective", false);
            stage.ContentBundle = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/GeneratedContent/spatial-proof.json");
            stage.WorkbenchAnchor = GameObject.Find("WorkbenchAnchor").transform;
            stage.MemoryDisplayAnchor = GameObject.Find("MemoryDisplayAnchor").transform;
            stage.Perspective.enabled = false;
            stage.Ordinary.WalkMeshes = new Mesh[4]; stage.Ordinary.WalkMaterials = new Material[4];
            for (int i = 0; i < 4; i++) {
                stage.Ordinary.WalkMeshes[i] = PlaneMesh("OrdinaryWalk" + i, new Vector2(48, i == 0 ? 1 : 0));
                stage.Ordinary.WalkMaterials[i] = SpriteMaterial("OrdinaryWalk" + i);
            }
            EditorSceneManager.SaveScene(scene, Root + "/Scenes/Bar.unity");
            var bootstrap = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("ProductionBootstrap").AddComponent<Bootstrap>();
            EditorSceneManager.SaveScene(bootstrap, Root + "/Scenes/Bootstrap.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Root + "/Scenes/Bootstrap.unity", true), new EditorBuildSettingsScene(Root + "/Scenes/Bar.unity", true) };
            AssetDatabase.SaveAssets(); EditorSceneManager.OpenScene(Root + "/Scenes/Bar.unity");
            Debug.Log("SPATIAL_PROOF_BUILT: new room, fixed A/B cameras, three character bindings.");
        }
        private static void ConfigurePipeline()
        {
            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            renderer.renderingMode = RenderingMode.Forward; renderer.depthPrimingMode = DepthPrimingMode.Disabled;
            ResourceReloader.ReloadAllNullIn(renderer, "Packages/com.unity.render-pipelines.universal");
            renderer = Save(renderer, Root + "/Settings/ForwardRenderer.asset");
            var pipeline = UniversalRenderPipelineAsset.Create(renderer);
            pipeline.msaaSampleCount = 1; pipeline.renderScale = 1; pipeline.supportsHDR = false;
            pipeline.shadowDistance = 18; pipeline.mainLightShadowmapResolution = 1024;
            pipeline = Save(pipeline, Root + "/Settings/SpatialProofURP.asset");
            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i); QualitySettings.renderPipeline = pipeline; }
            QualitySettings.vSyncCount = 0; PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.companyName = "Surlhwa"; PlayerSettings.productName = "Surlhwa Spatial Proof";
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.runInBackground = true;
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = settings.FindProperty("activeInputHandler"); if (input != null) { input.intValue = 1; settings.ApplyModifiedPropertiesWithoutUndo(); }
        }
        private static void ImportArt()
        {
            foreach (string id in AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "/Art" })) {
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(id));
                importer.textureType = TextureImporterType.Default; importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false; importer.filterMode = FilterMode.Point; importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.npotScale = TextureImporterNPOTScale.None; importer.SaveAndReimport();
            }
        }
        private static T Save<T>(T asset, string path) where T : Object
        {
            asset.name = Path.GetFileNameWithoutExtension(path);
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) { EditorUtility.CopySerialized(asset, existing); Object.DestroyImmediate(asset); return existing; }
            AssetDatabase.CreateAsset(asset, path); return asset;
        }
        private static Material Material(string name, Color color, bool unlit = false)
        {
            var result = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Simple Lit"));
            result.name = name; result.SetColor("_BaseColor", color); result.SetFloat("_Smoothness", 0);
            result.SetFloat("_SpecularHighlights", 0); result.SetFloat("_EnvironmentReflections", 0);
            return Save(result, Root + "/Materials/" + name + ".mat");
        }
        private static GameObject Cube(string name, Vector3 position, Vector3 size, Material material, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(room);
            go.transform.position = position; go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }
        private static Transform Anchor(string name, Vector3 position, Transform parent = null)
        { var t = new GameObject(name).transform; t.SetParent(parent ?? room); t.position = position; return t; }
        private static Light LightAt(string name, LightType type, Vector3 position, Color color, float intensity, float range, bool shadow)
        {
            var light = new GameObject(name).AddComponent<Light>(); light.transform.SetParent(room); light.transform.position = position;
            light.type = type; light.color = color; light.intensity = intensity; light.range = range;
            light.shadows = shadow ? LightShadows.Soft : LightShadows.None; light.shadowStrength = .45f; light.shadowBias = .03f; return light;
        }
        private static void Stool(Vector3 ground)
        {
            float top = 26f / 44 * Mathf.Cos(18 * Mathf.Deg2Rad);
            Cube("Stool", ground + Vector3.up * (top - .06f), new Vector3(.7f, .12f, .54f), edge);
            foreach (float x in new[] { -.25f, .25f }) foreach (float z in new[] { -.19f, .19f })
                Cube("StoolLeg", ground + new Vector3(x, (top - .12f) / 2, z), new Vector3(.065f, top - .12f, .065f), wood);
            Cube("StoolFootRest", ground + new Vector3(0, .25f, -.2f), new Vector3(.55f, .045f, .045f), metal);
        }
        private static Camera Camera(string name, bool orthographic)
        {
            var camera = new GameObject(name).AddComponent<Camera>(); camera.transform.rotation = PlaneRotation;
            camera.transform.position = new Vector3(0, 1.2f, .7f) - camera.transform.forward * 17;
            camera.orthographic = orthographic; camera.orthographicSize = 3.3f; camera.fieldOfView = 2 * Mathf.Atan(3.3f / 17) * Mathf.Rad2Deg;
            camera.nearClipPlane = .1f; camera.farClipPlane = 60; camera.backgroundColor = new Color(.055f, .065f, .085f);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.allowHDR = false; camera.allowMSAA = false;
            var data = camera.GetUniversalAdditionalCameraData(); data.renderPostProcessing = false; data.antialiasing = AntialiasingMode.None; return camera;
        }
        private static CharacterView Actor(string name, string id, string visual, Vector3 position, string standing, string seated, Vector2 standingPivot, Vector2 seatedPivot, Vector3 hip)
        {
            var actor = new GameObject(name).AddComponent<CharacterView>(); actor.transform.position = position;
            actor.GuestId = id; actor.VisualBindingId = visual; actor.FeetAnchor = Anchor("FeetAnchor", position, actor.transform);
            actor.SeatAnchor = Anchor("SeatAnchor", position + PlaneRotation * hip, actor.transform);
            var plane = new GameObject(name + "_Plane"); plane.transform.SetParent(actor.transform, false); plane.transform.localRotation = PlaneRotation;
            actor.Plane = plane.AddComponent<MeshFilter>(); actor.Renderer = plane.AddComponent<MeshRenderer>();
            actor.Renderer.shadowCastingMode = ShadowCastingMode.Off; actor.Renderer.receiveShadows = false;
            actor.StandingMesh = PlaneMesh(standing, standingPivot); actor.SeatedMesh = PlaneMesh(seated, seatedPivot);
            actor.StandingMaterial = SpriteMaterial(standing); actor.SeatedMaterial = SpriteMaterial(seated);
            actor.Plane.sharedMesh = actor.SeatedMesh; actor.Renderer.sharedMaterial = actor.SeatedMaterial;
            if (name == "Bartender") { actor.Plane.sharedMesh = actor.StandingMesh; actor.Renderer.sharedMaterial = actor.StandingMaterial; }
            var blob = new GameObject(name + "_Contact"); blob.transform.SetParent(actor.transform, false); blob.transform.localPosition = new Vector3(0, .012f, 0);
            var mesh = new Mesh(); var vertices = new Vector3[25]; var triangles = new int[72];
            for (int i = 0; i < 24; i++) { float angle = i * Mathf.PI * 2 / 24; vertices[i + 1] = new Vector3(Mathf.Cos(angle) * .38f, 0, Mathf.Sin(angle) * .17f); triangles[i * 3] = 0; triangles[i * 3 + 1] = (i + 1) % 24 + 1; triangles[i * 3 + 2] = i + 1; }
            mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            blob.AddComponent<MeshFilter>().sharedMesh = Save(mesh, Root + "/Meshes/" + name + "Contact.asset");
            actor.Contact = blob.AddComponent<MeshRenderer>(); actor.Contact.sharedMaterial = contact; actor.Contact.shadowCastingMode = ShadowCastingMode.Off; return actor;
        }
        private static Mesh PlaneMesh(string texture, Vector2 pivot)
        {
            var image = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/" + texture + ".png");
            float left = -pivot.x / 44, right = (image.width - pivot.x) / 44, bottom = -pivot.y / 44, top = (image.height - pivot.y) / 44;
            var mesh = new Mesh { name = texture + "_44ppu" };
            mesh.vertices = new[] { new Vector3(left, bottom, 0), new Vector3(left, top, 0), new Vector3(right, top, 0), new Vector3(right, bottom, 0) };
            mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right }; mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return Save(mesh, Root + "/Meshes/" + texture + ".asset");
        }
        private static Material SpriteMaterial(string texture)
        {
            var material = Material("Sprite_" + texture, new Color(.92f, .84f, .74f), true);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/" + texture + ".png"));
            material.SetFloat("_AlphaClip", 1); material.SetFloat("_Cutoff", .4f); material.SetFloat("_Cull", 0); material.SetFloat("_ZWrite", 1);
            material.EnableKeyword("_ALPHATEST_ON"); material.renderQueue = 2450; return material;
        }
    }
}
