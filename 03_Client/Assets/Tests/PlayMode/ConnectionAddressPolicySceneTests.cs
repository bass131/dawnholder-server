using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Dawnholder.Client.Network;
using Dawnholder.Client.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Goal 2026-11-10-code-audit-client-fixes item 8, user-approved policy "IP only": the probe and the real connection
    // apply the same rule, so a host name is refused by the probe without resolving it.
    public sealed class ConnectionAddressPolicySceneTests
    {
        const int ProbeTimeoutMs = 3000;
        const float CallbackLimitSeconds = 10f;

        TcpListener _listener;
        TcpListener _listenerV6;
        NetworkService _network;
        int _oldPort;
        bool _portCaptured;
        int _accepted;
        bool _callbackRan;
        bool _success;
        string _message;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return MapEntryPlayFixture.Wait(() => NetworkService.Instance != null && SceneTransition.Instance != null, "persistent services");
            _network = NetworkService.Instance;
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            // localhost may resolve to ::1 first. Both loopback stand-ins share the port, so the current probe reaches an
            // open listener whichever address it picks; a refused ::1 would otherwise look like the fixed behaviour.
            _listenerV6 = null;
            try
            {
                _listenerV6 = new TcpListener(IPAddress.IPv6Loopback, Port);
                _listenerV6.Start();
            }
            catch (SocketException error)
            {
                _listenerV6 = null;
                TestContext.Out.WriteLine("IPv6 loopback stand-in unavailable, continuing on IPv4 only: " + error.SocketErrorCode);
            }
            _portCaptured = false;
            _accepted = 0;
            _callbackRan = false;
            _success = false;
            _message = null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            try
            {
                yield return MapEntryPlayFixture.Wait(() => !SceneTransition.Instance.IsTransitioning, "owned scene operation", 20);
                Scene menu = SceneManager.GetSceneByName("MainMenu");
                if (menu.IsValid() && menu.isLoaded)
                {
                    Scene empty = SceneManager.CreateScene("AddressPolicy-empty");
                    SceneManager.SetActiveScene(empty);
                    yield return SceneManager.UnloadSceneAsync(menu);
                }
            }
            finally
            {
                _listener?.Stop();
                _listenerV6?.Stop();
                if (_portCaptured) MapEntryPlayFixture.Set(_network, "_serverPort", _oldPort);
            }
        }

        int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        void PumpAccepted()
        {
            foreach (TcpListener listener in new[] { _listener, _listenerV6 })
            {
                if (listener == null) continue;
                while (listener.Pending())
                {
                    listener.AcceptTcpClient().Close();
                    _accepted++;
                }
            }
        }

        IEnumerator Probe(string host)
        {
            ConnectionProbe.TryConnect(host, Port, (success, message) =>
            {
                _success = success;
                _message = message;
                _callbackRan = true;
            }, ProbeTimeoutMs);
            float deadline = Time.realtimeSinceStartup + CallbackLimitSeconds;
            while (!_callbackRan && Time.realtimeSinceStartup < deadline) yield return null;
            yield return null;
            PumpAccepted();
            Assert.IsTrue(_callbackRan, $"the probe of {host} calls back within {CallbackLimitSeconds} s");
            TestContext.Out.WriteLine($"host={host}; success={_success}; message=「{_message}」; accepted={_accepted}");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator HostName_IsRefusedByTheProbe_WithoutReachingTheListener()
        {
            // The resolution order decides what the current probe reaches; keep it beside the result.
            TestContext.Out.WriteLine("localhost resolves to: " + string.Join(", ", Dns.GetHostAddresses("localhost").Select(address => address.ToString())));
            yield return Probe("localhost");
            bool probeSucceeded = _success;
            int connectionsAccepted = _accepted;
            Assert.IsFalse(probeSucceeded, "item 8: the probe refuses a host name under the IP-only policy");
            Assert.AreEqual(0, connectionsAccepted, "item 8: a refused host name opens no connection");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator IpAddress_ProbeStillSucceeds()
        {
            yield return Probe("127.0.0.1");
            bool probeSucceeded = _success;
            int connectionsAccepted = _accepted;
            Assert.IsTrue(probeSucceeded, "item 8: the probe keeps accepting an IP address");
            Assert.AreEqual(1, connectionsAccepted, "item 8: the IP address probe connects once");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator RealConnection_KeepsRefusingAHostName()
        {
            Assert.IsFalse(_network.IsConnected, "fixture cannot adopt another connection");
            _oldPort = MapEntryPlayFixture.Field<int>(_network, "_serverPort");
            _portCaptured = true;
            MapEntryPlayFixture.Set(_network, "_serverPort", Port);

            _network.ConnectCharacterMenu("localhost");
            yield return MapEntryPlayFixture.Wait(() => SceneManager.GetSceneByName("MainMenu").isLoaded, "the refused address returns to MainMenu", 20);
            PumpAccepted();
            bool connected = _network.IsConnected;
            int connectionsAccepted = _accepted;
            Assert.IsFalse(connected, "item 8: the real connection refuses a host name");
            Assert.AreEqual(0, connectionsAccepted, "item 8: the refused host name reaches no listener");
        }
    }
}
