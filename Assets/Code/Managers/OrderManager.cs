using UnityEngine;
using System;

public class OrderManager : MonoBehaviour
{
    // ====== Debug: track state changes ======
    [Header("Debug")]
    [SerializeField] private bool allowPlayWithoutCustomer = true; // 디버그용: customer 없어도 미니게임 전환 테스트

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
    // (원하면 더 엄격히: currentCustomer != null && currentOrder != null)

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

        // 이미 주문이 활성 상태면 새로 발급하지 않음 (Update에서 반복 호출되는 문제 방지)
        if (HasActiveOrder)
        {
            // 같은 손님에게 이미 발급된 상태면 그냥 무시
            if (currentCustomer == cc) return;

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
            ui.SetOrderText($"ORDER: {currentOrder.orderName}");
    }

    // UI 버튼에서 호출 (UIManager가 이 메서드 호출)
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
            // 지금 너 상황 디버깅용: customer가 왜 null 되는지 추적하면서도 미니게임 전환은 확인 가능하게
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

        // 성공/실패에 따른 처리
        if (result.success)
        {
            // 평판: 기본 보상 + 미니게임 보정
            if (gm != null)
            {
                int totalRep = 0;
                if (currentOrder != null) totalRep += currentOrder.reputationReward;
                totalRep += result.repDelta;
                gm.AddReputation(totalRep);
            }

            if (currentCustomer != null)
                currentCustomer.OnServed(true);
        }
        else
        {
            // 실패 시 repDelta가 음수면 깎임
            if (gm != null)
                gm.AddReputation(result.repDelta);

            if (currentCustomer != null)
                currentCustomer.OnServed(false);
        }

        // 상태 초기화
        currentCustomer = null; // setter가 추적 로그 찍음
        currentOrder = null;

        if (ui != null) ui.SetOrderText("ORDER: -");
    }

    // 필요하면 외부에서 강제로 리셋할 때 사용
    public void ForceClearOrder()
    {
        Debug.LogWarning("[OrderManager] ForceClearOrder()");
        currentCustomer = null;
        currentOrder = null;
        if (ui != null) ui.SetOrderText("ORDER: -");
    }
}
