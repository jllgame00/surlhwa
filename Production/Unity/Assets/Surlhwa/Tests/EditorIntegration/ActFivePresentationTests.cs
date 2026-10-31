using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Domain;
using Surlhwa.Presentation;
using Object=UnityEngine.Object;

namespace Surlhwa.SpatialTests
{
    public sealed class ActFivePresentationTests
    {
        private const string ScenePath="Assets/Surlhwa/Scenes/Bar_Q1_Act5.unity";
        private const string Root="Assets/Surlhwa/Act5Assets";
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private sealed class Store : ICampaignSaveStore { public Task SaveAsync(CampaignCheckpoint state)=>Task.CompletedTask; }
        private static T Member<T>(CampaignMemoryView memory,string name)
        { var property=typeof(CampaignMemoryView).GetProperty(name); Assert.That(property,Is.Not.Null,name+" must project the earned receipt."); return (T)property.GetValue(memory); }
        private static FieldInfo FaustBinding()
        { var binding=typeof(CampaignController).GetField("Faust"); Assert.That(binding,Is.Not.Null,"Faust must have his own actor binding."); return binding; }
        // Presentation fixtures only. Natural transactional grants are exercised by campaign and Player tests.
        private static CampaignCheckpoint Memories(string fifth="faint_circle_broken_glass_red_light") => new CampaignCheckpoint(1,"123456781234123412341234567890ab",0,
            CampaignPhase.Complete,0,null,null,new[]{"presentation_fixture"},Array.Empty<string>(),Array.Empty<string>(),Array.Empty<CommittedService>(),
            new[]{new MemoryReceipt("mem_01","star_dim","fixture",1),new MemoryReceipt("mem_02","swing_empty_place","fixture",1),
                new MemoryReceipt("mem_03","windmill_fragment","fixture",1),new MemoryReceipt("mem_04","stage_skull_shadow","fixture",1)}
                .Concat(fifth==null ? Array.Empty<MemoryReceipt>() : new[]{new MemoryReceipt("mem_05",fifth,"fixture",1)}),
            new Dictionary<string,string>(),new Dictionary<string,string[]>());

        [Test] public void FaustSourceHipStaysOnTheSeatThroughFocusWithoutMovingAnchorsOrBuryingFeet()
        {
            EditorSceneManager.OpenScene(Surlhwa.Editor.ActFourExpansionBuilder.ScenePath);
            GameObject fixture=null; Texture2D source=null; Mesh mesh=null;
            try {
                var c=Object.FindFirstObjectByType<CampaignController>(); var view=Object.FindFirstObjectByType<Q1ProductionView>(); var binding=FaustBinding();
                fixture=Object.Instantiate(c.Ordinary.gameObject,c.Ordinary.transform.parent); var actor=fixture.GetComponent<CharacterView>(); binding.SetValue(c,actor);
                actor.GuestId="guest_faust"; actor.VisualBindingId="visual_faust";
                source=new Texture2D(2,2,TextureFormat.RGBA32,false); Assert.That(ImageConversion.LoadImage(source,File.ReadAllBytes(Root+"/FaustSeated.png")),Is.True);
                Assert.That(source.width,Is.EqualTo(96)); Assert.That(source.height,Is.EqualTo(100)); Assert.That(source.GetPixel(46,34).a,Is.GreaterThan(.4f));
                mesh=new Mesh{vertices=new[]{new Vector3(-34f/70,-8f/70,0),new Vector3(-34f/70,92f/70,0),new Vector3(62f/70,92f/70,0),new Vector3(62f/70,-8f/70,0)},triangles=new[]{0,1,2,0,2,3}};
                mesh.RecalculateBounds(); actor.SeatedMesh=mesh; actor.Show(true,true);
                actor.Plane.transform.localPosition=Vector3.zero; actor.Plane.transform.localRotation=Quaternion.identity; actor.Plane.transform.localScale=Vector3.one;
                var root=actor.transform.position; var seat=actor.SeatAnchor.position; var pixels=source.GetPixels32();
                foreach(bool focused in new[]{false,true,false}) {
                    view.ApplyView(focused); Vector3 thickness=focused ? Vector3.zero : c.Camera.transform.forward*.14f;
                    Assert.That(Vector3.Distance(actor.Plane.transform.TransformPoint(new Vector3(12f/70,26f/70,0))+thickness,seat),Is.LessThan(.0001f));
                    float floor=float.PositiveInfinity;
                    for(int y=0;y<source.height;y++) for(int x=0;x<source.width;x++) if(pixels[y*source.width+x].a>102)
                        floor=Mathf.Min(floor,(actor.Plane.transform.TransformPoint(new Vector3((x+.5f-34)/70,(y+.5f-8)/70,0))+thickness).y);
                    Assert.That(floor,Is.GreaterThan(.01f)); Assert.That(actor.transform.position,Is.EqualTo(root)); Assert.That(actor.SeatAnchor.position,Is.EqualTo(seat));
                }
                actor.Show(true,false); view.ApplyView(false); Assert.That(actor.Plane.transform.localPosition,Is.EqualTo(Vector3.zero));
            } finally { if(fixture!=null) Object.DestroyImmediate(fixture); if(mesh!=null) Object.DestroyImmediate(mesh); if(source!=null) Object.DestroyImmediate(source); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); }
        }

        [Test] public void FaustReceiptAloneAddsOneSupportedCircleBrokenGlassAndConfinedRedPoint()
        {
            EditorSceneManager.OpenScene(Surlhwa.Editor.ActFourExpansionBuilder.ScenePath); var go=new GameObject("ACT 5 receipt projection fixture");
            try {
                var memory=go.AddComponent<CampaignMemoryView>(); var chair=Object.FindFirstObjectByType<CampaignController>().Ordinary.transform.position;
                memory.Refresh(Memories(null),chair); memory.SetPresentationFacing(Q1ProductionView.WorldAngle); Assert.That(Member<bool>(memory,"FaustVisible"),Is.False);
                var old=new[]{go.transform.Find("MEM-01 small star"),memory.LoversSwing,memory.WindmillFragment,memory.HamletStage}; var places=old.Select(x=>x.position).ToArray();
                memory.Refresh(Memories("unearned_variant"),chair); Assert.That(Member<bool>(memory,"FaustVisible"),Is.False);
                memory.Refresh(Memories(),chair); Assert.That(Member<bool>(memory,"FaustVisible"),Is.True);
                Assert.That(memory.Visible && memory.LoversVisible && memory.WindmillVisible && memory.HamletVisible,Is.True); Assert.That(old.Select(x=>x.position),Is.EqualTo(places));
                var circle=Member<Transform>(memory,"FaustCircle"); var glass=Member<Transform>(memory,"FaustGlass"); var glow=Member<Transform>(memory,"FaustGlow");
                var points=circle.GetComponent<MeshFilter>().sharedMesh.vertices.Select(circle.TransformPoint).ToArray();
                Physics.SyncTransforms(); var floor=GameObject.Find("C_Floor").GetComponent<Collider>().bounds;
                foreach(var point in points) { Assert.That(point.y-floor.max.y,Is.InRange(.001f,.01f)); Assert.That(floor.Contains(new Vector3(point.x,floor.center.y,point.z)),Is.True); }
                var before=new[]{circle,glass,glow}.Select(x=>x.localToWorldMatrix).ToArray(); memory.SetPresentationFacing(Q1ProductionView.FocusAngle);
                Assert.That(new[]{circle,glass,glow}.Select(x=>x.localToWorldMatrix),Is.EqualTo(before),"Physical floor memory must not tilt away from support during focus.");
                var bounds=glass.GetComponent<Renderer>().bounds; Assert.That(bounds.min.y-floor.max.y,Is.InRange(.001f,.012f));
                Assert.That(bounds.size.y,Is.InRange(.10f,.35f)); Assert.That(glow.GetComponent<Renderer>().bounds.size.magnitude,Is.LessThan(.07f));
                Assert.That(bounds.Contains(glow.position+Vector3.forward*.004f),Is.True,"The red point stays within the broken glass.");
                Assert.That(glow.GetComponent<Renderer>().sharedMaterial.GetColor("_BaseColor").r,Is.GreaterThan(glow.GetComponent<Renderer>().sharedMaterial.GetColor("_BaseColor").g*2));
                Assert.That(circle.parent.GetComponentsInChildren<Light>().Length,Is.Zero,"The small red point does not light or darken the room.");
                Assert.That(circle.parent.GetComponentsInChildren<Collider>().Length,Is.Zero,"This receipt projection adds no trigger or interaction.");
                memory.Refresh(Memories(),chair); Assert.That(go.GetComponentsInChildren<Transform>(true).Count(x=>x.name=="MEM-05 faint circle broken glass and red light"),Is.EqualTo(1));
                memory.Refresh(Memories(null),chair); Assert.That(Member<bool>(memory,"FaustVisible"),Is.False); Assert.That(memory.Visible && memory.LoversVisible && memory.WindmillVisible && memory.HamletVisible,Is.True);
            } finally { Object.DestroyImmediate(go); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); }
        }

        [Test] public void SeparateSceneRetainsExactHistoryAndUsesFaustSourceArtWithIndependentSoleContact()
        {
            Assert.That(File.Exists(ScenePath),Is.True,"ACT 5 requires a separate prepared scene."); EditorSceneManager.OpenScene(ScenePath);
            try {
                var c=Object.FindFirstObjectByType<CampaignController>(); var actor=(CharacterView)FaustBinding().GetValue(c); Assert.That(actor,Is.Not.Null);
                Assert.That(c.Bundle.text,Is.EqualTo(File.ReadAllText("../Content/Campaign/act5.json")));
                Assert.That(c.PriorBundles.Select(x=>x.text),Is.EqualTo(new[]{File.ReadAllText("../Content/Campaign/act4.json"),File.ReadAllText("../Content/Campaign/act3.json"),File.ReadAllText("../Content/Campaign/act2.json")}));
                Assert.That(c.LegacyBundle.text,Is.EqualTo(File.ReadAllText("../Content/Campaign/act1.json")));
                Assert.That(actor.GuestId,Is.EqualTo("guest_faust")); Assert.That(actor.VisualBindingId,Is.EqualTo("visual_faust"));
                Assert.That(new[]{c.Hamlet,c.Quixote,c.Prince,c.Ordinary}.All(x=>x.SeatedMaterial!=actor.SeatedMaterial && x.SeatedMesh!=actor.SeatedMesh),Is.True);
                Assert.That(actor.SeatedMesh.bounds.min.x,Is.EqualTo(-34f/70).Within(.0001f)); Assert.That(actor.StandingMesh.bounds.min.x,Is.EqualTo(-20f/70).Within(.0001f));
                Assert.That(actor.WalkMeshes.Length,Is.EqualTo(4)); Assert.That(actor.WalkMaterials.Length,Is.EqualTo(4)); Assert.That(actor.WalkMeshes.All(x=>Mathf.Abs(x.bounds.min.x+46f/70)<.0001f),Is.True);
            } finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); }
        }

        [Test] public async Task ExplicitFaustRoutingPreservesTheRememberedChairAndHidesOnOtherVisitsAndMenu()
        {
            EditorSceneManager.OpenScene(Surlhwa.Editor.ActFourExpansionBuilder.ScenePath); GameObject fixture=null;
            try {
                var c=Object.FindFirstObjectByType<CampaignController>(); var binding=FaustBinding();
                Assert.That(File.Exists("../Content/Campaign/act5.json"),Is.True,"ACT 5 authored rows must exist for routing."); var content=CampaignContentLoader.Load(File.ReadAllText("../Content/Campaign/act5.json"));
                fixture=Object.Instantiate(c.Ordinary.gameObject,c.Ordinary.transform.parent); var actor=fixture.GetComponent<CharacterView>(); binding.SetValue(c,actor);
                void Set(string property,object value)=>typeof(CampaignController).GetProperty(property).SetValue(c,value);
                Set("Content",content); Set("Input",c.gameObject.AddComponent<SliceInputAdapter>()); Set("Ui",c.gameObject.AddComponent<CampaignUi>());
                var audio=c.gameObject.AddComponent<CampaignAudio>(); audio.Initialize(c); Set("Audio",audio);
                var original=c.Ordinary.transform.position; typeof(CampaignController).GetField("seatPosition",Hidden).SetValue(c,original);
                var cast=c.GetComponent<CampaignCastView>(); cast.Initialize(c,original); Set("Cast",cast);
                var select=typeof(CampaignController).GetMethod("SelectGuestActor",Hidden); Assert.That(select.Invoke(c,new object[]{"guest_faust"}),Is.SameAs(actor));
                void At(int slot,CampaignPhase phase=CampaignPhase.Request) {
                    c.Session?.Dispose(); var state=new CampaignCheckpoint(1,"123456781234123412341234567890ab",slot,phase,0,null,null,
                        content.Slots.Take(slot+1).Select(x=>x.EncounterId),Array.Empty<string>(),Array.Empty<string>(),Array.Empty<CommittedService>(),Memories(null).Memories,
                        content.KnownSeeds.ToDictionary(x=>x.Key,x=>x.Value),new Dictionary<string,string[]>());
                    Set("Session",CampaignSession.Continue(content,new Store(),state)); typeof(CampaignController).GetMethod("AnimateGuest",Hidden).Invoke(c,null);
                }
                foreach(int slot in new[]{33,36}) {
                    At(slot); Physics.SyncTransforms(); var stool=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Single(x=>(x.name=="C_Stool" || x.name=="C_ExtraStool") && Mathf.Abs(x.bounds.center.x+2.2f)<.01f);
                    Assert.That(actor.Renderer.enabled,Is.True); Assert.That(Vector3.Distance(actor.SeatAnchor.position,stool.bounds.ClosestPoint(actor.SeatAnchor.position)),Is.LessThan(.003f));
                    Assert.That(c.Prince.Renderer.enabled || c.Ordinary.Renderer.enabled || c.Hamlet.Renderer.enabled || c.Quixote.Renderer.enabled,Is.False);
                    foreach(int other in new[]{34,35,20,30}) { At(other); Assert.That(actor.Renderer.enabled,Is.False,"Faust must hide for ordinary, paired and Hamlet visits."); }
                    At(slot,CampaignPhase.NightEnd); Assert.That(actor.Renderer.enabled,Is.False);
                }
                At(36); typeof(CampaignController).GetField("store",Hidden).SetValue(c,new CampaignSaveStore(Path.GetFullPath("../Validation/Evidence/act5-menu-fixture-"+Guid.NewGuid().ToString("N")),content));
                c.ReturnToSavedMenu(); for(int i=0;c.Busy && i<100;i++) await Task.Delay(10);
                Assert.That(c.Busy,Is.False); Assert.That(c.Session,Is.Null); Assert.That(actor.Renderer.enabled,Is.False); Assert.That(actor.Contact.enabled,Is.False);
            } finally { if(fixture!=null) Object.DestroyImmediate(fixture); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); }
        }

        [TestCase(1280,720)] [TestCase(1920,1080)]
        public void AllFiveMemoriesRemainVisibleThroughActualRoomDepth(int width,int height)
        {
            EditorSceneManager.OpenScene(Surlhwa.Editor.ActFourExpansionBuilder.ScenePath);
            var originals=new Dictionary<Renderer,Material[]>(); var copies=new Dictionary<Material,Material>();
            GameObject fixture=null; RenderTexture target=null; Texture2D readback=null; var previous=RenderTexture.active;
            try {
                foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
                    var materials=renderer.sharedMaterials; originals.Add(renderer,materials); renderer.sharedMaterials=materials.Select(material=>{
                        if(material==null) return null; if(!copies.TryGetValue(material,out var copy)) { copy=new Material(material); copies.Add(material,copy); } return copy;
                    }).ToArray();
                }
                var c=Object.FindFirstObjectByType<CampaignController>(); var camera=c.Camera; camera.aspect=width/(float)height;
                fixture=new GameObject("ACT 5 diagnostic memory render"); var memory=fixture.AddComponent<CampaignMemoryView>(); memory.Refresh(Memories(),c.Ordinary.transform.position); memory.SetPresentationFacing(camera.transform.rotation);
                var parts=new[]{fixture.transform.Find("MEM-01 small star"),memory.LoversSwing,memory.LoversEmptyPlace,memory.WindmillFragment,memory.WindmillWing,memory.HamletStage,memory.HamletSkull,memory.HamletShadow,
                    Member<Transform>(memory,"FaustCircle"),Member<Transform>(memory,"FaustGlass"),Member<Transform>(memory,"FaustGlow")};
                target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32); target.Create(); readback=new Texture2D(width,height,TextureFormat.RGB24,false);
                var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target}; Assert.That(RenderPipeline.SupportsRenderRequest(camera,request),Is.True);
                Color32[] Render() { RenderPipeline.SubmitRenderRequest(camera,request); RenderTexture.active=target; readback.ReadPixels(new Rect(0,0,width,height),0,0); readback.Apply(); return readback.GetPixels32(); }
                // All parts retain their real geometry and depth. Per-part bright/dark captures
                // isolate visibility without hiding other memories or assigning sorting overrides.
                var counts=new int[parts.Length];
                for(int n=0;n<parts.Length;n++) {
                    var material=parts[n].GetComponent<Renderer>().sharedMaterial; var tint=material.GetColor("_BaseColor");
                    material.SetColor("_BaseColor",Color.white); var bright=Render(); material.SetColor("_BaseColor",Color.black); var dark=Render(); material.SetColor("_BaseColor",tint);
                    counts[n]=bright.Where((p,i)=>Math.Abs(p.r-dark[i].r)+Math.Abs(p.g-dark[i].g)+Math.Abs(p.b-dark[i].b)>100).Count();
                }
                TestContext.WriteLine("ACT5 diagnostic memory pixels "+width+"x"+height+": "+string.Join(",",counts));
                float area=width*height/(1280f*720); int[] minima={10,250,35,70,30,35,25,25,20,20,2};
                for(int i=0;i<parts.Length;i++) Assert.That(counts[i],Is.GreaterThan(minima[i]*area),parts[i].name+" must survive actual room depth.");
            } finally {
                RenderTexture.active=previous; if(target!=null) { target.Release(); Object.DestroyImmediate(target); } if(readback!=null) Object.DestroyImmediate(readback); if(fixture!=null) Object.DestroyImmediate(fixture);
                foreach(var pair in originals) if(pair.Key!=null) pair.Key.sharedMaterials=pair.Value; foreach(var copy in copies.Values) Object.DestroyImmediate(copy);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            }
        }
    }
}
