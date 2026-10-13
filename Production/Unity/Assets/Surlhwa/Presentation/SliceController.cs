using System;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Domain;

namespace Surlhwa.Presentation
{
    public sealed class SliceController : MonoBehaviour
    {
        public TextAsset Bundle; public TMP_FontAsset Font; public Texture2D Grip;
        public Camera Camera; public CharacterView Bartender,Ordinary,Prince;
        public AudioClip Room,Winter,Pour,Stir,Strain,Ice,Glass,Seat,Entrance,Foot;
        public SliceContent Content { get; private set; } public SliceSession Session { get; private set; }
        public SliceInputAdapter Input { get; private set; } public SliceWorkbenchView Workbench { get; private set; }
        public SliceUi Ui { get; private set; } public SliceAudio Audio { get; private set; }
        public SliceLoadResult Saved { get; private set; } public bool Busy { get; private set; }
        public string StartupError { get; private set; } public string StorageRoot { get; private set; }
        public double LastSaveMilliseconds => store?.LastWriteMilliseconds ?? 0;
        public System.Collections.Generic.IReadOnlyDictionary<string,double> LastSaveStages => store?.LastWriteStages;
        internal SliceSaveStore EvidenceStore => store;
        private SliceSaveStore store; private Vector3 seatPosition; private SlicePhase lastPhase=(SlicePhase)(-1); private int lastEncounter=-1,lastFoot=-1; private float poseClock; private int lastCommandFrame=-1;
        public float MasterVolume { get; private set; }=.65f; public bool LargeText { get; private set; } public bool HighContrast { get; private set; }
        private readonly System.Collections.Generic.HashSet<CharacterView> missingActors=new System.Collections.Generic.HashSet<CharacterView>();
        private bool CanShow(CharacterView actor)
        {
            if(actor==null) return false;
            if(actor.Plane!=null && actor.Renderer!=null && actor.Contact!=null && actor.StandingMesh!=null && actor.SeatedMesh!=null && actor.StandingMaterial!=null && actor.SeatedMaterial!=null) return true;
            if(missingActors.Add(actor)) Debug.LogWarning("Slice character presentation missing: "+actor.name+". Logical staging remains available; visual acceptance fails.");
            return false;
        }
        private async void Start()
        {
            try
            {
                Content=SliceContentLoader.Load(Bundle.text);
                UnityEngine.Application.targetFrameRate=60;
                StorageRoot=Path.Combine(UnityEngine.Application.persistentDataPath,"SurlhwaProduction","v1","slice");
                string[] args=Environment.GetCommandLineArgs(); int slot=System.Array.IndexOf(args,"-sliceEvidenceRoot");
                if(slot>=0 && slot+1<args.Length) StorageRoot=Path.GetFullPath(args[slot+1]);
                int saveSlot=System.Array.IndexOf(args,"-sliceSaveRoot"); if(saveSlot>=0 && saveSlot+1<args.Length) StorageRoot=Path.GetFullPath(args[saveSlot+1]);
                LoadSettings(); store=new SliceSaveStore(Path.Combine(StorageRoot,"slot1"),Content);
                Input=gameObject.AddComponent<SliceInputAdapter>(); Input.UiCommand=HandleCommand; Input.Configure(Content.Profile.Supply);
                var work=new GameObject("Resident Stir workbench"); work.transform.position=new Vector3(.80f,1.055f,-.65f);
                Workbench=work.AddComponent<SliceWorkbenchView>(); Workbench.Initialize(Grip);
                Audio=gameObject.AddComponent<SliceAudio>(); Audio.Initialize(this); Input.AcceptedCue+=Audio.Cue;
                Ui=gameObject.AddComponent<SliceUi>(); Ui.Initialize(this,Font);
                seatPosition=Ordinary!=null ? Ordinary.transform.position : new Vector3(-.72f,0,-2);
                if(CanShow(Bartender)) Bartender.Show(true,false); if(CanShow(Ordinary)) Ordinary.Show(false,true); if(CanShow(Prince)) Prince.Show(false,true);
                Busy=true; Saved=await Task.Run(()=>store.Load());
            }
            catch(Exception e) { StartupError=e.Message; Debug.LogError("SLICE_STARTUP: "+e.Message); }
            finally { Busy=false; }
        }
        public void HandleCommand(string command)
        {
            if(Busy || Session?.IsSaving==true || lastCommandFrame==Time.frameCount) return;
            lastCommandFrame=Time.frameCount;
            if(Session?.SaveError!=null) {
                if(command=="advance" && Ui.VisibleModal=="save_error") Ui.ActivateFocused(true);
                return;
            }
            if(command=="back") { if(Session?.Modals.Count>0) CloseModal(); else if(Ui.MenuModal!=null) Ui.CloseMenu(); else OpenModal("settings"); return; }
            if(command=="guide" || command=="codex" || command=="settings") { OpenModal(command); return; }
            if(command=="glass") { if(Session?.CanInteract==true && Session.State.Phase==SlicePhase.Brewing) Workbench.CocktailGlass=!Workbench.CocktailGlass; return; }
            if(command=="advance") { if(Ui.ActivateFocused()) return; Advance(); }
        }
        public void OpenModal(string name)
        { Input.Stop(); if(Session!=null) Session.OpenModal(name); else Ui.MenuModal=name; Ui.RememberFocus(); }
        public void CloseModal() { Input.Stop(); if(Session!=null) Session.CloseModal(); else Ui.MenuModal=null; Ui.RestoreFocus(); }
        public async void BeginNew(bool confirmed=false)
        {
            if(Busy || Session?.IsSaving==true) return;
            if(!confirmed && (Saved?.Checkpoint!=null || Session!=null)) { OpenModal("reset"); return; }
            Busy=true; Input.Stop(); Session?.Dispose();
            Session=await SliceSession.StartAsync(Content,store); Input.Bind(Session,Camera,Workbench.MixingCenter); Ui.CloseMenu(); Busy=false;
        }
        public void ContinueRun(bool confirmed=false)
        {
            if(Busy || Saved?.Checkpoint==null) return;
            if(Saved.RecoveryRequired && !confirmed) { Ui.MenuModal="recovery"; return; }
            Session?.Dispose(); Session=SliceSession.Continue(Content,store,Saved.Checkpoint); Input.Bind(Session,Camera,Workbench.MixingCenter); Ui.CloseMenu();
        }
        public async void Advance()
        {
            if(Session==null || Busy) return; Input.Stop(); await Session.AdvanceAsync(Input.NextEventId());
        }
        public async void RetrySave() { if(Session!=null) await Session.RetrySaveAsync(); }
        public void DiscardDrink()
        {
            Input.Stop(); if(Session?.DiscardBrew(Input.NextEventId())==true) Input.Bind(Session,Camera,Workbench.MixingCenter);
        }
        public async void ReturnToSavedMenu()
        {
            if(Busy || Session?.IsSaving==true) return;
            Busy=true; Input.Stop(); Session?.Dispose(); Session=null; Saved=null; Ui.CloseMenu(); Audio.Refresh(Input,false);
            if(CanShow(Ordinary)) Ordinary.Show(false,true); if(CanShow(Prince)) Prince.Show(false,true);
            try { Saved=await Task.Run(()=>store.Load()); StartupError=null; } catch(Exception e) { StartupError=e.Message; }
            finally { Busy=false; }
        }
        private async void Update()
        {
            if(Content==null || Ui==null) return;
            if(Session!=null)
            {
                Input.Step(Time.unscaledDeltaTime); AnimateGuest();
                Workbench.Refresh(Session.Brew,Input,Time.unscaledDeltaTime,Session.State.CurrentService?.Snapshot); Audio.Refresh(Input,Session.CanInteract);
                Ui.Refresh(); await Session.TickAsync(Math.Min(Time.unscaledDeltaTime,.25f));
            }
            else { Workbench.Refresh(null,null,0); Ui.Refresh(); }
        }
        private void AnimateGuest()
        {
            var phase=Session.State.Phase; int encounter=Session.State.EncounterIndex;
            if(lastPhase!=phase || lastEncounter!=encounter) { poseClock=0; lastFoot=-1; lastPhase=phase; lastEncounter=encounter; if(phase==SlicePhase.Arriving) Audio.Cue("entrance"); if(phase==SlicePhase.Intro) Audio.Cue("seat"); }
            if(Session.CanInteract) poseClock+=Time.unscaledDeltaTime;
            CharacterView actor=encounter==0 ? Ordinary : Prince,other=encounter==0 ? Prince : Ordinary;
            if(CanShow(other)) other.Show(false,true);
            if(!CanShow(actor)) return; // Logical stage clock does not depend on a renderer or clip.
            bool traveling=phase==SlicePhase.Arriving || phase==SlicePhase.Departing;
            if(phase==SlicePhase.Welcome || phase==SlicePhase.Complete) { actor.Show(false,true); return; }
            if(traveling)
            {
                int foot=(int)(poseClock*3); if(Session.CanInteract && foot!=lastFoot) { lastFoot=foot; Audio.Cue("foot"); }
                Vector3 entry=new Vector3(-4.25f,0,-2.65f), seat=seatPosition;
                float t=Mathf.SmoothStep(0,1,poseClock/1.5f); if(phase==SlicePhase.Departing) t=1-t;
                actor.transform.position=Vector3.Lerp(entry,seat,t);
                int frame=(int)(poseClock*6);
                bool canWalk=actor.WalkMeshes!=null && actor.WalkMeshes.Length>0 && actor.WalkMaterials!=null && actor.WalkMaterials.Length>0
                    && actor.WalkMeshes[frame%actor.WalkMeshes.Length]!=null && actor.WalkMaterials[frame%actor.WalkMaterials.Length]!=null;
                if(canWalk) actor.ShowWalkFrame(frame); else actor.Show(true,false);
            }
            else { actor.transform.position=seatPosition; actor.Show(true,true); }
        }
        private void OnApplicationFocus(bool focus) { Input?.Stop(); Session?.SetFocus(focus); }
        private void OnDestroy() { Session?.Dispose(); if(Workbench!=null) Destroy(Workbench.gameObject); }
        [Serializable] private sealed class Settings { public int schemaVersion=1; public float master=.65f; public bool largeText,highContrast; }
        private void LoadSettings()
        {
            var p=Path.Combine(StorageRoot,"settings.json"); if(!File.Exists(p)) return;
            try { var s=JsonUtility.FromJson<Settings>(File.ReadAllText(p)); if(s.schemaVersion!=1 || float.IsNaN(s.master) || s.master<0 || s.master>1) return; MasterVolume=s.master; LargeText=s.largeText; HighContrast=s.highContrast; }
            catch(Exception e) { Debug.LogWarning("Settings were not loaded: "+e.Message); }
        }
        public void ChangeSettings(float volume,bool large,bool contrast)
        {
            MasterVolume=Mathf.Clamp01(volume); LargeText=large; HighContrast=contrast;
            try {
                Directory.CreateDirectory(StorageRoot); string path=Path.Combine(StorageRoot,"settings.json"),temp=path+".pending";
                File.WriteAllText(temp,JsonUtility.ToJson(new Settings{master=MasterVolume,largeText=large,highContrast=contrast}));
                if(File.Exists(path)) File.Replace(temp,path,path+".previous"); else File.Move(temp,path);
            } catch(Exception e) { StartupError="설정 저장 실패: "+e.Message; }
        }
    }
}
