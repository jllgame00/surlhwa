using UnityEngine;

public class OrderManager : MonoBehaviour
{
    private CustomerController currentCustomer;
    private string currentOrderName;

    private UIManager ui;
    private GameManager gm;

    public OrderData[] orderPool;
    private OrderData currentOrder;

    void Awake()
    {
        ui = FindFirstObjectByType<UIManager>();
        gm = FindFirstObjectByType<GameManager>();
    }

    public void IssueOrderTo(CustomerController cc)
    {
        currentCustomer = cc;
        currentOrder = orderPool[Random.Range(0, orderPool.Length)];

        if (ui != null)
            ui.SetOrderText($"ORDER: {currentOrder.orderName}");
    }

    // UI 버튼에서 호출
    public void ServeCurrentOrder()
    {
        if (currentCustomer == null || currentOrder == null) return;

        if (gm != null)
            gm.AddReputation(currentOrder.reputationReward);

        currentCustomer.OnServed(true);
        currentCustomer = null;
        currentOrder = null;

        if (ui != null) ui.SetOrderText("ORDER: -");
    }
}
