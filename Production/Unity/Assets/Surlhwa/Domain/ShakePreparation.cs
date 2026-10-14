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
    }
}
