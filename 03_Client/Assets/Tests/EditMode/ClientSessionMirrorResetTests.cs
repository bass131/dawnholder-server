using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Network;
using Dawnholder.Client.Prediction;
using Dawnholder.Client.State;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Tests
{
    public sealed class ClientSessionMirrorResetTests
    {
        readonly List<GameObject> _objects = new();
        readonly List<Type> _singletons = new();
        PartyState _party;
        QuestState _quest;
        CharacterClass? _class;
        int _latency;
        bool _hadPreference;
        int _preference;
        const string Preference = "SelectedCharacterClass";
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        T Component<T>(bool singleton = true) where T : Component
        {
            var go = new GameObject(typeof(T).Name + " reset fixture");
            go.SetActive(false); // avoid scene/asset loading; exercise the lifecycle methods explicitly
            _objects.Add(go);
            T component = go.AddComponent<T>();
            if (singleton)
            {
                var property = typeof(T).GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
                Assert.IsNull(property.GetValue(null), "fixture must not overwrite an existing scene owner");
                property.GetSetMethod(true).Invoke(null, new object[] { component });
                _singletons.Add(typeof(T));
            }
            return component;
        }
        static IDictionary Dictionary(object owner, string field) =>
            (IDictionary)owner.GetType().GetField(field, PrivateInstance).GetValue(owner);
        static void StaticSessionValue(string property, object value) => typeof(UnityClientSession)
            .GetProperty(property).GetSetMethod(true).Invoke(null, new[] { value });

        [SetUp] public void Setup()
        {
            _class = ClassLoadout.SessionSelectedClass;
            _latency = UnityClientSession.SimulatedLatencyMs;
            _hadPreference = PlayerPrefs.HasKey(Preference);
            _preference = PlayerPrefs.GetInt(Preference);
            _party = Component<PartyState>();
            _quest = Component<QuestState>();
        }
        [TearDown] public void Teardown()
        {
            UnityClientSession.Instance?.Cleanup();
            UnityClientSession.ConsumePendingSpawn();
            foreach (Type type in _singletons)
                type.GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { null });
            _singletons.Clear();
            foreach (var go in _objects) UnityEngine.Object.DestroyImmediate(go);
            _objects.Clear();
            ClassLoadout.SessionSelectedClass = _class;
            UnityClientSession.SimulatedLatencyMs = _latency;
            if (_hadPreference) PlayerPrefs.SetInt(Preference, _preference);
            else PlayerPrefs.DeleteKey(Preference);
        }
        void SeedMirrors()
        {
            _party.ApplyUpdate(3, 10, 10, 11, 0, 1);
            _party.SetPendingInvite(12, 1);
            _party.SetLastError(2);
            _quest.ApplyUpdate(4, 5);
            StaticSessionValue("PendingSpawnX", 12f);
            StaticSessionValue("PendingSpawnY", 3f);
            StaticSessionValue("PendingMapId", 2);
            StaticSessionValue("HasPendingSpawn", true);
        }
        void AssertMirrorsEmpty()
        {
            Assert.AreEqual(0, _party.PartyId);
            Assert.AreEqual(0, _party.LeaderEntityId);
            Assert.AreEqual(0, _party.Member0EntityId);
            Assert.AreEqual(0, _party.Member1EntityId);
            Assert.AreEqual(0, _party.Member0Class);
            Assert.AreEqual(0, _party.Member1Class);
            Assert.IsFalse(_party.HasPendingInvite);
            Assert.AreEqual(0, _party.PendingInviterEntityId);
            Assert.AreEqual(0, _party.PendingInviterClass);
            Assert.AreEqual(-1, _party.LastErrorReason);
            Assert.AreEqual(0, _quest.CurrentCount);
            Assert.AreEqual(0, _quest.TargetCount);
            Assert.IsFalse(UnityClientSession.HasPendingSpawn);
            Assert.AreEqual(0, UnityClientSession.PendingSpawnX);
            Assert.AreEqual(0, UnityClientSession.PendingSpawnY);
            Assert.AreEqual(0, UnityClientSession.PendingMapId);
        }

        [Test]
        public void ResetObservers_SeeAllMirrorsCleared_AndUserSelectionPreserved()
        {
            SeedMirrors();
            ClassLoadout.SessionSelectedClass = CharacterClass.Mage;
            PlayerPrefs.SetInt(Preference, 1);
            UnityClientSession.SimulatedLatencyMs = 137;
            int partyNotifications = 0, questNotifications = 0;
            _party.OnPartyUpdated += () => { AssertMirrorsEmpty(); partyNotifications++; };
            _quest.OnQuestUpdated += () => { AssertMirrorsEmpty(); questNotifications++; };
            NetworkService.ResetGlobalSessionMirrors();
            AssertMirrorsEmpty();
            Assert.AreEqual(1, partyNotifications);
            Assert.AreEqual(1, questNotifications);
            Assert.AreEqual(CharacterClass.Mage, ClassLoadout.SessionSelectedClass);
            Assert.AreEqual(1, PlayerPrefs.GetInt(Preference));
            Assert.AreEqual(137, UnityClientSession.SimulatedLatencyMs);
        }

        [Test]
        public void ThrowingResetObserver_DoesNotSkipOtherObserversOrMirrors()
        {
            SeedMirrors();
            int laterParty = 0, laterQuest = 0;
            _party.OnPartyUpdated += () => throw new InvalidOperationException("injected party observer");
            _party.OnPartyUpdated += () => { AssertMirrorsEmpty(); laterParty++; };
            _quest.OnQuestUpdated += () => throw new InvalidOperationException("injected quest observer");
            _quest.OnQuestUpdated += () => { AssertMirrorsEmpty(); laterQuest++; };
            var error = Assert.Throws<AggregateException>(() => NetworkService.ResetGlobalSessionMirrors());
            Assert.AreEqual(2, error.Flatten().InnerExceptions.Count);
            Assert.AreEqual(1, laterParty);
            Assert.AreEqual(1, laterQuest);
            AssertMirrorsEmpty();
        }

        [Test]
        public void SessionCleanup_ResetsSessionFieldsAndRoster_WithoutClearingNewInstance()
        {
            var queue = new ManualConnectionQueue();
            var old = new UnityClientSession(() => true, queue.Post);
            var current = new UnityClientSession(() => true, queue.Post);
            try
            {
                old.Publish();
                SessionTestTools.Handshake(old, queue);
                SessionTestTools.Set(old, "LocalEntityId", (int?)10);
                SessionTestTools.Set(old, "LastReceivedServerTick", 100);
                var roster = SessionTestTools.Roster(old);
                SessionTestTools.Call(roster, "BeginTransition", "Town");
                int calls = 0;
                SessionTestTools.Call(roster, "TryBuffer", "old roster", (Action)(() => calls++));
                current.Publish();
                old.Cleanup();
                old.Cleanup();
                Assert.IsTrue(old.IsClosed);
                Assert.IsFalse(old.HandshakeOk);
                Assert.IsNull(old.LocalEntityId);
                Assert.AreEqual(0, old.LastReceivedServerTick);
                Assert.IsFalse((bool)roster.GetType().GetProperty("IsPending").GetValue(roster));
                Assert.AreEqual(0, ((ICollection)roster.GetType().GetField("_buffer", PrivateInstance).GetValue(roster)).Count);
                Assert.AreEqual(0, calls);
                Assert.AreSame(current, UnityClientSession.Instance);
                Assert.IsFalse(current.IsClosed);
            }
            finally { old.Cleanup(); current.Cleanup(); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ArmedTeleport_IsClearedOnSessionReset_ButNewSessionTeleportStillWorks(bool reset)
        {
            var movement = Component<LocalPlayerMovement>();
            var predictor = new PlayerPredictor(new MoveParams(5, 8));
            predictor.SetInitialPosition(Vector2.zero);
            typeof(LocalPlayerMovement).GetField("_predictor", PrivateInstance).SetValue(movement, predictor);
            int arrivals = 0;
            movement.NotifyTeleport(new Vector3(3, 4, 0));
            movement.ArmTeleportSnap(() => arrivals++);
            if (reset) NetworkService.ResetGlobalSessionMirrors();
            Assert.AreEqual(reset ? (Vector3?)null : new Vector3(3, 4, 0), movement.ConsumeTeleportDepartPos());
            movement.OnServerSnapshot(0.01f, 0, 0, 0, 1, 0, (byte)AnimState.Idle);
            Assert.AreEqual(reset ? 0 : 1, arrivals);
            Assert.AreEqual(reset ? 0f : 0.01f, predictor.Position.x, 0.0001f);
            if (reset)
            {
                movement.ArmTeleportSnap(() => arrivals++);
                movement.OnServerSnapshot(0, 0, 0, 0, 2, 0, (byte)AnimState.Idle);
                Assert.AreEqual(1, arrivals);
            }
        }

        [Test]
        public void ResetClearsRegistryBookkeeping()
        {
            var remote = Component<RemoteEntityRegistry>();
            var enemies = Component<EnemyRegistry>();
            // Bookkeeping fixture only; rendered GameObject destruction belongs to PlayMode.
            Dictionary(remote, "_entities").Add(55, null);
            Dictionary(remote, "_spawnedClasses").Add(55, CharacterClass.Knight);
            Dictionary(enemies, "_lastAnimState").Add(66, (byte)1);
            Assert.AreEqual(1, Dictionary(remote, "_entities").Count);
            Assert.AreEqual(1, Dictionary(enemies, "_lastAnimState").Count);
            NetworkService.ResetGlobalSessionMirrors();
            Assert.IsEmpty(remote.Entities);
            Assert.AreEqual(0, Dictionary(remote, "_spawnedClasses").Count);
            Assert.AreEqual(0, Dictionary(enemies, "_lastAnimState").Count);
        }

        [Test]
        public void FacadeDisconnect_ResetsPingAccumulator()
        {
            var facade = Component<NetworkService>();
            using var h = new ConnectionHarness();
            typeof(NetworkService).GetField("_lifetime", PrivateInstance).SetValue(facade, h.Owner);
            h.ResetObserver = () => SessionTestTools.Call(facade, "ResetSessionMirrors");
            h.Connect();
            typeof(NetworkService).GetField("_accumSec", PrivateInstance).SetValue(facade, 0.75f);
            facade.Disconnect();
            Assert.AreEqual(0f, typeof(NetworkService).GetField("_accumSec", PrivateInstance).GetValue(facade));
            Assert.IsFalse(facade.IsConnected);
        }
    }
}
