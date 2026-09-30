#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Owner-started launch only; input, reload and restoration belong to the existing runner.</summary>
[InitializeOnLoad]
internal static class SURLHWAPointerValidationLauncher
{
    const string Project = @"D:\univ\surlhwa\Surlhwa\Temp\PhaseMDiegetic\ValidationProject";
    const string Root = "SURLHWA/Validation/UI Pointer/";
    const ShortcutModifiers Keys = ShortcutModifiers.Action | ShortcutModifiers.Alt | ShortcutModifiers.Shift;
    static Action request;
    static EditorWindow gameView;
    static bool handoff;
    static double deadline;

    static SURLHWAPointerValidationLauncher()
    {
        AssemblyReloadEvents.beforeAssemblyReload += CancelQueued;
        EditorApplication.quitting += CancelQueued;
        EditorApplication.playModeStateChanged += _ => CancelQueued();
    }

    [MenuItem(Root + "A - Normal Control")]
    static void MenuA() => Queue(SURLHWAPhaseMUIValidation.RunPointerControl, true);
    [MenuItem(Root + "B - Expected Mismatch")]
    static void MenuB() => Queue(SURLHWAPhaseMUIValidation.RunPointerHandoffDiagnostic, true);
    [MenuItem(Root + "C - Pointer Ready Control")]
    static void MenuC() => Queue(SURLHWAPhaseMUIValidation.RunPointerReadyControl, true);

    [Shortcut(Root + "A - Normal Control", KeyCode.F6, Keys)]
    static void KeyA() => Queue(SURLHWAPhaseMUIValidation.RunPointerControl, false);
    [Shortcut(Root + "B - Expected Mismatch", KeyCode.F7, Keys)]
    static void KeyB() => Queue(SURLHWAPhaseMUIValidation.RunPointerHandoffDiagnostic, false);
    [Shortcut(Root + "C - Pointer Ready Control", KeyCode.F8, Keys)]
    static void KeyC() => Queue(SURLHWAPhaseMUIValidation.RunPointerReadyControl, false);

    // Use the same GameView identity check as the existing runner. Never create a window.
    static bool IsGameView(EditorWindow window) => window != null && window.GetType().Name == "GameView";
    static string PrerequisiteFailure()
    {
        if (!string.Equals(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), Project, StringComparison.OrdinalIgnoreCase))
            return "Open the validation project at " + Project + ".";
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPaused)
            return "Stop Play Mode and clear Editor pause before starting.";
        if (SessionState.GetBool("SURLHWA_PHASE_M_UI_PENDING", false) || SessionState.GetBool("SURLHWA_UI_OBSERVERS_ACTIVE", false))
            return "Wait for the existing validation's observer cleanup and progress/audio restoration.";
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return "Wait for compilation and asset import to finish, then launch again.";
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            return "Resolve unsaved scene changes yourself before launching; nothing was saved or discarded.";
        if (!UnityEditorInternal.InternalEditorUtility.isApplicationActive)
            return "Activate this validation Editor before launching; no application foreground request is made.";
        return null;
    }

    static void Queue(Action run, bool allowHandoff)
    {
        if (request != null) { Refuse("A launch was already queued; both requests were cancelled. Launch once again."); return; }
        string failure = PrerequisiteFailure();
        if (failure != null) { Refuse(failure); return; }
        if (!allowHandoff && !IsGameView(EditorWindow.focusedWindow))
        { Refuse("Select the existing Game tab before using the keyboard shortcut."); return; }
        var views = Resources.FindObjectsOfTypeAll<EditorWindow>().Where(IsGameView).ToArray();
        gameView = IsGameView(EditorWindow.focusedWindow) ? EditorWindow.focusedWindow : views.Length == 1 ? views[0] : null;
        if (gameView == null) { Refuse("Open/select the intended Game tab first; an unambiguous existing Game View is required."); return; }
        request = run;
        handoff = allowHandoff;
        deadline = EditorApplication.timeSinceStartup + 2;
        // Two one-shot Editor callbacks, not a polling/wait loop. Menus close before focus handoff.
        EditorApplication.delayCall += Handoff;
    }

    static bool CheckRequest()
    {
        if (request == null) return false;
        string failure = PrerequisiteFailure();
        if (failure == null && EditorApplication.timeSinceStartup > deadline)
            failure = "Launch expired before dispatch. Launch again when the Editor is idle.";
        if (failure == null && gameView == null) failure = "The selected Game View closed. Open it and launch again.";
        if (failure == null) return true;
        Refuse(failure);
        return false;
    }

    static void Handoff()
    {
        if (!CheckRequest()) return;
        if (handoff && EditorWindow.focusedWindow != gameView) gameView.Focus(); // At most once, before dispatch only.
        EditorApplication.delayCall += Dispatch;
    }

    static void Dispatch()
    {
        if (!CheckRequest()) return;
        if (EditorWindow.focusedWindow != gameView || !IsGameView(EditorWindow.focusedWindow))
        { Refuse("Game View focus was not verified. Select its tab and launch again; no runner started."); return; }
        Action run = request;
        Cancel(); // No pending launch survives dispatch or a runner exception.
        Debug.Log("[UI Pointer Launcher] Dispatch " + run.Method.Name + "; verified Game View. No further focus handoff.");
        try { run(); }
        catch (Exception e) { Debug.LogException(e); }
    }

    static void Refuse(string reason)
    {
        Cancel();
        Debug.LogWarning("[UI Pointer Launcher] NOT STARTED: " + reason + " Request cleared; later focus will not start it.");
    }
    static void CancelQueued()
    {
        if (request != null) Refuse("Editor lifecycle changed before dispatch. Launch again after compilation/Play Mode has stopped.");
    }
    static void Cancel()
    {
        EditorApplication.delayCall -= Handoff;
        EditorApplication.delayCall -= Dispatch;
        request = null;
        gameView = null;
    }
}
#endif
