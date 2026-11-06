using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using Dawnholder.Client.Demo;
using Dawnholder.Client.Network;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    public sealed class DemoUiFinishVerifyLoginPathTests
    {
        // Reuse only the public scene setup/cleanup. The original T6 stops at login; B6 also requires character list.
        MainMenuLoginIdPwSceneTests _menu;
        readonly List<string> _steps = new();
        TcpListener _probe;
        TcpClient _probeClient;
        EntryWirePeer _peer;
        GameObject _pilotObject;
        string _password;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _menu = new MainMenuLoginIdPwSceneTests();
            yield return _menu.SetUp();
            _steps.Clear();
            Application.logMessageReceived += ObserveStep;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Application.logMessageReceived -= ObserveStep;
            NetworkService.Instance?.Disconnect();
            _peer?.Dispose();
            _probeClient?.Dispose();
            _probe?.Stop();
            if (_pilotObject != null) UnityEngine.Object.Destroy(_pilotObject);
            if (_menu != null) yield return _menu.TearDown();
        }

        void ObserveStep(string message, string stack, LogType kind)
        {
            if (!message.StartsWith("[Demo] step ", StringComparison.Ordinal)) return;
            _steps.Add(message);
            if (message.Contains(" login-screen-visible "))
                _password = UnityEngine.Object.FindObjectsByType<TMP_InputField>(FindObjectsSortMode.None)
                    .Single(field => field.name == "PasswordInput").text;
        }

        [UnityTest]
        public IEnumerator AutopilotCrossesTheNewLoginAndReachesTheRealCharacterList()
        {
            MainMenuController controller = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
            _probe = new TcpListener(IPAddress.Loopback, 0);
            _probe.Start();
            MapEntryPlayFixture.Set(controller, "_serverPort", ((IPEndPoint)_probe.LocalEndpoint).Port);
            _peer = EntryWirePeer.Listen();
            MapEntryPlayFixture.Set(NetworkService.Instance, "_serverPort", _peer.Port);
            _pilotObject = new GameObject("Independent login path");
            _pilotObject.SetActive(false);
            DemoAutopilot pilot = _pilotObject.AddComponent<DemoAutopilot>();
            object options = typeof(DemoLaunchOptions).GetMethod("Parse", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { new[] { "Dawnholder.exe", "-demo-role", "knight",
                    "-demo-account", "verify_b6", "-demo-character", "VerifyKnight" } });
            MapEntryPlayFixture.Set(pilot, "_options", options);
            IEnumerator entry = (IEnumerator)typeof(DemoAutopilot)
                .GetMethod("EnterGame", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pilot, null);
            var pending = new Stack<IEnumerator>();
            pending.Push(entry);
            bool replied = false;
            float deadline = Time.realtimeSinceStartup + 20f;
            try
            {
                // Drive the real nested iterators, supplying only the owned server reply when its wire request arrives.
                while (pending.Count > 0 && !HasStep("character list"))
                {
                    Assert.Less(Time.realtimeSinceStartup, deadline, "B6: login-to-list path completes");
                    if (_probeClient == null && _probe.Pending()) _probeClient = _probe.AcceptTcpClient();
                    if (!replied && _peer.Count(PacketID.C_CharacterListRequest) > 0)
                    {
                        var request = _peer.Last<C_CharacterListRequest>(PacketID.C_CharacterListRequest);
                        _peer.Send(new S_CharacterListResult {
                            requestId = request.requestId, result = 0, occupiedMask = 0,
                            slot1CharacterIdD = "", slot1Name = "", slot2CharacterIdD = "", slot2Name = "",
                            slot3CharacterIdD = "", slot3Name = ""
                        }.Write());
                        replied = true;
                    }
                    IEnumerator current = pending.Peek();
                    if (!current.MoveNext())
                    {
                        (pending.Pop() as IDisposable)?.Dispose();
                        continue;
                    }
                    if (current.Current is IEnumerator nested) pending.Push(nested);
                    else yield return current.Current;
                }
            }
            finally
            {
                while (pending.Count > 0) (pending.Pop() as IDisposable)?.Dispose();
            }
            Assert.IsTrue(replied, "B6: a real list request received a framed reply");
            Assert.IsTrue(HasStep("login"));
            Assert.IsTrue(HasStep("character list"));
            Assert.Less(_steps.FindIndex(line => IsStep(line, "login")),
                _steps.FindIndex(line => IsStep(line, "character list")));
            Assert.AreEqual(8, _password?.Length, "the new PW field was filled before login");
            StringAssert.DoesNotContain(_password, string.Join("\n", _steps));
            Assert.IsEmpty(_peer.Errors);
            TestContext.Out.WriteLine(string.Join("\n", _steps));
            TestContext.Out.WriteLine("B6: real MainMenu -> probe -> CharacterSelect -> list request/reply; server is a loopback peer.");
        }

        bool HasStep(string name) => _steps.Any(line => IsStep(line, name));
        static bool IsStep(string line, string name) => Regex.IsMatch(line,
            @"\[Demo\] step \d+ " + Regex.Escape(name) + " ");
    }
}
