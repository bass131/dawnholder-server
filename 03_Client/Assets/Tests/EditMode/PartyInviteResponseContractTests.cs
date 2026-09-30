using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Dawnholder.Client.Audio;
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // Exercise the existing runtime builder and its actual UnityEvent listeners.
    // Explicit lifecycle calls keep EditMode independent of a loaded gameplay scene.
    internal sealed class PartyPopupFixture : IDisposable
    {
        internal readonly GameObject Root;
        internal readonly PartyState State;
        internal readonly PartyInvitePopup Popup;
        internal readonly CanvasGroup Group;
        internal readonly ManualConnectionQueue Queue = new();
        internal readonly List<Action> Delayed = new();
        internal readonly TestSocketPair Sockets;
        internal readonly UnityClientSession Session;
        internal readonly EntryBindingFixture Views;
        internal bool Current = true;
        internal Action BeforeSchedule;
        readonly GameObject _stateObject;
        readonly int _latency;

        internal PartyPopupFixture()
        {
            Assert.IsNull(PartyState.Instance);
            Assert.IsNull(PartyInvitePopup.Instance);
            Assert.IsNull(UnityClientSession.Instance);
            Assert.IsNull(AudioManager.Instance, "fixture must not play through a scene audio owner");
            _latency = UnityClientSession.SimulatedLatencyMs;
            UnityClientSession.SimulatedLatencyMs = 0;
            _stateObject = new GameObject("Party contract state");
            _stateObject.SetActive(false);
            State = _stateObject.AddComponent<PartyState>();
            SetState(State);
            State.ApplyUpdate(31, 71, 71, 72, 0, 1);
            State.SetLastError(2);
            Root = new GameObject("Party contract popup parent");
            Root.SetActive(false);
            Popup = PartyInvitePopup.BuildRuntime(Root.transform);
            SessionTestTools.Call(Popup, "Awake");
            SessionTestTools.Call(Popup, "OnEnable");
            Group = Popup.GetComponent<CanvasGroup>();
            Sockets = new TestSocketPair();
            Session = new UnityClientSession(() => Current, Queue.Post, (action, _) =>
            {
                BeforeSchedule?.Invoke();
                Delayed.Add(action);
            });
            Session.Start(Sockets.Client);
            Session.Publish();
            Views = new EntryBindingFixture(Session);
        }

        internal static void SetState(PartyState state) => typeof(PartyState).GetProperty("Instance")
            .GetSetMethod(true).Invoke(null, new object[] { state });
        internal void Invite(int inviter = 1234567) => State.SetPendingInvite(inviter, 1);
        internal void Click(bool accept) => Popup.transform.Find("Panel/" + (accept ? "AcceptButton" : "RejectButton"))
            .GetComponent<Button>().onClick.Invoke();
        internal void Ready()
        {
            SessionTestTools.Handshake(Session, Queue);
            Views.Begin();
            Views.Ready();
        }
        internal void AssertVisible(bool visible)
        {
            Assert.AreEqual(visible ? 1f : 0f, Group.alpha);
            Assert.AreEqual(visible, Group.interactable);
            Assert.AreEqual(visible, Group.blocksRaycasts);
        }
        internal void AssertMembership()
        {
            Assert.AreEqual(31, State.PartyId);
            Assert.AreEqual(71, State.LeaderEntityId);
            Assert.AreEqual(71, State.Member0EntityId);
            Assert.AreEqual(72, State.Member1EntityId);
            Assert.AreEqual(0, State.Member0Class);
            Assert.AreEqual(1, State.Member1Class);
            Assert.AreEqual(2, State.LastErrorReason);
        }
        internal void AssertPending(bool pending)
        {
            Assert.AreEqual(pending, State.HasPendingInvite);
            Assert.AreEqual(pending ? 1234567 : 0, State.PendingInviterEntityId);
            Assert.AreEqual(pending ? 1 : 0, State.PendingInviterClass);
        }
        internal void AssertNoPacket() => Assert.IsFalse(Sockets.Peer.Poll(100000, SelectMode.SelectRead));
        internal void AssertResponse(bool accept, int inviter = 1234567)
        {
            byte[] frame = Sockets.ReadFrame();
            Assert.AreEqual(PacketID.C_PartyRespond, (PacketID)BitConverter.ToUInt16(frame, 2));
            var packet = new C_PartyRespond();
            packet.Read(new ArraySegment<byte>(frame));
            Assert.AreEqual(inviter, packet.inviterEntityId);
            Assert.AreEqual(accept ? 1 : 0, packet.accept);
            AssertNoPacket();
        }
        public void Dispose()
        {
            SetState(State);
            SessionTestTools.Call(Popup, "OnDisable");
            SessionTestTools.Call(Popup, "OnDestroy");
            UnityEngine.Object.DestroyImmediate(Root);
            SetState(null);
            UnityEngine.Object.DestroyImmediate(_stateObject);
            Session.Disconnect();
            Queue.Drain();
            Session.Cleanup();
            Views.Dispose();
            Sockets.Dispose();
            UnityClientSession.SimulatedLatencyMs = _latency;
        }
    }

    public sealed class PartyInviteResponseContractTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void NoState_ButtonStillHidesWithoutSending(bool accept)
        {
            using var h = new PartyPopupFixture();
            h.Ready();
            h.Invite();
            PartyPopupFixture.SetState(null);
            h.Click(accept);
            h.AssertVisible(false);
            h.AssertPending(true);
            h.AssertMembership();
            h.AssertNoPacket();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void NoPending_ButtonHidesWithoutSending(bool accept)
        {
            using var h = new PartyPopupFixture();
            h.Ready();
            h.Invite();
            h.State.ClearPendingInvite();
            LogAssert.Expect(LogType.Warning, "[PartyInvitePopup] HasPendingInvite=false — 응답 취소.");
            h.Click(accept);
            h.AssertVisible(false);
            h.AssertPending(false);
            h.AssertMembership();
            h.AssertNoPacket();
        }

        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void MissingSessionOrHandshake_HidesButRetainsInvite(bool noSession, bool accept)
        {
            using var h = new PartyPopupFixture();
            h.Invite();
            if (noSession) h.Session.Cleanup();
            LogAssert.Expect(LogType.Warning, "[PartyInvitePopup] 세션 없음 또는 Handshake 미완료 — 응답 송신 불가.");
            h.Click(accept);
            h.AssertVisible(false);
            h.AssertPending(true);
            h.AssertMembership();
            h.AssertNoPacket();
        }

        [TestCase("entry")]
        [TestCase("generation")]
        [TestCase("closed")]
        public void SilentSendGateDrop_StillConsumesAndHides(string gate)
        {
            using var h = new PartyPopupFixture();
            h.Ready();
            h.Invite();
            if (gate == "entry") h.Views.Begin();
            if (gate == "generation") h.Current = false;
            // Natural-close callback marks closed before the queued cleanup clears handshake/Instance.
            if (gate == "closed") h.Session.OnDisconnected(new IPEndPoint(IPAddress.Loopback, 1));
            Assert.IsTrue(h.Session.HandshakeOk);
            Assert.IsFalse(h.Session.CanSendGameplay);
            h.Click(true);
            h.AssertPending(false);
            h.AssertVisible(false);
            h.AssertMembership();
            h.AssertNoPacket();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Ready_ButtonSendsCurrentInviterAndChoiceWithoutChangingMembership(bool accept)
        {
            using var h = new PartyPopupFixture();
            h.Ready();
            h.Invite(800);
            h.Invite();
            h.AssertVisible(true);
            h.Click(accept);
            h.AssertResponse(accept);
            h.AssertPending(false);
            h.AssertVisible(false);
            h.AssertMembership();
        }

        [TestCase("live")]
        [TestCase("generation")]
        [TestCase("epoch")]
        [TestCase("closed")]
        public void DelayedSend_ConsumesBeforeCallback_AndRechecksGates(string gate)
        {
            using var h = new PartyPopupFixture();
            h.Ready();
            h.Invite();
            UnityClientSession.SimulatedLatencyMs = 100;
            h.BeforeSchedule = () => { h.AssertPending(true); h.AssertVisible(true); h.AssertMembership(); };
            h.Click(false);
            Assert.AreEqual(1, h.Delayed.Count);
            h.AssertPending(false);
            h.AssertVisible(false);
            h.AssertNoPacket();
            if (gate == "generation") h.Current = false;
            if (gate == "epoch") { h.Views.Begin(); h.Views.Ready(); }
            if (gate == "closed") h.Session.OnDisconnected(new IPEndPoint(IPAddress.Loopback, 1));
            h.Delayed[0]();
            if (gate == "live") h.AssertResponse(false); else h.AssertNoPacket();
            h.AssertPending(false);
            h.AssertVisible(false);
            h.AssertMembership();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SendIntentException_PropagatesAndLeavesInviteAndPopup(bool accept)
        {
            using var h = new PartyPopupFixture();
            h.Ready();
            h.Invite();
            UnityClientSession.SimulatedLatencyMs = 100;
            var failure = new InvalidOperationException("contract scheduling failure");
            h.BeforeSchedule = () => throw failure;
            Assert.AreSame(failure, Assert.Throws<InvalidOperationException>(() => h.Click(accept)));
            Assert.IsEmpty(h.Delayed);
            h.AssertPending(true);
            h.AssertVisible(true);
            h.AssertMembership();
            h.AssertNoPacket();
        }

        [Test]
        public void ServerPartyUpdate_HidesPopupAndRemainsMembershipAuthority()
        {
            using var h = new PartyPopupFixture();
            h.Invite();
            h.AssertVisible(true);
            h.State.ApplyUpdate(90, 7, 7, 9, 1, 0);
            h.AssertVisible(false);
            Assert.AreEqual(90, h.State.PartyId);
            Assert.AreEqual(9, h.State.Member1EntityId);
            h.AssertPending(true);
            h.AssertNoPacket();
        }
    }
}
