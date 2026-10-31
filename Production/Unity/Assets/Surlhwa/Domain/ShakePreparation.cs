using System.Collections.Generic;
using System.Linq;

namespace Surlhwa.Domain
{
    internal static class ShakePreparation
    {
        internal static bool CoversCurrentBatch(StirRecipe recipe,BrewingTuning tuning,ServiceSnapshot s) =>
            s.Shake!=null && s.Amounts.Where(x=>x.Key!=recipe.LateIngredient).All(x=>
                s.Shake.Amounts.TryGetValue(x.Key,out var ml) && ml>=x.Value-tuning.ExtraToleranceMl);
        internal static void Validate(StirRecipe recipe,BrewingTuning tuning,ServiceSnapshot s,List<string> reasons)
        {
            var proof=s.Shake;
            if(proof==null || !proof.HowGood || !CoversCurrentBatch(recipe,tuning,s)) reasons.Add("shake_batch_missing");
            var transfers=s.Trace.Where(x=>x.Target==ContainerRole.Serving && x.Source=="shaker" && x.AcceptedMl>0).ToArray();
            if(recipe.ShakeFinish==ShakeFinishPolicy.StrainThenPourAndGentleStir)
            {
                if(s.HasIce) reasons.Add("serving_ice_unexpected");
                ValidateMeasuredFinish(recipe,tuning,s,transfers,reasons); return;
            }
            if(transfers.Length==0 || transfers.Any(x=>x.Kind!=BrewAction.PourFromContainer)) reasons.Add("shaker_pour_missing");
            if(s.HasIce) reasons.Add("serving_ice_unexpected");
            if(recipe.LateIngredient!=null)
            {
                var late=s.Trace.Where(x=>x.Target==ContainerRole.Serving && x.Source==recipe.LateIngredient && x.Kind==BrewAction.Splash).ToArray();
                int transferEnd=transfers.Length==0 ? int.MaxValue : transfers.Max(x=>x.Sequence);
                if(late.Where(x=>x.Sequence>transferEnd).Sum(x=>x.AcceptedMl)<recipe.Amounts[recipe.LateIngredient]-tuning.IngredientToleranceMl ||
                    (proof!=null && proof.Amounts.TryGetValue(recipe.LateIngredient,out var shaken) && shaken>0)) reasons.Add("late_splash_missing");
            }
        }
        private static void ValidateMeasuredFinish(StirRecipe recipe,BrewingTuning tuning,ServiceSnapshot s,PreparationAction[] transfers,List<string> reasons)
        {
            var additions=s.Trace.Where(x=>x.Target==ContainerRole.Serving && x.AcceptedMl>0).ToArray();
            if(transfers.Length==0 || transfers.Any(x=>x.Kind!=BrewAction.Strain) ||
                additions.Any(x=>x.AcceptedByIngredient.Any(a=>a.Key!=recipe.LateIngredient && a.Value>0) &&
                    (x.Source!="shaker" || x.Kind!=BrewAction.Strain))) reasons.Add("shaker_strain_missing");
            int transferEnd=transfers.Length==0 ? int.MaxValue : transfers.Max(x=>x.Sequence);
            var late=additions.Where(x=>x.AcceptedByIngredient.TryGetValue(recipe.LateIngredient,out var ml) && ml>0).ToArray();
            bool measured=late.Length>0 && late.All(x=>x.Sequence>transferEnd &&
                ((x.Kind==BrewAction.Pour && x.Source==recipe.LateIngredient) ||
                 (x.Kind==BrewAction.PourFromContainer && x.Source=="jigger" && x.AcceptedByIngredient.All(a=>a.Key==recipe.LateIngredient || a.Value==0))));
            if(!measured || late.Sum(x=>x.AcceptedByIngredient[recipe.LateIngredient])<recipe.Amounts[recipe.LateIngredient]-tuning.IngredientToleranceMl ||
                (s.Shake!=null && s.Shake.Amounts.TryGetValue(recipe.LateIngredient,out var shaken) && shaken>0)) reasons.Add("late_measured_pour_missing");
            var gentle=s.Trace.LastOrDefault(x=>x.Target==ContainerRole.Serving && x.Kind==BrewAction.GentleStir);
            if(gentle==null || s.Trace.Any(x=>x.Sequence>gentle.Sequence &&
                ((x.Target==ContainerRole.Serving && (x.AcceptedMl>0 || x.Kind==BrewAction.AddIce)) ||
                 (x.Source=="serving" && x.AcceptedMl+x.SpilledMl>0)))) reasons.Add("gentle_stir_batch_missing");
        }
    }
}
