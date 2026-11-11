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
    // UI-only state fixtures; authored reading and actual services have separate proofs.
    public sealed class ActSixUiTests
    {
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private sealed class Store : ICampaignSaveStore { public Task SaveAsync(CampaignCheckpoint state)=>Task.CompletedTask; }
        private CampaignController owner; private CampaignContent content; private CampaignUi ui; private GameObject events; private TMP_FontAsset font;
        [SetUp] public void Setup()
        {
            ui=null; EditorSceneManager.OpenScene(Surlhwa.Editor.ActFiveExpansionBuilder.ScenePath);
            owner=Object.FindFirstObjectByType<CampaignController>(); content=CampaignContentLoader.Load(File.ReadAllText("../Content/Campaign/act6.json"));
            font=TMP_FontAsset.CreateFontAsset(owner.Font.sourceFontFile,64,8,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true); owner.Font=font;
            Set("Content",content); Set("Input",owner.gameObject.AddComponent<SliceInputAdapter>());
            typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule).GetField("defaultActions",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
            events=new GameObject("ACT6 UI fixture events",typeof(EventSystem)); typeof(EventSystem).GetMethod("OnEnable",Hidden).Invoke(events.GetComponent<EventSystem>(),null);
            At(39); ui=owner.gameObject.AddComponent<CampaignUi>(); ui.Initialize(owner,font); ui.Refresh();
        }
        [TearDown] public void Teardown()
        {
            if(events!=null) { typeof(EventSystem).GetMethod("OnDisable",Hidden).Invoke(events.GetComponent<EventSystem>(),null); Object.DestroyImmediate(events); }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); if(font!=null) Object.DestroyImmediate(font);
        }
        private void Set(string property,object value)=>typeof(CampaignController).GetProperty(property).SetValue(owner,value);
        private void At(int slot)
        {
            owner.Session?.Dispose(); var state=new CampaignCheckpoint(1,"123456781234123412341234567890ab",slot,CampaignPhase.Brewing,0,null,null,
                content.Slots.Take(slot+1).Select(x=>x.EncounterId),Array.Empty<string>(),Array.Empty<string>(),Array.Empty<CommittedService>(),Array.Empty<MemoryReceipt>(),
                content.KnownSeeds.ToDictionary(x=>x.Key,x=>x.Value),new Dictionary<string,string[]>());
            Set("Session",CampaignSession.Continue(content,new Store(),state)); owner.Input.Bind(owner.Session,owner.Camera,Vector3.zero); owner.Input.ShakerStrainAvailable=content.ActAt(slot)>=4; ui?.Refresh();
        }
        private Dictionary<string,Button> Shelf=>(Dictionary<string,Button>)typeof(CampaignUi).GetField("shelf",Hidden).GetValue(ui);
        private Button Button(string name)=>ui.GetComponentsInChildren<Button>().Single(x=>x.name==name);
        private void RenderCodex(int page)
        {
            // This EditMode fixture checks projections/layout. Actual page navigation
            // and deferred modal destruction are exercised by the built Player proof.
            var old=(GameObject)typeof(CampaignUi).GetField("modal",Hidden).GetValue(ui);
            if(old!=null) Object.DestroyImmediate(old);
            typeof(CampaignUi).GetField("modal",Hidden).SetValue(ui,null);
            typeof(CampaignUi).GetField("shownModal",Hidden).SetValue(ui,null);
            typeof(CampaignUi).GetField("recipePage",Hidden).SetValue(ui,page);
            ui.MenuModal="codex"; ui.Refresh();
        }
        [TestCase(1280,720)] [TestCase(1920,1080)] public void NewPhysicalSupplyAppearsBeforeLessonWithoutLearningAndEveryIngredientIsReachable(int width,int height)
        {
            owner.Camera.aspect=width/(float)height; At(38);
            foreach(string id in new[]{"green_chartreuse","maraschino_luxardo","fresh_lime_juice"}) Assert.That(owner.Session.Supply.Contains(id),Is.False);
            At(39); Assert.That(owner.Session.Supply.Count,Is.EqualTo(22)); Assert.That(owner.Session.Knowledge.Project("recipe_last_word").Name,Is.Null);
            var seen=new HashSet<string>();
            for(int page=0;page<2;page++) {
                foreach(var item in Shelf.Where(x=>x.Value.gameObject.activeInHierarchy)) {
                    var rect=(RectTransform)item.Value.transform; Assert.That(rect.anchoredPosition.y,Is.GreaterThanOrEqualTo(66));
                    Assert.That(rect.anchoredPosition.y+rect.rect.height,Is.LessThanOrEqualTo(426)); item.Value.onClick.Invoke(); Assert.That(owner.Input.SelectedIngredient,Is.EqualTo(item.Key)); seen.Add(item.Key);
                }
                if(page==0) { Button("다음 재료").onClick.Invoke(); ui.Refresh(); }
            }
            Assert.That(seen,Is.EquivalentTo(owner.Session.Supply)); Assert.That(owner.Session.Knowledge.Project("recipe_last_word").Known,Is.False);
            var ingredients=(GameObject)typeof(CampaignUi).GetField("ingredients",Hidden).GetValue(ui);
            Assert.That(ingredients.GetComponentsInChildren<TextMeshProUGUI>().Any(x=>x.text.Contains("라스트 워드")),Is.False);
        }
        [Test] public void LastWordPartialPreparationDescribesOnlyItsActualStrainFinishAndAllNinePagesClearTheFooter()
        {
            At(40); int index=Array.IndexOf(content.Catalog.Recipes.Keys.ToArray(),"recipe_last_word"); RenderCodex(index);
            Assert.That(Button("Recipe page "+index).GetComponentInChildren<TextMeshProUGUI>().text,Is.EqualTo("아직 빈 페이지"));
            foreach(string fact in new[]{"identity","method","preparation","garnish"}.Concat(content.Catalog.Recipes["recipe_last_word"].Amounts.Keys.Select(x=>"amount:"+x)))
                owner.Session.Knowledge.GrantFact("recipe_last_word",fact);
            RenderCodex(index);
            var texts=ui.GetComponentsInChildren<TextMeshProUGUI>().Select(x=>x.text).ToArray();
            var preparation=texts.Single(x=>x.Contains("셰이커") && x.Contains("흔들기"));
            Assert.That(preparation,Does.Contain("U로 거르기")); Assert.That(string.Join("\n",texts),Does.Contain("가니시 없음"));
            foreach(string wrong in new[]{"소다","샴페인","Splash","가볍게 젓기","G로"}) Assert.That(preparation,Does.Not.Contain(wrong));
            Assert.That(owner.Session.Knowledge.Project("recipe_last_word").Known,Is.False,"Visible authored facts are not a discovery receipt.");
            var footer=ui.GetComponentsInChildren<TextMeshProUGUI>().Single(x=>x.text=="발견은 이름을 남깁니다. 단서는 그 전의 길을 비춥니다.");
            float limit=footer.rectTransform.anchoredPosition.y+footer.rectTransform.rect.height+12;
            var pages=ui.GetComponentsInChildren<Button>().Where(x=>x.name.StartsWith("Recipe page ")).ToArray(); Assert.That(pages.Length,Is.EqualTo(9));
            foreach(var page in pages) Assert.That(((RectTransform)page.transform).anchoredPosition.y,Is.GreaterThanOrEqualTo(limit),page.name);
        }
        [TestCase("recipe_french_75","플루트 잔",29)]
        [TestCase("recipe_last_word","칵테일 잔",40)]
        public void GlasswareMetadataWaitsForKnownWhileAuthoredPreparationRemainsAvailable(string recipeId,string glass,int slot)
        {
            At(slot); int page=Array.IndexOf(content.Catalog.Recipes.Keys.ToArray(),recipeId); RenderCodex(page);
            string Body()=>string.Join("\n",ui.GetComponentsInChildren<TextMeshProUGUI>().Select(x=>x.text));
            Assert.That(Body(),Does.Not.Contain(glass),"Unknown does not expose glassware.");
            foreach(string fact in new[]{"identity","method","preparation","garnish"}.Concat(content.Catalog.Recipes[recipeId].Amounts.Keys.Select(x=>"amount:"+x)))
                owner.Session.Knowledge.GrantFact(recipeId,fact);
            RenderCodex(page); Assert.That(owner.Session.Knowledge.Project(recipeId).Known,Is.False);
            Assert.That(Body(),Does.Contain("U로 거르기"),"The authored preparation remains available before discovery.");
            Assert.That(Body(),Does.Not.Contain(glass),"Canon KNOW-002/006: glassware is not a taught Partial fact.");
            owner.Session.Knowledge.SeedKnown(recipeId,"ui_projection_fixture"); RenderCodex(page);
            Assert.That(Body(),Does.Contain(glass),"Known exposes the supported full recipe metadata.");
        }
    }
}
