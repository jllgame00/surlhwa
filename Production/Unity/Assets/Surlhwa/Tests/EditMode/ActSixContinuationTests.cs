using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Domain;

namespace Surlhwa.Tests
{
    public sealed class ActSixContinuationTests
    {
        private static string Json(int act)=>File.ReadAllText(Path.Combine(SliceContentTests.ContentRoot(),"Campaign/act"+act+".json"));
        private static string Folder()=>Path.GetFullPath(Path.Combine(SliceContentTests.ContentRoot(),"../Validation/Evidence/act6-continuation-tests",Guid.NewGuid().ToString("N")));
        private static readonly Dictionary<int,List<CampaignCheckpoint>> Sources=new Dictionary<int,List<CampaignCheckpoint>>();
        private sealed class CaptureStore : ICampaignSaveStore
        {
            private readonly CampaignContent content;
            internal readonly List<CampaignCheckpoint> States=new List<CampaignCheckpoint>();
            internal bool Fail;
            internal CaptureStore(CampaignContent validate=null) { content=validate; }
            public Task SaveAsync(CampaignCheckpoint state)
            {
                if(Fail) throw new IOException("injected checkpoint publication failure");
                States.Add(content==null ? state : CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(state,content),content));
                return Task.CompletedTask;
            }
        }
        private static readonly string[] EarlierPlan={null,"recipe_test_stir","recipe_test_stir","recipe_dry_martini","recipe_dry_martini","recipe_manwol",
            "recipe_negroni","recipe_test_stir","recipe_negroni","recipe_manhattan",null,"recipe_test_stir","recipe_negroni",
            "recipe_gin_fizz","recipe_dry_martini","recipe_gin_fizz","recipe_dry_martini","recipe_test_stir","recipe_manhattan",
            "recipe_dry_martini","recipe_negroni","recipe_hanky_panky","recipe_manhattan","recipe_gin_fizz",null,
            "recipe_test_stir",null,null,"recipe_negroni","recipe_french_75",null,"recipe_negroni",null,
            "recipe_french_75","recipe_dry_martini","recipe_test_stir","recipe_french_75"};
        private static void Serve(CampaignSession s,string recipe,bool finish=true)
        {
            s.ReleaseHeldInput(); Assert.That(s.ExecuteBrew(++CampaignFlowTests.Event,b=> {
                if(recipe==null) b.Pour("water",ContainerRole.Serving,1m);
                else if(recipe=="recipe_hanky_panky") {
                    foreach(var a in s.Content.Catalog.Recipes[recipe].Amounts) b.Pour(a.Key,ContainerRole.Mixing,a.Value);
                    b.AddIce(ContainerRole.Mixing); BartendingDomainTests.Circles(b);
                    b.Transfer(ContainerRole.Mixing,ContainerRole.Serving,180m,BrewAction.Strain); b.Garnish("orange_zest","rim");
                } else if(recipe=="recipe_french_75") {
                    French75PreparationTests.Fill(b); ShakeDomainTests.Motion(b);
                    b.Transfer(ContainerRole.Shaker,ContainerRole.Serving,60m,BrewAction.Strain);
                    b.Pour("champagne",ContainerRole.Serving,60m); if(finish) b.GentleStir();
                } else if(recipe=="recipe_last_word") {
                    foreach(var a in s.Content.Catalog.Recipes[recipe].Amounts) b.Pour(a.Key,ContainerRole.Shaker,a.Value);
                    b.AddIce(ContainerRole.Shaker); ShakeDomainTests.Motion(b);
                    b.Transfer(ContainerRole.Shaker,ContainerRole.Serving,90m,finish ? BrewAction.Strain : BrewAction.PourFromContainer);
                } else CampaignContentTests.Make(b,recipe);
            }),Is.True);
            CampaignFlowTests.Advance(s); Assert.That(s.SaveError,Is.Null);
        }
        private static List<CampaignCheckpoint> Checkpoints(int act)
        {
            if(Sources.TryGetValue(act,out var cached)) return cached;
            var c=CampaignContentLoader.Load(Json(act)); var store=new CaptureStore();
            using(var s=CampaignSession.StartAsync(c,store).GetAwaiter().GetResult()) {
                for(int slot=0;slot<c.Slots.Count;slot++) {
                    CampaignFlowTests.ReachBrew(s); Assert.That(s.State.SlotIndex,Is.EqualTo(slot));
                    if(slot==6 || slot==21 || slot==29) {
                        Serve(s,slot==29 ? "recipe_french_75" : null,false);
                        for(int step=0;step<8 && s.State.Phase!=CampaignPhase.Retry;step++) CampaignFlowTests.Advance(s);
                        Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Retry));
                        CampaignFlowTests.Advance(s); Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Brewing));
                    }
                    Serve(s,EarlierPlan[slot]); CampaignFlowTests.ReachDeparture(s);
                    s.TickAsync(2).GetAwaiter().GetResult(); Assert.That(s.SaveError,Is.Null);
                }
                Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Complete));
            }
            Sources.Add(act,store.States); return store.States;
        }
        private static JObject Payload(CampaignCheckpoint state,CampaignContent c)=>(JObject)JObject.Parse(CampaignSaveCodec.Encode(state,c))["payload"];
        private static void Preserved(CampaignCheckpoint before,CampaignCheckpoint after,CampaignContinuation continuation)
        {
            var expected=Payload(before,continuation.SourceContent);
            if(before.Phase==CampaignPhase.Complete) {
                int next=continuation.SourceContent.Slots.Count;
                expected["slotIndex"]=next; expected["phase"]="NightEnd"; expected["lineIndex"]=0;
                ((JArray)expected["selectedEncounters"]).Add(continuation.ExpandedContent.Slots[next].EncounterId);
            }
            Assert.That(after.RunId,Is.EqualTo(before.RunId));
            Assert.That(after.Sequence,Is.EqualTo(before.Sequence+(before.Phase==CampaignPhase.Complete ? 1 : 0)));
            Assert.That(JToken.DeepEquals(expected,Payload(after,continuation.ExpandedContent)),Is.True,
                "Original result/reading/knowledge/memory changed at "+before.SlotIndex+" / "+before.Phase+" / "+before.LineIndex);
        }
        private static CampaignSaveStore WriteSource(string folder,int act,params CampaignCheckpoint[] states)
        {
            var store=new CampaignSaveStore(folder,CampaignContentLoader.Load(Json(act)));
            foreach(var state in states) Task.Run(()=>store.SaveAsync(state)).GetAwaiter().GetResult(); return store;
        }
        [TestCase(1,17)][TestCase(2,25)][TestCase(3,29)][TestCase(4,33)][TestCase(5,37)]
        public void ActSixContinuationPreservesEveryAcceptedSourcePhaseAndItsReceipts(int act,int nextSlot)
        {
            var continuation=new CampaignContinuation(Json(act),Json(6)); var states=Checkpoints(act);
            var selected=states.GroupBy(x=>x.Phase).Select(x=>x.Last()).Concat(states.Where(x=>x.SlotIndex>=29)).Distinct().ToArray();
            Assert.That(selected.Select(x=>x.Phase).Distinct(),Is.EquivalentTo(Enum.GetValues(typeof(CampaignPhase)).Cast<CampaignPhase>()));
            foreach(var before in selected) {
                var after=continuation.Convert(CampaignSaveCodec.Encode(before,continuation.SourceContent)); Preserved(before,after,continuation);
                using(var resumed=CampaignSession.Continue(continuation.ExpandedContent,new CaptureStore(),after)) {
                    if(before.Phase==CampaignPhase.Brewing) Assert.That(resumed.Brew.Container(ContainerRole.Serving).TotalMl,Is.Zero);
                    if(before.Phase==CampaignPhase.Reaction) {
                        Assert.That(resumed.Brew,Is.Null);
                        Assert.That(ServiceEvidenceCodec.Write(resumed.State.CurrentService),Is.EqualTo(ServiceEvidenceCodec.Write(before.CurrentService)));
                    }
                    if(before.SlotIndex==29 && before.Phase==CampaignPhase.Retry)
                        Assert.That(resumed.CurrentText.Id,Is.EqualTo("text_rc1_0039"));
                }
            }
            var complete=continuation.Convert(CampaignSaveCodec.Encode(states.Last(),continuation.SourceContent));
            Assert.That(complete.SlotIndex,Is.EqualTo(nextSlot)); Assert.That(complete.Phase,Is.EqualTo(CampaignPhase.NightEnd));
            Assert.That(complete.Memories.Count,Is.EqualTo(act)); Assert.That(complete.CurrentService,Is.Null);
            if(act==5) {
                Assert.That(complete.SelectedEncounters.Last(),Is.EqualTo("enc_rc1_38"));
                Assert.That(complete.KnownReceipts.ContainsKey("recipe_french_75"),Is.True);
                Assert.That(complete.KnownReceipts.ContainsKey("recipe_last_word"),Is.False);
                Assert.That(complete.ConsumedLines,Does.Contain(complete.RunId+":29:Retry:0:text_rc1_0039"));
            }
        }
        [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
        public void ActSixImportRetainsBothSourceGenerationsAndPublishesOnlyOnce(int act)
        {
            var c=new CampaignContinuation(Json(act),Json(6)); var states=Checkpoints(act); string folder=Folder();
            var source=WriteSource(folder,act,states[states.Count-2],states.Last()); var original=File.ReadAllBytes(source.CurrentPath); var previous=File.ReadAllBytes(source.PreviousPath);
            var target=new CampaignSaveStore(Folder(),c.ExpandedContent);
            var imported=Task.Run(()=>c.LoadOrImportAsync(folder,target)).GetAwaiter().GetResult(); Preserved(states.Last(),imported.Checkpoint,c);
            var published=File.ReadAllBytes(target.CurrentPath); target.Fault=point=> { throw new IOException("must not republish"); };
            var again=Task.Run(()=>c.LoadOrImportAsync(folder,target)).GetAwaiter().GetResult();
            Assert.That(again.Checkpoint.Sequence,Is.EqualTo(imported.Checkpoint.Sequence)); Assert.That(File.ReadAllBytes(target.CurrentPath),Is.EqualTo(published));
            Assert.That(File.Exists(target.PreviousPath),Is.False); Assert.That(File.ReadAllBytes(source.CurrentPath),Is.EqualTo(original)); Assert.That(File.ReadAllBytes(source.PreviousPath),Is.EqualTo(previous));
        }
        [Test] public void PendingFaustDepartureCompletesOnceBeforeTheFirstSimCheongNight()
        {
            var c=new CampaignContinuation(Json(5),Json(6)); var before=Checkpoints(5).Last(x=>x.Phase==CampaignPhase.Departing);
            Assert.That(before.SlotIndex,Is.EqualTo(36)); Assert.That(before.Memories.Count,Is.EqualTo(4));
            var converted=c.Convert(CampaignSaveCodec.Encode(before,c.SourceContent)); Preserved(before,converted,c);
            using(var s=CampaignSession.Continue(c.ExpandedContent,new CaptureStore(c.ExpandedContent),converted)) {
                Assert.That(s.Brew,Is.Null); s.TickAsync(2).GetAwaiter().GetResult(); Assert.That(s.SaveError,Is.Null);
                Assert.That(s.State.SlotIndex,Is.EqualTo(37)); Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.NightEnd));
                Assert.That(s.State.SelectedEncounters.Last(),Is.EqualTo("enc_rc1_38"));
                Assert.That(s.State.Services.Select(ServiceEvidenceCodec.Write),Is.EqualTo(before.Services.Select(ServiceEvidenceCodec.Write)));
                Assert.That(s.State.Memories.Count,Is.EqualTo(5)); Assert.That(s.State.Memories.Last().Id,Is.EqualTo("mem_05"));
                long sequence=s.State.Sequence; s.TickAsync(2).GetAwaiter().GetResult(); Assert.That(s.State.Sequence,Is.EqualTo(sequence));
            }
        }
        [TestCase("before_write")][TestCase("after_write")][TestCase("after_flush")][TestCase("after_validate")][TestCase("after_replace")][TestCase("after_final_read")]
        public void ActFiveToSixImportRecoversEveryPublicationFaultWithoutRewritingHistory(string fault)
        {
            var c=new CampaignContinuation(Json(5),Json(6)); var states=Checkpoints(5); string folder=Folder();
            var source=WriteSource(folder,5,states[states.Count-2],states.Last()); var original=File.ReadAllBytes(source.CurrentPath); var previous=File.ReadAllBytes(source.PreviousPath);
            var target=new CampaignSaveStore(Folder(),c.ExpandedContent); target.Fault=point=> { if(point==fault) throw new IOException("injected "+point); };
            Assert.Throws<IOException>(()=>Task.Run(()=>c.LoadOrImportAsync(folder,target)).GetAwaiter().GetResult());
            target.Fault=null; var recovered=Task.Run(()=>c.LoadOrImportAsync(folder,target)).GetAwaiter().GetResult(); Preserved(states.Last(),recovered.Checkpoint,c);
            Assert.That(File.ReadAllBytes(source.CurrentPath),Is.EqualTo(original)); Assert.That(File.ReadAllBytes(source.PreviousPath),Is.EqualTo(previous));
            Assert.That(File.Exists(target.PreviousPath),Is.False); Assert.That(Directory.GetFiles(Path.GetDirectoryName(target.CurrentPath),"pending-*.json"),Is.Empty);
        }
        [TestCase("source_bytes")][TestCase("old_label")][TestCase("old_text")][TestCase("old_recipe")][TestCase("old_retry")][TestCase("old_memory")][TestCase("old_supply")]
        public void ActSixRejectsUnacceptedBytesAndChangesToTheActFivePrefix(string fault)
        {
            string source=Json(5); var target=JObject.Parse(Json(6));
            if(fault=="source_bytes") source+=" ";
            if(fault=="old_label") {
                target["ingredients"][0]["label"]="changed earlier ingredient presentation";
                Assert.DoesNotThrow(()=>CampaignContentLoader.Load(target.ToString()));
            }
            if(fault=="old_text") target["texts"][0]["value"]="changed earlier authored text";
            if(fault=="old_recipe") target["recipes"][0]["amounts"][0]["ml"]=31m;
            if(fault=="old_retry") target["encounters"].Single(x=>x["id"].Value<string>()=="enc_rc1_30")["discoveryRetryTextId"]=null;
            if(fault=="old_memory") target["encounters"].Single(x=>x["id"].Value<string>()=="enc_rc1_37")["memoryVariant"]="changed_faust";
            if(fault=="old_supply") target["supplyGrants"].Single(x=>x["beforeEncounter"].Value<string>()=="enc_rc1_30")["beforeEncounter"]="enc_rc1_31";
            Assert.Throws<ContentValidationException>(()=>new CampaignContinuation(source,target.ToString()));
        }
        [TestCase(5,5)][TestCase(5,4)][TestCase(6,6)][TestCase(6,5)]
        public void ActSixAdditionCannotAuthorizeSameBackwardOrUnacceptedSource(int source,int target)
        { Assert.Throws<ContentValidationException>(()=>new CampaignContinuation(Json(source),Json(target))); }
        [TestCase("current.json",false)][TestCase("previous.json",false)][TestCase("current.json",true)][TestCase("previous.json",true)]
        public void ExistingActSixHistoryPreventsImportEvenWhenInvalid(string generation,bool future)
        {
            var c=new CampaignContinuation(Json(5),Json(6)); string sourceFolder=Folder(); WriteSource(sourceFolder,5,Checkpoints(5).Last());
            string folder=Folder(); Directory.CreateDirectory(folder); string path=Path.Combine(folder,generation),data=future ? "{\"schemaVersion\":999}" : "existing invalid ACT6";
            File.WriteAllText(path,data); var target=new CampaignSaveStore(folder,c.ExpandedContent);
            if(future) Assert.Throws<UnsupportedSaveVersionException>(()=>Task.Run(()=>c.LoadOrImportAsync(sourceFolder,target,true)).GetAwaiter().GetResult());
            else Assert.Throws<ContentValidationException>(()=>Task.Run(()=>c.LoadOrImportAsync(sourceFolder,target,true)).GetAwaiter().GetResult());
            Assert.That(File.ReadAllText(path),Is.EqualTo(data)); Assert.That(Directory.GetFiles(folder).Length,Is.EqualTo(1));
        }
        [TestCase(false)][TestCase(true)]
        public void ActFiveCorruptionRequiresRecoveryAndFutureVersionCannotRollBack(bool future)
        {
            var c=new CampaignContinuation(Json(5),Json(6)); var states=Checkpoints(5); string folder=Folder();
            var source=WriteSource(folder,5,states[states.Count-2],states.Last()); string data=future ? "{\"schemaVersion\":999}" : "corrupt ACT5";
            File.WriteAllText(source.CurrentPath,data); var previous=File.ReadAllBytes(source.PreviousPath); var target=new CampaignSaveStore(Folder(),c.ExpandedContent);
            if(future) Assert.Throws<UnsupportedSaveVersionException>(()=>Task.Run(()=>c.LoadOrImportAsync(folder,target,true)).GetAwaiter().GetResult());
            else {
                var preview=Task.Run(()=>c.LoadOrImportAsync(folder,target)).GetAwaiter().GetResult(); Assert.That(preview.RecoveryRequired,Is.True);
                Assert.That(preview.Checkpoint.Phase,Is.EqualTo(CampaignPhase.Departing)); Assert.That(target.HasHistory,Is.False);
                var recovered=Task.Run(()=>c.LoadOrImportAsync(folder,target,true)).GetAwaiter().GetResult(); Preserved(states[states.Count-2],recovered.Checkpoint,c);
            }
            if(future) Assert.That(target.HasHistory,Is.False);
            Assert.That(File.ReadAllText(source.CurrentPath),Is.EqualTo(data)); Assert.That(File.ReadAllBytes(source.PreviousPath),Is.EqualTo(previous));
        }
        [Test] public void ExistingActSixCheckpointWinsBeforeReadingAFutureActFiveSource()
        {
            var c=new CampaignContinuation(Json(5),Json(6)); string sourceFolder=Folder(); Directory.CreateDirectory(sourceFolder);
            string path=Path.Combine(sourceFolder,"current.json"); File.WriteAllText(path,"{\"schemaVersion\":999}");
            var checkpoint=c.Convert(CampaignSaveCodec.Encode(Checkpoints(5).Last(),c.SourceContent)); var target=new CampaignSaveStore(Folder(),c.ExpandedContent);
            Task.Run(()=>target.ImportAsync(checkpoint)).GetAwaiter().GetResult(); var bytes=File.ReadAllBytes(target.CurrentPath);
            var loaded=Task.Run(()=>c.LoadOrImportAsync(sourceFolder,target)).GetAwaiter().GetResult(); Assert.That(loaded.Checkpoint.Sequence,Is.EqualTo(checkpoint.Sequence));
            Assert.That(File.ReadAllBytes(target.CurrentPath),Is.EqualTo(bytes)); Assert.That(File.ReadAllText(path),Is.EqualTo("{\"schemaVersion\":999}"));
        }
        [Test] public void ActFiveContinuationReadsLastWordRetryAndEarnsMemoryBeforeFinalOrdinaryCompletion()
        {
            var c=new CampaignContinuation(Json(5),Json(6)); var before=Checkpoints(5).Last(); var store=new CaptureStore(c.ExpandedContent);
            var s=CampaignSession.Continue(c.ExpandedContent,store,c.Convert(CampaignSaveCodec.Encode(before,c.SourceContent)));
            string failedLastWord=null;
            var plan=new[]{"recipe_negroni","recipe_dry_martini","recipe_manhattan","recipe_last_word","recipe_french_75","recipe_dry_martini"};
            for(int slot=37;slot<43;slot++) {
                CampaignFlowTests.ReachBrew(s); Assert.That(s.State.SlotIndex,Is.EqualTo(slot));
                if(slot==39) {
                    Assert.That(s.Supply,Does.Contain("fresh_lime_juice")); Assert.That(s.Knowledge.Project("recipe_last_word").Known,Is.False);
                    Assert.That(s.Knowledge.Project("recipe_last_word").Name,Is.Null);
                }
                if(slot==40) {
                    Serve(s,"recipe_last_word",false);
                    Assert.That(s.State.CurrentService.Outcome.Preparation,Is.EqualTo(PreparationResult.Weak));
                    Assert.That(s.State.CurrentService.Outcome.RecipeId,Is.EqualTo("recipe_last_word"));
                    Assert.That(s.State.CurrentService.Outcome.QualityGood,Is.True); Assert.That(s.State.CurrentService.Outcome.Fits,Is.True);
                    var failed=ServiceEvidenceCodec.Write(s.State.CurrentService); var facts=Payload(s.State,c.ExpandedContent)["facts"].DeepClone();
                    failedLastWord=failed;
                    for(int step=0;step<8 && s.State.Phase!=CampaignPhase.Retry;step++) CampaignFlowTests.Advance(s);
                    Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Retry));
                    string retry=s.State.OccurrenceId+":Retry:0:text_rc1_0059";
                    Assert.That(s.CurrentText.Id,Is.EqualTo("text_rc1_0059")); Assert.That(s.State.ConsumedLines,Does.Not.Contain(retry));
                    var restoredRetry=CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(s.State,c.ExpandedContent),c.ExpandedContent);
                    s.Dispose(); s=CampaignSession.Continue(c.ExpandedContent,store,restoredRetry);
                    Assert.That(s.Brew,Is.Null); Assert.That(s.CurrentText.Id,Is.EqualTo("text_rc1_0059"));
                    Assert.That(ServiceEvidenceCodec.Write(s.State.CurrentService),Is.EqualTo(failed));
                    store.Fail=true; CampaignFlowTests.Advance(s); Assert.That(s.SaveError,Is.Not.Null);
                    Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Retry)); Assert.That(s.State.ConsumedLines,Does.Not.Contain(retry));
                    store.Fail=false; Assert.That(s.RetrySaveAsync().GetAwaiter().GetResult(),Is.True);
                    Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Brewing)); Assert.That(s.State.ConsumedLines.Count(x=>x==retry),Is.EqualTo(1));
                    var brewing=CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(s.State,c.ExpandedContent),c.ExpandedContent);
                    s.Dispose(); s=CampaignSession.Continue(c.ExpandedContent,store,brewing);
                    Assert.That(s.Brew.Container(ContainerRole.Shaker).TotalMl,Is.Zero); Assert.That(s.Brew.Container(ContainerRole.Serving).TotalMl,Is.Zero);
                    Assert.That(ServiceEvidenceCodec.Write(s.State.CurrentService),Is.EqualTo(failed));
                    Assert.That(JToken.DeepEquals(facts,Payload(s.State,c.ExpandedContent)["facts"]),Is.True);
                    Assert.That(s.Knowledge.Project("recipe_last_word").Known,Is.False);
                }
                Serve(s,plan[slot-37]); Assert.That(s.State.CurrentService.Outcome.Preparation,Is.EqualTo(PreparationResult.Good));
                Assert.That(s.State.CurrentService.Outcome.QualityGood,Is.True);
                if(slot==41) Assert.That(s.State.CurrentService.Outcome.Fits,Is.False);
                CampaignFlowTests.ReachDeparture(s);
                Assert.That(s.State.Memories.Any(x=>x.Id=="mem_06"),Is.EqualTo(slot==42));
                var restored=CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(s.State,c.ExpandedContent),c.ExpandedContent);
                s.Dispose(); s=CampaignSession.Continue(c.ExpandedContent,store,restored);
                if(slot==41) {
                    store.Fail=true; long sequence=s.State.Sequence; s.TickAsync(2).GetAwaiter().GetResult();
                    Assert.That(s.SaveError,Is.Not.Null); Assert.That(s.State.Sequence,Is.EqualTo(sequence)); Assert.That(s.State.Memories.Count,Is.EqualTo(5));
                    store.Fail=false; Assert.That(s.RetrySaveAsync().GetAwaiter().GetResult(),Is.True);
                    Assert.That(s.State.SlotIndex,Is.EqualTo(42)); Assert.That(s.State.Phase,Is.Not.EqualTo(CampaignPhase.Complete));
                    Assert.That(s.State.Memories.Count,Is.EqualTo(6));
                } else s.TickAsync(2).GetAwaiter().GetResult();
                Assert.That(s.SaveError,Is.Null);
            }
            var after=s.State; Assert.That(after.Phase,Is.EqualTo(CampaignPhase.Complete)); Assert.That(after.CompletedOccurrences.Count,Is.EqualTo(43));
            Assert.That(after.Memories.Select(x=>x.Id),Is.EqualTo(new[]{"mem_01","mem_02","mem_03","mem_04","mem_05","mem_06"}));
            Assert.That(after.Memories.Last().Variant,Is.EqualTo("half_submerged_lotus_slow_ripples")); Assert.That(after.Memories.Last().OccurrenceId,Is.EqualTo(after.RunId+":41"));
            Assert.That(after.Services.Take(before.Services.Count).Select(ServiceEvidenceCodec.Write),Is.EqualTo(before.Services.Select(ServiceEvidenceCodec.Write)));
            Assert.That(after.ConsumedLines.Take(before.ConsumedLines.Count),Is.EqualTo(before.ConsumedLines));
            var oldPayload=Payload(before,c.SourceContent); var newPayload=Payload(after,c.ExpandedContent);
            foreach(string key in new[]{"princeChoice","farewell"}) Assert.That(JToken.DeepEquals(oldPayload[key],newPayload[key]),Is.True,key);
            foreach(string key in new[]{"knownReceipts","facts"}) foreach(var fact in ((JObject)oldPayload[key]).Properties())
                Assert.That(JToken.DeepEquals(fact.Value,newPayload[key][fact.Name]),Is.True,key+" / "+fact.Name);
            Assert.That(after.KnownReceipts.ContainsKey("recipe_last_word"),Is.True);
            Assert.That(after.Services.Count(x=>ServiceEvidenceCodec.Write(x)==failedLastWord),Is.EqualTo(1));
            Assert.That(JToken.DeepEquals(oldPayload["memories"],new JArray(((JArray)newPayload["memories"]).Take(5))),Is.True);
            var json=CampaignSaveCodec.Encode(after,c.ExpandedContent); var envelope=JObject.Parse(json);
            Assert.That(envelope["schemaVersion"].Value<int>(),Is.EqualTo(1)); Assert.That(envelope["act"].Value<string>(),Is.EqualTo("through_act_06"));
            s.Dispose(); using(s=CampaignSession.Continue(c.ExpandedContent,store,CampaignSaveCodec.Decode(json,c.ExpandedContent))) {
                CampaignFlowTests.Advance(s); s.TickAsync(2).GetAwaiter().GetResult(); Assert.That(s.State.Sequence,Is.EqualTo(after.Sequence));
                Assert.That(CampaignSaveCodec.Encode(s.State,c.ExpandedContent),Is.EqualTo(json));
            }
        }
    }
}
