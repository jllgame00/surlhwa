using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Surlhwa.Domain;
using Surlhwa.Application;

namespace Surlhwa.Tests
{
    public sealed class CampaignFlowTests
    {
        internal sealed class Store : ICampaignSaveStore
        {
            public CampaignCheckpoint Last; public bool Fail; public int Writes;
            public Task SaveAsync(CampaignCheckpoint state) { Writes++; if(Fail) throw new InvalidOperationException("disk unavailable"); Last=state; return Task.CompletedTask; }
        }
        internal static CampaignContent Content(bool discovery=false)
        {
            var catalog=BartendingDomainTests.Catalog();
            var textIds=new[]{"intro","farewell","summary","good","weak","mismatch","bothWeak","unknown","prompt","keep","seek","keep_response","seek_response","keep_recall","seek_recall","none_recall","open","quiet"};
            var texts=textIds.ToDictionary(x=>x,x=>new SourcedText(x,x,"test_source","test_hash","test_fixture","test","0","spoken","TEST_FIXTURE_ONLY"));
            var responses=new Dictionary<string,string>{{"good","good"},{"weak","weak"},{"mismatch","mismatch"},{"bothWeak","bothWeak"},{"unknown","unknown"}};
            var guests=new Dictionary<string,GuestDefinition>{{"guest",new GuestDefinition(new StableId<GuestIdentity>("guest"),GuestKind.Special,new StableId<TextIdentity>("intro"),new StableId<VisualIdentity>("visual_prince"))}};
            var encounters=new List<CampaignEncounter>();
            for(int i=0;i<3;i++) encounters.Add(new CampaignEncounter("enc_"+i,"guest",i+1,"summary",new DrinkRequest("warmth"),
                new[]{new CampaignLine("intro",new[]{new FactGrant("recipe_manhattan","amount:rye_whiskey",new[]{"test_source"})})},new[]{"farewell"},responses,
                discovery && i==0 ? "recipe_manhattan" : null,discovery && i==0 ? (PrinceVisit?)null : (PrinceVisit)i));
            return new CampaignContent("test_act1_v1","test_hash",catalog,BartendingDomainTests.Tuning(),catalog.Ingredients.ToDictionary(x=>x,x=>x),texts,guests,
                catalog.Recipes.Keys.ToDictionary(x=>x,x=>new RecipePresentation("cocktail",false)),encounters,encounters.Select(x=>new CampaignSlot(x.Id)),catalog.Ingredients,
                new Dictionary<string,string>{{"recipe_test_stir","LEARN-SEOLYA"}},"prompt",new Dictionary<string,string>{{"keep","keep"},{"seek","seek"}},
                new Dictionary<string,string>{{"keep","keep_response"},{"seek","seek_response"}},new Dictionary<string,string>{{"keep","keep_recall"},{"seek","seek_recall"},{"none","none_recall"}},new[]{"open"},new[]{"quiet"});
        }
        internal static long Event=200000;
        internal static void Advance(CampaignSession s) { Task.Run(()=>s.AdvanceAsync(++Event)).GetAwaiter().GetResult(); }
        internal static void ReachBrew(CampaignSession s)
        {
            for(int i=0;i<30 && s.State.Phase!=CampaignPhase.Brewing;i++)
            {
                if(s.State.Phase==CampaignPhase.Arriving || s.State.Phase==CampaignPhase.Departing) s.TickAsync(2).GetAwaiter().GetResult();
                else if(s.State.Phase==CampaignPhase.Choice) s.ChooseAsync("keep",++Event).GetAwaiter().GetResult(); else Advance(s);
            }
            Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Brewing),"Could not reach the real preparation state.");
        }
        internal static void Serve(CampaignSession s,string recipe="recipe_test_stir")
        {
            s.ReleaseHeldInput(); s.ExecuteBrew(++Event,b=> {
                if(recipe==null) b.Pour("water",ContainerRole.Serving,1m);
                else
                {
                    foreach(var a in s.Content.Catalog.Recipes[recipe].Amounts) b.Pour(a.Key,ContainerRole.Mixing,a.Value,a.Key=="angostura_bitters" ? BrewAction.Dash : BrewAction.Pour);
                    b.AddIce(ContainerRole.Mixing); BartendingDomainTests.Circles(b); b.Transfer(ContainerRole.Mixing,ContainerRole.Serving,180m,BrewAction.Strain);
                    if(recipe=="recipe_manhattan") b.Garnish("cherry","in_glass");
                }
            }); Advance(s);
        }
        internal static void ReachDeparture(CampaignSession s)
        { for(int i=0;i<20 && s.State.Phase!=CampaignPhase.Departing;i++) Advance(s); Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Departing)); }

        [Test] public void ConsumedCluePersistsAndContinueStartsWithEmptyTools()
        {
            var store=new Store(); var c=Content(); var s=CampaignSession.StartAsync(c,store).GetAwaiter().GetResult(); ReachBrew(s);
            Assert.That(s.Knowledge.Project("recipe_manhattan").Amounts["rye_whiskey"],Is.EqualTo(50m));
            s.ReleaseHeldInput(); s.ExecuteBrew(++Event,b=>b.Pour("whiskey",ContainerRole.Jigger,20m));
            var continued=CampaignSession.Continue(c,store,store.Last);
            Assert.That(continued.State.SelectedEncounters,Is.EqualTo(s.State.SelectedEncounters));
            Assert.That(continued.Brew.Container(ContainerRole.Jigger).TotalMl,Is.Zero);
            Assert.That(continued.Knowledge.Project("recipe_manhattan").Amounts["rye_whiskey"],Is.EqualTo(50m));
        }
        [Test] public void RequiredDiscoveryRetryRetainsFailedAttemptAndCannotAdvanceSchedule()
        {
            var s=CampaignSession.StartAsync(Content(true),new Store()).GetAwaiter().GetResult(); ReachBrew(s); Serve(s);
            Assert.That(s.Knowledge.Project("recipe_manhattan").Known,Is.False);
            for(int i=0;i<6 && s.State.Phase!=CampaignPhase.Brewing;i++) Advance(s);
            Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Brewing)); Assert.That(s.State.SlotIndex,Is.Zero);
            Assert.That(s.State.CompletedOccurrences.Count,Is.Zero); Assert.That(s.State.Services.Count,Is.EqualTo(1));
            Serve(s,"recipe_manhattan"); ReachDeparture(s); s.TickAsync(2).GetAwaiter().GetResult();
            Assert.That(s.State.Services.Count,Is.EqualTo(2)); Assert.That(s.Knowledge.Project("recipe_manhattan").Known,Is.True);
            Assert.That(s.State.CompletedOccurrences.Count,Is.EqualTo(1));
        }
        [Test] public void FailedFirstPrinceServiceIsRetainedAndMemoryCommitsOnlyAfterNaturalDeparture()
        {
            var store=new Store(); var c=Content(); var s=CampaignSession.StartAsync(c,store).GetAwaiter().GetResult();
            for(int i=0;i<3;i++)
            {
                ReachBrew(s); Serve(s,i==0 ? null : "recipe_test_stir"); ReachDeparture(s);
                Assert.That(s.State.Memories.Count,Is.Zero); Assert.That(s.State.ActComplete,Is.False);
                s.TickAsync(2).GetAwaiter().GetResult();
            }
            Assert.That(s.State.ActComplete,Is.True); Assert.That(s.State.Farewell,Is.EqualTo(PrinceFarewell.Open));
            Assert.That(s.State.Memories.Single().Variant,Is.EqualTo("star_glowing"));
            Assert.That(s.State.Services[0].Outcome.Recognition,Is.EqualTo(DrinkRecognition.Unknown));
            var restored=CampaignSession.Continue(c,store,store.Last); restored.TickAsync(2).GetAwaiter().GetResult();
            Assert.That(restored.State.Memories.Count,Is.EqualTo(1)); Assert.That(restored.State.CompletedOccurrences.Count,Is.EqualTo(3));
        }
        [Test] public void FailedPublicationBlocksInteractionAndRetriesTheSameSelectionTransaction()
        {
            var store=new Store(); var s=CampaignSession.StartAsync(Content(),store).GetAwaiter().GetResult(); long before=s.State.Sequence;
            store.Fail=true; Advance(s); Assert.That(s.SaveError,Is.Not.Null); Assert.That(s.CanInteract,Is.False); Assert.That(s.State.Sequence,Is.EqualTo(before));
            store.Fail=false; s.RetrySaveAsync().GetAwaiter().GetResult(); Assert.That(s.State.Sequence,Is.EqualTo(before+1)); Assert.That(s.CanInteract,Is.True);
        }
        [Test] public void FocusAndModalCancellationPreventFlowDuringReadingOrBackground()
        {
            var s=CampaignSession.StartAsync(Content(),new Store()).GetAwaiter().GetResult(); ReachBrew(s); s.ReleaseHeldInput();
            s.ExecuteBrew(++Event,b=>b.BeginPour("whiskey",ContainerRole.Jigger)); s.SetFocus(false); s.UpdateBrew(1000000,false);
            Assert.That(s.Brew.Container(ContainerRole.Jigger).TotalMl,Is.Zero); Assert.That(s.Brew.FlowActive,Is.False);
            s.SetFocus(true); s.OpenModal("codex"); Assert.That(s.ExecuteBrew(++Event,b=>b.Pour("whiskey",ContainerRole.Serving,10m)),Is.False);
        }
    }
}
