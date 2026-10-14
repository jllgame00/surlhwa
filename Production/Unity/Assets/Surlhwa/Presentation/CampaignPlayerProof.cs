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
    public sealed class CampaignPlayerProof : MonoBehaviour
    {
        private CampaignController owner; private string output,fixture; private Keyboard keyboard; private Mouse mouse;
        private Vector2 pointer; private Key[] keys=Array.Empty<Key>(); private bool down,failed; private RenderTexture render;
        private readonly List<string> checks=new List<string>(),errors=new List<string>(); private readonly List<float> frames=new List<float>(); private int captures;
        private bool media,audioRecording; private BinaryWriter audio; private int audioChannels,audioBytes,mediaFrame; private float mediaStart,nextMedia;
        private string Arg(string key) { var args=Environment.GetCommandLineArgs(); int i=Array.IndexOf(args,key); return i<0 || i+1>=args.Length ? null : args[i+1]; }
        private IEnumerator Start()
        {
            fixture=Arg("-campaignProof"); if(fixture==null) { enabled=false; yield break; }
            output=Path.GetFullPath(Arg("-campaignEvidenceRoot")); Directory.CreateDirectory(output); owner=GetComponent<CampaignController>();
            UnityEngine.Application.logMessageReceived+=Log; float until=Time.realtimeSinceStartup+40;
            while(owner.Ui==null || owner.Busy) { if(Time.realtimeSinceStartup>until) { Fail("Initialization timeout"); yield break; } yield return null; }
            keyboard=InputSystem.AddDevice<Keyboard>(); mouse=InputSystem.AddDevice<Mouse>();
            render=new RenderTexture(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB); render.Create();
            media=Arg("-campaignMedia")=="1";
            if(media) {
                Directory.CreateDirectory(Path.Combine(output,"frames")); audioChannels=AudioSettings.speakerMode==AudioSpeakerMode.Mono ? 1 : 2; audioRecording=AudioRenderer.Start();
                if(!audioRecording) { Fail("AudioRenderer unavailable"); yield break; }
                audio=new BinaryWriter(File.Create(Path.Combine(output,"unity-mix.wav"))); int rate=AudioSettings.outputSampleRate;
                audio.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); audio.Write(0); audio.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); audio.Write(16); audio.Write((short)1); audio.Write((short)audioChannels); audio.Write(rate); audio.Write(rate*audioChannels*2); audio.Write((short)(audioChannels*2)); audio.Write((short)16); audio.Write(System.Text.Encoding.ASCII.GetBytes("data")); audio.Write(0); mediaStart=Time.realtimeSinceStartup;
            }
            yield return Capture("menu"); yield return Click("도감  Tab"); yield return Capture("codex-initial"); yield return Click("돌아가기  Esc");
            yield return Click(fixture=="continue" || fixture=="quiet" ? "이어 하기" : "새로 시작"); yield return Idle();
            string[] plan={"recipe_test_stir","recipe_test_stir","recipe_test_stir","recipe_dry_martini","recipe_dry_martini","recipe_manwol","recipe_negroni","recipe_test_stir","recipe_negroni","recipe_manhattan","recipe_manhattan","recipe_test_stir","recipe_negroni","recipe_gin_fizz","recipe_dry_martini","recipe_gin_fizz","recipe_dry_martini"};
            if(fixture=="quiet") plan[10]="recipe_dry_martini";
            var visited=new HashSet<int>();
            while(!failed && owner.Session?.State.Phase!=CampaignPhase.Complete)
            {
                yield return Idle(); var s=owner.Session; if(s==null) { Fail("Campaign not started"); yield break; }
                if(s.SaveError!=null) { Fail(s.SaveError); yield break; }
                var phase=s.State.Phase; if(phase==CampaignPhase.Complete) break;
                if(phase==CampaignPhase.Arriving || phase==CampaignPhase.Departing) { yield return null; continue; }
                if(phase==CampaignPhase.Brewing)
                {
                    if(!visited.Add(s.State.SlotIndex)) { Fail("Unexpected retry"); yield break; }
                    if(s.State.SlotIndex==10 || s.State.SlotIndex==16) ArchiveCheckpoint("before-service-"+s.State.SlotIndex);
                    if(s.State.SlotIndex==3 || s.State.SlotIndex==5) { yield return Click("도감  Tab"); int page=Array.IndexOf(owner.Content.Catalog.Recipes.Keys.ToArray(),s.State.SlotIndex==3 ? "recipe_dry_martini" : "recipe_manwol"); yield return ClickButton(owner.Ui.GetComponentsInChildren<Button>().Single(x=>x.name=="Recipe page "+page),"partial recipe page "+page); yield return Capture("codex-clues-"+s.State.SlotIndex); yield return Click("돌아가기  Esc"); }
                    yield return Prepare(plan[s.State.SlotIndex]); continue;
                }
                if(phase==CampaignPhase.Choice) { yield return Capture("prince-choice"); yield return Click(owner.Content.Texts[owner.Content.PrinceChoices["keep"]].Value); continue; }
                if(phase==CampaignPhase.Intro && s.CurrentEncounter.PrinceVisit.HasValue && s.State.LineIndex==0) yield return Capture("prince-intro-"+s.CurrentEncounter.PrinceVisit);
                if(phase==CampaignPhase.Farewell && s.CurrentEncounter.PrinceVisit==PrinceVisit.Clarity) yield return Capture("prince-farewell-"+s.State.LineIndex);
                if(media && s.CurrentText!=null) yield return new WaitForSecondsRealtime(1.5f);
                yield return Click(phase==CampaignPhase.Request ? "한 잔 준비하기" : "계속  Enter");
            }
            if(failed) yield break;
            yield return new WaitForSecondsRealtime(.4f); yield return Capture("earned-memory");
            if(media) yield return new WaitForSecondsRealtime(23f);
            Check(owner.Session.State.Services.Count==17,"17 actual services"); Check(owner.Session.State.CompletedOccurrences.Count==17,"17 natural departures");
            Check(owner.Session.State.ActComplete && owner.Memory.Visible && owner.Memory.Glowing==(fixture!="quiet"),"earned memory variant after return-visit understanding");
            Check(owner.Session.Knowledge.KnownReceipts.Count==6,"six recipes learned by real services");
            yield return Click("도감  Tab");
            foreach(var r in owner.Content.Catalog.Recipes.Values) { yield return Click(owner.Content.Catalog.Cocktails[r.CocktailId].Name); yield return Capture("codex-known-"+r.Method+"-"+r.Id); }
            yield return Click("돌아가기  Esc"); yield return Click("설정  Esc"); yield return Click("글자 크기 바꾸기"); yield return Capture("settings-large"); yield return Click("돌아가기  Esc");
            Check(FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length==1,"one EventSystem");
            Finish();
        }
        private void Log(string text,string stack,LogType type) { if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) { errors.Add(text); failed=true; Finish(); } }
        private void ArchiveCheckpoint(string name)
        {
            string destination=Path.Combine(output,"checkpoints",name,"slot1"); Directory.CreateDirectory(destination);
            // Copy actual published generations for later Continue coverage; never construct or alter a save.
            foreach(string file in new[]{"current.json","previous.json"}) { string source=Path.Combine(owner.StorageRoot,"slot1",file); if(File.Exists(source)) File.Copy(source,Path.Combine(destination,file),true); }
        }
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
        private IEnumerator SelectIngredient(string id)
        {
            string name=owner.Content.IngredientNames[id]; string label=owner.Ui.GetComponentsInChildren<Button>().Select(x=>x.GetComponentInChildren<TextMeshProUGUI>()?.text).Single(x=>x==name || (x?.EndsWith("  "+name) ?? false));
            yield return Click(label); Check(owner.Input.SelectedIngredient==id,"ingredient "+id);
        }
        private IEnumerator Pour(string id,decimal ml,ContainerRole role,Key key=Key.Q)
        {
            yield return SelectIngredient(id); decimal start=owner.Session.Brew.Container(role).TotalMl; float until=Time.realtimeSinceStartup+20;
            keys=new[]{key}; Queue();
            // Allow the production adapter's maximum 0.25 s step at 30 ml/s before switching to fine flow.
            while(owner.Session.Brew.Container(role).TotalMl-start<ml-8m) { if(Time.realtimeSinceStartup>until) { Fail("Pour stalled"); yield break; } yield return null; }
            keys=Array.Empty<Key>(); Queue(); yield return null; keys=new[]{key,Key.LeftShift}; Queue();
            while(owner.Session.Brew.Container(role).TotalMl-start<ml-.12m) { if(Time.realtimeSinceStartup>until) { Fail("Fine pour stalled"); yield break; } yield return null; }
            keys=Array.Empty<Key>(); Queue(); yield return new WaitForSecondsRealtime(.08f);
            Check(Math.Abs(owner.Session.Brew.Container(role).TotalMl-start-ml)<.5m,"measured "+id);
        }
        private IEnumerator Prepare(string id)
        {
            var recipe=owner.Content.Catalog.Recipes[id]; yield return Click(char.ToUpperInvariant(recipe.Method[0])+recipe.Method.Substring(1));
            if(recipe.Method=="stir")
            {
                foreach(var a in recipe.Amounts) { if(a.Key==recipe.DashIngredient) continue; yield return Target(ContainerRole.Jigger); yield return Pour(a.Key,a.Value,ContainerRole.Jigger); yield return Press(Key.E,2); }
                yield return Target(ContainerRole.Mixing); if(recipe.DashIngredient!=null) { yield return SelectIngredient(recipe.DashIngredient); yield return Press(Key.D); } yield return Press(Key.I);
                Vector2 center=owner.Camera.WorldToScreenPoint(owner.Workbench.MixingCenter); float radius=Screen.height/(2*owner.Camera.orthographicSize)*.25f;
                pointer=center+Vector2.right*radius; Queue(); yield return null; keys=new[]{Key.S}; Queue();
                float angle=0,until=Time.realtimeSinceStartup+20;
                while(owner.Session.Brew.StirAcceptedDegrees<1080) { if(Time.realtimeSinceStartup>until) { Fail("Stir stalled"); yield break; } angle+=Mathf.Min(Time.unscaledDeltaTime,.04f)*150*Mathf.Deg2Rad; pointer=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius; Queue(); yield return null; }
                keys=Array.Empty<Key>(); Queue(); yield return null; yield return Press(Key.R,4); if(recipe.RequiredGarnish=="cherry") yield return Press(Key.C);
            }
            else if(recipe.Method=="build")
            { yield return Target(ContainerRole.Serving); yield return Press(Key.I); foreach(var a in recipe.Amounts) yield return Pour(a.Key,a.Value,ContainerRole.Serving); yield return Press(Key.G); yield return Press(Key.O); }
            else if(recipe.Method=="shake")
            {
                yield return Target(ContainerRole.Shaker); foreach(var a in recipe.Amounts) if(a.Key!=recipe.LateIngredient) yield return Pour(a.Key,a.Value,ContainerRole.Shaker);
                yield return Press(Key.I); yield return Press(Key.L); pointer=new Vector2(Screen.width*.4f,Screen.height*.5f); Queue(); yield return null;
                keys=new[]{Key.K}; Queue(); float start=Time.realtimeSinceStartup;
                while(Time.realtimeSinceStartup-start<7) { float t=Time.realtimeSinceStartup-start; pointer=new Vector2(Screen.width*(.5f-.1f*Mathf.Cos(2*Mathf.PI*t)),Screen.height*.5f); Queue(); yield return null; }
                keys=Array.Empty<Key>(); Queue(); yield return null; yield return Press(Key.L); yield return Press(Key.T,4); yield return Target(ContainerRole.Serving); yield return Pour(recipe.LateIngredient,recipe.Amounts[recipe.LateIngredient],ContainerRole.Serving,Key.X);
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
            yield return Capture("prepared-"+owner.Session.State.SlotIndex+"-"+recipe.Method); yield return Click("손님께 내기"); yield return Idle();
            var result=owner.Session.State.CurrentService?.Outcome;
            Check(owner.Session.State.Phase==CampaignPhase.Reaction && result?.RecipeId==id && result.Preparation==PreparationResult.Good && result.QualityGood,"service "+owner.Session.State.SlotIndex+" "+id);
            yield return Capture("reaction-"+owner.Session.State.SlotIndex);
        }
        private IEnumerator Capture(string label)
        {
            yield return null; Canvas.ForceUpdateCanvases(); var texts=owner.Ui.GetComponentsInChildren<TextMeshProUGUI>().Where(x=>x.isActiveAndEnabled && !string.IsNullOrWhiteSpace(x.text)).ToArray();
            var overflow=texts.Where(x=>x.isTextOverflowing).Select(x=>x.text).ToArray(); if(overflow.Length>0) errors.Add("Text overflow "+label+": "+string.Join(" | ",overflow));
            SavePixels(Path.Combine(output,(++captures).ToString("D3")+"-"+label+".png"));
            File.AppendAllText(Path.Combine(output,"capture-times.csv"),captures+","+label+","+(Time.realtimeSinceStartup-mediaStart).ToString(System.Globalization.CultureInfo.InvariantCulture)+"\n");
        }
        private void SavePixels(string path)
        { var prior=RenderTexture.active; RenderTexture.active=render; var texture=new Texture2D(render.width,render.height,TextureFormat.RGB24,false); texture.ReadPixels(new Rect(0,0,render.width,render.height),0,0); texture.Apply(); RenderTexture.active=prior; File.WriteAllBytes(path,texture.EncodeToPNG()); Destroy(texture); }
        private void Check(bool good,string label) { if(good) checks.Add(label); else Fail(label); }
        private void Fail(string reason) { failed=true; errors.Add(reason); Finish(); }
        private void Finish()
        {
            if(audioRecording) { AudioRenderer.Stop(); audioRecording=false; audio.BaseStream.Position=4; audio.Write(36+audioBytes); audio.BaseStream.Position=40; audio.Write(audioBytes); audio.Dispose(); audio=null; } media=false;
            File.WriteAllText(Path.Combine(output,"player-result.json"),JsonUtility.ToJson(new Result{status=errors.Count==0 ? "PASS" : "FAIL",tier="Simulated production InputSystem in built Player; NOT native OS input",checks=checks.ToArray(),errors=errors.ToArray(),frames=frames.ToArray(),captures=captures},true));
            UnityEngine.Application.Quit(errors.Count==0 ? 0 : 2);
        }
        [Serializable] private sealed class Result { public string status,tier; public string[] checks,errors; public float[] frames; public int captures; }
        private void OnDestroy() { UnityEngine.Application.logMessageReceived-=Log; if(keyboard!=null) InputSystem.RemoveDevice(keyboard); if(mouse!=null) InputSystem.RemoveDevice(mouse); if(render!=null) { render.Release(); Destroy(render); } }
    }
}
