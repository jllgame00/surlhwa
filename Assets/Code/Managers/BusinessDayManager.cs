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
    [SerializeField] private int[] demoCustomerCountsByDay = { 5, 4, 4, 4 };
    [SerializeField, Min(1)] private int demoNightCount = 4;
    [Header("Campaign Extension (optional)")]
    [SerializeField] private CampaignData campaign;

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
    public CampaignData Campaign => campaign;
    public bool HasCampaign => campaign != null && campaign.nights.Length > 0;
    public bool IsDemoComplete => !HasCampaign && DemoCompletionPersistence.IsDemoComplete();
    public bool IsConfiguredFinalDemoNight => !HasCampaign && CurrentDay >= DemoNightCount && !IsDemoComplete;

    private void Awake()
    {
        currentDay = Mathf.Max(1, BusinessDayPersistence.LoadCurrentDay(startingDay));

        // A completed legacy demo continues after its authored four nights.
        // Keep its completion key and Prince history; neither implies successful service.
        if (HasCampaign && DemoCompletionPersistence.IsDemoComplete() && currentDay <= DemoNightCount)
            currentDay = CampaignData.FirstExtendedNight;
        if (HasCampaign && currentDay > campaign.FinalNight)
            currentDay = CampaignData.FirstExtendedNight;
        if (HasCampaign && currentDay >= CampaignData.FirstExtendedNight)
            servedCustomerCount = CampaignProgressPersistence.LoadSlot(currentDay, MaxCustomersPerDay);

        // Existing saves from the former unbounded loop may already point past
        // the configured four-night demo. Treat them as completed rather than
        // reopening Day 5 or silently corrupting their saved day value.
        if (!HasCampaign && currentDay > DemoNightCount && !DemoCompletionPersistence.IsDemoComplete())
            DemoCompletionPersistence.MarkDemoComplete();

        ResolveReferences();
    }

    private void Start()
    {
        if (IsDemoComplete)
        {
            dayEnded = true;
            ShowCompletedDemoState();
            return;
        }

        CaptureKnownRecipesAtNightStart();
        if (HasCampaign && servedCustomerCount >= MaxCustomersPerDay)
            EndDay();
    }

    public bool CanSpawnMoreCustomers()
    {
        if (dayEnded || IsDemoComplete || (HasCampaign && CampaignProgressPersistence.EndingRead))
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
        if (HasCampaign && CurrentDay == campaign.FinalNight)
        {
            ResolveReferences();
            if (resultPanel == null) resultPanel = DayResultPanelController.CreateRuntimePanel();
            resultPanel.OpenCampaignEnding(campaign.ending);
            return;
        }
        int completedNightCustomerTarget = MaxCustomersPerDay;
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
                completedNightCustomerTarget,
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
        CampaignNight night = HasCampaign ? campaign.GetNight(normalizedDay) : null;
        if (night != null) return night.encounters.Length;
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
