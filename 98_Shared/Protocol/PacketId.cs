namespace Shared.Protocol;

/// <summary>
/// 모든 패킷의 stable한 숫자 ID.
///
/// **헌법 #2 (Protocol is Sacred)** 핵심 룰:
/// - **은퇴한 ID는 절대 재사용 금지** — 옛 클라가 옛 ID로 새 의미를 받으면 보안/싱크 사고.
/// - 기존 패킷에 필드 추가 시 `Protocol.Version` bump (Phase 06+ ProtocolVersion 도입 예정).
/// - 클라/서버는 *같은 enum*을 참조 (`98_Shared/`가 양쪽 공유).
///
/// **범위 예약** (충돌 방지를 위한 사전 약속):
/// <code>
/// 1   ~  999  System (Ping/Pong, Heartbeat, Disconnect 등)
/// 1000~ 1999  Auth   (Login, Logout, TokenRefresh 등 — Phase 07+)
/// 2000~ 2999  Movement (Move, Position, Reconcile 등 — Phase 08+)
/// 3000~ 3999  Combat
/// 4000~ 4999  Inventory
/// 5000~ 5999  Chat
/// ...
/// </code>
///
/// Phase 05는 시스템 패킷(1~999) 첫 두 개만.
/// Phase 06에서 자체 PDL(`99_Tools/PacketGenerator/`)이 이 enum을
/// `Packets.xml` 단일 소스에서 자동 생성 예정 (ADR-002 v2).
/// </summary>
public enum PacketId : ushort
{
    None = 0,

    // ──────────────────────────────
    // 1 ~ 999: System
    // ──────────────────────────────
    Ping = 1,
    Pong = 2,
}
