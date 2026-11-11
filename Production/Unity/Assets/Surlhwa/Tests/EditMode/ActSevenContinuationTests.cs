using System;
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
    public sealed class ActSevenContinuationTests
    {
        private static CampaignContinuation Continuation(int act)=>new CampaignContinuation(CampaignEndingTests.Json(act),CampaignEndingTests.Json());
        private static void Preserved(CampaignCheckpoint before,CampaignCheckpoint after,CampaignContinuation c)
        {
            var expected=CampaignEndingTests.Payload(before,c.SourceContent);
            if(before.Phase==CampaignPhase.Complete) {
                int next=c.SourceContent.Slots.Count; expected["slotIndex"]=next; expected["phase"]="NightEnd"; expected["lineIndex"]=0;
                ((JArray)expected["selectedEncounters"]).Add(c.ExpandedContent.Slots[next].EncounterId);
            }
            Assert.That(after.RunId,Is.EqualTo(before.RunId));
            Assert.That(after.Sequence,Is.EqualTo(before.Sequence+(before.Phase==CampaignPhase.Complete ? 1 : 0)));
            Assert.That(JToken.DeepEquals(expected,CampaignEndingTests.Payload(after,c.ExpandedContent)),Is.True,"Historical payload changed at "+before.SlotIndex+" / "+before.Phase);
        }
        private static CampaignSaveStore WriteSource(string folder,CampaignContent content,params CampaignCheckpoint[] states)
        {
            var source=new CampaignSaveStore(folder,content); foreach(var state in states) Task.Run(()=>source.SaveAsync(state)).GetAwaiter().GetResult(); return source;
        }
        [TestCase(1,17)][TestCase(2,25)][TestCase(3,29)][TestCase(4,33)][TestCase(5,37)][TestCase(6,43)]
        public void AcceptedEarlierPhasesAndCompleteConvertWithoutReevaluatingHistory(int act,int nextSlot)
        {
            var c=Continuation(act); var states=CampaignEndingTests.History(act);
            var selected=states.GroupBy(x=>x.Phase).Select(x=>x.Last()).Concat(states.Where(x=>x.SlotIndex>=37)).Distinct().ToArray();
            Assert.That(selected.Select(x=>x.Phase).Distinct(),Is.EquivalentTo(Enum.GetValues(typeof(CampaignPhase)).Cast<CampaignPhase>().Where(x=>x<=CampaignPhase.Complete)));
            foreach(var before in selected) {
                var after=c.Convert(CampaignSaveCodec.Encode(before,c.SourceContent)); Preserved(before,after,c);
                using(var resumed=CampaignSession.Continue(c.ExpandedContent,new CampaignEndingTests.CaptureStore(),after)) {
                    if(before.Phase==CampaignPhase.Brewing) Assert.That(resumed.Brew.Container(ContainerRole.Serving).TotalMl,Is.Zero);
                    if(before.Phase==CampaignPhase.Reaction) Assert.That(ServiceEvidenceCodec.Write(resumed.State.CurrentService),Is.EqualTo(ServiceEvidenceCodec.Write(before.CurrentService)));
                }
            }
            var complete=c.Convert(CampaignSaveCodec.Encode(states.Last(),c.SourceContent));
            Assert.That(complete.SlotIndex,Is.EqualTo(nextSlot)); Assert.That(complete.Phase,Is.EqualTo(CampaignPhase.NightEnd));
            Assert.That(complete.Memories.Count,Is.EqualTo(act)); Assert.That(complete.CurrentService,Is.Null);
        }
        [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)]
        public void ImportPreservesBothHistoricalGenerationsAndExistingDestinationWins(int act)
        {
            var c=Continuation(act); var states=CampaignEndingTests.History(act); string folder=CampaignEndingTests.Folder();
            var source=WriteSource(folder,c.SourceContent,states[states.Count-2],states.Last());
            var current=File.ReadAllBytes(source.CurrentPath); var previous=File.ReadAllBytes(source.PreviousPath);
            var target=new CampaignSaveStore(CampaignEndingTests.Folder(),c.ExpandedContent);
            var imported=Task.Run(()=>c.LoadOrImportAsync(folder,target)).GetAwaiter().GetResult(); Preserved(states.Last(),imported.Checkpoint,c);
            var bytes=File.ReadAllBytes(target.CurrentPath); target.Fault=point=> {throw new IOException("must not republish");};
            var again=Task.Run(()=>c.LoadOrImportAsync(folder,target)).GetAwaiter().GetResult(); Assert.That(again.Checkpoint.Sequence,Is.EqualTo(imported.Checkpoint.Sequence));
            Assert.That(File.ReadAllBytes(target.CurrentPath),Is.EqualTo(bytes)); Assert.That(File.Exists(target.PreviousPath),Is.False);
            Assert.That(File.ReadAllBytes(source.CurrentPath),Is.EqualTo(current)); Assert.That(File.ReadAllBytes(source.PreviousPath),Is.EqualTo(previous));
        }
        [Test] public void PendingActSixDepartureGrantsNoDuplicateMemoryBeforeFirstOdysseusNight()
        {
            var c=Continuation(6); var before=CampaignEndingTests.History(6).Last(x=>x.Phase==CampaignPhase.Departing);
            using(var s=CampaignSession.Continue(c.ExpandedContent,new CampaignEndingTests.CaptureStore(),c.Convert(CampaignSaveCodec.Encode(before,c.SourceContent)))) {
                s.TickAsync(2).GetAwaiter().GetResult(); Assert.That(s.SaveError,Is.Null); Assert.That(s.State.SlotIndex,Is.EqualTo(43));
                Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.NightEnd)); Assert.That(s.State.SelectedEncounters.Last(),Is.EqualTo("enc_rc1_44"));
                Assert.That(s.State.Memories.Count,Is.EqualTo(6)); Assert.That(s.State.CompletedOccurrences.Count,Is.EqualTo(43));
                Assert.That(s.State.Services.Select(ServiceEvidenceCodec.Write),Is.EqualTo(before.Services.Select(ServiceEvidenceCodec.Write)));
                CampaignSaveCodec.Encode(s.State,c.ExpandedContent); long sequence=s.State.Sequence; s.TickAsync(2).GetAwaiter().GetResult(); Assert.That(s.State.Sequence,Is.EqualTo(sequence));
            }
        }
        [TestCase("before_write")][TestCase("after_write")][TestCase("after_flush")][TestCase("after_validate")][TestCase("after_replace")][TestCase("after_final_read")]
        public void ActSixImportFaultRetriesWithoutChangingAcceptedHistory(string fault)
        {
            var c=Continuation(6); var states=CampaignEndingTests.History(6); string folder=CampaignEndingTests.Folder();
            var source=WriteSource(folder,c.SourceContent,states[states.Count-2],states.Last()); var current=File.ReadAllBytes(source.CurrentPath); var previous=File.ReadAllBytes(source.PreviousPath);
            var target=new CampaignSaveStore(CampaignEndingTests.Folder(),c.ExpandedContent); target.Fault=point=> {if(point==fault) throw new IOException("injected "+point);};
            Assert.Throws<IOException>(()=>Task.Run(()=>c.LoadOrImportAsync(folder,target)).GetAwaiter().GetResult());
            target.Fault=null; var recovered=Task.Run(()=>c.LoadOrImportAsync(folder,target)).GetAwaiter().GetResult(); Preserved(states.Last(),recovered.Checkpoint,c);
            Assert.That(File.ReadAllBytes(source.CurrentPath),Is.EqualTo(current)); Assert.That(File.ReadAllBytes(source.PreviousPath),Is.EqualTo(previous)); Assert.That(File.Exists(target.PreviousPath),Is.False);
        }
        [TestCase("source_bytes")][TestCase("old_label")][TestCase("old_text")][TestCase("old_recipe")][TestCase("old_retry")][TestCase("old_memory")][TestCase("old_supply")]
        public void ContinuationRejectsChangedActSixPrefix(string fault)
        {
            string source=CampaignEndingTests.Json(6); var target=JObject.Parse(CampaignEndingTests.Json());
            if(fault=="source_bytes") source+=" ";
            if(fault=="old_label") target["ingredients"][0]["label"]="changed accepted label";
            if(fault=="old_text") target["texts"][0]["value"]="changed accepted text";
            if(fault=="old_recipe") target["recipes"][0]["amounts"][0]["ml"]=31m;
            if(fault=="old_retry") target["encounters"].Single(x=>x["id"].Value<string>()=="enc_rc1_41")["discoveryRetryTextId"]=null;
            if(fault=="old_memory") target["encounters"].Single(x=>x["id"].Value<string>()=="enc_rc1_42")["memoryVariant"]="changed_memory";
            if(fault=="old_supply") target["supplyGrants"].Last["beforeEncounter"]="enc_rc1_41";
            Assert.Throws<ContentValidationException>(()=>new CampaignContinuation(source,target.ToString()));
        }
        [TestCase(6,6)][TestCase(6,5)][TestCase(7,7)][TestCase(7,6)]
        public void SameBackwardAndUnacceptedSourcesCannotConvert(int source,int target)
        {Assert.Throws<ContentValidationException>(()=>new CampaignContinuation(CampaignEndingTests.Json(source),CampaignEndingTests.Json(target)));}
        [TestCase("current.json",false)][TestCase("previous.json",false)][TestCase("current.json",true)][TestCase("previous.json",true)]
        public void InvalidOrFutureActSevenHistoryBlocksImport(string generation,bool future)
        {
            var c=Continuation(6); string sourceFolder=CampaignEndingTests.Folder(); WriteSource(sourceFolder,c.SourceContent,CampaignEndingTests.History(6).Last());
            string folder=CampaignEndingTests.Folder(); Directory.CreateDirectory(folder); string path=Path.Combine(folder,generation),bytes=future ? "{\"schemaVersion\":999}" : "invalid ACT7";
            File.WriteAllText(path,bytes); var target=new CampaignSaveStore(folder,c.ExpandedContent);
            if(future) Assert.Throws<UnsupportedSaveVersionException>(()=>Task.Run(()=>c.LoadOrImportAsync(sourceFolder,target,true)).GetAwaiter().GetResult());
            else Assert.Throws<ContentValidationException>(()=>Task.Run(()=>c.LoadOrImportAsync(sourceFolder,target,true)).GetAwaiter().GetResult());
            Assert.That(File.ReadAllText(path),Is.EqualTo(bytes)); Assert.That(Directory.GetFiles(folder).Length,Is.EqualTo(1));
        }
        [TestCase(false)][TestCase(true)]
        public void CorruptActSixNeedsRecoveryAndFutureActSixCannotRollBack(bool future)
        {
            var c=Continuation(6); var states=CampaignEndingTests.History(6); string folder=CampaignEndingTests.Folder();
            var source=WriteSource(folder,c.SourceContent,states[states.Count-2],states.Last()); string bytes=future ? "{\"schemaVersion\":999}" : "invalid ACT6";
            File.WriteAllText(source.CurrentPath,bytes); var previous=File.ReadAllBytes(source.PreviousPath); var target=new CampaignSaveStore(CampaignEndingTests.Folder(),c.ExpandedContent);
            if(future) Assert.Throws<UnsupportedSaveVersionException>(()=>Task.Run(()=>c.LoadOrImportAsync(folder,target,true)).GetAwaiter().GetResult());
            else {
                var preview=Task.Run(()=>c.LoadOrImportAsync(folder,target)).GetAwaiter().GetResult(); Assert.That(preview.RecoveryRequired,Is.True); Assert.That(target.HasHistory,Is.False);
                var recovered=Task.Run(()=>c.LoadOrImportAsync(folder,target,true)).GetAwaiter().GetResult(); Preserved(states[states.Count-2],recovered.Checkpoint,c);
            }
            if(future) Assert.That(target.HasHistory,Is.False);
            Assert.That(File.ReadAllText(source.CurrentPath),Is.EqualTo(bytes)); Assert.That(File.ReadAllBytes(source.PreviousPath),Is.EqualTo(previous));
        }
        [Test] public void ExistingActSevenCheckpointWinsBeforeFutureSourceIsRead()
        {
            var c=Continuation(6); string sourceFolder=CampaignEndingTests.Folder(); Directory.CreateDirectory(sourceFolder);
            string path=Path.Combine(sourceFolder,"current.json"); File.WriteAllText(path,"{\"schemaVersion\":999}");
            var target=new CampaignSaveStore(CampaignEndingTests.Folder(),c.ExpandedContent); var checkpoint=c.Convert(CampaignSaveCodec.Encode(CampaignEndingTests.History(6).Last(),c.SourceContent));
            Task.Run(()=>target.ImportAsync(checkpoint)).GetAwaiter().GetResult(); var bytes=File.ReadAllBytes(target.CurrentPath);
            var loaded=Task.Run(()=>c.LoadOrImportAsync(sourceFolder,target)).GetAwaiter().GetResult(); Assert.That(loaded.Checkpoint.Sequence,Is.EqualTo(checkpoint.Sequence));
            Assert.That(File.ReadAllBytes(target.CurrentPath),Is.EqualTo(bytes)); Assert.That(File.ReadAllText(path),Is.EqualTo("{\"schemaVersion\":999}"));
        }
    }
}
