using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Tests.Maps;
using Shared.GameData;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Dungeon.DungeonClearTestWorld;

namespace Dawnholder.Server.GameServer.Tests.Dungeon;

// 클리어된 보스방 복사본이 살아 있는 동안의 다시 들어옴과 다시 클리어(goal 2026-10-10-dungeon-clear-rewards C4·C6, 완료조건 5·6).
// C4의 전제 「빈 복사본은 그 틱 끝에 정리된다」에는 예외가 있다. 빈 방도 입장 대기가 있으면 남고(InstanceMapRegistry.RemoveIdle),
// 그 방의 맵 틱은 보스를 다시 내며 클리어 표시를 지운다(GameMap.MaybeRespawnBoss). 두 번째 시험은 그 길이 실제 월드 틱 순서로
// 생기는 것을 보이고, 그 뒤의 처치를 새 클리어로 다루는 지금 계약을 고정한다(완료조건 5 「보스 사망 틱에 … 정확히 한 번」,
// server.md 「빈 방 보스 재출현 뒤 같은 복사본에서 다시 클리어하면 새 수혜자 집합으로 교체한다」). 클리어 보상을 복사본마다
// 한 번으로 정하면 그 시험의 마지막 단정 묶음이 바뀐다.
[Collection("GameWorldRegistryTests")]
public sealed class DungeonClearRepeatClearTests : IDisposable
{
    readonly DungeonClearTestWorld _world = new();

    InstanceMapTestWorld Fixture => _world.Fixture;

    public void Dispose() => _world.Dispose();

    // C4·C6: a·b가 클리어한 뒤 a는 방에 남고, b는 사냥터 복사본으로 나갔다가 같은 보스방 복사본에 다시 들어온다.
    // 방이 비지 않아 보스는 다시 나오지 않는다. b는 두 번째 결과를 받지 않고, 아직 확인하지 않은 수혜자라 40으로 마을에 간다.
    // 보스 문 조건은 이 시험의 대상이 아니라 입력으로 채운다.
    [Fact]
    public void BeneficiaryWhoWalksOutAndBack_GetsNoSecondResult_AndCanStillConfirm()
    {
        (InstanceProbeSession a, InstanceProbeSession b, GameMap bossRoom) = _world.ClearBossRoomAsParty();

        Fixture.MoveThroughPortal(b, portalId: 2);
        Fixture.UnlockBossRoom(b.EntityId);
        GameMap returned = Fixture.MoveThroughPortal(b, portalId: 1);
        Fixture.Settle(SettleTicks);
        int bossesAfterReturn = BossCount(bossRoom);
        int confirmMark = _world.SendConfirm(b);

        Assert.Same(bossRoom, returned);
        Assert.Equal(0, bossesAfterReturn);
        AssertSuccessResult(Assert.Single(ClearResultsSince(b, 0)));
        _world.AssertMovedToTown(b, confirmMark);
        Assert.Same(bossRoom, a.CurrentMap);
    }

    // C4 전제의 예외: a·b가 클리어한 뒤 b는 확인으로 마을에 가고, 보스방보다 늦게 만들어진 새 사냥터 복사본의 보스방 포탈 앞에 선다.
    // 한 월드 틱 안에서 보스방 job이 a의 확인으로 방을 비우고 같은 맵 틱의 재출현이 보스를 낸다. 그 뒤에 도는 사냥터 job이 b의
    // 입장을 대기에 올리므로 빈 보스방은 정리되지 않는다. b는 같은 복사본에 들어와 새 보스를 잡는다.
    [Fact]
    public void BossRoomKeptByAnArrivingPlayer_RespawnsItsBoss_AndTheNextKillRewardsThatRoomOnce()
    {
        (InstanceProbeSession a, InstanceProbeSession b, GameMap bossRoom) = _world.ClearBossRoomAsParty();
        _world.AssertMovedToTown(b, _world.SendConfirm(b));
        Fixture.UnlockBossRoom(b.EntityId);
        GameMap hunting = Fixture.MoveThroughPortal(b, portalId: 1);
        InventoryView bBefore = _world.QueryInventory(b);
        int aMark = a.Packets.Count;
        int bMark = b.Packets.Count;

        a.OnRecvPacket(Confirm());
        Fixture.EnterPortal(b, portalId: 1);
        Fixture.Tick();
        int playersAfterTheTick = bossRoom.Players.Count;
        int bossesAfterTheTick = BossCount(bossRoom);
        bool clearedAfterTheTick = bossRoom.IsStageCleared;
        Fixture.Settle(SettleTicks);
        GameMap? bArrivedIn = b.CurrentMap;
        _world.KillBoss(bossRoom, b);
        Fixture.Settle(SettleTicks);
        List<byte[]> bEconomy = EconomyPacketsSince(b, bMark);
        int confirmMark = _world.SendConfirm(b);

        Assert.Equal(MapId.HuntingGround, hunting.MapId);
        Assert.Same(Fixture.Town, a.CurrentMap);
        Assert.Equal(0, playersAfterTheTick);
        Assert.Equal(1, bossesAfterTheTick);
        Assert.False(clearedAfterTheTick);
        Assert.Same(bossRoom, bArrivedIn);

        // 새 클리어: 방 안의 b만 결과 하나와 snapshot 하나를 받는다. 마을에 간 첫 클리어 수혜자 a는 더 받지 않는다.
        // b는 같은 복사본 객체에서 두 클리어의 수혜자였으므로 결과가 모두 둘이다.
        Assert.Equal(new[] { PacketID.S_DungeonClearResult, PacketID.S_InventorySnapshot }, IdsOf(bEconomy));
        AssertSuccessResult(bEconomy[0]);
        Assert.Equal(new InventoryView(bBefore.Revision + 1, bBefore.Currency + 50, bBefore.Material + 1, bBefore.CoinPouch + 1), ViewOf(bEconomy[1]));
        Assert.Empty(ClearResultsSince(a, aMark));
        Assert.Single(ClearResultsSince(a, 0));
        Assert.Equal(2, ClearResultsSince(b, 0).Count);
        _world.AssertMovedToTown(b, confirmMark);
    }

    static int BossCount(GameMap map) => map.Enemies.Values.Count(enemy => enemy.Kind == EnemyKind.Boss);
}
