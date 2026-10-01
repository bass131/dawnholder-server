using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Network;
using Dawnholder.Client.Prediction;
using Dawnholder.Client.Rendering;
using Dawnholder.Client.State;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Dawnholder.Client.Tests
{
    // B 시범 회귀(2026-10-01): 컴포넌트 확보 4곳을 실제 진입점으로 실행한다.
    // ProjectileSpawner.Spawn, S_ProjectileLaunch·S_EnemyAttack 수신(세션 main queue 경유),
    // RemoteEntityRegistry.Spawn/UpdateSnapshot. 결과 GameObject의 컴포넌트 수·인스턴스·후속 상태만 확인하며
    // 확보 구문 자체를 복제하지 않는다.
    internal static class ComponentPilotTools
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        internal static HashSet<GameObject> SceneRoots() => new(SceneManager.GetActiveScene().GetRootGameObjects());

        internal static List<GameObject> NewRoots(HashSet<GameObject> before, string name = null) =>
            SceneManager.GetActiveScene().GetRootGameObjects()
                .Where(go => !before.Contains(go) && (name == null || go.name == name)).ToList();

        internal static void DestroyNewRoots(HashSet<GameObject> before)
        {
            List<GameObject> created = NewRoots(before);
            for (int i = created.Count - 1; i >= 0; i--)
                if (created[i] != null) Object.DestroyImmediate(created[i]);
        }

        internal static T Read<T>(object target, string field)
        {
            FieldInfo info = target.GetType().GetField(field, Private);
            Assert.IsNotNull(info, $"{target.GetType().Name}.{field} not found");
            return (T)info.GetValue(target);
        }

        internal static void Write(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, Private);
            Assert.IsNotNull(info, $"{target.GetType().Name}.{field} not found");
            info.SetValue(target, value);
        }

        internal static void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.0001f));
        }
    }

    // 실제 세션 + 기존 EntryBindingFixture(로컬 플레이어 id 7, Remote/Enemy registry 싱글턴)를
    // 진입 장벽(Ready)까지 통과시킨 뒤 패킷을 OnRecvPacket으로 넣는다. 적용은 Queue.Drain() 때만 일어난다.
    internal sealed class PilotSessionFixture : IDisposable
    {
        readonly HashSet<GameObject> _beforeFixture;
        readonly HashSet<GameObject> _beforeTest;
        internal readonly ManualConnectionQueue Queue = new();
        internal readonly UnityClientSession Session;
        internal readonly EntryBindingFixture Views;

        internal PilotSessionFixture()
        {
            Assert.IsNull(UnityClientSession.Instance);
            _beforeFixture = ComponentPilotTools.SceneRoots();
            Session = new UnityClientSession(() => true, Queue.Post, (action, _) => { });
            Session.Publish();
            SessionTestTools.Handshake(Session, Queue);
            Views = new EntryBindingFixture(Session);
            Views.Begin(x: 2, y: 3);
            Views.Ready();
            _beforeTest = ComponentPilotTools.SceneRoots();
        }

        internal HashSet<GameObject> Roots() => ComponentPilotTools.SceneRoots();

        public void Dispose()
        {
            // 테스트가 만든 투사체·이펙트·원격/적 엔티티를 registry보다 먼저 지운다(편집 모드 Destroy 회피).
            ComponentPilotTools.DestroyNewRoots(_beforeTest);
            Session.Cleanup();
            Views.Dispose();
            ComponentPilotTools.DestroyNewRoots(_beforeFixture);
        }
    }

    public sealed class ProjectileSpawnerComponentTests
    {
        HashSet<GameObject> _before;
        GameObject _template;

        [SetUp]
        public void SetUp()
        {
            _before = ComponentPilotTools.SceneRoots();
            _template = new GameObject("B pilot projectile template");
        }

        [TearDown]
        public void TearDown() => ComponentPilotTools.DestroyNewRoots(_before);

        [Test]
        public void TemplateWithoutVisual_AddsOneVisual_AndLaunchesTowardTarget()
        {
            var shooter = new GameObject("B pilot shooter");
            shooter.transform.position = new Vector3(1f, 2f, 0f);
            var target = new GameObject("B pilot target");
            target.transform.position = new Vector3(4f, 6f, 0f);

            GameObject projectile = SpawnOne(shooter.transform, target.transform, -1);

            ProjectileVisual[] visuals = projectile.GetComponents<ProjectileVisual>();
            Assert.AreEqual(1, visuals.Length, "missing visual must be added exactly once");
            Assert.AreEqual(0, _template.GetComponents<ProjectileVisual>().Length, "template must stay untouched");
            Assert.AreEqual(new Vector3(1f, 2f, 0f), projectile.transform.position);
            Assert.AreSame(target.transform, ComponentPilotTools.Read<Transform>(visuals[0], "_target"));
            ComponentPilotTools.AssertVector(new Vector3(0.6f, 0.8f, 0f), ComponentPilotTools.Read<Vector3>(visuals[0], "_direction"));
        }

        [Test]
        public void TemplateWithVisual_LaunchesInstantiatedInstance_WithoutDuplicate()
        {
            ProjectileVisual authored = _template.AddComponent<ProjectileVisual>();
            ComponentPilotTools.Write(authored, "_speed", 37f);
            var target = new GameObject("B pilot target");
            target.transform.position = new Vector3(3f, 0f, 0f);

            GameObject projectile = SpawnOne(null, target.transform, 1);

            ProjectileVisual[] visuals = projectile.GetComponents<ProjectileVisual>();
            Assert.AreEqual(1, visuals.Length, "existing visual must be reused, not duplicated");
            Assert.AreEqual(37f, ComponentPilotTools.Read<float>(visuals[0], "_speed"), "serialized prefab value must survive");
            Assert.AreSame(target.transform, ComponentPilotTools.Read<Transform>(visuals[0], "_target"));
            Assert.AreEqual(new Vector3(3f, 0f, 0f), projectile.transform.position, "no spawnRoot falls back to target position");
        }

        [TestCase(false, 1, 1f)]
        [TestCase(false, -1, -1f)]
        [TestCase(false, 0, 1f)]
        [TestCase(true, -1, -1f)]
        public void NoTarget_LaunchesStraightInFacingDirection(bool templateHasVisual, int facing, float expectedX)
        {
            if (templateHasVisual) _template.AddComponent<ProjectileVisual>();
            var shooter = new GameObject("B pilot shooter");
            shooter.transform.position = new Vector3(-2f, 5f, 0f);

            GameObject projectile = SpawnOne(shooter.transform, null, facing);

            ProjectileVisual[] visuals = projectile.GetComponents<ProjectileVisual>();
            Assert.AreEqual(1, visuals.Length);
            Assert.IsTrue(ReferenceEquals(ComponentPilotTools.Read<Transform>(visuals[0], "_target"), null));
            ComponentPilotTools.AssertVector(new Vector3(expectedX, 0f, 0f), ComponentPilotTools.Read<Vector3>(visuals[0], "_direction"));
            Assert.AreEqual(new Vector3(-2f, 5f, 0f), projectile.transform.position);
        }

        [Test]
        public void NullPrefab_SpawnsNothing()
        {
            HashSet<GameObject> beforeSpawn = ComponentPilotTools.SceneRoots();
            Assert.DoesNotThrow(() => ProjectileSpawner.Spawn(null, null, null, 1));
            Assert.IsEmpty(ComponentPilotTools.NewRoots(beforeSpawn));
        }

        GameObject SpawnOne(Transform spawnRoot, Transform target, int facing)
        {
            HashSet<GameObject> beforeSpawn = ComponentPilotTools.SceneRoots();
            ProjectileSpawner.Spawn(_template, spawnRoot, target, facing);
            List<GameObject> spawned = ComponentPilotTools.NewRoots(beforeSpawn);
            Assert.AreEqual(1, spawned.Count, "Spawn must create exactly one projectile root");
            return spawned[0];
        }
    }

    public sealed class ProjectileLaunchHandlerComponentTests
    {
        const int LocalId = 7;
        const int EnemyId = 5001;
        const int RemoteId = 4001;
        PilotSessionFixture _fx;

        [SetUp]
        public void SetUp() => _fx = new PilotSessionFixture();

        [TearDown]
        public void TearDown() => _fx?.Dispose();

        [Test]
        public void LocalLaunchWithTarget_UsesPrefabVisualOnMainQueue_AndAppliesTravelDuration()
        {
            _fx.Views.Enemies.Spawn(EnemyId, (byte)EnemyKind.Normal, 8f, 3f, 30, 30);
            Assert.IsTrue(_fx.Views.Enemies.TryGetTransform(EnemyId, out Transform target));
            HashSet<GameObject> before = _fx.Roots();

            _fx.Session.OnRecvPacket(new S_ProjectileLaunch { attackerEntityId = LocalId, targetEntityId = EnemyId, travelTicks = 4 }.Write());
            Assert.IsEmpty(ComponentPilotTools.NewRoots(before, "Projectile(Clone)"), "handler must not touch Unity objects before main-queue apply");
            _fx.Queue.Drain();

            GameObject projectile = ComponentPilotTools.NewRoots(before, "Projectile(Clone)").Single();
            ProjectileVisual[] visuals = projectile.GetComponents<ProjectileVisual>();
            Assert.AreEqual(1, visuals.Length, "prefab-authored visual must be reused without duplicate");
            Vector3 spawnPos = _fx.Views.Player.transform.position;
            Assert.AreEqual(spawnPos, projectile.transform.position);
            Assert.AreSame(target, ComponentPilotTools.Read<Transform>(visuals[0], "_target"));
            float duration = 4 * Constants.TickDuration;
            Assert.AreEqual(Vector3.Distance(spawnPos, target.position) / duration, ComponentPilotTools.Read<float>(visuals[0], "_speed"), 0.0001f);
            Assert.AreEqual(duration + 0.2f, ComponentPilotTools.Read<float>(visuals[0], "_maxLifetime"), 0.0001f);
        }

        [Test]
        public void LocalLaunchWithoutTarget_LaunchesStraight_AndKeepsPrefabTiming()
        {
            HashSet<GameObject> before = _fx.Roots();
            _fx.Session.OnRecvPacket(new S_ProjectileLaunch { attackerEntityId = LocalId, targetEntityId = 0, travelTicks = 4 }.Write());
            _fx.Queue.Drain();

            GameObject projectile = ComponentPilotTools.NewRoots(before, "Projectile(Clone)").Single();
            ProjectileVisual[] visuals = projectile.GetComponents<ProjectileVisual>();
            Assert.AreEqual(1, visuals.Length);
            GameObject prefab = Resources.Load<GameObject>("Effects/Projectile");
            float authoredSpeed = ComponentPilotTools.Read<float>(prefab.GetComponent<ProjectileVisual>(), "_speed");
            Assert.AreEqual(authoredSpeed, ComponentPilotTools.Read<float>(visuals[0], "_speed"), "no target: travel duration must not be applied");
            Assert.IsTrue(ReferenceEquals(ComponentPilotTools.Read<Transform>(visuals[0], "_target"), null));
            ComponentPilotTools.AssertVector(Vector3.right, ComponentPilotTools.Read<Vector3>(visuals[0], "_direction"));
        }

        [Test]
        public void RemoteLaunches_EachProjectileHasOneVisual_AtAttackerAnchor()
        {
            _fx.Views.Remote.SetRemotePlayerPrefab(Resources.Load<GameObject>("RemotePlayer"));
            _fx.Views.Remote.Spawn(RemoteId, -1f, 3f, CharacterClass.Mage);
            Assert.IsTrue(_fx.Views.Remote.TryGetTransform(RemoteId, out Transform attacker));
            HashSet<GameObject> before = _fx.Roots();

            for (int i = 0; i < 2; i++)
                _fx.Session.OnRecvPacket(new S_ProjectileLaunch { attackerEntityId = RemoteId, targetEntityId = 0, travelTicks = 0 }.Write());
            _fx.Queue.Drain();

            List<GameObject> projectiles = ComponentPilotTools.NewRoots(before, "RemoteProjectile(Clone)");
            Assert.AreEqual(2, projectiles.Count);
            Vector3 anchor = EffectAnchor.ResolvePosition(attacker);
            foreach (GameObject projectile in projectiles)
            {
                ProjectileVisual[] visuals = projectile.GetComponents<ProjectileVisual>();
                Assert.AreEqual(1, visuals.Length);
                Assert.AreEqual(anchor, projectile.transform.position);
                ComponentPilotTools.AssertVector(Vector3.right, ComponentPilotTools.Read<Vector3>(visuals[0], "_direction"));
            }
        }

        [Test]
        public void RemoteLaunchFromUnknownAttacker_SpawnsNothing()
        {
            HashSet<GameObject> before = _fx.Roots();
            _fx.Session.OnRecvPacket(new S_ProjectileLaunch { attackerEntityId = RemoteId + 99, targetEntityId = 0, travelTicks = 0 }.Write());
            _fx.Queue.Drain();
            Assert.IsEmpty(ComponentPilotTools.NewRoots(before));
        }
    }

    public sealed class EnemyAttackHandlerComponentTests
    {
        const int LocalId = 7;
        const int AttackerId = 5101;
        PilotSessionFixture _fx;
        GameObject _player;
        SpriteRenderer _sprite;

        [SetUp]
        public void SetUp()
        {
            _fx = new PilotSessionFixture();
            _player = _fx.Views.Player.gameObject;
            // 실제 LocalPlayer처럼 SpriteRenderer는 "Visual" 자식에 있다. Flash의 자식 탐색을 위해 활성화한다.
            var visual = new GameObject("Visual");
            visual.transform.SetParent(_player.transform, false);
            _sprite = visual.AddComponent<SpriteRenderer>();
            _sprite.color = Color.white;
            _player.SetActive(true);
        }

        [TearDown]
        public void TearDown() => _fx?.Dispose();

        [Test]
        public void LocalHit_WithoutDamageFlash_AddsOneOnMainQueue_AndFlashes()
        {
            Assert.AreEqual(0, _player.GetComponents<DamageFlash>().Length);
            Assert.AreEqual(0f, LocalLockRemaining());

            Hit(LocalId);
            Assert.AreEqual(0, _player.GetComponents<DamageFlash>().Length, "handler must not touch Unity objects before main-queue apply");
            Assert.AreEqual(Color.white, _sprite.color);
            _fx.Queue.Drain();

            Assert.AreEqual(1, _player.GetComponents<DamageFlash>().Length, "missing flash must be added exactly once");
            Assert.AreEqual(Color.red, _sprite.color, "Flash must run on the added component");
            Assert.Greater(LocalLockRemaining(), 0f, "NotifyHit must still run for the local hit");
        }

        [Test]
        public void LocalHitTwice_ReusesTheSameDamageFlash_AndFlashesAgain()
        {
            Hit(LocalId);
            _fx.Queue.Drain();
            DamageFlash first = _player.GetComponent<DamageFlash>();
            Assert.IsTrue(first != null);
            _sprite.color = Color.white;

            Hit(LocalId);
            _fx.Queue.Drain();

            Assert.AreEqual(1, _player.GetComponents<DamageFlash>().Length);
            Assert.AreSame(first, _player.GetComponent<DamageFlash>());
            Assert.AreEqual(Color.red, _sprite.color);
        }

        [Test]
        public void LocalHit_WithAuthoredDamageFlash_ReusesIt()
        {
            DamageFlash authored = _player.AddComponent<DamageFlash>();
            ComponentPilotTools.Write(authored, "_flashDuration", 0.5f);

            Hit(LocalId);
            _fx.Queue.Drain();

            Assert.AreEqual(1, _player.GetComponents<DamageFlash>().Length);
            Assert.AreSame(authored, _player.GetComponent<DamageFlash>());
            Assert.AreEqual(0.5f, ComponentPilotTools.Read<float>(authored, "_flashDuration"));
            Assert.AreEqual(Color.red, _sprite.color);
        }

        [Test]
        public void HitOnOtherTarget_LeavesLocalPlayerWithoutFlash()
        {
            Hit(LocalId + 1000);
            _fx.Queue.Drain();

            Assert.AreEqual(0, _player.GetComponents<DamageFlash>().Length);
            Assert.AreEqual(Color.white, _sprite.color);
            Assert.AreEqual(0f, LocalLockRemaining());
        }

        [Test]
        public void LocalHit_WithoutLocalPlayerInstance_SkipsFlashWithoutError()
        {
            typeof(LocalPlayerMovement).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { null });

            Hit(LocalId);
            Assert.DoesNotThrow(() => _fx.Queue.Drain());

            Assert.AreEqual(0, _player.GetComponents<DamageFlash>().Length);
            Assert.AreEqual(Color.white, _sprite.color);
        }

        void Hit(int targetId) =>
            _fx.Session.OnRecvPacket(new S_EnemyAttack { attackerId = AttackerId, targetId = targetId, attackPattern = 0 }.Write());

        float LocalLockRemaining()
        {
            object timers = ComponentPilotTools.Read<object>(_fx.Views.Player, "_timers");
            return (float)timers.GetType().GetProperty("LocalLockRemaining").GetValue(timers);
        }
    }

    public sealed class RemoteEntityRegistryMotionComponentTests
    {
        const int EntityId = 4101;
        HashSet<GameObject> _before;
        RemoteEntityRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            Assert.IsNull(RemoteEntityRegistry.Instance);
            Assert.IsNull(UnityClientSession.Instance);
            _before = ComponentPilotTools.SceneRoots();
            _registry = new GameObject("B pilot remote registry").AddComponent<RemoteEntityRegistry>();
        }

        [TearDown]
        public void TearDown()
        {
            // registry 정리는 Destroy를 쓰므로 엔티티를 먼저 즉시 파괴한다.
            foreach (RemoteEntity entity in _registry.Entities.ToList())
                if (entity != null) Object.DestroyImmediate(entity.gameObject);
            ComponentPilotTools.DestroyNewRoots(_before);
        }

        [Test]
        public void Spawn_TemplateWithoutMotion_AddsOneMotionBeforeDriver_AndRegistersIt()
        {
            _registry.SetRemotePlayerPrefab(Template(false));
            _registry.Spawn(EntityId, 1f, 2f, CharacterClass.Knight);

            GameObject go = _registry.Entities.Single().gameObject;
            Assert.AreEqual(EntityId, go.GetComponent<RemoteEntity>().EntityId);
            Assert.AreEqual(new Vector3(1f, 2f, 0f), go.transform.position);
            Assert.AreEqual(1, go.GetComponents<RemotePlayerMotion>().Length);
            Assert.AreEqual(1, go.GetComponents<AnimatorDriver>().Length);
            List<Component> order = go.GetComponents<Component>().ToList();
            Assert.Less(order.IndexOf(go.GetComponent<RemotePlayerMotion>()), order.IndexOf(go.GetComponent<AnimatorDriver>()),
                "motion must exist before the driver is added");

            // 비주얼 장착은 driver 준비 후: Attach가 호출한 Rebind가 Visual 자식의 renderer를 잡아야 한다.
            Transform visual = go.transform.Find(ClassVisualMount.ChildName);
            Assert.IsTrue(visual != null);
            Assert.AreSame(visual.GetComponentInChildren<SpriteRenderer>(),
                ComponentPilotTools.Read<SpriteRenderer>(go.GetComponent<AnimatorDriver>(), "_sr"));

            AssertRegisteredMotionDrivesFollowUps(go);
        }

        [Test]
        public void Spawn_TemplateWithMotion_ReusesItWithoutDuplicate()
        {
            GameObject template = Template(true);
            _registry.SetRemotePlayerPrefab(template);
            _registry.Spawn(EntityId, 1f, 2f, CharacterClass.Mage);

            GameObject go = _registry.Entities.Single().gameObject;
            Assert.AreEqual(1, go.GetComponents<RemotePlayerMotion>().Length, "existing motion must be reused");
            Assert.AreEqual(1, template.GetComponents<RemotePlayerMotion>().Length, "template must stay untouched");
            AssertRegisteredMotionDrivesFollowUps(go);
        }

        [Test]
        public void Spawn_ShippedRemotePlayerPrefab_GetsExactlyOneMotionAndDriver()
        {
            GameObject shipped = Resources.Load<GameObject>("RemotePlayer");
            int shippedMotions = shipped.GetComponents<RemotePlayerMotion>().Length;
            _registry.SetRemotePlayerPrefab(shipped);
            _registry.Spawn(EntityId, 0f, 0f, CharacterClass.Knight);

            GameObject go = _registry.Entities.Single().gameObject;
            TestContext.Out.WriteLine($"[B-REG] shipped RemotePlayer prefab RemotePlayerMotion count={shippedMotions}");
            Assert.AreEqual(1, go.GetComponents<RemotePlayerMotion>().Length);
            Assert.AreEqual(1, go.GetComponents<AnimatorDriver>().Length);
            AssertRegisteredMotionDrivesFollowUps(go);
        }

        [Test]
        public void SpawnSameEntityTwice_KeepsSingleEntityAndMotion()
        {
            _registry.SetRemotePlayerPrefab(Template(false));
            _registry.Spawn(EntityId, 1f, 2f, CharacterClass.Knight);
            _registry.Spawn(EntityId, 9f, 9f, CharacterClass.Knight);

            GameObject go = _registry.Entities.Single().gameObject;
            Assert.AreEqual(1, go.GetComponents<RemotePlayerMotion>().Length);
            Assert.AreEqual(new Vector3(1f, 2f, 0f), go.transform.position, "same-class respawn is a noop");
        }

        [Test]
        public void SnapshotBeforeSpawn_LazySpawnAppliesSnapshotToNewMotion()
        {
            _registry.SetRemotePlayerPrefab(Template(false));
            _registry.UpdateSnapshot(EntityId, 20, 5f, 6f, -4f, (byte)AnimState.Walk);

            GameObject go = _registry.Entities.Single().gameObject;
            RemotePlayerMotion motion = go.GetComponent<RemotePlayerMotion>();
            Assert.AreEqual(1, go.GetComponents<RemotePlayerMotion>().Length);
            Assert.AreEqual(-1, motion.Facing, "the first snapshot must reach the motion created by lazy spawn");
            Assert.AreEqual(AnimState.Walk, motion.CurrentAnimState);
        }

        // registry에 등록된 motion이 GameObject에 붙은 그 컴포넌트인지 후속 API로 확인한다.
        void AssertRegisteredMotionDrivesFollowUps(GameObject go)
        {
            RemotePlayerMotion attached = go.GetComponent<RemotePlayerMotion>();
            _registry.UpdateSnapshot(EntityId, 20, go.transform.position.x, go.transform.position.y, -3f, (byte)AnimState.Walk);
            Assert.IsTrue(_registry.TryGetFacing(EntityId, out int facing));
            Assert.AreEqual(-1, facing);
            Assert.AreEqual(-1, attached.Facing);
            Assert.AreEqual(AnimState.Walk, attached.CurrentAnimState);

            _registry.UpdateSnapshot(EntityId, 21, go.transform.position.x, go.transform.position.y, 3f, (byte)AnimState.Walk);
            Assert.AreEqual(1, attached.Facing);
            _registry.SetChanneling(EntityId, 1f);
            Assert.AreEqual(AnimState.Channeling, attached.CurrentAnimState);
        }

        GameObject Template(bool withMotion)
        {
            var template = new GameObject("B pilot remote template");
            template.AddComponent<RemoteEntity>();
            if (withMotion) template.AddComponent<RemotePlayerMotion>();
            return template;
        }
    }
}
