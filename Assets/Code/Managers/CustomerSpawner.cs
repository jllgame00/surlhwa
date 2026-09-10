using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
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

    [Header("Debug")]
    [SerializeField] private bool debugSpawnWithKey = false;
    [SerializeField] private KeyCode debugSpawnKey = KeyCode.Space;

    [Header("Special Guests")]
    [SerializeField] private SpecialGuestData[] specialGuests;
    [SerializeField] private GameObject littlePrincePrefab;

    private GameManager gm;
    private BartendingFlowCoordinator bartendingFlowCoordinator;
    [SerializeField] private BusinessDayManager businessDayManager;

    private CustomerController activeCustomer;
    private int servedCount;
    private int spawnedCount;
    private GameObject lastSpawnedPrefab;
    private bool activeCustomerIsLittlePrinceFirstVisit;
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
        if (autoStart)
            SpawnNextCustomer();
    }

    private void Update()
    {
        if (!debugSpawnWithKey)
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
        OrderData discoveryOrder = GetDayOneDiscoveryOrder();
        if (discoveryOrder != null)
            customer.ConfigureDedicatedEncounter(discoveryOrder);
        activeCustomerIsLittlePrinceFirstVisit =
            prefabToSpawn == littlePrincePrefab && ShouldSpawnLittlePrinceFirstVisit();
        customer.SetLoopManager(this);
        customer.SetUsesPatience(!IsTutorialSpawn());
        spawnedCount++;
        lastSpawnedPrefab = prefabToSpawn;

        if (defaultExitPoint != null)
            customer.defaultExitPoint = defaultExitPoint;

        customer.AssignSeat(seat);
        Debug.Log($"[CustomerSpawner] Spawned customer at seat {seat.name}");
    }

    public void NotifyCustomerLeft(CustomerController customer)
    {
        if (activeCustomer == customer)
        {
            if (activeCustomerIsLittlePrinceFirstVisit)
            {
                LittlePrinceVisitPersistence.MarkFirstVisitCompleted();
            }

            activeCustomer = null;
            activeCustomerIsLittlePrinceFirstVisit = false;
        }

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

    private OrderData GetDayOneDiscoveryOrder()
    {
        int currentDay = businessDayManager != null
            ? businessDayManager.CurrentDay
            : BusinessDayPersistence.LoadCurrentDay(1);

        if (currentDay != 1 || dayOneDiscoveryOrders == null ||
            spawnedCount < 0 || spawnedCount >= dayOneDiscoveryOrders.Length)
        {
            return null;
        }

        return dayOneDiscoveryOrders[spawnedCount];
    }

    private bool ShouldSpawnLittlePrinceFirstVisit()
    {
        return businessDayManager != null
            && businessDayManager.CurrentDay == 2
            && spawnedCount == 0
            && !LittlePrinceVisitPersistence.IsFirstVisitCompleted()
            && littlePrincePrefab != null;
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
