using System.Numerics;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Combat;


// 서버 소유 entity — owner GameSession 없음 (player와 가장 큰 차이). spawn/mutation/broadcast
// 모두 서버 권위 (헌법 #1).
//
// position은 `float x/y` 두 필드로 박혀 있고 PlayerEntity의 `Vector2 Position`과 다름 —
// `S_EntitySpawn` 패킷이 (x, y) 두 필드로 직렬화하기 때문에 wire format과 1:1로 박으면
// 추후 변환 코드 1단계 절약.
//
// **IsDead derived**: `Hp <= 0`. 음수 보호 자동(예: 데미지 overflow로 Hp=-5여도 IsDead true).
public class EnemyEntity
{
    public int EntityId { get; }
    public EnemyKind Kind { get; }
    public float X { get; set; }
    public float Y { get; set; }
    public int Hp { get; set; }
    public int MaxHp { get; }
    public bool IsDead => Hp <= 0;

    // 서버 권위 스탯 (헌법 #1: 적 스탯도 서버가 결정). struct default = Defense:0 — 무방어 적.
    public EnemyStats Stats { get; }

    // 적 entity의 피격 판정 AABB. 1×1 unit 박스 (center = X/Y, halfExtent = 0.5×0.5).
    public AABB Hitbox => new AABB(new Vector2(X, Y), new Vector2(0.5f, 0.5f));

    // ── AI 상태 필드 ─────────────────────────────────────────────────────────
    // tick thread invariant 안에서만 읽기/쓰기 (GameMap 단일 actor 보장 — lock 불필요).

    /// <summary>현재 AI 상태. Normal = Patrol 시작, Boss = Idle 고정.</summary>
    public EnemyState State { get; set; }

    /// <summary>
    /// Chase 대상 player entityId. null = 타겟 없음 (Patrol/Idle 상태).
    /// Chase 도중 target이 사라지거나 de-aggro 시 null로 초기화 후 Patrol 복귀.
    /// </summary>
    public int? TargetEntityId { get; set; }

    /// <summary>
    /// 스폰 좌표의 X. Patrol 왕복의 중심점.
    /// ctor에서 x 값으로 초기화 — respawn 시 이 좌표로 되돌아옴.
    /// </summary>
    public float SpawnX { get; }

    /// <summary>
    /// 스폰 좌표의 Y. Patrol/Idle 기준 Y.
    /// 이번 scope에서 AI는 X축 수평 이동만 — Y는 고정.
    /// </summary>
    public float SpawnY { get; }

    /// <summary>
    /// 현재 순찰 방향. +1 = 오른쪽, -1 = 왼쪽.
    /// Patrol 경계 닿으면 반전. Chase에서 Patrol 복귀 시에도 유지.
    /// </summary>
    public int PatrolDir { get; set; }

    /// <summary>
    /// Respawn 대기 카운트다운 (tick 단위).
    /// 0 = 살아있음 또는 respawn 대기 없음.
    /// >0 = 죽은 후 카운트다운 중. 매 tick 감소 → 0 도달 시 respawn.
    /// Boss는 respawn 없음 (StageClear 1회성) — 이 필드 불사용.
    /// </summary>
    public int RespawnTicksRemaining { get; set; }

    // 애니메이션 상태 latch 카운터 (tick 단위). PlayerEntity latch 설계와 동일.
    // 우선순위: Death > Hit > Attack > Walk > Idle (적은 Jump 없음).
    // tick thread invariant — EnemyAISystem.Update 안에서만 읽기/쓰기.
    public int AttackLatchTicks { get; set; }    // Attack 상태 남은 latch 틱 수
    public int HitLatchTicks    { get; set; }    // Hit 상태 남은 latch 틱 수

    // State 초기화: Normal → Patrol (AI 즉시 시작), Boss → Idle.
    public EnemyEntity(int entityId, EnemyKind kind, float x, float y, int maxHp, EnemyStats stats = default)
    {
        EntityId = entityId;
        Kind = kind;
        X = x;
        Y = y;
        SpawnX = x;
        SpawnY = y;
        MaxHp = maxHp;
        Hp = maxHp;
        Stats = stats;

        // AI 초기 상태: Boss = Idle (Phase 09 이전), 나머지(Normal/Golem) = Patrol 시작.
        // "적은 2종" 가정 화석 정정: Golem 추가로 Boss 명시 비교 필요 (M4.5-02).
        State = kind == EnemyKind.Boss ? EnemyState.Idle : EnemyState.Patrol;
        PatrolDir = 1; // 기본 오른쪽 출발
    }
}
