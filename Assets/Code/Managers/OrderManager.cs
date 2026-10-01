using UnityEngine;
using System;

public class OrderManager : MonoBehaviour
{
    private const float GuestReactionSequenceDuration = 4f;
    private const float DiscoveryFeedbackGap = 0.1f;
    private const string DefaultDiscoveryRetryFeedback =
        "레시피와 다른 부분이 있어요. 층과 순서를 확인하고 다시 만들어 주세요.";

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
    private GuestRequestContext currentGuestRequest;

    private UIManager ui;
    private GameManager gm;

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
            recipeUnlockManager.GetAllRecipesByMethod(method),
            recipeUnlockManager.GetKnownFacts,
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
        RecipeMatchResult matchResult = default;
        bool newlyKnownRecipe = false;
        bool secondCustomerRoute = currentCustomer != null && currentCustomer.IsInForcedSecondCustomerRoute();

        if (recipeUnlockManager != null && result.brewedComposition != null)
        {
            var candidates = recipeUnlockManager.GetAllRecipesByMethod(result.brewedComposition.method);
            matchResult = RecipeMatchService.FindBestMatch(result.brewedComposition, candidates);
        }

        // Recommendation has no expected answer recipe. Once the actual liquid is
        // recognized, evaluate its trace against that recipe without changing FIT or technique.
        if (currentGuestRequest != null && currentGuestRequest.RequestType == GuestRequestType.Recommendation &&
            matchResult.success && matchResult.matchedRecipe != null)
            result.preparationEvaluation = RecipePreparationEvaluator.Evaluate(
                matchResult.matchedRecipe, result.preparationTrace);

        RequestEvaluationResult requestEvaluation = RequestEvaluation.Evaluate(currentGuestRequest, matchResult);
        LogRequestEvaluation(currentGuestRequest, matchResult, requestEvaluation);

        bool requiresSuccessfulDiscovery = currentOrder != null &&
                                           currentOrder.RequiresSuccessfulDiscovery;
        CocktailRecipeData discoveryTargetRecipe = requiresSuccessfulDiscovery
            ? GetCurrentExactTargetRecipe()
            : null;
        bool successfulDiscovery = MeetsSuccessfulDiscoveryRequirements(
            currentOrder,
            currentGuestRequest,
            discoveryTargetRecipe,
            matchResult,
            requestEvaluation,
            result);

        if (requiresSuccessfulDiscovery && !successfulDiscovery)
        {
            RestoreAfterFailedDiscoveryAttempt();
            return;
        }

        if (recipeUnlockManager != null && result.brewedComposition != null)
        {
            if (matchResult.success && matchResult.matchedRecipe != null)
            {
                matchedRecipe = matchResult.matchedRecipe;
                servedCocktail = matchedRecipe.targetCocktail;

                Debug.Log(
                    $"[OrderManager] Recipe Match SUCCESS -> " +
                    $"recipeId={matchedRecipe.recipeId} / similarity={matchResult.similarity:0.00}"
                );

                bool wasKnown = recipeUnlockManager.IsKnown(matchedRecipe);

                bool canPromoteMatchedRecipe = requiresSuccessfulDiscovery
                    ? successfulDiscovery && matchedRecipe == discoveryTargetRecipe
                    : result.success && (currentCustomer == null || currentCustomer.LittlePrinceVisitNumber == 0 ||
                        (result.preparationEvaluation != null && result.preparationEvaluation.Status ==
                            RecipePreparationEvaluationStatus.Satisfied));

                if (canPromoteMatchedRecipe && !wasKnown)
                {
                    bool becameKnown = recipeUnlockManager.MarkKnown(matchedRecipe);

                    if (becameKnown)
                    {
                        newlyKnownRecipe = true;

                        Debug.Log($"[OrderManager] RECIPE BECAME KNOWN -> {GetCocktailDisplayName(matchedRecipe)}");

                        // Presentation is scheduled once below, including
                        // scenes without a diagnostic toast.
                    }
                }
            }
            else
            {
                Debug.Log(
                    $"[OrderManager] Recipe Match FAIL -> " +
                    $"best={(matchResult.matchedRecipe != null ? matchResult.matchedRecipe.recipeId : "None")} / " +
                    $"similarity={matchResult.similarity:0.00}"
                );
            }
        }

        GuestReactionResolver.GuestResponseOutcome guestResponse = currentCustomer != null
            ? GuestReactionResolver.EvaluateOutcome(
                requestEvaluation,
                result.success,
                currentCustomer.GetDesiredEmotionTags(),
                servedCocktail != null && servedCocktail.emotionTags != null
                    ? servedCocktail.emotionTags
                    : Array.Empty<EmotionTag>(),
                servedCocktail != null)
            : new GuestReactionResolver.GuestResponseOutcome(
                0,
                GuestReactionResolver.GuestResponseTone.NotApplicable,
                false);
        
        if (matchedRecipe == null)
        {
            Debug.LogWarning("[OrderManager] No matched cocktail. Serving as unknown cocktail.");
        }

        // ���� ó��
        int requestReputationReward = CalculateRequestReputationReward(
            result, matchedRecipe, currentGuestRequest, requestEvaluation);
        int reputationDelta = result.repDelta + requestReputationReward;
        bool requestRewardWithheld = IsRequestRewardWithheld(
            result, matchedRecipe, currentGuestRequest, requestEvaluation);
        bool canRevealMatchedCocktail = matchedRecipe != null && recipeUnlockManager != null &&
                                         recipeUnlockManager.IsKnown(matchedRecipe);
        if (gm != null)
            gm.AddReputation(reputationDelta);

        GuestServiceResult guestServiceResult = new(
            servedCocktail,
            result,
            requestEvaluation,
            guestResponse,
            reputationDelta,
            result.repDelta,
            requestReputationReward,
            requestRewardWithheld,
            canRevealMatchedCocktail,
            newlyKnownRecipe);

        // �� ���� �ý��� ȣ��
        Func<bool> feedbackCompleted = PresentServiceFeedback(
            guestServiceResult,
            matchedRecipe,
            newlyKnownRecipe,
            secondCustomerRoute,
            currentCustomer != null);

        if (currentCustomer != null)
            currentCustomer.OnServedWithReaction(guestServiceResult, feedbackCompleted);

        // ���� �ʱ�ȭ
        currentCustomer = null;
        currentOrder = null;
        currentGuestRequest = null;

        if (ui != null)
        {
            Debug.Log("[OrderManager] OnMiniGameDone -> resetting ORDER text to -");
            ui.SetOrderText("ORDER: -");
        }
    }

    /// <summary>
    /// Bounded completion rule for explicitly flagged authored recipe-practice
    /// orders. Ordinary service keeps its historical match/discovery behavior.
    /// </summary>
    internal static bool MeetsSuccessfulDiscoveryRequirements(
        OrderData order,
        GuestRequestContext request,
        CocktailRecipeData targetRecipe,
        RecipeMatchResult matchResult,
        RequestEvaluationResult requestEvaluation,
        MiniGameResult result)
    {
        return order != null &&
               order.RequiresSuccessfulDiscovery &&
               request != null &&
               request.RequestType == GuestRequestType.ExactCocktail &&
               targetRecipe != null &&
               result.expectedRecipe == targetRecipe &&
               matchResult.success &&
               matchResult.matchedRecipe == targetRecipe &&
               requestEvaluation.Status == RequestEvaluationStatus.Satisfied &&
               result.success &&
               result.preparationEvaluation != null &&
               result.preparationEvaluation.Status == RecipePreparationEvaluationStatus.Satisfied;
    }

    private CocktailRecipeData GetCurrentExactTargetRecipe()
    {
        if (recipeUnlockManager == null || currentOrder == null ||
            currentGuestRequest == null ||
            currentGuestRequest.RequestType != GuestRequestType.ExactCocktail)
        {
            return null;
        }

        return recipeUnlockManager.GetRecipeForCocktail(
            currentOrder.requestedCocktail,
            currentOrder.miniGameType);
    }

    private void RestoreAfterFailedDiscoveryAttempt()
    {
        CustomerController retryCustomer = currentCustomer;
        string retryFeedback = currentOrder != null &&
                               !string.IsNullOrWhiteSpace(currentOrder.DiscoveryRetryFeedback)
            ? currentOrder.DiscoveryRetryFeedback
            : DefaultDiscoveryRetryFeedback;

        Debug.Log(
            "[OrderManager] Discovery attempt incomplete. Keeping the same authored customer active for retry.");

        currentCustomer = null;
        currentOrder = null;
        currentGuestRequest = null;

        if (ui != null)
            ui.SetOrderText("ORDER: -");

        retryCustomer?.RestoreAfterDiscoveryRetry(retryFeedback);
    }

    private void LogRequestEvaluation(
        GuestRequestContext request,
        RecipeMatchResult matchResult,
        RequestEvaluationResult evaluation)
    {
        if (!verboseLogging && evaluation.Status != RequestEvaluationStatus.InvalidData)
            return;

        string requestedCocktailId = request != null && request.RequestedCocktail != null
            ? request.RequestedCocktail.cocktailId
            : "None";
        string actualCocktailId = matchResult.success &&
                                  matchResult.matchedRecipe != null &&
                                  matchResult.matchedRecipe.targetCocktail != null
            ? matchResult.matchedRecipe.targetCocktail.cocktailId
            : "None";
        string matchedRecipeId = matchResult.matchedRecipe != null
            ? matchResult.matchedRecipe.recipeId
            : "None";
        string message =
            $"[OrderManager] Request Evaluation -> " +
            $"request={request?.DisplayTitle ?? "NULL"} / " +
            $"status={evaluation.Status} / " +
            $"requested={requestedCocktailId} / " +
            $"recipe={matchedRecipeId} / " +
            $"actual={actualCocktailId}";

        if (evaluation.Status == RequestEvaluationStatus.InvalidData)
            Debug.LogWarning(message);
        else if (verboseLogging)
            Debug.Log(message);
    }
    
    /// <summary>
    /// Coordinates already-resolved transient feedback. The guest reaction is
    /// primary, then this diagnostic toast, then the sole discovery popup.
    /// No gameplay axis is recalculated here.
    /// </summary>
    private Func<bool> PresentServiceFeedback(
        GuestServiceResult serviceResult,
        CocktailRecipeData newlyKnownRecipe,
        bool recipeBecameKnown,
        bool secondCustomerRoute,
        bool hasCustomerReaction)
    {
        if (resultToast == null)
            resultToast = FindFirstObjectByType<ResultToastController>();

        float reactionDelay = hasCustomerReaction ? GuestReactionSequenceDuration : 0f;
        if (unlockPopup == null)
            unlockPopup = FindFirstObjectByType<RecipeUnlockPopupController>();

        // This request-local completion is presentation only. Recipe knowledge
        // was persisted above; the customer still owns farewell and departure.
        bool completed = false;
        void ShowDiscovery(float delay)
        {
            if (this == null || !isActiveAndEnabled || !recipeBecameKnown ||
                newlyKnownRecipe == null || unlockPopup == null)
            {
                completed = true;
                return;
            }
            unlockPopup.ShowDelayed(
                newlyKnownRecipe, delay,
                secondCustomerRoute ? "New Recipe Discovered" : null,
                secondCustomerRoute ? "The recipe has been added to your Guide." : null,
                _ => completed = true);
        }

        if (resultToast != null)
            resultToast.ShowServiceResult(serviceResult, reactionDelay, shown =>
            {
                // Replacement/disable cancels this presentation chain without
                // opening a late popup or holding a customer indefinitely.
                if (shown) ShowDiscovery(DiscoveryFeedbackGap);
                else completed = true;
            });
        else
            ShowDiscovery(reactionDelay);
        return () => completed;
    }

    // Legacy direct-toast routing is retained for serialized scene/debug
    // compatibility. Normal service now uses PresentServiceFeedback above.
    private void ShowResultToast(
        CocktailRecipeData matchedRecipe,
        bool canRevealMatchedRecipe,
        bool newlyKnownRecipe,
        MiniGameResult result,
        GuestReactionResolver.GuestResponseOutcome guestResponse,
        bool firstCustomerRoute,
        bool secondCustomerRoute,
        bool isWrongValidRequestMismatch
    )
    {
        if (resultToast == null)
            resultToast = FindFirstObjectByType<ResultToastController>();

        if (resultToast == null)
            return;

        if (guestResponse.HasGuestFitFeedback)
        {
            resultToast.ShowGuestFitOutcome(guestResponse.Tone);
            return;
        }

        // Composition recognition can succeed while the current result policy
        // rejects the attempt (for example, failed technique). That technical
        // result must not reveal an otherwise Unknown recipe identity.
        if (matchedRecipe == null || !canRevealMatchedRecipe)
        {
            if (firstCustomerRoute)
                resultToast.ShowFirstRouteUnknownCocktail(result.success);
            else
                resultToast.ShowUnknownCocktail();

            return;
        }

        if (isWrongValidRequestMismatch)
        {
            resultToast.Show(
                $"{GetCocktailDisplayName(matchedRecipe)} 완성",
                "손님이 요청한 칵테일과 일치하지 않습니다."
            );
            return;
        }

        if (newlyKnownRecipe)
        {
            if (secondCustomerRoute)
                resultToast.ShowSecondRouteRecipeUnlocked();
            else
                resultToast.ShowRecipeUnlocked(GetCocktailDisplayName(matchedRecipe));

            return;
        }

        if (firstCustomerRoute && result.success)
        {
            resultToast.ShowFirstCorrectBrew();
            return;
        }

        resultToast.ShowRecipeMatched(GetCocktailDisplayName(matchedRecipe));
    }

    private static bool ShouldGrantRequestReward(RequestEvaluationResult evaluation)
    {
        return evaluation.Status != RequestEvaluationStatus.Unsatisfied;
    }

    private static int CalculateReputationDelta(
        MiniGameResult result,
        CocktailRecipeData matchedRecipe,
        GuestRequestContext request,
        RequestEvaluationResult evaluation)
    {
        return result.repDelta + CalculateRequestReputationReward(
            result, matchedRecipe, request, evaluation);
    }

    private static int CalculateRequestReputationReward(
        MiniGameResult result,
        CocktailRecipeData matchedRecipe,
        GuestRequestContext request,
        RequestEvaluationResult evaluation)
    {
        if (result.success && matchedRecipe != null && request != null &&
            ShouldGrantRequestReward(evaluation))
        {
            return request.ReputationReward;
        }

        return 0;
    }

    private static bool IsRequestRewardWithheld(
        MiniGameResult result,
        CocktailRecipeData matchedRecipe,
        GuestRequestContext request,
        RequestEvaluationResult evaluation)
    {
        return result.success && matchedRecipe != null && request != null &&
               request.ReputationReward != 0 &&
               evaluation.Status == RequestEvaluationStatus.Unsatisfied;
    }

    private static bool IsWrongValidRequestMismatch(
        GuestRequestContext request,
        MiniGameResult result,
        RecipeMatchResult matchResult,
        RequestEvaluationResult evaluation)
    {
        return request != null &&
               request.RequestType == GuestRequestType.ExactCocktail &&
               request.RequestedCocktail != null &&
               result.success &&
               matchResult.success &&
               evaluation.Status == RequestEvaluationStatus.Unsatisfied;
    }

    private static string GetCocktailDisplayName(CocktailRecipeData recipe)
    {
        if (recipe != null && recipe.targetCocktail != null &&
            !string.IsNullOrWhiteSpace(recipe.targetCocktail.cocktailName))
        {
            return recipe.targetCocktail.cocktailName;
        }

        Debug.LogError($"[OrderManager] Recipe '{recipe?.recipeId ?? "NULL"}' has no valid targetCocktail display data.");
        return "Missing Cocktail Data";
    }

    public void PlaySpecificOrder(OrderData order, CustomerController customer)
    {
        PlayOrder(order, customer, GuestRequestContext.FromOrder(order));
    }

    /// <summary>
    /// Starts a generic emotional Recommendation through a selected method
    /// template. The template remains responsible only for bartending route;
    /// it cannot replace the guest request with its serialized exact target.
    /// </summary>
    public void PlayGenericRecommendationOrder(OrderData methodTemplate, CustomerController customer)
    {
        string displayTitle = customer != null ? customer.GetInteractionHintLine() : string.Empty;
        GuestRequestContext guestRequest = GuestRequestContext.CreateRecommendation(
            customer != null ? customer.GetDesiredEmotionTags() : null,
            displayTitle,
            methodTemplate != null ? methodTemplate.reputationReward : 0);

        PlayOrder(methodTemplate, customer, guestRequest);
    }

    /// <summary>
    /// Uses a method template only for the preparation route. Authored request
    /// tags, title and reward remain owned by the guest's Recommendation asset.
    /// </summary>
    public void PlayAuthoredRecommendationOrder(
        OrderData methodTemplate, OrderData authoredRequest, CustomerController customer)
    {
        if (authoredRequest == null ||
            authoredRequest.RequestType != GuestRequestType.Recommendation)
        {
            Debug.LogError("[OrderManager] An authored Recommendation is required.");
            return;
        }

        PlayOrder(methodTemplate, customer, GuestRequestContext.FromOrder(authoredRequest));
    }

    private void PlayOrder(
        OrderData order,
        CustomerController customer,
        GuestRequestContext guestRequest)
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

        if (guestRequest == null)
        {
            Debug.LogError("[OrderManager] PlayOrder called with NULL guest request context");
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
        currentGuestRequest = guestRequest;

        if (ui != null)
            ui.SetOrderText($"ORDER: {currentGuestRequest.DisplayTitle}");

        Debug.Log(
            $"[OrderManager] PlayOrder -> " +
            $"request={currentGuestRequest.RequestType} / " +
            $"method={currentOrder.miniGameType} / " +
            $"title={currentGuestRequest.DisplayTitle}"
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

        CocktailRecipeData expectedRecipe = recipeUnlockManager != null &&
            currentGuestRequest != null &&
            currentGuestRequest.RequestType == GuestRequestType.ExactCocktail
            ? recipeUnlockManager.GetRecipeForCocktail(
                currentOrder.requestedCocktail,
                currentOrder.miniGameType)
            : null;
        BusinessDayManager businessDayManager = FindFirstObjectByType<BusinessDayManager>();
        int currentBusinessDay = businessDayManager != null
            ? businessDayManager.CurrentDay
            : BusinessDayPersistence.LoadCurrentDay(1);
        BartendingRequestContext request = new BartendingRequestContext(
            currentOrder.miniGameType,
            currentGuestRequest != null ? currentGuestRequest.DisplayTitle : currentOrder.orderName,
            expectedRecipe,
            currentBusinessDay);
        RecipeGuideContext recipeGuideContext = BuildRecipeGuideContext();
        BartendingSession session = new BartendingSession(
            request,
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
