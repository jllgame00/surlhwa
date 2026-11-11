using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Domain;
using Surlhwa.Presentation;
using Object=UnityEngine.Object;

namespace Surlhwa.SpatialTests
{
    // Projection fixtures only; published history and reading receipts have separate runtime proofs.
    public sealed class ActSevenUiTests
    {
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private sealed class Store : ICampaignSaveStore { public Task SaveAsync(CampaignCheckpoint state)=>Task.CompletedTask; }
        private CampaignController owner; private CampaignContent content; private CampaignUi ui; private GameObject events; private TMP_FontAsset font;
        [SetUp] public void Setup()
        {
            EditorSceneManager.OpenScene(Surlhwa.Editor.ActSixExpansionBuilder.ScenePath);
            owner=Object.FindFirstObjectByType<CampaignController>(); content=CampaignContentLoader.Load(File.ReadAllText("../Content/Campaign/act7.json"));
            font=TMP_FontAsset.CreateFontAsset(owner.Font.sourceFontFile,64,8,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true); owner.Font=font;
            Set("Content",content); Set("Input",owner.gameObject.AddComponent<SliceInputAdapter>());
            typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule).GetField("defaultActions",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
            events=new GameObject("ACT7 UI fixture events",typeof(EventSystem)); typeof(EventSystem).GetMethod("OnEnable",Hidden).Invoke(events.GetComponent<EventSystem>(),null);
            At(CampaignPhase.EndingReady,0,"keep"); ui=owner.gameObject.AddComponent<CampaignUi>(); ui.Initialize(owner,font); ui.Refresh();
        }
        [TearDown] public void Teardown()
        {
            if(events!=null) { typeof(EventSystem).GetMethod("OnDisable",Hidden).Invoke(events.GetComponent<EventSystem>(),null); Object.DestroyImmediate(events); }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); if(font!=null) Object.DestroyImmediate(font); ui=null;
        }
        private void Set(string property,object value)=>typeof(CampaignController).GetProperty(property).SetValue(owner,value);
        private void At(CampaignPhase phase,int line,string choice)
        {
            owner.Session?.Dispose(); var state=new CampaignCheckpoint(1,"123456781234123412341234567890ab",46,phase,line,choice,PrinceFarewell.Quiet,
                content.Slots.Select(x=>x.EncounterId),Array.Empty<string>(),Array.Empty<string>(),Array.Empty<CommittedService>(),Array.Empty<MemoryReceipt>(),
                content.KnownSeeds.ToDictionary(x=>x.Key,x=>x.Value),new Dictionary<string,string[]>());
            Set("Session",CampaignSession.Continue(content,new Store(),state)); owner.Input.Bind(owner.Session,owner.Camera,Vector3.zero); ui?.Refresh();
        }
        private string[] Texts()=>ui.GetComponentsInChildren<TextMeshProUGUI>().Where(x=>x.isActiveAndEnabled).Select(x=>x.text).ToArray();
        [Test] public void ReadyOffersAnExplicitClosingReadingWithoutTheLastGuestsRequestOrResult()
        {
            Assert.That(Texts(),Does.Contain("마지막 손님이 떠난 자리를 돌아봅니다."));
            Assert.That(ui.DisplayedSpeaker,Is.Empty);
            Assert.That(ui.GetComponentsInChildren<Button>().Any(x=>x.isActiveAndEnabled && x.interactable && x.GetComponentInChildren<TextMeshProUGUI>()?.text=="밤을 정리하기"),Is.True);
            Assert.That(Texts(),Does.Not.Contain(content.Texts[owner.Session.CurrentEncounter.SummaryTextId].Value));
        }
        [TestCase("keep")][TestCase("seek")][TestCase(null)]
        public void EveryEndingLineHasTheOwnedWordsAndOnlyTheRecallNamesTheRememberedPrince(string choice)
        {
            var lines=content.Ending.ResolveLines(choice);
            for(int i=0;i<lines.Count;i++) {
                At(i==0 ? CampaignPhase.EndingEntered : CampaignPhase.EndingReading,i,choice);
                Assert.That(Texts(),Does.Contain(content.Texts[lines[i]].Value));
                Assert.That(ui.DisplayedSpeaker,Is.EqualTo(i==2 ? "기억 속 어린왕자" : ""));
                Assert.That(Texts().Any(x=>x.Contains("20번째 밤")),Is.False,"The ending is the bar's time, not the departed guest's active night.");
            }
        }
        [Test] public void CompletedContinueShowsTheCommonCodaWithNoAdvanceOrNewGuest()
        {
            At(CampaignPhase.Complete,0,"seek");
            Assert.That(Texts(),Does.Contain(content.Texts[content.Ending.CodaTextId].Value));
            Assert.That(ui.DisplayedSpeaker,Is.Empty);
            Assert.That(ui.GetComponentsInChildren<Button>().Any(x=>x.isActiveAndEnabled && x.GetComponentInChildren<TextMeshProUGUI>()?.text=="계속  Enter"),Is.False);
        }
    }
}
