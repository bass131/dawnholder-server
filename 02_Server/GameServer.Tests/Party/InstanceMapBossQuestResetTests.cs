using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Quest;
using Dawnholder.Server.GameServer.Tests.Maps;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Tests.Party;

// 보스 처치 때 지우는 퀘스트 진행의 범위와, 보스방 복사본마다 따로인 보스 상태.
// 요구: goal 2026-10-10-instance-map-lifecycle 완료조건 3, 만들 것 7, goal-review IM-12, 사용자 승인 msg_d0646fa471c6.
// 진행 값은 처치 적립 진입점으로 채운 입력이고, 기대값은 그 입력의 리터럴이다.
[Collection("GameWorldRegistryTests")]
public sealed class InstanceMapBossQuestResetTests : IDisposable
{
    readonly InstanceMapTestWorld _fixture = new();

    QuestRegistry Quest => _fixture.World.Quest;

    public void Dispose() => _fixture.Dispose();

    // T3 · 완료조건 3, IM-12: 파티 A(a1·a2, 진행 20)의 a1이 보스방에서 보스를 잡는다. 파티 B(b1·b2) 진행 3, 혼자 c 진행 2.
    [Fact]
    public void PartyBossKill_ClearsOnlyThatPartysProgress()
    {
        InstanceProbeSession a1 = _fixture.Join();
        InstanceProbeSession a2 = _fixture.Join();
        InstanceProbeSession b1 = _fixture.Join();
        InstanceProbeSession b2 = _fixture.Join();
        InstanceProbeSession c = _fixture.Join();
        int partyA = _fixture.FormParty(a1, a2);
        int partyB = _fixture.FormParty(b1, b2);
        _fixture.UnlockBossRoom(a1.EntityId);
        _fixture.CreditKills(b1.EntityId, 3);
        _fixture.CreditKills(c.EntityId, 2);
        _fixture.MoveThroughPortal(a1, portalId: 1);
        GameMap bossRoom = _fixture.MoveThroughPortal(a1, portalId: 1);
        int partyABefore = Quest.GetPartyProgress(partyA);

        KillBoss(bossRoom, a1);

        Assert.Equal(QuestConstants.BossUnlockKillCount, partyABefore);
        Assert.Equal(0, Quest.GetPartyProgress(partyA));
        Assert.Equal(3, Quest.GetPartyProgress(partyB));
        Assert.Equal(2, Quest.GetSoloProgress(c.EntityId));
    }

    // T3 · 완료조건 3, IM-12: 혼자 d(진행 20)가 자기 보스방에서 보스를 잡는다. 혼자 e 진행 4, 파티 B(b1·b2) 진행 3.
    [Fact]
    public void SoloBossKill_ClearsOnlyThatPlayersProgress()
    {
        InstanceProbeSession d = _fixture.Join();
        InstanceProbeSession e = _fixture.Join();
        InstanceProbeSession b1 = _fixture.Join();
        InstanceProbeSession b2 = _fixture.Join();
        int partyB = _fixture.FormParty(b1, b2);
        _fixture.UnlockBossRoom(d.EntityId);
        _fixture.CreditKills(e.EntityId, 4);
        _fixture.CreditKills(b1.EntityId, 3);
        _fixture.MoveThroughPortal(d, portalId: 1);
        GameMap bossRoom = _fixture.MoveThroughPortal(d, portalId: 1);
        int soloDBefore = Quest.GetSoloProgress(d.EntityId);

        KillBoss(bossRoom, d);

        Assert.Equal(QuestConstants.BossUnlockKillCount, soloDBefore);
        Assert.Equal(0, Quest.GetSoloProgress(d.EntityId));
        Assert.Equal(4, Quest.GetSoloProgress(e.EntityId));
        Assert.Equal(3, Quest.GetPartyProgress(partyB));
    }

    // T3 · 완료조건 3, IM-12: 파티 A의 a1과 혼자 d가 각자 보스방에 있고, a1이 보스를 잡은 뒤 자기 보스방을 비운다.
    [Fact]
    public void BossKillClearMarkAndRespawnInOneBossRoom_LeaveAnotherBossRoomUntouched()
    {
        InstanceProbeSession a1 = _fixture.Join();
        InstanceProbeSession a2 = _fixture.Join();
        InstanceProbeSession d = _fixture.Join();
        _fixture.FormParty(a1, a2);
        _fixture.UnlockBossRoom(a1.EntityId);
        _fixture.UnlockBossRoom(d.EntityId);
        _fixture.MoveThroughPortal(a1, portalId: 1);
        GameMap aRoom = _fixture.MoveThroughPortal(a1, portalId: 1);
        _fixture.MoveThroughPortal(d, portalId: 1);
        GameMap dRoom = _fixture.MoveThroughPortal(d, portalId: 1);
        int dBossId = BossIn(dRoom).EntityId;

        KillBoss(aRoom, a1);
        bool aRoomCleared = aRoom.IsStageCleared;
        bool dRoomClearedByOtherKill = dRoom.IsStageCleared;
        bool dBossAliveAfterOtherKill = dRoom.Enemies.ContainsKey(dBossId);
        _fixture.MoveThroughPortal(a1, portalId: 2);
        _fixture.Settle();

        Assert.True(aRoomCleared, "fixture: 보스 사망 후처리가 그 방에 클리어 표식을 남겨야 한다");
        Assert.False(dRoomClearedByOtherKill);
        Assert.True(dBossAliveAfterOtherKill);
        Assert.False(dRoom.IsStageCleared);
        Assert.Equal(new[] { dBossId }, BossIdsIn(dRoom));
    }

    static EnemyEntity BossIn(GameMap map)
        => Assert.Single(map.Enemies.Values, enemy => enemy.Kind == EnemyKind.Boss);

    static int[] BossIdsIn(GameMap map)
        => map.Enemies.Values.Where(enemy => enemy.Kind == EnemyKind.Boss).Select(enemy => enemy.EntityId).ToArray();

    // 전투 판정이 아니라 처치 뒤 진행 초기화의 범위가 대상이라, 그 방의 job 안에서 제품 사망 후처리를 직접 부른다
    // (QuestKillCountTests 5번 선례). 처치 콜백·Quest job·초기화는 제품 경로 그대로 같은 월드 틱에서 돈다.
    void KillBoss(GameMap bossRoom, InstanceProbeSession killer)
    {
        EnemyEntity boss = BossIn(bossRoom);
        bossRoom.EnqueueJob(() =>
        {
            boss.Hp = 0;
            bossRoom.HandleEnemyDeath(boss, killer.EntityId);
        });
        _fixture.Tick();
        Assert.False(bossRoom.Enemies.ContainsKey(boss.EntityId), "fixture: 보스 사망 후처리가 보스를 빼야 한다");
    }
}
