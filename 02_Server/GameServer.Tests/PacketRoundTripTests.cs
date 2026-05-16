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

    // ──────────────────────────────────────────────────────────────────
    // Phase 03 (M2): S_EnterMap / S_LeaveMap 라운드트립.
    //
    // 접속 핸드셰이크 패킷의 wire format 회귀 가드. spawnX/spawnY는 float이라
    // .NET Standard 2.1 호환 경유(SingleToInt32Bits) 경로를 같이 검증.
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public void S_EnterMap_RoundTrip_PreservesAllFields()
    {
        var pkt = new S_EnterMap { entityId = 7, spawnX = 3.0f, spawnY = -1.5f };

        ArraySegment<byte> bytes = pkt.Write();
        var decoded = new S_EnterMap();
        decoded.Read(bytes);

        Assert.Equal(7, decoded.entityId);
        Assert.Equal(3.0f, decoded.spawnX);
        Assert.Equal(-1.5f, decoded.spawnY);
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(3.0f, 0f)]            // Phase 03 검증 단계 spawn 좌표
    [InlineData(-100f, 100f)]
    [InlineData(float.MaxValue, float.MinValue)]
    public void S_EnterMap_RoundTrip_HandlesFloatEdgeValues(float x, float y)
    {
        var pkt = new S_EnterMap { entityId = 1, spawnX = x, spawnY = y };

        ArraySegment<byte> bytes = pkt.Write();
        var decoded = new S_EnterMap();
        decoded.Read(bytes);

        Assert.Equal(x, decoded.spawnX);
        Assert.Equal(y, decoded.spawnY);
    }

    [Fact]
    public void S_EnterMap_Write_ProducesCorrectSizeHeader()
    {
        // [size:2][id:2][entityId:4][spawnX:4][spawnY:4] = 16 bytes.
        var pkt = new S_EnterMap { entityId = 0, spawnX = 0f, spawnY = 0f };

        ArraySegment<byte> bytes = pkt.Write();

        Assert.Equal(16, bytes.Count);
        ushort size = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes.Array!, bytes.Offset, 2));
        Assert.Equal(16, size);
    }

    [Fact]
    public void S_EnterMap_Write_ProducesCorrectPacketId()
    {
        // PDL.xml 3번째 정의 = PacketID 3
        var pkt = new S_EnterMap { entityId = 0, spawnX = 0f, spawnY = 0f };

        ArraySegment<byte> bytes = pkt.Write();

        ushort packetId = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes.Array!, bytes.Offset + 2, 2));
        Assert.Equal((ushort)PacketID.S_EnterMap, packetId);
        Assert.Equal((ushort)3, packetId);
    }

    [Fact]
    public void S_LeaveMap_RoundTrip_PreservesEntityId()
    {
        var pkt = new S_LeaveMap { entityId = 42 };

        ArraySegment<byte> bytes = pkt.Write();
        var decoded = new S_LeaveMap();
        decoded.Read(bytes);

        Assert.Equal(42, decoded.entityId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void S_LeaveMap_RoundTrip_HandlesIntEdgeValues(int id)
    {
        var pkt = new S_LeaveMap { entityId = id };

        ArraySegment<byte> bytes = pkt.Write();
        var decoded = new S_LeaveMap();
        decoded.Read(bytes);

        Assert.Equal(id, decoded.entityId);
    }

    [Fact]
    public void S_LeaveMap_Write_ProducesCorrectSizeHeader()
    {
        // [size:2][id:2][entityId:4] = 8 bytes.
        var pkt = new S_LeaveMap { entityId = 0 };

        ArraySegment<byte> bytes = pkt.Write();

        Assert.Equal(8, bytes.Count);
        ushort size = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes.Array!, bytes.Offset, 2));
        Assert.Equal(8, size);
    }

    [Fact]
    public void S_LeaveMap_Write_ProducesCorrectPacketId()
    {
        // PDL.xml 4번째 정의 = PacketID 4
        var pkt = new S_LeaveMap { entityId = 0 };

        ArraySegment<byte> bytes = pkt.Write();

        ushort packetId = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes.Array!, bytes.Offset + 2, 2));
        Assert.Equal((ushort)PacketID.S_LeaveMap, packetId);
        Assert.Equal((ushort)4, packetId);
    }

    // ──────────────────────────────────────────────────────────────────
    // Phase 04 (M2): C_MoveIntent / S_Snapshot 라운드트립.
    //
    // 본 묶음의 *주된 의도*는 Phase 04에서 PacketGenerator의 byte/sbyte
    // 템플릿(`PacketFormat.cs` ReadByteFormat/WriteByteFormat)이 옛
    // ServerDev 잔재(`Segment.Array[Offset+count]`)에서 신 메서드의 `s[count]`
    // 패턴으로 정정된 것에 대한 *회귀 안전망*.
    //
    // sbyte 음수 경계가 (byte) 캐스트 왕복에서 살아남는지를 특히 확인.
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public void C_MoveIntent_RoundTrip_PreservesFields()
    {
        var intent = new C_MoveIntent { inputX = -1, clientTick = 12345 };

        ArraySegment<byte> bytes = intent.Write();
        var decoded = new C_MoveIntent();
        decoded.Read(bytes);

        Assert.Equal((sbyte)-1, decoded.inputX);
        Assert.Equal(12345u, decoded.clientTick);
    }

    [Theory]
    [InlineData((sbyte)-1)]       // 좌 (정상 사용)
    [InlineData((sbyte)0)]        // 정지 (정상 사용)
    [InlineData((sbyte)1)]        // 우 (정상 사용)
    [InlineData(sbyte.MinValue)]  // -128: byte 캐스트 왕복 경계
    [InlineData(sbyte.MaxValue)]  // 127: 양수 경계
    [InlineData((sbyte)-2)]       // cheat 범위 (HandleMoveIntent가 폐기) — 직렬화는 정확해야 함
    public void C_MoveIntent_RoundTrip_HandlesSByteEdgeValues(sbyte value)
    {
        // sbyte → byte 캐스트(WriteByteFormat) → 다시 sbyte 캐스트(ReadByteFormat) 왕복.
        // -1 같은 음수가 0xFF로 박힌 뒤 0xFF가 다시 -1로 복원돼야 함.
        // 본 테스트가 깨지면 = PacketGenerator의 byte/sbyte 패턴이 회귀한 것.
        var intent = new C_MoveIntent { inputX = value, clientTick = 0 };

        ArraySegment<byte> bytes = intent.Write();
        var decoded = new C_MoveIntent();
        decoded.Read(bytes);

        Assert.Equal(value, decoded.inputX);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(1u)]
    [InlineData((uint)int.MaxValue)]  // 21억 — signed/unsigned 경계
    [InlineData(uint.MaxValue)]       // 42억 — uint 최대 (wrap 직전)
    // 음수 케이스는 Phase 06에서 uint으로 정합 — 컴파일러가 원천 차단. 옛 리뷰 🟡 해결.
    public void C_MoveIntent_RoundTrip_HandlesClientTickEdgeValues(uint tick)
    {
        var intent = new C_MoveIntent { inputX = 0, clientTick = tick };

        ArraySegment<byte> bytes = intent.Write();
        var decoded = new C_MoveIntent();
        decoded.Read(bytes);

        Assert.Equal(tick, decoded.clientTick);
    }

    [Fact]
    public void C_MoveIntent_Write_ProducesCorrectSizeHeader()
    {
        // [size:2][id:2][inputX:1][clientTick:4] = 9 bytes 총.
        var intent = new C_MoveIntent { inputX = 1, clientTick = 0 };

        ArraySegment<byte> bytes = intent.Write();

        Assert.Equal(9, bytes.Count);
        ushort size = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes.Array!, bytes.Offset, 2));
        Assert.Equal(9, size);
    }

    [Fact]
    public void C_MoveIntent_Write_ProducesCorrectPacketId()
    {
        // PDL.xml 5번째 정의 = PacketID 5
        var intent = new C_MoveIntent { inputX = 0, clientTick = 0 };

        ArraySegment<byte> bytes = intent.Write();

        ushort packetId = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes.Array!, bytes.Offset + 2, 2));
        Assert.Equal((ushort)PacketID.C_MoveIntent, packetId);
        Assert.Equal((ushort)5, packetId);
    }

    [Fact]
    public void S_Snapshot_RoundTrip_PreservesAllFields()
    {
        var snap = new S_Snapshot
        {
            entityId = 42,
            x = 1.25f,           // GameMap.Tick 1회 = MoveSpeed(5) * TickDuration(0.05) = 0.25, 5tick 누적 = 1.25
            y = -3.5f,
            serverTick = 1000,
            lastAckedClientTick = 999
        };

        ArraySegment<byte> bytes = snap.Write();
        var decoded = new S_Snapshot();
        decoded.Read(bytes);

        Assert.Equal(42, decoded.entityId);
        Assert.Equal(1.25f, decoded.x);
        Assert.Equal(-3.5f, decoded.y);
        Assert.Equal(1000, decoded.serverTick);
        Assert.Equal(999u, decoded.lastAckedClientTick);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(1.25f)]            // 5 tick × MoveSpeed × TickDuration
    [InlineData(-1.25f)]
    [InlineData(float.MaxValue)]
    [InlineData(float.MinValue)]
    [InlineData(float.Epsilon)]    // 가장 작은 양수 — IEEE 754 비정규수 경계
    public void S_Snapshot_RoundTrip_HandlesFloatEdgeValues(float coord)
    {
        // float 직렬화는 .NET Standard 2.1 호환을 위해
        // BitConverter.SingleToInt32Bits / Int32BitsToSingle 경유 (PacketFormat.cs WriteFloatFormat).
        // 본 테스트가 깨지면 = float 직렬화 경로가 회귀한 것.
        // NaN은 자기 자신과 != 이므로 제외 — Equal 비교 자체가 깨짐.
        var snap = new S_Snapshot { entityId = 0, x = coord, y = coord, serverTick = 0, lastAckedClientTick = 0 };

        ArraySegment<byte> bytes = snap.Write();
        var decoded = new S_Snapshot();
        decoded.Read(bytes);

        Assert.Equal(coord, decoded.x);
        Assert.Equal(coord, decoded.y);
    }

    [Fact]
    public void S_Snapshot_Write_ProducesCorrectSizeHeader()
    {
        // [size:2][id:2][entityId:4][x:4][y:4][serverTick:4][lastAckedClientTick:4] = 24 bytes.
        var snap = new S_Snapshot { entityId = 0, x = 0f, y = 0f, serverTick = 0, lastAckedClientTick = 0 };

        ArraySegment<byte> bytes = snap.Write();

        Assert.Equal(24, bytes.Count);
        ushort size = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes.Array!, bytes.Offset, 2));
        Assert.Equal(24, size);
    }

    [Fact]
    public void S_Snapshot_Write_ProducesCorrectPacketId()
    {
        // PDL.xml 6번째 정의 = PacketID 6
        var snap = new S_Snapshot { entityId = 0, x = 0f, y = 0f, serverTick = 0, lastAckedClientTick = 0 };

        ArraySegment<byte> bytes = snap.Write();

        ushort packetId = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes.Array!, bytes.Offset + 2, 2));
        Assert.Equal((ushort)PacketID.S_Snapshot, packetId);
        Assert.Equal((ushort)6, packetId);
    }
}
