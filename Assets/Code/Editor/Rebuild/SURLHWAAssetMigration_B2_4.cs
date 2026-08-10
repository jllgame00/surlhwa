using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SURLHWAAssetMigration_B2_4
{
    private const string MenuPath = "SURLHWA/Rebuild/Migrate/B2.4 Production Prefabs";
    private const int ExpectedEntryCount = 15;

    private sealed class MoveEntry
    {
        public readonly string Source;
        public readonly string Destination;

        public MoveEntry(string source, string destination)
        {
            Source = source;
            Destination = destination;
        }
    }

    private sealed class SourceFolderExpectation
    {
        public readonly string Path;
        public readonly int PrefabCount;

        public SourceFolderExpectation(string path, int prefabCount)
        {
            Path = path;
            PrefabCount = prefabCount;
        }
    }

    private static readonly MoveEntry[] Entries =
    {
        new MoveEntry("Assets/Assets/Prefab/UI/BrewIngredientOption.prefab", "Assets/_SURLHWA/Prefabs/UI/BrewIngredientOption.prefab"),
        new MoveEntry("Assets/Assets/Prefab/UI/CocktailCodexCard.prefab", "Assets/_SURLHWA/Prefabs/UI/CocktailCodexCard.prefab"),
        new MoveEntry("Assets/Assets/Prefab/UI/HeartPopup.prefab", "Assets/_SURLHWA/Prefabs/UI/HeartPopup.prefab"),
        new MoveEntry("Assets/Assets/Prefab/UI/IngredientShelfPanel.prefab", "Assets/_SURLHWA/Prefabs/UI/IngredientShelfPanel.prefab"),
        new MoveEntry("Assets/Assets/Prefab/UI/RecipeButtonPrefab.prefab", "Assets/_SURLHWA/Prefabs/UI/RecipeButtonPrefab.prefab"),
        new MoveEntry("Assets/Assets/Prefab/DialogueBubble.prefab", "Assets/_SURLHWA/Prefabs/UI/DialogueBubble.prefab"),
        new MoveEntry("Assets/Assets/Prefab/Funiture/Counter_Top.prefab", "Assets/_SURLHWA/Prefabs/Environment/Counter_Top.prefab"),
        new MoveEntry("Assets/Assets/Prefab/Funiture/Shelf.prefab", "Assets/_SURLHWA/Prefabs/Environment/Shelf.prefab"),
        new MoveEntry("Assets/Assets/Prefab/Funiture/Stool.prefab", "Assets/_SURLHWA/Prefabs/Environment/Stool.prefab"),
        new MoveEntry("Assets/Assets/Prefab/Minigame/StirMiniGame.prefab", "Assets/_SURLHWA/Prefabs/Bartending/StirMiniGame.prefab"),
        new MoveEntry("Assets/Assets/Prefab/Minigame/ShakeMiniGame.prefab", "Assets/_SURLHWA/Prefabs/Bartending/ShakeMiniGame.prefab"),
        new MoveEntry("Assets/Assets/Prefab/Minigame/BuildMiniGame.prefab", "Assets/_SURLHWA/Prefabs/Bartending/BuildMiniGame.prefab"),
        new MoveEntry("Assets/Assets/Prefab/Minigame/FloatMiniGame.prefab", "Assets/_SURLHWA/Prefabs/Bartending/FloatMiniGame.prefab"),
        new MoveEntry("Assets/Assets/Prefab/Character/Customer_A.prefab", "Assets/_SURLHWA/Prefabs/Customers/General/Customer_A.prefab"),
        new MoveEntry("Assets/Assets/Prefab/Character/LittlePrinceCustomer.prefab", "Assets/_SURLHWA/Prefabs/Customers/SpecialGuests/LittlePrinceCustomer.prefab"),
    };

    private static readonly SourceFolderExpectation[] SourceFolderExpectations =
    {
        new SourceFolderExpectation("Assets/Assets/Prefab/UI", 5),
        new SourceFolderExpectation("Assets/Assets/Prefab/Funiture", 3),
        new SourceFolderExpectation("Assets/Assets/Prefab/Minigame", 4),
        new SourceFolderExpectation("Assets/Assets/Prefab/Character", 3),
    };

    private static readonly string[] ExcludedAssets =
    {
        "Assets/Assets/Prefab/Character/Customer_B.prefab",
        "Assets/texture/Tiles/Table.prefab",
        "Assets/texture/Tiles/wall.prefab",
    };

    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/SampleScene.unity",
        "Assets/BarScene_LayoutTest.unity",
    };

    [MenuItem(MenuPath)]
    private static void Migrate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[SURLHWA Migration B2.4] Run this migration in Edit Mode only.");
            return;
        }

        if (!ValidateManifestCountsAndExcludedAssets())
            return;

        if (!ValidateSourcesAndDestinations(
                out Dictionary<string, string> guidSnapshot,
                out Dictionary<string, HashSet<string>> dependencySnapshot))
        {
            return;
        }

        Dictionary<string, int> sceneReferenceSnapshot = CaptureSceneReferenceCounts(guidSnapshot);

        if (!EnsureTargetFolders())
            return;

        List<MoveEntry> movedEntries = new List<MoveEntry>();

        foreach (MoveEntry entry in Entries)
        {
            string error = AssetDatabase.MoveAsset(entry.Source, entry.Destination);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError(
                    $"[SURLHWA Migration B2.4] Move failed:\n{entry.Source}\n→ {entry.Destination}\n{error}"
                );
                RollBack(movedEntries);
                return;
            }

            string afterGuid = AssetDatabase.AssetPathToGUID(entry.Destination);
            if (!guidSnapshot.TryGetValue(entry.Source, out string beforeGuid) || afterGuid != beforeGuid)
            {
                Debug.LogError(
                    $"[SURLHWA Migration B2.4] GUID_PRESERVATION_FAILURE:\n{entry.Source}\n→ {entry.Destination}\n" +
                    $"Before: {beforeGuid}\nAfter: {afterGuid}"
                );
                movedEntries.Add(entry);
                RollBack(movedEntries);
                return;
            }

            movedEntries.Add(entry);
            Debug.Log(
                $"[SURLHWA Migration B2.4] GUID preserved: {entry.Source} → {entry.Destination} ({afterGuid})"
            );
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        if (!VerifyDependencyGraphs(dependencySnapshot) ||
            !VerifySceneReferenceCounts(sceneReferenceSnapshot, guidSnapshot))
        {
            RollBack(movedEntries);
            return;
        }

        Debug.Log(
            $"[SURLHWA Migration B2.4] Complete. GUIDs, dependency graphs, and scene prefab references matched: {movedEntries.Count}/{Entries.Length}."
        );
    }

    private static bool ValidateManifestCountsAndExcludedAssets()
    {
        if (Entries.Length != ExpectedEntryCount)
        {
            Debug.LogError(
                $"[SURLHWA Migration B2.4] MANIFEST_COUNT_MISMATCH: expected {ExpectedEntryCount} entries, found {Entries.Length}."
            );
            return false;
        }

        bool valid = true;

        foreach (SourceFolderExpectation expectation in SourceFolderExpectations)
        {
            string absolutePath = ToAbsolutePath(expectation.Path);
            int actualCount = Directory.Exists(absolutePath)
                ? Directory.GetFiles(absolutePath, "*.prefab", SearchOption.TopDirectoryOnly).Length
                : 0;

            if (actualCount == expectation.PrefabCount)
                continue;

            Debug.LogError(
                $"[SURLHWA Migration B2.4] MANIFEST_COUNT_MISMATCH: {expectation.Path} expected {expectation.PrefabCount}, found {actualCount}."
            );
            valid = false;
        }

        if (!File.Exists(ToAbsolutePath("Assets/Assets/Prefab/DialogueBubble.prefab")))
        {
            Debug.LogError("[SURLHWA Migration B2.4] Missing required DialogueBubble.prefab.");
            valid = false;
        }

        foreach (string excludedAsset in ExcludedAssets)
        {
            if (File.Exists(ToAbsolutePath(excludedAsset)))
                continue;

            Debug.LogError($"[SURLHWA Migration B2.4] Missing excluded asset: {excludedAsset}");
            valid = false;
        }

        return valid;
    }

    private static bool ValidateSourcesAndDestinations(
        out Dictionary<string, string> guidSnapshot,
        out Dictionary<string, HashSet<string>> dependencySnapshot)
    {
        guidSnapshot = new Dictionary<string, string>();
        dependencySnapshot = new Dictionary<string, HashSet<string>>();
        bool valid = true;

        foreach (MoveEntry entry in Entries)
        {
            if (!File.Exists(ToAbsolutePath(entry.Source)) || !File.Exists(ToAbsolutePath(entry.Source + ".meta")))
            {
                Debug.LogError($"[SURLHWA Migration B2.4] Missing source prefab or meta: {entry.Source}");
                valid = false;
                continue;
            }

            if (File.Exists(ToAbsolutePath(entry.Destination)) || File.Exists(ToAbsolutePath(entry.Destination + ".meta")))
            {
                Debug.LogError($"[SURLHWA Migration B2.4] Destination already exists: {entry.Destination}");
                valid = false;
                continue;
            }

            string guid = AssetDatabase.AssetPathToGUID(entry.Source);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"[SURLHWA Migration B2.4] Could not resolve source GUID: {entry.Source}");
                valid = false;
                continue;
            }

            guidSnapshot.Add(entry.Source, guid);
            dependencySnapshot.Add(entry.Source, GetDependencyGuids(entry.Source));
        }

        if (!valid)
            Debug.LogError("[SURLHWA Migration B2.4] Preflight failed. No prefabs were moved.");

        return valid;
    }

    private static Dictionary<string, int> CaptureSceneReferenceCounts(Dictionary<string, string> guidSnapshot)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();

        foreach (string scenePath in ScenePaths)
        {
            string sceneText = File.ReadAllText(ToAbsolutePath(scenePath));

            foreach (string guid in guidSnapshot.Values)
            {
                string key = scenePath + "|" + guid;
                counts.Add(key, CountOccurrences(sceneText, "guid: " + guid));
            }
        }

        return counts;
    }

    private static bool VerifySceneReferenceCounts(
        Dictionary<string, int> beforeCounts,
        Dictionary<string, string> guidSnapshot)
    {
        Dictionary<string, int> afterCounts = CaptureSceneReferenceCounts(guidSnapshot);
        bool valid = true;

        foreach (KeyValuePair<string, int> before in beforeCounts)
        {
            if (afterCounts.TryGetValue(before.Key, out int after) && after == before.Value)
                continue;

            Debug.LogError(
                $"[SURLHWA Migration B2.4] REFERENCE_PRESERVATION_FAILURE: Scene prefab GUID count changed for {before.Key}.\n" +
                $"Before: {before.Value}, After: {after}"
            );
            valid = false;
        }

        if (!valid)
            Debug.LogError("[SURLHWA Migration B2.4] Scene prefab reference validation failed.");

        return valid;
    }

    private static bool VerifyDependencyGraphs(Dictionary<string, HashSet<string>> dependencySnapshot)
    {
        bool valid = true;

        foreach (MoveEntry entry in Entries)
        {
            HashSet<string> before = dependencySnapshot[entry.Source];
            HashSet<string> after = GetDependencyGuids(entry.Destination);

            if (before.SetEquals(after))
                continue;

            Debug.LogError(
                $"[SURLHWA Migration B2.4] REFERENCE_PRESERVATION_FAILURE:\n{entry.Source}\n→ {entry.Destination}\n" +
                $"Dependency GUID count before: {before.Count}, after: {after.Count}"
            );
            valid = false;
        }

        if (!valid)
            Debug.LogError("[SURLHWA Migration B2.4] Dependency graph validation failed.");

        return valid;
    }

    private static HashSet<string> GetDependencyGuids(string assetPath)
    {
        HashSet<string> guids = new HashSet<string>();
        string[] dependencies = AssetDatabase.GetDependencies(assetPath, true);

        foreach (string dependency in dependencies)
        {
            string guid = AssetDatabase.AssetPathToGUID(dependency);
            if (!string.IsNullOrEmpty(guid))
                guids.Add(guid);
        }

        return guids;
    }

    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int startIndex = 0;

        while ((startIndex = text.IndexOf(value, startIndex, StringComparison.Ordinal)) >= 0)
        {
            count++;
            startIndex += value.Length;
        }

        return count;
    }

    private static string ToAbsolutePath(string assetPath)
    {
        const string assetsPrefix = "Assets/";

        if (!assetPath.StartsWith(assetsPrefix, StringComparison.Ordinal))
            throw new ArgumentException($"Expected an Assets-relative path: {assetPath}", nameof(assetPath));

        return Path.Combine(Application.dataPath, assetPath.Substring(assetsPrefix.Length));
    }

    private static bool EnsureTargetFolders()
    {
        string[] folders =
        {
            "Assets/_SURLHWA/Prefabs",
            "Assets/_SURLHWA/Prefabs/UI",
            "Assets/_SURLHWA/Prefabs/Environment",
            "Assets/_SURLHWA/Prefabs/Bartending",
            "Assets/_SURLHWA/Prefabs/Customers",
            "Assets/_SURLHWA/Prefabs/Customers/General",
            "Assets/_SURLHWA/Prefabs/Customers/SpecialGuests",
        };

        foreach (string folder in folders)
        {
            if (AssetDatabase.IsValidFolder(folder))
                continue;

            int lastSlashIndex = folder.LastIndexOf('/');
            string parent = folder.Substring(0, lastSlashIndex);
            string name = folder.Substring(lastSlashIndex + 1);
            AssetDatabase.CreateFolder(parent, name);

            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogError($"[SURLHWA Migration B2.4] Could not create target folder: {folder}");
                return false;
            }
        }

        return true;
    }

    private static void RollBack(List<MoveEntry> movedEntries)
    {
        for (int index = movedEntries.Count - 1; index >= 0; index--)
        {
            MoveEntry entry = movedEntries[index];
            string error = AssetDatabase.MoveAsset(entry.Destination, entry.Source);
            if (!string.IsNullOrEmpty(error))
                Debug.LogError($"[SURLHWA Migration B2.4] Rollback failed: {entry.Destination}\n→ {entry.Source}\n{error}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.LogWarning("[SURLHWA Migration B2.4] Migration stopped. Successfully moved prefabs were rolled back; created target folders were retained.");
    }
}
