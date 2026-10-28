using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using TMPro;
using UnityEngine;

namespace Dawnholder.Client.Tests
{
    // Pretest of the guild growth effect (goal 2026-10-13-demo-video-art, growth-effect-pr pretest-contract-v2 requirement 1
    // and its BuildRuntime clause), written before the product. GuildGrowthEffectView does not exist yet, so it is reached
    // through GuildProduct by the names pretest/api-contract.md fixes and the suite compiles on the baseline; a missing type
    // or member fails with "RED unimplemented". Expected shapes come from the contract text, not from any implementation.
    // EditMode sends no Unity messages, so ViewLifecycle sends them. Frames, timing, cancellation and pointers are judged in
    // PlayMode/GuildGrowthEffectLifecycleTests.
    public sealed class GuildGrowthEffectTests
    {
        const string ViewName = "GuildGrowthEffectView";
        const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        GameObject _parent;

        [TearDown]
        public void Teardown()
        {
            if (_parent != null) ViewLifecycle.Destroy(_parent);
            _parent = null;
        }

        // Requirement 1: the public shape the contract fixes. The view belongs to the scene that passes its parent, so no
        // static field or property of the view type (alone, in an array or in a generic collection) holds one.
        [Test]
        public void View_IsAPublicSealedMonoBehaviour_WithTheContractMembers_AndNoStaticInstance()
        {
            Type view = GuildProduct.Require(ViewName);
            Assert.IsTrue(view.IsPublic, "public class");
            Assert.IsTrue(view.IsSealed, "sealed class");
            Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(view), "a MonoBehaviour");

            MethodInfo build = GuildProduct.Method(view, "BuildRuntime", typeof(Transform));
            Assert.IsTrue(build.IsStatic, "BuildRuntime is static");
            Assert.AreEqual(view, build.ReturnType, "BuildRuntime returns the view it built");
            MethodInfo play = GuildProduct.Method(view, "TryPlayConfirmed", typeof(GuildChangeKind), typeof(int));
            Assert.IsFalse(play.IsStatic, "TryPlayConfirmed belongs to one view");
            Assert.AreEqual(typeof(bool), play.ReturnType, "TryPlayConfirmed answers whether it accepted the request");
            MethodInfo cancel = GuildProduct.Method(view, "CancelAll");
            Assert.IsFalse(cancel.IsStatic, "CancelAll belongs to one view");
            Assert.AreEqual(typeof(void), cancel.ReturnType, "CancelAll returns nothing");
            MethodInfo setArea = GuildProduct.Method(view, "TrySetAvailableArea", typeof(Rect?));
            Assert.IsFalse(setArea.IsStatic, "the available area belongs to one view");
            Assert.AreEqual(typeof(bool), setArea.ReturnType, "invalid areas can be refused without changing state");

            string[] staticHolders = view.GetFields(AnyStatic).Where(field => Holds(view, field.FieldType)).Select(field => field.Name)
                .Concat(view.GetProperties(AnyStatic).Where(property => Holds(view, property.PropertyType)).Select(property => property.Name))
                .ToArray();
            CollectionAssert.IsEmpty(staticHolders, "no global singleton or registry of views (contract BuildRuntime clause)");
        }

        // The BuildRuntime clause: everything a call makes is one new root directly under the given scene-owned parent,
        // with the view inside it, and a second call makes a second root and view instead of handing back a shared one.
        // Unity's own Awake/OnEnable/Start and one frame run first, so objects made there count too.
        [Test]
        public void BuildRuntime_MakesOneOwnRootUnderTheGivenParent_PerCall()
        {
            Type view = GuildProduct.Require(ViewName);
            MethodInfo build = GuildProduct.Method(view, "BuildRuntime", typeof(Transform));
            _parent = new GameObject("GuildGrowthEffectTestParent");
            HashSet<GameObject> before = SceneObjects();

            var first = (Component)GuildProduct.Invoke(() => build.Invoke(null, new object[] { _parent.transform }));
            var second = (Component)GuildProduct.Invoke(() => build.Invoke(null, new object[] { _parent.transform }));
            ViewLifecycle.Begin(_parent);
            ViewLifecycle.Frame(_parent);

            Assert.IsNotNull(first, "the first call returns a view");
            Assert.IsNotNull(second, "the second call returns a view");
            Assert.AreNotSame(first, second, "each call returns its own view");
            Assert.IsNull(_parent.GetComponent(view), "the view is not added to the caller's parent itself");
            Transform firstRoot = RootUnderParent(first);
            Transform secondRoot = RootUnderParent(second);
            Assert.AreNotSame(firstRoot, secondRoot, "each call makes its own root");
            string[] outside = SceneObjects().Except(before)
                .Where(made => !made.transform.IsChildOf(firstRoot) && !made.transform.IsChildOf(secondRoot))
                .Select(made => PathOf(made.transform))
                .ToArray();
            CollectionAssert.IsEmpty(outside, "every object the two calls made is under one of their two roots");
        }

        // The BuildRuntime clause: a null parent is refused plainly and leaves no root anywhere. The refusal is either an
        // ArgumentNullException or no view (null); which one the product uses is api-contract.md's open question 1.
        [Test]
        public void BuildRuntime_RefusesANullParent_AndLeavesNoObject()
        {
            Type view = GuildProduct.Require(ViewName);
            MethodInfo build = GuildProduct.Method(view, "BuildRuntime", typeof(Transform));
            HashSet<GameObject> before = SceneObjects();

            object built = null;
            Exception refusal = null;
            try
            {
                built = GuildProduct.Invoke(() => build.Invoke(null, new object[] { null }));
            }
            catch (Exception error)
            {
                refusal = error;
            }
            string builtType = ReferenceEquals(built, null) ? "<none>" : built.GetType().FullName;
            GameObject[] left = SceneObjects().Except(before).ToArray();
            string[] leftPaths = left.Select(item => PathOf(item.transform)).ToArray();
            DestroyLeftovers(left);

            if (refusal != null)
            {
                Assert.IsInstanceOf<ArgumentNullException>(refusal, "a refusing exception names the null argument");
            }
            else
            {
                Assert.IsTrue(ReferenceEquals(built, null), "without an exception the refusal returns no view, not " + builtType);
            }
            CollectionAssert.IsEmpty(leftPaths, "a refused build leaves no object in any loaded scene");
        }

        [Test]
        public void AvailableArea_NullRestoresTheOriginalLayout_WithoutChangingRunningEffects()
        {
            GuildGrowthEffectView view = BuildAreaView(new Vector2(960f, 540f));
            PlayBoth(view);
            object[] original = VisualState(view);
            Assert.AreEqual(new Vector2(0.43f, 0.62f), Slot(view, "StrongholdGrowth").anchorMin);
            Assert.AreEqual(new Vector2(0.80f, 0.28f), Slot(view, "WeaponEnhancement").anchorMin);
            Assert.AreEqual(new Vector2(380f, 400f), Slot(view, "StrongholdGrowth").sizeDelta);
            Assert.AreEqual(new Vector2(260f, 250f), Slot(view, "WeaponEnhancement").sizeDelta);
            Assert.AreEqual(Vector3.one, Slot(view, "StrongholdGrowth").localScale);

            Assert.IsTrue(view.TrySetAvailableArea(new Rect(0.15f, 0.20f, 0.60f, 0.55f)));
            Assert.IsTrue(view.TrySetAvailableArea(null));
            CollectionAssert.AreEqual(original, VisualState(view), "null restores layout and preserves the current visual pose/text");
            Assert.IsTrue(view.TrySetAvailableArea(null));
            CollectionAssert.AreEqual(original, VisualState(view), "repeated null is stable");
        }

        [TestCase(0f, 0f, 1f, 1f, TestName = "AvailableArea_Valid_Full")]
        [TestCase(0f, 0f, 0.5f, 0.5f, TestName = "AvailableArea_Valid_BottomLeft")]
        [TestCase(0.5f, 0.5f, 0.5f, 0.5f, TestName = "AvailableArea_Valid_TopRight")]
        [TestCase(0.2f, 0.3f, 0.6f, 0.4f, TestName = "AvailableArea_Valid_Interior")]
        [TestCase(0.45f, 0f, 0.1f, 1f, TestName = "AvailableArea_Valid_Narrow")]
        [TestCase(0f, 0.45f, 1f, 0.1f, TestName = "AvailableArea_Valid_Short")]
        [TestCase(0f, 0f, 0.001f, 0.001f, TestName = "AvailableArea_Valid_TinyGeometryOnly")]
        public void AvailableArea_ValidRect_ContainsWholeAnimatedContents_WithoutOverlap(float x, float y, float width, float height)
        {
            GuildGrowthEffectView view = BuildAreaView(new Vector2(1920f, 1080f));
            var area = new Rect(x, y, width, height);
            Assert.IsTrue(view.TrySetAvailableArea(area));
            double started = PlayBoth(view);
            foreach (double age in new[] { 0.1d, 0.35d, 0.6d, 0.95d, 1.05d, 1.7d })
            {
                TickAt(view, started + age);
                AssertWholeContents(view, area);
            }
            // These are synthetic geometry inputs. Acceptance of the tiny rectangle makes no readability claim.
        }

        [TestCaseSource(nameof(InvalidAreas))]
        public void AvailableArea_InvalidRect_IsRefusedWithoutChangingThePreviousLayoutOrRunningEffects(Rect invalid)
        {
            GuildGrowthEffectView view = BuildAreaView(new Vector2(960f, 540f));
            var previous = new Rect(0.10f, 0.15f, 0.75f, 0.70f);
            Assert.IsTrue(view.TrySetAvailableArea(previous));
            double started = PlayBoth(view);
            TickAt(view, started + 0.5d);
            object[] before = VisualState(view);

            Assert.IsFalse(view.TrySetAvailableArea(invalid));
            CollectionAssert.AreEqual(before, VisualState(view), "a refusal does not mutate transforms, labels, active state or colors");
            SetCanvasSize(view, new Vector2(1280f, 720f));
            TickAt(view, started + 0.6d);
            AssertWholeContents(view, previous);
            TickAt(view, started + 1.3d);
            Assert.IsFalse(Slot(view, "WeaponEnhancement").gameObject.activeSelf, "the refused input did not restart the personal timer");
            TickAt(view, started + 2.1d);
            Assert.IsFalse(Slot(view, "StrongholdGrowth").gameObject.activeSelf, "the refused input did not restart the shared timer");
        }

        [Test]
        public void AvailableArea_InvalidBeforeFirstPlay_LeavesTheDefaultAndEmptyState()
        {
            GuildGrowthEffectView view = BuildAreaView(new Vector2(960f, 540f));
            object[] original = VisualState(view);
            Assert.IsFalse(view.TrySetAvailableArea(new Rect(0f, 0f, 0f, 1f)));
            CollectionAssert.AreEqual(original, VisualState(view));
            Assert.IsTrue(view.TrySetAvailableArea(null));
            CollectionAssert.AreEqual(original, VisualState(view));
        }

        [Test]
        public void AvailableArea_UpdateAndResize_PreserveLevelsAndTheOriginalExpiry()
        {
            GuildGrowthEffectView view = BuildAreaView(new Vector2(1920f, 1080f));
            Assert.IsTrue(view.TrySetAvailableArea(new Rect(0f, 0f, 0.5f, 1f)));
            double started = PlayBoth(view);
            TickAt(view, started + 0.6d);
            var updated = new Rect(0.35f, 0.15f, 0.60f, 0.70f);
            Assert.IsTrue(view.TrySetAvailableArea(updated));
            AssertWholeContents(view, updated);
            SetCanvasSize(view, new Vector2(1280f, 720f));
            TickAt(view, started + 0.8d);
            AssertWholeContents(view, updated);
            CollectionAssert.AreEquivalent(new[] { "거점 레벨 2147483647", "무기 강화 2147483647" },
                view.GetComponentsInChildren<TMP_Text>().Select(label => label.text).ToArray());
            TickAt(view, started + 1.3d);
            Assert.IsFalse(Slot(view, "WeaponEnhancement").gameObject.activeSelf, "area/resize must not extend a running effect");
            TickAt(view, started + 2.1d);
            Assert.IsFalse(Slot(view, "StrongholdGrowth").gameObject.activeSelf);
        }

        [Test]
        public void AvailableArea_IsOwnedPerView_AndCancelDoesNotReplayOrLoseItsConfiguration()
        {
            GuildGrowthEffectView first = BuildAreaView(new Vector2(960f, 540f));
            GuildGrowthEffectView second = GuildGrowthEffectView.BuildRuntime(_parent.transform);
            object[] secondBefore = VisualState(second);
            var area = new Rect(0.10f, 0.25f, 0.70f, 0.60f);
            Assert.IsTrue(first.TrySetAvailableArea(area));
            CollectionAssert.AreEqual(secondBefore, VisualState(second), "one view does not change another view's area");
            PlayBoth(first);
            first.CancelAll();
            TickAt(first, Time.realtimeSinceStartupAsDouble + 3d);
            Assert.IsFalse(Slot(first, "StrongholdGrowth").gameObject.activeSelf);
            Assert.IsFalse(Slot(first, "WeaponEnhancement").gameObject.activeSelf);
            Assert.IsTrue(first.GetComponentsInChildren<TMP_Text>(true).All(label => label.text == string.Empty));
            PlayBoth(first);
            AssertWholeContents(first, area);
        }

        [Test]
        public void AvailableArea_DisabledView_DoesNotReplay_AndRetainsTheUpdatedArea()
        {
            GuildGrowthEffectView view = BuildAreaView(new Vector2(960f, 540f));
            PlayBoth(view);
            view.enabled = false;
            ViewLifecycle.Disable(view.gameObject);
            var area = new Rect(0.10f, 0.20f, 0.65f, 0.60f);
            Assert.IsTrue(view.TrySetAvailableArea(area), "layout configuration need not start an effect");
            Assert.IsFalse(view.TryPlayConfirmed(GuildChangeKind.WeaponEnhance, 2));
            view.enabled = true;
            ViewLifecycle.Begin(view.gameObject);
            TickAt(view, Time.realtimeSinceStartupAsDouble + 3d);
            Assert.IsFalse(Slot(view, "StrongholdGrowth").gameObject.activeSelf);
            Assert.IsFalse(Slot(view, "WeaponEnhancement").gameObject.activeSelf);
            PlayBoth(view);
            AssertWholeContents(view, area);
        }

        [Test]
        public void AvailableArea_DestroyedView_RefusesUpdates_AndLeavesItsEffectsCleared()
        {
            GuildGrowthEffectView view = BuildAreaView(new Vector2(960f, 540f));
            Assert.IsTrue(view.TrySetAvailableArea(new Rect(0.1f, 0.2f, 0.7f, 0.6f)));
            PlayBoth(view);
            Transform root = view.transform;
            // EditMode needs explicit lifecycle callbacks before destroying this ordinary MonoBehaviour.
            SessionTestTools.Call(view, "OnDisable");
            SessionTestTools.Call(view, "OnDestroy");
            UnityEngine.Object.DestroyImmediate(view);
            Assert.IsFalse(view.TrySetAvailableArea(null));
            Assert.IsFalse(root.Find("StrongholdGrowth").gameObject.activeSelf);
            Assert.IsFalse(root.Find("WeaponEnhancement").gameObject.activeSelf);
            Assert.IsTrue(root.GetComponentsInChildren<TMP_Text>(true).All(label => label.text == string.Empty));
        }

        static IEnumerable<TestCaseData> InvalidAreas()
        {
            yield return Invalid("NaNX", new Rect(float.NaN, 0f, 0.5f, 0.5f));
            yield return Invalid("NaNY", new Rect(0f, float.NaN, 0.5f, 0.5f));
            yield return Invalid("NaNWidth", new Rect(0f, 0f, float.NaN, 0.5f));
            yield return Invalid("NaNHeight", new Rect(0f, 0f, 0.5f, float.NaN));
            foreach (float infinity in new[] { float.PositiveInfinity, float.NegativeInfinity })
            {
                string sign = infinity > 0f ? "Positive" : "Negative";
                yield return Invalid(sign + "InfinityX", new Rect(infinity, 0f, 0.5f, 0.5f));
                yield return Invalid(sign + "InfinityY", new Rect(0f, infinity, 0.5f, 0.5f));
                yield return Invalid(sign + "InfinityWidth", new Rect(0f, 0f, infinity, 0.5f));
                yield return Invalid(sign + "InfinityHeight", new Rect(0f, 0f, 0.5f, infinity));
            }
            yield return Invalid("NegativeX", new Rect(-0.01f, 0f, 0.5f, 0.5f));
            yield return Invalid("NegativeY", new Rect(0f, -0.01f, 0.5f, 0.5f));
            yield return Invalid("XOutside", new Rect(1.01f, 0f, 0.1f, 0.5f));
            yield return Invalid("YOutside", new Rect(0f, 1.01f, 0.5f, 0.1f));
            yield return Invalid("ZeroWidth", new Rect(0f, 0f, 0f, 0.5f));
            yield return Invalid("ZeroHeight", new Rect(0f, 0f, 0.5f, 0f));
            yield return Invalid("NegativeWidth", new Rect(0.5f, 0f, -0.1f, 0.5f));
            yield return Invalid("NegativeHeight", new Rect(0f, 0.5f, 0.5f, -0.1f));
            yield return Invalid("RightOverflow", new Rect(0.75f, 0f, 0.5f, 0.5f));
            yield return Invalid("TopOverflow", new Rect(0f, 0.75f, 0.5f, 0.5f));
            yield return Invalid("RoundedRightOverflow", new Rect(0.75f, 0f, 0.25000003f, 0.5f));
            yield return Invalid("RoundedTopOverflow", new Rect(0f, 0.75f, 0.5f, 0.25000003f));
            yield return Invalid("RightEdgePositiveWidth", new Rect(1f, 0f, float.Epsilon, 0.5f));
            yield return Invalid("TopEdgePositiveHeight", new Rect(0f, 1f, 0.5f, float.Epsilon));
        }

        static TestCaseData Invalid(string name, Rect area) => new TestCaseData(area).SetName("AvailableArea_Invalid_" + name);

        GuildGrowthEffectView BuildAreaView(Vector2 canvasSize)
        {
            _parent = new GameObject("GuildGrowthEffectAreaTestParent");
            GuildGrowthEffectView view = GuildGrowthEffectView.BuildRuntime(_parent.transform);
            // Isolate owned RectTransform geometry; no GameView, Screen, registry or global resolution is changed.
            view.GetComponent<Canvas>().enabled = false;
            foreach (Behaviour behaviour in view.GetComponents<Behaviour>())
            {
                if (behaviour.GetType().FullName == "UnityEngine.UI.CanvasScaler") behaviour.enabled = false;
            }
            SetCanvasSize(view, canvasSize);
            return view;
        }

        static void SetCanvasSize(GuildGrowthEffectView view, Vector2 size)
        {
            var canvas = (RectTransform)view.transform;
            canvas.anchorMin = canvas.anchorMax = canvas.pivot = new Vector2(0.5f, 0.5f);
            canvas.anchoredPosition = Vector2.zero;
            canvas.localScale = Vector3.one;
            canvas.sizeDelta = size;
        }

        static double PlayBoth(GuildGrowthEffectView view)
        {
            Assert.IsTrue(view.TryPlayConfirmed(GuildChangeKind.StrongholdUpgrade, int.MaxValue));
            Assert.IsTrue(view.TryPlayConfirmed(GuildChangeKind.WeaponEnhance, int.MaxValue));
            return Time.realtimeSinceStartupAsDouble;
        }

        // The existing view supplies Unity time; invoke its actual presentation tick with a deterministic future time
        // in Edit Mode. This is a local geometry/state check, not a real Play Mode frame or screen-readability verdict.
        static void TickAt(GuildGrowthEffectView view, double now)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo field = typeof(GuildGrowthEffectView).GetField("_presentation", flags);
            Assert.IsNotNull(field, "fixture: the view owns its presentation");
            object presentation = field.GetValue(view);
            Assert.IsNotNull(presentation);
            MethodInfo tick = presentation.GetType().GetMethod("Tick", flags);
            Assert.IsNotNull(tick, "fixture: the production presentation accepts a frame time");
            GuildProduct.Invoke(() => tick.Invoke(presentation, new object[] { now }));
        }

        static RectTransform Slot(GuildGrowthEffectView view, string name) => (RectTransform)view.transform.Find(name);

        static object[] VisualState(GuildGrowthEffectView view)
        {
            var result = new List<object>();
            foreach (RectTransform rect in view.GetComponentsInChildren<RectTransform>(true))
            {
                result.AddRange(new object[] { rect.name, rect.gameObject.activeSelf, rect.anchorMin, rect.anchorMax,
                    rect.pivot, rect.sizeDelta, rect.anchoredPosition, rect.localScale, rect.localRotation });
            }
            foreach (TMP_Text label in view.GetComponentsInChildren<TMP_Text>(true))
            {
                result.AddRange(new object[] { label.text, label.color, label.fontSize, label.enabled, label.raycastTarget });
            }
            foreach (Component image in view.GetComponentsInChildren<Component>(true)
                .Where(component => component != null && component.GetType().FullName == "UnityEngine.UI.Image"))
            {
                result.Add(image.GetType().GetProperty("color").GetValue(image));
            }
            return result.ToArray();
        }

        static void AssertWholeContents(GuildGrowthEffectView view, Rect normalizedArea)
        {
            var canvas = (RectTransform)view.transform;
            var bounds = new List<Rect>();
            foreach (string name in new[] { "StrongholdGrowth", "WeaponEnhancement" })
            {
                RectTransform slot = Slot(view, name);
                if (!slot.gameObject.activeSelf) continue;
                var points = new List<Vector2>();
                foreach (RectTransform rect in slot.GetComponentsInChildren<RectTransform>())
                {
                    var corners = new Vector3[4];
                    rect.GetWorldCorners(corners);
                    points.AddRange(corners.Select(point => NormalizedPoint(canvas, point)));
                }
                TMP_Text label = slot.GetComponentInChildren<TMP_Text>();
                label.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
                TMP_CharacterInfo[] characters = label.textInfo.characterInfo;
                int visibleCharacters = 0;
                for (int index = 0; index < label.textInfo.characterCount; index++)
                {
                    TMP_CharacterInfo character = characters[index];
                    if (!character.isVisible) continue;
                    visibleCharacters++;
                    Vector3[] vertices = label.textInfo.meshInfo[character.materialReferenceIndex].vertices;
                    for (int corner = 0; corner < 4; corner++)
                    {
                        points.Add(NormalizedPoint(canvas, label.transform.TransformPoint(vertices[character.vertexIndex + corner])));
                    }
                }
                Assert.Greater(visibleCharacters, 0, "fixture: real glyph mesh vertices are required for " + name);
                Assert.Greater(points.Count, 0, "fixture: every active effect has observable geometry");
                Rect actual = Rect.MinMaxRect(points.Min(point => point.x), points.Min(point => point.y),
                    points.Max(point => point.x), points.Max(point => point.y));
                float tolerance = Mathf.Min(normalizedArea.width, normalizedArea.height) * 0.0001f;
                Assert.GreaterOrEqual(actual.xMin, normalizedArea.xMin - tolerance, name + " whole left bound");
                Assert.GreaterOrEqual(actual.yMin, normalizedArea.yMin - tolerance, name + " whole bottom bound");
                Assert.LessOrEqual(actual.xMax, normalizedArea.xMax + tolerance, name + " whole right bound");
                Assert.LessOrEqual(actual.yMax, normalizedArea.yMax + tolerance, name + " whole top bound");
                bounds.Add(actual);
            }
            Assert.Greater(bounds.Count, 0, "containment cannot pass with no active effect");
            if (bounds.Count == 2)
            {
                bool apart = bounds[0].xMax <= bounds[1].xMin || bounds[1].xMax <= bounds[0].xMin
                    || bounds[0].yMax <= bounds[1].yMin || bounds[1].yMax <= bounds[0].yMin;
                Assert.IsTrue(apart, "actual full-content bounds of the two effects must not overlap");
            }
        }

        static Vector2 NormalizedPoint(RectTransform canvas, Vector3 world)
        {
            Vector3 local = canvas.InverseTransformPoint(world);
            return new Vector2((local.x - canvas.rect.xMin) / canvas.rect.width,
                (local.y - canvas.rect.yMin) / canvas.rect.height);
        }

        static bool Holds(Type view, Type type)
        {
            if (view.IsAssignableFrom(type)) return true;
            if (type.IsArray) return Holds(view, type.GetElementType());
            return type.IsGenericType && type.GetGenericArguments().Any(argument => Holds(view, argument));
        }

        // Every GameObject of every loaded scene, hidden ones included; assets and prefabs have no valid scene.
        static HashSet<GameObject> SceneObjects() => new HashSet<GameObject>(
            Resources.FindObjectsOfTypeAll<GameObject>().Where(item => item.scene.IsValid()));

        Transform RootUnderParent(Component view)
        {
            Transform parent = _parent.transform;
            Assert.IsTrue(view.transform != parent && view.transform.IsChildOf(parent), "the view is under the given parent");
            Transform root = view.transform;
            while (root.parent != parent) root = root.parent;
            return root;
        }

        // The topmost leftovers only; their children go with them.
        static void DestroyLeftovers(GameObject[] left)
        {
            var leftSet = new HashSet<GameObject>(left);
            foreach (GameObject item in left.Where(item => item.transform.parent == null || !leftSet.Contains(item.transform.parent.gameObject)))
            {
                if (item != null) UnityEngine.Object.DestroyImmediate(item);
            }
        }

        static string PathOf(Transform transform)
        {
            var names = new List<string>();
            for (Transform current = transform; current != null; current = current.parent) names.Insert(0, current.name);
            return transform.gameObject.scene.name + ":" + string.Join("/", names);
        }
    }
}
