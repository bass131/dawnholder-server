using System;
using System.Collections;
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    public sealed class InventoryPointerEntrySceneTests
    {
        MapEntryPlayFixture _fixture;

        [SetUp]
        public void SetUp() => _fixture = new MapEntryPlayFixture();

        [UnityTearDown]
        public IEnumerator TearDown() => _fixture.Cleanup();

        [UnityTest]
        public IEnumerator InventoryKeyAndCurrentMouse_HoverThenRightPress_SendOneCurrentRevisionPouchUse()
        {
            // Same authority barrier as GuildUiConnectionTestTools.EnterTown: peer selection alone is earlier
            // than the client applying CharacterLoad, which BeginMapEntry requires.
            yield return _fixture.Prepare(false);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.CharacterLoad.IsSuccess,
                "actual character authority before EnterMap");
            _fixture.Peer.Send(new S_EnterMap { entityId = 700, spawnX = 2, spawnY = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.State == MapEntryState.Entering &&
                _fixture.Session.Entry.SceneReady && _fixture.Player != null, "actual Town binding before HP");
            _fixture.PairInput();
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_InventoryRequest) == 1,
                "initial inventory query", 3);
            var snapshot = new S_InventorySnapshot { revision = 7, currency = 10 };
            snapshot.slot0ItemId = (int)ItemId.Material;
            snapshot.slot0Count = 2;
            snapshot.slot1ItemId = (int)ItemId.CoinPouch;
            snapshot.slot1Count = 1;
            _fixture.Peer.Send(snapshot.Write());
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Status == "서버 확인 완료",
                "inventory snapshot applied", 3);
            Assert.AreEqual(7u, InventoryState.Instance.Revision, "The actual mirror received the new revision");
            Assert.AreEqual(0f, InventoryPanelProbe.Group.alpha, "Entry starts with a closed inventory");
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
            Assert.AreSame(_fixture.Mouse, Mouse.current, "The product reads this same Input System mouse");
            yield return InventoryPanelProbe.PointAt(_fixture.Mouse, InventoryPanelProbe.SlotPoint(1));
            Assert.IsTrue(InventoryPanelProbe.DetailShown, "Actual pointer hover reached the occupied slot");
            Assert.AreEqual("재화 주머니", InventoryPanelProbe.DetailLabel("Name").text);
            TestContext.Out.WriteLine($"fixtureMouse={_fixture.Mouse.deviceId} currentMouse={Mouse.current.deviceId} revision={InventoryState.Instance.Revision}");

            // The negative run clicks a valid empty slot; the same packet observer must then fail.
            bool negative = Environment.GetEnvironmentVariable("DAWNHOLDER_INVENTORY_OBSERVER_NEGATIVE") == "1";
            yield return InventoryPanelProbe.RightClick(_fixture.Mouse, negative ? 2 : 1);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_ItemUse) == 1,
                "one packet after the actual right press", 3);
            yield return _fixture.Frames(6);
            Assert.AreEqual(1, _fixture.Peer.Count(PacketID.C_ItemUse), "Exactly one packet, including settling frames");
            C_ItemUse packet = _fixture.Peer.Last<C_ItemUse>(PacketID.C_ItemUse);
            TestContext.Out.WriteLine($"itemId={packet.itemId} expectedRevision={packet.expectedRevision} packets={_fixture.Peer.Count(PacketID.C_ItemUse)} negative={negative}");
            Assert.AreEqual((int)ItemId.CoinPouch, packet.itemId);
            Assert.AreEqual(7u, packet.expectedRevision);
        }
    }
}
