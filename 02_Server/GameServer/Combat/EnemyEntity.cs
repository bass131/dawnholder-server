using System.Numerics;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Combat;


// M3 Phase 06 Step 2 (응급 전투 인프라):
// 서버 소유 entity — owner GameSession 없음 (player와 가장 큰 차이). spawn/mutation/broadcast
// 모두 서버 권위 (헌법 #1). 본 Step은 모델 + spawn만 — AI/이동/사망 broadcast는 Step 3+.
//
// **응급 단순화**: AI 없음, 고정 위치, 패시브 dummy. position은 `float x/y` 두 필드로 박혀
// 있고 PlayerEntity의 `Vector2 Position`과 다름 — `S_EntitySpawn` 패킷이 (x, y) 두 필드로
// 직렬화하기 때문에 wire format과 1:1로 박으면 추후 변환 코드 1단계 절약. (player처럼 movement
// 도입되면 Vector2로 승격 가능 — M4+ backlog.)
//
// **IsDead derived**: `Hp <= 0`. 음수 보호 자동(예: 데미지 overflow로 Hp=-5여도 IsDead true).
// 본 Step에선 spawn 시 Hp = MaxHp = 30 박힘 (Phase 정의 박제). 데미지 인입은 Step 5.
//
// M4.3 Phase 07: AI 상태 필드 추가 (FSM 필드).
//   Normal enemy는 Patrol 시작. Boss는 Idle 고정 (Phase 09에서 별도 behavior).
//   SpawnX = 스폰 위치 기준점 (patrol 왕복 중심).
//   PatrolDir = 현재 순찰 방향 (+1 = 오른쪽, -1 = 왼쪽).
//   TargetEntityId = Chase 대상 player entityId (null = 타겟 없음).
//   RespawnTicksRemaining = 0이면 살아있음, >0이면 respawn 대기 카운트다운.
public class EnemyEntity
{
    public int EntityId { get; }
    public EnemyKind Kind { get; }
    public float X { get; set; }
    public float Y { get; set; }
    public int Hp { get; set; }
    public int MaxHp { get; }
    public bool IsDead => Hp <= 0;

    // M4.1 Phase 05 (2단계): 서버 권위 스탯.
    // struct default = Defense:0 — 응급 무방어 적 표현 (헌법 #1: 적 스탯도 서버가 결정).
    // M4+ 몬스터 테이블 도입 시 ctor에서 테이블 룩업 스탯으로 교체 backlog.
    public EnemyStats Stats { get; }

    // M4.1 Phase 06 (5단계): 적 entity의 피격 판정 AABB.
    // 응급 = 1×1 unit 박스 (center = X/Y, halfExtent = 0.5×0.5).
    // EnemyEntity는 float X/Y로 위치 관리 → new Vector2(X, Y)로 변환.
    // M4+ 몬스터 테이블 도입 시 hitbox 크기도 테이블에서 로드 backlog.
    public AABB Hitbox => new AABB(new Vector2(X, Y), new Vector2(0.5f, 0.5f));

    // ── M4.3 Phase 07: AI 상태 필드 ─────────────────────────────────────────
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

    // M4.3 Phase 08a: 애니메이션 상태 latch 카운터 (tick 단위).
    // PlayerEntity latch 설계와 동일 — Attack/Hit는 1틱 이벤트라 최소 8틱 유지.
    // 우선순위: Death > Hit > Attack > Walk > Idle (적은 Jump 없음).
    // tick thread invariant — EnemyAISystem.Update 안에서만 읽기/쓰기.
    public int AttackLatchTicks { get; set; }    // Attack 상태 남은 latch 틱 수
    public int HitLatchTicks    { get; set; }    // Hit 상태 남은 latch 틱 수

    // M4.1 Phase 05 (2단계): stats 옵션 인자 추가. 옛 시그니처 (entityId, kind, x, y, maxHp) 보존 —
    // 기존 SpawnNormalEnemy/SpawnBoss 호출지 변경 X (default 인자 패턴).
    //
    // M4.3 Phase 07: Normal enemy는 NormalDefault() stats로 AI 파라미터 포함.
    // Boss는 default stats (AI 파라미터 = 0) — Phase 09에서 별도 처리.
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

        // AI 초기 상태: Normal = Patrol 시작, Boss = Idle (Phase 09 이전).
        State = kind == EnemyKind.Normal ? EnemyState.Patrol : EnemyState.Idle;
        PatrolDir = 1; // 기본 오른쪽 출발
    }
}
