using System;
using System.Collections.Generic;
using Dawnholder.Client.Network;
using NUnit.Framework;

namespace Dawnholder.Client.Tests
{
    public sealed class MapEntryCoordinatorTests
    {
        sealed class EntryProbe
        {
            public readonly List<string> Effects = new();
            public readonly List<Exception> Errors = new();
            public readonly MapEntryCoordinator Entry;

            public EntryProbe()
            {
                Entry = new MapEntryCoordinator(
                    entry =>
                    {
                        Assert.IsFalse(entry.IsGameplayReady, "input must stay locked during spawn");
                        Effects.Add($"spawn:{entry.MapId}:{entry.SpawnX}:{entry.SpawnY}");
                    },
                    (hp, max) => Effects.Add($"hp:{hp}/{max}"),
                    Errors.Add,
                    entry =>
                    {
                        Assert.IsFalse(entry.IsGameplayReady, "bindings finish before input resumes");
                        Effects.Add("bindings");
                    });
            }
        }

        static void ReadyScene(MapEntryCoordinator entry, long epoch, ulong sceneHandle = 41)
        {
            entry.BindScene(epoch, sceneHandle);
            entry.BindPlayer(epoch);
            entry.BindRegistries(epoch);
        }

        [Test]
        public void NewOwner_WaitsWithoutGameplayOrSpawn()
        {
            var probe = new EntryProbe();
            Assert.AreEqual(MapEntryState.Waiting, probe.Entry.State);
            Assert.IsFalse(probe.Entry.IsGameplayReady);
            Assert.IsFalse(probe.Entry.SpawnApplied);
            Assert.IsFalse(probe.Entry.IsCurrent(probe.Entry.Epoch));
            Assert.IsEmpty(probe.Effects);
        }

        // HP-first models packets arriving before scene/player binding; HP-last models
        // a bound player waiting for the server's initial authoritative HP packet.
        [TestCase("HSPR")]
        [TestCase("SPRH")]
        [TestCase("RPSH")]
        [TestCase("HPRS")]
        public void FirstTownEntry_DifferentReadinessOrders_ApplySpawnThenHpBeforeReady(string order)
        {
            var probe = new EntryProbe();
            var entry = probe.Entry;
            long epoch = entry.Begin(0, 12, -4, true);
            Assert.AreEqual(MapEntryState.Entering, entry.State);
            string steps = order;
            for (int i = 0; i < steps.Length; i++)
            {
                switch (steps[i])
                {
                    case 'H': entry.ReceiveHp(epoch, 37, 100); break;
                    case 'S': entry.BindScene(epoch, 41); break;
                    case 'P': entry.BindPlayer(epoch); break;
                    case 'R': entry.BindRegistries(epoch); break;
                }
                if (i < steps.Length - 1)
                {
                    Assert.IsFalse(entry.IsGameplayReady);
                    Assert.IsEmpty(probe.Effects, "partial readiness must not apply spawn or HP");
                }
            }
            CollectionAssert.AreEqual(new[] { "spawn:0:12:-4", "hp:37/100", "bindings" }, probe.Effects);
            Assert.AreEqual(MapEntryState.Ready, entry.State);
            Assert.IsTrue(entry.IsGameplayReady);
            Assert.IsTrue(entry.SpawnApplied);
            Assert.AreEqual(41, entry.SceneHandle);
            Assert.IsEmpty(probe.Errors);
        }

        [Test]
        public void MissingFirstHp_RemainsEntering_ThenUsesLatestAuthorityWithoutHudDependency()
        {
            var probe = new EntryProbe();
            long epoch = probe.Entry.Begin(1, 3, 9, true);
            ReadyScene(probe.Entry, epoch);
            Assert.AreEqual(MapEntryState.Entering, probe.Entry.State);
            Assert.IsFalse(probe.Entry.SpawnApplied);
            Assert.IsEmpty(probe.Effects);
            probe.Entry.ReceiveHp(epoch, 0, 100);
            Assert.IsTrue(probe.Entry.IsGameplayReady, "zero HP is still an authoritative HP value");
            Assert.AreEqual(0, probe.Entry.CurrentHp);
            probe.Entry.ReceiveHp(epoch, 24, 100);
            CollectionAssert.AreEqual(new[] { "spawn:1:3:9", "hp:0/100", "bindings", "hp:24/100" }, probe.Effects);
            Assert.AreEqual(24, probe.Entry.CurrentHp);
        }

        [Test]
        public void HpBeforeBindings_UsesLatestValue_AndRepeatedReadySignalsDoNotReplaySpawn()
        {
            var probe = new EntryProbe();
            long epoch = probe.Entry.Begin(0, 1, 2, true);
            probe.Entry.ReceiveHp(epoch, 90, 100);
            probe.Entry.ReceiveHp(epoch, 45, 100);
            ReadyScene(probe.Entry, epoch);
            ReadyScene(probe.Entry, epoch);
            CollectionAssert.AreEqual(new[] { "spawn:0:1:2", "hp:45/100", "bindings" }, probe.Effects);
        }

        [Test]
        public void NewEpochInSameMap_RejectsOldCompletionAndHp_AndNeedsFreshHp()
        {
            var probe = new EntryProbe();
            long previous = probe.Entry.Begin(1, 1, 2, true);
            probe.Entry.ReceiveHp(previous, 77, 100);
            ReadyScene(probe.Entry, previous);
            probe.Effects.Clear();
            long current = probe.Entry.Begin(1, 8, 9, true);
            Assert.AreNotEqual(previous, current);
            ReadyScene(probe.Entry, previous, 10);
            probe.Entry.ReceiveHp(previous, 1, 100);
            probe.Entry.Fail(previous, new InvalidOperationException("stale failure"));
            Assert.IsFalse(probe.Entry.SceneReady);
            Assert.IsFalse(probe.Entry.HasHp);
            ReadyScene(probe.Entry, current, 42);
            Assert.IsFalse(probe.Entry.IsGameplayReady, "HP from the previous map entry cannot satisfy this barrier");
            probe.Entry.ReceiveHp(current, 66, 100);
            CollectionAssert.AreEqual(new[] { "spawn:1:8:9", "hp:66/100", "bindings" }, probe.Effects);
            Assert.AreEqual(42, probe.Entry.SceneHandle);
            Assert.IsEmpty(probe.Errors);
        }

        [Test]
        public void Ending_OnlyNeedsScene_AndNeverEnablesGameplayOrMarksPlayerSpawned()
        {
            var probe = new EntryProbe();
            long epoch = probe.Entry.Begin(3, 0, 0, false);
            Assert.AreEqual(MapEntryState.Entering, probe.Entry.State);
            probe.Entry.BindScene(epoch, 99);
            Assert.AreEqual(MapEntryState.Ready, probe.Entry.State);
            Assert.IsFalse(probe.Entry.IsGameplayReady);
            Assert.IsFalse(probe.Entry.SpawnApplied);
            Assert.IsFalse(probe.Entry.HasHp);
            CollectionAssert.AreEqual(new[] { "spawn:3:0:0", "bindings" }, probe.Effects);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CloseBeforeOrAfterReady_InvalidatesEpochAndLateNotifications(bool readyBeforeClose)
        {
            var probe = new EntryProbe();
            long epoch = probe.Entry.Begin(1, 1, 2, true);
            if (readyBeforeClose)
            {
                probe.Entry.ReceiveHp(epoch, 50, 100);
                ReadyScene(probe.Entry, epoch);
            }
            probe.Effects.Clear();
            probe.Entry.Close();
            long closedEpoch = probe.Entry.Epoch;
            probe.Entry.Close();
            ReadyScene(probe.Entry, epoch);
            probe.Entry.ReceiveHp(epoch, 90, 100);
            probe.Entry.Fail(epoch, new Exception("late"));
            Assert.AreEqual(MapEntryState.Closed, probe.Entry.State);
            Assert.AreEqual(closedEpoch, probe.Entry.Epoch, "close is idempotent");
            Assert.IsFalse(probe.Entry.IsCurrent(epoch));
            Assert.IsFalse(probe.Entry.IsGameplayReady);
            Assert.IsFalse(probe.Entry.HasHp);
            Assert.IsFalse(probe.Entry.SpawnApplied);
            Assert.IsEmpty(probe.Effects);
            Assert.IsEmpty(probe.Errors);
            Assert.Throws<ObjectDisposedException>(() => probe.Entry.Begin(0, 0, 0, true));
        }

        [TestCase("spawn")]
        [TestCase("hp")]
        [TestCase("bindings")]
        public void RequiredApplicationThrows_FailsOnce_AndCannotBecomeReady(string failurePhase)
        {
            var error = new InvalidOperationException(failurePhase);
            var calls = new List<string>();
            var errors = new List<Exception>();
            void Apply(string phase)
            {
                calls.Add(phase);
                if (phase == failurePhase) throw error;
            }
            var entry = new MapEntryCoordinator(_ => Apply("spawn"), (_, _) => Apply("hp"),
                errors.Add, _ => Apply("bindings"));
            long epoch = entry.Begin(0, 1, 2, true);
            entry.ReceiveHp(epoch, 50, 100);
            ReadyScene(entry, epoch);
            int callsAtFailure = calls.Count;
            ReadyScene(entry, epoch);
            entry.ReceiveHp(epoch, 100, 100);
            entry.Fail(epoch, new Exception("duplicate"));
            Assert.AreEqual(MapEntryState.Failed, entry.State);
            Assert.IsFalse(entry.IsGameplayReady);
            Assert.AreSame(error, entry.LastError);
            CollectionAssert.AreEqual(new[] { error }, errors);
            Assert.AreEqual(callsAtFailure, calls.Count, "failed entry must not apply late work");
        }

        [TestCase("spawn", 1)]
        [TestCase("hp", 2)]
        [TestCase("bindings", 3)]
        public void DisconnectDuringApplication_DoesNotPublishReadyOrRunRemainingEffects(string closePhase, int expectedCalls)
        {
            var calls = new List<string>();
            MapEntryCoordinator entry = null;
            void Apply(string phase)
            {
                calls.Add(phase);
                if (phase == closePhase) entry.Close();
            }
            entry = new MapEntryCoordinator(_ => Apply("spawn"), (_, _) => Apply("hp"),
                error => Assert.Fail(error.ToString()), _ => Apply("bindings"));
            long epoch = entry.Begin(0, 1, 2, true);
            entry.ReceiveHp(epoch, 50, 100);
            ReadyScene(entry, epoch);
            Assert.AreEqual(MapEntryState.Closed, entry.State);
            Assert.IsFalse(entry.IsGameplayReady);
            Assert.IsFalse(entry.SpawnApplied);
            Assert.AreEqual(expectedCalls, calls.Count);
        }

        [Test]
        public void SceneFailureBeforeBinding_RejectsLateSuccessAndReportsOriginalErrorOnce()
        {
            var probe = new EntryProbe();
            long epoch = probe.Entry.Begin(2, 4, 5, true);
            var error = new InvalidOperationException("scene start failed");
            probe.Entry.Fail(epoch, error);
            ReadyScene(probe.Entry, epoch);
            probe.Entry.ReceiveHp(epoch, 40, 100);
            probe.Entry.Fail(epoch, error);
            Assert.AreEqual(MapEntryState.Failed, probe.Entry.State);
            Assert.IsFalse(probe.Entry.IsGameplayReady);
            Assert.IsEmpty(probe.Effects);
            CollectionAssert.AreEqual(new[] { error }, probe.Errors);
        }

        [TestCase("spawn")]
        [TestCase("bindings")]
        public void CallbackStartsFullyBoundNewEntry_NewEntryBecomesReadyWithoutAnotherPacket(string transitionPhase)
        {
            var effects = new List<string>();
            MapEntryCoordinator entry = null;
            long nextEpoch = 0;
            void Transition()
            {
                nextEpoch = entry.Begin(1, 8, 9, true);
                entry.ReceiveHp(nextEpoch, 20, 200);
                ReadyScene(entry, nextEpoch, 52);
            }
            entry = new MapEntryCoordinator(
                current =>
                {
                    byte map = current.MapId;
                    effects.Add($"spawn:{map}");
                    if (map == 0 && transitionPhase == "spawn") Transition();
                },
                (hp, max) => effects.Add($"hp:{hp}/{max}"),
                error => Assert.Fail(error.ToString()),
                current =>
                {
                    byte map = current.MapId;
                    effects.Add($"bindings:{map}");
                    if (map == 0 && transitionPhase == "bindings") Transition();
                });
            long oldEpoch = entry.Begin(0, 1, 2, true);
            entry.ReceiveHp(oldEpoch, 50, 100);
            ReadyScene(entry, oldEpoch);

            Assert.IsTrue(entry.IsGameplayReady, "a fully supplied replacement must not need a duplicate packet to wake it");
            Assert.AreEqual(nextEpoch, entry.Epoch);
            Assert.AreEqual(52, entry.SceneHandle);
            Assert.AreEqual(20, entry.CurrentHp);
            Assert.IsFalse(entry.IsCurrent(oldEpoch));
            string[] expected = transitionPhase == "spawn"
                ? new[] { "spawn:0", "spawn:1", "hp:20/200", "bindings:1" }
                : new[] { "spawn:0", "hp:50/100", "bindings:0", "spawn:1", "hp:20/200", "bindings:1" };
            CollectionAssert.AreEqual(expected, effects);
        }

        [Test]
        public void SceneIdentity_PreservesUpperBitsAcrossEntriesWithIdenticalLowerBits()
        {
            var probe = new EntryProbe();
            const ulong firstHandle = (1UL << 40) | 41;
            const ulong nextHandle = (2UL << 40) | 41;
            long old = probe.Entry.Begin(3, 0, 0, false);
            probe.Entry.BindScene(old, firstHandle);
            Assert.AreEqual(firstHandle, probe.Entry.SceneHandle);
            long current = probe.Entry.Begin(3, 0, 0, false);
            probe.Entry.BindScene(current, nextHandle);
            probe.Entry.BindScene(old, firstHandle);
            Assert.AreEqual(nextHandle, probe.Entry.SceneHandle);
            Assert.AreNotEqual(firstHandle, probe.Entry.SceneHandle);
        }
    }
}
