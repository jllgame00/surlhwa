using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Domain;
using Surlhwa.Presentation;
using Object=UnityEngine.Object;

namespace Surlhwa.SpatialTests
{
    public sealed class CampaignCastTests
    {
        private sealed class Store : ICampaignSaveStore
        {
            public Task SaveAsync(CampaignCheckpoint checkpoint) => Task.CompletedTask;
        }
        private static CampaignContent Content() => CampaignContentLoader.Load(File.ReadAllText("../Content/Campaign/act2.json"));
        private static CampaignCheckpoint State(CampaignContent content,CampaignPhase phase,IEnumerable<MemoryReceipt> memories=null) =>
            new CampaignCheckpoint(1,"123456781234123412341234567890ab",24,phase,0,null,null,
                content.Slots.Select(x=>x.EncounterId),Array.Empty<string>(),Array.Empty<string>(),Array.Empty<CommittedService>(),
                memories ?? Array.Empty<MemoryReceipt>(),content.KnownSeeds.ToDictionary(x=>x.Key,x=>x.Value),new Dictionary<string,string[]>());

        [Test] public void ExpandedSceneRetainsApprovedCameraAndPrinceWhileBindingTwoDistinctActors()
        {
            Assert.That(File.Exists(Surlhwa.Editor.CampaignExpansionBuilder.ScenePath),Is.True,"Prepare the separate candidate scene before running its integration tests.");
            EditorSceneManager.OpenScene(Surlhwa.Editor.CampaignExpansionBuilder.ScenePath);
            try {
                var c=Object.FindFirstObjectByType<CampaignController>(); var cast=c.GetComponent<CampaignCastView>();
                var view=Object.FindFirstObjectByType<Q1ProductionView>();
                Assert.That(c.Bundle.text,Is.EqualTo(File.ReadAllText("../Content/Campaign/act2.json")));
                Assert.That(c.LegacyBundle.text,Is.EqualTo(File.ReadAllText("../Content/Campaign/act1.json")));
                Assert.That(c.Chunhyang.GuestId,Is.EqualTo("guest_chunhyang"));
                Assert.That(c.Mongryong.GuestId,Is.EqualTo("guest_mongryong"));
                Assert.That(c.Chunhyang.SeatedMaterial,Is.Not.SameAs(c.Mongryong.SeatedMaterial));
                Assert.That(c.Chunhyang.SeatedMaterial,Is.Not.SameAs(c.Prince.SeatedMaterial));
                Assert.That(c.Mongryong.SeatedMaterial,Is.Not.SameAs(c.Prince.SeatedMaterial));
                Assert.That(view.PrinceSideQuarter,Is.SameAs(AssetDatabase.LoadAssetAtPath<Material>("Assets/Surlhwa/Q1Polish/PrinceSideQuarter.mat")));
                Assert.That(c.Camera.orthographicSize,Is.EqualTo(Q1ProductionView.NormalSize));
                cast.Initialize(c,c.Ordinary.transform.position);
                var encounter=Content().Encounters[Content().Slots[24].EncounterId];
                cast.Refresh(encounter,CampaignPhase.Intro,0,c.Ordinary.transform.position);
                Assert.That(cast.Paired,Is.True);
                Assert.That(Vector3.Distance(c.Chunhyang.SeatAnchor.position,c.Mongryong.SeatAnchor.position),Is.GreaterThan(1));
                foreach(bool focused in new[]{false,true,false}) {
                    var roots=new[]{c.Chunhyang.transform.position,c.Mongryong.transform.position};
                    var seats=new[]{c.Chunhyang.SeatAnchor.position,c.Mongryong.SeatAnchor.position};
                    c.Camera.transform.rotation=focused ? Q1ProductionView.FocusAngle : Q1ProductionView.WorldAngle;
                    cast.ApplyView(focused);
                    var actors=new[]{c.Chunhyang,c.Mongryong};
                    for(int i=0;i<actors.Length;i++) {
                        var actor=actors[i]; var hip=Quaternion.Inverse(Q1ProductionView.FocusAngle)*actor.SeatAnchor.localPosition;
                        Assert.That(actor.transform.position,Is.EqualTo(roots[i]));
                        Vector3 rendered=actor.Plane.transform.TransformPoint(hip),expected=seats[i]-(focused ? Vector3.zero : c.Camera.transform.forward*.14f);
                        Assert.That(actor.SeatAnchor.position,Is.EqualTo(seats[i]));
                        Assert.That(Vector3.Distance(rendered,expected),Is.LessThan(.0001f));
                        Assert.That(actor.Renderer.enabled,Is.True);
                    }
                }
                cast.Hide();
                Assert.That(c.Chunhyang.Renderer.enabled || c.Mongryong.Renderer.enabled,Is.False);
                Assert.That(c.Chunhyang.Contact.enabled || c.Mongryong.Contact.enabled,Is.False);
                cast.enabled=false;
                cast.Refresh(encounter,CampaignPhase.Intro,0,c.Ordinary.transform.position);
                Assert.That(c.Chunhyang.Renderer.enabled || c.Mongryong.Renderer.enabled,Is.False,"A cancelled presenter cannot reactivate its actors.");
            } finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); }
        }

        [Test] public void SourcePelvisPixelsMeetSeatsAndOpaqueSeatedPixelsRemainAboveTheFloor()
        {
            EditorSceneManager.OpenScene(Surlhwa.Editor.CampaignExpansionBuilder.ScenePath);
            try {
                var c=Object.FindFirstObjectByType<CampaignController>(); var cast=c.GetComponent<CampaignCastView>();
                cast.Initialize(c,c.Ordinary.transform.position);
                var content=Content(); cast.Refresh(content.Encounters[content.Slots[24].EncounterId],CampaignPhase.Intro,0,c.Ordinary.transform.position);
                foreach(var actor in new[]{c.Chunhyang,c.Mongryong}) {
                    string name=actor==c.Chunhyang ? "Chunhyang" : "Mongryong";
                    // Independent source-art review: the lower seated pelvis is at y29,
                    // not y45 at the waist. Read the actual PNG, including its alpha bounds.
                    Vector2 pelvis=new Vector2(actor==c.Chunhyang ? 44 : 47,29);
                    var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
                    try {
                        Assert.That(ImageConversion.LoadImage(source,File.ReadAllBytes(Surlhwa.Editor.CampaignExpansionBuilder.Root+"/"+name+"Seated.png")),Is.True);
                        Assert.That(source.GetPixel((int)pelvis.x,(int)pelvis.y).a,Is.GreaterThan(.4f));
                        var pixels=source.GetPixels32(); var bounds=actor.SeatedMesh.bounds;
                        var root=actor.transform.position; var seat=actor.SeatAnchor.position;
                        Vector3 FromPixel(float x,float y) => new Vector3(bounds.min.x+x/source.width*bounds.size.x,bounds.min.y+y/source.height*bounds.size.y,0);
                        foreach(bool focused in new[]{false,true,false}) {
                            c.Camera.transform.rotation=focused ? Q1ProductionView.FocusAngle : Q1ProductionView.WorldAngle;
                            cast.ApplyView(focused);
                            Vector3 depth=focused ? Vector3.zero : c.Camera.transform.forward*.14f;
                            var contact=actor.Plane.transform.TransformPoint(FromPixel(pelvis.x,pelvis.y))+depth;
                            Assert.That(Vector3.Distance(contact,seat),Is.LessThan(.0001f),name+" source pelvis must meet the physical seat after removing only the camera-ray thickness.");
                            float floor=float.PositiveInfinity;
                            for(int y=0;y<source.height;y++) for(int x=0;x<source.width;x++) if(pixels[y*source.width+x].a>102)
                                floor=Mathf.Min(floor,(actor.Plane.transform.TransformPoint(FromPixel(x+.5f,y+.5f))+depth).y);
                            Assert.That(floor,Is.GreaterThan(.01f),name+" opaque hem/feet cannot be buried in the floor; a depth-only offset must not conceal that defect.");
                            Assert.That(actor.transform.position,Is.EqualTo(root)); Assert.That(actor.SeatAnchor.position,Is.EqualTo(seat));
                        }
                        actor.Show(true,false); cast.ApplyView(false);
                        Assert.That(actor.Plane.transform.localPosition,Is.EqualTo(Vector3.zero),"The seated cutout offset must not carry into standing or departure.");
                    } finally { Object.DestroyImmediate(source); }
                }
            } finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); }
        }

        [Test] public void MissingPairedActorAndMissingWalkFramesCannotBlockDepartureOrInventASecondMemory()
        {
            EditorSceneManager.OpenScene(Surlhwa.Editor.CampaignExpansionBuilder.ScenePath);
            try {
                var c=Object.FindFirstObjectByType<CampaignController>(); var cast=c.GetComponent<CampaignCastView>();
                cast.Initialize(c,c.Ordinary.transform.position); c.Mongryong=null;
                c.Chunhyang.WalkMeshes=Array.Empty<Mesh>(); c.Chunhyang.WalkMaterials=Array.Empty<Material>();
                var content=Content(); var session=CampaignSession.Continue(content,new Store(),State(content,CampaignPhase.Departing));
                cast.Refresh(session.CurrentEncounter,session.State.Phase,1.49f,c.Ordinary.transform.position);
                Assert.That(c.Chunhyang.Renderer.enabled,Is.True,"A static fallback still follows the exit path.");
                Assert.That(c.Chunhyang.transform.position.x,Is.LessThan(-4));
                session.TickAsync(1.5).GetAwaiter().GetResult();
                cast.Refresh(session.CurrentEncounter,session.State.Phase,1.5f,c.Ordinary.transform.position);
                Assert.That(session.State.Phase,Is.EqualTo(CampaignPhase.Complete));
                Assert.That(session.State.Memories.Count(x=>x.Id=="mem_02"),Is.EqualTo(1));
                Assert.That(c.Chunhyang.Renderer.enabled,Is.False);
                session.TickAsync(1.5).GetAwaiter().GetResult();
                Assert.That(session.State.Memories.Count(x=>x.Id=="mem_02"),Is.EqualTo(1));
            } finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); }
        }

        [Test] public void LoversMemoryIsOneReceiptDrivenSwingWithAnEmptyAdjacentPlace()
        {
            var go=new GameObject("ACT 2 memory receipt fixture");
            try {
                var content=Content(); var memory=go.AddComponent<CampaignMemoryView>();
                memory.Refresh(State(content,CampaignPhase.Departing),Vector3.zero);
                Assert.That(memory.LoversVisible,Is.False); Assert.That(memory.Visible,Is.False);
                var receipt=new MemoryReceipt("mem_02","swing_empty_place","123456781234123412341234567890ab:24",1);
                var earned=State(content,CampaignPhase.Complete,new[]{receipt});
                memory.Refresh(earned,Vector3.zero); memory.SetPresentationFacing(Q1ProductionView.WorldAngle);
                Assert.That(memory.LoversVisible,Is.True); Assert.That(memory.Visible,Is.False,"MEM-02 never fabricates the Prince's star.");
                Assert.That(memory.LoversSwing,Is.Not.Null); Assert.That(memory.LoversEmptyPlace,Is.Not.Null);
                Assert.That(memory.LoversEmptyPlace.childCount,Is.Zero,"The place is intentionally unoccupied.");
                Assert.That(Vector3.Distance(memory.LoversSwing.position,memory.LoversEmptyPlace.position),Is.GreaterThan(.2f));
                memory.Refresh(earned,Vector3.zero);
                Assert.That(go.GetComponentsInChildren<Transform>(true).Count(x=>x.name=="MEM-02 swing and empty place"),Is.EqualTo(1));
                memory.Refresh(null,Vector3.zero); Assert.That(memory.LoversVisible,Is.False);
            } finally { Object.DestroyImmediate(go); }
        }

        [Test] public void CandidatePreparationCannotReplaceAnUnsavedScene()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var sentinel=new GameObject("Unrelated unsaved owner content"); EditorSceneManager.MarkSceneDirty(scene);
            try {
                Assert.That(()=>Surlhwa.Editor.CampaignExpansionBuilder.PrepareScene(),Throws.InvalidOperationException);
                Assert.That(sentinel!=null && sentinel.scene==UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Is.True);
            } finally { Object.DestroyImmediate(sentinel); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); }
        }
    }
}
