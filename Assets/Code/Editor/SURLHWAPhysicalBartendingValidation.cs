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
        ValidateMixedTransfer();
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
                view.PrepareBottle(ingredient, state.ActiveTarget);
                view.TickMotion(.04f);
                Vector3 interrupted = view.Bottle.position;
                view.EndBottle();
                Require(Vector3.Distance(interrupted, view.Bottle.position) < .001f,
                    "Releasing pickup teleported the bottle.");
                view.TickMotion(.001f);
                Require(Vector3.Distance(interrupted, view.Bottle.position) < 1f,
                    "Interrupted pickup jumped to the target before returning.");
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
            var shelf = root.GetComponentInChildren<IngredientShelfController>(true);
            shelf.TargetContainer.AddLiquid(shelf.ConfiguredIngredients[0], 20f);
            shelf.TargetContainer.TryAddIce();
            stir.BeginStir();
            var follow = typeof(StirMiniGame).GetMethod("UpdateSpoonVisual",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Vector3 forward = default, reference = default;
            foreach (float angle in new[] { 0f, 60f, 30f })
            {
                stir.TryRecordStirAngle(angle, .1f);
                for (int frame = 0; frame < 20; frame++) follow.Invoke(stir, new object[] { .02f });
                if (angle == 0f) reference = spoon.position;
                if (angle == 60f) forward = spoon.position;
            }
            Vector3 reversed = spoon.position;
            pose.Invoke(stir, new object[] { 30f });
            Require(Vector3.Distance(reversed, spoon.position) < .3f &&
                Vector3.Distance(reversed, reference) < Vector3.Distance(forward, reference),
                "Reversed input did not reverse the visible tip orbit.");
            Require(Mathf.Abs(stir.TotalStirRotationDegrees - 90f) < .001f,
                "Visual following changed the performed 60 + 30 degree rotation.");
            Require(!stir.TryRecordStirAngle(240f, .1f), "Cursor teleport accepted.");
            for (int frame = 0; frame < 20; frame++) follow.Invoke(stir, new object[] { .02f });
            Require(Vector3.Distance(reversed, spoon.position) < .3f &&
                Mathf.Abs(stir.TotalStirRotationDegrees - 90f) < .001f,
                "Rejected cursor jump moved the spoon or created gameplay rotation.");
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
        Debug.Log("[Phase K Stir] PASS: real scaled prefab, tip inside vessel, closed orbit, reverse input, rejected jumps, stable shaft and unchanged technique authority.");
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

    private static void ValidateMixedTransfer()
    {
        var first = ScriptableObject.CreateInstance<IngredientData>();
        var second = ScriptableObject.CreateInstance<IngredientData>();
        try
        {
            var source = new BartendingContainerState(BartendingContainerType.MixingGlass);
            var bounded = new BartendingContainerState(BartendingContainerType.Jigger, 25f);
            source.AddLiquid(first, 61f);
            source.AddLiquid(second, 29f);
            source.TryAddDash(second, 1f, out _);
            source.TryAddIce();
            Require(source.TransferLiquidTo(bounded, 50f) == 25f, "Destination capacity ignored.");
            Require(source.TransferLiquidTo(bounded, 1f) == 0f, "Full target accepted more liquid.");
            Require(Mathf.Abs(source.GetLiquidAmountMl(first) + bounded.GetLiquidAmountMl(first) - 61f) < .001f &&
                Mathf.Abs(source.GetLiquidAmountMl(second) + bounded.GetLiquidAmountMl(second) - 30f) < .001f,
                "Partial mixed transfer changed ingredient identity/quantity.");
            var serving = new BartendingContainerState(BartendingContainerType.ServingGlass);
            bounded.TransferLiquidTo(serving, 100f);
            for (int i = 0; source.CurrentLiquidVolumeMl > 0f && i < 100; i++) source.TransferLiquidTo(serving, 3.7f);
            Require(source.CurrentLiquidVolumeMl == 0f && Mathf.Abs(serving.CurrentLiquidVolumeMl - 91f) < .001f,
                "Repeated mixed transfer failed conservation/emptying.");
            Require(serving.GetDashCount(second) == 1 && source.GetDashCount(second) == 0,
                "Dash evidence duplicated or lost during progressive drain.");
            Require(source.HasIce && !serving.HasIce, "Liquid transfer changed ice ownership.");
            Require(source.TransferLiquidTo(serving, 1f) == 0f, "Empty source transferred twice.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(first);
            UnityEngine.Object.DestroyImmediate(second);
        }
        Debug.Log("[Phase K Mixed Transfer] PASS: capacity, per-ingredient conservation, final dash evidence and retained ice.");
    }
}
#endif
