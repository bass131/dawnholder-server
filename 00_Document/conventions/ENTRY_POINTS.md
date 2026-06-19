# Dawnholder Entry Points — 증상별 디버깅 진입점 룩업

> **이 파일의 책임**: 버그 증상 → 어느 파일·함수부터 봐야 하는지 1줄로 찾아주는 비상 디버깅 룩업표.
>
> 사용법: 버그 증상을 "증상" 열에서 찾아 "시작 파일"과 "시작 함수"로 이동. "흐름 요약"으로 전체 경로 파악.
> 서버는 **silent drop**(헌법 §3)이 기본이라 "아무 일도 안 일어남" 류 증상은 대부분 검증 단계 어딘가에서 조용히 떨어진 것 — 해당 함수의 검증 순서를 위에서부터 따라가면 된다.

---

## 전투 (Combat)

| 증상 | 시작 파일 | 시작 함수 | 흐름 요약 |
|------|-----------|-----------|-----------|
| 공격했는데 데미지가 안 들어감 | `02_Server/GameServer/Maps/Systems/CombatSystem.cs` | `ProcessAttack` | C_Attack → AttackHandler → GameSession.SubmitAttack → map.EnqueueJob → ProcessAttack. 검증 5단계(attacker 존재 → rate-limit 500ms → ValidateRewind → target 조회 → AABB 교차)가 전부 silent drop — 어느 단계에서 떨어지는지 순서대로 확인 |
| 허공 스윙인데 쿨다운만 돈다 | 〃 | `ProcessAttack` | **정상 거동** — 스윙/명중 분리(M4.7). rate-limit은 스윙 *시도* 기준(빈 스윙도 쿨다운 소비, 스팸 차단) |
| 적이 죽었는데 안 사라짐 / 리스폰 안 됨 / StageClear 안 뜸 | `02_Server/GameServer/Maps/GameMap.cs` | `HandleEnemyDeath` | 3 경로(즉시 Combat/지연 Deferred/Dash Skill) 공통 사망 **단일 출처**. S_EntityDeath → (Boss) S_StageClear → RemoveEnemy → (Normal) EnqueueRespawn 순서 계약 |
| 투사체/낙뢰 데미지가 안 터짐 | `02_Server/GameServer/Maps/Systems/DeferredDamageSystem.cs` | `Process` | EnqueueDeferredDamage로 등록 → impactTick 도달 시 HP 적용 + S_HitResult(hitEffect=1 투사체/2 낙뢰). 도착 전 target 사망이면 skip(정상) |
| 데미지 숫자가 이상함 | `98_Shared/GameData/Formulas.cs` | `ComputeDamage` | 서버만 호출(헌법 §1). baseDamage·각종 계수는 `CombatConstants`(02_Server) |
| 맞았는데 HP바가 안 깎임 | `03_Client/Assets/Scripts/Network/ClientPacketHandlers.cs` | HitResult 핸들러 | S_HitResult.currentHp(raw, 음수=사망 신호) → HitEffect enum 분기로 VFX. 서버가 보냈는지부터(위 행들) 분리 확인 |

---

## 이동 (Movement)

| 증상 | 시작 파일 | 시작 함수 | 흐름 요약 |
|------|-----------|-----------|-----------|
| 내 캐릭터가 안 움직임 | `03_Client/Assets/Scripts/Input/LocalPlayerInput.cs` | (입력 게이트) | 입력 → C_MoveIntent → 서버 MoveIntentHandler → InputBits.Decode → GameMap.Tick 물리. 클라 즉시 반응은 `Prediction/LocalPlayerMovement.cs` ⚠️ M4.10 편집 금지(force-adopt 심장부) |
| 캐릭터가 벽/맵 밖으로 나감 | `98_Shared/GameData/Physics.cs` + `GameMap.MapBoundsX` | 물리 step / clamp | 서버 권위 물리(헌법 §1). Teleport 경계는 `SkillSystem.ProcessTeleport`의 MapBoundsX clamp |

---

## 스킬 (Skill)

| 증상 | 시작 파일 | 시작 함수 | 흐름 요약 |
|------|-----------|-----------|-----------|
| 스킬이 안 나감 | `02_Server/GameServer/Maps/Systems/SkillSystem.cs` | `ProcessSkill` | 클라 `LocalPlayerInput.TrySendSkill`(클라측 쿨다운 게이트) → C_SkillUse → SkillUseHandler → ProcessSkill 분기(Thunderbolt/Dash/Teleport). 서버 쿨다운 → ValidateRewind 순 silent drop 확인 |
| 스킬 이펙트 방향/위치가 이상함 | 〃 (S_SkillCast.facing) ↔ 클라 SkillCast 핸들러 | — | facing 1비트 약속: 1=오른쪽/0=왼쪽 (`PlayerEntity.FacingByte` 단일 출처) |
| 쿨다운이 안 돌았는데 시전됨 / 안 돌아감 | `02_Server/GameServer/Entities/PlayerEntity.cs` | `GetLastSkillTick`/`SetLastSkillTick` | 스킬별 마지막 시전 tick 기록 — 서버 tick 기반(헌법 §5, blocking 0). 클래스↔스킬 게이트는 M4.12 예정(미구현 — 전사가 Mage 스킬 시전 가능) |

---

## 맵이동 (Zone Transfer)

| 증상 | 시작 파일 | 시작 함수 | 흐름 요약 |
|------|-----------|-----------|-----------|
| 포탈에 들어가도 무시됨 | `02_Server/GameServer/Maps/Transitions/MapMigration.cs` | `Execute` | 검증 3단계(portalId 유효 → 플레이어 존재 → 근접 2unit) 실패 시 `[Trust]` 로그 + silent drop — 서버 콘솔부터 |
| 맵 넘어가면 기존 플레이어/적이 안 보임 | `02_Server/GameServer/Maps/GameMap.cs` | `SendInitialRosterTo` | EnterGameWorld(최초 진입)/MapMigration(이동) **공용 단일 경로** — S_PlayerJoin(기존 player) + S_EntitySpawn(살아있는 enemy) 1:1 Send. 클라 수신측은 `RosterTransitionBuffer` drain |
| 맵 넘어가면 HP가 풀로 보임 | `02_Server/GameServer/Maps/Transitions/MapMigration.cs` | `Execute` 안 `destMap.SendPlayerHp` | 캐리된 HP를 S_PlayerHp 1:1 재통지 — 누락 시 클라 placeholder(full HP) 고착 |

---

## 동기화 (Sync / Reconcile)

| 증상 | 시작 파일 | 시작 함수 | 흐름 요약 |
|------|-----------|-----------|-----------|
| 원격 플레이어/적이 늦게·뚝뚝 따라옴 | `03_Client/Assets/Scripts/State/RemoteEntity.cs` | `EnqueueSnapshot` | ★S_Snapshot의 serverTick을 버리고 `Time.realtimeSinceStartup` 재도장 — 백로그 #5(창드래그 desync) 유력 범인. 보간 타임소스/버퍼부터 |
| 내 캐릭터가 튕김(rubber-band) | `03_Client/Assets/Scripts/Prediction/PlayerPredictor.cs` | reconcile / `ShouldForceAdopt` | SnapThreshold 1.5f dead-zone ↔ force-adopt 게이트(serverVx 0.0001f가 서버 lunge 감쇠와 결합) ⚠️ M4.10 편집 금지 — M4.11 Phase 3 회귀 안전망 통과 후만 |
| 창 드래그/포커스 잃으면 원격이 순간이동 | `03_Client/Assets/Scripts/Network/MainThreadDispatcher.cs` | `Update` 드레인 | Update 멈춤 → 수신 스냅샷 적체 → 일괄 드레인. 위 RemoteEntity 시간 재도장과 결합된 증상 |
| 전반적인 미세 떨림 / 박자 안 맞음 | (구조 원인 — 특정 파일 없음) | — | 근본 = 클라 가변 dt ↔ 서버 20TPS 고정틱의 공유 시계 부재. M4.11 고정스텝 전환("Fix Your Timestep") 대상 — 고정스텝=계산 박자, 시각 보간=부드러움(부드러움 안 잃음) |
