#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public static class SURLHWAPhysicalBartendingValidation
{
    [MenuItem("SURLHWA/Validation/Physical Bartending")]
    public static void Run()
    {
        Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll();
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
}
#endif
