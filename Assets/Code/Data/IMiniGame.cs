using System;

public interface IMiniGame
{
    void StartGame(OrderData order, Action<MiniGameResult> onDone);
}
