# M1c — 클라이언트 씬 진입과 맵 전환

상태: 완료·PR #133 병합. base main `f0e76af6f322baebfab8c814ddcf5d3365856174`, branch `feat/refactor-client-entry`. [M1b](../2026-09-29-refactor-client-connection/goal.md)의 PR #132를 독립 TestCode·리뷰·최신 CI 통과 후 조건부 승인으로 병합하고 시작했다. [M1](../2026-09-29-refactor-lifecycle/goal.md)의 마지막 목표이며 병합은 [AGENTS](../../../AGENTS.md)의 이번 로드맵 한정 예외를 따른다.

## 목표와 선택 설계

씬 준비, 서버 spawn/HP/roster 적용, 입력 재개의 소유자를 하나로 모으고 겹친 전환이나 연결 종료 뒤 늦은 씬 완료가 새 진입 상태를 덮지 못하게 한다. 선택은 **세션 수명의 순수 C# MapEntryCoordinator + 앱 수명의 SceneLoadQueue + Unity SceneTransition adapter**다. session generation, map entry epoch, 물리 load request ID는 수명이 달라 구분한다. bool 하나나 sceneLoaded 구독 순서로 준비를 추측하지 않는다. 범용 이벤트 버스/DI framework는 도입하지 않는다.

| 책임 | 소유자 | 계약 |
|---|---|---|
| 목적 map·epoch·권위 초기값·Ready | MapEntryCoordinator | Waiting/Entering/Ready/Failed/Closed, 세션 종료 시 무효화 |
| active 1개·최신 pending 1개와 결과 | SceneLoadQueue | 요청 ID별 성공/실패/대체/취소, 중복 완료 무시 |
| fade·Unity scene load·실제 scene binding | SceneTransition adapter | 이미 시작된 LoadSceneAsync를 취소/rollback했다고 가정하지 않음 |
| player/terrain/registry/HUD 연결 | 기존 Bootstrap와 view | 실제 준비 이후 현재 scene handle/request에 연결 |
| 입력·예측·지연 intent | LocalPlayerMovement/Input 및 UnityClientSession | 현재 entry가 Ready인 경우에만 생성·commit·송신 |

기존 SceneTransition.LoadScene(string)은 UI 호출 호환 facade로 유지한다. core queue는 Request/TryStartNext/Cancel/Complete 등 좁은 seam으로 시험할 수 있게 하고 map/session 게임 정책을 알지 못한다. A 물리 load 중 B→C 요청은 A 완료 후 C만 시작한다. A/B의 도메인 적용은 거부한다. fade 전 아직 시작하지 않은 요청은 대체할 수 있다. 같은 scene 이름도 요청 ID/epoch가 다르면 이전 완료로 처리하지 않는다.

메뉴 요청은 PauseMenu/Ending의 명시 navigation 경계에서 즉시 연결을 무효화하고 scene 요청을 보낸다. MainMenu.Awake까지 기다리거나 SceneTransition이 특정 문자열을 보고 networking을 임의 조작하지 않는다. 사용자 설정/클래스 선택과 pause의 timeScale 복원은 보존한다.

## 진입·상태·입력 계약

- 최초 EnterMap은 현행 wire대로 Town(map 0)이다. packet-before-player와 player-before-packet 모두 같은 owner의 bind 경로로 처리한다. 기존 정적 PendingSpawn의 원본 상태는 entry record로 옮기며 필요한 호환 facade는 owner 값에 위임한다.
- gameplay Ready는 현재 목적 scene/operation의 player·terrain·registry가 준비되고 권위 spawn 및 최초 HP를 적용한 뒤 성립한다. 서버의 EnterMap/MapTransition→PlayerHp 순서는 기존 생산 코드/통합 테스트로 확인한다. Ending은 플레이어/HP/terrain이 없는 결과 씬이므로 scene 준비만 확인하고 gameplay 입력은 계속 비활성이다.
- UI는 SceneBootstrap에서 additive 비동기로 로드되므로 gameplay Ready의 필수 전제에서 제외한다. HUD가 준비되면 최신 권위 HP를 late bind한다. HudController.Start의 임시 full HP가 이미 적용한 서버값을 덮지 않게 한다. UI load 실패는 기록하되 gameplay 입력을 영구 대기시키지 않는다.
- 맵 표시명은 목적 scene binding에서 적용한다. BGM·PortalEnter SFX는 유효한 서버 entry 요청을 수락할 때 기존 시점대로 한 번 재생한다. 중복 완료로 반복하지 않는다. 기존 scene/display/audio key, spawn·HP·entity ID 및 게임 규칙을 유지한다.
- EnterMap/MapTransition의 main queue 실행 순서를 epoch 경계로 삼는다. 수신 thread에서 아직 실행 전인 main epoch를 읽어 이후 패킷을 잘못 tagging하지 않는다. wire에 없는 map epoch로 서버가 잘못 보낸 오래된 map packet까지 구별할 수 있다고 주장하지 않는다.
- roster는 기존 capacity 100·overflow drop 정책과 생성/변경/삭제 순서를 보존하고 owner가 준비 뒤 한 번 drain한다. 새 entry는 이전 buffer를 폐기한다. 본인 snapshot은 올바른 player binding/초기 spawn 이후 적용한다. Enemy EntityState 등 우회 경로도 같은 준비 경계를 따른다.
- 이동 Update의 predict/history/NotifySent와 attack/skill의 PredictCommit 전에 Ready를 확인한다. 준비 전 누적된 accumulator/jump/impulse를 새 entry 입력으로 터뜨리지 않는다. Editor 지연 intent는 enqueue 시점 entry epoch와 실제 송신 시 epoch가 같아야 한다.
- 같은 세션의 이전 맵에서 등록한 teleport snap/arrive callback/depart stash는 새 entry 경계에서 무효화한다. 실제 callback에도 현재 epoch를 확인한다. 원격 registry의 callback/객체 수명도 같은 경계에 포함한다.
- StageClear는 현재 즉시 Show하고 씬 소속 UI가 fade한다. 존재하지 않는 기존 pending 상태를 가정하거나 결과 유지 정책을 새로 만들지 않는다. 새 준비 buffer가 필요하면 그 buffer에 epoch를 붙이고 기존 표시/SFX를 보존한다.
- 연결 종료·메뉴 이동은 entry와 pending 요청을 먼저 무효화한다. 뒤늦게 실제 load가 완료되어도 spawn/HP/roster/입력/후속 effect를 적용하지 않는다. Unity 물리 load 자체의 rollback 보장은 하지 않는다.
- 잘못된 map ID는 부작용 전 거절한다. scene 시작 예외/null operation은 Failed로 종료하고 fade/raycast/busy를 복원하며 입력을 잠근 채 연결을 종료하고 오류를 기록한다. 자동 재접속·자동 메뉴 복귀·새 timeout 정책은 추가하지 않는다.

## 범위·쓰기 소유권

| 담당 | 허용 파일 |
|---|---|
| 메인 | goal/CURRENT·이전 목표 통합 기록·PR/Git 통합, 실행 부작용 조율 |
| client_flow_audit 구현자 | 새 `03_Client/Assets/Scripts/Network/MapEntryCoordinator.cs`, `UI/SceneLoadQueue.cs` 및 각 새 .meta |
| 같은 구현자 | 기존 `Network/{UnityClientSession,NetworkService,RosterTransitionBuffer,SceneRouter}.cs`, `UI/{SceneTransition,HudController,MapNameDisplay,PauseMenuController}.cs`, `Scenes/EndingController.cs` |
| 같은 구현자 | `Bootstrap/{LocalPlayerSpawner,SceneBootstrap}.cs`, `Combat/CombatBootstrap.cs`, `Prediction/LocalPlayerMovement.cs`, `Input/LocalPlayerInput.cs`, `State/RemoteEntityRegistry.cs` |
| 같은 구현자 | `Network/Handlers/Session/EnterMapHandler.cs`, `Zone/MapTransitionHandler.cs`, `Combat/{PlayerHp,StageClear}Handler.cs`, `Sync/{Snapshot,EntityState}Handler.cs`, `Roster/{PlayerJoin,PlayerLeave,EntitySpawn}Handler.cs`, `Skill/SkillCastHandler.cs` |
| db_scope_review 독립 검증자 | 구현 쓰기 종료 후 새 EditMode `MapEntryCoordinatorTests.cs`, `SceneTransitionRequestTests.cs`, `MapEntryBindingTests.cs`와 새 .meta; 기존 `HudAndMapTests.cs` 및 M1b의 entry 소비 관련 기존 세 테스트 파일의 필요한 적응 |
| 같은 검증자 | 구현 쓰기 종료 후 새 `Assets/Tests/PlayMode/{MapEntrySceneLifecycleTests.cs,MapEntryServerIntegrationTests.cs,MapEntryPlayFixture.cs,Dawnholder.Client.Tests.PlayMode.asmdef}`와 각 새 .meta 및 PlayMode.meta(9파일). scripted 임의 포트 lane과 실제 GameServer 7777 lane을 분리하며 원본 씬은 재저장하지 않는다 |
| refactor_scope_review | 독립 production/TestCode 리뷰, HTML 보고서 단일 writer |

구현 중 spawn→death 순서 역전과 준비 전 VFX/HP 적용 경로를 확인해, 구현자에게 `Combat/{EntityDeath,HitResult,EnemyAttack,PlayerAttack}Handler.cs` 및 `Skill/ProjectileLaunchHandler.cs` 5파일의 적용 gate만 추가 배정했다. parsing/게임 본문은 보존하며 동일 100-cap FIFO에 들어오는 event 종류의 확대를 독립 테스트에 명시한다. StageClear는 기존 즉시 Show를 유지하고 새 예약 정책을 만들지 않는다. 추가 combat/skill handler, 프로젝트 assembly/API, bootstrap 접점이 필요하면 메인이 정확 경로와 이유를 추가 배정한다. 동시 파일 쓰기는 금지한다. StageClearUI 자체, Shared/PDL/ClientNet DLL·서버/봇 production, 기존 씬·프리팹·GUID·직렬화 값은 현재 쓰기 범위에서 제외한다. 원본 에셋 재저장에 의존하지 않는다.

### 단계적 인계

구현자가 MapEntryCoordinator.cs와 SceneLoadQueue.cs 및 각각 meta의 구현·쓰기 종료를 선언하여, 이 두 독립 작업 단위를 먼저 TestCode/코드 리뷰에 넘겼다. 검증자는 MapEntryCoordinatorTests.cs·SceneTransitionRequestTests.cs와 새 meta만 먼저 작성한다. 나머지 Unity adapter·binding production은 구현자가 계속 쓰며 통합 테스트 작성/Unity 실행은 해당 쓰기 종료 후 수행한다. core 재수정 필요 시 메인이 소유권을 조율한다. 이 분할은 작성 중 구현을 통과로 간주하는 것이 아니다.

## 독립 검증·완료조건

- [x] 설계·보존 정책·파일 소유권을 구현 전에 확정했다.
- [x] 순수 owner/queue TestCode: 최초 진입 두 순서, A 중 B→C, 같은 씬 두 epoch, 중복/대체/취소 완료, null/throw 실패, 연결 종료 뒤 ready 통보, Ending player 없음.
- [x] binding TestCode: HP가 HUD.Start 앞/뒤, spawn/HP/roster 순서·1회 drain·overflow, local/remote snapshot, map epoch를 넘는 intent/effect 차단과 정상 양성대조.
- [x] 입력 테스트: Ready 전 predict/history/attack·skill commit 차단, 이후 정상 재개, 준비 동안 누적 입력이 폭주하지 않음.
- [x] Unity compile/EditMode 및 실제 SceneManager/PlayMode 계약 검증. 실제 GameServer↔Unity Town↔플레이맵 왕복 결과와 scripted loopback 검증을 구분한다.
- [x] 실제 화면·fade·물리 입력·오디오 관찰의 수행/미수행을 별도로 기록한다. 자동 PlayMode 통과로 수동 플레이·청취 완료를 주장하지 않는다.
- [x] 서버 suite/production bot 영향 확인과 기존 M1b 테스트 회귀를 완료하고 에셋·GUID·Shared/ClientNet DLL 보존을 확인한다.
- [x] 독립 코드·TestCode 리뷰, 최신 PR head CI 통과 및 크리티컬 이슈 부재를 확인해 조건부 승인에 따라 직접 병합한다.

실행 전 [DEVELOPMENT](../../../00_Document/operations/DEVELOPMENT.md)의 포트·Unity editor·lock·DLL 부작용을 확인한다. 원시 결과는 `.backups/verification/2026-09-30-client-entry/`에 둔다. WSL/Unity IPC 제한은 전역 권한 변경 없이 메인 기계 대행과 독립 판정으로 구분한다. 같은 포트/에디터를 동시에 실행하지 않는다.

## 실제 결과·인계

설계 근거는 `.backups/reviews/2026-09-30-m1c-design-preflight.md`, 원시 검증 루트는 `.backups/verification/2026-09-30-client-entry/`다. production 32파일(기존 28, 새 C# 2·meta 2)을 변경했다. 구현자와 독립 TestCode 작성자·리뷰어를 분리했다.

- callback 재진입 시 최신 요청 오종료·새 epoch Ready 누락·observer 예외로 다음 통보 누락을 수정했다. Unity 6000.4.7f1 SceneHandle.GetRawData() 정식 API로 64비트 identity를 보존하고 상위 비트 차이를 독립 테스트했다. 최종 source/TestCode 리뷰의 미해결 P0–P2는 없다(`review/{core,core-tests,code,test-review}.md`). 옛 terrain 주석 P3은 M3로 인계했다.
- 최종 EditMode `unity/editmode-3`: native exit 0, **240/240 통과**, 실패/제외 0. 이전 M1b 179사례 누락 0. 초기 238 통과 뒤 PlayMode fixture assembly 참조 컴파일 오류를 수정했고 64비트 두 사례를 추가한 결과다.
- 최종 scripted TCP+실제 GPU/SceneManager PlayMode `unity/playscenes-4`: native exit 0, **10/10 통과**. 진입 두 순서·HP/HUD late bind·roster 순서/remote 좌표·준비 전후 입력·맵 왕복·Ending·실패/취소·동일 씬 epoch·실제 A load 중 B→C에서 B 미로드·stale teleport/정상 양성 경로를 검증했다. 가상 InputSystem 키보드를 사용했으며 원본 InputSettings를 바꾸지 않고 런타임 clone으로 batch focus 전제를 설정·복원했다. stale teleport callback 지연은 실제 등록된 delegate를 다시 호출해 재현했다.
- 실제 production GameServer 통합 `unity/playserver-1`: native exit 0, **1/1 통과**. 실제 포털 C_EnterPortal 송신으로 Town→HuntingGround→Town, entity ID/HP/roster 보존을 확인했다. 서버 build exit 0, 기존 warning 6/error 0. 원문 `server/server.log`, exact 소유 서버 PID 873은 stdin newline으로 정상 종료(exit 0), port 7777 listener 부재 확인. DB 연동 검증은 아니다.
- 첫 PlayMode의 -nographics는 RenderTexture/URP native crash(exit -1073741819, XML 없음)를 일으켰다. PlayMode launcher만 GPU batch로 바꾸고 재실행했다. 중간 GPU 실행 5/8 통과의 세 실패는 fixture 로그 대소문자 기대와 batch 입력 focus 전제였으며, production 변경 없이 위 최종 10사례가 통과했다. 실패 원문을 삭제하거나 성공 수치에 합치지 않았다. crash lane이 만든 정확한 InitTestScene 두 임시 파일만 근거 확인 후 제거했다.
- 수동 화면/fade 육안 확인, 물리 키보드 조작, 오디오 청취는 **미수행**이다. 자동 실제엔진/가상 입력 테스트를 수동 플레이로 보고하지 않는다. Unity MCP/AI 구독은 사용하지 않았다.
- Shared/ClientNet DLL SHA-256이 실행 전과 같고 기존 scene/prefab/asset/meta tracked 변경은 없다(`preflight/unity-dll-after.json`). 새 테스트/소스 meta만 추가한다.

서버 전체 suite는 exit 0, **751 total / 746 pass / 0 fail / 5 skip**이며 기존 5 skip을 유지한다(`server/tests.log`, `server/trx/`). production MapTransition bot은 exit 0, **PASS 1 / FAIL 0**, entity ID 및 HG/Boss/Ending/Town spawn 좌표를 확인했다(`server/bot-maptransition.log`). bot의 Debug 해금은 실제 퀘스트/보스 처치 검증과 구분한다. 실제 이동 로그의 GetMap null 진단 2건은 GameSession.GetMap이 migrating 동안 null을 반환하고 SubmitMoveIntent가 입력을 버리는 기존 계약과 일치한다. 도착 뒤 정상 HP/roster를 확인했으며 크리티컬 결함 근거는 발견하지 못했다. 로그에 entity/migrating 필드가 없어 해당 두 건의 세션을 단정하지 않는다. 문구 개선은 M3 후보로 인계한다. HTML은 실제 diff 9개를 누적했고 Chrome 1440/430/320/768 폭, 테마·접기·인쇄 복원 검사를 통과했다. 최신 PR head CI와 병합 결과는 완료 후 이곳에 추가한다. 독립 상세 판정은 `verification/summary.md`에 둔다.

통합 후 [M2](../2026-09-29-refactor-domain-state/goal.md)는 파티/퀘스트 분리와 스탯/캡처 모델의 두 목표로 나누는 사전 제안을 검토한다. 현재 읽기 전용 제안은 `.backups/reviews/2026-09-30-m2-design-preflight.md`이며 새 구현 권한이나 완료 증거가 아니다. DB 구현은 여전히 제외한다.
### CI에서 발견한 테스트 출력 격리 보완

PR #133 첫 head `0e7a49e081519303f2a62631c06520ae6e891f16`, run `36597497111`은 751 total/745 pass/1 fail/5 skip으로 실패했다. GolemCrossRespawnTests의 정상 로그 출력이 이미 닫힌 StringWriter에 도달했다. ConsoleSerial 25 fixture는 같은 이름으로만 묶였고 collection definition이 없어 다른 logging collection과 병렬 실행됐다. 캡처 복원·dispose 사이 이미 획득한 Console.Out 참조에서 TOCTOU가 가능하며 실제 예외와 일치한다. 어느 capture fixture였는지는 원문으로 단정할 수 없다.

합의된 검증 복구 범위로 구현자에게 새 `02_Server/GameServer.Tests/ConsoleSerialCollection.cs` 하나의 쓰기를 배정했다. 해당 collection만 DisableParallelization=true로 설정하고 다른 병렬 테스트는 유지한다. 다른 SetOut 두 caller는 기존 nonparallel collection임을 확인한다. 생산 코드 변경·테스트 삭제/제외/기대값 완화·단순 성공 rerun은 하지 않는다. 독립 리뷰와 전체 suite, 새 head CI를 다시 확인한 뒤 병합한다. 원문 `server/ci-failed.log`, 원인/재리뷰 `review/ci-console-*.md`.

최종 통합: 격리 보완 후 로컬 suite 751/746pass/0fail/5skip, 독립 raw 대조 완료. 최종 head `51cca702492f6a35083c9bff469959de663f370f` CI run `36598408036` / job `109509165380`은 2026-09-29T16:34:38Z SUCCESS(동일751/746/0/5). 최신 head를 고정해 조건부 승인으로 PR #133을 16:35:03Z 직접 squash 병합, main `4c32c917ff68450174c9883951a8d02e0223d11d`. 미해결 크리티컬 이슈 없음. 앞 초기 CI 실패는 보존하며 수동 GUI/물리입력/청취 미실행 한계도 유지한다. 후속은 M2a goal.
