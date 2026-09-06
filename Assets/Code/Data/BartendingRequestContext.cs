/// <summary>
/// Transient request data required to execute a Bartending session.
/// The Bar projects this from its active OrderData at the scene boundary.
/// </summary>
public sealed class BartendingRequestContext
{
    public MiniGameType Route { get; }
    public string DisplayTitle { get; }

    public BartendingRequestContext(MiniGameType route, string displayTitle)
    {
        Route = route;
        DisplayTitle = displayTitle ?? string.Empty;
    }
}
