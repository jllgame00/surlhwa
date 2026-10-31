using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Surlhwa.Domain;

namespace Surlhwa.Presentation
{
    // Projects the authored cast only. Session stage time, service and departure own progress.
    // A paired visit uses the resident workbench's one actual glass, never a second cup.
    public sealed class CampaignCastView : MonoBehaviour
    {
        public Transform ChunhyangSeat, MongryongSeat, SharedCupAnchor;
        public bool Paired { get; private set; }
        private CampaignController owner;
        private readonly HashSet<CharacterView> warned=new HashSet<CharacterView>();
        private CampaignPhase lastPhase=(CampaignPhase)(-1);
        private int lastFoot=-1;
        public static bool OwnsGuest(string id) => id=="guest_chunhyang" || id=="guest_mongryong";

        public void Initialize(CampaignController controller,Vector3 primarySeat)
        {
            owner=controller;
            // The separate scene normally supplies these bindings. A missing anchor is a
            // reversible presentation fallback, not a reason to block an earned departure.
            if(ChunhyangSeat==null) ChunhyangSeat=Anchor("Chunhyang seated root",primarySeat);
            if(MongryongSeat==null) MongryongSeat=Anchor("Mongryong seated root",primarySeat+Vector3.right*1.7f);
            if(SharedCupAnchor==null) SharedCupAnchor=Anchor("Lovers one shared cup anchor",new Vector3(primarySeat.x+1.0214286f,1.055f,-.65f));
            Hide();
        }
        private Transform Anchor(string name,Vector3 position)
        {
            var anchor=new GameObject(name).transform; anchor.SetParent(transform,false); anchor.position=position; return anchor;
        }
        public void Refresh(CampaignPhase phase,float poseClock,Vector3 primarySeat)
        { Refresh(owner?.Session?.CurrentEncounter,phase,poseClock,primarySeat); }

        public void Refresh(CampaignEncounter encounter,CampaignPhase phase,float poseClock,Vector3 primarySeat)
        {
            if(!isActiveAndEnabled || owner==null || encounter==null || !OwnsGuest(encounter.GuestId) ||
                phase==CampaignPhase.Welcome || phase==CampaignPhase.NightIntro || phase==CampaignPhase.NightEnd || phase==CampaignPhase.Complete)
            { Hide(); return; }
            Paired=encounter.GuestIds.Count>1;
            // The Prince's second stool becomes MEM01. Existing stools to its right
            // remain adjacent for the pair; every later solo uses the left stool.
            Vector3 first=primarySeat,second=primarySeat+Vector3.right*1.7f;
            if(owner.PrinceChairReserved) {
                first=Paired ? primarySeat+Vector3.right*1.7f : owner.SoloGuestSeatPosition;
                second=Paired ? primarySeat+Vector3.right*2.95f : owner.SoloGuestSeatPosition;
            }
            if(ChunhyangSeat!=null) ChunhyangSeat.position=first;
            if(MongryongSeat!=null) MongryongSeat.position=second;
            if(SharedCupAnchor!=null) SharedCupAnchor.position=new Vector3((first.x+second.x)*.5f+.1714286f,1.055f,-.65f);
            if(lastPhase!=phase) { lastPhase=phase; lastFoot=-1; }
            bool chunhyang=encounter.GuestIds.Contains("guest_chunhyang");
            bool mongryong=encounter.GuestIds.Contains("guest_mongryong");
            Present(owner.Chunhyang,chunhyang,first,phase,poseClock,false);
            Present(owner.Mongryong,mongryong,second,phase,poseClock,true);
            // Both paths arrive/leave within the authoritative 1.5-second logical stage.
            // Quiet listening is a still seated frame; silence never borrows another actor's loop.
            bool traveling=phase==CampaignPhase.Arriving || phase==CampaignPhase.Departing;
            int foot=(int)(Mathf.Max(0,poseClock-(phase==CampaignPhase.Departing ? .18f : 0))*3);
            if(traveling && owner.Session?.CanInteract==true && foot!=lastFoot && poseClock<1.4f) {
                lastFoot=foot; owner.Audio?.Cue("foot");
            }
            if(owner.Workbench!=null) {
                Vector3? at=null;
                if(phase!=CampaignPhase.Brewing && owner.Session?.State.CurrentService?.Snapshot!=null) {
                    Vector3 seat=chunhyang ? first : second;
                    at=Paired && SharedCupAnchor!=null ? SharedCupAnchor.position : new Vector3(seat.x+.1714286f,1.055f,-.65f);
                }
                owner.Workbench.SetServingPresentationPosition(at);
            }
        }
        private bool Ready(CharacterView actor)
        {
            if(actor==null) return false;
            bool ready=actor.Plane!=null && actor.Renderer!=null && actor.Contact!=null && actor.StandingMesh!=null &&
                actor.SeatedMesh!=null && actor.StandingMaterial!=null && actor.SeatedMaterial!=null && actor.SeatAnchor!=null;
            if(!ready && warned.Add(actor)) Debug.LogWarning("Campaign cast presentation missing: "+actor.name+". Logical staging remains available; visual acceptance fails.");
            return ready;
        }
        private void Present(CharacterView actor,bool present,Vector3 seat,CampaignPhase phase,float clock,bool second)
        {
            if(!present || !Ready(actor)) { HideActor(actor); return; }
            bool departing=phase==CampaignPhase.Departing,arriving=phase==CampaignPhase.Arriving;
            if(!departing && !arriving) { actor.transform.position=seat; actor.Show(true,true); return; }
            float pause=departing ? (second ? .12f : .18f) : (second && Paired ? .10f : 0);
            float duration=departing ? 1.30f : 1.25f;
            float progress=Mathf.SmoothStep(0,1,Mathf.Max(0,clock-pause)/duration);
            var entry=new Vector3(-4.25f,0,second ? -2.94f : -2.65f);
            actor.transform.position=Vector3.Lerp(departing ? seat : entry,departing ? entry : seat,progress);
            bool walking=clock>=pause && progress<1;
            if(walking) {
                int frame=(int)(Mathf.Max(0,clock-pause)*6);
                bool walk=actor.WalkMeshes!=null && actor.WalkMaterials!=null && actor.WalkMeshes.Length>0 && actor.WalkMaterials.Length>0 &&
                    actor.WalkMeshes[frame%actor.WalkMeshes.Length]!=null && actor.WalkMaterials[frame%actor.WalkMaterials.Length]!=null;
                if(walk) actor.ShowWalkFrame(frame); else actor.Show(true,false);
            }
            else actor.Show(true,arriving ? progress>=1 : clock<pause*.5f);
        }
        public void ApplyView(bool focused)
        {
            if(owner==null) return;
            foreach(var actor in new[]{owner.Chunhyang,owner.Mongryong}) {
                if(!Ready(actor)) continue;
                var plane=actor.Plane.transform; bool seated=actor.Plane.sharedMesh==actor.SeatedMesh;
                float scale=focused ? 1 : Q1ProductionView.CharacterScale;
                plane.localScale=Vector3.one*scale;
                plane.localRotation=focused ? Q1ProductionView.FocusAngle : Q1ProductionView.WorldAngle;
                Vector3 hip=Quaternion.Inverse(Q1ProductionView.FocusAngle)*actor.SeatAnchor.localPosition;
                plane.localPosition=seated ? actor.SeatAnchor.localPosition-plane.localRotation*(hip*scale) : Vector3.zero;
                // Match Q1's seated cutout surface thickness, along the camera ray only.
                // Physical root/seat and the projected pelvis stay fixed. Focused tools and
                // standing/walking poses retain their exact existing plane placement.
                if(seated && !focused) {
                    Vector3 forward=owner.Camera!=null ? owner.Camera.transform.forward : Q1ProductionView.WorldAngle*Vector3.forward;
                    plane.localPosition-=Quaternion.Inverse(actor.transform.rotation)*forward*.14f;
                }
            }
        }
        public void Hide()
        {
            Paired=false; lastPhase=(CampaignPhase)(-1); lastFoot=-1;
            if(owner==null) return;
            HideActor(owner.Chunhyang); HideActor(owner.Mongryong);
            owner.Workbench?.SetServingPresentationPosition(null);
        }
        private static void HideActor(CharacterView actor)
        {
            if(actor==null) return;
            if(actor.Renderer!=null) actor.Renderer.enabled=false;
            if(actor.Contact!=null) actor.Contact.enabled=false;
        }
        private void OnDisable() => Hide();
    }
}
