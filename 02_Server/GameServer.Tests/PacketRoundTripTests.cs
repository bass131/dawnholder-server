using System;
using System.Buffers.Binary;
using Shared.Protocol;

namespace GameServer.Tests;

/// <summary>
/// 자체 PDL이 자동 생성한 패킷 클래스(98_Shared/Protocol/Generated/GenPackets.cs)의
/// **라운드트립 정합 검증**.
///
/// **왜 이 테스트가 헌법 권고 영역인가** (루트 CLAUDE.md / 02_Server/CLAUDE.md):
/// 게임 서버 도메인에서 *직렬화 깨짐*은 프로덕션에서 발견되는 최악 사례 중 하나.
/// desync / 핵 발견이 1년 후 대량 환불 사건으로 이어지는 클래식 패턴. 본 테스트가
/// PDL.xml 변경 / PacketFormat.cs 템플릿 변경 / BinaryPrimitives endian 정합
/// 회귀를 *commit 시점*에 검출.
///
/// **테스트 패턴**: Write() → bytes → Read() → 같은 값 복원.
/// 새 패킷 추가 시 같은 패턴 따름 (Phase 08+ /new-packet 슬래시 커맨드 자동화 가능).
/// </summary>
public class PacketRoundTripTests
{
    [Fact]
    public void C_Ping_RoundTrip_PreservesClientTimestamp()
    {
        // Arrange: 클라가 Ping 보냄 (Unix epoch ms)
        var ping = new C_Ping { clientTimestampMs = 1234567890123L };

        // Act: 직렬화 → 역직렬화
        ArraySegment<byte> bytes = ping.Write();
        var decoded = new C_Ping();
        decoded.Read(bytes);

        // Assert: 값 보존
        Assert.Equal(1234567890123L, decoded.clientTimestampMs);
    }

    [Fact]
    public void S_Pong_RoundTrip_PreservesBothTimestamps()
    {
        // Arrange: 서버가 Pong 응답 (클라 Ts echo + 서버 Ts)
        var pong = new S_Pong
        {
            clientTimestampMs = 100L,
            serverTimestampMs = 200L
        };

        // Act
        ArraySegment<byte> bytes = pong.Write();
        var decoded = new S_Pong();
        decoded.Read(bytes);

        // Assert: 두 필드 모두 보존
        Assert.Equal(100L, decoded.clientTimestampMs);
        Assert.Equal(200L, decoded.serverTimestampMs);
    }

    [Fact]
    public void C_Ping_Write_ProducesCorrectSizeHeader()
    {
        // [size:2][packetId:2][clientTs:8] = 12 bytes 총.
        // 첫 2바이트가 size를 LittleEndian으로 정확히 박는지.
        var ping = new C_Ping { clientTimestampMs = 0L };

        ArraySegment<byte> bytes = ping.Write();

        Assert.Equal(12, bytes.Count); // 총 크기
        ushort size = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes.Array!, bytes.Offset, 2));
        Assert.Equal(12, size); // 헤더에 박힌 size = 실제 크기
    }

    [Fact]
    public void S_Pong_Write_ProducesCorrectSizeHeader()
    {
        // [size:2][packetId:2][clientTs:8][serverTs:8] = 20 bytes
        var pong = new S_Pong { clientTimestampMs = 0L, serverTimestampMs = 0L };

        ArraySegment<byte> bytes = pong.Write();

        Assert.Equal(20, bytes.Count);
        ushort size = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes.Array!, bytes.Offset, 2));
        Assert.Equal(20, size);
    }

    [Fact]
    public void C_Ping_Write_ProducesCorrectPacketId()
    {
        // bytes[2..4] = packetId (LittleEndian) = 1 (C_Ping = 1)
        var ping = new C_Ping { clientTimestampMs = 0L };

        ArraySegment<byte> bytes = ping.Write();

        ushort packetId = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes.Array!, bytes.Offset + 2, 2));
        Assert.Equal((ushort)PacketID.C_Ping, packetId);
        Assert.Equal((ushort)1, packetId); // PDL.xml 정의 순서 검증
    }

    [Fact]
    public void S_Pong_Write_ProducesCorrectPacketId()
    {
        var pong = new S_Pong { clientTimestampMs = 0L, serverTimestampMs = 0L };

        ArraySegment<byte> bytes = pong.Write();

        ushort packetId = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes.Array!, bytes.Offset + 2, 2));
        Assert.Equal((ushort)PacketID.S_Pong, packetId);
        Assert.Equal((ushort)2, packetId); // PDL.xml 두 번째 정의
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(-1L)]                // 음수 값
    [InlineData(long.MaxValue)]      // 64-bit 경계
    [InlineData(long.MinValue)]      // 음수 경계
    [InlineData(1748563200000L)]     // 2025-05-30 UTC 근처 실제 timestamp
    public void C_Ping_RoundTrip_HandlesEdgeValues(long value)
    {
        // 다양한 long 값에 대해 라운드트립 정합 보장.
        // BinaryPrimitives.*LittleEndian이 *음수 값 / 경계 값*도 정확한지 검증.
        var ping = new C_Ping { clientTimestampMs = value };

        ArraySegment<byte> bytes = ping.Write();
        var decoded = new C_Ping();
        decoded.Read(bytes);

        Assert.Equal(value, decoded.clientTimestampMs);
    }

    [Fact]
    public void Write_UsesLittleEndianForTimestamp()
    {
        // 명시적 endianness 검증: clientTimestampMs = 1L → LE 바이트 패턴 [01 00 00 00 00 00 00 00].
        // BitConverter(호스트 endian)가 아닌 BinaryPrimitives.*LittleEndian 명시인지.
        var ping = new C_Ping { clientTimestampMs = 1L };

        ArraySegment<byte> bytes = ping.Write();

        // bytes[4..12] = clientTimestampMs payload
        // LittleEndian: LSB가 먼저 → bytes[4] = 0x01, bytes[5..11] = 0x00
        Assert.Equal(0x01, bytes.Array![bytes.Offset + 4]);
        for (int i = 5; i < 12; i++)
            Assert.Equal(0x00, bytes.Array[bytes.Offset + i]);
    }
}
