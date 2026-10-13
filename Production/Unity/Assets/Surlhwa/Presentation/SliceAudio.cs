using UnityEngine;
namespace Surlhwa.Presentation
{
    public sealed class SliceAudio : MonoBehaviour
    {
        private SliceController owner; private AudioSource room,winter,physical,cue;
        public bool PhysicalPlaying => physical!=null && physical.isPlaying;
        public void Initialize(SliceController controller)
        {
            owner=controller; room=Source(owner.Room,true); winter=Source(owner.Winter,true); physical=Source(null,true); cue=Source(null,false);
            if(room.clip!=null) room.Play(); if(winter.clip!=null) winter.Play();
        }
        private AudioSource Source(AudioClip clip,bool loop)
        { var s=gameObject.AddComponent<AudioSource>(); s.clip=clip; s.loop=loop; s.playOnAwake=false; s.spatialBlend=0; s.volume=0; return s; }
        public void Refresh(SliceInputAdapter input,bool active)
        {
            float master=owner.MasterVolume; room.volume=master*.15f; winter.volume=master*.10f; cue.volume=master*.24f; physical.volume=master*.18f;
            AudioClip clip=active && input.AcceptedStirThisStep ? owner.Stir : active && input.AcceptedLiquidThisStep ? (input.ActiveTool=="strain" ? owner.Strain : owner.Pour) : null;
            if(clip==null) { physical.Stop(); physical.clip=null; }
            else if(physical.clip!=clip || !physical.isPlaying) { physical.Stop(); physical.clip=clip; physical.Play(); }
        }
        public void Cue(string action)
        {
            var clip=action=="ice" ? owner.Ice : action=="seat" ? owner.Seat : action=="entrance" ? owner.Entrance : action=="foot" ? owner.Foot : owner.Glass;
            if(clip!=null) cue.PlayOneShot(clip);
        }
        private void OnDisable() { if(room!=null) room.Stop(); if(winter!=null) winter.Stop(); if(physical!=null) physical.Stop(); if(cue!=null) cue.Stop(); }
    }
}
