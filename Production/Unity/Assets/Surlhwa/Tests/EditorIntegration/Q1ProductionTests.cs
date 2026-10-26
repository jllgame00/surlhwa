using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Surlhwa.Presentation;
using Surlhwa.ContentIO;
using Surlhwa.Domain;
using System.IO;

namespace Surlhwa.SpatialTests
{
    public sealed class Q1ProductionTests
    {
        [OneTimeSetUp] public void EnsureAuthoredBundle() => Surlhwa.Editor.CampaignBuilder.Sync();
        [Test] public void Q1BuildPreprocessRegeneratesMissingOrStaleBundleWithoutChangingItsGuid()
        {
            const string path="Assets/GeneratedContent/act1-content.json";
            var original=File.ReadAllBytes(path); var meta=File.ReadAllBytes(path+".meta");
            try {
                File.Delete(path); // Generated ignored data only; tracked metadata is never removed.
                new Surlhwa.Editor.Q1CampaignBuildPreprocessor().OnPreprocessBuild(null);
                Assert.That(File.ReadAllText(path),Is.EqualTo(File.ReadAllText("../Content/Campaign/act1.json")));
                File.WriteAllText(path,"stale generated bundle");
                new Surlhwa.Editor.Q1CampaignBuildPreprocessor().OnPreprocessBuild(null);
                Assert.That(File.ReadAllText(path),Is.EqualTo(File.ReadAllText("../Content/Campaign/act1.json")));
                Assert.That(File.ReadAllBytes(path+".meta"),Is.EqualTo(meta));
            } finally { File.WriteAllBytes(path,original); AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport); }
        }
        [Test] public void Q1HidesPrepButKeepsActualServedLiquidIceAndGarnishVisible()
        {
            var content=CampaignContentLoader.Load(File.ReadAllText("../Content/Campaign/act1.json"));
            var brew=new BrewingSession(content.Catalog,content.Tuning,content.Supply);
            brew.Pour("gin",ContainerRole.Serving,45m); brew.AddIce(ContainerRole.Serving); brew.Garnish("cherry","in_glass");
            var served=brew.Freeze(); int revision=brew.Revision;
            var go=new GameObject("Q1 served snapshot visibility");
            try {
                var work=go.AddComponent<SliceWorkbenchView>(); work.Initialize(null); work.ExtendedTools=true;
                work.ApplyArt(AssetDatabase.LoadAssetAtPath<CampaignArtSet>("Assets/Surlhwa/Act1Assets/ProductionArt/Act1Art.asset"));
                work.Refresh(null,null,0,served); var mixingCenter=work.MixingCenter;
                work.SetPresentationVisibility(false,true,Q1ProductionView.WorldAngle);
                Assert.That(work.MixingCenter,Is.EqualTo(mixingCenter),"Q1 served-glass facing cannot move the bound tool input anchor");
                foreach(string name in new[]{"Actual bowl liquid","Cherry in glass","Authored actual ice"}) {
                    var renderers=go.transform.Find("Serving glass 180 ml").GetComponentsInChildren<Renderer>().Where(r=>r.name==name).ToArray();
                    Assert.That(renderers,Is.Not.Empty);
                    Assert.That(renderers.All(r=>r.gameObject.activeInHierarchy && !r.forceRenderingOff),Is.True,name);
                }
                Assert.That(go.transform.Find("Selected bottle").GetComponentsInChildren<Renderer>().All(r=>r.forceRenderingOff),Is.True);
                Assert.That(work.ServingFillMl,Is.EqualTo(45m)); Assert.That(brew.Revision,Is.EqualTo(revision));
                work.SetPresentationVisibility(true,true);
                Assert.That(go.GetComponentsInChildren<Renderer>(true).All(r=>!r.forceRenderingOff),Is.True);
                work.SetPresentationVisibility(false,false);
                Assert.That(go.GetComponentsInChildren<Renderer>(true).All(r=>r.forceRenderingOff),Is.True);
            } finally { Object.DestroyImmediate(go); }
        }
        [Test] public void ScalePreservesSeatAndGroundRootsAcrossFocusedToolsAndReturn()
        {
            EditorSceneManager.OpenScene("Assets/Surlhwa/Scenes/Bar_Act1.unity");
            var owner=Object.FindFirstObjectByType<CampaignController>();
            var actors=new[]{owner.Bartender,owner.Ordinary,owner.Prince};
            var roots=actors.Select(a=>a.transform.position).ToArray();
            var feet=actors.Select(a=>a.FeetAnchor.position).ToArray();
            var seats=actors.Select(a=>a.SeatAnchor.position).ToArray();
            var temporary=actors.Select(a=>new Material(a.SeatedMaterial)).ToArray();
            for(int i=0;i<actors.Length;i++) actors[i].SeatedMaterial=temporary[i];
            var go=new GameObject("Isolated Q1 contract test"); var view=go.AddComponent<Q1ProductionView>(); view.Owner=owner;
            try {
                owner.Ordinary.Show(true,true); owner.Prince.Show(true,true);
                foreach(bool focus in new[]{false,true,false}) {
                    view.ApplyView(focus);
                    Assert.That(owner.Camera.orthographic,Is.True);
                    Assert.That(owner.Camera.orthographicSize,Is.EqualTo(focus ? 3.7f : 4.85f));
                    Assert.That(Quaternion.Angle(owner.Camera.transform.rotation,focus ? Q1ProductionView.FocusAngle : Q1ProductionView.WorldAngle),Is.LessThan(.001f));
                    for(int i=0;i<actors.Length;i++) {
                        var a=actors[i];
                        Assert.That(a.transform.position,Is.EqualTo(roots[i]));
                        Assert.That(a.FeetAnchor.position,Is.EqualTo(feet[i]));
                        Assert.That(a.SeatAnchor.position,Is.EqualTo(seats[i]));
                        Assert.That(a.Plane.transform.localScale.x,Is.EqualTo(focus ? 1 : 1.12f));
                        if(i>0) {
                            var hip=Quaternion.Inverse(Q1ProductionView.FocusAngle)*a.SeatAnchor.localPosition;
                            Assert.That(Vector3.Distance(a.Plane.transform.TransformPoint(hip),seats[i]),Is.LessThan(.0001f));
                        }
                    }
                }
            } finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); foreach(var m in temporary) Object.DestroyImmediate(m); }
        }
        [Test] public void ProductionSceneContainsQ1OnlyAndDepthWritingPose()
        {
            EditorSceneManager.OpenScene("Assets/Surlhwa/Scenes/Bar_Q1.unity");
            var view=Object.FindFirstObjectByType<Q1ProductionView>();
            Assert.That(view,Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<QuarterViewSpike>(),Is.Null);
            Assert.That(Object.FindFirstObjectByType<QuarterViewCapture>(),Is.Null);
            Assert.That(view.Owner.Camera.orthographic,Is.True);
            Assert.That(view.PrinceSideQuarter.GetFloat("_ZWrite"),Is.EqualTo(1));
            Assert.That(view.PrinceSideQuarter.IsKeywordEnabled("_ALPHATEST_ON"),Is.True);
            Assert.That(view.PrinceSideQuarter.mainTexture.filterMode,Is.EqualTo(FilterMode.Point));
            Assert.That(view.Owner.Bundle,Is.SameAs(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/GeneratedContent/act1-content.json")));
            Assert.That(EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path),Is.EqualTo(new[]{"Assets/Surlhwa/Scenes/Bar_Q1.unity"}));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }
    }
}
