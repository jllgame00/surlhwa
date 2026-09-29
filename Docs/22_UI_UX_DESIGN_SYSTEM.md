# Phase M — UI design and interaction contracts

2026-09-29. Owner-approved Phase L bar and character baseline is preserved.
Vision, gameplay, architecture, ADR-005/006/030/037/040, rebuild rules and
Docs/11, 13, 16–21 govern this presentation pass. No narrative or recipe edits.

## First-pass defaults

Use the existing uGUI / TextMesh Pro and legacy input. Reference size for new
UI is 1280 × 720, Canvas scaling matches width/height equally. Existing world
and tool scales remain independent. Review at 1280 × 720 and 1920 × 1080.

| Token | Default |
|---|---|
| Surface / raised control | #20272B / #303A3E |
| Text / secondary text | #F1E8D5 / #C4C1B6 |
| Accent / thin edge | #B9A06C / #686252 |
| Primary action | muted brass with dark text |
| Hover / pressed / keyboard focus | lighter surface / darker surface / visible edge |
| Disabled | dim surface and text; retain label |
| Selected | accent edge plus explicit selection mark; not color alone |
| Typography | existing TMP font with project's Korean fallback; no new font dependency |
| Type sizes at reference size | heading 26–30, body 20–22, supporting text 18 |
| Spacing | 8-unit grid; 16–24 inside panels; 24 screen margins |
| Controls | at least 44 reference units high where practical; padded icons |
| Transitions | button color 0.10 s; existing feedback sequencing retained |

Characters and bar remain the narrative focus; tools and liquid remain the
preparation focus. UI uses quiet opaque backing behind text, restrained edges
and live labels. No image generation is needed for this scope. Reuse the Codex,
settings, Shaker and cocktail illustrations. No baked text or global sprite changes.

## Presentation and navigation boundaries

- A surface owns only visibility, focus, local selection and scroll position.
- Guide and Settings pause existing time and technique reading. Nested closure
  must restore the prior pause, never blindly set timeScale to one.
- Codex preserves its existing unpaused Bar behavior. Dialogue retains its
  customer-owned narrative patience gate. No new global story pause.
- A blocking surface owns its full-screen raycast shield while visible. Its
  close/back operation affects only the topmost surface and returns focus to
  the opener. Inactive surfaces release shields, focus and pause ownership.
- Escape closes Settings, Guide, Codex detail/grid or method selection in that
  order of visible topmost surface; otherwise it opens existing Settings.
  Narrative reading and ending completion are never skipped by Escape.
- Inputs held across an overlay boundary require release before another pour
  or key action. Mouse stirring must rebase its first angle after reading.
- Buttons retain production callbacks and existing busy/state guards. One
  physical double activation must not consume two dialogue/ending beats.
- New Game confirms only when meaningful saved progress would be erased.
- Result → Codex → close returns to the same result without advancing.

## Data contracts

OrderData / GuestRequestContext own requests; selecting a method changes only
the route. Actual container, preparation trace and technique owners supply
measurements and feedback. RecipeMatchService recognizes the made liquid;
RequestEvaluation judges FIT separately. Knowledge is projected through
RecipeUnlockManager and RecipeKnowledgeGuideFormatter. Unknown/Partial/Known
must never bind unfiltered recipe facts. Supply remains independent of knowledge.

## Deferred implementation

CHARACTER_ANIMATION_DEFERRED: walking animation, turning, seated poses and
full-body transitions remain unimplemented work. Existing pose reuse is not
animation completion or merely subjective polish. Phase M does not implement it.

Exact visual values remain HUMAN_UI_TUNING_LATER after objective readability
and interaction checks. Audio polish is DEFERRED_AUDIO; future content is not
triggered by this interface pass.
