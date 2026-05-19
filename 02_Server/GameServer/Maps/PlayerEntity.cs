using System.Numerics;
using Dawnholder.Server.GameServer.Sessions;

namespace Dawnholder.Server.GameServer.Maps;

// Phase 02 (M2): 서버 권위 좌표를 가진 플레이어 1명을 표현하는 entity.
// Phase 03: GameMap.AddPlayer로 생성, OnDisconnected 시 RemovePlayer.
// Phase 04: 이동 intent 누적 필드 추가. tick thread에서만 mutate.
// M3 Phase 06 (응급 전투 Step 1): 전투 상태(Hp/MaxHp/IsDead) + rate-limit 타임스탬프 추가.
//   - 헌법 #1 (Server Authority): Hp는 *서버만* mutate. 클라는 S_HitResult로 받은 값을 표시만.
//   - 응집도 trade-off: combat state(Hp)를 movement state(Position/Velocity)와 같은 entity에
//     박을지 분리할지 — 응급은 같은 entity에 박음(*combat 컴포넌트 분리는 M4+*).
//   - LastAttackTickMs는 Step 5(AttackHandler rate-limit, 500ms silent drop)에서 검사 예정,
//     본 Step에선 0으로 초기화만.
//
// Position은 System.Numerics.Vector2. Unity의 UnityEngine.Vector2와 메모리 레이아웃은
// 같지만 타입은 다름 — 패킷 직렬화 시 (float x, float y) 두 필드로 풀어서 전송.
public class PlayerEntity
{
    public int EntityId { get; }
    public Vector2 Position { get; set; }
    public GameSession? Owner { get; }

    // Phase 07: 결정론 물리 상태 — Shared.GameData.Physics.Step이 매 tick mutation.
    // spawn 시점 Velocity=0 + OnGround=true (ground y=0 가정).
    public Vector2 Velocity { get; set; } = Vector2.Zero;
    public bool OnGround { get; set; } = true;

    // Phase 04: 다음 tick에 적용할 입력. 단일 thread(tick) mutation 보장 +
    // OnRecvPacket이 EnqueueJob으로 set하므로 동시성 안전.
    // Phase 07: jumpPressed 추가 (D4 (a) 클라 에지 — 1tick만 true).
    public sbyte PendingInputX { get; set; }
    public bool PendingJumpPressed { get; set; }
    public uint LastClientTick { get; set; }

    // M3 Phase 06 Step 1 (combat state): 응급 전투 HP. 헌법 #1 — 서버만 mutate.
    // 기본 100/100. `IsDead`는 derived: `Hp <= 0`. 음수 보호는 derived가 흡수
    // (`Hp = -5` 직접 set도 IsDead true이므로 후속 attack job이 idempotent하게 no-op).
    public int Hp { get; set; } = 100;
    public int MaxHp { get; set; } = 100;
    public bool IsDead => Hp <= 0;

    // M3 Phase 06 Step 1 (rate-limit hook): 마지막 공격 발생 tick(ms 단위) 기록.
    // Step 5에서 `AttackHandler`가 (now - LastAttackTickMs >= 500ms) 검사로 silent drop 판정.
    // 본 Step에선 필드 박힘만 — 갱신/검사 로직은 Step 5에서 추가.
    public long LastAttackTickMs { get; set; }

    public PlayerEntity(int entityId, Vector2 position, GameSession? owner = null)
    {
        EntityId = entityId;
        Position = position;
        Owner = owner;
    }
}
