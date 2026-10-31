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
    public sealed class ActThreePresentationTests
    {
        // Presentation-only fixture. Real departure/receipt ownership is validated in domain and Player tests.
        private static CampaignCheckpoint Memories(bool third=true) => new CampaignCheckpoint(1,"123456781234123412341234567890ab",0,
            CampaignPhase.Complete,0,null,null,new[]{"presentation_fixture"},Array.Empty<string>(),Array.Empty<string>(),Array.Empty<CommittedService>(),
            new[]{new MemoryReceipt("mem_01","star_dim","fixture",1),new MemoryReceipt("mem_02","swing_empty_place","fixture",1)}
                .Concat(third ? new[]{new MemoryReceipt("mem_03","windmill_fragment","fixture",1)} : Array.Empty<MemoryReceipt>()),
            new Dictionary<string,string>(),new Dictionary<string,string[]>());

        [Test] public void ActThreeUsesDistinctQuixoteArtAndSourcePelvisWithoutMovingThePhysicalSeat()
        {
            EditorSceneManager.OpenScene(Surlhwa.Editor.ActThreeExpansionBuilder.ScenePath);
            Texture2D source=null;
            try {
                var c=Object.FindFirstObjectByType<CampaignController>(); var view=Object.FindFirstObjectByType<Q1ProductionView>(); var actor=c.Quixote;
                Assert.That(c.Bundle.text,Is.EqualTo(File.ReadAllText("../Content/Campaign/act3.json")));
                Assert.That(c.PriorBundles.Select(x=>x.text),Is.EqualTo(new[]{File.ReadAllText("../Content/Campaign/act2.json")}));
                Assert.That(c.LegacyBundle.text,Is.EqualTo(File.ReadAllText("../Content/Campaign/act1.json")));
                Assert.That(actor.GuestId,Is.EqualTo("guest_quixote")); Assert.That(actor.VisualBindingId,Is.EqualTo("visual_quixote"));
                Assert.That(actor.Renderer,Is.Not.SameAs(c.Prince.Renderer)); Assert.That(actor.SeatedMaterial,Is.Not.SameAs(c.Prince.SeatedMaterial));
                var root=actor.transform.position; var seat=actor.SeatAnchor.position;
                source=new Texture2D(2,2,TextureFormat.RGBA32,false);
                Assert.That(ImageConversion.LoadImage(source,File.ReadAllBytes(Surlhwa.Editor.ActThreeExpansionBuilder.Root+"/QuixoteSeated.png")),Is.True);
                // Inspected source seam: (46,34 bottom-up), not the Prince or lovers' contact.
                Assert.That(source.GetPixel(46,34).a,Is.GreaterThan(.4f)); var pixels=source.GetPixels32(); var bounds=actor.SeatedMesh.bounds;
                Vector3 Pixel(float x,float y) => new Vector3(bounds.min.x+x/source.width*bounds.size.x,bounds.min.y+y/source.height*bounds.size.y,0);
                actor.Show(true,true);
                foreach(bool focused in new[]{false,true,false}) {
                    view.ApplyView(focused);
                    Vector3 thickness=focused ? Vector3.zero : c.Camera.transform.forward*.14f;
                    Assert.That(Vector3.Distance(actor.Plane.transform.TransformPoint(Pixel(46,34))+thickness,seat),Is.LessThan(.0001f));
                    float floor=float.PositiveInfinity;
                    for(int y=0;y<source.height;y++) for(int x=0;x<source.width;x++) if(pixels[y*source.width+x].a>102)
                        floor=Mathf.Min(floor,(actor.Plane.transform.TransformPoint(Pixel(x+.5f,y+.5f))+thickness).y);
                    Assert.That(floor,Is.GreaterThan(.01f),"Opaque feet/hem must not be buried below the floor.");
                    Assert.That(actor.transform.position,Is.EqualTo(root)); Assert.That(actor.SeatAnchor.position,Is.EqualTo(seat));
                }
                actor.Show(true,false); view.ApplyView(false);
                Assert.That(actor.Plane.transform.localPosition,Is.EqualTo(Vector3.zero));
                Assert.That(actor.WalkMeshes.Length,Is.EqualTo(4)); Assert.That(actor.WalkMaterials.Length,Is.EqualTo(4));
            } finally { if(source!=null) Object.DestroyImmediate(source); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); }
        }

        [Test] public void WindmillReceiptAddsOneAttachedTiltedWingBelowShelfWithoutReplacingEarlierMemories()
        {
            EditorSceneManager.OpenScene(Surlhwa.Editor.ActThreeExpansionBuilder.ScenePath);
            var go=new GameObject("ACT 3 memory fixture");
            try {
                var memory=go.AddComponent<CampaignMemoryView>(); var chair=Object.FindFirstObjectByType<CampaignController>().Ordinary.transform.position;
                memory.Refresh(Memories(false),chair); memory.SetPresentationFacing(Q1ProductionView.WorldAngle);
                Assert.That(memory.Visible && memory.LoversVisible,Is.True); Assert.That(memory.WindmillVisible,Is.False);
                var star=go.transform.Find("MEM-01 small star"); var starAt=star.position; var swingAt=memory.LoversSwing.position;
                memory.Refresh(Memories(),chair);
                Assert.That(memory.Visible && memory.LoversVisible && memory.WindmillVisible,Is.True);
                Assert.That(star.position,Is.EqualTo(starAt)); Assert.That(memory.LoversSwing.position,Is.EqualTo(swingAt));
                Assert.That(memory.WindmillWing.parent,Is.SameAs(memory.WindmillFragment));
                Assert.That(Quaternion.Angle(memory.WindmillWing.localRotation,Quaternion.identity),Is.InRange(20f,35f));
                var joint=memory.WindmillWing.localPosition;
                Assert.That(memory.WindmillFragment.GetComponent<MeshFilter>().sharedMesh.bounds.Contains(new Vector3(joint.x,joint.y,0)),Is.True,"The tilted wing stays attached at the hub.");
                Assert.That(Mathf.Abs(joint.z),Is.LessThan(.005f),"The attached wing uses only a thin overlap surface.");
                Physics.SyncTransforms(); var floor=GameObject.Find("C_Floor").GetComponent<Collider>().bounds;
                foreach(var point in memory.WindmillFragment.GetComponent<MeshFilter>().sharedMesh.vertices.Where(x=>x.y<.0001f)) {
                    var at=memory.WindmillFragment.TransformPoint(point);
                    Assert.That(at.y-floor.max.y,Is.InRange(.001f,.01f));
                    Assert.That(floor.Contains(new Vector3(at.x,floor.center.y,at.z)),Is.True);
                }
                var shelf=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(x=>x.name=="Shelf lip" && x.bounds.center.x<0 && x.bounds.center.y<1).Single().bounds;
                Assert.That(memory.WindmillFragment.GetComponentsInChildren<Renderer>().Max(x=>x.bounds.max.y),Is.LessThan(shelf.min.y));
                memory.Refresh(Memories(),chair);
                Assert.That(go.GetComponentsInChildren<Transform>(true).Count(x=>x.name=="MEM-03 windmill fragment"),Is.EqualTo(1));
                memory.Refresh(Memories(false),chair); Assert.That(memory.WindmillVisible,Is.False); Assert.That(memory.LoversVisible && memory.Visible,Is.True);
            } finally { Object.DestroyImmediate(go); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); }
        }

        [TestCase(1280,720)] [TestCase(1920,1080)]
        public void AllThreeEarnedMemoriesAndAttachedWingRemainVisibleThroughActualQ1Depth(int width,int height)
        {
            EditorSceneManager.OpenScene(Surlhwa.Editor.ActThreeExpansionBuilder.ScenePath);
            var originals=new Dictionary<Renderer,Material[]>(); var copies=new Dictionary<Material,Material>();
            GameObject fixture=null; RenderTexture target=null; Texture2D readback=null; var previous=RenderTexture.active;
            try {
                foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
                    var materials=renderer.sharedMaterials; originals.Add(renderer,materials);
                    renderer.sharedMaterials=materials.Select(material=>{
                        if(material==null) return null;
                        if(!copies.TryGetValue(material,out var copy)) { copy=new Material(material); copies.Add(material,copy); } return copy;
                    }).ToArray();
                }
                var owner=Object.FindFirstObjectByType<CampaignController>(); var camera=owner.Camera; camera.aspect=width/(float)height;
                Assert.That(camera.orthographicSize,Is.EqualTo(Q1ProductionView.NormalSize));
                fixture=new GameObject("ACT 3 diagnostic memory render"); var memory=fixture.AddComponent<CampaignMemoryView>();
                memory.Refresh(Memories(),owner.Ordinary.transform.position); memory.SetPresentationFacing(camera.transform.rotation);
                var parts=new[]{fixture.transform.Find("MEM-01 small star"),memory.LoversSwing,memory.LoversEmptyPlace,memory.WindmillFragment,memory.WindmillWing};
                var colors=new[]{Color.yellow,Color.magenta,Color.cyan,Color.green,Color.red};
                for(int i=0;i<parts.Length;i++) parts[i].GetComponent<Renderer>().sharedMaterial.SetColor("_BaseColor",colors[i]);
                target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32); target.Create(); readback=new Texture2D(width,height,TextureFormat.RGB24,false);
                var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
                Assert.That(RenderPipeline.SupportsRenderRequest(camera,request),Is.True);
                Color32[] Render() {
                    RenderPipeline.SubmitRenderRequest(camera,request); RenderTexture.active=target;
                    readback.ReadPixels(new Rect(0,0,width,height),0,0); readback.Apply(); return readback.GetPixels32();
                }
                var visible=Render(); fixture.SetActive(false); var hidden=Render(); var counts=new int[5];
                for(int i=0;i<visible.Length;i++) {
                    var p=visible[i]; var b=hidden[i];
                    if(Math.Abs(p.r-b.r)+Math.Abs(p.g-b.g)+Math.Abs(p.b-b.b)<90) continue;
                    bool red=p.r>230,green=p.g>230,blue=p.b>230,lowRed=p.r<40,lowGreen=p.g<40,lowBlue=p.b<40;
                    if(red && green && lowBlue) counts[0]++;
                    if(red && blue && lowGreen) counts[1]++;
                    if(green && blue && lowRed) counts[2]++;
                    if(green && lowRed && lowBlue) counts[3]++;
                    if(red && lowGreen && lowBlue) counts[4]++;
                }
                TestContext.WriteLine("ACT3 diagnostic memory pixels "+width+"x"+height+": "+string.Join(",",counts));
                float area=width*height/(1280f*720); int[] minima={10,250,35,70,30};
                for(int i=0;i<parts.Length;i++) Assert.That(counts[i],Is.GreaterThan(minima[i]*area),parts[i].name+" must survive actual room depth.");
            } finally {
                RenderTexture.active=previous; if(target!=null) { target.Release(); Object.DestroyImmediate(target); }
                if(readback!=null) Object.DestroyImmediate(readback); if(fixture!=null) Object.DestroyImmediate(fixture);
                foreach(var pair in originals) if(pair.Key!=null) pair.Key.sharedMaterials=pair.Value;
                foreach(var copy in copies.Values) Object.DestroyImmediate(copy);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            }
        }
    }
}
