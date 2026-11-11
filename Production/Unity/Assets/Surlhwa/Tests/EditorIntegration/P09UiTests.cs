using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Domain;
using Surlhwa.Presentation;
using Object=UnityEngine.Object;

namespace Surlhwa.SpatialTests
{
    // Isolated presentation fixtures, never published or used as Player history.
    internal sealed class P09UiFixture : IDisposable
    {
        internal const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        internal sealed class Store : ICampaignSaveStore { public Task SaveAsync(CampaignCheckpoint state)=>Task.CompletedTask; }
        internal readonly GameObject Root;
        internal readonly CampaignController Owner;
        internal CampaignContent Content;
        internal readonly CampaignUi Ui;
        private readonly TMP_FontAsset font;
        private GameObject editEvents;
        private RenderTexture target;
        internal P09UiFixture(bool shippedFont=false)
        {
            Content=CampaignContentLoader.Load(File.ReadAllText("../Content/Campaign/act7.json"));
            Root=new GameObject("P09 isolated UI fixture");
            Owner=Root.AddComponent<CampaignController>(); Owner.enabled=false;
            var camera=new GameObject("P09 UI camera",typeof(Camera)); camera.transform.SetParent(Root.transform,false);
            Owner.Camera=camera.GetComponent<Camera>(); Owner.Camera.orthographic=true; Owner.Camera.orthographicSize=5;
            Owner.Camera.clearFlags=CameraClearFlags.SolidColor; Owner.Camera.backgroundColor=new Color(.03f,.03f,.03f);
            Owner.Camera.GetUniversalAdditionalCameraData();
            Owner.Camera.transform.position=new Vector3(0,0,-10);
            var source=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(shippedFont ? "Assets/Surlhwa/FinalArt/FinalKorean.asset" : "Assets/Surlhwa/Act7Assets/Act7Korean.asset");
            Assert.That(source,Is.Not.Null,"Use the adopted Korean font, not a test replacement.");
            if(shippedFont) {
                // Preserve the shipped asset's serialized metrics and settings, while
                // keeping dynamic glyph additions out of the source asset and atlas.
                font=Object.Instantiate(source); font.atlasTextures=source.atlasTextures.Select(x=>Object.Instantiate(x)).ToArray();
                typeof(TMP_FontAsset).GetField("m_AtlasTexture",Hidden).SetValue(font,font.atlasTextures[0]);
                font.material=Object.Instantiate(source.material); font.material.mainTexture=font.atlasTextures[0];
                font.ReadFontAssetDefinition();
                Assert.That(font.faceInfo,Is.EqualTo(source.faceInfo));
            } else font=TMP_FontAsset.CreateFontAsset(source.sourceFontFile,64,8,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
            Owner.Font=font;
            Set("Content",Content); Set("Input",Root.AddComponent<SliceInputAdapter>());
            typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule).GetField("defaultActions",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
            if(!UnityEngine.Application.isPlaying) {
                editEvents=new GameObject("P09 edit focus",typeof(EventSystem)); editEvents.transform.SetParent(Root.transform,false);
                typeof(EventSystem).GetMethod("OnEnable",Hidden).Invoke(editEvents.GetComponent<EventSystem>(),null);
            }
            At(40);
            Ui=Root.AddComponent<CampaignUi>(); Set("Ui",Ui); Ui.Initialize(Owner,font); Ui.Refresh();
        }
        internal void Set(string name,object value)=>typeof(CampaignController).GetProperty(name).SetValue(Owner,value);
        internal void At(int slot,CampaignPhase phase=CampaignPhase.Brewing,int line=0)
        {
            Owner.Session?.Dispose();
            var state=new CampaignCheckpoint(1,"123456781234123412341234567890ab",slot,phase,line,null,null,
                Content.Slots.Take(slot+1).Select(x=>x.EncounterId),Array.Empty<string>(),Array.Empty<string>(),Array.Empty<CommittedService>(),Array.Empty<MemoryReceipt>(),
                Content.KnownSeeds.ToDictionary(x=>x.Key,x=>x.Value),new Dictionary<string,string[]>());
            Set("Session",CampaignSession.Continue(Content,new Store(),state));
            Owner.Input.Bind(Owner.Session,Owner.Camera,Vector3.zero); Owner.Input.ShakerStrainAvailable=Content.ActAt(slot)>=4;
            Ui?.Refresh();
        }
        internal void Settings(bool large,bool contrast) { Set("LargeText",large); Set("HighContrast",contrast); Ui.Refresh(); }
        internal GameObject Modal=>(GameObject)typeof(CampaignUi).GetField("modal",Hidden).GetValue(Ui);
        internal int Page(string recipe)=>Array.IndexOf(Content.Catalog.Recipes.Keys.ToArray(),recipe);
        internal Button RecipeButton(string recipe)=>Ui.GetComponentsInChildren<Button>().Single(x=>x.name=="Recipe page "+Page(recipe));
        internal Button Button(string label)=>Ui.GetComponentsInChildren<Button>().Single(x=>x.GetComponentInChildren<TextMeshProUGUI>()?.text==label);
        internal void Show(string kind,string recipe=null)
        {
            // EditMode projection checks do not pretend deferred Destroy is a Player frame.
            if(Modal!=null) Object.DestroyImmediate(Modal);
            typeof(CampaignUi).GetField("modal",Hidden).SetValue(Ui,null);
            typeof(CampaignUi).GetField("shownModal",Hidden).SetValue(Ui,null);
            if(recipe!=null) typeof(CampaignUi).GetField("recipePage",Hidden).SetValue(Ui,Page(recipe));
            Ui.MenuModal=kind; Ui.Refresh(); Canvas.ForceUpdateCanvases();
        }
        internal TextMeshProUGUI[] ModalTexts()=>Modal.GetComponentsInChildren<TextMeshProUGUI>().Where(x=>x.isActiveAndEnabled).ToArray();
        internal string Words()=>string.Join("\n",ModalTexts().Select(x=>x.text));
        internal void Resolution(int width,int height)
        {
            if(target!=null) { Owner.Camera.targetTexture=null; target.Release(); Object.DestroyImmediate(target); }
            target=new RenderTexture(width,height,16); target.Create(); Owner.Camera.targetTexture=target; Owner.Camera.aspect=width/(float)height;
            Canvas.ForceUpdateCanvases(); Render();
        }
        internal void Render()
        {
            var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
            Assert.That(RenderPipeline.SupportsRenderRequest(Owner.Camera,request),Is.True,"Use the project's pinned URP render request.");
            RenderPipeline.SubmitRenderRequest(Owner.Camera,request);
        }
        internal void ChangeGlassware(string recipe,string glass)
        {
            var c=Content; var presentation=c.RecipePresentation.ToDictionary(x=>x.Key,x=>x.Value);
            presentation[recipe]=new RecipePresentation(glass,presentation[recipe].OptionalGarnish);
            var starts=Enumerable.Range(0,c.Slots.Count).Where(i=>i==0 || c.ActAt(i)!=c.ActAt(i-1)).ToArray();
            var supply=c.Supply.Select(id=>new {Id=id,Start=Enumerable.Range(0,c.Slots.Count).First(i=>c.AvailableSupplyAt(i).Contains(id))}).Where(x=>x.Start>0).ToDictionary(x=>x.Id,x=>x.Start);
            Content=new CampaignContent(c.Revision,c.BundleHash,c.Catalog,c.Tuning,c.IngredientNames.ToDictionary(x=>x.Key,x=>x.Value),c.Texts.ToDictionary(x=>x.Key,x=>x.Value),
                c.Guests.ToDictionary(x=>x.Key,x=>x.Value),presentation,c.Encounters.Values,c.Slots,c.Supply,c.KnownSeeds.ToDictionary(x=>x.Key,x=>x.Value),c.ChoicePrompt,
                c.PrinceChoices.ToDictionary(x=>x.Key,x=>x.Value),c.PrinceResponses.ToDictionary(x=>x.Key,x=>x.Value),c.PrinceRecalls.ToDictionary(x=>x.Key,x=>x.Value),
                c.OpenFarewell,c.QuietFarewell,c.Scope,supply,starts,c.Ending);
            Set("Content",Content); At(40);
        }
        public void Dispose()
        {
            Owner.Session?.Dispose();
            if(editEvents!=null) typeof(EventSystem).GetMethod("OnDisable",Hidden).Invoke(editEvents.GetComponent<EventSystem>(),null);
            if(target!=null) { Owner.Camera.targetTexture=null; target.Release(); Object.DestroyImmediate(target); }
            Object.DestroyImmediate(Root);
            foreach(var atlas in font.atlasTextures.Where(x=>x!=null && !AssetDatabase.Contains(x)).Distinct()) Object.DestroyImmediate(atlas);
            if(font.material!=null && !AssetDatabase.Contains(font.material)) Object.DestroyImmediate(font.material);
            Object.DestroyImmediate(font);
        }
    }

    public sealed class P09UiTests
    {
        private P09UiFixture f;
        [SetUp] public void Setup() { f=new P09UiFixture(); }
        [TearDown] public void Teardown() { f?.Dispose(); }

        [Test] public void FirstConsumedDryMethodClueAcknowledgesLearningWithoutRevealingIdentity()
        {
            f.Owner.Session.Dispose(); f.Set("Session",CampaignSession.StartAsync(f.Content,new P09UiFixture.Store()).GetAwaiter().GetResult());
            long input=1;
            for(int guard=0;f.Owner.Session.CurrentText?.Id!="text_rc1_0323_act1_edit" && guard<20;guard++) {
                if(f.Owner.Session.State.Phase==CampaignPhase.Arriving) f.Owner.Session.TickAsync(2).GetAwaiter().GetResult();
                else f.Owner.Session.AdvanceAsync(input++).GetAwaiter().GetResult();
            }
            Assert.That(f.Owner.Session.CurrentText?.Id,Is.EqualTo("text_rc1_0323_act1_edit"));
            Assert.That(f.Owner.Session.AdvanceAsync(input++).GetAwaiter().GetResult(),Is.True);
            var projection=f.Owner.Session.Knowledge.Project("recipe_dry_martini");
            Assert.That(projection.Method,Is.EqualTo("Stir")); Assert.That(projection.Name,Is.Null); Assert.That(projection.Ingredients,Is.Empty);
            f.Show("codex","recipe_dry_martini");
            Assert.That(f.RecipeButton("recipe_dry_martini").GetComponentInChildren<TextMeshProUGUI>().text,Is.EqualTo("들었던 한 잔"));
            Assert.That(f.Words(),Does.Contain("Stir").And.Not.Contain("Dry Martini").And.Not.Contain("칵테일 잔"));
        }
        [TestCase("method")][TestCase("preparation")][TestCase("garnish")]
        public void AnyProjectedFactChangesTheGenericIndexLabel(string fact)
        {
            f.Owner.Session.Knowledge.GrantFact("recipe_dry_martini",fact); f.Show("codex","recipe_dry_martini");
            Assert.That(f.RecipeButton("recipe_dry_martini").GetComponentInChildren<TextMeshProUGUI>().text,Is.EqualTo("들었던 한 잔"));
            Assert.That(f.Words(),Does.Not.Contain("Dry Martini").And.Not.Contain("칵테일 잔"));
        }
        [Test] public void UnknownAndIngredientOnlyPagesDoNotFillMissingAmountsPreparationOrGlass()
        {
            f.Show("codex","recipe_last_word");
            Assert.That(f.Words(),Does.Not.Contain("라스트 워드").And.Not.Contain("그린 샤르트뢰즈").And.Not.Contain("22.5 ml").And.Not.Contain("U로 거르기").And.Not.Contain("칵테일 잔"));
            f.Owner.Session.Knowledge.GrantFact("recipe_last_word","ingredient:green_chartreuse"); f.Show("codex","recipe_last_word");
            Assert.That(f.Words(),Does.Contain("그린 샤르트뢰즈").And.Contain("양 미상").And.Not.Contain("22.5 ml").And.Not.Contain("U로 거르기").And.Not.Contain("칵테일 잔"));
            Assert.That(f.Owner.Session.Knowledge.Project("recipe_last_word").Known,Is.False);
        }
        [TestCase("recipe_french_75")][TestCase("recipe_manwol")]
        public void PreparationDoesNotFillUntaughtLateOrLayerIngredientNames(string recipeId)
        {
            f.Owner.Session.Knowledge.GrantFact(recipeId,"preparation"); f.Show("codex",recipeId);
            foreach(string ingredient in f.Content.Catalog.Recipes[recipeId].Amounts.Keys)
                Assert.That(f.Words(),Does.Not.Contain(f.Content.IngredientNames[ingredient]),"Preparation must not create a separate ingredient-disclosure route.");
            Assert.That(f.Owner.Session.Knowledge.Project(recipeId).Known,Is.False);
        }
        [TestCase("old_fashioned","올드 패션드 잔")][TestCase("cocktail","칵테일 잔")]
        [TestCase("tumbler","텀블러 잔")][TestCase("flute","플루트 잔")]
        public void KnownGlasswareComesFromMetadataRatherThanRecipeIdOrFinish(string glass,string label)
        {
            f.ChangeGlassware("recipe_french_75",glass);
            foreach(var fact in new[]{"identity","method","preparation","garnish"}) f.Owner.Session.Knowledge.GrantFact("recipe_french_75",fact);
            f.Show("codex","recipe_french_75"); Assert.That(f.Words(),Does.Not.Contain(label),"Partial preparation is not permission to expose glassware.");
            f.Owner.Session.Knowledge.SeedKnown("recipe_french_75","p09_ui_fixture"); f.Show("codex","recipe_french_75");
            Assert.That(f.Words(),Does.Contain(label));
            if(glass!="flute") Assert.That(f.Words(),Does.Not.Contain("플루트 잔"),"The original finish must not retain a second glassware authority.");
        }
        [TestCase("guide")][TestCase("settings")][TestCase("codex")][TestCase("reset")][TestCase("recovery")][TestCase("save_error")]
        public void ModalReadingAndControlsRespondToBothReadingPreferences(string modal)
        {
            f.Show(modal,"recipe_last_word");
            var ordinary=f.ModalTexts().Where(x=>!string.IsNullOrWhiteSpace(x.text)).GroupBy(x=>x.text).ToDictionary(x=>x.Key,x=>(Size:x.First().fontSize,Color:x.First().color));
            f.Settings(true,false); f.Show(modal,"recipe_last_word");
            foreach(var text in f.ModalTexts().Where(x=>ordinary.ContainsKey(x.text)))
                Assert.That(text.fontSize,Is.GreaterThan(ordinary[text.text].Size),modal+" LargeText: "+text.text);
            f.Settings(false,true); f.Show(modal,"recipe_last_word");
            foreach(var text in f.ModalTexts().Where(x=>ordinary.ContainsKey(x.text))) {
                var before=ordinary[text.text].Color; var after=text.color;
                Assert.That(after.r+after.g+after.b,Is.GreaterThan(before.r+before.g+before.b),modal+" HighContrast: "+text.text);
            }
        }
        [Test] public void RequestMeasurementAndToolbarUseTheReadingPreferences()
        {
            var wanted=new[]{"request","measurement","title"};
            var text=wanted.Select(n=>(TextMeshProUGUI)typeof(CampaignUi).GetField(n,P09UiFixture.Hidden).GetValue(f.Ui)).Concat(new[]{f.Button("도감  Tab").GetComponentInChildren<TextMeshProUGUI>()}).ToArray();
            var sizes=text.Select(x=>x.fontSize).ToArray(); var colors=text.Select(x=>x.color).ToArray();
            f.Settings(true,true);
            for(int i=0;i<text.Length;i++) {
                Assert.That(text[i].fontSize,Is.GreaterThan(sizes[i]),text[i].text);
                Assert.That(text[i].color.r+text[i].color.g+text[i].color.b,Is.GreaterThan(colors[i].r+colors[i].g+colors[i].b),text[i].text);
            }
        }
        [TestCase(1280,720,false,false)][TestCase(1280,720,true,false)][TestCase(1280,720,false,true)][TestCase(1280,720,true,true)]
        [TestCase(1920,1080,false,false)][TestCase(1920,1080,true,false)][TestCase(1920,1080,false,true)][TestCase(1920,1080,true,true)]
        public void EveryKnownPageKeepsActualKoreanGlyphsInsideItsReadingSurface(int width,int height,bool large,bool contrast)
        {
            f.Resolution(width,height); f.Settings(large,contrast);
            foreach(var id in f.Content.Catalog.Recipes.Keys) f.Owner.Session.Knowledge.SeedKnown(id,"p09_ui_fixture");
            foreach(var id in f.Content.Catalog.Recipes.Keys) {
                f.Show("codex",id); f.Render();
                Assert.That(f.Ui.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Recipe page ")),Is.EqualTo(9));
                foreach(var text in f.ModalTexts()) {
                    text.ForceMeshUpdate(true); var rect=text.rectTransform.rect;
                    Assert.That(text.isTextOverflowing,Is.False,id+" TMP layout overflow: "+text.text);
                    if(text.transform.parent==f.Modal.transform.GetChild(0) && text.rectTransform.anchoredPosition.x>=300)
                        Assert.That(text.rectTransform.anchoredPosition.y,Is.GreaterThanOrEqualTo(78),id+" must clear the journal footer and Close action.");
                    foreach(var glyph in text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(x=>x.isVisible))
                        Assert.That(glyph.bottomLeft.x>=rect.xMin-1 && glyph.topRight.x<=rect.xMax+1 && glyph.bottomLeft.y>=rect.yMin-1 && glyph.topRight.y<=rect.yMax+1,
                            Is.True,id+" "+width+"x"+height+" large="+large+" glyph outside text surface: "+text.text);
                }
            }
        }
    }

    public sealed class P09ShippedFontUiTests
    {
        private P09UiFixture f;
        [SetUp] public void Setup() { f=new P09UiFixture(true); }
        [TearDown] public void Teardown() { f?.Dispose(); }
        private void AssertReadingSurface(string context)
        {
            f.Render();
            foreach(var text in f.Ui.GetComponentsInChildren<TextMeshProUGUI>().Where(x=>x.isActiveAndEnabled && !string.IsNullOrWhiteSpace(x.text))) {
                text.ForceMeshUpdate(true); var rect=text.rectTransform.rect;
                Assert.That(text.isTextOverflowing,Is.False,context+" TMP layout overflow: "+text.text+" size="+text.fontSize+" rect="+rect.size+" preferred="+text.GetPreferredValues());
                foreach(var glyph in text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(x=>x.isVisible))
                    Assert.That(glyph.bottomLeft.x>=rect.xMin-1 && glyph.topRight.x<=rect.xMax+1 && glyph.bottomLeft.y>=rect.yMin-1 && glyph.topRight.y<=rect.yMax+1,
                        Is.True,context+" visible glyph outside text surface: "+text.text);
                if(f.Ui.VisibleModal=="codex" && text.transform.parent==f.Modal.transform.GetChild(0) && text.rectTransform.anchoredPosition.x>=300)
                    Assert.That(text.rectTransform.anchoredPosition.y,Is.GreaterThanOrEqualTo(78),context+" must clear the journal footer and Close action.");
            }
        }
        [TestCase(1280,720,false,false)][TestCase(1280,720,true,false)][TestCase(1280,720,false,true)][TestCase(1280,720,true,true)]
        [TestCase(1920,1080,false,false)][TestCase(1920,1080,true,false)][TestCase(1920,1080,false,true)][TestCase(1920,1080,true,true)]
        public void ShippedFontKnownPagesAndReadingModalsFitBothLayoutAndVisibleGlyphs(int width,int height,bool large,bool contrast)
        {
            f.At(46,CampaignPhase.Complete); f.Resolution(width,height); f.Settings(large,contrast);
            foreach(var id in f.Content.Catalog.Recipes.Keys) f.Owner.Session.Knowledge.SeedKnown(id,"p09_ui_fixture");
            foreach(var id in f.Content.Catalog.Recipes.Keys) { f.Show("codex",id); AssertReadingSurface(id); }
            foreach(string modal in new[]{"guide","settings","reset","recovery","save_error"}) { f.Show(modal); AssertReadingSurface(modal); }
        }
        [TestCase(1280,720,false,false)][TestCase(1280,720,true,false)][TestCase(1280,720,false,true)][TestCase(1280,720,true,true)]
        [TestCase(1920,1080,false,false)][TestCase(1920,1080,true,false)][TestCase(1920,1080,false,true)][TestCase(1920,1080,true,true)]
        public void ShippedFontUnknownAndPartialPagesFitWithoutGrantingKnown(int width,int height,bool large,bool contrast)
        {
            f.Resolution(width,height); f.Settings(large,contrast);
            foreach(var recipe in f.Content.Catalog.Recipes.Values) {
                f.At(40);
                if(f.Owner.Session.Knowledge.Project(recipe.Id).Known) continue;
                f.Show("codex",recipe.Id); AssertReadingSurface(recipe.Id+" Unknown");
                foreach(string fact in new[]{"identity","method","preparation","garnish"}) f.Owner.Session.Knowledge.GrantFact(recipe.Id,fact);
                foreach(string id in recipe.Amounts.Keys) f.Owner.Session.Knowledge.GrantFact(recipe.Id,"amount:"+id);
                f.Show("codex",recipe.Id); AssertReadingSurface(recipe.Id+" Partial");
                Assert.That(f.Owner.Session.Knowledge.Project(recipe.Id).Known,Is.False,"Layout must not manufacture a discovery receipt.");
            }
        }
        [TestCase(17,1280,720)][TestCase(21,1280,720)]
        [TestCase(17,1920,1080)][TestCase(21,1920,1080)]
        public void ShippedFontExpandedShelfKeepsEverySuppliedLabelInsideItsButton(int slot,int width,int height)
        {
            f.Resolution(width,height);
            foreach(var setting in new[]{(false,false),(true,false),(false,true),(true,true)}) {
                f.At(slot); f.Settings(setting.Item1,setting.Item2); f.Show(null);
                var seen=new HashSet<string>();
                for(int page=0;page<4;page++) {
                    AssertReadingSurface("slot "+slot+" shelf page "+page+" large="+setting.Item1);
                    var buttons=f.Ui.GetComponentsInChildren<Button>();
                    foreach(string id in f.Owner.Session.Supply) {
                        string name=f.Content.IngredientNames[id];
                        if(buttons.Any(b=>b.GetComponentInChildren<TextMeshProUGUI>() is TextMeshProUGUI label &&
                            (label.text==name || label.text.EndsWith("  "+name,StringComparison.Ordinal)))) seen.Add(id);
                    }
                    var next=buttons.SingleOrDefault(b=>b.name=="다음 재료");
                    if(next==null || !next.IsInteractable()) break;
                    next.onClick.Invoke(); f.Ui.Refresh();
                }
                Assert.That(seen,Is.EquivalentTo(f.Owner.Session.Supply),"Reading preferences must keep every supplied ingredient reachable.");
            }
        }
    }

    public sealed class P09ModalInteractionTests
    {
        [UnityTearDown] public IEnumerator LeavePlayModeAfterAnyResult()
        { if(UnityEngine.Application.isPlaying) yield return new ExitPlayMode(); }
        [UnityTest] public IEnumerator RecipeActivationRetainsItsFocusAndModalCloseRestoresTheCaller()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            yield return new EnterPlayMode();
            using(var f=new P09UiFixture()) {
                var caller=f.Button("도감  Tab"); EventSystem.current.SetSelectedGameObject(caller.gameObject);
                long sequence=f.Owner.Session.State.Sequence;
                f.Owner.OpenModal("codex"); f.Ui.Refresh();
                var page=f.RecipeButton("recipe_dry_martini");
                ExecuteEvents.Execute(page.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
                yield return null; f.Ui.Refresh();
                Assert.That(EventSystem.current.currentSelectedGameObject,Is.EqualTo(f.RecipeButton("recipe_dry_martini").gameObject),"Pointer selection must not move focus to Close.");
                Assert.That(f.Ui.ActivateFocused(true),Is.True,"Use the production keyboard activation boundary.");
                yield return null; f.Ui.Refresh();
                Assert.That(EventSystem.current.currentSelectedGameObject,Is.EqualTo(f.RecipeButton("recipe_dry_martini").gameObject));
                f.RecipeButton("recipe_dry_martini").enabled=false; f.Ui.Refresh();
                var fallback=EventSystem.current.currentSelectedGameObject.GetComponent<Button>();
                Assert.That(fallback.isActiveAndEnabled && fallback.IsInteractable() && fallback.transform.IsChildOf(f.Modal.transform),Is.True);
                Object.Destroy(fallback.gameObject); yield return null; f.Ui.Refresh();
                Assert.That(f.Ui.ModalOwnsFocus,Is.True,"Destroyed selections recover inside the top modal.");
                f.Owner.CloseModal(); f.Ui.Refresh(); yield return null;
                Assert.That(EventSystem.current.currentSelectedGameObject,Is.EqualTo(caller.gameObject));
                Assert.That(f.Owner.Session.State.Sequence,Is.EqualTo(sequence),"Reading the Codex must not publish story progress.");
            }
            yield return new ExitPlayMode();
        }
    }
}
