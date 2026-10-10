using System;
using System.Collections.Generic;
using System.Text;
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Dawnholder.Client.Tests
{
    // R1~R3 of the defect PR (01_Phases/goals/2026-10-10-dungeon-clear-rewards/goal.md 「결함 PR의 요구」): the HUD
    // gold shows the connection's inventory mirror. Real paths: snapshot packets through UnityClientSession.OnRecvPacket
    // into InventoryState and its change notification, the gameplay Ready barrier, and the connection owner's cleanup.
    // Stand-ins: the UI scene's HUD is a HudController whose serialized gold label is set by field name, and Unity
    // messages are sent in Unity's order (ViewLifecycle). The gold is read as the digits its label shows, so the text
    // format stays the implementer's; expected numbers are the server literals sent here.
    public sealed class HudGoldMirrorTests
    {
        const int SerializedGold = 777; // an inspector value that must never pass for the server's gold

        readonly List<GameObject> _objects = new();
        GameplayConnection _connection;
        InventoryMirrorProbe _mirror;
        GameObject _hud;
        bool _hudDisabled;
        int _latency;

        [SetUp]
        public void Setup()
        {
            Assert.IsNull(UnityClientSession.Instance, "another test left a published session");
            Assert.IsNull(HudController.Instance, "another test left a HUD");
            _latency = UnityClientSession.SimulatedLatencyMs;
            UnityClientSession.SimulatedLatencyMs = 0;
            _connection = new GameplayConnection();
            _connection.BeginMap(0);
            _connection.Ready();
        }

        [TearDown]
        public void Teardown()
        {
            try
            {
                _connection?.Dispose(); // a lost connection is cleaned while the HUD still lives
            }
            finally
            {
                if (_hud != null) ViewLifecycle.Destroy(_hud, _hudDisabled);
                foreach (GameObject owned in _objects)
                {
                    if (owned != null) UnityEngine.Object.DestroyImmediate(owned);
                }
                _objects.Clear();
                _mirror?.Dispose();
                if (!ReferenceEquals(HudController.Instance, null))
                {
                    // Only when the HUD's OnDestroy failed; the next test's Awake would otherwise see a stale owner.
                    typeof(HudController).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { null });
                }
                UnityClientSession.SimulatedLatencyMs = _latency;
            }
        }

        // R1: the HUD shows the mirror's currency, and a change is shown inside the mirror's change notification.
        [Test]
        public void Gold_ShowsTheMirrorCurrency_AndChangesInsideTheMirrorNotification()
        {
            _mirror = InventoryMirrorProbe.TryPublish();
            _connection.Deliver(new ServerInventory(1, 1234));
            TMP_Text gold = StartHud();
            string shownAtStart = ShownGold(gold);

            string shownInsideNotification = null;
            Action observe = () => shownInsideNotification = ShownGold(gold);
            InventoryState.Instance.OnInventoryChanged += observe; // runs after the HUD's own subscription
            _connection.Deliver(new ServerInventory(2, 56789));
            InventoryState.Instance.OnInventoryChanged -= observe;
            string shownAfterNotification = ShownGold(gold);

            Assert.AreEqual("1234", shownAtStart, "R1 the HUD shows the mirror's currency");
            Assert.AreEqual("56789", shownInsideNotification, "R1 the new currency is shown within the change notification");
            Assert.AreEqual("56789", shownAfterNotification, "R1 the new currency stays shown");
        }

        // R2: without a snapshot (before the first one, after the connection cleanup) no number is shown; neither 0
        // nor the serialized initial value passes for the server's gold. A real revision-0 snapshot does show 0.
        [Test]
        public void Gold_ShowsNoNumber_BeforeTheFirstSnapshot_AndAfterTheConnectionCleanup()
        {
            _mirror = InventoryMirrorProbe.TryPublish();
            TMP_Text gold = StartHud();
            string beforeSnapshot = ShownGold(gold);
            _connection.Deliver(new ServerInventory(0, 0));
            string revisionZero = ShownGold(gold);
            _connection.End(); // transport close, session cleanup, then the owner resets the mirror
            ViewLifecycle.Frame(_hud);
            string afterCleanup = ShownGold(gold);

            Assert.AreEqual(string.Empty, beforeSnapshot, "R2 no number before the first snapshot (not 0, not the serialized 777)");
            Assert.AreEqual("0", revisionZero, "R1 a confirmed revision-0 snapshot with currency 0 shows 0");
            Assert.AreEqual(string.Empty, afterCleanup, "R2 no number after the connection cleanup");
        }

        // R3: a mirror created after the HUD (CombatBootstrap or the first snapshot creates it) is followed.
        [Test]
        public void Gold_FollowsAMirrorCreatedAfterTheHud()
        {
            TMP_Text gold = StartHud();
            string withoutMirror = ShownGold(gold);
            _mirror = InventoryMirrorProbe.TryPublish();
            _connection.Deliver(new ServerInventory(1, 4321));
            ViewLifecycle.Frame(_hud);
            string shownFromLateMirror = ShownGold(gold);

            string shownInsideNotification = null;
            Action observe = () => shownInsideNotification = ShownGold(gold);
            InventoryState.Instance.OnInventoryChanged += observe;
            _connection.Deliver(new ServerInventory(2, 8765));
            InventoryState.Instance.OnInventoryChanged -= observe;

            Assert.AreEqual("4321", shownFromLateMirror, "R3 the HUD follows the mirror that appeared after it");
            Assert.AreEqual("8765", shownInsideNotification, "R3 and shows that mirror's next change within its notification");
            Assert.AreEqual(string.Empty, withoutMirror, "R2 no number while no mirror exists");
        }

        // R3: when the mirror is replaced the HUD leaves the one it subscribed to; that mirror's later notification
        // changes nothing. The replaced mirror is destroyed, but its managed event can still be raised.
        [Test]
        public void Gold_LeavesAReplacedMirror_AndIgnoresItsLaterNotification()
        {
            _mirror = InventoryMirrorProbe.TryPublish();
            InventoryState first = InventoryState.Instance;
            _connection.Deliver(new ServerInventory(1, 1111));
            TMP_Text gold = StartHud();
            string shownFromFirst = ShownGold(gold);

            _mirror.Dispose();
            _mirror = InventoryMirrorProbe.TryPublish();
            InventoryState second = InventoryState.Instance;
            _connection.Deliver(new ServerInventory(1, 2222));
            ViewLifecycle.Frame(_hud);
            string shownFromSecond = ShownGold(gold);
            first.ResetSession(); // a late notification from the replaced mirror, now without a snapshot
            string afterReplacedNotification = ShownGold(gold);
            int firstSubscribers = EventSubscribers.Count(first, nameof(InventoryState.OnInventoryChanged));
            int secondSubscribers = EventSubscribers.Count(second, nameof(InventoryState.OnInventoryChanged));

            Assert.AreEqual("1111", shownFromFirst, "R1 precondition: the HUD showed the first mirror");
            Assert.AreEqual("2222", shownFromSecond, "R3 the HUD follows the replacing mirror");
            Assert.AreEqual(0, firstSubscribers, "R3 the HUD left the mirror it had subscribed to");
            Assert.AreEqual(1, secondSubscribers, "R3 the HUD subscribes to the current mirror once");
            Assert.AreEqual("2222", afterReplacedNotification, "R3 the replaced mirror's notification does not change the HUD");
        }

        // R3: after disable or destroy the HUD has left the mirror, and a later notification changes nothing.
        [TestCase(false)]
        [TestCase(true)]
        public void Gold_IgnoresMirrorNotifications_AfterTheHudIsDisabledOrDestroyed(bool destroyed)
        {
            _mirror = InventoryMirrorProbe.TryPublish();
            InventoryState mirror = InventoryState.Instance;
            _connection.Deliver(new ServerInventory(1, 1111));
            TMP_Text gold = StartHud();
            string followed = ShownGold(gold);
            int subscribedWhileEnabled = EventSubscribers.Count(mirror, nameof(InventoryState.OnInventoryChanged));

            if (destroyed)
            {
                ViewLifecycle.Destroy(_hud);
            }
            else
            {
                ViewLifecycle.Disable(_hud);
                _hudDisabled = true;
            }
            int subscribedAfter = EventSubscribers.Count(mirror, nameof(InventoryState.OnInventoryChanged));
            Assert.DoesNotThrow(() => _connection.Deliver(new ServerInventory(2, 2222)),
                "R3 a later notification reaches no HUD code that fails");
            string afterNotification = ShownGold(gold);
            string ended = destroyed ? "destroy" : "disable";

            Assert.AreEqual("1111", followed, "R1 precondition: the HUD showed the mirror");
            Assert.AreEqual(1, subscribedWhileEnabled, "R1 precondition: the enabled HUD subscribed to the mirror once");
            Assert.AreEqual(0, subscribedAfter, $"R3 {ended} leaves the mirror the HUD subscribed to");
            Assert.AreEqual("1111", afterNotification, $"R3 a notification after {ended} does not change the HUD");
        }

        // What a player reads as the gold amount: the label's digits, or nothing while the label is not drawn.
        static string ShownGold(TMP_Text gold)
        {
            if (!gold.gameObject.activeInHierarchy || !gold.enabled) return string.Empty;
            var digits = new StringBuilder();
            foreach (char character in gold.text ?? string.Empty)
            {
                if (char.IsDigit(character)) digits.Append(character);
            }
            return digits.ToString();
        }

        // The UI scene's HUD: its serialized gold label is set by field name, then Unity starts it and runs a frame.
        // The label is a separate object, so it can still be read after the HUD itself is destroyed.
        TMP_Text StartHud()
        {
            GameObject label = Own(new GameObject("Gold label"));
            var gold = label.AddComponent<TextMeshProUGUI>();
            _hud = new GameObject("HUD under test");
            var hud = _hud.AddComponent<HudController>();
            var serialized = new SerializedObject(hud);
            SerializedProperty goldText = serialized.FindProperty("_goldText");
            Assert.IsNotNull(goldText, "fixture: HudController keeps its serialized gold label _goldText");
            goldText.objectReferenceValue = gold;
            SerializedProperty initialGold = serialized.FindProperty("_mockGold");
            if (initialGold != null) initialGold.intValue = SerializedGold; // absent once the mock value is removed
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ViewLifecycle.Begin(_hud);
            ViewLifecycle.Frame(_hud);
            return gold;
        }

        GameObject Own(GameObject owned)
        {
            _objects.Add(owned);
            return owned;
        }
    }
}
