# SURLHWA — First-pass campaign contract

Owner scope: 2026-09-28 full-campaign request. Baseline: `f819658`, with only
the owner's `.codex/config.toml` modified. This extends the four-night demo;
historical demo audits remain historical. Vision, current gameplay, the full
special-guest roadmap and IBA catalog were read before implementation.

## Audit and dependency plan

The shipped BarScene uses BusinessDayManager for nights, CustomerSpawner for
one seated customer at a time, OrderData/DialogueData for requests and reading,
and PlayerPrefs domain helpers for progress. Prince visits and service history
are canonical. No other main ACT or environmental memory is implemented.
The explicit four-night endpoint is the lifecycle boundary to extend.

1. Add an optional authored campaign schedule, departure checkpoints and a
   reusable post-service reading gate. Keep the demo path valid without it.
2. Author and validate each ACT separately, through existing orders/dialogue.
3. Introduce bounded ordinary discovery of the already-authored reserve recipes
   after checking official sources and actual preparation support.
4. Restore small spatial memories from completed ACTs; add local Hamlet staging.
5. Audit completed ACTs/memories against the vision before authoring the ending,
   then wire the complete schedule into BarScene.
6. Run full-route, Continue, failure/retry, legacy, reference and Windows build
   checks. Record input automation limits and human sign-off separately.

## Current first-pass ACT contracts

All new main visits are unhurried. A poor drink changes its response, but does
not erase a person's story or demand success to complete their visit. Reading
the post-service lines and departing completes a visit. Existing strict Prince
open/quiet farewell semantics remain unchanged.

| Nights | Main ACT contract | Memory |
|---|---|---|
| 1–4 | Existing 5/4/4/4 discovery and three Prince visits unchanged | Empty chair; small star for the existing open farewell eligibility |
| 5–7 | Chunhyang above/Mongryong below; his explanation and her silence; humiliation conveyed without exposition; simultaneous reunion sharing one served glass | Small swing and empty adjacent space |
| 8–9 | Quixote's declaration, remembered laughter, conviction after drinking, remaining alone; never a drunken punchline | Windmill fragment |
| 10–11 | Hamlet asks, lifts/lowers a cup, leaves without resolving the outside action | Stage fragment, skull, local light/shadow |
| 12–13 | Faust wants knowledge, recalls signing, lives with the cost; no newly invented rescue or condemnation | Faint circle and cracked glass |
| 14–16 | Sim Cheong talks before deciding, returns before the boat with her own resolve, returns after seeing her father changed and calm | Half-submerged lotus |
| 17–18 | Odysseus wanders, acknowledges return, finally stays for a while | Anchor and ship fragment |

The owner's explicit Odysseus scope resolves the old naming typo for this pass;
no Oedipus subplot is introduced. Hamlet's optional glance at Faust's statue is
not required before Faust's ACT; no cross-ACT causal story is invented.
Future companion/rose/secret revisits, click counters and memory upgrade trees
are excluded. Dialogue and pauses are first-pass authoring, not final tone or
pacing approval.

Every added night includes an ordinary encounter. These provide recognition of
the changing bar, rest between ACT beats, familiar requests and three spaced
IBA discoveries. No money/score/story-success gate or new quest framework.

## Authority and compatibility

Campaign data owns only authored encounter order, guest presentation and
post-service text. OrderData and DialogueData retain requests and clues; recipe,
knowledge, supply, matching and FIT owners remain unchanged. Memories derive
from completed visits rather than a second unlock authority.

Persistence adds bounded integer keys through SurlhwaPlayerPrefsStore. Existing
keys/IDs and recipe formats remain intact. Extended nights resume between
customers; mid-dialogue or mid-drink returns to that encounter. The first four
nights retain their existing save behavior. An old completed demo can continue
at the extension without fabricating old Prince successes or knowledge.
The ending has separate reached/read state so an interrupted ending can replay.
Completed Continue revisits the ending/bar, never creates an unbounded next day.

## Validation baseline

2026-09-28: `dotnet build Assembly-CSharp-Editor.csproj --no-restore` PASS,
0 errors, one existing assembly-reference warning. Previous demo runtime/build
records were inspected but are not evidence that the new campaign works.

## Implementation records

C1 — Optional schedule/encounter data, extended-night departure checkpoints,
post-service reading and ending reading infrastructure are implemented. No
campaign is connected to the scene yet; the shipped demo path remains active.
Additive integer keys use append-only visit enum identities. New Game delegates
their reset to the campaign persistence helper.

Validation: dotnet compile PASS (0 errors; 8 existing warnings on full rebuild);
Unity batch `SURLHWACampaignValidation.RunInfrastructure` PASS, exit 0 in
`Logs/campaign-infrastructure.log`. No license blocker occurred. Scene/prefab
diffs: none. Campaign route/runtime and presentation validation remain pending.
