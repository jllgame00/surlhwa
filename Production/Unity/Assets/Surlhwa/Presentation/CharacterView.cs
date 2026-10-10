using UnityEngine;
namespace Surlhwa.Presentation
{
    // Fixed authored XY plane: no Update, no camera-facing behavior, no sorting-order override.
    public sealed class CharacterView : MonoBehaviour
    {
        public string GuestId;
        public string VisualBindingId;
        public MeshFilter Plane;
        public MeshRenderer Renderer;
        public Mesh StandingMesh;
        public Mesh SeatedMesh;
        public Material StandingMaterial;
        public Material SeatedMaterial;
        public Transform FeetAnchor;
        public Transform SeatAnchor;
        public Renderer Contact;
        public Mesh[] WalkMeshes;
        public Material[] WalkMaterials;
        public void ShowWalkFrame(int frame)
        {
            Show(true, false);
            Plane.sharedMesh = WalkMeshes[frame % WalkMeshes.Length];
            Renderer.sharedMaterial = WalkMaterials[frame % WalkMaterials.Length];
        }
        public void Show(bool visible, bool seated)
        {
            Plane.sharedMesh = seated ? SeatedMesh : StandingMesh;
            Renderer.sharedMaterial = seated ? SeatedMaterial : StandingMaterial;
            Renderer.enabled = visible;
            Contact.enabled = visible;
        }
    }
}
