using System;

[Serializable]
public class BrewSessionData
{
    public MiniGameType method;
    public BrewSessionIngredient[] ingredients;
}

[Serializable]
public class BrewSessionIngredient
{
    public IngredientData ingredient;
    public float amount;
}