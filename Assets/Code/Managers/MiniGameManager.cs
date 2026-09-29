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
        BartendingRequestContext request,
        RecipeGuideContext recipeGuideContext,
        Action<string, string> firstRecipeRevealRequested,
        Action<MiniGameResult> onDone)
    {
        Debug.Log(
            $"[MiniGameManager] Play() called\n" +
            $" request.DisplayTitle = {(request != null ? request.DisplayTitle : "NULL")}\n" +
            $" request.Route = {(request != null ? request.Route.ToString() : "NULL")}\n" +
            $" currentMiniGame = {(currentMiniGame != null ? currentMiniGame.name : "NULL")}\n" +
            $" debugPlayMode = {debugPlayMode}"
        );

        if (currentMiniGame != null)
        {
            Destroy(currentMiniGame);
            currentMiniGame = null;
        }

        GameObject prefab = GetPrefabForPlay(request);

        if (prefab == null)
        {
            Debug.LogError("[MiniGameManager] Selected prefab is NULL");
            return;
        }

        SetMainSceneActive(false);

        currentMiniGame = Instantiate(prefab, transform);

        SetupRecipeKnowledgeDiscovery(currentMiniGame, request);

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

        if (game is IRequestDisplayTitleReceiver titleReceiver)
            titleReceiver.SetRequestDisplayTitle(request != null ? request.DisplayTitle : null);

        if (game is IRecipePreparationExpectationReceiver expectationReceiver)
            expectationReceiver.SetExpectedRecipe(request != null ? request.ExpectedRecipe : null);

        if (game is IIngredientAvailabilityContextReceiver availabilityReceiver)
        {
            availabilityReceiver.SetIngredientAvailabilityContext(
                request != null ? request.AvailabilityContext : null);
        }

        game.StartGame(result =>
        {
            if (currentMiniGame != null)
            {
                result.appearance = ServedDrinkAppearance.Capture(currentMiniGame);
                Destroy(currentMiniGame);
                currentMiniGame = null;
            }

            SetMainSceneActive(true);
            onDone?.Invoke(result);
        });
        SurlhwaBartendingPresentation.Apply(currentMiniGame, request.DisplayTitle);
    }

    private GameObject GetPrefabForPlay(BartendingRequestContext request)
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
                if (request == null)
                {
                    Debug.LogError("[MiniGameManager] BartendingRequestContext is NULL in OrderBased mode");
                    return null;
                }

                return GetPrefab(request.Route);
        }
    }

    /// <summary>
    /// Binds player-used liquid evidence only when the Bar already supplied an
    /// authoritative target recipe in the request. Generic requests with no
    /// target remain intentionally ineligible; no recipe is inferred from a
    /// later composition match.
    /// </summary>
    private static void SetupRecipeKnowledgeDiscovery(
        GameObject miniGameRoot,
        BartendingRequestContext request)
    {
        CocktailRecipeData targetRecipe = request != null ? request.ExpectedRecipe : null;
        if (miniGameRoot == null || targetRecipe == null)
            return;

        RecipeUnlockManager knowledgeManager = FindFirstObjectByType<RecipeUnlockManager>();
        IngredientShelfController shelf =
            miniGameRoot.GetComponentInChildren<IngredientShelfController>(true);

        if (knowledgeManager == null || shelf == null)
        {
            Debug.LogWarning(
                "[MiniGameManager] Recipe ingredient discovery is unavailable for the current target request.");
            return;
        }

        RecipeKnowledgeIngredientUseDiscovery.Bind(shelf, targetRecipe, knowledgeManager);
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
