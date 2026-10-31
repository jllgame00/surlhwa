using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Surlhwa.Domain;
namespace Surlhwa.Presentation
{
    // Read-only development probe. No commands, fixture state, or synthetic devices.
    public sealed class CampaignEvidenceProbe : MonoBehaviour
    {
        private CampaignController owner; private string output; private float next; private int capture;
        private bool armed,interrupted; private long sequence=-1;
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
        private static int Foreground() { GetWindowThreadProcessId(GetForegroundWindow(),out uint p); return (int)p; }
        private void Start()
        {
            owner=GetComponent<CampaignController>(); var args=Environment.GetCommandLineArgs(); int i=Array.IndexOf(args,"-campaignEvidenceRoot");
            if(i<0 || i+1>=args.Length) { enabled=false; return; } output=Path.GetFullPath(args[i+1]); Directory.CreateDirectory(output);
            UnityEngine.Application.logMessageReceived+=Log;
        }
        private void Log(string message,string stack,LogType kind)
        { if(kind==LogType.Error || kind==LogType.Exception || kind==LogType.Assert) File.AppendAllText(Path.Combine(output,"errors.log"),kind+": "+message+"\n"+stack+"\n"); }
        [Serializable] public sealed class Control { public string label; public float x,y,width,height; public bool enabled; }
        [Serializable] public sealed class Observation
        {
            public string observedUtc,phase,modal,ingredient,target,recipe,recognition,preparation,saveError,textRole,textId,displayedSpeaker,tool,guest,scope,scene;
            public int processId,foregroundProcessId,encounter,line,act,services,known,memories,shakeReversals,layerCount,eventSystems,audioListeners;
            public long sequence; public bool focused,nativeOwnershipEstablished,nativeInterrupted,saving,fits,quality,actComplete,shakerClosed,memoryVisible,memoryGlowing;
            public double jigger,mixing,shaker,serving,stirDegrees,frameMs,stirX,stirY,stirRadius,pointerX,pointerY;
            public string[] visibleText,overflowText,guestIds,visibleActors,actorBindingIds,invalidRenderers; public bool loversMemoryVisible,windmillMemoryVisible,hamletMemoryVisible,faustMemoryVisible; public Control[] controls;
        }
        private Control Read(Button button)
        {
            var corners=new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
            var a=RectTransformUtility.WorldToScreenPoint(owner.Camera,corners[0]); var b=RectTransformUtility.WorldToScreenPoint(owner.Camera,corners[2]);
            return new Control{label=button.GetComponentInChildren<TextMeshProUGUI>()?.text,x=a.x,y=Screen.height-b.y,width=b.x-a.x,height=b.y-a.y,enabled=button.IsInteractable()};
        }
        private void Update()
        {
            if(output==null || owner?.Ui==null) return;
            int pid=System.Diagnostics.Process.GetCurrentProcess().Id; bool owned=Foreground()==pid && UnityEngine.Application.isFocused;
            if(armed && !owned) interrupted=true; if(owned) armed=true;
            if(Keyboard.current!=null) foreach(var key in Keyboard.current.allKeys.Where(x=>x.wasPressedThisFrame))
                File.AppendAllText(Path.Combine(output,"input-observed.jsonl"),"{\"utc\":\""+DateTime.UtcNow.ToString("o")+"\",\"key\":\""+key.keyCode+"\",\"owned\":"+owned.ToString().ToLowerInvariant()+"}\n");
            if(Keyboard.current?.f12Key.wasPressedThisFrame==true) StartCoroutine(Capture());
            if(Time.realtimeSinceStartup>=next) { next=Time.realtimeSinceStartup+.1f; Observe(); }
        }
        private void Observe()
        {
            var s=owner.Session; var outcome=s?.State.CurrentService?.Outcome;
            var texts=owner.Ui.GetComponentsInChildren<TextMeshProUGUI>().Where(x=>x.isActiveAndEnabled).ToArray();
            Vector2 point=owner.Camera.WorldToScreenPoint(owner.Workbench.MixingCenter),pointer=Mouse.current?.position.ReadValue() ?? Vector2.zero;
            var o=new Observation{observedUtc=DateTime.UtcNow.ToString("o"),phase=s?.State.Phase.ToString() ?? "menu",modal=s?.Modals.Top ?? owner.Ui.MenuModal,
                processId=System.Diagnostics.Process.GetCurrentProcess().Id,foregroundProcessId=Foreground(),focused=UnityEngine.Application.isFocused,nativeOwnershipEstablished=armed,nativeInterrupted=interrupted,
                scope=owner.Content?.Scope,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,act=s==null ? 0 : owner.Content.ActAt(s.State.SlotIndex),
                encounter=s?.State.SlotIndex ?? -1,line=s?.State.LineIndex ?? -1,services=s?.State.Services.Count ?? 0,known=s?.Knowledge.KnownReceipts.Count ?? 0,memories=s?.State.Memories.Count ?? 0,sequence=s?.State.Sequence ?? 0,
                saving=s?.IsSaving ?? owner.Busy,ingredient=owner.Input.SelectedIngredient,target=owner.Input.Target.ToString(),tool=owner.Input.ActiveTool,guest=s?.CurrentEncounter.GuestId,
                recipe=outcome?.RecipeId,recognition=outcome?.Recognition.ToString(),preparation=outcome?.Preparation.ToString(),fits=outcome?.Fits ?? false,quality=outcome?.QualityGood ?? false,actComplete=s?.State.ActComplete ?? false,
                jigger=(double)(s?.Brew?.Container(ContainerRole.Jigger).TotalMl ?? 0),mixing=(double)(s?.Brew?.Container(ContainerRole.Mixing).TotalMl ?? 0),shaker=(double)(s?.Brew?.Container(ContainerRole.Shaker).TotalMl ?? 0),serving=(double)(s?.Brew?.Container(ContainerRole.Serving).TotalMl ?? s?.State.CurrentService?.Snapshot.TotalMl ?? 0),
                stirDegrees=s?.Brew?.StirAcceptedDegrees ?? 0,shakeReversals=s?.Brew?.ShakeReversals ?? 0,shakerClosed=s?.Brew?.ShakerClosed ?? false,layerCount=owner.Workbench.VisibleLayerCount,
                stirX=point.x,stirY=Screen.height-point.y,stirRadius=Screen.height/(2*owner.Camera.orthographicSize)*.25f,pointerX=pointer.x,pointerY=Screen.height-pointer.y,
                textRole=s?.CurrentText?.Role,textId=s?.CurrentText?.Id,displayedSpeaker=owner.Ui.DisplayedSpeaker,saveError=s?.SaveError ?? owner.StartupError,memoryVisible=owner.Memory.Visible,memoryGlowing=owner.Memory.Glowing,loversMemoryVisible=owner.Memory.LoversVisible,windmillMemoryVisible=owner.Memory.WindmillVisible,hamletMemoryVisible=owner.Memory.HamletVisible,faustMemoryVisible=owner.Memory.FaustVisible,
                guestIds=s?.CurrentEncounter.GuestIds.ToArray() ?? Array.Empty<string>(),visibleActors=VisibleActors().Select(a=>PresentedGuestId(owner,a)).ToArray(),actorBindingIds=VisibleActors().Select(a=>a.GuestId).ToArray(),invalidRenderers=InvalidVisibleAssets(),
                eventSystems=FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length,audioListeners=FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(x=>x.isActiveAndEnabled),
                frameMs=Time.unscaledDeltaTime*1000,visibleText=texts.Select(x=>x.text).ToArray(),overflowText=texts.Where(x=>!string.IsNullOrWhiteSpace(x.text) && x.isTextOverflowing).Select(x=>x.text).ToArray(),
                controls=owner.Ui.GetComponentsInChildren<Button>().Where(x=>x.isActiveAndEnabled).Select(Read).ToArray()};
            string json=JsonUtility.ToJson(o,true),path=Path.Combine(output,"observation.json");
            TryPublish(path,json); // An external reader may briefly deny replacement; the next observation retries.
            if(sequence!=o.sequence) { sequence=o.sequence; File.AppendAllText(Path.Combine(output,"transitions.jsonl"),JsonUtility.ToJson(o)+"\n"); }
        }
        internal static string[] InvalidVisibleAssets()
        {
            var invalid=FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled && !r.forceRenderingOff && r.gameObject.activeInHierarchy &&
                (r.sharedMaterials.Length==0 || r.sharedMaterials.Any(m=>m==null || m.shader==null || m.shader.name=="Hidden/InternalErrorShader"))).Select(r=>r.name+": material/shader");
            var actors=FindObjectsByType<CharacterView>(FindObjectsSortMode.None).Where(a=>a.Renderer!=null && a.Renderer.enabled && a.gameObject.activeInHierarchy &&
                (a.Plane==null || a.Plane.sharedMesh==null || a.Renderer.sharedMaterial==null || a.Renderer.sharedMaterial.mainTexture==null)).Select(a=>a.name+": mesh/texture");
            return invalid.Concat(actors).ToArray();
        }
        internal static CharacterView[] VisibleActors() => FindObjectsByType<CharacterView>(FindObjectsSortMode.None).Where(a=>a.Renderer!=null && a.Renderer.enabled && a.gameObject.activeInHierarchy).ToArray();
        internal static string PresentedGuestId(CampaignController controller,CharacterView actor)
        {
            // The accepted ordinary view is shared by authored ordinary encounters.
            // Preserve its raw binding separately; lovers retain their distinct actor IDs.
            var encounter=controller.Session?.CurrentEncounter;
            return actor==controller.Ordinary && encounter!=null && controller.Content.Guests[encounter.GuestId].Kind==GuestKind.Ordinary ? encounter.GuestId : actor.GuestId;
        }
        private static bool TryPublish(string path,string json)
        {
            string temp=path+".pending";
            try { File.WriteAllText(temp,json); if(File.Exists(path)) File.Replace(temp,path,null); else File.Move(temp,path); return true; }
            catch(IOException) { return false; } // Evidence observers must not interrupt production gameplay.
        }
        private IEnumerator Capture()
        {
            yield return new WaitForEndOfFrame(); var texture=ScreenCapture.CaptureScreenshotAsTexture(); string prefix=(++capture).ToString("D3")+"-"+(owner.Session?.State.Phase.ToString() ?? "menu");
            File.WriteAllBytes(Path.Combine(output,prefix+".png"),texture.EncodeToPNG()); Destroy(texture); Observe(); File.Copy(Path.Combine(output,"observation.json"),Path.Combine(output,prefix+".json"),true);
        }
        private void OnDestroy() { UnityEngine.Application.logMessageReceived-=Log; }
    }
}
