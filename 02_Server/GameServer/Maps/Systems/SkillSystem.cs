using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps;

/// <summary>
/// §2.2 SkillSystem — GameMap(컨테이너)에서 스킬 로직 추출.
///
/// **단일 책임**: 스킬 1건 처리 (쿨다운 → rewind → 박스 스캔 → deferred enqueue → broadcast).
/// **호출 규율(§1.1)**: GameMap.Tick 안에서만 호출 (EnqueueJob 람다 경유).
/// **데이터 소유**: GameMap이 소유. SkillSystem은 map을 인자로 받아 읽기·변경만.
/// **헌법 #1**: 쿨다운·박스 판정·데미지·freeze 서버 단독. 클라는 skillId+attackerClientTick 힌트뿐.
/// **헌법 #5**: tick loop 안 await/Sleep 0. deferred enqueue만 — tick 카운트다운 실행은 DeferredDamageSystem.
/// </summary>
internal sealed class SkillSystem
{
    internal void ProcessSkill(GameMap map, int casterEntityId, byte skillId, long attackerClientTick)
    {
        // skillId 분기. Phase 03(Dash)/05(Teleport) 구현 시 여기에 case 추가.
        // Dash(2)/Teleport(3)는 클래스 게이트는 통과하지만 아직 미구현 — 방어적 drop (null 참조 방지).
        if (skillId == (byte)SkillId.Thunderbolt)
            ProcessThunderbolt(map, casterEntityId, attackerClientTick);
        // else: 미구현 스킬 = 핸들러 통과 후 여기서 무해하게 drop.
    }

    void ProcessThunderbolt(GameMap map, int casterEntityId, long attackerClientTick)
    {
        // 1) caster player 조회
        PlayerEntity? caster = map.GetPlayer(casterEntityId);
        if (caster == null) return;

        // 2) 쿨다운 검증 (헌법 #3 — tick 기반, blocking call 0)
        long currentTick = map.CurrentTick;
        if (currentTick - caster.GetLastSkillTick((byte)SkillId.Thunderbolt) < CombatConstants.ThunderboltCooldownTicks) return;

        // 3) rewind 범위 검증 (ProcessAttack 동형 — 헌법 #3 Trust Boundary)
        if (attackerClientTick < 0) return;               // (a) 음수
        if (attackerClientTick > currentTick) return;     // (b) 미래
        if (currentTick - attackerClientTick > 4) return; // (c) 200ms 초과

        // rewind: 공격 버튼 눌렀을 당시 위치로 되돌림 → 박스 origin으로 사용.
        Vector2 rewindedOrigin = caster.GetPositionAtTick(attackerClientTick);

        // 4) 박스 스캔: origin 중심 AABB ∩ 살아있는 적 목록 (P3에서 신설한 헬퍼 첫 호출).
        List<EnemyEntity> targets = CombatSystem.ResolveImpactTargets(
            map,
            rewindedOrigin,
            new Vector2(CombatConstants.ThunderboltBoxHalfX, CombatConstants.ThunderboltBoxHalfY));

        // 5) 쿨다운 소비 — 쿨다운 통과 후 박스 스캔 성립 시점에 소비.
        //    빈 박스여도 캐스팅했으면 쿨다운 소비 (허공 시전도 의도한 행동).
        caster.SetLastSkillTick((byte)SkillId.Thunderbolt, currentTick);

        // 6) 각 타겟에 지연 데미지 + freeze (Normal/Golem만)
        long impactTick = currentTick + CombatConstants.LightningDelayTicks;
        foreach (EnemyEntity target in targets)
        {
            int damage = Formulas.ComputeDamage(caster.Stats, target.Stats, CombatConstants.BaseDamage);

            map.EnqueueDeferredDamage(new DeferredImpact
            {
                AttackerEntityId = caster.EntityId,
                TargetEntityId   = target.EntityId,
                Damage           = damage,
                ImpactTick       = impactTick,
                HitEffect        = 2, // 낙뢰
            });

            // freeze: Normal/Golem만. Boss는 ApplyFreeze 호출돼도 BossBehaviorSystem에 가드 없으니 면역.
            // 명시적 Kind 분기로 설계 의도 표현 — "Boss는 데미지만, 이동은 계속".
            if (target.Kind != EnemyKind.Boss)
                target.ApplyFreeze(impactTick + CombatConstants.StunTicks);
        }

        // 7) S_SkillCast broadcast — 캐스팅 연출(목록 없음). 빈 박스도 캐스팅 모션은 나감.
        //    facing: FacingDir>=0이면 1(오른쪽), 0(왼쪽). 1비트 약속(S_PlayerAttack facing과 동형).
        byte facingByte = caster.FacingDir >= 0 ? (byte)1 : (byte)0;
        S_SkillCast castPkt = new S_SkillCast
        {
            casterEntityId   = caster.EntityId,
            skillId          = (byte)SkillId.Thunderbolt,
            strikeDelayTicks = CombatConstants.LightningDelayTicks,
            facing           = facingByte,
        };
        map.BroadcastToAll(castPkt.Write()); // except=null: 전원(로컬 캐스터 포함)
    }
}
