# SURLHWA Production — Pass 1

Open **Production/Unity** with **Unity 6000.3.7f1**. The inherited repository-root Unity project is frozen reference history and is not the new project's build input.

This pass implements a hybrid winter-bar spatial proof for HG01 camera/depth, HG02 3D/2D coherence and HG03 scale/grounding. All three gates require owner judgment. It contains no bartending, narrative progression, save system, production UI or audio composition.

Execution and validation: `Validation/PASS1_PLAN.md`, `Validation/PASS1_PROGRESS.md`. Raw logs, renders and builds remain local under ignored `Validation/Evidence/` and `Validation/Builds/`; small result and provenance manifests are tracked.

Frozen inputs can be checked without writing to them:

```powershell
python Production/Tools/verify_preservation.py
```
