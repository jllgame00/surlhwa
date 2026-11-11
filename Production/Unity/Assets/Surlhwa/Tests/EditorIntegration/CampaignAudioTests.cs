using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Domain;
using Surlhwa.Presentation;
using Object=UnityEngine.Object;

namespace Surlhwa.SpatialTests
{
    // Audio projection fixtures are not constructed save-history or native-input evidence.
    // Discrete/motion inputs use real sessions. Ending publication uses the real session
    // transaction over an explicitly synthetic presentation checkpoint and memory store.
    internal sealed class CampaignAudioFixture : IDisposable
    {
        internal sealed class Store : ICampaignSaveStore
        {
            internal bool Fail;
            internal int Publications;
            public Task SaveAsync(CampaignCheckpoint state)
            {
                if(Fail) throw new IOException("injected audio publication boundary");
                Publications++; return Task.CompletedTask;
            }
        }
        internal readonly InputTestFixture Devices=new InputTestFixture();
        internal readonly GameObject Host;
        internal readonly CampaignController Owner;
        internal readonly CampaignContent Content;
        internal readonly CampaignAudio Audio;
        internal readonly SliceInputAdapter Input;
        internal readonly Keyboard Keyboard;
        internal readonly Mouse Mouse;
        internal readonly Store Saves=new Store();
        private readonly List<AudioClip> clips=new List<AudioClip>();
        internal CampaignSession Session=>Owner.Session;
        internal CampaignAudioFixture()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Devices.Setup(); Keyboard=InputSystem.AddDevice<Keyboard>(); Mouse=InputSystem.AddDevice<Mouse>();
            Host=new GameObject("Campaign audio fixture"); Host.AddComponent<AudioListener>();
            Owner=Host.AddComponent<CampaignController>(); Owner.enabled=false;
            Content=CampaignContentLoader.Load(File.ReadAllText("../Content/Campaign/act7.json"));
            Set("Content",Content); Input=Host.AddComponent<SliceInputAdapter>(); Set("Input",Input);
            Owner.Room=Tone("room"); Owner.Winter=Tone("winter"); Owner.Pour=Tone("pour");
            Owner.Stir=Tone("stir"); Owner.Strain=Tone("strain"); Owner.Ice=Tone("ice");
            Owner.Glass=Tone("glass"); Owner.Seat=Tone("seat"); Owner.Entrance=Tone("entrance");
            Owner.Foot=Tone("foot"); Owner.Music=Tone("music"); Owner.Shake=Tone("shake");
            var session=CampaignSession.StartAsync(Content,Saves).GetAwaiter().GetResult(); long id=1;
            while(session.State.Phase!=CampaignPhase.Brewing)
                if(session.State.Phase==CampaignPhase.Arriving) session.TickAsync(2).GetAwaiter().GetResult();
                else session.AdvanceAsync(id++).GetAwaiter().GetResult();
            Bind(session); Audio=Host.AddComponent<CampaignAudio>(); Audio.Initialize(Owner); Set("Audio",Audio);
        }
        private AudioClip Tone(string name)
        {
            var clip=AudioClip.Create("audio test "+name,96000,1,48000,false);
            var samples=new float[96000];
            for(int i=0;i<samples.Length;i++) samples[i]=Mathf.Sin(i*2*Mathf.PI*220/48000)*.01f;
            Assert.That(clip.SetData(samples,0),Is.True); clips.Add(clip); return clip;
        }
        internal void Set(string property,object value)=>typeof(CampaignController).GetProperty(property).SetValue(Owner,value);
        internal void AudioLifecycle(string callback)=>typeof(CampaignAudio).GetMethod(callback,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Audio,null);
        internal AudioSource Source(string name)=>(AudioSource)typeof(CampaignAudio).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(Audio);
        internal void Bind(CampaignSession session)
        {
            Input.Stop(); Owner.Session?.Dispose(); Set("Session",session); Input.Bind(session,null,Vector3.zero);
        }
        internal void At(CampaignPhase phase,int line=0,string run=null)
        {
            if(phase==CampaignPhase.Reaction) {
                At(CampaignPhase.Brewing,0,run);
                Assert.That(Session.ExecuteBrew(Input.NextEventId(),b=>b.Pour("water",ContainerRole.Serving,1)),Is.True);
                Assert.That(Session.AdvanceAsync(Input.NextEventId()).GetAwaiter().GetResult(),Is.True); return;
            }
            int slot=phase==CampaignPhase.Retry ? Content.Slots.ToList().FindIndex(x=>Content.Encounters[x.EncounterId].DiscoveryRetryTextId!=null) :
                phase==CampaignPhase.EndingReady || phase==CampaignPhase.EndingEntered || phase==CampaignPhase.EndingReading || phase==CampaignPhase.Complete ? 46 : 0;
            Assert.That(slot,Is.GreaterThanOrEqualTo(0)); run=run ?? Guid.NewGuid().ToString("N");
            var state=new CampaignCheckpoint(200,run,slot,phase,line,"keep",PrinceFarewell.Open,
                Content.Slots.Take(slot+1).Select(x=>x.EncounterId),Array.Empty<string>(),Array.Empty<string>(),Array.Empty<CommittedService>(),
                new[]{new MemoryReceipt("mem_01","star_glowing",run+":16",100)},
                Content.KnownSeeds.ToDictionary(x=>x.Key,x=>x.Value),new Dictionary<string,string[]>());
            Bind(CampaignSession.Continue(Content,Saves,state));
        }
        internal void KeyDown(Key key) { InputSystem.QueueStateEvent(Keyboard,new KeyboardState(key)); InputSystem.Update(); }
        internal void KeyUp() { InputSystem.QueueStateEvent(Keyboard,new KeyboardState()); InputSystem.Update(); }
        internal void Move(float x,float y=.5f) { InputSystem.QueueStateEvent(Mouse,new MouseState{position=new Vector2(Screen.width*x,Screen.height*y)}); InputSystem.Update(); }
        internal void ShakeReversal()
        {
            Assert.That(Session.ExecuteBrew(Input.NextEventId(),b=>{b.Pour("gin",ContainerRole.Shaker,20); b.AddIce(ContainerRole.Shaker); b.SetShakerClosed(true);}),Is.True);
            Move(.4f); KeyDown(Key.K); Move(.6f); Input.Step(.25);
            Assert.That(Session.Brew.ShakeReversals,Is.EqualTo(1),"The fixture must accept a real pointer reversal.");
        }
        internal void Refresh()=>Audio.Refresh(Input,Session?.CanInteract==true);
        public void Dispose()
        {
            Object.DestroyImmediate(Host); foreach(var clip in clips) Object.DestroyImmediate(clip);
            Devices.TearDown(); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }
    }

    public sealed class CampaignMenuAudioTests
    {
        [UnityTest] public IEnumerator ReturnedMenuAppliesMasterMuteAndRestoreWithoutReplayingAudio()=>MenuMaster(true);
        [UnityTest] public IEnumerator InitialMenuProjectsConfiguredMasterBeforeStartingAStory()=>MenuMaster(false);

        private static IEnumerator MenuMaster(bool returnFromStory)
        {
            using(var view=new P09UiFixture())
            {
                var owner=view.Owner;
                string storage=Path.GetFullPath("../Validation/Evidence/audio-menu-fixture-"+Guid.NewGuid().ToString("N"));
                view.Set("StorageRoot",storage);
                typeof(CampaignController).GetField("store",P09UiFixture.Hidden).SetValue(owner,new CampaignSaveStore(Path.Combine(storage,"slot1"),view.Content));
                view.Set("Workbench",view.Root.AddComponent<SliceWorkbenchView>());
                view.Set("Memory",view.Root.AddComponent<CampaignMemoryView>());
                view.Root.AddComponent<AudioListener>();
                var tone=AudioClip.Create("menu master positive control",96000,1,48000,false);
                try {
                    var samples=new float[96000];
                    for(int i=0;i<samples.Length;i++) samples[i]=Mathf.Sin(i*2*Mathf.PI*220/48000)*.01f;
                    Assert.That(tone.SetData(samples,0),Is.True);
                    owner.Room=tone; owner.Winter=tone; owner.Music=tone;
                    if(!returnFromStory) { owner.Session.Dispose(); view.Set("Session",null); }
                    var audio=view.Root.AddComponent<CampaignAudio>(); audio.Initialize(owner); view.Set("Audio",audio);
                    var sources=view.Root.GetComponents<AudioSource>(); Assert.That(sources.Length,Is.EqualTo(5));
                    var room=sources[0]; var winter=sources[1]; var music=sources[4];
                    yield return null;
                    Assert.That(room.isPlaying && winter.isPlaying,Is.True,"Menu audio transport must exist before checking the controller's gain projection.");
                    if(returnFromStory) {
                        audio.Refresh(owner.Input,true);
                        Assert.That(room.volume,Is.GreaterThan(0));
                        owner.ReturnToSavedMenu();
                        float until=Time.realtimeSinceStartup+5;
                        while(owner.Busy && Time.realtimeSinceStartup<until) yield return null;
                        Assert.That(owner.Busy,Is.False,"The actual saved-menu transition must finish.");
                    }
                    Assert.That(owner.Session,Is.Null);
                    Assert.That(owner.StartupError,Is.Null);
                    var update=typeof(CampaignController).GetMethod("Update",P09UiFixture.Hidden);
                    update.Invoke(owner,null);
                    Assert.That(room.volume,Is.EqualTo(owner.MasterVolume*.15f).Within(.00001f),"The menu uses the controller update path, not a direct Audio.Refresh from the test.");
                    Assert.That(winter.volume,Is.EqualTo(owner.MasterVolume*.08f).Within(.00001f));
                    owner.ChangeSettings(0,owner.LargeText,owner.HighContrast);
                    update.Invoke(owner,null);
                    Assert.That(sources.Select(x=>x.volume),Is.All.EqualTo(0f),"Changing master in the saved-game menu must mute existing ambience on that update.");
                    Assert.That(room.isPlaying && winter.isPlaying,Is.True,"Master gain does not stop or restart the ambience transport.");
                    room.timeSamples=12000; winter.timeSamples=12000;
                    owner.ChangeSettings(.3f,owner.LargeText,owner.HighContrast);
                    update.Invoke(owner,null);
                    Assert.That(room.volume,Is.EqualTo(.045f).Within(.00001f));
                    Assert.That(winter.volume,Is.EqualTo(.024f).Within(.00001f));
                    Assert.That(room.timeSamples,Is.GreaterThanOrEqualTo(12000));
                    Assert.That(winter.timeSamples,Is.GreaterThanOrEqualTo(12000));
                    Assert.That(music.isPlaying,Is.False,"Menu gain restoration cannot replay completion music.");
                    Assert.That(owner.Session,Is.Null); Assert.That(owner.StartupError,Is.Null);
                } finally { Object.DestroyImmediate(tone); }
            }
        }
    }

    public sealed class CampaignAudioTests
    {
        private CampaignAudioFixture f;
        [SetUp] public void Setup()=>f=new CampaignAudioFixture();
        [TearDown] public void Teardown()=>f.Dispose();

        [UnityTest] public IEnumerator AudioTransportPositiveControlCanPlayAndStopTheActualUnitySource()
        {
            var source=f.Source("cue"); source.clip=f.Owner.Ice; source.Play(); yield return null;
            Assert.That(source.isPlaying,Is.True,"Audio transport prerequisite failed; do not classify downstream transport cases as behavioral RED.");
            source.Stop(); Assert.That(source.isPlaying,Is.False);
        }

        [Test] public void AcceptedShakeDoesNotSelectOrRestartTheStirLoop()
        {
            f.ShakeReversal(); f.Refresh();
            Assert.That(f.Source("physical").clip,Is.Not.SameAs(f.Owner.Stir),"A real Shake reversal must not be represented by Stir audio.");
            Assert.That(f.Source("physical").clip,Is.SameAs(f.Owner.Shake)); Assert.That(f.Source("physical").loop,Is.False);
            Assert.That(f.Session.Brew.Container(ContainerRole.Shaker).TotalMl,Is.EqualTo(20));
            Assert.That(f.Session.Brew.ShakeReversals,Is.EqualTo(1));
        }

        [Test] public void StationaryShakeAndRepeatedRefreshDoNotRestartTheAcceptedContact()
        {
            f.ShakeReversal(); f.Refresh(); var physical=f.Source("physical");
            Assert.That(physical.isPlaying,Is.True); physical.timeSamples=12000;
            f.Refresh(); Assert.That(physical.timeSamples,Is.GreaterThanOrEqualTo(12000));
            f.Input.Step(.25); f.Refresh();
            Assert.That(f.Input.AcceptedShakeThisStep,Is.False); Assert.That(physical.timeSamples,Is.GreaterThanOrEqualTo(12000));
            physical.Stop(); f.Input.Step(.25); f.Refresh();
            Assert.That(physical.isPlaying,Is.False,"Stationary holding cannot restart an exhausted contact tail.");
        }

        [Test] public void ActualStirStillUsesItsAcceptedMotionLoop()
        {
            Assert.That(f.Session.ExecuteBrew(f.Input.NextEventId(),b=>{b.Pour("gin",ContainerRole.Mixing,20); b.AddIce(ContainerRole.Mixing);}),Is.True);
            f.Move(.5f+30f/Screen.width,.5f); f.KeyDown(Key.S);
            f.Move(.5f+Mathf.Cos(20*Mathf.Deg2Rad)*30f/Screen.width,.5f+Mathf.Sin(20*Mathf.Deg2Rad)*30f/Screen.height);
            f.Input.Step(.1); f.Refresh();
            Assert.That(f.Input.AcceptedStirThisStep,Is.True); Assert.That(f.Input.AcceptedShakeThisStep,Is.False);
            Assert.That(f.Source("physical").clip,Is.SameAs(f.Owner.Stir)); Assert.That(f.Source("physical").loop,Is.True);
        }

        [TestCase("release")][TestCase("modal")][TestCase("focus")][TestCase("session")]
        public void AcceptedShakeTailStopsAtCancellation(string boundary)
        {
            f.ShakeReversal(); f.Refresh(); Assert.That(f.Source("physical").isPlaying,Is.True);
            if(boundary=="release") f.KeyUp(); else if(boundary=="modal") f.Session.OpenModal("guide");
            else if(boundary=="focus") f.Session.SetFocus(false); else { f.Session.Dispose(); f.Set("Session",null); }
            f.Input.Step(.1); f.Refresh();
            Assert.That(f.Input.AcceptedShakeThisStep,Is.False); Assert.That(f.Source("physical").isPlaying,Is.False);
            Assert.That(f.Source("physical").clip,Is.Null);
        }

        [Test] public void MissingOptionalShakeClipLeavesTheAcceptedReversalIntactAndSilent()
        {
            f.Owner.Shake=null; f.ShakeReversal(); Assert.DoesNotThrow(()=>f.Refresh());
            Assert.That(f.Input.AcceptedShakeThisStep,Is.True); Assert.That(f.Session.Brew.ShakeReversals,Is.EqualTo(1));
            Assert.That(f.Session.Brew.Container(ContainerRole.Shaker).TotalMl,Is.EqualTo(20));
            Assert.That(f.Source("physical").clip,Is.Null); Assert.That(f.Source("physical").isPlaying,Is.False);
        }

        [UnityTest] public IEnumerator AllAuthoredReadingStatesUseTheQuieterRoomTreatment()
        {
            // Allow presentation fades to settle; the test does not require an instantaneous duck.
            foreach(var phase in new[]{CampaignPhase.Intro,CampaignPhase.Request,CampaignPhase.Choice,CampaignPhase.ChoiceResponse,
                CampaignPhase.Reaction,CampaignPhase.Retry,CampaignPhase.Farewell,
                CampaignPhase.EndingEntered,CampaignPhase.EndingReading,CampaignPhase.Complete})
            {
                f.At(CampaignPhase.Brewing); float until=Time.realtimeSinceStartup+.8f;
                while(Time.realtimeSinceStartup<until) { f.Refresh(); yield return null; }
                float idle=f.Source("room").volume;
                f.At(phase,phase==CampaignPhase.EndingReading ? 2 : 0);
                Assert.That(f.Session.CurrentText,Is.Not.Null,phase+" must have actual authored text.");
                until=Time.realtimeSinceStartup+.8f;
                while(Time.realtimeSinceStartup<until) { f.Refresh(); yield return null; }
                Assert.That(f.Source("room").volume,Is.LessThan(idle-.0001f),phase+" reading must be quieter than active work.");
            }
        }

        [Test] public void MasterZeroImmediatelySilencesEverySourceIncludingAnExistingMusicEnvelope()
        {
            f.At(CampaignPhase.Complete); f.Refresh();
            foreach(var source in f.Host.GetComponents<AudioSource>()) source.volume=.4f;
            f.Set("MasterVolume",0f); f.Refresh();
            Assert.That(f.Host.GetComponents<AudioSource>().Select(x=>x.volume),Is.All.EqualTo(0f),"Mute multiplies the envelope immediately, rather than fading the master setting.");
            Assert.That(f.Session.State.Phase,Is.EqualTo(CampaignPhase.Complete));
        }

        [UnityTest] public IEnumerator ActiveMusicMuteMultipliesTheEnvelopeWithoutRestartingTheCue()
        {
            f.At(CampaignPhase.EndingReading,8); f.Refresh();
            f.Session.AdvanceAsync(f.Input.NextEventId()).GetAwaiter().GetResult(); f.Refresh(); yield return null;
            var music=f.Source("music"); Assert.That(music.isPlaying,Is.True); music.timeSamples=12000;
            f.Set("MasterVolume",0f); f.Refresh();
            Assert.That(f.Host.GetComponents<AudioSource>().Select(x=>x.volume),Is.All.EqualTo(0f));
            Assert.That(music.isPlaying,Is.True,"Master gain does not replace the cue's lifecycle.");
            f.Set("MasterVolume",.5f); f.Refresh();
            Assert.That(music.volume,Is.GreaterThan(0).And.LessThanOrEqualTo(.06f));
            Assert.That(music.timeSamples,Is.GreaterThanOrEqualTo(12000));
        }

        [TestCase(false)][TestCase(true)]
        public void CompletedContinueAfterAnotherSessionHasNoMusicOrStaleEnvelope(bool sameRun)
        {
            string run=f.Session.State.RunId; f.Refresh();
            f.Source("music").volume=.04f; // Existing presentation envelope before session replacement.
            f.At(CampaignPhase.Complete,0,sameRun ? run : null); var before=f.Session.State;
            f.Refresh();
            Assert.That(f.Source("music").isPlaying,Is.False,"Loading Complete is not a live completion transition.");
            Assert.That(f.Source("music").volume,Is.Zero,"A new bound session must not inherit a musical envelope.");
            Assert.That(f.Session.State,Is.SameAs(before));
        }

        [Test] public void FreshCompletedContinueAndMenuRebindRemainSilent()
        {
            f.At(CampaignPhase.Complete); f.Refresh();
            Assert.That(f.Source("music").isPlaying,Is.False);
            Assert.That(f.Source("music").volume,Is.Zero);
            var state=f.Session.State; f.Input.Stop(); f.Session.Dispose(); f.Set("Session",null); f.Refresh();
            f.Bind(CampaignSession.Continue(f.Content,f.Saves,state)); f.Refresh();
            Assert.That(f.Source("music").isPlaying,Is.False); Assert.That(f.Source("music").volume,Is.Zero);
        }

        [UnityTest] public IEnumerator LiveLastLinePublicationPlaysOnceAndInterruptedCueDoesNotResume()
        {
            f.At(CampaignPhase.EndingReading,8); f.Refresh(); Assert.That(f.Source("music").isPlaying,Is.False);
            Assert.That(f.Session.AdvanceAsync(f.Input.NextEventId()).GetAwaiter().GetResult(),Is.True); f.Refresh(); yield return null;
            var music=f.Source("music"); Assert.That(music.isPlaying,Is.True,"Actual successful last-line publication should produce one motif.");
            music.timeSamples=12000; f.Refresh(); Assert.That(music.timeSamples,Is.GreaterThanOrEqualTo(12000),"Repeated Refresh must not restart the cue.");
            f.Session.OpenModal("settings"); f.Refresh(); Assert.That(music.isPlaying,Is.False);
            f.Session.CloseModal(); f.Refresh(); Assert.That(music.isPlaying,Is.False,"Closing a modal must not replay a consumed cue.");
            Assert.That(music.volume,Is.Zero,"An interrupted motif must not silently ramp a stale envelope.");
        }

        [Test] public void FailedEndingPublicationCannotStartTheCompletionCue()
        {
            f.At(CampaignPhase.EndingReading,8); f.Refresh(); var before=f.Session.State;
            f.Saves.Fail=true; f.Session.AdvanceAsync(f.Input.NextEventId()).GetAwaiter().GetResult(); f.Refresh();
            Assert.That(f.Session.SaveError,Is.Not.Null); Assert.That(f.Session.State,Is.SameAs(before));
            Assert.That(f.Source("music").isPlaying,Is.False); Assert.That(f.Source("music").volume,Is.Zero);
        }

        [Test] public void CompletionWhileAudioIsDisabledDoesNotBecomeADelayedCueOnEnable()
        {
            f.At(CampaignPhase.EndingReading,8); f.Refresh();
            f.Audio.enabled=false; f.AudioLifecycle("OnDisable");
            Assert.That(f.Session.AdvanceAsync(f.Input.NextEventId()).GetAwaiter().GetResult(),Is.True); f.Refresh();
            f.Audio.enabled=true; f.AudioLifecycle("OnEnable"); f.Refresh();
            Assert.That(f.Session.State.Phase,Is.EqualTo(CampaignPhase.Complete));
            Assert.That(f.Source("music").isPlaying,Is.False); Assert.That(f.Source("music").volume,Is.Zero);
        }

        [TestCase("modal")][TestCase("focus")][TestCase("session")][TestCase("disable")]
        public void CancellationStopsDiscreteCueTailsAsWellAsPhysicalLoops(string boundary)
        {
            f.Audio.Cue("ice");
            var physical=f.Source("physical"); physical.clip=f.Owner.Pour; physical.Play();
            if(boundary=="modal") f.Session.OpenModal("guide");
            else if(boundary=="focus") f.Session.SetFocus(false);
            else if(boundary=="session") { f.Session.Dispose(); f.Set("Session",null); }
            else {
                f.Audio.enabled=false;
                // EditMode does not awaken this fixture's MonoBehaviour. Invoke the
                // actual callback directly: SendMessage rejects disabled behaviours.
                // Real Player lifecycle delivery remains separate evidence.
                f.AudioLifecycle("OnDisable"); f.AudioLifecycle("OnDisable");
            }
            if(boundary!="disable") f.Refresh();
            Assert.That(f.Source("cue").isPlaying,Is.False,"Old action contact cannot continue under a canceled context.");
            Assert.That(physical.isPlaying,Is.False);
        }

        [Test] public void MissingOptionalPhysicalClipsDoNotChangeAcceptedBrewOrPublication()
        {
            f.Owner.Pour=null; f.Owner.Stir=null; f.Owner.Strain=null;
            f.Input.SelectIngredient("gin"); f.Input.SelectTarget(ContainerRole.Serving); f.KeyDown(Key.Q); f.Input.Step(.1);
            decimal total=f.Session.Brew.Container(ContainerRole.Serving).TotalMl;
            Assert.That(total,Is.GreaterThan(0)); Assert.DoesNotThrow(()=>f.Refresh());
            Assert.That(f.Session.Brew.Container(ContainerRole.Serving).TotalMl,Is.EqualTo(total));
            Assert.That(f.Source("physical").isPlaying,Is.False); f.KeyUp();
            Assert.That(f.Session.AdvanceAsync(f.Input.NextEventId()).GetAwaiter().GetResult(),Is.True);
            Assert.That(f.Session.State.Services.Count,Is.EqualTo(1));
        }
    }
}
