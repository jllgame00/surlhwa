using System;

public interface IMiniGame
{
    void StartGame(OrderData order, Action<MiniGameResult> onDone);
    void StartGame(OrderData order, BrewSessionData brewSession, Action<MiniGameResult> onDone);
}