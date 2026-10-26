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
        [Test] public void SimulatedFinePourDoesNotDeadlockBetweenFrameIncrementsOrLoosenTolerance()
        {
            var complete=typeof(CampaignPlayerProof).GetMethod("FinePourComplete",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            Assert.That((bool)complete.Invoke(null,new object[]{29.874m,30m,.25m}),Is.True,"50 ms frame previously waited forever despite .126 ml error");
            Assert.That((bool)complete.Invoke(null,new object[]{29.5m,30m,1.25m}),Is.False,"an unacceptable measurement cannot finish");
            Assert.That((bool)complete.Invoke(null,new object[]{14.727195m,15m,.35m}),Is.False,"captured late splash was below the existing 14.75 ml preparation minimum");
            Assert.That((bool)complete.Invoke(null,new object[]{14.75m,15m,.35m}),Is.False,"fixture aims inside the strict .25 ml boundary");
            Assert.That((bool)complete.Invoke(null,new object[]{29m,30m,1.25m}),Is.False,"a long capture frame must wait rather than forge acceptable volume");
            Assert.That((bool)complete.Invoke(null,new object[]{29.6m,30m,.08m}),Is.False,"continue toward target while the next normal step fits");
        }
        [Test] public void FocusRevealSwitchesProjectionImmediatelyWithoutBlockingOrAccumulatingUi()
        {
            EditorSceneManager.OpenScene("Assets/Surlhwa/Scenes/Bar_Q1.unity");
            var view=Object.FindFirstObjectByType<Q1ProductionView>();
            var root=view.Owner.Bartender.transform.position;
            try {
                foreach(bool focus in new[]{true,false,true,false}) {
                    view.SwitchFocus(focus);
                    Assert.That(view.Owner.Camera.orthographicSize,Is.EqualTo(focus ? 3.7f : 4.85f));
                    Assert.That(view.Focused,Is.EqualTo(focus));
                    var canvases=view.GetComponentsInChildren<Canvas>().Where(c=>c.name=="Q1 focus reveal").ToArray();
                    Assert.That(canvases.Length,Is.EqualTo(1));
                    Assert.That(canvases[0].sortingOrder,Is.LessThan(100),"reading UI remains above the reveal");
                    Assert.That(canvases[0].GetComponent<UnityEngine.UI.GraphicRaycaster>(),Is.Null);
                    Assert.That(canvases[0].GetComponentInChildren<UnityEngine.UI.Image>().raycastTarget,Is.False);
                    Assert.That(view.Owner.Bartender.transform.position,Is.EqualTo(root));
                    Assert.That(Time.timeScale,Is.EqualTo(1));
                    Assert.That(view.Owner.Session,Is.Null,"presentation cannot create progression");
                }
                typeof(Q1ProductionView).GetMethod("OnDisable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(view,null);
                Assert.That(view.GetComponentsInChildren<UnityEngine.UI.Image>().Single(i=>i.name=="Warm charcoal").enabled,Is.False);
            } finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); }
        }
        [Test] public void MemoryFacingDoesNotInventReceiptOrMoveItsAuthoredChairOffset()
        {
            var go=new GameObject("Q1 memory facing test");
            try {
                var memory=go.AddComponent<CampaignMemoryView>(); var chair=new Vector3(1,0,2);
                memory.Refresh(null,chair); memory.SetPresentationFacing(Q1ProductionView.WorldAngle);
                Assert.That(memory.Visible,Is.False); Assert.That(memory.Glowing,Is.False);
                var star=go.transform.Find("MEM-01 small star");
                Assert.That(star.position,Is.EqualTo(chair+new Vector3(.32f,.76f,-.06f)));
                Assert.That(Quaternion.Angle(star.rotation,Q1ProductionView.WorldAngle),Is.LessThan(.001f));
                memory.Refresh(null,chair);
                Assert.That(Quaternion.Angle(star.rotation,Q1ProductionView.WorldAngle),Is.LessThan(.001f));
            } finally { Object.DestroyImmediate(go); }
        }
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
