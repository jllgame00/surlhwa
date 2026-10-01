# Phase N — character motion and service presentation

Starting revision: `1bb9e23c07373a0923af3a4a3c93c4291104d835`, branch
`refactor/project-rebuild`. Four local changes are hash-protected: config,
ProjectSettings batching, EditorUserSettings and the editor layout. No original
Editor was running at inspection. Existing UI/input evidence is retained.

## Lifecycle audit and authority

| Transition | Existing owner and seam | Starting visual behavior |
|---|---|---|
| Spawn / reserve | CustomerSpawner.SpawnNextCustomer; SeatManager.GetFreeSeat; SeatPoint.Reserve | Instantiate at spawn marker; one SpriteRenderer |
| Walk / approach | CustomerController.Update MovingToSeat / MoveToward | Straight translation to sitMarker, same standing sprite; approachMarker unused |
| Seat / ready | SitDown → Sitting → Ordering → EnterWaitingState; InteractionUI.RegisterCustomer | Immediate same-art swap; no turn or seated anatomy |
| Dialogue | InteractionUI reading flow; customer narrative patience pause | Static pose, overhead anchors |
| Service / response | OrderManager resolves GuestServiceResult; OnServedWithReaction; ReactionSequenceThenLeave | Bubble/hearts and existing timed feedback |
| Departure | CustomerController Leaving / MoveToward / LeaveAndDestroy | Same seated sprite slides to exit; release seat then notify spawner then destroy |
| Next / checkpoint | CustomerSpawner.NotifyCustomerLeft, identity guard, BusinessDayManager and persistence helpers | Next scheduled customer after natural departure |

Ordinary A/B and Little Prince prefabs bind identical stand/sit art. Extended
specials replace both through CampaignGuestPresentation.Configure. Lovers have
one authoritative customer plus a child Mongryong renderer; shared glass and
Hamlet stillness are local children. There is no Animator or clip dependency.
Movement snaps only within the existing final arrival tolerance. Feet own
sorting (`-y * 100`); paired contours follow that order. Continue restarts the
interrupted encounter; no mid-animation persistence is required.

## Presentation contract

CustomerMotionPresentation reads the same controller state, position and facing.
It changes only SpriteRenderer sprite/flip. CustomerMotionLibrary contains art,
never routes, completion events, orders or save keys. Walking advances with
actual traveled distance. Seated art bends legs and raises the hip to stool
height; it does not lower a standing card. Seated three-quarter facing points
inward toward the bartender. Root transforms and companion offsets do not flip.

The existing controller gains finite Turning and Standing intervals and a finite
Sitting interval. These are bounded timers in that authority, independent of
art/component availability. Missing frames fall back to the original sprite;
removing the presenter cannot block ordering or departure. No animation event
completes service, releases a seat or writes a checkpoint.

Defaults: movement retains 2 units/s; walk 7 frames/s at nominal speed; turn
.18 s, sit .32 s, stand .32 s; listening cadence 7 s; reaction .8 s. Hamlet
has no repeating listening accent. All are HUMAN_ANIMATION_TUNING_LATER.

## Assets and provenance

`Tools/prepare_character_motion.py` derives poses from the accepted owned
`Assets/_SURLHWA/Art/Characters/Chibi` sprites. This is deterministic pixel
editing, not newly generated concept art. Original heads, costumes, palettes
and art remain unchanged. Four short walk frames, turn, seated, listening and
reaction use a common 96×100 canvas, bottom-center pivot, Point/32 PPU,
uncompressed binary alpha. New runtime files live in `Art/Characters/Motion`;
source recipes/hashes and the pose board live in `ArtSource/Pixel/CharacterMotion`.

## Ordinary foundation validation

Unity 6000.3.7f1 compiled the ordinary foundation in the existing isolated
ValidationProject. Reference checks and ordinary motion fixtures passed:
four observed walk frames, Turning/Sitting/Waiting/Standing/Leaving, stable
seated root, natural exit and seat release, repeated entry, and complete exit
with the presenter disabled. The fixture calls a resolved weak/unknown service
and shortens the later narrative wait through the existing departure seam;
it is not normal-play, pointer delivery or full campaign evidence.
Actual 720p frame sequence and 1080p seated render were inspected. Feet clear
the floor while seated and the bent leg/hip meet the stool. First seated still
was captured before the UI follower's initial LateUpdate; the capture fixture
now yields that frame. This does not change production input/UI.

Initial evidence is retained under `Logs/Review/PhaseN/ordinary-foundation`.
Full-cast integration and campaign/build coverage remain pending.

## Current cast and paired/service extension

The same eight-pose set now covers both ordinary silhouettes and Little Prince,
Chunhyang, Mongryong, Don Quixote, Hamlet, Faust, Sim Cheong and Odysseus.
Four walk slots intentionally repeat the neutral contact pose at slots 1/3;
the alternating stride images differ. Wardrobe/head pixels remain the accepted
source; this is limited pixel articulation, not newly painted directional art.
Three-quarter inward facing is intentional; a bespoke rear-facing cast is not
part of this first pass. No scarf physics or character-specific cinematic loop.

The companion receives CustomerMotionPresentation with the existing customer's
state as its owner. Spacing increases from 1.15 to 1.5 units; an existing stool
sprite is drawn at the companion's seat position without reserving another
gameplay seat. Both remain children and are destroyed with the one customer.
The shared glass is centered between them at countertop height. Service cues
come from OnServedWithReaction; departure hides the glass. The existing delayed
shared-glass call remains idempotent. No second order/service callback exists.

BartenderMotionPresentation is one added component on the existing bartender
prefab child. It retains the centered root/depth and accepted sprite. A one-pixel
hand-rest variation appears briefly every ten seconds only without an active
guest. During conversation it stays still, looks toward the guest, and gives a
.65-second head acknowledgment at the actual service-result seam. Two owned
pixel variants supplement the unchanged original. No bartender locomotion.

Service reaction lasts .8 seconds (.52 for Hamlet), then returns to the seated
pose while the original feedback/dialogue sequence proceeds. FIT, recognition,
knowledge, rewards and visit completion are unchanged. No new service glass is
invented for ordinary guests; the existing served-result drawing remains intact.

### Cast integration checks

All ten guest sets plus the paired scene completed the bounded motion fixture.
Both ordinary variants and all eight specials showed four walk slots, turn,
sit, idle, service reaction, stand and leave; each released its seat. Repeated
fixtures ended with no remaining customer presentation. The disabled-presenter
case also completed. Compile: zero errors, eight existing warnings. Motion,
modular-world and integrated campaign Editor validators passed. All 82 output
PNGs passed exact source-palette membership, binary-alpha and recorded-hash
checks. The only prefab diff adds the bartender component; BarScene is unchanged.

`Logs/Review/PhaseN/review.html` indexes 720p motion sequences/GIFs, individual
PNGs, native crops and 1080p seated stills. Prince, Hamlet, ordinary and paired
renders were inspected at display size. The lovers occupy separate stools;
costumes, face identity and inward facing remain readable. These observations
are not owner aesthetic approval. Full campaign, physical regression and
standalone verification are recorded separately below when executed.

## Integration validation boundaries

- The strengthened safety fixture checks service reaction/return, bartender
  acknowledgment with a stationary root, companion owner/spacing/depth/cleanup,
  missing pose data, and a disabled presenter. It observes runtime errors and
  passed in the isolated Editor. No asset is mutated to simulate missing poses.
- Existing Phase K Play Mode passed all four actual method prefabs and physical
  measurement/transfer/overflow behavior. The scene/prefab change is confined to
  the bartender component; no preparation prefab was modified.
- Existing `RunVisualAfter` completed its four-route, Settings/Codex, Back,
  repeated entry and modal/raycast callback fixtures. Saved progress/knowledge/
  audio matched a separate private pre-run snapshot exactly after restoration.
  This is direct-event/callback evidence, not foreground pointer delivery.
  The A/B/C investigation was not reopened. Historical Settings/Brew incidents
  remain unresolved; no resolution is attributed to Phase N.
- 268 relevant source/art/prefab/scene/resource files matched main and isolated
  copy byte-for-byte. Final compile at this point: zero errors, eight existing
  warnings. Actual Korean reaction text and committed font/render baseline were
  rendered; ordinary owner text/render review is still appropriate.
- The older focused J5 service fixture disables customer Update. Its immediate
  Leaving assertion must now observe Standing, advance the finite timer through
  that same Update, and require Leaving. It still uses its original explicit
  departure callback, so it is not natural movement evidence. The full campaign
  separately waits for real destruction and checks exactly one departure.

The current Windows pipeline adds a Phase N entry, retaining existing dependency
validation and BuildPipeline.BuildPlayer. The isolated build requires committed
PlayerSettings; original unresolved batching bytes are preserved. Package identity
records the exact commit and this source-setting distinction. No new build system.

### Rendered exit correction

The first frame sequences exposed an existing staging defect: active seat exits
were at world x ±8.3, inside the 16:9 camera's ±9.6 horizontal span. Destruction
could therefore remove a visible walking guest, especially a trailing companion.
Only four marker x coordinates in BarScene are adjusted: spawn to -12.5 and
three active seat exits to world ±12.5. Seat roots, sit/approach markers, paths'
direction, sorting, furniture, gameplay speed and completion callbacks remain.
The original unreferenced/legacy markers are untouched. A pair now fully clears
the review camera before the same controller releases the seat and destroys it.

The full campaign was already running on the integrated lifecycle code when
this render-only marker defect was identified; it is allowed to finish without
source mutation in that Editor. Its evidence will be attributed to that source,
with the marker correction covered by final production-spawn motion fixtures on
all three seats. Initial captures remain archived; final captures use the real
spawn marker rather than the earlier shortened in-frame visual setup.
The shared-glass native crop also placed its base below the countertop lip;
its local height is corrected from 2.05 to 2.4, keeping the existing contour
and service trigger. This is checked in the final paired captures. The temporary
companion stool is owned by the visit's presentation and is removed at the same
cleanup; furniture handling itself has no separate animation.

### Full campaign result

The existing `SURLHWACampaignPlayModeValidation.Run` completed once on the
`3156429` runtime integration: 18 nights, 47 natural departures, all 16 extended
special visits, required discovery/retry and genuine alternatives, weak-service
ACT continuation, checkpoint/mid-dialogue/ending Continue, seven memories,
completed coda, reset and legacy handoff. Exact departure counts and existing
visit persistence guards passed. No production gameplay event was synthesized
by the animation layer. This is automated input-seam progression, not human play.

Archive: `Logs/Review/PhaseN/campaign-and-safety.log` (completed 2026-10-01).
The private progress/knowledge/audio snapshot exactly matched after the Editor
restored and exited. The only later runtime differences are four entry/exit
marker x values and the shared-glass y value described above; final local motion
and paired contact checks cover those. A second 47-encounter run is not inferred
or claimed for those presentation-coordinate edits.

### Final local motion and retained failure

Final motion fixtures passed after the marker/glass correction: all ten guest
sets, paired guests on left/center/right seats, actual production spawn position,
all four walk slots, turn/sit/stand, service reaction and return, bartender
acknowledgment, stationary roots while seated, natural departure beyond world
|x| > 12, seat release, and companion/stool cleanup. Missing pose data and a
disabled presenter both completed. The bartender's real ten-second hand-rest
cadence was captured. No runtime exception was observed by this fixture.
The source-art recipe reproduced all 82 PNGs, metas and the runtime library
byte-for-byte. Final source rendering keeps the shared-glass base on the wood
surface; both companions clear the 16:9 frame before removal.

Evidence: `Logs/Review/PhaseN/review.html`, `review-board.png`, 14 GIF sequences,
812 individual 720p frames, native-scale crops and 1080p stills. `motion-samples.tsv`
records game timestamps/states. `initial-staging/` retains the earlier short
entrance and in-frame-exit captures; `ordinary-foundation/` retains N2 proof.
These are production-scene fixtures with explicitly shortened narrative waiting,
not OS input recordings or human aesthetic approval.

The updated older J5 focused service fixture **failed** in its first scenario's
closing visit: `Farewell must wait for a newly discovered recipe popup to close.`
The failure occurs before the closing guest enters the new standing interval;
the first two visits had passed the new finite standing-to-leaving assertions.
No causal link to Phase N was demonstrated. Popup/service sequencing code was
not changed and this assertion was not weakened. The remaining focused J5
scenarios are unrun; retain this as separate VALIDATION_DEBT rather than a PASS
or a claimed resolution of historical UI incidents. Full log:
`Logs/Review/PhaseN/final-motion-and-prince.log`. Existing full-campaign evidence
above does not silently substitute for this failed popup-visibility assertion.

Final Editor/reference/integrated checks passed again after the coordinate
correction. Final `RunVisualAfter` callback/modal regression also completed and
restored preferences. Compilation remains 0 errors / 8 existing warnings.
`validated-final-source-sha256.json` confirms 268 relevant files match the
isolated source; protected-file hashes remain unchanged. Final scene diff is
exactly four marker x coordinates; the prefab diff is one bartender component.
Task/staged diff whitespace checks are clean. Protected config contents are not
read into reports or included in diffs/staging.
No subjective animation approval is claimed. DEFERRED_AUDIO remains unchanged.
