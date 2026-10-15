using Dawnholder.Server.GameServer.Dungeon;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Tests.Maps;
using Shared.GameData;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Dungeon.DungeonClearTestWorld;
using static Dawnholder.Server.GameServer.Tests.Maps.InstanceMapTestWorld;

namespace Dawnholder.Server.GameServer.Tests.Dungeon;

// 선행 시험이 실제 경로로 만들지 못해 코드 대조로 남긴 거부 길(goal 2026-10-10-dungeon-clear-rewards 「클리어 선행 시험」
// 「시험에 없는 요구」): 마을 이동이 Accepted가 아닐 때 수혜 표시가 남는지(C6), 거부 39의 bytes(C3), 39에 실을 수 없는
// 결과(P3과 「리드가 정한 것」의 RevisionExhausted). 기대 bytes는 goal P2 필드 순서의 리터럴이다.
[Collection("GameWorldRegistryTests")]
public sealed class DungeonClearRefusalTests : IDisposable
{
    readonly DungeonClearTestWorld _world = new();

    InstanceMapTestWorld Fixture => _world.Fixture;

    public static TheoryData<InventoryResult> RejectedGrants => new() { InventoryResult.CurrencyCap, InventoryResult.InventoryFull };

    public static TheoryData<InventoryResult> ResultsWithoutAClearCode => new()
    {
        InventoryResult.Stale, InventoryResult.NotOwned, InventoryResult.NotUsable, InventoryResult.RevisionExhausted,
    };

    public void Dispose() => _world.Dispose();

    // C6 「Accepted가 아니면 수혜자 표시를 남겨 다시 누를 수 있다」: 확인 job의 앞 검사를 모두 통과한 수혜자가 World 진입점에서
    // 거부되는 경우다. 실제 경로에서 그 거부는 연결 종료 경합뿐이라 결정적으로 만들 수 없다. 그래서 진입점이 읽는 이동 중 표시 하나만
    // 기존 internal hook(SetMigrating)으로 그 틱 동안 세워 AlreadyMigrating 거부를 일으킨다. 맵·소유·클리어 상태는 제품 경로 그대로다.
    [Fact]
    public void ConfirmRefusedByTheTownMove_KeepsTheBeneficiaryPending_SoTheNextConfirmMoves()
    {
        (InstanceProbeSession a, InstanceProbeSession _, GameMap bossRoom) = _world.ClearBossRoomAsParty();
        GameMap? submittedTo = a.CurrentMap;
        int mark = a.Packets.Count;

        a.OnRecvPacket(Confirm());
        a.SetMigrating(1);
        Fixture.Tick();
        a.SetMigrating(0);
        bool stillInside = bossRoom.GetPlayer(a.EntityId) != null;
        int transitionsAfterRefusal = a.Packets.Skip(mark).Count(packet => IdOf(packet) == PacketID.S_MapTransition);
        bool closedByRefusal = a.IsClosing;
        int retryMark = _world.SendConfirm(a);

        Assert.Same(bossRoom, submittedTo);
        Assert.True(stillInside, "goal C6: 거부된 이동은 수혜자를 보스방에 둔다");
        Assert.Equal(0, transitionsAfterRefusal);
        Assert.False(closedByRefusal, "goal C6: 거부된 이동은 연결을 끊지 않는다");
        _world.AssertMovedToTown(a, retryMark);
    }

    // C3 「거부면 결과 코드만 있고 골드 0·빈 칸」을 서버가 만드는 39 bytes로 본다. 골드 상한 10억은 wire 경로로 만들 수 없어
    // (선행 시험 보고 5절) 지급 job이 쓰는 조립 함수를 직접 부른다. 크기 33, 번호 39, 결과, 나머지 28바이트 0이다.
    [Theory]
    [MemberData(nameof(RejectedGrants))]
    public void RejectedClearResult_CarriesOnlyItsCode_InThirtyThreeBytes(InventoryResult rejected)
    {
        byte[] expected = new byte[33];
        expected[0] = 33;
        expected[2] = 39;
        expected[4] = (byte)rejected;

        byte[] written = DungeonClearPackets.Result(rejected).ToArray();

        Assert.Equal(expected, written);
    }

    // P3: 39는 Success·CurrencyCap·InventoryFull만 싣는다. RevisionExhausted는 지급 job이 39를 만들기 전에 기록만 하고 돌아가며
    // (「리드가 정한 것」), 조립 함수도 이 결과들을 다른 코드로 바꾸지 않고 거부한다.
    [Theory]
    [MemberData(nameof(ResultsWithoutAClearCode))]
    public void ResultOutsideTheClearSet_IsNeverWrittenAsAClearResult(InventoryResult outcome)
    {
        Exception? refused = Record.Exception(() => DungeonClearPackets.Result(outcome));

        Assert.NotNull(refused);
    }
}
