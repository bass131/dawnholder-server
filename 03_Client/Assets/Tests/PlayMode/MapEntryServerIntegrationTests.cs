using System.Collections;
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using NUnit.Framework;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Separate CLI filter. Requires the main's exclusively owned real GameServer
    // at 127.0.0.1:7777; a scripted peer or headless bot cannot satisfy this lane.
    public sealed class MapEntryServerIntegrationTests
    {
        MapEntryPlayFixture _fixture;
        [SetUp] public void Setup() => _fixture = new MapEntryPlayFixture();
        [UnityTearDown] public IEnumerator Teardown() => _fixture.Cleanup();

        [UnityTest]
        public IEnumerator ProductionServer_TownHuntingGroundTown_KeyboardPortalsPreserveEntityHpAndRoster()
        {
            yield return _fixture.Prepare(true);
            yield return _fixture.WaitMap(0);
            UnityClientSession session = _fixture.Session;
            int entityId = session.LocalEntityId.Value;
            Assert.IsTrue(session.Entry.HasHp);
            Assert.Greater(session.Entry.MaxHp, 0);
            Assert.That(session.Entry.CurrentHp, Is.InRange(0, session.Entry.MaxHp));
            yield return _fixture.AssertHud();
            _fixture.Companion = EntryWirePeer.ConnectProduction(7777);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Companion.Count(PacketID.S_EnterMap) > 0, "production roster companion");
            int companionId = _fixture.Companion.Last<S_EnterMap>(PacketID.S_EnterMap).entityId;
            yield return MapEntryPlayFixture.Wait(() => RemoteEntityRegistry.Instance.TryGetTransform(companionId, out _), "Town roster join");
            long epoch = session.Entry.Epoch;
            yield return _fixture.WalkIntoPortal(1, 1);
            Assert.AreSame(session, _fixture.Session);
            Assert.AreEqual(entityId, session.LocalEntityId);
            Assert.Greater(session.Entry.Epoch, epoch);
            Assert.AreEqual(2f, session.Entry.SpawnX);
            Assert.AreEqual(0f, session.Entry.SpawnY);
            Assert.IsFalse(RemoteEntityRegistry.Instance.TryGetTransform(companionId, out _));
            yield return _fixture.AssertHud();
            epoch = session.Entry.Epoch;
            yield return _fixture.WalkIntoPortal(2, 0);
            Assert.AreSame(session, _fixture.Session);
            Assert.AreEqual(entityId, session.LocalEntityId);
            Assert.Greater(session.Entry.Epoch, epoch);
            Assert.AreEqual(17f, session.Entry.SpawnX);
            Assert.AreEqual(0f, session.Entry.SpawnY);
            yield return MapEntryPlayFixture.Wait(() => RemoteEntityRegistry.Instance.TryGetTransform(companionId, out _), "Town roster restored");
            yield return _fixture.AssertHud();
            Assert.IsTrue(session.CanControlPlayer(_fixture.Player));
            Debug.Log($"[M1c Production Unity] Town->HG->Town entity={entityId}, roster={companionId}, hp={session.Entry.CurrentHp}/{session.Entry.MaxHp}, keyboard/physics portals PASS");
        }
    }
}
