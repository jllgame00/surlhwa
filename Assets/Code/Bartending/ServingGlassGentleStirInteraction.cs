using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared I9B serving-glass stir interaction. It deliberately reuses I5's
/// wrapped-angle tracker rather than introducing a second technique engine.
/// This records a completed GentleStir semantic; it does not replace primary
/// Mixing Glass Stir quality or model dilution/temperature.
/// </summary>
public sealed class ServingGlassGentleStirInteraction
{
    private readonly float minimumDurationSeconds;
    private readonly float minimumRevolutions;
    private readonly float minimumMouseRadiusPixels;
    private readonly StirTechniqueTracker tracker;

    private RectTransform center;
    private RectTransform spoonVisual;
    private GameObject runtimeSpoonObject;
    private bool active;

    public bool IsActive => active;
    public float Revolutions => tracker.Revolutions;
    public float ElapsedSeconds => tracker.ElapsedSeconds;

    public ServingGlassGentleStirInteraction(
        float minimumDurationSeconds = 1.25f,
        float minimumRevolutions = 0.5f,
        float minimumMouseRadiusPixels = 24f,
        float maximumAngularJumpDegrees = 75f)
    {
        this.minimumDurationSeconds = Mathf.Max(0.1f, minimumDurationSeconds);
        this.minimumRevolutions = Mathf.Max(0.1f, minimumRevolutions);
        this.minimumMouseRadiusPixels = Mathf.Max(1f, minimumMouseRadiusPixels);
        tracker = new StirTechniqueTracker(maximumAngularJumpDegrees);
    }

    public bool Begin(RectTransform visualCenter, Transform runtimeVisualParent)
    {
        center = visualCenter;
        if (center == null)
            center = EnsureFallbackCenter(runtimeVisualParent);

        if (center == null)
            return false;

        spoonVisual = EnsureSpoonVisual(center);
        tracker.Reset();
        active = true;
        if (spoonVisual != null)
            spoonVisual.gameObject.SetActive(true);
        return true;
    }

    public void Tick()
    {
        if (!active)
            return;

        Camera uiCamera = ResolveUiCamera(center);
        Vector2 centerScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, center.position);
        Vector2 offset = (Vector2)Input.mousePosition - centerScreen;
        if (offset.sqrMagnitude < minimumMouseRadiusPixels * minimumMouseRadiusPixels)
        {
            tracker.RecordIdle(Time.deltaTime);
            return;
        }

        float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        if (tracker.RecordAngle(angle, Time.deltaTime) && spoonVisual != null)
            spoonVisual.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    /// <summary>
    /// The same wrapped-angle path used by runtime mouse input, exposed for
    /// deterministic validation without a second GentleStir scorer.
    /// </summary>
    public bool TryRecordAngle(float angle, float deltaTime)
    {
        if (!active)
            return false;

        bool accepted = tracker.RecordAngle(angle, deltaTime);
        if (accepted && spoonVisual != null)
            spoonVisual.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
        return accepted;
    }

    public bool TryFinish()
    {
        if (!active)
            return false;

        if (tracker.ElapsedSeconds < minimumDurationSeconds || tracker.Revolutions < minimumRevolutions)
            return false;

        End();
        return true;
    }

    public void End()
    {
        active = false;
        if (spoonVisual != null)
            spoonVisual.gameObject.SetActive(false);
    }

    private RectTransform EnsureFallbackCenter(Transform parent)
    {
        if (parent == null)
            return null;

        GameObject marker = new GameObject("RuntimeServingGlassGentleStir", typeof(RectTransform), typeof(Image));
        marker.transform.SetParent(parent, false);
        RectTransform rect = marker.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.78f, 0.52f);
        rect.anchorMax = new Vector2(0.78f, 0.52f);
        rect.sizeDelta = new Vector2(120f, 150f);
        Image image = marker.GetComponent<Image>();
        image.color = new Color(0.08f, 0.12f, 0.16f, 0.72f);
        return rect;
    }

    private RectTransform EnsureSpoonVisual(RectTransform parent)
    {
        if (runtimeSpoonObject == null)
        {
            runtimeSpoonObject = new GameObject("RuntimeGentleStirSpoon", typeof(RectTransform), typeof(Image));
            runtimeSpoonObject.transform.SetParent(parent, false);
            Image image = runtimeSpoonObject.GetComponent<Image>();
            image.color = new Color(0.8f, 0.85f, 0.9f, 0.95f);
        }

        RectTransform rect = runtimeSpoonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.2f);
        rect.anchoredPosition = new Vector2(0f, 0f);
        rect.sizeDelta = new Vector2(10f, 76f);
        return rect;
    }

    private static Camera ResolveUiCamera(RectTransform rect)
    {
        Canvas canvas = rect != null ? rect.GetComponentInParent<Canvas>() : null;
        return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
    }
}
