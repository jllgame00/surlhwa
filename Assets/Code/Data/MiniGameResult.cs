using System;

[System.Serializable]
public struct MiniGameResult
{
    public bool success;
    public int repDelta;
    public string grade; // Perfect / Good / Fail

    public BrewComposition brewedComposition;
}