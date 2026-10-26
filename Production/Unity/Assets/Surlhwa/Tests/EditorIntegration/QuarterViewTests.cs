using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using Surlhwa.Presentation;

namespace Surlhwa.SpatialTests
{
    public sealed class QuarterViewTests
    {
        [Test] public void ReversibleCandidatesPreserveC1CameraRootsContactsAndMaterials()
        {
            EditorSceneManager.OpenScene("Assets/Surlhwa/Scenes/Bar_Act1.unity");
            var c=Object.FindFirstObjectByType<CampaignController>();
            var actors=new[]{c.Bartender,c.Ordinary,c.Prince};
            var roots=actors.Select(a=>a.transform.position).ToArray();
            var feet=actors.Select(a=>a.FeetAnchor.position).ToArray();
            var seats=actors.Select(a=>a.SeatAnchor.position).ToArray();
            var planeRotations=actors.Select(a=>a.Plane.transform.localRotation).ToArray();
            var position=c.Camera.transform.position; var rotation=c.Camera.transform.rotation; float size=c.Camera.orthographicSize;
            var go=new GameObject("Isolated Quarter View test"); var q=go.AddComponent<QuarterViewSpike>(); q.Owner=c;
            // Editor preview must not let URP validation dirty accepted material assets.
            var temporary=actors.Select(a=>new Material(a.SeatedMaterial)).ToArray();
            for(int i=0;i<actors.Length;i++) actors[i].SeatedMaterial=temporary[i];
            try {
                c.Ordinary.Show(true,true); c.Prince.Show(true,true);
                var materials=actors.Select(a=>a.Renderer.sharedMaterial).ToArray();
                foreach(int candidate in new[]{1,2,0}) {
                    q.Select(candidate);
                    Assert.That(c.Camera.orthographic,Is.EqualTo(candidate!=2));
                    for(int i=0;i<actors.Length;i++) {
                        Assert.That(actors[i].transform.position,Is.EqualTo(roots[i]));
                        Assert.That(actors[i].FeetAnchor.position,Is.EqualTo(feet[i]));
                        Assert.That(actors[i].SeatAnchor.position,Is.EqualTo(seats[i]));
                        Assert.That(actors[i].Renderer.sharedMaterial,Is.SameAs(materials[i]));
                        if(i>0) {
                            Vector3 hip=Quaternion.Inverse(planeRotations[i])*actors[i].SeatAnchor.localPosition;
                            Vector3 projectedHip=actors[i].Plane.transform.TransformPoint(hip);
                            Assert.That(Vector3.Distance(projectedHip,seats[i]),Is.LessThan(.0001f),"Authored seat contact, not screen-space sorting");
                        }
                        Assert.That(actors[i].Renderer.sharedMaterial.GetFloat("_ZWrite"),Is.EqualTo(1));
                        Assert.That(actors[i].Renderer.sharedMaterial.IsKeywordEnabled("_ALPHATEST_ON"),Is.True);
                    }
                }
                Assert.That(c.Camera.transform.position,Is.EqualTo(position));
                Assert.That(c.Camera.transform.rotation,Is.EqualTo(rotation));
                Assert.That(c.Camera.orthographicSize,Is.EqualTo(size));
                for(int i=0;i<actors.Length;i++) Assert.That(actors[i].Plane.transform.localRotation,Is.EqualTo(planeRotations[i]));
            } finally { Object.DestroyImmediate(go); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); foreach(var material in temporary) Object.DestroyImmediate(material); }
        }
        [Test] public void AcceptedAct1HasNoQuarterViewComponentOrSceneReference()
        {
            EditorSceneManager.OpenScene("Assets/Surlhwa/Scenes/Bar_Act1.unity");
            Assert.That(Object.FindFirstObjectByType<QuarterViewSpike>(),Is.Null);
            Assert.That(Object.FindFirstObjectByType<QuarterViewCapture>(),Is.Null);
            Assert.That(Object.FindFirstObjectByType<CampaignController>().Art,Is.Not.Null);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }
    }
}
