# 기본 공격 키를 왼쪽 Ctrl로 바꾸기

## 재개 지점

**다음 첫 일:** 구현자(신규 Opus)가 제품 3파일을 바꾸고 선행 시험과 같은 명령의 통과 원시를 남긴다. 계약은 근거 폴더 `implement-01-contract-v1.md`다. 구현자는 먼저 띄워도 되지만 Unity 실행은 메인 배정 메시지가 온 뒤에만 한다. 창은 메인 대기열 1번이다(`msg_812478372cdf`). 선행 시험 결과는 아래 [선행 시험 결과](#선행-시험-결과)에 있다.

| 항목 | 현재 |
|---|---|
| 승인 | 결정 65 A(사용자). 범위 초안 v1은 메인이 보드 내용과 대조해 통과시켰고 사용자 재승인은 없다(메인 `msg_6d9e5309e129`). 이 goal은 초안 v1을 옮기고 최신 main 사실로 줄 번호를 다시 확인한 것이다. |
| 리드 | `[Effect 리드 Opus]`(claude-opus-5-5 xhigh). Run `run_5f3209972e68`, 회신 주소 `run:run_5f3209972e68`. |
| branch | `feat/attack-key-left-ctrl-20261030`. PR280(앞 goal 종료 기록) head `0029c59b` 위에 쌓았다. 그 아래 origin/main은 `7bc75898`(Art PR279 병합까지)다. 쌓은 이유는 이 goal이 고치는 `CURRENT.md` Effect 줄을 PR280도 고치기 때문이다(메인 운영 방식 「같은 파일을 겹쳐 고치는 PR은 앞 PR branch 위에 쌓는다」). PR base는 PR280 branch이고, PR280이 병합되면 main으로 바뀐다. 작업 공간 effect-active. |
| 근거 폴더 | `.backups/verification/2026-10-30-attack-key-left-ctrl/`(Git 제외). 리드 원시는 `lead/`다. |
| 실행 창 | Unity는 effect-active `03_Client` batchmode만 쓴다. `Library`(3,088,006,164 B)는 앞 goal에서 미리 가져온 것이라 다시 쓴다(메인 처분). 창은 메인 배정이다. |

## 진척 단계

- [x] goal과 계약 기록
- [x] 선행 시험과 실패 원시
- [>] 구현과 통과 원시
- [ ] 독립 검증 통과
- [ ] PR 생성과 확인 항목
- [ ] PR 병합
- [ ] 결과 기록과 종료

## 적용 중인 사용자 결정

### 10-30 결정 65 A — 기본 공격 키를 왼쪽 Ctrl로, 별도 작은 PR

- 사용자 원문(2026-10-30 13:4x KST 메인 pane 제출, Dash 플레이 중, 메인 전달): 「그리고 지금 기본 공격이 클릭으로 되어있는데」 「왼쪽 컨트롤로 키 매핑 바꿔야 될 거 같아」
- 결정 원문(13:5x KST 메인 pane 제출, 메인 전달 `msg_5462cd99ffc6`): 「대시보드 결정 응답: 1) 65 - 기본 공격 키를 마우스 클릭에서 왼쪽 Ctrl로 바꾸는 작업을 시연 전에 할지 → A 지금 별도 작은 PR로 한다」
- 메인이 보드에 적은 A의 내용: PR278 병합 뒤 Effect 리드가 새 목표로 맡는다. 키보드·마우스의 공격 바인딩을 왼쪽 클릭에서 왼쪽 Ctrl로 바꾸고 Enter는 그대로 둔다. 관련 시험을 고치고 독립 검증을 받은 뒤, 사용자가 한 번 더 직접 눌러 보고 병합 승인을 한다. A는 추가 파트 Effect에 새 목표 하나를 더 맡기는 승인도 겸한다.
- 메인 처분(`msg_6d9e5309e129`, 메인 결정): 범위 초안 v1은 보드 내용과 어긋나지 않는다. UI 맵 Click 유지와 「마우스 클릭은 공격하지 않음」 회귀 유지도 범위 안이다. 초안을 다시 사용자 승인에 올리지 않는다.
- 초안의 질문 1(시연을 Editor Play로 하나 빌드로 하나)은 메인이 사용자에게 물었고 마지막 보고에 묶여 있다. 메인은 빌드 기준으로 진행한다(`msg_3685d4ed0fea`). 그래도 사용자의 마지막 확인이 Editor Play일 수 있어 플레이 확인 항목에 Editor에서 생길 일과 피하는 법을 한 줄 넣는다(같은 msg). Unity 단축키 설정(사용자 전역 profile)은 바꾸지 않는다(같은 msg).

### 10-27 결정 4 A — 리드·구현자 Claude, 독립 검증만 GPT Astra

- 원문과 적용 범위는 CLAUDE.md [결정 4A 절](../../../CLAUDE.md#claude-4a-interim)이다. 선행 시험 작성자·구현자는 신규 `claude --model claude-opus-5-5 --effort xhigh`, 태그 `[Effect 구현 Opus]`다. 독립 검증은 신규 `codex --model gpt-6-astra -c model_reasoning_effort=xhigh --disable fast_mode`, 태그 `[Effect 검증자]`다.

### 10-30 메인 운영 방식 — msg_3685d4ed0fea(메인 결정, 사용자 결정 아님)

- 근거 사용자 원문(개선안 묶음, 14:1x KST, 메인 전달): 「모두 끝까지 진행하고 마지막에 한번에 체크 할 수 있게 전부 작업 완수하고 보고해줘」
- 캡처 확인·직접 플레이·병합 승인은 마지막에 사용자가 한 번에 한다. 각 PR은 끝에 사용자가 볼 캡처와 플레이 확인 항목 몇 줄을 준비한다.
- 병합은 마지막까지 없고 메인만 한다. 같은 파일을 겹쳐 고치는 PR은 앞 PR branch 위에 쌓는다.
- 선행 시험 방식은 정본 TDD다(경량 경로 미적용).
- 질문으로 일을 멈추지 않고 되돌리기 쉬운 쪽으로 진행한 뒤 그 선택을 보고에 적는다.

### 10-30 메인 결정 — PR280 실사의 맥락 메모 부재 처분 A — msg_57dbbc559b44

- 메인 결정(사용자 결정 아님). PR280(앞 goal 종료 기록) 문서 실사 검증자의 공식 질문 `msg_f24e3f90585d`를 리드가 올렸다(`msg_3a8ccaebd3ca`).
- 사실: 앞 리드가 종료 기록(결과 기록·Gardener·BACKLOG·CURRENT·PR280)을 쓰기 전에 맥락 메모를 갱신하지 않았다. 앞 리드 메모의 마지막 절은 「갱신 — Library 미리 가져오기」(mtime 2026-10-29T20:11:00Z)다. 원시는 앞 goal 근거 폴더 `lead/closeout-verify-01/memo-search-01.txt`다.
- 처분: 기록만 하고 차단을 푼다. 검증자는 판정의 절차 이탈 목록에 사실로 적고 처분 원문과 msg ID를 넣는다. 메인은 마지막 보고의 PR280 항목에 이 절차 이탈을 한 줄로 적는다.
- **첫 발생: 종료 기록 쓰기 전 맥락 메모 미갱신.** 같은 일이 다시 생기면 반복 규칙 후보로 메인에 올린다. PR280 head를 실사 중에 바꾸지 않으려고 앞 goal 대신 이 goal에 기록한다(리드 선택).
- 고쳐 읽기(메인 결정 `msg_68ef27e7bc61` 3 A): 「첫 발생」은 앞 goal 안의 첫 발생이다. 프로젝트로는 BACKLOG `lead-direct-write-context-memo`의 같은 부류 세 번째다. 그 행에 세 번째 관측을 덧붙이는 일은 이 goal의 종료 기록 PR에서 한다. 반복 규칙 요건이 이미 찬 행이라 정본 반영(Rules)은 사용자 결정이고, 메인이 마지막 보고의 규칙 후보에 올린다.
- 같은 메시지 1 A·2 A: Gardener의 허용 목록 밖 읽기 전용 셸 명령과 앞 리드의 표시용 인라인 `node -e` 1회는 판정의 절차 이탈 목록에 사실로 적고 차단하지 않는다.

### 메인 결정 — 기존 시험 실패의 전수 분류 — msg_71e41e231d55

- 원문은 [하네스 원칙 goal의 재전달 절](../2026-10-03-harness-principles/goal.md#기존-테스트-대량-실패의-분류)이다. 선행 시험 작성자와 독립 검증자 계약에 그 절 원문을 그대로 붙인다.

## 만들 것

- Player 맵 Attack 동작의 Keyboard&Mouse 바인딩 하나의 `path`를 `<Mouse>/leftButton`에서 `<Keyboard>/leftCtrl`로 바꾼다.
  - 대상: id `05f6913d-c316-48b2-a6bb-e225f14c7960`, `03_Client/Assets/InputSystem_Actions.inputactions` 270행 id, 271행 path.
  - id·`groups`(`;Keyboard&Mouse`)·나머지 필드는 그대로 둔다.
- 결과 동작:
  - 기본 공격은 왼쪽 Ctrl과 Enter다.
  - 게임 월드의 마우스 왼쪽 클릭은 더 이상 공격하지 않는다.
  - UI 버튼 클릭은 UI 맵 Click 바인딩(`<Mouse>/leftButton`)이라 그대로다.
- `LocalPlayerInput.cs` 70행의 낡은 주석 「Space 또는 좌클릭」을 실제 키로 고친다. Space 바인딩은 지금도 없다.
- 클라이언트 영역 계약 `00_Document/domains/client.md`의 공격 입력 문단(40행)에 바뀐 사실을 반영한다.
  - 마우스는 공격 동작에 묶이지 않는다.
  - 공격의 UI 위 포인터 차단은 터치 탭 바인딩에만 남는다.
- 시험은 아래 「요구 시험」을 따른다.

## 건드릴 곳과 소유

| 파일 | 변화 | 쓰는 세션 |
|---|---|---|
| `03_Client/Assets/InputSystem_Actions.inputactions` | path 1줄 | 구현자 |
| `03_Client/Assets/Scripts/Input/LocalPlayerInput.cs` | 주석 1줄 | 구현자 |
| `00_Document/domains/client.md` | 40행 문단 1~2문장 | 구현자 |
| PlayMode 시험 6파일과 새 EditMode 시험 1파일 | 아래 「요구 시험」 | 선행 시험 작성자 |
| 독립 검증자의 새 시험 파일 | 검증자가 정함 | 검증자 |
| goal.md·CURRENT Effect 줄 | 범위·결과·근거 | 리드 |

보존 조건:

- `.inputactions.meta`의 guid `2bcd2660ca9b64942af0de543d8d7100`과 `generateWrapperCode: 0`을 그대로 둔다. 생성 wrapper 클래스가 없어 재생성 대상도 없다.
- 이 자산은 index LF, 작업 트리 CRLF(`text=auto`)이고 끝 줄바꿈이 없다. `git diff origin/main -- <파일>`이 path 한 줄만 보여야 한다.
- Unity Input Actions 창에서 저장하면 파일 전체가 다시 직렬화될 수 있다. 그래서 텍스트 한 줄 편집으로 바꾸고, Unity 가져오기 뒤에도 diff가 한 줄인지 다시 확인한다.
- 이 바인딩 id를 참조하는 코드·시험·저장된 재설정 값은 없다(`bindingIdReferences` 0건).
- Art 70 A(잡는 칸 공격 0 시험)가 기대는 불변식 I1~I4를 지킨다. 원문은 Art 리드 `msg_ca5b2e0d7b5d`이고 저장본은 앞 goal 근거 폴더 `lead/goal-65a-draft/art-70a-contract-01.json`(sha256 `963c2324…7958`)이다.
  - I1: `GameplayPointerInput`과 `internal bool IsOverUi(InputControl?)`의 이름·인자·뜻이 남는다.
  - I2: 포인터에서 시작하는 공격이 남으면 `IsOverUi` 판정을 거친 뒤에만 나간다. 터치 탭 바인딩이 여기에 해당한다.
  - I3: Player 맵에 `Attack` 동작과 키보드 바인딩이 하나 이상 남는다. 포인터가 UI 위에 있어도 그 키로 `C_Attack`이 나간다.
  - I4: `LocalPlayerMovement.CanAttack`·`IsActionLocked`와 `PacketID.C_Attack`이 남는다.
  - 이 goal은 path 한 줄만 바꾸고 `OnAttack` 경로와 `GameplayPointerInput`은 건드리지 않아 넷을 모두 지킨다. 70 A는 이 goal의 파일에 쓰지 않는다. 70 A의 양성 대조군은 Attack 동작에 묶인 키보드 control을 런타임에 찾으므로 Enter든 왼쪽 Ctrl이든 통과한다. 그래서 두 PR은 어느 쪽이 먼저 병합돼도 다시 고칠 일이 없다.

## 요구 시험

바꾼 뒤에는 마우스 왼쪽 클릭이 공격 동작이 아니다. 그래서 「월드 클릭이 공격한다」는 양성 대조는 실패하고, 「UI 클릭이 공격하지 않는다」는 검사는 포인터 차단 경로를 거치지 않고 늘 참이 된다.

| 묶음 | 위치(PlayMode, 최신 main에서 다시 확인) | 지금 기대 | 바꾼 뒤 |
|---|---|---|---|
| 양성 대조 | `InventorySceneLifecycleTests.cs` 618–622, 주석 583–586 | 같은 마우스의 월드 클릭이 공격한다 | 포인터를 월드에 둔 채 왼쪽 Ctrl이 공격한다. 같은 월드 클릭이 이제 C_Attack 0건이라는 확인을 더한다. Enter 양성 대조(624–630)는 그대로 둔다. |
| 양성 대조 | `HudGoldAndPanelToggleSceneTests.cs` 82–87 | 닫힌 패널의 Use 자리 클릭이 공격으로 통과한다 | 뜻은 「닫힌 패널이 포인터를 가로채지 않는다」다. 공격 패킷 대신 그 자리의 UI raycast 0건과 C_ItemUse 0건으로 보고, 공격 가능 여부는 왼쪽 Ctrl로 따로 본다. |
| 양성 대조 | `GuildGameplayWindowTests.cs` 337–343 | 창을 닫은 뒤 월드 클릭 공격이 돌아온다 | 닫힌 창 raycast 0건 확인(335–336)은 그대로 두고, 공격 복구는 왼쪽 Ctrl로 본다. |
| 양성 대조 | `InventoryServerIntegrationTests.cs` 109–115 | 실서버에서 월드 클릭이 공격 예측을 시작한다 | 왼쪽 Ctrl로 바꾼다. 별도 CLI 필터이고 127.0.0.1:7777 실서버가 필요하다. |
| 양성 대조 | `GuildLiveServerWindowTestTools.cs` 457–(Swing), 호출 3곳 | live lane에서 월드 클릭으로 휘두른다 | 왼쪽 Ctrl로 바꾼다. Explicit lane이라 이 goal에서는 컴파일만 보고 미실행으로 적는다. |
| 늘 참이 되는 음성 검사 | 9줄(`InventorySceneLifecycleTests` 414·601·613·720, `HudGoldAndPanelToggleSceneTests` 124, `GuildUiConnectionTests` 116, `GuildGameplayWindowTestTools` 165, `GuildLiveServerWindowTestTools` 441, `GuildLiveServerWindowTests` 383) | UI 클릭이 C_Attack을 보내지 않는다 | 고치지 않는다. 「마우스 클릭은 어디서도 공격하지 않는다」의 회귀 검사로 남는다. 포인터 차단 경로의 증명이 아니라는 점을 결과 기록에 적는다. |

새로 넣을 시험:

- `MapEntrySceneLifecycleTests.cs` 68–69의 실제 TCP Enter 공격 뒤, 쿨다운을 기다려 왼쪽 Ctrl로 두 번째 C_Attack을 본다.
- EditMode 정적 검사 하나. `.inputactions`를 읽어 Player/Attack의 Keyboard&Mouse 바인딩이 정확히 `<Keyboard>/leftCtrl`·`<Keyboard>/enter` 둘이고 `<Mouse>/` 경로가 없는지, UI/Click은 `<Mouse>/leftButton` 그대로인지 본다.

위 줄 번호는 `lead/facts-65a-02.json`(기준 origin/main `7bc75898`)이 앞 초안의 `facts-65a-01.json`(기준 `325777b7`)과 같음을 확인한 값이다. 비교한 키는 양성 대조·Swing 호출·음성 검사·키보드 공격 줄·`LocalPlayerInput` 공격 줄·client.md 문단·사용자에게 보이는 「클릭」 문구·live lane 표식이고 모두 같았다. 입력 자산 sha256도 같다.

TDD 순서: 선행 시험 작성자가 위 시험을 쓰고 구현 전 실패 원시를 남긴다. 쓰기 종료 뒤 구현자가 제품 3파일을 바꾸고 같은 명령의 통과 원시를 남긴다. 화면·에셋으로 시험할 수 없는 요구는 없다. 왼쪽 Ctrl 입력은 Input System 가짜 키보드로 시험한다.

## 하지 않을 것

- 서버, 패킷(C_Attack 그대로), Shared DLL, PDL.
- 다른 키: Enter, WASD·화살표 이동, Q·E 스킬(Dash 포함), I, 패드·조이스틱·XR·터치 바인딩.
- UI 맵 Click 바인딩.
- 마우스 조준이나 공격 방향(`TryFaceNearestTarget` 그대로), 키 재설정 UI, 화면 키 안내 문구. 지금 화면에 「좌클릭」 안내 문구는 없고 주석 1곳뿐이다.
- `GameplayPointerInput` 제거·변경, 터치 탭의 UI 차단 시험 추가. 둘 다 다음 계획 후보다.
- Unity 판본·패키지·ProjectSettings, Unity 단축키 설정, effect-active `Library` 처분.
- Art 70 A의 파일과 시험.

## 관찰 가능한 완료조건

1. `.inputactions`의 diff가 Attack Keyboard&Mouse 바인딩 path 한 줄이고, id·groups·`.meta`가 그대로다.
2. EditMode 전체와 영향 PlayMode 클래스(위 표의 가짜 peer 4클래스와 `MapEntrySceneLifecycleTests`)가 통과하고 결과 XML이 근거 폴더에 있다.
   - 예외 한 건: 고치지 않은 `GuildGameplayWindowTests.MenuTransitionAndOutsideTown_NoPurchases`는 구현 전에 이미 실패했다. 이 goal은 이 시험을 (d) 기존 실패로 기록만 한다(메인 `msg_812478372cdf`). 같은 명령에서 같은 실패면 이 조건을 막지 않는다. 다른 실패로 바뀌면 다시 분류한다.
3. 시험이 보이는 사실:
   - 왼쪽 Ctrl이 실제 TCP로 C_Attack 1건을 보낸다.
   - Enter 공격은 그대로다.
   - 월드의 마우스 왼쪽 클릭은 C_Attack 0건이다.
   - UI 클릭은 UI 동작만 하고 C_Attack 0건이다.
4. `InventoryServerIntegrationTests`는 7777 실서버 창이 있으면 1회 실행하고, 없으면 사유와 함께 미실행으로 적는다. live lane은 미실행으로 적는다.
5. 신규 독립 검증자의 강 등급 판정이 통과다.
6. 사용자 직접 확인(아래)에서 문제가 없다.
7. 사용자가 `병합 승인: PR<번호> head <40자>`를 제출한다.

## PR 경계와 검증

- PR 하나로 끝낸다. branch `feat/attack-key-left-ctrl-20261030`. PR base는 PR280 branch `docs/knight-dash-effect-closeout-20261030`이고, PR280 병합 뒤 main이 된다(위 표 「branch」).
- 검증 등급은 강이다.
  - 이유 1: 입력 경계에 닿는 위험 신호 구현이다.
  - 이유 2: 제품 2줄, 문서 1~2문장, 시험 6~7파일 약 40~70줄로 추정해 50줄 경계에 걸친다. 실제 diff 줄 수로 다시 판정하고, 애매하면 강을 유지한다.
- 강 등급의 「실제 경로 1회」는 Unity PlayMode의 실제 TCP 가짜 peer 실행으로 채운다. 실서버 실행은 완료조건 4를 따른다.
- R-7 비해당: 메인이 승인한 시범 목표가 아니다.
- 다음 PR(개선안 3)과 파일이 겹치면 개선안 3 branch를 이 branch 위에 쌓는다. 쌓는 순서는 메인이 정한다.

## 실행 창과 사용자 손

- Unity는 effect-active batchmode 하나만 쓴다. 선행 시험 1회(실패 원시), 구현 1~2회(통과 원시), 검증자 1회다. 창은 단계마다 메인에 「무엇·예상 시간·관문 측정 출력 경로」를 보내 배정받는다.
- 매번 저장 스크립트로 관문을 잰다: 물리·commit 여유 각 4 GiB 이상, Unity 0, Windows dotnet 0.
- 가짜 peer PlayMode 시험은 서버·DB가 필요 없다.
- 캡처: 화면이 바뀌지 않는 키 바인딩 변경이라 해당 없다.
- 사용자 확인은 지난번과 같은 방식이다. 리드가 Orca 터미널에 서버 하나(DB 없는 Legacy)를 띄우고 effect-active Editor를 GUI로 연다. 장면·Play·키는 사용자가 한다.
- 사용자 확인 항목:
  1. 왼쪽 Ctrl로 공격한다.
  2. 게임 월드의 마우스 왼쪽 클릭으로는 공격하지 않는다.
  3. 인벤토리(I) 버튼 클릭은 그대로 된다.
  4. Enter 공격이 그대로다.
  5. 움직이면서 왼쪽 Ctrl로 공격하고 Q·E(Dash)를 쓸 수 있다.
  6. Editor Play로 확인할 때: Ctrl을 누른 채 다른 키를 누르면 그 조합이 Unity Editor 단축키로 먼저 처리될 수 있다(예: Ctrl+P는 Play 중지). 이동 키를 먼저 누르고 Ctrl은 나중에 짧게 누르면 피할 수 있다. 빌드에서는 해당이 없다.

## 선행 시험 결과

- 작성자: 신규 `claude-opus-5-5` xhigh(대화 기록 model 값 그대로, backend unknown). task `task_e1a3e0e20262`, worker_done `msg_3187ec8d6869`. 보고서는 근거 폴더 `pretest-01/report.md`(sha256 `21d1643288b1893672ba635d5a95d4752a9c97b52406e254f6d49d57a7d9238d`)다. 리드 대조는 `lead/accept-pretest-01.json`이다. 결과 XML 세 개의 건수·실패 이름이 보고서와 같고, 제품 blob이 바뀌지 않았다.
- 작업 위치: PR280 head `0029c59b` 위에서 시험만 썼다. PR280 실사 검증자가 같은 worktree를 읽고 있어 branch를 바꾸지 못했고, 메인이 Unity 창을 이미 배정했기 때문이다(리드 선택). 두 commit의 차이는 문서 4파일이라 시험·제품 blob이 같다.
- 1회째 EditMode 전체: 1576건 중 실패 3. 셋 다 새 `PlayerAttackBindingTests`의 선행 실패이고, 실패 메시지는 요구 path와 실제 `<Mouse>/leftButton`의 차이다. 기존 시험 실패는 0이다.
- 2회째 PlayMode 5클래스: 38건 중 실패 10. 선행 실패 4건은 C_Attack 건수 차이다. Guild 6건은 `DAWNHOLDER_GUILD_VERIFY_CAPTURE_DIR`를 비우게 한 계약 탓으로, 시험 도구 Recorder가 멈춘 (b) 환경 누락이다.
- 3회째 Guild 클래스만, 캡처 폴더를 작성자 폴더로 지정: 6건 중 실패 4. 선행 실패 3건은 왼쪽 Ctrl 공격 대기에서 실패했다. 나머지 1건이 아래 (d)다. 같은 메인 창 안에서 리드가 정했다.
- (d) 기존 실패 `GuildGameplayWindowTests.MenuTransitionAndOutsideTown_NoPurchases`(고치지 않은 시험):
  - 관측: 802x451에서 일시정지 메뉴를 연 뒤 대장간 Enhance 좌표를 클릭했다. 그 자리 맨 위가 일시정지 메뉴의 `MainMenuButton`이었고, 연결이 끊긴 뒤 190행에서 NullReferenceException이 났다.
  - 이 goal의 변경과 무관하다. 이 파일에서 바뀐 곳은 `ScreenScenario`뿐이고, 이 시험은 공격 키를 쓰지 않는다.
  - 원인 분류는 시험 주인 Content가 PR-B 구현 창에서 맡는다(Content 리드 `msg_1edb48816548`). 이 goal은 기록만 한다(메인 `msg_812478372cdf`).
- 컴파일만 본 시험: `InventoryServerIntegrationTests`(실서버 필요), `GuildLiveServerWindowTests` live lane(Explicit). 둘 다 미실행이다.
- 과정 기록(메인 처분 `msg_812478372cdf`, 기록만):
  - 작성자 생존 신호가 300초를 넘은 구간 둘(473초, 312초). 리드가 정본 helper로 쟀다(`lead/settle-pretest-01.json`).
  - 작성자가 출력 없는 빈 인라인 node 명령 3회를 스스로 보고했다. 리드가 대화 기록에서 같은 3건을 확인했다.

## 다음 계획 후보

- 터치 탭 공격의 UI 위 포인터 차단을 덮는 시험이 없다. 마우스 공격이 없어지면 `GameplayPointerInput`의 차단은 터치에만 쓰인다. BACKLOG 등록은 종료 기록 때 한다.

## 결과

아직 없다.
