using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Domain;
using Surlhwa.Presentation;
using Object=UnityEngine.Object;

namespace Surlhwa.SpatialTests
{
    public sealed class P11ToolMotionTests
    {
        private sealed class Store:ICampaignSaveStore { public Task SaveAsync(CampaignCheckpoint s)=>Task.CompletedTask; }
        private InputTestFixture inputFixture; private GameObject go; private CampaignSession session; private SliceInputAdapter input; private SliceWorkbenchView view;
        [SetUp] public void Setup()
        {
            inputFixture=new InputTestFixture(); inputFixture.Setup(); InputSystem.AddDevice<Mouse>(); InputSystem.AddDevice<Keyboard>();
            var c=CampaignContentLoader.Load(File.ReadAllText("../Content/Campaign/act7.json"));
            // Presentation-only checkpoint: real brew and adapter, no published save evidence.
            session=CampaignSession.Continue(c,new Store(),new CampaignCheckpoint(1,"123456781234123412341234567890ab",0,CampaignPhase.Brewing,0,null,null,
                new[]{c.Slots[0].EncounterId},Array.Empty<string>(),Array.Empty<string>(),Array.Empty<CommittedService>(),Array.Empty<MemoryReceipt>(),c.KnownSeeds.ToDictionary(x=>x.Key,x=>x.Value),new Dictionary<string,string[]>()));
            go=new GameObject("P11 tool contact"); input=go.AddComponent<SliceInputAdapter>(); input.Bind(session,null,Vector3.zero);
            view=go.AddComponent<SliceWorkbenchView>(); view.Initialize(null); view.ExtendedTools=true;
            var art=AssetDatabase.LoadAssetAtPath<CampaignArtSet>("Assets/Surlhwa/FinalArt/FinalArt.asset"); Assert.That(art,Is.Not.Null); view.ApplyArt(art);
            input.SelectIngredient("gin"); input.SelectTarget(ContainerRole.Serving);
        }
        [TearDown] public void Teardown() { Object.DestroyImmediate(go); session?.Dispose(); inputFixture.TearDown(); }
        private Transform Find(string name)=>go.GetComponentsInChildren<Transform>(true).Single(x=>x.name==name);
        [Test] public void ReleaseStopsLiquidImmediatelyWhileTheEmptyGripReturnsWithoutChangingTheBatch()
        {
            input.Begin("pour"); input.Step(.1); view.Refresh(session.Brew,input,.1f);
            Assert.That(Find("Accepted liquid stream").GetComponent<LineRenderer>().enabled,Is.True);
            input.Release(); decimal amount=session.Brew.Container(ContainerRole.Serving).TotalMl; int revision=session.Brew.Revision;
            view.Refresh(session.Brew,input,.03f);
            Assert.That(Find("Accepted liquid stream").GetComponent<LineRenderer>().enabled,Is.False);
            Assert.That(Quaternion.Angle(Find("Selected bottle").localRotation,Quaternion.identity),Is.GreaterThan(1));
            Assert.That(Find("2D grip").gameObject.activeSelf,Is.True);
            view.Refresh(session.Brew,input,.25f);
            Assert.That(Quaternion.Angle(Find("Selected bottle").localRotation,Quaternion.identity),Is.LessThan(.001f));
            Assert.That(Find("2D grip").gameObject.activeSelf,Is.False);
            Assert.That(session.Brew.Revision,Is.EqualTo(revision)); Assert.That(session.Brew.Container(ContainerRole.Serving).TotalMl,Is.EqualTo(amount));
        }
        [Test] public void ModalCancellationNeverLeavesAReturningHandOrStream()
        {
            input.Begin("pour"); input.Step(.1); view.Refresh(session.Brew,input,.1f); session.OpenModal("codex"); input.Step(.1); view.Refresh(session.Brew,input,.01f);
            Assert.That(Find("2D grip").gameObject.activeSelf,Is.False); Assert.That(Find("Accepted liquid stream").GetComponent<LineRenderer>().enabled,Is.False);
        }
        [Test] public void TheAcceptedStreamTargetsTheFluteBowlRatherThanItsStem()
        {
            view.CyclePresentationGlass(); view.CyclePresentationGlass(); view.CyclePresentationGlass();
            input.Begin("pour"); input.Step(.1); view.Refresh(session.Brew,input,.1f);
            var line=Find("Accepted liquid stream").GetComponent<LineRenderer>(); Assert.That(line.enabled,Is.True);
            var serving=Find("Serving glass 180 ml"); var local=serving.InverseTransformPoint(go.transform.TransformPoint(line.GetPosition(1)));
            Assert.That(local.y,Is.GreaterThan(view.ServingFillBottom)); Assert.That(local.y,Is.LessThan(.73f));
        }
        [TestCase("modal")][TestCase("focus")][TestCase("rebind")][TestCase("disable")]
        public void CancellationAlsoClearsAnAlreadyReturningHand(string reason)
        {
            input.Begin("pour"); input.Step(.1); view.Refresh(session.Brew,input,.1f); input.Release(); view.Refresh(session.Brew,input,.02f);
            Assert.That(Find("2D grip").gameObject.activeSelf,Is.True);
            if(reason=="modal") session.OpenModal("codex");
            else if(reason=="focus") session.SetFocus(false);
            else if(reason=="rebind") input.Bind(session,null,Vector3.zero);
            else { input.enabled=false; input.Stop(); }
            input.Step(.01); view.Refresh(session.Brew,input,.01f);
            Assert.That(Find("2D grip").gameObject.activeSelf,Is.False);
            Assert.That(Find("Accepted liquid stream").GetComponent<LineRenderer>().enabled,Is.False);
        }
        [Test] public void ARegripRespondsImmediatelyAndDoesNotReplayTheOldLiquidStep()
        {
            input.Begin("pour"); input.Step(.1); view.Refresh(session.Brew,input,.1f); input.Release(); view.Refresh(session.Brew,input,.03f);
            decimal before=session.Brew.Container(ContainerRole.Serving).TotalMl;
            input.Begin("pour"); view.Refresh(session.Brew,input,.001f);
            Assert.That(Quaternion.Angle(Find("Selected bottle").localRotation,Quaternion.Euler(0,0,-45)),Is.LessThan(.001f));
            Assert.That(Find("Accepted liquid stream").GetComponent<LineRenderer>().enabled,Is.False);
            Assert.That(session.Brew.Container(ContainerRole.Serving).TotalMl,Is.EqualTo(before));
            input.Step(.1); view.Refresh(session.Brew,input,.1f);
            Assert.That(Find("Accepted liquid stream").GetComponent<LineRenderer>().enabled,Is.True);
            Assert.That(session.Brew.Container(ContainerRole.Serving).TotalMl,Is.GreaterThan(before));
        }
    }
}
