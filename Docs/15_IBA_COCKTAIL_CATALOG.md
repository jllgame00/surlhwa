# SURLHWA — IBA Cocktail Catalog

> **Document Status:** Long-term canonical cocktail collection roadmap
>  
> **Verified against:** International Bartenders Association official cocktail catalog
>  
> **Verification date:** 2026-09-28
>  
> **Catalog size:** 102 cocktails
>  
> **Category count:** The Unforgettables 34 / Contemporary Classics 34 / New Era 34

> **IMPORTANT**
>
> 이 문서는 IBA 102종을 SURLHWA의 장기 레시피·도감 풀로 정의한다.
>
> **102종을 즉시 전부 구현하라는 지시가 아니다.**
>
> 현재 구현 상태는 실제 Repository의 `CocktailData`, `CocktailRecipeData`,
> `RecipeCatalogData`, IngredientData, availability data와
> `Docs/13_POST_I16_PRODUCTION_GAP_AUDIT.md`를 우선 확인한다.

---

# 1. Purpose

SURLHWA의 장기 칵테일 수집·해금 구조는 다음 두 축을 사용한다.

1. **SURLHWA Originals**
   - 게임의 세계와 서사를 위해 직접 만든 칵테일.

2. **IBA Official Cocktail Collection**
   - 일반 손님, 레시피 발견, 도감 확장의 핵심 장기 콘텐츠.

IBA 칵테일은 특수 손님에게만 종속되지 않는다.

일반 손님과의 대화, 요청, 단서, 실제 조주를 통해 새로운 칵테일을
조금씩 알아가고 도감을 채우는 경험 자체가 주요 장기 진행 요소다.

특수 손님은 필요할 경우 IBA 칵테일을 사용할 수 있지만,
`특수 손님 = 특정 IBA 레시피 해금`이라는 1:1 구조를 기본 규칙으로 두지 않는다.

---

# 2. Source / Version Rule

이 문서의 102종 목록과 IBA 분류는 2026-09-28 기준 IBA 공식 웹사이트의
`All Cocktails` 목록을 기준으로 확인했다.

Official sources:

- https://iba-world.com/cocktails/
- https://iba-world.com/cocktails/all-cocktails/
- https://iba-world.com/cocktails/the-unforgettables/
- https://iba-world.com/cocktails/the-contemporary/
- https://iba-world.com/cocktails/the-new-era/

IBA 공식 목록은 향후 변경될 수 있다.

따라서 미래에 IBA 목록이 변경되더라도:

- 기존 SURLHWA stable ID를 자동 삭제하지 않는다.
- 기존 save compatibility를 깨뜨리지 않는다.
- 이미 구현된 레시피를 자동으로 제거하지 않는다.
- 새 IBA 목록을 현재 게임 데이터에 즉시 동기화하지 않는다.

공식 목록 변경은 별도 catalog audit 대상으로 취급한다.

---

# 3. Current SURLHWA Originals

다음 칵테일은 IBA 102종에 포함되지 않는 SURLHWA 오리지널이다.

| Cocktail | Status | Role |
|---|---|---|
| Seolya / 설야 | CURRENT_PRODUCTION | SURLHWA Original |
| Manwol / 만월 | CURRENT_PRODUCTION | SURLHWA Original |

오리지널 칵테일은 IBA 도감과 별개의 정체성을 유지한다.

---

# 4. IBA Implementation Status

## CURRENT_PRODUCTION

현재 bounded production demo에 실제 사용되는 IBA 칵테일.

- Dry Martini
- Negroni
- Manhattan
- Gin Fizz

## RESERVE_EXISTING

기존 데이터/호환성을 위해 유지되지만 현재 4-night production 일정에는
일반적인 필수 레시피로 사용하지 않는 IBA 칵테일.

- Hanky Panky
- French 75
- Last Word

## FUTURE_IBA

공식 IBA 장기 도감 풀에는 포함되지만 아직 production recipe로
구현·검증되지 않은 칵테일.

현재 수량:

- CURRENT_PRODUCTION: 4
- RESERVE_EXISTING: 3
- FUTURE_IBA: 95
- Total IBA: 102

---

# 5. Knowledge / Unlock Philosophy

IBA 도감은 시작 시 102종을 전부 공개하는 수집 목록이 아니다.

현재 Recipe Knowledge architecture를 장기적으로 그대로 활용한다.

```text
Unknown
→ Partial
→ Known
```

## Unknown

- 플레이어가 아직 충분히 알지 못하는 칵테일.
- 이름 또는 세부 레시피가 숨겨질 수 있다.

## Partial

일반 손님과 특수 손님의 대화, 관찰, 단서 등을 통해 일부 정보를 알게 된 상태.

예:

- 이름
- method
- ingredient identity
- amount
- preparation
- garnish

등을 현재 typed knowledge architecture가 지원하는 범위에서 공개한다.

## Known

플레이어가 레시피를 충분히 배우고 실제 discovery 조건을 만족해
정식 도감 항목으로 해금한 상태.

날짜가 바뀌거나 재료가 공급됐다는 이유만으로 Known 처리하지 않는다.

---

# 6. General Guest Discovery

IBA 102종의 주된 장기 발견 경로는 **일반 손님**이다.

일반 손님 콘텐츠는 다음과 같은 역할을 할 수 있다.

- 기억나는 칵테일의 일부 재료를 말한다.
- 이름은 모르지만 맛이나 향을 묘사한다.
- 특정 method의 칵테일을 찾는다.
- 자신이 예전에 마셨던 레시피를 설명한다.
- 이름만 알려주고 플레이어가 Guide의 Partial 정보를 해석하게 한다.
- 한 손님에게 여러 정보를 모두 주지 않고 서로 다른 방문에서 단서를 축적한다.

하지만 모든 칵테일을 복잡한 퀘스트처럼 만들어서는 안 된다.

일부 기본 레시피는 한 번의 자연스러운 대화로 충분히 배울 수 있고,
더 복잡하거나 상징적인 칵테일만 긴 discovery arc를 사용할 수 있다.

도감 해금은 콘텐츠 반복을 위한 노동이 아니라
플레이어가 점점 더 많은 술을 이해하는 바텐더가 되는 경험이어야 한다.

---

# 7. Special Guest Relationship

특수 손님 콘텐츠는 `Docs/14_SPECIAL_GUEST_ROADMAP.md`를 따른다.

특수 손님 역시:

- Exact Cocktail
- emotion-based Recommendation
- 이미 배운 IBA cocktail
- SURLHWA Original

등을 필요에 따라 사용할 수 있다.

그러나 유명 문학 캐릭터에게 유명 칵테일을 기계적으로 1:1 배정하지 않는다.

해당 ACT가 전달하려는 이야기와 감정이 먼저다.

---

# 8. Implementation Rules

Agent가 이 문서를 읽더라도 IBA 102종을 한 번에 Asset으로 생성하지 않는다.

새 IBA 칵테일을 production에 추가할 때마다:

1. IBA 공식 개별 cocktail page를 확인한다.
2. 공식 ingredient / amount / method / garnish를 기록한다.
3. 현재 IngredientData와 정확한 identity compatibility를 audit한다.
4. 비슷한 이름이라는 이유로 기존 ingredient identity를 합치지 않는다.
5. 필요한 경우 새로운 IngredientData를 bounded하게 추가한다.
6. CocktailData를 추가/확인한다.
7. CocktailRecipeData를 추가/확인한다.
8. RecipeCatalogData에 의도적으로 포함한다.
9. 실제 bartending runtime에서 표현 가능한지 확인한다.
10. 발견/해금 경로를 설계한다.
11. normal-play supply와 knowledge reachability를 검증한다.
12. validator를 추가하거나 확장한다.

IBA 공식 recipe와 SURLHWA gameplay representation이 다를 필요가 있다면
차이를 문서에 명시한다.

공식 정보를 조용히 바꾸거나 '게임이니까'라는 이유로 다른 레시피를
동일한 IBA cocktail이라고 취급하지 않는다.

---

# 9. Emotion Tag Rule

IBA의 공식 정보와 SURLHWA의 EmotionTag는 서로 다른 authority다.

IBA 공식 source가 제공하는 것은 칵테일 레시피 정보이지
SURLHWA의 감정적 의미가 아니다.

따라서 새 칵테일의 EmotionTag는:

- IBA source에서 자동 생성하지 않는다.
- ingredient만 보고 기계적으로 결정하지 않는다.
- Recommendation answer pool을 맞추기 위해 임의로 붙이지 않는다.
- SURLHWA의 설명, 분위기, 손님 콘텐츠를 고려해 별도로 디자인한다.

EmotionTag 결정은 필요할 경우 Human Design Review 대상이다.

---

# 10. Historical Shortlist

전체 IBA collection을 장기 풀로 확장하기 전, production 후보로 다음 8종을
우선 검토한 기록이 있다.

- Dry Martini
- Manhattan
- Negroni
- Last Word
- Gin Fizz
- French 75
- New York Sour
- Bramble

현재 이 shortlist는 IBA collection의 범위를 제한하지 않는다.

현재 실제 production 4종과 reserve 상태는 Repository가 authority다.

---

# 11. Complete IBA Official Catalog

아래 목록은 2026-09-28 기준 IBA 공식 `All Cocktails` catalog의 102종이다.

Status 의미:

- `CURRENT_PRODUCTION` — 현재 SURLHWA bounded demo에서 사용
- `RESERVE_EXISTING` — 기존 recipe/data가 있으나 현재 필수 일정에는 미사용
- `FUTURE_IBA` — 장기 도감 풀, 아직 production 구현되지 않음

## The Unforgettables — 34

| # | Cocktail | SURLHWA Status | Note |
|---:|---|---|---|
| 1 | Alexander | FUTURE_IBA | 장기 도감 후보 |
| 2 | Americano | FUTURE_IBA | 장기 도감 후보 |
| 3 | Angel Face | FUTURE_IBA | 장기 도감 후보 |
| 4 | Aviation | FUTURE_IBA | 장기 도감 후보 |
| 5 | Between the Sheets | FUTURE_IBA | 장기 도감 후보 |
| 6 | Boulevardier | FUTURE_IBA | 장기 도감 후보 |
| 7 | Brandy Crusta | FUTURE_IBA | 장기 도감 후보 |
| 8 | Casino | FUTURE_IBA | 장기 도감 후보 |
| 9 | Clover Club | FUTURE_IBA | 장기 도감 후보 |
| 10 | Daiquiri | FUTURE_IBA | 장기 도감 후보 |
| 11 | Dry Martini | CURRENT_PRODUCTION | 현재 4-night production IBA |
| 12 | Gin Fizz | CURRENT_PRODUCTION | 현재 4-night production IBA |
| 13 | Hanky Panky | RESERVE_EXISTING | 기존 데이터 유지 / 일반 production 일정에는 미사용 |
| 14 | John Collins | FUTURE_IBA | 장기 도감 후보 |
| 15 | Last Word | RESERVE_EXISTING | 기존 데이터 유지 / 일반 production 일정에는 미사용 |
| 16 | Manhattan | CURRENT_PRODUCTION | 현재 4-night production IBA |
| 17 | Martinez | FUTURE_IBA | 장기 도감 후보 |
| 18 | Mary Pickford | FUTURE_IBA | 장기 도감 후보 |
| 19 | Monkey Gland | FUTURE_IBA | 장기 도감 후보 |
| 20 | Negroni | CURRENT_PRODUCTION | 현재 4-night production IBA |
| 21 | Old Fashioned | FUTURE_IBA | 장기 도감 후보 |
| 22 | Paradise | FUTURE_IBA | 장기 도감 후보 |
| 23 | Planters Punch | FUTURE_IBA | 장기 도감 후보 |
| 24 | Porto Flip | FUTURE_IBA | 장기 도감 후보 |
| 25 | Ramos Fizz | FUTURE_IBA | 장기 도감 후보 |
| 26 | Remember the Maine | FUTURE_IBA | 장기 도감 후보 |
| 27 | Rusty Nail | FUTURE_IBA | 장기 도감 후보 |
| 28 | Sazerac | FUTURE_IBA | 장기 도감 후보 |
| 29 | Sidecar | FUTURE_IBA | 장기 도감 후보 |
| 30 | Stinger | FUTURE_IBA | 장기 도감 후보 |
| 31 | Tuxedo | FUTURE_IBA | 장기 도감 후보 |
| 32 | Vieux Carré | FUTURE_IBA | 장기 도감 후보 |
| 33 | Whiskey Sour | FUTURE_IBA | 장기 도감 후보 |
| 34 | White Lady | FUTURE_IBA | 장기 도감 후보 |

## Contemporary Classics — 34

| # | Cocktail | SURLHWA Status | Note |
|---:|---|---|---|
| 35 | Bellini | FUTURE_IBA | 장기 도감 후보 |
| 36 | Black Russian | FUTURE_IBA | 장기 도감 후보 |
| 37 | Bloody Mary | FUTURE_IBA | 장기 도감 후보 |
| 38 | Caipirinha | FUTURE_IBA | 장기 도감 후보 |
| 39 | Cardinale | FUTURE_IBA | 장기 도감 후보 |
| 40 | Champagne Cocktail | FUTURE_IBA | 장기 도감 후보 |
| 41 | Corpse Reviver #2 | FUTURE_IBA | 장기 도감 후보 |
| 42 | Cosmopolitan | FUTURE_IBA | 장기 도감 후보 |
| 43 | Cuba Libre | FUTURE_IBA | 장기 도감 후보 |
| 44 | French 75 | RESERVE_EXISTING | 기존 데이터 유지 / 일반 production 일정에는 미사용 |
| 45 | French Connection | FUTURE_IBA | 장기 도감 후보 |
| 46 | Garibaldi | FUTURE_IBA | 장기 도감 후보 |
| 47 | Grasshopper | FUTURE_IBA | 장기 도감 후보 |
| 48 | Hemingway Special | FUTURE_IBA | 장기 도감 후보 |
| 49 | Horse’s Neck | FUTURE_IBA | 장기 도감 후보 |
| 50 | Irish Coffee | FUTURE_IBA | 장기 도감 후보 |
| 51 | Kir | FUTURE_IBA | 장기 도감 후보 |
| 52 | Lemon Drop Martini | FUTURE_IBA | 장기 도감 후보 |
| 53 | Long Island Iced Tea | FUTURE_IBA | 장기 도감 후보 |
| 54 | Mai-Tai | FUTURE_IBA | 장기 도감 후보 |
| 55 | Margarita | FUTURE_IBA | 장기 도감 후보 |
| 56 | Mimosa | FUTURE_IBA | 장기 도감 후보 |
| 57 | Mint Julep | FUTURE_IBA | 장기 도감 후보 |
| 58 | Mojito | FUTURE_IBA | 장기 도감 후보 |
| 59 | Moscow Mule | FUTURE_IBA | 장기 도감 후보 |
| 60 | Pina Colada | FUTURE_IBA | 장기 도감 후보 |
| 61 | Pisco Sour | FUTURE_IBA | 장기 도감 후보 |
| 62 | Rabo de Galo | FUTURE_IBA | 장기 도감 후보 |
| 63 | Sea Breeze | FUTURE_IBA | 장기 도감 후보 |
| 64 | Sex on the Beach | FUTURE_IBA | 장기 도감 후보 |
| 65 | Singapore Sling | FUTURE_IBA | 장기 도감 후보 |
| 66 | Tequila Sunrise | FUTURE_IBA | 장기 도감 후보 |
| 67 | Vesper | FUTURE_IBA | 장기 도감 후보 |
| 68 | Zombie | FUTURE_IBA | 장기 도감 후보 |

## New Era — 34

| # | Cocktail | SURLHWA Status | Note |
|---:|---|---|---|
| 69 | Bee’s Knees | FUTURE_IBA | 장기 도감 후보 |
| 70 | Bramble | FUTURE_IBA | 장기 도감 후보; 과거 IBA shortlist |
| 71 | Canchanchara | FUTURE_IBA | 장기 도감 후보 |
| 72 | Chartreuse Swizzle | FUTURE_IBA | 장기 도감 후보 |
| 73 | Dark ‘N’ Stormy | FUTURE_IBA | 장기 도감 후보 |
| 74 | Don's Special Daiquiri | FUTURE_IBA | 장기 도감 후보 |
| 75 | Espresso Martini | FUTURE_IBA | 장기 도감 후보 |
| 76 | Fernandito | FUTURE_IBA | 장기 도감 후보 |
| 77 | French Martini | FUTURE_IBA | 장기 도감 후보 |
| 78 | Gin Basil Smash | FUTURE_IBA | 장기 도감 후보 |
| 79 | Grand Margarita | FUTURE_IBA | 장기 도감 후보 |
| 80 | IBA Tiki | FUTURE_IBA | 장기 도감 후보 |
| 81 | Illegal | FUTURE_IBA | 장기 도감 후보 |
| 82 | Jungle Bird | FUTURE_IBA | 장기 도감 후보 |
| 83 | Missionary's Downfall | FUTURE_IBA | 장기 도감 후보 |
| 84 | Naked and Famous | FUTURE_IBA | 장기 도감 후보 |
| 85 | New York Sour | FUTURE_IBA | 장기 도감 후보; 과거 IBA shortlist |
| 86 | Old Cuban | FUTURE_IBA | 장기 도감 후보 |
| 87 | Paloma | FUTURE_IBA | 장기 도감 후보 |
| 88 | Paper Plane | FUTURE_IBA | 장기 도감 후보 |
| 89 | Penicillin | FUTURE_IBA | 장기 도감 후보 |
| 90 | Pisco Punch | FUTURE_IBA | 장기 도감 후보 |
| 91 | Porn Star Martini | FUTURE_IBA | 장기 도감 후보 |
| 92 | Russian Spring Punch | FUTURE_IBA | 장기 도감 후보 |
| 93 | Sherry Cobbler | FUTURE_IBA | 장기 도감 후보 |
| 94 | South Side | FUTURE_IBA | 장기 도감 후보 |
| 95 | Spicy Fifty | FUTURE_IBA | 장기 도감 후보 |
| 96 | Spritz | FUTURE_IBA | 장기 도감 후보 |
| 97 | Suffering Bastard | FUTURE_IBA | 장기 도감 후보 |
| 98 | Three Dots and a Dash | FUTURE_IBA | 장기 도감 후보 |
| 99 | Tipperary | FUTURE_IBA | 장기 도감 후보 |
| 100 | Tommy's Margarita | FUTURE_IBA | 장기 도감 후보 |
| 101 | Trinidad Sour | FUTURE_IBA | 장기 도감 후보 |
| 102 | Ve.N.To | FUTURE_IBA | 장기 도감 후보 |

---

# 12. Catalog Integrity Checks

이 문서 기준:

```text
The Unforgettables      34
Contemporary Classics   34
New Era                 34
---------------------------
Total                  102
```

SURLHWA state:

```text
CURRENT_PRODUCTION       4
RESERVE_EXISTING         3
FUTURE_IBA              95
---------------------------
Total IBA              102
```

SURLHWA Originals:

```text
Seolya
Manwol
```

따라서 현재 장기 canonical cocktail universe의 최소 기준은:

```text
102 IBA
+ 2 current SURLHWA Originals
= 104 identified cocktails
```

향후 SURLHWA Original이 추가되면 이 숫자는 증가할 수 있다.

---

# 13. Non-Goals

이 문서는 다음을 의미하지 않는다.

- 102개를 즉시 모두 구현한다.
- 102개 IngredientData를 한 번에 추가한다.
- 모든 IBA가 네 밤 데모에 등장한다.
- 모든 IBA가 동일한 discovery 난이도를 가진다.
- 모든 IBA에 고유 특수 손님이 필요하다.
- 모든 IBA를 현재 production supply에 넣는다.
- 모든 칵테일을 시작 시 도감에서 노출한다.
- IBA에 없는 SURLHWA Original을 만들 수 없다.
- IBA 목록 변경 시 기존 게임 데이터를 자동 삭제한다.

---

# 14. Long-Term Design Goal

IBA Collection의 목적은 체크리스트 102개를 채우는 것 자체가 아니다.

플레이어가 처음에는 몇 잔밖에 모르는 상태에서 시작해,
다양한 손님의 이야기를 듣고 실제로 술을 만들면서:

> 예전에는 이름도 몰랐던 술을 이제는 알고,
> 재료와 조주법을 기억하고,
> 누군가의 이야기를 들었을 때 자연스럽게 한 잔을 떠올릴 수 있는
> 바텐더가 되어가는 것

이 장기 경험이 핵심이다.

도감은 그 성장을 기록하는 결과물이어야 하며,
성장을 강요하는 수집 노동이 되어서는 안 된다.

특수 손님이 바에 기억을 남긴다면,
IBA 레시피들은 플레이어 자신이 바텐더로서 쌓아온 지식을 남긴다.


## First-pass campaign expansion ? 2026-09-28

The status tables above describe the historical four-night baseline. The owner-authorized campaign now promotes the three existing reserve recipes to campaign content: Hanky Panky on night 6, French 75 on night 10, Last Word on night 15. Current campaign IBA scope is 7, remaining future scope 95; no new cocktail/ingredient IDs or catalog entries were needed. Ordinary guests teach complete quantities, method, preparation and garnish before a retry-safe discovery attempt. Supply opens on those nights without granting knowledge. Runtime normal-play proof is pending integration.

Official recipes rechecked on 2026-09-28:

- [Hanky Panky](https://iba-world.com/iba-cocktail/hanky-panky/): London Dry Gin 45 ml, Sweet Red Vermouth 45 ml, Fernet 7.5 ml; stir with ice, strain; orange zest.
- [French 75](https://iba-world.com/iba-cocktail/french-75/): Gin 30 ml, Fresh Lemon Juice 15 ml, Sugar Syrup 15 ml, Champagne 60 ml; shake the first three, strain, add Champagne, gently stir; no garnish. Existing Simple Syrup is the documented sugar-syrup representation; Champagne remains a measured 60 ml Pour after Strain.
- [Last Word](https://iba-world.com/iba-cocktail/last-word/): Gin, Green Chartreuse, Maraschino Luxardo and Fresh Lime Juice, 22.5 ml each; shake with ice and strain; no garnish.

Existing exact identities, ounce storage conversion, glassware and preparation assets match these quantities and sequences. Chilled glass is presentation metadata, not a new temperature simulation. Modest Bitterness / Awakening / Clarity interpretations accompany new authored Korean descriptions; they are SURLHWA authorship, not IBA facts. No special character is assigned one of these as a password.
