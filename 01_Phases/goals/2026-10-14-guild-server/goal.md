# 길드 서버 — 클리어 보상에서 거점·무기 성장까지

## 재개 지점

**2026-10-14 KST 세션 마감·안전 지점 보존. 목표는 미완료다.** 메인 `msg_d4772cb09338`이 메인 pane의 사용자 「A 40분 마감」 결정을 전달했다. 사용자 직접 입력으로 격상하지 않는다. 마감은2026-10-13T19:15:00Z(04:15 KST)이며, 이 전달 이후 새 작업자/검증자/큰 실행을 열지 않는다. 재부팅 전 미완 시험을 보존하고 **이 goal 기록만 커밋·푸시**한다. 리드 pane은 스스로 닫지 않으며 사용자 종료를 기다린다. 아래 시간순 이력의 이전 실행 계획은 오늘 추가 착수 권한이 아니다.

- 경로/branch: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/world-active`, `feat/guild-server-20261014`. 구현·시험 작성 기준 HEAD는 `20ce59ddd72d0cb72b6e230e386a62587b9bfe1b`이다. 마감 기록 커밋은 이 goal만 포함하며 실제 커밋 SHA/푸시·원격 대조는 E/`shutdown/checkpoint-commit.json`에 남긴다. 새 진입 때 `git rev-parse HEAD`와 `git log -1 --format=%H -- 01_Phases/goals/2026-10-14-guild-server/goal.md`를 다시 확인한다.
- 중단한 작업: `task_74d20f503a32` / `ctx_e4dbbb2d2bb1`, official worker_done `msg_b993c35cc402`의 outcome `failed`로 부분 정산했다. 사유는 외부 자원 대기 때문에 RED가 미실행인 것이며 제품 확정 실패가 아니다. 정확한 작성자 split은 release→close했고 활성 작업자0·World 리드 pane1·reclaimable0이다. 동일 작성자 세션을 재사용하지 않는다.
- 보존한 결과: 신규 Guild8파일/1763줄/작성59사례, 기존 시험3파일의 지정 단정 변경. baseline은40/40·Guild0이고 신규 시험의 빌드·실행 발견·RED는 아직0이다. 부분 원문 E/`tdd-base/report.md`, 리드 실사/종료·생존 간격 초과3건은 E/`tdd-settlement-lead-note.md`에 있다. 요구 v1·A와 Q1 후속 시험 의무는 유지한다.
- 근거 보존: E는 `.backups/verification/2026-10-14-guild-server/`(Git 제외)다. E/`shutdown/preservation.json`에 미커밋12파일의 원본/사본 SHA256와 오류 없는 대조가 있다. `shutdown/working-copy/`는 사본, `tracked-uncommitted.patch`·`untracked-guild.patch`는 diff다. 원본 작업 사본은 그대로 둔다.
- 다음 첫 할 일: 사용자가 재개한 새 세션에서 지침/R-8·현재 Git 상태·이 goal·위 부분 보고를 읽고 현재 Run 권한/신원을 다시 확인한다. Core 실제 자원 정리/창 반환 상태와 당시 메모리 창을 새로 조율한 뒤에만 신규 `claude-opus-5-5`를 연다. 같은 Task가 현재 Run에서 유효할 때 공식 `--retry-of ctx_e4dbbb2d2bb1`의 새 Dispatch로 남은 RED1회/실패 전수 분류를 맡긴다. 재부팅 전 handle·자원 허용을 그대로 쓰지 않는다.
- 입력 주의: RED 보존 manifest SHA256은 `67954C41386B96FE2D7A9116D74F5F3D3C84308DF45ABBD293C281836441BD14`이나 HEAD 행을 포함하므로 **이번 goal 기록 커밋 뒤 전체 hash는 달라진다.** 새 실행자는 현재 HEAD의 새 preflight에서 변경된 커밋이 goal 한 파일뿐인지, 미완 시험 해시와 나머지 manifest 항목이 유지되는지 대조한다. 옛 preflight를 현재 입력으로 위장하지 않으며 새 메모리·공식 ask 뒤에만 실행한다.
- 자원/결정: World가 만든 Unity·컨테이너·서버는0, baseline 자기 clone의 마지막 잔여 process/lock 관측0 후 추가 실행 없음.18:37:08Z Windows Unity/GameServer 프로세스0·7777 listener0이다. **Core 컨테이너 cleanup/창 반환은 이 마감 기록 시점 미수신**이며 전역 컨테이너0을 주장하지 않는다. 남은 사용자 결정 요청은 없고, 전체 완료조건/Content 서버 main 병합 의존은 미해결이다.

미커밋 파일은 다음12개다. 아래 시험11개와 CURRENT는 이번 기록 커밋에서 제외한다.

| 상태 | 경로 |
|---|---|
| 수정·리드 진행 링크 | `00_Document/operations/CURRENT.md` |
| 수정·미완 시험 | `02_Server/GameServer.Tests/Items/InventoryProtocolContractTests.cs` |
| 수정·미완 시험 | `02_Server/GameServer.Tests/Maps/BossBehaviorTests.cs` |
| 수정·미완 시험 | `02_Server/GameServer.Tests/Network/LoginProtocolContractTests.cs` |
| 신규·미완 시험 | `02_Server/GameServer.Tests/Guild/GuildWire.cs` |
| 신규·미완 시험 | `02_Server/GameServer.Tests/Guild/GuildTestWorld.cs` |
| 신규·미완 시험 | `02_Server/GameServer.Tests/Guild/GuildMembershipTests.cs` |
| 신규·미완 시험 | `02_Server/GameServer.Tests/Guild/StrongholdUpgradeTests.cs` |
| 신규·미완 시험 | `02_Server/GameServer.Tests/Guild/WeaponEnhanceTests.cs` |
| 신규·미완 시험 | `02_Server/GameServer.Tests/Guild/EnhancedAttackDamageTests.cs` |
| 신규·미완 시험 | `02_Server/GameServer.Tests/Guild/GuildRequestBoundaryTests.cs` |
| 신규·미완 시험 | `02_Server/GameServer.Tests/Guild/GuildProtocolContractTests.cs` |

### 착수·진행 이력

**마감 전 진행 상태(이력): 범위 v1·A 승인, 기술 계약 v1 고정·기초 선행 시험 부분 결과 보존/작성자 정산 종료.** 기존 선택 시험 baseline은40/40이고 신규 Guild 시험은 작성59사례·미실행이다. 메인 `msg_6640294f802e`가 사용자 승인 원문과 초안 hash 대조를 전달했다. G1~G8·수치·수명·PR 경계가 확정됐다. Content의 기술 대조 `msg_e09e88a8eab0`를 반영했다. 신규 Opus 선행 시험→구현→강 등급 독립 검증 순서로 진행한다. 금고 적립 연결 구현은 Content 던전 서버 PR의 main 병합 뒤다.

- 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/world-active`.
- 새 branch: `feat/guild-server-20261014`. base·초안 작성 HEAD: 최신 main `20ce59ddd72d0cb72b6e230e386a62587b9bfe1b`. fetch 뒤 origin/main과 같고 진입 시 Git 변경은 없었다. 이전 종료 branch에서 새로 갈라졌다.
- 리드: `[World 리드 Astra]`, 요청 `gpt-6-astra` xhigh, 화면 `GPT-6-Astra xhigh`, backend unknown, Codex CLI 0.162.1.
- 현재 Run: `run_c443eba7fb12`, 회신 `run:run_c443eba7fb12`, coordinator `term_72fe3013-8169-4f16-b032-ee3445eaec89`. runtime `b91e135c-099c-4973-ae25-06b289c2c414`, incarnation `2e6c10b0-570d-4e6c-ae60-a8f65dece74a`. 재개 시 fresh 조회하며 옛 Run·Task·Dispatch를 권한으로 쓰지 않는다.
- 메인 수신 주소: 진입 때 확인한 `term_bd5c24e8-325b-4f3b-ae51-5cb587c98f00`(terminal-only). READY `msg_c16e7b54e01f`로 새 Run 주소를 알렸다.
- 근거 E: `.backups/verification/2026-10-14-guild-server/`(Git 제외). 사전 메모 `lead-context.md`, 진입 원문 `entry-mail.json`, `run-create.json`, 프로토콜 조사 `protocol-inspection.json`.
- 승인 고정 입력: E/`approved-scope-v1.md`, SHA256 `790951684505E7404A467E52458585E14C5F76EE91EB3BC7171DAC43CB32CB73`. 승인 전달 원시는 E/`mail-followup-01.json`, 이 단계 사전 메모는 E/`approved-design-context.md`다.
- 현재 활성 작업자 없음. 종료한 첫 할당은 `task_74d20f503a32` / `ctx_e4dbbb2d2bb1`, `[World 검증자]` 기초 선행 시험 작성자 `term_9bc7b2a5-e4dc-4fd9-898d-be7a63bd1907`(incarnation `982a3f67-7800-4c38-8927-86561dc40b2e`)다. 요청/최초 실행 `claude-opus-5-5`, 화면 Opus5.5 xhigh·Claude Code2.1.296, backend unknown. UTC17:13:44의 최초 입력/턴 시작,18:28:38Z의 failed 부분 정산 및 정확 pane 종료를 확인했다. 같은 세션은 재사용하지 않으며 독립 최종 검증자는 별도 새 세션이다.
- 발행 입력: E/`technical-contract-v1-goal.md` SHA256 `3966AB2633D113ABF761F9F03E212AE6523CAB68BE3F94370BA37B75B8872087`, E/`tdd-base-contract-v1.md` SHA256 `B75F7D7E4BD71F8538DC1331F323FFEC3CFE80F6B8AEEB62E88486386C695AE9`. 이 goal의 운영 상태 갱신과 요구사항 고정 사본을 구분한다. 발행 직전 입력34개 hash·경로 대조는 E/`tdd-dispatch-preflight.json`이며 규칙8절 원문이 계약에 포함됐다.
- 다음 행동: Core SQL 실제 정리·창 반환 통지와 새 자원 조율 뒤 신규 Opus를 연다. 같은 Task의 남은 작업은 `--task task_74d20f503a32 --retry-of ctx_e4dbbb2d2bb1`의 새 Dispatch로 재개한다. 새 맥락·같은 입력 hash·직전 메모리·RED 공식 ask를 확보하며 시험 발견/실패를 전수 분류한다. R-7 미적용이라는 메인 판단에 따라 불변식별 실패 시험을 기본 경로로 추적한다. 우편함용 대기 루프를 열지 않는다.
- 현재 실행 순서: baseline1회는17:41:21→17:41:38Z 종료했다. helper/wrapper exit0, TRX40/40(InventoryProtocol3·LoginProtocol8·BossBehavior29), Guild0을 리드가 원시 대조했다. E/`baseline-lead-raw-check.json`, 원시 TRX hash `6B8FD10F775972B19E8BE776305C7E9F6695C776175C8925FFAE0990CBFDD7C2`. 이는 기준선이며 신규 길드/독립 PASS가 아니다. Core·Content에 큰 실행 창을 반환했고 RED는 새 ask 전 미허용이다. 후속17:57Z 원시에서 자기 clone 귀속 process·점유 lock0을 확인했다(아래 정산 기록).
- Content `msg_53eaadacd118`(17:48:50Z)는 관련4클래스 PlayMode 두 번째 batch1회 허가 발행 통지다. Core의 회귀 우선 조율을 근거로 했으며, 이 통지 시점 실제 시작/종료는 미확인이다. World의 반환한 창을 다시 예약하지 않으며 RED는 Content 종료와 새 자원 조율 뒤 별도 허용한다.
- 후속 Content `msg_295c4a3ede0f`·`msg_1880e84c2c24`에서 PID38628의17:49:26.363→17:50:02.713Z 종료·14개 중12통과/2실패·Unity0·창 반환을 받았다. 상대 `batch2-lead-result-check.json`과 대조했으며 Content 회귀 전체는 통과가 아니다. World RED 공식 ask `msg_4ad0b3a50d84`는 Core 현재 창 재조율과 baseline 자기 자원 정산 전 `msg_02e5e025c5dc`로 보류했다. 시험 입력은 E/`red-lead-input-check.json`에 대조했고 신규 Guild8파일·기존3파일 지정 단정 수정이다. 작성자에게 이전 delivery 미ACK를 처리해 후속 자원 정산 메시지를 수신하고 새 ask를 제출하도록 했다.
- Core `msg_76480c214015`의 다음 SQL 우선 창(예상40~60분, 상한2h)을 수신했다. 이때 SQL release/실제 시작은 아직0이다. `msg_5642d35d9980`으로 World 큰 실행/예약0·자기 clone 정산 미수신을 회신했고, 작성자에게 `msg_57161834162f`로 정산 원시만 보고한 뒤 Core 실제 종료/창 반환 통지 이후에 RED 새 ask를 제출하도록 했다. 새 제품/시험 쓰기와 실행은 보류 상태다.
- 작성자 `msg_ac81cc3ad268`의 자기 자원 정산 원문을 직접 읽었다.17:57:43Z 자기 clone 귀속 프로세스·dotnet 계열0, 계산한 lock key eec8ef6360269731ab71/inode420556의 보유 행0,17:57:53Z baseline MSYS launcher PID4884 부재다. vmmemWSL 자체는 남아 있었으므로 WSL 전체 종료/다른 세션 자원 해제로 해석하지 않는다. E/`baseline-residual-lead-check.json`에 원시 hash와 관측 한계를 보존했고 Core `msg_cfb9e066f076`으로 World 큰 실행/예약0·현재 충돌없음을 전달했다. 작성자는 옛 delivery 두 묶음 ACK와 입력67954C41…BD14 보존을 보고했고, RED는 미실행 상태로 유지한다.
- Core `msg_dedcc33c85fe`(18:11:16Z)는 SQL 검증 창을 Start18:10:54.891Z·StopNewWork20:05:54.891Z·Deadline20:10:54.891Z로 지정했다. 상대 `lead-start-window.json`의 Task/Dispatch·계획hash C4B766B3…5963·세 시각·FormalG2=false와 World 자원 반환 회신 포함을 대조했다. 후속 `msg_142ae9aeec33`(18:21:05Z)은 첫 자원 생성18:12:23.6178006Z·컨테이너 StartedAt18:12:25.126553958Z를 Core가 원시 대조해 실제 시작을 확인한 보고다. 지정 deadline은 그대로며 검증 PASS/정리 완료는 아직 아니다. World 큰 실행/예약0·RED 보류를 유지하고 실제 정리/창 반환 통지를 받은 뒤 재조율한다. 이 deadline 경과만으로 World 실행을 시작하지 않는다.
- 메인 `msg_7aa98422a3ea`의 idle pane 확인 요청으로 실제 상태를 조회했다. Task는 in_progress·Dispatch dispatched·완료 시각null·RED 미실행으로 작업 완료가 아니었다. 마지막 생존 신호18:00:38Z 뒤20분 이상 보고가 끊겨 계약47행/ORCA의300초 규칙을 지키지 못했다. 리드의 장기 빈 prompt 대기 지시도 원인이며 `msg_5507b8cbf764`로 메인에 즉시 알렸다. 상시 waiter를 만들지 않고 부분 결과/미실행/간격 위반을 기록해 이번 작성자를 외부 자원 대기 사유의 미완료로 정산·종료할 예정이다. 아직 worker_done/release/close는 수행되지 않았다. Core 반환 뒤 신규 Opus 세션으로 남은 RED 실행·분류를 이어가며 제품 확정 실패로 세지 않는다.
- 정산 요청 `msg_198822b5eeb0`(18:25:22Z)을 현재 Dispatch로 보냈다. 실제 허용 쓰기는 부분 report 마무리와 근거만이며 제품/시험/고정 입력·무거운 실행은 그대로 금지했다. 공식 worker_done은 지원 outcome `failed`로 요구했고 외부 자원 대기/미완료라는 사유를 제품 결함과 구분하도록 했다. 자동 알림으로 작성자 턴이 시작한 화면을 확인했으며 수락된 worker_done을 기다리는 상태다. release/close를 미리 실행하지 않는다.
- Core `msg_13c758b38507`(18:24:49Z)은 메모리 급증으로 fixture 실행 PID5696만18:23:26Z 중지했고18:23:49Z 부재/메모리 회복을 확인한 보고다. Core는 이번 회차를 환경 중단으로 구분했으며 검증 PASS/FAIL이 아니다. 컨테이너/볼륨/비밀 정리·창 반환은 아직 미확인이므로 World 새 큰 실행은 계속 금지다. 작성자 `msg_e75fc465bcab`(18:25:43Z)은 부분 정산 시작 heartbeat이며 완료가 아니다. 앞 신호18:00:38Z와의 간격은1505초로300초를 초과했다(정산 helper로 전체 구간 측정 예정).
- 정산 완료: `msg_b993c35cc402`(18:28:38Z)의 exact worker_done/outcome failed를 현재 Task/Dispatch·worker-list settled와 대조했다. `tdd-base/report.md`는 신규8파일1763줄/Fact38+InlineData21=작성59사례·baseline40/40·Guild0·RED 미실행을 구분한다. 입력 불일치0이며 부분 결과 보존이고 제품 확정 실패/독립 PASS가 아니다. `tdd-settlement-liveness.json`의 공식 helper는13신호·300초 초과3구간(474·388·1505초), 질문 대기3구간 제외, exit1을 반환했다. 앞선 두 누락도 정산에 추가했다. `worker-release` retained/external_terminal 뒤 exact split만 close(ptyKilled=true), 재조회에 리드 하나만 남고 reclaimable0이다. 전체 근거·관측 한계·다음 진입은 E/`tdd-settlement-lead-note.md`에 있다.

## 진척 단계

- [x] 진입·현황 조사
- [x] 범위 초안 작성
- [x] 범위 승인
- [x] 기술 계약 확정
- [>] 선행 시험 작성
- [ ] 서버 구현
- [ ] 독립·봇 검증
- [ ] 제품 병합
- [ ] 종료 점검·기록

PR 번호가 생기면 해당 병합 단계에 실제 `PR번호 병합`을 적는다. Content 화면 PR의 상태는 상대 goal에서 관리하고 이 문서에는 의존성만 연결한다.

## 요구사항 원천과 적용 결정

| 원천 | 확인 수준과 이번 적용 |
|---|---|
| 메인 진입 `msg_9120fc11aff8`, 2026-10-13T16:15:37Z | 현재 메인 handle과 대조한 할당. 최신 main 새 branch·새 Run·길드 서버 범위 초안만 허용. 구현은 범위 승인 뒤다. |
| [이전 로그인 goal의 다음 목표·결정](../2026-10-11-multi-account-login/goal.md#적용-중인-사용자-결정), 메인 전달 `msg_5ed276291957` | 로그인 종료 다음은 길드 서버. 서버 PR이 패킷을 먼저 정하고 Content 화면 PR이 따른다. |
| `C:/Dev/DawnHolder_Dashboard/plans/demo-video-1028-scenario.md`의 승인 기록 | 메인 pane 사용자 답 「계획 검토 - Main 10-28 시연 영상 시나리오와 파트 배정 → A」를 메인이 전달. 우리 pane의 사용자 직접 입력으로 격상하지 않는다. 원문 SHA256 `A801F44FF16FCB74C0C2DE7D54B763EB4011B620CD9CA82C653C075C044710A4`. |
| 같은 계획의 정할 것·파트 배정 | 패널형 거점, 기본 길드 자동 가입, 무기 강화, 저장 장면 선택, 별도 Art, 동결 뒤 아트·연출·버그만이라는 메인 해석을 따른다. 수치와 강화 단계 수는 정하지 않았다. |
| 같은 사용자 코멘트 | 「길드거점은 맵형태로 나중에 시간나면 꼭 만들었으면 좋겠어, 시각적인 성장 이펙트나 이런게 도드라지면 좋겠네, Shader로 해결할 수 있는건 해결하고, …」. 맵형은 10-21 첫 확장 후보, 효과는 Content·Art 담당이다. |
| [ADR-007](../../../00_Document/ADR/gameplay/ADR-007-stronghold-model.md) | 채택된 시설 모델: 길드 자금으로 구매하면 길드원이 기능 사용. 생산·고용·자원 변환은 제외한다. |
| 메인 `msg_6640294f802e`, 2026-10-13T16:34:39Z | 사용자 원문(메인 pane Enter 제출, 메인 전달): 「대시보드 결정 응답: 1) 1 - 길드 서버 범위 - 금고를 별도 보너스로 쌓을지(A), 개인 보상에서 떼어 쌓을지(B) → A 별도 보너스」. 승인 v1 hash를 메인이 대조했고 이 리드도 보존본과 재계산 일치. G1~G8와 A 수치·수명 전체를 확정하며 개인50/재료1/주머니1은 보존한다. |
| 메인 후속 `msg_f0eecb333347`, 2026-10-13T16:35:17Z | 메인이 장면3을 금고10으로 수정했다. 현재 계획 SHA256 `FAA2206BAB317B9B56979BD7BE0436F9E1FF86A0B07225E567F8A2E872C2CB35`를 이 리드가 대조했다. 위 A801F44F…는 수정 전 승인 원천으로 보존한다. Core 진단은 UTC16:23 정리됐다는 전달이며 World 실행 실적이 아니다. |

승인 전 계획의 장면3 금고0 예시와 가입 시드의 차이, 장면6 「보상 일부」 문구와 별도 보너스의 차이를 메인이 사용자에게 함께 제시했고 A를 받았다. 두 계정 가입 뒤 금고10으로 바꾸며 대시보드 계획 수정은 메인이 한다. 이 리드는 goal만 갱신한다. 역사 PRD의 길드 여러 개·창고·시설 여러 종류는 이번 범위가 아니다.

## 범위

### 만들 것

1. **기본 길드 하나와 회원 관계.** 로그인된 계정이 실제 월드에 들어오면 기본 길드에 자동 가입한다. 같은 서버 실행 중 같은 계정은 한 회원이고 재접속으로 가입 시드를 다시 받지 않는다. 두 접속자가 같은 길드 식별자·거점·금고를 조회한다. 이번 화면 계약은 회원 수와 접속 회원 수까지며 회원 명단 편집·검색·초대는 없다.
2. **길드 금고와 클리어 적립.** Content가 확정한 보스방 복사본 수혜자 중 개인 클리어 보상이 실제 성공한 사람마다 서버가 금고를 한 번 적립한다. 확인 버튼·패킷 재전송·늦은 입장으로 적립하지 않는다. 승인 A는 기존 개인 보상을 전부 보존하고 공동 금고에 별도 보너스를 더한다.
3. **거점 레벨 1 → 2와 대장간 해금.** 살아 있는 길드원이 마을에서 요청하면 서버가 금고·현재 레벨·요청 revision을 확인하고 공동 자금 한 번 차감과 레벨 변경을 함께 확정한다. 한 번 열린 대장간은 해당 길드원 모두 사용할 수 있다. 동시 요청 두 개가 두 번 차감하지 않는다.
4. **개인 무기 강화 0 → 1.** 장비 아이템·장착 칸을 새로 만들지 않고 현재 직업의 무기에 강화 단계 하나를 둔다. 마을에서 대장간이 열린 경우 개인 재화·재료를 한 번 소비하고 공격력 보너스를 적용한다. 전사·마법사 평타와 기존 피해를 내는 스킬에 같은 서버 스탯 정책을 적용한다. 다른 회원은 자동 강화되지 않는다.
5. **조회·동기화·결과 패킷.** 공동 상태와 본인의 강화 상태·비용·결과를 Content가 표시할 수 있게 전달한다. 조회는 어디서든 실제 입장한 접속만, 두 구매 행위는 마을·살아 있음·소유 접속·이동 중 아님을 서버에서 확인한다. 입력은 의도·예상 revision만 보내며 비용·보상·행위자·공격력을 클라이언트가 지정하지 않는다.
6. **서버·프로토콜 시험과 봇 순환.** 두 계정 가입 → 같은 보스방 클리어 → 공동 금고 증가 → 마을 복귀 → 거점 성장 → 개인 강화 → 이후 실제 피해 증가를 관찰한다. 실패·중복·상한·재접속·맵 이동도 아래 완료조건대로 확인한다.
7. **현재 계약 문서와 소비자 호환성.** PDL·생성 코드·프로토콜 판·서버/Unity DLL 소비자를 함께 맞추고 관련 프로토콜·서버·도구 문서, 기능 지도, 이 goal과 CURRENT World 줄을 갱신한다. Content가 만들 화면에 필요한 wire 계약을 서버 PR에서 고정한다.

### 건드릴 곳

아래는 승인된 제품 범위다. 현재 리드 쓰기는 이 goal·CURRENT World 줄·E 근거이며, 기초 선행 시험 작성자가 계약에 따라 신규 Guild 시험8파일과 기존 시험3파일의 지정 단정을 썼다. 제품 구현은 아직 시작하지 않았다. 작업자별 정확한 쓰기 목록은 발행 계약에 고정한다.

| 책임 | 변경 후보와 경계 |
|---|---|
| 길드·개인 성장 데이터 | `98_Shared/GameData/`의 목적이 드러나는 길드/강화 규칙·결과 타입. 기존 `Items/`·`Combat/` 책임과 나눈다. 임시 시연 수치는 한 출처로 모으고 클라이언트 표시값과 일치시킨다. |
| 프로토콜 | `99_Tools/PacketGenerator/PDL.xml`, `98_Shared/Protocol/Generated/GenPackets.cs`, `ProtocolVersion.cs`, 관련 양쪽 계약 시험, `03_Client/Assets/Plugins/Shared/`·`ClientNet/`의 필요한 DLL 사본. 생성 코드는 생성기로만 바꾼다. |
| 길드 서버 | `02_Server/GameServer/`의 새 `Guild/` 책임 폴더, `Loop/GameWorld.cs`, `Sessions/GameSession.cs`, `Handlers/HandlerRegistry.cs`와 새 길드 핸들러. 회원·시드·금고·거점은 World 틱이 소유한다. |
| 경제·클리어 연결 | 기존 `Items/InventoryRegistry.cs`·`InventoryState.cs`·`InventoryTransitions.cs`의 개인 비용/강화·정산 연결. Content의 `Dungeon/`·`GameWorld.MakeMap` 클리어 경계를 재사용한다. 수혜자 판정을 새로 구현하지 않는다. 필요한 최소 변경은 Content 서버 PR 병합 뒤 World가 담당한다. |
| 공격력·맵 이동 보존 | `Entities/PlayerEntity.cs`, `Maps/PlayerSnapshot.cs`, `Maps/Transitions/`의 필요한 성장값 전달, `Maps/Actions/MeleeAction.cs`·`DashAction.cs`·`ThunderboltAction.cs`, 공유 `Combat/PlayerStats.cs`·`Formulas.cs` 중 설계가 선택한 최소 경계. 클래스 기본값·방어·HP·이동·쿨다운은 보존한다. |
| 시험·봇·문서 | `02_Server/GameServer.Tests/` 관련 신규/기존 시험, `99_Tools/headless-bot/` 신규 순환과 등록·회귀 목록, 위 양쪽 프로토콜 시험, `00_Document/domains/protocol.md`·`server.md`·`tooling.md`, `FEATURE_MAP.md`, CURRENT World 줄. |

`GameSession`·`GameWorld`는 Core 후속 로딩 게이트와 겹칠 수 있다. 착수 전 최신 main·소유권을 Core와 조율한다. Content의 현재 UI 수정 파일과 Art 자산은 읽기만 한다. 같은 파일 동시 쓰기는 금지한다.

### 하지 않을 것

- 새 길드 생성·초대·탈퇴·추방·역할/길드장·길드 여러 개·명단 관리·창고·직접 기부/출금/거래.
- 새 거점 맵·맵 번호·포탈·지형·건물 배치, 시설 둘째 종류, 생산·고용·자원 변환.
- 장비 인스턴스·장착 칸·무기 제작·강화 확률·실패 파괴·반복 강화 단계·강화 애니메이션 저작.
- DB·파일 저장, Core 저장 schema·로딩 게이트 변경, 서버 재시작 뒤 길드/개인 성장 복원. 비밀번호 인증이나 다중 계정 시드 악용 방어 전체를 구현하지 않는다.
- World에서 길드 패널·대장장이 상호작용·보상 창·셰이더·효과·촬영을 구현하는 일. 이 부분은 Content·Art의 승인 범위다.
- 일반 처치·던전 수혜자·클리어 확인·파티/퀘스트 판정 변경, 기존 1~42 패킷 변경/재사용, 무관한 코드 정리·검사 도구 추가.

### 관찰 가능한 완료조건

| 번호 | 승인 뒤 요구와 관찰 기준 |
|---|---|
| G1 | 서로 다른 두 계정이 기존 로그인·직업 선택을 지나 입장하면 같은 기본 길드다. 한 계정 재접속은 회원·시드 중복을 만들지 않는다. 접속 종료 후 온라인 수만 줄고 공동 금고/레벨은 남는다. 서버 재시작은 공동 상태 초기화다. |
| G2 | Content의 실제 클리어 성공 수혜자마다 금고가 확정표만큼 한 번 증가한다. 같은 보스 사망 중복·확인 두 번·늦은 입장·다른 복사본·가방/개인 재화 상한 거부·종료된 이전 세션은 추가 적립 0이다. 같은 복사본이 살아남아 보스가 재출현한 다음 정상 클리어는 다시 한 번 적립한다. 기존 개인 결과→인벤토리 snapshot과 보상량을 보존한다. |
| G3 | 동일 거점 revision의 동시 구매 두 건 중 하나만 성공한다. 금고 차감·레벨2·대장간 해금이 함께 바뀌고 두 계정이 같은 공동 상태를 받는다. 잔액 부족·이미 최고 레벨·오래된 revision·비회원·마을 밖·사망·이동 중 요청은 추가 소비/성장이 없다. |
| G4 | 강화 성공 한 번에 개인 골드20·재료1과 단계0→1·공격력+5가 함께 반영된다(숫자는 아래 승인 기본값). 부족·이미 강화됨·오래된 개인 경제 revision·닫힌 시설·중복 요청은 부분 소비·추가 강화가 없다. 강화하지 않은 상대 공격력은 같다. |
| G5 | 같은 적 종류·방어력·공격 종류에서 강화 뒤 서버 확정 피해가 5 커진다. 전사/마법사 평타와 Dash·Thunderbolt를 각각 확인한다. 이미 생성된 투사체/지연 피해는 시전 때 확정한 값을 유지한다. 강화 후 맵 이동·죽음/리스폰에서 단계와 공격력은 보존되며, 개인 재접속은 기존 개인 경제 수명대로 초기화된다. |
| G6 | 새 요청은 길이·머리·reserved·revision·세션/계정·소유권을 경계와 실행 시점에 검사한다. 연속 입력은 접속별 대기 한도를 넘겨 상태/큐를 무한 증식시키지 않는다. 상한 연산·revision은 넘치거나 wrap하지 않는다. 실패·종료 뒤 늦은 작업은 새 접속을 바꾸지 않는다. |
| G7 | 기존 1~42 ID/필드가 같고 새 번호·v20이 양쪽에서 일치한다. 구판 연결은 기존 handshake에서 거부한다. 생성기 재생성·독립 wire 대조·서버/봇 및 Unity 소비자 컴파일/계약 시험을 확인한다. DLL 복사·에셋 부작용은 원시와 Git diff로 대조한다. |
| G8 | 실제 서버에 접속한 두 봇이 G1→클리어→금고→거점→강화→실제 피해 증가를 끝까지 관찰한다. 기존 로그인·전투·파티·퀘스트·인벤토리·맵 이동 회귀를 실행한다. 신규 외부 검증자가 독립 시험과 실제 진입 실행을 판정하며 미실행을 통과로 쓰지 않는다. |

G5의 +5는 동등한 입력/대상의 차이 관찰이지 새 통계 계측 실적이 아니다. 초안에서 서버·봇·Unity·DB를 실행하지 않았다. Content 화면에서 두 클라이언트로 보는 장면 3·6~8과 성장 효과는 **후속 화면 PR/합동 리허설의 완료조건**이며 서버/봇 성공으로 갈음하지 않는다.

## 확정 기본값과 수명

아래는 `msg_6640294f802e`가 전달한 **승인 A 기본값**이다. 기존 Content 개인 보상(50골드·재료1·주머니1)은 현재 코드 근거이며 이번 신규 수치와 구분한다.

| 항목 | 승인 A |
|---|---|
| 시작과 가입 시드 | 길드 생성 시 금고0·레벨1·대장간 잠김. 계정의 첫 실제 입장에 공동 금고 +5, 같은 서버 실행 중 같은 AccountId에는 한 번. 두 계정 입장 뒤 금고10이다. |
| 클리어 적립 | 개인 클리어 보상 성공 1인당 공동 금고 +10. 개인 50골드·재료1·주머니1은 차감하지 않는다. 두 계정 모두 성공하면 +20이라 금고30이다. |
| 거점 구매 | 금고30으로 레벨1→2, 대장간 해금. 이후 거점 구매는 최고 레벨 거부다. |
| 개인 강화 | 개인 골드20 + Material1로 단계0→1, Attack +5. 모든 직업에 같은 가산값, 최대 한 단계. 강화 확률 없음. |
| 공동 상한 | 금고 최대 1,000,000,000. 자동 적립/시드는 `min(적립액, 남은 공간)`만 더하고 실제 증가가 없으면 공동 revision을 올리지 않는다. 개인 클리어 보상은 금고 포화 때문에 거부하지 않는다. 수동 출금 없음. |
| 개인/공동 수명 | 회원·시드 수령 기록·금고·거점은 서버 프로세스 수명. 개인 인벤토리·재화·강화는 기존 접속 수명. 맵 이동/사망은 유지, 재접속은 개인0단계, 서버 재시작은 전부 초기화. DB 미연동임을 화면·시연 설명에 숨기지 않는다. |
| 권한 | 실제 입장한 회원이면 같은 시설을 이용한다. 구매는 Town에서 살아 있고 이동/종료 중이 아닐 때만. 관리자/길드장 전용 승인 없음. |

추천 이유: 두 사람이 한 번 클리어하면 거점과 각자 강화까지 보여 줄 수 있고, 별도 지급 명령·장비 체계·DB가 필요 없다. 이는 설계상 계산이며 플레이 소요 시간이나 재미를 실측한 결과가 아니다. 새 이름을 계속 만드는 시드 획득은 정식 인증 없는 현재 데모의 한계로 남는다.

## 서버 책임과 보존 계약 초안

- **공동 상태 주인:** `GameWorld`의 길드 저장소가 AccountId 회원 관계·온라인 연결·가입 시드·공동 revision·금고·거점 레벨을 단독 변경한다. 소켓 수신은 검증 후 큐에 의도만 넣는다. 맵/Unity 화면이 금고를 계산하지 않는다.
- **개인 비용과 성장 주인:** 기존 Inventory의 개인 경제 수명을 확장해 강화 단계와 비용을 한 전이로 검사·확정하는 안을 우선 검토한다. 경제 저장소와 맵 combat 상태에 별도 유료 강화 로직을 두지 않는다. 틱 순서·맵 반영 경계·성공 통지 시점을 설계에서 고정해 돈만 빠지고 효과가 없는 상태를 만들지 않는다.
- **Content 연결:** 확인한 branch의 `DungeonClearState.Capture`가 복사본 수혜자를 고정하고 `InventoryRegistry.ApplyDungeonClear`가 성공 상태를 먼저 commit한 뒤 39→36을 직접 전송한다. 금고 적립은 성공한 서버 정산에 연결하며 송신 성공 여부·클라이언트 확인에 연결하지 않는다. 같은 정산의 중복 방지는 Content clear 수명과 연결해 시험한다. 한 복사본 영구 한 번으로 막으면 보스 재출현 뒤 정상 클리어를 잃으므로 기존 `DungeonClearRepeatClearTests`의 재클리어 계약을 보존한다. World가 별도 보스 죽음/파티 수혜자 계산을 복제하지 않는다.
- **양쪽 정산 책임:** 현재 개인 commit은 길드까지 원자성을 보장하지 않는다. 개인 성공·금고 증가의 다음 상태와 송신 자료를 먼저 준비하고, 같은 소유 틱에서 commit한 뒤 통지한다. 길드 내부 오류/공동 revision 고갈을 개인 성공 뒤 조용히 누락시키지 않도록 실패 정책을 구현 전 설계에서 고정한다. 금고 수치 상한은 위의 별도 적립 포화 정책이며 개인 보상을 막지 않는다. 이번에는 기본 길드 하나·탈퇴/변경 없음이므로 캡처/지급 사이 길드 변경은 불가능하고, 종료/이전 owner는 기존 동일 접속 검사로 배제한다.
- **공격력:** 현재 `PlayerStats`와 `PlayerEntity.Stats`는 불변이고 평타·Dash·Thunderbolt가 `Formulas.ComputeDamage`를 쓴다. 강화값의 단일 출처·유효 공격력 경계를 정하고 `PlayerSnapshot` 이동에서도 보존한다. 전투 공식의 기본값·기존 class fallback·이미 발사된 피해는 유지한다. 불변 기본 스탯을 전역으로 수정해 다른 플레이어를 강화하지 않는다.
- **대안:** 금고/강화/맵 상태를 별도 범용 거래 프레임워크로 묶는 대신 기존 World 틱과 불변 경제 전이에 필요한 좁은 경계만 추가한다. 설계 실사에서 원자성·소유권이 불명확하면 계약 발행 전 다시 나눈다. 단순히 현재 클래스에 모두 붙이는 것은 목표가 아니다.

## 패킷과 파트 간 계약 초안

조사 시각 `2026-10-13T16:22:22Z`, 원시 `E/protocol-inspection.json`, 실행 파일 `E/inspect-protocol.ps1`(성공 exit0). Orca가 열거한 로컬 checkout 8곳의 작업 파일과 열린 PR207 head를 확인했다. 아래는 **관측 당시 번호이며 예약이 아니다**.

| 대조 대상 | SHA | 관측 |
|---|---|---|
| main·World | `20ce59dd` | PDL 42개, 생성 ID 최대42, 마지막 S_LoginResult, v19. |
| Content `feat/dungeon-clear-server-20261011` | `f2753663` | main과 PDL hash 같음, 최대42·v19. 세 프로토콜 파일 미커밋 변경 없음. 던전 정산 코드는 이 branch에만 있다. |
| Art·Core·Rules·CodeMap | 원시의 각 head | 최대40·v18. CodeMap은 detached 상태다. 오래된 branch의 번호를 새 빈 번호로 보지 않는다. |
| Management·열린 PR207 - 게임 소개 페이지 | 원시의 각 head | 최대38·v17. 조회 시 열린 GitHub PR은 PR207 하나였다. 열린 작업 branch와 PR 유무는 구분한다. |

제안은 기존 PDL 뒤 **43~47**, `ProtocolVersion.Current` **19→20**이다. Content 요청 `msg_9e775883d5d9`에 대한 회신 `msg_26d28c4c755d`(2026-10-13T16:23:01Z)는 현재 Content 목표에 43 이상 추가 계획이 없고 39/40·개인50/1/1 보존 방향에 이견 없다고 확인했다. 현재 Run coordinator와 발신 handle을 대조했고 원시는 `E/mail-after-draft.json`이다. 이는 기술 대조이며 사용자 범위 승인·패킷 예약은 아니다. 구현 발행 직전 fetch·현재 branch를 다시 대조하고 앞선 새 패킷이 있으면 뒤 번호로 재생성한다.

| 후보 ID | 이름(설계에서 확정) | 담을 계약 |
|---|---|---|
| 43 | C_GuildRequest | reserved0. 현재 길드·본인 강화 상태 조회. |
| 44 | S_GuildSnapshot | 길드 소속·표시 이름/안정 식별자, 공동 revision·금고·거점 레벨·대장간 해금, 회원/온라인 수, 본인 강화 단계·공격 보너스·개인 경제 revision. 서버가 정한 다음 비용·최대 단계·행위 가능 여부를 전달해 화면이 권한/가격을 독자 계산하지 않는다. 계정 GUID나 임의 회원 목록은 보내지 않는다. |
| 45 | C_StrongholdUpgrade | 예상 공동 revision. 행위자·구매 비용·목표 길드를 받지 않는다. |
| 46 | C_WeaponEnhance | 예상 개인 경제 revision. 비용·보너스는 서버가 정한다. |
| 47 | S_GuildActionResult | 행위 종류·성공/구체 거부·권위 revision과 결과 상태를 연결할 정보. 성공·stale 뒤 snapshot으로 다시 맞춘다. 오류마다 새로운 패킷을 만들지 않는다. |

필드 타입·정확한 바이트 길이·결과 enum·개인/공동 갱신 순서·클라이언트 낮은 revision 처리·구매 성공과 성장 효과의 연결은 Content와 계약을 확정할 때 고정한다. 첫 snapshot/재조회만으로 성공 효과를 재생하지 않도록 성공 결과와 상태 차이를 구분한다. 이 초안을 바로 PDL 구현 명세로 사용하지 않는다.

### 기술 계약 v1 — Content 대조 반영

이 절은 승인 범위 안의 **리드 설계 계약**이다. Content `msg_e09e88a8eab0`(원시 E/mail-followup-02.json)가 필드 폭·기존 보상·sequence·재클리어를 대조했고, 아래 개인 revision·pending·문자열 검사를 보완해 v1로 고정했다. 이는 정적 기술 대조이며 구현/독립 검증 판정이 아니다. R-7은 메인 결정으로 미적용이며 아래 불변식을 기본 시험·독립 검증에 넣는다. 승인된 경제 수치와 G1~G8은 바꾸지 않는다. 발행 계약은 이 판의 사본/hash를 참조하고 새 변경은 별도 계약으로 알린다.

#### 패킷 필드와 결과

LittleEndian, 전체 길이는 ushort size/id의 4바이트 머리를 포함한다. 문자열은 기존 PDL의 UInt16 바이트 길이 + UTF-16LE다. 기본 길드 ID는1, 표시 이름은 `새벽 길드`(5 UTF-16 code unit), 기본 표시 데이터는 서버가 보낸다. 새 송신 문자열은1~16자/2~32바이트로 제한하고 입력에는 문자열을 추가하지 않는다. 목록이나 미검증 list 생성 경로는 쓰지 않는다.

| ID·이름 | 머리 뒤 필드 순서 | 전체 길이 |
|---|---|---|
| 43 C_GuildRequest | `byte reserved`(0) | 5 |
| 44 S_GuildSnapshot | `long snapshotSequence`, `byte changeKind`, `uint guildId`, `uint guildRevision`, `int treasury`, `byte strongholdLevel`, `byte maxStrongholdLevel`, `byte blacksmithUnlocked`, `int memberCount`, `int onlineMemberCount`, `uint inventoryRevision`, `byte weaponLevel`, `byte maxWeaponLevel`, `int attackBonus`, `int strongholdCost`, `int weaponCurrencyCost`, `int weaponMaterialId`, `int weaponMaterialCount`, `byte strongholdAvailability`, `byte weaponAvailability`, `string guildName` | `66 + 2N`(N은 이름 code unit 수), 기본 이름76 |
| 45 C_StrongholdUpgrade | `uint expectedGuildRevision` | 8 |
| 46 C_WeaponEnhance | `uint expectedInventoryRevision` | 8 |
| 47 S_GuildActionResult | `byte action`, `byte result`, `uint requestedRevision`, `uint guildRevision`, `uint inventoryRevision`, `long snapshotSequence` | 26 |

- `action`: StrongholdUpgrade=1, WeaponEnhance=2. `changeKind`: Query=0, Membership=1, TreasuryCredit=2, StrongholdUpgrade=3, WeaponEnhance=4, Presence=5. 조회/접속 첫 동기화는 과거 성공 이벤트를 재생하지 않는다.
- 결과/가능 여부 공용 코드: Success(가능)=0, Stale=1, NotMember=2, NotInTown=3, Dead=4, FacilityLocked=5, MaxLevel=6, InsufficientCurrency=7, InsufficientMaterial=8, RevisionExhausted=9, Unavailable=10. Snapshot의0은 행위 **가능**이며 성공 이벤트가 아니다. 내부 예외를 재화 부족 코드나 Success로 바꾸지 않는다.
- Snapshot의 범위: treasury0..1,000,000,000, stronghold1..2/max2, unlocked0/1과 level2의 동치, memberCount>=onlineMemberCount>=0, weapon0..1/max1, bonus0/5와 강화 단계의 동치. 거점 최고 단계면 다음 비용0, 무기 최고 단계면 다음 비용/재료 수0이다. 가능 여부는 D4에서 요청 판 대조만 제외한 순서를 따르므로 Town밖이면 최고 단계보다 NotInTown이 앞선다. 시설이 잠겨 있어도 강화 단계0의 표시 가격은20·재료ID1/수1이며, 앞선 거부 조건이 없을 때 FacilityLocked를 보인다.
- 서버의43/45/46 핸들러는 `Read` 전 정확한 길이·머리·reserved를 검사한다. 잘못된 wire/미입장/closing은 무응답으로 버린다. 양쪽44 계약은 `Read` 전에 최소66바이트·머리를 확인하고 offset64의 UInt16 이름 바이트 길이가2..32·짝수이며 전체 size가 `66 + 이름 바이트 길이`인지 검사한다. 단위는 UTF-16 code unit이며 Read 뒤 문자열 길이 검사만으로 대신하지 않는다. 클라이언트44/47은 정확한 전체 길이·enum·범위·필드 관계를 검사하고 잘못된 결과를 상태에 적용하지 않는다. 기존 계정 GUID는 보내지 않는다.
- `snapshotSequence`는 **접속별**1부터 증가하는 long이며44의 모든 발행 이유에 적용한다. 클라이언트는 현재 연결의 더 큰 sequence만 적용하며 새 연결은 동기화 상태를 비운다. long.MaxValue에서 wrap하지 않고 해당 접속의 발행을 중단·종료한다. 구매자의 다음44를 준비할 수 없으면 구매를 commit하지 않는다. 다른 수신자의 종료는 구매자의 확정 상태를 되돌리지 않는다.
- `guildRevision`은 공동 금고/거점의 경제 판, `inventoryRevision`은 기존 개인 경제 판이다. 회원/온라인 수는44 sequence로 정렬하므로 경제 revision을 단순 온라인 수 변화 때문에 올리지 않는다. 금고 포화로 실제 적립0이면 공동 판도 그대로다. 경제 판은 uint.MaxValue에서 새 변경을 거부하고 wrap하지 않는다.
- 개인 판의 최신값은 현재 연결의 유효한36/44에서 받은 `inventoryRevision`의 최댓값이다. 기존36이 인벤토리 슬롯/재화 표시를 소유하고44가 이를 덮어쓰지 않는다. 새36만 와도46의 expectedInventoryRevision은 이 최신값으로 갱신한다.44의 개인 판이 이미 알려진 최신 개인 판보다 작으면 개인 강화/비용/가능 여부 표시를 적용하지 않으며, 공동 필드는44 sequence 기준으로 적용할 수 있다. 필요 시43으로 최신 개인 표시를 재조회한다. 표시된 가능 여부는 예약이 아니며 최신 개인 판을 보낸 뒤에도 서버 D4 판정이 우선한다. stale→47/36/44 복구는 이 정상 최신판 합치기와 별개의 방어다.
- 47은 행위·요청 판을 그대로 돌려주고 이어지는 구매자44의 sequence를 가리킨다. Content는 **연결 세대+행위+요청 판**으로 현재 대기 입력과 대조한다. 클라이언트의 길드 구매 pending1개는 패널이 아니라 연결이 소유하며 창 닫기/다시 열기/시간 경과로 비우거나 자동 반복 전송하지 않는다. 해당47과 지목된44를 모두 처리해야 구매 pending을 끝낸다. 서로 다른 연결의 결과는 무시하며 재접속 때 연결 세대·pending·sequence·개인 판을 함께 초기화한다. 응답 유실/내부 오류로 결과가 완결되지 않은 연결은 구매를 계속 막고 진단/기존 재접속 경로로 해소한다. 현재47의 상관 필드가 충분하다는 주장은 이 소비 조건 안에서만 성립한다.
- 서버의43/45/46은 연결당 처리 대기1건을 공유하며 기존 인벤토리 요청 대기1건은 유지해 동시에 최대 두 경제 요청만 대기할 수 있다. 클라이언트는 구매 pending 중 새43/45/46을 발신하지 않는다. 조회43의 대기는 같은 연결의 더 큰 sequence를 가진 Query44로 끝내고, 다른 이유의 방송44로 새 구매를 허용하지 않는다. 결과를 기다리는 중의 방송44는 표시만 갱신하며 구매 pending을 대신 끝내지 않는다.

#### 상태·입력·출력 순서

| 번호 | 설계 계약 |
|---|---|
| D1 | World 길드 저장소가 AccountId별 회원/시드 수령 여부와 현재 session online 등록, 불변 공동 경제 상태(금고·레벨·revision)를 소유한다. 실제 첫 맵 입장 성공 후 가입하고 종료 정리에서 동일 session의 online 등록만 해제한다. 계정별 시드는 회원 등록과 함께 한 번만 확정한다. 신규 가입 중 준비/공동 revision 고갈 실패는 회원/시드 부분 등록을 남기지 않고 내부 진단 후 그 접속을 종료한다. 회원 수는 같은 프로세스의 고유 가입 계정 수다. |
| D2 | Inventory의 접속별 불변 상태가 강화 단계와 기존 슬롯·재화·revision을 함께 소유한다. Grant·Use·조회·일반 처치·던전 클리어가 강화값을 보존해야 한다. 맵 객체/세션에 별도 변경 가능한 강화 수치를 복사하지 않는다. 마지막 변경 주인은 Inventory이며 소비자는 읽기만 한다. |
| D3 | 길드 요청 큐는 World가 Inventory 처리 뒤 같은 틱에서 순차 drain한다. 길드 구매와 강화는 접속 owner·현재 맵/이동 상태를 재검사하고 그 시점 Town·생존이어야 한다. 맵 상태는 읽기만 한다. 강화는 개인 경제 주인의 prepare/commit API로 실행하고 호출 편의를 위해 Entry.State를 외부에 공개하지 않는다. 큐 job의 모든 반환/예외·종료 경로에서 길드 pending guard를 해제한다. |
| D4 | 유효 요청의 거부 순서는 현재 owner/회원 → 이동 중 또는 현재 맵 부재(Unavailable) → Town → 생존 → 해당 expectedRevision 일치 → 대상 최고 단계 → 시설 해금(강화) → 관련 revision 고갈 → 재화 → 재료다. 송신 전에 닫혔거나 owner가 바뀌었으면 응답도 버린다. 이 순서가 결과 enum 우선순위 시험의 기준이다. |
| D5 | 거점 구매는 공동 상태만 한 번 교체한다. 개인 강화는 개인 골드·재료·강화 단계·revision의 다음 값 하나를 교체한다. 필요한 결과/44/36 직렬화와 holder/배열 할당은 commit 전에 준비한다. 일반 거부/준비 실패는 어느 경제 상태도 바꾸지 않는다. commit 구간에는 I/O·외부 callback·조회/검증·새 할당을 넣지 않고 준비된 값만 게시한다. 내부 예외의 상세는 서버 진단이며 업무 거부 코드로 위장하지 않는다. |
| D6 | 공격력은 Inventory가 가진 현재 접속의 강화 단계를 World 틱 안에서 owner와 함께 읽고 기본 PlayerStats.Attack에 가산한다. 전사/마법사 평타·Dash·Thunderbolt가 하나의 공격력 조회/계산 경계를 사용한다. PlayerStats 기본 정의는 그대로 불변이고 맵 엔티티를 World 경제 단계에서 직접 수정하지 않는다. 해당 틱의 맵 공격이 먼저였다면 그 공격은 이전 값, 강화 commit 뒤 다음 공격부터 새 값이다. 이미 발사한 투사체/지연 피해는 생성 때 확정한 수치를 유지한다. |
| D7 | 연결의 Inventory 상태는 이동 공백에서도 유지되므로 같은 session이 새 맵에 붙으면 같은 강화값을 읽는다. 실제 이동 입력은 `Maps/Transitions/PlayerTransferState.cs`이며 PlayerSnapshot과 혼동하지 않는다. 무기 강화 때문에 불변 기본 Stats/HP/이동값을 덮어쓰지 않는다. 개인 종료 정리 뒤 늦은 job은 새 session 상태를 만들지 않는다. |
| D8 | 정상 조회는44 하나. 거점 구매는47→구매자44이며 같은 공동 revision의44를 다른 온라인 회원에게도 보낸다. 강화 성공은47→기존36→44, stale은47→현재36→44, 다른 업무 거부는47→현재44다. 다른 회원의 개인 강화값은 그 회원 자신의 값으로 보낸다. 공동 상태 방송 시 한 접속의 Send 실패가 나머지 수신자를 막지 않게 격리한다. |
| D9 | Snapshot의 가능 여부는 서버가 발행 시점의 현재 상태로 계산하지만 예약이나 미래 성공 보장은 아니다. 실제 요청 때 D4로 다시 판정한다. Content는 처음 받은44/Query/옛 sequence를 성공 효과로 쓰지 않는다. 거점 효과는 기존 동기화된 level1→2 + StrongholdUpgrade 표식, 개인 효과는 대기 요청과 일치한 Success47 및 해당44로 확인한다. |
| D10 | 모든 명령은 현 접속의 참조 동일성·EntityId·AccountId를 검증한다. 종료가 마지막 precommit owner 검사 전이면 변경0, commit 뒤라면 이미 확정한 공동 값은 유지되고 종료 정리가 개인 접속 상태를 회수한다. 늦은 전송/끊김을 이유로 결제를 rollback하거나 재지급하지 않는다. |

#### Content 클리어 성공과 공동 적립

Content 서버 PR 병합 전에는 이 연결을 구현하지 않는다. 고정 기준은 현재 읽은 branch `f2753663`가 아니라 **실제로 병합된 새 main**으로 발행 때 다시 고정한다.

1. `DungeonClearState`가 캡처한 `(EntityId, GameSession)` 수혜자를 그대로 사용한다. 이미 있는 clear 사건의 한 번 호출을 재사용하며 보스ID/맵ID만으로 영구 중복 방지 집합을 만들지 않는다. 재출현 뒤 다음 정상 clear는 새 보상이다.
2. 각 수혜자를 처리할 때 기존 IsActive와 길드 회원/online owner를 확인한다. 개인 Grant가 CurrencyCap/InventoryFull이면 기존 거부39만 보내고 공동 적립0이다. 개인 RevisionExhausted는 기존 로그/39 미발신을 보존한다.
3. 개인 성공의 다음 상태와 `min(10, 공동 잔여 공간)`을 반영할 공동 다음 상태, 필요한 패킷을 모두 준비한다. 금고 포화로0이면 개인 지급은 정상 진행한다. 증가가 필요한데 공동 revision이 고갈됐거나 내부 준비가 실패하면 **둘 다 commit하지 않고** 내부 실패를 기록한다. 이를 성공39로 보고하지 않으며 서버의 확인40 수락 자격은 보존한다. 이는 현 UI의 탈출 보장이 아니다.
4. 최종 동일 접속 검사를 통과한 뒤 같은 World 틱의 중단 없는 commit 구간에서 개인·공동 준비값을 게시한다. 할당/직렬화 실패를 개인 commit 뒤에 숨기지 않는다. 실제 기존 commit 하나가 자동으로 양쪽 원자성을 주지 않으므로 새 선행 시험이 prepare 실패·소유 종료·중간 예외 가능 위치를 검토한다.
5. 개인39→36의 상대 순서/보상50·재료1·주머니1을 유지하고, 그 뒤 길드44를 발행한다. 개인 Send가 실패해도 공동 적립은 유지하고 다른 온라인 회원44는 독립적으로 발행한다. 클리어확인40·조회43·송신 재시도로 재적립하지 않는다.

공동 revision 고갈·할당/직렬화 실패는 정상 시연 경제 정책과 다른 내부 실패 경계다. 현재 개인 지급도 준비 실패/RevisionExhausted에서 결과를 보내지 않는 계약을 유지하며 길드 실패를 정상 성공으로 포장하지 않는다. 정상 상한 포화는 이 내부 실패와 구분한다. **39 미송신 시 현 Content UI는 보상창을 열지 못해 그 창의40 버튼도 누를 수 없다.** 기존 개인 revision 고갈과 같은 종류의 한계에 공동 준비 실패가 추가된다. 서버의40 수락 자격과 UI 복귀 경로를 혼동하지 않으며 진단·최종 위험에 남긴다. 이번 계약에서 새39 결과/복귀 UI를 추가하지 않는다.

#### 선행 시험과 작업 분할

- **시험 준비:** 신규 Opus 한 세션에 서버 요청 진입·독립 wire·경제/성장 실패 경로를 맡긴다. 내부 API가 아직 없으면 미래 구현 타입을 참조해 컴파일부터 깨뜨리는 대신 현 GameSession 진입에 독립 바이트를 넣어 행동 차이로 RED를 만든다. 상한/준비 실패를 만드는 관찰 경계가 부족하면 제품을 수정하지 말고 리드에 올린다.
- **선행 의존:** Content 클리어 통합 G2/G8 시험은 실제 Content 서버가 main에 들어온 뒤 같은 계약 후속판으로 작성·실행한다. 기존 Content 시험/fixture를 branch째 가져오거나 검증 완료 근거를 재사용하지 않는다. 개인 보상/재클리어 보호 시험은 새 main 기준으로 직접 읽는다.
- **배정:** 이번 새 다파일·외부 입력·경제 수명·프로토콜 구현은 implementer-routing 신호1~4로 신규 `gpt-6-astra` xhigh 후보, 독립 검증은 프로토콜/공유 DLL 예외에 따라 신규 `claude-opus-5-5`다. 선행 시험도 신규 Opus다. 정확한 파일/줄 수는 구현 전에는 미측정이다.
- **R-7:** 메인 `msg_7a5063515cfa`가 한정 시범 횟수 소진으로 미적용을 결정했다. 불변식·전제 분류와 항목별 실패 시험은 기본 경로에 넣는다. Fable을 열지 않는다.
- **계약 고정:** 준비 계약은 E에 두고 Task/Dispatch를 만들지 않는다. wire 합의·실행 자원/선행 main을 확인해 v1 입력 SHA/hash·경로 기계 확인을 채운 뒤 발행한다. 변경된 요구를 실행 중인 시험 계약에 조용히 주입하지 않는다.

#### 불변식과 전제 — 기본 시험·독립 검증 공통 입력

메인 판단 `msg_7a5063515cfa`에 따라 **각 I항목에 실패를 검출하는 시험을 하나 이상** 연결한다. 보존 시험이 기준선에서 이미 통과하면 보호 시험으로 분리하고, 신규 동작의 RED를 대신했다고 쓰지 않는다. [소스]는 현재 계약/코드 근거, [추론]은 리드가 선택한 설계, [미측정]은 아직 실행으로 입증하지 않은 조건이다.

| ID | 불변식·전제 분류 | 필요한 실패 관찰 |
|---|---|---|
| I1 | [소스] 안정 AccountId·중복 로그인 거부(AccountIdResolver/AccountRegistry), [추론] 서버 실행 중 계정당 가입/시드 한 번(D1). | 재접속/중복 입장 job으로 시드나 회원 수가 늘면 실패. |
| I2 | [소스] Content 캡처 수혜자·연결 동일성·같은 복사본 다음 정상 clear, [추론] 성공한 개인별 공동+10. | 비수혜자/거부자/옛 연결에게 공동 적립 또는 재클리어 누락이면 실패. Content main 뒤 시험. |
| I3 | [소스] 개인 prepare/commit과 Send 비재시도, [추론] 공동·개인 모두 준비 후 게시, [미측정] 두 상태 준비 실패/송신 예외 경계. | 준비 실패는 둘 다 불변, commit 뒤 Send 실패는 둘 다 유지·재지급0. Content main 뒤 공동 시험. |
| I4 | [추론] 공동 판·단계·잔액을 같은 owner에서 확인 후 교체. | 동일 판 동시 구매가 두 번 소비/성공하거나 부족/최고 단계에서 변하면 실패. |
| I5 | [소스] Inventory 불변 전이·개인 접속 수명, [추론] 단계/비용/판을 하나의 상태로 확정. | 자금/재료 부족·stale·시설 잠김에서 일부만 소비되거나 다른 Grant/Use가 강화값을 지우면 실패. |
| I6 | [소스] 기존 기본 Stats·확정 지연 피해·같은 session 이동, [추론] 단일 권위 성장 조회, [미측정] 네 공격 경로의 실피해 +5. | 비강화자 증가, 일부 공격 미반영, 이동/리스폰 소실, 이미 발사한 피해 소급 변경이면 실패. |
| I7 | [소스] 경계 길이/현재 owner 검사·기존 pending finally, [추론] 길드1+기존경제1 한도와 완전 해제. | 잘못된 프레임/미입장/이동/사망/종료가 소비되거나 큐/guard가 새 접속에 남으면 실패. |
| I8 | [소스] 경제 상한/uint wrap 거부, [추론] 공동 포화0은 개인 지급 허용·revision고갈은 변경 거부. | overflow/wrap/초과 소비 또는 공동 포화 때문에 정상 개인 보상을 거부하면 실패. |
| I9 | [소스] 기존1~42/39→36·handshake equality, [추론] 새43~47·v20·고정 wire·접속별 sequence/결과 상관·36/44 개인 판 단조 합치기·연결 소유 pending. | 기존ID/필드 변화·버전 불일치 수락·문자열 사전 길이/enum 누락·stale snapshot 재적용·창 재개 뒤 중복 구매면 실패. 서버/독립 wire 시험과 후속 Content 소비자 시험을 구분한다. |
| I10 | [소스] 상태 변경은 틱 owner·틱에서 I/O 대기 금지, [추론] 개인 commit에서 맵 mutation 없이 공격 때 읽음, [미측정] 실제 서버 두 봇의 전체 순환. | 다른 틱 owner 우회·전송을 기다리는 경제 상태·가짜 화면 숫자로만 성장 확인하면 실패. 실행 시험과 독립 코드 실사를 함께 사용. |

## 적용 중인 운영 결정

- **R-7 미적용** — 메인 판단 `msg_7a5063515cfa`(2026-10-13T16:41:18Z), 사용자 결정과 구분한다. 메인은 main의 goal-review 네 건(2026-10-01-readability-baseline의2회차, 2026-10-04-persistence-integration의세 번째, 2026-10-05-items-inventory-currency, 2026-10-10-instance-map-lifecycle)을 근거로2~3개 한정 시범이 소진됐다고 판단했다. 이 리드가 시범을 늘리거나 Fable을 기동하지 않는다.
- 네 범주 해당성은 유지하고 리드가 불변식·전제 [소스]/[추론]/[미측정]을 기술 계약에 쓴다. 신규 Opus 선행 시험은 불변식별 실패 시험 하나 이상, 강 독립 검증은 같은 목록을 고정 입력으로 받는다. 원시 `E/mail-after-design-v0.json`.
- **기초 TDD v1 보충1** — 작성자 Q1 `msg_3850fb5bb98f`, 리드 답 `msg_74b131a4845a`. 공동 금고10억·공동 revision 고갈은 현재 외부 입력으로 실용적으로 만들 수 없고 길드 내부 fixture 경계도 아직 없다. 미래 타입 참조로 컴파일 실패만 만드는 시험을 쓰지 않는다. I8의 개인 revision 고갈은 이번 선행 시험으로 연결하고 공동 두 경계는 구현의 상태/fixture 경계가 확정된 뒤 **신규 후속 시험 계약의 필수 항목**으로 남긴다. 공동 상한·판 고갈 계약 자체를 면제하지 않으며 전체 I8 또는 강 검증 PASS 전에 직접 검증한다. 최초 계약과 응답 원문을 함께 후속 작성자/검증자에게 준다.

## PR 경계와 검증 계획

| 순서 | 담당·산출물 | 시작/종료 관문 |
|---|---|---|
| 1 - 길드 서버와 패킷 | World 제품 PR 하나: 패킷·공유 수치·길드/금고·거점/강화·공격 반영·시험·봇·관련 문서. 번호 미발행. | 사용자 범위 승인·기술 계약 고정 후 TDD 시작. 실제 Content 클리어 연결 구현은 그 서버 PR 병합과 새 main 반영 뒤. 강 등급 독립 검증·실제 서버 봇·소비자 호환성·CI 근거를 메인에 전달하고 사용자 명시 병합 승인. |
| 2 - 길드 화면과 성장 표현 | Content의 별도 goal/PR: 길드 패널·기존 마을 대장장이 상호작용·거점 성장·강화 상태/거부·효과 연결. Art가 표현 자산을 제공한다. | 서버 wire 계약을 먼저 확정하고 서버 PR이 선행 병합한다. World branch 파일을 Content가 동시에 쓰지 않는다. 두 클라이언트 실화면·시연 리허설은 이 단계에서 확인한다. |
| 3 - World 종료 기록 | World 제품 병합·로컬 결과 기록 뒤 Gardener와 종료 기록 PR 하나. | 독립 문서 실사·사용자 별도 병합 승인·미커밋/미정산 점검 뒤 메인의 R-8 교체. Content 미완료를 전체 시연 완료로 보고하지 않는다. |

- 제품 검증 등급 **강**: 서버 경제 판정·공유 프로토콜·DLL·상태 수명을 바꾸기 때문이다. 파일/줄 수는 구현 전 미측정이며 계약 발행·검증 때 실제 diff 원시를 기록한다. 이번 초안은 문서이며 독립 실사를 아직 하지 않았다.
- 승인 후 신규 외부 Opus가 요구사항 시험·실패 원시를 먼저 작성한다. 구현자는 현행 배정표로 정하고 신규 외부 세션을 쓰며, 프로토콜/공유 DLL 검증자는 신규 `claude-opus-5-5`다. 검증자는 제품 파일을 쓰지 않는다.
- R-7은 위 메인 판단으로 미적용이다. 네 범주의 불변식과 실패 시험을 기본 TDD·강 검증에 연결하며 Fable 시범을 우회 재개하지 않는다.
- 시험은 결과 상태·권위 패킷·실제 피해와 비용을 독립 입력으로 단정하고 제품 계산을 기대값으로 복제하지 않는다. 정상·부족·중복·동시·stale·상한·이동/종료 경계와 메모/규칙/diff 실사를 모두 연결한다.
- 서버/봇 실행과 Unity 호환성 검사, Content의 두 클라이언트 플레이, DB 미실행을 나눠 보고한다. 솔루션 빌드가 Unity 플레이를 증명하지 않는다.

## 날짜별 점검과 뒤로 미룰 것

- **10-15까지 목표:** 범위 승인과 패킷·데이터 계약. 날짜는 계획 목표이며 완료 실적이 아니다.
- **10-21 판단:** 서버 봇으로 가입→클리어 적립→거점→강화→피해 증가가 도는지 메인에 보고한다. 막히면 대장간 단순화 여부를 사용자에게 올리고 완료조건을 조용히 줄이지 않는다.
- **10-22~25:** Content 화면과 통합·두 클라이언트 리허설, **10-26~27** 촬영 지원, **10-28 오전** 시연. 저장 장면은 Core의 10-24 판단을 따르며 길드 DB를 이번 goal에 넣는 근거가 아니다.
- **시간 나면 첫 확장 후보:** 길드 전용 거점 맵. 사용자 희망을 보존하되 10-21 순환 완성/남은 시간을 보고 메인이 별도 범위를 정한다. 지금은 맵 파일·포탈·미사용 hook을 만들지 않는다.
- 나머지 지연 범위는 위 「하지 않을 것」이다. BACKLOG에서 길드·거점·대장간·guild의 기존 ID는 찾지 못했다. 이미 메인이 정한 길드 goal을 중복 후보로 올리지 않았으며 새 확장 goal을 자동 착수하지 않는다.

## 결정 완료 — A 별도 보너스

**1 - 길드 서버 최소 범위와 경제 기본값.** 메인 전달 `msg_6640294f802e`로 v1·A를 확정했다. 이미 승인한 패널형·자동 가입·무기 강화·맵형 후순위와 함께 G1~G8·수명·PR 경계를 따른다. 아래 B는 비교 기록이며 구현하지 않는다.

- **A — 채택:** 개인 클리어 보상은 보존하고 금고에 별도 +10/성공 인원, 최초 가입 +5/계정, 금고30으로 거점2·대장간, 개인20골드+재료1로 한 단계/+5. 두 명 가입 시 금고10으로 장면 3 예시를 고친다. 공동 상태는 서버 실행 동안, 개인 강화는 접속 동안 유지한다.
- **B — 개인 보상에서 금고 몫 차감:** 공동 +10을 개인 클리어 50에서 떼어 개인40·공동10으로 만든다. 나머지는 A와 같다. 이는 현재 Content 개인 보상 계약 변경이므로 원천/시험/보상 창 설명을 다시 합의하고 서버 PR에 포함해야 한다.

DB 저장·맵형 거점·추가 강화 단계는 포함하지 않는다. 이 승인과 각 PR의 병합 승인은 별개다.

## 현재 결과와 남은 위험

- 진입·새 branch·Run·사전 메모·요구 원천·번호/열린 branch 대조 뒤 v1/A 승인을 반영했다. 기술 계약 v0·불변식 I1~I10·미발행 선행 시험 계약 `E/tdd-base-contract-v0.md`를 작성했다. 관련 규칙 원문8절과 입력 경로29개를 `E/prepare-tdd-inputs.ps1`로 추출/기계 확인했고 결과는 `E/tdd-inputs-v0.json`(exit0)이다. 이는 문서 준비이며 구현/작업자/독립 검증/서버/봇/Unity/DB/새 PR/commit/push는 수행하지 않았다.
- Content의 열린 서버 branch와 회신을 대조해 재클리어·양쪽 정산 책임·UI 표시 필드를 반영했다. v0 요청 `msg_211134afe227`의 회신 `msg_e09e88a8eab0`을 반영해 v1로 고정했고 `msg_819884e53401`로 알렸다. Content 변경을 가져오거나 금고 연결을 선행 구현하지 않았다.
- **선행 차단:** Content 클리어 서버가 main에 아직 없다. 그 PR이 늦으면 G2·G8 통합이 늦어진다. 초기 조사는 가능하지만 연결 구현은 앞서지 않는다.
- **설계 위험:** 개인 비용/강화와 맵 combat 반영의 원자성·수명, 동시 길드 구매, packet/계약 재생성, Core와 겹치는 세션 파일. 계약·선행 시험에서 해소해야 한다.
- **시연 한계:** 서버 재시작은 공동 성장 초기화, 재접속은 개인 강화 초기화다. 정식 인증·영속화·장비 시스템을 만든 것처럼 설명하지 않는다. 피해 증가·성장 효과의 화면 전달은 Content/Art 통합이 남는다.
- **내부 실패 한계:** 공동 revision 고갈/준비 실패에서39가 없으면 현 보상창과40 버튼도 없다. 서버40 수락 자격 보존만으로 UI 복귀 성공을 주장하지 않는다. 정상 재화 포화 정책과 구분하며 새 결과/UI는 이번 범위 밖이다.
- **자원:** 최초 Core SQL 우선 창은 KST03:08/UTC18:08까지였으나 메인 후속 `msg_f0eecb333347`가 UTC16:23 정리 종료를 전했다. 큰 실행은 여전히 직전 여유 물리 메모리 원시를 남기고 관련 리드에 알린 뒤 한다. port7777·Unity 실행권을 임의 점유하지 않는다.
- **현재 자원 조율:** Content `msg_8dc14bff76ad`와 Core `msg_a24975bd4b01`은 World 전용 WSL baseline/RED와 현재 확정 충돌이 없다고 회신했다. Core의 다음 SQL 검증 창과 Content의 Unity batch는 미예약이며 각자 다시 조율한다. World는 각 실행의 공식 ask·물리/커밋 각각4GiB 관문 뒤 시작/종료를 알린다. 이 Task는 Unity/DB/7777/봇·타 clone을 쓰지 않는다.
