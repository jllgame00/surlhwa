using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Surlhwa.Domain;
using static Surlhwa.ContentIO.StrictJson;
using static Surlhwa.ContentIO.CampaignJson;
namespace Surlhwa.ContentIO
{
    public static class CampaignSaveCodec
    {
        public static string Encode(CampaignCheckpoint state,CampaignContent content)
        {
            var p=new JObject {
                ["slotIndex"]=state.SlotIndex,["phase"]=state.Phase.ToString(),["lineIndex"]=state.LineIndex,["princeChoice"]=state.PrinceChoice,
                ["farewell"]=state.Farewell?.ToString(),["selectedEncounters"]=new JArray(state.SelectedEncounters),["consumedLines"]=new JArray(state.ConsumedLines),
                ["completedOccurrences"]=new JArray(state.CompletedOccurrences),["knownReceipts"]=Map(state.KnownReceipts),["facts"]=Map(state.Facts),
                ["services"]=new JArray(state.Services.Select(x=>Parse(ServiceEvidenceCodec.Write(x)))),
                ["memories"]=new JArray(state.Memories.Select(x=>new JObject {["id"]=x.Id,["variant"]=x.Variant,["occurrenceId"]=x.OccurrenceId,["sequence"]=x.Sequence})) };
            var envelope=new JObject { ["format"]="surlhwa-production-save",["schemaVersion"]=1,["mode"]="campaign",["act"]="act_01",
                ["sequence"]=state.Sequence,["runId"]=state.RunId,["contentRevision"]=content.Revision,["contentHash"]=content.BundleHash,
                ["tuningRevision"]=content.Tuning.Revision,["farewellPolicyRevision"]=PrinceFarewellPolicy.Revision,
                ["payload"]=p,["payloadSha256"]=Hash(p.ToString(Formatting.None)) };
            string json=envelope.ToString(Formatting.None); Decode(json,content); return json;
        }
        public static CampaignCheckpoint Decode(string json,CampaignContent content)
        {
            try { return Read(Parse(json),content); }
            catch(UnsupportedSaveVersionException) { throw; }
            catch(ContentValidationException) { throw; }
            catch(Exception e) when(e is JsonException || e is ArgumentException || e is InvalidOperationException || e is OverflowException || e is KeyNotFoundException || e is FormatException)
            { throw Invalid("Invalid campaign save: "+e.Message); }
        }
        private static CampaignCheckpoint Read(JObject root,CampaignContent c)
        {
            int schema=Int(root,"schemaVersion"); if(schema>1) throw new UnsupportedSaveVersionException("Campaign save uses a newer schema; it has not been overwritten.");
            if(schema!=1) throw Invalid("Unsupported campaign save schema.");
            Fields(root,"format","schemaVersion","mode","act","sequence","runId","contentRevision","contentHash","tuningRevision","farewellPolicyRevision","payload","payloadSha256");
            if(String(root,"format")!="surlhwa-production-save" || String(root,"mode")!="campaign" || String(root,"act")!="act_01") throw Invalid("Incompatible campaign namespace.");
            if(String(root,"contentRevision")!=c.Revision || String(root,"contentHash")!=c.BundleHash || String(root,"tuningRevision")!=c.Tuning.Revision ||
                String(root,"farewellPolicyRevision")!=PrinceFarewellPolicy.Revision) throw Invalid("Campaign definitions differ; explicit migration is required.");
            var p=Object(root["payload"]); if(Hash(p.ToString(Formatting.None))!=String(root,"payloadSha256")) throw Invalid("Campaign checksum mismatch.");
            Fields(p,"slotIndex","phase","lineIndex","princeChoice","farewell","selectedEncounters","consumedLines","completedOccurrences","knownReceipts","facts","services","memories");
            var state=new CampaignCheckpoint(Long(root,"sequence"),String(root,"runId"),Int(p,"slotIndex"),EnumValue<CampaignPhase>(p,"phase"),Int(p,"lineIndex"),NullableString(p,"princeChoice"),
                p["farewell"].Type==JTokenType.Null ? (PrinceFarewell?)null : EnumValue<PrinceFarewell>(p,"farewell"),Strings(p,"selectedEncounters"),Strings(p,"consumedLines"),Strings(p,"completedOccurrences"),
                Array(p,"services").Select(x=>ServiceEvidenceCodec.Read(x.ToString(Formatting.None),c.Catalog,c.Tuning)),
                Array(p,"memories").Select(x=> { var m=Object(x); Fields(m,"id","variant","occurrenceId","sequence"); return new MemoryReceipt(String(m,"id"),String(m,"variant"),String(m,"occurrenceId"),Long(m,"sequence")); }),
                Object(p["knownReceipts"]).Properties().ToDictionary(x=>x.Name,x=>String(Object(p["knownReceipts"]),x.Name)),
                Object(p["facts"]).Properties().ToDictionary(x=>x.Name,x=>Strings(Object(p["facts"]),x.Name)));
            Validate(state,c); return state;
        }
        private static void Validate(CampaignCheckpoint s,CampaignContent c)
        {
            if(s.SlotIndex>=c.Slots.Count || (s.PrinceChoice!=null && !c.PrinceChoices.ContainsKey(s.PrinceChoice))) throw Invalid("Unknown campaign cursor/choice.");
            for(int i=0;i<s.SelectedEncounters.Count;i++)
                if(s.SelectedEncounters[i]!=c.Slots[i].EncounterId && s.SelectedEncounters[i]!=c.Slots[i].FallbackId) throw Invalid("Unowned selected encounter.");
            var current=c.Encounters[s.SelectedEncounters[s.SlotIndex]];
            var intro=current.ChoiceIntros.TryGetValue(s.PrinceChoice ?? "none",out var variant) ? variant : current.Intro.Select(x=>x.TextId).ToArray();
            var farewell=current.PrinceVisit==PrinceVisit.Clarity && s.Farewell.HasValue ? new[]{c.PrinceRecalls[s.PrinceChoice ?? "none"]}.Concat(s.Farewell==PrinceFarewell.Open ? c.OpenFarewell : c.QuietFarewell).ToArray() : current.Farewell.ToArray();
            int lineCount=s.Phase==CampaignPhase.Intro ? intro.Length : s.Phase==CampaignPhase.Farewell ? farewell.Length : 1;
            if(s.LineIndex>=lineCount || (s.Phase==CampaignPhase.ChoiceResponse && s.PrinceChoice==null) ||
                (s.Phase==CampaignPhase.Choice && (current.PrinceVisit!=PrinceVisit.First || s.PrinceChoice!=null))) throw Invalid("Invalid campaign reading cursor.");
            int completed=s.Phase==CampaignPhase.Complete ? c.Slots.Count : s.SlotIndex;
            if(!s.CompletedOccurrences.SequenceEqual(Enumerable.Range(0,completed).Select(x=>s.RunId+":"+x)) ||
                (s.Phase==CampaignPhase.Complete && s.SlotIndex!=c.Slots.Count-1)) throw Invalid("Invalid encounter completion ledger.");
            int Index(string occurrence)
            {
                var parts=occurrence.Split(':'); if(parts.Length!=2 || parts[0]!=s.RunId || !int.TryParse(parts[1],out var n) || n<0 || n>s.SlotIndex || parts[1]!=n.ToString()) throw Invalid("Invalid occurrence identity."); return n;
            }
            var knowledge=c.CreateKnowledge(); int last=-1;
            foreach(var service in s.Services)
            {
                int n=Index(service.OccurrenceId); if(n<last) throw Invalid("Out-of-order service."); last=n;
                var encounter=c.Encounters[s.SelectedEncounters[n]];
                bool discovered=encounter.RequiredDiscovery==null ? knowledge.Discover(service.Outcome,service.OccurrenceId) : knowledge.DiscoverRequired(service.Outcome,service.OccurrenceId,encounter.RequiredDiscovery);
                if(discovered!=service.Discovered) throw Invalid("Unearned discovery receipt.");
            }
            for(int n=0;n<=s.SlotIndex;n++)
            {
                var e=c.Encounters[s.SelectedEncounters[n]]; var records=s.Services.Where(x=>x.OccurrenceId==s.RunId+":"+n).ToArray();
                if(e.RequiredDiscovery==null && records.Length>1) throw Invalid("Ordinary/Prince visit cannot rewrite service.");
                if(n<completed && (records.Length==0 || (e.RequiredDiscovery!=null && !knowledge.Project(e.RequiredDiscovery).Known))) throw Invalid("Departure lacks committed service/discovery.");
                var departureLines=e.PrinceVisit==PrinceVisit.Clarity && s.Farewell.HasValue ? new[]{c.PrinceRecalls[s.PrinceChoice ?? "none"]}.Concat(s.Farewell==PrinceFarewell.Open ? c.OpenFarewell : c.QuietFarewell).ToArray() : e.Farewell.ToArray();
                int requiredRead=n<completed || (n==s.SlotIndex && s.Phase==CampaignPhase.Departing) ? departureLines.Length :
                    n==s.SlotIndex && s.Phase==CampaignPhase.Farewell ? s.LineIndex : 0;
                for(int j=0;j<requiredRead;j++)
                    if(!s.ConsumedLines.Contains(s.RunId+":"+n+":Farewell:"+j+":"+departureLines[j])) throw Invalid("Departure lacks consumed farewell reading.");
            }
            bool post=s.Phase==CampaignPhase.Reaction || s.Phase==CampaignPhase.Result || s.Phase==CampaignPhase.Discovery || s.Phase==CampaignPhase.Retry || s.Phase==CampaignPhase.Farewell || s.Phase==CampaignPhase.Departing || s.Phase==CampaignPhase.Complete;
            if(post && s.CurrentService==null) throw Invalid("Reading lacks a committed service.");
            if(s.Phase==CampaignPhase.Discovery && !s.CurrentService.Discovered) throw Invalid("Discovery screen lacks a grant.");
            if(s.Phase==CampaignPhase.Retry && (current.RequiredDiscovery==null || knowledge.Project(current.RequiredDiscovery).Known)) throw Invalid("Invalid discovery retry.");
            foreach(var receipt in s.ConsumedLines)
            {
                var parts=receipt.Split(':'); if(parts.Length!=5) throw Invalid("Invalid consumed line.");
                int n=Index(parts[0]+":"+parts[1]); var e=c.Encounters[s.SelectedEncounters[n]];
                if(!int.TryParse(parts[3],out int cursor) || cursor<0 || !c.Texts.ContainsKey(parts[4])) throw Invalid("Unknown consumed line.");
                string[] allowed;
                if(parts[2]=="Intro")
                {
                    allowed=e.ChoiceIntros.TryGetValue(s.PrinceChoice ?? "none",out var branch) ? branch : e.Intro.Select(x=>x.TextId).ToArray();
                    if(cursor>=allowed.Length || allowed[cursor]!=parts[4]) throw Invalid("Unowned intro line.");
                    if(e.ChoiceIntros.Count==0) foreach(var fact in e.Intro[cursor].Facts) knowledge.GrantFact(fact.RecipeId,fact.Fact);
                }
                else
                {
                    allowed=parts[2]=="Request" ? new[]{e.SummaryTextId} : parts[2]=="Choice" && e.PrinceVisit==PrinceVisit.First ? new[]{c.ChoicePrompt} :
                        parts[2]=="ChoiceResponse" && s.PrinceChoice!=null && e.PrinceVisit==PrinceVisit.First ? new[]{c.PrinceResponses[s.PrinceChoice]} :
                        parts[2]=="Reaction" ? e.Responses.Values.ToArray() : parts[2]=="Farewell" ?
                        (e.PrinceVisit==PrinceVisit.Clarity && s.Farewell.HasValue ? new[]{c.PrinceRecalls[s.PrinceChoice ?? "none"]}.Concat(s.Farewell==PrinceFarewell.Open ? c.OpenFarewell : c.QuietFarewell).ToArray() : e.Farewell.ToArray()) : System.Array.Empty<string>();
                    if(!allowed.Contains(parts[4]) || (parts[2]!="Reaction" && (cursor>=allowed.Length || allowed[cursor]!=parts[4]))) throw Invalid("Unowned consumed reading.");
                }
            }
            if(s.KnownReceipts.Count!=knowledge.KnownReceipts.Count || s.KnownReceipts.Any(x=>!knowledge.KnownReceipts.TryGetValue(x.Key,out var value) || value!=x.Value) ||
                s.Facts.Count!=knowledge.Facts.Count || s.Facts.Any(x=>!knowledge.Facts.TryGetValue(x.Key,out var values) || !x.Value.OrderBy(v=>v).SequenceEqual(values.OrderBy(v=>v)))) throw Invalid("Unearned campaign knowledge.");
            var closing=s.SelectedEncounters.Select((id,n)=>new {Encounter=c.Encounters[id],Index=n}).SingleOrDefault(x=>x.Encounter.PrinceVisit==PrinceVisit.Clarity);
            bool closingServed=closing!=null && s.Services.Any(x=>x.OccurrenceId==s.RunId+":"+closing.Index);
            if(closingServed!=s.Farewell.HasValue) throw Invalid("Farewell selection lacks closing service.");
            var memoryOwners=s.SelectedEncounters.Take(completed).Select((id,n)=>new {Encounter=c.Encounters[id],Index=n})
                .Where(x=>x.Encounter.PrinceVisit==PrinceVisit.Clarity || x.Encounter.MemoryId!=null).ToArray();
            if(s.Memories.Count!=memoryOwners.Length) throw Invalid("Memory requires completed natural departure.");
            for(int i=0;i<memoryOwners.Length;i++)
            {
                var owner=memoryOwners[i]; bool prince=owner.Encounter.PrinceVisit==PrinceVisit.Clarity;
                string id=prince ? "mem_01" : owner.Encounter.MemoryId;
                string memoryVariant=prince ? (s.Farewell==PrinceFarewell.Open ? "star_glowing" : "star_unlit") : owner.Encounter.MemoryVariant;
                var m=s.Memories[i];
                if(m.Id!=id || m.OccurrenceId!=s.RunId+":"+owner.Index || m.Sequence>s.Sequence || m.Variant!=memoryVariant)
                    throw Invalid("Memory receipt contradicts completed farewell.");
            }
        }
    }
    public static class ServiceEvidenceCodec
    {
        public static string Write(CommittedService service)
        {
            var s=service.Snapshot; var o=service.Outcome;
            var root=new JObject { ["occurrenceId"]=service.OccurrenceId,["responseKey"]=service.ResponseKey,["discovered"]=service.Discovered,
                ["snapshot"]=new JObject { ["attemptId"]=s.AttemptId,["tuningRevision"]=s.TuningRevision,["amounts"]=Map(s.Amounts),["dashAmounts"]=Map(s.DashAmounts),
                    ["hasIce"]=s.HasIce,["strained"]=s.Strained,["stirHowGood"]=s.StirHowGood,["stirQualityGood"]=s.StirQualityGood,
                    ["stirDegrees"]=(decimal)s.StirDegrees,["stableFraction"]=(decimal)s.StableFraction,["stirIngredients"]=new JArray(s.StirIngredients),
                    ["garnish"]=s.Garnish,["garnishPosition"]=s.GarnishPosition,
                    ["shake"]=s.Shake==null ? null : new JObject { ["revision"]=s.Shake.TuningRevision,["reversals"]=s.Shake.Reversals,["stableReversals"]=s.Shake.StableReversals,
                        ["howGood"]=s.Shake.HowGood,["qualityGood"]=s.Shake.QualityGood,["amounts"]=Map(s.Shake.Amounts) },
                    ["floatRevision"]=s.FloatTuningRevision,["layers"]=new JArray(s.Layers.Select(x=>new JObject {["amounts"]=Map(x.Amounts),["lowerBoundaryGood"]=x.LowerBoundaryGood})),
                    ["trace"]=new JArray(s.Trace.Select(x=>new JObject {["sequence"]=x.Sequence,["kind"]=x.Kind.ToString(),["source"]=x.Source,["target"]=x.Target.ToString(),
                        ["acceptedMl"]=x.AcceptedMl,["spilledMl"]=x.SpilledMl,["compositionRevision"]=x.CompositionRevision,["components"]=Map(x.AcceptedByIngredient),["elapsedMicroseconds"]=x.ElapsedMicroseconds})) },
                ["outcome"]=new JObject {["recognition"]=o.Recognition.ToString(),["recipeId"]=o.RecipeId,["cocktailId"]=o.CocktailId,["preparation"]=o.Preparation.ToString(),["qualityGood"]=o.QualityGood,["fits"]=o.Fits,["reasons"]=new JArray(o.Reasons)} };
            return root.ToString(Formatting.None);
        }
        public static CommittedService Read(string json,BartendingCatalog catalog,BrewingTuning tuning)
        {
            var root=Parse(json); Fields(root,"occurrenceId","responseKey","discovered","snapshot","outcome"); var s=Object(root["snapshot"]); var o=Object(root["outcome"]);
            Fields(s,"attemptId","tuningRevision","amounts","dashAmounts","hasIce","strained","stirHowGood","stirQualityGood","stirDegrees","stableFraction","stirIngredients","garnish","garnishPosition","shake","floatRevision","layers","trace");
            var amounts=Amounts(Object(s["amounts"]),catalog);
            if(String(s,"tuningRevision")!=tuning.Revision || amounts.Values.Sum()<=0 || amounts.Values.Sum()>tuning.ServingMl) throw Invalid("Invalid service liquid/tuning.");
            ShakeEvidence shake=null;
            if(s["shake"].Type!=JTokenType.Null)
            {
                var q=Object(s["shake"]); Fields(q,"revision","reversals","stableReversals","howGood","qualityGood","amounts");
                if(String(q,"revision")!=tuning.Shake.Revision) throw Invalid("Unknown Shake evidence revision.");
                shake=new ShakeEvidence(String(q,"revision"),Int(q,"reversals"),Int(q,"stableReversals"),Bool(q,"howGood"),Bool(q,"qualityGood"),Amounts(Object(q["amounts"]),catalog));
            }
            var layers=Array(s,"layers").Select(x=> { var row=Object(x); Fields(row,"amounts","lowerBoundaryGood"); return new FloatLayerEvidence(Amounts(Object(row["amounts"]),catalog),Bool(row,"lowerBoundaryGood")); }).ToArray();
            if(layers.Length>0 && NullableString(s,"floatRevision")!=tuning.Float.Revision) throw Invalid("Unknown Float evidence revision.");
            var trace=Array(s,"trace").Select(x=> { var row=Object(x); Fields(row,"sequence","kind","source","target","acceptedMl","spilledMl","compositionRevision","components","elapsedMicroseconds");
                return new PreparationAction(Int(row,"sequence"),EnumValue<BrewAction>(row,"kind"),String(row,"source"),EnumValue<ContainerRole>(row,"target"),Number(row,"acceptedMl"),Number(row,"spilledMl"),Int(row,"compositionRevision"),Amounts(Object(row["components"]),catalog),Long(row,"elapsedMicroseconds")); }).ToArray();
            var snapshot=new ServiceSnapshot(String(s,"attemptId"),String(s,"tuningRevision"),amounts,Amounts(Object(s["dashAmounts"]),catalog),Bool(s,"hasIce"),Bool(s,"strained"),Bool(s,"stirHowGood"),Bool(s,"stirQualityGood"),
                (double)Number(s,"stirDegrees"),(double)Number(s,"stableFraction"),Strings(s,"stirIngredients"),NullableString(s,"garnish"),NullableString(s,"garnishPosition"),trace,shake,layers,NullableString(s,"floatRevision"));
            Fields(o,"recognition","recipeId","cocktailId","preparation","qualityGood","fits","reasons"); string recipeId=NullableString(o,"recipeId");
            var recipe=recipeId==null ? null : catalog.Recipes[recipeId]; if(NullableString(o,"cocktailId")!=recipe?.CocktailId) throw Invalid("Saved outcome identity differs.");
            var outcome=new ServiceOutcome(EnumValue<DrinkRecognition>(o,"recognition"),recipe,EnumValue<PreparationResult>(o,"preparation"),Bool(o,"qualityGood"),Bool(o,"fits"),Strings(o,"reasons").ToList());
            bool good=outcome.Preparation==PreparationResult.Good && outcome.QualityGood;
            string response=outcome.Recognition!=DrinkRecognition.Recognized ? "unknown" : outcome.Fits ? (good ? "good" : "weak") : (good ? "mismatch" : "bothWeak");
            if(String(root,"responseKey")!=response) throw Invalid("Saved reaction differs from original outcome.");
            return new CommittedService(String(root,"occurrenceId"),snapshot,outcome,response,Bool(root,"discovered"));
        }
    }
    internal static class CampaignJson
    {
        internal static JObject Map<T>(IEnumerable<KeyValuePair<string,T>> map)
        { var result=new JObject(); foreach(var x in map.OrderBy(x=>x.Key,StringComparer.Ordinal)) result.Add(x.Key,JToken.FromObject(x.Value)); return result; }
        internal static long Long(JObject obj,string key)
        { if(obj[key]?.Type!=JTokenType.Integer) throw Invalid("Integer required: "+key); return obj[key].Value<long>(); }
        internal static T EnumValue<T>(JObject obj,string key) where T:struct
        { string text=String(obj,key); if(!Enum.TryParse(text,false,out T value) || !Enum.IsDefined(typeof(T),value) || value.ToString()!=text) throw Invalid("Unknown "+key); return value; }
        internal static Dictionary<string,decimal> Amounts(JObject obj,BartendingCatalog catalog)
        {
            var result=new Dictionary<string,decimal>(); foreach(var p in obj.Properties())
            { decimal value=Number(obj,p.Name); if(!catalog.Ingredients.Contains(p.Name) || value<0 || decimal.Round(value,6)!=value) throw Invalid("Unknown ingredient or invalid ml."); result.Add(p.Name,value); }
            return result;
        }
    }
}
