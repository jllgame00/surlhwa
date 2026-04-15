using UnityEngine;
using System;

public class OrderManager : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool allowPlayWithoutCustomer = true;

    // ====== Runtime State ======
    private CustomerController _currentCustomer;
    private CustomerController currentCustomer
    {
        get => _currentCustomer;
        set
        {
            if (_currentCustomer != value)
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

    [Header("MiniGame")]
    [SerializeField] private MiniGameManager miniGameManager;

    private bool HasActiveOrder => currentOrder != null;

    void Awake()
    {
        ui = FindFirstObjectByType<UIManager>();
        gm = FindFirstObjectByType<GameManager>();

        if (miniGameManager == null)
            miniGameManager = FindFirstObjectByType<MiniGameManager>();

        Debug.Log($"[OrderManager] Awake name={name} id={GetInstanceID()}");
    }

    public void IssueOrderTo(CustomerController cc)
    {
        if (cc == null)
        {
            Debug.LogError("[OrderManager] IssueOrderTo called with NULL customer");
            return;
        }

        // 이미 주문이 있으면 반복 발급 방지
        if (HasActiveOrder)
        {
            if (currentCustomer == cc) return; // 같은 손님이 계속 Update에서 호출하는 거 무시
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

    // UI 버튼에서 호출
    public void ResolveServe()
    {
        Debug.Log($"[OrderManager] ResolveServe id={GetInstanceID()} called");

        if (currentOrder == null)
        {
            Debug.LogError("[OrderManager] currentOrder is NULL (no active order)");
            return;
        }

        if (miniGameManager == null)
        {
            Debug.LogError("[OrderManager] miniGameManager is NULL (assign it in Inspector or place it in the scene)");
            return;
        }

        if (currentCustomer == null)
        {
            Debug.LogWarning("[OrderManager] currentCustomer is NULL (no active customer)");

            if (!allowPlayWithoutCustomer)
                return;

            Debug.LogWarning("[OrderManager] allowPlayWithoutCustomer=true -> still playing minigame for debugging");
        }

        Debug.Log($"[OrderManager] -> MiniGameManager.Play({currentOrder.orderName})");
        miniGameManager.Play(currentOrder, OnMiniGameDone);
    }

    private void OnMiniGameDone(MiniGameResult result)
    {
        Debug.Log($"[OrderManager] MiniGame Done => success={result.success}, repDelta={result.repDelta}, grade={result.grade}");

        // 평판 처리
        if (gm != null)
        {
            if (result.success)
            {
                int totalRep = 0;
                if (currentOrder != null) totalRep += currentOrder.reputationReward;
                totalRep += result.repDelta;
                gm.AddReputation(totalRep);
            }
            else
            {
                gm.AddReputation(result.repDelta);
            }
        }

        // 새 반응 시스템 호출
        if (currentCustomer != null)
            currentCustomer.OnServedWithReaction(currentOrder, result);

        // 상태 초기화
        currentCustomer = null;
        currentOrder = null;

        if (ui != null)
        {
            Debug.Log("[OrderManager] OnMiniGameDone -> resetting ORDER text to -");
            ui.SetOrderText("ORDER: -");
        }
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

        if (miniGameManager == null)
            miniGameManager = FindFirstObjectByType<MiniGameManager>();

        if (miniGameManager == null)
        {
            Debug.LogError("[OrderManager] miniGameManager is NULL");
            return;
        }

        currentCustomer = customer;
        currentOrder = order;

        if (ui != null)
            ui.SetOrderText($"ORDER: {currentOrder.orderName}");

        Debug.Log($"[OrderManager] PlaySpecificOrder -> {currentOrder.orderName}");
        miniGameManager.Play(currentOrder, OnMiniGameDone);
    }
}