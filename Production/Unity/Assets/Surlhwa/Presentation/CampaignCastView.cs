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
        private object lastSession;
        private string lastEncounter;
        private CharacterView boundFirst,boundSecond;
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
            SyncBindings();
            if(!isActiveAndEnabled || owner==null || encounter==null || !OwnsGuest(encounter.GuestId) ||
                phase==CampaignPhase.Welcome || phase==CampaignPhase.NightIntro || phase==CampaignPhase.NightEnd || phase==CampaignPhase.Complete || phase>=CampaignPhase.EndingReady)
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
            if(lastPhase!=phase || lastSession!=owner.Session || lastEncounter!=encounter.Id) {
                lastPhase=phase; lastSession=owner.Session; lastEncounter=encounter.Id;
                owner.Chunhyang?.ResetMotion(); owner.Mongryong?.ResetMotion();
            }
            bool chunhyang=encounter.GuestIds.Contains("guest_chunhyang");
            bool mongryong=encounter.GuestIds.Contains("guest_mongryong");
            bool firstContact=Present(owner.Chunhyang,chunhyang,first,phase,poseClock,false);
            bool secondContact=Present(owner.Mongryong,mongryong,second,phase,poseClock,true);
            // Both paths arrive/leave within the authoritative 1.5-second logical stage.
            // Quiet listening is a still seated frame; silence never borrows another actor's loop.
            // Two independently observed contacts may coincide. Keep one quiet
            // accent on the existing non-spatial source, not double gain.
            if(firstContact || secondContact) owner.Audio?.Cue("foot");
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
        private bool Present(CharacterView actor,bool present,Vector3 seat,CampaignPhase phase,float clock,bool second)
        {
            if(!present || !Ready(actor)) { HideActor(actor); return false; }
            var entry=new Vector3(-4.25f,seat.y,second ? -2.94f : -2.65f);
            var sample=CharacterMotionSample.At(phase,clock,seat,entry,CharacterMotionSample.Exit(owner.Camera,entry),
                second ? .12f : .18f,second && Paired ? .10f : 0);
            actor.Present(sample);
            return actor.ObserveFootContact(sample,owner.Session?.CanInteract==true);
        }
        public void ApplyView(bool focused)
        {
            if(owner==null) return;
            foreach(var actor in new[]{owner.Chunhyang,owner.Mongryong}) {
                if(!Ready(actor)) continue;
                float scale=focused ? 1 : Q1ProductionView.CharacterScale;
                // Match Q1's seated cutout surface thickness, along the camera ray only.
                // Physical root/seat and the projected pelvis stay fixed. Focused tools and
                // standing/walking poses retain their exact existing plane placement.
                Vector3 forward=owner.Camera!=null ? owner.Camera.transform.forward : Q1ProductionView.WorldAngle*Vector3.forward;
                actor.ComposePresentation(focused ? Q1ProductionView.FocusAngle : Q1ProductionView.WorldAngle,scale,
                    focused ? Vector3.zero : Quaternion.Inverse(actor.transform.rotation)*forward*.14f);
            }
        }
        public void Hide()
        {
            Paired=false; lastPhase=(CampaignPhase)(-1); lastSession=null; lastEncounter=null;
            if(owner==null) return;
            SyncBindings();
            HideActor(owner.Chunhyang); HideActor(owner.Mongryong);
            owner.Workbench?.SetServingPresentationPosition(null);
        }
        private static void HideActor(CharacterView actor)
        {
            if(actor==null) return;
            if(actor.Renderer!=null) actor.Renderer.enabled=false;
            if(actor.Contact!=null) actor.Contact.enabled=false;
            actor.ResetMotion();
        }
        private void SyncBindings()
        {
            if(owner==null) return;
            if(boundFirst!=owner.Chunhyang) { HideActor(boundFirst); boundFirst=owner.Chunhyang; boundFirst?.ResetMotion(); }
            if(boundSecond!=owner.Mongryong) { HideActor(boundSecond); boundSecond=owner.Mongryong; boundSecond?.ResetMotion(); }
        }
        private void OnDisable() => Hide();
    }
}
