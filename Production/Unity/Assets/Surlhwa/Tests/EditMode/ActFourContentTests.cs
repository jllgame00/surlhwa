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
    public sealed class ActFourContentTests
    {
        internal static string Json() => File.ReadAllText(Path.Combine(SliceContentTests.ContentRoot(),"Campaign/act4.json"));
        private sealed class Store : ICampaignSaveStore
        {
            private readonly CampaignContent content;
            public bool Fail;
            public Store(CampaignContent c) { content=c; }
            public Task SaveAsync(CampaignCheckpoint state)
            {
                if(Fail) throw new IOException("ACT 4 publication test failure");
                CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(state,content),content);
                return Task.CompletedTask;
            }
        }
        [Test] public void ActFourSuppliesPhysicalIngredientsWithoutKnowledgeAndProjectsSeparateFinish()
        {
            var c=CampaignContentLoader.Load(Json());
            Assert.That(c.Slots.Count,Is.EqualTo(33)); Assert.That(c.ActAt(28),Is.EqualTo(3)); Assert.That(c.ActAt(29),Is.EqualTo(4));
            Assert.That(c.AvailableSupplyAt(28),Does.Not.Contain("champagne"));
            Assert.That(c.AvailableSupplyAt(29),Does.Contain("champagne"));
            Assert.That(c.AvailableSupplyAt(29),Does.Contain("fresh_lemon_juice"));
            Assert.That(c.AvailableSupplyAt(29),Does.Contain("lemon_juice"));
            Assert.That(c.CreateKnowledge().Project("recipe_french_75").Known,Is.False);
            Assert.That(c.CreateKnowledge().Project("recipe_french_75").Name,Is.Null);
            Assert.That(c.RecipePresentation["recipe_french_75"].Glassware,Is.EqualTo("flute"));
            var property=typeof(StirRecipe).GetProperty("ShakeFinish"); Assert.That(property,Is.Not.Null);
            Assert.That(property.GetValue(c.Catalog.Recipes["recipe_french_75"]).ToString(),Is.EqualTo("StrainThenPourAndGentleStir"));
            Assert.That(property.GetValue(c.Catalog.Recipes["recipe_gin_fizz"]).ToString(),Is.EqualTo("PourThenSplash"));
        }
        [TestCase("missing")][TestCase("actor")][TestCase("companion")][TestCase("early_memory")]
        [TestCase("missing_memory")][TestCase("variant")][TestCase("visual")][TestCase("retry_source")]
        [TestCase("retry_not_discovery")][TestCase("early_supply")][TestCase("missing_lesson_fact")]
        [TestCase("wrong_lesson_source")][TestCase("wrong_preparation")]
        public void InvalidActFourSourceOrLearningOwnershipCannotLoad(string fault)
        {
            var root=JObject.Parse(Json());
            var lesson=root["encounters"].Single(x=>x["id"].Value<string>()=="enc_rc1_30");
            var question=root["encounters"].Single(x=>x["id"].Value<string>()=="enc_rc1_31");
            var stillness=root["encounters"].Single(x=>x["id"].Value<string>()=="enc_rc1_33");
            if(fault=="missing") ((JArray)root["slots"]).RemoveAt(32);
            if(fault=="actor") question["guestId"]="guest_quixote";
            if(fault=="companion") question["companionGuestId"]="guest_postman";
            if(fault=="early_memory") { question["memoryId"]="mem_04"; question["memoryVariant"]="stage_skull_shadow"; stillness["memoryId"]=null; stillness["memoryVariant"]=null; }
            if(fault=="missing_memory") { stillness["memoryId"]=null; stillness["memoryVariant"]=null; }
            if(fault=="variant") stillness["memoryVariant"]="global_darkness";
            if(fault=="visual") root["guests"].Single(x=>x["id"].Value<string>()=="guest_hamlet")["visualBindingId"]="visual_quixote";
            if(fault=="retry_source") lesson["discoveryRetryTextId"]="text_rc1_0035";
            if(fault=="retry_not_discovery") question["discoveryRetryTextId"]="text_rc1_0039";
            if(fault=="early_supply") root["supplyGrants"][1]["beforeEncounter"]="enc_rc1_29";
            if(fault=="missing_lesson_fact") ((JArray)lesson["intro"][2]["facts"]).RemoveAt(0);
            if(fault=="wrong_lesson_source") lesson["intro"][2]["facts"][0]["sourceIds"][0]="SRC-RC1-0035";
            if(fault=="wrong_preparation") root["recipes"].Last()["preparation"][3]="late_soda_splash";
            Assert.Throws<ContentValidationException>(()=>CampaignContentLoader.Load(root.ToString()));
        }
        internal static void Make(BrewingSession b,string recipe,CampaignContent c,bool finish=true)
        {
            if(recipe=="recipe_french_75") {
                b.Pour("gin",ContainerRole.Shaker,30m); b.Pour("fresh_lemon_juice",ContainerRole.Shaker,15m); b.Pour("simple_syrup",ContainerRole.Shaker,15m);
                b.AddIce(ContainerRole.Shaker); ShakeDomainTests.Motion(b);
                b.Transfer(ContainerRole.Shaker,ContainerRole.Serving,60m,BrewAction.Strain);
                b.Pour("champagne",ContainerRole.Serving,60m); if(finish) b.GentleStir();
            } else if(recipe=="recipe_hanky_panky") {
                foreach(var a in c.Catalog.Recipes[recipe].Amounts) b.Pour(a.Key,ContainerRole.Mixing,a.Value);
                b.AddIce(ContainerRole.Mixing); BartendingDomainTests.Circles(b);
                b.Transfer(ContainerRole.Mixing,ContainerRole.Serving,180m,BrewAction.Strain); b.Garnish("orange_zest","rim");
            } else CampaignContentTests.Make(b,recipe);
        }
        internal static readonly string[] Route={"recipe_test_stir","recipe_test_stir","recipe_test_stir","recipe_dry_martini","recipe_dry_martini","recipe_manwol",
            "recipe_negroni","recipe_test_stir","recipe_negroni","recipe_manhattan","recipe_manhattan","recipe_test_stir","recipe_negroni",
            "recipe_gin_fizz","recipe_dry_martini","recipe_gin_fizz","recipe_dry_martini","recipe_test_stir","recipe_manhattan",
            "recipe_dry_martini","recipe_negroni","recipe_hanky_panky","recipe_manhattan","recipe_gin_fizz","recipe_dry_martini",
            "recipe_test_stir","recipe_gin_fizz","recipe_negroni","recipe_negroni","recipe_french_75","recipe_french_75","recipe_dry_martini","recipe_french_75"};
        [Test] public void ActualCampaignReadsFrenchFactsRetriesMissingFinishAndAwardsFailedHamletMemoryNaturally()
        {
            var c=CampaignContentLoader.Load(Json()); var store=new Store(c); var s=CampaignSession.StartAsync(c,store).GetAwaiter().GetResult();
            for(int slot=0;slot<33;slot++) {
                if(slot==29) {
                    for(int n=0;n<8 && s.State.Phase!=CampaignPhase.Intro;n++)
                        if(s.State.Phase==CampaignPhase.Arriving) s.TickAsync(2).GetAwaiter().GetResult(); else CampaignFlowTests.Advance(s);
                    Assert.That(s.Knowledge.Project("recipe_french_75").Name,Is.Null);
                    CampaignFlowTests.Advance(s);
                    Assert.That(s.Knowledge.Project("recipe_french_75").Name,Is.EqualTo("프렌치 75"));
                    Assert.That(s.Knowledge.Project("recipe_french_75").Amounts.Count,Is.Zero);
                    CampaignFlowTests.Advance(s);
                    Assert.That(s.Knowledge.Project("recipe_french_75").Amounts.Count,Is.EqualTo(3));
                    Assert.That(s.Knowledge.Project("recipe_french_75").Ingredients,Does.Not.Contain("champagne"));
                    Assert.That(s.Knowledge.Project("recipe_french_75").PreparationVisible,Is.False);
                    s=CampaignSession.Continue(c,store,CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(s.State,c),c));
                    CampaignFlowTests.Advance(s);
                    Assert.That(s.Knowledge.Project("recipe_french_75").Amounts["champagne"],Is.EqualTo(60m));
                    Assert.That(s.Knowledge.Project("recipe_french_75").PreparationVisible,Is.True);
                    Assert.That(s.Knowledge.Project("recipe_french_75").GarnishVisible,Is.True);
                    Assert.That(s.Knowledge.Project("recipe_french_75").Known,Is.False);
                }
                CampaignFlowTests.ReachBrew(s); Assert.That(s.State.SlotIndex,Is.EqualTo(slot));
                if(slot==29) {
                    s.ReleaseHeldInput(); s.ExecuteBrew(++CampaignFlowTests.Event,b=>Make(b,"recipe_french_75",c,false)); CampaignFlowTests.Advance(s);
                    Assert.That(s.State.CurrentService.Outcome.RecipeId,Is.EqualTo("recipe_french_75"));
                    Assert.That(s.State.CurrentService.Outcome.Preparation,Is.Not.EqualTo(PreparationResult.Good));
                    for(int n=0;n<4 && s.State.Phase!=CampaignPhase.Retry;n++) CampaignFlowTests.Advance(s);
                    Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Retry)); Assert.That(s.CurrentText.Id,Is.EqualTo("text_rc1_0039"));
                    Assert.That(s.State.ConsumedLines,Does.Not.Contain(s.State.OccurrenceId+":Retry:0:text_rc1_0039"));
                    s=CampaignSession.Continue(c,store,CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(s.State,c),c));
                    CampaignFlowTests.Advance(s);
                    Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Brewing)); Assert.That(s.State.SlotIndex,Is.EqualTo(29));
                    Assert.That(s.State.ConsumedLines,Does.Contain(s.State.OccurrenceId+":Retry:0:text_rc1_0039"));
                    Assert.That(s.Knowledge.Project("recipe_french_75").Known,Is.False);
                }
                if(slot>=30) {
                    int reachable=0;
                    foreach(string recipe in s.Knowledge.KnownReceipts.Keys) {
                        var brew=new BrewingSession(c.Catalog,c.Tuning,s.Supply); Make(brew,recipe,c);
                        var result=ServiceEvaluator.Evaluate(c.Catalog,c.Tuning,brew.Freeze(),s.CurrentEncounter.Request);
                        if(result.Fits && result.Preparation==PreparationResult.Good && result.QualityGood) reachable++;
                    }
                    Assert.That(reachable,Is.GreaterThanOrEqualTo(2),s.CurrentEncounter.Id);
                    Assert.That(s.State.Memories.Any(x=>x.Id=="mem_03"),Is.True);
                }
                s.ReleaseHeldInput(); s.ExecuteBrew(++CampaignFlowTests.Event,b=>Make(b,Route[slot],c)); CampaignFlowTests.Advance(s);
                Assert.That(s.SaveError,Is.Null); Assert.That(s.State.CurrentService.Outcome.Preparation,Is.EqualTo(PreparationResult.Good));
                if(slot==29) Assert.That(s.State.CurrentService.Discovered,Is.True);
                if(slot==30 || slot==32) Assert.That(s.State.CurrentService.Outcome.Fits,Is.False);
                CampaignFlowTests.ReachDeparture(s);
                Assert.That(s.State.Memories.Any(x=>x.Id=="mem_04"),Is.False);
                if(slot==32) {
                    s=CampaignSession.Continue(c,store,CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(s.State,c),c));
                    store.Fail=true; s.TickAsync(2).GetAwaiter().GetResult(); Assert.That(s.SaveError,Is.Not.Null);
                    Assert.That(s.State.Memories.Any(x=>x.Id=="mem_04"),Is.False);
                    store.Fail=false; s.RetrySaveAsync().GetAwaiter().GetResult();
                } else s.TickAsync(2).GetAwaiter().GetResult();
                Assert.That(s.SaveError,Is.Null);
                s=CampaignSession.Continue(c,store,CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(s.State,c),c));
            }
            Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Complete)); Assert.That(s.State.CompletedOccurrences.Count,Is.EqualTo(33));
            Assert.That(s.State.Services.Count,Is.EqualTo(34)); Assert.That(s.Knowledge.KnownReceipts.Count,Is.EqualTo(8));
            Assert.That(s.State.Memories.Select(x=>x.Id),Is.EqualTo(new[]{"mem_01","mem_02","mem_03","mem_04"}));
            Assert.That(s.State.Memories.Last().Variant,Is.EqualTo("stage_skull_shadow"));
            Assert.That(s.State.Memories.Last().OccurrenceId,Is.EqualTo(s.State.RunId+":32"));
        }
    }
}
