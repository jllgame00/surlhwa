using UnityEngine;
using TMPro;
using System;

public class UIManager : MonoBehaviour
{
    public TMP_Text moneyText;
    public TMP_Text repText;
    public TMP_Text orderText;

    [SerializeField] private OrderManager orderManager;
    [SerializeField] private TMP_Text dialogueText;

    public void SetReputation(int rep)
    {
        if (repText != null)
            repText.text = $"Rep: {rep}";
    }

    public void SetOrderText(string text)
    {
        Debug.Log(
            $"[UIManager] SetOrderText -> {text}\n" +
            $"[UIManager] Caller StackTrace:\n{Environment.StackTrace}"
        );

        if (orderText != null)
            orderText.text = text;
    }

    public void OnClickServe()
    {
        Debug.Log("[UI] Serve clicked");

        if (orderManager == null)
        {
            Debug.LogError("[UI] orderManager is NULL (assign it in Inspector)");
            return;
        }

        Debug.Log("[UI] calling OrderManager.ResolveServe()");
        orderManager.ResolveServe();
        Debug.Log("[UI] returned from OrderManager.ResolveServe()");
    }

    public void ShowDialogue(string text)
    {
        if (dialogueText == null) return;

        dialogueText.text = text;
        dialogueText.gameObject.SetActive(true);
    }
}