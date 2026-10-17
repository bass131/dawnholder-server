using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Network;
using Dawnholder.Client.Prediction;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Tests
{
    // Lifecycle contract of the shared EntryBindingFixture that the EditMode gameplay tests build on.
    // A fixture releases only what it still owns: a singleton slot another owner took after construction, or another
    // connection's loaded class, stays in place. After the normal end (session Cleanup, then fixture Dispose) the next
    // connection starts without authority and its own server result wins.
    // Expected bytes are persistence runtime-entry api-contract v2 literals, never computed from product code:
    // C_CharacterSelect is [size 5][id 16][class], S_CharacterLoadResult is [size 6][id 48][Success 0][class],
    // with Knight 0 and Mage 1. Reflection is used only to let a foreign owner take a singleton slot.
    public sealed class EntryFixtureLifecycleContractTests
    {
        const byte Mage = 1;

        readonly List<UnityClientSession> _sessions = new();
        readonly List<EntryBindingFixture> _fixtures = new();
        readonly List<TestSocketPair> _sockets = new();
        readonly List<LocalPlayerMovement> _foreignPlayers = new();
        CharacterClass? _serverClass;
        string _notice;

        [SetUp]
        public void Setup()
        {
            // Captured first so Teardown restores this case's outside values even if an isolation check fails.
            _serverClass = ClassLoadout.ServerClass;
            MainMenuNotice.TryTake(out _notice);
            ClassLoadout.ResetServerClass();
            Assert.IsNull(UnityClientSession.Instance, "another test left a published session");
            Assert.IsNull(LocalPlayerMovement.Instance, "another test left a local player");
            Assert.IsNull(RemoteEntityRegistry.Instance, "another test left a remote registry");
            Assert.IsNull(EnemyRegistry.Instance, "another test left an enemy registry");
        }

        [TearDown]
        public void Teardown()
        {
            try
            {
                foreach (UnityClientSession session in _sessions) session.Cleanup();
                for (int i = _fixtures.Count - 1; i >= 0; i--) _fixtures[i].Dispose();
                foreach (TestSocketPair sockets in _sockets) sockets.Dispose();
            }
            finally
            {
                foreach (LocalPlayerMovement foreign in _foreignPlayers)
                {
                    if (ReferenceEquals(LocalPlayerMovement.Instance, foreign)) SetInstance<LocalPlayerMovement>(null);
                    if (foreign != null) UnityEngine.Object.DestroyImmediate(foreign.gameObject);
                }
                _sessions.Clear();
                _fixtures.Clear();
                _sockets.Clear();
                _foreignPlayers.Clear();
                ClassLoadout.ResetServerClass();
                if (_serverClass.HasValue) ClassLoadout.ApplyServerClass(_serverClass.Value);
                MainMenuNotice.TryTake(out _);
                MainMenuNotice.Set(_notice);
            }
        }

        [Test]
        public void Dispose_ReleasesOnlySingletonsItStillOwns_AndDestroysOnlyItsOwnObjects()
        {
            Connection connection = Open();
            EntryBindingFixture fixture = Track(new EntryBindingFixture(connection.Session));
            GameObject ownPlayer = fixture.Player.gameObject;
            GameObject ownRemote = fixture.Remote.gameObject;
            GameObject ownEnemies = fixture.Enemies.gameObject;
            // After construction another owner, such as the next connection's spawned player, takes the slot.
            LocalPlayerMovement foreignPlayer = ForeignPlayer();
            SetInstance(foreignPlayer);

            fixture.Dispose();

            Assert.AreSame(foreignPlayer, LocalPlayerMovement.Instance, "a slot another owner took is not the fixture's to clear");
            Assert.IsFalse(foreignPlayer == null, "the other owner's player survives the fixture");
            Assert.IsNull(RemoteEntityRegistry.Instance, "registries the fixture still owns are released");
            Assert.IsNull(EnemyRegistry.Instance, "registries the fixture still owns are released");
            Assert.IsTrue(ownPlayer == null, "the fixture's own player object is destroyed");
            Assert.IsTrue(ownRemote == null, "the fixture's own remote registry object is destroyed");
            Assert.IsTrue(ownEnemies == null, "the fixture's own enemy registry object is destroyed");
        }

        [Test]
        public void Admit_RefusesBeforeSending_WhenAnotherConnectionHoldsAuthority_AndDisposeKeepsIt()
        {
            Connection connection = Open();
            EntryBindingFixture fixture = Track(new EntryBindingFixture(connection.Session));
            // Another connection's loaded class (contract Mage 1) is already the process-wide authority.
            ClassLoadout.ApplyServerClass(CharacterClass.Mage);

            Assert.Throws<AssertionException>(() => fixture.Admit(connection.Queue, connection.Sockets));

            bool selectionSent = connection.Sockets.Peer.Poll(100000, SelectMode.SelectRead);
            Assert.IsFalse(selectionSent, "no C_CharacterSelect left the client");
            Assert.IsNull(connection.Session.CharacterLoad.Status, "the refused fixture never fed a server result");
            Assert.IsNull(connection.Session.LocalEntityId, "no entity binding without this connection's own result");
            Assert.IsFalse(connection.Session.CanSendGameplay);
            Assert.AreEqual(CharacterClass.Mage, ClassLoadout.ServerClass, "the other connection's class is untouched");

            fixture.Dispose();

            Assert.AreEqual(CharacterClass.Mage, ClassLoadout.ServerClass,
                "disposing a fixture that never admitted keeps the other connection's class");
        }

        [Test]
        public void NormalEnd_ThenNextConnection_StartsWithoutAuthority_AndItsOwnResultWins()
        {
            Connection first = Open();
            EntryBindingFixture firstViews = Track(new EntryBindingFixture(first.Session));
            firstViews.Admit(first.Queue, first.Sockets);
            Assert.AreEqual(CharacterClass.Knight, ClassLoadout.ServerClass, "precondition: the first connection loaded Knight");

            // The normal end used by the gameplay fixtures: the session cleans itself, then the fixture releases its views.
            first.Session.Cleanup();
            firstViews.Dispose();
            Assert.IsNull(UnityClientSession.Instance, "the ended session is unpublished");
            Assert.IsNull(ClassLoadout.ServerClass, "the ended connection leaves no class override behind");

            Connection next = Open();
            EntryBindingFixture nextViews = Track(new EntryBindingFixture(next.Session));
            Assert.IsNull(next.Session.CharacterLoad.Status, "the next connection has no result yet");
            Assert.IsNull(next.Session.LocalEntityId, "constructing views alone binds no entity");
            Assert.IsFalse(next.Session.CanSendGameplay);
            Assert.AreSame(nextViews.Player, LocalPlayerMovement.Instance, "the next fixture could take the released slots");

            next.Session.SendCharacterSelect(Mage);
            CollectionAssert.AreEqual(new byte[] { 5, 0, 16, 0, Mage }, next.Sockets.ReadFrame(), "C_CharacterSelect carries Mage");
            next.Session.OnRecvPacket(new ArraySegment<byte>(new byte[] { 6, 0, 48, 0, 0, Mage }));
            Assert.IsNull(next.Session.CharacterLoad.Status, "the result is applied only on the main queue");
            next.Queue.Drain();

            Assert.AreEqual(CharacterLoadStatus.Success, next.Session.CharacterLoad.Status);
            Assert.AreEqual(CharacterClass.Mage, next.Session.CharacterLoad.LoadedClass);
            Assert.AreEqual(CharacterClass.Mage, ClassLoadout.ServerClass, "the previous connection's Knight does not outlive it");
        }

        Connection Open()
        {
            var queue = new ManualConnectionQueue();
            var sockets = new TestSocketPair();
            _sockets.Add(sockets);
            var session = new UnityClientSession(() => true, queue.Post, (action, delay) => { }, (action, seconds) => { });
            _sessions.Add(session);
            session.Start(sockets.Client);
            session.Publish();
            SessionTestTools.Handshake(session, queue);
            return new Connection(queue, sockets, session);
        }

        EntryBindingFixture Track(EntryBindingFixture fixture)
        {
            _fixtures.Add(fixture);
            return fixture;
        }

        LocalPlayerMovement ForeignPlayer()
        {
            var owner = new GameObject("Foreign owner LocalPlayerMovement");
            owner.SetActive(false);
            LocalPlayerMovement player = owner.AddComponent<LocalPlayerMovement>();
            _foreignPlayers.Add(player);
            return player;
        }

        static void SetInstance<T>(T owner) where T : Component =>
            typeof(T).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).GetSetMethod(true)
                .Invoke(null, new object[] { owner });

        sealed class Connection
        {
            internal readonly ManualConnectionQueue Queue;
            internal readonly TestSocketPair Sockets;
            internal readonly UnityClientSession Session;

            internal Connection(ManualConnectionQueue queue, TestSocketPair sockets, UnityClientSession session)
            {
                Queue = queue;
                Sockets = sockets;
                Session = session;
            }
        }
    }
}
