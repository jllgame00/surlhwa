using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [SerializeField] private DemoAudioController presentationAudio;

    [Header("Spawn")]
    [SerializeField] private GameObject customerPrefab;
    [SerializeField] private GameObject[] generalCustomerPrefabs;
    [SerializeField] private bool avoidSameCustomerPrefabTwice = true;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private SeatManager seatManager;
    [SerializeField] private Transform defaultExitPoint;
    [SerializeField] private float spawnDelay = 1.5f;
    [SerializeField] private float postTutorialSpawnDelayMin = 2.5f;
    [SerializeField] private float postTutorialSpawnDelayMax = 6.0f;
    [SerializeField] private int tutorialCustomerCount = 4;
    [SerializeField] private int maxCustomersServed = 0;
    [SerializeField] private bool autoStart = true;

    [Header("Day 1 Recipe Discovery")]
    [Tooltip("Optional deterministic early encounters. Each entry configures the spawned generic customer with authored order/dialogue content.")]
    [SerializeField] private OrderData[] dayOneDiscoveryOrders;

    [Header("Day 1 Recommendation Proof")]
    [Tooltip("One authored recommendation request after the discovery arc. It remains optional and does not change generic request selection.")]
    [SerializeField] private OrderData dayOneRecommendationOrder;

    [Header("J4 Discovery Practice")]
    [Tooltip("Day-2 slot 2 practice encounter. It is selected only while Negroni is not Known.")]
    [SerializeField] private OrderData negroniDiscoveryOrder;
    [Tooltip("Day-3 slot 2 practice encounter, after the existing Little Prince return. It is selected only while Manhattan is not Known.")]
    [SerializeField] private OrderData manhattanDiscoveryOrder;
    [Tooltip("Day-4 slot 1 practice encounter. It is selected only while Gin Fizz is not Known.")]
    [SerializeField] private OrderData ginFizzDiscoveryOrder;

    [Tooltip("Day-2 slot 3 recovery practice only if Manwol was not learned during the Prince visit.")]
    [SerializeField] private OrderData manwolRecoveryOrder;

    [Header("Production Recommendations")]
    [SerializeField] private OrderData[] productionRecommendationOrders;
    [SerializeField] private IngredientAvailabilityData ingredientAvailability;

    [Header("Debug")]
    [SerializeField] private bool debugSpawnWithKey = false;
    [SerializeField] private KeyCode debugSpawnKey = KeyCode.Space;

    [Header("Special Guests")]
    [SerializeField] private SpecialGuestData[] specialGuests;
    [SerializeField] private GameObject littlePrincePrefab;

    [Header("Little Prince Follow-up")]
    [Tooltip("One deterministic later encounter for the I13 remembered-choice proof.")]
    [SerializeField] private int littlePrinceFollowupDay = 3;
    [SerializeField] private OrderData littlePrinceFollowupOrder;

    [Header("Little Prince Closing Visit")]
    [SerializeField] private OrderData littlePrinceClosingOrder;

    private GameManager gm;
    private BartendingFlowCoordinator bartendingFlowCoordinator;
    [SerializeField] private BusinessDayManager businessDayManager;
    private RecipeUnlockManager recipeUnlockManager;

    private CustomerController activeCustomer;
    private int servedCount;
    private int spawnedCount;
    private GameObject lastSpawnedPrefab;
    private bool activeCustomerIsLittlePrinceFirstVisit;
    private bool activeCustomerIsLittlePrinceFollowupVisit;
    private bool activeCustomerIsLittlePrinceClosingVisit;
    private Coroutine spawnRoutine;

    private void Awake()
    {
        gm = FindFirstObjectByType<GameManager>();

        if (seatManager == null)
            seatManager = GetComponent<SeatManager>();

        if (seatManager == null)
            seatManager = FindFirstObjectByType<SeatManager>();

        bartendingFlowCoordinator = FindFirstObjectByType<BartendingFlowCoordinator>();

        ResolveBusinessDayManager(false);
        ResolveRecipeUnlockManager();

        CustomerSpawner[] spawners = FindObjectsByType<CustomerSpawner>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        int activeSpawnerCount = 0;
        CustomerSpawner firstActiveSpawner = null;
        foreach (CustomerSpawner spawner in spawners)
        {
            if (spawner == null || !spawner.isActiveAndEnabled)
                continue;

            activeSpawnerCount++;
            if (firstActiveSpawner == null)
                firstActiveSpawner = spawner;
        }

        if (activeSpawnerCount > 1 && firstActiveSpawner == this)
            Debug.LogWarning($"[CustomerSpawner] {activeSpawnerCount} active CustomerSpawner components found. Use only one active spawner.");
    }

    private void Start()
    {
        if (autoStart && IsGameplaySessionStarted())
            SpawnNextCustomer();
    }

    private void Update()
    {
        if (!debugSpawnWithKey)
            return;

        if (!IsGameplaySessionStarted())
            return;

        if (bartendingFlowCoordinator != null && bartendingFlowCoordinator.IsBartendingActive)
            return;

        if (Input.GetKeyDown(debugSpawnKey))
            SpawnNextCustomer();
    }

    public void SpawnNextCustomer()
    {
        if (activeCustomer != null)
            return;

        if (!IsGameplaySessionStarted())
            return;

        EnsureReferences();
        ResolveBusinessDayManager(true);

        if (businessDayManager != null && !businessDayManager.CanSpawnMoreCustomers())
            return;

        if (customerPrefab == null)
        {
            Debug.LogError("[CustomerSpawner] customerPrefab is not assigned.");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("[CustomerSpawner] spawnPoint is not assigned.");
            return;
        }

        if (seatManager == null)
        {
            Debug.LogError("[CustomerSpawner] SeatManager not found.");
            return;
        }

        GameObject prefabToSpawn = GetCustomerPrefabForNextSpawn();
        if (prefabToSpawn == null)
        {
            Debug.LogError("[CustomerSpawner] Selected customer prefab is null.");
            return;
        }

        SeatPoint seat = null;
        GameObject customerObject = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
        CustomerController customer = customerObject.GetComponent<CustomerController>();

        if (customer == null)
        {
            if (seat != null)
                seatManager.ReleaseSeat(seat);

            Debug.LogError($"[CustomerSpawner] Selected customer prefab '{prefabToSpawn.name}' does not have a CustomerController.");
            Destroy(customerObject);
            return;
        }

        seat = seatManager.GetFreeSeat(customer);
        if (seat == null)
        {
            Destroy(customerObject);
            ScheduleNextSpawn(spawnDelay);
            return;
        }

        activeCustomer = customer;
        OrderData authoredOrder = GetAuthoredOrderForNextSpawn();
        if (authoredOrder != null)
            customer.ConfigureDedicatedEncounter(authoredOrder);

        activeCustomerIsLittlePrinceFirstVisit =
            prefabToSpawn == littlePrincePrefab && ShouldSpawnLittlePrinceFirstVisit();
        activeCustomerIsLittlePrinceFollowupVisit =
            prefabToSpawn == littlePrincePrefab && ShouldSpawnLittlePrinceFollowupVisit();

        activeCustomerIsLittlePrinceClosingVisit =
            prefabToSpawn == littlePrincePrefab && ShouldSpawnLittlePrinceClosingVisit();

        if (activeCustomerIsLittlePrinceFirstVisit)
            customer.ConfigureLittlePrinceFirstVisitNarrative();
        else if (activeCustomerIsLittlePrinceFollowupVisit)
            customer.ConfigureLittlePrinceFollowupEncounter(littlePrinceFollowupOrder);
        else if (activeCustomerIsLittlePrinceClosingVisit)
            customer.ConfigureLittlePrinceClosingEncounter(littlePrinceClosingOrder);

        customer.SetLoopManager(this);
        bool requiresSuccessfulDiscovery = customer.TryGetDedicatedRequest(out OrderData activeOrder) &&
                                           activeOrder.RequiresSuccessfulDiscovery;
        customer.SetUsesPatience(!IsTutorialSpawn() && !requiresSuccessfulDiscovery &&
            !activeCustomerIsLittlePrinceFirstVisit && !activeCustomerIsLittlePrinceFollowupVisit &&
            !activeCustomerIsLittlePrinceClosingVisit);
        spawnedCount++;
        lastSpawnedPrefab = prefabToSpawn;

        if (defaultExitPoint != null)
            customer.defaultExitPoint = defaultExitPoint;

        customer.AssignSeat(seat);
        presentationAudio?.PlayDoor();
        Debug.Log($"[CustomerSpawner] Spawned customer at seat {seat.name}");
    }

    public void NotifyCustomerLeft(CustomerController customer)
    {
        if (activeCustomer == customer)
        {
            int visit = activeCustomerIsLittlePrinceFirstVisit ? 1 :
                activeCustomerIsLittlePrinceFollowupVisit ? 2 : activeCustomerIsLittlePrinceClosingVisit ? 3 : 0;
            if (visit > 0 && LittlePrinceVisitPersistence.GetServiceOutcome(visit) == 0)
                LittlePrinceVisitPersistence.RecordServiceOutcome(visit, false);

            if (activeCustomerIsLittlePrinceFirstVisit)
            {
                LittlePrinceVisitPersistence.MarkFirstVisitCompleted();
            }
            else if (activeCustomerIsLittlePrinceFollowupVisit)
            {
                LittlePrinceVisitPersistence.MarkFollowupVisitCompleted();
            }

            else if (activeCustomerIsLittlePrinceClosingVisit)
            {
                LittlePrinceVisitPersistence.MarkClosingVisitCompleted();
            }

            activeCustomer = null;
            activeCustomerIsLittlePrinceFirstVisit = false;
            activeCustomerIsLittlePrinceFollowupVisit = false;
            activeCustomerIsLittlePrinceClosingVisit = false;
        }

        presentationAudio?.PlayDoor();
        servedCount++;
        Debug.Log($"[CustomerSpawner] Customer left. Served={servedCount}");

        ResolveBusinessDayManager(false);
        if (businessDayManager != null)
        {
            businessDayManager.NotifyCustomerServedOrLeft();

            if (!businessDayManager.CanSpawnMoreCustomers())
                return;
        }

        if (maxCustomersServed > 0 && servedCount >= maxCustomersServed)
        {
            Debug.Log($"[CustomerSpawner] maxCustomersServed reached ({maxCustomersServed}). Stopping customer loop.");
            return;
        }

        ScheduleNextSpawn(GetNextSpawnDelay());
    }

    private void ScheduleNextSpawn(float delay)
    {
        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        spawnRoutine = StartCoroutine(SpawnAfterDelay(delay));
        Debug.Log($"[CustomerSpawner] Next customer scheduled in {delay:0.##}s");
    }

    private IEnumerator SpawnAfterDelay(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        spawnRoutine = null;
        SpawnNextCustomer();
    }

    private float GetNextSpawnDelay()
    {
        if (servedCount < tutorialCustomerCount)
            return spawnDelay;

        float min = Mathf.Min(postTutorialSpawnDelayMin, postTutorialSpawnDelayMax);
        float max = Mathf.Max(postTutorialSpawnDelayMin, postTutorialSpawnDelayMax);

        return Random.Range(min, max);
    }

    private void EnsureReferences()
    {
        if (seatManager == null)
            seatManager = GetComponent<SeatManager>();

        if (seatManager == null)
            seatManager = FindFirstObjectByType<SeatManager>();
    }

    private bool IsGameplaySessionStarted()
    {
        if (gm == null)
            gm = FindFirstObjectByType<GameManager>();

        // Layout/recovery scenes without a GameManager retain their existing
        // inspection behavior. The production Bar scene starts only after the
        // minimal New Game or Continue entry decision.
        return gm == null || gm.IsGameplaySessionStarted;
    }

    private void ResolveBusinessDayManager(bool createIfMissing)
    {
        if (businessDayManager == null)
            businessDayManager = FindFirstObjectByType<BusinessDayManager>();

        if (businessDayManager == null && createIfMissing)
            businessDayManager = gameObject.AddComponent<BusinessDayManager>();
    }

    private GameObject GetCustomerPrefabForNextSpawn()
    {
        if (ShouldSpawnLittlePrinceFirstVisit())
            return littlePrincePrefab;

        if (ShouldSpawnLittlePrinceFollowupVisit() || ShouldSpawnLittlePrinceClosingVisit())
            return littlePrincePrefab;

        if (IsTutorialSpawn())
            return customerPrefab;

        if (generalCustomerPrefabs == null || generalCustomerPrefabs.Length == 0)
            return customerPrefab;

        List<GameObject> candidates = new List<GameObject>();
        foreach (GameObject prefab in generalCustomerPrefabs)
        {
            if (prefab != null)
                candidates.Add(prefab);
        }

        if (candidates.Count == 0)
            return customerPrefab;

        if (avoidSameCustomerPrefabTwice && candidates.Count > 1 && lastSpawnedPrefab != null)
        {
            List<GameObject> alternateCandidates = new List<GameObject>();
            foreach (GameObject candidate in candidates)
            {
                if (candidate != lastSpawnedPrefab)
                    alternateCandidates.Add(candidate);
            }

            if (alternateCandidates.Count > 0)
                candidates = alternateCandidates;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private bool IsTutorialSpawn()
    {
        int currentDay = businessDayManager != null
            ? businessDayManager.CurrentDay
            : BusinessDayPersistence.LoadCurrentDay(1);

        return currentDay == 1 && spawnedCount < tutorialCustomerCount;
    }

    private OrderData GetAuthoredOrderForNextSpawn()
    {
        int currentDay = businessDayManager != null
            ? businessDayManager.CurrentDay
            : BusinessDayPersistence.LoadCurrentDay(1);

        if (currentDay == 1)
            return GetDayOneAuthoredOrder();

        // Special narrative content and required practice retain priority.
        if (ShouldSpawnLittlePrinceFirstVisit() || ShouldSpawnLittlePrinceFollowupVisit() ||
            ShouldSpawnLittlePrinceClosingVisit())
            return null;

        OrderData discovery = GetJ4DiscoveryOrderForNextSpawn(currentDay);
        return discovery != null ? discovery : GetProductionRecommendation(currentDay);
    }

    private OrderData GetDayOneAuthoredOrder()
    {
        if (spawnedCount < 0)
            return null;

        if (dayOneDiscoveryOrders != null && spawnedCount < dayOneDiscoveryOrders.Length)
            return dayOneDiscoveryOrders[spawnedCount];

        return dayOneRecommendationOrder != null && dayOneDiscoveryOrders != null &&
               spawnedCount == dayOneDiscoveryOrders.Length
            ? dayOneRecommendationOrder
            : null;
    }

    /// <summary>
    /// Keeps the three J4 practice encounters in their authored early-night
    /// positions without owning the later J3/J5 schedule. A same-day scene
    /// reload resets this component's transient spawn count, so existing
    /// completed Prince-visit flags advance only the already-consumed special
    /// positions; recipe knowledge remains the authoritative discovery state.
    /// </summary>
    private OrderData GetJ4DiscoveryOrderForNextSpawn(int currentDay)
    {
        switch (currentDay)
        {
            case 2:
                // Fresh: Prince is D2-1 and Negroni is D2-2. After a reload
                // following Prince success, its persisted completion flag
                // reconstructs that consumed first position.
                if (spawnedCount == 1 ||
                    (spawnedCount == 0 && LittlePrinceVisitPersistence.IsFirstVisitCompleted()))
                {
                    return GetPendingDiscoveryOrder(negroniDiscoveryOrder);
                }

                if (spawnedCount == 2)
                    return GetPendingDiscoveryOrder(manwolRecoveryOrder);

                break;

            case 3:
                // Fresh: the existing Prince return is D3-1 and Manhattan is
                // D3-2. Its existing completion flag provides the same reload
                // reconstruction without a J4 persistence key.
                if (spawnedCount == 1 ||
                    (spawnedCount == 0 && LittlePrinceVisitPersistence.IsFollowupVisitCompleted()))
                {
                    return GetPendingDiscoveryOrder(manhattanDiscoveryOrder);
                }

                break;

            case 4:
                if (spawnedCount == 0)
                    return GetPendingDiscoveryOrder(ginFizzDiscoveryOrder);

                break;
        }

        return null;
    }

    private OrderData GetProductionRecommendation(int currentDay)
    {
        if (productionRecommendationOrders == null || ingredientAvailability == null)
            return null; // Compatibility scenes retain their existing generic route.

        // Two different overlaps after Day-3 practice: Warmth (Seolya/Manhattan)
        // and Bitterness (Negroni/Manhattan). Day 4 adds Clarity and Awakening.
        EmotionTag preferred = currentDay >= 4 && spawnedCount == 1
            ? EmotionTag.Clarity
            : currentDay >= 4 && spawnedCount == 2
                ? EmotionTag.Awakening
                : spawnedCount == 3 && currentDay < 4
                    ? EmotionTag.Bitterness
                    : EmotionTag.Warmth;
        List<OrderData> preferredRequests = new();
        List<OrderData> reachableRequests = new();
        foreach (OrderData request in productionRecommendationOrders)
        {
            if (!HasKnownSuppliedAnswer(request, currentDay))
                continue;

            reachableRequests.Add(request);
            if (System.Array.IndexOf(request.DesiredEmotionTags, preferred) >= 0)
                preferredRequests.Add(request);
        }

        List<OrderData> candidates = preferredRequests.Count > 0
            ? preferredRequests : reachableRequests;
        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    private bool HasKnownSuppliedAnswer(OrderData request, int currentDay)
    {
        RecipeUnlockManager knowledge = ResolveRecipeUnlockManager();
        if (request == null || request.RequestType != GuestRequestType.Recommendation ||
            knowledge == null || ingredientAvailability == null)
            return false;

        IngredientAvailabilityContext supply = new(currentDay);
        foreach (CocktailRecipeData recipe in knowledge.GetAllRecipes())
        {
            if (recipe == null || !knowledge.IsKnown(recipe))
                continue;

            bool supplied = true;
            foreach (RecipeIngredientEntry ingredient in recipe.ingredients)
            {
                if (ingredient == null ||
                    !ingredientAvailability.IsIngredientAvailable(ingredient.ingredient, supply))
                {
                    supplied = false;
                    break;
                }
            }

            if (supplied && RequestEvaluation.Evaluate(request, new RecipeMatchResult
                { success = true, matchedRecipe = recipe, similarity = 1f }).Status ==
                RequestEvaluationStatus.Satisfied)
                return true;
        }

        return false;
    }

    private OrderData GetPendingDiscoveryOrder(OrderData discoveryOrder)
    {
        if (discoveryOrder == null)
            return null;

        RecipeUnlockManager knowledge = ResolveRecipeUnlockManager();
        if (knowledge == null)
        {
            Debug.LogWarning(
                "[CustomerSpawner] RecipeUnlockManager unavailable; preserving the authored discovery encounter.");
            return discoveryOrder;
        }

        CocktailRecipeData targetRecipe = knowledge.GetRecipeForCocktail(
            discoveryOrder.requestedCocktail,
            discoveryOrder.miniGameType);
        if (targetRecipe == null)
        {
            Debug.LogWarning(
                "[CustomerSpawner] Discovery order has no authoritative target recipe; preserving the encounter -> " +
                discoveryOrder.name);
            return discoveryOrder;
        }

        if (!knowledge.IsKnown(targetRecipe))
            return discoveryOrder;

        Debug.Log(
            "[CustomerSpawner] Discovery target already Known; using normal fallback for this authored position -> " +
            targetRecipe.recipeId);
        return null;
    }

    private RecipeUnlockManager ResolveRecipeUnlockManager()
    {
        if (recipeUnlockManager == null)
            recipeUnlockManager = FindFirstObjectByType<RecipeUnlockManager>();

        return recipeUnlockManager;
    }

    private bool ShouldSpawnLittlePrinceFirstVisit()
    {
        return businessDayManager != null
            && businessDayManager.CurrentDay == 2
            && spawnedCount == 0
            && !LittlePrinceVisitPersistence.IsFirstVisitCompleted()
            && littlePrincePrefab != null;
    }

    private bool ShouldSpawnLittlePrinceFollowupVisit()
    {
        return businessDayManager != null
            && businessDayManager.CurrentDay == littlePrinceFollowupDay
            && spawnedCount == 0
            && LittlePrinceVisitPersistence.IsFirstVisitCompleted()
            && !LittlePrinceVisitPersistence.IsFollowupVisitCompleted()
            && littlePrinceFollowupOrder != null
            && littlePrincePrefab != null;
    }

    private bool ShouldSpawnLittlePrinceClosingVisit()
    {
        return businessDayManager != null && businessDayManager.CurrentDay == 4 &&
            spawnedCount == 3 && LittlePrinceVisitPersistence.IsFirstVisitCompleted() &&
            LittlePrinceVisitPersistence.IsFollowupVisitCompleted() &&
            !LittlePrinceVisitPersistence.IsClosingVisitCompleted() &&
            littlePrinceClosingOrder != null && littlePrincePrefab != null;
    }

    private GameObject ChooseCustomerPrefab()
    {
        GameObject prefabToSpawn = customerPrefab;

        if (gm == null)
            gm = FindFirstObjectByType<GameManager>();

        if (gm == null || specialGuests == null)
            return prefabToSpawn;

        foreach (SpecialGuestData guest in specialGuests)
        {
            if (guest == null || guest.prefab == null)
                continue;

            if (gm.reputation >= guest.requiredReputation)
            {
                prefabToSpawn = guest.prefab;
                break;
            }
        }

        return prefabToSpawn;
    }
}
