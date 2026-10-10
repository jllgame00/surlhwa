using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
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
    }
}
