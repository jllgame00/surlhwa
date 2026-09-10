using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The single runtime authority for player recipe knowledge. Its historical
/// component name is intentionally preserved because existing scenes and
/// prefabs serialize it. "Unlock" APIs below are compatibility aliases only;
/// current callers use the explicit Unknown / Partial / Known API.
/// </summary>
public class RecipeUnlockManager : MonoBehaviour
{
    [Header("Recipe Catalog")]
    [SerializeField] private RecipeCatalogData recipeCatalog;

    private readonly Dictionary<string, RecipeKnowledgeRecord> knowledgeByRecipeId = new();

    private void Awake()
    {
        LoadKnowledge();
        InitializeDefaultKnowledge();
    }

    public RecipeKnowledgeState GetKnowledgeState(CocktailRecipeData recipe)
    {
        return GetKnownFacts(recipe).State;
    }

    public bool IsKnown(CocktailRecipeData recipe)
    {
        return GetKnowledgeState(recipe) == RecipeKnowledgeState.Known;
    }

    public RecipeKnowledgeFacts GetKnownFacts(CocktailRecipeData recipe)
    {
        if (!TryGetRecipeId(recipe, out string recipeId) ||
            !knowledgeByRecipeId.TryGetValue(recipeId, out RecipeKnowledgeRecord record))
        {
            return CreateUnknownFacts();
        }

        return record.CreateFacts();
    }

    public bool LearnIdentity(CocktailRecipeData recipe)
    {
        return Learn(recipe, RecipeKnowledgeFact.Identity, null);
    }

    public bool LearnMethod(CocktailRecipeData recipe)
    {
        return Learn(recipe, RecipeKnowledgeFact.Method, null);
    }

    public bool LearnIngredient(CocktailRecipeData recipe, IngredientData ingredient)
    {
        return Learn(recipe, RecipeKnowledgeFact.Ingredient, ingredient);
    }

    /// <summary>
    /// Amount knowledge always establishes its corresponding ingredient fact
    /// first. This prevents an amount from existing without an identified
    /// authored IngredientData reference.
    /// </summary>
    public bool LearnIngredientAmount(CocktailRecipeData recipe, IngredientData ingredient)
    {
        return Learn(recipe, RecipeKnowledgeFact.IngredientAmount, ingredient);
    }

    public bool LearnGarnish(CocktailRecipeData recipe)
    {
        return Learn(recipe, RecipeKnowledgeFact.Garnish, null);
    }

    public bool LearnPreparation(CocktailRecipeData recipe)
    {
        return Learn(recipe, RecipeKnowledgeFact.Preparation, null);
    }

    /// <summary>
    /// Full knowledge is intentionally explicit in I11A. A successful recipe
    /// discovery/match grants it; accumulating partial facts does not attempt
    /// brittle automatic completeness inference.
    /// </summary>
    public bool MarkKnown(CocktailRecipeData recipe)
    {
        if (!TryGetRecipeId(recipe, out string recipeId))
            return false;

        RecipeKnowledgeRecord record = GetOrCreateRecord(recipeId);
        RecipeKnowledgeState before = record.GetState();
        if (record.known)
            return false;

        record.known = true;
        PersistChange(recipe, before, record.GetState(), "Marked Known");
        return true;
    }

    public List<CocktailRecipeData> GetKnownRecipesByMethod(MiniGameType method)
    {
        List<CocktailRecipeData> result = new();

        if (recipeCatalog == null)
            return result;

        foreach (RecipeCatalogEntry entry in recipeCatalog.Entries)
        {
            CocktailRecipeData recipe = entry != null ? entry.Recipe : null;
            if (recipe != null && recipe.method == method && IsKnown(recipe))
                result.Add(recipe);
        }

        return result;
    }

    public List<CocktailRecipeData> GetAllRecipes()
    {
        List<CocktailRecipeData> result = new();

        if (recipeCatalog == null)
            return result;

        foreach (RecipeCatalogEntry entry in recipeCatalog.Entries)
        {
            CocktailRecipeData recipe = entry != null ? entry.Recipe : null;
            if (recipe != null)
                result.Add(recipe);
        }

        return result;
    }

    public List<CocktailRecipeData> GetAllRecipesByMethod(MiniGameType method)
    {
        List<CocktailRecipeData> result = new();

        if (recipeCatalog == null)
            return result;

        foreach (RecipeCatalogEntry entry in recipeCatalog.Entries)
        {
            CocktailRecipeData recipe = entry != null ? entry.Recipe : null;
            if (recipe != null && recipe.method == method)
                result.Add(recipe);
        }

        return result;
    }

    public CocktailRecipeData GetRecipeForCocktail(CocktailData cocktail, MiniGameType method)
    {
        return recipeCatalog != null
            ? recipeCatalog.FindRecipeForCocktail(cocktail, method)
            : null;
    }

    public bool HasShownFirstRecipeReveal()
    {
        return RecipeProgressPersistence.HasShownFirstRecipeReveal();
    }

    public bool TryMarkFirstRecipeRevealShown()
    {
        return RecipeProgressPersistence.TryMarkFirstRecipeRevealShown();
    }

    // Compatibility surface for older callers and external editor scripts.
    // The names preserve old integrations; semantics are now recipe knowledge.
    public bool IsUnlocked(CocktailRecipeData recipe) => IsKnown(recipe);
    public bool UnlockRecipe(CocktailRecipeData recipe) => MarkKnown(recipe);
    public List<CocktailRecipeData> GetUnlockedRecipesByMethod(MiniGameType method) =>
        GetKnownRecipesByMethod(method);

    [ContextMenu("Debug/Clear Saved Recipe Knowledge")]
    public void ClearSavedUnlocks()
    {
        RecipeProgressPersistence.ClearRecipeProgress();
        knowledgeByRecipeId.Clear();
        InitializeDefaultKnowledge();

        Debug.Log("[RecipeKnowledge] Cleared saved recipe knowledge and restored catalog defaults.");
    }

    private bool Learn(
        CocktailRecipeData recipe,
        RecipeKnowledgeFact fact,
        IngredientData ingredient)
    {
        if (!TryGetRecipeId(recipe, out string recipeId))
            return false;

        if ((fact == RecipeKnowledgeFact.Ingredient || fact == RecipeKnowledgeFact.IngredientAmount) &&
            !IsAuthoredRecipeIngredient(recipe, ingredient))
        {
            Debug.LogWarning(
                $"[RecipeKnowledge] Cannot learn {fact} for '{recipeId}': " +
                "the supplied IngredientData is not an authored recipe ingredient.");
            return false;
        }

        RecipeKnowledgeRecord record = GetOrCreateRecord(recipeId);
        RecipeKnowledgeState before = record.GetState();
        bool changed = false;
        string detail = fact.ToString();

        switch (fact)
        {
            case RecipeKnowledgeFact.Identity:
                changed = !record.identityKnown;
                record.identityKnown = true;
                break;

            case RecipeKnowledgeFact.Method:
                changed = !record.methodKnown;
                record.methodKnown = true;
                detail = "Method: " + recipe.method;
                break;

            case RecipeKnowledgeFact.Ingredient:
                changed = record.knownIngredientIds.Add(ingredient.ingredientId);
                detail = "Ingredient: " + ingredient.displayName;
                break;

            case RecipeKnowledgeFact.IngredientAmount:
                bool ingredientAdded = record.knownIngredientIds.Add(ingredient.ingredientId);
                bool amountAdded = record.knownIngredientAmountIds.Add(ingredient.ingredientId);
                changed = ingredientAdded || amountAdded;
                detail = "Ingredient Amount: " + ingredient.displayName;
                break;

            case RecipeKnowledgeFact.Garnish:
                changed = !record.garnishKnown;
                record.garnishKnown = true;
                break;

            case RecipeKnowledgeFact.Preparation:
                changed = !record.preparationKnown;
                record.preparationKnown = true;
                break;

            default:
                Debug.LogWarning($"[RecipeKnowledge] Unsupported fact '{fact}'.");
                return false;
        }

        if (!changed)
            return false;

        PersistChange(recipe, before, record.GetState(), "Learned " + detail);
        return true;
    }

    private void LoadKnowledge()
    {
        knowledgeByRecipeId.Clear();

        RecipeKnowledgeSaveData currentData = RecipeProgressPersistence.LoadRecipeKnowledge();
        foreach (RecipeKnowledgeSaveEntry entry in currentData.entries)
            MergeSavedEntry(entry);

        string legacyData = RecipeProgressPersistence.LoadLegacyUnlockedRecipeIdsRaw();
        if (string.IsNullOrWhiteSpace(legacyData))
            return;

        foreach (string rawRecipeId in legacyData.Split('|'))
        {
            string recipeId = rawRecipeId != null ? rawRecipeId.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(recipeId))
                continue;

            RecipeKnowledgeRecord record = GetOrCreateRecord(recipeId);
            record.known = true;
        }

        Debug.Log("[RecipeKnowledge] Loaded legacy unlocked recipes as Known.");
    }

    private void InitializeDefaultKnowledge()
    {
        if (recipeCatalog == null)
            return;

        bool changed = false;
        foreach (RecipeCatalogEntry entry in recipeCatalog.Entries)
        {
            CocktailRecipeData recipe = entry != null ? entry.Recipe : null;
            if (recipe == null || !entry.UnlockedByDefault || !TryGetRecipeId(recipe, out string recipeId))
                continue;

            RecipeKnowledgeRecord record = GetOrCreateRecord(recipeId);
            if (record.known)
                continue;

            record.known = true;
            changed = true;
        }

        if (changed)
        {
            SaveKnowledge();
            Debug.Log("[RecipeKnowledge] Restored catalog default recipes as Known.");
        }
    }

    private void MergeSavedEntry(RecipeKnowledgeSaveEntry entry)
    {
        if (entry == null || string.IsNullOrWhiteSpace(entry.recipeId))
            return;

        RecipeKnowledgeRecord record = GetOrCreateRecord(entry.recipeId);
        record.known |= entry.known;
        record.identityKnown |= entry.identityKnown;
        record.methodKnown |= entry.methodKnown;
        record.garnishKnown |= entry.garnishKnown;
        record.preparationKnown |= entry.preparationKnown;

        AddStableIds(record.knownIngredientIds, entry.knownIngredientIds);

        // A corrupted or old entry cannot create an amount fact without its
        // corresponding ingredient fact.
        if (entry.knownIngredientAmountIds == null)
            return;

        foreach (string ingredientId in entry.knownIngredientAmountIds)
        {
            if (!string.IsNullOrWhiteSpace(ingredientId) &&
                record.knownIngredientIds.Contains(ingredientId))
            {
                record.knownIngredientAmountIds.Add(ingredientId);
            }
        }
    }

    private void PersistChange(
        CocktailRecipeData recipe,
        RecipeKnowledgeState before,
        RecipeKnowledgeState after,
        string detail)
    {
        SaveKnowledge();

        if (before != after)
            Debug.Log($"[RecipeKnowledge] {recipe.recipeId} {before} → {after}");

        Debug.Log($"[RecipeKnowledge] {detail}");
    }

    private void SaveKnowledge()
    {
        List<string> recipeIds = new(knowledgeByRecipeId.Keys);
        recipeIds.Sort(StringComparer.Ordinal);

        RecipeKnowledgeSaveData data = RecipeKnowledgeSaveData.CreateEmpty();
        foreach (string recipeId in recipeIds)
        {
            RecipeKnowledgeRecord record = knowledgeByRecipeId[recipeId];
            if (!record.HasAnyKnowledge)
                continue;

            data.entries.Add(record.ToSaveEntry(recipeId));
        }

        RecipeProgressPersistence.SaveRecipeKnowledge(data);
    }

    private RecipeKnowledgeRecord GetOrCreateRecord(string recipeId)
    {
        if (!knowledgeByRecipeId.TryGetValue(recipeId, out RecipeKnowledgeRecord record))
        {
            record = new RecipeKnowledgeRecord();
            knowledgeByRecipeId.Add(recipeId, record);
        }

        return record;
    }

    private static bool TryGetRecipeId(CocktailRecipeData recipe, out string recipeId)
    {
        recipeId = recipe != null ? recipe.recipeId : string.Empty;
        if (!string.IsNullOrWhiteSpace(recipeId))
            return true;

        Debug.LogWarning("[RecipeKnowledge] Cannot change knowledge for a recipe with an empty recipeId.");
        return false;
    }

    private static bool IsAuthoredRecipeIngredient(CocktailRecipeData recipe, IngredientData ingredient)
    {
        if (recipe == null || ingredient == null || string.IsNullOrWhiteSpace(ingredient.ingredientId))
            return false;

        foreach (RecipeIngredientEntry entry in recipe.ingredients ?? Array.Empty<RecipeIngredientEntry>())
        {
            if (entry != null && entry.ingredient == ingredient)
                return true;
        }

        return false;
    }

    private static void AddStableIds(HashSet<string> destination, IEnumerable<string> source)
    {
        if (source == null)
            return;

        foreach (string id in source)
        {
            if (!string.IsNullOrWhiteSpace(id))
                destination.Add(id);
        }
    }

    private static RecipeKnowledgeFacts CreateUnknownFacts()
    {
        return new RecipeKnowledgeFacts(false, false, false, false, false, null, null);
    }

    private sealed class RecipeKnowledgeRecord
    {
        public bool known;
        public bool identityKnown;
        public bool methodKnown;
        public bool garnishKnown;
        public bool preparationKnown;
        public readonly HashSet<string> knownIngredientIds = new(StringComparer.Ordinal);
        public readonly HashSet<string> knownIngredientAmountIds = new(StringComparer.Ordinal);

        public bool HasAnyKnowledge => known || GetState() != RecipeKnowledgeState.Unknown;

        public RecipeKnowledgeState GetState()
        {
            if (known)
                return RecipeKnowledgeState.Known;

            return identityKnown || methodKnown || garnishKnown || preparationKnown ||
                   knownIngredientIds.Count > 0 || knownIngredientAmountIds.Count > 0
                ? RecipeKnowledgeState.Partial
                : RecipeKnowledgeState.Unknown;
        }

        public RecipeKnowledgeFacts CreateFacts()
        {
            return new RecipeKnowledgeFacts(
                known,
                identityKnown,
                methodKnown,
                garnishKnown,
                preparationKnown,
                knownIngredientIds,
                knownIngredientAmountIds);
        }

        public RecipeKnowledgeSaveEntry ToSaveEntry(string recipeId)
        {
            List<string> ingredientIds = new(knownIngredientIds);
            ingredientIds.Sort(StringComparer.Ordinal);
            List<string> amountIds = new(knownIngredientAmountIds);
            amountIds.Sort(StringComparer.Ordinal);

            return new RecipeKnowledgeSaveEntry
            {
                recipeId = recipeId,
                known = known,
                identityKnown = identityKnown,
                methodKnown = methodKnown,
                garnishKnown = garnishKnown,
                preparationKnown = preparationKnown,
                knownIngredientIds = ingredientIds,
                knownIngredientAmountIds = amountIds
            };
        }
    }
}
