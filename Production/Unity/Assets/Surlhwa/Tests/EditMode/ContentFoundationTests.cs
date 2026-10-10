using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Surlhwa.Domain;
using Surlhwa.ContentIO;

namespace Surlhwa.Tests
{
    public sealed class ContentFoundationTests
    {
        public const string Valid = @"{
          ""schemaVersion"":1,""contentRevision"":""spatial_proof_v1"",""mode"":""spatial_proof"",
          ""visualBindings"":[{""id"":""visual_ordinary""}],
          ""texts"":[{""id"":""text_postman"",""value"":""우편 배달부 · 눈이 오는 밤""}],
          ""guests"":[{""id"":""guest_ordinary_postman"",""kind"":""ordinary"",
          ""displayTextId"":""text_postman"",""visualBindingId"":""visual_ordinary""}]}";

        [Test]
        public void TypedLoadingPreservesKoreanAndKind()
        {
            var content = SpatialProofLoader.Load(Valid);
            var guest = content.Guests[new StableId<GuestIdentity>("guest_ordinary_postman")];
            Assert.That(guest.Kind, Is.EqualTo(GuestKind.Ordinary));
            Assert.That(content.Texts[guest.DisplayTextId], Is.EqualTo("우편 배달부 · 눈이 오는 밤"));
        }

        [Test]
        public void RoundTripPreservesUnicodeAndStableReferences()
        {
            var first = SpatialProofLoader.Load(Valid);
            var second = SpatialProofLoader.Load(SpatialProofLoader.Write(first));
            Assert.That(second.Texts.Values.Single(), Is.EqualTo("우편 배달부 · 눈이 오는 밤"));
            Assert.That(second.Guests.Values.Single().VisualBindingId.Value, Is.EqualTo("visual_ordinary"));
        }

        [TestCase("Guest_quixote")]
        [TestCase("guest-quixote")]
        [TestCase("")]
        [TestCase("손님")]
        public void StableIdRejectsInvalidSpelling(string value)
        {
            Assert.Throws<ArgumentException>(() => new StableId<GuestIdentity>(value));
        }

        [Test]
        public void StableIdsUseValueEquality()
        {
            var items = new Dictionary<StableId<GuestIdentity>, string>();
            items.Add(new StableId<GuestIdentity>("guest_quixote"), "first");
            Assert.That(items[new StableId<GuestIdentity>("guest_quixote")], Is.EqualTo("first"));
        }

        [TestCase("\"schemaVersion\":1", "\"schemaVersion\":2")]
        [TestCase("\"schemaVersion\":1", "\"schemaVersion\":1.5")]
        [TestCase("\"schemaVersion\":1", "\"schemaVersion\":\"1\"")]
        [TestCase("\"schemaVersion\":1,", "")]
        [TestCase("\"schemaVersion\":1", "\"schemaVersion\":1,\"typo\":true")]
        [TestCase("\"mode\":\"spatial_proof\"", "\"mode\":\"campaign\"")]
        [TestCase("\"kind\":\"ordinary\"", "\"kind\":\"unknown\"")]
        [TestCase("\"visualBindingId\":\"visual_ordinary\"", "\"visualBindingId\":\"visual_missing\"")]
        [TestCase("\"displayTextId\":\"text_postman\"", "\"displayTextId\":\"text_missing\"")]
        [TestCase("\"id\":\"guest_ordinary_postman\"", "\"id\":\"Guest_Postman\"")]
        [TestCase("\"id\":\"visual_ordinary\"}", "\"id\":\"visual_ordinary\",\"extra\":0}")]
        [TestCase("\"schemaVersion\":1", "\"schemaVersion\":1,\"schemaVersion\":1")]
        [TestCase("\"value\":\"우편 배달부 · 눈이 오는 밤\"", "\"value\":null")]
        public void InvalidContentFailsAtLoad(string oldText, string newText)
        {
            Assert.Throws<ContentValidationException>(() => SpatialProofLoader.Load(Valid.Replace(oldText, newText)));
        }

        [Test]
        public void DuplicateIdFailsBeforeRegistryPublication()
        {
            string invalid = Valid.Replace("{\"id\":\"visual_ordinary\"}", "{\"id\":\"visual_ordinary\"},{\"id\":\"visual_ordinary\"}");
            Assert.Throws<ContentValidationException>(() => SpatialProofLoader.Load(invalid));
        }

        [Test]
        public void TrailingJsonIsRejected()
        {
            Assert.Throws<ContentValidationException>(() => SpatialProofLoader.Load(Valid + "{}"));
        }

        [Test]
        public void PublishedCollectionsCannotBeMutated()
        {
            var content = SpatialProofLoader.Load(Valid);
            var view = (IDictionary<StableId<TextIdentity>, string>)content.Texts;
            Assert.Throws<NotSupportedException>(() => view.Clear());
            Assert.That(content.Texts.Count, Is.EqualTo(1));
        }
    }
}

