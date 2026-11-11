using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Domain;

namespace Surlhwa.SpatialTests
{
    public sealed class CampaignAudioAcceptedCueTests
    {
        private sealed class SliceStore : ISliceSaveStore { public Task SaveAsync(SliceCheckpoint state)=>Task.CompletedTask; }
        private CampaignAudioFixture f;
        private readonly List<string> cues=new List<string>();
        [SetUp] public void Setup() { f=new CampaignAudioFixture(); cues.Clear(); f.Input.AcceptedCue+=cues.Add; }
        [TearDown] public void Teardown()=>f.Dispose();

        [TestCase(false)][TestCase(true)]
        public void RepeatedIceHasOneActualMutationAndOneContactInCampaignAndSharedSlice(bool legacySlice)
        {
            SliceSession slice=null;
            try {
                IBrewingSession session=f.Session;
                if(legacySlice) {
                    var content=SliceContentLoader.Load(SliceContentLoader.BundleFromDirectory(Path.GetFullPath("../Content")));
                    slice=SliceSession.StartAsync(content,new SliceStore()).GetAwaiter().GetResult(); long id=1;
                    while(slice.State.Phase!=SlicePhase.Brewing)
                        if(slice.State.Phase==SlicePhase.Arriving) slice.TickAsync(2).GetAwaiter().GetResult(); else slice.AdvanceAsync(id++).GetAwaiter().GetResult();
                    session=slice; f.Input.Bind(slice,null,Vector3.zero);
                }
                f.Input.SelectTarget(ContainerRole.Serving); f.Input.Command("ice"); int after=session.Brew.Revision;
                Assert.That(session.Brew.Container(ContainerRole.Serving).HasIce,Is.True); Assert.That(cues,Is.EqualTo(new[]{"ice"}));
                f.Input.Command("ice");
                Assert.That(session.Brew.Revision,Is.EqualTo(after),"Repeated ice is a domain no-op.");
                Assert.That(cues,Is.EqualTo(new[]{"ice"}),"Accepted command dispatch alone does not justify a second physical contact.");
            } finally { slice?.Dispose(); }
        }

        [TestCase("gentle")][TestCase("not_a_command")]
        public void AcceptedNoOpCommandDoesNotEmitPhysicalContact(string command)
        {
            int before=f.Session.Brew.Revision; f.Input.Command(command);
            Assert.That(f.Session.Brew.Revision,Is.EqualTo(before)); Assert.That(cues,Is.Empty);
        }

        [Test] public void RejectedGarnishAndModalInputStaySilent()
        {
            int before=f.Session.Brew.Revision; f.Input.Command("cherry");
            Assert.That(f.Input.Feedback,Is.Not.Null); Assert.That(cues,Is.Empty);
            f.Session.OpenModal("codex"); f.Input.Command("ice");
            Assert.That(f.Session.Brew.Revision,Is.EqualTo(before)); Assert.That(cues,Is.Empty);
        }

        [Test] public void AcceptedGentleActionHasExactlyOneContactAndPreservesLiquid()
        {
            Assert.That(f.Session.ExecuteBrew(f.Input.NextEventId(),b=>b.Pour("gin",ContainerRole.Serving,20)),Is.True);
            int before=f.Session.Brew.Revision; f.Input.Command("gentle");
            Assert.That(f.Session.Brew.Revision,Is.EqualTo(before+1)); Assert.That(cues,Is.EqualTo(new[]{"gentle"}));
            Assert.That(f.Session.Brew.Container(ContainerRole.Serving).TotalMl,Is.EqualTo(20));
        }

        [Test] public void CancelingShakeBeforeRepeatedIceDoesNotMisattributeTheCancellationRevision()
        {
            f.Input.SelectTarget(ContainerRole.Serving); f.Input.Command("ice"); cues.Clear();
            f.ShakeReversal(); int before=f.Session.Brew.Revision; f.Input.Command("ice");
            Assert.That(f.Session.Brew.Revision,Is.GreaterThan(before),"Ending the real Shake records its evidence before the ice command.");
            Assert.That(f.Session.Brew.Container(ContainerRole.Serving).HasIce,Is.True);
            Assert.That(cues,Is.Empty,"A revision caused by Stop is not a new ice contact.");
            Assert.That(f.Session.Brew.Container(ContainerRole.Shaker).TotalMl,Is.EqualTo(20));
        }

        [Test] public void RealShakeReversalDoesNotClaimAcceptedStirMovement()
        {
            f.ShakeReversal();
            Assert.That(f.Input.AcceptedStirThisStep,Is.False,"Shake must have its own accepted movement signal.");
            Assert.That(f.Input.AcceptedShakeThisStep,Is.True);
            Assert.That(f.Session.Brew.ShakeReversals,Is.EqualTo(1));
        }

        [Test] public void StationaryShakeCannotAddTechniqueAndReleaseClearsHeldActivity()
        {
            f.ShakeReversal(); f.Input.Step(.25); f.Input.Step(.25);
            Assert.That(f.Session.Brew.ShakeReversals,Is.EqualTo(1)); Assert.That(f.Input.AcceptedStirThisStep,Is.False); Assert.That(f.Input.AcceptedShakeThisStep,Is.False);
            f.KeyUp(); f.Input.Step(.1);
            Assert.That(f.Input.ActiveTool,Is.Null); Assert.That(f.Input.AcceptedStirThisStep || f.Input.AcceptedLiquidThisStep || f.Input.AcceptedShakeThisStep,Is.False);
            Assert.That(f.Session.Brew.Container(ContainerRole.Shaker).TotalMl,Is.EqualTo(20));
        }

        [TestCase("modal")][TestCase("focus")][TestCase("release")]
        public void AcceptedLiquidStopsAtExistingCancellationBoundaryWithoutResuming(string boundary)
        {
            f.Input.SelectIngredient("gin"); f.Input.SelectTarget(ContainerRole.Serving); f.KeyDown(Key.Q); f.Input.Step(.1);
            Assert.That(f.Input.AcceptedLiquidThisStep,Is.True); decimal accepted=f.Session.Brew.Container(ContainerRole.Serving).TotalMl;
            if(boundary=="modal") f.Session.OpenModal("guide"); else if(boundary=="focus") f.Session.SetFocus(false); else f.KeyUp();
            f.Input.Step(.1);
            Assert.That(f.Input.AcceptedLiquidThisStep,Is.False); Assert.That(f.Input.ActiveTool,Is.Null);
            if(boundary=="modal") f.Session.CloseModal(); else if(boundary=="focus") f.Session.SetFocus(true);
            f.Input.Step(.1); Assert.That(f.Session.Brew.Container(ContainerRole.Serving).TotalMl,Is.EqualTo(accepted));
        }

        [Test] public void NormalReleaseExposesOnlyThePreviousToolAndNeverAddsLiquid()
        {
            f.Input.SelectIngredient("gin"); f.Input.SelectTarget(ContainerRole.Serving); f.KeyDown(Key.Q); f.Input.Step(.1);
            decimal accepted=f.Session.Brew.Container(ContainerRole.Serving).TotalMl; int revision=f.Session.Brew.Revision;
            f.KeyUp();
            Assert.That(f.Input.ActiveTool,Is.Null); Assert.That(f.Input.ReleasedTool,Is.EqualTo("pour"));
            Assert.That(f.Input.AcceptedLiquidThisStep,Is.False);
            f.Input.Step(.1);
            Assert.That(f.Input.ReleasedTool,Is.EqualTo("pour"),"The view owns the short return duration, not the input clock.");
            Assert.That(f.Session.Brew.Container(ContainerRole.Serving).TotalMl,Is.EqualTo(accepted)); Assert.That(f.Session.Brew.Revision,Is.EqualTo(revision));
            f.Input.Begin("pour"); Assert.That(f.Input.ReleasedTool,Is.Null); Assert.That(f.Input.ActiveTool,Is.EqualTo("pour"));
        }

        [TestCase("stop")][TestCase("ingredient")][TestCase("target")][TestCase("bind")]
        [TestCase("modal")][TestCase("focus")][TestCase("session")][TestCase("disable")]
        public void CancellationClearsAnEligibleReturnWithoutChangingTheDrink(string boundary)
        {
            f.Input.SelectIngredient("gin"); f.Input.SelectTarget(ContainerRole.Serving); f.KeyDown(Key.Q); f.Input.Step(.1); f.KeyUp();
            Assert.That(f.Input.ReleasedTool,Is.EqualTo("pour"));
            var brew=f.Session.Brew; decimal accepted=brew.Container(ContainerRole.Serving).TotalMl;
            if(boundary=="stop") f.Input.Stop();
            else if(boundary=="ingredient") f.Input.SelectIngredient("water");
            else if(boundary=="target") f.Input.SelectTarget(ContainerRole.Mixing);
            else if(boundary=="bind") f.Input.Bind(f.Session,null,Vector3.zero);
            else if(boundary=="modal") { f.Session.OpenModal("guide"); f.Input.Step(.1); }
            else if(boundary=="focus") { f.Session.SetFocus(false); f.Input.Step(.1); }
            else if(boundary=="session") { f.Session.Dispose(); f.Input.Step(.1); }
            else {
                f.Input.enabled=false;
                typeof(Surlhwa.Presentation.SliceInputAdapter).GetMethod("OnDisable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(f.Input,null);
            }
            Assert.That(f.Input.ReleasedTool,Is.Null); Assert.That(f.Input.ActiveTool,Is.Null);
            Assert.That(brew.Container(ContainerRole.Serving).TotalMl,Is.EqualTo(accepted));
        }

        [TestCase("modal")][TestCase("focus")][TestCase("session")][TestCase("disable")]
        public void KeyUpAfterCancellationCannotCreateAReturn(string boundary)
        {
            f.Input.SelectIngredient("gin"); f.Input.SelectTarget(ContainerRole.Serving); f.KeyDown(Key.Q); f.Input.Step(.1);
            if(boundary=="modal") f.Session.OpenModal("guide"); else if(boundary=="focus") f.Session.SetFocus(false);
            else if(boundary=="session") f.Session.Dispose(); else f.Input.enabled=false;
            f.Input.Release();
            Assert.That(f.Input.ReleasedTool,Is.Null); Assert.That(f.Input.ActiveTool,Is.Null);
        }
    }
}
