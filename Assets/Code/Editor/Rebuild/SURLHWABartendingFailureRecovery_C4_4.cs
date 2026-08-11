using UnityEditor;
using UnityEngine;

/// <summary>
/// One-shot, Editor-only failure injection for manual C4.4 recovery checks.
/// Arm a mode in Play Mode, then begin the next production Bartending request.
/// </summary>
internal static class SURLHWABartendingFailureRecovery_C4_4
{
    private const string MenuRoot = "SURLHWA/Rebuild Tests/C4.4 Failure Recovery/";

    [MenuItem(MenuRoot + "Inject Load Failure (Next Bartending)")]
    private static void InjectLoadFailure()
    {
        Arm(BartendingFailureInjection.LoadFailure);
    }

    [MenuItem(MenuRoot + "Inject Runner Failure (Next Bartending)")]
    private static void InjectRunnerFailure()
    {
        Arm(BartendingFailureInjection.RunnerFailure);
    }

    [MenuItem(MenuRoot + "Inject Start Failure (Next Bartending)")]
    private static void InjectStartFailure()
    {
        Arm(BartendingFailureInjection.StartFailure);
    }

    [MenuItem(MenuRoot + "Inject Unload Failure After Result (Next Bartending)")]
    private static void InjectUnloadFailureAfterResult()
    {
        Arm(BartendingFailureInjection.UnloadFailureAfterResult);
    }

    [MenuItem(MenuRoot + "Clear Pending Injection")]
    private static void ClearPendingInjection()
    {
        Arm(BartendingFailureInjection.None);
    }

    private static void Arm(BartendingFailureInjection failureInjection)
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[SURLHWA C4.4] Enter Play Mode before arming a Bartending failure injection.");
            return;
        }

        BartendingFlowCoordinator coordinator =
            Object.FindFirstObjectByType<BartendingFlowCoordinator>();
        if (coordinator == null)
        {
            Debug.LogError("[SURLHWA C4.4] BartendingFlowCoordinator was not found in the loaded BarScene.");
            return;
        }

        if (coordinator.IsBartendingActive)
        {
            Debug.LogError("[SURLHWA C4.4] Failure injection cannot be changed while Bartending is active.");
            return;
        }

        coordinator.SetEditorFailureInjection(failureInjection);
        Debug.Log("[SURLHWA C4.4] Armed next Bartending request with " + failureInjection + ".");
    }
}
