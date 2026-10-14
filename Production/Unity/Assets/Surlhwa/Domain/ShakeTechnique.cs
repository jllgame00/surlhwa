using System;
using System.Collections.Generic;

namespace Surlhwa.Domain
{
    public sealed class ShakeTuning
    {
        public string Revision { get; }
        public int HowReversals { get; } public int QualityReversals { get; }
        public double Endpoint { get; } public long MinInterval { get; } public long MaxInterval { get; }
        public double StableFraction { get; }
        public ShakeTuning(string revision="shake_v1",int how=4,int quality=8,double endpoint=.7,
            long minInterval=250000,long maxInterval=1000000,double stable=.75)
        {
            if(string.IsNullOrWhiteSpace(revision) || how<1 || quality<how || double.IsNaN(endpoint) || endpoint<=0 || endpoint>1 ||
                minInterval<=0 || maxInterval<minInterval || double.IsNaN(stable) || stable<=0 || stable>1) throw new ArgumentException("Invalid Shake tuning.");
            Revision=revision; HowReversals=how; QualityReversals=quality; Endpoint=endpoint;
            MinInterval=minInterval; MaxInterval=maxInterval; StableFraction=stable;
        }
    }
    public sealed class ShakeEvidence
    {
        public string TuningRevision { get; }
        public int Reversals { get; } public int StableReversals { get; }
        public bool HowGood { get; } public bool QualityGood { get; }
        public IReadOnlyDictionary<string,decimal> Amounts { get; }
        public ShakeEvidence(string revision,int reversals,int stable,bool how,bool quality,IDictionary<string,decimal> amounts)
        {
            if(string.IsNullOrWhiteSpace(revision) || reversals<0 || stable<0 || stable>reversals || (quality && !how)) throw new ArgumentException("Invalid Shake evidence.");
            foreach(var a in amounts) { _=new StableId<IngredientIdentity>(a.Key); LiquidMath.Validate(a.Value); }
            TuningRevision=revision; Reversals=reversals; StableReversals=stable; HowGood=how; QualityGood=quality; Amounts=LiquidMath.Copy(amounts);
        }
    }
    internal sealed class ShakeStamp
    {
        internal readonly string Id=Guid.NewGuid().ToString("N");
        internal readonly int Reversals,StableReversals;
        internal readonly ShakeTuning Tuning;
        internal ShakeStamp(int reversals,int stable,ShakeTuning tuning) { Reversals=reversals; StableReversals=stable; Tuning=tuning; }
        internal ShakeEvidence Freeze(IDictionary<string,decimal> amounts) => new ShakeEvidence(Tuning.Revision,Reversals,StableReversals,
            Reversals>=Tuning.HowReversals,Reversals>=Tuning.QualityReversals && (double)StableReversals/Reversals>=Tuning.StableFraction,amounts);
    }
    internal sealed class ShakeMotion
    {
        private readonly ShakeTuning tuning;
        private int endpoint;
        private long elapsed;
        internal int Reversals { get; private set; } internal int StableReversals { get; private set; }
        internal ShakeMotion(ShakeTuning tuning,double position,ShakeStamp prior)
        {
            this.tuning=tuning; Reversals=prior?.Reversals ?? 0; StableReversals=prior?.StableReversals ?? 0;
            endpoint=Endpoint(position);
        }
        private int Endpoint(double p) => !double.IsNaN(p) && Math.Abs(p)<=1.25 && Math.Abs(p)>=tuning.Endpoint ? Math.Sign(p) : 0;
        internal void Move(double position,long micros)
        {
            if(micros<=0) throw new ArgumentOutOfRangeException(nameof(micros));
            if(double.IsNaN(position) || Math.Abs(position)>1.25) { endpoint=0; elapsed=0; return; }
            elapsed=checked(elapsed+micros); int next=Endpoint(position);
            if(next==0 || next==endpoint) return;
            if(endpoint!=0)
            { Reversals++; if(elapsed>=tuning.MinInterval && elapsed<=tuning.MaxInterval) StableReversals++; }
            endpoint=next; elapsed=0;
        }
    }
}
