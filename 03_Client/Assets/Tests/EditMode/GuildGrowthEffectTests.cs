using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Shared.GameData;
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
