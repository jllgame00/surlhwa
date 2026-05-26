using System;

[Serializable]
public class BrewComposition
{
    public MiniGameType method;
    public BrewIngredientEntry[] ingredients;
}

[Serializable]
public class BrewIngredientEntry
{
    public string ingredientId;
    public float amount;
}