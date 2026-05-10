using System;
using System.Buffers.Binary;

namespace Shared.Protocol;

/// <summary>
/// 서버 → 클라. Ping의 ClientTimestampMs를 그대로 echo + 서버 ServerTimestampMs 추가.
/// 클라가 RTT(`now - clientTs`)와 한쪽 latency 추정(`(now-clientTs)/2`)에 사용.
///
/// **와이어 포맷** (20 bytes total):
/// <code>
/// [size:2][packetId:2][clientTimestampMs:8][serverTimestampMs:8]
/// </code>
///
/// 나머지 룰은 PingPacket과 동일 (little-endian 명시, Phase 06 PDL로 교체 예정).
/// </summary>
public class PongPacket
{
    public const ushort Id = (ushort)PacketId.Pong;
    public const ushort PacketSize = 2 + 2 + 8 + 8; // 20 bytes

    /// <summary>클라가 보낸 Ping의 timestamp 그대로 echo.</summary>
    public long ClientTimestampMs;

    /// <summary>서버 처리 시점의 Unix epoch ms. 클라가 한쪽 latency 추정 시 사용.</summary>
    public long ServerTimestampMs;

    public void Read(ArraySegment<byte> buffer)
    {
        ReadOnlySpan<byte> span = buffer.AsSpan();
        ClientTimestampMs = BinaryPrimitives.ReadInt64LittleEndian(span.Slice(4, 8));
        ServerTimestampMs = BinaryPrimitives.ReadInt64LittleEndian(span.Slice(12, 8));
    }

    public byte[] ToBytes()
    {
        byte[] buffer = new byte[PacketSize];
        Span<byte> span = buffer.AsSpan();
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(0, 2), PacketSize);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(2, 2), Id);
        BinaryPrimitives.WriteInt64LittleEndian(span.Slice(4, 8), ClientTimestampMs);
        BinaryPrimitives.WriteInt64LittleEndian(span.Slice(12, 8), ServerTimestampMs);
        return buffer;
    }
}
