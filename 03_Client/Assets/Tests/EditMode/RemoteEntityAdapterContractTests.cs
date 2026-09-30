using System;
using System.Linq;
using System.Reflection;
using Dawnholder.Client.Combat;
using Dawnholder.Client.State;
using NUnit.Framework;
using Shared.GameData;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Dawnholder.Client.Tests
{
    public class RemoteEntityAdapterContractTests
    {
        GameObject _object;
        RemoteEntity _entity;

        [SetUp]
        public void SetUp()
        {
            _object = new GameObject("S4 adapter contract");
            _entity = _object.AddComponent<RemoteEntity>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_object);

        [Test]
        public void Initialize_SetsIdentityAndTransformImmediately_AndDropsOldSnapshots()
        {
            _entity.EnqueueSnapshot(20, 90f, 80f);
            _entity.Initialize(73, -4f, 8f);
            Assert.AreEqual(73, _entity.EntityId);
            Assert.AreEqual(new Vector3(-4f, 8f, 0f), _object.transform.position);
            Frame(_entity);
            Assert.AreEqual(new Vector3(-4f, 8f, 0f), _object.transform.position);
        }

        [Test]
        public void EmptyUpdateAndSnap_PreserveCurrentTransform_UntilFirstNewSnapshot()
        {
            _entity.Initialize(1, 3f, 4f);
            _entity.EnqueueSnapshot(20, 10f, 20f);
            Frame(_entity);
            _entity.SnapInterpolation();
            _object.transform.position = new Vector3(7f, 8f, 9f);
            Frame(_entity);
            Assert.AreEqual(new Vector3(7f, 8f, 9f), _object.transform.position);
            _entity.EnqueueSnapshot(0, 0f, 0f);
            _entity.EnqueueSnapshot(20, 20f, -40f);
            Frame(_entity);
            AssertPosition(_object.transform.position, 17f, -34f);
        }

        [Test]
        public void FirstFrame_AppliesDelayedInterpolation_AndForcesZToZero()
        {
            _object.transform.position = new Vector3(100f, 200f, 7f);
            _entity.EnqueueSnapshot(0, 0f, 0f);
            _entity.EnqueueSnapshot(10, 2f, 10f);
            _entity.EnqueueSnapshot(20, 12f, -10f);
            Assert.AreEqual(new Vector3(100f, 200f, 7f), _object.transform.position);
            Frame(_entity);
            AssertPosition(_object.transform.position, 9f, -4f);
        }

        [TestCase("clear")]
        [TestCase("snap")]
        [TestCase("initialize")]
        public void ResetOperations_PreservePendingCallback_AndCallbackSeesEnqueuedData(string operation)
        {
            _entity.Initialize(1, 3f, 4f);
            int calls = 0;
            Vector3 positionAtCallback = default;
            _entity.SetTeleportArriveCallback(() =>
            {
                calls++;
                positionAtCallback = _object.transform.position;
                Frame(_entity); // Must already see the new snapshot, before the next Unity frame.
                Assert.AreEqual(new Vector3(30f, 40f, 0f), _object.transform.position);
                _entity.EnqueueSnapshot(21, 30f, 40f); // Callback is cleared before invocation.
            });
            if (operation == "clear") _entity.ClearBuffer();
            else if (operation == "snap") _entity.SnapInterpolation();
            else _entity.Initialize(2, 3f, 4f);

            _entity.EnqueueSnapshot(20, 30f, 40f);

            Assert.AreEqual(1, calls);
            Assert.AreEqual(new Vector3(3f, 4f, 0f), positionAtCallback);
            _entity.EnqueueSnapshot(22, 30f, 40f);
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Callback_WithoutFrame_DoesNotMoveTransform_AndCanScheduleNextCallback()
        {
            _entity.Initialize(1, 1f, 2f);
            int first = 0, next = 0;
            _entity.SetTeleportArriveCallback(() =>
            {
                first++;
                _entity.SetTeleportArriveCallback(() => next++);
            });
            _entity.EnqueueSnapshot(20, 30f, 40f);
            Assert.AreEqual(new Vector3(1f, 2f, 0f), _object.transform.position);
            Assert.AreEqual(1, first);
            Assert.AreEqual(0, next);
            _entity.EnqueueSnapshot(21, 30f, 40f);
            _entity.EnqueueSnapshot(22, 30f, 40f);
            Assert.AreEqual(1, next);
        }

        [Test]
        public void Callback_ReplacementCancellationAndThrowingRemainOneShot()
        {
            int replaced = 0, replacement = 0;
            _entity.SetTeleportArriveCallback(() => replaced++);
            _entity.SetTeleportArriveCallback(() => replacement++);
            _entity.EnqueueSnapshot(20, 3f, 4f);
            Assert.AreEqual(0, replaced);
            Assert.AreEqual(1, replacement);
            _entity.SetTeleportArriveCallback(() => replaced++);
            _entity.SetTeleportArriveCallback(null);
            _entity.EnqueueSnapshot(21, 3f, 4f);
            Assert.AreEqual(0, replaced);
            var error = new InvalidOperationException("callback marker");
            _entity.SetTeleportArriveCallback(() => throw error);
            Assert.AreSame(error, Assert.Throws<InvalidOperationException>(() => _entity.EnqueueSnapshot(22, 3f, 4f)));
            Assert.DoesNotThrow(() => _entity.EnqueueSnapshot(23, 3f, 4f));
            Frame(_entity);
            Assert.AreEqual(new Vector3(3f, 4f, 0f), _object.transform.position);
        }

        [Test]
        public void PlayerRegistry_UsesRemoteEntityForSnapshotCoordinates()
        {
            Assert.IsNull(RemoteEntityRegistry.Instance);
            var registryObject = new GameObject("S4 player registry");
            GameObject spawned = null;
            try
            {
                var registry = registryObject.AddComponent<RemoteEntityRegistry>();
                registry.SetRemotePlayerPrefab(Resources.Load<GameObject>("RemotePlayer"));
                registry.Spawn(123456, 1f, 2f, null);
                RemoteEntity remote = registry.Entities.Single();
                spawned = remote.gameObject;
                registry.UpdateSnapshot(123456, 20, 13.25f, -2.5f, 0f, (byte)AnimState.Idle);
                Assert.AreEqual(new Vector3(1f, 2f, 0f), remote.transform.position);
                Frame(remote);
                Assert.AreEqual(new Vector3(13.25f, -2.5f, 0f), remote.transform.position);
            }
            finally
            {
                if (spawned != null) Object.DestroyImmediate(spawned);
                Object.DestroyImmediate(registryObject);
            }
        }

        [TestCase(EnemyKind.Normal)]
        [TestCase(EnemyKind.Golem)]
        [TestCase(EnemyKind.Boss)]
        public void EnemyRegistry_AddsVisualFootOffsetBeforeSharedInterpolation(EnemyKind kind)
        {
            Assert.IsNull(EnemyRegistry.Instance);
            var registryObject = new GameObject("S4 enemy registry");
            GameObject spawned = null;
            try
            {
                var registry = registryObject.AddComponent<EnemyRegistry>();
                registry.Spawn(123457, (byte)kind, 1f, 2f, 30, 30);
                Transform transform = registry.EnemyTransforms.Single().transform;
                spawned = transform.gameObject;
                RemoteEnemy enemy = spawned.GetComponent<RemoteEnemy>();
                RemoteEntity remote = spawned.GetComponent<RemoteEntity>();
                float offset = enemy.VisualFootOffset;
                Assert.AreEqual(new Vector3(1f, 2f + offset, 0f), transform.position);
                registry.UpdatePosition(123457, 20, -3.5f, 6.25f, (byte)AnimState.Idle);
                Frame(remote);
                Assert.AreEqual(new Vector3(-3.5f, 6.25f + offset, 0f), transform.position);
            }
            finally
            {
                if (spawned != null) Object.DestroyImmediate(spawned);
                Object.DestroyImmediate(registryObject);
            }
        }

        static void Frame(RemoteEntity entity)
        {
            // Invoke Unity's adapter callback; do not inspect private clock/buffer or the new state type.
            typeof(RemoteEntity).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(entity, null);
        }

        static void AssertPosition(Vector3 position, float x, float y)
        {
            Assert.That(position.x, Is.EqualTo(x).Within(0.0001f));
            Assert.That(position.y, Is.EqualTo(y).Within(0.0001f));
            Assert.AreEqual(0f, position.z);
        }
    }
}
