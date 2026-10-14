using System;
using System.Collections.Generic;

namespace Surlhwa.Domain
{
    public enum PrinceVisit { First, Warmth, Clarity }
    public enum PrinceFarewell { Quiet, Open }
    public sealed class PrinceServiceRecord
    {
        public PrinceVisit Visit { get; }
        public string OccurrenceId { get; }
        public ServiceOutcome Outcome { get; }
        public PrinceServiceRecord(PrinceVisit visit,string occurrenceId,ServiceOutcome outcome)
        {
            if(!Enum.IsDefined(typeof(PrinceVisit),visit) || string.IsNullOrWhiteSpace(occurrenceId)) throw new ArgumentException("Invalid Prince visit receipt.");
            Visit=visit; OccurrenceId=occurrenceId; Outcome=outcome ?? throw new ArgumentNullException(nameof(outcome));
        }
    }
    public static class PrinceFarewellPolicy
    {
        public const string Revision="prince_return_understanding_v1";
        public static PrinceFarewell Select(IEnumerable<PrinceServiceRecord> services)
        {
            if(services==null) throw new ArgumentNullException(nameof(services));
            var seen=new HashSet<PrinceVisit>(); var occurrences=new HashSet<string>(StringComparer.Ordinal);
            bool warmth=false,clarity=false;
            foreach(var service in services)
            {
                if(service==null || !seen.Add(service.Visit) || !occurrences.Add(service.OccurrenceId)) throw new ArgumentException("Duplicate or missing Prince service receipt.");
                bool understood=service.Outcome.Recognition==DrinkRecognition.Recognized && service.Outcome.Fits;
                if(service.Visit==PrinceVisit.Warmth) warmth=understood;
                if(service.Visit==PrinceVisit.Clarity) clarity=understood;
            }
            return warmth && clarity ? PrinceFarewell.Open : PrinceFarewell.Quiet;
        }
    }
}
