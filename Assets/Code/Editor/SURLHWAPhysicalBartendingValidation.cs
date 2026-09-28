#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public static class SURLHWAPhysicalBartendingValidation
{
    public static void RunRegression()
    {
        Run();
        SURLHWACampaignValidation.RunIntegrated();
    }
    [MenuItem("SURLHWA/Validation/Physical Bartending")]
    public static void Run()
    {
        Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll();
        ValidateMeasurement();
        ValidateSpoonOrbit();
        foreach (string method in new[] { "Build", "Stir", "Shake", "Float" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_SURLHWA/Prefabs/Bartending/" + method + "MiniGame.prefab");
            Require(prefab != null, "Missing production prefab " + method);
            var root = UnityEngine.Object.Instantiate(prefab);
            var ingredient = ScriptableObject.CreateInstance<IngredientData>();
            try
            {
                var state = new BartendingPreparationState(method == "Stir" ? BartendingContainerType.MixingGlass :
                    method == "Shake" ? BartendingContainerType.Shaker : BartendingContainerType.ServingGlass);
                var view = root.AddComponent<BartendingPhysicalPresentation>();
                view.Initialize(root.transform, state);
                Require(!view.PrepareBottle(ingredient, state.ActiveTarget), "Cannot pour before taking bottle.");
                Require(state.ActiveTarget.CurrentLiquidVolumeMl == 0f, "Presentation created liquid.");
                for (int i = 0; i < 100; i++) view.TickMotion(.01f);
                Require(view.IsAtPourPose && !view.StreamVisible, "Pose/stream must precede accepted liquid independently.");
                RectTransform vessel = view.GetVessel(state.ActiveTarget);
                Vector3 rim = vessel.TransformPoint(new Vector3(0f, vessel.rect.yMax, 0f));
                Require(Mathf.Abs(view.Neck.position.x - rim.x) < .05f, "Bottle neck is not aligned with vessel rim.");
                Require(view.Neck.position.y > rim.y, "Neck must be above target.");
                float accepted = state.ActiveTarget.AddLiquid(ingredient, 12.5f);
                view.ObserveAcceptedPour(accepted);
                Require(view.StreamVisible && state.ActiveTarget.CurrentLiquidVolumeMl == 12.5f, "Stream/state correspondence.");
                view.EndBottle();
                Require(!view.StreamVisible, "Release must stop stream immediately.");
                for (int i = 0; i < 100; i++) view.TickMotion(.01f);
                Require(view.IsResting && Mathf.Abs(Mathf.DeltaAngle(0f, view.Bottle.localEulerAngles.z)) < .05f,
                    "Bottle must return upright and release.");
                Require(state.ActiveTarget.CurrentLiquidVolumeMl == 12.5f, "Return changed authoritative volume.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(ingredient);
            }
        }
        Debug.Log("[Phase K Physical] PASS: four actual prefab hierarchies, neck alignment, pickup/tilt/return, state isolation and stream gating.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("[Phase K Physical] " + message);
    }

    private static void ValidateSpoonOrbit()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_SURLHWA/Prefabs/Bartending/StirMiniGame.prefab");
        var root = UnityEngine.Object.Instantiate(prefab);
        try
        {
            var stir = root.GetComponent<StirMiniGame>();
            stir.StartGame(_ => { });
            var spoon = root.transform.Find("UI/Spoon") as RectTransform;
            var cup = root.transform.Find("UI/CupBack") as RectTransform;
            var pose = typeof(StirMiniGame).GetMethod("UpdateSpoonPose",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Vector3 first = default;
            for (int angle = 0; angle <= 360; angle += 30)
            {
                pose.Invoke(stir, new object[] { (float)angle });
                Vector3 tipInCup = cup.InverseTransformPoint(spoon.position);
                Require(cup.rect.Contains(tipInCup), "Stir spoon tip left the vessel.");
                Require(Mathf.Abs(Mathf.DeltaAngle(90f, spoon.localEulerAngles.z)) < 20f,
                    "Spoon windmill regression: shaft made a full rotation.");
                if (angle == 0) first = spoon.position;
                if (angle == 180) Require(Vector3.Distance(first, spoon.position) > 20f, "Spoon did not orbit.");
                if (angle == 360) Require(Vector3.Distance(first, spoon.position) < .01f, "Orbit does not close.");
            }
            var gentle = new ServingGlassGentleStirInteraction();
            gentle.Begin(cup, root.transform.Find("UI"));
            for (int i = 0; i <= 180; i++) gentle.TryRecordAngle(i * 4f, 1f / 60f);
            var gentleSpoon = cup.Find("RuntimeGentleStirSpoon");
            Require(Mathf.Abs(Mathf.DeltaAngle(0f, gentleSpoon.localEulerAngles.z)) < 20f,
                "GentleStir shaft made a full rotation.");
            Require(gentle.Revolutions > 1.9f, "Presentation changed technique rotation.");
            gentle.End();
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        Debug.Log("[Phase K Stir] PASS: real scaled prefab, tip inside vessel, closed orbit, stable shaft and unchanged technique authority.");
    }

    private static void ValidateMeasurement()
    {
        var ingredient = ScriptableObject.CreateInstance<IngredientData>();
        try
        {
            foreach (float capacity in new[] { 30f, 60f, 75f })
            {
                var preparation = new BartendingPreparationState(BartendingContainerType.MixingGlass, capacity);
                var source = preparation.GetContainer(BartendingContainerType.Jigger);
                var target = preparation.ActiveTarget;
                float received = source.AddLiquid(ingredient, capacity + 12.25f);
                Require(Mathf.Approximately(received, capacity) && source.CurrentLiquidVolumeMl == capacity,
                    "Finite jigger must cap intake and report accepted amount.");
                Require(source.AddLiquid(ingredient, 100f) == 0f, "Full jigger must reject excess.");
                float moved = 0f;
                while (source.CurrentLiquidVolumeMl > 0f)
                    moved += source.TransferLiquidTo(target, 7.25f);
                Require(Mathf.Abs(moved - capacity) < .001f && Mathf.Abs(target.CurrentLiquidVolumeMl - capacity) < .001f,
                    "Measured transfer duplicated/lost liquid.");
                Require(source.CurrentLiquidVolumeMl == 0f, "Jigger did not empty.");
                source.AddLiquid(ingredient, 10f);
                source.TransferLiquidTo(target, 10f);
                Require(Mathf.Abs(target.CurrentLiquidVolumeMl - capacity - 10f) < .001f, "Repeated measurement failed.");
            }
            var trace = new BartendingPreparationTrace();
            trace.RecordMeasuredPour(ingredient, 60f, BartendingContainerType.MixingGlass);
            trace.RecordMeasuredPour(ingredient, 15f, BartendingContainerType.MixingGlass);
            Require(trace.Actions.Count == 1 && trace.Actions[0].ActualMl == 75f,
                "Repeated measurement must remain one authored Pour.");
        }
        finally { UnityEngine.Object.DestroyImmediate(ingredient); }
        Debug.Log("[Phase K Measurement] PASS: varied finite capacities, overflow rejection, partial emptying, repeat and trace.");
    }
}
#endif
