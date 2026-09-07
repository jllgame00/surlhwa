using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class IngredientShelfController : MonoBehaviour
{
    [Header("Ingredient Data")]
    [SerializeField] private IngredientData[] allIngredients;

    [Header("UI")]
    [SerializeField] private Transform ingredientListRoot;
    [SerializeField] private BrewIngredientOptionView ingredientOptionPrefab;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text summaryText;

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

    public IngredientData SelectedIngredient => selectedIngredient;
    public BartendingContainerState TargetContainer => targetContainer;
    public BartendingPreparationState PreparationState => preparationState;
    public BartendingPreparationTrace PreparationTrace => preparationTrace;
    public BartendingContainerState ServingGlass => preparationState != null
        ? preparationState.GetContainer(BartendingContainerType.ServingGlass)
        : null;

    public void Initialize(
        MiniGameType method,
        BartendingContainerType targetContainerType = BartendingContainerType.ServingGlass)
    {
        currentMethod = method;
        selectedIngredient = null;
        preparationState = new BartendingPreparationState(targetContainerType);
        targetContainer = preparationState.ActiveTarget;
        preparationTrace = new BartendingPreparationTrace();
        activePourIngredient = null;
        activePourTransferredMl = 0f;

        // Do not seed from the recipe/default session.
        // The shelf represents what the player has actually poured.
        RebuildIngredientList();
        RefreshTexts();

        gameObject.SetActive(true);
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
            Destroy(ingredientListRoot.GetChild(i).gameObject);
        }

        if (allIngredients == null)
            return;

        IEnumerable<IngredientData> sortedIngredients = allIngredients
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
            titleText.text = $"Ingredients - {currentMethod}";

        if (summaryText == null)
            return;

        string iceText = targetContainer != null && targetContainer.HasIce ? " / ice" : string.Empty;

        if (!HasAnySelectedIngredient())
        {
            summaryText.text = targetContainer != null
                ? $"{targetContainer.ContainerType} empty{iceText}"
                : "No active container";
            return;
        }

        List<string> parts = new List<string>();

        foreach (BartendingLiquidEntry entry in targetContainer.LiquidEntries)
        {
            if (entry == null || entry.Ingredient == null || entry.AmountMl <= 0f)
                continue;

            string dashText = entry.DashCount > 0 ? $" / {entry.DashCount} dash" : string.Empty;
            parts.Add(
                $"{entry.Ingredient.displayName} {entry.AmountMl:0.0} ml " +
                $"({BartendingVolume.MillilitersToOunces(entry.AmountMl):0.##} oz){dashText}");
        }

        summaryText.text =
            $"{targetContainer.ContainerType} {targetContainer.CurrentLiquidVolumeMl:0.0} ml: " +
            string.Join(" / ", parts) + iceText;
    }

    private void OnIngredientSelected(IngredientData ingredient)
    {
        if (ingredient == null)
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
        }

        return transferredMl;
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

    public float PourSelectedIngredient(float amountMl)
    {
        BeginSelectedPour();
        return AddLiquidMl(selectedIngredient, amountMl);
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
}
