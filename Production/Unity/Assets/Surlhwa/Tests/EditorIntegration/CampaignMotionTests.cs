using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Domain;
using Surlhwa.Presentation;
using Object=UnityEngine.Object;

namespace Surlhwa.SpatialTests
{
    // Real scene/pose fixtures. Reflection drives the existing presentation clock,
    // not a replacement domain clock or a future API that cannot compile for RED.
    public sealed class CampaignMotionTests
    {
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private const string Run="123456781234123412341234567890ab";
        private sealed class Store : ICampaignSaveStore
        {
            public int Writes;
            public Task SaveAsync(CampaignCheckpoint checkpoint) { Writes++; return Task.CompletedTask; }
        }
        private CampaignController owner;
        private CampaignContent content;
        private Q1ProductionView view;
        private CampaignCharacterArt art;
        private Store store;
        private Vector3 seat;

        [SetUp] public void Setup()
        {
            EditorSceneManager.OpenScene("Assets/Surlhwa/Scenes/Bar_Q1_Act7.unity");
            owner=Object.FindFirstObjectByType<CampaignController>();
            content=CampaignContentLoader.Load(File.ReadAllText("../Content/Campaign/act7.json"));
            view=Object.FindFirstObjectByType<Q1ProductionView>();
            seat=owner.Ordinary.transform.position;
            Set("Content",content); Field("seatPosition",seat);
            var audio=owner.gameObject.AddComponent<CampaignAudio>(); audio.Initialize(owner); Set("Audio",audio);
            var work=new GameObject("Motion fixture workbench").AddComponent<SliceWorkbenchView>();
            work.transform.position=new Vector3(.80f,1.055f,-.65f); work.Initialize(owner.Grip); work.ApplyArt(owner.Art);
            work.Refresh(null,null,0); Set("Workbench",work);
            var cast=owner.GetComponent<CampaignCastView>(); cast.Initialize(owner,seat); Set("Cast",cast);
            art=owner.gameObject.AddComponent<CampaignCharacterArt>(); art.Initialize(owner);
            foreach(var actor in Guests()) actor.Show(false,true);
            owner.Bartender.Show(true,false);
            At(0,CampaignPhase.Arriving);
        }

        [TearDown] public void Teardown()
        {
            owner.Session?.Dispose();
            if(owner.Workbench!=null) {
                var work=owner.Workbench.gameObject; Set("Workbench",null); Object.DestroyImmediate(work);
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }

        private void Set(string name,object value)=>typeof(CampaignController).GetProperty(name).SetValue(owner,value);
        private void Field(string name,object value)=>typeof(CampaignController).GetField(name,Hidden).SetValue(owner,value);
        private T Field<T>(string name)=>(T)typeof(CampaignController).GetField(name,Hidden).GetValue(owner);
        private static void Invoke(object target,string name)=>target.GetType().GetMethod(name,Hidden).Invoke(target,null);
        private CharacterView[] Guests()=>new[]{owner.Ordinary,owner.Prince,owner.Chunhyang,owner.Mongryong,owner.Quixote,owner.Hamlet,owner.Faust,owner.SimCheong,owner.Odysseus}.Where(x=>x!=null).ToArray();
        private int Feet=>(int)typeof(CampaignAudio).GetField("footIndex",Hidden).GetValue(owner.Audio);
        private void At(int slot,CampaignPhase phase,int line=0,bool reserved=false,IEnumerable<CommittedService> services=null)
        {
            owner.Session?.Dispose(); store=new Store();
            var memories=reserved ? new[]{new MemoryReceipt("mem_01","star_glowing",Run+":16",1)} : Array.Empty<MemoryReceipt>();
            var state=new CampaignCheckpoint(1,Run,slot,phase,line,"keep",PrinceFarewell.Open,
                content.Slots.Take(slot+1).Select(x=>x.EncounterId),Array.Empty<string>(),Array.Empty<string>(),
                services ?? Array.Empty<CommittedService>(),memories,
                content.KnownSeeds.ToDictionary(x=>x.Key,x=>x.Value),new Dictionary<string,string[]>());
            Set("Session",CampaignSession.Continue(content,store,state));
            Field("lastPhase",(CampaignPhase)(-1)); Field("lastEncounter",-1); Field("poseClock",0f);
            // Bind/reset the presenter while paused, before sampling an exact
            // elapsed time. This emits no planted contact and writes no state.
            owner.Session.SetFocus(false); Invoke(owner,"AnimateGuest"); owner.Session.SetFocus(true);
        }
        private void Pose(float elapsed)
        {
            // Bypass only the controller's automatic phase-reset/increment so this
            // fixture can inspect exact points within the existing 1.5 s boundary.
            Field("lastPhase",owner.Session.State.Phase); Field("lastEncounter",owner.Session.State.SlotIndex);
            Field("poseClock",elapsed-(owner.Session.CanInteract ? Time.unscaledDeltaTime : 0));
            Invoke(owner,"AnimateGuest"); view.ApplyActors();
        }
        private void ArtAt(float elapsed)
        {
            typeof(CampaignCharacterArt).GetField("clock",Hidden).SetValue(art,
                elapsed-(owner.Session.CanInteract ? Mathf.Min(Time.unscaledDeltaTime,.1f) : 0));
            Invoke(art,"LateUpdate"); view.ApplyActors();
        }
        private Transform CleaningGlass()
        {
            var prop=art.GetComponentsInChildren<Transform>(true).SingleOrDefault(x=>x.name=="Ending empty cleaning glass");
            Assert.That(prop,Is.Not.Null,"Line text_rc1_0007 needs a small empty cleanup-glass presentation owned by the existing character presenter.");
            return prop;
        }
        private void AssertEndingTruth(CampaignCheckpoint state)
        {
            Assert.That(owner.Session.State,Is.SameAs(state),"Presentation cannot publish/consume a reading line.");
            Assert.That(owner.Workbench.ServingFillMl,Is.Zero,"Cleanup glass is not the old service or another drink.");
            Assert.That(Guests().Any(x=>x.Renderer.enabled || x.Contact.enabled),Is.False);
            Assert.That(store.Writes,Is.Zero);
        }

        [Test] public void OrdinaryArrivalHasAGroundedStationaryTurnBeforeTheExactSeatedContact()
        {
            var actor=owner.Ordinary; var checkpoint=owner.Session.State;
            bool moved=false,standingAtSeat=false,seated=false; var previous=Vector3.zero;
            for(int i=0;i<=30;i++) {
                float time=i*.05f; Pose(time);
                Assert.That(actor.Renderer.enabled,Is.True);
                Assert.That(actor.transform.position.y,Is.EqualTo(seat.y).Within(.0001f));
                Assert.That(actor.FeetAnchor.position.y,Is.EqualTo(seat.y).Within(.0001f));
                Assert.That(actor.transform.localScale.x,Is.GreaterThan(0),"Facing must not reflect the physical root/anchors.");
                if(i>0 && Vector3.Distance(previous,actor.transform.position)>.001f) moved=true;
                if(moved && Vector3.Distance(actor.transform.position,seat)<.002f && actor.Plane.sharedMesh==actor.StandingMesh) standingAtSeat=true;
                if(actor.Plane.sharedMesh==actor.SeatedMesh) seated=true;
                previous=actor.transform.position;
            }
            Assert.That(standingAtSeat,Is.True,"Approach must finish with a short stationary turn/settle; walking cannot switch straight to seated.");
            Assert.That(seated,Is.True); Assert.That(actor.transform.position,Is.EqualTo(seat));
            var hip=Quaternion.Inverse(Q1ProductionView.FocusAngle)*actor.SeatAnchor.localPosition;
            var expected=actor.SeatAnchor.position-owner.Camera.transform.forward*.14f;
            Assert.That(Vector3.Distance(actor.Plane.transform.TransformPoint(hip),expected),Is.LessThan(.001f));
            Assert.That(owner.Session.State,Is.SameAs(checkpoint)); Assert.That(store.Writes,Is.Zero);
            Assert.That(owner.Session.TickAsync(1.49).GetAwaiter().GetResult(),Is.False);
            Assert.That(owner.Session.TickAsync(.01).GetAwaiter().GetResult(),Is.True);
            Assert.That(owner.Session.State.Phase,Is.EqualTo(CampaignPhase.Intro));
        }

        [Test] public void DepartureStandsBeforeTravelAndLeavesTheActualQ1FrameWithinTheLogicalBoundary()
        {
            At(0,CampaignPhase.Departing); var actor=owner.Ordinary;
            Pose(.04f); Assert.That(actor.Plane.sharedMesh,Is.SameAs(actor.SeatedMesh));
            Pose(.12f); Assert.That(actor.transform.position,Is.EqualTo(seat)); Assert.That(actor.Plane.sharedMesh,Is.SameAs(actor.StandingMesh));
            Assert.That(Feet,Is.Zero,"The seated/standing turn interval is stationary.");
            Pose(.60f); Assert.That(Vector3.Distance(actor.transform.position,seat),Is.GreaterThan(.1f));
            Pose(1.499f);
            Assert.That(actor.Renderer.enabled,Is.True,"Do not fake an exit by hiding a still-onscreen actor.");
            var projected=actor.Plane.sharedMesh.vertices.Select(x=>owner.Camera.WorldToViewportPoint(actor.Plane.transform.TransformPoint(x))).ToArray();
            bool outside=projected.All(x=>x.x<0) || projected.All(x=>x.x>1) || projected.All(x=>x.y<0) || projected.All(x=>x.y>1);
            Assert.That(outside,Is.True,"The existing entry endpoint remains visible in Q1; finish the presentation exit before logical departure.");
            Assert.That(owner.Session.State.Phase,Is.EqualTo(CampaignPhase.Departing)); Assert.That(store.Writes,Is.Zero);
        }

        [Test] public void MissingWalkFramesKeepGroundedTravelButCannotInventPlantedContacts()
        {
            var actor=owner.Ordinary; actor.WalkMeshes=Array.Empty<Mesh>(); actor.WalkMaterials=Array.Empty<Material>();
            Pose(.20f); var before=actor.transform.position; Pose(.70f);
            Assert.That(actor.Renderer.enabled,Is.True); Assert.That(actor.Plane.sharedMesh,Is.SameAs(actor.StandingMesh));
            Assert.That(Vector3.Distance(before,actor.transform.position),Is.GreaterThan(.01f));
            Assert.That(actor.FeetAnchor.position.y,Is.EqualTo(seat.y).Within(.0001f));
            Assert.That(Feet,Is.Zero,"A static fallback has no authored planted walk-frame contact.");
        }

        [Test] public void AFrameJumpDoesNotInventASkippedContactAndPauseCannotReplayTheSameFoot()
        {
            // Source four-frame sequences use planted frames 0/2 and passing frames 1/3.
            Pose(.01f); int initial=Feet;
            Pose(.90f);
            Assert.That(Feet,Is.EqualTo(initial),"A hitch landing on passing frame 1 must not replay the skipped planted frame.");
            Pose(1.02f); Assert.That(Feet-initial,Is.InRange(0,1),"Observe at most the current contact, not a backlog.");
            var position=owner.Ordinary.transform.position; var mesh=owner.Ordinary.Plane.sharedMesh; int before=Feet;
            owner.Session.SetFocus(false); float clock=Field<float>("poseClock");
            for(int i=0;i<5;i++) { Invoke(owner,"AnimateGuest"); view.ApplyActors(); }
            Assert.That(Field<float>("poseClock"),Is.EqualTo(clock)); Assert.That(Feet,Is.EqualTo(before));
            Assert.That(owner.Ordinary.transform.position,Is.EqualTo(position)); Assert.That(owner.Ordinary.Plane.sharedMesh,Is.SameAs(mesh));
            owner.Session.SetFocus(true); Pose(1.02f); Assert.That(Feet,Is.EqualTo(before));
            owner.Session.OpenModal("codex"); Invoke(owner,"AnimateGuest"); Assert.That(Feet,Is.EqualTo(before));
        }

        [Test] public void MissingPoseBindingHidesThePreviouslyVisibleActorWithoutTouchingProgress()
        {
            Pose(.4f); var actor=owner.Ordinary; var state=owner.Session.State;
            actor.StandingMesh=null; Pose(.5f);
            Assert.That(actor.Renderer.enabled || actor.Contact.enabled,Is.False,"A missing source cannot leave the previous walk surface stranded.");
            Assert.That(owner.Session.State,Is.SameAs(state)); Assert.That(store.Writes,Is.Zero);
        }

        [Test] public void BothPairedActorsRemainSilentBeforeTheirIndependentDeparturePauses()
        {
            At(24,CampaignPhase.Departing); var cast=owner.Cast;
            foreach(float t in new[]{0f,.04f,.08f,.11f}) cast.Refresh(owner.Session.CurrentEncounter,CampaignPhase.Departing,t,seat);
            Assert.That(Feet,Is.Zero,"No visible actor has started moving before the .12/.18 second pauses.");
            Assert.That(owner.Chunhyang.transform.position,Is.EqualTo(cast.ChunhyangSeat.position));
            Assert.That(owner.Mongryong.transform.position,Is.EqualTo(cast.MongryongSeat.position));
        }

        [TestCase(false)][TestCase(true)] public void OnlyTheSurvivingMovingPairedActorCanEmitAContact(bool removeFirst)
        {
            At(24,CampaignPhase.Departing);
            if(removeFirst) owner.Chunhyang=null; else owner.Mongryong=null;
            owner.Cast.Refresh(owner.Session.CurrentEncounter,CampaignPhase.Departing,.15f,seat);
            Assert.That(Feet,Is.EqualTo(removeFirst ? 1 : 0),"At .15 only Mongryong has passed his own .12 pause; Chunhyang still waits until .18.");
            int count=Feet; owner.Cast.Refresh(owner.Session.CurrentEncounter,CampaignPhase.Departing,.15f,seat);
            Assert.That(Feet,Is.EqualTo(count),"Repeated refresh observes no new foot contact.");
        }

        [TestCase(false)][TestCase(true)] public void LosingEitherPairedBindingCleansItsOldSurfaceAndPreservesOneLogicalDeparture(bool removeFirst)
        {
            var brew=new BrewingSession(content.Catalog,content.Tuning,content.AvailableSupplyAt(24)); brew.Pour("gin",ContainerRole.Serving,45m);
            var snapshot=brew.Freeze(); var encounter=content.Encounters[content.Slots[24].EncounterId];
            var service=new CommittedService(Run+":24",snapshot,ServiceEvaluator.Evaluate(content.Catalog,content.Tuning,snapshot,encounter.Request),"unknown",false);
            At(24,CampaignPhase.Departing,services:new[]{service});
            owner.Cast.Refresh(owner.Session.CurrentEncounter,CampaignPhase.Intro,0,seat);
            var removed=removeFirst ? owner.Chunhyang : owner.Mongryong;
            var survivor=removeFirst ? owner.Mongryong : owner.Chunhyang;
            if(removeFirst) owner.Chunhyang=null; else owner.Mongryong=null;
            owner.Cast.Refresh(owner.Session.CurrentEncounter,CampaignPhase.Departing,.5f,seat);
            Assert.That(removed.Renderer.enabled || removed.Contact.enabled,Is.False,"A lost binding during a live pair must clear its formerly bound presenter.");
            Assert.That(survivor.Renderer.enabled,Is.True);
            Assert.That(owner.Workbench.GetComponentsInChildren<Transform>(true).Count(x=>x.name=="Serving glass 180 ml"),Is.EqualTo(1));
            Assert.That(owner.Session.State.Services.Single(),Is.SameAs(service)); Assert.That(store.Writes,Is.Zero);
            owner.Session.TickAsync(1.5).GetAwaiter().GetResult();
            owner.Cast.Refresh(owner.Session.CurrentEncounter,owner.Session.State.Phase,0,seat);
            Assert.That(survivor.Renderer.enabled || survivor.Contact.enabled,Is.False);
            Assert.That(owner.Session.State.CompletedOccurrences.Count(x=>x==Run+":24"),Is.EqualTo(1));
            Assert.That(owner.Session.State.Memories.Count(x=>x.Id=="mem_02"),Is.EqualTo(1));
            Assert.That(owner.Session.State.Services.Count,Is.EqualTo(1));
            owner.Cast.Hide(); owner.Cast.Hide(); Assert.That(owner.Cast.Paired,Is.False);
        }

        [Test] public void HamletListeningAndTheApprovedPrinceSeatStayStillAcrossQ1Composition()
        {
            At(30,CampaignPhase.Intro, reserved:true); Pose(0); var hamlet=owner.Hamlet;
            var root=hamlet.transform.position; var plane=hamlet.Plane.transform.localPosition; var rotation=hamlet.Plane.transform.localRotation;
            foreach(float clock in new[]{.2f,1f,4f,12f}) { Pose(clock); Assert.That(hamlet.transform.position,Is.EqualTo(root)); Assert.That(hamlet.Plane.transform.localPosition,Is.EqualTo(plane)); Assert.That(hamlet.Plane.transform.localRotation,Is.EqualTo(rotation)); }
            Assert.That(Feet,Is.Zero);
            int princeSlot=Enumerable.Range(0,content.Slots.Count).First(i=>content.Encounters[content.Slots[i].EncounterId].GuestId=="guest_little_prince");
            At(princeSlot,CampaignPhase.Intro); Pose(.5f);
            var prince=owner.Prince; var accepted=view.PrinceSideQuarter; var anchor=prince.SeatAnchor.position;
            foreach(bool focused in new[]{true,false,true,false}) {
                view.ApplyView(focused); view.ApplyActors();
                Assert.That(prince.SeatAnchor.position,Is.EqualTo(anchor)); Assert.That(prince.Plane.sharedMesh,Is.SameAs(prince.SeatedMesh));
                Assert.That(prince.Renderer.sharedMaterial,Is.SameAs(focused ? prince.SeatedMaterial : accepted));
                Assert.That(prince.Plane.transform.localScale,Is.EqualTo(Vector3.one*(focused ? 1 : 1.12f)));
            }
        }

        [TestCase(1)][TestCase(6)]
        public void LegacyCompletionAndContinueDoNotCreateTheActSevenCleanupProp(int act)
        {
            // Use actual retained content, not an ACT7 object with Ending nulled.
            // As elsewhere in this fixture, preceding history is synthetic;
            // the final departure and presentation callbacks are the real owners.
            content=CampaignContentLoader.Load(File.ReadAllText("../Content/Campaign/act"+act+".json"));
            Assert.That(content.Ending,Is.Null); Set("Content",content);
            Assert.That(owner.Art.Rocks,Is.Not.Null,"The retained art must expose the accidental cleanup-prop path.");
            Assert.That(owner.Art.BartenderIdle[1],Is.Not.Null);
            At(content.Slots.Count-1,CampaignPhase.Departing,reserved:act>1); Pose(0); ArtAt(.3f);
            Assert.That(owner.Session.TickAsync(2).GetAwaiter().GetResult(),Is.True);
            Assert.That(owner.Session.State.Phase,Is.EqualTo(CampaignPhase.Complete));
            Assert.That(owner.Session.CurrentText,Is.Null,"Legacy completion has no common-ending coda.");
            var complete=owner.Session.State; store.Writes=0;
            void AssertLegacyPresentation()
            {
                Pose(0);
                foreach(float time in new[]{.3f,1f,4f}) {
                    ArtAt(time);
                    Assert.That(art.GetComponentsInChildren<Transform>(true).Any(x=>x.name=="Ending empty cleaning glass"),Is.False,
                        "An ACT"+act+" Complete checkpoint must not create the ACT7-only cleanup glass.");
                    Assert.That(owner.Bartender.Renderer.sharedMaterial,Is.Not.SameAs(owner.Art.BartenderIdle[1]),"Legacy completion must not borrow the ending wipe pose.");
                    Assert.That(owner.Session.State,Is.SameAs(complete)); Assert.That(store.Writes,Is.Zero);
                }
            }
            AssertLegacyPresentation();
            owner.Session.Dispose(); Set("Session",CampaignSession.Continue(content,store,complete));
            AssertLegacyPresentation();
        }

        [Test] public void LiveEndingLine0007WipesAnEmptyGlassOnceAndPausesWithoutPublishingProgress()
        {
            At(46,CampaignPhase.EndingReading,6,true); Pose(0); ArtAt(0);
            Assert.That(owner.Session.CurrentText.Id,Is.EqualTo("text_rc1_0006"));
            owner.Session.AdvanceAsync(1).GetAwaiter().GetResult(); store.Writes=0; var state=owner.Session.State;
            Assert.That(owner.Session.CurrentText.Id,Is.EqualTo("text_rc1_0007"));
            ArtAt(.01f); var glass=CleaningGlass(); var start=glass.position;
            Assert.That(glass.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),Is.True);
            ArtAt(.30f); Assert.That(Vector3.Distance(glass.position,start),Is.GreaterThan(.0005f),"The cleanup prop must actually wipe, not only select an idle texture.");
            Assert.That(owner.Bartender.Renderer.sharedMaterial,Is.SameAs(owner.Art.BartenderIdle[1]));
            var paused=glass.position; owner.Session.OpenModal("settings");
            for(int i=0;i<4;i++) Invoke(art,"LateUpdate");
            Assert.That(glass.position,Is.EqualTo(paused)); AssertEndingTruth(state);
            owner.Session.CloseModal(); ArtAt(2.5f); var putAway=glass.position;
            foreach(float time in new[]{3f,4f,9f}) { ArtAt(time); Assert.That(glass.position,Is.EqualTo(putAway),"Repeated line refresh cannot restart a completed wipe."); }
            AssertEndingTruth(state);
        }

        [Test] public void RapidEndingAdvanceAndCompletedRebindSettleTheEmptyPropWithoutReplayingTheGesture()
        {
            At(46,CampaignPhase.EndingReading,6,true); Pose(0); ArtAt(0);
            owner.Session.AdvanceAsync(1).GetAwaiter().GetResult(); ArtAt(.02f); var glass=CleaningGlass();
            Assert.That(owner.Session.AdvanceAsync(2).GetAwaiter().GetResult(),Is.True); ArtAt(.05f);
            Assert.That(owner.Session.CurrentText.Id,Is.EqualTo("text_rc1_0008"));
            Assert.That(owner.Session.AdvanceAsync(3).GetAwaiter().GetResult(),Is.True); store.Writes=0; var complete=owner.Session.State;
            ArtAt(.5f); var stored=glass.position;
            ArtAt(2f); Assert.That(glass.position,Is.EqualTo(stored)); AssertEndingTruth(complete);
            At(46,CampaignPhase.Complete,0,true); art.Initialize(owner); Pose(0); ArtAt(0); var rebound=CleaningGlass();
            var stable=rebound.position; var material=owner.Bartender.Renderer.sharedMaterial;
            foreach(float time in new[]{.3f,1f,4f}) { ArtAt(time); Assert.That(rebound.position,Is.EqualTo(stable)); Assert.That(owner.Bartender.Renderer.sharedMaterial,Is.SameAs(material)); }
            Assert.That(rebound.position,Is.EqualTo(stored)); AssertEndingTruth(owner.Session.State);
            art.enabled=false;
            Assert.That(!rebound.gameObject.activeInHierarchy || rebound.position==stored,Is.True,"Disable must not leave an empty prop hanging in the wiping position.");
            owner.Art=null; art.Initialize(owner); Assert.DoesNotThrow(()=>Invoke(art,"LateUpdate"));
        }
    }
}
