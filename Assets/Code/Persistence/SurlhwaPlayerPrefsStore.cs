using UnityEngine;

/// <summary>
/// Concrete low-level access to Unity PlayerPrefs.
/// Domain-specific keys and defaults belong to domain persistence helpers.
/// </summary>
public static class SurlhwaPlayerPrefsStore
{
    public static bool HasKey(string key)
    {
        return PlayerPrefs.HasKey(key);
    }

    public static int GetInt(string key, int defaultValue)
    {
        return PlayerPrefs.GetInt(key, defaultValue);
    }

    public static void SetInt(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
    }

    public static string GetString(string key, string defaultValue)
    {
        return PlayerPrefs.GetString(key, defaultValue);
    }

    public static void SetString(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
    }

    public static void DeleteKey(string key)
    {
        PlayerPrefs.DeleteKey(key);
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }
}
