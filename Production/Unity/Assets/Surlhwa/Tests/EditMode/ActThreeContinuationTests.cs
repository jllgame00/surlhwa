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
    public sealed class ActThreeContinuationTests
    {
        private static string Json(int act) => File.ReadAllText(Path.Combine(SliceContentTests.ContentRoot(),"Campaign/act"+act+".json"));
        private static string Folder() => Path.GetFullPath(Path.Combine(SliceContentTests.ContentRoot(),"../Validation/Evidence/act3-continuation-tests",Guid.NewGuid().ToString("N")));
        private sealed class CaptureStore : ICampaignSaveStore
        {
            private readonly CampaignContent content;
            internal readonly List<CampaignCheckpoint> States=new List<CampaignCheckpoint>();
            internal CaptureStore(CampaignContent content) { this.content=content; }
            public Task SaveAsync(CampaignCheckpoint checkpoint)
            { States.Add(CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(checkpoint,content),content)); return Task.CompletedTask; }
        }
        private static List<CampaignCheckpoint> Checkpoints(CampaignContent content)
        {
            var store=new CaptureStore(content);
            using(var session=CampaignSession.StartAsync(content,store).GetAwaiter().GetResult())
            {
                string[] recipes={null,"recipe_test_stir","recipe_test_stir","recipe_dry_martini","recipe_dry_martini","recipe_manwol",
                    "recipe_negroni","recipe_test_stir","recipe_negroni","recipe_manhattan",null,"recipe_test_stir","recipe_negroni",
                    "recipe_gin_fizz","recipe_dry_martini","recipe_gin_fizz","recipe_dry_martini","recipe_test_stir","recipe_manhattan",
                    "recipe_dry_martini","recipe_negroni","recipe_hanky_panky","recipe_manhattan","recipe_gin_fizz",null};
                for(int slot=0;slot<content.Slots.Count;slot++)
                {
                    CampaignFlowTests.ReachBrew(session); Assert.That(session.State.SlotIndex,Is.EqualTo(slot));
                    if(slot==6 || slot==21) {
                        Assert.That(session.CurrentEncounter.RequiredDiscovery,Is.Not.Null);
                        CampaignFlowTests.Serve(session,null); CampaignFlowTests.ReachBrew(session);
                        Assert.That(session.State.SlotIndex,Is.EqualTo(slot));
                    }
                    session.ReleaseHeldInput(); session.ExecuteBrew(++CampaignFlowTests.Event,b=> {
                        if(recipes[slot]==null) b.Pour("water",ContainerRole.Serving,1m);
                        else if(recipes[slot]=="recipe_hanky_panky") {
                            foreach(var amount in content.Catalog.Recipes[recipes[slot]].Amounts) b.Pour(amount.Key,ContainerRole.Mixing,amount.Value);
                            b.AddIce(ContainerRole.Mixing); BartendingDomainTests.Circles(b);
                            b.Transfer(ContainerRole.Mixing,ContainerRole.Serving,180m,BrewAction.Strain); b.Garnish("orange_zest","rim");
                        }
                        else CampaignContentTests.Make(b,recipes[slot]);
                    });
                    CampaignFlowTests.Advance(session); Assert.That(session.SaveError,Is.Null);
                    CampaignFlowTests.ReachDeparture(session); session.TickAsync(2).GetAwaiter().GetResult(); Assert.That(session.SaveError,Is.Null);
                }
                Assert.That(session.State.Phase,Is.EqualTo(CampaignPhase.Complete));
                Assert.That(session.State.Memories[0].Variant,Is.EqualTo("star_unlit"));
            }
            return store.States;
        }
        private static JObject Payload(CampaignCheckpoint state,CampaignContent content) => (JObject)JObject.Parse(CampaignSaveCodec.Encode(state,content))["payload"];
        private static void Preserved(CampaignCheckpoint before,CampaignCheckpoint after,CampaignContinuation continuation)
        {
            Assert.That(after.RunId,Is.EqualTo(before.RunId));
            var expected=Payload(before,continuation.SourceContent);
            if(before.Phase==CampaignPhase.Complete) {
                int next=continuation.SourceContent.Slots.Count;
                expected["slotIndex"]=next; expected["phase"]="NightEnd"; expected["lineIndex"]=0;
                ((JArray)expected["selectedEncounters"]).Add(continuation.ExpandedContent.Slots[next].EncounterId);
                Assert.That(after.Sequence,Is.EqualTo(before.Sequence+1));
            }
            else Assert.That(after.Sequence,Is.EqualTo(before.Sequence));
            Assert.That(JToken.DeepEquals(expected,Payload(after,continuation.ExpandedContent)),Is.True,
                "Changed source receipts or reading cursor at "+before.SlotIndex+" / "+before.Phase+" / "+before.LineIndex);
        }
        [TestCase(1)][TestCase(2)]
        public void ActThreeContinuationPreservesEverySourcePhaseAndAllOriginalReceipts(int sourceAct)
        {
            var continuation=new CampaignContinuation(Json(sourceAct),Json(3));
            var checkpoints=Checkpoints(continuation.SourceContent);
            Assert.That(checkpoints.Select(x=>x.Phase).Distinct(),Is.EquivalentTo(Enum.GetValues(typeof(CampaignPhase)).Cast<CampaignPhase>()));
            foreach(var before in checkpoints) {
                var after=continuation.Convert(CampaignSaveCodec.Encode(before,continuation.SourceContent));
                Preserved(before,after,continuation);
                if(before.Phase==CampaignPhase.Brewing || before.Phase==CampaignPhase.Reaction) {
                    using(var session=CampaignSession.Continue(continuation.ExpandedContent,new CampaignFlowTests.Store(),after)) {
                        if(before.Phase==CampaignPhase.Brewing) Assert.That(session.Brew.Container(ContainerRole.Serving).TotalMl,Is.Zero);
                        else { Assert.That(session.Brew,Is.Null); Assert.That(session.State.CurrentService.Snapshot.AttemptId,Is.EqualTo(before.CurrentService.Snapshot.AttemptId)); }
                    }
                }
            }
            var completed=continuation.Convert(CampaignSaveCodec.Encode(checkpoints.Last(),continuation.SourceContent));
            Assert.That(completed.SlotIndex,Is.EqualTo(sourceAct==1 ? 17 : 25)); Assert.That(completed.Phase,Is.EqualTo(CampaignPhase.NightEnd));
            Assert.That(completed.CurrentService,Is.Null); Assert.That(completed.Memories.Count,Is.EqualTo(sourceAct));
            if(sourceAct==2) {
                Assert.That(completed.Memories[1].Variant,Is.EqualTo("swing_empty_place"));
                Assert.That(completed.Services.Last().Outcome.Fits,Is.False);
                Assert.That(continuation.ExpandedContent.Encounters["enc_rc1_25"].GuestIds,Is.EqualTo(new[]{"guest_chunhyang","guest_mongryong"}));
                Assert.That(completed.KnownReceipts.ContainsKey("recipe_hanky_panky"),Is.True);
            }
        }
        [TestCase(1)][TestCase(2)] public void ActThreeImportRetainsBothAcceptedSourceFilesAndIsIdempotent(int sourceAct)
        {
            var continuation=new CampaignContinuation(Json(sourceAct),Json(3)); var checkpoints=Checkpoints(continuation.SourceContent);
            string sourceFolder=Folder(); var source=new CampaignSaveStore(sourceFolder,continuation.SourceContent);
            Task.Run(()=>source.SaveAsync(checkpoints[checkpoints.Count-2])).GetAwaiter().GetResult();
            Task.Run(()=>source.SaveAsync(checkpoints.Last())).GetAwaiter().GetResult();
            var current=File.ReadAllBytes(source.CurrentPath); var previous=File.ReadAllBytes(source.PreviousPath);
            var destination=new CampaignSaveStore(Folder(),continuation.ExpandedContent);
            var result=Task.Run(()=>continuation.LoadOrImportAsync(sourceFolder,destination)).GetAwaiter().GetResult();
            Preserved(checkpoints.Last(),result.Checkpoint,continuation); byte[] imported=File.ReadAllBytes(destination.CurrentPath);
            Task.Run(()=>continuation.LoadOrImportAsync(sourceFolder,destination)).GetAwaiter().GetResult();
            Assert.That(File.ReadAllBytes(source.CurrentPath),Is.EqualTo(current)); Assert.That(File.ReadAllBytes(source.PreviousPath),Is.EqualTo(previous));
            Assert.That(File.ReadAllBytes(destination.CurrentPath),Is.EqualTo(imported)); Assert.That(File.Exists(destination.PreviousPath),Is.False);
        }
        [TestCase("before_write")][TestCase("after_validate")][TestCase("after_replace")]
        public void ActTwoToThreeImportRetriesWithoutChangingOriginalMemoryOrSourceBytes(string fault)
        {
            var continuation=new CampaignContinuation(Json(2),Json(3)); var checkpoints=Checkpoints(continuation.SourceContent);
            string sourceFolder=Folder(); var source=new CampaignSaveStore(sourceFolder,continuation.SourceContent);
            Task.Run(()=>source.SaveAsync(checkpoints[checkpoints.Count-2])).GetAwaiter().GetResult();
            Task.Run(()=>source.SaveAsync(checkpoints.Last())).GetAwaiter().GetResult();
            var current=File.ReadAllBytes(source.CurrentPath); var previous=File.ReadAllBytes(source.PreviousPath);
            var destination=new CampaignSaveStore(Folder(),continuation.ExpandedContent);
            destination.Fault=point=> { if(point==fault) throw new IOException("injected "+point); };
            Assert.Throws<IOException>(()=>Task.Run(()=>continuation.LoadOrImportAsync(sourceFolder,destination)).GetAwaiter().GetResult());
            destination.Fault=null; var loaded=Task.Run(()=>continuation.LoadOrImportAsync(sourceFolder,destination)).GetAwaiter().GetResult();
            Preserved(checkpoints.Last(),loaded.Checkpoint,continuation);
            Assert.That(File.ReadAllBytes(source.CurrentPath),Is.EqualTo(current)); Assert.That(File.ReadAllBytes(source.PreviousPath),Is.EqualTo(previous));
            Assert.That(File.Exists(destination.PreviousPath),Is.False);
        }
        [TestCase("source_bytes")][TestCase("source_scope")][TestCase("old_text")][TestCase("old_farewell")]
        [TestCase("old_companion")][TestCase("old_memory")][TestCase("old_supply")]
        public void ActThreeContinuationRejectsUnapprovedSourceOrModifiedActTwoPrefix(string fault)
        {
            string source=Json(2); var expanded=JObject.Parse(Json(3));
            if(fault=="source_bytes") source+=" ";
            if(fault=="source_scope") { var changed=JObject.Parse(source); changed["act"]="unknown_source"; source=changed.ToString(); }
            if(fault=="old_text") expanded["texts"][80]["value"]="altered earlier authored wording";
            var reunion=expanded["encounters"].Single(x=>x["id"].Value<string>()=="enc_rc1_25");
            if(fault=="old_farewell") ((JArray)reunion["farewell"]).RemoveAt(0);
            if(fault=="old_companion") reunion["companionGuestId"]=null;
            if(fault=="old_memory") reunion["memoryVariant"]="altered_swing";
            if(fault=="old_supply") ((JArray)expanded["supplyGrants"]).Clear();
            Assert.Throws<ContentValidationException>(()=>new CampaignContinuation(source,expanded.ToString()));
        }
        [TestCase(2,1)][TestCase(2,2)][TestCase(3,3)] public void ContinuationRejectsBackwardSameOrUnacceptedSourceBoundary(int sourceAct,int targetAct)
        { Assert.Throws<ContentValidationException>(()=>new CampaignContinuation(Json(sourceAct),Json(targetAct))); }
        [Test] public void ActTwoSaveCannotUseUnknownHashOrScopeDuringActThreeContinuation()
        {
            var continuation=new CampaignContinuation(Json(2),Json(3));
            using(var session=CampaignSession.StartAsync(continuation.SourceContent,new CampaignFlowTests.Store()).GetAwaiter().GetResult()) {
                var saved=JObject.Parse(CampaignSaveCodec.Encode(session.State,continuation.SourceContent));
                saved["contentHash"]=new string('0',64); Assert.Throws<ContentValidationException>(()=>continuation.Convert(saved.ToString()));
                saved["contentHash"]=continuation.SourceContent.BundleHash; saved["act"]="through_act_03";
                Assert.Throws<ContentValidationException>(()=>continuation.Convert(saved.ToString()));
            }
        }
    }
}
