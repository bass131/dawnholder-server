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

    // M4.1 Phase 05 (2단계): stats 옵션 인자 추가. 옛 시그니처 (entityId, kind, x, y, maxHp) 보존 —
    // 기존 SpawnNormalEnemy/SpawnBoss 호출지 변경 X (default 인자 패턴).
    public EnemyEntity(int entityId, EnemyKind kind, float x, float y, int maxHp, EnemyStats stats = default)
    {
        EntityId = entityId;
        Kind = kind;
        X = x;
        Y = y;
        MaxHp = maxHp;
        Hp = maxHp;
        Stats = stats;
    }
}
