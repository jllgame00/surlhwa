# SURLHWA FULL PROJECT RED-TEAM REVIEW

- Review date: 2026-09-07
- Audited branch: `refactor/project-rebuild`
- Audited HEAD: `2fdb924 docs: finalize persistence ownership architecture`
- Project: SURLHWA / 설화
- Mode: independent, read-only game/design/production/technical review

This document exports the completed red-team review. It is not a new audit, an implementation plan approved by the owner, or a replacement for existing ADRs. Recommendations below remain proposals. No runtime code, assets, Scenes, Prefabs, persistence, or existing design decisions were changed by the review or this export.

## 1. Executive Verdict

**STRUCTURALLY SOUND, BUT THE CORE GAME EXPERIENCE IS STILL UNPROVEN.**

SURLHWA is worth continuing. It is **not ready for full content production**.

The strongest idea is not “a bartending game with literary characters.” It is:

> A bar where the drinks you choose become part of how people—and the room itself—remember an encounter.

The current implementation does not yet demonstrate that promise. It demonstrates a functional service loop, recipe recognition, timing challenges, collection, and a technically integrated special guest.

Four findings matter most:

1. **Interpretation is promised, but recipe reproduction and timing dominate the actual decisions.**
2. **Fresh-save recipe learning is incomplete:** the tutorial introduces methods whose recipes the Guide cannot yet show.
3. **Reading can consume a special guest’s patience:** Little Prince can depart while the player reads his introduction, completing his first-visit flag.
4. **Narrative consequence is not yet playable:** his visit does not remember how the player answered him.

These are more important than another architecture pass, more recipes, or additional ACTs.

### Review baseline and limitations

Confirmed during the completed audit:

- Branch: `refactor/project-rebuild`.
- HEAD: `2fdb924 docs: finalize persistence ownership architecture`.
- Worktree: clean before and after the read-only audit.
- Unity: `6000.3.7f1`, correct project, ready, stopped, not compiling.
- Active scene: BarScene, not dirty.
- No files, assets, Scenes, Prefabs, or saved progress changed; no commits.

The review covered the authoritative documents, current source, serialized assets and scene references through Unity MCP, selected artwork, a camera render, and read-only data probes. **It was not a new end-to-end human playtest.** Retention and pacing predictions are hypotheses, not measured player results. Retained Console entries included historical compilation/test errors and inspection-command failures; this report is not a fresh runtime-certification report.

Authoritative intent sources included:

- [Project Vision](00_PROJECT_VISION.md).
- [Game Design](01_GAME_DESIGN.md).
- [Content Roadmap](04_CONTENT_ROADMAP.md).
- [Rebuild Plan](05_REBUILD_PLAN.md).
- [Rebuild Rules](06_REBUILD_RULES.md).
- [Decisions / ADRs](07_DECISIONS.md).
- [Project Audit, including its labeled H5 current-state note](08_PROJECT_AUDIT.md).
- [Project README](../README.md) and other relevant current-state/planning material.

Historical observations in older current-state/rebuild documents were not treated as stronger evidence than actual HEAD. In particular, older claims that dedicated Little Prince dialogue is dormant no longer describe the current implementation.

### Evidence legend

| Label | Classification | Meaning |
| --- | --- | --- |
| **[I]** | **IMPLEMENTED EVIDENCE** | Current source, authored assets, or inspected Unity state |
| **[D]** | **DOCUMENTED DESIGN** | Explicitly planned, not necessarily implemented |
| **[INF]** | **STRONG INFERENCE** | A reasoned consequence of the implementation or design evidence |
| **[S]** | **SPECULATION** | A hypothesis requiring player testing or another form of validation |

Recommendations and scores are reviewer judgments. They are not assertions that a proposed design has already been tested.

## 2. What SURLHWA Currently Is

### Current Pitch

A prototype bartending game where players measure ingredients, complete short technique challenges, recognize and unlock four cocktails, and receive brief customer reactions across repeating nights.

### Intended Pitch

A story-first game set in a snowy, liminal bar, where cocktails answer guests’ emotions and encounters leave lasting memories in the space. **[D]**

That intention is clear in the Project Vision, especially its priority order and description of the bar as a memory gallery.

### Recommended Pitch

**In a snowbound bar, answer visitors’ unresolved feelings through the drinks you choose, and watch those encounters leave lasting traces in the room.**

The difference is important: the recommended pitch commits to **choice, response, and memory**, rather than the presence of cocktails, dialogue, and snow.

### What belongs to which project state?

| Classification | Current evidence |
| --- | --- |
| Documented intent | Emotional interpretation, literary guests, revisits, persistent objects and atmosphere |
| Implemented | Four preparation routes, ingredient measurement, recognition, unlocks, reactions, day progression, LP introduction and fixed request |
| Prototype-only | Four current cocktails, technique-first generic requests, eight-customer nights, temporary presentation |
| Deferred content | Post-result LP dialogue, ACTs, revisits, environmental memories, ending |
| Dead/dormant material | Generic authored dialogue playback, inactive special-guest selection path, unused special-guest configuration |

This review does not treat deferred content as an accidental implementation omission. It questions whether the planned content will make the loop compelling without changing how it works.

## 3. Why This Game?

**The current build does not yet provide a strong player-facing answer.**

“Listen to customers and make drinks” is an established premise. Four minigames and familiar literary names do not by themselves make it distinctive. **[INF]**

The defensible answer is the combination of:

- A place between departures and returns.
- Korean and Western stories sharing an intimate space.
- Drinks that communicate an interpretation rather than merely satisfy a recipe.
- A bar visibly changed by encounters.

The most valuable differentiator is **the room remembering**.

A drink should eventually mean something like “this was the night someone chose to stay”—not merely “recipe number seven unlocked.” That is a design direction, not a request to write that exact story.

The game should earn its atmosphere through what the player notices and remembers, not rely on snow, poetic names, and melancholy as sufficient differentiation.

## 4. Player Fantasy

The central fantasy should be **being an attentive bartender**, not an efficient order processor or amateur therapist.

| Question | Current answer | Stronger target |
| --- | --- | --- |
| What does the player think about? | Ingredients, quantities, technique inputs, hidden recipe knowledge | What this person is asking for—and what kind of response to offer |
| What skill improves? | Recipe recall, interface familiarity, timing | Observation, remembering guests, understanding drink character, confident preparation |
| What distinguishes expertise? | Fewer input mistakes and more known recipes | A defensible choice, not merely a perfect execution |
| What emotional experience is distinctive? | Brief poetic reactions | Recognizing that an encounter changed the guest or the bar |

The documents already provide an important restraint: the bartender does not need to immediately understand or solve everyone. **[D]**

Protect that. A drink can accompany uncertainty without curing it.

There can still be satisfaction in accurate measurement and good physical execution. Those should support the attentive-bartender fantasy, rather than become the only skills the game can recognize.

## 5. Actual Player Loop

```text
Launch into BarScene
    ↓
Load saved day / recipe progress / LP flag
    ↓
Spawn one active customer → movement → seating
    ↓
Ellipsis interaction
    ├─ Generic: generated hint
    └─ Little Prince: four authored lines + Next
    ↓
Brew
    ├─ Day-1 tutorial: forced technique
    ├─ Little Prince: fixed dedicated Float request
    └─ Generic: choose technique
    ↓
Bartending preview
    → Guide: unlocked recipes for this method only
    → select ingredients / measure / pour
    ↓
Short technique challenge
    ↓
Technical recognition + request evaluation
    ↓
Unlock / toast / reputation / customer reaction
    ↓
Departure → count encounter
    ↓
Repeat until eight encounters have ended
    ↓
DayResult → next day saved → reload BarScene
```

A parallel patience timer operates before bartending for non-tutorial customers. Departure can occur without service. **[I]**

Implemented macro progression currently adds Little Prince on the first eligible Day-2 spawn. It does not then lead through the planned ACT sequence or an ending.

The enabled build-scene path currently starts with the bar rather than a completed title/opening flow. A final opening, complete settings/navigation experience, and ending are not established by the current playable loop. **[I]**

## 6. Core Loop Analysis

| Dimension | Assessment |
| --- | --- |
| Decision density | Preparation has many actions, but relatively few consequential choices |
| Learning | Recipe and input learning exist; the path to learning locked recipes is weak |
| Tension | Short timing tests and patience provide pressure, but conflict with attentive reading |
| Payoff | Recognition is concrete; narrative payoff and emotional explanation are weak |
| Variation | Four inputs, four recipes, generated tag pairs; limited encounter differentiation |
| Downtime | Repeated movement, interface transitions, reaction delays, and respawning accumulate |
| Replay value | Currently practice and collection, not changed relationships or interpretation |

**The strongest link:** the player genuinely constructs the composition; the system does not simply pretend a selected menu item was made. **[I]**

**The weakest link:** the response does not convincingly connect the drink decision to this particular person.

### Why serve customer #20?

At present, there is no strong implemented answer beyond practicing execution or completing the small catalog. Customer #20 falls after the early tutorial and Day-2 introduction, without an equivalent new relationship development. **[INF]**

The problem is not simply that four recipes are too few. Adding recipes to an unchanged encounter structure can increase memorization without increasing interest.

### Repetition forecast

| Time | Likely experience with current content |
| --- | --- |
| First 10 minutes | Initial curiosity, followed by possible recipe-learning confusion |
| 30 minutes | Repeated preparation and shallow reactions become noticeable |
| 1 hour | Four recipes and anonymous emotional requests are unlikely to sustain discovery |
| 3 hours | Current content does not support this duration meaningfully |
| Full game | Must be carried by developing encounters, not a larger version of the same service quota |

These are **[S]**, not playtest findings.

The useful retention question is not “did players finish a night?” It is “what did they want to see happen next?”

## 7. Bartending Analysis

The documented differentiation is stronger than the implemented differentiation.

| Method | Actual input and failure | Mastery / readability | Physical fantasy | Repetition risk |
| --- | --- | --- | --- | --- |
| Stir | Press Space as an automatically advancing gauge enters a target; inspected prefab duration 2.5 seconds | Readable timing test; modest ceiling | Spoon movement is visual; player does not control stirring speed or stability | High once timing is learned |
| Shake | Alternating directional preparation followed by Space; six required shakes within six seconds; mistakes can fail | More input coordination, but no actual musical beat to follow | More active than Stir; still resembles a command sequence | Medium initially, high with an unchanged pattern |
| Build | Hold Space to raise liquid, release near target; timeout/overflow/bad release fail | Predictive release timing | Does not perform ingredient-by-ingredient building or order decisions | High |
| Float | Hold Space and release near a surface target, more slowly; timeout/overflow/bad release fail | Similar skill to Build | Spoon/layer presentation exists, but density and layering decisions do not | Very high relative to Build |

Sources: [StirMiniGame](../Assets/Code/MiniGames/StirMiniGame.cs), [ShakeMiniGame](../Assets/Code/MiniGames/ShakeMiniGame.cs), [BuildMiniGame](../Assets/Code/MiniGames/BuildMiniGame.cs), [FloatMiniGame](../Assets/Code/MiniGames/FloatMiniGame.cs).

### The most consequential disconnect

The composition is assembled **before** these challenges. Their outcomes do not change the measured ingredient composition. **[I]**

Consequently:

- A bad pour challenge does not necessarily mean the composition contains too much liquid.
- A good challenge does not repair an incorrect composition.
- Stir quality is not derived from maintaining stirring behavior.
- Float recognition does not depend on actual layer order.

Keeping execution and recognition separate is architecturally sensible. The problem is that the on-screen action can imply a physical consequence the simulation does not represent.

### Pacing and extensibility

The current nominal challenge lengths are short: approximately 2.5 seconds for inspected Stir, a six-second Shake limit, a 4.5-second Build limit, and a five-second Float limit. The surrounding preparation and feedback can occupy more attention than the skill test itself. **[I/INF]**

Making target windows narrower would add difficulty, but not necessarily deeper mastery. Adding more recipes would not automatically change the gestures. Extensions need to change what players observe or control, not just reskin the gauge.

### Are all four justified?

**MERGE SOME at the interaction-design level.**

Keep four preparation methods as recipe vocabulary. Do not assume each deserves a separate mandatory scoring game.

Recommendation:

- Keep a distinct Stir gesture and Shake gesture if they become satisfying.
- Integrate Build into ingredient pouring.
- Treat Float as a genuinely different layering variation only if player control changes materially.
- Otherwise share the pouring interaction and cut the extra grading step.

For the next slice, **two excellent interactions are more useful than four serviceable ones**. This recommendation challenges the gameplay differentiation, not the value of the existing technical route separation.

## 8. Recipe / Ingredient Analysis

### Current recipe space

Unity inspection confirms:

| Method | Cocktail | Recipe ID | Ingredients |
| --- | --- | --- | --- |
| Stir | 설야 | `recipe_test_stir` | Whiskey, vermouth, water |
| Shake | 여명 | `recipe_test_shake` | Rum, lime, syrup |
| Build | 잔설 | `recipe_janseol` | Gin, tonic, lemon |
| Float | 만월 | `recipe_manwol` | Vodka, coffee liqueur, cream |

All four have a `0.85` matching threshold. There is **one recipe per method and no shared ingredient between recipes**. **[I]**

That is useful for isolated engineering tests, but poor evidence of a scalable recipe-learning game.

### Discovery currently means recognition and registration

The player can experiment, but the game provides little structured information for learning from that experiment.

The matcher knows ingredient and quantity differences; most of that knowledge stays in diagnostics rather than becoming useful player feedback. See [RecipeMatchService](../Assets/Code/Services/RecipeMatchService.cs). **[I]**

A threshold alone does not create interesting experimentation. Unknown cocktails need not all become named products, but a failed experiment should teach something if experimentation is a pillar.

### Tolerance needs a content test, not immediate replacement

Matching uses weighted ingredient coverage, absolute quantity differences, and extra ingredients. It is not simply proportional ratio matching.

For the current three-ingredient recipes, changing one quantity by `0.5 oz`, with everything else exact, produces approximately `0.864` similarity under the formula—still a recognized recipe. **[I: calculation from source]**

That tolerance may be appropriate. The player needs to understand what changed, however, rather than seeing a binary database outcome. Currently, recognized variants within tolerance do not create a correspondingly rich explanation of the drink’s altered character.

### Ingredient weaknesses

- Twelve ingredients currently support four disjoint recipes.
- All inspected ingredient icons are unassigned.
- Ingredient identity is principally name/category, not an understandable flavor vocabulary.
- Emotional meaning belongs to finished cocktails, with little guidance connecting ingredient choice to that meaning. **[I]**

This encourages recipe reproduction more than transferable learning. A shared base, contrasting modifier, and a small set of recognizable sensory associations would let knowledge carry between drinks.

### The real-cocktail plan creates a near-term authoring constraint

The current [jigger](../Assets/Code/UI/JiggerPanelController.cs) exposes `0.5` and `1.0` increments. A final catalog needing quarter-ounce measures or other small units cannot be authored faithfully through that UI without a deliberate simplification policy. **[INF]**

Before scaling content:

1. Choose a small representative set of final-direction cocktails.
2. Reuse ingredients across them.
3. Check their measurements and preparation vocabulary against the actual controls.
4. Decide what the game intentionally simplifies.

Do not build a full chemistry simulation. Do not discover these constraints after illustrating thirty drinks.

Ingredient choice becomes meaningful when the player can reason about a small set of alternatives. It becomes combinatorial noise when every new drink introduces another unrelated group of bottles.

## 9. Customer Analysis

### Generic

**Currently, generic customers are closer to practice targets than people.**

The active scene falls back to Customer_A because the general customer collection is empty. Emotional requests vary, but continuing identity and memory do not. **[I]**

Random tags are not personality. A different emotional sentence is not necessarily a different encounter.

Their cheapest useful upgrade is not hundreds of lines:

- Two or three recognizable regular archetypes.
- One stable preference or habit each.
- A small change across successive nights.
- A request tied to something recently learned.
- One specific acknowledgment of what was served.

Ordinary guests should provide contrast, familiarity, and safe experimentation—not simply fill the interval between important guests.

Their mechanical purpose can be practice without becoming filler if the practice is purposeful: reinforce a new recipe family, let the player compare two effects, or revisit a familiar request under changed circumstances.

### Special

Special guests need at least one thing ordinary guests do not:

**A reason for the player to remember their choice afterward.**

That can be achieved with a short return, a changed line, or an object—not a branching novel.

The name of a famous character can create anticipation. It cannot substitute for characterization, an understandable dilemma, or a consequential offering.

## 10. Little Prince Vertical-Slice Critique

**Yes: currently, Little Prince is mechanically a generic customer with four extra lines and a fixed order.**

That is an accurate assessment of the artistic slice, not a dismissal of the integration work.

His [authored introduction](../Assets/_SURLHWA/Data/Dialogue/LittlePrinceFirstVisitDialogue.asset) asks about adults, importance, lingering starlight, and an unforgettable taste. It does **not** explicitly ask for 만월. The dedicated request nonetheless enforces exactly that cocktail through Float. **[I]**

The four existing lines, in authored order, are:

1. “문이 열리자, 눈보라 사이로 아주 작은 별빛이 흘러들어왔다.”
2. “어른들은 왜 중요한 걸 몰라요?”
3. “숫자는 잘 기억하면서, 별빛이 얼마나 오래 남는지는 묻지 않아요.”
4. “제게... 잊히지 않는 맛을 줄 수 있나요?”

Three problems follow.

### 1. Interpretation is implied, but the answer is fixed

The text invites an expressive response. The request behaves like a named-drink exam.

Both designs can work. Their combination currently misrepresents the player’s freedom.

### 2. His reaction is not authored to his request

`SitDown()` assigns generic random emotions, including to Little Prince. His dedicated dialogue does not expose those generated preferences. Therefore, making the exact requested cocktail can still receive a low emotional rating for reasons the player could not infer. **[I]**

### 3. Reading can consume the encounter

The prefab has 10–15 seconds of patience. The timer pauses during bartending, not during the four-line introduction. On timeout, departure can mark the first visit complete. **[I]**

This is a functional contradiction of the invitation to read slowly—not cosmetic polish.

Relevant sources: [CustomerController waiting/patience/reactions](../Assets/Code/Customer/CustomerController.cs), [CustomerSpawner departure flag](../Assets/Code/Managers/CustomerSpawner.cs).

Preserve departure-based completion if that remains the product policy. Remove the accidental reading deadline; retry logic is not required to solve this.

The empty post-result dialogue arrays are intentionally deferred content. The important judgment is that the next slice must use authored consequence to prove the guest’s narrative importance—not merely activate another static sequence.

## 11. Request / Interpretation Design

SURLHWA needs two clearly distinguishable request contracts.

| Request | Player understanding | Appropriate evaluation |
| --- | --- | --- |
| Explicit cocktail request | “They asked for this drink” | Identity correctness |
| Interpretive request | “They described a need; several responses might make sense” | Authored acknowledgment of the chosen response |

`requestedCocktail` is appropriate for the first contract. It should not silently become the answer key for the second.

For special guests, the recommendation is:

- Two plausible drink responses when choice matters.
- A clue explaining why each might fit.
- No requirement for the player to read the writer’s mind.
- A response acknowledging the interpretation, even when the guest disagrees.

Also reconsider **technique-first navigation**. For an emotional decision, choosing “Stir” before considering a drink is an implementation-shaped interaction. Let the player inspect possible drinks or responses before committing to a method.

A notable current limitation: because Float has only one valid recipe, Little Prince’s wrong-but-valid outcome has no natural alternative within his normal dedicated route. The evaluator supports the case; the current content does not meaningfully offer it. **[I/INF]**

Ambiguity is useful when players can articulate competing interpretations. It is frustrating when a poetic line conceals a single unexplained correct recipe.

## 12. Narrative / Gameplay Integration

Currently, story and bartending largely happen **in sequence**, rather than changing each other.

```text
Story text → fixed request → preparation → generic reaction
```

The intended connection is:

```text
Understand something → choose a response → observe meaning → remember it later
```

The architecture can already preserve the actual cocktail, technical result, and request evaluation separately. That is a useful foundation.

The minimum next consequence should be:

1. Two understandable drink choices.
2. One short, choice-specific acknowledgment.
3. One later callback.
4. One visible trace in the bar.

The subsequent story can reconverge. Separate ACT trees for every grade, recipe, and emotion are unnecessary.

Also avoid making each literary guest an embodiment of a theme who delivers philosophical questions on arrival. Give them a mundane behavior, contradiction, or moment of humor first. Otherwise seven different characters risk sounding like the same melancholy author. **[INF from the roadmap’s thematic structure]**

Story consequence need not mean the bartender determines a guest’s life. Remembering a preference, noticing an interpretation, or retaining an object can provide agency without positioning a drink as a cure for distress.

## 13. Failure / Outcome Design

Keep the distinctions internally. Make them intelligible externally.

| Outcome | Current meaning | Recommended player-facing treatment |
| --- | --- | --- |
| Mechanical failure | Input execution was poor | Show the physical imperfection; permit a human response |
| Technical no-match | Composition did not sufficiently match a recipe | Provide one useful clue about the mixture |
| Wrong valid cocktail | A real drink, but not the named request | Acknowledge the actual drink and the missed request |
| Emotional mismatch | Drink does not match generated desired tags | Explain the mismatch through the guest, or avoid numerical judgment |
| Poor execution, correct identity | Recognizable requested drink, poorly made | Communicate both without contradictory “complete/fail” impressions |

Currently, failure mostly changes rewards, hearts, and brief text. It seldom creates learning or a memorable encounter. **[I]**

The outcome implementation distinguishes these axes more carefully than the player-facing explanation does. Mechanical reputation changes remain independent; request rewards retain mechanical/technical gates and are withheld for request mismatch. Emotional heart satisfaction is not the authority for that reward. **[I]**

This means the most legible incentive can be executing and recognizing a recipe rather than understanding a person. **[INF]**

Do not make failure interesting by writing a giant alternate storyline. One specific observation is often enough. Small comic or awkward moments can also give failure character, provided the tone remains respectful and legible.

Preserve the ability to discover an actual recipe even when it was not what the customer requested. That is a good experimentation incentive.

## 14. EmotionTag / Reaction Analysis

### Verdict

**UNDERUSED DATA, with potential as a useful supporting system—not yet a convincing core system.**

The tags mix:

- Emotional states: loneliness, regret, longing.
- Desired effects: comfort, awakening.
- Sensory qualities: bitterness, softness, heaviness, clarity.

That mixture can support evocative writing, but the player lacks a reliable vocabulary for interpreting it.

### The coverage problem is concrete

The generator permits **30 ordered primary/secondary combinations**. Comparing those combinations with the four authored cocktails, only **five** can match both desired tags and therefore reach three hearts with successful execution. **[I]**

This is a coverage count, not a claimed probability distribution.

Additionally:

- Loneliness and Heavy appear on no current cocktail.
- Manwol’s Longing + Clarity pairing is not generated by the generic pairing map.
- The first two forced tutorial requests do not fully match the corresponding cocktails’ authored tag pairs. **[I]**

Sources: [GeneralGuestEmotionGenerator](../Assets/Code/Dialogue/GeneralGuestEmotionGenerator.cs), [GuestReactionResolver](../Assets/Code/Dialogue/GuestReactionResolver.cs).

Not every guest must be perfectly satisfiable. But if three hearts signal mastery, systematically unavailable full matches need an explicit design meaning. Otherwise players cannot tell insightful imperfection from an unfair answer key.

### Reaction feedback

Reactions use the actual cocktail’s tags, which is correct. However, the chosen lines are broad, and multi-tag database entries can qualify through any overlap rather than an exact pair. **[I]**

The current heart calculation adds a point for successful mechanical execution and points for matching desired tags. It does not communicate a nuanced distinction between Good and Perfect preparation or explain why a particular ingredient affected the guest. **[I]**

Recommendation:

- Keep tags as authoring support.
- Reduce their role as hidden arithmetic.
- Make important reactions explain one property of the offered drink.
- Do not grade every emotionally ambiguous encounter with hearts.

A combination of a visible physical reaction and one specific line is likely more valuable than additional emotion categories or expensive animation coverage. **[INF]**

## 15. Recipe Guide / Codex / Discovery

### Recipe Guide

It should be dependable reference material—not a memory exam and not an obstacle to learning.

The current Guide receives only unlocked recipes for the current method. Fresh progress defaults only Stir. The next tutorial customers force Shake, Build, and Float, while their recipes remain unavailable until discovered. The current hints do not supply their ingredient formulas. **[I]**

This creates a circular learning path:

```text
Need recipe to make drink
→ recipe hidden until discovery
→ no actionable discovery clue
```

See [Guide context construction in OrderManager](../Assets/Code/Managers/OrderManager.cs) and [unlock-filtered queries in RecipeUnlockManager](../Assets/Code/Data/Recipes/RecipeUnlockManager.cs).

The problem is therefore **not** that the Guide currently gives away every recipe. It is that it withholds information without supplying an alternative learning route.

A developer testing with known formulas or existing unlock progress can miss this fresh-save problem. **[INF]**

Recommended information levels:

| State | Show |
| --- | --- |
| Known | Exact recipe, method, recognizable drink character |
| Partially known | A useful clue: ingredient family, preparation, or a guest’s observation |
| Unknown | A reason to care and an achievable route to learning—not just `???` |

Players should be free to consult the Guide whenever needed. Repeated consultation should not be penalized or made cumbersome. Familiarity can naturally reduce consultation; the game need not force memorization to manufacture mastery.

### Codex

The current Codex is a collection/reference interface. It does not yet justify being a record of relationships.

Locked entries conceal the name and method and do not provide an adequate discovery trail. Known entries provide useful identity and recipe information, but do not record the encounter that made the drink meaningful. **[I]**

Keep it as **reference + encounter archive**:

- What the drink is.
- When or through whom it became meaningful.
- A short remembered observation.

Do not turn it into a second progression dashboard. Collection completion alone is unlikely to sustain the intended emotional experience. **[INF]**

### Discovery

Showing an exact learned recipe does not destroy discovery. Discovery can be learning **why and when that drink matters**, not repeatedly remembering quantities.

Technical match → actual recipe unlock is worth preserving. It supports accidental discoveries and separates experimentation from obedience to a specific request. The missing part is a paced route from curiosity to an informed attempt.

Guided first discoveries, useful near-miss observations, and a personal record of the discovery would strengthen the existing architecture without requiring a combinatorial crafting system.

## 16. Day / Macro Progression

Current nights end after eight departures, including customers who were not served. DayResult then saves the next day and offers a scene reload. **[I]**

Two issues:

1. Eight customers are a quota, not a dramatic structure.
2. DayResult’s “newly recorded cocktails” list is populated from **all unlocked cocktails**, not the ones discovered that night. **[I]**

Sources: [BusinessDayManager](../Assets/Code/Managers/BusinessDayManager.cs), [DayResultPanelController](../Assets/Code/UI/DayResultPanelController.cs).

Beyond Day 2, the implemented answer to “what changes?” is currently limited.

| Scale question | Repository answer |
| --- | --- |
| Customers per implemented night | 8 |
| Current recipes | 4 |
| Current special-guest visit | 1 LP first visit |
| Planned major ACTs | 7 |
| Planned central literary people | 8, counting Chunhyang/Mongryong separately |
| Revisit outlines | 6 |
| Final nights / ordinary encounters / playtime | Unresolved |
| Final cocktail count | Unresolved |
| Final ending structure | Not production-defined |

Recommend **three to five purposeful encounters per night**, with night length following narrative load. Eight short customers are not automatically better than four memorable ones.

A night should change at least one of these:

- What the player knows about a person.
- What a familiar drink means.
- What is available or learnable.
- What is visible or audible in the room.
- What the player anticipates for a later visit.

DayResult should close that night, not simply restate lifetime collection progress. A concise “who came / what changed / what was learned” summary is more aligned with the project than an efficiency report.

## 17. Ending / Long-Term Goal

The project has a strong **ending image**—a bar containing traces of departed guests—but not yet a sufficiently concrete ending contract.

The roadmap is a sequence of themed encounters. It does not clearly define what the player is trying to complete across the whole game. The README mentions multiple endings, but that is not a scoped, implemented ending design. **[D]**

Do not solve this with a reputation threshold or “complete every recipe.”

Recommendation:

**One primary ending that pays off the accumulated meaning of the room, with small variations or callbacks based on what the player offered.**

Before full production, answer:

- Why does this particular night conclude the game?
- What has the bartender or player come to understand?
- What can the player recognize in the room that they could not recognize at the beginning?

No elaborate branching is necessary. A clear destination is.

The ending need not reveal a giant secret about the bar. That would be a new story decision, not something established by the current implementation. It can instead complete a deliberately bounded sequence of encounters and let the changed space provide closure.

## 18. Tutorial / First 30 Minutes

The current first day teaches four route implementations in succession. That is closer to a feature demonstration than the strongest introduction to the fantasy.

Recommended teaching order:

1. Meet one understandable guest.
2. Learn one complete recipe with visible instructions.
3. Prepare and serve it.
4. Receive a clear response explaining what worked.
5. Meet a second guest whose request invites a small choice.
6. Introduce a second preparation gesture only when the drink calls for it.
7. End a short night with a meaningful change.

Do not teach all four methods immediately.

### Pacing target

| Time | Recommended beat |
| --- | --- |
| First 10 minutes | A complete, understandable encounter and tactile success |
| 30 minutes | A drink choice with a distinct response; first meaningful trace |
| 1 hour | A returning person or changed context |
| 2 hours | Familiar drinks acquire new meanings; the room visibly reflects prior encounters |
| Ending | Pay off those meanings, not a checklist |

These are recommended pacing goals, not descriptions of current playable content or validated timings.

The most important tutorial test is: **Can a new player explain why they served that drink?**

A second test is whether they can distinguish “I prepared that badly” from “I chose a different response than the guest wanted.” If they cannot, the tutorial has taught controls but not the game.

## 19. UI / UX

Functional issues ranked by severity:

| Severity | Issue | Why it matters |
| --- | --- | --- |
| Critical | Patience runs while reading dedicated dialogue | The game punishes the behavior its premise invites |
| High | Locked-recipe learning loop has no adequate clue path | Players cannot confidently progress from a fresh save |
| High | Technique choice precedes drink interpretation | Interface exposes the implementation before the player’s decision |
| High | Emotional ratings lack causal explanation | Players cannot learn from outcomes |
| Medium | Reaction lines are cleared after approximately 1.2 seconds | Authored text can disappear before it is comfortably read |
| Medium | Measurement requires repeated selection, increments, and pouring | Actions outnumber meaningful decisions |
| Medium | Several feedback channels compete | Important narrative information can lose attention |
| Medium | Context and navigation are fragmented | Players must remember the request while moving among panels |
| Lower, before demo | Settings button is unwired; mixed Korean/English operational text; `orderText` is unassigned | Incomplete presentation and unclear affordances |

The current three-ingredient `2 + 1 + 1` preparation can require roughly ten selection/measurement/pour clicks before starting the short skill challenge. **[I]**

That friction is acceptable only if the ritual feels good. Click count alone is not immersion.

### Interaction consistency

Pre-request dialogue uses explicit Next advancement, while served reactions use short automatic delays. That inconsistency matters because both may contain information the player is expected to interpret. **[I/INF]**

Known recipe reference should remain easy to reach, the current guest/request should remain understandable, and opening a reference interface should not silently create a timing penalty.

The inspection confirmed an unassigned `UIManager.orderText`, but this review does not assume that repairing that one field would solve request comprehension. The flow and the request’s meaning matter more than restoring one label.

The old dialogue-bubble configuration reports were not treated as proof of a current missing component. Current serialized bubble references were inspected; the strongest current concern is presentation timing and readability, not an assumed historical configuration failure.

## 20. Feedback Hierarchy

The current stack includes technique grade, result toast, unlock popup, reaction bubbles, hearts, and DayResult. Some appear during the same short interval. **[I]**

Recommended hierarchy:

1. **The glass:** immediate physical outcome.
2. **The guest:** what this offering meant.
3. **A quiet record update:** new recipe or memory.
4. **Night close:** what changed across the night.

System feedback should intervene prominently only when clarification is necessary.

Specific reductions:

- Combine duplicate unlock/name announcements.
- Do not show hearts over a meaningful authored reaction.
- Make text advance consistent or comfortably paced.
- Keep detailed technical diagnosis available without turning every serve into a score report.
- Replace special-case timing delays with one understandable presentation sequence when this flow is revised.

The existing delayed second-route toast is evidence that timing conflicts are already being handled locally. It is not a reason to create a global event bus.

The systems’ responsibilities can remain separate even when their presentation is coordinated. A clean technical separation does not require the player to receive a separate notification from every subsystem.

## 21. Visual / Audio Identity

### Visual

The inspected camera render is still a colored room/counter blockout. It does not demonstrate the snowy, warm, intimate atmosphere promised by the documents. That render does not include the full overlay UI, so it is not treated as a complete gameplay screenshot.

The cocktail/notebook assets show usable motifs:

- Light held inside glass.
- Snow/star imagery.
- A dark, personal notebook.
- Recognizable bartending tools.

However, the inspected art mixes pixel-like work with more painterly cocktail illustration, different background treatment, and inconsistent apparent detail scale. Ingredient icons and customer standing/sitting sprite fields remain unassigned. **[I]**

Highest-leverage visual work:

- One convincing view of the bar.
- Consistent glass/light treatment.
- A small recognizable ingredient set.
- One guest silhouette with readable posture.
- One object that visibly changes meaning after an encounter.

Do not spread art effort across seven guest casts yet.

The U-shaped bar and cold-outside/warm-inside contrast deserve attention because they frame the same place repeatedly. A single excellent recurring view can carry more identity than numerous isolated cocktail illustrations.

### Audio

Unity’s AssetDatabase returned **zero AudioClip assets under Assets**. No authored AnimationClip assets were found either; code-driven motion exists. **[I]**

Atmosphere is therefore a major unproven part of the experience, not a finishing touch.

Priority:

1. Outside wind versus interior quiet.
2. Pour, ice, shaker, spoon, glass placement.
3. A restrained music bed with deliberate silence.
4. One remembered sound associated with a guest or object.

Voice acting is unnecessary for the next proof.

The most valuable audio is repeated, responsive, and expressive of material. A convincing glass placement can improve every encounter; a costly one-off cinematic affects only one beat.

## 22. Content Production Risk

The [roadmap](04_CONTENT_ROADMAP.md) is an ambitious anthology:

- Seven ACTs.
- Eight central literary figures.
- Six revisit outlines.
- Additional supporting characters.
- Persistent props and environmental changes.
- Bespoke arrival, silence, lighting, contract, and reunion presentations.
- A simultaneous two-guest reunion concept.
- Multiple recipe categories and an unresolved ending scope.

The main trap is **bespoke presentation per guest**, not raw dialogue word count.

Other scaling risks:

- Outcome text multiplied across execution, recognition, request, and emotional axes.
- Real-world recipe requirements exceeding current measurement controls.
- One-off tutorial behavior remaining hardcoded as more encounters are added.
- Seven different kinds of grief or choice delivered with similar pacing.
- Revisit conditions based on repeatedly interacting with objects becoming hidden busywork.

### Content burden by area

| Content area | Main production burden | Lower-cost direction |
| --- | --- | --- |
| Cocktails | Illustration, recipe authoring, testing, presentation, descriptions | Small shared-ingredient catalog; consistent visual template |
| Ingredients | Recognition, naming, measurement affordances, icons | Reuse a comprehensible set before adding more bottles |
| Ordinary guests | Avoiding repetitive hints/reactions | Few recognizable regulars with small state changes |
| Special guests | Dialogue, choreography, consequence, later continuity | Complete one arc before introducing another |
| Reactions | Combinatorial coverage across several outcome axes | Author only meaningful distinctions; use clear fallback responses |
| Environmental memories | Art, placement, persistence, interaction, testing | One strong trace per arc rather than many weak collectibles |
| Endings | Macro continuity and payoff | One principal ending with small callbacks |

### Two production decisions need early attention

**Character/asset rights:** no rights-clearance record was found in the reviewed planning documents. The repository’s MIT license does not establish clearance for third-party characters or artwork. The official Little Prince site operates a licensing program, including games; that does not resolve any particular territory or adaptation’s legal status. Obtain qualified, territory-specific clearance before commercial promotion. [Official licensing information](https://www.lepetitprince.com/en/the-phenomenon/).

This is a release-planning concern, not a legal conclusion that a particular use is or is not permitted.

**Tone:** Little Prince is child-coded, while the enforced Manwol recipe contains vodka and coffee liqueur. **[I/INF]** Decide whether that is intentional, whether the offering should be nonalcoholic, or whether an original adult character better serves the game. This is a creative/product question, not a demand to prohibit the content.

## 23. Architecture

**Keep the refactored foundation.**

The following boundaries materially support future design work:

- Ingredient identity separated from recipes.
- Cocktail identity separated from technical preparation.
- Catalog membership/default configuration separated from unlock state.
- Technical matching separated from customer requests.
- Actual served cocktail preserved independently of the requested cocktail.
- Narrow Bar/Bartending context.
- Domain-specific persistence over one raw PlayerPrefs boundary.

The remaining architecture concerns are production-facing:

1. **Encounter control:** reading, waiting, serving, feedback, and leaving do not share a sufficiently reliable player-facing sequence.
2. **Hardcoded tutorial content:** four route-specific behaviors and lines are spread through controllers.
3. **Single-active-customer assumptions:** the planned paired reunion exceeds the current interaction model.
4. **Progress checkpoint policy:** days, unlocks, and the LP flag persist, but partial-night encounter position and reputation do not.

The last item is not a failed persistence refactor. Storage centralization succeeded. The product still needs to decide what quitting midway through a night means.

Do not respond with another broad manager rewrite.

The current one-place structure and narrow technical result boundary are useful constraints. A second meaningful guest encounter may justify a small encounter-data or sequencing addition; it does not automatically justify a general story engine, quest graph, or global singleton.

The inspected persistence structure remains concrete and minimal:

```text
Gameplay domain owner
    ↓
Domain persistence helper
    ↓
SurlhwaPlayerPrefsStore
    ↓
UnityEngine.PlayerPrefs
```

Its job is storage compatibility, not deciding what a guest’s story means. Preserve that separation if meaningful derived narrative progress is added later.

## 24. Overengineering

| System / direction | Verdict | Reason |
| --- | --- | --- |
| Raw store plus three persistence helpers | KEEP | Small, concrete, clear ownership; not excessive |
| Recipe/catalog/unlock separation | KEEP | Already serves matching and multiple UI consumers |
| Four separately maintained, largely similar interaction flows | SIMPLIFY | Maintenance cost exceeds current gameplay differentiation |
| Full special-guest framework before a second meaningful encounter | DEFER | No demonstrated need for a general quest/story engine |
| Dormant reputation-based special-guest selection | DELETE after reference confirmation | Competes conceptually with actual scheduling without contributing behavior |
| Separate Guide and Codex identities | KEEP, simplify shared presentation later if useful | Different player purposes; avoid duplicate content-entry work |
| A huge outcome enum covering every axis combination | DO NOT ADD | Content should determine which distinctions need authored treatment |

The recent refactor is not the principal source of overengineering. **The planned content machinery is the larger risk.**

File length, a static helper, or an interface with a legitimate current use is not sufficient evidence of overengineering. Rewrite recommendations should be tied to player value, duplicated maintenance, or demonstrated authoring friction—not stylistic preference.

## 25. Underengineering

The production-relevant gaps are:

- No safe, explicit treatment of narrative reading time.
- No reliable fresh-save discovery curriculum.
- No authored distinction between important guests’ preferences and generic randomness.
- No proven interaction that remembers a player’s response.
- No content validation checking whether generated requests are fulfillable.
- No established measurement policy for the intended final catalog.
- No public-facing checkpoint/navigation contract.

One inexpensive authoring validation would be particularly valuable: report generated emotional requests for which the available catalog has no full or intended acceptable answer.

That is justified by current data, not hypothetical extensibility.

Likewise, the planned two-person reunion should be treated as a bounded authored encounter requirement. Do not accidentally turn it into simultaneous multi-customer management unless the game truly needs that new challenge.

Simple architecture remains desirable. The warning is against allowing temporary tutorial-specific logic and hidden interaction assumptions to become the unexamined content-production contract.

## 26. Dead / Dormant Systems

| Item | Recommendation | Reason |
| --- | --- | --- |
| Generic `DialogueData` playback | KEEP DORMANT until regular-customer design is settled | Activating it wholesale adds clicks, not necessarily personality |
| `SpecialGuestData[]` / unused `ChooseCustomerPrefab` | DELETE after narrow reference audit | Inactive alternative scheduling model |
| LP empty post-result arrays | ACTIVATE SOON only with authored responses | This is where the next meaningful slice needs content |
| Existing four-line LP introduction | KEEP, revise request clarity as a product decision | Establishes voice but not an interpretable drink contract |
| Historical one-time migration helpers | KEEP as clearly marked Editor/archive tooling | Useful provenance; do not present as everyday content workflow |
| Regression fixture commands | KEEP | Useful for reproducible scenario setup |
| Old unused reputation resolution path | DELETE if reference audit confirms no consumer | Avoid maintaining misleading alternatives |
| Settings affordance | IMPLEMENT minimal navigation before demo, or hide | An inert control is worse than an intentionally absent feature |

No deletions were performed.

Dormant data is not automatically a defect. Its cost is confusion when authors cannot tell which path is authoritative. A small amount of clear classification is more useful than an indiscriminate cleanup campaign.

## 27. Scope Recommendation

### Must Have

- One completed emotional arc with a later acknowledgment.
- A clear final endpoint.
- Learnable recipes and understandable outcomes.
- Reading-safe interaction.
- Reliable progression/checkpoint behavior.
- Coherent bar presentation and essential sound.
- A small, rights-cleared content set.

### Strongly Recommended

- Two or three recognizable regulars.
- Shared ingredients and recipe families.
- A visible memory left in the bar.
- Drink-first or intention-first browsing.
- A concise encounter archive in the Codex.

### Nice to Have

- All four distinct skill interactions.
- Additional literary guests.
- Extra endings.
- Elaborate arrivals and camera effects.
- Many bespoke props or cosmetic variants.

### Cut

- Seven-ACT scope as the initial shipping commitment.
- Mandatory repeated object-clicking to unlock important revisits.
- Routine pressure timers that punish reading.
- Large amounts of interchangeable generic dialogue.
- Separate scoring minigames without distinct decisions.
- Any new generalized story framework built before the next encounter needs it.

The scope decision should protect completion and distinctiveness, not maximize the number of existing systems that survive unchanged.

## 28. Recommended Vertical Slice

This should prove the promise, not enumerate systems.

| Element | Recommended scope |
| --- | --- |
| Nights | 2 |
| Encounters | 6–8 total |
| Special guests | 1, encountered at least twice |
| Ordinary customers | 2 recognizable archetypes |
| Recipes | 4–6 |
| Ingredients | Approximately 6–8, meaningfully shared |
| Bartending | 2 satisfying interactions; optional layering variation |
| Agency | One understandable choice between two drink responses |
| Consequence | Immediate acknowledgment, later callback, one environmental trace |
| Closure | A complete small emotional arc |
| UI | Reliable Guide, request context, readable reactions, simple night close |
| Audio | Wind/interior contrast and tactile bartending sounds |
| Art | One coherent bar view and one convincing guest |

Target: roughly **25–35 minutes**, tested rather than assumed.

Success is not “all routes passed.” It is “players remember what they offered and why it mattered.”

This is intentionally different from the completed technical first-guest slice. The engineering slice proved data and control flow; this proposed slice must prove interpretation, payoff, atmosphere, and desire to continue.

## 29. Recommended Steam Demo

A public demo needs clarity and closure beyond an internal slice.

Recommended:

- **35–50 minutes**.
- Three short nights.
- Eight to ten encounters.
- One complete special-guest mini-arc.
- A brief, rights-cleared second-guest hook.
- Six to eight recipes.
- Two or three polished preparation interactions.
- Later recipes visibly intriguing but not misleadingly unobtainable.
- Functional navigation, audio controls, and a clear progress policy.

End on **recognition or remembrance**, not merely a new silhouette at the door.

The player should leave understanding the full-game promise even if they never purchase it.

### What should remain locked?

Later guest arcs and recipe families can remain unavailable, but the demo must complete its own learning and emotional arc. Do not lock the information necessary to resolve the encounters actually included in the demo.

### Closing hook

The strongest hook is a demonstrated consequence: the player recognizes an earlier offering in a changed line, object, or room detail. A teaser for another visitor can follow, but should not replace that payoff.

The internal vertical slice and public demo should not be treated as identical deliverables. The demo must also survive unassisted first use, unfamiliar hardware/input expectations, and a player’s decision to stop and return.

## 30. Recommended Full-Game Scale

For a constrained student/small-indie production:

| Area | Recommended range |
| --- | --- |
| Playtime | 2.5–4 hours |
| Nights | 8–12 |
| Total encounters | Approximately 32–48, deliberately paced |
| Recipes | 10–16 |
| Shared ingredients | Approximately 8–12 |
| Main special-guest arcs | 3 |
| Encounters per main arc | Usually 3–4 |
| Ordinary archetypes | 2–3 |
| Endings | 1 principal ending with limited tonal callbacks |

These are scope recommendations, not market forecasts. The ranges should be reconciled into one production plan rather than independently maximizing every category.

The roadmap’s local/original cocktail targets should not become mandatory quotas inside this smaller catalog. Preserve the real-cocktail-first direction with a few meaningful local/original exceptions.

Ship a complete small anthology before expanding to all seven ACTs.

Hours should emerge from worthwhile encounters, not an obligation to repeat enough customers to reach a nominal commercial playtime.

## 31. Portfolio Assessment

**The engineering portfolio is currently stronger than the player-facing product portfolio.**

Strong demonstrable material:

- Player-authored composition and pure matching.
- Catalog versus progression ownership.
- Independent mechanical, technical, and request outcomes.
- Additive Bar/Bartending flow.
- Data-driven recipes and UI consumers.
- Backward-compatible persistence centralization.
- Editor migration and regression tooling.

Screenshots will not communicate most of that.

Recommended portfolio material:

| Format | Show |
| --- | --- |
| Short gameplay video | One uninterrupted encounter, including why the drink was chosen |
| GIF | Actual measurement becoming a recognized cocktail |
| Outcome comparison | Same request, different actual drinks, independently handled recognition/reaction |
| Architecture diagram | Bar → narrow bartending context → result → domain outcomes |
| Code example | Matcher or evaluation logic with a small input/output example |
| Tooling example | A reproducible regression fixture and validation report |
| Screenshot pair | The bar before and after a remembered encounter |

Do not lead a recruiter through dozens of refactor phase names. Lead with the player problem, the boundary chosen, and evidence it works.

### What the portfolio can honestly claim now

Unity engineering, gameplay systems, data ownership, UI integration, and tooling are supported by actual implementation. A mature branching narrative system or a fully demonstrated emotional game loop is not yet supported to the same degree. Distinguish completed systems from planned creative outcomes.

A small, compelling player-facing slice would improve the portfolio more than another elaborate architecture diagram on its own.

## 32. Audience / Positioning

The likely audience is people interested in intimate narrative games, reflective atmosphere, and tactile rituals—not primarily demanding bartending simulation or optimization-heavy management. **[INF]**

Conceptually:

| Adjacent experience | Overlap | Expectation risk |
| --- | --- | --- |
| VA-11 Hall-A | Drinks as a way of participating in conversation | Players may expect authored consequences and distinctive recurring characters |
| Coffee Talk | Listening, warm service, intimate atmosphere | Players may expect reading to be unhurried and presentation to carry significant weight |
| Cooking/bartending skill games | Manual preparation and timing | Players may expect deeper technique mastery than current gauges provide |
| Cozy melancholic games | Safety, repetition, place attachment | Hidden deadlines and unexplained ratings can undermine that safety |

These comparisons follow the developers’ own descriptions of [VA-11 Hall-A](https://sukeban.moe/press/sheet.php?p=Cyberpunk_Bartender_Action+VA-11+HALL-A) and [Coffee Talk](https://togeproductions.itch.io/coffee-talk), not a sales forecast.

Differentiate through **snowbound isolation, Korean/literary juxtaposition, tactile drinks, and a room that remembers**.

Do not advertise “deep management” or “realistic bartending” unless those become genuine priorities.

Comparisons can hurt if they imply a level of dialogue density, authored consequence, sound, or visual completeness the demo does not deliver. Positioning should describe the game’s strongest demonstrated experience, not its most ambitious future roadmap.

## 33. Remove Three Things

| Remove / substantially reduce | Why | What is lost | What is gained |
| --- | --- | --- | --- |
| Routine pre-service impatience, especially during reading | Contradicts attentive, unhurried listening and can consume story content | Light time pressure | Trust, accessibility, consistency with the core fantasy |
| Separate mandatory Build and Float grading loops in their current form | They largely test the same release-timing skill | A headline count of four independent minigames | Less repetition and more effort available for satisfying physical interaction |
| Seven ACTs plus all revisits as baseline shipping scope | Breadth is being planned before the core meaningful encounter is proven | Immediate breadth | A realistic chance of finishing a distinctive, polished emotional arc |

These are intentional design reductions, not temporary omissions to restore automatically.

Removing a redundant grading interaction does not require removing the cultural/recipe identity of the preparation method. Reducing the shipping cast does not mean the unused themes have no artistic value.

## 34. Add Three Things

1. **One remembered drink choice.** Two understandable responses, one immediate acknowledgment, and one later callback. Highest value because it proves the game’s central claim.
2. **A learnable discovery chain.** Give a useful clue before an unknown recipe and one informative observation afterward. This turns trial-and-error into learning.
3. **A tactile audio-and-motion pass on two gestures.** Make selecting, pouring, stirring/shaking, and placing a glass feel connected. This improves every repeated encounter without multiplying story content.

None of these requires a general branching engine, a large new content database, or a new persistence format.

## 35. Protect Three Things

1. **The quiet snowy bar as a persistent place.** It gives the project a coherent staging ground and makes small environmental changes emotionally valuable.
2. **The actual drink remains the actual drink.** Preserve the separation between what was requested, what was technically made, and how the guest responds.
3. **The non-punitive, non-savior attitude.** Guests need not become happy, solved, or “corrected.” Some of the existing compassionate reaction writing already supports this.

These elements can survive changes to recipe count, method inputs, guest scheduling, or UI structure. They are closer to identity than any particular prototype implementation.

## 36. Top 10 Problems

Priorities:

- **P0:** threatens the game itself.
- **P1:** should be resolved before major content production.
- **P2:** should be resolved before a public demo.
- **P3:** polish or later work.

Cost estimates are rough effort classes, not schedules: **S** = roughly 1–3 person-days; **M** = several days to two weeks; **L** = multiple weeks, usually including substantial content/testing. These estimates assume limited, focused scope and do not constitute staffing or delivery guarantees.

| Rank | Priority | Problem | Evidence | Why it matters | Recommended change | Estimated implementation/content cost | Risk of changing it | Risk of NOT changing it |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | P0 | Interpretive fantasy lacks a demonstrated remembered consequence | **IMPLEMENTED EVIDENCE / DOCUMENTED DESIGN:** current LP introduction and fixed order do not lead to an authored remembered offering, while the vision centers interpretation and memory | Threatens the reason to play | Build one choice → response → return → trace encounter | M–L: writing, narrow sequencing, one callback and environmental trace | Writing may reveal that the premise needs further revision | Produce a large, polished but emotionally passive game |
| 2 | P1 | Fresh Guide exposes only Stir while tutorial forces other methods | **IMPLEMENTED EVIDENCE:** default catalog unlock, method-filtered unlocked Guide queries, and forced tutorial routes | New players lack an adequate learning route | Guided recipe clues/instruction before forced use | M: instructional content, discovery feedback, fresh-save testing | Discovery may become too explicit if overcorrected | Confusion mistaken for experimentation |
| 3 | P1 | LP patience runs during four-line reading and departure consumes visit | **IMPLEMENTED EVIDENCE:** 10–15-second prefab patience, bartending-only pause, departure-based flag write | Punishes attention and can erase the intended encounter | Reading-safe service timing | S for the narrow safety correction; validation still required | Reduces incidental pressure | Players lose story through normal reading |
| 4 | P1 | Hidden emotional targets lack catalog coverage; LP gets generic random targets | **IMPLEMENTED EVIDENCE:** only five of 30 permitted ordered tag pairs fully match a current cocktail; generic emotion assignment also applies at LP seating | Ratings are hard to interpret or earn | Author important guest preferences; validate generic coverage | M: tags, tutorial alignment, reaction clarity, authoring checks | Requires revising tags and tutorial feedback | Emotional system feels arbitrary |
| 5 | P1 | Build/Float duplicate release timing; composition is already fixed | **IMPLEMENTED EVIDENCE:** hold/release implementations and preview session construction | Low mastery and weak physical credibility | Connect preparation gestures to meaningful control; merge redundant grading | M–L: interaction prototype, feedback, tuning | Can introduce feel/tuning regressions | Repetition persists regardless of recipe count |
| 6 | P1 | Seven-ACT roadmap lacks a bounded shipping ending | **DOCUMENTED DESIGN:** seven ACTs, six revisit outlines, extensive bespoke presentation, no production-defined final scale | Scope expands before the game has a destination | Commit to a small complete arc and one ending | S for the decision; L for delivery | Beloved content gets deferred | Indefinite production |
| 7 | P1 | Disjoint recipes and coarse measurement controls have not proven final-catalog suitability | **IMPLEMENTED EVIDENCE / DOCUMENTED DESIGN:** four disjoint three-ingredient recipes, half/whole-ounce input, planned real-cocktail-first catalog | Content scales poorly and may require re-authoring | Test a shared-ingredient, final-direction mini-catalog first | M: recipe curation, measurement policy, authoring/UI tests | Some prototype recipes/UI must change | Expensive asset and recipe rework later |
| 8 | P1 | Eight-customer nights mostly repeat anonymous service after early content | **IMPLEMENTED EVIDENCE / STRONG INFERENCE:** fixed encounter cap, Customer_A fallback, limited implemented progression after Day 2 | Weak reason to continue to customer #20 | Shorter nights with regulars, changing context, and purposeful encounters | M: encounter pacing and a small amount of distinctive content | Less apparent content volume | Padding dominates the experience |
| 9 | P2 | Rapid reactions and competing toast/popup/hearts obscure meaning | **IMPLEMENTED EVIDENCE / STRONG INFERENCE:** short reaction delays and multiple outcome presentation channels | Players miss the payoff and cannot diagnose outcomes | Establish one feedback hierarchy | M: sequencing/readability work and player testing | Slower presentation if poorly tuned | Writing and system nuance go unread |
| 10 | P2 | Atmosphere remains unproven: blockout bar, mixed art treatment, no AudioClips | **IMPLEMENTED EVIDENCE:** inspected camera view and assets; AudioClip search returned zero | Atmosphere is a core selling point | Finish one representative audiovisual encounter before more assets | M–L: representative environment, sound, guest and UI treatment | Style decisions may require replacing prototypes | Attractive pitch, weak first impression |

This ranking prioritizes threats to the actual game over incidental cleanup. Rights clearance, checkpoint policy, and public-facing navigation remain important release/product gates even where they do not occupy one of these ten rows.

## 37. Top 10 Strengths

Some strengths are implemented; others are genuinely strong documented ideas. They are not interchangeable.

| Rank | Strength | Concrete evidence | Treatment |
| --- | --- | --- | --- |
| 1 | The bar as a memory-bearing space | Vision and roadmap explicitly connect departed guests to persistent props/atmosphere. **[D]** | AMPLIFIED; potential marketing hook once playable |
| 2 | Actual player composition drives recognition | Shelf/session/composition flow, not automatic fulfillment from selected recipe. **[I]** | PRESERVED |
| 3 | Outcomes are not collapsed into one success flag | Mechanical, technical, request, and actual-cocktail reaction remain separate. **[I]** | PRESERVED |
| 4 | A clear alternative to grind-based management | Explicit story-first priorities and rejection of rent/bankruptcy as the main loop. **[D]** | PRESERVED |
| 5 | Compassionate rather than purely punitive reaction writing | Existing reaction database includes responses that allow discomfort rather than demand happiness. **[I]** | AMPLIFIED |
| 6 | Korean and Western literary juxtaposition | Chunhyang/Sim Cheong alongside LP, Hamlet, Faust, and others. **[D]** | AMPLIFIED selectively, subject to rights and scope |
| 7 | Clean catalog/content ownership | Recipes, public cocktail identity, membership, and unlock state have distinct owners. **[I]** | PRESERVED |
| 8 | Minimal persistence architecture | Four existing keys remain behind concrete domain helpers and one raw boundary. **[I]** | PRESERVED |
| 9 | Useful visual motifs already exist | Glass-held light/snow, dark notebook, recognizable bar tools in inspected assets. **[I]** | AMPLIFIED into a consistent style |
| 10 | Reproducible engineering workflow | Editor fixtures, migration checks, and explicit architectural contracts. **[I]** | PRESERVED; portfolio evidence |

Most implemented strengths are enabling systems. They make the next design experiment cheaper; they do not substitute for it.

The marketing hook should be the resulting experience—not the architecture that enables it. Do not advertise a memory-bearing room as a completed feature before it exists in the public build.

## 38. System Scorecard

These score the **current demonstrated state**, except “overall potential,” which explicitly assesses potential. They are judgments, not measured player ratings.

| System | Score / 10 | Current limitation / what separates it from 9 |
| --- | --- | --- |
| Core concept | 8 | Strong memory/place premise; needs a sharper playable distinction |
| Core loop | 4 | Repetition outruns meaningful change |
| Bartending gameplay | 4 | Limited mastery and redundant interactions |
| Recipe system | 5 | Good recognition foundation; weak learning and recipe-space proof |
| Customer system | 3 | Mood variation without sustained individuality |
| Special guests | 3 | Technical differentiation, little meaningful consequence |
| Narrative integration | 2 | Story mostly precedes action rather than responding to it |
| Progression | 3 | Counters and collection, not a developed journey |
| Failure design | 4 | Nuanced internal axes, thin player-facing learning |
| Discovery/unlock | 3 | Registration works; discovery curriculum does not |
| Recipe Guide | 4 | Useful for known recipes, inadequate fresh-save bridge |
| Codex | 4 | Functional collection/reference, little memory value |
| Day structure | 4 | Reliable loop closure, weak dramatic variation |
| UI/UX | 3 | Reading timer, fragmented context, repeated interactions |
| Feedback | 3 | Multiple signals without adequate causal clarity |
| Visual identity | 4 | Promising motifs, inconsistent execution and blockout world |
| Atmosphere | 2 | Central promise not yet demonstrated audiovisually |
| Architecture | 8 | Strong boundaries; encounter sequencing/authoring remain practical gaps |
| Content scalability | 5 | Data scales better than writing, choreography, and learning design |
| Scope feasibility | 4 | Seven-ACT baseline exceeds what the proven slice supports |
| Portfolio value | 8 | Strong engineering evidence; needs one compelling player-facing showcase |
| Overall game potential | 8 | High if interpretation and memory become playable; not a rating of current enjoyment |

A higher architecture score does not compensate for low narrative integration. A score of 7 or 8 is not automatic permission to scale production; the central experience still needs to be demonstrated.

## 39. Highest-Leverage Changes

No more than five:

1. **Prove one remembered drink choice across two encounters.**
2. **Remove reading-related failure and make the request contract explicit.**
3. **Repair fresh-save recipe learning and emotional-feedback fairness.**
4. **Make two preparation interactions satisfying; eliminate redundant grading.**
5. **Finish one coherent audiovisual bar encounter before scaling content.**

Do these before expanding the cast or catalog.

These changes attack the biggest uncertainties: whether interpretation is meaningful, whether a new player can learn, whether repeated actions feel good, and whether the place is worth spending time in.

## 40. Recommended Development Order

```text
NOW
→ BEFORE CONTENT SCALE-UP
→ VERTICAL SLICE
→ DEMO
→ FULL PRODUCTION
→ POLISH
```

### NOW

- Decide whether the core is interpretation or exact-order service.
- Remove accidental reading pressure from the design.
- Write one complete encounter and its return on paper.
- Define the ending of the small version.

**Exit test:** a reviewer can identify the player’s choice and its meaning without being shown code.

### BEFORE CONTENT SCALE-UP

- Build the learnable recipe path.
- Validate emotional request coverage.
- Test representative final-direction recipes and measurement controls.
- Prototype two tactile interactions.
- Resolve character rights and tonal questions.

**Exit test:** fresh players can choose and prepare without developer knowledge.

### VERTICAL SLICE

- Deliver the two-night scope.
- Add one remembered consequence and environmental trace.
- Finish representative sound and art.
- Observe fresh players without coaching.

Ask afterward:

- Why did you choose that drink?
- Why did the guest respond that way?
- What changed because you were here?
- Would you like to see this person again?

**Exit test:** players’ answers demonstrate comprehension and interest, not just successful button use.

### DEMO

- Resolve navigation/checkpoints and readability.
- Add a complete public-facing beginning and ending.
- Test whether people want another encounter, not merely whether they finish the build.

### FULL PRODUCTION

- Expand only the proven encounter pattern.
- Author complete guest arcs before mass asset production.
- Keep a visible scope ceiling.
- Add a new subsystem only when the next authored encounter demonstrates a need for it.

### POLISH

- Animation refinements, additional barks, optional collectibles, visual variants.
- Not foundational audio, legibility, or narrative clarity—those belong earlier.

The roadmap is ordered by reduction of game-design risk, not by ease of coding. More content should follow evidence of a compelling encounter, not function as a substitute for that evidence.

## 41. Decisions the User Must Make

These are unresolved **product** decisions. Existing technical contracts do not need to be reopened merely to answer them.

| Decision | Options | Recommended choice | Reason | What changes depending on the answer |
| --- | --- | --- | --- | --- |
| What is the central challenge? | Exact orders / emotional interpretation / technique mastery | Emotional interpretation, supported by preparation | Best matches the project’s strongest distinctive promise | Requests, rewards, feedback, tutorial emphasis, and marketing |
| Should important requests have one correct drink? | Always / sometimes / never | Sometimes, only when explicitly named | Preserves clarity without making all narrative choices exams | Which encounters use exact identity evaluation versus authored interpretation |
| What should LP’s current request mean? | Explicit Manwol order / open emotional offering | Open offering if retaining current dialogue | Current words invite interpretation, not a named recipe | Multiple defensible responses versus rewriting the request to be explicit |
| Is routine time pressure part of the fantasy? | Timed service / untimed listening / separate challenge mode | Untimed listening; optional clearly separated execution challenge | Resolves conflict with the project’s stated pace | Patience rules, reading safety, accessibility, and tension |
| Are hearts authoritative emotional grades? | Always / only ordinary practice / remove | Restrict or replace for important encounters | Prevents ambiguous human responses from becoming unexplained scores | Reaction presentation, authoring coverage, and player interpretation of success |
| What does a later encounter remember? | Success flag / chosen offering / detailed simulation | Chosen offering or interpretation | Produces agency without branch explosion | Authored callbacks and meaningful derived progression state |
| How large is the first shipping game? | Seven ACTs / three arcs / one expanded arc | Three arcs after a successful one-arc slice | Establishes a finishable baseline | Cast, content budget, playtime, and release scope |
| What is the ending? | Catalog completion / guest resolution / bar-memory culmination | Bar-memory culmination with guest resolution | Gives the entire loop a destination | Final night, encounter ordering, required traces, and closure |
| How literal is bartending realism? | Simulation / simplified authentic recipes / symbolic mixing | Simplified authentic recipes | Fits the real-cocktail direction without demanding a simulator | Measures, ingredient count, terminology, and technique abstraction |
| What does quitting mid-night do? | Restart night / resume encounter position / manual checkpoint | An explicit, simple checkpoint policy suited to short nights | Avoids accidental or misleading continuity expectations | Resume UX and which present-domain state must be retained; not automatically a new save format |
| How is the child-coded LP/alcohol combination handled? | Keep deliberately / nonalcoholic offering / reinterpret character | Make the tonal choice explicit before demo production | Current characterization and enforced alcoholic recipe create a potentially unintended reading | Recipe, characterization, and audience interpretation |
| Which literary characters are cleared for distribution? | Territory-specific adaptations / licensed use / original equivalents | Resolve rights before committing public-demo assets | Avoids building the release hook on unverified clearance assumptions | Character selection, art, text, territories, and marketing |

None of these decisions requires another general-purpose save manager or quest framework. Some may eventually require small implementation changes, but those should follow the product choice and remain narrowly scoped.

## 42. Final Verdict

### Is SURLHWA worth continuing?

**Yes—but continue as a focused game-design experiment, not as full-scale content production.**

The premise has enough identity to justify further work. The implementation has enough usable infrastructure to make that work practical. Neither fact proves the current loop is compelling enough to scale.

### What is its biggest strength?

The idea that encounters remain in a quiet, snowbound bar after the people leave.

### What is its biggest weakness?

The current loop does not yet let the player meaningfully answer a person and recognize the consequence. It mostly evaluates preparation and displays a reaction.

### What single change would improve it most?

**One returning guest who acknowledges the player’s earlier drink choice in a specific, understandable way.**

That would test the central promise more effectively than adding several recipes or characters.

### What should NOT be changed?

The intimate single-bar setting, the non-punitive attitude, and the separation between requested drink, actual drink, technical quality, and customer response.

### What should the team/user work on next?

A two-night, one-guest experience with:

- Safe reading.
- Two understandable drink responses.
- Learnable preparation.
- A specific reaction.
- A later acknowledgment.
- One lasting trace in the room.

**The refactor has made SURLHWA easier to develop. The next milestone must prove why it deserves to be played.**

---

End of complete review. All recommendations remain unimplemented proposals. This export does not amend ADRs, authorize follow-up work, or claim fresh runtime/playtest validation.
