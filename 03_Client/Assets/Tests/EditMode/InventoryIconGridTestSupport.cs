using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Shared.GameData;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // Observes the icon grid without consulting the inventory mirror. Separate from the PlayMode probe because
    // the assemblies have different UI references; only this EditMode helper drives LateUpdate explicitly.
    internal static class InventoryIconGridTestSupport
    {
        internal const string PointerType = "Dawnholder.Client.UI.InventorySlotPointer";
        internal static readonly float[] Heights = { 335f, 430f, 525f, 620f, 715f, 810f };
        internal static readonly float[] Bottoms = { 215f, 310f, 405f, 500f, 595f, 690f };
        internal static readonly Rect[] SlotRects =
        {
            new Rect(12, 134, 81, 81), new Rect(107, 134, 81, 81), new Rect(202, 134, 81, 81), new Rect(297, 134, 81, 81),
            new Rect(12, 229, 81, 81), new Rect(107, 229, 81, 81), new Rect(202, 229, 81, 81), new Rect(297, 229, 81, 81),
            new Rect(12, 324, 81, 81), new Rect(107, 324, 81, 81), new Rect(202, 324, 81, 81), new Rect(297, 324, 81, 81),
            new Rect(12, 419, 81, 81), new Rect(107, 419, 81, 81), new Rect(202, 419, 81, 81), new Rect(297, 419, 81, 81),
            new Rect(12, 514, 81, 81), new Rect(107, 514, 81, 81), new Rect(202, 514, 81, 81), new Rect(297, 514, 81, 81),
            new Rect(12, 609, 81, 81), new Rect(107, 609, 81, 81), new Rect(202, 609, 81, 81), new Rect(297, 609, 81, 81),
        };

        internal static int HeightIndex
        {
            get
            {
                Assert.That(InventoryLimits.MaxSlots, Is.InRange(1, 24), "contract table covers at most 24 slots");
                return (InventoryLimits.MaxSlots + 3) / 4 - 1;
            }
        }

        internal static RectTransform Slot(Transform root, int index) => Required(root, $"Inventory/Slot{index + 1}");

        internal static RectTransform Required(Transform root, string path)
        {
            var found = root.Find(path) as RectTransform;
            Assert.IsNotNull(found, "required inventory object: " + path);
            return found;
        }

        internal static bool Visible(Image image) => image != null && image.isActiveAndEnabled && image.sprite != null && image.color.a > 0f;

        // 칸이 보이는 아이콘과 수량을 옛 줄 글 모양으로 읽는다.
        internal static string SlotText(Transform root, int index)
        {
            RectTransform slot = Slot(root, index);
            Image icon = Required(slot, "Icon").GetComponent<Image>();
            if (!Visible(icon)) return "빈 슬롯";
            string name = BrightUiKitTestSupport.PieceOf(icon.sprite)?.Name ?? icon.sprite.name;
            ItemId item = name == "Icon_Material" ? ItemId.Material : name == "Icon_CurrencyPouch" ? ItemId.CoinPouch : ItemId.None;
            Assert.AreNotEqual(ItemId.None, item, "known visible item icon: " + name);
            TMP_Text count = Required(slot, "Count").GetComponent<TMP_Text>();
            return ItemCatalog.For(item).DisplayName + " ×" + (count.gameObject.activeInHierarchy ? count.text : "1");
        }

        internal static bool HasSlotValue(Transform root, int index)
        {
            RectTransform slot = Slot(root, index);
            return Visible(Required(slot, "Icon").GetComponent<Image>()) || Required(slot, "Count").gameObject.activeSelf;
        }

        internal static RectTransform Detail(Transform root) => Required(root, "ItemDetail");
        internal static TMP_Text DetailLabel(Transform root, string name) => Required(Detail(root), name).GetComponent<TMP_Text>();
        internal static Vector2 SlotPoint(Transform root, int index, Camera camera = null)
        {
            RectTransform slot = Slot(root, index);
            return RectTransformUtility.WorldToScreenPoint(camera, slot.TransformPoint(slot.rect.center));
        }

        internal static Rect InSpace(RectTransform space, RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector3 a = space.InverseTransformPoint(corners[0]), c = space.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y), Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
        }

        internal static Rect FromTopLeft(RectTransform panel, RectTransform target)
        {
            Rect r = InSpace(panel, target);
            return new Rect(r.xMin - panel.rect.xMin, panel.rect.yMax - r.yMax, r.width, r.height);
        }

        internal static void AssertInside(Rect outer, Rect inner, string message)
        {
            const float tolerance = 0.1f;
            Assert.IsTrue(inner.xMin >= outer.xMin - tolerance && inner.xMax <= outer.xMax + tolerance &&
                inner.yMin >= outer.yMin - tolerance && inner.yMax <= outer.yMax + tolerance, $"{message}: {inner} inside {outer}");
        }

        internal sealed class Pointer : IDisposable
        {
            readonly Mouse _previous;
            readonly object _manager;
            readonly PropertyInfo _playerUpdatesInEditMode;
            readonly bool _previousPlayerUpdatesInEditMode;
            internal readonly Mouse Mouse;

            internal Pointer()
            {
                _previous = UnityEngine.InputSystem.Mouse.current;
                // EditMode editor updates do not stamp wasPressedThisFrame (VirtualKeyboard precedent).
                // Feature flags are not serialized in InputSettings copies; restore the manager flag itself.
                _manager = typeof(InputSystem).GetField("s_Manager", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
                Assert.IsNotNull(_manager, "fixture: InputSystem keeps its manager in s_Manager");
                _playerUpdatesInEditMode = _manager.GetType().GetProperty("runPlayerUpdatesInEditMode", BindingFlags.Instance | BindingFlags.Public);
                Assert.IsNotNull(_playerUpdatesInEditMode, "fixture: InputManager.runPlayerUpdatesInEditMode exists");
                _previousPlayerUpdatesInEditMode = (bool)_playerUpdatesInEditMode.GetValue(_manager);
                _playerUpdatesInEditMode.SetValue(_manager, true);
                try
                {
                    Mouse = InputSystem.AddDevice<Mouse>();
                    Mouse.MakeCurrent();
                }
                catch
                {
                    Dispose(); // A constructor failure never reaches the test's using block.
                    throw;
                }
            }

            internal void Step(Transform root, Vector2 point, MouseButton? button = null, bool expectRightPress = false, bool allowDisposed = false)
            {
                MouseState state = new MouseState { position = point };
                if (button.HasValue) state = state.WithButton(button.Value);
                InputSystem.QueueStateEvent(Mouse, state);
                InputSystem.Update();
                if (expectRightPress) Assert.IsTrue(Mouse.rightButton.wasPressedThisFrame, "fixture: right-button press reached the Input System this frame");
                Component part = Required(root, "Inventory").GetComponents<Component>()
                    .SingleOrDefault(component => component != null && component.GetType().FullName == PointerType);
                if (part == null && allowDisposed)
                {
                    TestContext.Out.WriteLine("disposed view: InventorySlotPointer absent after actual right press");
                    return;
                }
                Assert.IsNotNull(part, "InventorySlotPointer not found");
                MethodInfo lateUpdate = part.GetType().GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(lateUpdate, PointerType + ".LateUpdate");
                lateUpdate.Invoke(part, null);
            }

            internal void RightClick(Transform root, int index, Camera camera = null, bool allowDisposed = false)
            {
                Vector2 point = SlotPoint(root, index, camera);
                Step(root, point, allowDisposed: allowDisposed);
                Step(root, point, MouseButton.Right, expectRightPress: true, allowDisposed: allowDisposed);
                Step(root, point, allowDisposed: allowDisposed);
            }

            public void Dispose()
            {
                try
                {
                    if (Mouse != null && Mouse.added) InputSystem.RemoveDevice(Mouse);
                    if (_previous != null && _previous.added) _previous.MakeCurrent();
                }
                finally
                {
                    _playerUpdatesInEditMode?.SetValue(_manager, _previousPlayerUpdatesInEditMode);
                }
            }
        }
    }
}
