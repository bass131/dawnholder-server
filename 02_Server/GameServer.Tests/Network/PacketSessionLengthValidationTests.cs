using System.Net;
using Dawnholder.Server.Network;

namespace GameServer.Tests.Network;

/// <summary>
/// Phase 09 (M2.5 Trust-boundary): PacketSession.OnRecv length 검증 fail-closed 회귀 안전망.
///
/// **왜 이 테스트가 헌법 §3 핵심인가**: γ 감사(Claude α + Codex β)에서 발견된 위반 1순위.
/// dataSize=0/1/3 같은 invalid frame은 부분 무한 루프 또는 잘못된 PacketID dispatch로 갈 수 있고,
/// dataSize=70000 같은 oversize는 메모리 폭주 표적. 본 테스트가 *commit 시점*에 회귀 검출.
///
/// **테스트 전략**: TestPacketSession 서브클래스가 Disconnect/OnRecvPacket 호출을 카운트.
/// 실제 socket 없이 OnRecv 직접 호출 — Disconnect는 virtual로 표시되어 override 가능.
///
/// **Codex β 추가 발견**: 정상 분할 패킷(dataSize > buffer.Count)은 disconnect가 아닌
/// break + 다음 recv 대기. 케이스 F가 이 invariant 보존 검증.
/// </summary>
public class PacketSessionLengthValidationTests
{
    // 테스트용 서브클래스 — Disconnect/OnRecvPacket 호출을 추적.
    class TestPacketSession : PacketSession
    {
        public int DisconnectCalls { get; private set; }
        public int OnRecvPacketCalls { get; private set; }
        public List<int> ReceivedSizes { get; } = new();

        public override void Disconnect()
        {
            // 실제 socket 동작은 안 함 — 카운트만.
            DisconnectCalls++;
        }
        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
            OnRecvPacketCalls++;
            ReceivedSizes.Add(buffer.Count);
        }
        public override void OnConnected(EndPoint endPoint) { }
        public override void OnDisconnected(EndPoint endPoint) { }
        public override void OnSend(int numOfBytes) { }
    }

    // 헬퍼: dataSize 헤더만 박힌 buffer 생성 (LittleEndian).
    // totalBufferSize < 2면 헤더조차 못 적음 — partial header 시나리오용.
    static ArraySegment<byte> MakeBuffer(ushort dataSize, int totalBufferSize)
    {
        byte[] buf = new byte[totalBufferSize];
        if (totalBufferSize >= 1) buf[0] = (byte)(dataSize & 0xFF);
        if (totalBufferSize >= 2) buf[1] = (byte)((dataSize >> 8) & 0xFF);
        return new ArraySegment<byte>(buf, 0, totalBufferSize);
    }

    [Fact]
    public void Case_A_DataSize_0_TriggersDisconnect()
    {
        var s = new TestPacketSession();
        // dataSize=0, buffer.Count=4 (헤더 + 일부) — invalid: 0 < MinFrameSize(4)
        int processLen = s.OnRecv(MakeBuffer(0, 4));

        Assert.Equal(1, s.DisconnectCalls);
        Assert.Equal(0, s.OnRecvPacketCalls);
        Assert.Equal(0, processLen);
    }

    [Fact]
    public void Case_B_DataSize_1_TriggersDisconnect()
    {
        var s = new TestPacketSession();
        int processLen = s.OnRecv(MakeBuffer(1, 4));

        Assert.Equal(1, s.DisconnectCalls);
        Assert.Equal(0, s.OnRecvPacketCalls);
        Assert.Equal(0, processLen);
    }

    [Fact]
    public void Case_C_DataSize_3_TriggersDisconnect()
    {
        var s = new TestPacketSession();
        // dataSize=3 (헤더 2 + id 1) — id 못 읽음, MinFrameSize(4) 미달
        int processLen = s.OnRecv(MakeBuffer(3, 4));

        Assert.Equal(1, s.DisconnectCalls);
        Assert.Equal(0, s.OnRecvPacketCalls);
        Assert.Equal(0, processLen);
    }

    [Fact]
    public void Case_D_DataSize_4_PassesValidation()
    {
        var s = new TestPacketSession();
        // dataSize=4 (정확히 MinFrameSize), buffer.Count=4 — 검증 통과, dispatch 호출.
        int processLen = s.OnRecv(MakeBuffer(4, 4));

        Assert.Equal(0, s.DisconnectCalls);
        Assert.Equal(1, s.OnRecvPacketCalls);
        Assert.Equal(4, processLen);
        Assert.Equal(4, s.ReceivedSizes[0]);
    }

    [Fact]
    public void Case_E_DataSize_OverMax_TriggersDisconnect()
    {
        var s = new TestPacketSession();
        // dataSize = MaxFrameSize+1 = 4097
        int processLen = s.OnRecv(MakeBuffer((ushort)(PacketSession.MaxFrameSize + 1), 4));

        Assert.Equal(1, s.DisconnectCalls);
        Assert.Equal(0, s.OnRecvPacketCalls);
        Assert.Equal(0, processLen);
    }

    [Fact]
    public void Case_E2_DataSize_Exactly_Max_PassesValidation()
    {
        // 경계 케이스: dataSize=MaxFrameSize 정확히 — 통과해야 함 (>는 disconnect, =는 통과).
        var s = new TestPacketSession();
        int processLen = s.OnRecv(MakeBuffer((ushort)PacketSession.MaxFrameSize, PacketSession.MaxFrameSize));

        Assert.Equal(0, s.DisconnectCalls);
        Assert.Equal(1, s.OnRecvPacketCalls);
        Assert.Equal(PacketSession.MaxFrameSize, processLen);
    }

    [Fact]
    public void Case_F_PartialPacket_BreaksWithoutDisconnect()
    {
        // Codex β 추가 발견: 정상 분할 패킷 invariant 보존.
        // dataSize=10 (valid frame size), buffer.Count=5 (헤더는 왔지만 payload 일부만 도착).
        // → disconnect X, OnRecvPacket 호출 X, break 후 다음 recv 대기.
        var s = new TestPacketSession();
        int processLen = s.OnRecv(MakeBuffer(10, 5));

        Assert.Equal(0, s.DisconnectCalls);
        Assert.Equal(0, s.OnRecvPacketCalls);
        Assert.Equal(0, processLen); // 아무것도 처리 안 함 — buffer 그대로 보존
    }

    [Fact]
    public void Case_F2_HeaderOnly_BreaksWithoutDisconnect()
    {
        // 헤더(2 byte)만 도착 — partial. break.
        var s = new TestPacketSession();
        int processLen = s.OnRecv(MakeBuffer(0, 1)); // size 헤더의 첫 byte만

        // buffer.Count(=1) < HeaderSize(=2) → 첫 줄에서 break. disconnect X.
        Assert.Equal(0, s.DisconnectCalls);
        Assert.Equal(0, s.OnRecvPacketCalls);
        Assert.Equal(0, processLen);
    }

    [Fact]
    public void MaxFrameSize_MatchesSharedConstants_DriftGuard()
    {
        // Codex β 권장(Phase 09): PacketSession.MaxFrameSize와 Shared.GameData.Constants.MaxPacketSize는
        // *주석 컨벤션 동기화*라 drift 위험 — 둘 중 하나만 바뀌면 client/server가 다른 값 봄.
        // 본 자가-verify 테스트가 drift commit 시점에 즉시 검출.
        Assert.Equal(Shared.GameData.Constants.MaxPacketSize, PacketSession.MaxFrameSize);
    }

    [Fact]
    public void TwoValidPackets_DispatchedInOrder_NoDisconnect()
    {
        // 회귀 안전망: 정상 batch는 영향 X.
        // 두 정상 frame (각 size=4) batch.
        var s = new TestPacketSession();
        byte[] buf = new byte[8];
        // frame 1
        buf[0] = 4; buf[1] = 0;
        // frame 2
        buf[4] = 4; buf[5] = 0;
        int processLen = s.OnRecv(new ArraySegment<byte>(buf, 0, 8));

        Assert.Equal(0, s.DisconnectCalls);
        Assert.Equal(2, s.OnRecvPacketCalls);
        Assert.Equal(8, processLen);
    }

    [Fact]
    public void ValidPacket_ThenInvalid_FirstDispatchedSecondDisconnects()
    {
        // 회귀 안전망: 정상 frame 처리 후 invalid 만나도 disconnect 분기 정상.
        // frame 1 (size=4 valid) → dispatch
        // frame 2 (size=1 invalid) → disconnect
        var s = new TestPacketSession();
        byte[] buf = new byte[8];
        buf[0] = 4; buf[1] = 0; // frame 1
        buf[4] = 1; buf[5] = 0; // frame 2 (invalid)
        int processLen = s.OnRecv(new ArraySegment<byte>(buf, 0, 8));

        Assert.Equal(1, s.DisconnectCalls);
        Assert.Equal(1, s.OnRecvPacketCalls);
        // processLen은 첫 frame 처리분(4) 반환 — buffer cursor 정합.
        Assert.Equal(4, processLen);
    }
}
