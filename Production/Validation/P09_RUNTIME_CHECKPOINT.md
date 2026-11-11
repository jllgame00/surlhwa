# P09 runtime checkpoint — journal and shared UI

**Recorded 2026-11-11, during `final-campaign-media-720-02`.** P09 implementation and the current graphics-enabled Editor suite pass. The Build03 full Player is still running at this checkpoint; neither `player-result.json` nor `process.json` exists yet. This record does **not** claim its Player PASS or final P09 closure.

PD01/PD02 remain **OPEN / HUMAN_SIGNOFF_REQUIRED**. Gate3 visual composition, readability preference, interaction feel and overall UI harmony remain owner decisions. See [implementation plan](P09_IMPLEMENTATION_PLAN.md), [presentation debt](PRESENTATION_DEBT.md) and [owner approval policy](OWNER_APPROVAL_POLICY.md).

## Implemented scope and authority

- `CampaignUi` now presents the existing nine recipes as a dark-paper journal: a separate selected-page mark and keyboard focus outline, projected name/state/method, aligned ingredient/quantity columns, measured preparation and finish paragraphs, and a protected footer/Close band. The existing palette, Korean font family, card footprint and Q1 composition remain.
- A method/preparation/garnish-only Partial projection uses `들었던 한 잔`; Unknown uses `아직 빈 페이지`. Recipe identity, amounts and preparation remain governed by `RecipeKnowledge.Project`. Glassware is Known-only and comes from `RecipePresentation`; required/optional/no-garnish wording uses the existing recipe and presentation authorities. Reading does not manufacture Known receipts or supply.
- Selecting a page restores that recipe's focus after rebuild. Settings retain the activated toggle; disabled/destroyed selections fall back within the current modal. Existing buttons, IDs and controller-facing APIs remain.
- LargeText and HighContrast reach campaign text and controls, including Guide/settings/recovery/error surfaces. Guide uses two columns. LargeText reuses shelf pagination; every supplied ingredient remains reachable. No settings schema, new menu, toolkit or dependency was introduced.
- Shared `SliceHoldButton` checks its own and sibling Button availability before starting a pointer hold, and releases a held action when the sibling becomes unavailable. Historical Slice layout remains unchanged.

The P09 source changes are `Presentation/CampaignUi.cs`, the shared hold component in `Presentation/SliceUi.cs`, `Tests/EditorIntegration/P09UiTests.cs`, `SliceHoldButtonTests.cs` and their metadata, plus the existing ACT6 garnish-text lookup. Root owns Player proof, scene/build and the shared input teardown guard. This checkpoint does not claim a P09 commit or alter content/save authority.

## Failures retained and corrected

### Actual font layout, not only visible ink

[Build01 completed-save 720 attempt](Evidence/final-sample-complete-720-01/player-result.json) finished with 47 inherited services/departures, nine Known recipes and seven memories, but **FAIL / 53 overflow errors / process exit 2**. Ingredient rows and LargeText title/header bounds were too short. Visible glyphs could appear inside their boxes while TMP's line-layout overflow flag remained true; earlier glyph-only checks did not establish layout safety.

The shipped font has 64-point face metrics and a 92.672-unit line height. A 20-point row needs approximately 28.96 pixels, exceeding the former 28-pixel box; a 22-point LargeText line needs approximately 31.86 pixels, exceeding the former 30-pixel box. The fix measures each ingredient/amount row using the actual preferred height, enlarges title/header rectangles, and adjusts journal spacing to preserve footer clearance. Both LargeText shelf pagination labels also received enough height.

[Shipped-font RED](Evidence/p09-shipped-font-red-01.xml) retained **16/16 failures**. [First GREEN attempt](Evidence/p09-shipped-font-green-01.xml) retained **63/67 passes and four shelf-page-label failures**, which were then corrected. Tests now clone `FinalArt/FinalKorean.asset` with owned material/atlas copies and assert both `TMP.isTextOverflowing == false` and actual glyph/footer bounds; they do not mutate the source font or weaken overflow checks.

### The full route exposed the compact ACT2/3 shelf

[Earlier full media route](Evidence/final-campaign-media-720-01/player-result.json) reached **47 actual services, 47 departures, nine Known recipes and seven memories**, but remained **FAIL / 13 text-overflow errors / natural process exit 2**. Failures occurred on compact default-size ACT2/3 shelf labels during prepared slots 17–28 and the Hanky clue capture. Completion counts did not excuse the UI defect.

Those 22-pixel buttons gave their 14-point text only 20 pixels after vertical insets, below the shipped font's approximately 20.28-pixel line height. The bounded fix removes that inset only for the expanded, unpaged default-size shelf. Button sizes/spacing, ACT1 geometry, font size, physical supply and hotkeys remain unchanged. Paged and LargeText shelves retain their existing insets.

[Shelf/menu RED](Evidence/final-shelf-menu-red-01.xml) is **6/6 expected failures**: four P09 shelf cases and two separate root-owned menu/audio cases. The four P09 cases cover slots 17/21 at 720/1080, all reading preferences and all reachable shelf pages. The current full Editor run below clears them.

## Current objective evidence

| Evidence | Observed result | Meaning and limit |
| --- | --- | --- |
| [Full Editor03 XML](Evidence/final-editor-full-03.xml), [log](Evidence/final-editor-full-03.log) | **859 total / 859 passed / 0 failed / 0 skipped**, natural exit 0; NVIDIA GeForce RTX 3070 renderer | Current graphics-enabled Editor validation. P09 subsets: `P09UiTests` 26, `P09ShippedFontUiTests` 20, modal interaction 1, shared hold 9: **56/56 passed**. |
| [Full Editor02 classification](Evidence/final-editor-full-02-classification.json), [XML](Evidence/final-editor-full-02.xml) | **846/859 passed; 13 failed** under `NullGfxDevice` | **VALIDATION_BLOCKED_BY_ENVIRONMENT**: the incorrect graphics-disabled runner produced 12 empty GPU-depth results and one RenderTexture failure. Not a license failure and not a reason to weaken assertions. Retained; superseded for current validation by Editor03. |
| [Build03 log](Evidence/final-build-03.log), [build manifest](Manifests/final-production-build.json) | **Succeeded**, Unity 6000.3.7f1, **193,274,993 bytes**, **29.4628528 seconds**, **0 errors / 0 warnings** | Builds `Bar_Q1_Final`; successful build is not Player or subjective approval. |
| [Final tool suite06](Evidence/final-tools-06.log) | **175 tests, OK**, 4.214 seconds | Verified from the unittest summary. PowerShell's formatting of native stderr is not a test failure. |
| [Final proof GREEN](Evidence/final-reading-coverage-green-01.log), [focused regression](Evidence/final-reading-coverage-regression-01.log) | **9/9**, then **37/37** final/ACT7/ACT6 tests | Captures cannot silently disappear from final-candidate evidence. [RED01](Evidence/final-reading-coverage-red-01.log) and [expanded RED02](Evidence/final-reading-coverage-red-02.log) remain retained. |

The verifier requires 48 final reading captures: four actual preference combinations × nine Known pages plus settings, Guide and coda. It checks the preference booleans, complete knowledge and unique labels. A fresh slot-0 run additionally requires selected Unknown detail in all four settings; a slot-0 visit requires the real method-only Partial matrix. French/Last Word discovery visits require their four-setting Partial matrices unless the actual archived before-service checkpoint already holds that recipe's Known receipt. The historical clue captures remain; matrix content must match them. A completed Continue is not required to recreate inherited learning states.

## Earlier completed-save Player diagnostics — Build02

These runs used an already earned Complete checkpoint, so **47/47 history is inherited, not 47 newly played services**:

- [720 sample02 result](Evidence/final-sample-complete-720-02/player-result.json), [process](Evidence/final-sample-complete-720-02/process.json): **PASS, 62 captures, zero reported errors, exit 0, no timeout**.
- [1080 sample01 result](Evidence/final-sample-complete-1080-01/player-result.json), [process](Evidence/final-sample-complete-1080-01/process.json): **PASS, 62 captures, zero reported errors, exit 0, no timeout**.

The LargeText French 75 captures at [720](Evidence/final-sample-complete-720-02/037-p09-known-1-recipe_french_75.png) and [1080](Evidence/final-sample-complete-1080-01/037-p09-known-1-recipe_french_75.png) were opened for this checkpoint. Quantities align in their own column, preparation and finish are separated, and the final garnish line remains above the footer/Close band. This is a bounded observation, not owner approval.

These are older diagnostics. They do not exercise the compact ACT2/3 shelf, do not supply fresh Unknown/Partial route coverage, and predate the final strengthened capture contract/current Build03 identity. They must not be presented as current full-route PASS.

## Native pointer evidence and limits

[Native ending720 sample02 result](Evidence/final-native-ending-720-02/native-result.json), [launch](Evidence/final-native-ending-720-02/native-launch.json), [checkpoint origin](Evidence/final-native-ending-720-02/checkpoint-origin.json) and [inventory](Evidence/final-native-ending-720-02/inventory.json) identify the earlier interactive run: **PID 33168, window 2360072**, 1280×720, with no `campaignProof` argument or synthetic devices.

Its recorded status is **PARTIAL_NATIVE_COVERAGE**. Coordinate pointer actions observed all nine ending lines, Complete, Codex/French page, settings/LargeText, return to the saved menu and same-session completed Continue. Sixteen screenshots and JSON observations are retained, including [Complete](Evidence/final-native-ending-720-02/009-complete.png), [French page](Evidence/final-native-ending-720-02/011-codex-french.png) and [completed Continue](Evidence/final-native-ending-720-02/015-completed-continue.png). The initial startup menu observation preceded activation; transitions from the first pointer Continue through completed-menu Continue were recorded as owned and uninterrupted.

The native Return action was **not observed as Enter by Unity**. Keyboard coverage remains **VALIDATION_DEBT**; no keyboard PASS is claimed. Held key/down/up and curved gestures were not attempted because that native interface did not expose them. The window exited through native title-bar close and shutdown was logged, but an exit code was not collected. Separate-process reload on the final rebuilt candidate, native full-campaign/held-tool coverage and audio audition are not established by this run. Automated pointer/keyboard Editor tests and simulated Player input remain separate tiers.

## Build03 run currently in progress and remaining work

[Current expected run](Evidence/final-campaign-media-720-02/expected-run.json) records **New / open / ACT7 / finalCandidate / 1280×720**, started at **2026-11-11T09:08:36.8536763Z**. [Its inventory](Evidence/final-campaign-media-720-02/inventory.json) binds source, content and the complete Player package. The Unity launcher executable hash alone does not identify all managed/data changes between builds; use that full inventory. Source/assets remain frozen while this run is active.

At this checkpoint, final result/process files are absent: **RUNNING / no PASS claim**. The preceding full-route failure remains historical evidence until this new route finishes and passes its matching verifier.

Open objective work is the completed Build03 Player outcome and final required 720/1080 state/settings/lifecycle coverage, including current-candidate native keyboard/held-input limitations. The plan's explicit composed-state contrast measurements and exhaustive all-settings dialogue/request review are not closed by the narrower font/glyph tests. Do not check off a compound plan item merely because its Editor portion passed.

No known P09 implementation defect remains in the current Editor-validated source. Unobserved current Player/native results remain **VALIDATION_DEBT**. Subjective PD01/PD02 remain **HUMAN_SIGNOFF_REQUIRED**, and this checkpoint does not close Gate3 or certify final game feel.
