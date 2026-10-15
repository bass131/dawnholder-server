using System.Buffers.Binary;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Tests.Maps;
using Shared.GameData;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Maps.InstanceMapTestWorld;

namespace Dawnholder.Server.GameServer.Tests.Dungeon;

// 던전 클리어 PR 요구 시험(goal 2026-10-10-dungeon-clear-rewards 「던전 클리어 PR의 요구」)이 함께 쓰는 fixture.
// InstanceMapTestWorld의 실제 수신 경로·포탈 표·월드 틱 위에서 보스방 입장, 보스 사망 후처리, 받은 39·36 bytes,
// 인벤토리 조회를 다룬다. 클리어 상태·확인 처리기는 아직 없고 goal이 이름을 정하지 않았으므로 wire와 조회로만 본다.
// 기대값은 goal 리터럴, 공유 보상표(DungeonClearRewards), 시험 입력, 조회의 전후 비교로만 둔다.
internal sealed class DungeonClearTestWorld : IDisposable
{
    // 시험 허용값이지 계약값이 아니다. 결과는 보스 사망 틱의 월드 단계나 그 뒤 맵 job에서, 마을 도착은 다음 틱에 끝난다.
    internal const int SettleTicks = 3;

    // 정착 창보다 길다. 입력 없이도 오는 주기 패킷을 버림 관찰 전에 미리 안다(DungeonClearPacketContractTests와 같은 방법).
    internal const int IdleWindowTicks = 20;

    // goal P2·protocol.md v18: 39번은 머리 4 + 결과 1 + 골드 4 + 세 칸 × 8 = 33바이트다.
    internal const int ClearResultLength = 33;

    internal DungeonClearTestWorld()
    {
        Fixture = new InstanceMapTestWorld();
    }

    internal InstanceMapTestWorld Fixture { get; }

    public void Dispose() => Fixture.Dispose();

    internal static byte[] Confirm(byte reserved = 0) => new C_DungeonClearConfirm { reserved = reserved }.Write().ToArray();

    // goal C6의 버릴 입력. 크기 필드는 수신 framing이 보장하듯 실제 길이와 같다.
    internal static byte[] MalformedConfirm(string variant) => variant switch
    {
        "length-4" => ConfirmFrame(Array.Empty<byte>()),
        "length-6" => ConfirmFrame(new byte[] { 0, 0 }),
        "reserved-1" => Confirm(reserved: 1),
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
    };

    internal static List<byte[]> ClearResultsSince(InstanceProbeSession session, int mark)
        => session.Packets.Skip(mark).Where(packet => IdOf(packet) == PacketID.S_DungeonClearResult).ToList();

    // 결과와 snapshot의 순서만 남긴다. 그 사이의 이동·전투 패킷은 요구 밖이다.
    internal static List<byte[]> EconomyPacketsSince(InstanceProbeSession session, int mark)
        => session.Packets.Skip(mark)
            .Where(packet => IdOf(packet) is PacketID.S_DungeonClearResult or PacketID.S_InventorySnapshot)
            .ToList();

    internal static PacketID[] IdsOf(IEnumerable<byte[]> packets) => packets.Select(IdOf).ToArray();

    // goal C3: 결과 Success, 골드와 아이템은 공유 보상표. 칸 순서는 P2와 클라이언트 처리기가 정하지 않아 순서 없이 본다.
    internal static void AssertSuccessResult(byte[] packet)
    {
        S_DungeonClearResult result = ReadClearResult(packet);
        (int ItemId, int Count)[] slots = SlotsOf(result);
        (int ItemId, int Count)[] expectedItems = DungeonClearRewards.Items
            .Select(item => ((int)item.ItemId, item.Count))
            .OrderBy(item => item.Item1)
            .ToArray();

        Assert.Equal((byte)InventoryResult.Success, result.result);
        Assert.Equal(DungeonClearRewards.Currency, result.currency);
        Assert.Equal(expectedItems, slots.Where(slot => slot != (0, 0)).OrderBy(slot => slot.ItemId).ToArray());
        Assert.Equal(DungeonClearRewards.MaxItemKinds - expectedItems.Length, slots.Count(slot => slot == (0, 0)));
    }

    // goal C3: 거부면 결과 코드만 있고 골드 0, 세 칸 모두 0/0이다.
    internal static void AssertRejectedResult(byte[] packet, InventoryResult expected)
    {
        S_DungeonClearResult result = ReadClearResult(packet);

        Assert.Equal((byte)expected, result.result);
        Assert.Equal(0, result.currency);
        Assert.All(SlotsOf(result), slot => Assert.Equal((0, 0), slot));
    }

    // 조회·푸시 snapshot에서 요구가 말하는 값만 읽는다. 아이템은 칸 위치와 무관하게 개수로 본다.
    internal static InventoryView ViewOf(byte[] snapshot)
    {
        Assert.Equal(PacketID.S_InventorySnapshot, IdOf(snapshot));
        S_InventorySnapshot s = Read<S_InventorySnapshot>(snapshot);
        (int ItemId, int Count)[] slots =
        {
            (s.slot0ItemId, s.slot0Count), (s.slot1ItemId, s.slot1Count), (s.slot2ItemId, s.slot2Count), (s.slot3ItemId, s.slot3Count),
            (s.slot4ItemId, s.slot4Count), (s.slot5ItemId, s.slot5Count), (s.slot6ItemId, s.slot6Count), (s.slot7ItemId, s.slot7Count),
        };
        return new InventoryView(
            s.revision,
            s.currency,
            slots.Where(slot => slot.ItemId == (int)ItemId.Material).Sum(slot => slot.Count),
            slots.Where(slot => slot.ItemId == (int)ItemId.CoinPouch).Sum(slot => slot.Count));
    }

    // goal C6 버림의 관찰: 예외·수신 중 송신 없음, idle 창에 이미 보던 종류 밖의 새 패킷 없음, 연결 유지, 맵 그대로, 조회 인벤토리 같음.
    internal static void AssertDropped(DropObservation observed)
    {
        Assert.Null(observed.Thrown);
        Assert.Equal(0, observed.SentDuringReceive);
        Assert.Subset(observed.IdleIds, observed.AfterIds);
        Assert.False(observed.Closing, "goal C6: 버린 40은 연결을 끊지 않는다");
        Assert.True(observed.MapBefore != null, "fixture: 보낸 사람은 맵 안에 있어야 한다");
        Assert.Same(observed.MapBefore, observed.MapAfter);
        Assert.Equal(observed.InventoryBefore, observed.InventoryAfter);
    }

    // 2인 파티를 실제 초대·수락으로 만들고 퀘스트 처치 적립으로 보스 문을 연다. 파티 진행이 차면 두 사람 모두 해금된다.
    internal (InstanceProbeSession First, InstanceProbeSession Second) JoinParty()
    {
        InstanceProbeSession first = Fixture.Join();
        InstanceProbeSession second = Fixture.Join();
        Fixture.FormParty(first, second);
        Fixture.UnlockBossRoom(first.EntityId);
        return (first, second);
    }

    // 마을 → 사냥터 복사본 → 보스방 복사본. 이미 사냥터에 있으면 보스방 포탈만 지난다.
    internal GameMap EnterBossRoom(InstanceProbeSession session)
    {
        if (CurrentMapOf(session).MapId == MapId.Town) Fixture.MoveThroughPortal(session, portalId: 1);
        return Fixture.MoveThroughPortal(session, portalId: 1);
    }

    internal GameMap EnterSameBossRoom(InstanceProbeSession first, InstanceProbeSession second)
    {
        GameMap room = EnterBossRoom(first);
        GameMap other = EnterBossRoom(second);
        Assert.True(ReferenceEquals(room, other), "fixture: 같은 파티의 두 사람은 같은 보스방 복사본에 있어야 한다");
        return room;
    }

    // 확인 시험의 준비. 결과 수신은 여기서 단정하지 않는다(DungeonClearRewardTests의 몫).
    internal (InstanceProbeSession A, InstanceProbeSession B, GameMap BossRoom) ClearBossRoomAsParty()
    {
        (InstanceProbeSession a, InstanceProbeSession b) = JoinParty();
        GameMap bossRoom = EnterSameBossRoom(a, b);
        KillBoss(bossRoom, a);
        Fixture.Settle(SettleTicks);
        return (a, b, bossRoom);
    }

    // 전투 판정이 아니라 처치 뒤 처리(클리어 판정·지급·결과)가 대상이라, InstanceMapBossQuestResetTests.KillBoss처럼
    // 그 방의 job 안에서 제품 사망 후처리를 직접 부른다. 처치 콜백과 월드 단계는 제품 경로 그대로 같은 월드 틱에서 돈다.
    internal void KillBoss(GameMap bossRoom, InstanceProbeSession killer)
    {
        EnemyEntity boss = Assert.Single(bossRoom.Enemies.Values, enemy => enemy.Kind == EnemyKind.Boss);
        bossRoom.EnqueueJob(() =>
        {
            boss.Hp = 0;
            bossRoom.HandleEnemyDeath(boss, killer.EntityId);
        });
        Fixture.Tick();
        Assert.False(bossRoom.Enemies.ContainsKey(boss.EntityId), "fixture: 보스 사망 후처리가 보스를 빼야 한다");
    }

    // 실제 근접 공격으로 마을 일반 적 하나를 잡는다(ServerMapMoveTests.KillNormalInTown과 같은 서기·정착 순서).
    internal void KillNormalInTown(InstanceProbeSession attacker)
    {
        GameMap town = Fixture.Town;
        town.GetPlayer(attacker.EntityId)!.Position = new Vector2(10f, 0f);
        Fixture.Settle();
        EnemyEntity enemy = town.SpawnEnemy(EnemyKind.Normal, 11f, 0f, maxHp: 1);
        attacker.OnRecvPacket(new C_Attack { targetEntityId = enemy.EntityId, attackerClientTick = (int)(Fixture.LastTick + 1) }.Write());
        Fixture.Tick();
        Assert.False(town.Enemies.ContainsKey(enemy.EntityId), "fixture: 실제 근접 공격 경로가 적을 처치해야 한다");
        Fixture.Settle();
    }

    // 상태를 심지 않고 기존 처치 경로로 Material을 MaxStack까지 채운다(InventoryWireContractTests의 스택 상한 시험과 같은 방법).
    // 일반 처치 하나가 Material 1이므로, 한 번 맞으면 죽는 일반 적 MaxStack마리를 Dash 상자 하나에 두고 실제 Dash 한 번으로 잡는다.
    internal void FillMaterialToMaxStack(InstanceProbeSession knight)
    {
        GameMap town = Fixture.Town;
        float x = town.GetPlayer(knight.EntityId)!.Position.X + CombatConstants.DashBoxHalfX;
        EnemyEntity[] pack = Enumerable.Range(0, InventoryLimits.MaxStack)
            .Select(_ => town.SpawnEnemy(EnemyKind.Normal, x, 0f, maxHp: 1))
            .ToArray();
        knight.OnRecvPacket(new C_SkillUse
        {
            skillId = (byte)SkillId.Dash,
            attackerClientTick = (int)(Fixture.LastTick + 1),
            facing = 1,
        }.Write());
        Fixture.Tick();
        Assert.All(pack, enemy => Assert.False(town.Enemies.ContainsKey(enemy.EntityId), "fixture: Dash 한 번이 모두 잡아야 한다"));
        Fixture.Settle(Constants.DashTravelTicks);
    }

    internal byte[] QuerySnapshot(InstanceProbeSession session)
    {
        int mark = session.Packets.Count;
        session.OnRecvPacket(new C_InventoryRequest { reserved = 0 }.Write());
        Fixture.Settle(SettleTicks);
        return Assert.Single(session.Packets.Skip(mark), packet => IdOf(packet) == PacketID.S_InventorySnapshot);
    }

    internal InventoryView QueryInventory(InstanceProbeSession session) => ViewOf(QuerySnapshot(session));

    // 올바른 40을 보내고 정착한다. 돌려준 표시부터 받은 패킷이 이 확인의 결과다.
    internal int SendConfirm(InstanceProbeSession session)
    {
        int mark = session.Packets.Count;
        session.OnRecvPacket(Confirm());
        Fixture.Settle(SettleTicks);
        return mark;
    }

    // goal C6: 수혜자의 올바른 40은 마을행 이동 하나를 일으킨다.
    internal void AssertMovedToTown(InstanceProbeSession session, int mark)
    {
        byte[] transition = Assert.Single(session.Packets.Skip(mark), packet => IdOf(packet) == PacketID.S_MapTransition);
        Assert.Equal((byte)MapId.Town, Read<S_MapTransition>(transition).destMapId);
        Assert.Same(Fixture.Town, session.CurrentMap);
    }

    internal DropObservation SendAndObserve(InstanceProbeSession sender, byte[] confirm)
    {
        GameMap? mapBefore = sender.CurrentMap;
        byte[] inventoryBefore = QuerySnapshot(sender);
        int idleMark = sender.Packets.Count;
        Fixture.Settle(IdleWindowTicks);
        HashSet<PacketID> idleIds = IdsOf(sender.Packets.Skip(idleMark)).ToHashSet();

        int mark = sender.Packets.Count;
        Exception? thrown = Record.Exception(() => sender.OnRecvPacket(confirm));
        int sentDuringReceive = sender.Packets.Count - mark;
        Fixture.Settle(SettleTicks);
        HashSet<PacketID> afterIds = IdsOf(sender.Packets.Skip(mark)).ToHashSet();
        GameMap? mapAfter = sender.CurrentMap;
        bool closing = sender.IsClosing;
        byte[] inventoryAfter = QuerySnapshot(sender);
        return new DropObservation(thrown, sentDuringReceive, idleIds, afterIds, closing, mapBefore, mapAfter, inventoryBefore, inventoryAfter);
    }

    static S_DungeonClearResult ReadClearResult(byte[] packet)
    {
        Assert.Equal(PacketID.S_DungeonClearResult, IdOf(packet));
        Assert.Equal(ClearResultLength, packet.Length);
        Assert.Equal(ClearResultLength, BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(0, 2)));
        return Read<S_DungeonClearResult>(packet);
    }

    static (int ItemId, int Count)[] SlotsOf(S_DungeonClearResult result)
        => new[]
        {
            (result.reward0ItemId, result.reward0Count),
            (result.reward1ItemId, result.reward1Count),
            (result.reward2ItemId, result.reward2Count),
        };

    static byte[] ConfirmFrame(byte[] payload)
    {
        byte[] packet = new byte[4 + payload.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(0, 2), (ushort)packet.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(2, 2), (ushort)PacketID.C_DungeonClearConfirm);
        payload.CopyTo(packet, 4);
        return packet;
    }

    internal readonly record struct InventoryView(uint Revision, int Currency, int Material, int CoinPouch);

    internal sealed record DropObservation(
        Exception? Thrown,
        int SentDuringReceive,
        HashSet<PacketID> IdleIds,
        HashSet<PacketID> AfterIds,
        bool Closing,
        GameMap? MapBefore,
        GameMap? MapAfter,
        byte[] InventoryBefore,
        byte[] InventoryAfter);
}
