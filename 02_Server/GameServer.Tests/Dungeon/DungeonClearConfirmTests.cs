using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Party;
using Dawnholder.Server.GameServer.Tests.Maps;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Dungeon.DungeonClearTestWorld;
using static Dawnholder.Server.GameServer.Tests.Maps.InstanceMapTestWorld;

namespace Dawnholder.Server.GameServer.Tests.Dungeon;

// 보상 창 「확인」(40번)의 수락·버림(C6)과 마을 도착(C7).
// 요구: goal 2026-10-10-dungeon-clear-rewards 「던전 클리어 PR의 요구」 C6·C7, 완료조건 7의 서버 부분. 구현 전에 쓴 시험이다.
// 지금 서버는 40번을 등록하지 않아 무엇이든 버린다. 그래서 「40이 버려진다」는 단정은 언제나 같은 시험 안에서 뒤이은 수혜자의
// 올바른 40이 마을 이동을 일으키는 단정과 짝을 짓는다. 그래야 처리기가 있고, 버린 입력 뒤에도 수혜 표시가 남았다는 것까지 보인다.
[Collection("GameWorldRegistryTests")]
public sealed class DungeonClearConfirmTests : IDisposable
{
    readonly DungeonClearTestWorld _world = new();

    InstanceMapTestWorld Fixture => _world.Fixture;

    public static TheoryData<string> MalformedConfirms => new() { "length-4", "length-6", "reserved-1" };

    public void Dispose() => _world.Dispose();

    // C6: 파티 a·b가 보스를 잡은 뒤 a만 올바른 40을 보낸다.
    [Fact]
    public void BeneficiarysConfirm_MovesOnlyThatPlayerToTown()
    {
        (InstanceProbeSession a, InstanceProbeSession b, GameMap bossRoom) = _world.ClearBossRoomAsParty();
        int bMark = b.Packets.Count;

        int aMark = _world.SendConfirm(a);

        _world.AssertMovedToTown(a, aMark);
        Assert.Null(bossRoom.GetPlayer(a.EntityId));
        Assert.Same(bossRoom, b.CurrentMap);
        Assert.DoesNotContain(b.Packets.Skip(bMark), packet => IdOf(packet) == PacketID.S_MapTransition);
    }

    // C7 · 완료조건 7(서버 부분): 파티 a·b가 보스를 잡은 뒤 a가 확인한다. 도착은 World 진입점의 기본 입장 지점이다.
    [Fact]
    public void ConfirmedArrival_IsTheTownSpawn_WithInventoryAndPartyKept()
    {
        (InstanceProbeSession a, InstanceProbeSession b, GameMap _) = _world.ClearBossRoomAsParty();
        byte[] inventoryBefore = _world.QuerySnapshot(a);
        int partyBefore = PartyIdOf(a);

        int mark = _world.SendConfirm(a);
        byte[] transition = Assert.Single(a.Packets.Skip(mark), packet => IdOf(packet) == PacketID.S_MapTransition);
        S_MapTransition arrived = Read<S_MapTransition>(transition);
        PartyState? partyAfter = Fixture.World.Party.GetPartyByEntity(a.EntityId);

        Assert.Equal((byte)MapId.Town, arrived.destMapId);
        Assert.Equal(TownSpawnX, arrived.spawnX);
        Assert.Equal(0f, arrived.spawnY);
        Assert.Same(Fixture.Town, a.CurrentMap);
        Assert.Equal(inventoryBefore, _world.QuerySnapshot(a));
        Assert.Equal(partyBefore, partyAfter?.PartyId);
        Assert.Equal(Ids(a, b), partyAfter!.Members.OrderBy(id => id));
    }

    // C6: 수혜자 a가 틀린 40을 보낸 뒤, 같은 a가 올바른 40을 보낸다.
    [Theory]
    [MemberData(nameof(MalformedConfirms))]
    public void MalformedConfirm_IsDropped_ThenTheSameBeneficiarysValidConfirmMoves(string variant)
    {
        (InstanceProbeSession a, InstanceProbeSession _, GameMap _) = _world.ClearBossRoomAsParty();

        DropObservation dropped = _world.SendAndObserve(a, MalformedConfirm(variant));
        int mark = _world.SendConfirm(a);

        AssertDropped(dropped);
        _world.AssertMovedToTown(a, mark);
    }

    // C6: 보스가 살아 있는 보스방에서 a가 40을 보낸다. 이어서 a가 보스를 잡고 다시 보낸다.
    [Fact]
    public void ConfirmBeforeTheBossDies_IsDropped_ThenAfterTheClearTheSamePlayersConfirmMoves()
    {
        (InstanceProbeSession a, InstanceProbeSession b) = _world.JoinParty();
        GameMap bossRoom = _world.EnterSameBossRoom(a, b);

        DropObservation dropped = _world.SendAndObserve(a, Confirm());
        _world.KillBoss(bossRoom, a);
        Fixture.Settle(SettleTicks);
        int mark = _world.SendConfirm(a);

        AssertDropped(dropped);
        _world.AssertMovedToTown(a, mark);
    }

    // C6: a가 보스를 잡은 뒤 사냥터에서 기다리던 같은 파티 b가 들어와 40을 보낸다. 이어서 수혜자 a가 40을 보낸다.
    [Fact]
    public void ConfirmFromAPlayerWhoEnteredAfterTheClear_IsDropped_WhileTheBeneficiarysConfirmMoves()
    {
        (InstanceProbeSession a, InstanceProbeSession b) = _world.JoinParty();
        Fixture.MoveThroughPortal(b, portalId: 1);
        GameMap bossRoom = _world.EnterBossRoom(a);
        _world.KillBoss(bossRoom, a);
        Fixture.Settle(SettleTicks);
        GameMap entered = _world.EnterBossRoom(b);

        DropObservation dropped = _world.SendAndObserve(b, Confirm());
        int mark = _world.SendConfirm(a);

        Assert.Same(bossRoom, entered);
        AssertDropped(dropped);
        _world.AssertMovedToTown(a, mark);
    }

    // C6: 클리어 뒤 수혜자 b는 사냥터 복사본으로 나가 40을 보내고, 마을의 c도 40을 보낸다. 이어서 보스방에 남은 수혜자 a가 보낸다.
    [Fact]
    public void ConfirmFromOutsideTheClearedBossRoom_IsDropped_WhileTheBeneficiaryInsideMoves()
    {
        InstanceProbeSession c = Fixture.Join();
        (InstanceProbeSession a, InstanceProbeSession b, GameMap _) = _world.ClearBossRoomAsParty();
        GameMap hunting = Fixture.MoveThroughPortal(b, portalId: 2);

        DropObservation fromHunting = _world.SendAndObserve(b, Confirm());
        DropObservation fromTown = _world.SendAndObserve(c, Confirm());
        int mark = _world.SendConfirm(a);

        Assert.Equal(MapId.HuntingGround, hunting.MapId);
        AssertDropped(fromHunting);
        Assert.Same(hunting, fromHunting.MapAfter);
        AssertDropped(fromTown);
        Assert.Same(Fixture.Town, fromTown.MapAfter);
        _world.AssertMovedToTown(a, mark);
    }

    // C6: 핸드셰이크만 마친(캐릭터 선택 전) 세션 p가 40을 보낸다. 같은 월드에서 수혜자 a의 40은 이동을 일으킨다.
    [Fact]
    public void ConfirmBeforeCharacterSelect_IsDroppedWithoutReplyOrClose_WhileABeneficiarysConfirmMoves()
    {
        (InstanceProbeSession a, InstanceProbeSession _, GameMap _) = _world.ClearBossRoomAsParty();
        InstanceProbeSession p = new(Fixture.World);
        p.OnConnected(InstanceProbeSession.Endpoint);
        p.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        Fixture.Settle(SettleTicks);
        int pMark = p.Packets.Count;

        Exception? thrown = Record.Exception(() => p.OnRecvPacket(Confirm()));
        Fixture.Settle(IdleWindowTicks);
        int aMark = _world.SendConfirm(a);

        Assert.Null(thrown);
        Assert.Empty(p.Packets.Skip(pMark));
        Assert.False(p.IsClosing, "goal C6: 선택 전 세션의 40은 연결을 끊지 않고 버린다");
        Assert.Null(p.CurrentMap);
        _world.AssertMovedToTown(a, aMark);
    }

    // C6: 수혜자 a가 같은 틱에 40을 두 번 보내고, 마을에 도착한 뒤 한 번 더 보낸다.
    [Fact]
    public void RepeatedConfirms_MoveTheBeneficiaryOnce_AndAConfirmAfterArrivalChangesNothing()
    {
        (InstanceProbeSession a, InstanceProbeSession _, GameMap _) = _world.ClearBossRoomAsParty();
        int mark = a.Packets.Count;

        a.OnRecvPacket(Confirm());
        a.OnRecvPacket(Confirm());
        Fixture.Settle(SettleTicks);
        bool closedByTheSecond = a.IsClosing;
        DropObservation afterArrival = _world.SendAndObserve(a, Confirm());

        Assert.False(closedByTheSecond, "goal C6: 같은 틱의 두 번째 40은 연결을 끊지 않는다");
        _world.AssertMovedToTown(a, mark);
        AssertDropped(afterArrival);
        Assert.Same(Fixture.Town, afterArrival.MapAfter);
    }

    int PartyIdOf(InstanceProbeSession session)
    {
        PartyState? party = Fixture.World.Party.GetPartyByEntity(session.EntityId);
        Assert.True(party != null, "fixture: 확인 전 파티가 있어야 한다");
        return party!.PartyId;
    }
}
