using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers;

// M3 Phase 03 (헌법 #4 봉합): PacketID → IPacketHandler dispatch 테이블.
//
// **새 핸들러 추가 절차** (02_Server/CLAUDE.md "새 packet handler를 추가할 때" 정합):
//   1. 98_Shared/Protocol/PDL XML에 packet 정의 + 재생성
//   2. Handlers/XxxHandler.cs 신설 (IPacketHandler 구현)
//   3. 본 _handlers Dictionary에 *한 줄* 등록
//   4. 핸들러 단위 테스트: happy + invalid + auth (handshake 미완료) 페어
//
// **Dispatch 패턴 선택 trade-off**:
//   - if-else 체인: 새 핸들러 추가 시 누락 위험 (놓치고 다음 줄 안 박으면 silent drop)
//   - switch 문: 컴파일러 exhaustive 체크 강하지만 새 케이스 추가 = 본문 수정
//   - Dictionary (선택): 데이터 + 코드 분리, 등록 1줄, 핸들러 자체는 독립 단위
//
// **first-packet 게이트는 GameSession.OnRecvPacket 안에서 처리** (handshake 통과 전
// _handshakeCompleted=false 상태에선 C_Handshake만 본 dispatch로 진입, 다른 PacketID는
// 즉시 Disconnect). dispatch 책임 분리 — 핸들러가 lifecycle 만지지 않음.
internal static class HandlerRegistry
{
    static readonly IReadOnlyDictionary<PacketID, IPacketHandler> _handlers =
        new Dictionary<PacketID, IPacketHandler>
        {
            { PacketID.C_Handshake, new HandshakeHandler() },
            { PacketID.C_Ping, new PingHandler() },
            { PacketID.C_MoveIntent, new MoveIntentHandler() },
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
