using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Surlhwa.Domain;
using Surlhwa.ContentIO;

namespace Surlhwa.Presentation
{
    // Explicit comparison harness. ACT 1 owns staging and progression; this only records/replays poses.
    public sealed class Q1PolishCapture : MonoBehaviour
    {
        private Q1ProductionView stage; private CampaignController owner; private string output;
        private readonly List<string> checks=new List<string>();
        private readonly List<string> errors=new List<string>();
        private readonly List<Shot> shots=new List<Shot>();
        private readonly List<Measure> measures=new List<Measure>();
        private readonly List<Pose> motion=new List<Pose>();
        private CharacterView guest; private string actorName;
        private bool done;
        private static string Arg(string key) { var args=Environment.GetCommandLineArgs(); int i=Array.IndexOf(args,key); return i<0 || i+1>=args.Length ? null : args[i+1]; }
        private IEnumerator Start()
        {
            output=Arg("-quarterCapture"); if(output==null) { enabled=false; yield break; }
            output=Path.GetFullPath(output); Directory.CreateDirectory(output);
            UnityEngine.Application.logMessageReceived+=Log;
            stage=GetComponent<Q1ProductionView>(); owner=stage.Owner; stage.CaptureOwnsPose=true;
            float limit=Time.realtimeSinceStartup+40;
            while(owner.Ui==null || owner.Busy) { if(Time.realtimeSinceStartup>limit) throw new Exception("ACT 1 startup timed out"); yield return null; }
            Check(owner.StartupError==null,"ACT 1 scene loaded without startup error");
            Check(owner.Camera.orthographic && Quaternion.Angle(owner.Camera.transform.rotation,Q1ProductionView.WorldAngle)<.001f,"ACT 1 starts in locked Q1 without a camera selector");
            actorName=Arg("-quarterActor") ?? "Ordinary";
            guest=actorName=="Prince" ? owner.Prince : owner.Ordinary;
            if(actorName=="Prince") {
                Check(owner.Saved?.Checkpoint!=null,"Prince fixture uses an actual earned ACT 1 checkpoint copy");
                owner.ContinueRun(); yield return null;
                Check(owner.Content.Guests[owner.Session.CurrentEncounter.GuestId].Kind==GuestKind.Special,"ACT 1 selects Little Prince");
            } else {
                owner.BeginNew(); yield return Idle();
                while(owner.Session.State.Phase!=CampaignPhase.Arriving) { owner.Advance(); yield return Idle(); yield return null; }
                yield return RecordTravel("Arriving");
            }
            yield return null; yield return null;
            if(Arg("-quarterPreview")!="1") yield return RecordStill("Seated",.65f);
            owner.enabled=false; stage.ApplyView(true); guest.Show(true,true); stage.ApplyActors();
            Check(new[]{owner.Bartender,owner.Ordinary,owner.Prince}.All(a=>a.FeetAnchor!=null && a.SeatAnchor!=null && a.Plane!=null && a.Renderer.sharedMaterial!=null),"All three representative character/feet/seat bindings resolve");
            string state=CampaignSaveCodec.Encode(owner.Session.State,owner.Content);
            owner.OpenModal("codex"); owner.Ui.Refresh();
            Check(owner.Ui.VisibleModal=="codex","Existing Codex opens in Q1");
            owner.CloseModal(); owner.Ui.Refresh();
            Check(state==CampaignSaveCodec.Encode(owner.Session.State,owner.Content),"Codex open/close preserves gameplay checkpoint");
            var canvas=owner.Ui.GetComponentInChildren<Canvas>(); canvas.enabled=false;
            owner.Workbench.gameObject.SetActive(false);
            foreach(int variant in new[]{1}) {
                stage.ApplyView(false);
                foreach(string frame in new[]{"Seated","Composition","Wide","Grounding","Empty"}) {
                    if(frame=="Empty" && actorName=="Prince") continue;
                    guest.Show(frame!="Empty",true); owner.Bartender.Show(frame!="Seated" && frame!="Empty",false);
                    stage.ApplyView(false,frame);
                    owner.Workbench.gameObject.SetActive(false);
                    foreach(int width in new[]{1280,1920}) Capture(frame,width);
                }
                guest.Show(true,true); owner.Bartender.Show(true,false); stage.ApplyView(false);
                canvas.enabled=true;
                bool focus=owner.Session.State.Phase==CampaignPhase.Brewing;
                stage.ApplyView(focus); owner.Workbench.gameObject.SetActive(focus);
                foreach(int width in new[]{1280,1920}) Capture(focus ? "FocusedTools" : "ContextUI",width);
                stage.ApplyView(false);
                canvas.enabled=false; owner.Workbench.gameObject.SetActive(false);
                if(Arg("-quarterPreview")!="1") foreach(int width in new[]{1280,1920}) yield return MeasureIdle(width);
            }
            Check(state==CampaignSaveCodec.Encode(owner.Session.State,owner.Content),"Camera, world plates and perf leave the ACT 1 checkpoint byte-identical");
            if(Arg("-quarterPreview")!="1") {
                // Bounded technical service to reach the existing departure, not campaign QA.
                stage.ApplyView(true); owner.enabled=true; owner.Workbench.gameObject.SetActive(true); canvas.enabled=true;
                owner.Session.SetFocus(true);
                while(owner.Session.State.Phase!=CampaignPhase.Brewing) { owner.Advance(); yield return Idle(); yield return null; }
                string brewingState=CampaignSaveCodec.Encode(owner.Session.State,owner.Content);
                stage.CaptureOwnsPose=false; stage.ApplyView(false); yield return null; yield return null;
                Check(stage.Focused && owner.Camera.orthographic && Mathf.Abs(owner.Camera.orthographicSize-3.7f)<.001f,"Active bartending retains its existing focused input projection");
                Check(brewingState==CampaignSaveCodec.Encode(owner.Session.State,owner.Content),"Focused view changes no gameplay checkpoint");
                stage.CaptureOwnsPose=true;
                var brew=owner.Session.Brew; string recipe=actorName=="Prince" ? "recipe_manhattan" : "recipe_test_stir";
                owner.Session.ReleaseHeldInput();
                owner.Session.ExecuteBrew(owner.Input.NextEventId(),b=> {
                    foreach(var a in owner.Content.Catalog.Recipes[recipe].Amounts) b.Pour(a.Key,ContainerRole.Mixing,a.Value,a.Key=="angostura_bitters" ? BrewAction.Dash : BrewAction.Pour);
                    b.AddIce(ContainerRole.Mixing); b.BeginStir(.75,0);
                    for(int i=1;i<=144;i++) { double rad=i*10*Math.PI/180; b.MoveStir(.75*Math.Cos(rad),.75*Math.Sin(rad),66667); }
                    b.EndStir(); b.Transfer(ContainerRole.Mixing,ContainerRole.Serving,180m,BrewAction.Strain);
                    if(recipe=="recipe_manhattan") b.Garnish("cherry","in_glass");
                });
                owner.Advance(); yield return Idle(); yield return null;
                Check(owner.Session.State.CurrentService?.Outcome.Recognition==DrinkRecognition.Recognized,"Existing service recognition operates unchanged in spike scene");
                Check(owner.Session.State.CurrentService?.Outcome.Preparation==PreparationResult.Good,"Actual liquid/technique preparation remains authoritative");
                stage.CaptureOwnsPose=false; yield return null; yield return null;
                Check(!stage.Focused && Quaternion.Angle(owner.Camera.transform.rotation,Q1ProductionView.WorldAngle)<.001f,"Reaction returns to locked Q1 after focused bartending");
                stage.CaptureOwnsPose=true; stage.ApplyView(false); stage.ApplyActors();
                var servedFill=owner.Workbench.GetComponentsInChildren<Renderer>(true).Single(r=>r.name=="Actual bowl liquid");
                Check(servedFill.gameObject.activeInHierarchy && !servedFill.forceRenderingOff && owner.Workbench.ServingFillMl==owner.Session.State.CurrentService.Snapshot.TotalMl,"Q1 reaction retains the actual served liquid while prep tools are hidden");
                foreach(int width in new[]{1280,1920}) Capture("ReactionUI",width);
                yield return RecordStill("Reaction",.65f);
                while(owner.Session.State.Phase!=CampaignPhase.Departing) { owner.Advance(); yield return Idle(); yield return null; }
                yield return RecordTravel("Departing");
                owner.enabled=false; canvas.enabled=false; owner.Workbench.gameObject.SetActive(false);
                Vector3 seat=guest.transform.position;
                foreach(int variant in new[]{1}) foreach(int width in new[]{1280,1920}) {
                    stage.ApplyView(false); owner.Bartender.Show(true,false);
                    owner.Workbench.gameObject.SetActive(false);
                    string folder=Path.Combine(output,"Q1"+"_"+actorName+"_motion_"+width); Directory.CreateDirectory(folder);
                    for(int n=0;n<motion.Count;n++) {
                        var pose=motion[n]; guest.transform.position=pose.position; guest.Show(true,pose.seated);
                        (guest==owner.Prince ? owner.Ordinary : owner.Prince).Show(false,true);
                        guest.Plane.sharedMesh=pose.mesh; guest.Renderer.sharedMaterial=pose.material; stage.ApplyActors();
                        // Let renderer visibility/transform changes reach the normal Player frame
                        // before the explicit offscreen request. Gameplay remains disabled.
                        yield return null;
                        CheckFrame(guest.Renderer.enabled && !(guest==owner.Prince ? owner.Ordinary : owner.Prince).Renderer.enabled);
                        SavePixels(Path.Combine(folder,n.ToString("D4")+".png"),width);
                        yield return null;
                    }
                }
                guest.transform.position=seat;
                owner.enabled=true; canvas.enabled=true;
                yield return Idle();
                owner.ReturnToSavedMenu(); yield return Idle();
                Check(owner.Saved?.Checkpoint!=null && owner.StartupError==null,"Existing save reload succeeds after representative service/departure");
                string saved=CampaignSaveCodec.Encode(owner.Saved.Checkpoint,owner.Content);
                owner.ContinueRun(); yield return Idle();
                Check(saved==CampaignSaveCodec.Encode(owner.Session.State,owner.Content),"Continue resumes the existing encoded checkpoint without presentation state");
            }
            Finish();
        }
        private IEnumerator Idle() { yield return null; float until=Time.realtimeSinceStartup+20; while(owner.Busy || owner.Session?.IsSaving==true) { if(Time.realtimeSinceStartup>until) throw new Exception("ACT 1 save timeout"); yield return null; } }
        private IEnumerator RecordTravel(string phase)
        {
            float start=Time.realtimeSinceStartup, next=0;
            while(owner.Session.State.Phase.ToString()==phase) {
                if(Time.realtimeSinceStartup-start>=next) {
                    motion.Add(new Pose{time=Time.realtimeSinceStartup-start,phase=phase,position=guest.transform.position,mesh=guest.Plane.sharedMesh,material=guest.Renderer.sharedMaterial,seated=guest.Plane.sharedMesh==guest.SeatedMesh});
                    next+=1f/12;
                }
                yield return null;
            }
            motion.Add(new Pose{time=Time.realtimeSinceStartup-start,phase=phase+"End",position=guest.transform.position,mesh=guest.Plane.sharedMesh,material=guest.Renderer.sharedMaterial,seated=guest.Plane.sharedMesh==guest.SeatedMesh});
        }
        private IEnumerator RecordStill(string phase,float duration)
        {
            float start=Time.realtimeSinceStartup;
            while(Time.realtimeSinceStartup-start<duration) {
                motion.Add(new Pose{time=Time.realtimeSinceStartup-start,phase=phase,position=guest.transform.position,mesh=guest.Plane.sharedMesh,material=guest.Renderer.sharedMaterial,seated=guest.Plane.sharedMesh==guest.SeatedMesh});
                yield return new WaitForSecondsRealtime(1f/12);
            }
        }
        private void Capture(string frame,int width)
        {
            string file="Q1"+"_"+actorName+"_"+frame+"_"+width+".png";
            SavePixels(Path.Combine(output,file),width);
            var camera=owner.Camera;
            var bounds=guest.Renderer.bounds; var floor=GameObject.Find("C_Floor").GetComponent<Renderer>().bounds;
            shots.Add(new Shot{file=file,candidate="Q1",fixture=frame,width=width,height=width*9/16,position=camera.transform.position,rotation=camera.transform.eulerAngles,orthographic=camera.orthographic,size=camera.orthographicSize,fov=camera.fieldOfView,characterViewport=Viewport(bounds),barViewport=Viewport(floor),phase=owner.Session.State.Phase.ToString(),sequence=owner.Session.State.Sequence});
        }
        private Rect Viewport(Bounds b)
        {
            var p=new List<Vector3>();
            foreach(int x in new[]{-1,1}) foreach(int y in new[]{-1,1}) foreach(int z in new[]{-1,1}) p.Add(owner.Camera.WorldToViewportPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));
            return Rect.MinMaxRect(p.Min(v=>v.x),p.Min(v=>v.y),p.Max(v=>v.x),p.Max(v=>v.y));
        }
        private IEnumerator MeasureIdle(int width)
        {
            var target=RenderTexture.GetTemporary(width,width*9/16,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
            UnityEngine.Application.targetFrameRate=-1; QualitySettings.vSyncCount=0;
            for(int i=0;i<60;i++) { RenderPipeline.SubmitRenderRequest(owner.Camera,request); yield return null; }
            var samples=new List<float>(100000); long gcSum=0,gcMax=0,drawSum=0,batchSum=0;
            using(var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame"))
            using(var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count"))
            using(var batches=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count")) {
                float start=Time.realtimeSinceStartup;
                while(Time.realtimeSinceStartup-start<10) {
                    RenderPipeline.SubmitRenderRequest(owner.Camera,request); yield return null; samples.Add(Time.unscaledDeltaTime*1000);
                    if(gc.Valid) { gcSum+=gc.LastValue; gcMax=Math.Max(gcMax,gc.LastValue); }
                    if(draws.Valid) drawSum+=draws.LastValue; if(batches.Valid) batchSum+=batches.LastValue;
                }
                samples.Sort(); var renderers=FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled && r.gameObject.activeInHierarchy).ToArray();
                measures.Add(new Measure{candidate="Q1",width=width,samples=samples.Count,seconds=Time.realtimeSinceStartup-start,p50=samples[samples.Count/2],p95=samples[(int)(samples.Count*.95)],p99=samples[(int)(samples.Count*.99)],gcMean=gc.Valid ? (double)gcSum/samples.Count : -1,gcMax=gcMax,draws=draws.Valid ? (double)drawSum/samples.Count : -1,batches=batches.Valid ? (double)batchSum/samples.Count : -1,renderers=renderers.Length,materials=renderers.SelectMany(r=>r.sharedMaterials).Distinct().Count(),allocated=Profiler.GetTotalAllocatedMemoryLong(),reserved=Profiler.GetTotalReservedMemoryLong(),workingSet=System.Diagnostics.Process.GetCurrentProcess().WorkingSet64,lights=FindObjectsByType<Light>(FindObjectsSortMode.None).Count(l=>l.enabled),shadowLights=FindObjectsByType<Light>(FindObjectsSortMode.None).Count(l=>l.enabled && l.shadows!=LightShadows.None)});
            }
            RenderTexture.ReleaseTemporary(target); UnityEngine.Application.targetFrameRate=60;
        }
        private void SavePixels(string path,int width)
        {
            var camera=owner.Camera; var target=RenderTexture.GetTemporary(width,width*9/16,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var previous=RenderTexture.active; var pixels=new Texture2D(width,width*9/16,TextureFormat.RGB24,false);
            try { camera.aspect=16f/9; Canvas.ForceUpdateCanvases(); RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target}); RenderTexture.active=target; pixels.ReadPixels(new Rect(0,0,width,width*9/16),0,0); pixels.Apply(); File.WriteAllBytes(path,pixels.EncodeToPNG()); }
            finally { RenderTexture.active=previous; RenderTexture.ReleaseTemporary(target); Destroy(pixels); }
        }
        private void Check(bool value,string message) { if(!value) throw new Exception(message); checks.Add(message); }
        private void CheckFrame(bool value) { if(!value) throw new Exception("Replay actor visibility changed outside the recorded pose"); }
        private void Log(string message,string stack,LogType kind) { if(kind==LogType.Error || kind==LogType.Exception || kind==LogType.Assert) { errors.Add(message); Finish(); } }
        private void Finish()
        {
            if(done) return; done=true;
            File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(new Result{utc=DateTime.UtcNow.ToString("O"),actor=actorName,unity=UnityEngine.Application.unityVersion,gpu=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,checks=checks.ToArray(),errors=errors.ToArray(),shots=shots.ToArray(),measurements=measures.ToArray(),motion=motion.Select(p=>new PoseRecord{time=p.time,phase=p.phase,position=p.position,seated=p.seated,mesh=p.mesh.name,material=p.material.name}).ToArray()},true));
            UnityEngine.Application.Quit(errors.Count==0 ? 0 : 1);
        }
        private sealed class Pose { public float time; public string phase; public Vector3 position; public bool seated; public Mesh mesh; public Material material; }
        [Serializable] private sealed class PoseRecord { public float time; public string phase,mesh,material; public Vector3 position; public bool seated; }
        [Serializable] private sealed class Shot { public string file,candidate,fixture,phase; public int width,height; public long sequence; public Vector3 position,rotation; public bool orthographic; public float size,fov; public Rect characterViewport,barViewport; }
        [Serializable] private sealed class Measure { public string candidate; public int width,samples,renderers,materials,lights,shadowLights; public float seconds,p50,p95,p99; public double gcMean,draws,batches; public long gcMax,allocated,reserved,workingSet; }
        [Serializable] private sealed class Result { public string utc,actor,unity,gpu,cpu; public string[] checks,errors; public Shot[] shots; public Measure[] measurements; public PoseRecord[] motion; }
    }
}
