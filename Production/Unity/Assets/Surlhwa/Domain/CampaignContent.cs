using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Surlhwa.Domain
{
    public sealed class CampaignLine
    {
        public string TextId { get; } public IReadOnlyList<FactGrant> Facts { get; }
        public CampaignLine(string text,IEnumerable<FactGrant> facts=null)
        { _=new StableId<TextIdentity>(text); TextId=text; Facts=Array.AsReadOnly((facts ?? Array.Empty<FactGrant>()).ToArray()); }
    }
    public sealed class CampaignEncounter
    {
        public string Id { get; } public string GuestId { get; } public string SummaryTextId { get; }
        public IReadOnlyList<string> GuestIds { get; }
        public string MemoryId { get; } public string MemoryVariant { get; }
        public DrinkRequest Request { get; } public string RequiredDiscovery { get; }
        public PrinceVisit? PrinceVisit { get; } public int Night { get; }
        public IReadOnlyList<CampaignLine> Intro { get; } public IReadOnlyList<string> Farewell { get; }
        public IReadOnlyDictionary<string,string> Responses { get; }
        public IReadOnlyDictionary<string,string[]> ChoiceIntros { get; }
        public CampaignEncounter(string id,string guest,int night,string summary,DrinkRequest request,IEnumerable<CampaignLine> intro,
            IEnumerable<string> farewell,IDictionary<string,string> responses,string requiredDiscovery=null,PrinceVisit? princeVisit=null,
            IDictionary<string,string[]> choiceIntros=null,string companionGuestId=null,string memoryId=null,string memoryVariant=null)
        {
            _=new StableId<TextIdentity>(id); _=new StableId<GuestIdentity>(guest); _=new StableId<TextIdentity>(summary);
            if(night<1 || (princeVisit.HasValue && !Enum.IsDefined(typeof(PrinceVisit),princeVisit.Value))) throw new ArgumentException("Invalid encounter metadata.");
            Id=id; GuestId=guest; Night=night; SummaryTextId=summary; Request=request ?? throw new ArgumentNullException(nameof(request));
            if(companionGuestId!=null) {
                _=new StableId<GuestIdentity>(companionGuestId);
                if(companionGuestId==guest || princeVisit.HasValue) throw new ArgumentException("Invalid paired encounter actors.");
            }
            GuestIds=Array.AsReadOnly(companionGuestId==null ? new[]{guest} : new[]{guest,companionGuestId});
            if((memoryId==null)!=(memoryVariant==null) || (memoryId!=null && princeVisit.HasValue)) throw new ArgumentException("Invalid authored memory grant.");
            if(memoryId!=null) { _=new StableId<TextIdentity>(memoryId); _=new StableId<TextIdentity>(memoryVariant); }
            MemoryId=memoryId; MemoryVariant=memoryVariant;
            RequiredDiscovery=requiredDiscovery; PrinceVisit=princeVisit;
            Intro=Array.AsReadOnly(intro.ToArray()); Farewell=Array.AsReadOnly(farewell.ToArray());
            if(Intro.Count==0 || Farewell.Count==0) throw new ArgumentException("Encounter requires reading and departure.");
            Responses=new ReadOnlyDictionary<string,string>(new Dictionary<string,string>(responses));
            foreach(var key in new[]{"good","weak","mismatch","bothWeak","unknown"}) if(!Responses.ContainsKey(key)) throw new ArgumentException("Missing reaction.");
            ChoiceIntros=new ReadOnlyDictionary<string,string[]>((choiceIntros ?? new Dictionary<string,string[]>()).ToDictionary(x=>x.Key,x=>x.Value.ToArray()));
        }
    }
    public sealed class CampaignSlot
    {
        public string EncounterId { get; } public string FallbackId { get; } public string WhenKnownRecipe { get; }
        public CampaignSlot(string encounter,string fallback=null,string whenKnown=null)
        { EncounterId=encounter; FallbackId=fallback; WhenKnownRecipe=whenKnown; if((fallback==null)!=(whenKnown==null)) throw new ArgumentException("Incomplete fallback."); }
    }
    public sealed class CampaignContent
    {
        public string Revision { get; } public string BundleHash { get; }
        public string Scope { get; }
        private readonly IReadOnlyDictionary<string,int> supplyFromSlot;
        public IReadOnlyCollection<string> AvailableSupplyAt(int slot) => Array.AsReadOnly(Supply.Where(x=>!supplyFromSlot.TryGetValue(x,out int start) || slot>=start).ToArray());
        public BartendingCatalog Catalog { get; } public BrewingTuning Tuning { get; }
        public IReadOnlyDictionary<string,string> IngredientNames { get; }
        public IReadOnlyDictionary<string,SourcedText> Texts { get; }
        public IReadOnlyDictionary<string,GuestDefinition> Guests { get; }
        public IReadOnlyDictionary<string,RecipePresentation> RecipePresentation { get; }
        public IReadOnlyDictionary<string,CampaignEncounter> Encounters { get; }
        public IReadOnlyList<CampaignSlot> Slots { get; } public IReadOnlyCollection<string> Supply { get; }
        public IReadOnlyDictionary<string,string> KnownSeeds { get; }
        public IReadOnlyDictionary<string,string> PrinceChoices { get; }
        public IReadOnlyDictionary<string,string> PrinceResponses { get; }
        public IReadOnlyDictionary<string,string> PrinceRecalls { get; }
        public IReadOnlyList<string> OpenFarewell { get; } public IReadOnlyList<string> QuietFarewell { get; }
        public string ChoicePrompt { get; }
        public CampaignContent(string revision,string hash,BartendingCatalog catalog,BrewingTuning tuning,
            IDictionary<string,string> names,IDictionary<string,SourcedText> texts,IDictionary<string,GuestDefinition> guests,
            IDictionary<string,RecipePresentation> presentation,IEnumerable<CampaignEncounter> encounters,IEnumerable<CampaignSlot> slots,
            IEnumerable<string> supply,IDictionary<string,string> known,string choicePrompt,IDictionary<string,string> choices,
            IDictionary<string,string> responses,IDictionary<string,string> recalls,IEnumerable<string> open,IEnumerable<string> quiet,
            string scope="act_01",IDictionary<string,int> supplyFromSlot=null)
        {
            Revision=revision; BundleHash=hash; Catalog=catalog; Tuning=tuning; IngredientNames=Copy(names); Texts=Copy(texts); Guests=Copy(guests);
            RecipePresentation=Copy(presentation); Encounters=Copy(encounters.ToDictionary(x=>x.Id)); Slots=Array.AsReadOnly(slots.ToArray());
            Supply=Array.AsReadOnly(supply.ToArray()); KnownSeeds=Copy(known); ChoicePrompt=choicePrompt;
            _=new StableId<TextIdentity>(scope); Scope=scope;
            this.supplyFromSlot=Copy(supplyFromSlot ?? new Dictionary<string,int>());
            if(this.supplyFromSlot.Any(x=>!Supply.Contains(x.Key) || x.Value<1 || x.Value>=Slots.Count)) throw new ArgumentException("Invalid supply grant boundary.");
            PrinceChoices=Copy(choices); PrinceResponses=Copy(responses); PrinceRecalls=Copy(recalls);
            OpenFarewell=Array.AsReadOnly(open.ToArray()); QuietFarewell=Array.AsReadOnly(quiet.ToArray());
            if(Slots.Count==0 || Supply.Any(x=>!Catalog.Ingredients.Contains(x)) || KnownSeeds.Keys.Any(x=>!Catalog.Recipes.ContainsKey(x))) throw new ArgumentException("Invalid campaign profile.");
            foreach(var slot in Slots) if(!Encounters.ContainsKey(slot.EncounterId) || (slot.FallbackId!=null &&
                (!Encounters.ContainsKey(slot.FallbackId) || !Catalog.Recipes.ContainsKey(slot.WhenKnownRecipe)))) throw new ArgumentException("Invalid campaign selection.");
            foreach(var e in Encounters.Values)
            {
                if(e.GuestIds.Any(x=>!Guests.ContainsKey(x)) || !Texts.ContainsKey(e.SummaryTextId) || e.Intro.Any(x=>!Texts.ContainsKey(x.TextId)) ||
                    e.Farewell.Concat(e.Responses.Values).Concat(e.ChoiceIntros.Values.SelectMany(x=>x)).Any(x=>!Texts.ContainsKey(x))) throw new ArgumentException("Unresolved campaign text/guest.");
                if(e.RequiredDiscovery!=null && (!Catalog.Recipes.ContainsKey(e.RequiredDiscovery) || e.PrinceVisit.HasValue)) throw new ArgumentException("Invalid discovery encounter.");
                foreach(var line in e.Intro) foreach(var f in line.Facts) new RecipeKnowledge(Catalog).GrantFact(f.RecipeId,f.Fact);
            }
            var memoryOwners=Encounters.Values.Select(x=>x.PrinceVisit==PrinceVisit.Clarity ? "mem_01" : x.MemoryId).Where(x=>x!=null).ToArray();
            if(memoryOwners.Distinct(StringComparer.Ordinal).Count()!=memoryOwners.Length) throw new ArgumentException("Memory requires a unique encounter owner.");
            var scheduledMemories=new HashSet<string>(StringComparer.Ordinal);
            for(int slotIndex=0;slotIndex<Slots.Count;slotIndex++)
            {
                var slot=Slots[slotIndex]; var available=AvailableSupplyAt(slotIndex);
                foreach(var id in new[]{slot.EncounterId,slot.FallbackId}.Where(x=>x!=null).Distinct(StringComparer.Ordinal))
                {
                    var e=Encounters[id]; string memory=e.PrinceVisit==PrinceVisit.Clarity ? "mem_01" : e.MemoryId;
                    if(memory!=null && !scheduledMemories.Add(memory)) throw new ArgumentException("Memory owner cannot recur in the campaign schedule.");
                    if(e.RequiredDiscovery!=null && Catalog.Recipes[e.RequiredDiscovery].Amounts.Keys.Any(x=>!available.Contains(x)))
                        throw new ArgumentException("Required discovery precedes physical supply.");
                }
            }
            if(!Texts.ContainsKey(ChoicePrompt) || PrinceChoices.Count!=2 || PrinceChoices.Keys.Any(x=>!PrinceResponses.ContainsKey(x) || !PrinceRecalls.ContainsKey(x)) ||
                !PrinceRecalls.ContainsKey("none") || PrinceChoices.Values.Concat(PrinceResponses.Values).Concat(PrinceRecalls.Values).Concat(OpenFarewell).Concat(QuietFarewell).Any(x=>!Texts.ContainsKey(x))) throw new ArgumentException("Invalid Prince narrative branches.");
        }
        private static IReadOnlyDictionary<string,T> Copy<T>(IDictionary<string,T> map) => new ReadOnlyDictionary<string,T>(new Dictionary<string,T>(map,StringComparer.Ordinal));
        public RecipeKnowledge CreateKnowledge()
        { var k=new RecipeKnowledge(Catalog); foreach(var seed in KnownSeeds) k.SeedKnown(seed.Key,seed.Value); return k; }
    }
}
