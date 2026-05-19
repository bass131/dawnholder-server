using System.Numerics;

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

    public EnemyEntity(int entityId, EnemyKind kind, float x, float y, int maxHp)
    {
        EntityId = entityId;
        Kind = kind;
        X = x;
        Y = y;
        MaxHp = maxHp;
        Hp = maxHp;
    }
}
