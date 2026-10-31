#if DEBUG
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Guild;

// [시연 디버그 치트] F9 = C_CheatCommand cheatType 1 adds the stronghold cost to the communal treasury
// (user decision 16 A, 10-27: DEBUG servers only). Expected values are the decision's +30 and the approved
// admission seed 5 per account, written as literals rather than read from GuildRules.
// Input is the real wire into GameSession.OnRecvPacket; observation is only what each connection receives.
[Collection("GameWorldRegistryTests")]
public class GuildDebugTreasuryCheatTests
{
    static ArraySegment<byte> TreasuryCheat() => new C_CheatCommand { cheatType = 1 }.Write();

    [Fact]
    public void TreasuryCheat_AddsThirty_AndPushesTreasuryCreditToEveryMember()
    {
        using GuildTestWorld guild = new();
        GuildClient[] members = guild.JoinMembers(2); // seed 5 + 5
        guild.Settle();
        int[] marks = members.Select(member => member.Mark).ToArray();

        members[0].OnRecvPacket(TreasuryCheat());
        guild.Settle();

        for (int i = 0; i < members.Length; i++)
        {
            GuildSnapshot pushed = GuildTestWorld.ReadSnapshot(
                Assert.Single(members[i].Since(marks[i], GuildWire.GuildSnapshotId)));
            Assert.Equal(ChangeKind.TreasuryCredit, pushed.ChangeKind);
            Assert.Equal(40, pushed.Treasury);
        }
        Assert.Equal(40, guild.Query(members[1]).Treasury);
    }

    [Fact]
    public void TreasuryCheat_LetsASoloMemberUpgradeTheStronghold()
    {
        using GuildTestWorld guild = new();
        GuildClient buyer = guild.JoinAndEnter(CharacterClass.Knight); // seed 5

        buyer.OnRecvPacket(TreasuryCheat());
        guild.Settle();
        guild.UpgradeStronghold(buyer); // real 45: asserts Success and stronghold level 2

        Assert.Equal(5, guild.Query(buyer).Treasury); // 35 - 30
    }

    [Fact]
    public void TreasuryCheat_BeforeAdmission_ChangesNothing()
    {
        using GuildTestWorld guild = new();
        GuildClient member = guild.JoinAndEnter(CharacterClass.Knight);
        GuildClient visitor = guild.Connect(guild.NewAccountName()); // logged in, no class selected

        visitor.OnRecvPacket(TreasuryCheat());
        guild.Settle();

        Assert.Equal(5, guild.Query(member).Treasury);
    }
}
#endif
