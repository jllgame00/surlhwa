using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Domain;

namespace Surlhwa.Tests
{
    public sealed class CampaignContinuationTests
    {
        private static string ExpandedJson() => File.ReadAllText(Path.Combine(SliceContentTests.ContentRoot(),"Campaign/act2.json"));
        private static string Folder() => Path.GetFullPath(Path.Combine(SliceContentTests.ContentRoot(),"../Validation/Evidence/campaign-continuation-tests",Guid.NewGuid().ToString("N")));

        private static CampaignContinuation Continuation(string source=null,string expanded=null) =>
            new CampaignContinuation(source ?? CampaignContentTests.Json(),expanded ?? ExpandedJson());
        private sealed class CaptureStore : ICampaignSaveStore
        {
            private readonly CampaignContent content;
            internal readonly List<CampaignCheckpoint> States=new List<CampaignCheckpoint>();
            internal CaptureStore(CampaignContent content) { this.content=content; }
            public Task SaveAsync(CampaignCheckpoint checkpoint)
            { States.Add(CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(checkpoint,content),content)); return Task.CompletedTask; }
        }
        private static List<CampaignCheckpoint> ActOneCheckpoints()
        {
            var content=CampaignContentTests.Content(); var store=new CaptureStore(content);
            using(var session=CampaignSession.StartAsync(content,store).GetAwaiter().GetResult())
            {
                string[] recipes={null,"recipe_test_stir","recipe_test_stir","recipe_dry_martini","recipe_dry_martini","recipe_manwol",
                    "recipe_negroni","recipe_test_stir","recipe_negroni","recipe_manhattan","recipe_manhattan","recipe_test_stir","recipe_negroni",
                    "recipe_gin_fizz","recipe_dry_martini","recipe_gin_fizz","recipe_dry_martini"};
                for(int slot=0;slot<17;slot++)
                {
                    CampaignFlowTests.ReachBrew(session);
                    if(slot==6) {
                        Assert.That(session.CurrentEncounter.RequiredDiscovery,Is.EqualTo("recipe_negroni"));
                        CampaignFlowTests.Serve(session,null); CampaignFlowTests.ReachBrew(session);
                        Assert.That(session.State.SlotIndex,Is.EqualTo(slot));
                    }
                    session.ReleaseHeldInput();
                    session.ExecuteBrew(++CampaignFlowTests.Event,b=> {
                        if(recipes[slot]==null) b.Pour("water",ContainerRole.Serving,1m);
                        else CampaignContentTests.Make(b,recipes[slot]);
                    });
                    CampaignFlowTests.Advance(session); CampaignFlowTests.ReachDeparture(session);
                    session.TickAsync(2).GetAwaiter().GetResult(); Assert.That(session.SaveError,Is.Null);
                }
                Assert.That(session.State.Phase,Is.EqualTo(CampaignPhase.Complete));
            }
            return store.States;
        }
        private static JObject Payload(CampaignCheckpoint checkpoint,CampaignContent content) =>
            (JObject)JObject.Parse(CampaignSaveCodec.Encode(checkpoint,content))["payload"];
        private static void SameReceipts(CampaignCheckpoint before,CampaignContent source,CampaignCheckpoint after,CampaignContent expanded)
        {
            var oldPayload=Payload(before,source); var newPayload=Payload(after,expanded);
            foreach(string key in new[]{"princeChoice","farewell","consumedLines","completedOccurrences","knownReceipts","facts","services","memories"})
                Assert.That(JToken.DeepEquals(oldPayload[key],newPayload[key]),Is.True,"Changed receipt: "+key);
            Assert.That(after.RunId,Is.EqualTo(before.RunId));
        }
        private static CampaignSaveStore WriteSource(string folder,CampaignContent content,params CampaignCheckpoint[] states)
        {
            var store=new CampaignSaveStore(folder,content);
            foreach(var state in states) Task.Run(()=>store.SaveAsync(state)).GetAwaiter().GetResult();
            return store;
        }
        [Test] public void CompletedActOneContinuesAtNightEndWithoutReplayingOrRewritingAnyReceipt()
        {
            var continuation=Continuation(); var before=ActOneCheckpoints().Last();
            var after=continuation.Convert(CampaignSaveCodec.Encode(before,continuation.SourceContent));
            Assert.That(after.SlotIndex,Is.EqualTo(17)); Assert.That(after.Phase,Is.EqualTo(CampaignPhase.NightEnd));
            Assert.That(after.Sequence,Is.EqualTo(before.Sequence+1)); Assert.That(after.LineIndex,Is.Zero);
            Assert.That(after.SelectedEncounters.Take(17),Is.EqualTo(before.SelectedEncounters));
            Assert.That(after.SelectedEncounters.Last(),Is.EqualTo(continuation.ExpandedContent.Slots[17].EncounterId));
            Assert.That(after.CurrentService,Is.Null); SameReceipts(before,continuation.SourceContent,after,continuation.ExpandedContent);
            using(var session=CampaignSession.Continue(continuation.ExpandedContent,new CampaignFlowTests.Store(),after))
            {
                CampaignFlowTests.Advance(session); Assert.That(session.State.Phase,Is.EqualTo(CampaignPhase.NightIntro));
                Assert.That(session.State.CompletedOccurrences.Count,Is.EqualTo(17)); Assert.That(session.State.Memories.Count,Is.EqualTo(1));
            }
        }
        [Test] public void EveryAcceptedActOneCheckpointPreservesPhaseCursorAndReceiptsUnderExpandedDefinitions()
        {
            var continuation=Continuation(); var checkpoints=ActOneCheckpoints().Where(x=>x.Phase!=CampaignPhase.Complete).ToArray();
            Assert.That(checkpoints.Any(x=>x.Phase==CampaignPhase.Brewing),Is.True);
            Assert.That(checkpoints.Any(x=>x.Phase==CampaignPhase.Reaction),Is.True);
            Assert.That(checkpoints.Any(x=>x.Phase==CampaignPhase.Retry),Is.True);
            Assert.That(checkpoints.Any(x=>x.Phase==CampaignPhase.Farewell && x.LineIndex>0),Is.True);
            foreach(var before in checkpoints)
            {
                var after=continuation.Convert(CampaignSaveCodec.Encode(before,continuation.SourceContent));
                Assert.That(after.Phase,Is.EqualTo(before.Phase)); Assert.That(after.SlotIndex,Is.EqualTo(before.SlotIndex));
                Assert.That(after.LineIndex,Is.EqualTo(before.LineIndex)); Assert.That(after.Sequence,Is.EqualTo(before.Sequence));
                Assert.That(after.SelectedEncounters,Is.EqualTo(before.SelectedEncounters));
                SameReceipts(before,continuation.SourceContent,after,continuation.ExpandedContent);
            }
        }
        [Test] public void MidBrewContinuationUsesEmptyToolsButKeepsConsumedKnowledgeAndPostServiceDoesNotRequireRebrewing()
        {
            var continuation=Continuation(); var checkpoints=ActOneCheckpoints();
            foreach(var phase in new[]{CampaignPhase.Brewing,CampaignPhase.Reaction})
            {
                var before=checkpoints.First(x=>x.SlotIndex==13 && x.Phase==phase);
                var after=continuation.Convert(CampaignSaveCodec.Encode(before,continuation.SourceContent));
                using(var session=CampaignSession.Continue(continuation.ExpandedContent,new CampaignFlowTests.Store(),after))
                {
                    if(phase==CampaignPhase.Brewing) {
                        Assert.That(session.Brew.Container(ContainerRole.Serving).TotalMl,Is.Zero);
                        Assert.That(session.Knowledge.Project("recipe_gin_fizz").PreparationVisible,Is.True);
                    }
                    else { Assert.That(session.Brew,Is.Null); Assert.That(session.State.CurrentService.Snapshot.AttemptId,Is.EqualTo(before.CurrentService.Snapshot.AttemptId)); }
                }
            }
        }
        [TestCase("source_bytes")][TestCase("old_tuning")][TestCase("old_text")][TestCase("old_recipe")][TestCase("old_slot")][TestCase("old_memory")][TestCase("extra_seed")][TestCase("early_supply")]
        public void ContinuationRejectsUnapprovedSourceOrChangesToAcceptedActOneDefinitions(string fault)
        {
            string source=CampaignContentTests.Json(); var expanded=JObject.Parse(ExpandedJson());
            if(fault=="source_bytes") source+=" ";
            if(fault=="old_tuning") expanded["tuning"]["recognition"]["ingredientMl"]=1.1m;
            if(fault=="old_text") expanded["texts"][0]["value"]="changed authored wording";
            if(fault=="old_recipe") expanded["recipes"][0]["amounts"][0]["ml"]=31m;
            if(fault=="old_slot") { var slots=(JArray)expanded["slots"]; var first=slots[0].DeepClone(); slots[0]=slots[1].DeepClone(); slots[1]=first; }
            if(fault=="old_memory") { expanded["encounters"][0]["memoryId"]="invented_memory"; expanded["encounters"][0]["memoryVariant"]="invented_variant"; }
            if(fault=="extra_seed") ((JArray)expanded["known"]).Add(JObject.Parse("{\"recipeId\":\"recipe_hanky_panky\",\"provenance\":\"invented\"}"));
            if(fault=="early_supply") ((JArray)expanded["supplyGrants"]).Clear();
            Assert.Throws<ContentValidationException>(()=>Continuation(source,expanded.ToString()));
        }
        [Test] public void SourceHashMismatchCannotBeSilentlyReboundToExpandedContent()
        {
            var continuation=Continuation(); var before=CampaignSession.StartAsync(continuation.SourceContent,new CampaignFlowTests.Store()).GetAwaiter().GetResult().State;
            var root=JObject.Parse(CampaignSaveCodec.Encode(before,continuation.SourceContent)); root["contentHash"]=new string('0',64);
            Assert.Throws<ContentValidationException>(()=>continuation.Convert(root.ToString()));
        }
        [Test] public void ImportRetainsBothSourceGenerationsByteForByteAndRetryNeverDuplicatesHistory()
        {
            var continuation=Continuation(); var states=ActOneCheckpoints(); string sourceFolder=Folder();
            var source=WriteSource(sourceFolder,continuation.SourceContent,states[states.Count-2],states.Last());
            var original=File.ReadAllBytes(source.CurrentPath); var previous=File.ReadAllBytes(source.PreviousPath);
            var destination=new CampaignSaveStore(Folder(),continuation.ExpandedContent);
            var first=Task.Run(()=>continuation.LoadOrImportAsync(sourceFolder,destination)).GetAwaiter().GetResult();
            byte[] imported=File.ReadAllBytes(destination.CurrentPath);
            var second=Task.Run(()=>continuation.LoadOrImportAsync(sourceFolder,destination)).GetAwaiter().GetResult();
            Assert.That(second.Checkpoint.Sequence,Is.EqualTo(first.Checkpoint.Sequence));
            Assert.That(File.ReadAllBytes(destination.CurrentPath),Is.EqualTo(imported)); Assert.That(File.Exists(destination.PreviousPath),Is.False);
            Assert.That(File.ReadAllBytes(source.CurrentPath),Is.EqualTo(original)); Assert.That(File.ReadAllBytes(source.PreviousPath),Is.EqualTo(previous));
        }
        [TestCase("before_write")][TestCase("after_write")][TestCase("after_flush")][TestCase("after_validate")][TestCase("after_replace")][TestCase("after_final_read")]
        public void FailedImportRetryPublishesOneCheckpointAndPreservesSourceBytes(string boundary)
        {
            var continuation=Continuation(); string folder=Folder(); var states=ActOneCheckpoints();
            var source=WriteSource(folder,continuation.SourceContent,states[states.Count-2],states.Last());
            var current=File.ReadAllBytes(source.CurrentPath); var previous=File.ReadAllBytes(source.PreviousPath);
            var destination=new CampaignSaveStore(Folder(),continuation.ExpandedContent);
            destination.Fault=point=> { if(point==boundary) throw new IOException("injected "+boundary); };
            Assert.Throws<IOException>(()=>Task.Run(()=>continuation.LoadOrImportAsync(folder,destination)).GetAwaiter().GetResult());
            destination.Fault=null; var resumed=Task.Run(()=>continuation.LoadOrImportAsync(folder,destination)).GetAwaiter().GetResult();
            Assert.That(resumed.Checkpoint.SlotIndex,Is.EqualTo(17)); Assert.That(resumed.Checkpoint.Memories.Count,Is.EqualTo(1));
            Assert.That(File.Exists(destination.PreviousPath),Is.False);
            Assert.That(File.ReadAllBytes(source.CurrentPath),Is.EqualTo(current)); Assert.That(File.ReadAllBytes(source.PreviousPath),Is.EqualTo(previous));
            Assert.That(Directory.GetFiles(Path.GetDirectoryName(destination.CurrentPath),"pending-*.json"),Is.Empty);
        }
        [Test] public void SourceCorruptionIsExplicitRecoveryAndDoesNotWriteUntilAccepted()
        {
            var continuation=Continuation(); var states=ActOneCheckpoints(); string folder=Folder();
            var source=WriteSource(folder,continuation.SourceContent,states[states.Count-2],states.Last());
            File.WriteAllText(source.CurrentPath,"corrupt current"); var previous=File.ReadAllBytes(source.PreviousPath);
            var destination=new CampaignSaveStore(Folder(),continuation.ExpandedContent);
            var preview=Task.Run(()=>continuation.LoadOrImportAsync(folder,destination)).GetAwaiter().GetResult();
            Assert.That(preview.RecoveryRequired,Is.True); Assert.That(preview.Checkpoint.Phase,Is.EqualTo(CampaignPhase.Departing));
            Assert.That(File.Exists(destination.CurrentPath),Is.False);
            var accepted=Task.Run(()=>continuation.LoadOrImportAsync(folder,destination,true)).GetAwaiter().GetResult();
            Assert.That(accepted.Checkpoint.Phase,Is.EqualTo(CampaignPhase.Departing)); Assert.That(accepted.RecoveryRequired,Is.False);
            Assert.That(File.ReadAllText(source.CurrentPath),Is.EqualTo("corrupt current")); Assert.That(File.ReadAllBytes(source.PreviousPath),Is.EqualTo(previous));
        }
        [TestCase(false)][TestCase(true)] public void FutureSchemaNeverRollsBackToOlderGenerationOrLegacy(bool atDestination)
        {
            var continuation=Continuation(); var states=ActOneCheckpoints(); string folder=Folder();
            var source=WriteSource(folder,continuation.SourceContent,states[states.Count-2],states.Last());
            var destination=new CampaignSaveStore(Folder(),continuation.ExpandedContent);
            string path=atDestination ? destination.CurrentPath : source.CurrentPath;
            if(atDestination) { Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(destination.PreviousPath,CampaignSaveCodec.Encode(continuation.Convert(CampaignSaveCodec.Encode(states[0],continuation.SourceContent)),continuation.ExpandedContent)); }
            File.WriteAllText(path,"{\"schemaVersion\":999}");
            Assert.Throws<UnsupportedSaveVersionException>(()=>Task.Run(()=>continuation.LoadOrImportAsync(folder,destination,true)).GetAwaiter().GetResult());
            Assert.That(File.ReadAllText(path),Is.EqualTo("{\"schemaVersion\":999}"));
            if(!atDestination) Assert.That(File.Exists(destination.CurrentPath),Is.False);
        }
        [Test] public void ExistingCorruptDestinationNeverTriggersImportOrOverwritesEvidence()
        {
            var continuation=Continuation(); var states=ActOneCheckpoints(); string folder=Folder();
            WriteSource(folder,continuation.SourceContent,states.Last()); var destination=new CampaignSaveStore(Folder(),continuation.ExpandedContent);
            Directory.CreateDirectory(Path.GetDirectoryName(destination.CurrentPath)); File.WriteAllText(destination.CurrentPath,"existing broken ACT2");
            Assert.Throws<ContentValidationException>(()=>Task.Run(()=>continuation.LoadOrImportAsync(folder,destination,true)).GetAwaiter().GetResult());
            Assert.That(File.ReadAllText(destination.CurrentPath),Is.EqualTo("existing broken ACT2"));
        }
        [TestCase("current.json")][TestCase("previous.json")][TestCase("retained-evidence.json")]
        public void CreateOnlyImportCannotOverwriteOrMixWithNonemptyDestination(string existingFile)
        {
            var continuation=Continuation(); string folder=Folder();
            var sourceState=CampaignSession.StartAsync(continuation.SourceContent,new CampaignFlowTests.Store()).GetAwaiter().GetResult().State;
            var checkpoint=continuation.Convert(CampaignSaveCodec.Encode(sourceState,continuation.SourceContent));
            var destination=new CampaignSaveStore(folder,continuation.ExpandedContent); Directory.CreateDirectory(folder);
            string path=Path.Combine(folder,existingFile); string existing=CampaignSaveCodec.Encode(checkpoint,continuation.ExpandedContent);
            if(existingFile=="current.json") {
                using(var session=CampaignSession.Continue(continuation.ExpandedContent,new CampaignFlowTests.Store(),checkpoint))
                { CampaignFlowTests.Advance(session); existing=CampaignSaveCodec.Encode(session.State,continuation.ExpandedContent); }
            }
            File.WriteAllText(path,existing);
            Assert.Throws<InvalidOperationException>(()=>Task.Run(()=>destination.ImportAsync(checkpoint)).GetAwaiter().GetResult());
            Assert.That(File.ReadAllText(path),Is.EqualTo(existing)); Assert.That(Directory.GetFiles(folder).Length,Is.EqualTo(1));
        }
        [Test] public void IdenticalCreateOnlyImportRetryDoesNotReplaceEitherGeneration()
        {
            var continuation=Continuation();
            var state=CampaignSession.StartAsync(continuation.ExpandedContent,new CampaignFlowTests.Store()).GetAwaiter().GetResult().State;
            var store=new CampaignSaveStore(Folder(),continuation.ExpandedContent);
            Task.Run(()=>store.ImportAsync(state)).GetAwaiter().GetResult(); var bytes=File.ReadAllBytes(store.CurrentPath);
            store.Fault=point=> { throw new IOException("Retry must not republish"); };
            Task.Run(()=>store.ImportAsync(state)).GetAwaiter().GetResult();
            Assert.That(File.ReadAllBytes(store.CurrentPath),Is.EqualTo(bytes)); Assert.That(File.Exists(store.PreviousPath),Is.False);
        }
        [Test] public void MissingLegacySaveDoesNotCreateDestinationOrInventProgress()
        {
            var continuation=Continuation(); string source=Folder(); string folder=Folder(); var destination=new CampaignSaveStore(folder,continuation.ExpandedContent);
            Assert.That(continuation.LoadSource(source).Checkpoint,Is.Null);
            var result=Task.Run(()=>continuation.LoadOrImportAsync(source,destination)).GetAwaiter().GetResult();
            Assert.That(result.Checkpoint,Is.Null); Assert.That(result.RecoveryRequired,Is.False);
            Assert.That(Directory.Exists(source),Is.False); Assert.That(Directory.Exists(folder),Is.False);
        }
        [TestCase("complete")][TestCase("truncated")][TestCase("empty")]
        public void InterruptedImportCanRetryWithRecognizablePendingBytesRetained(string shape)
        {
            var continuation=Continuation(); string sourceFolder=Folder(); string destinationFolder=Folder();
            var source=new CampaignSaveStore(sourceFolder,continuation.SourceContent);
            using(var session=Task.Run(()=>CampaignSession.StartAsync(continuation.SourceContent,source)).GetAwaiter().GetResult())
            {
                CampaignFlowTests.Advance(session); Assert.That(session.SaveError,Is.Null);
                var current=File.ReadAllBytes(source.CurrentPath); var previous=File.ReadAllBytes(source.PreviousPath);
                var expected=continuation.Convert(CampaignSaveCodec.Encode(session.State,continuation.SourceContent));
                var expectedBytes=new UTF8Encoding(false).GetBytes(CampaignSaveCodec.Encode(expected,continuation.ExpandedContent));
                var pendingBytes=shape=="empty" ? Array.Empty<byte>() : shape=="truncated" ? expectedBytes.Take(expectedBytes.Length/2).ToArray() : expectedBytes;
                Directory.CreateDirectory(destinationFolder);
                string pending=Path.Combine(destinationFolder,"pending-0123456789abcdef0123456789abcdef.json");
                File.WriteAllBytes(pending,pendingBytes);
                var destination=new CampaignSaveStore(destinationFolder,continuation.ExpandedContent);
                var loaded=Task.Run(()=>continuation.LoadOrImportAsync(sourceFolder,destination)).GetAwaiter().GetResult();
                Assert.That(loaded.Checkpoint.Sequence,Is.EqualTo(expected.Sequence));
                Assert.That(File.ReadAllBytes(destination.CurrentPath),Is.EqualTo(expectedBytes));
                Assert.That(File.ReadAllBytes(pending),Is.EqualTo(pendingBytes));
                Assert.That(File.ReadAllBytes(source.CurrentPath),Is.EqualTo(current)); Assert.That(File.ReadAllBytes(source.PreviousPath),Is.EqualTo(previous));
                Task.Run(()=>continuation.LoadOrImportAsync(sourceFolder,destination)).GetAwaiter().GetResult();
                Assert.That(Directory.GetFiles(destinationFolder).Length,Is.EqualTo(2)); Assert.That(File.Exists(destination.PreviousPath),Is.False);
            }
        }
        [TestCase("different_checkpoint")][TestCase("future_schema")][TestCase("malformed_name")][TestCase("uppercase_name")][TestCase("directory")]
        public void PendingEvidenceCannotAuthorizeRollbackOrImportIntoUnrelatedFiles(string fault)
        {
            var continuation=Continuation(); string folder=Folder(); Directory.CreateDirectory(folder);
            using(var session=CampaignSession.StartAsync(continuation.ExpandedContent,new CampaignFlowTests.Store()).GetAwaiter().GetResult())
            {
                var imported=session.State; CampaignFlowTests.Advance(session);
                string data=fault=="future_schema" ? "{\"schemaVersion\":999}" : CampaignSaveCodec.Encode(fault=="different_checkpoint" ? session.State : imported,continuation.ExpandedContent);
                string path=Path.Combine(folder,fault=="malformed_name" ? "pending-not-a-guid.json" : fault=="uppercase_name" ?
                    "pending-0123456789ABCDEF0123456789ABCDEF.json" : "pending-0123456789abcdef0123456789abcdef.json");
                if(fault=="directory") Directory.CreateDirectory(path); else File.WriteAllText(path,data);
                var destination=new CampaignSaveStore(folder,continuation.ExpandedContent);
                Assert.Throws<InvalidOperationException>(()=>Task.Run(()=>destination.ImportAsync(imported)).GetAwaiter().GetResult());
                Assert.That(File.Exists(destination.CurrentPath),Is.False); Assert.That(File.Exists(destination.PreviousPath),Is.False);
                if(fault=="directory") Assert.That(Directory.Exists(path),Is.True); else Assert.That(File.ReadAllText(path),Is.EqualTo(data));
            }
        }
    }
}
