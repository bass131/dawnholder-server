using System.Reflection;
using Dawnholder.Server.GameServer.Items;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Items;

public sealed class InventorySnapshotBoundaryTests
{
    // Protocol v23's wire table fixes these offsets independently of the generated serializer.
    // Undefined item/count values exercise all four bytes, without claiming they are legal game state.
    [Theory]
    [InlineData("slot8", 76)]
    [InlineData("slot9", 84)]
    [InlineData("slot10", 92)]
    [InlineData("slot11", 100)]
    [InlineData("slot12", 108)]
    [InlineData("slot13", 116)]
    [InlineData("slot14", 124)]
    [InlineData("slot15", 132)]
    [InlineData("slot16", 140)]
    [InlineData("slot17", 148)]
    [InlineData("slot18", 156)]
    [InlineData("slot19", 164)]
    [InlineData("slot20", 172)]
    [InlineData("slot21", 180)]
    [InlineData("slot22", 188)]
    [InlineData("slot23", 196)]
    public void AppendedSlot_ReadAndWriteMatchTheIndependentOffset(string fieldPrefix, int byteOffset)
    {
        FieldInfo? itemField = typeof(S_InventorySnapshot).GetField(fieldPrefix + "ItemId");
        FieldInfo? countField = typeof(S_InventorySnapshot).GetField(fieldPrefix + "Count");
        Assert.NotNull(itemField);
        Assert.NotNull(countField);
        S_InventorySnapshot packet = new();
        itemField.SetValue(packet, 0x10203040);
        countField.SetValue(packet, 0x5060707F);

        byte[] expected = new byte[204];
        new byte[] { 0xCC, 0, 0x24, 0 }.CopyTo(expected, 0);
        new byte[] { 0x40, 0x30, 0x20, 0x10, 0x7F, 0x70, 0x60, 0x50 }.CopyTo(expected, byteOffset);
        Assert.Equal(expected, packet.Write().ToArray());

        byte[] receiveBuffer = Enumerable.Repeat((byte)0xA5, 230).ToArray();
        expected.CopyTo(receiveBuffer, 13);
        S_InventorySnapshot decoded = new();
        decoded.Read(new ArraySegment<byte>(receiveBuffer, 13, 204));
        Assert.Equal(0x10203040, Assert.IsType<int>(itemField.GetValue(decoded)));
        Assert.Equal(0x5060707F, Assert.IsType<int>(countField.GetValue(decoded)));
    }

    [Fact]
    public void AuthoritySnapshot_SortsTwoStacksAndPadsEveryRemainingSlotWithZeroes()
    {
        InventorySlot[] supplied =
        {
            new(ItemId.CoinPouch, 41), new(ItemId.None, 0), new(ItemId.Material, 63),
        };
        InventoryState state = new(0xA1B2C3D4u, 314159, supplied);
        supplied[0] = new InventorySlot(ItemId.Material, 1);

        byte[] expected = new byte[204];
        new byte[]
        {
            0xCC, 0, 0x24, 0, 0xD4, 0xC3, 0xB2, 0xA1, 0x2F, 0xCB, 0x04, 0,
            1, 0, 0, 0, 63, 0, 0, 0, 2, 0, 0, 0, 41, 0, 0, 0,
        }.CopyTo(expected, 0);

        byte[] actual = InventoryPackets.Snapshot(state).ToArray();
        Assert.Equal(204, actual.Length);
        Assert.Equal(expected, actual);
        Assert.Equal(24, state.Slots.Count);
        Assert.Equal(ItemId.None, state.Slots[23].ItemId);
        Assert.Equal(0, state.Slots[23].Count);
    }
}
