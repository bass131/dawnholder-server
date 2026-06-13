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
        // AttackState 진입 — 등속 대쉬: decay=1.0(감쇠 없음), Exit가 임펄스 0으로 정리.
        // 이동 거리 D = DashSpeed × DashTravelTicks × TickDuration = 10 × 8 × 0.05 = 4.0 unit.
        // durationTicks=DashTravelTicks: 대쉬 지속이 이 상수로 실제 제어됨 (AttackCommitWindowTicks 독립).
        caster.EnterAttackState(
            CombatConstants.DashSpeed * caster.FacingDir,
            decayPerTick: 1.0f,
            durationTicks: CombatConstants.DashTravelTicks);

        // 완전 무적: 시전 tick T부터 T+DashTravelTicks까지(포함) 피격 데미지·넉백 0.
        //   대쉬 모션(AttackState)은 T..T+(DashTravelTicks-1) = 8틱이고, 무적은 만료 tick 포함(<=)이라
        //   모션 종료 후 +1틱(50ms) 더 길다 — i-frame은 안전 방향(over-coverage)이 정석(under면 모션 중 노출).
        //   서버 발동 대쉬에서만 세팅 = dash≠melee 구분 (헌법 #3: 클라 무적 신고 경로 없음).
        caster.InvulnUntilTick = map.CurrentTick + CombatConstants.DashTravelTicks;

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
            {
                map.HandleEnemyDeath(target);
            }
            else
            {
                // 허딩: 생존 적을 대쉬 진행 방향으로 밀침. 기존 KnockbackVx 채널 재사용
                //   (EnemyHitState가 X 적분+감쇠 처리). Boss는 EnterHitState가 latch-only라 자동 면역.
                //   세기 시작값 = 기존 넉백(KnockbackInitialVx) 재사용 — Play 튜닝 대상.
                //   죽는 적엔 push 무의미 → 생존 적에만 (death 처리와 상호배타).
                target.EnterHitState(caster.FacingDir);
            }
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
