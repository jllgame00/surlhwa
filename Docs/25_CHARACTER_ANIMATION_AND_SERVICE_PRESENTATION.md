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
No subjective animation approval is claimed. DEFERRED_AUDIO remains unchanged.
