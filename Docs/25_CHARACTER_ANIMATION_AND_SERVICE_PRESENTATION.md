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
No subjective animation approval is claimed. DEFERRED_AUDIO remains unchanged.
