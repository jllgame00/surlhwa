using System;
using UnityEngine;

public static class BrewCompositionFactory
{
    public static BrewComposition FromSession(BrewSessionData session, MiniGameType fallbackMethod)
    {
        if (session == null)
        {
            Debug.LogWarning($"[BrewCompositionFactory] Session is null. fallbackMethod={fallbackMethod}");

            return new BrewComposition
            {
                method = fallbackMethod,
                ingredients = Array.Empty<BrewIngredientEntry>()
            };
        }

        BrewSessionIngredient[] sourceIngredients = session.ingredients;
        int count = sourceIngredients != null ? sourceIngredients.Length : 0;

        BrewIngredientEntry[] entries = new BrewIngredientEntry[count];

        for (int i = 0; i < count; i++)
        {
            BrewSessionIngredient source = sourceIngredients[i];

            string ingredientId = "";
            float amount = 0f;

            if (source != null)
            {
                amount = Mathf.Max(0f, source.amount);

                if (source.ingredient != null)
                {
                    ingredientId = source.ingredient.ingredientId;
                }
                else
                {
                    Debug.LogWarning($"[BrewCompositionFactory] IngredientData is null at index {i}");
                }
            }
            else
            {
                Debug.LogWarning($"[BrewCompositionFactory] BrewSessionIngredient is null at index {i}");
            }

            entries[i] = new BrewIngredientEntry
            {
                ingredientId = ingredientId,
                amount = amount
            };
        }

        if (session.method != fallbackMethod)
        {
            Debug.LogWarning(
                $"[BrewCompositionFactory] Session method({session.method}) != Current mini game method({fallbackMethod}). " +
                $"Using current mini game method."
            );
        }

        return new BrewComposition
        {
            method = fallbackMethod,
            ingredients = entries
        };
    }
}