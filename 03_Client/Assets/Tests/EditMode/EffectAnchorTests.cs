#nullable enable
using Dawnholder.Client.Combat;
using NUnit.Framework;
using UnityEngine;

namespace Dawnholder.Client.Tests
{
    // EffectAnchor.ResolvePosition 단위 테스트 — 재귀 탐색 + world 거울상 수학.
    // 거울상 부호 실수는 런타임에서 "이펙트가 반대편에서 터짐"으로만 발견됨 → 값으로 고정.
    public class EffectAnchorTests
    {
        GameObject _root = null!;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Root");
            _root.transform.position = new Vector3(10f, 2f, 0f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void ResolvePosition_NoAnchor_FallsBackToRoot()
        {
            Vector3 result = EffectAnchor.ResolvePosition(_root.transform);
            Assert.AreEqual(new Vector3(10f, 2f, 0f), result);
        }

        [Test]
        public void ResolvePosition_NestedAnchor_ReturnsWorldPosition()
        {
            // root > Visual > EffectAnchor 깊이 — v2 구조. localPosition 수학이면 깨지는 케이스.
            var visual = new GameObject("Visual");
            visual.transform.SetParent(_root.transform, false);
            var anchor = new GameObject(EffectAnchor.ChildName);
            anchor.transform.SetParent(visual.transform, false);
            anchor.transform.localPosition = new Vector3(0.3f, 0.6f, 0f);

            Vector3 result = EffectAnchor.ResolvePosition(_root.transform);

            Assert.AreEqual(10.3f, result.x, 1e-4f);
            Assert.AreEqual(2.6f, result.y, 1e-4f);
        }

        [Test]
        public void ResolvePosition_FlipX_MirrorsAroundRootX()
        {
            var visual = new GameObject("Visual");
            visual.transform.SetParent(_root.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.flipX = true;
            var anchor = new GameObject(EffectAnchor.ChildName);
            anchor.transform.SetParent(visual.transform, false);
            anchor.transform.localPosition = new Vector3(0.3f, 0.6f, 0f);

            Vector3 result = EffectAnchor.ResolvePosition(_root.transform);

            // 반사 중심 = root.x(10): 10.3 → 9.7. y는 불변.
            Assert.AreEqual(9.7f, result.x, 1e-4f);
            Assert.AreEqual(2.6f, result.y, 1e-4f);
        }
    }
}
