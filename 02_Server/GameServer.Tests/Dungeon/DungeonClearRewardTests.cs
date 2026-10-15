using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Tests.Maps;
using Shared.GameData;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Dungeon.DungeonClearTestWorld;

namespace Dawnholder.Server.GameServer.Tests.Dungeon;

// 던전 클리어 보상: 누가 받는지(C1), 지급과 결과 → snapshot(C2·C3), 한 번만(C4), 보스 처치 보상 합치기(C5).
// 요구: goal 2026-10-10-dungeon-clear-rewards 「던전 클리어 PR의 요구」 C1~C5, 완료조건 5·6. 구현 전에 쓴 시험이다.
// 지금 서버는 39번을 보내지 않으므로 「39가 오지 않는다」는 단정은 언제나 같은 시험 안의 수혜자가 39를 정확히 한 번
// 받는다는 단정과 짝을 짓는다. 그래야 결과를 보내는 경로가 있고 그 경로가 이 사람만 건너뛰었다는 것까지 보인다.
[Collection("GameWorldRegistryTests")]
public sealed class DungeonClearRewardTests : IDisposable
{
    // 클리어 뒤 남은 사람이 있는 동안 흘려 보낼 틱. 정착 창보다 충분히 길다.
    const int StayTicks = 40;

    readonly DungeonClearTestWorld _world = new();

    InstanceMapTestWorld Fixture => _world.Fixture;

    public void Dispose() => _world.Dispose();

    // C1·C2·C3 · 완료조건 5: 파티 a·b가 같은 보스방 복사본에 있고 a가 보스를 잡는다.
    [Fact]
    public void BossDeath_SendsEachPlayerInThatBossRoomOneSuccessResult_ThenTheirSnapshot()
    {
        (InstanceProbeSession a, InstanceProbeSession b) = _world.JoinParty();
        GameMap bossRoom = _world.EnterSameBossRoom(a, b);
        InventoryView aBefore = _world.QueryInventory(a);
        InventoryView bBefore = _world.QueryInventory(b);
        int aMark = a.Packets.Count;
        int bMark = b.Packets.Count;

        _world.KillBoss(bossRoom, a);
        Fixture.Settle(SettleTicks);

        AssertOneSuccessThenSnapshot(a, aMark, aBefore);
        AssertOneSuccessThenSnapshot(b, bMark, bBefore);
    }

    // C1 · 완료조건 5: b가 보스방 안에서 파티를 나간 뒤 a가 보스를 잡는다.
    [Fact]
    public void MemberWhoLeftThePartyInsideTheBossRoom_StillGetsOneClearResult()
    {
        (InstanceProbeSession a, InstanceProbeSession b) = _world.JoinParty();
        GameMap bossRoom = _world.EnterSameBossRoom(a, b);
        Fixture.LeaveParty(b);
        bool bStillInside = bossRoom.GetPlayer(b.EntityId) != null;
        int aMark = a.Packets.Count;
        int bMark = b.Packets.Count;

        _world.KillBoss(bossRoom, a);
        Fixture.Settle(SettleTicks);

        Assert.True(bStillInside, "fixture: 파티를 나가도 보스방 복사본 안에 남아야 한다");
        AssertSuccessResult(Assert.Single(ClearResultsSince(a, aMark)));
        AssertSuccessResult(Assert.Single(ClearResultsSince(b, bMark)));
    }

    // C1 · 완료조건 6: 파티 a·b 중 a만 보스방에 가고 b는 사냥터 복사본에 남는다.
    [Fact]
    public void PartyMemberLeftInTheHuntingGroundCopy_GetsNothing_WhileTheBossRoomPlayerGetsOneResult()
    {
        (InstanceProbeSession a, InstanceProbeSession b) = _world.JoinParty();
        GameMap hunting = Fixture.MoveThroughPortal(b, portalId: 1);
        GameMap bossRoom = _world.EnterBossRoom(a);
        byte[] bBefore = _world.QuerySnapshot(b);
        int aMark = a.Packets.Count;
        int bMark = b.Packets.Count;

        _world.KillBoss(bossRoom, a);
        Fixture.Settle(SettleTicks);

        Assert.Same(hunting, b.CurrentMap);
        AssertSuccessResult(Assert.Single(ClearResultsSince(a, aMark)));
        Assert.Empty(ClearResultsSince(b, bMark));
        Assert.Equal(bBefore, _world.QuerySnapshot(b));
    }

    // C1 · 완료조건 6: a가 보스를 잡은 뒤 사냥터에서 기다리던 같은 파티 b가 그 보스방 복사본에 들어온다.
    [Fact]
    public void PlayerWhoEntersTheClearedBossRoom_GetsNothing_WhileTheKillTimePlayerGetsOneResult()
    {
        (InstanceProbeSession a, InstanceProbeSession b) = _world.JoinParty();
        Fixture.MoveThroughPortal(b, portalId: 1);
        GameMap bossRoom = _world.EnterBossRoom(a);
        int aMark = a.Packets.Count;
        _world.KillBoss(bossRoom, a);
        Fixture.Settle(SettleTicks);
        byte[] bBefore = _world.QuerySnapshot(b);
        int bMark = b.Packets.Count;

        GameMap entered = _world.EnterBossRoom(b);
        Fixture.Settle(StayTicks);

        Assert.Same(bossRoom, entered);
        AssertSuccessResult(Assert.Single(ClearResultsSince(a, aMark)));
        Assert.Empty(ClearResultsSince(b, bMark));
        Assert.Equal(bBefore, _world.QuerySnapshot(b));
    }

    // C1: 파티 A(a1·a2)와 B(b1·b2)가 각자 보스방 복사본에 있다. A가 먼저, 이어서 B가 보스를 잡는다.
    [Fact]
    public void EachBossRoomCopy_RewardsOnlyItsOwnPlayers_OncePerPlayer()
    {
        (InstanceProbeSession a1, InstanceProbeSession a2) = _world.JoinParty();
        (InstanceProbeSession b1, InstanceProbeSession b2) = _world.JoinParty();
        GameMap roomA = _world.EnterSameBossRoom(a1, a2);
        GameMap roomB = _world.EnterSameBossRoom(b1, b2);
        InstanceProbeSession[] everyone = { a1, a2, b1, b2 };
        int[] marks = everyone.Select(session => session.Packets.Count).ToArray();

        _world.KillBoss(roomA, a1);
        Fixture.Settle(SettleTicks);
        int[] afterA = everyone.Select((session, i) => ClearResultsSince(session, marks[i]).Count).ToArray();
        _world.KillBoss(roomB, b1);
        Fixture.Settle(SettleTicks);
        int[] afterB = everyone.Select((session, i) => ClearResultsSince(session, marks[i]).Count).ToArray();

        Assert.NotSame(roomA, roomB);
        Assert.Equal(new[] { 1, 1, 0, 0 }, afterA);
        Assert.Equal(new[] { 1, 1, 1, 1 }, afterB);
        Assert.All(everyone.Select((session, i) => ClearResultsSince(session, marks[i])[0]), AssertSuccessResult);
    }

    // C1: a·b가 보스방에 있다. b의 보스방 포탈 2(사냥터행) job이 같은 틱, 같은 맵 job 큐에서 보스 사망보다 먼저 돈다.
    // 그래서 보스가 죽는 순간 b는 이동 중이다(보스방 Players에도, 어느 맵에도 없음). 같은 큐의 순서라 결정적이다.
    [Fact]
    public void PlayerMigratingOutAtTheBossDeathTick_GetsNothing_WhileThePlayerInsideGetsOneResult()
    {
        (InstanceProbeSession a, InstanceProbeSession b) = _world.JoinParty();
        GameMap bossRoom = _world.EnterSameBossRoom(a, b);
        byte[] bBefore = _world.QuerySnapshot(b);
        int aMark = a.Packets.Count;
        int bMark = b.Packets.Count;

        Fixture.EnterPortal(b, portalId: 2);
        _world.KillBoss(bossRoom, a);
        GameMap? bMapAtDeathTick = b.CurrentMap;
        bool bInBossRoomAtDeathTick = bossRoom.GetPlayer(b.EntityId) != null;
        Fixture.Settle(SettleTicks);

        Assert.Null(bMapAtDeathTick);
        Assert.False(bInBossRoomAtDeathTick);
        Assert.Equal(MapId.HuntingGround, b.CurrentMap?.MapId);
        AssertSuccessResult(Assert.Single(ClearResultsSince(a, aMark)));
        Assert.Empty(ClearResultsSince(b, bMark));
        Assert.Equal(bBefore, _world.QuerySnapshot(b));
    }

    // C2·C3 · 완료조건 6: f의 Material이 MaxStack이다(기존 처치 경로로 채움). 파티 f·g가 보스방에 있고 g가 보스를 잡는다.
    [Fact]
    public void PlayerWhoseRewardDoesNotFit_GetsInventoryFullWithoutSnapshot_WhileTheOtherGetsSuccess()
    {
        InstanceProbeSession f = Fixture.Join();
        _world.FillMaterialToMaxStack(f);
        InstanceProbeSession g = Fixture.Join();
        Fixture.FormParty(f, g);
        Fixture.UnlockBossRoom(f.EntityId);
        GameMap bossRoom = _world.EnterSameBossRoom(f, g);
        byte[] fBefore = _world.QuerySnapshot(f);
        InventoryView gBefore = _world.QueryInventory(g);
        int fMark = f.Packets.Count;
        int gMark = g.Packets.Count;

        _world.KillBoss(bossRoom, g);
        Fixture.Settle(SettleTicks);
        List<byte[]> fEconomy = EconomyPacketsSince(f, fMark);

        Assert.Equal(InventoryLimits.MaxStack, ViewOf(fBefore).Material);
        Assert.Equal(new[] { PacketID.S_DungeonClearResult }, IdsOf(fEconomy));
        AssertRejectedResult(fEconomy[0], InventoryResult.InventoryFull);
        Assert.Equal(fBefore, _world.QuerySnapshot(f));
        AssertOneSuccessThenSnapshot(g, gMark, gBefore);
    }

    // C4: a·b가 보스를 잡은 뒤 둘이 남은 채 틱이 흐르고, b만 나간 뒤 또 흐른다. 마지막으로 a도 나간다.
    // 빈 복사본의 정리 자체는 World 시험(InstanceMapRetirementTests 등)이 증명한다. 여기서는 클리어된 방도 그 정리로 이어지는지만 본다.
    [Fact]
    public void ClearResult_IsSentOncePerPlayer_WhileSurvivorsStay_AndTheBossDoesNotReturn()
    {
        (InstanceProbeSession a, InstanceProbeSession b) = _world.JoinParty();
        GameMap bossRoom = _world.EnterSameBossRoom(a, b);
        int aMark = a.Packets.Count;
        int bMark = b.Packets.Count;

        _world.KillBoss(bossRoom, a);
        Fixture.Settle(StayTicks);
        int bossesWhileBothStay = BossCount(bossRoom);
        Fixture.MoveThroughPortal(b, portalId: 2);
        Fixture.Settle(StayTicks);
        int bossesWhileOneStays = BossCount(bossRoom);
        bool clearedWhileOneStays = bossRoom.IsStageCleared;
        int aResultsWhileInside = ClearResultsSince(a, aMark).Count;
        Fixture.MoveThroughPortal(a, portalId: 2);
        bool emptyRoomStillTicked = Fixture.IsStillTicked(bossRoom);

        Assert.Equal(1, aResultsWhileInside);
        AssertSuccessResult(Assert.Single(ClearResultsSince(a, aMark)));
        AssertSuccessResult(Assert.Single(ClearResultsSince(b, bMark)));
        Assert.Equal(0, bossesWhileBothStay);
        Assert.Equal(0, bossesWhileOneStays);
        Assert.True(clearedWhileOneStays);
        Assert.False(emptyRoomStillTicked);
    }

    // C5 · 완료조건 6: 보스를 잡을 a는 마을에서 일반 적 하나를 먼저 잡아 둔다. 그 뒤 a·b가 보스방에서 a의 처치로 클리어한다.
    // 클리어 보상과 지금의 보스 처치 보상이 같은 묶음(50·Material 1·CoinPouch 1)이라 상태 비교만으로는 둘을 못 가른다.
    // 그래서 경제 패킷이 결과 하나와 snapshot 하나뿐인지와, 바뀐 양이 묶음 한 번(revision +1, 골드 +50, 100 아님)인지를 함께 본다.
    [Fact]
    public void BossKillerInTheBossRoom_GetsOnlyTheClearReward_WithoutASeparateKillReward()
    {
        InstanceProbeSession a = Fixture.Join();
        _world.KillNormalInTown(a);
        InstanceProbeSession b = Fixture.Join();
        Fixture.FormParty(a, b);
        Fixture.UnlockBossRoom(a.EntityId);
        GameMap bossRoom = _world.EnterSameBossRoom(a, b);
        InventoryView before = _world.QueryInventory(a);
        int mark = a.Packets.Count;

        _world.KillBoss(bossRoom, a);
        Fixture.Settle(SettleTicks);
        List<byte[]> economy = EconomyPacketsSince(a, mark);
        InventoryView after = _world.QueryInventory(a);

        Assert.True(before.Revision > 0, "fixture: 마을 처치 보상으로 경제 상태가 이미 한 번 바뀌어 있어야 한다");
        Assert.Equal(new[] { PacketID.S_DungeonClearResult, PacketID.S_InventorySnapshot }, IdsOf(economy));
        Assert.Equal(new InventoryView(before.Revision + 1, before.Currency + 50, before.Material + 1, before.CoinPouch + 1), after);
    }

    static int BossCount(GameMap map) => map.Enemies.Values.Count(enemy => enemy.Kind == EnemyKind.Boss);

    // goal C2·C3: 결과 Success 하나 뒤에 snapshot 하나. snapshot과 조회는 전보다 revision +1, 골드 +50, Material·CoinPouch 각 +1(goal P5).
    void AssertOneSuccessThenSnapshot(InstanceProbeSession session, int mark, InventoryView before)
    {
        List<byte[]> economy = EconomyPacketsSince(session, mark);
        InventoryView expected = new(before.Revision + 1, before.Currency + 50, before.Material + 1, before.CoinPouch + 1);

        Assert.Equal(new[] { PacketID.S_DungeonClearResult, PacketID.S_InventorySnapshot }, IdsOf(economy));
        AssertSuccessResult(economy[0]);
        Assert.Equal(expected, ViewOf(economy[1]));
        Assert.Equal(expected, _world.QueryInventory(session));
    }
}
