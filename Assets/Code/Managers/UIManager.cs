using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public TMP_Text moneyText;
    public TMP_Text repText;
    public TMP_Text orderText;

    public void SetReputation(int rep)
    {
        if (repText != null) repText.text = $"Rep: {rep}";
    }


    public void SetOrderText(string text)
    {
        if (orderText != null) orderText.text = text;
    }
}
