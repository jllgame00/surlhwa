using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Domain;
using Surlhwa.Presentation;
using Object=UnityEngine.Object;

namespace Surlhwa.SpatialTests
{
    public sealed class CampaignMemoryChairTests
    {
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private sealed class Store : ICampaignSaveStore { public Task SaveAsync(CampaignCheckpoint checkpoint)=>Task.CompletedTask; }
        private CampaignController owner; private CampaignContent content; private CampaignCastView cast;
        private Vector3 original; private GameObject events;
        [SetUp] public void Setup()
        {
            EditorSceneManager.OpenScene(Surlhwa.Editor.ActThreeExpansionBuilder.ScenePath);
            owner=Object.FindFirstObjectByType<CampaignController>(); content=CampaignContentLoader.Load(owner.Bundle.text);
            original=owner.Ordinary.transform.position;
            Set("Content",content); Set("Input",owner.gameObject.AddComponent<SliceInputAdapter>());
            typeof(CampaignController).GetField("seatPosition",Hidden).SetValue(owner,original);
            cast=owner.GetComponent<CampaignCastView>(); cast.Initialize(owner,original); Set("Cast",cast);
        }
        [TearDown] public void Teardown()
        {
            if(owner!=null && owner.Workbench!=null) { Object.DestroyImmediate(owner.Workbench.gameObject); Set("Workbench",null); }
            if(events!=null) {
                typeof(EventSystem).GetMethod("OnDisable",Hidden).Invoke(events.GetComponent<EventSystem>(),null);
                Object.DestroyImmediate(events);
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }
        private void Set(string name,object value)=>typeof(CampaignController).GetProperty(name).SetValue(owner,value);
        private void At(int slot,bool memory=true,CampaignPhase phase=CampaignPhase.Request,ServiceSnapshot served=null)
        {
            owner.Session?.Dispose();
            var state=new CampaignCheckpoint(1,"123456781234123412341234567890ab",slot,phase,0,null,null,
                content.Slots.Take(slot+1).Select(x=>x.EncounterId),Array.Empty<string>(),Array.Empty<string>(),
                served==null ? Array.Empty<CommittedService>() : new[]{new CommittedService("123456781234123412341234567890ab:"+slot,served,null,"good",false)},
                memory ? new[]{new MemoryReceipt("mem_01","star_glowing","fixture",1)} : Array.Empty<MemoryReceipt>(),
                content.KnownSeeds.ToDictionary(x=>x.Key,x=>x.Value),new Dictionary<string,string[]>());
            Set("Session",CampaignSession.Continue(content,new Store(),state));
        }
        private void Animate()=>typeof(CampaignController).GetMethod("AnimateGuest",Hidden).Invoke(owner,null);
        private Collider Stool(float x)=>Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)
            .Single(c=>(c.name=="C_Stool" || c.name=="C_ExtraStool") && Mathf.Abs(c.bounds.center.x-x)<.01f);
        private void OnSeat(CharacterView actor,float x)
        {
            Physics.SyncTransforms(); var bounds=Stool(x).bounds;
            Assert.That(Vector3.Distance(actor.SeatAnchor.position,bounds.ClosestPoint(actor.SeatAnchor.position)),Is.LessThan(.003f),actor.name+" must use the actual unreserved stool.");
            Assert.That(actor.Renderer.enabled,Is.True);
        }
        [Test] public void EarnedEmptyChairStaysVacantForEveryLaterSoloGuestWithoutMovingItsStar()
        {
            var memory=owner.gameObject.AddComponent<CampaignMemoryView>();
            foreach(int slot in new[]{17,19,22,25,26,27,28}) {
                At(slot); Animate(); memory.Refresh(owner.Session.State,original);
                var actor=CampaignCastView.OwnsGuest(owner.Session.CurrentEncounter.GuestId)
                    ? owner.Session.CurrentEncounter.GuestId=="guest_chunhyang" ? owner.Chunhyang : owner.Mongryong
                    : owner.Session.CurrentEncounter.GuestId=="guest_quixote" ? owner.Quixote : owner.Ordinary;
                OnSeat(actor,-2.2f);
                Assert.That(Vector3.Distance(actor.SeatAnchor.position,Stool(-.55f).bounds.center),Is.GreaterThan(1));
                Assert.That(owner.transform.Find("MEM-01 small star").position,Is.EqualTo(original+new Vector3(.32f,.76f,-.06f)));
            }
            foreach(int slot in new[]{0,16}) {
                At(slot,false); Animate(); OnSeat(slot==16 ? owner.Prince : owner.Ordinary,-.55f);
                Assert.That((slot==16 ? owner.Prince : owner.Ordinary).transform.position,Is.EqualTo(original),"ACT1 authored root is preserved.");
            }
        }
        [Test] public void PairedLoversUseTheTwoAdjacentUnreservedStoolsAndOneCupBetweenThem()
        {
            foreach(int slot in new[]{20,24}) {
                At(slot); Animate(); OnSeat(owner.Chunhyang,1.15f); OnSeat(owner.Mongryong,2.4f);
                Assert.That(cast.Paired,Is.True);
                Assert.That(Vector3.Distance(cast.SharedCupAnchor.position,new Vector3(1.775f,1.055f,-.65f)),Is.LessThan(.0001f));
                Assert.That(owner.GetComponentsInChildren<Transform>().Count(x=>x.name=="Lovers one shared cup anchor"),Is.EqualTo(1));
            }
        }
        [Test] public void ExistingServedCupFollowsReservedChairRoutingWithoutChangingLiquidOrBrewingHome()
        {
            var work=new GameObject("Reserved chair actual cup fixture"); work.transform.position=new Vector3(.8f,1.055f,-.65f);
            var view=work.AddComponent<SliceWorkbenchView>(); view.Initialize(null); Set("Workbench",view);
            var cup=work.transform.Find("Serving glass 180 ml"); var home=cup.position;
            var brew=new BrewingSession(content.Catalog,content.Tuning,content.Supply); brew.Pour("gin",ContainerRole.Serving,30);
            var snapshot=brew.Freeze();
            foreach(int slot in new[]{17,19,22,25,26,27,28,20,24}) {
                At(slot,true,CampaignPhase.Farewell,snapshot); Animate(); view.Refresh(null,null,0,snapshot);
                var expected=new Vector3(slot==20 || slot==24 ? 1.775f : -2.2f,1.055f,-.65f);
                Assert.That(Vector3.Distance(cup.position,expected),Is.LessThan(.0001f),"The one real serving subtree must follow the active guest.");
                Assert.That(owner.Session.State.CurrentService.Snapshot,Is.SameAs(snapshot)); Assert.That(view.ServingFillMl,Is.EqualTo(30));
                Assert.That(work.GetComponentsInChildren<Transform>(true).Count(x=>x.name=="Serving glass 180 ml"),Is.EqualTo(1));
                At(slot,true,CampaignPhase.Brewing); Animate(); Assert.That(cup.position,Is.EqualTo(home),"Brewing keeps its accepted input geometry.");
            }
            At(0,false,CampaignPhase.Farewell,snapshot); Animate(); Assert.That(cup.position,Is.EqualTo(home),"ACT1 serving position is preserved.");
            At(16,true,CampaignPhase.Complete,snapshot); Animate();
            Assert.That(cup.position,Is.EqualTo(home),"ACT1 Complete keeps the accepted last cup even after MEM01 is earned.");
            Assert.That(owner.Prince.Renderer.enabled,Is.False);
            cast.Hide(); Assert.That(cup.position,Is.EqualTo(home));
        }
        [TestCase(1280,720)] [TestCase(1920,1080)]
        public void PairedReadingClearsFourthStoolOpaquePixelsAndEveryAuthoredLineAtBothTextSizes(int width,int height)
        {
            At(24);
            typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule).GetField("defaultActions",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
            events=new GameObject("Reserved chair reading fixture events",typeof(EventSystem));
            typeof(EventSystem).GetMethod("OnEnable",Hidden).Invoke(events.GetComponent<EventSystem>(),null);
            var ui=owner.gameObject.AddComponent<CampaignUi>(); ui.Initialize(owner,owner.Font); ui.Refresh();
            var panel=((GameObject)typeof(CampaignUi).GetField("dialogue",Hidden).GetValue(ui)).GetComponent<RectTransform>();
            var body=(TextMeshProUGUI)typeof(CampaignUi).GetField("body",Hidden).GetValue(ui);
            // Independent physical fourth stool pose: the old UI must fail this even before seat routing changes.
            owner.Mongryong.transform.position=new Vector3(2.2285714f,0,-2.0043752f); owner.Mongryong.Show(true,true);
            owner.Camera.aspect=width/(float)height; cast.ApplyView(false);
            var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try {
                ImageConversion.LoadImage(source,File.ReadAllBytes(Surlhwa.Editor.CampaignExpansionBuilder.Root+"/MongryongSeated.png"));
                var pixels=source.GetPixels32(); var bounds=owner.Mongryong.SeatedMesh.bounds; float bottom=1;
                for(int y=0;y<source.height;y++) for(int x=0;x<source.width;x++) if(pixels[y*source.width+x].a>102) {
                    var local=new Vector3(bounds.min.x+(x+.5f)/source.width*bounds.size.x,bounds.min.y+(y+.5f)/source.height*bounds.size.y,0);
                    bottom=Mathf.Min(bottom,owner.Camera.WorldToViewportPoint(owner.Mongryong.Plane.transform.TransformPoint(local)).y);
                }
                float top=(panel.anchoredPosition.y+panel.rect.height)/720;
                Assert.That((bottom-top)*height,Is.GreaterThan(10*height/720f),"The fourth guest's opaque feet need visible separation from dialogue, including Large Text.");
                var paired=content.Encounters.Values.Where(x=>x.GuestIds.Count>1);
                var ids=paired.SelectMany(x=>x.Intro.Select(l=>l.TextId).Concat(x.Farewell).Concat(x.Responses.Values).Concat(new[]{x.SummaryTextId})).Distinct();
                foreach(bool large in new[]{false,true}) {
                    Set("LargeText",large); ui.Refresh();
                    foreach(string id in ids) {
                        body.text=content.Texts[id].Value; body.ForceMeshUpdate();
                        Assert.That(body.isTextOverflowing,Is.False,id+" at "+width+"x"+height+" large="+large);
                        Assert.That(body.textInfo.lineCount,Is.LessThanOrEqualTo(2),id+" must fit the bounded paired reading area.");
                    }
                }
                At(0,false); ui.Refresh();
                Assert.That(panel.anchoredPosition,Is.EqualTo(new Vector2(216,24))); Assert.That(panel.sizeDelta,Is.EqualTo(new Vector2(848,176)),"ACT1 reading geometry is unchanged.");
            } finally { Object.DestroyImmediate(source); }
        }
        [Test] public void DialogueBorderFollowsPairedBoundsAndRestoresActOneWithoutChangingOtherPanels()
        {
            At(0,false);
            typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule).GetField("defaultActions",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
            events=new GameObject("Dialogue border fixture events",typeof(EventSystem));
            typeof(EventSystem).GetMethod("OnEnable",Hidden).Invoke(events.GetComponent<EventSystem>(),null);
            var ui=owner.gameObject.AddComponent<CampaignUi>(); ui.Initialize(owner,owner.Font);
            var panel=((GameObject)typeof(CampaignUi).GetField("dialogue",Hidden).GetValue(ui)).GetComponent<RectTransform>();
            var rules=Enumerable.Range(0,panel.childCount).Select(panel.GetChild).Where(x=>x.name=="Quiet brass rule").Cast<RectTransform>().ToArray();
            Assert.That(rules.Length,Is.EqualTo(6));
            Rect Actual(RectTransform rule) {
                var corners=new Vector3[4]; rule.GetWorldCorners(corners);
                var low=rule.parent.InverseTransformPoint(corners[0]); var high=rule.parent.InverseTransformPoint(corners[2]);
                return Rect.MinMaxRect(low.x,low.y,high.x,high.y);
            }
            void Same(Rect actual,Rect expected,string label) {
                Assert.That(actual.x,Is.EqualTo(expected.x).Within(.02f),label+" x");
                Assert.That(actual.y,Is.EqualTo(expected.y).Within(.02f),label+" y");
                Assert.That(actual.width,Is.EqualTo(expected.width).Within(.02f),label+" width");
                Assert.That(actual.height,Is.EqualTo(expected.height).Within(.02f),label+" height");
            }
            void Border(float width,float height) {
                Canvas.ForceUpdateCanvases();
                var expected=new[]{new Rect(1,height-2,width-2,1),new Rect(1,1,width-2,1),new Rect(1,1,1,height-2),
                    new Rect(width-2,1,1,height-2),new Rect(5,height-6,13,2),new Rect(width-18,height-6,13,2)};
                for(int i=0;i<rules.Length;i++) {
                    var actual=Actual(rules[i]); Same(actual,expected[i],"dialogue rule "+i);
                    Assert.That(actual.xMin,Is.GreaterThanOrEqualTo(0)); Assert.That(actual.yMin,Is.GreaterThanOrEqualTo(0));
                    Assert.That(actual.xMax,Is.LessThanOrEqualTo(width)); Assert.That(actual.yMax,Is.LessThanOrEqualTo(height));
                }
            }
            Border(848,176);
            var others=ui.GetComponentsInChildren<RectTransform>(true).Where(x=>x.name=="Quiet brass rule" && x.parent!=panel).ToDictionary(x=>x,Actual);
            Assert.That(others.Count,Is.GreaterThan(6));
            At(24); ui.Refresh(); Border(1232,144);
            foreach(var pair in others) Same(Actual(pair.Key),pair.Value,"unchanged other panel rule");
            At(0,false); ui.Refresh(); Border(848,176);
            At(16,true,CampaignPhase.Complete); ui.Refresh(); Border(848,176);
            foreach(var pair in others) Same(Actual(pair.Key),pair.Value,"restored other panel rule");
        }
    }
}
