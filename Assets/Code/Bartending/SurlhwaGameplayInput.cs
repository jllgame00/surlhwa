using UnityEngine;

/// <summary>
/// Raw legacy input only. Modal permission and gameplay state remain with their
/// existing owners. Validation substitutes a device, never a gameplay command.
/// </summary>
public static class SurlhwaGameplayInput
{
#if UNITY_EDITOR || SURLHWA_RC_QA
    public interface ISource
    {
        bool GetKey(KeyCode key);
        bool GetKeyDown(KeyCode key);
        bool GetKeyUp(KeyCode key);
        Vector3 MousePosition { get; }
    }

    private static ISource source;
    public static string SourceName => source == null ? "Unity legacy input" : source.GetType().Name;
    public static System.IDisposable UseForValidation(ISource replacement)
    {
        if (replacement == null || source != null)
            throw new System.InvalidOperationException("Validation input requires one explicit owner.");
        source = replacement;
        return new Lease(replacement);
    }

    private sealed class Lease : System.IDisposable
    {
        private readonly ISource owner;
        public Lease(ISource owner) => this.owner = owner;
        public void Dispose() { if (ReferenceEquals(source, owner)) source = null; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => source = null;
#endif

    public static bool GetKey(KeyCode key)
    {
#if UNITY_EDITOR || SURLHWA_RC_QA
        if (source != null) return source.GetKey(key);
#endif
        return Input.GetKey(key);
    }
    public static bool GetKeyDown(KeyCode key)
    {
#if UNITY_EDITOR || SURLHWA_RC_QA
        if (source != null) return source.GetKeyDown(key);
#endif
        return Input.GetKeyDown(key);
    }
    public static bool GetKeyUp(KeyCode key)
    {
#if UNITY_EDITOR || SURLHWA_RC_QA
        if (source != null) return source.GetKeyUp(key);
#endif
        return Input.GetKeyUp(key);
    }
    public static Vector3 MousePosition
    {
        get
        {
#if UNITY_EDITOR || SURLHWA_RC_QA
            if (source != null) return source.MousePosition;
#endif
            return Input.mousePosition;
        }
    }
}
