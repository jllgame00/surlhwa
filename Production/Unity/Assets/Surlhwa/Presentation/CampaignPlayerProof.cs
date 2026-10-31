using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Unity.Collections;
using Surlhwa.Domain;
namespace Surlhwa.Presentation
{
    // Explicit simulated-input tier. Never invoked by the OS-native harness.
    // Every tested action goes through the same UI/InputSystem adapter as the player.
    [DefaultExecutionOrder(1100)]
    public sealed class CampaignPlayerProof : MonoBehaviour
    {
        private CampaignController owner; private string output,fixture; private Keyboard keyboard; private Mouse mouse;
        private Vector2 pointer; private Key[] keys=Array.Empty<Key>(); private bool down,failed; private RenderTexture render;
        private bool stirInput; private Vector2 stirCenter; private float stirRadius,stirAngle;
        private bool finePourInput; private ContainerRole fineRole; private decimal fineStart,fineTarget; private Key fineKey;
        private readonly List<string> checks=new List<string>(),errors=new List<string>(); private readonly List<float> frames=new List<float>(); private int captures;
        private bool media,audioRecording; private BinaryWriter audio; private int audioChannels,audioBytes,mediaFrame; private float mediaStart,nextMedia;
        private bool expanded,act3,act4,deliberateFailure,retryObserved,quixoteMismatchObserved,memoryThreeCaptured,frenchFailure,frenchRetryObserved,hamletMismatchObserved;
        private int expectedServices,startSlot,startDepartures,lastDepartures,totalSlots,expectedMemories;
        private string[] suppliedIngredients=Array.Empty<string>(); private readonly HashSet<string> shelfSelections=new HashSet<string>();
        private string startPhase,proofPrefix; private readonly HashSet<string> archived=new HashSet<string>(); private readonly HashSet<int> visited=new HashSet<int>();
        private string Arg(string key) { var args=Environment.GetCommandLineArgs(); int i=Array.IndexOf(args,key); return i<0 || i+1>=args.Length ? null : args[i+1]; }
        private IEnumerator Start()
        {
            fixture=Arg("-campaignProof"); if(fixture==null) { enabled=false; yield break; }
            output=Path.GetFullPath(Arg("-campaignEvidenceRoot")); Directory.CreateDirectory(output); owner=GetComponent<CampaignController>();
            UnityEngine.Application.logMessageReceived+=Log; float until=Time.realtimeSinceStartup+40;
            while(owner.Ui==null || owner.Busy) { if(Time.realtimeSinceStartup>until) { Fail("Initialization timeout"); yield break; } yield return null; }
            act4=owner.Content.Scope=="through_act_04"; act3=act4 || owner.Content.Scope=="through_act_03"; expanded=act3 || owner.Content.Scope=="through_act_02";
            totalSlots=owner.Content.Slots.Count; expectedMemories=owner.Content.ActAt(totalSlots-1); proofPrefix=act4 ? "act4" : act3 ? "act3" : "act2";
            keyboard=InputSystem.AddDevice<Keyboard>(); mouse=InputSystem.AddDevice<Mouse>();
            InputSystem.onBeforeUpdate+=QueueStirForCurrentFrame;
            render=new RenderTexture(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB); render.Create();
            media=Arg("-campaignMedia")=="1";
            if(media) {
                Directory.CreateDirectory(Path.Combine(output,"frames")); audioChannels=AudioSettings.speakerMode==AudioSpeakerMode.Mono ? 1 : 2; audioRecording=AudioRenderer.Start();
                if(!audioRecording) { Fail("AudioRenderer unavailable"); yield break; }
                audio=new BinaryWriter(File.Create(Path.Combine(output,"unity-mix.wav"))); int rate=AudioSettings.outputSampleRate;
                audio.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); audio.Write(0); audio.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); audio.Write(16); audio.Write((short)1); audio.Write((short)audioChannels); audio.Write(rate); audio.Write(rate*audioChannels*2); audio.Write((short)(audioChannels*2)); audio.Write((short)16); audio.Write(System.Text.Encoding.ASCII.GetBytes("data")); audio.Write(0); mediaStart=Time.realtimeSinceStartup;
            }
            yield return Capture("menu"); yield return Click("도감  Tab"); yield return Capture("codex-initial"); yield return Click("돌아가기  Esc");
            yield return Click(fixture.StartsWith("continue",StringComparison.Ordinal) || fixture=="quiet" ? "이어 하기" : "새로 시작"); yield return Idle();
            string[] plan={"recipe_test_stir","recipe_test_stir","recipe_test_stir","recipe_dry_martini","recipe_dry_martini","recipe_manwol","recipe_negroni","recipe_test_stir","recipe_negroni","recipe_manhattan","recipe_manhattan","recipe_test_stir","recipe_negroni","recipe_gin_fizz","recipe_dry_martini","recipe_gin_fizz","recipe_dry_martini"};
            if(fixture=="quiet") plan[10]="recipe_dry_martini";
            if(expanded) {
                plan=plan.Concat(new[]{"recipe_manwol","recipe_test_stir","recipe_dry_martini","recipe_manhattan","recipe_hanky_panky","recipe_test_stir","recipe_gin_fizz","recipe_dry_martini"}).ToArray();
                if(act3) plan=plan.Concat(new[]{"recipe_manhattan","recipe_gin_fizz",fixture.Contains("quixote-mismatch") ? "recipe_dry_martini" : "recipe_hanky_panky","recipe_negroni"}).ToArray();
                if(act4) plan=plan.Concat(new[]{"recipe_french_75","recipe_dry_martini","recipe_dry_martini",fixture.Contains("hamlet-mismatch") ? "recipe_french_75" : "recipe_dry_martini"}).ToArray();
                var initial=owner.Session; startSlot=initial.State.SlotIndex; startPhase=initial.State.Phase.ToString(); startDepartures=lastDepartures=initial.State.CompletedOccurrences.Count;
                bool pendingAccepted=initial.State.CurrentService!=null && !initial.State.CompletedOccurrences.Contains(initial.State.OccurrenceId) &&
                    (initial.CurrentEncounter.RequiredDiscovery==null || initial.Knowledge.Project(initial.CurrentEncounter.RequiredDiscovery).Known);
                expectedServices=initial.State.Services.Count+totalSlots-startDepartures-(pendingAccepted ? 1 : 0);
                Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="Bar_Q1_Act"+expectedMemories,"ACT "+expectedMemories+" dedicated production scene");
                if(fixture.Contains("hanky-retry")) Check(startSlot<=21 && !initial.Knowledge.Project("recipe_hanky_panky").Known,"required Hanky failure begins before earned discovery");
                if(fixture.Contains("quixote-mismatch")) Check(act3 && (startSlot<27 || (startSlot==27 && initial.State.CurrentService==null)),"Quixote mismatch begins before the actual slot 27 service");
                if(fixture.Contains("french-retry")) Check(act4 && startSlot<=29 && !initial.Knowledge.Project("recipe_french_75").Known,"required French failure begins before earned discovery");
                if(fixture.Contains("hamlet-mismatch")) Check(act4 && (startSlot<32 || (startSlot==32 && initial.State.CurrentService==null)),"Hamlet mismatch begins before the actual slot 32 service");
                memoryThreeCaptured=initial.State.Memories.Any(x=>x.Id=="mem_03");
                ArchiveCheckpoint(proofPrefix+"-start"); yield return Capture(proofPrefix+"-start");
            }
            while(!failed && owner.Session?.State.Phase!=CampaignPhase.Complete)
            {
                yield return Idle(); var s=owner.Session; if(s==null) { Fail("Campaign not started"); yield break; }
                if(s.SaveError!=null) { Fail(s.SaveError); yield break; }
                if(expanded) ArchiveDepartures();
                if(act3 && !memoryThreeCaptured && s.State.Memories.Any(x=>x.Id=="mem_03")) {
                    memoryThreeCaptured=true;
                    Check(s.State.CompletedOccurrences.Count==28 && s.State.SlotIndex==28 && s.State.CurrentService==null,"MEM-03 earned after Quixote departure before final ordinary service");
                    ArchiveCheckpoint("act3-memory-after-27"); yield return Capture("act3-earned-memory-after-27");
                }
                var phase=s.State.Phase; if(phase==CampaignPhase.Complete) break;
                if(phase==CampaignPhase.Arriving || phase==CampaignPhase.Departing) { yield return null; continue; }
                if(phase==CampaignPhase.Brewing)
                {
                    bool firstAttempt=visited.Add(s.State.SlotIndex);
                    if(!firstAttempt && !(expanded && s.State.SlotIndex==21 && deliberateFailure && retryObserved) && !(act4 && s.State.SlotIndex==29 && frenchFailure && frenchRetryObserved)) { Fail("Unexpected retry"); yield break; }
                    if(expanded && s.State.SlotIndex>=17 && firstAttempt) ArchiveCheckpoint("before-service-"+s.State.SlotIndex);
                    if(s.State.SlotIndex==5 || s.State.SlotIndex==10 || s.State.SlotIndex==16) ArchiveCheckpoint("before-service-"+s.State.SlotIndex);
                    if(s.State.SlotIndex==3 || s.State.SlotIndex==5) { yield return Click("도감  Tab"); int page=Array.IndexOf(owner.Content.Catalog.Recipes.Keys.ToArray(),s.State.SlotIndex==3 ? "recipe_dry_martini" : "recipe_manwol"); yield return ClickButton(owner.Ui.GetComponentsInChildren<Button>().Single(x=>x.name=="Recipe page "+page),"partial recipe page "+page); yield return Capture("codex-clues-"+s.State.SlotIndex); yield return Click("돌아가기  Esc"); }
                    if(expanded && s.State.SlotIndex==21 && firstAttempt) {
                        yield return Click("도감  Tab"); int page=Array.IndexOf(owner.Content.Catalog.Recipes.Keys.ToArray(),"recipe_hanky_panky");
                        yield return ClickButton(owner.Ui.GetComponentsInChildren<Button>().Single(x=>x.name=="Recipe page "+page),"partial Hanky recipe page");
                        yield return Capture("codex-hanky-clues"); yield return Click("돌아가기  Esc");
                    }
                    if(act4 && s.State.SlotIndex==29 && firstAttempt) {
                        yield return InspectSupplyPages();
                        yield return Click("도감  Tab"); int page=Array.IndexOf(owner.Content.Catalog.Recipes.Keys.ToArray(),"recipe_french_75");
                        yield return ClickButton(owner.Ui.GetComponentsInChildren<Button>().Single(x=>x.name=="Recipe page "+page),"partial French 75 recipe page");
                        yield return Capture("codex-french-clues"); yield return Click("돌아가기  Esc");
                    }
                    bool omitZest=expanded && s.State.SlotIndex==21 && fixture.Contains("hanky-retry") && !deliberateFailure;
                    bool omitGentle=act4 && s.State.SlotIndex==29 && fixture.Contains("french-retry") && !frenchFailure;
                    if(omitZest) { deliberateFailure=true; expectedServices++; }
                    if(omitGentle) { frenchFailure=true; expectedServices++; }
                    yield return Prepare(plan[s.State.SlotIndex],omitZest,omitGentle);
                    if(expanded && s.State.SlotIndex>=17) ArchiveCheckpoint((omitZest || omitGentle ? "failed-service-" : "after-service-")+s.State.SlotIndex);
                    continue;
                }
                if(expanded && phase==CampaignPhase.Retry) {
                    if(act4 && s.State.SlotIndex==29) {
                        Check(!s.Knowledge.Project("recipe_french_75").Known && !s.State.CompletedOccurrences.Contains(s.State.OccurrenceId) && s.CurrentText?.Id=="text_rc1_0039","French retry retains discovery and authored feedback");
                        frenchRetryObserved=true; ArchiveCheckpoint("french-retry"); yield return Capture("french-required-retry");
                        if(media) yield return new WaitForSecondsRealtime(1.5f);
                        yield return Click("계속  Enter"); yield return Idle();
                        Check(s.State.Phase==CampaignPhase.Brewing && s.State.ConsumedLines.Any(x=>x.EndsWith(":29:Retry:0:text_rc1_0039",StringComparison.Ordinal)),"French retry reading consumed before fresh attempt");
                        ArchiveCheckpoint("french-retry-read"); continue;
                    }
                    Check(s.State.SlotIndex==21 && !s.Knowledge.Project("recipe_hanky_panky").Known && !s.State.CompletedOccurrences.Contains(s.State.OccurrenceId),"required Hanky failure retains encounter and unknown recipe");
                    retryObserved=true; ArchiveCheckpoint("hanky-retry"); yield return Capture("hanky-required-retry");
                }
                if(expanded && s.State.SlotIndex>=17 && (phase==CampaignPhase.Intro || phase==CampaignPhase.Farewell)) {
                    string label=proofPrefix+"-"+phase.ToString().ToLowerInvariant()+"-"+s.State.SlotIndex+"-"+s.State.LineIndex;
                    ArchiveCheckpoint(label); yield return Capture(label);
                }
                if(phase==CampaignPhase.Choice) { yield return Capture("prince-choice"); yield return Press(Key.Enter); Check(s.State.Phase==CampaignPhase.ChoiceResponse && s.State.PrinceChoice=="keep","keyboard choice exactly once through production focus"); continue; }
                if(phase==CampaignPhase.Intro && s.CurrentEncounter.PrinceVisit.HasValue && s.State.LineIndex==0) {
                    ArchiveCheckpoint("prince-intro-"+s.CurrentEncounter.PrinceVisit);
                    yield return Capture("prince-intro-"+s.CurrentEncounter.PrinceVisit);
                }
                if(phase==CampaignPhase.Farewell && s.CurrentEncounter.PrinceVisit==PrinceVisit.Clarity) yield return Capture("prince-farewell-"+s.State.LineIndex);
                if(media && s.CurrentText!=null) yield return new WaitForSecondsRealtime(1.5f);
                yield return Click(phase==CampaignPhase.Request ? "한 잔 준비하기" : "계속  Enter");
            }
            if(failed) yield break;
            if(expanded) { ArchiveDepartures(); ArchiveCheckpoint(proofPrefix+"-complete"); }
            yield return new WaitForSecondsRealtime(.4f); yield return Capture("earned-memory");
            if(media) yield return new WaitForSecondsRealtime(23f);
            if(expanded) {
                Check(owner.Session.State.Services.Count==expectedServices,expectedServices+" actual services");
                Check(owner.Session.State.CompletedOccurrences.Count==totalSlots,totalSlots+" natural departures");
                Check(owner.Session.State.Services.Select(x=>x.OccurrenceId).Distinct().Count()==totalSlots,"one service occurrence per scheduled encounter");
                Check(owner.Session.Knowledge.KnownReceipts.Count==(act4 ? 8 : 7),(act4 ? "eight" : "seven")+" recipes learned by real services");
                Check(owner.Session.State.ActComplete && owner.Session.State.Memories.Count==expectedMemories && owner.Memory.Visible && owner.Memory.LoversVisible,
                    expectedMemories+" earned memories after natural departure");
                Check(owner.Memory.Glowing==(owner.Session.State.Memories.Single(x=>x.Id=="mem_01").Variant=="star_glowing"),"preserved ACT 1 memory variant");
                Check(owner.Memory.LoversSwing!=null && owner.Memory.LoversEmptyPlace!=null,"MEM-02 swing and adjacent empty place resolve");
                if(fixture.Contains("hanky-retry")) Check(deliberateFailure && retryObserved,"required Hanky failure and successful retry observed");
                if(act3) {
                    Check(owner.Memory.WindmillVisible && owner.Memory.WindmillFragment!=null && owner.Memory.WindmillWing!=null,"MEM-03 windmill fragment and attached wing resolve");
                    Check(owner.Session.State.Memories.Count(x=>x.Id=="mem_03" && x.Variant=="windmill_fragment")==1,"one canonical MEM-03 receipt");
                    if(fixture.Contains("quixote-mismatch")) Check(quixoteMismatchObserved,"mismatching Quixote drink still reaches natural memory departure");
                }
                if(act4) {
                    Check(owner.Memory.HamletVisible && owner.Memory.HamletStage!=null && owner.Memory.HamletSkull!=null && owner.Memory.HamletShadow!=null,"MEM-04 stage skull and shadow resolve");
                    Check(owner.Session.State.Memories.Count(x=>x.Id=="mem_04" && x.Variant=="stage_skull_shadow")==1,"one canonical MEM-04 receipt");
                    if(fixture.Contains("french-retry")) Check(frenchFailure && frenchRetryObserved,"required French failure and successful retry observed");
                    if(fixture.Contains("hamlet-mismatch")) Check(hamletMismatchObserved,"mismatching Hamlet drink still reaches natural memory departure");
                }
            } else {
                Check(owner.Session.State.Services.Count==17,"17 actual services"); Check(owner.Session.State.CompletedOccurrences.Count==17,"17 natural departures");
                Check(owner.Session.State.ActComplete && owner.Memory.Visible && owner.Memory.Glowing==(fixture!="quiet"),"earned memory variant after return-visit understanding");
                Check(owner.Session.Knowledge.KnownReceipts.Count==6,"six recipes learned by real services");
            }
            var q1=FindFirstObjectByType<Q1ProductionView>();
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="Bar_Q1")
                Check(q1!=null,"Q1 Production presentation root resolves separately from controller");
            if(q1!=null) {
                Check(!q1.Focused && Quaternion.Angle(owner.Camera.transform.rotation,Q1ProductionView.WorldAngle)<.001f,"Q1 restored after final service");
                var star=owner.Memory.transform.Find("MEM-01 small star");
                Check(star!=null && Quaternion.Angle(star.rotation,Q1ProductionView.WorldAngle)<.001f,"earned star follows Q1 facing");
                int revealCount=q1.GetComponentsInChildren<Canvas>().Count(c=>c.name=="Q1 focus reveal");
                Check(revealCount==(visited.Count>0 ? 1 : 0),"focus reveal is created once for services, absent on completed Continue");
                Check(!q1.GetComponentsInChildren<Image>().Any(i=>i.name=="Warm charcoal" && i.enabled),"focus reveal cleaned up after service");
                Check(FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled).All(r=>r.sharedMaterials.All(m=>m!=null && m.shader!=null && m.shader.name!="Hidden/InternalErrorShader")),"active renderers have materials and shaders");
            }
            yield return Click("도감  Tab");
            foreach(var r in owner.Content.Catalog.Recipes.Values) { yield return Click(owner.Content.Catalog.Cocktails[r.CocktailId].Name); yield return Capture("codex-known-"+r.Method+"-"+r.Id); }
            yield return Click("돌아가기  Esc"); yield return Click("설정  Esc"); yield return Click("글자 크기 바꾸기"); yield return Capture("settings-large"); yield return Click("돌아가기  Esc");
            Check(FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length==1,"one EventSystem");
            Finish();
        }
        private void Log(string text,string stack,LogType type) { if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) { errors.Add(text); failed=true; Finish(); } }
        private void ArchiveCheckpoint(string name)
        {
            if(expanded && !archived.Add(name)) return;
            string destination=Path.Combine(output,"checkpoints",name,"slot1"); Directory.CreateDirectory(destination);
            // Copy actual published generations for later Continue coverage; never construct or alter a save.
            foreach(string file in new[]{"current.json","previous.json"}) { string source=Path.Combine(owner.StorageRoot,"slot1",file); if(File.Exists(source)) File.Copy(source,Path.Combine(destination,file),true); }
            if(expanded) {
                string current=Path.Combine(destination,"current.json"),previous=Path.Combine(destination,"previous.json");
                Check(File.Exists(current),"actual published checkpoint "+name);
                File.AppendAllText(Path.Combine(output,"checkpoints.jsonl"),JsonUtility.ToJson(new CheckpointEvidence{name=name,slot=owner.Session.State.SlotIndex,
                    phase=owner.Session.State.Phase.ToString(),services=owner.Session.State.Services.Count,departures=owner.Session.State.CompletedOccurrences.Count,
                    sequence=owner.Session.State.Sequence,currentHash=Hash(current),previousHash=File.Exists(previous) ? Hash(previous) : null})+"\n");
            }
        }
        private void ArchiveDepartures()
        {
            int count=owner.Session.State.CompletedOccurrences.Count;
            for(int slot=Math.Max(17,lastDepartures);slot<count;slot++) ArchiveCheckpoint("after-departure-"+slot);
            lastDepartures=count;
        }
        private static string Hash(string path)
        { using(var sha=System.Security.Cryptography.SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant(); }
        [Serializable] private sealed class CheckpointEvidence { public string name,phase,currentHash,previousHash; public int slot,services,departures; public long sequence; }
        private void LateUpdate()
        {
            if(render==null || owner.Ui==null) return; Canvas.ForceUpdateCanvases(); RenderPipeline.SubmitRenderRequest(owner.Camera,new UniversalRenderPipeline.SingleCameraRequest{destination=render}); frames.Add(Time.unscaledDeltaTime*1000);
            if(audioRecording) {
                int count=AudioRenderer.GetSampleCountForCaptureFrame()*audioChannels;
                using(var buffer=new NativeArray<float>(count,Allocator.Temp)) {
                    if(!AudioRenderer.Render(buffer)) { Fail("Audio capture rejected"); return; }
                    var pcm=new short[count]; for(int i=0;i<count;i++) pcm[i]=(short)Mathf.RoundToInt(Mathf.Clamp(buffer[i],-1,1)*32767);
                    var bytes=new byte[count*2]; Buffer.BlockCopy(pcm,0,bytes,0,bytes.Length); audio.Write(bytes); audioBytes+=bytes.Length;
                }
            }
            if(media && Time.realtimeSinceStartup>=nextMedia) {
                nextMedia=Time.realtimeSinceStartup+.2f; SavePixels(Path.Combine(output,"frames",(++mediaFrame).ToString("D5")+".png"));
                File.AppendAllText(Path.Combine(output,"frame-times.csv"),mediaFrame+","+(Time.realtimeSinceStartup-mediaStart).ToString(System.Globalization.CultureInfo.InvariantCulture)+"\n");
            }
        }
        private void Queue()
        { InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys)); var state=new MouseState{position=pointer}.WithButton(MouseButton.Left,down); InputSystem.QueueStateEvent(mouse,state); }
        private void QueueStirForCurrentFrame()
        {
            if(InputState.currentUpdateType!=InputUpdateType.Dynamic) return;
            if(finePourInput) {
                // Anticipate the real adapter step before submitting simulated keys. A PNG
                // capture stall must not make this fixture overshoot; no liquid/time is injected.
                decimal measured=owner.Session.Brew.Container(fineRole).TotalMl-fineStart;
                decimal next=owner.Content.Tuning.FinePourRate*(decimal)Math.Min(Time.unscaledDeltaTime,.25f);
                if(FinePourComplete(measured,fineTarget,next)) finePourInput=false;
                keys=finePourInput && measured+next<=fineTarget+.12m ? new[]{fineKey,Key.LeftShift} : Array.Empty<Key>();
                Queue();
            }
            if(!stirInput) return;
            // Capture can stall a frame. Deliver this frame's simulated motion before
            // input processing, using the same elapsed time as the production adapter.
            // No recipe, technique or gameplay time is injected or changed.
            stirAngle+=Mathf.Min(Time.unscaledDeltaTime,.25f)*150*Mathf.Deg2Rad;
            pointer=stirCenter+new Vector2(Mathf.Cos(stirAngle),Mathf.Sin(stirAngle))*stirRadius;
            Queue();
        }
        private static bool FinePourComplete(decimal measured,decimal target,decimal next)
        {
            // Do not wait forever between frame-sized increments once the unchanged
            // aim is met and the next step would overshoot. Stay inside the existing
            // .25 ml preparation tolerance (not merely the older .5 ml fixture check).
            return measured>=target-.12m || (Math.Abs(measured-target)<.2m && measured+next>target+.12m);
        }
        private IEnumerator Idle()
        { float until=Time.realtimeSinceStartup+10; while(owner.Busy || owner.Session?.IsSaving==true) { if(Time.realtimeSinceStartup>until) { Fail("Save timeout"); yield break; } yield return null; } }
        private Button Find(string label)
        {
            var items=owner.Ui.GetComponentsInChildren<Button>().Where(x=>x.isActiveAndEnabled && x.IsInteractable() && x.GetComponentInChildren<TextMeshProUGUI>()?.text==label).ToArray();
            if(items.Length!=1) throw new InvalidOperationException("Expected one control: "+label+" count="+items.Length); return items[0];
        }
        private IEnumerator Click(string label)
        {
            yield return Idle(); yield return ClickButton(Find(label),label);
        }
        private IEnumerator ClickButton(Button b,string label)
        {
            Canvas.ForceUpdateCanvases(); var corners=new Vector3[4]; ((RectTransform)b.transform).GetWorldCorners(corners);
            pointer=RectTransformUtility.WorldToScreenPoint(owner.Camera,(corners[0]+corners[2])*.5f); down=false; Queue(); yield return null;
            down=true; Queue(); yield return new WaitForSecondsRealtime(.06f); down=false; Queue(); yield return new WaitForSecondsRealtime(.08f); yield return Idle();
            File.AppendAllText(Path.Combine(output,"simulated-input.jsonl"),"{\"control\":\""+label.Replace("\n"," ").Replace("\"","'")+"\",\"time\":"+Time.realtimeSinceStartup.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}\n");
        }
        private IEnumerator Press(Key key,float seconds=.08f)
        { keys=new[]{key}; Queue(); yield return new WaitForSecondsRealtime(seconds); keys=Array.Empty<Key>(); Queue(); yield return new WaitForSecondsRealtime(.08f); yield return Idle(); }
        private IEnumerator Target(ContainerRole role)
        { for(int n=0;n<4 && owner.Input.Target!=role;n++) yield return Press(Key.J); Check(owner.Input.Target==role,"target "+role); }
        private Button IngredientButton(string id)
        {
            string name=owner.Content.IngredientNames[id];
            return owner.Ui.GetComponentsInChildren<Button>().SingleOrDefault(x=>x.isActiveAndEnabled && x.IsInteractable() &&
                (x.GetComponentInChildren<TextMeshProUGUI>()?.text==name || (x.GetComponentInChildren<TextMeshProUGUI>()?.text.EndsWith("  "+name,StringComparison.Ordinal) ?? false)));
        }
        private Button PageButton(string label) => owner.Ui.GetComponentsInChildren<Button>().SingleOrDefault(x=>x.isActiveAndEnabled && x.IsInteractable() && x.GetComponentInChildren<TextMeshProUGUI>()?.text==label);
        private string[] VisibleIngredients() => owner.Session?.Supply.Where(id=>IngredientButton(id)!=null).ToArray() ?? Array.Empty<string>();
        private IEnumerator InspectSupplyPages()
        {
            suppliedIngredients=owner.Session.Supply.OrderBy(x=>x,StringComparer.Ordinal).ToArray();
            for(int n=0;n<5 && PageButton("이전 재료")!=null;n++) yield return Click("이전 재료");
            Check(PageButton("이전 재료")==null,"physical shelf begins at first page");
            for(int page=1;page<=5;page++) {
                yield return Capture("act4-shelf-page-"+page);
                if(PageButton("다음 재료")==null) break;
                yield return Click("다음 재료");
            }
            foreach(string id in suppliedIngredients) { yield return SelectIngredient(id); shelfSelections.Add(id); }
            Check(shelfSelections.SetEquals(suppliedIngredients),"every supplied ingredient selected through visible shelf controls");
        }
        private IEnumerator SelectIngredient(string id)
        {
            Button button=IngredientButton(id);
            for(int n=0;button==null && n<5;n++) {
                string page=PageButton("다음 재료")!=null ? "다음 재료" : PageButton("이전 재료")!=null ? "이전 재료" : null;
                if(page==null) break;
                yield return Click(page); button=IngredientButton(id);
            }
            if(button==null) { Fail("No visible supplied ingredient control: "+id); yield break; }
            yield return ClickButton(button,button.GetComponentInChildren<TextMeshProUGUI>().text); Check(owner.Input.SelectedIngredient==id,"ingredient "+id);
        }
        private IEnumerator Pour(string id,decimal ml,ContainerRole role,Key key=Key.Q)
        {
            yield return SelectIngredient(id); decimal start=owner.Session.Brew.Container(role).TotalMl; float until=Time.realtimeSinceStartup+20;
            keys=new[]{key}; Queue();
            // Allow the production adapter's maximum 0.25 s step at 30 ml/s before switching to fine flow.
            while(owner.Session.Brew.Container(role).TotalMl-start<ml-8m) { if(Time.realtimeSinceStartup>until) { Fail("Pour stalled"); yield break; } yield return null; }
            keys=Array.Empty<Key>(); Queue(); yield return null;
            fineRole=role; fineStart=start; fineTarget=ml; fineKey=key; finePourInput=true;
            while(finePourInput && owner.Session.Brew.Container(role).TotalMl-start<ml-.12m) { if(Time.realtimeSinceStartup>until) { Fail("Fine pour stalled"); yield break; } yield return null; }
            finePourInput=false; keys=Array.Empty<Key>(); Queue(); yield return new WaitForSecondsRealtime(.08f);
            decimal actual=owner.Session.Brew.Container(role).TotalMl-start;
            Check(Math.Abs(actual-ml)<.25m,"measured "+id+" "+actual+" / "+ml+" ml");
        }
        private IEnumerator Prepare(string id,bool omitZest=false,bool omitGentle=false)
        {
            var recipe=owner.Content.Catalog.Recipes[id]; yield return Click(char.ToUpperInvariant(recipe.Method[0])+recipe.Method.Substring(1));
            if(recipe.Method=="stir")
            {
                foreach(var a in recipe.Amounts) { if(a.Key==recipe.DashIngredient) continue; yield return Target(ContainerRole.Jigger); yield return Pour(a.Key,a.Value,ContainerRole.Jigger); yield return Press(Key.E,2); }
                yield return Target(ContainerRole.Mixing); if(recipe.DashIngredient!=null) { yield return SelectIngredient(recipe.DashIngredient); yield return Press(Key.D); } yield return Press(Key.I);
                Vector2 center=owner.Camera.WorldToScreenPoint(owner.Workbench.MixingCenter); float radius=Screen.height/(2*owner.Camera.orthographicSize)*.25f;
                pointer=center+Vector2.right*radius; Queue(); yield return null; keys=new[]{Key.S}; Queue();
                stirCenter=center; stirRadius=radius; stirAngle=0; stirInput=true;
                float until=Time.realtimeSinceStartup+20;
                while(owner.Session.Brew.StirAcceptedDegrees<1080) { if(Time.realtimeSinceStartup>until) { stirInput=false; Fail("Stir stalled"); yield break; } yield return null; }
                stirInput=false;
                keys=Array.Empty<Key>(); Queue(); yield return null; yield return Press(Key.R,4); if(recipe.RequiredGarnish=="cherry") yield return Press(Key.C);
                if(recipe.RequiredGarnish=="orange_zest" && !omitZest) yield return Click("제스트 Z");
            }
            else if(recipe.Method=="build")
            { yield return Target(ContainerRole.Serving); yield return Press(Key.I); foreach(var a in recipe.Amounts) yield return Pour(a.Key,a.Value,ContainerRole.Serving); yield return Press(Key.G); yield return Press(Key.O); }
            else if(recipe.Method=="shake")
            {
                yield return Target(ContainerRole.Shaker); foreach(var a in recipe.Amounts) if(a.Key!=recipe.LateIngredient) yield return Pour(a.Key,a.Value,ContainerRole.Shaker);
                yield return Press(Key.I); yield return Press(Key.L); pointer=new Vector2(Screen.width*.4f,Screen.height*.5f); Queue(); yield return null;
                keys=new[]{Key.K}; Queue(); float start=Time.realtimeSinceStartup;
                while(Time.realtimeSinceStartup-start<7) { float t=Time.realtimeSinceStartup-start; pointer=new Vector2(Screen.width*(.5f-.1f*Mathf.Cos(2*Mathf.PI*t)),Screen.height*.5f); Queue(); yield return null; }
                keys=Array.Empty<Key>(); Queue(); yield return null; yield return Press(Key.L);
                if(recipe.ShakeFinish==ShakeFinishPolicy.StrainThenPourAndGentleStir) {
                    Check(owner.Input.ShakerStrainAvailable,"production Shaker Strain is available for ACT 4");
                    yield return Press(Key.U,4); yield return Target(ContainerRole.Serving);
                    yield return Pour(recipe.LateIngredient,recipe.Amounts[recipe.LateIngredient],ContainerRole.Serving);
                    if(!omitGentle) yield return Press(Key.G);
                } else {
                    yield return Press(Key.T,4); yield return Target(ContainerRole.Serving); yield return Pour(recipe.LateIngredient,recipe.Amounts[recipe.LateIngredient],ContainerRole.Serving,Key.X);
                }
            }
            else
            {
                foreach(string ingredient in recipe.LayerOrder)
                {
                    yield return Target(ContainerRole.Jigger); yield return Pour(ingredient,recipe.Amounts[ingredient],ContainerRole.Jigger);
                    pointer=new Vector2(Screen.width*.45f,Screen.height*.38f); Queue(); yield return null; keys=new[]{Key.F}; Queue(); float until=Time.realtimeSinceStartup+15;
                    while(owner.Session.Brew.Container(ContainerRole.Jigger).TotalMl>0) { if(Time.realtimeSinceStartup>until) { Fail("Float stalled"); yield break; } yield return null; }
                    keys=Array.Empty<Key>(); Queue(); yield return null;
                }
            }
            string failureSuffix=omitZest ? "-missing-zest" : omitGentle ? "-missing-gentle" : "";
            yield return Capture("prepared-"+owner.Session.State.SlotIndex+"-"+recipe.Method+failureSuffix); yield return Click("손님께 내기"); yield return Idle();
            var result=owner.Session.State.CurrentService?.Outcome;
            if(omitZest) Check(owner.Session.State.Phase==CampaignPhase.Reaction && result?.RecipeId==id && result.Preparation!=PreparationResult.Good && !owner.Session.Knowledge.Project(id).Known,"required Hanky missing-zest preparation fails without discovery");
            else if(omitGentle) Check(owner.Session.State.Phase==CampaignPhase.Reaction && result?.RecipeId==id && result.Preparation!=PreparationResult.Good && !owner.Session.Knowledge.Project(id).Known,"required French missing-GentleStir preparation fails without discovery");
            else Check(owner.Session.State.Phase==CampaignPhase.Reaction && result?.RecipeId==id && result.Preparation==PreparationResult.Good && result.QualityGood,"service "+owner.Session.State.SlotIndex+" "+id);
            if(expanded && owner.Session.State.SlotIndex>=17 && !omitZest && !omitGentle) {
                bool quixoteMismatch=act3 && owner.Session.State.SlotIndex==27 && fixture.Contains("quixote-mismatch");
                bool hamletMismatch=act4 && owner.Session.State.SlotIndex==32 && fixture.Contains("hamlet-mismatch");
                bool mismatch=quixoteMismatch || hamletMismatch;
                Check(result?.Fits==!mismatch,"actual ACT "+owner.Content.ActAt(owner.Session.State.SlotIndex)+" request "+(mismatch ? "mismatch " : "fit ")+owner.Session.State.SlotIndex);
                if(quixoteMismatch) quixoteMismatchObserved=result?.Fits==false;
                if(hamletMismatch) hamletMismatchObserved=result?.Fits==false;
            }
            yield return Capture("reaction-"+owner.Session.State.SlotIndex+failureSuffix);
        }
        private IEnumerator Capture(string label)
        {
            yield return null; Canvas.ForceUpdateCanvases(); var texts=owner.Ui.GetComponentsInChildren<TextMeshProUGUI>().Where(x=>x.isActiveAndEnabled && !string.IsNullOrWhiteSpace(x.text)).ToArray();
            var overflow=texts.Where(x=>x.isTextOverflowing).Select(x=>x.text).ToArray(); if(overflow.Length>0) errors.Add("Text overflow "+label+": "+string.Join(" | ",overflow));
            string image=(++captures).ToString("D3")+"-"+label+".png"; SavePixels(Path.Combine(output,image));
            if(expanded) RecordCapture(label,image,overflow);
            File.AppendAllText(Path.Combine(output,"capture-times.csv"),captures+","+label+","+(Time.realtimeSinceStartup-mediaStart).ToString(System.Globalization.CultureInfo.InvariantCulture)+"\n");
        }
        private void RecordCapture(string label,string image,string[] overflow)
        {
            var s=owner.Session; var state=s?.State; var text=s?.CurrentText;
            var actors=CampaignEvidenceProbe.VisibleActors();
            var invalidRenderers=CampaignEvidenceProbe.InvalidVisibleAssets();
            Check(invalidRenderers.Length==0,"visible assets and materials "+label);
            var serving=owner.Workbench.GetComponentsInChildren<Transform>(true).Where(x=>x.name=="Serving glass 180 ml").ToArray();
            if(state!=null && state.SlotIndex>=17 && (state.Phase==CampaignPhase.Intro || state.Phase==CampaignPhase.Farewell)) {
                Check(s.CurrentEncounter.GuestIds.All(id=>actors.Any(a=>CampaignEvidenceProbe.PresentedGuestId(owner,a)==id)),"visible authored actors "+label);
                Check(actors.Where(a=>s.CurrentEncounter.GuestIds.Contains(CampaignEvidenceProbe.PresentedGuestId(owner,a))).All(a=>Mathf.Abs(a.transform.position.y)<.001f),"grounded authored actors "+label);
                if(s.CurrentEncounter.GuestIds.Count==2) {
                    Check(owner.Chunhyang!=owner.Mongryong && owner.Chunhyang.SeatAnchor!=owner.Mongryong.SeatAnchor && Vector3.Distance(owner.Chunhyang.transform.position,owner.Mongryong.transform.position)>.25f,"separate paired seats "+label);
                    Check(owner.Cast!=null && owner.Cast.Paired && serving.Length==1,"one shared serving vessel "+label);
                    if(state.Phase==CampaignPhase.Farewell) Check(Vector3.Distance(serving[0].position,owner.Cast.SharedCupAnchor.position)<.001f,"served cup at shared anchor "+label);
                }
                if(text?.Role=="narration") Check(string.IsNullOrEmpty(owner.Ui.DisplayedSpeaker),"narration retains silence "+label);
                if(!state.Memories.Any(x=>x.Id=="mem_02")) Check(!owner.Memory.LoversVisible,"MEM-02 absent before earned departure "+label);
                if(act3 && !state.Memories.Any(x=>x.Id=="mem_03")) Check(!owner.Memory.WindmillVisible,"MEM-03 absent before earned departure "+label);
                if(act4 && !state.Memories.Any(x=>x.Id=="mem_04")) Check(!owner.Memory.HamletVisible,"MEM-04 absent before earned departure "+label);
            }
            File.AppendAllText(Path.Combine(output,"captures.jsonl"),JsonUtility.ToJson(new CaptureEvidence{label=label,image=image,
                slot=state?.SlotIndex ?? -1,line=state?.LineIndex ?? -1,phase=state?.Phase.ToString() ?? "menu",textId=text?.Id,textRole=text?.Role,
                displayedSpeaker=owner.Ui.DisplayedSpeaker,guestIds=s?.CurrentEncounter.GuestIds.ToArray() ?? Array.Empty<string>(),
                visibleActors=actors.Select(a=>CampaignEvidenceProbe.PresentedGuestId(owner,a)).ToArray(),actorBindingIds=actors.Select(a=>a.GuestId).ToArray(),
                visibleText=owner.Ui.GetComponentsInChildren<TextMeshProUGUI>().Where(t=>t.isActiveAndEnabled).Select(t=>t.text).ToArray(),
                overflow=overflow,invalidRenderers=invalidRenderers,services=state?.Services.Count ?? 0,departures=state?.CompletedOccurrences.Count ?? 0,
                known=s?.Knowledge.KnownReceipts.Count ?? 0,memories=state?.Memories.Count ?? 0,loversVisible=owner.Memory.LoversVisible,windmillVisible=owner.Memory.WindmillVisible,hamletVisible=owner.Memory.HamletVisible,
                visibleIngredients=act4 ? VisibleIngredients() : Array.Empty<string>(),
                act=state==null ? 0 : owner.Content.ActAt(state.SlotIndex),servingVessels=serving.Length})+"\n");
        }
        [Serializable] private sealed class CaptureEvidence
        {
            public string label,image,phase,textId,textRole,displayedSpeaker; public string[] guestIds,visibleActors,actorBindingIds,visibleText,overflow,invalidRenderers,visibleIngredients;
            public int slot,line,services,departures,known,memories,servingVessels,act; public bool loversVisible,windmillVisible,hamletVisible;
        }
        private void SavePixels(string path)
        { var prior=RenderTexture.active; RenderTexture.active=render; var texture=new Texture2D(render.width,render.height,TextureFormat.RGB24,false); texture.ReadPixels(new Rect(0,0,render.width,render.height),0,0); texture.Apply(); RenderTexture.active=prior; File.WriteAllBytes(path,texture.EncodeToPNG()); Destroy(texture); }
        private void Check(bool good,string label) { if(good) checks.Add(label); else Fail(label); }
        private void Fail(string reason) { failed=true; errors.Add(reason); Finish(); }
        private void Finish()
        {
            if(audioRecording) { AudioRenderer.Stop(); audioRecording=false; audio.BaseStream.Position=4; audio.Write(36+audioBytes); audio.BaseStream.Position=40; audio.Write(audioBytes); audio.Dispose(); audio=null; } media=false;
            File.WriteAllText(Path.Combine(output,"player-result.json"),JsonUtility.ToJson(new Result{status=errors.Count==0 ? "PASS" : "FAIL",tier="Simulated production InputSystem in built Player; NOT native OS input",checks=checks.ToArray(),errors=errors.ToArray(),frames=frames.ToArray(),captures=captures,
                scope=owner?.Content?.Scope,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,fixture=fixture,services=owner.Session?.State.Services.Count ?? 0,
                departures=owner.Session?.State.CompletedOccurrences.Count ?? 0,known=owner.Session?.Knowledge.KnownReceipts.Count ?? 0,memories=owner.Session?.State.Memories.Count ?? 0,
                expectedServices=expectedServices,startSlot=startSlot,startDepartures=startDepartures,startPhase=startPhase,visitedSlots=visited.OrderBy(x=>x).ToArray(),deliberateFailure=deliberateFailure,retryObserved=retryObserved,quixoteMismatchObserved=quixoteMismatchObserved,
                frenchFailure=frenchFailure,frenchRetryObserved=frenchRetryObserved,hamletMismatchObserved=hamletMismatchObserved,suppliedIngredients=suppliedIngredients,shelfSelections=shelfSelections.OrderBy(x=>x,StringComparer.Ordinal).ToArray()},true));
            UnityEngine.Application.Quit(errors.Count==0 ? 0 : 2);
        }
        [Serializable] private sealed class Result { public string status,tier,scope,scene,fixture,startPhase; public string[] checks,errors,suppliedIngredients,shelfSelections; public float[] frames; public int captures,services,departures,known,memories,expectedServices,startSlot,startDepartures; public int[] visitedSlots; public bool deliberateFailure,retryObserved,quixoteMismatchObserved,frenchFailure,frenchRetryObserved,hamletMismatchObserved; }
        private void OnDestroy() { UnityEngine.Application.logMessageReceived-=Log; InputSystem.onBeforeUpdate-=QueueStirForCurrentFrame; if(keyboard!=null) InputSystem.RemoveDevice(keyboard); if(mouse!=null) InputSystem.RemoveDevice(mouse); if(render!=null) { render.Release(); Destroy(render); } }
    }
}
