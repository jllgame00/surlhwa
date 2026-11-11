using UnityEngine;
using UnityEngine.UI;
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
        private Image focusVeil;
        private float revealRemaining;
        private const float RevealSeconds=.22f;

        private void Awake() { ApplyView(false); }
        // Input projection changes immediately. This veil never gates input or progression.
        public void SwitchFocus(bool focused)
        {
            if(focused==Focused) return;
            ApplyView(focused);
            if(focusVeil==null) {
                var veilRoot=new GameObject("Q1 focus reveal",typeof(RectTransform),typeof(Canvas));
                veilRoot.transform.SetParent(transform,false);
                var canvas=veilRoot.GetComponent<Canvas>();
                canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=Owner.Camera;
                canvas.planeDistance=2; canvas.sortingOrder=50;
                var surface=new GameObject("Warm charcoal",typeof(RectTransform),typeof(Image));
                surface.transform.SetParent(veilRoot.transform,false);
                focusVeil=surface.GetComponent<Image>(); focusVeil.raycastTarget=false;
                var rect=focusVeil.rectTransform;
                rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one;
                rect.offsetMin=rect.offsetMax=Vector2.zero;
            }
            revealRemaining=RevealSeconds;
            focusVeil.color=new Color(.035f,.030f,.028f,.58f);
            focusVeil.enabled=true;
        }
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
            if(Owner.Memory!=null) Owner.Memory.SetPresentationFacing(camera.transform.rotation);
        }
        public void ApplyActors()
        {
            foreach(var actor in new[]{Owner.Bartender,Owner.Ordinary,Owner.Prince,Owner.Quixote,Owner.Hamlet,Owner.Faust,Owner.SimCheong,Owner.Odysseus}) {
                if(actor==null || actor.Plane==null || actor.SeatAnchor==null || actor.Renderer==null) continue;
                var plane=actor.Plane.transform;
                bool seated=actor!=Owner.Bartender && actor.Plane.sharedMesh==actor.SeatedMesh;
                float scale=Focused ? 1 : CharacterScale;
                plane.localScale=Vector3.one*scale;
                plane.localRotation=Focused ? FocusAngle : WorldAngle;
                // Scale about the authored seat contact, not the sprite canvas center.
                Vector3 hip=Quaternion.Inverse(FocusAngle)*actor.SeatAnchor.localPosition;
                plane.localPosition=seated ? actor.SeatAnchor.localPosition-plane.localRotation*(hip*scale) : Vector3.zero;
                if(seated) {
                    var material=actor==Owner.Prince ? PrinceSideQuarter : actor.SeatedMaterial!=null && actor.SeatedMaterial.name.StartsWith("OrdinaryCoat") ? OrdinaryBackQuarter : null;
                    actor.Renderer.sharedMaterial=!Focused && material!=null ? material : actor.SeatedMaterial;
                    // Presentation surface thickness; pelvis/root/contact and gameplay path stay fixed.
                    if(!Focused && (material!=null || actor==Owner.Quixote || actor==Owner.Hamlet || actor==Owner.Faust || actor==Owner.SimCheong || actor==Owner.Odysseus)) plane.localPosition-=Quaternion.Inverse(actor.transform.rotation)*Owner.Camera.transform.forward*.14f;
                }
            }
            Owner.Cast?.ApplyView(Focused);
        }
        private void LateUpdate()
        {
            if(CaptureOwnsPose) return;
            bool brewing=Owner.Session?.State.Phase==CampaignPhase.Brewing;
            if(brewing!=Focused) SwitchFocus(brewing);
            ApplyActors();
            ApplyWorkbench();
            if(Owner.Memory!=null) Owner.Memory.SetPresentationFacing(Owner.Camera.transform.rotation);
            if(focusVeil!=null && focusVeil.enabled) {
                revealRemaining=Mathf.Max(0,revealRemaining-Time.unscaledDeltaTime);
                float t=revealRemaining/RevealSeconds;
                focusVeil.color=new Color(.035f,.030f,.028f,.58f*t*t);
                if(revealRemaining==0) focusVeil.enabled=false;
            }
        }
        private void OnDisable() { revealRemaining=0; if(focusVeil!=null) focusVeil.enabled=false; }
        private void ApplyWorkbench()
        {
            if(Owner.Workbench==null) return;
            Owner.Workbench.gameObject.SetActive(true);
            // Only the served-glass plane turns for Q1. The tool root/mixing input anchor never move.
            Owner.Workbench.SetPresentationVisibility(Focused,Owner.Session?.State.CurrentService?.Snapshot!=null,Focused ? FocusAngle : WorldAngle);
        }
    }
}
