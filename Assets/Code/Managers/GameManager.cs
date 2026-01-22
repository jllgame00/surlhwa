using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int reputation { get; private set; }

    private UIManager ui;

    void Awake()
    {
        ui = FindFirstObjectByType<UIManager>();
        SyncUI();
    }

    public void OnOrderResolved(bool success)
    {
        if (!success) return;

        reputation += 1;

        if (ui != null)
            ui.SetReputation(reputation);
    }

    public void AddReputation(int amount)
    {
        reputation += amount;
        if (ui != null) ui.SetReputation(reputation);
    }


    void SyncUI()
    {
        if (ui != null)
            ui.SetReputation(reputation);
    }
}
