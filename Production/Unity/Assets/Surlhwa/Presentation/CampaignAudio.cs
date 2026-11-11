using UnityEngine;
using Surlhwa.Application;
using Surlhwa.Domain;
namespace Surlhwa.Presentation
{
    public sealed class CampaignAudio : MonoBehaviour
    {
        private CampaignController owner; private AudioSource room,winter,physical,cue,music;
        private CampaignSession observedSession;
        private BrewingSession observedShake;
        private bool wasComplete,musicActive;
        private float roomEnvelope=1,winterEnvelope=1,musicEnvelope;
        private int footIndex,lastShakeReversal=-1;
        public bool PhysicalPlaying => physical!=null && physical.isPlaying;
        public void Initialize(CampaignController controller)
        {
            owner=controller; room=Source(owner.Room,true); winter=Source(owner.Winter,true); physical=Source(null,true); cue=Source(null,false); music=Source(owner.Music,false);
            ObserveSession();
            if(room.clip!=null) room.Play(); if(winter.clip!=null) winter.Play();
        }
        private AudioSource Source(AudioClip clip,bool loop)
        { var s=gameObject.AddComponent<AudioSource>(); s.clip=clip; s.loop=loop; s.playOnAwake=false; s.spatialBlend=0; s.volume=0; return s; }
        public void Refresh(SliceInputAdapter input,bool active)
        {
            if(owner==null) return;
            if(!isActiveAndEnabled) { StopAll(); return; }
            ObserveSession();
            var session=owner.Session; var phase=session?.State.Phase;
            bool reading=session?.CurrentText!=null;
            bool quiet=phase==CampaignPhase.Farewell || phase==CampaignPhase.EndingReady ||
                phase==CampaignPhase.EndingEntered || phase==CampaignPhase.EndingReading || phase==CampaignPhase.Complete;
            float delta=Mathf.Max(0,Time.unscaledDeltaTime),master=owner.MasterVolume;
            roomEnvelope=Mathf.MoveTowards(roomEnvelope,quiet ? .055f/.15f : reading ? .09f/.15f : 1,delta*3);
            winterEnvelope=Mathf.MoveTowards(winterEnvelope,quiet ? .045f/.08f : reading ? .065f/.08f : 1,delta*3);
            room.volume=master*.15f*roomEnvelope; winter.volume=master*.08f*winterEnvelope;
            cue.volume=master*.24f; physical.volume=master*.18f;

            bool available=active && session?.CanInteract==true;
            bool complete=session?.State.ActComplete==true;
            // A bound Complete save is a baseline. Only this live session's new
            // successful completion can start music, never a load or a retry failure.
            if(complete && !wasComplete && available && music.clip!=null) {
                musicActive=true; musicEnvelope=0; music.Play();
            }
            wasComplete=complete;
            if(!available || !complete || !musicActive || !music.isPlaying) StopMusic();
            else musicEnvelope=Mathf.MoveTowards(musicEnvelope,1,delta*(.08f/.12f));
            music.volume=master*.12f*musicEnvelope;
            if(!available) { StopPhysical(); cue.Stop(); return; }

            var brew=session.Brew;
            if(input?.ActiveTool=="shake" && brew?.ShakeActive==true) {
                if(observedShake!=brew) { observedShake=brew; lastShakeReversal=-1; }
                if(owner.Shake==null) { StopPhysical(); return; }
                if(input.AcceptedShakeThisStep && brew.ShakeReversals!=lastShakeReversal) {
                    lastShakeReversal=brew.ShakeReversals;
                    physical.Stop(); physical.loop=false; physical.clip=owner.Shake; physical.Play();
                } else if(physical.clip!=owner.Shake) StopPhysical();
                return; // A short contact tail can decay; stationary frames never restart it.
            }
            observedShake=null; lastShakeReversal=-1;
            AudioClip clip=input?.AcceptedStirThisStep==true && brew?.StirActive==true ? owner.Stir :
                input?.AcceptedLiquidThisStep==true && brew?.FlowActive==true ?
                (input.ActiveTool=="strain" || input.ActiveTool=="shaker_strain" ? owner.Strain : owner.Pour) : null;
            if(clip==null) StopPhysical();
            else if(physical.clip!=clip || !physical.loop || !physical.isPlaying) {
                physical.Stop(); physical.loop=true; physical.clip=clip; physical.Play();
            }
        }
        public void Cue(string action)
        {
            if(owner==null || !isActiveAndEnabled) return;
            ObserveSession();
            if(owner.Session?.CanInteract!=true) return;
            var clip=action=="ice" ? owner.Ice : action=="seat" ? owner.Seat : action=="entrance" ? owner.Entrance : action=="foot" ? owner.Foot : owner.Glass;
            if(clip!=null) { cue.pitch=action=="foot" ? (++footIndex%2==0 ? .98f : 1.02f) : 1; cue.PlayOneShot(clip); }
        }
        private void ObserveSession()
        {
            if(observedSession==owner.Session) return;
            StopPhysical(); cue.Stop(); StopMusic();
            observedSession=owner.Session; wasComplete=observedSession?.State.ActComplete==true;
        }
        private void StopPhysical()
        {
            if(physical!=null) { physical.Stop(); physical.clip=null; }
            observedShake=null; lastShakeReversal=-1;
        }
        private void StopMusic()
        {
            if(music!=null) { music.Stop(); music.volume=0; }
            musicActive=false; musicEnvelope=0;
        }
        private void StopAll()
        {
            if(room!=null) room.Stop(); if(winter!=null) winter.Stop();
            StopPhysical(); if(cue!=null) cue.Stop(); StopMusic();
        }
        private void OnEnable()
        {
            if(owner==null) return;
            ObserveSession(); wasComplete=owner.Session?.State.ActComplete==true; StopMusic();
            if(room!=null) room.volume=owner.MasterVolume*.15f*roomEnvelope;
            if(winter!=null) winter.volume=owner.MasterVolume*.08f*winterEnvelope;
            if(room?.clip!=null) room.Play(); if(winter?.clip!=null) winter.Play();
        }
        private void OnDisable()=>StopAll();
    }
}
