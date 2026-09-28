using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;
using TMPro;
using UnityEngine;

public class IngredientShelfController : MonoBehaviour
{
    [Header("Ingredient Data")]
    [SerializeField] private IngredientData[] allIngredients;
    [SerializeField] private IngredientAvailabilityData availabilityData;

    [Header("UI")]
    [SerializeField] private Transform ingredientListRoot;
    [SerializeField] private BrewIngredientOptionView ingredientOptionPrefab;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text summaryText;
    [Header("Physical tools — HUMAN_TUNING_LATER")]
    [SerializeField, Min(1f)] private float jiggerCapacityMl = 60f;
    [SerializeField, Min(1f)] private float jiggerTransferMlPerSecond = 90f;
    [SerializeField, Min(1f)] private float containerTransferMlPerSecond = 120f;
    [SerializeField, Min(.01f)] private float doseStreamSeconds = .12f;

    private MiniGameType currentMethod;
    private IngredientData selectedIngredient;

    private readonly List<BrewIngredientOptionView> optionViews = new List<BrewIngredientOptionView>();

    // The shelf owns this preparation session only for the active mini-game.
    // I3 keeps container semantics in BartendingPreparationState; the shelf is
    // an interaction/UI adapter around its selected source and active target.
    private BartendingPreparationState preparationState;
    private BartendingContainerState targetContainer;
    private BartendingPreparationTrace preparationTrace;
    private IngredientData activePourIngredient;
    private BartendingContainerType activePourTarget;
    private RecipePreparationAction activePourAction;
    private float activePourTransferredMl;
    private IngredientAvailabilityContext availabilityContext;
    private BartendingPhysicalPresentation physicalPresentation;
    public BartendingPhysicalPresentation PhysicalPresentation => physicalPresentation;
    public bool PhysicalTransferActive { get; private set; }
    public bool Measuring { get; private set; }
    public BartendingContainerState Jigger => preparationState?.GetContainer(BartendingContainerType.Jigger);
    public bool HasPendingMeasurement => Jigger != null && Jigger.CurrentLiquidVolumeMl > 0f;
    public bool ToolsReady => !PhysicalTransferActive && !HasPendingMeasurement &&
                              (physicalPresentation == null || physicalPresentation.IsResting);

    public IngredientData SelectedIngredient => selectedIngredient;
    public IngredientAvailabilityContext AvailabilityContext => availabilityContext;
    public IReadOnlyList<IngredientData> ConfiguredIngredients => allIngredients ?? System.Array.Empty<IngredientData>();
    public IngredientAvailabilityData AvailabilityData => availabilityData;
    public int VisibleIngredientCount => optionViews.Count;
    public BartendingContainerState TargetContainer => targetContainer;
    public BartendingPreparationState PreparationState => preparationState;
    public BartendingPreparationTrace PreparationTrace => preparationTrace;
    public BartendingContainerState ServingGlass => preparationState != null
        ? preparationState.GetContainer(BartendingContainerType.ServingGlass)
        : null;

    /// <summary>
    /// Raised once per ingredient after an accepted physical liquid action in
    /// this Bartending session. It observes player action only; it does not
    /// know a recipe, store knowledge, or affect preparation truth.
    /// </summary>
    public event Action<IngredientData> IngredientUsed;

    private readonly HashSet<IngredientData> ingredientsUsedThisSession = new();

    public void Initialize(
        MiniGameType method,
        BartendingContainerType targetContainerType = BartendingContainerType.ServingGlass,
        IngredientAvailabilityContext context = null)
    {
        currentMethod = method;
        selectedIngredient = null;
        // Every production Bartending route receives this from the Bar-side
        // request context. Day 1 is only a deterministic direct-use fallback.
        availabilityContext = context ?? new IngredientAvailabilityContext(1);
        preparationState = new BartendingPreparationState(targetContainerType, jiggerCapacityMl);
        targetContainer = preparationState.ActiveTarget;
        preparationTrace = new BartendingPreparationTrace();
        activePourIngredient = null;
        activePourTransferredMl = 0f;
        ingredientsUsedThisSession.Clear();

        Transform methodRoot = transform;
        while (methodRoot.parent != null && methodRoot.Find("UI") == null)
            methodRoot = methodRoot.parent;
        if (Application.isPlaying && methodRoot.Find("UI") != null)
        {
            physicalPresentation = methodRoot.GetComponent<BartendingPhysicalPresentation>();
            if (physicalPresentation == null)
                physicalPresentation = methodRoot.gameObject.AddComponent<BartendingPhysicalPresentation>();
            physicalPresentation.Initialize(methodRoot, preparationState);
        }

        // Do not seed from the recipe/default session.
        // The shelf represents what the player has actually poured.
        RebuildIngredientList();
        RefreshTexts();

        gameObject.SetActive(true);
    }

    /// <summary>
    /// Refreshes the presentation consumer from the current availability
    /// context. It never changes the static policy or any saved state.
    /// </summary>
    public void SetAvailabilityContext(IngredientAvailabilityContext context)
    {
        availabilityContext = context ?? new IngredientAvailabilityContext(1);
        RefreshAvailability();
    }

    public void RefreshAvailability()
    {
        if (selectedIngredient != null && !IsIngredientAvailable(selectedIngredient))
        {
            EndSelectedPour();
            selectedIngredient = null;
        }

        RebuildIngredientList();
        RefreshTexts();
    }

    public bool IsIngredientVisible(IngredientData ingredient)
    {
        return ingredient != null && optionViews.Any(option =>
            option != null && option.Ingredient == ingredient);
    }

    public bool HasAnySelectedIngredient()
    {
        return targetContainer != null && targetContainer.CurrentLiquidVolumeMl > 0f;
    }

    /// <summary>
    /// Result submission is deliberately distinct from the active preparation
    /// target: I3 makes the final Serving Glass the actual cocktail source.
    /// </summary>
    public bool HasServingGlassContents()
    {
        return ServingGlass != null && ServingGlass.CurrentLiquidVolumeMl > 0f;
    }

    /// <summary>
    /// Returns a result-bound snapshot for the existing recipe matcher. The
    /// target container remains authoritative until the minigame submits it.
    /// </summary>
    public BrewComposition BuildComposition()
    {
        return targetContainer != null
            ? targetContainer.ToLegacyBrewComposition(currentMethod)
            : new BrewComposition
            {
                method = currentMethod,
                ingredients = System.Array.Empty<BrewIngredientEntry>()
            };
    }

    public BrewComposition BuildServingGlassComposition()
    {
        return ServingGlass != null
            ? ServingGlass.ToLegacyBrewComposition(currentMethod)
            : new BrewComposition
            {
                method = currentMethod,
                ingredients = System.Array.Empty<BrewIngredientEntry>()
            };
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    private void RebuildIngredientList()
    {
        if (ingredientListRoot == null || ingredientOptionPrefab == null)
        {
            Debug.LogWarning("[IngredientShelfController] List root or option prefab is missing.");
            return;
        }
        
        optionViews.Clear();

        for (int i = ingredientListRoot.childCount - 1; i >= 0; i--)
        {
            GameObject optionInstance = ingredientListRoot.GetChild(i).gameObject;

            if (Application.isPlaying)
                Destroy(optionInstance);
            else
                DestroyImmediate(optionInstance);
        }

        if (allIngredients == null)
            return;

        IEnumerable<IngredientData> availableIngredients = availabilityData != null
            ? availabilityData.GetAvailableIngredients(allIngredients, availabilityContext)
            : allIngredients.Where(ingredient => ingredient != null);

        IEnumerable<IngredientData> sortedIngredients = availableIngredients
            .Where(ingredient => ingredient != null)
            .OrderBy(ingredient => ingredient.category)
            .ThenBy(ingredient => ingredient.sortOrder)
            .ThenBy(ingredient => ingredient.displayName);

        foreach (IngredientData ingredient in sortedIngredients)
        {
            float amountMl = targetContainer != null
                ? targetContainer.GetLiquidAmountMl(ingredient)
                : 0f;

            BrewIngredientOptionView option = Instantiate(ingredientOptionPrefab, ingredientListRoot);

            option.Bind(
                ingredient,
                amountMl,
                OnIngredientSelected
            );

            optionViews.Add(option);
            option.SetSelectedVisual(ingredient == selectedIngredient);
        }
    }

    private void RefreshTexts()
    {
        if (titleText != null)
            titleText.text = $"재료 · {currentMethod}";

        if (summaryText == null)
            return;

        string iceText = targetContainer != null && targetContainer.HasIce ? " · 얼음" : string.Empty;

        if (!HasAnySelectedIngredient())
        {
            summaryText.text = targetContainer != null
                ? $"{targetContainer.ContainerType} · 비어 있음{iceText}"
                : "용기 없음";
            return;
        }

        int liquidCount = targetContainer.LiquidEntries.Count(entry =>
            entry != null && entry.Ingredient != null && entry.AmountMl > 0f);

        // Individual ingredient rows already present their exact amounts. Keep
        // this fixed-height summary bounded so a multi-liquid drink cannot
        // drive TMP's wrapped-ellipsis layout into recursive line breaking.
        summaryText.text =
            $"{targetContainer.ContainerType} {targetContainer.CurrentLiquidVolumeMl:0.0} ml / " +
            $"재료 {liquidCount}종{iceText}";
    }

    private void OnIngredientSelected(IngredientData ingredient)
    {
        if (PhysicalTransferActive || HasPendingMeasurement) return;
        if (ingredient == null || !IsIngredientAvailable(ingredient))
            return;

        EndSelectedPour();
        selectedIngredient = ingredient;
        RefreshSelectedVisuals();

        Debug.Log($"[IngredientShelf] Selected ingredient -> {ingredient.displayName}");
    }
    
    private void RefreshSelectedVisuals()
    {
        foreach (BrewIngredientOptionView option in optionViews)
        {
            if (option == null)
                continue;

            option.SetSelectedVisual(option.Ingredient == selectedIngredient);
        }
    }
    
    /// <summary>
    /// Adds the amount actually transferred by a Bartending 2.0 pour action.
    /// This API accepts canonical milliliters.
    /// </summary>
    public float AddLiquidMl(IngredientData ingredient, float amountMl)
    {
        if (targetContainer == null)
            return 0f;

        float transferredMl = targetContainer.AddLiquid(ingredient, amountMl);
        if (transferredMl > 0f)
        {
            physicalPresentation?.ObserveAcceptedPour(transferredMl);
            if (activePourIngredient == ingredient &&
                activePourTarget == targetContainer.ContainerType)
            {
                activePourTransferredMl += transferredMl;
            }
            else
            {
                preparationTrace?.Record(
                    RecipePreparationAction.Pour,
                    ingredient,
                    transferredMl,
                    null,
                    targetContainer.ContainerType);
            }

            RefreshContents();
            NotifyIngredientUsed(ingredient);
        }

        return transferredMl;
    }

    private bool IsIngredientAvailable(IngredientData ingredient)
    {
        return ingredient != null &&
               (availabilityData == null || availabilityData.IsIngredientAvailable(ingredient, availabilityContext));
    }

    /// <summary>
    /// Shares the click-selection path with narrow deterministic validation and
    /// route-local controls. It does not add liquid or change quantity truth.
    /// </summary>
    public bool TrySelectIngredient(IngredientData ingredient)
    {
        if (ingredient == null)
            return false;

        OnIngredientSelected(ingredient);
        return selectedIngredient == ingredient;
    }

    public float PourSelectedIngredient(float amountMl, RecipePreparationAction action = RecipePreparationAction.Pour)
    {
        BeginSelectedPour(action);
        return AddLiquidMl(selectedIngredient, amountMl);
    }

    /// <summary>Live input gate: no ml moves during pickup/alignment/tilt.</summary>
    public bool PreparePhysicalPour(RecipePreparationAction action = RecipePreparationAction.Pour)
    {
        if (PhysicalTransferActive || !BeginSelectedPour(action)) return false;
        return physicalPresentation == null ||
               physicalPresentation.PrepareBottle(selectedIngredient, targetContainer);
    }

    /// <summary>Shared live measurement controls, called only in preparation states.</summary>
    public bool HandlePhysicalTools(float fillRateMlPerSecond)
    {
        physicalPresentation?.ShowMeasurement(Measuring);
        if (PhysicalTransferActive) return true;
        if (Input.GetKeyDown(KeyCode.M) && !HasPendingMeasurement)
        {
            EndSelectedPour();
            Measuring = !Measuring;
        }
        if (!Measuring) return false;
        if (Input.GetKeyDown(KeyCode.T))
        {
            BeginMeasuredTransfer();
            return true;
        }
        if (!Input.GetKey(KeyCode.Space))
        {
            physicalPresentation?.EndBottle();
            return true;
        }
        if (selectedIngredient != null && (physicalPresentation == null ||
            physicalPresentation.PrepareBottle(selectedIngredient, Jigger)))
            FillJigger(fillRateMlPerSecond * Time.deltaTime);
        return true;
    }

    public float FillJigger(float incomingMl)
    {
        if (PhysicalTransferActive || Jigger == null || selectedIngredient == null ||
            float.IsNaN(incomingMl) || float.IsInfinity(incomingMl) || incomingMl <= 0f) return 0f;
        // One owned bottle per measure; do not mix an unlabelled second source.
        if (Jigger.LiquidEntries.Count > 0 && Jigger.LiquidEntries[0].Ingredient != selectedIngredient) return 0f;
        float accepted = Jigger.AddLiquid(selectedIngredient, incomingMl);
        physicalPresentation?.ObserveAcceptedPour(accepted);
        physicalPresentation?.ObserveSpill(incomingMl - accepted);
        physicalPresentation?.ShowMeasurement(Measuring);
        return accepted;
    }

    public bool BeginMeasuredTransfer()
    {
        if (PhysicalTransferActive || !HasPendingMeasurement || physicalPresentation == null ||
            !physicalPresentation.IsResting) return false;
        PhysicalTransferActive = true;
        physicalPresentation.StartCoroutine(TransferMeasurement());
        return true;
    }

    public bool BeginPhysicalContainerTransfer(RecipePreparationAction action, Action<float> completed)
    {
        if (!ToolsReady || physicalPresentation == null || targetContainer == null ||
            targetContainer.CurrentLiquidVolumeMl <= 0f ||
            (targetContainer.ContainerType != BartendingContainerType.MixingGlass &&
             targetContainer.ContainerType != BartendingContainerType.Shaker) ||
            (action != RecipePreparationAction.Strain && action != RecipePreparationAction.PourFromContainer))
            return false;
        EndSelectedPour();
        PhysicalTransferActive = true;
        physicalPresentation.StartCoroutine(TransferWorkingContainer(action, completed));
        return true;
    }

    private IEnumerator TransferWorkingContainer(RecipePreparationAction action, Action<float> completed)
    {
        var source = targetContainer;
        var target = ServingGlass;
        yield return physicalPresentation.MoveContainerToPour(source, target);
        float moved = 0f;
        while (source.CurrentLiquidVolumeMl > 0f)
        {
            if (RecipeGuidePanelController.IsTechniqueReadStateActive || DemoAudioController.IsSettingsOpen)
            { yield return null; continue; }
            float accepted = source.TransferLiquidTo(target, containerTransferMlPerSecond * Time.deltaTime);
            moved += accepted;
            physicalPresentation.ObserveContainerTransfer(accepted);
            yield return null;
        }
        preparationTrace.Record(action, null, moved, source.ContainerType, target.ContainerType);
        yield return physicalPresentation.ReturnContainer();
        preparationState.TrySetActiveTarget(BartendingContainerType.ServingGlass);
        targetContainer = preparationState.ActiveTarget;
        RefreshContents();
        PhysicalTransferActive = false;
        completed?.Invoke(moved);
    }

    private IEnumerator TransferMeasurement()
    {
        var source = Jigger;
        var target = targetContainer;
        var measuredIngredient = source.LiquidEntries[0].Ingredient;
        yield return physicalPresentation.MoveContainerToPour(source, target);
        while (source.CurrentLiquidVolumeMl > 0f)
        {
            if (RecipeGuidePanelController.IsTechniqueReadStateActive || DemoAudioController.IsSettingsOpen)
            { yield return null; continue; }
            float accepted = source.TransferLiquidTo(target, jiggerTransferMlPerSecond * Time.deltaTime);
            if (accepted > 0f)
            {
                preparationTrace.RecordMeasuredPour(measuredIngredient, accepted, target.ContainerType);
                NotifyIngredientUsed(measuredIngredient);
                RefreshContents();
            }
            physicalPresentation.ObserveContainerTransfer(accepted);
            physicalPresentation.ShowMeasurement(Measuring);
            yield return null;
        }
        yield return physicalPresentation.ReturnContainer();
        PhysicalTransferActive = false;
    }

    public bool BeginPhysicalDose(RecipePreparationAction action, float amountMl = 0f, Action<float> accepted = null)
    {
        if (!ToolsReady || selectedIngredient == null || physicalPresentation == null ||
            (action != RecipePreparationAction.Dash && action != RecipePreparationAction.Splash) ||
            (action == RecipePreparationAction.Splash && !BartendingVolume.IsValidSplashMilliliters(amountMl))) return false;
        EndSelectedPour();
        PhysicalTransferActive = true;
        physicalPresentation.StartCoroutine(PerformPhysicalDose(action, amountMl, accepted));
        return true;
    }

    private IEnumerator PerformPhysicalDose(RecipePreparationAction action, float amountMl, Action<float> accepted)
    {
        while (!physicalPresentation.PrepareBottle(selectedIngredient, targetContainer)) yield return null;
        float before = targetContainer.CurrentLiquidVolumeMl;
        if (action == RecipePreparationAction.Dash) TryDashSelectedIngredient(out _);
        else TrySplashSelectedIngredient(amountMl, out _);
        float actual = targetContainer.CurrentLiquidVolumeMl - before;
        accepted?.Invoke(actual);
        for (float elapsed = 0f; actual > 0f && elapsed < doseStreamSeconds; elapsed += Time.deltaTime)
        {
            if (!RecipeGuidePanelController.IsTechniqueReadStateActive && !DemoAudioController.IsSettingsOpen)
                physicalPresentation.ShowDoseStream();
            yield return null;
        }
        physicalPresentation.EndBottle();
        while (!physicalPresentation.IsResting) yield return null;
        PhysicalTransferActive = false;
    }

    /// <summary>
    /// Starts one evidence record for a continuous Pour Core operation. The
    /// container still receives every accepted frame amount immediately; this
    /// only coalesces the trace into the single player action that caused it.
    /// </summary>
    public bool BeginSelectedPour(RecipePreparationAction action = RecipePreparationAction.Pour)
    {
        if (selectedIngredient == null || targetContainer == null)
            return false;

        if (activePourIngredient == selectedIngredient &&
            activePourTarget == targetContainer.ContainerType &&
            activePourAction == action)
            return true;

        EndSelectedPour();
        activePourIngredient = selectedIngredient;
        activePourTarget = targetContainer.ContainerType;
        activePourAction = action;
        activePourTransferredMl = 0f;
        return true;
    }

    public void EndSelectedPour()
    {
        if (!PhysicalTransferActive) physicalPresentation?.EndBottle();
        if (activePourIngredient != null && activePourTransferredMl > 0f)
        {
            preparationTrace?.Record(
                activePourAction,
                activePourIngredient,
                activePourTransferredMl,
                null,
                activePourTarget);
        }

        activePourIngredient = null;
        activePourTransferredMl = 0f;
    }

    /// <summary>
    /// I3 proof seam: future UI may replace these temporary controls, but the
    /// selected target remains the actual container receiving Pour Core liquid.
    /// </summary>
    public bool TrySetActiveTarget(BartendingContainerType containerType)
    {
        EndSelectedPour();
        if (preparationState == null || !preparationState.TrySetActiveTarget(containerType))
            return false;

        targetContainer = preparationState.ActiveTarget;
        RefreshContents();
        Debug.Log($"[Container] Target={targetContainer.ContainerType}");
        return true;
    }

    public bool TryAddIceToActiveTarget()
    {
        EndSelectedPour();
        if (preparationState == null || !preparationState.TryAddIceToActiveTarget())
            return false;

        preparationTrace?.Record(
            RecipePreparationAction.AddIce,
            null,
            0f,
            null,
            targetContainer.ContainerType);
        RefreshContents();
        Debug.Log($"[Container] AddIce target={targetContainer.ContainerType}");
        return true;
    }

    public bool TryTransferActiveTargetToServingGlass(out float transferredMl)
    {
        transferredMl = 0f;
        EndSelectedPour();

        if (preparationState == null || targetContainer == null)
            return false;

        BartendingContainerType sourceType = targetContainer.ContainerType;
        if (!preparationState.TryTransferAllLiquid(
                sourceType,
                BartendingContainerType.ServingGlass,
                out transferredMl))
            return false;

        preparationState.TrySetActiveTarget(BartendingContainerType.ServingGlass);
        targetContainer = preparationState.ActiveTarget;
        RefreshContents();
        Debug.Log($"[Transfer] {sourceType} -> ServingGlass volumeMl={transferredMl:0.###}");
        return true;
    }

    /// <summary>
    /// I9B's Gin Fizz semantic: reuse I3's liquid-transfer primitive while
    /// recording that the bartender poured from the source container rather
    /// than strained it. Ice remains with the source in either operation.
    /// </summary>
    public bool TryPourActiveTargetToServingGlass(out float transferredMl)
    {
        transferredMl = 0f;
        EndSelectedPour();

        if (preparationState == null || targetContainer == null)
            return false;

        BartendingContainerType sourceType = targetContainer.ContainerType;
        if (!preparationState.TryTransferAllLiquid(
                sourceType,
                BartendingContainerType.ServingGlass,
                out transferredMl))
            return false;

        preparationTrace?.Record(
            RecipePreparationAction.PourFromContainer,
            null,
            transferredMl,
            sourceType,
            BartendingContainerType.ServingGlass);
        preparationState.TrySetActiveTarget(BartendingContainerType.ServingGlass);
        targetContainer = preparationState.ActiveTarget;
        RefreshContents();
        Debug.Log($"[PourFromContainer] {sourceType} -> ServingGlass volumeMl={transferredMl:0.###}");
        return true;
    }

    public bool TryStrainActiveTargetToServingGlass(out float transferredMl)
    {
        transferredMl = 0f;
        EndSelectedPour();

        if (preparationState == null || targetContainer == null)
            return false;

        BartendingContainerType sourceType = targetContainer.ContainerType;
        if (!preparationState.TryStrainToServingGlass(sourceType, out transferredMl))
            return false;

        BartendingContainerState source = preparationState.GetContainer(sourceType);
        preparationTrace?.Record(
            RecipePreparationAction.Strain,
            null,
            transferredMl,
            sourceType,
            BartendingContainerType.ServingGlass);
        preparationState.TrySetActiveTarget(BartendingContainerType.ServingGlass);
        targetContainer = preparationState.ActiveTarget;
        RefreshContents();
        Debug.Log(
            $"[Strain] {sourceType} -> ServingGlass volumeMl={transferredMl:0.###} " +
            $"iceRetained={(source != null && source.HasIce)}");
        return true;
    }

    /// <summary>
    /// Executes one discrete Dash for the currently selected source ingredient.
    /// Its ml compatibility amount and dash count both live in the target
    /// container; this UI controller does not retain a duplicate result.
    /// </summary>
    public bool TryDashSelectedIngredient(out int totalDashCount)
    {
        totalDashCount = 0;
        EndSelectedPour();

        if (targetContainer == null || selectedIngredient == null)
            return false;

        bool added = targetContainer.TryAddDash(
            selectedIngredient,
            BartendingVolume.DashesToMilliliters(1),
            out totalDashCount);

        if (added)
        {
            preparationTrace?.Record(
                RecipePreparationAction.Dash,
                selectedIngredient,
                BartendingVolume.DashesToMilliliters(1),
                null,
                targetContainer.ContainerType);
            RefreshContents();
            NotifyIngredientUsed(selectedIngredient);
        }

        return added;
    }

    /// <summary>
    /// A Splash is its own qualitative player action, distinct from a Dash.
    /// The accepted liquid is still stored as ml in the target container.
    /// </summary>
    public bool TrySplashSelectedIngredient(float requestedMl, out float transferredMl)
    {
        transferredMl = 0f;
        EndSelectedPour();

        if (targetContainer == null || selectedIngredient == null ||
            !BartendingVolume.IsValidSplashMilliliters(requestedMl))
            return false;

        transferredMl = targetContainer.AddLiquid(selectedIngredient, requestedMl);
        if (transferredMl <= 0f)
            return false;

        preparationTrace?.Record(
            RecipePreparationAction.Splash,
            selectedIngredient,
            transferredMl,
            null,
            targetContainer.ContainerType);
        RefreshContents();
        NotifyIngredientUsed(selectedIngredient);
        Debug.Log(
            $"[Splash] ingredient={selectedIngredient.ingredientId} target={targetContainer.ContainerType} " +
            $"ml={transferredMl:0.###}");
        return true;
    }

    public void RecordTechniqueAction(
        RecipePreparationAction action,
        BartendingContainerType target)
    {
        if (action != RecipePreparationAction.Stir &&
            action != RecipePreparationAction.GentleStir &&
            action != RecipePreparationAction.Shake)
            return;

        EndSelectedPour();
        preparationTrace?.Record(action, null, 0f, null, target);
    }

    private void RefreshContents()
    {
        physicalPresentation?.RefreshState();
        foreach (BrewIngredientOptionView option in optionViews)
        {
            if (option == null)
                continue;

            option.SetAmountMl(targetContainer != null
                ? targetContainer.GetLiquidAmountMl(option.Ingredient)
                : 0f);
        }

        RefreshTexts();
    }

    private void NotifyIngredientUsed(IngredientData ingredient)
    {
        if (ingredient != null && ingredientsUsedThisSession.Add(ingredient))
            IngredientUsed?.Invoke(ingredient);
    }
}
