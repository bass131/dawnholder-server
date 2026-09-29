using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Input;
using Dawnholder.Client.Network;
using Dawnholder.Client.Prediction;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // EditMode view fixture: real session/entry/terrain binding, no physical scene load.
    // Inactive objects avoid Unity lifecycle side effects; only singleton identity and
    // the player's ordinary constructor state are supplied. Entry readiness is never set.
    internal sealed class EntryBindingFixture : IDisposable
    {
        readonly List<GameObject> _objects = new();
        readonly List<Type> _singletons = new();
        internal readonly UnityClientSession Session;
        internal readonly LocalPlayerMovement Player;
        internal readonly PlayerPredictor Predictor;
        internal readonly RemoteEntityRegistry Remote;
        internal readonly EnemyRegistry Enemies;
        internal const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        internal EntryBindingFixture(UnityClientSession session)
        {
            Session = session;
            Player = Component<LocalPlayerMovement>(true);
            Predictor = new PlayerPredictor(new MoveParams(5, 8));
            SetField(Player, "_predictor", Predictor);
            Remote = Component<RemoteEntityRegistry>(true);
            Enemies = Component<EnemyRegistry>(true);
            SessionTestTools.Call(session, "SetLocalEntityId", 7);
        }

        internal T Component<T>(bool singleton = false) where T : Component
        {
            var go = new GameObject("Entry fixture " + typeof(T).Name);
            go.SetActive(false);
            _objects.Add(go);
            T component = go.AddComponent<T>();
            if (singleton)
            {
                var property = typeof(T).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                Assert.IsNull(property.GetValue(null), "fixture cannot replace an existing owner");
                property.GetSetMethod(true).Invoke(null, new object[] { component });
                _singletons.Add(typeof(T));
            }
            return component;
        }

        internal long Begin(byte map = 0, float x = 2, float y = 3)
        {
            long epoch = Session.Entry.Begin(map, x, y, map != 3);
            SessionTestTools.Call(SessionTestTools.Roster(Session), "BeginTransition", SceneRouter.MapIdToSceneName(map));
            return epoch;
        }

        internal void BindViews()
        {
            Session.Entry.BindScene(Session.Entry.Epoch, Player.gameObject.scene.handle.GetRawData());
            Session.TryBindEntryViews();
        }

        internal void Ready(int hp = 37, int max = 100)
        {
            BindViews();
            Session.ReceivePlayerHp(7, hp, max);
            Assert.IsTrue(Session.Entry.IsGameplayReady, "fixture must pass the actual spawn/terrain/registry/HP barrier");
        }

        internal static void SetField(object target, string field, object value) =>
            target.GetType().GetField(field, Private).SetValue(target, value);
        internal static T Field<T>(object target, string field) =>
            (T)target.GetType().GetField(field, Private).GetValue(target);

        public void Dispose()
        {
            foreach (Type type in _singletons)
                type.GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { null });
            for (int i = _objects.Count - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(_objects[i]);
        }
    }

    public sealed class MapEntryBindingTests
    {
        UnityClientSession _session;
        ManualConnectionQueue _queue;
        EntryBindingFixture _views;
        int _latency;
        readonly List<Action> _delayed = new();

        [SetUp]
        public void Setup()
        {
            Assert.IsNull(UnityClientSession.Instance);
            _latency = UnityClientSession.SimulatedLatencyMs;
            UnityClientSession.SimulatedLatencyMs = 100;
            _queue = new ManualConnectionQueue();
            _session = new UnityClientSession(() => true, _queue.Post, (action, _) => _delayed.Add(action));
            _session.Publish();
            SessionTestTools.Handshake(_session, _queue);
            _views = new EntryBindingFixture(_session);
        }

        [TearDown]
        public void Teardown()
        {
            _session?.Cleanup();
            _views?.Dispose();
            _delayed.Clear();
            UnityClientSession.SimulatedLatencyMs = _latency;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InitialHpBeforeOrAfterViews_SpawnAndHpBarrierPrecedesWorldApply(bool hpFirst)
        {
            _views.Begin(x: 12, y: 4);
            var applied = new List<Vector3>();
            _session.EnqueueWorldApply(() => applied.Add(_views.Player.transform.position));
            _queue.Drain();
            if (hpFirst) _session.ReceivePlayerHp(7, 26, 100);
            else _views.BindViews();
            Assert.IsEmpty(applied);
            Assert.IsFalse(_session.CanSendGameplay);
            if (hpFirst) _views.BindViews();
            else _session.ReceivePlayerHp(7, 26, 100);
            CollectionAssert.AreEqual(new[] { new Vector3(12, 4, 0) }, applied);
            Assert.AreEqual(26, _session.Entry.CurrentHp);
            Assert.IsTrue(_session.CanControlPlayer(_views.Player));
            _session.TryBindEntryViews();
            Assert.AreEqual(1, applied.Count, "world FIFO must drain once");
        }

        [Test]
        public void HpForDifferentEntity_DoesNotReleaseBarrier()
        {
            _views.Begin();
            _views.BindViews();
            _session.ReceivePlayerHp(8, 100, 100);
            Assert.IsFalse(_session.Entry.HasHp);
            Assert.IsFalse(_session.CanSendGameplay);
            _session.ReceivePlayerHp(7, 30, 100);
            Assert.IsTrue(_session.CanSendGameplay);
        }

        [Test]
        public void WrongSceneHandle_DoesNotBindExistingPlayerAndRegistries()
        {
            _views.Begin();
            _session.Entry.BindScene(_session.Entry.Epoch, ulong.MaxValue);
            _session.ReceivePlayerHp(7, 30, 100);
            _session.TryBindEntryViews();
            Assert.IsFalse(_session.CanSendGameplay);
            _views.BindViews();
            Assert.IsTrue(_session.CanSendGameplay);
        }

        [Test]
        public void HudLateBinding_UsesLatestAuthority_AndUnboundHudStopsReceivingUpdates()
        {
            _views.Begin();
            _views.Ready(20);
            _session.ReceivePlayerHp(7, 31, 120);
            var hud = _views.Component<HudController>();
            var slider = _views.Component<Slider>();
            EntryBindingFixture.SetField(hud, "_hpSlider", slider);
            Assert.IsTrue(_session.TryBindHud(hud));
            Assert.AreEqual(31f / 120, slider.value, 0.0001f);
            SessionTestTools.Call(hud, "Start");
            Assert.AreEqual(31f / 120, slider.value, 0.0001f, "Start must not overwrite already applied authority");
            _session.ReceivePlayerHp(7, 60, 120);
            Assert.AreEqual(0.5f, slider.value, 0.0001f);
            _session.UnbindHud(hud);
            _session.ReceivePlayerHp(7, 10, 120);
            Assert.AreEqual(0.5f, slider.value, 0.0001f);
            Assert.IsTrue(_session.TryBindHud(hud));
            Assert.AreEqual(10f / 120, slider.value, 0.0001f);
        }

        [Test]
        public void WorldFifo_PreservesOrder_AndDropsOnlyOverflow()
        {
            _views.Begin();
            var order = new List<int>();
            for (int i = 0; i < 101; i++)
            {
                int captured = i;
                _session.EnqueueWorldApply(() => order.Add(captured));
            }
            LogAssert.Expect(LogType.Warning, "[Unity] RosterBuffer overflow (>100) — world update dropped.");
            _queue.Drain();
            Assert.IsEmpty(order);
            _views.Ready();
            Assert.AreEqual(100, order.Count);
            for (int i = 0; i < 100; i++) Assert.AreEqual(i, order[i]);
            _session.EnqueueWorldApply(() => order.Add(101));
            _queue.Drain();
            Assert.AreEqual(101, order[100], "after Ready, normal updates must resume");
            _session.TryBindEntryViews();
            Assert.AreEqual(101, order.Count);
        }

        [Test]
        public void AllWorldPacketKinds_ShareTheSameBoundedReadinessQueue()
        {
            _views.Begin();
            var packets = new[]
            {
                new S_PlayerJoin().Write(), new S_PlayerLeave().Write(), new S_EntitySpawn().Write(),
                new S_Snapshot().Write(), new S_EntityState().Write(), new S_EntityDeath().Write(),
                new S_HitResult().Write(), new S_EnemyAttack().Write(), new S_PlayerAttack().Write(),
                new S_ProjectileLaunch().Write(), new S_SkillCast().Write()
            };
            object roster = SessionTestTools.Roster(_session);
            var buffer = EntryBindingFixture.Field<ICollection>(roster, "_buffer");
            for (int i = 0; i < packets.Length; i++)
            {
                _session.OnRecvPacket(packets[i]);
                _queue.Drain();
                Assert.AreEqual(i + 1, buffer.Count, "each packet kind must enter the common FIFO before binding");
            }
            Assert.IsFalse(_session.CanSendGameplay);
            _session.Cleanup();
            Assert.AreEqual(0, buffer.Count);
            // Actual spawn/update/death view effects require PlayMode object destruction.
        }

        [Test]
        public void MainQueueEntryBoundary_DiscardsOldBuffer_AndKeepsFollowingPacketForNewEntry()
        {
            _views.Begin();
            var effects = new List<string>();
            _session.EnqueueWorldApply(() => effects.Add("old"));
            _queue.Post(() => _views.Begin(1));
            _session.EnqueueWorldApply(() => effects.Add("new"));
            _queue.Drain();
            _views.Ready();
            CollectionAssert.AreEqual(new[] { "new" }, effects);
            Assert.AreEqual(1, _session.Entry.MapId);
        }

        [Test]
        public void LocalSnapshot_IsAppliedOnlyAfterInitialSpawn_AndServerTickUpdatesWithIt()
        {
            _views.Begin(x: 2, y: 3);
            Vector3 positionBeforeSnapshot = default;
            _session.EnqueueWorldApply(() => positionBeforeSnapshot = _views.Player.transform.position);
            _session.OnRecvPacket(new S_Snapshot { entityId = 7, serverTick = 91, x = 20, y = 7 }.Write());
            _queue.Drain();
            Assert.AreEqual(0, _session.LastReceivedServerTick);
            _views.Ready();
            Assert.AreEqual(new Vector3(2, 3, 0), positionBeforeSnapshot);
            Assert.AreEqual(91, _session.LastReceivedServerTick);
            Assert.AreEqual(new Vector2(20, 7), _views.Predictor.Position);
        }

        [Test]
        public void MovementBeforeReady_DoesNotPredictRecordOrSend_AndClearsAccumulatedInput()
        {
            _views.Begin();
            EntryBindingFixture.SetField(_views.Player, "_sendAccumulator", 10f);
            _views.Player.SetMoveX(1);
            _views.Player.RequestJump();
            EntryBindingFixture.SetField(_views.Player, "_impulsePending", true);
            Vector2 before = _views.Predictor.Position;
            var history = EntryBindingFixture.Field<InputHistory>(_views.Predictor, "_history");
            SessionTestTools.Call(_views.Player, "Update");
            Assert.AreEqual(before, _views.Predictor.Position);
            Assert.AreEqual(0, history.Count);
            Assert.IsEmpty(_delayed);
            Assert.AreEqual(0f, EntryBindingFixture.Field<float>(_views.Player, "_sendAccumulator"));
            Assert.AreEqual(0, EntryBindingFixture.Field<sbyte>(_views.Player, "_currentMoveX"));
            Assert.IsFalse(EntryBindingFixture.Field<bool>(_views.Player, "_jumpEdgeThisTick"));
            Assert.IsFalse(EntryBindingFixture.Field<bool>(_views.Player, "_impulsePending"));
            _views.Ready();
            Assert.AreEqual(0f, EntryBindingFixture.Field<float>(_views.Player, "_sendAccumulator"), "readiness must not restore old elapsed time");
            _views.Player.SetMoveX(1);
            EntryBindingFixture.SetField(_views.Player, "_sendAccumulator", Constants.TickDuration);
            SessionTestTools.Call(_views.Player, "Update");
            Assert.Greater(history.Count, 0, "new input resumes after binding");
            Assert.AreEqual(history.Count, _delayed.Count);
        }

        [Test]
        public void SkillBeforeReady_DoesNotPredictCommitOrScheduleSend_ThenResumesWhenReady()
        {
            var input = _views.Player.gameObject.AddComponent<LocalPlayerInput>();
            SessionTestTools.Call(input, "Awake");
            _views.Begin();
            Assert.IsTrue(_views.Player.CanUseDash);
            SessionTestTools.Call(input, "TrySendSkill", SkillId.Dash, CharacterClass.Knight);
            Assert.IsTrue(_views.Player.CanUseDash, "blocked skill cannot consume its prediction cooldown");
            Assert.IsEmpty(_delayed);
            _views.Ready();
            SessionTestTools.Call(input, "TrySendSkill", SkillId.Dash, CharacterClass.Knight);
            Assert.IsFalse(_views.Player.CanUseDash);
            Assert.AreEqual(1, _delayed.Count, "normal skill must schedule one intent after Ready");
        }

        [Test]
        public void EntrySuspension_ClearsOldTeleportArrival_AndAllowsNewArrival()
        {
            int oldArrival = 0, newArrival = 0;
            _views.Player.NotifyTeleport(new Vector3(8, 9, 0));
            _views.Player.ArmTeleportSnap(() => oldArrival++);
            _views.Player.SuspendForMapEntry();
            Assert.IsNull(_views.Player.ConsumeTeleportDepartPos());
            _views.Player.OnServerSnapshot(0, 0, 0, 0, 1, 0, (byte)AnimState.Idle);
            Assert.AreEqual(0, oldArrival);
            _views.Player.ArmTeleportSnap(() => newArrival++);
            _views.Player.OnServerSnapshot(1, 0, 0, 0, 2, 0, (byte)AnimState.Idle);
            Assert.AreEqual(1, newArrival);
            Assert.AreEqual(0, oldArrival);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DelayedIntent_RechecksMapEpochWithinSameSession(bool changeEntry)
        {
            using var sockets = new TestSocketPair();
            _session.Start(sockets.Client);
            try
            {
                _views.Begin();
                _views.Ready();
                _session.SendIntent(new C_Ping { clientTimestampMs = 3 }.Write());
                Assert.AreEqual(1, _delayed.Count);
                if (changeEntry) { _views.Begin(1); _views.Ready(); }
                _delayed[0]();
                if (changeEntry) Assert.IsFalse(sockets.Peer.Poll(100000, System.Net.Sockets.SelectMode.SelectRead));
                else Assert.AreEqual(PacketID.C_Ping, (PacketID)BitConverter.ToUInt16(sockets.ReadFrame(), 2));
            }
            finally { _session.Disconnect(); _queue.Drain(); }
        }

        [Test]
        public void UnknownMap_IsRejectedBeforeEpochOrWorldBufferChanges()
        {
            _views.Begin();
            long epoch = _session.Entry.Epoch;
            int applied = 0;
            _session.EnqueueWorldApply(() => applied++);
            _queue.Drain();
            LogAssert.Expect(LogType.Error, "[Unity] Unknown destination map 99; entry rejected.");
            Assert.IsFalse(_session.BeginMapEntry(99, 10, 20));
            Assert.AreEqual(epoch, _session.Entry.Epoch);
            Assert.IsFalse(_session.IsClosed);
            _views.Ready();
            Assert.AreEqual(1, applied);
        }
    }
}
