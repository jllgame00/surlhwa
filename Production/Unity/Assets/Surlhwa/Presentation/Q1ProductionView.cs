using UnityEngine;
using Surlhwa.Domain;

namespace Surlhwa.Presentation
{
    // Fixed authored Production composition. No campaign state, camera controls or Q2 path.
    [DefaultExecutionOrder(900)]
    public sealed class Q1ProductionView : MonoBehaviour
    {
        public CampaignController Owner;
        public Transform[] FacingProps;
        public Material PrinceSideQuarter, OrdinaryBackQuarter;
        public bool CaptureOwnsPose;
        public const float CharacterScale=1.12f;
        public const float NormalSize=4.85f;
        public static readonly Quaternion WorldAngle=Quaternion.Euler(38,-28,0);
        public static readonly Quaternion FocusAngle=Quaternion.Euler(28,0,0);
        public bool Focused { get; private set; }

        private void Awake() { ApplyView(false); }
        public void ApplyView(bool focused, string framing="Normal")
        {
            Focused=focused;
            var camera=Owner.Camera;
            camera.orthographic=true; camera.aspect=16f/9; camera.nearClipPlane=.1f; camera.farClipPlane=100;
            camera.transform.rotation=focused ? FocusAngle : WorldAngle;
            camera.orthographicSize=focused ? 3.7f : NormalSize;
            camera.transform.position=focused ? new Vector3(0,10.089f,-17.209f) : new Vector3(0,.50f,.65f)-camera.transform.forward*30;
            if(!focused && framing=="Wide") camera.orthographicSize=5.75f;
            if(!focused && framing=="Grounding") {
                camera.orthographicSize=1.65f;
                camera.transform.position=new Vector3(-.4f,.62f,-.6f)-camera.transform.forward*30;
            }
            if(FacingProps!=null) foreach(var t in FacingProps) if(t!=null) t.rotation=camera.transform.rotation;
            ApplyActors();
            ApplyWorkbench();
        }
        public void ApplyActors()
        {
            foreach(var actor in new[]{Owner.Bartender,Owner.Ordinary,Owner.Prince}) {
                var plane=actor.Plane.transform;
                bool seated=actor!=Owner.Bartender && actor.Plane.sharedMesh==actor.SeatedMesh;
                float scale=Focused ? 1 : CharacterScale;
                plane.localScale=Vector3.one*scale;
                plane.localRotation=Focused ? FocusAngle : WorldAngle;
                // Scale about the authored seat contact, not the sprite canvas center.
                Vector3 hip=Quaternion.Inverse(FocusAngle)*actor.SeatAnchor.localPosition;
                plane.localPosition=seated ? actor.SeatAnchor.localPosition-plane.localRotation*(hip*scale) : Vector3.zero;
                if(seated) {
                    var material=actor==Owner.Prince ? PrinceSideQuarter : actor.SeatedMaterial.name.StartsWith("OrdinaryCoat") ? OrdinaryBackQuarter : null;
                    actor.Renderer.sharedMaterial=!Focused && material!=null ? material : actor.SeatedMaterial;
                    // Presentation surface thickness; pelvis/root/contact and gameplay path stay fixed.
                    if(!Focused && material!=null) plane.localPosition-=Quaternion.Inverse(actor.transform.rotation)*Owner.Camera.transform.forward*.14f;
                }
            }
        }
        private void LateUpdate()
        {
            if(CaptureOwnsPose) return;
            bool brewing=Owner.Session?.State.Phase==CampaignPhase.Brewing;
            if(brewing!=Focused) ApplyView(brewing);
            ApplyActors();
            ApplyWorkbench();
        }
        private void ApplyWorkbench()
        {
            if(Owner.Workbench==null) return;
            Owner.Workbench.gameObject.SetActive(true);
            // Only the served-glass plane turns for Q1. The tool root/mixing input anchor never move.
            Owner.Workbench.SetPresentationVisibility(Focused,Owner.Session?.State.CurrentService?.Snapshot!=null,Focused ? FocusAngle : WorldAngle);
        }
    }
}
