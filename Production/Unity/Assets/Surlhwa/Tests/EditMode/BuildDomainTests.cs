using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Surlhwa.Domain;

namespace Surlhwa.Tests
{
    public sealed class BuildDomainTests
    {
        internal static BartendingCatalog Catalog() => new BartendingCatalog(
            new[]{"gin","campari","sweet_red_vermouth"},
            new[]{new CocktailDefinition("cocktail_negroni","Negroni",new[]{"bitterness"})},
            new[]{new StirRecipe("recipe_negroni","cocktail_negroni",new Dictionary<string,decimal>{["gin"]=30m,["campari"]=30m,["sweet_red_vermouth"]=30m},
                requiredGarnish:"orange_slice",method:"build",garnishPosition:"rim")});
        internal static BrewingSession Session()
        { var c=Catalog(); return new BrewingSession(c,BartendingDomainTests.Tuning(),c.Ingredients); }
        private static ServiceOutcome Outcome(BrewingSession s) => ServiceEvaluator.Evaluate(Catalog(),BartendingDomainTests.Tuning(),s.Freeze(),new DrinkRequest("bitterness"));
        private static void Fill(BrewingSession s)
        { s.Pour("gin",ContainerRole.Serving,30m); s.Pour("campari",ContainerRole.Serving,30m); s.Pour("sweet_red_vermouth",ContainerRole.Serving,30m); }

        [Test] public void DirectBuildNeedsNoStirTimingGradeOrStrain()
        {
            var s=Session(); Fill(s); s.AddIce(ContainerRole.Serving); s.GentleStir(); s.Garnish("orange_slice","rim");
            var result=Outcome(s);
            Assert.That(result.Recognition,Is.EqualTo(DrinkRecognition.Recognized));
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Good));
            Assert.That(result.QualityGood,Is.True); Assert.That(result.Fits,Is.True);
            Assert.That(s.Freeze().StirDegrees,Is.Zero); Assert.That(s.Freeze().TotalMl,Is.EqualTo(90m));
        }
        [Test] public void BuildAddedLiquidRequiresGentleStirOnCurrentBatch()
        {
            var s=Session(); s.Pour("gin",ContainerRole.Serving,30m); s.AddIce(ContainerRole.Serving); s.GentleStir();
            s.Pour("campari",ContainerRole.Serving,30m); s.Pour("sweet_red_vermouth",ContainerRole.Serving,30m); s.Garnish("orange_slice","rim");
            Assert.That(Outcome(s).Preparation,Is.EqualTo(PreparationResult.Weak));
            s.GentleStir(); Assert.That(Outcome(s).Preparation,Is.EqualTo(PreparationResult.Good));
        }
        [Test] public void BuildMeasuredInJiggerStillUsesServingAsPreparationContainer()
        {
            var s=Session(); foreach(var id in new[]{"gin","campari","sweet_red_vermouth"})
            { s.Pour(id,ContainerRole.Jigger,30m); s.Transfer(ContainerRole.Jigger,ContainerRole.Serving,30m,BrewAction.PourFromContainer); }
            s.AddIce(ContainerRole.Serving); s.GentleStir(); s.Garnish("orange_slice","rim");
            Assert.That(Outcome(s).Preparation,Is.EqualTo(PreparationResult.Good));
            Assert.That(s.Container(ContainerRole.Jigger).TotalMl,Is.Zero);
        }
        [Test] public void BuildMixedInMixingGlassCannotMasqueradeAsDirectBuild()
        {
            var s=Session(); foreach(var id in new[]{"gin","campari","sweet_red_vermouth"}) s.Pour(id,ContainerRole.Mixing,30m);
            s.Transfer(ContainerRole.Mixing,ContainerRole.Serving,90m,BrewAction.PourFromContainer);
            s.AddIce(ContainerRole.Serving); s.GentleStir(); s.Garnish("orange_slice","rim");
            Assert.That(Outcome(s).Preparation,Is.EqualTo(PreparationResult.Weak));
            Assert.That(Outcome(s).Recognition,Is.EqualTo(DrinkRecognition.Recognized));
        }
        [Test] public void BuildMissingIceOrRequiredGarnishIsPreparationFailureNotWrongCocktail()
        {
            var s=Session(); Fill(s); s.GentleStir();
            Assert.That(Outcome(s).Recognition,Is.EqualTo(DrinkRecognition.Recognized));
            Assert.That(Outcome(s).Preparation,Is.EqualTo(PreparationResult.Weak));
            s.AddIce(ContainerRole.Serving); s.GentleStir();
            Assert.That(Outcome(s).Preparation,Is.EqualTo(PreparationResult.Weak));
            s.Garnish("orange_slice","rim"); Assert.That(Outcome(s).Preparation,Is.EqualTo(PreparationResult.Good));
        }
        [Test] public void MethodKnowledgeIsFilteredAndUsesActualRecipeMethod()
        {
            var k=new RecipeKnowledge(Catalog()); Assert.That(k.Project("recipe_negroni").Method,Is.Null);
            k.GrantFact("recipe_negroni","method"); Assert.That(k.Project("recipe_negroni").Method,Is.EqualTo("Build"));
            Assert.That(k.Project("recipe_negroni").Known,Is.False);
        }
    }
}
