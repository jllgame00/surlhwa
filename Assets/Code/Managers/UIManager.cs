using UnityEngine;
using TMPro;
using System;

public class UIManager : MonoBehaviour
{
    public TMP_Text moneyText;
    public TMP_Text repText;
    public TMP_Text orderText;

    [SerializeField] private TMP_Text dialogueText;

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = false;

    public void SetReputation(int rep)
    {
        if (repText != null)
            repText.text = $"Rep: {rep}";
    }

    public void SetOrderText(string text)
    {
        if (verboseLogging)
        {
            Debug.Log(
                $"[UIManager] SetOrderText -> {text}\n" +
                $"[UIManager] Caller StackTrace:\n{Environment.StackTrace}"
            );
        }

        if (orderText != null)
            orderText.text = text;
    }

    public void ShowDialogue(string text)
    {
        if (dialogueText == null) return;

        dialogueText.text = text;
        dialogueText.gameObject.SetActive(true);
    }
}
