using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SURLHWAOrderDefaultBrewSessionCleanup_F2F
{
    private const string LegacyFieldName = "defaultBrewSession:";

    private static readonly string[] ExpectedTargetPaths =
    {
        "Assets/_SURLHWA/Data/Orders/TestStir.asset",
        "Assets/_SURLHWA/Data/Orders/TestShake.asset",
        "Assets/_SURLHWA/Data/Orders/TestBuild.asset",
        "Assets/_SURLHWA/Data/Orders/TestFloat.asset",
        "Assets/_SURLHWA/Data/Orders/LittlePrinceFirstVisitOrder.asset"
    };

    [MenuItem("SURLHWA/Rebuild/Migrate/F2F Remove Order Default Brew Session")]
    public static void RunFromMenu()
    {
        RunCleanup();
        Debug.Log("[SURLHWA F2F] OrderData defaultBrewSession cleanup completed.");
    }

    public static void RunBatchMode()
    {
        RunCleanup();
        Debug.Log("[SURLHWA F2F] OrderData defaultBrewSession cleanup completed.");
    }

    private static void RunCleanup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("[SURLHWA F2F] Migration cannot run while entering or in Play Mode.");

        HashSet<string> expectedTargets = new HashSet<string>(ExpectedTargetPaths, StringComparer.Ordinal);
        List<string> targetsRequiringCleanup = FindOrderDataAssetsWithLegacyField();
        HashSet<string> actualTargets = new HashSet<string>(targetsRequiringCleanup, StringComparer.Ordinal);

        if (actualTargets.Count == 0)
            return;

        if (!actualTargets.SetEquals(expectedTargets))
        {
            throw new InvalidOperationException(
                "[SURLHWA F2F] Unexpected OrderData legacy-field target set. Expected: " +
                string.Join(", ", ExpectedTargetPaths) +
                " / Actual: " + string.Join(", ", targetsRequiringCleanup));
        }

        foreach (string path in targetsRequiringCleanup)
        {
            if (AssetDatabase.LoadAssetAtPath<OrderData>(path) == null)
                throw new InvalidOperationException("[SURLHWA F2F] Target is not an OrderData asset: " + path);
        }

        AssetDatabase.ForceReserializeAssets(targetsRequiringCleanup);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        if (FindOrderDataAssetsWithLegacyField().Count != 0)
            throw new InvalidOperationException("[SURLHWA F2F] Serialized defaultBrewSession data remains after migration.");
    }

    private static List<string> FindOrderDataAssetsWithLegacyField()
    {
        List<string> matches = new List<string>();
        string[] guids = AssetDatabase.FindAssets("t:OrderData");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".asset", StringComparison.Ordinal))
                continue;

            if (File.ReadAllText(path).Contains(LegacyFieldName))
                matches.Add(path);
        }

        matches.Sort(StringComparer.Ordinal);
        return matches;
    }
}
