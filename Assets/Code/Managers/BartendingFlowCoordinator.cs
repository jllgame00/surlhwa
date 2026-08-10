using System;
using UnityEngine;

/// <summary>
/// BAR_LOCAL boundary between Bar-side order ownership and local MiniGame execution.
/// C2 intentionally delegates to the existing same-Scene MiniGameManager.
/// </summary>
public class BartendingFlowCoordinator : MonoBehaviour
{
    [SerializeField] private MiniGameManager miniGameManager;

    private bool isBartendingActive;

    public bool IsBartendingActive =>
        isBartendingActive || (miniGameManager != null && miniGameManager.IsPlaying);

    private void Awake()
    {
        ResolveMiniGameManager();
    }

    public bool BeginBartending(BartendingSession session, Action<MiniGameResult> onCompleted)
    {
        if (session == null || session.Order == null)
        {
            Debug.LogError("[BartendingFlowCoordinator] BeginBartending requires an OrderData session.");
            return false;
        }

        if (IsBartendingActive)
        {
            Debug.LogWarning("[BartendingFlowCoordinator] BeginBartending ignored because Bartending is already active.");
            return false;
        }

        if (!ResolveMiniGameManager())
        {
            Debug.LogError("[BartendingFlowCoordinator] MiniGameManager is NULL.");
            return false;
        }

        bool completionDelivered = false;
        isBartendingActive = true;

        miniGameManager.Play(session.Order, session.InitialSession, result =>
        {
            if (completionDelivered)
            {
                Debug.LogWarning("[BartendingFlowCoordinator] Duplicate MiniGame completion ignored.");
                return;
            }

            completionDelivered = true;
            isBartendingActive = false;
            onCompleted?.Invoke(result);
        });

        if (!completionDelivered && !miniGameManager.IsPlaying)
        {
            isBartendingActive = false;
            Debug.LogError("[BartendingFlowCoordinator] MiniGameManager did not start Bartending.");
            return false;
        }

        Debug.Log(
            "[BartendingFlowCoordinator] BeginBartending -> " +
            session.Order.orderName + " / " + session.Method);

        return true;
    }

    private bool ResolveMiniGameManager()
    {
        if (miniGameManager == null)
            miniGameManager = FindFirstObjectByType<MiniGameManager>();

        return miniGameManager != null;
    }
}
