# SURLHWA PROJECT AUDIT

Last Updated: 2026-09-07

Status: COMPLETE — STATIC REPOSITORY AUDIT

---

## 1. Audit Scope

이 문서는 2026-08-11 현재 Repository를 리빌딩의 Source of Truth로 삼아 수행한 Phase 0/1 Audit 결과다.

- 범위: Git, Docs, Assets, Packages, ProjectSettings, 모든 custom C# source, Unity text serialization이 가능한 Scene/Prefab/ScriptableObject, GUID reference, PlayerPrefs, TMP/encoding.
- 방법: 파일 전문 검토, ripgrep 호출/참조 검색, .meta GUID 역추적, Unity YAML object/fileID/parent/reference 분석.
- 제외: Unity Editor 실행, Play Mode, 빌드, 자동 formatting, import/reimport, asset 이동/삭제, code/Scene/Prefab/ProjectSettings 수정.
- 증거 등급:
  - CONFIRMED: 현재 파일, 코드 호출, YAML 또는 Git으로 직접 확인.
  - LIKELY: 정적 증거는 강하지만 Editor/Play Mode 확인이 남음.
  - NEEDS_VERIFICATION: Play Mode, Inspector 또는 사용자 결정이 필요.
  - UNKNOWN: Repository만으로 결정 불가.
- 이번 Audit에서 변경한 유일한 파일은 Docs/08_PROJECT_AUDIT.md다.

중요: 이 문서에서 “구현됨”은 별도 문구가 없으면 정적 구현과 연결이 존재한다는 의미다. 실제 런타임 성공은 Play Mode를 실행하지 않았으므로 NEEDS_VERIFICATION이다.

## 2. Git Baseline

Audit 시작 시 어떤 분석보다 먼저 다음을 실행했다.

    git status --short --branch
    git branch --show-current
    git log -5 --oneline

| 항목 | 결과 | 판정 |
| --- | --- | --- |
| Branch | main, origin/main tracking | CONFIRMED |
| Latest commit | 4098c7f docs: establish project rebuild documentation | CONFIRMED |
| Previous commits | ef26aa7 add: 어린왕자; c2cac6b feat: spawn little prince on second day; 1b0b6e8 feat: apply front-facing bar layout to main scene; 05e6576 feat: block out front-facing bar layout | CONFIRMED |
| Modified | 없음 | CONFIRMED |
| Untracked | 없음 | CONFIRMED |
| Staged | 없음 | CONFIRMED |
| Pre-rebuild unresolved changes | 없음; 시작 Working Tree clean | CONFIRMED |
| Tracked files | 462 total: Assets 412, Docs 11, Packages 2, ProjectSettings 27, UserSettings 5, root files 5 | CONFIRMED |

Repository에는 UserSettings 5개가 이미 tracked 상태다. 이번 Audit에서는 수정하지 않았지만 rebuild rule과 충돌하므로 향후 별도 Git hygiene 결정이 필요하다.

## 3. Project Tree

### 3.1 Unity / package overview

| 항목 | 실제 값 |
| --- | --- |
| Unity | 6000.3.7f1, revision 696ec25a53d1 |
| Render pipeline | URP 17.3.0, Renderer2D asset 사용 |
| Input | Input System 1.18.0 설치, activeInputHandler 2(Both); gameplay mini game은 legacy Input.GetKey 계열 사용 |
| Build scenes | Assets/Scenes/SampleScene.unity 한 개만 enabled |
| 주요 Unity packages | 2D feature 2.0.2, URP 17.3.0, Input System 1.18.0, uGUI 2.0.0, Visual Scripting 1.9.9, Timeline 1.8.10, Test Framework 1.6.0 |
| Third-party package | manifest 기준 별도 Git/local third-party package 없음 |
| Product | DefaultCompany / surlhwa |

### 3.2 Root

    Surlhwa/
    ├─ Assets/                 Unity tracked content
    ├─ Docs/                   11 documents
    ├─ Packages/               manifest.json, packages-lock.json
    ├─ ProjectSettings/        27 tracked settings
    ├─ UserSettings/           5 tracked local settings — repository hygiene risk
    ├─ Library/ Logs/ Temp/    generated/ignored
    ├─ obj/ .vs/ .idea/        generated/ignored
    ├─ Assembly-CSharp.csproj
    ├─ Surlhwa.sln             generated/ignored
    └─ README.md, LICENSE

### 3.3 Assets

    Assets/
    ├─ Assets/                         project-owned but redundantly nested
    │  ├─ Art/
    │  │  ├─ Cocktails/
    │  │  ├─ Minigame/Common/{Backgrounds,Liquids,Props}
    │  │  ├─ Minigame/{Shake,UI}
    │  │  └─ UI/
    │  ├─ Data/{Ingredients,Recipes}
    │  └─ Prefab/
    │     ├─ Character/
    │     ├─ Funiture/                 misspelling
    │     ├─ Minigame/
    │     └─ UI/
    ├─ Code/
    │  ├─ Customer/
    │  ├─ Data/{Cocktails,Ingredients,Recipes}
    │  ├─ Dialogue/
    │  ├─ Managers/
    │  ├─ MiniGames/
    │  ├─ Seats/
    │  ├─ Services/
    │  ├─ UI/
    │  └─ .idea/                       6 ignored IDE files inside Assets
    ├─ Data/{Cocktails,Dialogue,Orders}
    ├─ Scenes/SampleScene.unity
    ├─ BarScene_LayoutTest.unity
    ├─ preset/sample_sprite.png
    ├─ texture/Tiles/
    ├─ Settings/
    ├─ TextMesh Pro/
    ├─ DefaultVolumeProfile.asset
    ├─ InputSystem_Actions.inputactions
    └─ UniversalRenderPipelineGlobalSettings.asset

### 3.4 Ownership classification

| 분류 | 경로/내용 | 판정 |
| --- | --- | --- |
| Project-owned | Assets/Assets, Assets/Code, Assets/Data, Assets/Scenes, Assets/BarScene_LayoutTest.unity, Assets/texture, Assets/preset | CONFIRMED |
| Unity managed/project config | Assets/Settings, render pipeline assets, InputSystem_Actions, DefaultVolumeProfile, ProjectSettings, Packages | DO_NOT_TOUCH without scoped phase |
| TextMesh Pro | Assets/TextMesh Pro 기본 리소스 + project-added NotoSansKR copy | 기본 리소스 DO_NOT_TOUCH |
| Third Party | 별도 third-party root/package 없음 | CONFIRMED |
| Generated | root Library, Logs, Temp, obj, .vs, .idea, generated solution/project; Assets/Code/.idea | CONFIRMED |
| Prototype/Test | Test-prefixed data, Minigame concept images, preset/sample_sprite, BarScene_LayoutTest, OldLayout_Disabled | CONFIRMED naming/state |
| Backup | MainScene_Backup_BeforeBarLayout 또는 유사 backup Scene 없음 | CONFIRMED ABSENT |

## 4. Custom Scripts Inventory

Custom C#은 49 files / 8,150 lines다. namespace 선언은 한 개도 없고 모두 global namespace다. “Referenced by”는 주요 code/YAML owner를 압축 표기했다.

| File | Namespace | Class / Base | Primary responsibility | Serialized dependencies | Runtime dependencies / Referenced by | Domain | Current status | Notes / action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Code/Customer/CustomerController.cs | global | CustomerController : MonoBehaviour | 이동, 좌석, 상호작용, patience, 반응, 퇴장 | sprites, DialogueData, OrderData, reaction DB, anchors, popup prefabs, timeout text | SeatManager, InteractionUI, MiniGameManager, GameManager, ResultToast; 3 customer prefabs | Customers | ACTIVE_WITH_RISK | 853 lines; special/tutorial response와 generic customer 책임 혼재. REFACTOR |
| Code/Data/Cocktails/CocktailData.cs | global | CocktailData : ScriptableObject | 칵테일 표시/감정/visual 데이터 | steps, emotionTags, cocktailSprite | Recipe/Order/Customer reaction/Codex; 4 assets | Data | ACTIVE | Recipe와 name/description/method 중복. REFACTOR |
| Code/Data/DialogueData.cs | global | DialogueData : ScriptableObject | enter/success/fail lines | string arrays | Customer prefab 3개가 asset 2개 참조 | Data | PARTIAL | 현재 enter bubble 비활성, active serve path도 사용 안 함. KEEP |
| Code/Data/IMiniGame.cs | global | IMiniGame | mini game 공통 시작 계약 | 없음 | MiniGameManager, 5 implementations | Bartending | ACTIVE | KEEP |
| Code/Data/Ingredients/IngredientData.cs | global | IngredientData : ScriptableObject | ingredient identity/category/icon | icon | shelf, jigger, sessions; 12 assets | Data | ACTIVE | 모든 icon null. KEEP |
| Code/Data/MiniGameResult.cs | global | MiniGameResult : struct | success/rep/grade/composition 결과 | 없음 | 모든 mini game, OrderManager | Bartending | ACTIVE | KEEP |
| Code/Data/MiniGameType.cs | global | MiniGameType : enum | Build/Float/Stir/Shake 식별 | 없음 | Order, Recipe, managers, UI | Bartending | ACTIVE | CP949 source. KEEP after encoding phase |
| Code/Data/OrderData.cs | global | OrderData : ScriptableObject | 주문명, method, steps, target, default session | CocktailData, inline RecipeStep/BrewSession | InteractionUI, OrderManager, Customer prefab; 5 assets | Data | ACTIVE_WITH_RISK | current UI는 4 test orders만 사용; LP order는 dormant. REFACTOR |
| Code/Data/Recipes/BrewComposition.cs | global | BrewComposition / BrewIngredientEntry | 실제 제조 결과 DTO | 없음 | mini games, RecipeMatchService | Bartending | ACTIVE | KEEP |
| Code/Data/Recipes/BrewSessionData.cs | global | BrewSessionData / BrewSessionIngredient | 선택 ingredient/amount session DTO | IngredientData | UI, orders, mini games | Bartending | ACTIVE | KEEP |
| Code/Data/Recipes/CocktailRecipeData.cs | global | CocktailRecipeData : ScriptableObject | recipe identity, ratios, match/unlock config | CocktailData, entries | RecipeUnlock, Match, Guide, Codex; 4 assets | Data | ACTIVE | current recipe Source of Truth이나 Cocktail/Order와 중복. REFACTOR |
| Code/Data/Recipes/RecipeUnlockManager.cs | global | RecipeUnlockManager : MonoBehaviour | recipe list, unlock persistence | allRecipes | PlayerPrefs, guide/codex/order/day; Scene | Progression | ACTIVE_WITH_RISK | data folder에 manager 위치, direct save. REFACTOR |
| Code/Data/RecipeStep.cs | global | RecipeStep | legacy step tuning | inline fields | Order/Cocktail data, GaugeStop | Data | PARTIAL | current 4 mini games는 대부분 무시. KEEP pending schema review |
| Code/Data/RecipeStepType.cs | global | RecipeStepType : enum | step kind | 없음 | RecipeStep/GaugeStop | Data | PARTIAL | CP949 source. KEEP pending schema review |
| Code/Data/SpecialGuestData.cs | global | SpecialGuestData | id/prefab/reputation inline definition | prefab | dead ChooseCustomerPrefab | Story | UNUSED | ScriptableObject 아님, scene arrays empty. REFACTOR |
| Code/Dialogue/EmotionTag.cs | global | EmotionTag / ReactionPhase | 감정/반응 enum | 없음 | cocktail/reaction/customer/UI | Story | ACTIVE | CP949 source. KEEP |
| Code/Dialogue/GeneralGuestEmotionGenerator.cs | global | static class | 일반 손님 emotion randomization | 없음 | CustomerController | Customers | ACTIVE | KEEP |
| Code/Dialogue/GuestEmotionRequest.cs | global | serializable class | desired emotion pair | tags | CustomerController | Customers | ACTIVE | KEEP |
| Code/Dialogue/GuestReactionDatabase.cs | global | ScriptableObject + line model | tag/heart/phase별 line 선택 | lines | CustomerController; 1 asset | Story | ACTIVE | KEEP |
| Code/Dialogue/GuestReactionResolver.cs | global | static class | recipe success/tag match를 hearts로 변환 | 없음 | CustomerController | Story | ACTIVE | KEEP |
| Code/Managers/BusinessDayManager.cs | global | MonoBehaviour | day, customer cap, day end/result, currentDay save | max, resultPanel, startingDay | GameManager, RecipeUnlock, DayResult, PlayerPrefs | Progression | ACTIVE_WITH_CRITICAL_SCENE_RISK | Main Scene에 2개. REFACTOR |
| Code/Managers/CustomerSpawner.cs | global | MonoBehaviour | spawn, seat, delays, tutorial, LP, loop | prefab(s), points, managers, specialGuests | Customer/Seat/BusinessDay/PlayerPrefs; Scene | Customers | ACTIVE_WITH_CRITICAL_SCENE_RISK | Main Scene에 2개; actual startup broken/manual. REFACTOR |
| Code/Managers/GameManager.cs | global | MonoBehaviour | runtime reputation | 없음 | UIManager, Customer/Order/Day | Core | ACTIVE_WITH_RISK | reputation은 reload 시 0. REFACTOR |
| Code/Managers/MiniGameManager.cs | global | MonoBehaviour | method→prefab, instance lifetime, main world/camera toggle | 4 prefabs, world/camera/HUD | IMiniGame, UI, recipe guide | Bartending | ACTIVE | CP949 comments, mainHUD null. REFACTOR |
| Code/Managers/OrderManager.cs | global | MonoBehaviour | order state, play, match, unlock, reputation, customer result | orderPool, MiniGameManager, toast | Game/Recipe/UI/Customer | Bartending | ACTIVE_WITH_RISK | legacy IssueOrder/ResolveServe path와 active specific-order path 공존. REFACTOR |
| Code/Managers/UIManager.cs | global | MonoBehaviour | legacy rep/order/dialogue labels and Serve | text refs, OrderManager | Game/Order/MiniGame | PARTIAL | Main Scene text refs 전부 null; legacy path. MERGE |
| Code/MiniGames/BuildMiniGame.cs | global | MonoBehaviour, IMiniGame | ingredient selection 후 fill-stop Build | UI, guide, shelf, jigger | MiniGameManager prefab | Bartending | CONFIRMED_IMPLEMENTED_STATIC | Play Mode NEEDS_VERIFICATION. KEEP |
| Code/MiniGames/FloatMiniGame.cs | global | MonoBehaviour, IMiniGame | layer fill/stop Float | UI, guide, shelf, jigger | MiniGameManager prefab | Bartending | CONFIRMED_IMPLEMENTED_STATIC | Play Mode NEEDS_VERIFICATION. KEEP |
| Code/MiniGames/GaugeStopMiniGame.cs | global | MonoBehaviour, IMiniGame | generic bouncing gauge prototype | gauge UI | YAML reference 없음 | Bartending | UNUSED | DELETE_CANDIDATE, 삭제 금지 |
| Code/MiniGames/ShakeMiniGame.cs | global | MonoBehaviour, IMiniGame | alternate direction + space Shake | UI, guide, shelf, jigger | MiniGameManager prefab | Bartending | CONFIRMED_IMPLEMENTED_STATIC | Play Mode NEEDS_VERIFICATION. KEEP |
| Code/MiniGames/StirMiniGame.cs | global | MonoBehaviour, IMiniGame | timing gauge + spoon motion Stir | gauge/UI/guide/shelf/jigger | MiniGameManager prefab | Bartending | CONFIRMED_IMPLEMENTED_STATIC | 일부 optional prefab refs null/auto-bind. KEEP |
| Code/Seats/SeatManager.cs | global | MonoBehaviour | tag discovery, random free seat, repeat avoidance | booleans | SeatPoint, CustomerSpawner | Customers | ACTIVE_WITH_RISK | tag discovery. REFACTOR |
| Code/Seats/SeatPoint.cs | global | MonoBehaviour | occupation + sit/approach/exit markers | marker transforms | SeatManager/Customer | Customers | ACTIVE | logic object only; CP949. KEEP |
| Code/Services/BrewCompositionFactory.cs | global | static class | session→composition conversion | 없음 | all mini games | Bartending | ACTIVE | KEEP |
| Code/Services/RecipeMatchService.cs | global | static class | ingredient/amount similarity | 없음 | OrderManager | Bartending | ACTIVE | private CalculateSimilarity는 unused. KEEP |
| Code/UI/BrewIngredientOptionView.cs | global | MonoBehaviour | ingredient row rendering/select | images/text/button | BrewSelection/Shelf; prefab | UI | ACTIVE | KEEP |
| Code/UI/BrewSelectionController.cs | global | MonoBehaviour | pre-minigame selection prototype | ingredients/list/prefab/text | InteractionUI; BrewPanel | UI | PARTIAL | Scene refs all null; IngredientShelf와 책임 중복. MERGE |
| Code/UI/CocktailCodexCardView.cs | global | MonoBehaviour | card bind/locked/image/name | UI refs, placeholder | Codex panel; prefab | UI | ACTIVE_WITH_FALLBACK | missing Image/Layout/Text runtime 생성. REFACTOR |
| Code/UI/CocktailCodexPanelController.cs | global | MonoBehaviour | grid/detail/open/close/refresh | pages/cards/buttons/detail/recipes | RecipeUnlock, card prefab, DayResult | UI | ACTIVE | filter 없음. KEEP |
| Code/UI/CustomerWaitIndicator.cs | global | MonoBehaviour | customer follow/color/shake | graphics/camera/offset | InteractionUI/Customer | UI | ACTIVE | visual host 자체 toggle. KEEP |
| Code/UI/DayResultPanelController.cs | global | MonoBehaviour | result display/codex/next scene/runtime fallback | panel/text/buttons/codex | BusinessDay, SceneManager | UI | ACTIVE_WITH_RISK | controller가 inactive visual host에 부착. REFACTOR |
| Code/UI/DialogueBubble.cs | global | MonoBehaviour | world-follow timed bubble | text/lifetime | CustomerController; prefab | UI | ACTIVE | KEEP |
| Code/UI/HeartPopup.cs | global | MonoBehaviour | world-follow heart feedback | text/timing | CustomerController; prefab | UI | ACTIVE | KEEP |
| Code/UI/IngredientShelfController.cs | global | MonoBehaviour | ingredients/poured amounts/session | ingredient list/prefab/text | 4 mini games via nested prefab | UI | ACTIVE | unused SeedFromSession and stale amountStep YAML. KEEP |
| Code/UI/InteractionUIController.cs | global | MonoBehaviour | interaction/dialogue/brew method/tutorial routes | panels/buttons/4 orders/camera/indicator | Customer, Order, BrewSelection | UI | ACTIVE_WITH_CRITICAL_FLOW_RISK | 4 forced routes reset on Scene reload. REFACTOR |
| Code/UI/JiggerPanelController.cs | global | MonoBehaviour | measure/pour/undo UI | images/text/buttons/shelf | 4 mini game prefabs | UI | ACTIVE | KEEP |
| Code/UI/RecipeGuidePanelController.cs | global | MonoBehaviour | unlocked recipe list/detail/reveal flag | unlock/UI/button prefab/toast | MiniGameManager, PlayerPrefs | UI | ACTIVE_WITH_RISK | UI가 persistence 소유; Scene에도 extra instance. REFACTOR |
| Code/UI/RecipeUnlockPopupController.cs | global | MonoBehaviour | unlock popup animation | popup/text/image | OrderManager; Scene | UI | ACTIVE | KEEP |
| Code/UI/ResultToastController.cs | global | MonoBehaviour | result/timeout/reveal toast | CanvasGroup/text | Order/Customer/Guide; Scene | UI | ACTIVE | KEEP |

Primary script action count: KEEP 30, REFACTOR 16, MERGE 2, DELETE_CANDIDATE 1. DELETE_CANDIDATE는 삭제 허가가 아니다.

## 5. Current Architecture

현재 실제 orchestration은 문서의 단순 Manager chain보다 더 강하게 결합되어 있다.

    Scene composition
      → CustomerSpawner
        → SeatManager / BusinessDayManager
        → Instantiate CustomerController
          → Find InteractionUIController, GameManager, MiniGameManager, ResultToast
          → InteractionUIController
            → fixed Test OrderData
            → OrderManager.PlaySpecificOrder
              → MiniGameManager
                → method-specific prefab / IMiniGame
                → IngredientShelf + Jigger + RecipeGuide
              → RecipeMatchService
              → RecipeUnlockManager / PlayerPrefs
              → GameManager reputation
              → CustomerController reaction/leave
        → BusinessDayManager
          → PlayerPrefs currentDay
          → DayResultPanelController
            → CocktailCodexPanelController
            → Scene reload

Manager folder는 Customer, Bartending, Progression, Core, UI 책임이 한 폴더에 섞여 있다. Singleton은 없지만 FindFirstObjectByType가 광범위하며 Scene에 duplicate component가 있어 “first” 결과가 composition 순서에 민감하다.

## 6. Runtime Gameplay Flow

### 6.1 실제 코드 흐름

    Application Start
    → all Awake
    → SeatManager finds tagged active objects and filters SeatPoint
    → GameManager-hosted CustomerSpawner Start(autoStart=true)
    → customerPrefab null → error and stop
    → intended CustomerSpawner has autoStart=false, debugSpawnWithKey=true
    → player presses Space
    → SpawnNextCustomer
    → BusinessDay CanSpawnMoreCustomers
    → GetCustomerPrefabForNextSpawn
    → Instantiate before seat resolution
    → SeatManager.GetFreeSeat / Reserve
    → CustomerController.AssignSeat
    → direct movement to sitMarker
    → random general emotion assignment
    → InteractionUI.RegisterCustomer
    → dialogue hint / method selection
    → OrderManager.PlaySpecificOrder using Test OrderData
    → MiniGameManager instantiates method prefab
    → shelf + jigger + mini game
    → BrewComposition → RecipeMatchService
    → unlock / reputation / reaction
    → leave → SeatPoint.Release
    → CustomerSpawner.NotifyCustomerLeft
    → BusinessDayManager.NotifyCustomerServedOrLeft
    → delayed next spawn or EndDay

### 6.2 Baseline runtime conclusion

- CONFIRMED: 호출 경로와 serialized wiring은 위와 같다.
- CONFIRMED KNOWN ISSUE: application start에서 정상 prefab이 자동 spawn되지 않는다. 자동 spawner는 prefab null이고, 정상 설정 spawner는 Space debug 입력이 필요하다.
- NEEDS_VERIFICATION: 과거 문서의 “최근 전체 loop Play 테스트 성공”은 현재 commit의 static composition과 충돌한다. 현재 commit을 Play Mode에서 다시 검증해야 한다.
- CONFIRMED: approachMarker는 CustomerController 이동 경로에서 사용하지 않는다. sitMarker와 exitMarker만 사용한다.
- CONFIRMED: seat가 없으면 방금 만든 customer를 Destroy하고 spawnDelay 후 재시도한다.

## 7. Customer System

### 7.1 CustomerSpawner 집중 조사

| 질문 | 실제 결과 | 판정 |
| --- | --- | --- |
| Spawn entry point | Start→SpawnNextCustomer 또는 debug Space→SpawnNextCustomer, leave 후 coroutine 재호출 | CONFIRMED |
| Prefab resolution | SpawnNextCustomer→GetCustomerPrefabForNextSpawn | CONFIRMED |
| ChooseCustomerPrefab 호출 | 정의 외 호출 0 | DEAD_CODE_CANDIDATE |
| specialGuests | 2개 Scene spawner 모두 빈 배열; dead method 안에서만 읽음 | UNUSED |
| general pool | intended spawner generalCustomerPrefabs 빈 배열 | CONFIRMED |
| normal fallback | customerPrefab = Customer_A | CONFIRMED |
| repeat avoidance | pool에 2개 이상일 때만 의미; 현재 pool empty라 비활성 효과 | CONFIRMED |

### 7.2 Tutorial

CustomerSpawner.IsTutorialSpawn은 다음을 사용한다.

    currentDay == 1 && spawnedCount < tutorialCustomerCount

- tutorialCustomerCount = 4.
- Spawner 단계 Day 1 gate는 현재 코드에 반영되어 있다.
- 그러나 설야→여명→잔설→만월 순서는 spawner prefab이나 OrderData가 아니라 InteractionUIController의 4개 forced route로 구현된다.
- Main Scene에서 forceFirst/Second/Third/FourthCustomerRoute가 모두 1이고 method가 Stir/Shake/Build/Float다.
- forced route consumed flag는 runtime field이며 Scene reload마다 false로 초기화된다.
- 따라서 Day 2 Scene reload 후 첫 고객, 즉 Little Prince가 forced first Stir route를 다시 받는다.

결론: “Spawner tutorial은 Day 1 only”는 CONFIRMED, “게임 전체 tutorial route는 Day 1 only”는 FALSE. Day 2 tutorial repetition 가능성은 CONFIRMED_CODE_PATH다.

### 7.3 Little Prince

| 기능 | 결과 |
| --- | --- |
| littlePrincePrefab | intended spawner에 LittlePrinceCustomer.prefab 연결 |
| Day 2 first spawn | CurrentDay == 2, spawnedCount == 0, flag == 0 조건 |
| completion key | littlePrinceFirstVisitCompleted |
| fallback | prefab null이면 special condition false, general/customerPrefab 경로로 진행 |
| flag write | Little Prince로 표시된 active customer가 Leave할 때 1 저장 |
| exact completion semantics | serve 성공 여부와 무관; timeout/일반 leave도 완료 처리 |
| Little Prince dialogue | prefab에 연결되지만 current interaction path가 DialogueData를 읽지 않음 |
| Little Prince order | prefab orderData에 만월/Float order 연결되지만 CustomerController field를 읽는 code가 없음 |
| actual Day 2 interaction | Scene reload 후 forced first route 때문에 Stir/TestStir 가능 |

Little Prince는 visual prefab selection까지는 구현됐지만 character-specific dialogue/order/story visit은 PARTIAL이다.

### 7.4 Customer prefab behavior

- Customer_A, Customer_B, LittlePrince는 모두 Transform + SpriteRenderer + CustomerController와 InteractionAnchor child만 가진다.
- LittlePrince에 일반 customer에 없는 component는 없다.
- LittlePrince 차이는 name, tint, DialogueData, OrderData, 최신 serialized timeout/reaction fields다.
- Customer_B는 Repository 어떤 Scene/Prefab에서도 참조되지 않는다.
- Customer_B의 InteractionAnchor child는 존재하지만 CustomerController.interactionAnchor는 null이다.

## 8. Seat System

| 항목 | 실제 구조 |
| --- | --- |
| Discovery | GameObject.FindGameObjectsWithTag("SeatPoint") |
| Filter | tagged object에서 GetComponent<SeatPoint>(), null 제거 |
| Active production seats | SeatPoint_Left, SeatPoint_Center, SeatPoint_Right |
| Disabled legacy seats | SeatPoint_x, SeatPoint_y, SeatPoint_z under OldLayout_Disabled |
| Tag pollution | SeatManager host도 Tag=SeatPoint이나 SeatPoint component가 없어 filter에서 제거 |
| Reservation | SeatPoint.Reserve sets IsOccupied and Occupant |
| Release | Customer LeaveAndDestroy→SeatManager.ReleaseSeat→SeatPoint.Release |
| Repeat avoidance | lastAssignedSeat를 free candidate에서 제거 |
| Markers | sitMarker/approachMarker/exitMarker 모두 6 SeatPoint에 연결 |
| Runtime marker use | sitMarker, exitMarker 사용; approachMarker 미사용 |

SeatPoint는 Renderer가 없는 gameplay logic object로 ADR-003과 일치한다. Tag가 빠지거나 active hierarchy 밖으로 이동하면 Awake discovery에서 제외되며 재검색 API가 없으므로 risk HIGH다.

## 9. Bartending System

### 9.1 Flow

    InteractionUI selected method
    → matching Test OrderData
    → OrderManager.PlaySpecificOrder
    → MiniGameManager.GetPrefab(order.miniGameType)
    → Instantiate under MiniGameManager
    → GetComponentInChildren<IMiniGame>(includeInactive=true)
    → RecipeGuide method setup
    → IngredientShelf / Jigger
    → Start button
    → method-specific input and grade
    → MiniGameResult + BrewComposition
    → destroy prefab and restore world/camera
    → OrderManager recipe match/unlock/reaction

### 9.2 Implementation classification

| System | Entry/input/result | Prefab wiring | Classification |
| --- | --- | --- | --- |
| Stir | Start, ingredient requirement, Space stop, Perfect/Good/Fail | 47 native GOs + nested IngredientShelfPanel; manager linked | CONFIRMED_IMPLEMENTED static; runtime NEEDS_VERIFICATION |
| Shake | Left/Right prepare + Space, alternation/time/mistakes | 39 GOs + nested shelf; manager linked | CONFIRMED_IMPLEMENTED static; runtime NEEDS_VERIFICATION |
| Build | hold/release Space fill judgment | 43 GOs + nested shelf; manager linked | CONFIRMED_IMPLEMENTED static; runtime NEEDS_VERIFICATION |
| Float | hold/release Space layer judgment | 45 GOs + nested shelf; manager linked | CONFIRMED_IMPLEMENTED static; runtime NEEDS_VERIFICATION |
| GaugeStop | generic gauge | no Scene/Prefab reference | UNUSED / PROTOTYPE |
| IMiniGame | two StartGame overloads | manager invokes composition-aware overload | CONFIRMED_IMPLEMENTED |
| BrewSelectionController | pre-game selector | Scene component refs all null; redundant with prefab shelf | PARTIAL |

MiniGameManager.mainWorld와 mainCamera는 연결됐지만 mainHUD는 null이다. 플레이 중 World와 Main Camera는 inactive가 되고 main Canvas는 그대로 남는다. 각 prefab은 자체 MiniGameCamera를 가진다.

## 10. Cocktail / Recipe Data

Project custom ScriptableObject asset은 28개, 전체 .asset file은 42개다.

| Definition | Asset count / location | Referenced by / runtime usage | Duplicate responsibility / issue |
| --- | --- | --- | --- |
| CocktailData | 4, Assets/Data/Cocktails | Recipe target, reaction, codex sprite/detail | Recipe와 name/description/method 중복; steps 전부 empty |
| CocktailRecipeData | 4, Assets/Assets/Data/Recipes | Scene RecipeUnlock list, match, guide, codex | 현재 ingredient ratio/unlock Source of Truth |
| OrderData | 5, Assets/Data/Orders | 4 test orders는 Scene/UI; LP order는 prefab dormant | method/steps/target/default session이 Recipe/Cocktail과 중복 |
| IngredientData | 12, Assets/Assets/Data/Ingredients | nested IngredientShelf, inline sessions | icon 12개 모두 null; CoffeeLiqueur filename 중복 |
| DialogueData | 2, Assets/Data/Dialogue + Assets/Data root | customer prefabs | current interaction에서 dormant |
| GuestReactionDatabase | 1, Assets/Data/Dialogue | all customer prefabs / reaction sequence | active |
| SpecialGuestData | 0 assets; serializable class only | empty Scene arrays / dead chooser | SpecialGuest ScriptableObject는 존재하지 않음 |
| CustomerData | type/asset 없음 | 없음 | ABSENT |
| RecipeData | exact type 없음; CocktailRecipeData가 equivalent | 위 참조 | naming mismatch |

### 10.1 Initial cocktails

| Cocktail | Cocktail asset | Recipe asset | Order asset | Sprite | Method |
| --- | --- | --- | --- | --- | --- |
| 설야 | TestStirCocktail | Recipe_TestStir | TestStir | Assets/Assets/Art/Cocktails/설야.png | Stir |
| 여명 | TestShakeCocktail | Recipe_TestShake | TestShake | Assets/Assets/Art/Cocktails/여명.png | Shake |
| 잔설 | TestBuildCocktail | Recipe_TestBuild | TestBuild | Assets/Assets/Art/Cocktails/잔설.png | Build |
| 만월 | TestFloatCocktail | Recipe_TestFloat | TestFloat + LittlePrinceFirstVisitOrder | Assets/Assets/Art/Cocktails/만월.png | Float |

확인된 data anomalies:

- TestShake Order의 miniGameType은 Shake지만 defaultBrewSession.method는 Build(0)다.
- 5 Order asset의 step type은 모두 StirGauge(2)로 serialized되어 method와 일치하지 않는 경우가 있다.
- current four mini games는 RecipeStep 내용을 거의 사용하지 않아 mismatch가 가려진다.
- recipeId가 recipe_test_stir/shake와 recipe_janseol/manwol로 혼합된다.
- production display names를 Test-prefixed file/asset names가 보유한다.

## 11. Codex System

| 기능 | 실제 구현 |
| --- | --- |
| Controller | CocktailCodexPanelController on active CodexPanelControllerHost |
| UI panel | Scene-authored CodexPanel, GridPage, DetailPage |
| Card | CocktailCodexCard prefab, runtime Instantiate |
| Unlock source | RecipeUnlockManager.IsUnlocked |
| Locked state | name/method/detail masked, placeholder/locked overlay |
| Sprite source | CocktailRecipeData.targetCocktail.cocktailSprite |
| Detail | displayName/description/method/ingredient ratios |
| Open | TopHUD CodexButton UnityEvent→Open; DayResult→Open |
| Close | controller Close only hides codex panel |
| Filter | code/UI 없음 |
| Save | unlock manager PlayerPrefs string |

Cocktail image source는 예상이 아니라 실제 code에서 targetCocktail.cocktailSprite로 CONFIRMED다. CardView는 missing Image/Text/LayoutElement를 runtime에 추가할 수 있어 authored prefab과 runtime structure가 달라질 수 있다.

## 12. Business Day / Progression

### 12.1 Actual responsibility

- currentDay read/write.
- maxCustomersPerDay = 8.
- servedCustomerCount와 dayEnded runtime state.
- customer가 serve 성공 또는 timeout 여부와 무관하게 완전히 leave할 때 count 증가.
- end condition 후 currentDay를 즉시 1 증가시켜 PlayerPrefs에 저장.
- 그 다음 Day Result를 연다.
- NextNight button이 active Scene buildIndex를 reload한다.
- RecipeUnlockManager에서 “현재 unlocked 전체” name을 가져온다.

### 12.2 Duplicate composition

Main Scene에는 BusinessDayManager가 두 개다.

| Host | endDayWhenMaxCustomersServed | resultPanel | 사용 가능성 |
| --- | --- | --- | --- |
| Manager/BusinessDayManager | false | explicit DayResultPanel | day end 불가; FindFirst 결과 오염 |
| Manager/CustomerSpawner | true | null at serialization, inactive-inclusive search로 resolve | intended spawner가 explicit 참조하는 active day flow |

CustomerSpawner에도 두 component가 존재한다. 이 중 정상 prefab/LP/attached BusinessDay가 있는 component는 autoStart=false다.

### 12.3 Reload risk

Scene reload 시 다음 runtime field가 초기화된다.

- BusinessDay servedCustomerCount, dayEnded.
- CustomerSpawner servedCount, spawnedCount, lastSpawnedPrefab.
- InteractionUI forced route consumed flags.
- GameManager reputation.

Persist되는 것은 currentDay, recipe unlocks, Little Prince flag, first recipe reveal flag뿐이다. 특히 reputation은 Day Result 이후 다음 Scene에서 0으로 돌아간다.

## 13. Save / PlayerPrefs

Wrapper/SaveManager는 없다. 4개 key가 4개 class 책임에 분산되어 있다.

| Key | Type | Read location | Write/delete location | Purpose | Risk |
| --- | --- | --- | --- | --- | --- |
| currentDay | int | BusinessDayManager.Awake; CustomerSpawner fallback literal | BusinessDayManager.EndDay | 다음 영업일 | HIGH: duplicate manager, literal/const 이중 사용, migration/reset 없음 |
| littlePrinceFirstVisitCompleted | int | CustomerSpawner.ShouldSpawnLittlePrinceFirstVisit | CustomerSpawner.NotifyCustomerLeft | 첫 방문 재등장 방지 | HIGH: hardcoded story/save in spawner; any leave marks complete |
| SURLHWA_UNLOCKED_RECIPES | pipe-delimited string | RecipeUnlockManager.LoadUnlocks | SaveUnlocks; ClearSavedUnlocks delete | recipe IDs | HIGH: delimiter/version/migration 없음; direct UI coupling |
| SURLHWA_FIRST_RECIPE_REVEAL_SHOWN | int | RecipeGuidePanelController | same class set/delete; RecipeUnlockManager invokes clear helper | first guide toast | MEDIUM: UI owns persistence |

PlayerPrefs.DeleteAll은 없다. PlayerPrefs.Save는 각 write/delete 직후 호출된다. 향후 중앙화 시 기존 key를 유지하거나 migration을 제공해야 한다.

## 14. Special Guest System

현재 Special Guest는 두 개의 서로 분리된 prototype이다.

1. 일반화 시도: SpecialGuestData[] + requiredReputation + ChooseCustomerPrefab.
   - array empty.
   - method uncalled.
   - runtime 영향 없음.
2. Little Prince vertical slice: hardcoded prefab, Day 2, PlayerPrefs flag.
   - prefab selection은 active.
   - dialogue/order asset은 active interaction flow에 연결되지 않음.
   - visit completion은 leave 시점에 저장.

StoryProgress, VisitProgress, Revisit, Statue state, Environment state를 담당하는 code/data는 없다. Scene에는 StatueSlots placeholder 3개만 있다.

최소 target architecture 제안:

    CustomerSpawner
    → SpecialGuestResolver.TryResolve(day, spawnIndex, progress)
    → optional customer prefab/definition
    → existing general fallback

    Customer leave/result
    → minimal StoryProgress service
    → persistence facade preserving current keys

한 캐릭터 vertical slice를 위해 generic quest framework나 대규모 event bus는 필요하지 않다. 먼저 Little Prince의 eligibility와 completion 두 규칙만 resolver/progress boundary 밖으로 옮기는 것이 적정하다.

## 15. Scenes

| Path | Purpose | Build | Classification | Important systems | Risk |
| --- | --- | --- | --- | --- | --- |
| Assets/Scenes/SampleScene.unity | Main gameplay, front-facing layout | Enabled | MAIN | all managers, seats, world, interaction, codex, result, notification | CRITICAL |
| Assets/BarScene_LayoutTest.unity | Main의 거의 전체 clone인 layout/development scene | No | DEVELOPMENT / LEGACY_CANDIDATE | same broad hierarchy, no LP binding, several roots inactive | HIGH stale-copy risk |
| Assets/Settings/Scenes/URP2DSceneTemplate.unity | URP 2D scene template source | No | UNITY MANAGED TEMPLATE | camera/light template | DO_NOT_TOUCH |

SampleScene과 LayoutTest의 주요 차이는 Little Prince fields, spawn point, several active states, UI rects, startingDay serialization이다. LayoutTest는 독립 test라기보다 Main의 stale near-copy다.

요청에서 예시로 언급한 Assets/MainScene_Backup_BeforeBarLayout.unity 또는 유사 backup Scene은 존재하지 않는다.

## 16. Main Scene Current Hierarchy

YAML 기준 native GameObject 208, native root 7, prefab instance 10, MonoBehaviour 190이다. self-active 200/208이지만 disabled ancestor 때문에 effective active는 103/208이다. 모든 local fileID reference는 정의된 object를 가리키며 m_Script fileID 0은 발견되지 않았다.

Layer는 Canvas/UI subtree가 5, world/system/camera/light가 0이다. 명시 tag는 Main Camera의 MainCamera, active/legacy SeatPoint 6개와 SeatManager host의 SeatPoint이며 나머지는 Untagged다. 10개 prefab instance는 모두 disabled OldLayout 아래의 Counter/Shelf/Stool이다.

아래는 controller/system/structural nodes를 모두 포함하고 반복되는 leaf Background/Label/Icon은 brace로 압축한 current tree다.

    SampleScene
    ├─ MiniGameCanvas [ON, Canvas, CanvasScaler 1920×1080, RecipeGuidePanelController]
    │  ├─ RecipeBookButton [OFF] → Text
    │  └─ RecipeGuidePanel [OFF]
    │     ├─ Background
    │     ├─ RecipeNameText / RecipeMethodText / RecipeIngredientText
    │     ├─ RecipeListGroup
    │     └─ CloseButton → Text
    ├─ Manager [ON]
    │  ├─ BusinessDayManager [BusinessDayManager, end-day=false]
    │  ├─ GameManager [CustomerSpawner with null prefab + GameManager]
    │  ├─ CustomerSpawner [CustomerSpawner + BusinessDayManager]
    │  ├─ MiniGameManager
    │  ├─ UIManager
    │  ├─ RecipeUnlockManager
    │  ├─ EventSystem [EventSystem + InputSystemUIInputModule]
    │  ├─ OrderManager
    │  ├─ SpawnPoint
    │  └─ SeatManager [Tag=SeatPoint, SeatManager but no SeatPoint]
    ├─ NotificationCanvas [ON, Canvas, RecipeUnlockPopupController]
    │  └─ RecipeUnlockPopup
    │     ├─ UnlockPanel
    │     ├─ TitleText / MessageText
    │     └─ CocktailImage
    ├─ Global Light 2D [ON]
    ├─ World [ON]
    │  ├─ Seats
    │  │  ├─ SeatPoint_Center [Tag=SeatPoint, SeatPoint]
    │  │  │  └─ ApproachMarker / exitMarker / SitMarker
    │  │  ├─ SeatPoint_Left [Tag=SeatPoint, SeatPoint]
    │  │  │  └─ exitMarker / ApproachMarker / SitMarker
    │  │  └─ SeatPoint_Right [Tag=SeatPoint, SeatPoint]
    │  │     └─ ApproachMarker / exitMarker / SitMarker
    │  ├─ BarInterior_Prototype
    │  │  ├─ BackWall [SpriteRenderer]
    │  │  ├─ Counter_UShape
    │  │  │  └─ FrontCounter / LeftWing / RightWing [SpriteRenderer]
    │  │  ├─ Floor [SpriteRenderer]
    │  │  ├─ StatueSlots
    │  │  │  ├─ StatueSlot_Left → PlaceHolder
    │  │  │  ├─ StatueSlot_Right → PlaceHolder
    │  │  │  └─ StatueSlot_BackCenter → PlaceHolder
    │  │  └─ Props
    │  ├─ Markers
    │  │  └─ EntrancePoint / ExitPoint / SpawnPoint
    │  ├─ OldLayout_Disabled [OFF]
    │  │  ├─ World
    │  │  │  ├─ Seats
    │  │  │  │  ├─ SeatPoint_x → SitMarker / ExitMarker / ApproachMarker
    │  │  │  │  ├─ SeatPoint_y → SitMarker / ExitMarker / ApproachMarker
    │  │  │  │  └─ SeatPoint_z → SitMarker / ExitMarker / ApproachMarker
    │  │  │  ├─ Shelf_Prefab → Shelf (1), Shelf (2) [prefab instances]
    │  │  │  ├─ Counter_Prefab → Counter_Top (1..5) [prefab instances]
    │  │  │  ├─ Stool_Prefab → Stool (1..3) [prefab instances]
    │  │  │  └─ ExitPoint / EnterancePoint
    │  │  └─ Grid
    │  │     └─ Floor Prototype / Wall Prototype [Tilemaps]
    │  └─ SnowBackground [SpriteRenderer]
    ├─ Main Camera [ON, MainCamera tag, Camera, AudioListener, URP camera components]
    └─ Canvas [ON, Canvas, CanvasScaler ConstantPixelSize, InteractionUIController]
       ├─ CodexPanelControllerHost [ON, CocktailCodexPanelController]
       ├─ InteractionLayer
       │  ├─ Label
       │  └─ EllipsisPromptButton
       │     └─ EllipsisPrompt [CustomerWaitIndicator]
       ├─ BrewPanel [OFF, BrewSelectionController]
       │  ├─ MethodGroup
       │  │  └─ StirButton / ShakeButton / BuildButton / FloatButton
       │  ├─ ConfirmButton / CancelButton
       │  ├─ Background
       │  └─ TitleText
       ├─ OverlayBlocker
       ├─ ResultToastPanel [ResultToastController, CanvasGroup]
       │  └─ TitleText / MessageText
       ├─ IngredientGroup [OFF] → IngredientSlot_1..4
       ├─ CodexPanel
       │  ├─ GridPage
       │  │  ├─ CloseButton / TitleText
       │  │  └─ CardScroll
       │  │     └─ Viewport → Content; Scrollbar Vertical → Sliding Area → Handle
       │  └─ DetailPage
       │     ├─ LargeImage / BackButton / CloseButton
       │     └─ InfoArea
       │        └─ LockedDetailText / NameText / MethodText / DescriptionText / IngredientsText
       ├─ BaseSpiritGroup [OFF] → SpiritSlot_1..3
       ├─ TopHUD
       │  ├─ SettingsButton [no persistent action/controller found]
       │  └─ CodexButton [UnityEvent → CocktailCodexPanelController.Open]
       ├─ DialoguePanel
       │  ├─ DialogueText / NameText / Background
       │  ├─ BrewButton
       │  └─ NextButton [OFF]
       └─ DayResultPanel [OFF, DayResultPanelController]
          ├─ DimBackground
          └─ Panel
             ├─ TitleText / ReputationText / CustomerCountText / UnlockedRecipesText
             ├─ StayButton [runtime label/action becomes Codex]
             └─ NextNightButton [runtime action reloads Scene]

Hierarchy는 완전히 flat하지 않지만 Manager root에 duplicate/mixed responsibility가 있고, World 안에 active production layout과 disabled legacy world가 중첩되어 있다. UI는 3 root Canvas의 scaling policy가 다르다.

## 17. Proposed Scene Hierarchy

실제 dependencies를 반영한 최소 제안이다. 이 구조는 이번 Audit에서 적용하지 않았다.

    _SYSTEMS
    ├─ CustomerSystem
    │  ├─ CustomerSpawner
    │  └─ SeatManager
    ├─ BartendingSystem
    │  ├─ OrderManager
    │  ├─ MiniGameManager
    │  └─ RecipeUnlockManager
    ├─ ProgressionSystem
    │  ├─ BusinessDayManager
    │  └─ GameManager
    └─ EventSystem
    _WORLD
    ├─ Environment
    │  ├─ SnowBackground
    │  └─ BarInterior
    ├─ Seats
    ├─ Markers
    ├─ StatueSlots
    └─ Props
    _RUNTIME
    ├─ Customers
    └─ MiniGames
    _UI
    ├─ MainCanvas
    ├─ NotificationCanvas
    └─ MiniGameCanvas
    _CAMERA_LIGHTING
    ├─ MainCamera
    └─ GlobalLight2D
    _LEGACY_DISABLED
    └─ OldLayout_Disabled

전제:

- 먼저 duplicate spawner/day manager를 해결하고 canonical component를 정해야 한다.
- Serialized reference를 유지한 작은 parent move만 수행한다.
- OldLayout은 사용 여부 결정 전 DO_NOT_TOUCH한다.
- _RUNTIME root 도입 시 MiniGameManager의 parent/lifetime과 world active toggle을 별도 검증한다.

## 18. Prefabs

| Path | Root components / child structure | Usage / referenced by | Classification | Risk |
| --- | --- | --- | --- | --- |
| Prefab/Character/Customer_A | Transform, SpriteRenderer, CustomerController; InteractionAnchor | Main + LayoutTest customerPrefab | PROTOTYPE_ACTIVE | HIGH |
| Prefab/Character/Customer_B | same; InteractionAnchor | no YAML reference | UNUSED_CANDIDATE | MEDIUM; anchor field null |
| Prefab/Character/LittlePrinceCustomer | same; InteractionAnchor | Main littlePrincePrefab | SPECIAL_GUEST_PROTOTYPE | HIGH; custom data dormant |
| Prefab/DialogueBubble | DialogueBubble; Canvas/BubbleBG/TMP text | all 3 customers | ACTIVE_UI | MEDIUM |
| Prefab/Funiture/Counter_Top | Transform, SpriteRenderer | 5 instances under disabled OldLayout in two scenes | LEGACY_VISUAL | LOW until cleanup |
| Prefab/Funiture/Shelf | Transform, SpriteRenderer | 2 instances under disabled OldLayout | LEGACY_VISUAL | LOW |
| Prefab/Funiture/Stool | Transform, SpriteRenderer | 3 instances under disabled OldLayout | LEGACY_VISUAL | LOW |
| Prefab/Minigame/BuildMiniGame | BuildMiniGame; 43 GOs; nested IngredientShelfPanel | MiniGameManager in two scenes | PROTOTYPE_ACTIVE | HIGH |
| Prefab/Minigame/FloatMiniGame | FloatMiniGame; 45 GOs; nested shelf | MiniGameManager | PROTOTYPE_ACTIVE | HIGH |
| Prefab/Minigame/ShakeMiniGame | ShakeMiniGame; 39 GOs; nested shelf | MiniGameManager | PROTOTYPE_ACTIVE | HIGH |
| Prefab/Minigame/StirMiniGame | StirMiniGame; 47 GOs; nested shelf | MiniGameManager | PROTOTYPE_ACTIVE | HIGH |
| Prefab/UI/BrewIngredientOption | Rect UI + BrewIngredientOptionView, 3 GOs | IngredientShelfPanel | ACTIVE | MEDIUM |
| Prefab/UI/CocktailCodexCard | Rect UI + card view, 5 GOs | Codex Scene controller | ACTIVE | MEDIUM |
| Prefab/UI/HeartPopup | HeartPopup + world Canvas/TMP, 3 GOs | all customers | ACTIVE | MEDIUM |
| Prefab/UI/IngredientShelfPanel | IngredientShelfController, 9 GOs | nested by all 4 mini games | ACTIVE_SHARED | HIGH |
| Prefab/UI/RecipeButtonPrefab | Button/TMP, 2 GOs | Scene guide + all 4 mini games | ACTIVE_SHARED | MEDIUM |
| texture/Tiles/Table.prefab | Grid/Tilemap-like, 2 GOs | no reference | DELETE_CANDIDATE | UNKNOWN |
| texture/Tiles/wall.prefab | Grid/Tilemap-like, 2 GOs | no reference | DELETE_CANDIDATE | UNKNOWN |

Prefab local fileID broken reference와 m_Script fileID 0은 정적 검사에서 발견되지 않았다.

## 19. UI

### 19.1 Canvas and hosts

| Root/host | Static active | Scaling | Controller/behavior | Risk |
| --- | --- | --- | --- | --- |
| Canvas | ON | Constant Pixel Size, 800×600 values retained | InteractionUI | main controller host active: compliant |
| MiniGameCanvas | ON | Scale With Screen Size 1920×1080, match .5 | extra Scene RecipeGuide | likely duplicate/legacy |
| NotificationCanvas | ON | Constant Pixel Size | RecipeUnlockPopup | host active, popup child toggled: compliant |
| CodexPanelControllerHost | ON | parent Canvas | controller active, CodexPanel child toggled | compliant |
| ResultToastPanel | ON | parent Canvas | CanvasGroup alpha | compliant |
| DayResultPanel | OFF | parent Canvas | controller attached to visual root itself | UI Host Rule violation candidate, HIGH |
| BrewPanel | OFF | parent Canvas | BrewSelectionController on toggled visual root | PARTIAL but direct lookup includes inactive |
| EllipsisPrompt | runtime toggled | parent Canvas | CustomerWaitIndicator on toggled visual | MEDIUM |

### 19.2 Feature inventory

- Interaction: implemented and wired.
- Bartending selection: method UI wired; preselection controller fields null; actual ingredient selection is mini game prefab shelf.
- Dialogue: Interaction hint strings active; DialogueData-driven flow dormant.
- Codex: grid/detail/locked/sprite implemented; filter absent.
- Day Result: Scene-authored panel exists.
- Settings: visual button exists; controller/action not found.
- Unlock popup: implemented.
- Recipe guide: prefab implementation active; extra Scene instance likely unused.
- Runtime fallback: DayResult can create full Canvas; Codex card can add missing components.

## 20. Art / Audio / Assets

### 20.1 Counts and locations

| Type | Count / location | Notes |
| --- | --- | --- |
| Project images | 33 PNG/JPG outside TMP | cocktail, minigame, UI, tile/prototype |
| Audio | 0 wav/mp3/ogg/aiff | ABSENT |
| Animation/Animator | 0 anim/controller/overrideController | ABSENT |
| Material | 2, both TMP default materials | Unity/TMP managed |
| Custom font files | NotoSansKR TTF duplicated in Assets/Data and Assets/TextMesh Pro/Fonts | identical SHA-256 |
| VFX | no .vfx; no project-specific VFX system | ABSENT |

### 20.2 Referenced visuals

- Initial cocktail PNG 4개는 respective CocktailData.cocktailSprite가 참조.
- locked.png는 Codex Scene/prefab placeholder가 참조.
- barspoon, floatbarspoon, glass, glass2, jigger, shaker, shot은 mini game prefabs가 참조.
- UI icons 8개는 Main/LayoutTest Scene이 참조.
- bubble.png는 DialogueBubble prefab이 참조.
- tile images는 current/legacy Scene 및 character/furniture placeholder sprites에서 사용.

### 20.3 No-reference candidates

다음은 GUID reference 0이다: bartable.jpg, liquid fill.jpg, hand.jpg, Fail.jpg, gauge.jpg, Good.jpg, Perfect.jpg. Concept/reference일 수 있으므로 DELETE_CANDIDATE일 뿐 삭제 금지다.

Naming/location issues:

- Assets/Assets 중복 root.
- Funiture 오타.
- production 칵테일이 Test-prefixed asset.
- Ingredient_CoffeeLiqueurIngredient_CoffeeLiqueur 중복 name.
- water/vermouth display casing 불일치.
- root Assets와 Assets/Assets에 project content 분산.
- prototype tiles와 production front-facing layout이 함께 존재.

### 20.4 TextMesh Pro / encoding

| Check | Result | Risk |
| --- | --- | --- |
| Custom Korean TMP asset | Assets/Data/NotoSansKR-Regular SDF.asset | CONFIRMED |
| Atlas population | 0 = Static | HIGH: final text glyph coverage must be tested |
| Font fallback table | empty | HIGH |
| Global TMP default | LiberationSans SDF; global fallback empty | HIGH for Korean text that keeps default font |
| Main Scene font references | Noto asset GUID 83 occurrences; Liberation GUID 11 occurrences across 8 TMP blocks | mixed-font risk |
| Duplicate source font | Assets/Data/NotoSansKR-Regular.ttf and Assets/TextMesh Pro/Fonts/NotoSansKR-Regular.ttf are byte-identical | MOVE/cleanup decision; do not delete yet |

UTF-8 strict decode failed for the following 7 source files; CP949 decode restores readable Korean comments.

- Assets/Code/Data/MiniGameType.cs
- Assets/Code/Data/RecipeStep.cs
- Assets/Code/Data/RecipeStepType.cs
- Assets/Code/Dialogue/EmotionTag.cs
- Assets/Code/Managers/MiniGameManager.cs
- Assets/Code/MiniGames/GaugeStopMiniGame.cs
- Assets/Code/Seats/SeatPoint.cs

Assets/Code/Managers/OrderManager.cs is valid UTF-8 but already contains literal U+FFFD replacement characters in comments around lines 75, 78, 101, 221, 236, 240. Runtime Korean string literals inspected in CustomerController, result/codex UI, and ScriptableObject YAML are readable, but a Play Mode glyph sweep remains NEEDS_VERIFICATION.

## 21. Serialized Reference Risks

| Rank | Dependency | Mechanism | Risk | Evidence / failure mode |
| --- | --- | --- | --- | --- |
| 1 | duplicate Spawner/BusinessDay | Scene components + FindFirst | CRITICAL | 2+2 active; wrong autoStart/prefab/end settings |
| 2 | Interaction forced routes | Scene booleans + runtime flags | CRITICAL | Scene reload resets flags; Day 2 LP gets first route |
| 3 | Main Scene wiring | direct fileID/GUID | CRITICAL | 208 GOs, 190 MonoBehaviours, many UI/object refs |
| 4 | CustomerSpawner→prefabs/day/save | Inspector + hardcoded key | HIGH | LP/general/tutorial tightly coupled |
| 5 | Mini games | prefab GUID + nested prefab + transform.Find fallback | HIGH | hierarchy/name/ref changes can break input UI |
| 6 | Seat discovery | Tag + Awake one-time search | HIGH | inactive/tag-missing seats invisible; SeatManager tag polluted |
| 7 | UI controller availability | active state + direct/search refs | HIGH | DayResult controller host inactive; fallback changes topology |
| 8 | Recipe/Cocktail/Order | ScriptableObject GUID graph | HIGH | moves/type changes risk serialized data; duplicated fields |
| 9 | PlayerPrefs | literal keys in 4 classes | HIGH | migration/version/reset absent |
| 10 | Codex button | UnityEvent target fileID | MEDIUM | hierarchy/component replacement can silently lose Open |
| 11 | MiniGameManager world/camera | Inspector object refs | HIGH | reparent/deactivate can hide wrong root |
| 12 | TMP font | font asset/material GUID | HIGH | mixed font use, static atlas, no fallback |

Reference mechanism summary:

- FindFirstObjectByType is used across Customer, Order, Spawner, MiniGame, BusinessDay, RecipeGuide, Codex.
- Runtime AddComponent exists for missing BusinessDay, DayResult fallback UI, Codex card Image/Layout.
- transform.Find path fallback exists in all four mini games.
- Resources/Addressables load is absent.
- SeatPoint is the only custom tag-based discovery.

## 22. Dead / Legacy / Prototype Candidates

| Candidate | Evidence | Classification | Action |
| --- | --- | --- | --- |
| CustomerSpawner.ChooseCustomerPrefab | definition only | DEAD_CODE_CANDIDATE | keep until resolver migration decision |
| CustomerSpawner.specialGuests | empty arrays; dead method only | UNUSED | REFACTOR |
| GaugeStopMiniGame | no YAML/code caller beyond class | DELETE_CANDIDATE | do not delete yet |
| CustomerController.orderData | only field + LP prefab serialization | DEAD_FIELD_CANDIDATE | preserve; decide LP path |
| CustomerController.OnServed / OrderData overload | no caller | LEGACY_CANDIDATE | preserve |
| OrderManager.IssueOrderTo | no caller | LEGACY_CANDIDATE | preserve |
| OrderManager.ResolveServe | UIManager only; no wired Serve button | LEGACY_CANDIDATE | preserve |
| GameManager.OnOrderResolved | definition only | DEAD_CODE_CANDIDATE | preserve |
| UIManager.ShowDialogue | definition only | DEAD_CODE_CANDIDATE | preserve |
| IngredientShelf.SeedFromSession | definition only | DEAD_CODE_CANDIDATE | preserve |
| RecipeMatchService.CalculateSimilarity | private definition only | DEAD_CODE_CANDIDATE | preserve |
| BrewSelectionController | Scene-referenced but fields null; shelf duplicates purpose | MERGE |
| Scene MiniGameCanvas RecipeGuide | mini games carry own guides | LIKELY_LEGACY |
| Customer_B prefab | no reference | DELETE_CANDIDATE / UNKNOWN |
| Table.prefab / wall.prefab | no reference | DELETE_CANDIDATE |
| OldLayout_Disabled | inactive tree with 10 prefab instances/3 seats | LEGACY_CANDIDATE |
| BarScene_LayoutTest | stale near-copy, not build | KEEP_OR_DELETE_DECISION |
| 7 no-reference minigame images | GUID reference 0 | DELETE_CANDIDATE |

모든 DELETE_CANDIDATE는 승인 전 유지한다.

## 23. Responsibility Problems

1. CustomerSpawner가 spawn, tutorial, story eligibility, save flag, day link, scheduling을 동시에 소유한다.
2. CustomerController가 movement, patience, interaction discovery, tutorial-specific dialogue, emotion, popup, legacy dialogue까지 소유한다.
3. BusinessDayManager가 day orchestration, persistence, result lookup/fallback, recipe name projection을 소유한다.
4. OrderManager가 order state, mini game orchestration, recipe matching, unlock UI, reputation, customer reaction을 묶는다.
5. CocktailData, CocktailRecipeData, OrderData가 method/description/target/steps를 중복 보유한다.
6. InteractionUI가 UI뿐 아니라 Day-independent tutorial sequence state와 customer emotion을 결정한다.
7. RecipeGuide UI가 PlayerPrefs key를 직접 관리하고 RecipeUnlockManager가 그 UI static reset을 호출한다.
8. BrewSelectionController와 IngredientShelfController가 ingredient session 책임을 중복 구현한다.
9. runtime fallback UI와 scene-authored UI가 동시에 존재해 topology가 두 가지다.
10. Manager root와 Managers folder가 domain boundary를 표현하지 못한다.

## 24. Recommended Asset Structure

현재 concept를 유지하되 Scenes의 Development와 Legacy, Fonts를 명시하는 정도만 추가한다.

    Assets/
    ├─ _SURLHWA/
    │  ├─ Art/
    │  │  ├─ Characters/{General,SpecialGuests}
    │  │  ├─ Cocktails
    │  │  ├─ Environment
    │  │  ├─ Props
    │  │  ├─ UI
    │  │  └─ VFX
    │  ├─ Audio/{BGM,SFX,Ambience}
    │  ├─ Code
    │  ├─ Data/{Cocktails,Recipes,Orders,Ingredients,Dialogue,Guests}
    │  ├─ Fonts
    │  ├─ Prefabs/{Customers,Bartending,Environment,UI,VFX}
    │  ├─ Scenes/{Main,Development}
    │  └─ Settings
    ├─ ThirdParty
    └─ TextMesh Pro

별도 Backup 폴더를 만들기보다 Git history를 사용한다. Legacy 삭제가 승인되기 전에는 _SURLHWA/Legacy 같은 임시 quarantine을 만들 수 있지만 영구 구조로 과설계하지 않는다. 이동은 Unity Editor에서 .meta/GUID를 보존하며 작은 batch로 수행한다.

## 25. Recommended Code Structure

    Code/
    ├─ Core/
    │  └─ GameManager or a smaller runtime session model
    ├─ Customers/
    │  ├─ CustomerController
    │  ├─ CustomerSpawner
    │  └─ Seats/{SeatManager,SeatPoint}
    ├─ Bartending/
    │  ├─ Common/{IMiniGame,MiniGameType,MiniGameResult}
    │  ├─ Orchestration/{OrderManager,MiniGameManager}
    │  ├─ Recipes/{BrewComposition,Session,Factory,Match}
    │  └─ MiniGames/{Stir,Shake,Build,Float}
    ├─ Story/
    │  ├─ Dialogue
    │  └─ SpecialGuests/{SpecialGuestResolver,Progress boundary}
    ├─ Progression/
    │  ├─ BusinessDayManager
    │  ├─ RecipeUnlockManager
    │  └─ Persistence facade
    ├─ Data/
    │  ├─ CocktailData
    │  ├─ CocktailRecipeData
    │  ├─ OrderData
    │  └─ IngredientData
    ├─ UI/{Interaction,Bartending,Codex,DayResult,Feedback}
    └─ Utilities/

우선 responsibility를 안정화하고 namespace/folder move는 그 뒤에 한다. 한 번에 49 files에 namespace를 추가하지 않는다.

## 26. Rebuild Risks

실제 Repository 기준 TOP 10:

1. CRITICAL — duplicate CustomerSpawner/BusinessDayManager와 broken auto-start composition.
2. CRITICAL — Scene reload 후 tutorial forced routes가 Day 2 Little Prince에 반복.
3. CRITICAL — Main Scene의 대규모 serialized UI/system wiring 손상 가능성.
4. HIGH — Little Prince eligibility/completion/save가 CustomerSpawner에 hardcode되고 custom order/dialogue가 dormant.
5. HIGH — 4개의 큰 mini game prefab이 shared nested prefab, direct refs, path fallback을 혼합.
6. HIGH — 4 PlayerPrefs key가 4 classes에 분산되고 migration/versioning 없음.
7. HIGH — inactive DayResult controller host, runtime fallback, 서로 다른 Canvas scaling.
8. HIGH — Seat tag one-time discovery, polluted SeatManager tag, disabled legacy seats.
9. HIGH — Cocktail/Recipe/Order data 중복과 Shake session/RecipeStep mismatch.
10. HIGH — 7 CP949 source files, 1 replacement-character source, static Korean TMP asset/no fallback, duplicate TTF.

추가 repository risk: tracked UserSettings, Main/LayoutTest near-copy, prototype/production naming 혼재.

## 27. Recommended Rebuild Order

큰 rewrite 없이 각 phase를 독립 검증한다.

| Phase | Goal | Files affected candidate | Risk | Validation | Rollback point |
| --- | --- | --- | --- | --- | --- |
| A — Runtime Baseline Stabilization | canonical Spawner/BusinessDay 1개씩 결정, normal auto-start 복구 | SampleScene only first; Docs state update | HIGH | fresh Play start, one spawn, no duplicate warning/error, 8-customer end | Scene-only commit |
| B — Day 1 / Day 2 Flow Gate | forced routes를 current day와 연결, LP가 tutorial repeat를 받지 않게 함 | InteractionUI, CustomerSpawner, focused tests/docs | HIGH | Day 1 four methods; reload; Day 2 LP first and no Stir repeat | code-only focused commit |
| C — Little Prince Vertical Slice | existing dialogue/order를 실제 path에 연결하고 completion semantics 결정 | CustomerController/Interaction/OrderData/LP assets, no broad framework | HIGH | LP dialogue/order/result/leave/fallback | vertical-slice commit |
| D — Persistence Facade | 4 existing keys를 그대로 감싸고 call sites 단계 이관 | new small Progress/Persistence code + 4 callers | HIGH | existing PlayerPrefs migration/no data loss | one-key-at-a-time commits |
| E — Data Authority Alignment | Cocktail/Recipe/Order field authority와 Shake/step mismatch 정리 | data type code + 13 cocktail/recipe/order assets | HIGH | all four recipes match/unlock/codex | one method/data set per commit |
| F — UI Reliability | DayResult host rule, runtime fallback policy, Canvas scaling, redundant BrewSelection 결정 | UI code + SampleScene + UI prefabs | HIGH | result→codex→close→result→next day; all resolutions | one UI surface per commit |
| G — Special Guest Resolver | LP eligibility를 minimal resolver로 추출 | Spawner + new Story files + persistence API | MEDIUM | same Day 2 behavior/fallback/flag | resolver commit before removing old branch |
| H — Scene Hierarchy | proposed roots로 small parent moves, old layout quarantine decision | SampleScene | CRITICAL | missing refs 0, seats 3, spawn/minigame/UI loop | one root move per commit |
| I — Asset/Code Moves | _SURLHWA structure, .meta/GUID preserved, namespace only in separate batches | Assets paths | CRITICAL | compile, YAML GUID search, main+prefabs open | one folder/domain per commit |
| J — Legacy Cleanup | approved dead/unused candidates only | GaugeStop, Customer_B, old images/scenes etc. | MEDIUM | reference search + regression before delete | candidate-specific commits |
| K — Encoding/TMP | CP949→UTF-8 controlled conversion, font coverage/fallback | 8 source files, font/TMP assets only if approved | HIGH | byte validation + Korean UI glyph sweep | encoding and font separate commits |
| L — Full Regression | freeze rebuild baseline | no feature scope | LOW | checklist below, Console clean | release candidate tag/commit |

Recommended first actual modification phase는 A다. Asset 이동이나 namespace 추가보다 현재 startup composition을 먼저 신뢰할 수 있게 만들어야 이후 regression이 의미가 있다.

## 28. Regression Checklist

### 28.1 Repository / load

- [ ] Clean working tree에서 시작.
- [ ] Main Scene opens without reserialize prompt.
- [ ] Compile error 0.
- [ ] Missing Script / Missing Reference 0.
- [ ] duplicate Spawner warning 0.
- [ ] Korean text/glyph sweep and Console font warning 확인.

### 28.2 Application start

- [ ] debug Space 없이 intended customer가 한 번만 spawn.
- [ ] Customer_A/general fallback prefab correct.
- [ ] active seats exactly 3.
- [ ] no second customer while activeCustomer exists.
- [ ] no-seat path destroys temporary customer and retries without leaked reservation.

### 28.3 Day 1 customer loop

- [ ] Customer spawn→seat reserve→sitMarker movement.
- [ ] interaction ellipsis follows customer.
- [ ] dialogue→brew→cancel→dialogue→ellipsis transitions.
- [ ] first customer: 설야/Stir.
- [ ] second customer: 여명/Shake.
- [ ] third customer: 잔설/Build.
- [ ] fourth customer: 만월/Float.
- [ ] fifth+ is general route, patience enabled.
- [ ] every leave releases seat and schedules next spawn.
- [ ] avoid-same-seat behavior with multiple free seats.

### 28.4 Bartending

- [ ] Stir: recipe guide, ingredient select, jigger 0.5/1oz, pour, undo, Start gate, Perfect/Good/Fail.
- [ ] Shake: same preparation UI, alternating input, mistake/time fail.
- [ ] Build: hold/release, target/overflow/time.
- [ ] Float: base/top layer, spoon/target/overflow/time.
- [ ] correct composition matches expected recipe.
- [ ] wrong/missing/extra amounts produce expected unknown/fail.
- [ ] newly matched recipe unlocks once and persists.
- [ ] ResultToast and RecipeUnlockPopup do not overlap incorrectly.
- [ ] mini game closes, World/Main Camera restore, interaction resumes.

### 28.5 Customer result / timeout

- [ ] success/fail/unknown cocktail reaction and hearts.
- [ ] reputation delta correct.
- [ ] patience pauses while mini game is active.
- [ ] timeout applies penalty, shows toast, releases seat, counts leave.
- [ ] dialogue/heart popup prefab follows and destroys cleanly.

### 28.6 Day end / UI

- [ ] 8th leave triggers one EndDay.
- [ ] currentDay increments exactly once.
- [ ] Result Panel values correct; “newly unlocked” semantics reviewed.
- [ ] Open Codex from result.
- [ ] Close Codex.
- [ ] Result Panel remains visible.
- [ ] no Codex close path calls Next Day.
- [ ] player manually clicks Next Night.
- [ ] active Scene reloads once.

### 28.7 Day 2 / Little Prince

- [ ] currentDay reloads as 2.
- [ ] Little Prince is first spawn without debug key.
- [ ] Day 1 forced Stir/Shake/Build/Float sequence does not repeat.
- [ ] Little Prince-specific dialogue and intended order/method are used.
- [ ] completion flag is written only at approved completion point.
- [ ] second Day 2 customer is general.
- [ ] fallback works when LP reference is intentionally absent in a controlled test copy.
- [ ] reload after completion does not spawn first visit again.
- [ ] Day 3+ uses general/eligible special path only.

### 28.8 Persistence compatibility

- [ ] existing currentDay retained.
- [ ] existing littlePrinceFirstVisitCompleted retained.
- [ ] existing SURLHWA_UNLOCKED_RECIPES retained.
- [ ] existing SURLHWA_FIRST_RECIPE_REVEAL_SHOWN retained.
- [ ] fresh profile and legacy profile both tested.
- [ ] recipe IDs with current pipe format load.
- [ ] no test reset action deletes unrelated progress.

## 29. Open Questions

1. Main intended spawner의 manual Space mode가 의도인가, 최근 accidental Scene setting인가?
2. canonical BusinessDayManager는 CustomerSpawner host의 attached component가 맞는가?
3. Little Prince 첫 주문은 asset이 암시하는 만월/Float가 design Source of Truth인가?
4. Little Prince completion은 serve 성공, interaction 완료, 또는 단순 leave 중 어느 시점인가?
5. Customer_B를 generalCustomerPrefabs에 포함해야 하는가?
6. Day Result의 “새로 기록된 칵테일”은 오늘 unlock만인가, 전체 unlock 목록인가?
7. reputation은 day 간 persist해야 하는가?
8. BarScene_LayoutTest와 OldLayout_Disabled를 보존할 실제 이유가 있는가?
9. 7개 no-reference image는 concept archive인가 deletion candidate인가?
10. SettingsButton은 향후 scope인가 현재 missing implementation인가?
11. Static NotoSansKR atlas가 최종 문자열 전체를 포함하는가?
12. tracked UserSettings를 향후 repository에서 제거할 것인가?
13. 04_CONTENT_ROADMAP.md와 05_REBUILD_PLAN.md가 0 byte인 상태가 의도인가?

## 30. Final Classification Table

### 30.1 Evidence classification

| System/fact | Classification | Basis |
| --- | --- | --- |
| Git baseline clean | CONFIRMED | git status |
| Main Scene/build path | CONFIRMED | EditorBuildSettings + YAML |
| 49 custom scripts / no namespace | CONFIRMED | full source scan |
| 3 scenes / 18 prefabs / 28 custom SO assets | CONFIRMED | filesystem + m_Script GUID |
| Current automatic startup loop | CONFIRMED_BROKEN_CONFIGURATION | two spawners and serialized values |
| Manual debug-key customer loop | LIKELY | full code/wiring; Play Mode not run |
| Seat discovery/reserve/release | CONFIRMED_IMPLEMENTED | code + Scene tag/marker refs |
| Stir/Shake/Build/Float | CONFIRMED_IMPLEMENTED_STATIC | code + prefab + manager refs |
| Runtime mini game behavior | NEEDS_VERIFICATION | Play Mode not run |
| Day 1 gate in Spawner | CONFIRMED | code |
| Day 2 no tutorial repeat end-to-end | CONFIRMED_FALSE_CODE_PATH | Interaction flags reset on reload |
| LP Day 2 prefab selection | CONFIRMED_IMPLEMENTED_STATIC | code + Main Scene ref |
| LP full visit content | PARTIAL | dialogue/order assets dormant |
| Result→Codex→Close→Result | CONFIRMED_CODE_PATH | no result close/restart call |
| PlayerPrefs keys | CONFIRMED, 4 | repository-wide search |
| Audio/animation | CONFIRMED_ABSENT | asset extension scan |
| Missing local YAML fileID refs | NONE_FOUND | all Scene/Prefab local refs checked |
| Playable prototype at current commit | NEEDS_VERIFICATION | docs conflict with Scene startup config |

### 30.2 Primary action classification

| Scope | KEEP | MOVE | REFACTOR | MERGE | DELETE_CANDIDATE | UNKNOWN | DO_NOT_TOUCH |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Custom scripts (49, primary action) | 30 | 0 | 16 | 2 | 1 | 0 | 0 |
| Scenes (3) | 1 | 0 | 0 | 0 | 0 | 1 | 1 |
| Prefabs (18) | 15 | 0 | 0 | 0 | 2 | 1 | 0 |
| No-reference project images (7) | 0 | 0 | 0 | 0 | 7 | 0 | 0 |
| TMP/Unity managed roots | 0 | 0 | 0 | 0 | 0 | 0 | all |

MOVE 0은 최종 folder move가 불필요하다는 뜻이 아니다. 현재 phase의 primary responsibility action 기준이며, GUID-safe move는 Phase I에서 별도 분류/승인한다.

### 30.3 Documentation conflicts

| Document statement | Repository truth |
| --- | --- |
| Main loop playable/auto flow | current Main Scene normal spawner is manual; auto spawner has null prefab |
| general random prefabs | generalCustomerPrefabs empty; Customer_B unreferenced |
| tutorial Day 1 only target | Spawner fixed, Interaction forced routes still repeat after reload |
| Little Prince Day 2 first | prefab selection exists, but order/dialogue are not consumed |
| four mini games need audit | all four have code, prefab, manager link; runtime still needs Play verification |
| Story roadmap is Source of Truth | Docs/04_CONTENT_ROADMAP.md is 0 byte |
| rebuild plan | Docs/05_REBUILD_PLAN.md is 0 byte |
| backup Scene possibility | no backup Scene found |
| known PlayerPrefs keys 2 | actual keys 4 |

---

Audit stop condition reached: current state has been proven to the limit of static Repository analysis. No refactor, code fix, asset move, Scene edit, prefab edit, ProjectSettings edit, .meta edit, or runtime test was performed.

---

## H5 Current-State Completion Note (2026-09-07)

The audit findings above are the pre-refactor static baseline and remain as
historical evidence. They do not describe the completed Phase H persistence
ownership boundary.

Current active persistence architecture:

- `SurlhwaPlayerPrefsStore` is the sole direct `UnityEngine.PlayerPrefs`
  caller in project C# code.
- `BusinessDayPersistence` owns the legacy `currentDay` storage contract.
- `RecipeProgressPersistence` owns the legacy recipe-unlock and
  first-recipe-reveal storage contracts.
- `LittlePrinceVisitPersistence` owns the legacy
  `littlePrinceFirstVisitCompleted` storage contract.
- Domain managers retain gameplay semantics, and `SURLHWARegressionTools`
  uses the domain persistence APIs rather than raw keys.

The four existing key strings, value types, serialized representations,
missing-key behavior, and immediate-save timing were retained without a save
format migration. No Scene, Prefab, or ScriptableObject migration was required
for Phase H.

---

## Phase I Current Direction Note (2026-09-07)

The structural rebuild through Phase H remains complete and current. The
independent red-team review identified the player-facing core loop, rather than
another ownership or persistence refactor, as the next material project risk.

Bartending 2.0 is therefore the approved next development direction: preparation
actions must determine the actual drink state, and the remaining prototype
mini-games are reference/fallback implementations until their replacements are
validated. I0-A and I1 through I7.5 are the currently validated Phase I runtime
increments: dedicated narrative reading pauses patience; direct Pour Core
records milliliter liquid transfer in runtime container state before bridging to
the unchanged matcher; bounded amount variants prove arbitrary fixed quantities,
discrete Dash, and manual TopUp guidance; and I3 supplies separate Serving
Glass, Mixing Glass, and Shaker state with target-specific ice, full liquid
transfer, and non-scored Strain. I4 makes Build the first fully migrated route:
the player directly prepares the Serving Glass, manually completes preparation,
and that actual glass snapshot enters the existing result flow without the old
detached Build timing score. I5 makes Stir the second migrated route: direct
Pour Core feeds the Mixing Glass, ice is explicit, circular mouse movement
drives spoon motion and a bounded technique result, then non-scored Strain moves
the actual liquid to the Serving Glass. Technique quality remains independent of
recipe recognition. I6 makes Shake the third migrated route: direct Pour Core
feeds the Shaker, ice is explicit, a short alternating Left/Right rhythm drives
visible shaker movement and bounded timing quality, then non-scored Strain moves
the actual liquid to the Serving Glass. Timing quality remains independent of
recipe recognition. I7 makes Float the fourth migrated route: the Serving
Glass stays the sole liquid-composition authority while an ordered layer
companion preserves actual layer order and per-adjacent-boundary
Clean/Partial/Mixed quality from controlled spoon-assisted additions. It
supports both layered shots and final-float drinks; its old hold/release score,
timer, overflow, Jigger authority, and legacy session snapshot are inactive.
I7.5 then removed the proven-dead Jigger controller, fixed-increment shelf
adapters, `BrewSessionData`, and `BrewCompositionFactory`; prefab Jigger
components were removed and their visual roots were deactivated without Missing
Scripts. `BrewComposition` and the canonical container-to-matcher bridge remain
active. Gauge/target-zone visual hierarchy cleanup is intentionally deferred as
inert prefab residue. Current recipe assets do not yet author expected layer
order, so order scoring is deliberately deferred to I9. No broader Bartending
2.0 content implementation is implied. See
`Docs/11_BARTENDING_2_0_DESIGN.md` and Phase I in
`Docs/05_REBUILD_PLAN.md`.

I8 is now validated: the Serving Glass records one optional runtime garnish
identity and bounded placement (`Rim`, `InGlass`, or `Surface`) after actual
liquid preparation. The shared proof UI permits selection, replacement, clear,
and no-garnish submission, and renders from that same state. Garnish is carried
as a result-bound snapshot for later preparation evaluation but remains outside
the liquid `BrewComposition` / `RecipeMatchService` contract, technique grade,
and persistence. The temporary Lemon Twist, Cherry, and Olive choices are proof
controls rather than an adopted catalog or unlock system. No I9 content,
recipe-schema, matcher, or persistence implementation is implied.
