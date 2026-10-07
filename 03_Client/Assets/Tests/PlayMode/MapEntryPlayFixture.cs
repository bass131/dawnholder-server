using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Gameplay;
using Dawnholder.Client.Network;
using Dawnholder.Client.Prediction;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace Dawnholder.Client.Tests.PlayMode
{
    // A real framed TCP endpoint. Scripted mode only supplies test authority; it is
    // deliberately not a replacement implementation of GameServer simulation.
    internal sealed class EntryWirePeer : IDisposable
    {
        readonly object _sendLock = new();
        readonly ConcurrentQueue<byte[]> _received = new();
        readonly bool _scriptedServer;
        TcpListener _listener;
        TcpClient _client;
        NetworkStream _stream;
        volatile bool _disposed;
        internal readonly ConcurrentQueue<Exception> Errors = new();
        internal Task Completion { get; private set; }
        internal int Port { get; private set; }
        internal bool Selected => Count(PacketID.C_CharacterSelect) > 0;

        EntryWirePeer(bool scriptedServer) { _scriptedServer = scriptedServer; }

        internal static EntryWirePeer Listen()
        {
            var peer = new EntryWirePeer(true);
            peer._listener = new TcpListener(IPAddress.Loopback, 0);
            peer._listener.Start();
            peer.Port = ((IPEndPoint)peer._listener.LocalEndpoint).Port;
            peer.Completion = peer.Run(true);
            return peer;
        }

        internal static EntryWirePeer ConnectProduction(int port)
        {
            var peer = new EntryWirePeer(false) { Port = port };
            peer.Completion = peer.Run(false);
            return peer;
        }

        async Task Run(bool accept)
        {
            try
            {
                if (accept)
                {
                    _client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    _listener.Stop();
                }
                else
                {
                    _client = new TcpClient();
                    await _client.ConnectAsync(IPAddress.Loopback, Port).ConfigureAwait(false);
                }
                _client.NoDelay = true;
                _stream = _client.GetStream();
                if (!accept) Send(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
                while (!_disposed)
                {
                    byte[] header = new byte[4];
                    if (!await ReadExact(header, 0, 4).ConfigureAwait(false)) break;
                    int length = BitConverter.ToUInt16(header, 0);
                    if (length < 4 || length > 65535) throw new InvalidDataException("Invalid frame length");
                    byte[] frame = new byte[length];
                    Buffer.BlockCopy(header, 0, frame, 0, 4);
                    if (!await ReadExact(frame, 4, length - 4).ConfigureAwait(false)) break;
                    _received.Enqueue(frame);
                    var id = (PacketID)BitConverter.ToUInt16(frame, 2);
                    if (_scriptedServer && id == PacketID.C_Handshake)
                        Send(new S_HandshakeResult { ok = true, serverVersion = ProtocolVersion.Current, reason = "" }.Write());
                    else if (!_scriptedServer && id == PacketID.S_HandshakeResult)
                        Send(new C_CharacterSelect { characterClass = 0 }.Write());
                    else if (_scriptedServer && id == PacketID.C_Ping)
                    {
                        var ping = new C_Ping();
                        ping.Read(new ArraySegment<byte>(frame));
                        Send(new S_Pong { clientTimestampMs = ping.clientTimestampMs,
                            serverTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }.Write());
                    }
                }
            }
            catch (Exception error) { if (!_disposed) Errors.Enqueue(error); }
        }

        async Task<bool> ReadExact(byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                int received = await _stream.ReadAsync(buffer, offset, count).ConfigureAwait(false);
                if (received == 0) return false;
                offset += received;
                count -= received;
            }
            return true;
        }

        internal void Send(ArraySegment<byte> packet)
        {
            lock (_sendLock)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(EntryWirePeer));
                if (_stream == null) throw new InvalidOperationException("Peer is not connected");
                _stream.Write(packet.Array, packet.Offset, packet.Count);
            }
        }

        internal int Count(PacketID id) => _received.Count(frame => BitConverter.ToUInt16(frame, 2) == (ushort)id);
        internal T Last<T>(PacketID id) where T : IPacket, new()
        {
            byte[] frame = _received.LastOrDefault(item => BitConverter.ToUInt16(item, 2) == (ushort)id);
            if (frame == null) return default;
            var packet = new T();
            packet.Read(new ArraySegment<byte>(frame));
            return packet;
        }

        public void Dispose()
        {
            _disposed = true;
            _client?.Close();
            _listener?.Stop();
        }
    }

    internal sealed class MapEntryPlayFixture
    {
        internal EntryWirePeer Peer;
        internal EntryWirePeer Companion;
        internal Keyboard Keyboard;
        internal Mouse Mouse;
        NetworkService _service;
        int _oldPort;
        float _oldFade, _oldScale;
        CharacterClass? _oldClass;
        int _oldLatency;
        bool _globalsCaptured;
        bool _ownsConnection;
        InputSettings _originalInputSettings;
        InputSettings _originalInputSettingsCopy;
        InputSettings _runtimeInputSettings;
        bool _oldRunInBackground;
        PlayerInput _pairedPlayer;
        internal UnityClientSession Session => UnityClientSession.Instance;
        internal LocalPlayerMovement Player => LocalPlayerMovement.Instance;
        internal SceneTransition Loader => SceneTransition.Instance;
        internal const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        internal static T Field<T>(object owner, string field) => (T)owner.GetType().GetField(field, Private).GetValue(owner);
        internal static void Set(object owner, string field, object value) => owner.GetType().GetField(field, Private).SetValue(owner, value);

        internal IEnumerator Prepare(bool production)
        {
            Assert.IsTrue(Application.isPlaying, "this lane must execute real Unity PlayMode");
            yield return Wait(() => NetworkService.Instance != null && SceneTransition.Instance != null, "persistent services");
            _service = NetworkService.Instance;
            Assert.IsFalse(_service.IsConnected, "fixture must not adopt another session");
            _oldPort = Field<int>(_service, "_serverPort");
            _oldFade = Field<float>(Loader, "_fadeDuration");
            _oldScale = Time.timeScale;
            _oldClass = ClassLoadout.SessionSelectedClass;
            _oldLatency = UnityClientSession.SimulatedLatencyMs;
            _originalInputSettings = InputSystem.settings;
            _oldRunInBackground = Application.runInBackground;
            _globalsCaptured = true;
            // Input System 1.20.0 destroys replaced settings flagged HideAndDontSave (its default when the project has
            // no InputSettings asset); 1.19.0 keeps them. Restore from an untouched copy if the original does not survive.
            _originalInputSettingsCopy = UnityEngine.Object.Instantiate(_originalInputSettings);
            _originalInputSettingsCopy.hideFlags = HideFlags.HideAndDontSave;
            _runtimeInputSettings = UnityEngine.Object.Instantiate(_originalInputSettings);
            _runtimeInputSettings.hideFlags = HideFlags.HideAndDontSave;
            _runtimeInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _runtimeInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = _runtimeInputSettings;
            Application.runInBackground = true;
            Time.timeScale = 1;
            UnityClientSession.SimulatedLatencyMs = 0;
            ClassLoadout.SessionSelectedClass = CharacterClass.Knight;
            Set(Loader, "_fadeDuration", 0.03f);
            Keyboard = InputSystem.AddDevice<Keyboard>("EntryTestKeyboard");
            Mouse = InputSystem.AddDevice<Mouse>("EntryTestMouse");
            Keyboard.MakeCurrent();
            if (production) Set(_service, "_serverPort", 7777);
            else
            {
                Peer = EntryWirePeer.Listen();
                Set(_service, "_serverPort", Peer.Port);
            }
            _ownsConnection = true;
            _service.Connect("127.0.0.1", 0);
            yield return Wait(() => Session != null && Session.HandshakeOk && (Peer == null || Peer.Selected), "handshake and character selection");
        }

        internal IEnumerator EnterScriptedTown(bool sceneFirst = false)
        {
            yield return Prepare(false);
            if (sceneFirst)
            {
                var request = Loader.RequestScene("Town");
                yield return Wait(() => request.IsFinished && Player != null, "Town before EnterMap");
            }
            Peer.Send(new S_EnterMap { entityId = 700, spawnX = 2, spawnY = 0 }.Write());
            yield return Wait(() => Session.Entry.State == MapEntryState.Entering && Session.Entry.SceneReady && Player != null,
                "Town binding before HP");
            PairInput();
        }

        internal void Hp(int hp = 37, int max = 100) => Peer.Send(new S_PlayerHp { entityId = 700, currentHp = hp, maxHp = max }.Write());

        internal IEnumerator WaitMap(byte map)
        {
            yield return Wait(() => Session != null && Session.Entry.MapId == map && Session.Entry.State == MapEntryState.Ready &&
                !Loader.IsTransitioning, "ready map " + map);
            string[] expectedScenes = { "Town", "HuntingGround", "BossRoom", "Ending" };
            Assert.AreEqual(expectedScenes[map], SceneManager.GetActiveScene().name);
            if (map != 3)
            {
                Assert.IsNotNull(Player);
                Assert.IsTrue(Session.CanControlPlayer(Player));
                Assert.AreEqual(Session.Entry.SceneHandle, Player.gameObject.scene.handle.GetRawData());
                Assert.AreEqual(Session.Entry.SceneHandle, RemoteEntityRegistry.Instance.gameObject.scene.handle.GetRawData());
                Assert.AreEqual(Session.Entry.SceneHandle, EnemyRegistry.Instance.gameObject.scene.handle.GetRawData());
                PairInput();
            }
        }

        internal void PairInput()
        {
            if (Player == null) return;
            var input = Player.GetComponent<PlayerInput>();
            Assert.IsNotNull(input);
            if (_pairedPlayer == input) return;
            input.SwitchCurrentControlScheme("Keyboard&Mouse", Keyboard, Mouse);
            _pairedPlayer = input;
        }

        internal void Keys(params Key[] keys)
        {
            PairInput();
            InputSystem.QueueStateEvent(Keyboard, new KeyboardState(keys));
        }

        internal IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        internal static IEnumerator Wait(Func<bool> condition, string label, float seconds = 20)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition())
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, "Timed out: " + label);
                yield return null;
            }
        }

        internal IEnumerator AssertHud()
        {
            yield return Wait(() => HudController.Instance != null, "additive HUD");
            yield return null;
            var label = Field<TMP_Text>(HudController.Instance, "_hpText");
            Assert.IsNotNull(label);
            Assert.AreEqual($"HP {Session.Entry.CurrentHp} / {Session.Entry.MaxHp}", label.text);
        }

        internal IEnumerator WalkIntoPortal(int portalId, byte destination)
        {
            var portal = UnityEngine.Object.FindObjectsByType<PortalTrigger>()
                .Single(item => Field<int>(item, "_portalId") == portalId);
            float deadline = Time.realtimeSinceStartup + 20;
            while (Mathf.Abs(Player.transform.position.x - portal.transform.position.x) > 0.3f)
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, "virtual keyboard could not reach portal");
                Keys(Player.transform.position.x < portal.transform.position.x ? Key.D : Key.A);
                yield return null;
            }
            Keys();
            yield return Frames(4);
            Assert.IsTrue(Field<bool>(portal, "_isOverlapping"), "real physics trigger must detect portal overlap");
            Keys(Key.UpArrow);
            yield return Frames(2);
            Keys();
            yield return WaitMap(destination);
        }

        internal IEnumerator Cleanup()
        {
            try
            {
                Peer?.Dispose();
                Companion?.Dispose();
                if (_ownsConnection) _service?.Disconnect();
                if (Peer != null) yield return Wait(() => Peer.Completion.IsCompleted, "scripted socket shutdown", 3);
                if (Companion != null) yield return Wait(() => Companion.Completion.IsCompleted, "companion socket shutdown", 3);
                if (_ownsConnection && Loader != null) yield return Wait(() => !Loader.IsTransitioning, "owned scene operation shutdown");
                if (!_globalsCaptured) yield break;
                // Unload runtime copies only. Never save or rewrite scene/prefab assets.
                Scene empty = SceneManager.CreateScene("EntryTestCleanup_" + Guid.NewGuid().ToString("N"));
                SceneManager.SetActiveScene(empty);
                var scenes = new List<Scene>();
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (scene != empty && (scene.name == "Town" || scene.name == "HuntingGround" ||
                        scene.name == "BossRoom" || scene.name == "Ending" || scene.name == "UI" ||
                        scene.name.StartsWith("EntryTestCleanup_"))) scenes.Add(scene);
                }
                foreach (Scene scene in scenes)
                {
                    AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
                    if (unload != null) yield return Wait(() => unload.isDone, "scene unload " + scene.name);
                }
                Assert.IsNull(UnityClientSession.Instance);
                if (Peer != null) Assert.IsEmpty(Peer.Errors, "scripted transport failures");
                if (Companion != null) Assert.IsEmpty(Companion.Errors, "production companion transport failures");
            }
            finally { RestoreOwnedRuntimeState(); }
        }

        void RestoreOwnedRuntimeState()
        {
            var errors = new List<Exception>();
            void Restore(Action action)
            {
                try { action(); }
                catch (Exception error) { errors.Add(error); }
            }
            Restore(() => Peer?.Dispose());
            Restore(() => Companion?.Dispose());
            if (_ownsConnection) Restore(() => _service?.Disconnect());
            Restore(() => { if (Keyboard != null && Keyboard.added) InputSystem.RemoveDevice(Keyboard); });
            Restore(() => { if (Mouse != null && Mouse.added) InputSystem.RemoveDevice(Mouse); });
            if (_globalsCaptured)
            {
                Restore(() => { if (_service != null) Set(_service, "_serverPort", _oldPort); });
                Restore(() => { if (Loader != null) Set(Loader, "_fadeDuration", _oldFade); });
                Restore(() => Time.timeScale = _oldScale);
                Restore(() => ClassLoadout.SessionSelectedClass = _oldClass);
                Restore(() => UnityClientSession.SimulatedLatencyMs = _oldLatency);
                // Unity null check, not ??: a destroyed original reads as null, and its copy takes its place.
                Restore(() =>
                {
                    InputSettings restored = _originalInputSettings != null ? _originalInputSettings : _originalInputSettingsCopy;
                    if (restored != null) InputSystem.settings = restored;
                });
                Restore(() => Application.runInBackground = _oldRunInBackground);
            }
            // Destroy only the copies the Input System no longer uses. A copy it still uses (normally the restored one)
            // is its settings now; destroying that would break the next Prepare.
            foreach (InputSettings copy in new[] { _runtimeInputSettings, _originalInputSettingsCopy })
                Restore(() => { if (copy != null && copy != InputSystem.settings) UnityEngine.Object.Destroy(copy); });
            if (errors.Count != 0) throw new AggregateException("Test fixture runtime restoration failed", errors);
        }
    }
}
