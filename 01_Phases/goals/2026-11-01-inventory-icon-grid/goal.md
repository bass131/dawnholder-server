# 인벤토리 아이콘 격자

## 재개 지점

### 현재 재개 카드 — 2026-11-01 좁은 재실사 NOT PASS(#GRIDR-01)와 고침 commit 뒤, 두 번째 좁은 재실사 전

**다음 첫 일:** 고침 commit에서 바뀐 줄만 보는 두 번째 좁은 재실사 계약을 써서 메인에 보고한 뒤 새 `[Art 검증자]`를 띄운다(아래 「작업 방식」). 대상은 아래 「좁은 재실사 — 2026-11-01」의 「두 번째 좁은 재실사로 넘길 것」이다. PASS 뒤 그때의 main으로 merge-tree를 다시 재고(메인 `msg_31e4dfa1781f`) 메인에 병합 승인 요청을 올린다. 병합은 사용자가 「병합 승인: PR<번호> head <40자>」를 제출한 뒤 메인이 한다. PR1 병합 뒤에는 최신 main에서 PR2 branch를 만든다. 첫 commit은 PR2 goal 범위 patch다. 2판(근거 폴더 `pr2-goal-scope-02.patch`, 메인 보고 `msg_80b6ca150a0b`)은 이 결과 기록 앞의 goal 기준이라, 그때의 goal에 맞춰 다시 만든다(메인 `msg_f1139600734d`). 병합 순서는 Art PR1 → Core 24칸 → Art PR2 → Content 녹화 빌드다(메인 `msg_1357f45b9d35`). 작업자가 일하는 동안 리드는 이 파일을 고치지 않는다(아래 「선행 시험 RED — 2026-11-01」 첫 발생 1). 기록은 근거 폴더의 리드 맥락 메모 2(`art-lead-context-1101-02.md`)에 쌓는다. 메모 1은 검증 계약에 고정됐고 끝 정정 절만 더했다. 리드 시험 계약에는 「몸짓을 바꾸는 시험 계약은 같은 몸짓의 기존 선례(설정·기다림)를 파일:줄로 가리킨다.」를 넣는다(메인 `msg_484ea2fa03bb`, 규칙 정본 반영 전까지).

| 항목 | 현재 |
|---|---|
| 상태 | 범위는 메인이 승인했다(`msg_b79ad103b86e`). 선행 시험 RED commit `219f7cdb`, main 받기 `9e883317` 뒤 구현 v1을 commit했다(`4eddf759`, 아래 「구현 v1 — 2026-11-01」). F PlayMode 실패 1건은 시험 결함이었고, 시험 고침 commit `7d9d4ff5` 뒤 PlayMode 전체는 실패 0이다(아래 「시험 고침 v1 — 2026-11-01」). 사용자 확인 캡처 세 실행이 모두 통과했고 상세 창 캡처 case 둘을 commit했다(`5aeb415b`, 아래 「사용자 확인 캡처 v1 — 2026-11-01」). 사용자 그림 확인은 112 「OK」다. 강 등급 독립 검증은 head `47cbdcac`에서 PASS다(아래 「독립 검증 — 2026-11-01」). 결과 기록 commit과 독립 시험 채택 commit 뒤 좁은 재실사는 head `52fa3286`에서 NOT PASS다. 기록 결함 #GRIDR-01 하나이고 제품 결함은 0이다(아래 「좁은 재실사 — 2026-11-01」). 그 줄을 고친 commit 뒤 두 번째 좁은 재실사 전이다. |
| 리드 | `[Art 리드 Opus]`(claude-opus-5-5 xhigh), 대화 ID `c92c0adb-250c-48fd-b4cc-8d6c85df4386`. 회신 주소는 `run:run_6687e0512b2c`다. |
| branch | `feat/art-inventory-icon-grid-20261101`이다. origin/main `ffe7f983b96eda8002a27169b3e8b7cb5eb32c72`(PR292 병합)에서 만들었다. RED commit 뒤 origin/main `3bb76772`(PR268 병합)을 받았다. 받기 commit은 `9e88331743064b390f6ab1d238dc173825a4bd23`이고 충돌은 0이다(근거 폴더 `main-merge-1101-01.txt`). |
| 관계 | Core 인벤토리 24칸 goal(`core-active/01_Phases/goals/2026-11-01-inventory-24-slots/goal.md`, Core PR 때 main에 들어간다)이 패킷·서버·공유 칸 수를 바꾼다. 병합 순서는 이 goal의 격자 PR이 먼저다(아래 「Core 24칸 계약 — 확정」). |
| 실행 창 | W1 Unity 창 세 번을 썼다. U0 04:30~05:00Z, U1 04:56~05:20Z, U1 03 05:18~05:40Z이다. 구현 v1은 05:57~06:45Z 창을 받아 06:18Z에 일찍 반납했다. 시험 고침은 06:48~07:10Z 창을 받아 06:57Z에 반납했다. 사용자 확인 캡처는 Editor 창 07:39~08:05Z를 사용자 부재로 받아 07:48Z에 반납했다. 독립 검증은 08:47~09:37Z 창을 받아 09:21Z에 반납했다. 모두 반납했다. Unity·WSL·DB 창은 메인이 배정한다. |
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

### 11-01 사용자 결정 111 A — 확인 캡처 동안 PC 비우기 — msg_163fec4ff6c2

- 사용자 결정 원문(메인 pane 제출, 메인 `msg_163fec4ff6c2` 전달): 「대시보드 결정 응답: 1) 111 - 사용자 할 일: 인벤토리 격자 확인 캡처 동안 약 25분 PC를 비우기(마우스·키보드 손대지 않기) → A 지금 비움」
- 메인 적용(같은 메시지): Editor 창은 2026-11-01T07:39:00Z~08:05:00Z다. 사용자가 일찍 돌아오면 메인이 「멈춤」을 보낸다.

## 지금 상태(사실)

기준은 origin/main `ffe7f983b96eda8002a27169b3e8b7cb5eb32c72`다. F1·F2·F3은 근거 폴더의 저장 출력이다.

- F1: `inventory-grid-scope-facts-1101-01.txt`(스크립트 `inventory-grid-scope-facts-1101-01.sh`, 1031판 사본)
- F2: `inventory-grid-scope-facts-1101-02.txt`(스크립트 `inventory-grid-scope-facts-1101-02.sh`, 1031판 사본)
- F3: `inventory-grid-scope-facts-1101-03.txt`(스크립트 `inventory-grid-scope-facts-1101-03.sh`, Core가 짚은 시험과 8칸 가정)
- 1031판 출력과의 차이: `facts-diff-1031-vs-1101-01.txt`(인라인 명령으로 만든 기록이었다. 저장 스크립트 재측정 `provenance-remeasure-02/out/facts-diff-1031-vs-1101.txt`가 시계 줄 밖 본문이 같음을 보인다. 정정 기록 `provenance-remeasure-02/correction-record.md`). 코드 줄 번호는 같다. client.md 인용 줄은 10줄씩 밀렸고, probe를 쓰는 시험 파일이 둘 늘었다.

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
| 8칸을 숫자로 박은 Art 시험 줄 | `InventoryPresentationTests` :477~:478(빈 칸 순회 `slot <= 8`, Core 참고 `msg_11d7daa692d2`), `InventoryPanelViewAppearanceTests` :57·:117·:127·:179, `UiWindowAppearanceContractTests` :257·:583·:1051, `UiWindowCompositionRenderTests` :380·:428, `BrightWindowInteractionTests` :114·:125, `InventorySceneLifecycleTests` :263(메시지), `InventoryWindowPlacementTests` :136~:154(F3). 만들 것 1대로 MaxSlots에서 기대값을 계산한다. 정정: 초안의 `UiWindowLiveCaptureTests` :571은 3D 경계 상자 모서리 8개 순회라 인벤토리 칸 줄이 아니고, `BrightWindowInteractionTests` 두 줄은 초안 목록에 없었다(근거 폴더 `grid-red-facts-1101-03.txt`). 전체 줄 목록은 선행 시험 계약의 「바꾸는 기존 단정」 표다. | Art |
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
- client.md 줄(새 리드 관측): Core 로컬 branch `feat/inventory-24-slots-20261101`(`7eb3edac`)이 client.md :38(받는 쪽)과 :50(InventoryPanelView 문장의 「8슬롯」 → 「공유 칸 수(24)의 슬롯」)을 고친다(근거 폴더 `overlap-client-md-hunks-1101-01.txt`. 인라인 명령으로 만든 기록이었다. 저장 스크립트 재측정 `provenance-remeasure-01/overlap-client-md-hunks.txt`가 시계 줄 밖 본문이 같음을 보인다. 정정 기록 `provenance-remeasure-01/correction-record.md`). 격자 PR도 :50을 다시 쓰므로, 격자가 먼저 들어가면 Core PR rebase 때 이 줄이 겹친다. 메인 결정 B-2 A(`msg_b79ad103b86e`): 격자 PR은 client.md :50의 칸 수를 숫자 없이 「공유 칸 수(InventoryLimits.MaxSlots)」로 적는다. Core는 rebase 때 :50 변경을 버리고 :38만 남긴다. Core에는 메인이 그 메시지 사본으로 알린다.
- 병합 순서: Art 격자 PR이 먼저, Core 24칸 PR이 다음이다. Core PR이 먼저 준비되면 병합 준비 상태로 기다린다(Core 제안 2항). Art 격자 PR의 Unity 시험은 그때의 8칸에서 돈다. Core PR의 Unity 확인은 Art 격자가 들어간 main 위에서 24칸으로 돈다(Core 제안 4항).
- 6줄 창과 캡처: 6줄 창은 Core PR의 Unity 확인에서 처음 생긴다. 6줄 사용자 확인 캡처는 Core 24칸 PR 병합 뒤 main에서 찍어 이 goal의 결과 기록 PR에 넣는다(메인 결정 2 A). 1920×1080 「화면 안」 단정을 줄 수 인자로 6줄까지 볼지는 RED 계약에서 정한다(같은 결정).
- 6줄에서 깨지는 시험: 고치기 전에 귀속을 나눈다. Art 소유는 Art 리드에게, Content 소유(보상 창 겹침 시험 등)는 Content 리드와 메인에게 먼저 알린다(Core 동의 2항). `InventoryServerIntegrationTests`의 주인은 24칸 서버에서 칸 수 가정이 깨질 때 Core·Art·메인이 정한다(같은 항).
- Core goal 원문: `core-active/01_Phases/goals/2026-11-01-inventory-24-slots/goal.md`(Core PR 때 main에 들어간다). 새 Art 주소 `run:run_6687e0512b2c`는 Core 리드 Run `run:run_ee7c33fd5873`에 알렸다(`msg_d509ea07114e`, 2026-11-01T03:41:16Z).

## PR 경계

- PR1 격자·상세 창·우클릭: commit은 기록 → RED → 구현 → 캡처 순이다. 첫 기록 commit은 이 goal 파일·CURRENT의 Art 줄·BACKLOG 승격 칸이다. 강 등급 독립 검증을 받는다. 새 PlayMode 시험은 「마우스 클릭 = 공격」 가정에 기대지 않는다(앞 goal의 msg_9cbc405339aa 조건을 잇는다).
- PR2 결과 기록·6줄 캡처·Gardener: Core 24칸 PR 병합 뒤 main에서 6줄 캡처를 찍어 넣는다(메인 결정 2 A). 그 캡처 계약에서 격자 case가 창 열림 효과가 끝날 때까지 기다리게 고친다(아래 「사용자 확인 캡처 v1 — 2026-11-01」의 첫 발생).
- 앞 goal의 #PR5T-01~03 처분: 메인 결정 B-1 A(`msg_b79ad103b86e`)로 PR1의 첫 기록 commit에 [밝은 외관 goal](../2026-10-29-inventory-character-select-look/goal.md#종료-뒤-보충--pr292-문서-재실사-v3와-pr5t-처분)의 「종료 뒤 보충」 절로 넣었다. 원천은 R-8 노트 3판의 마지막 두 절과 메인 `msg_b8eeb4d0a16a`·`msg_5d7952f9d461`이다. 이 절은 앞 PR292 판정 대상 밖이고 PR1 독립 검증 범위에 들어간다. 이유는 PR2가 Core 24칸 병합까지 늦고, 따로 PR을 열면 실사와 병합 승인이 한 번 더 들기 때문이다.
- branch는 앞 goal의 PR4·PR5가 main에 들어간 뒤 최신 main(`ffe7f983`)에서 만들었다. 열린 PR과 다른 파트 branch의 merge-tree 대조는 근거 폴더 `merge-tree-open-work-1101-01.txt`에 있다.
  - PR268(`23cc2713`): ffe7f983과 충돌 0이다. 격자 대상 중 client.md를 함께 고치지만 :30 근처 두 줄 추가라 :50·:54와 다른 곳이다. 메인 참고(`msg_b79ad103b86e`): PR268 head는 지금 `3e46ea65`다(Content goal.md 한 파일 commit 추가). PR1 병합 전에 그 head로 merge-tree를 다시 잰다. 이어서 PR268은 main에 병합됐다(merge commit `3bb76772`, 메인 사본 `msg_31e4dfa1781f`). 그 main과 이 branch의 merge-tree 충돌은 0이다(근거 폴더 `merge-tree-after-pr268-1101-01.txt`). client.md의 인벤토리 문장은 두 줄 밀려 InventoryPanel이 :50, InventoryPanelView가 :52다. 구현 계약은 main을 받은 뒤의 줄 번호로 쓴다. PR1 병합 전에 그때의 main으로 한 번 더 잰다. RED commit 뒤 이 main을 branch에 받았다(`9e883317`, 아래 「선행 시험 RED — 2026-11-01」의 main 받기).
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

## 남은 위험

- 상세 창의 아래쪽 넘침, 그리고 마지막 줄·마지막 열의 채운 칸은 지금 시험하지 않는다. 클라 snapshot 검증(`InventorySnapshotState.IsValidSnapshot` :25~:46)은 빈틈 없음·ID 오름차순·중복 없음만 받는다. 아이템이 2종이라 채울 수 있는 칸은 칸 1·2뿐이다. 그래서 그 상태는 제품이 만들 수 없다. 시험은 칸 1·2와 실제 끌기로 오른쪽 끝 넘침만 본다(`msg_6b2af69945fc`). 구현 독립 검증 계약에 「상세 창 자리 계산이 방향과 상관없이 화면 안으로 맞추는지 코드 실사」를 넣는다(메인 `msg_8a35900d5374`). 아이템 종류가 늘면 다시 본다.
- 맵 이동 시험의 Ready 전 우클릭은 「사용 0건」만 본다. 그 클릭이 칸에 닿았는지 보여 주는 제품 공개 신호는 없다. Ready 전에는 패널이 raycast를 끄고, 상세 창과 칸 강조도 칸을 찾기 전에 멈추기 때문이다(`InventoryPanel.cs` :92~:96, `InventoryPanelView.cs` :203~:209, `InventorySlotHoverHighlight.cs` :31~:41, 근거 폴더 `lead-ready-signal-lines-1101-01.txt`). 그래서 시험은 패널이 열렸다는 것(alpha 1)과 패널이 멈춘 뒤의 좌표만 보장한다. private 상태를 reflection으로 읽어 막지는 않는다(메인 `msg_28eea903507a`).

## 진척 단계

- [x] 범위 초안 승인
- [x] 선행 시험 RED
- [x] 구현과 전체 시험
- [x] 사용자 확인 캡처(촬영. 사용자 그림 확인 112 「OK」)
- [x] 강 등급 독립 검증(PASS)
- [>] 결과 기록 commit 뒤 좁은 재실사(첫 번째 NOT PASS #GRIDR-01, 고침 commit 뒤 두 번째 전)
- [ ] 격자 PR 병합
- [ ] 24칸 뒤 6줄 캡처
- [ ] 결과 기록 PR 병합
- [ ] 종료 Gardener

## 다음 계획 후보

- 75 「버튼 조각을 진하게」는 BACKLOG `bright-button-piece-contrast`다(리셋 뒤).

## 단계 결과와 근거

### 범위 초안과 승인 — 2026-11-01

- 출발점은 앞 리드가 쓴 초안 v6 `.backups/verification/2026-10-29-inventory-character-select-look/inventory-icon-grid-scope-draft-v6.md`(SHA256 `c96517e1138f4f17f3cfd373ef6c81f941eadc5d05fa68e71cc40cca789e98be`)다. v1~v5 승인 이력은 v6 머리의 상태 줄에 있다. 메인 R-8 진입 지시는 `msg_4bef008e3fa9`(2026-11-01T03:37:09Z)다.
- 새 리드의 진입: 맥락 메모 `art-lead-context-1101-01.md`, branch 전환 `entry-switch-1101-01.txt`, 미추적 5개 전후 같음(`entry-status-before-switch-1101-01.txt`·`entry-status-after-switch-1101-01.txt`. 전환 기록과 이 둘은 인라인 명령의 원시 출력이라 재측정할 수 없다. 교차 근거는 branch reflog 「03:39:38 Created from origin/main」이다: `provenance-remeasure-03/out/summary.txt`. 정정 기록 `provenance-remeasure-03/correction-record.md`), Run `run_6687e0512b2c`(`entry-run-create-1101-01.json`), 메인 READY `msg_71631483ada4`(03:40:59Z).
- 범위 초안 제출 `msg_eff826366fbc`(03:49:43Z, 본문 `send-main-scope-draft-1101-01-body.md`). v6과의 줄 단위 차이 표(A)와 새 리드의 확인 결과(B)가 그 본문에 있다. 제출 때 goal.md sha256은 `525fc31119d6f2981320e76209d702750a7e6de3d151071b61986ed322b07899`다(`measure-goal-draft-1101-01.txt`).
- 메인 승인 `msg_b79ad103b86e`(03:50:26Z, 메인 결정, 저장 `lead-wait-1101-01-messages.txt`). 원문 요지: 「승인이다」. 대상은 위 sha256이고 v6을 대신한다. A표의 차이는 줄 번호 이동·기록 위치 이동·이미 내린 결정 문장의 옮겨 적기뿐이라 범위 뜻이 바뀌지 않았고, 사용자 판단 대상이 아니다. B-1 A(#PR5T 처분을 앞 goal 「종료 뒤 보충」 절로), B-2 A(client.md :50의 칸 수를 공유 칸 수로)다.
- 승인 뒤 이 파일에서 고친 곳은 상태 줄뿐이다. 재개 카드, B-1·B-2를 「제안」에서 결정으로 바꾼 두 줄, PR268 head 참고 한 줄, 진척 단계, 이 절이다. 범위 네 항목과 완료조건은 승인본 그대로다.

### 선행 시험 계약 — 2026-11-01

- 계약 v1 전문은 근거 폴더 `grid-red-contract-v1.md`(sha256 `da7acc6be3b20e9b450701c5bb6f409fe79accc4b1f54ae593c54ef564ac385c`, 조립 receipt `grid-red-contract-v1.receipt.txt`)다. 기준 commit은 `e4b296af`다.
- 리드 설계는 계약 「리드가 정한 설계」 절과 리드 맥락 메모 「계획 변경 3」에 있다. 메인 `msg_e1d856276e79` 2항이 RED 계약에 맡긴 「1920×1080 화면 안을 줄 수 인자로 6줄까지 볼지」는 쓰지 않기로 정했다. 시험이 MaxSlots에서 기대값을 고르므로 Core PR의 24칸 Unity 실행이 같은 시험으로 6줄을 보고, 실화면 6줄은 W5 캡처가 본다.
- 작성자 `[Art 시험 Astra]`: handle `term_41ef81bd-d5f7-4575-ae65-35c33c8144ce`, Task `task_57518987f34b`, rollout `01a19554-d6df-7212-bf8f-57b3808a4ec5`(model gpt-6-astra, effort xhigh, service_tier default). 근거는 `grid-red-launch-*` 출력이다. 메인 보고는 `msg_b65f9f51f0a0`다.
- 시험 주인 차이: `InventoryPresentationTests`·`InventoryPanelToggleTests`·`HudGoldAndPanelToggleSceneTests`는 다른 goal이 처음 만들었다(`test-owner-facts-1101-01~03.txt`. 인라인 명령으로 만든 기록이었다. 재측정 `provenance-remeasure-01/test-owner-facts-01~03.txt`, HEAD는 원 기록 시각의 `e4b296af`로 고정. 정정 기록 `provenance-remeasure-01/correction-record.md`). 위 「건드릴 곳」대로 Art가 고치고, 고칠 줄은 Content 리드에게 미리 알렸다(`msg_f0690a77365d`).
- 계약 인용 정정(#GRIDV-01): 계약은 고정 입력이라 계약 파일은 고치지 않는다. 줄 번호는 `pr1-result-contract-lines-1101-01.txt`다.
  - 계약 :32·:249·:436~:438이 가리키는 `test-owner-facts-1101-01~03.txt`는 인라인 명령 출처였다. 정정은 위 시험 주인 줄의 괄호와 같다.
  - 계약 :771·:877은 goal 절 사본이다. 가리키는 `facts-diff-1031-vs-1101-01.txt`·`overlap-client-md-hunks-1101-01.txt`도 인라인 명령 출처였다. 정정은 「지금 상태(사실)」의 1031판 차이 줄과 「Core 24칸 계약 — 확정」의 client.md 줄 괄호와 같다.

### 선행 시험 RED — 2026-11-01

- 작성자 worker_done은 `msg_a6cf8d2fbdc8`(05:21:38Z, 성공)이다. 보고는 근거 폴더 `grid-red-01/report.md`(sha256 `a39aace699a81d53e4a88a3828bf15492cff3fa994929eec080d4be69e7d5c6e`)다. 작성자 pane은 대기 확인 뒤 닫았다(`close-writer-1101-01.json`).
- 결과는 두 단계로 냈다. 통과/실패/건너뜀은 작성자 summary에서 뽑았다.

| 단계 | EditMode | PlayMode |
|---|---|---|
| U0(기준 commit `e4b296af`) | 1725 / 0 / 0 | 217 / 0 / 46 |
| U1(최종: EditMode 03, PlayMode 02) | 1722 / 13 / 0 | 209 / 15 / 46 |

- 리드 원천 대조(`lead-r2-u0-vs-u1-changes-01.txt`. 인라인 명령으로 만든 기록이었다. 재측정 `provenance-remeasure-01/lead-r2-u0-vs-u1-changes.txt`, 파일 SHA256 같음. 정정 기록 `provenance-remeasure-01/correction-record.md`)
  - EditMode에서 사라진 이름은 4개다. 셋은 「eight」→「every」 이름 변경이고 새 이름으로 통과한다. 하나(`InventoryWindowPlacementTests.SlotContents_…StackedIconTextUse…`)는 계약 (다)가 허용한 삭제이고 T2가 대신한다.
  - 기존 시험의 통과→실패는 EditMode 3, PlayMode 8이다. 모두 칸 글→아이콘·수량 읽기 또는 사용 몸짓→우클릭으로 바꾼 시험이다.
  - 새 시험은 EditMode 11 case 중 1개가 통과(칸 수)하고 PlayMode 7개가 모두 실패한다. 실패 28건은 작성자가 모두 (a)로 분류했다(승인된 새 관찰과 옛 구현의 차이).
  - 보존 대상(계약 T8)의 결과 차이는 0이다. 제품·공유·서버 파일의 diff도 0이다.
- U1의 세 시도
  - 01: `Assert.Multiple`(Unity NUnit에 없음) 컴파일 오류로 시험 0건이다. PlayMode 01은 돌리지 않았다.
  - 02: 컴파일만 고쳐 다시 돌렸다(리드 `msg_020ff803ceef`, 메인 동의 `msg_51bde59fa91c`).
  - 03: EditMode만 다시 돌렸다(메인 허용 `msg_e17c8a43dd1c`). 우클릭 helper가 EditMode 버튼 누름 설정을 켜지 않아 생긴 fixture 실패 1건을 선례(`GameplayViewTestSupport.cs` :226~:275)대로 고친 뒤다. 그 1건만 통과로 바뀌었고 나머지 13건은 이름·첫 실패 줄이 02와 같다(`u1-03-vs-02-editmode-lead-01.txt`. 인라인 명령으로 만든 기록이었다. 재측정 `provenance-remeasure-01/u1-03-vs-02-editmode.txt`, 파일 SHA256 같음. 정정 기록 `provenance-remeasure-01/correction-record.md`).
  - 같은 시험 산출물의 고침은 두 번이다. 한 번 더면 메인 체크포인트다.
- 계약 보충(작성자 질의에 리드가 답함, 계약 원문은 그대로): 줄 경계 `msg_1dc06a7fef3b`·`msg_0b5485c8c5f1`·`msg_60b87f37ef68`, snapshot 상태 `msg_6b2af69945fc`, U1 02 `msg_020ff803ceef`, helper `msg_192c78b82e98`이다. 요점은 사용 버튼 단정을 우클릭·Refresh·상세 창 표시로 옮겨 단정 기준을 낮추지 않는 것이다. 지운 단정은 대신할 단정이나 「대상 소멸」 이유와 함께 작성자의 줄 단위 표(`grid-red-01/line-table-final-03.md`)에 있다.
- 첫 발생 기록(교정 층, 두 번째부터 반복 규칙)
  1. 리드가 계약 발행 뒤 작업 트리 goal.md를 고쳐 작성자 입력 관문을 깼다. 고침을 patch로 보관하고 되돌렸다가 RED 뒤 다시 얹었다(`park-goal-edits-1101-01.txt`, `settle-red-tree-1101-01.txt`).
  2. 리드 계약의 T2·T5·T6 채움 상태가 클라 snapshot 검증과 어긋났다. 작성자의 private 필드 reflection 주입 안은 거절했다(위 「남은 위험」).
  3. 작성자가 Unity NUnit 3.5에 없는 `Assert.Multiple`을 썼다. 리드는 계약 원문 「컴파일 오류는 고쳐 다시 돌리지 말고 보고」를 ask 답으로 한 번 넓혔다.
  4. 리드 계약 T4가 버튼 누름 선례(`GameplayViewTestSupport.VirtualKeyboard`)를 가리키지 않았다. 그래서 새 helper가 EditMode 누름 설정을 빠뜨렸다.
  - 구현 검증자 계약에는 「바꾼 시험의 API가 기준 commit 기존 사용과 맞는지」를 넣는다(메인 `msg_51bde59fa91c`).
- 기록 차이: report.md는 몇 곳에서 리드 답 대신 작성자 자신의 질문 msg ID를 「리드 msg」로 적었다. 예: 「리드 msg_804c2274a94c가 허용」의 실제 답은 `msg_020ff803ceef`다. 판정 내용은 이 절의 ID가 맞다.
- 정산: SENTIS 한 줄(diff sha256 `31e7aaf1…83e2`, 앞 goal과 같음)은 사본을 남기고 HEAD로 되돌렸다(`settle-red-tree-1101-01.txt`, `red-settle-sentis-diff-1101-01.txt`). 리드의 잔류 확인은 U0·U1 02·U1 03 뒤 모두 0이다(`proc-after-u0-1101-02.txt`·`proc-after-u1-1101-01.txt`·`proc-after-u1-03-1101-01.txt`).
- 구현 계약으로 넘길 것(report 「한계·구현 계약으로 넘길 것」): `InventorySlotPointer`는 입력과 연결만 맡고, 칸 찾기·상세 자리·가림 계산은 일반 타입이나 helper로 나눈다. 기존 강조 부품은 그대로 둔다. 가림 판정의 Canvas 탐색 비용, 밝은 바탕 대비 글자 대비, 방향별 상세 창 경계를 구현·검증에서 본다.
- RED commit은 `219f7cdb1c2bfc5b26a4ae741d27ff8b6f459a91`이고 push했다. 18파일이다: 시험 11파일 수정, 새 시험 3파일과 Unity가 만든 `.meta` 3개, 이 goal 파일. 미추적 5개는 그대로다(근거 폴더 `red-commit-1101-01.txt`). 메인 보고는 `msg_ff551fa53383`이다.
- main 받기: RED commit 뒤 origin/main `3bb76772`(PR268 병합)을 받았다(`9e883317`, `main-merge-1101-01.txt`). 들어온 것은 Demo 자동 진행기 코드·시험, `CharacterSelectController` 15줄, client.md 두 줄이다. Demo 코드 4파일에 인벤토리 참조는 없다. 새 Demo 시험이 들어와 전체 시험 수가 U0·U1과 달라진다. 그래서 구현 결과는 「main 받은 뒤 기준」 실행과 비교한다(구현 계약). client.md의 인벤토리 문장은 InventoryPanel :50, InventoryPanelView :52, 사용 :56이다.
- 리드 자기 신고: RED commit 뒤 보내기 명령 모양을 찾는 읽기 명령 하나에서 `2>/dev/null`을 썼다. 그 출력의 값은 어떤 기록에도 쓰지 않았다. 규칙은 이미 있다(메인 `msg_4bef008e3fa9`). 메인에 보고한다.

### 구현 v1 — 2026-11-01

- 계약은 근거 폴더 `grid-impl-contract-v1.md`(sha256 `5b2360560ab91a7dfe6722541ecbe445160e8b2d491c98f21f19d86cba9046a5`)다. 메인 보고는 `msg_d29db3a6e0fe`다. 리드가 RED 설계에 더 정한 것은 둘이다. 가림 판정은 TMP 글을 그린 글자 범위로 보고, 상세 창이 켜진 동안 매 프레임 다시 본다. 상세 창 자리는 네 방향을 같은 코드로 화면 안에 맞춘다.
- 구현자는 `[Art Sol]` gpt-6.1-sol max다. handle `term_2fd3ecb2-661a-4268-8465-a6e4ea12359b`, Task `task_7b979ecc38e5`, Dispatch `ctx_7dc19ffcfd36`이다. rollout으로 cwd·model·effort를 대조했다(`grid-impl-launch-rollout-check-01.txt`). worker_done은 `msg_a1a672b6cd71`(06:26:33Z, outcome failed)이고, 보고는 `grid-impl-01/report.md`(sha256 `0b8e090cdae266d367a196eead006e093c4e3b761b5e0e24b9789bd98378af16`)다. pane은 대기 확인 뒤 닫았다(`impl-pane-close-1101-01.json`).
- 쓴 것: `InventoryPanelView.cs`, `InventorySlotHoverHighlight.cs`(공용 helper로 바꾼 한 줄), 새 `InventorySlotPointer.cs`·`InventoryPointerGeometry.cs`·`InventoryOverlayOcclusion.cs`와 Unity가 만든 `.meta` 셋, client.md다. 시험·ProjectSettings 쓰기는 0이다(`lead-write-end-check-1101-01.txt`. 인라인 명령이었다. SHA256·줄 수 부분은 저장 스크립트 재측정 본문이 같다: `provenance-remeasure-03/out/summary.txt`. 「쓰기 0」을 받친 status 부분은 인라인 명령의 원시 출력이라 재측정할 수 없다. 교차 근거는 구현 commit `4eddf759`의 diff 9파일에 시험·ProjectSettings가 없다는 것이다: `provenance-remeasure-03/out/lead-write-end-check.diffstat-commit.txt`. 정정 기록 `provenance-remeasure-03/correction-record.md`).
- 결과는 아래 표다. 원시 결과 XML에서 리드 스크립트로 다시 뽑았다(`lead-r2-impl-v1-1101-01.txt`).

| 실행 | 통과 / 실패 / 건너뜀 |
|---|---|
| R(격자 시험 묶음만 거른 EditMode) | 116 / 0 / 0 |
| F EditMode 전체 | 1781 / 0 / 0 |
| F PlayMode 전체 | 224 / 1 / 46 |

- 리드 원천 대조(`lead-r2-impl-v1-1101-02.txt`, 기준은 RED U1 최종 실행)
  - EditMode: RED 실패 13건이 모두 통과로 바뀌었다. F에만 있는 46건은 main 받기로 들어온 Demo 시험이고 모두 통과다.
  - PlayMode: RED 실패 15건 중 14건이 통과로 바뀌었다. F에만 있는 1건은 Demo 시험이고 통과다.
  - 두 실행 모두 RED에서 통과·건너뜀이던 시험의 결과 변화는 0이다.
- 열린 실패 D-IMPL-01: `InventorySceneLifecycleTests.MapTransition_RebuildsOnePanelWithTheStoredMirror_RequeriesAfterReady_AndEndingHasNone` :522에서 C_ItemUse가 기대 1·실제 0이다. 구현자 분류는 (d)다.
  - 이 시험은 RED commit에서 사용 버튼 검사를 「우클릭 → 6프레임 → 사용 1건」으로 바꾼 것이다. 같은 패킷을 보는 형제 시험은 최대 3초를 기다린다. 클릭 좌표는 누르기 전에 한 번만 재고, 패널은 HUD를 따라 자기 Update에서 움직인다(`lead-d-impl-01-look-1101-01.txt`~`-03.txt`. 인라인 명령으로 만든 기록이었다. 재측정 `provenance-remeasure-02/out/lead-d-impl-01-look-01~03.txt`. 01은 끝 종료 코드 줄만 재측정 셸의 pipefail 때문에 다르다: `provenance-remeasure-02/look01-exit-check.txt`. 정정 기록 `provenance-remeasure-02/correction-record.md`).
  - 리드 가설은 「움직이는 패널을 눌렀거나 6프레임 안에 패킷이 도착하지 않았다」이다. 확정이 아니다.
  - 리드는 진단 실행 없이 멈추고 보고하게 했다(`msg_5448fbbd3ab0`). 메인 체크포인트 `msg_9de519bcc878`에 메인이 「A」로 답했다(`msg_28eea903507a`). 새 `[Art 시험 Astra]`가 이 시험의 두 우클릭만 고친다. 두 클릭 모두 패널이 멈추기를 기다리고, 사용 패킷은 최대 3초 기다린다. 다시 실패하면 제품 결함으로 보고 새 구현자를 연다. 같은 시험 산출물의 세 번째 수정이며, 네 번째는 사용자 판단이다. 구현 확정 실패 집계와는 따로다.
- Unity 창: 메인 `msg_463977b9ecb6` 05:57~06:45Z이다. 구현자 쪽 한국어 decoding 실패로 창 메시지를 ASCII 줄과 함께 다시 보냈다(`msg_e1c07936ef5a`). 06:18Z에 일찍 반납했다(`msg_c7385d5ca33b`). 리드 잔류 확인은 0이다(`lead-post-impl-unity-check-1101-01.txt`. 인라인 명령이었다. 새 `.meta`·SENTIS SHA 부분은 저장 스크립트 재측정 본문이 같다: `provenance-remeasure-03/out/summary.txt`. 「잔류 0」을 받친 프로세스 수 부분은 인라인 명령의 원시 출력이라 재측정할 수 없고, 리드 쪽 교차 근거도 없다. 정정 기록 `provenance-remeasure-03/correction-record.md`).
- 정산: SENTIS 한 줄(diff sha256 `31e7aaf1…83e2`)을 사본(`impl-settle-sentis-diff-1101-01.txt`)으로 남기고 HEAD로 되돌렸다. 구현 commit은 `4eddf75980029fb9999c92428b518bd29845bda3`이고 원격에 올렸다. 9파일이다(`impl-commit-1101-01.txt`).
- 구현자 자기 신고(`grid-impl-01/entry-self-report.md`): 계약을 읽기 전 첫 메모에 인라인 date/git 값을 적었다. 첫 「쓰기 끝」 보내기는 PS5 UTF-8로 실패해 pwsh로 다시 보냈다. 창 메시지의 한국어 decoding이 깨져 설정 검사기가 거부했다. 정식 메모는 저장 출력으로 보충했다고 했다.
- 독립 검증 계약으로 넘길 설계 관찰(리드가 diff로 본 것, 판정 아님)
  - 가림 판정은 칸 위에 있는 동안 매 프레임 `FindObjectsByType<Canvas>`를 부르고 배열을 새로 만든다. 계약의 「켜진 동안 매 프레임」을 따른 것이다. 비용과 할당이 괜찮은지 본다.
  - 상세 창은 인벤토리 canvas(sortingOrder 5) 안에 있어 HUD(10)와 겹치면 HUD가 위에 그려진다. 사용자 확인 캡처에서 본다.
  - 자리 계산은 오른쪽·아래로 넘치면 뒤집은 뒤 두 축을 모두 clamp한다. 왼쪽·위쪽 넘침도 코드로 본다(메인 `msg_8a35900d5374`).
- 교정 층(두 번째 발생인지 메인에 묻는다). 메인 A(`msg_484ea2fa03bb`), 두 번째 발생, 규칙 후보로 올림.
  - RED 계약 (마)는 사용 버튼 몸짓을 우클릭으로 바꾸라고만 했다. PlayMode 우클릭 사용의 선례(패널이 멈추기를 기다림, 패킷을 최대 3초 기다림)는 가리키지 않았다. 같은 절 첫 발생 4(EditMode 버튼 누름 선례를 가리키지 않음)와 같은 종류로 보면 두 번째 발생이다.
  - 같은 종류라면 반복 규칙 후보는 「몸짓을 바꾸는 시험 계약은 같은 몸짓의 기존 선례(설정·기다림)를 가리킨다」이다. 규칙 정본(.agents)은 Codex 영역이라 리드가 쓰지 않는다. 메인에 올린다.
  - 구현자 쪽 한국어 decoding 실패는 첫 발생이다. 리드는 앞으로 창 메시지에 ASCII 줄을 함께 넣는다.

### 시험 고침 v1 — 2026-11-01

- 계약은 근거 폴더 `grid-fix-contract-v1.md`(sha256 `fbfe25554394e77f9e963795ca1cc85315aa2e08a572beffdb364f9ed8ca2d99`)다. 메인 보고는 `msg_a1a973a2bf24`이고, 근거는 메인 체크포인트 답 「A」(`msg_28eea903507a`)다.
- 계약 인용 정정(#GRIDV-01): 계약은 고정 입력이라 계약 파일은 고치지 않는다. 줄 번호는 `pr1-result-contract-lines-1101-01.txt`다.
  - 계약 :22는 `grid-fix-facts-1101-01.txt`를 「리드의 저장 스크립트로 쟀다」고 적었다. 실제로는 인라인 명령이었다. 함께 인용한 `grid-fix-facts-1101-02.txt`는 저장 스크립트였다(`lead-provenance-trace-1101-04.txt`).
  - 계약 :22·:44·:206의 `grid-fix-facts-1101-01.txt`와 :209~:211·:584의 `lead-d-impl-01-look-1101-01~03.txt`는 저장 스크립트 재측정 본문이 같다. look 01은 끝 종료 코드 줄만 pipefail 때문에 다르다(`provenance-remeasure-02/out/summary.txt`, `provenance-remeasure-02/look01-exit-check.txt`).
  - 계약 :743·:751·:773·:787·:790은 부록 D의 goal 절 사본이라 이 goal의 해당 줄 괄호를 따른다. 부록 F의 :1227·:1444는 선행 시험 계약 :32·:249의 사본이라 「선행 시험 계약 — 2026-11-01」 절의 정정 줄을 따른다.
- 작성자는 `[Art 시험 Astra]` gpt-6-astra xhigh다. handle `term_2aaadcfb-9cfa-4147-be31-b5050ee81c15`, Task `task_fbfe48c19933`, Dispatch `ctx_087dbb3ec793`이다. rollout으로 model·effort를 대조했다(`grid-fix-launch-rollout-check-01.txt`). worker_done은 `msg_cd42c82302a2`(07:00:00Z, outcome succeeded)이고, 보고는 `grid-fix-01/report.md`(sha256 `fe7d50649194228f8749da2403fd3ae0dbafca82c3634190a6cb2afb68d08e12`)다. pane은 대기 확인 뒤 닫았다(`fix-pane-close-1101-01.json`).
- 계약 넓힘
  - 작성자가 ask `msg_80792ba97c62`로 알렸다. 이 시험은 `ReadySyncedTown` 뒤 패널을 「I」로 열지 않는다. 열림 상태는 연결이 갖고 처음 값이 닫힘이다. 그래서 클릭이 닫힌 패널로 갔다.
  - 리드가 다시 쟀다(`lead-fix-open-check-1101-01.txt`. 인라인 명령으로 만든 기록이었다. 재측정 `provenance-remeasure-02/out/lead-fix-open-check.txt`, `0fe7b1a8` 트리. 정정 기록 `provenance-remeasure-02/correction-record.md`). 같은 파일에서 우클릭을 쓰는 시험 다섯은 모두 `OpenWithI`를 부르고, 이 시험만 없다. RED commit은 이 시험의 열기를 건드리지 않았다. 옛 단정 「사용 버튼이 눌릴 수 있는가」는 닫힌 패널에서도 참이었다.
  - ask 답으로 계약을 넓히는 것은 「선행 시험 RED」 때 첫 발생으로 적었다. 그래서 리드가 혼자 정하지 않고 메인에 물었다(`msg_2c8d7a14e138`). 메인이 「A」로 답했고(`msg_c90780508031`), 이것은 같은 시험 산출물 세 번째 수정 안의 내용 넓힘이다. 기록만 한다.
- 바꾼 것은 대상 메서드 한 곳이다(5줄 더함·1줄 뺌, `lead-fix-ready-check-1101-01.txt`).
  - :494 `OpenWithI` 한 줄.
  - :507·:523 두 우클릭 앞의 기존 `WaitForHudAndSettledPanel`.
  - :508 Ready 전 새 패널 열림(alpha 1) 단정 하나.
  - :525 사용 패킷 최대 3초 대기. :526의 정확히 1건 단정은 그대로다.
  - Ready 전 6프레임·0건 단정은 그대로다. 직접 단정은 21에서 22이고, helper 본문은 바뀌지 않았다(`grid-fix-01/change-final.json`, `grid-fix-01/line-table.md`).
- 결과: P(PlayMode 전체 한 번)는 통과 225 / 실패 0 / 건너뜀 46이다. 리드가 결과 XML에서 다시 뽑았다(`lead-r2-fix-v1-1101-01.txt`). 구현 v1 F PlayMode 대비 결과가 바뀐 시험은 대상 하나(실패 → 통과)다. 한쪽에만 있는 시험은 0이다.
- D-IMPL-01은 시험 결함으로 닫혔다. 작성자 분류는 (b) fixture 입력 누락이다. 제품 결함이 아니므로 새 구현자는 열지 않는다. P는 열기와 두 기다림을 함께 넣은 결과라 각 변경의 독립 기여는 재지 않았다.
- 가설 정정: 위 「구현 v1」 절의 리드 가설(움직이는 패널을 눌렀거나 6프레임 안에 패킷이 오지 않음)은 주된 원인이 아니었을 가능성이 크다. 리드와 메인이 함께 정정한다(`msg_2c8d7a14e138`, 메인 `msg_c90780508031`).
- 수정 횟수: 같은 시험 산출물 세 번째 수정으로 닫혔다. 네 번째는 쓰지 않았다. 구현 확정 실패 집계와는 따로다(메인 `msg_28eea903507a`).
- Unity 창: 메인 `msg_9b35874622dd` 06:48~07:10Z이다. 작성자 창 메시지는 한국어와 ASCII 줄로 보냈다(`msg_45bc83911f00`). 06:57Z에 반납했다(`msg_1d7c0c6f7fcc`). 리드 잔류 확인은 Unity·dotnet 0이다(`lead-fix-window-end-check-1101-01.txt`).
- 정산: SENTIS 한 줄(diff sha256 `31e7aaf1…83e2`)을 사본(`fix-settle-sentis-diff-1101-01.txt`)으로 남기고 HEAD로 되돌렸다. 시험 고침 commit은 `7d9d4ff59c80413421546054820f0dac32908f6a`이고 원격에 올렸다. 1파일이다(`fix-commit-1101-01.txt`).
- 작성자 자기 신고(`msg_858445be8861`, 보고 「자기 기록 한계」): 첫 계약 경로 탐색·스킬 안내·계약 읽기·첫 check·계약 hash 명령은 receipt 기록 도구를 만들기 전이라 별도 receipt 파일이 없다. 도구 대화 원시에는 있고, 계약 hash 값은 `grid-fix-01/contract-check.txt`에 남았다고 했다.
- 독립 검증으로 넘길 것
  - Ready 전 클릭 적중의 공개 신호가 없다(위 「남은 위험」).
  - 더한 alpha 단정과 바꾼 대기의 API가 기준 commit 기존 사용과 맞는지 본다(메인 `msg_51bde59fa91c`).
  - 작성자 첫 명령 receipt 누락을 검토한다.
- 교정 층
  - 이번 원인(열기 누락)도 RED 계약 (마) 「몸짓 선례」 문제의 한 갈래다. 형제 시험의 선례는 「연 뒤 누른다」였다.
  - 고침 계약 v1도 기다림 선례만 가리켰고 열기 선례는 가리키지 않았다. 작성자가 ask로 찾았다. 메인 A(`msg_484ea2fa03bb`)의 규칙 후보 「몸짓을 바꾸는 시험 계약은 같은 몸짓의 기존 선례(설정·기다림)를 파일:줄로 가리킨다.」의 「설정」에 든다. 새 첫 발생으로 세지 않는다.
  - 이 규칙 문장은 규칙 정본 반영 전까지 리드 계약에 넣는다.

### 사용자 확인 캡처 v1 — 2026-11-01

- 계약은 근거 폴더 `grid-capture-contract-v1.md`(sha256 `54ac12ecbe5a88100c83d42a78c0cd8824640ab1c40dc55e17794abf9a97cb35`)다. 메인 보고는 `msg_347f04d0559f`다.
  - 격자는 기존 인벤토리 캡처 case(G1)가 그대로 찍는다.
  - 상세 창 둘(G2 재화 주머니, G3 재료)은 같은 캡처 파일에 새 case로 더했다. 촬영 도우미가 그 파일의 private라 새 파일이면 약 200줄을 옮겨 써야 하기 때문이다.
- 실행자는 `[Art Sol]` gpt-6.1-sol max다. handle `term_3dc85efe-5272-4fe8-a505-14259a66e0f4`, Task `task_69639d1023dc`, Dispatch `ctx_1d679e2ed5f8`이다. rollout으로 model·effort를 대조했다(`grid-capture-launch-rollout-check-01.txt`).
  - worker_done은 `msg_a4d590a280bc`(08:01:40Z, outcome succeeded)다. 보고는 `grid-capture-01/report.md`(sha256 `c07712e0d5b8af7e7bac6b817de9b7f4d4cc8ee943737e6a6c60b906425b46af`)다.
  - pane은 대기 확인 뒤 닫았다(`capture-pane-close-1101-01.json`).
- 바꾼 것은 `UiWindowLiveCaptureTests.cs` 하나다(117줄 더함·3줄 뺌, `lead-capture-ready-check-1101-01.txt`).
  - 새 case 둘은 계약의 몸짓 선례 표 순서대로 연다: 마을 준비 → I → 패널 기다림 → 칸에 가상 마우스 올림 → 두 크기 촬영.
  - 칸에 올리는 세 단계는 private 도우미 `HoverInventoryDetail(int slotIndex)` 하나에 모았다. 패널 멈춤 기다림 → 칸으로 이동 → 상세 창 기다림 순서이고, 두 크기 모두 이것을 부른다(리드 답 `msg_bbb0cc079556`).
  - `CaptureBothSizes`에는 선택 인자 `IEnumerator prepareAtMinimum = null` 하나와 줄 하나만 더했다. 기존 호출은 그대로다.
  - 앞 harness의 입력 관문은 이 파일만 「준비됨」 SHA256으로 대조했다(리드 답 `msg_9e667b09aac4`).
- 결과: G2 → G3 → G1을 Editor 창 모드 PlayMode로 case 하나씩 돌렸다. 셋 모두 통과 1·실패 0·건너뜀 0이다.
  - 리드 R-2(`lead-r2-capture-v1-1101-01.txt`)에서 다시 확인했다. 결과 XML의 시험 이름·결과가 맞다. PNG 여섯 장의 SHA256이 record의 capture 줄과 같다. G2·G3는 두 크기 모두 상세 창 표시 True와 이름 「재화 주머니」·「재료」다. 재료는 효과·「우클릭: 사용」 줄이 active False다. teardown에는 Game view 크기 되돌림과 G2·G3의 마우스 되돌림 줄이 있다.
  - 원본 PNG는 Git 제외 근거 폴더 `grid-capture-01/raw/<g1|g2|g3>/attempt-01/captures/`에 있다.
- Unity 창: 사용자 결정 111 A(위 「적용 중인 사용자 결정」)로 Editor 창 07:39~08:05Z를 받았다. 실행자 창 메시지는 한국어와 ASCII 줄로 보냈다(`msg_57904e07c925`). 진입 마감은 앞 goal 선례대로 G2가 창 끝 − 8분, G3·G1이 창 끝 − 6분이다. 07:48Z에 반납했다(`msg_84b0cac58870`). 리드 잔류 확인은 Unity·dotnet 0이다(`lead-capture-window-end-check-1101-01.txt`).
- 정산: SENTIS 한 줄(diff sha256 `31e7aaf1…83e2`)을 사본(`capture-settle-sentis-diff-1101-01.txt`)으로 남기고 HEAD로 되돌렸다. 캡처 commit은 `5aeb415b3f05f92e86e4b8433075485a7601cf60`이고 원격에 올렸다. 1파일이다(`capture-commit-1101-01.txt`).
- 메인 질문 `msg_c446b0fc6fef`와 리드 원천 답 `msg_8c2ab57ee341`(줄 `lead-capture-question-lines-1101-01.txt`, 화소 `lead-png-pixels-1101-01.txt`·`-02.txt`)
  - G1 1920×1080은 패널이 반투명하게 찍혔다. 기존 창 열림 효과(`UiWindowOpenMotion.cs` :12, 0.12초, 효과 자체 CanvasGroup `UiWindowOpenEffect.cs` :29·:66)가 끝나기 전에 G1 case가 I 뒤 3프레임 만에 찍었기 때문이다(`UiWindowLiveCaptureTests.cs` :215~:216·:888). 제품 결함이 아니다. 앞 goal의 같은 case 캡처도 같다. G1 1280×720과 G2·G3는 불투명 바탕(`#F6E2BE`)이다. 리드 판단으로 다시 찍지 않는다.
  - 2줄 격자 아래 빈칸은 설계값이다. 창 높이 식(`InventoryPanelView.cs` :15~:21)이 2줄에서 430이다. 1920에서는 최대 1.3배 맞춤으로 507×559다. 재화 줄 아래는 「마지막 사용 결과」 줄 자리다(`UseResult` :127).
- 사용자에게 보일 관찰(디자인 확인, 리드가 정하지 않음)
  - G3 두 크기에서 재료 상세 창이 그 칸 구석 숫자 「3」을 덮는다. 수량은 상세 창 「수량 3」에 있다(실행자 관찰 `msg_8c5b2b2bcffb`).
  - 상세 창은 2줄째 칸 일부와 패널 오른쪽 밖까지 덮는다. 상세 창 자리 설계대로다.
- 실행자 자기 신고(report 「자기 신고와 기록 한계」)
  - 첫 명령 몇 개가 receipt 도구 전에 돌았고, 계약 해시는 인라인 명령으로 저장했다(ask `msg_cdd99b75edef`). 리드는 계속을 답했고, 저장 스크립트로 다시 잰 값은 기준과 같았다.
  - 「Unity 사용 끝」의 SENTIS 「알려진 쌍 아님」은 `-U0` diff 해시를 기본 context 해시와 비교한 실수였다. 실행자가 정정했고(`msg_b6d59b1c814e`) 리드 측정과 같다.
  - heartbeat 간격이 899.96초(07:42~07:57Z)로 5분을 넘었다(escalation `msg_c1f3ba6b3ab3`). 그 사이 status 셋이 왔다. 리드는 report 자기 신고로 정산하게 했다(`msg_5f2eeb0c18ac`).
- 독립 검증으로 넘길 것
  - 새 case 둘의 stillShown 람다 약 30줄이 칸 번호·이름 문자열만 빼고 같다. 가독성 판정 항목이다(메인 동의 `msg_163fec4ff6c2`).
  - G1 record의 「panel alpha」는 창 root group만 읽어 열림 효과 group을 보지 않는다.
  - 실행자 첫 명령 receipt 누락과 heartbeat 간격을 검토한다.
  - PlayMode batchmode 전체에서 새 캡처 case 둘은 출력 폴더 변수가 없어 건너뜀으로 센다(건너뜀 +2 예상, `UiWindowLiveCaptureTests.cs` `Begin`).
- 교정 층
  - 첫 발생: 캡처 case가 창 열림 효과 도중을 찍었다. 앞 goal 캡처부터 있던 일이다. Core 24칸 뒤 6줄 캡처 계약에서 격자 case가 효과 끝까지 기다리게 고친다(위 「PR 경계」 PR2).
  - 두 번째 발생: 작업자 첫 명령이 receipt 도구 전에 돌았다(첫 발생은 시험 고침 작성자 `msg_858445be8861`). 포인터에 「처음 명령부터 receipt 기록」을 적었는데도 났다. 다음 계약부터 리드가 시작 스크립트(계약 SHA256·HEAD·status를 receipt와 함께 저장)를 만들어 포인터에 경로·SHA256으로 준다. 메인은 이 방향에 이견이 없고 메인 최종 보고의 규칙 후보로 올린다(`msg_163fec4ff6c2`).
- 사용자 그림 확인: 112 「OK」다(메인 `msg_7e7523b25aa1` 1항). PR1에 바꿀 것은 없다. 같은 사용자 입력의 HUD 새 요구는 PR2다(아래 재개 카드).

### 독립 검증 — 2026-11-01

- 계약은 근거 폴더 `grid-verify-contract-v1.md`(sha256 `6ab7a6451bcd33b5da4670714fd089b5398d9a363f9c818e9a89189909a3b2a3`, 조립 receipt `grid-verify-contract-v1.receipt.txt`)다. 대상 head는 `47cbdcac`다. 메인 보고는 계약 준비 `msg_838bc19793b8`, 기동 `msg_c64b74970dcc`다.
  - 포인터에 시작 스크립트 `grid-verify-start-1101-01.mjs`를 넣어 첫 명령부터 receipt를 남기게 했다. 위 「사용자 확인 캡처 v1」 교정 층 두 번째 발생의 교정이다. 검증자의 첫 명령은 이 스크립트였다(판정 원문 「대상과 독립성」).
  - 몸짓 선례 규칙(메인 `msg_484ea2fa03bb`)을 계약에 넣었다.
- 검증자는 `[Art 검증자]` gpt-6-astra xhigh다. handle `term_1d599bf7-ab27-46aa-9304-6f1994188ed2`, Task `task_946c398e4a36`, Dispatch `ctx_d517c169214c`이다. 첫 화면에 업데이트 창·선택창이 없었고(`grid-verify-launch-screen-01.json`), rollout으로 model·effort를 대조했다(`grid-verify-launch-rollout-check-01.txt`).
  - worker_done은 `msg_893608d8584d`(09:27:43Z, PASS)다.
  - 판정 원문은 `grid-verify-01/verdict.md`(sha256 `b175347884b698dee50843f99ea791467964ae30b73d1584869784d86241cbc9`)다.
  - pane은 빈 프롬프트를 확인하고 닫았다(`verify-pane-close-1101-01.json`).
- 판정은 PASS다. 제품 차단 결함은 0이다. 리드 기록 결함 #GRIDV-01·02는 아래 메인 처분과 재측정 대조로 처분됐다.

| 실행(판정 원문 「실행과 결과」) | 통과 / 실패 / 건너뜀 |
|---|---|
| F EditMode 전체 | 1792 / 0 / 0 |
| F PlayMode 전체 | 226 / 0 / 48 |

- 결과 읽기
  - PlayMode 건너뜀 48은 Ignored 44·Explicit 4다. 새 캡처 case 둘은 출력 폴더 변수가 비어 건너뜀에 든다(판정 원문 「기존 결과 비교와 실패 분류」).
  - 실제 진입 경로: 새 PlayMode 시험 `InventoryPointerEntrySceneTests`가 실제 Town 장면에서 I → 같은 `Mouse.current`로 올림 → 우클릭 → `C_ItemUse`를 봤다. 출력은 `fixtureMouse=193 currentMouse=193 revision=7`, `itemId=2 expectedRevision=7 packets=1 negative=False`다. 서버 쪽은 대본 TCP peer다(완료조건 9).
  - 보조 실행 R 다섯: EditMode 01은 의도한 음성(경계 8·관문 3), 02는 정상 통과다. PlayMode 03은 새 시험의 Town 진입 준비에서 실패했고, 04는 검증자 시험의 using 누락 컴파일 오류(CS0103)다. 05는 메인 「VR05 A」(`msg_993e07ebd115`)로 허용한 추가 1회이고 의도한 음성 실패다. 03·04는 제품 회귀나 음성 성공으로 세지 않았다.
  - ask로 바뀐 계약 방법: R PlayMode 05 1회와 어셈블리별 최신성 관문이다(메인 `msg_993e07ebd115`). 검증자 질문의 namespace 오기(Combat)는 실제 `Dawnholder.Client.Network`로 정정했다(리드 `msg_4ddead10f810`, 근거 `lead-vr05-namespace-check-1101-01.txt`).
  - 최종 runner SHA256은 `f0e4d47d7ba79915289ec8144c0127065d7bdf38a07e6af786f6878b9529599e`이다. 바뀐 줄은 diff 둘(`d144f8bb…5484`, `9db43857…6566`)에 있다(메인 GRIDV-02 처분 `msg_434b69242354`).
- 리드 R-2 표본(`lead-r2-verify-sample-1101-01.cjs` → `.txt`, 09:28:29Z): 판정 원문과 모두 같다. verdict·F XML 둘의 SHA256, F EditMode 1792 case 전부 통과·고유 이름 1792, F PlayMode 274 case(통과 226·건너뜀 48), 새 독립 시험 EditMode 11·PlayMode 1 통과와 출력 줄, 새 시험 4파일과 최종 runner의 SHA256을 봤다.
- Unity 창: 08:47:02Z~09:37:02Z였다(`lead-verify-window-open-1101-01.txt`, 메인 미리 배정 `msg_f1139600734d`). 마지막 Unity 종료는 09:17:11Z이고, 메인에 「V 끝, 창 반납」을 보냈다(`msg_9e349bfbdda7`, 09:21:29Z). 리드 잔류 확인은 프로세스 0이다(`lead-verify-window-end-check-1101-01.txt`).
- 정산(`settle-verify-sentis-1101-01.txt`, 09:28:44Z): SENTIS 한 줄(diff sha256 `31e7aaf1…83e2`)을 검증자 사본과 대조하고 사본(`verify-settle-sentis-diff-1101-01.txt`)을 남긴 뒤 HEAD로 되돌렸다. 추적 변경은 0이다.
- 독립 시험 채택(리드 결정): 판정이 PR 포함을 제안했고 포함 여부는 리드 몫이라고 적었다. 리드는 새 시험 두 파일과 Unity `.meta` 둘을 PR1에 넣는다. 선례는 PR268의 독립 회귀 시험 채택 `69eb9fdf`다. 메인 보고는 `msg_df285250ec13`이다.

| 채택 파일 | SHA256(판정 원문) |
|---|---|
| `03_Client/Assets/Tests/EditMode/InventoryPointerBoundaryTests.cs` | `6225e5c7474b1f3ded484013bcf51e5c5c8565fbd1360cad4c49766e4bf0a0ee` |
| 같은 `.cs.meta` | `8de7cab2a35d68f285abd82339931bb425cd7bbe9196b55d667dacc983016cf0` |
| `03_Client/Assets/Tests/PlayMode/InventoryPointerEntrySceneTests.cs` | `b3679ec87e268563339f6d29cad550c9ae8d8ea8cb73e249f8f8c5e27dc4bac5` |
| 같은 `.cs.meta` | `e5bbbcc2589a03f88850b95ebf29a2911e34ad7af86ccd576596b570dbfe6ed3` |

- 비차단 관찰(판정 원문 O1~O4)과 리드 처리
  - O1 `InventoryOverlayOcclusion.cs:17`: 채운 칸에 올린 동안 매 프레임 Canvas 배열을 찾고 새로 만든다. 비용은 재지 않았고 성능 요구 위반 근거는 없다. 지금 고치지 않고, 같은 판정을 다시 쓰는 다음 작업에서 다시 본다.
  - O2 `UiWindowLiveCaptureTests.cs:240`·`:291`: 새 캡처 case 둘의 stillShown 람다 약 30줄이 겹친다. 6줄 캡처 계약에서 같은 파일을 고칠 때 판단한다.
  - O3 상세 창 canvas 5와 HUD 10: HUD 위에 그리라는 요구는 없다. 그림은 사용자 112 「OK」다.
  - O4 맵 이동 시험의 Ready 전 클릭: 칸 적중 공개 신호가 없다. 위 「남은 위험」 둘째 줄 그대로다.
- 미검증(판정 원문): 사람 플레이, GameServer, DB, 사람 마우스·Editor GUI, 실화면 캡처, 합친 main/PR207 상태의 Unity, Core 실제 24칸 통합, CI, 별도 dotnet build, 그림 품질(사용자 몫)이다. 모두 독립 검증 세션이 실행하지 않은 것이다. 「실화면 캡처」는 결과 기록 commit에서 빠졌고, 아래 「좁은 재실사 — 2026-11-01」의 #GRIDR-01로 더했다.
- 메인 처분(검증 중, 모두 A)
  - 「GRIDV-01 A」(`msg_2d6d403def3e`): 인라인 측정 출력이 goal·계약에 쓰였다. 정정 기록으로 검증을 계속하고, 결과 기록 commit에서 출처 설명을 고친 뒤 바뀐 줄만 새 독립 세션이 재실사한다. 위 각 절의 출처 괄호와 계약 인용 정정 줄이 그 고침이다.
  - 「GRIDV-01 추가 A」(`msg_7e3209a42aca`): 추가 다섯 사슬을 첫 처분에 묶는다. 리드 전수에서 같은 종류는 묻지 않고 묶고, 다르거나 재측정할 수 없는 것만 따로 묻는다. 전수 분류는 `provenance-remeasure-03/classification.tsv`이고 개수는 `provenance-remeasure-03/count-classes.txt`다.
  - 「전수 A」(`msg_3aefb7b0bdc2`): 재측정할 수 없는 기록도 기록과 문장을 그대로 둔다. 「범위 초안과 승인」의 진입 줄, 「구현 v1」의 쓴 것 줄과 Unity 창 줄에 재측정 불가와 교차 근거를 적었다. 리드 메모 01과 메인 보고 본문 쪽은 메모 01 끝 정정 절에만 적는다.
  - 「GRIDV-02 A」(`msg_434b69242354`): 리드의 `/dev/null` 일곱 호출이다. 원 시점 stderr는 복구할 수 없고, 지금 재실행한 근거로만 본다(`provenance-remeasure-01/correction-record-devnull.md`).
  - 「VR05 A」(`msg_993e07ebd115`): 위 R PlayMode 05와 어셈블리별 최신성이다.
  - 「VDELAY A」(`msg_b57c63a172cf`): 아래 교정 층의 첫 발생이다.
- 교정 층
  - 두 번째 발생(메인 `msg_2d6d403def3e` 2항): 리드 근거 기록이 저장 스크립트가 아닌 인라인 명령으로 만들어졌다.
    - #GRIDV-01(검증자 `msg_e0c3e4c09173`)의 여섯과 추가 소비 위치(`msg_9b5f9dfd5907`)의 일곱 파일이다. 리드 전수(`provenance-remeasure-03`)가 같은 종류를 더 찾았다.
    - 규칙은 이미 있었다(메인 `msg_4bef008e3fa9` 「반복 금지」). 리드 기록도 시작 스크립트·저장 스크립트로만 만든다. 메인 최종 보고의 규칙 후보다.
  - 반복 발생: 리드 명령의 `/dev/null` 일곱 호출이 신고되지 않았다(#GRIDV-02 `msg_c415be1687f3`, 메인 처분 A `msg_434b69242354`).
    - 앞 신고 둘은 리드 맥락 메모 01 :76과 위 「선행 시험 RED」의 리드 자기 신고 줄이다. 정정 기록은 `provenance-remeasure-01/correction-record-devnull.md`다.
    - 기계 검사(프로젝트 hook) 제안은 메인 최종 보고의 규칙 후보다. 설정 변경은 사용자 결정이라 지금 하지 않는다.
  - 두 번째 발생(메인 `msg_7e3209a42aca` 3항): 리드 요약 파일의 hash 목록이 쓰는 중인 자기 자신을 해시해 자기 줄이 틀렸다.
    - 첫째는 `provenance-remeasure-01/devnull-rerun-01/summary.txt`(검증자 `msg_74a2abc1aaa0`), 둘째는 `provenance-remeasure-02/out/summary.txt`다.
    - 교정: 저장 스크립트는 자기 출력 파일을 hash 목록에서 뺀다. manifest는 완성 뒤 폴더 밖에서 따로 잰다(`provenance-manifest-1101-01.sh`·`-02.sh`). 메인 최종 보고의 규칙 후보다.
  - 첫 발생(메인 「VDELAY A」 `msg_b57c63a172cf`): 검증자의 「Unity 사용 끝」 알림이 마지막 Unity 종료(09:17:11Z) 뒤 약 3분인 09:20:22Z에 갔다(자기 신고 `msg_3c34d6adca16`). 창 안이었고 기다린 파트는 없었다. 다음 계약부터 「Unity 종료를 확인한 직후 「사용 끝」을 먼저 보내고, 잔여 관찰은 그 뒤에 덧붙인다」를 넣는다.
- 좁은 재실사로 넘길 것(메인 `msg_2d6d403def3e` 2항)
  - 결과 기록 commit에서 바뀐 goal 줄: 출처·재측정 불가 괄호, 계약 인용 정정 줄, 사용자 그림 확인 줄, 이 절, 재개 카드와 진척 단계.
  - 리드 메모 01 끝 정정 절. 앞 280줄은 검증 계약 부록 A의 SHA256 `cd10d368…cb57` 그대로여야 한다.
  - 채택한 독립 시험 4파일. SHA256이 위 표와 같아야 한다.

### 좁은 재실사 — 2026-11-01

- 계약은 근거 폴더 `grid-recheck-contract-v1.md`(sha256 `43bd51d89f14006a9ec76ecc51857700e91d13a17141d09b8d6917bd8cdef399`, 조립 기록 `assemble-grid-recheck-contract-1101-01.txt`)다. 대상은 `47cbdcac..52fa3286`의 5파일과 리드 메모 01 끝 정정 절이다. Unity는 쓰지 않았다. 메인 보고는 계약 `msg_9e458c946944`, 기동 `msg_fd44c518a232`다.
- 검증자는 새 `[Art 검증자]` gpt-6-astra xhigh다. handle `term_b8038cf9-7d92-4bd6-b228-267bb095fa01`, Task `task_f39656ddac47`, Dispatch `ctx_858821c8d03a`이다. 첫 화면에 업데이트 창·선택창이 없었고(`grid-recheck-launch-screen-01.tail.txt`), rollout으로 model·effort를 대조했다(`grid-recheck-launch-rollout-check-01.txt`).
  - worker_done은 `msg_227dd4694f0a`(10:25:07Z, NOT PASS)다.
  - 판정 원문은 `grid-recheck-01/verdict.md`(sha256 `77122af4264bc271d287c83759a86fb327561c1d51cbe543f74cb9cf0989c79b`)다.
  - pane은 빈 프롬프트를 확인하고 닫았다(`recheck-screen-done-1101-01.json`, `recheck-pane-close-1101-01.json`).
- 판정은 NOT PASS다. 기록 결함 #GRIDR-01 하나다. 제품 결함은 없고, 앞 독립 검증의 PASS를 다시 판정하지 않았다. 계약 판정 기준 아홉 가운데 5만 결함이고 나머지는 지적이 없다(판정 원문 「계약 판정 기준 1~9」).
- #GRIDR-01: 위 「독립 검증」 절의 미검증 줄에 앞 판정 원문 `grid-verify-01/verdict.md` :140의 「실화면 캡처」가 빠졌다. 리드가 판정 원문의 목록을 옮길 때 빠뜨렸다. 검증자의 중간 통지는 `msg_0fae9c8716b2`다. 고침은 그 줄에 「실화면 캡처」를 더한 것이다. 고정 계약·앞 판정·원시는 고치지 않았다.
- 리드 R-2 표본(`lead-r2-recheck-sample-1101-01.mjs` → `lead-r2-recheck-sample-1101-01.txt`, 10:26:26Z): 판정 원문과 모두 같다. verdict SHA256, goal :429와 앞 판정 :140의 원문, 채택 4파일의 HEAD blob SHA256, 시험 고침 계약 :1227·:1444가 RED 계약 :32·:249의 사본인 것, goal의 `grep_exit` 소비 0을 봤다.
- 생존 신호 정산(ORCA 「생존 신호와 초과 정산」): 정본 helper `99_Tools/Orca/check-liveness.mjs`로 잰 최대 간격은 285초이고 300초 초과는 0이다(저장 스크립트 `grid-recheck-liveness-1101-02.sh`의 출력 `grid-recheck-liveness-1101-02.txt`). 리드가 helper를 인라인 인자로 먼저 돌린 `grid-recheck-liveness-1101-01.txt`와 결과 JSON이 같다. 검증자가 자기 신고한 302.291초(`msg_2dc5ca8ba678`)는 heartbeat 호출만 센 간격이다. 정본은 그 사이의 status `msg_0fae9c8716b2`도 신호로 센다.
- 비차단 관찰(판정 원문 O1~O3, 줄 번호는 head `52fa3286` 기준)과 리드 처리: 지금 고치지 않는다. 고칠 줄이 늘면 두 번째 재실사 범위가 커진다. 결과 기록 PR(6줄 캡처 뒤) 때 다시 본다.
  - O1 이 파일 :289·:306: 긴 괄호 안에 출처·같음·재측정 불가·교차 근거가 함께 있다.
  - O2 메모 01 :285: 끝 정정 절이 첫 여섯 사슬의 메모 01 :69와 `provenance-remeasure-01/correction-record.md` :27을 직접 가리키지 않는다.
  - O3 이 파일 :412·:446: `→ .txt`와 `-02.sh`처럼 줄인 파일 이름이다.
- 교정 층
  - 첫 발생: 판정 원문의 미실행 목록을 goal에 옮길 때 항목 하나가 빠졌다(#GRIDR-01). 판정 원문은 앞 판정의 미실행 목록과 결과 기록의 차집합 검사로 옮길 수 있다고 적었다. 동의어는 사람이 판정한다. 이번 범위에서는 만들지 않는다.
- 두 번째 좁은 재실사로 넘길 것
  - 고침 commit에서 바뀐 goal 줄: 위 「독립 검증」 절의 미검증 줄, 이 절, 재개 카드, 상태 줄, 진척 단계.
