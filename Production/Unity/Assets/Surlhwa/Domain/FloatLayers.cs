using System;
using System.Collections.Generic;
using System.Linq;

namespace Surlhwa.Domain
{
    public sealed class FloatTuning
    {
        public string Revision { get; }
        public decimal MaxStableRate { get; } public double MaxHeight { get; } public double MaxHeightSpeed { get; }
        public FloatTuning(string revision="float_v1",decimal maxRate=12m,double maxHeight=.25,double maxHeightSpeed=.5)
        {
            LiquidMath.Validate(maxRate);
            if(string.IsNullOrWhiteSpace(revision) || maxRate<=0 || double.IsNaN(maxHeight) || maxHeight<0 || maxHeight>1 ||
                double.IsNaN(maxHeightSpeed) || double.IsInfinity(maxHeightSpeed) || maxHeightSpeed<=0) throw new ArgumentException("Invalid Float tuning.");
            Revision=revision; MaxStableRate=maxRate; MaxHeight=maxHeight; MaxHeightSpeed=maxHeightSpeed;
        }
    }
    public sealed class FloatLayerEvidence
    {
        public IReadOnlyDictionary<string,decimal> Amounts { get; }
        public bool LowerBoundaryGood { get; }
        public FloatLayerEvidence(IDictionary<string,decimal> amounts,bool good)
        {
            if(amounts.Count==0) throw new ArgumentException("Empty layer.");
            foreach(var a in amounts) { _=new StableId<IngredientIdentity>(a.Key); LiquidMath.Validate(a.Value); if(a.Value<=0) throw new ArgumentException("Empty layer component."); }
            Amounts=LiquidMath.Copy(amounts); LowerBoundaryGood=good;
        }
    }
    // Quantities remain in BrewContainer. This companion tracks ordered portions and boundary history.
    internal sealed class FloatLayers
    {
        private sealed class Layer
        {
            internal readonly Dictionary<string,decimal> Amounts;
            internal bool Good;
            internal Layer(IDictionary<string,decimal> amounts,bool good) { Amounts=new Dictionary<string,decimal>(amounts); Good=good; }
        }
        private readonly List<Layer> layers=new List<Layer>();
        internal FloatLayers(IDictionary<string,decimal> existing) { if(existing.Count>0) layers.Add(new Layer(existing,true)); }
        internal void Add(IDictionary<string,decimal> amounts,bool good)
        {
            if(amounts.Values.Sum()==0) return;
            var top=layers.LastOrDefault();
            if(top!=null && top.Amounts.Count==1 && amounts.Count==1 && top.Amounts.Keys.First()==amounts.Keys.First())
            { BrewContainer.Add(top.Amounts,amounts.Keys.First(),amounts.Values.First()); top.Good &= good; }
            else layers.Add(new Layer(amounts,layers.Count==0 || good));
        }
        internal Dictionary<string,decimal> TakeTop(decimal ml)
        {
            var result=new Dictionary<string,decimal>(StringComparer.Ordinal);
            while(ml>0 && layers.Count>0)
            {
                var layer=layers[layers.Count-1]; decimal take=Math.Min(ml,layer.Amounts.Values.Sum());
                foreach(var a in LiquidMath.Portion(layer.Amounts,take))
                { BrewContainer.Add(result,a.Key,a.Value); layer.Amounts[a.Key]-=a.Value; if(layer.Amounts[a.Key]==0) layer.Amounts.Remove(a.Key); }
                if(layer.Amounts.Count==0) layers.RemoveAt(layers.Count-1); ml-=take;
            }
            if(ml!=0) throw new InvalidOperationException("Layer inventory differs from authoritative liquid.");
            return result;
        }
        internal IReadOnlyList<FloatLayerEvidence> Freeze() => Array.AsReadOnly(layers.Select(x=>new FloatLayerEvidence(x.Amounts,x.Good)).ToArray());
        internal static bool Quality(ServiceSnapshot s) => s.Layers.Count>1 && s.Layers.Skip(1).All(x=>x.LowerBoundaryGood);
        internal static void Validate(StirRecipe recipe,ServiceSnapshot s,List<string> reasons)
        {
            if(s.HasIce) reasons.Add("serving_ice_unexpected");
            if(s.Layers.Count!=recipe.LayerOrder.Count || s.Layers.Where((x,i)=>
                x.Amounts.Count!=1 || !x.Amounts.ContainsKey(recipe.LayerOrder[i])).Any()) reasons.Add("float_layer_order_missing");
        }
    }
}
