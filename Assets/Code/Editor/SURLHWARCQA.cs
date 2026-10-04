#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Opt-in command runner for an isolated RC project. Never activates in the owner's Editor.</summary>
[InitializeOnLoad]
public static class SURLHWARCQA
{
    const string Prefix = "SURLHWA_RC_QA_";
    static readonly string Folder = Path.GetFullPath("Logs/Review/RCAutomatedQA");
    static string logPath;
    static bool failed;
    static bool started;
    static double deadline;
    static SURLHWARCQA()
    {
        EditorApplication.playModeStateChanged += Changed;
        if (IsIsolated) EditorApplication.update += Poll;
        Application.logMessageReceived += Observe;
    }
    static bool IsIsolated => Application.companyName == "SURLHWA_RC_AUTOMATED_QA";
    public static void Boot()
    {
        if (!IsIsolated) throw new InvalidOperationException("RC automation requires the isolated project/save namespace.");
        Directory.CreateDirectory(Folder);
        Application.runInBackground = true;
        File.WriteAllText(Path.Combine(Folder, "ready.txt"), Application.unityVersion);
    }
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorUtility.scriptCompilationFailed) return;
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        EditorApplication.QueuePlayerLoopUpdate();
        if (!EditorApplication.isPlayingOrWillChangePlaymode && SessionState.GetBool(Prefix + "Entered", false))
        { Finish(); return; }
        if (started)
        {
            if (EditorApplication.timeSinceStartup > deadline)
            {
                failed = true;
                File.AppendAllText(logPath, "\nRC HARNESS WATCHDOG EXPIRED\n");
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
                else Finish();
            }
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        string request = Path.Combine(Folder, "request.txt");
        if (!File.Exists(request)) return;
        string command = File.ReadAllText(request).Trim();
        File.Delete(request);
        if (command == "exit") { EditorApplication.Exit(0); return; }
        if (command == "refresh") { AssetDatabase.Refresh(); return; }
        started = true; failed = false;
        SessionState.EraseBool(Prefix + "Terminal");
        deadline = EditorApplication.timeSinceStartup + 2400;
        SessionState.SetString(Prefix + "Command", command);
        logPath = Path.Combine(Folder, DateTime.UtcNow.ToString("yyyyMMddTHHmmss") + "-" + command + ".log");
        SessionState.SetString(Prefix + "Log", logPath);
        File.WriteAllText(logPath, "RC command=" + command + " UTC=" + DateTime.UtcNow.ToString("O") + "\n");
        File.WriteAllText(Path.Combine(Folder, "status.txt"), "RUNNING " + command);
        try
        {
            switch (command)
            {
                case "static":
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    SURLHWACampaignValidation.RunIntegrated();
                    SURLHWAJ4DiscoveryValidation.RunProductionRegression();
                    SURLHWAPhysicalBartendingValidation.RunRegression();
                    SURLHWACharacterMotionValidation.References();
                    SURLHWARCDataAudit.Run();
                    Finish(); break;
                case "build": SURLHWARCBuild.Run(); Finish(); break;
                case "focused":
                case "measurement":
                case "stress":
                    SessionState.SetBool(Prefix + "Focused", true);
                    EditorSceneManager.OpenScene("Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity");
                    EditorApplication.EnterPlaymode(); break;
                case "campaign": SURLHWACampaignPlayModeValidation.Run(); break;
                case "quiet": SURLHWACampaignPlayModeValidation.RunQuiet(); break;
                case "audio": SURLHWAAudioPlayModeValidation.Run(); break;
                case "motion": SURLHWACharacterMotionValidation.Run(); break;
                case "motion-safety": SURLHWACharacterMotionValidation.RunSafety(); break;
                case "physical": SURLHWAPhysicalBartendingPlayMode.Run(); break;
                case "ui": SURLHWAPhaseMUIValidation.RunVisualAfter(); break;
                case "ui-review": SURLHWAPhaseMUIValidation.Run(); break;
                case "modal": SURLHWAPhaseMUIValidation.RunModalSequence(); break;
                case "navigation": SURLHWAPhaseMUIValidation.RunNavigation(); break;
                case "j5": SURLHWAJ5PrinceClosurePlayModeValidation.Run(); break;
                default: throw new InvalidOperationException("Unknown RC command: " + command);
            }
        }
        catch (Exception e) { Debug.LogException(e); failed = true; Finish(); }
    }
    static void Changed(PlayModeStateChange state)
    {
        if (!IsIsolated) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            // The relocated disposable project has an invalid Search database.
            // Suppress only its pending Editor indexing callback, in this process;
            // no preferences or gameplay callbacks are changed. Otherwise its
            // startup exception contaminates legacy suites' exception observers.
            var callbacks = EditorApplication.delayCall;
            if (callbacks != null)
                foreach (var callback in callbacks.GetInvocationList())
                    if (callback.Method.DeclaringType?.FullName == "UnityEditor.Search.SearchInit" &&
                        callback.Method.Name == "IndexationOnStartup")
                    {
                        EditorApplication.delayCall -= (EditorApplication.CallbackFunction)callback;
                        Debug.Log("[RC environment] Skipped relocated Editor Search startup indexing only.");
                    }
            SessionState.SetBool(Prefix + "Entered", true);
            logPath = SessionState.GetString(Prefix + "Log", "");
            started = !string.IsNullOrEmpty(logPath);
            deadline = EditorApplication.timeSinceStartup + 2400;
            if (SessionState.GetBool(Prefix + "Focused", false))
            {
                SessionState.EraseBool(Prefix + "Focused");
                new GameObject("RC Automated Gameplay QA").AddComponent<SurlhwaRCGameplayQA>().Capture = SURLHWAReviewCapture.Save;
            }
        }
        if (state == PlayModeStateChange.EnteredEditMode && !string.IsNullOrEmpty(SessionState.GetString(Prefix + "Log", "")))
            Finish();
    }
    static void Observe(string message, string stack, LogType type)
    {
        string path = logPath ?? SessionState.GetString(Prefix + "Log", "");
        if (string.IsNullOrEmpty(path)) return;
        string command = SessionState.GetString(Prefix + "Command", "");
        string terminal = command switch
        {
            "static" => "[RC data] PASS",
            "build" => "[RC build] PASS",
            "focused" => "[RC QA] PASS scenarios=27",
            "measurement" => "[RC QA] PASS scenarios=4",
            "stress" => "[RC QA] PASS scenarios=4",
            "campaign" or "quiet" => "[Full Campaign] PASS: New Game through 47 natural departures",
            "audio" => "[Audio PlayMode] PASS:",
            "motion" or "motion-safety" => "[Phase N Motion] PASS:",
            "physical" => "[Phase K Play Mode] PASS:",
            "ui" => "[Visual Finish] PASS:",
            "modal" => "[Modal sequence] PASS four consecutive services",
            "navigation" => "[Phase M UI] navigation COMPLETE:",
            "ui-review" => "[Phase M UI] review COMPLETE:",
            "j5" => "[J5 PlayMode] PASS: production service handoff",
            _ => "__UNRECOGNIZED_SUITE__"
        };
        if (message.Contains(terminal)) SessionState.SetBool(Prefix + "Terminal", true);
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            // Unity's relocated Search index can fail during Editor startup.
            // Keep the complete evidence, separately classified from gameplay.
            if (type == LogType.Exception && stack.Contains("UnityEditor.Search.SearchDatabase") &&
                message.StartsWith("ArgumentOutOfRangeException", StringComparison.Ordinal))
                File.AppendAllText(path, "[ENVIRONMENT_ONLY] relocated Unity Editor Search index\n");
            else { failed = true; SessionState.SetBool(Prefix + "Failed", true); }
        }
        File.AppendAllText(path, "[" + type + "] " + message + (type == LogType.Exception ? "\n" + stack : "") + "\n");
    }
    static void Finish()
    {
        string command = SessionState.GetString(Prefix + "Command", "");
        if (string.IsNullOrEmpty(command)) return;
        bool success = !failed && !SessionState.GetBool(Prefix + "Failed", false) &&
            SessionState.GetBool(Prefix + "Terminal", false);
        string evidence = logPath ?? SessionState.GetString(Prefix + "Log", "");
        if (!SessionState.GetBool(Prefix + "Terminal", false) && !string.IsNullOrEmpty(evidence))
            File.AppendAllText(evidence, "RC terminal PASS evidence missing; suite cannot pass.\n");
        File.WriteAllText(Path.Combine(Folder, "status.txt"), (success ? "PASS " : "FAIL ") + command);
        SessionState.EraseString(Prefix + "Command"); SessionState.EraseString(Prefix + "Log");
        SessionState.EraseBool(Prefix + "Failed");
        SessionState.EraseBool(Prefix + "Entered");
        SessionState.EraseBool(Prefix + "Terminal");
        started = false; logPath = null; failed = false;
    }
}
#endif
