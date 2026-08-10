/// <summary>
/// Minimal Bar-to-Bartending execution contract.
/// It intentionally contains only Bartending input data and no Bar Scene object references.
/// </summary>
public sealed class BartendingSession
{
    public OrderData Order { get; }
    public MiniGameType Method { get; }
    public BrewSessionData InitialSession { get; }

    public BartendingSession(OrderData order, MiniGameType method, BrewSessionData initialSession)
    {
        Order = order;
        Method = method;
        InitialSession = initialSession;
    }

    public static BartendingSession FromOrder(OrderData order, BrewSessionData initialSession)
    {
        return new BartendingSession(
            order,
            order != null ? order.miniGameType : MiniGameType.Stir,
            initialSession);
    }
}
