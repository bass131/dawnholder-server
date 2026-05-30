using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers;

// PacketID → IPacketHandler dispatch 테이블.
// 새 핸들러 추가 절차는 02_Server/CLAUDE.md "새 packet handler를 추가할 때" 참조.
//
// **Dispatch 패턴 선택 trade-off**:
//   - if-else / switch: 새 핸들러 추가 시 누락 위험 또는 본문 수정 필요.
//   - Dictionary (선택): 데이터 + 코드 분리, 등록 1줄, 핸들러 자체는 독립 단위.
//
// **first-packet 게이트는 GameSession.OnRecvPacket 안에서 처리** — 핸들러가 lifecycle 만지지 않음.
internal static class HandlerRegistry
{
    static readonly IReadOnlyDictionary<PacketID, IPacketHandler> _handlers =
        new Dictionary<PacketID, IPacketHandler>
        {
            { PacketID.C_Handshake, new HandshakeHandler() },
            { PacketID.C_Ping, new PingHandler() },
            { PacketID.C_MoveIntent, new MoveIntentHandler() },
            { PacketID.C_Attack, new AttackHandler() },
            { PacketID.C_CharacterSelect, new CharacterSelectHandler() },
            { PacketID.C_EnterPortal,    new EnterPortalHandler() },
        };

    public static bool TryGet(PacketID id, out IPacketHandler handler)
    {
        if (_handlers.TryGetValue(id, out IPacketHandler? found))
        {
            handler = found;
            return true;
        }
        handler = null!;
        return false;
    }
}
