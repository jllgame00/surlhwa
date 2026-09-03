using UnityEngine;
using System;

public class OrderManager : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool verboseLogging = false;
    
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

    [Header("Bartending")]
    [SerializeField] private BartendingFlowCoordinator bartendingFlowCoordinator;

    private RecipeUnlockManager recipeUnlockManager;

    private RecipeUnlockPopupController unlockPopup;

    private RecipeGuideContext BuildRecipeGuideContext()
    {
        if (currentOrder == null || recipeUnlockManager == null)
            return null;

        MiniGameType method = currentOrder.miniGameType;
        bool isForcedFirstCustomerRouteActive =
            currentCustomer != null && currentCustomer.IsInForcedFirstCustomerRoute();

        return new RecipeGuideContext(
            method,
            recipeUnlockManager.GetUnlockedRecipesByMethod(method),
            isForcedFirstCustomerRouteActive);
    }

    private void HandleFirstRecipeRevealRequest(
        RecipeGuideContext context,
        string title,
        string message)
    {
        if (context == null || !context.IsForcedFirstCustomerRouteActive)
            return;

        if (recipeUnlockManager == null || recipeUnlockManager.HasShownFirstRecipeReveal())
            return;

        if (resultToast == null)
            resultToast = FindFirstObjectByType<ResultToastController>();

        if (resultToast == null)
        {
            Debug.LogWarning("[OrderManager] Cannot show first recipe reveal: ResultToastController not found.");
            return;
        }

        resultToast.Show(title, message);

        if (recipeUnlockManager.TryMarkFirstRecipeRevealShown())
            Debug.Log("[OrderManager] First recipe reveal shown");
    }

    private bool HasActiveOrder => currentOrder != null;

    public event Action<BartendingFlowFailure> BartendingFlowFailed;

    void Awake()
    {
        ui = FindFirstObjectByType<UIManager>();
        gm = FindFirstObjectByType<GameManager>();

        if (bartendingFlowCoordinator == null)
            bartendingFlowCoordinator = GetComponent<BartendingFlowCoordinator>();

        if (bartendingFlowCoordinator == null)
            bartendingFlowCoordinator = FindFirstObjectByType<BartendingFlowCoordinator>();
        
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
    private void NotifyBartendingFlowFailed(BartendingFlowFailure failure)
    {
        Debug.LogError(
            "[OrderManager] Bartending infrastructure failure -> " + failure + ". " +
            "Current order and customer remain owned by BarScene.");

        BartendingFlowFailed?.Invoke(failure);
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
        bool firstCustomerRoute = currentCustomer != null && currentCustomer.IsInForcedFirstCustomerRoute();
        bool secondCustomerRoute = currentCustomer != null && currentCustomer.IsInForcedSecondCustomerRoute();

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

                if (result.success && !wasUnlocked)
                {
                    bool newlyUnlocked = recipeUnlockManager.UnlockRecipe(matchedRecipe);

                    if (newlyUnlocked)
                    {
                        unlockedNewRecipe = true;

                        Debug.Log($"[OrderManager] NEW RECIPE UNLOCKED -> {matchedRecipe.displayName}");

                        if (unlockPopup != null)
                        {
                            if (secondCustomerRoute)
                            {
                                unlockPopup.Show(
                                    matchedRecipe,
                                    "새 레시피가 피어났습니다",
                                    "선명한 끝맛이 레시피북에 남았습니다."
                                );
                            }
                            else
                            {
                                unlockPopup.Show(matchedRecipe);
                            }
                        }
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
        
        ShowResultToast(matchedRecipe, unlockedNewRecipe, result, firstCustomerRoute, secondCustomerRoute);

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
        bool firstCustomerRoute,
        bool secondCustomerRoute
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
            if (secondCustomerRoute)
                resultToast.ShowSecondRouteRecipeUnlocked();
            else
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

        if (bartendingFlowCoordinator == null)
        {
            Debug.LogError("[OrderManager] BartendingFlowCoordinator is NULL");
            return;
        }

        if (bartendingFlowCoordinator.IsBartendingActive)
        {
            Debug.LogWarning("[OrderManager] PlaySpecificOrder ignored because Bartending is already active.");
            return;
        }

        currentCustomer = customer;
        currentOrder = order;

        if (ui != null)
            ui.SetOrderText($"ORDER: {currentOrder.orderName}");

        Debug.Log(
            $"[OrderManager] PlaySpecificOrder -> " +
            currentOrder.orderName
        );

        BeginCurrentOrderBartending();
    }

    private void BeginCurrentOrderBartending()
    {
        if (currentOrder == null)
        {
            Debug.LogError("[OrderManager] Cannot begin Bartending without a current order.");
            return;
        }

        if (bartendingFlowCoordinator == null)
        {
            Debug.LogError("[OrderManager] BartendingFlowCoordinator is NULL");
            return;
        }

        RecipeGuideContext recipeGuideContext = BuildRecipeGuideContext();
        BartendingSession session = BartendingSession.FromOrder(
            currentOrder,
            recipeGuideContext,
            (title, message) => HandleFirstRecipeRevealRequest(recipeGuideContext, title, message));

        bool accepted = bartendingFlowCoordinator.BeginBartending(
            session,
            OnMiniGameDone,
            NotifyBartendingFlowFailed);

        if (!accepted)
        {
            Debug.LogWarning("[OrderManager] Bartending request was not started.");

            if (!bartendingFlowCoordinator.IsBartendingActive)
                NotifyBartendingFlowFailed(BartendingFlowFailure.BeginRejected);
        }
    }
}
