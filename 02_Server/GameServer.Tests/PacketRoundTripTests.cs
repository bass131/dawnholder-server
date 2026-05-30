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
    // S_EnterMap / S_LeaveMap 라운드트립.
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
    [InlineData(3.0f, 0f)]            // 대표 spawn 좌표
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
    // C_MoveIntent / S_Snapshot 라운드트립.
    // inputX는 byte input 비트필드 (InputBits 단일 출처).
    // S_Snapshot에 vx/vy 포함 (prediction velocity 동기화).
    //
    // 본 묶음의 의도: PacketGenerator의 byte/uint/float 템플릿 *wire round-trip* 회귀 안전망.
    // 비트필드 의미(인코딩 매핑) 자체는 InputBitsTests가 검증 — 본 테스트는 byte 그대로 보존만.
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public void C_MoveIntent_RoundTrip_PreservesFields()
    {
        // 비트 패턴: inputX=-1 (00) + jumpPressed=true (bit 2) = 0b0000_0100 = 0x04
        var intent = new C_MoveIntent { input = 0x04, clientTick = 12345 };

        ArraySegment<byte> bytes = intent.Write();
        var decoded = new C_MoveIntent();
        decoded.Read(bytes);

        Assert.Equal((byte)0x04, decoded.input);
        Assert.Equal(12345u, decoded.clientTick);
    }

    [Theory]
    [InlineData((byte)0x00)]       // inputX=-1, no jump (00 + 0)
    [InlineData((byte)0x01)]       // inputX=0, no jump (01 + 0)
    [InlineData((byte)0x02)]       // inputX=+1, no jump (10 + 0)
    [InlineData((byte)0x03)]       // reserved invalid (11) — wire는 통과, Decode가 정상화
    [InlineData((byte)0x04)]       // inputX=-1, jump (00 + 100)
    [InlineData((byte)0x06)]       // inputX=+1, jump (10 + 100)
    [InlineData((byte)0xFF)]       // 모든 비트 on (미래 예약 영역까지) — wire는 통과
    public void C_MoveIntent_RoundTrip_HandlesByteBitPatterns(byte value)
    {
        // PacketGenerator byte 템플릿(WriteByteFormat/ReadByteFormat) wire round-trip 회귀.
        // 비트 의미는 InputBitsTests에서 별도 검증 — 본 테스트는 byte 자체 보존만.
        var intent = new C_MoveIntent { input = value, clientTick = 0 };

        ArraySegment<byte> bytes = intent.Write();
        var decoded = new C_MoveIntent();
        decoded.Read(bytes);

        Assert.Equal(value, decoded.input);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(1u)]
    [InlineData((uint)int.MaxValue)]  // 21억 — signed/unsigned 경계
    [InlineData(uint.MaxValue)]       // 42억 — uint 최대 (wrap 직전)
    // 음수 케이스는 uint이라 컴파일러가 원천 차단.
    public void C_MoveIntent_RoundTrip_HandlesClientTickEdgeValues(uint tick)
    {
        var intent = new C_MoveIntent { input = 0x01, clientTick = tick };

        ArraySegment<byte> bytes = intent.Write();
        var decoded = new C_MoveIntent();
        decoded.Read(bytes);

        Assert.Equal(tick, decoded.clientTick);
    }

    [Fact]
    public void C_MoveIntent_Write_ProducesCorrectSizeHeader()
    {
        // [size:2][id:2][input:1][clientTick:4] = 9 bytes 총.
        var intent = new C_MoveIntent { input = 0x02, clientTick = 0 };

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
        var intent = new C_MoveIntent { input = 0x01, clientTick = 0 };

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
            x = 1.25f,            // GameMap.Tick 5회 = MoveSpeed(5) * TickDuration(0.05) * 5 = 1.25
            y = -3.5f,
            vx = 5.0f,            // 우측 이동 중 속도 = MoveSpeed
            vy = 8.0f,            // 점프 직후 vy = JumpSpeed
            serverTick = 1000,
            lastAckedClientTick = 999
        };

        ArraySegment<byte> bytes = snap.Write();
        var decoded = new S_Snapshot();
        decoded.Read(bytes);

        Assert.Equal(42, decoded.entityId);
        Assert.Equal(1.25f, decoded.x);
        Assert.Equal(-3.5f, decoded.y);
        Assert.Equal(5.0f, decoded.vx);
        Assert.Equal(8.0f, decoded.vy);
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
        // vx/vy도 같은 float 템플릿으로 직렬화 — 같은 경로 검증.
        var snap = new S_Snapshot
        {
            entityId = 0,
            x = coord, y = coord,
            vx = coord, vy = coord,
            serverTick = 0,
            lastAckedClientTick = 0
        };

        ArraySegment<byte> bytes = snap.Write();
        var decoded = new S_Snapshot();
        decoded.Read(bytes);

        Assert.Equal(coord, decoded.x);
        Assert.Equal(coord, decoded.y);
        Assert.Equal(coord, decoded.vx);
        Assert.Equal(coord, decoded.vy);
    }

    [Fact]
    public void S_Snapshot_Write_ProducesCorrectSizeHeader()
    {
        // [size:2][id:2][entityId:4][x:4][y:4][vx:4][vy:4][serverTick:4][lastAckedClientTick:4][animState:1] = 33 bytes.
        var snap = new S_Snapshot
        {
            entityId = 0, x = 0f, y = 0f, vx = 0f, vy = 0f,
            serverTick = 0, lastAckedClientTick = 0, animState = 0
        };

        ArraySegment<byte> bytes = snap.Write();

        Assert.Equal(33, bytes.Count);
        ushort size = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes.Array!, bytes.Offset, 2));
        Assert.Equal(33, size);
    }

    [Fact]
    public void S_Snapshot_Write_ProducesCorrectPacketId()
    {
        // PDL.xml 6번째 정의 = PacketID 6
        var snap = new S_Snapshot
        {
            entityId = 0, x = 0f, y = 0f, vx = 0f, vy = 0f,
            serverTick = 0, lastAckedClientTick = 0
        };

        ArraySegment<byte> bytes = snap.Write();

        ushort packetId = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes.Array!, bytes.Offset + 2, 2));
        Assert.Equal((ushort)PacketID.S_Snapshot, packetId);
        Assert.Equal((ushort)6, packetId);
    }

    // ──────────────────────────────────────────────────────────────────
    // C_Handshake / S_HandshakeResult 라운드트립.
    //
    // **본 묶음의 가치**:
    //   - PacketGenerator의 bool / string 템플릿 직접 회귀 안전망 (S_HandshakeResult가 두 타입의 첫 실수요자)
    //   - empty / ASCII / Unicode reason 포함 → string wire format 정합 (UTF-16 LE, GetByteCount + GetBytes(span,span))
    //   - bool 0/1 양쪽 라운드트립
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public void C_Handshake_RoundTrip_PreservesClientVersion()
    {
        var pkt = new C_Handshake { clientVersion = 42 };

        ArraySegment<byte> bytes = pkt.Write();
        var decoded = new C_Handshake();
        decoded.Read(bytes);

        Assert.Equal((ushort)42, decoded.clientVersion);
    }

    [Theory]
    [InlineData((ushort)0)]
    [InlineData((ushort)1)]
    [InlineData((ushort)2)]               // ProtocolVersion.Current
    [InlineData((ushort)256)]             // 1 byte 경계
    [InlineData(ushort.MaxValue)]         // 65535 boundary
    public void C_Handshake_RoundTrip_HandlesUshortEdgeValues(ushort version)
    {
        var pkt = new C_Handshake { clientVersion = version };

        ArraySegment<byte> bytes = pkt.Write();
        var decoded = new C_Handshake();
        decoded.Read(bytes);

        Assert.Equal(version, decoded.clientVersion);
    }

    [Fact]
    public void C_Handshake_Write_ProducesCorrectSizeAndPacketId()
    {
        // [size:2][id:2][clientVersion:2] = 6 bytes. PDL.xml 7번째 정의 = PacketID 7.
        var pkt = new C_Handshake { clientVersion = 0 };

        ArraySegment<byte> bytes = pkt.Write();

        Assert.Equal(6, bytes.Count);
        ushort size = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes.Array!, bytes.Offset, 2));
        Assert.Equal(6, size);

        ushort packetId = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes.Array!, bytes.Offset + 2, 2));
        Assert.Equal((ushort)PacketID.C_Handshake, packetId);
        Assert.Equal((ushort)7, packetId);
    }

    [Fact]
    public void S_HandshakeResult_RoundTrip_PreservesAllFields_OkTrue()
    {
        var pkt = new S_HandshakeResult
        {
            ok = true,
            serverVersion = 2,
            reason = "",
        };

        ArraySegment<byte> bytes = pkt.Write();
        var decoded = new S_HandshakeResult();
        decoded.Read(bytes);

        Assert.True(decoded.ok);
        Assert.Equal((ushort)2, decoded.serverVersion);
        Assert.Equal("", decoded.reason);
    }

    [Fact]
    public void S_HandshakeResult_RoundTrip_PreservesAllFields_OkFalse()
    {
        var pkt = new S_HandshakeResult
        {
            ok = false,
            serverVersion = 2,
            reason = "ProtocolVersion mismatch (client=3, server=2)",
        };

        ArraySegment<byte> bytes = pkt.Write();
        var decoded = new S_HandshakeResult();
        decoded.Read(bytes);

        Assert.False(decoded.ok);
        Assert.Equal((ushort)2, decoded.serverVersion);
        Assert.Equal("ProtocolVersion mismatch (client=3, server=2)", decoded.reason);
    }

    [Theory]
    [InlineData("")]                                       // empty
    [InlineData("OK")]                                     // ASCII 2 char
    [InlineData("ProtocolVersion mismatch")]              // ASCII 긴 reason
    [InlineData("한글 reason 메시지")]                       // Unicode 혼합 (CJK)
    [InlineData("✨ emoji 🚀")]                            // surrogate pair (4 byte UTF-16)
    public void S_HandshakeResult_RoundTrip_HandlesReasonStringEdgeValues(string reason)
    {
        // string wire format = UTF-16 LE + UInt16 LE length prefix. PacketGenerator string 템플릿 회귀 안전망.
        var pkt = new S_HandshakeResult
        {
            ok = false,
            serverVersion = 99,
            reason = reason,
        };

        ArraySegment<byte> bytes = pkt.Write();
        var decoded = new S_HandshakeResult();
        decoded.Read(bytes);

        Assert.Equal(reason, decoded.reason);
        Assert.False(decoded.ok);                          // bool false 보존도 같이 검증
        Assert.Equal((ushort)99, decoded.serverVersion);
    }

    [Fact]
    public void S_HandshakeResult_Write_ProducesCorrectPacketId()
    {
        // PDL.xml 8번째 정의 = PacketID 8
        var pkt = new S_HandshakeResult { ok = true, serverVersion = 0, reason = "" };

        ArraySegment<byte> bytes = pkt.Write();

        ushort packetId = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes.Array!, bytes.Offset + 2, 2));
        Assert.Equal((ushort)PacketID.S_HandshakeResult, packetId);
        Assert.Equal((ushort)8, packetId);
    }

    // ──────────────────────────────────────────────────────────────────
    // C_EnterPortal / S_MapTransition 라운드트립.
    //
    // 맵 전환 패킷 2종 (PacketID 17/18). S_MapTransition.spawnX/Y의 float 직렬화는
    // .NET Standard 2.1 ↔ .NET 10 cross-runtime 경로(SingleToInt32Bits)라 회귀 가치 큼.
    // entityId 필드 없음 (ADR-026: entity id 전역 유지).
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public void C_EnterPortal_RoundTrip_PreservesPortalId()
    {
        var pkt = new C_EnterPortal { portalId = 3 };

        ArraySegment<byte> bytes = pkt.Write();
        var decoded = new C_EnterPortal();
        decoded.Read(bytes);

        Assert.Equal(3, decoded.portalId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void C_EnterPortal_RoundTrip_HandlesIntEdgeValues(int portalId)
    {
        var pkt = new C_EnterPortal { portalId = portalId };

        ArraySegment<byte> bytes = pkt.Write();
        var decoded = new C_EnterPortal();
        decoded.Read(bytes);

        Assert.Equal(portalId, decoded.portalId);
    }

    [Fact]
    public void C_EnterPortal_Write_ProducesCorrectSizeAndPacketId()
    {
        // [size:2][id:2][portalId:4] = 8 bytes. PDL.xml 17번째 정의 = PacketID 17.
        var pkt = new C_EnterPortal { portalId = 0 };

        ArraySegment<byte> bytes = pkt.Write();

        Assert.Equal(8, bytes.Count);
        ushort size = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes.Array!, bytes.Offset, 2));
        Assert.Equal(8, size);

        ushort packetId = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes.Array!, bytes.Offset + 2, 2));
        Assert.Equal((ushort)PacketID.C_EnterPortal, packetId);
        Assert.Equal((ushort)17, packetId);
    }

    [Fact]
    public void S_MapTransition_RoundTrip_PreservesAllFields()
    {
        var pkt = new S_MapTransition { destMapId = 1, spawnX = 2.0f, spawnY = -1.5f };

        ArraySegment<byte> bytes = pkt.Write();
        var decoded = new S_MapTransition();
        decoded.Read(bytes);

        Assert.Equal((byte)1, decoded.destMapId);
        Assert.Equal(2.0f, decoded.spawnX);
        Assert.Equal(-1.5f, decoded.spawnY);
    }

    [Theory]
    [InlineData((byte)0, 0f, 0f)]         // Town
    [InlineData((byte)1, 2.0f, 0f)]       // HuntingGround spawn
    [InlineData((byte)2, 22.0f, 0f)]      // BossRoom spawn
    [InlineData((byte)255, float.MaxValue, float.MinValue)]
    public void S_MapTransition_RoundTrip_HandlesEdgeValues(byte destMapId, float x, float y)
    {
        // float 직렬화 cross-runtime 경로(SingleToInt32Bits) 회귀 안전망. byte destMapId 보존.
        var pkt = new S_MapTransition { destMapId = destMapId, spawnX = x, spawnY = y };

        ArraySegment<byte> bytes = pkt.Write();
        var decoded = new S_MapTransition();
        decoded.Read(bytes);

        Assert.Equal(destMapId, decoded.destMapId);
        Assert.Equal(x, decoded.spawnX);
        Assert.Equal(y, decoded.spawnY);
    }

    [Fact]
    public void S_MapTransition_Write_ProducesCorrectSizeAndPacketId()
    {
        // [size:2][id:2][destMapId:1][spawnX:4][spawnY:4] = 13 bytes. PDL.xml 18번째 정의 = PacketID 18.
        var pkt = new S_MapTransition { destMapId = 0, spawnX = 0f, spawnY = 0f };

        ArraySegment<byte> bytes = pkt.Write();

        Assert.Equal(13, bytes.Count);
        ushort size = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes.Array!, bytes.Offset, 2));
        Assert.Equal(13, size);

        ushort packetId = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes.Array!, bytes.Offset + 2, 2));
        Assert.Equal((ushort)PacketID.S_MapTransition, packetId);
        Assert.Equal((ushort)18, packetId);
    }
}
