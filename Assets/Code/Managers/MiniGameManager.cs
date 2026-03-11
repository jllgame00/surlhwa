using UnityEngine;
using System;

public class MiniGameManager : MonoBehaviour
{
    public enum DebugPlayMode
    {
        OrderBased,
        RandomStirShake,
        ForceStir,
        ForceShake
    }

    [Header("MiniGame Prefabs")]
    public GameObject buildPrefab;
    public GameObject floatPrefab;
    public GameObject stirPrefab;
    public GameObject shakePrefab;

    [Header("Debug")]
    [SerializeField] private DebugPlayMode debugPlayMode = DebugPlayMode.ForceShake;

    [Header("Main Scene References")]
    [SerializeField] private GameObject mainHUD;     // Serve 버튼, ORDER UI
    [SerializeField] private GameObject mainWorld;   // 바 테이블/손님 루트
    [SerializeField] private Camera mainCamera;      // 메인 카메라

    private GameObject currentMiniGame;
    private UIManager ui;

    public bool IsPlaying => currentMiniGame != null;

    private void Awake()
    {
        ui = FindFirstObjectByType<UIManager>();

        Debug.Log(
            $"[MiniGameManager] Awake\n" +
            $" debugPlayMode = {debugPlayMode}\n" +
            $" stirPrefab = {(stirPrefab != null ? stirPrefab.name : "NULL")}\n" +
            $" shakePrefab = {(shakePrefab != null ? shakePrefab.name : "NULL")}\n" +
            $" buildPrefab = {(buildPrefab != null ? buildPrefab.name : "NULL")}\n" +
            $" floatPrefab = {(floatPrefab != null ? floatPrefab.name : "NULL")}\n" +
            $" ui = {(ui != null ? ui.name : "NULL")}"
        );
    }

    public void Play(OrderData order, Action<MiniGameResult> onDone)
    {
        Debug.Log(
            $"[MiniGameManager] Play() called\n" +
            $" order = {(order != null ? order.orderName : "NULL")}\n" +
            $" order.miniGameType = {(order != null ? order.miniGameType.ToString() : "NULL")}\n" +
            $" currentMiniGame = {(currentMiniGame != null ? currentMiniGame.name : "NULL")}\n" +
            $" debugPlayMode = {debugPlayMode}"
        );

        if (currentMiniGame != null)
        {
            Debug.Log($"[MiniGameManager] Existing currentMiniGame found -> destroying {currentMiniGame.name}");
            Destroy(currentMiniGame);
            currentMiniGame = null;
        }

        GameObject prefab = GetPrefabForPlay(order);

        Debug.Log($"[MiniGameManager] Selected prefab = {(prefab != null ? prefab.name : "NULL")}");

        if (prefab == null)
        {
            Debug.LogError("[MiniGameManager] Selected prefab is NULL");
            return;
        }

        Debug.Log("[MiniGameManager] Before UpdateDebugOrderLabel()");
        UpdateDebugOrderLabel(prefab, order);
        Debug.Log("[MiniGameManager] After UpdateDebugOrderLabel()");

        Debug.Log("[MiniGameManager] Before SetMainSceneActive(false)");
        SetMainSceneActive(false);
        Debug.Log("[MiniGameManager] After SetMainSceneActive(false)");

        currentMiniGame = Instantiate(prefab, transform);
        Debug.Log($"[MiniGameManager] Spawned MiniGame object = {currentMiniGame.name}");

        var game = currentMiniGame.GetComponentInChildren<IMiniGame>(true);
        Debug.Log($"[MiniGameManager] IMiniGame found = {(game != null ? "YES" : "NO")}");

        if (game == null)
        {
            Debug.LogError($"[MiniGameManager] MiniGame prefab does not implement IMiniGame -> {prefab.name}");

            Destroy(currentMiniGame);
            currentMiniGame = null;
            SetMainSceneActive(true);
            return;
        }

        Debug.Log("[MiniGameManager] Before game.StartGame()");
        game.StartGame(order, result =>
        {
            Debug.Log(
                $"[MiniGameManager] game.StartGame callback invoked\n" +
                $" result.success = {result.success}\n" +
                $" result.repDelta = {result.repDelta}\n" +
                $" result.grade = {result.grade}"
            );

            if (currentMiniGame != null)
            {
                Debug.Log($"[MiniGameManager] Destroying currentMiniGame in callback -> {currentMiniGame.name}");
                Destroy(currentMiniGame);
                currentMiniGame = null;
            }

            Debug.Log("[MiniGameManager] Before SetMainSceneActive(true)");
            SetMainSceneActive(true);
            Debug.Log("[MiniGameManager] After SetMainSceneActive(true)");

            onDone?.Invoke(result);
        });
        Debug.Log("[MiniGameManager] After game.StartGame()");
    }

    private GameObject GetPrefabForPlay(OrderData order)
    {
        Debug.Log("[MiniGameManager] GetPrefabForPlay() entered");

        switch (debugPlayMode)
        {
            case DebugPlayMode.ForceStir:
                Debug.Log($"[MiniGameManager] Debug mode -> ForceStir / prefab={(stirPrefab != null ? stirPrefab.name : "NULL")}");
                return stirPrefab;

            case DebugPlayMode.ForceShake:
                Debug.Log($"[MiniGameManager] Debug mode -> ForceShake / prefab={(shakePrefab != null ? shakePrefab.name : "NULL")}");
                return shakePrefab;

            case DebugPlayMode.RandomStirShake:
                {
                    bool pickStir = UnityEngine.Random.value < 0.5f;
                    GameObject picked = pickStir ? stirPrefab : shakePrefab;
                    string pickedName = pickStir ? "Stir" : "Shake";

                    Debug.Log(
                        $"[MiniGameManager] Debug mode -> RandomStirShake\n" +
                        $" picked = {pickedName}\n" +
                        $" prefab = {(picked != null ? picked.name : "NULL")}"
                    );
                    return picked;
                }

            case DebugPlayMode.OrderBased:
            default:
                if (order == null)
                {
                    Debug.LogError("[MiniGameManager] Order is NULL in OrderBased mode");
                    return null;
                }

                GameObject prefab = GetPrefab(order.miniGameType);

                Debug.Log(
                    $"[MiniGameManager] Debug mode -> OrderBased\n" +
                    $" order.miniGameType = {order.miniGameType}\n" +
                    $" prefab = {(prefab != null ? prefab.name : "NULL")}"
                );
                return prefab;
        }
    }

    private void UpdateDebugOrderLabel(GameObject selectedPrefab, OrderData order)
    {
        if (ui == null)
            ui = FindFirstObjectByType<UIManager>();

        Debug.Log($"[MiniGameManager] UpdateDebugOrderLabel() entered / ui={(ui != null ? ui.name : "NULL")}");

        if (ui == null)
        {
            Debug.LogWarning("[MiniGameManager] UIManager not found");
            return;
        }

        string label = order != null ? order.orderName : "-";

        if (debugPlayMode == DebugPlayMode.RandomStirShake ||
            debugPlayMode == DebugPlayMode.ForceStir ||
            debugPlayMode == DebugPlayMode.ForceShake)
        {
            if (selectedPrefab == stirPrefab)
                label = "TestStir";
            else if (selectedPrefab == shakePrefab)
                label = "TestShake";
        }

        Debug.Log(
            $"[MiniGameManager] UpdateDebugOrderLabel()\n" +
            $" selectedPrefab = {(selectedPrefab != null ? selectedPrefab.name : "NULL")}\n" +
            $" stirPrefab = {(stirPrefab != null ? stirPrefab.name : "NULL")}\n" +
            $" shakePrefab = {(shakePrefab != null ? shakePrefab.name : "NULL")}\n" +
            $" final label = {label}"
        );

        ui.SetOrderText($"ORDER: {label}");
    }

    private void SetMainSceneActive(bool active)
    {
        Debug.Log(
            $"[MiniGameManager] SetMainSceneActive({active})\n" +
            $" mainHUD = {(mainHUD != null ? mainHUD.name : "NULL")}\n" +
            $" mainWorld = {(mainWorld != null ? mainWorld.name : "NULL")}\n" +
            $" mainCamera = {(mainCamera != null ? mainCamera.name : "NULL")}"
        );

        if (mainHUD != null) mainHUD.SetActive(active);
        if (mainWorld != null) mainWorld.SetActive(active);
        if (mainCamera != null) mainCamera.gameObject.SetActive(active);
    }

    private GameObject GetPrefab(MiniGameType type)
    {
        Debug.Log($"[MiniGameManager] GetPrefab({type}) called");

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