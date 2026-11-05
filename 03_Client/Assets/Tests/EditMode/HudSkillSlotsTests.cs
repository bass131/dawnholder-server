using System;
using System.Linq;
using System.Reflection;
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests
{
    // PR2 H1/H2: the scene has no gold display, and inventory notifications no longer reach the HUD.
    public sealed class HudSkillSlotsTests
    {
        [Test]
        public void H1_UiScene_HasNoGoldObjectsOrLabels_AndKeepsOneHud()
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/99.UI/UI.unity");
            bool opened = !scene.isLoaded;
            bool replaced = false;
            if (opened)
            {
                try { scene = EditorSceneManager.OpenScene("Assets/Scenes/99.UI/UI.unity", OpenSceneMode.Additive); }
                catch (Exception)
                {
                    scene = EditorSceneManager.OpenScene("Assets/Scenes/99.UI/UI.unity", OpenSceneMode.Single);
                    replaced = true;
                }
            }
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                HudController[] controllers = roots.SelectMany(root => root.GetComponentsInChildren<HudController>(true)).ToArray();
                HudLayout[] layouts = roots.SelectMany(root => root.GetComponentsInChildren<HudLayout>(true)).ToArray();
                Assert.AreEqual(1, controllers.Length, "H1 one scene-owned HUD controller");
                Transform hud = controllers[0].transform;
                {
                    Assert.AreEqual(1, layouts.Length, "H1 one scene-owned HUD layout");
                    Assert.IsNull(hud.Find("Gold_bar"), "H1 HUD/Gold_bar is removed");
                    Assert.IsNull(hud.Find("Gold_bar/GoldText"), "H1 HUD/Gold_bar/GoldText is removed");
                    Assert.IsEmpty(hud.GetComponentsInChildren<TMP_Text>(true).Where(text => IsGold(text.text)),
                        "H1 no serialized HUD TMP label says Gold or 골드");
                    LogAssert.NoUnexpectedReceived();
                }
            }
            finally
            {
                if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                if (replaced) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                else if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        [Test]
        public void H2_InventoryNotification_HasNoHudSubscriber_AndLeavesHudTextUnchanged_WithLiveObserver()
        {
            Assert.IsNull(UnityClientSession.Instance, "fixture: no previous session");
            Assert.IsNull(HudController.Instance, "fixture: no previous HUD");
            int latency = UnityClientSession.SimulatedLatencyMs;
            UnityClientSession.SimulatedLatencyMs = 0;
            GameplayConnection connection = null;
            InventoryMirrorProbe mirror = null;
            GameObject root = null;
            InventoryState inventory = null;
            Action observer = null;
            try
            {
                connection = new GameplayConnection();
                connection.BeginMap(0);
                connection.Ready();
                mirror = InventoryMirrorProbe.TryPublish();
                connection.Deliver(new ServerInventory(1, 1250));
                root = new GameObject("HUD notification fixture");
                var label = new GameObject("Observed HUD text", typeof(RectTransform), typeof(TextMeshProUGUI));
                label.transform.SetParent(root.transform, false);
                label.GetComponent<TMP_Text>().text = "unchanged HUD text";
                HudController hud = root.AddComponent<HudController>();
                // Connect the old field when present so the RED test observes its real mutation; removal is allowed.
                var serialized = new SerializedObject(hud);
                SerializedProperty legacyGold = serialized.FindProperty("_goldText");
                if (legacyGold != null) legacyGold.objectReferenceValue = label.GetComponent<TMP_Text>();
                serialized.ApplyModifiedPropertiesWithoutUndo();
                ViewLifecycle.Begin(root);
                ViewLifecycle.Frame(root);
                inventory = InventoryState.Instance;
                int calls = 0;
                observer = () => calls++;
                inventory.OnInventoryChanged += observer;
                FieldInfo storage = typeof(InventoryState).GetField(nameof(InventoryState.OnInventoryChanged),
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(storage, "fixture: inventory event storage");
                Delegate callbacks = storage.GetValue(inventory) as Delegate;
                Delegate[] subscribers = callbacks?.GetInvocationList() ?? Array.Empty<Delegate>();
                string[] before = root.GetComponentsInChildren<TMP_Text>(true).Select(text => text.text).ToArray();
                connection.Deliver(new ServerInventory(2, 56789));
                string[] after = root.GetComponentsInChildren<TMP_Text>(true).Select(text => text.text).ToArray();
                {
                    CollectionAssert.Contains(subscribers, observer, "H2 positive control: the live observer is in the same event list");
                    Assert.AreEqual(1, calls, "H2 positive control: the snapshot notified the observer once");
                    Assert.IsEmpty(subscribers.Where(callback => callback.Target is HudController), "H2 inventory has no HUD subscriber");
                    CollectionAssert.AreEqual(before, after, "H2 inventory notification leaves every HUD TMP string unchanged");
                }
            }
            finally
            {
                if (inventory != null && observer != null) inventory.OnInventoryChanged -= observer;
                connection?.Dispose();
                if (root != null) ViewLifecycle.Destroy(root);
                mirror?.Dispose();
                UnityClientSession.SimulatedLatencyMs = latency;
            }
        }

        static bool IsGold(string text) => (text ?? string.Empty).TrimStart().StartsWith("Gold", StringComparison.Ordinal) ||
            (text ?? string.Empty).Contains("골드");
    }
}
