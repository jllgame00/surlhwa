using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using Surlhwa.Editor;
using Surlhwa.Presentation;
using Object = UnityEngine.Object;
namespace Surlhwa.Spatial.Tests
{
    public sealed class PixelCutawayPolishTests
    {
        [OneTimeSetUp] public void Build()
        {
            var builder = typeof(SpatialProofBuilder).Assembly.GetType("Surlhwa.Editor.PixelCutawayBuilder");
            // RED exercises the actual previous C geometry; GREEN exercises its bounded C1 revision.
            (builder.GetMethod("BuildC1") ?? builder.GetMethod("Build")).Invoke(null, null);
        }
        [SetUp] public void Open()
        {
            string scene = System.IO.File.Exists("Assets/Surlhwa/Scenes/Bar_C1.unity") ? "Bar_C1" : "Bar_C";
            EditorSceneManager.OpenScene("Assets/Surlhwa/Scenes/" + scene + ".unity");
            var stage = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Single(x => x.GetType().Name == "PixelCutawayStage");
            stage.GetType().GetMethod("Select").Invoke(stage, new object[] { 3 });
        }
        [Test] public void LongCounterReturnsEncloseBartenderWorkingArea()
        {
            var front = GameObject.Find("C_CounterTop").GetComponent<Renderer>().bounds;
            var returns = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.name == "C_CounterReturnTop").Select(r => r.bounds).OrderBy(b => b.center.x).ToArray();
            Assert.That(returns.Length, Is.EqualTo(2));
            Assert.That(front.size.x, Is.GreaterThanOrEqualTo(7.8f), "U counter must occupy the main bar width");
            foreach (var arm in returns) {
                Assert.That(arm.size.z, Is.GreaterThanOrEqualTo(3.3f), "Returns must read as long U arms, not rear posts");
                Assert.That(arm.min.z, Is.LessThan(front.max.z));
            }
            var bartender = GameObject.Find("C_Bartender").transform.position;
            Assert.That(bartender.x, Is.InRange(returns[0].max.x + .3f, returns[1].min.x - .3f));
            Assert.That(bartender.z, Is.InRange(front.max.z + .2f, returns[0].max.z - .3f));
            foreach (string name in new[] { "C_Ordinary", "C_Quixote" })
                Assert.That(GameObject.Find(name).GetComponent<CharacterView>().SeatAnchor.position.z, Is.LessThan(front.min.z - .25f));
        }
        [Test] public void GuestStandingScaleIsSubordinateToBartenderWithoutLosingDetail()
        {
            var bartender = GameObject.Find("C_Bartender").GetComponent<CharacterView>();
            foreach (string name in new[] { "C_Ordinary", "C_Quixote" }) {
                var guest = GameObject.Find(name).GetComponent<CharacterView>();
                Assert.That(guest.StandingMesh.bounds.size.y / bartender.StandingMesh.bounds.size.y, Is.InRange(.72f, .84f));
                Assert.That(guest.StandingMesh.bounds.size.y, Is.GreaterThan(1.15f));
                Assert.That(guest.Renderer.sharedMaterial.mainTexture.filterMode, Is.EqualTo(FilterMode.Point));
            }
        }
        [Test] public void BarRemainsPrimarySubjectWithExteriorMargins()
        {
            var camera = GameObject.Find("Camera_C_PixelCutaway").GetComponent<Camera>();
            var floor = GameObject.Find("C_Floor").GetComponent<Renderer>().bounds;
            float width = camera.WorldToViewportPoint(new Vector3(floor.max.x, 0, 0)).x - camera.WorldToViewportPoint(new Vector3(floor.min.x, 0, 0)).x;
            Assert.That(width, Is.InRange(.70f, .80f), "Playable bar should fill the frame while retaining snowy sides");
            Assert.That(camera.orthographic, Is.True);
            Assert.That(camera.transform.eulerAngles.x, Is.EqualTo(28).Within(.001));
        }
        [Test] public void FarGroundHasBrokenNeutralSnowInsteadOfOnlyBlueBands()
        {
            var material = GameObject.Find("C_SnowGround").GetComponent<Renderer>().sharedMaterials[4];
            var texture = new Texture2D(2, 2);
            try {
                string path = UnityEditor.AssetDatabase.GetAssetPath(material.mainTexture);
                texture.LoadImage(System.IO.File.ReadAllBytes(path));
                int outside = 0, snow = 0, dark = 0;
                for (int y = 0; y < texture.height; y++) for (int x = 0; x < texture.width; x++) {
                    float wx = (x / (float)(texture.width - 1) - .5f) * 24;
                    float wz = (y / (float)(texture.height - 1) - .5f) * 24 + .8f;
                    if (Mathf.Abs(wx) < 6.4f && Mathf.Abs(wz - .4f) < 4.5f) continue;
                    Color p = texture.GetPixel(x, y); outside++;
                    if (p.grayscale > .35f && p.b - p.r < .14f) snow++;
                    if (p.grayscale < .22f) dark++;
                }
                Assert.That(snow / (float)outside, Is.InRange(.08f, .55f), "Scattered neutral snow must continue beyond the perimeter");
                Assert.That(dark / (float)outside, Is.GreaterThan(.25f), "Quiet dark winter ground must remain between snow banks");
            } finally { Object.DestroyImmediate(texture); }
        }
    }
}
