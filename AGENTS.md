# SURLHWA — Repository Agent Contract

## 0. Core Rule

SURLHWA is not complete merely because it compiles, passes validators, or
technically allows the player to finish a task.

Every implementation must satisfy BOTH:

1. technical correctness;
2. the intended SURLHWA player experience.

When those conflict, do not sacrifice the project vision for implementation
convenience.

---

# 1. Project Source of Truth

Before making any meaningful design or implementation decision, consult the
following sources in this priority order.

## 1.1 `Docs/00_PROJECT_VISION.md`

Highest-level product philosophy.

This document defines:

- why SURLHWA exists;
- Story First;
- emotional intent;
- the bar as a place where memories accumulate;
- the meaning of cocktails within guest stories;
- pacing and interaction philosophy;
- UI philosophy;
- explicit product non-goals.

Treat this document as the constitutional design authority for SURLHWA.

Implementation convenience, test convenience, roadmap convenience, or
short-term feature completion must not override it.

---

## 1.2 `Docs/01_GAME_DESIGN.md`

Canonical concrete gameplay-design contracts.

Use this document to determine what the current game is supposed to do at
the gameplay/product level.

---

## 1.3 `Docs/11_BARTENDING_2_0_DESIGN.md`

Authoritative detailed bartending-system design where applicable.

Use this for:

- bartending interaction behavior;
- preparation semantics;
- container/state behavior;
- technique behavior;
- recipe execution details;
- related runtime expectations.

---

## 1.4 `Docs/13_POST_I16_PRODUCTION_GAP_AUDIT.md`

Primary production roadmap and audit record.

It contains:

- historical audit evidence;
- production gaps;
- completed implementation records;
- validation evidence;
- validation debt;
- remaining production work.

Historical findings must remain historical.

Do not rewrite an old audit finding merely because the implementation was
later fixed. Append or update implementation records instead.

---

## 1.5 `Docs/06_REBUILD_RULES.md`

Repository rebuild, migration, and implementation-safety rules.

Preserve these rules unless current repository evidence demonstrates that a
documented rule has explicitly been superseded.

---

## 1.6 `Docs/14_SPECIAL_GUEST_ROADMAP.md`

Long-term authored special-guest narrative roadmap.

Defines:

- special-guest thematic roles;
- ACT concepts;
- recurring-character ideas;
- environmental memory / statue concepts;
- revisit concepts;
- long-term unlock ideas.

This document is a **long-term content roadmap**, not an instruction to
implement every special guest immediately.

Current Repository behavior and already-implemented canonical guest flows take
priority over older concept material unless this document explicitly marks a
section as CURRENT.

Do not automatically implement FUTURE / CONCEPT special-guest content merely
because it exists in this document.

---

## 1.7 `Docs/15_IBA_COCKTAIL_CATALOG.md`

Canonical long-term IBA cocktail collection roadmap.

Defines:

- the complete IBA cocktail catalog tracked by SURLHWA;
- long-term recipe / collection scope;
- current production IBA cocktails;
- reserve cocktails;
- future IBA candidates;
- general-guest recipe discovery direction;
- IBA source/version rules.

This document defines the long-term cocktail collection pool, but does NOT
mean all IBA cocktails should be implemented immediately.

Current `CocktailData`, `CocktailRecipeData`, `RecipeCatalogData`,
IngredientData, ingredient availability data, and the production roadmap
remain authoritative for what is actually implemented and reachable now.

New IBA cocktails must be introduced as bounded work with recipe-source,
ingredient, knowledge, supply, runtime, and normal-play validation.

---

# 2. Authority Resolution

When documents or repository evidence appear to conflict, resolve them by
domain.

- `00_PROJECT_VISION.md`
  wins on product philosophy, tone, player experience, and high-level intent.

- `01_GAME_DESIGN.md`
  wins on current concrete gameplay-design contracts.

- `11_BARTENDING_2_0_DESIGN.md`
  wins on detailed bartending-system behavior.

- current repository evidence
  wins over stale assumptions about how an implementation currently works.

- `13_POST_I16_PRODUCTION_GAP_AUDIT.md`
  describes production state and roadmap, but does NOT override the project
  philosophy merely because an audit entry is newer.

- `14_SPECIAL_GUEST_ROADMAP.md`
  wins on long-term special-guest concepts when that content is explicitly in
  scope, but does NOT override already-implemented canonical guest behavior or
  trigger FUTURE content automatically.

- `15_IBA_COCKTAIL_CATALOG.md`
  wins on long-term IBA collection membership and catalog intent, but does NOT
  override current runtime recipe data, ingredient identity, knowledge,
  supply, or production scheduling.

A roadmap acceptance criterion does not justify violating the vision.

If a proposed solution technically satisfies a validator but conflicts with
`Docs/00_PROJECT_VISION.md`, do not implement it as-is.

Find the smallest solution that satisfies both.

If that cannot be done without a meaningful design decision, stop and ask
the human owner.

---

# 3. Project

SURLHWA is a Unity game project.

- Unity: `6000.3.7f1`
- Primary branch: `refactor/project-rebuild`
- Project root: `D:\univ\surlhwa\Surlhwa`
- Primary production roadmap:
  `Docs/13_POST_I16_PRODUCTION_GAP_AUDIT.md`

The repository and its current documentation are the source of truth.

Always inspect current code and serialized assets before relying on historical
chat context, old summaries, old audit assumptions, or previous prompts.

Do not trust a stated milestone status without confirming it against Git and
the current repository.

---

# 4. SURLHWA Design Spirit

Technical correctness is necessary but not sufficient.

Always preserve the design intent in `Docs/00_PROJECT_VISION.md`.

## 4.1 The bar is not an optimization machine

SURLHWA is not primarily an efficiency, management, score-attack, or
optimization game.

Do not introduce unnecessary pressure to:

- maximize throughput;
- maximize score;
- find one mathematically optimal drink;
- rush dialogue;
- optimize customers per minute;
- grind progression systems.

Mechanics may provide structure, but should not overwhelm the atmosphere.

---

## 4.2 The bar is a place where memories accumulate

The bar should feel increasingly familiar as nights pass.

Systems should support:

- listening;
- remembering;
- recognition;
- return visits;
- gradual emotional accumulation;
- relationships with recurring guests;
- the feeling that previous nights mattered.

Do not treat each customer as an isolated disposable transaction when the
existing design intends continuity.

---

## 4.3 Cocktails are interpretation, not passwords

Cocktails are not merely recipes, scoring targets, or answers to hidden
multiple-choice questions.

They are part of how the player interprets:

- a guest;
- a mood;
- a memory;
- a request;
- an emotional situation.

Recommendation gameplay must preserve genuine interpretation.

Avoid turning emotional tags into an obvious mechanical password system.

Multiple reasonable cocktails should remain meaningful where the design
supports them.

A technically valid Recommendation system is still wrong if the player
experience becomes:

> identify keyword → select predetermined drink → receive maximum score

---

## 4.4 Story First

Systems exist to support the story and the player's experience inside the
bar.

Do not add systems merely because they are conventional game mechanics.

Before adding:

- meters;
- currencies;
- timers;
- reward loops;
- progression layers;
- extra menus;
- extra feedback;
- additional UI states;

ask whether they materially improve the intended experience.

Prefer restraint.

---

## 4.5 Quiet does not mean unclear

Atmosphere must not come at the cost of basic usability.

The player should understand:

- what the guest is asking;
- what they have learned;
- what actions are available;
- why a preparation failed when feedback is necessary.

Preserve subtle presentation, but do not make required gameplay information
needlessly obscure.

---

## 4.6 Prefer restrained solutions

When two implementations are technically valid, prefer the one that:

- preserves atmosphere;
- introduces less UI;
- introduces less state;
- creates less cognitive noise;
- better supports narrative pacing;
- better preserves player interpretation;
- requires fewer artificial mechanics.

Do not add a framework when a small existing-system extension is sufficient.

---

# 5. Working Style

Work autonomously.

Do not stop after every subtask or phase to ask for permission.

For every bounded production phase:

1. inspect the current Git baseline;
2. read the relevant current documentation;
3. inspect the current implementation and serialized data;
4. audit before editing;
5. identify authority boundaries;
6. implement the smallest architecture-consistent solution;
7. compile;
8. validate;
9. inspect regression risk;
10. inspect serialized diffs;
11. update documentation;
12. commit internally complete bounded work;
13. continue to the next roadmap phase.

Do not blindly follow stale roadmap assumptions.

Prefer current repository evidence.

Do not manufacture work merely because an old roadmap line exists if the
current implementation already satisfies it.

---

# 6. Stop Conditions

Stop and request human input ONLY when one or more of these conditions apply.

- A genuine game/design decision has multiple materially different valid
  answers.

- A choice materially changes SURLHWA's tone, story, pacing, or emotional
  meaning.

- Current repository evidence materially contradicts the production roadmap.

- A destructive migration is required.

- Stable IDs or save compatibility would need to change.

- A required architecture authority boundary cannot be preserved.

- A new persistence format or irreversible migration is necessary.

- Runtime behavior cannot be validated and the NEXT task critically depends
  on the missing runtime result.

- Final visual, audio, dialogue-feel, gameplay-feel, or pacing approval
  requires subjective human judgment.

Do NOT stop merely because:

- the task is large;
- several files need changes;
- the phase requires many tool calls;
- static validators need to be added;
- an implementation needs multiple iterations;
- Unity licensing blocks Editor validation but subsequent work remains safe
  based on repository/static evidence.

When safe, record validation debt and continue.

---

# 7. Architecture Authority

Preserve the current post-rebuild authority boundaries.

## IngredientData

Owns:

- ingredient identity;
- player-facing ingredient presentation.

---

## CocktailData

Owns:

- cocktail identity;
- player-facing description;
- cocktail art reference;
- emotional interpretation metadata.

---

## CocktailRecipeData

Owns technical recipe truth:

- method;
- ingredients;
- amounts;
- preparation;
- garnish;
- glassware where supported.

---

## RecipeCatalogData

Owns recipe/catalog membership and canonical catalog configuration.

---

## RecipeUnlockManager / Recipe Knowledge

Owns runtime recipe knowledge and progression.

Do not create another recipe-knowledge authority.

---

## Ingredient Availability

Owns physical ingredient supply.

Knowledge and supply are separate concepts.

A recipe being Known does not imply its ingredients are supplied.

An ingredient being supplied does not imply the recipe is Known.

---

## RecipeMatchService

Owns resulting-drink recognition from actual player-created composition.

Do not use customer intent to change what cocktail the liquid composition
actually represents.

---

## RequestEvaluation / FIT

Owns whether the recognized drink fits the active customer request.

Recipe recognition and request suitability are separate concepts.

---

## Bartending Runtime State

Owns actual player-created runtime state, including appropriate:

- liquid state;
- containers;
- preparation actions;
- layering;
- ice;
- garnish;
- related execution state.

Expected recipe metadata must not replace actual runtime truth.

---

## General Rule

Do not duplicate authority.

Do not create a second source of truth because it makes a new feature easier
to implement.

---

# 8. Stable Identity

Do NOT casually change:

- cocktail IDs;
- recipe IDs;
- ingredient IDs;
- persistence keys;
- serialized identity relationships.

Exact ingredient identities remain distinct.

Examples:

- Gin != London Dry Gin
- Dry Vermouth != Sweet Red Vermouth != legacy Vermouth
- Whiskey != Rye Whiskey
- Syrup != Simple Syrup
- legacy citrus != authored fresh citrus identities

Do not merge IDs merely because ingredients appear similar to a human.

Save compatibility takes priority over cosmetic cleanup.

If an ID migration is genuinely necessary, stop and request human review
before performing it.

---

# 9. Git Safety

At the start of every phase run or inspect:

```bash
git status --short
git log -n 5 --oneline
```

Never automatically run:

```bash
git reset --hard
git clean -fd
git push --force
```

Also do not automatically:

- destructively checkout user changes;
- rebase shared existing history;
- amend existing user commits;
- discard unrelated work;
- rewrite history.

Do not overwrite unrelated user changes.

---

# 10. Commit Safety

Before every commit:

- inspect `git status`;
- inspect `git diff`;
- inspect `git diff --check`;
- verify only intended files changed;
- inspect every `.unity` diff;
- inspect every `.prefab` diff;
- verify no unrelated serialization churn;
- verify generated/editor-local files are excluded.

Never commit:

- `Library/`
- `Temp/`
- `Logs/`
- `Obj/`
- `UserSettings/`
- recovery files
- IDE layout files
- unrelated editor-generated files
- unrelated `ProjectSettings` changes

unless the bounded phase explicitly requires a tracked project-setting change.

Use concise Conventional Commit-style messages.

Examples:

```text
feat: add production cocktail discovery flow
content: finalize selected demo cocktail data
fix: preserve discovery retry state
refactor: centralize ...
docs: finalize ...
```

Do not bundle unrelated work.

---

# 11. Commit Policy Under Validation Debt

A bounded phase may be committed when:

- implementation is internally complete;
- compilation/static checks pass;
- the Git diff is intentional;
- no known implementation defect remains;
- environmental validation debt is explicitly documented.

If Unity Editor validation is blocked solely because of local licensing or
Software Terms:

a commit is allowed when:

- the environmental blockage is recorded;
- runtime PASS is NOT claimed;
- static/compile evidence is clean;
- the next phase can safely proceed without the missing runtime evidence.

Do not let a local license problem create one giant unrelated uncommitted
worktree.

Do not use this rule to hide actual implementation uncertainty.

---

# 12. Unity Serialization

Avoid broad scene/prefab serialization churn.

When a `.unity` or `.prefab` modification is required:

- make the smallest possible edit;
- inspect the textual diff;
- reject unrelated transform changes;
- reject unrelated prefab overrides;
- reject unrelated reference churn;
- reject unrelated object ordering changes;
- never accept large serialization noise merely because Unity touched the
  asset.

Do not save unrelated scenes or prefabs.

If a required three-reference change produces hundreds of unrelated scene
lines, investigate before accepting it.

---

# 13. Validation

For relevant phases run as available:

```bash
dotnet build Assembly-CSharp-Editor.csproj --no-restore
```

Also use as applicable:

- focused project validators;
- relevant existing regression validators;
- static contract checks;
- serialized asset/reference checks;
- `git diff --check`;
- Unity Console inspection;
- normal Editor validation;
- Play Mode validation;
- deterministic normal-play simulations.

Never weaken validator assertions merely to obtain PASS.

Never delete a failing validator because it reveals a real contract problem.

Never report runtime PASS when runtime validation did not run.

---

# 14. Unity License Failure

If Unity fails before project load because of local Software Terms or license
state, classify it as:

```text
VALIDATION_BLOCKED_BY_ENVIRONMENT
```

Do not classify that as an implementation failure unless independent
evidence supports that conclusion.

Continue to subsequent phases only when they do not critically depend on the
unobserved runtime result.

Maintain explicit validation debt.

When licensed Unity execution becomes available again, prioritize clearing
accumulated runtime validation debt before claiming final technical closure.

---

# 15. Production Reachability

Never confuse these concepts:

```text
TAG_VALID
MECHANICALLY_REACHABLE
PLAYER_KNOWLEDGE_REACHABLE
ACTUALLY_REACHABLE_IN_NORMAL_PLAY
```

A cocktail is not a valid production answer merely because its emotion tag
matches.

Production gameplay must consider:

- physical supply;
- recipe knowledge;
- available method;
- current request;
- actual encounter routing;
- player-visible information.

Acceptance criteria involving choice must be demonstrated in normal play,
not only in unit tests.

---

# 16. Recipe Discovery

Production recipe discovery should be fair.

The player should receive enough authored information to reasonably attempt
a recipe before successful discovery is required.

Do not mark recipes Known merely because:

- the day started;
- ingredients became available;
- the recipe exists in the catalog;
- a debug route needs it;
- another system wants a multi-answer test to pass.

Knowledge must follow the established recipe-discovery architecture.

Successful authored discovery should reflect the intended recipe, technique,
and required preparation where the production contract requires them.

Failure must not silently consume essential progression content unless that
behavior is explicitly intended by design.

---

# 17. Recommendation Gameplay

Recommendation is interpretation, not exact-recipe lookup.

Preserve the conceptual separation:

```text
recognized cocktail
→ emotional interpretation
→ FIT to guest request
```

Do not allow route templates or mini-game method assets to secretly become
the emotional answer key.

When multiple fitting cocktails are intended, they must be genuinely
reachable during the encounter.

Avoid fake multi-answer situations where:

- multiple cocktails share a tag;
- but only one method is accessible;
- or only one recipe is Known;
- or only one recipe is supplied.

Do not introduce rankings or a single "best" answer unless the game design
explicitly requires one.

---

# 18. Documentation

`Docs/13_POST_I16_PRODUCTION_GAP_AUDIT.md` contains both historical audit
evidence and later implementation records.

Do not rewrite historical audit findings.

Append/update implementation records with:

- implemented scope;
- final authority decisions;
- validation results;
- environment blockers;
- remaining dependencies;
- deferred human sign-off.

Use `Docs/01_GAME_DESIGN.md` only for canonical gameplay-design contracts,
not transient implementation logs.

Use `Docs/00_PROJECT_VISION.md` for stable product philosophy, not milestone
status.

Use `Docs/14_SPECIAL_GUEST_ROADMAP.md` for long-term special-guest content
planning. Do not treat FUTURE / CONCEPT entries as current implementation
requirements.

Use `Docs/15_IBA_COCKTAIL_CATALOG.md` for the long-term IBA collection and
recipe-expansion roadmap. Do not bulk-implement the catalog without an
explicit bounded milestone.

Do not casually rewrite the vision document during implementation.

Changes to the project's fundamental vision require human approval.

---

# 19. Current Production Set

The selected bounded-demo production cocktails are:

- Seolya
- Manwol
- Dry Martini
- Negroni
- Manhattan
- Gin Fizz

Reserve content may remain present for compatibility or future content.

Do not delete reserve content merely because it is outside the current
bounded demo.

Do not add new production cocktails unless the roadmap explicitly calls for
them or the human owner approves the scope change.

---

# 20. Production Priority

Prioritize:

1. coherent normal-play demo flow;
2. preservation of SURLHWA's intended emotional experience;
3. player-understandable recipe learning;
4. genuine Recommendation choice;
5. persistence correctness;
6. architecture authority correctness;
7. deterministic validation;
8. presentation polish.

Do not add speculative systems outside the roadmap.

Prefer deleting accidental complexity over introducing another framework.

Prefer narrow extensions of existing architecture.

---

# 21. Scope Control

Do not spontaneously expand production work into:

- a generalized quest system;
- a new dialogue framework;
- a replacement recipe matcher;
- a replacement knowledge system;
- a new inventory economy;
- procedural dialogue generation;
- new scoring architecture;
- new cocktail families;
- speculative progression systems;
- broad scene rewrites;
- broad UI redesigns.

If an acceptance criterion can be satisfied through the existing architecture,
use the existing architecture.

---

# 22. Human-Owned Final Sign-Off

The agent may implement and objectively validate technical behavior.

The human owner retains final subjective approval for:

- bartending feel;
- dialogue tone;
- dialogue pacing;
- visual composition;
- UI readability;
- art quality;
- animation feel;
- audio quality;
- four-night overall pacing;
- emotional impact;
- final game feel.

Do not report these areas as objectively PASS without human review.

Automated validators may verify technical prerequisites, but they cannot
replace subjective playtesting.

---

# 23. Final Production Closure

When the roadmap appears technically complete, perform a repository-wide
production audit before declaring closure.

Classify every remaining issue into exactly one of:

```text
PASS
VALIDATION_DEBT
HUMAN_SIGNOFF_REQUIRED
DEFERRED_PRESENTATION
OUT_OF_SCOPE
```

Do not call the project technically complete while objective implementation
defects remain.

Do not classify a known defect as presentation debt merely to close the
roadmap.

Do not classify subjective polish as an implementation failure when the
underlying technical contract is already correct.

Final closure must explicitly report:

- gameplay-loop status;
- recipe discovery status;
- Recommendation reachability;
- persistence status;
- four-night lifecycle;
- validation debt;
- presentation backlog;
- human playtest requirements.

Only the human owner decides when SURLHWA is finally ready to present as a
finished game.
