using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // ScreenUiProbe's ScreenRect, Bounds and PathOf are the one copy of the screen readings HudOverlapProbe,
    // DungeonClearRewardOverlapSceneTests and DungeonClearRewardPresentationLifecycleTests share (goal
    // 2026-10-10-dungeon-clear-rewards 「C9 독립 batch23·판정 정정과 좁은 후속」). Each test builds objects whose geometry
    // is set by hand, so every expected value follows from Unity's transform and overlay-canvas rules, not from the
    // readings under test.
    public sealed class ScreenUiProbeTests
    {
        // Float corners of exactly placed rectangles.
        const float Tolerance = 0.01f;

        readonly List<GameObject> _created = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            foreach (GameObject created in _created)
            {
                if (created != null) Object.Destroy(created);
            }
            _created.Clear();
            yield return null;
        }

        // An overlay canvas scaled by 2 spans the screen from (0, 0) and doubles local units. A 30×40 box anchored and
        // pivoted at its bottom-left with offset (10, 20) therefore covers screen pixels (20, 40)–(80, 120), and a 10×10
        // box at the centre covers 20×20 pixels around the screen's middle.
        [UnityTest]
        public IEnumerator ScreenRect_ReadsAnOverlayRectangleInScreenPixels_ThroughTheCanvasScale()
        {
            GameObject canvasObject = Track(new GameObject("ProbeOverlayCanvas"));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.scaleFactor = 2f;
            RectTransform corner = Box("Corner", canvasObject.transform, Vector2.zero, new Vector2(10f, 20f), new Vector2(30f, 40f));
            RectTransform centre = Box("Centre", canvasObject.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f));
            yield return null;
            Canvas.ForceUpdateCanvases();

            var root = (RectTransform)canvasObject.transform;
            Rect cornerRead = ScreenUiProbe.ScreenRect(corner);
            Rect centreRead = ScreenUiProbe.ScreenRect(centre);
            var middle = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Rect centreExpected = Rect.MinMaxRect(middle.x - 10f, middle.y - 10f, middle.x + 10f, middle.y + 10f);
            Debug.Log($"[dungeon C9 review-2] ScreenRect: screen {Screen.width}x{Screen.height}; canvas scale " +
                $"{canvas.scaleFactor:F2} root {root.rect} localScale {root.localScale}; corner {cornerRead}; centre {centreRead}");

            Assert.AreEqual(2f, root.localScale.x, Tolerance, "fixture: the overlay canvas root doubles local units");
            Assert.AreEqual(Screen.width * 0.5f, root.rect.width, Tolerance, "fixture: the scaled root spans the screen width");
            AssertSame(Rect.MinMaxRect(20f, 40f, 80f, 120f), cornerRead,
                "ScreenRect reads a bottom-left box in screen pixels, scaled by the canvas");
            AssertSame(centreExpected, centreRead, "ScreenRect reads a centred box around the screen's middle");
        }

        // Bounds turns a local box into its extent after the owner's transform. Mirroring x (scale -2) sends the local
        // minimum to the right, so only a per-axis min and max keep the width positive: local (-1, -1)–(3, 5) on an owner at
        // (100, 50) with scale (-2, 3) spans x 94–102 and y 47–65.
        [Test]
        public void Bounds_GivesTheExtentOfALocalBox_EvenWithAMirroredAxis()
        {
            Transform owner = Track(new GameObject("ProbeMirroredOwner")).transform;
            owner.position = new Vector3(100f, 50f, 0f);
            owner.localScale = new Vector3(-2f, 3f, 1f);
            var local = new Bounds(new Vector3(1f, 2f, 0f), new Vector3(4f, 6f, 0f));

            Rect read = ScreenUiProbe.Bounds(owner, local);
            Debug.Log($"[dungeon C9 review-2] Bounds: mirrored owner reads {read}");

            AssertSame(Rect.MinMaxRect(94f, 47f, 102f, 65f), read, "Bounds keeps min and max per axis under a mirrored scale");
        }

        // The callers' path strings: HudOverlapProbe and the overlap probe read paths relative to the root, and
        // HudOverlapProbe picks character_status graphics by that prefix; the presentation probe asks for the root's name in
        // front, which the overlap probe adds itself. The root alone is an empty relative path.
        [Test]
        public void PathOf_NamesTheChainBelowTheRoot_AndPutsTheRootInFrontOnlyWhenAsked()
        {
            Transform root = Track(new GameObject("ProbeHudCanvas")).transform;
            Transform status = Node("character_status", root);
            Transform fill = Node("fill", Node("hp_bar", status));

            string relative = ScreenUiProbe.PathOf(fill, root);
            string withRoot = ScreenUiProbe.PathOf(fill, root, includeRoot: true);
            string callerPrefixed = root.name + "/" + ScreenUiProbe.PathOf(fill, root);
            string rootRelative = ScreenUiProbe.PathOf(root, root);
            string rootWithRoot = ScreenUiProbe.PathOf(root, root, includeRoot: true);
            Debug.Log($"[dungeon C9 review-2] PathOf: relative '{relative}', with root '{withRoot}', caller prefixed " +
                $"'{callerPrefixed}', root '{rootRelative}' / '{rootWithRoot}'");

            Assert.AreEqual("character_status/hp_bar/fill", relative, "PathOf without the root names the chain below it");
            Assert.AreEqual("ProbeHudCanvas/character_status/hp_bar/fill", withRoot, "includeRoot puts the root's name in front");
            Assert.AreEqual(withRoot, callerPrefixed, "the overlap probe's own root prefix gives the same string as includeRoot");
            Assert.AreEqual(string.Empty, rootRelative, "the root alone is an empty relative path");
            Assert.AreEqual("ProbeHudCanvas/", rootWithRoot, "the root alone with includeRoot is its name and a separator");
        }

        GameObject Track(GameObject created)
        {
            _created.Add(created);
            return created;
        }

        // A box anchored and pivoted at one point of its parent, offset by `position`.
        static RectTransform Box(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, worldPositionStays: false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        static Transform Node(string name, Transform parent)
        {
            Transform node = new GameObject(name).transform;
            node.SetParent(parent, worldPositionStays: false);
            return node;
        }

        static void AssertSame(Rect expected, Rect actual, string message)
        {
            Assert.AreEqual(expected.xMin, actual.xMin, Tolerance, message + ": left");
            Assert.AreEqual(expected.yMin, actual.yMin, Tolerance, message + ": bottom");
            Assert.AreEqual(expected.xMax, actual.xMax, Tolerance, message + ": right");
            Assert.AreEqual(expected.yMax, actual.yMax, Tolerance, message + ": top");
        }
    }
}
