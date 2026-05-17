using System.Numerics;
using Dawnholder.Server.GameServer.Sessions;

namespace Dawnholder.Server.GameServer.Maps;

// Phase 02 (M2): 서버 권위 좌표를 가진 플레이어 1명을 표현하는 entity.
// Phase 03: GameMap.AddPlayer로 생성, OnDisconnected 시 RemovePlayer.
// Phase 04: 이동 intent 누적 필드 추가. tick thread에서만 mutate.
//
// Position은 System.Numerics.Vector2. Unity의 UnityEngine.Vector2와 메모리 레이아웃은
// 같지만 타입은 다름 — 패킷 직렬화 시 (float x, float y) 두 필드로 풀어서 전송.
public class PlayerEntity
{
    public int EntityId { get; }
    public Vector2 Position { get; set; }
    public GameSession? Owner { get; }

    // Phase 04: 다음 tick에 적용할 입력. 단일 thread(tick) mutation 보장 +
    // OnRecvPacket이 EnqueueJob으로 set하므로 동시성 안전.
    // Phase 07: jumpPressed 추가 (D4 (a) 클라 에지 — 1tick만 true).
    public sbyte PendingInputX { get; set; }
    public bool PendingJumpPressed { get; set; }
    public uint LastClientTick { get; set; }

    public PlayerEntity(int entityId, Vector2 position, GameSession? owner = null)
    {
        EntityId = entityId;
        Position = position;
        Owner = owner;
    }
}
