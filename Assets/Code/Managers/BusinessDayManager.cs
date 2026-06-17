using System.Collections.Generic;
using UnityEngine;

public class BusinessDayManager : MonoBehaviour
{
    [SerializeField] private int maxCustomersPerDay = 8;
    [SerializeField] private bool endDayWhenMaxCustomersServed = true;
    [SerializeField] private DayResultPanelController resultPanel;

    private int servedCustomerCount;
    private bool dayEnded;
    private GameManager gameManager;
    private RecipeUnlockManager recipeUnlockManager;

    public int ServedCustomerCount => servedCustomerCount;
    public int MaxCustomersPerDay => Mathf.Max(1, maxCustomersPerDay);
    public bool IsDayEnded => dayEnded;

    private void Awake()
    {
        ResolveReferences();
    }

    public bool CanSpawnMoreCustomers()
    {
        if (dayEnded)
            return false;

        if (!endDayWhenMaxCustomersServed)
            return true;

        return servedCustomerCount < MaxCustomersPerDay;
    }

    public void NotifyCustomerServedOrLeft()
    {
        if (dayEnded)
            return;

        servedCustomerCount++;

        if (endDayWhenMaxCustomersServed && servedCustomerCount >= MaxCustomersPerDay)
            EndDay();
    }

    private void EndDay()
    {
        if (dayEnded)
            return;

        dayEnded = true;
        ResolveReferences();

        if (resultPanel == null)
            resultPanel = DayResultPanelController.CreateRuntimePanel();

        if (resultPanel != null)
        {
            resultPanel.OpenResult(
                GetCurrentReputation(),
                servedCustomerCount,
                MaxCustomersPerDay,
                GetUnlockedRecipeNames()
            );
        }
    }

    private int GetCurrentReputation()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        return gameManager != null ? gameManager.reputation : 0;
    }

    private List<string> GetUnlockedRecipeNames()
    {
        if (recipeUnlockManager == null)
            recipeUnlockManager = FindFirstObjectByType<RecipeUnlockManager>();

        List<string> names = new List<string>();
        if (recipeUnlockManager == null)
            return names;

        List<CocktailRecipeData> recipes = recipeUnlockManager.GetAllRecipes();
        foreach (CocktailRecipeData recipe in recipes)
        {
            if (recipe == null || !recipeUnlockManager.IsUnlocked(recipe))
                continue;

            string displayName = string.IsNullOrWhiteSpace(recipe.displayName)
                ? recipe.recipeId
                : recipe.displayName;

            if (!string.IsNullOrWhiteSpace(displayName) && !names.Contains(displayName))
                names.Add(displayName);
        }

        return names;
    }

    private void ResolveReferences()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (recipeUnlockManager == null)
            recipeUnlockManager = FindFirstObjectByType<RecipeUnlockManager>();

        if (resultPanel == null)
            resultPanel = FindFirstObjectByType<DayResultPanelController>(FindObjectsInactive.Include);
    }
}
