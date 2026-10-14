using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Surlhwa.Domain
{
    public enum ContainerRole { Jigger, Mixing, Serving, Shaker }
    public enum BrewAction { Pour, Dash, PourFromContainer, Strain, AddIce, Stir, Garnish, GentleStir, Splash, Shake, CloseShaker, OpenShaker, Float }
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
        internal readonly Dictionary<string,decimal> ShakenAmounts=new Dictionary<string,decimal>(StringComparer.Ordinal);
        internal ShakeStamp Shake;
        internal FloatLayers Layers;
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
        public ShakeEvidence Shake { get; }
        public IReadOnlyList<FloatLayerEvidence> Layers { get; }
        public string FloatTuningRevision { get; }
        internal ServiceSnapshot(string attempt,string revision,BrewContainer c,string garnish,string position,List<PreparationAction> trace,string floatRevision)
        {
            AttemptId=attempt; TuningRevision=revision; Amounts=LiquidMath.Copy(c.Amounts); DashAmounts=LiquidMath.Copy(c.DashAmounts);
            HasIce=c.Ice; Strained=c.Strained; StirHowGood=c.Stamp?.HowGood ?? false; StirQualityGood=c.Stamp?.QualityGood ?? false;
            StirDegrees=c.Stamp?.Degrees ?? 0; StableFraction=c.Stamp?.StableFraction ?? 0;
            StirIngredients=Array.AsReadOnly(c.Stamp?.Ingredients.ToArray() ?? Array.Empty<string>());
            Garnish=garnish; GarnishPosition=position; Trace=new ReadOnlyCollection<PreparationAction>(trace.ToArray());
            Shake=c.Shake?.Freeze(c.ShakenAmounts);
            Layers=c.Layers?.Freeze() ?? Array.Empty<FloatLayerEvidence>(); FloatTuningRevision=floatRevision;
        }
        // Restore immutable evidence only; Continue never reconstructs a live mid-pour container.
        public ServiceSnapshot(string attempt,string revision,IDictionary<string,decimal> amounts,IDictionary<string,decimal> dash,
            bool ice,bool strained,bool how,bool quality,double degrees,double stable,IEnumerable<string> stirIngredients,
            string garnish,string position,IEnumerable<PreparationAction> trace,ShakeEvidence shake=null,
            IEnumerable<FloatLayerEvidence> layers=null,string floatRevision=null)
        {
            if(!Guid.TryParseExact(attempt,"N",out _) || string.IsNullOrWhiteSpace(revision) || double.IsNaN(degrees) || double.IsInfinity(degrees) || degrees<0 || double.IsNaN(stable) || stable<0 || stable>1) throw new ArgumentException("Invalid service evidence.");
            foreach(var a in amounts) { _=new StableId<IngredientIdentity>(a.Key); LiquidMath.Validate(a.Value); if(a.Value==0) throw new ArgumentException("Empty component."); }
            foreach(var a in dash) if(a.Value<=0 || decimal.Round(a.Value,6)!=a.Value || !amounts.TryGetValue(a.Key,out var v) || a.Value>v) throw new ArgumentException("Invalid dash evidence.");
            if((garnish==null)!=(position==null) || (garnish!=null && !GarnishRules.Valid(garnish,position))) throw new ArgumentException("Invalid garnish evidence.");
            AttemptId=attempt; TuningRevision=revision; Amounts=LiquidMath.Copy(amounts); DashAmounts=LiquidMath.Copy(dash);
            HasIce=ice; Strained=strained; StirHowGood=how; StirQualityGood=quality; StirDegrees=degrees; StableFraction=stable;
            StirIngredients=Array.AsReadOnly(stirIngredients.ToArray()); Garnish=garnish; GarnishPosition=position; Trace=Array.AsReadOnly(trace.ToArray());
            if(shake!=null && shake.Amounts.Any(x=>!amounts.TryGetValue(x.Key,out var ml) || x.Value>ml)) throw new ArgumentException("Shake evidence exceeds actual liquid.");
            Shake=shake;
            Layers=Array.AsReadOnly((layers ?? Array.Empty<FloatLayerEvidence>()).ToArray()); FloatTuningRevision=floatRevision;
            if(Layers.Count>0)
            {
                if(string.IsNullOrWhiteSpace(floatRevision)) throw new ArgumentException("Missing Float revision.");
                var sum=new Dictionary<string,decimal>(); foreach(var layer in Layers) foreach(var a in layer.Amounts) BrewContainer.Add(sum,a.Key,a.Value);
                if(sum.Count!=amounts.Count || sum.Any(x=>!amounts.TryGetValue(x.Key,out var ml) || ml!=x.Value)) throw new ArgumentException("Layer evidence differs from actual liquid.");
            }
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
        private ShakeMotion shakeMotion; private int shakeRevision;
        private bool stableFloat; private double? lastFloatHeight;
        public bool ShakerClosed { get; private set; }
        public bool ShakeActive => shakeMotion!=null;
        public int ShakeReversals => shakeMotion?.Reversals ?? Get(ContainerRole.Shaker).Shake?.Reversals ?? 0;
        private long acceptedInterval;
        private int flowGeneration,lastTraceFlow=-1; private bool fineFlow,lastTraceFine; private decimal flowIntensity,lastTraceIntensity;
        public bool FlowActive => flow; public bool StirActive => motion!=null;
        public double StirAcceptedDegrees => motion?.Degrees ?? Get(ContainerRole.Mixing).Stamp?.Degrees ?? 0;
        public string GarnishId => garnish;
        public int Revision { get; private set; }
        public BrewingSession(BartendingCatalog catalog,BrewingTuning tuning,IEnumerable<string> supplied)
        {
            this.catalog=catalog ?? throw new ArgumentNullException(nameof(catalog)); this.tuning=tuning ?? throw new ArgumentNullException(nameof(tuning));
            supply=new HashSet<string>(supplied,StringComparer.Ordinal);
            if(supply.Any(x=>!catalog.Ingredients.Contains(x))) throw new ArgumentException("Unknown supply ingredient.");
            containers=new Dictionary<ContainerRole,BrewContainer> { [ContainerRole.Jigger]=new BrewContainer(tuning.JiggerMl),[ContainerRole.Mixing]=new BrewContainer(tuning.MixingMl),[ContainerRole.Serving]=new BrewContainer(tuning.ServingMl),[ContainerRole.Shaker]=new BrewContainer(tuning.MixingMl) };
        }
        public ContainerReadModel Container(ContainerRole role) => new ContainerReadModel(Get(role));
        private BrewContainer Get(ContainerRole role) => containers.TryGetValue(role,out var c) ? c : throw new ArgumentOutOfRangeException(nameof(role));
        private void CheckSupply(string ingredient)
        { if(ingredient==null || !catalog.Ingredients.Contains(ingredient) || !supply.Contains(ingredient)) throw new InvalidOperationException("Ingredient is not supplied: "+ingredient); }
        private void Record(BrewAction kind,string source,ContainerRole target,decimal accepted=0,decimal spilled=0,IDictionary<string,decimal> components=null)
        {
            var prior=trace.Count>0 ? trace[trace.Count-1] : null;
            if(acceptedInterval>0 && lastTraceFlow==flowGeneration && lastTraceFine==fineFlow && lastTraceIntensity==flowIntensity &&
                prior!=null && prior.ElapsedMicroseconds>0 && prior.Kind==kind && prior.Source==source && prior.Target==target)
            {
                var amounts=prior.AcceptedByIngredient.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal);
                if(components!=null) foreach(var a in components) BrewContainer.Add(amounts,a.Key,a.Value);
                trace[trace.Count-1]=new PreparationAction(prior.Sequence,kind,source,target,prior.AcceptedMl+accepted,prior.SpilledMl+spilled,
                    Get(target).Revision,amounts,checked(prior.ElapsedMicroseconds+acceptedInterval));
            }
            else trace.Add(new PreparationAction(trace.Count+1,kind,source,target,accepted,spilled,Get(target).Revision,components,acceptedInterval));
            lastTraceFlow=acceptedInterval>0 ? flowGeneration : -1; lastTraceFine=fineFlow; lastTraceIntensity=flowIntensity; Revision++;
        }
        public LiquidMovement Pour(string ingredient,ContainerRole target,decimal ml,BrewAction kind=BrewAction.Pour)
        {
            CheckSupply(ingredient); LiquidMath.Validate(ml); var c=Get(target);
            CheckOpen(target);
            if(kind!=BrewAction.Pour && kind!=BrewAction.Dash && kind!=BrewAction.Splash) throw new ArgumentException("Invalid supply action.");
            if(kind==BrewAction.Dash && ml!=tuning.DashMl) throw new ArgumentException("Dash requires the authored game unit.");
            if(ml==0) return LiquidMovement.Empty();
            decimal accepted=Math.Min(ml,c.Capacity-c.Total),spill=ml-accepted;
            if(accepted>0)
            {
                BrewContainer.Add(c.Amounts,ingredient,accepted);
                if(kind==BrewAction.Dash) BrewContainer.Add(c.DashAmounts,ingredient,accepted);
                c.Revision++; c.Stamp=null; c.Strained=false;
                if(target!=ContainerRole.Serving) { c.Shake=null; c.ShakenAmounts.Clear(); }
                if(c.Layers!=null) c.Layers=new FloatLayers(c.Amounts);
            }
            var received=new Dictionary<string,decimal> { [ingredient]=accepted };
            Record(kind,ingredient,target,accepted,spill,received);
            return new LiquidMovement(received,spill);
        }
        public LiquidMovement Transfer(ContainerRole source,ContainerRole target,decimal ml,BrewAction kind)
        {
            LiquidMath.Validate(ml); var from=Get(source); var to=Get(target);
            CheckOpen(source); CheckOpen(target);
            if(source==target || (kind!=BrewAction.Strain && kind!=BrewAction.PourFromContainer && kind!=BrewAction.Float)) throw new ArgumentException("Invalid container transfer.");
            if(kind==BrewAction.Float && (source!=ContainerRole.Jigger || target!=ContainerRole.Serving)) throw new ArgumentException("Float transfers jigger to serving.");
            if(kind==BrewAction.Strain && (source!=ContainerRole.Mixing || target!=ContainerRole.Serving)) throw new ArgumentException("Strain is mixing to serving.");
            decimal moved=Math.Min(ml,from.Total); if(moved==0) return LiquidMovement.Empty();
            decimal accepted=Math.Min(moved,to.Capacity-to.Total),spill=moved-accepted;
            Dictionary<string,decimal> extracted,received;
            if(from.Layers!=null)
            {
                received=from.Layers.TakeTop(accepted); extracted=new Dictionary<string,decimal>(received);
                foreach(var a in from.Layers.TakeTop(spill)) BrewContainer.Add(extracted,a.Key,a.Value);
            }
            else { extracted=LiquidMath.Portion(from.Amounts,moved); received=LiquidMath.Portion(extracted,accepted); }
            if(kind==BrewAction.Float && to.Layers==null) to.Layers=new FloatLayers(to.Amounts);
            decimal before=from.Total,targetBefore=to.Total; var stamp=from.Stamp;
            var shakeStamp=from.Shake;
            bool combineShake=targetBefore==0 || (to.Shake!=null && shakeStamp!=null && to.Shake.Id==shakeStamp.Id);
            foreach(var item in extracted)
            {
                decimal got=received.TryGetValue(item.Key,out decimal value) ? value : 0;
                decimal old=from.Amounts[item.Key]; decimal oldDash=from.DashAmounts.TryGetValue(item.Key,out decimal dash) ? dash : 0;
                decimal removedDash=item.Value==old ? oldDash : LiquidMath.Down(oldDash*item.Value/old);
                decimal acceptedDash=item.Value==0 ? 0 : LiquidMath.Down(removedDash*got/item.Value);
                decimal oldShaken=from.ShakenAmounts.TryGetValue(item.Key,out var shaken) ? shaken : 0;
                decimal removedShaken=item.Value==old ? oldShaken : LiquidMath.Down(oldShaken*item.Value/old);
                decimal acceptedShaken=item.Value==0 ? 0 : LiquidMath.Down(removedShaken*got/item.Value);
                from.Amounts[item.Key]-=item.Value; if(from.Amounts[item.Key]==0) from.Amounts.Remove(item.Key);
                if(oldDash>0) { from.DashAmounts[item.Key]-=removedDash; if(from.DashAmounts[item.Key]==0) from.DashAmounts.Remove(item.Key); }
                BrewContainer.Add(to.Amounts,item.Key,got); BrewContainer.Add(to.DashAmounts,item.Key,acceptedDash);
                if(oldShaken>0) { from.ShakenAmounts[item.Key]-=removedShaken; if(from.ShakenAmounts[item.Key]==0) from.ShakenAmounts.Remove(item.Key); }
                if(combineShake) BrewContainer.Add(to.ShakenAmounts,item.Key,acceptedShaken);
            }
            from.Revision++;
            if(from.Total==0) { from.Stamp=null; from.Strained=false; from.Shake=null; from.ShakenAmounts.Clear(); }
            if(accepted>0)
            {
                to.Revision++;
                to.Stamp=targetBefore==0 || (to.Stamp!=null && stamp!=null && to.Stamp.Id==stamp.Id) ? stamp : null;
                to.Strained=kind==BrewAction.Strain && (targetBefore==0 || to.Strained);
                to.Shake=combineShake ? shakeStamp : null;
                if(!combineShake) to.ShakenAmounts.Clear();
                if(kind==BrewAction.Float) to.Layers.Add(received,stableFloat);
                else if(to.Layers!=null) to.Layers=new FloatLayers(to.Amounts);
            }
            if(before-from.Total!=accepted+spill) throw new InvalidOperationException("Liquid conservation failure.");
            Record(kind,source.ToString().ToLowerInvariant(),target,accepted,spill,received);
            return new LiquidMovement(received,spill);
        }
        public void AddIce(ContainerRole target)
        { CheckOpen(target); var c=Get(target); if(c.Ice) return; c.Ice=true; c.Revision++; c.Stamp=null; c.Shake=null; c.ShakenAmounts.Clear(); Record(BrewAction.AddIce,"ice",target); }
        public void GentleStir()
        {
            if(flow || motion!=null || shakeMotion!=null) throw new InvalidOperationException("Another tool action is active.");
            if(Get(ContainerRole.Serving).Total==0) return;
            var serving=Get(ContainerRole.Serving); if(serving.Layers!=null) serving.Layers=new FloatLayers(serving.Amounts);
            Record(BrewAction.GentleStir,"spoon",ContainerRole.Serving);
        }
        private void CheckOpen(ContainerRole role)
        { if(role==ContainerRole.Shaker && ShakerClosed) throw new InvalidOperationException("The shaker is closed."); }
        public void SetShakerClosed(bool closed)
        {
            if(ShakerClosed==closed) return;
            if(flow || motion!=null) throw new InvalidOperationException("Another tool action is active.");
            if(!closed) EndShake(); ShakerClosed=closed;
            Record(closed ? BrewAction.CloseShaker : BrewAction.OpenShaker,"lid",ContainerRole.Shaker);
        }
        public void BeginShake(double position)
        {
            if(flow || motion!=null || shakeMotion!=null) throw new InvalidOperationException("Another tool action is active.");
            var c=Get(ContainerRole.Shaker); if(!ShakerClosed || c.Total==0 || !c.Ice) return;
            shakeMotion=new ShakeMotion(tuning.Shake,position,c.Shake); shakeRevision=c.Revision;
        }
        public void MoveShake(double position,long microseconds) { shakeMotion?.Move(position,microseconds); }
        public void EndShake()
        {
            if(shakeMotion==null) return; var c=Get(ContainerRole.Shaker);
            if(ShakerClosed && c.Revision==shakeRevision && c.Total>0 && c.Ice)
            {
                c.Shake=new ShakeStamp(shakeMotion.Reversals,shakeMotion.StableReversals,tuning.Shake);
                c.ShakenAmounts.Clear(); foreach(var a in c.Amounts) c.ShakenAmounts.Add(a.Key,a.Value);
                Record(BrewAction.Shake,"shaker",ContainerRole.Shaker);
            }
            shakeMotion=null;
        }
        public void Garnish(string id,string placement)
        {
            if(!GarnishRules.Valid(id,placement)) throw new ArgumentException("Unsupported garnish placement.");
            if(Get(ContainerRole.Serving).Total==0) throw new InvalidOperationException("No serving liquid.");
            garnish=id; position=placement; Record(BrewAction.Garnish,id,ContainerRole.Serving);
        }
        public void BeginPour(string ingredient,ContainerRole target,BrewAction kind=BrewAction.Pour)
        { if(kind!=BrewAction.Pour && kind!=BrewAction.Splash) throw new ArgumentException("Bottle flow must pour or splash."); CheckSupply(ingredient); Get(target); CheckOpen(target); StartFlow(); flowIngredient=ingredient; flowSource=null; flowTarget=target; flowKind=kind; }
        public void BeginTransfer(ContainerRole source,ContainerRole target,BrewAction kind)
        {
            Get(source); Get(target);
            CheckOpen(source); CheckOpen(target);
            if(source==target || (kind!=BrewAction.Strain && kind!=BrewAction.PourFromContainer && kind!=BrewAction.Float) ||
                (kind==BrewAction.Strain && (source!=ContainerRole.Mixing || target!=ContainerRole.Serving)) ||
                (kind==BrewAction.Float && (source!=ContainerRole.Jigger || target!=ContainerRole.Serving))) throw new ArgumentException("Invalid transfer binding.");
            StartFlow(); flowSource=source; flowIngredient=null; flowTarget=target; flowKind=kind;
        }
        private void StartFlow() { if(flow || motion!=null || shakeMotion!=null) throw new InvalidOperationException("Another tool action is active."); flow=true; remainder=0; flowGeneration++; }
        public LiquidMovement UpdateFlow(long microseconds,decimal intensity,bool fine)
        {
            if(microseconds<0 || intensity<0 || intensity>1) throw new ArgumentOutOfRangeException(nameof(microseconds));
            if(!flow || microseconds==0 || intensity==0) return LiquidMovement.Empty();
            decimal rate=flowSource.HasValue ? (fine ? tuning.FineTransferRate : tuning.TransferRate) : (fine ? tuning.FinePourRate : tuning.PourRate);
            fineFlow=fine; flowIntensity=intensity;
            decimal exact=rate*intensity*microseconds/1000000m+remainder,ml=LiquidMath.Down(exact); remainder=exact-ml;
            acceptedInterval=microseconds;
            try { return flowSource.HasValue ? Transfer(flowSource.Value,flowTarget,ml,flowKind) : Pour(flowIngredient,flowTarget,ml,flowKind); }
            finally { acceptedInterval=0; }
        }
        public void EndFlow() { flow=false; remainder=0; flowIngredient=null; flowSource=null; stableFloat=false; lastFloatHeight=null; }
        public void BeginFloat() { BeginTransfer(ContainerRole.Jigger,ContainerRole.Serving,BrewAction.Float); lastFloatHeight=null; }
        public LiquidMovement UpdateFloat(long microseconds,decimal intensity,double height)
        {
            if(microseconds<0 || intensity<0 || intensity>1 || double.IsNaN(height) || double.IsInfinity(height) || height<0 || height>1) throw new ArgumentOutOfRangeException(nameof(height));
            if(!flow || flowKind!=BrewAction.Float || microseconds==0) return LiquidMovement.Empty();
            stableFloat=tuning.TransferRate*intensity<=tuning.Float.MaxStableRate && height<=tuning.Float.MaxHeight &&
                (!lastFloatHeight.HasValue || Math.Abs(height-lastFloatHeight.Value)*1000000/microseconds<=tuning.Float.MaxHeightSpeed);
            lastFloatHeight=height;
            try { return UpdateFlow(microseconds,intensity,false); } finally { stableFloat=false; }
        }
        public void BeginStir(double x,double y)
        {
            if(flow || motion!=null || shakeMotion!=null) throw new InvalidOperationException("Another tool action is active.");
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
        public void CancelInput() { EndFlow(); EndStir(); EndShake(); }
        public ServiceSnapshot Freeze() => new ServiceSnapshot(attempt,tuning.Revision,Get(ContainerRole.Serving),garnish,position,trace,tuning.Float.Revision);
    }
}
