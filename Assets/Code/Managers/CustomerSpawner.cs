using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    public GameObject customerPrefab;
    public Transform spawnPoint;
    public SeatManager seatManager;
    public Transform defaultExitPoint;
    public SpecialGuestData[] specialGuests;
    private GameManager gm;
    [SerializeField] private MiniGameManager miniGameManager;


    void Update()
    {   
        if (miniGameManager != null && miniGameManager.IsPlaying)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
            SpawnCustomer();
    }

    void SpawnCustomer()
    {
        if (customerPrefab == null || spawnPoint == null) return;

        GameObject prefabToSpawn = customerPrefab;

        // 특별 손님 체크
        if (gm != null && specialGuests != null)
        {
            foreach (var guest in specialGuests)
            {
                if (gm.reputation >= guest.requiredReputation)
                {
                    prefabToSpawn = guest.prefab;
                    break; // 지금은 1명만
                }
            }
        }

        var go = Instantiate(prefabToSpawn, spawnPoint.position, Quaternion.identity);

        var cc = go.GetComponent<CustomerController>();
        Debug.Log($"[Spawner] cc={(cc ? "OK" : "NULL")}");
        if (cc != null && seatManager != null)
        {
            var seat = seatManager.GetFreeSeat(cc);
            Debug.Log($"[Spawner] seat={(seat ? seat.name : "NULL")}");
            cc.AssignSeat(seat);
        }
    }


    void Awake()
    {
        gm = FindFirstObjectByType<GameManager>();

        if (miniGameManager == null)
            miniGameManager = FindFirstObjectByType<MiniGameManager>();
    }
}
