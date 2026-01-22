using UnityEngine;
using System;

public class MiniGameManager : MonoBehaviour
{
    public GameObject buildPrefab;
    public GameObject floatPrefab;
    public GameObject stirPrefab;
    public GameObject shakePrefab;

    private GameObject currentMiniGame;

    public void Play(OrderData order, Action<MiniGameResult> onDone)
    {
        if (currentMiniGame != null)
        {
            Destroy(currentMiniGame);
            currentMiniGame = null;
        }

        GameObject prefab = GetPrefab(order.miniGameType);
        if (prefab == null)
        {
            Debug.LogError("MiniGame prefab missing for type: " + order.miniGameType);
            return;
        }

        currentMiniGame = Instantiate(prefab, transform);

        var game = currentMiniGame.GetComponent<IMiniGame>();
        if (game == null)
        {
            Debug.LogError("MiniGame prefab does not implement IMiniGame");
            return;
        }

        game.StartGame(order, result =>
        {
            Destroy(currentMiniGame);
            currentMiniGame = null;
            onDone?.Invoke(result);
        });
    }

    GameObject GetPrefab(MiniGameType type)
    {
        return type switch
        {
            MiniGameType.Build => buildPrefab,
            MiniGameType.Float => floatPrefab,
            MiniGameType.Stir => stirPrefab,
            MiniGameType.Shake => shakePrefab,
            _ => null
        };
    }
}
