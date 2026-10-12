using System;
using System.Linq;
using Surlhwa.ContentIO;
using Surlhwa.Domain;
using UnityEngine;
namespace Surlhwa.Presentation
{
    // Owner-review C only. Authored fixed view; no production UI or gameplay controller.
    public sealed class PixelCutawayStage : MonoBehaviour
    {
        public static readonly string[] Fixtures = { "Empty", "OrdinarySeated", "SpecialSeated", "Composition", "GroundingInspection", "Wide" };
        public Camera Camera;
        public CharacterView Bartender, Ordinary, Quixote;
        public TextAsset ContentBundle;
        public string Revision => name == "C1_Review" ? "C1" : "C";
        public bool IsInitialized { get; private set; }
        public int Fixture { get; private set; }
        private bool automated;
        private void Awake()
        {
            IsInitialized = false;
            automated = Environment.GetCommandLineArgs().Contains("-reviewCapture");
            var content = SpatialProofLoader.Load(ContentBundle.text);
            foreach (var actor in new[] { Bartender, Ordinary, Quixote }) {
                if (content.Guests[new StableId<GuestIdentity>(actor.GuestId)].VisualBindingId.Value != actor.VisualBindingId)
                    throw new InvalidOperationException("C guest/visual binding mismatch: " + actor.GuestId);
            }
            Select(3); IsInitialized = true;
        }
        public void Select(int fixture)
        {
            if (fixture < 0 || fixture >= Fixtures.Length) throw new ArgumentOutOfRangeException(nameof(fixture));
            Fixture = fixture;
            Camera.aspect = 16f / 9;
            bool inspection = fixture == 4, wide = fixture == 5;
            Camera.orthographicSize = inspection ? 1.65f : wide ? (Revision == "C1" ? 4.55f : 4.9f) : (Revision == "C1" ? 3.7f : 4.1f);
            var target = inspection ? new Vector3(-.15f, .65f, -.35f) : new Vector3(0, .7f, .45f);
            Camera.transform.position = target - Camera.transform.forward * 20;
            Bartender.Show(fixture >= 3, false);
            Ordinary.Show(fixture == 1, true);
            Quixote.Show(fixture == 2 || fixture >= 3, true);
        }
        private void OnGUI()
        {
            if (automated || !IsInitialized) return;
            GUILayout.BeginArea(new Rect(12, 12, 235, 240), GUI.skin.box);
            GUILayout.Label(Revision + " / Pixel Cutaway Hybrid");
            for (int i = 0; i < Fixtures.Length; i++) if (GUILayout.Button(Fixtures[i])) Select(i);
            GUILayout.Label(Revision == "C1" ? "HG01/02 approved · HG03 pending" : "HG01–03 · historical C fixture"); GUILayout.EndArea();
        }
    }
}
