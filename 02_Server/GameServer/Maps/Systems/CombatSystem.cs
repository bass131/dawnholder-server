using System.Collections.Generic;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps.Actions;
using Dawnholder.Server.GameServer.Entities;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps.Systems;

/// <summary>
/// §2.2 CombatSystem — GameMap(컨테이너)에서 전투 로직 추출.
///
/// **단일 책임**: 공격 1건 처리를 ActionGate 단일 입구에 위임.
/// **호출 규율(§1.1)**: GameMap.Tick 안에서만 호출 (EnqueueJob 람다 경유).
/// **데이터 소유**: GameMap이 소유. CombatSystem은 map을 인자로 받아 읽기·변경만.
/// **System 간 직접 호출 X(§2.2)**: RespawnSystem 큐 등록은 map.EnqueueRespawn() 경유.
/// </summary>
internal sealed class CombatSystem
{
    readonly ActionGate _gate = new();

    /// <summary>
    /// tick thread 안에서 attack 1건 처리. ActionGate.TryPerform에 위임.
    ///
    /// **검증 순서(헌법 #3)**: attacker 존재 확인 후 ActionGate가 ①상태 ②쿨다운 ③클래스 ④rewind 전부 검사.
    /// </summary>
    internal void ProcessAttack(GameMap map, int attackerEntityId, int targetEntityId, long attackerClientTick)
    {
        PlayerEntity? attacker = map.GetPlayer(attackerEntityId);
        if (attacker == null) return;

        _gate.TryPerform(map, attacker, ActionKind.Melee, new ActionContext(attackerClientTick, targetEntityId, 0));
    }

    /// <summary>
    /// attacker 위치 중심으로 공격 AABB 박스를 생성.
    /// static 순수 함수 — GameMap 상태 의존 X.
    /// 클래스별 X/Y half-extent 분리(Phase 02):
    ///   Mage: X=MageAttackHalfX(11.0f), Y=MageAttackHalfY(1.0f)
    ///   Knight: X=KnightAttackHalfX(1.5f), Y=KnightAttackHalfY(1.0f)
    /// Y를 X보다 좁게 설정해 사이드스크롤 위/아래 층 오판정 제거.
    /// </summary>
    internal static AABB GetAttackHitbox(Vector2 origin, CharacterClass cls)
    {
        (float halfX, float halfY) = cls == CharacterClass.Mage
            ? (CombatConstants.MageAttackHalfX, CombatConstants.MageAttackHalfY)
            : (CombatConstants.KnightAttackHalfX, CombatConstants.KnightAttackHalfY);
        return new AABB(origin, new Vector2(halfX, halfY));
    }

    /// <summary>
    /// rewind(lag-comp) 범위 검증 (헌법 §3 Trust Boundary). 3분기 — 음수/미래/상한 초과면 false(reject).
    /// ProcessAttack + SkillSystem 3개 Process*가 공유. 부등호·경계 1:1 보존(거동 불변).
    /// </summary>
    internal static bool ValidateRewind(long clientTick, long serverTick)
    {
        if (clientTick < 0) return false;                              // (a) 음수
        if (clientTick > serverTick) return false;                    // (b) 미래
        if (serverTick - clientTick > CombatConstants.MaxRewindTicks) return false; // (c) 상한 초과
        return true;
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
