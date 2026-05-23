namespace Shared.Protocol;

/// <summary>
/// 와이어 프로토콜 버전. 패킷 모양이 바뀔 때마다 bump (헌법 #2 "Protocol is Sacred").
///
/// **자리잡이 위치 활용** (98_Shared/CLAUDE.md Layout 표에 박혀있던 `(예정 — Phase M2+ 핸드셰이크)`
/// 자리에 Phase 07 D3 결정으로 박음):
///
/// **버전 이력**:
///   - v1: M2 Phase 04~06 — C_MoveIntent (sbyte inputX), S_Snapshot (x/y만).
///   - v2: M2 Phase 07 — C_MoveIntent (byte input 비트필드), S_Snapshot (vx/vy 추가).
///         InputBits 헬퍼 신설, jumpPressed 에지 패턴(D4).
///   - v3: M3 Phase 06 — Combat 4패킷 (C_Attack/S_EntitySpawn/S_HitResult/S_EntityDeath).
///         additive지만 데모 기능이 신규 패킷 의존이라 stale client 빠른 cutoff 위해 bump
///         (Codex β 사전 검증 MEDIUM #4, handshake exact equality 정합).
///   - v4: M3.8 Phase 03 — C_CharacterSelect (캐릭터 선택, 전사/원거리 분기).
///         backward compatible append-only지만 옛 빌드 클라가 캐릭터 선택 안 보내고 EnterGameWorld
///         시도하면 default stats 미박힘 → server-side null 처리 사고 차단 위해 bump.
///   - v5: M4.1 Phase 06 — C_Attack.attackerClientTick 추가 (lag compensation rewind).
///         backward compatible append-only지만 옛 클라가 tick 안 보내면 attackerClientTick=0 →
///         서버 rewind 범위 검증에서 silent drop 가능 → 빠른 cutoff 위해 bump.
///
/// **핸드셰이크 봉합 (M3 Phase 02 완료, 2026-05-18)**:
///   - C_Handshake { clientVersion } / S_HandshakeResult { ok, serverVersion, reason } 신설 (PDL).
///   - GameSession.OnRecvPacket first-packet 강제 — handshake 외 첫 패킷은 즉시 Disconnect.
///   - clientVersion == Current → ok=true + EnterGameWorld (AddPlayer).
///   - clientVersion != Current → ok=false + reason 박고 즉시 Disconnect (헌법 #3 정합 — timeout 안 기다림).
///   - 호환 가능 minor version 호환표는 응급 모드 범위 밖 — 본 마감 시 별도 Phase.
///
/// **타입 ushort 이유**: 4 byte uint은 과잉, 1 byte byte는 256 버전 한계로 부족할 수 있어 2 byte ushort.
/// 65535 버전이면 12년간 매일 bump해도 안 떨어짐.
/// </summary>
public static class ProtocolVersion
{
    /// <summary>현재 프로토콜 버전. M4.1 Phase 06 = v5 (C_Attack.attackerClientTick 추가, lag compensation).</summary>
    public const ushort Current = 5;
}
