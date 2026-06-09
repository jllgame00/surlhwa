using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionUIController : MonoBehaviour
{
    [Header("Core Panels")]
    [SerializeField] private RectTransform ellipsisPrompt;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private GameObject brewPanel;
    [SerializeField] private GameObject overlayBlocker;

    [Header("Dialogue")]
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button ellipsisButton;
    [SerializeField] private Button brewButton;

    [Header("Brew")]
    [SerializeField] private Button stirButton;
    [SerializeField] private Button shakeButton;
    [SerializeField] private Button buildButton;
    [SerializeField] private Button floatButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Test Orders")]
    [SerializeField] private OrderData testStirOrder;
    [SerializeField] private OrderData testShakeOrder;
    [SerializeField] private OrderData testBuildOrder;
    [SerializeField] private OrderData testFloatOrder;

    [Header("Debug First Customer Route")]
    [SerializeField] private bool forceFirstCustomerRoute = false;
    [SerializeField] private MiniGameType forcedFirstCustomerMethod = MiniGameType.Stir;
    [SerializeField] private EmotionTag forcedFirstCustomerPrimaryTag = EmotionTag.Loneliness;
    [SerializeField] private EmotionTag forcedFirstCustomerSecondaryTag = EmotionTag.Warmth;
    [SerializeField] private bool autoSelectForcedMethodOnBrew = true;

    [Header("Debug Second Customer Route")]
    [SerializeField] private bool forceSecondCustomerRoute = false;
    [SerializeField] private MiniGameType forcedSecondCustomerMethod = MiniGameType.Shake;
    [SerializeField] private EmotionTag forcedSecondCustomerPrimaryTag = EmotionTag.Awakening;
    [SerializeField] private EmotionTag forcedSecondCustomerSecondaryTag = EmotionTag.Clarity;

    [Header("Follow")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.2f, 0f);

    [Header("Method Button Visual")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.9f, 0.6f, 1f);

    [SerializeField] private BrewSelectionController brewSelectionController;

    private OrderManager orderManager;
    private CustomerController activeCustomer;

    private MiniGameType selectedMethod;
    private bool hasMethodSelected;
    private bool wantsEllipsisVisible;
    private bool forcedFirstCustomerRouteConsumed;
    private bool forcedSecondCustomerRouteConsumed;
    private bool activeCustomerUsesForcedRoute;
    private bool activeCustomerUsesFirstCustomerRoute;
    private MiniGameType activeForcedMethod = MiniGameType.Stir;

    public bool IsForcedFirstCustomerRouteActive => activeCustomerUsesFirstCustomerRoute;

    private void Awake()
    {
        orderManager = FindFirstObjectByType<OrderManager>();

        if (brewSelectionController == null && brewPanel != null)
            brewSelectionController = brewPanel.GetComponentInChildren<BrewSelectionController>(true);

        if (ellipsisButton != null) ellipsisButton.onClick.AddListener(OnClickEllipsis);
        if (brewButton != null) brewButton.onClick.AddListener(OnClickBrew);

        if (stirButton != null) stirButton.onClick.AddListener(() => SelectMethod(MiniGameType.Stir));
        if (shakeButton != null) shakeButton.onClick.AddListener(() => SelectMethod(MiniGameType.Shake));
        if (buildButton != null) buildButton.onClick.AddListener(() => SelectMethod(MiniGameType.Build));
        if (floatButton != null) floatButton.onClick.AddListener(() => SelectMethod(MiniGameType.Float));

        if (confirmButton != null) confirmButton.onClick.AddListener(OnClickConfirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnClickCancel);

        hasMethodSelected = false;
        wantsEllipsisVisible = false;

        ClosePanels(false);
        RefreshMethodVisuals();
        RefreshConfirmState();
    }

    private void LateUpdate()
    {
        if (activeCustomer == null || ellipsisPrompt == null)
            return;

        UpdateEllipsisPosition();
    }

    public void RegisterCustomer(CustomerController customer)
    {
        activeCustomer = customer;
        hasMethodSelected = false;
        wantsEllipsisVisible = customer != null;
        activeCustomerUsesForcedRoute = false;
        activeCustomerUsesFirstCustomerRoute = false;
        activeForcedMethod = MiniGameType.Stir;

        if (TryApplyFirstCustomerRoute(customer))
        {
            activeCustomerUsesForcedRoute = true;
            activeCustomerUsesFirstCustomerRoute = true;
            activeForcedMethod = forcedFirstCustomerMethod;
        }
        else if (TryApplySecondCustomerRoute(customer))
        {
            activeCustomerUsesForcedRoute = true;
            activeForcedMethod = forcedSecondCustomerMethod;
        }

        ClosePanels(wantsEllipsisVisible);
        RefreshMethodVisuals();
        RefreshConfirmState();

        Debug.Log($"[InteractionUI] RegisterCustomer -> {(customer != null ? customer.name : "NULL")}");
    }

    public void ClearCustomer(CustomerController customer)
    {
        if (activeCustomer != customer)
            return;

        Debug.Log($"[InteractionUI] ClearCustomer -> {(customer != null ? customer.name : "NULL")}");

        activeCustomer = null;
        hasMethodSelected = false;
        wantsEllipsisVisible = false;
        activeCustomerUsesForcedRoute = false;
        activeCustomerUsesFirstCustomerRoute = false;
        activeForcedMethod = MiniGameType.Stir;

        ClosePanels(false);
        RefreshMethodVisuals();
        RefreshConfirmState();
    }

    private void UpdateEllipsisPosition()
    {
        Camera cam = worldCamera != null ? worldCamera : Camera.main;
        if (cam == null || activeCustomer == null || ellipsisPrompt == null)
            return;

        Vector3 worldPos = activeCustomer.GetInteractionAnchorPosition() + worldOffset;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);

        Canvas canvas = ellipsisPrompt.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam,
            out Vector2 localPoint))
        {
            ellipsisPrompt.anchoredPosition = localPoint;
            ellipsisPrompt.gameObject.SetActive(true);
        }
    }

    private void OnClickEllipsis()
    {
        if (activeCustomer == null)
            return;

        ClosePanels(false);

        string line = activeCustomer.GetInteractionHintLine();
        OpenDialogue(line);
    }

    private void OpenDialogue(string line)
    {
        wantsEllipsisVisible = false;
        hasMethodSelected = false;

        if (dialogueText != null)
            dialogueText.text = line;

        if (overlayBlocker != null) overlayBlocker.SetActive(true);

        if (brewPanel != null) brewPanel.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (ellipsisPrompt != null) ellipsisPrompt.gameObject.SetActive(false);

        RefreshMethodVisuals();
        RefreshConfirmState();
    }

    private void OnClickBrew()
    {
        if (activeCustomer == null)
            return;

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (brewPanel != null) brewPanel.SetActive(true);
        if (overlayBlocker != null) overlayBlocker.SetActive(true);

        hasMethodSelected = false;
        RefreshMethodVisuals();
        RefreshConfirmState();

        if (activeCustomerUsesForcedRoute && autoSelectForcedMethodOnBrew)
            SelectMethod(activeForcedMethod);
    }

    private void SelectMethod(MiniGameType type)
    {
        if (activeCustomerUsesForcedRoute && type != activeForcedMethod)
        {
            Debug.Log(
                $"[InteractionUI] Debug route locks method to {activeForcedMethod}. " +
                $"Ignoring requested method={type}."
            );

            type = activeForcedMethod;
        }
        selectedMethod = type;
        hasMethodSelected = true;

        OrderData order = GetTestOrder(selectedMethod);

        if (brewSelectionController != null)
        {
            brewSelectionController.Begin(order);
            brewSelectionController.SetMethod(type);
        }

        Debug.Log($"[InteractionUI] SelectMethod -> {type}");

        RefreshMethodVisuals();
        RefreshConfirmState();
    }

    private void RefreshMethodVisuals()
    {
        SetButtonSelected(stirButton, hasMethodSelected && selectedMethod == MiniGameType.Stir);
        SetButtonSelected(shakeButton, hasMethodSelected && selectedMethod == MiniGameType.Shake);
        SetButtonSelected(buildButton, hasMethodSelected && selectedMethod == MiniGameType.Build);
        SetButtonSelected(floatButton, hasMethodSelected && selectedMethod == MiniGameType.Float);
    }

    private void RefreshConfirmState()
    {
        if (confirmButton != null)
            confirmButton.interactable = hasMethodSelected;
    }

    private void SetButtonSelected(Button button, bool selected)
    {
        if (button == null || button.image == null)
            return;

        button.image.color = selected ? selectedColor : normalColor;
    }

    private void OnClickConfirm()
    {
        if (activeCustomer == null)
        {
            Debug.LogWarning("[InteractionUI] activeCustomer is NULL");
            return;
        }

        if (!hasMethodSelected)
        {
            Debug.LogWarning("[InteractionUI] No method selected");
            return;
        }

        OrderData order = GetTestOrder(selectedMethod);
        if (order == null)
        {
            Debug.LogError($"[InteractionUI] Test order missing for method={selectedMethod}");
            return;
        }

        BrewSessionData selectedSession = null;

        if (brewSelectionController != null && brewSelectionController.HasAnySelectedIngredient())
        {
            selectedSession = brewSelectionController.BuildSelectedSession();
        }

        ClosePanels(false);

        if (orderManager == null)
            orderManager = FindFirstObjectByType<OrderManager>();

        if (orderManager == null)
        {
            Debug.LogError("[InteractionUI] OrderManager is NULL");
            return;
        }

        Debug.Log($"[InteractionUI] Confirm -> {selectedMethod} / {order.orderName} / HasSelectedSession={selectedSession != null}");

        orderManager.PlaySpecificOrder(order, activeCustomer, selectedSession);
    }

    private OrderData GetTestOrder(MiniGameType type)
    {
        return type switch
        {
            MiniGameType.Stir => testStirOrder,
            MiniGameType.Shake => testShakeOrder,
            MiniGameType.Build => testBuildOrder,
            MiniGameType.Float => testFloatOrder,
            _ => null
        };
    }

    private bool TryApplyFirstCustomerRoute(CustomerController customer)
    {
        if (!forceFirstCustomerRoute)
            return false;

        if (forcedFirstCustomerRouteConsumed)
            return false;

        if (customer == null)
            return false;

        OrderData forcedOrder = GetTestOrder(forcedFirstCustomerMethod);
        if (forcedOrder == null)
        {
            Debug.LogWarning(
                $"[InteractionUI] Debug first route requested {forcedFirstCustomerMethod}, but no test order is assigned."
            );
            return false;
        }

        customer.ForceEmotionRequestForDebug(
            forcedFirstCustomerPrimaryTag,
            forcedFirstCustomerSecondaryTag
        );

        forcedFirstCustomerRouteConsumed = true;

        Debug.Log(
            $"[InteractionUI] Debug first route armed -> " +
            $"Method={forcedFirstCustomerMethod}, Order={forcedOrder.orderName}, " +
            $"Emotion={forcedFirstCustomerPrimaryTag}+{forcedFirstCustomerSecondaryTag}"
        );

        return true;
    }

    private bool TryApplySecondCustomerRoute(CustomerController customer)
    {
        if (!forceSecondCustomerRoute)
            return false;

        if (!forceFirstCustomerRoute || !forcedFirstCustomerRouteConsumed)
            return false;

        if (forcedSecondCustomerRouteConsumed)
            return false;

        if (customer == null)
            return false;

        OrderData forcedOrder = GetTestOrder(forcedSecondCustomerMethod);
        if (forcedOrder == null)
        {
            Debug.LogWarning(
                $"[InteractionUI] Debug second route requested {forcedSecondCustomerMethod}, but no test order is assigned."
            );
            return false;
        }

        customer.ForceEmotionRequestForDebug(
            forcedSecondCustomerPrimaryTag,
            forcedSecondCustomerSecondaryTag
        );

        forcedSecondCustomerRouteConsumed = true;

        Debug.Log(
            $"[InteractionUI] Debug second route armed -> " +
            $"Method={forcedSecondCustomerMethod}, Order={forcedOrder.orderName}, " +
            $"Emotion={forcedSecondCustomerPrimaryTag}+{forcedSecondCustomerSecondaryTag}"
        );

        return true;
    }

    private void OnClickCancel()
    {
        // BrewPanel에서 취소하면 DialoguePanel로 복귀
        if (brewPanel != null && brewPanel.activeSelf)
        {
            string line = activeCustomer != null ? activeCustomer.GetInteractionHintLine() : "";
            OpenDialogue(line);
            return;
        }

        // DialoguePanel에서 취소하면 다시 ellipsis로
        wantsEllipsisVisible = activeCustomer != null;
        ClosePanels(wantsEllipsisVisible);
    }

    private void ClosePanels(bool keepEllipsis)
    {
        wantsEllipsisVisible = keepEllipsis;

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (brewPanel != null) brewPanel.SetActive(false);
        if (overlayBlocker != null) overlayBlocker.SetActive(false);

        if (ellipsisPrompt != null)
            ellipsisPrompt.gameObject.SetActive(keepEllipsis && activeCustomer != null);
    }
}
