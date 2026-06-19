using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.Systems;
using Dawnholder.Server.GameServer.Maps.States;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps.Actions;

// 근접 평타(Melee) 행동. CombatSystem.ProcessAttack 본체를 1:1 이관 — 거동 불변.
// AttackGate가 상태·쿨다운·rewind를 선행 검증하므로 Execute는 mutation만.
internal sealed class MeleeAction : IGameAction
{
    internal static readonly MeleeAction Instance = new();

    public ActionKind Kind => ActionKind.Melee;
    public int CooldownTicks => CombatConstants.MeleeCooldownTicks;
    public CharacterClass? RequiredClass => null;

    // [흐름] ActionGate가 ①상태 ②쿨다운 ③클래스 ④rewind 검증 후 단일 Execute 호출.
    // ctx.TargetEntityId는 C_Attack 패킷에서만 들어오는 평타 전용 힌트 (스킬은 -1).
    public bool Execute(GameMap map, PlayerEntity attacker, in ActionContext ctx)
    {
        // attacker 존재는 ActionGate에서 보장 — 여기선 mutation만.

        // rewind: attacker가 공격 버튼 눌렀을 당시 tick의 서버 저장 위치.
        Vector2 rewindedPos = attacker.GetPositionAtTick(ctx.ClientTick);
        AABB attackBox = CombatSystem.GetAttackHitbox(rewindedPos, attacker.Stats.Class);

        // target 조회 (선택) — null이면 허공 스윙.
        EnemyEntity? target = map.GetEnemyById(ctx.TargetEntityId);
        bool hasLiveTarget = target != null && !target.IsDead;

        // facing 스냅 — 타겟 방향, 허공 스윙은 FacingDir 유지.
        if (hasLiveTarget)
            attacker.FacingDir = target!.X >= attacker.Position.X ? (sbyte)1 : (sbyte)-1;

        // AttackState 진입 — lunge 파라미터를 상태에 위임 (§8).
        float lungeVx = attacker.Stats.Class != CharacterClass.Mage
            ? Constants.AttackLungeInitialVx * attacker.FacingDir
            : 0f;
        attacker.EnterAttackState(lungeVx, Constants.KnockbackDecayPerTick);

        // S_PlayerAttack broadcast.
        byte attackType = attacker.Stats.Class == CharacterClass.Mage ? (byte)1 : (byte)0;
        S_PlayerAttack swing = new S_PlayerAttack
        {
            attackerEntityId = attacker.EntityId,
            attackType       = attackType,
            targetEntityId   = ctx.TargetEntityId,
            facing           = attacker.FacingByte,
        };
        map.BroadcastToAll(swing.Write(), except: attacker.Owner);

        if (!hasLiveTarget) return true; // 허공 스윙 — 연출만

        // AABB precision hitbox — miss면 데미지 스킵.
        if (!attackBox.Intersects(target!.Hitbox)) return true;

        int damage = Formulas.ComputeDamage(attacker.Stats, target.Stats, CombatConstants.BaseDamage);

        if (attacker.Stats.Class == CharacterClass.Mage)
        {
            float dx = target.X - rewindedPos.X;
            float dy = target.Y - rewindedPos.Y;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            int travelTicks = Math.Max(
                CombatConstants.MinTravelTicks,
                (int)Math.Ceiling(dist / CombatConstants.ProjectileSpeedPerTick));

            map.EnqueueDeferredDamage(new DeferredImpact
            {
                AttackerEntityId = attacker.EntityId,
                TargetEntityId   = target.EntityId,
                Damage           = damage,
                ImpactTick       = map.CurrentTick + travelTicks,
                HitEffect        = (byte)HitEffect.Projectile,
            });

            S_ProjectileLaunch launch = new S_ProjectileLaunch
            {
                attackerEntityId = attacker.EntityId,
                targetEntityId   = target.EntityId,
                projectileType   = 0,
                travelTicks      = travelTicks,
            };
            map.BroadcastToAll(launch.Write());
        }
        else
        {
            target.Hp -= damage;
            target.TargetEntityId = attacker.EntityId;

            S_HitResult hit = new S_HitResult
            {
                attackerEntityId = attacker.EntityId,
                targetEntityId   = target.EntityId,
                damage           = damage,
                currentHp        = target.Hp,
                maxHp            = target.MaxHp,
                hitEffect        = (byte)HitEffect.Melee,
            };
            map.BroadcastToAll(hit.Write());

            if (target.Hp <= 0)
            {
                map.HandleEnemyDeath(target, attacker.EntityId);
            }
            else
            {
                float knockbackDir = target.X >= attacker.Position.X ? 1f : -1f;
                target.EnterHitState(knockbackDir);
            }
        }
        return true;
    }
}
