# SURLHWA RC1 manifest

Sealed 2026-10-04: **SURLHWA_RC1_AUTOMATED_READY**.
Acceptance: **HUMAN_ACCEPTANCE_PENDING**.

| Field | Canonical value / evidence |
| --- | --- |
| Canonical source HEAD | `da480e5e76b9f45c18635b36d1d2e07732f7842e` on `refactor/project-rebuild` |
| Canonical Windows build | `Builds/Windows-RC-5bdc0b1/SURLHWA.exe` (keep the complete folder) |
| Build source SHA | `5bdc0b10034fc44b53b502799cb6d5f58ea5dda0` |
| Runtime equivalent to canonical HEAD | **YES**; exact six-file diff classified below; no production runtime or asset changes |
| Unity / package | `6000.3.7f1`, Windows x64 Development QA package; opt-in instrumentation and isolated saves |
| Automated QA | 31 scenarios passed in both Editor and Player; 774 key pulses / 141 Submits per environment; 204 simulated pointer cycles; UI, audio and motion cleanup passed |
| Known P0 / P1 | **0 / 0** |
| Campaign | **18 nights / 47 natural departures PASS**, all special guests, memories and ending |
| Recipes | **9/9 campaign recipes PASS**, all four methods; no unresolved campaign reachability failure; two reserve recipes remain out of scope |
| Save / Continue | Eight campaign Continue entry points, reset and standalone Continue/service passed; no known save blocker or progression soft lock |
| Standalone | Startup, 31 regression cases, ordinary service/UI/Continue and visible Player captures passed within the documented boundaries; no full native campaign claim |
| Runtime references / build errors | No known missing required runtime reference or unexplained build error; static reference checks and successful Player build remain applicable |
| Native input | Windows keyboard and cursor **PASS**; simulated production pointer **PASS**; native pointer buttons remain unverified under PickerHost interception |
| Remaining human-only debt | `HUMAN_VISUAL`, `HUMAN_AUDIO` (including physical output-device listening), `HUMAN_WRITING`, `HUMAN_PLAY_FEEL`, `NATIVE_POINTER_HARDWARE_DEBT` |

The canonical source is the inspected `da480e5` snapshot. The commit recording
this seal changes documentation only and does not move the RC1 code baseline.
The existing tested build is preserved; no rebuild or gameplay rerun was needed.

## Exact diff: `5bdc0b1..da480e5`

| Changed file | Classification | Inspected change |
| --- | --- | --- |
| `Assets/Code/Editor/SURLHWARCQA.cs` | TEST_ONLY | Add existing UI review fixture and correct terminal markers; Editor folder and `UNITY_EDITOR` guard exclude it from Player |
| `Tools/Invoke-RcRegression.ps1` | TEST_ONLY | Add UI review to the external QA suite list |
| `Tools/Invoke-RcUnity.ps1` | TEST_ONLY | Allow UI review and mark the local QA result queued before rerun |
| `Docs/13_POST_I16_PRODUCTION_GAP_AUDIT.md` | DOCUMENTATION_ONLY | Append the completed RC implementation/validation record |
| `Docs/27_RC_AUTOMATED_GAMEPLAY_QA.md` | DOCUMENTATION_ONLY | Record coverage, results, provenance and limits |
| `Docs/RELEASE_REVIEW.md` | DOCUMENTATION_ONLY | Update the owner handoff |

Counts: PRODUCTION_RUNTIME **0**, PRODUCTION_ASSET **0**, TEST_ONLY **3**,
DOCUMENTATION_ONLY **3**, REVIEW_EVIDENCE_ONLY **0**, BUILD_TOOLING_ONLY **0**.

The package's 1,504-input manifest was compared with current files. Its only new
source difference is the Editor-only QA runner above. The three previously
declared copy-only inputs remain the isolated PlayerSettings and generated URP
prefilter/runtime-resource tables. Preserved owner art/font inputs retain their
recorded provenance. All 67 current protected owner/recovery files and all 322
existing package files were hashed for preservation during this seal.

## Warning and evidence disposition

- Missing RuntimePipelineConfig: optional Pipeline remote-command server
  disabled; rendering is unaffected. Existing classification retained.
- KoreanFallback importer inconsistency: import-cache warning; all 570 required
  Hangul characters and actual Player rendering passed. Existing classification
  retained; the owner font was not regenerated.

Source-qualified coverage and input boundaries remain in
[RC QA](27_RC_AUTOMATED_GAMEPLAY_QA.md). Terminal results, warning harvest,
recipe inventory and the seal's diff/hash verification are retained locally in
`Logs/Review/RCAutomatedQA/` and its `RC1Seal/` subfolder. No routine manual
gameplay pass is required by this seal. Human approval is not claimed.
