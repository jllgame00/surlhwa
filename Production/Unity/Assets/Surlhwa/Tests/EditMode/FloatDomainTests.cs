using System;
using System.Collections.Generic;
using NUnit.Framework;
using Surlhwa.Domain;

namespace Surlhwa.Tests
{
    public sealed class FloatDomainTests
    {
        internal static BartendingCatalog Catalog() => new BartendingCatalog(new[]{"vodka","coffee_liqueur","cream"},
            new[]{new CocktailDefinition("cocktail_manwol","만월",new[]{"longing","clarity"})},
            new[]{new StirRecipe("recipe_manwol","cocktail_manwol",new Dictionary<string,decimal>{["vodka"]=59.14706m,["coffee_liqueur"]=29.57353m,["cream"]=29.57353m},method:"float",layerOrder:new[]{"vodka","coffee_liqueur","cream"})});
        internal static BrewingSession Session() { var c=Catalog(); return new BrewingSession(c,BartendingDomainTests.Tuning(),c.Ingredients); }
        internal static void Layer(BrewingSession s,string ingredient,decimal ml,decimal intensity=.2m,double height=.1)
        { s.Pour(ingredient,ContainerRole.Jigger,ml); s.BeginFloat(); s.UpdateFloat(4000000,intensity,height); s.EndFlow(); }
        private static ServiceOutcome Outcome(BrewingSession s) => ServiceEvaluator.Evaluate(Catalog(),BartendingDomainTests.Tuning(),s.Freeze(),new DrinkRequest("clarity"));
        [Test] public void ManwolLayersRequireActualMeasuredLiquidAndStableFlow()
        {
            var s=Session(); s.Pour("vodka",ContainerRole.Serving,59.14706m); Layer(s,"coffee_liqueur",29.57353m); Layer(s,"cream",29.57353m);
            var result=Outcome(s); Assert.That(result.Recognition,Is.EqualTo(DrinkRecognition.Recognized));
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Good)); Assert.That(result.QualityGood,Is.True);
            Assert.That(s.Freeze().TotalMl,Is.EqualTo(118.29412m)); Assert.That(s.Container(ContainerRole.Jigger).TotalMl,Is.Zero);
        }
        [Test] public void CorrectFloatIngredientsInWrongOrderRemainRecognizedButWrongPreparation()
        {
            var s=Session(); s.Pour("vodka",ContainerRole.Serving,59.14706m); Layer(s,"cream",29.57353m); Layer(s,"coffee_liqueur",29.57353m);
            Assert.That(Outcome(s).Recognition,Is.EqualTo(DrinkRecognition.Recognized)); Assert.That(Outcome(s).Preparation,Is.EqualTo(PreparationResult.Weak));
        }
        [Test] public void GoodTopLayerCannotRepairAnEarlierMixedBoundary()
        {
            var s=Session(); s.Pour("vodka",ContainerRole.Serving,59.14706m); Layer(s,"coffee_liqueur",29.57353m,1m); Layer(s,"cream",29.57353m);
            Assert.That(Outcome(s).Preparation,Is.EqualTo(PreparationResult.Good)); Assert.That(Outcome(s).QualityGood,Is.False);
        }
        [Test] public void HighPourPositionMixesBoundaryEvenAtLowFlow()
        {
            var s=Session(); s.Pour("vodka",ContainerRole.Serving,59.14706m); Layer(s,"coffee_liqueur",29.57353m,.2m,.9); Layer(s,"cream",29.57353m);
            Assert.That(Outcome(s).QualityGood,Is.False);
        }
        [Test] public void LayeredServingTransferRemovesTopLiquidInsteadOfUniformStirPortion()
        {
            var s=Session(); s.Pour("vodka",ContainerRole.Serving,59.14706m); Layer(s,"coffee_liqueur",29.57353m); Layer(s,"cream",29.57353m);
            s.Transfer(ContainerRole.Serving,ContainerRole.Jigger,20m,BrewAction.PourFromContainer);
            Assert.That(s.Container(ContainerRole.Jigger).Amounts["cream"],Is.EqualTo(20m));
            Assert.That(s.Container(ContainerRole.Jigger).Amounts.ContainsKey("vodka"),Is.False);
            Assert.That(s.Container(ContainerRole.Serving).TotalMl,Is.EqualTo(98.29412m));
        }
        [Test] public void OrdinaryPourCannotGrantFloatTechnique()
        {
            var s=Session(); s.Pour("vodka",ContainerRole.Serving,59.14706m); s.Pour("coffee_liqueur",ContainerRole.Serving,29.57353m); s.Pour("cream",ContainerRole.Serving,29.57353m);
            Assert.That(Outcome(s).Recognition,Is.EqualTo(DrinkRecognition.Recognized)); Assert.That(Outcome(s).QualityGood,Is.False);
        }
        [Test] public void CancelledFloatCannotContinueAddingLiquid()
        {
            var s=Session(); s.Pour("vodka",ContainerRole.Serving,59.14706m); s.Pour("coffee_liqueur",ContainerRole.Jigger,29.57353m);
            s.BeginFloat(); s.UpdateFloat(1000000,.2m,.1); s.CancelInput(); s.UpdateFloat(1000000,.2m,.1);
            Assert.That(s.Container(ContainerRole.Serving).TotalMl,Is.EqualTo(67.14706m));
            Assert.That(s.Container(ContainerRole.Jigger).TotalMl,Is.EqualTo(21.57353m));
        }
        [Test] public void LayeredOverflowAcceptsTopFirstAndSpillsTheLaterExtractedLiquid()
        {
            var s=Session(); s.Pour("vodka",ContainerRole.Serving,59.14706m); Layer(s,"coffee_liqueur",29.57353m); Layer(s,"cream",29.57353m);
            s.Pour("vodka",ContainerRole.Jigger,50m);
            var move=s.Transfer(ContainerRole.Serving,ContainerRole.Jigger,40m,BrewAction.PourFromContainer);
            Assert.That(move.AcceptedMl,Is.EqualTo(10m)); Assert.That(move.SpilledMl,Is.EqualTo(30m));
            Assert.That(move.Accepted["cream"],Is.EqualTo(10m)); Assert.That(move.Accepted.ContainsKey("coffee_liqueur"),Is.False);
        }
    }
}
