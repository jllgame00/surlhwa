# HG04 technical review

One fresh whole-branch reviewer (`hg04_review`) reviewed source, serialized assets and evidence read-only. The same reviewer handled follow-up checks. It did not build, run Unity, launch a Player or approve subjective experience.

| Finding | Resolution and evidence |
| --- | --- |
| No attempt-only recovery from a bad pour | Discard replaces transient brewing state, preserves the durable checkpoint/knowledge, and requires rearming. Standalone86/86 and Player discard assertions. |
| Save failure offered only retry | Return reloads the actual durable store; no pending outcome is fabricated. Player save-error path checks exact encoded checkpoint and exactly one service. |
| Narration/mixed/request summary attributed to guest | Only `spoken` gets a guest label; summaries use a neutral heading. Exact source wording remains unchanged. Captures validate text roles. |
| Rejected tool actions invisible | Korean feedback explains empty serving glass or missing liquid/ice. |
| Intended first drink and Continue assertions weak | Actual WHAT/FIT checked for every fixture; Continue compares the complete encoded checkpoint and empty transient state. |
| Modal timing included scripted waits | Measure input queue to rendered modal state; retain offscreen-render qualification. |
| Enter blocked in save-error modal | Reproduced in `hg04-recovery-keyboard-red`; fixed modal-scoped focused activation. `hg04-final-save-error-720` passes keyboard Retry and RightArrow/Enter Return. |
| Missing walk materials stalled logical arrival | Reproduced in `hg04-walk-material-red`; validate both frame arrays and selected entries, otherwise stand. `hg04-final-missing-actor-720` completes both encounters without errors. |
| Stale build could mask negative validator tests | Captured baseline is validated positively before ten mutations;11/11 pass. Current-build inventory validation stays separate. |

The final source recheck reported no remaining blocking source finding within the reviewed HG04 scope. Native OS coverage, exact final evidence inventory, performance acceptance, and subjective gates are separate checks. Review is not proof that untested behavior works.

Retained limits: actual GPU/main-thread time and physical input latency are not measured; inherited art remains provisional; human bartending feel and later gates are unapproved. Owner-interrupted input evidence is preserved without assigning a gameplay verdict.

Final evidence consistency recheck: reviewer inspected the18-run/161-capture manifest, all23 native ownership observations, native file hashes and normal exit0. No significant inconsistency found. HG04_BLOCKED remains accurate for missing native keyboard/fullF01 evidence; no architecture revision or new blocking source finding is indicated. This was read-only, without rerunning tests or current-inventory validation.
