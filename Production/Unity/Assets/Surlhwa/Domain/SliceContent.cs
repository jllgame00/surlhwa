using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Surlhwa.Domain
{
    public sealed class SourcedText
    {
        public string Id { get; } public string Value { get; } public string SourceId { get; }
        public string SourceHash { get; } public string SourcePath { get; } public string SourceField { get; }
        public string SourceOrder { get; } public string Role { get; } public string Adoption { get; }
        public SourcedText(string id,string value,string sourceId,string hash,string path,string field,string order,string role,string adoption)
        { Id=id; Value=value; SourceId=sourceId; SourceHash=hash; SourcePath=path; SourceField=field; SourceOrder=order; Role=role; Adoption=adoption; }
    }
    public sealed class SliceRequest
    {
        public string Id { get; } public string Tag { get; } public string SummaryTextId { get; }
        public SliceRequest(string id,string tag,string summary) { Id=id; Tag=tag; SummaryTextId=summary; }
    }
    public sealed class SliceEncounter
    {
        public string Id { get; } public string GuestId { get; } public string RequestId { get; }
        public IReadOnlyList<string> Intro { get; } public IReadOnlyList<string> Farewell { get; }
        public IReadOnlyDictionary<string,string> Responses { get; }
        public IReadOnlyCollection<string> MemoryGrants { get; }
        public SliceEncounter(string id,string guest,string request,IEnumerable<string> intro,IEnumerable<string> farewell,IDictionary<string,string> responses)
        {
            Id=id; GuestId=guest; RequestId=request; Intro=Array.AsReadOnly(intro.ToArray()); Farewell=Array.AsReadOnly(farewell.ToArray());
            Responses=new ReadOnlyDictionary<string,string>(new Dictionary<string,string>(responses,StringComparer.Ordinal)); MemoryGrants=Array.AsReadOnly(Array.Empty<string>());
        }
    }
    public sealed class FactGrant
    {
        public string RecipeId { get; } public string Fact { get; } public IReadOnlyCollection<string> SourceIds { get; }
        public FactGrant(string recipe,string fact,IEnumerable<string> sources) { RecipeId=recipe; Fact=fact; SourceIds=Array.AsReadOnly(sources.ToArray()); }
    }
    public sealed class SliceStartProfile
    {
        public string Id { get; } public string FixtureContext { get; }
        public IReadOnlyCollection<string> Supply { get; }
        public IReadOnlyDictionary<string,string> KnownSeeds { get; }
        public IReadOnlyList<FactGrant> Facts { get; }
        public bool CompletesAct => false;
        public SliceStartProfile(string id,string context,IEnumerable<string> supply,IDictionary<string,string> known,IEnumerable<FactGrant> facts)
        { Id=id; FixtureContext=context; Supply=Array.AsReadOnly(supply.ToArray()); KnownSeeds=new ReadOnlyDictionary<string,string>(new Dictionary<string,string>(known,StringComparer.Ordinal)); Facts=Array.AsReadOnly(facts.ToArray()); }
    }
    public sealed class RecipePresentation
    {
        public string Glassware { get; } public bool OptionalGarnish { get; }
        public RecipePresentation(string glassware,bool optional) { Glassware=glassware; OptionalGarnish=optional; }
    }
    public sealed class SliceContent
    {
        public string Revision { get; } public string BundleHash { get; }
        public BartendingCatalog Catalog { get; } public BrewingTuning Tuning { get; } public SliceStartProfile Profile { get; }
        public IReadOnlyDictionary<string,string> IngredientNames { get; }
        public IReadOnlyDictionary<string,SourcedText> Texts { get; }
        public IReadOnlyDictionary<string,GuestDefinition> Guests { get; }
        public IReadOnlyDictionary<string,SliceRequest> Requests { get; }
        public IReadOnlyDictionary<string,RecipePresentation> RecipePresentation { get; }
        public IReadOnlyList<SliceEncounter> Encounters { get; }
        public SliceContent(string revision,string bundleHash,BartendingCatalog catalog,BrewingTuning tuning,SliceStartProfile profile,
            IDictionary<string,string> names,IDictionary<string,SourcedText> texts,IDictionary<string,GuestDefinition> guests,
            IDictionary<string,SliceRequest> requests,IDictionary<string,RecipePresentation> presentation,IEnumerable<SliceEncounter> encounters)
        {
            Revision=revision; BundleHash=bundleHash; Catalog=catalog; Tuning=tuning; Profile=profile;
            IngredientNames=Copy(names); Texts=Copy(texts); Guests=Copy(guests); Requests=Copy(requests); RecipePresentation=Copy(presentation);
            Encounters=Array.AsReadOnly(encounters.ToArray());
        }
        private static IReadOnlyDictionary<string,T> Copy<T>(IDictionary<string,T> value) => new ReadOnlyDictionary<string,T>(new Dictionary<string,T>(value,StringComparer.Ordinal));
        public RecipeKnowledge CreateKnowledge()
        {
            var knowledge=new RecipeKnowledge(Catalog);
            foreach(var seed in Profile.KnownSeeds) knowledge.SeedKnown(seed.Key,seed.Value);
            foreach(var fact in Profile.Facts) knowledge.GrantFact(fact.RecipeId,fact.Fact);
            return knowledge;
        }
    }
}
