using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.Systems;
using Dawnholder.Server.GameServer.Entities;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps.Actions;

// Mage Thunderbolt 행동. SkillSystem.ProcessThunderbolt 본체 1:1 이관 — 거동 불변.
// 쿨다운·클래스·rewind 검증은 ActionGate 선행 처리.
internal sealed class ThunderboltAction : IGameAction
{
    internal static readonly ThunderboltAction Instance = new();

    public ActionKind Kind => ActionKind.Thunderbolt;
    public int CooldownTicks => CombatConstants.ThunderboltCooldownTicks;
    public CharacterClass? RequiredClass => CharacterClass.Mage;

    public bool Execute(GameMap map, PlayerEntity caster, in ActionContext ctx)
    {
        Vector2 rewindedOrigin = caster.GetPositionAtTick(ctx.ClientTick);

        List<EnemyEntity> targets = CombatSystem.ResolveImpactTargets(
            map,
            rewindedOrigin,
            new Vector2(CombatConstants.ThunderboltBoxHalfX, CombatConstants.ThunderboltBoxHalfY));

        long impactTick = map.CurrentTick + CombatConstants.LightningDelayTicks;
        foreach (EnemyEntity target in targets)
        {
            int damage = Formulas.ComputeDamage(caster.Stats, target.Stats, CombatConstants.BaseDamage);

            map.EnqueueDeferredDamage(new DeferredImpact
            {
                AttackerEntityId = caster.EntityId,
                TargetEntityId   = target.EntityId,
                Damage           = damage,
                ImpactTick       = impactTick,
                HitEffect        = (byte)HitEffect.Lightning,
            });
        }

        S_SkillCast castPkt = new S_SkillCast
        {
            casterEntityId   = caster.EntityId,
            skillId          = (byte)SkillId.Thunderbolt,
            strikeDelayTicks = CombatConstants.LightningDelayTicks,
            facing           = caster.FacingByte,
        };
        map.BroadcastToAll(castPkt.Write());
        return true;
    }
}
