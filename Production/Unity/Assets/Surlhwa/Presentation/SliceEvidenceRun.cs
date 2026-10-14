using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using Unity.Collections;
using Surlhwa.Domain;
using Surlhwa.ContentIO;

namespace Surlhwa.Presentation
{
    // Development evidence only. Automated commands enter through Input System devices,
    // never through BrewingSession, SliceSession, UI callbacks, or checkpoint injection.
    public sealed class SliceEvidenceRun : MonoBehaviour
    {
        private SliceController owner; private string output,fixture,runId; private readonly List<float> frames=new List<float>(); private readonly List<string> errors=new List<string>();
        private float nextObservation; private bool capturing,stopping; private int captureIndex; private Keyboard keyboard; private Mouse mouse;
        private Vector2 pointer; private Key[] keys=System.Array.Empty<Key>(); private bool down; private float start;
        private RenderTexture renderTarget; private bool media,audioRecording; private int mediaFrame; private float mediaStart,nextMedia;
        private bool nativeArmed,nativeInterrupted;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
#endif
        private static int ForegroundProcess()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            GetWindowThreadProcessId(GetForegroundWindow(),out uint process); return (int)process;
#else
            return 0;
#endif
        }
        private readonly List<short> pcm=new List<short>(); private readonly List<double> saveTimes=new List<double>(); private long observedSequence=-1; private string observedRun;
        private readonly List<string> lifecycleRuns=new List<string>();
        private readonly List<double> lifecycleMemory=new List<double>(),transitions=new List<double>(); private int channels;
        private string Argument(string name) { var args=Environment.GetCommandLineArgs(); int i=System.Array.IndexOf(args,name); return i>=0 && i+1<args.Length ? args[i+1] : null; }
        private IEnumerator Start()
        {
            owner=GetComponent<SliceController>(); output=Argument("-sliceEvidenceRoot"); fixture=Argument("-sliceProof"); runId=Argument("-sliceRunId");
            if(output==null) yield break;
            output=Path.GetFullPath(output); Directory.CreateDirectory(output); start=Time.realtimeSinceStartup;
            UnityEngine.Application.logMessageReceived+=Log;
            while(owner.Ui==null || owner.Busy) { if(Time.realtimeSinceStartup-start>45) { Fail("Initialization timeout"); yield break; } yield return null; }
            if(fixture==null) yield break;
            renderTarget=new RenderTexture(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB); renderTarget.Create();
            keyboard=InputSystem.AddDevice<Keyboard>(); mouse=InputSystem.AddDevice<Mouse>();
            QualitySettings.vSyncCount=0; UnityEngine.Application.targetFrameRate=60;
            media=Argument("-sliceMedia")=="1";
            if(media) { Directory.CreateDirectory(Path.Combine(output,"frames")); channels=AudioSettings.speakerMode==AudioSpeakerMode.Mono ? 1 : 2; audioRecording=AudioRenderer.Start(); mediaStart=Time.realtimeSinceStartup; }
            if(fixture=="ui-review") yield return UiReview(); else if(fixture=="lifecycle") yield return Lifecycle(); else if(fixture=="continue") yield return ContinueProof(); else if(fixture=="save-error") yield return SaveFailure(); else yield return Proof();
        }
        private void LateUpdate()
        {
            if(renderTarget!=null && owner.Ui!=null) { Canvas.ForceUpdateCanvases(); RenderPipeline.SubmitRenderRequest(owner.Camera,new UniversalRenderPipeline.SingleCameraRequest{destination=renderTarget}); }
            if(audioRecording) {
                int count=AudioRenderer.GetSampleCountForCaptureFrame()*channels;
                using(var buffer=new NativeArray<float>(count,Allocator.Temp)) { if(!AudioRenderer.Render(buffer)) errors.Add("AudioRenderer rejected output capture"); for(int i=0;i<count;i++) pcm.Add((short)Mathf.RoundToInt(Mathf.Clamp(buffer[i],-1,1)*32767)); }
            }
            if(media && Time.realtimeSinceStartup>=nextMedia) {
                nextMedia=Time.realtimeSinceStartup+.1f; var previous=RenderTexture.active; RenderTexture.active=renderTarget;
                var texture=new Texture2D(renderTarget.width,renderTarget.height,TextureFormat.RGB24,false); texture.ReadPixels(new Rect(0,0,renderTarget.width,renderTarget.height),0,0); texture.Apply(); RenderTexture.active=previous;
                File.WriteAllBytes(Path.Combine(output,"frames",(++mediaFrame).ToString("D5")+".png"),texture.EncodeToPNG()); Destroy(texture);
                File.AppendAllText(Path.Combine(output,"frame-times.csv"),mediaFrame+","+(Time.realtimeSinceStartup-mediaStart).ToString(System.Globalization.CultureInfo.InvariantCulture)+"\n");
            }
        }
        private void Log(string text,string stack,LogType type) { if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) errors.Add(text); }
        private void Update()
        {
            if(output==null || owner==null) return;
            if(fixture==null) {
                int process=System.Diagnostics.Process.GetCurrentProcess().Id; bool owned=ForegroundProcess()==process && UnityEngine.Application.isFocused;
                if(nativeArmed && !owned && !nativeInterrupted) { nativeInterrupted=true; File.WriteAllText(Path.Combine(output,"OWNER_INTERFERENCE.json"),"{\"classification\":\"OWNER_INTERFERENCE / INTERRUPTED_EVIDENCE\",\"reason\":\"Foreground or application focus changed after ownership was established\"}"); }
                if(owned) nativeArmed=true;
                if(owned && !nativeInterrupted && Keyboard.current!=null) foreach(var key in Keyboard.current.allKeys.Where(x=>x.wasPressedThisFrame)) File.AppendAllText(Path.Combine(output,"native-input-observed.jsonl"),"{\"key\":\""+key.keyCode+"\",\"owned\":true,\"seconds\":"+Time.realtimeSinceStartup.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}\n");
            }
            if(owner.Session!=null && !owner.Session.IsSaving && (owner.Session.State.Sequence!=observedSequence || owner.Session.State.RunId!=observedRun)) { observedSequence=owner.Session.State.Sequence; observedRun=owner.Session.State.RunId; saveTimes.Add(owner.LastSaveMilliseconds); if(owner.LastSaveStages!=null) File.AppendAllText(Path.Combine(output,"save-stages.csv"),observedRun+","+observedSequence+","+string.Join(",",owner.LastSaveStages.Select(x=>x.Key+"="+x.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)))+"\n"); }
            if(!capturing) frames.Add(Time.unscaledDeltaTime*1000);
            if(owner.Ui!=null && Time.realtimeSinceStartup>nextObservation) { nextObservation=Time.realtimeSinceStartup+.5f; Observe(); }
            if(fixture==null && Keyboard.current?.f12Key.wasPressedThisFrame==true) StartCoroutine(Capture("native-"+(owner.Session?.State.Phase.ToString() ?? "menu")));
        }
        [Serializable] private sealed class Observation
        {
            public string observedUtc,runId,evidenceTier,phase,modal,ingredient,target,recipe,recognition,preparation,saveError,textRole,displayedSpeaker; public int encounter,services,known,memories,eventSystems,audioListeners;
            public long sequence; public bool fits,quality,actComplete,saving,physicalLoop,flatTools,cocktailGlass;
            public int processId,foregroundProcessId; public bool focused,nativeOwnershipEstablished,nativeInterrupted;
            public double jigger,mixing,serving,stirDegrees; public string[] visibleText,overflowText;
        }
        private void Observe()
        {
            var s=owner.Session; var outcome=s?.State.CurrentService?.Outcome; var texts=owner.Ui.GetComponentsInChildren<TextMeshProUGUI>().Where(x=>x.isActiveAndEnabled).ToArray();
            var o=new Observation{runId=runId,evidenceTier=fixture==null ? "V5 native OS input / observational probe" : "V4 simulated Input System events / native Player rendering",phase=s?.State.Phase.ToString() ?? "menu",
                observedUtc=DateTime.UtcNow.ToString("o"),processId=System.Diagnostics.Process.GetCurrentProcess().Id,foregroundProcessId=ForegroundProcess(),focused=UnityEngine.Application.isFocused,nativeOwnershipEstablished=nativeArmed,nativeInterrupted=nativeInterrupted,
                textRole=s?.CurrentText?.Role,displayedSpeaker=owner.Ui.DisplayedSpeaker,eventSystems=FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length,audioListeners=FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(x=>x.isActiveAndEnabled),
                sequence=s?.State.Sequence ?? 0,encounter=s?.State.EncounterIndex ?? -1,modal=s?.Modals.Top ?? owner.Ui.MenuModal,
                ingredient=owner.Input.SelectedIngredient,target=owner.Input.Target.ToString(),services=s?.State.Services.Count ?? 0,known=s?.Knowledge.KnownReceipts.Count ?? 0,
                memories=s?.State.MemoryIds.Count ?? 0,actComplete=s?.State.ActComplete ?? false,saving=s?.IsSaving ?? owner.Busy,
                recipe=outcome?.RecipeId,recognition=outcome?.Recognition.ToString(),preparation=outcome?.Preparation.ToString(),fits=outcome?.Fits ?? false,quality=outcome?.QualityGood ?? false,
                jigger=(double)(s?.Brew?.Container(ContainerRole.Jigger).TotalMl ?? 0),mixing=(double)(s?.Brew?.Container(ContainerRole.Mixing).TotalMl ?? 0),serving=(double)(s?.Brew?.Container(ContainerRole.Serving).TotalMl ?? s?.State.CurrentService?.Snapshot.TotalMl ?? 0),
                stirDegrees=s?.Brew?.StirAcceptedDegrees ?? 0,physicalLoop=owner.Audio.PhysicalPlaying,flatTools=owner.Workbench.FlatTools,cocktailGlass=owner.Workbench.CocktailGlass,saveError=s?.SaveError ?? owner.StartupError,
                visibleText=texts.Select(x=>x.text).ToArray(),overflowText=texts.Where(x=>!string.IsNullOrWhiteSpace(x.text) && x.isTextOverflowing).Select(x=>x.text).ToArray()};
            string path=Path.Combine(output,"observation.json"),temp=path+".pending"; File.WriteAllText(temp,JsonUtility.ToJson(o,true)); if(File.Exists(path)) File.Replace(temp,path,null); else File.Move(temp,path);
        }
        private IEnumerator Capture(string label)
        {
            capturing=true; yield return new WaitForEndOfFrame();
            string name=(++captureIndex).ToString("D2")+"-"+label; Texture2D texture;
            if(renderTarget!=null) {
                var previous=RenderTexture.active; RenderTexture.active=renderTarget; texture=new Texture2D(renderTarget.width,renderTarget.height,TextureFormat.RGB24,false);
                texture.ReadPixels(new Rect(0,0,renderTarget.width,renderTarget.height),0,0); texture.Apply(); RenderTexture.active=previous;
            } else texture=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG()); Destroy(texture); Observe();
            File.Copy(Path.Combine(output,"observation.json"),Path.Combine(output,name+".json"),true);
            capturing=false;
        }
        private void Queue()
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys)); var state=new MouseState{position=pointer}; state=state.WithButton(MouseButton.Left,down); InputSystem.QueueStateEvent(mouse,state);
        }
        private IEnumerator Press(Key key,float seconds=.12f)
        {
            keys=new[]{key}; Queue(); File.AppendAllText(Path.Combine(output,"input-events.jsonl"),"{\"key\":\""+key+"\",\"seconds\":"+seconds.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}\n");
            yield return new WaitForSecondsRealtime(seconds); keys=System.Array.Empty<Key>(); Queue(); yield return new WaitForSecondsRealtime(.1f);
            while(owner.Busy || owner.Session?.IsSaving==true) yield return null;
        }
        private IEnumerator Click(float x,float y)
        { pointer=new Vector2(x*Screen.width/1280,y*Screen.height/720); down=false; Queue(); yield return null; down=true; Queue(); yield return new WaitForSecondsRealtime(.12f); down=false; Queue(); yield return new WaitForSecondsRealtime(.3f); while(owner.Busy || owner.Session?.IsSaving==true) yield return null; }
        private IEnumerator WaitPhase(SlicePhase phase)
        {
            float until=Time.realtimeSinceStartup+20;
            while(owner.Session==null || owner.Session.State.Phase!=phase || owner.Session.IsSaving)
            { if(Time.realtimeSinceStartup>until) { Fail("Expected phase "+phase+"; actual "+owner.Session?.State.Phase); yield break; } yield return null; }
            yield return new WaitForSecondsRealtime(.15f);
        }
        private bool FocusShapeVisible()
        {
            var selected=UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
            var marker=selected!=null ? selected.transform.Find("Focus outline") : null;
            return marker!=null && marker.gameObject.activeInHierarchy && marker.GetComponentsInChildren<UnityEngine.UI.Image>().All(x=>!x.raycastTarget);
        }
        private IEnumerator UiReview()
        {
            yield return null;
            if(!FocusShapeVisible()) { Fail("Initial keyboard focus has no non-raycasting outline"); yield break; }
            yield return Capture("menu-focus");
            yield return Click(430,238); yield return WaitPhase(SlicePhase.Welcome); yield return ToBrew();
            long sequence=owner.Session.State.Sequence;
            yield return Capture("brew-layout");
            foreach(var key in new[]{Key.B,Key.Tab,Key.Escape}) {
                yield return Press(key); yield return null;
                if(!FocusShapeVisible() || !owner.Ui.ModalOwnsFocus) { Fail("Modal focus outline missing or outside modal"); yield break; }
                yield return Capture(key==Key.B ? "guide-functional" : key==Key.Tab ? "codex-visually-not-approved" : "settings-focus");
                if(key==Key.Escape) {
                    var selected=UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
                    yield return Press(Key.LeftArrow); yield return null;
                    if(selected==UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject || !FocusShapeVisible() || !owner.Ui.ModalOwnsFocus) { Fail("Keyboard navigation did not stay within the modal"); yield break; }
                    yield return Capture("settings-keyboard-navigation");
                }
                yield return Press(Key.Escape);
            }
            if(owner.Session.State.Sequence!=sequence || owner.Session.Brew.Container(ContainerRole.Jigger).TotalMl!=0 || owner.Session.Modals.Top!=null) { Fail("UI review changed gameplay or retained a modal"); yield break; }
            yield return Capture("returned-to-brew"); Finish(true,null); UnityEngine.Application.Quit(0);
        }
        private IEnumerator ToBrew()
        {
            if(owner.Session.State.Phase==SlicePhase.Welcome) yield return Press(Key.Enter);
            yield return WaitPhase(SlicePhase.Intro);
            if(Argument("-sliceStopAt")=="intro") { yield return Press(Key.Enter); yield return Capture("intro-checkpoint"); stopping=true; Finish(true,null); UnityEngine.Application.Quit(0); yield break; }
            while(owner.Session.State.Phase==SlicePhase.Intro) { yield return Press(Key.Enter); yield return new WaitForSecondsRealtime(.1f); }
            yield return Capture("request-"+owner.Session.State.EncounterIndex);
            yield return Press(Key.Enter); yield return WaitPhase(SlicePhase.Brewing);
        }
        private IEnumerator Choose(string ingredient)
        { int n=owner.Content.Profile.Supply.ToList().IndexOf(ingredient)+1; yield return Press((Key)Enum.Parse(typeof(Key),"Digit"+n)); }
        private IEnumerator Measure(float ml)
        {
            keys=new[]{Key.Q}; Queue(); yield return null;
            while(owner.Session.Brew.Container(ContainerRole.Jigger).TotalMl<(decimal)ml-1.5m) yield return null;
            keys=System.Array.Empty<Key>(); Queue(); yield return null;
            keys=new[]{Key.Q,Key.LeftShift}; Queue(); yield return null;
            while(owner.Session.Brew.Container(ContainerRole.Jigger).TotalMl<(decimal)ml-.04m) yield return null;
            keys=System.Array.Empty<Key>(); Queue(); yield return new WaitForSecondsRealtime(.1f);
        }
        private IEnumerator Make(string name,bool weak=false)
        {
            while(owner.Input.Target!=ContainerRole.Jigger) yield return Press(Key.J);
            if(name=="unknown") { yield return Choose("water"); yield return Press(Key.J); yield return Press(Key.J); yield return Press(Key.Q,.3f); yield break; }
            var amounts=name=="seolya" ? new[]{("whiskey",59.14706f),("vermouth",29.57353f),("water",29.57353f)} : name=="dry" ? new[]{("gin",60f),("dry_vermouth",10f)} : new[]{("rye_whiskey",50f),("sweet_red_vermouth",20f)};
            foreach(var amount in amounts) { yield return Choose(amount.Item1); yield return Measure(amount.Item2); yield return Press(Key.E,2); }
            yield return Press(Key.J); // Jigger -> Mixing
            if(name=="manhattan") { yield return Choose("angostura_bitters"); yield return Press(Key.D); }
            if(!weak)
            {
                yield return Press(Key.I); var center=(Vector2)owner.Camera.WorldToScreenPoint(owner.Workbench.MixingCenter); float radius=Screen.height/(2*owner.Camera.orthographicSize)*.25f;
                pointer=center+Vector2.right*radius; Queue(); yield return null; keys=new[]{Key.S}; Queue(); yield return null;
                float elapsed=0; while(elapsed<7.3f) { elapsed+=Time.unscaledDeltaTime; float angle=elapsed*Mathf.PI; pointer=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius; Queue(); yield return null; }
                keys=System.Array.Empty<Key>(); Queue(); yield return new WaitForSecondsRealtime(.2f);
            }
            yield return Press(Key.R,4); if(name=="manhattan") yield return Press(Key.C);
        }
        private IEnumerator Proof()
        {
            yield return Capture("menu"); yield return Click(430,238); yield return WaitPhase(SlicePhase.Welcome);
            if(fixture=="missing-actor") { owner.Ordinary.Plane=null; owner.Prince.WalkMaterials=null; }
            for(int n=0;n<2;n++)
            {
                yield return ToBrew(); if(stopping) yield break; string drink=n==1 || fixture=="weak" || fixture=="missing-actor" ? "manhattan" : fixture;
                if(n==0) { var checkpoint=owner.Session.State; yield return Press(Key.Q,.2f); yield return Click(110,174); if(!ReferenceEquals(checkpoint,owner.Session.State) || owner.Session.Brew.Container(ContainerRole.Jigger).TotalMl!=0 || owner.Session.Brew.Freeze().Trace.Count!=0) { Fail("Discard changed story or retained transient state"); yield break; } }
                if(fixture=="checkpoint-brew") { yield return Press(Key.Q,.4f); yield return Capture("checkpoint-brew"); Finish(true,null); UnityEngine.Application.Quit(0); yield break; }
                yield return Press(Key.B); yield return Capture("guide-before-"+n); yield return Press(Key.Escape);
                yield return Make(drink,n==0 && fixture=="weak"); yield return Capture("prepared-"+n);
                yield return Click(761,59); yield return Capture("flat-tools-"+n); yield return Click(761,59);
                yield return Click(1157,115); yield return WaitPhase(SlicePhase.Reaction); yield return Capture("reaction-"+n);
                var result=owner.Session.State.CurrentService.Outcome;
                string expectedRecipe=drink=="seolya" ? "recipe_test_stir" : drink=="dry" ? "recipe_dry_martini" : drink=="unknown" ? null : "recipe_manhattan";
                if(result.RecipeId!=expectedRecipe || result.Fits!=(drink!="dry" && drink!="unknown")) { Fail("Unexpected WHAT/FIT for "+drink); yield break; }
                if(n==1 && (result.RecipeId!="recipe_manhattan" || result.Preparation!=PreparationResult.Good || !result.QualityGood || !result.Fits)) { Fail("Second guest's literal Manhattan fixture was not prepared correctly"); yield break; }
                if(n==0 && ((fixture=="unknown" && result.Recognition!=DrinkRecognition.Unknown) || (fixture=="weak" && result.Preparation!=PreparationResult.Weak) ||
                    (fixture!="unknown" && fixture!="weak" && (result.Preparation!=PreparationResult.Good || !result.QualityGood || result.Fits!=(fixture!="dry"))))) { Fail("Unexpected literal fixture outcome: "+fixture); yield break; }
                yield return Press(Key.Enter); yield return Capture("result-"+n);
                if(n==0 && Argument("-sliceStopAt")=="result") { Finish(true,null); UnityEngine.Application.Quit(0); yield break; }
                yield return Press(Key.Enter);
                if(owner.Session.State.Phase==SlicePhase.Discovery) { yield return Capture("discovery"); yield return Press(Key.Enter); }
                while(owner.Session.State.Phase==SlicePhase.Farewell) { yield return Capture("farewell-"+n); if(n==0 && Argument("-sliceStopAt")=="farewell") { Finish(true,null); UnityEngine.Application.Quit(0); yield break; } yield return Press(Key.Enter); }
                yield return WaitPhase(n==0 ? SlicePhase.Intro : SlicePhase.Complete);
            }
            yield return Capture("complete");
            if(owner.Session.State.Services.Count!=2 || owner.Session.State.MemoryIds.Count!=0 || owner.Session.State.ActComplete || errors.Count>0) { Fail("Completion or runtime error check failed"); yield break; }
            Finish(true,null); UnityEngine.Application.Quit(0);
        }
        private IEnumerator ContinueProof()
        {
            yield return Capture("continue-menu"); var saved=owner.Saved?.Checkpoint;
            if(saved==null) { Fail("Continue requires an actual earlier Player checkpoint"); yield break; }
            yield return Click(634,238); yield return new WaitForSecondsRealtime(.3f); yield return Capture("continued");
            var state=owner.Session?.State;
            if(state==null || SliceSaveCodec.Encode(state,owner.Content)!=SliceSaveCodec.Encode(saved,owner.Content) || state.MemoryIds.Count!=0 || state.ActComplete || (owner.Session.Brew!=null && (Enum.GetValues(typeof(ContainerRole)).Cast<ContainerRole>().Any(x=>owner.Session.Brew.Container(x).TotalMl!=0) || owner.Session.Brew.Freeze().Trace.Count!=0 || owner.Session.Brew.StirAcceptedDegrees!=0))) { Fail("Continue changed the durable cursor/outcome or restored transient liquid"); yield break; }
            Finish(true,null); UnityEngine.Application.Quit(0);
        }
        private IEnumerator SaveFailure()
        {
            yield return Click(430,238); yield return WaitPhase(SlicePhase.Welcome); yield return ToBrew(); yield return Make("unknown");
            Action<string> fail=point=>{if(point=="before_write") throw new IOException("Injected write failure for recovery validation");};
            owner.EvidenceStore.Fault=fail; yield return Click(1157,115); yield return Capture("save-failed");
            if(owner.Session.SaveError==null || owner.Session.State.Services.Count!=0 || owner.Ui.VisibleModal!="save_error") { Fail("Failed write fabricated service or omitted recovery"); yield break; }
            owner.EvidenceStore.Fault=null; yield return Press(Key.Enter); yield return WaitPhase(SlicePhase.Reaction); yield return Capture("retry-published-once");
            var durable=SliceSaveCodec.Encode(owner.Session.State,owner.Content);
            owner.EvidenceStore.Fault=fail; yield return Press(Key.Enter); yield return Capture("reading-save-failed");
            if(owner.Session.SaveError==null) { Fail("Second injected failure was not reached"); yield break; }
            yield return Press(Key.RightArrow); yield return Press(Key.Enter); yield return Capture("returned-to-saved-menu"); owner.EvidenceStore.Fault=null;
            if(owner.Session!=null || owner.Saved?.Checkpoint==null || SliceSaveCodec.Encode(owner.Saved.Checkpoint,owner.Content)!=durable) { Fail("Exit from error lost durable outcome"); yield break; }
            yield return Click(634,238); yield return Capture("error-continued");
            if(SliceSaveCodec.Encode(owner.Session.State,owner.Content)!=durable || owner.Session.State.Services.Count!=1) { Fail("Error Continue duplicated service"); yield break; }
            Finish(errors.Count==0,null); UnityEngine.Application.Quit(errors.Count==0 ? 0 : 1);
        }
        private IEnumerator Lifecycle()
        {
            yield return Capture("idle-start"); frames.Clear(); yield return new WaitForSecondsRealtime(60); yield return Capture("idle-end");
            for(int i=0;i<20;i++) {
                if(i==0) yield return Click(430,238);
                else { yield return Press(Key.Escape); yield return Click(440,262); yield return Click(440,262); }
                yield return WaitPhase(SlicePhase.Welcome);
                lifecycleRuns.Add(owner.Session.State.RunId);
                yield return ModalTransition(Key.B,"guide"); yield return ModalTransition(Key.Escape,null);
                yield return null; lifecycleMemory.Add(UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576d);
                if(owner.Session.State.Services.Count!=0 || owner.Session.Knowledge.KnownReceipts.Count!=2 || owner.Session.State.MemoryIds.Count!=0) { Fail("New Game leaked prior run progress"); yield break; }
            }
            if(lifecycleRuns.Distinct().Count()!=20) { Fail("Restart did not create twenty distinct runs"); yield break; }
            yield return Press(Key.Escape); yield return Click(440,336); yield return Capture("large-text-settings"); yield return Press(Key.Escape); yield return Press(Key.B); yield return Capture("large-text-guide"); yield return Press(Key.Escape);
            yield return Capture("lifecycle-complete"); Finish(errors.Count==0,null); UnityEngine.Application.Quit(errors.Count==0 ? 0 : 1);
        }
        private IEnumerator ModalTransition(Key key,string expected)
        {
            float before=Time.realtimeSinceStartup; keys=new[]{key}; Queue();
            do { yield return new WaitForEndOfFrame(); if(Time.realtimeSinceStartup-before>5) { Fail("Modal transition timeout"); yield break; } } while(owner.Ui.VisibleModal!=expected);
            transitions.Add((Time.realtimeSinceStartup-before)*1000); keys=System.Array.Empty<Key>(); Queue(); yield return new WaitForSecondsRealtime(.1f);
        }
        [Serializable] private sealed class Report { public bool passed; public string runId,fixture,tier,error,graphics,cpu,unity; public int width,height,frames; public double seconds,p50ms,p95ms,p99ms,allocatedMb; public double[] saveMilliseconds,lifecycleAllocatedMb,uiRoundTripMilliseconds; public string[] errors,lifecycleRunIds; }
        private void Finish(bool pass,string error)
        {
            var sample=frames.OrderBy(x=>x).ToArray(); double Percent(double n)=>sample.Length==0 ? 0 : sample[Math.Min(sample.Length-1,(int)(sample.Length*n))];
            if(audioRecording) { AudioRenderer.Stop(); audioRecording=false; using(var w=new BinaryWriter(File.Create(Path.Combine(output,"unity-mix.wav")))) { int bytes=pcm.Count*2,rate=AudioSettings.outputSampleRate; w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36+bytes); w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)channels); w.Write(rate); w.Write(rate*channels*2); w.Write((short)(channels*2)); w.Write((short)16); w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(bytes); foreach(short value in pcm) w.Write(value); } }
            File.WriteAllText(Path.Combine(output,"run.json"),JsonUtility.ToJson(new Report{passed=pass,runId=runId,fixture=fixture,tier="V4 Input System events; V6 native offscreen camera/UI render",error=error,graphics=SystemInfo.graphicsDeviceName+" / "+SystemInfo.graphicsDeviceType,
                cpu=SystemInfo.processorType,unity=UnityEngine.Application.unityVersion,width=Screen.width,height=Screen.height,frames=frames.Count,seconds=Time.realtimeSinceStartup-start,p50ms=Percent(.5),p95ms=Percent(.95),p99ms=Percent(.99),allocatedMb=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576d,saveMilliseconds=saveTimes.ToArray(),lifecycleAllocatedMb=lifecycleMemory.ToArray(),uiRoundTripMilliseconds=transitions.ToArray(),lifecycleRunIds=lifecycleRuns.ToArray(),errors=errors.ToArray()},true));
        }
        private void Fail(string message) { Finish(false,message); Debug.LogError("SLICE_PROOF_FAILED: "+message); UnityEngine.Application.Quit(1); }
        private void OnDestroy() { UnityEngine.Application.logMessageReceived-=Log; if(renderTarget!=null) { renderTarget.Release(); Destroy(renderTarget); } }
    }
}
