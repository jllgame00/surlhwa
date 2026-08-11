using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates and validates the C3 BartendingScene shell without changing BarScene routing.
/// The existing MiniGameManager is hosted under MiniGameRoot so its unchanged Instantiate
/// parent remains BartendingScene-owned until a later boundary-hardening phase introduces an explicit runtime-root field.
/// </summary>
internal static class SURLHWABartendingSceneMigration_C3
{
    private const string BarScenePath = "Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity";
    private const string BartendingScenePath = "Assets/_SURLHWA/Scenes/Gameplay/BartendingScene.unity";

    private const string StirPrefabPath = "Assets/_SURLHWA/Prefabs/Bartending/StirMiniGame.prefab";
    private const string ShakePrefabPath = "Assets/_SURLHWA/Prefabs/Bartending/ShakeMiniGame.prefab";
    private const string BuildPrefabPath = "Assets/_SURLHWA/Prefabs/Bartending/BuildMiniGame.prefab";
    private const string FloatPrefabPath = "Assets/_SURLHWA/Prefabs/Bartending/FloatMiniGame.prefab";

    private const string StirPrefabGuid = "081d8cfa995984442afe1762848abab9";
    private const string ShakePrefabGuid = "e17507ce645e9a04f98d08387d3f4665";
    private const string BuildPrefabGuid = "7a1a9bf5165ca3440bff0de6bbb3bda3";
    private const string FloatPrefabGuid = "182158c4b6a10f34b9f7881e0ab75fbe";

    private const string SystemsRootName = "_SYSTEMS";
    private const string GameplayRootName = "_GAMEPLAY";
    private const string UiRootName = "_UI";
    private const string CameraRootName = "_CAMERA";
    private const string MiniGameRootName = "MiniGameRoot";
    private const string RunnerName = "MiniGameManager";

    [MenuItem("SURLHWA/Rebuild/Migrate/C3 BartendingScene Shell")]
    private static void CreateBartendingSceneShell()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[SURLHWA C3] BartendingScene migration is blocked while Play Mode is active.");
            return;
        }

        try
        {
            PreflightShared();
            bool sceneAlreadyExists = AssetDatabase.LoadAssetAtPath<SceneAsset>(BartendingScenePath) != null ||
                                      File.Exists(BartendingScenePath);

            if (!EditorUtility.DisplayDialog(
                    "SURLHWA C3 - BartendingScene Shell",
                    sceneAlreadyExists
                        ? "Reuse the existing BartendingScene without changing its contents, validate the C3 shell, and align Build Settings?\n\n" +
                          "BarScene, production routing, additive loading, cameras, EventSystem, and Bar gameplay systems will not be changed."
                        : "Create the isolated BartendingScene shell with one local MiniGameManager and four existing production prefabs, then align Build Settings?\n\n" +
                          "BarScene, production routing, additive loading, cameras, EventSystem, and Bar gameplay systems will not be changed.",
                    sceneAlreadyExists ? "Validate and Align" : "Create and Align",
                    "Cancel"))
            {
                return;
            }

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene bartendingScene;
            if (sceneAlreadyExists)
            {
                Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(BartendingScenePath) != null,
                    "BartendingScene file exists but Unity cannot load it as a SceneAsset: " + BartendingScenePath);

                bartendingScene = GetOrOpenBartendingScene();
                ValidateBartendingSceneStructure(bartendingScene);
            }
            else
            {
                bartendingScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

                CreateRoot(bartendingScene, SystemsRootName);
                Transform gameplayRoot = CreateRoot(bartendingScene, GameplayRootName);
                CreateRoot(bartendingScene, UiRootName);
                CreateRoot(bartendingScene, CameraRootName);

                Transform miniGameRoot = CreateChild(bartendingScene, gameplayRoot, MiniGameRootName);
                MiniGameManager runner = CreateRunner(bartendingScene, miniGameRoot);

                EditorUtility.SetDirty(runner);
                EditorSceneManager.MarkSceneDirty(bartendingScene);
                EditorSceneManager.SaveScene(bartendingScene, BartendingScenePath);
                AssetDatabase.ImportAsset(BartendingScenePath, ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            AlignBuildSettings();
            ValidateBartendingScene(bartendingScene);

            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                SceneManager.SetActiveScene(previousActiveScene);

            Debug.Log(sceneAlreadyExists
                ? "[SURLHWA C3] Existing BartendingScene shell was reused and Build Settings were aligned."
                : "[SURLHWA C3] BartendingScene shell created and Build Settings were aligned. " +
                  "The local runner is hosted at _GAMEPLAY/MiniGameRoot; _SYSTEMS is reserved for C4 scene-local orchestration.");
        }
        catch (Exception exception)
        {
            Debug.LogError("[SURLHWA C3] BartendingScene migration failed: " + exception.Message + "\n" + DescribeBuildSettings());
        }
    }

    [MenuItem("SURLHWA/Rebuild/Verify/C3 BartendingScene Shell")]
    private static void VerifyBartendingSceneShell()
    {
        try
        {
            Scene bartendingScene = GetOrOpenBartendingScene();

            ValidateBartendingScene(bartendingScene);
            Debug.Log("[SURLHWA C3] BartendingScene shell validation passed.");
        }
        catch (Exception exception)
        {
            Debug.LogError("[SURLHWA C3] BartendingScene shell validation failed: " + exception.Message + "\n" + DescribeBuildSettings());
        }
    }

    private static void PreflightShared()
    {
        Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(BarScenePath) != null,
            "Canonical BarScene is missing: " + BarScenePath);

        RequirePrefab(StirPrefabPath, StirPrefabGuid);
        RequirePrefab(ShakePrefabPath, ShakePrefabGuid);
        RequirePrefab(BuildPrefabPath, BuildPrefabGuid);
        RequirePrefab(FloatPrefabPath, FloatPrefabGuid);

        RequireTemplateDefaultSceneIsBarScene();
    }

    private static Transform CreateRoot(Scene scene, string name)
    {
        GameObject root = new GameObject(name);
        SceneManager.MoveGameObjectToScene(root, scene);
        return root.transform;
    }

    private static Transform CreateChild(Scene scene, Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        SceneManager.MoveGameObjectToScene(child, scene);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static MiniGameManager CreateRunner(Scene scene, Transform miniGameRoot)
    {
        GameObject runnerObject = new GameObject(RunnerName);
        SceneManager.MoveGameObjectToScene(runnerObject, scene);
        runnerObject.transform.SetParent(miniGameRoot, false);

        MiniGameManager runner = runnerObject.AddComponent<MiniGameManager>();
        runner.stirPrefab = LoadPrefab(StirPrefabPath, StirPrefabGuid);
        runner.shakePrefab = LoadPrefab(ShakePrefabPath, ShakePrefabGuid);
        runner.buildPrefab = LoadPrefab(BuildPrefabPath, BuildPrefabGuid);
        runner.floatPrefab = LoadPrefab(FloatPrefabPath, FloatPrefabGuid);

        return runner;
    }

    private static Scene GetOrOpenBartendingScene()
    {
        Scene bartendingScene = SceneManager.GetSceneByPath(BartendingScenePath);
        if (!bartendingScene.isLoaded)
            bartendingScene = EditorSceneManager.OpenScene(BartendingScenePath, OpenSceneMode.Additive);

        return bartendingScene;
    }

    private static void AlignBuildSettings()
    {
        AssetDatabase.ImportAsset(BarScenePath, ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(BartendingScenePath, ImportAssetOptions.ForceSynchronousImport);

        List<EditorBuildSettingsScene> preservedEntries = new List<EditorBuildSettingsScene>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.path != BarScenePath && scene.path != BartendingScenePath)
                preservedEntries.Add(scene);
        }

        List<EditorBuildSettingsScene> canonicalEntries = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(BarScenePath, true),
            new EditorBuildSettingsScene(BartendingScenePath, true)
        };
        canonicalEntries.AddRange(preservedEntries);

        EditorBuildSettings.scenes = canonicalEntries.ToArray();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ValidateBuildSettings();
    }

    private static void ValidateBartendingScene(Scene scene)
    {
        ValidateBartendingSceneStructure(scene);
        ValidateBuildSettings();
        RequireTemplateDefaultSceneIsBarScene();
    }

    private static void ValidateBartendingSceneStructure(Scene scene)
    {
        Require(scene.IsValid() && scene.isLoaded && scene.path == BartendingScenePath,
            "BartendingScene is not loaded from its canonical path.");

        GameObject[] roots = scene.GetRootGameObjects();
        Require(roots.Length == 4, "BartendingScene must have exactly four root objects. Actual: " + roots.Length);

        Transform systemsRoot = FindSingleRoot(roots, SystemsRootName);
        Transform gameplayRoot = FindSingleRoot(roots, GameplayRootName);
        Transform uiRoot = FindSingleRoot(roots, UiRootName);
        Transform cameraRoot = FindSingleRoot(roots, CameraRootName);

        Require(systemsRoot.childCount == 0,
            "_SYSTEMS must remain empty in C3; the runner stays under MiniGameRoot for unchanged parent ownership.");
        Require(uiRoot.childCount == 0, "C3 must not duplicate Bar UI under _UI.");
        Require(cameraRoot.childCount == 0, "C3 must not add a scene-level Camera under _CAMERA.");

        Transform miniGameRoot = FindSingleDirectChild(gameplayRoot, MiniGameRootName);
        MiniGameManager runner = FindSingleComponent<MiniGameManager>(scene, RunnerName);
        Require(runner.transform.parent == miniGameRoot,
            "MiniGameManager must be parented to _GAMEPLAY/MiniGameRoot for BartendingScene runtime ownership.");

        ValidateRunnerPrefabs(runner);
        Require(GetObjectReference(runner, "mainHUD") == null, "C3 runner must not reference Bar mainHUD.");
        Require(GetObjectReference(runner, "mainWorld") == null, "C3 runner must not reference Bar mainWorld.");
        Require(GetObjectReference(runner, "mainCamera") == null, "C3 runner must not reference Bar mainCamera.");

        RequireNoComponents<CustomerSpawner>(scene, "CustomerSpawner");
        RequireNoComponents<BusinessDayManager>(scene, "BusinessDayManager");
        RequireNoComponents<SeatManager>(scene, "SeatManager");
        RequireNoComponents<OrderManager>(scene, "OrderManager");
        RequireNoComponents<BartendingFlowCoordinator>(scene, "BartendingFlowCoordinator");
        RequireNoComponents<InteractionUIController>(scene, "InteractionUIController");
        RequireNoComponents<RecipeUnlockManager>(scene, "RecipeUnlockManager");
        RequireNoComponents<EventSystem>(scene, "EventSystem");
        RequireNoComponents<Camera>(scene, "scene-level Camera");
        RequireNoComponents<AudioListener>(scene, "scene-level AudioListener");
    }

    private static void ValidateBuildSettings()
    {
        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        Require(buildScenes.Length >= 2 && buildScenes[0].enabled && buildScenes[0].path == BarScenePath,
            "Build Settings index 0 is not the enabled BarScene.\n" + DescribeBuildSettings());
        Require(buildScenes[1].enabled && buildScenes[1].path == BartendingScenePath,
            "Build Settings index 1 is not the enabled BartendingScene.\n" + DescribeBuildSettings());

        int barSceneCount = 0;
        int bartendingSceneCount = 0;
        foreach (EditorBuildSettingsScene scene in buildScenes)
        {
            if (scene.path == BarScenePath)
                barSceneCount++;
            else if (scene.path == BartendingScenePath)
                bartendingSceneCount++;
        }

        Require(barSceneCount == 1,
            "Build Settings must contain exactly one canonical BarScene entry. Actual: " + barSceneCount + "\n" +
            DescribeBuildSettings());
        Require(bartendingSceneCount == 1,
            "Build Settings must contain exactly one canonical BartendingScene entry. Actual: " + bartendingSceneCount + "\n" +
            DescribeBuildSettings());
    }

    private static string DescribeBuildSettings()
    {
        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        List<string> lines = new List<string>
        {
            "[SURLHWA C3] Current EditorBuildSettings entries:"
        };

        for (int index = 0; index < buildScenes.Length; index++)
        {
            EditorBuildSettingsScene scene = buildScenes[index];
            lines.Add(index + " | enabled=" + scene.enabled + " | " + scene.path);
        }

        return string.Join("\n", lines);
    }

    private static void ValidateRunnerPrefabs(MiniGameManager runner)
    {
        Require(runner.stirPrefab == LoadPrefab(StirPrefabPath, StirPrefabGuid), "Stir prefab reference is invalid.");
        Require(runner.shakePrefab == LoadPrefab(ShakePrefabPath, ShakePrefabGuid), "Shake prefab reference is invalid.");
        Require(runner.buildPrefab == LoadPrefab(BuildPrefabPath, BuildPrefabGuid), "Build prefab reference is invalid.");
        Require(runner.floatPrefab == LoadPrefab(FloatPrefabPath, FloatPrefabGuid), "Float prefab reference is invalid.");
    }

    private static GameObject LoadPrefab(string path, string expectedGuid)
    {
        RequirePrefab(path, expectedGuid);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static void RequirePrefab(string path, string expectedGuid)
    {
        Require(AssetDatabase.AssetPathToGUID(path) == expectedGuid,
            "Production prefab GUID mismatch: " + path);
        Require(AssetDatabase.LoadAssetAtPath<GameObject>(path) != null,
            "Production prefab is missing: " + path);
    }

    private static Transform FindSingleRoot(GameObject[] roots, string rootName)
    {
        List<Transform> matches = new List<Transform>();
        foreach (GameObject root in roots)
        {
            if (root.name == rootName)
                matches.Add(root.transform);
        }

        Require(matches.Count == 1, "Expected exactly one root named " + rootName + ". Actual: " + matches.Count);
        return matches[0];
    }

    private static Transform FindSingleDirectChild(Transform parent, string childName)
    {
        List<Transform> matches = new List<Transform>();
        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (child.name == childName)
                matches.Add(child);
        }

        Require(matches.Count == 1,
            "Expected exactly one child named " + childName + " under " + parent.name + ". Actual: " + matches.Count);
        return matches[0];
    }

    private static T FindSingleComponent<T>(Scene scene, string displayName) where T : Component
    {
        List<T> matches = FindSceneComponents<T>(scene);
        Require(matches.Count == 1, "Expected exactly one " + displayName + ". Actual: " + matches.Count);
        return matches[0];
    }

    private static void RequireNoComponents<T>(Scene scene, string displayName) where T : Component
    {
        Require(FindSceneComponents<T>(scene).Count == 0,
            "BartendingScene must not contain " + displayName + ".");
    }

    private static List<T> FindSceneComponents<T>(Scene scene) where T : Component
    {
        List<T> matches = new List<T>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (T component in root.GetComponentsInChildren<T>(true))
            {
                if (component.gameObject.scene == scene)
                    matches.Add(component);
            }
        }

        return matches;
    }

    private static UnityEngine.Object GetObjectReference(UnityEngine.Object target, string propertyName)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        Require(property != null,
            "Serialized property was not found: " + target.GetType().Name + "." + propertyName);
        return property.objectReferenceValue;
    }

    private static void RequireTemplateDefaultSceneIsBarScene()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string projectSettingsPath = Path.Combine(projectRoot, "ProjectSettings", "ProjectSettings.asset");
        string projectSettings = File.ReadAllText(projectSettingsPath);
        Require(projectSettings.Contains("templateDefaultScene: " + BarScenePath),
            "ProjectSettings.templateDefaultScene must remain BarScene.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
