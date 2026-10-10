using System;
using System.Linq;
using UnityEngine;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Domain;
namespace Surlhwa.Presentation
{
    public sealed class ReviewStage : MonoBehaviour
    {
        public Camera Orthographic;
        public Camera Perspective;
        public CharacterView Bartender;
        public CharacterView Ordinary;
        public CharacterView Quixote;
        public TextAsset ContentBundle;
        public Transform WorkbenchAnchor;
        public Transform MemoryDisplayAnchor;
        private readonly ReviewSession session = new ReviewSession();
        private bool automaticReview;
        public Camera ActiveCamera => session.Camera == CameraVariant.OrthographicA ? Orthographic : Perspective;
        public ReviewFixture Fixture => session.Fixture;
        public CameraVariant Variant => session.Camera;
        private void Awake()
        {
            automaticReview = Environment.GetCommandLineArgs().Contains("-reviewCapture");
            var content = SpatialProofLoader.Load(ContentBundle.text);
            foreach (var actor in new[] { Bartender, Ordinary, Quixote })
            {
                var definition = content.Guests[new StableId<GuestIdentity>(actor.GuestId)];
                if (definition.VisualBindingId.Value != actor.VisualBindingId)
                    throw new InvalidOperationException("Authored guest/visual binding mismatch: " + actor.GuestId);
            }
            Select(CameraVariant.OrthographicA, ReviewFixture.Composition);
        }
        public void Select(CameraVariant camera, ReviewFixture fixture)
        {
            session.Select(camera, fixture);
            bool inspection = fixture == ReviewFixture.GroundingInspection;
            Orthographic.orthographicSize = inspection ? 1.85f : 3.3f;
            Perspective.fieldOfView = 2 * Mathf.Atan((inspection ? 1.85f : 3.3f) / 17f) * Mathf.Rad2Deg;
            Vector3 target = inspection ? new Vector3(-.4f, .95f, .1f) : new Vector3(0, 1.2f, .7f);
            Orthographic.transform.position = Perspective.transform.position = target - Orthographic.transform.forward * 17f;
            Orthographic.enabled = camera == CameraVariant.OrthographicA;
            Perspective.enabled = camera == CameraVariant.PerspectiveB;
            bool composition = fixture == ReviewFixture.Composition || fixture == ReviewFixture.GroundingInspection;
            bool standing = fixture == ReviewFixture.Standing;
            Bartender.Show(composition || standing, false);
            Ordinary.Show(fixture == ReviewFixture.OrdinarySeated || standing, !standing);
            Quixote.Show(fixture == ReviewFixture.QuixoteSeated || composition, true);
        }
        private void OnGUI()
        {
            if (automaticReview) return;
            GUILayout.BeginArea(new Rect(12, 12, 235, 260), GUI.skin.box);
            GUILayout.Label("SURLHWA / HG01–HG03 review");
            if (GUILayout.Button("A · Orthographic")) Select(CameraVariant.OrthographicA, session.Fixture);
            if (GUILayout.Button("B · Restrained perspective")) Select(CameraVariant.PerspectiveB, session.Fixture);
            foreach (ReviewFixture fixture in Enum.GetValues(typeof(ReviewFixture)))
                if (GUILayout.Button(fixture.ToString())) Select(session.Camera, fixture);
            GUILayout.Label("ARCH_SLICE_PROVISIONAL");
            GUILayout.EndArea();
        }
    }
}
