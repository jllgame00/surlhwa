using UnityEngine;
using Surlhwa.Domain;

namespace Surlhwa.Presentation
{
    // A small presentation-only pose selector. Existing controller owns travel and seating.
    public sealed class CampaignCharacterArt : MonoBehaviour
    {
        private CampaignController owner;
        private int slot=-1;
        private float clock;
        public void Initialize(CampaignController controller) { owner=controller; }
        private void LateUpdate()
        {
            var art=owner?.Art;
            if(art==null || owner.Session==null) return;
            var session=owner.Session;
            if(session.CanInteract) clock+=Mathf.Min(Time.unscaledDeltaTime,.1f);
            if(slot!=session.State.SlotIndex)
            {
                slot=session.State.SlotIndex;
                // Stable visual variety, not a second guest identity or saved progression.
                var frames=slot%2==0 ? art.OrdinaryCoat : art.OrdinaryBun;
                if(frames!=null && frames.Length==4 && owner.Ordinary!=null)
                {
                    owner.Ordinary.StandingMaterial=frames[0]; owner.Ordinary.SeatedMaterial=frames[1];
                    owner.Ordinary.WalkMaterials=new[]{frames[2],frames[0],frames[3],frames[0]};
                }
            }
            if(owner.Bartender?.Renderer==null || art.BartenderIdle==null || art.BartenderIdle.Length<4) return;
            var phase=session.State.Phase;
            // The blink shares the towel pose. No bobbing feet or outcome-coded reaction.
            float cycle=Mathf.Repeat(clock,9.2f);
            int pose=phase==CampaignPhase.Brewing ? 0 : cycle<.16f ? 2 : cycle<3.8f ? 1 : 3;
            if(art.BartenderIdle[pose]!=null) owner.Bartender.Renderer.sharedMaterial=art.BartenderIdle[pose];
        }
    }
}
