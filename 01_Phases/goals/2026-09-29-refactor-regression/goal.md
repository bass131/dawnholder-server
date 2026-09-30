# M3 — 설정·주석 정리와 전체 회귀·재평가

상태: 구현·독립 TestCode·리뷰·전체 회귀·AI 재평가 완료, PR/최신 CI/병합 준비. base main `0b3b7224ef5e314d7a28bb8a0265f1e3a41fe07a`, branch `feat/refactor-config-regression`. [M2b](../2026-09-30-refactor-player-state/goal.md) PR #135의 독립 검증·리뷰·최신 CI를 통과하고 조건부 승인으로 병합한 뒤 시작했다.

## 선택 설계와 보존 계약

설정은 값을, 실행 시스템은 맵별 변화 상태를 소유한다. 외부화 자체보다 한 곳의 정의와 주입 가능한 경계를 만든다. 전체 콘텐츠 형식/모든 상수/새 전략 framework로 확장하지 않는다.

| 작업 단위 | 선택 설계와 이유 |
|---|---|
| 리스폰 배치 | 서버 internal readonly record struct EnemyRespawnPlacement(Vector2 GolemLeft, Vector2 GolemRight), Default=(-8.5,0)/(9.5,0). RespawnSystem이 readonly 값으로 주입받는다. 첫 좌측 flag·교대·대기 큐는 각 맵 시스템에 남는다. |
| GameMap 연결 | 기존 public ctor는 Default를 명시해 placement가 첫 인자인 internal overload로 위임한다. internal ctor가 RespawnSystem을 한 번 만든다. public API에 internal 타입을 노출하거나 GameWorld/Shared/Unity content 형식을 바꾸지 않는다. 명시 default(placement)의 영점을 자동 치환하지 않는다. |
| 지연 정의 | EnemyCatalog의 기존 RespawnTicks 행에 Normal100/Golem120/Boss0을 단일 정의한다. 실행 시스템 상수와 Catalog→System 참조를 제거하며 System→Catalog 소비 방향만 남긴다. |
| 통합 fixture | 기존 ServerFixture가 ServerHost를 단일 소유한다. loopback port0로 Start 후 LocalEndPoint에서 Port를 얻고 Dispose는 Host.Stop에 위임한다. listener/세션/world 정리를 fixture에 다시 구현하지 않는다. |
| 주석·진단 | 실제 tick 순서/킬 전달/보스 빈 방·terrain binding을 현재 코드와 맞춘다. migrating 때도 생기는 null-map 로그를 config/shutdown race로 단정하지 않게 수정하되 입력 drop 동작은 보존한다. |

보존 정책:

- 최초 적 배치는 content, Normal 재출현은 원본 SpawnX/Y, Golem은 맵마다 첫 좌→우다. 각 생성 때 toggle하고 같은 tick 다중 만료의 기존 역순 처리를 유지한다.
- death가 GameMap job에서 enqueue되면 같은 tick Respawn.Process에서 첫 감소가 일어난다. 기존 100/120 tick 의미, 새 AllocId·원본 MaxHp/Stats·spawn packet 1회 의미를 보존한다.
- Boss0은 timed queue 제외이며, 빈 방+boss 없음+content 존재의 MaybeRespawnBoss는 별도 경로다. StageClear reset과 사망/낙사 callback 차이를 유지한다.
- fixture World/Port 소비 API와 기존 5개 통합 소비자를 유지한다. 외부 사용 없는 Listener 프로퍼티와 probe-port race는 제거한다. Host.Start뿐 아니라 endpoint/Port 게시까지 생성자 실패 경계를 포함한다. ctor 실패에는 Host.Stop을 시도하고 원래 예외와 cleanup 예외를 보존한다. timeout에서 finally World.Stop으로 살아 있는 callback 아래 world를 강제 해제하지 않는다.
- 이 작업은 게임 튜닝·새 리스폰 정책·패킷/DB/인증 변경이 아니다. 이미 M2b에서 정리된 snapshot 주석을 재작성하기 위해 소스를 넓게 수집하지 않는다.

## 파일 소유와 순서

- 메인: goal/CURRENT·이전 통합 결과·Git/PR·실행·평가 분리·종합 판단.
- client_flow_audit 구현자: 기존 `02_Server/GameServer/Maps/Systems/RespawnSystem.cs`, `Combat/EnemyCatalog.cs`, `Maps/GameMap.cs`; 신규 `Maps/EnemyRespawnPlacement.cs`. 부속 좁은 범위는 `Sessions/GameSession.cs`의 null-map 진단 문구와 `03_Client/Assets/Scripts/Prediction/LocalPlayerMovement.cs`의 옛 terrain 주입 주석만이다. fixture 구현은 `02_Server/GameServer.Tests/Integration/M2BasicMovementIntegrationTests.cs` 안의 ServerFixture 부분만 같은 구현자가 소유한다. fixture 테스트 작성자와 구분한다.
- db_scope_review 독립 TestCode: 구현 쓰기 종료 후 기존 `GameServer.Tests/Combat/EnemyCatalogValueTests.cs`, `Maps/GameMapContentInjectionTests.cs`; 신규 `Maps/RespawnPlacementTests.cs`, `Integration/ServerFixtureLifecycleTests.cs`. BossEmptyRoomRespawnTests/EnemyGravityTests/HandleEnemyDeathKillerTests는 우선 회귀 실행 대상으로, 필요한 추가 쓰기는 정확 경로를 main에 보고한다. 구현 fixture 파일을 동시에 쓰지 않는다.
- refactor_scope_review: 독립 source/TestCode 리뷰 및 누적 HTML 단일 writer. AI 답안 역할은 코드·테스트 쓰기 종료 및 고정 commit 확인 뒤 별도로 배정한다.

원시 결과 `.backups/verification/2026-09-30-regression/`. 사전 제안/설계 리뷰/TestCode 계획은 `.backups/reviews/2026-09-30-m3-{api-proposal,design-review,test-plan}.md`이며 구현·실행 성공 증거가 아니다. 구현→독립 TestCode→리뷰→검증 순서를 유지하고 같은 파일 동시 쓰기를 금지한다. worker 권한 제한 시 main의 기계 실행과 독립 raw 판정을 구분한다.

## 회귀·평가 계획

- 전체 서버 build/test로 기존798 및 새 계약 사례, 통합 fixture 소비자5개(M2BasicMovement/LagSim/PartyQuest/MapTransition/BossGate)를 확인한다. lifecycle 새 테스트는 실제 handshake/입장 양성→Dispose 후 socket EOF/reset·listener 거부→다음 fixture 성공 및 반복 Dispose를 bounded wait로 확인한다. endpoint 실패 injection seam이 없으면 그 부분은 정적 검토로 구분한다.
- production 봇은 fresh server 실행별 EmergencyCombatSmoke, HpSyncSmoke, BossFightSmoke, MapTransition을 선택한다. raw의 death 대체 flag/부활 필수 여부/DEBUG unlock를 성공 숫자와 함께 기록한다. standalone PartyQuest/BossGate는 퀘스트 seed가 없으므로 전체 xUnit socket 경로와 혼동하지 않는다.
- Unity compile/EditMode, 실제 SceneManager PlayScenes, 실제 production GameServer PlayServer를 수행한다. Shared/ClientNet 변경이 없으므로 M2b 검증 DLL hash를 확인하고 임의 재복사하지 않는다. 자동 가상 입력과 사람의 물리 조작/화면/청취는 분리한다.
- [고정 과제 v1](../../milestones/2026-09-29-refactor-before-persistence/assessment.md)의 동일 T1–T4, 동일 4항목×0–2점 기준을 쓴다. 가능한 M0와 같은 답안자(refactor_scope_review)/별도 채점자(client_flow_audit)를 유지하되, 둘 모두 구현·감사 맥락을 알고 있어 blind benchmark가 아님을 명시한다. 고정 source commit, 과제별 시작/종료·10분 한도·읽은 고유 파일/검색·읽기 호출을 실제 수집 가능한 범위에서 기록한다. 정확 하위 모델/effort를 독립 확인하지 못하면 unknown으로 두며 사용자 root 실행 명령으로 대체하지 않는다.
- M0는 답안 근거 적합도29/32이며 탐색시간/효율의 완전한 기준선이 아니다. M3 점수 차이를 전체 코드 품질·모델 능력·인과적 생산성 향상률로 주장하지 않는다. 상태 소유/도메인 직접 쓰기/검증 경계/주석 정확성을 실제 before/after 심볼과 함께 비교한다. source freeze 뒤 채점 전 기존 답안을 정답으로 제공하지 않는다.

## 완료조건

- [x] 선택 설계·보존값·파일 소유·실행·평가 경계를 구현 전에 확정했다.
- [x] 배치/지연 소유를 분리하고 좌표·교대·틱·ID·보스 빈 방 정책을 독립 TestCode로 확인했다.
- [x] fixture의 listener/session/world 소유를 기존 Host로 모으고 실제 종료/재시작을 독립 검증했다.
- [x] 현재 주석·진단을 구현과 맞추고 정책/미구현 주장을 정리했다.
- [x] 전체 서버·봇·Unity 회귀 결과와 M0 비교·미실행/실패 이력을 기록했다.
- [x] 고정 과제 원점수·조건·계측·한계와 구조 지표를 독립 평가했다.
- [ ] 독립 source/TestCode 리뷰·최신 head CI·크리티컬 이슈 부재 확인 뒤 조건부 승인으로 직접 병합했다.
- [x] HTML에 실제 diff·평가·검증 결과와 한계를 기록하고 후속 DB 설계에 필요한 사실/미합의 결정을 인계했다.

## 실제 결과와 인계

지정7파일의 구현·쓰기 종료. placement 주입과 Catalog100/0/120, fixture Host 단일 소유, 실제 계약 주석/진단을 반영했다. API·정적 점검·한계는 `.backups/verification/2026-09-30-regression/implementation/summary.md`에 인계했다. 독립 코드 리뷰의 미해결 P0–P3는 없으며 Tick XML summary의 실제 순서 불일치를 주석만 수정해 해소했다(`review/code.md`). 독립 TestCode 4파일과 재리뷰를 완료했고 잔여 P0–P2는 없다(`review/test-review.md`). 실제 결과는 다음과 같다.

- 첫 서버 build는 새 lifecycle 테스트의 Maps using 누락(CS0246)으로 exit1, 테스트 미실행이었다. 누락 using과 새 Assert.Single 경고 표현을 동치 수정했다. 원문 `verification/server-suite.log`를 보존한다.
- 최종 Debug 전체 suite exit0: **808 total / 803 pass / 0 fail / 5 기존 skip**. 직전 M2b798 누락0, 신규10(리스폰8·fixture2) 모두Passed. 기존6 warning/0 error(`server-suite-2.log`, `server-results-2/`). M0의713/708pass/5skip 대비 시험 범위가 늘었으며 같은 시험만 실행한 수치가 아니다.
- fixture 실제 handshake/입장→종료 EOF/reset/새 접속거부→다음 fixture 입장 및 반복 Dispose가 통과했다. 생성자 endpoint 오류 injection/timeout 강제주입은 하지 않았다. 테스트 WaitAsync timeout이 내부 Dispose task를 취소한다고 보장하지 않는다.
- production 봇은 각각 fresh server에서 exit0, 각 **PASS1/FAIL0**. EmergencyCombatSmoke: hits2·deathTrue·optionBFalse·target HP30→-20·rateLimitDroppedTrue. HpSyncSmoke: initialFull/damage/zero/reviveFull 모두True, max150/events15. BossFightSmoke: hits7·HP150→-4·StageClearTrue·EnemyAttack3, respawnFalse(필수조건 아님). MapTransition: HG2/Boss22/Ending0/Town0 spawn·ID7 보존. 원문/종료 receipt는 `verification/bots/`.
- BossFight/MapTransition은 DEBUG 해금을 사용한다. 실제 퀘스트20킬 완료로 과장하지 않으며 파티·퀘스트 적립/수신자/게이트는 전체 suite의 socket+seed 계약과 구분한다. 실제 플레이어 사망→풀HP 부활은 HpSync가 확인했다.
- Unity EditMode native0 **249/249**, GPU SceneManager/scripted peer PlayScenes native0 **10/10**, production GameServer PlayServer native0 **1/1**. 실제 Town→HG→Town entity7/roster8/HP143/150 관측. 원문 `verification/unity/`. Shared/ClientNet은 M2b 검증 hash 유지, 이번 DLL 복사0(`verification/dll-hashes.json`). 기존 meta/scene/prefab/asset/DLL diff0·InitTestScene 잔여0.
- task-owned server PID899는 exact stdin FIFO newline으로 graceful exit0, `verification/server/server.log`에 Server stopped. main이 port7777/PID 부재 확인. 외부 SQL/다른 process는 조작하지 않았다.
- 수동 GUI/물리 키보드/오디오 청취는 미수행. 자동 실제 엔진/가상 InputSystem 및 production 봇 검증을 수동 평가나 DB 연동 성공으로 보고하지 않는다.

독립 raw 판정은 `verification/summary.md`. 최신 PR CI/병합 결과는 아직 대기다. 다음 [DB 설계](../2026-09-29-persistence-design/goal.md)는 최종 main과 안정된 상태 수명·비동기 경계, 기존 schema/접속 인계를 대조한다. DB 저장·인증·schema 적용 구현 권한으로 확대하지 않는다. 저장 범위/계정 모델에 관한 사용자 질의는 응답 대기이며 답변 없는 항목을 합의로 취급하지 않는다.

## 고정 과제 재평가와 구조 비교

평가 source commit은 `2f2fbed37446ac9c19e27676b68d1ef3d3eccdec`로 고정했다. 이후 통합 문서 갱신은 코드·테스트 변경 없이 진행한다. 동일 T1–T4 답안자 refactor_scope_review, 독립 채점자 client_flow_audit이며 두 작업자 모두 과거 감사/구현에 노출됐다. 정확 runtime 모델/effort는 unknown이고 blind benchmark나 리팩토링의 인과적 생산성 평가가 아니다. 기존 답안·채점을 이번 답안 작성에 제공하지 않았다.

답안은 `.backups/verification/2026-09-30-regression/assessment/answers.md`에 고정했다. 과제별 실측 78.2/81.2/68.8/76.5초로 각각 10분 이내이며, 과제 도구 호출11·rg14·source 읽기37, 고유 source28개와 시작 문서3개를 기록했다. 준비/기록 호출은 별도 집계하고 저장 뒤 무관 shell token 오류1회도 공개했다. 이는 답안자 기록 계측이며 채점자가 전체 도구 이력을 독립 감사한 수치가 아니다. M0의 사전 탐색 계측이 불완전하고 배치 크기가 다르므로 시간·검색 감소율을 계산하지 않는다.

독립 채점 `assessment/grades.md`의 결과는 **31/32**: T1 위치·흐름/경계·영향/검증/불확실성=(1,2,2,2), T2–T4는 각각(2,2,2,2)다. T1에서 실제 MainMenu의 Disconnect→GameEntryPoint의 IsConnected/Connect 재진입 호출자 경로 1묶음이 누락됐으며 사실 정정 요청은0이다. 원본 답안을 사후 보정해 점수를 높이지 않았다. M0의 29/32(T1–T4=6/8/8/7) 대비 +2는 중복 완료 검증과 tick 캡처 소유 설명의 보완이다. 핵심 누락은3→1, 사실 정정 요청0→0으로 비교한다. 네 답안 설명 적합도의 보조 환산96.875/100이며 전체 코드 품질·외부 공인 AI Readiness·인과적 개선률로 보고하지 않는다.

`assessment/structure.md`는 M0 게임 코드 `4b2c84e3bb94d91452d0b50af0ed76b107d5d6a6`와 위 source를 좁게 대조했다. QuestRegistry가 PartyState.KillCount를 직접 쓰던 문장 **3→0**, PlayerStats의 public mutable setter 속성 **1→0**을 확인했다. 서버 종료는 world close queue/Host, 클라이언트 연결은 lifetime·entry·물리 load queue, 퀘스트 진행은 Quest owner, 전달값은 불변 정의·현재 HP 캡처로 책임을 구분했다. Unity singleton/mirror·entity setter·migration 상태는 필요한 경계에 남아 있다. 이 수치를 repository 전체 결합 총계·완전한 SOLID 준수·DB 연동 완료로 해석하지 않는다.

HTML의 실제 diff18개와 CSS/JS를 보존해 누적 결과를 갱신했다. 원 담당자의 사용량 제한 오류 종료 후 db_scope_review에 HTML 단독 쓰기를 이관했고 쓰기 종료 후 메인이 브라우저 QA를 실행했다. 2026-09-30T01:13:28Z, 1440/430/320/768px 가로 overflow0, light/dark·details·print 확장/복원 통과, 430px light screenshot을 실제 확인했다(`.backups/report-preview/qa-results.json`, `qa-m3.log`). DB 사전안과 독립 리뷰는 별도 설계 단계 입력이며 저장 구현·현재 SQL 접속 성공을 뜻하지 않는다.
