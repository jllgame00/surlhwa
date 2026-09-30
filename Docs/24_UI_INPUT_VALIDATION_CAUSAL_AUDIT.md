# UI, input and validation lifecycle: consolidated causal audit

Audit date: 2026-09-30. Historical run times below are UTC on 2026-09-29 unless stated otherwise.

## 1. Conclusions and scope

Actual repository: D:/univ/surlhwa/Surlhwa, branch **refactor/project-rebuild**, HEAD **57ad0bf4db2af9d0d3520b3155334fe6886f17bc**.

The evidence establishes several different failures, not one persistent modal lock:

- **A, historical Settings close:** the final compound assertion failed. It did not record which term failed, whether close reached its callback, or the final scale/top surface. Its cause remains an evidence gap. Later successful closes do not explain this incident.
- **B, historical Brew:** before and after the attempted activation, the raycast hit Brew, no modal/pause owner remained, and the route was idle. Selection stayed closed. Delivery, callback entry and guard outcomes were not recorded. A persistent modal lock is contradicted by these snapshots; an input delivery problem is the strongest remaining family of candidates, without a proven historical cause.
- **C, later Brew:** raw virtual mouse events were observed, but the player Dynamic input state stayed at (0,0), released, while application/Game View focus was absent. No action, pointer event or Brew callback followed. This failure is causally explained at the input-state boundary by the installed Editor input policy. It does not prove B had the same cause.
- **D, coordinated preflight:** host foreground verification failed, followed by an expired manual READY wait. The scenario never started and no input was injected. This is a setup blockage, not a runtime modal failure.
- **E, harness defect:** the old ModuleClick reported a missing callback and returned normally. The farewell loop therefore continued clicking. This defect is proven and was corrected in bf89c6d. The correction is present at HEAD.
- A separate later **Next failure after focus returned** fits a shared UI pointer retaining another mouse's position. This is a supported candidate with a concrete installed-source mechanism. The retained trace does not contain the module's internal pointer position needed to establish that mechanism for the incident.

No production correction is justified by A/B/C/D/E. In particular, there is no basis for resetting all locks, forcing timeScale to 1, changing production focus policy, or changing the selector visuals. One small host-observation improvement is justified: declare native API types and check the PID-query return value before interpreting PID 0. It would improve diagnostics; it is not a proven cure for historical D. **No fixes were implemented.**

The audit read source, serialized scenes/prefabs, installed package code, Git changes, original failures, successful comparisons, full delivery summaries and host scripts together. No new Unity scenario or standalone build was run. After the integrated analysis, one read-only native PID-query comparison was performed (§9). One new report is the only intended repository addition.

## 2. Baseline, interrupted-work reconciliation and preservation

### 2.1 Repository and execution state

The interrupted investigation had completed substantial reads but had not written a report, edited code, committed, or started a new Unity test. The proposed Docs/24 filename was absent. Outstanding shell reads had completed; no pending audit submission was replayed.

Git status at reconciliation contained **63 modified tracked files**, with no staged changes:

- .codex/config.toml;
- Assets/Settings/UniversalRP.asset and Assets/UniversalRenderPipelineGlobalSettings.asset;
- 56 art PNG metadata files under Assets/_SURLHWA/Art/Bartending/Pixel, Characters/Chibi and Environment/ModularBar;
- Assets/_SURLHWA/Art/Fonts/KoreanFallback.asset;
- ProjectSettings/ProjectSettings.asset;
- UserSettings/EditorUserSettings.asset and UserSettings/Layouts/default-6000.dwlt.

All 63 current file hashes matched the retained path/hash baseline in Logs/Review/phase-m-coordinated/baseline.json. Against the older Temp/PhaseMModalFollowup/baseline.json, only ProjectSettings.asset and the Editor layout differed, as already recorded before this audit. Those differences are analyzed in §5; they were preserved. No Assets/Code or Packages worktree diff was present.

At final process inspection, the process named unity (PID 7940) was the windowless command tool at C:/Users/surlw/AppData/Local/Unity/bin/unity.exe, with MainWindowHandle 0. It was **not a verified running Unity Editor**. An earlier name-only observation was insufficient to identify it as an Editor. No agent action launched, closed, refocused or stopped any Editor or helper. Historical reports saying an Editor remained open describe their own observation time, not today's state.

Ordinary sandbox shell launch sometimes failed with “helper_unknown_error: setup refresh had errors.” Explicit read-only shell requests succeeded through automatic review. This was neither a Unity licensing result nor an approval rejection. No approval restriction was bypassed.

### 2.2 Authority and evidence handling

Read the root AGENTS.md; discovery found no applicable nested instruction file. Read Docs/00_PROJECT_VISION.md, Docs/06_REBUILD_RULES.md, Docs/22_UI_UX_DESIGN_SYSTEM.md and **all of Docs/23_UI_UX_IMPLEMENTATION_AND_REVIEW.md**. Consulted relevant current gameplay, architecture, bartending and production-audit sections in Docs/01, 03, 11 and 13. Historical entries remain intact.

The product contracts remain: restrained UI, explicit player reading/confirmation, Exact requests entering their authored method, Recommendation preserving method choice, and actual preparation determining the drink. The audit makes no content or subjective visual assessment.

No authentication file, environment dump, full process command line or credential was printed. Raw historical startup logs were inspected only through selected diagnostic messages; private save payloads were not published. This report cites ignored local evidence that must be retained locally; those logs are not assumed to exist in a clean checkout.

## 3. Production lifecycle

### 3.1 Source reference key

All production line references below refer to the current working tree at the stated HEAD. Aliases in tables expand to these actual paths:

| Alias | Source |
|---|---|
| GM | Assets/Code/Managers/GameManager.cs |
| Spawn | Assets/Code/Managers/CustomerSpawner.cs |
| Guest | Assets/Code/Customer/CustomerController.cs |
| Day | Assets/Code/Managers/BusinessDayManager.cs |
| UI | Assets/Code/UI/InteractionUIController.cs |
| Setup | Assets/Code/UI/SurlhwaMethodSetupView.cs |
| Order | Assets/Code/Managers/OrderManager.cs |
| Flow | Assets/Code/Managers/BartendingFlowCoordinator.cs |
| Mini | Assets/Code/Managers/MiniGameManager.cs |
| Modal | Assets/Code/UI/SurlhwaModalSurface.cs |
| Guide | Assets/Code/UI/RecipeGuidePanelController.cs |
| Audio | Assets/Code/UI/DemoAudioController.cs |
| Reading | Assets/Code/UI/SurlhwaReadingClickBoundary.cs |
| Codex | Assets/Code/UI/CocktailCodexPanelController.cs |
| Result | Assets/Code/UI/DayResultPanelController.cs |
| Shelf | Assets/Code/UI/IngredientShelfController.cs |
| Harness | Assets/Code/Editor/SURLHWAPhaseMUIValidation.cs |
| Input | Library/PackageCache/com.unity.inputsystem@57d0e36f6123/InputSystem |
| UI module | Input/Plugins/UI/InputSystemUIInputModule.cs |

### 3.2 Transition map

| Transition and owner | Preconditions, calls and state written | Completion, rejection, cancellation and references |
|---|---|---|
| Entry → Bar ready: GM, Day, Spawn | GM.Awake consumes launchGameplay. New Game/Continue guard entryTransition, set launch state and load the Bar scene. Day restores day/campaign progress. Spawn.Start requires an active gameplay session and auto-spawn. | Entry/night navigation is a scene load, unlike the additive bartending return. GM:21–30,75–113; Day:39–75; Spawn:113–123. |
| Bar → eligible guest/order: Spawn → Guest → UI | Spawn rejects an existing guest, inactive session, exhausted quota or missing setup; assigns authored order/campaign data and activeCustomer. Guest arrival enters Sitting/Ordering, registers itself and its order with UI, then Waiting. | Spawn:140–241; Guest:371–423. UI.RegisterCustomer:170–235 clears old selection/forced-method state and configures this guest's request. Authored dedicated requests do not inherit tutorial route overrides. |
| Eligible request → visible Brew: Guest/UI | Ellipsis and Next reveal authored dialogue. Pre-request lines set narrative pause; finishing them restores request controls. ShowDialogue clears selection, shows the dialogue blocker, hides BrewPanel and chooses Next/Brew visibility. | UI:365–419,475–568. Narrative pause stops guest patience; it is distinct from shared modal time pause. Farewell uses a separate callback and does not allow Brew. |
| Device event → Button click: Input System → EventSystem → Button | Active module actions receive player-state changes. Point supplies module pointer position. Button press/release must resolve to compatible click handlers and remain eligible. Reading boundary rejects raycasts during the shared 0.24-second reading beat. | UI module:637–745,2146–2190,2381–2417; Reading:11–25. Selection, a manual raycast, and queued input are separate facts. The module can process a different internal position from the probe's desired position. |
| Brew click → callback/guards: UI | Awake adds brewButton.onClick once. OnBrewClicked rejects farewell/no active guest, handles Prince choice, rejects unfinished pre-request dialogue, then handles dedicated request or opens selection. | UI:103–140,675–759. Serialized Brew Button has no persistent calls: BarScene.unity:974–1025. A UnityEvent observer count proves invocation of the event, not that every production guard accepted. |
| Brew → Exact route or method selection: UI → Order/Setup | TryStartDedicatedRequest returns false for Recommendation. Exact closes panels and calls PlaySpecificOrder. Recommendation hides dialogue/blocker, shows BrewPanel, clears hasSelectedMethod, refreshes the existing method controls. | UI:675–710,728–759. If OrderManager is absent on dedicated entry, it logs and returns without opening selection; this is a real rejection branch, not evidenced in B. Opening the selector registers an unpaused order-200 Modal surface. |
| Selection → selected setup: UI → Setup | SelectMethod respects any forced-method restriction, writes selectedMethod/hasSelectedMethod, updates visuals and enables confirm. Setup reparents existing buttons; it does not own request or route state. | UI:762–803; Setup:18–81,101–134. LateUpdate projects countertop anchors. Hover/selection highlights do not launch bartending. Decorative sprites do not raycast; tool hit targets remain 128×132. |
| Selected setup → confirmation: UI → Order | Confirm rejects no guest, no selected method or missing template. It closes panels, resolves OrderManager, then passes the chosen execution template while retaining authored Recommendation request semantics. | UI:862–909. Cancel returns to the request and next Brew clears selection; UI:1137–1174. ClosePanels also resets narrative state. Missing-data failures do not prove a modal defect. |
| Confirmation → accepted session: Order → Flow | Order rejects null input/customer/request, missing coordinator and an already active flow before assigning order/customer/request. It constructs availability, recipe/guide and session context; expectedRecipe is tied to Exact requests. | Order:587–724. BeginBartending can reject an invalid session, busy coordinator or already loaded bartending scene. Failure notification/recovery is explicit. Flow:65–99; Order:115–121; UI:270–321. |
| Accepted session → bartending scene: Flow → Mini | BeginAcceptedFlow sets busy/generation and clears prior captured/finalized state. Coroutine loads BartendingScene additively, keeps Bar active, validates the scene-scoped runner, captures camera/audio state and invokes Mini.Play. | Flow:102–359,1000–1008. Async load is awaited and stale generations rejected. Bar remains loaded; camera/audio presentation changes do not disable the Bar EventSystem/UI host. |
| Mini.Play → preparation workspace: Mini → method prefab/Shelf/Guide | Mini selects route prefab, instantiates, initializes actual preparation and guide knowledge, finds IMiniGame, then StartGame with a completion callback. Invalid route/prefab/interface follows error cleanup. | Mini:68–139. BartendingScene.unity:163–173 has debugPlayMode false and legacy mainHUD/mainWorld/mainCamera references null. No second EventSystem was found in this scene or the four method prefabs. |
| Workspace → Guide/Settings: Guide or Audio → Modal | Guide requires initialized context, activates its child panel and takes its technique-reading flag. Settings creates its root once, sets settingsOpen/owner and activates it. First pausing Modal owner records the current time scale, then sets zero. | Guide:52–154; Audio:138–161,184–209; Modal:53–92. Guide order2200 and Settings order3000 both pause; Codex1800 and selector200 do not. |
| Overlay → restored workspace: close callback → OnDisable/Modal.Release | Guide.Close deactivates its child and clears its read flag. Audio.CloseSettings clears open state, deactivates root synchronously and clears owner. Release is idempotent, removes the surface, disables its shield immediately, restores gates/focus and releases pause ownership. | Guide:114–154; Audio:148–155; Modal:151–170. Shield Destroy is deferred, but deactivation is immediate. Final pause owner restores the captured prior scale if scale is still zero; it does not force a default 1. |
| Preparation → service result: method → Mini callback | Actual containers, preparation actions and garnish produce MiniGameResult. Read/pause flags suspend technique/transfer progression; tools/garnish/state guards reject premature service. Methods enter Result before invoking onDone. | StirMiniGame.cs:242–269,331–354; ShakeMiniGame.cs:451–479; BuildMiniGame.cs:127–153,174–212; FloatMiniGame.cs:385–451, all under Assets/Code/MiniGames. |
| Result → scene return: Mini → Flow → Order | Mini captures served appearance and destroys its current instance. Flow accepts one callback per generation, unloads additive scene, validates return, restores prior presentation, finalizes/clears busy, then applies the result through Order. | Flow:342–359,394–505,1011–1053. Failure path unloads/restores/finalizes separately:508–587. Result application is after unload, not before. |
| Service → departure/retry: Order → Guest/UI | Normal service evaluates actual drink/request, presents feedback, calls OnServedWithReaction, and clears Order's currentCustomer/order/request. Required discovery failure clears those order references but returns the same authored guest for another attempt. | Order:124–303,316–370. UI has its own current guest until cleared for service/departure; these are distinct scopes, not automatically stale duplicates. |
| Departure → next guest/night: Guest → Spawn/Day/Result → GM | Guest reaction waits for feedback and explicit farewell reading. Last Next clears/caches farewell callback before invocation; Guest leaves and notifies Spawn. Spawn identity-checks, records departure, clears active refs, increments served and schedules the next guest. Day guards one close and persists the next night. | Guest:598–605,738–854; UI:465–509; Spawn:243–317; Day:78–139. Result.Restart/ReturnToTitle guard navigationRequested and call GM; Result:233–244,263–290. |

### 3.3 Subscription, disable and invariant review

- **UI listeners:** UI.Awake:110,118–128 subscribes the flow-failure event and buttons once. UI.OnDestroy:142–147 removes the external event. OnDisable:150–153 resets narrative pause. Existing buttons belong to the same scene lifetime; no repeated OnEnable registration was found. Serialized Brew persistent calls are empty. Later listener-count traces report one, but those observations alone cannot prove every lifetime.
- **Audio listeners:** Audio.ObserveBartending:60–72 detaches the previous shelf before attaching the new one. The runtime Settings button is created for the current workspace. Audio.OnDestroy:157–161 closes Settings and detaches subscriptions. Audio has no OnDisable close hook; no normal audited path disables this Bar owner while retaining an open Settings root. Treating the missing hook alone as a demonstrated production bug would require an additional reachable path.
- **Modal lifetimes:** SubsystemRegistration clears static lists/gates/held-key state (Modal:48–52). Activation and release are registered/idempotent. OnDisable and OnDestroy both release. Gate management preserves Selectable.interactable and restores prior usable focus rather than reviving a hidden control (Modal:101–134,153–170). Back/input boundary handles held keys and the transition frame (135–150). A destroyed shield awaiting end-of-frame is not an active shield.
- **Pause restoration:** first-owner capture and last-owner restore support nested Guide/Settings. A legitimately non-1 prior scale is preserved. External code changing scale while paused is deliberately not overwritten when scale is already nonzero. No log proves that such an external write caused A. Current SettingsCycle compares prior scale/top/pause/opener individually.
- **Guest expiry:** Spawn:229 only enables patience for non-campaign guests. B's authored Night 5 guest does not use that timer. Guest:508–525 also suspends patience during bartending/narrative pause. A timeout between press and release is possible for other ordinary guests in principle, but is not supported for B. ClearCustomer checks identity (UI:237–263); Guest.OnDisable:162–168 also clears this guest.
- **Open then immediately close:** Brew selection and Settings visibility changes are synchronous. Modal LateUpdate maintains focus/gates; it does not close the selector. Setup disable clears visual emphasis, not UI choice. No B callback/visibility trace exists to exclude a transient open/close there. C contains no callback or open request at all, so open-then-close is ruled out for C.
- **Completion/busy:** each method's terminal state rejects another normal serve, and Flow additionally rejects duplicate/stale completion before finalization. The coordinator waits for the method's result; it has no general watchdog that manufactures completion. Scene-load/unload failures have explicit cleanup. An arbitrarily disabled/destroyed in-flight runner could strand a coroutine, but no such reachable production path was established for these incidents. A/B occur outside an active route.
- **Physical work:** Shelf:424–506,579–583 clears PhysicalTransferActive when transfers/doses/ice finish; the coroutines run on BartendingPhysicalPresentation. Guide/Settings pause transfers rather than canceling them. Method OnDisable ends direct pours (Stir:736, Shake:1073, Build:573, Float:888). Whole workspace unload destroys these session-local objects. Disabling the coroutine host mid-transfer would need a separate retained-session path to establish a stuck flag. The tempting “full target causes endless transfer” theory is not reachable through the current constructor: BartendingPreparationState.cs:20–25 leaves serving/mixing/shaker capacities unbounded; only Jigger is bounded.
- **Reading pace and assertion timing:** Reading uses unscaled time. Current ModuleClick waits 0.3 seconds after release and direct ReadToRequest waits 0.26 seconds. The beat is 0.24 seconds. Newly revealed controls may need a Canvas frame after Update; the current fixture explicitly yields one before Shake strain and night-summary clicks. Increasing waits everywhere has no causal justification.
- **Day callbacks:** Result.BindButtons removes/re-adds its listeners; ConfigureDemoNavigationButton removes both route handlers before installing the correct one. navigationRequested prevents duplicate scene navigation (Result:263–290). No extra result completion was found in the audited normal loop.

## 4. Harness lifecycle and assumptions

### 4.1 Actual entry points and ownership

The Editor class is SURLHWAPhaseMUIValidation. Existing public methods at Harness:32–42 are RunBaseline, Run, RunNavigation, RunInputModule, RunModalSequence, RunForegroundNavigation, RunPointerHandoffDiagnostic, RunDiegetic, RunStagingBefore, RunStagingAfter and RunNotificationLayout. The runtime fixture is SURLHWAPhaseMUIRunner. None was invoked by this audit.

| Harness transition | Actual behavior and boundary |
|---|---|
| Process/project selection and copy | Host scripts target Temp/PhaseMDiegetic/ValidationProject explicitly through the Unity command tool. Compare scripts hash original Assets/Packages/ProjectSettings into a manifest. Initial temporary launcher scheduling differed from later eval_file dispatch; Temp/PhaseMModalFollowup/next.cs:1–4 removes the old launcher's callback/sequence state. |
| Scene preparation / snapshot | Harness:43–65 rejects playing/pending and dirty loaded scenes, snapshots the enumerated progress/knowledge/audio keys, sets Pending, resets progress, opens disk BarScene and requests GameView focus before EnterPlaymode. This alters test state and is not a harmless inspector query. |
| Domain/scene reload → runner | EditorSettings enables Enter Play Mode options with value 0, so neither domain nor scene reload is disabled. InitializeOnLoad subscribes Changed; EnteredPlayMode schedules the runner through delayCall (Harness:19–31,67–71). Runner persists across fixture scene changes via DontDestroyOnLoad. |
| Input/focus policy | Runner stores runInBackground; all modes except foreground-navigation set it true (246–259). This keeps work advancing but does not override Editor pointer/keyboard focus policy. Foreground mode retains the production value false. |
| Host focus gate | run-segment.py:22–38 requests foreground once and checks OS PID before dispatch. run-manual-segment.py:35–47 observes for 180 seconds; only after PID matches does it query focused Editor GameView and non-Play state. This is a pre-Play Editor-window check, not Application.isFocused. |
| In-Play preflight | Harness:661–720 runs after SeedNight/Guest inside ModalSequence. It checks application+GameView+not Editor-paused, arms focus monitoring, requires active module/actions and a harmless Codex target, sends two released positions 3 pixels apart, then checks Dynamic advancement, own Point action, device position and module hover. No click is required for readiness. |
| Virtual input creation/processing | Modes add a virtual Mouse; ModuleClick queues released position, yields a frame, queues press, yields a frame, queues release, then waits 0.3 real seconds. It does not force InputSystem.Update (465–514). Dynamic player state and Editor state can differ. |
| UI delivery and observation | CanPointAt is a manual center raycast plus interactability check, not evidence of the module's actual pointer position. Current observer is added to the same Button only after checking handler identity; it does not Use, forward, focus or create a target (481–489,1041–1054). Global onEvent, onAfterUpdate and action subscriptions observe separate boundaries. |
| Callback and assertions | Local onClick listener counts deliveries. Current code requires exactly 1 for enabled actions and 0 only for explicit disabled probes; mismatch throws at512–513. State assertions independently check selected method/visibility, original request semantics, surface/shield counts, and restoration. Observer callback order can put the production callback before the observer's PointerClick line. |
| Capture/logging | Capture:315–339 waits real time, yields a Canvas frame and renders offscreen captures; no focus call is present. Trace buffer holds 8,192 events with dropped count. TracePointer limits event samples per click. These bounds must be checked before claiming a complete timeline. |
| Next scenario | SeedNight/ReadToRequest:341–371 use direct setup clicks and seeded progress. ModalSequence:573–660 seeds once, then services Stir/Shake/Build/Float with normal result/unload/farewell/night transition. Earlier overlay repetitions were 3; foreground mode uses 1. These modes have different coverage. |
| Physical setup seam | ServeSimplePreparation:722–770 disables the method component during public physical action seams to avoid released-Space polling ending a programmatic pour. It uses a supplied imperfect 10 ml preparation, restores the component, then module-clicks technique/serve. This tests actual result/unload, not full human preparation or successful recipe discovery. |
| Stop, teardown and restoration | Guarded catches exceptions, Finish writes trace/result, restores runInBackground and requests ExitPlaymode. OnDestroy removes global/action observers/devices. Changed(EnteredEditMode) restores keys and compares them before clearing Pending (67–86,261–309). Result-file appearance alone precedes completed restoration. Current coordinated host waits for not-playing and Pending=false before another scenario. |

### 4.2 Six distinct input observations

| Observation | What it establishes | What it does not establish |
|---|---|---|
| OS foreground PID | Which native process owns the foreground window at that sample | Focused Editor tab, player focus, action delivery |
| EditorWindow.focusedWindow | Which Editor window Unity reports selected | OS foreground or Application.isFocused; G reports GameView while appFocus=false |
| Application.isFocused / installed gameHasFocus policy | Player/application focus state relevant to input routing | Actual mouse position or correct raycast |
| EventSystem.currentSelectedGameObject | Keyboard/navigation selection and prior-focus restoration | The pointer hit target; B could retain Next selection while manual raycast hit Brew |
| Manual raycast / module raycast | Hit at a queried point / hit from the module's processed pointer | These points need not be equal; manual success cannot establish Down/Up/Click |
| probeMouse state after Dynamic update | Applied player position/button state for that device | A Point action, module pointer update or Button callback without further evidence |

### 4.3 Circularity, clocks, observer interference and cleanup

No circular precondition was found in D: the manual host checks EditorWindow and non-Play state, both available before Play. The separate Application.isFocused check is inside Play. The in-Play readiness probe supplies harmless released movement itself, so it does not require the click that it prevents.

Timeouts in Harness.Wait, Guarded, preflight and ModuleClick are real/unscaled; host deadlines use time.monotonic. Settings/Guide pause therefore does not by itself stall a scaled test timeout. There is a different liveness limit: the in-Play deadline is evaluated by a coroutine. If the player loop stops advancing before foregroundArmed is set, the runner cannot execute that deadline. SeedNight/Guest precedes the in-Play preflight. The host's independent 660-second deadline can still expire, but deliberately preserves an active fixture/snapshot. This is not the cause of D, which never dispatched, nor evidence of a production pause leak.

The earlier host run.py variants used a fixed 3-second post-result delay (Logs/Review/phase-m-event-trace/run.py:23–28). Current coordinated scripts observe Pending=false instead. Current Start also rejects a pending test, preventing intentional overlap. Hard process failure or an exception after Pending is set but before Play entry is not covered by the normal EnteredEditMode restoration callback. The snapshot is a recovery record, not an isolated save namespace or crash-proof transaction.

SurlhwaPlayerPrefsStore.cs:9–41 directly accesses Unity PlayerPrefs. The copy has the same company/product settings; no separate persistence namespace is created by copying the directory. Concurrent owner edits could be overwritten by snapshot restoration. This audit did not access or change those values, and a future scenario must use a controlled idle validation session with no concurrent save editing.

Observers do not mutate PointerEventData or call callbacks. Adding an event interface can affect handler lookup in principle; current ModuleClick asserts that existing Down/Click resolution is already the same Button. No evidence shows observer interception in these traces. Trace logging can affect frame timing; older traces are therefore not timing-identical to the original uninstrumented runs. The development logger did throw on a destroyed Unity object (§6.6), which is an evidenced observer defect, subsequently corrected.

Host view sizing/focus calls occur before the protected segment. Current coordinated host performs only file polling during the segment. The capture code does not request focus. Neither fact identifies the actor that changed application focus in the later traces.

## 5. Original project, test copy and settings

### 5.1 What “1,251 files match” actually means

Temp/PhaseMModalFollowup/compare.py:5–14 and coordinated/run-segment.py:5–14 enumerate every original file under Assets, Packages and ProjectSettings, including C#, scenes, prefabs, data/art assets, their metadata, package manifest/lock and settings. The retained manifests were parsed: each contains 1,251 entries and zero false comparison flags. The historical procedure is **one-way**: it did not count files existing only in the copy. Historical records identify an extra launcher and metadata.

This audit also performed a fresh two-way SHA256 comparison. **1,251/1,251 matched; zero copy-only files** in those three trees at the current snapshot. This establishes current disk equivalence, not retroactive identity of every executed revision. The old launcher is no longer an extra current file.

Current hashes:

| File | SHA256 |
|---|---|
| Harness | 9a27d69363fe5d885f7858c7c27991f3a06cff3058c8de405a8c69a9e00f702e |
| Packages/manifest.json | 96f2728c1603da8c90ee29942059ca18ab628b03d4b2ab0a7b7e1f10ec5dd4f3 |
| Packages/packages-lock.json | e8da2edd898c6c4bf0b73d076ac126ce90cd9153eacd381e5a217f2b758871a6 |
| ProjectSettings/EditorSettings.asset | 7827f85ac2d8ce213a15baea0157d2df582eb6be435654139c974c6e3ee5c969 |
| ProjectSettings/ProjectSettings.asset | 7164a495ab0f50825079f51dec8bdc2e70b531a0341121d868cda53434cda827 |

Excluded: Library/import caches, generated binaries, Temp/Logs, local UserSettings/layout, EditorPrefs, SessionState, OS focus/devices, in-memory objects, and shared runtime saves. Unsaved original Editor scenes/assets could differ from disk; no live original-scene inspection was performed. Byte equivalence of unrelated caches is unnecessary. Evidence that a particular loaded assembly used a given revision comes from the retained run/source records, not the current disk comparison alone.

### 5.2 Installed configuration and active UI host

- Unity is 6000.3.7f1, revision696ec25a53d1 (ProjectSettings/ProjectVersion.txt:1–2).
- Input System is 1.18.0 (Packages/manifest.json:7; packages-lock.json:181–182; installed package.json). uGUI is 2.0.0 (manifest:12; lock:313–314).
- ProjectSettings.asset:86 runInBackground=0; :887 activeInputHandler=2 (Both). EditorSettings.asset:27–28 enables enter-play options with value 0; domain/scene reload are enabled.
- Recorded runtime input policy is ProcessEventsInDynamicUpdate, PointersAndKeyboardsRespectGameViewFocus, ResetAndDisableNonBackgroundDevices. Input/InputManager.cs:3321–3348 defers pointer/keyboard events away from player updates when Game View lacks focus under that policy. onEvent receipt does not establish Dynamic application. UI module:996–1004 also considers runInBackground when deciding whether to process UI, but that does not supply missing player input state.
- BarScene.unity:7169–7233 contains one active EventSystem with enabled InputSystemUIInputModule and explicit Point/Click actions. Its pointerBehavior is 0, the shared mouse/pen mode. Runtime traces identify EventSystem/InputSystemUIInputModule with enabled actions. Bartending does not add another EventSystem.
- BarScene.unity:17413–17427 binds UI's actual dialogue/Brew/method/confirm/cancel objects. Its Brew Button persistent UnityEvent list is empty; Awake is the listener owner. No second serialized Brew route was found.
- UI module OnEnable hooks/enables its actions and resets pointers; OnDisable resets pointers, disables/unhooks actions (1638–1670). These are independently owned from the project-wide action asset.

### 5.3 Removed preload reference

The removed entry is fileID -944628639613478452, GUID **2bcd2660ca9b64942af0de543d8d7100**, resolving to **Assets/InputSystem_Actions.inputactions** through its metadata. It contains project-wide Player and UI actions. EditorBuildSettings.asset:15 still points com.unity.input.settings.actions to that asset.

Installed Input/Editor/ProjectWideActions/ProjectWideActionsBuildProvider.cs:16–28,56 onward obtains this configuration, marks it project-wide and includes it through the preload build provider. Input/Editor/Internal/BuildProviderHelpers.cs:13–57 adds a persistent preload when needed and removes the provider-added entry afterward. Input/InputSystem.cs:2997 onward and3724–3745 establishes project-wide enabling during initialization; OnPlayModeChange:3618–3622 synchronizes devices. Thus the reference has a real project-wide initialization/build role. Its disappearance must not be dismissed as formatting.

It is **not Bar's UI action asset**. BarScene.unity:7203–7213 references GUID **ca9f5fa95ffab41fb9a615ab714db018**, resolved to the installed package's Plugins/PlayerInput/DefaultInputActions.inputactions. No Assets/Code consumer of InputSystem.actions or PlayerInput was found. Active UI actions and later delivered clicks contradict a claim that the removed entry left this module without actions.

The historical byte change was reconciled in Docs/23:1416–1430: restoring the single entry into then-current LF bytes reproduces the former ProjectSettings hash 3c96e7c360c7b2eb8f7b0874d2b56bb4acf177acf648dedbe70aba65cf952d61. Current original/copy hash is in the table. This audit independently confirmed current resolution, empty preload list and current Git diff, which is only Standalone batching static1/dynamic0. The build provider supplies a possible explanation for preload-list churn, **not proof of who changed it or when**. It neither explains historical A/B nor establishes universal standalone equivalence.

### 5.4 Layout and startup differences

The older retained layout hash is 83e0f8340d11261bec227c870506d408f5f511be9cb9d1a930ad58e00dc1e1f6; current is 86bd9574a92eaaf1503bb377e6dce00858b920bcd6970a39258ba691ea75fddd. The recorded changes are geometry, control identities/title and stored view data (Docs/23:1431–1438). The copy has its own layout. Layout can affect which view starts selected, but cannot establish later OS/player focus. No layout or original settings were normalized.

The original chain used a temporary isolated launcher. A later launcher delay callback did not progress and host dispatch replaced its scheduling while retaining the copy/process and probe order. Copy recovery/restart, cold import versus an existing owner Editor, reload boundaries, fixed Game View sizing and direct setup clicks all limit “same source” as a reproduction claim. A/B must be compared at their actual request/overlay states, not closed by a fresh unrelated entry pass.

## 6. Failure timelines

Evidence lines refer to physical log lines or the named trace event/attempt; some Windows log readers count embedded line separators differently. Unique messages, run IDs and frames are provided to disambiguate. A missing field is not assigned a guessed value.

### 6.1 A — original Settings-close mismatch

Sources: Temp/PhaseMStaging/editor-final-after.log around17888–17940; validation-results-final-sequence.txt:1–5; historical Harness at a027d6c:392–398. The chain completed staging720/1080 at 12:09:16/12:09:34, diegetic720/1080 at 12:10:27/12:11:22, then the input-module result was **False at 12:11:38.7304364**.

| Boundary | Supported observation |
|---|---|
| Last confirmed correct | Guide close completed sufficiently for the immediately preceding assertion: guide.IsReading=false and timeScale=1. |
| Attempted transition | Harness queued BartendingSettings then CloseSoundSettings; both dispatch messages exist. Those old messages did not count callbacks. |
| First confirmed incorrect | “Module flow left a modal lock”: timeScale !=1 OR Modal.Top !=null. The log does not identify which, or both. |
| Missing evidence | Settings-open/close callback, player input state, module pointer/press/release, settingsOpen, owner count, top identity and actual scale at failure. |

Competing chains: close never delivered and Settings remained open; close callback ran but release/restoration failed; an unexpected surface/scale change occurred. No evidence selects among them. The assertion ran after a 0.3-second real-time window and normal close is synchronous, so a blanket extra focus or animation wait is not a demonstrated correction.

Comparison: Temp/PhaseMStaging/input-final.log has a fresh input-module pass at 12:15:47.5410781. Later trace Logs/Review/phase-m-event-trace/traces/20260929T141055872-input-module-pass.txt records attempt 15: frame4003/mono14.387526 close entry,14.388453 release with owners 0/scale 1/no top,14.388784 close complete, frame 4105/14.685512 restoration equality. This proves a successful close at the relevant workspace state, not A's cause or chained reproducibility.

### 6.2 B — historical Brew activation failure

Sources: Temp/PhaseMModalFollowup/reproduce-chain.log around145193–145249; Docs/23:909–924. Third original-order chain, **13:23:29**, diegetic720p. Executed production a027d6c with state-trace harness hash 94BD5F658AE25439F2535C8B3587F770465C691BD1B06E484915F999E3BB1A23.

| Boundary | Supported observation |
|---|---|
| Before, frame2073 | timeScale1, pause owners 0, top empty, Settings/Guide false, route false, selected EventSystem object NextButton, manual ray BrewButton, appFocused true, editorFocus GameView, one button listener, no active surfaces. |
| Attempt | A virtual mouse activation was queued/logged for Brew. |
| After, frame2227 | Same no-lock state, manual ray BrewButton; selection not open. |
| First incorrect | “Selection entry: visible=False, selected=False, confirm=False, appFocused=True, editorFocus=GameView.” |
| Missing | Focus between snapshots, Dynamic input state, Point/Click actions, module position, Down/Up/Click, callback entry, pre-request/farewell guard values and visibility transitions. |

Last correct boundary is request availability/manual target eligibility, not actual input delivery. The first unobserved causal boundary is player/module pointer delivery. No persistent modal/pause owner or busy route can be carried forward as the explanation. Stale Next selection is not a pointer blocker. Authored guest patience cannot explain this particular guest disappearing.

Relevant successful comparison: 20260929T140808211-staging-after-pass.txt, first Brew on Night 5. Frame2455/9.859223 receives released state and9.859728 Point; frame 2456/9.863982 Click and9.865226 Down; frame 2457/9.868635 Up,9.868907 Brew callback,9.869212 open request,9.870648 open complete,9.871289 observer Click; frame 2564/10.167034 count 1 and selection visible. It shows the complete expected chain for the same request class. It is a different run and does not supply B's missing boundaries.

### 6.3 C — raw events received, player state unchanged

Source: Logs/Review/phase-m-event-trace/traces/20260929T142937378-pointer-handoff-FAIL.txt:15–28, complete28-line trace, dropped 0.

| Frame / monotonic seconds | Observation |
|---|---|
| 3234 /13.340846 | Brew active/interactable/allowed; scale 1, no top/pause, route idle; appFocus=false, no focused Editor-window name. |
| 3234–3235 | Queue released point (892,56); raw receipt13.351835. Module pointers empty/ray NONE. |
| 3235 | Dynamic post-update remains position (0,0), left 0. |
| 3236 /13.355273 | Raw pressed event received; Dynamic state still(0,0), left 0. |
| 3237 /13.358235 | Raw release received; same unchanged Dynamic state. |
| 3372 /13.657807 | callbacks 0, selector inactive; immediate mismatch13.659230 and Finish(false). |

Last correct boundary: virtual event arrival. First missing boundary: applied player input state. No UI action, target pointer event, Brew callback or open request followed. InputManager's focus routing is consistent with these actual state samples; runInBackground=true did not override it. Production callback rejection and open-then-close are ruled out for this attempt. The actor responsible for focus loss is unknown.

This was the **positive leg** of RunPointerHandoffDiagnostic. It failed before the competing mouse was created. No negative handoff result exists from this run.

### 6.4 D — coordinated gate never entered the scenario

Sources: Logs/Review/phase-m-coordinated/report.txt; 720p/focus-acquisition.json; 720p/manual-focus-acquisition.json; run-segment.py:22–46; run-manual-segment.py:32–55; prepare-view.cs:1–5.

- Prepared copy/owned Editor was PID 29120; source comparison passed.
- One native foreground request reported requestSucceeded=true, but the following PID query recorded0. Host stopped before dispatching RunForegroundNavigation.
- Manual observation used the correctly typed PID function, waited 180 seconds and ended with foregroundPID 2716 (reported Chrome), gameViewVerified=false.
- No 720p/dispatch.log was present at audit inspection. The manual helper only calls its Editor query after OS PID matches. The absent log supports that this branch was not reached; it is not evidence of an in-Play readiness failure.

Last correct boundary: copy preparation and view setup. First unmet boundary: verified foreground ownership/GameView readiness. Missing evidence: API return/error and captured HWND at the automatic PID 0 sample; no scenario, input or modal timeline exists. “SetForegroundWindow returned true” is not the same observation as a verified subsequent foreground PID.

Automatic query omitted native argtypes and ignored the GetWindowThreadProcessId return; manual query declared argtypes. That asymmetry warrants diagnostic hardening but cannot explain both attempts by itself. The narrow comparison in §9 did not reproduce a query disagreement.

### 6.5 E and the distinct later Next failure

Sources: Logs/Review/phase-m-event-trace/traces/live-before-stop.txt:774–820; Temp/PhaseMEventTrace/editor.log:71547 onward; final 20260929T142020944-modal-sequence-FAIL.txt. Earlier successful portion remains in live-before-stop because the final 8,192-event ring dropped it.

| Frame / mono | Observation |
|---|---|
| 15305–15308 /42.95–42.97 | Attempt 50 Next: own mouse 21 Point, Click, Down, Up, callback 1; module ray hits Next. |
| 15400 /43.341284 | Point cancellation on mouse 21, appFocus=false; Click cancellation follows. Editor-window name remains GameView. |
| 15401 /43.347903–43.348434 | External mouse 2 at (1120,-886.83) and Point performed; appFocus=true again. |
| 15401 /43.350373 | Attempt 51 begins with focus true; desired/probe point remains(388,56). |
| 15402–15404 | Released/pressed/released mouse 21 states apply in Dynamic and Click actions perform. No new own Point action is recorded. Module ray is NONE; no target Down/Up/Click. |
| 15547 /43.660524 | callbacks 0; next attempt immediately begins instead of failing. |

For the Next loss, last correct boundary is applied device state/Click action. First missing boundary is a module target hit/Down at Next. This differs from C, where player state never updated. UI module:1905–1949 reuses one mouse pointer,2146–2161 changes its position only on Point,2178–2190 changes button state on Click. A Point from mouse 2 followed by an unchanged-position mouse 21 click can therefore leave the shared pointer at mouse2's point. Earlier logging called the probe device's position “position”; it did not expose the internal pointer position. The candidate fits but is not fully measured.

For E, the violated invariant is independently proven: at 424b65b Harness:343–352 logs delivered count and returns without asserting it. The farewell loop retries because the reading callback remains pending. Full editor.log has 825 completed delivery summaries for the failed sequence, including 775 unexpected zero-callback Next attempts after the first 50 successful clicks. The final trace reaches attempt 826; dispatch and completed-summary counts need not be identical. It was eventually stopped normally and restoration was recorded. This is harness continuation after failure, not repeated successful navigation.

bf89c6d adds the mismatch throw (historical lines484–485; current 512–513). C then demonstrates the first failed Brew stopping immediately. No fallback callback was used.

### 6.6 Other retained fixture-development failures

These precede the final repeated-navigation fixture and must not be folded into A/B:

| Evidence in Temp/PhaseMModalFollowup/reproduce-chain.log | Boundary and disposition |
|---|---|
| 29558–29586 | Back returned to request dialogue. Codex preflight ray hit OverlayBlocker; the fixture tried a corner control while dialogue intentionally blocked it. Confirmed harness state assumption; current fixture tests idle-bar overlays before dialogue. |
| 39138–39145 | After Stir service/unload, TraceModal accessed a destroyed GameObject.name and threw MissingReferenceException. Confirmed diagnostic lifetime bug, separate from successful production completion just before it. Current identity code checks Unity-null. |
| 47774–47801 | Shake technique completed; immediate StartupButton query ray hit Background. A newly revealed control had not yet acquired its expected rendered hit layout. One frame was added; supported timing explanation, without a complete original Canvas trace. |
| 60781–60822 | Build departure recorded; DayResultPanel was top and NextNightButton selected, but its immediate ray was NONE. Same rendered-transition timing candidate; current fixture yields a frame. |

Docs/23:834–842 also records the public pour seam being stopped by ordinary released-Space polling during fixture development. Current ServeSimplePreparation explicitly suppresses that polling only while driving the seam. This is a test-input mismatch, not a reason to alter production keyboard behavior. These failures do not show B's Brew callback ran and rejected.

### 6.7 Later foreground-navigation stop

Source: Logs/Review/phase-m-preflight/20260929T145613251-foreground-navigation-FAIL.txt and timeline.txt.

- Frame 3114/11.861394: preflight ready, two Dynamic updates, own mouse 14 position (56,668), Codex hover, focus true.
- First 26 attempted clicks delivered once. Settings close 22 at frame 6016/19.232745 restored scale 1/top NONE/pause false/allowed/interactable as expected.
- Final Stir service attempt 26 completed at frame 9234/26.435774 with route idle.
- Frame 9965/27.963988: appFocus=false while Editor window still GameView; armed monitor stopped with BLOCKED_TEST_ENVIRONMENT before another pointer attempt.

This proves a successful preflight and first service, followed by an observed environmental stop. It does not prove departure/next guest, Shake/Build/Float,1080p or a full navigation pass. It used harness hash 309979e9db84e3ead09e3843a486e80dd02888eac824b6b29448617d35636444; the final HEAD hash includes later Point-observer cleanup/environment classification and has no full runtime pass in this record. The focus-changing actor remains unproven.

## 7. Verified change history

Every listed ref resolved through Git; no checkout or worktree overwrite was used.

| Ref | Runtime behavior | Harness/evidence change and invariant implications |
|---|---|---|
| ba4346f | Phase M usable focus restoration for hidden/disabled UI selection and modal handling | Input-module masked scrolling/focus checks. A baseline, not “known good” for chained failures. |
| aa4fc33 | None | Documentation-only Phase M handoff. |
| 72ff492 | Diegetic setup presentation reuses existing method Buttons; selector uses transparent Modal shield and disables legacy blocker while open; one Shaker scene reference | Adds selector review coverage. Brew guards, dedicated-request routing and confirmation semantics unchanged. Selector display/geometry lifecycle does change. |
| ca16d95 | None | Documentation-only selector handoff. |
| a027d6c | Countertop presentation/light polish: grouped props, jigger, nonraycasting shadow,0.14-second unscaled emphasis, labels | Capture fixture changes. Existing128×132 targets and callback/route semantics retained. Earliest retained A and B occur with this runtime revision; that correlation does not identify the visual change as cause. |
| 424b65b | No production behavior changes | State tracing, legal repeated navigation, later callback/device observations. Still logs a missing callback without throwing. Several intermediate harness hashes ran before final commit. |
| cef4112 | None | Documentation-only follow-up handoff. |
| bf89c6d | Conditional Editor trace calls at Brew/Settings/modal boundaries; no guard/close policy change | Full event boundaries, pointer inspection, same-handler observer checks, fail-fast and bounded handoff entry. Earliest measured C; E correction. Budget passes and later failed verification used different harness stages. |
| 5a74876 | None | Documentation-only event-trace disposition. |
| 57ad0bf | No production behavior changes | Foreground preflight/monitor and cleanup; final-source runtime coverage remains narrower than compile coverage. D is subsequent coordinated host evidence, not a executed scenario regression. |

The first evidenced A is12:11:38, B 13:23:29, the later Next loss43.660524 seconds into the 14:20:20 run, C the 14:29:37 run, and the foreground stop the 14:56:13 run. D is separately retained under phase-m-coordinated. No earlier commit is declared free of these failures merely because it has no recorded incident. Diagnostic source calls add timing work even when game semantics remain unchanged; “same runtime” is not a claim of identical timing.

## 8. Causal findings matrix

The matrix gives classification and ownership. Details immediately below provide the competing explanation, distinguishing evidence, smallest correction and behavior risk for each finding.

| ID | Observation / owner | Classification | Exact evidence and violated assumption / causal chain |
|---|---|---|---|
| F01 | A / mixed evidence | EVIDENCE_GAP | a027d6c Harness:398; original editor-final-after.log “Module flow left a modal lock.” Compound failure cannot identify close delivery, pause or top-surface failure. |
| F02 | B / mixed | EVIDENCE_GAP | reproduce-chain.log frames 2073/2227 and selection-entry exception. Manual Brew hit plus no persistent lock does not prove Button delivery or accepted guard. |
| F03 | C / environment–input harness | CONFIRMED_CAUSE | Complete pointer-handoff-FAIL:15–28 and InputManager:3321–3348. Unfocused input routed away from player state → no actions/pointer/callback. |
| F04 | E / harness | CONFIRMED_CAUSE | 424b65b Harness:343–352; live-before-stop:817–820; full editor.log775 unexpected zero attempts. Missing throw → caller continues farewell attempts. Already corrected. |
| F05 | Later Next; possible B / harness–package interaction | SUPPORTED_CANDIDATE | live-before-stop:796–817; UI module:1905–1949,2146–2190. External Point, no own Point, own Click/state present, ray NONE → shared pointer may retain external position. |
| F06 | D / host environment | CONFIRMED_CAUSE | Coordinated focus JSON/report and run-segment:38/manual:47 stop before dispatch. Failed verification/expired wait prevents scenario; no runtime assertion exists. |
| F07 | D automatic sample / host harness | CONFIRMED_DEFECT_NOT_LINKED_TO_HISTORICAL_FAILURE | run-segment:34–36 does not declare PID-query argument types/check native return. PID 0 is reported without distinguishing query failure. Manual:34 fixes type declaration. §9 did not reproduce differing outputs. |
| F08 | A/B persistent lock theory; C callback rejection / production | RULED_OUT_WITH_EVIDENCE | B snapshots have owners 0/top empty/scale 1/route false; C has no callback. A itself is not ruled out. Blanket unification as a modal lock violates these observations. |
| F09 | Missing UI actions due to preload / settings | RULED_OUT_WITH_EVIDENCE | GUID resolution, BarScene:7203–7213, UI module:1638–1652 and action traces. Removed project-wide asset is not the active module's explicitly bound asset. Scope is this UI path, not every built-player startup behavior. |
| F10 | G foreground stop / environment | CONFIRMED_CAUSE | Foreground trace frame 9965/27.963988 and Harness:113–121: actual focus loss triggers immediate environment stop. Actor unknown; successful first service does not imply full route. |
| F11 | Early logger/Codex failures / harness | CONFIRMED_DEFECT_NOT_LINKED_TO_HISTORICAL_FAILURE | reproduce-chain:29572–29586 wrong UI-state assumption;39138–39145 destroyed-object logger. Both precede/fall outside historical A/B causal proof; current fixture corrects them. |
| F12 | Current timeout/restoration completeness / harness | EVIDENCE_GAP | Harness:61–71,268,310–313,670–710; host 660-second clock. Player coroutine deadlines require frames; snapshot restore requires EnteredEditMode. No measured incident ties these limits to A/B/C/D. |

### F01 — A's missing close boundary

Competing explanation: missed CloseSoundSettings delivery versus a real owner/restoration failure after a delivered close. Distinguishing evidence: one failure trace showing Down/Up/Click, close entry, release owner count, actual scale and top immediately before/after. Smallest potential correction: if delivery missing, fix only the demonstrated harness input assumption; if callback/release runs with wrong state, fix that owner transition. Risk: production pause changes are high enough to be unjustified until that branch is known. Current separated assertions already remove the old diagnostic ambiguity.

### F02 — B's missing Brew boundary

Competing explanations: transient focus/input loss; same-position pointer handoff; a production guard; transient open/close. Distinguish with the existing event/guard trace at the same authored request and chain boundary. Smallest correction depends on the first failed boundary; no speculative selector change. Risk: altering Brew guards can bypass authored reading/request semantics. Snapshot appFocus=true excludes sustained absence at those samples, not transient loss between them.

### F03 — C's player-state failure

Competing explanation of a production rejection is contradicted by no callback entry. Device-to-player routing precedes UI and failed first. Remaining uncertainty is why focus was absent. Smallest correction is a valid foreground input prerequisite plus first-failure stop, already embodied in the foreground harness; preserve production focus policy. Risk: low for harness gating, high for globally ignoring focus. A new focus wait cannot reconstruct B's missing state.

### F04 — E's continuation defect

Competing explanation that every logged activation constituted a successful action is contradicted by callbacks 0 and unchanged farewell. Need no new experiment to establish the defect. Keep exactly-one callback assertion, negative disabled expectation and immediate failure retention. Risk: no production change; assertions must continue to distinguish UnityEvent invocation from production guard success. No additional fail-fast framework is needed.

### F05 — shared-pointer handoff

Competing explanation: module ray query/device identity or another UI timing condition, rather than retained internal position. Distinguishing evidence: existing ModulePointers output during the negative handoff fixture, plus own Point and applied Dynamic state. Same-position virtual events should fail with external internal position if the candidate is correct; a fresh own Point should be present when internal position is correct. Smallest possible later correction is a harness pointer-position precondition before pressing, contingent on the result, not callback fallback/refocus/retry. Production behavior risk is low if confined to test-device movement; changing global pointerBehavior would affect real input and is not justified.

### F06/F07 — D and native measurement

The automatic focus request can succeed yet a later sample differ; the query itself can also fail. Retaining its HWND, thread return, error and immediate PID distinguishes these. The audit's same-handle typed/untyped comparison returned the same valid PID, so “ctypes caused historical PID 0” remains unproven. The manual helper already declared types and ended outside the owned Editor, so API typing cannot close the whole blockage. Smallest safe improvement: declare all native signatures used and report query failure separately; no automatic refocus. Production risk: none. Historical HWND/error evidence cannot be recovered by another present-day wait.

### F08/F09 — bounded exclusions

Competing production theories remain possible only beyond the recorded scope: a transient B change between snapshots or a different project-wide action consumer/startup path. To reopen those candidates, show an actual transient owner/guard or such a consumer. No correction is indicated now. Restoring preload/settings to make a test convenient would alter preserved owner state without explaining current enabled UI actions.

### F10/F11 — distinct observed failures

For G, the trigger is measured focus loss; actor attribution needs an OS/window observation at that instant, not a guessed tool/window cause. Keep stop-on-loss. For early logger/Codex failures, the stack/raycast establishes the incorrect harness assumption; null-aware identity and testing corner overlays only in their allowed state are sufficient and already present. No production change is needed. Newly revealed-control timing remains narrower evidence than a full UI rendering proof.

### F12 — limits that need no speculative fix now

The coroutine/restore dependencies are static facts, not demonstrated historical causes. A future interrupted-start test could evaluate recovery, but is unnecessary to distinguish A/B from input handoff and would touch persistence. Preserve current snapshots and require observed restoration before another test. Do not turn a generic crash-recovery concern into a new framework during this audit.

## 9. Bounded experiment after the integrated analysis

### 9.1 Completed: native PID observation only

Candidates: the old implicit native call systematically misreads the current foreground handle, versus a valid current query whose historical failure needs other evidence. Predictions: differing/failed implicit output with valid explicitly typed output supports a current marshaling failure; identical successful outputs do not reproduce that candidate.

At **2026-09-30T03:38:59.436483Z**, one captured foreground HWND was passed to two independent user32 bindings: the original implicit GetWindowThreadProcessId invocation and the manually corrected explicit HWND/DWORD-pointer signature. No window activation or wait occurred. No Editor call, file mutation or input injection occurred.

Result: 64-bit Python; handle fit signed 32-bit range; foreground unchanged; both calls returned thread 35152, PID 35352, lastError 0. This rules out a systematic disagreement for that sampled handle. It does not prove the automatic historical PID 0 was valid, and it does not identify the historical foreground window. The host's unchecked-return defect remains a justified small correction; historical causation remains open.

### 9.2 Highest-value remaining experiment, not executed

Use existing **RunPointerHandoffDiagnostic** (Harness:784–804) once at 720p in an owned isolated validation Editor. It contains a normal Brew/Back positive leg and then a competing mouse at (-100,-100), followed by a same-position Brew attempt. The deliberately negative fixture is expected to stop on its first lost click; its overall FAILED label alone is not a production failure.

Required prerequisites: identifiable idle validation Editor, disk source hash equal to the intended harness revision, no Pending/dirty scene, protected save snapshot established, focused GameView with advancing Dynamic player input and enabled module/actions, one coherent positive Brew trace. Do not reuse an unknown owner session or reset it. Do not advance past a failed positive leg.

| Candidate | Different predicted evidence |
|---|---|
| Shared-pointer position retained | Positive Brew succeeds. Negative own player button state and Click action update, own Point is absent, internal pointer remains at competing point, module ray misses and callback 0. |
| General unfocused delivery (C) | Player Dynamic position/button does not apply; no own action/pointer chain. The prerequisite failed, so the negative fixture is not a handoff result. |
| Production Brew guard / transient close | Correct module hit and Down/Up/Click plus Brew callback occur; existing guard/open/close trace identifies the rejected or reversed transition. |
| Handoff candidate not reproduced | Internal pointer and target delivery remain correct through competing-device fixture; preserve this outcome, do not force a miss or infer A/B closure. |

Only the original first-failure artifact should be retained; no automatic refocus, duplicate click, callback fallback or full campaign rerun. This still would demonstrate a mechanism, not prove that historical B used it.

Execution prerequisite was unavailable: no running owned validation Editor was verified; the observed unity process was a windowless command helper. The single missing owner/setup condition is **an idle owned validation Editor with its GameView genuinely ready for the protected input segment**. This audit did not launch another Editor or repeat the 180-second READY wait. Static conclusions and the written deliverable do not depend on that missing runtime result.

## 10. Validation-claim review

Full per-click summaries were checked, not just PASS labels or truncated rings. Temp/PhaseMEventTrace/editor.log contains:

| Run / completion line in raw log | Completed click summaries | Unexpected callback mismatch |
|---|---:|---:|
| Staging720 /3664 |12|0|
| Staging1080 /5152 |12|0|
| Diegetic720 /12269 |60|0; four intended disabled Confirm zeros|
| Diegetic1080 /19334 |60|0; four intended disabled Confirm zeros|
| InputModule1080 /28406 |15|0; one intended disabled Continue zero|
| ModalSequence720 /47091 |204|0|
| ModalSequence1080 /65764 |204|0|
| Later failed ModalSequence /113399 |825|775 zero-callback Next attempts|
| Failed handoff positive leg /114962 |1|Brew callback 0; stopped|

The expected disabled zeros match fixture calls using disabledProbe. Full successful navigation summaries have exactly 1 delivery each. Their final trace rings alone were incomplete (39,033 and38,682 dropped events); the full raw summaries fill the callback-count gap. Therefore **none of these seven later successful runs is evidenced to have depended on a missed enabled callback**. They remain bounded programmatic successes for their recorded scenarios. This does not retroactively add fail-fast enforcement to those versions.

Earlier successful original-order chains before callback instrumentation cannot establish every click's delivery. State assertions still support their asserted states. A missed repeated same-state selection or a later successful Next could have been hidden by the old helper. The proper narrowing is “recorded fixture completed its assertions,” without a universal claim that every attempted virtual action was delivered or first failure always stopped. There is no evidence to label those earlier passes definitely false.

Specific corrections/limits:

1. The 14:20 failed verification is not a complete route pass. Retain prior delivered interactions, but exclude attempts from the first lost Next onward as navigation success.
2. The 14:29 handoff run did not execute its negative leg. It establishes C and corrected fail-fast behavior only.
3. The 14:56 foreground run establishes preflight plus 26 delivered clicks/first Stir service, then environment stop. It does not establish all methods, departure/next guest or 1080p.
4. D supplies no scenario PASS or FAIL; it is blocked before scenario start.
5. A/B remain historical failures even though later runs reached the corresponding state successfully.
6. Compilation, static contracts, serialized-reference checks and independent physical-state checks do not depend on ModuleClick and are not invalidated by E. This audit did not rerun or upgrade their claims.
7. Direct onClick baseline calls, manual ExecuteEvents setup, virtual InputSystem pointer delivery, physical public-action seams, rendered capture, standalone execution and human input are separate evidence classes. Harness.Click:226–244 uses direct onClick in baseline mode and manual ExecuteEvents otherwise; it does not prove the InputSystem path. NotificationLayout is explicitly a layout fixture. No human/standalone/full-recipe/pacing approval follows from these records.
8. Existing finite imperfect-drink services preserve actual composition/result processing but do not establish successful recipe discovery or full human bartending input. No campaign content acceptance claim is added here.

## 11. Prioritized minimal next actions

### 11.1 Confirmed fixes

1. **Keep the already implemented fail-fast correction.** No additional production or harness edit is needed for E. Keep prior-state Settings assertions, null-aware trace identity and observed restoration/Pending completion.
2. **In a later bounded host change, type/check the native focus-query APIs.** Capture query success separately from a valid different foreground PID. This is a small justified diagnostic correction with no production behavior effect; do not advertise it as A/B/D's root-cause fix. No fix was made in this audit.
3. **Use this report's narrowed evidence claims.** Preserve all historical documents/logs; no blanket removal of prior compilation or physical validation results.

### 11.2 Discriminating tests

1. When the single Editor prerequisite in §9.2 exists, run the existing handoff comparison once with the existing internal-pointer diagnostics. It distinguishes the strongest measured post-focus candidate at the smallest scope.
2. If that mechanism is established, make only a separately authorized, bounded harness correction and verify the same positive/negative boundary. Do not change production UI pointer policy to fit the fixture.
3. Historical A/B attribution still requires a failing close/Brew trace at their relevant chained state. Only if resolving those historical incidents remains necessary after the narrow comparison, use the existing traces in the minimum relevant chain and stop at the first failure. A fresh entry pass cannot close them. No new tracing framework is needed.

### 11.3 Work to stop

- Repeated blind focus waits, automatic refocus/retry and replays whose prerequisites were never observed.
- Treating manual raycast, selected object, player input state and OS focus as interchangeable.
- Calling every failure a modal lock or assigning C's cause to B without its missing events.
- Blanket modal reset, forced default time scale, callback fallback, production focus-policy changes or selector visual changes without a violated production invariant.
- Review builds, standalone builds, campaign-content review and more aggregate PASS totals as substitutes for causal evidence.
- Byte matching irrelevant caches, restoring owner settings to make the test convenient, or committing another inconclusive rerun.

## 12. Final disposition

The integrated audit explains C, D's pre-scenario stop, E and the separate foreground stop; it identifies corrected fixture defects and a concrete shared-pointer candidate. A remains an ambiguous close-boundary failure. B remains a Brew delivery/guard-boundary gap with evidence against a persistent modal/pause lock. No production defect has been proven to cause either historical incident.

No production/harness code, visual, scene, package, setting, save, audio preference or protected configuration was changed by this audit. No Editor was closed or manipulated, no test was retried, no build or commit was created, and no unrelated file was staged. The only deliverable is this new consolidated report; historical evidence remains unchanged.

Final verification: all 63 protected baseline hashes still match; the index is empty; the only new untracked file is this report. Report reference checks resolved the explicit file paths, and the report has no trailing whitespace. A no-index diff check emitted no whitespace diagnostics (exit1 reflects the new-file difference). The repository-wide diff check reports existing whitespace in the preserved art metadata/font changes; those files were not normalized. Compilation was not rerun for this documentation-only addition.


## 13. Single handoff experiment execution attempt ? 2026-09-30

**EXPERIMENT_NOT_RUN: the initial foreground acquisition failed.** No Play Mode entry, test dispatch, virtual input, positive control or negative handoff fixture occurred. This is an execution-prerequisite result; it neither supports nor contradicts the shared-pointer hypothesis. Historical A/B retain their prior disposition.

### Experiment and discriminating observations

The requested experiment was the existing RunPointerHandoffDiagnostic, once at 1280?720 (?9.2): normal Brew/Back, then a competing mouse at (-100,-100), followed by Brew at the original virtual-mouse position. The shared-pointer candidate requires applied player button state and Click action with the UI's internal pointer remaining at the competing position, a missed target and callback zero. Missing player-state updates would instead indicate the earlier focus/input-delivery class. Correct pointer delivery followed by a Brew guard or immediate close would identify a production transition. A successful positive control alone would not establish handoff causation.

### Actual preparation and gate result

- Starting HEAD remained 57ad0bf4db2af9d0d3520b3155334fe6886f17bc on refactor/project-rebuild. Captured hashes of all 63 dirty tracked files and the existing uncommitted audit document before work.
- No running Editor was initially found. The two processes named unity were windowless command tools. Connected-Editor status independently reported zero instances.
- Launched the existing isolated project once through the installed Unity tool: Temp/PhaseMDiegetic/ValidationProject, Unity 6000.3.7f1. Identified the resulting Editor as PID 1704 through both executable path and connected-project status. No owner's Editor was reused or closed.
- Queried the Editor: correct project/PID, compiling=false, playing=false, pending=false, dirty=false. Prepared its Game View at 1280?720; that preparation returned focusedWindow=GameView.
- Rechecked all 1,251 original Assets/Packages/ProjectSettings files against the copy after startup: all matched. Production and harness sources were unchanged.
- The startup window handle changed before the first native acquisition. The stale-handle identity check stopped before ShowWindow, SetForegroundWindow or dispatch. Resolved the current window once from the same verified PID; this was not a second foreground request or a repeated experiment.
- Applied the supported native diagnostic correction in the one-shot host invocation: explicit HWND/DWORD-pointer argument and result types, checked GetWindowThreadProcessId's thread return and last error, and recorded the actual foreground handle/PID. No production input policy or harness scenario code was changed; historical host scripts were preserved.
- **2026-09-30T05:11:54.998907Z:** the single SetForegroundWindow request returned **false**. The subsequent correctly typed PID query succeeded: foreground HWND 265606, thread 9716, lastError 0, foreground PID **15140**, different from owned Editor PID **1704**. Thus this gate failed on an observed different foreground owner, not an uninterpreted PID zero.
- The host stopped before creating the dispatch script or calling RunPointerHandoffDiagnostic. No focus wait, second foreground request, click retry or callback substitute followed.
- Final read-only Editor query: correct project/PID, compiling=false, playing=false, pending=false, dirty=false, focusedWindow=NONE. Player Dynamic readiness and the positive-control trace were therefore not reached or claimed.

The actual execution budget consumed was one Editor launch, one initial foreground request and **zero experiment dispatches**. The prepared validation Editor was left open. Startup output was retained privately without printing credentials or process command lines.

### Evidence and preservation

Execution records are under Temp/PhaseMPointerHandoffExecution/: baseline.json, status.json, prepare-result.json, source-check.json, focus-acquisition.json and final-state.json. The native gate JSON retains the failed request and successful PID-query result. There is no new experiment trace/result because the scenario did not start. Startup/command outputs with a private filename are not review artifacts.

Before this append, all 64 captured file hashes still matched, including the entire existing audit document. This section was appended without rewriting its prior bytes. All 63 dirty tracked files, including .codex/config.toml, remain unchanged. No save reset, preference write or restoration was necessary because the harness was never invoked. No code, scene, visual, package, original setting, build, commit or staging change was made.

### Specific action needed to proceed

Manually activate the **ValidationProject Unity 6000.3.7f1 Editor (PID 1704 while this instance remains open)** and click its **Game** tab, keeping Play Mode stopped. Then resume this same single experiment task with that window available for a verified protected input segment. The next attempt must first observe its actual foreground PID and Game View state; do not assume the earlier window handle remains valid and do not repeat automatic focus requests. The zero-dispatch outcome leaves the one-scenario experiment budget unused.


## 14. Manual handoff experiment executed - 2026-09-30

**EXPERIMENT_RUN - the negative fixture reproduced the shared-pointer position mismatch.** The normal Brew/Back control succeeded, and the competing-device leg then demonstrated applied virtual-mouse input with a different position consumed by the UI module. This confirms the causal mechanism in this controlled fixture. It does not establish that historical A or B used that mechanism; their missing historical boundaries remain missing.

### Readiness, scope and execution

The user authorized one manual handoff lasting up to 60 seconds. The host identified the connected Editor by its exact ValidationProject path and version, obtaining current PID 1704 from live status rather than assuming the prior PID. Using explicitly typed native APIs, it observed foreground ownership and queried the selected Editor window only after the PID matched. Readiness was verified after 0.56 seconds: correct project/PID, GameView selected, compiling=false, playing=false, pending=false, dirty=false. No automatic foreground request was made.

All 1,251 source/assets/settings files still matched the original before dispatch. HEAD remained 57ad0bf4db2af9d0d3520b3155334fe6886f17bc. The executed harness hash remained 9a27d69363fe5d885f7858c7c27991f3a06cff3058c8de405a8c69a9e00f702e. The existing 1280x720 Game View was retained. The host invoked SURLHWAPhaseMUIValidation.RunPointerHandoffDiagnostic exactly once. No Editor tab, foreground terminal or window-selection operation was performed during the experiment. The entry point retained its existing initial GameView focus call before entering Play Mode; no subsequent refocus was added.

Budget: one scenario, three ModuleClick attempts (Brew, Back, negative Brew), no retries. The existing positive/negative fixture and public setup seams were unchanged. Brew/Back delivery used queued InputSystem mouse states; no direct callback was substituted for any tested click. The positive leg also verified that player Dynamic state, actions, the module pointer and the production callback were operational before the negative fixture.

Result record: **2026-09-30T05:28:45.2278903Z UI pointer-handoff False**. The False result is the expected fail-fast result of the deliberately negative input fixture, not a claim that a normal full gameplay route failed.

### Retained trace and causal boundaries

Trace: [20260930T052836955-pointer-handoff-FAIL.txt](../Temp/PhaseMPointerHandoffManual/20260930T052836955-pointer-handoff-FAIL.txt). It contains 88 lines, dropped=0, screen=1280x720. SHA256: 8a096d619c7be3cf71ada286a2debfb3c16f5115a9171420e445fc1294975b4f.

| Trace lines / frame / monotonic seconds | Actual observation |
|---|---|
| 15-41 / 2903-3024 / 12.147971-12.466403 | Positive Brew: mouse 18 Point, applied Dynamic state, correct module ray, Down/Up/Click, production Brew callback and selection-open completion; callback count 1. |
| 42-67 / 3024-3167 / 12.468321-12.776788 | Back/Cancel delivered once and closed selection normally. |
| 68-69 / 3168 / 12.781619 | Original mouse returned to Brew point (892,56). Existing fixture created competing mouse 19 and queued (-100,-100). |
| 70-72 / 3169 / 12.784193-12.785145 | Negative Brew began; intended/probe position was (892,56), but module internal pointer was id=19/device=19/pos=(-100,-100), ray NONE. |
| 73-78 / 3170-3171 / 12.787071-12.789759 | Mouse 18 released/pressed events arrived and applied to Dynamic player state at (892,56); Click performed for mouse 18. No new own Point action was recorded. |
| 80 / 3171 / 12.790665 | Module pointer identity changed to id=18/device=18, but its position remained (-100,-100), ray NONE, while mouse 18's actual player state was (892,56), left=1. |
| 81-84 / 3172 / 12.792678-12.793482 | Release applied and Click performed; internal pointer still (-100,-100), ray NONE; actual mouse state remained (892,56), left=0. |
| 85-88 / 3324 / 13.092481-13.097580 | callback count 0, immediate delivery-mismatch exception, classification FAILED and Finish(false). No fourth click or retry. |

Last confirmed correct boundary: mouse 18's applied Dynamic press/release and Click action. First incorrect boundary: the UI module retained the competing mouse's position while consuming mouse 18's button state. No target Down/Up/Click or Brew callback occurred on the negative leg, so a Brew guard or immediate selector close cannot explain this fixture's failure. No modal or pause lock remained; the target was active, interactable and allowed.

The competing mouse's Point action itself was between the local per-click observation windows. The fixture injection is recorded and the subsequent internal device-19 pointer state is directly recorded; this report does not invent an unlogged action event. The decisive new evidence, missing from the historical Next failure, is line 80's internal pointer/device position alongside the correctly applied original mouse state.

### Causal disposition and limits

- **CONFIRMED_CAUSE for this negative fixture:** sharing the UI pointer across mice allowed the competing position to remain when the original mouse supplied only unchanged-position/button events. Device identity switched back without the position switching back. This agrees with the installed OnPoint/OnLeftClick implementation cited in section 8, F05.
- **Ruled out for this negative attempt:** the earlier C-style failure to apply player input, production Brew guard rejection, selector opening then immediately closing, and a persistent modal/pause lock.
- **Historical later Next failure:** the experiment strengthens F05 by demonstrating the proposed mechanism with the internal pointer evidence. The old incident still lacks that internal position measurement, so its attribution remains a supported candidate.
- **Historical A/B:** unresolved at their previously identified boundaries. This deliberately constructed fixture is not a reproduction of either historical incident.
- No correction was implemented. A later narrow harness correction could require its pointer position to be established before pressing; this result does not justify a production pointer-policy, modal or visual change.

### Focus monitoring limitation

Every recorded state in this complete trace reports appFocus=true and editorFocus=GameView; there is no recorded focus-loss event or environment-stop classification. Thus focus loss is not the observed explanation for the negative result.

The one-shot host also attempted to arm the runner's existing foregroundArmed guard using a temporary Editor callback. No arming acknowledgment appeared in the Editor log. That temporary pre-Play callback was not a verified mechanism across the normal domain reload, so continuous focus-loss protection must **not** be claimed as validated for this run. The observed result is limited to the recorded focused interval. No extra run, refocus or retry was used to repair or conceal that orchestration limitation.

### Teardown and preservation

The harness logged '[Phase M UI] Progress and sound preferences restored and verified.' (Temp/PhaseMPointerHandoffExecution/editor-private.log:1975). A final read-only query independently returned playing=false, pending=false, compiling=false and dirty=false for the same validation project/PID. The existing snapshot comparison performed the save/audio restoration; no raw save payload was printed.

Before this append, all 64 captured hashes matched, including the prior audit document. This section preserves every prior report byte. All 63 dirty tracked files, including .codex/config.toml, remain unchanged; nothing was staged. No production/harness source, package, original setting, visual, build or commit was changed. The validation Editor remains open, and no original Editor was closed or manipulated.

Supporting execution records are under Temp/PhaseMPointerHandoffManual/: editor-identity.json, manual-readiness.json, source-check.json, dispatch.cs, before.json, results.txt, final-state.json and the retained trace. Startup/private command output and save snapshots are not published as review content.

## 15. Test pointer driver hardening - implementation and blocked runtime validation (2026-09-30)

### Scope and tested revision

Base HEAD: `57ad0bf4db2af9d0d3520b3155334fe6886f17bc`, branch `refactor/project-rebuild`. The sole source edit is `Assets/Code/Editor/SURLHWAPhaseMUIValidation.cs`; its SHA256 at compile validation is `8e91e0bbfbbb22c1f729eb2ea4ff7ead7c9d063541cfce4a6c0971823babe080`. This is an uncommitted test-driver/observer change. No production action binding, pointer policy, gameplay, modal policy or countertop staging changed.

Section 14 demonstrated a shared-pointer mismatch in a deliberately displaced fixture: the original mouse's applied button state and device identity reached the module, while its unchanged position did not replace the competing pointer position. The target callback was absent and fail-fast stopped execution. That mechanism justifies making normal test clicks establish and observe pointer readiness. It does not establish historical A/B's cause.

The installed Input System 1.18.0 module was checked directly: `InputSystemUIInputModule.cs:1903-1949` updates shared-pointer device metadata; `OnPoint` at 2146-2161 updates screen position; `OnLeftClick` at 2178-2190 updates button state without performing an equivalent mouse-position update. These are package-cache line references for the installed revision, not edits.

### Exact driver change

- `PointerAt` (driver:496) reads the live active module's existing pointer state using the existing diagnostic reflection. It verifies intended device identity, applied device position/button state, live pointer position, the actual current raycast button and hovered target. For a pressed pointer it also checks `pointerPress` and `eligibleForClick`; for released readiness it requires no existing press. A separate `RaycastAll` remains a preliminary eligibility check and is insufficient to establish readiness.
- `EstablishPointer` (519) queues one released move to three pixels left of the target, then one released move to the target through the existing InputSystem pipeline. It observes a fresh own Point action, an advanced Dynamic update and matching live module state before permitting a press. Observation has a one-second real-time bound; it does not repeatedly resend coordinates or retry a click.
- Normal `ModuleClick` (542) rechecks readiness immediately before its first press, checks the consumed press target after processing and retains the exact-one-callback assertion. Foreign Point/Click actions during the protected segment invalidate the action. The driver's own release is still queued for cleanup; there is no second press, callback fallback or silent repair after a failed click.
- No UI pointer internals are written. No physical/remote mouse is disabled and no operating-system input setting is changed.
- The existing negative fixture explicitly selects the original sequence: released target event, press, release. It bypasses the new readiness moves only for the planned negative control, preserving the demonstrated mismatch rather than repairing it.

### Separate planned controls and evidence requirements

| Case | Entry point | Required evidence | Result on this revision |
|---|---|---|---|
| A: normal control | `RunPointerControl` | Real UI Brew and Back delivery, each expected callback exactly once; actual pointer readiness before each first press | UNRUN: foreground prerequisite unavailable |
| B: original negative control | `RunPointerHandoffDiagnostic` | Original positive Brew/Back, competitor displacement, own device press with stale module position, missing Brew callback and expected fail-fast | UNRUN: foreground prerequisite unavailable |
| C: corrected control | `RunPointerReadyControl` | Equivalent displacement in its own planned run; fresh own Point and matching consumed pointer before the first click; real Brew callback exactly once | UNRUN: foreground prerequisite unavailable |

B and C use separate runs. C is not an automatic retry of B. The prior section 14 trace is evidence for the mechanism on the earlier driver; it is not a runtime PASS for B or C on this revision. There is no new pointer/callback trace because no scenario was dispatched.

### Focus observer lifetime and coverage

The existing runner now registers its input/focus observations in its post-reload lifetime, records `focus.observer.registered`, and arms the existing foreground guard inside the running coroutine after one frame (`focus.observer.armed afterDomainReload=True`). The existing application-focus callback, InputSystem after-update callback and LateUpdate checks remain the observation paths. Finish records disarming and the number of checks; OnDestroy removes the subscriptions/devices and clears the observer-active SessionState marker. This replaces reliance on an unverified temporary pre-Play callback surviving domain reload.

Pointer scenarios reject a non-GameView start before resetting validation saves, preserve the prior runInBackground value, and no longer force GameView focus. The foreground preflight checks immediately rather than spending another ten seconds waiting for focus. The host's manual handoff is the single readiness wait.

These registration, arming and cleanup paths were inspected and compiled. **Their runtime coverage remains unverified on this revision.** The final idle observer marker was false, which establishes idle state only; it does not prove exercised registration or cleanup.

### Validation performed and exact blocker

- COMPLETED: `dotnet build Assembly-CSharp-Editor.csproj --no-restore`, exit 0, zero errors, eight existing warnings (one MSB3277 assembly-version conflict and seven existing CS0252 warnings in `SURLHWAJ4DiscoveryValidation`). Scoped `git diff --check` passed and the complete driver diff was reviewed.
- COMPLETED: verified the existing isolated ValidationProject Editor identity from its actual project path, PID 1704 at this attempt. Verified stopped, not compiling, no pending validation and no dirty scene before copying only the changed test driver into that existing copy. Unity refreshed the script; the final read-only query confirmed the new `RunPointerReadyControl` method was loaded.
- BLOCKED: one manual readiness handoff of up to 60 seconds. `Temp/PhaseMPointerDriverHardening/readiness.json` contains 240 OS foreground samples, none matching the verified validation Editor PID. The final sampled foreground PID was 38520. Thus the host never reached its conditional Editor/GameView readiness query during that handoff, and readiness remained false. This does not establish which Editor tab the owner had selected in the background.
- UNRUN: all new-revision A/B/C controls; Brew/selection/change/Back/reopen regression; all four method-entry targets; Guide/Settings close and restoration; 1280x720 and 1920x1080 runtime regression. The existing diegetic regression includes those navigation/method checks; Settings close/restoration was added alongside the existing Guide check in each workspace. None is claimed to have run on this revision.
- FAILED: no runtime scenario result, because none started. The prerequisite failure is recorded separately from scenario failures. No input was injected, no callback was substituted, and no second wait/refocus/retry was attempted.

The final query (`Temp/PhaseMPointerDriverHardening/loaded-state.json`) returned compiling=false, playing=false, pending=false, observers=false, newDriver=true for PID 1704. Required next owner action: make the identified ValidationProject Editor the OS foreground application with its Game tab focused and Play Mode stopped for a newly coordinated bounded validation window. Reverify the live identity at that time; PID 1704 is evidence from this attempt, not a future constant.

### Preservation and remaining limits

All 64 captured pre-task file hashes matched immediately before this append: the 63 pre-existing dirty tracked files and the uncommitted audit document. All earlier report bytes are retained. `.codex/config.toml`, production assets/settings, original saves/audio preferences and the owner's Editor were preserved. No validation scenario started, so this attempt did not reset gameplay saves or audio preferences. The validation Editor remains open. Supporting baseline and readiness records are under `Temp/PhaseMPointerDriverHardening/`.

The driver is compile-validated only. Runtime correction efficacy, negative-control preservation under execution, callback counts, observer coverage and the requested regressions remain validation debt. Historical A/B retain their earlier unresolved boundaries. No production-gameplay fix or historical causal closure is claimed. The focused source change and this report append remain uncommitted because the task's required runtime validation is unavailable; prior unreviewed report content was not staged.

## 16. Owner-started Editor launcher (2026-09-30)

Added only `Assets/Code/Editor/SURLHWAPointerValidationLauncher.cs` and its metadata. Base HEAD remains `57ad0bf4db2af9d0d3520b3155334fe6886f17bc`. The existing uncommitted hardened driver and focus-observer lifecycle code are unchanged (driver SHA256 `8e91e0bbfbbb22c1f729eb2ea4ff7ead7c9d063541cfce4a6c0971823babe080`). Launcher SHA256: `c21547b528b1953f3b36f6c4fc508e0ab83374f949f5cacf679d46d1f72c4e05`.

### Owner runbook

Use the existing Unity Editor for `D:\univ\surlhwa\Surlhwa\Temp\PhaseMDiegetic\ValidationProject`.

| Menu under SURLHWA/Validation/UI Pointer | Registered shortcut | Existing runner |
|---|---|---|
| A - Normal Control | Ctrl+Alt+Shift+F6 | RunPointerControl |
| B - Expected Mismatch | Ctrl+Alt+Shift+F7 | RunPointerHandoffDiagnostic |
| C - Pointer Ready Control | Ctrl+Alt+Shift+F8 | RunPointerReadyControl |

Start with Play Mode stopped, pause cleared, no compilation/import, no unsaved scenes and no previous validation awaiting cleanup/restoration. Activate the validation Editor. For shortcuts, select its existing Game tab first. The runner loads BarScene and enters Play Mode itself.

Start A, then B, then C independently. Before each next launch, wait for Play Mode to stop and the Console messages `Focus/input observers removed; test devices removed` and `Progress and sound preferences restored and verified.` Do not switch tabs, move/click the pointer or leave the Editor during a control. Inspect Console after Play Mode stops.

Results, relative to that validation project, are `Temp/PhaseM/validation-results.txt` and `Temp/PhaseM/EventTraces/<timestamp>-<mode>-pass.txt` or `-FAIL.txt`. Modes are pointer-control, pointer-handoff and pointer-ready. A/C require their expected callback evidence and successful completion. B deliberately records a failure; count it as the expected negative only when its trace demonstrates the intended shared-pointer displacement, original-device button input and missing Brew callback. A generic failure, focus blockage or unrelated exception is insufficient.

A rejected launch logs `[UI Pointer Launcher] NOT STARTED:` with the required action and clears the request. It creates no scenario result. Post-dispatch focus loss is classified by the existing runner in its trace. A result line alone does not establish restoration completion.

### Launch contract and verification

The launcher checks the exact normalized project path, stopped/unpaused Play Mode, existing Phase M pending/observer flags, compilation/import, unsaved loaded scenes and application activity. It never saves/discards scenes. Keyboard launch requires Game View already focused. Menu launch allows at most one focus request to an existing Game View, after the menu closes; a separate deferred callback verifies application activity and exact focused Game View before dispatch. Two one-shot callbacks have a two-second expiry, with no polling loop. Unsafe conditions, lifecycle changes or duplicate launch requests cancel pending dispatch. Later focus alone cannot start a cancelled request. The selected existing runner is invoked once; no production callback substitute or in-test focus attempt is added.

Unity successfully compiled and loaded the final launcher in ValidationProject. Read-only enumeration confirmed all three actual menu registrations and the three shortcuts above, with no other registered binding matching those key combinations in the current profile. Final query: compiling=false, updating=false, playing=false, pending=false, launchQueued=false. Registration evidence: `Temp/PhaseMPointerLauncher/registration.json`. Only the new launcher and metadata were copied; driver/source metadata equality was verified, including the previous observer change. No divergent copy-only code exists.

No A/B/C control, timed focus handoff, standalone build or production change ran in this task. Registration/compilation is not runtime validation of launch focus, pointer delivery or controls. The pending runtime limitations from section 15 and historical Settings/Brew causal limits remain unchanged. All 65 captured pre-existing dirty-file hashes matched before this append, including the driver, protected config and prior report. This append preserves every prior report byte. No save/audio reset, original Editor manipulation, staging or commit was performed.

## 17. Pointer-ready held-state assertion investigation (2026-09-30)

### Executed source and retained evidence

Investigated only attempt `20260930T080347357-pointer-ready/3/BrewButton`. The matching trace is `Temp/PhaseMDiegetic/ValidationProject/Temp/PhaseM/EventTraces/20260930T080347357-pointer-ready-FAIL.txt`: 111 lines, dropped=0, 3840x2160, SHA256 `dc50ea1f6ef76b0990cbccc5ea12b387e9d0e9fb479e2d9f264dae90f957cc7c`. A byte-preserving copy and the pre-correction driver are retained in `Temp/PhaseMPointerAssertion/`.

The validation-copy driver and repository driver matched SHA256 `8e91e0bbfbbb22c1f729eb2ea4ff7ead7c9d063541cfce4a6c0971823babe080`, the compiled/synchronized revision documented in sections 15-16. Its exception is at line 609, matching the supplied stack. The installed module and PointerModel sources also match between repository and validation copy. The trace itself does not embed a source hash; source attribution uses this preserved source, prior synchronization record and matching exception/line/path evidence.

### Cause: incorrect per-button state observation while held

**Confirmed harness assertion defect explaining this failure.** This was neither a demonstrated coordinate divergence after readiness nor an assertion evaluated against the post-click SetupRequestNote raycast.

In the executed driver, line 594 assigns `validPress` once, after the pressed frame and before queueing release. Line 609 reports that latched value after the 0.3-second observation interval. The only other invalidation is `pointerConflict`: reset at line 553 and assigned only by `TraceAction` line 180 for a performed foreign-device Point/Click while protected. That conflict has its own exception at line 608; the trace reports conflict=NONE. No late target-change callback sets `validPress`.

`PointerAt` line 511 incorrectly treated `eventData.pointerPress` as retained left-button state. In the installed InputSystemUIInputModule, ProcessPointer copies/handles left at 475-491, then right at 494-499, then middle at 501-506. PointerModel.ButtonState.CopyPressStateTo (260-280) replaces the shared eventData.pointerPress with each button's stored m_PressObject. The same eventData instance is scratch state for all three buttons. After a changed mouse frame, its press reference describes middle, whose press is null in this left-only sequence. The valid left press is retained separately in state.leftButton.m_PressObject by CopyPressStateFrom (283-294). This makes the old held assertion false even with a correctly consumed left click. The eventData pointer position and current ray remain useful shared-pointer observations; per-button press ownership must come from the correct button record or that button's actual event callback.

The old trace does not log each PointerAt subpredicate or the scratch-versus-left press references at line 594. The installed source proves the incorrect ownership assumption, and the following timeline places that check before release. The correction adds those exact missing observations so future evidence does not depend on reconstructing scratch-state semantics from code.

### Supported timeline

| Trace lines | Frame / monotonic seconds | Boundary |
|---|---|---|
| 2-3 | 2-3 / 9.227404-9.284398 | Focus observer registered, then armed after reload. |
| 74-77 | 2112 / 18.854396-18.856486 | Intentional fixture displacement: shared pointer device 22 at (-100,-100); intended device 21 at (2676,168). This is before readiness, not an injected click. |
| 78-86 | 2113-2114 / 18.860744-18.868016 | Genuine changed-position events move own device through (2673,168) to (2676,168); fresh Point observed. Readiness confirms device 21, live UI position (2676,168), ray BrewButton. |
| 89-94 | 2115 / 18.872663-18.874349 | Press applies; PointerDown reaches BrewButton at (2676,168). Line 594 evaluates and latches false between PointerDown (18.873522) and release queue (18.874046). The held-frame trace still shows device/UI positions equal and ray BrewButton. This is the first erroneous assertion boundary, reconstructed from exact source order. |
| 95-104 | 2116 / 18.878782-18.882706 | Release applies; PointerUp has BrewButton current/press/click targets, eligible=true, dragging=false. Brew callback enters, panel opens, callback observer counts 1 and PointerClick is delivered. |
| 106 | 2117 / 18.893042 | First ray change to SetupRequestNote, after callback. Device/UI positions still (2676,168). |
| 107-111 | 2183 / 19.176231-19.186848 | Latched validPress=false reported with callbacks=1, conflict=NONE; exception, focus disarm (4361 checks), FAILED and Finish(false). |

Focus remained appFocus=true/editorFocus=GameView in the recorded interval. Input/action observation for attempt 3 was enabled at ModuleClick entry and disabled after attempt.end; foreign-action protection was active only for this corrected-sequence leg in the old source. The focus observer has an independent lifetime from frame 3 through frame 2183.

### Why the first Brew passed

The first Brew (and Cancel) in this C fixture explicitly use `originalSequence:true` at old lines 899/901. That preserves the original positive setup preceding the deliberately displaced pointer. It bypasses EstablishPointer, sets protectPointer=false, short-circuits validPress to true at line 594 and skips the normal held assertion at lines 606-610. It still requires exactly one callback. Attempt 3 uses `originalSequence:false`, performs readiness, then hits the defective held-state assertion.

Both Brew deliveries legitimately change the subsequent ray to SetupRequestNote: first at frame 1971, second at frame 2117. The first pass is therefore not evidence that the same held assertion previously accepted that transition. It did not execute that assertion. Removing a post-transition target check would not fix the actual failing line, because no such check assigned this failure.

### Exact test-only correction

Only `Assets/Code/Editor/SURLHWAPhaseMUIValidation.cs` changed:

- `PointerAt` (current line 499) retains active module/actions, device position/button, UI device/position, live ray and hover readiness checks. It now reads `leftButton.m_IsPressed` and `m_PressObject`; held readiness also rejects ignored clicks and dragging. It no longer reads the shared scratch pointerPress as retained left-button ownership.
- The existing same-object event observer now checks actual PointerUp data before the production click callback: own device, left-button release, device/UI coordinates, current target, press target, click eligibility and no drag. Exactly one release observation is required. The guard remains independent of callback count.
- The foreign-input protected segment ends at that release observation. Callback delivery and the subsequent screen transition remain traced; no assertion requires BrewButton to remain active or under the pointer after it opens BrewPanel. The existing exact callback count remains required, and a pre-release conflict remains latched/failing.
- Added `pointer.held.check` (621) and `pointer.release.check` (599), plus scratch eventButton/eventPress and stored leftPressed/leftPress in the existing pointer snapshot. No new diagnostic framework, internal-pointer writes or synthetic callback invocation.
- Original-sequence B input ordering and callback fail-fast remain unchanged. Its held/release guard bypass remains explicit; diagnostic observations do not repair its intentional displacement.

### Control evidence and validation limits

| Case | Owner-run evidence on prior source 8e91e0... | Corrected source 644560... |
|---|---|---|
| A normal | `20260930T080138653-pointer-control-FAIL.txt`: first Brew callback=1 but validPress=false; same held assertion failure | UNRUN: execution prerequisite unavailable |
| B original negative | `20260930T080252949-pointer-handoff-FAIL.txt`: own mouse 19 at (2676,168), shared UI pointer retained (-100,-100) on press/release (lines 86/90), callback=0, exact delivery-mismatch fail-fast. This is the intended negative mechanism, not merely a generic FAIL. | UNRUN: execution prerequisite unavailable; original sequence preserved in source |
| C pointer-ready | Investigated trace: readiness restored matching coordinates; all three callbacks=1; defective held assertion failed | UNRUN: execution prerequisite unavailable |

Corrected driver SHA256: `644560ed4748a60f3bee87becce9d15e996aa2e366eb3e5d16834959649fcecc`. Unity compiled and loaded this correction; read-only reflection verified the new observer field and all required installed left-button fields. Scoped diff/whitespace review passed. Repository/copy source bytes match. These checks are compilation/source checks, not runtime A/B/C results.

The single post-compile prerequisite query identified the current ValidationProject Editor by actual path, PID 41408 (not the previous PID). It returned active=false, focused=null, playing=false, compiling=false, updating=false, pending=false, observers=false, dirty=false. No control was dispatched and no timed focus handoff/refocus/retry was attempted. To obtain corrected-revision evidence, the owner can activate that ValidationProject's Game tab and start the existing A, B, C menus/shortcuts from section 16, waiting for each prior run's full cleanup. No terminal response is required.

### Cleanup and preservation

The failing C trace proves focus disarm/Finish(false); it does not contain the later restoration message because the trace is written before Play Mode exits. The current read-only state confirms no playing/pending validation or armed observer, but is not a replacement for a per-run restoration log. This task started no runtime control and performed no save/audio reset. Existing progress/audio restoration code is unchanged.

All captured dirty-file hashes except the intentionally corrected driver matched before this report append. The original trace and pre-correction source are retained; prior report bytes are unchanged. Launcher, protected config, unrelated files, production code, visuals and Editor focus policies are unchanged. No Editor was closed or brought to the foreground. No build, staging or commit was made. The correction remains runtime-unvalidated. Historical Settings-close and Brew incidents remain separate and unresolved at their previously documented boundaries.

### Corrected-revision validation reconciliation and owner handoff

Checked at 2026-09-30T08:31:24.867237+00:00. HEAD remains `57ad0bf4db2af9d0d3520b3155334fe6886f17bc` on `refactor/project-rebuild`. All 67 captured pre-existing dirty-file hashes matched before this append. Driver/observer and launcher are unchanged and match the validation copy, including their metadata. Driver SHA256: `644560ed4748a60f3bee87becce9d15e996aa2e366eb3e5d16834959649fcecc`; launcher SHA256: `c21547b528b1953f3b36f6c4fc508e0ab83374f949f5cacf679d46d1f72c4e05`.

No owner-completed corrected-revision results were found. The only results remain the three prior-source runs at 08:01:47, 08:03:02 and 08:03:57 UTC, already evaluated above. No run was duplicated.

| CONTROL | EXECUTED SOURCE | ACTUAL RESULT | KEY EVIDENCE |
|---|---|---|---|
| CONTROL_A | None this validation; 644560ed... ready | UNRUN / owner start required | Corrected observer loaded; Editor inactive with no focused window |
| CONTROL_B | None this validation; 644560ed... ready | UNRUN / pending A completion | Prior-source negative result is not credited to this corrected revision |
| CONTROL_C | None this validation; 644560ed... ready | UNRUN / pending A and B completion | No corrected-revision trace exists |

Read-only live verification identified ValidationProject PID 41408, with compiling=false, updating=false, playing=false, paused=false, pending=false, observers=false, launchQueued=false, runners=0 and dirty=false. The corrected observer was loaded (assembly MVID `aa523e09-fe15-48ef-a73c-c2f2d0cfe6ab`). All three menu paths and actual Ctrl+Alt+Shift+F6/F7/F8 bindings were enumerated and matched section 16. Evidence is in `Temp/PhaseMPointerValidationReconcile/latest-state.json` and `sources.json`.

**READY_FOR_OWNER_RUN: CONTROL_A.** Activate `D:\univ\surlhwa\Surlhwa\Temp\PhaseMDiegetic\ValidationProject`, select Game with Play Mode stopped, then use `SURLHWA/Validation/UI Pointer/A - Normal Control` or Ctrl+Alt+Shift+F6. No terminal reply is needed after focusing. Before B or C, require the prior run's observer/device cleanup and verified progress/sound restoration; a result line alone is insufficient. Existing result and trace locations remain unchanged.

No tests, timed focus waits, source edits, save/audio resets, builds or commits occurred in this reconciliation. Source changes remain pending and runtime-unvalidated. Prior report bytes and all other protected baseline files are preserved. Production is unchanged. HISTORICAL_SETTINGS_A and HISTORICAL_BREW_B remain separate and unresolved.

### Owner-completed controls: retained B/C acceptance and missing A assertion evidence

Reviewed the exact owner-reported completions at 2026-09-30 08:36:53.2272611Z (CONTROL_A), 08:42:33.8740786Z (CONTROL_B) and 08:44:49.6333242Z (CONTROL_C). No tests, focus handoff, build or code change was performed during this review.

| CONTROL | EXECUTED SOURCE IDENTITY | ACTUAL RESULT | KEY EVIDENCE |
|---|---|---|---|
| CONTROL_A | Correction was loaded before the reported run; per-run assertion/source record missing | A_EVIDENCE_MISSING; owner reports True | Matching detailed trace/result file did not survive. Previous Editor log corroborates a later normal-control completion, Brew/Cancel callbacks=1 and restoration, but does not retain held/PointerUp decisions or the exact UTC result line. |
| CONTROL_B | Corrected driver 644560ed...; matching corrected trace markers and unchanged source/assembly | EXPECTED_FAILURE_CONFIRMED | Exact surviving result at 08:42:33.8740786Z; own-device/UI coordinate mismatch, Brew callback=0 and immediate callback-delivery failure. Cleanup/restoration verified. |
| CONTROL_C | Same corrected driver/assembly as B | PASS | Exact surviving result at 08:44:49.6333242Z; corrected third Brew has readiness, held and PointerUp decisions true, callback=1 and expected screen transition. Cleanup/restoration verified. |

**TEST_HARNESS_CORRECTION: VALIDATION_INCOMPLETE (A_EVIDENCE_MISSING).** B/C evidence remains accepted; do not rerun them merely to replace missing A evidence. No validated commit is justified yet.

#### Durable evidence preservation and source agreement

Relevant safe evidence was copied without overwriting prior records to:
`Logs/Review/PointerControls/20260930T085355860813Z/`
This is outside both Unity Temp directories. It contains the exact surviving B/C traces/results, source snapshots, pre-run source/state records, current idle-state query and filtered per-control Console/cleanup excerpts. No full Editor/process log, save payload, credential or command-line dump was copied. `owner-reported-A-summary.txt` explicitly records the owner-message provenance of A's summary. Manifests retain artifact SHA256 values.

Repository and validation-copy driver, embedded observer, launcher and metadata remain byte-identical. Full driver SHA256: `644560ed4748a60f3bee87becce9d15e996aa2e366eb3e5d16834959649fcecc`; launcher SHA256: `c21547b528b1953f3b36f6c4fc508e0ab83374f949f5cacf679d46d1f72c4e05`. Current loaded Editor assembly MVID `aa523e09-fe15-48ef-a73c-c2f2d0cfe6ab` equals the saved 08:31 pre-A query. Corrected held/release trace markers corroborate B/C execution on that implementation. Traces do not embed a full source hash; identity is supported by pre/post source equality, loaded assembly identity and those implementation-specific markers.

The owner restarted the Editor after A. B/C appear in the current Editor log, while the corroborating A completion is in Editor-prev.log. These controls are not claimed to share one uninterrupted Editor session. Current verified ValidationProject PID is 44140, distinct from the pre-A PID 41408.

#### CONTROL_B: exact intended failure

Trace `20260930T084227060-pointer-handoff-FAIL.txt`, SHA256 `4623390473697e018c96864c2b267f07a1598b69ec76768634bdc7ea3b6f3f14`, records 100 lines, dropped=0, actual input-test screen=3840x2160.

- Initial Brew and Cancel each deliver one callback (lines 45/73). All three attempts explicitly use originalSequence=true: their held/release booleans are bypass values, not corrected-assertion coverage.
- Competing mouse 19 displaces the shared pointer to (-100,-100), line 76. On original mouse 18's press, its applied position is (2676,168), while UI pointer identity switches to mouse 18 but position stays (-100,-100), ray NONE and leftPress NONE (lines 87-90, frame 1377). The same mismatch persists through release (93-94, frame 1378).
- Brew callback count is zero at the first delivery assertion, followed by the exact Input-module callback delivery mismatch exception (96-97, frame 1452). No retry, callback fallback, readiness repair, focus-loss classification or timeout appears.
- Focus registration/arming are lines 2/3. Disarm is line 98, wasArmed=True, checks=2899. Every recorded focus state is appFocus=True/GameView.
- Current Editor.log lines 1908 and 1930 show observer/test-device removal (checks=2899) and progress/sound preferences restored and verified before the next control's dispatch at line 1951. Safe excerpts are in `CONTROL_B-cleanup-console.txt`.

#### CONTROL_C: corrected assertion coverage

Trace `20260930T084442863-pointer-ready-pass.txt`, SHA256 `d47aaa8d7506402a62685c7a7df2bc9eef46441cda8ff6a4365f8e6efece1be2`, records 115 lines, dropped=0, actual input-test screen=3840x2160.

- Initial Brew and Cancel each deliver one callback through the preserved original sequence (45/73); these two clicks do not exercise the corrected held/PointerUp guards.
- Attempt 3 is the corrected click. After deliberate competitor displacement (76), real own-device moves restore readiness (88, frame 1346): own device 20 and live UI pointer agree at (2676,168), ray BrewButton, fresh Point=True.
- Held decision passes with originalSequence=False (95, frame 1347). The diagnostic directly distinguishes shared scratch eventButton=Middle/eventPress=NONE from leftPressed=True/leftPress=BrewButton. This is the exact state the previous assertion misread.
- Actual PointerUp arrives with the intended BrewButton ray/press/click target (101); release decision is true, count=1, device/UI coordinates equal (102, frame 1348). These precede the production Brew callback/screen transition. Callback count=1 and real PointerClick follow (107-108).
- The next-frame ray legitimately becomes SetupRequestNote (110); attempt.end retains validPress=True/validRelease=True/conflict=NONE and callback=1 (111). control.C completion and PASS follow (112-115).
- Focus registered/armed at 2/3, disarmed at 113 with checks=2823. No recorded focus loss. Current Editor.log lines 2620/2643 show observer/test-device removal (checks=2823) and verified progress/sound restoration. Safe excerpts are in `CONTROL_C-cleanup-console.txt`.

Fixture setup/dialogue advancement still uses the pre-existing setup seams. The cited Brew/Cancel actions are queued virtual mouse delivery through the UI module, not human-input or standalone evidence. Input resolution comes from trace headers; capture file sizes do not establish additional 720p/1080p coverage.

#### CONTROL_A: exact remaining gap and next owner action

Searched surviving test records under repository Temp, Logs and Docs, including the validation copy, and read only relevant test-message excerpts from the current and previous Editor logs. No matching 08:36 normal-control EventTrace or saved assertion report was found. The old `20260930T080138653-pointer-control-FAIL.txt` is not substituted. The surviving dialogue capture is setup evidence only.

Editor-prev.log lines 5382-6021 retain a later RunPointerControl dispatch, Brew/Cancel callback counts of one with released-move/press/release events, COMPLETE, observer/device cleanup (checks=2747) and verified progress/sound restoration. These corroborate the owner's reported completion but do not recover the detailed held/PointerUp decisions, readiness/focus-arming trace or exact 08:36:53 result timestamp. No inference from this COMPLETE summary or C's trace substitutes for A's required assertion evidence.

Request only **one owner-started CONTROL_A rerun** using the already registered `SURLHWA/Validation/UI Pointer/A - Normal Control` or Ctrl+Alt+Shift+F6, in ValidationProject with Game focused and Play Mode stopped. Retain its EventTrace before restarting the Editor, together with observer/device cleanup and verified progress/sound restoration. B/C need no rerun. This review did not launch the requested replacement run or wait for focus.

#### Preservation and commit disposition

Current read-only state: playing=false, pending=false, observers=false, launchQueued=false, runners=0, compiling=false, updating=false, dirty=false. B/C per-run cleanup is independently supported by the preserved log messages; the current state alone is not used as their proof. All 67 pre-existing dirty-file hashes matched before this append. Earlier report bytes are preserved. No source or protected config change, save/audio reset, production/visual change, Editor closure, staging or commit occurred. Existing task changes remain pending. HISTORICAL_SETTINGS_A and HISTORICAL_BREW_B remain separate and unresolved.

### Final bounded correction validation: owner-run CONTROL_A evidence recovered by replacement run

**TEST_HARNESS_CORRECTION: VALIDATED.** This closes the test pointer-driver/left-button assertion correction only. It supersedes the pending A-evidence status above without changing or deleting that historical record.

| CONTROL | Exact accepted completion (UTC, 2026-09-30) | Result | Assertion/delivery coverage |
|---|---|---|---|
| CONTROL_A | 09:01:05.6836343 | PASS | Both Brew and Cancel use originalSequence=False; readiness, stored-left held state and actual PointerUp pass; callbacks=1 each; cleanup/restoration verified. |
| CONTROL_B | 08:42:33.8740786 | EXPECTED_FAILURE_CONFIRMED | Preserved original sequence produces own-device/UI coordinate mismatch, zero intended Brew callbacks and first delivery-mismatch failure; cleanup/restoration verified. |
| CONTROL_C | 08:44:49.6333242 | PASS | Corrected third Brew restores displaced pointer readiness, passes held/PointerUp checks and delivers once; cleanup/restoration verified. First two setup clicks explicitly use the original sequence. |

The newest A trace is `20260930T090059235-pointer-control-pass.txt`, SHA256 `f7f4042026e1d53f1d87c903e4d23c480c7cc20fa09cf0d553ebefc08d1e4066`: 89 lines, dropped=0, actual input-test resolution 3840x2160. Both owner-created A traces (08:59 and 09:00 starts) were preserved; acceptance here uses the newest execution requested by the owner. Neither was launched or retried by the reviewing agent.

A evidence boundaries:

- Brew: readiness line 28/frame 1175; held line 35/frame 1176 has valid=True/originalSequence=False, eventButton=Middle/eventPress=NONE but leftPressed=True/leftPress=BrewButton. PointerUp line 41 precedes release check True/count=1 at line 42/frame 1177, then production callback at 43, callback observer count=1 at 47 and real PointerClick at 48. SetupRequestNote becomes the subsequent ray at frame 1178; attempt.end is validPress=True/validRelease=True/conflict=NONE (51).
- Cancel: readiness line 63/frame 1244; corrected held decision True/originalSequence=False at 70/frame 1245; actual PointerUp at 76 and release check True/count=1 at 77/frame 1246, before selection.cancel at 78. Callback count=1 (81); attempt.end passes both decisions (85).
- Focus observer registration/arming at lines 2/3; disarm wasArmed=True/checks=2621 at 87, then PASS/Finish(true). No focus-loss or competing-input failure appears.
- Matching latest-run Editor log excerpts retain completion at line 3975, observer/test-device removal with checks=2621 at 4000, and `Progress and sound preferences restored and verified.` at 4023. These are preserved as `CONTROL_A-cleanup-console.txt`, not inferred from the summary boolean.

New A archive, outside both Unity Temp directories: `Logs/Review/PointerControls/20260930T090240290085Z/`. It contains both surviving A traces, exact results, reviewed source snapshots, cleanup excerpt, final idle state and hash manifests. Existing B/C archive `Logs/Review/PointerControls/20260930T085355860813Z/` remains intact. B/C were neither rerun nor replaced with A's evidence. Only safe relevant evidence was copied; no full Editor logs or save payloads are review artifacts.

Source identity is unchanged across repository, validation copy and archived A/B/C sources: driver/embedded observer SHA256 `644560ed4748a60f3bee87becce9d15e996aa2e366eb3e5d16834959649fcecc`; launcher SHA256 `c21547b528b1953f3b36f6c4fc508e0ab83374f949f5cacf679d46d1f72c4e05`. The loaded assembly MVID remains `aa523e09-fe15-48ef-a73c-c2f2d0cfe6ab`, matching the pre-control record. Source attribution uses these preserved comparisons and trace-specific corrected decisions, not a claim that the trace embeds a full source hash. The owner restarted between earlier A and B/C; no uninterrupted-session claim is made.

Final read-only query verifies no Play Mode, pending validation, observers, queued launcher, runner, compilation/import or dirty scene. Per-control log excerpts independently prove cleanup/restoration for all three accepted controls. No production code, input/focus policy, visuals, persistence behavior or protected configuration changed. No tests, focus waits or builds ran during this verification. HISTORICAL_SETTINGS_A and HISTORICAL_BREW_B remain unresolved. No full-route, four-method, 720p/1080p regression, campaign, human-input or standalone validation is implied.

Commit review covers the test driver, owner launcher and metadata, and this consolidated report. The report's previously untracked historical sections were read for commit review and retained byte-for-byte; only this closure was appended. All unrelated dirty files are excluded. Existing broader regression limitations remain documented. Local evidence archives/caches and protected baseline files are not staged.
