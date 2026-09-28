using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Replaceable first-pass UI tool choreography. Owns transforms only, never ml,
/// ingredients, preparation evidence, recipe recognition or technique scores.
/// The shelf admits gameplay transfers only after the tool reaches its pose.
/// </summary>
public sealed class BartendingPhysicalPresentation : MonoBehaviour
{
    [Header("HUMAN_TUNING_LATER")]
    [SerializeField, Min(0.01f)] private float approachSeconds = 0.25f;
    [SerializeField, Min(0.01f)] private float tiltSeconds = 0.18f;
    [SerializeField, Min(0.01f)] private float returnSeconds = 0.25f;
    [SerializeField] private float bottleTiltDegrees = 115f;
    [SerializeField] private float pourHeight = 32f;
    [SerializeField, Min(1f)] private float streamWidth = 4f;
    [SerializeField, Min(1f)] private float displayVolumeMl = 240f;
    [SerializeField] private Vector2 bottleHome = new Vector2(-250f, 140f);

    private enum Motion { Rest, Approach, Tilt, Pour, Upright, Return }
    private Motion motion;
    private float elapsed;
    private Vector2 motionStart;
    private float startAngle;
    private RectTransform ui;
    private RectTransform bottle;
    private RectTransform neck;
    private RectTransform stream;
    private IngredientData ingredient;
    private BartendingContainerState destination;
    private int acceptedFrame = -1;
    private readonly Dictionary<BartendingContainerState, RectTransform> vessels = new();
    private readonly Dictionary<BartendingContainerState, RectTransform> fills = new();

    public bool IsAtPourPose => motion == Motion.Pour;
    public bool IsResting => motion == Motion.Rest;
    public bool StreamVisible => stream != null && stream.gameObject.activeSelf;
    public RectTransform Bottle => bottle;
    public RectTransform Neck => neck;
    public RectTransform GetVessel(BartendingContainerState state) =>
        state != null && vessels.TryGetValue(state, out var vessel) ? vessel : null;

    public void Initialize(Transform methodRoot, BartendingPreparationState state)
    {
        ui = methodRoot.Find("UI") as RectTransform;
        if (ui == null) return;
        Register(state.GetContainer(BartendingContainerType.ServingGlass),
            methodRoot.Find("UI/GlassImage") as RectTransform, new Vector2(240f, 10f));
        Register(state.GetContainer(BartendingContainerType.MixingGlass),
            methodRoot.Find("UI/CupBack") as RectTransform, new Vector2(0f, 10f));
        Register(state.GetContainer(BartendingContainerType.Shaker),
            methodRoot.Find("UI/Shaker") as RectTransform, new Vector2(0f, 10f));
        // Show only the method's working vessel plus its final destination.
        foreach (var pair in vessels)
            pair.Value.gameObject.SetActive(pair.Key.ContainerType == BartendingContainerType.ServingGlass ||
                                            pair.Key == state.ActiveTarget);
        bottle = Shape("PhysicalBottleGrip", ui, new Vector2(48f, 108f), new Color(.2f, .42f, .32f, 1f));
        bottle.anchoredPosition = bottleHome;
        var top = Shape("BottleNeck", bottle, new Vector2(16f, 30f), new Color(.3f, .52f, .4f, 1f));
        top.anchoredPosition = new Vector2(0f, 66f);
        neck = Shape("PourOrigin", bottle, Vector2.one, Color.clear);
        neck.anchoredPosition = new Vector2(0f, 81f);
        stream = Shape("LiquidStream", ui, Vector2.one, new Color(.85f, .78f, .55f, .9f));
        stream.pivot = new Vector2(.5f, 0f);
        stream.gameObject.SetActive(false);
        bottle.gameObject.SetActive(false);
        RefreshState();
    }

    public void Register(BartendingContainerState state, RectTransform vessel, Vector2 fallbackPosition)
    {
        if (state == null || ui == null || vessels.ContainsKey(state)) return;
        if (vessel == null)
        {
            vessel = Shape("Physical" + state.ContainerType, ui, new Vector2(115f, 155f), new Color(.28f, .36f, .4f, .65f));
            vessel.anchoredPosition = fallbackPosition;
        }
        vessels.Add(state, vessel);
        var fill = Shape("StateLiquid", vessel, Vector2.zero, new Color(.7f, .48f, .22f, .65f));
        fill.anchorMin = new Vector2(.12f, .08f);
        fill.anchorMax = new Vector2(.88f, .08f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        fills.Add(state, fill);
    }

    public bool PrepareBottle(IngredientData source, BartendingContainerState target)
    {
        if (source == null || GetVessel(target) == null || bottle == null) return false;
        if (motion == Motion.Upright || motion == Motion.Return) return false;
        if (motion != Motion.Rest && (ingredient != source || destination != target))
        {
            EndBottle();
            return false;
        }
        if (motion == Motion.Rest)
        {
            ingredient = source;
            destination = target;
            bottle.gameObject.SetActive(true);
            Enter(Motion.Approach);
        }
        return IsAtPourPose;
    }

    public void ObserveAcceptedPour(float acceptedMl)
    {
        if (!IsAtPourPose || acceptedMl <= 0f) return;
        acceptedFrame = Time.frameCount;
        RefreshState();
        DrawStream();
    }

    public void EndBottle()
    {
        if (stream != null) stream.gameObject.SetActive(false);
        if (motion == Motion.Rest || motion == Motion.Return || motion == Motion.Upright) return;
        Enter(Motion.Upright);
    }

    private void Enter(Motion next)
    {
        motion = next;
        elapsed = 0f;
        motionStart = bottle.anchoredPosition;
        startAngle = Mathf.DeltaAngle(0f, bottle.localEulerAngles.z);
    }

    private Vector2 TargetPoint()
    {
        var vessel = GetVessel(destination);
        return vessel != null
            ? (Vector2)ui.InverseTransformPoint(vessel.TransformPoint(new Vector3(0f, vessel.rect.yMax, 0f))) - ui.rect.center
            : Vector2.zero;
    }

    // Align the neck in a common coordinate space; tilt about the held grip.
    private void AlignNeck(float degrees)
    {
        bottle.localRotation = Quaternion.Euler(0f, 0f, degrees);
        Vector2 neckOffset = (Vector2)(bottle.localRotation * new Vector3(0f, 81f, 0f));
        bottle.anchoredPosition = TargetPoint() + Vector2.up * pourHeight - neckOffset;
    }

    public void TickMotion(float deltaTime)
    {
        if (bottle == null || deltaTime <= 0f) return;
        elapsed += deltaTime;
        switch (motion)
        {
            case Motion.Approach:
                bottle.anchoredPosition = Vector2.Lerp(motionStart,
                    TargetPoint() + Vector2.up * (pourHeight - 81f), Smooth(elapsed / approachSeconds));
                if (elapsed >= approachSeconds) Enter(Motion.Tilt);
                break;
            case Motion.Tilt:
                AlignNeck(Mathf.Lerp(0f, bottleTiltDegrees, Smooth(elapsed / tiltSeconds)));
                if (elapsed >= tiltSeconds) Enter(Motion.Pour);
                break;
            case Motion.Pour:
                AlignNeck(bottleTiltDegrees);
                break;
            case Motion.Upright:
                AlignNeck(Mathf.Lerp(startAngle, 0f, Smooth(elapsed / tiltSeconds)));
                if (elapsed >= tiltSeconds) Enter(Motion.Return);
                break;
            case Motion.Return:
                bottle.anchoredPosition = Vector2.Lerp(motionStart, bottleHome, Smooth(elapsed / returnSeconds));
                if (elapsed >= returnSeconds)
                {
                    motion = Motion.Rest;
                    bottle.gameObject.SetActive(false);
                    ingredient = null;
                    destination = null;
                }
                break;
        }
    }

    private void LateUpdate()
    {
        if (RecipeGuidePanelController.IsTechniqueReadStateActive || DemoAudioController.IsSettingsOpen)
        {
            stream?.gameObject.SetActive(false);
            return;
        }
        TickMotion(Time.deltaTime);
        RefreshState();
        if (acceptedFrame == Time.frameCount && IsAtPourPose) DrawStream();
        else if (stream != null) stream.gameObject.SetActive(false);
    }

    public void RefreshState()
    {
        foreach (var pair in fills)
        {
            float scale = pair.Key.CapacityMl > 0f ? pair.Key.CapacityMl : Mathf.Max(1f, displayVolumeMl);
            float level = Mathf.Clamp01(pair.Key.CurrentLiquidVolumeMl / scale);
            pair.Value.anchorMax = new Vector2(.88f, .08f + .8f * level);
            pair.Value.gameObject.SetActive(level > 0f);
        }
    }

    private void DrawStream()
    {
        if (stream == null) return;
        Vector2 from = (Vector2)ui.InverseTransformPoint(neck.position) - ui.rect.center;
        Vector2 to = TargetPoint();
        Vector2 delta = to - from;
        stream.anchoredPosition = from;
        stream.sizeDelta = new Vector2(streamWidth, delta.magnitude);
        stream.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f);
        stream.gameObject.SetActive(true);
    }

    private static float Smooth(float value) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value));
    public static RectTransform Shape(string name, Transform parent, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private void OnDisable()
    {
        if (stream != null) stream.gameObject.SetActive(false);
    }
}
