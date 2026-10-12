using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Surlhwa.Domain;
using static Surlhwa.ContentIO.StrictJson;

namespace Surlhwa.ContentIO
{
    public sealed class UnsupportedSaveVersionException : Exception { public UnsupportedSaveVersionException(string text):base(text) {} }
    public static class SliceSaveCodec
    {
        public static string Encode(SliceCheckpoint state,SliceContent content)
        {
            var facts=new JObject(); foreach(var f in state.Facts.OrderBy(x=>x.Key,StringComparer.Ordinal)) facts.Add(f.Key,new JArray(f.Value));
            var payload=new JObject {
                ["encounterIndex"]=state.EncounterIndex,["phase"]=state.Phase.ToString(),["lineIndex"]=state.LineIndex,
                ["consumedLines"]=new JArray(state.ConsumedLines),["completedOccurrences"]=new JArray(state.CompletedOccurrences),
                ["services"]=new JArray(state.Services.Select(WriteService)),["knownReceipts"]=Map(state.KnownReceipts),["facts"]=facts,
                ["actComplete"]=false,["memoryIds"]=new JArray(),["profileId"]=content.Profile.Id
            };
            var envelope=new JObject { ["format"]="surlhwa-production-save",["schemaVersion"]=1,["sequence"]=state.Sequence,["runId"]=state.RunId,
                ["mode"]="slice",["contentRevision"]=content.Revision,["contentHash"]=content.BundleHash,["tuningRevision"]=content.Tuning.Revision,
                ["aliasRevision"]="none_v1",["payload"]=payload,["payloadSha256"]=Hash(payload.ToString(Formatting.None)) };
            string json=envelope.ToString(Formatting.None); Decode(json,content); return json;
        }
        private static JObject Map<T>(IEnumerable<KeyValuePair<string,T>> map)
        { var result=new JObject(); foreach(var x in map.OrderBy(x=>x.Key,StringComparer.Ordinal)) result.Add(x.Key,JToken.FromObject(x.Value)); return result; }
        private static JObject WriteService(CommittedService service)
        {
            var s=service.Snapshot; var o=service.Outcome;
            return new JObject {
                ["occurrenceId"]=service.OccurrenceId,["responseKey"]=service.ResponseKey,["discovered"]=service.Discovered,
                ["snapshot"]=new JObject { ["attemptId"]=s.AttemptId,["tuningRevision"]=s.TuningRevision,["amounts"]=Map(s.Amounts),["dashAmounts"]=Map(s.DashAmounts),
                    ["hasIce"]=s.HasIce,["strained"]=s.Strained,["stirHowGood"]=s.StirHowGood,["stirQualityGood"]=s.StirQualityGood,
                    ["stirDegrees"]=(decimal)s.StirDegrees,["stableFraction"]=(decimal)s.StableFraction,["stirIngredients"]=new JArray(s.StirIngredients),
                    ["garnish"]=s.Garnish,["garnishPosition"]=s.GarnishPosition,
                    ["trace"]=new JArray(s.Trace.Select(a=>new JObject { ["sequence"]=a.Sequence,["kind"]=a.Kind.ToString(),["source"]=a.Source,
                        ["target"]=a.Target.ToString(),["acceptedMl"]=a.AcceptedMl,["spilledMl"]=a.SpilledMl,["compositionRevision"]=a.CompositionRevision,
                        ["components"]=Map(a.AcceptedByIngredient),["elapsedMicroseconds"]=a.ElapsedMicroseconds })) },
                ["outcome"]=new JObject { ["recognition"]=o.Recognition.ToString(),["recipeId"]=o.RecipeId,["cocktailId"]=o.CocktailId,
                    ["preparation"]=o.Preparation.ToString(),["qualityGood"]=o.QualityGood,["fits"]=o.Fits,["reasons"]=new JArray(o.Reasons) }
            };
        }
        public static SliceCheckpoint Decode(string json,SliceContent content)
        {
            try { return Read(Parse(json),content); }
            catch(UnsupportedSaveVersionException) { throw; }
            catch(ContentValidationException) { throw; }
            catch(Exception e) when(e is JsonException || e is ArgumentException || e is InvalidOperationException || e is OverflowException || e is KeyNotFoundException)
            { throw Invalid("Invalid save: "+e.Message); }
        }
        private static long Long(JObject obj,string field)
        { if(obj[field]?.Type!=JTokenType.Integer) throw Invalid("Integer required: "+field); return obj[field].Value<long>(); }
        private static T EnumValue<T>(JObject obj,string field) where T:struct
        { string text=String(obj,field); if(!Enum.TryParse(text,false,out T value) || !Enum.IsDefined(typeof(T),value) || value.ToString()!=text) throw Invalid("Unknown "+field); return value; }
        private static Dictionary<string,decimal> Amounts(JObject obj,SliceContent content)
        {
            var result=new Dictionary<string,decimal>(StringComparer.Ordinal);
            foreach(var p in obj.Properties()) { if(!content.Catalog.Ingredients.Contains(p.Name)) throw Invalid("Unknown ingredient: "+p.Name); decimal amount=Number(obj,p.Name); if(amount<0 || decimal.Round(amount,6)!=amount) throw Invalid("Invalid ml precision."); result.Add(p.Name,amount); }
            return result;
        }
        private static SliceCheckpoint Read(JObject root,SliceContent c)
        {
            int version=Int(root,"schemaVersion"); if(version>1) throw new UnsupportedSaveVersionException("Save uses a newer schema; it has not been overwritten."); if(version!=1) throw Invalid("Unsupported older save schema.");
            Fields(root,"format","schemaVersion","sequence","runId","mode","contentRevision","contentHash","tuningRevision","aliasRevision","payload","payloadSha256");
            if(String(root,"format")!="surlhwa-production-save" || String(root,"mode")!="slice" || String(root,"aliasRevision")!="none_v1") throw Invalid("Incompatible save namespace.");
            if(String(root,"contentRevision")!=c.Revision || String(root,"contentHash")!=c.BundleHash || String(root,"tuningRevision")!=c.Tuning.Revision) throw Invalid("Save content or tuning differs; no automatic migration.");
            var p=Object(root["payload"]); if(Hash(p.ToString(Formatting.None))!=String(root,"payloadSha256")) throw Invalid("Save checksum mismatch.");
            Fields(p,"encounterIndex","phase","lineIndex","consumedLines","completedOccurrences","services","knownReceipts","facts","actComplete","memoryIds","profileId");
            if(Bool(p,"actComplete") || Array(p,"memoryIds").Count!=0 || String(p,"profileId")!=c.Profile.Id) throw Invalid("Slice cannot grant ACT completion or memories.");
            string run=String(root,"runId"); int index=Int(p,"encounterIndex"),line=Int(p,"lineIndex"); var phase=EnumValue<SlicePhase>(p,"phase");
            if(index<0 || index>=c.Encounters.Count) throw Invalid("Unknown encounter cursor.");
            var encounter=c.Encounters[index]; int maxLine=phase==SlicePhase.Intro ? encounter.Intro.Count : phase==SlicePhase.Farewell ? encounter.Farewell.Count : 1;
            if(line<0 || line>=maxLine) throw Invalid("Invalid reading cursor.");
            var consumed=Strings(p,"consumedLines"); var completed=Strings(p,"completedOccurrences");
            if(consumed.Distinct().Count()!=consumed.Length || completed.Distinct().Count()!=completed.Length) throw Invalid("Duplicate progress receipt.");
            foreach(string receipt in consumed)
            {
                var parts=receipt.Split(':'); if(parts.Length!=5 || parts[0]!=run || !int.TryParse(parts[1],out int n) || n<0 || n>index || !c.Texts.ContainsKey(parts[4])) throw Invalid("Invalid consumed line receipt.");
                var e=c.Encounters[n]; string[] allowed=parts[2]=="Intro" ? e.Intro.ToArray() : parts[2]=="Farewell" ? e.Farewell.ToArray() :
                    parts[2]=="Request" ? new[]{c.Requests[e.RequestId].SummaryTextId} : parts[2]=="Reaction" ? e.Responses.Values.ToArray() : System.Array.Empty<string>();
                if(!int.TryParse(parts[3],out int cursor) || cursor<0 || !allowed.Contains(parts[4]) || (parts[2]!="Reaction" && (cursor>=allowed.Length || allowed[cursor]!=parts[4]))) throw Invalid("Unowned consumed text.");
            }
            int completedCount=phase==SlicePhase.Complete ? c.Encounters.Count : index;
            if(!completed.SequenceEqual(Enumerable.Range(0,completedCount).Select(x=>run+":"+x))) throw Invalid("Invalid completed occurrence ledger.");
            if(phase==SlicePhase.Complete && index!=c.Encounters.Count-1) throw Invalid("Premature completion.");
            var services=Array(p,"services").Select(x=>ReadService(Object(x),c)).ToArray();
            bool post=phase==SlicePhase.Reaction || phase==SlicePhase.Result || phase==SlicePhase.Discovery || phase==SlicePhase.Farewell || phase==SlicePhase.Departing || phase==SlicePhase.Complete;
            int expected=index+(post ? 1 : 0); if(services.Length!=expected || !services.Select(x=>x.OccurrenceId).SequenceEqual(Enumerable.Range(0,expected).Select(x=>run+":"+x))) throw Invalid("Invalid service occurrence ledger.");
            if(services.Select(x=>x.Snapshot.AttemptId).Distinct().Count()!=services.Length) throw Invalid("Duplicate service attempt.");
            if(phase==SlicePhase.Discovery && !services.Last().Discovered) throw Invalid("Discovery has no committed grant.");
            var known=Object(p["knownReceipts"]).Properties().ToDictionary(x=>x.Name,x=>String(Object(p["knownReceipts"]),x.Name),StringComparer.Ordinal);
            var factObject=Object(p["facts"]); var facts=factObject.Properties().ToDictionary(x=>x.Name,x=>Strings(factObject,x.Name),StringComparer.Ordinal);
            var expectedKnowledge=c.CreateKnowledge();
            foreach(var service in services)
            {
                bool gained=expectedKnowledge.Discover(service.Outcome,service.OccurrenceId); if(gained!=service.Discovered) throw Invalid("Invalid discovery receipt.");
            }
            if(known.Count!=expectedKnowledge.KnownReceipts.Count || known.Any(x=>!expectedKnowledge.KnownReceipts.TryGetValue(x.Key,out var receipt) || receipt!=x.Value)) throw Invalid("Unknown or unearned knowledge receipt.");
            if(facts.Count!=expectedKnowledge.Facts.Count || facts.Any(x=>!expectedKnowledge.Facts.TryGetValue(x.Key,out var expectedFacts) || !x.Value.OrderBy(v=>v,StringComparer.Ordinal).SequenceEqual(expectedFacts))) throw Invalid("Unknown or unearned fact.");
            return new SliceCheckpoint(Long(root,"sequence"),run,index,phase,line,consumed,completed,services,known,facts);
        }
        private static CommittedService ReadService(JObject record,SliceContent c)
        {
            Fields(record,"occurrenceId","responseKey","discovered","snapshot","outcome"); var s=Object(record["snapshot"]); var o=Object(record["outcome"]);
            Fields(s,"attemptId","tuningRevision","amounts","dashAmounts","hasIce","strained","stirHowGood","stirQualityGood","stirDegrees","stableFraction","stirIngredients","garnish","garnishPosition","trace");
            if(String(s,"tuningRevision")!=c.Tuning.Revision) throw Invalid("Snapshot tuning mismatch.");
            var amounts=Amounts(Object(s["amounts"]),c); if(amounts.Values.Sum()<=0 || amounts.Values.Sum()>c.Tuning.ServingMl) throw Invalid("Invalid served volume.");
            var stir=Strings(s,"stirIngredients"); if(stir.Any(x=>!c.Catalog.Ingredients.Contains(x)) || stir.Distinct().Count()!=stir.Length) throw Invalid("Invalid stirred ingredients.");
            var trace=Array(s,"trace").Select(x=> {
                var a=Object(x); Fields(a,"sequence","kind","source","target","acceptedMl","spilledMl","compositionRevision","components","elapsedMicroseconds");
                return new PreparationAction(Int(a,"sequence"),EnumValue<BrewAction>(a,"kind"),String(a,"source"),EnumValue<ContainerRole>(a,"target"),
                    Number(a,"acceptedMl"),Number(a,"spilledMl"),Int(a,"compositionRevision"),Amounts(Object(a["components"]),c),Long(a,"elapsedMicroseconds"));
            }).ToArray();
            var snapshot=new ServiceSnapshot(String(s,"attemptId"),String(s,"tuningRevision"),amounts,Amounts(Object(s["dashAmounts"]),c),Bool(s,"hasIce"),Bool(s,"strained"),Bool(s,"stirHowGood"),Bool(s,"stirQualityGood"),
                (double)Number(s,"stirDegrees"),(double)Number(s,"stableFraction"),stir,NullableString(s,"garnish"),NullableString(s,"garnishPosition"),trace);
            Fields(o,"recognition","recipeId","cocktailId","preparation","qualityGood","fits","reasons"); string recipe=NullableString(o,"recipeId");
            StirRecipe definition=recipe==null ? null : c.Catalog.Recipes[recipe]; if(NullableString(o,"cocktailId")!=definition?.CocktailId) throw Invalid("Outcome identity mismatch.");
            var reasons=Strings(o,"reasons"); string[] validReasons={"unrecognized_composition","ambiguous_composition","strain_missing","stir_batch_missing","dash_missing","required_garnish_missing","stir_quality_weak","request_mismatch"};
            if(reasons.Any(x=>!validReasons.Contains(x)) || reasons.Distinct().Count()!=reasons.Length) throw Invalid("Unknown outcome reason.");
            var outcome=new ServiceOutcome(EnumValue<DrinkRecognition>(o,"recognition"),definition,EnumValue<PreparationResult>(o,"preparation"),Bool(o,"qualityGood"),Bool(o,"fits"),reasons.ToList());
            bool good=outcome.Preparation==PreparationResult.Good && outcome.QualityGood;
            string expected=outcome.Recognition!=DrinkRecognition.Recognized ? "unknown" : outcome.Fits ? (good ? "good" : "weak") : (good ? "mismatch" : "bothWeak");
            if(String(record,"responseKey")!=expected) throw Invalid("Outcome response mismatch.");
            return new CommittedService(String(record,"occurrenceId"),snapshot,outcome,expected,Bool(record,"discovered"));
        }
    }
}
