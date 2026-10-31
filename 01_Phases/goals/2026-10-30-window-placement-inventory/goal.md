# 창 배치 공용 부품과 인벤토리

## 재개 지점

### 현재 재개 카드 — 2026-11-01 종료

**다음 첫 일:** 없음. 이 goal은 [PR284](https://github.com/bass131/dawnholder-server/pull/284) 병합(merge commit `a0195c84`)으로 제품 작업을 마쳤고, [밝은 외관 goal](../2026-10-29-inventory-character-select-look/goal.md#재개-지점)의 PR5(종료 기록)와 함께 닫는다. 나머지 창의 공용 배치 이전은 아래 「다음 계획 후보」의 BACKLOG 행이다.

| 항목 | 현재 |
|---|---|
| 상태 | 종료. 독립 검증은 강 등급 PASS다. 조건마다의 상태와 남은 미확인은 아래 「완료조건 대조 — 종료」에 있다. |
| 병합 | PR284 head `41568d73`, merge commit `a0195c84d6b92632e38e619a175963b09b15b8f5`. |
| 리드 | `[Art 리드 Opus]`, Run `run_6ae742140ce9`. 밝은 외관 goal과 함께 메인 R-8로 닫는다. |
| Gardener | 밝은 외관 goal과 함께 점검했다. 보고는 [그 goal의 gardener.md](../2026-10-29-inventory-character-select-look/gardener.md)다. 이 goal에 온 반환은 후보 도착 링크(GR-01)와 생존 신호 정산(GR-03)이고, 아래에서 처리했다. |

### 이전 재개 카드 — 2026-10-30 구현 뒤

**다음 첫 일:** 구현이 끝났고 구현자를 정산했다(아래 「구현」). 다음은 구현 commit·push 뒤 merge-tree 대조, 독립 검증 계약, 완료조건 8의 캡처 순서다. merge-tree에서 충돌이 나오면 base를 옮기기 전에 메인에 먼저 알린다(`msg_580c64a10e66`, `msg_c81d46ef6b17`). 검증 Unity 창은 메인 대기열의 도착순이다.

| 항목 | 현재 |
|---|---|
| 승인 | 범위 초안 원문은 근거 폴더 `window-placement-goal-scope-draft-01.txt`(메인에 보낸 `msg_234cbf0759fa`)다. 메인이 사용자 결정 66·67 A·69 A·70 A와 대조해 어긋남 없음으로 받았다(`msg_a0f50907d201` 1번). |
| 리드 | `[Art 리드 Opus]`(claude-opus-5-5 xhigh). 회신 주소는 `run:run_6ae742140ce9`다. |
| branch | `feat/art-window-placement-20261030`이다. PR279 병합 commit `7bc75898`에서 만들었다. 같은 PR의 첫 commit은 [밝은 외관 goal](../2026-10-29-inventory-character-select-look/goal.md#pr2-사용자-플레이-확인과-병합--2026-10-30)의 PR2 결과 기록이다(문서만, 제품 검증 대상 아님). |
| 관계 | 같은 worktree의 [인벤토리·캐릭터 선택창 밝은 외관](../2026-10-29-inventory-character-select-look/goal.md#재개-지점) goal이 PR3·PR4를 남기고 열려 있다. 그 goal의 PR3는 이 goal의 branch 위에 쌓는다. 두 PR이 `UiWindowAppearanceContractTests.cs`를 함께 고칠 수 있어서다. |
| 실행 창 | 이미지 생성 0회다. Unity는 메인이 배정한 batchmode 창 네 번에서 돌렸다. 선행 시험은 U0(Unity 09:10:41Z~09:23:14Z)와 U1(09:51:18Z~10:00:46Z)이다. 구현은 S0(11:04:04Z~11:08:09Z)와 R/F(11:25:47Z~11:42:42Z)다. 서버 실행은 0이다. |
| 근거 폴더 | `.backups/verification/2026-10-29-inventory-character-select-look/`를 이어 쓴다. Git 제외 폴더다. |

## 적용 중인 사용자 결정

### 10-30 결정 70 A — 공용 창 배치 부품과 인벤토리부터 — msg_57a051a48b14

- 사용자 원문(2026-10-30 14:1x KST 메인 pane 제출, 메인 `msg_57a051a48b14` 전달): 「70 A로 하자」
- 결정 70 원문(보드, 메인 전달): 「70 - 창 배치를 공용 부품 하나로 모으면서, 이번엔 인벤토리만 옮길지 6개 창을 모두 옮길지」 → A 「공용 부품을 만들고 인벤토리부터」: 기본 자리·화면 맞춤 크기·화면 안 막기·끌기·자리 기억(켜 둔 동안, 69 A)을 맡는 공용 배치 컴포넌트 하나를 만든다. 이번 PR에서는 인벤토리만 자기 배치 코드를 지우고 이 부품으로 옮긴다(390×720, 기본 자리 화면 왼쪽). 나머지 5개 창은 시연 뒤 후보로, 옮길 때 각자 배치 코드를 지우고 기본 자리 값만 넘긴다. 보상 창 자리 규칙 수정과 50 B 대체는 앞 초안 1 A 그대로다.
- 메인 처분(같은 메시지 1번, 사용자 결정 아님)으로 함께 확정: 66(비율 1.3:2.4 → 390×720)과 50 B 대체, 보상 창 자리 규칙 수정(Content 파일, Content 리드가 다시 열릴 때 사후 확인), 67 A(PR279 먼저 병합, 이 작업은 새 PR), 69 A.
- 메인 처분(`msg_c10e89706c8c`): 부품은 6개 창 기준으로 설계하고 11-06 전엔 인벤토리만 옮긴다. Content 쓰기는 보상 창 규칙 하나로 둔다. 시간은 약 하루(미측정)로 받는다.
- 원문 근거는 근거 폴더 `lead-wait-182.json`·`lead-wait-183.json`이다.

### 10-30 결정 68 C·69 A — 창 끌기 범위 질문과 자리 기억 — msg_b67ebe349cf3

- 사용자 원문(2026-10-30 14:0x KST 메인 pane 제출, 메인 `msg_b67ebe349cf3` 전달): 「대시보드 결정 응답: 1) 68 - 창 끌기와 인벤토리 비율(1.3:2.4)을 인벤토리만 먼저 할지, 떠 있는 창 6개 전부 할지 → C 코멘트 「UI를 각각 만들어서 각각 수정을 해야하는구나? 추상화 계층으로 상속구조랑 컴포넌트 구조가 아니라?」 · 2) 69 - 끌어서 옮긴 창 자리를 언제까지 기억할지 → A 게임을 켜 둔 동안만」
- 69 A 효과: 맵 이동과 I로 닫았다 열기에도 자리가 남는다. 게임을 다시 켜면 기본 자리다. PlayerPrefs와 파일에 쓰지 않는다.
- 68 C의 질문은 결정 70으로 다시 물었고, 70 A가 답이다.
- 원문 근거는 근거 폴더 `lead-wait-181.json`이다.

### 10-30 결정 66 C·67 A — 인벤토리 비율과 창 끌기 제안 — msg_252b6a8847f4

- 원문은 [밝은 외관 goal의 67 A 절](../2026-10-29-inventory-character-select-look/goal.md#10-30-결정-67-a--pr279-먼저-병합-인벤토리-비율은-새-pr--msg_252b6a8847f4)에 있다. 이 goal의 출발점인 사용자 피드백 「비율좀 수정할까, 가로 1.3 : 세로 2.4 로」와 66 C 코멘트 「…드래그해서 위치 옮기게 가능하게 하면 되는거 아니야?」가 그 절에 있다.
- 효과: 1.3:2.4를 390×720으로 받는다. PR279를 먼저 병합했고, 이 작업은 새 PR이다.

### 10-30 사용자 지시 — GUI 전면 최신화와 끝까지 진행 — msg_0dfd9e52932d

- 원문은 [밝은 외관 goal의 해당 절](../2026-10-29-inventory-character-select-look/goal.md#10-30-사용자-지시--gui-전면-최신화와-끝까지-진행--msg_0dfd9e52932d)에 있다.
- 이 goal에 닿는 메인 결정(사용자 결정 아님): 병합은 마지막에 한 번에 한다. 같은 파일을 겹쳐 고치는 PR은 앞 PR branch 위에 쌓는다. 사용자 캡처 확인과 직접 플레이는 마지막에 한 번에 한다. 그래서 이 PR은 끝에 사용자가 볼 캡처와 플레이 확인 항목 몇 줄을 준비해 둔다.

### 10-27 결정 4A — 리드·구현자 Claude

- 리드·구현자·선행 시험 작성자는 신규 `claude --model claude-opus-5-5 --effort xhigh`이고, 독립 검증은 신규 `codex --model gpt-6-astra -c model_reasoning_effort=xhigh --disable fast_mode`다. 정본은 저장소 `CLAUDE.md`의 「결정 4A 적용 기간의 Claude 리드·구현자」 절이다.

## 만들 것

- 공용 창 배치 부품 하나(`UiWindowPlacement`)를 만든다. 다섯 가지를 맡는다.
  - 기본 자리: 창이 넘긴 「기본 영역」 안에 정한 위치(왼쪽 위 또는 가운데)로 맞춰 놓는다.
  - 화면 맞춤 크기: min(최대 배율, 영역 너비/창 너비, 영역 높이/창 높이)이고 최소 0.1이다.
  - 화면 안 막기: 창 전체가 캔버스 여백 안에 남게 당긴다.
  - 끌기: 제목 띠 위에 보이지 않는 잡는 칸을 둔다(sprite 없음·alpha 0). 버튼 위에서는 버튼이 먼저다.
  - 자리 기억(69 A): 옮긴 거리를 창 이름별로 게임을 켜 둔 동안만 기억한다.
- 해상도가 바뀌면 옮긴 거리는 그대로 두고 다시 화면 안으로 당긴다.
- 부품은 각 창 캔버스의 자기 좌표로 계산한다. 지금 창들은 1920×1080 기준, 960×540 기준, 기본 CanvasScaler(배율 없음)를 섞어 쓴다(근거 폴더 `window-placement-scope-01.txt` 2절). 부품은 6개 창의 자리 규칙을 모두 담을 수 있게 설계한다.
- 부착은 열기 연출(`UiWindowOpenEffect.Attach`)과 같은 방식이다. 상속 구조는 쓰지 않는다. 6개 창 클래스는 서로 공통 부모가 없다(같은 파일 4·5절).
- 인벤토리를 부품으로 옮긴다.
  - 바깥 틀은 390×720(13:24, 결정 66)이다. 2열 4행 8칸은 그대로다.
  - 칸 안은 위에 아이콘, 가운데에 「이름 ×수량」, 아래에 「사용」을 둔다.
  - 위에 제목 띠를 두고 새로고침은 위 오른쪽에 그대로 둔다. 재화 줄은 아래에 그대로 둔다. 동전·재화와 사용 결과 글은 두 줄로 나눈다.
  - 기본 자리는 화면 왼쪽이다. 왼쪽 여백 12, 위는 캐릭터 상태·파티 정보의 실제 아래 끝 + 12, 아래는 화면 아래 + 12다. 880×531 마을 계산으로 1.0092배이고, 스킬칸·골드 띠와 가로로 떨어진다(`window-drag-scope-01.txt` 6절).
  - `InventoryPanelView`에는 자기 배율·자리 계산이 남지 않는다(지금 Resize 139~158, 20줄). Resize 본문은 부품을 부르는 한 줄이 된다.
- 보상 창은 열린 인벤토리의 아래 끝을 읽지 않게 바꾼다(`DungeonClearRewardWindow.cs:89`·`:94` 근처). 캐릭터 상태 아래 끝을 읽는 규칙은 그대로다. 그러면 기본 자리끼리 겹치지 않고, I를 눌러도 「확인」이 움직이지 않는다. 세로 긴 인벤토리를 그대로 두면 보상 창이 위 가운데에서 0.2612배, 왼쪽 칸에서 최소 0.1배로 줄어든다(`window-drag-scope-01.txt` 6절).

## 건드릴 곳과 소유

- Art가 쓴다:
  - 새 파일 `03_Client/Assets/Scripts/UI/UiWindowPlacement.cs`와 Unity가 만든 `.meta`다. `.meta`는 손으로 만들지 않는다.
  - `InventoryPanelView.cs`의 배치·크기 계산·칸 안 배치·끌기 연결이다.
  - 시험: `UiWindowAppearanceContractTests.cs`의 인벤토리 고정값과 새 시험 파일이다.
- Content 파일이고 메인 처분으로 이번에 쓴다: `DungeonClearRewardWindow.cs`의 인벤토리 아래 끝 읽기 제거다. 사후 확인은 Content 리드에게 받는다. 메인이 Content 리드를 새로 열었으므로(`msg_0dfd9e52932d`) 그 리드에게 직접 알린다.
- 문서: `00_Document/domains/client.md:50`(보상 창 자리 규칙 문장)과 `:38`(InventoryPanelView 설명)을 실제 동작에 맞춘다.
- 바꾸지 않는다:
  - `InventoryPanel.cs`(Content)다. `:81`은 지금처럼 매 프레임 `_view?.Resize()`를 부르고, 이 호출이 부품으로 넘어간다.
  - Slot 경로·이름(Inventory/Slot1~8/Item, Use, Currency, Synchronization), 「이름 ×수량」 형식, richText false다.
  - 다른 창 5개의 파일, HUD 파일, 입력 파일(`LocalPlayerInput.cs`·입력 액션)이다.
- 시험 변경 목록:
  - 기대값만 바꾼다(50 B 대체): `UiWindowAppearanceContractTests.cs`의 패널 크기 `:549`, 새로고침 입력 칸 `:554`, 칸·사용 칸 사각형 표 `:163`~`:176`(대조 `:557`·`:558`)이다(`inventory-ratio-scope-01.txt` 3절). 같은 시험의 다른 단정은 줄 단위로 그대로 둔다. 줄 번호는 PR279 병합 전 기준이라 선행 시험 단계에서 다시 잰다.
  - 그대로 통과해야 하는 보존 시험(diff 0):
    - `InventorySceneLifecycleTests` `:260`~`:262`: HUD 겹침 0, 버튼 위 HUD raycast 0, 패널 전체가 화면 안이다. 기본 자리에 대해 본다.
    - `DungeonClearRewardOverlapSceneTests` `:93`·`:165`와 `DungeonClearRewardPresentationLifecycleTests` `:178`: 보상 창 겹침 0이다.
    - 재화 줄이 칸 아래(`InventoryPanelViewAppearanceTests` `:127`), 글자 넘침·글리프 누락 판정, 장식 raycast 금지(`UiWindowAppearanceContractTests` `:1181`·`:1211`, 보이지 않는 칸 예외)다.
  - 새로 더한다:
    - 부품 EditMode 시험: 맞춤 크기, 기본 영역, 화면 안 막기, 캔버스 배율 방식 세 가지별 끌기 거리 변환, 창을 다시 만들어도 남는 기억, 해상도 변경이다.
    - PlayMode 시험: 마을에서 제목 띠 끌기, 화면 끝에서 멈춤, 맵 이동 뒤 유지, 잡는 칸을 눌러도 공격 0, 새로고침·사용 버튼 동작이다.
  - 칸 안 배치를 고정한 다른 외관 시험이 더 있는지는 선행 시험 단계에서 전수로 찾고 목록에 더한다(지금 미확인).

## 하지 않을 것

- 나머지 5개 창(길드 현황·대장간·던전 보상·파티 초대·NPC 대화)을 부품으로 옮기지 않는다. 보상 창은 규칙 한 곳만 고친다.
- HUD 끌기, 창 크기를 사용자가 바꾸기, 디스크 저장(PlayerPrefs·파일)은 하지 않는다.
- 새 그림과 이미지 생성은 하지 않는다(0회). 창·제목 띠·칸·버튼·구분선이 모두 PR279의 9-slice 조각이다.
- 기능·서버·패킷·아이템 규칙·입력 방식을 바꾸지 않는다. 공격 키 변경(결정 65 A)은 Effect PR 몫이다.
- 캐릭터 선택창, 석재 미추적 파일 5개, 전역 설정은 건드리지 않는다.
- 다른 창의 밝은 외관(GUI 전면 최신화)은 이 goal이 아니라 밝은 외관 goal의 PR3와 그 뒤 PR에서 한다.

## 관찰 가능한 완료조건

1. 인벤토리 바깥 틀이 390×720이고 2열 4행 8칸이다. 칸 안은 위 아이콘·가운데 「이름 ×수량」·아래 「사용」이다. 제목 띠와 위 오른쪽 새로고침, 아래 재화 줄이 있다.
2. 기본 자리는 화면 왼쪽이다. 1920×1080·1280×720과 시험 해상도에서 HUD 겹침 0, 보상 창 겹침 0, 화면 안이 보존 시험 그대로 통과한다.
3. 제목 띠를 끌면 창이 움직이고 창 전체가 화면 안에 남는다. 해상도 변경 때 다시 화면 안으로 들어온다. 맵 이동과 I로 닫았다 열기 뒤에도 자리가 남고, 다시 켜면 기본 자리다. PlayerPrefs 쓰기는 0이다.
4. 잡는 칸을 누르거나 끌어도 공격 0이다. 새로고침·사용 버튼은 지금처럼 동작한다.
5. 보상 창은 인벤토리 자리를 읽지 않는다. I로 열고 닫아도 「확인」 자리가 같다. 보상 창 시험의 기대값 변경은 0이다.
6. `InventoryPanelView`에 자기 배율·자리 계산이 남지 않는다. 부품 EditMode 시험이 통과한다.
7. EditMode·PlayMode 전체가 통과한다. 바꾼 기대값은 위 목록뿐이다.
8. 1920×1080·1280×720 캡처(마을에서 연 인벤토리, 보상 창과 함께 연 장면)와 플레이 확인 항목을 준비하고, 사용자가 마지막에 메인을 통해 한 번에 확인한다. 신규 Astra 검증자가 강 등급 PASS를 낸다.

## PR 경계와 작업 방식

- 제품 PR 하나다. 이 goal 문서와 밝은 외관 goal의 PR2 결과 기록 commit을 같은 PR에 넣는다. 종료 기록 PR은 R-8 순서(결과 기록 → Gardener → 종료 기록)로 따로 한다.
- 검증 등급은 강이다. 제품 배치 코드, 보존 시험 기대값, Content 파일 한 곳이 함께 바뀐다. 첫 commit(밝은 외관 goal 문서)은 문서 실사만 받는다.
- 세션(결정 4A): 신규 Opus 선행 시험 작성자 1 → 신규 Opus 구현자 1 → 신규 Astra 검증자 1이다. 작업 하나 뒤 정산·종료한다. 계약마다 `unity-protection-rules-v1.md` 원문과 「허용 명령의 값도 보고·판정에 쓰려면 저장 스크립트 안에서 잰다」를 넣는다.
- TDD: 선행 시험은 완료조건 1~6을 고정하고, 구현 전 실패 원시를 근거 폴더에 둔다.
- 병합 순서: 이 PR이 먼저다. 밝은 외관 goal의 PR3는 이 branch 위에 쌓고(base를 이 branch로), 병합은 마지막에 메인이 순서대로 한다.
- R-7 시범은 해당 없음이다.

## 실행 창과 사용자 손

- Unity: 한 번에 하나만 띄운다. 창을 열기 직전에 메인에 「무엇·예상 시간」을 보내 시각을 받는다. 매 창 직전에 메모리 관문(물리·commit 여유 각 4 GiB, Unity·Windows dotnet 0)을 저장 스크립트로 잰다.
- batch EditMode, PlayMode(겹침·끌기·렌더), 1920×1080·1280×720 캡처를 돌린다.
- 사용자 손: 마지막에 캡처 확인과 직접 플레이(끌기·화면 끝 멈춤·맵 이동 뒤 유지)를 한 번에 한다. 메인이 묶는다.

## 일정

| 날짜 | 할 일 |
|---|---|
| 10-30 | 범위 승인, goal commit, 선행 시험, 구현 시작 |
| 10-31 | 구현·독립 검증, 캡처와 플레이 확인 항목 준비 |
| 그 뒤 | 밝은 외관 goal의 PR3와 그 뒤 PR을 이 branch 위에 쌓는다. 사용자 확인과 병합은 마지막에 한 번에 한다. |

- 시간은 선행 시험 약 1.5시간, 구현 약 3시간, 검증 약 2~3시간이다(미측정). 납기를 보장하지 않는다.

## 다음 계획 후보

- 나머지 창의 공용 배치 부품 이전(시연 뒤, 소유 파트별): BACKLOG `guild-windows-shared-placement`, `dungeon-reward-window-shared-placement`, `party-invite-popup-shared-placement`, `npc-dialog-panel-shared-placement`
- 시험 메시지 속 제품 줄 번호 정리: `UiWindowAppearanceContractTests.cs` :161 주석과 :540~:576 메시지가 `InventoryPanelView.cs:<줄>`을 적는다. 대부분은 이 goal 전 기준 commit에서도 이미 그 줄을 가리키지 않았다(근거 폴더 `placement-impl-01/old-view-lines-02.txt`). 완료조건을 막지 않아 이번 PR에서 고치지 않았다: BACKLOG `ui-appearance-test-message-line-refs`
- 끈 인벤토리를 보상 창 위에 두면 보상 창이 위에 그려지는 겹침(검증 판정 설계 관찰): BACKLOG `dragged-inventory-reward-overlap`

## 진척 단계

- [x] 범위 승인
- [x] goal commit
- [x] 선행 시험
- [x] 구현
- [x] 독립 검증
- [x] 사용자 확인(마지막에 한 번에)
- [x] 제품 PR 병합
- [x] 종료 기록

## 단계 결과와 근거

근거 폴더는 `.backups/verification/2026-10-29-inventory-character-select-look/`다. Git 제외 폴더라 다른 checkout에도 있다고 가정하지 않는다.

### 범위 초안과 승인 — 2026-10-30

- 사용자 피드백 뒤 리드가 범위 초안을 세 번 보냈다. 비율 초안 `msg_b96c75dc1325`, 끌기 초안 `msg_42bea42cc8d2`, 70 A 재추정 `msg_5e41659e51b5`다. 값은 저장 출력 `inventory-ratio-scope-01.txt`·`-02.txt`, `window-drag-scope-01.txt`, `window-placement-scope-01.txt`에서 옮겼다.
- 첫 초안의 「640×480에서 보상 창과 겹친다」와 「Content 파일 변경 0」은 틀렸다. 보상 창은 열린 인벤토리의 아래 끝을 읽어 그 아래에 놓이므로 겹치지 않고 작아진다. 리드가 두 번째 초안에서 정정했다.
- goal 범위 초안은 `window-placement-goal-scope-draft-01.txt`(`msg_234cbf0759fa`)다. 메인이 대조 통과와 착수 허가를 줬다(`msg_a0f50907d201`).

### 선행 시험 — RED

- 작성자: 신규 `[Art 구현 Opus]`(`claude --model claude-opus-5-5 --effort xhigh`), pane term_c0f5ecac-03c1-4081-9989-a376304e3e36, task_498f737faf6d / ctx_cd03d0978dcb다. 기동 근거는 `placement-red-launch-record-01.md`다.
- 계약: `placement-red-contract-v1.md`(SHA256 `90706acb…`, `placement-red-contract-v1.receipt.txt`), 기준 commit 7c94be35다. 목록 밖 기존 단정 세 줄의 처분 원문(메인 `msg_9cbc405339aa` 1항·2항)과 `unity-protection-rules-v1.md`를 부록으로 넣었다.
- 작성자 보고: 근거 폴더 `placement-red-01/report.md`(SHA256 `e8a0574c…`, worker_done `msg_daf004fcc8c0`, 10:25:41Z, succeeded)다. 아래 수는 그 보고와 보고가 가리키는 `placement-red-01/raw/` 저장 출력에서 옮겼다.

#### U0 — 기준 commit 그대로

- 메인 배정 `msg_7e717e5cd24b`(창 09:06:34Z~09:45:00Z). 리드 관문 `placement-red-u0-window-gate-01.txt`(09:07:59Z, 통과). 작성자에게 `msg_581d99e9cd25`.
- 작성자 「Unity 사용 끝」 `msg_53bf3c3f873c`: Unity 09:10:41Z~09:23:14Z, 재시도 0이다. EditMode 1571건 통과 1571·실패 0·건너뜀 0, PlayMode 185건 통과 143·실패 23·건너뜀 19다(`placement-red-01/raw/u0-*-results-table.stdout.txt`). 앞 goal PR2 재검증과 같은 수다.
- PlayMode 실패 23과 건너뜀 19는 모두 (b) 환경으로 분류됐다. 캡처 폴더를 주는 runner 없음, batchmode의 GameView 없음, runner permit 없음, 서버 필요다(보고 6절).
- 리드 반환 점검 `check-post-u0-drift-1030-01.sh` → `post-u0-drift-check-1030-01.txt`(09:26:44Z): Unity·dotnet·Burst 0, SENTIS diff = 31e7aaf1…(작성자 두 원시도 같음), MinimapRT 변화 0이다. 메인 반환 보고 `msg_1c50f3cefab9`, 메인 수락 `msg_3ccd6911fb4e`.
- U1 정식 요청 `msg_f24168ab38cc`, 메인 「대기열 1번(Effect 반환 뒤)」 `msg_65f4f6a8582a`.

#### U1 — 바꾼 시험 3 + 새 시험 2

- 메인 배정 `msg_ea4001c0c547`(09:48:32Z, 창 지금부터~10:25:00Z). 리드 관문 `placement-red-u1-window-gate-01.txt`(09:48:57Z, 통과). 작성자에게 `msg_68a43add456e`(09:49:34Z), 메인 보고 `to-main-u1-window-opened-1030-01.receipt.json`.
- 작성자 pane이 빈 프롬프트라 터미널 안내를 1회 넣었다(`placement-red-writer-nudge-u1-open-01.json`). 턴 시작은 `placement-red-writer-screen-u1-open-03.json`에서 확인했다.
- 작성자 「Unity 시작」 `msg_290b01c48944`(09:51:08Z): 설정 `placement-red-01/harness/config-u1.json`, EditMode 전체 → PlayMode 전체, 시도 1회씩이다.
- 작성자 「Unity 사용 끝」 `msg_3049bde9daad`(10:01:48Z): Unity EditMode 09:51:18Z~09:53:58Z, PlayMode 09:54:26Z~10:00:46Z, 재시도 0이다.
  - EditMode 1576건 통과 1568·실패 8·건너뜀 0, PlayMode 194건 통과 144·실패 31·건너뜀 19다.
  - 계약의 「구현 전 예상」 18건이 모두 맞았다(`placement-red-01/raw/expect-table-*-01.stdout.txt`). 새로 실패한 16건은 모두 예상 실패이고, 예상 통과 2건은 통과했다.
  - U0 대비 그 밖에 결과가 바뀐 시험은 0이다(`raw/results-table-*-u0u1-01.stdout.txt`). 두 로그의 컴파일 오류는 0이다.
  - Unity가 새 시험 두 파일의 `.meta`를 만들었다.
- 리드 반환 점검 `check-post-u1-drift-1030-01.sh` → `post-u1-drift-check-1030-01.txt`(10:02:02Z): Unity·dotnet·Burst 0, SENTIS diff = 31e7aaf1…(작성자 U1 원시 두 개도 같음), MinimapRT 변화 0이다. 메인 반환 보고 `msg_f04a8a4a0216`, 메인 수락 `msg_78b6ef01487c`.
- 부품 시험 `UiWindowPlacementTests.cs`는 계약대로 U1 뒤에 놓았다(SHA256 `7d4ff535…`, `.meta` 없음, `raw/place-part-test-01.stdout.txt`). stub이 생기기 전까지 EditMode 시험 어셈블리가 컴파일되지 않는다. 그래서 구현자는 stub을 먼저 쓰고 그 첫 실행의 실패 원시를 남긴다.

#### 바꾼 기존 단정 — 메인 `msg_9cbc405339aa` 조건 대조

- 줄 단위 표는 보고 7절이다. 파일:줄, 시험 이름, 이전 단정, 새 단정, 요구 출처(완료조건 4, `msg_9cbc405339aa`)가 9행이다. 원시 `raw/u1-prep-diff-01.stdout.txt`의 hunk 9개와 하나씩 맞고 표 밖 줄 변경은 0이다.
- 바꾸지 않은 줄: helper :1301, 보상 창 :586, 대조 :848, `UiWindowLiveCaptureTests` :1060·:1181·:1211, 비교 반복문 :557·:558은 diff에 없다.
- 새 단정의 대상은 잡는 칸과 겹치는 제목 띠 안의 모든 버튼이다. 버튼마다 중심 포인터의 첫 hit가 그 버튼인지 보고, 그 앞에 「대상 버튼 1개 이상」 전제를 두어 대상 0 통과를 막는다(Render :445 시험).
- 같은 명령 전후의 단정 수(`raw/count-asserts-u1-prep-01.stdout.txt`): Contract :512 시험 28 → 31, :171 시험 1 → 1, Render :369 시험 21 → 25다. 통과·실패 전후는 U0·U1 원시에 있다.
- 2항의 「잡는 칸을 누르거나 끌어도 공격 0」 PlayMode 시험은 마우스 binding에 기대지 않는다. 키보드 공격 +1 양성 대조군을 같은 파일에 두었다(`KeyboardAttack_WithThePointerOnTheGrabArea_StillSwingsOnce`).
- T1 전수 탐색(보고 8절, `raw/t1-layout-search-01.stdout.txt`): 기준 commit 시험 파일 116개에서 표 밖의 옛 배치 상수 단정은 0이다. 그래서 작성자의 blocking ask는 없었다.

#### 계약과 다른 점과 리드 판단

- T5 맵 이동은 포털 걷기 대신 대본 서버의 `S_MapTransition`으로 한다. 리드가 작성자 질문 `msg_787ee778b870`에 승인했다(`msg_9d17df8ce52c`). 받는다.
- harness 보완: 작성자가 바꾸는 기존 시험 세 개가 계약 부록 A 입력에 들어 있어, 작업 트리 SHA로 재면 U1 진입 관문이 반드시 실패했다. 그 세 행만 기준 commit 내용으로 대조하고 작업 트리 변화는 작업 트리 관문이 맡게 했다(보고 3절). 받는다. 구현 계약은 선행 시험 commit을 기준으로 잡아 이 충돌이 생기지 않는다.
- T4 (나)를 T5 끌기 시험과 한 시험으로 합쳤다(`TitleBarDrag_MovesThePanelByTheDrag_AndStartsNoAttackOnAnyFrame`). 같은 끌기 동안 매 프레임 공격 0과 끈 만큼 움직임을 함께 본다. 받는다.
- 부품 시험의 끌기 사건에 uGUI가 실제로 보내는 `initializePotentialDrag`를 더했다. 받는다.
- 시험 주석·메시지의 옛 제품 줄 번호(`UiWindowAppearanceContractTests.cs`:161의 `InventoryPanelView.cs:68-76` 등)는 구현 뒤 달라진다. 구현자는 시험을 고치지 않는다. 구현 뒤 실제 줄을 본 다음 같은 PR 안에서 처리 방법을 정한다.
- 남은 위험(보고 16절): U1에서 잡는 칸이 없어 여러 단정이 전제에서 멈췄다. `IsOverUi` reflection, 키보드 공격 control 찾기, 대본 맵 이동 뒤 재조회 같은 시험 쪽 도구는 구현 뒤 첫 실행에서 처음 돈다. 시험 결함이 드러나면 구현자가 리드에게 올린다. 크리티컬은 아니다. 컴파일과 앞 단계 도구(마을 준비, I로 열기, 보상 창 열기)는 U1에서 돌았다.

#### 정산

- 작성자 pane을 닫았다(`placement-red-writer-pane-close-01.json`, ptyKilled true). 닫기 전 작성자 쪽 orca 대기 0을 저장 스크립트로 확인했다(`placement-red-writer-shells-before-close-02.txt`, `placement-red-writer-ancestors-before-close-03.txt`).
- SENTIS 한 줄은 계약 :286과 메인 수락(`msg_3ccd6911fb4e`, `msg_78b6ef01487c`)대로 정산 뒤 리드가 복원했다. `restore-projectsettings-sentis.sh` → `placement-red-sentis-restore-1030-01.txt`(10:30:18Z)이고, 복원 뒤 파일이 HEAD blob과 같다. 메인 알림 `msg_063e16658dc1`.

### 구현

- 기준 commit: 선행 시험 commit `eddd61c5`(push 완료, `placement-impl-base-01.txt`).
- 계약: `placement-impl-contract-v1-02.md`(SHA256 `6a7d0305…`, `placement-impl-contract-v1-02.receipt.txt`). 입력 경로 62개는 bad 0이다(`placement-impl-contract-v1-paths-check-02.txt`). 인용 message ID 4개는 모두 부록 안에 있다(`placement-impl-contract-v1-02-msg-ids-01.txt`).
  - 먼저 조립한 `placement-impl-contract-v1.md`는 보내지 않았다. 부록 D2(선행 시험 계약)의 작성자용 절이 구현자에게 적용되지 않는다는 한 문장을 더해 `-02`로 다시 조립했다.
  - 첫 경로 확인은 장면 경로 오기로 bad 1이었다(`-paths-check-01.txt`). 고친 목록을 새 이름으로 다시 쟀다.
- 계약 안 리드 결정:
  - 인벤토리 기본 자리의 위 끝은 캐릭터 상태와 파티 정보 중 더 낮은 아래 끝 + 12다. 파티 정보는 숨겨도 켜진 채라 파티 출입으로 창이 뛰지 않는다.
  - 끌기 handler는 잡는 칸 자신에 둔다. 새로고침은 제목 띠가 아니라 패널의 자식이라, 그래야 새로고침에서 시작한 끌기가 창을 옮기지 않는다.
  - 시험 결함이 의심되면 구현자는 시험을 고치지 않고 리드에게 묻는다. 시험 메시지 안 옛 제품 줄 번호는 구현 뒤 같은 PR 안에서 리드가 처리를 정한다.
- 구현자: 신규 `[Art 구현 Opus]`(`claude --model claude-opus-5-5 --effort xhigh`), pane term_353e4b42-f33a-4b68-ab39-b105da3fb7f5, task_37fa6e136b4f / ctx_65173e123a30다. 기동 근거는 `placement-impl-launch-record-01.md`다. 첫 화면 선택창은 없었고, 대화 기록 model 값은 claude-opus-5-5, backend 실제 모델은 unknown이다.
- 메인 보고 `msg_a141a805b90b`, 메인 수락 `msg_d8591ee21dd6`(10:46:57Z). 메인은 계약·기동 기록·리드 결정 셋을 받았다.
- 이 절의 리드 발신 msg ID와 시각은 `list-impl-receipt-ids-1030-01.txt`에서 옮겼다.

#### S0 — stub 상태의 부품 시험

- 구현자 「S0 준비됨」 `msg_08eb575b010f`(10:57:17Z): 제품 쓰기는 stub `UiWindowPlacement.cs` 하나였다. 리드 원천 대조 `check-s0-ready-1030-01.txt`·`check-s0-ready-config-1030-02.txt`에서 보고와 같았다.
- 정식 요청 `msg_76db377d8062`(10:58:18Z), 메인 회신 `msg_8360efcf4074`(Content u3c 뒤 1번), 배정 `msg_e847cae8d699`(11:01:40Z, 시한 11:20:00Z)다.
- 리드 관문 `placement-impl-s0-window-gate-01.txt`(11:01:53Z, 통과). 구현자에게 `msg_53c7a6cd0a4e`, 메인에 `msg_e9367bfc68a1`을 보냈다.
- 구현자 「Unity 사용 끝」 `msg_db774c4e4e36`: Unity 11:04:04Z~11:08:09Z, EditMode 1회, 재시도 0이다.
- 리드 대조 `check-s0-results-1030-01.txt`: 부품 시험 22건이 통과 0·실패 22·건너뜀 0이다. 22건 모두 `NotImplementedException`이고, fixture 밖 0, 컴파일 오류 0이다.
- 구현자 참고: `ForgetMovedPositions_RunsOnLoadBeforeTheFirstScene`은 본문이 아니라 TearDown의 `ForgetMovedPositions` 호출에서 실패했다. 본문은 속성만 보므로 구현 뒤 통과는 속성 확인만 뜻한다. 독립 검증 계약에 적는다.
- 반환 점검 `post-s0-drift-check-1030-01.txt`(11:10:18Z): Unity·dotnet·Burst 0, SENTIS diff = 31e7aaf1…, MinimapRT 변화 0이다. 메인 반환 보고 `msg_57fe656a3d18`.

#### R·F — 구현 뒤 거른 실행과 전체 실행

- 구현자 「R 준비됨」 `msg_a6aaf4efadf3`(11:20:15Z): 새 제품 파일 넷(`UiWindowPlacement`·`UiWindowPlacementGeometry`·`UiWindowAreas`·`UiWindowGrabArea`)과 추적 셋(인벤토리 view, 보상 창, client.md)이다.
- 리드 대조: SHA256이 보고와 같았다(`check-r-ready-1030-01.txt`). 줄 끝은 LF이고 BOM이 없다(`check-line-endings-1030-01.txt`). diff는 커밋 기준(`eddd61c5..af1489fb`) 세 파일 +31/−52다(`check-r-ready-numstat-1030-02.txt`). 구현자 보고의 +30/−51은 틀린 값이라 보고서에는 저장 출력 값을 쓰게 했다. 리드 결정 셋과 diff의 어긋남은 0이다.
  - `InventoryPanelView.cs`는 R 준비 뒤 R2 수정으로 바뀌었다. 그래서 커밋 blob의 SHA256이 R 준비 때 값과 다르다(같은 출력). 나머지 두 파일은 같다.
- 정식 요청 `msg_b75a6f87e4af`(40분), 배정 `msg_a634656ec261`(11:22:58Z, 시한 12:05:00Z)이다. 배정 조건은 「계획 2회를 넘길 때마다 리드가 그 이유를 한 줄로 남겨라」였다.
- 리드 관문 `placement-impl-rf-window-gate-01.txt`(11:23:20Z, 통과). 구현자에게 `msg_f13784f5c35e`를 보냈다.
- 회차별 결과(시각은 `check-rf-run-times-1030-01.txt`, 수는 `check-rf-results-1030-02.txt`. 둘 다 원시 result.json·results.xml에서 옮겼다):

  | 회차 | 시작–끝(UTC) | 범위 | 통과/실패/건너뜀 |
  |---|---|---|---|
  | R1 EditMode | 11:25:47–11:27:31 | 10개 fixture | 132/0/0 |
  | R2 PlayMode | 11:27:49–11:29:39 | 11개 fixture | 49/3/2 |
  | R3 EditMode | 11:30:23–11:31:18 | 고친 영역 | 72/0/0 |
  | R4 PlayMode | 11:31:38–11:33:03 | 고친 영역 | 18/0/0 |
  | F EditMode | 11:33:17–11:35:56 | 전체 | 1598/0/0 |
  | F PlayMode | 11:36:13–11:42:42 | 전체 | 152/23/19 |

- 계획 2회 초과 이유(구현자 `msg_c26b7cf3acc1`, 11:30:14Z, R3 직전): R2 실패 3건(`InventorySceneLifecycleTests`)을 고친 뒤 같은 영역을 다시 돌렸다. 칸 글 높이 30이 한국어 대체 글꼴 한 줄 34.4보다 작았다. 제품 결함이고 시험 결함이 아니다. 수정은 인벤토리 view의 칸 안 값 넷과 주석 한 줄이다(`check-impl-final-1030-01.txt.tracked.diff`).
- F와 기준(`check-rf-results-1030-02.txt`): U1 대비 새 실패는 0이다. 새 통과는 EditMode 8, PlayMode 8(인벤토리 창 배치 장면 시험)이다. F PlayMode 실패 23건은 U0 실패 23건과 같은 집합이다. 그래서 F2는 돌리지 않았다.
- 메인 회신 `msg_c81d46ef6b17`(메인 결정): 「이 23건 집합 동일성은 검증자가 원시 XML로 다시 대조하게 하라. 메인은 판정 때 R-2로 표본을 본다.」
- 반환 점검 `post-rf-drift-check-1030-01.txt`(11:44:55Z): Unity 계열·Windows dotnet·Burst dotnet 0이다. SENTIS는 알려진 한 줄이고 회차별 원시 7개도 같다. MinimapRT는 HEAD와 같다. 메인 반환 보고 `msg_1960a01df61d`.
- 독립 검증 계약에 넣을 관찰:
  - R1 EditMode는 132건 모두 통과했지만, 렌더 시험 로그의 칸 글 10줄이 모두 truncated=True였다. EditMode 렌더 시험은 잘림을 기록만 하고, PlayMode 수명 시험이 잡았다.
  - 4창 합성 fixture(`UiWindowCompositionRenderTests`)는 `HudController.Instance`가 null이라 기본 영역의 대체 규칙(캔버스 위 − 12)만 탄다. 그 캡처에서 인벤토리가 fixture HUD와 겹친다. 실제 장면의 HUD 아래 시험은 통과다(구현 보고 13절).

#### 정산

- 구현자 worker_done `msg_3963475318f1`(11:53:51Z, succeeded). 보고는 `placement-impl-01/report.md`(SHA256 `27c7acb8…`)다.
- 리드 대조 `check-impl-final-1030-01.txt`(11:54:38Z):
  - 제품 파일 일곱의 SHA256·bytes가 보고 2절 표와 같다. 모두 LF이고 BOM이 없다.
  - Unity가 만든 `.meta` 다섯의 guid가 보고 표와 같다. 추적 `.meta`와 겹치는 guid는 0이다.
  - 추적 diff는 세 파일 +31/−52다. 시험 파일 변경은 `.meta` 하나뿐이고, 제품 코드의 PlayerPrefs·파일 API 줄은 0이다.
- 구현자 쪽 orca 대기 0을 확인했다(`placement-impl-shells-before-close-01.txt`, `placement-impl-ancestors-before-close-01.txt`). pane을 닫았다(`placement-impl-pane-close-01.json`, ptyKilled true).
- SENTIS 한 줄은 정산 뒤 리드가 복원했다(`placement-impl-sentis-restore-1030-01.txt`, 11:55:49Z). 복원 뒤 파일이 HEAD blob과 같다. 메인 알림 `msg_d5c28fd18c42`.
- 구현자가 스스로 적은 것(보고 11절): S0 전에 인라인 `node -e`로 `gates.json`을 한 번 보았다. 그 값은 기록에 넣지 않고 저장 스크립트 출력으로 다시 적었다. 독립 검증이 대화 기록으로 확인한다.
- 시험 메시지 속 옛 줄 번호는 이번 PR에서 고치지 않는다(「다음 계획 후보」).

### 독립 검증

- 순서: 구현 commit `af1489fb`·push → merge-tree 대조(`merge-tree-placement-1030-01.txt`, 다섯 ref 모두 충돌 0, 메인 보고 `msg_5fad6b8d20b9`) → 계약. 메인이 받은 순서다(`msg_c81d46ef6b17`).
- 계약 `placement-verify-contract-v1-02.md`(receipt `placement-verify-contract-v1-02.receipt.txt`), 강 등급, 검증 대상 head `af1489fb`. 판정 기준은 12개다. `msg_9cbc405339aa` 1항 원문과 `msg_c81d46ef6b17`의 23건 집합 재대조 조건 원문을 넣었다. 입력 경로 86개 bad 0(`placement-verify-contract-v1-paths-check-02.txt`), 본문이 인용한 message ID 12개가 모두 부록에 있다(`placement-verify-contract-v1-02-msg-ids-01.txt`).
  - 첫 조립 `placement-verify-contract-v1.md`는 msg ID 대조에서 `msg_1e160139abea`가 부록에 없어 보내지 않았다. 부록 B5를 더해 `-02`로 다시 조립했다.
  - 부록 E에 없는 검증자용 절(goal-loop 「검증 강도 4주 시범」, templates 「검증 판정」)은 부록 E2로 뽑았다(`extract-placement-verify-extra-rules-01.stdout.txt`).
- 기동(`placement-verify-launch-record-01.md`): 신규 `codex --model gpt-6-astra -c model_reasoning_effort=xhigh --disable fast_mode`, pane term_ec6b0f6d.
  - 첫 화면이 Codex 업데이트 선택창(0.165.0 → 0.166.0)이었다. 리드는 아무것도 누르지 않고 메인에 알렸다(`msg_48390023b593`). 메인이 R-6으로 Skip만 골랐고 0.165.0이 그대로다(`msg_7116a97ecd99`).
  - 다시 잰 첫 화면은 v0.165.0·GPT-6-Astra xhigh·Fast 표시 없음이다. worker-start는 task_0a0b260641d9 / ctx_3b1a4d8c145d다. rollout은 cwd art-active, gpt-6-astra, xhigh, service_tier "default"다. backend 실제 모델은 unknown이다.
- Content 사후 확인(goal 「건드릴 곳과 소유」): 리드 요청 `msg_2369dc8a6cda`, 회신 `msg_644750668713`(원문 `content-reward-postcheck-reply-msg-01.txt`).
  - 보상 창 변경은 Content 의도와 맞다고 했다.
  - PR-B는 아직 push 전이다. PR-B에서는 대장간이 열리면 길드 창이 왼쪽으로 비켜 새 인벤토리 자리와 겹칠 수 있고, 「I」 우선 닫기(`GuildGameplayWindowTests`)가 이를 막는다. 두 PR 가운데 나중 것을 병합하기 전에 합친 트리에서 그 시험을 돌리는 확인을 메인에 제안했다(`msg_cf3263fac604`).
  - 검증자에게는 보충 입력으로 보냈고 범위는 넓히지 않았다(`msg_8b40b763d723`).
- 시작 관문: 검증자가 goal.md 추적 변경을 찾고 Unity 전에 멈춰 물었다(`msg_1a742577ec1b`, 12:23:16Z). 리드가 검증 기동 뒤 이 절을 커밋 없이 넣은 것이었다(아래 「리드 실수 첫 발생 — 검증 중 goal.md 쓰기」).
  - 리드는 그 diff를 `goal-verify-section-pending-1030-01.diff`로 보존하고 goal.md를 HEAD blob으로 되돌렸다(`hold-goal-verify-section-1030-01.txt`). 되돌린 뒤 SHA256이 부록 A 기대값과 같고 추적 변경은 0이다.
  - 검증자 회신 `msg_32e0ab84da57`, 메인 알림 `msg_3f97d2d20daf`다. 검증자는 시작 관문을 다시 재고 이어 갔다. 보존한 diff는 판정 뒤 이 절로 다시 붙였다.
- 발견과 처분: 셋 모두 메인 결정이며 사용자 결정은 아니다. 판정은 셋을 지우지 않고 처분과 함께 보존했다.

  | 번호 | 발견 | 메인 처분 |
  |---|---|---|
  | WP-01 | 리드가 인라인 명령으로 만든 근거 파일을 goal :228·:239와 기동 기록 :20이 인용했다(검증자 `msg_f0c9fda505c4`). | A `msg_17871944f60d`, 정정 `msg_7318fa7283a1`. :228·:239는 재측정 출력으로 바꿨다(위 「구현」). 기동 기록 :20의 시각 파일은 근거로 쓰지 않음(WP-01, 인라인 date). 기동 기록 파일은 지우지 않는다. |
  | WP-02 | 구현자 맥락 메모가 SHA·HEAD·시각을 직접 적었다(`msg_38ba25a9a4ec`). 구현자 귀속 규칙 위반이다. | A `msg_848654814bc7`. 메모는 고치지 않는다. SHA·HEAD는 저장 start-state와 같고, 시각 두 개는 근거로 쓰지 않는다. |
  | WP-03 | 검증자 새 시험의 Rig 필드 표기가 규칙 표와 어긋나 보였다(`msg_2131cd4cd18b`). | A `msg_7e3c6c3aaf61`. 규칙 표가 시험 Rig의 섞인 정적 표기를 정하지 않고, 시험 영역 관례(`survey-test-field-naming-1030-01.txt`)와 맞는다. 시험 파일은 동결본 그대로 넣는다. |

  - 계약 보충: 2(WP-01) `msg_efc021df8110`, 2 정정 `msg_1a5975613c93`, 3(창 열림·WP-02) `msg_dcdbc31a5450`, 4(WP-03) `msg_53a0679b3eed`다.
  - 보충 2는 메인의 첫 조건대로 기동 기록 :20을 지운다고 전했다. 리드가 :20이 goal이 아니라 기동 기록의 줄이라고 메인에 알렸고(`msg_400ab9dea590`), 메인 정정 뒤 보충 2 정정으로 그 줄을 버리게 했다.
- 검증 Unity 창: 「V 준비됨」 `msg_e921b3d498d1`(12:43:50Z) → 정식 요청 `msg_000fa054134d`(12:47:04Z) → 배정 `msg_848654814bc7`(12:47:55Z, 시한 13:15:00Z)다. 리드 관문 `placement-verify-window-gate-01.txt`(12:48:37Z)는 통과다. 검증자에게 보충 3으로 창을 열었다.
  - 회차별 결과(`summarize-verify-unity-runs-1030-01.txt`, 원시 result.json·results.xml에서 옮김). 세 실행 모두 시한 전에 끝났다.

    | 회차 | 시작–끝(UTC) | 범위 | 통과/실패/건너뜀 |
    |---|---|---|---|
    | V-R EditMode | 12:52:26–12:56:17 | 새 HUD 경계 fixture | 8/0/0 |
    | V-F EditMode | 12:57:27–13:00:10 | 전체 | 1606/0/0 |
    | V-F PlayMode | 13:03:26–13:09:46 | 전체 | 152/23/19 |

  - V-F PlayMode 실패 23건의 이름 집합은 구현자 F PlayMode의 23건과 같다(같은 출력, 리드 표본 대조). 검증자는 원시 XML로 23건을 전수 분류했고, 모두 batchmode·서버 없음 경계에서 빠진 환경 때문인 (b)다(`placement-verify-01/failure-classification.md`).
  - 검증자 「Unity 시작」 `msg_4a60b312dcfe`, 「Unity 사용 끝」 `msg_d9ebd1836684`(13:10:30Z)다.
  - 반환 점검 `post-verify-drift-check-1030-01.txt`(13:10:43Z): Unity 계열·Windows dotnet·Burst dotnet 0이다. SENTIS는 알려진 한 줄이고 MinimapRT는 HEAD와 같다. 새 시험 파일은 「V 준비됨」 때 리드 대조 값과 같다. 메인 반환 보고 `msg_99477c5bc1b3`.
- 판정: `placement-verify-01/verdict.md`, worker_done `msg_465d162a2a07`(13:24:15Z, succeeded)다. **강 등급 PASS, 현재 차단 결함 0, WP-01~03 처분 보존**이다.
  - 리드 표본 대조 `audit-placement-verdict-1030-02.txt`(13:25:32Z): 판정 SHA256이 worker_done 본문과 같다. 실패 분류 표 23행과 XML 실패 23건이 같은 집합이다. 동결 pin 두 개가 지금 파일과 같다. 실제 진입 시험과 인벤토리 창 배치 장면 시험 9건이 통과다.
  - 판정이 미실행으로 남긴 것: 실제 GameView 캡처, 서버·DB, 사용자 플레이, PR282·Content PR-B와 합친 트리의 실행이다. merge-tree 무충돌은 실행 통과로 쓰지 않는다.
  - 설계 관찰(판정 「사람 가독성과 설계 관찰」, 비차단): 끈 인벤토리가 보상 창과 겹치면 보상 창이 위에 그려진다. 4창 합성 fixture는 실제 HUD가 없어 대체 규칙만 탄다. 시험 메시지의 옛 줄 번호(「다음 계획 후보」)가 남았다. EditMode 렌더 로그의 잘림 값은 단정이 아니다.
- 정산: 검증자 쪽 orca 대기는 0이다. 남은 대기 둘은 메인 Claude의 것이다(`placement-verify-shells-before-close-01.txt`, `placement-verify-ancestors-before-close-01.txt`). pane을 닫았다(`placement-verify-pane-close-01.json`, ptyKilled true).
  - SENTIS 한 줄은 정산 뒤 리드가 복원했다(`placement-verify-sentis-restore-1030-01.txt`, 13:27:25Z). 복원 뒤 파일이 HEAD blob과 같다.
  - 새 시험 `UiWindowPlacementBoundaryTests.cs`와 `.meta`는 검증자 쓰기다. 동결 pin과 같은 채로 이 기록 commit에 넣는다(WP-03 A 조건 1).
- 다음 위임 계약부터 「맥락 메모의 시각·SHA·HEAD도 저장 스크립트 출력으로 적는다」를 넣는다(WP-02 A 조건 3). 이 PR의 남은 작업에도 적용한다.
- 메인이 마지막 보고에 올릴 규칙 후보: 리드 자가 점검 층(`msg_17871944f60d`), 맥락 메모 양식의 시계 칸(`msg_848654814bc7`), 시험 Rig·자료 묶음의 internal 필드와 섞인 정적 필드 기준(`msg_7e3c6c3aaf61`)이다.
- 판정 뒤 goal 변경은 메인 조건대로 :228·:239 두 줄과 이 절뿐이다. 재개 카드와 진척 단계는 이 commit에서 바꾸지 않는다. 다음 첫 일은 PR 전 merge-tree 재대조(그때의 Effect head와 Content PR-B head), PR 생성, 완료조건 8의 캡처 창 요청이다.
- 이 절의 msg ID와 시각은 `list-verify-msg-ids-1030-01.txt`에서 옮겼다.

#### 리드 실수 첫 발생 — 검증 중 goal.md 쓰기

- 무엇: 검증 기동 뒤 리드가 이 절을 goal.md에 커밋 없이 넣었다. 계약이 goal.md를 부록 A input으로 고정한 뒤였다.
- 발견: 검증자 시작 관문(`msg_1a742577ec1b`)이 Unity 전에 잡았다. 실행 영향은 0이다.
- 처리: 위 「시작 관문」대로 diff를 보존하고 되돌렸다. 판정 전까지 리드는 goal.md와 계약 input 파일을 쓰지 않았다.
- 층: 첫 발생이라 goal 기록만 한다. 두 번째면 리드가 검증 기동 때 계약 input 목록을 저장하고, 기록 쓰기 전에 그 목록과 대조하는 helper를 둔다.

#### 리드 실수 반복 — 인라인 조회 값을 근거로 인용(WP-01)

- 무엇: 조건 1(`msg_af08be79ca51`)과 재진입 규칙(`msg_dbc8a605e5c1`) 뒤에도, 인라인 date·sha256sum·node -e로 만든 파일 세 개를 근거로 인용했다(`audit-lead-inline-evidence-1030-01-classification.md`).
- 층: 반복이라 한 층 올렸다. 리드는 기록 commit과 메인 보고 전마다 자가 점검 `audit-lead-inline-evidence-1030-01.mjs`를 새 출력 번호로 돌려, 인용된 인라인 근거가 0인지 확인한다. 메인이 받았다(`msg_17871944f60d`).
- 한계: 이 점검은 리드가 화면 값을 손으로 옮겨 적는 경우를 잡지 못한다. 그래서 기록에는 저장 출력의 값만 그 파일 이름과 함께 쓴다.
- 도구 수정: 첫 판은 HEAD의 goal만 읽어서 커밋 전 새 인용을 보지 못했다. 둘째 판 `audit-lead-inline-evidence-1030-02.mjs`는 작업 트리 goal도 읽는다. 이 commit 전 점검은 `audit-lead-inline-evidence-1030-11.txt`이고, 분류는 `audit-lead-inline-evidence-1030-11-classification.md`다. 작업 트리 goal이 인용한 플래그 출력 넷은 모두 원시 CLI 출력이거나 저장 스크립트 출력이다.

### 리드 실수 첫 발생 — 줄 끝 계수와 정규식 이스케이프

- `check-r-ready-1030-01.txt`의 `cr_lines` 칸은 틀린 값이다. Git Bash grep의 `$'\r'` 해석이 줄마다 맞았다. node 바이트 셈(`check-line-endings-1030-01.txt`)으로 다시 쟀다.
- `check-rf-results-1030-01.mjs`는 템플릿 문자열의 `\b`를 backspace로 읽어 test-run 값을 모두 absent로 냈다. 기록에는 고친 둘째 판(`check-rf-results-1030-02.txt`)만 쓴다.
- 층: 각각 첫 발생이라 goal 기록만 한다. 두 번째면 줄 끝은 node 스크립트로만 세고, 결과 XML 읽기는 저장된 공용 helper 하나로 모은다.

### 리드 실수 첫 발생 — 보내기 receipt가 저장소 루트에 생김

- 무엇: 09:27Z·09:28Z 두 보내기(메인 `msg_1c50f3cefab9`, Content `msg_19ba265fd0eb`)의 receipt 접두어에 근거 폴더 경로를 빠뜨렸다. receipt 6개가 저장소 루트에 미추적으로 생겼다.
- 발견: 작성자의 U1 작업 트리 관문(`msg_c43a97128e5b` 2번)이 Unity를 띄우기 전에 잡았다. U0 끝 뒤라 U0 영향은 0이다.
- 처리: `move-root-receipts-1030-01.sh` → `move-root-receipts-1030-01.txt`로 근거 폴더에 옮겼다(전후 SHA256 같음). 메인은 기록만 받았다(`msg_65f4f6a8582a`).
- 층: 첫 발생이라 goal 기록만 한다. 두 번째면 보내기 helper가 근거 폴더 밖 접두어를 거부하게 고친다.

### PR 생성·캡처·사용자 확인·병합

- PR: [PR284](https://github.com/bass131/dawnholder-server/pull/284)(`placement-pr-create-1030-01.txt`), 설명 `placement-pr-body-1030-01.md`, head `41568d73a442bc17b2bcfab5c948c18f7c0895b2`(`check-placement-capture-return-1030-02.txt`).
- 완료조건 8 캡처: 메인 결정 「캡처 A」(`msg_a8db05949eb7`, 사용자 결정 아님). 실행자는 신규 `[Art 구현 Opus]`(claude-opus-5-5 xhigh) task_e09735ee678d다(`placement-capture-launch-record-01.md`). 결정 4A 기간이라 Claude 세션이다.
  - C1(마을에서 I로 연 인벤토리)과 C2(보상 창과 함께 연 장면)는 각각 Unity exit 0, 1 통과·0 실패·0 건너뜀이다(`placement-capture-01/report.md` 「결과 요약」).
  - C2는 커밋하지 않은 임시 case로 찍었다. 확인 자료이고 통과 근거가 아니다. 실행 뒤 임시 case를 되돌렸고, 시험 파일이 HEAD blob과 같다.
  - 리드 반환 점검(`check-placement-capture-return-1030-02.txt`): PNG 4장의 SHA256이 report에 적힌 값과 같다. 결과 XML 원시는 C1·C2 각각 1 통과·0 실패·0 건너뜀이다.
  - 실행자 자기 신고 다섯 건(빈 `node -e "0"` 1회, 계약 읽기 전 인라인 `sha256sum`·`ls`, 점검 스크립트의 CR 세기 오류, Bash 한 번의 `cd`, 대화 기록 grep 읽기)은 report 「경계 위반과 실수」에 있다. 값으로 쓴 것은 없다.
- 사용자 확인 항목: `placement-user-check-lines-draft-01.md`(캡처 넷, 직접 플레이 일곱 줄, 알려진 점 셋).
- 병합: 사용자 승인 줄을 받아 메인이 병합했다. merge commit은 `a0195c84d6b92632e38e619a175963b09b15b8f5`이고, 첫 부모는 PR286 병합 `9cc80a9d`다(`closeout-01/pr-facts-275-279-284-287-290-01.txt`, 메인 `msg_8ee5e8973b07` 2026-10-31T07:57:39Z).
- 직접 플레이: 사용자가 병합된 main으로 플레이했다(77 A, 메인 `msg_38d8fd806c50`). 78 C(메인 `msg_2863fd2984ea`)의 「나머지는 괜찮음」으로 PR284 창 끌기는 문제 없음이다. 같은 의견의 캐릭터 선택창 요청은 Content의 다음 goal 몫이다(BACKLOG `character-select-three-wide-slots`).

### 생존 신호 정산

- 이 goal 단계의 생존 신호는 단계마다 정산하지 않았다. 외관 goal의 Gardener 반환 GR-03을 처리하면서 정본 helper로 함께 정산했다. 대상은 선행 시험, 구현, 독립 검증, 캡처 네 단계다(`art-active/.backups/verification/2026-10-29-inventory-character-select-look/closeout-01/liveness-settlement-closeout-01.md`).
- 선행 시험의 301~303초 구간들은 heartbeat 루프가 `sleep 300` 뒤에 보내는 구조 탓이다(`placement-red-01/scripts/heartbeat-loop.sh`). 나머지 초과 구간은 이유 미측정이다. 구간과 초는 위 정산의 표에 있다. 미측정 구간은 메인 결정 1 A(`msg_e1d856276e79`)로 그대로 닫았다.

### 완료조건 대조 — 종료

| 조건 | 상태 | 근거 |
|---|---|---|
| 1~6 | 충족 | 위 「선행 시험 — RED」·「구현」, 판정 `placement-verify-01/verdict.md` |
| 7 | EditMode는 전체 통과다. PlayMode는 실패 23건이 남았고, 검증자가 모두 변경 전부터 있던 환경 실패 (b)로 분류해 PASS로 봤다 | 위 「독립 검증」 회차 표와 `placement-verify-01/failure-classification.md` |
| 8 | 강 등급 PASS와 캡처 넷은 충족. 직접 플레이는 78 C 「나머지는 괜찮음」이다. 사용자의 캡처 확인 기록은 리드가 받지 않았다 | 위 「독립 검증」, 「PR 생성·캡처·사용자 확인·병합」 |
