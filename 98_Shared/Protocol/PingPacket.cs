using System;
using System.Buffers.Binary;

namespace Shared.Protocol;

/// <summary>
/// 클라 → 서버. 클라 timestamp만 실어 보냄 (RTT 측정용).
///
/// **와이어 포맷** (12 bytes total):
/// <code>
/// [size:2][packetId:2][clientTimestampMs:8]
/// </code>
///
/// **little-endian 명시** — 게임 wire format은 *플랫폼 무관 약속*이라야 함.
/// `BitConverter`는 호스트 endian 따르므로 사용 X. `BinaryPrimitives.*LittleEndian` 명시.
///
/// **Phase 05 임시 구현** — Phase 06에서 자체 PDL(`99_Tools/PacketGenerator/`)이
/// 본 클래스 통째를 자동 생성된 코드로 교체 예정 (ADR-002 v2). 임시 코드의
/// 신호: 교체 시 *다른 시스템에 파급 0* (필드 1개, 동작 검증 끝남).
/// </summary>
public class PingPacket
{
    public const ushort Id = (ushort)PacketId.Ping;
    public const ushort PacketSize = 2 + 2 + 8; // 12 bytes

    /// <summary>클라 송신 시점의 Unix epoch ms. 서버는 그대로 echo, 클라가 RTT 계산.</summary>
    public long ClientTimestampMs;

    /// <summary>
    /// PacketSession이 *완전한 한 패킷*(header 포함)을 잘라 넘긴 buffer를 읽음.
    /// buffer[0..2] = size, buffer[2..4] = id (이미 dispatch에서 검증).
    /// </summary>
    public void Read(ArraySegment<byte> buffer)
    {
        ReadOnlySpan<byte> span = buffer.AsSpan();
        ClientTimestampMs = BinaryPrimitives.ReadInt64LittleEndian(span.Slice(4, 8));
    }

    /// <summary>[size][id][payload]를 한 번에 박은 byte[]를 반환. 송신측이 Send에 넘김.</summary>
    public byte[] ToBytes()
    {
        byte[] buffer = new byte[PacketSize];
        Span<byte> span = buffer.AsSpan();
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(0, 2), PacketSize);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(2, 2), Id);
        BinaryPrimitives.WriteInt64LittleEndian(span.Slice(4, 8), ClientTimestampMs);
        return buffer;
    }
}
