using System.Collections.Generic;
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
    ///
    /// **검증 순서 (헌법 #3 Trust Boundary — fail-closed silent drop)**:
    ///   1. attacker player 존재 — 없으면 silent drop.
    ///   2. rate-limit 500ms — AttackCooldownMs 안 재공격 silent drop.
    ///   3. rewind 범위 검증 (음수/미래/200ms 초과).
    ///   ↳ 여기까지 통과 = 유효 스윙 시도 → EnterAttackState + S_PlayerAttack broadcast.
    ///   4. target enemy 조회 (선택) + alive 확인.
    ///   5. AABB precision hitbox → 명중 시에만 데미지 + S_HitResult.
    ///
    /// **연출/명중 분리 정책 (M4.7 Phase 03)**:
    ///   rate-limit/rewind 통과 = 유효 스윙. 명중 무관하게 스윙 상태 진입.
    ///   데미지는 AABB 명중 시에만 적용 — 서버 권위 불변(헌법 #1).
    ///   rate-limit 카운트는 스윙 시도 기준 유지 — 스팸 차단 불변(헌법 #3).
    /// </summary>
    internal void ProcessAttack(GameMap map, int attackerEntityId, int targetEntityId, long attackerClientTick)
    {
        // 1) attacker player exists
        PlayerEntity? attacker = map.GetPlayer(attackerEntityId);
        if (attacker == null) return;

        // 2) rate-limit 500ms silent drop — target 유무와 무관하게 앞단에서 검증.
        long now = Environment.TickCount64;
        if (now - attacker.LastAttackTickMs < CombatConstants.AttackCooldownMs) return;

        // 3) rewind 범위 검증 (헌법 #3 Trust Boundary — 3분기 silent drop)
        long currentTick = map.CurrentTick;
        if (attackerClientTick < 0) return;                     // (a) 음수
        if (attackerClientTick > currentTick) return;            // (b) 미래
        if (currentTick - attackerClientTick > 4) return;       // (c) 200ms 초과

        // rewind: attacker가 공격 버튼을 눌렀을 당시 tick의 서버 저장 위치로 되돌림.
        // target은 현재 위치 사용 (target rewind는 M4.4 backlog).
        Vector2 rewindedPos = attacker.GetPositionAtTick(attackerClientTick);
        AABB attackBox = GetAttackHitbox(rewindedPos, attacker.Stats.Class);

        // 통과 → 스윙 권위 mutation 진입 (명중 여부 무관).
        // rate-limit 카운트는 스윙 시도 기준 — 빈 스윙도 쿨다운 소비(스팸 차단 불변, 헌법 #3).
        attacker.LastAttackTickMs = now;

        // 공격 commit window 진입 — ActionFsm이 AnimState.Attack 상태를 유지하며 이동을 잠금.
        attacker.EnterAttackState();

        // 근접 스윙 전방 lunge — Knight만(원거리 Mage 제외). FacingDir 방향으로 임펄스 박고
        //   AttackState.Tick이 감쇠. 서버 권위 이동(헌법 #1), 클라는 force-adopt로 렌더.
        //   EnterAttackState 직후 세팅 — ChangeState의 이전 상태 Exit(lunge 0 정리) 다음에 박혀야 유지.
        if (attacker.Stats.Class != CharacterClass.Mage)
            attacker.AttackLungeVx = CombatConstants.AttackLungeInitialVx * attacker.FacingDir;

        // S_PlayerAttack broadcast: 공격 연출(스윙) 알림. attacker 본인 제외 — 로컬 선예측 중.
        // attackType: Mage=1(원거리 연출), Knight=0(근접 연출) — CharacterClass enum 정합.
        // facing: FacingDir(마지막 이동 방향). target 있으면 target 방향으로 snap 가능하지만
        //         목표물 없는 허공 스윙도 방향을 유지하려면 FacingDir이 더 안전 — 통일.
        byte attackType = attacker.Stats.Class == CharacterClass.Mage ? (byte)1 : (byte)0;
        byte facingByte = attacker.FacingDir >= 0 ? (byte)1 : (byte)0; // 1=오른쪽, 0=왼쪽
        S_PlayerAttack swing = new S_PlayerAttack
        {
            attackerEntityId = attacker.EntityId,
            attackType       = attackType,
            targetEntityId   = targetEntityId, // sentinel(0) 또는 stale id도 그대로 전달 — 클라가 처리
            facing           = facingByte,
        };
        map.BroadcastToAll(swing.Write(), except: attacker.Owner);

        // 4) target 조회 (선택) — null이면 허공 스윙, 데미지 없음.
        // targetEntityId=0 sentinel 또는 이미 죽은 stale id면 null 반환.
        EnemyEntity? target = map.GetEnemyById(targetEntityId);
        if (target == null || target.IsDead) return;

        // 5) AABB precision hitbox — miss면 데미지 스킵 (스윙·S_PlayerAttack은 이미 나감).
        if (!attackBox.Intersects(target.Hitbox)) return;

        // 명중 → 데미지 권위 mutation 진입.
        int damage = Formulas.ComputeDamage(attacker.Stats, target.Stats, CombatConstants.BaseDamage);

        if (attacker.Stats.Class == CharacterClass.Mage)
        {
            // Mage 원거리: 즉발 명중 판정 후 지연 데미지(헌법 #5). 사거리 비례 travelTicks.
            // 사이드스크롤 특성상 Y 이동이 제한적이므로 수평 |dx| 기준 계산 — 투사체 시각 이동과 일치.
            float dx = Math.Abs(target.X - rewindedPos.X);
            int travelTicks = Math.Clamp(
                (int)Math.Round(dx / CombatConstants.ProjectileSpeedPerTick),
                CombatConstants.MinTravelTicks,
                CombatConstants.MaxTravelTicks);

            map.EnqueueDeferredDamage(new DeferredImpact
            {
                AttackerEntityId = attacker.EntityId,
                TargetEntityId   = target.EntityId,
                Damage           = damage,
                ImpactTick       = map.CurrentTick + travelTicks,
                HitEffect        = 1,
            });

            // freeze: 투사체 도착 + StunTicks 동안 이동 봉쇄(stun). Boss는 ApplyFreeze 호출돼도 면역(EnemyEntity 설계).
            target.ApplyFreeze(map.CurrentTick + travelTicks + CombatConstants.StunTicks);

            // 발사 연출 broadcast — 전원(S_HitResult와 동일 except=null)
            S_ProjectileLaunch launch = new S_ProjectileLaunch
            {
                attackerEntityId = attacker.EntityId,
                targetEntityId   = target.EntityId,
                projectileType   = 0, // 0 = Mage 평타
                travelTicks      = travelTicks,
            };
            map.BroadcastToAll(launch.Write());
            // S_HitResult는 도착 시 DeferredDamageSystem이 발송 — 여기서 보내지 않음
        }
        else
        {
            // Knight 즉시 데미지 경로 유지(hitEffect=0 근접).
            // currentHp raw(음수 가능) — LagSim 봇/테스트의 사망 신호 계약(음수=사망). 건드리지 마라.
            target.Hp -= damage;

            // 피격 aggro 트리거 (후공 포함): 공격자를 target으로 등록.
            // Boss는 Fsm=null라 ResolveAfterHit 미호출이므로 무해.
            target.TargetEntityId = attacker.EntityId;

            S_HitResult hit = new S_HitResult
            {
                attackerEntityId = attacker.EntityId,
                targetEntityId   = target.EntityId,
                damage           = damage,
                currentHp        = target.Hp,  // raw(음수 가능) — 음수=사망 신호 계약
                maxHp            = target.MaxHp,
                hitEffect        = 0,           // 근접
            };
            map.BroadcastToAll(hit.Write());

            if (target.Hp <= 0)
            {
                S_EntityDeath death = new S_EntityDeath { entityId = target.EntityId };
                map.BroadcastToAll(death.Write());

                if (target.Kind == EnemyKind.Boss && !map.IsStageCleared)
                {
                    map.SetStageCleared();
                    S_StageClear stageClear = new S_StageClear { bossEntityId = target.EntityId };
                    map.BroadcastToAll(stageClear.Write());
                }
                map.RemoveEnemy(target.EntityId);
                if (target.Kind == EnemyKind.Normal)
                    map.EnqueueRespawn(target);
            }
            else
            {
                // 생존 = HitState(멈칫 + 넉백). Boss는 EnterHitState 내부에서 latch만 세팅.
                float knockbackDir = target.X >= attacker.Position.X ? 1f : -1f;
                target.EnterHitState(knockbackDir);
            }
        }
    }

    /// <summary>
    /// attacker 위치 중심으로 공격 AABB 박스를 생성.
    /// static 순수 함수 — GameMap 상태 의존 X.
    /// Mage면 MageAttackHalfExtent(4.0f), 그 외 AttackHalfExtent(1.5f).
    /// </summary>
    internal static AABB GetAttackHitbox(Vector2 origin, CharacterClass cls)
    {
        float half = cls == CharacterClass.Mage
            ? CombatConstants.MageAttackHalfExtent
            : CombatConstants.AttackHalfExtent;
        return new AABB(origin, new Vector2(half, half));
    }

    /// <summary>
    /// origin 중심 AABB ∩ 살아있는 적 목록 반환.
    ///
    /// P3(단일 평타)는 List에서 첫 1개만 사용. P4(썬더볼트 AoE)가 N개 반환으로 동일 헬퍼 확장 — AoE-ready.
    /// halfExtents: (x, y) 각축 절반 크기. 호출자가 class별/스킬별 값으로 전달.
    /// </summary>
    internal static List<EnemyEntity> ResolveImpactTargets(GameMap map, Vector2 origin, Vector2 halfExtents)
    {
        AABB box = new AABB(origin, halfExtents);
        List<EnemyEntity> results = new();
        foreach (EnemyEntity e in map.Enemies.Values)
        {
            if (!e.IsDead && box.Intersects(e.Hitbox))
                results.Add(e);
        }
        return results;
    }
}
