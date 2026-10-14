using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Domain;
using Surlhwa.Presentation;
namespace Surlhwa.SpatialTests
{
    public sealed class CampaignInputTests
    {
        private sealed class Memory : ICampaignSaveStore { public Task SaveAsync(CampaignCheckpoint s)=>Task.CompletedTask; }
        private InputTestFixture fixture; private Keyboard keyboard; private Mouse mouse; private GameObject go;
        private CampaignSession session; private SliceInputAdapter input;
        [SetUp] public void Setup()
        {
            fixture=new InputTestFixture(); fixture.Setup(); keyboard=InputSystem.AddDevice<Keyboard>(); mouse=InputSystem.AddDevice<Mouse>();
            var c=CampaignContentLoader.Load(File.ReadAllText(Path.GetFullPath("../Content/Campaign/act1.json")));
            session=CampaignSession.StartAsync(c,new Memory()).GetAwaiter().GetResult(); long n=1;
            while(session.State.Phase!=CampaignPhase.Brewing)
                if(session.State.Phase==CampaignPhase.Arriving) session.TickAsync(2).GetAwaiter().GetResult(); else session.AdvanceAsync(n++).GetAwaiter().GetResult();
            go=new GameObject("Campaign production input test"); input=go.AddComponent<SliceInputAdapter>(); input.Bind(session,null,Vector3.zero);
        }
        [TearDown] public void TearDown() { UnityEngine.Object.DestroyImmediate(go); session.Dispose(); fixture.TearDown(); }
        private void Down(Key key) { InputSystem.QueueStateEvent(keyboard,new KeyboardState(key)); InputSystem.Update(); }
        private void Up() { InputSystem.QueueStateEvent(keyboard,new KeyboardState()); InputSystem.Update(); }
        private void Tap(Key key) { Down(key); Up(); }
        private void Move(float x,float y) { InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(Screen.width*x,Screen.height*y)}); InputSystem.Update(); }
        private void Flow(Key key,decimal seconds) { Down(key); while(seconds>0) { decimal dt=Math.Min(.1m,seconds); input.Step((double)dt); seconds-=dt; } Up(); }
        [Test] public void BuildThroughSharedControlsUsesServingIceGentleStirAndOrange()
        {
            input.SelectTarget(ContainerRole.Serving); Tap(Key.I);
            foreach(string id in new[]{"gin","campari","sweet_red_vermouth"}) { input.SelectIngredient(id); Flow(Key.Q,1); }
            Tap(Key.G); Tap(Key.O);
            var result=ServiceEvaluator.Evaluate(session.Content.Catalog,session.Content.Tuning,session.Brew.Freeze(),new DrinkRequest("bitterness"));
            Assert.That(result.RecipeId,Is.EqualTo("recipe_negroni")); Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Good)); Assert.That(result.QualityApplicable,Is.False);
        }
        [Test] public void ShakeAndLateSplashUseActualPointerBatchAndHeldFlow()
        {
            input.SelectTarget(ContainerRole.Shaker);
            foreach(var a in new[]{("gin",45m),("lemon_juice",30m),("simple_syrup",10m)}) { input.SelectIngredient(a.Item1); Flow(Key.Q,a.Item2/30m); }
            Tap(Key.I); Tap(Key.L); Move(.4f,.5f); Down(Key.K);
            for(int i=0;i<10;i++) { Move(i%2==0 ? .6f : .4f,.5f); input.Step(.25); input.Step(.25); }
            Up(); Tap(Key.L); Flow(Key.T,4);
            input.SelectTarget(ContainerRole.Serving); input.SelectIngredient("soda_water"); Flow(Key.X,.5m);
            var snapshot=session.Brew.Freeze(); var result=ServiceEvaluator.Evaluate(session.Content.Catalog,session.Content.Tuning,snapshot,new DrinkRequest("awakening"));
            // Held flow uses whole microseconds; the 10 ml fraction ends one clock quantum short.
            Assert.That(snapshot.TotalMl,Is.EqualTo(100m).Within(.00003m)); Assert.That(snapshot.Shake.Reversals,Is.GreaterThanOrEqualTo(8));
            Assert.That(result.RecipeId,Is.EqualTo("recipe_gin_fizz")); Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Good)); Assert.That(result.QualityGood,Is.True);
        }
        [Test] public void FloatTransfersMeasuredLayersWithPointerHeightAndStopsOnFocusLoss()
        {
            foreach(var a in session.Content.Catalog.Recipes["recipe_manwol"].Amounts)
            {
                input.SelectTarget(ContainerRole.Jigger); input.SelectIngredient(a.Key); Flow(Key.Q,a.Value/30m);
                Move(.45f,.38f); Flow(Key.F,10m);
            }
            var result=ServiceEvaluator.Evaluate(session.Content.Catalog,session.Content.Tuning,session.Brew.Freeze(),DrinkRequest.Specific("cocktail_manwol"));
            Assert.That(result.Preparation,Is.EqualTo(PreparationResult.Good)); Assert.That(result.QualityGood,Is.True); Assert.That(result.Fits,Is.True);
            input.SelectIngredient("vodka"); Flow(Key.Q,1); Move(.45f,.38f); Down(Key.F); input.Step(.1);
            session.SetFocus(false); input.Step(.1); var total=session.Brew.Container(ContainerRole.Serving).TotalMl;
            session.SetFocus(true); input.Step(.1); Assert.That(session.Brew.Container(ContainerRole.Serving).TotalMl,Is.EqualTo(total)); Assert.That(input.ActiveTool,Is.Null);
        }
    }
}
