#if UNITY_EDITOR
using System;
using System.Collections;
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
            EditorApplication.Exit(code);
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
        foreach (string method in new[] { "Build", "Stir", "Shake" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_SURLHWA/Prefabs/Bartending/" + method + "MiniGame.prefab");
            var root = Instantiate(prefab);
            var game = root.GetComponent<IMiniGame>();
            // Drive the production input seams explicitly, without competing
            // keyboard polling from an unattended Editor mouse/keyboard.
            ((MonoBehaviour)game).enabled = false;
            game.StartGame(_ => { });
            var shelf = root.GetComponentInChildren<IngredientShelfController>(true);
            var ingredient = shelf.ConfiguredIngredients[0];
            foreach (var candidate in shelf.ConfiguredIngredients)
                if (shelf.TrySelectIngredient(candidate)) { ingredient = candidate; break; }
            Require(shelf.SelectedIngredient != null, "No supplied selectable ingredient.");
            var view = shelf.PhysicalPresentation;
            Require(view != null, "Production route did not install physical view.");
            float original = shelf.TargetContainer.CurrentLiquidVolumeMl;
            while (!shelf.PreparePhysicalPour())
            {
                Require(shelf.TargetContainer.CurrentLiquidVolumeMl == original, "Liquid moved during bottle alignment.");
                yield return null;
            }
            Require(shelf.PourSelectedIngredient(12.5f) == 12.5f && view.StreamVisible, "Accepted bottle stream missing.");
            shelf.EndSelectedPour();
            Require(!view.StreamVisible, "Release continued pouring.");
            while (!view.IsResting) yield return null;
            while (!view.PrepareBottle(ingredient, shelf.Jigger)) yield return null;
            float measured = shelf.FillJigger(72f);
            Require(measured == 60f && shelf.Jigger.CurrentLiquidVolumeMl == 60f, "Overflow accounting wrong.");
            Require(shelf.TargetContainer.CurrentLiquidVolumeMl == 12.5f, "Measuring changed destination.");
            shelf.PhysicalPresentation.EndBottle();
            while (!view.IsResting) yield return null;
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
                if (source > .01f && source < 59.99f) observedPartial = true;
                previous = source;
                yield return null;
            }
            Require(observedPartial && shelf.Jigger.CurrentLiquidVolumeMl == 0f, "Transfer must visibly empty over frames.");
            Require(shelf.ToolsReady, "Tools remained locked after return.");
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
                Require(shelf.PreparationTrace.Actions[shelf.PreparationTrace.Actions.Count - 1].Action == RecipePreparationAction.TopUp,
                    "TopUp changed into an ordinary Pour.");
            }
            if (method != "Build")
            {
                var source = shelf.TargetContainer;
                source.TryAddIce();
                if (method == "Shake")
                {
                    var shake = (ShakeMiniGame)game;
                    var body = view.GetVessel(source);
                    Vector2 home = body.anchoredPosition;
                    float beforeShakeMl = source.CurrentLiquidVolumeMl;
                    shake.BeginShake();
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
                Require(shelf.PreparationTrace.Actions[shelf.PreparationTrace.Actions.Count - 1].Action ==
                    (method == "Stir" ? RecipePreparationAction.Strain : RecipePreparationAction.PourFromContainer),
                    "Transfer trace did not preserve action semantics.");
            }
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
