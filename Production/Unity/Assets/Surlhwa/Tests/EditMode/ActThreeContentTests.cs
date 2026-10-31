using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Domain;

namespace Surlhwa.Tests
{
    public sealed class ActThreeContentTests
    {
        private static string Json() => File.ReadAllText(Path.Combine(SliceContentTests.ContentRoot(),"Campaign/act3.json"));
        private sealed class Store : ICampaignSaveStore
        {
            private readonly CampaignContent content;
            public bool Fail;
            public Store(CampaignContent c) { content=c; }
            public Task SaveAsync(CampaignCheckpoint state)
            {
                if(Fail) throw new IOException("ACT 3 publication test failure");
                CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(state,content),content);
                return Task.CompletedTask;
            }
        }
        [TestCase("missing")][TestCase("order")][TestCase("actor")][TestCase("companion")]
        [TestCase("early_memory")][TestCase("missing_memory")][TestCase("variant")][TestCase("visual")][TestCase("scope")]
        public void InvalidActThreeSourceOwnershipCannotLoad(string fault)
        {
            var root=JObject.Parse(Json());
            var declaration=root["encounters"].Single(x=>x["id"].Value<string>()=="enc_rc1_27");
            var alone=root["encounters"].Single(x=>x["id"].Value<string>()=="enc_rc1_28");
            if(fault=="missing") ((JArray)root["slots"]).RemoveAt(28);
            if(fault=="order") { root["slots"][25]["encounterId"]="enc_rc1_27"; root["slots"][26]["encounterId"]="enc_rc1_26"; }
            if(fault=="actor") declaration["guestId"]="guest_little_prince";
            if(fault=="companion") alone["companionGuestId"]="guest_postman";
            if(fault=="early_memory") { declaration["memoryId"]="mem_03"; declaration["memoryVariant"]="windmill_fragment"; alone["memoryId"]=null; alone["memoryVariant"]=null; }
            if(fault=="missing_memory") { alone["memoryId"]=null; alone["memoryVariant"]=null; }
            if(fault=="variant") alone["memoryVariant"]="sancho_flag";
            if(fault=="visual") root["guests"].Single(x=>x["id"].Value<string>()=="guest_quixote")["visualBindingId"]="visual_prince";
            if(fault=="scope") root["act"]="through_act_04";
            Assert.Throws<ContentValidationException>(()=>CampaignContentLoader.Load(root.ToString()));
        }
        [Test] public void AuthoredActNumberIsProjectedByContentAtValidatedBoundaries()
        {
            var method=typeof(CampaignContent).GetMethod("ActAt");
            Assert.That(method,Is.Not.Null,"UI must consume the content ACT projection.");
            var c=CampaignContentLoader.Load(Json());
            Assert.That(Enumerable.Range(0,29).Select(i=>(int)method.Invoke(c,new object[]{i})),
                Is.EqualTo(Enumerable.Repeat(1,17).Concat(Enumerable.Repeat(2,8)).Concat(Enumerable.Repeat(3,4))));
            var fixture=CampaignFlowTests.Content();
            Assert.That((int)method.Invoke(fixture,new object[]{2}),Is.EqualTo(1));
        }
        private static void Make(BrewingSession b,string recipe,CampaignContent c)
        {
            if(recipe==null) b.Pour("water",ContainerRole.Serving,1m);
            else if(recipe=="recipe_hanky_panky") {
                foreach(var a in c.Catalog.Recipes[recipe].Amounts) b.Pour(a.Key,ContainerRole.Mixing,a.Value);
                b.AddIce(ContainerRole.Mixing); BartendingDomainTests.Circles(b);
                b.Transfer(ContainerRole.Mixing,ContainerRole.Serving,180m,BrewAction.Strain); b.Garnish("orange_zest","rim");
            } else CampaignContentTests.Make(b,recipe);
        }
        [Test] public void FullCampaignPreservesChoicesAndFailedQuixoteDepartureAwardsOneMemoryBeforeOrdinaryGuest()
        {
            var c=CampaignContentLoader.Load(Json()); var store=new Store(c);
            var s=CampaignSession.StartAsync(c,store).GetAwaiter().GetResult();
            string[] plan={"recipe_test_stir","recipe_test_stir","recipe_test_stir","recipe_dry_martini","recipe_dry_martini","recipe_manwol",
                "recipe_negroni","recipe_test_stir","recipe_negroni","recipe_manhattan","recipe_manhattan","recipe_test_stir","recipe_negroni",
                "recipe_gin_fizz","recipe_dry_martini","recipe_gin_fizz","recipe_dry_martini","recipe_test_stir","recipe_manhattan",
                "recipe_dry_martini","recipe_negroni","recipe_hanky_panky","recipe_manhattan","recipe_gin_fizz","recipe_dry_martini",
                "recipe_test_stir",null,null,"recipe_negroni"};
            for(int slot=0;slot<29;slot++)
            {
                CampaignFlowTests.ReachBrew(s); Assert.That(s.State.SlotIndex,Is.EqualTo(slot));
                if(slot>=25) {
                    Assert.That(s.State.Memories.Any(x=>x.Id=="mem_02"),Is.True,"The postman notices the existing swing.");
                    Assert.That(s.State.Memories.Any(x=>x.Id=="mem_03"),Is.EqualTo(slot==28));
                    int reachable=0;
                    foreach(string recipe in s.Knowledge.KnownReceipts.Keys) {
                        var brew=new BrewingSession(c.Catalog,c.Tuning,s.Supply); Make(brew,recipe,c);
                        var result=ServiceEvaluator.Evaluate(c.Catalog,c.Tuning,brew.Freeze(),s.CurrentEncounter.Request);
                        if(result.Fits && result.Preparation==PreparationResult.Good && result.QualityGood) reachable++;
                    }
                    Assert.That(reachable,Is.GreaterThanOrEqualTo(2),s.CurrentEncounter.Id);
                }
                s.ReleaseHeldInput(); s.ExecuteBrew(++CampaignFlowTests.Event,b=>Make(b,plan[slot],c)); CampaignFlowTests.Advance(s);
                Assert.That(s.SaveError,Is.Null);
                if(slot==26 || slot==27) Assert.That(s.State.CurrentService.Outcome.Fits,Is.False);
                CampaignFlowTests.ReachDeparture(s);
                if(slot==27) {
                    Assert.That(s.State.Memories.Any(x=>x.Id=="mem_03"),Is.False);
                    // Crash before departure resumes that same committed failed service.
                    s=CampaignSession.Continue(c,store,CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(s.State,c),c));
                    long sequence=s.State.Sequence; store.Fail=true; s.TickAsync(2).GetAwaiter().GetResult();
                    Assert.That(s.SaveError,Is.Not.Null); Assert.That(s.State.Sequence,Is.EqualTo(sequence));
                    Assert.That(s.State.Memories.Any(x=>x.Id=="mem_03"),Is.False);
                    store.Fail=false; s.RetrySaveAsync().GetAwaiter().GetResult();
                    Assert.That(s.State.Memories.Count(x=>x.Id=="mem_03"),Is.EqualTo(1));
                    Assert.That(s.State.SlotIndex,Is.EqualTo(28));
                } else s.TickAsync(2).GetAwaiter().GetResult();
                Assert.That(s.SaveError,Is.Null);
                s=CampaignSession.Continue(c,store,CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(s.State,c),c));
            }
            Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Complete));
            Assert.That(s.State.Services.Count,Is.EqualTo(29)); Assert.That(s.State.CompletedOccurrences.Count,Is.EqualTo(29));
            Assert.That(s.State.Memories.Select(x=>x.Id),Is.EqualTo(new[]{"mem_01","mem_02","mem_03"}));
            Assert.That(s.State.Memories.Last().Variant,Is.EqualTo("windmill_fragment"));
            Assert.That(s.State.Memories.Last().OccurrenceId,Is.EqualTo(s.State.RunId+":27"));
            Assert.That(s.Knowledge.KnownReceipts.Count,Is.EqualTo(7));
            s.TickAsync(2).GetAwaiter().GetResult(); Assert.That(s.State.Memories.Count,Is.EqualTo(3));
        }
    }
}
