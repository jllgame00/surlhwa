using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Domain;

namespace Surlhwa.Presentation
{
    public sealed class CampaignController : MonoBehaviour
    {
        public TextAsset Bundle,LegacyBundle; public TMP_FontAsset Font; public Texture2D Grip;
        public CampaignArtSet Art;
        public Camera Camera; public CharacterView Bartender,Ordinary,Prince,Chunhyang,Mongryong;
        public AudioClip Room,Winter,Pour,Stir,Strain,Ice,Glass,Seat,Entrance,Foot,Music;
        public CampaignContent Content { get; private set; } public CampaignSession Session { get; private set; }
        public SliceInputAdapter Input { get; private set; } public SliceWorkbenchView Workbench { get; private set; }
        public CampaignUi Ui { get; private set; } public CampaignAudio Audio { get; private set; }
        public CampaignMemoryView Memory { get; private set; }
        public CampaignCastView Cast { get; private set; }
        public bool LegacyContinuationPending { get; private set; }
        public CampaignLoadResult Saved { get; private set; } public bool Busy { get; private set; }
        public string StartupError { get; private set; } public string StorageRoot { get; private set; }
        
        
        internal CampaignSaveStore EvidenceStore => store;
        private CampaignSaveStore store; private Vector3 seatPosition; private CampaignPhase lastPhase=(CampaignPhase)(-1); private int lastEncounter=-1,lastFoot=-1; private float poseClock; private int lastCommandFrame=-1;
        private CampaignContinuation continuation; private string legacyRoot;
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
                Content=CampaignContentLoader.Load(Bundle.text);
                UnityEngine.Application.targetFrameRate=60;
                legacyRoot=Path.Combine(UnityEngine.Application.persistentDataPath,"SurlhwaProduction","v1","campaign-act1");
                StorageRoot=Content.Scope=="act_01" ? legacyRoot : Path.Combine(UnityEngine.Application.persistentDataPath,"SurlhwaProduction","v1","campaign-through-act-02");
                string[] args=Environment.GetCommandLineArgs(); int slot=System.Array.IndexOf(args,"-campaignEvidenceRoot");
                if(slot>=0 && slot+1<args.Length) StorageRoot=Path.GetFullPath(args[slot+1]);
                int saveSlot=System.Array.IndexOf(args,"-campaignSaveRoot"); if(saveSlot>=0 && saveSlot+1<args.Length) StorageRoot=Path.GetFullPath(args[saveSlot+1]);
                int legacySlot=System.Array.IndexOf(args,"-campaignLegacySaveRoot"); if(legacySlot>=0 && legacySlot+1<args.Length) legacyRoot=Path.GetFullPath(args[legacySlot+1]);
                if(Content.Scope!="act_01") continuation=new CampaignContinuation(LegacyBundle!=null ? LegacyBundle.text : null,Bundle.text);
                LoadSettings(); store=new CampaignSaveStore(Path.Combine(StorageRoot,"slot1"),Content);
                Input=gameObject.AddComponent<SliceInputAdapter>(); Input.UiCommand=HandleCommand; Input.Configure(Content.Supply);
                var work=new GameObject("Resident ACT 1 workbench"); work.transform.position=new Vector3(.80f,1.055f,-.65f);
                Workbench=work.AddComponent<SliceWorkbenchView>(); Workbench.Initialize(Art!=null ? Art.Grip : Grip); Workbench.ExtendedTools=true;
                if(Art!=null) { Workbench.ApplyArt(Art); gameObject.AddComponent<CampaignCharacterArt>().Initialize(this); }
                Audio=gameObject.AddComponent<CampaignAudio>(); Audio.Initialize(this); Input.AcceptedCue+=Audio.Cue;
                Ui=gameObject.AddComponent<CampaignUi>(); Ui.Initialize(this,Font); Memory=gameObject.AddComponent<CampaignMemoryView>();
                seatPosition=Ordinary!=null ? Ordinary.transform.position : new Vector3(-.72f,0,-2);
                Cast=GetComponent<CampaignCastView>() ?? gameObject.AddComponent<CampaignCastView>(); Cast.Initialize(this,seatPosition); Cast.Hide();
                if(CanShow(Bartender)) Bartender.Show(true,false); if(CanShow(Ordinary)) Ordinary.Show(false,true); if(CanShow(Prince)) Prince.Show(false,true);
                Busy=true; await LoadSavedAsync();
            }
            catch(Exception e) { StartupError=e.Message; Debug.LogError("CAMPAIGN_STARTUP: "+e.Message); }
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
            if(command=="glass") { if(Session?.CanInteract==true && Session.State.Phase==CampaignPhase.Brewing) Workbench.CocktailGlass=!Workbench.CocktailGlass; return; }
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
            Session=await CampaignSession.StartAsync(Content,store); LegacyContinuationPending=false; StartupError=null; Input.Bind(Session,Camera,Workbench.MixingCenter); Ui.CloseMenu(); Busy=false;
        }
        private async Task LoadSavedAsync()
        {
            LegacyContinuationPending=false;
            Saved=await Task.Run(()=>store.Load());
            if(continuation!=null && !store.HasHistory)
            {
                Saved=await Task.Run(()=>continuation.LoadSource(Path.Combine(legacyRoot,"slot1")));
                LegacyContinuationPending=Saved.Checkpoint!=null;
            }
        }
        public async void ContinueRun(bool confirmed=false)
        {
            if(Busy || Saved?.Checkpoint==null) return;
            if(Saved.RecoveryRequired && !confirmed) { Ui.MenuModal="recovery"; return; }
            Busy=true; Input.Stop();
            try {
                if(LegacyContinuationPending) {
                    Saved=await continuation.LoadOrImportAsync(Path.Combine(legacyRoot,"slot1"),store,confirmed);
                    if(Saved.Checkpoint==null) { StartupError=Saved.Message; return; }
                    if(Saved.RecoveryRequired && !confirmed) { Ui.MenuModal="recovery"; return; }
                    LegacyContinuationPending=false;
                }
                Session?.Dispose(); Session=CampaignSession.Continue(Content,store,Saved.Checkpoint); Input.Bind(Session,Camera,Workbench.MixingCenter); Ui.CloseMenu(); StartupError=null;
            } catch(Exception e) { StartupError="기록을 이어오지 못했습니다: "+e.Message; }
            finally { Busy=false; }
        }
        public async void Advance()
        {
            if(Session==null || Busy) return; Input.Stop(); await Session.AdvanceAsync(Input.NextEventId());
        }
        public async void Choose(string choice) { if(Session!=null && !Busy) { Input.Stop(); await Session.ChooseAsync(choice,Input.NextEventId()); } }
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
            Cast?.Hide();
            try { await LoadSavedAsync(); StartupError=null; } catch(Exception e) { StartupError=e.Message; }
            finally { Busy=false; }
        }
        private async void Update()
        {
            if(Content==null || Ui==null) return;
            if(Session!=null)
            {
                Input.ZestAvailable=Session.State.SlotIndex>=17 && Content.Catalog.Recipes.Values.Any(r=>r.RequiredGarnish=="orange_zest");
                Input.Step(Time.unscaledDeltaTime); AnimateGuest();
                Workbench.Refresh(Session.Brew,Input,Time.unscaledDeltaTime,Session.State.CurrentService?.Snapshot); Audio.Refresh(Input,Session.CanInteract);
                Ui.Refresh(); Memory.Refresh(Session.State,seatPosition); await Session.TickAsync(Math.Min(Time.unscaledDeltaTime,.25f));
            }
            else { Cast?.Hide(); Workbench.Refresh(null,null,0); Memory.Refresh(null,seatPosition); Ui.Refresh(); }
        }
        private void AnimateGuest()
        {
            var phase=Session.State.Phase; int encounter=Session.State.SlotIndex;
            if(lastPhase!=phase || lastEncounter!=encounter) { poseClock=0; lastFoot=-1; lastPhase=phase; lastEncounter=encounter; if(phase==CampaignPhase.Arriving) Audio.Cue("entrance"); if(phase==CampaignPhase.Intro) Audio.Cue("seat"); }
            if(Session.CanInteract) poseClock+=Time.unscaledDeltaTime;
            if(CampaignCastView.OwnsGuest(Session.CurrentEncounter.GuestId)) {
                if(CanShow(Prince)) Prince.Show(false,true); if(CanShow(Ordinary)) Ordinary.Show(false,true);
                Cast.Refresh(phase,poseClock,seatPosition); return;
            }
            Cast?.Hide();
            bool special=Content.Guests[Session.CurrentEncounter.GuestId].Kind==GuestKind.Special;
            CharacterView actor=special ? Prince : Ordinary,other=special ? Ordinary : Prince;
            if(CanShow(other)) other.Show(false,true);
            if(!CanShow(actor)) return; // Logical stage clock does not depend on a renderer or clip.
            bool traveling=phase==CampaignPhase.Arriving || phase==CampaignPhase.Departing;
            if(phase==CampaignPhase.Welcome || phase==CampaignPhase.Complete || phase==CampaignPhase.NightIntro || phase==CampaignPhase.NightEnd) { actor.Show(false,true); return; }
            if(traveling)
            {
                Vector3 entry=new Vector3(-4.25f,0,-2.65f), seat=seatPosition;
                bool departing=phase==CampaignPhase.Departing;
                float walkClock=departing ? Mathf.Max(0,poseClock-.18f) : poseClock;
                bool walking=departing ? poseClock>=.18f : poseClock<1.25f;
                float duration=departing ? 1.32f : 1.25f;
                float t=Mathf.SmoothStep(0,1,walkClock/duration); if(departing) t=1-t;
                actor.transform.position=Vector3.Lerp(entry,seat,t);
                int frame=(int)(walkClock*6),foot=frame/2;
                if(walking && Session.CanInteract && foot!=lastFoot) { lastFoot=foot; Audio.Cue("foot"); }
                bool canWalk=actor.WalkMeshes!=null && actor.WalkMeshes.Length>0 && actor.WalkMaterials!=null && actor.WalkMaterials.Length>0
                    && actor.WalkMeshes[frame%actor.WalkMeshes.Length]!=null && actor.WalkMaterials[frame%actor.WalkMaterials.Length]!=null;
                if(walking && canWalk) actor.ShowWalkFrame(frame); else actor.Show(true,!walking && (!departing || poseClock<.09f));
            }
            else { actor.transform.position=seatPosition; actor.Show(true,true); }
        }
        private void OnApplicationFocus(bool focus) { Input?.Stop(); Session?.SetFocus(focus); }
        private void OnDestroy() { Session?.Dispose(); if(Workbench!=null) Destroy(Workbench.gameObject); }
        [Serializable] private sealed class Settings { public int schemaVersion=1; public float master=.65f; public bool largeText,highContrast; }
        private void LoadSettings()
        {
            var p=Path.Combine(StorageRoot,"settings.json");
            if(!File.Exists(p) && Content?.Scope!="act_01" && legacyRoot!=null) p=Path.Combine(legacyRoot,"settings.json");
            if(!File.Exists(p)) return;
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
