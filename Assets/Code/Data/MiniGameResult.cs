using System;

[System.Serializable]
public struct MiniGameResult
{
    public bool success;
    public int repDelta;
    // Technique outcome or truthful preparation-complete state (for example, Build's "Complete").
    public string grade;

    public BrewComposition brewedComposition;
}
