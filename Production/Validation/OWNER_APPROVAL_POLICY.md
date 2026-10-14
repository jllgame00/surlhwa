# Owner approval minimization policy

Owner instruction, 2026-10-14 Asia/Seoul. This policy supersedes the expansion plan's earlier per-package owner approval requirements. It does not itself approve the expansion plan or change Canon, C1, HG04 or presentation-debt status.

**Default: continue autonomously within the approved scope.** Make reasonable reversible decisions and record consequential choices and evidence. Technical task boundaries, commits, internal reviews and QA checkpoints are not owner gates.

No owner approval is required for deterministic gameplay implementation, Canon-defined content wiring, routine choices covered by approved architecture, recipe/data migration, save/progression implementation, ordinary guest integration, automated QA, regression fixes, measured performance fixes, build/package work, objective UI defects, objective audio lifecycle defects, minor reversible tuning or technical implementation details.

## The three planned owner gates

| Gate | One review package | Continuation |
| --- | --- | --- |
| GATE 1 | Production Expansion Plan. | Once approved, execute the planned work autonomously through the complete ACT 1 Production Milestone. No separate P00, recipe, method or asset approval is required. |
| GATE 2 | Complete ACT 1 Production Milestone: campaign pacing, Little Prince ACT, Manwol/Float, memory presentation, current UI, current audio/music, character/environment presentation and overall SURLHWA identity. Include all other currently known subjective decisions in the same package. | After approval, continue ACTs 2–7 autonomously unless a true exception occurs. |
| GATE 3 | Final Release Candidate human signoff, including final presentation and disposition of any remaining debt. | Release acceptance remains the owner's decision. |

## The only reasons for an extra owner decision

1. Canon does not determine a meaningful product choice.
2. Two viable options materially change the player's artistic experience.
3. A change would alter an already approved narrative/theme.
4. A change would invalidate an approved visual/audio direction.
5. A destructive or irreversible migration is required.
6. The implementation technically works but no longer feels like SURLHWA.
7. A major scope increase is required.

Do not ask about trivial preferences. A technical failure alone is a reason to diagnose and fix, not to ask the owner to choose an implementation. Environment interruption remains accurately recorded and separate from gameplay failure.

When a real exception needs owner judgment, finish the safe work needed to show the concrete choice and its consequences, collect all currently known subjective decisions into **one review package**, and identify which exception applies. Prefer the next planned gate if dependent work can safely wait. If a decision truly blocks work before that gate, stop only the dependent branch and continue independent authorized work; do not silently settle the unresolved product choice or invent a recurring extra gate. Demonstrate the reason for any additional interruption.

The three gates consolidate judgments; they do not waive final presentation review. Keep Codex, footsteps and every other debt item open until the owner explicitly accepts or defers the named scope in a bundled ruling. Technical PASS and permission to continue are not final art/audio approval.
