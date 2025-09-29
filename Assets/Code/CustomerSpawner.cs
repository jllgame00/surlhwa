using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    public GameObject customerPrefab;   // 스폰할 손님 프리팹
    public Transform spawnPoint;        // 출입구 위치(빈 오브젝트로 지정)

    void Update()
    {
        // 테스트용: 스페이스바 누르면 손님 소환
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnCustomer();
        }
    }

    void SpawnCustomer()
    {
        if (customerPrefab != null && spawnPoint != null)
        {
            Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("Prefab이나 SpawnPoint가 지정되지 않았음!");
        }
    }
}
