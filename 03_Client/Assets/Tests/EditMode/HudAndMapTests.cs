#nullable enable
using Dawnholder.Client.Network;
using Dawnholder.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // HudController.UpdateMP 경계 + SceneRouter.MapIdToDisplayName + MapNameDisplay 단위 테스트.
    public class HudAndMapTests
    {
        // EditMode에서 생성한 GameObject는 자동 파괴되지 않고, MapNameDisplay의
        // static 상태가 테스트 간 누수되므로 SetUp/TearDown으로 격리.
        readonly System.Collections.Generic.List<GameObject> _spawned = new();

        [SetUp]
        public void SetUp() => MapNameDisplay.ResetForTest();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawned)
            {
                Object.DestroyImmediate(go);
            }
            _spawned.Clear();
            MapNameDisplay.ResetForTest();
        }

        // ── UpdateMP ──────────────────────────────────────────────────────────

        HudController CreateHud(out Slider mpSlider)
        {
            var go = new GameObject("Hud");
            _spawned.Add(go);
            var hud = go.AddComponent<HudController>();

            // _mpSlider를 SerializedObject로 주입 (private SerializeField — 기존 EnemyPrefabContractTests 패턴).
            var mpSliderGo = new GameObject("MpSlider");
            _spawned.Add(mpSliderGo);
            mpSlider = mpSliderGo.AddComponent<Slider>();

            var so = new UnityEditor.SerializedObject(hud);
            so.FindProperty("_mpSlider").objectReferenceValue = mpSlider;
            so.ApplyModifiedPropertiesWithoutUndo();
            return hud;
        }

        // EditMode에서 SendMessage("Start")는 ShouldRunBehaviour 가드에 걸리므로 reflection으로 직접 호출.
        static void InvokeStart(MapNameDisplay display) =>
            typeof(MapNameDisplay)
                .GetMethod("Start", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(display, null);

        MapNameDisplay CreateMapDisplay(out TextMeshProUGUI label)
        {
            var go = new GameObject("MapDisplay");
            _spawned.Add(go);
            label = go.AddComponent<TextMeshProUGUI>();
            var display = go.AddComponent<MapNameDisplay>();

            var so = new UnityEditor.SerializedObject(display);
            so.FindProperty("_label").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();
            return display;
        }

        [Test]
        public void UpdateMP_ZeroCurrent_SliderIsZero()
        {
            var hud = CreateHud(out Slider slider);
            hud.UpdateMP(0, 100);
            Assert.AreEqual(0f, slider.value, 0.0001f);
        }

        [Test]
        public void UpdateMP_FullCurrent_SliderIsOne()
        {
            var hud = CreateHud(out Slider slider);
            hud.UpdateMP(100, 100);
            Assert.AreEqual(1f, slider.value, 0.0001f);
        }

        [Test]
        public void UpdateMP_OverMax_ClampsToOne()
        {
            var hud = CreateHud(out Slider slider);
            hud.UpdateMP(110, 100);
            Assert.AreEqual(1f, slider.value, 0.0001f);
        }

        [Test]
        public void UpdateMP_MaxZero_GuardDoesNotThrow()
        {
            var hud = CreateHud(out Slider slider);
            // max=0 가드 — 예외 없이 반환, slider 값은 기본(0) 유지.
            Assert.DoesNotThrow(() => hud.UpdateMP(0, 0));
        }

        // ── MapIdToDisplayName ────────────────────────────────────────────────

        [Test]
        public void MapIdToDisplayName_Town() =>
            Assert.AreEqual("Town", SceneRouter.MapIdToDisplayName(0));

        [Test]
        public void MapIdToDisplayName_HuntingGround() =>
            Assert.AreEqual("Hunting Ground", SceneRouter.MapIdToDisplayName(1));

        [Test]
        public void MapIdToDisplayName_BossRoom() =>
            Assert.AreEqual("Boss Room", SceneRouter.MapIdToDisplayName(2));

        [Test]
        public void MapIdToDisplayName_Ending() =>
            Assert.AreEqual("Ending", SceneRouter.MapIdToDisplayName(3));

        [Test]
        public void MapIdToDisplayName_Unknown_ReturnsEmpty() =>
            Assert.AreEqual(string.Empty, SceneRouter.MapIdToDisplayName(99));

        // ── MapNameDisplay ────────────────────────────────────────────────────

        [Test]
        public void MapNameDisplay_SetMapId_InstanceStartReflectsName()
        {
            // SetMapId 먼저 → 이후 인스턴스 Start()가 해당 이름을 표시해야 함 (씬 재로드 복원 경로).
            MapNameDisplay.SetMapId(1);

            var display = CreateMapDisplay(out TextMeshProUGUI label);

            InvokeStart(display);

            Assert.AreEqual("Hunting Ground", label.text);
        }

        [Test]
        public void MapNameDisplay_SetMapId_LiveInstanceUpdatesImmediately()
        {
            // 인스턴스가 살아 있는 상태에서 SetMapId → 즉시 갱신.
            var display = CreateMapDisplay(out TextMeshProUGUI label);

            InvokeStart(display);   // Town(=0) 표시로 초기화
            MapNameDisplay.SetMapId(2);

            Assert.AreEqual("Boss Room", label.text);
        }

        [Test]
        public void MapNameDisplay_UnknownMapId_LogsError()
        {
            var display = CreateMapDisplay(out TextMeshProUGUI _);

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("알 수 없는 mapId=99"));
            MapNameDisplay.SetMapId(99);
            InvokeStart(display);
        }
    }
}
