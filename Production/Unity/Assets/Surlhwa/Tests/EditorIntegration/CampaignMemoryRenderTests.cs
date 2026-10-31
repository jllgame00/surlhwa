using System;
using System.Collections.Generic;
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
    public sealed class CampaignMemoryRenderTests
    {
        // Isolated presentation receipt, not a forged campaign-save or normal-play claim.
        private static CampaignCheckpoint Earned() => new CampaignCheckpoint(1,"123456781234123412341234567890ab",0,
            CampaignPhase.Complete,0,null,null,new[]{"presentation_fixture"},Array.Empty<string>(),Array.Empty<string>(),
            Array.Empty<CommittedService>(),new[]{new MemoryReceipt("mem_02","swing_empty_place","123456781234123412341234567890ab:0",1)},
            new Dictionary<string,string>(),new Dictionary<string,string[]>());
        private static CampaignMemoryView CreateMemory()
        {
            var memory=new GameObject("Isolated earned MEM-02 render fixture").AddComponent<CampaignMemoryView>();
            memory.Refresh(Earned(),Vector3.zero); memory.SetPresentationFacing(Q1ProductionView.WorldAngle); return memory;
        }

        [Test] public void SwingAndAdjacentPlaceRestOnExistingWindowSideCounter()
        {
            EditorSceneManager.OpenScene(Surlhwa.Editor.CampaignExpansionBuilder.ScenePath);
            CampaignMemoryView memory=null;
            try {
                memory=CreateMemory(); Physics.SyncTransforms();
                var support=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Single(x=>x.name=="C_CounterReturnTop" && x.bounds.center.x>0).bounds;
                var window=GameObject.Find("C_WindowFrame").transform;
                Assert.That(Vector3.Distance(memory.LoversSwing.parent.position,window.position),Is.LessThan(1.5f),"The authored farewell locates the memory beside the window.");
                foreach(var part in new[]{memory.LoversSwing,memory.LoversEmptyPlace})
                    foreach(var point in part.GetComponent<MeshFilter>().sharedMesh.vertices.Where(v=>v.y<.0001f)) {
                        Vector3 world=part.TransformPoint(point);
                        Assert.That(world.y-support.max.y,Is.InRange(.001f,.01f),part.name+" base must meet the actual counter surface.");
                        Assert.That(support.Contains(new Vector3(world.x,support.center.y,world.z)),Is.True,part.name+" must be supported rather than floating beside the counter.");
                    }
            } finally {
                if(memory!=null) Object.DestroyImmediate(memory.gameObject);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            }
        }

        [TestCase(1280,720)] [TestCase(1920,1080)]
        public void BothMemoryPartsProduceVisiblePixelsThroughActualQ1Depth(int width,int height)
        {
            EditorSceneManager.OpenScene(Surlhwa.Editor.CampaignExpansionBuilder.ScenePath);
            var original=new Dictionary<Renderer,Material[]>(); var copies=new Dictionary<Material,Material>();
            CampaignMemoryView memory=null; RenderTexture target=null; Texture2D readback=null;
            RenderTexture previous=RenderTexture.active;
            try {
                // URP may validate materials while rendering. Never let that mutate accepted
                // assets: the fixture uses temporary copies and restores every reference.
                foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
                    var materials=renderer.sharedMaterials; original.Add(renderer,materials);
                    renderer.sharedMaterials=materials.Select(material=>{
                        if(material==null) return null;
                        if(!copies.TryGetValue(material,out var copy)) { copy=new Material(material); copies.Add(material,copy); }
                        return copy;
                    }).ToArray();
                }
                var camera=Object.FindFirstObjectByType<Q1ProductionView>().Owner.Camera;
                Assert.That(camera.orthographicSize,Is.EqualTo(Q1ProductionView.NormalSize));
                Assert.That(Quaternion.Angle(camera.transform.rotation,Q1ProductionView.WorldAngle),Is.LessThan(.001f));
                camera.aspect=width/(float)height; memory=CreateMemory();
                memory.LoversSwing.GetComponent<Renderer>().sharedMaterial.SetColor("_BaseColor",Color.magenta);
                memory.LoversEmptyPlace.GetComponent<Renderer>().sharedMaterial.SetColor("_BaseColor",Color.cyan);
                target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32); target.Create();
                readback=new Texture2D(width,height,TextureFormat.RGB24,false);
                var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
                Assert.That(RenderPipeline.SupportsRenderRequest(camera,request),Is.True,"The normal licensed Editor must support the project's pinned URP render request.");
                Color32[] Render()
                {
                    RenderPipeline.SubmitRenderRequest(camera,request);
                    RenderTexture.active=target; readback.ReadPixels(new Rect(0,0,width,height),0,0); readback.Apply();
                    return readback.GetPixels32();
                }
                var visible=Render();
                memory.LoversSwing.parent.gameObject.SetActive(false);
                var hidden=Render();
                int swing=0,place=0;
                for(int i=0;i<visible.Length;i++) {
                    var color=visible[i]; var background=hidden[i];
                    if(Math.Abs(color.r-background.r)+Math.Abs(color.g-background.g)+Math.Abs(color.b-background.b)<90) continue;
                    if(color.r>230 && color.b>230 && color.g<40) swing++;
                    if(color.g>230 && color.b>230 && color.r<40) place++;
                }
                float area=width*height/(1280f*720);
                TestContext.WriteLine("MEM02 diagnostic visible pixels at "+width+"x"+height+": swing="+swing+", adjacent place="+place);
                Assert.That(swing,Is.GreaterThan(250*area),"Active flags are insufficient: the swing frame/ropes must survive actual room depth, not hide behind the rear niche.");
                Assert.That(place,Is.GreaterThan(35*area),"The adjacent empty place must be separately visible in the actual Q1 composition.");
            } finally {
                RenderTexture.active=previous;
                if(target!=null) { target.Release(); Object.DestroyImmediate(target); }
                if(readback!=null) Object.DestroyImmediate(readback);
                if(memory!=null) Object.DestroyImmediate(memory.gameObject);
                foreach(var pair in original) if(pair.Key!=null) pair.Key.sharedMaterials=pair.Value;
                foreach(var copy in copies.Values) Object.DestroyImmediate(copy);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            }
        }
    }
}
