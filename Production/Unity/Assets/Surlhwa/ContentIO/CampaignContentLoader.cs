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
        private static readonly string[] StrainedShakePreparation={"shaker_ice","closed_shake","strain_from_shaker","late_measured_pour","gentle_stir","no_serving_ice"};
        public static CampaignContent Load(string json)
        {
            try { return Read(json); }
            catch(ContentValidationException) { throw; }
            catch(Exception e) when(e is JsonException || e is ArgumentException || e is OverflowException || e is KeyNotFoundException || e is FormatException || e is InvalidOperationException)
            { throw Invalid("Invalid campaign content: "+e.Message); }
        }
        private static CampaignContent Read(string json)
        {
            var root=Parse(json);
            int schema=Int(root,"schemaVersion"); bool expanded=schema==2;
            string scope=String(root,"act");
            if(schema!=1 && schema!=2) throw Invalid("Unsupported campaign content schema.");
            int act=scope=="act_01" ? 1 : scope=="through_act_02" ? 2 : scope=="through_act_03" ? 3 : scope=="through_act_04" ? 4 : scope=="through_act_05" ? 5 : 0;
            var rootFields=new[]{"schemaVersion","mode","act","contentRevision","sourceRegistrySha256","ingredients","cocktails","recipes","tuning","guests","texts","encounters","slots","supply","known","prince"};
            Fields(root,expanded ? rootFields.Concat(new[]{"supplyGrants"}).ToArray() : rootFields);
            if(String(root,"mode")!="campaign" || act==0 || (expanded ? act<2 : act!=1) || !IsHash(String(root,"sourceRegistrySha256"))) throw Invalid("Unsupported campaign scope/source manifest.");
            var names=new Dictionary<string,string>();
            foreach(var token in Array(root,"ingredients")) { var row=Object(token); Fields(row,"id","label"); names.Add(Id<IngredientIdentity>(row,"id"),String(row,"label")); }
            var cocktails=new List<CocktailDefinition>();
            foreach(var token in Array(root,"cocktails")) { var row=Object(token); Fields(row,"id","name","tags"); cocktails.Add(new CocktailDefinition(Id<CocktailIdentity>(row,"id"),String(row,"name"),Strings(row,"tags"))); }
            var recipes=new List<StirRecipe>(); var presentation=new Dictionary<string,RecipePresentation>();
            foreach(var token in Array(root,"recipes"))
            {
                var row=Object(token); Fields(row,"id","cocktailId","method","amounts","dashIngredient","requiredGarnish","glassware","optionalGarnish","preparation","garnishPosition","lateIngredient","layerOrder");
                string method=String(row,"method"); var preparation=Strings(row,"preparation");
                bool strainedShake=act>=4 && method=="shake" && preparation.SequenceEqual(StrainedShakePreparation);
                if(!Preparation.TryGetValue(method,out var expected) || (!preparation.SequenceEqual(expected) && !strainedShake)) throw Invalid("Unsupported method/preparation.");
                var amounts=new Dictionary<string,decimal>();
                foreach(var item in Array(row,"amounts")) { var a=Object(item); Fields(a,"ingredientId","ml"); amounts.Add(Id<IngredientIdentity>(a,"ingredientId"),Number(a,"ml")); }
                string id=Id<RecipeIdentity>(row,"id"),glass=String(row,"glassware");
                if(!new[]{"old_fashioned","cocktail","tumbler"}.Contains(glass) && !(act>=4 && glass=="flute")) throw Invalid("Unsupported serving glass.");
                var layers=Strings(row,"layerOrder"); if(method!="float" && layers.Length!=0) throw Invalid("Unexpected layer policy.");
                recipes.Add(new StirRecipe(id,Id<CocktailIdentity>(row,"cocktailId"),amounts,NullableString(row,"dashIngredient"),NullableString(row,"requiredGarnish"),
                    method,String(row,"garnishPosition"),NullableString(row,"lateIngredient"),layers,
                    strainedShake ? ShakeFinishPolicy.StrainThenPourAndGentleStir : ShakeFinishPolicy.PourThenSplash));
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
                var specialVisuals=act>=5 ? new[]{"visual_prince","visual_chunhyang","visual_mongryong","visual_quixote","visual_hamlet","visual_faust"} : act==4 ? new[]{"visual_prince","visual_chunhyang","visual_mongryong","visual_quixote","visual_hamlet"} : act==3 ? new[]{"visual_prince","visual_chunhyang","visual_mongryong","visual_quixote"} : expanded ? new[]{"visual_prince","visual_chunhyang","visual_mongryong"} : new[]{"visual_prince"};
                if((kind!="ordinary" && kind!="special") || !texts.ContainsKey(label) || texts[label].Role!="display" || (kind=="special" ? !specialVisuals.Contains(visual) : visual!="visual_ordinary")) throw Invalid("Unsupported campaign guest presentation.");
                if(act>=3 && id=="guest_quixote" && (kind!="special" || visual!="visual_quixote")) throw Invalid("Invalid Quixote presentation identity.");
                if(act>=4 && id=="guest_hamlet" && (kind!="special" || visual!="visual_hamlet")) throw Invalid("Invalid Hamlet presentation identity.");
                if(act>=5 && id=="guest_faust" && (kind!="special" || visual!="visual_faust" || label!="text_rc1_0060")) throw Invalid("Invalid Faust presentation identity.");
                guests.Add(id,new GuestDefinition(new StableId<GuestIdentity>(id),kind=="ordinary" ? GuestKind.Ordinary : GuestKind.Special,new StableId<TextIdentity>(label),new StableId<VisualIdentity>(visual)));
            }
            var encounters=new List<CampaignEncounter>(); var sourceRows=new Dictionary<string,string>();
            foreach(var token in Array(root,"encounters"))
            {
                var row=Object(token); var encounterFields=new[]{"id","sourceRow","night","guestId","request","summaryTextId","requiredDiscovery","princeVisit","intro","farewell","responses","choiceIntros"};
                var permittedFields=expanded ? encounterFields.Concat(new[]{"companionGuestId","memoryId","memoryVariant"}).ToArray() : encounterFields;
                if(row.Property("discoveryRetryTextId")!=null) permittedFields=permittedFields.Concat(new[]{"discoveryRetryTextId"}).ToArray();
                Fields(row,permittedFields);
                string id=Id<TextIdentity>(row,"id"),sourceRow=String(row,"sourceRow");
                if(!Enumerable.Range(1,act>=5 ? 37 : act==4 ? 33 : act==3 ? 29 : expanded ? 25 : 17).Select(x=>"RC1-"+x.ToString("00")).Contains(sourceRow)) throw Invalid("Later ACT content is outside this milestone.");
                sourceRows.Add(id,sourceRow); var request=Object(row["request"]); Fields(request,"cocktailId","anyTags","allTags","excludedTags");
                string cocktail=NullableString(request,"cocktailId");
                if(cocktail!=null && (!catalog.Cocktails.ContainsKey(cocktail) || Strings(request,"anyTags").Length+Strings(request,"allTags").Length+Strings(request,"excludedTags").Length!=0)) throw Invalid("Invalid Specific request.");
                var drinkRequest=cocktail!=null ? DrinkRequest.Specific(cocktail) : DrinkRequest.Recommendation(Strings(request,"anyTags"),Strings(request,"allTags"),Strings(request,"excludedTags"));
                string required=NullableString(row,"requiredDiscovery");
                if(required!=null && (!catalog.Recipes.ContainsKey(required) || cocktail!=catalog.Recipes[required].CocktailId)) throw Invalid("Required discovery must name its exact recipe identity.");
                string retry=row.Property("discoveryRetryTextId")==null ? null : NullableString(row,"discoveryRetryTextId");
                if(retry!=null && (required==null || !texts.TryGetValue(retry,out var retryText) || retryText.SourceField!="discoveryRetryFeedback" || retryText.Role!="spoken"))
                    throw Invalid("Invalid discovery retry source.");
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
                    responses.Properties().ToDictionary(x=>x.Name,x=>String(responses,x.Name)),required,visit,variants,
                    expanded ? NullableString(row,"companionGuestId") : null,expanded ? NullableString(row,"memoryId") : null,expanded ? NullableString(row,"memoryVariant") : null,retry));
            }
            var slots=new List<CampaignSlot>();
            foreach(var token in Array(root,"slots")) { var row=Object(token); Fields(row,"encounterId","fallbackId","whenKnownRecipe"); slots.Add(new CampaignSlot(Id<TextIdentity>(row,"encounterId"),NullableString(row,"fallbackId"),NullableString(row,"whenKnownRecipe"))); }
            int expectedSlots=act>=5 ? 37 : act==4 ? 33 : act==3 ? 29 : expanded ? 25 : 17;
            if(slots.Count!=expectedSlots || slots.Select(x=>sourceRows[x.EncounterId]).Distinct().Count()!=expectedSlots) throw Invalid("Missing campaign source-row disposition.");
            var ordered=slots.Select(x=>encounters.Single(e=>e.Id==x.EncounterId)).ToArray();
            if(!ordered.Where(x=>x.PrinceVisit.HasValue).Select(x=>x.PrinceVisit.Value).SequenceEqual(new[]{PrinceVisit.First,PrinceVisit.Warmth,PrinceVisit.Clarity}) || ordered[16].PrinceVisit!=PrinceVisit.Clarity ||
                ordered.Zip(ordered.Skip(1),(a,b)=>b.Night<a.Night).Any(x=>x)) throw Invalid("Invalid ACT 1 narrative order.");
            if(expanded)
            {
                if(!ordered.Skip(17).Select(x=>sourceRows[x.Id]).SequenceEqual(Enumerable.Range(18,expectedSlots-17).Select(x=>"RC1-"+x))) throw Invalid("Invalid expanded campaign event order.");
                foreach(var e in encounters)
                {
                    bool paired=sourceRows[e.Id]=="RC1-21" || sourceRows[e.Id]=="RC1-25";
                    if(paired ? !e.GuestIds.OrderBy(x=>x).SequenceEqual(new[]{"guest_chunhyang","guest_mongryong"}) : e.GuestIds.Count!=1) throw Invalid("Invalid ACT 2 actor ownership.");
                    bool reunion=sourceRows[e.Id]=="RC1-25";
                    bool quixoteDeparture=act>=3 && sourceRows[e.Id]=="RC1-28";
                    bool hamletDeparture=act>=4 && sourceRows[e.Id]=="RC1-33";
                    bool faustDeparture=act>=5 && sourceRows[e.Id]=="RC1-37";
                    if(reunion ? e.MemoryId!="mem_02" || e.MemoryVariant!="swing_empty_place" :
                        quixoteDeparture ? e.MemoryId!="mem_03" || e.MemoryVariant!="windmill_fragment" :
                        hamletDeparture ? e.MemoryId!="mem_04" || e.MemoryVariant!="stage_skull_shadow" :
                        faustDeparture ? e.MemoryId!="mem_05" || e.MemoryVariant!="faint_circle_broken_glass_red_light" : e.MemoryId!=null) throw Invalid("Invalid authored memory owner.");
                    if(act>=3 && StringComparer.Ordinal.Compare(sourceRows[e.Id],"RC1-26")>=0 && StringComparer.Ordinal.Compare(sourceRows[e.Id],"RC1-29")<=0) {
                        string expectedGuest=sourceRows[e.Id]=="RC1-26" ? "guest_postman" : sourceRows[e.Id]=="RC1-29" ? "guest_snow_worker" : "guest_quixote";
                        if(e.GuestId!=expectedGuest || e.RequiredDiscovery!=null || e.Request.CocktailId!=null || e.Intro.Any(line=>line.Facts.Count!=0))
                            throw Invalid("Invalid ACT 3 encounter ownership.");
                    }
                    if(act>=4 && StringComparer.Ordinal.Compare(sourceRows[e.Id],"RC1-30")>=0 && StringComparer.Ordinal.Compare(sourceRows[e.Id],"RC1-33")<=0) {
                        string expectedGuest=sourceRows[e.Id]=="RC1-30" ? "guest_station_worker" : sourceRows[e.Id]=="RC1-32" ? "guest_repairer" : "guest_hamlet";
                        if(e.GuestId!=expectedGuest || e.Night!=(sourceRows[e.Id]=="RC1-30" || sourceRows[e.Id]=="RC1-31" ? 12 : 13)) throw Invalid("Invalid ACT 4 encounter ownership.");
                        if(e.Id=="enc_rc1_30") ValidateFrenchLesson(e,texts);
                        else if(e.RequiredDiscovery!=null || e.DiscoveryRetryTextId!=null || e.Request.CocktailId!=null || e.Intro.Any(line=>line.Facts.Count!=0))
                            throw Invalid("Invalid ACT 4 ordinary/recommendation ownership.");
                    }
                    if(act>=5 && StringComparer.Ordinal.Compare(sourceRows[e.Id],"RC1-34")>=0) ValidateActFiveEncounter(e,sourceRows[e.Id]);
                }
                if(act>=5 && slots.Skip(33).Any(x=>x.FallbackId!=null || x.WhenKnownRecipe!=null)) throw Invalid("ACT 5 recommendations cannot be conditional.");
            }
            foreach(var slot in slots.Where(x=>x.FallbackId!=null))
            {
                var main=encounters.Single(x=>x.Id==slot.EncounterId); var alternate=encounters.Single(x=>x.Id==slot.FallbackId);
                if(sourceRows[main.Id]!=sourceRows[alternate.Id] || main.Night!=alternate.Night || main.PrinceVisit.HasValue || alternate.PrinceVisit.HasValue) throw Invalid("Invalid ordinary fallback ownership.");
            }
            var supply=Strings(root,"supply"); if(supply.Distinct().Count()!=supply.Length || recipes.Any(r=>r.Amounts.Keys.Any(x=>!supply.Contains(x)))) throw Invalid("ACT 1 recipe is not physically reachable.");
            var supplyFromSlot=new Dictionary<string,int>();
            if(expanded) foreach(var token in Array(root,"supplyGrants")) {
                var grant=Object(token); Fields(grant,"beforeEncounter","ingredientIds");
                int slot=slots.FindIndex(x=>x.EncounterId==String(grant,"beforeEncounter"));
                if(slot<1) throw Invalid("Unresolved supply grant boundary.");
                foreach(var ingredient in Strings(grant,"ingredientIds")) supplyFromSlot.Add(ingredient,slot);
            }
            if(expanded && supplyFromSlot.Keys.Any(x=>x!="london_dry_gin" && x!="fernet" && !(act>=4 && (x=="fresh_lemon_juice" || x=="champagne"))))
                throw Invalid("ACT 2 supply grants cannot delay accepted ACT 1 supply.");
            if(act>=4 && (!supplyFromSlot.TryGetValue("fresh_lemon_juice",out var lemonSlot) || lemonSlot!=29 ||
                !supplyFromSlot.TryGetValue("champagne",out var champagneSlot) || champagneSlot!=29)) throw Invalid("ACT 4 supply must begin with French 75 discovery.");
            var known=new Dictionary<string,string>(); foreach(var token in Array(root,"known")) { var row=Object(token); Fields(row,"recipeId","provenance"); known.Add(Id<RecipeIdentity>(row,"recipeId"),String(row,"provenance")); }
            if(known.Count!=1 || !known.TryGetValue("recipe_test_stir",out var provenance) || provenance!="LEARN-SEOLYA") throw Invalid("ACT 1 must not inherit Slice fixture knowledge.");
            var prince=Object(root["prince"]); Fields(prince,"prompt","choices","responses","recalls","openFarewell","quietFarewell","policyRevision");
            if(String(prince,"policyRevision")!=PrinceFarewellPolicy.Revision) throw Invalid("Unapproved farewell policy.");
            Dictionary<string,string> TextMap(string key,params string[] fields)
            { var map=Object(prince[key]); Fields(map,fields); return map.Properties().ToDictionary(x=>x.Name,x=>String(map,x.Name)); }
            return new CampaignContent(String(root,"contentRevision"),Hash(json),catalog,tuning,names,texts,guests,presentation,encounters,slots,supply,known,
                Id<TextIdentity>(prince,"prompt"),TextMap("choices","keep","seek"),TextMap("responses","keep","seek"),TextMap("recalls","keep","seek","none"),Strings(prince,"openFarewell"),Strings(prince,"quietFarewell"),scope,supplyFromSlot,
                act>=5 ? new[]{0,17,25,29,33} : act==4 ? new[]{0,17,25,29} : act==3 ? new[]{0,17,25} : expanded ? new[]{0,17} : new[]{0});
        }
        private static void ValidateActFiveEncounter(CampaignEncounter e,string sourceRow)
        {
            int index=int.Parse(sourceRow.Substring(4))-34;
            var guests=new[]{"guest_faust","guest_station_worker","guest_postman","guest_faust"};
            var intros=new[]{new[]{66,67,68,69},new[]{204,205},new[]{212,213},new[]{77,78,79,80}};
            var farewells=new[]{new[]{61,62,63},new[]{200,201},new[]{208,209},new[]{72,73,74}};
            var summaries=new[]{70,206,214,81}; var good=new[]{64,202,210,75}; var other=new[]{65,203,211,76};
            var tags=new[]{new[]{"clarity","awakening"},new[]{"awakening","clarity"},new[]{"warmth"},new[]{"bitterness","warmth"}};
            string TextId(int value) => "text_rc1_"+value.ToString("0000");
            if(e.Id!="enc_rc1_"+(index+34) || e.GuestId!=guests[index] || e.Night!=(index<2 ? 14 : 15) ||
                e.RequiredDiscovery!=null || e.DiscoveryRetryTextId!=null || e.PrinceVisit.HasValue || e.ChoiceIntros.Count!=0 ||
                e.Intro.Any(line=>line.Facts.Count!=0) || e.Request.CocktailId!=null || !e.Request.AnyTags.SequenceEqual(tags[index]) ||
                e.Request.AllTags.Count!=0 || e.Request.ExcludedTags.Count!=0) throw Invalid("Invalid ACT 5 encounter ownership.");
            if(e.SummaryTextId!=TextId(summaries[index]) || !e.Intro.Select(x=>x.TextId).SequenceEqual(intros[index].Select(TextId)) ||
                !e.Farewell.SequenceEqual(farewells[index].Select(TextId)) ||
                e.Responses.Any(x=>x.Value!=TextId(x.Key=="good" ? good[index] : other[index]))) throw Invalid("Invalid ACT 5 authored reading.");
        }
        private static void ValidateFrenchLesson(CampaignEncounter e,IDictionary<string,SourcedText> texts)
        {
            if(e.RequiredDiscovery!="recipe_french_75" || e.DiscoveryRetryTextId!="text_rc1_0039" ||
                !e.Intro.Select(x=>x.TextId).SequenceEqual(new[]{"text_rc1_0035","text_rc1_0036","text_rc1_0037"})) throw Invalid("Invalid French 75 discovery reading.");
            var expected=new[]{new[]{"identity"},new[]{"method","amount:gin","amount:fresh_lemon_juice","amount:simple_syrup"},new[]{"amount:champagne","preparation","garnish"}};
            for(int i=0;i<expected.Length;i++)
                if(!e.Intro[i].Facts.Select(x=>x.Fact).SequenceEqual(expected[i]) || e.Intro[i].Facts.Any(x=>x.RecipeId!="recipe_french_75" ||
                    !x.SourceIds.SequenceEqual(new[]{texts[e.Intro[i].TextId].SourceId}))) throw Invalid("Invalid French 75 fact provenance.");
            if(texts[e.DiscoveryRetryTextId].SourceId!="SRC-RC1-0039") throw Invalid("Invalid French 75 retry provenance.");
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
