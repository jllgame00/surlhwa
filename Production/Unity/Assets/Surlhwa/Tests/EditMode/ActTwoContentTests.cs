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
    public sealed class ActTwoContentTests
    {
        private static string Json() => File.ReadAllText(Path.Combine(SliceContentTests.ContentRoot(),"Campaign/act2.json"));
        private static void Make(BrewingSession b,string recipe,CampaignContent c)
        {
            if(recipe==null) b.Pour("water",ContainerRole.Serving,1m);
            else if(recipe=="recipe_hanky_panky") {
                foreach(var a in c.Catalog.Recipes[recipe].Amounts) b.Pour(a.Key,ContainerRole.Mixing,a.Value);
                b.AddIce(ContainerRole.Mixing); BartendingDomainTests.Circles(b);
                b.Transfer(ContainerRole.Mixing,ContainerRole.Serving,180m,BrewAction.Strain); b.Garnish("orange_zest","rim");
            } else CampaignContentTests.Make(b,recipe);
        }
        [TestCase("late_supply")][TestCase("duplicate_supply")][TestCase("pair")][TestCase("memory")]
        [TestCase("missing_beat")][TestCase("garnish")][TestCase("unknown_field")][TestCase("known_supply")]
        public void InvalidExpandedContentCannotPublish(string fault)
        {
            var root=JObject.Parse(Json());
            var reunion=root["encounters"].Single(x=>x["id"].Value<string>()=="enc_rc1_25");
            if(fault=="late_supply") root["supplyGrants"][0]["beforeEncounter"]="enc_rc1_25";
            if(fault=="duplicate_supply") ((JArray)root["supplyGrants"]).Add(root["supplyGrants"][0].DeepClone());
            if(fault=="pair") reunion["companionGuestId"]="guest_chunhyang";
            if(fault=="memory") reunion["memoryVariant"]="two_trophies";
            if(fault=="missing_beat") ((JArray)root["slots"]).RemoveAt(20);
            if(fault=="garnish") root["recipes"].Last["garnishPosition"]="in_glass";
            if(fault=="unknown_field") root["inventedReward"]=true;
            if(fault=="known_supply") ((JArray)root["supplyGrants"][0]["ingredientIds"]).Add("whiskey");
            Assert.Throws<ContentValidationException>(()=>CampaignContentLoader.Load(root.ToString()));
        }
        private sealed class Store : ICampaignSaveStore
        {
            private readonly CampaignContent content;
            public Store(CampaignContent c) { content=c; }
            public Task SaveAsync(CampaignCheckpoint s)
            { CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(s,content),content); return Task.CompletedTask; }
        }
        [Test] public void AuthoredActTwoRetainsActOneAndReachesPairedMemoryWithScopedSupply()
        {
            var c=CampaignContentLoader.Load(Json());
            var store=new Store(c); var s=CampaignSession.StartAsync(c,store).GetAwaiter().GetResult();
            string[] plan={"recipe_test_stir","recipe_test_stir","recipe_test_stir","recipe_dry_martini","recipe_dry_martini","recipe_manwol",
                "recipe_negroni","recipe_test_stir","recipe_negroni","recipe_manhattan","recipe_manhattan","recipe_test_stir","recipe_negroni",
                "recipe_gin_fizz","recipe_dry_martini","recipe_gin_fizz","recipe_dry_martini", "recipe_test_stir","recipe_manhattan",
                "recipe_dry_martini","recipe_negroni","recipe_hanky_panky","recipe_manhattan","recipe_gin_fizz",null};
            for(int slot=0;slot<25;slot++)
            {
                CampaignFlowTests.ReachBrew(s); Assert.That(s.State.SlotIndex,Is.EqualTo(slot));
                Assert.That(s.Supply.Contains("london_dry_gin"),Is.EqualTo(slot>=21));
                Assert.That(s.Supply.Contains("fernet"),Is.EqualTo(slot>=21));
                if(slot>=17 && s.CurrentEncounter.Request.CocktailId==null) {
                    int reachable=0;
                    foreach(var recipe in s.Knowledge.KnownReceipts.Keys) {
                        var alternative=new BrewingSession(c.Catalog,c.Tuning,s.Supply); Make(alternative,recipe,c);
                        var outcome=ServiceEvaluator.Evaluate(c.Catalog,c.Tuning,alternative.Freeze(),s.CurrentEncounter.Request);
                        if(outcome.Fits && outcome.Preparation==PreparationResult.Good && outcome.QualityGood) reachable++;
                    }
                    Assert.That(reachable,Is.GreaterThanOrEqualTo(2),"Two earned, supplied, actually prepared choices: "+s.CurrentEncounter.Id);
                }
                if(slot==21) {
                    Assert.That(s.Knowledge.Project("recipe_hanky_panky").Known,Is.False);
                    Assert.That(s.Knowledge.Project("recipe_hanky_panky").Amounts["fernet"],Is.EqualTo(7.5m));
                    CampaignFlowTests.Serve(s,null);
                    for(int i=0;i<8 && s.State.Phase!=CampaignPhase.Retry;i++) CampaignFlowTests.Advance(s);
                    Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Retry)); CampaignFlowTests.Advance(s);
                    Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Brewing));
                }
                s.ReleaseHeldInput(); s.ExecuteBrew(++CampaignFlowTests.Event,b=>Make(b,plan[slot],c));
                CampaignFlowTests.Advance(s); Assert.That(s.SaveError,Is.Null);
                if(plan[slot]!=null) {
                    Assert.That(s.State.CurrentService.Outcome.Fits,Is.True,s.CurrentEncounter.Id);
                    Assert.That(s.State.CurrentService.Outcome.Preparation,Is.EqualTo(PreparationResult.Good));
                }
                CampaignFlowTests.ReachDeparture(s); s.TickAsync(2).GetAwaiter().GetResult(); Assert.That(s.SaveError,Is.Null);
                s=CampaignSession.Continue(c,store,CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(s.State,c),c));
            }
            Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Complete));
            Assert.That(s.State.Services.Count,Is.EqualTo(26)); Assert.That(s.State.CompletedOccurrences.Count,Is.EqualTo(25));
            Assert.That(s.State.Memories.Select(x=>x.Id),Is.EqualTo(new[]{"mem_01","mem_02"}));
            Assert.That(s.Knowledge.KnownReceipts.Count,Is.EqualTo(7));
        }
    }
}
