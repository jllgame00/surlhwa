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
    public sealed class CampaignEndingTests
    {
        internal static string Json(int act=7)=>File.ReadAllText(Path.Combine(SliceContentTests.ContentRoot(),"Campaign/act"+act+".json"));
        internal static string Folder()=>Path.GetFullPath(Path.Combine(SliceContentTests.ContentRoot(),"../Validation/Evidence/act7-save-tests",Guid.NewGuid().ToString("N")));
        internal sealed class CaptureStore : ICampaignSaveStore
        {
            internal readonly List<CampaignCheckpoint> States=new List<CampaignCheckpoint>();
            internal bool Fail;
            public Task SaveAsync(CampaignCheckpoint state)
            { if(Fail) throw new IOException("injected ending publication failure"); States.Add(state); return Task.CompletedTask; }
        }
        private static readonly Dictionary<string,List<CampaignCheckpoint>> Histories=new Dictionary<string,List<CampaignCheckpoint>>();
        internal static List<CampaignCheckpoint> History(int act=7,string choice="keep",bool quiet=false,bool skipDryMartini=false)
        {
            string key=act+":"+choice+":"+quiet+":"+skipDryMartini; if(Histories.TryGetValue(key,out var cached)) return cached;
            var c=CampaignContentLoader.Load(Json(act)); var store=new CaptureStore(); var s=CampaignSession.StartAsync(c,store).GetAwaiter().GetResult();
            var route=ActFourContentTests.Route.Concat(new[]{"recipe_dry_martini","recipe_gin_fizz","recipe_manhattan","recipe_negroni",
                "recipe_negroni","recipe_dry_martini","recipe_manhattan","recipe_last_word","recipe_last_word","recipe_dry_martini",
                "recipe_negroni","recipe_negroni","recipe_dry_martini",quiet ? null : "recipe_dry_martini"}).ToArray();
            for(int slot=0;slot<c.Slots.Count;slot++) {
                for(int guard=0;s.State.Phase!=CampaignPhase.Brewing && guard<40;guard++) {
                    if(s.State.Phase==CampaignPhase.Arriving) s.TickAsync(2).GetAwaiter().GetResult();
                    else if(s.State.Phase==CampaignPhase.Choice) s.ChooseAsync(choice,++CampaignFlowTests.Event).GetAwaiter().GetResult();
                    else CampaignFlowTests.Advance(s);
                }
                Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Brewing)); Assert.That(s.State.SlotIndex,Is.EqualTo(slot));
                if(s.CurrentEncounter.RequiredDiscovery!=null) {
                    Serve(s,null); for(int guard=0;s.State.Phase!=CampaignPhase.Retry && guard<8;guard++) CampaignFlowTests.Advance(s);
                    Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Retry)); CampaignFlowTests.Advance(s);
                }
                string recipe=route[slot]; if(skipDryMartini && recipe=="recipe_dry_martini") recipe=slot<5 ? "recipe_test_stir" : "recipe_manwol";
                Serve(s,quiet && (s.CurrentEncounter.PrinceVisit==PrinceVisit.Warmth || s.CurrentEncounter.PrinceVisit==PrinceVisit.Clarity) ? null : recipe);
                CampaignFlowTests.ReachDeparture(s); s.TickAsync(2).GetAwaiter().GetResult(); Assert.That(s.SaveError,Is.Null);
            }
            for(int guard=0;s.State.Phase!=CampaignPhase.Complete && guard<12;guard++) CampaignFlowTests.Advance(s);
            Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Complete)); s.Dispose(); Histories.Add(key,store.States); return store.States;
        }
        internal static void Serve(CampaignSession s,string recipe)
        {
            s.ReleaseHeldInput(); Assert.That(s.ExecuteBrew(++CampaignFlowTests.Event,b=> {
                if(recipe==null) b.Pour("water",ContainerRole.Serving,1m); else ActSixContentTests.Make(b,recipe,s.Content);
            }),Is.True); CampaignFlowTests.Advance(s); Assert.That(s.SaveError,Is.Null);
        }
        internal static JObject Payload(CampaignCheckpoint s,CampaignContent c)=>(JObject)JObject.Parse(CampaignSaveCodec.Encode(s,c))["payload"];
        private static string[] EndingReceipts(CampaignCheckpoint s)=>s.ConsumedLines.Where(x=>x.Contains(":Ending")).ToArray();
        private static CampaignCheckpoint RoundTrip(CampaignCheckpoint s,CampaignContent c)=>CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(s,c),c);
        [Test] public void EndingPhasesAppendWithoutRenumberingAcceptedPhases()
        {
            Assert.That((int)CampaignPhase.Welcome,Is.EqualTo(0));
            Assert.That((int)CampaignPhase.Complete,Is.EqualTo(15));
            Assert.That(Enum.GetNames(typeof(CampaignPhase)).Skip(16),Is.EqualTo(new[]{"EndingReady","EndingEntered","EndingReading"}));
        }
        [TestCase("keep",false,"text_rc1_0412","star_glowing")]
        [TestCase("seek",true,"text_rc1_0413","star_unlit")]
        public void EveryEndingBoundaryResumesFirstUnreadLineAndPreservesHistory(string choice,bool quiet,string recall,string memory)
        {
            var c=CampaignContentLoader.Load(Json()); var history=History(7,choice,quiet);
            var ready=history.Single(x=>x.Phase==CampaignPhase.EndingReady); var before=history.Last(x=>x.Phase==CampaignPhase.Departing);
            Assert.That(ready.Sequence,Is.EqualTo(before.Sequence+1)); Assert.That(ready.CompletedOccurrences.Count,Is.EqualTo(47));
            Assert.That(ready.Memories.Count,Is.EqualTo(7)); Assert.That(ready.Memories[0].Variant,Is.EqualTo(memory));
            Assert.That(ready.Memories.Last().Id,Is.EqualTo("mem_07")); Assert.That(ready.Memories.Last().Variant,Is.EqualTo("anchor_ship_fragment"));
            Assert.That(ready.Memories.Last().Sequence,Is.EqualTo(ready.Sequence)); Assert.That(ready.Memories.Last().OccurrenceId,Is.EqualTo(ready.RunId+":46"));
            Assert.That(ready.Services.Last().Outcome.Fits,Is.EqualTo(!quiet)); Assert.That(EndingReceipts(ready),Is.Empty);
            var ids=new[]{"text_rc1_0001","text_rc1_0002",recall,"text_rc1_0003","text_rc1_0004","text_rc1_0005","text_rc1_0006","text_rc1_0007","text_rc1_0008"};
            var readings=history.Where(x=>x.Phase==CampaignPhase.EndingEntered || x.Phase==CampaignPhase.EndingReading).ToArray();
            Assert.That(readings.Length,Is.EqualTo(9));
            for(int i=0;i<readings.Length;i++) {
                using(var resumed=CampaignSession.Continue(c,new CaptureStore(),RoundTrip(readings[i],c))) {
                    Assert.That(resumed.State.LineIndex,Is.EqualTo(i)); Assert.That(resumed.CurrentText.Id,Is.EqualTo(ids[i])); Assert.That(resumed.Brew,Is.Null);
                    Assert.That(EndingReceipts(resumed.State).Length,Is.EqualTo(i)); Assert.That(resumed.State.SlotIndex,Is.EqualTo(46));
                    Assert.That(resumed.State.SelectedEncounters.Count,Is.EqualTo(47)); Assert.That(resumed.CurrentEncounter.Night,Is.EqualTo(20));
                    foreach(string field in new[]{"services","knownReceipts","facts","memories","completedOccurrences","selectedEncounters","princeChoice","farewell"})
                        Assert.That(JToken.DeepEquals(Payload(ready,c)[field],Payload(resumed.State,c)[field]),Is.True,field);
                    CampaignFlowTests.Advance(resumed); Assert.That(resumed.SaveError,Is.Null);
                    Assert.That(CampaignSaveCodec.Encode(resumed.State,c),Is.EqualTo(CampaignSaveCodec.Encode(history[history.IndexOf(readings[i])+1],c)));
                }
            }
            var complete=history.Last(); Assert.That(EndingReceipts(complete).Length,Is.EqualTo(9)); Assert.That(complete.LineIndex,Is.Zero);
            using(var resumed=CampaignSession.Continue(c,new CaptureStore(),RoundTrip(complete,c))) {
                string json=CampaignSaveCodec.Encode(complete,c); Assert.That(resumed.CurrentText.Id,Is.EqualTo("text_rc1_0008"));
                CampaignFlowTests.Advance(resumed); resumed.TickAsync(500).GetAwaiter().GetResult();
                Assert.That(CampaignSaveCodec.Encode(resumed.State,c),Is.EqualTo(json)); Assert.That(resumed.Brew,Is.Null);
            }
            using(var resumed=CampaignSession.Continue(c,new CaptureStore(),RoundTrip(ready,c))) {
                Assert.That(resumed.CurrentText,Is.Null); resumed.TickAsync(500).GetAwaiter().GetResult(); Assert.That(resumed.State.Sequence,Is.EqualTo(ready.Sequence));
                CampaignFlowTests.Advance(resumed); Assert.That(resumed.State.Phase,Is.EqualTo(CampaignPhase.EndingEntered));
                Assert.That(resumed.CurrentText.Id,Is.EqualTo("text_rc1_0001")); Assert.That(EndingReceipts(resumed.State),Is.Empty);
            }
        }
        [Test] public void EveryEndingPublicationFailureRetriesTheSameTransactionWithoutDuplicateMemoryOrReading()
        {
            var c=CampaignContentLoader.Load(Json()); var history=History(); var start=history.Last(x=>x.Phase==CampaignPhase.Departing);
            var boundaries=history.Skip(history.IndexOf(start)).ToArray();
            for(int i=0;i<boundaries.Length-1;i++) {
                var store=new CaptureStore {Fail=true}; using(var s=CampaignSession.Continue(c,store,RoundTrip(boundaries[i],c))) {
                    if(s.State.Phase==CampaignPhase.Departing) s.TickAsync(2).GetAwaiter().GetResult(); else CampaignFlowTests.Advance(s);
                    Assert.That(s.SaveError,Is.Not.Null);
                    Assert.That(CampaignSaveCodec.Encode(s.State,c),Is.EqualTo(CampaignSaveCodec.Encode(boundaries[i],c)));
                    CampaignFlowTests.Advance(s); Assert.That(s.State.Sequence,Is.EqualTo(boundaries[i].Sequence));
                    store.Fail=false; Assert.That(s.RetrySaveAsync().GetAwaiter().GetResult(),Is.True);
                    Assert.That(CampaignSaveCodec.Encode(s.State,c),Is.EqualTo(CampaignSaveCodec.Encode(boundaries[i+1],c)));
                    Assert.That(s.RetrySaveAsync().GetAwaiter().GetResult(),Is.False); Assert.That(s.State.Memories.Count(x=>x.Id=="mem_07"),Is.EqualTo(1));
                }
            }
        }
        [Test] public void EndingDoesNotRequireEveryRecipeToBeKnown()
        {
            var c=CampaignContentLoader.Load(Json()); var history=History(skipDryMartini:true);
            var ready=history.Single(x=>x.Phase==CampaignPhase.EndingReady);
            Assert.That(ready.KnownReceipts.ContainsKey("recipe_dry_martini"),Is.False);
            Assert.That(RoundTrip(history.Last(),c).Phase,Is.EqualTo(CampaignPhase.Complete));
            Assert.That(history.Last().KnownReceipts.ContainsKey("recipe_dry_martini"),Is.False);
        }
        [Test] public void MissingHistoricalChoiceReadsNoneRecallWithoutInventingChoiceOrMemory()
        {
            var c=CampaignContentLoader.Load(Json()); var ready=History(quiet:true).Single(x=>x.Phase==CampaignPhase.EndingReady);
            // Compatibility fixture for an absent historical choice; no player input path is claimed.
            var consumed=ready.ConsumedLines.Where(x=>!x.Contains(":ChoiceResponse:")).Select(receipt=> {
                var p=receipt.Split(':'); var encounter=c.Encounters[ready.SelectedEncounters[int.Parse(p[1])]];
                if(p[2]=="Intro" && encounter.ChoiceIntros.TryGetValue("none",out var lines)) p[4]=lines[int.Parse(p[3])];
                if(p[2]=="Farewell" && encounter.PrinceVisit==PrinceVisit.Clarity && p[3]=="0") p[4]="text_rc1_0414";
                return string.Join(":",p);
            });
            var absent=new CampaignCheckpoint(ready.Sequence,ready.RunId,ready.SlotIndex,ready.Phase,0,null,ready.Farewell,
                ready.SelectedEncounters,consumed,ready.CompletedOccurrences,ready.Services,ready.Memories,
                ready.KnownReceipts.ToDictionary(x=>x.Key,x=>x.Value),ready.Facts.ToDictionary(x=>x.Key,x=>x.Value.ToArray()));
            var store=new CaptureStore(); using(var s=CampaignSession.Continue(c,store,RoundTrip(absent,c))) {
                CampaignFlowTests.Advance(s); CampaignFlowTests.Advance(s); CampaignFlowTests.Advance(s);
                Assert.That(s.State.LineIndex,Is.EqualTo(2)); Assert.That(s.CurrentText.Id,Is.EqualTo("text_rc1_0414"));
                using(var resumed=CampaignSession.Continue(c,store,RoundTrip(s.State,c))) {
                    Assert.That(resumed.CurrentText.Id,Is.EqualTo("text_rc1_0414"));
                    for(int guard=0;resumed.State.Phase!=CampaignPhase.Complete && guard<8;guard++) CampaignFlowTests.Advance(resumed);
                    Assert.That(RoundTrip(resumed.State,c).Phase,Is.EqualTo(CampaignPhase.Complete)); Assert.That(resumed.State.PrinceChoice,Is.Null);
                    Assert.That(resumed.State.Memories.First().Variant,Is.EqualTo("star_unlit"));
                    Assert.That(JToken.DeepEquals(Payload(absent,c)["memories"],Payload(resumed.State,c)["memories"]),Is.True);
                }
            }
        }
        [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)]
        public void EarlierScopesRejectEndingPhasesAndKeepTheirOriginalCompleteBehavior(int act)
        {
            var c=CampaignContentLoader.Load(Json(act)); var complete=History(act).Last(); Assert.That(c.Ending,Is.Null);
            foreach(var phase in new[]{CampaignPhase.EndingReady,CampaignPhase.EndingEntered,CampaignPhase.EndingReading}) {
                var forged=new CampaignCheckpoint(complete.Sequence,complete.RunId,complete.SlotIndex,phase,phase==CampaignPhase.EndingReading ? 1 : 0,
                    complete.PrinceChoice,complete.Farewell,complete.SelectedEncounters,complete.ConsumedLines,complete.CompletedOccurrences,complete.Services,complete.Memories,
                    complete.KnownReceipts.ToDictionary(x=>x.Key,x=>x.Value),complete.Facts.ToDictionary(x=>x.Key,x=>x.Value.ToArray()));
                Assert.Throws<ContentValidationException>(()=>CampaignSaveCodec.Encode(forged,c));
            }
            using(var s=CampaignSession.Continue(c,new CaptureStore(),RoundTrip(complete,c))) {
                Assert.That(s.CurrentText,Is.Null); CampaignFlowTests.Advance(s); s.TickAsync(2).GetAwaiter().GetResult();
                Assert.That(s.State.Sequence,Is.EqualTo(complete.Sequence));
            }
        }
        [TestCase("premature")][TestCase("missing")][TestCase("skipped")][TestCase("repeated")][TestCase("wrong_choice")]
        [TestCase("foreign_occurrence")][TestCase("wrong_phase")][TestCase("ready_receipt")][TestCase("entered_cursor")]
        [TestCase("reading_zero")][TestCase("reading_too_far")][TestCase("complete_cursor")][TestCase("no_departure")]
        [TestCase("no_memory")][TestCase("wrong_order")][TestCase("noncanonical_cursor")][TestCase("foreign_text")]
        public void CodecRejectsForgedOrContradictoryEndingProgress(string fault)
        {
            var c=CampaignContentLoader.Load(Json()); var history=History(); var complete=history.Last();
            var checkpoint=fault=="premature" ? history.Last(x=>x.Phase==CampaignPhase.Departing) :
                fault=="ready_receipt" ? history.Single(x=>x.Phase==CampaignPhase.EndingReady) :
                fault=="entered_cursor" ? history.Single(x=>x.Phase==CampaignPhase.EndingEntered) :
                fault=="reading_zero" || fault=="reading_too_far" ? history.First(x=>x.Phase==CampaignPhase.EndingReading) : complete;
            var root=JObject.Parse(CampaignSaveCodec.Encode(checkpoint,c)); var p=(JObject)root["payload"]; var receipts=(JArray)p["consumedLines"];
            int first=receipts.Count-EndingReceipts(checkpoint).Length; string receipt=complete.RunId+":46:EndingEntered:0:text_rc1_0001";
            if(fault=="premature" || fault=="ready_receipt") receipts.Add(receipt);
            if(fault=="missing") receipts.RemoveAt(receipts.Count-1);
            if(fault=="skipped") receipts.RemoveAt(first+2);
            if(fault=="repeated") receipts.Add(receipts[first].DeepClone());
            if(fault=="wrong_choice") receipts[first+2]=complete.RunId+":46:EndingReading:2:text_rc1_0413";
            if(fault=="foreign_occurrence") receipts[first]=complete.RunId+":45:EndingEntered:0:text_rc1_0001";
            if(fault=="wrong_phase") receipts[first]=complete.RunId+":46:EndingReading:0:text_rc1_0001";
            if(fault=="entered_cursor" || fault=="complete_cursor") p["lineIndex"]=1;
            if(fault=="reading_zero") p["lineIndex"]=0;
            if(fault=="reading_too_far") p["lineIndex"]=9;
            if(fault=="no_departure") ((JArray)p["completedOccurrences"]).RemoveAt(46);
            if(fault=="no_memory") ((JArray)p["memories"]).RemoveAt(6);
            if(fault=="wrong_order") { var value=receipts[first].DeepClone(); receipts[first]=receipts[first+1].DeepClone(); receipts[first+1]=value; }
            if(fault=="noncanonical_cursor") receipts[first]=complete.RunId+":46:EndingEntered:00:text_rc1_0001";
            if(fault=="foreign_text") receipts[first]=complete.RunId+":46:EndingEntered:0:text_rc1_0002";
            using(var hash=System.Security.Cryptography.SHA256.Create()) root["payloadSha256"]=BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(p.ToString(Newtonsoft.Json.Formatting.None)))).Replace("-","").ToLowerInvariant();
            Assert.Throws<ContentValidationException>(()=>CampaignSaveCodec.Decode(root.ToString(),c));
        }
        [TestCase("before_write")][TestCase("after_write")][TestCase("after_flush")][TestCase("after_validate")][TestCase("after_replace")][TestCase("after_final_read")]
        public void EndingStoreFaultKeepsAValidatedGenerationAndRetryDoesNotPublishTwice(string fault)
        {
            var c=CampaignContentLoader.Load(Json()); var history=History();
            var before=history.First(x=>x.Phase==CampaignPhase.EndingReading && x.LineIndex==2); var next=history[history.IndexOf(before)+1];
            var store=new CampaignSaveStore(Folder(),c); Task.Run(()=>store.SaveAsync(before)).GetAwaiter().GetResult();
            store.Fault=point=> {if(point==fault) throw new IOException("injected "+fault);};
            using(var s=CampaignSession.Continue(c,store,before)) {
                CampaignFlowTests.Advance(s); Assert.That(s.SaveError,Is.Not.Null); Assert.That(s.State.Sequence,Is.EqualTo(before.Sequence));
                var loaded=store.Load(); Assert.That(loaded.Checkpoint.Sequence,Is.EqualTo(fault=="after_replace" || fault=="after_final_read" ? next.Sequence : before.Sequence));
                store.Fault=null; Assert.That(Task.Run(()=>s.RetrySaveAsync()).GetAwaiter().GetResult(),Is.True);
                string bytes=File.ReadAllText(store.CurrentPath); Assert.That(bytes,Is.EqualTo(CampaignSaveCodec.Encode(next,c)));
                Assert.That(CampaignSaveCodec.Decode(File.ReadAllText(store.PreviousPath),c).Sequence,Is.EqualTo(before.Sequence));
                Assert.That(s.RetrySaveAsync().GetAwaiter().GetResult(),Is.False); Assert.That(File.ReadAllText(store.CurrentPath),Is.EqualTo(bytes));
            }
        }
    }
}
