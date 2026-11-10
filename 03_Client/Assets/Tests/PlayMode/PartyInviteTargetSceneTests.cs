using System.Collections;
using Dawnholder.Client.Combat;
using Dawnholder.Client.State;
using NUnit.Framework;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Goal 2026-11-10-code-audit-client-fixes item 3: the P key invites a remote player, never an enemy. Both kinds carry
    // RemoteEntity; the spawn packets below build them through the product's own roster and enemy paths.
    public sealed class PartyInviteTargetSceneTests
    {
        const int RemoteId = 702;
        const int EnemyId = 800;
        const float LocalSpawnX = 2;
        const float NearEnemyX = 3;
        const float FarRemoteX = 15;
        const float InviteCooldownSeconds = 1.2f;
        const float NoPacketObservationSeconds = 0.6f;

        MapEntryPlayFixture _fixture;

        [SetUp]
        public void Setup() => _fixture = new MapEntryPlayFixture();

        [UnityTearDown]
        public IEnumerator Teardown() => _fixture.Cleanup();

        IEnumerator EnterReadyTown()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
        }

        IEnumerator SpawnRemote(float x)
        {
            _fixture.Peer.Send(new S_PlayerJoin { entityId = RemoteId, characterClass = 0, spawnX = x, spawnY = 0 }.Write());
            _fixture.Peer.Send(new S_Snapshot { entityId = RemoteId, serverTick = 2, x = x, y = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => RemoteEntityRegistry.Instance.TryGetTransform(RemoteId, out _),
                "remote player spawned", 5);
        }

        IEnumerator SpawnEnemy(float x)
        {
            _fixture.Peer.Send(new S_EntitySpawn { entityId = EnemyId, entityKind = 0, x = x, y = 0, currentHp = 100, maxHp = 100 }.Write());
            yield return MapEntryPlayFixture.Wait(() => EnemyRegistry.Instance.TryGetTransform(EnemyId, out _), "enemy spawned", 5);
            EnemyRegistry.Instance.TryGetTransform(EnemyId, out Transform enemy);
            bool enemyHasRemoteEntity = enemy.GetComponent<RemoteEntity>() != null;
            bool enemyInPlayerRegistry = RemoteEntityRegistry.Instance.TryGetTransform(EnemyId, out _);
            Assert.IsTrue(enemyHasRemoteEntity, "fixture: the enemy carries RemoteEntity as the defect requires");
            Assert.IsFalse(enemyInPlayerRegistry, "fixture: the enemy is not a registered remote player");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator InviteKey_WithNearerEnemy_TargetsTheRemotePlayer()
        {
            yield return EnterReadyTown();
            yield return SpawnEnemy(NearEnemyX);
            yield return SpawnRemote(FarRemoteX);

            Vector3 origin = _fixture.Player.transform.position;
            EnemyRegistry.Instance.TryGetTransform(EnemyId, out Transform enemy);
            RemoteEntityRegistry.Instance.TryGetTransform(RemoteId, out Transform remote);
            bool enemyIsNearer = (enemy.position - origin).sqrMagnitude < (remote.position - origin).sqrMagnitude;
            Assert.IsTrue(enemyIsNearer, $"fixture: enemy {enemy.position} is nearer than remote {remote.position} from {origin}");

            _fixture.Keys(Key.P);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_PartyInvite) == 1, "invite reaches the stand-in", 3);
            _fixture.Keys();
            int invitedId = _fixture.Peer.Last<C_PartyInvite>(PacketID.C_PartyInvite).targetEntityId;
            Assert.AreEqual(RemoteId, invitedId, "item 3: the invite names the remote player, not the nearer enemy");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator InviteKey_WithOnlyAnEnemy_SendsNothing()
        {
            yield return EnterReadyTown();
            yield return SpawnRemote(LocalSpawnX + 2);
            _fixture.Keys(Key.P);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_PartyInvite) == 1,
                "control: the invite reaches the stand-in while a remote player exists", 3);
            _fixture.Keys();
            int controlTarget = _fixture.Peer.Last<C_PartyInvite>(PacketID.C_PartyInvite).targetEntityId;
            Assert.AreEqual(RemoteId, controlTarget, "control: the only candidate is invited");

            _fixture.Peer.Send(new S_PlayerLeave { entityId = RemoteId }.Write());
            yield return MapEntryPlayFixture.Wait(() => !RemoteEntityRegistry.Instance.TryGetTransform(RemoteId, out _),
                "remote player left", 5);
            yield return SpawnEnemy(NearEnemyX);
            float end = Time.realtimeSinceStartup + InviteCooldownSeconds;
            while (Time.realtimeSinceStartup < end) yield return null;

            _fixture.Keys(Key.P);
            end = Time.realtimeSinceStartup + NoPacketObservationSeconds;
            while (Time.realtimeSinceStartup < end) yield return null;
            _fixture.Keys();
            int invitesToEnemy = _fixture.Peer.Count(PacketID.C_PartyInvite) - 1;
            Assert.AreEqual(0, invitesToEnemy, "item 3: with no remote player the invite is not sent");
        }
    }
}
