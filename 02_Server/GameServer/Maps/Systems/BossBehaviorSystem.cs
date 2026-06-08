using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps;

/// <summary>
/// §2.2 BossBehaviorSystem — Boss entity FSM 로직 담당.
///
/// **단일 책임**: EnemyKind.Boss의 패턴 FSM 1틱 진행.
///   쿨다운 카운트다운 → telegraph 시작 → 데미지 판정 → 쿨다운 리셋.
///
/// **호출 규율(§1.1)**: GameMap.Tick에서 EnemyAISystem 다음에 호출.
///   tick thread invariant — lock 없음.
///
/// **헌법 #1 (Server Authority)**: 데미지 판정은 player.Position (서버 권위 위치).
///   클라 신고 위치 사용 절대 금지.
///
/// **헌법 #5 정합**: Task.Delay / Thread.Sleep / DateTime 타이머 전혀 없음.
///   모든 타이밍은 tick 카운터(int) 감소만.
///
/// **매 틱 객체 할당 금지**: 데미지 판정 결과로 패킷 생성은 broadcast 시점만.
///   패턴 루프 안에서 List/array 동적 할당 없음 (EnemyEntity 필드 사전 할당).
///
/// **System 간 직접 호출 X(§2.2)**: CombatSystem 직접 참조 없음.
///   보스→플레이어 데미지는 본 System이 직접 처리 (독립 경로).
///   EnemyAISystem의 Boss 분기 latch 감소 + broadcast는 본 System으로 이관.
/// </summary>
internal sealed class BossBehaviorSystem
{
    /// <summary>
    /// Boss FSM 1틱 진행.
    ///
    /// **FSM 흐름**:
    ///   1. 보스가 살아있는 동안만 처리 (IsDead → skip).
    ///   2. 페이즈 2 전환 체크 (HP ≤ 50%, 1회성 idempotent flag).
    ///   3. telegraph 중이면 카운트다운 감소 → 0 도달 틱에 데미지 판정 + 쿨다운 리셋.
    ///   4. telegraph 아니면 쿨다운 감소 → 0 도달 시 telegraph 시작 (S_EntityState animState=Attack broadcast).
    ///   5. latch 카운터 감소 + S_EntityState broadcast (SnapshotTickInterval 마다).
    /// </summary>
    internal void Update(GameMap map, long tickNumber)
    {
        bool shouldBroadcast = tickNumber % Constants.SnapshotTickInterval == 0;

        foreach (EnemyEntity enemy in map.Enemies.Values)
        {
            if (enemy.Kind != EnemyKind.Boss) continue;
            if (enemy.IsDead) continue;

            // ── 페이즈 2 전환 체크 (1회성 idempotent) ─────────────────────────
            if (!enemy.IsPhase2 && enemy.Hp <= (int)(enemy.MaxHp * CombatConstants.BossPhase2HpThreshold))
            {
                enemy.IsPhase2 = true;
                // 쿨다운 중이면 페이즈 2 쿨다운으로 clamp (남은 틱이 더 크면 교체).
                // 진행 중 telegraph는 의도적으로 유지 — 이미 예고한 타이밍을 단축하면 회피 공정성 깨짐.
                if (enemy.TelegraphTicksRemaining == 0 &&
                    enemy.AttackCooldownTicks > CombatConstants.BossPhase2CooldownTicks)
                {
                    enemy.AttackCooldownTicks = CombatConstants.BossPhase2CooldownTicks;
                }
            }

            // ── telegraph 중 ───────────────────────────────────────────────────
            if (enemy.TelegraphTicksRemaining > 0)
            {
                enemy.TelegraphTicksRemaining--;

                if (enemy.TelegraphTicksRemaining == 0)
                {
                    // telegraph 완료 → 데미지 판정
                    ApplyBossAttack(map, enemy);

                    // 쿨다운 리셋 (페이즈별)
                    enemy.AttackCooldownTicks = enemy.IsPhase2
                        ? CombatConstants.BossPhase2CooldownTicks
                        : CombatConstants.BossPhase1CooldownTicks;
                }
            }
            else
            {
                // ── 쿨다운 중 ────────────────────────────────────────────────
                if (enemy.AttackCooldownTicks > 0)
                {
                    enemy.AttackCooldownTicks--;
                }

                if (enemy.AttackCooldownTicks == 0)
                {
                    // 쿨다운 완료 → telegraph 시작
                    enemy.TelegraphTicksRemaining = enemy.IsPhase2
                        ? CombatConstants.BossPhase2TelegraphTicks
                        : CombatConstants.BossTelegraphTicks;

                    // animState = Attack broadcast — 클라가 이펙트로 예고 표시 (Phase 05 소비).
                    enemy.AttackLatchTicks = enemy.TelegraphTicksRemaining + CombatConstants.AnimLatchTicks;

                    S_EntityState telegraphPkt = new S_EntityState
                    {
                        entityId = enemy.EntityId,
                        x = enemy.X,
                        y = enemy.Y,
                        state = (byte)enemy.State,
                        animState = (byte)AnimState.Attack,
                    };
                    map.BroadcastToAll(telegraphPkt.Write());
                }
            }

            // ── latch 카운터 감소 ─────────────────────────────────────────────
            if (enemy.HitLatchTicks > 0) enemy.HitLatchTicks--;
            // AttackLatchTicks는 위에서 세팅되거나 telegraph 완료 후 자연 감소.
            if (enemy.AttackLatchTicks > 0) enemy.AttackLatchTicks--;

            // ── S_EntityState broadcast (SnapshotTickInterval 마다) ────────────
            if (shouldBroadcast)
            {
                byte bossAnimState = ComputeBossAnimState(enemy);
                S_EntityState statePkt = new S_EntityState
                {
                    entityId = enemy.EntityId,
                    x = enemy.X,
                    y = enemy.Y,
                    state = (byte)enemy.State,
                    animState = bossAnimState,
                };
                map.BroadcastToAll(statePkt.Write());
            }
        }
    }

    /// <summary>
    /// 보스 공격 범위 내 플레이어에게 데미지 적용. 헌법 #1 — player.Position (서버 권위).
    ///
    /// **범위 판정**: 보스 중심 ±BossAttackHalfExtent AABB ∩ 플레이어 현재 권위 위치.
    ///   범위 밖 = 데미지 0 (판정 skip).
    ///
    /// **사망 처리**: HP ≤ 0 → 스폰 재배치 + HP full + Revive() 호출(ActionFsm을 Idle로 복귀).
    ///   리스폰 통지는 다음 S_Snapshot에 맡김 (snapshot이 100ms 주기라 체감 즉각).
    ///
    /// tick thread invariant — BossBehaviorSystem.Update 안에서만 호출.
    /// </summary>
    void ApplyBossAttack(GameMap map, EnemyEntity boss)
    {
        AABB bossAttackBox = new AABB(
            new Vector2(boss.X, boss.Y),
            new Vector2(CombatConstants.BossAttackHalfExtent, CombatConstants.BossAttackHalfExtent));

        byte attackPattern = boss.IsPhase2 ? (byte)1 : (byte)0;

        // 범위 내 모든 플레이어에게 판정 (보스는 다인 공격).
        // Players는 List — foreach 중 mutation 없음 (리스폰은 position 변경, RemovePlayer 아님).
        foreach (PlayerEntity player in map.Players)
        {
            // 플레이어 AABB 충돌 체크 (헌법 #1 — player.Position = 서버 권위 위치)
            AABB playerBox = new AABB(player.Position, new Vector2(0.5f, 0.5f));
            if (!bossAttackBox.Intersects(playerBox)) continue;

            // 데미지 계산 (헌법 #1 서버만 계산, 헌법 #4 Formulas.cs 단일 공식)
            int damage = Formulas.ComputeDamage(boss.Stats, player.Stats, CombatConstants.BossBaseDamage);
            player.Hp -= damage;

            // 피격 hitstun 진입. 보스 X와 플레이어 X 비교로 넉백 방향 결정.
            // dirX = 보스가 어느 쪽에 있냐 (플레이어가 보스보다 오른쪽 → dirX 양수 → 오른쪽으로 날아감).
            float dirX = player.Position.X >= boss.X ? 1f : -1f;
            player.EnterHitState(dirX);

            // S_EnemyAttack broadcast (데미지 적용 직후 값, 0 이하 가능)
            S_EnemyAttack attackPkt = new S_EnemyAttack
            {
                attackerId = boss.EntityId,
                targetId = player.EntityId,
                damage = damage,
                targetCurrentHp = player.Hp,
                attackPattern = attackPattern,
            };
            map.BroadcastToAll(attackPkt.Write());

            // 플레이어 사망 → 스폰 재배치 + HP full
            if (player.Hp <= 0)
            {
                Vector2 spawn = map.PlayerSpawnPosition;
                player.Position = spawn;
                player.Velocity = Vector2.Zero;
                player.OnGround = false;
                player.Hp = player.Stats.MaxHp;

                // Revive()로 ActionFsm을 Idle로 초기화 — 안 하면 부활 후 DeathState에서 이동 불가.
                player.Revive();
            }
        }
    }

    /// <summary>
    /// 보스 animState 계산. 우선순위: Death > Attack > Hit > Idle.
    /// Attack이 Hit보다 높음 — telegraph/공격 모션이 피격에 끊기지 않게.
    /// 피격 피드백은 클라 DamageFlash(S_HitResult 경로)가 담당.
    /// 보스는 Walk 없음 (이동 없는 고정형).
    /// </summary>
    static byte ComputeBossAnimState(EnemyEntity boss)
    {
        if (boss.IsDead)
            return (byte)AnimState.Death;
        if (boss.AttackLatchTicks > 0)
            return (byte)AnimState.Attack;
        if (boss.HitLatchTicks > 0)
            return (byte)AnimState.Hit;
        return (byte)AnimState.Idle;
    }
}
