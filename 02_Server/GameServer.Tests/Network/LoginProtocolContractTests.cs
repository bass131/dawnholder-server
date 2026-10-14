using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Network;

// Goal T9 (completion condition 9): the login packets are appended, existing IDs never move.
// Expected numbers are literals from PDL.xml at main 6782cd67, never read back from the generator.
// The protocol version number is not asserted here: it depends on the merge order with the Content
// packet PR (goal "PR 경계와 점검"), and the three existing version tests own it.
public sealed class LoginProtocolContractTests
{
    // T9 (protective, passes before the change): IDs 1..38 keep their names and numbers.
    [Fact]
    public void T9_ExistingPacketIds_OneToThirtyEight_KeepTheirNumbers()
    {
        (PacketID Id, int Number)[] existing =
        {
            (PacketID.C_Ping, 1), (PacketID.S_Pong, 2), (PacketID.S_EnterMap, 3), (PacketID.S_LeaveMap, 4),
            (PacketID.C_MoveIntent, 5), (PacketID.S_Snapshot, 6), (PacketID.C_Handshake, 7),
            (PacketID.S_HandshakeResult, 8), (PacketID.S_PlayerJoin, 9), (PacketID.S_PlayerLeave, 10),
            (PacketID.C_Attack, 11), (PacketID.S_EntitySpawn, 12), (PacketID.S_HitResult, 13),
            (PacketID.S_EntityDeath, 14), (PacketID.S_StageClear, 15), (PacketID.C_CharacterSelect, 16),
            (PacketID.C_EnterPortal, 17), (PacketID.S_MapTransition, 18), (PacketID.S_EntityState, 19),
            (PacketID.S_EnemyAttack, 20), (PacketID.S_PlayerHp, 21), (PacketID.S_PlayerAttack, 22),
            (PacketID.S_ProjectileLaunch, 23), (PacketID.C_SkillUse, 24), (PacketID.S_SkillCast, 25),
            (PacketID.C_PartyInvite, 26), (PacketID.C_PartyRespond, 27), (PacketID.C_PartyLeave, 28),
            (PacketID.S_PartyInviteRecv, 29), (PacketID.S_PartyUpdate, 30), (PacketID.S_PartyError, 31),
            (PacketID.S_QuestUpdate, 32), (PacketID.S_PortalLocked, 33), (PacketID.C_CheatCommand, 34),
            (PacketID.C_InventoryRequest, 35), (PacketID.S_InventorySnapshot, 36), (PacketID.C_ItemUse, 37),
            (PacketID.S_ItemUseResult, 38),
        };

        int[] numbers = existing.Select(entry => (int)entry.Id).ToArray();
        int[] expected = existing.Select(entry => entry.Number).ToArray();
        Assert.Equal(expected, numbers);
        Assert.Equal(Enumerable.Range(1, 38), expected);
    }

    // T9: C_Login and S_LoginResult come after 38 and are the two largest IDs, request before result.
    [Fact]
    public void T9_LoginPackets_AreTheTwoLargestIds_AfterTheExistingThirtyEight()
    {
        int largest = Enum.GetValues<PacketID>().Max(id => (int)id);

        Assert.True((int)PacketID.C_Login > 38, $"C_Login reuses existing ID {(int)PacketID.C_Login}");
        Assert.Equal(largest - 1, (int)PacketID.C_Login);
        Assert.Equal(largest, (int)PacketID.S_LoginResult);
    }

    // T9: C_Login reads back the written name, including the 16-character maximum.
    [Theory]
    [InlineData("abcdefghijklmnop")]
    [InlineData("a_1")]
    public void T9_C_Login_RoundTripsTheAccountName(string accountName)
    {
        C_Login read = new();

        read.Read(new C_Login { accountName = accountName }.Write());

        Assert.Equal(accountName, read.accountName);
    }

    [Theory]
    [InlineData(LoginResult.Accepted)]
    [InlineData(LoginResult.InvalidName)]
    [InlineData(LoginResult.AlreadyOnline)]
    public void T9_S_LoginResult_RoundTripsEveryResult(LoginResult result)
    {
        S_LoginResult read = new();

        read.Read(new S_LoginResult { result = (byte)result }.Write());

        Assert.Equal((byte)result, read.result);
    }

    // T9: the wire values are fixed: Accepted 0, InvalidName 1, AlreadyOnline 2, and nothing else.
    [Fact]
    public void T9_LoginResult_ValuesAreZeroOneTwo()
    {
        byte[] values = Enum.GetValues<LoginResult>().Select(result => (byte)result).ToArray();

        Assert.Equal(typeof(byte), Enum.GetUnderlyingType(typeof(LoginResult)));
        Assert.Equal(0, (byte)LoginResult.Accepted);
        Assert.Equal(1, (byte)LoginResult.InvalidName);
        Assert.Equal(2, (byte)LoginResult.AlreadyOnline);
        Assert.Equal(new byte[] { 0, 1, 2 }, values);
    }
}
