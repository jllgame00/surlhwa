#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>Read production data, do not manufacture recipe reachability from emotion tags.</summary>
public static class SURLHWARCDataAudit
{
    public static void Run()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<RecipeCatalogData>("Assets/_SURLHWA/Data/Recipes/RecipeCatalog.asset");
        var recipes = catalog.Entries.Select(e => e.Recipe).ToArray();
        Require(recipes.All(r => r != null), "Null catalog member.");
        Require(recipes.Select(r => r.recipeId).Distinct().Count() == recipes.Length, "Duplicate catalog recipe ID.");
        var lines = new List<string> { "recipe\tmethod\tsteps\tingredients\tclassification" };
        foreach (var r in recipes)
        {
            Require(!string.IsNullOrWhiteSpace(r.recipeId) && r.targetCocktail != null, "Invalid recipe identity/reference.");
            Require(r.ingredients.Length > 0 && r.ingredients.All(i => i.ingredient != null && i.ratio > 0), "Invalid ingredient/amount: " + r.recipeId);
            if (!SurlhwaRCGameplayQA.IsCampaignRecipe(r))
            {
                lines.Add(r.recipeId + "\t" + r.method + "\t" + (r.expectedPreparation?.Length ?? 0) + "\t\tOUT_OF_SCOPE_RESERVE; INCOMPLETE_PREPARATION_METADATA");
                continue;
            }
            Require(r.expectedPreparation != null && r.expectedPreparation.Length > 0, "Missing campaign preparation metadata: " + r.recipeId);
            foreach (var step in r.expectedPreparation)
            {
                Require(step != null && Enum.IsDefined(typeof(RecipePreparationAction), step.action), "Invalid preparation step.");
                if (step.action == RecipePreparationAction.Garnish)
                    Require(step.acceptedGarnishes.Length > 0 && step.acceptedGarnishes.All(g =>
                        BartendingGarnishState.IsValid(g.garnish) && BartendingGarnishState.IsValid(g.placement)), "Invalid garnish expectation.");
            }
            lines.Add(r.recipeId + "\t" + r.method + "\t" + string.Join(",", r.expectedPreparation.Select(s => s.action)) +
                "\t" + string.Join(",", r.ingredients.Select(i => i.ingredient.ingredientId)) + "\tCAMPAIGN_CATALOG");
        }
        foreach (var path in AssetDatabase.FindAssets("t:CocktailRecipeData", new[] { "Assets/_SURLHWA" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            var recipe = AssetDatabase.LoadAssetAtPath<CocktailRecipeData>(path);
            if (!recipes.Contains(recipe)) lines.Add(recipe.recipeId + "\t" + recipe.method + "\t" +
                (recipe.expectedPreparation?.Length ?? 0) + "\t\tOUT_OF_SCOPE_RESERVE; metadata=" +
                (recipe.expectedPreparation == null || recipe.expectedPreparation.Length == 0 ? "INCOMPLETE" : "present"));
        }
        Unique<IngredientData>("ingredientId"); Unique<CocktailData>("cocktailId");
        string[] requiredScenes = { "Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity", "Assets/_SURLHWA/Scenes/Gameplay/BartendingScene.unity" };
        Require(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).SequenceEqual(requiredScenes), "Unexpected enabled build scenes.");
        foreach (var path in AssetDatabase.GetDependencies(requiredScenes, true).Where(p => p.EndsWith(".prefab")))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Require(prefab != null, "Missing build prefab: " + path);
            foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0, "Missing component: " + path + "/" + t.name);
        }
        Directory.CreateDirectory("Logs/Review/RCAutomatedQA");
        File.WriteAllLines("Logs/Review/RCAutomatedQA/recipe-inventory.tsv", lines);
        AuditKorean();
        AuditOrdinaryEmotions();
        Debug.Log("[RC data] PASS catalog=" + recipes.Length + "; reserve metadata explicitly classified; stable IDs, ingredients, preparation, scene list, prefab scripts.");
    }
    static void AuditOrdinaryEmotions()
    {
        var previousRandom = UnityEngine.Random.state;
        var combinations = new HashSet<string>();
        var primaries = new HashSet<EmotionTag>();
        try
        {
            foreach (int seed in SurlhwaRCGameplayQA.Seeds)
            {
                UnityEngine.Random.InitState(seed);
                var request = new GuestEmotionRequest();
                var previous = EmotionTag.Loneliness;
                for (int n = 0; n < 128; n++)
                {
                    GeneralGuestEmotionGenerator.FillRandom(request, previous);
                    Require(Enum.IsDefined(typeof(EmotionTag), request.primaryTag) &&
                        Enum.IsDefined(typeof(EmotionTag), request.secondaryTag) &&
                        request.primaryTag != request.secondaryTag && request.primaryTag != previous,
                        "Invalid ordinary emotion assignment; seed=" + seed + " sample=" + n);
                    primaries.Add(request.primaryTag);
                    combinations.Add(request.primaryTag + "/" + request.secondaryTag);
                    previous = request.primaryTag;
                }
            }
        }
        finally { UnityEngine.Random.state = previousRandom; }
        Require(primaries.Count == 10, "Seed corpus did not cover the current ten ordinary primary emotions.");
        File.WriteAllLines("Logs/Review/RCAutomatedQA/ordinary-emotions.txt", combinations.OrderBy(s => s));
        Debug.Log("[RC ordinary] PASS 384 seeded requests; primary=" + primaries.Count + "; combinations=" + combinations.Count);
    }
    static void Unique<T>(string field) where T : UnityEngine.Object
    {
        var ids = new HashSet<string>();
        foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets/_SURLHWA" }))
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            string id = (string)typeof(T).GetField(field).GetValue(asset);
            Require(!string.IsNullOrWhiteSpace(id) && ids.Add(id), "Empty/duplicate " + field + ": " + id);
        }
    }
    static void AuditKorean()
    {
        // Includes serialized authored text and runtime UI strings. Hangul coverage
        // is checked against the actual fallback font, in this disposable project.
        var chars = new HashSet<char>();
        foreach (string file in Directory.EnumerateFiles("Assets/_SURLHWA", "*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".asset") || p.EndsWith(".prefab") || p.EndsWith(".unity"))
            .Concat(Directory.EnumerateFiles("Assets/Code", "*.cs", SearchOption.AllDirectories)))
        {
            string text = Regex.Replace(File.ReadAllText(file), @"\\u([0-9a-fA-F]{4})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
            foreach (char c in text) if (c >= '\uac00' && c <= '\ud7a3') chars.Add(c);
        }
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_SURLHWA/Art/Fonts/KoreanFallback.asset");
        Require(font != null && font.sourceFontFile != null, "Korean fallback/source font missing.");
        var missing = chars.Where(c => !font.HasCharacter(c, true, true)).ToArray();
        File.WriteAllText("Logs/Review/RCAutomatedQA/korean-required.txt", new string(chars.OrderBy(c => c).ToArray()));
        File.WriteAllText("Logs/Review/RCAutomatedQA/korean-glyphs.txt", "font=" + font.name +
            "\nmode=" + font.atlasPopulationMode + "\ncharacters=" + chars.Count + "\nmissing=" + new string(missing));
        Require(missing.Length == 0, "Missing authored Korean glyphs: " + new string(missing));
        Debug.Log("[RC font] PASS " + chars.Count + " Hangul characters from authored assets and source; perceptual typography is separate.");
    }
    static void Require(bool condition, string detail) { if (!condition) throw new InvalidOperationException(detail); }
}
#endif
