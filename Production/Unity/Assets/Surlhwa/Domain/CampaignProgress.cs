using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Surlhwa.Domain
{
    public enum CampaignPhase { Welcome, NightIntro, Arriving, Intro, Choice, ChoiceResponse, Request, Brewing, Reaction, Result, Discovery, Retry, Farewell, Departing, NightEnd, Complete }
    public sealed class MemoryReceipt
    {
        public string Id { get; } public string Variant { get; } public string OccurrenceId { get; } public long Sequence { get; }
        public MemoryReceipt(string id,string variant,string occurrence,long sequence)
        {
            _=new StableId<TextIdentity>(id); _=new StableId<TextIdentity>(variant);
            if(string.IsNullOrWhiteSpace(occurrence) || sequence<1) throw new ArgumentException("Invalid memory receipt.");
            Id=id; Variant=variant; OccurrenceId=occurrence; Sequence=sequence;
        }
    }
    public sealed class CampaignCheckpoint
    {
        public long Sequence { get; } public string RunId { get; } public int SlotIndex { get; }
        public CampaignPhase Phase { get; } public int LineIndex { get; } public string PrinceChoice { get; }
        public PrinceFarewell? Farewell { get; }
        public IReadOnlyList<string> SelectedEncounters { get; } public IReadOnlyList<string> ConsumedLines { get; }
        public IReadOnlyList<string> CompletedOccurrences { get; } public IReadOnlyList<CommittedService> Services { get; }
        public IReadOnlyList<MemoryReceipt> Memories { get; }
        public IReadOnlyDictionary<string,string> KnownReceipts { get; }
        public IReadOnlyDictionary<string,IReadOnlyList<string>> Facts { get; }
        public string OccurrenceId => RunId+":"+SlotIndex;
        public CommittedService CurrentService => Services.LastOrDefault(x=>x.OccurrenceId==OccurrenceId);
        public bool ActComplete => Phase==CampaignPhase.Complete && Memories.Any(x=>x.Id=="mem_01");
        public CampaignCheckpoint(long sequence,string run,int slot,CampaignPhase phase,int line,string choice,PrinceFarewell? farewell,
            IEnumerable<string> selected,IEnumerable<string> consumed,IEnumerable<string> completed,IEnumerable<CommittedService> services,
            IEnumerable<MemoryReceipt> memories,IDictionary<string,string> known,IDictionary<string,string[]> facts)
        {
            if(sequence<1 || !Guid.TryParseExact(run,"N",out _) || slot<0 || line<0 || !Enum.IsDefined(typeof(CampaignPhase),phase) ||
                (farewell.HasValue && !Enum.IsDefined(typeof(PrinceFarewell),farewell.Value))) throw new ArgumentException("Invalid campaign checkpoint identity.");
            Sequence=sequence; RunId=run; SlotIndex=slot; Phase=phase; LineIndex=line; PrinceChoice=choice; Farewell=farewell;
            SelectedEncounters=Array.AsReadOnly(selected.ToArray()); ConsumedLines=Array.AsReadOnly(consumed.ToArray()); CompletedOccurrences=Array.AsReadOnly(completed.ToArray());
            Services=Array.AsReadOnly(services.ToArray()); Memories=Array.AsReadOnly(memories.ToArray());
            KnownReceipts=new ReadOnlyDictionary<string,string>(new Dictionary<string,string>(known));
            Facts=new ReadOnlyDictionary<string,IReadOnlyList<string>>(facts.ToDictionary(x=>x.Key,x=>(IReadOnlyList<string>)Array.AsReadOnly(x.Value.ToArray())));
            if(SelectedEncounters.Count!=slot+1 || ConsumedLines.Distinct().Count()!=ConsumedLines.Count || CompletedOccurrences.Distinct().Count()!=CompletedOccurrences.Count ||
                Services.Select(x=>x.Snapshot.AttemptId).Distinct().Count()!=Services.Count || Memories.Select(x=>x.Id).Distinct().Count()!=Memories.Count) throw new ArgumentException("Invalid campaign receipt ledger.");
        }
    }
}
