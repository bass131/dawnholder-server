using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Net;
using Dawnholder.Client.Network;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Tests
{
    // Unity consumer of S_CharacterLoadResult (persistence runtime-entry api-contract v2).
    // Expected values are the contract literals: ID 48, 6 bytes little endian, Success0/Busy1/RecoveryRequired2/
    // Unavailable3, success class Knight0/Mage1, failure class 255. They are never derived from the product decoder.
    // The session, handlers, ClassLoadout and entry barrier are the real product types. The test owns only the main
    // queue drain (ManualConnectionQueue) and a loopback socket, so receive-thread versus main-thread effects are visible.
    public sealed class CharacterLoadAuthorityContractTests
    {
        const byte Knight = 0, Mage = 1, Unspecified = 255;
        const byte Success = 0, Busy = 1, RecoveryRequired = 2, Unavailable = 3;

        UnityClientSession _session;
        ManualConnectionQueue _queue;
        TestSocketPair _sockets;
        EntryBindingFixture _views;
        bool _current;
        int _posted;
        int _authorityChanges;
        int _latency;
        CharacterClass? _menuIntent;
        string _notice;
        readonly List<CharacterLoadStatus> _rejections = new();
        readonly List<Action> _delayed = new();
        readonly List<float> _delays = new();
        readonly List<float> _deadlines = new();

        // [size u16 LE = 6][id u16 LE = 48][result][characterClass]
        static byte[] LoadResult(byte result, byte characterClass) =>
            new byte[] { 0x06, 0x00, 0x30, 0x00, result, characterClass };

        [SetUp]
        public void Setup()
        {
            Assert.IsNull(UnityClientSession.Instance);
            _latency = UnityClientSession.SimulatedLatencyMs;
            UnityClientSession.SimulatedLatencyMs = 0;
            _menuIntent = ClassLoadout.SessionSelectedClass;
            MainMenuNotice.TryTake(out _notice);
            // ServerClass mirrors one connection's authority; with no connection open its value is null.
            ClassLoadout.ResetServerClass();
            _views = null;
            _posted = 0;
            _authorityChanges = 0;
            _current = true;
            ClassLoadout.AuthorityChanged += CountAuthorityChange;
            _queue = new ManualConnectionQueue();
            _sockets = new TestSocketPair();
            _session = new UnityClientSession(() => _current,
                // Count after enqueueing so a waiter that sees the count can drain the posted work.
                action => { _queue.Post(action); Interlocked.Increment(ref _posted); },
                (action, delay) => { _delayed.Add(action); _delays.Add(delay); },
                (action, seconds) => _deadlines.Add(seconds));
            _session.CharacterLoadRejected += _rejections.Add;
            _session.Start(_sockets.Client);
            _session.Publish();
            SessionTestTools.Handshake(_session, _queue);
        }

        [TearDown]
        public void Teardown()
        {
            _session?.Disconnect();
            _queue?.Drain();
            _session?.Cleanup();
            _views?.Dispose();
            _sockets?.Dispose();
            ClassLoadout.AuthorityChanged -= CountAuthorityChange;
            ClassLoadout.ResetServerClass();
            ClassLoadout.SessionSelectedClass = _menuIntent;
            MainMenuNotice.TryTake(out _);
            MainMenuNotice.Set(_notice);
            UnityClientSession.SimulatedLatencyMs = _latency;
            _rejections.Clear();
            _delayed.Clear();
            _delays.Clear();
            _deadlines.Clear();
        }

        [Test]
        public void Wire_LoadResultIsSixLittleEndianBytesWithId48_AndEarlierIdsArePreserved()
        {
            Assert.AreEqual(48, (int)PacketID.S_CharacterLoadResult);
            Assert.AreEqual(0, (int)CharacterLoadStatus.Success);
            Assert.AreEqual(1, (int)CharacterLoadStatus.Busy);
            Assert.AreEqual(2, (int)CharacterLoadStatus.RecoveryRequired);
            Assert.AreEqual(3, (int)CharacterLoadStatus.Unavailable);
            Assert.AreEqual(4, Enum.GetValues(typeof(CharacterLoadStatus)).Length);
            Assert.AreEqual(0, (int)CharacterClass.Knight);
            Assert.AreEqual(1, (int)CharacterClass.Mage);

            var shapes = new[] { (Success, Knight), (Success, Mage), (Busy, Unspecified), (RecoveryRequired, Unspecified), (Unavailable, Unspecified) };
            foreach (var (result, characterClass) in shapes)
            {
                ArraySegment<byte> written = new S_CharacterLoadResult { result = result, characterClass = characterClass }.Write();
                CollectionAssert.AreEqual(LoadResult(result, characterClass), written.ToArray(), $"write result={result} class={characterClass}");
                var read = new S_CharacterLoadResult();
                read.Read(new ArraySegment<byte>(LoadResult(result, characterClass)));
                Assert.AreEqual(result, read.result, "read result");
                Assert.AreEqual(characterClass, read.characterClass, "read class");
            }

            // Clients already depend on these numbers; World 43..47 keep their place ahead of the new 48.
            var names = new Dictionary<int, string>
            {
                { 16, "C_CharacterSelect" }, { 41, "C_Login" }, { 42, "S_LoginResult" }, { 43, "C_GuildRequest" },
                { 44, "S_GuildSnapshot" }, { 45, "C_StrongholdUpgrade" }, { 46, "C_WeaponEnhance" },
                { 47, "S_GuildActionResult" }, { 48, "S_CharacterLoadResult" },
            };
            foreach (var pair in names)
                Assert.AreEqual(pair.Value, Enum.GetName(typeof(PacketID), (ushort)pair.Key), $"packet id {pair.Key}");
            int[] ids = Enum.GetValues(typeof(PacketID)).Cast<PacketID>().Select(id => (int)id).ToArray();
            CollectionAssert.AllItemsAreUnique(ids, "no packet id is reused");
            CollectionAssert.IsSubsetOf(Enumerable.Range(1, 48).ToArray(), ids, "ids 1..48 all remain defined");
        }

        [Test]
        public void LoadedProtocolAndTransportAssemblies_AreTheTrackedPluginFiles()
        {
            AssertLoadedFromPlugin(typeof(PacketID).Assembly, "Shared", "Plugins/Shared/Shared.dll");
            AssertLoadedFromPlugin(typeof(PacketSession).Assembly, "Dawnholder.Client.Net", "Plugins/ClientNet/Dawnholder.Client.Net.dll");
        }

        [Test]
        public void SocketFrame_IsAppliedOnlyOnMainQueue_AndStoredClassWinsOverMenuIntent()
        {
            ClassLoadout.SessionSelectedClass = CharacterClass.Knight;
            SelectClass(Knight);
            Assert.AreEqual((int)Knight, ClassLoadout.GetSelectedClassValue(-1), "before authority the menu intent is visible");

            ReceiveFromServer(LoadResult(Success, Mage));
            // Decoded and posted by the receive thread only; nothing is applied before the main queue runs.
            Assert.IsNull(_session.CharacterLoad.Status, "receive thread must not apply authority");
            Assert.IsNull(ClassLoadout.ServerClass, "receive thread must not publish the class");
            Assert.AreEqual(0, _authorityChanges);

            _queue.Drain();
            Assert.AreEqual(CharacterLoadStatus.Success, _session.CharacterLoad.Status);
            Assert.AreEqual(CharacterClass.Mage, _session.CharacterLoad.LoadedClass);
            Assert.IsTrue(_session.CharacterLoad.IsSuccess);
            Assert.AreEqual(CharacterClass.Mage, ClassLoadout.ServerClass);
            Assert.AreEqual((int)Mage, ClassLoadout.GetSelectedClassValue(-1), "class readers see the stored class");
            Assert.AreEqual((int)Knight, ClassLoadout.GetMenuSelectedClassValue(-1), "the menu intent itself is kept");
            Assert.AreEqual(1, _authorityChanges);
            Assert.IsEmpty(_rejections);
        }

        static IEnumerable<TestCaseData> OutOfContractFrames()
        {
            yield return new TestCaseData(new byte[] { 0x05, 0x00, 0x30, 0x00, 0x00 }).SetName("OutOfContractFrame_ShortFiveBytes");
            yield return new TestCaseData(new byte[] { 0x07, 0x00, 0x30, 0x00, 0x00, 0x01, 0x00 }).SetName("OutOfContractFrame_TrailingSeventhByte");
            yield return new TestCaseData(new byte[] { 0x07, 0x00, 0x30, 0x00, 0x00, 0x01 }).SetName("OutOfContractFrame_SizeHeaderDisagreesWithLength");
            yield return new TestCaseData(LoadResult(4, Unspecified)).SetName("OutOfContractFrame_UnknownStatus4");
            yield return new TestCaseData(LoadResult(255, Unspecified)).SetName("OutOfContractFrame_UnknownStatus255");
            yield return new TestCaseData(LoadResult(Success, 2)).SetName("OutOfContractFrame_SuccessWithUndefinedClass2");
            yield return new TestCaseData(LoadResult(Success, Unspecified)).SetName("OutOfContractFrame_SuccessWithFailureClass255");
            yield return new TestCaseData(LoadResult(Busy, Knight)).SetName("OutOfContractFrame_BusyWithKnightClass");
            yield return new TestCaseData(LoadResult(RecoveryRequired, Knight)).SetName("OutOfContractFrame_RecoveryRequiredWithKnightClass");
            yield return new TestCaseData(LoadResult(Unavailable, Mage)).SetName("OutOfContractFrame_UnavailableWithMageClass");
        }

        [TestCaseSource(nameof(OutOfContractFrames))]
        public void OutOfContractFrame_ChangesNoAuthorityClassOrRejection(byte[] frame)
        {
            ClassLoadout.SessionSelectedClass = CharacterClass.Knight;
            SelectClass(Knight);
            // Product dispatch entry with the exact frame; PacketSession framing would split a mismatched size header.
            _session.OnRecvPacket(new ArraySegment<byte>(frame));
            _queue.Drain();
            Assert.IsNull(_session.CharacterLoad.Status);
            Assert.IsFalse(_session.CharacterLoad.IsSuccess);
            Assert.IsNull(ClassLoadout.ServerClass);
            Assert.AreEqual((int)Knight, ClassLoadout.GetSelectedClassValue(-1));
            Assert.AreEqual(0, _authorityChanges);
            Assert.IsEmpty(_rejections);
            Assert.IsFalse(_session.CanSendGameplay);
        }

        [TestCase(Busy)]
        [TestCase(RecoveryRequired)]
        [TestCase(Unavailable)]
        public void FailureResult_IsReported_AndEntryInputAndClassStayClosed(byte status)
        {
            ClassLoadout.SessionSelectedClass = CharacterClass.Mage;
            SelectClass(Mage);
            ReceiveFromServer(LoadResult(status, Unspecified));
            _queue.Drain();
            Assert.AreEqual((CharacterLoadStatus)status, _session.CharacterLoad.Status);
            Assert.IsFalse(_session.CharacterLoad.IsSuccess);
            Assert.IsNull(_session.CharacterLoad.LoadedClass);
            Assert.IsNull(ClassLoadout.ServerClass, "a failure never becomes an authoritative class");
            CollectionAssert.AreEqual(new[] { (CharacterLoadStatus)status }, _rejections);
            Assert.AreEqual(0, _authorityChanges);
            AssertWorldEntryAndInputClosed();
        }

        [Test]
        public void BeforeResult_EnterMapReadyBarrierAndInputStayClosed()
        {
            SelectClass(Knight);
            AssertWorldEntryAndInputClosed();
            _views = new EntryBindingFixture(_session);
            Assert.IsNull(_session.LocalEntityId, "the fixture's entity binding is refused before authority as well");
            _views.Begin();
            _views.BindViews();
            _session.ReceivePlayerHp(7, 30, 100);
            Assert.IsFalse(_session.Entry.IsGameplayReady, "the existing Ready barrier waits for the result");
            Assert.IsFalse(_session.CanSendGameplay);
            Assert.IsNull(ClassLoadout.ServerClass);
        }

        [Test]
        public void AfterSuccess_ExistingReadyBarrierStillApplies_ThenGameplayIntentIsSent()
        {
            SelectClass(Mage);
            ReceiveFromServer(LoadResult(Success, Mage));
            _queue.Drain();
            _views = new EntryBindingFixture(_session);
            Assert.AreEqual(7, _session.LocalEntityId);
            _views.Begin();
            Assert.IsFalse(_session.CanSendGameplay, "authority alone does not skip scene/player/registry/HP readiness");
            _views.Ready();
            ReadyInventoryQuery.Consume(_sockets, _delayed, _delays, 0, _deadlines);
            Assert.IsTrue(_session.CanSendGameplay);
            Assert.IsTrue(_session.CanControlPlayer(_views.Player));
            _session.SendIntent(new C_Ping { clientTimestampMs = 9 }.Write());
            Assert.AreEqual(PacketID.C_Ping, (PacketID)BitConverter.ToUInt16(_sockets.ReadFrame(), 2));
        }

        [Test]
        public void DuplicateSuccess_IsIdempotent_AndConflictingResultsCannotReplaceAuthority()
        {
            ClassLoadout.SessionSelectedClass = CharacterClass.Knight;
            SelectClass(Knight);
            ReceiveFromServer(LoadResult(Success, Mage));
            _queue.Drain();
            var later = new[]
            {
                LoadResult(Success, Mage), LoadResult(Success, Knight), LoadResult(Busy, Unspecified), LoadResult(Unavailable, Unspecified),
            };
            foreach (byte[] frame in later)
            {
                ReceiveFromServer(frame);
                _queue.Drain();
                Assert.AreEqual(CharacterLoadStatus.Success, _session.CharacterLoad.Status, $"after frame result={frame[4]}");
                Assert.AreEqual(CharacterClass.Mage, _session.CharacterLoad.LoadedClass, $"after frame class={frame[5]}");
                Assert.AreEqual(CharacterClass.Mage, ClassLoadout.ServerClass);
            }
            Assert.AreEqual(1, _authorityChanges, "consumers are notified once for one connection's authority");
            Assert.IsEmpty(_rejections);
        }

        [Test]
        public void ResultQueuedBeforeClose_IsNotAppliedAfterTheConnectionEnds()
        {
            SelectClass(Knight);
            ReceiveFromServer(LoadResult(Success, Mage));
            _session.Disconnect();
            _queue.Drain();
            Assert.IsNull(_session.CharacterLoad.Status);
            Assert.IsFalse(_session.CharacterLoad.IsSuccess);
            Assert.IsNull(ClassLoadout.ServerClass);
            Assert.AreEqual(0, _authorityChanges);
            Assert.IsNull(UnityClientSession.Instance, "the ended session is unpublished");
        }

        [Test]
        public void ResultForSupersededConnection_IsNotApplied()
        {
            SelectClass(Knight);
            ReceiveFromServer(LoadResult(Success, Mage));
            _current = false; // the owner switched to a newer connection before the main queue ran
            _queue.Drain();
            Assert.IsNull(_session.CharacterLoad.Status);
            Assert.IsNull(ClassLoadout.ServerClass);
            Assert.AreEqual(0, _authorityChanges);
        }

        [Test]
        public void ConnectionEnd_ClearsStoredClassOverride_AndMenuIntentRemains()
        {
            ClassLoadout.SessionSelectedClass = CharacterClass.Knight;
            SelectClass(Knight);
            ReceiveFromServer(LoadResult(Success, Mage));
            _queue.Drain();
            Assert.AreEqual((int)Mage, ClassLoadout.GetSelectedClassValue(-1));

            // ClientConnectionLifetime ends a connection with session Disconnect/Cleanup, then the owner's mirror reset.
            _session.Disconnect();
            _queue.Drain();
            Assert.IsFalse(_session.CharacterLoad.IsSuccess, "the ended connection keeps no authority");
            Assert.IsFalse(_session.CanSendGameplay);
            NetworkService.ResetGlobalSessionMirrors();
            Assert.IsNull(ClassLoadout.ServerClass);
            Assert.AreEqual((int)Knight, ClassLoadout.GetSelectedClassValue(-1), "the next connection starts from the menu intent");
        }

        void CountAuthorityChange() => _authorityChanges++;

        void SelectClass(byte characterClass)
        {
            _session.SendCharacterSelect(characterClass);
            CollectionAssert.AreEqual(new byte[] { 0x05, 0x00, 0x10, 0x00, characterClass }, _sockets.ReadFrame(),
                "C_CharacterSelect(16) carries the menu intent");
        }

        // Real transport: peer bytes -> ClientSession receive -> PacketSession framing -> OnRecvPacket -> handler -> posted apply.
        void ReceiveFromServer(byte[] frame)
        {
            int before = Volatile.Read(ref _posted);
            _sockets.Peer.Send(frame);
            Assert.IsTrue(SpinWait.SpinUntil(() => Volatile.Read(ref _posted) > before, 3000), "receive thread did not post the result");
        }

        void AssertWorldEntryAndInputClosed()
        {
            long epoch = _session.Entry.Epoch;
            _session.OnRecvPacket(new S_EnterMap { entityId = 7, spawnX = 2, spawnY = 3 }.Write());
            _queue.Drain();
            Assert.IsNull(_session.LocalEntityId, "EnterMap cannot bind the local entity without authority");
            Assert.AreEqual(epoch, _session.Entry.Epoch, "EnterMap cannot begin a map entry without authority");
            Assert.IsFalse(_session.CanSendGameplay);
            _session.SendIntent(new C_Ping { clientTimestampMs = 5 }.Write());
            Assert.IsFalse(_sockets.Peer.Poll(100000, SelectMode.SelectRead), "no gameplay intent reaches the server");
        }

        static void AssertLoadedFromPlugin(Assembly assembly, string name, string pluginPath)
        {
            string plugin = Path.GetFullPath(Path.Combine(Application.dataPath, pluginPath));
            string loaded = assembly.Location;
            string loadedHash = !string.IsNullOrEmpty(loaded) && File.Exists(loaded) ? Sha256(loaded) : "unavailable";
            string pluginHash = Sha256(plugin);
            // Recorded for the verdict's comparison with the promoted DLL hashes; the assertion below is identity only.
            TestContext.Out.WriteLine($"[CharacterLoadAuthorityContract] assembly={assembly.FullName} " +
                $"mvid={assembly.ManifestModule.ModuleVersionId} location={loaded} loadedSha256={loadedHash} " +
                $"plugin={plugin} pluginSha256={pluginHash}");
            Assert.AreEqual(name, assembly.GetName().Name);
            Assert.AreEqual(pluginHash, loadedHash, $"{name} must be loaded from the tracked plugin bytes");
        }

        static string Sha256(string path)
        {
            using var sha = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }
    }
}
