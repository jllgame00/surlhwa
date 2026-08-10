using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SURLHWAAssetMigration_B2_2
{
    private const string MenuPath = "SURLHWA/Rebuild/Migrate/B2.2 Environment Tiles";

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

    private sealed class DependencyExpectation
    {
        public readonly string SourceOwner;
        public readonly string DestinationOwner;
        public readonly string SourceDependency;
        public readonly string DestinationDependency;

        public DependencyExpectation(
            string sourceOwner,
            string destinationOwner,
            string sourceDependency,
            string destinationDependency)
        {
            SourceOwner = sourceOwner;
            DestinationOwner = destinationOwner;
            SourceDependency = sourceDependency;
            DestinationDependency = destinationDependency;
        }
    }

    private static readonly MoveEntry[] Entries =
    {
        new MoveEntry(
            "Assets/texture/Tiles/bar_tileset_basic.png",
            "Assets/_SURLHWA/Art/Environment/Tiles/bar_tileset_basic.png"
        ),
        new MoveEntry(
            "Assets/texture/Tiles/Counter_Front.asset",
            "Assets/_SURLHWA/Art/Environment/Tiles/Counter_Front.asset"
        ),
        new MoveEntry(
            "Assets/texture/Tiles/isometric_tile_example.png",
            "Assets/_SURLHWA/Art/Environment/Tiles/isometric_tile_example.png"
        ),
        new MoveEntry(
            "Assets/texture/Tiles/isometric_tile_example.asset",
            "Assets/_SURLHWA/Art/Environment/Tiles/isometric_tile_example.asset"
        ),
        new MoveEntry(
            "Assets/texture/Tiles/isometric_wall_tile.png",
            "Assets/_SURLHWA/Art/Environment/Tiles/isometric_wall_tile.png"
        ),
        new MoveEntry(
            "Assets/texture/Tiles/wall.asset",
            "Assets/_SURLHWA/Art/Environment/Tiles/wall.asset"
        ),
    };

    private static readonly DependencyExpectation[] DependencyExpectations =
    {
        new DependencyExpectation(
            "Assets/texture/Tiles/Counter_Front.asset",
            "Assets/_SURLHWA/Art/Environment/Tiles/Counter_Front.asset",
            "Assets/texture/Tiles/bar_tileset_basic.png",
            "Assets/_SURLHWA/Art/Environment/Tiles/bar_tileset_basic.png"
        ),
        new DependencyExpectation(
            "Assets/texture/Tiles/isometric_tile_example.asset",
            "Assets/_SURLHWA/Art/Environment/Tiles/isometric_tile_example.asset",
            "Assets/texture/Tiles/isometric_tile_example.png",
            "Assets/_SURLHWA/Art/Environment/Tiles/isometric_tile_example.png"
        ),
        new DependencyExpectation(
            "Assets/texture/Tiles/wall.asset",
            "Assets/_SURLHWA/Art/Environment/Tiles/wall.asset",
            "Assets/texture/Tiles/isometric_wall_tile.png",
            "Assets/_SURLHWA/Art/Environment/Tiles/isometric_wall_tile.png"
        ),
    };

    [MenuItem(MenuPath)]
    private static void Migrate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[SURLHWA Migration B2.2] Run this migration in Edit Mode only.");
            return;
        }

        if (!ValidateSourcesAndDestinations(out Dictionary<string, string> guidSnapshot))
            return;

        if (!VerifyTileDependencies(useDestinationPaths: false))
            return;

        if (!EnsureTargetFolders())
            return;

        List<MoveEntry> movedEntries = new List<MoveEntry>();

        foreach (MoveEntry entry in Entries)
        {
            string error = AssetDatabase.MoveAsset(entry.Source, entry.Destination);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError(
                    $"[SURLHWA Migration B2.2] Move failed:\n{entry.Source}\n→ {entry.Destination}\n{error}"
                );
                RollBack(movedEntries);
                return;
            }

            string afterGuid = AssetDatabase.AssetPathToGUID(entry.Destination);
            if (!guidSnapshot.TryGetValue(entry.Source, out string beforeGuid) || afterGuid != beforeGuid)
            {
                Debug.LogError(
                    $"[SURLHWA Migration B2.2] GUID_PRESERVATION_FAILURE:\n{entry.Source}\n→ {entry.Destination}\n" +
                    $"Before: {beforeGuid}\nAfter: {afterGuid}"
                );
                movedEntries.Add(entry);
                RollBack(movedEntries);
                return;
            }

            movedEntries.Add(entry);
            Debug.Log(
                $"[SURLHWA Migration B2.2] GUID preserved: {entry.Source} → {entry.Destination} ({afterGuid})"
            );
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        if (!VerifyTileDependencies(useDestinationPaths: true))
        {
            RollBack(movedEntries);
            return;
        }

        Debug.Log($"[SURLHWA Migration B2.2] Complete. GUIDs matched: {movedEntries.Count}/{Entries.Length}.");
    }

    private static bool ValidateSourcesAndDestinations(out Dictionary<string, string> guidSnapshot)
    {
        guidSnapshot = new Dictionary<string, string>();
        bool valid = true;

        foreach (MoveEntry entry in Entries)
        {
            if (!File.Exists(ToAbsolutePath(entry.Source)) || !File.Exists(ToAbsolutePath(entry.Source + ".meta")))
            {
                Debug.LogError($"[SURLHWA Migration B2.2] Missing source asset or meta: {entry.Source}");
                valid = false;
                continue;
            }

            if (File.Exists(ToAbsolutePath(entry.Destination)) || File.Exists(ToAbsolutePath(entry.Destination + ".meta")))
            {
                Debug.LogError($"[SURLHWA Migration B2.2] Destination already exists: {entry.Destination}");
                valid = false;
                continue;
            }

            string guid = AssetDatabase.AssetPathToGUID(entry.Source);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"[SURLHWA Migration B2.2] Could not resolve source GUID: {entry.Source}");
                valid = false;
                continue;
            }

            guidSnapshot.Add(entry.Source, guid);
        }

        if (!valid)
            Debug.LogError("[SURLHWA Migration B2.2] Preflight failed. No assets were moved.");

        return valid;
    }

    private static bool VerifyTileDependencies(bool useDestinationPaths)
    {
        bool valid = true;

        foreach (DependencyExpectation expectation in DependencyExpectations)
        {
            string owner = useDestinationPaths ? expectation.DestinationOwner : expectation.SourceOwner;
            string dependency = useDestinationPaths ? expectation.DestinationDependency : expectation.SourceDependency;
            string[] dependencies = AssetDatabase.GetDependencies(owner, recursive: true);

            if (Array.IndexOf(dependencies, dependency) >= 0)
                continue;

            Debug.LogError(
                $"[SURLHWA Migration B2.2] Missing Tile-to-Sprite dependency:\n{owner}\n→ {dependency}"
            );
            valid = false;
        }

        if (!valid)
            Debug.LogError("[SURLHWA Migration B2.2] Tile dependency validation failed.");

        return valid;
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
            "Assets/_SURLHWA/Art/Environment",
            "Assets/_SURLHWA/Art/Environment/Tiles",
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
                Debug.LogError($"[SURLHWA Migration B2.2] Could not create target folder: {folder}");
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
                Debug.LogError($"[SURLHWA Migration B2.2] Rollback failed: {entry.Destination}\n→ {entry.Source}\n{error}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.LogWarning("[SURLHWA Migration B2.2] Migration stopped. Successfully moved assets were rolled back; created target folders were retained.");
    }
}
