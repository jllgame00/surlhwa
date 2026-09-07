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
    [SerializeField] private Button nextButton;
    [SerializeField] private Button brewButton;

    [Header("Brew")]
    [SerializeField] private Button stirButton;
    [SerializeField] private Button shakeButton;
    [SerializeField] private Button buildButton;
    [SerializeField] private Button floatButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Prototype Technique Requests")]
    [SerializeField] private OrderData[] prototypeTechniqueOrders;

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

    [Header("Debug Third Customer Route")]
    [SerializeField] private bool forceThirdCustomerRoute = false;
    [SerializeField] private MiniGameType forcedThirdCustomerMethod = MiniGameType.Build;
    [SerializeField] private EmotionTag forcedThirdCustomerPrimaryTag = EmotionTag.Regret;
    [SerializeField] private EmotionTag forcedThirdCustomerSecondaryTag = EmotionTag.Comfort;

    [Header("Debug Fourth Customer Route")]
    [SerializeField] private bool forceFourthCustomerRoute = false;
    [SerializeField] private MiniGameType forcedFourthCustomerMethod = MiniGameType.Float;
    [SerializeField] private EmotionTag forcedFourthCustomerPrimaryTag = EmotionTag.Longing;
    [SerializeField] private EmotionTag forcedFourthCustomerSecondaryTag = EmotionTag.Clarity;

    [Header("Follow")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.2f, 0f);
    [SerializeField] private CustomerWaitIndicator waitIndicator;

    [Header("Method Button Visual")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.9f, 0.6f, 1f);

    private OrderManager orderManager;
    private BusinessDayManager businessDayManager;
    private CustomerController activeCustomer;

    private MiniGameType selectedMethod;
    private bool hasMethodSelected;
    private bool wantsEllipsisVisible;
    private bool forcedFirstCustomerRouteConsumed;
    private bool forcedSecondCustomerRouteConsumed;
    private bool forcedThirdCustomerRouteConsumed;
    private bool forcedFourthCustomerRouteConsumed;
    private bool activeCustomerUsesForcedRoute;
    private bool activeCustomerUsesFirstCustomerRoute;
    private bool activeCustomerUsesSecondCustomerRoute;
    private bool activeCustomerUsesThirdCustomerRoute;
    private bool activeCustomerUsesFourthCustomerRoute;
    private MiniGameType activeForcedMethod = MiniGameType.Stir;
    private string[] activePreRequestLines;
    private int activePreRequestLineIndex;
    private bool isPreRequestDialogueActive;

    public bool IsForcedFirstCustomerRouteActive => activeCustomerUsesFirstCustomerRoute;
    public bool IsForcedSecondCustomerRouteActive => activeCustomerUsesSecondCustomerRoute;
    public bool IsForcedThirdCustomerRouteActive => activeCustomerUsesThirdCustomerRoute;
    public bool IsForcedFourthCustomerRouteActive => activeCustomerUsesFourthCustomerRoute;

    private void Awake()
    {
        orderManager = FindFirstObjectByType<OrderManager>();
        businessDayManager = FindFirstObjectByType<BusinessDayManager>();

        if (orderManager != null)
            orderManager.BartendingFlowFailed += RestoreAfterBartendingFailure;

        if (waitIndicator == null && ellipsisPrompt != null)
            waitIndicator = ellipsisPrompt.GetComponentInChildren<CustomerWaitIndicator>(true);

        if (waitIndicator != null)
            waitIndicator.SetWorldCamera(worldCamera);

        if (ellipsisButton != null) ellipsisButton.onClick.AddListener(OnClickEllipsis);
        if (nextButton != null) nextButton.onClick.AddListener(OnClickNextDialogueLine);
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

    private void OnDestroy()
    {
        ResetPreRequestDialogue();

        if (orderManager != null)
            orderManager.BartendingFlowFailed -= RestoreAfterBartendingFailure;
    }

    private void OnDisable()
    {
        ResetPreRequestDialogue();
    }

    private void LateUpdate()
    {
        if (activeCustomer == null || ellipsisPrompt == null)
            return;

        if (!wantsEllipsisVisible)
        {
            ellipsisPrompt.gameObject.SetActive(false);
            return;
        }

        UpdateEllipsisPosition();
    }

    public void RegisterCustomer(CustomerController customer)
    {
        Debug.Log($"[InteractionUI] RegisterCustomer called: {(customer != null ? customer.name : "NULL")}");

        if (customer == null)
        {
            Debug.LogWarning("[InteractionUI] customer is NULL");
            return;
        }

        Vector3 anchorPos = customer.GetInteractionAnchorPosition();
        Debug.Log($"[InteractionUI] Anchor world pos: {anchorPos}");
        
        activeCustomer = customer;
        hasMethodSelected = false;
        wantsEllipsisVisible = customer != null;
        activeCustomerUsesForcedRoute = false;
        activeCustomerUsesFirstCustomerRoute = false;
        activeCustomerUsesSecondCustomerRoute = false;
        activeCustomerUsesThirdCustomerRoute = false;
        activeCustomerUsesFourthCustomerRoute = false;
        activeForcedMethod = MiniGameType.Stir;

        bool canApplyTutorialRoute = IsTutorialDay();
        if (canApplyTutorialRoute && TryApplyFirstCustomerRoute(customer))
        {
            activeCustomerUsesForcedRoute = true;
            activeCustomerUsesFirstCustomerRoute = true;
            activeForcedMethod = forcedFirstCustomerMethod;
        }
        else if (canApplyTutorialRoute && TryApplySecondCustomerRoute(customer))
        {
            activeCustomerUsesForcedRoute = true;
            activeCustomerUsesSecondCustomerRoute = true;
            activeForcedMethod = forcedSecondCustomerMethod;
        }
        else if (canApplyTutorialRoute && TryApplyThirdCustomerRoute(customer))
        {
            activeCustomerUsesForcedRoute = true;
            activeCustomerUsesThirdCustomerRoute = true;
            activeForcedMethod = forcedThirdCustomerMethod;
        }
        else if (canApplyTutorialRoute && TryApplyFourthCustomerRoute(customer))
        {
            activeCustomerUsesForcedRoute = true;
            activeCustomerUsesFourthCustomerRoute = true;
            activeForcedMethod = forcedFourthCustomerMethod;
        }

        if (waitIndicator != null)
        {
            waitIndicator.SetWorldCamera(worldCamera);
            waitIndicator.SetFollowTarget(customer.GetInteractionAnchorTransform());
        }

        ClosePanels(wantsEllipsisVisible);
        SetCustomerWaitIndicatorVisible(customer, wantsEllipsisVisible);
        SetCustomerWaitUrgency(customer, 1f);
        RefreshMethodVisuals();
        RefreshConfirmState();

        Debug.Log($"[InteractionUI] RegisterCustomer -> {(customer != null ? customer.name : "NULL")}");
    }

    public void ClearCustomer(CustomerController customer)
    {
        if (activeCustomer != customer)
            return;

        Debug.Log($"[InteractionUI] ClearCustomer -> {(customer != null ? customer.name : "NULL")}");

        SetCustomerWaitIndicatorVisible(customer, false);

        ResetPreRequestDialogue();

        activeCustomer = null;
        hasMethodSelected = false;
        wantsEllipsisVisible = false;
        activeCustomerUsesForcedRoute = false;
        activeCustomerUsesFirstCustomerRoute = false;
        activeCustomerUsesSecondCustomerRoute = false;
        activeCustomerUsesThirdCustomerRoute = false;
        activeCustomerUsesFourthCustomerRoute = false;
        activeForcedMethod = MiniGameType.Stir;

        ClosePanels(false);
        RefreshMethodVisuals();
        RefreshConfirmState();
    }

    /// <summary>
    /// Reopens only the existing Brew UI after a retry-safe additive
    /// infrastructure failure. It does not re-register the customer or reapply
    /// tutorial routing, so the current method and ingredient selection remain.
    /// </summary>
    public void RestoreAfterBartendingFailure(BartendingFlowFailure failure)
    {
        if (failure == BartendingFlowFailure.CleanupFailed)
        {
            Debug.LogError(
                "[InteractionUI] Bartending failure is not retry-safe in the current session -> " + failure);
            return;
        }

        if (activeCustomer == null)
        {
            Debug.LogWarning(
                "[InteractionUI] Bartending failure retry UI was ignored because there is no active customer.");
            return;
        }

        wantsEllipsisVisible = false;

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (brewPanel != null) brewPanel.SetActive(true);
        if (overlayBlocker != null) overlayBlocker.SetActive(true);
        if (ellipsisPrompt != null) ellipsisPrompt.gameObject.SetActive(false);
        if (waitIndicator != null) waitIndicator.SetVisible(false);

        RefreshMethodVisuals();
        RefreshConfirmState();

        Debug.Log(
            "[InteractionUI] Restored Brew panel after retry-safe Bartending failure -> " + failure);
    }

    public void SetCustomerWaitIndicatorVisible(CustomerController customer, bool visible)
    {
        if (activeCustomer != customer || waitIndicator == null)
            return;

        bool shouldShow = visible && wantsEllipsisVisible;
        waitIndicator.SetFollowTarget(shouldShow && customer != null ? customer.GetInteractionAnchorTransform() : null);
        waitIndicator.SetVisible(shouldShow);
    }

    public void SetCustomerWaitUrgency(CustomerController customer, float normalizedRemainingTime)
    {
        if (activeCustomer != customer || waitIndicator == null)
            return;

        waitIndicator.SetUrgency(normalizedRemainingTime);
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

        if (TryOpenDedicatedPreRequestDialogue())
            return;

        string line = activeCustomer.GetInteractionHintLine();
        OpenDialogue(line);
    }

    private void OpenDialogue(string line)
    {
        ResetPreRequestDialogue();
        ShowDialogue(line, false);
    }

    private bool TryOpenDedicatedPreRequestDialogue()
    {
        if (activeCustomer == null || activeCustomerUsesForcedRoute)
            return false;

        if (!activeCustomer.TryGetDedicatedRequest(out _) ||
            !activeCustomer.TryGetDialogueData(out DialogueData dialogue) ||
            dialogue.onEnterLines == null || dialogue.onEnterLines.Length == 0)
        {
            return false;
        }

        if (nextButton == null)
        {
            Debug.LogError(
                "[InteractionUI] Dedicated pre-request dialogue cannot start because NextButton is not assigned.");
            return false;
        }

        activePreRequestLines = dialogue.onEnterLines;
        activePreRequestLineIndex = 0;
        isPreRequestDialogueActive = true;
        activeCustomer.SetPatiencePausedForNarrativeDialogue(true);
        ShowDialogue(activePreRequestLines[activePreRequestLineIndex], true);

        Debug.Log(
            $"[InteractionUI] Dedicated pre-request dialogue -> {activeCustomer.name} / " +
            $"lines={activePreRequestLines.Length}"
        );
        return true;
    }

    private void OnClickNextDialogueLine()
    {
        if (!isPreRequestDialogueActive || activePreRequestLines == null)
            return;

        activePreRequestLineIndex++;
        if (activePreRequestLineIndex < activePreRequestLines.Length)
        {
            if (dialogueText != null)
                dialogueText.text = activePreRequestLines[activePreRequestLineIndex];

            return;
        }

        ResetPreRequestDialogue();
    }

    private void ShowDialogue(string line, bool showNext)
    {
        wantsEllipsisVisible = false;
        hasMethodSelected = false;

        if (dialogueText != null)
            dialogueText.text = line;

        if (overlayBlocker != null) overlayBlocker.SetActive(true);

        if (brewPanel != null) brewPanel.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (ellipsisPrompt != null) ellipsisPrompt.gameObject.SetActive(false);

        SetDialogueActionVisibility(showNext);

        RefreshMethodVisuals();
        RefreshConfirmState();
    }

    private void SetDialogueActionVisibility(bool showNext)
    {
        if (nextButton != null)
            nextButton.gameObject.SetActive(showNext);

        if (brewButton != null)
            brewButton.gameObject.SetActive(!showNext);
    }

    private void ResetPreRequestDialogue()
    {
        if (isPreRequestDialogueActive && activeCustomer != null)
            activeCustomer.SetPatiencePausedForNarrativeDialogue(false);

        activePreRequestLines = null;
        activePreRequestLineIndex = 0;
        isPreRequestDialogueActive = false;
        SetDialogueActionVisibility(false);
    }

    private void OnClickBrew()
    {
        if (activeCustomer == null)
            return;

        if (isPreRequestDialogueActive)
            return;

        if (TryStartDedicatedRequest())
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

    private bool TryStartDedicatedRequest()
    {
        if (activeCustomer == null || activeCustomerUsesForcedRoute)
            return false;

        if (!activeCustomer.TryGetDedicatedRequest(out OrderData dedicatedRequest))
            return false;

        if (orderManager == null)
            orderManager = FindFirstObjectByType<OrderManager>();

        if (orderManager == null)
        {
            Debug.LogError("[InteractionUI] OrderManager is NULL for dedicated request.");
            return true;
        }

        ClosePanels(false);

        Debug.Log(
            $"[InteractionUI] Dedicated request -> {activeCustomer.name} / " +
            $"{dedicatedRequest.orderName} / {dedicatedRequest.miniGameType}"
        );

        orderManager.PlaySpecificOrder(dedicatedRequest, activeCustomer);
        return true;
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

        OrderData order = FindPrototypeOrder(selectedMethod);
        if (order == null)
        {
            return;
        }

        ClosePanels(false);

        if (orderManager == null)
            orderManager = FindFirstObjectByType<OrderManager>();

        if (orderManager == null)
        {
            Debug.LogError("[InteractionUI] OrderManager is NULL");
            return;
        }

        Debug.Log($"[InteractionUI] Confirm -> {selectedMethod} / {order.orderName} / IngredientAuthority=BartendingShelf");

        orderManager.PlaySpecificOrder(order, activeCustomer);
    }

    private OrderData FindPrototypeOrder(MiniGameType type)
    {
        if (prototypeTechniqueOrders == null || prototypeTechniqueOrders.Length == 0)
        {
            Debug.LogError(
                $"[InteractionUI] Prototype technique request collection is empty for method={type} on {name}."
            );
            return null;
        }

        OrderData match = null;
        int matchCount = 0;
        int nullEntryCount = 0;

        foreach (OrderData candidate in prototypeTechniqueOrders)
        {
            if (candidate == null)
            {
                nullEntryCount++;
                continue;
            }

            if (candidate.miniGameType != type)
                continue;

            match = candidate;
            matchCount++;
        }

        if (nullEntryCount > 0)
        {
            Debug.LogError(
                $"[InteractionUI] Prototype technique request collection has {nullEntryCount} null entry/entries on {name}."
            );
        }

        if (matchCount == 1)
            return match;

        if (matchCount == 0)
        {
            Debug.LogError(
                $"[InteractionUI] Prototype technique request missing for method={type} on {name}."
            );
        }
        else
        {
            Debug.LogError(
                $"[InteractionUI] Prototype technique request is ambiguous for method={type} on {name}. Matches={matchCount}."
            );
        }

        return null;
    }

    private bool IsTutorialDay()
    {
        if (businessDayManager == null)
            businessDayManager = FindFirstObjectByType<BusinessDayManager>();

        return businessDayManager != null && businessDayManager.CurrentDay == 1;
    }

    private bool TryApplyFirstCustomerRoute(CustomerController customer)
    {
        if (!forceFirstCustomerRoute)
            return false;

        if (forcedFirstCustomerRouteConsumed)
            return false;

        if (customer == null)
            return false;

        OrderData forcedOrder = FindPrototypeOrder(forcedFirstCustomerMethod);
        if (forcedOrder == null)
        {
            Debug.LogWarning(
                $"[InteractionUI] Debug first route requested {forcedFirstCustomerMethod}, but no prototype order is assigned."
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

        OrderData forcedOrder = FindPrototypeOrder(forcedSecondCustomerMethod);
        if (forcedOrder == null)
        {
            Debug.LogWarning(
                $"[InteractionUI] Debug second route requested {forcedSecondCustomerMethod}, but no prototype order is assigned."
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

    private bool TryApplyThirdCustomerRoute(CustomerController customer)
    {
        if (!forceThirdCustomerRoute)
            return false;

        if (!forceFirstCustomerRoute || !forcedFirstCustomerRouteConsumed)
            return false;

        if (!forceSecondCustomerRoute || !forcedSecondCustomerRouteConsumed)
            return false;

        if (forcedThirdCustomerRouteConsumed)
            return false;

        if (customer == null)
            return false;

        OrderData forcedOrder = FindPrototypeOrder(forcedThirdCustomerMethod);
        if (forcedOrder == null)
        {
            Debug.LogWarning(
                $"[InteractionUI] Debug third route requested {forcedThirdCustomerMethod}, but no prototype order is assigned."
            );
            return false;
        }

        customer.ForceEmotionRequestForDebug(
            forcedThirdCustomerPrimaryTag,
            forcedThirdCustomerSecondaryTag
        );

        forcedThirdCustomerRouteConsumed = true;

        Debug.Log(
            $"[InteractionUI] Debug third route armed -> " +
            $"Method={forcedThirdCustomerMethod}, Order={forcedOrder.orderName}, " +
            $"Emotion={forcedThirdCustomerPrimaryTag}+{forcedThirdCustomerSecondaryTag}"
        );

        return true;
    }

    private bool TryApplyFourthCustomerRoute(CustomerController customer)
    {
        if (!forceFourthCustomerRoute)
            return false;

        if (!forceFirstCustomerRoute || !forcedFirstCustomerRouteConsumed)
            return false;

        if (!forceSecondCustomerRoute || !forcedSecondCustomerRouteConsumed)
            return false;

        if (!forceThirdCustomerRoute || !forcedThirdCustomerRouteConsumed)
            return false;

        if (forcedFourthCustomerRouteConsumed)
            return false;

        if (customer == null)
            return false;

        OrderData forcedOrder = FindPrototypeOrder(forcedFourthCustomerMethod);
        if (forcedOrder == null)
        {
            Debug.LogWarning(
                $"[InteractionUI] Debug fourth route requested {forcedFourthCustomerMethod}, but no prototype order is assigned."
            );
            return false;
        }

        customer.ForceEmotionRequestForDebug(
            forcedFourthCustomerPrimaryTag,
            forcedFourthCustomerSecondaryTag
        );

        forcedFourthCustomerRouteConsumed = true;

        Debug.Log(
            $"[InteractionUI] Debug fourth route armed -> " +
            $"Method={forcedFourthCustomerMethod}, Order={forcedOrder.orderName}, " +
            $"Emotion={forcedFourthCustomerPrimaryTag}+{forcedFourthCustomerSecondaryTag}"
        );

        return true;
    }

    private void OnClickCancel()
    {
        // BrewPanel에서 취소하면 DialoguePanel로 복귀
        ResetPreRequestDialogue();

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
        ResetPreRequestDialogue();
        wantsEllipsisVisible = keepEllipsis;

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (brewPanel != null) brewPanel.SetActive(false);
        if (overlayBlocker != null) overlayBlocker.SetActive(false);

        if (ellipsisPrompt != null)
        {
            bool showEllipsis = keepEllipsis && activeCustomer != null;
            ellipsisPrompt.gameObject.SetActive(showEllipsis);

            if (waitIndicator != null)
                waitIndicator.SetVisible(showEllipsis);
        }
    }
}
