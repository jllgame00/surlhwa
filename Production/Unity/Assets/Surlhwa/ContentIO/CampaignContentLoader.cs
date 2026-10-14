using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Surlhwa.Domain;
using static Surlhwa.ContentIO.StrictJson;
namespace Surlhwa.ContentIO
{
    public static class CampaignContentLoader
    {
        private static readonly Dictionary<string,string[]> Preparation=new Dictionary<string,string[]> {
            ["stir"]=new[]{"mixing_ice","stir_after_ingredients","strain_to_serving"},
            ["build"]=new[]{"direct_serving","serving_ice","gentle_stir","orange_slice_rim"},
            ["shake"]=new[]{"shaker_ice","closed_shake","pour_from_shaker","late_soda_splash","no_serving_ice"},
            ["float"]=new[]{"vodka_base","coffee_layer","cream_layer","no_ice"} };
        public static CampaignContent Load(string json)
        {
            try { return Read(json); }
            catch(ContentValidationException) { throw; }
            catch(Exception e) when(e is JsonException || e is ArgumentException || e is OverflowException || e is KeyNotFoundException || e is FormatException || e is InvalidOperationException)
            { throw Invalid("Invalid ACT 1 content: "+e.Message); }
        }
        private static CampaignContent Read(string json)
        {
            var root=Parse(json);
            Fields(root,"schemaVersion","mode","act","contentRevision","sourceRegistrySha256","ingredients","cocktails","recipes","tuning","guests","texts","encounters","slots","supply","known","prince"); Schema(root);
            if(String(root,"mode")!="campaign" || String(root,"act")!="act_01" || !IsHash(String(root,"sourceRegistrySha256"))) throw Invalid("Unsupported campaign scope/source manifest.");
            var names=new Dictionary<string,string>();
            foreach(var token in Array(root,"ingredients")) { var row=Object(token); Fields(row,"id","label"); names.Add(Id<IngredientIdentity>(row,"id"),String(row,"label")); }
            var cocktails=new List<CocktailDefinition>();
            foreach(var token in Array(root,"cocktails")) { var row=Object(token); Fields(row,"id","name","tags"); cocktails.Add(new CocktailDefinition(Id<CocktailIdentity>(row,"id"),String(row,"name"),Strings(row,"tags"))); }
            var recipes=new List<StirRecipe>(); var presentation=new Dictionary<string,RecipePresentation>();
            foreach(var token in Array(root,"recipes"))
            {
                var row=Object(token); Fields(row,"id","cocktailId","method","amounts","dashIngredient","requiredGarnish","glassware","optionalGarnish","preparation","garnishPosition","lateIngredient","layerOrder");
                string method=String(row,"method"); if(!Preparation.TryGetValue(method,out var expected) || !Strings(row,"preparation").SequenceEqual(expected)) throw Invalid("Unsupported method/preparation.");
                var amounts=new Dictionary<string,decimal>();
                foreach(var item in Array(row,"amounts")) { var a=Object(item); Fields(a,"ingredientId","ml"); amounts.Add(Id<IngredientIdentity>(a,"ingredientId"),Number(a,"ml")); }
                string id=Id<RecipeIdentity>(row,"id"),glass=String(row,"glassware");
                if(!new[]{"old_fashioned","cocktail","tumbler"}.Contains(glass)) throw Invalid("Unsupported serving glass.");
                var layers=Strings(row,"layerOrder"); if(method!="float" && layers.Length!=0) throw Invalid("Unexpected layer policy.");
                recipes.Add(new StirRecipe(id,Id<CocktailIdentity>(row,"cocktailId"),amounts,NullableString(row,"dashIngredient"),NullableString(row,"requiredGarnish"),
                    method,String(row,"garnishPosition"),NullableString(row,"lateIngredient"),layers));
                presentation.Add(id,new RecipePresentation(glass,Bool(row,"optionalGarnish")));
            }
            var catalog=new BartendingCatalog(names.Keys,cocktails,recipes); var tuning=ReadTuning(Object(root["tuning"]));
            if(recipes.Any(x=>x.Amounts.Values.Sum()>tuning.ServingMl)) throw Invalid("Recipe exceeds serving capacity.");
            var texts=new Dictionary<string,SourcedText>();
            foreach(var token in Array(root,"texts"))
            {
                var row=Object(token); Fields(row,"id","value","sourceText","sourceId","sourceHash","sourcePath","sourceField","sourceOrder","role","adoption");
                string id=Id<TextIdentity>(row,"id"),original=String(row,"sourceText"),hash=String(row,"sourceHash"),role=String(row,"role");
                if(Hash(original)!=hash || !new[]{"spoken","display","narration","mixed","request_summary"}.Contains(role)) throw Invalid("Invalid source text/hash/role.");
                texts.Add(id,new SourcedText(id,String(row,"value"),String(row,"sourceId"),hash,String(row,"sourcePath"),String(row,"sourceField"),String(row,"sourceOrder"),role,String(row,"adoption")));
            }
            var sourceIds=new HashSet<string>(texts.Values.Select(x=>x.SourceId));
            var guests=new Dictionary<string,GuestDefinition>();
            foreach(var token in Array(root,"guests"))
            {
                var row=Object(token); Fields(row,"id","kind","displayTextId","visualBindingId"); string id=Id<GuestIdentity>(row,"id"),kind=String(row,"kind"),label=Id<TextIdentity>(row,"displayTextId"),visual=Id<VisualIdentity>(row,"visualBindingId");
                if((kind!="ordinary" && kind!="special") || !texts.ContainsKey(label) || texts[label].Role!="display" || (kind=="special" ? visual!="visual_prince" : visual!="visual_ordinary")) throw Invalid("Unsupported ACT 1 guest presentation.");
                guests.Add(id,new GuestDefinition(new StableId<GuestIdentity>(id),kind=="ordinary" ? GuestKind.Ordinary : GuestKind.Special,new StableId<TextIdentity>(label),new StableId<VisualIdentity>(visual)));
            }
            var encounters=new List<CampaignEncounter>(); var sourceRows=new Dictionary<string,string>();
            foreach(var token in Array(root,"encounters"))
            {
                var row=Object(token); Fields(row,"id","sourceRow","night","guestId","request","summaryTextId","requiredDiscovery","princeVisit","intro","farewell","responses","choiceIntros");
                string id=Id<TextIdentity>(row,"id"),sourceRow=String(row,"sourceRow");
                if(!Enumerable.Range(1,17).Select(x=>"RC1-"+x.ToString("00")).Contains(sourceRow)) throw Invalid("Later ACT content is outside this milestone.");
                sourceRows.Add(id,sourceRow); var request=Object(row["request"]); Fields(request,"cocktailId","anyTags","allTags","excludedTags");
                string cocktail=NullableString(request,"cocktailId");
                if(cocktail!=null && (!catalog.Cocktails.ContainsKey(cocktail) || Strings(request,"anyTags").Length+Strings(request,"allTags").Length+Strings(request,"excludedTags").Length!=0)) throw Invalid("Invalid Specific request.");
                var drinkRequest=cocktail!=null ? DrinkRequest.Specific(cocktail) : DrinkRequest.Recommendation(Strings(request,"anyTags"),Strings(request,"allTags"),Strings(request,"excludedTags"));
                string required=NullableString(row,"requiredDiscovery");
                if(required!=null && (!catalog.Recipes.ContainsKey(required) || cocktail!=catalog.Recipes[required].CocktailId)) throw Invalid("Required discovery must name its exact recipe identity.");
                var intro=new List<CampaignLine>();
                foreach(var lineToken in Array(row,"intro"))
                {
                    var line=Object(lineToken); Fields(line,"textId","facts"); var grants=new List<FactGrant>();
                    foreach(var factToken in Array(line,"facts"))
                    {
                        var f=Object(factToken); Fields(f,"recipeId","fact","sourceIds"); var sources=Strings(f,"sourceIds");
                        if(sources.Length==0 || sources.Any(x=>!sourceIds.Contains(x))) throw Invalid("Unresolved fact provenance.");
                        grants.Add(new FactGrant(Id<RecipeIdentity>(f,"recipeId"),String(f,"fact"),sources));
                    }
                    intro.Add(new CampaignLine(Id<TextIdentity>(line,"textId"),grants));
                }
                var responses=Object(row["responses"]); Fields(responses,"good","weak","mismatch","bothWeak","unknown");
                var choiceIntros=Object(row["choiceIntros"]); var variants=choiceIntros.Properties().ToDictionary(x=>x.Name,x=>Strings(choiceIntros,x.Name));
                if(variants.Keys.Any(x=>x!="keep" && x!="seek" && x!="none")) throw Invalid("Unknown choice recall branch.");
                PrinceVisit? visit=row["princeVisit"].Type==JTokenType.Null ? (PrinceVisit?)null : CampaignJson.EnumValue<PrinceVisit>(row,"princeVisit");
                encounters.Add(new CampaignEncounter(id,Id<GuestIdentity>(row,"guestId"),Int(row,"night"),Id<TextIdentity>(row,"summaryTextId"),drinkRequest,intro,Strings(row,"farewell"),
                    responses.Properties().ToDictionary(x=>x.Name,x=>String(responses,x.Name)),required,visit,variants));
            }
            var slots=new List<CampaignSlot>();
            foreach(var token in Array(root,"slots")) { var row=Object(token); Fields(row,"encounterId","fallbackId","whenKnownRecipe"); slots.Add(new CampaignSlot(Id<TextIdentity>(row,"encounterId"),NullableString(row,"fallbackId"),NullableString(row,"whenKnownRecipe"))); }
            if(slots.Count!=17 || slots.Select(x=>sourceRows[x.EncounterId]).Distinct().Count()!=17) throw Invalid("Missing ACT 1 source-row disposition.");
            var ordered=slots.Select(x=>encounters.Single(e=>e.Id==x.EncounterId)).ToArray();
            if(!ordered.Where(x=>x.PrinceVisit.HasValue).Select(x=>x.PrinceVisit.Value).SequenceEqual(new[]{PrinceVisit.First,PrinceVisit.Warmth,PrinceVisit.Clarity}) || ordered.Last().PrinceVisit!=PrinceVisit.Clarity ||
                ordered.Zip(ordered.Skip(1),(a,b)=>b.Night<a.Night).Any(x=>x)) throw Invalid("Invalid ACT 1 narrative order.");
            foreach(var slot in slots.Where(x=>x.FallbackId!=null))
            {
                var main=encounters.Single(x=>x.Id==slot.EncounterId); var alternate=encounters.Single(x=>x.Id==slot.FallbackId);
                if(sourceRows[main.Id]!=sourceRows[alternate.Id] || main.Night!=alternate.Night || main.PrinceVisit.HasValue || alternate.PrinceVisit.HasValue) throw Invalid("Invalid ordinary fallback ownership.");
            }
            var supply=Strings(root,"supply"); if(supply.Distinct().Count()!=supply.Length || recipes.Any(r=>r.Amounts.Keys.Any(x=>!supply.Contains(x)))) throw Invalid("ACT 1 recipe is not physically reachable.");
            var known=new Dictionary<string,string>(); foreach(var token in Array(root,"known")) { var row=Object(token); Fields(row,"recipeId","provenance"); known.Add(Id<RecipeIdentity>(row,"recipeId"),String(row,"provenance")); }
            if(known.Count!=1 || !known.TryGetValue("recipe_test_stir",out var provenance) || provenance!="LEARN-SEOLYA") throw Invalid("ACT 1 must not inherit Slice fixture knowledge.");
            var prince=Object(root["prince"]); Fields(prince,"prompt","choices","responses","recalls","openFarewell","quietFarewell","policyRevision");
            if(String(prince,"policyRevision")!=PrinceFarewellPolicy.Revision) throw Invalid("Unapproved farewell policy.");
            Dictionary<string,string> TextMap(string key,params string[] fields)
            { var map=Object(prince[key]); Fields(map,fields); return map.Properties().ToDictionary(x=>x.Name,x=>String(map,x.Name)); }
            return new CampaignContent(String(root,"contentRevision"),Hash(json),catalog,tuning,names,texts,guests,presentation,encounters,slots,supply,known,
                Id<TextIdentity>(prince,"prompt"),TextMap("choices","keep","seek"),TextMap("responses","keep","seek"),TextMap("recalls","keep","seek","none"),Strings(prince,"openFarewell"),Strings(prince,"quietFarewell"));
        }
        private static BrewingTuning ReadTuning(JObject t)
        {
            Fields(t,"id","liquidUnit","precision","capacity","flow","technique","recognition","shake","float");
            if(String(t,"liquidUnit")!="ml" || Int(t,"precision")!=6) throw Invalid("Unsupported liquid precision.");
            var c=Object(t["capacity"]); Fields(c,"jigger","mixing","serving"); var f=Object(t["flow"]); Fields(f,"pour","finePour","transfer","fineTransfer","dash");
            var q=Object(t["technique"]); Fields(q,"howDegrees","qualityDegrees","minSpeed","maxSpeed","stableFraction"); var r=Object(t["recognition"]); Fields(r,"relative","ingredientMl","extraMl");
            var shake=Object(t["shake"]); Fields(shake,"revision","howReversals","qualityReversals","endpoint","minInterval","maxInterval","stableFraction");
            var floating=Object(t["float"]); Fields(floating,"revision","maxStableRate","maxHeight","maxHeightSpeed");
            return new BrewingTuning(Id<TextIdentity>(t,"id"),Number(c,"jigger"),Number(c,"mixing"),Number(c,"serving"),Number(f,"pour"),Number(f,"finePour"),Number(f,"transfer"),Number(f,"fineTransfer"),Number(f,"dash"),
                (double)Number(q,"howDegrees"),(double)Number(q,"qualityDegrees"),(double)Number(q,"minSpeed"),(double)Number(q,"maxSpeed"),(double)Number(q,"stableFraction"),Number(r,"relative"),Number(r,"ingredientMl"),Number(r,"extraMl"),
                new ShakeTuning(String(shake,"revision"),Int(shake,"howReversals"),Int(shake,"qualityReversals"),(double)Number(shake,"endpoint"),CampaignJson.Long(shake,"minInterval"),CampaignJson.Long(shake,"maxInterval"),(double)Number(shake,"stableFraction")),
                new FloatTuning(String(floating,"revision"),Number(floating,"maxStableRate"),(double)Number(floating,"maxHeight"),(double)Number(floating,"maxHeightSpeed")));
        }
        private static string Id<T>(JObject row,string field) => new StableId<T>(String(row,field)).Value;
        private static bool IsHash(string value) => value.Length==64 && value.All(x=>x>='0' && x<='9' || x>='a' && x<='f');
    }
}
