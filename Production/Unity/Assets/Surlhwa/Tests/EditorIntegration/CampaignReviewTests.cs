using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Domain;
using Surlhwa.Presentation;
namespace Surlhwa.SpatialTests
{
    public sealed class CampaignReviewTests
    {
        private CampaignController c; private CampaignUi ui; private CampaignContent content; private CampaignSession s; private GameObject events;
        private sealed class Store:ICampaignSaveStore { public Task SaveAsync(CampaignCheckpoint state)=>Task.CompletedTask; }
        private static object Field(object o,string name)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
        private static void Set(object o,string name,object value)=>o.GetType().GetProperty(name).SetValue(o,value);
        private static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
        [SetUp] public void Setup()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Surlhwa/Scenes/Bar_Act1.unity"); c=UnityEngine.Object.FindFirstObjectByType<CampaignController>();
            // EditMode scene reloads do not run the package's SubsystemRegistration reset.
            typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule).GetField("defaultActions",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
            content=CampaignContentLoader.Load(File.ReadAllText("../Content/Campaign/act1.json")); Set(c,"Content",content);
            Set(c,"Input",c.gameObject.AddComponent<SliceInputAdapter>()); c.Input.Configure(content.Supply);
            events=new GameObject("Review fixture events",typeof(EventSystem)); Call(events.GetComponent<EventSystem>(),"OnEnable");
            ui=c.gameObject.AddComponent<CampaignUi>(); Set(c,"Ui",ui); ui.Initialize(c,c.Font);
        }
        [TearDown] public void Cleanup()
        { s?.Dispose(); Call(events.GetComponent<EventSystem>(),"OnDisable"); UnityEngine.Object.DestroyImmediate(events); UnityEngine.Object.DestroyImmediate(c.gameObject); }
        private void StartSession() { s=CampaignSession.StartAsync(content,new Store()).GetAwaiter().GetResult(); Set(c,"Session",s); }
        private void Advance()=>s.AdvanceAsync(c.Input.NextEventId()).GetAwaiter().GetResult();
        private void Reach(int slot,CampaignPhase phase)
        {
            for(int n=0;n<500 && !(s.State.SlotIndex==slot && s.State.Phase==phase);n++) {
                if(s.State.Phase==CampaignPhase.Arriving || s.State.Phase==CampaignPhase.Departing) s.TickAsync(2).GetAwaiter().GetResult();
                else if(s.State.Phase==CampaignPhase.Choice) s.ChooseAsync("keep",c.Input.NextEventId()).GetAwaiter().GetResult();
                else { if(s.State.Phase==CampaignPhase.Brewing) { s.ReleaseHeldInput(); s.ExecuteBrew(c.Input.NextEventId(),b=>b.Pour("water",ContainerRole.Serving,1)); } Advance(); }
            }
            Assert.That(s.State.SlotIndex,Is.EqualTo(slot)); Assert.That(s.State.Phase,Is.EqualTo(phase));
        }
        [Test] public void RecognizedWrongLessonDrinkDoesNotRevealUnknownIdentityOnResultRetryOrContinue()
        {
            StartSession(); Reach(6,CampaignPhase.Brewing); s.ReleaseHeldInput(); s.ExecuteBrew(c.Input.NextEventId(),b=> {
                b.Pour("gin",ContainerRole.Mixing,60); b.Pour("dry_vermouth",ContainerRole.Mixing,10); b.AddIce(ContainerRole.Mixing); b.BeginStir(1,0);
                for(int n=1;n<=72;n++) { double a=n*Math.PI/12; b.MoveStir(Math.Cos(a),Math.Sin(a),83333); } b.EndStir(); b.Transfer(ContainerRole.Mixing,ContainerRole.Serving,70,BrewAction.Strain);
            }); Advance(); Advance();
            Assert.That(s.State.CurrentService.Outcome.RecipeId,Is.EqualTo("recipe_dry_martini")); Assert.That(s.Knowledge.Project("recipe_dry_martini").Name,Is.Null);
            foreach(bool continued in new[]{false,true}) {
                if(continued) { s=CampaignSession.Continue(content,new Store(),CampaignSaveCodec.Decode(CampaignSaveCodec.Encode(s.State,content),content)); Set(c,"Session",s); }
                Assert.That((string)Call(ui,"ResultText"),Does.Not.Contain("Dry Martini"));
            }
            Advance(); Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.Retry)); Assert.That((string)Call(ui,"ResultText"),Does.Not.Contain("Dry Martini"));
            // An explicitly disclosed Partial identity remains legal even without a Known receipt.
            s.Knowledge.GrantFact("recipe_dry_martini","identity"); Assert.That((string)Call(ui,"ResultText"),Does.Contain("Dry Martini"));
        }
        [Test] public void GenericGuideDoesNotRevealTheUnlearnedLateIngredient()
        { ui.MenuModal="guide"; ui.Refresh(); Assert.That(string.Join("\n",ui.GetComponentsInChildren<TextMeshProUGUI>().Select(x=>x.text)),Does.Not.Contain("소다")); }
        [Test] public void KeyboardChoiceEntryRecoversToAnAvailableChoice()
        {
            StartSession(); Reach(5,CampaignPhase.Intro); ui.Refresh(); var next=(Button)Field(ui,"next"); EventSystem.current.SetSelectedGameObject(next.gameObject);
            while(s.State.Phase==CampaignPhase.Intro) Advance(); ui.Refresh();
            var selected=EventSystem.current.currentSelectedGameObject; var choices=(GameObject)Field(ui,"choicePanel");
            Assert.That(selected!=null && selected.transform.IsChildOf(choices.transform),Is.True);
            Assert.That(ui.ActivateFocused(),Is.True); Assert.That(s.State.Phase,Is.EqualTo(CampaignPhase.ChoiceResponse));
        }
        [TestCase("disabled")][TestCase("inactive")][TestCase("destroyed")]
        public void ModalReturnNeverSelectsUnavailableControls(string kind)
        {
            StartSession(); Reach(0,CampaignPhase.Brewing); ui.Refresh();
            var prior=ui.GetComponentsInChildren<Button>().First(x=>x.isActiveAndEnabled && x.IsInteractable()); EventSystem.current.SetSelectedGameObject(prior.gameObject); ui.RememberFocus();
            if(kind=="disabled") prior.enabled=false; else if(kind=="inactive") prior.gameObject.SetActive(false); else UnityEngine.Object.DestroyImmediate(prior.gameObject);
            ui.RestoreFocus(); ui.Refresh(); var selected=EventSystem.current.currentSelectedGameObject?.GetComponent<Button>();
            Assert.That(selected!=null && selected.isActiveAndEnabled && selected.IsInteractable(),Is.True);
        }
        [TestCase(2)][TestCase(3)] public void MethodGuidanceLeavesTheAuthoredToolAndCharacterAreaClear(int method)
        {
            StartSession(); Reach(0,CampaignPhase.Brewing); Call(ui,"SelectMethod",method); ui.Refresh(); Canvas.ForceUpdateCanvases();
            var root=(RectTransform)Field(ui,"root"); var protectedArea=new Rect(400,200,700,320);
            foreach(var text in ui.GetComponentsInChildren<TextMeshProUGUI>()) {
                if(string.IsNullOrWhiteSpace(text.text)) continue;
                var corners=new Vector3[4]; text.rectTransform.GetWorldCorners(corners);
                Vector2 a=(Vector2)root.InverseTransformPoint(corners[0])-root.rect.min,b=(Vector2)root.InverseTransformPoint(corners[2])-root.rect.min;
                Assert.That(protectedArea.Overlaps(new Rect(a,b-a)),Is.False,text.text);
            }
        }
    }
}
