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
        if (skillId == (byte)SkillId.Thunderbolt)
            ProcessThunderbolt(map, casterEntityId, attackerClientTick);
        else if (skillId == (byte)SkillId.Dash)
            ProcessDash(map, casterEntityId, attackerClientTick);
        else if (skillId == (byte)SkillId.Teleport)
            ProcessTeleport(map, casterEntityId, attackerClientTick);
        // else: 미구현 스킬 = 무해 drop.
    }

    void ProcessTeleport(GameMap map, int casterEntityId, long attackerClientTick)
    {
        // 1) caster 조회
        PlayerEntity? caster = map.GetPlayer(casterEntityId);
        if (caster == null) return;

        // 2) 쿨다운 검증 (헌법 #3 — tick 기반, blocking call 0)
        long currentTick = map.CurrentTick;
        if (currentTick - caster.GetLastSkillTick((byte)SkillId.Teleport) < CombatConstants.TeleportCooldownTicks) return;

        // 3) rewind 범위 검증 (ProcessDash/ProcessThunderbolt 동형 — 헌법 #3 Trust Boundary)
        if (attackerClientTick < 0) return;               // (a) 음수
        if (attackerClientTick > currentTick) return;     // (b) 미래
        if (currentTick - attackerClientTick > 4) return; // (c) 200ms 초과

        // 4) 쿨다운 소비 — 검증 통과 직후 (경계 밖 clamp 후에도 쿨다운 소비, 허공 시전 정합).
        caster.SetLastSkillTick((byte)SkillId.Teleport, currentTick);

        // 5) 목적지 계산: facing 방향 × TeleportDistance.
        //    Teleport는 속도 채널이 아닌 위치 즉시 set — Velocity 불변, ExternalVelX 채널 안 씀.
        float rawDestX = caster.Position.X + CombatConstants.TeleportDistance * caster.FacingDir;

        // 6) 맵 경계 clamp (헌법 §3 서버 권위 — 클라가 좌표 보내면 벽 뚫기 핵이 됨).
        //    MapBoundsX는 terrain Solids 전체의 MinX/MaxX 합산 — terrain null이면 ±∞(평지 맵).
        (float boundsMin, float boundsMax) = map.MapBoundsX;
        float destX = MathF.Max(boundsMin, MathF.Min(boundsMax, rawDestX));

        // 7) 위치 즉시 set + position history 갱신.
        //    Velocity와 OnGround는 점프 전 상태 유지 — 이동 채널이 아닌 순간이동이므로 물리 연속성 유지.
        //    position history(rewind 버퍼)는 새 위치로 갱신 — 텔레포트 후 틱에서 lag-comp가 새 위치 참조.
        caster.Position = new Vector2(destX, caster.Position.Y);
        caster.RecordPosition(currentTick, caster.Position);

        // 8) S_SkillCast broadcast — 클라 "보간 끊기" 신호 (Phase 06 클라가 이 신호로 스냅).
        //    데미지/타격 없음 — 순수 이동 스킬. DeferredDamage/HitResult 경로 안 탐.
        byte facingByte = caster.FacingDir >= 0 ? (byte)1 : (byte)0;
        S_SkillCast castPkt = new S_SkillCast
        {
            casterEntityId   = caster.EntityId,
            skillId          = (byte)SkillId.Teleport,
            strikeDelayTicks = 0,
            facing           = facingByte,
        };
        map.BroadcastToAll(castPkt.Write());
    }

    void ProcessDash(GameMap map, int casterEntityId, long attackerClientTick)
    {
        // 1) caster 조회
        PlayerEntity? caster = map.GetPlayer(casterEntityId);
        if (caster == null) return;

        // 2) 쿨다운 검증 (헌법 #3 — tick 기반, blocking call 0)
        long currentTick = map.CurrentTick;
        if (currentTick - caster.GetLastSkillTick((byte)SkillId.Dash) < CombatConstants.DashCooldownTicks) return;

        // 3) rewind 범위 검증 (ProcessThunderbolt 동형 — 헌법 #3 Trust Boundary)
        if (attackerClientTick < 0) return;               // (a) 음수
        if (attackerClientTick > currentTick) return;     // (b) 미래
        if (currentTick - attackerClientTick > 4) return; // (c) 200ms 초과

        // 4) 쿨다운 소비 — 검증 통과 직후.
        caster.SetLastSkillTick((byte)SkillId.Dash, currentTick);

        // 5) 전방 lunge 부여: AttackLungeVx 채널 재활용 (M4.7 근접 스윙과 동일 채널, 더 큰 값).
        //    EnterAttackState → AttackState.Enter에서 StateTicksRemaining = AttackCommitWindowTicks(8틱).
        //    LungeDecayPerTick를 Dash 전용 값(0.85)으로 덮어써 평타(0.75)보다 완만하게 감쇠
        //    → 더 긴 전진 + 끝이 부드럽게 잦아드는 느낌. AttackState.Exit에서 0.75로 자동 리셋.
        caster.EnterAttackState();
        caster.LungeDecayPerTick = CombatConstants.DashLungeDecayPerTick;
        caster.AttackLungeVx = CombatConstants.DashLungeInitialVx * caster.FacingDir;

        // 6) 경로 타격: rewind 위치 중심 AABB — facing 방향으로 편심 박스.
        //    FacingDir(+1=오른쪽 / -1=왼쪽)으로 박스 origin을 전방에 배치.
        //    halfX 범위 안 적에게 즉시 데미지(대쉬는 짧아 지연 필요 없음 — 썬더볼트와의 trade-off).
        Vector2 rewindedPos = caster.GetPositionAtTick(attackerClientTick);
        // 박스 center를 전방 halfX 만큼 이동시켜 "전방 스윕" 효과.
        Vector2 boxOrigin = rewindedPos + new Vector2(CombatConstants.DashBoxHalfX * caster.FacingDir, 0f);
        List<EnemyEntity> targets = CombatSystem.ResolveImpactTargets(
            map,
            boxOrigin,
            new Vector2(CombatConstants.DashBoxHalfX, CombatConstants.DashBoxHalfY));

        foreach (EnemyEntity target in targets)
        {
            int damage = Formulas.ComputeDamage(caster.Stats, target.Stats, CombatConstants.BaseDamage);
            target.Hp -= damage;
            target.TargetEntityId = caster.EntityId; // 피격 aggro

            S_HitResult hit = new S_HitResult
            {
                attackerEntityId = caster.EntityId,
                targetEntityId   = target.EntityId,
                damage           = damage,
                currentHp        = target.Hp,  // raw(음수 가능) — 음수=사망 신호 계약
                maxHp            = target.MaxHp,
                hitEffect        = (byte)HitEffect.Dash,
            };
            map.BroadcastToAll(hit.Write());

            if (target.Hp <= 0)
                map.HandleEnemyDeath(target);
        }

        // 7) S_SkillCast broadcast — 클라 연출 신호. strikeDelayTicks=0 (즉시 적용).
        byte facingByte = caster.FacingDir >= 0 ? (byte)1 : (byte)0;
        S_SkillCast castPkt = new S_SkillCast
        {
            casterEntityId   = caster.EntityId,
            skillId          = (byte)SkillId.Dash,
            strikeDelayTicks = 0,
            facing           = facingByte,
        };
        map.BroadcastToAll(castPkt.Write());
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
                HitEffect        = (byte)HitEffect.Lightning,
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
