using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Surlhwa.Domain;
using static Surlhwa.ContentIO.StrictJson;

namespace Surlhwa.ContentIO
{
    public static class SliceContentLoader
    {
        private static readonly Dictionary<string,string[]> Files=new Dictionary<string,string[]> {
            ["Ingredients/liquids.json"]=new[]{"ingredients"},["Recipes/stir.json"]=new[]{"cocktails","recipes"},
            ["Profiles/stir-tuning.json"]=new[]{"tuning"},["Dialogue/slice.json"]=new[]{"texts"},["Guests/slice.json"]=new[]{"guests"},
            ["Requests/warmth.json"]=new[]{"requests"},["Encounters/slice.json"]=new[]{"encounters"},["Profiles/stir-slice.json"]=new[]{"contentRevision","profile"} };
        public static string BundleFromDirectory(string contentDirectory)
        {
            var root=new JObject { ["schemaVersion"]=1,["validatorVersion"]="slice_loader_v1",["mode"]="slice" };
            var sources=new JArray();
            foreach(var entry in Files.OrderBy(x=>x.Key,StringComparer.Ordinal))
            {
                var path=Path.Combine(contentDirectory,entry.Key); var text=File.ReadAllText(path); var part=Parse(text);
                Fields(part,new[]{"schemaVersion"}.Concat(entry.Value).ToArray()); Schema(part);
                foreach(var field in entry.Value) root[field]=part[field].DeepClone();
                sources.Add(new JObject { ["path"]=entry.Key,["sha256"]=Hash(File.ReadAllBytes(path)) });
            }
            root["sourceFiles"]=sources; string result=root.ToString(Formatting.Indented); Load(result); return result;
        }
        public static SliceContent Load(string json)
        {
            try { return Read(json); }
            catch(ContentValidationException) { throw; }
            catch(Exception e) when(e is JsonException || e is ArgumentException || e is OverflowException || e is KeyNotFoundException || e is FormatException)
            { throw Invalid("Invalid Slice content: "+e.Message); }
        }
        private static SliceContent Read(string json)
        {
            var root=Parse(json);
            Fields(root,"schemaVersion","validatorVersion","mode","contentRevision","sourceFiles","ingredients","cocktails","recipes","tuning","texts","guests","requests","encounters","profile"); Schema(root);
            if(String(root,"mode")!="slice" || String(root,"validatorVersion")!="slice_loader_v1") throw Invalid("Unsupported Slice bundle.");
            var sourcePaths=new HashSet<string>(StringComparer.Ordinal);
            foreach(var token in Array(root,"sourceFiles"))
            { var row=Object(token); Fields(row,"path","sha256"); var path=String(row,"path"); if(!Files.ContainsKey(path)||!sourcePaths.Add(path)||!IsHash(String(row,"sha256"))) throw Invalid("Invalid source manifest."); }
            if(sourcePaths.Count!=Files.Count) throw Invalid("Incomplete source manifest.");
            var names=new Dictionary<string,string>(StringComparer.Ordinal);
            foreach(var token in Array(root,"ingredients")) { var row=Object(token); Fields(row,"id","label"); names.Add(Id<IngredientIdentity>(row,"id"),String(row,"label")); }
            var cocktails=new List<CocktailDefinition>();
            foreach(var token in Array(root,"cocktails")) { var row=Object(token); Fields(row,"id","name","tags"); cocktails.Add(new CocktailDefinition(Id<CocktailIdentity>(row,"id"),String(row,"name"),Strings(row,"tags"))); }
            var recipes=new List<StirRecipe>(); var presentation=new Dictionary<string,RecipePresentation>(StringComparer.Ordinal);
            foreach(var token in Array(root,"recipes"))
            {
                var row=Object(token); Fields(row,"id","cocktailId","method","amounts","dashIngredient","requiredGarnish","glassware","optionalGarnish","preparation");
                if(String(row,"method")!="stir") throw Invalid("Unsupported method: "+String(row,"method"));
                if(!Strings(row,"preparation").SequenceEqual(new[]{"mixing_ice","stir_after_ingredients","strain_to_serving"})) throw Invalid("Unsupported preparation constraint set.");
                var amounts=new Dictionary<string,decimal>(StringComparer.Ordinal);
                foreach(var amountToken in Array(row,"amounts")) { var amount=Object(amountToken); Fields(amount,"ingredientId","ml"); amounts.Add(Id<IngredientIdentity>(amount,"ingredientId"),Number(amount,"ml")); }
                var id=Id<RecipeIdentity>(row,"id"); var glass=String(row,"glassware"); if(glass!="old_fashioned" && glass!="cocktail") throw Invalid("Unsupported glass profile.");
                recipes.Add(new StirRecipe(id,Id<CocktailIdentity>(row,"cocktailId"),amounts,NullableString(row,"dashIngredient"),NullableString(row,"requiredGarnish")));
                presentation.Add(id,new RecipePresentation(glass,Bool(row,"optionalGarnish")));
            }
            var catalog=new BartendingCatalog(names.Keys,cocktails,recipes); var tuning=ReadTuning(Object(root["tuning"]));
            if(recipes.Any(x=>x.Amounts.Values.Sum()>tuning.ServingMl)) throw Invalid("Recipe exceeds serving capacity.");
            var texts=new Dictionary<string,SourcedText>(StringComparer.Ordinal);
            foreach(var token in Array(root,"texts"))
            {
                var row=Object(token); Fields(row,"id","value","sourceId","sourceHash","sourcePath","sourceField","sourceOrder","role","adoption");
                var id=Id<TextIdentity>(row,"id"); var text=String(row,"value"); var hash=String(row,"sourceHash"); var role=String(row,"role");
                if(Hash(text)!=hash || !new[]{"spoken","display","narration","mixed","request_summary"}.Contains(role)) throw Invalid("Invalid source text/hash/role: "+id);
                texts.Add(id,new SourcedText(id,text,String(row,"sourceId"),hash,String(row,"sourcePath"),String(row,"sourceField"),String(row,"sourceOrder"),role,String(row,"adoption")));
            }
            var guests=new Dictionary<string,GuestDefinition>(StringComparer.Ordinal);
            foreach(var token in Array(root,"guests"))
            {
                var row=Object(token); Fields(row,"id","kind","displayTextId","visualBindingId"); var id=Id<GuestIdentity>(row,"id");
                string kind=String(row,"kind"),label=Id<TextIdentity>(row,"displayTextId"); if(!texts.ContainsKey(label) || texts[label].Role!="display") throw Invalid("Missing guest display text.");
                if(kind!="ordinary" && kind!="special") throw Invalid("Unsupported guest kind.");
                guests.Add(id,new GuestDefinition(new StableId<GuestIdentity>(id),kind=="ordinary" ? GuestKind.Ordinary : GuestKind.Special,new StableId<TextIdentity>(label),new StableId<VisualIdentity>(String(row,"visualBindingId"))));
            }
            var requests=new Dictionary<string,SliceRequest>(StringComparer.Ordinal);
            foreach(var token in Array(root,"requests"))
            {
                var row=Object(token); Fields(row,"id","kind","tag","summaryTextId","discoveryPolicy");
                if(String(row,"kind")!="recommendation" || String(row,"discoveryPolicy")!="normal_recognition") throw Invalid("Unsupported Slice request policy.");
                var summary=Id<TextIdentity>(row,"summaryTextId"); if(!texts.ContainsKey(summary)) throw Invalid("Missing request text.");
                var id=Id<TextIdentity>(row,"id"); requests.Add(id,new SliceRequest(id,Id<TextIdentity>(row,"tag"),summary));
            }
            var encounters=new Dictionary<string,SliceEncounter>(StringComparer.Ordinal);
            foreach(var token in Array(root,"encounters"))
            {
                var row=Object(token); Fields(row,"id","guestId","requestId","intro","farewell","responses","memoryGrants","completesAct","disposition");
                if(Bool(row,"completesAct") || Array(row,"memoryGrants").Count!=0 || String(row,"disposition")!="slice_excerpt") throw Invalid("This excerpt cannot grant ACT completion or memory.");
                var id=Id<TextIdentity>(row,"id"); var guest=Id<GuestIdentity>(row,"guestId"); var request=Id<TextIdentity>(row,"requestId");
                if(!guests.ContainsKey(guest)||!requests.ContainsKey(request)) throw Invalid("Missing encounter reference.");
                var intro=Strings(row,"intro"); var farewell=Strings(row,"farewell"); var responses=Object(row["responses"]); Fields(responses,"good","weak","mismatch","bothWeak","unknown");
                var responseMap=responses.Properties().ToDictionary(x=>x.Name,x=>String(responses,x.Name),StringComparer.Ordinal);
                if(intro.Length==0 || farewell.Length==0 || intro.Concat(farewell).Concat(responseMap.Values).Any(x=>!texts.ContainsKey(x))) throw Invalid("Missing encounter line.");
                encounters.Add(id,new SliceEncounter(id,guest,request,intro,farewell,responseMap));
            }
            var p=Object(root["profile"]); Fields(p,"id","fixtureContext","supply","known","facts","encounters","completesAct","initialMemories","priorChoices","priorServices");
            if(Bool(p,"completesAct") || Array(p,"initialMemories").Count!=0 || Array(p,"priorChoices").Count!=0 || Array(p,"priorServices").Count!=0) throw Invalid("Invented past progress is forbidden.");
            var supply=Strings(p,"supply"); if(supply.Distinct(StringComparer.Ordinal).Count()!=supply.Length || supply.Any(x=>!names.ContainsKey(x))) throw Invalid("Invalid supply.");
            var known=new Dictionary<string,string>(StringComparer.Ordinal);
            foreach(var token in Array(p,"known")) { var row=Object(token); Fields(row,"recipeId","provenance"); var id=Id<RecipeIdentity>(row,"recipeId"); if(!catalog.Recipes.ContainsKey(id)) throw Invalid("Unknown seed recipe."); known.Add(id,String(row,"provenance")); }
            var sourceIds=new HashSet<string>(texts.Values.Select(x=>x.SourceId),StringComparer.Ordinal); var grants=new List<FactGrant>();
            foreach(var token in Array(p,"facts"))
            {
                var row=Object(token); Fields(row,"recipeId","fact","sourceIds"); var sources=Strings(row,"sourceIds");
                if(sources.Length==0 || sources.Any(x=>!sourceIds.Contains(x))) throw Invalid("Missing fact provenance.");
                grants.Add(new FactGrant(Id<RecipeIdentity>(row,"recipeId"),String(row,"fact"),sources));
            }
            var ordered=Strings(p,"encounters"); if(ordered.Length==0 || ordered.Distinct(StringComparer.Ordinal).Count()!=ordered.Length || ordered.Any(x=>!encounters.ContainsKey(x))) throw Invalid("Invalid encounter plan.");
            if(recipes.Any(r=>r.Amounts.Keys.Any(x=>!supply.Contains(x)))) throw Invalid("Slice recipe is not physically reachable.");
            foreach(var request in requests.Values)
                if(recipes.Count(r=>known.ContainsKey(r.Id) && catalog.Cocktails[r.CocktailId].Tags.Contains(request.Tag))<2) throw Invalid("Recommendation lacks two reachable Known alternatives.");
            var profile=new SliceStartProfile(Id<TextIdentity>(p,"id"),String(p,"fixtureContext"),supply,known,grants);
            var content=new SliceContent(String(root,"contentRevision"),Hash(json),catalog,tuning,profile,names,texts,guests,requests,presentation,ordered.Select(x=>encounters[x]));
            content.CreateKnowledge(); return content;
        }
        private static BrewingTuning ReadTuning(JObject t)
        {
            Fields(t,"id","liquidUnit","precision","capacity","flow","technique","recognition");
            if(String(t,"liquidUnit")!="ml" || Int(t,"precision")!=6) throw Invalid("Unsupported liquid unit/precision.");
            var c=Object(t["capacity"]); Fields(c,"jigger","mixing","serving"); var f=Object(t["flow"]); Fields(f,"pour","finePour","transfer","fineTransfer","dash");
            var q=Object(t["technique"]); Fields(q,"howDegrees","qualityDegrees","minSpeed","maxSpeed","stableFraction"); var r=Object(t["recognition"]); Fields(r,"relative","ingredientMl","extraMl");
            return new BrewingTuning(Id<TextIdentity>(t,"id"),Number(c,"jigger"),Number(c,"mixing"),Number(c,"serving"),Number(f,"pour"),Number(f,"finePour"),Number(f,"transfer"),Number(f,"fineTransfer"),Number(f,"dash"),
                (double)Number(q,"howDegrees"),(double)Number(q,"qualityDegrees"),(double)Number(q,"minSpeed"),(double)Number(q,"maxSpeed"),(double)Number(q,"stableFraction"),Number(r,"relative"),Number(r,"ingredientMl"),Number(r,"extraMl"));
        }
        private static string Id<T>(JObject row,string field) => new StableId<T>(String(row,field)).Value;
        private static bool IsHash(string value) => value.Length==64 && value.All(x=>(x>='0'&&x<='9')||(x>='a'&&x<='f'));
    }
}
