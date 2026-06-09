using UnityEngine;
using System;

public class OrderManager : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool verboseLogging = false;
    [SerializeField] private bool allowPlayWithoutCustomer = true;
    
    [Header("Result Toast")]
    [SerializeField] private ResultToastController resultToast;

    // ====== Runtime State ======
    private CustomerController _currentCustomer;
    private CustomerController currentCustomer
    {
        get => _currentCustomer;
        set
        {
            if (verboseLogging && _currentCustomer != value)
            {
                Debug.Log(
                    $"[OrderManager] currentCustomer change: " +
                    $"{(_currentCustomer ? _currentCustomer.name : "NULL")} -> {(value ? value.name : "NULL")}\n" +
                    $"{Environment.StackTrace}"
                );
            }
            _currentCustomer = value;
        }
    }

    private OrderData currentOrder;

    private UIManager ui;
    private GameManager gm;

    [Header("Orders")]
    public OrderData[] orderPool;

    [Header("MiniGame")]
    [SerializeField] private MiniGameManager miniGameManager;

    private RecipeUnlockManager recipeUnlockManager;

    private RecipeUnlockPopupController unlockPopup;

    private bool HasActiveOrder => currentOrder != null;

    void Awake()
    {
        ui = FindFirstObjectByType<UIManager>();
        gm = FindFirstObjectByType<GameManager>();

        if (miniGameManager == null)
            miniGameManager = FindFirstObjectByType<MiniGameManager>();
        
        if (resultToast == null)
            resultToast = FindFirstObjectByType<ResultToastController>();

        recipeUnlockManager = FindFirstObjectByType<RecipeUnlockManager>();

        unlockPopup = FindFirstObjectByType<RecipeUnlockPopupController>();

        Debug.Log($"[OrderManager] Awake name={name} id={GetInstanceID()}");
    }

    public void IssueOrderTo(CustomerController cc)
    {
        if (cc == null)
        {
            Debug.LogError("[OrderManager] IssueOrderTo called with NULL customer");
            return;
        }

        // �̹� �ֹ��� ������ �ݺ� �߱� ����
        if (HasActiveOrder)
        {
            if (currentCustomer == cc) return; // ���� �մ��� ��� Update���� ȣ���ϴ� �� ����
            Debug.LogWarning($"[OrderManager] Another order is already active (order={currentOrder?.orderName}). Ignoring new issue from {cc.name}");
            return;
        }

        if (orderPool == null || orderPool.Length == 0)
        {
            Debug.LogError("[OrderManager] orderPool is empty!");
            return;
        }

        currentCustomer = cc;
        currentOrder = orderPool[UnityEngine.Random.Range(0, orderPool.Length)];

        Debug.Log($"[OrderManager] IssueOrderTo SET customer={currentCustomer.name} order={currentOrder.orderName}");

        if (ui != null)
        {
            Debug.Log($"[OrderManager] IssueOrderTo -> setting ORDER text = {currentOrder.orderName}");
            ui.SetOrderText($"ORDER: {currentOrder.orderName}");
        }
    }

    // UI ��ư���� ȣ��
    public void ResolveServe()
    {
        Debug.Log($"[OrderManager] ResolveServe id={GetInstanceID()} called");

        if (currentOrder == null)
        {
            Debug.LogError("[OrderManager] currentOrder is NULL (no active order)");
            return;
        }

        if (miniGameManager == null)
        {
            Debug.LogError("[OrderManager] miniGameManager is NULL (assign it in Inspector or place it in the scene)");
            return;
        }

        if (currentCustomer == null)
        {
            Debug.LogWarning("[OrderManager] currentCustomer is NULL (no active customer)");

            if (!allowPlayWithoutCustomer)
                return;

            Debug.LogWarning("[OrderManager] allowPlayWithoutCustomer=true -> still playing minigame for debugging");
        }

        Debug.Log($"[OrderManager] -> MiniGameManager.Play({currentOrder.orderName})");
        miniGameManager.Play(currentOrder, OnMiniGameDone);
    }

    private void OnMiniGameDone(MiniGameResult result)
    {
        Debug.Log($"[OrderManager] MiniGame Done => success={result.success}, repDelta={result.repDelta}, grade={result.grade}");

        if (result.brewedComposition != null)
        {
            Debug.Log($"[OrderManager] Brewed Method = {result.brewedComposition.method}");

            if (result.brewedComposition.ingredients != null)
            {
                foreach (var ingredient in result.brewedComposition.ingredients)
                {
                    Debug.Log($"[OrderManager] Brewed Ingredient = {ingredient.ingredientId} / amount={ingredient.amount:0.##} oz");
                }
            }
        }
        else
        {
            Debug.LogWarning("[OrderManager] brewedComposition is NULL");
        }

        CocktailData servedCocktail = null;
        CocktailRecipeData matchedRecipe = null;
        bool unlockedNewRecipe = false;

        if (recipeUnlockManager != null && result.brewedComposition != null)
        {
            var candidates = recipeUnlockManager.GetAllRecipesByMethod(result.brewedComposition.method);
            RecipeMatchResult matchResult = RecipeMatchService.FindBestMatch(result.brewedComposition, candidates);

            if (matchResult.success && matchResult.matchedRecipe != null)
            {
                matchedRecipe = matchResult.matchedRecipe;
                servedCocktail = matchedRecipe.targetCocktail;

                Debug.Log(
                    $"[OrderManager] Recipe Match SUCCESS -> " +
                    $"{matchedRecipe.displayName} / similarity={matchResult.similarity:0.00}"
                );

                bool wasUnlocked = recipeUnlockManager.IsUnlocked(matchedRecipe);

                if (!wasUnlocked)
                {
                    bool newlyUnlocked = recipeUnlockManager.UnlockRecipe(matchedRecipe);

                    if (newlyUnlocked)
                    {
                        unlockedNewRecipe = true;

                        Debug.Log($"[OrderManager] NEW RECIPE UNLOCKED -> {matchedRecipe.displayName}");

                        if (unlockPopup != null)
                            unlockPopup.Show(matchedRecipe);
                    }
                }
            }
            else
            {
                Debug.Log(
                    $"[OrderManager] Recipe Match FAIL -> " +
                    $"best={(matchResult.matchedRecipe != null ? matchResult.matchedRecipe.displayName : "None")} / " +
                    $"similarity={matchResult.similarity:0.00}"
                );
            }
        }
        
        bool firstCustomerRoute = currentCustomer != null && currentCustomer.IsInForcedFirstCustomerRoute();
        ShowResultToast(matchedRecipe, unlockedNewRecipe, result, firstCustomerRoute);

        if (matchedRecipe == null)
        {
            Debug.LogWarning("[OrderManager] No matched cocktail. Serving as unknown cocktail.");
        }

        // ���� ó��
        if (gm != null)
        {
            if (result.success)
            {
                int totalRep = result.repDelta;
                if (matchedRecipe != null && currentOrder != null) totalRep += currentOrder.reputationReward;
                gm.AddReputation(totalRep);
            }
            else
            {
                gm.AddReputation(result.repDelta);
            }
        }

        // �� ���� �ý��� ȣ��
        if (currentCustomer != null)
            currentCustomer.OnServedWithReaction(servedCocktail, result);

        // ���� �ʱ�ȭ
        currentCustomer = null;
        currentOrder = null;

        if (ui != null)
        {
            Debug.Log("[OrderManager] OnMiniGameDone -> resetting ORDER text to -");
            ui.SetOrderText("ORDER: -");
        }
    }
    
    private void ShowResultToast(
        CocktailRecipeData matchedRecipe,
        bool unlockedNewRecipe,
        MiniGameResult result,
        bool firstCustomerRoute
    )
    {
        if (resultToast == null)
            resultToast = FindFirstObjectByType<ResultToastController>();

        if (resultToast == null)
            return;

        if (matchedRecipe == null)
        {
            if (firstCustomerRoute)
                resultToast.ShowFirstRouteUnknownCocktail(result.success);
            else
                resultToast.ShowUnknownCocktail();

            return;
        }

        if (unlockedNewRecipe)
        {
            resultToast.ShowRecipeUnlocked(matchedRecipe.displayName);
            return;
        }

        if (firstCustomerRoute && result.success)
        {
            resultToast.ShowFirstCorrectBrew();
            return;
        }

        resultToast.ShowRecipeMatched(matchedRecipe.displayName);
    }

    public void ForceClearOrder()
    {
        Debug.LogWarning("[OrderManager] ForceClearOrder()");
        currentCustomer = null;
        currentOrder = null;
        if (ui != null) ui.SetOrderText("ORDER: -");
    }

    public void PlaySpecificOrder(OrderData order, CustomerController customer)
    {
        PlaySpecificOrder(order, customer, null);
    }

    public void PlaySpecificOrder(OrderData order, CustomerController customer, BrewSessionData selectedSession)
    {
        if (order == null)
        {
            Debug.LogError("[OrderManager] PlaySpecificOrder called with NULL order");
            return;
        }

        if (customer == null)
        {
            Debug.LogError("[OrderManager] PlaySpecificOrder called with NULL customer");
            return;
        }

        if (miniGameManager == null)
            miniGameManager = FindFirstObjectByType<MiniGameManager>();

        if (miniGameManager == null)
        {
            Debug.LogError("[OrderManager] miniGameManager is NULL");
            return;
        }

        currentCustomer = customer;
        currentOrder = order;

        BrewSessionData sessionToUse = selectedSession;

        if (sessionToUse == null && currentOrder != null)
            sessionToUse = currentOrder.defaultBrewSession;

        if (ui != null)
            ui.SetOrderText($"ORDER: {currentOrder.orderName}");

        Debug.Log(
            $"[OrderManager] PlaySpecificOrder -> " +
            $"{currentOrder.orderName} / " +
            $"SessionMethod={(sessionToUse != null ? sessionToUse.method.ToString() : "NULL")} / " +
            $"SelectedSession={(selectedSession != null)}"
        );

        miniGameManager.Play(currentOrder, sessionToUse, OnMiniGameDone);
    }
}
