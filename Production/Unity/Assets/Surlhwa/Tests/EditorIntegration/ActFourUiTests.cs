using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
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
    // Isolated UI layout/input fixture; actual earned checkpoint proof is separate.
    public sealed class ActFourUiTests
    {
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private sealed class Store : ICampaignSaveStore { public Task SaveAsync(CampaignCheckpoint state)=>Task.CompletedTask; }
        private CampaignController owner; private CampaignContent content; private CampaignUi ui; private GameObject events;
        private TMPro.TMP_FontAsset isolatedFont;
        [SetUp] public void Setup()
        {
            ui=null;
            EditorSceneManager.OpenScene(Surlhwa.Editor.ActThreeExpansionBuilder.ScenePath);
            owner=Object.FindFirstObjectByType<CampaignController>(); content=CampaignContentLoader.Load(File.ReadAllText("../Content/Campaign/act4.json"));
            isolatedFont=TMPro.TMP_FontAsset.CreateFontAsset(owner.Font.sourceFontFile,64,8,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,TMPro.AtlasPopulationMode.Dynamic,true); owner.Font=isolatedFont;
            Set("Content",content); Set("Input",owner.gameObject.AddComponent<SliceInputAdapter>());
            typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule).GetField("defaultActions",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
            events=new GameObject("ACT4 shelf fixture events",typeof(EventSystem));
            typeof(EventSystem).GetMethod("OnEnable",Hidden).Invoke(events.GetComponent<EventSystem>(),null);
            At(29); ui=owner.gameObject.AddComponent<CampaignUi>(); ui.Initialize(owner,owner.Font);
        }
        [TearDown] public void Teardown()
        {
            if(events!=null) { typeof(EventSystem).GetMethod("OnDisable",Hidden).Invoke(events.GetComponent<EventSystem>(),null); Object.DestroyImmediate(events); }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            if(isolatedFont!=null) Object.DestroyImmediate(isolatedFont);
        }
        private void Set(string property,object value)=>typeof(CampaignController).GetProperty(property).SetValue(owner,value);
        private void At(int slot)
        {
            owner.Session?.Dispose();
            var state=new CampaignCheckpoint(1,"123456781234123412341234567890ab",slot,CampaignPhase.Brewing,0,null,null,
                content.Slots.Take(slot+1).Select(x=>x.EncounterId),Array.Empty<string>(),Array.Empty<string>(),Array.Empty<CommittedService>(),Array.Empty<MemoryReceipt>(),
                content.KnownSeeds.ToDictionary(x=>x.Key,x=>x.Value),new Dictionary<string,string[]>());
            Set("Session",CampaignSession.Continue(content,new Store(),state)); owner.Input.Bind(owner.Session,owner.Camera,Vector3.zero);
            owner.Input.ShakerStrainAvailable=content.ActAt(slot)>=4; ui?.Refresh();
        }
        private Dictionary<string,Button> Shelf=>(Dictionary<string,Button>)typeof(CampaignUi).GetField("shelf",Hidden).GetValue(ui);
        private Button Find(string name)=>ui.GetComponentsInChildren<Button>(true).SingleOrDefault(x=>x.name==name);
        [TestCase(1280,720)] [TestCase(1920,1080)] public void EveryPhysicalIngredientIsSelectableWithoutClippingOrKnowledgeLeak(int width,int height)
        {
            owner.Camera.aspect=width/(float)height; ui.Refresh();
            Assert.That(owner.Session.Knowledge.Project("recipe_french_75").Name,Is.Null);
            var next=Find("다음 재료"); var previous=Find("이전 재료");
            Assert.That(next,Is.Not.Null,"The nineteenth ingredient must not overlap discard or escape the shelf."); Assert.That(previous,Is.Not.Null);
            var visited=new HashSet<string>();
            for(int page=0;page<2;page++) {
                var visible=Shelf.Where(x=>x.Value.gameObject.activeInHierarchy).ToArray();
                Assert.That(visible.Length,Is.EqualTo(page==0 ? 15 : 4));
                foreach(var item in visible) {
                    var rect=(RectTransform)item.Value.transform;
                    Assert.That(rect.anchoredPosition.y,Is.GreaterThanOrEqualTo(66));
                    Assert.That(rect.anchoredPosition.y+rect.rect.height,Is.LessThanOrEqualTo(426));
                    item.Value.onClick.Invoke(); Assert.That(owner.Input.SelectedIngredient,Is.EqualTo(item.Key)); visited.Add(item.Key);
                }
                if(page==0) { owner.Input.Begin("pour"); next.onClick.Invoke(); Assert.That(owner.Input.ActiveTool,Is.Null,"Changing shelf pages releases held tools."); ui.Refresh(); }
            }
            Assert.That(visited,Is.EquivalentTo(owner.Session.Supply)); Assert.That(next.interactable,Is.False);
            previous.onClick.Invoke(); ui.Refresh(); Assert.That(previous.interactable,Is.False);
            Assert.That(Shelf["fresh_lemon_juice"].gameObject.activeInHierarchy,Is.False);
            var ingredients=(GameObject)typeof(CampaignUi).GetField("ingredients",Hidden).GetValue(ui);
            Assert.That(ingredients.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Any(x=>x.text.Contains("프렌치 75")),Is.False,"Physical supply reveals ingredients, not unknown recipe names; authored guest requests have their own disclosure.");
        }
        [Test] public void EarlierShelfGeometryAndShakeControlsRemainUnchanged()
        {
            At(28); var available=Shelf.Where(x=>x.Value.gameObject.activeInHierarchy).ToArray(); Assert.That(available.Length,Is.EqualTo(17));
            for(int n=0;n<17;n++) Assert.That(((RectTransform)available[n].Value.transform).anchoredPosition,Is.EqualTo(new Vector2(10,402-n*22.5f)));
            Assert.That(Find("다음 재료"),Is.Not.Null); Assert.That(Find("다음 재료").gameObject.activeInHierarchy,Is.False);
            typeof(CampaignUi).GetMethod("SelectMethod",Hidden).Invoke(ui,new object[]{2});
            var strains=ui.GetComponentsInChildren<Button>(true).Where(x=>x.name=="거르기 U").ToArray(); Assert.That(strains.Length,Is.EqualTo(1));
            Assert.That(strains[0].gameObject.activeInHierarchy,Is.False);
            At(29); Assert.That(strains[0].gameObject.activeInHierarchy,Is.True);
            At(0); Assert.That(strains[0].gameObject.activeInHierarchy,Is.False);
            foreach(var pair in Shelf.Select((x,n)=>(x,n)).Where(x=>x.x.Value.gameObject.activeInHierarchy))
                Assert.That(((RectTransform)pair.x.Value.transform).anchoredPosition,Is.EqualTo(new Vector2(10,402-pair.n*25)));
        }
        [Test] public void EveryCodexPageClearsFooterWithoutRevealingAnUnknownRecipe()
        {
            ui.MenuModal="codex"; ui.Refresh();
            var footer=ui.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Single(x=>x.text=="발견은 이름을 남깁니다. 단서는 그 전의 길을 비춥니다.");
            float limit=footer.rectTransform.anchoredPosition.y+footer.rectTransform.rect.height+12;
            foreach(var page in ui.GetComponentsInChildren<Button>().Where(x=>x.name.StartsWith("Recipe page "))) {
                Assert.That(((RectTransform)page.transform).anchoredPosition.y,Is.GreaterThanOrEqualTo(limit),page.name+" must clear the footer.");
                if(page.name=="Recipe page 7") Assert.That(page.GetComponentInChildren<TMPro.TextMeshProUGUI>().text,Is.EqualTo("아직 빈 페이지"));
            }
        }
    }
}
