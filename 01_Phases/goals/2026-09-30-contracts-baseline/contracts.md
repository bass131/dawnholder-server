# 영역별 소유권·요청·상태 계약

P0의 제한 조사에 따른 계약 지도다. 초기 조사 ref는 `4a8700b9abbd04392affd618fa816a2ced426176`, 최종 기준선 B는 `c27b03e888986f2ec8c593cd6c626a9c515595e1`이다. 후속 PR143/144/145의 독립 통합 검토와 최종 기준선 정답 근거 검토를 보완 근거로 사용한다. 코드 전수 감사나 실행 검증 결과가 아니다. 목표 상태·검증 결과는 goal.md에만 둔다.

## 역할과 수명

| 영역 | 요청·상태 owner와 수명 | 실패·완료 경계 | 판단과 다음 검증 |
|---|---|---|---|
| 연결·입장 | 서버 GameSession이 handshake/class/closing·runtime entity 연결을, 클라이언트 ClientConnectionLifetime가 연결 attempt/generation을 소유 | TCP 연결·로컬 queue 접수·gameplay Ready는 다르다. class/closing 및 적용 시 owner를 검사 | 기존 연결 경계 유지. D3에서 저장 class가 확정되는 Loading gate 연결. Handshake/SessionStateMachine/ClientSessionGeneration 계열 테스트 |
| 맵 entry·씬 | 서버 MapMigration이 source 제거·destination 등록을 조정하고, 클라이언트 MapEntryCoordinator가 epoch/HP/player/registry 준비를 소유. SceneLoadQueue는 실제 씬 요청 수명 | MapTransition→HP→roster→join 순서와 coordinator epoch 계약. PlayerHpHandler는 적용 시 현재 Entry.Epoch를 전달하므로 네트워크의 과거 HP packet 자체를 epoch로 구분한다고 보장할 수 없다. 씬 요청 접수와 entry commit 구분 | 기존 M1c/S2 경계 유지. MapPublication/MapEntryCoordinator/MapEntryBinding/SceneTransitionRequest 테스트. 직접 epoch API 검증과 wire 경계 검증을 구분 |
| 이동·예측 | 서버 map actor의 PlayerPhysicsSystem·PlayerEntity가 권위 위치/속도/ack를 갱신. 클라이언트 Input→Movement→Predictor/History/Timers는 입력·예측·replay·쿨다운을 나눔 | 입력 제출 시 값 캡처, 실행 시 owner 재검사. 적용한 입력만 ack. 로컬 예측 성공은 서버 적용 성공이 아님 | 유지. 같은 gated input과 기록의 대응을 보존. Physics/TerrainPhysics/PlayerPredictor/InputHistory/MovementGate 테스트 |
| 원격 표시 | map publisher가 snapshot 표현, player/enemy registry가 view 수명. PR144의 RemoteInterpolationState는 계산 상태, RemoteEntity는 Unity Transform/callback 연결 | 지연 보간·clamp·drift·clear/reset과 callback 실행 순서를 보존 | 기존 S4 적용 유지, 중복 재추출 없음. state/adapter/registry EditMode와 source 동일성 확인. 자동 PlayerLoop·시각 품질은 별도 검증 |
| 전투·스킬·피격 | 세션이 행위자/입력 gate, map actor의 ActionGate/Combat/Skill/Actions가 판정. GameMap은 즉시 피해·사망 후처리, DeferredDamage는 지연 큐 | 즉시/지연 S_HitResult.currentHp의 raw/clamp 차이(권위 target.Hp 상태 보정과 구분), 일반 적 Hit 우선과 보스 Attack 우선, 각 패킷 순서가 다름 | S1/S2 경계 유지. 모양이 비슷해도 다른 정책을 합치지 않는다. ImmediateEnemyHit/CombatSwing/DeferredDamage/ClassSkillGate/MapPublication 테스트 |
| 적·보스·리스폰 | AI/Boss system이 FSM/phase/발행 시점, map의 respawn queue가 지연 재생성 수명을 소유 | map 내부에서 죽은 enemy 참조를 보유하는 것은 외부 snapshot 전달과 다르다. 일반 적·보스 재출현 정책은 별개 | 유지. EnemyAi/BossBehavior/RespawnPlacement/BossEmptyRoomRespawn 테스트. FSM 세부/모든 자산 전수 검증은 이번 조사 밖 |
| 파티·퀘스트 | 서버 PartyRegistry/PartyFlow와 QuestRegistry가 membership/progress/unlock, 클라이언트 PartyState/QuestState는 서버 mirror와 세션 임시 표시를 소유 | runtime ID 기반 세션 한정. pending invite 소비는 membership 확정이 아니다. reset 값 갱신 뒤 알림 순서 보존 | 서버 M2 소유 경계 유지. quest solo/unlock 엔트리 회수 수명은 좁은 추가 조사 필요. PartyRegistry/QuestMembershipLifecycle/QuestNotificationContract 테스트 |
| UI 기능 요청 | 파티 popup이 현재 wire 조립·session 검사·pending 소비·표시를 함께 수행. feature command로 요청 책임을 분리할 후보 | popup의 HandshakeOk 검사와 SendIntent의 entry-ready gate가 다름. void 호출 뒤 pending 소비가 실제 전송 접수를 뜻하지 않을 수 있음 | P1 첫 후보. 기존 정상/거부/drop 계약을 fixture로 고정하고 실제 UI 도달성 확인. popup 닫힘·재시도 정책 변경은 사용자와 결정 |
| UI 표시·구독 | HUD는 session+epoch binding, Skill HUD는 예측 timer 조회. party/quest HUD의 구독은 화면 수명 | 구독한 동일 source를 해제해야 한다. 현재 Instance 재조회 방식의 실제 source 교체 가능성은 추가 확인. MP/Gold 임시 표시를 저장값으로 해석하지 않음 | 표시 전용 화면은 필요한 query/binding 유지. 구독 교체·재enable·late callback의 좁은 검증 필요. HUD/SkillHud/MirrorReset 테스트의 범위를 과장하지 않음 |
| 메뉴·일시정지 | MainMenu는 endpoint 입력·probe·씬 요청, ConnectionProbe는 임시 socket/작업 완료. Pause는 로컬 입력·timeScale과 메뉴/종료 요청 | probe 당시 host와 callback 재조회 값, 닫힌 화면의 late 결과 처리가 미확정. EndConnect의 SocketException 분기는 실패 callback 등록 후 return하며 명시적 Close/finally가 없다. 로컬 pause는 서버 정지가 아님 | 요청 snapshot·attempt 수명과 실패 정리의 재현 후보. 실제 운영 발생·누수량은 확인하지 않았다. 실제 도달 조건과 현행 동작을 먼저 재현하고 stale 성공·실패 정책을 사용자와 결정 |
| 전투 표현 | HitResultHandler가 packet scalar를 캡처해 main apply, 권위 HP mirror 다음 사운드/VFX. EffectSpawnService는 생성·offset·flip·수명을 담당 | 대상/리소스 없음, warn-once와 HP 적용 순서를 보존. 시각 효과와 판정 성공은 별개 | 제한 조사 후 변경 필요성 선택. EffectAnchor/EnemyPrefab/EnemyVisualTable 테스트. 실제 오디오·시각은 미검증 |
| Shared·프로토콜·전송 | PDL→generator→Shared codec, 서버/Unity/bot 소비. transport는 socket/recv/send queue와 framing/종료 수명 | 입력 길이·ID/버전·양쪽 직렬화와 DLL 계약. 서버 PR143 예외 재전파와 ClientNet의 기존 catch/log 정책은 같지 않음 | 유지. 외부 패킷 변경은 단일 writer와 양쪽 검증. FrameValidator/Symmetry/PacketRoundTrip/SessionDisconnectCleanup/ClientConnectorLifecycle 테스트 |
| 도구·설정·실행환경 | generator가 PDL에서 출력 root 결정, WSL helper가 경로별 복제·lock·실행 소유. ClassConfig는 Unity asset, PlayerStats는 불변 기본 정의 | 생성 명령은 파일 쓰기 부작용. PR145는 출력 실패 nonzero, 정상 bytes 및 parse 예외 경계 보존. serialized 참조와 불변 값 정의를 구분 | 기존 S5 적용·기존 runner 유지. PacketGeneratorExit 및 설정 wiring 테스트. 새 전역 도구/프레임워크 구축 근거 없음 |
| DB·저장 수명 | D0의 identity/class·안전 checkpoint; 실제 repository/authority/operation과 입장·종료 연결은 후속 설계/구현 | schema 접속≠게임 저장 완료. tick 비차단, actor 밖 결과 적용 시 owner 확인, 결과불명·fence·release 증빙 필요 | P2–P5/P7 변경 필요. 신규 migration/격리 SQL 두 연결 시험 및 실제 재접속 검증. 기존 스키마를 덮어쓰지 않음 |
| Management 경계 | UI·기록은 별도 worktree/세션, 게임 명령·조회 snapshot·판정은 서버 actor | 운영 principal과 GM 계정 권한, 접수/적용/저장/표시 완료를 구분. 기록 조회 권한은 운영 명령 권한이 아님 | P6에서 명령 목록·권한·오류·as-of/완료 이벤트 공동 설계. 현재 문서 협의는 adapter/API 구현 성공이 아님 |

## 참조와 값 복사의 적용 기준

파일 위치·일상 변경 규칙은 [기능 지도](../../../00_Document/FEATURE_MAP.md)와 [영역별 계약](../../../00_Document/domains/INDEX.md)을 따른다. 위 판단의 대표 코드 진입점은 다음과 같다. 링크는 작업 revision을 열며, 기준선 재현은 B의 동일 경로를 사용한다.

- 연결·입장: [GameSession](../../../02_Server/GameServer/Sessions/GameSession.cs), [ClientConnectionLifetime](../../../03_Client/Assets/Scripts/Network/ClientConnectionLifetime.cs), [MapEntryCoordinator](../../../03_Client/Assets/Scripts/Network/MapEntryCoordinator.cs).
- 이동·전투·적: [GameMap](../../../02_Server/GameServer/Maps/GameMap.cs)의 tick/hit/death와 `Maps/Systems`의 PlayerPhysicsSystem·CombatSystem·DeferredDamageSystem·EnemyAISystem·BossBehaviorSystem; [MapMigration](../../../02_Server/GameServer/Maps/Transitions/MapMigration.cs)의 Execute.
- 파티·퀘스트: [PartyFlow](../../../02_Server/GameServer/Party/PartyFlow.cs), [QuestRegistry](../../../02_Server/GameServer/Quest/QuestRegistry.cs), [PartyInvitePopup](../../../03_Client/Assets/Scripts/UI/PartyInvitePopup.cs), [PartyMemberHud](../../../03_Client/Assets/Scripts/UI/PartyMemberHud.cs).
- 표시·메뉴: [RemoteInterpolationState](../../../03_Client/Assets/Scripts/State/RemoteInterpolationState.cs), [MainMenuController](../../../03_Client/Assets/Scripts/UI/MainMenuController.cs), [ConnectionProbe](../../../03_Client/Assets/Scripts/Network/ConnectionProbe.cs), [EffectSpawnService](../../../03_Client/Assets/Scripts/Combat/Effects/EffectSpawnService.cs).
- 공유·전송·생성: [PlayerStats](../../../98_Shared/GameData/Combat/PlayerStats.cs), [ClientSession](../../../04_ClientNet/ClientSession.cs), [서버 Session](../../../02_Server/Network/Session.cs), [generator Program](../../../99_Tools/PacketGenerator/Program.cs), [PDL](../../../99_Tools/PacketGenerator/PDL.xml).
- 영속성·외부 경계: [D0 설계](../2026-09-29-persistence-design/design.md), [ServerHost](../../../02_Server/GameServer/Hosting/ServerHost.cs), [GameWorld](../../../02_Server/GameServer/Loop/GameWorld.cs). Management 서버 adapter는 아직 구현 근거가 없다.

- PlayerStats는 sealed이며 getter-only 기본값으로 구성된다. PlayerSnapshot/PlayerTransferState는 값과 이 불변 정의 참조를 담으므로 정의 객체를 공유할 수 있다.
- PlayerEntity/EnemyEntity와 session은 살아 있는 owner·수명이 있다. actor 내부 협업을 이유 없이 모두 DTO로 바꾸지 않으며 외부 DB/Management 경계를 넘을 때 필요한 값만 캡처한다.
- ArraySegment의 byte[]나 향후 authority token 배열은 읽기 전용 모양만으로 불변이 되지 않는다. 복사/보유 기간/쓰기 owner를 외부 계약에 명시한다.
- Unity prefab/ScriptableObject 참조와 직렬화 값은 에셋 계약이다. 일반 C# 값 복사로 GUID·씬 참조·수명 검증을 대신하지 않는다.

## 실행 전에 논의할 선택

| 시점 | 필요한 증거와 결정 | 그 전 가능한 일 |
|---|---|---|
| P1 UI 동작 수정 전 | pending 소비/drop/팝업 닫힘, host 변경과 화면 종료 뒤 late 결과를 재현한 뒤 현재 동작 보존 또는 좁은 수정 정책을 사용자와 결정 | 의미를 바꾸지 않는 계약 추출 설계·baseline fixture |
| P2/D1a 복구 설계 | D0의 결과불명/RecoveryRequired 원칙을 유지한 복구 운영 절차, 허용 recovery 주체·수동/자동 절차와 증빙 보존 기간의 영향 정리 | D0 기술 명세·대안·반증 시나리오 작성 |
| P3 SQL 실행 전 | 정확한 격리 DB·권한·시험 GUID·migration/실패 주입/cleanup의 부작용 확정 | 소스·시험 계획 검토와 SQL 미실행 테스트 |
| P6 서버 운영 연동 전 | 조회/수정 명령 목록, principal/GM 권한, 완료·오류·일관성 의미를 사용자와 Management가 공동 결정 | 명령/조회/event 경계 초안과 의존성 정리 |
| 종합 실행 검증 전 | 실제 플레이/씬/입력/오디오/서버/DB 시나리오, 소유 자원과 중단 조건 확정 | 자동 계약 회귀·실행 준비와 미검증 범위 공개 |

추가조사는 결함 확정이 아니다. 기존 Town/fullHP·최초 class 유지·quest 세션 한정 결정은 재합의하지 않는다. 동작을 바꾸지 않는 이름·작은 interface·파일 분할·fixture 방식은 메인이 기술적으로 결정한다.

## 조사 범위의 한계

서버 신규 직접 열람은 16파일과 타입 보강 3파일, 클라이언트/공통/도구 신규 열람은 13파일의 필요한 구간이다. 이전 클라이언트 12파일 조사는 기준 ref 사이 해당 경로 diff 없음으로 재사용했다. 테스트 이름은 탐색 진입점이며 이 조사에서 새로 실행한 결과가 아니다. 생성 코드 본문·대형 맵·shader·전체 prefab/scene/InputAction·모든 FSM과 packet handler를 전수 열람하지 않았다. 이번 표가 그러한 영역의 결함 부재를 보증하지 않는다.

초기 조사 ref→최종 B의 다섯 코드 영역 경로 차이는 12파일이며 PR143의 서버 Session/테스트, PR144의 원격 보간/테스트/meta, PR145의 생성기/테스트/프로젝트 참조로 한정된다. 해당 PR의 독립 통합 검토와 검증본 blob 일치로 보강했다. 이 차이 확인을 모든 조사 파일의 새 실행이나 전수 재검토로 세지 않는다. 최종 B의 .NET 실행 결과는 goal에 별도 기록한다.

근거 장부는 로컬 `.backups/reviews/2026-09-30-p0-server-owner-inventory.md` 및 `2026-09-30-p0-client-boundary-inventory.md`다. Git 공유 문서는 위 경로·심볼과 고정 commit을 통해 확인할 수 있는 계약 요약이며, 로컬 장부 자체의 배포를 전제하지 않는다.
