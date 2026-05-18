using Dawnholder.Server.GameServer.Sessions;

namespace Dawnholder.Server.GameServer.Handlers;

// M3 Phase 03 (헌법 #4 가짜 약속 2번째 봉합): 핸들러 layer 분리.
//
// **책임 분리 (Codex 인사이트, Phase 02 review)**:
//   handler = decode + 검증 + session 캡슐화 메서드 호출만.
//   lifecycle state (_handshakeCompleted / _entityId / rate-limit window)는
//   GameSession 안에 그대로 — 외부에서 직접 만지지 않음.
//
// **헌법 #3 정합**: 모든 핸들러는 untrusted input을 다룬다 — 범위 검증 + 정규화 필수.
internal interface IPacketHandler
{
    void Handle(GameSession session, ArraySegment<byte> buffer);
}
