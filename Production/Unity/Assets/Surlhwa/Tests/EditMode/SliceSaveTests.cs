using System;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using Surlhwa.Domain;
using Surlhwa.ContentIO;

namespace Surlhwa.Tests
{
    public sealed class SliceSaveTests
    {
        private static string Folder() { var path=Path.Combine(SliceContentTests.ContentRoot(),"../Validation/Evidence/save-tests",Guid.NewGuid().ToString("N")); Directory.CreateDirectory(path); return path; }
        [Test] public void CodecRoundTripsActualServiceWithoutReEvaluation_AndRejectsAlteration()
        {
            var s=SliceFlowTests.Start(); SliceFlowTests.ToBrew(s); SliceFlowTests.Make(s,"recipe_manhattan"); SliceFlowTests.Advance(s);
            var json=SliceSaveCodec.Encode(s.State,s.Content); var restored=SliceSaveCodec.Decode(json,s.Content);
            Assert.That(restored.CurrentService.Snapshot.TotalMl,Is.EqualTo(71m));
            Assert.That(restored.CurrentService.Snapshot.Trace.Count,Is.EqualTo(s.State.CurrentService.Snapshot.Trace.Count));
            Assert.That(restored.CurrentService.Outcome.Fits,Is.True);
            var changed=JObject.Parse(json); changed["payload"]["phase"]="Complete";
            Assert.Throws<ContentValidationException>(()=>SliceSaveCodec.Decode(changed.ToString(),s.Content));
        }
        [Test] public void PreviousRecoveryIsExplicit_AndFutureCurrentNeverSilentlyRollsBack()
        {
            var content=SliceFlowTests.Content(); var store=new SliceSaveStore(Folder(),content);
            var s=Task.Run(()=>Surlhwa.Application.SliceSession.StartAsync(content,store)).GetAwaiter().GetResult(); SliceFlowTests.Advance(s);
            File.WriteAllText(store.CurrentPath,"broken"); var read=store.Load();
            Assert.That(read.RecoveryRequired,Is.True); Assert.That(read.Checkpoint.Phase,Is.EqualTo(SlicePhase.Welcome));
            var future=JObject.Parse(SliceSaveCodec.Encode(s.State,content)); future["schemaVersion"]=999; File.WriteAllText(store.CurrentPath,future.ToString());
            Assert.Throws<UnsupportedSaveVersionException>(()=>store.Load());
        }
        [TestCase("before_write")][TestCase("after_write")][TestCase("after_flush")][TestCase("after_validate")][TestCase("after_replace")][TestCase("after_final_read")]
        public void EveryInterruptedWriteRetainsAValidGeneration(string boundary)
        {
            var content=SliceFlowTests.Content(); var folder=Folder(); var store=new SliceSaveStore(folder,content);
            var s=Task.Run(()=>Surlhwa.Application.SliceSession.StartAsync(content,store)).GetAwaiter().GetResult(); SliceFlowTests.Advance(s);
            store.Fault=point=> { if(point==boundary) throw new IOException("injected "+point); };
            Assert.That(Task.Run(()=>s.TickAsync(2)).GetAwaiter().GetResult(),Is.False);
            var read=new SliceSaveStore(folder,content).Load(); Assert.That(read.Checkpoint,Is.Not.Null);
            store.Fault=null; Assert.That(Task.Run(()=>s.RetrySaveAsync()).GetAwaiter().GetResult(),Is.True);
            Assert.That(store.Load().Checkpoint.Sequence,Is.EqualTo(s.State.Sequence));
        }
        [Test] public void RecoveredSavePreservesCorruptBytesAndGoodPrevious()
        {
            var content=SliceFlowTests.Content(); var store=new SliceSaveStore(Folder(),content);
            var s=Task.Run(()=>Surlhwa.Application.SliceSession.StartAsync(content,store)).GetAwaiter().GetResult(); SliceFlowTests.Advance(s);
            File.WriteAllText(store.CurrentPath,"corrupt-original"); var recovered=store.Load();
            var resumed=Surlhwa.Application.SliceSession.Continue(content,store,recovered.Checkpoint); SliceFlowTests.Advance(resumed);
            Assert.That(File.ReadAllText(store.PreviousPath),Does.Contain("Welcome"));
            Assert.That(Directory.GetFiles(Path.GetDirectoryName(store.CurrentPath),"corrupt-*.json").Length,Is.EqualTo(1));
            Assert.That(store.Load().RecoveryRequired,Is.False);
        }
        [TestCase("unknown_recipe")][TestCase("future_content")][TestCase("unearned_memory")][TestCase("duplicate_service")][TestCase("unearned_fact")]
        public void StructurallyValidButIncompatibleProgressIsRejected(string fault)
        {
            var s=SliceFlowTests.Start(); SliceFlowTests.ToBrew(s); SliceFlowTests.Make(s,"recipe_manhattan"); SliceFlowTests.Advance(s);
            var root=JObject.Parse(SliceSaveCodec.Encode(s.State,s.Content)); var p=(JObject)root["payload"];
            if(fault=="unknown_recipe") p["services"][0]["outcome"]["recipeId"]="missing_recipe";
            if(fault=="future_content") root["contentRevision"]="future_content";
            if(fault=="unearned_memory") ((JArray)p["memoryIds"]).Add("MEM-01");
            if(fault=="duplicate_service") ((JArray)p["services"]).Add(p["services"][0].DeepClone());
            if(fault=="unearned_fact") ((JArray)p["facts"]["recipe_dry_martini"]).Add("identity");
            using(var hash=SHA256.Create()) root["payloadSha256"]=BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(p.ToString(Newtonsoft.Json.Formatting.None)))).Replace("-","").ToLowerInvariant();
            Assert.Throws<ContentValidationException>(()=>SliceSaveCodec.Decode(root.ToString(),s.Content));
        }
        [Test] public void OlderSequenceAndConflictingRetryCannotOverwriteCommittedGeneration()
        {
            var content=SliceFlowTests.Content(); var store=new SliceSaveStore(Folder(),content);
            var s=Task.Run(()=>Surlhwa.Application.SliceSession.StartAsync(content,store)).GetAwaiter().GetResult(); var first=s.State;
            SliceFlowTests.Advance(s); string before=File.ReadAllText(store.CurrentPath);
            Assert.Throws<InvalidOperationException>(()=>store.SaveAsync(first).GetAwaiter().GetResult());
            Assert.That(File.ReadAllText(store.CurrentPath),Is.EqualTo(before));
        }
    }
}
