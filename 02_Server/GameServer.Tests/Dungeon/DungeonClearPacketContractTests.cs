using System.Buffers.Binary;
using System.Net;
using System.Reflection;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Handlers;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.GameServer.Tests.Maps;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Dungeon;

// Dungeon goal P2·P3·P4 (01_Phases/goals/2026-10-10-dungeon-clear-rewards/goal.md 「던전 패킷·창 PR의 요구」),
// written before the packets exist. The generated types are looked up by name in the Shared assembly: a direct
// reference to S_DungeonClearResult or to a new PacketID value would stop this whole test assembly from compiling
// until the generator runs. Expected bytes come from the independent encoder below (goal P2 field order,
// LittleEndian, 4-byte header), never from the generator or a product calculation.
[Collection("GameWorldRegistryTests")]
public sealed class DungeonClearPacketContractTests : IDisposable
{
    // Test allowance, not a contract value: an inventory reply leaves from the world tick or a map job.
    const int SettleTicks = 3;

    // Longer than the settle window, so a periodic packet that also appears without any input is known beforehand.
    const int IdleWindowTicks = 20;

    static readonly IPEndPoint Endpoint = new(IPAddress.Loopback, 0);
    readonly GameWorld _world = new(new Dictionary<MapId, (MapTerrain?, MapContent?)>());
    long _tick;

    // Goal P2 boundaries: the reward table's own shape, gold 0 and the cap with empty or full slots, and the two
    // rejection results (P3: only Success, CurrencyCap and InventoryFull of the shared InventoryResult are used).
    // The third full slot uses distinct bytes so a swapped or shifted field shows in the byte comparison.
    public static TheoryData<byte, int, int[]> ClearResultCases => new()
    {
        { (byte)InventoryResult.Success, 50, new[] { 1, 1, 2, 1, 0, 0 } },
        { (byte)InventoryResult.Success, 0, new[] { 0, 0, 0, 0, 0, 0 } },
        { (byte)InventoryResult.Success, InventoryLimits.MaxCurrency, new[] { 1, 99, 2, 98, 0x01020304, 0x0A0B0C0D } },
        { (byte)InventoryResult.CurrencyCap, 0, new[] { 0, 0, 0, 0, 0, 0 } },
        { (byte)InventoryResult.InventoryFull, 0, new[] { 0, 0, 0, 0, 0, 0 } },
    };

    public void Dispose() => _world.Stop();

    [Theory]
    [MemberData(nameof(ClearResultCases))]
    public void ClearResult_WritesAndReadsTheGoalFieldOrder_AsThirtyThreeLittleEndianBytes(byte result, int currency, int[] slots)
    {
        Type type = RequirePacketType("S_DungeonClearResult");
        byte[] expected = Wire.ClearResult(result, currency, slots);
        IPacket packet = NewPacket(type);
        SetField(packet, "result", typeof(byte), result);
        SetField(packet, "currency", typeof(int), currency);
        for (int i = 0; i < Wire.RewardSlots; i++)
        {
            SetField(packet, $"reward{i}ItemId", typeof(int), slots[i * 2]);
            SetField(packet, $"reward{i}Count", typeof(int), slots[(i * 2) + 1]);
        }

        byte[] written = packet.Write().ToArray();
        IPacket decoded = NewPacket(type);
        decoded.Read(new ArraySegment<byte>(expected));

        Assert.Equal(Wire.ClearResultId, packet.Protocol);
        Assert.Equal(Wire.ClearResultLength, written.Length);
        Assert.Equal(expected, written);
        Assert.Equal(result, GetField(decoded, "result"));
        Assert.Equal(currency, GetField(decoded, "currency"));
        for (int i = 0; i < Wire.RewardSlots; i++)
        {
            Assert.Equal(slots[i * 2], GetField(decoded, $"reward{i}ItemId"));
            Assert.Equal(slots[(i * 2) + 1], GetField(decoded, $"reward{i}Count"));
        }
    }

    // The reward table's shape written out byte by byte, which also pins the encoder above to the goal's table.
    [Fact]
    public void ClearResult_ForFiftyGoldOneMaterialOneCoinPouch_WritesTheseThirtyThreeBytes()
    {
        byte[] literal =
        {
            0x21, 0x00, 0x27, 0x00,
            0x00,
            0x32, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x02, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };
        byte[] encoded = Wire.ClearResult((byte)InventoryResult.Success, 50, new[] { 1, 1, 2, 1, 0, 0 });
        // Checked first, so the oracle is confirmed even while the generated type is still missing.
        Assert.Equal(literal, encoded);
        Type type = RequirePacketType("S_DungeonClearResult");
        IPacket packet = NewPacket(type);
        SetField(packet, "result", typeof(byte), (byte)InventoryResult.Success);
        SetField(packet, "currency", typeof(int), 50);
        SetField(packet, "reward0ItemId", typeof(int), 1);
        SetField(packet, "reward0Count", typeof(int), 1);
        SetField(packet, "reward1ItemId", typeof(int), 2);
        SetField(packet, "reward1Count", typeof(int), 1);

        byte[] written = packet.Write().ToArray();

        Assert.Equal(literal, written);
    }

    [Fact]
    public void ClearConfirm_WritesAndReadsFiveBytes_WithReservedZero()
    {
        byte[] expected = Wire.ClearConfirm(reserved: 0);
        // Checked first, so the oracle is confirmed even while the generated type is still missing.
        Assert.Equal(new byte[] { 0x05, 0x00, 0x28, 0x00, 0x00 }, expected);
        Type type = RequirePacketType("C_DungeonClearConfirm");
        IPacket packet = NewPacket(type);
        SetField(packet, "reserved", typeof(byte), (byte)0);

        byte[] written = packet.Write().ToArray();
        IPacket decoded = NewPacket(type);
        decoded.Read(new ArraySegment<byte>(expected));

        Assert.Equal(Wire.ClearConfirmId, packet.Protocol);
        Assert.Equal(expected, written);
        Assert.Equal((byte)0, GetField(decoded, "reserved"));
    }

    // Dungeon goal C6: the clear PR registers a 40 handler, and a selected session in Town is not a beneficiary of a
    // cleared boss room copy, so its 40 is still dropped. The registration check keeps the drop from passing vacuously.
    [Fact]
    public void ConfirmFromASelectedSession_IsDroppedWithoutReplyCloseOrInventoryChange()
    {
        bool handlerRegistered = HandlerRegistry.TryGet((PacketID)Wire.ClearConfirmId, out _);
        ProbeSession session = JoinAndEnter();
        GainGolemReward(session);
        byte[] before = QuerySnapshot(session);
        int idleMark = session.Packets.Count;
        Settle(IdleWindowTicks);
        HashSet<ushort> idleIds = session.IdsSince(idleMark);

        int mark = session.Packets.Count;
        Exception? thrown = Record.Exception(() => session.OnRecvPacket(Wire.ClearConfirm(reserved: 0)));
        int sentDuringReceive = session.Packets.Count - mark;
        Settle();
        HashSet<ushort> afterIds = session.IdsSince(mark);
        byte[] after = QuerySnapshot(session);

        Assert.True(handlerRegistered, "C6 the server registers a handler for 40");
        Assert.Null(thrown);
        Assert.Equal(0, sentDuringReceive);
        Assert.Subset(idleIds, afterIds);
        Assert.DoesNotContain(Wire.ClearResultId, afterIds);
        Assert.False(session.IsClosing, "C6 a 40 from a non-beneficiary in Town is dropped without closing the connection");
        Assert.Equal(before, after);
    }

    static Type RequirePacketType(string name)
    {
        Type? type = typeof(PacketID).Assembly.GetType("Shared.Protocol." + name);
        Assert.True(type is not null, $"goal P1/P2: the generated packet Shared.Protocol.{name} does not exist yet");
        return type!;
    }

    static IPacket NewPacket(Type type) => (IPacket)Activator.CreateInstance(type)!;

    // The field names and types are the goal's fixed public names (「미리 정하는 공개 이름」) and P2 wire types.
    static void SetField(IPacket packet, string name, Type wireType, object value)
    {
        FieldInfo? field = packet.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
        Assert.True(field is not null, $"goal public name: {packet.GetType().Name}.{name} is a public field");
        Assert.Equal(wireType, field!.FieldType);
        field.SetValue(packet, value);
    }

    static object? GetField(IPacket packet, string name)
        => packet.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance)!.GetValue(packet);

    void Tick() => LifecycleTestWorld.Tick(_world, ++_tick);

    void Settle(int ticks = SettleTicks)
    {
        for (int i = 0; i < ticks; i++)
        {
            Tick();
        }
    }

    // Real handshake and class selection bytes, then one world tick to enter Town.
    ProbeSession JoinAndEnter()
    {
        ProbeSession session = new(_world);
        session.OnConnected(Endpoint);
        session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        session.OnRecvPacket(new C_CharacterSelect { characterClass = (byte)CharacterClass.Knight }.Write());
        Tick();
        Assert.True(session.EntityId >= 0, "fixture: the real handshake and class selection must enter the world");
        return session;
    }

    // A non-empty inventory, so a handler that granted or reset anything would show in the snapshot comparison.
    void GainGolemReward(ProbeSession session)
    {
        EnemyEntity golem = _world.Map.SpawnEnemy(EnemyKind.Golem, 1f, 0f, maxHp: 1);
        session.OnRecvPacket(new C_Attack { targetEntityId = golem.EntityId, attackerClientTick = (int)(_tick + 1) }.Write());
        Tick();
        Settle();
        Assert.False(_world.Map.Enemies.ContainsKey(golem.EntityId), "fixture: the real melee path must kill the golem");
    }

    byte[] QuerySnapshot(ProbeSession session)
    {
        int mark = session.Packets.Count;
        session.OnRecvPacket(new C_InventoryRequest { reserved = 0 }.Write());
        Settle();
        ushort snapshotId = (ushort)PacketID.S_InventorySnapshot;
        return Assert.Single(session.Packets.Skip(mark), packet => Wire.IdOf(packet) == snapshotId);
    }

    sealed class ProbeSession : GameSession
    {
        internal readonly List<byte[]> Packets = new();

        internal ProbeSession(GameWorld world) : base(world) { }

        public override void Send(ArraySegment<byte> packet) => Packets.Add(packet.ToArray());

        public override void Disconnect() => OnDisconnected(Endpoint);

        internal HashSet<ushort> IdsSince(int mark) => Packets.Skip(mark).Select(Wire.IdOf).ToHashSet();
    }

    // Independent oracle for goal P2: [ushort size incl. header][ushort id], then the fields in goal order,
    // LittleEndian. 39 = result, gold, three (itemId, count) slots; 40 = one reserved byte.
    static class Wire
    {
        internal const ushort ClearResultId = 39;
        internal const ushort ClearConfirmId = 40;
        internal const int ClearResultLength = 33;
        internal const int RewardSlots = 3;

        internal static byte[] ClearResult(byte result, int currency, int[] slots)
        {
            byte[] payload = new byte[ClearResultLength - 4];
            payload[0] = result;
            BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(1, 4), currency);
            for (int i = 0; i < slots.Length; i++)
            {
                BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(5 + (i * 4), 4), slots[i]);
            }
            return Frame(ClearResultId, payload);
        }

        internal static byte[] ClearConfirm(byte reserved) => Frame(ClearConfirmId, new[] { reserved });

        internal static ushort IdOf(byte[] packet) => BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(2, 2));

        static byte[] Frame(ushort id, byte[] payload)
        {
            byte[] packet = new byte[4 + payload.Length];
            BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(0, 2), (ushort)packet.Length);
            BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(2, 2), id);
            payload.CopyTo(packet, 4);
            return packet;
        }
    }
}
