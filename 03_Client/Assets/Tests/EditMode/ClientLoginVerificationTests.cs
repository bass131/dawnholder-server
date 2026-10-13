using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using Dawnholder.Client.Net;
using Dawnholder.Client.Network;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests
{
    // Independent verification of the multi-account login client (goal 2026-10-11-multi-account-login): D2 a result
    // byte outside LoginResult, D6 a login result whose remote close follows at once. Each case runs the production
    // stack (Connector, the ClientSession receive thread, UnityClientSession dispatch, LoginResultHandler and
    // ClientConnectionLifetime) against a loopback peer that plays the server. Main-thread work goes to a queue
    // the test drains, so "queued on main while the close arrives" is an observed state, not an assumption.
    public sealed class ClientLoginVerificationTests
    {
        const string AccountName = "alice_01";
        const byte SelectedClass = 1;
        static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

        // D2 (goal 설계 클라이언트): a result byte of 3 or 255 on an open connection is logged and the connection
        // closes. LoginRejected is not raised and no C_CharacterSelect reaches the peer.
        [TestCase((byte)3)]
        [TestCase((byte)255)]
        public void D2_ResultOutsideLoginResult_IsLoggedAndClosesWithoutRejectionOrClassSelect(byte value)
        {
            using var peer = new LoginPeer(new S_LoginResult { result = value }.Write());
            using var client = new LifetimeClient();
            LogAssert.Expect(LogType.Warning, new Regex($"(?i)result\\D*\\b{value}\\b"));

            client.Connect(peer.Port);
            bool clientClosed = client.PumpUntil(() =>
                peer.ClientClosed.IsSet && client.Owner.State == ClientConnectionState.Disconnected);

            Assert.IsTrue(clientClosed, "the connection stayed open after an out-of-range result");
            CollectionAssert.IsEmpty(client.Rejections);
            CollectionAssert.AreEqual(new[] { AccountName }, peer.Logins);
            CollectionAssert.DoesNotContain(peer.Received, PacketID.C_CharacterSelect);
        }

        // D6 (implementer fix, lead answer msg_6c2d51ac00e0): the peer sends the rejection and closes at once, as
        // GameSession.RejectLogin does. The test holds main until the transport has closed, so the rejection is
        // still queued when the close lands. LoginRejected is raised exactly once and the owner ends disconnected.
        [TestCase(LoginResult.InvalidName)]
        [TestCase(LoginResult.AlreadyOnline)]
        public void D6_RejectionQueuedWhenTheRemoteCloseArrives_IsReportedOnceAndDisconnects(LoginResult result)
        {
            using var peer = new LoginPeer(new S_LoginResult { result = (byte)result }.Write(), closeAfterReply: true,
                holdReply: true);
            using var client = new LifetimeClient();
            client.Connect(peer.Port);
            Assert.IsTrue(client.PumpUntil(() => peer.Logins.Count == 1), "fixture: the login did not reach the peer");
            UnityClientSession session = client.Session;

            peer.ReleaseReply();
            bool transportClosed = SpinWait.SpinUntil(() => session.IsClosed, Wait);
            int queuedRejections = client.Rejections.Count;
            client.Queue.Drain();

            Assert.IsTrue(transportClosed, "fixture: the remote close did not reach the session");
            Assert.AreEqual(0, queuedRejections, "the rejection ran before main drained");
            CollectionAssert.AreEqual(new[] { result }, client.Rejections);
            Assert.AreEqual(ClientConnectionState.Disconnected, client.Owner.State);
            Assert.IsNull(client.Owner.CurrentSession);
            Assert.IsTrue(peer.ClientClosed.Wait(Wait), "the client kept the connection");
            CollectionAssert.DoesNotContain(peer.Received, PacketID.C_CharacterSelect);
        }

        // D6: a rejection already queued for the old connection is ignored once the owner disconnected, and after a
        // reconnect it neither reports nor disturbs the new connection, which still selects its class once.
        [TestCase(false)]
        [TestCase(true)]
        public void D6_OldConnectionRejectionAfterDisconnectOrReconnect_IsIgnored(bool reconnect)
        {
            using var peer = new LoginPeer(new S_LoginResult { result = (byte)LoginResult.Accepted }.Write(),
                holdReply: true);
            using var client = new LifetimeClient();
            client.Connect(peer.Port);
            Assert.IsTrue(client.PumpUntil(() => peer.Logins.Count == 1), "fixture: the first login did not arrive");
            UnityClientSession old = client.Session;

            // The receive path of the old connection delivers AlreadyOnline; main has not run it yet.
            old.OnRecvPacket(new S_LoginResult { result = (byte)LoginResult.AlreadyOnline }.Write());
            int queuedBeforeDisconnect = client.Queue.Count;
            client.Owner.Disconnect();
            // The peer was holding its reply to the old login; let it finish that connection and serve the next.
            peer.ReleaseReply();
            if (reconnect)
            {
                client.Connect(peer.Port);
                Assert.IsTrue(client.PumpUntil(() => peer.Logins.Count == 2), "fixture: the second login did not arrive");
                peer.ReleaseReply();
                Assert.IsTrue(client.PumpUntil(() => peer.Selects.Count == 1), "the new connection did not select");
            }
            client.Queue.Drain();

            Assert.Greater(queuedBeforeDisconnect, 0, "fixture: the old rejection was not queued");
            CollectionAssert.IsEmpty(client.Rejections);
            if (reconnect)
            {
                Assert.AreEqual(ClientConnectionState.Connected, client.Owner.State);
                Assert.AreNotSame(old, client.Owner.CurrentSession);
                CollectionAssert.AreEqual(new[] { SelectedClass }, peer.Selects);
            }
            else
            {
                Assert.AreEqual(ClientConnectionState.Disconnected, client.Owner.State);
                Assert.IsNull(client.Owner.CurrentSession);
                CollectionAssert.IsEmpty(peer.Selects);
            }
        }

        // D6: Accepted followed at once by the remote close is not applied on the closed transport, so no class is
        // selected; the peer keeps reading after its half-close and records nothing but the login.
        [Test]
        public void D6_AcceptedQueuedWhenTheRemoteCloseArrives_SelectsNoClass()
        {
            using var peer = new LoginPeer(new S_LoginResult { result = (byte)LoginResult.Accepted }.Write(),
                closeAfterReply: true, holdReply: true);
            using var client = new LifetimeClient();
            client.Connect(peer.Port);
            Assert.IsTrue(client.PumpUntil(() => peer.Logins.Count == 1), "fixture: the login did not reach the peer");
            UnityClientSession session = client.Session;

            peer.ReleaseReply();
            bool transportClosed = SpinWait.SpinUntil(() => session.IsClosed, Wait);
            client.Queue.Drain();
            bool ownerClosed = client.PumpUntil(() => client.Owner.State == ClientConnectionState.Disconnected);

            Assert.IsTrue(transportClosed, "fixture: the remote close did not reach the session");
            Assert.IsTrue(ownerClosed, "the owner did not end the closed connection");
            Assert.IsTrue(peer.ClientClosed.Wait(Wait), "the client kept the connection");
            CollectionAssert.IsEmpty(peer.Selects);
            CollectionAssert.IsEmpty(client.Rejections);
        }

        // D6: an out-of-range result (3) and an unregistered packet (id 999) that precede the remote close are not
        // applied on the closed transport: neither warning appears and nothing is rejected. With the connection
        // left open (the control case) the same two frames produce both warnings, so the absence is meaningful.
        [TestCase(true)]
        [TestCase(false)]
        public void D6_OutOfRangeResultAndOtherPacket_ApplyOnlyWhileTheTransportIsOpen(bool closeAfterReply)
        {
            byte[] unknown = { 4, 0, 0xE7, 0x03 };
            byte[] outOfRange = LoginPeer.Copy(new S_LoginResult { result = 3 }.Write());
            byte[] reply = new byte[unknown.Length + outOfRange.Length];
            Buffer.BlockCopy(unknown, 0, reply, 0, unknown.Length);
            Buffer.BlockCopy(outOfRange, 0, reply, unknown.Length, outOfRange.Length);
            using var peer = new LoginPeer(new ArraySegment<byte>(reply), closeAfterReply, holdReply: true);
            using var client = new LifetimeClient();
            using var logs = new WarningCapture();
            client.Connect(peer.Port);
            Assert.IsTrue(client.PumpUntil(() => peer.Logins.Count == 1), "fixture: the login did not reach the peer");
            UnityClientSession session = client.Session;

            peer.ReleaseReply();
            if (closeAfterReply)
            {
                Assert.IsTrue(SpinWait.SpinUntil(() => session.IsClosed, Wait), "fixture: the close did not arrive");
                client.Queue.Drain();
            }
            else
            {
                LogAssert.Expect(LogType.Warning, new Regex("999"));
                LogAssert.Expect(LogType.Warning, new Regex($"(?i)result\\D*\\b3\\b"));
                Assert.IsTrue(client.PumpUntil(() => client.Owner.State == ClientConnectionState.Disconnected),
                    "control: the out-of-range result did not close the open connection");
            }

            bool unknownWarned = logs.Any("999");
            bool outOfRangeWarned = logs.Any("result");
            Assert.AreEqual(!closeAfterReply, unknownWarned, "unregistered packet warning");
            Assert.AreEqual(!closeAfterReply, outOfRangeWarned, "out-of-range result warning");
            CollectionAssert.IsEmpty(client.Rejections);
            CollectionAssert.IsEmpty(peer.Selects);
        }

        // Main-thread queue the test drains; Count lets a case prove that work is waiting.
        sealed class DrainQueue
        {
            readonly ConcurrentQueue<Action> _items = new();

            internal int Count => _items.Count;

            internal void Post(Action action) => _items.Enqueue(action);

            internal void Drain()
            {
                while (_items.TryDequeue(out Action action)) action();
            }
        }

        // The production owner, connector and Unity session, wired as NetworkService does except for the queue.
        sealed class LifetimeClient : IDisposable
        {
            internal readonly DrainQueue Queue = new();
            internal readonly List<LoginResult> Rejections = new();
            internal readonly ClientConnectionLifetime Owner;

            internal LifetimeClient()
            {
                var connector = new Connector();
                Owner = new ClientConnectionLifetime(connector.BeginConnect, Queue.Post,
                    current => new UnityClientSession(current, Queue.Post), () => { });
                Owner.LoginRejected += Rejections.Add;
            }

            internal UnityClientSession Session => Owner.CurrentSession as UnityClientSession;

            internal void Connect(int port)
            {
                var endPoint = new IPEndPoint(IPAddress.Loopback, port);
                Assert.IsTrue(Owner.Connect(endPoint, SelectedClass, AccountName), "fixture: the owner refused to connect");
            }

            // Drains main until the condition holds; a bounded wait, not a measurement.
            internal bool PumpUntil(Func<bool> done)
            {
                DateTime deadline = DateTime.UtcNow + Wait;
                while (DateTime.UtcNow < deadline)
                {
                    Queue.Drain();
                    if (done()) return true;
                    Thread.Sleep(5);
                }
                Queue.Drain();
                return done();
            }

            public void Dispose()
            {
                Owner.Dispose();
                Queue.Drain();
            }
        }

        // Warnings logged while it is alive, from any thread.
        sealed class WarningCapture : IDisposable
        {
            readonly List<string> _warnings = new();

            internal WarningCapture() => Application.logMessageReceivedThreaded += Record;

            internal bool Any(string fragment)
            {
                lock (_warnings)
                {
                    return _warnings.Exists(message => message.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0);
                }
            }

            public void Dispose() => Application.logMessageReceivedThreaded -= Record;

            void Record(string message, string stackTrace, LogType type)
            {
                if (type != LogType.Warning) return;
                lock (_warnings)
                {
                    _warnings.Add(message);
                }
            }
        }

        // Plays the server: answers C_Handshake with ok and every C_Login with the given bytes, released at once or
        // when the test calls ReleaseReply. With closeAfterReply it half-closes right after the reply, as the server
        // sends a rejection and disconnects, and keeps reading so every frame the client still sends is recorded.
        // Each accepted connection is served in turn on one background thread.
        sealed class LoginPeer : IDisposable
        {
            readonly TcpListener _listener = new(IPAddress.Loopback, 0);
            readonly byte[] _reply;
            readonly bool _closeAfterReply;
            readonly SemaphoreSlim _release;
            readonly Thread _thread;
            readonly object _gate = new();
            readonly List<PacketID> _received = new();
            readonly List<string> _logins = new();
            readonly List<byte> _selects = new();
            Socket _socket;
            volatile bool _disposed;

            internal LoginPeer(ArraySegment<byte> reply, bool closeAfterReply = false, bool holdReply = false)
            {
                _reply = Copy(reply);
                _closeAfterReply = closeAfterReply;
                _release = holdReply ? new SemaphoreSlim(0) : null;
                _listener.Start();
                Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
                _thread = new Thread(Serve) { IsBackground = true, Name = "login verification peer" };
                _thread.Start();
            }

            internal int Port { get; }

            internal ManualResetEventSlim ClientClosed { get; } = new();

            internal List<PacketID> Received { get { lock (_gate) return new List<PacketID>(_received); } }

            internal List<string> Logins { get { lock (_gate) return new List<string>(_logins); } }

            internal List<byte> Selects { get { lock (_gate) return new List<byte>(_selects); } }

            internal void ReleaseReply() => _release?.Release();

            public void Dispose()
            {
                _disposed = true;
                _listener.Stop();
                lock (_gate) _socket?.Dispose();
                _release?.Release(16);
                _thread.Join(Wait);
            }

            void Serve()
            {
                try
                {
                    while (!_disposed)
                    {
                        Socket socket = _listener.AcceptSocket();
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
                ClientClosed.Reset();
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
                            _release?.Wait();
                            if (_disposed) return;
                            Send(socket, new ArraySegment<byte>(_reply));
                            if (_closeAfterReply) socket.Shutdown(SocketShutdown.Send);
                        }
                        else if (id == PacketID.C_CharacterSelect)
                        {
                            var select = new C_CharacterSelect();
                            select.Read(new ArraySegment<byte>(frame));
                            lock (_gate) _selects.Add(select.characterClass);
                        }
                    }
                }
                catch (SocketException)
                {
                }
                catch (ObjectDisposedException) when (_disposed)
                {
                }
                finally
                {
                    ClientClosed.Set();
                }
            }

            internal static byte[] Copy(ArraySegment<byte> packet)
            {
                byte[] bytes = new byte[packet.Count];
                Buffer.BlockCopy(packet.Array, packet.Offset, bytes, 0, packet.Count);
                return bytes;
            }

            static void Send(Socket socket, ArraySegment<byte> packet) =>
                socket.Send(packet.Array, packet.Offset, packet.Count, SocketFlags.None);

            // False at the client's close (EOF before a whole frame).
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
