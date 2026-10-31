using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Domain;

namespace Surlhwa.Tests
{
    public sealed class ActFourContinuationTests
    {
        private static string Json(int act) => File.ReadAllText(Path.Combine(SliceContentTests.ContentRoot(),"Campaign/act"+act+".json"));
        private static string Folder() => Path.GetFullPath(Path.Combine(SliceContentTests.ContentRoot(),"../Validation/Evidence/act4-continuation-tests",Guid.NewGuid().ToString("N")));
        private static readonly Dictionary<int,List<CampaignCheckpoint>> SourceStates=new Dictionary<int,List<CampaignCheckpoint>>();
        private sealed class CaptureStore : ICampaignSaveStore
        {
            internal readonly List<CampaignCheckpoint> States=new List<CampaignCheckpoint>();
            public Task SaveAsync(CampaignCheckpoint checkpoint) { States.Add(checkpoint); return Task.CompletedTask; }
        }
        private static readonly string[] Plan={null,"recipe_test_stir","recipe_test_stir","recipe_dry_martini","recipe_dry_martini","recipe_manwol",
            "recipe_negroni","recipe_test_stir","recipe_negroni","recipe_manhattan",null,"recipe_test_stir","recipe_negroni",
            "recipe_gin_fizz","recipe_dry_martini","recipe_gin_fizz","recipe_dry_martini","recipe_test_stir","recipe_manhattan",
            "recipe_dry_martini","recipe_negroni","recipe_hanky_panky","recipe_manhattan","recipe_gin_fizz",null,
            "recipe_test_stir",null,null,"recipe_negroni","recipe_french_75",null,"recipe_negroni",null};
        private static void Serve(CampaignSession session,string recipe,bool omitFinish=false)
        {
            session.ReleaseHeldInput();
            Assert.That(session.ExecuteBrew(++CampaignFlowTests.Event,b=> {
                if(recipe==null) b.Pour("water",ContainerRole.Serving,1m);
                else if(recipe=="recipe_hanky_panky") {
                    foreach(var a in session.Content.Catalog.Recipes[recipe].Amounts) b.Pour(a.Key,ContainerRole.Mixing,a.Value);
                    b.AddIce(ContainerRole.Mixing); BartendingDomainTests.Circles(b);
                    b.Transfer(ContainerRole.Mixing,ContainerRole.Serving,180m,BrewAction.Strain); b.Garnish("orange_zest","rim");
                }
                else if(recipe=="recipe_french_75") {
                    b.Pour("gin",ContainerRole.Shaker,30m); b.Pour("fresh_lemon_juice",ContainerRole.Shaker,15m);
                    b.Pour("simple_syrup",ContainerRole.Shaker,15m); b.AddIce(ContainerRole.Shaker); ShakeDomainTests.Motion(b);
                    b.Transfer(ContainerRole.Shaker,ContainerRole.Serving,60m,BrewAction.Strain);
                    b.Pour("champagne",ContainerRole.Serving,60m); if(!omitFinish) b.GentleStir();
                }
                else CampaignContentTests.Make(b,recipe);
            }),Is.True);
            CampaignFlowTests.Advance(session); Assert.That(session.SaveError,Is.Null);
        }
        private static List<CampaignCheckpoint> Checkpoints(int act)
        {
            if(SourceStates.TryGetValue(act,out var cached)) return cached;
            var content=CampaignContentLoader.Load(Json(act)); var store=new CaptureStore();
            using(var session=CampaignSession.StartAsync(content,store).GetAwaiter().GetResult())
            {
                for(int slot=0;slot<content.Slots.Count;slot++) {
                    CampaignFlowTests.ReachBrew(session); Assert.That(session.State.SlotIndex,Is.EqualTo(slot));
                    if(slot==6 || slot==21 || slot==29) {
                        Serve(session,slot==29 ? "recipe_french_75" : null,true);
                        for(int step=0;step<8 && session.State.Phase!=CampaignPhase.Retry;step++) CampaignFlowTests.Advance(session);
                        Assert.That(session.State.Phase,Is.EqualTo(CampaignPhase.Retry));
                        Assert.That(session.Knowledge.Project(session.CurrentEncounter.RequiredDiscovery).Known,Is.False);
                        CampaignFlowTests.Advance(session); Assert.That(session.State.Phase,Is.EqualTo(CampaignPhase.Brewing));
                    }
                    Serve(session,Plan[slot]); CampaignFlowTests.ReachDeparture(session);
                    session.TickAsync(2).GetAwaiter().GetResult(); Assert.That(session.SaveError,Is.Null);
                }
                Assert.That(session.State.Phase,Is.EqualTo(CampaignPhase.Complete));
            }
            SourceStates.Add(act,store.States); return store.States;
        }
        private static JObject Payload(CampaignCheckpoint state,CampaignContent content) => (JObject)JObject.Parse(CampaignSaveCodec.Encode(state,content))["payload"];
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
                "Changed original reading/results/knowledge/memory at "+before.SlotIndex+" / "+before.Phase+" / "+before.LineIndex);
        }
        private static CampaignSaveStore WriteSource(string folder,int act,params CampaignCheckpoint[] states)
        {
            var store=new CampaignSaveStore(folder,CampaignContentLoader.Load(Json(act)));
            foreach(var state in states) Task.Run(()=>store.SaveAsync(state)).GetAwaiter().GetResult(); return store;
        }
        private static CampaignSession RetryFixture()
        {
            var c=CampaignFlowTests.Content(true); var first=c.Encounters["enc_0"];
            var withRetry=new CampaignEncounter(first.Id,first.GuestId,first.Night,first.SummaryTextId,first.Request,first.Intro,first.Farewell,
                first.Responses.ToDictionary(x=>x.Key,x=>x.Value),first.RequiredDiscovery,discoveryRetryTextId:"unknown");
            var content=new CampaignContent(c.Revision,c.BundleHash,c.Catalog,c.Tuning,c.IngredientNames.ToDictionary(x=>x.Key,x=>x.Value),
                c.Texts.ToDictionary(x=>x.Key,x=>x.Value),c.Guests.ToDictionary(x=>x.Key,x=>x.Value),c.RecipePresentation.ToDictionary(x=>x.Key,x=>x.Value),
                new[]{withRetry}.Concat(c.Encounters.Values.Skip(1)),c.Slots,c.Supply,c.KnownSeeds.ToDictionary(x=>x.Key,x=>x.Value),c.ChoicePrompt,
                c.PrinceChoices.ToDictionary(x=>x.Key,x=>x.Value),c.PrinceResponses.ToDictionary(x=>x.Key,x=>x.Value),c.PrinceRecalls.ToDictionary(x=>x.Key,x=>x.Value),c.OpenFarewell,c.QuietFarewell);
            var session=CampaignSession.StartAsync(content,new CaptureStore()).GetAwaiter().GetResult();
            CampaignFlowTests.ReachBrew(session); CampaignFlowTests.Serve(session,null);
            CampaignFlowTests.Advance(session); CampaignFlowTests.Advance(session); Assert.That(session.State.Phase,Is.EqualTo(CampaignPhase.Retry));
            return session;
        }
        [Test] public void OwnedOptionalRetryReadingRoundTripsAfterFailedDiscoveryWithoutLearningTheRecipe()
        {
            using(var session=RetryFixture()) {
                Assert.That(session.CurrentText.Id,Is.EqualTo("unknown"));
                CampaignSaveCodec.Encode(session.State,session.Content);
                CampaignFlowTests.Advance(session); Assert.That(session.State.Phase,Is.EqualTo(CampaignPhase.Brewing));
                var restored=CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(session.State,session.Content),session.Content);
                Assert.That(restored.ConsumedLines,Does.Contain(session.State.OccurrenceId+":Retry:0:unknown"));
                Assert.That(restored.KnownReceipts.ContainsKey("recipe_manhattan"),Is.False);
                Assert.That(restored.Services.Single().Snapshot.AttemptId,Is.EqualTo(session.State.CurrentService.Snapshot.AttemptId));
            }
        }
        [TestCase(1,17)][TestCase(2,25)][TestCase(3,29)]
        public void ActFourContinuationPreservesEverySourcePhaseAndReceipt(int act,int nextSlot)
        {
            var continuation=new CampaignContinuation(Json(act),Json(4)); var states=Checkpoints(act);
            Assert.That(states.Select(x=>x.Phase).Distinct(),Is.EquivalentTo(Enum.GetValues(typeof(CampaignPhase)).Cast<CampaignPhase>()));
            foreach(var before in states) {
                var after=continuation.Convert(CampaignSaveCodec.Encode(before,continuation.SourceContent)); Preserved(before,after,continuation);
                if(before.Phase==CampaignPhase.Brewing || before.Phase==CampaignPhase.Reaction)
                    using(var resumed=CampaignSession.Continue(continuation.ExpandedContent,new CaptureStore(),after)) {
                        if(before.Phase==CampaignPhase.Brewing) Assert.That(resumed.Brew.Container(ContainerRole.Serving).TotalMl,Is.Zero);
                        else { Assert.That(resumed.Brew,Is.Null); Assert.That(resumed.State.CurrentService.Snapshot.AttemptId,Is.EqualTo(before.CurrentService.Snapshot.AttemptId)); }
                    }
            }
            var completed=continuation.Convert(CampaignSaveCodec.Encode(states.Last(),continuation.SourceContent));
            Assert.That(completed.SlotIndex,Is.EqualTo(nextSlot)); Assert.That(completed.Phase,Is.EqualTo(CampaignPhase.NightEnd));
            Assert.That(completed.CurrentService,Is.Null); Assert.That(completed.Memories.Count,Is.EqualTo(act));
            Assert.That(completed.KnownReceipts.ContainsKey("recipe_french_75"),Is.False);
            using(var resumed=CampaignSession.Continue(continuation.ExpandedContent,new CaptureStore(),completed)) {
                CampaignFlowTests.Advance(resumed); Assert.That(resumed.State.Phase,Is.EqualTo(CampaignPhase.NightIntro));
                Assert.That(resumed.State.CompletedOccurrences.Count,Is.EqualTo(nextSlot));
            }
        }
        [TestCase(1)][TestCase(2)][TestCase(3)]
        public void ActFourImportPreservesBothOriginalGenerationsAndIsIdempotent(int act)
        {
            var continuation=new CampaignContinuation(Json(act),Json(4)); var states=Checkpoints(act); string folder=Folder();
            var source=WriteSource(folder,act,states[states.Count-2],states.Last());
            var current=File.ReadAllBytes(source.CurrentPath); var previous=File.ReadAllBytes(source.PreviousPath);
            var destination=new CampaignSaveStore(Folder(),continuation.ExpandedContent);
            var first=Task.Run(()=>continuation.LoadOrImportAsync(folder,destination)).GetAwaiter().GetResult(); Preserved(states.Last(),first.Checkpoint,continuation);
            var imported=File.ReadAllBytes(destination.CurrentPath);
            destination.Fault=point=> { throw new IOException("Existing history must not republish"); };
            var again=Task.Run(()=>continuation.LoadOrImportAsync(folder,destination)).GetAwaiter().GetResult();
            Assert.That(again.Checkpoint.Sequence,Is.EqualTo(first.Checkpoint.Sequence));
            Assert.That(File.ReadAllBytes(destination.CurrentPath),Is.EqualTo(imported)); Assert.That(File.Exists(destination.PreviousPath),Is.False);
            Assert.That(File.ReadAllBytes(source.CurrentPath),Is.EqualTo(current)); Assert.That(File.ReadAllBytes(source.PreviousPath),Is.EqualTo(previous));
        }
        [TestCase("before_write")][TestCase("after_write")][TestCase("after_flush")][TestCase("after_validate")][TestCase("after_replace")][TestCase("after_final_read")]
        public void ActThreeToFourPublicationFaultRetryKeepsOneImportAndOriginalHistory(string fault)
        {
            var continuation=new CampaignContinuation(Json(3),Json(4)); var states=Checkpoints(3); string folder=Folder();
            var source=WriteSource(folder,3,states[states.Count-2],states.Last());
            var current=File.ReadAllBytes(source.CurrentPath); var previous=File.ReadAllBytes(source.PreviousPath);
            var destination=new CampaignSaveStore(Folder(),continuation.ExpandedContent);
            destination.Fault=point=> { if(point==fault) throw new IOException("injected "+point); };
            Assert.Throws<IOException>(()=>Task.Run(()=>continuation.LoadOrImportAsync(folder,destination)).GetAwaiter().GetResult());
            destination.Fault=null; var loaded=Task.Run(()=>continuation.LoadOrImportAsync(folder,destination)).GetAwaiter().GetResult();
            Preserved(states.Last(),loaded.Checkpoint,continuation); Assert.That(File.Exists(destination.PreviousPath),Is.False);
            Assert.That(Directory.GetFiles(Path.GetDirectoryName(destination.CurrentPath),"pending-*.json"),Is.Empty);
            Assert.That(File.ReadAllBytes(source.CurrentPath),Is.EqualTo(current)); Assert.That(File.ReadAllBytes(source.PreviousPath),Is.EqualTo(previous));
        }
        [TestCase("source_bytes")][TestCase("old_text")][TestCase("old_recipe")][TestCase("old_memory")][TestCase("old_supply")][TestCase("old_retry")]
        public void ActFourRejectsChangedAcceptedSourceBytesOrAuthoredPrefix(string fault)
        {
            string source=Json(3); var expanded=JObject.Parse(Json(4));
            if(fault=="source_bytes") source+=" ";
            if(fault=="old_text") expanded["texts"][0]["value"]="changed original wording";
            if(fault=="old_recipe") expanded["recipes"][0]["amounts"][0]["ml"]=31m;
            if(fault=="old_memory") expanded["encounters"].Single(x=>x["id"].Value<string>()=="enc_rc1_28")["memoryVariant"]="changed_windmill";
            if(fault=="old_supply") expanded["supplyGrants"][0]["beforeEncounter"]="enc_rc1_20";
            if(fault=="old_retry") expanded["encounters"].First(x=>x["requiredDiscovery"].Type==JTokenType.String)["discoveryRetryTextId"]="text_rc1_0039";
            Assert.Throws<ContentValidationException>(()=>new CampaignContinuation(source,expanded.ToString()));
        }
        [TestCase(1)][TestCase(2)][TestCase(3)]
        public void ActFourPrefixAllowsOnlyAbsentOrExplicitNullOptionalRetryMetadata(int act)
        {
            var expanded=JObject.Parse(Json(4)); int prefix=((JArray)JObject.Parse(Json(act))["encounters"]).Count;
            foreach(var encounter in expanded["encounters"].Take(prefix)) encounter["discoveryRetryTextId"]=null;
            Assert.DoesNotThrow(()=>new CampaignContinuation(Json(act),expanded.ToString()));
        }
        [TestCase(false)][TestCase(true)]
        public void ActFourImportNeverRollsBackFutureSourceOrDestinationHistory(bool destinationFuture)
        {
            var continuation=new CampaignContinuation(Json(3),Json(4)); var states=Checkpoints(3); string folder=Folder();
            var source=WriteSource(folder,3,states[states.Count-2],states.Last()); var destination=new CampaignSaveStore(Folder(),continuation.ExpandedContent);
            string path=destinationFuture ? destination.CurrentPath : source.CurrentPath;
            if(destinationFuture) { Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(destination.PreviousPath,CampaignSaveCodec.Encode(continuation.Convert(CampaignSaveCodec.Encode(states[0],continuation.SourceContent)),continuation.ExpandedContent)); }
            File.WriteAllText(path,"{\"schemaVersion\":999}");
            Assert.Throws<UnsupportedSaveVersionException>(()=>Task.Run(()=>continuation.LoadOrImportAsync(folder,destination,true)).GetAwaiter().GetResult());
            Assert.That(File.ReadAllText(path),Is.EqualTo("{\"schemaVersion\":999}"));
            if(!destinationFuture) Assert.That(destination.HasHistory,Is.False);
        }
        [TestCase("current.json")][TestCase("previous.json")]
        public void ExistingInvalidActFourGenerationPreventsOlderImport(string generation)
        {
            var continuation=new CampaignContinuation(Json(3),Json(4)); string folder=Folder(); WriteSource(folder,3,Checkpoints(3).Last());
            string targetFolder=Folder(); Directory.CreateDirectory(targetFolder); string path=Path.Combine(targetFolder,generation); File.WriteAllText(path,"existing broken ACT4");
            var destination=new CampaignSaveStore(targetFolder,continuation.ExpandedContent);
            Assert.Throws<ContentValidationException>(()=>Task.Run(()=>continuation.LoadOrImportAsync(folder,destination,true)).GetAwaiter().GetResult());
            Assert.That(File.ReadAllText(path),Is.EqualTo("existing broken ACT4")); Assert.That(Directory.GetFiles(targetFolder).Length,Is.EqualTo(1));
        }
        [Test] public void ExistingActFourHistoryWinsWithoutReadingEvenAFutureActThreeSource()
        {
            var continuation=new CampaignContinuation(Json(3),Json(4)); string sourceFolder=Folder(); Directory.CreateDirectory(sourceFolder);
            string future=Path.Combine(sourceFolder,"current.json"); File.WriteAllText(future,"{\"schemaVersion\":999}");
            var before=Checkpoints(4).First(x=>x.SlotIndex==30 && x.Phase==CampaignPhase.Reaction);
            var destination=new CampaignSaveStore(Folder(),continuation.ExpandedContent);
            Task.Run(()=>destination.SaveAsync(before)).GetAwaiter().GetResult(); var bytes=File.ReadAllBytes(destination.CurrentPath);
            var loaded=Task.Run(()=>continuation.LoadOrImportAsync(sourceFolder,destination)).GetAwaiter().GetResult();
            Assert.That(loaded.Checkpoint.Sequence,Is.EqualTo(before.Sequence)); Assert.That(loaded.Checkpoint.RunId,Is.EqualTo(before.RunId));
            Assert.That(File.ReadAllBytes(destination.CurrentPath),Is.EqualTo(bytes)); Assert.That(File.ReadAllText(future),Is.EqualTo("{\"schemaVersion\":999}"));
        }
        [Test] public void ActThreeCorruptionRequiresExplicitRecoveryWithoutTouchingEitherSourceGeneration()
        {
            var continuation=new CampaignContinuation(Json(3),Json(4)); var states=Checkpoints(3); string folder=Folder();
            var source=WriteSource(folder,3,states[states.Count-2],states.Last()); File.WriteAllText(source.CurrentPath,"corrupt ACT3");
            var previous=File.ReadAllBytes(source.PreviousPath); var destination=new CampaignSaveStore(Folder(),continuation.ExpandedContent);
            var preview=Task.Run(()=>continuation.LoadOrImportAsync(folder,destination)).GetAwaiter().GetResult();
            Assert.That(preview.RecoveryRequired,Is.True); Assert.That(preview.Checkpoint.Phase,Is.EqualTo(CampaignPhase.Departing)); Assert.That(destination.HasHistory,Is.False);
            var recovered=Task.Run(()=>continuation.LoadOrImportAsync(folder,destination,true)).GetAwaiter().GetResult();
            Preserved(states[states.Count-2],recovered.Checkpoint,continuation);
            Assert.That(File.ReadAllText(source.CurrentPath),Is.EqualTo("corrupt ACT3")); Assert.That(File.ReadAllBytes(source.PreviousPath),Is.EqualTo(previous));
        }
        [Test] public void FrenchRetryAndHamletDepartureRoundTripEveryActualCheckpointWithoutChangingEvidence()
        {
            var content=CampaignContentLoader.Load(Json(4)); var states=Checkpoints(4);
            foreach(var before in states.Where(x=>x.SlotIndex>=29)) {
                string json=CampaignSaveCodec.Encode(before,content); var after=CampaignSaveCodec.Decode(json,content);
                Assert.That(CampaignSaveCodec.Encode(after,content),Is.EqualTo(json));
                using(var resumed=CampaignSession.Continue(content,new CaptureStore(),after)) {
                    if(after.Phase==CampaignPhase.Retry) { Assert.That(resumed.CurrentText.Id,Is.EqualTo("text_rc1_0039")); Assert.That(resumed.Knowledge.Project("recipe_french_75").Known,Is.False); }
                    if(after.Phase==CampaignPhase.Brewing) Assert.That(resumed.Brew.Container(ContainerRole.Serving).TotalMl,Is.Zero);
                }
            }
            var complete=states.Last(); string receipt=complete.RunId+":29:Retry:0:text_rc1_0039";
            Assert.That(complete.ConsumedLines.Count(x=>x==receipt),Is.EqualTo(1));
            Assert.That(complete.KnownReceipts["recipe_french_75"],Is.EqualTo("service:"+complete.RunId+":29"));
            Assert.That(complete.Services.Count(x=>x.OccurrenceId==complete.RunId+":29"),Is.EqualTo(2));
            Assert.That(complete.Services.Last().Outcome.Fits,Is.False);
            Assert.That(complete.Memories.Select(x=>x.Id),Is.EqualTo(new[]{"mem_01","mem_02","mem_03","mem_04"}));
            Assert.That(complete.Memories.Last().Variant,Is.EqualTo("stage_skull_shadow")); Assert.That(complete.CompletedOccurrences.Count,Is.EqualTo(33));
            using(var resumed=CampaignSession.Continue(content,new CaptureStore(),CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(complete,content),content))) {
                long sequence=resumed.State.Sequence; CampaignFlowTests.Advance(resumed); resumed.TickAsync(3).GetAwaiter().GetResult();
                Assert.That(resumed.State.Sequence,Is.EqualTo(sequence)); Assert.That(resumed.State.Memories.Count,Is.EqualTo(4));
            }
        }
        [TestCase("unattempted")][TestCase("premature")][TestCase("wrong_encounter")][TestCase("wrong_text")][TestCase("wrong_cursor")]
        public void ForgedRetryReceiptCannotClaimAnUnownedOrUnattemptedReading(string fault)
        {
            var content=CampaignContentLoader.Load(Json(4)); var states=Checkpoints(4);
            var before=fault=="unattempted" ? states.First(x=>x.SlotIndex==29 && x.Phase==CampaignPhase.Brewing) : states.First(x=>x.SlotIndex==29 && x.Phase==CampaignPhase.Retry);
            var root=JObject.Parse(CampaignSaveCodec.Encode(before,content)); var payload=(JObject)root["payload"];
            ((JArray)payload["consumedLines"]).Add(before.RunId+":"+(fault=="wrong_encounter" ? "28" : "29")+":Retry:"+(fault=="wrong_cursor" ? "1" : "0")+":"+(fault=="wrong_text" ? "text_rc1_0035" : "text_rc1_0039"));
            using(var sha=SHA256.Create()) root["payloadSha256"]=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload.ToString(Formatting.None)))).Replace("-","").ToLowerInvariant();
            Assert.Throws<ContentValidationException>(()=>CampaignSaveCodec.Decode(root.ToString(),content));
        }
        [TestCase("after_validate")][TestCase("after_replace")]
        public void FailedFrenchRetryReadingPublicationResumesTheSameEmptyAttemptAndOneReceipt(string fault)
        {
            var content=CampaignContentLoader.Load(Json(4)); var before=Checkpoints(4).First(x=>x.SlotIndex==29 && x.Phase==CampaignPhase.Retry);
            var store=new CampaignSaveStore(Folder(),content); Task.Run(()=>store.SaveAsync(before)).GetAwaiter().GetResult();
            using(var session=CampaignSession.Continue(content,store,before)) {
                store.Fault=point=> { if(point==fault) throw new IOException("injected "+point); };
                CampaignFlowTests.Advance(session); Assert.That(session.SaveError,Is.Not.Null);
                Assert.That(session.State.Sequence,Is.EqualTo(before.Sequence)); Assert.That(session.State.Phase,Is.EqualTo(CampaignPhase.Retry));
                store.Fault=null; Assert.That(Task.Run(()=>session.RetrySaveAsync()).GetAwaiter().GetResult(),Is.True);
                var after=store.Load().Checkpoint; Assert.That(after.Sequence,Is.EqualTo(before.Sequence+1));
                Assert.That(after.Phase,Is.EqualTo(CampaignPhase.Brewing)); Assert.That(after.Services.Count,Is.EqualTo(before.Services.Count));
                Assert.That(after.KnownReceipts.ContainsKey("recipe_french_75"),Is.False);
                Assert.That(after.ConsumedLines.Count(x=>x==before.OccurrenceId+":Retry:0:text_rc1_0039"),Is.EqualTo(1));
                Assert.That(session.Brew.Container(ContainerRole.Serving).TotalMl,Is.Zero);
            }
        }
        [TestCase("Brewing")][TestCase("Reaction")][TestCase("Complete")]
        public void FrenchRetryCannotAdvanceWithoutItsConsumedReadingEvenWithAValidChecksum(string phase)
        {
            var content=CampaignContentLoader.Load(Json(4)); var states=Checkpoints(4);
            var before=states.First(x=>x.Phase.ToString()==phase && x.ConsumedLines.Any(line=>line.Contains(":Retry:0:text_rc1_0039")));
            var root=JObject.Parse(CampaignSaveCodec.Encode(before,content)); var payload=(JObject)root["payload"];
            payload["consumedLines"]=new JArray(((JArray)payload["consumedLines"]).Values<string>().Where(x=>!x.EndsWith(":Retry:0:text_rc1_0039",StringComparison.Ordinal)));
            using(var sha=SHA256.Create()) root["payloadSha256"]=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload.ToString(Formatting.None)))).Replace("-","").ToLowerInvariant();
            Assert.Throws<ContentValidationException>(()=>CampaignSaveCodec.Decode(root.ToString(),content));
        }
    }
}
