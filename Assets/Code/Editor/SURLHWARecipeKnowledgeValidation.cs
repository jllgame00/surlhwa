#if UNITY_EDITOR
using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Focused I11A proof for the bounded knowledge model. It temporarily replaces
/// recipe-progress keys, restores their exact prior raw values in finally, and
/// never writes recipe/catalog/availability assets.
/// </summary>
public static class SURLHWARecipeKnowledgeValidation
{
    private const string CatalogPath = "Assets/_SURLHWA/Data/Recipes/RecipeCatalog.asset";
    private const string AvailabilityPath =
        "Assets/_SURLHWA/Data/Ingredients/IngredientAvailability.asset";

    [MenuItem("SURLHWA/Validation/Run I11 Recipe Knowledge")]
    public static void Run()
    {
        RecipeProgressSnapshot snapshot = RecipeProgressSnapshot.Capture();
        try
        {
            RecipeCatalogData catalog =
                AssetDatabase.LoadAssetAtPath<RecipeCatalogData>(CatalogPath);
            IngredientAvailabilityData availability =
                AssetDatabase.LoadAssetAtPath<IngredientAvailabilityData>(AvailabilityPath);

            Require(catalog != null, "RecipeCatalogData is missing.");
            Require(availability != null, "IngredientAvailabilityData is missing.");

            CocktailRecipeData dryMartini = FindRecipe(catalog, "recipe_dry_martini");
            CocktailRecipeData manhattan = FindRecipe(catalog, "recipe_manhattan");
            CocktailRecipeData negroni = FindRecipe(catalog, "recipe_negroni");
            CocktailRecipeData manwol = FindRecipe(catalog, "recipe_manwol");
            IngredientData gin = FindIngredient(dryMartini, "gin");

            ValidateUnknownPartialKnownProof(catalog, dryMartini, gin);
            ValidateSaveReloadProof(catalog, dryMartini, manhattan, negroni, gin);
            ValidateLegacyMigration(catalog, dryMartini, negroni, manwol);
            ValidateAvailabilityIndependence(catalog, availability, dryMartini);
            ValidateDefaultPrototypeKnowledge(catalog);
            ValidateSerializationRoundTrip();

            Debug.Log(
                "[I11 Validation] PASS: Unknown / Partial / Known, fact idempotency, " +
                "save-reload, legacy migration, default prototype knowledge, and " +
                "availability independence are preserved.");
        }
        finally
        {
            snapshot.Restore();
        }
    }

    private static void ValidateUnknownPartialKnownProof(
        RecipeCatalogData catalog,
        CocktailRecipeData dryMartini,
        IngredientData gin)
    {
        ClearKnowledgeForTest();
        RecipeUnlockManager manager = CreateKnowledgeManager(catalog);
        try
        {
            RecipeKnowledgeFacts unknown = manager.GetKnownFacts(dryMartini);
            Require(unknown.State == RecipeKnowledgeState.Unknown,
                "Dry Martini must start Unknown in the controlled proof.");
            Require(RecipeKnowledgeGuideFormatter.GetCardLabel(dryMartini, unknown) == "???",
                "Unknown card leaked the cocktail identity.");
            string unknownDetail = RecipeKnowledgeGuideFormatter.BuildDetailText(dryMartini, unknown);
            Require(!ContainsAny(unknownDetail, "Dry Martini", "Gin", "Dry Vermouth", "Stir"),
                "Unknown detail leaked authored recipe facts.");

            Require(manager.LearnMethod(dryMartini), "LearnMethod should change Unknown Dry Martini.");
            Require(!manager.LearnMethod(dryMartini), "Repeated LearnMethod must be idempotent.");
            Require(manager.LearnIngredient(dryMartini, gin),
                "LearnIngredient should accept the authored Gin asset.");
            Require(manager.LearnIngredientAmount(dryMartini, gin),
                "LearnIngredientAmount should add Gin's amount fact.");
            Require(!manager.LearnIngredientAmount(dryMartini, gin),
                "Repeated LearnIngredientAmount must be idempotent.");

            RecipeKnowledgeFacts partial = manager.GetKnownFacts(dryMartini);
            Require(partial.State == RecipeKnowledgeState.Partial,
                "Method and Gin facts must derive Partial, not Known.");
            Require(partial.MethodKnown && partial.IsIngredientKnown(gin) &&
                    partial.IsIngredientAmountKnown(gin),
                "Partial Dry Martini lost an acquired typed fact.");

            string partialDetail = RecipeKnowledgeGuideFormatter.BuildDetailText(dryMartini, partial);
            Require(RecipeKnowledgeGuideFormatter.GetMethodText(dryMartini, partial) == "Method: Stir" &&
                    ContainsAll(partialDetail, "Gin", "2.03 oz"),
                "Partial Guide fields did not show the learned method and Gin amount.");
            Require(!ContainsAny(partialDetail, "Dry Martini", "Dry Vermouth", "Olive"),
                "Partial detail leaked identity, unlearned vermouth, or garnish.");

            Require(manager.MarkKnown(dryMartini), "MarkKnown should promote Partial Dry Martini.");
            Require(!manager.MarkKnown(dryMartini), "Repeated MarkKnown must be idempotent.");
            RecipeKnowledgeFacts known = manager.GetKnownFacts(dryMartini);
            Require(known.State == RecipeKnowledgeState.Known,
                "Successful discovery must explicitly produce Known.");

            string knownDetail = RecipeKnowledgeGuideFormatter.BuildDetailText(dryMartini, known);
            Require(RecipeKnowledgeGuideFormatter.GetDetailName(dryMartini, known) == "Dry Martini" &&
                    ContainsAll(knownDetail, "Gin", "Dry Vermouth", "Stir", "Olive", "Cocktail Glass"),
                "Known Dry Martini did not reveal the supported full recipe truth.");
        }
        finally
        {
            DestroyKnowledgeManager(manager);
        }
    }

    private static void ValidateSaveReloadProof(
        RecipeCatalogData catalog,
        CocktailRecipeData dryMartini,
        CocktailRecipeData manhattan,
        CocktailRecipeData negroni,
        IngredientData gin)
    {
        ClearKnowledgeForTest();
        RecipeUnlockManager manager = CreateKnowledgeManager(catalog);
        try
        {
            Require(manager.LearnMethod(dryMartini), "Could not create partial recipe for reload proof.");
            Require(manager.LearnIngredient(dryMartini, gin), "Could not save partial Gin fact.");
            Require(manager.MarkKnown(manhattan), "Could not create Known recipe for reload proof.");
        }
        finally
        {
            DestroyKnowledgeManager(manager);
        }

        RecipeUnlockManager reloaded = CreateKnowledgeManager(catalog);
        try
        {
            RecipeKnowledgeFacts dryFacts = reloaded.GetKnownFacts(dryMartini);
            Require(dryFacts.State == RecipeKnowledgeState.Partial && dryFacts.MethodKnown &&
                    dryFacts.IsIngredientKnown(gin) && !dryFacts.IsIngredientAmountKnown(gin),
                "Partial recipe facts did not survive reload exactly.");
            Require(reloaded.GetKnowledgeState(manhattan) == RecipeKnowledgeState.Known,
                "Known recipe regressed after reload.");
            Require(reloaded.GetKnowledgeState(negroni) == RecipeKnowledgeState.Unknown,
                "Untouched recipe must remain Unknown after reload.");
        }
        finally
        {
            DestroyKnowledgeManager(reloaded);
        }
    }

    private static void ValidateLegacyMigration(
        RecipeCatalogData catalog,
        CocktailRecipeData dryMartini,
        CocktailRecipeData negroni,
        CocktailRecipeData manwol)
    {
        ClearKnowledgeForTest();
        RecipeProgressPersistence.SaveUnlockedRecipeIdsRaw("recipe_dry_martini|recipe_manwol");

        RecipeUnlockManager manager = CreateKnowledgeManager(catalog);
        try
        {
            Require(manager.GetKnowledgeState(dryMartini) == RecipeKnowledgeState.Known,
                "Legacy unlocked recipe did not migrate to Known.");
            Require(manager.GetKnowledgeState(negroni) == RecipeKnowledgeState.Unknown,
                "Legacy locked recipe did not remain Unknown.");
            Require(manager.GetKnowledgeState(manwol) == RecipeKnowledgeState.Known,
                "Legacy Known Manwol did not remain Known.");
        }
        finally
        {
            DestroyKnowledgeManager(manager);
        }
    }

    private static void ValidateAvailabilityIndependence(
        RecipeCatalogData catalog,
        IngredientAvailabilityData availability,
        CocktailRecipeData dryMartini)
    {
        ClearKnowledgeForTest();
        RecipeUnlockManager manager = CreateKnowledgeManager(catalog);
        try
        {
            Require(manager.GetKnowledgeState(dryMartini) == RecipeKnowledgeState.Unknown,
                "Dry Martini must begin Unknown for availability proof.");

            foreach (RecipeIngredientEntry entry in dryMartini.ingredients)
            {
                Require(entry != null && entry.ingredient != null &&
                        availability.IsIngredientAvailable(
                            entry.ingredient,
                            new IngredientAvailabilityContext(1)),
                    "Dry Martini must remain physically available on Day 1.");
            }

            Require(manager.GetKnowledgeState(dryMartini) == RecipeKnowledgeState.Unknown,
                "Ingredient availability changed recipe knowledge.");
        }
        finally
        {
            DestroyKnowledgeManager(manager);
        }
    }

    private static void ValidateDefaultPrototypeKnowledge(RecipeCatalogData catalog)
    {
        ClearKnowledgeForTest();
        RecipeUnlockManager manager = CreateKnowledgeManager(catalog);
        try
        {
            foreach (RecipeCatalogEntry entry in catalog.Entries)
            {
                if (entry != null && entry.Recipe != null && entry.UnlockedByDefault)
                {
                    Require(manager.IsKnown(entry.Recipe),
                        "Catalog default/prototype recipe was not restored as Known.");
                }
            }
        }
        finally
        {
            DestroyKnowledgeManager(manager);
        }
    }

    private static void ValidateSerializationRoundTrip()
    {
        RecipeKnowledgeSaveData source = new RecipeKnowledgeSaveData
        {
            version = 1,
            entries = new()
            {
                new RecipeKnowledgeSaveEntry
                {
                    recipeId = "recipe_dry_martini",
                    methodKnown = true,
                    knownIngredientIds = new() { "gin" },
                    knownIngredientAmountIds = new() { "gin" }
                },
                new RecipeKnowledgeSaveEntry
                {
                    recipeId = "recipe_manhattan",
                    known = true
                }
            }
        };

        string serialized = JsonUtility.ToJson(source);
        RecipeKnowledgeSaveData restored = JsonUtility.FromJson<RecipeKnowledgeSaveData>(serialized);
        Require(restored != null && restored.version == 1 && restored.entries.Count == 2 &&
                restored.entries[0].knownIngredientIds.Count == 1 &&
                restored.entries[0].knownIngredientAmountIds.Count == 1 &&
                restored.entries[1].known,
            "Recipe-knowledge JSON did not preserve bounded recipe and ingredient IDs.");
    }

    private static RecipeUnlockManager CreateKnowledgeManager(RecipeCatalogData catalog)
    {
        GameObject host = new("I11 Recipe Knowledge Validation")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        RecipeUnlockManager manager = host.AddComponent<RecipeUnlockManager>();

        SerializedObject serializedManager = new(manager);
        SerializedProperty catalogProperty = serializedManager.FindProperty("recipeCatalog");
        Require(catalogProperty != null, "RecipeUnlockManager.recipeCatalog field is missing.");
        catalogProperty.objectReferenceValue = catalog;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();

        MethodInfo awake = typeof(RecipeUnlockManager).GetMethod(
            "Awake",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(awake != null, "RecipeUnlockManager.Awake is missing.");
        awake.Invoke(manager, null);
        return manager;
    }

    private static void DestroyKnowledgeManager(RecipeUnlockManager manager)
    {
        if (manager != null)
            UnityEngine.Object.DestroyImmediate(manager.gameObject);
    }

    private static void ClearKnowledgeForTest()
    {
        RecipeProgressPersistence.ClearRecipeKnowledge();
        RecipeProgressPersistence.ClearLegacyUnlockedRecipeIds();
    }

    private static CocktailRecipeData FindRecipe(RecipeCatalogData catalog, string recipeId)
    {
        foreach (RecipeCatalogEntry entry in catalog.Entries)
        {
            CocktailRecipeData recipe = entry != null ? entry.Recipe : null;
            if (recipe != null && recipe.recipeId == recipeId)
                return recipe;
        }

        throw new InvalidOperationException("[I11 Validation] Missing recipe '" + recipeId + "'.");
    }

    private static IngredientData FindIngredient(CocktailRecipeData recipe, string ingredientId)
    {
        foreach (RecipeIngredientEntry entry in recipe.ingredients)
        {
            if (entry != null && entry.ingredient != null &&
                entry.ingredient.ingredientId == ingredientId)
            {
                return entry.ingredient;
            }
        }

        throw new InvalidOperationException(
            "[I11 Validation] Missing ingredient '" + ingredientId + "' in " + recipe.recipeId + ".");
    }

    private static bool ContainsAny(string value, params string[] expectedFragments)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        foreach (string fragment in expectedFragments)
        {
            if (!string.IsNullOrEmpty(fragment) && value.Contains(fragment))
                return true;
        }

        return false;
    }

    private static bool ContainsAll(string value, params string[] expectedFragments)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        foreach (string fragment in expectedFragments)
        {
            if (string.IsNullOrEmpty(fragment) || !value.Contains(fragment))
                return false;
        }

        return true;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("[I11 Validation] " + message);
    }

    private readonly struct RecipeProgressSnapshot
    {
        private readonly bool hadKnowledge;
        private readonly string knowledgeRaw;
        private readonly bool hadLegacyUnlocks;
        private readonly string legacyUnlocksRaw;

        private RecipeProgressSnapshot(
            bool hadKnowledge,
            string knowledgeRaw,
            bool hadLegacyUnlocks,
            string legacyUnlocksRaw)
        {
            this.hadKnowledge = hadKnowledge;
            this.knowledgeRaw = knowledgeRaw;
            this.hadLegacyUnlocks = hadLegacyUnlocks;
            this.legacyUnlocksRaw = legacyUnlocksRaw;
        }

        public static RecipeProgressSnapshot Capture()
        {
            return new RecipeProgressSnapshot(
                RecipeProgressPersistence.HasRecipeKnowledge(),
                RecipeProgressPersistence.LoadRecipeKnowledgeRaw(),
                RecipeProgressPersistence.HasLegacyUnlockedRecipeIds(),
                RecipeProgressPersistence.LoadLegacyUnlockedRecipeIdsRaw());
        }

        public void Restore()
        {
            if (hadKnowledge)
                RecipeProgressPersistence.SaveRecipeKnowledgeRaw(knowledgeRaw);
            else
                RecipeProgressPersistence.ClearRecipeKnowledge();

            if (hadLegacyUnlocks)
                RecipeProgressPersistence.SaveUnlockedRecipeIdsRaw(legacyUnlocksRaw);
            else
                RecipeProgressPersistence.ClearLegacyUnlockedRecipeIds();
        }
    }
}

/// <summary>
/// Runs a small real-Play-Mode projection proof for the shared Stir Guide
/// prefab. It is separate from the edit-mode persistence validator because the
/// prefab controller must receive Unity lifecycle calls in Play Mode.
/// </summary>
[InitializeOnLoad]
public static class SURLHWARecipeKnowledgePlayModeValidation
{
    private const string PendingKey = "SURLHWA_I11_PLAYMODE_VALIDATION_PENDING";
    private const string CompleteKey = "SURLHWA_I11_PLAYMODE_VALIDATION_COMPLETE";
    private const string SuccessKey = "SURLHWA_I11_PLAYMODE_VALIDATION_SUCCESS";
    private const string CatalogPath = "Assets/_SURLHWA/Data/Recipes/RecipeCatalog.asset";
    private const string StirGuidePrefabPath = "Assets/_SURLHWA/Prefabs/Bartending/StirMiniGame.prefab";

    static SURLHWARecipeKnowledgePlayModeValidation()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    [MenuItem("SURLHWA/Validation/Run I11 Recipe Knowledge Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[I11 PlayMode Validation] Exit Play Mode before starting this validation.");
            return;
        }

        SessionState.SetBool(PendingKey, true);
        SessionState.SetBool(CompleteKey, false);
        SessionState.SetBool(SuccessKey, false);
        EditorApplication.EnterPlaymode();
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
        {
            RunInPlayMode();
            return;
        }

        if (state != PlayModeStateChange.EnteredEditMode ||
            !SessionState.GetBool(CompleteKey, false))
        {
            return;
        }

        int exitCode = SessionState.GetBool(SuccessKey, false) ? 0 : 1;
        SessionState.EraseBool(PendingKey);
        SessionState.EraseBool(CompleteKey);
        SessionState.EraseBool(SuccessKey);
        EditorApplication.Exit(exitCode);
    }

    private static void RunInPlayMode()
    {
        RecipeProgressSnapshot snapshot = RecipeProgressSnapshot.Capture();
        bool success = false;
        try
        {
            RecipeCatalogData catalog = AssetDatabase.LoadAssetAtPath<RecipeCatalogData>(CatalogPath);
            Require(catalog != null, "RecipeCatalogData is missing.");

            CocktailRecipeData dryMartini = FindRecipe(catalog, "recipe_dry_martini");
            IngredientData gin = FindIngredient(dryMartini, "gin");

            RecipeProgressPersistence.ClearRecipeKnowledge();
            RecipeProgressPersistence.ClearLegacyUnlockedRecipeIds();
            RecipeUnlockManager manager = CreateKnowledgeManager(catalog);
            try
            {
                ValidateGuideProjection(
                    dryMartini,
                    manager.GetKnownFacts(dryMartini),
                    "???",
                    "Unknown Cocktail",
                    "Method: ???",
                    string.Empty,
                    "Dry Martini",
                    "Gin");

                Require(manager.LearnMethod(dryMartini), "Could not learn Dry Martini method.");
                Require(manager.LearnIngredient(dryMartini, gin), "Could not learn Dry Martini Gin.");
                Require(manager.LearnIngredientAmount(dryMartini, gin), "Could not learn Dry Martini Gin amount.");
                ValidateGuideProjection(
                    dryMartini,
                    manager.GetKnownFacts(dryMartini),
                    "???",
                    "Unknown Cocktail",
                    "Method: Stir",
                    "Gin",
                    "Dry Martini",
                    "Dry Vermouth");

                Require(manager.MarkKnown(dryMartini), "Could not mark Dry Martini Known.");
                ValidateGuideProjection(
                    dryMartini,
                    manager.GetKnownFacts(dryMartini),
                    "Dry Martini",
                    "Dry Martini",
                    "Method: Stir",
                    "Dry Vermouth",
                    string.Empty,
                    string.Empty);
            }
            finally
            {
                if (manager != null)
                    UnityEngine.Object.Destroy(manager.gameObject);
            }

            success = true;
            Debug.Log("[I11 PlayMode Validation] PASS: Stir Guide prefab projects Unknown, Partial, and Known facts without leakage.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            snapshot.Restore();
            SessionState.SetBool(PendingKey, false);
            SessionState.SetBool(SuccessKey, success);
            SessionState.SetBool(CompleteKey, true);
            EditorApplication.ExitPlaymode();
        }
    }

    private static void ValidateGuideProjection(
        CocktailRecipeData recipe,
        RecipeKnowledgeFacts facts,
        string expectedCardLabel,
        string expectedName,
        string expectedMethod,
        string requiredDetail,
        string forbiddenDetailA,
        string forbiddenDetailB)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StirGuidePrefabPath);
        Require(prefab != null, "StirMiniGame prefab is missing.");

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        try
        {
            RecipeGuidePanelController guide =
                instance.GetComponentInChildren<RecipeGuidePanelController>(true);
            Require(guide != null, "StirMiniGame prefab has no RecipeGuidePanelController.");

            guide.Initialize(
                new RecipeGuideContext(
                    MiniGameType.Stir,
                    new[] { recipe },
                    _ => facts,
                    false),
                null);

            InvokePrivate(guide, "OpenPanel", null);
            Transform listParent = GetPrivateField<Transform>(guide, "recipeListParent");
            TMP_Text cardLabel = listParent != null
                ? listParent.GetComponentInChildren<TMP_Text>(true)
                : null;
            Require(cardLabel != null && cardLabel.text == expectedCardLabel,
                "Guide card did not project the expected knowledge-safe label.");

            InvokePrivate(guide, "SelectRecipe", new object[] { recipe });
            TMP_Text nameText = GetPrivateField<TMP_Text>(guide, "recipeNameText");
            TMP_Text methodText = GetPrivateField<TMP_Text>(guide, "recipeMethodText");
            TMP_Text detailText = GetPrivateField<TMP_Text>(guide, "recipeIngredientText");
            Require(nameText != null && nameText.text == expectedName,
                "Guide name field did not project the expected knowledge state.");
            Require(methodText != null && methodText.text == expectedMethod,
                "Guide method field did not project the expected knowledge state.");

            string detail = detailText != null ? detailText.text : string.Empty;
            if (string.IsNullOrEmpty(requiredDetail))
                Require(string.IsNullOrEmpty(detail), "Unknown Guide detail must remain empty.");
            else
                Require(detail.Contains(requiredDetail), "Guide detail omitted a learned fact.");

            if (!string.IsNullOrEmpty(forbiddenDetailA))
                Require(!detail.Contains(forbiddenDetailA), "Guide detail leaked an unlearned authored fact.");
            if (!string.IsNullOrEmpty(forbiddenDetailB))
                Require(!detail.Contains(forbiddenDetailB), "Guide detail leaked an unlearned authored fact.");
        }
        finally
        {
            UnityEngine.Object.Destroy(instance);
        }
    }

    private static RecipeUnlockManager CreateKnowledgeManager(RecipeCatalogData catalog)
    {
        GameObject host = new("I11 PlayMode Recipe Knowledge Validation");
        RecipeUnlockManager manager = host.AddComponent<RecipeUnlockManager>();
        FieldInfo catalogField = typeof(RecipeUnlockManager).GetField(
            "recipeCatalog",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(catalogField != null, "RecipeUnlockManager.recipeCatalog field is missing.");
        catalogField.SetValue(manager, catalog);
        InvokePrivate(manager, "Awake", null);
        return manager;
    }

    private static T GetPrivateField<T>(object instance, string fieldName) where T : class
    {
        FieldInfo field = instance.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        return field != null ? field.GetValue(instance) as T : null;
    }

    private static void InvokePrivate(object instance, string methodName, object[] parameters)
    {
        MethodInfo method = instance.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(method != null, "Missing private method '" + methodName + "'.");
        method.Invoke(instance, parameters);
    }

    private static CocktailRecipeData FindRecipe(RecipeCatalogData catalog, string recipeId)
    {
        foreach (RecipeCatalogEntry entry in catalog.Entries)
        {
            if (entry != null && entry.Recipe != null && entry.Recipe.recipeId == recipeId)
                return entry.Recipe;
        }

        throw new InvalidOperationException("[I11 PlayMode Validation] Missing recipe '" + recipeId + "'.");
    }

    private static IngredientData FindIngredient(CocktailRecipeData recipe, string ingredientId)
    {
        foreach (RecipeIngredientEntry entry in recipe.ingredients)
        {
            if (entry != null && entry.ingredient != null && entry.ingredient.ingredientId == ingredientId)
                return entry.ingredient;
        }

        throw new InvalidOperationException("[I11 PlayMode Validation] Missing ingredient '" + ingredientId + "'.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("[I11 PlayMode Validation] " + message);
    }

    private readonly struct RecipeProgressSnapshot
    {
        private readonly bool hadKnowledge;
        private readonly string knowledgeRaw;
        private readonly bool hadLegacyUnlocks;
        private readonly string legacyUnlocksRaw;

        private RecipeProgressSnapshot(
            bool hadKnowledge,
            string knowledgeRaw,
            bool hadLegacyUnlocks,
            string legacyUnlocksRaw)
        {
            this.hadKnowledge = hadKnowledge;
            this.knowledgeRaw = knowledgeRaw;
            this.hadLegacyUnlocks = hadLegacyUnlocks;
            this.legacyUnlocksRaw = legacyUnlocksRaw;
        }

        public static RecipeProgressSnapshot Capture()
        {
            return new RecipeProgressSnapshot(
                RecipeProgressPersistence.HasRecipeKnowledge(),
                RecipeProgressPersistence.LoadRecipeKnowledgeRaw(),
                RecipeProgressPersistence.HasLegacyUnlockedRecipeIds(),
                RecipeProgressPersistence.LoadLegacyUnlockedRecipeIdsRaw());
        }

        public void Restore()
        {
            if (hadKnowledge)
                RecipeProgressPersistence.SaveRecipeKnowledgeRaw(knowledgeRaw);
            else
                RecipeProgressPersistence.ClearRecipeKnowledge();

            if (hadLegacyUnlocks)
                RecipeProgressPersistence.SaveUnlockedRecipeIdsRaw(legacyUnlocksRaw);
            else
                RecipeProgressPersistence.ClearLegacyUnlockedRecipeIds();
        }
    }
}
#endif
