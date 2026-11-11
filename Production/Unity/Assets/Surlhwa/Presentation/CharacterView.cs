using UnityEngine;
using Surlhwa.Domain;
namespace Surlhwa.Presentation
{
    // One unsaved presentation sample supplies both the visible pose and contact
    // observation. CampaignSession alone owns the 1.5-second logical boundary.
    public readonly struct CharacterMotionSample
    {
        public readonly Vector3 Position;
        public readonly bool Seated, Walking, Mirrored;
        public readonly int Frame;
        private CharacterMotionSample(Vector3 position,bool seated,bool walking,bool mirrored,int frame)
        { Position=position; Seated=seated; Walking=walking; Mirrored=mirrored; Frame=frame; }

        public static CharacterMotionSample At(CampaignPhase phase,float clock,Vector3 seat,Vector3 entry,Vector3 exit,float departurePause=.18f,float arrivalPause=0)
        {
            clock=Mathf.Max(0,clock);
            if(phase==CampaignPhase.Arriving) {
                float travel=clock-arrivalPause, end=arrivalPause+1.05f;
                float progress=Mathf.SmoothStep(0,1,travel/1.05f);
                // A whole standing frame faces the approach, then turns while its
                // root is already at the seat. Never squash a cutout edge-on.
                return new CharacterMotionSample(Vector3.Lerp(entry,seat,progress),clock>=end+.23f,
                    travel>0 && clock<end,clock<end+.10f,(int)(Mathf.Max(0,travel)*6));
            }
            if(phase==CampaignPhase.Departing) {
                float travel=Mathf.Max(0,clock-departurePause);
                float progress=Mathf.SmoothStep(0,1,travel/(1.48f-departurePause));
                float approachLength=Vector3.Distance(seat,entry), exitLength=Vector3.Distance(entry,exit);
                float distance=progress*(approachLength+exitLength);
                var position=distance<=approachLength ? Vector3.Lerp(seat,entry,distance/Mathf.Max(.001f,approachLength)) :
                    Vector3.Lerp(entry,exit,(distance-approachLength)/Mathf.Max(.001f,exitLength));
                return new CharacterMotionSample(position,clock<departurePause*.5f,
                    clock>departurePause && clock<1.48f,clock>=departurePause*.5f && clock<departurePause*.7f,(int)(travel*6));
            }
            return new CharacterMotionSample(seat,true,false,false,0);
        }

        public static Vector3 Exit(Camera camera,Vector3 entry)
        {
            if(camera==null || !camera.orthographic) return entry+Vector3.left*7;
            // The old entrance is visible in Q1. Continue only its exit segment
            // beyond the camera edge, with enough margin for the widest cloak.
            float distance=Mathf.Max(0,camera.WorldToViewportPoint(entry).x+.15f)*2*camera.orthographicSize*camera.aspect;
            var at=entry-camera.transform.right*distance; at.y=entry.y; return at;
        }
    }

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
        public bool PoseReady => isActiveAndEnabled && Plane!=null && Renderer!=null && Contact!=null && StandingMesh!=null && SeatedMesh!=null && StandingMaterial!=null && SeatedMaterial!=null;
        private bool mirrored,warnedWalk;
        private int observedFrame=-1;
        private bool HasWalkFrame(int frame) => WalkMeshes!=null && WalkMaterials!=null && WalkMeshes.Length>0 && WalkMaterials.Length>0 &&
            WalkMeshes[frame%WalkMeshes.Length]!=null && WalkMaterials[frame%WalkMaterials.Length]!=null;

        public void ResetMotion() { observedFrame=-1; mirrored=false; }
        public void Present(CharacterMotionSample sample)
        {
            if(!PoseReady) { Show(false,false); return; }
            transform.position=sample.Position;
            if(sample.Walking && HasWalkFrame(sample.Frame)) ShowWalkFrame(sample.Frame);
            else {
                if(sample.Walking && !warnedWalk) { warnedWalk=true; Debug.LogWarning("Character walk art missing: "+name+". Grounded static travel remains available without foot contacts."); }
                Show(true,sample.Seated);
            }
            mirrored=!sample.Seated && sample.Mirrored;
        }
        public bool ObserveFootContact(CharacterMotionSample sample,bool active)
        {
            if(!active || !PoseReady || !Renderer.enabled || !sample.Walking) return false;
            if(observedFrame==sample.Frame) return false;
            observedFrame=sample.Frame;
            // Four source frames alternate planted 0/2 and passing 1/3. A hitch
            // observes only the current frame; absent art never invents a step.
            return sample.Frame%2==0 && HasWalkFrame(sample.Frame);
        }
        public void ComposePresentation(Quaternion rotation,float scale,Vector3 seatedDepth)
        {
            if(Plane==null || SeatAnchor==null) return;
            bool seated=Plane.sharedMesh==SeatedMesh && SeatedMesh!=StandingMesh;
            var plane=Plane.transform;
            plane.localRotation=rotation;
            plane.localScale=new Vector3(!seated && mirrored ? -scale : scale,scale,scale);
            Vector3 hip=Quaternion.Inverse(Q1ProductionView.FocusAngle)*SeatAnchor.localPosition;
            plane.localPosition=seated ? SeatAnchor.localPosition-rotation*(hip*scale)-seatedDepth : Vector3.zero;
        }
        public void ShowWalkFrame(int frame)
        {
            Show(true, false);
            if(!PoseReady || !HasWalkFrame(frame)) return;
            Plane.sharedMesh = WalkMeshes[frame % WalkMeshes.Length];
            Renderer.sharedMaterial = WalkMaterials[frame % WalkMaterials.Length];
        }
        public void Show(bool visible, bool seated)
        {
            mirrored=false;
            if(Plane!=null) Plane.sharedMesh = seated ? SeatedMesh : StandingMesh;
            if(Renderer!=null) { Renderer.sharedMaterial = seated ? SeatedMaterial : StandingMaterial; Renderer.enabled = visible && PoseReady; }
            if(Contact!=null) Contact.enabled = visible && PoseReady;
            if(!visible) ResetMotion();
        }
        private void OnDisable() => Show(false,false);
    }
}
