using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Surlhwa.Domain;

namespace Surlhwa.Application
{
    public interface ISliceSaveStore { Task SaveAsync(SliceCheckpoint checkpoint); }
    public sealed class SliceSession : IDisposable
    {
        private readonly ISliceSaveStore store; private readonly InputGate input=new InputGate();
        private SliceCheckpoint pending; private double stageElapsed; private bool disposed;
        public SliceContent Content { get; } public SliceCheckpoint State { get; private set; }
        public BrewingSession Brew { get; private set; } public RecipeKnowledge Knowledge { get; private set; }
        public ModalStack Modals { get; }=new ModalStack(); public bool IsSaving { get; private set; }
        public string SaveError { get; private set; } public bool Focused { get; private set; }=true;
        public bool CanInteract => !disposed && Focused && !IsSaving && SaveError==null && Modals.Count==0;
        public SliceEncounter CurrentEncounter => Content.Encounters[State.EncounterIndex];
        public SourcedText CurrentText
        {
            get
            {
                string id=State.Phase==SlicePhase.Intro ? CurrentEncounter.Intro[State.LineIndex] :
                    State.Phase==SlicePhase.Farewell ? CurrentEncounter.Farewell[State.LineIndex] :
                    State.Phase==SlicePhase.Reaction ? CurrentEncounter.Responses[State.CurrentService.ResponseKey] :
                    State.Phase==SlicePhase.Request ? Content.Requests[CurrentEncounter.RequestId].SummaryTextId : null;
                return id==null ? null : Content.Texts[id];
            }
        }
        private SliceSession(SliceContent content,ISliceSaveStore store,SliceCheckpoint state)
        { Content=content; this.store=store; State=state; Apply(state); }
        public static async Task<SliceSession> StartAsync(SliceContent content,ISliceSaveStore store)
        {
            var k=content.CreateKnowledge(); var state=new SliceCheckpoint(1,Guid.NewGuid().ToString("N"),0,SlicePhase.Welcome,0,
                Array.Empty<string>(),Array.Empty<string>(),Array.Empty<CommittedService>(),k.KnownReceipts.ToDictionary(x=>x.Key,x=>x.Value),k.Facts.ToDictionary(x=>x.Key,x=>x.Value));
            var session=new SliceSession(content,store,state); await session.Publish(state); return session;
        }
        public static SliceSession Continue(SliceContent content,ISliceSaveStore store,SliceCheckpoint state) => new SliceSession(content,store,state);
        private void Apply(SliceCheckpoint state)
        {
            State=state; Knowledge=new RecipeKnowledge(Content.Catalog);
            foreach(var k in state.KnownReceipts) Knowledge.SeedKnown(k.Key,k.Value);
            foreach(var f in state.Facts) foreach(var value in f.Value) Knowledge.GrantFact(f.Key,value);
            Brew=state.Phase==SlicePhase.Brewing ? new BrewingSession(Content.Catalog,Content.Tuning,Content.Profile.Supply) : null;
            stageElapsed=0;
        }
        public void OpenModal(string id) { CancelHeld(); Modals.Push(id); }
        public void CloseModal() { CancelHeld(); Modals.Pop(); }
        public void SetFocus(bool focused) { Focused=focused; CancelHeld(); }
        public void CancelHeld() { Brew?.CancelInput(); input.CancelHeld(); }
        public void ReleaseHeldInput() { Brew?.CancelInput(); input.Release(); }
        public bool ExecuteBrew(long id,Action<BrewingSession> action)
        {
            if(!input.Consume(id) || !CanInteract || State.Phase!=SlicePhase.Brewing || input.HeldBlocked) return false;
            action(Brew); return true;
        }
        public bool UpdateBrew(long microseconds,bool fine)
        {
            if(!CanInteract || input.HeldBlocked || State.Phase!=SlicePhase.Brewing) { Brew?.CancelInput(); return false; }
            Brew.UpdateFlow(microseconds,1m,fine); return true;
        }
        public bool DiscardBrew(long eventId)
        {
            if(!input.Consume(eventId) || !CanInteract || State.Phase!=SlicePhase.Brewing) return false;
            CancelHeld(); Brew=new BrewingSession(Content.Catalog,Content.Tuning,Content.Profile.Supply); return true;
        }
        public async Task<bool> TickAsync(double seconds)
        {
            if(double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds<0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if(!CanInteract || (State.Phase!=SlicePhase.Arriving && State.Phase!=SlicePhase.Departing)) return false;
            stageElapsed+=seconds; if(stageElapsed<1.5) return false;
            if(State.Phase==SlicePhase.Arriving) return await Publish(Copy(SlicePhase.Intro));
            var completed=State.CompletedOccurrences.Concat(new[]{State.OccurrenceId});
            bool last=State.EncounterIndex+1==Content.Encounters.Count;
            return await Publish(Copy(last ? SlicePhase.Complete : SlicePhase.Arriving,encounter:last ? State.EncounterIndex : State.EncounterIndex+1,completed:completed));
        }
        public async Task<bool> AdvanceAsync(long eventId)
        {
            if(!input.Consume(eventId) || !CanInteract) return false;
            var consumed=State.ConsumedLines.ToList(); string line=CurrentText?.Id;
            if(line!=null) { string receipt=State.OccurrenceId+":"+State.Phase+":"+State.LineIndex+":"+line; if(!consumed.Contains(receipt)) consumed.Add(receipt); }
            switch(State.Phase)
            {
                case SlicePhase.Welcome: return await Publish(Copy(SlicePhase.Arriving));
                case SlicePhase.Intro: return await Publish(Copy(State.LineIndex+1<CurrentEncounter.Intro.Count ? SlicePhase.Intro : SlicePhase.Request,
                    line:State.LineIndex+1<CurrentEncounter.Intro.Count ? State.LineIndex+1 : 0,consumed:consumed));
                case SlicePhase.Request: input.Release(); return await Publish(Copy(SlicePhase.Brewing,consumed:consumed));
                case SlicePhase.Brewing:
                    if(Brew.Container(ContainerRole.Serving).TotalMl<=0) return false;
                    CancelHeld(); var snapshot=Brew.Freeze();
                    var outcome=ServiceEvaluator.Evaluate(Content.Catalog,Content.Tuning,snapshot,new DrinkRequest(Content.Requests[CurrentEncounter.RequestId].Tag));
                    var knowledge=CloneKnowledge(); bool discovered=knowledge.Discover(outcome,State.OccurrenceId);
                    bool good=outcome.Preparation==PreparationResult.Good && outcome.QualityGood;
                    string response=outcome.Recognition!=DrinkRecognition.Recognized ? "unknown" : outcome.Fits ? (good ? "good" : "weak") : (good ? "mismatch" : "bothWeak");
                    var service=new CommittedService(State.OccurrenceId,snapshot,outcome,response,discovered);
                    return await Publish(Copy(SlicePhase.Reaction,services:State.Services.Concat(new[]{service}),knowledge:knowledge));
                case SlicePhase.Reaction: return await Publish(Copy(SlicePhase.Result,consumed:consumed));
                case SlicePhase.Result: return await Publish(Copy(State.CurrentService.Discovered ? SlicePhase.Discovery : SlicePhase.Farewell));
                case SlicePhase.Discovery: return await Publish(Copy(SlicePhase.Farewell));
                case SlicePhase.Farewell: return await Publish(Copy(State.LineIndex+1<CurrentEncounter.Farewell.Count ? SlicePhase.Farewell : SlicePhase.Departing,
                    line:State.LineIndex+1<CurrentEncounter.Farewell.Count ? State.LineIndex+1 : 0,consumed:consumed));
                default: return false;
            }
        }
        private RecipeKnowledge CloneKnowledge()
        { var k=new RecipeKnowledge(Content.Catalog); foreach(var x in Knowledge.KnownReceipts) k.SeedKnown(x.Key,x.Value); foreach(var f in Knowledge.Facts) foreach(var value in f.Value) k.GrantFact(f.Key,value); return k; }
        private SliceCheckpoint Copy(SlicePhase phase,int line=0,int? encounter=null,IEnumerable<string> consumed=null,IEnumerable<string> completed=null,IEnumerable<CommittedService> services=null,RecipeKnowledge knowledge=null)
        {
            var k=knowledge ?? Knowledge;
            return new SliceCheckpoint(State.Sequence+1,State.RunId,encounter ?? State.EncounterIndex,phase,line,consumed ?? State.ConsumedLines,
                completed ?? State.CompletedOccurrences,services ?? State.Services,k.KnownReceipts.ToDictionary(x=>x.Key,x=>x.Value),k.Facts.ToDictionary(x=>x.Key,x=>x.Value));
        }
        private async Task<bool> Publish(SliceCheckpoint next)
        {
            if(disposed || IsSaving) return false; IsSaving=true; pending=next;
            try { await store.SaveAsync(next); if(disposed) return false; Apply(next); pending=null; SaveError=null; return true; }
            catch(Exception e) { SaveError=e.Message; return false; }
            finally { IsSaving=false; }
        }
        public Task<bool> RetrySaveAsync() => pending==null || IsSaving || disposed ? Task.FromResult(false) : Publish(pending);
        public void Dispose() { disposed=true; CancelHeld(); }
    }
}
