# SDD ledger — plan: Production/Validation/C_REVISION_PLAN.md

Ruling: the detailed owner design and existing autonomous execution instruction authorize implementation without another design/plan approval — optional approval loops would prevent the requested revision — cost if wrong: owner may request another bounded visual iteration.

Ruling: the latest owner direction supersedes provisional architecture emphasis on a low-detail 3D environment — C uses pixel-led flat masses with selective depth — cost if wrong: C remains provisional and may need recomposition; Canon/Architecture stay unchanged.

Ruling: keep the plan/ledger as reviewable Production validation records, using Windows-native commands instead of skill shell bookkeeping scripts — consistent with the existing Pass 1 workflow and this workspace — cost if wrong: extra local documentation, no gameplay impact.

Pre-flight interfaces: Task 1 → 2 tests consume generated C scene / fixed camera / actor anchors; Task 2 → 3 capture consumes six fixture indices and initialized stage. C output uses a new folder and manifest names; historical A/B files must not be overwritten. No conflict.

Task 1: complete. Historical manifest records 892 A/B files. `c-red.xml`: 6 failures from the expected missing C builder assertion, no compilation failure.

Task 2: in progress.

Ruling: historical A/B native tests open the existing authored scene rather than regenerate it on every run — regeneration would rewrite historical scene assets during C validation — cost if wrong: builder regeneration is covered by earlier A/B validation, not rerun in C.

Task 2: complete. Native C contracts RED (missing builder) → GREEN; full `c-native-seat-green.xml` 44/44. Physical knee regression `c-seat-red.xml` failed as expected before seat geometry correction, then passed in the full suite. Earlier Windows mapped-file failures in native authored-content mutation tests were corrected with atomic replacement; checks still mutate real source JSON and restore exact bytes. `c-final-build.log` / `build-c.json`: Succeeded, zero errors/warnings.

Unity's material upgrader rewrote three historical sprite materials when the A/B fixtures were opened in native tests, including with C-only save calls. These incidental metadata upgrades were reversed using only the three recorded original byte streams, each verified against the immutable history manifest. No owner dirty files or RC1 inputs were involved. Final A/B check: all 892 original files byte-identical. Frozen preservation: 74 specification hashes and all 65 RC1 dirty paths/index/branch/HEAD unchanged.

Task 3: in progress. Separate C Player capture run `c-reviewed`; source/Player hashes recorded in C-only manifests. Python capture outcome contracts RED (validator absent) → GREEN 14/14 including historical 7. Standalone .NET foundation 25/25. No HG04 implementation.

Task 3 evidence: `captures-c.json` PASS, 12 native stills + diagnostic, two 121-frame 12fps native clips, exit 0 / no Player errors / no native allocation warnings; counter removal reveals opaque bartender lower-body source pixels. 60.01s explicit offscreen 1080p sample, 82,931 frames; mean 0.72ms / P95 0.86ms, 1,624 draws/batches, zero idle GC. This is a Development/offscreen submission baseline, not GPU time or a low-end performance promise. The simple separate prop faces produce many draws; no optimization claim. Actual C captures and A/B/C board visually inspected; owner decision remains pending. Generated Unity YAML contains serializer whitespace; handwritten C#/Python/PowerShell whitespace checks pass.
