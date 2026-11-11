# P09 UI and Codex Refinement Implementation Plan

> **For agentic workers:** Use `superpowers:executing-plans` to implement the checked tasks after the root releases the current ACT7 source freeze. This document authorizes no concurrent Editor, Player or build process. Root coordinates execution, evidence and commits.

**Goal:** Make the nine-recipe Codex a visibly crafted, readable bar journal and give the existing campaign UI a consistent reading and interaction treatment, without changing what the player has learned or how the game progresses.

**Architecture:** Keep `CampaignUi` as the campaign presentation owner, `RecipeKnowledge.Project` as the disclosure owner, and existing controller/modal/input owners. Compose the journal from the current uGUI/TMP panels, text and buttons. Correct the shared pointer-hold boundary in `SliceHoldButton`; use small private presentation helpers, not a new UI framework.

**Tech stack:** Unity `6000.3.7f1`, existing uGUI, TextMeshPro, Input System and Korean font assets. No new packages, toolkit, input system or raster-generation dependency.

**Spec:** [Production Expansion Plan, P09](PRODUCTION_EXPANSION_PLAN.md#p09-dedicated-ui-and-codex-refinement), [P09/P11 audit](P09_P11_PREIMPLEMENTATION_AUDIT.md), [presentation debt PD01/PD02](PRESENTATION_DEBT.md), [owner approval policy](OWNER_APPROVAL_POLICY.md), [Q1 confirmation](Q1_OWNER_CONFIRMATION.md) and [Gate2 approval](GATE2_OWNER_APPROVAL.md).

**Status, 2026-11-11:** PLAN ONLY. Baseline inspected at `ac3aaf3` with root-owned ACT7 changes and inherited owner changes present. Only this new document is written for this task. No P09 implementation, tests or new Player evidence are claimed. Start implementation after the ACT7 checkpoint and a new root inventory; do not treat this older Git ID as the future build identity.

## Authority and boundaries

- Project Vision remains the product authority: Story First, quiet legibility, interpretation rather than answer keys, and memories accumulating in a familiar bar.
- Final Canon `14_UI_UX_DIRECTION.md`, UI-01 through UI-05, owns readable surfaces, projections, modal/focus ownership and input consumption. `22_RECIPE_KNOWLEDGE_AND_DISCOVERY.md`, KNOW-002/006, owns learned-field disclosure; `71_DATA_AUTHORITY_PRINCIPLES.md`, DATA-02/06/07/09/10, separates recipe truth, knowledge, presentation and stable identity. Source root: `C:/Users/surlw/.codex/.chatgpt-projects/g-p-6a8cfbb439748191ac8fac094112d31b/SURLHWA_CANON/`. Use the committed authority versions already pinned by Production, not an unreviewed replacement.
- Preserve approved Q1 camera endpoints, pixel-first warm winter palette, character scale/contacts, room composition and the accepted Prince pose. UI work does not reopen C1, HG04 or the spatial/art direction.
- Keep the existing nine recipes, exact ingredient IDs/amounts, authored Korean story text, knowledge receipts, supply rules, save schema/namespaces and ending boundaries. No automatic knowledge, supply-derived knowledge, answer-ranking UI or all-known progression gate.
- Unknown hides unsupported facts. Partial exposes only its learned fields, including a learned method or preparation/garnish condition. Glassware appears only when `projection.Known`; the current ACT6 fix remains intact.
- No search/filter system, recipe progress meter, new screen, extra menu, collection reward, tooltips, quest layer or replacement input navigation. Existing shelf pagination may adapt to readable row height; recipe count stays nine.
- P10 art, P11 character/tool motion, focus reveal and shimmer, and P12 audio remain separate packages. P09 may verify these existing surfaces are not obscured; it does not redesign them.
- No edits to accepted ACT1-6 content bytes, Canon/Architecture/RC1 sources, owner HTML or ProjectSettings. Keep inherited changes. Do not reset, clean, stash, restore or discard them.
- PD01/PD02 remain **OPEN / HUMAN_SIGNOFF_REQUIRED** for the named Gate3 ruling. Objective usability and reviewable captures permit autonomous continuation; this is not a new owner approval gate.

## Existing evidence and the selected visual proposal

This develops the existing audit into implementation work; it does not repeat its investigation. The following retained ACT6 build02 images were opened with `view_image` at their actual resolution. They are actual Player captures driven by simulated production UI/Input System input, not OS-native certification. They are comparison baselines, not P09 results.

| State / size | Retained image | Visible baseline relevant to P09 |
| --- | --- | --- |
| Unknown / 1280x720 | [Last Word before facts](Evidence/act6-player-early-discovery-720-02/006-codex-last-word-unknown.png) | Generic page name and empty facts are protected; large blank blocks separate the two unknown notices. Nine rows fit above the footer. |
| Partial / 1280x720 | [Last Word learned clues](Evidence/act6-player-open-720-02/033-codex-last-word-clues.png) | Learned four amounts and preparation render without glassware. Quantities follow variable-length ingredient names rather than a shared column. |
| Known / 1280x720 | [Last Word discovered](Evidence/act6-player-open-720-02/063-codex-known-shake-recipe_last_word.png) | Glassware now appears correctly. Preparation, ice and garnish share one undifferentiated text block. |
| Partial / 1920x1080 | [Last Word learned clues](Evidence/act6-player-quiet-retry-mismatch-1080-02/033-codex-last-word-clues.png) | The same hierarchy scales up; the muted page state and dark selected row remain visually restrained. Close has the visible focus outline. |
| Known / 1920x1080 | [French 75 discovered](Evidence/act6-player-quiet-retry-mismatch-1080-02/065-codex-known-shake-recipe_french_75.png) | Real long Korean finish instructions fit this default view, but method, steps, glass and garnish need clearer grouping. No clipping failure is inferred for unviewed settings combinations. |

These are the corrected ACT6 images described in [ACT6 runtime checkpoint](ACT6_RUNTIME_CHECKPOINT.md). The earlier Partial glassware leak is historical and fixed at `3c25ba0`; do not present its correction as new P09 work.

### Journal composition

Retain the current dark paper, warm ink and dull brass: `paper=(.075,.068,.061)`, `ink=(.93,.86,.73)`, `muted=(.70,.68,.62)`, `gold=(.72,.53,.29)`. Opaque reading surfaces keep the winter room behind them quiet. Use the current Korean font, fine inset rules and measured spacing to suggest a worn bar record; no stock parchment texture, new display typeface or bright website-style cards.

The visible redesign is a journal spread, not only a font-size or color patch:

1. **A calm header and index.** Retain `한 잔의 기록`. Make the nine entries a restrained left index with a short non-raycast rule marking the displayed entry. Preserve the independent `SliceFocusOutline` for the current keyboard target; a page can remain selected while focus moves to Close. Unknown entries retain generic labels. No recipe number or name becomes a discovery hint.
2. **A clear page heading.** Give the projected name first emphasis, then existing learned/Known state and projected method. Keep a method-only Partial page visibly distinct from an untouched page without revealing its name.
3. **Aligned ingredients and quantities.** Use one label column and one aligned amount column, retaining exact `0.##` decimal formatting, `ml`, learned-only amounts and `양 미상`. Keep a learned Dash annotation attached to the relevant ingredient. Do not sort/reclassify ingredient identity for visual convenience.
4. **Readable preparation and finish.** Separate `준비` from `마무리` with space and a quiet rule. Preparation uses the existing recipe method/finish/layer semantics behind `PreparationVisible`; garnish uses `GarnishVisible`. Glassware is a separate Known-only metadata line, read from `RecipePresentation`. No status or section reveals an unlearned fact.
5. **A protected footer.** Keep the existing quiet footer sentence and Close action in their own band. Decorative paper edges and rules never receive raycasts. Unknown pages use intentional empty space and the existing notices; they do not display a silhouette or exemplar drink.

Start within the current card footprint `(174,54,932,604)` on the `1280x720` reference canvas. Allocate approximately 246 px to the index, a narrow gutter, and the remaining approximately 580 px to the recipe. Reserve the bottom 64 px for footer/Close. Nine index rows can use 40 px height and 5 px gaps within a 400 px band. Ingredient rows align vertically; the preparation/finish groups use their measured text height rather than the current fixed two large blocks. Keep the modal within the same safe screen margin. These are bounded first-pass dimensions, to be adjusted from rendered text bounds rather than treated as visual approval.

Use a small set of reading roles: page/modal heading, body, supporting label and control. Initial campaign targets at reference resolution are headings 27-28 / 30 with LargeText, body 20-21 / 23, supporting labels 16 / 18 and controls 16-18 / 18-20. Preserve existing body sizing where it already meets the contract. Do not make everything the same size, shrink essential text to force a fit, or silently clip text. Split the existing Guide text into two quiet columns of existing method groups if its large setting needs more room; preserve its wording and current control availability.

### Broader UI consistency

Apply the same text roles, foreground/background contrast, spacing and focus treatment to dialogue/speaker, request, Guide, result/discovery/retry, settings/reset, save error/recovery, welcome/Continue, ACT7 ending/coda and method/shelf controls. Retain the paired-dialogue safe rectangle and readable tool/guest areas. Keep narrative lines and result semantics unchanged.

HighContrast must reach these surfaces, including supporting text and button labels, without making disabled controls appear enabled. Retain non-color focus/selection and a clear unavailable state. LargeText must reach actual reading/control labels rather than only the dialogue body and two Codex blocks. Use existing shelf pagination with fewer, taller rows if long labels require two lines; do not change physical supply or global ingredient hotkey identities. Check every supplied item remains reachable. Settings changes must update currently visible UI and rebuilt modals without resetting the reading cursor or selected recipe.

## Planned file map

Paths below are relative to the repository root. These are future changes, not changes made by this plan.

| File | Bounded responsibility |
| --- | --- |
| `Production/Unity/Assets/Surlhwa/Presentation/CampaignUi.cs` | Journal composition, safe label formatting, metadata display, recipe focus restoration and consistent campaign reading styles/layout. Keep the existing public controller-facing API. |
| `Production/Unity/Assets/Surlhwa/Presentation/SliceUi.cs` | Fix `SliceHoldButton` once for both UIs. Preserve Slice's accepted layout and `SliceFocusOutline`; do not port the nine-recipe design into the historical Slice. |
| New `Production/Unity/Assets/Surlhwa/Tests/EditorIntegration/P09UiTests.cs` and `.meta` | Rendered projection, label/metadata, focus, reading settings and geometry tests using the current campaign fixtures/font patterns. |
| New `Production/Unity/Assets/Surlhwa/Tests/EditorIntegration/SliceHoldButtonTests.cs` and `.meta` | Actual uGUI pointer-event dispatch and cancellation coverage for the shared control. |
| Existing `ActSixUiTests.cs`, `ActFourUiTests.cs`, `CampaignPresentationTests.cs` under that test directory | Preserve disclosure/reachability assertions; update only necessary layout lookups and extend authored request/text coverage through ACT7. Existing focus/input tests remain regression evidence. |
| `Production/Unity/Assets/Surlhwa/Presentation/CampaignPlayerProof.cs` | Add only the P09 capture/interaction assertions needed at existing real reading/service states. Preserve current ACT proof modes and successful route behavior. |
| `Production/Tools/run_campaign_proof.ps1`, `verify_campaign_proof.py`, associated focused Python tests | Only if needed to select/verify the bounded P09 capture pass through the existing harness. Keep simulated evidence labeled as such and retain historical proof compatibility. |
| New `Production/Validation/P09_RUNTIME_CHECKPOINT.md`, fresh `Evidence/p09-*` folders, append-only current records in `PRESENTATION_DEBT.md` / expansion checklist | Record exact candidate identity, RED/GREEN, captures, input provenance, limitations and Gate3 review scope. Never overwrite retained ACT6 or ACT7 evidence. |

`RecipeKnowledge.cs`, session/domain/save code and recipe/content JSON are read-only authorities for P09. Private helpers belong in `CampaignUi` initially. Extract a small shared style helper only if both existing UIs actually need the same changed logic; no theme registry, data-driven screen framework or new dependency is planned. The hold component is already shared and needs no new abstraction.

## Review focus

| Failure mode | Required behavior / owning task |
| --- | --- |
| Method-only or preparation-only Partial has no ingredient/name fact | The index acknowledges learned facts without filling hidden fields; Task 1. |
| A pointer hold handler survives disabled/noninteractable sibling Button | No new hold starts; disabling during a hold releases safely; Task 1. |
| Page/settings rebuild destroys the focused object | The selected recipe is restored by stable ID when appropriate; otherwise existing contextual fallback remains inside the top modal; Tasks 2/3. |
| Large Korean text plus four quantities and long finish instructions at 720p | Essential glyphs/controls stay within their surfaces, separated from footer and guests/tools; Tasks 2/3/4. |
| Modal opened during held input, save/Continue or terminal ending | No accepted flow, duplicated reading/progression or resumed hold without release/rearm; Task 4. |

## Task 1 — Safe learned labels and shared hold availability

**Files:** `CampaignUi.cs`, `SliceUi.cs`, new `P09UiTests.cs`, new `SliceHoldButtonTests.cs`.

**Interfaces:** Consume `RecipeKnowledge.Project(string recipeId)` and existing `KnowledgeProjection` fields. A private `RecipeLabel(KnowledgeProjection projection)` returns the projected name, otherwise `들었던 한 잔` when any projected fact is visible, otherwise `아직 빈 페이지`. Do not add another knowledge-state owner. Preserve `SliceHoldButton.Down` / `Up` and its existing pointer interfaces.

- [ ] Add `MethodOnlyPartialAcknowledgesClueWithoutRevealingName`: consume the real first Dry Martini method-grant line (`text_rc1_0323_act1_edit`) through a session, render its page, and assert learned `Stir`, generic Partial index label, no Dry Martini identity, ingredients, amounts or glassware. Cover preparation/garnish-only projection fixtures separately and label them UI fixtures.
- [ ] Add real `ExecuteEvents` pointer-down cases for `Button.enabled=false`, `interactable=false`, inactive object/ancestor and disallowed mouse button, plus the enabled left-button positive case. Assert whether `Down` fires and whether the adapter accepts flow; do not merely call the Button's `onClick`.
- [ ] Add press-then-disable, pointer-up-after-disable, component/object disable and focus/modal interruption cases. Assert `Up` cleanup stops the active tool and held input does not rearm by itself. Button component disable may not call the sibling handler's `OnDisable`; reproduce that boundary rather than assume it does.
- [ ] Run focused Unity RED after the root releases the freeze. Retain actual failure counts/logs. Expected regression failures are the method-only label and enabled hold handler on disabled/noninteractable Button; already-correct cases may pass.
- [ ] Implement the label predicate using the projection only. Guard shared hold start with sibling Button active/enabled/interactable availability. If the held-then-disabled RED requires it, track only the active hold and cancel through existing `Up` when the sibling becomes unavailable. Preserve unconditional safe pointer-up/disable release; no parallel input owner.
- [ ] Run focused GREEN and `SliceFocusTests` / affected `CampaignInputTests`. Verify both Slice and Campaign callers use the fixed component. Root inspects the bounded diff and may commit `fix: respect learned clues and hold availability` after required checks; no push.

## Task 2 — Crafted journal and authoritative metadata

**Files:** `CampaignUi.cs`, `P09UiTests.cs`, necessary existing UI test lookups.

**Interfaces:** Keep `recipePage` and existing `Recipe page <index>` proof bindings unless a concrete change requires caller updates. Extend private `BuildModal(string kind, string preferredRecipeId = null)` to restore a requested available recipe control. A small `Book` return/local ID-to-button mapping supplies that control; page-click callbacks pass the actual recipe ID, not its label or array position as identity. Initial opening and unavailable targets retain existing fallback behavior.

- [ ] Add rendered `UnknownPartialKnownExposeOnlyPermittedFields` cases covering identity-only, method-only, ingredient-only, amount-with-ingredient, preparation and garnish. Use the actual `KnowledgeProjection`; assert absence of hidden text, not only presence of expected labels. Retain ACT6 French/Last Word glassware guards.
- [ ] Add `KnownGlasswareUsesContentMetadata`: render Known recipes across `old_fashioned`, `cocktail`, `tumbler`, `flute`. In an explicitly labeled in-memory UI fixture, change metadata for the same recipe/finish and assert the displayed glass changes; Unknown/Partial still hide it. This prevents a recipe-ID or Shake-finish answer table from masquerading as data authority.
- [ ] Add `SelectingRecipeKeepsThatRecipeFocusedAfterRebuild` using real pointer/keyboard activation, then test disabled/destroyed preferred target and modal confinement. Assert selected-entry mark and current-focus outline are separate non-raycast visuals.
- [ ] Run focused RED. Implement the journal composition described above, exact quantity columns, measured preparation/finish groups, and safe focus restoration. Map only supported glassware keys to Korean display labels; read the key from `Content.RecipePresentation[recipeId].Glassware` behind `projection.Known`.
- [ ] Keep preparation/required garnish sourced from recipe truth behind their existing learned guards. Where optional/no garnish wording is composed, use existing `RecipePresentation.OptionalGarnish` and required-garnish data rather than guessing from a recipe ID or `ShakeFinish`. Do not change discovery/preparation requirements or content metadata to match UI copy. Preserve the distinction between optional garnish and no authored garnish; test both under `GarnishVisible`.
- [ ] Run GREEN for all nine pages and existing ACT4/ACT6 disclosure/layout tests. Inspect the first current-candidate Unknown, Partial and longest Known card internally at 720p before broadening the treatment. Root may commit `feat: refine the campaign recipe journal` after review; visual approval remains open.

## Task 3 — Consistent reading styles across the existing UI

**Files:** `CampaignUi.cs`, `P09UiTests.cs`, `CampaignPresentationTests.cs`.

**Interfaces:** Continue consuming `CampaignController.LargeText`, `.HighContrast` and existing `ChangeSettings`. Use private style/size helpers in `CampaignUi.Text` / `Button` and refresh existing references when settings change. Do not create a settings schema or persist view-only recipe selection.

- [ ] Add parameterized checks for default, LargeText, HighContrast and both settings. Cover request, Guide, settings/recovery/error, Codex and controls in addition to the already-styled dialogue. Assert size/contrast changes on the intended text and stable availability, not exact implementation helper calls.
- [ ] Render all adopted request summaries/dialogue lengths through the actual TMP font at both viewport sizes. Include paired speakers, long ingredient names, French 75 finish, Last Word decimal amounts, discovery/result/retry and ACT7 ending/coda. Force mesh updates and check glyph/text bounds and essential-control overlap; raw RectTransform dimensions alone are insufficient.
- [ ] Run focused RED for missing settings coverage. Apply the role treatment and only the layout changes necessary for readable real content. Guide may use two method columns; shelf may reuse existing paging with taller/wrapped labels. Keep global ingredient hotkeys and selected ingredient semantics intact. Do not truncate essential copy or hide a control to make the test pass.
- [ ] Retain opaque reading backgrounds. Compute/check text contrast against actual composed surfaces, including selected, focused and disabled states; record the measured values and inspect the images. Use a local objective floor of 4.5:1 for essential text, without claiming a complete accessibility certification. Non-color focus and distinct disabled behavior remain required regardless of that number.
- [ ] Verify toggling each setting updates the current modal and the underlying screen upon closing; selected page and modal confinement survive rebuild. Saved reading preferences survive quit/Continue and New Game according to the existing settings contract, without mutating campaign progress merely by opening a modal.
- [ ] Run GREEN plus affected focus/input/UI regressions. Root reviews safe areas in solo and paired scenes and may commit `feat: unify campaign reading styles` after required validation.

## Task 4 — Current Player, native interaction and lifecycle evidence

**Files:** minimal existing proof harness extensions, new P09 checkpoint/evidence records. Root owns every Unity/build/Player/native-input session.

- [ ] At implementation start, record Git status/log, current source/content hashes and inherited differences. At each root-approved freeze, record a fresh candidate inventory before Editor/build; no source or serialized changes while that identity is being tested. Keep Editor process exit, test XML and build exit evidence distinct.
- [ ] Add bounded capture points to the existing production-input proof at actual reachable states: fresh Unknown; first consumed Dry method clue; French/Last Word Partial before successful discovery; Known after actual service/discovery. Do not call `SeedKnown`, alter save JSON or fabricate receipts in a Player proof. UI fixtures remain Editor-only and clearly labeled. Retained earned checkpoint copies are allowed only after normal loader validation, with provenance and isolated output directories.
- [ ] Run the matrix below at 1280x720 and 1920x1080 on the new candidate. Inspect actual images with `view_image`; record observations and any corrected defects. Compare like-for-like states with the retained ACT6 images above. Do not reuse old images as evidence that the new UI passes.
- [ ] Exercise changed pointer/keyboard controls in the actual Player, then perform targeted OS-native input on the same identified executable/window. Record executable/source identity, PID/HWND, route/checkpoint provenance, actions, screenshots/video and observed state. Label simulated production input separately from native evidence. An unavailable native tool is `VALIDATION_DEBT`, not a PASS.
- [ ] Open/close Codex, Guide and settings while brewing and reading; hold then open modal/lose focus/disable the relevant control; release/regrip. Confirm accepted quantity, active tool and audio loop stop, current line/phase/checkpoint stay protected, and no release is converted into a click or second advance. Verify reset/back/Continue, nested recovery/error surfaces, destroyed focused control and clean teardown/reopen.
- [ ] Re-run affected retained regression suites and the normal root verification policy once after final code. Do not rerun unrelated routes repeatedly without a new failure/change. Append exact evidence and remaining debts, inspect serialized diffs, and commit only the bounded P09 candidate after review.

### Required matrix

| Surface / boundary | Editor/domain checks | Actual Player evidence at both sizes |
| --- | --- | --- |
| Unknown and method-only Partial | No identity/ingredients/amounts/finish leak; any learned fact changes the generic row state | Fresh Unknown and real consumed Dry method line; default + LargeText + HighContrast + combined captures |
| Rich Partial | Exact learned French/Last Word facts; no glassware; reading never grants Known | Pre-discovery Partial with preparation, decimal quantities and garnish condition; all four setting combinations |
| Known nine-recipe journal | All nine pages, quantities, required/optional/no garnish, metadata-driven glass; no footer overlap | All nine earned Known pages at both sizes; default and largest combined setting; separate HighContrast/large snapshots of the longest pages |
| Page/index/close navigation | Pointer activation, arrow navigation, Enter once, stable-ID focus after rebuild, destroyed/disabled fallback | Pointer and keyboard page changes, selected-entry marker versus focus outline, return to original context |
| Dialogue / request / result / discovery / retry / ending | All adopted text bounds, paired safe areas, no hidden result names, no duplicated consumption | Representative longest solo/paired lines, Brew request, discovery/result/retry, Ready/reading/coda with changed settings |
| Guide / settings / welcome / Continue / reset / recovery / save error | Visible text style coverage, confined controls, settings persistence, save isolation | Reachable normal screens; real isolated recovery/error exercise for exceptional screens, labeled by how the fault was introduced |
| Supply and four-method controls | Every supplied item reachable, unavailable items remain unavailable, hotkeys unchanged, no truncated essential labels | Full late shelf pages and each method panel, normal/focused view, pointer and keyboard in both reading sizes |
| Hold/modal/focus lifecycle | Actual pointer dispatch cannot bypass Button availability; safe release and rearm | Changed holds, modal interruption, focus loss, release/regrip, scene/menu/Continue lifecycle; no accepted flow or loop while blocked |

For each actual state capture, store the page recipe ID, visible projection state, reading settings, resolution and checkpoint identity in evidence alongside the image. Essential Korean must render without replacement glyphs; check text/controls against their own surface and room/actor/tool safe areas. Font/glyph tests, bounds checks and screenshots complement one another; none alone certifies subjective readability or visual quality.

### Commands and evidence discipline

Root runs the existing wrapper from the repository root, with an unused evidence name for every attempt. Example focused RED/GREEN invocations, to be executed only after implementation begins and the process lock is clear:

```powershell
& ./Production/Tools/run_unity.ps1 -Operation Tests -EvidenceName 'p09-ui-red-01' -TestFilter 'Surlhwa.SpatialTests.P09UiTests|Surlhwa.SpatialTests.SliceHoldButtonTests'
& ./Production/Tools/run_unity.ps1 -Operation Tests -EvidenceName 'p09-ui-green-01' -TestFilter 'Surlhwa.SpatialTests.P09UiTests|Surlhwa.SpatialTests.SliceHoldButtonTests|Surlhwa.SpatialTests.ActSixUiTests|Surlhwa.SpatialTests.ActFourUiTests|Surlhwa.SpatialTests.SliceFocusTests|Surlhwa.SpatialTests.CampaignPresentationTests|Surlhwa.SpatialTests.CampaignInputTests'
```

Use the actual namespace/class filters adopted in the tests. Check XML totals and Editor exit, not only wrapper output or compiler success. Retain `p09-ui-red-*`, `p09-ui-green-*`, `p09-player-720-*`, `p09-player-1080-*`, `p09-native-*` and an inventory/checkpoint record. Existing `run_campaign_proof.ps1` rejects reused output directories and accepts only an actually published checkpoint for Continue; preserve that behavior. Any added proof-script branches need focused Python RED/GREEN without weakening old proof assertions. Build the final candidate only after the affected checks pass and no active Player remains.

## Completion record and remaining scope

P09's technical exit requires the two audited defects corrected, authoritative safe journal content, page focus recovery, consistent settings coverage, clean affected regressions, and current rendered/input/lifecycle evidence with explicit limitations. An implementation defect stays open until fixed; it cannot be relabeled aesthetic debt. A blocked native check stays visible as validation debt rather than being inferred from simulated input.

Report changed files, actual RED/GREEN counts, current candidate inventory, screenshot/native evidence paths and any unfinished objective checks in `P09_RUNTIME_CHECKPOINT.md`. Update the current P09 roadmap record without erasing historical audit findings. Final card composition, material treatment, typography/readability preference, interaction feel and broader UI harmony remain PD01/PD02 **OPEN / HUMAN_SIGNOFF_REQUIRED** for the bundled Gate3 review. This plan neither implements nor approves them.
