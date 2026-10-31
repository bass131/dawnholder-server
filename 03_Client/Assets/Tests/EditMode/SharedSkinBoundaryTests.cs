using Dawnholder.Client.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Dawnholder.Client.Tests
{
    // The shared-skin contract changes only the twelve common fields. Inventory and character-only fields
    // remain empty in the default asset (goal completion 10; UiWindowSkin's dedicated-field boundary).
    public sealed class SharedSkinBoundaryTests
    {
        const string DefaultPath = "Assets/Resources/UI/Components/UiWindowSkin.asset";
        const string BrightPath = "Assets/Resources/UI/BrightComponents/UiWindowSkin.asset";

        [TestCase("_itemSlotEmpty")]
        [TestCase("_itemSlotFilled")]
        [TestCase("_itemSlotSelected")]
        [TestCase("_iconBase")]
        [TestCase("_materialIcon")]
        [TestCase("_currencyPouchIcon")]
        [TestCase("_coinIcon")]
        [TestCase("_characterSlot")]
        [TestCase("_characterSlotSelected")]
        [TestCase("_pedestal")]
        [TestCase("_selectionGlow")]
        [TestCase("_inputField")]
        [TestCase("_knightArt")]
        [TestCase("_mageArt")]
        public void DefaultSkin_LeavesDedicatedSlotEmpty_AndObserverRejectsFilledClone(string field)
        {
            var defaults = AssetDatabase.LoadAssetAtPath<UiWindowSkin>(DefaultPath);
            var bright = AssetDatabase.LoadAssetAtPath<UiWindowSkin>(BrightPath);
            Assert.IsNotNull(defaults, "fixture: the actual default skin loads");
            Assert.IsNotNull(bright, "fixture: the actual bright skin loads");
            var source = new SerializedObject(bright).FindProperty(field);
            Assert.IsNotNull(source, "fixture: the dedicated field exists in the bright skin");
            Assert.IsNotNull(source.objectReferenceValue, "fixture: the bright skin supplies this dedicated piece");

            var clone = Object.Instantiate(defaults);
            try
            {
                var wrong = new SerializedObject(clone);
                var changed = wrong.FindProperty(field);
                Assert.IsNotNull(changed, "fixture: the clone exposes the same field");
                changed.objectReferenceValue = source.objectReferenceValue;
                wrong.ApplyModifiedPropertiesWithoutUndo();
                var failure = Assert.Throws<AssertionException>(() => RequireEmpty(wrong, field),
                    "negative control: filling this field must be rejected");
                TestContext.Out.WriteLine("NEGATIVE CONTROL " + field + " rejected: " + failure.Message);

                RequireEmpty(new SerializedObject(defaults), field);
                TestContext.Out.WriteLine("actual default " + field + " is empty");
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        static void RequireEmpty(SerializedObject skin, string field)
        {
            var property = skin.FindProperty(field);
            Assert.IsNotNull(property, "the dedicated field must exist: " + field);
            Assert.IsNull(property.objectReferenceValue, "the default skin keeps the dedicated slot empty: " + field);
        }
    }
}
