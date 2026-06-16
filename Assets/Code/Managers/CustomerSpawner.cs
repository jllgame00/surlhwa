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
    [SerializeField] private int tutorialCustomerCount = 3;
    [SerializeField] private int maxCustomersServed = 0;
    [SerializeField] private bool autoStart = true;

    [Header("Debug")]
    [SerializeField] private bool debugSpawnWithKey = false;
    [SerializeField] private KeyCode debugSpawnKey = KeyCode.Space;

    [Header("Special Guests")]
    [SerializeField] private SpecialGuestData[] specialGuests;

    private GameManager gm;
    [SerializeField] private MiniGameManager miniGameManager;

    private CustomerController activeCustomer;
    private int servedCount;
    private int spawnedCount;
    private GameObject lastSpawnedPrefab;
    private Coroutine spawnRoutine;

    private void Awake()
    {
        gm = FindFirstObjectByType<GameManager>();

        if (seatManager == null)
            seatManager = GetComponent<SeatManager>();

        if (seatManager == null)
            seatManager = FindFirstObjectByType<SeatManager>();

        if (miniGameManager == null)
            miniGameManager = FindFirstObjectByType<MiniGameManager>();

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

        if (miniGameManager != null && miniGameManager.IsPlaying)
            return;

        if (Input.GetKeyDown(debugSpawnKey))
            SpawnNextCustomer();
    }

    public void SpawnNextCustomer()
    {
        if (activeCustomer != null)
            return;

        EnsureReferences();

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
        customer.SetLoopManager(this);
        customer.SetUsesPatience(spawnedCount >= tutorialCustomerCount);
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
            activeCustomer = null;

        servedCount++;
        Debug.Log($"[CustomerSpawner] Customer left. Served={servedCount}");

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

    private GameObject GetCustomerPrefabForNextSpawn()
    {
        if (spawnedCount < tutorialCustomerCount)
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
