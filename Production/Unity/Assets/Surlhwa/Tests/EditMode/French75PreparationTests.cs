using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Surlhwa.Domain;

namespace Surlhwa.Tests
{
    public sealed class French75PreparationTests
    {
        internal static BartendingCatalog Catalog() => new BartendingCatalog(new[]{"gin","fresh_lemon_juice","simple_syrup","champagne"},
            new[]{new CocktailDefinition("cocktail_french_75","French 75",new[]{"awakening"})},
            new[]{Recipe()});
        private static StirRecipe Recipe(string method="shake",string late="champagne",ShakeFinishPolicy finish=ShakeFinishPolicy.StrainThenPourAndGentleStir) =>
            new StirRecipe("recipe_french_75","cocktail_french_75",new Dictionary<string,decimal>{["gin"]=30m,["fresh_lemon_juice"]=15m,["simple_syrup"]=15m,["champagne"]=60m},
                method:method,lateIngredient:late,shakeFinish:finish);
        internal static BrewingSession Session() { var c=Catalog(); return new BrewingSession(c,BartendingDomainTests.Tuning(),c.Ingredients); }
        internal static void Fill(BrewingSession s,decimal scale=1m)
        {
            s.Pour("gin",ContainerRole.Shaker,30m*scale); s.Pour("fresh_lemon_juice",ContainerRole.Shaker,15m*scale);
            s.Pour("simple_syrup",ContainerRole.Shaker,15m*scale); s.AddIce(ContainerRole.Shaker);
        }
        private static BrewingSession Strained(long interval=500000)
        {
            var s=Session(); Fill(s); ShakeDomainTests.Motion(s,interval:interval);
            s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,60m,BrewAction.Strain); return s;
        }
        private static void Champagne(BrewingSession s,decimal ml=60m,bool jigger=false)
        {
            s.Pour("champagne",jigger ? ContainerRole.Jigger : ContainerRole.Serving,ml);
            if(jigger) s.Transfer(ContainerRole.Jigger,ContainerRole.Serving,ml,BrewAction.PourFromContainer);
        }
        private static ServiceOutcome Outcome(BrewingSession s,string tag="awakening") =>
            ServiceEvaluator.Evaluate(Catalog(),BartendingDomainTests.Tuning(),s.Freeze(),new DrinkRequest(tag));

        [Test] public void ShakerStrainMovesOnlyActualLiquidAndRetainsSourceIce()
        {
            var s=ShakeDomainTests.Session(); ShakeDomainTests.Fill(s); ShakeDomainTests.Motion(s);
            Assert.DoesNotThrow(()=>s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,20m,BrewAction.Strain));
            Assert.That(s.Container(ContainerRole.Shaker).TotalMl,Is.EqualTo(65m));
            Assert.That(s.Container(ContainerRole.Serving).TotalMl,Is.EqualTo(20m));
            Assert.That(s.Container(ContainerRole.Shaker).HasIce,Is.True);
            Assert.That(s.Freeze().HasIce,Is.False); Assert.That(s.Freeze().Shake,Is.Not.Null);
            Assert.That(s.Freeze().Strained,Is.True);
        }

        [Test] public void HeldShakerStrainConservesLiquidAcrossPartialFlow()
        {
            var s=ShakeDomainTests.Session(); ShakeDomainTests.Fill(s); ShakeDomainTests.Motion(s);
            Assert.DoesNotThrow(()=>s.BeginTransfer(ContainerRole.Shaker,ContainerRole.Serving,BrewAction.Strain));
            var moved=s.UpdateFlow(500000,1m,false); s.EndFlow();
            Assert.That(moved.AcceptedMl,Is.EqualTo(20m)); Assert.That(moved.SpilledMl,Is.Zero);
            Assert.That(s.Container(ContainerRole.Shaker).TotalMl,Is.EqualTo(65m));
            Assert.That(s.Freeze().TotalMl,Is.EqualTo(20m));
        }

        [Test] public void ClosedShakerStrainRejectsBothTransferPathsBeforeMutation()
        {
            var s=ShakeDomainTests.Session(); ShakeDomainTests.Fill(s); s.SetShakerClosed(true);
            Assert.Throws<InvalidOperationException>(()=>s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,20m,BrewAction.Strain));
            Assert.Throws<InvalidOperationException>(()=>s.BeginTransfer(ContainerRole.Shaker,ContainerRole.Serving,BrewAction.Strain));
            Assert.That(s.Container(ContainerRole.Shaker).TotalMl,Is.EqualTo(85m));
            Assert.That(s.Freeze().TotalMl,Is.Zero); Assert.That(s.FlowActive,Is.False);
        }

        [TestCase(false)] [TestCase(true)]
        public void StrainedBaseThenMeasuredChampagneAndFreshGentleStirProducesFrench75(bool jigger)
        {
            var s=Strained(); Champagne(s,jigger:jigger); s.GentleStir(); var result=Outcome(s);
            Assert.That(result.Recognition,Is.EqualTo(DrinkRecognition.Recognized));
            Assert.That(result.CocktailId,Is.EqualTo("cocktail_french_75"));
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Good));
            Assert.That(result.QualityGood,Is.True); Assert.That(result.Fits,Is.True);
            Assert.That(s.Freeze().TotalMl,Is.EqualTo(120m)); Assert.That(s.Freeze().Strained,Is.False);
            Assert.That(s.Freeze().Shake.Amounts["gin"],Is.EqualTo(30m));
            Assert.That(s.Freeze().Shake.Amounts.ContainsKey("champagne"),Is.False);
        }

        [Test] public void PartialStrainAndMultipleMeasuredAdditionsConserveActualShakenBase()
        {
            var s=Session(); Fill(s); ShakeDomainTests.Motion(s);
            s.BeginTransfer(ContainerRole.Shaker,ContainerRole.Serving,BrewAction.Strain);
            s.UpdateFlow(500000,1m,false); s.EndFlow();
            Assert.That(s.Freeze().Shake.Amounts.Values.Sum(),Is.EqualTo(20m));
            s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,40m,BrewAction.Strain);
            s.Pour("champagne",ContainerRole.Jigger,60m);
            s.Transfer(ContainerRole.Jigger,ContainerRole.Serving,25m,BrewAction.PourFromContainer);
            Assert.That(s.Container(ContainerRole.Jigger).TotalMl,Is.EqualTo(35m));
            Assert.That(s.Freeze().TotalMl,Is.EqualTo(85m)); Assert.That(s.Freeze().Shake,Is.Not.Null);
            Assert.That(s.Freeze().Shake.Amounts.Values.Sum(),Is.EqualTo(60m));
            s.Transfer(ContainerRole.Jigger,ContainerRole.Serving,35m,BrewAction.PourFromContainer); s.GentleStir();
            Assert.That(s.Container(ContainerRole.Jigger).TotalMl,Is.Zero); Assert.That(s.Container(ContainerRole.Shaker).TotalMl,Is.Zero);
            Assert.That(s.Freeze().TotalMl,Is.EqualTo(120m)); Assert.That(Outcome(s).Preparation,Is.EqualTo(PreparationResult.Good));
            Assert.That(Outcome(s).QualityGood,Is.True);
        }

        [Test] public void MeasuredLateAdditionDoesNotDependOnCurrentStrainedFlag()
        {
            var s=Strained(); Champagne(s,20m); Assert.That(s.Freeze().Strained,Is.False);
            Champagne(s,40m,true); s.GentleStir();
            Assert.That(Outcome(s).Preparation,Is.EqualTo(PreparationResult.Good)); Assert.That(Outcome(s).QualityGood,Is.True);
        }

        [Test] public void WrongRequestCannotChangeRecognizedFrench75OrEarnedPreparation()
        {
            var s=Strained(); Champagne(s); s.GentleStir(); var result=Outcome(s,"warmth");
            Assert.That(result.CocktailId,Is.EqualTo("cocktail_french_75")); Assert.That(result.Fits,Is.False);
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Good)); Assert.That(result.QualityGood,Is.True);
        }

        [Test] public void FastShakeRetainsPreparationButDoesNotEarnTechniqueQuality()
        {
            var s=Strained(1); Champagne(s); s.GentleStir(); var result=Outcome(s);
            Assert.That(result.Recognition,Is.EqualTo(DrinkRecognition.Recognized));
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Good)); Assert.That(result.QualityGood,Is.False);
        }

        [TestCase("missing")] [TestCase("before_champagne")] [TestCase("before_final_pour")] [TestCase("before_final_jigger")]
        public void MissingOrStaleGentleStirFailsHowWithoutErasingActualShake(string sequence)
        {
            var s=Strained();
            if(sequence=="before_champagne") s.GentleStir();
            if(sequence=="before_final_pour" || sequence=="before_final_jigger")
            { Champagne(s,30m); s.GentleStir(); Champagne(s,30m,sequence=="before_final_jigger"); }
            else Champagne(s);
            var result=Outcome(s);
            Assert.That(result.Recognition,Is.EqualTo(DrinkRecognition.Recognized));
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Weak));
            Assert.That(result.Reasons,Does.Contain("gentle_stir_batch_missing")); Assert.That(result.QualityGood,Is.True);
        }

        [TestCase("early")] [TestCase("shaken")] [TestCase("splash")] [TestCase("wrong_transfer")]
        [TestCase("between_strains")] [TestCase("partly_early")] [TestCase("partly_splash")]
        public void CorrectFinalCompositionCannotHideIncorrectFrenchFinish(string sequence)
        {
            var s=Session(); Fill(s);
            if(sequence=="early") Champagne(s);
            if(sequence=="partly_early") Champagne(s,.1m);
            if(sequence=="shaken") s.Pour("champagne",ContainerRole.Shaker,60m);
            ShakeDomainTests.Motion(s);
            if(sequence=="between_strains")
            {
                s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,30m,BrewAction.Strain); Champagne(s);
                s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,30m,BrewAction.Strain);
            }
            else s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,120m,sequence=="wrong_transfer" ? BrewAction.PourFromContainer : BrewAction.Strain);
            if(sequence=="partly_splash") s.Pour("champagne",ContainerRole.Serving,.1m,BrewAction.Splash);
            if(sequence=="splash") s.Pour("champagne",ContainerRole.Serving,60m,BrewAction.Splash);
            else if(sequence=="partly_early" || sequence=="partly_splash") Champagne(s,59.9m);
            else if(sequence=="wrong_transfer") Champagne(s);
            s.GentleStir(); var result=Outcome(s);
            Assert.That(result.Recognition,Is.EqualTo(DrinkRecognition.Recognized));
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Weak));
        }

        [TestCase(false)] [TestCase(true)]
        public void UnshakenBaseCannotBorrowEarlierShakeThroughLateAddition(bool jigger)
        {
            var s=Strained(); s.Pour("gin",jigger ? ContainerRole.Jigger : ContainerRole.Serving,.2m);
            Champagne(s,jigger ? 59.8m : 60m,jigger); s.GentleStir(); var result=Outcome(s);
            Assert.That(result.Recognition,Is.EqualTo(DrinkRecognition.Recognized));
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Weak)); Assert.That(result.QualityGood,Is.False);
            Assert.That(s.Freeze().Shake,Is.Not.Null); Assert.That(s.Freeze().Shake.Amounts["gin"],Is.EqualTo(30m));
        }

        [Test] public void DifferentShakenBatchesCannotCombineTheirTechniqueEvidence()
        {
            var s=Session(); Fill(s,.5m); ShakeDomainTests.Motion(s);
            s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,30m,BrewAction.Strain);
            Fill(s,.5m); ShakeDomainTests.Motion(s); s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,30m,BrewAction.Strain);
            Champagne(s,60m,true); s.GentleStir(); var result=Outcome(s);
            Assert.That(result.Recognition,Is.EqualTo(DrinkRecognition.Recognized)); Assert.That(s.Freeze().Shake,Is.Null);
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Weak)); Assert.That(result.QualityGood,Is.False);
        }

        [Test] public void NewAttemptCannotReuseDiscardedShakeOrGentleStir()
        {
            var discarded=Strained(); Champagne(discarded); discarded.GentleStir();
            var fresh=Session(); Fill(fresh); fresh.Transfer(ContainerRole.Shaker,ContainerRole.Serving,60m,BrewAction.Strain);
            Champagne(fresh); var result=Outcome(fresh);
            Assert.That(fresh.Freeze().AttemptId,Is.Not.EqualTo(discarded.Freeze().AttemptId));
            Assert.That(result.Recognition,Is.EqualTo(DrinkRecognition.Recognized));
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Weak)); Assert.That(result.QualityGood,Is.False);
        }

        [Test] public void NewFinishDoesNotAuthorizeGinFizzStrainOrJiggerSplashSubstitution()
        {
            var strained=ShakeDomainTests.Session(); ShakeDomainTests.Fill(strained); ShakeDomainTests.Motion(strained);
            strained.Transfer(ContainerRole.Shaker,ContainerRole.Serving,85m,BrewAction.Strain);
            var wrongStrain=ShakeDomainTests.Finish(strained,0m);
            Assert.That(wrongStrain.Preparation,Is.EqualTo(PreparationResult.Weak));
            var measured=ShakeDomainTests.Session(); ShakeDomainTests.Fill(measured); ShakeDomainTests.Motion(measured);
            measured.Transfer(ContainerRole.Shaker,ContainerRole.Serving,85m,BrewAction.PourFromContainer);
            measured.Pour("soda_water",ContainerRole.Jigger,15m); measured.Transfer(ContainerRole.Jigger,ContainerRole.Serving,15m,BrewAction.PourFromContainer);
            var wrongLate=ServiceEvaluator.Evaluate(ShakeDomainTests.Catalog(),BartendingDomainTests.Tuning(),measured.Freeze(),new DrinkRequest("clarity"));
            Assert.That(wrongLate.Preparation,Is.EqualTo(PreparationResult.Weak)); Assert.That(wrongLate.QualityGood,Is.False);
        }

        [TestCase("stir","champagne",1)] [TestCase("stir",null,1)] [TestCase("shake",null,1)] [TestCase("shake","gin",2)]
        public void InvalidFinishConfigurationIsRejectedAtRecipeConstruction(string method,string late,int finish)
        { Assert.Throws<ArgumentException>(()=>Recipe(method,late,(ShakeFinishPolicy)finish)); }
    }
}
