using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dawnholder.Client.Audio;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Dungeon goal C9's two screen defects (01_Phases/goals/2026-10-10-dungeon-clear-rewards/goal.md 「C9 실화면 — 차단」):
    // S-1 the reward window shares screen area with the open inventory panel; S-2 the existing STAGE CLEAR banner keeps
    // covering the reward rows. Real Town/UI scenes, the scripted TCP peer, real 15 and 39 frames and the I key; the test
    // places and shows nothing itself. Rectangles come from RectTransform corners and TMP glyph bounds of the live
    // ScreenSpaceOverlay canvases (HudOverlapProbe's convention), never from the products' layout formulas, and every
    // judged value is logged with the actual Screen size. Covering is geometric: canvas sorting orders are logged only.
    public sealed class DungeonClearRewardOverlapSceneTests
    {
        const ushort ClearResultId = 39;
        const ushort ClearConfirmId = 40;
        const int ClearResultLength = 33;
        const int ClearConfirmLength = 5;
        const int MaxHp = 100;
        const float OpenSeconds = 3f;
        const int BossEntityId = 7;
        const int MaterialCount = 3;
        const int CoinPouchCount = 7;
        const string ConfirmLabel = "확인";

        // The banner's existing fade-in lasts 0.4 s of game time. Covering is judged only after that fade and a margin,
        // counted from the later of the 15's application and the window's appearance, for JudgeSeconds of game time.
        const float BannerFadeSeconds = 0.4f;
        const float FadeMarginSeconds = 0.05f;
        const float JudgeSeconds = 0.6f;

        // A shown banner reads as fully drawn at this alpha.
        const float FullAlpha = 0.99f;

        // Half a pixel absorbs float corners of rectangles that only touch (HudOverlapProbe's Contains does the same).
        const float EdgeTolerance = 0.5f;

        MapEntryPlayFixture _fixture;
        int _hpMarker;

        static string MaterialName => ItemCatalog.For(ItemId.Material).DisplayName;
        static string CoinPouchName => ItemCatalog.For(ItemId.CoinPouch).DisplayName;
        static Rect ScreenArea => new Rect(0f, 0f, Screen.width, Screen.height);

        [SetUp]
        public void Setup()
        {
            _fixture = new MapEntryPlayFixture();
            _hpMarker = 40;
        }

        [UnityTearDown] public IEnumerator Teardown() => _fixture.Cleanup();

        // S-1, panel first: a player keeps the inventory open through the boss fight and the result arrives. Both stay
        // shown, the window with its gold, item rows and 「확인」 stays on screen, and no part of the window lies on the panel.
        [UnityTest]
        public IEnumerator OpenPanelThenResult_TheRewardWindowSharesNoScreenAreaWithThePanel()
        {
            const int gold = 1234567;
            yield return EnterReadyTown();
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
            bool opened = false;
            yield return OpenSuccessWindow(gold, met => opened = met);
            Assert.IsTrue(opened, "precondition: a valid success result opens the window over the open panel");
            yield return WaitSettled(() => WindowView.Read(gold).Bounds, "the reward window");

            WindowView window = WindowView.Read(gold);
            float panelAlpha = InventoryPanelProbe.Group.alpha;
            Rect panel = PanelRect();
            float overlap = OverlapArea(window.Bounds, panel);
            float controlOverlap = PanelControlOverlap(window.Bounds, out bool controlRestored);
            LogLayout("panel opened, then result", window, panel, overlap, controlOverlap);

            AssertWindowReadable("panel opened, then result", window);
            Assert.AreEqual(1f, panelAlpha, "S-1 both shown: the open inventory panel stays shown with the reward window");
            Assert.Greater(Area(panel), 0f, "fixture: the panel has a screen rectangle to compare");
            Assert.Greater(controlOverlap, 0f, "control: the panel moved onto the window centre is reported by the same reading");
            Assert.IsTrue(controlRestored, "fixture: the control restored the panel rectangle");
            Assert.AreEqual(0f, overlap,
                $"C9 S-1 the reward window {window.Bounds} shares no screen area with the open panel {panel} " +
                $"on {Screen.width}x{Screen.height}");
        }

        // S-1, window first: I opens and then closes the panel while the window is up. In every state the gold, both
        // item rows and an interactable 「확인」 stay shown and on screen, and the open panel shares no area with the window.
        // A real pointer click on 「확인」 then sends one five-byte 40, closes the window and leaves its spots raycast-free.
        [UnityTest]
        public IEnumerator ResultThenPanelToggle_KeepsRewardsAndConfirm_AndTheOpenPanelSharesNoAreaWithTheWindow()
        {
            const int gold = 24680;
            yield return EnterReadyTown();
            bool opened = false;
            yield return OpenSuccessWindow(gold, met => opened = met);
            Assert.IsTrue(opened, "precondition: a valid success result opens the window");
            yield return WaitSettled(() => WindowView.Read(gold).Bounds, "the reward window");
            WindowView beforePanel = WindowView.Read(gold);

            yield return InventoryPanelProbe.OpenWithI(_fixture);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
            yield return WaitSettled(() => WindowView.Read(gold).Bounds, "the reward window with the panel open");
            WindowView withPanel = WindowView.Read(gold);
            float panelAlpha = InventoryPanelProbe.Group.alpha;
            Rect panel = PanelRect();
            float overlap = OverlapArea(withPanel.Bounds, panel);
            float controlOverlap = PanelControlOverlap(withPanel.Bounds, out bool controlRestored);
            LogLayout("result, then panel opened", withPanel, panel, overlap, controlOverlap);

            yield return PressI();
            yield return WaitSettled(() => WindowView.Read(gold).Bounds, "the reward window after the panel closed");
            WindowView afterPanel = WindowView.Read(gold);
            float panelAlphaAfter = InventoryPanelProbe.Group.alpha;

            yield return ScreenUiProbe.WaitForEventSystem();
            bool clickable = afterPanel.Shown && afterPanel.GoldText != null;
            bool openWindowTakesRaycast = false;
            string[] windowHitsAfterClick = Array.Empty<string>();
            if (clickable)
            {
                HashSet<GameObject> windowGroup = ScreenUiProbe.GroupBelowCanvas(afterPanel.Confirm);
                windowGroup.UnionWith(ScreenUiProbe.GroupBelowCanvas(afterPanel.GoldText));
                Vector2[] spots = { ScreenUiProbe.ScreenPoint(afterPanel.Confirm), ScreenUiProbe.ScreenPoint(afterPanel.GoldText) };
                openWindowTakesRaycast = ScreenUiProbe.RaycastHits(spots[0]).Any(windowGroup.Contains);
                yield return InventoryPanelProbe.ClickAt(_fixture.Mouse, spots[0]);
                yield return ScreenUiProbe.WaitUntil(() => Confirms().Count > 0, OpenSeconds, _ => { });
                yield return _fixture.Frames(5);
                windowHitsAfterClick = spots.SelectMany(ScreenUiProbe.RaycastHits)
                    .Where(windowGroup.Contains)
                    .Select(hit => hit.name)
                    .ToArray();
            }
            List<byte[]> confirms = Confirms();
            bool closedByConfirm = ConfirmButtons().Length == 0;
            Debug.Log($"[dungeon C9 S-1] toggle: panel alpha open {panelAlpha:F2} closed {panelAlphaAfter:F2}; " +
                $"window before panel {beforePanel.Bounds}, after panel {afterPanel.Bounds}; open window takes raycast " +
                $"{openWindowTakesRaycast}; confirms {confirms.Count}; closed {closedByConfirm}; window hits after click " +
                $"{windowHitsAfterClick.Length}");

            AssertWindowReadable("result, panel still closed", beforePanel);
            AssertWindowReadable("result, panel opened", withPanel);
            AssertWindowReadable("result, panel closed again", afterPanel);
            Assert.AreEqual(1f, panelAlpha, "S-1 both shown: I opens the panel while the reward window is up");
            Assert.AreEqual(0f, panelAlphaAfter, "precondition: the next I closes the panel");
            Assert.Greater(controlOverlap, 0f, "control: the panel moved onto the window centre is reported by the same reading");
            Assert.IsTrue(controlRestored, "fixture: the control restored the panel rectangle");
            Assert.IsTrue(openWindowTakesRaycast, "control: the open window takes the raycast at its 「확인」");
            Assert.AreEqual(1, confirms.Count, "preserved: a pointer click on 「확인」 sends C_DungeonClearConfirm exactly once");
            Assert.AreEqual(ClearConfirmLength, confirms[0].Length, "preserved: the confirm frame is five bytes");
            Assert.AreEqual(0, confirms[0][4], "preserved: reserved is 0");
            Assert.IsTrue(closedByConfirm, "preserved: 「확인」 closes the window");
            Assert.IsEmpty(windowHitsAfterClick, "preserved: the closed window's spots take no UI raycast");
            Assert.AreEqual(0f, overlap,
                $"C9 S-1 the reward window {withPanel.Bounds} shares no screen area with the panel {panel} opened over it " +
                $"on {Screen.width}x{Screen.height}");
        }

        // S-2 control and preserved behaviour: a stage clear alone shows the banner fully, with ink on screen, and opens
        // no reward window. The StageClear SFX is only observed through AudioManager's state; nothing is heard (muted).
        [UnityTest]
        public IEnumerator StageClearAlone_ShowsTheBannerFullyOnScreen()
        {
            yield return EnterReadyTown();
            bool bannerPresent = StageClearUI.Instance != null;
            CoverSample before = Cover(0);
            string sfxBefore = SfxState();
            _fixture.Peer.Send(StageClear(BossEntityId));
            float stageClearAt = 0f;
            yield return AfterEarlierPacketsApply(at => stageClearAt = at);
            var samples = new List<CoverSample>();
            yield return SampleCover(0, stageClearAt + BannerFadeSeconds + FadeMarginSeconds + JudgeSeconds, samples);
            CoverSample peak = samples.OrderByDescending(sample => sample.BannerAlpha).First();
            Debug.Log($"[dungeon C9 S-2] stage clear alone: screen {Screen.width}x{Screen.height}; banner {BannerInfo()}; " +
                $"before {before}; 15 applied by t={stageClearAt:F3}; samples {samples.Count}; peak {peak}; " +
                $"SFX before [{sfxBefore}] after [{SfxState()}] (code path only, listener muted)");

            Assert.IsTrue(bannerPresent, "fixture: the Town scene has the stage clear banner");
            Assert.IsFalse(before.WindowShown, "precondition: no reward window before the stage clear");
            Assert.AreEqual(0f, before.BannerAlpha, "preserved: no banner is drawn before a stage clear");
            Assert.IsFalse(samples.Any(sample => sample.WindowShown), "preserved: a stage clear alone opens no reward window");
            Assert.GreaterOrEqual(peak.BannerAlpha, FullAlpha, "preserved: the 15 shows the banner fully");
            Assert.Greater(peak.BannerOnScreen, 0f, "preserved: the shown banner has ink on screen");
        }

        // S-2, 15 then 39, as the boss dies: the banner is fading in when the result opens the window. After the fade the
        // banner covers none of the gold, item rows or 「확인」, and an old fade does not bring it back over them.
        [UnityTest]
        public IEnumerator StageClearThenResult_AfterTheFadeTheBannerCoversNoRewardTextOrConfirm()
        {
            const int gold = 13579;
            yield return EnterReadyTown();
            string sfxBefore = SfxState();
            _fixture.Peer.Send(StageClear(BossEntityId));
            float stageClearAt = 0f;
            yield return AfterEarlierPacketsApply(at => stageClearAt = at);
            bool bannerShown = false;
            yield return ScreenUiProbe.WaitUntil(() => MaxAlpha(BannerGraphics()) > 0f, OpenSeconds, met => bannerShown = met);
            CoverSample beforeResult = Cover(gold);
            string sfxAfter = SfxState();

            bool opened = false;
            yield return OpenSuccessWindow(gold, met => opened = met);
            float windowShownAt = Time.time;
            float judgeStart = Mathf.Max(stageClearAt, windowShownAt) + BannerFadeSeconds + FadeMarginSeconds;
            var samples = new List<CoverSample>();
            yield return SampleCover(gold, judgeStart + JudgeSeconds, samples);
            CoverSample[] judged = samples.Where(sample => sample.Time >= judgeStart).ToArray();
            CoverSample[] covered = judged.Where(sample => sample.CoveredArea > 0f).ToArray();
            LogCover("15 then 39", stageClearAt, windowShownAt, judgeStart, samples,
                $"before 39 {beforeResult}; SFX before [{sfxBefore}] after 15 [{sfxAfter}] (code path only, listener muted)");

            Assert.IsTrue(bannerShown, "precondition: the 15 shows the banner before the result arrives");
            Assert.Greater(beforeResult.BannerOnScreen, 0f, "precondition: the shown banner has ink on screen");
            Assert.IsTrue(opened, "precondition: the result opens the window during the banner's fade");
            Assert.Greater(judged.Length, 0, "fixture: frames after the fade were judged");
            Assert.AreEqual(0, judged.Count(sample => !sample.Readable),
                "preserved: the gold, both item rows and 「확인」 stay shown through the judged frames");
            Assert.AreEqual(0, covered.Length,
                $"C9 S-2 after the fade the banner covers no reward text or 「확인」 on {Screen.width}x{Screen.height}: " +
                $"{covered.Length} of {judged.Length} judged frames covered, first {FirstOrNone(covered)}");
        }

        // S-2, 39 then 15: the window is already up when the stage clear arrives. After the fade the banner covers none
        // of the gold, item rows or 「확인」.
        [UnityTest]
        public IEnumerator ResultThenStageClear_AfterTheFadeTheBannerCoversNoRewardTextOrConfirm()
        {
            const int gold = 97531;
            yield return EnterReadyTown();
            bool opened = false;
            yield return OpenSuccessWindow(gold, met => opened = met);
            float windowShownAt = Time.time;
            yield return WaitSettled(() => WindowView.Read(gold).Bounds, "the reward window");
            bool bannerPresent = StageClearUI.Instance != null;
            CoverSample beforeStageClear = Cover(gold);
            string sfxBefore = SfxState();

            _fixture.Peer.Send(StageClear(BossEntityId));
            float stageClearAt = 0f;
            yield return AfterEarlierPacketsApply(at => stageClearAt = at);
            float judgeStart = Mathf.Max(stageClearAt, windowShownAt) + BannerFadeSeconds + FadeMarginSeconds;
            var samples = new List<CoverSample>();
            yield return SampleCover(gold, judgeStart + JudgeSeconds, samples);
            CoverSample[] judged = samples.Where(sample => sample.Time >= judgeStart).ToArray();
            CoverSample[] covered = judged.Where(sample => sample.CoveredArea > 0f).ToArray();
            LogCover("39 then 15", stageClearAt, windowShownAt, judgeStart, samples,
                $"before 15 {beforeStageClear}; SFX before [{sfxBefore}] after [{SfxState()}] (code path only, listener muted)");

            Assert.IsTrue(opened, "precondition: a valid success result opens the window");
            Assert.IsTrue(bannerPresent, "fixture: the Town scene has the stage clear banner to judge");
            Assert.IsTrue(beforeStageClear.Readable, "precondition: the window's gold, item rows and 「확인」 are shown");
            Assert.AreEqual(0f, beforeStageClear.BannerAlpha, "preserved: no banner is drawn before a stage clear");
            Assert.Greater(judged.Length, 0, "fixture: frames after the fade were judged");
            Assert.AreEqual(0, judged.Count(sample => !sample.Readable),
                "preserved: the gold, both item rows and 「확인」 stay shown through the judged frames");
            Assert.AreEqual(0, covered.Length,
                $"C9 S-2 after the fade the banner covers no reward text or 「확인」 on {Screen.width}x{Screen.height}: " +
                $"{covered.Length} of {judged.Length} judged frames covered, first {FirstOrNone(covered)}");
        }

        // S-2 boundary kept by any fix: after 15, 39 and 「확인」 the next independent stage clear shows the banner fully
        // again and opens no window. Today the banner never hides, so this passing alone does not prove a re-show path.
        [UnityTest]
        public IEnumerator AfterConfirm_TheNextStageClearShowsTheBannerAgain()
        {
            const int gold = 86420;
            yield return EnterReadyTown();
            _fixture.Peer.Send(StageClear(BossEntityId));
            yield return AfterEarlierPacketsApply(_ => { });
            bool bannerShown = false;
            yield return ScreenUiProbe.WaitUntil(() => MaxAlpha(BannerGraphics()) > 0f, OpenSeconds, met => bannerShown = met);
            bool opened = false;
            yield return OpenSuccessWindow(gold, met => opened = met);
            yield return GameSeconds(BannerFadeSeconds + FadeMarginSeconds);

            Component[] buttons = ConfirmButtons();
            if (buttons.Length == 1) ScreenUiProbe.Press(buttons[0]);
            yield return ScreenUiProbe.WaitUntil(() => Confirms().Count > 0, OpenSeconds, _ => { });
            yield return _fixture.Frames(5);
            int confirms = Confirms().Count;
            bool closedByConfirm = ConfirmButtons().Length == 0;

            _fixture.Peer.Send(StageClear(BossEntityId + 1));
            float nextClearAt = 0f;
            yield return AfterEarlierPacketsApply(at => nextClearAt = at);
            var samples = new List<CoverSample>();
            yield return SampleCover(gold, nextClearAt + BannerFadeSeconds + FadeMarginSeconds + JudgeSeconds, samples);
            CoverSample peak = samples.OrderByDescending(sample => sample.BannerAlpha).First();
            float lowest = samples.Min(sample => sample.BannerAlpha);
            Debug.Log($"[dungeon C9 S-2] after confirm: screen {Screen.width}x{Screen.height}; banner {BannerInfo()}; " +
                $"confirms {confirms}; closed {closedByConfirm}; next 15 applied by t={nextClearAt:F3}; samples {samples.Count}; " +
                $"lowest banner alpha after it {lowest:F2}; peak {peak}");

            Assert.IsTrue(bannerShown, "precondition: the first 15 shows the banner");
            Assert.IsTrue(opened, "precondition: the result opens the window");
            Assert.AreEqual(1, buttons.Length, "precondition: one 「확인」 to press");
            Assert.AreEqual(1, confirms, "preserved: 「확인」 sends C_DungeonClearConfirm once");
            Assert.IsTrue(closedByConfirm, "preserved: 「확인」 closes the window");
            Assert.IsFalse(samples.Any(sample => sample.WindowShown), "preserved: the next stage clear alone opens no window");
            Assert.GreaterOrEqual(peak.BannerAlpha, FullAlpha, "preserved: the next independent 15 shows the banner fully");
            Assert.Greater(peak.BannerOnScreen, 0f, "preserved: that banner has ink on screen");
        }

        IEnumerator EnterReadyTown()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp(_hpMarker, MaxHp);
            yield return _fixture.WaitMap(0);
        }

        IEnumerator OpenSuccessWindow(int gold, Action<bool> done)
        {
            _fixture.Peer.Send(Wire(ClearResult((byte)InventoryResult.Success, gold,
                ((int)ItemId.Material, MaterialCount), ((int)ItemId.CoinPouch, CoinPouchCount))));
            yield return ScreenUiProbe.WaitUntil(() => WindowView.Read(gold).Readable, OpenSeconds, done);
        }

        // The peer writes in order on one TCP stream and the client applies packets in order on the main thread, so
        // once this HP value shows, every packet sent before it has been handled. The game time then is reported as an
        // upper bound of when those packets were applied.
        IEnumerator AfterEarlierPacketsApply(Action<float> appliedBy)
        {
            int marker = ++_hpMarker;
            _fixture.Hp(marker, MaxHp);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.CurrentHp == marker, "HP marker " + marker, 3);
            appliedBy(Time.time);
        }

        // One press and release of I through the fixture keyboard, each followed by two frames.
        IEnumerator PressI()
        {
            _fixture.Keys(Key.I);
            yield return _fixture.Frames(2);
            _fixture.Keys();
            yield return _fixture.Frames(2);
        }

        // The peer's raw frames: C_DungeonClearConfirm's generated reader is not needed to count five-byte confirms.
        List<byte[]> Confirms() => MapEntryPlayFixture.Field<ConcurrentQueue<byte[]>>(_fixture.Peer, "_received")
            .Where(frame => BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(2, 2)) == ClearConfirmId)
            .ToList();

        static void AssertWindowReadable(string moment, WindowView window)
        {
            Assert.AreEqual(1, window.ButtonCount, $"{moment}: the reward window is shown with one 「확인」");
            Assert.Greater(window.Graphics.Length, 0, $"{moment}: the window has drawn graphics to measure");
            Assert.IsNotNull(window.GoldText, $"{moment}: the window shows the received gold");
            Assert.IsTrue(window.MaterialRow, $"{moment}: the window shows {MaterialName} ×{MaterialCount}");
            Assert.IsTrue(window.CoinPouchRow, $"{moment}: the window shows {CoinPouchName} ×{CoinPouchCount}");
            Assert.IsTrue(window.ConfirmInteractable, $"{moment}: 「확인」 can be pressed");
            Assert.IsTrue(Contains(ScreenArea, window.Bounds),
                $"{moment}: the whole window {window.Bounds} is on the {Screen.width}x{Screen.height} screen");
            Assert.IsTrue(Contains(ScreenArea, window.ConfirmRect), $"{moment}: 「확인」 {window.ConfirmRect} is on screen");
            foreach (Drawn text in window.RewardTexts)
            {
                Assert.IsTrue(Contains(ScreenArea, text.Ink), $"{moment}: reward text {text} is on screen");
            }
        }

        // Every frame until `until` (game time, the clock the banner fades on), the window's state and the banner's cover
        // over the window's reward texts and 「확인」. A real-time limit keeps a stalled game clock from hanging the test.
        static IEnumerator SampleCover(int gold, float until, List<CoverSample> samples)
        {
            float realDeadline = Time.realtimeSinceStartup + 10f;
            do
            {
                samples.Add(Cover(gold));
                yield return null;
            }
            while (Time.time <= until && Time.realtimeSinceStartup < realDeadline);
            samples.Add(Cover(gold));
        }

        static IEnumerator GameSeconds(float seconds)
        {
            float end = Time.time + seconds;
            float realDeadline = Time.realtimeSinceStartup + 10f;
            while (Time.time < end && Time.realtimeSinceStartup < realDeadline) yield return null;
        }

        static IEnumerator WaitSettled(Func<Rect> read, string label)
        {
            Rect previous = read();
            int still = 0;
            float deadline = Time.realtimeSinceStartup + 3f;
            while (still < 2)
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, label + " never settled");
                yield return null;
                Rect current = read();
                still = current == previous ? still + 1 : 0;
                previous = current;
            }
        }

        // Banner graphics drawn now (alpha above zero) over the window's reward texts or 「확인」, whatever their draw order.
        static CoverSample Cover(int gold)
        {
            WindowView window = WindowView.Read(gold);
            Drawn[] banner = BannerGraphics();
            Drawn[] shownBanner = banner.Where(graphic => graphic.Alpha > 0f).ToArray();
            float bannerOnScreen = shownBanner.Sum(graphic => OverlapArea(graphic.Ink, ScreenArea));
            var pairs = new List<string>();
            float covered = 0f;
            if (window.Shown)
            {
                var targets = window.RewardTexts.Select(text => (name: text.Path, rect: text.Ink))
                    .Append((name: "Confirm button", rect: window.ConfirmRect))
                    .ToArray();
                foreach (Drawn graphic in shownBanner)
                {
                    foreach ((string name, Rect rect) in targets)
                    {
                        float shared = OverlapArea(graphic.Ink, rect);
                        if (shared <= 0f) continue;
                        covered += shared;
                        pairs.Add($"{graphic.Path} over {name} {shared:F2}");
                    }
                }
            }
            return new CoverSample(Time.time, window.Shown, window.Readable, MaxAlpha(banner), bannerOnScreen, covered,
                string.Join("; ", pairs));
        }

        static Drawn[] BannerGraphics()
        {
            StageClearUI banner = StageClearUI.Instance;
            return banner == null ? Array.Empty<Drawn>() : DrawnUnder(banner.transform);
        }

        static float MaxAlpha(Drawn[] graphics) => graphics.Length == 0 ? 0f : graphics.Max(graphic => graphic.Alpha);

        static Rect PanelRect() => ScreenRect((RectTransform)InventoryPanelProbe.Root.Find("Inventory"));

        // Positive control in the same frame, as HudOverlapProbe does for the HUD: with the panel's top centre placed on
        // the window centre, the same reading must report an overlap. The panel's own placement is restored at once.
        static float PanelControlOverlap(Rect window, out bool restored)
        {
            var panel = (RectTransform)InventoryPanelProbe.Root.Find("Inventory");
            Rect before = ScreenRect(panel);
            Vector2 placed = panel.anchoredPosition;
            panel.position = new Vector3(window.center.x, window.center.y, panel.position.z);
            float overlap = OverlapArea(window, ScreenRect(panel));
            panel.anchoredPosition = placed;
            restored = Same(before, ScreenRect(panel));
            return overlap;
        }

        static void LogLayout(string moment, WindowView window, Rect panel, float overlap, float controlOverlap)
        {
            Transform panelRoot = InventoryPanelProbe.Root;
            Debug.Log($"[dungeon C9 S-1] {moment}: screen {Screen.width}x{Screen.height}; window {CanvasInfo(window.Confirm)} " +
                $"buttons {window.ButtonCount} graphics {window.Graphics.Length} bounds {window.Bounds} confirm {window.ConfirmRect} " +
                $"interactable {window.ConfirmInteractable} reward texts {window.RewardTexts.Length}: " +
                $"{string.Join("; ", window.RewardTexts.Select(text => text.ToString()))}; panel alpha " +
                $"{InventoryPanelProbe.Group.alpha:F2} {CanvasInfo(panelRoot)} rect {panel}; window ∩ panel {overlap:F2} px² " +
                $"(raw {Area(Intersect(window.Bounds, panel)):F2}); control {controlOverlap:F2}; other drawn overlays over the " +
                $"window: {string.Join("; ", OverlaysOver(window))}");
        }

        static void LogCover(string order, float stageClearAt, float windowShownAt, float judgeStart,
            List<CoverSample> samples, string extra)
        {
            CoverSample[] judged = samples.Where(sample => sample.Time >= judgeStart).ToArray();
            CoverSample[] covered = judged.Where(sample => sample.CoveredArea > 0f).ToArray();
            int firstClear = samples.FindIndex(sample => sample.WindowShown && sample.CoveredArea <= 0f);
            bool coveredAgain = firstClear >= 0 && samples.Skip(firstClear).Any(sample => sample.CoveredArea > 0f);
            CoverSample last = samples[samples.Count - 1];
            WindowView window = WindowView.Read(0);
            Debug.Log($"[dungeon C9 S-2] {order}: screen {Screen.width}x{Screen.height}; banner {BannerInfo()}; window " +
                $"{CanvasInfo(window.Confirm)}; 15 applied by t={stageClearAt:F3}; window shown t={windowShownAt:F3}; judged " +
                $"from t={judgeStart:F3}; samples {samples.Count}; judged {judged.Length}; judged covered {covered.Length}; max " +
                $"covered area {(judged.Length == 0 ? 0f : judged.Max(sample => sample.CoveredArea)):F2}; first judged cover " +
                $"{FirstOrNone(covered)}; covered again after a clear frame {coveredAgain}; last {last}; {extra}");
        }

        static string FirstOrNone(CoverSample[] samples) => samples.Length == 0 ? "none" : samples[0].ToString();

        static string BannerInfo()
        {
            StageClearUI banner = StageClearUI.Instance;
            if (banner == null) return "absent";
            Drawn[] graphics = DrawnUnder(banner.transform);
            return $"{CanvasInfo(banner)} graphics {graphics.Length}: " +
                string.Join("; ", graphics.Select(graphic => $"{graphic} box {graphic.Box}"));
        }

        static string CanvasInfo(Component component)
        {
            Canvas canvas = component != null ? component.GetComponentInParent<Canvas>() : null;
            if (canvas == null) return "(no canvas)";
            Canvas root = canvas.rootCanvas;
            return $"(canvas {root.name} {root.renderMode} sort {root.sortingOrder} scale {root.scaleFactor:F4})";
        }

        // Screen review aid only: every other drawn overlay graphic on the window, with its canvas order.
        static string[] OverlaysOver(WindowView window)
        {
            if (!window.Shown) return Array.Empty<string>();
            Canvas windowCanvas = window.Confirm.GetComponentInParent<Canvas>().rootCanvas;
            var found = new List<string>();
            foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>())
            {
                if (!canvas.isRootCanvas || canvas == windowCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                foreach (Drawn graphic in DrawnUnder(canvas.transform))
                {
                    if (graphic.Alpha > 0f && OverlapArea(graphic.Ink, window.Bounds) > 0f)
                    {
                        found.Add($"{canvas.name}#{canvas.sortingOrder}/{graphic}");
                    }
                }
            }
            return found.ToArray();
        }

        // AudioManager's pool cursor moves once per played SFX and its cache records each key it resolved. Both show the
        // code path only: the fixture mutes the listener, so nothing is heard.
        static string SfxState()
        {
            AudioManager audio = AudioManager.Instance;
            if (audio == null) return "no AudioManager";
            int cursor = MapEntryPlayFixture.Field<int>(audio, "_sfxIndex");
            var cache = MapEntryPlayFixture.Field<Dictionary<string, AudioClip>>(audio, "_clipCache");
            string clip = !cache.TryGetValue(SoundKeys.StageClear, out AudioClip resolved) ? "not resolved"
                : resolved != null ? resolved.name : "missing";
            return $"sfx cursor {cursor}, stage clear clip {clip}";
        }

        // Every active, enabled, unculled graphic under the root whose ink has area, with its alpha as drawn now: the
        // graphic's colour alpha times its CanvasGroups', zero under a disabled canvas.
        static Drawn[] DrawnUnder(Transform root)
        {
            var drawn = new List<Drawn>();
            foreach (CanvasRenderer renderer in root.GetComponentsInChildren<CanvasRenderer>())
            {
                Component graphic = renderer.GetComponents<Component>().FirstOrDefault(IsGraphic);
                if (graphic == null || !((Behaviour)graphic).isActiveAndEnabled || renderer.cull) continue;
                var rect = (RectTransform)renderer.transform;
                Rect ink = Ink(graphic, rect);
                if (Area(ink) <= 0f) continue;
                drawn.Add(new Drawn(renderer.gameObject, root.name + "/" + PathOf(rect, root), ScreenRect(rect), ink,
                    DrawnAlpha(graphic)));
            }
            return drawn.ToArray();
        }

        static float DrawnAlpha(Component graphic)
        {
            if (!graphic.GetComponentsInParent<Canvas>().All(canvas => canvas.enabled)) return 0f;
            float alpha = graphic is TMP_Text text ? text.color.a : ((Color)graphic.GetType().GetProperty("color").GetValue(graphic)).a;
            for (Transform current = graphic.transform; current != null; current = current.parent)
            {
                var group = current.GetComponent<CanvasGroup>();
                if (group == null || !group.enabled) continue;
                alpha *= group.alpha;
                if (group.ignoreParentGroups) break;
            }
            return alpha;
        }

        // TMP texts by their laid-out glyph bounds, so an empty or oversized text box is not mistaken for ink; other
        // graphics by their rectangle, letterboxed when a Simple Image preserves its sprite's aspect.
        static Rect Ink(Component graphic, RectTransform rect)
        {
            if (graphic is TMP_Text label)
            {
                bool laidOut = !string.IsNullOrEmpty(label.text) && label.textInfo != null && label.textInfo.characterCount > 0;
                return laidOut ? Bounds(rect, label.textBounds) : Rect.zero;
            }
            return PreservedAspect(graphic, rect, ScreenRect(rect));
        }

        // uGUI draws a Simple Image with preserveAspect as its sprite fitted inside the rectangle and aligned by the pivot
        // (Image.PreserveSpriteAspectRatio). This assembly does not reference uGUI, so the Image is read by reflection.
        static Rect PreservedAspect(Component graphic, RectTransform rect, Rect box)
        {
            Type type = graphic.GetType();
            object preserve = type.GetProperty("preserveAspect")?.GetValue(graphic);
            object drawType = type.GetProperty("type")?.GetValue(graphic);
            var sprite = type.GetProperty("overrideSprite")?.GetValue(graphic) as Sprite;
            bool fitted = preserve is bool preserved && preserved && drawType != null && drawType.ToString() == "Simple";
            if (!fitted || sprite == null || sprite.rect.width <= 0f || sprite.rect.height <= 0f || Area(box) <= 0f) return box;
            float spriteRatio = sprite.rect.width / sprite.rect.height;
            if (spriteRatio > box.width / box.height)
            {
                float height = box.width / spriteRatio;
                return new Rect(box.x, box.y + (box.height - height) * rect.pivot.y, box.width, height);
            }
            float width = box.height * spriteRatio;
            return new Rect(box.x + (box.width - width) * rect.pivot.x, box.y, width, box.height);
        }

        static bool IsGraphic(Component component)
        {
            for (Type type = component != null ? component.GetType() : null; type != null; type = type.BaseType)
            {
                if (type.FullName == "UnityEngine.UI.Graphic") return true;
            }
            return false;
        }

        static string PathOf(Transform transform, Transform root)
        {
            var names = new List<string>();
            for (Transform current = transform; current != null && current != root; current = current.parent) names.Insert(0, current.name);
            return string.Join("/", names);
        }

        // ScreenSpaceOverlay canvases place world corners in screen pixels.
        static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        static Rect Bounds(Transform owner, Bounds local)
        {
            Vector3 min = owner.TransformPoint(local.min);
            Vector3 max = owner.TransformPoint(local.max);
            return Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
        }

        static Rect Union(IEnumerable<Rect> rects)
        {
            Rect[] drawn = rects.Where(rect => Area(rect) > 0f).ToArray();
            if (drawn.Length == 0) return Rect.zero;
            return Rect.MinMaxRect(drawn.Min(rect => rect.xMin), drawn.Min(rect => rect.yMin),
                drawn.Max(rect => rect.xMax), drawn.Max(rect => rect.yMax));
        }

        static Rect Intersect(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin),
            Mathf.Min(a.xMax, b.xMax), Mathf.Min(a.yMax, b.yMax));

        static float Area(Rect rect) => rect.width > 0f && rect.height > 0f ? rect.width * rect.height : 0f;

        static float OverlapArea(Rect a, Rect b)
        {
            Rect shared = Intersect(a, b);
            return shared.width > EdgeTolerance && shared.height > EdgeTolerance ? shared.width * shared.height : 0f;
        }

        static bool Same(Rect a, Rect b) => Mathf.Abs(a.xMin - b.xMin) < 0.01f && Mathf.Abs(a.yMin - b.yMin) < 0.01f &&
            Mathf.Abs(a.xMax - b.xMax) < 0.01f && Mathf.Abs(a.yMax - b.yMax) < 0.01f;

        static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - EdgeTolerance &&
            inner.yMin >= outer.yMin - EdgeTolerance && inner.xMax <= outer.xMax + EdgeTolerance &&
            inner.yMax <= outer.yMax + EdgeTolerance;

        static Component[] ConfirmButtons() => ScreenUiProbe.ShownButtons(ConfirmLabel);

        // The item's name, with its count in the same label or in a label beside it (one row of the window).
        static bool ItemRowShows(TMP_Text[] texts, string name, int count) => texts
            .Where(text => text.text.Contains(name))
            .Any(label => texts.Any(other => (other == label || other.transform.parent == label.transform.parent) &&
                HasNumber(other.text.Replace(name, string.Empty), count)));

        // A whole number as a player reads it: digit grouping (1,234,567) is allowed, abbreviation (1.2M) is not.
        static bool HasNumber(string text, long value)
        {
            string ungrouped = Regex.Replace(text ?? string.Empty, @"(?<=\d)[,   ](?=\d{3}(?!\d))", string.Empty);
            return Regex.IsMatch(ungrouped, $@"(?<!\d){value}(?!\d)");
        }

        static ArraySegment<byte> StageClear(int bossEntityId) => new S_StageClear { bossEntityId = bossEntityId }.Write();

        static ArraySegment<byte> Wire(byte[] bytes) => new ArraySegment<byte>(bytes);

        // Goal P2's wire, written independently as DungeonClearRewardWindowSceneTests does (its encoder is private):
        // [ushort size incl. header][ushort id 39], byte result, int gold, then three (int itemId, int count) slots, all
        // LittleEndian. Unused slots stay 0/0.
        static byte[] ClearResult(byte result, int gold, params (int itemId, int count)[] slots)
        {
            var wire = new byte[ClearResultLength];
            BinaryPrimitives.WriteUInt16LittleEndian(wire.AsSpan(0, 2), ClearResultLength);
            BinaryPrimitives.WriteUInt16LittleEndian(wire.AsSpan(2, 2), ClearResultId);
            wire[4] = result;
            BinaryPrimitives.WriteInt32LittleEndian(wire.AsSpan(5, 4), gold);
            for (int i = 0; i < slots.Length; i++)
            {
                BinaryPrimitives.WriteInt32LittleEndian(wire.AsSpan(9 + (i * 8), 4), slots[i].itemId);
                BinaryPrimitives.WriteInt32LittleEndian(wire.AsSpan(13 + (i * 8), 4), slots[i].count);
            }
            return wire;
        }

        // One graphic as drawn this frame: its owner, path, rectangle, ink rectangle and alpha.
        readonly struct Drawn
        {
            internal readonly GameObject Owner;
            internal readonly string Path;
            internal readonly Rect Box;
            internal readonly Rect Ink;
            internal readonly float Alpha;

            internal Drawn(GameObject owner, string path, Rect box, Rect ink, float alpha)
            {
                Owner = owner;
                Path = path;
                Box = box;
                Ink = ink;
                Alpha = alpha;
            }

            public override string ToString() => $"{Path} ink {Ink} alpha {Alpha:F2}";
        }

        // One frame of the S-2 observation: the window's state and how much shown banner ink lies on its rewards.
        readonly struct CoverSample
        {
            internal readonly float Time;
            internal readonly bool WindowShown;
            internal readonly bool Readable;
            internal readonly float BannerAlpha;
            internal readonly float BannerOnScreen;
            internal readonly float CoveredArea;
            internal readonly string Covering;

            internal CoverSample(float time, bool windowShown, bool readable, float bannerAlpha, float bannerOnScreen,
                float coveredArea, string covering)
            {
                Time = time;
                WindowShown = windowShown;
                Readable = readable;
                BannerAlpha = bannerAlpha;
                BannerOnScreen = bannerOnScreen;
                CoveredArea = coveredArea;
                Covering = covering;
            }

            public override string ToString() => $"t={Time:F3} window {WindowShown} readable {Readable} banner alpha " +
                $"{BannerAlpha:F2} on screen {BannerOnScreen:F2} covered {CoveredArea:F2} [{Covering}]";
        }

        // One frame's reading of the reward window, found by its shown 「확인」 button as the other window tests do: its
        // drawn graphics and their bounds, and the gold and item texts a player must read.
        sealed class WindowView
        {
            internal readonly int ButtonCount;
            internal readonly Component Confirm;
            internal readonly Rect ConfirmRect;
            internal readonly bool ConfirmInteractable;
            internal readonly Drawn[] Graphics = Array.Empty<Drawn>();
            internal readonly Rect Bounds;
            internal readonly TMP_Text GoldText;
            internal readonly Drawn[] RewardTexts = Array.Empty<Drawn>();
            internal readonly bool MaterialRow;
            internal readonly bool CoinPouchRow;

            WindowView(int gold)
            {
                Component[] buttons = ConfirmButtons();
                ButtonCount = buttons.Length;
                if (buttons.Length != 1) return;
                Confirm = buttons[0];
                ConfirmRect = ScreenRect((RectTransform)Confirm.transform);
                ConfirmInteractable = InventoryPanelProbe.Interactable(Confirm);
                Transform canvas = Confirm.GetComponentInParent<Canvas>().rootCanvas.transform;
                HashSet<GameObject> group = ScreenUiProbe.GroupBelowCanvas(Confirm);
                Graphics = DrawnUnder(canvas).Where(graphic => graphic.Alpha > 0f && group.Contains(graphic.Owner)).ToArray();
                Bounds = Union(Graphics.Select(graphic => graphic.Ink));
                TMP_Text[] texts = ScreenUiProbe.ShownTextsUnder(canvas);
                GoldText = texts.FirstOrDefault(text => HasNumber(text.text, gold));
                MaterialRow = ItemRowShows(texts, MaterialName, MaterialCount);
                CoinPouchRow = ItemRowShows(texts, CoinPouchName, CoinPouchCount);
                GameObject[] rewardOwners = texts
                    .Where(text => HasNumber(text.text, gold) || text.text.Contains(MaterialName) || text.text.Contains(CoinPouchName))
                    .Select(text => text.gameObject)
                    .ToArray();
                RewardTexts = Graphics.Where(graphic => rewardOwners.Contains(graphic.Owner)).ToArray();
            }

            internal bool Shown => Confirm != null;

            // Gold, both item rows and 「확인」 are shown, each reward text with ink to judge.
            internal bool Readable => Shown && GoldText != null && MaterialRow && CoinPouchRow && RewardTexts.Length >= 3;

            internal static WindowView Read(int gold) => new WindowView(gold);
        }
    }
}
