namespace Dawnholder.Server.GameServer.Combat;

// 적의 종류 식별자. byte로 박아 S_EntitySpawn 패킷의 entityKind 필드와 1:1 매핑.
//
// stability 약속: 값은 영원히 고정. 새 종류는 *append-only* (Normal=0, Boss=1, 다음 = 2).
// 클라/서버 양쪽이 같은 byte 표를 봐야 하므로 enum 자체는 server-only지만 wire값 = byte.
public enum EnemyKind : byte
{
    Normal = 0,
    Boss = 1,
}
