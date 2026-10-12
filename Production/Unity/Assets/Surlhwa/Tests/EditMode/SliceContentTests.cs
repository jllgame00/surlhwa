using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Surlhwa.ContentIO;

namespace Surlhwa.Tests
{
    public sealed class SliceContentTests
    {
        internal static string ContentRoot()
        {
            for(var p=new DirectoryInfo(Environment.CurrentDirectory);p!=null;p=p.Parent)
            {
                foreach(var suffix in new[]{"Content","Production/Content"})
                {
                    var candidate=Path.Combine(p.FullName,suffix);
                    if(File.Exists(Path.Combine(candidate,"Profiles/stir-slice.json"))) return candidate;
                }
            }
            throw new InvalidOperationException("Authored Slice content not found.");
        }
        internal static string Bundle() => SliceContentLoader.BundleFromDirectory(ContentRoot());
        [Test] public void AuthoredSliceUsesEightExactIngredientsAndApprovedPrinceExcerpt()
        {
            var c=SliceContentLoader.Load(Bundle());
            Assert.That(c.Catalog.Ingredients.Count,Is.EqualTo(8));
            Assert.That(c.Catalog.Recipes["recipe_test_stir"].Amounts["whiskey"],Is.EqualTo(59.14706m));
            Assert.That(c.Catalog.Recipes["recipe_dry_martini"].Amounts["gin"],Is.EqualTo(60m));
            Assert.That(c.Encounters.Select(x=>x.Id),Is.EqualTo(new[]{"enc_ordinary13","enc_prince_warmth_excerpt"}));
            Assert.That(c.Encounters[1].GuestId,Is.EqualTo("guest_little_prince"));
            Assert.That(c.Encounters.All(x=>x.MemoryGrants.Count==0),Is.True);
            Assert.That(c.Profile.CompletesAct,Is.False);
        }
        [Test] public void SourceWordingRemainsKoreanAndKeepsItsRole()
        {
            var c=SliceContentLoader.Load(Bundle());
            Assert.That(c.Texts["text_rc1_0389"].Value,Is.EqualTo("지난번의 대답은 듣지 못했지만, 오늘은 조용히 앉아 있을게요."));
            Assert.That(c.Texts["text_rc1_0389"].SourceId,Is.EqualTo("SRC-RC1-0389"));
            Assert.That(c.Texts["text_rc1_0208"].Role,Is.EqualTo("narration"));
            Assert.That(c.Texts["text_rc1_0209"].Role,Is.EqualTo("mixed"));
        }
        [Test] public void SeededKnowledgeIsIndependentOfSupplyAndOtherRuns()
        {
            var c=SliceContentLoader.Load(Bundle()); var first=c.CreateKnowledge(); var second=c.CreateKnowledge();
            Assert.That(first.Project("recipe_test_stir").Known,Is.True);
            Assert.That(first.Project("recipe_manhattan").Known,Is.True);
            Assert.That(first.Project("recipe_dry_martini").Known,Is.False);
            Assert.That(first.Project("recipe_dry_martini").Name,Is.Null);
            Assert.That(first.Project("recipe_dry_martini").Amounts["gin"],Is.EqualTo(60m));
            first.SeedKnown("recipe_dry_martini","test_only_seed");
            Assert.That(second.Project("recipe_dry_martini").Known,Is.False);
            Assert.That(c.Profile.Supply,Does.Contain("gin"));
        }
        [TestCase("unknown_field")]
        [TestCase("future_schema")]
        [TestCase("wrong_unit")]
        [TestCase("too_precise")]
        [TestCase("negative_amount")]
        [TestCase("missing_ingredient")]
        [TestCase("duplicate_ingredient")]
        [TestCase("unsupported_method")]
        [TestCase("source_changed")]
        [TestCase("missing_text")]
        [TestCase("unearned_memory")]
        [TestCase("unreachable_supply")]
        public void InvalidAuthoredContentFailsBeforePublication(string fault)
        {
            var b=JObject.Parse(Bundle());
            switch(fault)
            {
                case "unknown_field": b["typo"]=true; break;
                case "future_schema": b["schemaVersion"]=2; break;
                case "wrong_unit": b["tuning"]["liquidUnit"]="oz"; break;
                case "too_precise": b["recipes"][0]["amounts"][0]["ml"]=.0000001m; break;
                case "negative_amount": b["recipes"][0]["amounts"][0]["ml"]=-1; break;
                case "missing_ingredient": b["recipes"][0]["amounts"][0]["ingredientId"]="missing"; break;
                case "duplicate_ingredient": ((JArray)b["ingredients"]).Add(b["ingredients"][0].DeepClone()); break;
                case "unsupported_method": b["recipes"][0]["method"]="float"; break;
                case "source_changed": b["texts"][0]["value"]="changed"; break;
                case "missing_text": b["encounters"][0]["intro"][0]="missing"; break;
                case "unearned_memory": ((JArray)b["encounters"][1]["memoryGrants"]).Add("memory_prince_star"); break;
                case "unreachable_supply": ((JArray)b["profile"]["supply"]).Clear(); break;
            }
            Assert.Throws<ContentValidationException>(()=>SliceContentLoader.Load(b.ToString()));
        }
        [Test] public void DuplicateJsonKeyCannotOverrideAnApprovedField()
        {
            var valid=Bundle(); var duplicate=valid.Insert(valid.IndexOf('{')+1,"\"schemaVersion\":1,");
            Assert.Throws<ContentValidationException>(()=>SliceContentLoader.Load(duplicate));
        }
    }
}
