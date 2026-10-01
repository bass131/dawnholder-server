using System;
using System.Collections.Generic;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Dawnholder.Client.Tests
{
    // B 시범 특성화(2026-10-01): 컴포넌트가 없는 GameObject에서 GetComponent<T>()가 실제로 무엇을 돌려주는지 기록한다.
    // 결함을 전제하지 않는다. CLR 참조(ReferenceEquals)와 Unity 비교(==)를 함께 "[B-CHAR]" 행으로 남기고,
    // 어느 가설에서도 성립해야 하는 사실만 단언한다. 전제 관찰(GetComponent 반환값)과
    // 원래 `GetComponent ?? AddComponent` 표현식의 실제 결과(부착 여부)를 서로 다른 GameObject에서 따로 기록한다.
    public sealed class ComponentNullCharacterizationTests
    {
        readonly List<GameObject> _objects = new();

        [OneTimeSetUp]
        public void RecordEnvironment()
        {
            Record($"env unityVersion={Application.unityVersion} isEditor={Application.isEditor} " +
                   $"isPlaying={Application.isPlaying} isBatchMode={Application.isBatchMode}");
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
                if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
        }

        // 시범 대상 세 타입. origin: 새 활성 객체, 새 비활성 객체, Instantiate 복제본(투사체 경로와 같은 생성 방식).
        [TestCase("new-active")]
        [TestCase("new-inactive")]
        [TestCase("clone")]
        public void Missing_ProjectileVisual(string origin) => ObserveMissing<ProjectileVisual>(origin);

        [TestCase("new-active")]
        [TestCase("new-inactive")]
        [TestCase("clone")]
        public void Missing_DamageFlash(string origin) => ObserveMissing<DamageFlash>(origin);

        [TestCase("new-active")]
        [TestCase("new-inactive")]
        [TestCase("clone")]
        public void Missing_RemotePlayerMotion(string origin) => ObserveMissing<RemotePlayerMotion>(origin);

        // 대조: 엔진 내장 컴포넌트도 같은 방식으로 기록한다.
        [TestCase("new-active")]
        public void Missing_BuiltIn_Rigidbody2D(string origin) => ObserveMissing<Rigidbody2D>(origin);

        [TestCase("new-active")]
        public void Missing_BuiltIn_SpriteRenderer(string origin) => ObserveMissing<SpriteRenderer>(origin);

        // 대조: 이미 붙은 컴포넌트는 두 표현식 모두 같은 살아 있는 인스턴스를 돌려주고 중복을 만들지 않아야 한다.
        [Test] public void Present_ProjectileVisual() => ObservePresent<ProjectileVisual>();
        [Test] public void Present_DamageFlash() => ObservePresent<DamageFlash>();
        [Test] public void Present_RemotePlayerMotion() => ObservePresent<RemotePlayerMotion>();

        // 측정 도구 대조: 파괴된 컴포넌트 참조는 Unity가 문서화한 "CLR 비null, Unity ==null" 객체다.
        // 이 행이 clrRefNull=False/unityEqNull=True로 찍히면 아래 관찰 방식이 그런 객체를 구분할 수 있다는 뜻이다.
        [Test] public void Destroyed_ProjectileVisual() => ObserveDestroyed<ProjectileVisual>();
        [Test] public void Destroyed_DamageFlash() => ObserveDestroyed<DamageFlash>();
        [Test] public void Destroyed_RemotePlayerMotion() => ObserveDestroyed<RemotePlayerMotion>();

        void ObserveMissing<T>(string origin) where T : Component
        {
            string type = typeof(T).Name;

            // 1) 전제 관찰: 같은 GameObject에서 generic / non-generic / TryGetComponent 결과를 기록.
            GameObject premiseGo = Create($"B-char premise {type}", origin);
            T generic = premiseGo.GetComponent<T>();
            Component nonGeneric = premiseGo.GetComponent(typeof(T));
            bool tryFound = premiseGo.TryGetComponent(out T tryOut);
            Record($"premise type={type} origin={origin} call=GetComponent<T> {Describe(generic)}");
            Record($"premise type={type} origin={origin} call=GetComponent(Type) {Describe(nonGeneric)}");
            Record($"premise type={type} origin={origin} call=TryGetComponent found={tryFound} {Describe(tryOut)}");

            // 2) 원래 표현식의 실제 결과: 별도 GameObject에서 그대로 실행하고 부착 여부를 센다.
            GameObject legacyGo = Create($"B-char legacy {type}", origin);
            int legacyBefore = legacyGo.GetComponents<T>().Length;
            T legacy = legacyGo.GetComponent<T>() ?? legacyGo.AddComponent<T>();
            int legacyAfter = legacyGo.GetComponents<T>().Length;
            T legacyAttached = legacyGo.GetComponent<T>();
            bool legacyIsAttached = !ReferenceEquals(legacy, null) && ReferenceEquals(legacy, legacyAttached);
            Record($"legacy-coalesce type={type} origin={origin} countBefore={legacyBefore} countAfter={legacyAfter} " +
                   $"addComponentRan={legacyAfter > legacyBefore} resultIsAttachedInstance={legacyIsAttached} " +
                   $"{Describe(legacy)} gameObjectAccess={Probe(() => legacy.gameObject.name)}");

            // 3) 수정 후 표현식: TryGetComponent 실패 시 AddComponent.
            GameObject fixedGo = Create($"B-char try {type}", origin);
            if (!fixedGo.TryGetComponent(out T fixedResult))
                fixedResult = fixedGo.AddComponent<T>();
            int fixedAfter = fixedGo.GetComponents<T>().Length;
            Record($"try-add type={type} origin={origin} countAfter={fixedAfter} " +
                   $"resultIsAttachedInstance={ReferenceEquals(fixedResult, fixedGo.GetComponent<T>())} {Describe(fixedResult)}");

            // 가설과 무관하게 성립해야 하는 사실만 단언한다.
            Assert.IsTrue(generic == null, "Unity 비교에서 없는 컴포넌트는 null이어야 한다");
            Assert.IsTrue(nonGeneric == null, "Unity 비교에서 없는 컴포넌트는 null이어야 한다(non-generic)");
            Assert.IsFalse(tryFound, "TryGetComponent는 없는 컴포넌트를 찾았다고 하면 안 된다");
            Assert.AreEqual(0, legacyBefore);
            // `??`는 CLR 참조 기준이므로 AddComponent 실행 여부는 전제 관찰의 CLR null 여부와 같아야 한다.
            Assert.AreEqual(ReferenceEquals(generic, null), legacyAfter == 1,
                "원래 표현식의 부착 여부는 같은 조건의 GetComponent CLR null 여부와 일치해야 한다");
            Assert.AreEqual(1, fixedAfter, "수정 표현식은 정확히 하나를 붙여야 한다");
            Assert.IsTrue(fixedResult != null && ReferenceEquals(fixedResult, fixedGo.GetComponent<T>()));
        }

        void ObservePresent<T>() where T : Component
        {
            string type = typeof(T).Name;
            GameObject go = Create($"B-char present {type}", "new-active");
            T existing = go.AddComponent<T>();

            T legacy = go.GetComponent<T>() ?? go.AddComponent<T>();
            int afterLegacy = go.GetComponents<T>().Length;
            if (!go.TryGetComponent(out T viaTry))
                viaTry = go.AddComponent<T>();
            int afterTry = go.GetComponents<T>().Length;
            Record($"present type={type} legacySame={ReferenceEquals(legacy, existing)} tryFound={ReferenceEquals(viaTry, existing)} " +
                   $"countAfterLegacy={afterLegacy} countAfterTry={afterTry} {Describe(go.GetComponent<T>())}");

            Assert.AreSame(existing, legacy);
            Assert.AreSame(existing, viaTry);
            Assert.AreEqual(1, afterLegacy);
            Assert.AreEqual(1, afterTry);
        }

        void ObserveDestroyed<T>() where T : Component
        {
            string type = typeof(T).Name;
            GameObject go = Create($"B-char destroyed {type}", "new-active");
            T component = go.AddComponent<T>();
            Object.DestroyImmediate(component);
            Record($"destroyed-reference type={type} {Describe(component)}");
            T after = go.GetComponent<T>();
            Record($"after-destroy-get type={type} {Describe(after)}");

            Assert.IsFalse(ReferenceEquals(component, null), "파괴 전 받은 참조 자체는 CLR 객체로 남는다");
            Assert.IsTrue(component == null, "Unity 비교는 파괴된 컴포넌트를 null로 본다");
            Assert.IsTrue(after == null);
        }

        GameObject Create(string name, string origin)
        {
            GameObject go;
            switch (origin)
            {
                case "new-active":
                    go = new GameObject(name);
                    break;
                case "new-inactive":
                    go = new GameObject(name);
                    go.SetActive(false);
                    break;
                case "clone":
                    var template = new GameObject(name + " template");
                    _objects.Add(template);
                    go = Object.Instantiate(template);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
            }
            _objects.Add(go);
            return go;
        }

        static string Describe(Object value)
        {
            bool clrNull = ReferenceEquals(value, null);
            string runtimeType = clrNull ? "<clr-null>" : value.GetType().FullName;
            string text = clrNull ? "<clr-null>" : Probe(() => value.ToString());
            string id = clrNull ? "<clr-null>" : Probe(() => value.GetInstanceID().ToString());
            return $"clrRefNull={clrNull} unityEqNull={value == null} runtimeType={runtimeType} toString={text} instanceId={id}";
        }

        static string Probe(Func<string> read)
        {
            try { return "ok:" + read(); }
            catch (Exception e) { return "throws:" + e.GetType().Name; }
        }

        static void Record(string row)
        {
            string line = "[B-CHAR] " + row;
            TestContext.Out.WriteLine(line);
            Debug.Log(line);
        }
    }
}
