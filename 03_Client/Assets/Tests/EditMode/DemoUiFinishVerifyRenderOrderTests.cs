#nullable enable
using System.Collections.Generic;
using System.Linq;
using Dawnholder.Client.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dawnholder.Client.Tests
{
    // E1/E4: the shipped prefabs have one body each, so exercise the multi-part contract independently.
    public sealed class DemoUiFinishVerifyRenderOrderTests
    {
        readonly List<GameObject> _roots = new();
        Texture2D _texture = null!;
        Sprite _sprite = null!;

        [SetUp]
        public void Setup()
        {
            _texture = new Texture2D(2, 2);
            _sprite = Sprite.Create(_texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (GameObject root in _roots) Object.DestroyImmediate(root);
            _roots.Clear();
            Object.DestroyImmediate(_sprite);
            Object.DestroyImmediate(_texture);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MultipartBodiesRemainSeparateAndRetiredVisualsKeepTheirState(bool reverseCreation)
        {
            Transform local = Root("Local");
            Transform remote = Root("Remote");
            SpriteRenderer localBack;
            SpriteRenderer localFront;
            if (reverseCreation)
            {
                localFront = Body(local, "Front", 8);
                localBack = Body(local, "Back", -4);
            }
            else
            {
                localBack = Body(local, "Back", -4);
                localFront = Body(local, "Front", 8);
            }
            SpriteRenderer remoteBack = Body(remote, "Back", -9);
            SpriteRenderer remoteFront = Body(remote, "Front", 7);
            SpriteRenderer retired = Body(local, "RetiredVisual", 97);
            retired.gameObject.SetActive(false);
            SpriteRenderer disabled = Body(remote, "DisabledBody", 98);
            disabled.enabled = false;
            SpriteRenderer empty = Body(remote, "EmptyBody", 99);
            empty.sprite = null;
            SpriteRenderer[] live = { localBack, localFront, remoteBack, remoteFront };
            Color[] colors = live.Select(body => body.color).ToArray();

            // Three is the approved ceiling below authored Player-layer effects at four, not a product-derived oracle.
            int remaining = CharacterRenderOrder.Apply(local, 3);
            CharacterRenderOrder.Apply(remote, remaining);
            int[] initial = live.Select(body => body.sortingOrder).ToArray();
            Assert.Greater(localBack.sortingOrder, remoteFront.sortingOrder, "whole local group precedes remote");
            Assert.Greater(localFront.sortingOrder, localBack.sortingOrder, "authored body depth is retained");
            Assert.Greater(remoteFront.sortingOrder, remoteBack.sortingOrder);
            Assert.AreEqual(live.Length, initial.Distinct().Count(), "every body has its own order");
            Assert.That(initial, Has.All.LessThan(4), "E4: effects keep their authored foreground range");

            remaining = CharacterRenderOrder.Apply(local, 3);
            CharacterRenderOrder.Apply(remote, remaining);
            CollectionAssert.AreEqual(initial, live.Select(body => body.sortingOrder), "repacking is stable");
            CollectionAssert.AreEqual(colors, live.Select(body => body.color));
            foreach (SpriteRenderer body in live)
            {
                Assert.AreEqual("Player", body.sortingLayerName);
                Assert.AreEqual(0f, body.transform.position.z);
                Assert.AreSame(_sprite, body.sprite);
                Assert.IsTrue(body.enabled);
                Assert.IsNull(body.GetComponentInParent<SortingGroup>());
            }
            Assert.AreEqual(97, retired.sortingOrder, "old inactive Visual is outside the live body set");
            Assert.IsFalse(retired.gameObject.activeSelf);
            Assert.AreEqual(98, disabled.sortingOrder);
            Assert.IsFalse(disabled.enabled);
            Assert.AreEqual(99, empty.sortingOrder);
            Assert.IsNull(empty.sprite);
        }

        Transform Root(string name)
        {
            var root = new GameObject(name);
            _roots.Add(root);
            return root.transform;
        }

        SpriteRenderer Body(Transform root, string name, int order)
        {
            var body = new GameObject(name);
            body.transform.SetParent(root, false);
            SpriteRenderer renderer = body.AddComponent<SpriteRenderer>();
            renderer.sprite = _sprite;
            renderer.sortingLayerName = "Player";
            renderer.sortingOrder = order;
            renderer.color = new Color(0.8f, 0.6f, 0.4f, 0.7f);
            return renderer;
        }
    }
}
