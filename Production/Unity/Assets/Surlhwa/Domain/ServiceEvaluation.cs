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
        public DrinkRequest(string tag) { _=new StableId<TextIdentity>(tag); Tag=tag; }
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
            if(!snapshot.Strained) reasons.Add("strain_missing");
            if(!snapshot.StirHowGood || recipe.Amounts.Keys.Any(x=>!snapshot.StirIngredients.Contains(x))) reasons.Add("stir_batch_missing");
            if(recipe.DashIngredient!=null && (!snapshot.DashAmounts.TryGetValue(recipe.DashIngredient,out decimal dash) || dash<recipe.Amounts[recipe.DashIngredient]-tuning.ExtraToleranceMl)) reasons.Add("dash_missing");
            if(recipe.RequiredGarnish!=null && (snapshot.Garnish!=recipe.RequiredGarnish || snapshot.GarnishPosition!="in_glass")) reasons.Add("required_garnish_missing");
            bool quality=snapshot.StirQualityGood; if(!quality) reasons.Add("stir_quality_weak");
            bool fits=catalog.Cocktails[recipe.CocktailId].Tags.Contains(request.Tag); if(!fits) reasons.Add("request_mismatch");
            bool how=!reasons.Any(x=>x=="strain_missing" || x=="stir_batch_missing" || x=="dash_missing" || x=="required_garnish_missing");
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
