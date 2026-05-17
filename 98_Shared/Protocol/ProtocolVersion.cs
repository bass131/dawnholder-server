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
///
/// **다음 단계** (별도 Phase 후보, 본 Phase 07 범위 밖):
///   - 핸드셰이크에 클라/서버 버전 비교 박기 — mismatch 시 즉시 disconnect + 명확한 에러 코드
///     (98_Shared/CLAUDE.md "Protocol 버전 핸드셰이크" 섹션 박혀있음).
///   - 라이브 게임 표준: 옛 클라 차단 + 강제 업데이트 유도. 핵/취약점 노출 차단.
///
/// **타입 ushort 이유**: 4 byte uint은 과잉, 1 byte byte는 256 버전 한계로 부족할 수 있어 2 byte ushort.
/// 65535 버전이면 12년간 매일 bump해도 안 떨어짐.
/// </summary>
public static class ProtocolVersion
{
    /// <summary>현재 프로토콜 버전. Phase 07 = v2.</summary>
    public const ushort Current = 2;
}
