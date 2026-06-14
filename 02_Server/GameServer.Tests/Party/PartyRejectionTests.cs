using System.Net;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Party;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Party;

/// <summary>
/// 파티 신뢰경계(M5 Phase 05) — 거절 4종 + 응답 race + 초대 만료 + disconnect 정리 검증.
///
/// **검증 invariant**:
///   - 자기초대 → S_PartyError(2), 파티 미생성.
///   - 이미파티중 초대 → S_PartyError(1).
///   - 상대없음(없는 entityId) → S_PartyError(0).
///   - claimedInviter 불일치 respond → silent(에러도 파티 변경도 없음 — 위장/지연 응답).
///   - 초대 만료(InviteTimeoutTicks 경과 후 respond) → silent.
///   - disconnect → 파티 해산 + 남은 멤버 S_PartyUpdate{partyId=0} + 양쪽 lookup null.
///
/// **테스트 전략**: PartyHandlerHappyTests와 동일(실제 GameWorld + Town 맵 + Send 캡처 fake 세션).
///   거절은 *서버 판정*이라 PartyRegistry actor job 안에서 실행 → Party.Tick() + map.Tick() 드레인 후 확인.
/// </summary>
[Collection("GameWorldPartyIntegrationTests")]
public class PartyRejectionTests : IDisposable
{
    readonly GameWorld _world;
    readonly GameMap _town;

    public PartyRejectionTests()
    {
        _world = new GameWorld(new Dictionary<MapId, (MapTerrain?, MapContent?)>());
        _town = _world.GetMap(MapId.Town)!;
    }

    public void Dispose() => _world.Stop();

    // ── 헬퍼 (Happy 테스트와 동일 패턴) ──────────────────────────────────────────

    static PacketID PacketIdOf(byte[] payload)
        => (PacketID)(ushort)(payload[2] | (payload[3] << 8));

    static int CountOf(List<byte[]> sent, PacketID type)
        => sent.Count(p => PacketIdOf(p) == type);

    static byte[] FirstOf(List<byte[]> sent, PacketID type)
        => sent.First(p => PacketIdOf(p) == type);

    void DrainAll(long tick)
    {
        _world.Party.Tick(tick);
        _town.Tick(tick);
    }

    PartySession EnterSession(byte characterClass, long tick)
    {
        PartySession s = new();
        s.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        s.Bypass(characterClass);
        _town.Tick(tick);
        return s;
    }

    static ArraySegment<byte> InviteBytes(int targetEntityId)
        => new C_PartyInvite { targetEntityId = targetEntityId }.Write();

    static ArraySegment<byte> RespondBytes(int inviterEntityId, byte accept)
        => new C_PartyRespond { inviterEntityId = inviterEntityId, accept = accept }.Write();

    static byte ErrorReason(byte[] payload)
    {
        S_PartyError err = new();
        err.Read(new ArraySegment<byte>(payload));
        return err.reason;
    }

    // ── 1. 자기초대 → S_PartyError(2), 파티 미생성 ───────────────────────────────

    [Fact]
    public void SelfInvite_SendsErrorReason2_NoParty()
    {
        PartySession inviter = EnterSession((byte)CharacterClass.Knight, tick: 1);
        inviter.Sent.Clear();

        // target == 본인 entityId — 자기초대.
        inviter.OnRecvPacket(InviteBytes(inviter.PublicEntityId));
        DrainAll(2);

        Assert.Equal(1, CountOf(inviter.Sent, PacketID.S_PartyError));
        Assert.Equal(PartyRegistry.ErrorSelfInvite,
            ErrorReason(FirstOf(inviter.Sent, PacketID.S_PartyError)));
        Assert.Null(_world.Party.GetPartyByEntity(inviter.PublicEntityId));
    }

    // ── 2. 이미파티중 초대 → S_PartyError(1) ─────────────────────────────────────

    [Fact]
    public void InviteWhenInviterAlreadyInParty_SendsErrorReason1()
    {
        PartySession a = EnterSession((byte)CharacterClass.Knight, tick: 1);
        PartySession b = EnterSession((byte)CharacterClass.Mage, tick: 2);
        PartySession c = EnterSession((byte)CharacterClass.Knight, tick: 3);

        // a–b 파티 결성.
        a.OnRecvPacket(InviteBytes(b.PublicEntityId));
        DrainAll(4);
        b.OnRecvPacket(RespondBytes(a.PublicEntityId, accept: 1));
        DrainAll(5);

        a.Sent.Clear();

        // a가 이미 파티 중 — c를 초대 시도 → 거부(reason 1).
        a.OnRecvPacket(InviteBytes(c.PublicEntityId));
        DrainAll(6);

        Assert.Equal(1, CountOf(a.Sent, PacketID.S_PartyError));
        Assert.Equal(PartyRegistry.ErrorAlreadyInParty,
            ErrorReason(FirstOf(a.Sent, PacketID.S_PartyError)));
        // c는 파티 미가입.
        Assert.Null(_world.Party.GetPartyByEntity(c.PublicEntityId));
    }

    // ── 2b. 피초대자(target)가 이미 파티중 → S_PartyError(1) (OR 분기 두 번째 절) ──

    [Fact]
    public void InviteWhenTargetAlreadyInParty_SendsErrorReason1()
    {
        PartySession a = EnterSession((byte)CharacterClass.Knight, tick: 1);
        PartySession b = EnterSession((byte)CharacterClass.Mage, tick: 2);
        PartySession c = EnterSession((byte)CharacterClass.Knight, tick: 3);

        // a–b 파티 결성.
        a.OnRecvPacket(InviteBytes(b.PublicEntityId));
        DrainAll(4);
        b.OnRecvPacket(RespondBytes(a.PublicEntityId, accept: 1));
        DrainAll(5);

        c.Sent.Clear();

        // c가 이미 파티중인 a를 초대 → 거부(reason 1, GetPartyByEntity(target) != null 절).
        c.OnRecvPacket(InviteBytes(a.PublicEntityId));
        DrainAll(6);

        Assert.Equal(1, CountOf(c.Sent, PacketID.S_PartyError));
        Assert.Equal(PartyRegistry.ErrorAlreadyInParty,
            ErrorReason(FirstOf(c.Sent, PacketID.S_PartyError)));
        Assert.Null(_world.Party.GetPartyByEntity(c.PublicEntityId));
    }

    // ── 3. 상대없음(없는 entityId) → S_PartyError(0) ─────────────────────────────

    [Fact]
    public void InviteNonexistentTarget_SendsErrorReason0()
    {
        PartySession inviter = EnterSession((byte)CharacterClass.Knight, tick: 1);
        inviter.Sent.Clear();

        // 어느 맵에도 없는 entityId.
        inviter.OnRecvPacket(InviteBytes(targetEntityId: 999999));
        DrainAll(2);

        Assert.Equal(1, CountOf(inviter.Sent, PacketID.S_PartyError));
        Assert.Equal(PartyRegistry.ErrorTargetMissing,
            ErrorReason(FirstOf(inviter.Sent, PacketID.S_PartyError)));
        Assert.Null(_world.Party.GetPartyByEntity(inviter.PublicEntityId));
    }

    // ── 4. claimedInviter 불일치 respond → silent (파티 미생성) ───────────────────

    [Fact]
    public void RespondWithWrongInviter_SilentDrop_NoParty()
    {
        PartySession inviter = EnterSession((byte)CharacterClass.Knight, tick: 1);
        PartySession target = EnterSession((byte)CharacterClass.Mage, tick: 2);

        // 정상 초대 기록.
        inviter.OnRecvPacket(InviteBytes(target.PublicEntityId));
        DrainAll(3);

        target.Sent.Clear();
        inviter.Sent.Clear();

        // target이 응답하되 claimedInviter를 엉뚱한 id(위장)로 — 서버 기록과 불일치 → silent.
        target.OnRecvPacket(RespondBytes(inviterEntityId: 424242, accept: 1));
        DrainAll(4);

        // 에러도 파티 변경도 없음.
        Assert.Equal(0, CountOf(target.Sent, PacketID.S_PartyError));
        Assert.Equal(0, CountOf(target.Sent, PacketID.S_PartyUpdate));
        Assert.Equal(0, CountOf(inviter.Sent, PacketID.S_PartyUpdate));
        Assert.Null(_world.Party.GetPartyByEntity(inviter.PublicEntityId));
        Assert.Null(_world.Party.GetPartyByEntity(target.PublicEntityId));
    }

    // ── 5. 초대 만료(Tick N회 경과 후 respond) → silent (파티 미생성) ─────────────

    [Fact]
    public void ExpiredInvite_RespondAfterTimeout_SilentDrop_NoParty()
    {
        PartySession inviter = EnterSession((byte)CharacterClass.Knight, tick: 1);
        PartySession target = EnterSession((byte)CharacterClass.Mage, tick: 2);

        // 초대 발급(issuedTick = world.CurrentTick = 0 — TickScheduler 미시작).
        inviter.OnRecvPacket(InviteBytes(target.PublicEntityId));
        DrainAll(3); // 발급 처리

        // 만료 임계 경과 후 Party.Tick → 보류 초대 청소.
        long expiredTick = PartyRegistry.InviteTimeoutTicks + 1;
        _world.Party.Tick(expiredTick);

        target.Sent.Clear();
        inviter.Sent.Clear();

        // 만료 후 수락 → 보류 초대 없음 → silent.
        target.OnRecvPacket(RespondBytes(inviter.PublicEntityId, accept: 1));
        DrainAll(expiredTick + 1);

        Assert.Equal(0, CountOf(target.Sent, PacketID.S_PartyUpdate));
        Assert.Equal(0, CountOf(inviter.Sent, PacketID.S_PartyUpdate));
        Assert.Null(_world.Party.GetPartyByEntity(inviter.PublicEntityId));
        Assert.Null(_world.Party.GetPartyByEntity(target.PublicEntityId));
    }

    // ── 6. disconnect → 파티 해산 + 남은 멤버 S_PartyUpdate{partyId=0} + lookup null ──

    [Fact]
    public void Disconnect_DisbandsParty_NotifiesRemainingMember()
    {
        PartySession inviter = EnterSession((byte)CharacterClass.Knight, tick: 1);
        PartySession target = EnterSession((byte)CharacterClass.Mage, tick: 2);

        inviter.OnRecvPacket(InviteBytes(target.PublicEntityId));
        DrainAll(3);
        target.OnRecvPacket(RespondBytes(inviter.PublicEntityId, accept: 1));
        DrainAll(4);

        int inviterId = inviter.PublicEntityId;
        int targetId = target.PublicEntityId;
        inviter.Sent.Clear();
        target.Sent.Clear();

        // target이 disconnect → 파티 해산. 남은 멤버(inviter)에게 partyId=0 통보.
        target.OnDisconnected(new IPEndPoint(IPAddress.Loopback, 0));
        DrainAll(5);

        Assert.Equal(1, CountOf(inviter.Sent, PacketID.S_PartyUpdate));
        S_PartyUpdate disband = new();
        disband.Read(new ArraySegment<byte>(FirstOf(inviter.Sent, PacketID.S_PartyUpdate)));
        Assert.Equal(0, disband.partyId);

        // 양쪽 모두 파티 lookup null.
        Assert.Null(_world.Party.GetPartyByEntity(inviterId));
        Assert.Null(_world.Party.GetPartyByEntity(targetId));
    }

    // ── 7. disconnect 멱등: 이중 OnDisconnected → 단일 해산 ──────────────────────

    [Fact]
    public void Disconnect_Twice_IsIdempotent_SingleDisband()
    {
        PartySession inviter = EnterSession((byte)CharacterClass.Knight, tick: 1);
        PartySession target = EnterSession((byte)CharacterClass.Mage, tick: 2);

        inviter.OnRecvPacket(InviteBytes(target.PublicEntityId));
        DrainAll(3);
        target.OnRecvPacket(RespondBytes(inviter.PublicEntityId, accept: 1));
        DrainAll(4);

        inviter.Sent.Clear();

        // 이중 disconnect(정상 종료 + 소켓 에러 race 모사) — _closing Exchange 게이트가 두 번째 무시.
        target.OnDisconnected(new IPEndPoint(IPAddress.Loopback, 0));
        target.OnDisconnected(new IPEndPoint(IPAddress.Loopback, 0));
        DrainAll(5);

        // 해산 통보는 정확히 1회(이중 해산 X).
        Assert.Equal(1, CountOf(inviter.Sent, PacketID.S_PartyUpdate));
    }

    // ── Fake 세션 (Happy 테스트 PartySession과 동일) ─────────────────────────────

    class PartySession : GameSession
    {
        public List<byte[]> Sent { get; } = new();
        public int PublicEntityId { get; private set; } = -1;

        public override void Send(ArraySegment<byte> seg)
        {
            byte[] copy = new byte[seg.Count];
            Array.Copy(seg.Array!, seg.Offset, copy, 0, seg.Count);
            Sent.Add(copy);

            if (PublicEntityId < 0 && PacketIdOf(copy) == PacketID.S_EnterMap)
            {
                S_EnterMap enter = new();
                enter.Read(new ArraySegment<byte>(copy));
                PublicEntityId = enter.entityId;
            }
        }

        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { }

        public void Bypass(byte characterClass)
        {
            CompleteHandshakeAndEnter();
            SetCharacterClass(characterClass);
            EnterGameWorldIfReady();
        }
    }
}
