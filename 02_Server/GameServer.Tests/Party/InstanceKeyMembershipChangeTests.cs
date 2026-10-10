using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Quest;
using Dawnholder.Server.GameServer.Tests.Maps;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Tests.Party;

// 복사본 열쇠가 입장 때 정해진 뒤 그 안에서 파티 소속이 바뀌는 경우의 독립 검증.
// 요구: goal 2026-10-10-instance-map-lifecycle 「10-08 승인 계획과 달라진 곳」 10과 사용자 승인 msg_d0646fa471c6
//   (복사본 안에서 파티를 나간 사람의 개인 진행은 지우지 않는다), 「요구사항 원천과 적용 결정」의 S-06 기본값
//   (메인 판단 msg_1e59e0802942), 「위험」 6, goal-review IM-10·IM-12.
// 진행 값은 처치 적립 진입점으로 채운 입력이고, 기대값은 그 입력의 리터럴이다.
[Collection("GameWorldRegistryTests")]
public sealed class InstanceKeyMembershipChangeTests : IDisposable
{
    readonly InstanceMapTestWorld _fixture = new();

    QuestRegistry Quest => _fixture.World.Quest;

    public void Dispose() => _fixture.Dispose();

    // 확인 항목 1: 파티 P(a1·a2, 파티 진행 20으로 둘 다 해금)가 사냥터 P 복사본에 들어간 뒤 a2가 그 안에서 탈퇴한다.
    // a2의 개인 진행은 파티 전 4 + 탈퇴 뒤 3 = 7, a1의 개인 진행은 탈퇴 뒤 2다. 처치자는 열쇠 P를 승계한 보스방에서 보스를 잡는다.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PartyKeyBossKill_AfterAMemberLeftInside_KeepsThePersonalProgressOfBoth(bool leaverKills)
    {
        InstanceProbeSession a1 = _fixture.Join();
        InstanceProbeSession a2 = _fixture.Join();
        _fixture.CreditKills(a2.EntityId, 4);
        int partyP = _fixture.FormParty(a1, a2);
        _fixture.UnlockBossRoom(a1.EntityId);
        GameMap hunting = _fixture.MoveThroughPortal(a1, portalId: 1);
        GameMap partnerHunting = _fixture.MoveThroughPortal(a2, portalId: 1);
        _fixture.LeaveParty(a2);
        _fixture.CreditKills(a2.EntityId, 3);
        _fixture.CreditKills(a1.EntityId, 2);
        InstanceProbeSession killer = leaverKills ? a2 : a1;
        GameMap bossRoom = _fixture.MoveThroughPortal(killer, portalId: 1);
        int leaverPersonalBefore = Quest.GetSoloProgress(a2.EntityId);
        int stayerPersonalBefore = Quest.GetSoloProgress(a1.EntityId);

        KillBoss(bossRoom, killer);

        Assert.Same(hunting, partnerHunting);
        Assert.Equal(InstanceKey.ForParty(partyP), hunting.InstanceKey);
        Assert.Equal(InstanceKey.ForParty(partyP), bossRoom.InstanceKey);
        Assert.Equal(7, leaverPersonalBefore);
        Assert.Equal(2, stayerPersonalBefore);
        Assert.Equal(7, Quest.GetSoloProgress(a2.EntityId));
        Assert.Equal(2, Quest.GetSoloProgress(a1.EntityId));
        Assert.Equal(0, Quest.GetPartyProgress(partyP));
        Assert.True(Quest.IsBossUnlocked(a1.EntityId));
        Assert.True(Quest.IsBossUnlocked(a2.EntityId));
    }

    // 확인 항목 2(S-06 2번): 혼자 d(개인 진행 20, 해금)가 혼자 열쇠 사냥터에 들어간 뒤 마을의 e(개인 진행 2)와 파티 Q를 맺는다.
    // 그 뒤 d의 처치 4는 파티 진행으로 쌓이고, d는 혼자 열쇠를 승계한 보스방에서 보스를 잡는다.
    [Fact]
    public void SoloKeyBossKill_AfterJoiningAPartyInside_ClearsOnlyThePersonalProgress_AndKeepsThePartyProgressAndUnlock()
    {
        InstanceProbeSession d = _fixture.Join();
        InstanceProbeSession e = _fixture.Join();
        _fixture.UnlockBossRoom(d.EntityId);
        _fixture.CreditKills(e.EntityId, 2);
        GameMap hunting = _fixture.MoveThroughPortal(d, portalId: 1);
        int partyQ = _fixture.FormParty(e, d);
        _fixture.CreditKills(d.EntityId, 4);
        GameMap bossRoom = _fixture.MoveThroughPortal(d, portalId: 1);
        int personalBefore = Quest.GetSoloProgress(d.EntityId);
        int partyBefore = Quest.GetPartyProgress(partyQ);

        KillBoss(bossRoom, d);

        Assert.Equal(InstanceKey.ForSolo(d.EntityId), hunting.InstanceKey);
        Assert.Equal(InstanceKey.ForSolo(d.EntityId), bossRoom.InstanceKey);
        Assert.Equal(QuestConstants.BossUnlockKillCount, personalBefore);
        Assert.Equal(4, partyBefore);
        Assert.Equal(0, Quest.GetSoloProgress(d.EntityId));
        Assert.Equal(4, Quest.GetPartyProgress(partyQ));
        Assert.Equal(2, Quest.GetSoloProgress(e.EntityId));
        Assert.True(Quest.IsBossUnlocked(d.EntityId));
        Assert.Equal(QuestConstants.BossUnlockKillCount, Quest.GetKillCount(d.EntityId));
    }

    // S-06 3번(메인 판단 기본값, 「위험」 6): c가 혼자 열쇠로 사냥터에 먼저 들어간 뒤 마을의 f와 파티를 맺고, f가 뒤늦게 들어온다.
    [Fact]
    public void MemberWhoEnteredSoloBeforeThePartyFormed_StaysApartFromTheLaterPartyEntrant()
    {
        InstanceProbeSession c = _fixture.Join();
        InstanceProbeSession f = _fixture.Join();
        GameMap soloHunting = _fixture.MoveThroughPortal(c, portalId: 1);
        int party = _fixture.FormParty(f, c);
        GameMap partyHunting = _fixture.MoveThroughPortal(f, portalId: 1);

        Assert.NotSame(soloHunting, partyHunting);
        Assert.Equal(InstanceKey.ForSolo(c.EntityId), soloHunting.InstanceKey);
        Assert.Equal(InstanceKey.ForParty(party), partyHunting.InstanceKey);
        Assert.Equal(new[] { c.EntityId }, InstanceMapTestWorld.PlayerIds(soloHunting));
        Assert.Equal(new[] { f.EntityId }, InstanceMapTestWorld.PlayerIds(partyHunting));
        Assert.Equal(2, _fixture.World.LiveInstanceCount);
    }

    // 처치 뒤 진행 초기화의 범위가 대상이라 그 방의 job 안에서 제품 사망 후처리를 직접 부른다
    // (InstanceMapBossQuestResetTests와 같은 방법). 처치 콜백·Quest job·초기화는 같은 월드 틱에서 돈다.
    void KillBoss(GameMap bossRoom, InstanceProbeSession killer)
    {
        EnemyEntity boss = Assert.Single(bossRoom.Enemies.Values, enemy => enemy.Kind == EnemyKind.Boss);
        bossRoom.EnqueueJob(() =>
        {
            boss.Hp = 0;
            bossRoom.HandleEnemyDeath(boss, killer.EntityId);
        });
        _fixture.Tick();
        Assert.False(bossRoom.Enemies.ContainsKey(boss.EntityId), "fixture: 보스 사망 후처리가 보스를 빼야 한다");
    }
}
