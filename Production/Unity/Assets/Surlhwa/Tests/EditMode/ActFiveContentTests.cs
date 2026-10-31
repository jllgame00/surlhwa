using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Domain;

namespace Surlhwa.Tests
{
    public sealed class ActFiveContentTests
    {
        internal static string Json() => File.ReadAllText(Path.Combine(SliceContentTests.ContentRoot(),"Campaign/act5.json"));
        private sealed class Store : ICampaignSaveStore
        {
            private readonly CampaignContent content;
            public bool Fail;
            public Store(CampaignContent c) { content=c; }
            public Task SaveAsync(CampaignCheckpoint state)
            {
                if(Fail) throw new IOException("ACT 5 publication test failure");
                CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(state,content),content); return Task.CompletedTask;
            }
        }
        [Test] public void ActFiveExtendsOnlyNarrativeAndProjectsTheNewBoundary()
        {
            var c=CampaignContentLoader.Load(Json()); var prior=CampaignContentLoader.Load(ActFourContentTests.Json());
            Assert.That(c.Slots.Count,Is.EqualTo(37)); Assert.That(c.ActAt(32),Is.EqualTo(4)); Assert.That(c.ActAt(33),Is.EqualTo(5)); Assert.That(c.ActAt(36),Is.EqualTo(5));
            Assert.That(c.Catalog.Recipes.Keys,Is.EquivalentTo(prior.Catalog.Recipes.Keys)); Assert.That(c.Catalog.Recipes.Count,Is.EqualTo(8));
            Assert.That(c.AvailableSupplyAt(33),Is.EquivalentTo(prior.AvailableSupplyAt(32))); Assert.That(c.AvailableSupplyAt(36).Count,Is.EqualTo(19));
            Assert.That(c.CreateKnowledge().KnownReceipts,Is.EqualTo(prior.CreateKnowledge().KnownReceipts));
            Assert.That(c.Slots.Take(33).Select(x=>x.EncounterId),Is.EqualTo(prior.Slots.Select(x=>x.EncounterId)));
            Assert.That(c.Slots.Skip(33).Select(x=>c.Encounters[x.EncounterId].GuestId),Is.EqualTo(new[]{"guest_faust","guest_station_worker","guest_postman","guest_faust"}));
        }
        [TestCase("missing")][TestCase("order")][TestCase("actor")][TestCase("companion")][TestCase("early_memory")]
        [TestCase("missing_memory")][TestCase("variant")][TestCase("visual")][TestCase("display")][TestCase("night")]
        [TestCase("summary")][TestCase("intro")][TestCase("farewell")][TestCase("response")][TestCase("request")][TestCase("fact")][TestCase("scope")]
        public void InvalidActFiveIdentityReadingOrMemoryOwnershipCannotLoad(string fault)
        {
            var root=JObject.Parse(Json());
            var contract=root["encounters"].Single(x=>x["id"].Value<string>()=="enc_rc1_34");
            var cost=root["encounters"].Single(x=>x["id"].Value<string>()=="enc_rc1_37");
            var guest=root["guests"].Single(x=>x["id"].Value<string>()=="guest_faust");
            if(fault=="missing") ((JArray)root["slots"]).RemoveAt(36);
            if(fault=="order") { root["slots"][33]["encounterId"]="enc_rc1_35"; root["slots"][34]["encounterId"]="enc_rc1_34"; }
            if(fault=="actor") contract["guestId"]="guest_hamlet";
            if(fault=="companion") cost["companionGuestId"]="guest_postman";
            if(fault=="early_memory") { contract["memoryId"]="mem_05"; contract["memoryVariant"]="faint_circle_broken_glass_red_light"; cost["memoryId"]=null; cost["memoryVariant"]=null; }
            if(fault=="missing_memory") { cost["memoryId"]=null; cost["memoryVariant"]=null; }
            if(fault=="variant") cost["memoryVariant"]="rescue";
            if(fault=="visual") guest["visualBindingId"]="visual_hamlet";
            if(fault=="display") guest["displayTextId"]="text_rc1_0082";
            if(fault=="night") contract["night"]=13;
            if(fault=="summary") cost["summaryTextId"]="text_rc1_0070";
            if(fault=="intro") contract["intro"][0]["textId"]="text_rc1_0077";
            if(fault=="farewell") cost["farewell"][2]="text_rc1_0063";
            if(fault=="response") cost["responses"]["mismatch"]="text_rc1_0075";
            if(fault=="request") cost["request"]["anyTags"]=new JArray("awakening");
            if(fault=="fact") ((JArray)contract["intro"][0]["facts"]).Add(JObject.Parse("{recipeId:'recipe_french_75',fact:'identity',sourceIds:['SRC-RC1-0066']}"));
            if(fault=="scope") root["act"]="through_act_06";
            Assert.Throws<ContentValidationException>(()=>CampaignContentLoader.Load(root.ToString()));
        }
        [TestCase(false)][TestCase(true)]
        public void AllThirtySevenDeparturesPreserveLearningAndNaturalFaustMemoryIndependentOfFit(bool finalFits)
        {
            var c=CampaignContentLoader.Load(Json()); var store=new Store(c); var s=CampaignSession.StartAsync(c,store).GetAwaiter().GetResult();
            var route=ActFourContentTests.Route.Concat(new[]{"recipe_dry_martini","recipe_french_75","recipe_test_stir",finalFits ? "recipe_negroni" : "recipe_french_75"}).ToArray();
            string knownBefore=null,factsBefore=null;
            for(int slot=0;slot<37;slot++) {
                CampaignFlowTests.ReachBrew(s); Assert.That(s.State.SlotIndex,Is.EqualTo(slot)); Assert.That(s.SaveError,Is.Null);
                if(slot>=33) {
                    if(slot==33) { knownBefore=JsonConvert.SerializeObject(s.State.KnownReceipts); factsBefore=JsonConvert.SerializeObject(s.State.Facts); }
                    Assert.That(s.State.Memories.Select(x=>x.Id),Is.EqualTo(new[]{"mem_01","mem_02","mem_03","mem_04"}));
                    Assert.That(s.CurrentEncounter.RequiredDiscovery,Is.Null);
                    int alternatives=0;
                    foreach(string recipe in s.Knowledge.KnownReceipts.Keys) {
                        var brew=new BrewingSession(c.Catalog,c.Tuning,s.Supply); ActFourContentTests.Make(brew,recipe,c);
                        var result=ServiceEvaluator.Evaluate(c.Catalog,c.Tuning,brew.Freeze(),s.CurrentEncounter.Request);
                        if(result.Fits && result.Preparation==PreparationResult.Good && result.QualityGood) alternatives++;
                    }
                    Assert.That(alternatives,Is.GreaterThanOrEqualTo(2),s.CurrentEncounter.Id);
                    foreach(var line in s.CurrentEncounter.Intro.Select((value,index)=>new{value,index}))
                        Assert.That(s.State.ConsumedLines,Does.Contain(s.State.OccurrenceId+":Intro:"+line.index+":"+line.value.TextId));
                    Assert.That(s.State.ConsumedLines,Does.Contain(s.State.OccurrenceId+":Request:0:"+s.CurrentEncounter.SummaryTextId));
                }
                s.ReleaseHeldInput(); s.ExecuteBrew(++CampaignFlowTests.Event,b=>ActFourContentTests.Make(b,route[slot],c)); CampaignFlowTests.Advance(s);
                Assert.That(s.SaveError,Is.Null); Assert.That(s.State.CurrentService.Outcome.Preparation,Is.EqualTo(PreparationResult.Good));
                Assert.That(s.State.CurrentService.Outcome.QualityGood,Is.True);
                if(slot==36) Assert.That(s.State.CurrentService.Outcome.Fits,Is.EqualTo(finalFits));
                CampaignFlowTests.ReachDeparture(s);
                if(slot>=33) {
                    foreach(var line in s.CurrentEncounter.Farewell.Select((value,index)=>new{value,index}))
                        Assert.That(s.State.ConsumedLines,Does.Contain(s.State.OccurrenceId+":Farewell:"+line.index+":"+line.value));
                    Assert.That(s.State.ConsumedLines,Does.Contain(s.State.OccurrenceId+":Reaction:0:"+s.CurrentEncounter.Responses[s.State.CurrentService.ResponseKey]));
                    Assert.That(JsonConvert.SerializeObject(s.State.KnownReceipts),Is.EqualTo(knownBefore));
                    Assert.That(JsonConvert.SerializeObject(s.State.Facts),Is.EqualTo(factsBefore));
                    Assert.That(s.State.Memories.Any(x=>x.Id=="mem_05"),Is.False);
                }
                s=CampaignSession.Continue(c,store,CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(s.State,c),c));
                if(slot==36 && !finalFits) {
                    var originalService=s.State.CurrentService; long sequence=s.State.Sequence;
                    store.Fail=true; s.TickAsync(2).GetAwaiter().GetResult(); Assert.That(s.SaveError,Is.Not.Null);
                    Assert.That(s.State.Sequence,Is.EqualTo(sequence)); Assert.That(s.State.CurrentService,Is.SameAs(originalService)); Assert.That(s.State.Memories.Count,Is.EqualTo(4));
                    store.Fail=false; s.RetrySaveAsync().GetAwaiter().GetResult();
                } else s.TickAsync(2).GetAwaiter().GetResult();
                Assert.That(s.SaveError,Is.Null);
            }
            Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Complete)); Assert.That(s.State.Services.Count,Is.EqualTo(37)); Assert.That(s.State.CompletedOccurrences.Count,Is.EqualTo(37));
            Assert.That(s.State.Memories.Select(x=>x.Id),Is.EqualTo(new[]{"mem_01","mem_02","mem_03","mem_04","mem_05"}));
            Assert.That(s.State.Memories.Last().Variant,Is.EqualTo("faint_circle_broken_glass_red_light")); Assert.That(s.State.Memories.Last().OccurrenceId,Is.EqualTo(s.State.RunId+":36"));
            Assert.That(s.Knowledge.KnownReceipts.Count,Is.EqualTo(8));
            s=CampaignSession.Continue(c,store,CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(s.State,c),c));
            long completeSequence=s.State.Sequence; s.TickAsync(2).GetAwaiter().GetResult(); CampaignFlowTests.Advance(s);
            Assert.That(s.State.Sequence,Is.EqualTo(completeSequence)); Assert.That(s.State.Memories.Count,Is.EqualTo(5));
        }
    }
}
