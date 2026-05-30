using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps;

/// <summary>
/// §2.2 CombatSystem — GameMap(컨테이너)에서 전투 로직 추출.
///
/// **단일 책임**: 공격 1건 처리 (attacker/target 검증 → 데미지 → broadcast → death 처리).
/// **호출 규율(§1.1)**: GameMap.Tick 안에서만 호출 (EnqueueJob 람다 경유).
/// **데이터 소유**: GameMap이 소유. CombatSystem은 map을 인자로 받아 읽기·변경만.
/// **System 간 직접 호출 X(§2.2)**: RespawnSystem 큐 등록은 map.EnqueueRespawn() 경유.
///
/// target rewind 비대칭 (attacker-only rewind): M4.4 이월. 본 System에서 *그대로 유지*.
/// </summary>
internal sealed class CombatSystem
{
    /// <summary>
    /// tick thread 안에서 attack 1건 처리.
    /// GameMap.ProcessAttack이 이 메서드로 위임 — 기존 시그니처·동작 완전 보존.
    ///
    /// **검증 순서 (헌법 #3 Trust Boundary — fail-closed silent drop)**:
    ///   1. attacker player 존재 — 없으면 silent drop.
    ///   2. target enemy 존재 — null이면 silent drop.
    ///   3. target alive — IsDead면 silent drop (idempotent).
    ///   4. rate-limit 500ms — AttackCooldownMs 안 재공격 silent drop.
    ///   4.5. rewind 범위 검증 (음수/미래/200ms 초과).
    ///   5. AABB precision hitbox.
    /// </summary>
    internal void ProcessAttack(GameMap map, int attackerEntityId, int targetEntityId, long attackerClientTick)
    {
        // 1) attacker player exists
        PlayerEntity? attacker = map.GetPlayer(attackerEntityId);
        if (attacker == null) return;

        // 2) target enemy exists (player id 던지면 자동 silent drop — PvP 미지원)
        EnemyEntity? target = map.GetEnemyById(targetEntityId);
        if (target == null) return;

        // 3) target alive (idempotent — kill broadcast 후 후속 attack no-op)
        if (target.IsDead) return;

        // 4) rate-limit 500ms silent drop
        long now = Environment.TickCount64;
        if (now - attacker.LastAttackTickMs < CombatConstants.AttackCooldownMs) return;

        // 4.5) rewind 범위 검증 (헌법 #3 Trust Boundary — 3분기 silent drop)
        long currentTick = map.CurrentTick;
        if (attackerClientTick < 0) return;                           // (a) 음수
        if (attackerClientTick > currentTick) return;                  // (b) 미래
        if (currentTick - attackerClientTick > 4) return;             // (c) 200ms 초과

        // rewind: attacker가 공격 버튼을 눌렀을 당시 tick의 서버 저장 위치로 되돌림.
        // target은 현재 위치 사용 (target rewind는 M4.4 backlog).
        Vector2 rewindedPos = attacker.GetPositionAtTick(attackerClientTick);

        // 5) AABB precision hitbox (옛 dist² < range² 교체)
        AABB attackBox = GetAttackHitbox(rewindedPos);
        if (!attackBox.Intersects(target.Hitbox)) return;

        // 통과 → 권위 mutation 진입
        attacker.LastAttackTickMs = now;

        // M4.3 Phase 08a: Attack latch 설정 — attacker의 AnimState.Attack이 AnimLatchTicks 동안 유지.
        // 헌법 #5: tick 단위 카운터 (ms 타이머 X).
        attacker.AttackLatchTicks = CombatConstants.AnimLatchTicks;

        int damage = Formulas.ComputeDamage(attacker.Stats, target.Stats, CombatConstants.BaseDamage);
        target.Hp -= damage;

        // M4.3 Phase 08a: Hit latch 설정 — target(enemy)의 AnimState.Hit이 AnimLatchTicks 동안 유지.
        // Death 우선순위(> Hit)이므로 아래 IsDead 체크 이전에 latch 먼저 세팅 — target이 죽어도
        // latch는 해가 없음 (despawn 후 entity 사라지므로 카운터 감소 경로 없음).
        target.HitLatchTicks = CombatConstants.AnimLatchTicks;

        S_HitResult hit = new S_HitResult
        {
            attackerEntityId = attacker.EntityId,
            targetEntityId = target.EntityId,
            damage = damage,
            currentHp = target.Hp,
            maxHp = target.MaxHp,
        };
        map.BroadcastToAll(hit.Write()); // 전원 (attacker 자기 포함) — except=null

        if (target.Hp <= 0)
        {
            S_EntityDeath death = new S_EntityDeath { entityId = target.EntityId };
            map.BroadcastToAll(death.Write());

            // Boss 사망 시 S_StageClear 1회 broadcast.
            // 순서 약속: S_EntityDeath → S_StageClear (lifecycle → game event).
            if (target.Kind == EnemyKind.Boss && !map.IsStageCleared)
            {
                map.SetStageCleared();
                S_StageClear stageClear = new S_StageClear { bossEntityId = target.EntityId };
                map.BroadcastToAll(stageClear.Write());
            }

            map.RemoveEnemy(target.EntityId);

            // Normal enemy respawn 큐 등록.
            // Boss는 StageClear 1회성 → respawn 없음.
            if (target.Kind == EnemyKind.Normal)
            {
                map.EnqueueRespawn(target);
            }
        }
    }

    /// <summary>
    /// attacker 위치 중심으로 공격 AABB 박스를 생성.
    /// static 순수 함수 — GameMap 상태 의존 X.
    /// AttackHalfExtent = 1.5f → 전체 3×3 unit.
    /// </summary>
    internal static AABB GetAttackHitbox(Vector2 origin)
        => new AABB(origin, new Vector2(CombatConstants.AttackHalfExtent, CombatConstants.AttackHalfExtent));
}
