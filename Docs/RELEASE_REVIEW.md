# Windows first-pass campaign review

This package is for owner playtesting, not final public-release approval. Keep the whole Windows folder together. Start SURLHWA.exe and use the Korean New Game / Continue controls. README.md contains controls; CREDITS.md records provenance and remaining ownership checks. For Phase M use the versioned `Windows-PhaseM-*` package and check its `BUILD_INFO.txt`; the old `Builds/Windows` executable is not Phase M evidence.

## Human playtest

1. Play New Game through all 18 nights and the authored ending with physical mouse and keyboard. The first four nights retain 5 / 4 / 4 / 4 encounters; the extension adds 30. Read clues before attempting each required recipe, including Hanky Panky / French 75 / Last Word on nights 6 / 10 / 15.
2. Read the Guide while stirring/shaking, then resume. Open Esc sound settings, adjust every slider and mute; confirm comfortable levels and pause/resume.
3. Fail a required discovery once and retry. In a second run, fail one Prince service; confirm quiet farewell. All three successful services should lead to open farewell. Wrong technique must not count as success even with a fitting cocktail.
4. In later recommendations try different learned/supplied cocktails and methods. Confirm the request feels open to interpretation, with no hidden single emotional answer.
5. Continue after nights and after extended-night departures, then interrupt an extended dialogue/drink: that encounter should restart. Interrupt the ending: it should replay. After completing it, Continue must return to the final coda without night 19. New Game resets story/knowledge/memories but preserves sound settings.
6. Review Korean text, scrolling, dialogue choices, recipe quantities, card art and click targets at 1280×720 and 1920×1080.
7. Judge all seven themes, ordinary-guest pacing, dialogue cadence, character scale/poses, winter room mood, music repetition/mix and the final closing. Hamlet's quiet should feel deliberate while controls remain responsive. The lovers share one glass. Check whether the seven small spatial traces suggest people who were here without feeling like trophies.

## Current limits

- Input-seam automation proves production state transitions. Phase M additionally tests actual EventSystem raycasts and the existing InputSystemUIInputModule with programmatic virtual mouse events. This is not a human or OS mouse test.
- Save resumes night/knowledge/story state and, after night 4, the last departure checkpoint; no in-progress liquid or exact dialogue cursor save.
- The owner accepted the Phase L modular bar and character style as the first-pass baseline. Phase N now adds first-pass walking, turning, seated poses, service response and departure over the same customer lifecycle. Pose quality and timing remain HUMAN_ANIMATION_TUNING_LATER. Glass/garnish refinement and final memory art remain later presentation work.
- Prince leaves an empty chair after his closing visit; the small star respects his existing open-farewell eligibility. Chibi special guests and charcoal ordinary guests retain the accepted style. Shared-glass staging and spatial memory contours preserve current behavior. Hamlet's stillness is local; stronger animation/audio treatment is deferred.
- Companion/rose/secret revisits, memory upgrade trees and additional IBA expansion are FUTURE_EXPANSION. Other aspect ratios/platforms and broad quest/economy systems are OUT_OF_SCOPE.
- Public distribution requires owner review of the pre-existing artwork's source/permission and final credits.

The authoritative technical results and remaining classifications are in 13_POST_I16_PRODUCTION_GAP_AUDIT.md. Build and standalone smoke evidence are appended there after execution; do not infer them from this checklist.

## Phase M focused human review

1. At both 1280×720 and 1920×1080, click the visible guest prompt and read long Korean lines/choices. In a Recommendation, choose the countertop mixing glass/spoon, Shaker, glass/bottle and glass/horizontal-spoon setups; check hover help, selected mark, change, Back and reopen before starting. Float should clearly communicate layering. Exact authored requests retain direct entry. Check that a fast second click does not skip a reading beat.
2. Hold Space while pouring, open Guide or Settings, release while reading, close, and press again. Repeat while stirring and during Shake notes. Confirm no queued pour, motion, note or serve; liquids and pending transfers must retain their actual state.
3. Open Guide → Esc Settings → close Settings → close Guide. Check the appropriate top surface closes and reading does not consume Shake time. Repeat opening/closing and returning between scenes.
4. Scroll ingredient/recipe lists, including bottom entries. Check visible targets, masks, disabled Continue and click/drag release near other controls. Keyboard focus should stay inside the active modal and return to its opener.
5. Check Unknown/Partial/Known Codex entries without inferring unavailable recipe facts. Summary → Codex → close must return to the same summary. Read service quality, preparation and suitability as separate feedback.
6. On a disposable run, cancel New Game reset and verify progress survives; confirm once and verify a fresh first night. Continue must not promise mid-drink restoration. Check ending interruption, completed coda and return to entry.

Docs/22 contains tuning locations. Docs/23 contains screen ownership, exact
evidence levels, fixture labels, source revision, captures and build/smoke limits.
Human UI preference, auditory mix, narrative pacing and full physical-input
playthrough are not certified by the automated checks.

## Phase M package identity

The countertop staging polish adds contact shadows, grouped supporting tools,
warm hover/selected light and smaller labels without opaque menu bars. At both
review resolutions, inspect all four hover and selected states, especially the
spoon/Jigger/glass layering arrangement. Judge whether the tools feel grounded
and the light stays quiet. Human aesthetic approval remains pending. The local
before/after index is `Logs/Review/phase-m-staging-review.html`; Docs/23 records
source hashes, executed tests and remaining native-input/standalone coverage.

The countertop method-selection patch is documented in Docs/23 and implemented
in `72ff492`. The package below predates that patch; do not use it as evidence
of the new tool-selection UX. Patch validation used an isolated Editor copy.

`Builds/Windows-PhaseM-ba4346f/SURLHWA.exe` was built from runtime source
`ba4346f14a1aa9dfd73f5083399ca1230f0bd1fe` with Unity 6000.3.7f1 for Windows x64
on 2026-09-29. The build succeeded; graphics-enabled startup loaded BarScene and
remained alive for 15 seconds with saved preferences restored afterward.
This is startup evidence only. Standalone UI navigation and a full physical-input
playthrough remain to be done. See Docs/23 for exact timestamps, hashes, warning,
Editor test coverage and build-generated worktree exclusions.

## Phase N focused human review

Use the versioned `Windows-PhaseN-*` package and its BUILD_INFO.txt when available;
older Phase M executables do not contain the character work. Docs/25 records
actual build execution and evidence separately from this review checklist.

1. Watch an ordinary guest enter, walk, turn inward, sit, receive a drink,
   stand and leave. Judge foot sliding, stool contact, cadence and stillness.
2. Compare both ordinary silhouettes and all eight current special identities.
   Faces/clothes should remain familiar. The first pass uses limited pixel
   articulation and inward three-quarter views, not bespoke full directional art.
3. Watch the lovers' arrival, separate stools, shared glass and departure. There
   should still be one request/service and no companion remaining afterward.
4. Read Hamlet slowly. His quiet idle is deliberate. The bartender should listen
   without a busy loop and acknowledge service briefly from the central position.
5. At 720p/1080p check silhouettes, bent knees, reaction/interaction indicators,
   counter depth, earned memories and Korean text. The restored committed font
   and rendering settings should receive this ordinary visual review too.
6. Repeat service and Continue after an interrupted visit. Animation should
   restart with the customer; exact mid-animation persistence is not promised.

Human physical-input playtesting and aesthetic approval remain pending.
DEFERRED_AUDIO is unchanged; future special revisits remain FUTURE_EXPANSION.
