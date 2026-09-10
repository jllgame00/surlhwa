using System.Collections.Generic;
using System.Text;

/// <summary>
/// Shared, presentation-only projection of authored recipe data through a
/// RecipeKnowledgeFacts snapshot. Its branch order prevents Guide and Codex
/// from exposing authored details that have not been learned.
/// </summary>
public static class RecipeKnowledgeGuideFormatter
{
    public static bool IsFullyKnown(RecipeKnowledgeFacts facts)
    {
        return facts != null && facts.State == RecipeKnowledgeState.Known;
    }

    public static bool IsIdentityKnown(RecipeKnowledgeFacts facts)
    {
        return IsFullyKnown(facts) || (facts != null && facts.IdentityKnown);
    }

    public static string GetCardLabel(CocktailRecipeData recipe, RecipeKnowledgeFacts facts)
    {
        return IsIdentityKnown(facts)
            ? GetCocktailDisplayName(recipe)
            : "???";
    }

    public static string GetDetailName(CocktailRecipeData recipe, RecipeKnowledgeFacts facts)
    {
        return IsIdentityKnown(facts)
            ? GetCocktailDisplayName(recipe)
            : "Unknown Cocktail";
    }

    public static string GetMethodText(CocktailRecipeData recipe, RecipeKnowledgeFacts facts)
    {
        return IsFullyKnown(facts) || (facts != null && facts.MethodKnown)
            ? "Method: " + (recipe != null ? recipe.method.ToString() : "???")
            : "Method: ???";
    }

    public static string BuildDetailText(
        CocktailRecipeData recipe,
        RecipeKnowledgeFacts facts,
        bool includeKnownDescription = true)
    {
        if (recipe == null || facts == null || facts.State == RecipeKnowledgeState.Unknown)
            return string.Empty;

        bool fullyKnown = IsFullyKnown(facts);
        StringBuilder detail = new();

        if (fullyKnown && includeKnownDescription)
        {
            CocktailData cocktail = recipe.targetCocktail;
            if (cocktail != null && !string.IsNullOrWhiteSpace(cocktail.description))
            {
                detail.AppendLine(cocktail.description.Trim());
                detail.AppendLine();
            }
        }

        AppendIngredients(detail, recipe, facts, fullyKnown);

        if (fullyKnown || facts.PreparationKnown)
            AppendPreparation(detail, recipe, facts, fullyKnown);

        if (fullyKnown || facts.GarnishKnown)
            AppendGarnish(detail, recipe);

        if (fullyKnown && recipe.targetCocktail != null &&
            recipe.targetCocktail.glassware != CocktailGlassware.Unspecified)
        {
            AppendSectionBreak(detail);
            detail.Append("Glassware: ")
                .Append(FormatGlassware(recipe.targetCocktail.glassware));
        }

        return detail.ToString().TrimEnd();
    }

    private static void AppendIngredients(
        StringBuilder detail,
        CocktailRecipeData recipe,
        RecipeKnowledgeFacts facts,
        bool fullyKnown)
    {
        if (recipe.ingredients == null || recipe.ingredients.Length == 0)
        {
            if (fullyKnown)
                detail.Append("Ingredients: No ingredient data.");
            return;
        }

        List<RecipeIngredientEntry> knownEntries = new();
        bool hasUnknownEntries = false;

        foreach (RecipeIngredientEntry entry in recipe.ingredients)
        {
            if (entry == null || entry.ingredient == null)
                continue;

            if (fullyKnown || facts.IsIngredientKnown(entry.ingredient))
                knownEntries.Add(entry);
            else
                hasUnknownEntries = true;
        }

        if (!fullyKnown && knownEntries.Count == 0)
            return;

        detail.AppendLine("Ingredients:");
        foreach (RecipeIngredientEntry entry in knownEntries)
        {
            bool amountKnown = fullyKnown || facts.IsIngredientAmountKnown(entry.ingredient);
            detail.Append("- ")
                .Append(entry.ingredient.displayName)
                .Append(amountKnown ? $" — {entry.ratio:0.##} oz" : " — ?")
                .AppendLine();
        }

        if (!fullyKnown && hasUnknownEntries)
            detail.AppendLine("- ???");
    }

    private static void AppendPreparation(
        StringBuilder detail,
        CocktailRecipeData recipe,
        RecipeKnowledgeFacts facts,
        bool fullyKnown)
    {
        AppendSectionBreak(detail);
        detail.AppendLine("Preparation:");

        if (recipe.expectedPreparation == null || recipe.expectedPreparation.Length == 0)
        {
            detail.Append("No preparation sequence recorded.");
            return;
        }

        foreach (RecipePreparationStep step in recipe.expectedPreparation)
        {
            if (step != null)
                detail.Append("- ").AppendLine(DescribeStep(step, facts, fullyKnown));
        }
    }

    private static void AppendGarnish(StringBuilder detail, CocktailRecipeData recipe)
    {
        AppendSectionBreak(detail);
        detail.AppendLine("Garnish:");

        HashSet<string> garnishLines = new();
        if (recipe.expectedPreparation != null)
        {
            foreach (RecipePreparationStep step in recipe.expectedPreparation)
            {
                if (step == null || step.action != RecipePreparationAction.Garnish ||
                    step.acceptedGarnishes == null)
                {
                    continue;
                }

                foreach (RecipeGarnishExpectation garnish in step.acceptedGarnishes)
                {
                    if (garnish != null)
                    {
                        garnishLines.Add(
                            BartendingGarnishState.GetDisplayName(garnish.garnish) +
                            " — " + garnish.placement);
                    }
                }
            }
        }

        if (garnishLines.Count == 0)
        {
            detail.Append("No garnish expectation.");
            return;
        }

        foreach (string garnishLine in garnishLines)
            detail.Append("- ").AppendLine(garnishLine);
    }

    private static string DescribeStep(
        RecipePreparationStep step,
        RecipeKnowledgeFacts facts,
        bool fullyKnown)
    {
        StringBuilder text = new();
        if (step.optional)
            text.Append("Optional ");

        text.Append(step.action);
        if (step.ingredient != null)
        {
            text.Append(' ')
                .Append(fullyKnown || facts.IsIngredientKnown(step.ingredient)
                    ? step.ingredient.displayName
                    : "???");
        }

        if (step.repeatCount > 1)
            text.Append(" ×").Append(step.repeatCount);

        if (step.source != RecipePreparationContainer.Any)
            text.Append(" from ").Append(step.source);

        if (step.target != RecipePreparationContainer.Any)
            text.Append(" to ").Append(step.target);

        return text.ToString();
    }

    private static void AppendSectionBreak(StringBuilder detail)
    {
        if (detail.Length > 0)
            detail.AppendLine();
    }

    private static string GetCocktailDisplayName(CocktailRecipeData recipe)
    {
        CocktailData cocktail = recipe != null ? recipe.targetCocktail : null;
        return cocktail != null && !string.IsNullOrWhiteSpace(cocktail.cocktailName)
            ? cocktail.cocktailName
            : "Unknown Cocktail";
    }

    private static string FormatGlassware(CocktailGlassware glassware)
    {
        switch (glassware)
        {
            case CocktailGlassware.CocktailGlass:
                return "Cocktail Glass";
            case CocktailGlassware.OldFashionedGlass:
                return "Old Fashioned Glass";
            case CocktailGlassware.TallTumbler:
                return "Tall Tumbler";
            case CocktailGlassware.ChampagneFlute:
                return "Champagne Flute";
            default:
                return "Unspecified";
        }
    }
}
