using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Domain;
using Surlhwa.Presentation;

namespace Surlhwa.SpatialTests
{
    // Input isolation fixture, not an earned campaign save or normal-play proof.
    public sealed class ActFourInputTests
    {
        private sealed class Store : ICampaignSaveStore { public Task SaveAsync(CampaignCheckpoint s)=>Task.CompletedTask; }
        private InputTestFixture fixture; private Keyboard keyboard; private GameObject go,work;
        private CampaignSession session; private SliceInputAdapter input;
        [SetUp] public void Setup()
        {
            fixture=new InputTestFixture(); fixture.Setup(); keyboard=InputSystem.AddDevice<Keyboard>(); InputSystem.AddDevice<Mouse>();
            var c=CampaignContentLoader.Load(File.ReadAllText(Path.GetFullPath("../Content/Campaign/act1.json")));
            session=CampaignSession.StartAsync(c,new Store()).GetAwaiter().GetResult(); long n=1;
            while(session.State.Phase!=CampaignPhase.Brewing)
                if(session.State.Phase==CampaignPhase.Arriving) session.TickAsync(2).GetAwaiter().GetResult(); else session.AdvanceAsync(n++).GetAwaiter().GetResult();
            go=new GameObject("ACT4 isolated physical control fixture"); input=go.AddComponent<SliceInputAdapter>(); input.Bind(session,null,Vector3.zero);
            input.SelectIngredient("water"); input.SelectTarget(ContainerRole.Shaker); Flow(Key.Q,1);
        }
        [TearDown] public void Teardown()
        { if(work!=null) UnityEngine.Object.DestroyImmediate(work); UnityEngine.Object.DestroyImmediate(go); session.Dispose(); fixture.TearDown(); }
        private void Down(Key key) { InputSystem.QueueStateEvent(keyboard,new KeyboardState(key)); InputSystem.Update(); }
        private void Up() { InputSystem.QueueStateEvent(keyboard,new KeyboardState()); InputSystem.Update(); }
        private void Tap(Key key) { Down(key); Up(); }
        private void Flow(Key key,double seconds) { Down(key); while(seconds>0) { double dt=Math.Min(.1,seconds); input.Step(dt); seconds-=dt; } Up(); }
        private void EnableExpandedStrain()
        {
            var gate=typeof(SliceInputAdapter).GetProperty("ShakerStrainAvailable");
            Assert.That(gate,Is.Not.Null,"Later-act Shaker Strain needs an explicit availability gate; ACT1 U remains inert.");
            gate.SetValue(input,true);
        }
        [Test] public void ShakerStrainRequiresExpansionGateAndRetainsMixingStrainBinding()
        {
            Flow(Key.U,.1); Assert.That(session.Brew.Container(ContainerRole.Serving).TotalMl,Is.Zero);
            Assert.That(session.Brew.Container(ContainerRole.Shaker).TotalMl,Is.EqualTo(30));
            EnableExpandedStrain(); Flow(Key.U,.1);
            decimal served=session.Brew.Container(ContainerRole.Serving).TotalMl;
            Assert.That(served,Is.GreaterThan(0));
            Assert.That(session.Brew.Container(ContainerRole.Shaker).TotalMl+served,Is.EqualTo(30));
            var first=session.Brew.Freeze().Trace.Last(x=>x.AcceptedMl>0);
            Assert.That(first.Kind,Is.EqualTo(BrewAction.Strain)); Assert.That(first.Source,Is.EqualTo("shaker"));
            decimal remaining=session.Brew.Container(ContainerRole.Shaker).TotalMl;
            input.SelectTarget(ContainerRole.Mixing); Flow(Key.Q,.5); Flow(Key.R,.1);
            Assert.That(session.Brew.Container(ContainerRole.Shaker).TotalMl,Is.EqualTo(remaining),"R remains the accepted Mixing Strain.");
            Assert.That(session.Brew.Freeze().Trace.Last(x=>x.AcceptedMl>0).Source,Is.EqualTo("mixing"));
        }
        [Test] public void ShakerStrainCannotTransferThroughClosedLid()
        {
            EnableExpandedStrain(); Tap(Key.L); Flow(Key.U,.2);
            Assert.That(session.Brew.Container(ContainerRole.Serving).TotalMl,Is.Zero);
            Assert.That(input.ActiveTool,Is.Null); Assert.That(input.Feedback,Is.Not.Null);
            Tap(Key.L); Flow(Key.U,.1); Assert.That(session.Brew.Container(ContainerRole.Serving).TotalMl,Is.GreaterThan(0));
        }
        [Test] public void UnavailableShakerStrainReleaseCannotInterruptAcceptedPour()
        {
            Down(Key.Q); input.Step(.1); decimal before=session.Brew.Container(ContainerRole.Shaker).TotalMl;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Q,Key.U)); InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Q)); InputSystem.Update();
            Assert.That(input.ActiveTool,Is.EqualTo("pour"),"Unavailable U must stay inert, including release, in accepted earlier ACTs.");
            input.Step(.1); Assert.That(session.Brew.Container(ContainerRole.Shaker).TotalMl,Is.GreaterThan(before)); Up();
        }
        [Test] public void ShakerStrainStopsForModalAndRequiresFreshPress()
        {
            EnableExpandedStrain(); Down(Key.U); input.Step(.1); session.OpenModal("codex"); input.Step(.1);
            decimal before=session.Brew.Container(ContainerRole.Serving).TotalMl;
            Assert.That(input.ActiveTool,Is.Null); session.CloseModal(); input.Step(.1);
            Assert.That(session.Brew.Container(ContainerRole.Serving).TotalMl,Is.EqualTo(before));
            Up(); Down(Key.U); input.Step(.1); Up(); Assert.That(session.Brew.Container(ContainerRole.Serving).TotalMl,Is.GreaterThan(before));
        }
        [Test] public void ShakerStrainShowsActualSourceAndSingleStrainerAndStopsOnFocusLoss()
        {
            EnableExpandedStrain(); work=new GameObject("ACT4 actual strainer fixture");
            var view=work.AddComponent<SliceWorkbenchView>(); view.ExtendedTools=true; view.Initialize(null);
            view.ApplyArt(UnityEditor.AssetDatabase.LoadAssetAtPath<CampaignArtSet>("Assets/Surlhwa/Act1Assets/ProductionArt/Act1Art.asset"));
            Down(Key.U); input.Step(.1); view.Refresh(session.Brew,input,.1f);
            var shaker=work.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="Shaker 300 ml");
            var strainer=work.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="Strainer on source");
            var stream=work.transform.Find("Accepted liquid stream").GetComponent<LineRenderer>();
            Assert.That(stream.enabled,Is.True); Assert.That(strainer.parent,Is.SameAs(shaker)); Assert.That(strainer.gameObject.activeSelf,Is.True);
            Assert.That(Vector3.Distance(work.transform.TransformPoint(stream.GetPosition(0)),shaker.TransformPoint(new Vector3(0,.55f,-.02f))),Is.LessThan(.002f));
            session.SetFocus(false); input.Step(.1); view.Refresh(session.Brew,input,.1f);
            Assert.That(stream.enabled,Is.False); Assert.That(strainer.gameObject.activeSelf,Is.False);
        }
    }
}
