using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-time C1 migration tool for the canonical production Bar scene.
/// This tool moves only the Scene asset, updates its two verified path consumers,
/// and verifies GUID and Scene YAML byte preservation.
/// </summary>
internal static class SURLHWASceneMigration_C1
{
    private const string SourceScenePath = "Assets/Scenes/SampleScene.unity";
    private const string TargetScenePath = "Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity";
    private const string ProjectSettingsRelativePath = "ProjectSettings/ProjectSettings.asset";

    private const string MigrationMenuPath = "SURLHWA/Rebuild/Migrate/C1 Canonical BarScene";
    private const string VerificationMenuPath = "SURLHWA/Rebuild/Verify/C1 Canonical BarScene";

    [MenuItem(MigrationMenuPath)]
    private static void Migrate()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[SURLHWA C1] Scene migration is blocked while Play Mode is active.");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "SURLHWA C1 — Canonical BarScene",
                "Move the production Scene through AssetDatabase?\n\n" +
                "From:\n" + SourceScenePath + "\n\n" +
                "To:\n" + TargetScenePath + "\n\n" +
                "The tool verifies Scene GUID, Scene YAML bytes, Build Settings index 0, and templateDefaultScene.",
                "Migrate",
                "Cancel"))
        {
            return;
        }

        bool sceneMoved = false;
        EditorBuildSettingsScene[] originalBuildScenes = null;
        byte[] originalProjectSettingsBytes = null;

        try
        {
            MigrationSnapshot snapshot = CreatePreflightSnapshot();
            originalBuildScenes = snapshot.BuildScenes;
            originalProjectSettingsBytes = snapshot.ProjectSettingsBytes;

            EnsureFolder("Assets/_SURLHWA");
            EnsureFolder("Assets/_SURLHWA/Scenes");
            EnsureFolder("Assets/_SURLHWA/Scenes/Gameplay");

            string moveError = AssetDatabase.MoveAsset(SourceScenePath, TargetScenePath);
            if (!string.IsNullOrEmpty(moveError))
            {
                throw new InvalidOperationException("AssetDatabase.MoveAsset failed: " + moveError);
            }

            sceneMoved = true;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            UpdateBuildSettings(snapshot.BuildScenes);
            UpdateTemplateDefaultScenePathIfNeeded(snapshot.ProjectSettingsBytes);

            ValidateCompletedMigration(snapshot);

            Debug.Log(
                "[SURLHWA C1] Complete.\n" +
                "GUID: " + snapshot.SceneGuid + "\n" +
                "Scene YAML bytes: unchanged\n" +
                "Build Settings: index 0 → " + TargetScenePath + "\n" +
                "templateDefaultScene: " + TargetScenePath);
        }
        catch (Exception exception)
        {
            Debug.LogError("[SURLHWA C1] Migration failed: " + exception.Message);

            if (sceneMoved)
            {
                TryRollback(originalBuildScenes, originalProjectSettingsBytes);
            }
        }
    }

    [MenuItem(VerificationMenuPath)]
    private static void VerifyCompletedMigration()
    {
        try
        {
            ValidatePostMigrationState();
            Debug.Log("[SURLHWA C1] Post-migration path validation passed.");
        }
        catch (Exception exception)
        {
            Debug.LogError("[SURLHWA C1] Post-migration validation failed: " + exception.Message);
        }
    }

    private static MigrationSnapshot CreatePreflightSnapshot()
    {
        Require(File.Exists(SourceScenePath), "Source Scene does not exist: " + SourceScenePath);
        Require(File.Exists(SourceScenePath + ".meta"), "Source Scene meta does not exist: " + SourceScenePath + ".meta");
        Require(!File.Exists(TargetScenePath), "Target Scene already exists: " + TargetScenePath);
        Require(!File.Exists(TargetScenePath + ".meta"), "Target Scene meta already exists: " + TargetScenePath + ".meta");

        string sourceGuid = AssetDatabase.AssetPathToGUID(SourceScenePath);
        Require(!string.IsNullOrEmpty(sourceGuid), "Source Scene GUID is empty.");

        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        Require(buildScenes.Length == 1, "C1 expects exactly one Build Settings Scene.");
        Require(buildScenes[0].enabled, "Build Settings Scene at index 0 must be enabled.");
        Require(buildScenes[0].path == SourceScenePath,
            "Build Settings Scene at index 0 must be the source Scene. Actual: " + buildScenes[0].path);

        string projectSettingsAbsolutePath = GetProjectSettingsAbsolutePath();
        Require(File.Exists(projectSettingsAbsolutePath), "ProjectSettings.asset is missing.");

        byte[] projectSettingsBytes = File.ReadAllBytes(projectSettingsAbsolutePath);
        byte[] oldTemplateEntry = Utf8Bytes("templateDefaultScene: " + SourceScenePath);
        byte[] newTemplateEntry = Utf8Bytes("templateDefaultScene: " + TargetScenePath);

        Require(CountOccurrences(projectSettingsBytes, oldTemplateEntry) == 1,
            "Expected exactly one old templateDefaultScene path before migration.");
        Require(CountOccurrences(projectSettingsBytes, newTemplateEntry) == 0,
            "Target templateDefaultScene path already exists before migration.");

        return new MigrationSnapshot(
            sourceGuid,
            File.ReadAllBytes(SourceScenePath),
            buildScenes,
            projectSettingsBytes);
    }

    private static void UpdateBuildSettings(EditorBuildSettingsScene[] preMigrationBuildScenes)
    {
        EditorBuildSettingsScene[] updatedScenes = preMigrationBuildScenes
            .Select(scene => new EditorBuildSettingsScene(
                scene.path == SourceScenePath ? TargetScenePath : scene.path,
                scene.enabled))
            .ToArray();

        EditorBuildSettings.scenes = updatedScenes;
    }

    private static void UpdateTemplateDefaultScenePathIfNeeded(byte[] originalProjectSettingsBytes)
    {
        string projectSettingsAbsolutePath = GetProjectSettingsAbsolutePath();
        byte[] currentBytes = File.ReadAllBytes(projectSettingsAbsolutePath);
        byte[] oldTemplateEntry = Utf8Bytes("templateDefaultScene: " + SourceScenePath);
        byte[] newTemplateEntry = Utf8Bytes("templateDefaultScene: " + TargetScenePath);

        int oldCount = CountOccurrences(currentBytes, oldTemplateEntry);
        int newCount = CountOccurrences(currentBytes, newTemplateEntry);

        if (oldCount == 0 && newCount == 1)
        {
            return;
        }

        Require(oldCount == 1 && newCount == 0,
            "templateDefaultScene has an unexpected value after Scene move.");

        byte[] updatedBytes = ReplaceSingleOccurrence(currentBytes, oldTemplateEntry, newTemplateEntry);
        File.WriteAllBytes(projectSettingsAbsolutePath, updatedBytes);

        Require(!originalProjectSettingsBytes.SequenceEqual(updatedBytes),
            "templateDefaultScene update did not change ProjectSettings bytes.");
    }

    private static void ValidateCompletedMigration(MigrationSnapshot snapshot)
    {
        Require(!File.Exists(SourceScenePath), "Old Scene path still exists after migration.");
        Require(!File.Exists(SourceScenePath + ".meta"), "Old Scene meta path still exists after migration.");
        Require(File.Exists(TargetScenePath), "Target Scene does not exist after migration.");
        Require(File.Exists(TargetScenePath + ".meta"), "Target Scene meta does not exist after migration.");

        string targetGuid = AssetDatabase.AssetPathToGUID(TargetScenePath);
        Require(targetGuid == snapshot.SceneGuid,
            "GUID_PRESERVATION_FAILURE. Expected " + snapshot.SceneGuid + ", actual " + targetGuid);

        byte[] targetSceneBytes = File.ReadAllBytes(TargetScenePath);
        Require(snapshot.SceneBytes.SequenceEqual(targetSceneBytes),
            "CONTENT_PRESERVATION_FAILURE. Scene YAML bytes changed during the path migration.");

        ValidatePostMigrationState();
    }

    private static void ValidatePostMigrationState()
    {
        Require(!File.Exists(SourceScenePath), "Old SampleScene path is present.");
        Require(File.Exists(TargetScenePath), "Canonical BarScene path is missing.");
        Require(File.Exists(TargetScenePath + ".meta"), "Canonical BarScene meta is missing.");

        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        Require(buildScenes.Length == 1, "Expected exactly one Build Settings Scene after migration.");
        Require(buildScenes[0].enabled, "Build Settings Scene at index 0 is disabled.");
        Require(buildScenes[0].path == TargetScenePath,
            "Build Settings Scene at index 0 is not the canonical BarScene. Actual: " + buildScenes[0].path);

        byte[] projectSettingsBytes = File.ReadAllBytes(GetProjectSettingsAbsolutePath());
        Require(CountOccurrences(projectSettingsBytes, Utf8Bytes("templateDefaultScene: " + SourceScenePath)) == 0,
            "ProjectSettings still references the old templateDefaultScene path.");
        Require(CountOccurrences(projectSettingsBytes, Utf8Bytes("templateDefaultScene: " + TargetScenePath)) == 1,
            "ProjectSettings does not reference the canonical templateDefaultScene path exactly once.");
    }

    private static void TryRollback(EditorBuildSettingsScene[] originalBuildScenes, byte[] originalProjectSettingsBytes)
    {
        try
        {
            if (File.Exists(TargetScenePath) && !File.Exists(SourceScenePath))
            {
                string rollbackError = AssetDatabase.MoveAsset(TargetScenePath, SourceScenePath);
                if (!string.IsNullOrEmpty(rollbackError))
                {
                    Debug.LogError("[SURLHWA C1] Rollback Scene move failed: " + rollbackError);
                }
            }

            if (originalBuildScenes != null)
            {
                EditorBuildSettings.scenes = originalBuildScenes;
            }

            if (originalProjectSettingsBytes != null)
            {
                File.WriteAllBytes(GetProjectSettingsAbsolutePath(), originalProjectSettingsBytes);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.LogWarning("[SURLHWA C1] Migration was rolled back to the original Scene path.");
        }
        catch (Exception rollbackException)
        {
            Debug.LogError("[SURLHWA C1] Rollback failed: " + rollbackException.Message);
        }
    }

    private static void EnsureFolder(string assetFolderPath)
    {
        if (AssetDatabase.IsValidFolder(assetFolderPath))
        {
            return;
        }

        string parentFolder = Path.GetDirectoryName(assetFolderPath)?.Replace('\\', '/');
        string folderName = Path.GetFileName(assetFolderPath);

        Require(!string.IsNullOrEmpty(parentFolder) && AssetDatabase.IsValidFolder(parentFolder),
            "Cannot create folder because its parent is missing: " + assetFolderPath);

        string createdGuid = AssetDatabase.CreateFolder(parentFolder, folderName);
        Require(!string.IsNullOrEmpty(createdGuid), "Failed to create folder: " + assetFolderPath);
    }

    private static string GetProjectSettingsAbsolutePath()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", ProjectSettingsRelativePath));
    }

    private static byte[] Utf8Bytes(string value)
    {
        return Encoding.UTF8.GetBytes(value);
    }

    private static int CountOccurrences(byte[] source, byte[] value)
    {
        int count = 0;

        for (int index = 0; index <= source.Length - value.Length; index++)
        {
            bool match = true;
            for (int offset = 0; offset < value.Length; offset++)
            {
                if (source[index + offset] != value[offset])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                count++;
                index += value.Length - 1;
            }
        }

        return count;
    }

    private static byte[] ReplaceSingleOccurrence(byte[] source, byte[] oldValue, byte[] newValue)
    {
        int occurrenceIndex = -1;

        for (int index = 0; index <= source.Length - oldValue.Length; index++)
        {
            bool match = true;
            for (int offset = 0; offset < oldValue.Length; offset++)
            {
                if (source[index + offset] != oldValue[offset])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                Require(occurrenceIndex < 0, "Expected one templateDefaultScene occurrence, found multiple.");
                occurrenceIndex = index;
                index += oldValue.Length - 1;
            }
        }

        Require(occurrenceIndex >= 0, "Expected templateDefaultScene occurrence was not found.");

        byte[] result = new byte[source.Length - oldValue.Length + newValue.Length];
        Buffer.BlockCopy(source, 0, result, 0, occurrenceIndex);
        Buffer.BlockCopy(newValue, 0, result, occurrenceIndex, newValue.Length);

        int sourceTailIndex = occurrenceIndex + oldValue.Length;
        int destinationTailIndex = occurrenceIndex + newValue.Length;
        Buffer.BlockCopy(source, sourceTailIndex, result, destinationTailIndex, source.Length - sourceTailIndex);

        return result;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class MigrationSnapshot
    {
        public string SceneGuid { get; }
        public byte[] SceneBytes { get; }
        public EditorBuildSettingsScene[] BuildScenes { get; }
        public byte[] ProjectSettingsBytes { get; }

        public MigrationSnapshot(
            string sceneGuid,
            byte[] sceneBytes,
            EditorBuildSettingsScene[] buildScenes,
            byte[] projectSettingsBytes)
        {
            SceneGuid = sceneGuid;
            SceneBytes = sceneBytes;
            BuildScenes = buildScenes;
            ProjectSettingsBytes = projectSettingsBytes;
        }
    }
}
