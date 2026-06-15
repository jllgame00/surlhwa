using UnityEngine;
using UnityEngine.UI;

public class CustomerWaitIndicator : MonoBehaviour
{
    [SerializeField] private Graphic[] colorTargets;
    [SerializeField] private Transform shakeRoot;
    [SerializeField] private Color relaxedColor = Color.white;
    [SerializeField] private Color urgentColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    [SerializeField] private float maxShakeOffset = 4f;
    [SerializeField] private float shakeSpeed = 35f;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Vector2 screenOffset = new Vector2(0f, 24f);

    private Transform followTarget;
    private RectTransform rectTransform;
    private RectTransform parentRect;
    private Canvas parentCanvas;
    private Vector3 baseLocalPosition;
    private Vector2 baseAnchoredPosition;
    private bool basePositionCaptured;
    private bool isVisible = true;
    private float urgency;

    private void Awake()
    {
        CacheReferences();

        if (colorTargets == null || colorTargets.Length == 0)
            colorTargets = GetComponentsInChildren<Graphic>(true);

        CaptureBasePosition();
        SetUrgency(1f);
    }

    private void OnEnable()
    {
        CaptureBasePosition();
    }

    private void LateUpdate()
    {
        if (!isVisible)
            return;

        if (followTarget == null)
        {
            SetVisible(false);
            return;
        }

        UpdateFollowPosition();
        ApplyShake();
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;

        if (gameObject.activeSelf != visible)
            gameObject.SetActive(visible);

        if (!visible)
            ResetShake();
    }

    public void SetFollowTarget(Transform target)
    {
        followTarget = target;

        if (followTarget == null)
            SetVisible(false);
    }

    public void SetWorldCamera(Camera camera)
    {
        if (camera != null)
            worldCamera = camera;
    }

    public void SetUrgency(float normalizedRemainingTime)
    {
        float remaining = Mathf.Clamp01(normalizedRemainingTime);
        urgency = 1f - remaining;

        Color color = Color.Lerp(urgentColor, relaxedColor, remaining);

        if (colorTargets == null)
            return;

        foreach (Graphic target in colorTargets)
        {
            if (target != null)
                target.color = color;
        }
    }

    private void ApplyShake()
    {
        if (!basePositionCaptured)
            CaptureBasePosition();

        float amount = maxShakeOffset * urgency * urgency;
        if (amount <= 0.01f)
        {
            ResetShake();
            return;
        }

        float time = Time.unscaledTime * shakeSpeed;
        float x = (Mathf.PerlinNoise(time, 0.13f) - 0.5f) * amount * 2f;
        float y = (Mathf.PerlinNoise(0.37f, time) - 0.5f) * amount;
        Vector2 offset = new Vector2(x, y);

        if (HasSeparateShakeRoot())
            shakeRoot.localPosition = baseLocalPosition + new Vector3(x, y, 0f);
        else if (rectTransform != null)
            rectTransform.anchoredPosition = baseAnchoredPosition + offset;
        else
            transform.localPosition = baseLocalPosition + new Vector3(x, y, 0f);
    }

    private void ResetShake()
    {
        if (!basePositionCaptured)
            return;

        if (HasSeparateShakeRoot())
            shakeRoot.localPosition = baseLocalPosition;
        else if (rectTransform != null)
            rectTransform.anchoredPosition = baseAnchoredPosition;
        else
            transform.localPosition = baseLocalPosition;
    }

    private void CaptureBasePosition()
    {
        if (HasSeparateShakeRoot())
        {
            baseLocalPosition = shakeRoot.localPosition;
        }
        else
        {
            baseLocalPosition = transform.localPosition;

            if (rectTransform != null)
                baseAnchoredPosition = rectTransform.anchoredPosition;
        }

        basePositionCaptured = true;
    }

    private void UpdateFollowPosition()
    {
        if (rectTransform == null || parentRect == null || parentCanvas == null)
            CacheReferences();

        if (rectTransform == null || parentRect == null || parentCanvas == null)
            return;

        Camera cam = worldCamera != null ? worldCamera : Camera.main;
        if (cam == null)
            return;

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, followTarget.position);
        screenPos += screenOffset;

        Camera uiCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : (parentCanvas.worldCamera != null ? parentCanvas.worldCamera : cam);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screenPos,
            uiCamera,
            out Vector2 localPoint))
        {
            baseAnchoredPosition = localPoint;
            rectTransform.anchoredPosition = localPoint;
        }
    }

    private void CacheReferences()
    {
        rectTransform = transform as RectTransform;
        parentRect = rectTransform != null ? rectTransform.parent as RectTransform : null;
        parentCanvas = GetComponentInParent<Canvas>();
    }

    private bool HasSeparateShakeRoot()
    {
        return shakeRoot != null && shakeRoot != transform;
    }
}
