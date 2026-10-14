using System;
using System.Collections.Generic;
using NUnit.Framework;
using Surlhwa.Domain;

namespace Surlhwa.Tests
{
    public sealed class ShakeDomainTests
    {
        internal static BartendingCatalog Catalog() => new BartendingCatalog(new[]{"gin","lemon_juice","simple_syrup","soda_water"},
            new[]{new CocktailDefinition("cocktail_gin_fizz","Gin Fizz",new[]{"awakening","clarity"})},
            new[]{new StirRecipe("recipe_gin_fizz","cocktail_gin_fizz",new Dictionary<string,decimal>{["gin"]=45m,["lemon_juice"]=30m,["simple_syrup"]=10m,["soda_water"]=15m},method:"shake",lateIngredient:"soda_water")});
        internal static BrewingSession Session() { var c=Catalog(); return new BrewingSession(c,BartendingDomainTests.Tuning(),c.Ingredients); }
        internal static void Fill(BrewingSession s)
        { s.Pour("gin",ContainerRole.Shaker,45m); s.Pour("lemon_juice",ContainerRole.Shaker,30m); s.Pour("simple_syrup",ContainerRole.Shaker,10m); s.AddIce(ContainerRole.Shaker); }
        internal static void Motion(BrewingSession s,int crossings=8,long interval=500000)
        {
            s.SetShakerClosed(true); s.BeginShake(1);
            for(int n=0;n<crossings;n++) s.MoveShake(n%2==0 ? -1 : 1,interval);
            s.EndShake(); s.SetShakerClosed(false);
        }
        internal static ServiceOutcome Finish(BrewingSession s,decimal ml=85m)
        {
            s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,ml,BrewAction.PourFromContainer);
            s.Pour("soda_water",ContainerRole.Serving,15m,BrewAction.Splash);
            return ServiceEvaluator.Evaluate(Catalog(),BartendingDomainTests.Tuning(),s.Freeze(),new DrinkRequest("clarity"));
        }
        [Test] public void ActualClosedShakerMotionProducesGinFizzWithoutServingIceOrStirGrade()
        {
            var s=Session(); Fill(s); Motion(s); var result=Finish(s);
            Assert.That(result.Recognition,Is.EqualTo(DrinkRecognition.Recognized)); Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Good));
            Assert.That(result.QualityGood,Is.True); Assert.That(s.Freeze().HasIce,Is.False);
            Assert.That(s.Freeze().TotalMl,Is.EqualTo(100m)); Assert.That(s.Freeze().StirQualityGood,Is.False);
        }
        [Test] public void ShakingWithoutClosingCannotGrantTechnique()
        {
            var s=Session(); Fill(s); s.BeginShake(1); s.MoveShake(-1,500000); s.MoveShake(1,500000); s.EndShake();
            Assert.That(Finish(s).QualityGood,Is.False);
        }
        [Test] public void RapidTeleportingShakeDoesNotBecomeGoodRhythm()
        {
            var s=Session(); Fill(s); Motion(s,8,1); var result=Finish(s);
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Good)); Assert.That(result.QualityGood,Is.False);
        }
        [Test] public void ClosedShakerRejectsLiquidAndIceChangesBeforeMutation()
        {
            var s=Session(); Fill(s); s.SetShakerClosed(true);
            Assert.Throws<InvalidOperationException>(()=>s.Pour("gin",ContainerRole.Shaker,1m));
            Assert.Throws<InvalidOperationException>(()=>s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,10m,BrewAction.PourFromContainer));
            Assert.That(s.Container(ContainerRole.Shaker).TotalMl,Is.EqualTo(85m)); Assert.That(s.Container(ContainerRole.Serving).TotalMl,Is.Zero);
        }
        [Test] public void NewLiquidInvalidatesShakeEvenIfFinalRecipeStillMatches()
        {
            var s=Session(); Fill(s); Motion(s); s.Pour("gin",ContainerRole.Shaker,.1m);
            Assert.That(Finish(s,85.1m).QualityGood,Is.False);
        }
        [Test] public void SodaShakenInsteadOfLateSplashFailsPreparation()
        {
            var s=Session(); Fill(s); s.Pour("soda_water",ContainerRole.Shaker,15m); Motion(s);
            s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,100m,BrewAction.PourFromContainer);
            var result=ServiceEvaluator.Evaluate(Catalog(),BartendingDomainTests.Tuning(),s.Freeze(),new DrinkRequest("clarity"));
            Assert.That(result.Recognition,Is.EqualTo(DrinkRecognition.Recognized)); Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Weak));
        }
        [Test] public void MultiplePartialTransfersPreserveOnlyActuallyShakenLiquid()
        {
            var s=Session(); Fill(s); Motion(s);
            s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,20m,BrewAction.PourFromContainer);
            Assert.That(Finish(s,65m).QualityGood,Is.True); Assert.That(s.Container(ContainerRole.Shaker).TotalMl,Is.Zero);
        }
        [Test] public void CancelledInputCannotAccumulateFurtherShakeMotion()
        {
            var s=Session(); Fill(s); s.SetShakerClosed(true); s.BeginShake(1); s.MoveShake(-1,500000); s.CancelInput();
            for(int i=0;i<20;i++) s.MoveShake(i%2==0 ? 1 : -1,500000);
            s.SetShakerClosed(false); Assert.That(Finish(s).QualityGood,Is.False);
        }
        [Test] public void UnshakenAdditionToServingCannotBorrowEarlierShakeQuality()
        {
            var s=Session(); Fill(s); Motion(s); s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,85m,BrewAction.PourFromContainer);
            s.Pour("gin",ContainerRole.Serving,.2m); s.Pour("soda_water",ContainerRole.Serving,15m,BrewAction.Splash);
            var result=ServiceEvaluator.Evaluate(Catalog(),BartendingDomainTests.Tuning(),s.Freeze(),new DrinkRequest("clarity"));
            Assert.That(result.Recognition,Is.EqualTo(DrinkRecognition.Recognized));
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Weak)); Assert.That(result.QualityGood,Is.False);
        }
    }
}
