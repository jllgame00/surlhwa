using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Surlhwa.Domain
{
    public enum ContainerRole { Jigger, Mixing, Serving }
    public enum BrewAction { Pour, Dash, PourFromContainer, Strain, AddIce, Stir, Garnish }
    public sealed class LiquidMovement
    {
        public decimal AcceptedMl { get; } public decimal SpilledMl { get; }
        public IReadOnlyDictionary<string,decimal> Accepted { get; }
        internal LiquidMovement(IDictionary<string,decimal> accepted,decimal spilled) { Accepted=LiquidMath.Copy(accepted); AcceptedMl=accepted.Values.Sum(); SpilledMl=spilled; }
        internal static LiquidMovement Empty() => new LiquidMovement(new Dictionary<string,decimal>(),0);
    }
    public sealed class PreparationAction
    {
        public int Sequence { get; } public BrewAction Kind { get; } public string Source { get; }
        public ContainerRole Target { get; } public decimal AcceptedMl { get; } public decimal SpilledMl { get; }
        public int CompositionRevision { get; }
        public IReadOnlyDictionary<string,decimal> AcceptedByIngredient { get; }
        public long ElapsedMicroseconds { get; }
        public PreparationAction(int sequence,BrewAction kind,string source,ContainerRole target,decimal accepted,decimal spilled,int revision,
            IDictionary<string,decimal> components,long elapsed)
        {
            if(sequence<1 || !Enum.IsDefined(typeof(BrewAction),kind) || !Enum.IsDefined(typeof(ContainerRole),target) || revision<0 || elapsed<0) throw new ArgumentException("Invalid action record.");
            LiquidMath.Validate(accepted); LiquidMath.Validate(spilled);
            if(components!=null && (components.Values.Any(x=>x<0 || decimal.Round(x,6)!=x) || components.Values.Sum()!=accepted)) throw new ArgumentException("Invalid accepted components.");
            Sequence=sequence; Kind=kind; Source=source; Target=target; AcceptedMl=accepted; SpilledMl=spilled; CompositionRevision=revision;
            AcceptedByIngredient=LiquidMath.Copy(components ?? new Dictionary<string,decimal>()); ElapsedMicroseconds=elapsed;
        }
    }
    public sealed class ContainerReadModel
    {
        public IReadOnlyDictionary<string,decimal> Amounts { get; }
        public decimal TotalMl => Amounts.Values.Sum(); public decimal CapacityMl { get; }
        public bool HasIce { get; } public int Revision { get; }
        internal ContainerReadModel(BrewContainer c) { Amounts=LiquidMath.Copy(c.Amounts); CapacityMl=c.Capacity; HasIce=c.Ice; Revision=c.Revision; }
    }
    internal sealed class BrewContainer
    {
        internal readonly decimal Capacity;
        internal readonly Dictionary<string,decimal> Amounts=new Dictionary<string,decimal>(StringComparer.Ordinal);
        internal readonly Dictionary<string,decimal> DashAmounts=new Dictionary<string,decimal>(StringComparer.Ordinal);
        internal decimal Total => Amounts.Values.Sum();
        internal bool Ice,Strained; internal int Revision; internal StirStamp Stamp;
        internal BrewContainer(decimal capacity) { Capacity=capacity; }
        internal static void Add(Dictionary<string,decimal> into,string key,decimal value)
        { if(value>0) into[key]=(into.TryGetValue(key,out decimal old) ? old : 0)+value; }
    }
    public sealed class ServiceSnapshot
    {
        public string AttemptId { get; } public string TuningRevision { get; }
        public IReadOnlyDictionary<string,decimal> Amounts { get; }
        public IReadOnlyDictionary<string,decimal> DashAmounts { get; }
        public decimal TotalMl => Amounts.Values.Sum(); public bool HasIce { get; }
        public bool Strained { get; } public bool StirHowGood { get; } public bool StirQualityGood { get; }
        public double StirDegrees { get; } public double StableFraction { get; }
        public IReadOnlyCollection<string> StirIngredients { get; }
        public string Garnish { get; } public string GarnishPosition { get; }
        public IReadOnlyList<PreparationAction> Trace { get; }
        internal ServiceSnapshot(string attempt,string revision,BrewContainer c,string garnish,string position,List<PreparationAction> trace)
        {
            AttemptId=attempt; TuningRevision=revision; Amounts=LiquidMath.Copy(c.Amounts); DashAmounts=LiquidMath.Copy(c.DashAmounts);
            HasIce=c.Ice; Strained=c.Strained; StirHowGood=c.Stamp?.HowGood ?? false; StirQualityGood=c.Stamp?.QualityGood ?? false;
            StirDegrees=c.Stamp?.Degrees ?? 0; StableFraction=c.Stamp?.StableFraction ?? 0;
            StirIngredients=Array.AsReadOnly(c.Stamp?.Ingredients.ToArray() ?? Array.Empty<string>());
            Garnish=garnish; GarnishPosition=position; Trace=new ReadOnlyCollection<PreparationAction>(trace.ToArray());
        }
        // Restore immutable evidence only; Continue never reconstructs a live mid-pour container.
        public ServiceSnapshot(string attempt,string revision,IDictionary<string,decimal> amounts,IDictionary<string,decimal> dash,
            bool ice,bool strained,bool how,bool quality,double degrees,double stable,IEnumerable<string> stirIngredients,
            string garnish,string position,IEnumerable<PreparationAction> trace)
        {
            if(!Guid.TryParseExact(attempt,"N",out _) || string.IsNullOrWhiteSpace(revision) || double.IsNaN(degrees) || double.IsInfinity(degrees) || degrees<0 || double.IsNaN(stable) || stable<0 || stable>1) throw new ArgumentException("Invalid service evidence.");
            foreach(var a in amounts) { _=new StableId<IngredientIdentity>(a.Key); LiquidMath.Validate(a.Value); if(a.Value==0) throw new ArgumentException("Empty component."); }
            foreach(var a in dash) if(a.Value<=0 || decimal.Round(a.Value,6)!=a.Value || !amounts.TryGetValue(a.Key,out var v) || a.Value>v) throw new ArgumentException("Invalid dash evidence.");
            if((garnish==null)!=(position==null) || (garnish!=null && (garnish!="cherry" || position!="in_glass"))) throw new ArgumentException("Invalid garnish evidence.");
            AttemptId=attempt; TuningRevision=revision; Amounts=LiquidMath.Copy(amounts); DashAmounts=LiquidMath.Copy(dash);
            HasIce=ice; Strained=strained; StirHowGood=how; StirQualityGood=quality; StirDegrees=degrees; StableFraction=stable;
            StirIngredients=Array.AsReadOnly(stirIngredients.ToArray()); Garnish=garnish; GarnishPosition=position; Trace=Array.AsReadOnly(trace.ToArray());
            for(int i=0;i<Trace.Count;i++) if(Trace[i].Sequence!=i+1) throw new ArgumentException("Unordered action record.");
        }
    }
    public sealed class BrewingSession
    {
        private readonly BartendingCatalog catalog; private readonly BrewingTuning tuning; private readonly HashSet<string> supply;
        private readonly Dictionary<ContainerRole,BrewContainer> containers;
        private readonly List<PreparationAction> trace=new List<PreparationAction>();
        private readonly string attempt=Guid.NewGuid().ToString("N");
        private string garnish,position,flowIngredient; private ContainerRole? flowSource;
        private ContainerRole flowTarget; private BrewAction flowKind; private bool flow; private decimal remainder;
        private StirMotion motion; private int motionRevision;
        private long acceptedInterval;
        public bool FlowActive => flow; public bool StirActive => motion!=null;
        public int Revision { get; private set; }
        public BrewingSession(BartendingCatalog catalog,BrewingTuning tuning,IEnumerable<string> supplied)
        {
            this.catalog=catalog ?? throw new ArgumentNullException(nameof(catalog)); this.tuning=tuning ?? throw new ArgumentNullException(nameof(tuning));
            supply=new HashSet<string>(supplied,StringComparer.Ordinal);
            if(supply.Any(x=>!catalog.Ingredients.Contains(x))) throw new ArgumentException("Unknown supply ingredient.");
            containers=new Dictionary<ContainerRole,BrewContainer> { [ContainerRole.Jigger]=new BrewContainer(tuning.JiggerMl),[ContainerRole.Mixing]=new BrewContainer(tuning.MixingMl),[ContainerRole.Serving]=new BrewContainer(tuning.ServingMl) };
        }
        public ContainerReadModel Container(ContainerRole role) => new ContainerReadModel(Get(role));
        private BrewContainer Get(ContainerRole role) => containers.TryGetValue(role,out var c) ? c : throw new ArgumentOutOfRangeException(nameof(role));
        private void CheckSupply(string ingredient)
        { if(ingredient==null || !catalog.Ingredients.Contains(ingredient) || !supply.Contains(ingredient)) throw new InvalidOperationException("Ingredient is not supplied: "+ingredient); }
        private void Record(BrewAction kind,string source,ContainerRole target,decimal accepted=0,decimal spilled=0,IDictionary<string,decimal> components=null)
        { trace.Add(new PreparationAction(trace.Count+1,kind,source,target,accepted,spilled,Get(target).Revision,components,acceptedInterval)); Revision++; }
        public LiquidMovement Pour(string ingredient,ContainerRole target,decimal ml,BrewAction kind=BrewAction.Pour)
        {
            CheckSupply(ingredient); LiquidMath.Validate(ml); var c=Get(target);
            if(kind!=BrewAction.Pour && kind!=BrewAction.Dash) throw new ArgumentException("Invalid supply action.");
            if(kind==BrewAction.Dash && ml!=tuning.DashMl) throw new ArgumentException("Dash requires the authored game unit.");
            if(ml==0) return LiquidMovement.Empty();
            decimal accepted=Math.Min(ml,c.Capacity-c.Total),spill=ml-accepted;
            if(accepted>0)
            {
                BrewContainer.Add(c.Amounts,ingredient,accepted);
                if(kind==BrewAction.Dash) BrewContainer.Add(c.DashAmounts,ingredient,accepted);
                c.Revision++; c.Stamp=null; c.Strained=false;
            }
            var received=new Dictionary<string,decimal> { [ingredient]=accepted };
            Record(kind,ingredient,target,accepted,spill,received);
            return new LiquidMovement(received,spill);
        }
        public LiquidMovement Transfer(ContainerRole source,ContainerRole target,decimal ml,BrewAction kind)
        {
            LiquidMath.Validate(ml); var from=Get(source); var to=Get(target);
            if(source==target || (kind!=BrewAction.Strain && kind!=BrewAction.PourFromContainer)) throw new ArgumentException("Invalid container transfer.");
            if(kind==BrewAction.Strain && (source!=ContainerRole.Mixing || target!=ContainerRole.Serving)) throw new ArgumentException("Strain is mixing to serving.");
            decimal moved=Math.Min(ml,from.Total); if(moved==0) return LiquidMovement.Empty();
            decimal accepted=Math.Min(moved,to.Capacity-to.Total),spill=moved-accepted;
            var extracted=LiquidMath.Portion(from.Amounts,moved); var received=LiquidMath.Portion(extracted,accepted);
            decimal before=from.Total,targetBefore=to.Total; var stamp=from.Stamp;
            foreach(var item in extracted)
            {
                decimal got=received.TryGetValue(item.Key,out decimal value) ? value : 0;
                decimal old=from.Amounts[item.Key]; decimal oldDash=from.DashAmounts.TryGetValue(item.Key,out decimal dash) ? dash : 0;
                decimal removedDash=item.Value==old ? oldDash : LiquidMath.Down(oldDash*item.Value/old);
                decimal acceptedDash=item.Value==0 ? 0 : LiquidMath.Down(removedDash*got/item.Value);
                from.Amounts[item.Key]-=item.Value; if(from.Amounts[item.Key]==0) from.Amounts.Remove(item.Key);
                if(oldDash>0) { from.DashAmounts[item.Key]-=removedDash; if(from.DashAmounts[item.Key]==0) from.DashAmounts.Remove(item.Key); }
                BrewContainer.Add(to.Amounts,item.Key,got); BrewContainer.Add(to.DashAmounts,item.Key,acceptedDash);
            }
            from.Revision++;
            if(from.Total==0) { from.Stamp=null; from.Strained=false; }
            if(accepted>0)
            {
                to.Revision++;
                to.Stamp=targetBefore==0 || (to.Stamp!=null && stamp!=null && to.Stamp.Id==stamp.Id) ? stamp : null;
                to.Strained=kind==BrewAction.Strain && (targetBefore==0 || to.Strained);
            }
            if(before-from.Total!=accepted+spill) throw new InvalidOperationException("Liquid conservation failure.");
            Record(kind,source.ToString().ToLowerInvariant(),target,accepted,spill,received);
            return new LiquidMovement(received,spill);
        }
        public void AddIce(ContainerRole target)
        { var c=Get(target); if(c.Ice) return; c.Ice=true; c.Revision++; c.Stamp=null; Record(BrewAction.AddIce,"ice",target); }
        public void Garnish(string id,string placement)
        {
            if(id!="cherry" || placement!="in_glass") throw new ArgumentException("Unsupported garnish placement.");
            if(Get(ContainerRole.Serving).Total==0) throw new InvalidOperationException("No serving liquid.");
            garnish=id; position=placement; Record(BrewAction.Garnish,id,ContainerRole.Serving);
        }
        public void BeginPour(string ingredient,ContainerRole target)
        { CheckSupply(ingredient); Get(target); StartFlow(); flowIngredient=ingredient; flowSource=null; flowTarget=target; flowKind=BrewAction.Pour; }
        public void BeginTransfer(ContainerRole source,ContainerRole target,BrewAction kind)
        {
            Get(source); Get(target);
            if(source==target || (kind!=BrewAction.Strain && kind!=BrewAction.PourFromContainer) || (kind==BrewAction.Strain && (source!=ContainerRole.Mixing || target!=ContainerRole.Serving))) throw new ArgumentException("Invalid transfer binding.");
            StartFlow(); flowSource=source; flowIngredient=null; flowTarget=target; flowKind=kind;
        }
        private void StartFlow() { if(flow || motion!=null) throw new InvalidOperationException("Another tool action is active."); flow=true; remainder=0; }
        public LiquidMovement UpdateFlow(long microseconds,decimal intensity,bool fine)
        {
            if(microseconds<0 || intensity<0 || intensity>1) throw new ArgumentOutOfRangeException(nameof(microseconds));
            if(!flow || microseconds==0 || intensity==0) return LiquidMovement.Empty();
            decimal rate=flowSource.HasValue ? (fine ? tuning.FineTransferRate : tuning.TransferRate) : (fine ? tuning.FinePourRate : tuning.PourRate);
            decimal exact=rate*intensity*microseconds/1000000m+remainder,ml=LiquidMath.Down(exact); remainder=exact-ml;
            acceptedInterval=microseconds;
            try { return flowSource.HasValue ? Transfer(flowSource.Value,flowTarget,ml,flowKind) : Pour(flowIngredient,flowTarget,ml); }
            finally { acceptedInterval=0; }
        }
        public void EndFlow() { flow=false; remainder=0; flowIngredient=null; flowSource=null; }
        public void BeginStir(double x,double y)
        {
            if(flow || motion!=null) throw new InvalidOperationException("Another tool action is active.");
            var c=Get(ContainerRole.Mixing); if(c.Total==0 || !c.Ice) return;
            motion=new StirMotion(tuning,x,y,c.Stamp); motionRevision=c.Revision;
        }
        public void MoveStir(double x,double y,long microseconds) { motion?.Move(x,y,microseconds); }
        public void EndStir()
        {
            if(motion==null) return;
            var c=Get(ContainerRole.Mixing);
            if(c.Revision==motionRevision && c.Total>0 && c.Ice)
            { c.Stamp=new StirStamp(motion.Degrees,motion.MovingMicros,motion.StableMicros,tuning,c.Amounts.Keys); Record(BrewAction.Stir,"spoon",ContainerRole.Mixing); }
            motion=null;
        }
        public void CancelInput() { EndFlow(); EndStir(); }
        public ServiceSnapshot Freeze() => new ServiceSnapshot(attempt,tuning.Revision,Get(ContainerRole.Serving),garnish,position,trace);
    }
}
