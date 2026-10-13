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
        private readonly List<InputAction> actions=new List<InputAction>(); private SliceSession session; private Camera view;
        private Vector3 mixing; private long serial=10000; private string held; private Vector2 previousPointer;
        public string SelectedIngredient { get; private set; }="whiskey";
        public ContainerRole Target { get; private set; }=ContainerRole.Jigger;
        public string ActiveTool => held; public bool Fine { get; set; }
        public bool AcceptedLiquidThisStep { get; private set; } public bool AcceptedStirThisStep { get; private set; }
        public float SpoonAngle { get; private set; } public string Feedback { get; private set; }
        public Action<string> UiCommand; public event Action<string> AcceptedCue;
        public long NextEventId() => ++serial;
        public void Bind(SliceSession value,Camera camera,Vector3 mixingCenter)
        {
            Stop(); session=value; view=camera; mixing=mixingCenter; Target=ContainerRole.Jigger; Fine=false; SelectedIngredient=value.Content.Profile.Supply.First();
            Configure(value.Content.Profile.Supply);
        }
        public void Configure(IEnumerable<string> supply)
        {
            if(actions.Count>0) return;
            ActionKey("pour","q",true); ActionKey("transfer","e",true); ActionKey("strain","r",true); ActionKey("stir","s",true);
            ActionKey("ice","i",false); ActionKey("cherry","c",false); ActionKey("dash","d",false);
            ActionKey("target","j",false); ActionKey("glass","v",false); ActionKey("guide","b",false);
            ActionKey("codex","tab",false); ActionKey("back","escape",false); ActionKey("advance","enter",false);
            string[] ingredients=supply.ToArray();
            for(int n=0;n<ingredients.Length;n++) { string id=ingredients[n]; var a=new InputAction("ingredient"+n,InputActionType.Button,"<Keyboard>/"+(n+1)); a.performed+=_=>SelectIngredient(id); a.Enable(); actions.Add(a); }
        }
        private void ActionKey(string command,string key,bool continuous)
        {
            var a=new InputAction(command,InputActionType.Button,"<Keyboard>/"+key);
            a.performed+=_=> { if(continuous) Begin(command); else Command(command); };
            if(continuous) a.canceled+=_=>Release(); a.Enable(); actions.Add(a);
        }
        public void SelectIngredient(string id)
        { if(session==null || !session.CanInteract || !session.Content.Profile.Supply.Contains(id)) return; Stop(); SelectedIngredient=id; Feedback=null; }
        public void SelectTarget(ContainerRole role) { if(session==null || !session.CanInteract) return; Stop(); Target=role; Feedback=null; }
        public void Command(string command)
        {
            if(command=="guide" || command=="codex" || command=="back" || command=="advance" || command=="glass") { UiCommand?.Invoke(command); return; }
            if(session==null) return;
            if(command=="target") { SelectTarget((ContainerRole)(((int)Target+1)%3)); return; }
            if(!session.CanInteract) return;
            try
            {
                Stop(); bool accepted=session.ExecuteBrew(++serial,b=> {
                    if(command=="ice") b.AddIce(Target);
                    else if(command=="cherry") b.Garnish("cherry","in_glass");
                    else if(command=="dash") b.Pour(SelectedIngredient,Target,session.Content.Tuning.DashMl,BrewAction.Dash);
                });
                if(accepted) { AcceptedCue?.Invoke(command); Feedback=null; }
            }
            catch(InvalidOperationException) { Feedback=command=="cherry" ? "서빙 잔이 비어 있어요" : "도구와 재료를 확인해요"; }
            catch(ArgumentException) { Feedback="도구와 재료를 확인해요"; }
        }
        public void Begin(string tool)
        {
            if(session==null || held!=null || !session.CanInteract) return;
            try
            {
                bool accepted=session.ExecuteBrew(++serial,b=> {
                    if(tool=="pour") b.BeginPour(SelectedIngredient,Target);
                    else if(tool=="transfer") b.BeginTransfer(ContainerRole.Jigger,ContainerRole.Mixing,BrewAction.PourFromContainer);
                    else if(tool=="strain") b.BeginTransfer(ContainerRole.Mixing,ContainerRole.Serving,BrewAction.Strain);
                    else if(tool=="stir") { var p=StirPoint(); b.BeginStir(p.x,p.y); }
                });
                if(accepted && (session.Brew.FlowActive || session.Brew.StirActive)) { held=tool; previousPointer=Mouse.current?.position.ReadValue() ?? Vector2.zero; Feedback=null; }
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
        public void Step(double seconds)
        {
            AcceptedLiquidThisStep=false; AcceptedStirThisStep=false;
            if(session==null || !session.CanInteract || session.Brew==null) { Stop(); return; }
            var keyboard=Keyboard.current;
            if(held==null && !(Mouse.current?.leftButton.isPressed ?? false) && (keyboard==null || (!keyboard.qKey.isPressed && !keyboard.eKey.isPressed && !keyboard.rKey.isPressed && !keyboard.sKey.isPressed))) session.ReleaseHeldInput();
            if(held==null || seconds<=0) return;
            long micros=(long)Math.Round(Math.Min(seconds,.25)*1000000); // Hitches pause excess time rather than delivering an unseen surge.
            bool fine=Fine || (Keyboard.current?.leftShiftKey.isPressed ?? false) || (Keyboard.current?.rightShiftKey.isPressed ?? false);
            if(held=="stir")
            {
                var p=StirPoint(); double before=session.Brew.StirAcceptedDegrees;
                session.ExecuteBrew(++serial,b=>b.MoveStir(p.x,p.y,micros)); AcceptedStirThisStep=session.Brew.StirAcceptedDegrees>before;
                if(AcceptedStirThisStep) SpoonAngle=Mathf.Atan2(p.y,p.x)*Mathf.Rad2Deg;
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
