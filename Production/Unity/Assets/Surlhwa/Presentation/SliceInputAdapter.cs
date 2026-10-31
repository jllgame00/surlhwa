using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Surlhwa.Application;
using Surlhwa.Domain;

namespace Surlhwa.Presentation
{
    public sealed class SliceInputAdapter : MonoBehaviour
    {
        private readonly List<InputAction> actions=new List<InputAction>(); private IBrewingSession session; private Camera view;
        private Vector3 mixing; private long serial=10000; private string held; private Vector2 previousPointer;
        public string SelectedIngredient { get; private set; }="whiskey";
        public ContainerRole Target { get; private set; }=ContainerRole.Jigger;
        public string ActiveTool => held; public bool Fine { get; set; }
        public bool ZestAvailable { get; set; }
        public bool ShakerStrainAvailable { get; set; }
        public bool AcceptedLiquidThisStep { get; private set; } public bool AcceptedStirThisStep { get; private set; }
        public float SpoonAngle { get; private set; } public string Feedback { get; private set; }
        public float ShakePosition { get; private set; } public float FloatHeight { get; private set; } public float FloatIntensity { get; private set; }
        public Action<string> UiCommand; public event Action<string> AcceptedCue;
        public long NextEventId() => ++serial;
        public void Bind(IBrewingSession value,Camera camera,Vector3 mixingCenter)
        {
            Stop(); session=value; view=camera; mixing=mixingCenter; Target=ContainerRole.Jigger; Fine=false; SelectedIngredient=value.Supply.First();
            Configure(value.Supply);
        }
        public void Configure(IEnumerable<string> supply)
        {
            if(actions.Count>0) return;
            ActionKey("pour","q",true); ActionKey("transfer","e",true); ActionKey("strain","r",true); ActionKey("stir","s",true);
            ActionKey("shake","k",true); ActionKey("float","f",true); ActionKey("shaker_transfer","t",true); ActionKey("splash","x",true);
            ActionKey("shaker_strain","u",true);
            ActionKey("lid","l",false); ActionKey("gentle","g",false); ActionKey("orange","o",false);
            ActionKey("zest","z",false);
            ActionKey("ice","i",false); ActionKey("cherry","c",false); ActionKey("dash","d",false);
            ActionKey("target","j",false); ActionKey("glass","v",false); ActionKey("guide","b",false);
            ActionKey("codex","tab",false); ActionKey("back","escape",false); ActionKey("advance","enter",false);
            string[] ingredients=supply.ToArray();
            for(int n=0;n<Math.Min(9,ingredients.Length);n++) { string id=ingredients[n]; var a=new InputAction("ingredient"+n,InputActionType.Button,"<Keyboard>/"+(n+1)); a.performed+=_=>SelectIngredient(id); a.Enable(); actions.Add(a); }
        }
        private void ActionKey(string command,string key,bool continuous)
        {
            var a=new InputAction(command,InputActionType.Button,"<Keyboard>/"+key);
            a.performed+=_=> { if(continuous) Begin(command); else Command(command); };
            if(continuous) a.canceled+=_=> {
                // An unavailable U cannot cancel another tool. An available U still
                // releases the input latch after a modal/focus stop cleared its tool.
                if(command!="shaker_strain" || held==command || (ShakerStrainAvailable && held==null)) Release();
            }; a.Enable(); actions.Add(a);
        }
        public void SelectIngredient(string id)
        { if(session==null || !session.CanInteract || !session.Supply.Contains(id)) return; Stop(); SelectedIngredient=id; Feedback=null; }
        public void SelectTarget(ContainerRole role) { if(session==null || !session.CanInteract) return; Stop(); Target=role; Feedback=null; }
        public void Command(string command)
        {
            if(command=="guide" || command=="codex" || command=="back" || command=="advance" || command=="glass") { UiCommand?.Invoke(command); return; }
            if(session==null) return;
            if(command=="target") { SelectTarget((ContainerRole)(((int)Target+1)%(session.ExtendedMethods ? 4 : 3))); return; }
            if(!session.CanInteract) return;
            if(command=="zest" && (!ZestAvailable || !session.ExtendedMethods)) return;
            if(!session.ExtendedMethods && (command=="lid" || command=="gentle" || command=="orange")) return;
            try
            {
                Stop(); bool accepted=session.ExecuteBrew(++serial,b=> {
                    if(command=="ice") b.AddIce(Target);
                    else if(command=="cherry") b.Garnish("cherry","in_glass");
                    else if(command=="orange") b.Garnish("orange_slice","rim");
                    else if(command=="zest") b.Garnish("orange_zest","rim");
                    else if(command=="gentle") b.GentleStir();
                    else if(command=="lid") b.SetShakerClosed(!b.ShakerClosed);
                    else if(command=="dash") b.Pour(SelectedIngredient,Target,session.Tuning.DashMl,BrewAction.Dash);
                });
                if(accepted) { AcceptedCue?.Invoke(command); Feedback=null; }
            }
            catch(InvalidOperationException) { Feedback=command=="cherry" ? "서빙 잔이 비어 있어요" : "도구와 재료를 확인해요"; }
            catch(ArgumentException) { Feedback="도구와 재료를 확인해요"; }
        }
        public void Begin(string tool)
        {
            if(session==null || held!=null || !session.CanInteract) return;
            if(tool=="shaker_strain" && !ShakerStrainAvailable) return;
            if(!session.ExtendedMethods && tool!="pour" && tool!="transfer" && tool!="strain" && tool!="stir") return;
            try
            {
                bool accepted=session.ExecuteBrew(++serial,b=> {
                    if(tool=="pour") b.BeginPour(SelectedIngredient,Target);
                    else if(tool=="splash") b.BeginPour(SelectedIngredient,Target,BrewAction.Splash);
                    else if(tool=="transfer") b.BeginTransfer(ContainerRole.Jigger,session.ExtendedMethods && Target!=ContainerRole.Jigger ? Target : ContainerRole.Mixing,BrewAction.PourFromContainer);
                    else if(tool=="strain") b.BeginTransfer(ContainerRole.Mixing,ContainerRole.Serving,BrewAction.Strain);
                    else if(tool=="stir") { var p=StirPoint(); b.BeginStir(p.x,p.y); }
                    else if(tool=="shake") b.BeginShake(ReadShake());
                    else if(tool=="shaker_transfer") b.BeginTransfer(ContainerRole.Shaker,ContainerRole.Serving,BrewAction.PourFromContainer);
                    else if(tool=="shaker_strain") b.BeginTransfer(ContainerRole.Shaker,ContainerRole.Serving,BrewAction.Strain);
                    else if(tool=="float") b.BeginFloat();
                });
                if(accepted && (session.Brew.FlowActive || session.Brew.StirActive || session.Brew.ShakeActive)) { held=tool; previousPointer=Mouse.current?.position.ReadValue() ?? Vector2.zero; Feedback=null; }
                else if(accepted && tool=="stir") Feedback="술과 얼음이 필요해요";
            }
            catch(InvalidOperationException) { Feedback="도구와 재료를 확인해요"; }
            catch(ArgumentException) { Feedback="도구와 재료를 확인해요"; }
        }
        public void Release() { Stop(); session?.ReleaseHeldInput(); }
        public void Stop() { session?.Brew?.CancelInput(); held=null; AcceptedLiquidThisStep=false; AcceptedStirThisStep=false; }
        private Vector2 StirPoint()
        {
            Vector2 center=view!=null ? (Vector2)view.WorldToScreenPoint(mixing) : new Vector2(Screen.width*.5f,Screen.height*.5f);
            float radius=view!=null ? Screen.height/(2*view.orthographicSize)*.28f : 30;
            return ((Mouse.current?.position.ReadValue() ?? center)-center)/radius;
        }
        private float ReadShake() => ShakePosition=Mathf.Clamp(((Mouse.current?.position.ReadValue().x ?? Screen.width*.5f)/Screen.width-.5f)/.1f,-1,1);
        private void ReadFloat()
        {
            var p=Mouse.current?.position.ReadValue() ?? new Vector2(Screen.width*.4f,Screen.height*.35f);
            FloatIntensity=Mathf.Clamp01((p.x/Screen.width-.4f)/.2f);
            FloatHeight=Mathf.Clamp01((p.y/Screen.height-.35f)/.3f);
        }
        public void Step(double seconds)
        {
            AcceptedLiquidThisStep=false; AcceptedStirThisStep=false;
            if(session==null || !session.CanInteract || session.Brew==null) { Stop(); return; }
            var keyboard=Keyboard.current;
            if(held==null && !(Mouse.current?.leftButton.isPressed ?? false) && (keyboard==null || (!keyboard.qKey.isPressed && !keyboard.eKey.isPressed && !keyboard.rKey.isPressed && !keyboard.sKey.isPressed && !keyboard.kKey.isPressed && !keyboard.fKey.isPressed && !keyboard.tKey.isPressed && !keyboard.xKey.isPressed && (!ShakerStrainAvailable || !keyboard.uKey.isPressed)))) session.ReleaseHeldInput();
            if(held==null || seconds<=0) return;
            long micros=(long)Math.Round(Math.Min(seconds,.25)*1000000); // Hitches pause excess time rather than delivering an unseen surge.
            bool fine=Fine || (Keyboard.current?.leftShiftKey.isPressed ?? false) || (Keyboard.current?.rightShiftKey.isPressed ?? false);
            if(held=="stir")
            {
                var p=StirPoint(); double before=session.Brew.StirAcceptedDegrees;
                session.ExecuteBrew(++serial,b=>b.MoveStir(p.x,p.y,micros)); AcceptedStirThisStep=session.Brew.StirAcceptedDegrees>before;
                if(AcceptedStirThisStep) SpoonAngle=Mathf.Atan2(p.y,p.x)*Mathf.Rad2Deg;
            }
            else if(held=="shake")
            {
                int before=session.Brew.ShakeReversals; session.ExecuteBrew(++serial,b=>b.MoveShake(ReadShake(),micros)); AcceptedStirThisStep=session.Brew.ShakeReversals>before;
            }
            else if(held=="float")
            {
                ReadFloat(); int before=session.Brew.Revision;
                session.ExecuteBrew(++serial,b=>b.UpdateFloat(micros,(decimal)FloatIntensity,FloatHeight)); AcceptedLiquidThisStep=session.Brew.Revision!=before;
            }
            else
            {
                int before=session.Brew.Revision; session.UpdateBrew(micros,fine);
                AcceptedLiquidThisStep=session.Brew.Revision!=before;
            }
        }
        private void OnDisable() { Stop(); foreach(var a in actions) a.Disable(); }
        private void OnEnable() { foreach(var a in actions) a.Enable(); }
        private void OnDestroy() { Stop(); foreach(var a in actions) a.Dispose(); actions.Clear(); }
    }
}
