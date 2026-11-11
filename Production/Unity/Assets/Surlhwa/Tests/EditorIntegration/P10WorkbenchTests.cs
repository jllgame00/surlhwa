using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Surlhwa.ContentIO;
using Surlhwa.Domain;
using Surlhwa.Presentation;
using Object=UnityEngine.Object;

namespace Surlhwa.SpatialTests
{
    public sealed class P10WorkbenchTests
    {
        private GameObject fixture; private SliceWorkbenchView view; private BrewingSession brew;
        [SetUp] public void Setup()
        {
            fixture=new GameObject("P10 actual container projection"); view=fixture.AddComponent<SliceWorkbenchView>(); view.Initialize(null);
            var content=CampaignContentLoader.Load(File.ReadAllText("../Content/Campaign/act7.json"));
            brew=new BrewingSession(content.Catalog,content.Tuning,content.Supply);
        }
        [TearDown] public void Teardown() { Object.DestroyImmediate(fixture); }
        private void Cycle()
        {
            var method=typeof(SliceWorkbenchView).GetMethod("CyclePresentationGlass");
            Assert.That(method,Is.Not.Null,"Existing manual glass choice must include the adopted physical profiles."); method.Invoke(view,null);
        }
        private CampaignArtSet FinalArt()
        {
            var art=AssetDatabase.LoadAssetAtPath<CampaignArtSet>("Assets/Surlhwa/FinalArt/FinalArt.asset");
            Assert.That(art,Is.Not.Null,"Final candidate has its own optional art bindings."); return art;
        }
        [Test] public void LegacyArtRetainsTwoManualProfilesAndActualComposition()
        {
            view.ApplyArt(AssetDatabase.LoadAssetAtPath<CampaignArtSet>("Assets/Surlhwa/Act1Assets/ProductionArt/Act1Art.asset"));
            brew.Pour("gin",ContainerRole.Serving,90); int revision=brew.Revision;
            Cycle(); view.Refresh(brew,null,0); Assert.That(view.CocktailGlass,Is.True);
            Assert.That(view.ServingFillBottom,Is.GreaterThan(view.ServingStemTop));
            Cycle(); view.Refresh(brew,null,0); Assert.That(view.CocktailGlass,Is.False);
            Assert.That(view.ServingFillMl,Is.EqualTo(90)); Assert.That(brew.Revision,Is.EqualTo(revision));
        }
        [TestCase(30)] [TestCase(90)] [TestCase(180)]
        public void FourManualProfilesKeepActualLiquidInsideBowlWithoutChangingPreparation(int ml)
        {
            view.ApplyArt(FinalArt()); brew.Pour("gin",ContainerRole.Serving,ml); int revision=brew.Revision;
            var shapes=new System.Collections.Generic.HashSet<string>();
            for(int profile=0;profile<4;profile++) {
                view.Refresh(brew,null,0); var fill=fixture.GetComponentsInChildren<MeshFilter>().Single(x=>x.name=="Actual bowl liquid");
                var vertices=fill.sharedMesh.vertices;
                Assert.That(vertices.All(x=>x.y>=view.ServingFillBottom-.0001f),Is.True);
                Assert.That(vertices.All(x=>!float.IsNaN(x.x) && !float.IsNaN(x.y)),Is.True);
                if(profile==1 || profile==3) Assert.That(view.ServingFillBottom,Is.GreaterThan(view.ServingStemTop));
                shapes.Add(string.Join(";",vertices.Select(x=>x.ToString("F4"))));
                Assert.That(view.ServingFillMl,Is.EqualTo(ml)); Assert.That(brew.Revision,Is.EqualTo(revision));
                Cycle();
            }
            Assert.That(shapes.Count,Is.EqualTo(4),"Four physical choices require four actual bowl projections.");
            Assert.That(brew.Container(ContainerRole.Serving).Amounts["gin"],Is.EqualTo(ml));
        }
        [Test] public void AuthoredShakerAndGarnishFollowActualStatesAndClearSafely()
        {
            view.ExtendedTools=true; view.ApplyArt(FinalArt()); view.Refresh(brew,null,0);
            var shell=fixture.GetComponentsInChildren<Transform>(true).SingleOrDefault(x=>x.name=="Authored shaker shell");
            Assert.That(shell,Is.Not.Null); var lid=fixture.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="Closed lid");
            Assert.That(lid.gameObject.activeSelf,Is.False);
            brew.Pour("gin",ContainerRole.Shaker,90); brew.AddIce(ContainerRole.Shaker); brew.SetShakerClosed(true); view.Refresh(brew,null,0);
            Assert.That(lid.gameObject.activeSelf,Is.True); Assert.That(view.ShakerFillMl,Is.EqualTo(90));
            var orange=fixture.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="Orange slice at rim");
            Assert.That(orange.gameObject.activeSelf,Is.False); brew.Pour("gin",ContainerRole.Serving,10); brew.Garnish("orange_slice","rim"); view.Refresh(brew,null,0);
            Assert.That(orange.gameObject.activeSelf,Is.True);
            view.Refresh(null,null,0); Assert.That(lid.gameObject.activeSelf || orange.gameObject.activeSelf,Is.False);
            Assert.That(view.ShakerFillMl,Is.Zero); Assert.That(view.ServingFillMl,Is.Zero);
        }
        [Test] public void AnActualCherryRemainsInsideTheSelectedFluteBowl()
        {
            view.ApplyArt(FinalArt()); brew.Pour("gin",ContainerRole.Serving,90); brew.Garnish("cherry","in_glass");
            Cycle(); Cycle(); Cycle(); view.Refresh(brew,null,0);
            var cherry=fixture.GetComponentsInChildren<MeshFilter>().Single(x=>x.name=="Cherry in glass");
            var glass=fixture.GetComponentsInChildren<MeshFilter>().Single(x=>x.name=="Authored flute");
            foreach(var vertex in cherry.sharedMesh.vertices) {
                var local=glass.transform.InverseTransformPoint(cherry.transform.TransformPoint(vertex));
                Assert.That(Mathf.Abs(local.x),Is.LessThan(.075f),"Actual garnish must fit inside the narrow bowl, not extend beyond the glass.");
                Assert.That(local.y,Is.GreaterThan(.40f)); Assert.That(local.y,Is.LessThan(.73f));
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void CompleteMemoriesRefineWeatheredAnchorButKeepLightWaterShadowAndRipplesFlat(bool refine)
        {
            // Presentation fixture only; earned receipts are validated by campaign and Player tests.
            var state=new CampaignCheckpoint(1,"123456781234123412341234567890ab",0,CampaignPhase.Complete,0,null,null,
                new[]{"presentation_fixture"},Array.Empty<string>(),Array.Empty<string>(),Array.Empty<CommittedService>(),
                new[]{new MemoryReceipt("mem_01","star_dim","fixture",1),new MemoryReceipt("mem_02","swing_empty_place","fixture",1),
                    new MemoryReceipt("mem_03","windmill_fragment","fixture",1),new MemoryReceipt("mem_04","stage_skull_shadow","fixture",1),
                    new MemoryReceipt("mem_05","faint_circle_broken_glass_red_light","fixture",1),
                    new MemoryReceipt("mem_06","half_submerged_lotus_slow_ripples","fixture",1),new MemoryReceipt("mem_07","anchor_ship_fragment","fixture",1)},
                new System.Collections.Generic.Dictionary<string,string>(),new System.Collections.Generic.Dictionary<string,string[]>());
            var memory=fixture.AddComponent<CampaignMemoryView>(); memory.RefineSurfaces=refine; memory.Refresh(state,Vector3.zero);
            Assert.That(memory.Visible && memory.LoversVisible && memory.WindmillVisible && memory.HamletVisible && memory.FaustVisible && memory.SimCheongVisible && memory.OdysseusVisible,Is.True);
            foreach(var flat in new[]{memory.FaustGlow,memory.SimCheongWater,memory.HamletShadow,memory.SimCheongRipples}) {
                Assert.That(flat.childCount,Is.Zero,flat.name+" must retain its flat trace without material edge bands.");
            }
            foreach(var part in new[]{memory.OdysseusAnchor,memory.OdysseusShipFragment}) {
                foreach(string edge in new[]{"Worn upper edge","Quiet material edge"}) {
                    var band=part.Find(edge);
                    if(refine) {
                        Assert.That(band,Is.Not.Null,part.name+" needs both final material edge bands.");
                        Assert.That(band.GetComponent<MeshFilter>().sharedMesh.vertexCount,Is.GreaterThan(0));
                        Assert.That(band.GetComponent<MeshRenderer>().enabled,Is.True);
                    }
                    else Assert.That(band,Is.Null,"Accepted memory presentation must remain unchanged when refinement is disabled.");
                }
            }
            if(!refine) Assert.That(fixture.GetComponentsInChildren<Transform>(true).Any(x=>x.name=="Worn upper edge" || x.name=="Quiet material edge"),Is.False);
        }
    }
}
