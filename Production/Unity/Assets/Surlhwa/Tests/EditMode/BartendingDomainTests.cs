using System;
using System.Collections.Generic;
using NUnit.Framework;
using Surlhwa.Domain;

namespace Surlhwa.Tests
{
    public sealed class BartendingDomainTests
    {
        // Literal fixtures independently transcribed from Architecture 20/40.
        internal static BrewingTuning Tuning() => new BrewingTuning("stir_slice_v1", 60m, 300m, 180m,
            30m, 5m, 40m, 8m, 1m, 360, 1080, 60, 300, .75);
        internal static BartendingCatalog Catalog() => new BartendingCatalog(
            new[] { "whiskey", "vermouth", "water", "rye_whiskey", "sweet_red_vermouth", "angostura_bitters", "gin", "dry_vermouth" },
            new[] {
                new CocktailDefinition("cocktail_seolya", "설야", new[] { "warmth", "softness" }),
                new CocktailDefinition("cocktail_manhattan", "Manhattan", new[] { "warmth", "bitterness" }),
                new CocktailDefinition("cocktail_dry_martini", "Dry Martini", new[] { "clarity" }) },
            new[] {
                new StirRecipe("recipe_test_stir", "cocktail_seolya", new Dictionary<string, decimal> { ["whiskey"]=59.14706m, ["vermouth"]=29.57353m, ["water"]=29.57353m }),
                new StirRecipe("recipe_manhattan", "cocktail_manhattan", new Dictionary<string, decimal> { ["rye_whiskey"]=50m, ["sweet_red_vermouth"]=20m, ["angostura_bitters"]=1m }, "angostura_bitters", "cherry"),
                new StirRecipe("recipe_dry_martini", "cocktail_dry_martini", new Dictionary<string, decimal> { ["gin"]=60m, ["dry_vermouth"]=10m }) });
        internal static BrewingSession Session() { var c=Catalog(); return new BrewingSession(c,Tuning(),c.Ingredients); }
        internal static void Circles(BrewingSession s, int segments=72, long micros=83333)
        {
            s.BeginStir(1,0);
            for(int n=1;n<=segments;n++) { double a=n*Math.PI/12; s.MoveStir(Math.Cos(a),Math.Sin(a),micros); }
            s.EndStir();
        }
        internal static BrewingSession Mixed(string recipe, bool dash=true, bool cherry=true, int segments=72)
        {
            var s=Session();
            foreach(var x in Catalog().Recipes[recipe].Amounts)
                s.Pour(x.Key,ContainerRole.Mixing,x.Value,x.Key=="angostura_bitters" && dash ? BrewAction.Dash : BrewAction.Pour);
            s.AddIce(ContainerRole.Mixing); Circles(s,segments);
            s.Transfer(ContainerRole.Mixing,ContainerRole.Serving,300m,BrewAction.Strain);
            if(recipe=="recipe_manhattan" && cherry) s.Garnish("cherry","in_glass");
            return s;
        }
        [Test] public void ContinuousFlowEvidenceIsBoundedWithoutLosingActualLiquidOrTime()
        {
            var s=Session(); s.BeginPour("gin",ContainerRole.Mixing);
            for(int i=0;i<10000;i++) s.UpdateFlow(100,1m,false);
            s.EndFlow(); s.BeginPour("water",ContainerRole.Mixing); s.UpdateFlow(100000,1m,false); s.EndFlow();
            var trace=s.Freeze().Trace; Assert.That(trace.Count,Is.EqualTo(2));
            Assert.That(trace[0].AcceptedMl,Is.EqualTo(30m)); Assert.That(trace[0].AcceptedByIngredient["gin"],Is.EqualTo(30m));
            Assert.That(trace[0].ElapsedMicroseconds,Is.EqualTo(1000000)); Assert.That(trace[1].AcceptedMl,Is.EqualTo(3m));
            Assert.That(s.Container(ContainerRole.Mixing).TotalMl,Is.EqualTo(33m));
        }
        [Test] public void CoalescedOverflowPreservesSpill_AndFineChangesRemainSeparate()
        {
            var s=Session(); s.Pour("gin",ContainerRole.Mixing,290); s.BeginPour("water",ContainerRole.Mixing);
            s.UpdateFlow(1000000,1m,false); s.UpdateFlow(1000000,1m,false); s.UpdateFlow(100000,1m,true); s.EndFlow();
            var trace=s.Freeze().Trace; Assert.That(trace.Count,Is.EqualTo(3));
            Assert.That(trace[1].AcceptedMl,Is.EqualTo(10m)); Assert.That(trace[1].SpilledMl,Is.EqualTo(50m));
            Assert.That(trace[1].ElapsedMicroseconds,Is.EqualTo(2000000)); Assert.That(trace[2].SpilledMl,Is.EqualTo(.5m));
        }
        [Test] public void FiniteJiggerOverflowConservesAllLiquid()
        {
            var s=Session(); s.Pour("gin",ContainerRole.Jigger,60m);
            s.Pour("water",ContainerRole.Serving,173m);
            var moved=s.Transfer(ContainerRole.Jigger,ContainerRole.Serving,10m,BrewAction.PourFromContainer);
            Assert.That(s.Container(ContainerRole.Jigger).TotalMl,Is.EqualTo(50m));
            Assert.That(s.Container(ContainerRole.Serving).TotalMl,Is.EqualTo(180m));
            Assert.That(moved.AcceptedMl,Is.EqualTo(7m)); Assert.That(moved.SpilledMl,Is.EqualTo(3m));
        }
        [Test] public void PartialMixedTransferPreservesIngredientProportions()
        {
            var s=Session(); s.Pour("gin",ContainerRole.Jigger,40m); s.Pour("water",ContainerRole.Jigger,20m);
            s.Transfer(ContainerRole.Jigger,ContainerRole.Mixing,15m,BrewAction.PourFromContainer);
            Assert.That(s.Container(ContainerRole.Mixing).Amounts["gin"],Is.EqualTo(10m));
            Assert.That(s.Container(ContainerRole.Mixing).Amounts["water"],Is.EqualTo(5m));
            Assert.That(s.Container(ContainerRole.Jigger).TotalMl,Is.EqualTo(45m));
        }
        [Test] public void TransferTraceRecordsAcceptedComponentsRatherThanRequestedMl()
        {
            var s=Session(); s.Pour("gin",ContainerRole.Jigger,40m); s.Pour("water",ContainerRole.Jigger,20m);
            s.Pour("water",ContainerRole.Serving,177m);
            s.Transfer(ContainerRole.Jigger,ContainerRole.Serving,15m,BrewAction.PourFromContainer);
            var trace=s.Freeze().Trace; var last=trace[trace.Count-1];
            Assert.That(last.AcceptedByIngredient["gin"],Is.EqualTo(2m));
            Assert.That(last.AcceptedByIngredient["water"],Is.EqualTo(1m));
            Assert.That(last.SpilledMl,Is.EqualTo(12m));
        }
        [Test] public void FineFlowPartitionKeepsSubMicroLitreRemainder()
        {
            var a=Session(); var b=Session(); a.BeginPour("gin",ContainerRole.Mixing); b.BeginPour("gin",ContainerRole.Mixing);
            for(int n=0;n<1000;n++) a.UpdateFlow(1,.333333m,true);
            b.UpdateFlow(1000,.333333m,true);
            Assert.That(a.Container(ContainerRole.Mixing).TotalMl,Is.EqualTo(.001666m));
            Assert.That(a.Container(ContainerRole.Mixing).TotalMl,Is.EqualTo(b.Container(ContainerRole.Mixing).TotalMl));
            a.EndFlow(); a.UpdateFlow(1000000,1m,false);
            Assert.That(a.Container(ContainerRole.Mixing).TotalMl,Is.EqualTo(.001666m));
        }
        [Test] public void InvalidOrUnavailableSupplyNeverMutatesLiquid()
        {
            var s=new BrewingSession(Catalog(),Tuning(),new[] { "water" });
            Assert.Throws<InvalidOperationException>(()=>s.Pour("gin",ContainerRole.Mixing,10m));
            Assert.Throws<ArgumentOutOfRangeException>(()=>s.Pour("water",ContainerRole.Mixing,-1m));
            Assert.Throws<ArgumentException>(()=>s.Pour("water",ContainerRole.Mixing,.0000001m));
            Assert.That(s.Container(ContainerRole.Mixing).TotalMl,Is.Zero);
            Assert.That(s.Transfer(ContainerRole.Jigger,ContainerRole.Serving,10m,BrewAction.PourFromContainer).AcceptedMl,Is.Zero);
        }
        [TestCase("recipe_test_stir", "cocktail_seolya", true)]
        [TestCase("recipe_manhattan", "cocktail_manhattan", true)]
        [TestCase("recipe_dry_martini", "cocktail_dry_martini", false)]
        public void RealServingCompositionHasIndependentWhatAndWarmthFit(string recipe,string cocktail,bool fits)
        {
            var result=ServiceEvaluator.Evaluate(Catalog(),Tuning(),Mixed(recipe).Freeze(),new DrinkRequest("warmth"));
            Assert.That(result.CocktailId,Is.EqualTo(cocktail)); Assert.That(result.Fits,Is.EqualTo(fits));
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Good)); Assert.That(result.QualityGood,Is.True);
        }
        [Test] public void SameManhattanMlWithPourInsteadOfDashOnlyChangesHow()
        {
            var good=ServiceEvaluator.Evaluate(Catalog(),Tuning(),Mixed("recipe_manhattan").Freeze(),new DrinkRequest("warmth"));
            var bad=ServiceEvaluator.Evaluate(Catalog(),Tuning(),Mixed("recipe_manhattan",false).Freeze(),new DrinkRequest("warmth"));
            Assert.That(bad.CocktailId,Is.EqualTo("cocktail_manhattan")); Assert.That(bad.Fits,Is.True);
            Assert.That(bad.QualityGood,Is.True); Assert.That(bad.Preparation,Is.EqualTo(PreparationResult.Weak));
            Assert.That(good.Preparation,Is.EqualTo(PreparationResult.Good));
        }
        [Test] public void RequiredCherryAndOptionalDryGarnishHaveDifferentContracts()
        {
            var noCherry=ServiceEvaluator.Evaluate(Catalog(),Tuning(),Mixed("recipe_manhattan",true,false).Freeze(),new DrinkRequest("warmth"));
            var dry=ServiceEvaluator.Evaluate(Catalog(),Tuning(),Mixed("recipe_dry_martini").Freeze(),new DrinkRequest("warmth"));
            Assert.That(noCherry.Preparation,Is.EqualTo(PreparationResult.Weak));
            Assert.That(dry.Preparation,Is.EqualTo(PreparationResult.Good));
        }
        [Test] public void WeakActualStirDoesNotChangeIdentityOrFit()
        {
            var r=ServiceEvaluator.Evaluate(Catalog(),Tuning(),Mixed("recipe_test_stir",segments:24).Freeze(),new DrinkRequest("warmth"));
            Assert.That(r.CocktailId,Is.EqualTo("cocktail_seolya")); Assert.That(r.Fits,Is.True);
            Assert.That(r.Preparation,Is.EqualTo(PreparationResult.Good)); Assert.That(r.QualityGood,Is.False);
        }
        [Test] public void StirringEmptyOrOffCircleDoesNotEarnTechnique()
        {
            var s=Session(); Circles(s);
            s.Pour("gin",ContainerRole.Mixing,60m); s.Pour("dry_vermouth",ContainerRole.Mixing,10m); s.AddIce(ContainerRole.Mixing);
            s.BeginStir(.01,0); for(int n=0;n<100;n++) s.MoveStir(.01,0,100000); s.EndStir();
            s.Transfer(ContainerRole.Mixing,ContainerRole.Serving,70m,BrewAction.Strain);
            var r=ServiceEvaluator.Evaluate(Catalog(),Tuning(),s.Freeze(),new DrinkRequest("clarity"));
            Assert.That(r.QualityGood,Is.False); Assert.That(r.Preparation,Is.EqualTo(PreparationResult.Weak));
        }
        [Test] public void AddedLiquidInvalidatesPriorBatchTechnique()
        {
            var s=Session(); s.Pour("gin",ContainerRole.Mixing,60m); s.AddIce(ContainerRole.Mixing); Circles(s);
            s.Pour("dry_vermouth",ContainerRole.Mixing,10m); s.Transfer(ContainerRole.Mixing,ContainerRole.Serving,70m,BrewAction.Strain);
            var r=ServiceEvaluator.Evaluate(Catalog(),Tuning(),s.Freeze(),new DrinkRequest("clarity"));
            Assert.That(r.CocktailId,Is.EqualTo("cocktail_dry_martini")); Assert.That(r.QualityGood,Is.False);
        }
        [Test] public void RegrippingSpoonKeepsAcceptedMotionForUnchangedBatch()
        {
            var s=Session(); s.Pour("gin",ContainerRole.Mixing,60m); s.Pour("dry_vermouth",ContainerRole.Mixing,10m); s.AddIce(ContainerRole.Mixing);
            Circles(s,24); Circles(s,24); Circles(s,24);
            s.Transfer(ContainerRole.Mixing,ContainerRole.Serving,70m,BrewAction.Strain);
            var r=ServiceEvaluator.Evaluate(Catalog(),Tuning(),s.Freeze(),new DrinkRequest("clarity"));
            Assert.That(r.QualityGood,Is.True,"Three real rotations across releases still stir the same liquid batch.");
        }
        [Test] public void RepeatedDirectionReversalCannotEarnStableQuality()
        {
            var s=Session(); s.Pour("gin",ContainerRole.Mixing,60m); s.Pour("dry_vermouth",ContainerRole.Mixing,10m); s.AddIce(ContainerRole.Mixing);
            s.BeginStir(1,0);
            for(int n=1;n<=144;n++) { double a=(n%2)*Math.PI/12; s.MoveStir(Math.Cos(a),Math.Sin(a),83333); }
            s.EndStir(); s.Transfer(ContainerRole.Mixing,ContainerRole.Serving,70m,BrewAction.Strain);
            Assert.That(ServiceEvaluator.Evaluate(Catalog(),Tuning(),s.Freeze(),new DrinkRequest("clarity")).QualityGood,Is.False);
        }
        [Test] public void EquivalentRecognitionCandidatesStayAmbiguous()
        {
            var first=Catalog(); var recipes=new List<StirRecipe>(first.Recipes.Values);
            recipes.Add(new StirRecipe("recipe_duplicate_dry","cocktail_seolya",new Dictionary<string,decimal>{["gin"]=60m,["dry_vermouth"]=10m}));
            var catalog=new BartendingCatalog(first.Ingredients,first.Cocktails.Values,recipes);
            var r=ServiceEvaluator.Evaluate(catalog,Tuning(),Mixed("recipe_dry_martini").Freeze(),new DrinkRequest("warmth"));
            Assert.That(r.Recognition,Is.EqualTo(DrinkRecognition.Ambiguous)); Assert.That(r.CocktailId,Is.Null); Assert.That(r.Fits,Is.False);
        }
        [Test] public void PartialTransferKeepsEvidenceWithoutCopyingLiquid()
        {
            var s=Session(); s.Pour("gin",ContainerRole.Mixing,120m); s.Pour("dry_vermouth",ContainerRole.Mixing,20m);
            s.AddIce(ContainerRole.Mixing); Circles(s); s.Transfer(ContainerRole.Mixing,ContainerRole.Serving,70m,BrewAction.Strain);
            var r=ServiceEvaluator.Evaluate(Catalog(),Tuning(),s.Freeze(),new DrinkRequest("clarity"));
            Assert.That(s.Container(ContainerRole.Mixing).TotalMl,Is.EqualTo(70m));
            Assert.That(r.CocktailId,Is.EqualTo("cocktail_dry_martini")); Assert.That(r.QualityGood,Is.True);
            Assert.That(s.Container(ContainerRole.Serving).HasIce,Is.False);
        }
        [TestCase("whiskey",50)] [TestCase("gin",1)]
        public void WrongIdentityOrUnderfilledGlassIsNotRequestedCocktail(string ingredient,int ml)
        {
            var s=Session(); s.Pour(ingredient,ContainerRole.Serving,ml);
            var r=ServiceEvaluator.Evaluate(Catalog(),Tuning(),s.Freeze(),new DrinkRequest("warmth"));
            Assert.That(r.CocktailId,Is.Null); Assert.That(r.Fits,Is.False);
            Assert.That(r.Preparation,Is.EqualTo(PreparationResult.NotEvaluable));
        }
        [Test] public void ServiceSnapshotCannotFollowLaterAttemptMutations()
        {
            var s=Mixed("recipe_dry_martini"); var snap=s.Freeze();
            s.Pour("water",ContainerRole.Serving,20m);
            Assert.That(snap.Amounts.ContainsKey("water"),Is.False); Assert.That(snap.TotalMl,Is.EqualTo(70m));
            Assert.Throws<NotSupportedException>(()=>((IDictionary<string,decimal>)snap.Amounts).Add("water",1m));
        }
        [Test] public void CompletePartialFactsNeverAutoPromoteButActualDiscoveryDoes()
        {
            var k=new RecipeKnowledge(Catalog());
            k.GrantFact("recipe_dry_martini","amount:gin"); k.GrantFact("recipe_dry_martini","amount:dry_vermouth");
            k.GrantFact("recipe_dry_martini","method"); k.GrantFact("recipe_dry_martini","preparation");
            var before=k.Project("recipe_dry_martini"); Assert.That(before.Name,Is.Null); Assert.That(before.Known,Is.False);
            Assert.That(before.Ingredients,Does.Contain("gin"));
            var r=ServiceEvaluator.Evaluate(Catalog(),Tuning(),Mixed("recipe_dry_martini").Freeze(),new DrinkRequest("warmth"));
            Assert.That(k.Discover(r,"occurrence_1"),Is.True); Assert.That(k.Discover(r,"occurrence_1"),Is.False);
            Assert.That(k.Project("recipe_dry_martini").Name,Is.EqualTo("Dry Martini")); Assert.That(r.Fits,Is.False);
        }
    }
}
