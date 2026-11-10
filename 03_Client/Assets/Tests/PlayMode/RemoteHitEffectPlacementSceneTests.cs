using System.Collections;
using System.Linq;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Prediction;
using Dawnholder.Client.State;
using NUnit.Framework;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Goal 2026-11-10-code-audit-client-fixes item 9: a non-boss hit effect appears at its victim, local or remote. The
    // effect is the product's Resources prefab, found by its clone name; attacker and victim are kept far apart so the
    // nearer one is unambiguous whatever anchor offset the prefabs carry.
    public sealed class RemoteHitEffectPlacementSceneTests
    {
        const int LocalId = 700;
        const int RemoteId = 702;
        const int EnemyId = 800;
        const byte NormalKind = 0;
        const byte BossKind = 1;
        const float AttackerX = 4;
        const float RemoteX = 18;
        const float FarAttackerX = 14;

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

        IEnumerator SpawnEnemy(byte kind, float x)
        {
            _fixture.Peer.Send(new S_EntitySpawn { entityId = EnemyId, entityKind = kind, x = x, y = 0, currentHp = 100, maxHp = 100 }.Write());
            yield return MapEntryPlayFixture.Wait(() => EnemyRegistry.Instance.TryGetTransform(EnemyId, out _), "enemy spawned", 5);
        }

        IEnumerator SpawnRemote()
        {
            _fixture.Peer.Send(new S_PlayerJoin { entityId = RemoteId, characterClass = 0, spawnX = RemoteX, spawnY = 0 }.Write());
            _fixture.Peer.Send(new S_Snapshot { entityId = RemoteId, serverTick = 2, x = RemoteX, y = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => RemoteEntityRegistry.Instance.TryGetTransform(RemoteId, out Transform remote) &&
                Mathf.Abs(remote.position.x - RemoteX) < 0.01f, "remote player at its snapshot position", 5);
        }

        void SendAttack(int targetId) => _fixture.Peer.Send(new S_EnemyAttack
        {
            attackerId = EnemyId, targetId = targetId, damage = 1, targetCurrentHp = 50, attackPattern = 0,
        }.Write());

        // Captured in the first frame the effect exists; EffectLifetime removes it shortly after.
        Vector3? _effectPosition;
        IEnumerator CaptureEffect(string prefabName)
        {
            _effectPosition = null;
            yield return MapEntryPlayFixture.Wait(() =>
            {
                EffectLifetime effect = UnityEngine.Object.FindObjectsByType<EffectLifetime>()
                    .FirstOrDefault(item => item.name.StartsWith(prefabName));
                if (effect == null) return false;
                _effectPosition = effect.transform.position;
                return true;
            }, "the effect " + prefabName + " appears", 3);
        }

        static float Distance(Vector3 a, Vector3 b) => Vector2.Distance(a, b);

        static void AssertNearerTo(string requirement, Vector3 effect, Vector3 expected, Vector3 other)
        {
            float toExpected = Distance(effect, expected);
            float toOther = Distance(effect, other);
            Assert.Less(toExpected, toOther, $"{requirement}: effect {effect}, expected near {expected}, other {other}");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator EnemyHitsRemotePlayer_EffectAppearsAtTheRemotePlayer()
        {
            yield return EnterReadyTown();
            yield return SpawnEnemy(NormalKind, AttackerX);
            yield return SpawnRemote();
            EnemyRegistry.Instance.TryGetTransform(EnemyId, out Transform attacker);
            RemoteEntityRegistry.Instance.TryGetTransform(RemoteId, out Transform victim);

            SendAttack(RemoteId);
            yield return CaptureEffect("SlimeDamageEffect");
            AssertNearerTo("item 9: the non-boss hit effect appears at its remote victim", _effectPosition.Value, victim.position, attacker.position);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator EnemyHitsRemotePlayer_LeavesLocalHitFeedbackAlone()
        {
            yield return EnterReadyTown();
            yield return SpawnEnemy(NormalKind, AttackerX);
            yield return SpawnRemote();
            LocalPlayerMovement player = _fixture.Player;

            SendAttack(RemoteId);
            yield return CaptureEffect("SlimeDamageEffect");
            bool localFlashAdded = player.TryGetComponent(out DamageFlash _);
            bool localHitBridgeStarted = player.IsActionLocked;
            Assert.IsFalse(localFlashAdded, "item 9: a remote victim adds no local damage flash");
            Assert.IsFalse(localHitBridgeStarted, "item 9: a remote victim starts no local hit bridge");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator EnemyHitsLocalPlayer_EffectAndLocalFeedbackStayLocal()
        {
            yield return EnterReadyTown();
            yield return SpawnEnemy(NormalKind, FarAttackerX);
            LocalPlayerMovement player = _fixture.Player;
            EnemyRegistry.Instance.TryGetTransform(EnemyId, out Transform attacker);

            SendAttack(LocalId);
            yield return CaptureEffect("SlimeDamageEffect");
            AssertNearerTo("item 9: the local victim keeps its effect", _effectPosition.Value, player.transform.position, attacker.position);
            bool localFlashAdded = player.TryGetComponent(out DamageFlash _);
            Assert.IsTrue(localFlashAdded, "item 9: the local victim keeps its damage flash");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator BossHitsRemotePlayer_EffectStaysAtTheBoss()
        {
            yield return EnterReadyTown();
            yield return SpawnEnemy(BossKind, AttackerX);
            yield return SpawnRemote();
            EnemyRegistry.Instance.TryGetTransform(EnemyId, out Transform attacker);
            RemoteEntityRegistry.Instance.TryGetTransform(RemoteId, out Transform victim);

            SendAttack(RemoteId);
            yield return CaptureEffect("BossAttackPattern0");
            AssertNearerTo("item 9: a boss attack keeps its attacker anchor", _effectPosition.Value, attacker.position, victim.position);
        }
    }
}
