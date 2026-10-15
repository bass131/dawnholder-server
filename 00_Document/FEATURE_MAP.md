# 기능 지도

기능을 바꿀 때 입력 진입점 → 판정·상태 소유자 → 클라이언트 적용 순서로 확인한다. 줄번호는 유지하지 않으며 파일과 심볼을 기준으로 찾는다. 구조는 [ARCHITECTURE](ARCHITECTURE.md), 변경 계약·함정은 [도메인 문서](domains/INDEX.md)에 있다.

아래 서버 경로는 `02_Server/GameServer/`, 클라이언트 경로는 `03_Client/Assets/Scripts/` 기준이다.

| 기능 | 서버 진입·상태·판정 | 클라이언트 적용 |
|---|---|---|
| 연결·버전·계정·캐릭터 | `Handlers/Session/LoginHandler.cs` → `GameSession.SubmitLogin` → `Accounts/AccountRegistry.cs`(World 틱 등록·종료 해제), `AccountIdResolver.cs`(GUID); 로그인 뒤 `CharacterSelectHandler` | `UI/MainMenuController.cs`·`MainMenuNotice.cs`, `Bootstrap/AccountLoadout.cs`·`ClassLoadout.cs`, `Network/ClientConnectionLifetime.cs`·`NetworkService.cs`·`Handlers/Session/` |
| 이동·점프 | `Handlers/Movement/MoveIntentHandler.cs` → `GameSession.SubmitMoveIntent` → `Maps/Systems/PlayerPhysicsSystem.cs`; 상태 `Entities/PlayerEntity.cs` | `Input/`, `Prediction/LocalPlayerMovement.cs`, `PlayerPredictor.cs`, `InputHistory.cs` |
| 스냅샷·보간 | `Maps/MapPacketPublisher.cs`, `GameMap.Tick` | `Network/Handlers/Sync/`, `State/RemoteInterpolationState.cs`, `State/RemoteEntity.cs`, `Rendering/` |
| 근접·원거리 공격 | `Handlers/Combat/AttackHandler.cs` → `GameSession.SubmitAttack` → `Maps/Systems/CombatSystem.cs`, `DeferredDamageSystem.cs` | `Network/Handlers/Combat/`, `Combat/Attack/`, `Combat/Effects/` |
| 스킬·행동 가능 조건 | `Handlers/Skill/SkillUseHandler.cs` → `Maps/Systems/ActionGate.cs`, `SkillSystem.cs`; `Maps/States/Actions/` | `Network/Handlers/Skill/`, `Prediction/PlayerAbilityTimers.cs` |
| 적·보스 | `Maps/Systems/EnemyAISystem.cs`, `BossBehaviorSystem.cs`; `Entities/EnemyEntity.cs`, `Maps/States/EnemyStates.cs` | `Network/Handlers/Sync/EntityStateHandler.cs`, `Combat/Enemies/` |
| 피격·사망·리스폰 | `Maps/Systems/CombatSystem.cs`, `RespawnSystem.cs`; 엔티티 HP·상태 | `Network/Handlers/Combat/`, `UI/` |
| 포탈·맵 이동 | `Handlers/Zone/EnterPortalHandler.cs` → `GameSession.SubmitEnterPortal` → `Maps/Transitions/MapMigration.cs`(포탈·서버 공용 맵 복귀); `Maps/PortalTable.cs`, `MapKindTable.cs`, `InstanceKey.cs`, `InstanceMapRegistry.cs`(열쇠별 생성·입장 대기·틱 끝 정리); `GameSession`의 현재 맵 참조 | `Network/Handlers/Zone/MapTransitionHandler.cs`, `Network/SceneRouter.cs` |
| 파티 | `Handlers/Party/` → `Party/PartyFlow.cs`; `PartyRegistry.cs`, `PartyState.cs`, `PartyNotifier.cs` | `Network/Handlers/Party/`, `State/PartyState.cs`, `UI/PartyInvitePopup.cs` → `PartyInviteResponseCommand.cs`, `PartyMemberHud.cs` |
| 처치 진행·보스 해금 | `Loop/GameWorld.cs`의 처치 콜백 → `Quest/QuestRegistry.cs`의 solo/party 진행·해금, 보스 처치는 복사본 열쇠의 진행만 초기화; `QuestConstants.cs` | `Network/Handlers/Quest/`, `State/QuestState.cs`, `UI/QuestProgressHud.cs` |
| 아이템·인벤토리·재화 | `Handlers/Inventory/` → `GameSession.SubmitInventoryRequest/SubmitItemUse` → `Items/InventoryRegistry.cs`의 연결 등록·상태 → `InventoryTransitions.cs`; 일반·골렘 처치 입력은 `GameWorld.MakeMap` → `InventoryRegistry.EnqueueKill/ApplyKill` → `KillRewardPolicy.cs`(보스 제외) | `Network/Handlers/Inventory/` → `State/InventoryState.cs`·`InventorySnapshotState.cs` → `UI/HudController.cs` 골드; `MapEntryCoordinator.Ready` → `Network/InventoryRequestController.cs`의 재조회·사용/결과 → `UI/InventoryPanel.cs`·`InventoryPanelView.cs`; I 열림 상태는 `Network/UnityClientSession.IsInventoryPanelOpen`이 연결별 소유 |
| 던전 클리어·보상·복귀 | `GameMap.HandleEnemyDeath` → `Dungeon/DungeonClearState.cs`(수혜자·미확인 집합) → `InventoryRegistry.EnqueueDungeonClear/ApplyDungeonClear` → `Dungeon/DungeonClearPackets.cs`(39); [공유 보상표](../98_Shared/GameData/Dungeon/DungeonClearRewards.cs); `Handlers/Dungeon/DungeonClearConfirmHandler.cs`(40) → `GameSession.SubmitDungeonClearConfirm` → `Dungeon/DungeonClearConfirmation.cs` → `MapMigration.MoveToPublicMap` | `Network/Handlers/Dungeon/DungeonClearResultHandler.cs` → `State/DungeonClearRewardState.cs` → `UI/DungeonClearRewardWindow.cs`; `Combat/CombatBootstrap.cs` → `UI/DungeonEntranceLabel.cs`(Town 포탈 위) |

길드·거점·개인 강화의 서버 진입점은 `Handlers/Guild/` → `GameSession.SubmitGuildRequest/SubmitStrongholdUpgrade/SubmitWeaponEnhance` → `Guild/GuildRegistry.cs`다. 공동 상태/판정/표현은 `GuildState`·`GuildRules`·`GuildPackets`, 개인 성장과 소비는 `Items/InventoryRegistry`·`InventoryState`·`InventoryTransitions`가 소유한다. 세 공격 액션은 `GameMap.ComputePlayerDamage` → `Combat/PlayerDamage.cs`로 현재 강화값을 읽는다. 길드 클라이언트 소비자·Content clear 적립·두 봇 검증은 후속이며 [서버 계약](domains/server.md#길드와-강화)과 [v20 wire](domains/protocol.md#v20-길드거점개인-강화)를 따른다.

## 공통 계약

| 바꾸는 대상 | 원본과 함께 확인할 곳 |
|---|---|
| 패킷 | [PDL](../99_Tools/PacketGenerator/PDL.xml) → [생성 코드](../98_Shared/Protocol/Generated/GenPackets.cs), [버전](../98_Shared/Protocol/ProtocolVersion.cs), 양쪽 핸들러·봇; [v18 던전 클리어 39·40](domains/protocol.md#v18-던전-클리어-패킷) |
| 물리·입력 | [Physics](../98_Shared/GameData/Physics.cs), [InputBits](../98_Shared/GameData/InputBits.cs); 서버 물리와 클라이언트 예측 |
| 전투 값·공식 | [Combat](../98_Shared/GameData/Combat/), [Enums](../98_Shared/GameData/Enums/); 실행·판정은 서버 |
| 맵 데이터 | [맵 바이너리](../98_Shared/GameData/Maps/), [서버 로더](../02_Server/GameServer/Maps/MapDataLoader.cs), [클라이언트 지형](../03_Client/Assets/Scripts/Prediction/ClientTerrainStore.cs) |
| 전송·프레이밍 | [서버 Network](../02_Server/Network/), [ClientNet](../04_ClientNet/); 프레임 경계와 종료 상태를 양쪽 확인 |
| 회귀 | [GameServer.Tests](../02_Server/GameServer.Tests/), [봇 시나리오](../99_Tools/headless-bot/Scenarios/) |

## 상태의 수명

플레이어 HP·스탯·맵 위치는 현재 실행 중 서버가 소유한다. 입력 큐, 재조정 기록, 이동 중 플래그, 미응답 파티 초대는 임시 상태다. [D0 설계](../01_Phases/goals/2026-09-29-persistence-design/design.md)는 고정 계정/캐릭터 하나의 identity·최초 class·안전 checkpoint를 저장 대상으로 정했다. 재접속은 Town·풀 HP이며 quest/보스 해금은 세션 한정이다. [PlayerSnapshot](../02_Server/GameServer/Maps/PlayerSnapshot.cs) 전체의 DB 저장을 뜻하지 않으며 GameServer 저장·복원 연동은 후속 구현이다.

아이템·재화·revision은 World 틱의 `InventoryRegistry`가 연결 수명 동안 메모리로 소유한다. 실제 입장 뒤 등록하고 맵 이동의 소속 공백에서도 유지하며, 종료 시 등록과 상태를 지우고 재접속은 빈 상태로 시작한다. DB 저장은 구현하지 않았다. 입력·보상·종료 경계는 [서버 경제 계약](domains/server.md#경제-상태와-연결-수명), 패킷은 [v17 경제 계약](domains/protocol.md#v17-경제-패킷)에서 확인한다.

무기 강화도 같은 Inventory 접속 수명을 따르며 맵 이동·리스폰에 보존되고 재접속은0단계다. 기본 길드 회원·시드 기록과 공동 금고·거점은 GuildRegistry의 프로세스 수명이고, 접속 종료는 동일 owner의 online만 해제한다. 두 경제 상태 모두 DB에 저장하지 않는다.

함수별 디버깅 출발점은 [ENTRY_POINTS](conventions/ENTRY_POINTS.md), 과거 변경 이유는 [영역별 기록](archive/INDEX.md)에서 찾는다.
