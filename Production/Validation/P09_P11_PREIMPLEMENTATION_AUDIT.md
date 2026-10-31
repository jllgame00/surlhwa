# P09 / P11 preimplementation audit

Later implementation update: ACT6 checkpoint `3c25ba0` corrects the glassware
finding below. Two expected RED cases, nine affected UI GREEN cases and eight
current-identity Player proofs are retained in ACT6_RUNTIME_CHECKPOINT.md. The
original audit's in-progress wording below is historical. Other findings remain
for the scheduled pass after ACT7.

During the redesign, render supported Known glassware from the existing
`CampaignContent.RecipePresentation[recipeId].Glassware` metadata. Current
French75/LastWord labels are correct for their adopted definitions but embedded
in preparation-specific strings; use the existing metadata authority when
composing the final card. Keep the new Known disclosure guard intact.

Date: 2026-10-31. Read-only source audit performed while ACT6 Player proofs were
running. This records findings and a bounded final-pass proposal, not implemented
changes or runtime acceptance. No Unity, Player, aggregate test, pointer-event
reproduction, or new rendered-motion inspection was performed for this audit.

The glassware disclosure correction is **root-owned ACT6 work in progress**.
All other items below belong to the planned pass **after ACT7 and the common
ending**. Do not infer completion from this document or modify inventoried Player
sources during a proof run.

## Authority and preserved boundaries

- [Production Expansion Plan](PRODUCTION_EXPANSION_PLAN.md), P09 and P11:
  refine the existing UI/knowledge projection and presentation owners; verify
  readable Korean, navigation, disclosure, cancellation, contacts and changed
  native controls. No replacement UI/input/animation framework is required.
- [Presentation debt](PRESENTATION_DEBT.md), current Gate2 closure and
  PD01/PD02/PD05/PD11: implementation and functional checks do not grant final
  visual, motion or tactile approval. Named subjective scope remains for Gate3.
- [Q1 owner confirmation](Q1_OWNER_CONFIRMATION.md) and
  [Gate2 owner approval](GATE2_OWNER_APPROVAL.md): preserve the accepted Q1
  spatial direction and Prince seated pose. Directional transitions, oblique
  pixel shimmer and focused bartending transitions remain final-pass work.
- Final Canon `22_RECIPE_KNOWLEDGE_AND_DISCOVERY.md`, KNOW-002 and KNOW-006:
  Partial displays authored learned facts; glassware is outside its pre-Known
  disclosure fields. Immutable source:
  `C:/Users/surlw/.codex/.chatgpt-projects/g-p-6a8cfbb439748191ac8fac094112d31b/SURLHWA_CANON/22_RECIPE_KNOWLEDGE_AND_DISCOVERY.md`.

Keep ACT1 gameplay, stable IDs, source text, knowledge and physical-supply
boundaries, save behavior, Q1 camera endpoints, character scale, seat/foot
anchors and authoritative input projection intact. Preserve pixel-art-first
presentation, selective depth and the accepted room composition. No free camera,
global blur, PBR shift, new progression gate or routine owner approval is proposed.

## Objective findings and UI coverage

| Finding | Exact implementation/source evidence | Disposition and smallest action |
| --- | --- | --- |
| French75 and LastWord Partial preparation reveals glassware before Known | [CampaignUi.cs](../Unity/Assets/Surlhwa/Presentation/CampaignUi.cs), `Book`: at audit time, the `PreparationVisible` finish branches included `플루트 잔` / `칵테일 잔`. [RecipeKnowledge.cs](../Unity/Assets/Surlhwa/Domain/RecipeKnowledge.cs), `Project`, separates Known from individual facts. Canon22 KNOW-002/006 applies. Lesson source `SRC-RC1-0037` / `0057` teaches transfer/preparation and garnish, not those glass identities. | **Objective disclosure defect; root-owned ACT6 fix in progress.** Gate glass metadata on `projection.Known`, retain the taught U/Strain/preparation instructions, and verify Partial/Known separately. This audit does not certify the later fix. |
| A method-only learned recipe is still labelled an empty page | `CampaignUi.Book` chooses the unnamed row label only from `projection.Ingredients.Count`. [act1.json](../Content/Campaign/act1.json), `enc_rc1_01.intro`, grants `recipe_dry_martini` its method fact through `text_rc1_0323_act1_edit` / `SRC-RC1-0323`, before any ingredient fact. | **Objective UI inconsistency, source-confirmed; not runtime-reproduced here.** Distinguish heard/empty using all already-visible projection fields. Keep the undisclosed recipe name and amounts hidden. Implement after ACT7. |
| Pointer hold does not honor the sibling Button's availability | [SliceUi.cs](../Unity/Assets/Surlhwa/Presentation/SliceUi.cs), `SliceHoldButton.OnPointerDown`, invokes `Down` without checking `Button.isActiveAndEnabled` or `IsInteractable`. Installed uGUI `com.unity.ugui@11343a6274f3/Runtime/UGUI/EventSystem/ExecuteEvents.cs`, `GetEventList` / `ShouldSendToComponent`, dispatches each active handler independently. A disabled Button does not disable the separate hold handler. | **Objective input-contract defect established by source inspection; no event/runtime reproduction in this audit.** First reproduce disabled and noninteractable Button cases with actual pointer dispatch; then guard hold start and preserve release/cancel cleanup. Shared Slice behavior needs a focused regression. Implement after ACT7. |
| Reading settings have uneven coverage | `CampaignUi.Refresh` applies `LargeText` and `HighContrast` to dialogue body. `Book` applies large text to amounts/preparation. Guide, requests, headings and most controls retain their fixed styles; high contrast does not apply to those surfaces. | **Final-pass coverage gap, not a demonstrated clipping/contrast failure.** Use a small reading-style helper inside the existing UI and inspect wrapping before expanding the settings' effective scope. |
| Choosing a Codex page moves focus to Close | `CampaignUi.Book` rebuilds the modal after selecting a recipe; `BuildModal` always selects the close control. Existing `RecoverFocus`, `RestoreFocus` and explicit modal navigation provide availability/confinement fallbacks. | **Interaction-polish candidate, not loss of all navigation.** Restore the chosen recipe control by stable recipe ID after rebuilding, with the existing safe fallback when unavailable. |

Existing strengths to preserve: `SliceFocusOutline` provides a non-color marker
and nonblocking decoration; modal navigation remains confined; keyboard activation
checks component availability; Codex content comes from `KnowledgeProjection`.
The audit did not find a reason to replace these owners.

## Motion, focus and tactile candidates

These are implementation observations and review candidates. They are not claims
that a newly inspected Player clip exhibited a defect.

- **Directional travel and seating.**
  [CharacterView.cs](../Unity/Assets/Surlhwa/Presentation/CharacterView.cs),
  `Show` / `ShowWalkFrame`, selects standing, seated or walking surfaces.
  [CampaignController.cs](../Unity/Assets/Surlhwa/Presentation/CampaignController.cs),
  `AnimateGuest`, reverses the travel path for departure while using the same
  walk-frame set; standing/seating switches are discrete. Add optional direction
  and turn transitions within these presentation owners, retaining existing
  static poses and missing-frame fallback. Do not reopen the accepted Prince
  seated pose merely to produce another variation.
- **Listening, reaction and paired restraint.**
  [CampaignCharacterArt.cs](../Unity/Assets/Surlhwa/Presentation/CampaignCharacterArt.cs),
  `LateUpdate`, supplies a small timed bartender material selector. Guest
  nontravel phases generally reuse their seated pose.
  [CampaignCastView.cs](../Unity/Assets/Surlhwa/Presentation/CampaignCastView.cs),
  `Present` / `Refresh`, owns the paired presentation and one actual shared cup.
  Establish one restrained family sample before extending the cast; preserve
  Hamlet's stillness, authored silence and the lovers' shared cup.
- **Focused bartending transition.**
  [Q1ProductionView.cs](../Unity/Assets/Surlhwa/Presentation/Q1ProductionView.cs),
  `SwitchFocus` / `ApplyView`, changes projection immediately and fades a
  nonblocking charcoal veil over 0.22 seconds. The existing approach preserves
  input coordinates. Refine its visible reveal without moving the pointer-to-tool
  mapping during active input. Keep fixed normal/focused camera endpoints,
  Q1 scale, roots and contact anchors.
- **Tool response and return.**
  [SliceWorkbenchView.cs](../Unity/Assets/Surlhwa/Presentation/SliceWorkbenchView.cs),
  `Refresh`, projects tilt, grip and return directly from `ActiveTool`; stream
  visibility follows `AcceptedLiquidThisStep`. Start with presentation-only
  pickup/return feedback and stream/contact refinement. Do not delay accepted
  input, fabricate liquid, or award technique from animation.
- **Pixel shimmer.** Point-sampled oblique surfaces and smooth movement remain
  candidates for actual motion inspection. This audit did not measure shimmer.
  Target only demonstrated offending surfaces or frames; preserve the accepted
  pixel-first treatment and avoid global blur, camera changes or character-scale
  changes as a blanket workaround.

## Bounded implementation order after ACT7

1. Confirm the root-owned ACT6 glassware correction and its evidence, then close
   the method-only label and pointer-hold availability defects. Prepare one
   consistent Codex/Guide/settings treatment with real authored Korean and all
   nine existing recipes, without changing knowledge rules.
2. Refine one ordinary guest's directional approach/turn/sit/depart sequence.
   Check contacts, fallback and interruptions before extending other families.
3. Refine one focus reveal and one tool return/transfer presentation. Keep
   authoritative projection endpoints and domain timing unchanged.
4. Compare early and accumulated-memory room states at 720p and 1080p. Retain
   specific subjective candidates and evidence for the bundled Gate3 review.

## Meaningful validation to run during implementation

- UI regressions: method-only/Unknown/Partial/Known rows; no hidden name, amount
  or glassware disclosure; disabled/noninteractable pointer hold produces no
  accepted flow; selection restoration after rebuild, destruction and component
  disable; modal confinement and non-color focus remain intact.
- Actual layout checks: long Korean dialogue and requests, nine Codex entries,
  full supplied shelf, Guide and settings at both resolutions and both reading
  settings. Inspect glyph/text bounds and essential-control overlap; source
  dimensions alone cannot certify rendered readability.
- Changed-gesture checks: modal opening, focus loss, release/regrip and component
  disable during each affected control. Verify stopped liquid/audio and no
  resumed held action without the required release. Use targeted native input
  for changed boundaries rather than replaying unrelated accepted suites.
- Motion/lifecycle checks: unchanged roots/seat/foot contacts, departure and seat
  release, missing/cancelled frames, one absent paired actor, and no duplicate
  progression or cues. Align step sound to visible contact rather than imposing
  an independent arbitrary cadence.
- Actual short Player clips: approach/turn/sit/stand/depart, normal-to-focused
  transition and return, and the changed tool path at 720p/1080p. Compare static
  and moving pixels before claiming a shimmer improvement. Confirm visual easing
  never changes liquid quantities, technique evidence or transaction boundaries.

Technical checks can establish objective behavior. They do not close
PD01/PD02/PD05/PD11, final direction quality, motion feel, tactile feel, or final
presentation acceptance without the named Gate3 disposition.
