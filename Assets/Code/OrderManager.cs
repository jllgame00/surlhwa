using UnityEngine;

public class OrderManager : MonoBehaviour
{
    private CustomerController currentCustomer;
    private string currentOrderName;

    private UIManager ui;
    private GameManager gm;

    void Awake()
    {
        ui = FindFirstObjectByType<UIManager>();
        gm = FindFirstObjectByType<GameManager>();
    }

    public void IssueOrderTo(CustomerController cc)
    {
        currentCustomer = cc;
        currentOrderName = GetRandomOrderName();

        if (ui != null) ui.SetOrderText($"ORDER: {currentOrderName} (Press Serve)");
    }

    string GetRandomOrderName()
    {
        // 최소: 문자열만. 나중에 ScriptableObject로 승격
        string[] pool = { "Snow Bloom", "Warm Tea", "Bitter Moon", "Silent Gin" };
        return pool[Random.Range(0, pool.Length)];
    }

    // UI 버튼에서 호출
    public void ServeCurrentOrder()
    {
        if (currentCustomer == null) return;

        bool success = true; // 지금은 무조건 성공. 미니게임 붙일 때 여기서 판정

        if (gm != null) gm.OnOrderResolved(success);

        currentCustomer.OnServed(success);
        currentCustomer = null;

        if (ui != null) ui.SetOrderText("ORDER: -");
    }
}
