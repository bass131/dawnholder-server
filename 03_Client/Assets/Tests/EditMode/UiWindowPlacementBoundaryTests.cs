using System;
using System.Reflection;
using Dawnholder.Client.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // Independent boundary checks: fixed pixel fixtures keep expectations separate from the placement calculations.
    public class UiWindowPlacementBoundaryTests
    {
        static readonly PropertyInfo HudInstance = typeof(HudController).GetProperty("Instance");
        HudController _previousHud;

        [SetUp]
        public void SetUp()
        {
            _previousHud = HudController.Instance;
            HudInstance.SetValue(null, null);
        }

        [TearDown]
        public void TearDown() => HudInstance.SetValue(null, _previousHud);

        [TestCase(1f, 188f, 98f)]
        [TestCase(2f, 28f, -152f)]
        public void ActiveHud_UsesTheLowerPanel_EvenWhenPartyIsTransparent(float scale, float partyTop, float statusTop)
        {
            using var rig = new HudRig(scale);
            AssertArea(UiWindowAreas.LeftColumnBelowHud(rig.Owner), partyTop);
            rig.Party.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            rig.Draw();
            AssertArea(UiWindowAreas.LeftColumnBelowHud(rig.Owner), partyTop);

            // Reversing their vertical order catches an implementation that always takes Party_Info.
            rig.Status.anchoredPosition = new Vector2(20f, -200f);
            rig.Draw();
            AssertArea(UiWindowAreas.LeftColumnBelowHud(rig.Owner), statusTop);
        }

        [TestCase(1f, 278f)]
        [TestCase(2f, 208f)]
        public void InactiveHudPanels_AreExcluded_AndNoActivePanelUsesCanvasMargin(float scale, float statusTop)
        {
            using var rig = new HudRig(scale);
            rig.Party.gameObject.SetActive(false);
            rig.Draw();
            AssertArea(UiWindowAreas.LeftColumnBelowHud(rig.Owner), statusTop);
            rig.Status.gameObject.SetActive(false);
            rig.Draw();
            AssertArea(UiWindowAreas.LeftColumnBelowHud(rig.Owner), 348f);
        }

        [Test]
        public void MissingHud_UsesAllFourCanvasMargins()
        {
            using var rig = new HudRig(2f);
            HudInstance.SetValue(null, null);
            AssertArea(UiWindowAreas.LeftColumnBelowHud(rig.Owner), 348f);
        }

        [Test]
        public void InactiveHudAncestor_ExcludesBothPanels()
        {
            using var rig = new HudRig(2f);
            rig.Hud.gameObject.SetActive(false);
            rig.Draw();
            AssertArea(UiWindowAreas.LeftColumnBelowHud(rig.Owner), 348f);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void NegativeControl_AreaObserverRejectsAMissingMargin(bool top)
        {
            using var rig = new HudRig(2f);
            Rect observed = UiWindowAreas.LeftColumnBelowHud(rig.Owner);
            AssertArea(observed, 28f);
            Rect damaged = observed;
            if (top) damaged.yMax += 12f;
            else damaged.xMin -= 12f;
            AssertionException rejection = Assert.Throws<AssertionException>(() => AssertArea(damaged, 28f));
            Debug.Log($"[placement boundary negative control] missing {(top ? "top" : "left")} margin rejected: {rejection.Message}");
        }

        static void AssertArea(Rect area, float top)
        {
            Assert.AreEqual(-628f, area.xMin, 0.05f, "owner left inset is 12 canvas units");
            Assert.AreEqual(628f, area.xMax, 0.05f, "owner right inset is 12 canvas units");
            Assert.AreEqual(-348f, area.yMin, 0.05f, "owner bottom inset is 12 canvas units");
            Assert.AreEqual(top, area.yMax, 0.05f, "top is 12 owner units below the lower active HUD panel");
        }

        // Like UiWindowPlacementTests.Rig, explicitly runs CanvasScaler.Handle in EditMode, then renders once.
        // HUD panels have fixed authored local coordinates; expected owner coordinates are constants in the cases.
        sealed class HudRig : IDisposable
        {
            readonly GameObject _cameraObject;
            readonly Camera _camera;
            readonly RenderTexture _target;
            readonly CanvasScaler _ownerScaler;
            readonly CanvasScaler _hudScaler;
            internal readonly RectTransform Owner;
            internal readonly HudController Hud;
            internal readonly RectTransform Status;
            internal readonly RectTransform Party;

            internal HudRig(float hudScale)
            {
                _cameraObject = new GameObject("Placement boundary camera") { hideFlags = HideFlags.DontSave };
                _camera = _cameraObject.AddComponent<Camera>();
                _camera.enabled = false;
                _camera.orthographic = true;
                _camera.nearClipPlane = 0.3f;
                _camera.farClipPlane = 1000f;
                _target = new RenderTexture(1280, 720, 24);
                _target.Create();
                _camera.targetTexture = _target;
                _ownerScaler = CreateCanvas("Owner", 1f);
                Owner = (RectTransform)_ownerScaler.transform;
                _hudScaler = CreateCanvas("HUD", hudScale);
                Hud = _hudScaler.gameObject.AddComponent<HudController>();
                HudInstance.SetValue(null, Hud);
                Status = Panel("character_status", new Vector2(20f, -20f), new Vector2(200f, 50f));
                Party = Panel("Party_Info", new Vector2(20f, -100f), new Vector2(200f, 60f));
                Draw();
                Assert.AreEqual(1280f, Owner.rect.width, 0.05f, "fixture owner width");
                Assert.AreEqual(720f, Owner.rect.height, 0.05f, "fixture owner height");
                Assert.AreEqual(hudScale, _hudScaler.GetComponent<Canvas>().scaleFactor, 0.001f, "fixture HUD scale");
            }

            CanvasScaler CreateCanvas(string name, float scale)
            {
                var gameObject = new GameObject(name, typeof(RectTransform));
                gameObject.transform.SetParent(_cameraObject.transform, false);
                Canvas canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = _camera;
                canvas.planeDistance = 100f;
                CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = scale;
                return scaler;
            }

            RectTransform Panel(string name, Vector2 position, Vector2 size)
            {
                var panel = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
                panel.SetParent(Hud.transform, false);
                panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0f, 1f);
                panel.anchoredPosition = position;
                panel.sizeDelta = size;
                return panel;
            }

            internal void Draw()
            {
                Canvas.ForceUpdateCanvases();
                SessionTestTools.Call(_ownerScaler, "Handle");
                SessionTestTools.Call(_hudScaler, "Handle");
                Canvas.ForceUpdateCanvases();
                _camera.Render();
                Canvas.ForceUpdateCanvases();
            }

            public void Dispose()
            {
                _camera.targetTexture = null;
                _target.Release();
                UnityEngine.Object.DestroyImmediate(_target);
                UnityEngine.Object.DestroyImmediate(_cameraObject);
            }
        }
    }
}
