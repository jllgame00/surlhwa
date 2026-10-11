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

Final review: one fresh gpt-6-astra xhigh review; two Important findings accepted (seat stripe at actual opaque hip pixels, padded walking pivot discontinuity). Both reproduced in `c-final-review-red.xml` (3/3 RED), then fixed in the authorized single pass. Subsequent regeneration hit Windows mapped-PNG IO 1224; `c-regeneration-red.xml` captures it. C texture generation now avoids unchanged writes and atomically replaces changed source files; loaded texture regeneration has a native regression. No assertions weakened.

Final: minor (deferred): C automatic freshness checks compare the executable only; full source/payload inventory is independently verified for this handoff, but should be wired into future automated validation.
Final: minor (deferred): failed C validation manifests still emit ready owner-gate fields; FAIL/nonzero exit and board gate block failed package generation, but readiness fields should derive from result in future.

Final: Ruling: final pixel quality, palette, composition, perceived scale and camera choice remain pending owner judgment — technical verification cannot grant visual acceptance — cost if wrong: another visual iteration.
Final: Ruling: final animation, navigation and complete departure remain outside this deterministic pose proof — fixes remove objective contact discontinuities without implementing gameplay — cost if wrong: later movement work.
Final: Ruling: production weather/audio/dialogue/Stir/bartending/campaign/full Slice remain outside HG01–03 — no authority to cross HG04 — cost if wrong: those systems still require their planned implementation.
Final: Ruling: no GPU latency, minimum hardware or cross-platform support claim — measured evidence is local Windows Development/offscreen only — cost if wrong: later platform/performance validation.
Final: Ruling: inherited-art adoption/licensing remains pending per provenance — this pass uses existing reference assets only — cost if wrong: replace art before final adoption.
Final: Ruling: standalone 25/25 is supported by the executor's fresh saved c-foundation-final.log and native suite, while reviewer did not independently rerun it — the earlier 23-test historical log is not the current rerun — cost if wrong: no separate reviewer execution of standalone checks.

Final: fixed stool/opaque hip intersection — ActualOpaqueHipPixelsRemainInFrontOfSeat for both families RED→GREEN, full native suite 48/48; hip remains within 3mm of the supporting edge.
Final: fixed padded walking-pivot discontinuity — WalkingOpaqueFeetStayAtTheRootAcrossPoseChanges RED→GREEN, full native suite 48/48, authored pivot 48px / frame0 bottom padding 1px.
Final: fixed mapped texture regeneration failure — native failure IO 1224 reproduced; RegenerationDoesNotTruncateImportedPixelSources GREEN with a live read-only memory mapping, full native suite 48/48.
Final fix-pass build: `c-fixed-build.log`, Succeeded, zero errors/warnings. Code/assets commit `d0e1a1e`; fresh source/Player identity recorded after build. Positive capture run uses fresh folder `c-reviewed-fixed`; prior C iteration and every A/B original retained. No second review dispatched.

Task 3: complete (implementation range `187dbc81..d0e1a1e`, review-package refresh follows). Final C Player run `4875b825a05e4db481f695901ffa731e` exited 0 with no errors or native allocation warnings. `inspect_c_review.py Production/Validation/Evidence/c-reviewed-fixed` → PASS: all six fixtures at native 1280×720 and 1920×1080, two native 121-frame/12fps clips, counter diagnostic revealing 3,408 opaque lower-body pixels. Final stills and transition crops were visually inspected: opaque hip stripe removed and walking/seated horizontal anchors aligned. These are objective contact checks, not owner aesthetic acceptance.

Final handoff verification: native evidence 48/48 and standalone log 25/25 hashes match; fresh Python suite 14/14. All 279 source / 282 Player files match the C identity inventory at `d0e1a1e`. All final still/clip/diagnostic hashes, nine board source hashes, owner reference hash and 38 local review-index links pass. `c_history.py` → 892/892 original A/B files byte-identical; `verify_preservation.py` → 74 specification hashes plus RC1 branch/HEAD/status/index and 65 dirty paths unchanged. Handwritten/review-package `git diff --check` passes.

Final local sample: 84,335 explicit offscreen 1080p render requests across 60.013s; mean 0.71ms / P95 0.82ms, 1,624 draws/batches, zero idle GC. Earlier `c-reviewed` figures above remain historical. No GPU/display/minimum-spec claim.

Finish disposition: keep `production-rebuild` and the existing production worktree in place for the owner's requested review; no merge, push or cleanup. HG01, HG02 and HG03 are READY_FOR_OWNER_REVIEW; owner judgment PENDING; C remains provisional; HG04 untouched.
