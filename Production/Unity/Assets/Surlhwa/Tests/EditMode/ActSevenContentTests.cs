using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Domain;

namespace Surlhwa.Tests
{
    public sealed class ActSevenContentTests
    {
        internal static string Json() => File.ReadAllText(Path.Combine(SliceContentTests.ContentRoot(),"Campaign/act7.json"));
        [Test] public void EndingCopiesItsInputsAndResolvesOnlyTheSavedChoice()
        {
            var lines=Enumerable.Range(1,8).Select(n=>"text_rc1_"+n.ToString("0000")).ToArray();
            var recalls=new Dictionary<string,string>{{"keep","text_rc1_0412"},{"seek","text_rc1_0413"},{"none","text_rc1_0414"}};
            var ending=new CampaignEnding(lines,1,recalls,"text_rc1_0008");
            lines[0]="changed"; recalls["keep"]="changed";
            foreach(string choice in new[]{"keep","seek","none",null}) {
                var resolved=ending.ResolveLines(choice);
                Assert.That(resolved.Count,Is.EqualTo(9)); Assert.That(resolved[2],Is.EqualTo(ending.RecallByChoice[choice ?? "none"]));
                Assert.That(resolved.Where((_,i)=>i!=2),Is.EqualTo(ending.Lines));
            }
            Assert.That(ending.Lines[0],Is.EqualTo("text_rc1_0001")); Assert.That(ending.RecallByChoice["keep"],Is.EqualTo("text_rc1_0412"));
            Assert.That(ending.CodaTextId,Is.EqualTo("text_rc1_0008")); Assert.Throws<ArgumentException>(()=>ending.ResolveLines("invented"));
            Assert.Throws<NotSupportedException>(()=>((IList<string>)ending.Lines)[0]="changed");
            Assert.Throws<NotSupportedException>(()=>((IDictionary<string,string>)ending.RecallByChoice)["keep"]="changed");
        }
        [Test] public void ActSevenPreservesTheAcceptedPrefixAndAddsOneBoundedEnding()
        {
            var c=CampaignContentLoader.Load(Json()); var prior=CampaignContentLoader.Load(ActSixContentTests.Json());
            Assert.That(c.Slots.Count,Is.EqualTo(47)); Assert.That(c.ActAt(42),Is.EqualTo(6)); Assert.That(c.ActAt(43),Is.EqualTo(7)); Assert.That(c.ActAt(46),Is.EqualTo(7));
            Assert.That(c.Slots.Take(43).Select(x=>x.EncounterId),Is.EqualTo(prior.Slots.Select(x=>x.EncounterId)));
            Assert.That(c.Catalog.Recipes.Count,Is.EqualTo(9)); Assert.That(c.Supply.Count,Is.EqualTo(22)); Assert.That(c.KnownSeeds,Is.EqualTo(prior.KnownSeeds));
            Assert.That(c.Ending,Is.Not.Null); Assert.That(prior.Ending,Is.Null);
            Assert.That(c.Ending.RecallAfterLine,Is.EqualTo(1)); Assert.That(c.Ending.RecallByChoice,Is.EqualTo(c.PrinceRecalls));
            Assert.That(c.Ending.ResolveLines("keep"),Is.EqualTo(new[]{"text_rc1_0001","text_rc1_0002","text_rc1_0412","text_rc1_0003","text_rc1_0004","text_rc1_0005","text_rc1_0006","text_rc1_0007","text_rc1_0008"}));
            foreach(string id in new[]{"text_rc1_0232","text_rc1_0240"}) Assert.That(c.Texts[id].Role,Is.EqualTo("narration"));
            foreach(string id in new[]{"text_rc1_0233","text_rc1_0241"}) Assert.That(c.Texts[id].Role,Is.EqualTo("mixed"));
        }
        [TestCase("missing")][TestCase("order")][TestCase("actor")][TestCase("companion")][TestCase("early_memory")]
        [TestCase("missing_memory")][TestCase("variant")][TestCase("visual")][TestCase("display")][TestCase("night")]
        [TestCase("summary")][TestCase("intro")][TestCase("farewell")][TestCase("response")][TestCase("request")][TestCase("fact")]
        [TestCase("conditional")][TestCase("missing_ending")][TestCase("ending_order")][TestCase("recall_position")]
        [TestCase("recall_choice")][TestCase("coda")][TestCase("source_id")][TestCase("source_path")][TestCase("source_field")]
        [TestCase("source_order")][TestCase("source_role")][TestCase("edited_value")][TestCase("mixed_role")][TestCase("recall_source")]
        public void InvalidActSevenOwnershipAndEndingCannotLoad(string fault)
        {
            var root=JObject.Parse(Json());
            var returned=root["encounters"].Single(x=>x["id"].Value<string>()=="enc_rc1_45");
            var stay=root["encounters"].Single(x=>x["id"].Value<string>()=="enc_rc1_47");
            var guest=root["guests"].Single(x=>x["id"].Value<string>()=="guest_odysseus");
            var source=root["texts"].Single(x=>x["id"].Value<string>()=="text_rc1_0001");
            if(fault=="missing") ((JArray)root["slots"]).RemoveAt(46);
            if(fault=="order") { root["slots"][43]["encounterId"]="enc_rc1_45"; root["slots"][44]["encounterId"]="enc_rc1_44"; }
            if(fault=="actor") stay["guestId"]="guest_faust";
            if(fault=="companion") stay["companionGuestId"]="guest_postman";
            if(fault=="early_memory") { returned["memoryId"]="mem_07"; returned["memoryVariant"]="anchor_ship_fragment"; stay["memoryId"]=null; stay["memoryVariant"]=null; }
            if(fault=="missing_memory") { stay["memoryId"]=null; stay["memoryVariant"]=null; }
            if(fault=="variant") stay["memoryVariant"]="completed_ship";
            if(fault=="visual") guest["visualBindingId"]="visual_faust";
            if(fault=="display") guest["displayTextId"]="text_rc1_0060";
            if(fault=="night") returned["night"]=20;
            if(fault=="summary") stay["summaryTextId"]="text_rc1_0146";
            if(fault=="intro") stay["intro"][0]["textId"]="text_rc1_0142";
            if(fault=="farewell") stay["farewell"][0]="text_rc1_0137";
            if(fault=="response") stay["responses"]["mismatch"]="text_rc1_0152";
            if(fault=="request") stay["request"]["anyTags"]=new JArray("awakening");
            if(fault=="fact") ((JArray)returned["intro"][0]["facts"]).Add(JObject.Parse("{recipeId:'recipe_last_word',fact:'identity',sourceIds:['SRC-RC1-0142']}"));
            if(fault=="conditional") { root["slots"][46]["fallbackId"]="enc_rc1_43"; root["slots"][46]["whenKnownRecipe"]="recipe_last_word"; }
            if(fault=="missing_ending") root.Remove("ending");
            if(fault=="ending_order") { root["ending"]["lines"][0]="text_rc1_0002"; root["ending"]["lines"][1]="text_rc1_0001"; }
            if(fault=="recall_position") root["ending"]["recallAfterLine"]=0;
            if(fault=="recall_choice") { root["ending"]["recallByChoice"]["keep"]="text_rc1_0413"; root["ending"]["recallByChoice"]["seek"]="text_rc1_0412"; }
            if(fault=="coda") root["ending"]["codaTextId"]="text_rc1_0007";
            if(fault=="source_id") source["sourceId"]="SRC-RC1-0002";
            if(fault=="source_path") source["sourcePath"]="57_ODYSSEUS.md";
            if(fault=="source_field") source["sourceField"]="onSuccessLines[0]";
            if(fault=="source_order") source["sourceOrder"]="1";
            if(fault=="source_role") source["role"]="spoken";
            if(fault=="edited_value") source["value"]="Invented victory.";
            if(fault=="mixed_role") root["texts"].Single(x=>x["id"].Value<string>()=="text_rc1_0241")["role"]="spoken";
            if(fault=="recall_source") root["texts"].Single(x=>x["id"].Value<string>()=="text_rc1_0412")["sourceField"]="seekTheStarClosingRecall";
            Assert.Throws<ContentValidationException>(()=>CampaignContentLoader.Load(root.ToString()));
        }
        [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)]
        public void EarlierScopesRemainWithoutAnEnding(int act)
        {
            string json=File.ReadAllText(Path.Combine(SliceContentTests.ContentRoot(),"Campaign/act"+act+".json"));
            Assert.That(CampaignContentLoader.Load(json).Ending,Is.Null);
            var earlier=JObject.Parse(json); earlier["ending"]=JObject.Parse(Json())["ending"];
            Assert.Throws<ContentValidationException>(()=>CampaignContentLoader.Load(earlier.ToString()));
        }
        [TestCase(false)][TestCase(true)]
        public void ActualRouteKeepsFourRecommendationsReachableAndFinalFailureStillLeavesMemory(bool quiet)
        {
            var c=CampaignContentLoader.Load(Json()); var history=CampaignEndingTests.History(7,quiet ? "seek" : "keep",quiet);
            var before=history.Single(x=>x.SlotIndex==43 && x.Phase==CampaignPhase.Brewing);
            for(int slot=43;slot<47;slot++) {
                var state=history.Single(x=>x.SlotIndex==slot && x.Phase==CampaignPhase.Brewing);
                using(var s=CampaignSession.Continue(c,new CampaignEndingTests.CaptureStore(),CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(state,c),c))) {
                    Assert.That(s.Knowledge.KnownReceipts.Count,Is.EqualTo(9)); Assert.That(s.Supply.Count,Is.EqualTo(22));
                    var alternatives=new List<string>();
                    foreach(string recipe in s.Knowledge.KnownReceipts.Keys) {
                        var brew=new BrewingSession(c.Catalog,c.Tuning,s.Supply); ActSixContentTests.Make(brew,recipe,c);
                        var result=ServiceEvaluator.Evaluate(c.Catalog,c.Tuning,brew.Freeze(),s.CurrentEncounter.Request);
                        if(result.Fits && result.Preparation==PreparationResult.Good && result.QualityGood) alternatives.Add(recipe);
                    }
                    Assert.That(alternatives.Count,Is.GreaterThanOrEqualTo(2),s.CurrentEncounter.Id);
                    Console.WriteLine("ACT7 actual-route slot "+slot+" known/supplied fits: "+string.Join(",",alternatives));
                    var departed=history.Last(x=>x.SlotIndex==slot && x.Phase==CampaignPhase.Departing);
                    foreach(var line in s.CurrentEncounter.Intro.Select((value,index)=>new{value,index}))
                        Assert.That(departed.ConsumedLines,Does.Contain(state.OccurrenceId+":Intro:"+line.index+":"+line.value.TextId));
                    foreach(var line in s.CurrentEncounter.Farewell.Select((value,index)=>new{value,index}))
                        Assert.That(departed.ConsumedLines,Does.Contain(state.OccurrenceId+":Farewell:"+line.index+":"+line.value));
                    Assert.That(departed.Memories.Any(x=>x.Id=="mem_07"),Is.False);
                }
            }
            var complete=history.Last();
            Assert.That(complete.KnownReceipts,Is.EqualTo(before.KnownReceipts)); Assert.That(complete.Facts,Is.EqualTo(before.Facts));
            Assert.That(complete.Services.Last().Outcome.Fits,Is.EqualTo(!quiet));
            Assert.That(complete.Memories.Count(x=>x.Id=="mem_07"),Is.EqualTo(1));
            Assert.That(complete.Memories.Last().OccurrenceId,Is.EqualTo(complete.RunId+":46"));
            Assert.That(complete.Memories.Last().Variant,Is.EqualTo("anchor_ship_fragment"));
        }
    }
}
