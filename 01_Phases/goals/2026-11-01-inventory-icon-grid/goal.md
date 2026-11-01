# 인벤토리 아이콘 격자

## 재개 지점

### 현재 재개 카드 — 2026-11-01 범위 승인 뒤

**다음 첫 일:** 선행 시험 계약(Unity 보호 규칙 원문 포함)을 쓰고 새 `[Art 시험 Astra]`를 띄운다. 계약이 준비되면 W1 Unity 창을 메인에 「준비됨」으로 청한다.

| 항목 | 현재 |
|---|---|
| 상태 | 범위는 메인이 승인했다(`msg_b79ad103b86e`, 아래 「범위 초안과 승인」). 선행 시험 계약 준비 중이다. |
| 리드 | `[Art 리드 Opus]`(claude-opus-5-5 xhigh), 대화 ID `c92c0adb-250c-48fd-b4cc-8d6c85df4386`. 회신 주소는 `run:run_6687e0512b2c`다. |
| branch | `feat/art-inventory-icon-grid-20261101`이다. origin/main `ffe7f983b96eda8002a27169b3e8b7cb5eb32c72`(PR292 병합)에서 만들었다. |
| 관계 | Core 인벤토리 24칸 goal(`core-active/01_Phases/goals/2026-11-01-inventory-24-slots/goal.md`, Core PR 때 main에 들어간다)이 패킷·서버·공유 칸 수를 바꾼다. 병합 순서는 이 goal의 격자 PR이 먼저다(아래 「Core 24칸 계약 — 확정」). |
| 실행 창 | 아직 0회다. Unity·WSL·DB 창은 메인이 배정한다. |
| 근거 폴더 | `.backups/verification/2026-11-01-inventory-icon-grid/`다. Git 제외 폴더다. 앞 goal의 근거 폴더 `.backups/verification/2026-10-29-inventory-character-select-look/`에 초안 v1~v6과 facts 1031판이 있다. |

## 적용 중인 사용자 결정

### 10-31 사용자 지시와 92 B·93 A — 4×N 격자·아이콘·마우스 올림 상세·우클릭 — msg_d75240eadc75

- 사용자 원문(메인 `msg_d75240eadc75`가 전달): 「그리고 추가 개선안으로 인벤토리는 4xN 형태로 만들어줘, 그리고 각 칸은 아이콘만 띄우고, 마웃스 포인터로 올려놨을때 상세표기가 나오게 개편하자」
- 사용자 결정 원문(같은 메시지): 「대시보드 결정 응답: 1) 92 - 인벤토리 4칸 격자 개편을 리셋 뒤에 할지, 지금 녹화 준비와 함께 할지 → B 지금 함께 · 2) 93 - 아이콘만 남은 칸에서 아이템을 어떻게 쓸지(지금은 줄마다 「사용」 버튼) → A 우클릭으로 사용」
- 작업자 전환 원문(같은 메시지): 「그러면 작업자를 Codex로 바꿔줘, 이번에 사용한도 다시 2배 되었거든」
- 이어지는 조건: 결정 43의 조건(`msg_6ac2652214cc`) 원문은 「그림풍은 우리느낌으로 가되 ui 배치랑 연출을 참고하라고 지시해줘」다. 참고 게임은 배치와 연출만 참고한다. 원본 캡처·그림·아이콘·로고·글꼴·문구는 쓰지 않는다.

### 10-31 결정 95 B·96 A — 칸 구석 수량 숫자와 창 높이 — msg_021f63e57b9f

- 95 B 원문(메인 전달): 「대시보드 결정 응답: 2) 95 - 인벤토리 격자 칸 구석에 수량 숫자를 둘지(네 원문은 「아이콘만」) → B 구석 작은 숫자」
- 96 A 원문(메인 전달): 「3) 96 - 인벤토리 창을 두 줄 격자에 맞게 낮출지(지금 390×720, 결정 66의 1.3:2.4) → A 높이 줄임」
- 이어진 사용자 원문(메인 전달): 「아니면 인벤토리 크기를 더 늘려도 돼, 내 생각에는 메이플스토리 인벤토리 크기 정도면 상관없어」
- 메인 지시(같은 메시지): 「어느 쪽이든 격자는 열 4개를 고정하고 줄 수는 InventoryLimits.MaxSlots에서 계산해라. 창 높이도 줄 수에서 계산해라」

### 10-31 결정 97 A — 24칸(4×6) — msg_99abe9fb4182

- 사용자 원문(메인 전달): 「대시보드 결정 응답: 1) 97 - 인벤토리를 24칸(4×6)으로 늘릴지, 8칸(4×2)으로 둘지 → A 24칸으로 늘림」
- 97 A 선택지 문장(현황판, 같은 메시지): 「Core가 지금 인벤토리 패킷과 서버 칸 수를 24로 바꾸고 서버·Unity 양쪽을 검증한다. Art 격자는 칸 수를 따라 4×6이 되고 창은 지금 390×720 가까이 된다. 시연 빌드는 Core PR까지 기다린다」
- 메인 지시(같은 메시지, 사용자 결정 아님): 1항은 칸 수 변경(패킷·서버·공유 상수)은 Core가 하고 Art는 패킷·서버·공유 코드를 쓰지 않는다는 것이다. 2항은 격자 PR이 Core 24칸 PR보다 먼저 들어가도 되고, 그동안은 MaxSlots 8로 2줄이라는 것이다.
- 메인 해석: 96 A의 「약 430」은 8칸일 때의 숫자였다. 창 높이는 줄 수에서 계산한다.

### 10-31 메인 결정 창 높이 A와 11-01 사용자 결정 100 A·101 A·102 A — msg_599b28e18b73·msg_da3be91ab030

- 메인 결정(`msg_599b28e18b73`, 2026-10-31T17:58:00Z) 1항 원문: 「창 높이는 A다. 칸 한 변 81을 지키고 창 높이는 줄 수에서 계산한다(6줄 790~810). 정확한 값은 네 말대로 RED 계약에서 캡처 전에 고정한다.」
- 같은 메시지 3항 원문: 「1920×1080에서 HUD 아래 왼쪽 자리에 810 높이 창이 다 들어가는지를 완료조건 2의 「화면 안」 단정에 포함해라. 1280×720은 맞춤 규칙으로 줄어든 크기를 캡처로 확인한다.」
- 사용자 결정 원문(메인 `msg_da3be91ab030` 전달): 「대시보드 결정 응답: 1) 100 - 인벤토리 24칸(4열×6줄) 창 높이를 키울지(칸 81px), 지금 높이 720px를 지킬지 → A 창을 키운다(칸 81px) · 2) 101 - 목표 종료 정리 점검 세션(Gardener)을 앞으로도 Codex Sol max로 열지, 정본대로 Claude Opus로 돌릴지 → A Codex Sol max 유지 · 3) 102 - 「작업자를 Codex로」에 선행 시험 작성자도 넣을지(지금 해석), 구현자만으로 좁힐지 → A 시험 작성자 포함(대신 Astra로)」
- 메인 적용(같은 메시지): 24칸 창 높이는 칸 81px·창 790~810px로 사용자가 확정했다. 지난 세션 메인 결정과 같다.

### 10-31~11-01 메인 결정 — 6줄 캡처 시점과 첫 화면 처리 — msg_e1d856276e79·msg_ad79455eb3b9

- 6줄 캡처(`msg_e1d856276e79` 2항, 메인 결정, 원문): 「6줄 캡처는 A로 한다. 격자 goal이 Core 24칸 PR 병합 뒤 main에서 6줄 캡처를 찍어 결과 기록 PR에 넣는다. 캡처 시험의 주인이 Art라서 그렇다. 격자 goal 종료가 Core PR을 기다리는 것은 받아들인다. 1920×1080 「화면 안」 단정을 줄 수 인자로 6줄까지 볼지는 새 리드가 RED 계약에서 정한다. 인계에 이 문장을 남겨라.」
- 첫 화면(`msg_ad79455eb3b9` O3, 메인 결정, 원문): 「다음 기동 계약은 R-6을 따른다: 첫 화면에 업데이트 창·선택창이 뜨면 리드·작업자는 아무것도 누르지 않고 메인에 보고한다. 메인이 처리한다(업데이트 창이면 Esc로 넘김).」

### 11-01 작업자 배정 — 저장소 CLAUDE.md 4A 절

- 정본은 저장소 `CLAUDE.md`의 「결정 4A 적용 기간의 Claude 리드와 작업자 배정」 절(ffe7f983)이다. 사용자 원문은 그 절과 위 `msg_d75240eadc75`·`msg_da3be91ab030`에 있다.
- 이 goal의 배정은 아래 「작업 방식」에 적는다.

## 지금 상태(사실)

기준은 origin/main `ffe7f983b96eda8002a27169b3e8b7cb5eb32c72`다. F1·F2·F3은 근거 폴더의 저장 출력이다.

- F1: `inventory-grid-scope-facts-1101-01.txt`(스크립트 `inventory-grid-scope-facts-1101-01.sh`, 1031판 사본)
- F2: `inventory-grid-scope-facts-1101-02.txt`(스크립트 `inventory-grid-scope-facts-1101-02.sh`, 1031판 사본)
- F3: `inventory-grid-scope-facts-1101-03.txt`(스크립트 `inventory-grid-scope-facts-1101-03.sh`, Core가 짚은 시험과 8칸 가정)
- 1031판 출력과의 차이: `facts-diff-1031-vs-1101-01.txt`. 코드 줄 번호는 같다. client.md 인용 줄은 10줄씩 밀렸고, probe를 쓰는 시험 파일이 둘 늘었다.

| 항목 | 지금 | 근거 |
|---|---|---|
| 칸 배치 | 이미 2열×4행 격자다. 칸 하나는 176×109이고 간격은 14다. | F1 `InventoryPanelView.cs:81`·`:82`·`:85` |
| 칸 안 | 아이콘, 「이름 ×수량」 글자, 「사용」 버튼이 위에서 아래로 있다. | F1 `InventoryPanelView.cs:90`·`:96`·`:100`·`:165` |
| 창 크기 | 390×720이다. 창 배치 goal의 66 C·70 A가 정했다. | F1 `InventoryPanelView.cs:15`·`:16`, F2 앞 goal goal.md:229 |
| 8칸의 출처 | 고정 길이 패킷이다. 공유 상수 `MaxSlots = 8`과 PDL의 slot0~slot7이다. | F1 `InventoryLimits.cs:3`·`:6`, `PDL.xml:416`·`:424`·`:439` |
| 화면 코드의 칸 수 | 배열과 순회는 이미 `InventoryLimits.MaxSlots`를 쓴다. | F3 `InventoryPanelView.cs:31`~`:35`·`:76`~`:78`·`:159` |
| 사용 흐름 | 「사용」 → `UseSlot` → `TryUse(itemId)` → `C_ItemUse{itemId, expectedRevision}`다. `CanUse`가 동기화·결과 대기·정의·사용 가능을 본다. | F1 `InventoryPanel.cs:133`·`:138`, `InventoryRequestController.cs:48`~`:51`·`:72` |
| 공유 데이터 | 이름(`DisplayName`)과 사용 시 재화(`CurrencyOnUse`)만 있다. 사용 가능은 `CurrencyOnUse > 0`이다. 재료는 0, 재화 주머니는 50이다. 설명 문구 필드는 0개다. | F1 `ItemDefinition.cs:21`·`:23`·`:25`, `ItemCatalog.cs:8`·`:9`, 설명 검색 0 |
| 마우스 올림 | `InventorySlotHoverHighlight`가 매 프레임 마우스 위치를 칸과 비교해 테두리를 켠다. 칸 바탕은 raycast를 받지 않는다. 툴팁 클래스는 0개다. | F1 `InventorySlotHoverHighlight.cs:9`·`:10`·`:31`~`:36`, 검색 0 |
| 우클릭 | UI 입력 map에 `RightClick` → `<Mouse>/rightButton`이 이미 있다. 게임플레이에서 읽는 곳은 엔딩 화면 하나뿐이다. | F1 inputactions `:529`·`:961`·`:965`, `EndingController.cs:57` |
| 마우스와 공격 | 마우스는 공격 바인딩이 아니다(결정 65 A). 공격은 Left Ctrl·Enter다. 창 위에서 키보드 공격은 그대로 된다. | F1 `InventorySceneLifecycleTests.cs:585`·`:625`·`:644` |
| raycast | 열린 창은 버튼과 제목 띠의 잡는 칸만 raycast를 받는다. | F1 `UiWindowAppearanceContractTests.cs:592`·`:593`, client.md:50 |
| 만드는 곳 | 장면·prefab 없이 `CombatBootstrap`이 실행 중 코드로 만든다. | F1 `CombatBootstrap.cs:27`·`:64`·`:154` |
| 처치 보상 글 | 「+금화 N」 떠오르는 글은 이미 있다(Effect 몫, 끝남). | F1 client.md:78 |

- 앞 초안의 읽기 전용 조사 보고는 「마우스 왼쪽 = 공격」이라고 했다. 저장 출력에서 `<Mouse>/leftButton`은 한 줄(inputactions :906)뿐이고, 마우스가 공격이 아님은 시험 :585·:625가 단정한다. 그래서 이 보고는 틀렸고, 칸 좌클릭이 공격으로 새는 문제는 없다.

## 만들 것

1. 4열 격자. 열 수 4는 고정한다. 줄 수는 `InventoryLimits.MaxSlots`에서 계산한다(MaxSlots ÷ 4, 올림). 칸 수와 줄 수를 코드에 숫자로 박지 않는다. 사용자 결정 97 A로 Core가 MaxSlots를 24로 바꾸면 4열×6줄이다. 격자 PR은 Core 24칸 PR보다 먼저 들어가도 되고, 그동안은 지금 MaxSlots 8로 2줄이다(메인 msg_99abe9fb4182 2항). 시험도 8이나 24를 박지 않고 MaxSlots에서 기대값을 계산한다.
2. 칸에는 아이콘과 구석 수량 숫자만 보인다. 빈 칸은 빈 칸 조각만, 채운 칸은 채운 칸 조각과 아이콘을 보인다. 칸에서 「이름 ×수량」 글자와 「사용」 버튼을 뺀다.
   - 구석 수량 숫자(사용자 결정 95 B, 메인 msg_021f63e57b9f): 수량이 2 이상이면 칸 오른쪽 아래에 작은 숫자를 둔다. 수량 1은 아이콘만이다.
3. 마우스 올림 상세 창. 열린 창의 채운 칸에 포인터가 있으면 뜬다.
   - 재화 주머니처럼 쓸 수 있는 아이템: 이름, 「수량 N」, 「사용 효과: 금화 +N」, 마지막 줄 「우클릭: 사용」이다. N은 공유 데이터 `CurrencyOnUse`에서 읽고 숫자를 코드에 박지 않는다.
   - 재료처럼 못 쓰는 아이템: 이름과 「수량 N」만 보인다.
   - 빈 칸에는 뜨지 않는다. 포인터가 칸을 떠나거나, 창이 닫히거나, 잠기거나, 씬이 바뀌면 사라진다.
   - 상세 창은 raycast를 받지 않는다. 화면 안에 다 들어가게 자리를 뒤집거나 민다.
   - 글자는 밝은 창의 기준 잉크 `s_ink`·`s_quietInk`(F1 `InventoryPanelView.cs:20`·`:21`)를 쓰고 새 색을 만들지 않는다. 바탕 대비는 WCAG 4.5:1 이상이다(이 goal의 글자 색 A·msg_1f6fd458913d를 이어 적용하자는 제안이다).
   - 바탕은 기존 밝은 조각(`BrightComponents`의 판·inset 류)을 다시 쓴다. 새 이미지는 만들지 않는다.
4. 우클릭 사용(93 A). 쓸 수 있는 아이템 칸에서 오른쪽 버튼을 한 번 누르면 기존 `UseSlot` → `TryUse` 흐름을 그대로 탄다.
   - 못 쓰는 아이템 칸과 빈 칸의 우클릭은 아무것도 하지 않는다.
   - 지금 버튼이 막히는 때(일시정지, 씬 전환 중, Ready 전, 동기화 전, 결과 대기 중)에는 우클릭도 막힌다.
   - 다른 창이 칸을 덮고 있으면 그 자리 우클릭은 쓰지 않는다. 보상 창도 HUD 아래 공간에 뜨므로 인벤토리와 겹칠 수 있다(client.md:50·:62).
5. 창 높이는 줄 수에서 계산한다. 너비 390은 그대로 둔다. 계산식은 「칸 시작 134 + 줄 수×칸 한 변 + (줄 수 − 1)×세로 칸 사이 + 칸 아래 공간 120」이다. 지금 배치 상수에서 칸 한 변은 81이다(앞 goal 근거 폴더 `compute-grid-window-height-1101-01.txt`).
   - 24칸 6줄: 세로 칸 사이 14면 810, 지금 줄 간격 10이면 790이다. 97 A 선택지의 「390×720 가까이」보다 크다.
   - 높이를 720으로 지키려면 칸 한 변이 66(세로 사이 14)~69(사이 10)로 줄어든다(같은 출력).
   - Core PR 전 8칸 2줄이면 426~430이다(96 A의 「약 430」).
   - 결정: 계산값(칸 한 변 81)이다. 메인 결정 A(msg_599b28e18b73)를 사용자 결정 100 A가 확정했다(메인 msg_da3be91ab030). 70 A의 왼쪽 12 자리와 최대 1.3배 맞춤 규칙(client.md:50)은 그대로라, 작은 화면에서는 창 전체가 그 규칙으로 줄어든다. 정확한 값은 RED 계약에서 캡처 전에 고정한다.
6. 칸 위 강조 테두리는 그대로 둔다.
7. client.md :50·:54의 인벤토리 문장을 새 동작에 맞춘다. 「client.md는 창 배치 PR만 고친다」에 해당하는 PR이다. :50의 칸 수는 숫자 대신 공유 칸 수(`InventoryLimits.MaxSlots`)로 적는다. Core 24칸 branch도 같은 :50 줄을 고치기 때문이다(아래 「Core 24칸 계약 — 확정」의 client.md 줄).

## 건드릴 곳

| 경로 | 무엇 | 주인 |
|---|---|---|
| `03_Client/Assets/Scripts/UI/InventoryPanelView.cs` | 격자, 칸 내용, 상세 창 조립 | Art |
| `03_Client/Assets/Scripts/UI/InventoryPanel.cs` | 우클릭을 `UseSlot`에 잇기 | Art |
| `03_Client/Assets/Scripts/UI/InventorySlotHoverHighlight.cs` 또는 같은 폴더의 새 파일 | 올린 칸을 상세 창·우클릭과 나눠 쓰기. 새 파일이면 이름은 책임으로 짓는다(예: 상세 창). 위치와 이름 근거는 리드 맥락 메모에 남긴다. | Art |
| 기존 EditMode 시험 | 파일별 참조 줄 수: `InventoryPresentationTests` 11, `UiWindowAppearanceContractTests` 7, `UiWindowCompositionRenderTests` 20, `InventoryPanelToggleTests` 2(F2). 그 밖에 `InventoryWindowPlacementTests`(아이콘·글자·버튼 쌓기), `InventoryPanelViewAppearanceTests`(`SlotHover_KeepsOnlyButtonsAsRaycastTargets` 등) | Art |
| Core 작업본에서 실패하는 Art 시험 3건(Core I24-A01~03) | `InventoryWindowPlacementTests.CurrencyAndUseResult_TakeSeparateLines_BelowTheEightSlots`(F3 :139), `UiWindowCompositionRenderTests.Inventory_OnCamera_…`의 1920×1080·1280×720 두 경우(F3 :367). 격자 PR이 MaxSlots 계산으로 고친다. | Art |
| 8칸을 숫자로 박은 Art 시험 줄 | `InventoryPresentationTests` :477~:478(빈 칸 순회 `slot <= 8`, Core 참고 `msg_11d7daa692d2`), `InventoryPanelViewAppearanceTests` :57·:117·:127·:179, `UiWindowAppearanceContractTests` :257·:583·:1051, `UiWindowCompositionRenderTests` :380·:428, `UiWindowLiveCaptureTests` :571, `InventorySceneLifecycleTests` :263(메시지), `InventoryWindowPlacementTests` :136~:154(F3). 만들 것 1대로 MaxSlots에서 기대값을 계산한다. | Art |
| `BrightUiKitImportTests` | Core 제안 3항이 든 파일이다. F3의 「eight」·「Slot」 줄(:53·:303~:325)은 밝은 스킨의 빈 sprite 필드 이야기이고, 인벤토리 칸 줄은 0이다. W1 기준 실행에서 깨지는지만 보고, 깨지지 않으면 쓰지 않는다. | Art |
| 기존 PlayMode 시험 | `InventorySceneLifecycleTests` 20, `HudGoldAndPanelToggleSceneTests` 2, `InventoryServerIntegrationTests` 3, `InventoryWindowPlacementSceneTests` 1, `UiWindowLiveCaptureTests` 1(F2) | Art. 단 `InventoryServerIntegrationTests`는 주인이 미정이다(PR286 goal). 24칸 서버에서 이 시험의 칸 수 가정이 깨지면 Core·Art·메인이 그때 정한다(아래 「Core 24칸 계약 — 확정」). 그 전에는 Art가 고친다고 보고 진행한다. |
| `InventoryPanelProbe`(`InventorySceneLifecycleTests.cs:24`~`:93`) | `Slot()`·`Use()`를 새 구조에 맞춘다. 이 probe는 보상 창·길드·HUD 시험 등 여러 파일이 쓴다(F2 「InventoryPanelProbe를 쓰는 파일」 목록). 1031판보다 `BrightRemainingScreensCaptureTests`·`PauseMenuAppearanceSceneTests` 둘이 늘었다. 쓰는 파일을 하나씩 고치지 않고 probe에서 맞춘다. | Art |
| 새 시험 | 선행 RED 시험, 독립 검증 시험, 캡처 시험 | Art |
| `00_Document/domains/client.md` :50·:54 | 인벤토리 표시·사용 문장 | Art |
| 이 goal 파일, CURRENT의 Art 줄, BACKLOG `inventory-icon-grid` 행의 상태 칸(`goal 승격 → 링크`) | 기록 | Art |

## 하지 않을 것

- 칸 수 변경(패킷·서버·공유 상수)은 Core가 한다. Art는 패킷·서버·공유 코드를 쓰지 않는다(메인 msg_99abe9fb4182 1항). DB·생성 코드·공유 DLL 쓰기도 0이다. 클라 받는 쪽 `InventorySnapshotHandler`·`InventorySnapshotState`도 쓰지 않는다. 이번 24칸 변경에서 Core가 맡는다(아래 「Core 24칸 계약 — 확정」).
- Core가 고치는 클라 프로토콜 호환 시험(`InventoryClientTestSupport`·`InventoryProtocolCompatibilityTests`·`InventoryClientContractTests`)을 쓰지 않는다. F3의 `InventoryProtocolCompatibilityTests` :156 `Assert.AreEqual(8, InventoryLimits.MaxSlots)`와 `InventoryClientTestSupport` :161도 Core 몫이다.
- 아이템 설명 문구나 새 아이템 데이터 필드를 만들지 않는다. 공유 데이터에 설명이 없다.
- 좌클릭 사용, 두 번 클릭 사용, 끌어 옮기기, 칸 바꾸기·정렬·나누기, 키보드·게임패드 사용 경로, 사용 확인 창을 만들지 않는다.
- 입력 설정 파일(`InputSystem_Actions.inputactions`)을 쓰지 않는다. 공격 키(65 A)도 그대로다.
- 처치 보상 떠오르는 글, 캐릭터 선택창(Content 85 A), 길드 창(Content), 75 「버튼 조각을 진하게」(BACKLOG), 다른 창은 손대지 않는다.
- 새 이미지 생성은 0회다. 리드가 꼭 필요하다고 보면 호출마다 메인 승인을 받는 예외로 올린다.
- 참고 게임의 원본 자료를 쓰지 않는다(결정 43 조건).
- Content 소유 파일(보상 창·길드 시험 등) 쓰기는 0이다. Content 시험이 깨지면 고치기 전에 Content 리드와 메인에 먼저 알린다.
  - 알려진 위험(초안 v6 Q2의 「A의 위험」): 보상 창 겹침 시험(Content 소유)이 인벤토리 창 사각형과 겹침 넓이를 잰다(F2 `DungeonClearRewardOverlapSceneTests.cs:445`·`:468`). 창 높이가 바뀌면 이 값이 바뀔 수 있다. W1 기준 실행으로 먼저 본다.
- 미추적 석재·지형 5개(`03_Client/Assets/Art/Environment/Structure/Shared`·`Terrain`과 `.meta`, `01_Phases/goals/2026-10-13-demo-video-art/concept-alignment-plan-draft.md`)는 사용자 결정 전이라 읽기·쓰기·삭제·commit 0이다.

## 관찰 가능한 완료조건

1. 열린 인벤토리에 칸 MaxSlots개가 4열로 보이고, 줄 수는 MaxSlots ÷ 4(올림)다. 창 높이는 그 줄 수에서 나온 값이다. 칸 안에는 칸 조각, 아이콘, 수량 2 이상일 때의 오른쪽 아래 작은 숫자만 있다. 「이름 ×수량」 글자와 「사용」 버튼은 없다. MaxSlots 24(97 A, Core PR 뒤)면 24칸·6줄이고, 창 높이는 만들 것 5의 계산값이다. Core PR 전이면 8칸·2줄이다. 시험은 어느 쪽이든 MaxSlots에서 기대값을 계산해 통과한다.
2. 채운 칸에 포인터를 올리면 상세 창이 뜬다. 재화 주머니는 이름·수량·「사용 효과: 금화 +50」·「우클릭: 사용」이고, 재료는 이름·수량만이다. 빈 칸에서는 뜨지 않는다. 칸을 떠나거나 창이 닫히면 사라진다. 상세 창은 raycast를 받지 않고 1920×1080과 1280×720에서 화면 안에 다 들어간다. 「화면 안」 단정에는 인벤토리 창 자체도 넣는다. 1920×1080에서는 HUD 아래 왼쪽 기본 자리에 계산 높이(6줄이면 790~810)의 창이 다 들어간다. 1280×720에서는 최대 1.3배 맞춤 규칙으로 줄어든 크기를 캡처로 확인한다(메인 msg_599b28e18b73 3항). 6줄까지 줄 수 인자로 볼지는 RED 계약에서 정한다(메인 msg_e1d856276e79 2항).
3. 재화 주머니 칸 우클릭 한 번은 `C_ItemUse`를 정확히 1개 보낸다(itemId CoinPouch, 현재 revision). 결과를 기다리는 동안 다시 우클릭하면 추가 송신은 0이다. 재료 칸·빈 칸 우클릭의 송신은 0이다.
4. 창 위 우클릭·좌클릭은 `C_Attack` 0, 공격 예측 쿨다운 0, 공격 잠금 0이다(기존 #2/R8 보존). 닫힌 창 자리, 일시정지, 씬 전환 중, Ready 전, 다른 창이 덮은 칸의 우클릭은 `C_ItemUse` 0이다.
5. 기존 동작을 보존한다: 「I」 열고 닫기, 닫힌 창 raycast 0, 잡는 칸 끌기와 자리 기억, 열림 연출, 재화 줄과 결과 줄, 칸 강조, 새로고침 버튼. 기존 단정을 바꿀 때는 줄 단위 분류 표, 같은 명령의 전후 원시, 요구 출처(위 92·93 원문)를 갖춘다. 몸짓(버튼 클릭 → 우클릭)과 칸 내용 말고 동작·수명·문구 단정은 그대로 둔다.
6. 서버·공유·PDL·입력 설정·Content 소유 파일의 diff는 0이다.
7. 기준선 대비 새 실패는 0이다. EditMode 전체와 PlayMode 전체를 돈다. main에는 PR286이 있어 기준 PlayMode의 실패 기대값은 0이다(앞 goal 근거 폴더 `pr4-playmode-failures-vs-pr286-groups-1031-02.txt`, 실서버 2건은 Explicit). 기준값은 W1에서 ffe7f983으로 다시 잰다.
8. 사용자 확인 캡처(통과 근거 아님): 1920×1080·1280×720 각각에 격자(아이템 둘과 빈 칸), 재화 주머니 상세, 재료 상세가 있다. 6줄 캡처는 Core 24칸 PR 병합 뒤 결과 기록 PR에 넣는다(메인 msg_e1d856276e79 2항).
9. 독립 검증 PASS(강 등급)다. 실제 경로 시험은 「I」로 열기 → 실제 마우스로 올리기 → 우클릭 → `C_ItemUse`까지 본다.
10. client.md :50·:54 문장이 새 동작과 맞다.

## Core 24칸 계약 — 확정

원천: Core 제안 `msg_5254b764b105`(2026-10-31T18:09:33Z), Art 답 `msg_bacd33a804f4`(18:12:05Z), Core 동의 `msg_6d95ee3a3f84`(18:15:55Z), Core 참고 `msg_11d7daa692d2`(18:22:42Z), 6줄 캡처 시점 메인 결정 2 A `msg_e1d856276e79`. 원문은 앞 goal 근거 폴더 `lead-wait-475-messages.txt`·`lead-wait-476-messages.txt`·`lead-wait-477-messages.txt`·`lead-wait-478-messages.txt`와 `closeout-01/reply-core-24-slot-contract-1101-01.md`(Art 답 본문)에 있다.

- Core PR 하나가 하는 것(Core 제안): 36번 `S_InventorySnapshot` 끝에 slot8~23을 붙이고 ProtocolVersion을 23으로 올린다. `InventoryLimits.MaxSlots`를 8에서 24로 바꾸고, 서버 snapshot과 클라 `InventorySnapshotHandler`를 24칸으로 맞춘다. Shared.dll을 다시 빌드해 Plugins에 복사하고, 클라 프로토콜 호환 EditMode 시험(`InventoryClientTestSupport`·`InventoryProtocolCompatibilityTests`·`InventoryClientContractTests`)도 Core가 고친다.
- Art 격자 PR이 쓰지 않는 파일: `InventorySnapshotHandler`·`InventorySnapshotState`. Art 답이 「Art 격자 PR은 두 파일을 쓰지 않는다」·「이번 24칸 변경에서 Core가 두 파일의 주인을 맡는 데 동의한다」고 했고, Core가 동의했다.
- 클라 상태의 공개 모양: Core PR은 인벤토리 창이 읽는 클라 상태의 공개 모양을 바꾸지 않는다. 칸 목록 멤버의 이름·형식은 그대로이고 길이만 MaxSlots(24)를 따른다. 바꿔야 하면 Core가 Art에 먼저 알린다(Core 동의 1항).
- Art 격자 PR이 맡는 것: 화면 코드와 화면 시험의 「8칸」 가정을 MaxSlots 계산으로 바꾼다. 대상 줄은 위 「건드릴 곳」의 Core I24-A01~03 행과 8칸 숫자 행이다. `BrightUiKitImportTests`는 같은 표의 행대로 기준 실행으로만 본다. Core PR은 이 파일들을 고치지 않는다.
- client.md 줄(새 리드 관측): Core 로컬 branch `feat/inventory-24-slots-20261101`(`7eb3edac`)이 client.md :38(받는 쪽)과 :50(InventoryPanelView 문장의 「8슬롯」 → 「공유 칸 수(24)의 슬롯」)을 고친다(근거 폴더 `overlap-client-md-hunks-1101-01.txt`). 격자 PR도 :50을 다시 쓰므로, 격자가 먼저 들어가면 Core PR rebase 때 이 줄이 겹친다. 메인 결정 B-2 A(`msg_b79ad103b86e`): 격자 PR은 client.md :50의 칸 수를 숫자 없이 「공유 칸 수(InventoryLimits.MaxSlots)」로 적는다. Core는 rebase 때 :50 변경을 버리고 :38만 남긴다. Core에는 메인이 그 메시지 사본으로 알린다.
- 병합 순서: Art 격자 PR이 먼저, Core 24칸 PR이 다음이다. Core PR이 먼저 준비되면 병합 준비 상태로 기다린다(Core 제안 2항). Art 격자 PR의 Unity 시험은 그때의 8칸에서 돈다. Core PR의 Unity 확인은 Art 격자가 들어간 main 위에서 24칸으로 돈다(Core 제안 4항).
- 6줄 창과 캡처: 6줄 창은 Core PR의 Unity 확인에서 처음 생긴다. 6줄 사용자 확인 캡처는 Core 24칸 PR 병합 뒤 main에서 찍어 이 goal의 결과 기록 PR에 넣는다(메인 결정 2 A). 1920×1080 「화면 안」 단정을 줄 수 인자로 6줄까지 볼지는 RED 계약에서 정한다(같은 결정).
- 6줄에서 깨지는 시험: 고치기 전에 귀속을 나눈다. Art 소유는 Art 리드에게, Content 소유(보상 창 겹침 시험 등)는 Content 리드와 메인에게 먼저 알린다(Core 동의 2항). `InventoryServerIntegrationTests`의 주인은 24칸 서버에서 칸 수 가정이 깨질 때 Core·Art·메인이 정한다(같은 항).
- Core goal 원문: `core-active/01_Phases/goals/2026-11-01-inventory-24-slots/goal.md`(Core PR 때 main에 들어간다). 새 Art 주소 `run:run_6687e0512b2c`는 Core 리드 Run `run:run_ee7c33fd5873`에 알렸다(`msg_d509ea07114e`, 2026-11-01T03:41:16Z).

## PR 경계

- PR1 격자·상세 창·우클릭: commit은 기록 → RED → 구현 → 캡처 순이다. 첫 기록 commit은 이 goal 파일·CURRENT의 Art 줄·BACKLOG 승격 칸이다. 강 등급 독립 검증을 받는다. 새 PlayMode 시험은 「마우스 클릭 = 공격」 가정에 기대지 않는다(앞 goal의 msg_9cbc405339aa 조건을 잇는다).
- PR2 결과 기록·6줄 캡처·Gardener: Core 24칸 PR 병합 뒤 main에서 6줄 캡처를 찍어 넣는다(메인 결정 2 A).
- 앞 goal의 #PR5T-01~03 처분: 메인 결정 B-1 A(`msg_b79ad103b86e`)로 PR1의 첫 기록 commit에 [밝은 외관 goal](../2026-10-29-inventory-character-select-look/goal.md#종료-뒤-보충--pr292-문서-재실사-v3와-pr5t-처분)의 「종료 뒤 보충」 절로 넣었다. 원천은 R-8 노트 3판의 마지막 두 절과 메인 `msg_b8eeb4d0a16a`·`msg_5d7952f9d461`이다. 이 절은 앞 PR292 판정 대상 밖이고 PR1 독립 검증 범위에 들어간다. 이유는 PR2가 Core 24칸 병합까지 늦고, 따로 PR을 열면 실사와 병합 승인이 한 번 더 들기 때문이다.
- branch는 앞 goal의 PR4·PR5가 main에 들어간 뒤 최신 main(`ffe7f983`)에서 만들었다. 열린 PR과 다른 파트 branch의 merge-tree 대조는 근거 폴더 `merge-tree-open-work-1101-01.txt`에 있다.
  - PR268(`23cc2713`): ffe7f983과 충돌 0이다. 격자 대상 중 client.md를 함께 고치지만 :30 근처 두 줄 추가라 :50·:54와 다른 곳이다. 메인 참고(`msg_b79ad103b86e`): PR268 head는 지금 `3e46ea65`다(Content goal.md 한 파일 commit 추가). PR1 병합 전에 그 head로 merge-tree를 다시 잰다.
  - PR207(`02fcd8a4`): CURRENT.md 하나로 충돌이 난다. 앞 goal 때도 같았고 메인은 범위 밖으로 둔다(`msg_b8eeb4d0a16a`).
  - Content 캐릭터 선택 로컬 branch(`fb3d65b1`)는 이미 main 안에 있다(merge-base = head). 공유 시험 파일(`UiWindowAppearanceContractTests`·`UiWindowCompositionRenderTests`) 겹침은 지금 0이다. Content의 커밋 안 된 작업본은 보지 못했다.
  - Core 24칸 로컬 branch(`7eb3edac`): ffe7f983과 충돌 0이다. client.md :50은 위 Core 계약의 client.md 줄대로 다룬다.
- 병합 순서와 6줄 캡처 시점은 위 「Core 24칸 계약 — 확정」을 따른다.

## 작업 방식

- 리드는 Claude Opus(`[Art 리드 Opus]`)다.
- 배정은 저장소 `CLAUDE.md` 4A 절의 11-01 표, 사용자 결정 102 A·101 A(메인 msg_da3be91ab030), 메인 msg_d75240eadc75를 따른다.
  - 선행 시험 작성자: 새 `codex --model gpt-6-astra -c model_reasoning_effort=xhigh --disable fast_mode`, 태그 `[Art 시험 Astra]`.
  - 구현자: 새 `codex --model gpt-6.1-sol -c model_reasoning_effort=max --disable fast_mode`, 태그 `[Art Sol]`. 캡처 실행자도 작업자라 같은 배정이다. 같은 계약에서 Sol 구현이 두 번째 독립 불합격을 받으면 다음 시도는 새 gpt-6-astra xhigh `[Art 구현 Astra]`다.
  - 독립 검증자: 새 `codex --model gpt-6-astra -c model_reasoning_effort=xhigh --disable fast_mode`, 태그 `[Art 검증자]`.
  - 종료 Gardener: 새 `codex --model gpt-6.1-sol -c model_reasoning_effort=max --disable fast_mode`, 태그 `[Art Sol]`(101 A).
- R-5 첫 화면에 지정 모델이나 max가 없으면 바꿔 띄우지 않고 메인에 보고한다. 첫 화면에 업데이트 창·선택창이 뜨면 아무것도 누르지 않고 메인에 보고한다(메인 결정 `msg_ad79455eb3b9` O3).
- Unity는 한 번에 하나이고 창은 메인이 준다. 다른 파트 창과 겹치지 않게 「준비됨」 뒤 청한다. 창마다 저장 스크립트로 관문을 잰다. 계약마다 `unity-protection-rules-v1.md`를 원문 그대로 붙이고, SENTIS 한 줄은 정산 때 되돌린다.
- 값 기록 규칙(msg_dbc8a605e5c1)을 그대로 적용한다. 시계와 SHA도 저장 스크립트 안에서 잰다. 명령 출력을 `/dev/null`로 버리지 않고, 인라인 셈 값을 계약·보고 값으로 쓰지 않는다(메인 `msg_4bef008e3fa9`).

## Unity 창 추정(추정이다, 측정 아님)

| 창 | 하는 일 | 추정 |
|---|---|---|
| W1 | 선행 시험: 기준 EditMode·PlayMode 전체와 RED 실패 확인 | 약 35분 |
| W2 | 구현 F: EditMode·PlayMode 전체 | 약 30분 |
| W3 | 캡처(Editor 창 모드, 사용자가 비울 때) | 약 20분 |
| W4 | 독립 검증: 음성 대조, 실제 경로, 전체 | 약 40분 |
| W5 | Core 24칸 PR 병합 뒤 6줄 캡처 | 약 20분 |

- W1~W4 합계 약 2시간 5분이다. 고침이 한 번 생길 때마다 약 30분을 더한다.
- 근거: 앞 goal PR4의 검증 창은 40분(msg_e9cfec7f9f2b), K2 캡처 창은 20분(msg_e39367a0d2ac)이었다. 70 A 구현 F의 PlayMode 전체는 336초였다(PR286 goal 원문). W5는 W3와 같은 크기로 본 추정이다.

## 진척 단계

- [x] 범위 초안 승인
- [>] 선행 시험 RED
- [ ] 구현과 전체 시험
- [ ] 사용자 확인 캡처
- [ ] 강 등급 독립 검증
- [ ] 격자 PR 병합
- [ ] 24칸 뒤 6줄 캡처
- [ ] 결과 기록 PR 병합
- [ ] 종료 Gardener

## 다음 계획 후보

- 75 「버튼 조각을 진하게」는 BACKLOG `bright-button-piece-contrast`다(리셋 뒤).

## 단계 결과와 근거

### 범위 초안과 승인 — 2026-11-01

- 출발점은 앞 리드가 쓴 초안 v6 `.backups/verification/2026-10-29-inventory-character-select-look/inventory-icon-grid-scope-draft-v6.md`(SHA256 `c96517e1138f4f17f3cfd373ef6c81f941eadc5d05fa68e71cc40cca789e98be`)다. v1~v5 승인 이력은 v6 머리의 상태 줄에 있다. 메인 R-8 진입 지시는 `msg_4bef008e3fa9`(2026-11-01T03:37:09Z)다.
- 새 리드의 진입: 맥락 메모 `art-lead-context-1101-01.md`, branch 전환 `entry-switch-1101-01.txt`, 미추적 5개 전후 같음(`entry-status-before-switch-1101-01.txt`·`entry-status-after-switch-1101-01.txt`), Run `run_6687e0512b2c`(`entry-run-create-1101-01.json`), 메인 READY `msg_71631483ada4`(03:40:59Z).
- 범위 초안 제출 `msg_eff826366fbc`(03:49:43Z, 본문 `send-main-scope-draft-1101-01-body.md`). v6과의 줄 단위 차이 표(A)와 새 리드의 확인 결과(B)가 그 본문에 있다. 제출 때 goal.md sha256은 `525fc31119d6f2981320e76209d702750a7e6de3d151071b61986ed322b07899`다(`measure-goal-draft-1101-01.txt`).
- 메인 승인 `msg_b79ad103b86e`(03:50:26Z, 메인 결정, 저장 `lead-wait-1101-01-messages.txt`). 원문 요지: 「승인이다」. 대상은 위 sha256이고 v6을 대신한다. A표의 차이는 줄 번호 이동·기록 위치 이동·이미 내린 결정 문장의 옮겨 적기뿐이라 범위 뜻이 바뀌지 않았고, 사용자 판단 대상이 아니다. B-1 A(#PR5T 처분을 앞 goal 「종료 뒤 보충」 절로), B-2 A(client.md :50의 칸 수를 공유 칸 수로)다.
- 승인 뒤 이 파일에서 고친 곳은 상태 줄뿐이다. 재개 카드, B-1·B-2를 「제안」에서 결정으로 바꾼 두 줄, PR268 head 참고 한 줄, 진척 단계, 이 절이다. 범위 네 항목과 완료조건은 승인본 그대로다.
