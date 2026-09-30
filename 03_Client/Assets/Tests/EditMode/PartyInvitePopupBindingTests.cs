using System;
using System.Reflection;
using Dawnholder.Client.State;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Dawnholder.Client.Tests
{
    public sealed class PartyInvitePopupBindingTests
    {
        static int Listeners(PartyState state, string name) =>
            ((Delegate)typeof(PartyState).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(state))?.GetInvocationList().Length ?? 0;

        [TestCase("OnDisable")]
        [TestCase("OnDestroy")]
        public void SourceDestroyedFirst_ReleasesManagedEventSubscriptions(string callback)
        {
            using var h = new PartyPopupFixture();
            Assert.AreEqual(1, Listeners(h.State, "OnInviteReceived"));
            Assert.AreEqual(1, Listeners(h.State, "OnPartyUpdated"));
            UnityEngine.Object.DestroyImmediate(h.State.gameObject);
            Assert.IsTrue(h.State == null, "Unity native object has been destroyed");
            Assert.IsFalse(ReferenceEquals(h.State, null), "managed source still exists for event cleanup");
            SessionTestTools.Call(h.Popup, callback);
            Assert.AreEqual(0, Listeners(h.State, "OnInviteReceived"));
            Assert.AreEqual(0, Listeners(h.State, "OnPartyUpdated"));
        }

        [Test]
        public void DisableAndReenable_UnbindsAndRestoresOneSubscriptionWithoutReplay()
        {
            using var h = new PartyPopupFixture();
            Assert.AreEqual(1, Listeners(h.State, "OnInviteReceived"));
            Assert.AreEqual(1, Listeners(h.State, "OnPartyUpdated"));
            SessionTestTools.Call(h.Popup, "OnDisable");
            Assert.AreEqual(0, Listeners(h.State, "OnInviteReceived"));
            Assert.AreEqual(0, Listeners(h.State, "OnPartyUpdated"));
            h.Invite();
            h.AssertVisible(false);
            SessionTestTools.Call(h.Popup, "OnEnable");
            h.AssertVisible(false);
            Assert.AreEqual(1, Listeners(h.State, "OnInviteReceived"));
            Assert.AreEqual(1, Listeners(h.State, "OnPartyUpdated"));
            h.Invite();
            h.AssertVisible(true);
            h.State.ApplyUpdate(31, 71, 71, 72, 0, 1);
            h.AssertVisible(false);
        }

        [Test]
        public void EnableWithoutSource_DoesNotAutomaticallyBindOrReplayWhenSourceAppears()
        {
            using var h = new PartyPopupFixture();
            SessionTestTools.Call(h.Popup, "OnDisable");
            PartyPopupFixture.SetState(null);
            SessionTestTools.Call(h.Popup, "OnEnable");
            PartyPopupFixture.SetState(h.State);
            h.Invite();
            h.AssertVisible(false);
            Assert.AreEqual(0, Listeners(h.State, "OnInviteReceived"));
            SessionTestTools.Call(h.Popup, "OnDisable");
            SessionTestTools.Call(h.Popup, "OnEnable");
            h.AssertVisible(false);
            h.Invite();
            h.AssertVisible(true);
        }

        [Test]
        public void ForcedSourceReplacement_ActiveCallbackKeepsCurrentInstanceLookupPolicy()
        {
            using var h = new PartyPopupFixture();
            var replacement = new GameObject("forced replacement state");
            replacement.SetActive(false);
            var next = replacement.AddComponent<PartyState>();
            try
            {
                // This bypasses PartyState.Awake's duplicate owner guard; it is not a game reproduction.
                PartyPopupFixture.SetState(next);
                next.SetPendingInvite(999, 1);
                h.AssertVisible(false); // no automatic rebind
                h.State.SetPendingInvite(111, 0);
                h.AssertVisible(true);
                Assert.AreEqual("Mage 파티 초대", h.Popup.transform.Find("Panel/InviteText").GetComponent<TMP_Text>().text);
                next.ClearPendingInvite();
                h.State.ApplyUpdate(31, 71, 71, 72, 0, 1);
                h.AssertVisible(false);
                h.State.SetPendingInvite(111, 0);
                h.AssertVisible(false); // old event still reads the current source with no pending invite
            }
            finally
            {
                PartyPopupFixture.SetState(h.State);
                UnityEngine.Object.DestroyImmediate(replacement);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ForcedSourceReplacement_DisableReleasesOriginalSource(bool replaceWithNull)
        {
            using var h = new PartyPopupFixture();
            var replacement = new GameObject("forced unbind replacement");
            replacement.SetActive(false);
            var next = replacement.AddComponent<PartyState>();
            try
            {
                PartyPopupFixture.SetState(replaceWithNull ? null : next);
                SessionTestTools.Call(h.Popup, "OnDisable");
                Assert.AreEqual(0, Listeners(h.State, "OnInviteReceived"));
                Assert.AreEqual(0, Listeners(h.State, "OnPartyUpdated"));
                PartyPopupFixture.SetState(next);
                next.SetPendingInvite(999, 1);
                h.State.SetPendingInvite(111, 0);
                h.AssertVisible(false);
                SessionTestTools.Call(h.Popup, "OnEnable");
                h.AssertVisible(false);
                Assert.AreEqual(1, Listeners(next, "OnInviteReceived"));
                Assert.AreEqual(1, Listeners(next, "OnPartyUpdated"));
                h.State.SetPendingInvite(111, 0);
                h.AssertVisible(false);
                next.SetPendingInvite(999, 1);
                h.AssertVisible(true);
                SessionTestTools.Call(h.Popup, "OnDisable");
                Assert.AreEqual(0, Listeners(next, "OnInviteReceived"));
                Assert.AreEqual(0, Listeners(next, "OnPartyUpdated"));
            }
            finally
            {
                SessionTestTools.Call(h.Popup, "OnDisable");
                PartyPopupFixture.SetState(h.State);
                UnityEngine.Object.DestroyImmediate(replacement);
            }
        }
    }
}
