using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Network;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Independent verification D3 (goal 2026-10-11-multi-account-login, 설계 클라이언트 NetworkService): a login
    // rejection becomes the menu notice and a return to MainMenu, and a missing or rule-breaking account name returns
    // to MainMenu without connecting. The real persistent services run in PlayMode against a loopback peer; the
    // MainMenu that loads shows the notice through its own Start. The expected sentences are the goal's text. The
    // peer port goes in through Unity serialization (JsonUtility), the data path the Inspector writes, and the
    // service's serialized state is restored afterwards.
    public sealed class LoginRejectionMenuTests
    {
        const string AccountName = "alice_01";
        const string InvalidNameNotice = "계정 이름은 영문 소문자, 숫자, 밑줄 3~16자로 입력해주세요";
        const string AlreadyOnlineNotice = "이미 접속 중인 계정입니다";

        // D3: the server answers C_Login with InvalidName and closes; the menu returns showing the name rule.
        [UnityTest]
        public IEnumerator D3_InvalidNameRejection_ReturnsToTheMenuShowingTheNameRule()
            => RejectedLogin(LoginResult.InvalidName, InvalidNameNotice);

        // D3: the server answers C_Login with AlreadyOnline and closes; the menu returns showing "already online".
        [UnityTest]
        public IEnumerator D3_AlreadyOnlineRejection_ReturnsToTheMenuShowingAlreadyOnline()
            => RejectedLogin(LoginResult.AlreadyOnline, AlreadyOnlineNotice);

        // D3: no account name chosen in this process; Connect opens no connection and the menu shows the name rule.
        [UnityTest]
        public IEnumerator D3_MissingAccountName_ReturnsToTheMenuWithoutConnecting() => RefusedName(null);

        // D3: a name outside the rule (hyphen); Connect opens no connection and the menu shows the name rule.
        [UnityTest]
        public IEnumerator D3_RuleBreakingAccountName_ReturnsToTheMenuWithoutConnecting() => RefusedName("bad-name");

        static IEnumerator RejectedLogin(LoginResult result, string notice)
        {
            using var peer = new MenuLoginPeer(result);
            using var services = new PersistentServicesUnderTest();
            yield return services.Prepare(peer.Port, AccountName);

            services.Network.Connect("127.0.0.1", (int)CharacterClass.Knight);
            yield return MapEntryPlayFixture.Wait(() => MenuShows(notice), "MainMenu showing the rejection notice");

            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
            Assert.IsFalse(services.Network.IsConnected, "the rejected connection stayed up");
            CollectionAssert.AreEqual(new[] { AccountName }, peer.Logins);
            CollectionAssert.DoesNotContain(peer.Received, PacketID.C_CharacterSelect);
            Assert.IsFalse(MainMenuNotice.TryTake(out _), "the menu showed the notice but left it pending");
            yield return LeaveMainMenu();
        }

        static IEnumerator RefusedName(string accountName)
        {
            using var peer = new MenuLoginPeer(LoginResult.Accepted);
            using var services = new PersistentServicesUnderTest();
            yield return services.Prepare(peer.Port, accountName);

            services.Network.Connect("127.0.0.1", (int)CharacterClass.Knight);
            yield return MapEntryPlayFixture.Wait(() => MenuShows(InvalidNameNotice), "MainMenu showing the name rule");

            Assert.AreEqual(0, peer.Accepted, "a connection was opened without a valid account name");
            Assert.IsFalse(services.Network.IsConnected);
            yield return LeaveMainMenu();
        }

        // The menu error text is the only TMP_Text in MainMenu that shows a notice sentence.
        static bool MenuShows(string notice)
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.name != "MainMenu") return false;
            foreach (TMP_Text text in UnityEngine.Object.FindObjectsByType<TMP_Text>())
            {
                if (text.gameObject.scene == active && text.text == notice) return true;
            }
            return false;
        }

        // Leave an empty active scene, as the runner starts, for the PlayMode classes that follow.
        static IEnumerator LeaveMainMenu()
        {
            Scene empty = SceneManager.CreateScene("LoginRejectionMenuTests-empty");
            SceneManager.SetActiveScene(empty);
            Scene menu = SceneManager.GetSceneByName("MainMenu");
            if (menu.IsValid() && menu.isLoaded) yield return SceneManager.UnloadSceneAsync(menu);
        }

        // Captures and restores what the test changes on the persistent services and process-local globals.
        sealed class PersistentServicesUnderTest : IDisposable
        {
            string _savedNetwork;
            string _oldAccountName;
            float _oldListenerVolume;
            bool _prepared;

            internal NetworkService Network { get; private set; }

            internal IEnumerator Prepare(int port, string accountName)
            {
                Assert.IsTrue(Application.isPlaying, "this check must execute real Unity PlayMode");
                yield return MapEntryPlayFixture.Wait(
                    () => NetworkService.Instance != null && SceneTransition.Instance != null, "persistent services");
                Network = NetworkService.Instance;
                Assert.IsFalse(Network.IsConnected, "fixture must not adopt another session");
                _savedNetwork = JsonUtility.ToJson(Network);
                _oldAccountName = AccountLoadout.SessionAccountName;
                _oldListenerVolume = AudioListener.volume;
                _prepared = true;
                // Test play stays silent, as MapEntryPlayFixture does; a runtime value, never a project setting.
                AudioListener.volume = 0f;
                JsonUtility.FromJsonOverwrite("{\"_serverPort\":" + port + "}", Network);
                AccountLoadout.SessionAccountName = accountName;
                MainMenuNotice.TryTake(out _);
            }

            public void Dispose()
            {
                if (!_prepared) return;
                if (Network != null)
                {
                    Network.Disconnect();
                    JsonUtility.FromJsonOverwrite(_savedNetwork, Network);
                }
                AccountLoadout.SessionAccountName = _oldAccountName;
                AudioListener.volume = _oldListenerVolume;
                MainMenuNotice.TryTake(out _);
            }
        }

        // Plays the server for one test: answers C_Handshake with ok and C_Login with the given result; any result
        // but Accepted is followed by a half-close, as GameSession.RejectLogin sends and disconnects. It keeps
        // reading until the client closes and records every packet id and login name it received.
        sealed class MenuLoginPeer : IDisposable
        {
            readonly TcpListener _listener = new(IPAddress.Loopback, 0);
            readonly LoginResult _result;
            readonly Thread _thread;
            readonly object _gate = new();
            readonly List<PacketID> _received = new();
            readonly List<string> _logins = new();
            Socket _socket;
            int _accepted;
            volatile bool _disposed;

            internal MenuLoginPeer(LoginResult result)
            {
                _result = result;
                _listener.Start();
                Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
                _thread = new Thread(Serve) { IsBackground = true, Name = "login menu peer" };
                _thread.Start();
            }

            internal int Port { get; }

            internal int Accepted => Volatile.Read(ref _accepted);

            internal List<PacketID> Received { get { lock (_gate) return new List<PacketID>(_received); } }

            internal List<string> Logins { get { lock (_gate) return new List<string>(_logins); } }

            public void Dispose()
            {
                _disposed = true;
                _listener.Stop();
                lock (_gate) _socket?.Dispose();
                _thread.Join(TimeSpan.FromSeconds(5));
            }

            void Serve()
            {
                try
                {
                    while (!_disposed)
                    {
                        Socket socket = _listener.AcceptSocket();
                        Interlocked.Increment(ref _accepted);
                        lock (_gate) _socket = socket;
                        ServeConnection(socket);
                        socket.Dispose();
                    }
                }
                catch (SocketException) when (_disposed)
                {
                }
                catch (ObjectDisposedException) when (_disposed)
                {
                }
                catch (InvalidOperationException) when (_disposed)
                {
                }
            }

            void ServeConnection(Socket socket)
            {
                try
                {
                    while (TryReadFrame(socket, out byte[] frame))
                    {
                        var id = (PacketID)BitConverter.ToUInt16(frame, 2);
                        lock (_gate) _received.Add(id);
                        if (id == PacketID.C_Handshake)
                        {
                            Send(socket, new S_HandshakeResult { ok = true, serverVersion = ProtocolVersion.Current, reason = "" }.Write());
                        }
                        else if (id == PacketID.C_Login)
                        {
                            var login = new C_Login();
                            login.Read(new ArraySegment<byte>(frame));
                            lock (_gate) _logins.Add(login.accountName);
                            Send(socket, new S_LoginResult { result = (byte)_result }.Write());
                            if (_result != LoginResult.Accepted) socket.Shutdown(SocketShutdown.Send);
                        }
                    }
                }
                catch (SocketException)
                {
                }
                catch (ObjectDisposedException) when (_disposed)
                {
                }
            }

            static void Send(Socket socket, ArraySegment<byte> packet) =>
                socket.Send(packet.Array, packet.Offset, packet.Count, SocketFlags.None);

            static bool TryReadFrame(Socket socket, out byte[] frame)
            {
                frame = null;
                byte[] header = new byte[2];
                if (!TryRead(socket, header, 0, 2)) return false;
                int size = BitConverter.ToUInt16(header, 0);
                if (size < 4) return false;
                frame = new byte[size];
                frame[0] = header[0];
                frame[1] = header[1];
                return TryRead(socket, frame, 2, size - 2);
            }

            static bool TryRead(Socket socket, byte[] buffer, int offset, int count)
            {
                while (count > 0)
                {
                    int received = socket.Receive(buffer, offset, count, SocketFlags.None);
                    if (received == 0) return false;
                    offset += received;
                    count -= received;
                }
                return true;
            }
        }
    }
}
