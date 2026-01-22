public interface IMiniGame
{
    void StartGame(OrderData order, System.Action<MiniGameResult> onDone);
}