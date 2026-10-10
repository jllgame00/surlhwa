using System;
using NUnit.Framework;
using Surlhwa.Application;
namespace Surlhwa.Tests
{
    public sealed class ReviewSessionTests
    {
        [Test]
        public void SelectionKeepsCameraAndFixtureIndependent()
        {
            var session = new ReviewSession();
            session.Select(CameraVariant.PerspectiveB, ReviewFixture.QuixoteSeated);
            Assert.That(session.Camera, Is.EqualTo(CameraVariant.PerspectiveB));
            Assert.That(session.Fixture, Is.EqualTo(ReviewFixture.QuixoteSeated));
            session.Select(CameraVariant.OrthographicA, session.Fixture);
            Assert.That(session.Fixture, Is.EqualTo(ReviewFixture.QuixoteSeated));
        }
        [Test]
        public void InvalidSelectionFailsWithoutPartiallyChangingReviewState()
        {
            var session = new ReviewSession();
            session.Select(CameraVariant.PerspectiveB, ReviewFixture.Standing);
            Assert.Throws<ArgumentOutOfRangeException>(() => session.Select(CameraVariant.OrthographicA, (ReviewFixture)999));
            Assert.That(session.Camera, Is.EqualTo(CameraVariant.PerspectiveB));
            Assert.That(session.Fixture, Is.EqualTo(ReviewFixture.Standing));
        }
    }
}
