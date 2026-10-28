using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Catalog;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Network.CharacterMenuRequestWire;

namespace Dawnholder.Server.GameServer.Tests.Network;

// packet-contract-v1: Catalog entry is ID-authorized, then 54 -> stored 48 -> entry -> guild.
// Only repositories and transport are substituted; input uses the actual handlers and World tick.
[Collection("GameWorldRegistryTests")]
public sealed class CharacterMenuAdmissionBoundaryTests
{
    static readonly Guid CharacterId = new("53000000-0000-4000-8000-000000000001");
    static readonly CharacterSummary Mage = new(CharacterId, 2, "검증마법사", CharacterClass.Mage);

    [Theory]
    [InlineData((byte)0)]
    [InlineData((byte)1)]
    public void CatalogClassOnlySelection_Refuses48WithoutAnyAdmission(byte localClass)
    {
        using CatalogMenuTestWorld fixture = new();
        CharacterMenuRequestSession session = fixture.LoggedIn();
        session.OnRecvPacket(new C_CharacterSelect { characterClass = localClass }.Write());
        fixture.Tick();

        Assert.Equal(new byte[] { 6, 0, 48, 0, 3, 255 }, Assert.Single(session.Packets));
        Assert.Equal(0, fixture.Catalog.SelectionCount);
        Assert.Equal(0, fixture.Storage.ProbeCount);
        Assert.Equal(0, fixture.Storage.AcquireCount);
        fixture.AssertNoEntry(session);
    }

    [Fact]
    public async Task CatalogLoading_DropsGuildInputs_ThenPublishesStoredClassBeforeFirstSeed()
    {
        using CatalogMenuTestWorld fixture = new();
        TaskCompletionSource<CatalogStorageResult> selected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Catalog.SelectionPending = selected.Task;
        CharacterMenuRequestSession session = fixture.LoggedIn();
        session.OnRecvPacket(new C_CharacterSelectRequest
        {
            requestId = 1,
            characterIdD = CharacterId.ToString("D"),
        }.Write());
        await fixture.PumpUntilAsync(() => fixture.Catalog.SelectionCount == 1);

        session.OnRecvPacket(new ArraySegment<byte>(new byte[] { 5, 0, 43, 0, 0 }));
        session.OnRecvPacket(new ArraySegment<byte>(new byte[] { 8, 0, 45, 0, 0, 0, 0, 0 }));
        session.OnRecvPacket(new ArraySegment<byte>(new byte[] { 8, 0, 46, 0, 0, 0, 0, 0 }));
        fixture.Tick();
        Assert.Empty(session.Packets);
        fixture.AssertNoEntry(session);

        selected.SetResult(new(CatalogStatus.Selected, Mage));
        await fixture.PumpUntilAsync(() => session.Packets.Any(packet => Id(packet) == 44));
        ushort[] ids = session.Packets.Select(Id).ToArray();
        // This is the first player, so there is no existing peer roster (packet 9).
        Assert.Equal(new ushort[] { 54, 48, 3, 21, 44 }, ids.Take(5));
        Assert.Equal(new byte[] { 6, 0, 48, 0, 0, 1 }, session.Packets[1]);
        S_CharacterSelectResult selection = new();
        selection.Read(new ArraySegment<byte>(session.Packets[0]));
        Assert.Equal((uint)1, selection.requestId);
        Assert.Equal((byte)0, selection.result);
        Assert.Equal(CharacterId.ToString("D"), selection.selectedCharacterIdD);
        S_GuildSnapshot guild = new();
        guild.Read(new ArraySegment<byte>(Assert.Single(session.Packets, packet => Id(packet) == 44)));
        Assert.Equal(5, guild.treasury);
        Assert.Equal(1, guild.memberCount);
        Assert.Equal(1, guild.onlineMemberCount);
        Assert.Equal(0, guild.weaponLevel);
        Assert.True(session.EntityId > 0);
        session.Disconnect();
        fixture.Tick();
        await fixture.PumpUntilAsync(() => fixture.Storage.ReleaseCount == 1);
    }

    [Fact]
    public async Task FailedAuthority48Send_ReleasesSelectionWithoutEntryOrGuild()
    {
        using CatalogMenuTestWorld fixture = new();
        fixture.Catalog.Selected = Mage;
        CharacterMenuRequestSession session = fixture.LoggedIn();
        session.FailSendFor = 48;
        session.OnRecvPacket(new C_CharacterSelectRequest
        {
            requestId = 1,
            characterIdD = CharacterId.ToString("D"),
        }.Write());
        await fixture.PumpUntilAsync(() => session.IsClosing && fixture.Storage.ReleaseCount == 1);

        Assert.Equal((ushort)54, Id(Assert.Single(session.Packets)));
        Assert.Equal(-1, session.EntityId);
        Assert.DoesNotContain(session.Packets, packet => Id(packet) is 3 or 44 or 48);
        // Class may already be retained locally before Send fails; admission must still be absent.
        Assert.Equal(1, fixture.Storage.AcquireCount);
        Assert.Equal(1, fixture.Storage.ReleaseCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidStoredListing_IsRejectedAsAWhole_WithoutPartialSuccess(bool tooMany)
    {
        using CatalogMenuTestWorld fixture = new();
        CharacterSummary[] rows = tooMany
            ? new[] { Mage, Mage with { Slot = 1 }, Mage with { Slot = 3 }, Mage with { Slot = 4 } }
            : new[] { Mage, Mage with { CharacterId = new Guid("53000000-0000-4000-8000-000000000002") } };
        fixture.Catalog.ListResult = new(CatalogStatus.Listed, characters: rows);
        CharacterMenuRequestSession session = fixture.LoggedIn();
        session.OnRecvPacket(new C_CharacterListRequest { requestId = 1 }.Write());
        await fixture.PumpUntilAsync(() => session.Packets.Count == 1);

        Assert.Equal(Failure(49, 1, 14), Assert.Single(session.Packets));
        fixture.AssertNoEntry(session);
    }
}
