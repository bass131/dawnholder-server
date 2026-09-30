# M2b — 기본 스탯과 현재 상태·캡처·이동 값

상태: 구현·독립 TestCode·리뷰·서버/Unity 검증 완료, PR CI 준비. base main `a0c60943cc89d74830d826044bd8fb852a458e7b`, branch `feat/refactor-player-state`. [M2a](../2026-09-30-refactor-party-quest/goal.md) PR #134의 독립 TestCode·리뷰·최신 CI를 통과하고 조건부 승인으로 병합한 뒤 시작했다. [M2](../2026-09-29-refactor-domain-state/goal.md)의 두 번째 목표다.

## 선택 설계

기본 정의와 현재 상태가 같은 mutable PlayerStats.Hp에 섞여 보이던 경계를 분리한다. 기본 정의를 불변으로 만들면 snapshot의 공유 참조도 캡처 이후 바뀌지 않으며 매번 정의를 복제하는 관례가 필요 없다. 이동은 필요한 값만 묶어 전달한다. 새 성장·장비·attribute framework는 도입하지 않는다.

| 경계 | API/소유 |
|---|---|
| 기본 정의 | 기존 sealed PlayerStats 및 Knight/Mage/ForClass factory 유지. Hp getter/setter를 제거하고 InitialHp getter만 둔다. 나머지 값형 getter와 수치는 유지한다. |
| 현재 HP | PlayerEntity.Hp가 단일 원본이다. 생성 시 Stats.InitialHp/Stats.MaxHp를 읽으며 기존 Hp/MaxHp setter·사망/부활 정책을 유지한다. 모든 entity.Hp를 일괄 rename하지 않는다. |
| 캡처 | PlayerSnapshot readonly record struct의 Hp만 CurrentHp로 명확히 한다. EntityId/Position/CurrentHp/현재 MaxHp는 값 복사, Stats는 sealed getter-only 불변 참조를 공유한다. CaptureSnapshot이 생성한 값의 안정성과 임의 default struct의 유효성은 구분한다. |
| 이동 | 새 PlayerTransferState(int EntityId, PlayerStats? Stats, int CurrentHp). MapMigration이 제거 전에 한 번 캡처한다. session owner/목적 map/spawn은 별도 orchestration 인자다. |
| 목적지 생성 | AddPlayerWithId(PlayerTransferState transfer, GameSession? owner, Vector2 spawnPos). 기존 PlayerEntity 생성 뒤 raw CurrentHp를 대입한다. 구 raw 인자 overload를 남겨 중복 계약을 만들지 않는다. |

PlayerStats.Hp 제거는 공개 Shared ABI 변경이다. 서버·봇 재컴파일과 검증 Shared DLL 반영·Unity compile/테스트까지 같은 목표에서 완료한다. 이름/factory 유지 자체가 옛 get_Hp/set_Hp 바이너리 호환을 보장하지 않는다.

## 보존 정책과 한계

- Knight 150/150·15·5·4·8, Mage 80/80·12·2·6·8, invalid class Knight fallback을 유지한다. initial HP와 MaxHp의 개념은 별개다.
- 이동은 기존 entity ID·Stats·현재 HP를 보존하고 위치는 목적 portal spawn을 쓴다. 새 entity.MaxHp는 source의 현재 MaxHp가 아니라 Stats.MaxHp로 재구성하는 기존 정책을 유지한다. 현재 HP를 새 최대값으로 clamp하거나 회복시키지 않는다.
- 기존 AddPlayerWithId는 nonnullable 표기에도 PlayerEntity의 null→Knight fallback으로 이어진다. Transfer Stats를 nullable로 명시하고 동일 fallback을 보존한다. default transfer도 Stats=null/ID0/HP0일 수 있다. 새 guard·optional 인자·ID 정책을 넣지 않으며 default를 검증된 정상 데이터라고 보장하지 않는다. 실제 migration은 살아 있는 player의 명시 값으로 생성한다.
- 음수/최대 초과 raw HP 보존은 AddPlayerWithId/캡처 값 계약이다. gameplay migration의 기존 owner/portal/근접/보스 gate를 보존한다. 현재 별도 IsDead/Hp gate는 없으며 새 사망 gate를 추가하지 않는다. 패킷 표시 보정과 값 저장을 혼동하지 않는다.
- 새 entity 생성에 따른 cooldown/FSM/input/history/무적 초기화, closing 가드·0~1 owner, MapTransition→HP→roster 순서를 보존한다. transient를 transfer에 새로 넣지 않는다.
- 사망 복구의 Stats.MaxHp 사용을 현재 entity.MaxHp로 조용히 바꾸지 않는다. Snapshot의 DB primary key/20TPS write queue/race 없음 같은 미구현 보장 주석을 실제 tick 안의 값 캡처 설명으로 고친다.

제외: DB schema·인증·저장/복원·서비스 변경, 전투/성장 정책, wire/PDL, 기존 Unity 에셋/메타/직렬화 값. DTO 정리는 게임 저장 연동 완료가 아니다.

## 파일 소유와 실행 경계

- 메인: goal/CURRENT·이전 목표 통합 기록·Git/PR/실행/DLL 복사 조율.
- client_flow_audit 구현자: 기존 `98_Shared/GameData/Combat/PlayerStats.cs`, `02_Server/GameServer/Entities/PlayerEntity.cs`, `Maps/{PlayerSnapshot,GameMap}.cs`, `Maps/Transitions/MapMigration.cs`, `Sessions/GameSession.cs`의 클래스 로그 getter만; 신규 `Maps/Transitions/PlayerTransferState.cs`. 테스트/Unity/ClientNet/PDL/goal/report/DLL 쓰기는 제외한다.
- db_scope_review 독립 TestCode: 구현 종료 후 `GameServer.Tests/Maps/{PlayerSnapshotTests,PlayerStatsWiringTests,MapMigrationTests}.cs`, `Party/{GameWorldPartyIntegrationTests,QuestKillCountTests,QuestNotificationContractTests}.cs`의 직접 API 적응 및 계약 검증. Unity의 `Assets/Tests/EditMode/HudAndMapTests.cs`, `Assets/Tests/EditMode/Prediction/PlayerPredictorTests.cs`에서 클래스/HUD/예측값을 필요한 범위로 확인·보강한다. 추가 파일은 정확 경로를 보고해 배정받는다.
- refactor_scope_review: 독립 source/TestCode 리뷰 및 누적 HTML 단일 writer.

Shared/서버 source는 단일 writer다. source 쓰기 종료 후 테스트를 작성하고 컴파일·실행한다. Unity 실행 중 DLL을 교체하지 않는다. WSL clone의 검증된 `98_Shared/bin/Debug/netstandard2.1/Shared.dll`을 메인이 원본 Unity plugin에 선택 복사하고 SHA-256을 대조한다. 기존 DLL .meta/GUID는 보존한다. ClientNet source/API 사용 변화가 없어 DLL을 임의 교체하지 않는다. Windows 전체 build의 자동 DLL 복사 부작용을 피한다.

원시 결과 `.backups/verification/2026-09-30-player-state/`, 사전 제안/리뷰/테스트 계획 `.backups/reviews/2026-09-30-m2b-{api-proposal,design-review,test-plan}.md`. 원문은 실행 성공으로 취급하지 않는다. worker 권한 제한 시 main이 정확 명령을 대행하고 독립 검증자가 raw를 판정한다.

## 완료조건

- [x] 설계·null/default/HP 정책·파일 소유·DLL 실행 순서를 구현 전에 확정했다.
- [x] 불변 정의·현재 HP·목적별 캡처/이동 값의 단일 소유를 구현했다.
- [x] 두 entity/current HP 독립, 두 캡처/원본 mutation 독립, 클래스 기본값·fallback·null/default·raw HP/MaxHp 경계를 독립 TestCode로 검증했다.
- [x] 실제 migration의 ID/HP/목적 spawn·transient 초기화·closing/owner와 기존 Party 소비자·전체 서버/봇 컴파일·회귀를 확인했다.
- [x] 검증 Shared DLL hash/기존 meta/ClientNet 보존, Unity compile/EditMode·실제 씬 PlayMode·실제 서버 왕복을 수행하고 수동 플레이 한계를 구분했다.
- [ ] 독립 source/TestCode 리뷰·최신 head CI·크리티컬 이슈 부재 확인 뒤 이번 로드맵 조건부 승인으로 직접 병합했다.
- [x] 누적 HTML에 실제 diff와 실행 결과/한계를 기록했다.

## 실제 결과와 다음 단계

production 기존6+신규1파일 구현과 독립 TestCode server6+Unity2파일 쓰기를 종료했다. 상세 API는 `implementation/summary.md`, source/TestCode 리뷰는 `review/{code,test-review}.md`이며 미해결 P0–P2는 없다. 섹션의 옛 M8 영속화 주석도 실제 tick 캡처 설명으로 수정했다.

- 첫 서버 suite는 798 total/792 pass/1 fail/5 skip이었다. 신규 roster fixture가 owner:null resident를 만들어 기존 publisher의 null-owner 제외 계약에 걸렸다. 유효한 ObserverSession owner와 nonclosing 전제를 주입하고 3패킷 순서·정확 ID/class assertion은 유지했다. 생산 코드를 바꾸거나 기대값을 줄이지 않았다. 첫 실패 원문은 `verification/server-suite.log`, `server-results/`에 보존했다.
- 최종 전체 서버 suite: exit 0, **798 total / 793 pass / 0 fail / 5 기존 skip**(`verification/server-suite-2.log`, `server-results-2/`). 기존775 fullname 누락0, 신규23 모두Passed를 독립 대조했다. 서버/봇/Shared/ClientNet 소비자 재컴파일 성공, 기존6 warning/0 error.
- 검증 WSL Shared.dll을 Unity 종료 상태에서 선택 복사했다. source/target SHA-256 **B82F6A3285FD8196F8E5E0CD7476CAAE68C852458A40F68569ECE7F096D4EE11** 일치. 기존 Shared.meta와 ClientNet.dll hash는 유지(`preflight/dll-before.json`, `dll-copy.json`). 의도된 Shared DLL 외 기존 meta/scene/prefab/asset 변경0, 임시 InitTestScene 잔여0.
- Unity 6000.4.7f1 EditMode: native exit0 **249/249**, 기존240 누락0·신규HUD6/예측3 통과. GPU 실제 SceneManager+scripted TCP PlayScenes: native0 **10/10**. 실제 GameServer 연동 PlayServer: native0 **1/1**, Town→HuntingGround→Town/HP/roster/ID 보존. raw는 `verification/unity/{editmode-1,playscenes-1,playserver-1}/`에 분리했다.
- 실제 서버 task-owned Linux PID889는 exact stdin FIFO newline으로 정상 종료(exit0). 원문 `verification/server/server.log`의 Server stopped와 port7777/PID 부재를 main이 확인했다. 다른 프로세스/SQL 서비스는 조작하지 않았다.
- 자동 실제 엔진 테스트는 가상 InputSystem을 사용한다. 수동 화면/fade 평가·물리 키보드·오디오 청취는 미수행이며 DB 연결/저장 검증도 아니다. 이번 goal에서는 별도 production bot을 반복 실행하지 않았으며 실제 socket suite와 Unity 왕복 범위를 구분한다.

독립 raw 판정은 `verification/summary.md`에 둔다. 최신 PR head CI와 병합 결과는 통합 후 기록한다. 후속 [M3](../2026-09-29-refactor-regression/goal.md)는 설정·주석·회귀와 AI 로컬 재평가를 진행한다. DB 구현은 여전히 제외한다.
