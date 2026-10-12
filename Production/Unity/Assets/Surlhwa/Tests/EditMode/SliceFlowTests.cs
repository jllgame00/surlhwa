using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Surlhwa.Domain;
using Surlhwa.Application;
using Surlhwa.ContentIO;

namespace Surlhwa.Tests
{
    public sealed class SliceFlowTests
    {
        internal sealed class MemoryStore : ISliceSaveStore
        {
            internal SliceCheckpoint Saved; internal bool Fail;
            public Task SaveAsync(SliceCheckpoint state)
            { if(Fail) throw new InvalidOperationException("disk failure"); Saved=state; return Task.CompletedTask; }
        }
        internal static SliceContent Content() => SliceContentLoader.Load(SliceContentTests.Bundle());
        internal static SliceSession Start(MemoryStore store=null) => SliceSession.StartAsync(Content(),store ?? new MemoryStore()).GetAwaiter().GetResult();
        internal static long serial;
        internal static void Advance(SliceSession s) => Assert.That(Task.Run(()=>s.AdvanceAsync(++serial)).GetAwaiter().GetResult(),Is.True);
        internal static void ToBrew(SliceSession s)
        {
            for(int guard=0;s.State.Phase!=SlicePhase.Brewing && guard<30;guard++)
                if(s.State.Phase==SlicePhase.Arriving || s.State.Phase==SlicePhase.Departing) s.TickAsync(2).GetAwaiter().GetResult(); else Advance(s);
            Assert.That(s.State.Phase,Is.EqualTo(SlicePhase.Brewing));
        }
        internal static void Make(SliceSession s,string recipe)
        {
            Assert.That(s.ExecuteBrew(++serial,b=> {
                var r=s.Content.Catalog.Recipes[recipe];
                foreach(var a in r.Amounts) b.Pour(a.Key,ContainerRole.Mixing,a.Value,a.Key==r.DashIngredient ? BrewAction.Dash : BrewAction.Pour);
                b.AddIce(ContainerRole.Mixing); BartendingDomainTests.Circles(b);
                b.Transfer(ContainerRole.Mixing,ContainerRole.Serving,b.Container(ContainerRole.Mixing).TotalMl,BrewAction.Strain);
                if(r.RequiredGarnish!=null) b.Garnish(r.RequiredGarnish,"in_glass");
            }),Is.True);
        }
        [Test] public void ReadingIsConsumedOnce_AndModalCannotAdvanceOrLeakHeldLiquid()
        {
            var s=Start(); Advance(s); s.TickAsync(2).GetAwaiter().GetResult(); long id=++serial;
            Assert.That(s.AdvanceAsync(id).GetAwaiter().GetResult(),Is.True);
            int lines=s.State.ConsumedLines.Count;
            Assert.That(s.AdvanceAsync(id).GetAwaiter().GetResult(),Is.False); Assert.That(s.State.ConsumedLines.Count,Is.EqualTo(lines));
            ToBrew(s); s.ExecuteBrew(++serial,b=>b.BeginPour("gin",ContainerRole.Mixing));
            s.OpenModal("guide"); Assert.That(s.Brew.FlowActive,Is.False);
            Assert.That(s.UpdateBrew(1000000,false),Is.False); s.CloseModal();
            Assert.That(s.UpdateBrew(1000000,false),Is.False);
            s.ReleaseHeldInput(); Assert.That(s.ExecuteBrew(++serial,b=>b.BeginPour("gin",ContainerRole.Mixing)),Is.True);
            s.UpdateBrew(1000000,false); Assert.That(s.Brew.Container(ContainerRole.Mixing).TotalMl,Is.EqualTo(30m));
        }
        [Test] public void DryMismatchDiscoversOnce_AndBothEncountersEndWithoutActOrMemory()
        {
            var s=Start(); ToBrew(s); Make(s,"recipe_dry_martini"); Advance(s);
            Assert.That(s.State.Phase,Is.EqualTo(SlicePhase.Reaction)); Assert.That(s.State.CurrentService.Outcome.Fits,Is.False);
            Assert.That(s.Knowledge.Project("recipe_dry_martini").Known,Is.True);
            Advance(s); Assert.That(s.State.Phase,Is.EqualTo(SlicePhase.Result)); Advance(s);
            Assert.That(s.State.Phase,Is.EqualTo(SlicePhase.Discovery)); Advance(s);
            Assert.That(s.State.Phase,Is.EqualTo(SlicePhase.Farewell)); ToBrew(s);
            Assert.That(s.CurrentEncounter.GuestId,Is.EqualTo("guest_little_prince"));
            Make(s,"recipe_manhattan"); Advance(s);
            for(int i=0;i<20 && s.State.Phase!=SlicePhase.Complete;i++)
                if(s.State.Phase==SlicePhase.Departing) s.TickAsync(2).GetAwaiter().GetResult(); else Advance(s);
            Assert.That(s.State.Phase,Is.EqualTo(SlicePhase.Complete)); Assert.That(s.State.Services.Count,Is.EqualTo(2));
            Assert.That(s.State.CompletedOccurrences.Count,Is.EqualTo(2)); Assert.That(s.State.ActComplete,Is.False);
            Assert.That(s.State.MemoryIds,Is.Empty); Assert.That(s.Knowledge.KnownReceipts.Count,Is.EqualTo(3));
        }
        [Test] public void ContinueBrewStartsEmpty_ButCommittedOutcomeAndReadingCursorSurvive()
        {
            var store=new MemoryStore(); var s=Start(store); ToBrew(s); Make(s,"recipe_manhattan");
            var resumed=SliceSession.Continue(Content(),store,store.Saved);
            Assert.That(resumed.Brew.Container(ContainerRole.Serving).TotalMl,Is.Zero);
            Advance(s); Advance(s); var committed=store.Saved;
            resumed=SliceSession.Continue(Content(),store,committed);
            Assert.That(resumed.State.Phase,Is.EqualTo(SlicePhase.Result));
            Assert.That(resumed.State.CurrentService.Snapshot.TotalMl,Is.EqualTo(71m));
            Assert.That(resumed.State.CurrentService.Outcome.Preparation,Is.EqualTo(PreparationResult.Good));
            Assert.That(resumed.State.Services.Count,Is.EqualTo(1));
        }
        [Test] public void FailedCommitBlocksProgress_RetryPublishesSameTransaction()
        {
            var store=new MemoryStore(); var s=Start(store); ToBrew(s); Make(s,"recipe_manhattan");
            long sequence=s.State.Sequence; store.Fail=true;
            Assert.That(s.AdvanceAsync(++serial).GetAwaiter().GetResult(),Is.False); Assert.That(s.SaveError,Is.Not.Null);
            Assert.That(s.State.Sequence,Is.EqualTo(sequence));
            Assert.That(s.ExecuteBrew(++serial,b=>b.Pour("water",ContainerRole.Serving,1)),Is.False);
            store.Fail=false; Assert.That(s.RetrySaveAsync().GetAwaiter().GetResult(),Is.True);
            Assert.That(s.State.Sequence,Is.EqualTo(sequence+1)); Assert.That(s.State.Services.Count,Is.EqualTo(1));
        }
        [Test] public void UnknownDrinkStillLeaves_NoForcedRetryAndNoDiscovery()
        {
            var s=Start(); ToBrew(s); s.ExecuteBrew(++serial,b=>b.Pour("water",ContainerRole.Serving,5)); Advance(s);
            Assert.That(s.State.CurrentService.Outcome.Recognition,Is.EqualTo(DrinkRecognition.Unknown));
            Assert.That(s.CurrentText.Value,Is.EqualTo(s.Content.Texts[s.CurrentEncounter.Responses["unknown"]].Value));
            Advance(s); Advance(s); Assert.That(s.State.Phase,Is.EqualTo(SlicePhase.Farewell));
            ToBrew(s); Assert.That(s.State.CompletedOccurrences.Count,Is.EqualTo(1));
        }
        private sealed class DelayedStore : ISliceSaveStore
        {
            internal TaskCompletionSource<bool> Pending;
            public Task SaveAsync(SliceCheckpoint checkpoint) => Pending?.Task ?? Task.CompletedTask;
        }
        [Test] public void PendingWriterRejectsDuplicateCommands_AndDisposedSessionCannotAdvance()
        {
            var store=new DelayedStore(); var s=SliceSession.StartAsync(Content(),store).GetAwaiter().GetResult();
            store.Pending=new TaskCompletionSource<bool>(); var first=s.AdvanceAsync(++serial);
            Assert.That(s.IsSaving,Is.True); Assert.That(s.AdvanceAsync(++serial).GetAwaiter().GetResult(),Is.False);
            s.Dispose(); store.Pending.SetResult(true);
            Assert.That(first.GetAwaiter().GetResult(),Is.False); Assert.That(s.State.Phase,Is.EqualTo(SlicePhase.Welcome));
        }
    }
}
