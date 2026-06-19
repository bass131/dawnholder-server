---
phase: P04b
title: 사망 진입점 정리 — HandlePlayerDeath 대칭 추출 (#8)
milestone: M7.6
owner: youngho
grade: 복잡  # 산정: 보통(1도메인×2파일 EnemyStates+GameMap, ~15줄 이동+테스트1) + trust-boundary 위험깃발 자동상향 → 복잡. [opus-routing-by-complexity] 복잡+trust-boundary=Opus worker.
risk: trust-boundary (권위 사망/부활 판정)
depends_on: []
status: in_progress
note: "P04는 감사 #8(사망진입점)+#5(게이트선언화). #5는 04-precondition-gate-declaration.md(`592ac5d` 완료). 본 파일은 #8. 봇 16/16 회복으로 HpSyncSmoke 부활증명 가능해져 진입."
---

# P04b — 사망 진입점 정리 (#8 / HandlePlayerDeath)

> 근거: 감사 #8 (`../../../00_Document/reviews/2026-06-19-architecture-logic-audit.html` line 122-123, 191)
> — *적→플레이어 데미지/HP감소/사망판정/풀피부활이 `States/EnemyStates.cs`에 인라인. 플레이어→적은 이미 `HandleEnemyDeath`로 추출됐는데 적→플레이어만 State에 잔류 = 비대칭. "죽으면 풀피 부활" 핵심 규칙이 묻혀 발견성 낮음.*

## 🎯 목표

`EnemyStates.ApplyMeleeDamage` 안에 인라인된 **플레이어 사망→풀피 부활** 블록을 `GameMap.HandlePlayerDeath(PlayerEntity)` 명시 진입점으로 추출 (`HandleEnemyDeath`와 대칭). 동작 *완전 불변* — 코드 위치만 State 폴더 → GameMap(권위 맵 소유자)으로 이동.

**왜 (감사 제안 line 191)**: M8에서 사망 = 영속화 트리거 후보. 사망 진입점이 단일·명시적이어야 영속화 훅이 깨끗하게 한 곳에 붙는다.

## 📏 현황 실측 (2026-06-19, file:line)

**유일한 플레이어 사망/부활 site** = `02_Server/GameServer/Maps/States/EnemyStates.cs:59-68` (`ApplyMeleeDamage` 내부):
```csharp
if (player.Hp <= 0)
{
    Vector2 spawn = map.PlayerSpawnPosition;
    player.Position = spawn;
    player.Velocity = Vector2.Zero;
    player.OnGround = false;
    player.Hp = player.Stats.MaxHp;
    player.Revive();              // PlayerEntity.cs:262 — ActionFsm→Idle 리셋
    map.SendPlayerHp(player);     // GameMap.cs:514
}
```

- **보스도 같은 경로**: `BossStates.ApplyBossAttack`(BossStates.cs:37-42) → `EnemyStates.ApplyMeleeDamage` 위임. 즉 사망/부활 코드는 **BossStates에 중복 없음** — `ApplyMeleeDamage` 한 곳뿐. (work-pin 이전 메모의 "BossStates 중복" 가정은 실측 결과 부정확 — 정정.)
- **대칭 타깃 존재**: `GameMap.HandleEnemyDeath(EnemyEntity, int)` (GameMap.cs:477). 적 사망은 DashAction:62 / MeleeAction:107 / DeferredDamageSystem:79 → 이 단일 진입점.
- **접근성**: `PlayerSpawnPosition`(internal:117), `SendPlayerHp`(internal:514), `HandleEnemyDeath`(internal:477) — 전부 internal, EnemyStates와 동일 어셈블리. `HandlePlayerDeath`를 `internal`로 추가하면 EnemyStates에서 `map.HandlePlayerDeath(player)` 호출 가능.

### 🔲 범위 밖 (건드리지 말 것 — 별개 관심사)

- **death-guard** `GameMap.cs:236-239` (`IsDead && not DeathState → ChangeState(Death)`): ApplyMeleeDamage가 *즉시 부활*(Hp=MaxHp)시키므로 사실상 도달 불가능한 방어 코드. **본 추출 대상 아님** — 동작 불변 원칙상 그대로 잔류. (주석 staleness는 후속 🟡 후보.)
- **kill-plane 리셋** `GameMap.cs:267-273`: 위치만 리셋, HP 무변화(낙사 데미지 없음) — 사망 아님. 잔류.
- **최초 입장 스폰** `GameSession.cs:512-513`: 입장 placement — 사망 아님. 잔류.
- **#8 클라(시각)**: 로컬 사망 페이드를 `EnemyAttackHandler` 대신 `S_PlayerHp`에서 도출(리포트 line 169) = bucket-b 영호 육안 트랙. **별도, 본 Phase 범위 밖.**

## 🧭 설계 결정 — 인라인 블록 → 명시 진입점

`GameMap.cs` `HandleEnemyDeath` 인접(발견성·대칭)에 신규:
```csharp
/// 플레이어 사망 후처리: PlayerSpawn 재배치 + 풀피 부활 + HUD HP 송신.
/// HandleEnemyDeath와 대칭 — 사망 처리는 권위 맵 소유자의 책임(State 폴더 아님).
/// 현재 호출 경로: EnemyStates.ApplyMeleeDamage(적/보스 근접 치사타). M8 영속화 훅 단일 후보.
/// tick thread invariant: GameMap.Tick(EnemyAISystem/BossBehaviorSystem) 안에서만.
internal void HandlePlayerDeath(PlayerEntity player)
{
    Vector2 spawn = PlayerSpawnPosition;
    player.Position = spawn;
    player.Velocity = Vector2.Zero;
    player.OnGround = false;
    player.Hp = player.Stats.MaxHp;
    player.Revive();
    SendPlayerHp(player);
}
```
`EnemyStates.cs:59-68` 인라인 블록 → 호출 한 줄로 교체:
```csharp
if (player.Hp <= 0)
    map.HandlePlayerDeath(player);
```

**동작 불변 보장**: 옮긴 6문장이 *비트 동일*(`map.` → `this.` 치환만). 호출 시점 동일(ApplyMeleeDamage 내 같은 위치). SendPlayerHp 동반 호출 규율(GameMap.cs:508-509 "Hp mutate 지점마다 SendPlayerHp") 유지.

## ✅ 부활 동치 증명 (plan-auditor 🟡 봉합 — 밀스톤 플랜 line 76)

"사망→풀피 부활" 동치를 증명하는 검증 명시 (없으면 "동작 불변" 주장 = 검증 공백):

1. **단위 (기존 회귀 가드)** — `02_Server/GameServer.Tests/Maps/BossBehaviorTests.cs:23` "사망→리스폰": HP 낮게 세팅 → 보스 공격 → `Position == PlayerSpawnPosition` + `Hp == Stats.MaxHp` + `ActionFsm != DeathState` 단언. 보스 공격이 `ApplyMeleeDamage → HandlePlayerDeath` 경유하므로 추출 후에도 *그대로* 이 동치를 가드.
2. **단위 (신규 — 진입점 명시 증명)** — `HandlePlayerDeathTests` 신설. `HandleEnemyDeathKillerTests` 패턴(EnqueueJob 마샬링) 미러. 직접 `map.HandlePlayerDeath(player)` 호출 → spawn 재배치 + Hp==MaxHp + ActionFsm==Idle(Revive) + S_PlayerHp 송신 단언. **추출된 명시 진입점 자체를 직접 검증** (+1 test → 659).
3. **e2e (봇 안전망)** — `HpSyncSmoke` 시나리오 (death→revive). 봇 회귀 16/16 green(`e5db185`)으로 회복됨 → 사망/부활 전체 경로 e2e 통과 확인.

## ✅ 완료 조건 (done 판사, ADR-029)

- [ ] 빌드 0 error / 신규 warning 0.
- [ ] WSL2 회귀 green — **진입 시점 N=658 확인**(657 baseline + P04#5 `592ac5d` +1) → **N+1=659 기대**(신규 HandlePlayerDeathTests). 비감소 필수.
- [ ] 봇 회귀 16/16 green — **HpSyncSmoke가 부활 동치 e2e 증명**.
- [ ] `reviewer` 🔴 0.
- [ ] `Protocol.Version` 불변 (와이어 포맷 0 변경 — S_PlayerHp/S_EnemyAttack 동일).
- [ ] 동치: `BossBehaviorTests` PASS 유지 + 신규 `HandlePlayerDeathTests` PASS.
- [ ] `EnemyStates.cs`에 사망/부활 인라인 잔류 0 (grep `player.Revive()` → GameMap.HandlePlayerDeath 1곳만).

## ⚠️ 함정

- **trust-boundary 동치 0 약화**: 사망/부활 = 권위 판정. spawn 좌표·MaxHp·Revive·SendPlayerHp 6문장이 *정확히* 보존. 호출 시점(ApplyMeleeDamage 내 동일 위치) 불변.
- **범위 밖 3곳 잔류**: death-guard(236-239)·kill-plane(267-273)·입장스폰(512-513)은 별개 관심사 — 동작 불변 원칙상 건드리지 말 것.
- **DRY 헬퍼 유지**: `ApplyMeleeDamage`의 데미지 적용 로직(hitbox/Formulas/EnterHitState/broadcast)은 그대로 — 사망 분기만 추출. 감사 제안 "DRY 헬퍼 유지 설계" 정합.
- **tick thread invariant**: `HandlePlayerDeath`는 GameMap.Tick(EnemyAISystem/BossBehaviorSystem) 안에서만 호출 — HandleEnemyDeath와 동일 규율. 테스트는 EnqueueJob 마샬링.
