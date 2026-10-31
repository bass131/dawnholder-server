using System;
using System.Collections.Generic;
using System.Reflection;
using Dawnholder.Client.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // The shared window placement part on plain fixture windows, written before the implementation (goal
    // 2026-10-30-window-placement-inventory 완료조건 3·6 and 「만들 것」; user decisions 68 C, 69 A, 70 A; contract
    // placement-red-contract-v1 T3 and 「부품 API」). The part is called directly, so this file compiles only once the part
    // exists. Windows are plain RectTransforms with a title bar child, built with different anchors and pivots because
    // the part is meant for six windows. Canvases use the render tests' camera rig (ScreenSpaceCamera, CanvasScaler.Handle)
    // at a 1280×720 render size, so a canvas scale other than 1 is covered. Expected scales and places are worked out by
    // hand from each case's constants; the formula is written next to the case, never recomputed in code. Drags go to
    // the grab area through ExecuteEvents in the order a pointer sends them.
    public sealed class UiWindowPlacementTests
    {
        // UI units; only absorbs float rounding through transforms.
        const float Tolerance = 0.5f;

        // Screen pixels; the same rounding allowance on the camera's render target.
        const float PixelTolerance = 0.5f;

        // The inventory's maximum scale (InventoryPanelView MaxScale), used for every fixture window.
        const float MaxScale = 1.3f;

        static readonly Vector2 s_windowSize = new Vector2(390f, 720f);
        static readonly Vector2Int s_renderSize = new Vector2Int(1280, 720);

        // A drag in screen pixels over DragSteps moves; the first step (25, -15) is past the 10-pixel drag threshold.
        static readonly Vector2 s_drag = new Vector2(100f, -60f);
        const int DragSteps = 4;

        // Anchor and pivot of the inventory (top centre), of the reward window (centre) and an uneven pair.
        static readonly object[] s_anchorsAndPivots =
        {
            new object[] { new Vector2(0.5f, 1f), new Vector2(0.5f, 1f) },
            new object[] { new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f) },
            new object[] { new Vector2(0f, 0f), new Vector2(1f, 0.25f) },
        };

        // The three canvas scale modes of goal 「만들 것」 (window-placement-scope-01 2절).
        static readonly object[] s_scalers =
        {
            new object[] { "ScaleWithScreenSize 1920x1080 match 0.5" },
            new object[] { "ScaleWithScreenSize 960x540" },
            new object[] { "ConstantPixelSize" },
        };

        readonly List<IDisposable> _rigs = new List<IDisposable>();

        [TearDown]
        public void Teardown()
        {
            UiWindowPlacement.ForgetMovedPositions();
            for (int i = _rigs.Count - 1; i >= 0; i--) _rigs[i].Dispose();
            _rigs.Clear();
        }

        // ---------------------------------------------------------------------------------------------------------
        // Fit, alignment and staying inside the canvas
        // ---------------------------------------------------------------------------------------------------------

        // 맞춤 배율 = max(0.1, min(1.3, area width / 390, area height / 720)). Canvas 1920×1080 units (1280×720 render,
        // reference 1920×1080, match 0.5), centred on its origin; every area lies inside it.
        //   large  600×1000: min(1.3, 600/390 = 1.538…, 1000/720 = 1.388…) = 1.3 (the maximum)
        //   narrow 312×1000: min(1.3, 312/390 = 0.8, 1.388…) = 0.8 (the width)
        //   low    600×360:  min(1.3, 1.538…, 360/720 = 0.5) = 0.5 (the height)
        //   tiny   30×36:    min(1.3, 30/390 = 0.0769…, 36/720 = 0.05) = 0.05, raised to 0.1 (the minimum)
        [TestCase(600f, 1000f, 1.3f, TestName = "FitScale_LargeArea_IsTheMaximum1_3")]
        [TestCase(312f, 1000f, 0.8f, TestName = "FitScale_NarrowArea_FollowsTheWidth0_8")]
        [TestCase(600f, 360f, 0.5f, TestName = "FitScale_LowArea_FollowsTheHeight0_5")]
        [TestCase(30f, 36f, 0.1f, TestName = "FitScale_TinyArea_IsTheMinimum0_1")]
        public void FitScale_IsTheLargestScaleThatFits_BetweenTheMinimumAndTheMaximum(float width, float height, float expected)
        {
            Rig rig = NewRig("ScaleWithScreenSize 1920x1080 match 0.5");
            Fixture window = BuildWindow(rig, "Fit window", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            UiWindowPlacement placement = UiWindowPlacement.Attach(window.Window, window.TitleBar, "Fit", MaxScale, UiWindowAlignment.TopLeft);
            var area = new Rect(-900f, -500f, width, height);

            rig.Draw();
            placement.Place(area);
            rig.Draw();
            TestContext.Out.WriteLine($"canvas={rig.CanvasRect.rect} area={area} scale={window.Window.localScale}");

            Assert.AreEqual(1920f, rig.CanvasRect.rect.width, Tolerance, "fixture: the canvas is 1920 units wide");
            Assert.AreEqual(1080f, rig.CanvasRect.rect.height, Tolerance, "fixture: the canvas is 1080 units high");
            Assert.AreEqual(expected, window.Window.localScale.x, 1e-3f, $"T3 fit scale x for a {width}×{height} area");
            Assert.AreEqual(expected, window.Window.localScale.y, 1e-3f, $"T3 fit scale y for a {width}×{height} area");
        }

        // TopLeft: area (-800, -480, 600, 1000) gives scale min(1.3, 1.538…, 1.388…) = 1.3, a 507×936 window whose top-left
        // corner is the area's top-left (-800, 520), whatever the window's own anchor and pivot.
        [TestCaseSource(nameof(s_anchorsAndPivots))]
        public void TopLeft_PutsTheScaledWindowsTopLeftOnTheAreasTopLeft(Vector2 anchor, Vector2 pivot)
        {
            Rig rig = NewRig("ScaleWithScreenSize 1920x1080 match 0.5");
            Fixture window = BuildWindow(rig, "TopLeft window", anchor, pivot);
            UiWindowPlacement placement = UiWindowPlacement.Attach(window.Window, window.TitleBar, "TopLeft", MaxScale, UiWindowAlignment.TopLeft);
            var area = new Rect(-800f, -480f, 600f, 1000f);

            rig.Draw();
            placement.Place(area);
            rig.Draw();
            Rect placed = InCanvas(rig, window.Window);
            TestContext.Out.WriteLine($"anchor={anchor} pivot={pivot} area={area} placed={placed}");

            Assert.AreEqual(507f, placed.width, Tolerance, "T3 the window is 390 × 1.3 wide");
            Assert.AreEqual(936f, placed.height, Tolerance, "T3 the window is 720 × 1.3 high");
            Assert.AreEqual(-800f, placed.xMin, Tolerance, "T3 TopLeft puts the window's left edge on the area's left edge");
            Assert.AreEqual(520f, placed.yMax, Tolerance, "T3 TopLeft puts the window's top edge on the area's top edge");
        }

        // Center: the same area and 507×936 window, centred on the area's centre (-500, 20).
        [TestCaseSource(nameof(s_anchorsAndPivots))]
        public void Center_PutsTheScaledWindowsCentreOnTheAreasCentre(Vector2 anchor, Vector2 pivot)
        {
            Rig rig = NewRig("ScaleWithScreenSize 1920x1080 match 0.5");
            Fixture window = BuildWindow(rig, "Center window", anchor, pivot);
            UiWindowPlacement placement = UiWindowPlacement.Attach(window.Window, window.TitleBar, "Center", MaxScale, UiWindowAlignment.Center);
            var area = new Rect(-800f, -480f, 600f, 1000f);

            rig.Draw();
            placement.Place(area);
            rig.Draw();
            Rect placed = InCanvas(rig, window.Window);
            TestContext.Out.WriteLine($"anchor={anchor} pivot={pivot} area={area} placed={placed}");

            Assert.AreEqual(507f, placed.width, Tolerance, "T3 the window is 390 × 1.3 wide");
            Assert.AreEqual(936f, placed.height, Tolerance, "T3 the window is 720 × 1.3 high");
            Assert.AreEqual(-500f, placed.center.x, Tolerance, "T3 Center puts the window's centre x on the area's centre x");
            Assert.AreEqual(20f, placed.center.y, Tolerance, "T3 Center puts the window's centre y on the area's centre y");
        }

        // The part writes only scale and anchoredPosition: attaching, placing and a drag leave the window's anchors, pivot
        // and size as the window built them.
        [TestCaseSource(nameof(s_anchorsAndPivots))]
        public void AttachPlaceAndDrag_KeepTheWindowsAnchorsPivotAndSize(Vector2 anchor, Vector2 pivot)
        {
            Rig rig = NewRig("ScaleWithScreenSize 1920x1080 match 0.5");
            Fixture window = BuildWindow(rig, "Kept window", anchor, pivot);
            UiWindowPlacement placement = UiWindowPlacement.Attach(window.Window, window.TitleBar, "Kept", MaxScale, UiWindowAlignment.TopLeft);
            Func<Rect> area = () => OwnerArea(rig);

            rig.Draw();
            placement.Place(area());
            rig.Draw();
            Drag(rig, placement, area, s_drag, DragSteps);

            Assert.AreEqual(anchor, window.Window.anchorMin, "T3 the part keeps anchorMin");
            Assert.AreEqual(anchor, window.Window.anchorMax, "T3 the part keeps anchorMax");
            Assert.AreEqual(pivot, window.Window.pivot, "T3 the part keeps the pivot");
            Assert.AreEqual(s_windowSize, window.Window.sizeDelta, "T3 the part keeps sizeDelta");
        }

        // An area reaching past the canvas's right and bottom edges (canvas x ≤ 960, y ≥ -540): TopLeft at (760, 200) with
        // the 507×936 window would end at x 1267 and y -736, so the part pulls the whole window back inside the canvas.
        [Test]
        public void AreaReachingOutsideTheCanvas_StillKeepsTheWholeWindowInsideIt()
        {
            Rig rig = NewRig("ScaleWithScreenSize 1920x1080 match 0.5");
            Fixture window = BuildWindow(rig, "Edge window", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            UiWindowPlacement placement = UiWindowPlacement.Attach(window.Window, window.TitleBar, "Edge", MaxScale, UiWindowAlignment.TopLeft);
            var area = new Rect(760f, -800f, 600f, 1000f);

            rig.Draw();
            placement.Place(area);
            rig.Draw();
            Rect canvas = rig.CanvasRect.rect;
            Rect placed = InCanvas(rig, window.Window);
            TestContext.Out.WriteLine($"canvas={canvas} area={area} placed={placed}");

            Assert.IsTrue(Inside(canvas, placed), $"T3 the whole window {placed} stays inside the canvas {canvas}");
        }

        // The grab area: a child of the title bar named GrabArea, an Image with no sprite, alpha 0 and raycastTarget on,
        // filling the title bar's rectangle.
        [Test]
        public void GrabArea_IsAnInvisibleRaycastImage_FillingTheTitleBar()
        {
            Rig rig = NewRig("ScaleWithScreenSize 1920x1080 match 0.5");
            Fixture window = BuildWindow(rig, "Grab window", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            UiWindowPlacement placement = UiWindowPlacement.Attach(window.Window, window.TitleBar, "Grab", MaxScale, UiWindowAlignment.TopLeft);
            rig.Draw();
            placement.Place(OwnerArea(rig));
            rig.Draw();

            RectTransform grabArea = placement.GrabArea;
            Assert.IsNotNull(grabArea, "T3 the part exposes its grab area");
            var image = grabArea.GetComponent<Image>();
            Rect inBar = InSpaceOf(window.TitleBar, grabArea);
            TestContext.Out.WriteLine($"grabArea={grabArea.name} parent={grabArea.parent.name} inBar={inBar} bar={window.TitleBar.rect}");

            Assert.AreEqual("GrabArea", grabArea.name, "T3 the grab area is named GrabArea");
            Assert.AreSame(window.TitleBar, grabArea.parent, "T3 the grab area is a child of the title bar");
            Assert.AreSame(placement, window.Window.GetComponent<UiWindowPlacement>(), "T3 the part sits on the window's GameObject");
            Assert.IsNotNull(image, "T3 the grab area is an Image");
            Assert.IsNull(image.sprite, "T3 the grab area has no sprite");
            Assert.AreEqual(0f, image.color.a, "T3 the grab area is fully transparent");
            Assert.IsTrue(image.raycastTarget, "T3 the grab area takes raycasts");
            Assert.IsTrue(SameRect(window.TitleBar.rect, inBar), $"T3 the grab area {inBar} fills the title bar {window.TitleBar.rect}");
        }

        // ---------------------------------------------------------------------------------------------------------
        // Drag, screen edges, remembered places and resolution changes
        // ---------------------------------------------------------------------------------------------------------

        // 끌기 거리 변환: for each canvas scale mode at a 1280×720 render, dragging the grab area by (100, -60) screen pixels
        // moves the window by (100, -60) pixels on screen. Canvas units per pixel are 1.5, 0.75 and 1 in the three modes.
        [TestCaseSource(nameof(s_scalers))]
        public void GrabAreaDrag_MovesTheWindowByTheSamePixelsOnScreen(string scaler)
        {
            Rig rig = NewRig(scaler);
            Fixture window = BuildWindow(rig, "Drag window", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            UiWindowPlacement placement = UiWindowPlacement.Attach(window.Window, window.TitleBar, "Drag " + scaler, MaxScale, UiWindowAlignment.TopLeft);
            Func<Rect> area = () => OwnerArea(rig);
            rig.Draw();
            placement.Place(area());
            rig.Draw();
            Rect before = OnScreen(rig, window.Window);

            Drag(rig, placement, area, s_drag, DragSteps);
            Rect after = OnScreen(rig, window.Window);
            Vector2 moved = after.position - before.position;
            TestContext.Out.WriteLine($"{scaler}: scaleFactor={rig.Canvas.scaleFactor} canvas={rig.CanvasRect.rect} before={before} after={after} moved={moved}");

            Assert.AreEqual(s_drag.x, moved.x, PixelTolerance, $"T3 {scaler}: the window moves by the drag's x on screen");
            Assert.AreEqual(s_drag.y, moved.y, PixelTolerance, $"T3 {scaler}: the window moves by the drag's y on screen");
        }

        // 화면 끝에서 멈춤: a drag 3000 pixels to the left stops the window inside the canvas; dragging 100 pixels back to the
        // right then moves it 100 pixels at once (no hidden overshoot is kept).
        [Test]
        public void DragPastTheEdge_StopsInside_AndTheWayBackMovesAtOnce()
        {
            Rig rig = NewRig("ScaleWithScreenSize 1920x1080 match 0.5");
            Fixture window = BuildWindow(rig, "Edge drag window", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            UiWindowPlacement placement = UiWindowPlacement.Attach(window.Window, window.TitleBar, "Edge drag", MaxScale, UiWindowAlignment.TopLeft);
            Func<Rect> area = () => OwnerArea(rig);
            rig.Draw();
            placement.Place(area());
            rig.Draw();

            Drag(rig, placement, area, new Vector2(-3000f, 0f), 6);
            Rect stopped = OnScreen(rig, window.Window);
            Rect stoppedInCanvas = InCanvas(rig, window.Window);
            Drag(rig, placement, area, new Vector2(100f, 0f), DragSteps);
            Rect back = OnScreen(rig, window.Window);
            TestContext.Out.WriteLine($"canvas={rig.CanvasRect.rect} stopped={stoppedInCanvas} screen {stopped} -> {back}");

            Assert.IsTrue(Inside(rig.CanvasRect.rect, stoppedInCanvas), $"T3 the far drag stops the window {stoppedInCanvas} inside the canvas");
            Assert.AreEqual(100f, back.xMin - stopped.xMin, PixelTolerance, "T3 dragging 100 pixels back moves the window 100 pixels at once");
            Assert.AreEqual(0f, back.yMin - stopped.yMin, PixelTolerance, "T3 a horizontal drag keeps the window's height on screen");
        }

        // 자리 기억 (69 A): after a drag, a new window attached with the same key starts at the dragged place, a window with
        // another key at the default place, and after ForgetMovedPositions the same key starts at the default place again.
        [Test]
        public void MovedPlace_IsKeptPerKey_ForANewWindow_UntilForgotten()
        {
            Rig rig = NewRig("ScaleWithScreenSize 1920x1080 match 0.5");
            Func<Rect> area = () => OwnerArea(rig);
            Fixture first = BuildWindow(rig, "Remembered window", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            UiWindowPlacement placement = UiWindowPlacement.Attach(first.Window, first.TitleBar, "Memory A", MaxScale, UiWindowAlignment.TopLeft);
            rig.Draw();
            placement.Place(area());
            rig.Draw();
            Rect defaultPlace = InCanvas(rig, first.Window);
            Drag(rig, placement, area, s_drag, DragSteps);
            Rect dragged = InCanvas(rig, first.Window);
            UnityEngine.Object.DestroyImmediate(first.Window.gameObject);

            Rect sameKey = PlacedNewWindow(rig, "Memory A");
            Rect otherKey = PlacedNewWindow(rig, "Memory B");
            UiWindowPlacement.ForgetMovedPositions();
            Rect forgotten = PlacedNewWindow(rig, "Memory A");
            TestContext.Out.WriteLine($"default={defaultPlace} dragged={dragged} sameKey={sameKey} otherKey={otherKey} forgotten={forgotten}");

            Assert.IsFalse(SameRect(defaultPlace, dragged), "precondition: the drag moved the window");
            Assert.IsTrue(SameRect(dragged, sameKey), $"T3 the same key starts a new window at the dragged place {dragged}, got {sameKey}");
            Assert.IsTrue(SameRect(defaultPlace, otherKey), $"T3 another key starts at the default place {defaultPlace}, got {otherKey}");
            Assert.IsTrue(SameRect(defaultPlace, forgotten), $"T3 after ForgetMovedPositions the key starts at the default place {defaultPlace}, got {forgotten}");
        }

        // 해상도 변경: ConstantPixelSize keeps one canvas unit per pixel. At 1280×720 the window is dragged 700 pixels right
        // (x -628 → 72 for the 12-unit inset area). Shrinking the render to 960×540 pulls it back inside the canvas; growing
        // it back to 1280×720 returns the dragged place, because the pull for a resize does not shorten the stored drag.
        [Test]
        public void ResolutionChange_PullsTheWindowInside_AndRestoringItReturnsTheDraggedPlace()
        {
            Rig rig = NewRig("ConstantPixelSize");
            Fixture window = BuildWindow(rig, "Resolution window", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            UiWindowPlacement placement = UiWindowPlacement.Attach(window.Window, window.TitleBar, "Resolution", MaxScale, UiWindowAlignment.TopLeft);
            Func<Rect> area = () => FullArea(rig);
            rig.Draw();
            placement.Place(area());
            rig.Draw();
            Drag(rig, placement, area, new Vector2(700f, 0f), DragSteps);
            Rect dragged = InCanvas(rig, window.Window);

            rig.Resize(new Vector2Int(960, 540));
            placement.Place(area());
            rig.Draw();
            Rect smallCanvas = rig.CanvasRect.rect;
            Rect pulled = InCanvas(rig, window.Window);
            rig.Resize(s_renderSize);
            placement.Place(area());
            rig.Draw();
            Rect restored = InCanvas(rig, window.Window);
            TestContext.Out.WriteLine($"dragged={dragged} smallCanvas={smallCanvas} pulled={pulled} restored={restored}");

            Assert.AreEqual(960f, smallCanvas.width, Tolerance, "fixture: the shrunk canvas is 960 units wide");
            Assert.IsTrue(Inside(smallCanvas, pulled), $"T3 the smaller canvas {smallCanvas} pulls the window {pulled} inside");
            Assert.IsTrue(SameRect(dragged, restored), $"T3 the restored canvas returns the dragged place {dragged}, got {restored}");
        }

        // 다시 켜기 (69 A): ForgetMovedPositions runs from a RuntimeInitializeOnLoadMethod that is not AfterSceneLoad, so
        // the table is empty before the first scene's windows read it.
        [Test]
        public void ForgetMovedPositions_RunsOnLoadBeforeTheFirstScene()
        {
            MethodInfo forget = typeof(UiWindowPlacement).GetMethod(nameof(UiWindowPlacement.ForgetMovedPositions),
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            Assert.IsNotNull(forget, "fixture: UiWindowPlacement.ForgetMovedPositions()");
            var onLoad = forget.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>();
            TestContext.Out.WriteLine($"onLoad={(onLoad == null ? "none" : onLoad.loadType.ToString())}");

            Assert.IsNotNull(onLoad, "T3 ForgetMovedPositions carries RuntimeInitializeOnLoadMethod");
            Assert.AreNotEqual(RuntimeInitializeLoadType.AfterSceneLoad, onLoad.loadType, "T3 it runs before the first scene loads");
        }

        // ---------------------------------------------------------------------------------------------------------
        // Fixture: camera rig, windows, owner areas and the pointer drag
        // ---------------------------------------------------------------------------------------------------------

        sealed class Fixture
        {
            internal RectTransform Window;
            internal RectTransform TitleBar;
        }

        // A fixture camera drawing into a render texture, with one ScreenSpaceCamera canvas on it (the render tests'
        // CaptureRig, which is private to UiWindowCompositionRenderTests; no pixels are read here). EditMode does not run
        // CanvasScaler.Update, so Draw calls its Handle as the render tests do.
        sealed class Rig : IDisposable
        {
            readonly GameObject _cameraObject;
            readonly GameObject _eventSystemObject;
            RenderTexture _target;
            internal readonly Camera Camera;
            internal readonly Canvas Canvas;
            internal readonly CanvasScaler Scaler;
            internal readonly GraphicRaycaster Raycaster;
            internal readonly EventSystem EventSystem;
            internal RectTransform CanvasRect => (RectTransform)Canvas.transform;

            internal Rig(string scaler)
            {
                _cameraObject = new GameObject("Placement camera (fixture)");
                _cameraObject.hideFlags = HideFlags.DontSave;
                Camera = _cameraObject.AddComponent<Camera>();
                Camera.enabled = false;
                Camera.orthographic = true;
                Camera.clearFlags = CameraClearFlags.SolidColor;
                Camera.nearClipPlane = 0.3f;
                Camera.farClipPlane = 1000f;
                SetTarget(s_renderSize);

                var canvasObject = new GameObject("Placement canvas (fixture)", typeof(RectTransform));
                canvasObject.transform.SetParent(_cameraObject.transform, false);
                Canvas = canvasObject.AddComponent<Canvas>();
                Canvas.renderMode = RenderMode.ScreenSpaceCamera;
                Canvas.worldCamera = Camera;
                Canvas.planeDistance = 100f;
                Scaler = canvasObject.AddComponent<CanvasScaler>();
                switch (scaler)
                {
                    case "ScaleWithScreenSize 1920x1080 match 0.5":
                        Scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                        Scaler.referenceResolution = new Vector2(1920f, 1080f);
                        Scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                        Scaler.matchWidthOrHeight = 0.5f;
                        break;
                    case "ScaleWithScreenSize 960x540":
                        Scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                        Scaler.referenceResolution = new Vector2(960f, 540f);
                        Scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                        Scaler.matchWidthOrHeight = 0.5f;
                        break;
                    case "ConstantPixelSize":
                        Scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                        Scaler.scaleFactor = 1f;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(scaler), scaler, "fixture: unknown canvas scale mode");
                }
                Raycaster = canvasObject.AddComponent<GraphicRaycaster>();
                _eventSystemObject = new GameObject("Placement event system (fixture)", typeof(EventSystem));
                _eventSystemObject.hideFlags = HideFlags.DontSave;
                EventSystem = _eventSystemObject.GetComponent<EventSystem>();
            }

            // A new render size, as a changed screen resolution.
            internal void Resize(Vector2Int size)
            {
                SetTarget(size);
                Draw();
            }

            internal void Draw()
            {
                Canvas.ForceUpdateCanvases();
                SessionTestTools.Call(Scaler, "Handle");
                Canvas.ForceUpdateCanvases();
                Camera.Render();
                Canvas.ForceUpdateCanvases();
            }

            void SetTarget(Vector2Int size)
            {
                RenderTexture previous = _target;
                _target = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                _target.Create();
                Camera.targetTexture = _target;
                if (previous != null)
                {
                    previous.Release();
                    UnityEngine.Object.DestroyImmediate(previous);
                }
            }

            public void Dispose()
            {
                if (Camera != null) Camera.targetTexture = null;
                if (_target != null)
                {
                    _target.Release();
                    UnityEngine.Object.DestroyImmediate(_target);
                }
                UnityEngine.Object.DestroyImmediate(_eventSystemObject);
                UnityEngine.Object.DestroyImmediate(_cameraObject);
            }
        }

        Rig NewRig(string scaler)
        {
            var rig = new Rig(scaler);
            _rigs.Add(rig);
            rig.Draw();
            return rig;
        }

        // A 390×720 window under the canvas with a 366×44 title bar 12 units in and 10 down from its top-left (the
        // inventory's Header rectangle), with the given anchors and pivot.
        static Fixture BuildWindow(Rig rig, string name, Vector2 anchor, Vector2 pivot)
        {
            var window = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            window.SetParent(rig.CanvasRect, false);
            window.anchorMin = anchor;
            window.anchorMax = anchor;
            window.pivot = pivot;
            window.sizeDelta = s_windowSize;
            var titleBar = (RectTransform)new GameObject("TitleBar", typeof(RectTransform)).transform;
            titleBar.SetParent(window, false);
            titleBar.anchorMin = new Vector2(0f, 1f);
            titleBar.anchorMax = new Vector2(1f, 1f);
            titleBar.pivot = new Vector2(0.5f, 1f);
            titleBar.sizeDelta = new Vector2(-24f, 44f);
            titleBar.anchoredPosition = new Vector2(0f, -10f);
            return new Fixture { Window = window, TitleBar = titleBar };
        }

        // A window attached with the key and placed in the owner area, as a window rebuilt in a new scene would be.
        static Rect PlacedNewWindow(Rig rig, string key)
        {
            Fixture window = BuildWindow(rig, "Window " + key, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            UiWindowPlacement placement = UiWindowPlacement.Attach(window.Window, window.TitleBar, key, MaxScale, UiWindowAlignment.TopLeft);
            rig.Draw();
            placement.Place(OwnerArea(rig));
            rig.Draw();
            return InCanvas(rig, window.Window);
        }

        // An owner's default area with room around it: the canvas's left half (12 units in) and middle half in height,
        // read from the canvas every frame as an owning window does.
        static Rect OwnerArea(Rig rig)
        {
            Rect canvas = rig.CanvasRect.rect;
            return Rect.MinMaxRect(canvas.xMin + 12f, canvas.center.y - canvas.height * 0.25f, canvas.center.x, canvas.center.y + canvas.height * 0.25f);
        }

        // The whole canvas less 12 units on every side.
        static Rect FullArea(Rig rig)
        {
            Rect canvas = rig.CanvasRect.rect;
            return Rect.MinMaxRect(canvas.xMin + 12f, canvas.yMin + 12f, canvas.xMax - 12f, canvas.yMax - 12f);
        }

        // A pointer drag on the grab area as an input module sends it: raycast at the grab area's centre, pointer down,
        // the potential drag, begin drag with the first move, drag for every move, end drag and pointer up, with position,
        // pressPosition, delta and the raycasts filled as a real drag fills them. Between moves the owner places the
        // window and the canvas draws, as one frame would.
        static void Drag(Rig rig, UiWindowPlacement placement, Func<Rect> area, Vector2 delta, int steps)
        {
            Vector2 press = OnScreen(rig, placement.GrabArea).center;
            var data = new PointerEventData(rig.EventSystem)
            {
                position = press,
                pressPosition = press,
                delta = Vector2.zero,
                button = PointerEventData.InputButton.Left,
                useDragThreshold = true,
                eligibleForClick = true,
            };
            var hits = new List<RaycastResult>();
            rig.Raycaster.Raycast(data, hits);
            Assert.IsTrue(hits.Count > 0 && hits[0].gameObject == placement.GrabArea.gameObject,
                $"fixture: a pointer at the grab area centre {press} hits the grab area first");
            data.pointerCurrentRaycast = hits[0];
            data.pointerPressRaycast = hits[0];
            GameObject hit = hits[0].gameObject;
            GameObject pressed = ExecuteEvents.ExecuteHierarchy(hit, data, ExecuteEvents.pointerDownHandler);
            data.pointerPress = pressed;
            data.rawPointerPress = hit;
            GameObject dragged = ExecuteEvents.GetEventHandler<IDragHandler>(hit);
            Assert.IsNotNull(dragged, "T3 the grab area takes pointer drags");
            data.pointerDrag = dragged;
            ExecuteEvents.Execute(dragged, data, ExecuteEvents.initializePotentialDrag);
            for (int step = 1; step <= steps; step++)
            {
                Vector2 previous = data.position;
                data.position = press + delta * step / steps;
                data.delta = data.position - previous;
                if (step == 1)
                {
                    ExecuteEvents.Execute(dragged, data, ExecuteEvents.beginDragHandler);
                    data.dragging = true;
                }
                ExecuteEvents.Execute(dragged, data, ExecuteEvents.dragHandler);
                placement.Place(area());
                rig.Draw();
            }
            data.delta = Vector2.zero;
            ExecuteEvents.Execute(dragged, data, ExecuteEvents.endDragHandler);
            data.dragging = false;
            if (pressed != null) ExecuteEvents.Execute(pressed, data, ExecuteEvents.pointerUpHandler);
            data.pointerDrag = null;
            data.pointerPress = null;
            data.rawPointerPress = null;
            placement.Place(area());
            rig.Draw();
        }

        // ---------------------------------------------------------------------------------------------------------
        // Geometry
        // ---------------------------------------------------------------------------------------------------------

        // A rectangle in the space's local UI units, whatever the anchors, pivots or scale in between.
        static Rect InSpaceOf(RectTransform space, RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector3 a = space.InverseTransformPoint(corners[0]);
            Vector3 c = space.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y), Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
        }

        // The rectangle in the root canvas's local units, the space Place's area is given in.
        static Rect InCanvas(Rig rig, RectTransform target) => InSpaceOf(rig.CanvasRect, target);

        // Screen pixels on the camera's render target.
        static Rect OnScreen(Rig rig, RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector2 a = RectTransformUtility.WorldToScreenPoint(rig.Camera, corners[0]);
            Vector2 c = RectTransformUtility.WorldToScreenPoint(rig.Camera, corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y), Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
        }

        static bool SameRect(Rect a, Rect b) =>
            Mathf.Abs(a.xMin - b.xMin) <= Tolerance && Mathf.Abs(a.xMax - b.xMax) <= Tolerance &&
            Mathf.Abs(a.yMin - b.yMin) <= Tolerance && Mathf.Abs(a.yMax - b.yMax) <= Tolerance;

        static bool Inside(Rect outer, Rect inner) =>
            inner.xMin >= outer.xMin - Tolerance && inner.xMax <= outer.xMax + Tolerance &&
            inner.yMin >= outer.yMin - Tolerance && inner.yMax <= outer.yMax + Tolerance;
    }
}
