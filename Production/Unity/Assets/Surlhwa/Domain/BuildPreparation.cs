using System.Collections.Generic;
using System.Linq;

namespace Surlhwa.Domain
{
    internal static class BuildPreparation
    {
        internal static void Validate(ServiceSnapshot snapshot,List<string> reasons)
        {
            if(!snapshot.HasIce) reasons.Add("serving_ice_missing");
            var additions=snapshot.Trace.Where(x=>x.Target==ContainerRole.Serving && x.AcceptedMl>0).ToArray();
            if(additions.Length==0 || additions.Any(x=>x.Kind==BrewAction.Strain ||
                (x.Kind==BrewAction.PourFromContainer && x.Source!="jigger"))) reasons.Add("direct_build_missing");
            var gentle=snapshot.Trace.LastOrDefault(x=>x.Target==ContainerRole.Serving && x.Kind==BrewAction.GentleStir);
            if(gentle==null || snapshot.Trace.Any(x=>x.Sequence>gentle.Sequence &&
                ((x.Target==ContainerRole.Serving && (x.AcceptedMl>0 || x.Kind==BrewAction.AddIce)) ||
                 (x.Source=="serving" && x.AcceptedMl+x.SpilledMl>0)))) reasons.Add("gentle_stir_batch_missing");
        }
    }
}
