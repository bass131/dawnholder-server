# FEATURE_MAP — 기능별 읽는 지도 (Reader's Map)

> **목적**: 미래 독자(또는 미래의 나)가 *어떤 기능이 어디 사는지*를 헤매지 않고 찾게 하는 지도. 예쁜 문서가 아니라 **탐색 비용을 줄이는 색인**이다. (M7.7 P0 산출물, Codex 검토 #1 — "이동보다 지도 먼저")
>
> **읽는 법**: 기능마다 9차원으로 위치를 박았다 — **Entry**(진입점) / **Trust gate**(신뢰경계 검증) / **Orchestration**(도메인 조율) / **State owner**(상태 소유 actor) / **Notification**(S2C 송신 경계) / **Client mirror**(클라 수신) / **UI** / **Persistence candidate**(M8 저장 후보) / **Volatile**(저장 금지 휘발 상태).
>
> ⚠️ **줄번호는 2026-06-20 기준 — 코드 변경 시 드리프트한다.** 파일·심볼 이름이 1차 좌표, 줄번호는 보조. M7.7 P5(데이터화)·P6(이동) 후 갱신 대상.
>
> 헌법 정합: §1 서버권위(클라는 렌더러), §3 신뢰경계(클라 입력 untrusted), §5 틱 블로킹 금지(actor 모델). 본 지도가 그 경계를 *코드 좌표로* 박는다.

---

## 클러스터 A — 전투 (Combat / Skill / Enemy·AI / Death·Respawn)

### A1. Combat (평타·히트판정)
| 차원 | 좌표 |
|---|---|
| Entry | `Handlers/Combat/AttackHandler.cs:24` (C_Attack 디코드) |
| Trust gate | `AttackHandler.cs:6-11` (패킷=targetEntityId만, attacker=session 강제) + `Network/GameSession.cs:285` (SubmitAttack, _entityId 캡처) |
| Orchestration | `Maps/Systems/CombatSystem.cs:27` → `Maps/Systems/ActionGate.cs:16` (4단계 검증: 상태/쿨다운/클래스/rewind) |
| State owner | `Combat/EnemyEntity.cs:18` (Hp/Hitbox) + `Maps/GameMap.cs:24` (_players/_enemies 컨테이너) |
| Notification | `Maps/Actions/MeleeAction.cs:46` (S_PlayerAttack) · `:94` (S_HitResult 즉시/Knight) · `:80` (S_ProjectileLaunch deferred/Mage) |
| Client mirror | `Handlers/Combat/PlayerAttackHandler.cs:28` (연출, 로컬 skip) · `Handlers/Combat/HitResultHandler.cs:34` (데미지/HP/VFX) |
| UI | `HitResultHandler.cs:51` (EnemyRegistry.ApplyHit) · `Handlers/Combat/EnemyAttackHandler.cs:31` (적 공격 모션/flash) |
| Persistence (M8) | `EnemyEntity.cs:48` (Hp/MaxHp) · `GameMap.cs:531` SendPlayerHp (player HP) |
| Volatile | `EnemyEntity.cs:94-107` (RespawnTicksRemaining, AttackWindupTicks, KnockbackVx/Vy, FrozenUntilTick), ActionState latch |

### A2. Skill (시전·연출)
| 차원 | 좌표 |
|---|---|
| Entry | `Handlers/Skill/SkillUseHandler.cs:20` (C_SkillUse) |
| Trust gate | `SkillUseHandler.cs:25-56` (skillId 범위 :27 / 클래스 게이트 :37 / facing·verticalDir 정규화 :46) + `GameSession.cs:309` (SubmitSkillUse) |
| Orchestration | `Maps/Systems/SkillSystem.cs:17` → ActionRegistry.FromSkillId → `ActionGate.cs:16` |
| State owner | `Maps/Actions/ThunderboltAction.cs:19` (AoE/deferred) · `DashAction.cs` (facing 권위) · `TeleportAction.cs` (수직목표) |
| Notification | `ThunderboltAction.cs:43` (S_SkillCast) · `Maps/Systems/DeferredDamageSystem.cs:67` (S_HitResult @ impactTick) |
| Client mirror | `Handlers/Skill/SkillCastHandler.cs:38` (skillId 분기 연출) · `Handlers/Skill/ProjectileLaunchHandler.cs` |
| UI | `SkillCastHandler.cs:65-91` (채널링/dash/teleport depart+arrive) + `Combat/Effects/` |
| Persistence (M8) | 없음 (스킬은 서버 권위 순간 처리, 저장 대상 아님) |
| Volatile | `ActionGate.cs:39` (LastActionTick 쿨다운) · `DeferredDamageSystem.cs:14` (impact queue) · facing(정규화 후 버림) |

### A3. Enemy·AI (행동·FSM)
| 차원 | 좌표 |
|---|---|
| Entry | (자동 스폰) `Maps/GameMap.cs:422` (SpawnEnemy: kind별 stats/HP/Fsm) |
| Trust gate | `GameMap.cs:79` (kindId 범위 fail-loud); 적은 서버 권위 생성(클라 입력 0) |
| Orchestration | `Maps/Systems/EnemyAISystem.cs:16` (Fsm.Tick + latch + S_EntityState) · `Maps/Systems/BossBehaviorSystem.cs:30` (페이즈2 + 보스 FSM) |
| State owner | `Combat/EnemyEntity.cs:17` (State/Target/AI필드) · `Maps/States/EnemyStates.cs:17` (Patrol/Chase/Hit/Attack) · `Maps/States/BossStates.cs:69` (Idle/Move/Telegraph/Attack) |
| Notification | `EnemyAISystem.cs:39` / `BossBehaviorSystem.cs:66` (S_EntityState: x/y/state/animState) |
| Client mirror | `State/RemoteEntityRegistry.cs` (트랜스폼/애니 동기) · `Combat/Enemies/EnemyRegistry.cs` (적 미러) |
| UI | `Combat/Enemies/` (EnemyView/Motion, animState 기반) + BossAttackEffectSpawner |
| Persistence (M8) | 적 위치(X/Y), HP, 보스 페이즈(IsPhase2), 스폰점 |
| Volatile | `EnemyEntity.cs:68-121` (Target/PatrolDir/latch/KnockbackVx/FrozenUntilTick), Fsm.CurrentState(매 tick 재계산) |

### A4. Death·Respawn (사망·부활)
| 차원 | 좌표 |
|---|---|
| Entry | (자동) 플레이어=`Maps/States/EnemyStates.cs:59` (ApplyMeleeDamage Hp<=0) · `BossStates.cs:37` (ApplyBossAttack) · `DeferredDamageSystem.cs:78`. 적=동 사망 site |
| Trust gate | 서버 권위만 (클라는 HP 수신 후 도출). deferred는 dispose 체크 `DeferredDamageSystem.cs:56` (stale ID 무효화) |
| Orchestration | 플레이어=`GameMap.cs:502` HandlePlayerDeath(spawn 재배치/풀피/부활음) · 적=`GameMap.cs:477` HandleEnemyDeath(S_EntityDeath + StageClear + RemoveEnemy + EnqueueRespawn) |
| State owner | 플레이어=`Maps/PlayerEntity.cs` (IsDead=Hp<=0, DeathState via ActionFsm) · 적=`EnemyEntity.cs:50` + `:94` RespawnTicksRemaining · 부활=`Maps/Systems/RespawnSystem.cs` |
| Notification | 플레이어=`GameMap.cs:531` S_PlayerHp(0) · 적=`GameMap.cs:479` S_EntityDeath + `RespawnSystem.cs:82` S_EntitySpawn |
| Client mirror | 플레이어=`Handlers/Combat/PlayerHpHandler.cs:42` (Hp<=0→RespawnFade) · 적=`Handlers/Combat/EntityDeathHandler.cs:22` (Despawn+사망음) |
| UI | 플레이어=`PlayerHpHandler.cs:44` (사망음+페이드) · 적=`EntityDeathHandler.cs:34` (kind별 사망음) |
| Persistence (M8) | 플레이어 PlayerSpawnPosition·HP복구(MaxHp); 적 HP/원래 스폰; 보스 IsStageCleared 1회성 |
| Volatile | 플레이어 DeathState(revive까지); 적 RespawnTicksRemaining(tick 감소); StageClear flag(맵 단일, 재입장 리셋) |

---

## 클러스터 B — 접속·이동 (Session·Auth / Movement / MapTransition)

### B1. Session·Auth (핸드셰이크·캐릭터선택·lifecycle) — ★ M8 계정식별 핵심
| 차원 | 좌표 |
|---|---|
| Entry | `Handlers/Session/HandshakeHandler.cs:15` · `CharacterSelectHandler.cs:18` · `PingHandler.cs:12` |
| Trust gate | `GameSession.cs:156` (first-packet 강제, 재handshake 거절) · `CharacterSelectHandler.cs:21` (중복/범위) · `GameSession.cs:184` (RequiresSelectedClass 게이트) |
| Orchestration | `GameSession.cs:463` CompleteHandshakeAndEnter → `:479` EnterGameWorldIfReady → `:489` EnterGameWorld |
| State owner | `GameSession.cs:37` _handshakeCompleted · `:41` _stats · `:28` _entityId · `:65` _enteredWorld |
| Notification | S_HandshakeResult=`GameSession.cs:213` Reject / `:466` Complete · S_EnterMap=`:523` |
| Client mirror | `Network/UnityClientSession.cs:30` HandshakeOk · `Handlers/Session/HandshakeResultHandler.cs:22` · `EnterMapHandler.cs:34` (SetLocalEntityId) |
| UI | `Scenes/CharacterSelectController.cs:41` (PlayerPrefs 저장) · `:56` (씬 전환) |
| **Persistence (M8)** | ⚠️ **계정식별 미결**: 현재 신원 = **TCP 세션(GameSession 인스턴스) 자체뿐**, 명시적 계정 개념 0. C_Handshake는 ProtocolVersion만 검증, accountId/token 필드 없음. → M8 = C_Handshake에 accountId/token 추가 + DB 조회 + 다중세션 방어. (단일 LocalDB 계정1+캐릭터N FK, SSOT 1곳 — `_diagnosis.md` Part4) |
| Volatile | `GameSession.cs:33` _closing · `:37` _handshakeCompleted · `:65` _enteredWorld · ClassLoadout.SessionSelectedClass(클라 프로세스 캐시) |

### B2. Movement (intent·예측·reconcile)
| 차원 | 좌표 |
|---|---|
| Entry | `Handlers/Movement/MoveIntentHandler.cs:17` (C_MoveIntent) · `:22` InputBits.Decode |
| Trust gate | `Network/IntentRateLimiter.cs:33` (1초 500 intent fail-closed) · `GameSession.cs:234` SubmitMoveIntent(rate-limit + inputBits 검증 + entityId<0 방어 + migration null race 방어 :256) |
| Orchestration | `GameSession.cs:269` (EnqueueJob → PlayerEntity.EnqueueInput) → `GameMap.Tick()` physics loop 입력 소비 |
| State owner | `Maps/PlayerEntity.cs:69` Position · `:78` Velocity · `:79` OnGround · `:28` _inputQueue(max6, drop-oldest) |
| Notification | S_Snapshot=`SnapshotHandler` (20Hz 전 플레이어 위치) |
| Client mirror | `Prediction/LocalPlayerMovement.cs:27` _predictor · `:60` curr/prev predict pos(보간) · OnServerSnapshot(reconcile) |
| UI | `LocalPlayerMovement` 렌더 위치=보간된 _currPredictPos · _motion.facing |
| Persistence (M8) | 현재 입장 시 고정 spawn만. M8=마지막 위치(MapId+Position) 저장 고려(재접속 복구) |
| Volatile | `PlayerEntity.cs:47` _posHistory(4tick=200ms lag comp) · `:89` _lastClientTick · `LocalPlayerMovement.cs:52` _localTickCounter · `:53` _sendAccumulator |

### B3. MapTransition (포탈·존 이동)
| 차원 | 좌표 |
|---|---|
| Entry | `Handlers/Zone/EnterPortalHandler.cs:19` (C_EnterPortal) · `:24` SubmitEnterPortal |
| Trust gate | `Network/MapMigration.cs:43` Execute(portal lookup :56 / 플레이어 존재 :67 / **근접 2unit :71** / 보스포탈 killCount 게이트 :86 서버권위 QuestRegistry 조회) |
| Orchestration | `GameSession.cs:354` SubmitEnterPortal(EnqueueJob) → `MapMigration.cs:104` (검증→RemovePlayer→SetMigrating(1)→맵B EnqueueJob→AddPlayerWithId→SetMigrating(0)) |
| State owner | `GameSession.cs:61` _migrating(socket thread null 반환) · `:55` _currentMapIdValue(Volatile) · `Maps/PortalTable.cs:35` Portal record |
| Notification | S_PlayerLeave=`MapMigration.cs:118` · S_MapTransition=`:158` · S_PlayerJoin=`:175` |
| Client mirror | `Handlers/Zone/MapTransitionHandler.cs:23` (씬 전환+pending spawn) · `UnityClientSession.cs:195` PendingSpawn · `LocalPlayerMovement.cs:111` (pending 소비) |
| UI | `MapTransitionHandler.cs:34` SceneRouter · `:43` MapNameDisplay · `:61` BGM 전환 |
| Persistence (M8) | 선형 플로우(Town→HG→Boss→Ending→Town). M8=마지막 맵ID+spawn 저장 |
| Volatile | `GameSession.cs:61` _migrating(transient drop 보호) · MapMigration 로컬(capturedStats:107/capturedHp:108/destSpawn/destMapId) |

> ⚠️ **이름이 거짓말하는 곳(P6 이동 대상)**: `MapMigration`은 폴더 `GameServer/Network/`인데 *네트워크가 아니라 존 이동 로직* → P6에서 `Maps/Transitions/`로. `GameSession`도 폴더=Network인데 NS=`...Sessions` → `Sessions/`로.

---

## 클러스터 C — 사회 (Party / Quest) — M7.6에서 분리됨, 경계 검증 완료

### C1. Party (초대/응답/탈퇴/disconnect)
| 차원 | 좌표 |
|---|---|
| Entry | `Handlers/Party/PartyInviteHandler.cs:18` · `PartyRespondHandler.cs:20` · `PartyLeaveHandler.cs:22` |
| Trust gate | `GameSession.cs:386` SubmitPartyInvite · `:396` Respond · `:406` Leave (전부 entityId 강제) |
| Orchestration | `Party/PartyFlow.cs:20` Invite · `:60` Respond · `:87` Leave · `:107` CleanupOnDisconnect |
| State owner | `Party/PartyRegistry.cs:14` (전역 actor) · `Party/PartyState.cs:5` (Members/Leader/PartyId) · `Loop/GameWorld.cs:38` (소유) |
| Notification | `Party/PartyNotifier.cs:21` Error · `:28` InviteRecv · `:40` Update · `:77` Disband |
| Client mirror | `State/PartyState.cs:16` (Member/Leader) · `:68` ApplyUpdate · `:81` Clear · `:94` SetPendingInvite |
| UI | `UI/PartyInvitePopup.cs:26` · `UI/PartyMemberHud.cs:17` |
| Persistence (M8) | `PartyState.cs:20` Members · `PartyRegistry.cs:34` _parties — *멤버십 영속 여부는 M8 결정*(런타임만일 수도) |
| Volatile | `PartyRegistry.cs:46` _pendingInvites · `:69` ExpireStaleInvites(600tick=30초 만료) |

### C2. Quest (진행도·보스 언락·킬카운트)
| 차원 | 좌표 |
|---|---|
| Entry | `Loop/GameWorld.cs:177` onKill(enemy death) · `GameSession.cs:420` SubmitCheatCommand(DEBUG) · `GameWorld.cs:180` ResetAllQuestProgress(보스 킬) |
| Trust gate | `GameWorld.cs:177` onKill 마샬링(킬러 검증=맵 tick) · `Quest/QuestRegistry.cs:72` OnKill(killer 신뢰 보장) |
| Orchestration | `Quest/QuestRegistry.cs:72` OnKill(파티공유 vs 솔로) · `:107` DebugCompleteQuest · `:134` ResetAllQuestProgress |
| State owner | `Quest/QuestRegistry.cs:27` (전역 actor) · `:36` _soloProgress · `:41` _bossUnlocked(영구 latch) · **`Party/PartyState.cs:22` KillCount(파티 공유 — Quest가 cross-actor write)** · `GameWorld.cs:43` 소유 |
| Notification | `Party/PartyNotifier.cs:65` SendQuestUpdate · `QuestRegistry.cs:85` (파티원 전원) |
| Client mirror | `State/QuestState.cs:14` (Current/Target) · `:41` ApplyUpdate |
| UI | `UI/QuestProgressHud.cs:23` · `UI/QuestIntroSequencer.cs:23` · `UI/QuestAlert.cs:21` |
| Persistence (M8) | `PartyState.cs:22` KillCount · `QuestRegistry.cs:36` _soloProgress · `:41` _bossUnlocked — *전부 저장 후보* |
| Volatile | `Quest/QuestConstants.cs:13` BossUnlockKillCount=20(서버권위 상수, 저장 아님) |

> **★ Party↔Quest 경계 검증(Codex #6)**: **단방향 의존, 사이클 0.** QuestRegistry가 PartyRegistry를 생성자 주입(`QuestRegistry.cs:43`, `GameWorld.cs:62` `new QuestRegistry(_party)`)으로 *읽기*. PartyRegistry는 Quest 문자열 0개(역참조 없음). 동일 tick-thread 불변식: `GameWorld.cs:200` **Party.Tick → Quest.Tick** 순서(Quest가 PartyState.KillCount를 읽고 쓰므로 Party 먼저 반영).
>
> **★ M8 열린 질문**: QuestRegistry가 `PartyState.KillCount`를 **직접 쓴다**(`QuestRegistry.cs:78` `party.KillCount++`) = cross-actor mutation(현재 tick 순서로 안전). M8 영속화 시 **트랜잭션 경계** 주의 — KillCount가 Party 소유인데 Quest가 변경하므로, 저장 위치(Party 테이블 컬럼 vs Quest 테이블)와 소유권 이동을 M8에서 결정.

---

## 일부러 깨끗해서 — 손대지 않는 곳

- **소켓 전송 계층 복제** (`04_ClientNet/` ↔ `02_Server/Network/`): ADR-012 의도된 자매구현(.NET Standard 2.1 vs .NET 10). 패킷 정의는 PDL→GenPackets 단일 출처로 강제. 손대지 않음.
- **루트 00~99 레이아웃**: 헌법 정의와 정합.
- **패킷 dispatch 테이블**: 서버 `Handlers/HandlerRegistry.cs` · 클라 `Network/UnityClientSession.cs` 둘 다 `Dictionary<PacketID, IHandler>` 단일 디스패치 — 모범. (확장에 강함: 새 패킷=등록 1줄+핸들러 1파일)
- **actor/registry 패턴, state machine, 생성 protocol(GenPackets)**: Atlas가 "순환/static"으로 잡지만 의도된 설계 — 손대지 않음(`_milestone-plan.md` §11).

---

## M7.7 이후 갱신 의무

- **P5(데이터화)** 후: A2 Skill / A3 Enemy의 "정의" 좌표가 catalog로 이동 → 갱신.
- **P6(이동)** 후: B1 GameSession → `Sessions/`, B3 MapMigration → `Maps/Transitions/`, Maps/Systems NS 정합 → 좌표 갱신.
- **M8(영속화)** 후: 각 기능 "Persistence candidate" → 실제 DB 엔티티/테이블 좌표로 구체화.

> 이 지도는 *살아있는 문서*다. 기능 위치가 바뀌면 여기를 같이 고친다 — 안 그러면 다시 "이름이 거짓말하는" 상태로 돌아간다.
