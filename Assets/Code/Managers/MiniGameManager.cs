using UnityEngine;
using System;

public class MiniGameManager : MonoBehaviour
{
    [Header("MiniGame Prefabs")]
    public GameObject buildPrefab;
    public GameObject floatPrefab;
    public GameObject stirPrefab;
    public GameObject shakePrefab;

    [Header("Main Scene References")]
    [SerializeField] private GameObject mainHUD;     // Serve 버튼, ORDER UI
    [SerializeField] private GameObject mainWorld;   // 바 테이블/손님 루트 (없으면 비워도 됨)
    [SerializeField] private Camera mainCamera;      // 메인 카메라

    private GameObject currentMiniGame;

    // 외부에서 "미니게임 중인가?" 체크용
    public bool IsPlaying => currentMiniGame != null;

    public void Play(OrderData order, Action<MiniGameResult> onDone)
    {
        // 혹시 남아있던 미니게임 정리
        if (currentMiniGame != null)
        {
            Destroy(currentMiniGame);
            currentMiniGame = null;
        }

        GameObject prefab = GetPrefab(order.miniGameType);
        if (prefab == null)
        {
            Debug.LogError("[MiniGameManager] MiniGame prefab missing for type: " + order.miniGameType);
            return;
        }

        // 메인 씬 숨기기 (여기서 '전환 느낌' 난다)
        SetMainSceneActive(false);

        // 미니게임 생성
        currentMiniGame = Instantiate(prefab, transform);

        // 루트가 아니라 자식에 붙어 있어도 잡히게
        var game = currentMiniGame.GetComponentInChildren<IMiniGame>(true);
        if (game == null)
        {
            Debug.LogError("[MiniGameManager] MiniGame prefab does not implement IMiniGame");
            SetMainSceneActive(true);
            return;
        }

        // 미니게임 시작
        game.StartGame(order, result =>
        {
            // 미니게임 종료
            Destroy(currentMiniGame);
            currentMiniGame = null;

            // 메인 씬 복귀
            SetMainSceneActive(true);

            onDone?.Invoke(result);
        });
    }

    void SetMainSceneActive(bool active)
    {
        if (mainHUD) mainHUD.SetActive(active);
        if (mainWorld) mainWorld.SetActive(active);
        if (mainCamera) mainCamera.gameObject.SetActive(active);
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
