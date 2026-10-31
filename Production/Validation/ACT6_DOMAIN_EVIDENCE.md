# ACT6 Last Word preparation domain evidence

Baseline: `2541b62`. This task changes `CocktailCatalog.cs` and
`ShakePreparation.cs`, and adds `LastWordPreparationTests.cs` with Unity metadata.
`Brewing.cs`, saved snapshot fields, save schema, stable identities, tuning,
scenes and prefabs are unchanged by this task.

Authority: Project Vision and current game/bartending design; Final Canon
DRINK-09, SUPPLY-002/005; Architecture 03/04/05; ACT6 implementation plan domain
task. Last Word is four equal 22.5 ml ingredients: `gin`, `green_chartreuse`,
`maraschino_luxardo`, `fresh_lime_juice`. Content adoption and supply/discovery
reachability are separate integration tasks.

`ShakeFinishPolicy.StrainOnly` is appended as value 2. Existing values 0
(`PourThenSplash`) and 1 (`StrainThenPourAndGentleStir`) and the default remain
unchanged. Recipe construction rejects StrainOnly for a non-Shake recipe or any
late ingredient, and still rejects undefined policy values.

Preparation checks the existing actual Shake evidence and coverage of the
current serving composition. Its finish requires at least one accepted Shaker
Strain into Serving and rejects any accepted serving addition made by another
action or source. An asserted final `Strained` flag or an empty/spill-only Strain
record cannot substitute for accepted transfer evidence. Serving ice is
rejected. No late addition, GentleStir or garnish is required by this recipe.

The existing runtime already enforces closed/iced actual motion, opening before
transfer, proportional shaken amounts on partial transfers, and invalidation
after unshaken source additions or mixed Shake stamps. No runtime state extension
was needed. Recognition still reads actual composition; HOW, QUALITY and request
FIT remain independent. Existing Gin Fizz and French 75 finish policies retain
their original code paths.

## Focused standalone evidence

Raw evidence and the isolated harness are under ignored
`Production/Validation/Evidence/act6-domain/`. The harness compiles current
Domain sources and these test classes only: `BartendingDomainTests`,
`BuildDomainTests`, `ShakeDomainTests`, `FloatDomainTests`,
`French75PreparationTests`, `LastWordPreparationTests`. Its local output and
intermediate directories do not share other agents' build outputs.

Command: `dotnet run --project Production/Validation/Evidence/act6-domain/DomainTests.csproj --configuration Debug`.

| Run | Observed result |
| --- | --- |
| `red-policy.log`, before production changes | 83 pass / 26 expected failures: valid StrainOnly construction unavailable |
| `red-preparation.log`, enum/constructor only | 93 pass / 16 expected failures: correct strain rejected, poured finish accepted, missing strain-specific provenance handling |
| `green-preparation.log`, completed implementation | 109 pass / 0 failures; process exit 0 |

The final run contains 32 Last Word cases and 77 existing Stir/Build/Shake/Float/
French 75 cases. It covers direct and held partial Strain conservation, full or
partial Shaker Pour rejection, direct-serving bypass, Mixing Strain and Jigger
return rejection, tiny and ordinary unshaken serving additions, shaker additions
after motion/between strains, absent ice/open lid/no motion/short motion, closed
transfer rejection before mutation, serving ice, independent poor-rhythm QUALITY
and mismatching FIT, mixed batches, and restored flags without accepted transfers.

`git diff --check` is clean for the owned files. The domain diff is 13 inserted
and 2 replaced lines in two C# files; no serialized scene/prefab changes occur.
No failing assertions were weakened. No aggregate tests, Unity Editor execution,
Player input, normal-play reachability, save integration or subjective bartending
feel are claimed by this focused standalone evidence; root integration owns
those remaining tiers.
