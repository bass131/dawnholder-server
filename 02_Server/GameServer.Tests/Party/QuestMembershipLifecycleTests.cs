using Dawnholder.Server.GameServer.Party;
using Dawnholder.Server.GameServer.Quest;

namespace Dawnholder.Server.GameServer.Tests.Party;

// State/result contracts use fresh registries; world wiring and packets are tested separately.
public sealed class QuestMembershipLifecycleTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RawProgress_ContinuesBeyondTarget_WhileEveryResultIsClamped(bool inParty)
    {
        PartyRegistry party = new();
        PartyState? group = inParty ? party.CreateParty(10, 20) : null;
        QuestRegistry quest = new(party.GetMembershipByEntity);
        int target = QuestConstants.BossUnlockKillCount;
        for (int kill = 1; kill <= target + 2; kill++)
        {
            IReadOnlyList<QuestProgressUpdate> updates = quest.OnKill(10);
            Assert.Equal(inParty ? new[] { 10, 20 } : new[] { 10 },
                updates.Select(u => u.RecipientEntityId));
            Assert.All(updates, u =>
            {
                Assert.Equal(Math.Min(kill, target), u.CurrentCount);
                Assert.Equal(target, u.TargetCount);
            });
            Assert.Equal(kill >= target, quest.IsBossUnlocked(10));
            Assert.Equal(inParty && kill >= target, quest.IsBossUnlocked(20));
            Assert.False(quest.IsBossUnlocked(30));
        }
        Assert.Equal(target + 2, inParty ? quest.GetPartyProgress(group!.PartyId) : quest.GetSoloProgress(10));
        Assert.Equal(target, quest.GetKillCount(10));
    }

    [Fact]
    public void SoloProgress_IsRestored_AfterPartyEnds_AndNewPartyStartsAtZero()
    {
        PartyRegistry party = new();
        QuestRegistry quest = new(party.GetMembershipByEntity);
        quest.OnKill(10);
        quest.OnKill(10);
        quest.OnKill(20);
        PartyState first = party.CreateParty(10, 20)!;
        Assert.Equal(0, quest.GetKillCount(10));
        Assert.Equal(0, quest.GetKillCount(20));
        quest.OnKill(10);
        quest.OnKill(20);
        quest.OnKill(10);
        Assert.Equal(3, quest.GetPartyProgress(first.PartyId));
        Assert.Equal(2, quest.GetSoloProgress(10));
        Assert.Equal(1, quest.GetSoloProgress(20));
        party.Disband(first.PartyId);
        quest.ForgetPartyProgress(first.PartyId);
        Assert.Equal(2, quest.GetKillCount(10));
        Assert.Equal(1, quest.GetKillCount(20));
        Assert.Equal(3, Assert.Single(quest.OnKill(10)).CurrentCount);
        PartyState second = party.CreateParty(10, 20)!;
        Assert.NotEqual(first.PartyId, second.PartyId);
        Assert.Equal(0, quest.GetKillCount(10));
        Assert.Equal(0, quest.GetPartyProgress(second.PartyId));
        Assert.Equal(0, quest.TrackedPartyProgressCount);
        Assert.Equal(new[] { 1, 1 }, quest.OnKill(20).Select(u => u.CurrentCount));
    }

    [Fact]
    public void GlobalReset_ClearsEveryPartyAndSoloRaw_ButPreservesOnlyExistingLatches()
    {
        PartyRegistry party = new();
        QuestRegistry quest = new(party.GetMembershipByEntity);
        PartyState first = party.CreateParty(10, 20)!;
        PartyState second = party.CreateParty(30, 40)!;
        int target = QuestConstants.BossUnlockKillCount;
        for (int i = 0; i < target + 1; i++)
        {
            quest.OnKill(10);
            quest.OnKill(50);
        }
        quest.OnKill(30);
        quest.OnKill(60);
        quest.ResetAllQuestProgress();
        Assert.Equal(0, quest.GetPartyProgress(first.PartyId));
        Assert.Equal(0, quest.GetPartyProgress(second.PartyId));
        Assert.Equal(0, quest.TrackedPartyProgressCount);
        Assert.Equal(0, quest.GetSoloProgress(50));
        Assert.Equal(0, quest.GetSoloProgress(60));
        foreach (int id in new[] { 10, 20, 50 })
        {
            Assert.True(quest.IsBossUnlocked(id));
            Assert.Equal(target, quest.GetKillCount(id));
        }
        foreach (int id in new[] { 30, 40, 60 })
        {
            Assert.False(quest.IsBossUnlocked(id));
            Assert.Equal(0, quest.GetKillCount(id));
        }
        Assert.All(quest.OnKill(20), u => Assert.Equal(1, u.CurrentCount));
        Assert.Equal(1, Assert.Single(quest.OnKill(50)).CurrentCount);
        Assert.Equal(target, quest.GetKillCount(20));
        Assert.Equal(target, quest.GetKillCount(50));
    }

    [Fact]
    public void Kill_ResolvesMembershipOnce_AndReturnsImmutableCapturedRecipients()
    {
        int lookups = 0;
        int[] original = { 10, 20 };
        PartyMembership current = new(7, original);
        QuestRegistry quest = new(_ => { lookups++; return current; });
        IReadOnlyList<QuestProgressUpdate> before = quest.OnKill(10);
        Assert.Equal(1, lookups);
        original[1] = 99;
        current = new PartyMembership(8, new[] { 10, 30 });
        quest.OnKill(10);
        quest.ResetAllQuestProgress();
        Assert.Equal(new[] { 10, 20 }, before.Select(u => u.RecipientEntityId));
        Assert.All(before, u => Assert.Equal(1, u.CurrentCount));
        Assert.False(before is QuestProgressUpdate[]);
        if (before is IList<QuestProgressUpdate> mutable)
            Assert.Throws<NotSupportedException>(() => mutable[0] = default);
        Assert.Equal(10, before[0].RecipientEntityId);
    }

    [Fact]
    public void ForgetParty_IsIdempotent_AndPreservesOtherPartySoloAndUnlocks()
    {
        PartyRegistry party = new();
        QuestRegistry quest = new(party.GetMembershipByEntity);
        quest.OnKill(10);
        PartyState first = party.CreateParty(10, 20)!;
        PartyState other = party.CreateParty(30, 40)!;
        for (int i = 0; i < QuestConstants.BossUnlockKillCount; i++) quest.OnKill(10);
        quest.OnKill(30);
        party.Disband(first.PartyId);
        quest.ForgetPartyProgress(first.PartyId);
        quest.ForgetPartyProgress(first.PartyId);
        quest.ForgetPartyProgress(int.MaxValue);
        Assert.Equal(1, quest.TrackedPartyProgressCount);
        Assert.Equal(0, quest.GetPartyProgress(first.PartyId));
        Assert.Equal(1, quest.GetPartyProgress(other.PartyId));
        Assert.Equal(1, quest.GetSoloProgress(10));
        Assert.True(quest.IsBossUnlocked(10));
        Assert.True(quest.IsBossUnlocked(20));
    }

#if DEBUG
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DebugCompletion_AssignsTarget_EvenAfterRawExceededTarget(bool inParty)
    {
        PartyRegistry party = new();
        PartyState? group = inParty ? party.CreateParty(10, 20) : null;
        QuestRegistry quest = new(party.GetMembershipByEntity);
        int target = QuestConstants.BossUnlockKillCount;
        for (int i = 0; i < target + 2; i++) quest.OnKill(10);
        IReadOnlyList<QuestProgressUpdate> updates = quest.DebugCompleteQuest(10);
        Assert.Equal(target, inParty ? quest.GetPartyProgress(group!.PartyId) : quest.GetSoloProgress(10));
        Assert.Equal(inParty ? new[] { 10, 20 } : new[] { 10 }, updates.Select(u => u.RecipientEntityId));
        Assert.All(updates, u =>
        {
            Assert.Equal(target, u.CurrentCount);
            Assert.Equal(target, u.TargetCount);
            Assert.True(quest.IsBossUnlocked(u.RecipientEntityId));
        });
    }
#else
    [Fact]
    public void ReleaseAssembly_DoesNotExposeDebugCompletionChain()
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        Assert.Null(typeof(QuestRegistry).GetMethod("DebugCompleteQuest", flags));
        Assert.Null(typeof(Dawnholder.Server.GameServer.Loop.GameWorld).GetMethod("CompleteQuestForDebug", flags));
        Assert.Null(typeof(Dawnholder.Server.GameServer.Sessions.GameSession).GetMethod("SubmitCheatCommand", flags));
    }
#endif
}
