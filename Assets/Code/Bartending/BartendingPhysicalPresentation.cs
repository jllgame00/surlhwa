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
    private RectTransform bottleAccent;
    private RectTransform droplet;
    private Color transferColor;
    private Sprite shakerClosedSprite;
    private Sprite shakerOpenSprite;
    private RectTransform shakerShell;
    private RectTransform shakerMouth;
    private bool shakerClosed = true;
    private const float ShakerBodyFraction = .6f;
    private readonly Dictionary<BartendingContainerState, RectTransform> masks = new();
    private readonly Dictionary<BartendingContainerState, Vector2> fillRanges = new();
    private readonly Dictionary<BartendingContainerState, RectTransform> shells = new();
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
    public RectTransform GetLiquidMask(BartendingContainerState state) => masks[state];

    public bool TryGetLiquidBounds(RectTransform vessel, out float bottom, out float surface)
    {
        foreach (var pair in vessels)
            if (pair.Value == vessel && fills.TryGetValue(pair.Key, out var fill))
            {
                bottom = vessel.InverseTransformPoint(fill.TransformPoint(new Vector3(0f, fill.rect.yMin, 0f))).y;
                surface = vessel.InverseTransformPoint(GetLiquidSurfaceWorld(pair.Key)).y;
                return true;
            }
        bottom = surface = 0f;
        return false;
    }

    // View-only lid state, driven by existing preparation/technique transitions.
    // The open cup is a crop of the approved sprite, never a replacement asset.
    public void SetShakerClosed(bool closed)
    {
        if (shakerClosedSprite == null || shakerShell == null) return;
        shakerClosed = closed;
        if (!closed && shakerOpenSprite == null)
        {
            Rect crop = shakerClosedSprite.rect;
            crop.height = Mathf.Floor(crop.height * ShakerBodyFraction);
            shakerOpenSprite = Sprite.Create(shakerClosedSprite.texture, crop, new Vector2(.5f, .5f),
                shakerClosedSprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            shakerOpenSprite.name = "PreservedShakerOpenCup";
        }
        shakerShell.GetComponent<Image>().sprite = closed ? shakerClosedSprite : shakerOpenSprite;
        shakerShell.anchorMax = new Vector2(1f, closed ? 1f : ShakerBodyFraction);
        shakerMouth.gameObject.SetActive(!closed);
    }

    private Vector3 MouthLocal(RectTransform vessel) => new Vector3(0f,
        Mathf.Lerp(vessel.rect.yMin, vessel.rect.yMax,
            shakerShell != null && shakerShell.parent == vessel && !shakerClosed ? ShakerBodyFraction : 1f), 0f);

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
        if (source.ContainerType == BartendingContainerType.Shaker) SetShakerClosed(false);
        if (strainer == null) strainer = Shape("Strainer", vessel, Vector2.one, Color.white);
        strainer.SetParent(vessel, false);
        PixelBartendingArt.Apply(strainer, "Strainer");
        strainer.anchorMin = strainer.anchorMax = new Vector2(.5f,
            source.ContainerType == BartendingContainerType.Shaker ? ShakerBodyFraction : 1f);
        strainer.pivot = new Vector2(.5f, .8f);
        strainer.sizeDelta = new Vector2(88f, 128f);
        strainer.anchoredPosition = Vector2.zero;
        strainer.gameObject.SetActive(true);
    }

    public IEnumerator PlaceIce(BartendingContainerState target)
    {
        var ice = Shape("HeldIce", ui, new Vector2(24f, 24f), new Color(.7f, .88f, 1f, .9f));
        PixelBartendingArt.Apply(ice, "IceCluster");
        PixelBartendingArt.Grip(ice, new Vector2(18f, 8f), .8f);
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
        jiggerVisual.sizeDelta = new Vector2(56f, 96f);
        var labelObject = new GameObject("Measurement", typeof(RectTransform), typeof(TextMeshProUGUI));
        measurementText = labelObject.GetComponent<TextMeshProUGUI>();
        measurementText.rectTransform.SetParent(ui, false);
        measurementText.rectTransform.sizeDelta = new Vector2(220f, 55f);
        measurementText.rectTransform.anchoredPosition = new Vector2(-150f, -100f);
        measurementText.fontSize = 16f;
        measurementText.alignment = TextAlignmentOptions.Center;
        measurementText.raycastTarget = false;
        spill = Shape("JiggerOverflow", jiggerVisual, new Vector2(4f, 65f), new Color(.85f, .78f, .55f, .9f));
        PixelBartendingArt.Apply(spill, "OverflowStream");
        spill.anchoredPosition = new Vector2(25f, 8f);
        droplet = Shape("SmallDroplet", spill, new Vector2(4f, 6f), Color.white);
        PixelBartendingArt.Apply(droplet, "SmallDroplet");
        droplet.anchoredPosition = new Vector2(0f, -38f);
        spill.gameObject.SetActive(false);
        bottle = Shape("PhysicalBottleGrip", ui, new Vector2(64f, 224f), Color.white);
        PixelBartendingArt.Apply(bottle, "BottleTall");
        bottle.anchoredPosition = bottleHome;
        bottleAccent = Shape("IngredientAccent", bottle, new Vector2(40f, 8f), Color.white);
        bottleAccent.anchoredPosition = new Vector2(0f, -22f);
        PixelBartendingArt.Grip(bottle, new Vector2(22f, -45f), 1.4f);
        neck = Shape("PourOrigin", bottle, Vector2.one, Color.clear);
        neck.anchoredPosition = new Vector2(0f, bottle.rect.height * .5f);
        stream = Shape("LiquidStream", ui, Vector2.one, new Color(.85f, .78f, .55f, .9f));
        PixelBartendingArt.Apply(stream, "NormalStream");
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
        vessel.localScale = Vector3.one;
        vessel.anchorMin = vessel.anchorMax = new Vector2(.5f, .5f);
        vessel.pivot = new Vector2(.5f, .5f);
        vessel.anchoredPosition = fallbackPosition;
        var body = vessel.GetComponent<Image>();
        Sprite preservedShaker = state.ContainerType == BartendingContainerType.Shaker && body != null ? body.sprite : null;
        if (body != null)
        {
            body.sprite = null;
            body.color = Color.clear;
            body.raycastTarget = false;
        }
        var mask = Shape("LiquidMask", vessel, Vector2.zero, Color.white);
        Stretch(mask);
        mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        masks.Add(state, mask);
        var fill = Shape("StateLiquid", mask, Vector2.zero, Color.white);
        PixelBartendingArt.Apply(fill, "Fill");
        fill.anchorMin = new Vector2(0f, .08f);
        fill.anchorMax = new Vector2(1f, .08f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        fills.Add(state, fill);
        var surface = Shape("Surface", fill, new Vector2(0f, 2f), new Color(.93f, .9f, .8f, .65f));
        surface.anchorMin = new Vector2(0f, 1f);
        surface.anchorMax = Vector2.one;
        surface.pivot = new Vector2(.5f, 1f);
        var iceRoot = new GameObject("StateIce", typeof(RectTransform)).GetComponent<RectTransform>();
        iceRoot.SetParent(mask, false);
        iceRoot.anchorMin = Vector2.zero;
        iceRoot.anchorMax = Vector2.one;
        iceRoot.offsetMin = iceRoot.offsetMax = Vector2.zero;
        var cube = Shape("IceCluster", iceRoot, new Vector2(52f, 48f), Color.white);
        PixelBartendingArt.Apply(cube, "IceCluster");
        iceRoot.gameObject.SetActive(state.HasIce);
        iceVisuals.Add(state, iceRoot);
        var shell = Shape("PixelShell", vessel, Vector2.zero, Color.white);
        Stretch(shell);
        shells.Add(state, shell);
        string family = state.ContainerType == BartendingContainerType.MixingGlass ? "MixingGlass" : "RocksGlass";
        SetGlass(state, family);
        if (state.ContainerType == BartendingContainerType.Jigger)
        {
            PixelBartendingArt.Apply(shell, "Jigger");
            vessel.sizeDelta = new Vector2(56f, 96f);
            // A small top-cup cutaway is a volume cue; shell remains metal.
            mask.SetAsLastSibling();
            mask.anchorMin = new Vector2(.25f, .55f);
            mask.anchorMax = new Vector2(.75f, .94f);
            fillRanges[state] = new Vector2(.02f, .94f);
        }
        if (state.ContainerType == BartendingContainerType.Shaker)
        {
            vessel.sizeDelta = new Vector2(130f, 240f);
            shell.GetComponent<Image>().sprite = preservedShaker;
            shakerClosedSprite = preservedShaker;
            shakerShell = shell;
            mask.anchorMin = new Vector2(.18f, 0f);
            mask.anchorMax = new Vector2(.82f, ShakerBodyFraction);
            shakerMouth = Shape("OpenShakerMouth", vessel, new Vector2(112f, 6f), new Color(.12f, .14f, .19f));
            shakerMouth.anchorMin = shakerMouth.anchorMax = new Vector2(.5f, ShakerBodyFraction);
            var rim = Shape("SteelRim", shakerMouth, new Vector2(108f, 2f), new Color(.75f, .8f, .84f));
            rim.anchoredPosition = new Vector2(0f, -3f);
            shakerMouth.gameObject.SetActive(false);
            // Contents are attached behind opaque steel, never painted on its face.
            PixelBartendingArt.Grip(vessel, new Vector2(55f, -15f), 1.8f).gameObject.SetActive(false);
        }
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    public void SetServingGlass(CocktailGlassware glassware)
    {
        foreach (var pair in vessels)
            if (pair.Key.ContainerType == BartendingContainerType.ServingGlass)
                SetGlass(pair.Key, glassware == CocktailGlassware.CocktailGlass ? "CocktailGlass" :
                    glassware == CocktailGlassware.TallTumbler ? "TallGlass" :
                    glassware == CocktailGlassware.ChampagneFlute ? "Flute" : "RocksGlass");
    }

    private void SetGlass(BartendingContainerState state, string family)
    {
        PixelBartendingArt.Apply(shells[state], family);
        PixelBartendingArt.Apply(masks[state], family + "Mask");
        var sprite = PixelBartendingArt.Get(family);
        if (sprite != null) vessels[state].sizeDelta = sprite.rect.size * 2f;
        fillRanges[state] = family == "CocktailGlass" ? new Vector2(.48f, .92f) :
            family == "Flute" ? new Vector2(.38f, .95f) : new Vector2(.09f, .9f);
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
            string family = source.bottleShape == IngredientBottleShape.Small ? "BottleSmall" :
                source.bottleShape == IngredientBottleShape.Broad ? "BottleBroad" : "BottleTall";
            PixelBartendingArt.Apply(bottle, family);
            bottle.GetComponent<Image>().color = Color.Lerp(Color.white, source.liquidColor, .25f);
            var sprite = PixelBartendingArt.Get(family);
            if (sprite != null) bottle.sizeDelta = sprite.rect.size * 2f;
            bottleAccent.GetComponent<Image>().color = source.liquidColor;
            neck.anchoredPosition = Vector2.up * bottle.rect.height * .5f;
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
        // A released pickup is already upright. Return from its current pose;
        // aligning to the target here would teleport an interrupted approach.
        if (motion == Motion.Approach) { Enter(Motion.Return); return; }
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
        spill.GetComponent<Image>().color = ingredient != null ? ingredient.liquidColor : PixelBartendingArt.Liquid(jiggerState);
        droplet.GetComponent<Image>().color = spill.GetComponent<Image>().color;
        spill.gameObject.SetActive(true);
        // Incoming stream continues over the finite rim, even when accepted ml is zero.
        if (IsAtPourPose) { acceptedFrame = Time.frameCount; DrawStream(); }
    }

    public IEnumerator MoveContainerToPour(BartendingContainerState source, BartendingContainerState target)
    {
        if (source.ContainerType == BartendingContainerType.Shaker) SetShakerClosed(false);
        movingVessel = GetVessel(source);
        transferColor = PixelBartendingArt.Liquid(source);
        if (movingVessel != null) PixelBartendingArt.Grip(movingVessel, new Vector2(movingVessel.rect.width * .4f, -10f)).gameObject.SetActive(true);
        destination = target;
        if (movingVessel == null) yield break;
        vesselHomePosition = movingVessel.position;
        vesselHomeRotation = movingVessel.rotation;
        Vector3 rim = TargetWorldRim(target) + ui.TransformVector(Vector3.up * CurrentPourHeight);
        Vector3 lip = MouthLocal(movingVessel);
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
        return vessel.TransformPoint(MouthLocal(vessel));
    }

    private void PoseContainer(Vector3 rim, float tilt)
    {
        movingVessel.rotation = vesselHomeRotation * Quaternion.Euler(0f, 0f, containerTiltDegrees * tilt);
        movingVessel.position += rim - movingVessel.TransformPoint(MouthLocal(movingVessel));
    }

    public void ObserveContainerTransfer(float acceptedMl)
    {
        RefreshState();
        if (movingVessel == null || acceptedMl <= 0f) { stream.gameObject.SetActive(false); return; }
        PoseContainer(TargetWorldRim(destination) + ui.TransformVector(Vector3.up * CurrentPourHeight), 1f);
        Vector3 lip = movingVessel.TransformPoint(MouthLocal(movingVessel));
        DrawStreamBetween(lip, TargetWorldRim(destination));
    }

    public IEnumerator ReturnContainer()
    {
        stream.gameObject.SetActive(false);
        if (movingVessel == null) yield break;
        Vector3 rim = movingVessel.TransformPoint(MouthLocal(movingVessel));
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
        movingVessel.Find("PixelGrip")?.gameObject.SetActive(false);
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
        Vector2 neckOffset = (Vector2)(bottle.localRotation * (Vector3)neck.anchoredPosition);
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
                    TargetPoint() + Vector2.up * (CurrentPourHeight - neck.anchoredPosition.y), Smooth(elapsed / approachSeconds));
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
        foreach (var pair in iceVisuals)
        {
            pair.Value.gameObject.SetActive(pair.Key.HasIce);
            var cluster = pair.Value.Find("IceCluster") as RectTransform;
            cluster.anchorMin = cluster.anchorMax = new Vector2(.5f, fillRanges[pair.Key].x + .18f);
            cluster.anchoredPosition = Vector2.zero;
        }
        foreach (var pair in fills)
        {
            if (pair.Key == surfaceContainer) continue;
            float scale = pair.Key.CapacityMl > 0f ? pair.Key.CapacityMl : Mathf.Max(1f, displayVolumeMl);
            float level = Mathf.Clamp01(pair.Key.CurrentLiquidVolumeMl / scale);
            Vector2 range = fillRanges[pair.Key];
            pair.Value.anchorMin = new Vector2(0f, range.x);
            pair.Value.anchorMax = new Vector2(1f, Mathf.Lerp(range.x, range.y, level));
            pair.Value.GetComponent<Image>().color = PixelBartendingArt.Liquid(pair.Key);
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
        bool thin = destination == surfaceContainer && liquidSurface != null;
        PixelBartendingArt.Apply(stream, thin ? "ThinFloatStream" : movingVessel != null ? "TransferStream" : "NormalStream");
        stream.GetComponent<Image>().color = movingVessel != null ? transferColor :
            ingredient != null ? ingredient.liquidColor : Color.white;
        stream.sizeDelta = new Vector2(thin ? streamWidth * .5f : streamWidth, delta.magnitude);
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

    private void OnDestroy()
    {
        if (shakerOpenSprite == null) return;
        if (Application.isPlaying) Destroy(shakerOpenSprite);
        else DestroyImmediate(shakerOpenSprite);
    }
}
