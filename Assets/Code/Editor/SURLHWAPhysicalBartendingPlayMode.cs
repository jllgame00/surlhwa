#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SURLHWAPhysicalBartendingPlayMode
{
    private const string Key = "SURLHWA_K_PHYSICAL_TEST";
    static SURLHWAPhysicalBartendingPlayMode() => EditorApplication.playModeStateChanged += Changed;
    [MenuItem("SURLHWA/Validation/Physical Bartending Play Mode")]
    public static void Run()
    {
        SessionState.SetInt(Key, 1);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }
    private static void Changed(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetInt(Key, 0) == 1)
            new GameObject("Phase K Physical Validation").AddComponent<SURLHWAPhysicalRunner>();
        if (change == PlayModeStateChange.EnteredEditMode && SessionState.GetInt(Key, 0) >= 2)
        {
            int code = SessionState.GetInt(Key, 0) == 2 ? 0 : 1;
            SessionState.EraseInt(Key);
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }
    }
    internal static void Complete(bool success)
    {
        SessionState.SetInt(Key, success ? 2 : 3);
        EditorApplication.ExitPlaymode();
    }
}

internal sealed class SURLHWAPhysicalRunner : MonoBehaviour
{
    private readonly System.Collections.Generic.Stack<IEnumerator> stack = new();
    private IEnumerator Start()
    {
        Application.runInBackground = true;
        stack.Push(Validate());
        float deadline = Time.realtimeSinceStartup + 120f;
        while (stack.Count > 0)
        {
            object current;
            try
            {
                Require(Time.realtimeSinceStartup < deadline, "Physical validation timed out.");
                if (!stack.Peek().MoveNext()) { stack.Pop(); continue; }
                current = stack.Peek().Current;
                if (current is IEnumerator nested) { stack.Push(nested); continue; }
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                SURLHWAPhysicalBartendingPlayMode.Complete(false);
                yield break;
            }
            yield return current;
        }
        Debug.Log("[Phase K Play Mode] PASS: programmatic physical input, actual tool transforms, finite measurement/overflow, source reduction and target growth.");
        SURLHWAPhysicalBartendingPlayMode.Complete(true);
    }

    private IEnumerator Validate()
    {
        var sounds = gameObject.AddComponent<SurlhwaSoundscape>();
        sounds.SetMix(.7f, .35f, .6f, false); sounds.Trace = true;
        var cueCounts = new System.Collections.Generic.Dictionary<BarSound, int>();
        sounds.CueChanged += (cue, start, channel) => { if (start) cueCounts[cue] = cueCounts.GetValueOrDefault(cue) + 1; };
        foreach (string method in new[] { "Build", "Stir", "Shake", "Float" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_SURLHWA/Prefabs/Bartending/" + method + "MiniGame.prefab");
            var root = Instantiate(prefab);
            var approvedShaker = root.transform.Find("UI/Shaker")?.GetComponent<UnityEngine.UI.Image>()?.sprite;
            var game = root.GetComponent<IMiniGame>();
            // Drive the production input seams explicitly, without competing
            // keyboard polling from an unattended Editor mouse/keyboard.
            ((MonoBehaviour)game).enabled = false;
            game.StartGame(_ => { });
            SurlhwaBartendingPresentation.Apply(root, "물리 조주 검증"); // Explicit fixture label, not a guest request.
            var shelf = root.GetComponentInChildren<IngredientShelfController>(true);
            root.AddComponent<BartendingAudioPresentation>().Bind(sounds);
            var ingredient = shelf.ConfiguredIngredients[0];
            foreach (var candidate in shelf.ConfiguredIngredients)
                if (shelf.TrySelectIngredient(candidate)) { ingredient = candidate; break; }
            Require(shelf.SelectedIngredient != null, "No supplied selectable ingredient.");
            var view = shelf.PhysicalPresentation;
            Require(view != null, "Production route did not install physical view.");
            if (method == "Shake")
                Require(view.GetVessel(shelf.TargetContainer).Find("OpenShakerMouth").gameObject.activeSelf,
                    "Shaker must be open while receiving ingredients.");
            Require(view.GetVessel(shelf.ServingGlass).Find("PixelShell").GetComponent<UnityEngine.UI.Image>().sprite != null,
                "Phase L serving shell missing in production Play Mode.");
            float original = shelf.TargetContainer.CurrentLiquidVolumeMl;
            var floating = game as FloatMiniGame;
            if (floating != null) floating.BeginLayerPour(FloatMiniGame.FloatPourIntensity.Gentle);
            while (!shelf.PreparePhysicalPour())
            {
                Require(shelf.TargetContainer.CurrentLiquidVolumeMl == original, "Liquid moved during bottle alignment.");
                yield return null;
            }
            float bottleMl = floating != null
                ? floating.PourActiveLayerForDuration(12.5f / 30f, FloatMiniGame.FloatPourIntensity.Gentle)
                : shelf.PourSelectedIngredient(12.5f);
            Require(Mathf.Abs(bottleMl - 12.5f) < .001f && view.StreamVisible, "Accepted bottle stream missing.");
            SURLHWAReviewCapture.Save("phase-k-" + method + "-pour", 1280, 720);
            yield return null; // Let the actual visible stream reach the presentation observer.
            Require(sounds.PourSource.isPlaying && sounds.PourSource.clip == sounds.Library.Get(view.StreamSound).clip,
                method + " visible stream/audio start mismatch.");
            if (floating != null) floating.EndActiveLayerPour();
            shelf.EndSelectedPour();
            Require(!view.StreamVisible, "Release continued pouring.");
            while (!view.IsResting) yield return null;
            Require(!sounds.PourSource.isPlaying, method + " audio remained after bottle return.");
            if (floating != null)
                foreach (var candidate in shelf.ConfiguredIngredients)
                    if (candidate.ingredientId == "cream" && shelf.TrySelectIngredient(candidate))
                    { ingredient = candidate; break; }
            while (!view.PrepareBottle(ingredient, shelf.Jigger)) yield return null;
            float measured = shelf.FillJigger(72f);
            Require(measured == 60f && shelf.Jigger.CurrentLiquidVolumeMl == 60f, "Overflow accounting wrong.");
            Require(Mathf.Abs(shelf.TargetContainer.CurrentLiquidVolumeMl - 12.5f) < .001f, "Measuring changed destination.");
            shelf.PhysicalPresentation.EndBottle();
            while (!view.IsResting) yield return null;
            Require(!sounds.PourSource.isPlaying, method + " audio remained after bottle return.");
            if (floating != null) floating.SetMeasuredPourIntensity(FloatMiniGame.FloatPourIntensity.Gentle);
            Require(shelf.BeginMeasuredTransfer(), "Measured transfer did not start.");
            Require(!shelf.TrySelectIngredient(null), "Invalid selection accepted.");
            float previous = 60f;
            bool observedPartial = false;
            while (shelf.PhysicalTransferActive)
            {
                float source = shelf.Jigger.CurrentLiquidVolumeMl;
                float target = shelf.TargetContainer.CurrentLiquidVolumeMl;
                Require(Mathf.Abs(source + target - 72.5f) < .002f, "Transfer did not conserve liquid.");
                Require(source <= previous + .001f, "Source grew during outgoing transfer.");
                if (source > .01f && source < 59.99f && !observedPartial)
                {
                    observedPartial = true;
                    SURLHWAReviewCapture.Save("phase-k-" + method + "-measure", 1280, 720);
                }
                previous = source;
                yield return null;
            }
            Require(observedPartial && shelf.Jigger.CurrentLiquidVolumeMl == 0f, "Transfer must visibly empty over frames.");
            Require(shelf.ToolsReady, "Tools remained locked after return.");
            if (floating != null)
            {
                Require(floating.LayeredDrinkState.Layers.Count == 2 && floating.LayeredDrinkState.Boundaries.Count == 1,
                    "Measured Float lost its ordered layer allocation.");
                Require(floating.LayeredDrinkState.Boundaries[0].Quality == BartendingLayerBoundaryQuality.Clean,
                    "Measured gentle flow changed Float judgment.");
                float allocated = 0f;
                foreach (var layer in floating.LayeredDrinkState.Layers) allocated += layer.AmountMl;
                Require(Mathf.Abs(allocated - shelf.ServingGlass.CurrentLiquidVolumeMl) < .002f,
                    "Layer allocation differs from actual serving liquid.");
                var layerImage = root.transform.GetComponentInChildren<FloatMiniGame>().transform
                    .GetComponentInChildren<IngredientShelfController>(true).PhysicalPresentation
                    .GetLiquidMask(shelf.ServingGlass).Find("LiquidArea/TopLiquid")?.GetComponent<UnityEngine.UI.Image>();
                Require(layerImage != null && layerImage.color == ingredient.liquidColor,
                    "Float color does not follow the actual second ingredient.");
            }
            if (method == "Build")
            {
                float beforeDose = shelf.ServingGlass.CurrentLiquidVolumeMl;
                Require(shelf.BeginPhysicalDose(RecipePreparationAction.Dash), "Physical Dash rejected.");
                Require(shelf.ServingGlass.CurrentLiquidVolumeMl == beforeDose, "Dash arrived before bottle movement.");
                while (shelf.PhysicalTransferActive) yield return null;
                Require(Mathf.Abs(shelf.ServingGlass.CurrentLiquidVolumeMl - beforeDose - BartendingVolume.DashesToMilliliters(1)) < .002f,
                    "Physical Dash quantity mismatch.");
                beforeDose = shelf.ServingGlass.CurrentLiquidVolumeMl;
                Require(shelf.BeginPhysicalDose(RecipePreparationAction.Splash, BartendingVolume.DefaultSplashMilliliters), "Physical Splash rejected.");
                while (shelf.PhysicalTransferActive) yield return null;
                Require(Mathf.Abs(shelf.ServingGlass.CurrentLiquidVolumeMl - beforeDose - BartendingVolume.DefaultSplashMilliliters) < .002f,
                    "Physical Splash quantity mismatch.");
                while (!shelf.PreparePhysicalPour(RecipePreparationAction.TopUp)) yield return null;
                shelf.PourSelectedIngredient(8f, RecipePreparationAction.TopUp);
                shelf.EndSelectedPour();
                while (!view.IsResting) yield return null;
            Require(!sounds.PourSource.isPlaying, method + " audio remained after bottle return.");
                Require(shelf.PreparationTrace.Actions[shelf.PreparationTrace.Actions.Count - 1].Action == RecipePreparationAction.TopUp,
                    "TopUp changed into an ordinary Pour.");
            }
            if (method == "Stir" || method == "Shake")
            {
                var source = shelf.TargetContainer;
                Require(shelf.TryAddIceToActiveTarget(), "Physical ice placement rejected.");
                Require(!source.HasIce, "Ice appeared before placement.");
                while (shelf.PhysicalTransferActive) yield return null;
                Require(source.HasIce && view.GetIceVisual(source).gameObject.activeInHierarchy,
                    "Authoritative ice has no attached visual.");
                if (method == "Stir")
                {
                    var stir = (StirMiniGame)game;
                    var bowl = root.transform.Find("UI/Spoon") as RectTransform;
                    var vessel = view.GetVessel(source);
                    var update = typeof(StirMiniGame).GetMethod("UpdateSpoonVisual",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    stir.BeginStir();
                    for (int step = 0; step <= 72; step++)
                    {
                        stir.TryRecordStirAngle(step * 10f, .05f);
                        update.Invoke(stir, new object[] { .05f });
                        Require(view.TryGetLiquidBounds(vessel, out float bottom, out float surface), "Liquid bounds missing.");
                        var tip = vessel.InverseTransformPoint(bowl.position);
                        Require(tip.y >= bottom && tip.y <= surface && vessel.rect.Contains(tip),
                            "Stir bowl left the actual liquid region.");
                        Require(Mathf.Abs(Mathf.DeltaAngle(90f, bowl.localEulerAngles.z)) <= 8.1f,
                            "Pixel spoon shaft became a windmill.");
                        yield return null;
                    }
                    Require(sounds.TechniqueSource.isPlaying && cueCounts.GetValueOrDefault(BarSound.Stir) > 0,
                        "Moving spoon has no Stir loop.");
                    SURLHWAReviewCapture.Save("phase-l-Stir-action", 1280, 720);
                    stir.FinishStir();
                }
                if (method == "Shake")
                {
                    var shake = (ShakeMiniGame)game;
                    var body = view.GetVessel(source);
                    Vector2 home = body.anchoredPosition;
                    float beforeShakeMl = source.CurrentLiquidVolumeMl;
                    shake.BeginShake();
                    Require(body.Find("PixelShell").GetComponent<UnityEngine.UI.Image>().sprite == approvedShaker &&
                        !body.Find("OpenShakerMouth").gameObject.activeSelf,
                        "Held Shake must restore the exact approved closed sprite.");
                    for (int note = 0; note < shake.NoteCount; note++)
                    {
                        while (Time.unscaledTime < shake.GetExpectedNoteTime(note)) yield return null;
                        var direction = note % 2 == 0 ? ShakeMiniGame.ShakeDirection.Left : ShakeMiniGame.ShakeDirection.Right;
                        Require(shake.TryJudgeShakeInput(direction, Time.unscaledTime) != ShakeMiniGame.ShakeJudgement.Miss,
                            "Physical Shake changed rhythm judgment.");
                        float observeUntil = Time.unscaledTime + .05f;
                        while (Time.unscaledTime < observeUntil) yield return null;
                        Require((body.anchoredPosition.x - home.x) * (note % 2 == 0 ? -1f : 1f) > 0f,
                            "Shake translation did not follow direction.");
                        Require(Mathf.Abs(Mathf.DeltaAngle(0f, body.localEulerAngles.z)) > .1f &&
                            Mathf.Abs(Mathf.DeltaAngle(0f, body.localEulerAngles.z)) <= 12.1f,
                            "Shake rotation absent or unbounded.");
                        Require(source.CurrentLiquidVolumeMl == beforeShakeMl && shelf.ServingGlass.CurrentLiquidVolumeMl == 0f,
                            "Shake released its contents before transfer.");
                        if (note == 2) SURLHWAReviewCapture.Save("phase-l-Shake-held", 1280, 720);
                    }
                    float settle = Time.unscaledTime + .3f;
                    while (Time.unscaledTime < settle) yield return null;
                    Require(Vector2.Distance(body.anchoredPosition, home) < .01f, "Shake did not return.");
                }
                bool complete = false;
                Require(shelf.BeginPhysicalContainerTransfer(method == "Stir" ? RecipePreparationAction.Strain :
                    RecipePreparationAction.PourFromContainer, _ => complete = true), "Working transfer rejected.");
                // A repeated click must not start another operation.
                Require(!shelf.BeginPhysicalContainerTransfer(RecipePreparationAction.Strain, _ => { }), "Concurrent transfer accepted.");
                while (shelf.PhysicalTransferActive)
                {
                    Require(Mathf.Abs(source.CurrentLiquidVolumeMl + shelf.ServingGlass.CurrentLiquidVolumeMl - 72.5f) < .003f,
                        "Working transfer lost/duplicated liquid.");
                    Require(source.HasIce && !shelf.ServingGlass.HasIce, "Liquid transfer moved/duplicated ice.");
                    yield return null;
                }
                Require(complete && source.CurrentLiquidVolumeMl == 0f && shelf.TargetContainer == shelf.ServingGlass,
                    "Transfer did not complete in the final glass.");
                if (method == "Shake")
                    Require(view.GetVessel(source).Find("OpenShakerMouth").gameObject.activeSelf,
                        "Shaker transfer must use its open mouth.");
                Require(shelf.PreparationTrace.Actions[shelf.PreparationTrace.Actions.Count - 1].Action ==
                    (method == "Stir" ? RecipePreparationAction.Strain : RecipePreparationAction.PourFromContainer),
                    "Transfer trace did not preserve action semantics.");
            }
            var garnishController = root.GetComponent<ServingGlassGarnishController>();
            garnishController.BeginSelection();
            foreach (var placement in new[] { BartendingGarnishPlacement.Rim, BartendingGarnishPlacement.InGlass, BartendingGarnishPlacement.Surface })
            {
                var previousGarnish = shelf.ServingGlass.Garnish;
                Require(garnishController.TryApplyGarnish(BartendingGarnishId.Cherry, placement), "Garnish pickup rejected.");
                Require(shelf.ServingGlass.Garnish == previousGarnish, "Garnish state changed before placement.");
                Require(!shelf.ToolsReady && !shelf.PreparePhysicalPour() && !shelf.TryAddIceToActiveTarget(),
                    "Another physical action interrupted garnish placement.");
                while (garnishController.IsPlacing) yield return null;
                Require(shelf.ServingGlass.Garnish.Placement == placement, "Placed garnish differs from state.");
                var marker = view.GetVessel(shelf.ServingGlass).Find("RuntimeGarnishMarker");
                Require(marker != null && marker.gameObject.activeInHierarchy, "Garnish state has no visible marker.");
                if (placement == BartendingGarnishPlacement.Surface)
                    Require(Vector3.Distance(marker.position, view.GetLiquidSurfaceWorld(shelf.ServingGlass)) < .1f,
                        "Surface garnish is detached from liquid surface.");
            }
            if (game is BuildMiniGame build)
            {
                build.BeginGentleStir();
                for (int step = 0; step <= 36; step++)
                {
                    // StirTechniqueTracker.RecordAngle explicitly returns false for the first reference sample.
                    bool accepted = build.TryRecordGentleStirAngle(step * 10f, .05f);
                    Require(accepted == (step > 0), "Gentle stir reference/accepted-motion contract changed.");
                    yield return null;
                }
                Require(sounds.TechniqueSource.isPlaying, "Build spoon motion had no sound.");
                build.FinishGentleStir();
                yield return new WaitForSecondsRealtime(.12f);
                Require(!sounds.TechniqueSource.isPlaying, "Gentle stir loop survived spoon removal.");
            }
            SURLHWAReviewCapture.Save("phase-k-" + method + "-finished", 1280, 720);
            SURLHWAReviewCapture.Save("phase-m-physical-" + method + "-finished", 1280, 720);
            SURLHWAReviewCapture.Save("phase-m-physical-" + method + "-finished", 1920, 1080);
            var appearance = ServedDrinkAppearance.Capture(root);
            Require(appearance != null && appearance.Garnish == shelf.ServingGlass.Garnish &&
                appearance.HasIce == shelf.ServingGlass.HasIce, "Final drawing differs from actual glass.");
            Debug.Log("[Phase K Play Mode] " + method + " physical bottle/measure/transfer PASS.");
            Destroy(root);
            yield return null;
        }
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
#endif
