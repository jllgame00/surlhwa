using System;
namespace Surlhwa.Application
{
    public enum CameraVariant { OrthographicA, PerspectiveB }
    public enum ReviewFixture { Empty, OrdinarySeated, QuixoteSeated, Composition, GroundingInspection, Standing }
    // Review-only orchestration. This owns no campaign, persistence or guest lifecycle facts.
    public sealed class ReviewSession
    {
        public CameraVariant Camera { get; private set; }
        public ReviewFixture Fixture { get; private set; }
        public void Select(CameraVariant camera, ReviewFixture fixture)
        {
            if (!Enum.IsDefined(typeof(CameraVariant), camera)) throw new ArgumentOutOfRangeException(nameof(camera));
            if (!Enum.IsDefined(typeof(ReviewFixture), fixture)) throw new ArgumentOutOfRangeException(nameof(fixture));
            Camera = camera; Fixture = fixture;
        }
    }
}
