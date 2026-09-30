using System;
using System.Collections.Generic;
using System.Reflection;
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests
{
    public sealed class PartyInviteResponseCommandTests
    {
        // Reach the internal feature boundary without exposing a production-only testing API.
        static object Command(Func<PartyState> state, Func<UnityClientSession> session)
        {
            Type type = typeof(PartyInvitePopup).Assembly.GetType("Dawnholder.Client.UI.PartyInviteResponseCommand", true);
            return Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { state, session }, null);
        }
        static void Execute(object command, byte accept) => SessionTestTools.Call(command, "Execute", accept);

        [TestCase("state")]
        [TestCase("pending")]
        [TestCase("session")]
        [TestCase("handshake")]
        public void EarlyReturn_QueriesOnlyProvidersReachedByTheOriginalOrder(string unavailable)
        {
            using var h = new PartyPopupFixture();
            var calls = new List<string>();
            if (unavailable != "pending") h.Invite();
            object command = Command(() =>
            {
                calls.Add("state");
                return unavailable == "state" ? null : h.State;
            }, () =>
            {
                calls.Add("session");
                return unavailable == "session" ? null : h.Session;
            });
            Assert.IsEmpty(calls, "construction must not capture current state/session");
            if (unavailable == "pending")
                LogAssert.Expect(LogType.Warning, "[PartyInvitePopup] HasPendingInvite=false — 응답 취소.");
            if (unavailable == "session" || unavailable == "handshake")
                LogAssert.Expect(LogType.Warning, "[PartyInvitePopup] 세션 없음 또는 Handshake 미완료 — 응답 송신 불가.");
            Execute(command, 1);
            string[] expected = unavailable == "state" ? new[] { "state" } :
                unavailable == "pending" ? new[] { "state", "state" } : new[] { "state", "state", "session" };
            CollectionAssert.AreEqual(expected, calls);
            h.AssertPending(unavailable != "pending");
            h.AssertMembership();
            h.AssertNoPacket();
        }

        [Test]
        public void EachExecute_ResolvesLatestSessionAndConsumesOnlyAfterSchedulingReturns()
        {
            using var h = new PartyPopupFixture();
            h.Ready();
            h.Invite();
            UnityClientSession current = h.Session;
            var calls = new List<string>();
            object command = Command(() => { calls.Add("state"); return h.State; },
                () => { calls.Add("session"); return current; });
            UnityClientSession.SimulatedLatencyMs = 100;
            h.BeforeSchedule = () =>
            {
                calls.Add("schedule");
                h.AssertPending(true);
                h.AssertMembership();
            };
            Execute(command, 1);
            CollectionAssert.AreEqual(new[] { "state", "state", "session", "schedule" }, calls);
            h.AssertPending(false);
            h.AssertVisible(true); // command itself does not own popup hiding
            Assert.AreEqual(1, h.Delayed.Count);
            h.Delayed[0]();
            h.AssertResponse(true);

            h.Invite();
            current = null;
            calls.Clear();
            LogAssert.Expect(LogType.Warning, "[PartyInvitePopup] 세션 없음 또는 Handshake 미완료 — 응답 송신 불가.");
            Execute(command, 0);
            CollectionAssert.AreEqual(new[] { "state", "state", "session" }, calls);
            h.AssertPending(true);
            h.AssertNoPacket();
            h.AssertMembership();
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void ProviderException_PropagatesWithoutConsumingOrCallingLaterProviders(int throwAt)
        {
            using var h = new PartyPopupFixture();
            h.Ready();
            h.Invite();
            var failure = new InvalidOperationException("provider boundary failure");
            int calls = 0;
            object command = Command(() => { if (++calls == throwAt) throw failure; return h.State; },
                () => { if (++calls == throwAt) throw failure; return h.Session; });
            var wrapper = Assert.Throws<TargetInvocationException>(() => Execute(command, 1));
            Assert.AreSame(failure, wrapper.InnerException);
            Assert.AreEqual(throwAt, calls);
            h.AssertPending(true);
            h.AssertVisible(true);
            h.AssertMembership();
            h.AssertNoPacket();
        }
    }
}
