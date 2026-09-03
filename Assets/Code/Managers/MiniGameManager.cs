using UnityEngine;
using System;

public class MiniGameManager : MonoBehaviour
{
    public enum DebugPlayMode
    {
        OrderBased,
        RandomStirShake,
        ForceStir,
        ForceShake,
        ForceBuild,
        ForceFloat
    }

    [Header("MiniGame Prefabs")]
    public GameObject buildPrefab;
    public GameObject floatPrefab;
    public GameObject stirPrefab;
    public GameObject shakePrefab;

    [Header("Debug")]
    [SerializeField] private DebugPlayMode debugPlayMode = DebugPlayMode.OrderBased;

    [Header("Main Scene References")]
    [SerializeField] private GameObject mainHUD;
    [SerializeField] private GameObject mainWorld;
    [SerializeField] private Camera mainCamera;

    private GameObject currentMiniGame;

    public bool IsPlaying => currentMiniGame != null;

    private void SetupRecipeGuide(
        GameObject miniGameRoot,
        RecipeGuideContext recipeGuideContext,
        Action<string, string> firstRecipeRevealRequested)
    {
        if (miniGameRoot == null)
            return;

        RecipeGuidePanelController guide =
            miniGameRoot.GetComponentInChildren<RecipeGuidePanelController>(true);

        if (guide == null)
        {
            Debug.LogWarning("[MiniGameManager] RecipeGuidePanelController not found in minigame prefab");
            return;
        }

        guide.Initialize(recipeGuideContext, firstRecipeRevealRequested);

        Debug.Log("[MiniGameManager] Recipe guide initialized");
    }

    private void Awake()
    {
        Debug.Log(
            $"[MiniGameManager] Awake\n" +
            $" debugPlayMode = {debugPlayMode}\n" +
            $" stirPrefab = {(stirPrefab != null ? stirPrefab.name : "NULL")}\n" +
            $" shakePrefab = {(shakePrefab != null ? shakePrefab.name : "NULL")}\n" +
            $" buildPrefab = {(buildPrefab != null ? buildPrefab.name : "NULL")}\n" +
            $" floatPrefab = {(floatPrefab != null ? floatPrefab.name : "NULL")}"
        );
    }

    public void Play(
        OrderData order,
        BrewSessionData brewSession,
        RecipeGuideContext recipeGuideContext,
        Action<string, string> firstRecipeRevealRequested,
        Action<MiniGameResult> onDone)
    {
        Debug.Log(
            $"[MiniGameManager] Play() called\n" +
            $" order = {(order != null ? order.orderName : "NULL")}\n" +
            $" order.miniGameType = {(order != null ? order.miniGameType.ToString() : "NULL")}\n" +
            $" brewSession = {(brewSession != null ? brewSession.method.ToString() : "NULL")}\n" +
            $" currentMiniGame = {(currentMiniGame != null ? currentMiniGame.name : "NULL")}\n" +
            $" debugPlayMode = {debugPlayMode}"
        );

        if (currentMiniGame != null)
        {
            Destroy(currentMiniGame);
            currentMiniGame = null;
        }

        GameObject prefab = GetPrefabForPlay(order);

        if (prefab == null)
        {
            Debug.LogError("[MiniGameManager] Selected prefab is NULL");
            return;
        }

        SetMainSceneActive(false);

        currentMiniGame = Instantiate(prefab, transform);

        // 레시피 가이드 패널에 현재 조주 방식 전달
        SetupRecipeGuide(currentMiniGame, recipeGuideContext, firstRecipeRevealRequested);

        var game = currentMiniGame.GetComponentInChildren<IMiniGame>(true);
        if (game == null)
        {
            Debug.LogError($"[MiniGameManager] MiniGame prefab does not implement IMiniGame -> {prefab.name}");
            Destroy(currentMiniGame);
            currentMiniGame = null;
            SetMainSceneActive(true);
            return;
        }

        game.StartGame(order, brewSession, result =>
        {
            if (currentMiniGame != null)
            {
                Destroy(currentMiniGame);
                currentMiniGame = null;
            }

            SetMainSceneActive(true);
            onDone?.Invoke(result);
        });
    }

    private GameObject GetPrefabForPlay(OrderData order)
    {
        switch (debugPlayMode)
        {
            case DebugPlayMode.ForceFloat:
                return floatPrefab;

            case DebugPlayMode.ForceStir:
                return stirPrefab;

            case DebugPlayMode.ForceShake:
                return shakePrefab;

            case DebugPlayMode.ForceBuild:
                return buildPrefab;

            case DebugPlayMode.RandomStirShake:
                {
                    bool pickStir = UnityEngine.Random.value < 0.5f;
                    return pickStir ? stirPrefab : shakePrefab;
                }

            case DebugPlayMode.OrderBased:
            default:
                if (order == null)
                {
                    Debug.LogError("[MiniGameManager] Order is NULL in OrderBased mode");
                    return null;
                }

                return GetPrefab(order.miniGameType);
        }
    }

    private void SetMainSceneActive(bool active)
    {
        if (mainHUD != null) mainHUD.SetActive(active);
        if (mainWorld != null) mainWorld.SetActive(active);
        if (mainCamera != null) mainCamera.gameObject.SetActive(active);
    }

    private GameObject GetPrefab(MiniGameType type)
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
