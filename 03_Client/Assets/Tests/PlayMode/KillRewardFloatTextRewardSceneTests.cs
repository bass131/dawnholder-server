using System.Collections;
using System.Linq;
using Dawnholder.Client.Combat;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Additional reward combinations through the real TCP entry and the existing black-box text probe.
    public sealed class KillRewardFloatTextRewardSceneTests
    {
        MapEntryPlayFixture _fixture;
        int _hpMarker;
        uint _revision;

        [SetUp]
        public void Setup()
        {
            _fixture = new MapEntryPlayFixture();
            _hpMarker = 40;
        }

        [UnityTearDown] public IEnumerator Teardown() => _fixture.Cleanup();

        [UnityTest]
        public IEnumerator GolemReward_ShowsGoldBeforeCoinPouch_AndASecondKillKeepsBothRewardsVisible()
        {
            yield return EnterWithBaseline();
            yield return Spawn(901, EnemyKind.Golem, 8f);
            yield return Spawn(902, EnemyKind.Normal, 5f);
            Vector3 golemSpot = Kill(901);
            Snapshot(35, 2, 1);
            yield return MapEntryPlayFixture.Wait(() => At(8f).Length > 0, "golem reward text", 3);
            TextMeshPro[] first = At(8f);
            CollectionAssert.AreEqual(new[] { "+금화 10", "+재화 주머니 1" }, KillRewardFloatTextProbe.Lines(first));
            Assert.Greater(KillRewardFloatTextProbe.Center(first).y, golemSpot.y);

            Kill(902);
            Snapshot(45, 3, 1);
            yield return MapEntryPlayFixture.Wait(() => At(5f).Length > 0, "second reward text", 3);
            CollectionAssert.AreEqual(new[] { "+금화 10", "+재료 1" }, KillRewardFloatTextProbe.Lines(At(5f)));
            CollectionAssert.AreEqual(new[] { "+금화 10", "+재화 주머니 1" }, KillRewardFloatTextProbe.Lines(At(8f)),
                "The next kill must not replace the still-visible first reward.");
            Debug.Log("[kill reward independent] two simultaneous rewards: " + KillRewardFloatTextProbe.Describe(KillRewardFloatTextProbe.Find()));
        }

        [UnityTest]
        public IEnumerator FallingGoldAndRisingMaterial_ShowsOneItemLineWithoutAGoldLine()
        {
            yield return EnterWithBaseline();
            yield return Spawn(903, EnemyKind.Normal, 8f);
            Kill(903);
            Snapshot(15, 3, 0);
            yield return MapEntryPlayFixture.Wait(() => KillRewardFloatTextProbe.Find().Length > 0, "item-only reward", 3);
            CollectionAssert.AreEqual(new[] { "+재료 1" }, KillRewardFloatTextProbe.Lines(KillRewardFloatTextProbe.Find()));
            Debug.Log("[kill reward independent] mixed snapshot: " + KillRewardFloatTextProbe.Describe(KillRewardFloatTextProbe.Find()));
        }

        IEnumerator EnterWithBaseline()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp(_hpMarker, 100);
            yield return _fixture.WaitMap(0);
            Snapshot(25, 2, 0);
            // Same-stream HP acknowledgment establishes that the earlier inventory baseline was applied.
            int marker = ++_hpMarker;
            _fixture.Hp(marker, 100);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.CurrentHp == marker, "baseline applied", 3);
            yield return _fixture.Frames(5);
        }

        IEnumerator Spawn(int id, EnemyKind kind, float x)
        {
            _fixture.Peer.Send(new S_EntitySpawn { entityId = id, entityKind = (byte)kind, x = x, y = 0, currentHp = 100, maxHp = 100 }.Write());
            _fixture.Peer.Send(new S_EntityState { entityId = id, serverTick = 1, x = x, y = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => EnemyRegistry.Instance != null &&
                EnemyRegistry.Instance.TryGetTransform(id, out Transform enemy) && Mathf.Abs(enemy.position.x - x) < 0.01f,
                "enemy settled " + id, 5);
        }

        Vector3 Kill(int id)
        {
            Assert.IsTrue(EnemyRegistry.Instance.TryGetTransform(id, out Transform enemy), "death has a live enemy");
            Vector3 spot = enemy.position;
            _fixture.Peer.Send(new S_EntityDeath { entityId = id }.Write());
            return spot;
        }

        void Snapshot(int gold, int material, int coinPouch)
        {
            _fixture.Peer.Send(new S_InventorySnapshot
            {
                revision = ++_revision, currency = gold,
                slot0ItemId = (int)ItemId.Material, slot0Count = material,
                slot1ItemId = coinPouch > 0 ? (int)ItemId.CoinPouch : 0, slot1Count = coinPouch,
            }.Write());
        }

        static TextMeshPro[] At(float x) => KillRewardFloatTextProbe.Find()
            .Where(text => Mathf.Abs(text.transform.position.x - x) < 0.5f).ToArray();
    }
}
