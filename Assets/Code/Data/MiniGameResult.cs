using System;

[System.Serializable]
public struct MiniGameResult
{
    public bool success;
    public int repDelta;
    // Technique outcome or truthful preparation-complete state (for example, Build's "Complete").
    public string grade;

    public BrewComposition brewedComposition;
    // Immutable result-bound snapshot of the Serving Glass's optional garnish.
    // It is not part of liquid matching and does not replace container truth.
    public BartendingGarnishState garnish;
}
