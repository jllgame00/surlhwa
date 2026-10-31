using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Surlhwa.Domain;

namespace Surlhwa.Tests
{
    public sealed class LastWordPreparationTests
    {
        // Append-only policy value; this also runs against the pre-ACT6 constructor for RED evidence.
        private const ShakeFinishPolicy StrainOnly=(ShakeFinishPolicy)2;
        private static readonly string[] Ingredients={"gin","green_chartreuse","maraschino_luxardo","fresh_lime_juice"};
        private static StirRecipe Recipe(string method="shake",string late=null,ShakeFinishPolicy finish=StrainOnly) =>
            new StirRecipe("recipe_last_word","cocktail_last_word",new Dictionary<string,decimal>
            { ["gin"]=22.5m,["green_chartreuse"]=22.5m,["maraschino_luxardo"]=22.5m,["fresh_lime_juice"]=22.5m },
                method:method,lateIngredient:late,shakeFinish:finish);
        internal static BartendingCatalog Catalog() => new BartendingCatalog(Ingredients,
            new[]{new CocktailDefinition("cocktail_last_word","Last Word",new[]{"clarity"})},new[]{Recipe()});
        private static BrewingSession Session() { var c=Catalog(); return new BrewingSession(c,BartendingDomainTests.Tuning(),c.Ingredients); }
        private static void Fill(BrewingSession s,decimal scale=1m,bool ice=true,ContainerRole target=ContainerRole.Shaker)
        { foreach(var id in Ingredients) s.Pour(id,target,22.5m*scale); if(ice) s.AddIce(target); }
        private static BrewingSession Shaken(long interval=500000)
        { var s=Session(); Fill(s); ShakeDomainTests.Motion(s,interval:interval); return s; }
        private static BrewingSession Strained(long interval=500000)
        { var s=Shaken(interval); s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,90m,BrewAction.Strain); return s; }
        private static ServiceOutcome Outcome(BrewingSession s,DrinkRequest request=null) =>
            ServiceEvaluator.Evaluate(Catalog(),BartendingDomainTests.Tuning(),s.Freeze(),request ?? new DrinkRequest("clarity"));
        private static void Recognized(ServiceOutcome result)
        { Assert.That(result.Recognition,Is.EqualTo(DrinkRecognition.Recognized)); Assert.That(result.CocktailId,Is.EqualTo("cocktail_last_word")); }

        [Test] public void StrainOnlyRecipeAcceptsNoLateIngredientWithoutChangingExistingFinishValues()
        {
            Assert.DoesNotThrow(()=>Recipe());
            Assert.That((int)ShakeFinishPolicy.PourThenSplash,Is.EqualTo(0));
            Assert.That((int)ShakeFinishPolicy.StrainThenPourAndGentleStir,Is.EqualTo(1));
            Assert.That(Enum.GetName(typeof(ShakeFinishPolicy),StrainOnly),Is.EqualTo("StrainOnly"));
        }

        [TestCase("stir",null)] [TestCase("build",null)] [TestCase("float",null)]
        [TestCase("shake","gin")] [TestCase("shake","fresh_lime_juice")]
        public void StrainOnlyCannotDeclareAnotherMethodOrLateIngredient(string method,string late)
        { Assert.Throws<ArgumentException>(()=>Recipe(method,late)); }

        [Test] public void UnknownFinishPolicyStillFailsAtRecipeConstruction()
        { Assert.Throws<ArgumentException>(()=>Recipe(finish:(ShakeFinishPolicy)3)); }

        [Test] public void ActualClosedShakeThenShakerStrainProducesLastWordWithoutLateFinish()
        {
            var s=Strained(); var result=Outcome(s); var snapshot=s.Freeze(); Recognized(result);
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Good)); Assert.That(result.QualityGood,Is.True); Assert.That(result.Fits,Is.True);
            Assert.That(snapshot.TotalMl,Is.EqualTo(90m)); Assert.That(snapshot.Shake.Amounts.Values.Sum(),Is.EqualTo(90m));
            Assert.That(snapshot.HasIce,Is.False); Assert.That(s.Container(ContainerRole.Shaker).HasIce,Is.True);
            Assert.That(snapshot.Garnish,Is.Null); Assert.That(snapshot.Trace.Any(x=>x.Kind==BrewAction.GentleStir || x.Kind==BrewAction.Splash),Is.False);
        }

        [Test] public void HeldAndPartialStrainsPreserveOnlyAcceptedShakenLiquid()
        {
            var s=Shaken(); s.BeginTransfer(ContainerRole.Shaker,ContainerRole.Serving,BrewAction.Strain);
            var moved=s.UpdateFlow(500000,1m,false); s.EndFlow();
            Assert.That(moved.AcceptedMl,Is.EqualTo(20m)); Assert.That(moved.SpilledMl,Is.Zero);
            Assert.That(s.Freeze().Shake.Amounts.Values.Sum(),Is.EqualTo(20m)); Assert.That(s.Container(ContainerRole.Shaker).TotalMl,Is.EqualTo(70m));
            s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,70m,BrewAction.Strain);
            Assert.That(s.Container(ContainerRole.Shaker).TotalMl,Is.Zero); Assert.That(s.Freeze().TotalMl,Is.EqualTo(90m));
            Assert.That(Outcome(s).Preparation,Is.EqualTo(PreparationResult.Good)); Assert.That(Outcome(s).QualityGood,Is.True);
        }

        [TestCase(false)] [TestCase(true)]
        public void PouredShakerLiquidCannotBorrowStrainFromAnotherPortion(bool partial)
        {
            var s=Shaken();
            if(partial) s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,45m,BrewAction.Strain);
            s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,90m,BrewAction.PourFromContainer);
            var result=Outcome(s); Recognized(result);
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Weak)); Assert.That(result.Reasons,Does.Contain("shaker_strain_missing"));
            Assert.That(result.QualityGood,Is.True); Assert.That(result.Fits,Is.True);
        }

        [Test] public void DirectServingCompositionCannotSubstituteForActualShakerWork()
        {
            var s=Session(); Fill(s,ice:false,target:ContainerRole.Serving); var result=Outcome(s); Recognized(result);
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Weak)); Assert.That(result.QualityGood,Is.False);
            Assert.That(result.Reasons,Does.Contain("shake_batch_missing")); Assert.That(result.Reasons,Does.Contain("shaker_strain_missing"));
        }

        [TestCase(false)] [TestCase(true)]
        public void MixingStrainOrJiggerReturnCannotImpersonateShakerStrain(bool jiggerReturn)
        {
            var s=Shaken();
            if(jiggerReturn)
            {
                s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,90m,BrewAction.Strain);
                s.Transfer(ContainerRole.Serving,ContainerRole.Jigger,45m,BrewAction.PourFromContainer);
                s.Transfer(ContainerRole.Jigger,ContainerRole.Serving,45m,BrewAction.PourFromContainer);
            }
            else
            {
                s.Transfer(ContainerRole.Shaker,ContainerRole.Mixing,90m,BrewAction.PourFromContainer);
                s.Transfer(ContainerRole.Mixing,ContainerRole.Serving,90m,BrewAction.Strain);
            }
            var result=Outcome(s); Recognized(result); Assert.That(s.Freeze().Shake,Is.Not.Null);
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Weak)); Assert.That(result.Reasons,Does.Contain("shaker_strain_missing"));
        }

        [TestCase(false,.2)] [TestCase(true,.2)] [TestCase(false,.001)] [TestCase(true,.001)]
        public void UnshakenServingAdditionCannotBypassStrainPolicyEvenBelowRecognitionTolerance(bool jigger,double amount)
        {
            var s=Strained(); decimal ml=(decimal)amount;
            s.Pour("gin",jigger ? ContainerRole.Jigger : ContainerRole.Serving,ml);
            if(jigger) s.Transfer(ContainerRole.Jigger,ContainerRole.Serving,ml,BrewAction.PourFromContainer);
            var result=Outcome(s); Recognized(result);
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Weak)); Assert.That(result.Reasons,Does.Contain("shaker_strain_missing"));
            Assert.That(s.Freeze().Shake.Amounts["gin"],Is.EqualTo(22.5m));
            if(ml>.01m) Assert.That(result.QualityGood,Is.False);
        }

        [TestCase(false)] [TestCase(true)]
        public void ShakerAdditionAfterMotionCannotTransferOldShakeEvidence(bool afterPartialStrain)
        {
            var s=Shaken();
            if(afterPartialStrain) s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,45m,BrewAction.Strain);
            s.Pour("gin",ContainerRole.Shaker,.2m); s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,90.2m,BrewAction.Strain);
            var result=Outcome(s); Recognized(result);
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Weak)); Assert.That(result.QualityGood,Is.False);
            Assert.That(result.Reasons,Does.Contain("shake_batch_missing"));
        }

        [TestCase("no_ice")] [TestCase("open_lid")] [TestCase("no_motion")] [TestCase("too_short")]
        public void RequiredShakerStateAndActualMotionCannotBeSkipped(string missing)
        {
            var s=Session(); Fill(s,ice:missing!="no_ice");
            if(missing=="open_lid")
            { s.BeginShake(1); for(int n=0;n<8;n++) s.MoveShake(n%2==0 ? -1 : 1,500000); s.EndShake(); }
            else if(missing!="no_motion") ShakeDomainTests.Motion(s,crossings:missing=="too_short" ? 2 : 8);
            s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,90m,BrewAction.Strain); var result=Outcome(s); Recognized(result);
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Weak)); Assert.That(result.QualityGood,Is.False);
            Assert.That(result.Reasons,Does.Contain("shake_batch_missing"));
        }

        [Test] public void ClosedLidRejectsDirectAndHeldStrainWithoutChangingLiquid()
        {
            var s=Shaken(); s.SetShakerClosed(true);
            Assert.Throws<InvalidOperationException>(()=>s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,90m,BrewAction.Strain));
            Assert.Throws<InvalidOperationException>(()=>s.BeginTransfer(ContainerRole.Shaker,ContainerRole.Serving,BrewAction.Strain));
            Assert.That(s.Container(ContainerRole.Shaker).TotalMl,Is.EqualTo(90m)); Assert.That(s.Freeze().TotalMl,Is.Zero);
            Assert.That(s.FlowActive,Is.False);
        }

        [Test] public void ServingIceFailsLastWordPreparation()
        {
            var s=Strained(); s.AddIce(ContainerRole.Serving); var result=Outcome(s); Recognized(result);
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Weak)); Assert.That(result.Reasons,Does.Contain("serving_ice_unexpected"));
        }

        [Test] public void PoorShakeRhythmDoesNotChangeRecognitionPreparationOrGuestFit()
        {
            var result=Outcome(Strained(1)); Recognized(result);
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Good)); Assert.That(result.QualityGood,Is.False); Assert.That(result.Fits,Is.True);
        }

        [TestCase(false)] [TestCase(true)]
        public void UnfittingRequestCannotChangeActualLastWordRecognitionOrPreparation(bool exact)
        {
            var result=Outcome(Strained(),exact ? DrinkRequest.Specific("cocktail_french_75") : new DrinkRequest("warmth")); Recognized(result);
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Good)); Assert.That(result.QualityGood,Is.True); Assert.That(result.Fits,Is.False);
        }

        [Test] public void DifferentShakenBatchesCannotMergeTechniqueEvidence()
        {
            var s=Session(); Fill(s,.5m); ShakeDomainTests.Motion(s); s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,45m,BrewAction.Strain);
            Fill(s,.5m); ShakeDomainTests.Motion(s); s.Transfer(ContainerRole.Shaker,ContainerRole.Serving,45m,BrewAction.Strain);
            var result=Outcome(s); Recognized(result); Assert.That(s.Freeze().Shake,Is.Null);
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Weak)); Assert.That(result.QualityGood,Is.False);
        }

        [TestCase(false)] [TestCase(true)]
        public void RestoredStrainedFlagAndShakeCannotReplaceAcceptedTransferEvidence(bool emptyTransfer)
        {
            var source=Strained().Freeze();
            var trace=source.Trace.Where(x=>x.Target!=ContainerRole.Serving).ToList();
            if(emptyTransfer) trace.Add(new PreparationAction(trace.Count+1,BrewAction.Strain,"shaker",ContainerRole.Serving,0m,90m,1,null,0));
            var restored=new ServiceSnapshot(source.AttemptId,source.TuningRevision,source.Amounts.ToDictionary(x=>x.Key,x=>x.Value),
                new Dictionary<string,decimal>(),false,true,false,false,0,0,Array.Empty<string>(),null,null,trace,source.Shake);
            var result=ServiceEvaluator.Evaluate(Catalog(),BartendingDomainTests.Tuning(),restored,new DrinkRequest("clarity")); Recognized(result);
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Weak)); Assert.That(result.Reasons,Does.Contain("shaker_strain_missing"));
        }
    }
}
