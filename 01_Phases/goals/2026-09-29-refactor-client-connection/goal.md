# M1b — 클라이언트 연결 수명과 지연 callback

상태: 구현·독립 TestCode·코드 리뷰 완료, PR/최신 CI 준비. base main `956a3918b0c63a025c61e88a711ac51c119e0e8d`, branch `feat/refactor-client-connection`. [M1a](../2026-09-29-refactor-server-lifecycle/goal.md) 통합 후 시작한 [M1](../2026-09-29-refactor-lifecycle/goal.md)의 두 번째 목표다. 이번 로드맵은 사용자가 명시한 [AGENTS](../../../AGENTS.md)의 조건부 병합 예외를 따른다. 메인이 독립 테스트·리뷰·최신 head CI 성공과 크리티컬 이슈 부재를 확인해 직접 병합한다. 자동 병합 예약은 하지 않는다.

## 목표와 선택 설계

연결 시도·실패·취소·자연 종료를 한 소유자가 관리하고, 이전 연결의 완료/패킷/지연 입력이 새 연결을 건드리지 못하게 한다. 선택은 **공용 ClientNet의 취소 가능한 attempt + Unity main-thread 연결 owner**다. Unity 전용 socket adapter는 같은 경합·자원 정리를 두 군데 구현하게 하므로 채택하지 않는다. 비용은 ClientNet DLL 교체와 양쪽 사용처 검증이며 이를 이번 범위에 명시 포함한다.

| 책임 | 소유자 | 계약 |
|---|---|---|
| 연결 중·성공 후 미인수 socket | 공용 connection attempt | 완료는 한 번, 인수 전 Cancel/Dispose가 자원 정리 |
| 연결 상태·generation·시도/세션 선택 | Unity의 일반 C# ClientConnectionLifetime | main-thread writer 하나, Disconnected/Connecting/Connected |
| Inspector/PlayerPrefs/Update/Unity 생명주기 | NetworkService facade | 검증한 endpoint/class를 owner에 전달하고 상태를 이중 저장하지 않음 |
| framing·송수신·transport 종료 | ClientSession/UnityClientSession | terminal 오류를 알리고 실제 socket 정리; Unity 상태는 main queue로 전달 |
| queued packet·roster·지연 intent 적용 | 해당 session과 generation | 실행 직전 현재 세션·closed 여부 확인 |

### 공용 API와 자원 소유

`Connector.BeginConnect(endpoint)`는 독립 attempt를 반환한다. attempt는 `Completion`(Success/Failed/Canceled 및 오류 정보), `TryTakeConnectedSocket`, `Cancel`, `Dispose` 경계를 제공한다. 완료 결과와 socket 소유권을 분리한다. OS 성공이 이미 알려졌더라도 main queue 대기 중 취소되면 인수는 실패하고 socket을 닫는다. 인수 성공 뒤에는 ClientSession이 socket을 소유하며 attempt Dispose로 닫지 않는다. pending SocketAsyncEventArgs는 callback 사용이 끝나기 전에 해제하지 않는다. 사용자 callback은 lock 안에서 실행하지 않는다.

기존 `Connect(endpoint, sessionFactory, count)`는 같은 엔진의 호환 wrapper로 유지한다. factory는 호출별 지역 값으로 보존하고 factory→Start→OnConnected 순서를 유지한다. 자동 재시도·새 timeout 정책은 추가하지 않는다. netstandard2.1 호환 API만 사용한다.

`ClientSession.cs`는 수신/송신 시작 또는 완료의 terminal 예외가 로그만 남기고 연결을 살아 있다고 오인하는 경로를 좁게 보완한다. 중복 disconnect 방지, 종료 알림과 socket/queue 정리를 보장하고 초기화 실패·이미 닫힌 socket에서도 정리를 생략하지 않는다. framing·프로토콜·송수신 알고리즘 전면 재작성은 제외한다.

### Unity 연결과 callback 계약

- Connect 때 endpoint와 선택 class를 검증·고정한다. Connecting/Connected에서 중복 Connect는 no-op이다. Connected는 TCP/session 활성 상태이며 씬 진입 완료를 뜻하지 않는다.
- Cancel/Disconnect/Destroy/Quit는 먼저 generation과 활성 참조를 무효화한 후 attempt/session을 종료한다. 중복 종료는 멱등이다. 연결 거절·handshake 거절·자연 종료 뒤 다음 명시 Connect가 가능하다. 자동 재접속·새 실패 화면 정책은 추가하지 않는다.
- 성공은 main에서 generation/attempt/owner 생존을 확인하고 socket을 한 번 인수한다. session 생성·구독·Start·최초 handshake 송신을 마친 뒤 Instance를 공개한다. Start 도중 즉시 종료된 세션을 다시 Connected로 덮어쓰지 않는다.
- handshake 성공 뒤 시도 시작 때 고정한 class를 한 번 보낸다. 기존 first-packet 순서, ping 주기, handshake 전 intent drop을 유지한다.
- 자연 종료는 atomic closed를 즉시 세우고 별도 필수 종료 알림을 main으로 보낸다. 일반 packet 적용 gate 때문에 필수 종료 정리가 버려지면 안 된다. 이전 세션은 자기 자원만 정리하고 새 세션의 전역 상태를 지우지 않는다.
- 22 handler는 게임 처리를 보존하면서 `EnqueueApply`로 적용 경계만 옮긴다. roster의 sceneLoaded drain과 Editor 지연 intent도 실행 직전 검사한다. 전역 dispatcher 큐 전체를 지우지 않는다.
- 현재 세션 종료 시 Instance/HandshakeOk/entity ID/server tick/pending spawn/roster/ping 누적과 remote·enemy registry, Party pending/error/members 및 Quest 진행 mirror를 초기화한다. 이벤트는 초기화 완료 상태를 관찰한다. ClassLoadout·PlayerPrefs·개발 latency 설정은 보존한다.
- sceneReady/전환 epoch·BGM·StageClear 표시·이미 시작된 scene load의 취소/완료는 M1c의 책임이다. 이번에 씬 로딩 취소까지 해결했다고 주장하지 않는다.

## 범위와 쓰기 소유권

| 담당 | 허용 파일·작업 |
|---|---|
| 메인 | goal/CURRENT·기존 목표 병합 기록·AGENTS/skill 예외·Git 통합; 검증된 ClientNet DLL 선택 복사 |
| client_flow_audit 구현자 | `04_ClientNet/{Connector.cs,ClientSession.cs,ConnectionAttempt.cs(new)}`; Unity `Assets/Scripts/Network/{NetworkService.cs,UnityClientSession.cs,RosterTransitionBuffer.cs,ClientConnectionLifetime.cs(new),ClientConnectionLifetime.cs.meta(new)}`; `Assets/Scripts/State/{PartyState.cs,QuestState.cs}` |
| 같은 구현자 | `Network/Handlers/IClientPacketHandler.cs` 및 Session/{HandshakeResult,EnterMap,Pong}, Zone/{MapTransition,PortalLocked}, Sync/{Snapshot,EntityState}, Roster/{PlayerJoin,PlayerLeave,EntitySpawn}, Party/{PartyUpdate,PartyInviteRecv,PartyError}, Quest/QuestUpdate, Skill/{SkillCast,ProjectileLaunch}, Combat/{EnemyAttack,EntityDeath,HitResult,PlayerAttack,StageClear,PlayerHp}의 각 Handler.cs 적용 gate |
| db_scope_review 독립 검증자 | 구현자 쓰기 종료 후 `02_Server/GameServer.Tests/Network/ClientConnectorLifecycleTests.cs(new)`; Unity `Assets/Tests/EditMode/{ClientConnectionLifetimeTests.cs,ClientSessionGenerationTests.cs,ClientSessionMirrorResetTests.cs}`와 새 .meta; `Dawnholder.Client.Tests.EditMode.asmdef`에 ClientNet 참조 추가 |
| refactor_scope_review | HTML 보고서 단일 writer, 구현 종료 후 독립 코드 리뷰(읽기만) |

독립 리뷰에서 같은 로컬 플레이어를 유지한 재연결에 이전 Teleport 도착 callback/snap 예약이 남는 경계를 확인하여, 구현자에게 `Prediction/LocalPlayerMovement.cs`의 연결 종료 시 teleport 예약·callback·출발 위치 stash를 비우는 좁은 메서드와 NetworkService 호출을 추가 배정했다. 전체 예측·씬 정책은 M1c로 남긴다. Party/Quest는 값 초기화와 통보를 분리해 두 mirror 초기화 완료 후 이벤트를 발행한다. 독립 검증자는 기존 배정 테스트에서 이 두 계약도 확인한다. 정확한 변경 범위가 늘어나면 메인이 이유와 소유권을 먼저 기록한다. ClientNet csproj 변경은 현재 허용하지 않는다. WSL build의 post-build는 Linux 복사본만 갱신하며, main이 검증 산출 `Dawnholder.Client.Net.dll` 하나를 원본 Unity plugin으로 복사하고 SHA256 일치를 기록한다. 기존 DLL .meta/GUID, Shared.dll, PDL/생성 패킷, 서버·봇 production, 기존 씬·프리팹·Inspector 값은 보존한다. 새 Unity .cs에는 새 GUID의 .meta만 추가한다.

DB 설계/구현·게임 정책 변경·전체 DI framework/이벤트 버스는 제외한다. UI popup의 추가 수정이 필요하면 보고 후 소유 범위를 정한다.

## 검증과 완료조건

구현 후 다른 서브에이전트가 요구사항 기반 테스트 코드를 작성·실행한다. 테스트는 fake attempt/수동 main queue/activation factory/mirror reset 경계를 사용해 결정적 순서와 외부 결과를 확인하며, 실제 Connector는 loopback 임의 포트로 별도 검증한다.

- [x] 선택 설계·API·보존 계약·파일 소유권을 구현 전에 확정했다.
- [x] 중복 시작, 실패 뒤 재시도, 취소 후 늦은 성공, 성공했지만 미인수인 socket 취소를 검증한다.
- [x] Start 중 종료, 자연 종료·handshake 거절, 이전 disconnect/packet/지연 intent/roster가 새 세션을 건드리지 않음을 정상 동작 대조와 함께 검증한다.
- [x] class override 캡처·중복 handshake의 1회 전송·Destroy/Quit·mirror reset을 검증한다.
- [x] 공용 Connector의 실제 성공/거절/취소·legacy wrapper 호환·terminal 자원 정리, 영향받은 서버 suite 및 bot handshake/왕복을 확인한다.
- [x] ClientNet 소스 산출물과 Unity plugin hash 일치 및 Unity compile/EditMode를 확인한다. 실제 플레이·화면 관찰과 구분한다.
- [ ] 독립 코드 리뷰·TestCode 결과·최신 CI 통과, 크리티컬 이슈 없음 확인 후 PR을 직접 병합하고 결과를 기록한다.

실행 전 [DEVELOPMENT](../../../00_Document/operations/DEVELOPMENT.md)의 DLL 복사·WSL/포트 lock·Unity 프로세스를 확인한다. 원시 근거는 `.backups/verification/2026-09-29-client-connection/`에 둔다. 작업자 WSL 권한 제한이 지속되면 main은 명령만 기계 대행하고 독립 검증자가 테스트·판정을 소유한다. 전역 권한 변경은 하지 않는다.

## 실제 결과·인계

구현자는 production 34파일(새 C# 2개·meta 1개 포함)의 쓰기를 종료했고, 메인이 검증된 ClientNet DLL 하나를 원본 Unity plugin에 반영했다. 별도 검증자가 서버 테스트 11건과 Unity 테스트 32건을 작성했다. 별도 리뷰어는 production과 TestCode를 검토했다. 각자의 변경 범위는 위 소유권 표와 추가 LocalPlayerMovement 보완에 한정한다.

| 검증 | 실제 결과 | 원시 근거 (`.backups/verification/2026-09-29-client-connection/`) |
|---|---|---|
| WSL 빌드 | 오류 0, 기존 경고 6. 신규 선언 순서 경고 보완 완료 | `verification/main-source-build-final.log` |
| 서버 전체 | 751건: 통과 746, 실패 0, 기존 제외 5 | `verification/server-suite.log`, `server-results/*.trx` |
| 최종 Connector 재검증 | 11/11 통과. 동기 실패 직후 자동 정리 assertion이 명시 Disconnect보다 먼저 수행되도록 테스트 보완 | `verification/server-targeted.log`, `server-targeted-results/*.trx` |
| Unity 6000.4.7f1 | 컴파일 오류 0, EditMode 179/179 통과: 기존 147 모두 유지, 신규 owner 15·generation 10·mirror 7 | `verification/unity-retry-1/{editor.log,editmode-results.xml,exit.json}` |
| 실제 서버–봇 MapTransition | PASS 1 / FAIL 0. entity 7로 HG/Boss/Ending/Town, entityId·spawn 보존 | `verification/production-smoke.log`, `MapTransition-{server,bot}.log` |
| DLL | source/Unity SHA256 `CA54059FBE3DB4B029583372213DD5908D627E69EFC5923A7E391BBD5D38E8E8` 일치, Shared 및 기존 DLL meta 불변 | `verification/dll-copy.json` |
| 독립 코드·테스트 리뷰 | 로컬 teleport 예약 잔류와 cross-mirror 알림 순서 보완 후 잔여 지적 없음. 테스트의 자동 정리 false positive도 보완·재검증 | `review/code.md`, `review/test-review.md` |

작업자 WSL은 E_ACCESSDENIED, 최초 Unity는 라이선스 IPC에서 차단되어 테스트가 시작되지 않았다. 메인은 해당 검증용 Unity PID 33828만 정확한 executable/project/log로 확인·종료하고 동일 독립 테스트를 기계 대행 실행했다. 최종 Unity PID 12440은 정상 종료했다. 검증자는 원시 TRX/XML·로그를 독립 대조하여 판정했다. 전역 권한·Hub·라이선스 설정은 변경하지 않았다. 최초 환경 차단을 테스트 실패나 성공으로 합산하지 않는다.

Unity 실제 화면·물리 입력·오디오 플레이 확인은 이번에 수행하지 않았다. 봇은 Debug 해금 경로로 이동 계약을 확인하며 실제 퀘스트·보스 전투 완료를 뜻하지 않는다. 이미 시작된 scene load, sceneReady/epoch, BGM/StageClear는 M1c 범위다. DB 저장·복원은 구현하지 않았다.

[HTML 보고서](../../reports/2026-09-29-refactor-before-persistence/report.html)는 시점별 결과·설계 선택·핵심 실제 Diff 산출물이다. 데스크톱 및 320/430/768/1440 viewport overflow 부재, 테마 전환·Diff 접기·인쇄 확장/복원을 확인했다(`.backups/report-preview/qa-results.json`). 기준 상태는 본 goal이며 HTML은 실행 증거를 대체하지 않는다.

PR/최신 head CI는 아직 준비 중이다. 사용자 조건부 병합 예외에 따라 독립 결과와 최신 CI를 대조하고 크리티컬 이슈가 없을 때 직접 병합한다. 이후 최신 main에서 [M1c](../2026-09-29-refactor-client-entry/goal.md)를 시작한다. 읽기 전용 사전 설계는 `.backups/reviews/2026-09-30-m1c-design-preflight.md`이며 연결 generation·취소·mirror reset과 미실행 플레이 경계를 인계한다.