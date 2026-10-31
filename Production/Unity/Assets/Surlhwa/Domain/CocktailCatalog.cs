using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Surlhwa.Domain
{
    public sealed class IngredientIdentity { }
    public sealed class CocktailIdentity { }
    public sealed class RecipeIdentity { }
    public sealed class CocktailDefinition
    {
        public StableId<CocktailIdentity> Key { get; }
        public string Id => Key.Value;
        public string Name { get; }
        public IReadOnlyCollection<string> Tags { get; }
        public CocktailDefinition(string id,string name,IEnumerable<string> tags)
        {
            Key=new StableId<CocktailIdentity>(id);
            if(string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Cocktail name is required.");
            Name=name; Tags=Array.AsReadOnly(tags.Distinct(StringComparer.Ordinal).ToArray());
            foreach(var tag in Tags) _=new StableId<TextIdentity>(tag);
        }
    }
    public enum ShakeFinishPolicy { PourThenSplash, StrainThenPourAndGentleStir, StrainOnly }
    public sealed class StirRecipe
    {
        public StableId<RecipeIdentity> Key { get; }
        public StableId<CocktailIdentity> CocktailKey { get; }
        public string Id => Key.Value;
        public string CocktailId => CocktailKey.Value;
        public IReadOnlyDictionary<string,decimal> Amounts { get; }
        public string DashIngredient { get; }
        public string RequiredGarnish { get; }
        public string Method { get; }
        public string GarnishPosition { get; }
        public string LateIngredient { get; }
        public ShakeFinishPolicy ShakeFinish { get; }
        public IReadOnlyList<string> LayerOrder { get; }
        public StirRecipe(string id,string cocktailId,IDictionary<string,decimal> amounts,string dashIngredient=null,string requiredGarnish=null,
            string method="stir",string garnishPosition="in_glass",string lateIngredient=null,IEnumerable<string> layerOrder=null,
            ShakeFinishPolicy shakeFinish=ShakeFinishPolicy.PourThenSplash)
        {
            Key=new StableId<RecipeIdentity>(id); CocktailKey=new StableId<CocktailIdentity>(cocktailId);
            if(amounts==null || amounts.Count==0) throw new ArgumentException("A recipe needs actual ingredients.");
            foreach(var a in amounts) { _=new StableId<IngredientIdentity>(a.Key); LiquidMath.Validate(a.Value); if(a.Value==0) throw new ArgumentException("Recipe amounts must be positive."); }
            if(dashIngredient!=null && !amounts.ContainsKey(dashIngredient)) throw new ArgumentException("Dash ingredient is absent from recipe.");
            if(method!="stir" && method!="build" && method!="shake" && method!="float") throw new ArgumentException("Unsupported recipe method.");
            if(lateIngredient!=null && (method!="shake" || !amounts.ContainsKey(lateIngredient))) throw new ArgumentException("Invalid late addition.");
            if(!Enum.IsDefined(typeof(ShakeFinishPolicy),shakeFinish) ||
                (shakeFinish==ShakeFinishPolicy.StrainThenPourAndGentleStir && (method!="shake" || lateIngredient==null)) ||
                (shakeFinish==ShakeFinishPolicy.StrainOnly && (method!="shake" || lateIngredient!=null))) throw new ArgumentException("Invalid Shake finish policy.");
            if(requiredGarnish!=null && !GarnishRules.Valid(requiredGarnish,garnishPosition)) throw new ArgumentException("Unsupported garnish.");
            Amounts=new ReadOnlyDictionary<string,decimal>(new Dictionary<string,decimal>(amounts,StringComparer.Ordinal));
            DashIngredient=dashIngredient; RequiredGarnish=requiredGarnish;
            Method=method; GarnishPosition=garnishPosition;
            LateIngredient=lateIngredient; ShakeFinish=shakeFinish;
            LayerOrder=Array.AsReadOnly((layerOrder ?? Array.Empty<string>()).ToArray());
            if(method=="float" && (LayerOrder.Count!=amounts.Count || LayerOrder.Distinct().Count()!=LayerOrder.Count || LayerOrder.Any(x=>!amounts.ContainsKey(x)))) throw new ArgumentException("Invalid layer order.");
        }
    }
    internal static class GarnishRules
    {
        internal static bool Valid(string id,string placement) =>
            (id=="cherry" && placement=="in_glass") || ((id=="orange_slice" || id=="orange_zest") && placement=="rim");
    }
    public sealed class BartendingCatalog
    {
        public IReadOnlyCollection<string> Ingredients { get; }
        public IReadOnlyDictionary<string,CocktailDefinition> Cocktails { get; }
        public IReadOnlyDictionary<string,StirRecipe> Recipes { get; }
        public BartendingCatalog(IEnumerable<string> ingredients,IEnumerable<CocktailDefinition> cocktails,IEnumerable<StirRecipe> recipes)
        {
            var ids=ingredients.ToArray(); foreach(var id in ids) _=new StableId<IngredientIdentity>(id);
            if(ids.Distinct(StringComparer.Ordinal).Count()!=ids.Length) throw new ArgumentException("Duplicate ingredient ID.");
            var drinks=new Dictionary<string,CocktailDefinition>(StringComparer.Ordinal);
            foreach(var d in cocktails) { if(drinks.ContainsKey(d.Id)) throw new ArgumentException("Duplicate cocktail ID."); drinks.Add(d.Id,d); }
            var rules=new Dictionary<string,StirRecipe>(StringComparer.Ordinal);
            foreach(var r in recipes)
            {
                if(!drinks.ContainsKey(r.CocktailId) || r.Amounts.Keys.Any(x=>!ids.Contains(x,StringComparer.Ordinal))) throw new ArgumentException("Unresolved recipe reference.");
                if(rules.ContainsKey(r.Id)) throw new ArgumentException("Duplicate recipe ID."); rules.Add(r.Id,r);
            }
            Ingredients=Array.AsReadOnly(ids); Cocktails=new ReadOnlyDictionary<string,CocktailDefinition>(drinks); Recipes=new ReadOnlyDictionary<string,StirRecipe>(rules);
        }
    }
    public sealed class BrewingTuning
    {
        public ShakeTuning Shake { get; }
        public FloatTuning Float { get; }
        public string Revision { get; }
        public decimal JiggerMl { get; } public decimal MixingMl { get; } public decimal ServingMl { get; }
        public decimal PourRate { get; } public decimal FinePourRate { get; }
        public decimal TransferRate { get; } public decimal FineTransferRate { get; } public decimal DashMl { get; }
        public double HowDegrees { get; } public double QualityDegrees { get; }
        public double MinSpeed { get; } public double MaxSpeed { get; } public double StableFraction { get; }
        public decimal RecognitionFraction { get; } public decimal IngredientToleranceMl { get; } public decimal ExtraToleranceMl { get; }
        public BrewingTuning(string revision,decimal jigger,decimal mixing,decimal serving,decimal pour,decimal finePour,
            decimal transfer,decimal fineTransfer,decimal dash,double how,double quality,double minSpeed,double maxSpeed,double stable,
            decimal recognitionFraction=.05m,decimal ingredientTolerance=.25m,decimal extraTolerance=.01m,ShakeTuning shake=null,FloatTuning floatTuning=null)
        {
            if(string.IsNullOrWhiteSpace(revision)) throw new ArgumentException("Tuning revision required.");
            foreach(var x in new[]{jigger,mixing,serving,pour,finePour,transfer,fineTransfer,dash}) { LiquidMath.Validate(x); if(x<=0) throw new ArgumentOutOfRangeException(nameof(x)); }
            foreach(var x in new[]{how,quality,minSpeed,maxSpeed,stable}) if(double.IsNaN(x)||double.IsInfinity(x)||x<=0) throw new ArgumentOutOfRangeException(nameof(x));
            if(quality<how || maxSpeed<minSpeed || stable>1 || recognitionFraction<=0 || recognitionFraction>=1 || ingredientTolerance<0 || extraTolerance<0) throw new ArgumentException("Invalid tuning bounds.");
            Revision=revision; JiggerMl=jigger; MixingMl=mixing; ServingMl=serving; PourRate=pour; FinePourRate=finePour;
            TransferRate=transfer; FineTransferRate=fineTransfer; DashMl=dash; HowDegrees=how; QualityDegrees=quality;
            MinSpeed=minSpeed; MaxSpeed=maxSpeed; StableFraction=stable; RecognitionFraction=recognitionFraction;
            IngredientToleranceMl=ingredientTolerance; ExtraToleranceMl=extraTolerance;
            Shake=shake ?? new ShakeTuning();
            Float=floatTuning ?? new FloatTuning();
        }
    }
    internal static class LiquidMath
    {
        internal static decimal Down(decimal value) => decimal.Truncate(value*1000000m)/1000000m;
        internal static void Validate(decimal value)
        {
            if(value<0) throw new ArgumentOutOfRangeException(nameof(value));
            if(Down(value)!=value) throw new ArgumentException("Liquid amounts require at most six decimal places.");
        }
        internal static IReadOnlyDictionary<string,decimal> Copy(IDictionary<string,decimal> values) =>
            new ReadOnlyDictionary<string,decimal>(new Dictionary<string,decimal>(values,StringComparer.Ordinal));
        internal static Dictionary<string,decimal> Portion(IDictionary<string,decimal> source,decimal amount)
        {
            var result=new Dictionary<string,decimal>(StringComparer.Ordinal); decimal total=source.Values.Sum();
            if(total==0 || amount==0) return result;
            decimal remaining=amount;
            foreach(var pair in source.OrderBy(x=>x.Key,StringComparer.Ordinal))
            { decimal value=amount==total ? pair.Value : Down(pair.Value*amount/total); result[pair.Key]=value; remaining-=value; }
            // Distribute quantization residue only from ingredients that actually exist.
            foreach(var key in result.Keys.ToArray())
            { decimal extra=Math.Min(remaining,source[key]-result[key]); result[key]+=extra; remaining-=extra; if(remaining==0) break; }
            if(remaining!=0) throw new InvalidOperationException("Transfer residue is not conserved.");
            return result;
        }
    }
}
