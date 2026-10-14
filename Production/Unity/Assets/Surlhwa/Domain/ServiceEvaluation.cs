using System;
using System.Collections.Generic;
using System.Linq;

namespace Surlhwa.Domain
{
    public enum PreparationResult { NotEvaluable, Weak, Good }
    public enum DrinkRecognition { Unknown, Ambiguous, Recognized }
    public sealed class DrinkRequest
    {
        public string Tag { get; }
        public string CocktailId { get; }
        public IReadOnlyList<string> AnyTags { get; } public IReadOnlyList<string> AllTags { get; } public IReadOnlyList<string> ExcludedTags { get; }
        public DrinkRequest(string tag) : this(null,new[]{tag},null,null) { Tag=tag; }
        private DrinkRequest(string cocktail,IEnumerable<string> any,IEnumerable<string> all,IEnumerable<string> excluded)
        {
            CocktailId=cocktail; if(cocktail!=null) _=new StableId<CocktailIdentity>(cocktail);
            string[] Validate(IEnumerable<string> tags)
            { var values=(tags ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal).ToArray(); foreach(var tag in values) _=new StableId<TextIdentity>(tag); return values; }
            AnyTags=Array.AsReadOnly(Validate(any)); AllTags=Array.AsReadOnly(Validate(all)); ExcludedTags=Array.AsReadOnly(Validate(excluded));
            if(cocktail==null && AnyTags.Count+AllTags.Count==0) throw new ArgumentException("Recommendation requires a positive interpretation.");
            Tag=AnyTags.FirstOrDefault() ?? AllTags.FirstOrDefault();
        }
        public static DrinkRequest Specific(string cocktailId)
        { if(cocktailId==null) throw new ArgumentNullException(nameof(cocktailId)); return new DrinkRequest(cocktailId,null,null,null); }
        public static DrinkRequest Recommendation(IEnumerable<string> any,IEnumerable<string> all=null,IEnumerable<string> excluded=null) => new DrinkRequest(null,any,all,excluded);
        internal bool Fits(CocktailDefinition drink) => CocktailId!=null ? CocktailId==drink.Id :
            (AnyTags.Count==0 || AnyTags.Any(drink.Tags.Contains)) && AllTags.All(drink.Tags.Contains) && !ExcludedTags.Any(drink.Tags.Contains);
    }
    public sealed class ServiceOutcome
    {
        public DrinkRecognition Recognition { get; }
        public string RecipeId { get; } public string CocktailId { get; }
        public PreparationResult Preparation { get; } public bool QualityGood { get; } public bool Fits { get; }
        public IReadOnlyCollection<string> Reasons { get; }
        public ServiceOutcome(DrinkRecognition recognition,StirRecipe recipe,PreparationResult preparation,bool quality,bool fits,List<string> reasons)
        {
            if(!Enum.IsDefined(typeof(DrinkRecognition),recognition) || !Enum.IsDefined(typeof(PreparationResult),preparation) ||
                (recognition==DrinkRecognition.Recognized)!=(recipe!=null) || (recipe==null && (fits || preparation!=PreparationResult.NotEvaluable))) throw new ArgumentException("Invalid saved outcome.");
            Recognition=recognition; RecipeId=recipe?.Id; CocktailId=recipe?.CocktailId; Preparation=preparation; QualityGood=quality; Fits=fits; Reasons=Array.AsReadOnly(reasons.ToArray());
        }
    }
    public static class ServiceEvaluator
    {
        public static ServiceOutcome Evaluate(BartendingCatalog catalog,BrewingTuning tuning,ServiceSnapshot snapshot,DrinkRequest request)
        {
            if(snapshot.TuningRevision!=tuning.Revision) throw new ArgumentException("Snapshot tuning revision mismatch.");
            var candidates=catalog.Recipes.Values.Where(r=>Matches(r,tuning,snapshot)).ToArray();
            if(candidates.Length!=1) return new ServiceOutcome(candidates.Length==0 ? DrinkRecognition.Unknown : DrinkRecognition.Ambiguous,
                null,PreparationResult.NotEvaluable,snapshot.StirQualityGood,false,new List<string>{candidates.Length==0 ? "unrecognized_composition" : "ambiguous_composition"});
            var recipe=candidates[0]; var reasons=new List<string>();
            if(recipe.Method=="stir")
            {
                if(!snapshot.Strained) reasons.Add("strain_missing");
                if(!snapshot.StirHowGood || recipe.Amounts.Keys.Any(x=>!snapshot.StirIngredients.Contains(x))) reasons.Add("stir_batch_missing");
            }
            else if(recipe.Method=="build") BuildPreparation.Validate(snapshot,reasons);
            else if(recipe.Method=="shake") ShakePreparation.Validate(recipe,tuning,snapshot,reasons);
            else FloatLayers.Validate(recipe,snapshot,reasons);
            if(recipe.DashIngredient!=null && (!snapshot.DashAmounts.TryGetValue(recipe.DashIngredient,out decimal dash) || dash<recipe.Amounts[recipe.DashIngredient]-tuning.ExtraToleranceMl)) reasons.Add("dash_missing");
            if(recipe.RequiredGarnish!=null && (snapshot.Garnish!=recipe.RequiredGarnish || snapshot.GarnishPosition!=recipe.GarnishPosition)) reasons.Add("required_garnish_missing");
            bool how=reasons.Count==0;
            // Build has preparation constraints, not an invented timing skill grade.
            bool quality=recipe.Method=="build" || (recipe.Method=="shake" ? (snapshot.Shake?.QualityGood ?? false) && ShakePreparation.CoversCurrentBatch(recipe,tuning,snapshot) :
                recipe.Method=="float" ? FloatLayers.Quality(snapshot) : snapshot.StirQualityGood);
            if(!quality) reasons.Add(recipe.Method+"_quality_weak");
            bool fits=request.Fits(catalog.Cocktails[recipe.CocktailId]); if(!fits) reasons.Add("request_mismatch");
            return new ServiceOutcome(DrinkRecognition.Recognized,recipe,how ? PreparationResult.Good : PreparationResult.Weak,quality,fits,reasons);
        }
        private static bool Matches(StirRecipe r,BrewingTuning t,ServiceSnapshot s)
        {
            decimal expected=r.Amounts.Values.Sum();
            if(Math.Abs(s.TotalMl-expected)>expected*t.RecognitionFraction) return false;
            if(s.Amounts.Where(x=>!r.Amounts.ContainsKey(x.Key)).Sum(x=>x.Value)>t.ExtraToleranceMl) return false;
            foreach(var a in r.Amounts)
                if(!s.Amounts.TryGetValue(a.Key,out decimal value) || Math.Abs(value-a.Value)>Math.Max(t.IngredientToleranceMl,a.Value*t.RecognitionFraction)) return false;
            return true;
        }
    }
}
