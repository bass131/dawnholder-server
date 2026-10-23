using System;
using System.Collections;
using System.Linq;
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using static Dawnholder.Client.Tests.PlayMode.GuildUiConnectionTestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // PR2 live binding requirements: real scene bootstrap, current TCP dispatch, product views,
    // actual pointer/G frames and peer-observed requests. Only server authority is scripted.
    // No window/model is built here; missing scene windows are the first observable product RED.
    public sealed class GuildUiConnectionTests
    {
        MapEntryPlayFixture _fixture;

        [SetUp]
        public void Setup() => _fixture = new MapEntryPlayFixture();

        [UnityTearDown]
        public IEnumerator Teardown() => _fixture.Cleanup();

        [UnityTest]
        public IEnumerator Bootstrap_CreatesSceneOwnedGuildWindow_UnsynchronizedBeforeFirst44()
        {
            yield return EnterTown(_fixture);
            GuildWindow guild = Window<GuildWindow>();
            guild.SetOpen(true);
            yield return _fixture.Frames(3);
            Assert.IsFalse(_fixture.Session.Guild.State.HasSnapshot);
            StringAssert.Contains("기다리는", Label(guild, "GuildName"));
            foreach (string label in new[] { "MemberCount", "OnlineCount", "Treasury", "BaseLevel", "LevelUpCost" })
                Assert.IsFalse(Label(guild, label).Any(char.IsDigit), "no sample/zero authority before 44: " + label);
            Assert.IsFalse(InventoryPanelProbe.Interactable(Button(guild, "LevelUp")));
            Debug.Log("[guild live boundary] guild unsynchronized view assertions completed");
        }

        [UnityTest]
        public IEnumerator Bootstrap_CreatesSceneOwnedBlacksmith_UnsynchronizedBeforeFirst44()
        {
            yield return EnterTown(_fixture);
            BlacksmithWindow smith = Window<BlacksmithWindow>();
            smith.SetOpen(true);
            yield return _fixture.Frames(3);
            Assert.IsFalse(_fixture.Session.Guild.State.HasSnapshot);
            foreach (string label in new[] { "EnhanceLevel", "AttackBonus", "GoldCost", "MaterialCost" })
                Assert.IsFalse(Label(smith, label).Any(char.IsDigit), "no sample/zero authority before 44: " + label);
            Assert.IsFalse(InventoryPanelProbe.Interactable(Button(smith, "Enhance")));
            Debug.Log("[guild live boundary] blacksmith unsynchronized view assertions completed");
        }

        [UnityTest]
        public IEnumerator CurrentTcp44_RendersSharedAndPersonalServerLiterals_InBothExistingWindows()
        {
            yield return EnterTown(_fixture);
            GuildWindow guild = Window<GuildWindow>();
            BlacksmithWindow smith = Window<BlacksmithWindow>();
            yield return Synchronize(_fixture);
            guild.SetOpen(true);
            smith.SetOpen(true);
            yield return AnswerOptionalQuery(_fixture, Snapshot(sequence: 12));
            yield return _fixture.Frames(3);
            Assert.AreEqual("새벽 길드", Label(guild, "GuildName"));
            HasNumbers(guild, "MemberCount", "7");
            HasNumbers(guild, "OnlineCount", "3");
            HasNumbers(guild, "Treasury", "40");
            HasNumbers(guild, "BaseLevel", "12");
            HasNumbers(guild, "LevelUpCost", "30");
            StringAssert.Contains("잠김", Label(guild, "BlacksmithStatus"));
            StringAssert.Contains("기사", Label(smith, "CharacterClass"));
            HasNumbers(smith, "EnhanceLevel", "01");
            HasNumbers(smith, "AttackBonus", "0");
            HasNumbers(smith, "GoldCost", "20");
            HasNumbers(smith, "MaterialCost", "1");
            Assert.IsFalse(InventoryPanelProbe.Interactable(Button(smith, "Enhance")));
            Send(_fixture.Peer, Snapshot(sequence: 20, guildRevision: 8, kind: 3, treasury: 10, unlocked: true));
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Guild.State.SnapshotSequence == 20, "broadcast44", 5);
            yield return AnswerOptionalQuery(_fixture,
                Snapshot(sequence: 21, guildRevision: 8, treasury: 10, unlocked: true));
            yield return _fixture.Frames(3);
            HasNumbers(guild, "BaseLevel", "22");
            HasNumbers(guild, "Treasury", "10");
            StringAssert.Contains("해금", Label(guild, "BlacksmithStatus"));
            Assert.IsTrue(InventoryPanelProbe.Interactable(Button(smith, "Enhance")));
            Debug.Log("[guild live boundary] both windows rendered TCP44 literals and broadcast unlock");
        }

        [UnityTest]
        public IEnumerator GAndPointer_LevelUpSendsOne45_PendingBlocksAllRequests_UntilMatching47And44()
        {
            yield return EnterTown(_fixture);
            GuildWindow guild = Window<GuildWindow>();
            BlacksmithWindow smith = Window<BlacksmithWindow>();
            yield return Synchronize(_fixture);
            Assert.IsFalse(guild.IsOpen, "new connection starts closed");
            _fixture.Keys(Key.G);
            yield return _fixture.Frames(2);
            _fixture.Keys();
            yield return _fixture.Frames(3);
            Assert.IsTrue(guild.IsOpen, "real G frame opens the product guild window");
            yield return AnswerOptionalQuery(_fixture, Snapshot(sequence: 12));
            yield return ScreenUiProbe.WaitForEventSystem();
            Component levelUp = Button(guild, "LevelUp");
            Assert.IsTrue(ScreenUiProbe.RaycastHits(ScreenUiProbe.ScreenPoint(levelUp))
                .Any(hit => hit.transform.IsChildOf(levelUp.transform)), "actual action receives pointer");
            int attacks = _fixture.Peer.Count(PacketID.C_Attack);
            yield return InventoryPanelProbe.Click(_fixture.Mouse, levelUp);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_StrongholdUpgrade) == 1, "actual UI45", 5);
            Assert.AreEqual(7u, _fixture.Peer.Last<C_StrongholdUpgrade>(PacketID.C_StrongholdUpgrade).expectedGuildRevision);
            Assert.AreEqual(attacks, _fixture.Peer.Count(PacketID.C_Attack), "UI pointer does not attack");
            int requests = Requests(_fixture.Peer);
            yield return InventoryPanelProbe.Click(_fixture.Mouse, levelUp);
            smith.SetOpen(true);
            yield return InventoryPanelProbe.Click(_fixture.Mouse, Button(smith, "Enhance"));
            yield return _fixture.Frames(4);
            Assert.AreEqual(requests, Requests(_fixture.Peer), "pending allows no additional43/45/46");
            HasNumbers(guild, "Treasury", "40");
            HasNumbers(guild, "BaseLevel", "12");
            Assert.AreEqual(71, InventoryState.Instance.Currency, "no optimistic personal spending");
            Send(_fixture.Peer, Result(action: 1, requested: 7, guildRevision: 8, inventoryRevision: 3, sequence: 20));
            yield return _fixture.Frames(5);
            Assert.IsTrue(_fixture.Session.Guild.IsPending, "47 alone does not settle purchase");
            HasNumbers(guild, "BaseLevel", "12");
            Send(_fixture.Peer, Snapshot(sequence: 20, guildRevision: 8, kind: 3, treasury: 10, unlocked: true));
            yield return MapEntryPlayFixture.Wait(() => !_fixture.Session.Guild.IsPending, "matching47+44", 5);
            yield return _fixture.Frames(3);
            HasNumbers(guild, "BaseLevel", "22");
            HasNumbers(guild, "Treasury", "10");
            StringAssert.Contains("완료", Label(guild, "ActionResult"));
            Debug.Log("[guild live boundary] G/pointer45, no attack, pending and paired confirmation completed");
        }

        [UnityTest]
        public IEnumerator EnhancePointer_SendsCurrent46_ClosingAndReopeningPreservesConnectionPending()
        {
            yield return EnterTown(_fixture);
            BlacksmithWindow smith = Window<BlacksmithWindow>();
            yield return Synchronize(_fixture, unlocked: true);
            smith.SetOpen(true);
            yield return AnswerOptionalQuery(_fixture, Snapshot(sequence: 12, unlocked: true));
            yield return _fixture.Frames(3);
            yield return ScreenUiProbe.WaitForEventSystem();
            Component enhance = Button(smith, "Enhance");
            var owner = _fixture.Session.Guild;
            yield return InventoryPanelProbe.Click(_fixture.Mouse, enhance);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_WeaponEnhance) == 1, "actual UI46", 5);
            Assert.AreEqual(3u, _fixture.Peer.Last<C_WeaponEnhance>(PacketID.C_WeaponEnhance).expectedInventoryRevision);
            int requests = Requests(_fixture.Peer);
            Vector2 point = ScreenUiProbe.ScreenPoint(enhance);
            smith.SetOpen(false);
            yield return _fixture.Frames(3);
            Assert.IsFalse(ScreenUiProbe.RaycastHits(point).Any(hit => hit.transform.IsChildOf(smith.transform)),
                "closed window does not block raycast");
            yield return InventoryPanelProbe.ClickAt(_fixture.Mouse, point);
            smith.SetOpen(true);
            smith.gameObject.SetActive(false);
            yield return _fixture.Frames(2);
            smith.gameObject.SetActive(true);
            yield return _fixture.Frames(3);
            yield return InventoryPanelProbe.Click(_fixture.Mouse, enhance);
            Assert.AreSame(owner, _fixture.Session.Guild, "view lifetime cannot replace controller");
            Assert.IsTrue(owner.IsPending);
            Assert.AreEqual(requests, Requests(_fixture.Peer), "closed/reopened/pending sends no extra request");
            HasNumbers(smith, "EnhanceLevel", "01");
            Assert.AreEqual(71, InventoryState.Instance.Currency);
            Send(_fixture.Peer, Snapshot(sequence: 20, inventoryRevision: 4, kind: 4, unlocked: true, enhanced: true));
            yield return _fixture.Frames(5);
            Assert.IsTrue(owner.IsPending, "44 alone does not settle purchase");
            Send(_fixture.Peer, Result(action: 2, requested: 3, guildRevision: 7, inventoryRevision: 4, sequence: 20));
            yield return MapEntryPlayFixture.Wait(() => !owner.IsPending, "matching enhancement47+44", 5);
            yield return _fixture.Frames(3);
            HasNumbers(smith, "EnhanceLevel", "11");
            HasNumbers(smith, "AttackBonus", "5");
            StringAssert.Contains("완료", Label(smith, "ActionResult"));
            Debug.Log("[guild live boundary] pointer46/view lifetime/paired enhancement completed");
        }

        [UnityTest]
        public IEnumerator Newer36_ThenLowerPersonal44_DoesNotRegressPersonalLabelsOrExpectedRevision()
        {
            yield return EnterTown(_fixture);
            BlacksmithWindow smith = Window<BlacksmithWindow>();
            GuildWindow guild = Window<GuildWindow>();
            yield return Synchronize(_fixture, unlocked: true);
            smith.SetOpen(true);
            Send(_fixture.Peer, Snapshot(sequence: 20, inventoryRevision: 4, unlocked: true));
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Guild.State.SnapshotSequence == 20, "personal44 rev4", 5);
            _fixture.Peer.Send(new S_InventorySnapshot { revision = 5, currency = 99, slot0ItemId = 1, slot0Count = 8 }.Write());
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Guild.State.InventoryRevision == 5, "newer36 rev5", 5);
            yield return _fixture.Frames(3);
            string[] before = new[] { "EnhanceLevel", "AttackBonus", "GoldCost", "MaterialCost" }.Select(name => Label(smith, name)).ToArray();
            Send(_fixture.Peer, Snapshot(sequence: 30, guildRevision: 8, inventoryRevision: 3,
                treasury: 19, unlocked: true, enhanced: true));
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Guild.State.SnapshotSequence == 30, "shared-new/personal-old44", 5);
            yield return AnswerOptionalQuery(_fixture, Snapshot(sequence: 31, guildRevision: 8, inventoryRevision: 3,
                treasury: 19, unlocked: true, enhanced: true));
            yield return _fixture.Frames(3);
            CollectionAssert.AreEqual(before, new[] { "EnhanceLevel", "AttackBonus", "GoldCost", "MaterialCost" }
                .Select(name => Label(smith, name)).ToArray(), "lower personal44 cannot change labels/prices");
            HasNumbers(guild, "Treasury", "19");
            Assert.AreEqual(99, InventoryState.Instance.Currency);
            yield return ScreenUiProbe.WaitForEventSystem();
            yield return InventoryPanelProbe.Click(_fixture.Mouse, Button(smith, "Enhance"));
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_WeaponEnhance) == 1, "UI46 after36", 5);
            Assert.AreEqual(5u, _fixture.Peer.Last<C_WeaponEnhance>(PacketID.C_WeaponEnhance).expectedInventoryRevision);
            Debug.Log("[guild live boundary] personal36/old44 preservation and UI46 latest revision completed");
        }

        [UnityTest]
        public IEnumerator BeforeReady_AndAfterDisconnect_ExistingUiCannotSendPurchases()
        {
            yield return EnterTown(_fixture, ready: false);
            GuildWindow guild = Window<GuildWindow>();
            BlacksmithWindow smith = Window<BlacksmithWindow>();
            Send(_fixture.Peer, Snapshot(unlocked: true));
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Guild.State.HasSnapshot, "44 before Ready", 5);
            guild.SetOpen(true);
            smith.SetOpen(true);
            yield return _fixture.Frames(3);
            yield return ScreenUiProbe.WaitForEventSystem();
            yield return InventoryPanelProbe.Click(_fixture.Mouse, Button(guild, "LevelUp"));
            yield return InventoryPanelProbe.Click(_fixture.Mouse, Button(smith, "Enhance"));
            Assert.AreEqual(0, _fixture.Peer.Count(PacketID.C_StrongholdUpgrade));
            Assert.AreEqual(0, _fixture.Peer.Count(PacketID.C_WeaponEnhance));
            UnityClientSession old = _fixture.Session;
            NetworkService.Instance.Disconnect();
            yield return _fixture.Frames(4);
            int requests = Requests(_fixture.Peer);
            old.OnRecvPacket(new ArraySegment<byte>(Snapshot(sequence: 30, unlocked: true, enhanced: true)));
            yield return _fixture.Frames(4);
            Assert.IsFalse(old.Guild.State.HasSnapshot, "closed old callback cannot repopulate state");
            Assert.IsFalse(InventoryPanelProbe.Interactable(Button(guild, "LevelUp")));
            Assert.IsFalse(InventoryPanelProbe.Interactable(Button(smith, "Enhance")));
            yield return InventoryPanelProbe.Click(_fixture.Mouse, Button(smith, "Enhance"));
            Assert.AreEqual(requests, Requests(_fixture.Peer));
            Debug.Log("[guild live boundary] beforeReady/disconnect/closed callback completed");
        }

        [UnityTest]
        public IEnumerator ReplacementConnection_StartsEmpty_AndRejectsOldReceiveCallback()
        {
            yield return EnterTown(_fixture);
            Window<GuildWindow>();
            Window<BlacksmithWindow>();
            yield return Synchronize(_fixture, unlocked: true);
            UnityClientSession old = _fixture.Session;
            var oldController = old.Guild;
            yield return _fixture.Cleanup();
            _fixture = new MapEntryPlayFixture();
            yield return EnterTown(_fixture);
            GuildWindow guild = Window<GuildWindow>();
            BlacksmithWindow smith = Window<BlacksmithWindow>();
            Assert.AreNotSame(old, _fixture.Session);
            Assert.AreNotSame(oldController, _fixture.Session.Guild);
            old.OnRecvPacket(new ArraySegment<byte>(Snapshot(sequence: 90, unlocked: true, enhanced: true)));
            yield return _fixture.Frames(4);
            Assert.IsFalse(_fixture.Session.Guild.State.HasSnapshot);
            HasNumbers(guild, "Treasury", "");
            HasNumbers(smith, "EnhanceLevel", "");
            yield return Synchronize(_fixture);
            Assert.AreEqual("새벽 길드", Label(guild, "GuildName"));
            HasNumbers(smith, "EnhanceLevel", "01");
            Debug.Log("[guild live boundary] replacement/old callback/current rebind completed");
        }
    }
}
