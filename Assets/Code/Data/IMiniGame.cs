using System;

public interface IMiniGame
{
    void StartGame(Action<MiniGameResult> onDone);
}

public interface IRequestDisplayTitleReceiver
{
    void SetRequestDisplayTitle(string displayTitle);
}
