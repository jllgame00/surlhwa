using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Domain;
using Surlhwa.Presentation;
using Object=UnityEngine.Object;

namespace Surlhwa.SpatialTests
{
    public sealed class SliceHoldButtonTests
    {
        [UnityTearDown] public IEnumerator LeavePlayModeAfterAnyResult()
        { if(UnityEngine.Application.isPlaying) yield return new ExitPlayMode(); }
        private sealed class Store : ICampaignSaveStore { public Task SaveAsync(CampaignCheckpoint state)=>Task.CompletedTask; }
        private sealed class HoldFixture : IDisposable
        {
            internal readonly GameObject Root,Control;
            internal readonly Button Button;
            internal readonly SliceHoldButton Hold;
            internal readonly SliceInputAdapter Input;
            internal readonly CampaignSession Session;
            internal int Starts;
            internal HoldFixture()
            {
                var content=CampaignContentLoader.Load(File.ReadAllText("../Content/Campaign/act1.json"));
                Session=CampaignSession.StartAsync(content,new Store()).GetAwaiter().GetResult(); long n=1;
                while(Session.State.Phase!=CampaignPhase.Brewing)
                    if(Session.State.Phase==CampaignPhase.Arriving) Session.TickAsync(2).GetAwaiter().GetResult(); else Session.AdvanceAsync(n++).GetAwaiter().GetResult();
                Root=new GameObject("Shared hold fixture"); Input=Root.AddComponent<SliceInputAdapter>(); Input.Bind(Session,null,Vector3.zero);
                Control=new GameObject("Pour",typeof(RectTransform),typeof(Image),typeof(Button)); Control.transform.SetParent(Root.transform,false);
                Button=Control.GetComponent<Button>(); Hold=Control.AddComponent<SliceHoldButton>();
                Hold.Down=()=>{Starts++; Input.Begin("pour");}; Hold.Up=()=>Input.Release();
            }
            internal decimal Amount=>Session.Brew.Container(ContainerRole.Jigger).TotalMl;
            internal void Down(PointerEventData.InputButton button=PointerEventData.InputButton.Left)=>ExecuteEvents.Execute(Control,new PointerEventData(EventSystem.current){button=button},ExecuteEvents.pointerDownHandler);
            internal void Up()=>ExecuteEvents.Execute(Control,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerUpHandler);
            public void Dispose() { Object.DestroyImmediate(Root); Session.Dispose(); }
        }
        [TestCase("button_disabled")][TestCase("not_interactable")][TestCase("control_inactive")][TestCase("parent_inactive")][TestCase("handler_disabled")]
        public void UnavailableButtonCannotStartASeparatePointerHoldHandler(string unavailable)
        {
            using(var f=new HoldFixture()) {
                if(unavailable=="button_disabled") f.Button.enabled=false;
                if(unavailable=="not_interactable") f.Button.interactable=false;
                if(unavailable=="control_inactive") f.Control.SetActive(false);
                if(unavailable=="parent_inactive") f.Root.SetActive(false);
                if(unavailable=="handler_disabled") f.Hold.enabled=false;
                f.Down(); f.Input.Step(.1);
                Assert.That(f.Starts,Is.Zero,"uGUI dispatches each active handler independently of Button.");
                Assert.That(f.Input.ActiveTool,Is.Null); Assert.That(f.Amount,Is.Zero);
            }
        }
        [TestCase(PointerEventData.InputButton.Right)][TestCase(PointerEventData.InputButton.Middle)]
        public void OtherPointerButtonsDoNotBeginPouring(PointerEventData.InputButton button)
        {
            using(var f=new HoldFixture()) { f.Down(button); f.Input.Step(.1); Assert.That(f.Starts,Is.Zero); Assert.That(f.Amount,Is.Zero); }
        }
        [Test] public void EnabledPointerHoldUsesActualFlowAndUpStillReleasesAfterButtonDisable()
        {
            using(var f=new HoldFixture()) {
                f.Down(); f.Input.Step(.1); Assert.That(f.Amount,Is.GreaterThan(0)); decimal amount=f.Amount;
                f.Button.enabled=false; f.Up(); f.Input.Step(.1);
                Assert.That(f.Input.ActiveTool,Is.Null); Assert.That(f.Amount,Is.EqualTo(amount));
                f.Button.enabled=true; f.Input.Step(.1); Assert.That(f.Amount,Is.EqualTo(amount),"Availability alone is not another press.");
                f.Down(); f.Input.Step(.1); Assert.That(f.Amount,Is.GreaterThan(amount)); f.Up();
            }
        }
        [UnityTest] public IEnumerator LosingButtonAvailabilityDuringAPressCancelsWithoutWaitingForPointerUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            yield return new EnterPlayMode();
            foreach(string unavailable in new[]{"button_disabled","not_interactable","control_inactive","handler_disabled"}) {
                using(var f=new HoldFixture()) {
                    f.Down(); f.Input.Step(.1); Assert.That(f.Amount,Is.GreaterThan(0)); decimal amount=f.Amount;
                    if(unavailable=="button_disabled") f.Button.enabled=false;
                    if(unavailable=="not_interactable") f.Button.interactable=false;
                    if(unavailable=="control_inactive") f.Control.SetActive(false);
                    if(unavailable=="handler_disabled") f.Hold.enabled=false;
                    yield return null; yield return null; f.Input.Step(.1);
                    Assert.That(f.Input.ActiveTool,Is.Null,unavailable); Assert.That(f.Amount,Is.EqualTo(amount),unavailable);
                    f.Button.enabled=true; f.Button.interactable=true; f.Control.SetActive(true); f.Hold.enabled=true;
                    yield return null; f.Input.Step(.1); Assert.That(f.Amount,Is.EqualTo(amount),"Re-enable must not resume the old press.");
                    f.Up(); f.Down(); f.Input.Step(.1); Assert.That(f.Amount,Is.GreaterThan(amount)); f.Up();
                }
            }
            yield return new ExitPlayMode();
        }
    }
}
