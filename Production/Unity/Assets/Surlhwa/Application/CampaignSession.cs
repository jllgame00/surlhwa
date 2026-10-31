using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Surlhwa.Domain;

namespace Surlhwa.Application
{
    public interface ICampaignSaveStore { Task SaveAsync(CampaignCheckpoint checkpoint); }
    public sealed class CampaignSession : IDisposable,IBrewingSession
    {
        private readonly ICampaignSaveStore store; private readonly InputGate input=new InputGate();
        private CampaignCheckpoint pending; private double stageElapsed; private bool disposed;
        public CampaignContent Content { get; }
        public IReadOnlyCollection<string> Supply { get; private set; }
        public BrewingTuning Tuning => Content.Tuning;
        public bool ExtendedMethods => true;
        public CampaignCheckpoint State { get; private set; }
        public BrewingSession Brew { get; private set; }
        public RecipeKnowledge Knowledge { get; private set; }
        public string SaveError { get; private set; }
        public bool IsSaving { get; private set; }
        public bool Focused { get; private set; }=true;
        public ModalStack Modals { get; }=new ModalStack();
        public bool CanInteract => !disposed && Focused && !IsSaving && SaveError==null && Modals.Count==0;
        public CampaignEncounter CurrentEncounter => Content.Encounters[State.SelectedEncounters[State.SlotIndex]];
        public IReadOnlyList<CampaignLine> Intro => CurrentEncounter.ChoiceIntros.TryGetValue(State.PrinceChoice ?? "none",out var lines) ?
            Array.AsReadOnly(lines.Select(x=>new CampaignLine(x)).ToArray()) : CurrentEncounter.Intro;
        public IReadOnlyList<string> FarewellLines => CurrentEncounter.PrinceVisit==PrinceVisit.Clarity && State.Farewell.HasValue ?
            Array.AsReadOnly(new[]{Content.PrinceRecalls[State.PrinceChoice ?? "none"]}.Concat(State.Farewell==PrinceFarewell.Open ? Content.OpenFarewell : Content.QuietFarewell).ToArray()) : CurrentEncounter.Farewell;
        public SourcedText CurrentText
        {
            get
            {
                string id=State.Phase==CampaignPhase.Intro ? Intro[State.LineIndex].TextId : State.Phase==CampaignPhase.Request ? CurrentEncounter.SummaryTextId :
                    State.Phase==CampaignPhase.Choice ? Content.ChoicePrompt : State.Phase==CampaignPhase.ChoiceResponse ? Content.PrinceResponses[State.PrinceChoice] :
                    State.Phase==CampaignPhase.Reaction ? CurrentEncounter.Responses[State.CurrentService.ResponseKey] : State.Phase==CampaignPhase.Farewell ? FarewellLines[State.LineIndex] :
                    State.Phase==CampaignPhase.Retry ? CurrentEncounter.DiscoveryRetryTextId : null;
                return id==null ? null : Content.Texts[id];
            }
        }
        private CampaignSession(CampaignContent content,ICampaignSaveStore store,CampaignCheckpoint state)
        { Content=content; this.store=store; Apply(state); }
        public static async Task<CampaignSession> StartAsync(CampaignContent c,ICampaignSaveStore store)
        {
            var k=c.CreateKnowledge(); var state=new CampaignCheckpoint(1,Guid.NewGuid().ToString("N"),0,CampaignPhase.Welcome,0,null,null,
                new[]{Select(c.Slots[0],k)},Array.Empty<string>(),Array.Empty<string>(),Array.Empty<CommittedService>(),Array.Empty<MemoryReceipt>(),k.KnownReceipts.ToDictionary(x=>x.Key,x=>x.Value),k.Facts.ToDictionary(x=>x.Key,x=>x.Value));
            var session=new CampaignSession(c,store,state); await session.Publish(state); return session;
        }
        private static string Select(CampaignSlot slot,RecipeKnowledge knowledge) => slot.WhenKnownRecipe!=null && knowledge.Project(slot.WhenKnownRecipe).Known ? slot.FallbackId : slot.EncounterId;
        public static CampaignSession Continue(CampaignContent c,ICampaignSaveStore store,CampaignCheckpoint state) => new CampaignSession(c,store,state);
        private void Apply(CampaignCheckpoint state)
        {
            State=state; Knowledge=RestoreKnowledge(); Supply=Content.AvailableSupplyAt(state.SlotIndex); stageElapsed=0;
            Brew=state.Phase==CampaignPhase.Brewing ? new BrewingSession(Content.Catalog,Content.Tuning,Supply) : null;
        }
        private RecipeKnowledge RestoreKnowledge()
        {
            var k=new RecipeKnowledge(Content.Catalog); foreach(var seed in State.KnownReceipts) k.SeedKnown(seed.Key,seed.Value);
            foreach(var pair in State.Facts) foreach(var fact in pair.Value) k.GrantFact(pair.Key,fact); return k;
        }
        private List<string> ConsumeCurrent()
        {
            var result=State.ConsumedLines.ToList(); if(CurrentText==null) return result;
            string receipt=State.OccurrenceId+":"+State.Phase+":"+State.LineIndex+":"+CurrentText.Id;
            if(!result.Contains(receipt)) result.Add(receipt); return result;
        }
        public async Task<bool> AdvanceAsync(long id)
        {
            if(!input.Consume(id) || !CanInteract) return false;
            var consumed=ConsumeCurrent();
            switch(State.Phase)
            {
                case CampaignPhase.Welcome: return await Publish(Copy(CampaignPhase.NightIntro));
                case CampaignPhase.NightIntro: return await Publish(Copy(CampaignPhase.Arriving));
                case CampaignPhase.NightEnd: return await Publish(Copy(CampaignPhase.NightIntro));
                case CampaignPhase.Intro:
                    var knowledge=RestoreKnowledge(); foreach(var f in Intro[State.LineIndex].Facts) knowledge.GrantFact(f.RecipeId,f.Fact);
                    bool more=State.LineIndex+1<Intro.Count;
                    return await Publish(Copy(more ? CampaignPhase.Intro : CurrentEncounter.PrinceVisit==PrinceVisit.First && State.PrinceChoice==null ? CampaignPhase.Choice : CampaignPhase.Request,
                        line:more ? State.LineIndex+1 : 0,consumed:consumed,knowledge:knowledge));
                case CampaignPhase.ChoiceResponse: return await Publish(Copy(CampaignPhase.Request,consumed:consumed));
                case CampaignPhase.Request: input.Release(); return await Publish(Copy(CampaignPhase.Brewing,consumed:consumed));
                case CampaignPhase.Brewing:
                    if(Brew.Container(ContainerRole.Serving).TotalMl==0) return false;
                    CancelHeld(); var snapshot=Brew.Freeze(); var outcome=ServiceEvaluator.Evaluate(Content.Catalog,Content.Tuning,snapshot,CurrentEncounter.Request);
                    var learned=RestoreKnowledge(); bool discovered=CurrentEncounter.RequiredDiscovery==null ? learned.Discover(outcome,State.OccurrenceId) : learned.DiscoverRequired(outcome,State.OccurrenceId,CurrentEncounter.RequiredDiscovery);
                    bool good=outcome.Preparation==PreparationResult.Good && outcome.QualityGood;
                    string response=outcome.Recognition!=DrinkRecognition.Recognized ? "unknown" : outcome.Fits ? (good ? "good" : "weak") : (good ? "mismatch" : "bothWeak");
                    var services=State.Services.Concat(new[]{new CommittedService(State.OccurrenceId,snapshot,outcome,response,discovered)}).ToArray();
                    PrinceFarewell? farewell=State.Farewell;
                    if(CurrentEncounter.PrinceVisit==PrinceVisit.Clarity)
                    {
                        var visits=new List<PrinceServiceRecord>();
                        for(int n=0;n<State.SelectedEncounters.Count;n++)
                        {
                            var visit=Content.Encounters[State.SelectedEncounters[n]].PrinceVisit;
                            var service=services.LastOrDefault(x=>x.OccurrenceId==State.RunId+":"+n);
                            if(visit.HasValue && service!=null) visits.Add(new PrinceServiceRecord(visit.Value,service.OccurrenceId,service.Outcome));
                        }
                        farewell=PrinceFarewellPolicy.Select(visits);
                    }
                    return await Publish(Copy(CampaignPhase.Reaction,services:services,knowledge:learned,farewell:farewell));
                case CampaignPhase.Reaction: return await Publish(Copy(CampaignPhase.Result,consumed:consumed));
                case CampaignPhase.Result:
                    if(CurrentEncounter.RequiredDiscovery!=null && !Knowledge.Project(CurrentEncounter.RequiredDiscovery).Known) return await Publish(Copy(CampaignPhase.Retry));
                    return await Publish(Copy(State.CurrentService.Discovered ? CampaignPhase.Discovery : CampaignPhase.Farewell));
                case CampaignPhase.Discovery: return await Publish(Copy(CampaignPhase.Farewell));
                case CampaignPhase.Retry: input.Release(); return await Publish(Copy(CampaignPhase.Brewing,consumed:consumed));
                case CampaignPhase.Farewell:
                    bool next=State.LineIndex+1<FarewellLines.Count;
                    return await Publish(Copy(next ? CampaignPhase.Farewell : CampaignPhase.Departing,line:next ? State.LineIndex+1 : 0,consumed:consumed));
                default: return false;
            }
        }
        public Task<bool> ChooseAsync(string choice,long id)
        {
            if(!input.Consume(id) || !CanInteract || State.Phase!=CampaignPhase.Choice || !Content.PrinceChoices.ContainsKey(choice)) return Task.FromResult(false);
            return Publish(Copy(CampaignPhase.ChoiceResponse,choice:choice,consumed:ConsumeCurrent()));
        }
        public async Task<bool> TickAsync(double seconds)
        {
            if(double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds<0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if(!CanInteract || (State.Phase!=CampaignPhase.Arriving && State.Phase!=CampaignPhase.Departing)) return false;
            stageElapsed+=seconds; if(stageElapsed<1.5) return false;
            if(State.Phase==CampaignPhase.Arriving) return await Publish(Copy(CampaignPhase.Intro));
            var completed=State.CompletedOccurrences.Concat(new[]{State.OccurrenceId}); var memories=State.Memories.ToList();
            if(CurrentEncounter.PrinceVisit==PrinceVisit.Clarity)
            {
                if(!State.Farewell.HasValue) throw new InvalidOperationException("Closing departure lacks a committed farewell.");
                memories.Add(new MemoryReceipt("mem_01",State.Farewell==PrinceFarewell.Open ? "star_glowing" : "star_unlit",State.OccurrenceId,State.Sequence+1));
            }
            else if(CurrentEncounter.MemoryId!=null)
                memories.Add(new MemoryReceipt(CurrentEncounter.MemoryId,CurrentEncounter.MemoryVariant,State.OccurrenceId,State.Sequence+1));
            if(State.SlotIndex+1==Content.Slots.Count) return await Publish(Copy(CampaignPhase.Complete,completed:completed,memories:memories));
            int slot=State.SlotIndex+1; string selected=Select(Content.Slots[slot],Knowledge);
            return await Publish(Copy(Content.Encounters[selected].Night!=CurrentEncounter.Night ? CampaignPhase.NightEnd : CampaignPhase.Arriving,
                slot:slot,selected:State.SelectedEncounters.Concat(new[]{selected}),completed:completed,memories:memories));
        }
        private CampaignCheckpoint Copy(CampaignPhase phase,int line=0,int? slot=null,string choice=null,PrinceFarewell? farewell=null,
            IEnumerable<string> selected=null,IEnumerable<string> consumed=null,IEnumerable<string> completed=null,IEnumerable<CommittedService> services=null,
            IEnumerable<MemoryReceipt> memories=null,RecipeKnowledge knowledge=null)
        {
            var k=knowledge ?? Knowledge;
            return new CampaignCheckpoint(State.Sequence+1,State.RunId,slot ?? State.SlotIndex,phase,line,choice ?? State.PrinceChoice,farewell ?? State.Farewell,
                selected ?? State.SelectedEncounters,consumed ?? State.ConsumedLines,completed ?? State.CompletedOccurrences,services ?? State.Services,memories ?? State.Memories,
                k.KnownReceipts.ToDictionary(x=>x.Key,x=>x.Value),k.Facts.ToDictionary(x=>x.Key,x=>x.Value));
        }
        private async Task<bool> Publish(CampaignCheckpoint next)
        {
            if(disposed || IsSaving) return false; IsSaving=true; pending=next;
            try { await store.SaveAsync(next); if(disposed) return false; Apply(next); pending=null; SaveError=null; return true; }
            catch(Exception e) { SaveError=e.Message; return false; }
            finally { IsSaving=false; }
        }
        public Task<bool> RetrySaveAsync() => pending==null || IsSaving || disposed ? Task.FromResult(false) : Publish(pending);
        public bool ExecuteBrew(long id,Action<BrewingSession> action)
        { if(!input.Consume(id) || !CanInteract || input.HeldBlocked || State.Phase!=CampaignPhase.Brewing) return false; action(Brew); return true; }
        public bool UpdateBrew(long micros,bool fine)
        { if(!CanInteract || input.HeldBlocked || State.Phase!=CampaignPhase.Brewing) { Brew?.CancelInput(); return false; } Brew.UpdateFlow(micros,1m,fine); return true; }
        public bool DiscardBrew(long id)
        { if(!input.Consume(id) || !CanInteract || State.Phase!=CampaignPhase.Brewing) return false; CancelHeld(); Brew=new BrewingSession(Content.Catalog,Content.Tuning,Supply); return true; }
        public void CancelHeld() { Brew?.CancelInput(); input.CancelHeld(); }
        public void ReleaseHeldInput() { Brew?.CancelInput(); input.Release(); }
        public void SetFocus(bool value) { Focused=value; CancelHeld(); }
        public void OpenModal(string id) { CancelHeld(); Modals.Push(id); }
        public void CloseModal() { CancelHeld(); Modals.Pop(); }
        public void Dispose() { disposed=true; CancelHeld(); }
    }
}
