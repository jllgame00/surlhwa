using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Surlhwa.Domain
{
    public enum SlicePhase { Welcome, Arriving, Intro, Request, Brewing, Reaction, Result, Discovery, Farewell, Departing, Complete }
    public sealed class CommittedService
    {
        public string OccurrenceId { get; } public ServiceSnapshot Snapshot { get; } public ServiceOutcome Outcome { get; }
        public string ResponseKey { get; } public bool Discovered { get; }
        public CommittedService(string occurrence,ServiceSnapshot snapshot,ServiceOutcome outcome,string response,bool discovered)
        { OccurrenceId=occurrence; Snapshot=snapshot; Outcome=outcome; ResponseKey=response; Discovered=discovered; }
    }
    public sealed class SliceCheckpoint
    {
        public long Sequence { get; } public string RunId { get; } public int EncounterIndex { get; }
        public SlicePhase Phase { get; } public int LineIndex { get; }
        public IReadOnlyList<string> ConsumedLines { get; } public IReadOnlyList<string> CompletedOccurrences { get; }
        public IReadOnlyList<CommittedService> Services { get; }
        public IReadOnlyDictionary<string,string> KnownReceipts { get; }
        public IReadOnlyDictionary<string,IReadOnlyList<string>> Facts { get; }
        public bool ActComplete => false; public IReadOnlyList<string> MemoryIds => Array.Empty<string>();
        public string OccurrenceId => RunId+":"+EncounterIndex;
        public CommittedService CurrentService => Services.SingleOrDefault(x=>x.OccurrenceId==OccurrenceId);
        public SliceCheckpoint(long sequence,string run,int encounter,SlicePhase phase,int line,IEnumerable<string> consumed,
            IEnumerable<string> completed,IEnumerable<CommittedService> services,IDictionary<string,string> known,IDictionary<string,string[]> facts)
        {
            if(sequence<1 || !Guid.TryParseExact(run,"N",out _) || encounter<0 || line<0 || !Enum.IsDefined(typeof(SlicePhase),phase)) throw new ArgumentException("Invalid checkpoint identity.");
            Sequence=sequence; RunId=run; EncounterIndex=encounter; Phase=phase; LineIndex=line;
            ConsumedLines=Array.AsReadOnly(consumed.ToArray()); CompletedOccurrences=Array.AsReadOnly(completed.ToArray()); Services=Array.AsReadOnly(services.ToArray());
            KnownReceipts=new ReadOnlyDictionary<string,string>(new Dictionary<string,string>(known,StringComparer.Ordinal));
            Facts=new ReadOnlyDictionary<string,IReadOnlyList<string>>(facts.ToDictionary(x=>x.Key,x=>(IReadOnlyList<string>)Array.AsReadOnly(x.Value.ToArray()),StringComparer.Ordinal));
        }
    }
}
