using System.Net;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Party;

/// <summary>
/// 파티 초대/수락/탈퇴 happy path e2e — 핸들러 → session.Submit* → PartyRegistry actor →
/// PartyNotifier → GameWorld.SendToEntity → 대상 맵 Send까지 전 경로 검증.
///
/// **검증 invariant**:
///   - Invite: 피초대자 1명에게만 S_PartyInviteRecv(inviterEntityId/Class 정확) + 초대자 본인엔 안 감.
///   - Accept: 양 멤버 둘 다 S_PartyUpdate(partyId>0, 멤버 2명, 클래스 정확).
///   - Leave: 남은 멤버에게 S_PartyUpdate{partyId=0}(해산 통보).
///   - Auth 게이트: class 선택 전(미진입) 세션의 파티 패킷 = silent drop(Submit 마샬링 X).
///
/// **테스트 전략** (GameWorldPartyIntegrationTests 패턴 정합):
///   - 실제 GameWorld + 실제 Town 맵 — SubmitParty*가 GameWorld.Instance 의존.
///   - TestGameSession이 Send override로 수신 패킷 캡처. GetMap은 override 안 함(실제 Town).
///   - 송신은 PartyRegistry job → 대상 맵 EnqueueJob 2단 마샬링 → Party.Tick() + map.Tick() 드레인 후 확인.
///   - 싱글톤 직렬화: GameWorld 단일 인스턴스 → IDisposable + [Collection].
/// </summary>
[Collection("GameWorldPartyIntegrationTests")]
public class PartyHandlerHappyTests : IDisposable
{
    readonly GameWorld _world;
    readonly GameMap _town;

    public PartyHandlerHappyTests()
    {
        _world = new GameWorld(new Dictionary<MapId, (MapTerrain?, MapContent?)>());
        _town = _world.GetMap(MapId.Town)!;
    }

    public void Dispose() => _world.Stop();

    // ── 헬퍼 ──────────────────────────────────────────────────────────────────

    static PacketID PacketIdOf(byte[] payload)
        => (PacketID)(ushort)(payload[2] | (payload[3] << 8));

    static int CountOf(List<byte[]> sent, PacketID type)
        => sent.Count(p => PacketIdOf(p) == type);

    static byte[] FirstOf(List<byte[]> sent, PacketID type)
        => sent.First(p => PacketIdOf(p) == type);

    // 모든 맵 + 파티 actor 드레인. 파티 job이 SendToEntity로 맵에 2차 enqueue하므로
    //   Party.Tick() 먼저 → 맵 Tick()으로 실제 Send 도달.
    void DrainAll(long tick)
    {
        _world.Party.Tick();
        _town.Tick(tick);
    }

    // 정상 진입한 세션(handshake + class 선택 + 월드 진입 완료). class byte 지정.
    PartySession EnterSession(byte characterClass, long tick)
    {
        PartySession s = new();
        s.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        s.Bypass(characterClass); // 월드 진입 job enqueue
        _town.Tick(tick);         // EnterGameWorld 람다 처리 → _entityId 박힘
        return s;
    }

    static ArraySegment<byte> InviteBytes(int targetEntityId)
        => new C_PartyInvite { targetEntityId = targetEntityId }.Write();

    static ArraySegment<byte> RespondBytes(int inviterEntityId, byte accept)
        => new C_PartyRespond { inviterEntityId = inviterEntityId, accept = accept }.Write();

    static ArraySegment<byte> LeaveBytes()
        => new C_PartyLeave { reserved = 0 }.Write();

    // ── 1. Invite: 피초대자에게만 S_PartyInviteRecv ───────────────────────────

    [Fact]
    public void Invite_DeliversInviteRecv_OnlyToTarget()
    {
        PartySession inviter = EnterSession((byte)CharacterClass.Knight, tick: 1);
        PartySession target = EnterSession((byte)CharacterClass.Mage, tick: 2);
        inviter.Sent.Clear();
        target.Sent.Clear();

        inviter.OnRecvPacket(InviteBytes(target.PublicEntityId));
        DrainAll(3);

        // 피초대자 1명에게만 도착.
        Assert.Equal(1, CountOf(target.Sent, PacketID.S_PartyInviteRecv));
        Assert.Equal(0, CountOf(inviter.Sent, PacketID.S_PartyInviteRecv));

        S_PartyInviteRecv recv = new();
        recv.Read(new ArraySegment<byte>(FirstOf(target.Sent, PacketID.S_PartyInviteRecv)));
        Assert.Equal(inviter.PublicEntityId, recv.inviterEntityId);
        Assert.Equal((byte)CharacterClass.Knight, recv.inviterClass); // 서버 권위 클래스
    }

    // ── 2. Accept: 양 멤버에게 S_PartyUpdate ─────────────────────────────────

    [Fact]
    public void Accept_DeliversPartyUpdate_ToBothMembers()
    {
        PartySession inviter = EnterSession((byte)CharacterClass.Knight, tick: 1);
        PartySession target = EnterSession((byte)CharacterClass.Mage, tick: 2);

        // invite → 보류 초대 기록.
        inviter.OnRecvPacket(InviteBytes(target.PublicEntityId));
        DrainAll(3);

        inviter.Sent.Clear();
        target.Sent.Clear();

        // accept(1) → 파티 결성 + 양 멤버 통보.
        target.OnRecvPacket(RespondBytes(inviter.PublicEntityId, accept: 1));
        DrainAll(4);

        Assert.Equal(1, CountOf(inviter.Sent, PacketID.S_PartyUpdate));
        Assert.Equal(1, CountOf(target.Sent, PacketID.S_PartyUpdate));

        S_PartyUpdate update = new();
        update.Read(new ArraySegment<byte>(FirstOf(inviter.Sent, PacketID.S_PartyUpdate)));
        Assert.True(update.partyId > 0);
        Assert.Equal(inviter.PublicEntityId, update.leaderEntityId);
        // 멤버 슬롯 정합 — Members 순서 = [inviter, responder].
        Assert.Equal(inviter.PublicEntityId, update.member0EntityId);
        Assert.Equal(target.PublicEntityId, update.member1EntityId);
        Assert.Equal((byte)CharacterClass.Knight, update.member0Class);
        Assert.Equal((byte)CharacterClass.Mage, update.member1Class);
    }

    // ── 3. Leave: 남은 멤버에게 해산 통보(partyId=0) ──────────────────────────

    [Fact]
    public void Leave_DisbandsParty_NotifiesRemainingMember()
    {
        PartySession inviter = EnterSession((byte)CharacterClass.Knight, tick: 1);
        PartySession target = EnterSession((byte)CharacterClass.Mage, tick: 2);

        inviter.OnRecvPacket(InviteBytes(target.PublicEntityId));
        DrainAll(3);
        target.OnRecvPacket(RespondBytes(inviter.PublicEntityId, accept: 1));
        DrainAll(4);

        inviter.Sent.Clear();
        target.Sent.Clear();

        // target이 탈퇴 → 해산. 양 멤버에게 partyId=0 통보.
        target.OnRecvPacket(LeaveBytes());
        DrainAll(5);

        Assert.Equal(1, CountOf(inviter.Sent, PacketID.S_PartyUpdate));
        Assert.Equal(1, CountOf(target.Sent, PacketID.S_PartyUpdate));

        S_PartyUpdate disband = new();
        disband.Read(new ArraySegment<byte>(FirstOf(inviter.Sent, PacketID.S_PartyUpdate)));
        Assert.Equal(0, disband.partyId); // 해산 = partyId 0

        // 해산 후 양쪽 모두 파티 lookup 실패.
        Assert.Null(_world.Party.GetPartyByEntity(inviter.PublicEntityId));
        Assert.Null(_world.Party.GetPartyByEntity(target.PublicEntityId));
    }

    // ── 4. Auth 게이트: class 선택 전 파티 패킷 거부 ──────────────────────────

    [Fact]
    public void AuthGate_PartyPacketBeforeClassSelect_IsDropped()
    {
        // handshake만 완료(class 선택 X) — 월드 미진입 상태.
        PartySession s = new();
        s.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        s.BypassHandshakeOnly(); // _handshakeCompleted=true, HasSelectedClass=false

        s.Sent.Clear();

        // class 선택 전 invite → 핸들러 auth 게이트가 silent drop. Submit 마샬링 X.
        s.OnRecvPacket(InviteBytes(targetEntityId: 999));
        DrainAll(1);

        Assert.Empty(s.Sent);
        Assert.Equal(0, s.DisconnectCalls); // silent drop — disconnect 아님
    }

    // ── Fake 세션 ─────────────────────────────────────────────────────────────

    // 실제 Town 맵에 진입(GetMap override 안 함) + Send 캡처.
    class PartySession : GameSession
    {
        public List<byte[]> Sent { get; } = new();
        public int DisconnectCalls { get; private set; }

        // _entityId는 private — 진입 후 S_EnterMap 패킷에서 추출하거나 맵 조회로 노출.
        //   여기선 진입 직후 EnterGameWorld가 Send한 S_EnterMap에서 entityId를 캡처.
        public int PublicEntityId { get; private set; } = -1;

        public override void Send(ArraySegment<byte> seg)
        {
            byte[] copy = new byte[seg.Count];
            Array.Copy(seg.Array!, seg.Offset, copy, 0, seg.Count);
            Sent.Add(copy);

            // S_EnterMap에서 자기 entityId 캡처 (테스트가 패킷 송신 대상으로 사용).
            if (PublicEntityId < 0 && PacketIdOf(copy) == PacketID.S_EnterMap)
            {
                S_EnterMap enter = new();
                enter.Read(new ArraySegment<byte>(copy));
                PublicEntityId = enter.entityId;
            }
        }

        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { DisconnectCalls++; }

        public void Bypass(byte characterClass)
        {
            CompleteHandshakeAndEnter();
            SetCharacterClass(characterClass);
            EnterGameWorldIfReady();
        }

        public void BypassHandshakeOnly() => CompleteHandshakeAndEnter();
    }
}
