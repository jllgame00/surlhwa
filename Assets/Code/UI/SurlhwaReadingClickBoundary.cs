using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>A short pointer boundary shared by controls in one reading band.
/// Callback-only validation does not exercise this physical-click safeguard.</summary>
public sealed class SurlhwaReadingClickBoundary : MonoBehaviour, ICanvasRaycastFilter, IPointerClickHandler
{
    sealed class Beat { public float until; }
    Beat beat;
    public static void Bind(params Button[] buttons)
    {
        var shared = new Beat();
        foreach (var button in buttons)
        {
            if (button == null || button.GetComponent<SurlhwaReadingClickBoundary>() != null) continue;
            var boundary = button.gameObject.AddComponent<SurlhwaReadingClickBoundary>();
            boundary.beat = shared;
        }
    }
    public bool IsRaycastLocationValid(Vector2 point, Camera camera) => beat == null || Time.unscaledTime >= beat.until;
    public void OnPointerClick(PointerEventData data)
    {
        if (beat != null && data.button == PointerEventData.InputButton.Left)
            beat.until = Time.unscaledTime + .24f;
    }
}
