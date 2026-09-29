#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Automated I14 production-UI sign-off. This deliberately enters the shipped
/// BarScene, invokes OrderManager's real completed-bartending boundary, and
/// observes CustomerController, ResultToastController, and
/// RecipeUnlockPopupController through their normal coroutine timing. It is
/// not a formatter test and it creates no serialized content.
/// </summary>
[InitializeOnLoad]
public static class SURLHWAServiceFeedbackPlayModeValidation
{
    private const string PendingKey = "SURLHWA_I14_PLAYMODE_PENDING";
    private const string CompleteKey = "SURLHWA_I14_PLAYMODE_COMPLETE";
    private const string SuccessKey = "SURLHWA_I14_PLAYMODE_SUCCESS";
    private const string SnapshotPrefix = "SURLHWA_I14_PLAYMODE_SNAPSHOT_";

    private const string BarScenePath = "Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity";
    private const string CatalogPath = "Assets/_SURLHWA/Data/Recipes/RecipeCatalog.asset";
    private const string RecommendationOrderPath =
        "Assets/_SURLHWA/Data/Orders/ClarityRecommendationOrder.asset";
    private const string CustomerPrefabPath =
        "Assets/_SURLHWA/Prefabs/Customers/General/Customer_A.prefab";

    static SURLHWAServiceFeedbackPlayModeValidation()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    [MenuItem("SURLHWA/Validation/Run I14 Production UI Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[I14 PlayMode Validation] Exit Play Mode before starting this validation.");
            return;
        }

        CaptureRecipeProgressToSessionState();
        Scene scene = EditorSceneManager.OpenScene(BarScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded || scene.path != BarScenePath)
        {
            RestoreRecipeProgressFromSessionState();
            throw new InvalidOperationException(
                "[I14 PlayMode Validation] Could not open the shipped BarScene.");
        }

        SessionState.SetBool(PendingKey, true);
        SessionState.SetBool(CompleteKey, false);
        SessionState.SetBool(SuccessKey, false);
        EditorApplication.EnterPlaymode();
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode &&
            SessionState.GetBool(PendingKey, false))
        {
            // Let the existing scene Awake/Start lifecycle complete before
            // observing its real presentation path.
            EditorApplication.delayCall += StartPlayModeRunner;
            return;
        }

        if (state != PlayModeStateChange.EnteredEditMode ||
            !SessionState.GetBool(CompleteKey, false))
        {
            return;
        }

        RestoreRecipeProgressFromSessionState();
        int exitCode = SessionState.GetBool(SuccessKey, false) ? 0 : 1;
        SessionState.EraseBool(PendingKey);
        SessionState.EraseBool(CompleteKey);
        SessionState.EraseBool(SuccessKey);
        EditorApplication.Exit(exitCode);
    }

    private static void StartPlayModeRunner()
    {
        if (!EditorApplication.isPlaying || !SessionState.GetBool(PendingKey, false))
            return;

        GameObject host = new("I14 Production UI PlayMode Validation")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        host.AddComponent<SURLHWAServiceFeedbackPlayModeRunner>();
    }

    internal static void Complete(bool success)
    {
        if (!SessionState.GetBool(PendingKey, false))
            return;

        SessionState.SetBool(PendingKey, false);
        SessionState.SetBool(SuccessKey, success);
        SessionState.SetBool(CompleteKey, true);
        EditorApplication.ExitPlaymode();
    }

    private static void CaptureRecipeProgressToSessionState()
    {
        SessionState.SetBool(SnapshotPrefix + "HadKnowledge",
            RecipeProgressPersistence.HasRecipeKnowledge());
        SessionState.SetString(SnapshotPrefix + "Knowledge",
            RecipeProgressPersistence.LoadRecipeKnowledgeRaw());
        SessionState.SetBool(SnapshotPrefix + "HadLegacy",
            RecipeProgressPersistence.HasLegacyUnlockedRecipeIds());
        SessionState.SetString(SnapshotPrefix + "Legacy",
            RecipeProgressPersistence.LoadLegacyUnlockedRecipeIdsRaw());
    }

    private static void RestoreRecipeProgressFromSessionState()
    {
        if (SessionState.GetBool(SnapshotPrefix + "HadKnowledge", false))
            RecipeProgressPersistence.SaveRecipeKnowledgeRaw(
                SessionState.GetString(SnapshotPrefix + "Knowledge", string.Empty));
        else
            RecipeProgressPersistence.ClearRecipeKnowledge();

        if (SessionState.GetBool(SnapshotPrefix + "HadLegacy", false))
            RecipeProgressPersistence.SaveUnlockedRecipeIdsRaw(
                SessionState.GetString(SnapshotPrefix + "Legacy", string.Empty));
        else
            RecipeProgressPersistence.ClearLegacyUnlockedRecipeIds();

        SessionState.EraseBool(SnapshotPrefix + "HadKnowledge");
        SessionState.EraseString(SnapshotPrefix + "Knowledge");
        SessionState.EraseBool(SnapshotPrefix + "HadLegacy");
        SessionState.EraseString(SnapshotPrefix + "Legacy");
    }
}

/// <summary>
/// Runtime-only state machine for the production-UI assertion. It uses actual
/// scene UI and production customer prefabs, but does not alter their assets.
/// </summary>
internal sealed class SURLHWAServiceFeedbackPlayModeRunner : MonoBehaviour
{
    private const float InitialReactionWait = 0.1f;
    private const float ToastWait = 4.15f;
    private const float PopupWait = 2.65f;
    private const float PopupSettleWait = 3.2f;
    private const float ScenarioBSettleWait = 3.0f;
    private const float WatchdogSeconds = 35f;

    private const string CatalogPath = "Assets/_SURLHWA/Data/Recipes/RecipeCatalog.asset";
    private const string RecommendationOrderPath =
        "Assets/_SURLHWA/Data/Orders/ClarityRecommendationOrder.asset";
    private const string CustomerPrefabPath =
        "Assets/_SURLHWA/Prefabs/Customers/General/Customer_A.prefab";

    private OrderManager orderManager;
    private RecipeUnlockManager knowledgeManager;
    private ResultToastController resultToast;
    private RecipeUnlockPopupController unlockPopup;
    private CanvasGroup toastCanvas;
    private TMP_Text toastTitle;
    private TMP_Text toastMessage;
    private RectTransform popupRoot;
    private TMP_Text popupTitle;
    private TMP_Text popupMessage;
    private RecipeCatalogData catalog;
    private OrderData recommendation;
    private CocktailRecipeData dryMartini;
    private CocktailRecipeData incompatibleRecipe;
    private GameObject customerPrefab;
    private GameObject activeCustomerObject;
    private float beganAt;
    private bool finished;
    private bool runInBackgroundBeforeValidation;
    private bool popupWasActive;
    private int popupActivationCount;
    private readonly List<string> scenarioAEvents = new();
    private readonly List<string> scenarioBEvents = new();

    private void Start()
    {
        runInBackgroundBeforeValidation = Application.runInBackground;
        Application.runInBackground = true;
        StartCoroutine(Run());
    }

    private void Update()
    {
        if (!finished && Time.realtimeSinceStartup - beganAt > WatchdogSeconds)
            Fail("Timed out while waiting for production feedback UI.");
    }

    private IEnumerator Run()
    {
        beganAt = Time.realtimeSinceStartup;
        yield return null;

        if (!PrepareProductionScene())
            yield break;

        yield return RunGoodFitGoodExecution();
        if (finished)
            yield break;

        yield return RunPoorFitGoodExecution();
        if (finished)
            yield break;

        Debug.Log(
            "[I14 PlayMode Validation] Scenario A exact UI order: " +
            string.Join(" -> ", scenarioAEvents));
        Debug.Log(
            "[I14 PlayMode Validation] Scenario B exact UI order: " +
            string.Join(" -> ", scenarioBEvents));
        Debug.Log(
            "[I14 PlayMode Validation] PASS: shipped BarScene OrderManager -> " +
            "CustomerController/GuestReactionResolver -> ResultToastController -> " +
            "RecipeUnlockPopupController preserves the I14 hierarchy for both " +
            "Good FIT/Good execution and Poor FIT/Good execution.");
        Finish(true);
    }

    private bool PrepareProductionScene()
    {
        DisableAutomaticCustomers();
        // This focused fixture bypasses entry navigation; expose the service UI
        // for rendering without starting/resetting a separate campaign.
        GameObject.Find("DemoEntryCanvas")?.SetActive(false);

        orderManager = FindFirstObjectByType<OrderManager>();
        knowledgeManager = FindFirstObjectByType<RecipeUnlockManager>();
        resultToast = FindFirstObjectByType<ResultToastController>();
        unlockPopup = FindFirstObjectByType<RecipeUnlockPopupController>();
        catalog = AssetDatabase.LoadAssetAtPath<RecipeCatalogData>(CatalogPath);
        recommendation = AssetDatabase.LoadAssetAtPath<OrderData>(RecommendationOrderPath);
        customerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CustomerPrefabPath);

        if (!Require(orderManager != null && knowledgeManager != null && resultToast != null &&
                     unlockPopup != null && catalog != null && recommendation != null &&
                     customerPrefab != null,
                "Shipped BarScene production controllers or I14 representative content is missing."))
        {
            return false;
        }

        if (!Require(recommendation.RequestType == GuestRequestType.Recommendation,
                "ClarityRecommendationOrder is not an authored Recommendation request."))
        {
            return false;
        }

        CustomerSpawner spawner = FindFirstObjectByType<CustomerSpawner>();
        if (!Require(spawner != null &&
                     GetPrivateField<OrderData>(spawner, "dayOneRecommendationOrder") == recommendation,
                "The exercised Clarity request is not the shipped Day-1 recommendation content."))
        {
            return false;
        }

        dryMartini = FindRecipe(catalog, "recipe_dry_martini");
        incompatibleRecipe = FindRecipe(catalog, "recipe_test_stir");
        if (!Require(dryMartini.targetCocktail != null && incompatibleRecipe.targetCocktail != null,
                "Representative recipes do not reference CocktailData."))
        {
            return false;
        }

        toastCanvas = GetPrivateField<CanvasGroup>(resultToast, "canvasGroup");
        toastTitle = GetPrivateField<TMP_Text>(resultToast, "titleText");
        toastMessage = GetPrivateField<TMP_Text>(resultToast, "messageText");
        popupRoot = GetPrivateField<RectTransform>(unlockPopup, "popupRoot");
        popupTitle = GetPrivateField<TMP_Text>(unlockPopup, "titleText");
        popupMessage = GetPrivateField<TMP_Text>(unlockPopup, "messageText");
        return Require(toastCanvas != null && toastTitle != null && toastMessage != null &&
                       popupRoot != null && popupTitle != null && popupMessage != null,
            "Shipped result toast or recipe popup has an incomplete production UI reference.");
    }

    private IEnumerator RunGoodFitGoodExecution()
    {
        ResetKnowledgeForScenario();
        if (!Require(!knowledgeManager.IsKnown(dryMartini),
                "Dry Martini must begin Unknown for the newly-Known discovery assertion."))
        {
            yield break;
        }

        popupActivationCount = 0;
        popupWasActive = IsPopupActive();
        if (!StartCompletedBartending(dryMartini))
            yield break;

        yield return new WaitForSeconds(InitialReactionWait);
        TrackPopupActivation();
        string reaction = GetActiveReactionText();
        if (!Require(IsProductionFitReaction(
                         activeCustomerObject.GetComponent<CustomerController>(),
                         GuestReactionResolver.GuestResponseTone.GoodFitGoodExecution,
                         reaction),
                "Scenario A did not first display an authored Good FIT/Good execution guest reaction."))
        {
            yield break;
        }

        if (!Require(toastCanvas.alpha <= 0.001f && !IsPopupActive(),
                "Scenario A feedback appeared before the primary guest reaction completed."))
        {
            yield break;
        }

        scenarioAEvents.Add("guest reaction: \"" + reaction + "\"");

        yield return new WaitForSeconds(ToastWait - InitialReactionWait);
        TrackPopupActivation();
        string expectedMessage = "내어준 잔: " + dryMartini.targetCocktail.cocktailName + "\n" +
                                 "조주: 안정적\n" +
                                 "평판: +3 (조주 +2, 요청 +1)";
        if (!Require(toastCanvas.alpha > 0.01f && !IsPopupActive() &&
                     toastTitle.text == "마음에 닿은 잔" &&
                     toastMessage.text == expectedMessage,
                "Scenario A did not present the resolved Dry Martini, Good technique, and granted reward through the production toast."))
        {
            yield break;
        }

        if (!Require(!ContainsConflict(toastTitle.text) && !ContainsConflict(toastMessage.text),
                "Scenario A toast contained a duplicate generic Success/unlock/discovery message."))
        {
            yield break;
        }

        scenarioAEvents.Add("toast: \"" + toastTitle.text + "\" | \"" +
                            EscapeForLog(toastMessage.text) + "\"");

        yield return new WaitForSeconds(PopupWait);
        TrackPopupActivation();
        if (!Require(IsPopupActive() && toastCanvas.alpha <= 0.001f &&
                     popupTitle.text == dryMartini.targetCocktail.cocktailName &&
                     !string.IsNullOrWhiteSpace(popupMessage.text) &&
                     knowledgeManager.IsKnown(dryMartini),
                "Scenario A discovery popup was not shown once after normal service feedback."))
        {
            yield break;
        }

        scenarioAEvents.Add("recipe popup: \"" + popupTitle.text + "\" | \"" +
                            EscapeForLog(popupMessage.text) + "\"");

        yield return new WaitForSeconds(PopupSettleWait);
        TrackPopupActivation();
        if (!Require(popupActivationCount == 1 && !IsPopupActive(),
                "Scenario A recipe discovery presentation was duplicated or did not close normally."))
        {
            yield break;
        }
    }

    private IEnumerator RunPoorFitGoodExecution()
    {
        DestroyActiveCustomer();
        ClearReactionBubbles();
        knowledgeManager.MarkKnown(incompatibleRecipe);
        if (!Require(knowledgeManager.IsKnown(incompatibleRecipe),
                "Scenario B requires its recognized incompatible cocktail to already be Known."))
        {
            yield break;
        }

        popupActivationCount = 0;
        popupWasActive = IsPopupActive();
        if (!Require(!popupWasActive, "Scenario B began while a prior discovery popup was still active."))
            yield break;

        if (!StartCompletedBartending(incompatibleRecipe))
            yield break;

        yield return new WaitForSeconds(InitialReactionWait);
        TrackPopupActivation();
        string reaction = GetActiveReactionText();
        if (!Require(IsProductionFitReaction(
                         activeCustomerObject.GetComponent<CustomerController>(),
                         GuestReactionResolver.GuestResponseTone.PoorFitGoodExecution,
                         reaction),
                "Scenario B did not first display an authored Poor FIT/Good execution guest reaction."))
        {
            yield break;
        }

        if (!Require(toastCanvas.alpha <= 0.001f && !IsPopupActive(),
                "Scenario B feedback appeared before the poor-FIT guest reaction."))
        {
            yield break;
        }

        scenarioBEvents.Add("guest reaction: \"" + reaction + "\"");

        yield return new WaitForSeconds(ToastWait - InitialReactionWait);
        TrackPopupActivation();
        string expectedMessage = "내어준 잔: " + incompatibleRecipe.targetCocktail.cocktailName + "\n" +
                                 "조주: 안정적\n" +
                                 "평판: +2 (요청 보상 없음)";
        if (!Require(toastCanvas.alpha > 0.01f && !IsPopupActive() &&
                     toastTitle.text == "잘 만들었지만 다른 마음의 잔" &&
                     toastMessage.text == expectedMessage,
                "Scenario B did not preserve poor FIT, recognized identity, Good technique, and the withheld request reward in the production toast."))
        {
            yield break;
        }

        if (!Require(!ContainsConflict(toastTitle.text) && !ContainsConflict(toastMessage.text) &&
                     toastMessage.text.IndexOf(dryMartini.targetCocktail.cocktailName,
                         StringComparison.Ordinal) < 0,
                "Scenario B contained a generic Success/unlock conflict or leaked the unrelated Dry Martini answer."))
        {
            yield break;
        }

        scenarioBEvents.Add("toast: \"" + toastTitle.text + "\" | \"" +
                            EscapeForLog(toastMessage.text) + "\"");
        if (!Require(resultToast.transform.Find("ServedPixelDrink/Glass") != null,
                "Phase L actual drink drawing missing from the production service panel."))
            yield break;
        var titleCorners = new Vector3[4];
        var bodyCorners = new Vector3[4];
        toastTitle.rectTransform.GetWorldCorners(titleCorners);
        toastMessage.rectTransform.GetWorldCorners(bodyCorners);
        if (!Require(bodyCorners[1].y < titleCorners[0].y, "Service title and body overlap."))
            yield break;
        SURLHWAReviewCapture.Save("phase-l-served-drink", 1280, 720);

        yield return new WaitForSeconds(ScenarioBSettleWait);
        TrackPopupActivation();
        if (!Require(popupActivationCount == 0 && !IsPopupActive(),
                "Scenario B showed an unrelated recipe discovery popup for an already Known cocktail."))
        {
            yield break;
        }

        scenarioBEvents.Add("recipe popup: none");
    }

    private bool StartCompletedBartending(CocktailRecipeData recipe)
    {
        DestroyActiveCustomer();
        ClearReactionBubbles();
        activeCustomerObject = Instantiate(customerPrefab);
        CustomerController customer = activeCustomerObject.GetComponent<CustomerController>();
        if (!Require(customer != null, "Production Customer_A prefab has no CustomerController."))
            return false;

        MiniGameResult result = BuildGoodResult(recipe);
        SetPrivateField(orderManager, "_currentCustomer", customer);
        SetPrivateField(orderManager, "currentOrder", recommendation);
        SetPrivateField(
            orderManager,
            "currentGuestRequest",
            GuestRequestContext.FromOrder(recommendation));
        InvokePrivate(orderManager, "OnMiniGameDone", new object[] { result });
        return true;
    }

    private static MiniGameResult BuildGoodResult(CocktailRecipeData recipe)
    {
        BartendingPreparationTrace trace = BuildExpectedTrace(recipe);
        RecipePreparationEvaluationResult preparation = RecipePreparationEvaluator.Evaluate(recipe, trace);
        if (preparation.Status == RecipePreparationEvaluationStatus.NotEvaluated)
        {
            // TestStir has no authored I9B preparation sequence. Its real
            // production result therefore remains explicitly NotEvaluated;
            // the technique result is still its truthful Good completion.
            preparation = new RecipePreparationEvaluationResult(
                RecipePreparationEvaluationStatus.NotEvaluated,
                0,
                0,
                "Recipe has no expected preparation steps.");
        }

        // Presentation fixture follows the same composition used by this existing
        // feedback test; runtime capture remains owned by MiniGameManager.
        var serving = new BartendingContainerState(BartendingContainerType.ServingGlass);
        float totalRatio = 0f;
        foreach (var entry in recipe.ingredients)
            if (entry?.ingredient != null) totalRatio += entry.ratio;
        foreach (var entry in recipe.ingredients)
            if (entry?.ingredient != null) serving.AddLiquid(entry.ingredient, 120f * entry.ratio / totalRatio);
        return new MiniGameResult
        {
            appearance = new ServedDrinkAppearance(serving, null, "RocksGlass"),
            success = true,
            repDelta = 2,
            grade = "Good",
            brewedComposition = BuildExactComposition(recipe),
            expectedRecipe = recipe,
            preparationTrace = trace,
            preparationEvaluation = preparation
        };
    }

    private static BartendingPreparationTrace BuildExpectedTrace(CocktailRecipeData recipe)
    {
        BartendingPreparationTrace trace = new();
        if (recipe == null || recipe.expectedPreparation == null)
            return trace;

        foreach (RecipePreparationStep step in recipe.expectedPreparation)
        {
            if (step == null || step.optional)
                continue;

            int repetitions = Mathf.Max(1, step.repeatCount);
            for (int index = 0; index < repetitions; index++)
            {
                trace.Record(
                    step.action,
                    step.ingredient,
                    0f,
                    ToContainer(step.source),
                    ToContainer(step.target));
            }
        }

        return trace;
    }

    private static BartendingContainerType? ToContainer(RecipePreparationContainer container)
    {
        return container == RecipePreparationContainer.Any
            ? null
            : (BartendingContainerType)((int)container - 1);
    }

    private static BrewComposition BuildExactComposition(CocktailRecipeData recipe)
    {
        List<BrewIngredientEntry> ingredients = new();
        foreach (RecipeIngredientEntry entry in recipe.ingredients)
        {
            if (entry != null && entry.ingredient != null)
            {
                ingredients.Add(new BrewIngredientEntry
                {
                    ingredientId = entry.ingredient.ingredientId,
                    amount = entry.ratio
                });
            }
        }

        return new BrewComposition
        {
            method = recipe.method,
            ingredients = ingredients.ToArray()
        };
    }

    private void ResetKnowledgeForScenario()
    {
        RecipeProgressPersistence.ClearRecipeKnowledge();
        RecipeProgressPersistence.ClearLegacyUnlockedRecipeIds();
        knowledgeManager.ClearSavedUnlocks();
    }

    private void DisableAutomaticCustomers()
    {
        foreach (CustomerSpawner spawner in FindObjectsByType<CustomerSpawner>(
                     FindObjectsInactive.Exclude,
                     FindObjectsSortMode.None))
        {
            spawner.enabled = false;
        }

        foreach (CustomerController customer in FindObjectsByType<CustomerController>(
                     FindObjectsInactive.Exclude,
                     FindObjectsSortMode.None))
        {
            Destroy(customer.gameObject);
        }
    }

    private static bool IsProductionFitReaction(
        CustomerController customer,
        GuestReactionResolver.GuestResponseTone tone,
        string displayedText)
    {
        GuestReactionDatabase database = GetPrivateField<GuestReactionDatabase>(customer, "reactionDatabase");
        if (database == null || database.guestFitLines == null || string.IsNullOrWhiteSpace(displayedText))
            return false;

        foreach (GuestFitReactionLine line in database.guestFitLines)
        {
            if (line != null && line.tone == tone && line.text == displayedText)
                return true;
        }

        return false;
    }

    private static string GetActiveReactionText()
    {
        DialogueBubble[] bubbles = FindObjectsByType<DialogueBubble>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        foreach (DialogueBubble bubble in bubbles)
        {
            if (bubble != null && bubble.text != null && !string.IsNullOrWhiteSpace(bubble.text.text))
                return bubble.text.text;
        }

        return string.Empty;
    }

    private void ClearReactionBubbles()
    {
        foreach (DialogueBubble bubble in FindObjectsByType<DialogueBubble>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (bubble != null)
                Destroy(bubble.gameObject);
        }
    }

    private void DestroyActiveCustomer()
    {
        if (activeCustomerObject != null)
            Destroy(activeCustomerObject);

        activeCustomerObject = null;
    }

    private bool IsPopupActive()
    {
        return popupRoot != null && popupRoot.gameObject.activeInHierarchy;
    }

    private void TrackPopupActivation()
    {
        bool active = IsPopupActive();
        if (active && !popupWasActive)
            popupActivationCount++;

        popupWasActive = active;
    }

    private static bool ContainsConflict(string text)
    {
        return !string.IsNullOrWhiteSpace(text) &&
               (text.IndexOf("success", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("unlock", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("discover", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static string EscapeForLog(string value)
    {
        return (value ?? string.Empty).Replace("\n", " | ");
    }

    private static CocktailRecipeData FindRecipe(RecipeCatalogData recipeCatalog, string recipeId)
    {
        foreach (RecipeCatalogEntry entry in recipeCatalog.Entries)
        {
            if (entry != null && entry.Recipe != null && entry.Recipe.recipeId == recipeId)
                return entry.Recipe;
        }

        throw new InvalidOperationException(
            "[I14 PlayMode Validation] Missing recipe '" + recipeId + "'.");
    }

    private bool Require(bool condition, string message)
    {
        if (condition)
            return true;

        Fail(message);
        return false;
    }

    private void Fail(string message)
    {
        if (finished)
            return;

        Debug.LogError("[I14 PlayMode Validation] FAIL: " + message);
        Finish(false);
    }

    private void Finish(bool success)
    {
        if (finished)
            return;

        finished = true;
        Application.runInBackground = runInBackgroundBeforeValidation;
        DestroyActiveCustomer();
        SURLHWAServiceFeedbackPlayModeValidation.Complete(success);
    }

    private static T GetPrivateField<T>(object instance, string fieldName) where T : class
    {
        FieldInfo field = instance != null
            ? instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            : null;
        return field != null ? field.GetValue(instance) as T : null;
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance != null
            ? instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            : null;
        if (field == null)
            throw new InvalidOperationException(
                "[I14 PlayMode Validation] Missing private field '" + fieldName + "'.");

        field.SetValue(instance, value);
    }

    private static void InvokePrivate(object instance, string methodName, object[] parameters)
    {
        MethodInfo method = instance != null
            ? instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            : null;
        if (method == null)
            throw new InvalidOperationException(
                "[I14 PlayMode Validation] Missing private method '" + methodName + "'.");

        method.Invoke(instance, parameters);
    }
}
#endif
