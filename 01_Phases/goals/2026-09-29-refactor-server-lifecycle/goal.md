# M1a — 서버 시작·종료와 세션 해제의 소유권

상태: 구현·로컬 검증·독립 리뷰 완료, [PR #131](https://github.com/bass131/dawnholder-server/pull/131) 병합 승인 대기. [M1](../2026-09-29-refactor-lifecycle/goal.md)의 첫 독립 PR이다. base main `95058e585ac4a9c12f305b7135db124d53171941`, branch `feat/refactor-lifecycle`, 구현·테스트 커밋 `e31a399`. M0 PR #130 병합과 다음 단계 진행은 사용자 승인됨. PR #131은 별도 승인 대상이며 아직 승인·병합하지 않았다. 최신 head의 CI는 PR 검사 결과에서 확인한다.

## 목표와 선택 설계

연결 종료가 순간적인 현재 맵 조회에 의존하지 않도록 하고, 호스트가 접속 수신·연결·월드 틱의 시작과 종료 순서를 소유한다. 현재 단일 월드 틱과 맵 작업 큐, 출발 제거→목적지 등록의 두 단계 이동을 유지한다.

현재 종료는 소켓 스레드에서 `GetMap()`이 null이면 제거 요청을 보내지 않는다. 목적지 등록 작업의 closing 검사 뒤 종료가 오면 제거 요청이 소실될 수 있다. closing 재검사만 추가해도 마지막 검사 뒤의 창은 남는다. 선택은 **GameWorld의 종료 요청 큐와 작은 ServerHost**다. 전체 세션 FSM·DI 프레임워크·맵 역인덱스는 도입하지 않는다.

| 책임 | 소유자·실행 경계 | 계약 |
|---|---|---|
| accept와 연결 목록 | Listener/ServerHost, 제어·전송 흐름 | 준비 완료 후 accept, 정지 후 새 연결 시작 방지 |
| handshake·직업 선택·진입 요청 gate | GameSession 수신 흐름 | 기존 first-packet/중복 진입 규칙 보존 |
| closing 의도 | GameSession atomic gate | 한 번 설정 후 항상 월드 해제 요청 제출 |
| entity ID·맵·이동 상태 | 월드 틱에서 실행되는 맵 작업 | 소켓의 맵 조회는 입력 라우팅 힌트이며 해제의 권위가 아님 |
| 맵 엔티티·파티 해제 | GameWorld 틱 | 모든 맵에서 세션 참조로 제거하고 상태 정리 |

### 세션 종료·이동

1. `OnDisconnected`는 한 번 closing을 설정하고 `RequestSessionClose(session)`를 제출한다. 맵 이동 중에도 생략하지 않는다.
2. 월드 틱은 맵 순회 전과 맵 순회 후/Party 처리 전에 종료 요청을 드레인한다. 모든 맵의 owner reference를 확인해 실제 등록만 제거하고 leave를 한 번 통보한다. 기존 ID로 파티를 정리한 뒤 세션 entity/이동 상태를 초기화한다.
3. 등록 수는 세션당 전체 맵에서 항상 0/1이다. closing 순간 즉시 0이라는 보장은 하지 않는다. 종료 요청을 처리한 틱 경계 이후에는 0이고, 지연된 진입·이동 작업이 되살리지 않는다.
4. 큐에 들어간 이동·공격·스킬·포탈 및 파티 요청은 실행 시 세션 생존과 해당 owner를 확인한다. 이미 시작한 동기 판정은 완료될 수 있지만 cleanup barrier 뒤 낡은 작업이 상태를 다시 만들면 안 된다.
5. GameWorld의 직접 해제는 모든 맵이 동일한 단일 틱에서 실행된다는 현재 구조에 한정한다. 향후 맵별 스레드 분리 시 이 경계는 재설계 대상이다.

### 호스트 시작·종료

- 월드 내부 구성을 끝낸 뒤 singleton을 공개한다. world 시작 후 listener를 열고 실패하면 만든 자원을 역순 정리한다.
- Listener의 멱등 Stop/Dispose는 listening socket과 진행 accept를 정리하며, Stop 반환 후 새 세션이 시작되지 않도록 한다.
- ServerHost는 handshake 전 연결도 추적한다. 정지는 accept 차단 → 진행 callback 정리 → 연결 disconnect → 월드 cleanup barrier → 실제 tick 종료 확인 → 참조 해제 순서다.
- timeout/fault는 종료 성공으로 숨기지 않는다. scheduler가 끝나기 전에 참조를 비우거나 새 월드 시작을 허용하지 않는다. 대기는 호스트 제어 흐름에서만 수행하고 게임 틱에서 I/O나 자기 종료를 기다리지 않는다.
- `Listener.Completion`은 accept 작업 전체의 종료를 관찰하는 읽기 전용 Task다. 모든 작업이 끝난 fault는 안전한 연결·월드 정리를 마친 뒤 보고하고, callback이 아직 살아 있는 timeout은 참조를 유지한다. 회복 가능한 accept 오류와 terminal fault를 구분한다.

## 보존 계약과 제외

패킷 ID/필드/직렬화/버전, 최초 Town 진입, handshake→직업 선택 gate, 이동 중 입력 drop, 동일 entity ID/HP/Stats 이동, 현행 쿨다운 초기화와 파티·퀘스트 정책을 보존한다. 최초 `S_EnterMap→S_PlayerHp→initial roster`, 이동 `S_MapTransition→S_PlayerHp→initial roster`, 다른 플레이어의 join/leave 의미를 유지한다. Party→Quest 처리 순서도 유지한다.

제외: 클라이언트 구현, Shared/PDL/DLL·Unity 에셋 변경, DB 설계/접속, 전투·퀘스트 정책 변경, 전체 singleton 제거, GitHub 인증·보호 설정 변경. 추가 파일이 필요하면 메인이 정확한 범위와 쓰기 담당을 먼저 정한다.

## 작업 분할과 쓰기 소유권

| 담당 | 허용 파일·작업 | 금지·이전 경계 |
|---|---|---|
| 메인 | 본 goal, M0/M1 상태·분할, CURRENT, AGENTS/목표 루프의 사용자 테스트 지시, Git/PR | 구현 전체를 대신하지 않음; 각 merge 별도 승인 |
| `refactor_scope_review` 구현자 | `02_Server/GameServer/{Program.cs,Loop/GameWorld.cs,Loop/TickScheduler.cs,Sessions/GameSession.cs,Maps/Transitions/MapMigration.cs,Party/PartyFlow.cs}`, `02_Server/Network/Listener.cs`, 신규 `02_Server/GameServer/Hosting/ServerHost.cs` | 신규 회귀 테스트는 독립 검증자 소유. 기존 테스트 호환 변경 필요 시 먼저 목록 보고 |
| `db_scope_review` 독립 검증자 | 구현자 쓰기 종료 후 관련 server test 파일 작성·보완·실행 및 결과 기록 | 요구사항·보존 계약 기준으로 테스트. 구현 결함은 구현자에게 반환 |
| `client_flow_audit` 독립 리뷰 지원 | 관련 diff와 wire 계약 읽기·리뷰 | 서버 구현/테스트 쓰기 없음 |

사용자 추가 지시(2026-09-29): 구현 이후 다른 서브에이전트가 **테스트 코드로 검증**한다. 기존 테스트 통과나 diff 리뷰만으로 대신하지 않는다. 테스트와 구현 수정의 파일 소유권을 분리하고 실패 수정 후 재검증한다.

## 완료조건·검증

- [x] 구현 전에 선택 설계·보존 계약·파일 소유권을 문서화했다.
- [x] 월드 준비 전 접속과 정지 후 accept를 차단하며 시작 실패·중복 종료를 처리한다.
- [x] handshake/직업 선택/중복 진입·종료의 기존 동작을 보존한다.
- [x] 진입 및 이동 단계별 종료에서 0/1 등록과 cleanup 이후 0, 낡은 작업의 무효화를 독립 테스트 코드로 확인한다.
- [x] 실제 listener/host 접속과 종료·scheduler 종료 실패의 관찰 가능한 결과를 독립 테스트로 확인한다.
- [x] 영향받는 서버 suite와 production MapTransition smoke를 실행한다. 기존 M0의 skip·fixture 한계와 구분한다.
- [x] 독립 코드 리뷰·테스트 결과·미실행 범위를 기록하고 PR을 준비한다. 병합 승인은 별도 기록한다.

실행 전 [DEVELOPMENT](../../../00_Document/operations/DEVELOPMENT.md)의 WSL lock·포트·DLL 복사 부작용을 확인한다. 원시 근거는 `.backups/verification/2026-09-29-server-lifecycle/`에 둔다. 서버만 변경하므로 Unity 편집·실제 플레이 성공을 이 PR의 서버 결과로 주장하지 않는다. 클라이언트 변경과 통합 플레이는 후속 M1 목표에서 검증한다.

## 실제 결과와 다음 인계

설계 원문: 로컬 `.backups/reviews/2026-09-29-m1-server-design.md`, 독립 검증 계획 `2026-09-29-m1-verification-plan.md`. 선택한 계약은 본 문서가 기준이다.

### 변경과 독립 검증

구현자는 위 8개 소스 파일을 변경했다. GameSession은 생성 당시 월드에 결합하고, 월드가 종료 요청을 틱에서 처리한다. Host가 준비·accept·연결 해제·cleanup barrier·틱 종료를 조정한다. 프로토콜·Shared·Unity·DB 파일은 변경하지 않았다.

독립 검증자는 테스트 6파일을 작성·보완했다. 신규 `Maps/SessionCleanupTests.cs` 15사례, 신규 `Network/ServerHostLifecycleTests.cs` 9사례, `TickSchedulerTests.cs` 신규 3사례로 총 27사례를 추가했다. 기존 `Network/GameSessionLifecycleTests.cs`, `Network/BroadcastTests.cs`, `Party/PartyRejectionTests.cs`는 월드 종료 큐를 처리하도록 준비 코드를 이행했으며 기존 시나리오를 삭제하거나 기대값을 완화하지 않았다. 공격·스킬은 살아 있는 세션과 닫힌 세션을 대조하고, Town/목적지에 실제 observer를 둬 HP 뒤 roster 순서를 확인한다.

독립 코드 리뷰에서 accept fault가 Host의 나머지 정리를 중단시키는 경로 1개를 찾아 구현자에게 반환했다. 수정 후 재리뷰 지적 0이며 socket dispose를 통한 terminal fault 주입 테스트도 통과했다. 테스트 리뷰의 coverage 공백 2개(종료 전 queued attack/skill, nonempty roster 순서)는 보완 후 재리뷰에서 해소됐다. 독립 문서 리뷰의 상태 불일치 2개도 해소했고 상대 링크 30개를 확인했다. 근거는 `review/{docs,code,test-review}.md`다.

### 실제 실행

원시 근거의 공통 루트는 `.backups/verification/2026-09-29-server-lifecycle/`다. 실행 환경은 WSL Ubuntu, .NET SDK 10.0.300, Debug다. 작업자의 WSL 실행은 이전 제한 권한의 `E_ACCESSDENIED`로 차단됐다. 사용자에게 알리고 독립 검증자가 테스트 코드·명령·판정을 소유한 채 메인이 명령 실행만 대행했다. 전역 권한 설정은 변경하지 않았다.

| 검증 | 관찰 결과 | 근거 |
|---|---|---|
| 초기 구현 빌드 | exit 0, 오류 0·경고 15. 이후 추가 경고 원인 수정 | `implementation/main-build.log` |
| 전체 서버 suite | 2026-09-29 14:15:35–14:17:44 UTC. 736개 중 통과 731·실패 0·기존 skip 5, exit 0 | `verification/suite-1.log`, `suite-1-run.json`, `results/*.trx` |
| 보완 후 대상 suite | 14:20:34–14:20:51 UTC. 30/30 통과, 실패·skip 0. 신규 27+기존 scheduler 3. 오류 0·기존 경고 6, 추가 xUnit1031 해소 | `verification/targeted.log`, `targeted-run.json`, `targeted-results/*.trx` |
| production MapTransition | 14:21:12–14:21:38 UTC. PASS 1/FAIL 0, HG/Boss/Ending/Town 도달, entity 7 유지·도착 좌표 일치 | `verification/production-smoke.log`, `production-smoke-run.json`, `MapTransition-{server,bot}.log` |
| 운영 문서/skill | diff --check 및 skill quick_validate 통과 | 독립 문서 리뷰와 메인 실행 |
| 최종 전체 CI | 최종 테스트 집계는 740개다. 최신 head 실행 결과는 PR #131의 CI가 기준이며 로컬 740개 실행으로 보고하지 않는다 | PR 검사·로컬 `pr-checks.json` |

서버 명령은 `wsl -d Ubuntu --exec bash 99_Tools/sync-wsl.sh test --logger trx --results-directory <전용 경로>`다. 보완 검증은 `--filter 'FullyQualifiedName~SessionCleanupTests|FullyQualifiedName~ServerHostLifecycleTests|FullyQualifiedName~TickSchedulerTests'`를 추가했다. 봇 명령은 같은 helper의 `bot MapTransition`이다. 보완 뒤 로컬 전체 suite를 다시 실행했다고 주장하지 않으며 최종 PR CI와 구분한다. 실행 후 WSL 7777 listener 및 이 작업의 GameServer/testhost/HeadlessBot/helper 실행이 없음을 확인했다.

### 한계와 후속

- 기존 skip 5개는 장시간 반복/lag/boss 시나리오이며 이번 변경의 통과로 대체하지 않는다. 정확한 목록은 TRX에 있다.
- 이동 종료는 단계별 상태·큐 순서와 목적지 등록 중 transient routing 상태를 제어해 검증했다. 기존 경쟁 창을 실제 OS 스케줄링에서 재현했다는 뜻은 아니다. 일시적 OS accept 오류의 자연 발생·재시도도 실행 재현하지 않았다.
- production 봇은 Debug 해금 명령을 사용하며 실제 처치 목표·보스 전투 검증이 아니다. helper의 프로세스 종료는 Program의 Enter 입력에 의한 정상 종료 검증이 아니다. 정상 Host 종료·timeout/재시도는 별도 loopback 테스트로 검증했다.
- Unity 실제 플레이·DB 검증은 미실행이며 클라이언트 수명/씬 진입 완료로 표시하지 않는다.
- 독립 검증자가 TRX와 봇 원문을 대조한 최종 판정은 로컬 `verification/summary.md`에 있다. 구현자·테스트 작성자·리뷰어는 쓰기를 종료했다. 메인은 문서·Git·PR 통합만 소유한다. PR #131을 생성했으며 병합은 미승인이다. 명시 승인을 받기 전 다음 목표의 구현 브랜치로 진행하지 않는다.

이 PR 통합 후 최신 main에서 [M1b](../2026-09-29-refactor-client-connection/goal.md)를 시작한다. 클라이언트 설계에서 서버 wire 순서 보존을 확인했고 서버 변경을 클라이언트 연결/씬 진입 완료로 취급하지 않는다.
