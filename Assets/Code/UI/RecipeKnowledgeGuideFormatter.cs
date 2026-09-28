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
            : "이름을 모르는 칵테일";
    }

    public static string GetMethodText(CocktailRecipeData recipe, RecipeKnowledgeFacts facts)
    {
        return IsFullyKnown(facts) || (facts != null && facts.MethodKnown)
            ? "조주법: " + (recipe != null ? recipe.method.ToString() : "???")
            : "조주법: ???";
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
            detail.Append("잔: ")
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
                detail.Append("재료 정보가 없습니다.");
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

        detail.AppendLine("재료:");
        foreach (RecipeIngredientEntry entry in knownEntries)
        {
            bool amountKnown = fullyKnown || facts.IsIngredientAmountKnown(entry.ingredient);
            detail.Append("- ")
                .Append(entry.ingredient.displayName)
                .Append(amountKnown ? $" — {BartendingVolume.OuncesToMilliliters(entry.ratio):0.#} ml ({entry.ratio:0.##} oz)" : " — ?")
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
        detail.AppendLine("준비 순서:");

        if (recipe.expectedPreparation == null || recipe.expectedPreparation.Length == 0)
        {
            detail.Append("아직 기록된 준비 순서가 없습니다.");
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
        detail.AppendLine("가니시:");

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
            detail.Append("가니시 없음");
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
            text.Append("선택 · ");

        text.Append(FormatAction(step.action));
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
            text.Append(" · 출발: ").Append(FormatContainer(step.source));

        if (step.target != RecipePreparationContainer.Any)
            text.Append(" · 도착: ").Append(FormatContainer(step.target));

        return text.ToString();
    }

    private static string FormatAction(RecipePreparationAction action)
    {
        string label = action switch
        {
            RecipePreparationAction.Pour => "붓기",
            RecipePreparationAction.Dash => "대시 더하기",
            RecipePreparationAction.Splash => "스플래시 더하기",
            RecipePreparationAction.AddIce => "얼음 넣기",
            RecipePreparationAction.Stir => "젓기",
            RecipePreparationAction.GentleStir => "가볍게 젓기",
            RecipePreparationAction.Shake => "흔들기",
            RecipePreparationAction.Strain => "걸러 따르기",
            RecipePreparationAction.PourFromContainer => "잔으로 옮겨 붓기",
            RecipePreparationAction.TopUp => "채우기",
            RecipePreparationAction.Garnish => "가니시 올리기",
            _ => action.ToString()
        };
        return label;
    }

    private static string FormatContainer(RecipePreparationContainer container) => container switch
    {
        RecipePreparationContainer.ServingGlass => "서빙 잔",
        RecipePreparationContainer.MixingGlass => "믹싱 글라스",
        RecipePreparationContainer.Shaker => "셰이커",
        _ => string.Empty
    };

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
            : "이름을 모르는 칵테일";
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
