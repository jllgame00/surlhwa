using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Surlhwa.Domain;

namespace Surlhwa.Presentation
{
    // Throwaway presentation adapter, present only in Bar_QuarterView. No saved state.
    [DefaultExecutionOrder(900)]
    public sealed class QuarterViewSpike : MonoBehaviour
    {
        public CampaignController Owner;
        public GameObject Dressing;
        public Material PrinceBackQuarter, OrdinaryBackQuarter;
        public bool UseBackQuarter=true;
        public int Candidate = 1;
        private readonly Dictionary<Transform, Quaternion> rotations = new Dictionary<Transform, Quaternion>();
        private readonly Dictionary<CharacterView, Vector3> hips = new Dictionary<CharacterView, Vector3>();
        private Vector3 originalCamera;
        private Quaternion originalRotation;
        private float originalSize;
        private Transform[] facing;
        private Renderer ground;
        private readonly Dictionary<Renderer,Material[]> surfaces=new Dictionary<Renderer,Material[]>();
        private bool initialized;
        public bool CaptureOwnsPose;
        public string CandidateName => Candidate == 0 ? "C1" : Candidate == 1 ? "Q1" : "Q2";

        private void Awake()
        {
            // The review executable must never open the owner's production save namespace.
            var args=Environment.GetCommandLineArgs();
            int saveArgument=Array.IndexOf(args,"-campaignSaveRoot");
            if(saveArgument<0 || saveArgument+1>=args.Length || args[saveArgument+1].StartsWith("-")) {
                Owner.enabled=false; isolatedSaveMissing=true;
            }
            Initialize(); Select(Candidate);
        }
        private bool isolatedSaveMissing;
        public void Initialize()
        {
            if(initialized) return;
            originalCamera=Owner.Camera.transform.position; originalRotation=Owner.Camera.transform.rotation;
            originalSize=Owner.Camera.orthographicSize;
            ground=GameObject.Find("C_SnowGround").GetComponent<Renderer>();
            foreach(var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="C_CounterReturn")) surfaces[r]=r.sharedMaterials;
            foreach(var actor in new[]{Owner.Bartender,Owner.Ordinary,Owner.Prince}) {
                rotations.Add(actor.Plane.transform,actor.Plane.transform.localRotation);
                hips.Add(actor,Quaternion.Inverse(actor.Plane.transform.localRotation)*actor.SeatAnchor.localPosition);
            }
            // Existing planes authored parallel to the old camera. Rear-wall art stays on its wall.
            facing=FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(r=>r.GetComponentInParent<CharacterView>()==null && Quaternion.Angle(r.transform.rotation,originalRotation)<.1f)
                .Select(r=>r.transform).ToArray();
            foreach(var t in facing) if(!rotations.ContainsKey(t)) rotations.Add(t,t.localRotation);
            initialized=true;
        }
        public void Select(int candidate, string framing="Normal")
        {
            if(candidate<0 || candidate>2) throw new ArgumentOutOfRangeException(nameof(candidate));
            Initialize(); Candidate=candidate;
            var camera=Owner.Camera; camera.aspect=16f/9; camera.nearClipPlane=.1f; camera.farClipPlane=100;
            if(candidate==0) {
                camera.orthographic=true; camera.orthographicSize=originalSize;
                camera.transform.SetPositionAndRotation(originalCamera,originalRotation);
            } else {
                camera.transform.rotation=Quaternion.Euler(38,-28,0);
                camera.orthographic=candidate==1; camera.orthographicSize=4.85f; camera.fieldOfView=14;
                float distance=candidate==1 ? 30 : 4.85f/Mathf.Tan(7*Mathf.Deg2Rad);
                camera.transform.position=new Vector3(0,.50f,.65f)-camera.transform.forward*distance;
            }
            if(framing=="Wide") {
                if(camera.orthographic) camera.orthographicSize=candidate==0 ? 4.55f : 5.75f;
                else camera.transform.position-=camera.transform.forward*7.33f;
            } else if(framing=="Grounding") {
                if(camera.orthographic) camera.orthographicSize=1.65f;
                camera.transform.position=new Vector3(-.4f,.62f,-.6f)-camera.transform.forward*(camera.orthographic ? 30 : 1.65f/Mathf.Tan(7*Mathf.Deg2Rad));
            }
            if(Dressing!=null) Dressing.SetActive(candidate!=0);
            ground.enabled=candidate==0;
            foreach(var pair in surfaces) {
                var mats=(Material[])pair.Value.Clone();
                if(candidate!=0) for(int i=0;i<mats.Length;i++) if(i!=4) mats[i]=pair.Value[0];
                pair.Key.sharedMaterials=mats;
            }
            foreach(var t in facing) if(t!=null) t.localRotation=candidate==0 ? rotations[t] : camera.transform.rotation;
            ApplyActors();
        }
        public void ApplyActors()
        {
            foreach(var actor in new[]{Owner.Bartender,Owner.Ordinary,Owner.Prince}) {
                var plane=actor.Plane.transform;
                plane.localRotation=Candidate==0 ? rotations[plane] : Owner.Camera.transform.rotation;
                bool seated=actor!=Owner.Bartender && actor.Plane.sharedMesh==actor.SeatedMesh;
                plane.localPosition=Candidate!=0 && seated ? actor.SeatAnchor.localPosition-plane.localRotation*hips[actor] : Vector3.zero;
                var back=actor==Owner.Prince ? PrinceBackQuarter : actor==Owner.Ordinary ? OrdinaryBackQuarter : null;
                if(seated && back!=null) {
                    bool rear=Candidate!=0 && UseBackQuarter;
                    actor.Renderer.sharedMaterial=rear ? back : actor.SeatedMaterial;
                    // The rear coat is the near surface of the seated body, not the pelvis center.
                    // A bounded 14 cm camera-axis surface offset keeps the real stool behind the coat.
                    // Foot/seat roots stay fixed; ordinary depth test/write still owns all occlusion.
                    if(rear) plane.localPosition-=Quaternion.Inverse(actor.transform.rotation)*Owner.Camera.transform.forward*.14f;
                }
            }
        }
        private void LateUpdate()
        {
            if(!CaptureOwnsPose) ApplyActors();
            // Existing focused bartending projection is retained as a reversible presentation choice.
            // The spike does not remap the existing physical tool/input interaction.
            if(!CaptureOwnsPose && Owner.Session!=null) {
                bool brewing=Owner.Session.State.Phase==CampaignPhase.Brewing;
                if(brewing && Candidate!=0) { requestedCandidate=Candidate; Select(0); focused=true; }
                else if(!brewing && focused) { focused=false; Select(requestedCandidate); }
            }
        }
        private bool focused;
        private int requestedCandidate=1;
        private void OnGUI()
        {
            if(Environment.GetCommandLineArgs().Contains("-quarterCapture")) return;
            GUILayout.BeginArea(new Rect(12,65,220,150),GUI.skin.box);
            GUILayout.Label("QUARTER VIEW / SPIKE ONLY");
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("C1")) { focused=false; Select(0); }
            if(GUILayout.Button("Q1")) Select(1);
            if(GUILayout.Button("Q2")) Select(2);
            GUILayout.EndHorizontal(); GUILayout.Label("C1 remains Production baseline");
            if(isolatedSaveMissing) GUILayout.Label("Use run_quarter_view.ps1 for an isolated ACT 1 save.");
            GUILayout.EndArea();
        }
    }
}
