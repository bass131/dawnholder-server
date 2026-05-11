using System.Numerics;
using Dawnholder.Server.GameServer.Sessions;

namespace Dawnholder.Server.GameServer.Maps;

// Phase 02 (M2): 서버 권위 좌표를 가진 플레이어 1명을 표현하는 entity.
// 이번 Phase에선 GameMap 안에 컬렉션으로만 존재 (네트워크 연결 없이 모의 entity OK).
// Phase 03부터 GameSession.OnConnected → AddPlayer 흐름에서 실제 생성.
//
// Position은 System.Numerics.Vector2. Unity의 UnityEngine.Vector2와 메모리 레이아웃은
// 같지만 타입은 다름 — 패킷 직렬화 시 (float x, float y) 두 필드로 풀어서 전송.
public class PlayerEntity
{
    public int EntityId { get; }
    public Vector2 Position { get; set; }
    public GameSession? Owner { get; }

    public PlayerEntity(int entityId, Vector2 position, GameSession? owner = null)
    {
        EntityId = entityId;
        Position = position;
        Owner = owner;
    }
}
