# PlayMode 전체 실행에서 전제가 빠진 시험을 실패 대신 건너뜀으로

## 재개 지점

**다음 첫 일:** PR #286은 병합됐다(병합 commit `9cc80a9d`). 이 goal의 결과 기록과 종료 Gardener는 Effect 세 goal이 함께 쓰는 종료 기록 [PR288](https://github.com/bass131/dawnholder-server/pull/288)(branch `docs/effect-closeout-20261031`)에 넣었다. 다음은 그 PR의 문서 실사를 신규 독립 검증자로 열고, PASS 뒤 메인에 준비 보고를 올리는 일이다. 그 PR이 병합되면 메인이 R-8로 리드를 새로 연다. 결과는 아래 [결과](#결과)에 있다.

| 항목 | 현재 |
|---|---|
| 승인 | 메인 승인 초안 v1(`msg_71f0ed6d4994`). 이 goal과의 차이 9곳은 메인이 대조해 통과시켰다(`msg_e302e19ae9fa`, 아래). |
| 리드 | `[Effect 리드 Opus]`(claude-opus-5-5 xhigh). Run `run_5f3209972e68`, 회신 주소 `run:run_5f3209972e68`. 시험 21건의 소유자 Content 리드와 기술 계약을 직접 조율한다. |
| branch | `fix/playmode-precondition-ignores-20261030`. Content PR-B(PR #285) branch `feat/content-guild-base-window-20261030`의 head `c41952da` 위에 쌓았다. 처음에는 `6669e2d7`에서 만들었고, PR-B 최종 head 알림 뒤 13:44:28Z에 `ae839e0c`로(`lead/rebase-01.txt`), PR-B goal 기록 commit 뒤 15:22:13Z에 `c41952da`로 옮겼다(`lead/rebase-02.txt`). PR-B가 `GuildWindowIndependentSceneTests.cs`를 이미 고쳤기 때문이다. 선행 시험의 기준 head `429481c2`와 지금 head는 `03_Client`·`DEVELOPMENT.md`가 같다(같은 기록). 작업 공간 effect-active. |
| PR | #286, base `feat/content-guild-base-window-20261030`. 화면·플레이 변화가 없어 사용자 확인 항목은 없고 병합 승인만 남았다. 설명 원문은 `lead/pr-body-01.md`다. PR281·PR285 병합 뒤 base가 main으로 바뀌었고, head `bf044c72` 그대로 병합됐다(2026-10-31T07:54:37Z, 병합 commit `9cc80a9d`, `lead/pr-state-16.json`). |
| 근거 폴더 | `.backups/verification/2026-10-30-playmode-precondition-ignores/`(Git 제외). 리드 원시는 `lead/`다. |
| 실행 창 | Unity는 effect-active `03_Client` batchmode만 쓴다. 창은 메인 배정이다. |

## 진척 단계

- [x] goal과 계약 기록
- [x] 선행 시험(lane 판정 harness)과 실패 원시
- [x] 구현과 통과 원시
- [x] 독립 검증 통과
- [x] PR 생성과 확인 항목
- [x] PR 병합
- [>] 결과 기록과 종료

## 적용 중인 사용자 결정

### 10-30 실패 23건 정리

- 사용자 원문(2026-10-30 22:1x KST 메인 pane 제출, 메인 전달 `msg_71f0ed6d4994`): 「실패 23건도 이참에 병렬로 좀 정리좀 해줘」
- 파트 분할(Effect 리드가 맡고 Content 리드가 소유자로 조율)은 메인 결정이다. 목표 자체는 사용자 지시다.

### 10-31 결정 73 A — 원인 미확정 기존 실패 13행은 실패로 두고 후속 후보 — msg_20097f42c3e3

- 사용자 원문(2026-10-31 16:5x KST 메인 창에 Enter로 제출, 메인 전달 `msg_20097f42c3e3`): 「대시보드 결정 응답: … · 3) 73 - 원인 미확정 기존 실패 13행(잉크 글자 12·길드 창 위치 1) 처분 → A 실패로 두고 후속 후보 · …」
- 메인이 전한 적용: [독립 검증 결과](#독립-검증-결과)의 원인 미확정 기존 실패 13행은 「통과 아님」으로 그대로 두고 후속 후보로 간다. 시험 주인 Content가 BACKLOG 후보를 쓴다. Effect는 이 goal에 결정만 기록한다.
- 13행의 내역은 L1 잉크 11행과 L2 2행(`OverlayAtTarget_802x451` C8/C9 잉크, `OverlayAtTarget_1920x1080` 창 위치)이라 결정 문구의 「잉크 글자 12·길드 창 위치 1」과 같다.

### 10-31 결정 76 A — 종료 Gardener 하나로 묶음 — msg_84d148dc5236

- 원문과 적용은 [개선안 3 goal의 같은 절](../2026-10-30-kill-reward-float-text/goal.md#10-31-결정-76-a77-a--종료-gardener-하나로-묶음-pr283-병합-뒤-바로-플레이--msg_84d148dc5236)이다. 이 goal도 그 Gardener 하나가 본다(정본과 다른 이번 한 번의 사용자 결정 예외).

### 10-30 메인 승인 초안 v1 — msg_71f0ed6d4994(메인 결정, 사용자 결정 아님)

- 만들 것·건드릴 곳·하지 않을 것·완료조건 다섯·PR 경계는 아래 각 절에 옮겼다. 차이는 [메인 초안과의 차이](#메인-초안과의-차이)에 모았다.

### 10-30 메인 처분 — goal 대조 통과 — msg_e302e19ae9fa(메인 결정, 사용자 결정 아님)

- 차이 9곳을 받는다. 완료조건 1은 「21건 Ignored + 이유, 2건 Explicit + 이유, failed 0」으로 읽는다. 기준 head에서 23건 집합이 다르면 구현 전에 보고한다.
- `DEVELOPMENT.md` 절은 실행 전제 안내다. 변수 표와 「각 lane을 무엇으로 켜고, lane 결과에 skipped가 있으면 그 lane은 통과가 아니다」를 쓴다. 모든 시험에 적용하는 새 작업 규칙으로 넓히지 않는다. 규칙 정본화는 Rules 몫이라 메인이 마지막 보고에 규칙 후보로 올린다.
- harness의 판정 식(23건 이름 목록, 기대 결과, skipped 0 검사)은 구현 전에 고정하고 sha256을 남긴다. 구현자는 harness를 고치지 않는다.
- 서버 2건의 주인 파트는 미정으로 적는다. Core 리드는 지금 열려 있지 않아 새로 열지 않는다. 메인이 마지막 보고에 한 줄로 올린다. Content에는 이미 알렸다.

### 10-27 결정 4 A·10-30 메인 운영 방식

- 65 A goal(`01_Phases/goals/2026-10-30-attack-key-left-ctrl/goal.md`)의 같은 이름 절을 따른다. 선행 시험 작성자·구현자는 신규 Opus xhigh, 독립 검증은 신규 GPT Astra xhigh다. 병합 승인은 마지막에 한 번에 한다.

### 메인 결정 — 기존 시험 실패의 전수 분류 — msg_71e41e231d55

- 원문은 [하네스 원칙 goal의 재전달 절](../2026-10-03-harness-principles/goal.md#기존-테스트-대량-실패의-분류)이다. 작업자와 독립 검증자 계약에 그 절 원문을 그대로 붙인다. 이번 23건의 분류는 (b) 환경이다.

### 독립 검증 중 메인 처분(메인 결정, 사용자 결정 아님)

원문 저장본은 `lead/wait-26.json`·`wait-30.json`·`wait-32.json`·`wait-33.json`이다.

- `msg_202151c481c8`: 선행 시험 작성자의 빈 `node -e "" 2>/dev/null;` 1회는 계약 금지 실행 위반이다. 비차단으로 두고 기록하며, 위반이 없었다고 소급하지 않는다. 리드가 두 번 섞은 같은 빈 `node -e`도 받고, 메인이 마지막 보고에 규칙 후보로 기록만 한다. 리드는 앞으로 명령 앞에 빈 `node -e`를 붙이지 않는다.
- `msg_a2b82a34e33f`: 캡처 변수가 공백만일 때 `GuildGameplayWindowTests` 6건이 건너뛰는 것은 이 PR의 결함이 아니다(관찰 기록). 조건은 goal 「남은 우려」와 PR 설명에 빈 값 판단식 차이를 적는 것이다. Content 답(`msg_5e4d2d7151f9`)이 「공백도 빈 값」이라 처분은 그대로다.
- `msg_b0795f230724`: 검증자 자신의 이탈 두 건(heartbeat 436.621초 공백, 기록 wrapper 밖 읽기 1회)은 비차단·기록이다. 조건은 실패 분류의 (d)를 「lane 기대 실패」와 「원인 미확정」으로 나누는 것이었다.
- `msg_e0d1d3c3723f`: 기준과 같은 기존 실패는 이 goal의 PASS를 막지 않는다. 「기준과 같은 기존 (d), 통과로 바꾸지 않음, 사용자 처분 대기」로 남기고, PASS는 완료조건 2(결과 보존)로 판정한다. permit lane 기존 실패 2건은 PR 설명과 마지막 보고에 「원인 미확정, Content 후속」으로 올린다.

## 파트 간 계약 — Content 리드 조건 — msg_196d2c0ec1d4(리드 간 조율, 사용자 결정 아님)

- 원문 저장본: `lead/source-messages-01.json`(메인 초안과 이 메시지 두 건, sha256 `e5ba7b6b…a27e`). 방식에는 반대하지 않고 조건 넷을 붙였다.
  1. 건너뜀은 변수가 「없을 때」만 쓴다. 값이 틀리면 지금처럼 실패한다. `CheckPermit()`은 「memory use requires a runner permit」 한 줄만 바꾸고 이어지는 위치·schema·token·Task·Dispatch·시한·PID 검사는 그대로 둔다. 캡처 폴더도 빈 값만 건너뛰고, 경로가 틀리거나 폴더를 못 만들면 실패로 남긴다.
  2. 잉크 판정 11건은 판정 단계에 들어가기 전에 건너뛴다. 변수가 있는 실행에서는 판정 중 「no capture folder」(`KeepOriginal`, PR-B :2118~2120)가 지금처럼 실패로 남아야 한다. 판정·문턱·식은 건드리지 않는다.
  3. `DefaultScreen_ApprovedValues`의 건너뜀 이유에 「Editor Game 화면 필요」를 적는다. 지금 lane은 모두 batchmode라 이 시험이 Editor 대화형 실행에서만 판정된다는 점을 남은 우려에 적는다.
  4. 서버 2건은 Content 파일이 아니다. 「포트에 붙어 보고 안 되면 건너뜀」은 서버가 있어야 하는 lane에서 서버가 죽어도 조용히 넘어가므로 피한다.
- 장치 위치 의견: 시험 쪽은 조건 1로 값 오류를 막는다. 남는 구멍은 「runner가 변수를 빠뜨림」이다. 지금 runner(`run-u3c.ps1`·`run-u4.ps1`·permit runner)는 모두 Git 밖이라, 검사는 실행 계약의 lane 판정 기준에 둔다. Content는 다음 길드 lane 계약부터 「가려진 lane skipped 0」을 넣는다. 저장소 안 runner는 이번 범위 밖이다.
- PR-B 상태: 독립 검증 V1이 Unity 창을 쓰는 중이다(시한 13:38Z). PR-B 최종 head에는 검증자 새 시험 `GuildBaseWindowIndependentTransitionTests.cs`와 goal commit이 더 얹힌다. 이 goal의 파일과 겹치지 않는다. 판정 결과로 두 길드 시험 파일을 다시 고치면 Content가 쓰기 전에 알린다.
- PR-B 최종 head(Content `msg_da63bd767371`, 13:43:17Z): PR #285, head `ae839e0c`, 독립 검증은 계약 범위 통과다. `6669e2d7` 뒤의 commit은 검증자 새 시험 `GuildBaseWindowIndependentTransitionTests.cs`(시험 5건, 캡처·permit 변수를 쓰지 않음)와 goal 기록이다. 두 길드 시험 파일은 바뀌지 않았다(리드 `git diff --stat 6669e2d7 ae839e0c` 확인).
- PR-B head 변경(메인 `msg_9d2adae269d5`): `c41952da`. 바뀐 것은 Content goal.md뿐이고, 옮길 때는 리드가 정한다고 했다. 리드는 구현 전에 옮겼다(위 branch 줄). 구현자·검증자·PR이 같은 head 위에서 일하게 하려는 것이다.

## 대상 23건

원시: Art 70 A 구현 F의 PlayMode 전체 실행 `art-active/.backups/verification/2026-10-29-inventory-character-select-look/placement-impl-01/f/playmode-01/results.xml`(sha256 `9aad93d1…5751`, 194건 중 통과 152·실패 23·skipped 19, 실행 336초). 리드 추출은 `lead/extract-full-run-01.json`이다. 메인 목록과 클래스별 수가 같다. 줄 번호는 PR-B head 기준이다(원시의 줄은 art-active 판이라 다르다).

| 묶음 | 수 | 지금 실패 문구 | 실패 지점(PR-B) | 빠진 전제 |
|---|---|---|---|---|
| `GuildGameplayWindowTests` 전부 | 6 | runner supplies the owned capture directory | `GuildGameplayWindowTestTools.cs` :111~112 `Recorder` 생성자 | `DAWNHOLDER_GUILD_VERIFY_CAPTURE_DIR` |
| `GuildWindowIndependentSceneTests.OverlayAtTarget_*` | 3 | memory use requires a runner permit | 같은 파일 :2650~2651 `CheckPermit` | `DAWNHOLDER_GUILD_MEMORY_PERMIT` |
| `…DefaultScreen_ApprovedValues_*` | 1 | GUI: the main play mode view is the editor's own GameView… | :683~684 | Editor의 main play mode view(Game 화면) |
| 잉크 판정(`NegativeControl_*` 7, `RenderTextureAuxiliary_*` 3, `DefaultScreen_UpperBoundValues_*` 1) | 11 | 판정 결과 단정(:690·:734·:781)이 unjudgeable로 실패 | 판정 중 `KeepOriginal` :2118~2120 | `DAWNHOLDER_GUILD_VERIFY_CAPTURE_DIR` |
| `InventoryServerIntegrationTests`·`MapEntryServerIntegrationTests` 각 1 | 2 | Timed out: handshake and character selection | `MapEntryPlayFixture.Wait` | 127.0.0.1:7777의 실제 GameServer(파일 머리 주석 「Separate CLI filter」) |

- 이미 건너뛰는 19건의 선례: 캡처 변수 없음은 `if (string.IsNullOrEmpty(변수)) Assert.Ignore($"… {Variable}")` 다음 줄에 폴더 존재 단정을 둔다(`GuildGrowthEffectAreaEvidenceTests.cs` :430 등 5곳). Game 화면 없음은 `Assert.Ignore("… needs the Editor's Game view")`다(`GuildGrowthEffectIndependentAreaSceneTests.cs` :217, `GuildGrowthEffectIndependentCaptureTests.cs` :173). 실서버 lane은 `[UnityTest, Explicit(이유)]`다(`GuildLiveServerWindowTests.cs` :94·97, 원시 result `Skipped`·label `Explicit`).
- 기준 head의 집합은 선행 시험의 전체 실행으로 다시 확정한다. PR-B 판의 실패 집합이 이 23건과 다르면 구현 전에 메인에 보고한다.

## 리드 결정

1. **서버 2건은 `Explicit(이유)`로 둔다.** 메인 초안의 「실서버 lane 전제」를 기존 실서버 lane 선례로 채운다.
   - 이유: 파일 머리 주석이 이미 「Separate CLI filter」다. 이름으로 고른 실서버 lane에서는 시험이 그대로 돌아, 서버가 없으면 지금처럼 실패한다(Content 조건 4의 우려). 새 변수 이름과 문서 항목이 필요 없다.
   - 핵심 단점: 원시 XML에 Ignored가 아니라 Skipped·label `Explicit`로 나온다. 되돌리기는 속성 한 줄이다.
2. **「전제를 준 lane의 건너뜀은 실패」 장치는 실행 전제 안내와 이 goal의 lane 판정 harness에 둔다.**
   - 교정 층: 가장 높은 층은 저장소 안 lane runner나 CI 검사다. 지금 runner는 모두 Git 밖 근거 폴더에 있고, 저장소 runner는 새 도구라 이번 승인 범위 밖이다. 그래서 문서(DEVELOPMENT 새 절의 실행 전제 안내)와 계약 검사로 막고, 저장소 runner는 「다음 계획 후보」에 남긴다. 이 안내를 모든 시험의 작업 규칙으로 정본화하는 일은 Rules 몫이다(메인 `msg_e302e19ae9fa`).
   - 시험 쪽 막음은 Content 조건 1(빈 값만 건너뜀)이다.
3. **시험 실행 안내는 `00_Document/operations/DEVELOPMENT.md`에 새 절로 둔다.** 저장소 문서에 이 변수 이름이 한 번도 나오지 않는다(리드 `git grep`, PR-B head). 「개발과 검증」 문서가 실행 전제·부작용의 진입점이다(AGENTS 「작업 진입점」).
4. **선행 시험은 새 시험 클래스가 아니라 lane 판정 harness다.** 환경 변수는 프로세스 전체 값이라 시험 안에서 「변수 없는 실행」을 따로 만들 수 없다. 그래서 RED는 저장된 runner와 결과 XML 판정기가 기준 head의 전체 실행에서 23건 실패를 잡는 것이다. 판정기는 근거 폴더에 둔다(Git 밖). 판정 식은 구현 전에 고정하고 sha256을 남기며, 구현자는 고치지 않는다.
5. **CURRENT는 이 PR에서 고치지 않는다.** Effect 줄을 PR283 branch도 고쳐, base가 다른 두 PR이 같은 줄을 겹쳐 고치게 된다. 이 goal 링크는 PR283 병합 뒤 종료 기록 때 Effect 줄에 넣는다.
6. **값이 틀린 lane의 `OverlayAtTarget`은 `DAWNHOLDER_GUILD_MEMORY_DIR` 없음을 따로 다루지 않는다.** 선행 시험 작성자는 L4에서 이 3건이 permit 문구가 아니라 `MemoryDirectory()`의 `ArgumentNullException`으로 실패한다고 관찰했다.
   - 이유: Content 조건 1이 `CheckPermit`에서 permit 한 줄만 바꾸라고 했다. L4는 permit을 틀린 값으로 주는 lane이라 지금처럼 실패해야 한다. 판정 식의 skips는 Skipped만 보고, lane L4는 그 3건이 기준과 같아야 통과다.
   - 핵심 단점: 그 lane의 실패 문구가 permit 오류를 직접 말하지 않는다. 되돌리기는 검사 한 줄이다.
7. **기본 화면 두 시험의 건너뜀은 rendering 해상도 읽기 뒤, `CheckModel`·`Judge` 앞에 둔다.** 기준 L0·L4에서 `DefaultScreen_ApprovedValues`는 view 없음으로 시작했고, 그 측정 중에 batchmode의 main GameView(880x531)가 생겨 뒤 시험까지 남았다(`lead/facts-view-side-effect-01.md`). 건너뜀을 더 앞에 두면 뒤 시험의 화면 크기가 바뀔 수 있다. Content 조건 2(판정 단계 전)도 지킨다. 정확히 어느 호출이 GameView를 만드는지는 확인하지 않았다(추정). 대상 밖 결과가 바뀌면 구현자는 다른 시험을 고치지 않고 멈춰 묻는다.

## 만들 것

1. 전제 판정 다섯 곳. 빠진 전제가 「없을 때」만 건너뛰고, 이유에 빠진 전제 이름(변수 이름 또는 「Editor Game 화면 필요」)을 넣는다. 한 시험의 전제가 둘 이상 빠졌으면 모두 적는다. 예를 들어 `DefaultScreen_ApprovedValues`는 캡처 폴더와 Game 화면을 둘 다 쓴다. 형태는 위 선례를 따른다.
   - `Recorder` 생성자: `DAWNHOLDER_GUILD_VERIFY_CAPTURE_DIR` 빈 값. 폴더를 못 만들면 지금처럼 실패한다.
   - `CheckPermit`: `DAWNHOLDER_GUILD_MEMORY_PERMIT` 빈 값 한 줄만. 앞의 Unity 버전 단정과 뒤의 검사들은 그대로다.
   - `DefaultScreen_ApprovedValues`: main play mode view가 없을 때만. view가 있는데 종류·크기가 틀리면 지금처럼 실패한다. 판단 값은 시험이 이미 기록하는 `mainPlayModeView`다. batchmode 여부만으로 정하지 않는다. 같은 측정 함수를 쓰는 UpperBound가 같은 batchmode 실행에서 :683을 지나 판정까지 갔기 때문이다. batchmode의 view 유무가 실행 순서에 따라 다를 수 있으니, 선행 시험 작성자가 두 시험의 로그 `main play mode view` 값을 기록한다.
   - 잉크 판정 11건: 판정 단계 전에 `DAWNHOLDER_GUILD_VERIFY_CAPTURE_DIR` 빈 값. 변수가 있는데 폴더가 없으면 지금처럼 판정 중 실패한다. 같은 클래스의 캡처를 쓰지 않는 시험(Disabled·Pointer·Markup·MissingGlyph 등)은 건너뛰지 않는다.
   - 서버 2건: `[UnityTest, Explicit(이유)]`. 이유에는 「127.0.0.1:7777의 실제 GameServer, 이름으로 고른 실서버 lane에서만」을 적는다.
2. `DEVELOPMENT.md` 새 절(실행 전제 안내): 어느 변수·lane이 어느 시험을 켜는지 표 하나와 한 문단(각 lane을 무엇으로 켜고, lane 결과에 대상 시험의 skipped가 있으면 그 lane은 통과가 아니다). 모든 시험의 새 작업 규칙으로 쓰지 않는다.
3. lane 판정 harness(근거 폴더): 전체 실행의 판정, lane별 전후 비교, skipped 0 검사.

## 건드릴 곳과 소유

| 파일 | 변화 | 쓰는 세션 | 소유·조율 |
|---|---|---|---|
| `03_Client/Assets/Tests/PlayMode/GuildGameplayWindowTestTools.cs` | `Recorder` 전제 | 구현자 | Content(조건 `msg_196d2c0ec1d4`) |
| `03_Client/Assets/Tests/PlayMode/GuildWindowIndependentSceneTests.cs` | permit·Game 화면·잉크 판정 전 전제 | 구현자 | Content |
| `03_Client/Assets/Tests/PlayMode/InventoryServerIntegrationTests.cs`·`MapEntryServerIntegrationTests.cs` | 속성 한 줄씩 | 구현자 | 주인 파트 미정. 생긴 곳은 Inventory가 [아이템·인벤토리 goal](../2026-10-05-items-inventory-currency/goal.md), MapEntry가 [클라이언트 진입 goal](../2026-09-29-refactor-client-entry/goal.md)이다. Content 리드는 자기 파일이 아니라고 했다. 메인이 마지막 보고에 한 줄로 올린다(`msg_e302e19ae9fa`). |
| `00_Document/operations/DEVELOPMENT.md` | 새 절 하나 | 구현자 | Effect |
| lane 판정 harness | 근거 폴더(Git 밖) | 선행 시험 작성자 | Effect |
| goal.md | 범위·결과·근거 | 리드 | Effect |

- 열린 PR과의 겹침(리드 조회 13:33:32Z, `lead/open-prs-files-01.json`, 열린 PR 6개): `DEVELOPMENT.md`와 두 길드 시험 파일을 고치는 열린 PR은 없다. PR282(왼쪽 Ctrl)가 `InventoryServerIntegrationTests.cs` 106~118행을 고친다. 이 PR은 같은 파일의 시험 속성 한 줄(PR-B 판 :43)만 바꿔 hunk가 60줄 넘게 떨어진다.
- 리드 결정(되돌리기 쉬운 쪽): 이 PR은 PR-B 위에 쌓아야 해서 PR282 위에 함께 쌓을 수 없다. 그래서 떨어진 hunk로 두 PR의 병합 순서를 무관하게 둔다. 처치 보상 글 PR과 Art 70 A의 `client.md` 선례와 같다. 두 번째로 병합되는 PR에서 리드가 충돌 없음을 다시 확인한다.

## 하지 않을 것

- 기대값·문턱·잉크 규칙·판정 식 변경, 시험 삭제, 무조건 `[Ignore]` 속성, 제품 코드 변경.
- 잉크 10건의 판정 실패(전제를 준 실행) 처분. 범위를 정할 때는 사용자 처분 대기였고, 뒤에 결정 73 A(실패로 두고 후속 후보)로 처분됐다.
- 저장소 안 lane runner·CI 검사 새로 두기.
- CURRENT Effect 줄(리드 결정 5).

## 관찰 가능한 완료조건

1. 전체 PlayMode 실행(실행 변수 없음, batchmode, 실서버 없음): failed 0. 원시 XML의 fullname 대조로 21건이 Ignored와 빠진 전제 이름으로, 서버 2건이 Skipped·`Explicit`와 이유로 나온다. 23건 밖의 시험은 기준 head의 같은 명령과 이름·결과가 같다.
2. 전제를 준 실행에서는 결과가 바뀌지 않는다. 기준 head와 바꾼 head에서 같은 명령을 돌려 이름·결과·첫 문구 단위로 비교한다.
   - 캡처 변수 lane: 길드 21건 중 그 변수가 켜는 시험. 메인 초안의 PR-B U4 ⓪(7/10)과도 이름·첫 문구 단위로 대조한다.
   - permit lane: `OverlayAtTarget` 3건. permit은 `CheckPermit`의 schema대로 작업자가 근거 폴더에 만든다.
   - 다른 전제가 여전히 빠진 시험은 그 전제 이름으로 건너뛸 수 있다. 이 경우는 이름과 이유를 표로 남긴다.
   - Game 화면 시험: 정적 diff(빠진 경우만 건너뜀)로 보이고 「Editor 대화형 실행 미실행」으로 적는다.
   - 서버 2건: 정적 diff(속성 한 줄)와 함께, 서버 없이 class 이름 filter로 고른 실행에서 Skipped가 아니라 지금처럼 「Timed out: handshake」로 실패함을 보인다. 실서버 실행은 하지 않는다.
3. 「전제를 깜빡한 lane이 조용히 건너뛰어 통과」를 막는다. `DEVELOPMENT.md`에 실행 전제 안내가 있고, harness의 skipped 0 검사가 실제로 실패를 낸다(1의 전체 실행 XML을 전제 lane 판정에 넣으면 실패).
4. 기존 단정 변경 정본 절차를 지킨다: 줄 단위 분류 표, 같은 명령 전후 원시, 요구 출처(위 사용자 원문). 분류는 (b) 환경이다.
5. 정본 TDD 순서(선행 시험 Opus → 구현 Opus → 검증 Astra)를 따르고, 신규 독립 검증자의 강 등급 판정이 통과다.
6. 사용자가 `병합 승인: PR<번호> head <40자>`를 제출한다. 화면·플레이 변화가 없어 사용자 확인 항목은 없다.

## PR 경계와 검증

- PR 하나다. base는 PR-B branch이고, 병합 순서는 PR281 → PR-B → 이 PR이다. PR-B 최종 head가 바뀌면 이 branch를 그 위로 옮기고 다시 대조한다.
- 검증 등급은 강이다. 시험의 실패·건너뜀 분류를 바꾸는 일이라 「오류 분류」 범주에 닿는다. 시험 네 파일·문서 한 절·harness를 합치면 50줄을 넘을 것으로 본다.
- 강 등급의 「실제 경로 1회」는 Unity PlayMode 전체 실행(batchmode)과 전제 lane 실행으로 채운다.
- R-7 비해당이다(선정 목표 아님).

## 실행 창과 사용자 손

- 전체 PlayMode 실행 한 번은 원시 기준 약 6분이다. Unity 창은 단계마다 30~45분으로 잡는다(전체 실행, 캡처 변수 lane, permit lane, 서버 2건 filter 실행). 단계마다 메인에 「무엇·예상 시간·관문 측정 출력 경로」를 보낸다.
- 관문은 매번 저장 스크립트로 잰다: 물리·commit 여유 각 4 GiB, Unity 0, Windows dotnet 0.
- 서버·DB는 쓰지 않는다. 사용자 손은 병합 승인뿐이다.

## 선행 시험 결과(구현 전 RED)

작성 신규 `[Effect 구현 Opus]`(Task `task_a91407ca2f11`, 대화 기록 모델 `claude-opus-5-5`만, backend unknown). 보고 `pretest-01/report.md`(sha256 `9c864457…cb09`), 리드 수락 `lead/accept-pretest-01.md`. 기준 head `429481c2`에서 batchmode 일곱 번을 돌렸다(메인 창 `msg_46325a7455d4`, 마지막 Unity 종료 15:02:36Z, 반환 `msg_6df563795cf9`).

| lane | 무엇을 주나 | 전체·통과·실패·건너뜀 |
|---|---|---|
| L0 | 없음(전체) | 230·188·23·19 |
| L1 두 번 | 캡처 변수 | 28·14·14·0, 두 번 같음 |
| L2 | 캡처·memory·permit·token·Task·Dispatch | 3·1·2·0 |
| L3a | 서버 2건 class 이름 filter(실서버 없음) | 4·0·4·0 |
| L3b | 서버 2건 정확한 이름(실서버 없음) | 3·0·3·0 |
| L4 | 틀린 캡처 경로·틀린 permit 경로 | 28·7·21·0 |

- L0 실패 집합이 대상 23건과 같다(`red` 통과). 같은 L0을 전후에 넣은 `full`은 결함 이름 23개로 실패한다(구현 전 RED). L1 두 번은 정규화 없이 같았고, `skips` 일곱 번과 U4 ⓪ 17건 대조는 통과다.
- class 이름 filter는 Explicit 시험도 고른다(L3a 탐침 「고름」). 그래서 서버 2건을 Explicit로 바꿔도 이름으로 고른 실행에서는 서버가 없으면 지금처럼 실패한다.
- 판정 식 고정: freeze-v1 `4b3e429b…71db`(Unity 전), freeze-final `1e8abaa7…8034`(v1 뒤 바뀐 tools 0개, 사양은 기준 원시에서 옮긴 칸만). 리드가 따로 다시 재서 같았다(`lead/accept-pretest-check-01.json`).
- 일곱 실행의 실패 81행 분류: (a) 0, (b) 46, (c) 0, (d) 35. (d)는 전제를 준 lane의 잉크·글자 판정 34행과 L2 `OverlayAtTarget_1920x1080`의 창 위치 미복원 1행이다. 단정은 하나도 바꾸지 않았다.

## 구현 결과

작성 신규 `[Effect 구현 Opus]`(Task `task_860ffe449ef4`, 대화 기록 모델 `claude-opus-5-5`만, backend unknown). 보고 `impl-01/report.md`(sha256 `d93c94f4…cca0`), 리드 수락 `lead/accept-impl-01.md`. 시험 네 파일 +28줄 −4줄과 `DEVELOPMENT.md` 새 절(+13줄) 「Unity PlayMode 실행 전제」다(diff sha256 `d5cdd4cd…4e1a`). 같은 고정 runner로 바꾼 head를 여섯 번 돌렸다(메인 창 `msg_aff3c4e49fde`, 마지막 Unity 종료 16:03:04Z, 다시 돈 횟수 0, 반환 `msg_d7ab2edce401`).

| lane | 기준 전체·통과·실패·건너뜀 | 바꾼 뒤 | 바뀐 시험 |
|---|---|---|---|
| L0 | 230·188·23·19 | 230·188·0·42 | 대상 23건만. 21건 Ignored, 서버 2건 Explicit |
| L1 | 28·14·14·0(두 번) | 28·14·11·3 | `OverlayAtTarget_*` 3건이 permit 이름으로 건너뜀 |
| L2 | 3·1·2·0 | 3·1·2·0 | 없음 |
| L3a | 4·0·4·0 | 4·0·4·0 | 없음 |
| L3b | 3·0·3·0 | 3·0·3·0 | 없음 |
| L4 | 28·7·21·0 | 28·7·20·1 | `DefaultScreen_ApprovedValues`가 「Editor Game 화면 필요」로 건너뜀 |

- 고정 판정기: `full` 결함 0, `lane` 일곱(L1은 기준 두 번 각각)·`u4` 통과다. 바꾼 L0을 L1 `skips`에 넣으면 결함 17개로 실패한다(완료조건 3).
- 리드가 판정기 없이 원시를 다시 읽어 같은 결과를 얻었다(`lead/r2-after-raw-02.json`). 고정 도구 35개와 freeze 기록 둘도 수락 때와 같다(`lead/r2-tools-after-01.json`).
- 바꾼 뒤 여섯 실행의 실패 40행은 모두 기준 분류와 같은 행이다. (b) 16, (d) 24, (a)·(c) 0이다. 값이 틀린 L4에서 캡처 6건은 `IOException`, `OverlayAtTarget_*` 3건은 `ArgumentNullException`으로 실패를 유지했다(Content 조건 1).
- 기본 화면 시험의 GameView 부작용: 뒤 시험의 view 줄이 기준과 같았다(리드 결정 7의 배치).

## 독립 검증 결과

검증 신규 `[Effect 검증자]`(`gpt-6-astra` xhigh, Task `task_c30014122ad5`, rollout turn_context 두 번 모두 `gpt-6-astra`·xhigh, backend unknown). 판정 `verify-01/verdict.md`(sha256 `309ecc70…cbcc`)는 **PASS(전제 분류 변경과 결과 보존 범위)**이고 차단 결함 번호는 없다. 머리 문장은 「(d) 35건은 기준과 같은 기존 실패로 남음, 통과 아님」이다. 검증 head `37dadbec`에서 같은 고정 runner로 Unity를 여덟 번 돌렸다(메인 창 `msg_a485ab2d0091`, 마지막 Unity 종료 17:03:16Z, 반환 `msg_98ba81a275b5`).

| 실행 | 무엇을 주나 | 전체·통과·실패·건너뜀 | 구현 원시와 비교 |
|---|---|---|---|
| verify-L0 | 없음(전체) | 230·188·0·42 | 같음 |
| verify-L1 | 캡처 변수 | 28·14·11·3 | 같음 |
| verify-L2 | 캡처·memory·permit·token·Task·Dispatch | 3·1·2·0 | 같음 |
| verify-L3a / L3b | 서버 2건 이름 filter(실서버 없음) | 4·0·4·0 / 3·0·3·0 | 같음 |
| verify-L4 | 틀린 캡처 경로·틀린 permit 경로 | 28·7·20·1 | 같음 |
| extra-whitespace(검증자 추가) | 캡처 변수에 공백만 | 28·7·11·10 | 같은 조합의 기준 실행 없음 |
| extra-permit-only(검증자 추가) | permit만, 캡처 없음 | 3·0·3·0 | 같은 조합의 기준 실행 없음 |

- 고정 판정기: `full` 결함 0, `lane` L1(기준 둘)·L2·L3a·L3b·L4·`u4` 모두 exit 0이다. 음성 검사는 바꾼 L0→L1에서 17건, L1·L0→L2에서 각 3건, 공백 lane→L1에서 6건을 실제로 잡았다.
- 실패 54행의 최종 구분: 고정 lane 기대 실패 27(틀린 값 L4 20, 서버 없음 L3a·L3b 7), 추가 lane 기대 실패 14(공백 11, permit만 3), 원인 미확정 13(L1 잉크 11, L2 2)이다. 처음 (d) 35로 묶은 행 중 L4·공백 22행은 전체 message에 「no capture folder」가 있어 기대 실패로 옮겼다. 공백 11행은 같은 조합의 기준 실행이 없어 base-L4의 같은 이름·첫 문구를 참고만 했다. 리드가 메인에 쓴 「35건 모두 같은 lane」(`msg_a62bd59fda05`)은 이 11행에서 틀렸고, 검증자가 바로잡았다(`msg_c35e6f6a60ae`).
- 원인 미확정 13행은 모두 기준 같은 lane에서 같은 fullname·result·label·첫 문구로 실패했다. 통과로 바꾸지 않고 사용자 처분 대기다(메인 `msg_e0d1d3c3723f`). 사용자 처분은 결정 73 A(실패로 두고 후속 후보)다.
- 설계 관찰(비차단): O1 빈 값 판단식 차이, O2 실서버 lane의 `Recorder`(둘 다 남은 우려), O3 `judgeSkips`(:231)는 주지 않은 전제 낱말 하나만 이유에 있어도 허용한다(고정 식의 기록된 한계, `lead/accept-pretest-01.md` 5번), O4 기존 잉크·창 위치 실패.
- 절차 이탈 셋은 모두 메인 결정으로 비차단·기록이다(위 「독립 검증 중 메인 처분」): 선행 작성자의 빈 `node -e` 1회, 검증자의 heartbeat 436.621초 공백과 기록 밖 읽기 1회다. 리드도 명령 앞에 빈 `node -e`를 두 번 섞었다(`lead/verify-launch-record-01.md`).
- 리드 원천 대조: 판정기 없이 여덟 원시를 다시 읽었다. 수치, base·after 대비 바뀐 시험, 13행의 기준 일치, 22행의 「no capture folder」, L2 1920 창 위치가 판정서와 같았다(`lead/r2-verify-raw-01.json`). 고정 도구 35개와 freeze 기록 둘도 같다(`lead/r2-tools-verify-01.json`). 검증 뒤 HEAD는 `37dadbec`, git status는 빈 값이다. Unity가 바꾼 알려진 drift 두 파일만 고정 도구로 복원했고 GameViewSizes·Shortcuts는 같다.
- 미실행: 실서버, Editor 대화형(Game 화면 시험), 직접 플레이, DB, 별도 빌드, CI.

## 남은 우려

- `DefaultScreen_ApprovedValues`는 Editor 대화형 실행에서만 판정된다. 지금 lane은 모두 batchmode라 사실상 돌지 않는다(Content 조건 3). 바꾼 뒤에는 view가 없으면 「Editor Game 화면 필요」로 건너뛴다. batchmode에서도 앞 시험이 만든 view가 남으면 판정된다(바꾼 L1).
- `GuildLiveServerWindowTests`(:108)도 GameView event sink로 `Recorder`를 만든다. 실서버 lane이 `DAWNHOLDER_LIVE_CAPTURE_DIR`만 주고 `DAWNHOLDER_GUILD_VERIFY_CAPTURE_DIR`를 빠뜨리면, 그 역할 시험이 실패 대신 건너뛴다. 실서버 lane은 이 goal 밖이고 크리티컬은 아니다. 그 lane 판정에 skipped 0을 넣으면 막힌다. 주인 Content 리드가 받아 Content BACKLOG 후속 후보로 올린다(`msg_8f2d5b2da854`).
- 전제를 주는 lane이 변수를 빠뜨리는 실수는 시험이 알 수 없다. 문서 규칙과 계약 검사에 기댄다. runner가 Git 밖에 있어 다음 세션이 새 runner를 쓰면 검사가 빠질 수 있다.
- 서버 2건의 실서버 실행은 이 goal에서 하지 않는다. 이름 filter 선택만 보인다.
- 잉크 10건의 판정 실패는 그대로 남는다. 검증자 실행에서 원인 미확정으로 남은 기존 실패는 13행(L1 잉크 11, L2 2)이고, 모두 기준과 같으며 통과가 아니다. 결정 73 A로 이 13행은 실패로 두고 후속 후보로 간다. BACKLOG 후보는 시험 주인 Content가 쓴다.
- permit lane(L2) 기존 실패 2건은 원인 미확정이고 Content 후속이다(메인 `msg_e0d1d3c3723f`). `OverlayAtTarget_1920x1080`은 잉크가 아니라 「memory return: original position returned」로 실패한다(기대 x:0,y:26, 실제 x:599,y:237). 기준·구현·검증 세 번 모두 같은 자리였고, Editor 창 배치 부작용인지 확인하지 않았다. `OverlayAtTarget_802x451`은 C8/C9 잉크 판정 실패다.
- Recorder는 `IsNullOrWhiteSpace`, 장면 시험 helper는 `IsNullOrEmpty` — 빈 값 판단식이 두 곳에서 다름(기준 head부터, Content 코드). 캡처 변수가 공백만이면 `GuildGameplayWindowTests` 6건은 건너뛰고 장면 시험은 판정까지 가서 실패한다(검증자 추가 lane 28·7·11·10). 캡처 lane 판정은 그 6건을 결함으로 잡는다. 통일은 Content 후속 후보다(Content `msg_5e4d2d7151f9`, 메인 `msg_a2b82a34e33f`).
- 잉크 판정 결과가 lane이나 실행 순서에 따라 달라질 수 있다. `RenderTextureAuxiliary_709x399`가 L1에서는 통과, L4에서는 실패했다. 전후 비교는 같은 lane끼리만 한다.

## 메인 초안과의 차이

| 번호 | 초안 | 이 goal | 이유 |
|---|---|---|---|
| 1 | 서버 2건도 「Ignored + 빠진 전제 이름」, 실서버 lane 전제 | `Explicit(이유)`, 원시는 Skipped·`Explicit` | 기존 실서버 lane 선례와 파일 머리 주석. 서버가 죽은 lane이 조용히 넘어가지 않는다(Content 조건 4) |
| 2 | 장치를 runner에 두거나 기존 위치를 보인다 | 문서의 실행 전제 안내 + 이 goal harness의 skipped 0 검사(메인 보충: 새 작업 규칙 아님). 저장소 runner는 다음 후보 | runner가 모두 Git 밖(Content 의견과 같음). 교정 층 이유는 리드 결정 2 |
| 3 | 캡처 lane은 U4 ⓪(7/10)과 대조, permit lane은 전 원시와 같다 | 둘 다 기준 head 같은 명령 전후 비교를 기준으로 하고, U4 ⓪ 대조도 함께 한다. 다른 전제가 빠진 시험의 건너뜀은 표로 남긴다 | U4 ⓪은 다른 head의 실행이다 |
| 4 | Game 화면 시험의 전제 실행이 정해지지 않음 | 정적 diff + 「Editor 대화형 미실행」 | 대화형 Editor 창을 따로 잡는 비용. 건너뜀 조건은 view가 없을 때만이다 |
| 5 | 「시험 실행 안내 문서 한 문단」 | `DEVELOPMENT.md` 새 절(변수 표 + 실행 전제 안내 문단) | 저장소 문서에 이 변수 이름이 없다 |
| 6 | CURRENT 언급 없음 | 이 PR에서 고치지 않는다 | PR283과 같은 Effect 줄 |
| 7 | 선행 시험 형태 언급 없음 | 선행 시험 = lane 판정 harness(Git 밖), 새 시험 클래스 없음 | 환경 변수는 프로세스 전체 값이다 |
| 8 | 잉크 11건 「전체 실행에서는 unjudgeable로 실패」, ApprovedValues 「batchmode라 Game 화면 없음」 | 판정 단계 전에 빈 값만 건너뜀, 폴더 부재는 실패 유지. Game 화면은 batchmode가 아니라 view 부재로 판단. 전제가 둘 빠지면(ApprovedValues) 이유에 둘 다 적음 | Content 조건 1·2, UpperBound 원시 |
| 9 | 서버 2건 주인 언급 없음 | 주인 파트 미정으로 적고, 생긴 goal을 남긴다. 메인이 마지막 보고에 올린다 | Content 리드가 자기 파일 아님이라 함. Core 리드는 열려 있지 않음(메인 처분) |

## 다음 계획 후보

- 저장소 안 PlayMode lane runner(변수·permit 준비, 결과 XML의 skipped 0 검사 내장). 지금 runner가 근거 폴더마다 따로 있어 같은 책임의 임시 runner가 복제된다(하네스 원칙 2). BACKLOG `playmode-lane-runner-in-repo`.
- 원인 미확정 기존 실패 13행과 Content 몫의 남은 우려(빈 값 판단식 통일, 실서버 lane의 `Recorder`)는 시험 주인 Content가 BACKLOG 후보를 쓴다(결정 73 A, Content `msg_8f2d5b2da854`·`msg_5e4d2d7151f9`). Effect는 행을 만들지 않는다. 도착 위치는 [Content 로그인 화면·길드 창 goal](../2026-10-30-login-screen-guild-window/goal.md)이다. 10-31 리드 확인 때 Content는 자기 종료 기록 branch(`docs/content-login-guild-closeout-20261031`, 아직 병합 전)에 `guild-ink-undetermined-failures`·`guild-capture-blank-predicate`·`capture-lane-skip-guard` 행을 쓰는 중이었다. `capture-lane-skip-guard`는 위 `playmode-lane-runner-in-repo`의 skipped 0 검사와 겹치니, 둘 다 병합된 뒤 Rules나 메인이 묶을지 정한다.

## 결과

- PR #286 병합: 2026-10-31T07:54:37Z, head `bf044c7235d0a956b019b245ca366a3833363980`, 병합 commit `9cc80a9d2b8cf17766831e8f93e06a6322237d33`. 사용자 승인 줄을 받아 메인이 병합했다(메인 `msg_20097f42c3e3` 1). 리드는 병합 뒤 기록으로 base가 main이고 병합된 head가 PR 생성 때와 같음을 확인했다(`lead/pr-state-16.json`). base가 바뀐 뒤 병합 전까지는 확인하지 못했다. 앞 PR 둘(PR281·PR285)과 이 PR이 30초 안에 차례로 병합됐기 때문이다(07:54:07Z~07:54:37Z).
- PR282와 함께 고친 `InventoryServerIntegrationTests.cs`는 충돌 없이 둘 다 들어갔다. origin/main `dd9e755b`에 이 PR의 `Explicit` 속성(43행)과 PR282의 왼쪽 Ctrl 공격(109~116행)이 함께 있다.
- 완료조건 대조:

| 조건 | 결과 | 근거 |
|---|---|---|
| 1 전체 실행 failed 0 | 충족. 230·188·0·42, 21건 Ignored와 빠진 전제 이름, 서버 2건 Skipped·`Explicit` | [독립 검증 결과](#독립-검증-결과) verify-L0 |
| 2 전제를 준 실행의 결과 보존 | 충족. 다섯 lane 모두 구현 원시와 같고 기준 head와 같은 명령 전후 비교가 판정서에 있다. Game 화면 시험은 Editor 대화형 미실행, 서버 2건은 「Timed out: handshake」 실패 유지 | 같은 절 표, `verify-01/verdict.md` |
| 3 조용한 통과 막음 | 충족. `DEVELOPMENT.md` 실행 전제 안내, 바꾼 전체 실행을 캡처 lane 판정에 넣으면 결함 17건 | 같은 절 |
| 4 기존 단정 변경 절차 | 충족. 분류 (b) 환경, 줄 단위 표와 같은 명령 전후 원시 | [선행 시험 결과](#선행-시험-결과구현-전-red)·[구현 결과](#구현-결과) |
| 5 TDD 순서와 강 등급 PASS | 충족. 선행 Opus → 구현 Opus → 검증 Astra PASS | 같은 절들 |
| 6 병합 승인 | 충족. 사용자 승인 줄 제출 뒤 메인 병합 | 위 병합 기록 |

- 통과가 아닌 것: 원인 미확정 기존 실패 13행은 실패 그대로다(결정 73 A). 실서버·Editor 대화형·직접 플레이·DB·별도 빌드는 미실행이다.
- 생존 신호 정산: 단계마다 정본 helper로 재지 않았다. 종료 때 리드가 이 Run의 저장 우편함 원시 전체를 정본 helper에 넣어 쟀다(근거 폴더 `2026-10-30-kill-reward-float-text/lead/closeout-01/liveness-select-01.json`).
  - 선행 작성자: 신호 32회, 최대 240초. 구현자: 21회, 최대 298초. 300초 초과는 없다.
  - 검증자: 26회, 300초 초과 1구간 437초(2026-10-30 17:06:43Z→17:14:00Z, heartbeat→heartbeat)다. 이유는 Unity 반환 뒤 판정서 작성과 문맥 정리였다(`verify-01/verdict.md` 166행). 검증자가 스스로 신고한 436.621초 공백과 같은 구간이고, 메인 처분은 비차단·기록이다(`msg_b0795f230724`).
- 종료 Gardener: 결정 76 A로 세 goal에 하나를 두었다(정본과 다른 이번 한 번의 사용자 결정 예외). 이 goal에서 차단 관찰은 0이고, 리드 반환 GR-01(다음 계획 후보 둘째 줄의 도착 표시)과 GR-05(생존 신호 이유)를 처분했다. 설계 관찰 O1(`DEVELOPMENT.md` 실행 전제 표가 23건만 담는데 범위를 말하지 않음)은 이 종료 기록에서 고치지 않았다. 결과와 처분 전체는 [개선안 3 goal의 종료 Gardener 절](../2026-10-30-kill-reward-float-text/goal.md#종료-gardener--2026-10-31결정-76-a)과 [보고서](../2026-10-30-kill-reward-float-text/gardener.md)다.
