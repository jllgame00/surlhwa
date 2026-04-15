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

    [Header("Follow")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Method Button Visual")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.9f, 0.6f, 1f);

    private OrderManager orderManager;
    private CustomerController activeCustomer;

    private MiniGameType selectedMethod;
    private bool hasMethodSelected;

    private void Awake()
    {
        orderManager = FindFirstObjectByType<OrderManager>();

        if (ellipsisButton != null) ellipsisButton.onClick.AddListener(OnClickEllipsis);
        if (brewButton != null) brewButton.onClick.AddListener(OnClickBrew);

        if (stirButton != null) stirButton.onClick.AddListener(() => SelectMethod(MiniGameType.Stir));
        if (shakeButton != null) shakeButton.onClick.AddListener(() => SelectMethod(MiniGameType.Shake));
        if (buildButton != null) buildButton.onClick.AddListener(() => SelectMethod(MiniGameType.Build));
        if (floatButton != null) floatButton.onClick.AddListener(() => SelectMethod(MiniGameType.Float));

        if (confirmButton != null) confirmButton.onClick.AddListener(OnClickConfirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnClickCancel);

        ClosePanels(keepEllipsis: false);
        RefreshMethodVisuals();
    }

    private void LateUpdate()
    {
        if (activeCustomer == null || ellipsisPrompt == null || !ellipsisPrompt.gameObject.activeSelf)
            return;

        UpdateEllipsisPosition();
    }

    public void RegisterCustomer(CustomerController customer)
    {
        activeCustomer = customer;
        hasMethodSelected = false;

        ClosePanels(keepEllipsis: customer != null);
        RefreshMethodVisuals();
    }

    public void ClearCustomer(CustomerController customer)
    {
        if (activeCustomer != customer)
            return;

        activeCustomer = null;
        ClosePanels(keepEllipsis: false);
    }

    private void UpdateEllipsisPosition()
    {
        Camera cam = worldCamera != null ? worldCamera : Camera.main;
        if (cam == null || activeCustomer == null)
            return;

        Vector3 worldPos = activeCustomer.GetInteractionAnchorPosition() + worldOffset;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        if (screenPos.z <= 0f)
        {
            ellipsisPrompt.gameObject.SetActive(false);
            return;
        }

        ellipsisPrompt.gameObject.SetActive(true);
        ellipsisPrompt.position = screenPos;
    }

    private void OnClickEllipsis()
    {
        if (activeCustomer == null)
            return;

        string line = activeCustomer.GetInteractionHintLine();
        OpenDialogue(line);
    }

    private void OpenDialogue(string line)
    {
        if (dialogueText != null)
            dialogueText.text = line;

        if (overlayBlocker != null) overlayBlocker.SetActive(true);
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (brewPanel != null) brewPanel.SetActive(false);
        if (ellipsisPrompt != null) ellipsisPrompt.gameObject.SetActive(false);
    }

    private void OnClickBrew()
    {
        if (overlayBlocker != null) overlayBlocker.SetActive(true);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (brewPanel != null) brewPanel.SetActive(true);

        hasMethodSelected = false;
        RefreshMethodVisuals();
    }

    private void SelectMethod(MiniGameType type)
    {
        selectedMethod = type;
        hasMethodSelected = true;
        RefreshMethodVisuals();
    }

    private void RefreshMethodVisuals()
    {
        SetButtonSelected(stirButton, hasMethodSelected && selectedMethod == MiniGameType.Stir);
        SetButtonSelected(shakeButton, hasMethodSelected && selectedMethod == MiniGameType.Shake);
        SetButtonSelected(buildButton, hasMethodSelected && selectedMethod == MiniGameType.Build);
        SetButtonSelected(floatButton, hasMethodSelected && selectedMethod == MiniGameType.Float);
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

        ClosePanels(keepEllipsis: false);

        if (orderManager == null)
            orderManager = FindFirstObjectByType<OrderManager>();

        if (orderManager == null)
        {
            Debug.LogError("[InteractionUI] OrderManager is NULL");
            return;
        }

        orderManager.PlaySpecificOrder(order, activeCustomer);
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

    private void OnClickCancel()
    {
        ClosePanels(keepEllipsis: activeCustomer != null);
    }

    private void ClosePanels(bool keepEllipsis)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (brewPanel != null) brewPanel.SetActive(false);
        if (overlayBlocker != null) overlayBlocker.SetActive(false);

        if (ellipsisPrompt != null)
            ellipsisPrompt.gameObject.SetActive(keepEllipsis && activeCustomer != null);
    }
}