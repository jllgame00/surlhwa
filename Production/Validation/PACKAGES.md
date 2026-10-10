# New project dependencies

Editor: Unity 6000.3.7f1 (696ec25a53d1). Fresh import and resolution succeeded after the owner activated the local Editor license. No RC1 settings or package manifest were copied.

| Direct dependency | Version | Reason |
|---|---|---|
| Universal RP | 17.3.0 | Minimal Universal Renderer Forward, depth-writing cutouts and 3D room lighting |
| Input System | 1.18.0 | Architecture input backend; no gameplay action map or free camera in this pass |
| Newtonsoft JSON | 3.2.2 | Strict duplicate-key/type/version/field/reference validation at ContentIO boundary |
| Test Framework | 1.6.0 | EditMode content and scene contract tests |
| imageconversion | 1.0.0 | Native PNG evidence encoding |
| physics | 1.0.0 | Primitive colliders and objective counter ray occlusion checks |
| imgui | 1.0.0 | Review-only camera/fixture controls |
| screencapture | 1.0.0 | Evidence capture capability |
| jsonserialize | 1.0.0 | Capture/performance evidence serialization, never authored content parsing |
| ui | 1.0.0 | Required UI module dependency for the input/URP package closure |

The fresh `packages-lock.json` pins the dependency closure. Transitives include URP Core/config, Shader Graph, Burst, Collections, Mathematics, Searcher, Mono.Cecil, performance test support, NUnit, UI Toolkit/hierarchy, uGUI and terrain module. These are package dependencies, not migrated RC1 systems. No optional cinematic, audio, gameplay, 2D lighting or post-processing package was added.
