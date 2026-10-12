using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Surlhwa.Domain
{
    public sealed class KnowledgeProjection
    {
        public bool Known { get; } public string Name { get; } public string Method { get; }
        public IReadOnlyCollection<string> Ingredients { get; } public IReadOnlyDictionary<string,decimal> Amounts { get; }
        public bool PreparationVisible { get; } public bool GarnishVisible { get; }
        internal KnowledgeProjection(bool known,string name,string method,IEnumerable<string> ingredients,IDictionary<string,decimal> amounts,bool preparation,bool garnish)
        { Known=known; Name=name; Method=method; Ingredients=Array.AsReadOnly(ingredients.ToArray()); Amounts=LiquidMath.Copy(amounts); PreparationVisible=preparation; GarnishVisible=garnish; }
    }
    public sealed class RecipeKnowledge
    {
        private readonly BartendingCatalog catalog;
        private readonly Dictionary<string,HashSet<string>> facts=new Dictionary<string,HashSet<string>>(StringComparer.Ordinal);
        private readonly Dictionary<string,string> known=new Dictionary<string,string>(StringComparer.Ordinal);
        public RecipeKnowledge(BartendingCatalog catalog) { this.catalog=catalog; }
        public void GrantFact(string recipeId,string fact)
        {
            var recipe=catalog.Recipes[recipeId];
            if(fact==null) throw new ArgumentNullException(nameof(fact));
            if(fact!="identity" && fact!="method" && fact!="preparation" && fact!="garnish" &&
                !(fact.StartsWith("ingredient:",StringComparison.Ordinal) && recipe.Amounts.ContainsKey(fact.Substring(11))) &&
                !(fact.StartsWith("amount:",StringComparison.Ordinal) && recipe.Amounts.ContainsKey(fact.Substring(7)))) throw new ArgumentException("Unknown recipe fact.");
            if(!facts.TryGetValue(recipeId,out var set)) { set=new HashSet<string>(StringComparer.Ordinal); facts.Add(recipeId,set); }
            set.Add(fact); if(fact.StartsWith("amount:",StringComparison.Ordinal)) set.Add("ingredient:"+fact.Substring(7));
        }
        public void SeedKnown(string recipeId,string provenance)
        { if(!catalog.Recipes.ContainsKey(recipeId) || string.IsNullOrWhiteSpace(provenance)) throw new ArgumentException("Invalid knowledge seed."); if(!known.ContainsKey(recipeId)) known.Add(recipeId,provenance); }
        public bool Discover(ServiceOutcome outcome,string occurrenceId)
        {
            if(string.IsNullOrWhiteSpace(occurrenceId)) throw new ArgumentException("Occurrence required.");
            if(outcome.Recognition!=DrinkRecognition.Recognized || known.ContainsKey(outcome.RecipeId)) return false;
            known.Add(outcome.RecipeId,"service:"+occurrenceId); return true;
        }
        public KnowledgeProjection Project(string recipeId)
        {
            var r=catalog.Recipes[recipeId]; bool full=known.ContainsKey(recipeId);
            facts.TryGetValue(recipeId,out var set); bool Has(string f) => full || (set!=null && set.Contains(f));
            var amounts=r.Amounts.Where(x=>Has("amount:"+x.Key)).ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal);
            return new KnowledgeProjection(full,Has("identity") ? catalog.Cocktails[r.CocktailId].Name : null,Has("method") ? "Stir" : null,
                r.Amounts.Keys.Where(x=>Has("ingredient:"+x)),amounts,Has("preparation"),Has("garnish"));
        }
        public IReadOnlyDictionary<string,string> KnownReceipts => new ReadOnlyDictionary<string,string>(new Dictionary<string,string>(known,StringComparer.Ordinal));
        public IReadOnlyDictionary<string,string[]> Facts => new ReadOnlyDictionary<string,string[]>(facts.ToDictionary(x=>x.Key,x=>x.Value.OrderBy(f=>f,StringComparer.Ordinal).ToArray(),StringComparer.Ordinal));
    }
}
