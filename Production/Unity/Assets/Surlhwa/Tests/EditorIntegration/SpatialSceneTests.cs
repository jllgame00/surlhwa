using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using Surlhwa.Editor;
using Surlhwa.Application;
using Surlhwa.Presentation;
namespace Surlhwa.Spatial.Tests
{
    public sealed class SpatialSceneTests
    {
        [OneTimeSetUp] public void BuildScene() => SpatialProofBuilder.Build();
        [Test] public void RequiredRoomAndAnchorsExist()
        {
            EditorSceneManager.OpenScene("Assets/Surlhwa/Scenes/Bar.unity");
            foreach (string name in new[] { "Floor", "RearWall", "Counter", "Stool", "Shelving", "WinterWindow", "Lamp",
                "FeetAnchor", "SeatAnchor", "StandingAnchor", "CounterHandAnchor", "DialogueAnchor", "EntryAnchor", "ExitAnchor", "WorkbenchAnchor", "MemoryDisplayAnchor" })
                Assert.That(GameObject.Find(name), Is.Not.Null, name);
        }
        [Test] public void NoMissingScriptsMaterialsOrTextures()
        {
            EditorSceneManager.OpenScene("Assets/Surlhwa/Scenes/Bar.unity");
            foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go.gameObject), Is.Zero, go.name);
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                foreach (var material in renderer.sharedMaterials) Assert.That(material != null && material.shader != null, Is.True, renderer.name);
            Assert.That(AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Surlhwa/Art" }).Length, Is.GreaterThanOrEqualTo(5));
        }
        [Test] public void BothCamerasHaveFixedMatchingOrientationAndNoPostEffects()
        {
            EditorSceneManager.OpenScene("Assets/Surlhwa/Scenes/Bar.unity");
            var a = GameObject.Find("Camera_A_Orthographic").GetComponent<Camera>();
            var b = GameObject.Find("Camera_B_Perspective").GetComponent<Camera>();
            Assert.That(a.orthographic, Is.True); Assert.That(b.orthographic, Is.False);
            Assert.That(Quaternion.Angle(a.transform.rotation, b.transform.rotation), Is.LessThan(.001f));
            Assert.That(b.fieldOfView, Is.InRange(12f, 30f));
            foreach (var camera in new[] { a, b })
                Assert.That(camera.GetComponents<MonoBehaviour>().All(c => c.GetType().Name == "UniversalAdditionalCameraData"), Is.True);
        }
        [Test] public void CharacterMaterialUsesDepthWritingCutoutWithoutLongShadows()
        {
            EditorSceneManager.OpenScene("Assets/Surlhwa/Scenes/Bar.unity");
            foreach (var name in new[] { "Bartender_Plane", "Ordinary_Plane", "Quixote_Plane" })
            {
                var renderer = GameObject.Find(name).GetComponent<MeshRenderer>();
                Assert.That(renderer.sharedMaterial.GetFloat("_AlphaClip"), Is.EqualTo(1));
                Assert.That(renderer.sharedMaterial.GetFloat("_ZWrite"), Is.EqualTo(1));
                Assert.That(renderer.sharedMaterial.mainTexture, Is.Not.Null);
                Assert.That(renderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
            }
        }
        [Test] public void FeetAndSeatAnchorsMeetActualSupportingGeometry()
        {
            EditorSceneManager.OpenScene("Assets/Surlhwa/Scenes/Bar.unity");
            float floor = GameObject.Find("Floor").GetComponent<Collider>().bounds.max.y;
            float seat = GameObject.Find("Stool").GetComponent<Collider>().bounds.max.y;
            foreach (var actor in Object.FindObjectsByType<CharacterView>(FindObjectsSortMode.None))
            {
                Assert.That(Mathf.Abs(actor.FeetAnchor.position.y - floor), Is.LessThan(.002f), actor.name + " floor contact");
                if (actor.name != "Bartender") Assert.That(Mathf.Abs(actor.SeatAnchor.position.y - seat), Is.LessThan(.002f), actor.name + " seat contact");
            }
        }
        [Test] public void FixtureAndCameraSwitchingDoNotMoveCharacterRootsOrFeet()
        {
            EditorSceneManager.OpenScene("Assets/Surlhwa/Scenes/Bar.unity");
            var stage = Object.FindFirstObjectByType<ReviewStage>();
            var actors = Object.FindObjectsByType<CharacterView>(FindObjectsSortMode.None);
            var positions = actors.Select(x => x.transform.position).ToArray();
            var feet = actors.Select(x => x.FeetAnchor.position).ToArray();
            foreach (CameraVariant camera in System.Enum.GetValues(typeof(CameraVariant)))
                foreach (ReviewFixture fixture in System.Enum.GetValues(typeof(ReviewFixture))) stage.Select(camera, fixture);
            for (int i = 0; i < actors.Length; i++) {
                Assert.That(actors[i].transform.position, Is.EqualTo(positions[i]));
                Assert.That(actors[i].FeetAnchor.position, Is.EqualTo(feet[i]));
            }
        }
        [Test] public void PhysicalCounterBlocksBartenderLowerBodyRayForBothCameras()
        {
            EditorSceneManager.OpenScene("Assets/Surlhwa/Scenes/Bar.unity"); Physics.SyncTransforms();
            var actor = GameObject.Find("Bartender").transform;
            foreach (string name in new[] { "Camera_A_Orthographic", "Camera_B_Perspective" }) {
                var camera = GameObject.Find(name).GetComponent<Camera>();
                var target = actor.position + Vector3.up * .3f;
                var ray = camera.ViewportPointToRay(camera.WorldToViewportPoint(target));
                Assert.That(Physics.Raycast(ray, out var hit, 60), Is.True);
                Assert.That(hit.collider.name, Does.StartWith("Counter"));
            }
        }
        [Test] public void StoolDoesNotOccludeSeatedTorsoForEitherCamera()
        {
            EditorSceneManager.OpenScene("Assets/Surlhwa/Scenes/Bar.unity"); Physics.SyncTransforms();
            var actor = GameObject.Find("Quixote").GetComponent<CharacterView>();
            var torso = actor.Plane.transform.TransformPoint(new Vector3(18f / 44, .8f, 0));
            foreach (string name in new[] { "Camera_A_Orthographic", "Camera_B_Perspective" }) {
                var camera = GameObject.Find(name).GetComponent<Camera>();
                var ray = camera.ViewportPointToRay(camera.WorldToViewportPoint(torso));
                bool hit = Physics.Raycast(ray, out var obstruction, Vector3.Distance(ray.origin, torso) - .005f);
                Assert.That(hit && obstruction.collider.name.StartsWith("Stool"), Is.False, name + " torso penetrates stool");
            }
        }
        [Test] public void GeneratedBundleCanBeRecreatedWithoutChangingItsReferenceGuid()
        {
            const string generated = "Assets/GeneratedContent/spatial-proof.json";
            string previous = System.IO.File.ReadAllText(generated);
            string guid = AssetDatabase.AssetPathToGUID(generated);
            try {
                System.IO.File.Delete(generated);
                ContentBundleGenerator.Sync();
                Assert.That(System.IO.File.ReadAllText(generated), Is.EqualTo(System.IO.File.ReadAllText("../Content/spatial-proof.json")));
                Assert.That(AssetDatabase.AssetPathToGUID(generated), Is.EqualTo(guid));
                Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>(generated), Is.Not.Null);
            } finally {
                if (!System.IO.File.Exists(generated)) System.IO.File.WriteAllText(generated, previous);
                AssetDatabase.Refresh();
            }
        }
        [Test] public void FailedBindingInitializationCannotLeaveStageReadyForCapture()
        {
            EditorSceneManager.OpenScene("Assets/Surlhwa/Scenes/Bar.unity");
            var stage = Object.FindFirstObjectByType<ReviewStage>();
            var awake = typeof(ReviewStage).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            awake.Invoke(stage, null);
            var ready = typeof(ReviewStage).GetProperty("IsInitialized");
            Assert.That(ready, Is.Not.Null, "Capture must have an explicit successful initialization gate");
            Assert.That(ready.GetValue(stage), Is.True);
            stage.Bartender.VisualBindingId = "visual_ordinary";
            var error = Assert.Throws<System.Reflection.TargetInvocationException>(() => awake.Invoke(stage, null));
            Assert.That(error.InnerException, Is.TypeOf<System.InvalidOperationException>());
            Assert.That(ready.GetValue(stage), Is.False, "A failed initialization must close the capture gate");
        }
        [TestCase(false)] [TestCase(true)]
        public void NativeBuildCallbackUsesCurrentAuthoredJsonAndRejectsInvalidChanges(bool invalid)
        {
            const string authored = "../Content/spatial-proof.json", generated = "Assets/GeneratedContent/spatial-proof.json";
            byte[] original = System.IO.File.ReadAllBytes(authored);
            string guid = AssetDatabase.AssetPathToGUID(generated);
            var callbacks = TypeCache.GetTypesDerivedFrom<IPreprocessBuildWithReport>()
                .Where(t => t.Namespace == "Surlhwa.Editor" && !t.IsAbstract)
                .Select(t => (IPreprocessBuildWithReport)System.Activator.CreateInstance(t)).ToArray();
            Assert.That(callbacks, Is.Not.Empty, "Normal Unity builds must validate the authored source");
            try {
                string changed = System.Text.Encoding.UTF8.GetString(original)
                    .Replace("spatial_proof_v1", "native_build_한글_probe");
                if (invalid) changed = changed.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2");
                System.IO.File.WriteAllText(authored, changed, new System.Text.UTF8Encoding(false));
                if (invalid) {
                    string previous = System.IO.File.ReadAllText(generated);
                    Assert.Throws<BuildFailedException>(() => callbacks.Single().OnPreprocessBuild(null));
                    Assert.That(System.IO.File.ReadAllText(generated), Is.EqualTo(previous));
                } else {
                    foreach (var callback in callbacks) callback.OnPreprocessBuild(null);
                    Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>(generated).text, Is.EqualTo(changed));
                }
                Assert.That(AssetDatabase.AssetPathToGUID(generated), Is.EqualTo(guid));
            } finally {
                System.IO.File.WriteAllBytes(authored, original);
                ContentBundleGenerator.Sync();
            }
        }
    }
}
