using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SURLHWAAssetMigration_B2_1
{
    private const string MenuPath = "SURLHWA/Rebuild/Migrate/B2.1 Art Assets";

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

    private static readonly MoveEntry[] Entries =
    {
        new MoveEntry("Assets/Assets/Art/bubble.png", "Assets/_SURLHWA/Art/UI/bubble.png"),
        new MoveEntry("Assets/Assets/Art/UI/도감.png", "Assets/_SURLHWA/Art/UI/도감.png"),
        new MoveEntry("Assets/Assets/Art/UI/말줄임표.png", "Assets/_SURLHWA/Art/UI/말줄임표.png"),
        new MoveEntry("Assets/Assets/Art/UI/빌드.png", "Assets/_SURLHWA/Art/UI/빌드.png"),
        new MoveEntry("Assets/Assets/Art/UI/설정.png", "Assets/_SURLHWA/Art/UI/설정.png"),
        new MoveEntry("Assets/Assets/Art/UI/셰이커.png", "Assets/_SURLHWA/Art/UI/셰이커.png"),
        new MoveEntry("Assets/Assets/Art/UI/스터.png", "Assets/_SURLHWA/Art/UI/스터.png"),
        new MoveEntry("Assets/Assets/Art/UI/제작.png", "Assets/_SURLHWA/Art/UI/제작.png"),
        new MoveEntry("Assets/Assets/Art/UI/플로트.png", "Assets/_SURLHWA/Art/UI/플로트.png"),
        new MoveEntry("Assets/Assets/Art/Cocktails/locked.png", "Assets/_SURLHWA/Art/Cocktails/locked.png"),
        new MoveEntry("Assets/Assets/Art/Cocktails/만월.png", "Assets/_SURLHWA/Art/Cocktails/만월.png"),
        new MoveEntry("Assets/Assets/Art/Cocktails/설야.png", "Assets/_SURLHWA/Art/Cocktails/설야.png"),
        new MoveEntry("Assets/Assets/Art/Cocktails/여명.png", "Assets/_SURLHWA/Art/Cocktails/여명.png"),
        new MoveEntry("Assets/Assets/Art/Cocktails/잔설.png", "Assets/_SURLHWA/Art/Cocktails/잔설.png"),
        new MoveEntry("Assets/Assets/Art/Minigame/Common/Props/barspoon.png", "Assets/_SURLHWA/Art/Bartending/Props/barspoon.png"),
        new MoveEntry("Assets/Assets/Art/Minigame/Common/Props/floatbarspoon.png", "Assets/_SURLHWA/Art/Bartending/Props/floatbarspoon.png"),
        new MoveEntry("Assets/Assets/Art/Minigame/Common/Props/glass.png", "Assets/_SURLHWA/Art/Bartending/Props/glass.png"),
        new MoveEntry("Assets/Assets/Art/Minigame/Common/Props/glass2.png", "Assets/_SURLHWA/Art/Bartending/Props/glass2.png"),
        new MoveEntry("Assets/Assets/Art/Minigame/Common/Props/jigger.png", "Assets/_SURLHWA/Art/Bartending/Props/jigger.png"),
        new MoveEntry("Assets/Assets/Art/Minigame/Common/Props/shaker.png", "Assets/_SURLHWA/Art/Bartending/Props/shaker.png"),
        new MoveEntry("Assets/Assets/Art/Minigame/Common/Props/shot.png", "Assets/_SURLHWA/Art/Bartending/Props/shot.png"),
        new MoveEntry("Assets/Assets/Art/Minigame/UI/Sprite_WhiteSquare.png", "Assets/_SURLHWA/Art/Bartending/UI/Sprite_WhiteSquare.png"),
        new MoveEntry("Assets/preset/sample_sprite.png", "Assets/_SURLHWA/Art/Bartending/UI/sample_sprite.png"),
    };

    [MenuItem(MenuPath)]
    private static void Migrate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[SURLHWA Migration B2.1] Run this migration in Edit Mode only.");
            return;
        }

        if (!ValidateSourcesAndDestinations(out Dictionary<string, string> guidSnapshot))
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
                    $"[SURLHWA Migration B2.1] Move failed:\n{entry.Source}\n→ {entry.Destination}\n{error}"
                );
                RollBack(movedEntries);
                return;
            }

            string afterGuid = AssetDatabase.AssetPathToGUID(entry.Destination);
            if (!guidSnapshot.TryGetValue(entry.Source, out string beforeGuid) || afterGuid != beforeGuid)
            {
                Debug.LogError(
                    $"[SURLHWA Migration B2.1] GUID_PRESERVATION_FAILURE:\n{entry.Source}\n→ {entry.Destination}\n" +
                    $"Before: {beforeGuid}\nAfter: {afterGuid}"
                );
                movedEntries.Add(entry);
                RollBack(movedEntries);
                return;
            }

            movedEntries.Add(entry);
            Debug.Log(
                $"[SURLHWA Migration B2.1] GUID preserved: {entry.Source} → {entry.Destination} ({afterGuid})"
            );
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log($"[SURLHWA Migration B2.1] Complete. GUIDs matched: {movedEntries.Count}/{Entries.Length}.");
    }

    private static bool ValidateSourcesAndDestinations(out Dictionary<string, string> guidSnapshot)
    {
        guidSnapshot = new Dictionary<string, string>();
        bool valid = true;

        foreach (MoveEntry entry in Entries)
        {
            if (!File.Exists(ToAbsolutePath(entry.Source)) || !File.Exists(ToAbsolutePath(entry.Source + ".meta")))
            {
                Debug.LogError($"[SURLHWA Migration B2.1] Missing source asset or meta: {entry.Source}");
                valid = false;
                continue;
            }

            if (File.Exists(ToAbsolutePath(entry.Destination)) || File.Exists(ToAbsolutePath(entry.Destination + ".meta")))
            {
                Debug.LogError($"[SURLHWA Migration B2.1] Destination already exists: {entry.Destination}");
                valid = false;
                continue;
            }

            string guid = AssetDatabase.AssetPathToGUID(entry.Source);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"[SURLHWA Migration B2.1] Could not resolve source GUID: {entry.Source}");
                valid = false;
                continue;
            }

            guidSnapshot.Add(entry.Source, guid);
        }

        if (!valid)
            Debug.LogError("[SURLHWA Migration B2.1] Preflight failed. No assets were moved.");

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
            "Assets/_SURLHWA",
            "Assets/_SURLHWA/Art",
            "Assets/_SURLHWA/Art/UI",
            "Assets/_SURLHWA/Art/Cocktails",
            "Assets/_SURLHWA/Art/Bartending",
            "Assets/_SURLHWA/Art/Bartending/Props",
            "Assets/_SURLHWA/Art/Bartending/UI",
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
                Debug.LogError($"[SURLHWA Migration B2.1] Could not create target folder: {folder}");
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
                Debug.LogError($"[SURLHWA Migration B2.1] Rollback failed: {entry.Destination}\n→ {entry.Source}\n{error}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.LogWarning("[SURLHWA Migration B2.1] Migration stopped. Successfully moved assets were rolled back; created target folders were retained.");
    }
}
