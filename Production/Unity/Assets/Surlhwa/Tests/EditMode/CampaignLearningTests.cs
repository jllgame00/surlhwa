using System;
using NUnit.Framework;
using Surlhwa.Domain;

namespace Surlhwa.Tests
{
    public sealed class CampaignLearningTests
    {
        [Test] public void SpecificRequestUsesRecognizedIdentityRatherThanSharedMoodTag()
        {
            var c=BartendingDomainTests.Catalog(); var s=BartendingDomainTests.Mixed("recipe_test_stir").Freeze();
            Assert.That(ServiceEvaluator.Evaluate(c,BartendingDomainTests.Tuning(),s,DrinkRequest.Specific("cocktail_manhattan")).Fits,Is.False);
            Assert.That(ServiceEvaluator.Evaluate(c,BartendingDomainTests.Tuning(),s,DrinkRequest.Specific("cocktail_seolya")).Fits,Is.True);
        }
        [Test] public void RecommendationAnyAllAndExclusionsRemainIndependent()
        {
            var c=BartendingDomainTests.Catalog(); var s=BartendingDomainTests.Mixed("recipe_test_stir").Freeze();
            var t=BartendingDomainTests.Tuning();
            Assert.That(ServiceEvaluator.Evaluate(c,t,s,DrinkRequest.Recommendation(new[]{"clarity","warmth"})).Fits,Is.True);
            Assert.That(ServiceEvaluator.Evaluate(c,t,s,DrinkRequest.Recommendation(new[]{"warmth"},new[]{"softness"})).Fits,Is.True);
            Assert.That(ServiceEvaluator.Evaluate(c,t,s,DrinkRequest.Recommendation(new[]{"warmth"},new[]{"bitterness"})).Fits,Is.False);
            Assert.That(ServiceEvaluator.Evaluate(c,t,s,DrinkRequest.Recommendation(new[]{"warmth"},excluded:new[]{"softness"})).Fits,Is.False);
        }
        [Test] public void RequiredDiscoveryCannotLearnNonTargetOrWeakDrinkButRetainsFacts()
        {
            var c=BartendingDomainTests.Catalog(); var k=new RecipeKnowledge(c); k.GrantFact("recipe_manhattan","amount:rye_whiskey");
            var wrong=ServiceEvaluator.Evaluate(c,BartendingDomainTests.Tuning(),BartendingDomainTests.Mixed("recipe_test_stir").Freeze(),new DrinkRequest("warmth"));
            Assert.That(k.DiscoverRequired(wrong,"occ_1","recipe_manhattan"),Is.False);
            Assert.That(k.KnownReceipts.Count,Is.Zero); Assert.That(k.Project("recipe_manhattan").Amounts["rye_whiskey"],Is.EqualTo(50m));
            var weak=ServiceEvaluator.Evaluate(c,BartendingDomainTests.Tuning(),BartendingDomainTests.Mixed("recipe_manhattan",dash:false).Freeze(),new DrinkRequest("warmth"));
            Assert.That(k.DiscoverRequired(weak,"occ_2","recipe_manhattan"),Is.False);
            var good=ServiceEvaluator.Evaluate(c,BartendingDomainTests.Tuning(),BartendingDomainTests.Mixed("recipe_manhattan").Freeze(),new DrinkRequest("warmth"));
            Assert.That(k.DiscoverRequired(good,"occ_3","recipe_manhattan"),Is.True);
            Assert.That(k.KnownReceipts["recipe_manhattan"],Is.EqualTo("service:occ_3"));
        }
        [Test] public void NormalDiscoveryRemainsIndependentOfRequestFit()
        {
            var c=BartendingDomainTests.Catalog(); var k=new RecipeKnowledge(c);
            var outcome=ServiceEvaluator.Evaluate(c,BartendingDomainTests.Tuning(),BartendingDomainTests.Mixed("recipe_test_stir").Freeze(),new DrinkRequest("clarity"));
            Assert.That(k.Discover(outcome,"ordinary_1"),Is.True); Assert.That(outcome.Fits,Is.False);
        }
    }
}
