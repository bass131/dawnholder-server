using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Network;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using static Dawnholder.Client.Tests.PlayMode.GuildGameplayWindowTestTools;
using static Dawnholder.Client.Tests.PlayMode.GuildUiConnectionTestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Pretest of the guild growth effect consumer (goal 2026-10-24-growth-effect-screen-link, completion conditions 1-3
    // and 5, consumer-contract-v3 §1-3), written before the consumer exists. The Art view is already in the product, so
    // the suite compiles on the baseline; a missing consumer fails as behaviour: no effect root under _CombatBootstrap and
    // no effect drawn for a confirmed growth. Only server authority is scripted (the fixture peer). Expected levels are
    // the fixture's literal 44 values (stronghold 1→2, weapon 0→1), never read back from GuildClientState.
    // The two-resolution/live-resize capture and HUD readability belong to the capture lane, not to these cases.
    public sealed class GuildGrowthEffectConsumerLifecycleTests
    {
        // Literal confirmed levels of GuildUiConnectionTestTools.Snapshot (its comment: level1/2, weapon0/1).
        const long ConfirmedStronghold = 2;
        const long ConfirmedWeapon = 1;
        const float NoReplaySeconds = 2.5f;

        MapEntryPlayFixture _fixture;
        GuildGrowthEffectConsumerObservation _effects;
        bool _logPolicy;

        [SetUp]
        public void Setup()
        {
            _logPolicy = LogAssert.ignoreFailingMessages;
            _fixture = new MapEntryPlayFixture();
            _effects = new GuildGrowthEffectConsumerObservation();
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            _effects?.Dispose();
            _effects = null;
            LogAssert.ignoreFailingMessages = _logPolicy;
            yield return _fixture.Cleanup();
        }

        // Completion condition 1, consumer-contract §1: one scene-owned root beside GuildUi/GuildWindow/BlacksmithWindow
        // under the real Town _CombatBootstrap, no nested canvas, one GrowthConfirmed delegate for the current source.
        [UnityTest]
        public IEnumerator TownBootstrap_CreatesOneSceneOwnedEffectRoot_BesideTheGuildWindows()
        {
            return _effects.Observe(Steps());

            IEnumerator Steps()
            {
                yield return EnterTown(_fixture);
                var bootstrap = UnityEngine.Object.FindAnyObjectByType<CombatBootstrap>();
                Transform parent = bootstrap.transform;
                GuildUiController owner = Owner();
                Assert.AreSame(parent, owner.transform.parent, "existing GuildUi root stays under _CombatBootstrap");
                Assert.AreSame(parent, Window<GuildWindow>().transform.parent, "existing GuildWindow root is preserved");
                Assert.AreSame(parent, Window<BlacksmithWindow>().transform.parent, "existing BlacksmithWindow root is preserved");

                GuildGrowthEffectView[] views = GuildGrowthEffectConsumerObservation.SceneViews();
                TestContext.Out.WriteLine("[GuildGrowthConsumer] views=" + views.Length + " [" +
                    string.Join("; ", views.Select(view => view.gameObject.scene.name + ":" + ScreenUiProbe.PathOf(view.transform, null))) + "]");
                Assert.AreEqual(1, views.Length, "RED missing consumer: the actual Town CombatBootstrap→GuildUiController path " +
                    "must create exactly one scene-owned GuildGrowthEffectView in all loaded scenes");
                GuildGrowthEffectView view = views[0];
                Assert.AreSame(parent, view.transform.parent, "the effect root is a direct child of _CombatBootstrap");
                Assert.AreEqual(bootstrap.gameObject.scene, view.gameObject.scene, "scene-owned, not DontDestroyOnLoad");
                Assert.IsFalse(view.transform.IsChildOf(owner.transform), "the effect is not nested under the GuildUi canvas");
                Canvas canvas = view.GetComponentInParent<Canvas>();
                Assert.IsNotNull(canvas, "the effect draws on its own canvas");
                Assert.IsTrue(canvas.isRootCanvas, "the effect canvas is a root canvas, not a nested one");
                Assert.AreEqual(1, _effects.ConsumerDelegates(_fixture.Session.Guild).Length,
                    "one GrowthConfirmed delegate for the current connection's GuildRequestController");
                Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<GuildUiController>().Length, "one existing GuildUiController");
                Debug.Log("[guild growth consumer] Town root/sibling/single subscription completed");
            }
        }

        // Completion condition 2, Main a13 decision 2: with both windows closed the Town gate still shows a confirmed
        // stronghold, and a weapon confirmation shows after the smith window that sent 46 was closed.
        [UnityTest]
        public IEnumerator ConfirmedGrowth_WithWindowsClosed_ShowsEachConfirmedLevelOnce()
        {
            return _effects.Observe(Steps());

            IEnumerator Steps()
            {
                yield return EnterTown(_fixture);
                BlacksmithWindow smith = Window<BlacksmithWindow>();
                yield return Synchronize(_fixture);
                float stronghold = _effects.Mark("stronghold 1→2 broadcast with both windows closed");
                Send(_fixture.Peer, StrongholdBroadcast(20));
                yield return _effects.UntilQuiet(0.5f, 6f);
                _effects.AssertOnePlay(GuildChangeKind.StrongholdUpgrade, ConfirmedStronghold, stronghold, "closed windows");

                smith.SetOpen(true);
                yield return ClickEnhance(smith, 1);
                smith.SetOpen(false);
                yield return _fixture.Frames(2);
                Assert.IsFalse(smith.IsOpen, "fixture: the smith window is closed before its confirmation");
                float weapon = _effects.Mark("weapon 47+44 after closing the smith window");
                Send(_fixture.Peer, EnhanceResult(30, guildRevision: 8));
                Send(_fixture.Peer, Enhanced(30, guildRevision: 8));
                yield return _effects.UntilQuiet(0.5f, 6f);
                _effects.AssertOnePlay(GuildChangeKind.WeaponEnhance, ConfirmedWeapon, weapon, "closed smith window");
                _effects.AssertLevels(stronghold, ConfirmedStronghold, ConfirmedWeapon);
                Debug.Log("[guild growth consumer] closed-window stronghold/weapon plays completed");
            }
        }

        // Main a13 decision 2: closing Guild/Smith windows alone does not cancel a playing effect.
        [UnityTest]
        public IEnumerator ClosingGuildAndSmithWindows_DuringAPlayingEffect_DoesNotCancelIt()
        {
            return _effects.Observe(Steps());

            IEnumerator Steps()
            {
                yield return EnterTown(_fixture);
                GuildWindow guild = Window<GuildWindow>();
                BlacksmithWindow smith = Window<BlacksmithWindow>();
                yield return Synchronize(_fixture);
                guild.SetOpen(true);
                smith.SetOpen(true);
                yield return _fixture.Frames(3);
                float sent = _effects.Mark("stronghold broadcast with both windows open");
                Send(_fixture.Peer, StrongholdBroadcast(20));
                yield return _effects.UntilShown(ConfirmedStronghold, 2f);
                _effects.AssertStarted(GuildChangeKind.StrongholdUpgrade, ConfirmedStronghold, sent, "open windows");
                guild.SetOpen(false);
                smith.SetOpen(false);
                float closed = _effects.Mark("both windows closed during the effect");
                yield return _effects.UntilQuiet(0.5f, 6f);
                Assert.IsTrue(_effects.DrawnAfter(closed), "closing Guild/Smith windows alone does not cancel a confirmed effect");
                _effects.AssertOnePlay(GuildChangeKind.StrongholdUpgrade, ConfirmedStronghold, sent, "windows closed mid-effect");
                Debug.Log("[guild growth consumer] window close keeps the playing effect");
            }
        }

        // Completion condition 2: first sync, Query, duplicate and old-sequence 44 show nothing; the existing owner's
        // one confirmed enhancement (44 then 47) shows once and its repeated wire shows nothing more.
        [UnityTest]
        public IEnumerator FirstSyncQueryDuplicateAndOldSnapshots_ShowNothing_ThenOneEnhancementShowsOnce()
        {
            return _effects.Observe(Steps());

            IEnumerator Steps()
            {
                yield return EnterTown(_fixture);
                BlacksmithWindow smith = Window<BlacksmithWindow>();
                float first = _effects.Mark("first synchronization");
                yield return Synchronize(_fixture);
                int queries = _fixture.Peer.Count(PacketID.C_GuildRequest);
                Assert.IsTrue(_fixture.Session.Guild.Refresh(), "fixture: the existing owner sends a Query43");
                yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_GuildRequest) == queries + 1, "fixture: Query43", 5);
                // A Query44 that already shows level 2 carries no growth marker.
                Send(_fixture.Peer, Snapshot(sequence: 12, unlocked: true));
                yield return MapEntryPlayFixture.Wait(() => !_fixture.Session.Guild.IsPending, "fixture: Query44", 5);
                Send(_fixture.Peer, Snapshot(sequence: 12, kind: 3, unlocked: true));
                Send(_fixture.Peer, Snapshot(sequence: 11, kind: 3, unlocked: true));
                yield return _effects.For(NoReplaySeconds);
                Assert.IsEmpty(_effects.Sources(first), "fixture: the existing owner confirmed no growth for these 44s");
                _effects.AssertNothingDrawn(first, "first sync/Query/duplicate/old sequence");

                smith.SetOpen(true);
                yield return ClickEnhance(smith, 1);
                float confirmed = _effects.Mark("enhancement 44 then 47");
                Send(_fixture.Peer, Enhanced(20, guildRevision: 7));
                Send(_fixture.Peer, EnhanceResult(20, guildRevision: 7));
                yield return _effects.UntilQuiet(0.5f, 6f);
                _effects.AssertOnePlay(GuildChangeKind.WeaponEnhance, ConfirmedWeapon, confirmed, "44 then 47");

                float repeated = _effects.Mark("the same 44 and 47 again");
                Send(_fixture.Peer, Enhanced(20, guildRevision: 7));
                Send(_fixture.Peer, EnhanceResult(20, guildRevision: 7));
                yield return _effects.For(NoReplaySeconds);
                Assert.IsEmpty(_effects.Sources(repeated), "fixture: repeated wire is not a new confirmation");
                _effects.AssertNothingDrawn(repeated, "duplicate 44/47");
                Debug.Log("[guild growth consumer] first sync/Query/duplicate/old and one confirmed enhancement completed");
            }
        }

        // Completion condition 2: an unmatched 47 and a refused purchase show nothing; the next matching success (47
        // then 44) shows once.
        [UnityTest]
        public IEnumerator UnmatchedAndRefusedEnhancements_ShowNothing_UntilTheMatchingSuccess()
        {
            return _effects.Observe(Steps());

            IEnumerator Steps()
            {
                yield return EnterTown(_fixture);
                BlacksmithWindow smith = Window<BlacksmithWindow>();
                yield return Synchronize(_fixture, unlocked: true);
                smith.SetOpen(true);
                yield return AnswerOptionalQuery(_fixture, Snapshot(sequence: 12, unlocked: true));
                yield return ClickEnhance(smith, 1);
                float refused = _effects.Mark("unmatched 47, unchanged 44, refused 47");
                Send(_fixture.Peer, Result(action: 2, requested: 9, guildRevision: 7, inventoryRevision: 3, sequence: 20));
                Send(_fixture.Peer, Snapshot(sequence: 20, unlocked: true));
                byte[] refusal = Result(action: 2, requested: 3, guildRevision: 7, inventoryRevision: 3, sequence: 20);
                refusal[5] = (byte)GuildResult.InsufficientCurrency; // S_GuildActionResult.result
                Send(_fixture.Peer, refusal);
                yield return MapEntryPlayFixture.Wait(() => !_fixture.Session.Guild.IsPending, "fixture: refusal settles the purchase", 5);
                yield return _effects.For(NoReplaySeconds);
                Assert.IsEmpty(_effects.Sources(refused), "fixture: the existing owner confirmed no growth");
                _effects.AssertNothingDrawn(refused, "unmatched/refused purchase");

                yield return ClickEnhance(smith, 2);
                float confirmed = _effects.Mark("matching success 47 then 44");
                Send(_fixture.Peer, EnhanceResult(30, guildRevision: 7));
                Send(_fixture.Peer, Enhanced(30, guildRevision: 7));
                yield return _effects.UntilQuiet(0.5f, 6f);
                _effects.AssertOnePlay(GuildChangeKind.WeaponEnhance, ConfirmedWeapon, confirmed, "47 then 44");
                Debug.Log("[guild growth consumer] unmatched/refused/success completed");
            }
        }

        // Completion condition 3: the existing inventory priority window closes the gate; CancelAll, and closing it
        // replays nothing.
        [UnityTest]
        public IEnumerator PriorityInventory_CancelsAPlayingEffect_AndClosingItReplaysNothing()
        {
            return _effects.Observe(Steps());

            IEnumerator Steps()
            {
                yield return EnterTown(_fixture);
                yield return Synchronize(_fixture);
                float sent = _effects.Mark("stronghold broadcast");
                Send(_fixture.Peer, StrongholdBroadcast(20));
                yield return _effects.UntilShown(ConfirmedStronghold, 2f);
                _effects.AssertStarted(GuildChangeKind.StrongholdUpgrade, ConfirmedStronghold, sent, "before inventory");
                float pressed = _effects.Mark("I pressed");
                _fixture.Keys(Key.I);
                yield return MapEntryPlayFixture.Wait(InventoryShown, "fixture: actual I opens the existing inventory", 3);
                float shown = _effects.Mark("inventory shown");
                _fixture.Keys();
                yield return _effects.For(1f);
                _effects.AssertCancelled(pressed, shown, "inventory priority window");
                yield return KeyFrame(_fixture, Key.I);
                Assert.IsFalse(InventoryShown(), "fixture: actual I closes the inventory");
                float closed = _effects.Mark("inventory closed");
                yield return _effects.For(NoReplaySeconds);
                _effects.AssertNothingDrawn(closed, "closing the inventory");
                Debug.Log("[guild growth consumer] inventory cancel/no replay completed");
            }
        }

        // Completion condition 3: a confirmation that arrives under the existing pause menu (timeScale 0) shows nothing,
        // resuming replays nothing, and the next Town confirmation shows once.
        [UnityTest]
        public IEnumerator ConfirmationUnderPauseMenu_ShowsNothing_ResumeReplaysNothing()
        {
            return _effects.Observe(Steps());

            IEnumerator Steps()
            {
                yield return EnterTown(_fixture);
                BlacksmithWindow smith = Window<BlacksmithWindow>();
                yield return Synchronize(_fixture);
                yield return KeyFrame(_fixture, Key.Escape);
                Assert.AreEqual(0f, Time.timeScale, "fixture: actual Escape opens the existing pause menu");
                float paused = _effects.Mark("stronghold broadcast under the pause menu");
                Send(_fixture.Peer, StrongholdBroadcast(20));
                yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Guild.State.SnapshotSequence == 20, "fixture: 44 under menu", 5);
                yield return _effects.For(1.5f);
                Assert.AreEqual(1, _effects.Sources(paused).Length, "fixture: the existing owner confirmed the stronghold under the menu");
                _effects.AssertNothingDrawn(paused, "pause menu gate");
                yield return KeyFrame(_fixture, Key.Escape);
                Assert.AreEqual(1f, Time.timeScale, "fixture: actual Escape resumes");
                float resumed = _effects.Mark("resumed");
                yield return _effects.For(NoReplaySeconds);
                _effects.AssertNothingDrawn(resumed, "resuming from the menu");

                smith.SetOpen(true);
                yield return ClickEnhance(smith, 1);
                float confirmed = _effects.Mark("enhancement after resume");
                Send(_fixture.Peer, EnhanceResult(30, guildRevision: 8));
                Send(_fixture.Peer, Enhanced(30, guildRevision: 8));
                yield return _effects.UntilQuiet(0.5f, 6f);
                _effects.AssertOnePlay(GuildChangeKind.WeaponEnhance, ConfirmedWeapon, confirmed, "after resume");
                Debug.Log("[guild growth consumer] menu gate/no replay/next play completed");
            }
        }

        // Completion condition 3: the existing server39 reward window closes the gate; CancelAll, and confirming it
        // replays nothing.
        [UnityTest]
        public IEnumerator PriorityReward_CancelsAPlayingEffect_AndConfirmingItReplaysNothing()
        {
            return _effects.Observe(Steps());

            IEnumerator Steps()
            {
                yield return EnterTown(_fixture);
                yield return Synchronize(_fixture);
                float sent = _effects.Mark("stronghold broadcast");
                Send(_fixture.Peer, StrongholdBroadcast(20));
                yield return _effects.UntilShown(ConfirmedStronghold, 2f);
                _effects.AssertStarted(GuildChangeKind.StrongholdUpgrade, ConfirmedStronghold, sent, "before reward");
                float pressed = _effects.Mark("server39 sent");
                _fixture.Peer.Send(new S_DungeonClearResult { result = 0, currency = 50,
                    reward0ItemId = 1, reward0Count = 1, reward1ItemId = 2, reward1Count = 1 }.Write());
                yield return MapEntryPlayFixture.Wait(RewardShown, "fixture: actual39 reward window shown", 5);
                float shown = _effects.Mark("reward shown");
                yield return _effects.For(1f);
                _effects.AssertCancelled(pressed, shown, "reward priority window");
                yield return ScreenUiProbe.WaitForEventSystem();
                yield return InventoryPanelProbe.Click(_fixture.Mouse, Button(DungeonClearRewardWindow.Instance, "Confirm"));
                yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_DungeonClearConfirm) == 1,
                    "fixture: existing reward confirmation40", 5);
                yield return MapEntryPlayFixture.Wait(() => !RewardShown(), "fixture: confirmation hides the reward", 5);
                float closed = _effects.Mark("reward confirmed");
                yield return _effects.For(NoReplaySeconds);
                _effects.AssertNothingDrawn(closed, "confirming the reward");
                Debug.Log("[guild growth consumer] reward cancel/no replay completed");
            }
        }

        // Main a13 decision 2 (required): a purchase requested in Town and confirmed after leaving Town shows nothing,
        // returning to Town replays nothing, and a new Town confirmation still shows once.
        [UnityTest]
        public IEnumerator RequestedInTown_ConfirmedAwayFromTown_ShowsNothing_AndReturningReplaysNothing()
        {
            return _effects.Observe(Steps());

            IEnumerator Steps()
            {
                yield return EnterTown(_fixture);
                GuildWindow guild = Window<GuildWindow>();
                yield return Synchronize(_fixture);
                guild.SetOpen(true);
                yield return AnswerOptionalQuery(_fixture, Snapshot(sequence: 12));
                yield return ScreenUiProbe.WaitForEventSystem();
                yield return InventoryPanelProbe.Click(_fixture.Mouse, Button(guild, "LevelUp"));
                yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_StrongholdUpgrade) == 1, "fixture: actual UI45 in Town", 5);
                yield return MoveTo(1);
                float away = _effects.Mark("47+44 while in HuntingGround");
                Send(_fixture.Peer, Result(action: 1, requested: 7, guildRevision: 8, inventoryRevision: 3, sequence: 20));
                Send(_fixture.Peer, StrongholdBroadcast(20));
                yield return MapEntryPlayFixture.Wait(() => !_fixture.Session.Guild.IsPending, "fixture: matching 47+44 away from Town", 5);
                yield return _effects.For(NoReplaySeconds);
                Assert.AreEqual(1, _effects.Sources(away).Length, "fixture: the existing owner confirmed the stronghold away from Town");
                _effects.AssertNothingDrawn(away, "confirmation outside Town");

                yield return MoveTo(0);
                float back = _effects.Mark("returned to Town");
                yield return _effects.For(NoReplaySeconds);
                _effects.AssertNothingDrawn(back, "returning to Town");

                BlacksmithWindow smith = Window<BlacksmithWindow>();
                smith.SetOpen(true);
                yield return ClickEnhance(smith, 1);
                float confirmed = _effects.Mark("enhancement in Town after returning");
                Send(_fixture.Peer, Enhanced(30, guildRevision: 8));
                Send(_fixture.Peer, EnhanceResult(30, guildRevision: 8));
                yield return _effects.UntilQuiet(0.5f, 6f);
                _effects.AssertOnePlay(GuildChangeKind.WeaponEnhance, ConfirmedWeapon, confirmed, "Town after return");
                Debug.Log("[guild growth consumer] Town leave before confirmation/no replay/new play completed");
            }
        }

        // Completion condition 3, consumer-contract §1: disabling the owner cancels and unsubscribes; a confirmation while
        // disabled shows nothing and re-enabling replays nothing.
        [UnityTest]
        public IEnumerator OwnerDisabled_CancelsAndUnsubscribes_ReenableReplaysNothing()
        {
            return _effects.Observe(Steps());

            IEnumerator Steps()
            {
                yield return EnterTown(_fixture);
                yield return Synchronize(_fixture);
                float sent = _effects.Mark("stronghold broadcast");
                Send(_fixture.Peer, StrongholdBroadcast(20));
                yield return _effects.UntilShown(ConfirmedStronghold, 2f);
                _effects.AssertStarted(GuildChangeKind.StrongholdUpgrade, ConfirmedStronghold, sent, "before disable");
                GuildUiController owner = Owner();
                float disabled = _effects.Mark("GuildUi owner disabled");
                owner.gameObject.SetActive(false);
                yield return _effects.For(0.5f);
                _effects.AssertCancelled(disabled, disabled, "owner disable");
                Assert.AreEqual(0, _effects.ConsumerDelegates(_fixture.Session.Guild).Length, "a disabled owner keeps no GrowthConfirmed delegate");

                // The windows are detached while their owner is disabled, so the source's own public request is used.
                float whileDisabled = _effects.Mark("enhancement confirmed while the owner is disabled");
                Assert.IsTrue(_fixture.Session.Guild.TryEnhanceWeapon(), "fixture: the existing owner sends 46");
                yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_WeaponEnhance) == 1, "fixture: 46", 5);
                Send(_fixture.Peer, EnhanceResult(30, guildRevision: 8));
                Send(_fixture.Peer, Enhanced(30, guildRevision: 8));
                yield return MapEntryPlayFixture.Wait(() => !_fixture.Session.Guild.IsPending, "fixture: matching 47+44", 5);
                yield return _effects.For(1.5f);
                Assert.AreEqual(1, _effects.Sources(whileDisabled).Length, "fixture: the existing owner confirmed the enhancement");
                _effects.AssertNothingDrawn(whileDisabled, "confirmation while the owner is disabled");

                owner.gameObject.SetActive(true);
                float enabled = _effects.Mark("GuildUi owner re-enabled");
                yield return _effects.For(NoReplaySeconds);
                _effects.AssertNothingDrawn(enabled, "re-enabling the owner");
                Assert.AreEqual(1, _effects.ConsumerDelegates(_fixture.Session.Guild).Length, "re-enable binds one delegate again");
                Debug.Log("[guild growth consumer] owner disable/unsubscribe/no replay completed");
            }
        }

        // Consumer-contract §1: destroying the owner destroys the effect root it created and its subscription.
        [UnityTest]
        public IEnumerator OwnerDestroyed_RemovesItsEffectRootAndSubscription()
        {
            return _effects.Observe(Steps());

            IEnumerator Steps()
            {
                yield return EnterTown(_fixture);
                yield return Synchronize(_fixture);
                Assert.AreEqual(1, GuildGrowthEffectConsumerObservation.SceneViews().Length,
                    "RED missing consumer: the Town owner must have created one effect view before it is destroyed");
                UnityEngine.Object.Destroy(Owner().gameObject);
                yield return _fixture.Frames(2);
                Assert.AreEqual(0, GuildGrowthEffectConsumerObservation.SceneViews().Length, "the owner's destruction removes the effect root it created");
                Assert.AreEqual(0, _effects.ConsumerDelegates(_fixture.Session.Guild).Length, "a destroyed owner keeps no GrowthConfirmed delegate");
                float destroyed = _effects.Mark("stronghold broadcast after the owner was destroyed");
                Send(_fixture.Peer, StrongholdBroadcast(20));
                yield return _effects.For(1.5f);
                Assert.AreEqual(1, _effects.Sources(destroyed).Length, "fixture: the existing owner confirmed the stronghold");
                _effects.AssertNothingDrawn(destroyed, "after owner destruction");
                Debug.Log("[guild growth consumer] owner destroy/root and delegate cleanup completed");
            }
        }

        // Completion condition 3: closing the connection cancels a playing effect; the closed session shows nothing.
        [UnityTest]
        public IEnumerator Disconnect_CancelsAPlayingEffect_AndTheClosedSessionShowsNothing()
        {
            return _effects.Observe(Steps());

            IEnumerator Steps()
            {
                yield return EnterTown(_fixture);
                yield return Synchronize(_fixture);
                float sent = _effects.Mark("stronghold broadcast");
                Send(_fixture.Peer, StrongholdBroadcast(20));
                yield return _effects.UntilShown(ConfirmedStronghold, 2f);
                _effects.AssertStarted(GuildChangeKind.StrongholdUpgrade, ConfirmedStronghold, sent, "before disconnect");
                UnityClientSession old = _fixture.Session;
                float closed = _effects.Mark("NetworkService.Disconnect");
                NetworkService.Instance.Disconnect();
                yield return _effects.For(1f);
                _effects.AssertCancelled(closed, closed, "disconnect");
                old.OnRecvPacket(new ArraySegment<byte>(Snapshot(sequence: 30, kind: 3, unlocked: true)));
                yield return _effects.For(1.5f);
                _effects.AssertNothingDrawn(closed, "closed session callback");
                Debug.Log("[guild growth consumer] disconnect cancel/closed callback completed");
            }
        }

        // Completion condition 3, consumer-contract §1: a replacement connection's first sync shows nothing, an earlier
        // invoke-list's consumer delegate run late reaches neither the new owner nor its view, and the new connection's
        // confirmation shows once through one new delegate.
        [UnityTest]
        public IEnumerator ReplacementConnection_StaleDelegatesReachNothing_NewConfirmationShowsOnce()
        {
            return _effects.Observe(Steps());

            IEnumerator Steps()
            {
                yield return EnterTown(_fixture);
                yield return Synchronize(_fixture);
                float firstConnection = _effects.Mark("first connection stronghold broadcast");
                Send(_fixture.Peer, StrongholdBroadcast(20));
                yield return _effects.UntilQuiet(0.5f, 6f);
                _effects.AssertOnePlay(GuildChangeKind.StrongholdUpgrade, ConfirmedStronghold, firstConnection, "first connection");
                UnityClientSession old = _fixture.Session;
                Action<GuildChangeKind>[] stale = _effects.ConsumerDelegates(old.Guild);
                Assert.AreEqual(1, stale.Length, "the first connection had one consumer delegate");

                yield return _fixture.Cleanup();
                _fixture = new MapEntryPlayFixture();
                yield return EnterTown(_fixture);
                BlacksmithWindow smith = Window<BlacksmithWindow>();
                Assert.AreNotSame(old, _fixture.Session, "fixture: a replacement connection");
                float replacement = _effects.Mark("replacement first sync at level 2");
                yield return Synchronize(_fixture, unlocked: true);
                foreach (Action<GuildChangeKind> late in stale)
                {
                    try
                    {
                        late(GuildChangeKind.StrongholdUpgrade);
                    }
                    catch (Exception error)
                    {
                        Assert.Fail("a stale consumer delegate threw instead of returning for its old source: " + error);
                    }
                }
                old.OnRecvPacket(new ArraySegment<byte>(Snapshot(sequence: 90, kind: 3, unlocked: true)));
                yield return _effects.For(NoReplaySeconds);
                Assert.IsEmpty(_effects.Sources(replacement), "fixture: the new owner confirmed no growth at first sync");
                _effects.AssertNothingDrawn(replacement, "replacement first sync and stale delegates");
                Assert.AreEqual(1, _effects.ConsumerDelegates(_fixture.Session.Guild).Length, "one delegate for the new source");

                smith.SetOpen(true);
                yield return ClickEnhance(smith, 1);
                float confirmed = _effects.Mark("replacement enhancement");
                Send(_fixture.Peer, EnhanceResult(20, guildRevision: 7));
                Send(_fixture.Peer, Enhanced(20, guildRevision: 7));
                yield return _effects.UntilQuiet(0.5f, 6f);
                _effects.AssertOnePlay(GuildChangeKind.WeaponEnhance, ConfirmedWeapon, confirmed, "replacement connection");
                Debug.Log("[guild growth consumer] replacement/stale delegates/new play completed");
            }
        }

        // Completion condition 4 (automatable part), consumer-contract §3: two simultaneous effects with both windows
        // open stay whole on screen and outside both window panels from their first drawn frame; the windows stay open
        // and drawn, their buttons stay the topmost pointer target and the effect takes no pointer.
        [UnityTest]
        public IEnumerator BothWindowsOpen_TwoSimultaneousEffects_StayOnScreenAndOutsideBothPanels()
        {
            return _effects.Observe(Steps());

            IEnumerator Steps()
            {
                yield return EnterTown(_fixture);
                GuildWindow guild = Window<GuildWindow>();
                BlacksmithWindow smith = Window<BlacksmithWindow>();
                yield return Synchronize(_fixture);
                guild.SetOpen(true);
                smith.SetOpen(true);
                yield return _fixture.Frames(3);
                float stronghold = _effects.Mark("stronghold broadcast with both windows open");
                Send(_fixture.Peer, StrongholdBroadcast(20));
                yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Guild.State.SnapshotSequence == 20, "fixture: broadcast44", 5);
                yield return ClickEnhance(smith, 1);
                float weapon = _effects.Mark("weapon 47 then 44 during the stronghold effect");
                Send(_fixture.Peer, EnhanceResult(21, guildRevision: 8));
                Send(_fixture.Peer, Enhanced(21, guildRevision: 8));
                yield return _effects.UntilShown(ConfirmedWeapon, 2f);
                _effects.AssertStarted(GuildChangeKind.StrongholdUpgrade, ConfirmedStronghold, stronghold, "both windows");
                _effects.AssertStarted(GuildChangeKind.WeaponEnhance, ConfirmedWeapon, weapon, "both windows");
                Assert.IsTrue(_effects.BothShownNow(ConfirmedStronghold, ConfirmedWeapon),
                    "fixture: the two confirmations overlap in time so simultaneous placement is observable");
                yield return ScreenUiProbe.WaitForEventSystem();
                _effects.AssertWindowButtonsOnTop(guild, "LevelUp", "Refresh", "Close");
                _effects.AssertWindowButtonsOnTop(smith, "Enhance", "Refresh", "Close");
                _effects.AssertEffectTakesNoPointer();
                yield return _effects.UntilQuiet(0.5f, 8f);
                _effects.AssertOnePlay(GuildChangeKind.StrongholdUpgrade, ConfirmedStronghold, stronghold, "both windows");
                _effects.AssertOnePlay(GuildChangeKind.WeaponEnhance, ConfirmedWeapon, weapon, "both windows");
                Assert.IsTrue(guild.IsOpen && smith.IsOpen, "the effects do not close either window");
                _effects.AssertLayout(stronghold, "two effects, both windows open");
                _effects.LogHudAndReadability(stronghold);
                Debug.Log("[guild growth consumer] two simultaneous effects/both panels/input completed");
            }
        }

        // Completion condition 4 (automatable part): guild-only, then both after opening the smith mid-effect, then
        // smith-only. Each layout holds from the RelayoutFrames-th frame after a window change.
        [UnityTest]
        public IEnumerator EachWindowAlone_AndOpeningOneMidEffect_KeepTheEffectOutsideOpenPanels()
        {
            return _effects.Observe(Steps());

            IEnumerator Steps()
            {
                yield return EnterTown(_fixture);
                GuildWindow guild = Window<GuildWindow>();
                BlacksmithWindow smith = Window<BlacksmithWindow>();
                yield return Synchronize(_fixture);
                guild.SetOpen(true);
                yield return _fixture.Frames(3);
                float stronghold = _effects.Mark("stronghold broadcast with the guild window only");
                Send(_fixture.Peer, StrongholdBroadcast(20));
                yield return _effects.UntilShown(ConfirmedStronghold, 2f);
                _effects.AssertStarted(GuildChangeKind.StrongholdUpgrade, ConfirmedStronghold, stronghold, "guild only");
                smith.SetOpen(true);
                float opened = _effects.Mark("smith window opened mid-effect");
                yield return _effects.UntilQuiet(0.5f, 6f);
                Assert.IsTrue(_effects.DrawnAfter(opened), "fixture: the effect was still drawn after the smith window opened");
                _effects.AssertOnePlay(GuildChangeKind.StrongholdUpgrade, ConfirmedStronghold, stronghold, "guild only then both");
                _effects.AssertLayout(stronghold, "guild only, then both windows");

                guild.SetOpen(false);
                yield return _fixture.Frames(3);
                yield return ClickEnhance(smith, 1);
                float weapon = _effects.Mark("weapon confirmation with the smith window only");
                Send(_fixture.Peer, EnhanceResult(30, guildRevision: 8));
                Send(_fixture.Peer, Enhanced(30, guildRevision: 8));
                yield return _effects.UntilQuiet(0.5f, 6f);
                _effects.AssertOnePlay(GuildChangeKind.WeaponEnhance, ConfirmedWeapon, weapon, "smith only");
                _effects.AssertLayout(weapon, "smith window only");
                Debug.Log("[guild growth consumer] single-window and mid-effect relayout completed");
            }
        }

        // Task requirement 5, consumer-contract §2: a refused Art play (the view is disabled here as a test fault) is
        // handled and recorded by the consumer without throwing into the server event, and is not retried later.
        [UnityTest]
        public IEnumerator RefusedPlay_IsRecordedWithoutThrowing_AndIsNotReplayed()
        {
            return _effects.Observe(Steps());

            IEnumerator Steps()
            {
                yield return EnterTown(_fixture);
                GuildWindow guild = Window<GuildWindow>();
                yield return Synchronize(_fixture);
                GuildGrowthEffectView[] views = GuildGrowthEffectConsumerObservation.SceneViews();
                Assert.AreEqual(1, views.Length, "RED missing consumer: the Town owner must have created one effect view");
                views[0].gameObject.SetActive(false);
                // Only this window: a consumer may record the refusal as an error; exceptions are asserted below.
                LogAssert.ignoreFailingMessages = true;
                float refused = _effects.Mark("stronghold broadcast to a disabled view");
                _effects.CaptureLogs();
                Send(_fixture.Peer, StrongholdBroadcast(20));
                yield return _effects.For(1f);
                Assert.AreEqual(1, _effects.Sources(refused).Length, "fixture: the existing owner confirmed the stronghold");
                Assert.IsEmpty(_effects.Logs(refused, LogType.Exception), "the consumer handles a refused play without an exception");
                Assert.IsNotEmpty(_effects.Logs(refused, LogType.Warning, LogType.Error),
                    "the consumer records the refused play (a Unity warning or error log in the event window)");
                LogAssert.ignoreFailingMessages = _logPolicy;

                views[0].gameObject.SetActive(true);
                float enabled = _effects.Mark("view re-enabled");
                yield return _effects.For(NoReplaySeconds);
                _effects.AssertNothingDrawn(enabled, "re-enabling the refused view (no retry queue)");
                guild.SetOpen(true);
                yield return _fixture.Frames(3);
                Assert.IsTrue(ScreenUiProbe.IsShown(Button(guild, "Close")), "the existing guild window still opens after a refused play");
                Debug.Log("[guild growth consumer] refused play record/no throw/no retry completed");
            }
        }

        GuildUiController Owner()
        {
            Scene scene = SceneManager.GetActiveScene();
            GuildUiController[] owners = UnityEngine.Object.FindObjectsByType<GuildUiController>()
                .Where(owner => owner.gameObject.scene == scene).ToArray();
            Assert.AreEqual(1, owners.Length, "fixture: one existing GuildUiController in " + scene.name);
            return owners[0];
        }

        IEnumerator ClickEnhance(BlacksmithWindow smith, int requests)
        {
            yield return ScreenUiProbe.WaitForEventSystem();
            yield return InventoryPanelProbe.Click(_fixture.Mouse, Button(smith, "Enhance"));
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_WeaponEnhance) == requests, "fixture: actual UI46", 5);
            Assert.AreEqual(3u, _fixture.Peer.Last<C_WeaponEnhance>(PacketID.C_WeaponEnhance).expectedInventoryRevision,
                "fixture: 46 carries the synchronized inventory revision");
        }

        // The scripted server moves the player as a portal would: S_MapTransition, then HP for the new map
        // (the DungeonClearRewardPresentationLifecycleTests.MoveTo convention).
        IEnumerator MoveTo(byte map)
        {
            _fixture.Peer.Send(new S_MapTransition { destMapId = map, spawnX = 2, spawnY = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.MapId == map && _fixture.Session.Entry.SceneReady,
                "fixture: scene of map " + map, 10);
            _fixture.Hp();
            yield return _fixture.WaitMap(map);
        }

        // Stronghold 1→2 with the growth marker (kind 3), unlocking the smith; GuildUiConnectionTests' broadcast44.
        static byte[] StrongholdBroadcast(long sequence) =>
            Snapshot(sequence: sequence, guildRevision: 8, kind: 3, treasury: 10, unlocked: true);

        // The personal 44 of a successful enhancement (inventory revision 3→4, weapon 0→1, kind 4).
        static byte[] Enhanced(long sequence, uint guildRevision) => Snapshot(sequence: sequence, guildRevision: guildRevision,
            inventoryRevision: 4, kind: 4, treasury: guildRevision == 8 ? 10 : 40, unlocked: true, enhanced: true);

        static byte[] EnhanceResult(long sequence, uint guildRevision) =>
            Result(action: 2, requested: 3, guildRevision: guildRevision, inventoryRevision: 4, sequence: sequence);
    }

    // Every frame of a consumer case, read from live scene objects only: what each GuildGrowthEffectView in a loaded
    // scene draws (GuildGrowthEffectScreen.Drawn, the Art pretest's reading), what the two Content windows draw while
    // open, and when the existing GuildRequestController raised GrowthConfirmed. Observe runs a case's steps so that no
    // frame goes unsampled. Expected counts and levels stay literal in the cases; nothing here repeats the controller's
    // matching, the effect's timing or the consumer's area calculation.
    internal sealed class GuildGrowthEffectConsumerObservation : IDisposable
    {
        // A window or screen change may take the consumer this many frames to relayout; later frames are judged.
        internal const int RelayoutFrames = 2;
        // A confirmed level reaches the view when GrowthConfirmed arrives, not on a later window or Town event.
        const float StartWithinSeconds = 0.5f;
        // Rounding of shared edges between screen-pixel rectangles.
        const float EdgePixels = 0.5f;
        const float OverlapPixels = 1f;
        const float LogEverySeconds = 0.1f;
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        // Art's read-only HUD verification references (consumer-contract-v3, Art agreement v3); not a product API.
        static readonly string[] HudReferences = { "character_status", "Party_Info", "MinimapPlaceholder", "Quest_Hud", "Gold_bar", "skill_panel" };

        readonly List<Frame> _frames = new List<Frame>();
        readonly List<Source> _sources = new List<Source>();
        readonly List<CapturedLog> _logs = new List<CapturedLog>();
        GuildRequestController _observed;
        bool _capturing;
        string _lastShape;
        float _lastLogged = float.NegativeInfinity;

        public void Dispose()
        {
            if (_observed != null) _observed.GrowthConfirmed -= OnGrowth;
            _observed = null;
            if (_capturing) Application.logMessageReceived -= OnLog;
            _capturing = false;
        }

        internal static GuildGrowthEffectView[] SceneViews() => Resources.FindObjectsOfTypeAll<GuildGrowthEffectView>()
            .Where(view => view.gameObject.scene.IsValid()).ToArray();

        // Runs nested steps frame by frame, sampling after each frame they wait for.
        internal IEnumerator Observe(IEnumerator steps)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(steps);
            Sample();
            while (stack.Count != 0)
            {
                IEnumerator top = stack.Peek();
                if (!top.MoveNext())
                {
                    stack.Pop();
                    continue;
                }
                if (top.Current is IEnumerator nested)
                {
                    stack.Push(nested);
                    continue;
                }
                yield return top.Current;
                Sample();
            }
        }

        internal float Mark(string moment)
        {
            float now = Time.realtimeSinceStartup;
            TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "[GuildGrowthConsumer] mark '{0}' real={1:F4} frame={2} timeScale={3} screen={4}x{5}", moment, now, Time.frameCount,
                Time.timeScale, Screen.width, Screen.height));
            return now;
        }

        internal IEnumerator For(float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until) yield return null;
        }

        // Waits without failing; the case asserts what was or was not seen.
        internal IEnumerator UntilShown(long level, float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (!Latest().Numbers.Contains(level) && Time.realtimeSinceStartup < until) yield return null;
        }

        // Until nothing has been drawn for quietSeconds after something was, or until capSeconds pass.
        internal IEnumerator UntilQuiet(float quietSeconds, float capSeconds)
        {
            float start = Time.realtimeSinceStartup;
            float quietSince = float.NaN;
            bool seen = false;
            while (Time.realtimeSinceStartup - start < capSeconds)
            {
                yield return null;
                if (Latest().Effect.Length != 0)
                {
                    seen = true;
                    quietSince = float.NaN;
                }
                else if (seen && float.IsNaN(quietSince))
                {
                    quietSince = Time.realtimeSinceStartup;
                }
                if (seen && !float.IsNaN(quietSince) && Time.realtimeSinceStartup - quietSince >= quietSeconds) yield break;
            }
        }

        internal Source[] Sources(float since) => _sources.Where(source => source.At > since).ToArray();

        internal Source[] Sources(GuildChangeKind kind, float since) => Sources(since).Where(source => source.Kind == kind).ToArray();

        internal bool DrawnAfter(float since) => Judged(since).Any(frame => frame.Effect.Length != 0);

        internal bool BothShownNow(long first, long second) => Latest().Numbers.Contains(first) && Latest().Numbers.Contains(second);

        internal void AssertStarted(GuildChangeKind kind, long level, float since, string scenario)
        {
            Source[] sources = Sources(kind, since);
            Assert.AreEqual(1, sources.Length, $"fixture: the existing GuildRequestController raised GrowthConfirmed({kind}) once ({scenario})");
            Episode[] plays = Episodes(level, since);
            Assert.IsNotEmpty(plays, $"{scenario}: a confirmed {kind} shows an effect with level {level}; nothing with that level was drawn");
            float delay = plays[0].Start.At - sources[0].At;
            Assert.GreaterOrEqual(delay, 0f, $"{scenario}: the level-{level} effect started before its confirmation");
            Assert.LessOrEqual(delay, StartWithinSeconds, $"{scenario}: the confirmed level is passed on arrival, delay={delay:F3}s");
        }

        internal void AssertOnePlay(GuildChangeKind kind, long level, float since, string scenario)
        {
            AssertStarted(kind, level, since, scenario);
            Episode[] plays = Episodes(level, since);
            Assert.AreEqual(1, plays.Length, $"{scenario}: one confirmed {kind} shows exactly one level-{level} effect; " +
                $"episodes={plays.Length} [{string.Join("; ", plays.Select(play => play.ToString()))}]");
        }

        // The distinct levels any effect drew since the mark are exactly the confirmed ones.
        internal void AssertLevels(float since, params long[] levels)
        {
            long[] drawn = Judged(since).SelectMany(frame => frame.Numbers).Distinct().OrderBy(number => number).ToArray();
            CollectionAssert.AreEqual(levels.OrderBy(number => number).ToArray(), drawn, "only the confirmed levels are shown");
        }

        internal void AssertNothingDrawn(float since, string scenario)
        {
            Frame[] drawn = Judged(since).Where(frame => frame.Effect.Length != 0).ToArray();
            Assert.IsEmpty(drawn.Select(frame => frame.ToString()).Take(5).ToArray(),
                $"{scenario}: no effect is drawn ({drawn.Length} frames drew one)");
        }

        // pressed: just before the gate-closing action; closed: when that action was observed to have happened.
        internal void AssertCancelled(float pressed, float closed, string scenario)
        {
            Frame before = _frames.LastOrDefault(frame => frame.At <= pressed);
            Assert.IsTrue(before != null && before.Effect.Length != 0, $"fixture: an effect was drawn when '{scenario}' began");
            Frame[] after = Judged(closed).Skip(RelayoutFrames).Where(frame => frame.Effect.Length != 0).ToArray();
            Assert.IsEmpty(after.Select(frame => frame.ToString()).Take(5).ToArray(),
                $"{scenario}: CancelAll leaves nothing drawn from the {RelayoutFrames}th frame on ({after.Length} frames drew one)");
        }

        // Every judged frame that draws an effect: each drawn part lies on screen and outside each open window's drawn
        // panel, and an open window stays drawn (making room by hiding a window does not count).
        internal void AssertLayout(float since, string scenario)
        {
            var offScreen = new List<string>();
            var overWindow = new List<string>();
            var hiddenWindow = new List<string>();
            int judged = 0;
            int settled = int.MaxValue;
            Frame previous = null;
            foreach (Frame frame in Judged(since))
            {
                bool changed = previous != null && (previous.GuildOpen != frame.GuildOpen || previous.SmithOpen != frame.SmithOpen ||
                    previous.Screen != frame.Screen);
                settled = changed ? 0 : settled == int.MaxValue ? settled : settled + 1;
                previous = frame;
                if (frame.Effect.Length == 0 || settled < RelayoutFrames) continue;
                judged++;
                if (frame.GuildOpen && frame.GuildPanel == null) hiddenWindow.Add("guild " + frame);
                if (frame.SmithOpen && frame.SmithPanel == null) hiddenWindow.Add("smith " + frame);
                var screen = new Rect(0f, 0f, frame.Screen.x, frame.Screen.y);
                foreach (GuildGrowthEffectGraphic part in frame.Effect)
                {
                    string at = part + " @" + frame.FrameCount;
                    if (!Inside(part.ScreenRect, screen)) offScreen.Add(at);
                    if (Covers(part, frame.GuildOpen, frame.GuildPanel)) overWindow.Add("guild " + at);
                    if (Covers(part, frame.SmithOpen, frame.SmithPanel)) overWindow.Add("smith " + at);
                }
            }
            TestContext.Out.WriteLine($"[GuildGrowthConsumer] layout '{scenario}' judgedFrames={judged} offScreen={offScreen.Count} " +
                $"overWindow={overWindow.Count} hiddenWindow={hiddenWindow.Count}");
            Assert.Greater(judged, 0, $"fixture: '{scenario}' has settled frames that draw an effect");
            Assert.IsEmpty(hiddenWindow.Take(5).ToArray(), $"{scenario}: open windows stay drawn while an effect plays");
            Assert.IsEmpty(offScreen.Take(5).ToArray(), $"{scenario}: every drawn effect part lies whole on screen");
            Assert.IsEmpty(overWindow.Take(5).ToArray(), $"{scenario}: no drawn effect part covers an open window panel");
        }

        internal void AssertWindowButtonsOnTop(Component window, params string[] buttons)
        {
            foreach (string name in buttons)
            {
                Component button = Button(window, name);
                GameObject[] hits = ScreenUiProbe.RaycastHits(ScreenUiProbe.ScreenPoint(button));
                string first = hits.Length != 0 ? hits[0].name : "<none>";
                Assert.IsTrue(hits.Length != 0 && hits[0].transform.IsChildOf(button.transform),
                    $"{window.name}/{name} stays the topmost pointer target while effects play; first hit={first}");
            }
        }

        internal void AssertEffectTakesNoPointer()
        {
            Frame latest = Latest();
            Assert.IsNotEmpty(latest.Effect, "fixture: an effect is drawn for the pointer check");
            foreach (GuildGrowthEffectGraphic part in latest.Effect)
            {
                GameObject[] hits = ScreenUiProbe.RaycastHits(GuildGrowthEffectScreen.ScreenCentre(part));
                Assert.IsFalse(hits.Any(hit => hit.GetComponentInParent<GuildGrowthEffectView>() != null),
                    "the effect takes no pointer at " + part);
            }
        }

        // The current connection's consumer delegates: GrowthConfirmed's invocation list without this observation's own
        // handler. InventoryPanelProbe.Targets reads the same compiler backing field but returns targets, not delegates.
        internal Action<GuildChangeKind>[] ConsumerDelegates(GuildRequestController guild)
        {
            FieldInfo field = typeof(GuildRequestController).GetField("GrowthConfirmed", Private);
            Assert.IsNotNull(field, "fixture: GrowthConfirmed is a field-like event");
            var handler = (Delegate)field.GetValue(guild);
            if (handler == null) return Array.Empty<Action<GuildChangeKind>>();
            return handler.GetInvocationList().Where(item => !ReferenceEquals(item.Target, this))
                .Cast<Action<GuildChangeKind>>().ToArray();
        }

        internal void CaptureLogs()
        {
            if (_capturing) return;
            Application.logMessageReceived += OnLog;
            _capturing = true;
        }

        internal string[] Logs(float since, params LogType[] types) => _logs
            .Where(log => log.At > since && types.Contains(log.Type)).Select(log => log.Type + ": " + log.Text).ToArray();

        // Log only: there is no approved HUD overlap or glyph-size criterion; the capture lane judges readability.
        internal void LogHudAndReadability(float since)
        {
            Frame[] drawn = Judged(since).Where(frame => frame.Effect.Length != 0).ToArray();
            foreach (string name in HudReferences)
            {
                RectTransform[] found = Resources.FindObjectsOfTypeAll<RectTransform>()
                    .Where(rect => rect.name == name && rect.gameObject.scene.IsValid() && rect.gameObject.activeInHierarchy).ToArray();
                if (found.Length != 1)
                {
                    TestContext.Out.WriteLine($"[GuildGrowthConsumer] hud-reference {name} found={found.Length} (log only)");
                    continue;
                }
                Rect hud = ScreenUiProbe.ScreenRect(found[0]);
                float overlap = drawn.SelectMany(frame => frame.Effect)
                    .Select(part => GuildGrowthEffectScreen.OverlapArea(part.ScreenRect, hud)).DefaultIfEmpty(0f).Max();
                TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "[GuildGrowthConsumer] hud-reference {0} rect=({1:F0},{2:F0},{3:F0},{4:F0}) maxEffectOverlapPx2={5:F0} (log only)",
                    name, hud.x, hud.y, hud.width, hud.height, overlap));
            }
            foreach (long level in drawn.SelectMany(frame => frame.Numbers).Distinct())
            {
                float[] heights = drawn.SelectMany(frame => frame.Effect).Where(part => part.Numbers.Contains(level))
                    .Select(part => part.ScreenRect.height).ToArray();
                TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "[GuildGrowthConsumer] level-{0} label glyph height px min={1:F1} max={2:F1} screen={3}x{4} (log only)",
                    level, heights.Min(), heights.Max(), Screen.width, Screen.height));
            }
        }

        static bool Inside(Rect box, Rect screen) => box.xMin >= screen.xMin - EdgePixels && box.yMin >= screen.yMin - EdgePixels &&
            box.xMax <= screen.xMax + EdgePixels && box.yMax <= screen.yMax + EdgePixels;

        static bool Covers(GuildGrowthEffectGraphic part, bool open, Rect? panel) =>
            open && panel.HasValue && GuildGrowthEffectScreen.OverlapArea(part.ScreenRect, panel.Value) > OverlapPixels;

        IEnumerable<Frame> Judged(float since) => _frames.Where(frame => frame.At > since);

        Frame Latest() => _frames[_frames.Count - 1];

        Episode[] Episodes(long level, float since)
        {
            var plays = new List<Episode>();
            Frame[] frames = Judged(since).ToArray();
            int lastShown = int.MinValue;
            for (int index = 0; index < frames.Length; index++)
            {
                if (!frames[index].Numbers.Contains(level)) continue;
                // A gap of up to RelayoutFrames unshown frames is one effect (a relayout), not a second play.
                if (lastShown == int.MinValue || index - lastShown > RelayoutFrames + 1) plays.Add(new Episode(frames[index]));
                plays[plays.Count - 1].End = frames[index];
                lastShown = index;
            }
            return plays.ToArray();
        }

        void Sample()
        {
            UnityClientSession session = UnityClientSession.Instance;
            GuildRequestController guild = session != null ? session.Guild : null;
            if (!ReferenceEquals(guild, _observed))
            {
                if (_observed != null) _observed.GrowthConfirmed -= OnGrowth;
                _observed = guild;
                if (guild != null) guild.GrowthConfirmed += OnGrowth;
            }
            GuildGrowthEffectGraphic[] effect = UnityEngine.Object.FindObjectsByType<GuildGrowthEffectView>()
                .SelectMany(view => GuildGrowthEffectScreen.Drawn(view.transform)).ToArray();
            GuildWindow guildWindow = ActiveScene<GuildWindow>();
            BlacksmithWindow smithWindow = ActiveScene<BlacksmithWindow>();
            var frame = new Frame(Time.realtimeSinceStartup, Time.frameCount, new Vector2(Screen.width, Screen.height), effect,
                guildWindow != null && guildWindow.IsOpen, Panel(guildWindow, guildWindow != null && guildWindow.IsOpen),
                smithWindow != null && smithWindow.IsOpen, Panel(smithWindow, smithWindow != null && smithWindow.IsOpen));
            _frames.Add(frame);
            string shape = frame.Shape;
            if (shape != _lastShape || (effect.Length != 0 && frame.At - _lastLogged >= LogEverySeconds))
            {
                _lastLogged = frame.At;
                TestContext.Out.WriteLine("[GuildGrowthConsumer] " + frame);
            }
            _lastShape = shape;
        }

        static T ActiveScene<T>() where T : Component
        {
            Scene scene = SceneManager.GetActiveScene();
            T[] found = UnityEngine.Object.FindObjectsByType<T>().Where(item => item.gameObject.scene == scene).ToArray();
            return found.Length == 1 ? found[0] : null;
        }

        // The window's whole drawn area (panel, inset, texts and buttons), read from what is drawn, not from its layout.
        static Rect? Panel(Component window, bool open)
        {
            if (!open) return null;
            Rect[] parts = GuildGrowthEffectScreen.Drawn(window.transform).Select(part => part.ScreenRect).ToArray();
            if (parts.Length == 0) return null;
            return Rect.MinMaxRect(parts.Min(part => part.xMin), parts.Min(part => part.yMin),
                parts.Max(part => part.xMax), parts.Max(part => part.yMax));
        }

        void OnGrowth(GuildChangeKind kind)
        {
            var source = new Source(kind, Time.realtimeSinceStartup, Time.frameCount);
            _sources.Add(source);
            // Logged for the raw record only; expectations never read these State values.
            TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "[GuildGrowthConsumer] GrowthConfirmed {0} real={1:F4} frame={2} stateStronghold={3} stateWeapon={4}", kind, source.At,
                source.FrameCount, _observed?.State.StrongholdLevel, _observed?.State.WeaponLevel));
        }

        void OnLog(string text, string stack, LogType type) => _logs.Add(new CapturedLog(Time.realtimeSinceStartup, type, text));

        internal sealed class Source
        {
            internal Source(GuildChangeKind kind, float at, int frameCount)
            {
                Kind = kind;
                At = at;
                FrameCount = frameCount;
            }

            internal GuildChangeKind Kind { get; }
            internal float At { get; }
            internal int FrameCount { get; }
        }

        internal sealed class Frame
        {
            internal Frame(float at, int frameCount, Vector2 screen, GuildGrowthEffectGraphic[] effect, bool guildOpen, Rect? guildPanel,
                bool smithOpen, Rect? smithPanel)
            {
                At = at;
                FrameCount = frameCount;
                Screen = screen;
                Effect = effect;
                Numbers = effect.SelectMany(part => part.Numbers).Distinct().OrderBy(number => number).ToArray();
                GuildOpen = guildOpen;
                GuildPanel = guildPanel;
                SmithOpen = smithOpen;
                SmithPanel = smithPanel;
            }

            internal float At { get; }
            internal int FrameCount { get; }
            internal Vector2 Screen { get; }
            internal GuildGrowthEffectGraphic[] Effect { get; }
            internal long[] Numbers { get; }
            internal bool GuildOpen { get; }
            internal Rect? GuildPanel { get; }
            internal bool SmithOpen { get; }
            internal Rect? SmithPanel { get; }
            internal string Shape => string.Join("|", Effect.Select(part => part.Path + (part.Text ?? string.Empty))) +
                $"#{GuildOpen}{SmithOpen}{Screen}";

            public override string ToString() => string.Format(CultureInfo.InvariantCulture,
                "frame={0} real={1:F4} screen={2}x{3} numbers=[{4}] guild={5}{6} smith={7}{8} effect=[{9}]", FrameCount, At, Screen.x, Screen.y,
                string.Join(",", Numbers), GuildOpen ? "open" : "closed", Box(GuildPanel), SmithOpen ? "open" : "closed", Box(SmithPanel),
                string.Join("; ", Effect.Select(part => part.ToString())));

            static string Box(Rect? rect) => rect.HasValue
                ? string.Format(CultureInfo.InvariantCulture, "({0:F0},{1:F0},{2:F0},{3:F0})", rect.Value.x, rect.Value.y,
                    rect.Value.width, rect.Value.height)
                : string.Empty;
        }

        sealed class Episode
        {
            internal Episode(Frame start)
            {
                Start = start;
                End = start;
            }

            internal Frame Start { get; }
            internal Frame End { get; set; }

            public override string ToString() => string.Format(CultureInfo.InvariantCulture, "frames {0}-{1} real {2:F3}-{3:F3}",
                Start.FrameCount, End.FrameCount, Start.At, End.At);
        }

        sealed class CapturedLog
        {
            internal CapturedLog(float at, LogType type, string text)
            {
                At = at;
                Type = type;
                Text = text;
            }

            internal float At { get; }
            internal LogType Type { get; }
            internal string Text { get; }
        }
    }
}
