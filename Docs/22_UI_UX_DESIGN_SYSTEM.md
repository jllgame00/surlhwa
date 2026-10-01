# Phase M — UI design and interaction contracts

2026-09-29. Owner-approved Phase L bar and character baseline is preserved.
Vision, gameplay, architecture, ADR-005/006/030/037/040, rebuild rules and
Docs/11, 13, 16–21 govern this presentation pass. No narrative or recipe edits.

## First-pass defaults

Use the existing uGUI / TextMesh Pro. UI events use the existing
`InputSystemUIInputModule`; physical technique polling uses legacy `Input`.
Neither input path is replaced. Reference size for new
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
  Page changes also repair focus when the previous control becomes hidden or
  disabled; restoration only targets an active, interactable control.
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

## Tuning locations and final screen defaults

| Implementation | Presentation-only values |
|---|---|
| `Assets/Code/UI/SurlhwaUITheme.cs` | palette, 1-unit panel edge, 2-unit focus edge, 0.10 s button tint, 64×64 corner icons |
| `InteractionUIController` | existing method selection/confirmation ownership; dialogue 1040×224 with 22-unit body and 54-unit actions |
| `SurlhwaMethodSetupView` | counter-projected tool setups, 128×132 hit areas; compact Korean labels 16; request note 1040×136 with body 20 and small confirm/back controls |
| `SurlhwaReadingClickBoundary` | shared 0.24 s pointer beat boundary; authored reading still requires explicit advancement |
| `CocktailCodexPanelController` / `RecipeGuidePanelController` | opaque knowledge surfaces, 20-unit recipe text, scroll areas; Guide close 160×44 |
| `BrewIngredientOptionView` / `IngredientShelfController` | 56-unit ingredient row; name 18, actual quantity 16; selected check mark |
| `SurlhwaBartendingPresentation` | public request 22; contextual instructions 20; no recipe/FIT computation |
| `GameManager` / `DemoAudioController` | 1280×720, match 0.5; entry 460×400, confirmation 480×340; settings 520×490 |
| `DayResultPanelController` | 680×380 reading surface; authored ending body 22; result remains behind Codex |
| `ResultToastController` / `RecipeUnlockPopupController` | nonblocking service toast with content-fit height; discovery 440×164; existing queue/timing |

Modal sorting: method 200, result 1000, Codex 1800, entry 2000,
reset confirmation 2100, Guide 2200, Settings 3000. Each visible modal owns
one shield immediately below itself. Only Guide and Settings acquire the
shared reading pause. Closing either restores the previous owner/time scale.
The existing Escape route closes the top surface, including nested Settings;
it does not skip authored ending text. A method selection is transient until
confirmation; no route template supplies a secret emotional answer.

### Diegetic method-selection patch

Recommendation Brew now presents tools on the accepted front countertop. The
old 800×224 menu background, heading and option tiles are hidden at runtime;
the serialized BrewPanel and its original buttons remain the modal/callback
owner. This does not create another route-start path. Exact authored requests
retain their existing direct entry (including discovery and the first Manwol).

| Setup | Method | Support |
|---|---|---|
| Mixing glass with upright spoon | 젓기 / Stir | slow stirring description |
| Existing Shaker artwork with small Jigger | 흔들기 / Shake | sideways shaking description |
| Serving glass with small bottle | 잔에 만들기 / Build | add ingredients directly |
| Empty glass with spoon across the rim and small Jigger | 층 쌓기 / Float | explicitly explains pouring slowly along the spoon to form layers |

The tools are uGUI sprite projections anchored to fixed world points on the
existing countertop, not new world physics or interaction objects. Counter
anchors, sizes and text live in `SurlhwaMethodSetupView`. World composition,
camera, furniture and characters are untouched. No prepared cocktail is
invented in these empty setup previews.

Each transparent button is larger than its visible tool. Hover/focus adds warm
tool edges, gentle brightening, soft countertop light and one-line help.
Selection sustains stronger object-centered light after pointer exit, with a
secondary check mark. Opaque label bars and underlines are removed. Only the
existing controller owns the selected method.
The request note repeats already-visible guest context, identifies recommendation
versus exact, and retains small Start/Back actions. Confirm remains disabled
until an explicit choice. Back returns to dialogue; reopening clears selection.

The method surface uses a transparent full-screen modal shield: the counter
stays visible while underlying controls remain blocked. Other surfaces retain
their existing shade. Decorative sprites, shadows, labels and selection markers
do not raycast. Codex/Settings keep their existing priority and return behavior;
Guide remains available after entering the chosen workspace. No new shortcut,
pause owner, request state, liquid state or recipe-knowledge source is added.

#### Countertop staging defaults (polish patch)

`CountertopArrangement` groups each setup inside the unchanged hit area. Stir's
spoon sits inside the mixing glass; Shake gains a small adjacent Jigger; Build's
bottle stands behind the serving glass; Float places a horizontal spoon at the
glass rim with a small pouring vessel alongside. Art-only depth offsets are
+2 units for Shake and -2 for Build; world anchors and camera stay unchanged.

`SurlhwaSetupSoftOval` is a deterministic, non-raycasting uGUI mesh helper in the
same source file. It uses 32 segments and four radial alpha bands, with an
explicit CanvasRenderer. No sprite import, texture, material or font is added.

| Presentation | Reference-unit defaults |
|---|---|
| Shared contact shadow | 82×18 at y=-33, alpha .48 |
| Individual tool footprints | .95×tool width, height 9, alpha .75 |
| Countertop light | 100×28, warm ivory/brass, maximum alpha .30 |
| Tool light and rim | 82×96 halo, maximum alpha .13; 1-unit outline, alpha .62 |
| State emphasis | idle 0, keyboard focus .30, hover .65, selected 1; .14 s full-range unscaled fade |
| Labels | 16-unit live Korean text, 1-unit text shadow; small soft backing alpha .26–.36, no opaque plate |

Props brighten from muted ivory to white. There is no movement, scale pulse or
timing change. The small check remains a non-color selection cue; it is no longer
the primary emphasis. Request/help and Start/Back sizes are unchanged. Final
subjective balance remains human tuning; capture and input evidence is in Docs/23.

No new image or font assets were needed. All changes use live TMP text, uGUI
graphics and owned art. Korean fallback remains the existing project font setup.
English ingredient names remain authored IngredientData presentation; established
Stir/Shake/Build/Float labels accompany Korean technique controls. Service grade
copy is Korean, while original grade values and every evaluation remain unchanged.

### Current visual finish (2026-10-01)

The owner's latest request explicitly permits replacing the **Settings and
Codex open-button icons** and their small chrome. This supersedes the earlier
utility-icon preservation instruction. Shaker art, Codex **content** art/glasses,
liquid colors, characters and environment remain preserved.

This subsection supersedes the countertop-light/outline values above. Existing
arrangements, contact shadows, 128×132 hit areas, 16-unit labels, request note and
Start/Back remain. There is no countertop light oval or tool halo. A local uGUI
material samples actual sprite alpha edges; low-alpha glass interiors do not
become an opaque mask. One display-pixel edge and existing bright material planes
receive restrained warm ivory. Idle uses original source color; hover/keyboard
focus use .55 emphasis and selection uses 1, with the existing .14 s unscaled
fade. The check remains secondary. Modal/disabled input clears hover/focus and
fades emphasis to zero without changing the controller's selected method.

Corner controls retain 64×64 hit areas and their positions. Their visible charcoal
backing is 48×48, the ivory book/gear is 28×28, and short 14-unit captions appear
only on hover/focus. Hover lightens the backing, press darkens it, keyboard focus
adds an ivory edge, and disabled icons dim. The actual Settings owner and Codex
panel visibility supply open state: a thin warm edge and small inset mark. No
click listeners or modal state are owned by the visual helper. Existing close,
Back and workspace Settings controls already use the shared quiet palette and
remain unchanged.

Glyphs are original deterministic uGUI geometry, not imagegen output or reused
Codex illustrations. The shader is local to projected setup Images, with no
global rendering/import changes. Source and reproduction are recorded in
`ArtSource/Pixel/UIVisualFinish/README.md`; current evidence/debt is in Docs/23.

## Deferred implementation status

CHARACTER_ANIMATION_DEFERRED: walking animation, turning, seated poses and
full-body transitions remain unimplemented work. Existing pose reuse is not
animation completion or merely subjective polish. Phase M does not implement it.

Exact visual values remain HUMAN_UI_TUNING_LATER after objective readability
and interaction checks. Audio polish is DEFERRED_AUDIO; future content is not
triggered by this interface pass.
