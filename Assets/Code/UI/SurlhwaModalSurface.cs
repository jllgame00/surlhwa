using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Bounded uGUI surface: shielding, focus and shared reading pause.
/// Does not own requests, technique, completion or persistence.</summary>
public sealed class SurlhwaModalSurface : MonoBehaviour
{
#if UNITY_EDITOR
    // Opt-in observation only. The validation runner owns the bounded buffer.
    public static Action<string> DiagnosticTrace;
#endif
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Trace(string boundary)
    {
#if UNITY_EDITOR
        DiagnosticTrace?.Invoke(boundary);
#endif
    }
    static readonly List<SurlhwaModalSurface> open = new();
    static readonly Dictionary<Selectable, CanvasGroup> gates = new();
    static float previousTimeScale;
    static int pauseOwners;
    static int boundaryFrame = -1;
    static readonly HashSet<KeyCode> heldKeys = new();
    static readonly KeyCode[] actionKeys = { KeyCode.Space, KeyCode.M, KeyCode.T, KeyCode.D, KeyCode.S,
        KeyCode.I, KeyCode.J, KeyCode.P, KeyCode.A, KeyCode.LeftArrow, KeyCode.RightArrow,
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.G, KeyCode.X, KeyCode.Backspace,
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6 };
    bool configured, registered, pauses;
    int order;
    Action back;
    GameObject shield, previousFocus;
    Color shieldColor = SurlhwaUITheme.Shade;
    public static bool IsPaused => pauseOwners > 0;
    public static int InputBoundaryVersion { get; private set; }
    public static SurlhwaModalSurface Top
    {
        get {
            SurlhwaModalSurface top = null;
            foreach (var item in open)
                if (item != null && item.isActiveAndEnabled && (top == null || item.order >= top.order)) top = item;
            return top;
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        open.Clear(); gates.Clear(); heldKeys.Clear(); pauseOwners = 0; boundaryFrame = -1; InputBoundaryVersion = 0;
    }
    public static SurlhwaModalSurface Configure(GameObject root, int sortingOrder, Action onBack, bool pause = false, Color? shade = null)
    {
        if (root == null || !Application.isPlaying) return null;
        var surface = root.GetComponent<SurlhwaModalSurface>() ?? root.AddComponent<SurlhwaModalSurface>();
        surface.order = sortingOrder; surface.back = onBack; surface.pauses = pause;
        surface.shieldColor = shade ?? SurlhwaUITheme.Shade;
        var canvas = root.GetComponent<Canvas>();
        if (canvas == null) canvas = root.AddComponent<Canvas>();
        canvas.overrideSorting = true; canvas.sortingOrder = sortingOrder;
        if (root.GetComponent<GraphicRaycaster>() == null) root.AddComponent<GraphicRaycaster>();
        surface.configured = true;
        if (root.activeInHierarchy) surface.Activate();
        return surface;
    }
    void OnEnable() { if (configured) Activate(); }
    void Activate()
    {
        if (registered) return;
        registered = true;
        previousFocus = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        // Unity initializes an inactive, newly added nested Canvas on first enable.
        // Apply its override after that initialization, on every activation.
        var panelCanvas = GetComponent<Canvas>();
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = order;
        var canvas = panelCanvas.rootCanvas;
        shield = new GameObject("ModalInputShield", typeof(RectTransform), typeof(Image), typeof(Canvas), typeof(GraphicRaycaster));
        shield.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)shield.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        shield.GetComponent<Image>().color = shieldColor;
        var layer = shield.GetComponent<Canvas>(); layer.overrideSorting = true; layer.sortingOrder = order - 1;
        open.Add(this);
        if (pauses && pauseOwners++ == 0) { previousTimeScale = Time.timeScale; Time.timeScale = 0; }
        Boundary();
        RefreshGates();
        FocusFirst();
        Trace("modal.acquire name=" + name + "#" + GetInstanceID() + " pauses=" + pauses +
            " owners=" + pauseOwners + " previous=" + previousTimeScale + " scale=" + Time.timeScale);
    }
    void FocusFirst()
    {
        if (Top != this || EventSystem.current == null) return;
        foreach (var control in GetComponentsInChildren<Selectable>())
            if (control.IsInteractable()) { EventSystem.current.SetSelectedGameObject(control.gameObject); return; }
        EventSystem.current.SetSelectedGameObject(null);
    }
    void LateUpdate()
    {
        if (Top != this) return;
        RefreshGates(); // Includes controls populated after the surface opens.
        var focus = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (!UsableFocus(focus) || !focus.transform.IsChildOf(transform)) FocusFirst();
    }
    static bool UsableFocus(GameObject focus) => focus != null && focus.activeInHierarchy &&
        focus.TryGetComponent<Selectable>(out var control) && control.IsActive() && control.IsInteractable();
    public static bool Back()
    {
        var top = Top;
        if (top == null || top.back == null) return false;
        top.back(); return true;
    }
    public static bool Allows(Transform control) => Top == null || control.IsChildOf(Top.transform);
    static void RefreshGates()
    {
        // Hidden controls may reopen after the final modal has gone away.
        foreach (var pair in gates)
            if (pair.Key != null && pair.Value != null) pair.Value.interactable = Allows(pair.Key.transform);
        foreach (var control in Selectable.allSelectablesArray)
        {
            if (!gates.TryGetValue(control, out var group) || group == null)
            {
                group = control.gameObject.AddComponent<CanvasGroup>();
                gates[control] = group;
            }
            group.interactable = Allows(control.transform);
        }
        var removed = new List<Selectable>();
        foreach (var pair in gates) if (pair.Key == null || pair.Value == null) removed.Add(pair.Key);
        foreach (var key in removed) gates.Remove(key);
    }
    static void Boundary()
    {
        boundaryFrame = Time.frameCount;
        InputBoundaryVersion++;
        // Routes do not poll while reading. A key released inside the modal
        // must not suppress the first new press when the modal closes.
        heldKeys.RemoveWhere(key => !Input.GetKey(key));
        foreach (var key in actionKeys) if (Input.GetKey(key)) heldKeys.Add(key);
    }
    public static bool KeyAllowed(KeyCode key)
    {
        if (IsPaused || Time.frameCount == boundaryFrame) return false;
        if (!heldKeys.Contains(key)) return true;
        if (!Input.GetKey(key)) heldKeys.Remove(key);
        return false;
    }
    public static bool GetKey(KeyCode key) => KeyAllowed(key) && Input.GetKey(key);
    public static bool GetKeyDown(KeyCode key) => KeyAllowed(key) && Input.GetKeyDown(key);
    public static bool GetKeyUp(KeyCode key) => KeyAllowed(key) && Input.GetKeyUp(key);
    void OnDisable() => Release();
    void OnDestroy() => Release();
    void Release()
    {
        if (!registered) return;
        Trace("modal.release.begin name=" + name + "#" + GetInstanceID() + " owners=" + pauseOwners);
        bool wasTop = Top == this || (open.Count > 0 && open[open.Count - 1] == this);
        registered = false; open.Remove(this);
        if (shield != null) { shield.SetActive(false); Destroy(shield); }
        if (pauses && --pauseOwners == 0 && Mathf.Approximately(Time.timeScale, 0)) Time.timeScale = previousTimeScale;
        Boundary(); RefreshGates();
        if (wasTop && EventSystem.current != null)
        {
            if (UsableFocus(previousFocus) && Allows(previousFocus.transform))
                EventSystem.current.SetSelectedGameObject(previousFocus);
            else if (Top != null) Top.FocusFirst();
            else EventSystem.current.SetSelectedGameObject(null);
        }
        Trace("modal.release.end name=" + name + " owners=" + pauseOwners + " scale=" + Time.timeScale +
            " top=" + (Top != null ? Top.name : "NONE"));
    }
}
