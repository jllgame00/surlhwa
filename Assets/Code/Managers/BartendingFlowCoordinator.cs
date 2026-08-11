using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// BAR_LOCAL boundary between Bar-side order ownership and MiniGame execution.
/// Production execution loads BartendingScene additively while BarScene retains
/// the order, customer, and active Scene ownership.
/// </summary>
public class BartendingFlowCoordinator : MonoBehaviour
{
    private const string BartendingScenePath = "Assets/_SURLHWA/Scenes/Gameplay/BartendingScene.unity";

    [SerializeField] private MiniGameManager miniGameManager;
    [SerializeField] private Camera barMainCamera;

    private bool isBartendingActive;
    private int sessionGeneration;
    private int activeSessionGeneration;
    private bool completionCaptured;
    private bool completionFinalized;

    public bool IsBartendingActive => isBartendingActive;

    private void Awake()
    {
        ResolveLegacyBarMiniGameManager();
    }

    /// <summary>
    /// Bar-facing contract. The serialized MiniGameManager remains the legacy
    /// BarScene runner, but production execution is scoped to BartendingScene.
    /// </summary>
    public bool BeginBartending(BartendingSession session, Action<MiniGameResult> onCompleted)
    {
        if (session == null || session.Order == null)
        {
            Debug.LogError("[BartendingFlowCoordinator] BeginBartending requires an OrderData session.");
            return false;
        }

        if (IsBartendingActive)
        {
            Debug.LogWarning("[BartendingFlowCoordinator] BeginBartending ignored because Bartending is already active.");
            return false;
        }

        Scene existingBartendingScene = SceneManager.GetSceneByPath(BartendingScenePath);
        if (existingBartendingScene.IsValid() && existingBartendingScene.isLoaded)
        {
            Debug.LogError("[BartendingFlowCoordinator] BeginBartending requires BartendingScene to be unloaded before a new session starts.");
            return false;
        }

        if (!TryResolveBarAudioListener(out AudioListener barAudioListener))
            return false;

        int generation = BeginAcceptedSession();
        StartCoroutine(RunAdditiveBartending(generation, session, onCompleted, barAudioListener));

        Debug.Log(
            "[BartendingFlowCoordinator] BeginBartending additive -> " +
            session.Order.orderName + " / " + session.Method + " / generation=" + generation);

        return true;
    }

    private IEnumerator RunAdditiveBartending(
        int generation,
        BartendingSession session,
        Action<MiniGameResult> onCompleted,
        AudioListener barAudioListener)
    {
        Scene bartendingScene = default;
        bool barPresentationCaptured = false;
        bool barCameraWasEnabled = false;
        bool barAudioListenerWasEnabled = false;

        AsyncOperation loadOperation = null;
        Exception loadException = null;
        try
        {
            loadOperation = SceneManager.LoadSceneAsync(BartendingScenePath, LoadSceneMode.Additive);
        }
        catch (Exception exception)
        {
            loadException = exception;
        }

        if (loadException != null)
        {
            yield return FailAdditiveBartendingWithCleanup(
                generation,
                "LOAD_OPERATION_NULL",
                "LoadSceneAsync threw: " + loadException.Message,
                bartendingScene,
                barAudioListener,
                barPresentationCaptured,
                barCameraWasEnabled,
                barAudioListenerWasEnabled);
            yield break;
        }

        if (loadOperation == null)
        {
            yield return FailAdditiveBartendingWithCleanup(
                generation,
                "LOAD_OPERATION_NULL",
                "LoadSceneAsync returned null.",
                bartendingScene,
                barAudioListener,
                barPresentationCaptured,
                barCameraWasEnabled,
                barAudioListenerWasEnabled);
            yield break;
        }

        yield return loadOperation;

        if (!IsCurrentSession(generation))
        {
            ReportAdditiveFailure("STALE_GENERATION", "Load completion belongs to stale generation " + generation + ".");
            yield break;
        }

        bartendingScene = SceneManager.GetSceneByPath(BartendingScenePath);
        if (!bartendingScene.IsValid())
        {
            yield return FailAdditiveBartendingWithCleanup(
                generation,
                "SCENE_INVALID",
                "GetSceneByPath returned an invalid BartendingScene.",
                bartendingScene,
                barAudioListener,
                barPresentationCaptured,
                barCameraWasEnabled,
                barAudioListenerWasEnabled);
            yield break;
        }

        if (!bartendingScene.isLoaded)
        {
            yield return FailAdditiveBartendingWithCleanup(
                generation,
                "SCENE_NOT_LOADED",
                "BartendingScene is not loaded after additive load completion.",
                bartendingScene,
                barAudioListener,
                barPresentationCaptured,
                barCameraWasEnabled,
                barAudioListenerWasEnabled);
            yield break;
        }

        if (bartendingScene.path != BartendingScenePath)
        {
            yield return FailAdditiveBartendingWithCleanup(
                generation,
                "SCENE_PATH_MISMATCH",
                "Loaded BartendingScene path does not match the canonical path: " + bartendingScene.path,
                bartendingScene,
                barAudioListener,
                barPresentationCaptured,
                barCameraWasEnabled,
                barAudioListenerWasEnabled);
            yield break;
        }

        if (SceneManager.GetActiveScene().path != gameObject.scene.path)
        {
            yield return FailAdditiveBartendingWithCleanup(
                generation,
                "ACTIVE_SCENE_CHANGED",
                "BarScene did not remain active during additive load.",
                bartendingScene,
                barAudioListener,
                barPresentationCaptured,
                barCameraWasEnabled,
                barAudioListenerWasEnabled);
            yield break;
        }

        List<MiniGameManager> runners = FindMiniGameManagersInScene(bartendingScene);
        if (runners.Count != 1)
        {
            yield return FailAdditiveBartendingWithCleanup(
                generation,
                "RUNNER_COUNT_MISMATCH",
                "Expected exactly one BartendingScene MiniGameManager. Actual: " + runners.Count,
                bartendingScene,
                barAudioListener,
                barPresentationCaptured,
                barCameraWasEnabled,
                barAudioListenerWasEnabled);
            yield break;
        }

        MiniGameManager runner = runners[0];
        if (runner == null || runner.gameObject.scene != bartendingScene)
        {
            yield return FailAdditiveBartendingWithCleanup(
                generation,
                "RUNNER_SCENE_MISMATCH",
                "Discovered MiniGameManager does not belong to the loaded BartendingScene.",
                bartendingScene,
                barAudioListener,
                barPresentationCaptured,
                barCameraWasEnabled,
                barAudioListenerWasEnabled);
            yield break;
        }

        if (!TryCaptureAndDisableBarPresentation(
                barAudioListener,
                out barCameraWasEnabled,
                out barAudioListenerWasEnabled))
        {
            yield return FailAdditiveBartendingWithCleanup(
                generation,
                "BAR_PRESENTATION_INVALID",
                "Bar camera or AudioListener could not be captured before MiniGame start.",
                bartendingScene,
                barAudioListener,
                barPresentationCaptured,
                barCameraWasEnabled,
                barAudioListenerWasEnabled);
            yield break;
        }

        barPresentationCaptured = true;
        MiniGameResult capturedResult = default;
        Exception playException = null;
        try
        {
            runner.Play(session.Order, session.InitialSession, result =>
            {
                if (!TryCaptureCompletion(generation, "BartendingScene MiniGame callback"))
                    return;

                capturedResult = result;
            });
        }
        catch (Exception exception)
        {
            playException = exception;
        }

        if (playException != null)
        {
            yield return FailAdditiveBartendingWithCleanup(
                generation,
                "RUNNER_PLAY_EXCEPTION",
                "BartendingScene MiniGameManager.Play threw: " + playException.Message,
                bartendingScene,
                barAudioListener,
                barPresentationCaptured,
                barCameraWasEnabled,
                barAudioListenerWasEnabled);
            yield break;
        }

        if (IsCurrentSession(generation) && !completionCaptured && !runner.IsPlaying)
        {
            yield return FailAdditiveBartendingWithCleanup(
                generation,
                "RUNNER_DID_NOT_START",
                "BartendingScene MiniGameManager.Play completed without starting a MiniGame.",
                bartendingScene,
                barAudioListener,
                barPresentationCaptured,
                barCameraWasEnabled,
                barAudioListenerWasEnabled);
            yield break;
        }

        while (IsCurrentSession(generation) && !completionCaptured)
            yield return null;

        if (!IsCurrentSession(generation))
        {
            ReportAdditiveFailure("STALE_GENERATION", "MiniGame completion belongs to stale generation " + generation + ".");
            yield break;
        }

        AsyncOperation unloadOperation = null;
        Exception unloadException = null;
        try
        {
            unloadOperation = SceneManager.UnloadSceneAsync(bartendingScene);
        }
        catch (Exception exception)
        {
            unloadException = exception;
        }

        if (unloadException != null)
        {
            yield return FailAdditiveBartendingWithCleanup(
                generation,
                "UNLOAD_OPERATION_NULL",
                "UnloadSceneAsync threw: " + unloadException.Message,
                bartendingScene,
                barAudioListener,
                barPresentationCaptured,
                barCameraWasEnabled,
                barAudioListenerWasEnabled);
            yield break;
        }

        if (unloadOperation == null)
        {
            yield return FailAdditiveBartendingWithCleanup(
                generation,
                "UNLOAD_OPERATION_NULL",
                "UnloadSceneAsync returned null.",
                bartendingScene,
                barAudioListener,
                barPresentationCaptured,
                barCameraWasEnabled,
                barAudioListenerWasEnabled);
            yield break;
        }

        yield return unloadOperation;

        if (!IsCurrentSession(generation))
        {
            ReportAdditiveFailure("STALE_GENERATION", "Unload completion belongs to stale generation " + generation + ".");
            yield break;
        }

        Scene postUnloadScene = SceneManager.GetSceneByPath(BartendingScenePath);
        if (postUnloadScene.IsValid() && postUnloadScene.isLoaded)
        {
            yield return FailAdditiveBartendingWithCleanup(
                generation,
                "SCENE_STILL_LOADED",
                "BartendingScene remains loaded after UnloadSceneAsync completion.",
                postUnloadScene,
                barAudioListener,
                barPresentationCaptured,
                barCameraWasEnabled,
                barAudioListenerWasEnabled);
            yield break;
        }

        if (SceneManager.GetActiveScene().path != gameObject.scene.path)
        {
            yield return FailAdditiveBartendingWithCleanup(
                generation,
                "ACTIVE_SCENE_CHANGED",
                "BarScene did not remain active after BartendingScene unload.",
                default,
                barAudioListener,
                barPresentationCaptured,
                barCameraWasEnabled,
                barAudioListenerWasEnabled);
            yield break;
        }

        RestoreBarPresentation(
            barAudioListener,
            barPresentationCaptured,
            barCameraWasEnabled,
            barAudioListenerWasEnabled);

        if (!TryFinalizeSession(generation, "BartendingScene additive lifecycle"))
            yield break;

        onCompleted?.Invoke(capturedResult);
    }

    private IEnumerator FailAdditiveBartendingWithCleanup(
        int generation,
        string code,
        string detail,
        Scene loadedBartendingScene,
        AudioListener barAudioListener,
        bool barPresentationCaptured,
        bool barCameraWasEnabled,
        bool barAudioListenerWasEnabled)
    {
        ReportAdditiveFailure(code, detail);

        if (!IsCurrentSession(generation))
        {
            ReportAdditiveFailure("STALE_GENERATION", "Failure handling belongs to stale generation " + generation + ".");
            yield break;
        }

        Scene sceneToCleanup = loadedBartendingScene;
        if (!sceneToCleanup.IsValid() || !sceneToCleanup.isLoaded)
        {
            Scene resolvedScene = SceneManager.GetSceneByPath(BartendingScenePath);
            if (resolvedScene.IsValid() && resolvedScene.isLoaded)
                sceneToCleanup = resolvedScene;
        }

        if (sceneToCleanup.IsValid() && sceneToCleanup.isLoaded)
        {
            AsyncOperation cleanupOperation = null;
            Exception cleanupException = null;
            try
            {
                cleanupOperation = SceneManager.UnloadSceneAsync(sceneToCleanup);
            }
            catch (Exception exception)
            {
                cleanupException = exception;
            }

            if (cleanupException != null)
            {
                ReportAdditiveFailure("UNLOAD_OPERATION_NULL", "Failure cleanup threw: " + cleanupException.Message);
            }
            else if (cleanupOperation == null)
            {
                ReportAdditiveFailure("UNLOAD_OPERATION_NULL", "Failure cleanup could not start unload.");
            }
            else
            {
                yield return cleanupOperation;

                Scene postCleanupScene = SceneManager.GetSceneByPath(BartendingScenePath);
                if (postCleanupScene.IsValid() && postCleanupScene.isLoaded)
                    ReportAdditiveFailure("SCENE_STILL_LOADED", "Failure cleanup left BartendingScene loaded.");
            }
        }

        RestoreBarPresentation(
            barAudioListener,
            barPresentationCaptured,
            barCameraWasEnabled,
            barAudioListenerWasEnabled);

        TryFinalizeSession(generation, "additive Bartending failure: " + code);
    }

    private bool TryResolveBarAudioListener(out AudioListener barAudioListener)
    {
        barAudioListener = null;

        if (barMainCamera == null)
        {
            Debug.LogError("[BartendingFlowCoordinator] BeginBartending requires the serialized Bar Main Camera.");
            return false;
        }

        if (barMainCamera.gameObject.scene != gameObject.scene)
        {
            Debug.LogError("[BartendingFlowCoordinator] Serialized Bar Main Camera does not belong to the coordinator's BarScene.");
            return false;
        }

        AudioListener[] listeners = barMainCamera.GetComponents<AudioListener>();
        if (listeners.Length != 1 || listeners[0] == null)
        {
            Debug.LogError(
                "[BartendingFlowCoordinator] Expected exactly one AudioListener on the serialized Bar Main Camera. Actual: " +
                listeners.Length);
            return false;
        }

        barAudioListener = listeners[0];
        return true;
    }

    private bool TryCaptureAndDisableBarPresentation(
        AudioListener barAudioListener,
        out bool barCameraWasEnabled,
        out bool barAudioListenerWasEnabled)
    {
        barCameraWasEnabled = false;
        barAudioListenerWasEnabled = false;

        if (barMainCamera == null || barAudioListener == null)
            return false;

        barCameraWasEnabled = barMainCamera.enabled;
        barAudioListenerWasEnabled = barAudioListener.enabled;

        barAudioListener.enabled = false;
        barMainCamera.enabled = false;
        return true;
    }

    private void RestoreBarPresentation(
        AudioListener barAudioListener,
        bool barPresentationCaptured,
        bool barCameraWasEnabled,
        bool barAudioListenerWasEnabled)
    {
        if (!barPresentationCaptured)
            return;

        if (barMainCamera != null)
            barMainCamera.enabled = barCameraWasEnabled;

        if (barAudioListener != null)
            barAudioListener.enabled = barAudioListenerWasEnabled;
    }

    private static void ReportAdditiveFailure(string code, string detail)
    {
        Debug.LogError("[BartendingFlowCoordinator][" + code + "] " + detail);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only C4.2 probe. It exercises coordinator-owned additive lifecycle
    /// state without starting a MiniGame or changing the production execution path.
    /// </summary>
    public bool BeginAdditiveLifecycleProbe(Action<bool, string> onCompleted)
    {
        if (!Application.isPlaying)
        {
            ReportProbeFailure("NOT_IN_PLAY_MODE", "Enter Play Mode before running the C4.2 coordinator probe.");
            onCompleted?.Invoke(false, "NOT_IN_PLAY_MODE");
            return false;
        }

        if (IsBartendingActive)
        {
            ReportProbeFailure("COORDINATOR_ALREADY_ACTIVE", "The coordinator is already active.");
            onCompleted?.Invoke(false, "COORDINATOR_ALREADY_ACTIVE");
            return false;
        }

        Scene existingBartendingScene = SceneManager.GetSceneByPath(BartendingScenePath);
        if (existingBartendingScene.IsValid() && existingBartendingScene.isLoaded)
        {
            ReportProbeFailure("BARTENDING_ALREADY_LOADED", "Refusing to stack another BartendingScene instance.");
            onCompleted?.Invoke(false, "BARTENDING_ALREADY_LOADED");
            return false;
        }

        int generation = BeginAcceptedSession();
        Debug.Log(
            "[SURLHWA C4.2][Probe] generation=" + generation +
            " accepted. Before=false, during=" + IsBartendingActive);
        StartCoroutine(RunAdditiveLifecycleProbe(generation, onCompleted));
        return true;
    }

    private IEnumerator RunAdditiveLifecycleProbe(int generation, Action<bool, string> onCompleted)
    {
        AsyncOperation loadOperation = null;
        Exception loadException = null;
        try
        {
            loadOperation = SceneManager.LoadSceneAsync(BartendingScenePath, LoadSceneMode.Additive);
        }
        catch (Exception exception)
        {
            loadException = exception;
        }

        if (loadException != null)
        {
            yield return FailProbeWithCleanup(
                generation,
                "LOAD_OPERATION_NULL",
                "LoadSceneAsync threw: " + loadException.Message,
                onCompleted);
            yield break;
        }

        if (loadOperation == null)
        {
            yield return FailProbeWithCleanup(
                generation,
                "LOAD_OPERATION_NULL",
                "LoadSceneAsync returned null.",
                onCompleted);
            yield break;
        }

        yield return loadOperation;

        if (!IsCurrentSession(generation))
        {
            ReportProbeFailure("STALE_GENERATION", "Load completion belongs to stale generation " + generation + ".");
            onCompleted?.Invoke(false, "STALE_GENERATION");
            yield break;
        }

        Scene bartendingScene = SceneManager.GetSceneByPath(BartendingScenePath);
        if (!bartendingScene.IsValid())
        {
            yield return FailProbeWithCleanup(
                generation,
                "SCENE_INVALID",
                "GetSceneByPath returned an invalid BartendingScene.",
                onCompleted);
            yield break;
        }

        if (!bartendingScene.isLoaded)
        {
            yield return FailProbeWithCleanup(
                generation,
                "SCENE_NOT_LOADED",
                "BartendingScene is not loaded after additive load completion.",
                onCompleted);
            yield break;
        }

        if (SceneManager.GetActiveScene().path != gameObject.scene.path)
        {
            yield return FailProbeWithCleanup(
                generation,
                "ACTIVE_SCENE_CHANGED",
                "BarScene did not remain active during additive load.",
                onCompleted);
            yield break;
        }

        List<MiniGameManager> runners = FindMiniGameManagersInScene(bartendingScene);
        if (runners.Count != 1)
        {
            yield return FailProbeWithCleanup(
                generation,
                "RUNNER_COUNT_MISMATCH",
                "Expected exactly one BartendingScene MiniGameManager. Actual: " + runners.Count,
                onCompleted);
            yield break;
        }

        MiniGameManager runner = runners[0];
        if (runner == null || runner.gameObject.scene != bartendingScene)
        {
            yield return FailProbeWithCleanup(
                generation,
                "RUNNER_SCENE_MISMATCH",
                "Discovered MiniGameManager does not belong to the loaded BartendingScene.",
                onCompleted);
            yield break;
        }

        Debug.Log(
            "[SURLHWA C4.2][Probe] generation=" + generation +
            " loaded BartendingScene and resolved one scene-scoped runner. " +
            "IsBartendingActive=" + IsBartendingActive + "."
        );

        AsyncOperation unloadOperation = null;
        Exception unloadException = null;
        try
        {
            unloadOperation = SceneManager.UnloadSceneAsync(bartendingScene);
        }
        catch (Exception exception)
        {
            unloadException = exception;
        }

        if (unloadException != null)
        {
            yield return FailProbeWithCleanup(
                generation,
                "UNLOAD_OPERATION_NULL",
                "UnloadSceneAsync threw: " + unloadException.Message,
                onCompleted);
            yield break;
        }

        if (unloadOperation == null)
        {
            yield return FailProbeWithCleanup(
                generation,
                "UNLOAD_OPERATION_NULL",
                "UnloadSceneAsync returned null.",
                onCompleted);
            yield break;
        }

        yield return unloadOperation;

        if (!IsCurrentSession(generation))
        {
            ReportProbeFailure("STALE_GENERATION", "Unload completion belongs to stale generation " + generation + ".");
            onCompleted?.Invoke(false, "STALE_GENERATION");
            yield break;
        }

        Scene postUnloadScene = SceneManager.GetSceneByPath(BartendingScenePath);
        if (postUnloadScene.IsValid() && postUnloadScene.isLoaded)
        {
            yield return FailProbeWithCleanup(
                generation,
                "SCENE_STILL_LOADED",
                "BartendingScene remains loaded after UnloadSceneAsync completion.",
                onCompleted);
            yield break;
        }

        if (SceneManager.GetActiveScene().path != gameObject.scene.path)
        {
            yield return FailProbeWithCleanup(
                generation,
                "ACTIVE_SCENE_CHANGED",
                "BarScene did not remain active after BartendingScene unload.",
                onCompleted);
            yield break;
        }

        if (!TryCaptureCompletion(generation, "C4.2 additive lifecycle probe"))
        {
            onCompleted?.Invoke(false, "STALE_GENERATION");
            yield break;
        }

        if (!TryFinalizeSession(generation, "C4.2 additive lifecycle probe"))
        {
            onCompleted?.Invoke(false, "STALE_GENERATION");
            yield break;
        }

        if (IsBartendingActive)
        {
            ReportProbeFailure("ACTIVE_STATE_NOT_CLEARED", "Coordinator stayed active after probe finalization.");
            onCompleted?.Invoke(false, "ACTIVE_STATE_NOT_CLEARED");
            yield break;
        }

        Debug.Log(
            "[SURLHWA C4.2][PASS] generation=" + generation +
            " completed. After IsBartendingActive=" + IsBartendingActive + "."
        );
        onCompleted?.Invoke(true, "PASS");
    }

    private IEnumerator FailProbeWithCleanup(
        int generation,
        string code,
        string detail,
        Action<bool, string> onCompleted)
    {
        ReportProbeFailure(code, detail);

        if (!IsCurrentSession(generation))
        {
            ReportProbeFailure("STALE_GENERATION", "Failure handling belongs to stale generation " + generation + ".");
            onCompleted?.Invoke(false, "STALE_GENERATION");
            yield break;
        }

        Scene loadedBartendingScene = SceneManager.GetSceneByPath(BartendingScenePath);
        if (loadedBartendingScene.IsValid() && loadedBartendingScene.isLoaded)
        {
            AsyncOperation cleanupOperation;
            try
            {
                cleanupOperation = SceneManager.UnloadSceneAsync(loadedBartendingScene);
            }
            catch (Exception exception)
            {
                ReportProbeFailure("UNLOAD_OPERATION_NULL", "Failure cleanup threw: " + exception.Message);
                cleanupOperation = null;
            }

            if (cleanupOperation != null)
            {
                yield return cleanupOperation;
                Scene postCleanupScene = SceneManager.GetSceneByPath(BartendingScenePath);
                if (postCleanupScene.IsValid() && postCleanupScene.isLoaded)
                    ReportProbeFailure("SCENE_STILL_LOADED", "Failure cleanup left BartendingScene loaded.");
            }
            else
            {
                ReportProbeFailure("UNLOAD_OPERATION_NULL", "Failure cleanup could not start unload.");
            }
        }

        if (!TryFinalizeSession(generation, "C4.2 probe failure: " + code))
        {
            onCompleted?.Invoke(false, "STALE_GENERATION");
            yield break;
        }

        if (IsBartendingActive)
        {
            ReportProbeFailure("ACTIVE_STATE_NOT_CLEARED", "Coordinator stayed active after probe failure finalization.");
            onCompleted?.Invoke(false, "ACTIVE_STATE_NOT_CLEARED");
            yield break;
        }

        onCompleted?.Invoke(false, code);
    }

    private static void ReportProbeFailure(string code, string detail)
    {
        Debug.LogError("[SURLHWA C4.2][" + code + "] " + detail);
    }
#endif

    private int BeginAcceptedSession()
    {
        sessionGeneration++;
        activeSessionGeneration = sessionGeneration;
        completionCaptured = false;
        completionFinalized = false;
        isBartendingActive = true;
        return activeSessionGeneration;
    }

    private bool TryCaptureCompletion(int generation, string source)
    {
        if (!IsCurrentSession(generation))
        {
            Debug.LogWarning(
                "[BartendingFlowCoordinator] Stale completion ignored from " + source +
                " for generation=" + generation + ".");
            return false;
        }

        if (completionCaptured)
        {
            Debug.LogWarning(
                "[BartendingFlowCoordinator] Duplicate completion ignored from " + source +
                " for generation=" + generation + ".");
            return false;
        }

        completionCaptured = true;
        return true;
    }

    private bool TryFinalizeSession(int generation, string source)
    {
        if (!IsCurrentSession(generation))
        {
            Debug.LogWarning(
                "[BartendingFlowCoordinator] Stale finalization ignored from " + source +
                " for generation=" + generation + ".");
            return false;
        }

        if (completionFinalized)
        {
            Debug.LogWarning(
                "[BartendingFlowCoordinator] Duplicate finalization ignored from " + source +
                " for generation=" + generation + ".");
            return false;
        }

        completionFinalized = true;
        isBartendingActive = false;
        return true;
    }

    private bool IsCurrentSession(int generation)
    {
        return isBartendingActive && activeSessionGeneration == generation;
    }

    private bool ResolveLegacyBarMiniGameManager()
    {
        if (miniGameManager != null && miniGameManager.gameObject.scene == gameObject.scene)
            return true;

        List<MiniGameManager> candidates = FindMiniGameManagersInScene(gameObject.scene);
        if (candidates.Count != 1)
        {
            Debug.LogError(
                "[BartendingFlowCoordinator] Expected exactly one legacy MiniGameManager in the coordinator's BarScene. Actual: " +
                candidates.Count);
            return false;
        }

        miniGameManager = candidates[0];
        return true;
    }

    private static List<MiniGameManager> FindMiniGameManagersInScene(Scene scene)
    {
        List<MiniGameManager> matches = new List<MiniGameManager>();
        if (!scene.IsValid() || !scene.isLoaded)
            return matches;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (MiniGameManager manager in root.GetComponentsInChildren<MiniGameManager>(true))
            {
                if (manager != null && manager.gameObject.scene == scene)
                    matches.Add(manager);
            }
        }

        return matches;
    }
}
