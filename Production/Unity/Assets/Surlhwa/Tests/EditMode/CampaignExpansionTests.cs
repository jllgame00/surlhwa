using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Domain;

namespace Surlhwa.Tests
{
    public sealed class CampaignExpansionTests
    {
        // Explicit domain fixture: this is not adopted ACT 2 content or Player evidence.
        private static CampaignContent Content(string companion="companion", bool duplicateGrant=false,bool repeatSlot=false)
        {
            var c=CampaignFlowTests.Content();
            var guests=c.Guests.ToDictionary(x=>x.Key,x=>x.Value);
            guests.Add("companion",new GuestDefinition(new StableId<GuestIdentity>("companion"),GuestKind.Special,
                new StableId<TextIdentity>("intro"),new StableId<VisualIdentity>("visual_mongryong")));
            var encounters=c.Slots.Select(x=>c.Encounters[x.EncounterId]).ToList();
            CampaignEncounter Reunion(string id) => new CampaignEncounter(id,"guest",4,"summary",new DrinkRequest("warmth"),
                new[]{new CampaignLine("intro")},new[]{"farewell"},c.Encounters["enc_0"].Responses.ToDictionary(x=>x.Key,x=>x.Value),
                companionGuestId:companion,memoryId:"mem_02",memoryVariant:"swing_empty_place");
            encounters.Add(Reunion("reunion"));
            if(duplicateGrant) encounters.Add(Reunion("reunion_duplicate"));
            var slots=encounters.Select(x=>new CampaignSlot(x.Id)).ToList();
            if(repeatSlot) slots.Add(new CampaignSlot("reunion"));
            return new CampaignContent(c.Revision,c.BundleHash,c.Catalog,c.Tuning,c.IngredientNames.ToDictionary(x=>x.Key,x=>x.Value),
                c.Texts.ToDictionary(x=>x.Key,x=>x.Value),guests,c.RecipePresentation.ToDictionary(x=>x.Key,x=>x.Value),
                encounters,slots,c.Supply,c.KnownSeeds.ToDictionary(x=>x.Key,x=>x.Value),c.ChoicePrompt,
                c.PrinceChoices.ToDictionary(x=>x.Key,x=>x.Value),c.PrinceResponses.ToDictionary(x=>x.Key,x=>x.Value),
                c.PrinceRecalls.ToDictionary(x=>x.Key,x=>x.Value),c.OpenFarewell,c.QuietFarewell);
        }
        private static CampaignCheckpoint WithMemories(CampaignCheckpoint s,IEnumerable<MemoryReceipt> memories) =>
            new CampaignCheckpoint(s.Sequence,s.RunId,s.SlotIndex,s.Phase,s.LineIndex,s.PrinceChoice,s.Farewell,
                s.SelectedEncounters,s.ConsumedLines,s.CompletedOccurrences,s.Services,memories,
                s.KnownReceipts.ToDictionary(x=>x.Key,x=>x.Value),s.Facts.ToDictionary(x=>x.Key,x=>x.Value.ToArray()));

        private static CampaignSession ReachReunion(CampaignContent c,CampaignFlowTests.Store store)
        {
            var s=CampaignSession.StartAsync(c,store).GetAwaiter().GetResult();
            for(int n=0;n<3;n++) {
                CampaignFlowTests.ReachBrew(s); CampaignFlowTests.Serve(s); CampaignFlowTests.ReachDeparture(s);
                s.TickAsync(2).GetAwaiter().GetResult();
                s=CampaignSession.Continue(c,store,CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(s.State,c),c));
            }
            CampaignFlowTests.ReachBrew(s); return s;
        }
        [Test] public void PairedReunionSharesOneFailedServiceAndEarnsOneMemoryAtDeparture()
        {
            var c=Content(); var store=new CampaignFlowTests.Store(); var s=ReachReunion(c,store);
            Assert.That(s.CurrentEncounter.GuestIds,Is.EqualTo(new[]{"guest","companion"}));
            Assert.That(s.State.Memories.Select(x=>x.Id),Is.EqualTo(new[]{"mem_01"}));
            CampaignFlowTests.Serve(s,null); CampaignFlowTests.ReachDeparture(s);
            Assert.That(s.State.Services.Count,Is.EqualTo(4));
            Assert.That(s.State.CurrentService.Outcome.Recognition,Is.EqualTo(DrinkRecognition.Unknown));
            Assert.That(s.State.Memories.Count,Is.EqualTo(1));
            var restored=CampaignSession.Continue(c,store,CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(s.State,c),c));
            restored.TickAsync(2).GetAwaiter().GetResult();
            Assert.That(restored.State.Memories.Select(x=>x.Id),Is.EqualTo(new[]{"mem_01","mem_02"}));
            Assert.That(restored.State.CompletedOccurrences.Count,Is.EqualTo(4));
            var final=CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(restored.State,c),c);
            restored=CampaignSession.Continue(c,store,final); restored.TickAsync(2).GetAwaiter().GetResult();
            Assert.That(restored.State.Memories.Count,Is.EqualTo(2)); Assert.That(restored.State.Services.Count,Is.EqualTo(4));
        }
        [Test] public void FailedPairedDeparturePublicationRetriesSameMemoryWithoutDuplicateService()
        {
            var c=Content(); var store=new CampaignFlowTests.Store(); var s=ReachReunion(c,store);
            CampaignFlowTests.Serve(s); CampaignFlowTests.ReachDeparture(s); long before=s.State.Sequence;
            store.Fail=true; s.TickAsync(2).GetAwaiter().GetResult();
            Assert.That(s.State.Sequence,Is.EqualTo(before)); Assert.That(s.State.Memories.Count,Is.EqualTo(1));
            Assert.That(s.CanInteract,Is.False); store.Fail=false; s.RetrySaveAsync().GetAwaiter().GetResult();
            Assert.That(s.State.Sequence,Is.EqualTo(before+1)); Assert.That(s.State.Memories.Count,Is.EqualTo(2));
            Assert.That(s.State.Services.Count,Is.EqualTo(4));
            Assert.DoesNotThrow(()=>CampaignSaveCodec.Encode(s.State,c));
        }
        [TestCase("guest")][TestCase("unknown")]
        public void CompanionMustBeDistinctAndResolved(string companion)
        { Assert.Throws<ArgumentException>(()=>Content(companion)); }
        [Test] public void MemoryCannotBeGrantedByTwoAuthoredEncounters()
        { Assert.Throws<ArgumentException>(()=>Content(duplicateGrant:true)); }
        [Test] public void MemoryOwnerCannotBeScheduledTwice()
        { Assert.Throws<ArgumentException>(()=>Content(repeatSlot:true)); }
        [TestCase("early")][TestCase("id")][TestCase("variant")][TestCase("owner")]
        public void ForgedMemoryCannotBeSavedEvenWithValidEnvelope(string fault)
        {
            var c=Content(); var s=ReachReunion(c,new CampaignFlowTests.Store());
            CampaignFlowTests.Serve(s); CampaignFlowTests.ReachDeparture(s);
            if(fault!="early") s.TickAsync(2).GetAwaiter().GetResult();
            var memories=s.State.Memories.Where(x=>x.Id!="mem_02").ToList();
            memories.Add(new MemoryReceipt(fault=="id" ? "mem_03" : "mem_02",fault=="variant" ? "wrong_variant" : "swing_empty_place",
                s.State.RunId+":"+(fault=="owner" ? "0" : "3"),s.State.Sequence));
            Assert.Throws<ContentValidationException>(()=>CampaignSaveCodec.Encode(WithMemories(s.State,memories),c));
        }
    }
}
