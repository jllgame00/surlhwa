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
    public sealed class ActSixContentTests
    {
        internal static string Json() => File.ReadAllText(Path.Combine(SliceContentTests.ContentRoot(),"Campaign/act6.json"));
        private sealed class Store : ICampaignSaveStore
        {
            private readonly CampaignContent content; public bool Fail;
            public Store(CampaignContent c) { content=c; }
            public Task SaveAsync(CampaignCheckpoint state)
            {
                if(Fail) throw new IOException("ACT6 publication test failure");
                CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(state,content),content); return Task.CompletedTask;
            }
        }
        [Test] public void ActSixAddsPhysicalSupplyBeforeTheLessonWithoutRevealingKnowledge()
        {
            var c=CampaignContentLoader.Load(Json()); var prior=CampaignContentLoader.Load(ActFiveContentTests.Json());
            Assert.That(c.Slots.Count,Is.EqualTo(43)); Assert.That(c.ActAt(36),Is.EqualTo(5)); Assert.That(c.ActAt(37),Is.EqualTo(6)); Assert.That(c.ActAt(42),Is.EqualTo(6));
            Assert.That(c.Slots.Take(37).Select(x=>x.EncounterId),Is.EqualTo(prior.Slots.Select(x=>x.EncounterId)));
            Assert.That(c.AvailableSupplyAt(38),Is.EquivalentTo(prior.AvailableSupplyAt(36))); Assert.That(c.AvailableSupplyAt(39).Count,Is.EqualTo(22));
            foreach(string ingredient in new[]{"green_chartreuse","maraschino_luxardo","fresh_lime_juice"}) {
                Assert.That(c.AvailableSupplyAt(38),Does.Not.Contain(ingredient)); Assert.That(c.AvailableSupplyAt(39),Does.Contain(ingredient));
            }
            Assert.That(c.Catalog.Recipes.Count,Is.EqualTo(9)); var recipe=c.Catalog.Recipes["recipe_last_word"];
            Assert.That(recipe.Amounts.Keys,Is.EquivalentTo(new[]{"gin","green_chartreuse","maraschino_luxardo","fresh_lime_juice"}));
            Assert.That(recipe.Amounts.Values,Is.All.EqualTo(22.5m)); Assert.That(recipe.LateIngredient,Is.Null); Assert.That(recipe.RequiredGarnish,Is.Null);
            Assert.That(recipe.ShakeFinish.ToString(),Is.EqualTo("StrainOnly")); Assert.That(c.RecipePresentation[recipe.Id].Glassware,Is.EqualTo("cocktail"));
            Assert.That(c.Catalog.Recipes["recipe_french_75"].ShakeFinish.ToString(),Is.EqualTo("StrainThenPourAndGentleStir"));
            Assert.That(c.Catalog.Recipes["recipe_gin_fizz"].ShakeFinish.ToString(),Is.EqualTo("PourThenSplash"));
            var knowledge=c.CreateKnowledge().Project(recipe.Id); Assert.That(knowledge.Known,Is.False); Assert.That(knowledge.Name,Is.Null);
            Assert.That(knowledge.Ingredients,Is.Empty); Assert.That(knowledge.Amounts,Is.Empty); Assert.That(knowledge.Method,Is.Null);
            Assert.That(c.KnownSeeds,Is.EqualTo(prior.KnownSeeds));
        }
        [TestCase("missing")][TestCase("order")][TestCase("actor")][TestCase("companion")][TestCase("early_memory")]
        [TestCase("missing_memory")][TestCase("variant")][TestCase("visual")][TestCase("display")][TestCase("night")]
        [TestCase("summary")][TestCase("intro")][TestCase("farewell")][TestCase("response")][TestCase("request")][TestCase("fact")]
        [TestCase("retry_source")][TestCase("early_supply")][TestCase("late_supply")][TestCase("missing_lesson_fact")]
        [TestCase("wrong_lesson_source")][TestCase("wrong_preparation")][TestCase("fallback")][TestCase("scope")]
        public void InvalidActSixOwnershipLearningSupplyAndReadingCannotLoad(string fault)
        {
            var root=JObject.Parse(Json()); var lesson=root["encounters"].Single(x=>x["id"].Value<string>()=="enc_rc1_41");
            var resolve=root["encounters"].Single(x=>x["id"].Value<string>()=="enc_rc1_40");
            var returned=root["encounters"].Single(x=>x["id"].Value<string>()=="enc_rc1_42");
            var guest=root["guests"].Single(x=>x["id"].Value<string>()=="guest_simcheong");
            if(fault=="missing") ((JArray)root["slots"]).RemoveAt(42);
            if(fault=="order") { root["slots"][41]["encounterId"]="enc_rc1_43"; root["slots"][42]["encounterId"]="enc_rc1_42"; }
            if(fault=="actor") returned["guestId"]="guest_faust";
            if(fault=="companion") returned["companionGuestId"]="guest_postman";
            if(fault=="early_memory") { resolve["memoryId"]="mem_06"; resolve["memoryVariant"]="half_submerged_lotus_slow_ripples"; returned["memoryId"]=null; returned["memoryVariant"]=null; }
            if(fault=="missing_memory") { returned["memoryId"]=null; returned["memoryVariant"]=null; }
            if(fault=="variant") returned["memoryVariant"]="reward";
            if(fault=="visual") guest["visualBindingId"]="visual_faust";
            if(fault=="display") guest["displayTextId"]="text_rc1_0060";
            if(fault=="night") resolve["night"]=16;
            if(fault=="summary") returned["summaryTextId"]="text_rc1_0289";
            if(fault=="intro") returned["intro"][0]["textId"]="text_rc1_0285";
            if(fault=="farewell") returned["farewell"][2]="text_rc1_0282";
            if(fault=="response") returned["responses"]["mismatch"]="text_rc1_0294";
            if(fault=="request") returned["request"]["anyTags"]=new JArray("awakening");
            if(fault=="fact") ((JArray)resolve["intro"][0]["facts"]).Add(JObject.Parse("{recipeId:'recipe_last_word',fact:'identity',sourceIds:['SRC-RC1-0285']}"));
            if(fault=="retry_source") lesson["discoveryRetryTextId"]="text_rc1_0039";
            if(fault=="early_supply") root["supplyGrants"].Last["beforeEncounter"]="enc_rc1_39";
            if(fault=="late_supply") root["supplyGrants"].Last["beforeEncounter"]="enc_rc1_41";
            if(fault=="missing_lesson_fact") ((JArray)lesson["intro"][1]["facts"]).RemoveAt(3);
            if(fault=="wrong_lesson_source") lesson["intro"][1]["facts"][0]["sourceIds"]=new JArray("SRC-RC1-0055");
            if(fault=="wrong_preparation") root["recipes"].Last["preparation"]=new JArray("shaker_ice","closed_shake","no_serving_ice");
            if(fault=="fallback") root["slots"][40]["fallbackId"]="enc_rc1_43";
            if(fault=="scope") root["act"]="through_act_07";
            Assert.Throws<ContentValidationException>(()=>CampaignContentLoader.Load(root.ToString()));
        }
        internal static void Make(BrewingSession b,string recipe,CampaignContent c,bool strain=true)
        {
            if(recipe!="recipe_last_word") { ActFourContentTests.Make(b,recipe,c); return; }
            foreach(var amount in c.Catalog.Recipes[recipe].Amounts) b.Pour(amount.Key,ContainerRole.Shaker,amount.Value);
            b.AddIce(ContainerRole.Shaker); ShakeDomainTests.Motion(b);
            b.Transfer(ContainerRole.Shaker,ContainerRole.Serving,90m,strain ? BrewAction.Strain : BrewAction.PourFromContainer);
        }
        private static CampaignSession Reload(CampaignSession s,CampaignContent c,Store store)
        { var checkpoint=CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(s.State,c),c); s.Dispose(); return CampaignSession.Continue(c,store,checkpoint); }
        [TestCase(false,false)][TestCase(true,false)][TestCase(true,true)]
        public void FullRouteReadsRetriesAndGrantsMemoryBeforeFinalOrdinaryRegardlessOfFit(bool finalFits,bool earlyKnown)
        {
            var c=CampaignContentLoader.Load(Json()); var store=new Store(c); var s=CampaignSession.StartAsync(c,store).GetAwaiter().GetResult();
            var route=ActFourContentTests.Route.Concat(new[]{"recipe_dry_martini","recipe_gin_fizz","recipe_manhattan","recipe_negroni",
                "recipe_negroni","recipe_dry_martini",earlyKnown ? "recipe_last_word" : "recipe_manhattan",earlyKnown ? "recipe_negroni" : "recipe_last_word",
                finalFits ? "recipe_last_word" : "recipe_french_75","recipe_dry_martini"}).ToArray();
            string[] priorMemories={"mem_01","mem_02","mem_03","mem_04","mem_05"}; string[] earlierKnowledge=null;
            for(int slot=0;slot<43;slot++) {
                if(slot==40 && !earlyKnown) {
                    for(int guard=0;s.State.Phase!=CampaignPhase.Intro && guard<8;guard++) {
                        if(s.State.Phase==CampaignPhase.Arriving) s.TickAsync(2).GetAwaiter().GetResult(); else CampaignFlowTests.Advance(s);
                    }
                    Assert.That(s.CurrentText.Id,Is.EqualTo("text_rc1_0055")); Assert.That(s.Knowledge.Project("recipe_last_word").Name,Is.Null);
                    CampaignFlowTests.Advance(s); var identity=s.Knowledge.Project("recipe_last_word");
                    Assert.That(identity.Name,Is.EqualTo("라스트 워드")); Assert.That(identity.Amounts,Is.Empty); Assert.That(identity.Method,Is.Null);
                    CampaignFlowTests.Advance(s); var amounts=s.Knowledge.Project("recipe_last_word");
                    Assert.That(amounts.Amounts.Count,Is.EqualTo(4)); Assert.That(amounts.Ingredients.Count,Is.EqualTo(4)); Assert.That(amounts.Amounts.Values,Is.All.EqualTo(22.5m)); Assert.That(amounts.Method,Is.Null);
                    CampaignFlowTests.Advance(s); var preparation=s.Knowledge.Project("recipe_last_word");
                    Assert.That(preparation.Method,Is.EqualTo("Shake")); Assert.That(preparation.PreparationVisible && preparation.GarnishVisible,Is.True); Assert.That(preparation.Known,Is.False);
                }
                CampaignFlowTests.ReachBrew(s); Assert.That(s.State.SlotIndex,Is.EqualTo(slot)); Assert.That(s.SaveError,Is.Null);
                if(slot>=37) {
                    if(slot==37) earlierKnowledge=s.Knowledge.KnownReceipts.Select(x=>x.Key+":"+x.Value).ToArray();
                    Assert.That(s.State.Memories.Select(x=>x.Id),Is.EqualTo(slot==42 ? priorMemories.Concat(new[]{"mem_06"}) : priorMemories));
                    if(slot==39) { Assert.That(s.Supply.Count,Is.EqualTo(22)); Assert.That(s.Knowledge.Project("recipe_last_word").Name,Is.Null); }
                    if(slot==40 && earlyKnown) {
                        Assert.That(s.CurrentEncounter.Id,Is.EqualTo("enc_rc1_41_known")); Assert.That(s.CurrentEncounter.RequiredDiscovery,Is.Null);
                        Assert.That(s.CurrentEncounter.Request.AnyTags,Is.EqualTo(new[]{"bitterness"})); Assert.That(s.CurrentText,Is.Null);
                        Assert.That(s.State.ConsumedLines.Any(x=>x.EndsWith(":text_rc1_0055",StringComparison.Ordinal)),Is.False);
                    }
                    if(s.CurrentEncounter.RequiredDiscovery==null) {
                        int alternatives=0;
                        foreach(string recipe in s.Knowledge.KnownReceipts.Keys) {
                            var brew=new BrewingSession(c.Catalog,c.Tuning,s.Supply); Make(brew,recipe,c);
                            var result=ServiceEvaluator.Evaluate(c.Catalog,c.Tuning,brew.Freeze(),s.CurrentEncounter.Request);
                            if(result.Fits && result.Preparation==PreparationResult.Good && result.QualityGood) alternatives++;
                        }
                        Assert.That(alternatives,Is.GreaterThanOrEqualTo(2),s.CurrentEncounter.Id);
                    }
                    foreach(var line in s.CurrentEncounter.Intro.Select((value,index)=>new{value,index}))
                        Assert.That(s.State.ConsumedLines,Does.Contain(s.State.OccurrenceId+":Intro:"+line.index+":"+line.value.TextId));
                    Assert.That(s.State.ConsumedLines,Does.Contain(s.State.OccurrenceId+":Request:0:"+s.CurrentEncounter.SummaryTextId));
                }
                if(slot==40 && !earlyKnown) {
                    s.ReleaseHeldInput(); s.ExecuteBrew(++CampaignFlowTests.Event,b=>Make(b,"recipe_last_word",c,false)); CampaignFlowTests.Advance(s);
                    var failed=s.State.CurrentService; Assert.That(failed.Outcome.RecipeId,Is.EqualTo("recipe_last_word")); Assert.That(failed.Outcome.Fits,Is.True);
                    Assert.That(failed.Outcome.Preparation,Is.Not.EqualTo(PreparationResult.Good)); Assert.That(s.Knowledge.Project("recipe_last_word").Known,Is.False);
                    for(int guard=0;s.State.Phase!=CampaignPhase.Retry && guard<8;guard++) CampaignFlowTests.Advance(s);
                    Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Retry)); Assert.That(s.CurrentText.Id,Is.EqualTo("text_rc1_0059"));
                    Assert.That(s.State.CompletedOccurrences.Count,Is.EqualTo(40)); s=Reload(s,c,store);
                    Assert.That(s.CurrentText.Id,Is.EqualTo("text_rc1_0059")); CampaignFlowTests.Advance(s);
                    Assert.That(s.State.ConsumedLines,Does.Contain(s.State.OccurrenceId+":Retry:0:text_rc1_0059")); Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Brewing));
                    s=Reload(s,c,store); Assert.That(s.Brew.Container(ContainerRole.Serving).TotalMl,Is.Zero); Assert.That(s.Knowledge.Project("recipe_last_word").Known,Is.False);
                }
                s.ReleaseHeldInput(); s.ExecuteBrew(++CampaignFlowTests.Event,b=>Make(b,route[slot],c)); CampaignFlowTests.Advance(s);
                Assert.That(s.SaveError,Is.Null); Assert.That(s.State.CurrentService.Outcome.Preparation,Is.EqualTo(PreparationResult.Good)); Assert.That(s.State.CurrentService.Outcome.QualityGood,Is.True);
                if(slot==41) Assert.That(s.State.CurrentService.Outcome.Fits,Is.EqualTo(finalFits));
                CampaignFlowTests.ReachDeparture(s);
                if(slot>=37) {
                    foreach(var line in s.CurrentEncounter.Farewell.Select((value,index)=>new{value,index}))
                        Assert.That(s.State.ConsumedLines,Does.Contain(s.State.OccurrenceId+":Farewell:"+line.index+":"+line.value));
                    Assert.That(s.State.ConsumedLines,Does.Contain(s.State.OccurrenceId+":Reaction:0:"+s.CurrentEncounter.Responses[s.State.CurrentService.ResponseKey]));
                    Assert.That(s.Knowledge.KnownReceipts.Where(x=>x.Key!="recipe_last_word").Select(x=>x.Key+":"+x.Value),Is.EqualTo(earlierKnowledge));
                }
                s=Reload(s,c,store);
                if(slot==41) {
                    Assert.That(s.State.Memories.Count,Is.EqualTo(5)); long sequence=s.State.Sequence; var service=s.State.CurrentService;
                    store.Fail=true; s.TickAsync(2).GetAwaiter().GetResult(); Assert.That(s.SaveError,Is.Not.Null);
                    Assert.That(s.State.Sequence,Is.EqualTo(sequence)); Assert.That(s.State.CurrentService,Is.SameAs(service)); Assert.That(s.State.Memories.Count,Is.EqualTo(5));
                    store.Fail=false; s.RetrySaveAsync().GetAwaiter().GetResult();
                    Assert.That(s.State.SlotIndex,Is.EqualTo(42)); Assert.That(s.State.Phase,Is.Not.EqualTo(CampaignPhase.Complete)); Assert.That(s.State.CompletedOccurrences.Count,Is.EqualTo(42));
                    Assert.That(s.State.CurrentService,Is.Null); Assert.That(s.State.Memories.Last().Id,Is.EqualTo("mem_06")); Assert.That(s.State.Memories.Last().OccurrenceId,Is.EqualTo(s.State.RunId+":41"));
                    s=Reload(s,c,store);
                } else s.TickAsync(2).GetAwaiter().GetResult();
                Assert.That(s.SaveError,Is.Null);
            }
            Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Complete)); Assert.That(s.State.Services.Count,Is.EqualTo(earlyKnown ? 43 : 44)); Assert.That(s.State.CompletedOccurrences.Count,Is.EqualTo(43));
            Assert.That(s.State.Memories.Count,Is.EqualTo(6)); Assert.That(s.State.Memories.Last().Variant,Is.EqualTo("half_submerged_lotus_slow_ripples")); Assert.That(s.Knowledge.KnownReceipts.Count,Is.EqualTo(9));
            s=Reload(s,c,store); var complete=CampaignSaveCodec.Encode(s.State,c); s.TickAsync(2).GetAwaiter().GetResult(); CampaignFlowTests.Advance(s);
            Assert.That(CampaignSaveCodec.Encode(s.State,c),Is.EqualTo(complete)); s.Dispose();
        }
    }
}
