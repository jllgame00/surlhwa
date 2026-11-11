using UnityEngine;
using UnityEngine.Rendering;
using Surlhwa.Domain;

namespace Surlhwa.Presentation
{
    // A small presentation-only pose selector. Existing controller owns travel and seating.
    public sealed class CampaignCharacterArt : MonoBehaviour
    {
        private CampaignController owner;
        private int slot=-1;
        private float clock;
        private object observedSession;
        private string previousText;
        private bool wiping,endingSettled;
        private float wipeStarted;
        private GameObject cleaningGlass;
        private Mesh cleaningMesh;
        private Material cleaningMaterial;
        public void Initialize(CampaignController controller)
        {
            owner=controller; observedSession=null; previousText=null; slot=-1; clock=0; wiping=false; endingSettled=false;
            if(cleaningGlass!=null) cleaningGlass.SetActive(false);
        }
        private void LateUpdate()
        {
            if(!isActiveAndEnabled) return;
            var art=owner?.Art;
            if(art==null || owner.Session==null || owner.Bartender?.Renderer==null || owner.Bartender.Plane==null) {
                wiping=false; observedSession=null; previousText=null;
                if(cleaningGlass!=null) cleaningGlass.SetActive(false);
                return;
            }
            var session=owner.Session;
            var phase=session.State.Phase; string text=session.CurrentText?.Id;
            bool ending=owner.Content?.Ending!=null && (phase>=CampaignPhase.EndingReady || phase==CampaignPhase.Complete);
            bool rebound=observedSession!=session;
            if(rebound) {
                observedSession=session; previousText=text; slot=-1; clock=0; wiping=false;
                // Continue establishes a stable pose at the current line. It does
                // not replay a gesture whose live entry was never observed.
                endingSettled=ending && (phase==CampaignPhase.Complete || text=="text_rc1_0007" || text=="text_rc1_0008");
            }
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
            if(ending && !rebound && text=="text_rc1_0007" && previousText!=text) { wiping=true; endingSettled=false; wipeStarted=clock; }
            if(wiping && (text!="text_rc1_0007" || clock-wipeStarted>=.95f)) { wiping=false; endingSettled=true; }
            previousText=text;
            if(ending && art.Rocks!=null) EnsureCleaningGlass(art.Rocks);
            if(cleaningGlass!=null) cleaningGlass.SetActive(ending && art.Rocks!=null);
            bool reading=phase==CampaignPhase.Intro || phase==CampaignPhase.Request || phase==CampaignPhase.Choice ||
                phase==CampaignPhase.ChoiceResponse || phase==CampaignPhase.Reaction || phase==CampaignPhase.Farewell || ending;
            // Listening is a hold, not a periodic towel/nod loop underneath text.
            // The one authored cleanup entry is independent of the saved cursor.
            int pose=ending ? (wiping ? 1 : endingSettled ? 0 : 3) : reading ? 3 : phase==CampaignPhase.Brewing ? 0 : Mathf.Repeat(clock,9.2f)<.12f ? 2 : 0;
            SetBartenderPose(pose);
            ApplyPresentation();
        }

        private void SetBartenderPose(int index)
        {
            var frames=owner?.Art?.BartenderIdle;
            if(owner?.Bartender?.Renderer!=null && frames!=null && index<frames.Length && frames[index]!=null)
                owner.Bartender.Renderer.sharedMaterial=frames[index];
        }
        private void EnsureCleaningGlass(Texture2D texture)
        {
            if(cleaningGlass!=null) return;
            cleaningGlass=new GameObject("Ending empty cleaning glass"); cleaningGlass.transform.SetParent(transform,false);
            cleaningMesh=new Mesh { name="Resident empty cleanup glass",
                vertices=new[]{new Vector3(-.085f,0,0),new Vector3(-.085f,.21f,0),new Vector3(.085f,.21f,0),new Vector3(.085f,0,0)},
                // Existing 56x56 Rocks source has cutoff-alpha bounds x3..53,
                // bottom-up y3..52. Crop only its UV margin so the opaque base
                // meets the counter instead of floating above it.
                uv=new[]{new Vector2(3f/56,3f/56),new Vector2(3f/56,53f/56),new Vector2(54f/56,53f/56),new Vector2(54f/56,3f/56)},triangles=new[]{0,1,2,0,2,3} };
            cleaningMesh.RecalculateBounds(); cleaningMesh.RecalculateNormals();
            cleaningGlass.AddComponent<MeshFilter>().sharedMesh=cleaningMesh;
            cleaningMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            cleaningMaterial.SetTexture("_BaseMap",texture); cleaningMaterial.SetColor("_BaseColor",Color.white);
            cleaningMaterial.SetFloat("_AlphaClip",1); cleaningMaterial.SetFloat("_Cutoff",.4f); cleaningMaterial.SetFloat("_Cull",0); cleaningMaterial.SetFloat("_ZWrite",1);
            cleaningMaterial.EnableKeyword("_ALPHATEST_ON"); cleaningMaterial.renderQueue=2450;
            var renderer=cleaningGlass.AddComponent<MeshRenderer>(); renderer.sharedMaterial=cleaningMaterial;
            renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
        }
        // Q1 calls this after its actor composition; it also supports the earlier
        // fixed view without Q1. No separate writer modifies the bartender plane.
        public void ApplyPresentation()
        {
            if(cleaningGlass==null || !cleaningGlass.activeSelf || owner?.Bartender?.Plane==null) return;
            var plane=owner.Bartender.Plane.transform; var prop=cleaningGlass.transform;
            if(wiping) {
                // Towel source hand/cloth contact near (52,40) in its 96x84 frame,
                // with the existing (48,0), 52 PPU body pivot. Only a tiny wipe
                // moves the empty shell; no old serving liquid is copied.
                float sweep=Mathf.Sin((clock-wipeStarted)/.95f*Mathf.PI*4)*.008f;
                prop.position=plane.TransformPoint(new Vector3(4f/52+sweep,40f/52-.105f,-.025f));
                prop.rotation=plane.rotation;
            } else {
                // The pose change puts the glass on the resident counter. A
                // floating cross-room tween would detach it from the source hands.
                prop.position=new Vector3(owner.Bartender.transform.position.x+.45f,.995f,-.45f);
                prop.rotation=owner.Camera!=null ? owner.Camera.transform.rotation : Q1ProductionView.WorldAngle;
            }
            var scale=plane.lossyScale; prop.localScale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
        }
        private void OnDisable()
        {
            wiping=false; endingSettled=true; observedSession=null; previousText=null;
            SetBartenderPose(0); ApplyPresentation(); if(cleaningGlass!=null) cleaningGlass.SetActive(false);
        }
        private void OnDestroy()
        {
            if(cleaningMesh!=null) Release(cleaningMesh); if(cleaningMaterial!=null) Release(cleaningMaterial);
            if(cleaningGlass!=null) Release(cleaningGlass);
        }
        private static void Release(Object item) { if(UnityEngine.Application.isPlaying) Destroy(item); else DestroyImmediate(item); }
    }
}
