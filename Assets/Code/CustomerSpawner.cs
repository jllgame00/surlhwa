using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    public GameObject customerPrefab;
    public Transform spawnPoint;
    public SeatManager seatManager;
    public Transform defaultExitPoint;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            SpawnCustomer();
    }

    void SpawnCustomer()
    {
        if (customerPrefab == null || spawnPoint == null) return;

        var go = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);

        var cc = go.GetComponent<CustomerController>();
        if (cc != null)
        {
            cc.defaultExitPoint = defaultExitPoint;

            if (seatManager != null)
            {
                var seat = seatManager.GetFreeSeat(cc);
                cc.AssignSeat(seat);
            }
        }
    }
}
