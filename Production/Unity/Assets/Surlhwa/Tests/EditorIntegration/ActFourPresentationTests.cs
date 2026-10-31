using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Surlhwa.Domain;
using Surlhwa.Presentation;
using Object=UnityEngine.Object;

namespace Surlhwa.SpatialTests
{
    public sealed class ActFourPresentationTests
    {
        private const string ScenePath="Assets/Surlhwa/Scenes/Bar_Q1_Act4.unity";
        private const string Root="Assets/Surlhwa/Act4Assets";
        private static T Member<T>(CampaignMemoryView memory,string name)
        { var property=typeof(CampaignMemoryView).GetProperty(name); Assert.That(property,Is.Not.Null,name+" is a receipt projection."); return (T)property.GetValue(memory); }
        // Presentation fixture only; actual grants/reloads are independently tested through the campaign.
        private static CampaignCheckpoint Memories(bool fourth=true) => new CampaignCheckpoint(1,"123456781234123412341234567890ab",0,
            CampaignPhase.Complete,0,null,null,new[]{"presentation_fixture"},Array.Empty<string>(),Array.Empty<string>(),Array.Empty<CommittedService>(),
            new[]{new MemoryReceipt("mem_01","star_dim","fixture",1),new MemoryReceipt("mem_02","swing_empty_place","fixture",1),new MemoryReceipt("mem_03","windmill_fragment","fixture",1)}
                .Concat(fourth ? new[]{new MemoryReceipt("mem_04","stage_skull_shadow","fixture",1)} : Array.Empty<MemoryReceipt>()),
            new Dictionary<string,string>(),new Dictionary<string,string[]>());

        [Test] public void HamletQ1ProjectionUsesHisSourceContactThroughFocusWithoutMovingPhysicalAnchors()
        {
            EditorSceneManager.OpenScene(Surlhwa.Editor.ActThreeExpansionBuilder.ScenePath);
            GameObject fixture=null; Texture2D source=null; Mesh mesh=null;
            try {
                var c=Object.FindFirstObjectByType<CampaignController>(); var view=Object.FindFirstObjectByType<Q1ProductionView>();
                var binding=typeof(CampaignController).GetField("Hamlet"); Assert.That(binding,Is.Not.Null,"Hamlet needs an explicit actor binding.");
                fixture=Object.Instantiate(c.Ordinary.gameObject,c.Ordinary.transform.parent); var actor=fixture.GetComponent<CharacterView>(); binding.SetValue(c,actor);
                actor.GuestId="guest_hamlet"; actor.VisualBindingId="visual_hamlet";
                source=new Texture2D(2,2,TextureFormat.RGBA32,false); Assert.That(ImageConversion.LoadImage(source,File.ReadAllBytes(Root+"/HamletSeated.png")),Is.True);
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

        [Test] public void HamletMemoryReadsOnlyItsReceiptAndStaysSupportedInFrontOfOpaqueNiche()
        {
            EditorSceneManager.OpenScene(Surlhwa.Editor.ActThreeExpansionBuilder.ScenePath);
            var go=new GameObject("ACT 4 memory fixture");
            try {
                var memory=go.AddComponent<CampaignMemoryView>(); var chair=Object.FindFirstObjectByType<CampaignController>().Ordinary.transform.position;
                memory.Refresh(Memories(false),chair); memory.SetPresentationFacing(Q1ProductionView.WorldAngle); Assert.That(Member<bool>(memory,"HamletVisible"),Is.False);
                var old=new[]{go.transform.Find("MEM-01 small star"),memory.LoversSwing,memory.WindmillFragment}; var places=old.Select(x=>x.position).ToArray();
                memory.Refresh(Memories(),chair); memory.SetPresentationFacing(Q1ProductionView.WorldAngle);
                Assert.That(Member<bool>(memory,"HamletVisible"),Is.True); Assert.That(memory.Visible && memory.LoversVisible && memory.WindmillVisible,Is.True);
                Assert.That(old.Select(x=>x.position),Is.EqualTo(places));
                var stage=Member<Transform>(memory,"HamletStage"); var skull=Member<Transform>(memory,"HamletSkull"); var shadow=Member<Transform>(memory,"HamletShadow");
                Assert.That(skull.parent,Is.SameAs(stage.parent)); Assert.That(shadow.parent,Is.SameAs(stage.parent));
                Physics.SyncTransforms(); var shelf=GameObject.Find("C_MemoryShelf").GetComponent<Collider>().bounds;
                foreach(var point in stage.GetComponent<MeshFilter>().sharedMesh.vertices.Where(x=>x.y<.0001f)) {
                    var at=stage.TransformPoint(point); Assert.That(at.y-shelf.max.y,Is.InRange(.001f,.005f));
                    Assert.That(shelf.Contains(new Vector3(at.x,shelf.center.y,at.z)),Is.True,"Every base corner rests over the shelf.");
                }
                Assert.That(new[]{stage,skull,shadow}.All(x=>x.GetComponent<Renderer>().bounds.max.z<3.05f),Is.True,"Niche cannot hide the new memory.");
                Assert.That(stage.parent.GetComponentsInChildren<Light>().Length,Is.Zero,"No room-wide lighting change.");
                memory.Refresh(Memories(),chair); Assert.That(go.GetComponentsInChildren<Transform>(true).Count(x=>x.name=="MEM-04 stage skull and shadow"),Is.EqualTo(1));
                memory.Refresh(Memories(false),chair); Assert.That(Member<bool>(memory,"HamletVisible"),Is.False); Assert.That(memory.Visible && memory.LoversVisible && memory.WindmillVisible,Is.True);
            } finally { Object.DestroyImmediate(go); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); }
        }

        [Test] public void SeparateActFourSceneBindsHamletAndExactAcceptedHistoryWithoutBorrowingActorArt()
        {
            Assert.That(File.Exists(ScenePath),Is.True,"ACT 4 needs its separate prepared scene."); EditorSceneManager.OpenScene(ScenePath);
            try {
                var c=Object.FindFirstObjectByType<CampaignController>(); var binding=typeof(CampaignController).GetField("Hamlet"); Assert.That(binding,Is.Not.Null);
                var actor=(CharacterView)binding.GetValue(c); Assert.That(actor,Is.Not.Null);
                Assert.That(c.Bundle.text,Is.EqualTo(File.ReadAllText("../Content/Campaign/act4.json")));
                Assert.That(c.PriorBundles.Select(x=>x.text),Is.EqualTo(new[]{File.ReadAllText("../Content/Campaign/act3.json"),File.ReadAllText("../Content/Campaign/act2.json")}));
                Assert.That(c.LegacyBundle.text,Is.EqualTo(File.ReadAllText("../Content/Campaign/act1.json")));
                Assert.That(actor.GuestId,Is.EqualTo("guest_hamlet")); Assert.That(actor.VisualBindingId,Is.EqualTo("visual_hamlet"));
                Assert.That(actor.SeatedMaterial,Is.Not.SameAs(c.Quixote.SeatedMaterial)); Assert.That(actor.SeatedMaterial,Is.Not.SameAs(c.Prince.SeatedMaterial));
                Assert.That(actor.WalkMeshes.Length,Is.EqualTo(4)); Assert.That(actor.WalkMaterials.Length,Is.EqualTo(4));
                Assert.That(actor.SeatedMesh.bounds.min.x,Is.EqualTo(-34f/70).Within(.0001f));
                Assert.That(actor.StandingMesh.bounds.min.x,Is.EqualTo(-25f/70).Within(.0001f));
                Assert.That(actor.WalkMeshes.All(x=>Mathf.Abs(x.bounds.min.x+46f/70)<.0001f),Is.True);
            } finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); }
        }

        [TestCase(1280,720)] [TestCase(1920,1080)]
        public void AllFourMemoriesAndHamletPartsSurviveActualQ1Depth(int width,int height)
        {
            EditorSceneManager.OpenScene(Surlhwa.Editor.ActThreeExpansionBuilder.ScenePath);
            var originals=new Dictionary<Renderer,Material[]>(); var copies=new Dictionary<Material,Material>();
            GameObject fixture=null; RenderTexture target=null; Texture2D readback=null; var previous=RenderTexture.active;
            try {
                foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
                    var materials=renderer.sharedMaterials; originals.Add(renderer,materials); renderer.sharedMaterials=materials.Select(material=>{
                        if(material==null) return null; if(!copies.TryGetValue(material,out var copy)) { copy=new Material(material); copies.Add(material,copy); } return copy;
                    }).ToArray();
                }
                var owner=Object.FindFirstObjectByType<CampaignController>(); var camera=owner.Camera; camera.aspect=width/(float)height;
                fixture=new GameObject("ACT 4 diagnostic memory render"); var memory=fixture.AddComponent<CampaignMemoryView>(); memory.Refresh(Memories(),owner.Ordinary.transform.position); memory.SetPresentationFacing(camera.transform.rotation);
                var parts=new[]{fixture.transform.Find("MEM-01 small star"),memory.LoversSwing,memory.LoversEmptyPlace,memory.WindmillFragment,memory.WindmillWing,
                    Member<Transform>(memory,"HamletStage"),Member<Transform>(memory,"HamletSkull"),Member<Transform>(memory,"HamletShadow")};
                var colors=new[]{Color.yellow,Color.magenta,Color.cyan,Color.green,Color.red,Color.blue,Color.white,new Color(1,.5f,0)};
                for(int i=0;i<parts.Length;i++) parts[i].GetComponent<Renderer>().sharedMaterial.SetColor("_BaseColor",colors[i]);
                target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32); target.Create(); readback=new Texture2D(width,height,TextureFormat.RGB24,false);
                var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target}; Assert.That(RenderPipeline.SupportsRenderRequest(camera,request),Is.True);
                Color32[] Render() { RenderPipeline.SubmitRenderRequest(camera,request); RenderTexture.active=target; readback.ReadPixels(new Rect(0,0,width,height),0,0); readback.Apply(); return readback.GetPixels32(); }
                var visible=Render(); fixture.SetActive(false); var hidden=Render(); var counts=new int[8];
                for(int i=0;i<visible.Length;i++) {
                    var p=visible[i]; var b=hidden[i]; if(Math.Abs(p.r-b.r)+Math.Abs(p.g-b.g)+Math.Abs(p.b-b.b)<90) continue;
                    bool r=p.r>230,g=p.g>230,bigBlue=p.b>230,lr=p.r<40,lg=p.g<40,lb=p.b<40;
                    if(r&&g&&lb) counts[0]++; if(r&&bigBlue&&lg) counts[1]++; if(g&&bigBlue&&lr) counts[2]++;
                    if(g&&lr&&lb) counts[3]++; if(r&&lg&&lb) counts[4]++; if(bigBlue&&lr&&lg) counts[5]++;
                    if(r&&g&&bigBlue) counts[6]++; if(r&&p.g>100&&p.g<210&&lb) counts[7]++;
                }
                TestContext.WriteLine("ACT4 diagnostic memory pixels "+width+"x"+height+": "+string.Join(",",counts));
                float area=width*height/(1280f*720); int[] minima={10,250,35,70,30,35,25,25};
                for(int i=0;i<parts.Length;i++) Assert.That(counts[i],Is.GreaterThan(minima[i]*area),parts[i].name+" must survive actual room depth.");
            } finally {
                RenderTexture.active=previous; if(target!=null) { target.Release(); Object.DestroyImmediate(target); } if(readback!=null) Object.DestroyImmediate(readback); if(fixture!=null) Object.DestroyImmediate(fixture);
                foreach(var pair in originals) if(pair.Key!=null) pair.Key.sharedMaterials=pair.Value; foreach(var copy in copies.Values) Object.DestroyImmediate(copy);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            }
        }
    }
}
