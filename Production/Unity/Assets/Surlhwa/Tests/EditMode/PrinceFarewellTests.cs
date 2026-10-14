using System;
using NUnit.Framework;
using Surlhwa.Domain;

namespace Surlhwa.Tests
{
    public sealed class PrinceFarewellTests
    {
        private static PrinceServiceRecord Visit(PrinceVisit visit,bool recognized,bool fits,bool perfect=false)
        {
            var recipe=recognized ? BartendingDomainTests.Catalog().Recipes["recipe_test_stir"] : null;
            return new PrinceServiceRecord(visit,"visit_"+visit.ToString().ToLowerInvariant(),new ServiceOutcome(
                recognized ? DrinkRecognition.Recognized : DrinkRecognition.Unknown,recipe,
                recognized ? (perfect ? PreparationResult.Good : PreparationResult.Weak) : PreparationResult.NotEvaluable,
                perfect,recognized && fits,new System.Collections.Generic.List<string>()));
        }
        [Test] public void FailedFirstVisitAndImperfectLaterTechniqueCanStillLeadToOpenFarewell()
        { Assert.That(PrinceFarewellPolicy.Select(new[]{Visit(PrinceVisit.First,false,false),Visit(PrinceVisit.Warmth,true,true),Visit(PrinceVisit.Clarity,true,true)}),Is.EqualTo(PrinceFarewell.Open)); }
        [Test] public void FirstVisitCannotSubstituteForMissingWarmthUnderstanding()
        { Assert.That(PrinceFarewellPolicy.Select(new[]{Visit(PrinceVisit.First,true,true,true),Visit(PrinceVisit.Warmth,true,false,true),Visit(PrinceVisit.Clarity,true,true,true)}),Is.EqualTo(PrinceFarewell.Quiet)); }
        [Test] public void MissingHistoryCannotInventUnderstanding()
        { Assert.That(PrinceFarewellPolicy.Select(new[]{Visit(PrinceVisit.Clarity,true,true,true)}),Is.EqualTo(PrinceFarewell.Quiet)); }
        [Test] public void BothRecognizedLaterServicesAreRequired()
        { Assert.That(PrinceFarewellPolicy.Select(new[]{Visit(PrinceVisit.Warmth,false,false),Visit(PrinceVisit.Clarity,true,true)}),Is.EqualTo(PrinceFarewell.Quiet)); }
        [Test] public void DuplicatePrinceVisitsCannotRewriteFailedService()
        { Assert.Throws<ArgumentException>(()=>PrinceFarewellPolicy.Select(new[]{Visit(PrinceVisit.Warmth,true,false),Visit(PrinceVisit.Warmth,true,true),Visit(PrinceVisit.Clarity,true,true)})); }
    }
}
