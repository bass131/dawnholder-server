# S2 — 서버 패킷 표현 책임 통합

상태: `refactor/server-packet-publication` 구현·독립 테스트·리뷰 완료, [PR #142](https://github.com/bass131/dawnholder-server/pull/142) 통합 대기. 구현·테스트 commit `f1ebd15`. S1 PR #140의 검증된 head `2b533c752076e3e00cfdfe081a9f45d824059606`를 선행 기준으로 삼는다. 미병합 선행 변경을 사용하는 이번 로드맵의 stacked PR이며 main 통합 완료를 뜻하지 않는다. PR은 종합 보고서에서 사용자와 확인하기 전 병합하지 않는다.

## 목표와 설계

플레이어 입장과 일반 적·보스의 주기적 상태 스냅샷 패킷 조립을 기존 `MapPacketPublisher`에 모은다. 최초 입장·맵 이동·roster의 같은 플레이어 표현이 세 곳에 있고, 주기적 적 상태 표현이 AI/보스 시스템에 중복되어 표현 변경 이유가 분산되어 있다. 기존 publisher와 GameMap 위임을 사용하며 새 상태·인터페이스·범용 프레임워크를 만들지 않는다. `BossStates.BeginTelegraph`의 별도 즉시 공격 예고 발행은 이번 여섯 파일 범위 밖으로 유지하며, 모든 EntityState 조립의 단일화를 주장하지 않는다.

등록·이동·수신자 정책은 기존 소유자에 남기고, AI/FSM/애니메이션 선택은 각 시스템이 결정한다. 일반 적 Hit 우선과 보스 Attack 우선의 차이는 의도적으로 유지한다. 다른 서버 영역은 대표 책임 경계 조사에서 추가 분리의 근거가 부족해 현행 유지한다. 이는 모든 파일·분기의 전수 검증을 뜻하지 않는다.

## 보존 계약과 완료조건

- 최초 EnterMap→HP→등록 전 roster→타인 join, 맵 이동 MapTransition→HP→roster→타인 join 순서와 정확히 한 번 발행, 본인 제외·closing 필터를 보존한다.
- PlayerJoin/EntityState의 전체 bytes·필드·수신자·tick, snapshot 간격, freeze/dead skip, AI→보스→중력 발행 시점을 유지한다.
- 맵 등록·세션 종료·월드 actor 큐·FSM·피해·게임 정책은 변경하지 않는다. PDL·공유 DLL·Unity 자산·DB는 수정하지 않는다.
- 별도 Astra 검증자가 테스트 코드를 보완해 실제 최초 입장/이동/tick 경로와 publisher 표현을 확인한다. 기존 경로 테스트는 기준 소스와 비교한다.
- 관련 계약 테스트 및 solution build/test 통과, 별도 diff 검토, 변경 이유·측정·미실행 범위 기록 후 PR 생성. 대상 밖 경고와 미조사 영역을 구분한다.

## 분할과 소유권

- 메인: 이 goal·CURRENT·로드맵, 범위·결정·통합·보고.
- Sol 구현자: GameServer의 `Sessions/GameSession.cs`, `Maps/Transitions/MapMigration.cs`, `Maps/GameMap.cs`, `Maps/MapPacketPublisher.cs`, `Maps/Systems/EnemyAISystem.cs`, `Maps/Systems/BossBehaviorSystem.cs`, 관련 `domains/server.md` 계약, 해당 파일에 한정한 `.editorconfig` 정적 규칙 확장과 CODE_CONVENTION 검사 범위 설명.
- Astra 검증자: `Maps/MapPacketPublisherByteParityTests.cs`, 신규 `Maps/MapPublicationContractTests.cs`; 필요 시 기존 관련 테스트의 파일 소유권을 추가 배정한다. 생산코드를 수정하지 않고 결함은 구현자에게 반환한다.
- 독립 리뷰: 구현·테스트 작성자 외 Astra가 보존 계약과 책임 경계를 읽기 검토한다.

추가 위임 금지. 쓰기 종료 후 명시적으로 소유권을 이전한다. 구현 Sol6.1 / 검증 Astra 요청과 실제 런타임 확인 여부를 구분한다.

## 검증과 측정

DEVELOPMENT의 WSL 격리 실행을 사용한다. 검증자 한 명이 동기화·빌드·테스트 공간을 소유하며 원본 Unity DLL을 복사하지 않는다. 구현 전 선행 head 기준선과 관련 소스를 보존하고 구현 후 새 테스트의 계약 기대를 비교한다. 기존 S1의 세 정적 error 규칙을 추가 수정 production 파일에도 적용하고 정상 통과와 대표 위반 실패를 격리본에서 검증한다. 무관한 warning을 제거하지 않는다.

근거: `.backups/reviews/2026-09-30-s2-server-assessment.md`, `2026-09-30-s2-test-plan.md`. 실행 결과는 `.backups/verification/2026-09-30-server-packet-publication/`에 보존한다. 비교 지표는 wire 조립 소유 지점과 실제 통합 계약 검증이다. 복잡도·AI 탐색 시간을 계측하지 않았다면 미측정으로 남긴다. 서버 테스트는 Unity 플레이·DB 검증을 대신하지 않는다.

## 결과와 다음 단계

Sol 구현자와 별도 Astra 테스트 작성자의 쓰기 종료를 확인했다. 다른 Astra의 읽기 리뷰에서도 수정 필요 결함이 없었다. 지정 모델은 각각 Sol6.1/Astra이며 실제 런타임 모델은 독립 확인하지 못해 unknown이다.

| 검증 | 실제 결과 |
|---|---|
| WSL solution 강제 빌드 | 성공, 기존 warning4개 |
| 전체 회귀 | 831 total / 826 pass / 5 기존 skip / 0 fail |
| 관련 회귀·새 계약 | 관련95/95, 신규11/11 통과 |
| 새 실제 경로의 기준 소스 비교 | 7/7 통과; 신규 publisher API4개는 기준선에 없어 제외 |
| 추가5파일 정적 규칙 | 각 파일의 예상 SA/IDE error와 exit1 확인; 대상 밖 warning·Tests/Tools 완화 유지 |
| 독립 코드 리뷰 | 수정 필요 결함 없음 |

전체 회귀 후 새 테스트의 동등한 Assert.Single predicate 표기 한 곳만 정리하고 최종 강제 빌드와 신규11개를 재검증했다. 생산코드는 같으며 전체 회귀 당시/최종 manifest를 분리해 보존했다. 원시 명령·환경·결과·정확한 skip 목록·해시는 `.backups/verification/2026-09-30-server-packet-publication/summary.md`와 연결된 파일, 별도 리뷰는 `.backups/reviews/2026-09-30-s2-packet-contract-review.md`다. GitHub의 정확한 PR head CI는 PR 생성 후 확인한다.

PR 생성 뒤 기존 workflow의 `pull_request.branches: [main]` 때문에 stacked PR의 GitHub CI가 아예 시작되지 않는 것을 확인했다. Sol이 해당 필터 한 줄만 제거하는 patch를 작성했고 별도 Astra가 문법·범위·권한 불변을 검토했다. 메인이 이를 적용했으며 push 조건·job·명령·권한은 그대로다. 이는 이번 PR의 원격 검증 진입 조건 보완이다. 원격 실행 결과는 정확한 head로 별도 확인한다.

확인한 변화는 PlayerJoin 조립3→1, 선택한 두 시스템의 주기 EntityState 조립2→1과 실제 입장/이동/tick의 bytes·순서·수신자 보존이다. SnapshotTickInterval은 현재1이므로 정상 비발행 tick 테스트를 수행했다고 주장하지 않는다. 일반 적의 사망 검증은 제거 후 상태 통지 부재이며 보스에는 명시 dead guard가 있다. 별도 즉시 공격 예고 발행은 유지했다. 복잡도·AI 탐색 시간·성능·전수 감사·별도 봇·Unity/시각 플레이·DB는 미측정 또는 미실행이다.

후속 S3에는 사용자가 승인한 서버 Disconnect 오류 경로 보강을 별도 목표로 진행한다: 자원 정리를 보장한 뒤 기존 예외를 전파한다. S3 부분 송신 후보는 실제 정상 API 도달성 근거가 없어 보류했다. 전체 단계와 의존성은 [로드맵](../../milestones/2026-09-30-maintainability-rollout/roadmap.md)을 따른다.
