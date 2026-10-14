using UnityEngine;
using Surlhwa.Domain;
namespace Surlhwa.Presentation
{
    public sealed class CampaignAudio : MonoBehaviour
    {
        private CampaignController owner; private AudioSource room,winter,physical,cue,music;
        private string musicRun; private int footIndex;
        public bool PhysicalPlaying => physical!=null && physical.isPlaying;
        public void Initialize(CampaignController controller)
        {
            owner=controller; room=Source(owner.Room,true); winter=Source(owner.Winter,true); physical=Source(null,true); cue=Source(null,false); music=Source(owner.Music,false);
            if(room.clip!=null) room.Play(); if(winter.clip!=null) winter.Play();
        }
        private AudioSource Source(AudioClip clip,bool loop)
        { var s=gameObject.AddComponent<AudioSource>(); s.clip=clip; s.loop=loop; s.playOnAwake=false; s.spatialBlend=0; s.volume=0; return s; }
        public void Refresh(SliceInputAdapter input,bool active)
        {
            var session=owner.Session; var phase=session?.State.Phase;
            bool reading=phase==CampaignPhase.Intro || phase==CampaignPhase.Reaction || phase==CampaignPhase.Farewell || phase==CampaignPhase.ChoiceResponse;
            bool quiet=phase==CampaignPhase.Farewell;
            float master=owner.MasterVolume; room.volume=master*(quiet ? .055f : reading ? .09f : .15f); winter.volume=master*(quiet ? .045f : .08f); cue.volume=master*.24f; physical.volume=master*.18f;
            // One sparse cue after the actual memory commit. No music under farewell, no replay on Continue.
            if(session!=null && musicRun==null) musicRun=session.State.ActComplete ? session.State.RunId : "";
            if(session?.State.ActComplete==true && musicRun!=session.State.RunId) { musicRun=session.State.RunId; if(music.clip!=null && active) music.Play(); }
            float target=active && phase==CampaignPhase.Complete ? master*.12f : 0;
            music.volume=Mathf.MoveTowards(music.volume,target,Time.unscaledDeltaTime*.08f);
            if(session==null || !session.Focused || session.Modals.Count>0) { music.Stop(); music.volume=0; }
            AudioClip clip=active && input.AcceptedStirThisStep ? owner.Stir : active && input.AcceptedLiquidThisStep ? (input.ActiveTool=="strain" ? owner.Strain : owner.Pour) : null;
            if(clip==null) { physical.Stop(); physical.clip=null; }
            else if(physical.clip!=clip || !physical.isPlaying) { physical.Stop(); physical.clip=clip; physical.Play(); }
        }
        public void Cue(string action)
        {
            var clip=action=="ice" ? owner.Ice : action=="seat" ? owner.Seat : action=="entrance" ? owner.Entrance : action=="foot" ? owner.Foot : owner.Glass;
            if(clip!=null) { cue.pitch=action=="foot" ? (++footIndex%2==0 ? .98f : 1.02f) : 1; cue.PlayOneShot(clip); }
        }
        private void OnDisable() { if(room!=null) room.Stop(); if(winter!=null) winter.Stop(); if(physical!=null) physical.Stop(); if(cue!=null) cue.Stop(); if(music!=null) music.Stop(); }
    }
}
