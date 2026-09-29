# 기능 지도

기능을 바꿀 때 입력 진입점 → 판정·상태 소유자 → 클라이언트 적용 순서로 확인한다. 줄번호는 유지하지 않으며 파일과 심볼을 기준으로 찾는다. 구조는 [ARCHITECTURE](ARCHITECTURE.md), 변경 계약·함정은 [도메인 문서](domains/INDEX.md)에 있다.

아래 서버 경로는 `02_Server/GameServer/`, 클라이언트 경로는 `03_Client/Assets/Scripts/` 기준이다.

| 기능 | 서버 진입·상태·판정 | 클라이언트 적용 |
|---|---|---|
| 연결·버전·캐릭터 | `Sessions/GameSession.cs`, `Handlers/Session/`, `Handlers/HandlerRegistry.cs` | `Network/NetworkService.cs`, `Network/Handlers/Session/`, `Bootstrap/ClassLoadout.cs` |
| 이동·점프 | `Handlers/Movement/MoveIntentHandler.cs` → `GameSession.SubmitMoveIntent` → `Maps/Systems/PlayerPhysicsSystem.cs`; 상태 `Entities/PlayerEntity.cs` | `Input/`, `Prediction/LocalPlayerMovement.cs`, `PlayerPredictor.cs`, `InputHistory.cs` |
| 스냅샷·보간 | `Maps/MapPacketPublisher.cs`, `GameMap.Tick` | `Network/Handlers/Sync/`, `Rendering/` |
| 근접·원거리 공격 | `Handlers/Combat/AttackHandler.cs` → `GameSession.SubmitAttack` → `Maps/Systems/CombatSystem.cs`, `DeferredDamageSystem.cs` | `Network/Handlers/Combat/`, `Combat/Attack/`, `Combat/Effects/` |
| 스킬·행동 가능 조건 | `Handlers/Skill/SkillUseHandler.cs` → `Maps/Systems/ActionGate.cs`, `SkillSystem.cs`; `Maps/States/Actions/` | `Network/Handlers/Skill/`, `Prediction/PlayerAbilityTimers.cs` |
| 적·보스 | `Maps/Systems/EnemyAISystem.cs`, `BossBehaviorSystem.cs`; `Entities/EnemyEntity.cs`, `Maps/States/EnemyStates.cs` | `Network/Handlers/Sync/EntityStateHandler.cs`, `Combat/Enemies/` |
| 피격·사망·리스폰 | `Maps/Systems/CombatSystem.cs`, `RespawnSystem.cs`; 엔티티 HP·상태 | `Network/Handlers/Combat/`, `UI/` |
| 포탈·맵 이동 | `Handlers/Zone/EnterPortalHandler.cs` → `GameSession.SubmitEnterPortal` → `Maps/Transitions/MapMigration.cs`; `Maps/PortalTable.cs` | `Network/Handlers/Zone/MapTransitionHandler.cs`, `Network/SceneRouter.cs` |
| 파티 | `Handlers/Party/` → `Party/PartyFlow.cs`; `PartyRegistry.cs`, `PartyState.cs`, `PartyNotifier.cs` | `Network/Handlers/Party/`, `State/PartyState.cs`, `UI/PartyInvitePopup.cs`, `PartyMemberHud.cs` |
| 처치 진행·보스 해금 | `Loop/GameWorld.cs`의 처치 콜백 → `Quest/QuestRegistry.cs`; `QuestConstants.cs`, Party의 KillCount | `Network/Handlers/Quest/`, `State/QuestState.cs`, `UI/QuestProgressHud.cs` |

## 공통 계약

| 바꾸는 대상 | 원본과 함께 확인할 곳 |
|---|---|
| 패킷 | [PDL](../99_Tools/PacketGenerator/PDL.xml) → [생성 코드](../98_Shared/Protocol/Generated/GenPackets.cs), [버전](../98_Shared/Protocol/ProtocolVersion.cs), 양쪽 핸들러·봇 |
| 물리·입력 | [Physics](../98_Shared/GameData/Physics.cs), [InputBits](../98_Shared/GameData/InputBits.cs); 서버 물리와 클라이언트 예측 |
| 전투 값·공식 | [Combat](../98_Shared/GameData/Combat/), [Enums](../98_Shared/GameData/Enums/); 실행·판정은 서버 |
| 맵 데이터 | [맵 바이너리](../98_Shared/GameData/Maps/), [서버 로더](../02_Server/GameServer/Maps/MapDataLoader.cs), [클라이언트 지형](../03_Client/Assets/Scripts/Prediction/ClientTerrainStore.cs) |
| 전송·프레이밍 | [서버 Network](../02_Server/Network/), [ClientNet](../04_ClientNet/); 프레임 경계와 종료 상태를 양쪽 확인 |
| 회귀 | [GameServer.Tests](../02_Server/GameServer.Tests/), [봇 시나리오](../99_Tools/headless-bot/Scenarios/) |

## 상태의 수명

플레이어 HP·스탯·맵 위치는 현재 실행 중 서버가 소유한다. 입력 큐, 재조정 기록, 이동 중 플래그, 미응답 파티 초대는 임시 상태다. 저장 후보를 모두 DB에 저장하는 것으로 가정하지 않는다. [PlayerSnapshot](../02_Server/GameServer/Maps/PlayerSnapshot.cs)과 파티·퀘스트 저장 정책은 영속화 목표에서 확정해야 한다.

함수별 디버깅 출발점은 [ENTRY_POINTS](conventions/ENTRY_POINTS.md), 과거 변경 이유는 [영역별 기록](archive/INDEX.md)에서 찾는다.
