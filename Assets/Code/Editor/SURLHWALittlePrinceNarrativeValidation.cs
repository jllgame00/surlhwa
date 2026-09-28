#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Narrow I13 validation for the one Little Prince remembered-choice arc. It
/// uses production persistence and authored assets, then restores prior local
/// progress exactly.
/// </summary>
public static class SURLHWALittlePrinceNarrativeValidation
{
    private const string NarrativePath =
        "Assets/_SURLHWA/Data/Narrative/LittlePrinceNarrative.asset";
    private const string FirstVisitOrderPath =
        "Assets/_SURLHWA/Data/Orders/LittlePrinceFirstVisitOrder.asset";
    private const string FollowupOrderPath =
        "Assets/_SURLHWA/Data/Orders/LittlePrinceFollowupOrder.asset";
    private const string AvailabilityPath =
        "Assets/_SURLHWA/Data/Ingredients/IngredientAvailability.asset";
    private const string CatalogPath =
        "Assets/_SURLHWA/Data/Recipes/RecipeCatalog.asset";

    [MenuItem("SURLHWA/Validation/Run I13 Little Prince Narrative")]
    public static void Run()
    {
        ProgressSnapshot snapshot = ProgressSnapshot.Capture();
        try
        {
            LittlePrinceNarrativeData narrative =
                AssetDatabase.LoadAssetAtPath<LittlePrinceNarrativeData>(NarrativePath);
            OrderData firstVisitOrder = AssetDatabase.LoadAssetAtPath<OrderData>(FirstVisitOrderPath);
            OrderData followupOrder = AssetDatabase.LoadAssetAtPath<OrderData>(FollowupOrderPath);
            IngredientAvailabilityData availability =
                AssetDatabase.LoadAssetAtPath<IngredientAvailabilityData>(AvailabilityPath);
            RecipeCatalogData catalog = AssetDatabase.LoadAssetAtPath<RecipeCatalogData>(CatalogPath);

            Require(narrative != null && firstVisitOrder != null && followupOrder != null &&
                    availability != null && catalog != null,
                "I13 narrative, order, availability, or catalog content is missing.");

            ValidateContent(narrative, firstVisitOrder, followupOrder, availability, catalog);
            ValidateFreshAndChoiceA(narrative);
            ValidateChoiceB(narrative);
            ValidateLegacyNoneBranch(narrative);

            Debug.Log(
                "[I13 Validation] PASS: stable choice persistence, A/B/legacy branches, " +
                "choice lock, exact Manwol request, and independent visit scheduling are preserved.");
        }
        finally
        {
            snapshot.Restore();
        }
    }

    private static void ValidateContent(
        LittlePrinceNarrativeData narrative,
        OrderData firstVisitOrder,
        OrderData followupOrder,
        IngredientAvailabilityData availability,
        RecipeCatalogData catalog)
    {
        Require(!string.IsNullOrWhiteSpace(narrative.ChoicePrompt) &&
                !string.IsNullOrWhiteSpace(
                    narrative.GetChoiceLabel(LittlePrinceNarrativeChoice.KeepTheLight)) &&
                !string.IsNullOrWhiteSpace(
                    narrative.GetChoiceLabel(LittlePrinceNarrativeChoice.SeekTheStar)),
            "Little Prince choice labels or prompt are missing.");

        DialogueData choiceA = narrative.GetFollowupDialogue(LittlePrinceNarrativeChoice.KeepTheLight);
        DialogueData choiceB = narrative.GetFollowupDialogue(LittlePrinceNarrativeChoice.SeekTheStar);
        DialogueData legacy = narrative.GetFollowupDialogue(LittlePrinceNarrativeChoice.None);
        Require(choiceA != null && choiceB != null && legacy != null &&
                choiceA != choiceB && choiceA != legacy && choiceB != legacy,
            "Each Little Prince narrative branch needs distinct authored dialogue.");

        Require(firstVisitOrder.RequestType == GuestRequestType.ExactCocktail &&
                firstVisitOrder.requestedCocktail != null &&
                firstVisitOrder.miniGameType == MiniGameType.Float &&
                followupOrder.RequestType == GuestRequestType.Recommendation &&
                followupOrder.requestedCocktail == null &&
                Array.IndexOf(followupOrder.DesiredEmotionTags, EmotionTag.Warmth) >= 0 &&
                !firstVisitOrder.RequiresSuccessfulDiscovery && !followupOrder.RequiresSuccessfulDiscovery,
            "Prince first visit teaches exact Manwol; the return asks for Warmth without a hidden target or retry gate.");

        CocktailRecipeData manwol = FindRecipe(catalog, "recipe_manwol");
        Require(manwol.targetCocktail == firstVisitOrder.requestedCocktail,
            "Little Prince orders must target authored Manwol truth.");
        foreach (RecipeIngredientEntry ingredient in manwol.ingredients)
        {
            Require(ingredient != null && ingredient.ingredient != null &&
                    availability.IsIngredientAvailable(
                        ingredient.ingredient,
                        new IngredientAvailabilityContext(2)),
                "Manwol must remain constructible for the Day 2 Little Prince route.");
        }
    }

    private static void ValidateFreshAndChoiceA(LittlePrinceNarrativeData narrative)
    {
        LittlePrinceVisitPersistence.ClearFirstVisitCompleted();
        LittlePrinceVisitPersistence.ClearFollowupVisitCompleted();
        LittlePrinceNarrativePersistence.ClearChoice();

        Require(!LittlePrinceVisitPersistence.IsFirstVisitCompleted() &&
                !LittlePrinceNarrativePersistence.HasSavedChoice() &&
                LittlePrinceNarrativePersistence.LoadChoice() == LittlePrinceNarrativeChoice.None,
            "Fresh progress must contain no first-visit completion or narrative choice.");

        Require(LittlePrinceNarrativePersistence.TryCommitChoice(
                    LittlePrinceNarrativeChoice.KeepTheLight),
            "Choice A could not be committed.");
        Require(LittlePrinceNarrativePersistence.LoadChoice() ==
                    LittlePrinceNarrativeChoice.KeepTheLight &&
                LittlePrinceNarrativePersistence.TryCommitChoice(
                    LittlePrinceNarrativeChoice.KeepTheLight) &&
                !LittlePrinceNarrativePersistence.TryCommitChoice(
                    LittlePrinceNarrativeChoice.SeekTheStar),
            "Choice A must persist, be idempotent, and reject conflicting overwrite.");
        Require(narrative.GetFollowupDialogue(LittlePrinceNarrativePersistence.LoadChoice()) ==
                    narrative.GetFollowupDialogue(LittlePrinceNarrativeChoice.KeepTheLight),
            "Choice A did not resolve the authored remembered branch after reload.");

        Require(!LittlePrinceVisitPersistence.IsFirstVisitCompleted(),
            "Narrative choice must not mark the first visit complete.");
        LittlePrinceVisitPersistence.MarkFirstVisitCompleted();
        Require(LittlePrinceVisitPersistence.IsFirstVisitCompleted() &&
                !LittlePrinceVisitPersistence.IsFollowupVisitCompleted(),
            "First and follow-up visit completion must remain separate scheduling state.");
    }

    private static void ValidateChoiceB(LittlePrinceNarrativeData narrative)
    {
        LittlePrinceNarrativePersistence.ClearChoice();
        Require(LittlePrinceNarrativePersistence.TryCommitChoice(
                    LittlePrinceNarrativeChoice.SeekTheStar) &&
                LittlePrinceNarrativePersistence.LoadChoice() ==
                    LittlePrinceNarrativeChoice.SeekTheStar,
            "Choice B could not be persisted.");
        Require(narrative.GetFollowupDialogue(LittlePrinceNarrativePersistence.LoadChoice()) ==
                    narrative.GetFollowupDialogue(LittlePrinceNarrativeChoice.SeekTheStar),
            "Choice B did not resolve its distinct authored branch.");
    }

    private static void ValidateLegacyNoneBranch(LittlePrinceNarrativeData narrative)
    {
        LittlePrinceVisitPersistence.SetFirstVisitCompleted(true);
        LittlePrinceNarrativePersistence.ClearChoice();

        Require(LittlePrinceVisitPersistence.IsFirstVisitCompleted() &&
                LittlePrinceNarrativePersistence.LoadChoice() == LittlePrinceNarrativeChoice.None &&
                !LittlePrinceNarrativePersistence.HasSavedChoice() &&
                narrative.GetFollowupDialogue(LittlePrinceNarrativeChoice.None) != null,
            "Legacy completed first visit must use the neutral branch without fabricating a choice.");
    }

    private static CocktailRecipeData FindRecipe(RecipeCatalogData catalog, string recipeId)
    {
        foreach (RecipeCatalogEntry entry in catalog.Entries)
        {
            if (entry != null && entry.Recipe != null && entry.Recipe.recipeId == recipeId)
                return entry.Recipe;
        }

        throw new InvalidOperationException("[I13 Validation] Missing recipe '" + recipeId + "'.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("[I13 Validation] " + message);
    }

    private readonly struct ProgressSnapshot
    {
        private readonly bool hadFirstVisit;
        private readonly bool firstVisitCompleted;
        private readonly bool hadFollowupVisit;
        private readonly bool followupVisitCompleted;
        private readonly bool hadChoice;
        private readonly LittlePrinceNarrativeChoice choice;

        private ProgressSnapshot(
            bool hadFirstVisit,
            bool firstVisitCompleted,
            bool hadFollowupVisit,
            bool followupVisitCompleted,
            bool hadChoice,
            LittlePrinceNarrativeChoice choice)
        {
            this.hadFirstVisit = hadFirstVisit;
            this.firstVisitCompleted = firstVisitCompleted;
            this.hadFollowupVisit = hadFollowupVisit;
            this.followupVisitCompleted = followupVisitCompleted;
            this.hadChoice = hadChoice;
            this.choice = choice;
        }

        public static ProgressSnapshot Capture()
        {
            return new ProgressSnapshot(
                LittlePrinceVisitPersistence.HasSavedFirstVisitState(),
                LittlePrinceVisitPersistence.IsFirstVisitCompleted(),
                LittlePrinceVisitPersistence.HasSavedFollowupVisitState(),
                LittlePrinceVisitPersistence.IsFollowupVisitCompleted(),
                LittlePrinceNarrativePersistence.HasSavedChoice(),
                LittlePrinceNarrativePersistence.LoadChoice());
        }

        public void Restore()
        {
            if (hadFirstVisit)
                LittlePrinceVisitPersistence.SetFirstVisitCompleted(firstVisitCompleted);
            else
                LittlePrinceVisitPersistence.ClearFirstVisitCompleted();

            if (hadFollowupVisit)
                LittlePrinceVisitPersistence.SetFollowupVisitCompleted(followupVisitCompleted);
            else
                LittlePrinceVisitPersistence.ClearFollowupVisitCompleted();

            LittlePrinceNarrativePersistence.ClearChoice();
            if (hadChoice && choice != LittlePrinceNarrativeChoice.None)
                LittlePrinceNarrativePersistence.TryCommitChoice(choice);
        }
    }
}

/// <summary>
/// Real Play Mode counterpart to the I13 persistence/content validator. It
/// opens the shipped Bar scene, drives the actual Little Prince dialogue UI,
/// chooses the authored A response through its real Button event, and starts
/// the existing Manwol Float request. This remains a narrow vertical-slice
/// proof, not a reusable dialogue test framework.
/// </summary>
[InitializeOnLoad]
public static class SURLHWALittlePrinceNarrativePlayModeValidation
{
    private const string PendingKey = "SURLHWA_I13_PLAYMODE_PENDING";
    private const string CompleteKey = "SURLHWA_I13_PLAYMODE_COMPLETE";
    private const string SuccessKey = "SURLHWA_I13_PLAYMODE_SUCCESS";
    private const string ScenePath = "Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity";
    private const string LittlePrincePrefabPath =
        "Assets/_SURLHWA/Prefabs/Customers/SpecialGuests/LittlePrinceCustomer.prefab";
    private const string NarrativePath =
        "Assets/_SURLHWA/Data/Narrative/LittlePrinceNarrative.asset";
    private const string FollowupOrderPath =
        "Assets/_SURLHWA/Data/Orders/LittlePrinceFollowupOrder.asset";

    static SURLHWALittlePrinceNarrativePlayModeValidation()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    [MenuItem("SURLHWA/Validation/Run I13 Little Prince Narrative Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[I13 PlayMode Validation] Exit Play Mode before starting this validation.");
            return;
        }

        PlayModeProgressSnapshot.CaptureToSessionState();
        LittlePrinceVisitPersistence.ClearFirstVisitCompleted();
        LittlePrinceVisitPersistence.ClearFollowupVisitCompleted();
        LittlePrinceNarrativePersistence.ClearChoice();

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
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
            // Let scene Start methods wire their existing services before this
            // production UI proof begins.
            EditorApplication.delayCall += RunInPlayMode;
            return;
        }

        if (state != PlayModeStateChange.EnteredEditMode ||
            !SessionState.GetBool(CompleteKey, false))
        {
            return;
        }

        PlayModeProgressSnapshot.RestoreFromSessionState();
        int exitCode = SessionState.GetBool(SuccessKey, false) ? 0 : 1;
        SessionState.EraseBool(PendingKey);
        SessionState.EraseBool(CompleteKey);
        SessionState.EraseBool(SuccessKey);
        EditorApplication.Exit(exitCode);
    }

    private static void RunInPlayMode()
    {
        GameObject littlePrinceInstance = null;
        GameObject followupInstance = null;
        bool success = false;

        try
        {
            InteractionUIController interactionUi =
                UnityEngine.Object.FindFirstObjectByType<InteractionUIController>();
            BartendingFlowCoordinator flow =
                UnityEngine.Object.FindFirstObjectByType<BartendingFlowCoordinator>();
            GameObject littlePrincePrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(LittlePrincePrefabPath);
            LittlePrinceNarrativeData narrative =
                AssetDatabase.LoadAssetAtPath<LittlePrinceNarrativeData>(NarrativePath);
            OrderData followupOrder =
                AssetDatabase.LoadAssetAtPath<OrderData>(FollowupOrderPath);

            Require(interactionUi != null && flow != null && littlePrincePrefab != null &&
                    narrative != null && followupOrder != null,
                "Bar scene UI, flow coordinator, or Little Prince production content is missing.");

            littlePrinceInstance = UnityEngine.Object.Instantiate(littlePrincePrefab);
            CustomerController littlePrince =
                littlePrinceInstance.GetComponent<CustomerController>();
            Require(littlePrince != null, "Little Prince prefab has no CustomerController.");

            littlePrince.ConfigureLittlePrinceFirstVisitNarrative();
            littlePrince.SetUsesPatience(true);
            InvokePrivate(littlePrince, "StartPatienceTimer");
            interactionUi.RegisterCustomer(littlePrince);

            Require(InvokePrivateBool(interactionUi, "TryOpenDedicatedPreRequestDialogue"),
                "Little Prince first-visit dialogue did not open through InteractionUIController.");
            Require(littlePrince.IsPatiencePausedForNarrativeDialogue,
                "Patience was not paused while Little Prince dialogue was open.");

            Button nextButton = GetPrivateField<Button>(interactionUi, "nextButton");
            Button brewButton = GetPrivateField<Button>(interactionUi, "brewButton");
            Require(nextButton != null && brewButton != null,
                "Little Prince choice requires the existing Next and Brew buttons.");

            // Consume the real first-visit content, including J4 teaching,
            // before asserting the unchanged A/B choice and patience contract.
            Require(littlePrince.TryGetDialogueData(out DialogueData firstDialogue) &&
                    firstDialogue.onEnterLines.Length == 6,
                "First visit must retain four narrative and two Manwol teaching lines.");
            for (int advance = 0; advance < firstDialogue.onEnterLines.Length; advance++)
                nextButton.onClick.Invoke();

            Require(nextButton.gameObject.activeSelf && brewButton.gameObject.activeSelf,
                "Both authored Little Prince choice actions were not visible.");
            Require(GetButtonLabel(nextButton) ==
                        narrative.GetChoiceLabel(LittlePrinceNarrativeChoice.KeepTheLight) &&
                    GetButtonLabel(brewButton) ==
                        narrative.GetChoiceLabel(LittlePrinceNarrativeChoice.SeekTheStar),
                "Little Prince choice UI did not show the authored stable A/B labels.");
            Require(littlePrince.IsPatiencePausedForNarrativeDialogue,
                "Patience resumed while the Little Prince choice was open.");

            // Choice A is one real Button event, not a direct persistence call.
            nextButton.onClick.Invoke();
            Require(LittlePrinceNarrativePersistence.LoadChoice() ==
                        LittlePrinceNarrativeChoice.KeepTheLight &&
                    littlePrince.IsPatiencePausedForNarrativeDialogue,
                "Selecting Little Prince choice A did not persist it while preserving the narrative pause.");

            // Choosing is intentionally independent of first-visit completion.
            Require(!LittlePrinceVisitPersistence.IsFirstVisitCompleted(),
                "Choice selection must not complete the Little Prince visit.");

            // The response's Brew button continues through the existing exact
            // Manwol request and Float bartending entry point.
            brewButton.onClick.Invoke();
            Require(!littlePrince.IsPatiencePausedForNarrativeDialogue,
                "Patience did not resume when Little Prince narrative dialogue ended.");
            Require(flow.IsBartendingActive,
                "Little Prince choice response did not start the existing Manwol Float bartending flow.");

            // A later Little Prince instance reads PlayerPrefs through the
            // production CustomerController branch resolver after the save.
            LittlePrinceVisitPersistence.MarkFirstVisitCompleted();
            followupInstance = UnityEngine.Object.Instantiate(littlePrincePrefab);
            CustomerController followup = followupInstance.GetComponent<CustomerController>();
            Require(followup != null, "Little Prince follow-up prefab instance is missing CustomerController.");
            followup.ConfigureLittlePrinceFollowupEncounter(followupOrder);
            Require(followup.TryGetDialogueData(out DialogueData followupDialogue) &&
                    followupDialogue == narrative.GetFollowupDialogue(
                        LittlePrinceNarrativeChoice.KeepTheLight),
                "Later Little Prince encounter did not resolve the persisted A branch.");

            success = true;
            Debug.Log(
                "[I13 PlayMode Validation] PASS: actual Little Prince dialogue UI paused patience, " +
                "displayed A/B, persisted A through Button input, entered Manwol Float, and resolved the later A branch.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            if (followupInstance != null)
                UnityEngine.Object.Destroy(followupInstance);
            if (littlePrinceInstance != null)
                UnityEngine.Object.Destroy(littlePrinceInstance);

            SessionState.SetBool(PendingKey, false);
            SessionState.SetBool(SuccessKey, success);
            SessionState.SetBool(CompleteKey, true);
            EditorApplication.ExitPlaymode();
        }
    }

    private static string GetButtonLabel(Button button)
    {
        TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
        return label != null ? label.text : string.Empty;
    }

    private static T GetPrivateField<T>(object instance, string fieldName) where T : class
    {
        FieldInfo field = instance.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        return field != null ? field.GetValue(instance) as T : null;
    }

    private static void InvokePrivate(object instance, string methodName)
    {
        MethodInfo method = instance.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(method != null, "Missing private method '" + methodName + "'.");
        method.Invoke(instance, null);
    }

    private static bool InvokePrivateBool(object instance, string methodName)
    {
        MethodInfo method = instance.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(method != null, "Missing private method '" + methodName + "'.");
        return method.Invoke(instance, null) is bool result && result;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("[I13 PlayMode Validation] " + message);
    }

    private readonly struct PlayModeProgressSnapshot
    {
        private const string SnapshotPrefix = "SURLHWA_I13_PLAYMODE_SNAPSHOT_";

        public static void CaptureToSessionState()
        {
            SessionState.SetBool(SnapshotPrefix + "HadKnowledge", RecipeProgressPersistence.HasRecipeKnowledge());
            SessionState.SetString(SnapshotPrefix + "Knowledge", RecipeProgressPersistence.LoadRecipeKnowledgeRaw());
            SessionState.SetBool(SnapshotPrefix + "HadLegacy", RecipeProgressPersistence.HasLegacyUnlockedRecipeIds());
            SessionState.SetString(SnapshotPrefix + "Legacy", RecipeProgressPersistence.LoadLegacyUnlockedRecipeIdsRaw());
            SessionState.SetBool(SnapshotPrefix + "HadFirst", LittlePrinceVisitPersistence.HasSavedFirstVisitState());
            SessionState.SetBool(SnapshotPrefix + "First", LittlePrinceVisitPersistence.IsFirstVisitCompleted());
            SessionState.SetBool(SnapshotPrefix + "HadFollowup", LittlePrinceVisitPersistence.HasSavedFollowupVisitState());
            SessionState.SetBool(SnapshotPrefix + "Followup", LittlePrinceVisitPersistence.IsFollowupVisitCompleted());
            SessionState.SetBool(SnapshotPrefix + "HadChoice", LittlePrinceNarrativePersistence.HasSavedChoice());
            SessionState.SetInt(SnapshotPrefix + "Choice", (int)LittlePrinceNarrativePersistence.LoadChoice());
        }

        public static void RestoreFromSessionState()
        {
            if (SessionState.GetBool(SnapshotPrefix + "HadKnowledge", false))
                RecipeProgressPersistence.SaveRecipeKnowledgeRaw(SessionState.GetString(SnapshotPrefix + "Knowledge", string.Empty));
            else
                RecipeProgressPersistence.ClearRecipeKnowledge();
            if (SessionState.GetBool(SnapshotPrefix + "HadLegacy", false))
                RecipeProgressPersistence.SaveUnlockedRecipeIdsRaw(SessionState.GetString(SnapshotPrefix + "Legacy", string.Empty));
            else
                RecipeProgressPersistence.ClearLegacyUnlockedRecipeIds();
            SessionState.EraseBool(SnapshotPrefix + "HadKnowledge");
            SessionState.EraseString(SnapshotPrefix + "Knowledge");
            SessionState.EraseBool(SnapshotPrefix + "HadLegacy");
            SessionState.EraseString(SnapshotPrefix + "Legacy");

            RestoreVisit(
                SessionState.GetBool(SnapshotPrefix + "HadFirst", false),
                SessionState.GetBool(SnapshotPrefix + "First", false),
                LittlePrinceVisitPersistence.SetFirstVisitCompleted,
                LittlePrinceVisitPersistence.ClearFirstVisitCompleted);
            RestoreVisit(
                SessionState.GetBool(SnapshotPrefix + "HadFollowup", false),
                SessionState.GetBool(SnapshotPrefix + "Followup", false),
                LittlePrinceVisitPersistence.SetFollowupVisitCompleted,
                LittlePrinceVisitPersistence.ClearFollowupVisitCompleted);

            LittlePrinceNarrativePersistence.ClearChoice();
            if (SessionState.GetBool(SnapshotPrefix + "HadChoice", false))
            {
                LittlePrinceNarrativePersistence.TryCommitChoice(
                    (LittlePrinceNarrativeChoice)SessionState.GetInt(
                        SnapshotPrefix + "Choice",
                        (int)LittlePrinceNarrativeChoice.None));
            }

            SessionState.EraseBool(SnapshotPrefix + "HadFirst");
            SessionState.EraseBool(SnapshotPrefix + "First");
            SessionState.EraseBool(SnapshotPrefix + "HadFollowup");
            SessionState.EraseBool(SnapshotPrefix + "Followup");
            SessionState.EraseBool(SnapshotPrefix + "HadChoice");
            SessionState.EraseInt(SnapshotPrefix + "Choice");
        }

        private static void RestoreVisit(
            bool hadValue,
            bool value,
            Action<bool> setValue,
            Action clearValue)
        {
            if (hadValue)
                setValue(value);
            else
                clearValue();
        }
    }
}
#endif
