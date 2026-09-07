using UnityEditor;
using UnityEngine;

public static class SURLHWARegressionTools
{
    [MenuItem("SURLHWA/Rebuild Tests/Prepare Day 1 Tutorial")]
    private static void PrepareDay1Tutorial()
    {
        if (!CanModifyProgress())
            return;

        BusinessDayPersistence.SaveCurrentDay(1);
        LittlePrinceVisitPersistence.SetFirstVisitCompleted(false);

        Debug.Log(
            "[SURLHWA Regression] Prepared Day 1 Tutorial test.\n" +
            "CurrentDay = 1\n" +
            "LittlePrinceFirstVisitCompleted = false"
        );
    }

    [MenuItem("SURLHWA/Rebuild Tests/Prepare Day 2 Little Prince")]
    private static void PrepareDay2LittlePrince()
    {
        if (!CanModifyProgress())
            return;

        BusinessDayPersistence.SaveCurrentDay(2);
        LittlePrinceVisitPersistence.SetFirstVisitCompleted(false);

        Debug.Log(
            "[SURLHWA Regression] Prepared Day 2 Little Prince test.\n" +
            "CurrentDay = 2\n" +
            "LittlePrinceFirstVisitCompleted = false"
        );
    }

    [MenuItem("SURLHWA/Rebuild Tests/Print Current Progress")]
    private static void PrintCurrentProgress()
    {
        if (!CanModifyProgress())
            return;

        bool hasCurrentDay = BusinessDayPersistence.HasSavedCurrentDay();
        bool hasLittlePrinceCompletion = LittlePrinceVisitPersistence.HasSavedFirstVisitState();
        int currentDay = BusinessDayPersistence.LoadCurrentDay(1);
        bool littlePrinceFirstVisitCompleted =
            LittlePrinceVisitPersistence.IsFirstVisitCompleted();

        Debug.Log(
            "[SURLHWA Regression State]\n" +
            $"currentDay: {currentDay} (HasKey: {hasCurrentDay})\n" +
            "littlePrinceFirstVisitCompleted: " +
            $"{littlePrinceFirstVisitCompleted} (HasKey: {hasLittlePrinceCompletion})"
        );
    }

    private static bool CanModifyProgress()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
            return true;

        Debug.LogWarning(
            "[SURLHWA Regression] Progress tools are available only in Edit Mode. Exit Play Mode first."
        );
        return false;
    }
}
