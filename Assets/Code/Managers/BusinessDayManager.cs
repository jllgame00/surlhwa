using System.Collections.Generic;
using UnityEngine;

public class BusinessDayManager : MonoBehaviour
{
    private const int DefaultDemoNightCount = 4;

    [SerializeField] private int maxCustomersPerDay = 8;
    [SerializeField] private bool endDayWhenMaxCustomersServed = true;
    [SerializeField] private DayResultPanelController resultPanel;
    [SerializeField, Min(1)] private int startingDay = 1;
    [Header("Bounded Demo Schedule")]
    [Tooltip("Customer targets for demo nights 1 through 4. Later content may tune these counts without changing day progression code.")]
    [SerializeField] private int[] demoCustomerCountsByDay = { 5, 8, 8, 8 };
    [SerializeField, Min(1)] private int demoNightCount = 4;

    private int servedCustomerCount;
    private bool dayEnded;
    private int currentDay;
    private GameManager gameManager;
    private RecipeUnlockManager recipeUnlockManager;
    private readonly HashSet<string> knownRecipeIdsAtNightStart = new();

    public int ServedCustomerCount => servedCustomerCount;
    public int MaxCustomersPerDay => GetCustomerTargetForDay(CurrentDay);
    public bool IsDayEnded => dayEnded;
    public int CurrentDay => Mathf.Max(1, currentDay);
    // Existing BarScene instances were serialized before this field existed.
    // Unity reads that missing scalar as zero, so retain the intended J1
    // four-night default without requiring an unrelated scene rewrite.
    public int DemoNightCount => demoNightCount > 0 ? demoNightCount : DefaultDemoNightCount;
    public bool IsDemoComplete => DemoCompletionPersistence.IsDemoComplete();
    public bool IsConfiguredFinalDemoNight => CurrentDay >= DemoNightCount && !IsDemoComplete;

    private void Awake()
    {
        currentDay = Mathf.Max(1, BusinessDayPersistence.LoadCurrentDay(startingDay));

        // Existing saves from the former unbounded loop may already point past
        // the configured four-night demo. Treat them as completed rather than
        // reopening Day 5 or silently corrupting their saved day value.
        if (currentDay > DemoNightCount && !DemoCompletionPersistence.IsDemoComplete())
            DemoCompletionPersistence.MarkDemoComplete();

        ResolveReferences();
    }

    private void Start()
    {
        if (DemoCompletionPersistence.IsDemoComplete())
        {
            dayEnded = true;
            ShowCompletedDemoState();
            return;
        }

        CaptureKnownRecipesAtNightStart();
    }

    public bool CanSpawnMoreCustomers()
    {
        if (dayEnded || DemoCompletionPersistence.IsDemoComplete())
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
        bool completedDemo = IsConfiguredFinalDemoNight;
        if (completedDemo)
        {
            DemoCompletionPersistence.MarkDemoComplete();
        }
        else
        {
            currentDay = CurrentDay + 1;
            BusinessDayPersistence.SaveCurrentDay(currentDay);
        }

        ResolveReferences();

        if (resultPanel == null)
            resultPanel = DayResultPanelController.CreateRuntimePanel();

        if (resultPanel != null)
        {
            resultPanel.OpenResult(
                GetCurrentReputation(),
                servedCustomerCount,
                MaxCustomersPerDay,
                GetNewlyKnownRecipeNames(),
                completedDemo
            );
        }
    }

    private void ShowCompletedDemoState()
    {
        ResolveReferences();

        if (resultPanel == null)
            resultPanel = DayResultPanelController.CreateRuntimePanel();

        resultPanel?.OpenDemoComplete();
    }

    private int GetCurrentReputation()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        return gameManager != null ? gameManager.reputation : 0;
    }

    private void CaptureKnownRecipesAtNightStart()
    {
        knownRecipeIdsAtNightStart.Clear();

        if (recipeUnlockManager == null)
            recipeUnlockManager = FindFirstObjectByType<RecipeUnlockManager>();

        if (recipeUnlockManager == null)
            return;

        foreach (CocktailRecipeData recipe in recipeUnlockManager.GetAllRecipes())
        {
            if (recipe != null && recipeUnlockManager.IsKnown(recipe) &&
                !string.IsNullOrWhiteSpace(recipe.recipeId))
            {
                knownRecipeIdsAtNightStart.Add(recipe.recipeId);
            }
        }
    }

    private List<string> GetNewlyKnownRecipeNames()
    {
        if (recipeUnlockManager == null)
            recipeUnlockManager = FindFirstObjectByType<RecipeUnlockManager>();

        List<string> names = new List<string>();
        if (recipeUnlockManager == null)
            return names;

        List<CocktailRecipeData> recipes = recipeUnlockManager.GetAllRecipes();
        foreach (CocktailRecipeData recipe in recipes)
        {
            if (recipe == null || !recipeUnlockManager.IsKnown(recipe) ||
                string.IsNullOrWhiteSpace(recipe.recipeId) ||
                knownRecipeIdsAtNightStart.Contains(recipe.recipeId))
                continue;

            CocktailData cocktail = recipe.targetCocktail;
            if (cocktail == null || string.IsNullOrWhiteSpace(cocktail.cocktailName))
            {
                Debug.LogError(
                    $"[BusinessDayManager] Recipe '{recipe.recipeId}' has no valid targetCocktail display data.");
                continue;
            }

            if (!names.Contains(cocktail.cocktailName))
                names.Add(cocktail.cocktailName);
        }

        return names;
    }

    private int GetCustomerTargetForDay(int day)
    {
        int normalizedDay = Mathf.Max(1, day);
        if (demoCustomerCountsByDay != null && normalizedDay <= demoCustomerCountsByDay.Length)
            return Mathf.Max(1, demoCustomerCountsByDay[normalizedDay - 1]);

        return Mathf.Max(1, maxCustomersPerDay);
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
