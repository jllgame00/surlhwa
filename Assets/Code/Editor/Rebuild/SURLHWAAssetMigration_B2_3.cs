using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SURLHWAAssetMigration_B2_3
{
    private const string MenuPath = "SURLHWA/Rebuild/Migrate/B2.3 Gameplay Data";
    private const int ExpectedEntryCount = 28;

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
        public readonly int AssetCount;

        public SourceFolderExpectation(string path, int assetCount)
        {
            Path = path;
            AssetCount = assetCount;
        }
    }

    private static readonly MoveEntry[] Entries =
    {
        new MoveEntry("Assets/Assets/Data/Ingredients/Ingredient_CoffeeLiqueurIngredient_CoffeeLiqueur.asset", "Assets/_SURLHWA/Data/Ingredients/Ingredient_CoffeeLiqueurIngredient_CoffeeLiqueur.asset"),
        new MoveEntry("Assets/Assets/Data/Ingredients/Ingredient_Cream.asset", "Assets/_SURLHWA/Data/Ingredients/Ingredient_Cream.asset"),
        new MoveEntry("Assets/Assets/Data/Ingredients/Ingredient_Gin.asset", "Assets/_SURLHWA/Data/Ingredients/Ingredient_Gin.asset"),
        new MoveEntry("Assets/Assets/Data/Ingredients/Ingredient_Lemon.asset", "Assets/_SURLHWA/Data/Ingredients/Ingredient_Lemon.asset"),
        new MoveEntry("Assets/Assets/Data/Ingredients/Ingredient_Lime.asset", "Assets/_SURLHWA/Data/Ingredients/Ingredient_Lime.asset"),
        new MoveEntry("Assets/Assets/Data/Ingredients/Ingredient_Rum.asset", "Assets/_SURLHWA/Data/Ingredients/Ingredient_Rum.asset"),
        new MoveEntry("Assets/Assets/Data/Ingredients/Ingredient_Syrup.asset", "Assets/_SURLHWA/Data/Ingredients/Ingredient_Syrup.asset"),
        new MoveEntry("Assets/Assets/Data/Ingredients/Ingredient_Tonic.asset", "Assets/_SURLHWA/Data/Ingredients/Ingredient_Tonic.asset"),
        new MoveEntry("Assets/Assets/Data/Ingredients/Ingredient_Vermouth.asset", "Assets/_SURLHWA/Data/Ingredients/Ingredient_Vermouth.asset"),
        new MoveEntry("Assets/Assets/Data/Ingredients/Ingredient_Vodka.asset", "Assets/_SURLHWA/Data/Ingredients/Ingredient_Vodka.asset"),
        new MoveEntry("Assets/Assets/Data/Ingredients/Ingredient_Water.asset", "Assets/_SURLHWA/Data/Ingredients/Ingredient_Water.asset"),
        new MoveEntry("Assets/Assets/Data/Ingredients/Ingredient_Whiskey.asset", "Assets/_SURLHWA/Data/Ingredients/Ingredient_Whiskey.asset"),
        new MoveEntry("Assets/Assets/Data/Recipes/Recipe_TestStir.asset", "Assets/_SURLHWA/Data/Recipes/Recipe_TestStir.asset"),
        new MoveEntry("Assets/Assets/Data/Recipes/Recipe_TestShake.asset", "Assets/_SURLHWA/Data/Recipes/Recipe_TestShake.asset"),
        new MoveEntry("Assets/Assets/Data/Recipes/Recipe_TestBuild.asset", "Assets/_SURLHWA/Data/Recipes/Recipe_TestBuild.asset"),
        new MoveEntry("Assets/Assets/Data/Recipes/Recipe_TestFloat.asset", "Assets/_SURLHWA/Data/Recipes/Recipe_TestFloat.asset"),
        new MoveEntry("Assets/Data/Cocktails/TestStirCocktail.asset", "Assets/_SURLHWA/Data/Cocktails/TestStirCocktail.asset"),
        new MoveEntry("Assets/Data/Cocktails/TestShakeCocktail.asset", "Assets/_SURLHWA/Data/Cocktails/TestShakeCocktail.asset"),
        new MoveEntry("Assets/Data/Cocktails/TestBuildCocktail.asset", "Assets/_SURLHWA/Data/Cocktails/TestBuildCocktail.asset"),
        new MoveEntry("Assets/Data/Cocktails/TestFloatCocktail.asset", "Assets/_SURLHWA/Data/Cocktails/TestFloatCocktail.asset"),
        new MoveEntry("Assets/Data/Orders/TestStir.asset", "Assets/_SURLHWA/Data/Orders/TestStir.asset"),
        new MoveEntry("Assets/Data/Orders/TestShake.asset", "Assets/_SURLHWA/Data/Orders/TestShake.asset"),
        new MoveEntry("Assets/Data/Orders/TestBuild.asset", "Assets/_SURLHWA/Data/Orders/TestBuild.asset"),
        new MoveEntry("Assets/Data/Orders/TestFloat.asset", "Assets/_SURLHWA/Data/Orders/TestFloat.asset"),
        new MoveEntry("Assets/Data/Orders/LittlePrinceFirstVisitOrder.asset", "Assets/_SURLHWA/Data/Orders/LittlePrinceFirstVisitOrder.asset"),
        new MoveEntry("Assets/Data/Dialogue/GeneralGuestReactionDB.asset", "Assets/_SURLHWA/Data/Dialogue/GeneralGuestReactionDB.asset"),
        new MoveEntry("Assets/Data/Dialogue/LittlePrinceFirstVisitDialogue.asset", "Assets/_SURLHWA/Data/Dialogue/LittlePrinceFirstVisitDialogue.asset"),
        new MoveEntry("Assets/Data/DialogueData.asset", "Assets/_SURLHWA/Data/Dialogue/DialogueData.asset"),
    };

    private static readonly SourceFolderExpectation[] SourceFolderExpectations =
    {
        new SourceFolderExpectation("Assets/Assets/Data/Ingredients", 12),
        new SourceFolderExpectation("Assets/Assets/Data/Recipes", 4),
        new SourceFolderExpectation("Assets/Data/Cocktails", 4),
        new SourceFolderExpectation("Assets/Data/Orders", 5),
        new SourceFolderExpectation("Assets/Data/Dialogue", 2),
    };

    [MenuItem(MenuPath)]
    private static void Migrate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[SURLHWA Migration B2.3] Run this migration in Edit Mode only.");
            return;
        }

        if (!ValidateManifestCounts())
            return;

        if (!ValidateSourcesAndDestinations(
                out Dictionary<string, string> guidSnapshot,
                out Dictionary<string, HashSet<string>> dependencySnapshot))
        {
            return;
        }

        if (!EnsureTargetFolders())
            return;

        List<MoveEntry> movedEntries = new List<MoveEntry>();

        foreach (MoveEntry entry in Entries)
        {
            string error = AssetDatabase.MoveAsset(entry.Source, entry.Destination);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError(
                    $"[SURLHWA Migration B2.3] Move failed:\n{entry.Source}\n→ {entry.Destination}\n{error}"
                );
                RollBack(movedEntries);
                return;
            }

            string afterGuid = AssetDatabase.AssetPathToGUID(entry.Destination);
            if (!guidSnapshot.TryGetValue(entry.Source, out string beforeGuid) || afterGuid != beforeGuid)
            {
                Debug.LogError(
                    $"[SURLHWA Migration B2.3] GUID_PRESERVATION_FAILURE:\n{entry.Source}\n→ {entry.Destination}\n" +
                    $"Before: {beforeGuid}\nAfter: {afterGuid}"
                );
                movedEntries.Add(entry);
                RollBack(movedEntries);
                return;
            }

            movedEntries.Add(entry);
            Debug.Log(
                $"[SURLHWA Migration B2.3] GUID preserved: {entry.Source} → {entry.Destination} ({afterGuid})"
            );
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        if (!VerifyDependencyGraphs(dependencySnapshot))
        {
            RollBack(movedEntries);
            return;
        }

        Debug.Log(
            $"[SURLHWA Migration B2.3] Complete. GUIDs and dependency graphs matched: {movedEntries.Count}/{Entries.Length}."
        );
    }

    private static bool ValidateManifestCounts()
    {
        if (Entries.Length != ExpectedEntryCount)
        {
            Debug.LogError(
                $"[SURLHWA Migration B2.3] MANIFEST_COUNT_MISMATCH: expected {ExpectedEntryCount} entries, found {Entries.Length}."
            );
            return false;
        }

        bool valid = true;

        foreach (SourceFolderExpectation expectation in SourceFolderExpectations)
        {
            string absolutePath = ToAbsolutePath(expectation.Path);
            int actualCount = Directory.Exists(absolutePath)
                ? Directory.GetFiles(absolutePath, "*.asset", SearchOption.TopDirectoryOnly).Length
                : 0;

            if (actualCount == expectation.AssetCount)
                continue;

            Debug.LogError(
                $"[SURLHWA Migration B2.3] MANIFEST_COUNT_MISMATCH: {expectation.Path} expected {expectation.AssetCount}, found {actualCount}."
            );
            valid = false;
        }

        if (!File.Exists(ToAbsolutePath("Assets/Data/DialogueData.asset")))
        {
            Debug.LogError("[SURLHWA Migration B2.3] Missing required root DialogueData.asset.");
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
                Debug.LogError($"[SURLHWA Migration B2.3] Missing source asset or meta: {entry.Source}");
                valid = false;
                continue;
            }

            if (File.Exists(ToAbsolutePath(entry.Destination)) || File.Exists(ToAbsolutePath(entry.Destination + ".meta")))
            {
                Debug.LogError($"[SURLHWA Migration B2.3] Destination already exists: {entry.Destination}");
                valid = false;
                continue;
            }

            string guid = AssetDatabase.AssetPathToGUID(entry.Source);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"[SURLHWA Migration B2.3] Could not resolve source GUID: {entry.Source}");
                valid = false;
                continue;
            }

            guidSnapshot.Add(entry.Source, guid);
            dependencySnapshot.Add(entry.Source, GetDependencyGuids(entry.Source));
        }

        if (!valid)
            Debug.LogError("[SURLHWA Migration B2.3] Preflight failed. No assets were moved.");

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
                $"[SURLHWA Migration B2.3] REFERENCE_PRESERVATION_FAILURE:\n{entry.Source}\n→ {entry.Destination}\n" +
                $"Dependency GUID count before: {before.Count}, after: {after.Count}"
            );
            valid = false;
        }

        if (!valid)
            Debug.LogError("[SURLHWA Migration B2.3] Dependency graph validation failed.");

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
            "Assets/_SURLHWA/Data",
            "Assets/_SURLHWA/Data/Cocktails",
            "Assets/_SURLHWA/Data/Dialogue",
            "Assets/_SURLHWA/Data/Ingredients",
            "Assets/_SURLHWA/Data/Orders",
            "Assets/_SURLHWA/Data/Recipes",
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
                Debug.LogError($"[SURLHWA Migration B2.3] Could not create target folder: {folder}");
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
                Debug.LogError($"[SURLHWA Migration B2.3] Rollback failed: {entry.Destination}\n→ {entry.Source}\n{error}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.LogWarning("[SURLHWA Migration B2.3] Migration stopped. Successfully moved assets were rolled back; created target folders were retained.");
    }
}
