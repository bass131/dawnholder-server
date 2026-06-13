using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps.Actions;

// Knight Dash 행동. SkillSystem.ProcessDash 본체 1:1 이관 — 거동 불변.
// 쿨다운·클래스·rewind 검증은 ActionGate 선행 처리.
internal sealed class DashAction : IGameAction
{
    internal static readonly DashAction Instance = new();

    public ActionKind Kind => ActionKind.Dash;
    public int CooldownTicks => CombatConstants.DashCooldownTicks;
    public CharacterClass? RequiredClass => CharacterClass.Knight;

    public bool Execute(GameMap map, PlayerEntity caster, long clientTick)
    {
        // AttackState 진입 — Dash 전용 lunge + 감쇠 계수 상태에 위임 (§8).
        caster.EnterAttackState(
            CombatConstants.DashLungeInitialVx * caster.FacingDir,
            CombatConstants.DashLungeDecayPerTick);

        // 경로 타격: rewind 위치 중심 AABB.
        Vector2 rewindedPos = caster.GetPositionAtTick(clientTick);
        Vector2 boxOrigin = rewindedPos + new Vector2(CombatConstants.DashBoxHalfX * caster.FacingDir, 0f);
        List<EnemyEntity> targets = CombatSystem.ResolveImpactTargets(
            map,
            boxOrigin,
            new Vector2(CombatConstants.DashBoxHalfX, CombatConstants.DashBoxHalfY));

        foreach (EnemyEntity target in targets)
        {
            int damage = Formulas.ComputeDamage(caster.Stats, target.Stats, CombatConstants.BaseDamage);
            target.Hp -= damage;
            target.TargetEntityId = caster.EntityId;

            S_HitResult hit = new S_HitResult
            {
                attackerEntityId = caster.EntityId,
                targetEntityId   = target.EntityId,
                damage           = damage,
                currentHp        = target.Hp,
                maxHp            = target.MaxHp,
                hitEffect        = (byte)HitEffect.Dash,
            };
            map.BroadcastToAll(hit.Write());

            if (target.Hp <= 0)
                map.HandleEnemyDeath(target);
        }

        S_SkillCast castPkt = new S_SkillCast
        {
            casterEntityId   = caster.EntityId,
            skillId          = (byte)SkillId.Dash,
            strikeDelayTicks = 0,
            facing           = caster.FacingByte,
        };
        map.BroadcastToAll(castPkt.Write());
        return true;
    }
}
