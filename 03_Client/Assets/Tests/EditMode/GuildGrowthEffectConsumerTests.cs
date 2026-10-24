using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dawnholder.Client.UI;
using NUnit.Framework;
using UnityEngine;

namespace Dawnholder.Client.Tests
{
    // Structural guards of the guild growth effect consumer (goal 2026-10-24-growth-effect-screen-link, consumer-contract-v3
    // §1): no global or persistent effect view, GuildUiController as the only holder of a view reference, and no effect
    // canvas nested under the GuildUi canvas in any scene. Edit Mode cannot make a scene named Town, so these hold on the
    // baseline and stay true after the consumer; the behavioural RED of the consumer is in
    // PlayMode/GuildGrowthEffectConsumerLifecycleTests. Shapes are read from the compiled client assembly and live objects.
    public sealed class GuildGrowthEffectConsumerTests
    {
        const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        GameObject _parent;

        [TearDown]
        public void Teardown()
        {
            if (_parent != null) UnityEngine.Object.DestroyImmediate(_parent);
            _parent = null;
        }

        // No static field or property of any client type keeps a view (alone, in an array or in a generic collection),
        // so no view outlives its scene through a registry or singleton.
        [Test]
        public void NoClientType_KeepsAStaticGrowthEffectView()
        {
            string[] holders = ClientTypes().Where(type => !IsCompilerGenerated(type))
                .SelectMany(type => type.GetFields(AnyStatic).Where(field => !IsCompilerGenerated(field) && Holds(field.FieldType))
                    .Select(field => type.FullName + "." + field.Name)
                    .Concat(type.GetProperties(AnyStatic).Where(property => Holds(property.PropertyType))
                        .Select(property => type.FullName + "." + property.Name)))
                .ToArray();
            CollectionAssert.IsEmpty(holders, "no global or persistent GuildGrowthEffectView holder");
        }

        // GuildUiController owns creation, reference and destruction of the view. Its nested types (including compiler
        // closures) count as GuildUiController; any other client type that keeps a view reference is a second owner.
        [Test]
        public void OnlyGuildUiController_KeepsAGrowthEffectViewReference()
        {
            string[] holders = ClientTypes().Where(type => !IsWithin(type, typeof(GuildUiController)))
                .SelectMany(type => type.GetFields(AnyInstance).Where(field => Holds(field.FieldType))
                    .Select(field => type.FullName + "." + field.Name))
                .ToArray();
            CollectionAssert.IsEmpty(holders, "only GuildUiController (and its nested types) keeps a GuildGrowthEffectView reference");
        }

        // Outside Town the composition may or may not build a view; any view it builds is a direct child of the given
        // parent (beside GuildUi and the windows), not under the GuildUi canvas, and leaves nothing behind.
        [Test]
        public void GuildUiComposition_NestsNoEffectCanvas_AndLeavesNoViewAfterItsParent()
        {
            HashSet<GuildGrowthEffectView> before = LiveViews();
            _parent = new GameObject("GuildGrowthEffectConsumerTestParent");
            GuildUiController owner = GuildUiController.BuildRuntime(_parent.transform);

            GuildGrowthEffectView[] made = LiveViews().Except(before).ToArray();
            string[] paths = made.Select(view => PathOf(view.transform)).ToArray();
            TestContext.Out.WriteLine("[GuildGrowthConsumer] edit-mode composition views=" + made.Length + " [" + string.Join("; ", paths) + "]");
            Assert.LessOrEqual(made.Length, 1, "at most one view per composition: " + string.Join("; ", paths));
            foreach (GuildGrowthEffectView view in made)
            {
                Assert.AreSame(_parent.transform, view.transform.parent, "a built view is a direct child of the given parent: " + PathOf(view.transform));
                Assert.IsFalse(view.transform.IsChildOf(owner.transform), "a built view is not under the GuildUi canvas: " + PathOf(view.transform));
            }

            UnityEngine.Object.DestroyImmediate(_parent);
            _parent = null;
            CollectionAssert.IsEmpty(LiveViews().Except(before).Select(view => PathOf(view.transform)).ToArray(),
                "destroying the composition's parent leaves no view of it in any loaded scene");
        }

        static Type[] ClientTypes()
        {
            try
            {
                return typeof(GuildUiController).Assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException error)
            {
                Assert.Fail("fixture: the client assembly's types load: " + string.Join("; ", error.LoaderExceptions.Select(item => item.Message)));
                throw;
            }
        }

        // A view, or an array or generic argument of one. Delegate types are not holders: a cached non-capturing lambda
        // over views is code, not a kept reference.
        static bool Holds(Type type)
        {
            if (typeof(Delegate).IsAssignableFrom(type)) return false;
            if (typeof(GuildGrowthEffectView).IsAssignableFrom(type)) return true;
            if (type.IsArray) return Holds(type.GetElementType());
            return type.IsGenericType && type.GetGenericArguments().Any(Holds);
        }

        static bool IsWithin(Type type, Type owner)
        {
            for (Type current = type; current != null; current = current.DeclaringType)
            {
                if (current == owner) return true;
            }
            return false;
        }

        static bool IsCompilerGenerated(MemberInfo member) => member.Name.Contains("<");

        // Live views in loaded scenes; assets and prefabs have no valid scene.
        static HashSet<GuildGrowthEffectView> LiveViews() => new HashSet<GuildGrowthEffectView>(
            Resources.FindObjectsOfTypeAll<GuildGrowthEffectView>().Where(view => view != null && view.gameObject.scene.IsValid()));

        static string PathOf(Transform transform)
        {
            var names = new List<string>();
            for (Transform current = transform; current != null; current = current.parent) names.Insert(0, current.name);
            return transform.gameObject.scene.name + ":" + string.Join("/", names);
        }
    }
}
