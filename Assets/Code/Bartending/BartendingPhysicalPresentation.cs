using System.Collections.Generic;
using System.Collections;
using TMPro;
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
    [SerializeField, Min(.01f)] private float containerMoveSeconds = .3f;
    [SerializeField] private float containerTiltDegrees = 105f;
    [SerializeField, Min(.01f)] private float spillSeconds = .3f;
    [SerializeField] private float surfacePourHeight = 12f;
    [SerializeField, Min(.01f)] private float icePlacementSeconds = .35f;

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
    private RectTransform movingVessel;
    private Vector3 vesselHomePosition;
    private Quaternion vesselHomeRotation;
    private RectTransform spill;
    private float spillRemaining;
    private TMP_Text measurementText;
    private BartendingContainerState jiggerState;
    private BartendingContainerState surfaceContainer;
    private RectTransform liquidSurface;
    private RectTransform strainer;
    private readonly Dictionary<BartendingContainerState, RectTransform> vessels = new();
    private readonly Dictionary<BartendingContainerState, RectTransform> fills = new();
    private readonly Dictionary<BartendingContainerState, RectTransform> iceVisuals = new();

    public bool IsAtPourPose => motion == Motion.Pour;
    public static float MotionDeltaTime => RecipeGuidePanelController.IsTechniqueReadStateActive ||
        DemoAudioController.IsSettingsOpen ? 0f : Time.deltaTime;
    public bool IsResting => motion == Motion.Rest;
    public bool StreamVisible => stream != null && stream.gameObject.activeSelf;
    public RectTransform Bottle => bottle;
    public RectTransform Neck => neck;
    public RectTransform GetVessel(BartendingContainerState state) =>
        state != null && vessels.TryGetValue(state, out var vessel) ? vessel : null;
    public RectTransform GetIceVisual(BartendingContainerState state) =>
        state != null && iceVisuals.TryGetValue(state, out var ice) ? ice : null;

    public Vector3 GetLiquidSurfaceWorld(BartendingContainerState state)
    {
        if (state == surfaceContainer && liquidSurface != null) return liquidSurface.position;
        if (fills.TryGetValue(state, out var fill))
            return fill.TransformPoint(new Vector3(fill.rect.center.x, fill.rect.yMax, 0f));
        return GetVessel(state).position;
    }

    public void SetStrainer(BartendingContainerState source, bool visible)
    {
        if (!visible) { if (strainer != null) strainer.gameObject.SetActive(false); return; }
        var vessel = GetVessel(source);
        if (strainer == null) strainer = Shape("Strainer", vessel, Vector2.one, new Color(.75f, .8f, .84f, 1f));
        strainer.SetParent(vessel, false);
        strainer.anchorMin = new Vector2(.05f, .95f);
        strainer.anchorMax = new Vector2(.95f, 1f);
        strainer.offsetMin = strainer.offsetMax = Vector2.zero;
        strainer.gameObject.SetActive(true);
    }

    public IEnumerator PlaceIce(BartendingContainerState target)
    {
        var ice = Shape("HeldIce", ui, new Vector2(24f, 24f), new Color(.7f, .88f, 1f, .9f));
        Vector3 from = ui.TransformPoint(ui.rect.center + new Vector2(-290f, -150f));
        Vector3 to = GetVessel(target).TransformPoint(GetVessel(target).rect.center);
        for (float t = 0f; t < icePlacementSeconds; t += MotionDeltaTime)
        {
            float fraction = Smooth(t / icePlacementSeconds);
            ice.position = Vector3.Lerp(from, to, fraction) + ui.TransformVector(Vector3.up * (Mathf.Sin(fraction * Mathf.PI) * 55f));
            yield return null;
        }
        ice.position = to;
        Destroy(ice.gameObject);
    }

    public void BindLayerSurface(BartendingContainerState state, RectTransform surface)
    {
        surfaceContainer = state;
        liquidSurface = surface;
        if (fills.TryGetValue(state, out var fill)) fill.gameObject.SetActive(false);
    }

    public void Initialize(Transform methodRoot, BartendingPreparationState state)
    {
        ui = methodRoot.Find("UI") as RectTransform;
        if (ui == null) return;
        if (methodRoot.GetComponent<BuildMiniGame>() != null)
            methodRoot.Find("UI/LiquidArea")?.gameObject.SetActive(false);
        Register(state.GetContainer(BartendingContainerType.ServingGlass),
            methodRoot.Find("UI/GlassImage") as RectTransform, new Vector2(240f, 10f));
        Register(state.GetContainer(BartendingContainerType.MixingGlass),
            methodRoot.Find("UI/CupBack") as RectTransform, new Vector2(0f, 10f));
        Register(state.GetContainer(BartendingContainerType.Shaker),
            methodRoot.Find("UI/Shaker") as RectTransform, new Vector2(0f, 10f));
        var front = methodRoot.Find("UI/CupFront");
        if (front != null)
        {
            front.SetParent(GetVessel(state.GetContainer(BartendingContainerType.MixingGlass)), true);
            // Legacy art bakes an opaque filled drink into the image. Keep the
            // serialized asset untouched; use open placeholder walls for state fill.
            var frontImage = front.GetComponent<Image>();
            if (frontImage != null) frontImage.enabled = false;
        }
        // Show only the method's working vessel plus its final destination.
        foreach (var pair in vessels)
            pair.Value.gameObject.SetActive(pair.Key.ContainerType == BartendingContainerType.ServingGlass ||
                                            pair.Key == state.ActiveTarget);
        jiggerState = state.GetContainer(BartendingContainerType.Jigger);
        Register(jiggerState, null, new Vector2(-150f, -20f));
        var jiggerVisual = GetVessel(jiggerState);
        jiggerVisual.sizeDelta = new Vector2(55f, 70f);
        var labelObject = new GameObject("Measurement", typeof(RectTransform), typeof(TextMeshProUGUI));
        measurementText = labelObject.GetComponent<TextMeshProUGUI>();
        measurementText.rectTransform.SetParent(ui, false);
        measurementText.rectTransform.sizeDelta = new Vector2(220f, 55f);
        measurementText.rectTransform.anchoredPosition = new Vector2(-150f, -100f);
        measurementText.fontSize = 16f;
        measurementText.alignment = TextAlignmentOptions.Center;
        measurementText.raycastTarget = false;
        spill = Shape("JiggerOverflow", jiggerVisual, new Vector2(4f, 65f), new Color(.85f, .78f, .55f, .9f));
        spill.anchoredPosition = new Vector2(30f, -10f);
        spill.gameObject.SetActive(false);
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
        var body = vessel.GetComponent<Image>();
        if (body != null)
        {
            body.sprite = null;
            body.color = new Color(.22f, .32f, .36f, .3f);
        }
        for (int side = 0; side < 3; side++)
        {
            var wall = Shape("VesselWall" + side, vessel, Vector2.zero, new Color(.7f, .8f, .84f, .9f));
            wall.anchorMin = side == 0 ? new Vector2(.04f, .04f) : side == 1 ? new Vector2(.94f, .04f) : new Vector2(.04f, .04f);
            wall.anchorMax = side == 0 ? new Vector2(.06f, .98f) : side == 1 ? new Vector2(.96f, .98f) : new Vector2(.96f, .06f);
            wall.offsetMin = wall.offsetMax = Vector2.zero;
        }
        var fill = Shape("StateLiquid", vessel, Vector2.zero, new Color(.7f, .48f, .22f, .65f));
        fill.anchorMin = new Vector2(.12f, .08f);
        fill.anchorMax = new Vector2(.88f, .08f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        fills.Add(state, fill);
        var iceRoot = new GameObject("StateIce", typeof(RectTransform)).GetComponent<RectTransform>();
        iceRoot.SetParent(vessel, false);
        iceRoot.anchorMin = Vector2.zero;
        iceRoot.anchorMax = Vector2.one;
        iceRoot.offsetMin = iceRoot.offsetMax = Vector2.zero;
        for (int i = 0; i < 3; i++)
        {
            var cube = Shape("IceCube" + i, iceRoot, Vector2.zero, new Color(.7f, .88f, 1f, .85f));
            cube.anchorMin = new Vector2(.23f + i * .15f, .3f + (i % 2) * .12f);
            cube.anchorMax = cube.anchorMin + new Vector2(.18f, .16f);
            cube.offsetMin = cube.offsetMax = Vector2.zero;
        }
        iceRoot.gameObject.SetActive(state.HasIce);
        iceVisuals.Add(state, iceRoot);
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

    // Keep a short, already accepted discrete dose readable without adding ml.
    public void ShowDoseStream()
    {
        if (!IsAtPourPose) return;
        acceptedFrame = Time.frameCount;
        DrawStream();
    }

    public void EndBottle()
    {
        if (stream != null) stream.gameObject.SetActive(false);
        if (motion == Motion.Rest || motion == Motion.Return || motion == Motion.Upright) return;
        Enter(Motion.Upright);
    }

    public void ShowMeasurement(bool measuring)
    {
        if (measurementText != null && jiggerState != null)
            measurementText.text = $"Jigger {jiggerState.CurrentLiquidVolumeMl:0.0} / {jiggerState.CapacityMl:0} ml\nM: {(measuring ? "Direct" : "Measure")} · T: Transfer";
    }

    public void ObserveSpill(float lostMl)
    {
        if (lostMl <= 0f || spill == null) return;
        spillRemaining = spillSeconds;
        spill.gameObject.SetActive(true);
        // Incoming stream continues over the finite rim, even when accepted ml is zero.
        if (IsAtPourPose) { acceptedFrame = Time.frameCount; DrawStream(); }
    }

    public IEnumerator MoveContainerToPour(BartendingContainerState source, BartendingContainerState target)
    {
        movingVessel = GetVessel(source);
        destination = target;
        if (movingVessel == null) yield break;
        vesselHomePosition = movingVessel.position;
        vesselHomeRotation = movingVessel.rotation;
        Vector3 rim = TargetWorldRim(target) + ui.TransformVector(Vector3.up * CurrentPourHeight);
        Vector3 lip = new Vector3(0f, movingVessel.rect.yMax, 0f);
        Vector3 start = movingVessel.position;
        Vector3 upright = rim - (movingVessel.TransformPoint(lip) - movingVessel.position);
        for (float t = 0f; t < containerMoveSeconds; t += MotionDeltaTime)
        {
            movingVessel.position = Vector3.Lerp(start, upright, Smooth(t / containerMoveSeconds));
            yield return null;
        }
        for (float t = 0f; t < tiltSeconds; t += MotionDeltaTime)
        {
            PoseContainer(rim, Smooth(t / tiltSeconds));
            yield return null;
        }
        PoseContainer(rim, 1f);
    }

    private Vector3 TargetWorldRim(BartendingContainerState target)
    {
        if (target == surfaceContainer && liquidSurface != null) return liquidSurface.position;
        var vessel = GetVessel(target);
        return vessel.TransformPoint(new Vector3(0f, vessel.rect.yMax, 0f));
    }

    private void PoseContainer(Vector3 rim, float tilt)
    {
        movingVessel.rotation = vesselHomeRotation * Quaternion.Euler(0f, 0f, containerTiltDegrees * tilt);
        movingVessel.position += rim - movingVessel.TransformPoint(new Vector3(0f, movingVessel.rect.yMax, 0f));
    }

    public void ObserveContainerTransfer(float acceptedMl)
    {
        RefreshState();
        if (movingVessel == null || acceptedMl <= 0f) { stream.gameObject.SetActive(false); return; }
        PoseContainer(TargetWorldRim(destination) + ui.TransformVector(Vector3.up * CurrentPourHeight), 1f);
        Vector3 lip = movingVessel.TransformPoint(new Vector3(0f, movingVessel.rect.yMax, 0f));
        DrawStreamBetween(lip, TargetWorldRim(destination));
    }

    public IEnumerator ReturnContainer()
    {
        stream.gameObject.SetActive(false);
        if (movingVessel == null) yield break;
        Vector3 rim = movingVessel.TransformPoint(new Vector3(0f, movingVessel.rect.yMax, 0f));
        for (float t = 0f; t < tiltSeconds; t += MotionDeltaTime)
        {
            PoseContainer(rim, 1f - Smooth(t / tiltSeconds));
            yield return null;
        }
        PoseContainer(rim, 0f);
        Vector3 start = movingVessel.position;
        for (float t = 0f; t < returnSeconds; t += MotionDeltaTime)
        {
            movingVessel.position = Vector3.Lerp(start, vesselHomePosition, Smooth(t / returnSeconds));
            yield return null;
        }
        movingVessel.position = vesselHomePosition;
        movingVessel.rotation = vesselHomeRotation;
        movingVessel = null;
        destination = null;
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
            ? (Vector2)ui.InverseTransformPoint(TargetWorldRim(destination)) - ui.rect.center
            : Vector2.zero;
    }

    private float CurrentPourHeight => destination == surfaceContainer && liquidSurface != null ? surfacePourHeight : pourHeight;

    // Align the neck in a common coordinate space; tilt about the held grip.
    private void AlignNeck(float degrees)
    {
        bottle.localRotation = Quaternion.Euler(0f, 0f, degrees);
        Vector2 neckOffset = (Vector2)(bottle.localRotation * new Vector3(0f, 81f, 0f));
        bottle.anchoredPosition = TargetPoint() + Vector2.up * CurrentPourHeight - neckOffset;
    }

    public void TickMotion(float deltaTime)
    {
        if (bottle == null || deltaTime <= 0f) return;
        elapsed += deltaTime;
        switch (motion)
        {
            case Motion.Approach:
                bottle.anchoredPosition = Vector2.Lerp(motionStart,
                    TargetPoint() + Vector2.up * (CurrentPourHeight - 81f), Smooth(elapsed / approachSeconds));
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
        spillRemaining -= Time.deltaTime;
        if (spill != null && spillRemaining <= 0f) spill.gameObject.SetActive(false);
        if (movingVessel != null) return;
        if (acceptedFrame == Time.frameCount && IsAtPourPose) DrawStream();
        else if (stream != null) stream.gameObject.SetActive(false);
    }

    public void RefreshState()
    {
        foreach (var pair in iceVisuals) pair.Value.gameObject.SetActive(pair.Key.HasIce);
        foreach (var pair in fills)
        {
            if (pair.Key == surfaceContainer) continue;
            float scale = pair.Key.CapacityMl > 0f ? pair.Key.CapacityMl : Mathf.Max(1f, displayVolumeMl);
            float level = Mathf.Clamp01(pair.Key.CurrentLiquidVolumeMl / scale);
            pair.Value.anchorMax = new Vector2(.88f, .08f + .8f * level);
            pair.Value.gameObject.SetActive(level > 0f);
        }
    }

    private void DrawStream()
    {
        if (stream == null) return;
        DrawStreamBetween(neck.position, TargetWorldRim(destination));
    }

    private void DrawStreamBetween(Vector3 worldFrom, Vector3 worldTo)
    {
        Vector2 from = (Vector2)ui.InverseTransformPoint(worldFrom) - ui.rect.center;
        Vector2 to = (Vector2)ui.InverseTransformPoint(worldTo) - ui.rect.center;
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
