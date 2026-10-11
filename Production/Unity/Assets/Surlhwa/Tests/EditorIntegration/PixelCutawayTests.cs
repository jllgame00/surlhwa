using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Surlhwa.Editor;
using Surlhwa.Presentation;
using Object = UnityEngine.Object;
namespace Surlhwa.Spatial.Tests
{
    public sealed class PixelCutawayTests
    {
        [OneTimeSetUp] public void BuildC()
        {
            var builder = typeof(SpatialProofBuilder).Assembly.GetType("Surlhwa.Editor.PixelCutawayBuilder");
            Assert.That(builder, Is.Not.Null, "Owner revision C has no builder yet");
            builder.GetMethod("Build").Invoke(null, null);
        }
        [SetUp] public void Open()
        {
            EditorSceneManager.OpenScene("Assets/Surlhwa/Scenes/Bar_C.unity");
            var stage = GameObject.Find("C_Review").GetComponents<MonoBehaviour>().Single(x => x.GetType().Name == "PixelCutawayStage");
            stage.GetType().GetMethod("Select").Invoke(stage, new object[] { 3 });
        }
        [Test] public void SnowIsVisibleWorldGroundBeyondEveryInteriorEdge()
        {
            var floor = GameObject.Find("C_Floor").GetComponent<Collider>().bounds;
            var snow = GameObject.Find("C_SnowGround").GetComponent<Collider>().bounds;
            Assert.That(snow.min.x, Is.LessThan(floor.min.x - 1)); Assert.That(snow.max.x, Is.GreaterThan(floor.max.x + 1));
            Assert.That(snow.min.z, Is.LessThan(floor.min.z - 1)); Assert.That(snow.max.z, Is.GreaterThan(floor.max.z + 1));
            var camera = GameObject.Find("Camera_C_PixelCutaway").GetComponent<Camera>();
            foreach (var point in new[] { new Vector3(floor.min.x - .6f, 0, 0), new Vector3(floor.max.x + .6f, 0, 0), new Vector3(0, 0, floor.min.z - .6f) }) {
                var v = camera.WorldToViewportPoint(point); Assert.That(v.x, Is.InRange(.02f, .98f)); Assert.That(v.y, Is.InRange(.02f, .98f));
            }
        }
        [Test] public void CameraAndBroadSurfacesUseNativePixelLanguageWithoutScreenFilter()
        {
            var camera = GameObject.Find("Camera_C_PixelCutaway").GetComponent<Camera>();
            Assert.That(camera.orthographic, Is.True); Assert.That(camera.transform.eulerAngles.x, Is.InRange(23, 32));
            Assert.That(camera.GetUniversalAdditionalCameraData().renderPostProcessing, Is.False);
            Assert.That(camera.targetTexture, Is.Null); Assert.That(camera.allowHDR, Is.False);
            foreach (string name in new[] { "C_Floor", "C_RearPlane", "C_SnowGround" }) {
                var mat = GameObject.Find(name).GetComponent<Renderer>().sharedMaterials.FirstOrDefault(m => m.mainTexture != null);
                Assert.That(mat, Is.Not.Null, name + " needs a pixel-textured visible face");
                Assert.That(mat.shader.name, Is.EqualTo("Universal Render Pipeline/Unlit"));
                var texture = (Texture2D)mat.mainTexture; Assert.That(texture, Is.Not.Null);
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point)); Assert.That(texture.mipmapCount, Is.EqualTo(1));
            }
            Assert.That(GameObject.Find("C_RearPlane").transform.localScale.z, Is.LessThan(.1f));
        }
        [Test] public void AuthoredCharacterScaleAndContactSupportAreRecheckedForC()
        {
            float floor = GameObject.Find("C_Floor").GetComponent<Collider>().bounds.max.y;
            float seat = GameObject.Find("C_Stool").GetComponent<Collider>().bounds.max.y;
            var actors = Object.FindObjectsByType<CharacterView>(FindObjectsSortMode.None);
            foreach (var actor in actors) {
                Assert.That(actor.transform.lossyScale, Is.EqualTo(Vector3.one));
                Assert.That(actor.FeetAnchor.position.y, Is.EqualTo(floor).Within(.002));
                Assert.That(actor.StandingMesh.bounds.size.y, Is.InRange(1.2f, 1.65f));
                if (actor.name != "C_Bartender") Assert.That(actor.SeatAnchor.position.y, Is.EqualTo(seat).Within(.002));
                Assert.That(actor.Renderer.sharedMaterial.GetFloat("_ZWrite"), Is.EqualTo(1));
                Assert.That(actor.Renderer.sharedMaterial.GetFloat("_AlphaClip"), Is.EqualTo(1));
                Assert.That(actor.Renderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
            }
        }
        [Test] public void ActualCounterBlocksLowerBartenderButStoolLeavesGuestTorsoClear()
        {
            Physics.SyncTransforms();
            var camera = GameObject.Find("Camera_C_PixelCutaway").GetComponent<Camera>();
            var bartender = GameObject.Find("C_Bartender").transform;
            var ray = camera.ViewportPointToRay(camera.WorldToViewportPoint(bartender.position + Vector3.up * .25f));
            Assert.That(Physics.Raycast(ray, out var hit, 60), Is.True); Assert.That(hit.collider.name, Does.StartWith("C_Counter"));
            var guest = GameObject.Find("C_Quixote").GetComponent<CharacterView>();
            var torso = guest.Plane.transform.TransformPoint(new Vector3(18f / 64, .72f, 0));
            ray = camera.ViewportPointToRay(camera.WorldToViewportPoint(torso));
            bool blocked = Physics.Raycast(ray, out hit, Vector3.Distance(ray.origin, torso) - .005f);
            Assert.That(blocked && hit.collider.name.StartsWith("C_Stool"), Is.False);
        }
        [Test] public void SixFixturesKeepActorRootsAndAnchorsStable()
        {
            var stage = GameObject.Find("C_Review").GetComponents<MonoBehaviour>().Single(x => x.GetType().Name == "PixelCutawayStage");
            var actors = Object.FindObjectsByType<CharacterView>(FindObjectsSortMode.None);
            var roots = actors.Select(a => a.transform.position).ToArray();
            var feet = actors.Select(a => a.FeetAnchor.position).ToArray();
            for (int i = 0; i < 6; i++) stage.GetType().GetMethod("Select").Invoke(stage, new object[] { i });
            for (int i = 0; i < actors.Length; i++) {
                Assert.That(actors[i].transform.position, Is.EqualTo(roots[i])); Assert.That(actors[i].FeetAnchor.position, Is.EqualTo(feet[i]));
            }
            Assert.Throws<TargetInvocationException>(() => stage.GetType().GetMethod("Select").Invoke(stage, new object[] { 6 }));
        }
        [Test] public void StoolLipDoesNotCoverFrontOfSeatedKnee()
        {
            Physics.SyncTransforms();
            var guest = GameObject.Find("C_Quixote").GetComponent<CharacterView>();
            var camera = GameObject.Find("Camera_C_PixelCutaway").GetComponent<Camera>();
            var knee = guest.Plane.transform.TransformPoint(new Vector3(18f / 64, 20f / 64, 0));
            var ray = camera.ViewportPointToRay(camera.WorldToViewportPoint(knee));
            bool blocked = Physics.Raycast(ray, out var hit, Vector3.Distance(ray.origin, knee) - .005f);
            Assert.That(blocked && hit.collider.name.StartsWith("C_Stool"), Is.False, "Seat front lip intrudes into visible knee instead of supporting hip");
        }
        [Test] public void InitializationFailureClosesCaptureReadiness()
        {
            var stage = GameObject.Find("C_Review").GetComponents<MonoBehaviour>().Single(x => x.GetType().Name == "PixelCutawayStage");
            var awake = stage.GetType().GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            awake.Invoke(stage, null); Assert.That(stage.GetType().GetProperty("IsInitialized").GetValue(stage), Is.True);
            GameObject.Find("C_Bartender").GetComponent<CharacterView>().VisualBindingId = "visual_ordinary";
            var error = Assert.Throws<TargetInvocationException>(() => awake.Invoke(stage, null));
            Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(stage.GetType().GetProperty("IsInitialized").GetValue(stage), Is.False);
        }
    }
}
