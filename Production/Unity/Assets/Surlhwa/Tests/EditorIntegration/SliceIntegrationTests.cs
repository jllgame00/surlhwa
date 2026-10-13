using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Surlhwa.Domain;
using Surlhwa.Application;
using Surlhwa.ContentIO;
using Surlhwa.Presentation;

namespace Surlhwa.SpatialTests
{
    public sealed class SliceIntegrationTests
    {
        private sealed class Memory : ISliceSaveStore { public Task SaveAsync(SliceCheckpoint s) => Task.CompletedTask; }
        private static void Step(SliceInputAdapter adapter,double seconds) { for(int i=0;i<(int)Math.Round(seconds*10);i++) adapter.Step(.1); }
        private static SliceSession Brewing()
        {
            var c=SliceContentLoader.Load(SliceContentLoader.BundleFromDirectory(Path.GetFullPath("../Content")));
            var s=SliceSession.StartAsync(c,new Memory()).GetAwaiter().GetResult(); long n=1;
            while(s.State.Phase!=SlicePhase.Brewing)
                if(s.State.Phase==SlicePhase.Arriving) s.TickAsync(2).GetAwaiter().GetResult(); else s.AdvanceAsync(n++).GetAwaiter().GetResult();
            return s;
        }
        [Test] public void KeyboardUiCommandsAreAvailableBeforeNewGame_AndConfigurationDoesNotDuplicateThem()
        {
            var fixture=new InputTestFixture(); fixture.Setup(); var keyboard=InputSystem.AddDevice<Keyboard>(); var go=new GameObject("MenuInputTest");
            try {
                var input=go.AddComponent<SliceInputAdapter>(); int calls=0; string command=null; input.UiCommand=value=>{calls++;command=value;};
                input.Configure(new[]{"gin"}); input.Configure(new[]{"gin"});
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter)); InputSystem.Update();
                Assert.That(calls,Is.EqualTo(1)); Assert.That(command,Is.EqualTo("advance"));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.B)); InputSystem.Update();
                Assert.That(calls,Is.EqualTo(2)); Assert.That(command,Is.EqualTo("guide"));
            } finally { UnityEngine.Object.DestroyImmediate(go); fixture.TearDown(); }
        }
        [Test] public void SliceRetainsC1CameraAndCounter_AndPrinceMeetsSupportingSeat()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Surlhwa/Scenes/Bar_Slice.unity"); Physics.SyncTransforms();
            var c=UnityEngine.Object.FindFirstObjectByType<SliceController>();
            Assert.That(c.Camera.orthographic,Is.True); Assert.That(c.Camera.orthographicSize,Is.EqualTo(3.7f).Within(.001f)); Assert.That(c.Camera.transform.eulerAngles.x,Is.EqualTo(28).Within(.001f));
            Assert.That(GameObject.Find("C_Quixote"),Is.Null);
            var seat=GameObject.Find("C_Stool").GetComponent<Collider>().bounds;
            Assert.That(Vector3.Distance(c.Prince.SeatAnchor.position,seat.ClosestPoint(c.Prince.SeatAnchor.position)),Is.LessThan(.003f));
            Assert.That(c.Prince.StandingMesh.bounds.size.y/c.Bartender.StandingMesh.bounds.size.y,Is.InRange(.72f,.84f));
            var front=GameObject.Find("C_CounterTop").GetComponent<Renderer>().bounds;
            Assert.That(c.Bartender.transform.position.z,Is.GreaterThan(front.max.z+.2f));
            Assert.That(c.Prince.SeatAnchor.position.z,Is.LessThan(front.min.z-.25f));
        }
        [Test] public void InputSystemPourStopsOnReleaseModalAndFocusLoss()
        {
            var fixture=new InputTestFixture(); fixture.Setup();
            var keyboard=InputSystem.AddDevice<Keyboard>(); var mouse=InputSystem.AddDevice<Mouse>(); var go=new GameObject("AdapterTest");
            try
            {
                var s=Brewing(); var adapter=go.AddComponent<SliceInputAdapter>(); adapter.Bind(s,null,Vector3.zero);
                adapter.SelectIngredient("gin"); adapter.SelectTarget(ContainerRole.Mixing);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Q)); InputSystem.Update(); Step(adapter,1);
                Assert.That(s.Brew.Container(ContainerRole.Mixing).TotalMl,Is.EqualTo(30m));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); InputSystem.Update(); Step(adapter,1);
                Assert.That(s.Brew.Container(ContainerRole.Mixing).TotalMl,Is.EqualTo(30m));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Q)); InputSystem.Update(); Step(adapter,.5);
                s.OpenModal("guide"); Step(adapter,1); s.CloseModal(); Step(adapter,1);
                Assert.That(s.Brew.Container(ContainerRole.Mixing).TotalMl,Is.EqualTo(45m));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Q)); InputSystem.Update(); Step(adapter,.5);
                s.SetFocus(false); Step(adapter,1); s.SetFocus(true); Step(adapter,1);
                Assert.That(s.Brew.Container(ContainerRole.Mixing).TotalMl,Is.EqualTo(60m));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse); fixture.TearDown(); }
        }
        [Test] public void WorkbenchShowsActualVolumeAndNeverFillsCocktailStem()
        {
            var go=new GameObject("ViewTest");
            try
            {
                var s=Brewing(); var view=go.AddComponent<SliceWorkbenchView>(); view.Initialize(null); view.CocktailGlass=true;
                s.ExecuteBrew(900,b=>b.Pour("water",ContainerRole.Serving,90)); view.Refresh(s.Brew,null,0);
                Assert.That(view.ServingFillMl,Is.EqualTo(90m)); Assert.That(view.ServingFillBottom,Is.GreaterThan(view.ServingStemTop));
                view.FlatTools=true; view.Refresh(s.Brew,null,0); Assert.That(view.ServingFillMl,Is.EqualTo(90m));
                Assert.That(s.Brew.Container(ContainerRole.Serving).TotalMl,Is.EqualTo(90m));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [TestCase("recipe_test_stir",true)][TestCase("recipe_manhattan",true)][TestCase("recipe_dry_martini",false)]
        public void ThreeActualRecipesThroughInputSystemRemainIndependentOfWarmth(string recipe,bool fits)
        {
            var fixture=new InputTestFixture(); fixture.Setup(); var keyboard=InputSystem.AddDevice<Keyboard>(); var mouse=InputSystem.AddDevice<Mouse>(); var go=new GameObject("RecipeInputTest");
            try
            {
                var s=Brewing(); var a=go.AddComponent<SliceInputAdapter>(); a.Bind(s,null,Vector3.zero);
                void Down(Key key) { InputSystem.QueueStateEvent(keyboard,new KeyboardState(key)); InputSystem.Update(); }
                void Up() { InputSystem.QueueStateEvent(keyboard,new KeyboardState()); InputSystem.Update(); }
                void Tap(Key key) { Down(key); Up(); }
                void Flow(Key key,decimal seconds) { Down(key); while(seconds>0) { decimal dt=Math.Min(.1m,seconds); a.Step((double)dt); seconds-=dt; } Up(); }
                var ingredients=new System.Collections.Generic.Dictionary<string,Key>{{"whiskey",Key.Digit1},{"vermouth",Key.Digit2},{"water",Key.Digit3},{"rye_whiskey",Key.Digit4},{"sweet_red_vermouth",Key.Digit5},{"angostura_bitters",Key.Digit6},{"gin",Key.Digit7},{"dry_vermouth",Key.Digit8}};
                foreach(var amount in s.Content.Catalog.Recipes[recipe].Amounts)
                {
                    if(amount.Key=="angostura_bitters") continue;
                    Tap(ingredients[amount.Key]); Flow(Key.Q,amount.Value/30m); Flow(Key.E,2m);
                }
                Tap(Key.J); if(recipe=="recipe_manhattan") { Tap(Key.Digit6); Tap(Key.D); } Tap(Key.I);
                var center=new Vector2(Screen.width*.5f,Screen.height*.5f);
                InputSystem.QueueStateEvent(mouse,new MouseState{position=center+Vector2.right*27}); InputSystem.Update(); Down(Key.S);
                for(int i=1;i<=78;i++) { float angle=i*Mathf.PI/12; InputSystem.QueueStateEvent(mouse,new MouseState{position=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*27}); InputSystem.Update(); a.Step(1d/12); }
                Up(); Flow(Key.R,4); if(recipe=="recipe_manhattan") Tap(Key.C);
                var outcome=ServiceEvaluator.Evaluate(s.Content.Catalog,s.Content.Tuning,s.Brew.Freeze(),new DrinkRequest("warmth"));
                Assert.That(outcome.RecipeId,Is.EqualTo(recipe)); Assert.That(outcome.Preparation,Is.EqualTo(PreparationResult.Good)); Assert.That(outcome.QualityGood,Is.True); Assert.That(outcome.Fits,Is.EqualTo(fits));
                // The same event sequence source supplies the flow command; UI and tools share one identity stream.
                Assert.That(s.AdvanceAsync(a.NextEventId()).GetAwaiter().GetResult(),Is.True);
                Assert.That(s.State.Services.Count,Is.EqualTo(1));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); fixture.TearDown(); }
        }
    }
}
