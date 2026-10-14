using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using Surlhwa.Domain;
using Surlhwa.Application;
using Surlhwa.ContentIO;

namespace Surlhwa.Tests
{
    public sealed class CampaignContentTests
    {
        internal static string Json() => File.ReadAllText(Path.Combine(SliceContentTests.ContentRoot(),"Campaign/act1.json"));
        internal static CampaignContent Content() => CampaignContentLoader.Load(Json());
        [Test] public void ActOneAdoptionKeepsUnknownRecipesUnlearnedAndAllFourMethodsReachable()
        {
            var c=Content(); Assert.That(c,Is.Not.Null); var k=c.CreateKnowledge();
            Assert.That(k.KnownReceipts.Keys,Is.EquivalentTo(new[]{"recipe_test_stir"}));
            Assert.That(k.Project("recipe_manwol").Known,Is.False); Assert.That(k.Project("recipe_manwol").Amounts.Count,Is.Zero);
            Assert.That(c.Catalog.Recipes.Values.Select(x=>x.Method).Distinct(),Is.EquivalentTo(new[]{"stir","build","shake","float"}));
            Assert.That(c.Slots.Count,Is.EqualTo(17));
            Assert.That(c.Slots.Select(x=>c.Encounters[x.EncounterId]).Where(x=>x.PrinceVisit.HasValue).Select(x=>x.PrinceVisit),Is.EqualTo(new[]{PrinceVisit.First,PrinceVisit.Warmth,PrinceVisit.Clarity}));
        }
        [TestCase("source_text")][TestCase("unknown_preparation")][TestCase("unknown_field")][TestCase("layer_order")][TestCase("missing_supply")][TestCase("act_two")]
        public void InvalidCampaignContentCannotPublish(string fault)
        {
            var root=JObject.Parse(Json());
            if(fault=="source_text") root["texts"][0]["sourceText"]="changed source";
            if(fault=="unknown_preparation") root["recipes"][0]["preparation"][0]="automatic_win";
            if(fault=="unknown_field") root["unapprovedSystem"]=true;
            if(fault=="layer_order") root["recipes"][5]["layerOrder"][1]="water";
            if(fault=="missing_supply") ((JArray)root["supply"]).RemoveAt(0);
            if(fault=="act_two") root["act"]="act_02";
            Assert.Throws<ContentValidationException>(()=>CampaignContentLoader.Load(root.ToString()));
        }
        [Test] public void ActualSeventeenEncounterCampaignReachesEarnedMemoryThroughNormalDomainActions()
        {
            var c=Content(); Assert.That(c,Is.Not.Null); var store=new ValidatingStore(c); var s=CampaignSession.StartAsync(c,store).GetAwaiter().GetResult();
            string[] recipes={"recipe_test_stir","recipe_test_stir","recipe_test_stir","recipe_dry_martini","recipe_dry_martini","recipe_manwol",
                "recipe_negroni","recipe_test_stir","recipe_negroni","recipe_manhattan","recipe_manhattan","recipe_test_stir","recipe_negroni","recipe_gin_fizz","recipe_dry_martini","recipe_gin_fizz","recipe_dry_martini"};
            for(int n=0;n<17;n++)
            {
                CampaignFlowTests.ReachBrew(s); Assert.That(s.State.SlotIndex,Is.EqualTo(n));
                s.ReleaseHeldInput(); s.ExecuteBrew(++CampaignFlowTests.Event,b=>Make(b,recipes[n])); CampaignFlowTests.Advance(s);
                Assert.That(s.SaveError,Is.Null); Assert.That(s.State.CurrentService.Outcome.Preparation,Is.EqualTo(PreparationResult.Good),s.CurrentEncounter.Id);
                Assert.That(s.State.CurrentService.Outcome.Fits,Is.True,s.CurrentEncounter.Id);
                CampaignFlowTests.ReachDeparture(s); s.TickAsync(2).GetAwaiter().GetResult(); Assert.That(s.SaveError,Is.Null);
            }
            Assert.That(s.State.ActComplete,Is.True); Assert.That(s.State.Memories.Single().Variant,Is.EqualTo("star_glowing"));
            Assert.That(s.State.Services.Count,Is.EqualTo(17)); Assert.That(s.Knowledge.KnownReceipts.Count,Is.EqualTo(6));
            Assert.That(store.Last.Sequence,Is.EqualTo(s.State.Sequence));
        }
        internal static void Make(BrewingSession b,string id)
        {
            if(id=="recipe_manwol") { b.Pour("vodka",ContainerRole.Serving,59.14706m); FloatDomainTests.Layer(b,"coffee_liqueur",29.57353m); FloatDomainTests.Layer(b,"cream",29.57353m); }
            else if(id=="recipe_gin_fizz") { ShakeDomainTests.Fill(b); ShakeDomainTests.Motion(b); b.Transfer(ContainerRole.Shaker,ContainerRole.Serving,85m,BrewAction.PourFromContainer); b.Pour("soda_water",ContainerRole.Serving,15m,BrewAction.Splash); }
            else if(id=="recipe_negroni") { foreach(var ingredient in new[]{"gin","campari","sweet_red_vermouth"}) b.Pour(ingredient,ContainerRole.Serving,30m); b.AddIce(ContainerRole.Serving); b.GentleStir(); b.Garnish("orange_slice","rim"); }
            else
            {
                foreach(var a in BartendingDomainTests.Catalog().Recipes[id].Amounts) b.Pour(a.Key,ContainerRole.Mixing,a.Value,a.Key=="angostura_bitters" ? BrewAction.Dash : BrewAction.Pour);
                b.AddIce(ContainerRole.Mixing); BartendingDomainTests.Circles(b); b.Transfer(ContainerRole.Mixing,ContainerRole.Serving,180m,BrewAction.Strain);
                if(id=="recipe_manhattan") b.Garnish("cherry","in_glass");
            }
        }
        private sealed class ValidatingStore : ICampaignSaveStore
        {
            private readonly CampaignContent content; public CampaignCheckpoint Last;
            internal ValidatingStore(CampaignContent content) { this.content=content; }
            public System.Threading.Tasks.Task SaveAsync(CampaignCheckpoint state)
            { Last=CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(state,content),content); return System.Threading.Tasks.Task.CompletedTask; }
        }
    }
}
