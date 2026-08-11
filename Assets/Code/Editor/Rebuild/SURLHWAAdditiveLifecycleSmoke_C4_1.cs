using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor-only Play Mode smoke for the C4.1 additive Scene lifecycle.
/// It never starts a MiniGame or changes production Bartending routing.
/// </summary>
internal static class SURLHWAAdditiveLifecycleSmoke_C4_1
{
    private const string MenuPath = "SURLHWA/Rebuild Tests/C4.1 Additive Lifecycle Smoke";
    private const string BarScenePath = "Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity";
    private const string BartendingScenePath = "Assets/_SURLHWA/Scenes/Gameplay/BartendingScene.unity";

    private enum LifecyclePhase
    {
        None,
        Loading,
        Unloading,
        CleaningUpAfterFailure
    }

    private static LifecyclePhase phase;
    private static AsyncOperation pendingOperation;
    private static Scene barScene;
    private static Scene bartendingScene;

    [MenuItem(MenuPath)]
    private static void RunSmoke()
    {
        if (phase != LifecyclePhase.None)
        {
            Debug.LogWarning("[SURLHWA C4.1][ALREADY_RUNNING] Additive lifecycle smoke is already running.");
            return;
        }

        if (!Application.isPlaying)
        {
            ReportFailure("NOT_IN_PLAY_MODE", "Enter Play Mode with BarScene active before running this test.");
            return;
        }

        barScene = SceneManager.GetSceneByPath(BarScenePath);
        Scene activeScene = SceneManager.GetActiveScene();
        Scene existingBartendingScene = SceneManager.GetSceneByPath(BartendingScenePath);

        if (!barScene.IsValid() || !barScene.isLoaded)
        {
            ReportFailure("BAR_SCENE_LOST", "BarScene is not loaded: " + BarScenePath);
            return;
        }

        if (activeScene.path != BarScenePath)
        {
            ReportFailure(
                "WRONG_ACTIVE_SCENE",
                "Expected active Scene: " + BarScenePath + " | Actual: " + DescribeScene(activeScene)
            );
            return;
        }

        if (existingBartendingScene.IsValid() && existingBartendingScene.isLoaded)
        {
            ReportFailure("BARTENDING_ALREADY_LOADED", "Refusing to stack another BartendingScene instance.");
            return;
        }

        try
        {
            pendingOperation = SceneManager.LoadSceneAsync(BartendingScenePath, LoadSceneMode.Additive);
        }
        catch (Exception exception)
        {
            ReportFailure("LOAD_OPERATION_NULL", "LoadSceneAsync threw: " + exception.Message);
            return;
        }

        if (pendingOperation == null)
        {
            ReportFailure("LOAD_OPERATION_NULL", "LoadSceneAsync returned null for " + BartendingScenePath);
            return;
        }

        phase = LifecyclePhase.Loading;
        EditorApplication.update += AdvanceLifecycle;

        Debug.Log(
            "[SURLHWA C4.1] Loading BartendingScene additively. " +
            "BarScene remains the active Scene; no MiniGame execution will occur."
        );
    }

    private static void AdvanceLifecycle()
    {
        if (!Application.isPlaying)
        {
            FinishRun();
            ReportFailure("NOT_IN_PLAY_MODE", "Play Mode ended before the lifecycle smoke completed.");
            return;
        }

        if (pendingOperation == null || !pendingOperation.isDone)
            return;

        switch (phase)
        {
            case LifecyclePhase.Loading:
                VerifyLoadedSceneAndBeginUnload();
                break;

            case LifecyclePhase.Unloading:
                VerifyPostUnload();
                break;

            case LifecyclePhase.CleaningUpAfterFailure:
                FinishRun();
                Debug.Log("[SURLHWA C4.1] Failure cleanup unloaded BartendingScene.");
                break;
        }
    }

    private static void VerifyLoadedSceneAndBeginUnload()
    {
        bartendingScene = SceneManager.GetSceneByPath(BartendingScenePath);

        if (!bartendingScene.IsValid())
        {
            FailAndCleanUp("SCENE_INVALID_AFTER_LOAD", "GetSceneByPath returned an invalid BartendingScene.");
            return;
        }

        if (!bartendingScene.isLoaded)
        {
            FailAndCleanUp("SCENE_NOT_LOADED_AFTER_LOAD", "BartendingScene is not loaded after LoadSceneAsync completion.");
            return;
        }

        if (!IsBarSceneStillLoaded())
        {
            FailAndCleanUp("BAR_SCENE_LOST", "BarScene was not loaded after additive load.");
            return;
        }

        if (!IsBarSceneStillActive())
        {
            FailAndCleanUp("ACTIVE_SCENE_CHANGED", "Additive load changed the active Scene: " + DescribeScene(SceneManager.GetActiveScene()));
            return;
        }

        List<MiniGameManager> runners = FindMiniGameManagersInScene(bartendingScene);
        if (runners.Count != 1)
        {
            FailAndCleanUp(
                "RUNNER_COUNT_MISMATCH",
                "Expected exactly one scene-scoped MiniGameManager. Actual: " + runners.Count
            );
            return;
        }

        MiniGameManager runner = runners[0];
        if (runner == null || runner.gameObject.scene.path != BartendingScenePath)
        {
            string actualScenePath = runner != null ? runner.gameObject.scene.path : "NULL";
            FailAndCleanUp(
                "RUNNER_SCENE_MISMATCH",
                "Discovered MiniGameManager belongs to: " + actualScenePath
            );
            return;
        }

        int barRunnerCount = FindMiniGameManagersInScene(barScene).Count;
        Debug.Log(
            "[SURLHWA C4.1] Scene-scoped runner discovery passed. " +
            "BarScene MiniGameManager count=" + barRunnerCount +
            ", BartendingScene MiniGameManager count=" + runners.Count +
            ". Beginning exact BartendingScene unload."
        );

        try
        {
            pendingOperation = SceneManager.UnloadSceneAsync(bartendingScene);
        }
        catch (Exception exception)
        {
            FailAndCleanUp("UNLOAD_OPERATION_NULL", "UnloadSceneAsync threw: " + exception.Message);
            return;
        }

        if (pendingOperation == null)
        {
            FailAndCleanUp("UNLOAD_OPERATION_NULL", "UnloadSceneAsync returned null for the loaded BartendingScene.");
            return;
        }

        phase = LifecyclePhase.Unloading;
    }

    private static void VerifyPostUnload()
    {
        Scene postUnloadBartendingScene = SceneManager.GetSceneByPath(BartendingScenePath);
        if (postUnloadBartendingScene.IsValid() && postUnloadBartendingScene.isLoaded)
        {
            bartendingScene = postUnloadBartendingScene;
            FailAndCleanUp("SCENE_STILL_LOADED_AFTER_UNLOAD", "BartendingScene remains loaded after UnloadSceneAsync completion.");
            return;
        }

        if (!IsBarSceneStillLoaded())
        {
            FinishRun();
            ReportFailure("BAR_SCENE_LOST", "BarScene was not loaded after BartendingScene unload.");
            return;
        }

        if (!IsBarSceneStillActive())
        {
            FinishRun();
            ReportFailure("ACTIVE_SCENE_CHANGED", "Active Scene changed after unload: " + DescribeScene(SceneManager.GetActiveScene()));
            return;
        }

        FinishRun();
        Debug.Log(
            "[SURLHWA C4.1][PASS] Additive lifecycle smoke passed. " +
            "BartendingScene was loaded, discovered through Scene-scoped traversal, and unloaded. " +
            "BarScene remained loaded and active."
        );
    }

    private static void FailAndCleanUp(string code, string detail)
    {
        ReportFailure(code, detail);

        if (!bartendingScene.IsValid() || !bartendingScene.isLoaded)
        {
            FinishRun();
            return;
        }

        try
        {
            pendingOperation = SceneManager.UnloadSceneAsync(bartendingScene);
        }
        catch (Exception exception)
        {
            FinishRun();
            Debug.LogError("[SURLHWA C4.1][UNLOAD_OPERATION_NULL] Failure cleanup threw: " + exception.Message);
            return;
        }

        if (pendingOperation == null)
        {
            FinishRun();
            Debug.LogError("[SURLHWA C4.1][UNLOAD_OPERATION_NULL] Failure cleanup returned null.");
            return;
        }

        phase = LifecyclePhase.CleaningUpAfterFailure;
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

    private static bool IsBarSceneStillLoaded()
    {
        Scene currentBarScene = SceneManager.GetSceneByPath(BarScenePath);
        return currentBarScene.IsValid() && currentBarScene.isLoaded;
    }

    private static bool IsBarSceneStillActive()
    {
        return SceneManager.GetActiveScene().path == BarScenePath;
    }

    private static void FinishRun()
    {
        EditorApplication.update -= AdvanceLifecycle;
        phase = LifecyclePhase.None;
        pendingOperation = null;
        bartendingScene = default;
        barScene = default;
    }

    private static void ReportFailure(string code, string detail)
    {
        Debug.LogError("[SURLHWA C4.1][" + code + "] " + detail);
    }

    private static string DescribeScene(Scene scene)
    {
        return "path=" + scene.path + ", valid=" + scene.IsValid() + ", loaded=" + scene.isLoaded;
    }
}
