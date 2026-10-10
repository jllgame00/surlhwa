# Pass 1 whole-branch review package

Base: 93939bb6179e51504a1bf3b036abcbe2cbb895b3. Head: current production-rebuild HEAD at review dispatch. Read-only review; never change HEAD, index, branch, worktrees, original RC1 or external specifications. No additional reviewers.

Specification: C:/Users/surlw/.codex/attachments/ea56748a-5312-49f9-a05f-353ed414b1bb/붙여넣은 텍스트.txt. Approved architecture and Canon roots are in Manifests/preservation-baseline.json.

Plan: PASS1_PLAN.md. Ledger: PASS1_PROGRESS.md (read every Ruling). Results: Manifests/tests.json (34/34 native EditMode), build.json (Windows Development Player success, zero build errors/warnings), captures.json (24 native images, two diagnostic depth frames, four clips, no Player errors/static drift), preservation-result.json. Runtime/raw logs and originals are in ignored Evidence/captures-final and Evidence/*.log. Source and payload identity are in Manifests/build-identity.json.

Review focus:

1. Duplicate JSON keys, unknown fields/version, invalid ASCII IDs and missing visual bindings must fail explicitly.
2. Korean Unicode and immutable loaded definitions must survive round trip without normalization or mutation leaks.
3. Transparent sprite margins must not occlude the room; actual counter geometry must occlude opaque body pixels.
4. Capture switching must not move actor roots/feet or silently modify material/geometry between A/B.
5. Editor-generated cache, build output, hidden Unity errors and external-file changes must not enter commits or masquerade as PASS.

Judge behavior a reasonable owner needs even when not enumerated in the spec. Distinguish important defects from first-pass visual choices and deliberately deferred HG04+ work. No artistic winner/acceptance is requested. List every behavior declined as outside scope, with reason, before the assessment. Findings need exact file/line, effect and severity. Never run Unity or regenerate scenes during this read-only review.
