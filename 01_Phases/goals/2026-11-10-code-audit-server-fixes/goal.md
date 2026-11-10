# 전체 코드 감사 서버 묶음 수정과 실행 검증

- 파트: Core · 리드 `[Core 리드 Opus]`(claude-opus-5-5 xhigh)
- 상태: **진행**, 사용자 계획 승인(176 A). 입력 예산 독립 검증 단계.
- 기준: main `acd5a7db065b37baa7f491c4502002eb8ce184a3` · branch `fix/core-audit-connection-20261110`
- 근거 폴더: `.backups/verification/2026-11-10-code-audit-server-fixes/`
- 초안 작성 시각: 2026-11-09T22:35Z(`date -u` 관측, 11-10 07:35 KST)

## 재개 지점

**다음 첫 일:** 종료 기록 PR의 병합 승인을 받는다. 병합 뒤 종료 점검과 R-8로 이어가고, 메인이 사용자와 결과·남은 위험·BACKLOG·다음 계획을 확인한다. 다음 goal은 자동으로 시작하지 않는다. 제품 PR305~PR309는 모두 병합됐고 통합 실행 검증도 끝났다(「통합 실행 검증 결과」).

막힌 지점: 없음. 근거 메모는 근거 폴더의 `lead-context.md`, 선행 시험 계약과 원시는 `tdd/`, PR1·PR2 계약과 원시는 `pr1/`·`pr2/`이다. PR4의 선행 시험은 `tdd/held/`에 두었고 그 PR을 시작할 때 hash를 대조해 되돌린다.

## 적용 중인 사용자 결정

### 2026-11-10 결정 183 — Codex 없이 Claude Opus(메인 전달 `msg_03ee2dce17ef`)

- 사용자 원문(11-10 18:4x KST 메인 pane 제출, 메인 전달): 「대시보드 결정 응답: 2) 183 - Codex가 GPT Astra 모델을 거부함: 계정을 살릴지, Codex 없이 갈지 → C 코멘트 「구독이 끝났나보네, claude Opus로 해야할 거 같은데」」
- 메인의 적용 해석(사용자 문장이 아니다): Codex 세션을 새로 열지 않는다. 독립 검증자는 `claude --model claude-opus-5-5 --effort xhigh --autocompact 250000`(`[Core 검증자]`), Gardener는 `claude --model claude-opus-5-5 --effort high`(`[Core Gardener Opus]`), 구현자·선행 시험 작성자는 `claude --model claude-opus-5-5 --effort high`(`[Core 구현 Opus]`·`[Core 시험 Opus]`)이고 같은 계약의 두 번째 독립 불합격 뒤 구현 시도만 xhigh다. 구현자와 검증자의 분리, 작업 하나 뒤 정산·종료, 리드의 Git 담당, 병합 관문은 그대로다.
- 거부 문구는 `The 'gpt-6-astra' model is not supported when using Codex with a ChatGPT account.`다. 원인은 확인하지 못했고 구독 종료는 사용자의 짐작이다. 메인 확인: 08:55:23Z의 한도 기록은 plan_type pro, 주간 93%, 크레딧 있음, rate limit null이다.

### 2026-11-10 사용자 지시 — Codex 작업자 모델 변경(메인 전달 `msg_ccc1e4849cb4`)

- 사용자 원문(11-10 08:4x KST 메인 pane 제출, 메인 전달): 「codex는 리셋권 2장있으니까 걱정마」, 「모든 Codex 모델은 작업자는 Astra High, 검증자는 Astra xhigh로 잡아줘」
- 메인의 적용 해석(사용자 문장이 아니다): 새로 여는 세션부터 구현자·선행 시험 작성자·Gardener는 `codex --model gpt-6-astra -c model_reasoning_effort=high --disable fast_mode`(태그 `[Core 구현 Astra]`·`[Core 시험 Astra]`·`[Core Gardener Astra]`), 독립 검증자는 `gpt-6-astra` xhigh(`[Core 검증자]`). 같은 계약 두 번째 독립 불합격 뒤 다음 구현 시도는 Astra xhigh 유지. 이미 떠 있는 세션의 모델은 바꾸지 않는다.
- 함께 바뀐 것(메인): Codex 잔량은 제약이 아니다. `msg_a59b8eafa5ed` 4번의 크레딧 실측 보고는 취소됐다. 불합격이 나면 예산을 이유로 범위를 줄이지 않고 새 세션으로 다시 돌린다. Claude 주간 사용량이 08:30 KST에 87%라 20:00 KST 전까지 큰 파일 재읽기와 빈 턴을 줄인다.
- PR1 구현자는 지시 전에 뜬 `GPT-6.1-Sol max`로 끝까지 진행했다(첫 화면 관측, backend unknown).

### 2026-11-10 결정 176 A — 계획 승인(메인 전달 `msg_2fd9d770d23b`)

- 사용자 원문(메인 pane에서 Enter로 제출, 메인 전달): 「대시보드 결정 응답: 1) 계획 검토 - Core 176 - 코드 감사 서버 묶음 8건 수정 계획 → A 승인 (초안 msg_3f93976f1ff9)」
- 사용자가 읽고 승인한 글: `.backups/verification/2026-11-10-code-audit/plan-review-176-main-summary.md`(SHA256 `8027c19160c9b1ccc40e2ce606b90fd4125999ae4593c7d24aa9ffb58a2dad24`, 메인 정리본). 이 글에는 초안에 없던 「메인 대조와 판단」 절이 있다.
- 메인 대조: 초안 SHA256 `da3b786a6f5e61be1de0ff2c0381d7f9d8f8627c6f0a3d9187d00130986543c7` 일치, 범위 8건이 승인 범위 문서와 일치.
- 메인이 전달한 판단 요청의 답(정리본에 적혀 승인에 포함): 정책값은 제안대로. 이동·공격·스킬·메뉴·핑 밖의 요청도 공유 예산에 넣는다. 메인의 클라이언트 확인: 공격은 키 누름 한 번에 한 번(`LocalPlayerInput.cs:74`), 스킬은 누른 프레임에만(같은 파일 110-119).
- 진행 조건: 32 MiB 상한은 PR1 봇 실측 뒤 측정 원시값과 최종값 제안을 함께 보고한다. 올리는 쪽뿐 아니라 낮추는 쪽도 검토한다. 값을 바꾸면 이 문서에 측정 근거와 함께 적는다. 7번의 클라이언트 쪽 확인(`PartyUpdateHandler.cs:30-35`)은 메인의 관측이므로 검증자 계약에 사실로 넘기지 않고 직접 보게 한다. `sync-wsl.sh`·패킷 정의·ID·프로토콜 버전 변경이 필요해 보이면 구현 전에 메인에 올린다.
- 선행 시험 계약 조건: 한 세션이 8건을 PR 순서(1·3·4·6 → 2 → 5 → 7·8)로 쓴다. 항목마다 실패 원시와 중간 기록을 저장한다. 재현 못 한 항목은 시도와 관측을 적고 넘어간다. 통과 시험을 실패로 적지 않는다. 제품 계산 복제 시험은 통과 차단이다.
- 메인 보고 의무: 선행 시험 뒤 항목별 재현·제외와 오늘 끝낼 수 있는 PR 수, Unity 창·DB 서버 요청은 필요 시점 전에, PR마다 검증자 PASS 원문 경로와 정확한 head 40자, 단계가 넘어갈 때 한 줄.

### 2026-11-10 결정 174 A·175 A — 감사 수정 범위와 착수

메인 메시지 `msg_2b39dd31f646`(메인 전달이며 사용자 직접 입력으로 격상하지 않는다)이 아래를 전달했다. 원문 전체는 범위 문서 `.backups/verification/2026-11-10-code-audit/fix-scope-server-core.md`(SHA256 `af552d68f81a2cfe661ef21084e143d1f423f85fe32e9cf9359fd1cd458eeb54`)에 있다.

- 감사 지시 원문에서: 「이후에 실제로 리팩토링 이후에 정상적으로 돌아가는지 까지 실행 검증까지 완료하는걸 목표로 하자」, 「마일스톤급 작업으로 인지해줘」.
- 「대시보드 결정 응답: 1) 174 - 전체 코드 감사 1차 수정 범위(심각도 높은 결함) → A 여섯 묶음 34건 · 2) 175 - 감사 수정 착수 시점(지금 또는 20:00 리셋 뒤) → A 지금 서버 묶음부터」
- 사용자가 고른 결정문의 진행 방식(메인이 쓴 글): 「모든 항목은 실패하는 시험을 먼저 만든 뒤 고친다. 실패를 재현하지 못한 항목은 범위에서 빼고 목표 문서에 기록한다.」, 「고친 뒤 빌드, 서버 시험, Unity 시험, 자동 접속 봇, DB 저장 실행까지 실제 실행으로 확인한다. PR마다 병합 승인 한 줄을 따로 받는다. 그래서 11-11 전에는 main에 넣지 않게 조절할 수 있다.」

메인이 같은 메시지로 정한 것: 작업자 배정(아래), Codex 작업은 11-10 안에 끝나는 순서로 잡고 못 끝낼 항목이 보이면 모델을 바꾸지 말고 미리 알린다, 패킷 정의·ID·프로토콜 버전 변경이 필요해 보이면 구현 전에 올린다.

| 역할 | 기동 명령 | 태그 |
|---|---|---|
| 선행 시험 작성자 | `codex --model gpt-6-astra -c model_reasoning_effort=xhigh --disable fast_mode` | `[Core 시험 Astra]` |
| 구현자 | `codex --model gpt-6.1-sol -c model_reasoning_effort=max --disable fast_mode` | `[Core Sol]` |
| 독립 검증자 | `codex --model gpt-6-astra -c model_reasoning_effort=xhigh --disable fast_mode` | `[Core 검증자]` |

## 적용 중인 메인 결정

### 메인 `msg_a59b8eafa5ed` — 선행 시험 보고 회신

메인이 원시 표본을 대조해 일치를 확인했고(기준선·최종 요약 줄, 실패 클래스 분포, 종료 신호 exit), PR1 진행에 동의했다.

1. 6번 제외는 승인 계획대로라 사용자에게 다시 묻지 않는다.
2. 8번 사망 사례 제외에 동의한다. 선행 시험 파일 수정은 TDD 정본의 선행 시험 수정 절차를 따르고, **PR4 검증자 계약에 그 변경을 대조 대상으로 넣는다.**
3. 2번 시험 가운데 공유 예산·창 경과 사례는 실제 시간에 기댄다(`tdd/report.md` 49·58행). **PR2 검증자 계약에 그 클래스의 반복 실행을 넣어** 불안정 여부를 본다. 불안정하면 결함 번호로 받는다.
4. PR1 구현·검증 세션의 Codex 크레딧 실측 보고. **`msg_ccc1e4849cb4`로 취소됐다.**

### 메인 `msg_076badf9a3ef` — PR1 구현 보고 회신

1. 연결별 메모리 상한은 **32 MiB로 확정**한다. 메인 추정(미실측): 실제 전송 바이트는 작아 커널 송신 버퍼가 먼저 흡수하므로 대기열은 상대가 오래 읽지 않을 때만 쌓이고 짧은 지연으로는 상한에 닿지 않는다. 측정 한계: 실측 최대 65,535바이트·1조각은 읽기를 멈춘 상대가 없는 loopback 봇의 값이다.
2. 기존 시험 실패 2건은 PR1 보고에 분류 표로 넣는다. 메인이 승인 요청 전에 원천과 대조한다.
3. 봇 기준선 실패 2건을 기준선 실패로 적고 다음 계획 후보로 올린다. 원인 조사는 이 goal에서 하지 않는다. 이후 PR의 봇 판정은 수정 전후 같은 결과로 보고, 대시 피해·보스전 경로는 봇으로 확인되지 않았다고 PR 보고에 적는다.

### 메인 `msg_e4e30db48dac` — PR306 대조 회신과 예산 밖 경로

메인이 PR306 판정·원시·제품 diff를 대조했고 차단 사유는 없다. 제품 코드에서 예산이 걸리지 않은 경로 둘을 찾았고, 항목 2 범위 안 결함으로 본다. PR306이 만든 회귀는 아니다.

- **등록되지 않은 패킷 번호.** `GameSession.OnRecvPacket`의 else 가지가 패킷마다 「Unknown PacketId … dropped」를 찍는다. 핸들러가 없어 예산을 지나지 않는다.
- **중복 로그인.** `LoginHandler.BudgetKind`가 None이라 로그인한 연결이 `C_Login`을 반복하면 패킷마다 콘솔 두 줄을 찍는다.

처리: PR306을 고치지 않고 다른 PR로 올린다. 선행 시험 → 구현 → 새 검증자 순서이고, 재현되지 않으면 빼고 기록한다. PR 경계와 순서는 리드가 정한다. 설계는 리드 판단이며 메인 의견은 「등록되지 않은 번호는 그 밖 공유 예산을 함께 쓰고, 로그인에는 메뉴 예산을 준다」이다. 핸드셰이크 전 경로와 DEBUG 전용 치트는 대상이 아니다.

리드 판단: 메인 의견을 따른다. 구현 계약의 Login None에 별도 근거는 없었고, 정상 로그인은 연결당 한 번이라 메뉴 예산에 걸리지 않는다. 순서는 아래 표와 같이 종료 경로 PR 뒤다.

## 범위

### 만들 것

| 번호 | 결함 | 고친 뒤의 동작 |
|---|---|---|
| 1 | 송신 대기열에 상한이 없다 | 연결마다 대기·진행 중 송신량이 상한을 넘으면 그 연결을 끊는다. 패킷을 골라 버리지 않는다. 끊긴 뒤의 송신은 쌓지 않는다. |
| 2 | 이동 외 입력에 빈도 제한이 없다 | 수신 경계에서 연결별·입력 종류별 예산을 적용하고, 초과분은 틱 큐·핸들러·입력별 콘솔 로그에 닿기 전에 버린다. 판정은 계속 틱이 한다. |
| 3 | 송수신 등록 예외 뒤 연결이 남는다 | 등록·완료 처리의 예외는 중복 호출에 안전한 연결 종료로 이어진다. |
| 4 | 수신 즉시 완료 때 상호 호출한다 | 즉시 완료를 반복문으로 처리해 콜스택이 자라지 않는다. |
| 5 | 종료 신호가 정상 종료로 이어지지 않는다 | SIGINT·SIGTERM이 기존 Enter 경로와 같은 `host.Stop()`을 거쳐 영속 마무리 후 종료한다. |
| 6 | 부분 송신을 검사하지 않는다 | 재현되면 전송량이 대기 합보다 적은 경우의 처리를 한 군데에 둔다. 재현하지 못하면 범위에서 뺀다. |
| 7 | 맵 이동 공백에 파티 해산 통지가 빠진다 | 재현되면 도착 뒤 현재 파티 상태를 권위로 한 번 통지한다. 재현하지 못하면 범위에서 뺀다. |
| 8 | 착지 점프 버퍼가 이동 잠금을 우회한다 | 재현되면 잠금 틱에는 버퍼가 점프를 발사하지 않는다. 재현하지 못하면 범위에서 뺀다. |

### 건드릴 곳

- `02_Server/Network/Session.cs`와 `GameServer.Tests/Network`의 연결 계층 시험.
- `02_Server/GameServer/Sessions/GameSession.cs`, `Sessions/IntentRateLimiter.cs`, `Handlers/IPacketHandler.cs`와 핸들러의 예산 종류 선언, 해당 시험.
- `02_Server/GameServer/Program.cs`, 필요하면 `Hosting/` 아래 종료 신호를 받는 작은 단위, 해당 시험.
- `02_Server/GameServer/Maps/Transitions/MapMigration.cs`, `Party/PartyFlow.cs`·`PartyNotifier.cs`, `Entities/PlayerEntity.cs`, `Maps/Systems/PlayerPhysicsSystem.cs`, 해당 시험.
- 계약이 바뀐 현재 문서의 해당 구간(`00_Document/domains` 등, 구현 계약에서 특정), 이 goal, CURRENT의 Core 줄, `BACKLOG.md`의 `server-lane-graceful-stop` 행 처리.

`99_Tools/sync-wsl.sh`는 바꾸지 않을 계획이다. `stop_server`가 이미 `kill`(TERM) 뒤 `wait`를 하므로, 서버가 TERM을 정상 처리하면 스크립트는 그대로 맞는다. 필요해지면 구현 전에 메인에 올린다.

### 하지 않을 것

- 감사의 다른 다섯 묶음과 `MageRangedCombatTests.cs:346-362` 복제 계산 시험(시험 신뢰도 묶음).
- 중간·낮은 심각도 발견(`AttackHandler.cs:21` 등 범위 문서에 적힌 것).
- 의도된 선택으로 판정된 동작: 이름만으로 로그인(155 A), Debug 치트 기본값(16 A), 길드 비영속(144 A), `RosterTransitionBuffer` 상한, 영속화 절대 deadline·자동 탈취 금지.
- 패킷 정의·패킷 ID·프로토콜 버전, 생성 코드(`GenPackets.cs`)와 생성기. 구현에 꼭 필요해 보이면 구현 전에 메인에 올린다.
- 서버 시작 전(영속 초기화 중) 신호 처리, 포트 설정 도입, 틱 큐 자체의 상한.

### 관찰 가능한 완료조건

1. 범위에 남은 항목마다 수정 전 코드에서 실패한 시험과 수정 뒤 같은 명령의 통과 원시가 근거 폴더에 있다. 재현하지 못해 뺀 항목은 시도 내용과 함께 이 문서에 있다.
2. 비읽기 상대에 계속 보내면 연결이 끊기고 대기량이 상한 안에 머문다(실제 소켓).
3. 예산을 넘는 공격·스킬·메뉴·핑·그 밖의 요청은 틱 큐에 들어가지 않고, 정상 빈도의 입력과 이동 500/s 예산은 그대로다.
4. 실제 서버 프로세스에 SIGTERM·SIGINT를 보내면 `Server stopped.`를 찍고 exit 0으로 끝나며, Stored 모드에서 종료 뒤 재시작하면 저장값이 이어진다. Enter·EOF 종료와 `Stop` 실패 시 exit 1은 그대로다.
5. 빌드, 서버 시험, 자동 접속 봇, Unity 시험, DB 저장 실행을 아래 표대로 실제 실행했고, 실행하지 않은 것은 이유와 함께 미실행으로 적혀 있다.
6. PR마다 새 독립 검증자의 PASS 판정 원문이 있고, 사용자 병합 승인 한 줄을 PR마다 따로 받았다.

## 설계 선택과 메인 제안 대조

범위 문서의 메인 설계 제안을 소스와 대조했다. 다른 점만 적는다.

- **1번의 메모리 계산이 다르다.** 생성 코드의 `Write()`는 패킷마다 `new byte[ushort.MaxValue]`를 만들고 앞부분만 가리키는 조각을 돌려준다(`GenPackets.cs:176` 부근). 그래서 18바이트 `S_Pong` 하나가 대기열에서 65,535바이트를 붙든다. 상한은 전송 바이트가 아니라 **조각이 붙든 배열 크기의 합**과 개수로 센다.
- **핑의 입력 길이 검사는 필요 없다.** `C_Ping`은 고정 길이 `long` 하나만 읽는다(`PingHandler.cs:13-19`). 핑 폭주는 2번의 핑 예산과 1번의 상한이 막는다.
- **2번의 위치는 `OnRecvPacket`의 공통 관문이다.** 22개 핸들러의 `Submit*`마다 넣지 않고, 기존 선례 `IPacketHandler.RequiresSelectedClass`처럼 핸들러가 예산 종류를 선언하고 관문 한 곳에서 소비한다. `IntentRateLimiter`의 고정 창 틀을 재사용하고 시각은 `TimeProvider`로 주입한다. 관문은 입력별 콘솔 로그(`GameSession.cs:168`, `PingHandler.cs:18`)보다 앞에 둔다.
- 3·4·6번은 1번과 같은 파일이라 한 구현 계약으로 묶는다(메인 제안과 같다).
- **`02_Server/Network`는 공유 DLL이 아니다.** 소비자는 `GameServer`·`GameServer.Tests`뿐이고 Unity Plugins에는 `ClientNet`·`Shared`만 있다(저장소 검색 관측). PR1은 서버 쪽 검증으로 닫는다.
- 5번은 신호와 표준입력 중 먼저 오는 쪽이 같은 `host.Stop()`을 부른다. 기존 예산(기본 2초)과 실패 시 exit 1을 유지한다. 신호 등록은 `host.Start()` 뒤다.
- 7번은 도착(`MapMigration` 입장 완료) 뒤 파티 actor에서 현재 상태를 그 사람에게만 1회 보내는 안이다. 파티가 없으면 기존 해산 형식(`partyId=0`)이다. 새 패킷은 없다.

## 항목별 선행 시험 계획

전부 제품 경로를 실제로 지난다. 기대값을 제품과 같은 계산으로 다시 만들지 않는다. 시험 파일은 PR별로 나눠 리드가 해당 PR에 넣을 수 있게 한다.

| 번호 | 시험 | 수정 전 예상 실패 | 재현 판정 |
|---|---|---|---|
| 1 | 실제 소켓 쌍에서 상대가 읽지 않을 때 상한을 넘게 송신, 끊김과 끊긴 뒤 대기량 확인 | 끊기지 않는다 | 필수 |
| 2 | 시험용 시계로 `OnRecvPacket`에 종류별 폭주, `EnqueueJob` 등 실제 큐 진입 수 확인, 창 경과 뒤 회복, 정상 빈도 통과, 이동 500/s 보존 | 전부 큐에 들어간다 | 필수 |
| 3 | 소켓을 먼저 닫아 송신·수신 등록 예외를 내고, `OnSend` 예외를 내 끊김과 `OnDisconnected` 1회 확인 | 끊기지 않고 대기 목록이 남는다 | 필수 |
| 4 | 커널 버퍼에 쌓인 데이터를 즉시 완료로 연속 수신시켜 `OnRecv` 시점의 콜스택 깊이를 잰다 | 깊이가 완료 횟수만큼 는다 | 깊이 증가를 실측하면 재현. 스택 소진 자체는 시험 프로세스를 죽여 재현 조건으로 쓰지 않는다. |
| 5 | WSL에서 실제 `GameServer` 프로세스를 띄워 TERM·INT를 보내고 exit와 `Server stopped.` 확인, Enter·EOF 보존 | exit 143/130, 정지 줄 없음 | 필수. 7777 단독 사용 관문을 둔다. |
| 6 | 실제 소켓에서 대기 합보다 적은 성공 완료를 만들어 본다 | 부분 완료인데 목록이 비워진다 | 부분 완료를 관측하지 못하면 범위에서 뺀다 |
| 7 | 실제 `GameWorld`·이동 경로에서 이동 중 상대가 탈퇴, 도착 뒤 `partyId=0` 수신 확인 | 통지가 없다 | 실패하지 않으면 범위에서 뺀다 |
| 8 | 실제 `PlayerPhysicsSystem.Step`에서 버퍼가 있는 채로 공격 상태로 착지 틱을 진행 | 점프가 발사된다 | 실패하지 않으면 범위에서 뺀다 |

선행 시험은 **세션 하나로 8건 전부** 쓴다. 재현 안 되는 항목을 구현 전에 가려 구현자 세션을 아끼고, Astra 세션 수를 줄이기 위해서다.

## PR 경계와 검증

순서는 메인 판단과 같다. 뒤 PR이 앞 PR 위에 쌓이는 스택 PR로 낸다. 병합이 11-11 뒤로 밀려도 오늘 Codex 작업을 끝낼 수 있어서다. 앞 PR의 branch가 병합으로 지워지면 GitHub가 다음 PR의 base를 옮기는 것을 기대하며, 이번에 처음 확인한다.

| PR | 항목 | branch | 등급과 이유 |
|---|---|---|---|
| PR1 연결 계층 | 1·3·4·6 | `fix/core-audit-connection-20261110` | 강. 보안 경계, 실패 수명, I/O |
| PR2 입력 예산 | 2 | `fix/core-audit-input-budget-20261110` | 강. 입력 경계, 서버 판정, 비용 상한 |
| PR3 종료 경로 | 5 | `fix/core-audit-graceful-stop-20261110` | 강. 영속 상태, 실패 수명 |
| 입력 예산 잔여 경로 | 2의 잔여(미등록 패킷 번호·중복 로그인) | `fix/core-audit-input-gaps-20261110` | 강. 입력 경계 |
| PR4 게임 규칙 | 7·8 | `fix/core-audit-party-jump-20261110` | 강. 서버 판정 |

표의 위에서 아래로 쌓는다. 실행 검증의 「네 PR을 쌓은 최종 head」는 다섯 PR을 쌓은 최종 head로 읽는다.

R-7은 메인 승인이 없어 비해당이다. 실제 변경 줄 수의 원시는 각 PR 구현 뒤 근거 폴더에 둔다.

### 실행 검증

| 검증 | 언제 | 닿는 이유 또는 닿지 않는 이유 |
|---|---|---|
| 빌드·서버 시험 (`sync-wsl.sh build`·`test`) | PR마다, 구현자와 검증자 각각 | 모든 PR이 서버 코드를 바꾼다 |
| 자동 접속 봇 (`sync-wsl.sh bot`) | PR마다 | 송신 경로·입력 예산·이동 경로에 실제 접속으로 닿는다. 예산이 정상 흐름을 막지 않는지 본다 |
| 세션별 최대 송신 대기량 측정 | PR1 봇 실행 | 상한 제안값의 실측 근거 |
| 정식 서식 검사 (`99_Tools/format-check.sh`, WSL 일회용 복제본) | PR마다 push 전. 검증자가 새 시험 파일까지 포함해 마지막에 1회 | CI와 같은 진입점이다. Windows 진입점은 Windows의 SDK 10.0.301을 요구해 쓸 수 없다. 도구는 근거 폴더의 `lead/harness/format-in-clone.sh`이고, 공백 오류면 약 7분, 통과하면 뒤 도구 시험까지 약 40분 걸린다(메인 `msg_888b768de9f5`) |
| 실제 프로세스 종료 신호 | PR3 | 5번의 실제 진입 경로 |
| Unity 시험 EditMode·PlayMode | 네 PR을 쌓은 최종 head에서 1회. 메인 창 배정 요청 | 공유 DLL은 안 바뀐다. 다만 7·8번과 예산이 클라이언트가 보는 서버 동작을 바꾸므로 사용자 지정대로 1회 돌린다. 기존 실패(끌기 1, 길드 창 11)와 수를 대조한다 |
| DB 저장 실행 (Stored 모드) | 최종 head에서 1회. 메인이 서버 시작·정지 대행 | 5번이 영속 마무리에 닿는다. TERM 종료 뒤 재시작 복원을 본다 |

## 제안 정책값(메인 판단 요청 1)

아래는 **제안값이며 실측이 아니다.**

| 값 | 제안 | 근거 |
|---|---|---|
| 연결별 대기 메모리 상한 | 32 MiB(붙든 배열 합) | 패킷당 65,535바이트라 약 512패킷이다. 커널 송신 버퍼가 먼저 흡수하므로 앱 대기열은 상대가 멈췄을 때만 자란다. PR1 봇 실측 최대가 이 값의 4분의 1을 넘으면 올리자고 보고한다 |
| 연결별 대기 개수 상한 | 4,096개 | 범위 문서의 「개수·바이트」 둘 다 충족. 지금 패킷 크기에서는 메모리 상한이 먼저 걸린다 |
| 공격 | 초당 20 | 쿨다운 10틱 = 500ms(`Constants.cs:54`), 정상 2/s의 10배 |
| 스킬 | 초당 20 | 공격과 같은 여유 |
| 메뉴(캐릭터 목록·생성·선택) | 초당 10 | 화면 조작 단위의 요청 |
| 핑 | 초당 10 | 클라이언트 1초 주기(`NetworkService.cs:33`), 정상의 10배 |
| 그 밖의 요청 공유 예산 | 초당 30 | 판단 요청 2의 답이 포함일 때 |
| 초과 시 처리 | 버리고 창당 첫 1회만 경고 | 기존 이동 예산과 같은 동작. 끊지 않아 오탐이 시연을 끊지 않는다 |

## 메인 판단 요청과 답

176 A 승인에 답이 들어 있다(`msg_2fd9d770d23b`).

1. 위 정책값으로 진행해도 되는가 → 제안대로 진행. 32 MiB는 PR1 실측 뒤 재검토한다.
2. 이동·공격·스킬·메뉴·핑 밖의 요청(파티·포털·인벤토리·아이템·길드·던전 확인)을 공유 예산에 넣을지 → 넣는다. 결정 174의 글은 네 종류였고, 이 확장은 메인이 범위 문서와 달라진 점으로 사용자에게 보였다.

## 일정과 사용량 위험

- 선행 시험 뒤 재계산(2026-11-09T23:27Z rollout 관측, 원시 `lead/raw/codex-credit-after-red.json`): 주간 사용률 81%, 잔액 26,451크레딧, 선행 시험 세션 383크레딧(모델 단가 환산). 주간 창 리셋은 `resets_at` 1794406478 관측이다. 남은 Sol 4·Astra 4 세션을 10-19 단가 기록과 이번 값으로 어림하면 약 2,400크레딧(주간 약 8%p)이라 **네 PR 모두 예산 안**이다. 어림이고, 구독이 언제 끝나는지는 메인이 전달한 「11-10 자정 가정」을 따른다.
- 세션 계획은 선행 시험 Astra 1, 구현 Sol 4, 검증 Astra 4로 **최소 9세션**이다. 불합격이 나면 늘어난다. 같은 worktree의 WSL 잠금 때문에 순차로 돈다.
- 추정(실측 아님): 선행 시험 ~10:00, PR1 ~13:00, PR2 ~15:30, PR3 ~18:00, PR4 ~20:30, 최종 실행 ~22:00 KST.
- **Codex 잔량이 가장 큰 위험이다.** 11-10 07:23 측정으로 주간 80%를 썼고 오늘 끝난다. 메모리 기록에는 Astra 단가가 Sol의 5배로 남아 있어(10-19 기록, 지금 값은 미확인) 검증자 몫이 크다. 선행 시험이 끝나면 rollout의 잔액으로 남은 PR 수를 다시 계산해 메인에 먼저 알린다. 모자라면 뒤에서부터(PR4 → PR3) 못 끝낸다고 보고하고, 모델은 바꾸지 않는다.
- Claude 주간 85%(07:23 측정)라 20:00 전에 차면 리드가 멈춘다. 리드는 소스를 더 읽지 않고 계약 필드와 원시 근거만 다룬다.

## 결과와 남은 위험

### 선행 시험 결과

작성자 `[Core 시험 Astra]`(rollout 관측 `gpt-6-astra`, backend unknown), 완료 `msg_c3879ddd08e5`, 원시와 표는 `tdd/report.md`. 리드가 원시 요약 줄을 직접 대조했다: 기준선 실패 0·통과 2,569·전체 2,597, 최종 실패 26·통과 2,585·전체 2,639, 실패 이름은 모두 신규 시험이다.

| 항목 | 결과 | RED | 보존 |
|---|---|---:|---:|
| 1 송신 상한 | 재현 | 6 | 1 |
| 3 등록 예외 | 재현 | 4 | 2 |
| 4 수신 상호 호출 | 재현(깊이 23→85, 32회) | 1 | 0 |
| 6 부분 송신 | **재현 불가, 범위에서 제외.** 8MiB 단일·1,024조각·작은 송신 버퍼 비차단 세 방법 모두 완료 바이트가 요청 전체와 같았다. Linux/.NET/loopback 조건의 결과이며 모든 플랫폼의 불가능을 뜻하지 않는다. 원시 `tdd/raw/item-6-attempt-1.log` | 0 | 0 |
| 2 입력 예산 | 재현 | 11 | 11 |
| 5 종료 신호 | 재현(TERM exit 143, INT exit 130, 정지 줄 없음). Enter·EOF는 exit 0 | 프로세스 2 | 프로세스 2 |
| 7 파티 통지 | 재현(도착 뒤 `partyId=0` 없음) | 1 | 1 |
| 8 점프 버퍼 | 재현(공격·피격 잠금 중 vy=8) | 3 | 1 |

**리드 계약의 오류 하나.** 8번 계약에 「사망」을 넣었는데 승인 범위는 공격·피격이다. 그래서 8번 RED 3건 중 사망 1건은 범위 밖이고, PR4 시작 때 그 사례를 뺀다. 사망 상태가 이동을 잠그지 않는 관측은 아래 다음 계획 후보로 옮겼다.

### PR1 구현 결과(자체점검, 독립 판정 전)

구현자 `[Core Sol]`, 완료 `msg_db8ea2b37bb0`, 보고 `pr1/report.md`, 변경은 `02_Server/Network/Session.cs` 하나다(SHA256 `96bc28f2abeb44749633076c202c6785f0da4ea36e3d19c8989de1e1bcc4ad8c`, 리드 대조 일치).

- 선행 시험 세 클래스: 수정 전 실패 11·통과 3, 수정 후 실패 0·통과 14.
- 전체 시험: 실패 2·통과 2,581·건너뜀 28·전체 2,611(리드가 원시 요약 줄 확인). 실패 2건은 `SessionDisconnectCleanupTests`의 닫힌 소켓 단정이고, 처리는 독립 검증 계약에서 한다.
- 봇(`DashSmoke`·`BossFightSmoke`·`EnemyAiSmoke`·`MultiAccountLoginSmoke`): 수정 전후 모두 2 통과·2 실패로 같다. **`DashSmoke`와 `BossFightSmoke`는 수정 전 기준 판에서도 실패했다**(`pr1/raw/bot-before.log`). 이번 회귀가 아니며, 원인은 이 goal에서 보지 않았다.
- 송신 대기량 실측: 8개 연결의 최대 `backingBytes=65,535`, `segments=1`(`pr1/raw/summary-1.json`). 읽기를 멈추는 상대가 없는 loopback 봇의 값이라 정지 상황의 여유를 말해 주지 않는다. 그래서 32 MiB를 올리거나 낮출 근거가 되지 못하며, 리드 제안은 유지다. 메인에 원시값과 함께 보고한다.
- 서식 검사: Windows에 고정 SDK 10.0.301이 없어(`8.0.427`·`10.0.103`만 있음) **미실행**이다. 정식 판정은 PR의 CI(`dotnet-tests.yml`)로 받는다.

### PR1 독립 검증 결과

검증자 `[Core 검증자]`(화면 `GPT-6-Astra xhigh`, backend unknown), 완료 `msg_454bd9c2f2ad`, 판정 원문 `pr1/verify/verdict.md`: **PASS, 제품 결함 없음.** 리드가 원시 요약을 대조했다.

- 전체 시험: 시험 변경 전 실패 2·통과 2,581·건너뜀 28·전체 2,611, 변경 뒤 실패 0·통과 2,604·건너뜀 28·전체 2,632(새 독립 시험 21사례 포함).
- 봇: 2 통과·2 실패로 수정 전과 같다. 실제 서버 경로다. 기준선 실패는 `DashSmoke` 「Path enemy found but no S_HitResult(hitEffect=3) received」, `BossFightSmoke` 「reapproach timeout at attempt 1 — bot could not close to boss」다. **대시 피해와 보스전 경로는 봇으로 확인되지 않았다.**
- 미실행: 정식 서식 검사(Windows SDK 없음, PR CI로 판정), Unity 시험·DB 저장(최종 head에서 1회).

기존 시험 분류(`msg_71e41e231d55` 정본, 판정 표 `pr1/verify/verdict.md` 64~67행):

| 시험 | 수정 전 기대 | 수정 뒤 실제 | 분류 | 근거 | 원시 |
|---|---|---|---|---|---|
| `SessionDisconnectCleanupTests.Disconnect_EndpointUnavailable_ClearsBuffersEvenWhenDiagnosticWriterFails(logThrows: False)` | `ObjectDisposedException`, 알림 0회 | 예외 없음, 알림 1회, 버퍼 정리 | (a) 옛 구현 세부 단정, 의도된 동작 변화. 제품 회귀 아님 | 항목 3 요구: 소켓이 먼저 닫혀도 `OnDisconnected`를 한 번 부른다(`pr1/contract.md` 항목 3). 안 부르면 플레이어 정리가 일어나지 않는다 | `pr1/verify/raw/full-before.log`, `full-after-1.log`, `cleanup-tests.diff` |
| 같은 시험 `(logThrows: True)` | 위와 같음 | 위와 같음, 진단 writer 시도 유지 | 위와 같음 | 위와 같음 | 위와 같음 |

PR305 병합: 사용자 승인 원문 「병합 승인: PR305 head bdfc81b6ff200c70240f368abe793d81e1df107c」(11-10 10:08 KST 메인 pane에서 제출, 메인 전달 `msg_001c18f08bf2`). 병합 커밋 `8cfebebd7e3002f43852287d8f00c95c05100f41`, mergedAt 2026-11-10T01:08:48Z, 방식 `--merge`, CI 4개 통과.

### 입력 예산 결과

branch `fix/core-audit-input-budget-20261110`. 구현 `gpt-6-astra` high, 독립 검증 `gpt-6-astra` xhigh. 판정 원문 `pr2/verify/verdict.md`는 PASS이고 제품 결함은 없다. 대상 head는 `e19c8b84445f47b4e0685a0bd2970817dd1967d7`이고, 그 뒤 commit은 검증자가 쓴 시험과 이 goal 기록뿐이다.

- 선행 시험: 수정 전 실패 11·통과 11 → 수정 후 실패 0·통과 22. 같은 명령 10회 연속 전부 22통과라 실제 시간에 기대는 사례의 흔들림은 관측되지 않았다.
- 독립 시험 39사례(`Network/InputBudgetBoundaryTests.cs`) 통과. 종류별 경계·창 회복·연결 간과 종류 간 격리·예산 없음 경로·관문 순서를 다룬다.
- 전체 시험: 시험 변경 전 실패 1·통과 2,625·건너뜀 28·전체 2,654, 변경 뒤 실패 0·통과 2,665·건너뜀 28·전체 2,693(새 독립 39사례 포함).
- 봇: 2 통과·2 실패로 수정 전과 같다. 네 시나리오 서버 로그의 `[InputBudget]` 경고는 각 0건이다. **대시 피해와 보스전 경로는 봇으로 확인되지 않았다.**
- 핸들러 선언은 21개 전수 일치한다. 구현 계약 설명 문장의 22개는 리드 표기 오류이고 요구 표와 등록 소스는 21개다.
- 미실행: Unity 시험·DB 저장(최종 head에서 1회).
- CI 서식 실패와 수정: head `64937d67`의 CI가 검증자 시험 파일 `InputBudgetBoundaryTests.cs`의 공백 4곳으로 실패했다(메인 `msg_888b768de9f5`). 원인은 리드가 검증 계약에서 정식 서식 검사를 「Windows SDK 없음」으로 미실행 처리하고 WSL 경로를 확인하지 않은 것이다. 새 작업자가 초기화 대입을 줄 나눔만 해서 고쳤다(`5f5a23ab`). 공백·개행을 지운 바이트의 SHA256이 전후 같고(`pr2/format/lead/reinspect-hash.txt`), 독립 시험 39통과, 정식 서식 검사 exit 0이라 기존 독립 판정을 유지한다. 바뀐 줄은 `pr2/format/lead/changed-lines.diff`다.

기존 시험 분류(`msg_71e41e231d55` 정본, 판정 표 `pr2/verify/verdict.md` 127~129행):

| 시험 | 수정 전 기대·입력 | 수정 뒤 | 분류 | 근거 | 원시 |
|---|---|---|---|---|---|
| `GuildIndependentLifetimeTests.BurstOfGuildInput_IsAnsweredOnce_AndTheSlotIsFreeAfterwards` | 추가 입력 200개 뒤 같은 초의 조회도 응답 | 추가 입력 20개, 단정 전부 불변 | (a) 예산 이전의 무제한 입력 가정. 제품 회귀 아님 | 항목 2 요구: 그 밖 입력은 연결별 공유 초당 30. 20개는 대기 슬롯 1보다 많아 시험 목적을 유지하고, 폭주 자체는 독립 시험이 맡는다 | `pr2/verify/raw/full-before.log`, `full-after-1.log`, `worktree-before-aftertest-tracked.diff` |

PR306 병합: 사용자 승인 원문 「병합 승인: PR306 head 5f5a23ab02cf98b5284ce6e9b001788a72e6557a」(11-10 12:41 KST 메인 pane에서 제출, 메인 전달 `msg_b58fc16450b6`). 병합 커밋 `f1f2b2e970ff7d74e8b38b20bfb1d5869ee3f796`, mergedAt 2026-11-10T03:41:38Z, 방식 `--merge`, CI 4개 통과. 앞 PR 병합 뒤 PR307의 base는 `main`로 관측했다.

### 종료 경로 결과

branch `fix/core-audit-graceful-stop-20261110`. 구현 `gpt-6-astra` high, 독립 검증 `gpt-6-astra` xhigh. 판정 원문 `pr3/verify/verdict.md`는 PASS이고 제품 결함은 없다. 대상 head는 `a049f37cdcb55d5b88d8b580bbbb938fe888c89a`이고, 그 뒤 commit은 검증자가 쓴 시험 1파일과 이 goal 기록뿐이다.

- 종료 네 경로: 수정 전 TERM 143·INT 130(정지 줄 없음), Enter·EOF 0 → 수정 후 네 경로 모두 exit 0·`Server stopped.`(`pr3/raw/item-5-attempt-3.log`, `4.log`, `pr3/verify/raw/item-5-attempt-5.log`).
- 독립 프로세스 시험: 신호 경합·입력 도착 6/6, 정지 실패 2/2(실제 `GameServer.dll`에 시험용 틱 지연만 주입, exit 1·표준 오류 문구·정지 줄 없음).
- 전체 시험: 시험 변경 전 실패 0·통과 2,665·건너뜀 28·전체 2,693, 변경 뒤 실패 0·통과 2,670·건너뜀 28·전체 2,698(새 독립 5사례 포함). 기존 시험은 바꾸지 않았다.
- 봇: 2 통과·2 실패로 수정 전과 같다. 시나리오별 `Server stopped.`는 0줄에서 1줄이 됐다. **대시 피해와 보스전 경로는 봇으로 확인되지 않았다.**
- 정식 서식 검사: exit 0, 공백 오류 0줄(새 시험 포함).
- 미실행: Unity 시험·DB 저장(최종 head에서 1회), 신호 등록 전 실제 신호.

### 입력 예산 잔여 경로 결과

branch `fix/core-audit-input-gaps-20261110`. 선행 시험 `Network/InputBudgetUnregisteredAndLoginTests.cs` 8사례(작성 `gpt-6-astra` high): 수정 전 실패 5·보존 3으로 두 경로 모두 재현됐다. 전체 시험은 실패 5·통과 2,673·건너뜀 28·전체 2,706이고 실패는 새 클래스뿐이다. 구현 뒤 바뀔 기존 시험 네 사례는 `gaps/tdd/report.md` 46행 이후에 있다.

구현 `gpt-6-astra` high, 독립 검증 `gpt-6-astra` xhigh. 판정 원문 `gaps/verify/verdict.md`는 PASS이고 제품 결함은 없다. 대상 head는 `5ce5c775c87a67b872f80d8914967e5c3bf880cf`이고, 그 뒤 commit은 검증자가 쓴 시험 2파일과 이 goal 기록뿐이다.

- 선행 시험: 수정 전 실패 5·통과 3 → 수정 후 실패 0·통과 8. 독립 시험 8통과.
- 전체 시험: 시험 변경 전 실패 4·통과 2,674·건너뜀 28·전체 2,706, 변경 뒤 실패 0·통과 2,686·건너뜀 28·전체 2,714.
- 봇: 2 통과·2 실패로 수정 전과 같고 네 시나리오 예산 경고는 각 0건이다. **대시 피해와 보스전 경로는 봇으로 확인되지 않았다.**
- 정식 서식 검사 exit 0, 공백 오류 0줄.
- 미실행: Unity 시험·DB 저장(최종 head에서 1회).

기존 시험 분류(`msg_71e41e231d55` 정본, 판정 표 `gaps/verify/verdict.md` 65~70행): 전체 실패 4건을 전수 분류했다. `RegisteredHandler_DeclaresRequiredBudget(C_Login, …)`과 `LoginHasNoBudget_…`(이름을 `LoginSharesMenuBudget_…`로 변경)은 (a) 옛 단정이고, `ExactLimit_…(menu)`와 `AllFourMenuHandlers_…`는 (b) fixture 로그인이 같은 창의 메뉴 1건을 쓰는 전제 문제다. 한도를 낮추지 않고 그 사례만 시계를 1초 진행했다. 제품 회귀는 없다.

PR307 병합: 사용자 승인 원문 「병합 승인: PR307 head bd372aee20d88292b0a17e5ea77860b731fb3cbd」, 병합 커밋 `6ac5fdf61cf12c5eb34877dc659dc4f4fa268533`, mergedAt 2026-11-10T05:38:43Z. PR308 병합: 사용자 승인 원문 「병합 승인: PR308 head b18266e156a5c1eb092512cb0631c1325a3d5ec0」, 병합 커밋 `3032afb38043220ad26fc8c0b7376a03716cbd55`, mergedAt 2026-11-10T05:39:02Z. 둘 다 방식 `--merge`이고 메인 전달은 `msg_b0d2e5503748`이다.

### 게임 규칙 결과

branch `fix/core-audit-party-jump-20261110`. 선행 시험은 보관본을 SHA256 대조로 복원했다(작성 `gpt-6-astra` high).

- 8번 시험을 승인 범위로 줄였다(메인 `msg_7d8ae31d35b6`·`msg_1bf9011869bf`). 변경은 `InlineData("death")` 한 줄, switch 가지 하나, 윗부분 주석 한 줄뿐이다. 원래 SHA256 `96dcc8884b77369af94b98633ffc62d2ebedd1c7fb704ca417aef33c6d149d85`, 줄인 뒤 `3a817ffc67946680ecd5a332a4d78e9cef02d94f967fdd540a985917bbcd8b43`. 7번 시험은 복원 그대로다(`f8620ded7b2bf44d84d6ddc9691849c4cd09992d4b99f46193b9b897d88a178c`). 메인이 찾던 「선행 시험 수정 절차」는 정본에 없던 표현이었다.
- 수정 전: 7번 실패 1·보존 1, 8번 실패 2·보존 1. 전체 시험 실패 3·통과 2,688·건너뜀 28·전체 2,719이고 실패는 새 사례뿐이다.

첫 독립 검증은 NOT PASS였다(`pr4/verify/verdict.md`, 결함 PR4-01). 파티 없는 사람이 초대 대기 중에 도착하면 빈 통지가 클라이언트 초대 팝업을 닫는다. 선행 시험 축소는 허용한 세 곳뿐임을 그 검증자가 확인했다.

승인 설계의 예외(메인 `msg_b0d2e5503748`, B안): 도착 때 파티가 없고 대기 초대가 있으면 빈 통지를 보내지 않는다. 예외의 수명은 초대의 수명과 같다. 남는 경우는 이동 공백에 해산을 놓쳤고 도착 상태 작업보다 먼저 새 초대가 기록된 경우뿐이고, 수락하면 새 통지로, 아니면 다음 도착 때 복구된다. 메인 관측 정정: 계획에 적었던 「클라이언트가 빈 통지를 무시한다」는 효과음에만 맞았다. 팝업은 닫힌다.

재검증 중단 기록. 메인 판단 원문(`msg_131b0fd2c82c`)은 「재검증자 종료는 전원 차단에 따른 비정상 종료다. 불합격이 아니다.」이고 확정 실패 집계에 넣지 않는다.

- 리드 관측: 재검증 Task `task_8d48f185af0e`(Dispatch `ctx_c5ce489a283d`)의 종료 코드는 1073807364이고 판정 파일은 없다. 11-10 KST의 정산 receipt는 `state: retained`·`reason: external_terminal`·`processAction: none`이다(`pr4/verify-2/lead/worker-release.json`).
- 잔여물 확인(11-10 06:35:58Z, `lead/raw/residual-check-1.log`): 일회용 서식 복제본 폴더는 비어 있고 잠금 파일 27개는 전부 풀려 있다. 7777 listener와 dotnet·서식 검사 프로세스는 없고 Unity Editor는 0개다. 정리할 것은 없었다. WSL 작업 복제본 내부는 보지 않았다.
- 중단된 세션이 작업 트리에 남긴 시험 두 파일은 `pr4/verify-3/held/`로 옮겼다. `JumpBufferGateTests.cs`(`fa136444bc7ea0f00e4e446d84be77ac6f451f6a6144bd6b0433518218951b27`)는 첫 판정 세션의 보관본과 같은 바이트다. `PartyArrivalStateTests.cs`(`0e6c74243eb051a68f1492c86738e9eeed2ab7692725912b35274960eab52688`)는 보관본과 다르고 종료 시점에 편집 중이었을 수 있다. 새 검증자에게 신뢰하지 않는 입력으로 넘겼다.
- 전원 차단으로 끊긴 이 재검증 세션은 재부팅 뒤 pane으로 되살아나 있었다(`term_92692c88…`, 화면 `Conversation interrupted`, 메인 관측 `msg_06e6801feae6`). 재사용을 막으려고 빈 입력줄과 idle을 확인한 뒤 11-10 09:03:11Z에 닫았다.

독립 재검증은 PASS다(`pr4/verify-3/verdict.md`, SHA256 `5d4932349a0884517ff0ce0df962ca2c3a697730728b0f5393f0fd888b0a8287`, Task `task_f1e1f15ac67f`, 화면 `GPT-6-Astra` xhigh, backend unknown). PR4-01 수정을 확인했고 새 제품 결함은 없다. 리드가 시험 집계와 exit를 원시에서 표본 대조했다.

- 선행 시험 수정 후 7번 2통과, 8번 3통과. 7번은 원본과 바이트가 같고 8번은 허용한 세 곳만 줄었다.
- 전체 서버 시험은 시험 추가 전 실패 0·통과 2,691·건너뜀 28·전체 2,719, 독립 시험 21사례(파티 13·점프 8) 추가 후 실패 0·통과 2,712·건너뜀 28·전체 2,740이다.
- 봇 4개는 2 통과·2 실패로 수정 전과 같다. 봇은 파티 해산과 잠금 점프 경로에 닿지 않아서 이 두 경로는 서버 시험으로만 확인했다.
- 정식 서식 검사는 exit 0, `error WHITESPACE` 0줄이다(07:30:25Z 완료).
- 중단된 세션의 시험은 새 검증자가 내용을 감사해 채택했고 `PartyArrivalStateTests.cs`에 사례 하나와 단정 하나를 보탰다(`pr4/verify-3/raw/held-party-adoption.diff`).
- 비차단 관찰 둘: `MapMigration`의 `GameWorld.Instance`는 다중 world 도입 때 세션 소유 world로 바꿔야 한다. 승인 예외(B안) 때문에 해산을 놓친 뒤 새 초대가 먼저 오면 옛 파티 표시가 남을 수 있다.

PR309 병합: 사용자 승인 원문 「병합 승인: PR309 head c6fca35765400a9462c7fc057db2dadc59f9023e」(11-10 18:5x KST 메인 pane에서 제출, 메인 전달 `msg_ee2cff84e72f`). 병합 커밋 `7a724f18012ecfc27253ed9fcd9e8886ab8a0fc9`, mergedAt 2026-11-10T09:55:21Z, 방식 `--merge`, CI 4개 통과. 메인 운영 결정으로 DB 저장 실행 완료를 기다리지 않고 승인을 요청했다(`msg_06e6801feae6`).

### 통합 실행 검증 결과

대상은 PR309 head `c6fca35765400a9462c7fc057db2dadc59f9023e`다. 병합 커밋과 tree가 같다(`06aac70dbfcc18c1cb616a932149697ca49ce084`, 메인 확인).

**Unity EditMode·PlayMode 1회.** 결론은 「차이 있음」이다(`pr4/unity/report.md`, SHA256 `6b20d7ea962715e9bf7e8a90267b6370b1505dc07a3afc601c3f4d69f904a1bf`, Task `task_f658f66acaf8`, 화면 `GPT-6-Astra` xhigh, backend unknown).

| 모드 | 전체 | 통과 | 실패 | 건너뜀 |
|---|---|---|---|---|
| EditMode | 1,861 | 1,861 | 0 | 0 |
| PlayMode | 305 | 252 | 5 | 48 |

- 실패 5건은 모두 `HudPanelSizingRenderTests`다: `G1_Knight_PartyInkStaysInsideBothRowsWithFourPixelVerticalMargins`, `G1_Mage_PartyInkStaysInsideBothRowsWithFourPixelVerticalMargins`, `G4_InventoryOpensTwelvePixelsBelowThePartyPanel`, `Portrait_Knight_PortraitCellContainsNoPanelParchment`, `Portrait_Mage_PortraitCellContainsNoPanelParchment`. 환경 변수 `DAWNHOLDER_GUILD_SIZE_CAPTURE_DIR`가 없어 `Capture:298`의 단정에서 끝난다. 분류는 (b) 환경·fixture 전제 누락이고 통과로 세지 않는다.
- 이 시험은 이 묶음 전인 11-07부터 main에 있고 이 묶음의 `03_Client`·`98_Shared` 변경은 0이다. 전제가 없을 때 건너뜀이 아니라 실패로 끝나서 DEVELOPMENT의 전제 표에 없는 lane이다.
- 기준선(11-06 Content U3)과 이름 대조: 공통 이름의 상태 변화 0, 건너뜀 48의 이름·이유·label 변화 0, 새 이름 EditMode 18·PlayMode 7, 사라진 통과 이름 EditMode 8. 계획에 적었던 길드 창 11건은 지금 같은 건너뜀이다. 옛 끌기 실패의 이름은 특정하지 못했다.
- 실제 플레이와 실서버 lane은 돌지 않았다. Unity가 자동으로 바꾼 두 파일(`MinimapRT.renderTexture`, `ProjectSettings.asset`)은 `pr4/unity/raw/drift.patch`로 남기고 리드가 이름으로 되돌렸다.

**DB 저장 실행.** 단계 카드는 `pr4/db/card-v1.1.md`(SHA256 `334b9ab280e4eb61aa18653988f8d9726d097025fedc91116f87c36654b14eca`), 메인 결정은 `msg_35400729c4f6`이다. 서버 시작·TERM·정지는 메인이 했고(137 A) 자격 입력은 사용자가 했다.

- 1차 실행(참고 기록, `pr4/db/lead/first-run-note.md`): S1~S4 관측, S5 미실행, 서버는 Enter로 정상 정지했다(메인 `msg_e52d85e0e1eb`). Astra 검증자 Task `task_9a6b6e1e5e30`이 S2 뒤 위 거부 문구로 끊겼고 같은 세션 재시도 네 번(08:56·08:59·09:04·09:14Z)도 즉시 같은 문구였다. 비정상 종료 처분이고 불합격 집계에 넣지 않는다(메인 `msg_03ee2dce17ef`).
- 2차 실행은 「기대와 일치」다(`pr4/db-2/report.md`, SHA256 `eb78bd9fb5ea30eeb53740a78c48ef90db7b1545950ea77b7c4d120d19e98cf8`, Task `task_910333ca1356`과 S6 보충 `task_d01c4d0bffe8`, 화면 Opus 5.5 xhigh, backend unknown). 메인 원천 대조 통과(`msg_d5eca4f14a7d`, `msg_c67b37e3ac5f`).

| 단계 | 결과 | 원천 |
|---|---|---|
| 빌드 | exit 0, 경고 80·오류 0 | `pr4/db-2/raw/build-1.*` |
| S1 시작 | 10:01:36Z, 허용 목록 세 줄, 실패 문구 0 | 메인 `msg_e39a377441ed` |
| S2 봇 before | exit 0, 저장 직업 Mage, 맵 입장, 종료 10:02:32Z | `pr4/db-2/raw/s2-*` |
| S3 TERM | pid 498에 10:03:52Z 1회, 정상 정지 세 줄, 프롬프트 복귀, 실패 문구 0, listener 0 | 메인 `msg_4d183068c7a4` |
| S4 재시작 | 10:04:06Z, 세 줄, 실패 문구 0, 사용자 손 없음 | 메인 `msg_4d183068c7a4` |
| S5 봇 after | exit 0, 저장 직업 Mage, 맵 입장, `AlreadyOnline` 0줄, 종료 10:05:06Z | `pr4/db-2/raw/s5-*` |
| S6 Enter 정지 | 10:11:38Z, 세 줄, 프롬프트 복귀, 실패 문구 0, listener 0 | 메인 `msg_b71c58c24348` |

- 이 실행이 보여 주는 것(메인 판단): Stored 모드에서 launcher를 거친 TERM이 정상 정지 세 줄로 끝나고, 그 직후 재시작이 되고(영속 소유 반환), 재시작 뒤 같은 계정이 다시 들어와 저장된 직업을 읽고 맵에 입장한다. launcher의 종료 코드 숫자는 읽지 않았다. 근거는 실패 문구가 없고 정상 정지 세 줄 뒤 프롬프트로 돌아온 것이며, launcher는 자식 종료 코드가 0이 아니거나 출력이 불완전하면 예외를 던진다(`Start-PersistenceServer.ps1:586-591`).
- 보여 주지 않는 것(메인 판단): S5가 읽는 직업과 입장은 S2 이전부터 DB에 있던 값이다. S2가 쓴 값이 TERM을 넘어 남았다는 증거가 아니다. 접속 중인 세션의 마지막 저장도 아니다.
- 메인 정정: launcher의 `Owned WSL process PID`는 `wsl.exe`의 Windows PID다(`Start-PersistenceServer.ps1:532`, `:555`). TERM 대상은 7777 listener의 Linux PID다.

### 종료 Gardener 결과

`gardener.md`(Task `task_15e44ff057f8`, 화면 Opus 5.5 high, backend unknown). 새 정리 후보는 없다. 경고 억제·설정 완화 추가도 그의 검색식 범위에서 0건이다. 제안은 기존 행에 관측을 잇는 2건이고 채택 전이다: 세션별 실행 harness 재작성은 `verification-harness-powershell-helper`, PR306 CI 서식 실패는 `prepush-ci-format-checks`와 겹친다. 그가 찾은 기록 불일치 3건 중 goal·BACKLOG의 2건은 이 기록에서 바로잡았고, 되살아난 세션은 「재검증 중단 기록」에 적었다. 그 세션은 후보로 등록하지 않았다. `gardener.md`의 줄 번호는 정정 전 기준이다.

**완료조건 대조.** 1~3번은 앞 절의 PR별 결과와 같다. 4번은 관측한 범위만 충족이다. 실제 프로세스의 SIGTERM·SIGINT는 종료 경로 PR에서 `Server stopped.`와 exit 0으로 관측했고, Stored 모드는 위 표까지 관측했다. 「저장값이 이어진다」는 S2가 쓴 값으로는 확인하지 못했다. 5번은 위와 같이 실행했고 PlayMode 실패 5건은 통과로 세지 않는다. 6번은 PR305~PR309 각각 새 독립 검증자의 PASS 원문과 사용자 승인 한 줄이 있다.

### 남은 위험

아래는 메인 지시 `msg_2fd9d770d23b`로 기록한다.

- 전체 메모리 한도는 연결 수 × 연결별 상한이다. 동시 연결 수 제한은 범위 밖이다.
- 패킷마다 65,535바이트 배열을 만드는 생성 코드는 범위 밖이다.
- 맵에 도착할 때마다 파티 상태 통지가 한 번 더 간다(7번이 재현되어 고칠 때).
- 스택 PR의 GitHub base 자동 이동은 PR306 병합 뒤 PR307의 base가 `main`으로 옮겨진 것으로 확인했다(「입력 예산 결과」). PR305 병합 직후의 이동은 적어 두지 않았다.
- 접속 중인 세션이 있을 때의 종료 신호와 마지막 안전 지점 저장은 실행으로 확인하지 못했다.
- 파티 해산과 잠금 점프 경로는 봇이 닿지 않아 서버 시험으로만 확인했다.

## 다음 계획 후보

- 패킷마다 65,535바이트 배열을 만드는 생성기를 패킷 크기에 맞게 바꾸기(`GenPackets.cs`의 `Write()`, 메인 지시 `msg_2fd9d770d23b`).
- 사망 상태의 이동 미잠금: `DeathState`가 `LocksMovement`를 재정의하지 않아 기본값 false다. 수정 전 `PlayerPhysicsSystem`의 주석은 Death도 잠근다고 적었고, 이 goal의 수정 커밋 `1619297f`에서 `Attack/Hit`로 바로잡았다. 선행 시험에서 사망 중 버퍼 점프 발사를 관측했다(`tdd/report.md`, 리드가 `LocksMovement` 검색으로 재정의 위치 두 곳을 확인). 승인 범위가 공격·피격이라 이번에 고치지 않는다.
- `bot-dash-boss-baseline-failure` 봇 기준선 실패: 수정 전 main `acd5a7db`에서 `DashSmoke`·`BossFightSmoke`가 실패한다(`pr1/raw/bot-before.log`, 메인 확인 `msg_076badf9a3ef`). BACKLOG와 감사 산출물에 없던 새 관측이다. 원인 미조사. 메인이 시험 신뢰도 묶음의 범위로 사용자에게 올린다. BACKLOG 등록은 다른 후보와 함께 종료 기록 PR에서 한다.
- `Session.Clear`가 연결이 끊길 때마다 send-pending peak 한 줄을 콘솔에 찍어 운영 로그가 접속 종료 수만큼 늘어난다(메인 관찰 `msg_001c18f08bf2`, 이 goal에서 고치지 않는다).
- 클라이언트 `PartyInvitePopup.OnPartyUpdated`가 빈 통지에도 팝업을 닫는다. 파티에 들어갔을 때만 닫도록 바꾸면 서버의 도착 통지 예외를 지울 수 있다(메인 `msg_b0d2e5503748`, 서버 묶음 범위 밖).
- 접속 세션이 있을 때 종료 신호 → 마지막 안전 지점 저장을 실행으로 확인하기(메인 `msg_35400729c4f6`).
- 앞선 접속이 쓴 값이 종료 신호를 넘어 남는지 확인하기(메인 `msg_d5eca4f14a7d`).
- `HudPanelSizingRenderTests` 5건의 캡처 전제를 건너뜀으로 처리하거나 전제 표에 넣기(메인 `msg_d5eca4f14a7d`).
- DEVELOPMENT에 launcher의 PID가 Windows PID임을 적기(메인 `msg_35400729c4f6`의 정정).

## 진척 단계

PR 번호는 PR을 만들 때 채운다.

- [x] goal 초안 승인
- [x] 선행 시험 작성
- [x] 연결 계층 구현
- [x] 입력 예산 구현
- [x] 종료 경로 구현
- [x] 입력 예산 잔여 경로 구현
- [x] 게임 규칙 구현
- [x] 통합 실행 검증
- [x] PR305 병합
- [x] PR306 병합
- [x] PR307 병합
- [x] PR308 병합
- [x] PR309 병합
- [>] 종료 기록 정리
