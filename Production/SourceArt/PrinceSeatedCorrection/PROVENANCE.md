# Prince seated pose correction — 2026-10-27

Owner-requested bounded correction of the Q1 bartender-facing seated texture.
Built-in imagegen edit of `../Q1Polish/PrinceSideQuarterSource.png`; original
generated output is `PrinceSeatedSource.png`. `Before.png` preserves the previous
production sprite. The original Q1 source, import record and review evidence remain
historical and unchanged.

Intent: natural quiet stool-perch, separate relaxed hands, subtly asymmetrical
knees/boots and less rigid shoulders. Preserve youthful contemplative identity,
costume, side-quarter facing and restrained silhouette; no exaggerated stance.

Import: `python Production/Tools/prepare_prince_seated_correction.py` reproduces
the existing Q1 normalization (alpha crop, nearest-neighbor resize to 76px high,
96×100 transparent frame, same horizontal center and bottom). No mesh, material,
texture GUID/import settings, scene, camera, stool, root, contact anchor or ACT 1
logic is changed. `import.json` records exact source/target hashes and geometry.

Final visual approval remains HUMAN_SIGNOFF_REQUIRED. Native render evidence and
validation limits are documented in `../../Validation/PRINCE_SEATED_CORRECTION.md`.

## Exact built-in imagegen prompt

Use case: precise-object-edit. Edit the attached transparent Little Prince seated sprite ONLY to correct his seated posture. This is the exact identity/style/costume authority. Preserve golden tousled hair, face and eye, cream scarf, dark muted teal gold-edged coat, brown shorts and boots, chibi proportions, warm pixel-art cluster style, overall figure height and head size. Preserve screen-right side-quarter bartender-facing orientation and mildly elevated Q1 camera view. Change the stiff prim symmetrical sitting pose to a natural quiet stool-perch: relaxed slightly uneven shoulders, torso leaning subtly toward bartender on screen right, slight natural curvature rather than bolt-upright posture. Knees not pressed together: modest separation and slight asymmetry, one knee and boot a little forward/right, the other lower and closer beneath the hip, no wide-legged stance. Hands must NOT touch or fold: one relaxed open hand rests on its own thigh toward the knee, the other hangs relaxed beside the near hip at implied stool edge. Keep seat-contact hip location and lowest boot height approximately as reference; maintain compact readable silhouette without enlarging character. Quiet, thoughtful, youthful, slightly tired/contemplative; no macho stance, no exaggerated slouch, no smile, no coy gestures. Render just ONE full character with transparent background and generous margins, no stool, no counter, no shadow, no props, no text, no additional frames. Change only body posture; keep existing identity and costume. Strong pixel clusters must remain readable when normalized to 76 pixels silhouette height.
