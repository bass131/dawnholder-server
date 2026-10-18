using System.Buffers.Binary;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Tests.Guild;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Network.CharacterMenuRequestWire;

namespace Dawnholder.Server.GameServer.Tests.Network;

// server-request-contract-v1.md §1. These enter the actual session/registry/handler path.
// Legacy guidance and read-only registration observations do not exercise Stored/Core/DB.
[Collection("GameWorldRegistryTests")]
public sealed class CharacterMenuRequestBoundaryTests
{
    public static TheoryData<ushort, string> MalformedCases
    {
        get
        {
            TheoryData<ushort, string> cases = new();
            foreach (ushort id in new ushort[] { 49, 51, 53 })
            {
                foreach (string fault in new[] { "header-only", "short-id", "size-small", "size-large", "trailing", "reserved" })
                    cases.Add(id, fault);
                if (id == 49) continue;
                foreach (string fault in new[] { "missing-prefix", "short-prefix", "truncated-string", "odd-string", "over-limit", "huge-prefix", "high-surrogate", "low-surrogate", "reversed-surrogates", "unpaired-high" })
                    cases.Add(id, fault);
            }
            return cases;
        }
    }

    [Theory]
    [InlineData((ushort)49)]
    [InlineData((ushort)51)]
    [InlineData((ushort)53)]
    public void MenuHandler_IsRegisteredAndAvailableBeforeClassSelection(ushort id)
        => RequireRegistered(id);

    [Theory]
    [InlineData((ushort)49)]
    [InlineData((ushort)51)]
    [InlineData((ushort)53)]
    public void BeforeHandshake_FirstPacketPolicyClosesWithoutMenuOrRegistration(ushort id)
    {
        using CharacterMenuRequestWorld fixture = new();
        CharacterMenuRequestSession session = fixture.Connected();
        session.OnRecvPacket(AtOffset(Request(id, 1)));
        fixture.Tick();
        Assert.True(session.IsClosing);
        Assert.Equal(1, session.DisconnectCalls);
        Assert.Empty(session.Packets);
        fixture.AssertNoAdmission(session, 0);
    }

    [Theory]
    [InlineData((ushort)49)]
    [InlineData((ushort)51)]
    [InlineData((ushort)53)]
    public void BeforeLogin_ValidOffsetFrameReturnsInvalidStateAndEmptyFailureFields(ushort id)
    {
        RequireRegistered(id);
        using CharacterMenuRequestWorld fixture = new();
        CharacterMenuRequestSession session = fixture.Handshaken();
        session.Clear();
        session.OnRecvPacket(AtOffset(Request(id, 0x12345678)));
        fixture.Tick();
        Assert.Equal(Failure(id, 0x12345678, 3), Assert.Single(session.Packets));
        Assert.False(session.IsClosing);
        Assert.False(session.IsLoggedIn);
        fixture.AssertNoAdmission(session, 0);
    }

    [Theory]
    [InlineData((ushort)49)]
    [InlineData((ushort)51)]
    [InlineData((ushort)53)]
    public void LoggedInLegacy_ValidOffsetFrameReturnsOnlyLegacyGuidance(ushort id)
    {
        RequireRegistered(id);
        using CharacterMenuRequestWorld fixture = new();
        CharacterMenuRequestSession session = fixture.LoggedIn();
        session.OnRecvPacket(AtOffset(Request(id, 0x12345678)));
        fixture.Tick();
        Assert.Equal(Failure(id, 0x12345678, 16), Assert.Single(session.Packets));
        Assert.False(session.IsClosing);
        fixture.AssertNoAdmission(session, 1);
    }

    [Theory]
    [MemberData(nameof(MalformedCases))]
    public void MalformedHighId_IsSilentAndDoesNotConsumeTheNextValidLowerId(ushort id, string fault)
    {
        RequireRegistered(id);
        using CharacterMenuRequestWorld fixture = new();
        CharacterMenuRequestSession session = fixture.LoggedIn();
        byte[] malformed = Malformed(Request(id, 0xFFFF0000), id, fault);
        Assert.True(malformed.Length >= 4, "sub-header fragments belong to the framer, not GameSession");
        session.OnRecvPacket(AtOffset(malformed));
        fixture.Tick();
        Assert.Empty(session.Packets);
        Assert.False(session.IsClosing);
        fixture.AssertNoAdmission(session, 1);

        // A real normal response on this same connection rules out accidental unknown drop.
        session.OnRecvPacket(AtOffset(Request(id, 1)));
        fixture.Tick();
        Assert.Equal(Failure(id, 1, 16), Assert.Single(session.Packets));
        fixture.AssertNoAdmission(session, 1);
    }

    [Theory]
    [InlineData((ushort)49)]
    [InlineData((ushort)51)]
    [InlineData((ushort)53)]
    public void ZeroId_IsSilentWithoutConsumingARealRequest(ushort id)
    {
        RequireRegistered(id);
        using CharacterMenuRequestWorld fixture = new();
        CharacterMenuRequestSession session = fixture.LoggedIn();
        session.OnRecvPacket(Request(id, 0));
        fixture.Tick();
        Assert.Empty(session.Packets);
        fixture.AssertNoAdmission(session, 1);
        session.OnRecvPacket(Request(id, 1));
        fixture.Tick();
        Assert.Equal(Failure(id, 1, 16), Assert.Single(session.Packets));
    }

    [Theory]
    [InlineData((ushort)49)]
    [InlineData((ushort)51)]
    [InlineData((ushort)53)]
    public void StateRejectionConsumesHighWater_AcrossKindsPayloadsAndLaterLogin(ushort firstId)
    {
        foreach (ushort id in new ushort[] { 49, 51, 53 }) RequireRegistered(id);
        using CharacterMenuRequestWorld fixture = new();
        CharacterMenuRequestSession session = fixture.Handshaken();
        session.Clear();
        session.OnRecvPacket(Request(firstId, 10));
        fixture.Tick();
        Assert.Equal(Failure(firstId, 10, 3), Assert.Single(session.Packets));
        session.Clear();
        session.OnRecvPacket(new C_Login { accountName = "menu_account" }.Write());
        fixture.Tick();
        Assert.True(session.IsLoggedIn);
        session.Clear();

        foreach (ushort id in new ushort[] { 49, 51, 53 })
        {
            session.OnRecvPacket(Request(id, 10));
            session.OnRecvPacket(Request(id, 9));
        }
        byte[] changedPayload = Request(51, 10);
        changedPayload[8] = 3; // still a valid slot, but a different payload under the same id
        session.OnRecvPacket(changedPayload);
        fixture.Tick();
        Assert.Empty(session.Packets);
        fixture.AssertNoAdmission(session, 1);
        session.OnRecvPacket(Request(53, 11));
        fixture.Tick();
        Assert.Equal(Failure(53, 11, 16), Assert.Single(session.Packets));
    }

    [Fact]
    public void LegacyRejectionConsumesMaxValue_AndAnotherConnectionStartsFresh()
    {
        foreach (ushort id in new ushort[] { 49, 51, 53 }) RequireRegistered(id);
        using CharacterMenuRequestWorld fixture = new();
        CharacterMenuRequestSession old = fixture.LoggedIn("menu_old");
        old.OnRecvPacket(Request(49, uint.MaxValue));
        fixture.Tick();
        Assert.Equal(Failure(49, uint.MaxValue, 16), Assert.Single(old.Packets));
        old.Clear();
        old.OnRecvPacket(Request(51, uint.MaxValue));
        old.OnRecvPacket(Request(53, 1));
        fixture.Tick();
        Assert.Empty(old.Packets);
        old.Disconnect();
        fixture.Tick();
        old.OnRecvPacket(Request(49, 1));
        Assert.Empty(old.Packets);
        CharacterMenuRequestSession fresh = fixture.LoggedIn("menu_old");
        fresh.OnRecvPacket(Request(53, 1));
        fixture.Tick();
        Assert.Equal(Failure(53, 1, 16), Assert.Single(fresh.Packets));
        fixture.AssertNoAdmission(fresh, 1);
    }

    [Theory]
    [InlineData((ushort)49)]
    [InlineData((ushort)51)]
    [InlineData((ushort)53)]
    public void AfterClose_MenuCannotReplyOrRegister(ushort id)
    {
        RequireRegistered(id);
        using CharacterMenuRequestWorld fixture = new();
        CharacterMenuRequestSession session = fixture.LoggedIn();
        session.OnRecvPacket(Request(id, 1));
        fixture.Tick();
        Assert.Equal(Failure(id, 1, 16), Assert.Single(session.Packets));
        session.Disconnect();
        fixture.Tick();
        session.Clear();
        session.OnRecvPacket(AtOffset(Request(id, 2)));
        fixture.Tick();
        Assert.Empty(session.Packets);
        fixture.AssertNoAdmission(session, 0);
    }

    [Theory]
    [InlineData((byte)0, (ushort)49)]
    [InlineData((byte)0, (ushort)51)]
    [InlineData((byte)0, (ushort)53)]
    [InlineData((byte)1, (ushort)49)]
    [InlineData((byte)1, (ushort)51)]
    [InlineData((byte)1, (ushort)53)]
    public void EnteringAndActive_MenuIsInvalidStateWithoutReplacingClassPlayerOrGuild(byte selectedClass, ushort id)
    {
        RequireRegistered(id);
        using CharacterMenuRequestWorld fixture = new();
        CharacterMenuRequestSession session = fixture.LoggedIn();
        session.OnRecvPacket(new byte[] { 5, 0, 16, 0, selectedClass });
        bool enteringObserved = false;
        // Selection's real map job is already queued. It appends entry after this observation job.
        fixture.World.Map.EnqueueJob(() =>
        {
            Assert.Equal("Entering", session.AdmissionState.ToString());
            Assert.True(session.Selected);
            Assert.Null(fixture.PlayerOf(session));
            enteringObserved = true;
            session.OnRecvPacket(AtOffset(Request(id, 1)));
        });
        fixture.Tick();
        fixture.Tick();
        Assert.True(enteringObserved, "the test must enter the real Entering interval");
        Assert.Equal(Failure(id, 1, 3),
            Assert.Single(session.Packets.Where(packet => Id(packet) == id + 1)));
        Assert.Equal("Active", session.AdmissionState.ToString());
        PlayerEntity player = Assert.IsType<PlayerEntity>(fixture.PlayerOf(session));
        Assert.Equal((CharacterClass)selectedClass, player.Stats.Class);
        byte[] guildBefore = Assert.Single(session.Packets.Where(packet => Id(packet) == 44));
        GuildSnapshot before = GuildWire.ReadSnapshot(guildBefore);
        session.Clear();
        session.OnRecvPacket(Request(id, 2));
        fixture.Tick();
        Assert.Equal(Failure(id, 2, 3), Assert.Single(session.Packets));
        Assert.Same(player, fixture.PlayerOf(session));
        Assert.Equal((CharacterClass)selectedClass, player.Stats.Class);
        session.Clear();
        session.OnRecvPacket(GuildWire.GuildRequest());
        fixture.Tick();
        GuildSnapshot after = GuildWire.ReadSnapshot(Assert.Single(session.Packets.Where(packet => Id(packet) == 44)));
        Assert.Equal(before.GuildRevision, after.GuildRevision);
        Assert.Equal(before.Treasury, after.Treasury);
        Assert.Equal(before.MemberCount, after.MemberCount);
        Assert.Equal(before.OnlineMemberCount, after.OnlineMemberCount);
        Assert.Equal(before.InventoryRevision, after.InventoryRevision);
        Assert.Equal(before.WeaponLevel, after.WeaponLevel);
        Assert.Equal(1, fixture.World.Accounts.OnlineCount);
        Assert.Single(CharacterMenuRequestWorld.Registrations(fixture.World.Inventory, "_registeredSessions"));
        Assert.Single(CharacterMenuRequestWorld.Registrations(fixture.World.Guild, "_members"));
    }

    [Theory]
    [InlineData((byte)0)]
    [InlineData((byte)1)]
    public void ExistingLegacySelect16_PublishesAuthority48BeforeRealEntryAndGuildSeed(byte selectedClass)
    {
        using CharacterMenuRequestWorld fixture = new();
        CharacterMenuRequestSession session = fixture.LoggedIn();
        session.OnRecvPacket(new byte[] { 5, 0, 16, 0, selectedClass });
        Assert.Empty(session.Packets);
        fixture.Tick();
        fixture.Tick();
        ushort[] entry = session.Packets.Select(Id).Where(id => id is 48 or 3 or 21 or 44).ToArray();
        Assert.Equal(new ushort[] { 48, 3, 21, 44 }, entry);
        Assert.Equal(new byte[] { 6, 0, 48, 0, 0, selectedClass }, session.Packets.First());
        Assert.Equal((CharacterClass)selectedClass, fixture.PlayerOf(session)!.Stats.Class);
        Assert.Equal("Active", session.AdmissionState.ToString());
        GuildSnapshot guild = GuildWire.ReadSnapshot(Assert.Single(session.Packets.Where(packet => Id(packet) == 44)));
        Assert.Equal(5, guild.Treasury);
        Assert.Equal(1, guild.MemberCount);
        Assert.Equal(1, guild.OnlineMemberCount);
        Assert.Equal(0u, guild.InventoryRevision);
        Assert.Equal(0, guild.WeaponLevel);
        Assert.Single(CharacterMenuRequestWorld.Registrations(fixture.World.Inventory, "_registeredSessions"));
        Assert.Single(CharacterMenuRequestWorld.Registrations(fixture.World.Guild, "_members"));
    }

    static byte[] Malformed(byte[] frame, ushort id, string fault)
    {
        int prefix = id == 51 ? 11 : 9;
        switch (fault)
        {
            case "header-only": return WithSize(frame[..4]);
            case "short-id": return WithSize(frame[..7]);
            case "size-small": frame[0]--; return frame;
            case "size-large": frame[0]++; return frame;
            case "trailing": return WithSize(frame.Concat(new byte[] { 0xAC }).ToArray());
            case "reserved": frame[id == 51 ? 10 : 8] = 1; return frame;
            case "missing-prefix": return WithSize(frame[..prefix]);
            case "short-prefix": return WithSize(frame[..(prefix + 1)]);
            case "truncated-string": return WithSize(frame[..^1]);
            case "huge-prefix": BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(prefix, 2), 65534); return frame;
        }
        byte[] invalidText = fault switch
        {
            "odd-string" => new byte[] { 0x41 },
            "over-limit" => Enumerable.Repeat(new byte[] { 0x41, 0 }, 37).SelectMany(pair => pair).ToArray(),
            "high-surrogate" => new byte[] { 0, 0xD8 },
            "low-surrogate" => new byte[] { 0, 0xDC },
            "reversed-surrogates" => new byte[] { 0, 0xDC, 0, 0xD8 },
            "unpaired-high" => new byte[] { 0, 0xD8, 0x41, 0 },
            _ => throw new ArgumentOutOfRangeException(nameof(fault)),
        };
        byte[] malformed = frame[..(prefix + 2)].Concat(invalidText).ToArray();
        BinaryPrimitives.WriteUInt16LittleEndian(malformed.AsSpan(prefix, 2), checked((ushort)invalidText.Length));
        return WithSize(malformed);
    }
}
