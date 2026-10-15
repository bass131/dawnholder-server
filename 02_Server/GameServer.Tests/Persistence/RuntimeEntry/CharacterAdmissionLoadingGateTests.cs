using Dawnholder.Server.GameServer.Persistence;
using Shared.GameData;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry.AdmissionTestWorld;
using Repo = Dawnholder.Server.GameServer.Tests.Persistence.RecordingCharacterRepository;

namespace Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry;

/// <summary>
/// Requirement 1 (runtime-entry pretest contract; api-contract v2 「서버 입장·owner 출력·종료」; technical-spec §6 D3):
/// while the persisted account is Loading (its acquire is pending), move, attack, skill, portal, party, cheat, economy
/// and a repeated select change no authority or game state, and none of them is replayed once the stored result is
/// applied. An invalid select stays a silent drop before any store call (api-contract v2 「패킷과 공개 결과」).
/// Each input is its own theory case, so one missing gate fails on its own.
/// </summary>
[Collection("GameWorldRegistryTests")]
public sealed class CharacterAdmissionLoadingGateTests
{
    // What an applied input would send to the inputting session or to the other player.
    static readonly ushort[] InputEffects =
    {
        (ushort)PacketID.S_PlayerAttack, (ushort)PacketID.S_HitResult, (ushort)PacketID.S_SkillCast,
        (ushort)PacketID.S_ProjectileLaunch, (ushort)PacketID.S_MapTransition, (ushort)PacketID.S_PortalLocked,
        (ushort)PacketID.S_PartyInviteRecv, (ushort)PacketID.S_PartyUpdate, (ushort)PacketID.S_PartyError,
        (ushort)PacketID.S_QuestUpdate, (ushort)PacketID.S_InventorySnapshot, (ushort)PacketID.S_ItemUseResult,
    };

    static readonly string[] InputNames =
    {
        "move", "attack", "skill", "portal", "party invite", "party respond", "party leave", "cheat", "inventory",
        "item use", "repeated select",
    };

    public static TheoryData<string> LoadingInputs
    {
        get
        {
            TheoryData<string> inputs = new();
            foreach (string name in InputNames)
                inputs.Add(name);
            return inputs;
        }
    }

    [Theory]
    [MemberData(nameof(LoadingInputs))]
    public async Task InputDuringLoading_ChangesNoAuthorityOrGameState(string input)
    {
        using AdmissionTestWorld fixture = new();
        EntrySession other = await fixture.EnterAsync(OtherName, CharacterClass.Knight);
        (EntrySession loading, Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> acquire) =
            await fixture.LoadingAsync(CharacterClass.Knight);
        string stateBefore = RuntimeEntryApi.AdmissionState(loading);
        int loadingMark = loading.Mark;
        int otherMark = other.Mark;

        loading.Receive(InputPacket(input, other.EntityId));
        await fixture.TicksAsync(3);

        string stateAfter = RuntimeEntryApi.AdmissionState(loading);
        ushort[] sentToLoading = loading.IdsSince(loadingMark);
        ushort[] effectsOnOther = other.IdsSince(otherMark).Intersect(InputEffects).ToArray();

        Assert.Equal("Loading", stateBefore);
        Assert.Equal("Loading", stateAfter);
        Assert.Empty(sentToLoading);
        Assert.Empty(effectsOnOther);
        Assert.Null(fixture.PlayerOf(loading));
        Assert.Same(other, Assert.Single(fixture.Players).Owner);
        Assert.Null(fixture.World.Party.GetPartyByEntity(other.EntityId));
        Assert.False(loading.IsClosing, $"{input} during Loading closed the connection");
        Assert.Equal(new[] { Repo.Admission, Repo.Acquire }, fixture.Repository.Calls);
        Assert.Equal(CharacterClass.Knight, acquire.Request.IntendedClass);
    }

    [Fact]
    public async Task InputsDuringLoading_AreNotReplayedAfterTheStoredResultIsApplied()
    {
        using AdmissionTestWorld fixture = new();
        EntrySession other = await fixture.EnterAsync(OtherName, CharacterClass.Knight);
        (EntrySession loading, Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> acquire) =
            await fixture.LoadingAsync(CharacterClass.Knight);
        foreach (string input in InputNames)
            loading.Receive(InputPacket(input, other.EntityId));
        await fixture.TicksAsync(3);
        int otherMark = other.Mark;

        acquire.Complete(Acquired(acquire.Request, CharacterClass.Knight, KnightMaxHp));
        await fixture.TickUntilAsync(() => fixture.PlayerOf(loading) is not null, "entry after the stored result");
        await fixture.TicksAsync(3);

        int enterIndex = FirstIndexOf(loading, (ushort)PacketID.S_EnterMap);
        ushort[] replayedToLoading = loading.Sent().Skip(enterIndex + 1).Select(sent => sent.Id).Intersect(InputEffects).ToArray();
        ushort[] replayedToOther = other.IdsSince(otherMark).Intersect(InputEffects).ToArray();
        bool classKnown = fixture.World.TryGetEntityClass(loading.EntityId, out byte enteredClass);

        Assert.True(enterIndex >= 0, "the stored result did not enter the world");
        Assert.Empty(replayedToLoading);
        Assert.Empty(replayedToOther);
        Assert.Null(fixture.World.Party.GetPartyByEntity(other.EntityId));
        Assert.Null(fixture.World.Party.GetPartyByEntity(loading.EntityId));
        Assert.Equal(new[] { Repo.Admission, Repo.Acquire }, fixture.Repository.Calls);
        Assert.True(classKnown);
        Assert.Equal((byte)CharacterClass.Knight, enteredClass);
        Assert.Equal("Active", RuntimeEntryApi.AdmissionState(loading));
    }

    // The persisted path keeps the existing silent drop, and the drop does not use up the one selection.
    [Theory]
    [InlineData((byte)2)]
    [InlineData((byte)255)]
    public async Task InvalidSelectFromThePersistedAccount_IsDroppedBeforeAnyStoreCall(byte invalidClass)
    {
        using AdmissionTestWorld fixture = new();
        EntrySession session = await fixture.LogInAsync(PersistedName);
        int mark = session.Mark;

        session.Receive(new C_CharacterSelect { characterClass = invalidClass });
        await fixture.TicksAsync(3);
        string state = RuntimeEntryApi.AdmissionState(session);
        ushort[] sent = session.IdsSince(mark);
        string[] callsAfterInvalid = fixture.Repository.Calls.ToArray();
        session.Receive(new C_CharacterSelect { characterClass = (byte)CharacterClass.Knight });
        Repo.PendingCall<ReadAdmissionRequest, ReadAdmissionResult> probe = await fixture.NextReadAdmissionAsync();

        Assert.Equal("Selecting", state);
        Assert.Empty(sent);
        Assert.Empty(callsAfterInvalid);
        Assert.False(session.IsClosing);
        Assert.Null(fixture.PlayerOf(session));
        Assert.Equal(PersistedIdentity, probe.Request.Identity);
    }

    static IPacket InputPacket(string input, int otherEntityId) => input switch
    {
        "move" => new C_MoveIntent { input = 1, clientTick = 1 },
        "attack" => new C_Attack { targetEntityId = otherEntityId, attackerClientTick = 1 },
        "skill" => new C_SkillUse { skillId = 0, attackerClientTick = 1, facing = 1, verticalDir = 0 },
        "portal" => new C_EnterPortal { portalId = 0 },
        "party invite" => new C_PartyInvite { targetEntityId = otherEntityId },
        "party respond" => new C_PartyRespond { inviterEntityId = otherEntityId, accept = 1 },
        "party leave" => new C_PartyLeave { reserved = 0 },
        "cheat" => new C_CheatCommand { cheatType = 0 },
        "inventory" => new C_InventoryRequest { reserved = 0 },
        "item use" => new C_ItemUse { itemId = (int)ItemId.CoinPouch, expectedRevision = 0 },
        "repeated select" => new C_CharacterSelect { characterClass = (byte)CharacterClass.Mage },
        _ => throw new ArgumentOutOfRangeException(nameof(input), input, "unknown input case"),
    };
}
