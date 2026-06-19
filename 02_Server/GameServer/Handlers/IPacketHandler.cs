using Dawnholder.Server.GameServer.Sessions;

namespace Dawnholder.Server.GameServer.Handlers;

// **책임 분리**:
//   handler = decode + 검증 + session 캡슐화 메서드 호출만.
//   lifecycle state (_handshakeCompleted / _entityId / rate-limit window)는
//   GameSession 안에 그대로 — 외부에서 직접 만지지 않음.
//
// **헌법 #3 정합**: 모든 핸들러는 untrusted input을 다룬다 — 범위 검증 + 정규화 필수.
internal interface IPacketHandler
{
    // class 선택 전제조건 — dispatch 일괄 게이트가 사용 (Handle 전 미선택 시 silent drop).
    // default 구현 없음: 새 핸들러가 누락하면 컴파일 에러 → 게이트 누락 구조적 불가화 (헌법 #3).
    bool RequiresSelectedClass { get; }

    void Handle(GameSession session, ArraySegment<byte> buffer);
}
