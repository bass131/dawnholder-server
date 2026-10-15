using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Dawnholder.Client.Audio;
using Dawnholder.Client.Network;
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
    // Independent verification of dungeon goal C9's minimal fix (01_Phases/goals/2026-10-10-dungeon-clear-rewards/goal.md
    // 「C9 실화면 — 차단」 S-1·S-2 under the user decision 「C9 최소 수정 + 계층 따로」) together with the window's P7
    // lifecycle (「던전 패킷·창 PR의 요구」). It reaches what DungeonClearRewardOverlapSceneTests only logs or does not
    // reach: the window against the HUD, the BossRoom where the boss dies, 「확인」 holding its place through I and through
    // the next map's load, and the StageClear SFX path while the banner stays off the rewards. Rectangles come from the
    // live ScreenSpaceOverlay canvases and hits from the EventSystem raycast, never from the window's layout formula.
    // The 39 wire is pinned by DungeonClearRewardWindowSceneTests' independent encoder, so the generated packet is plain
    // input here, as in DungeonClearRewardWindowActiveSceneTests.
    public sealed class DungeonClearRewardPresentationLifecycleTests
    {
        const string ConfirmLabel = "확인";
        const int MaxHp = 100;
        const int BossEntityId = 7;
        const int MaterialCount = 3;
        const int CoinPouchCount = 7;
        const float OpenSeconds = 3f;

        // The banner's existing fade-in lasts 0.4 s of game time; covering is judged after it and a margin.
        const float BannerFadeSeconds = 0.4f;
        const float FadeMarginSeconds = 0.05f;
        const float JudgeSeconds = 0.6f;

        // A banner shown by a standalone 15 reads as fully drawn at this alpha.
        const float FullAlpha = 0.99f;

        // Half a pixel absorbs float corners of rectangles that only touch or are placed again at the same spot.
        const float EdgeTolerance = 0.5f;

        // Frames the next map's window is watched after it first shows and after the map is ready.
        const int WatchFrames = 30;

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

        // The boss dies in the BossRoom while the player keeps the inventory open; the server's 15 and then 39 arrive.
        // S-1: the window shares no screen area with the open panel or any shown HUD graphic, and I closing and reopening
        // the panel moves neither 「확인」 nor a reward text. S-2: after the fade the banner covers no reward text or 「확인」,
        // and the 15 still ran its SFX path. P7: one pointer click with the panel open sends one 40 and closes the window
        // without a raycast target left; the server's move to Town then shows no window and nothing more is sent.
        [UnityTest]
        public IEnumerator BossRoomClearWithThePanelOpen_RewardsShareNoAreaWithHudOrPanel_StayPutThroughI_AndOneClickConfirms()
        {
            const int gold = 31415;
            yield return EnterReadyTown();
            yield return MoveTo(2);
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();

            int controlSfx = -1;
            yield return SfxPlayedBy(() => { }, played => controlSfx = played);
            int stageClearSfx = -1;
            yield return SfxPlayedBy(() => _fixture.Peer.Send(new S_StageClear { bossEntityId = BossEntityId }.Write()),
                played => stageClearSfx = played);
            bool bannerShown = false;
            yield return ScreenUiProbe.WaitUntil(() => BannerAlpha() > 0f, OpenSeconds, met => bannerShown = met);

            SendSuccess(gold);
            bool opened = false;
            yield return ScreenUiProbe.WaitUntil(() => RewardView.Read(gold).Readable, OpenSeconds, met => opened = met);
            float judgeFrom = Time.time + BannerFadeSeconds + FadeMarginSeconds;
            var judged = new List<CoverSample>();
            yield return SampleCover(gold, judgeFrom, judgeFrom + JudgeSeconds, judged);
            yield return WaitSettled(() => RewardView.Read(gold).WindowRect, "the reward window");

            // S-1 with the panel open.
            RewardView withPanel = RewardView.Read(gold);
            // What the BossRoom log line prints for this reading, taken while the window still exists.
            string withPanelLoggedAtRead = withPanel.ToString();
            float panelAlpha = InventoryPanelProbe.Group.alpha;
            Rect panel = PanelRect();
            float panelOverlap = OverlapArea(withPanel.WindowRect, panel);
            Drawn[] hud = HudGraphics();
            string[] shownHudOver = hud.Where(graphic => graphic.Alpha > 0f && OverlapArea(graphic.Ink, withPanel.WindowRect) > 0f)
                .Select(graphic => graphic.ToString()).ToArray();
            string[] hiddenHudOver = hud.Where(graphic => graphic.Alpha <= 0f && OverlapArea(graphic.Ink, withPanel.WindowRect) > 0f)
                .Select(graphic => graphic.ToString()).ToArray();
            float hudControl = HudControlOverlap(withPanel, hud, out bool hudControlRestored);

            // I closes, then reopens the panel under the window.
            yield return PressI();
            RewardView panelClosed = RewardView.Read(gold);
            float panelAlphaClosed = InventoryPanelProbe.Group.alpha;
            yield return PressI();
            RewardView panelReopened = RewardView.Read(gold);
            float panelAlphaReopened = InventoryPanelProbe.Group.alpha;
            float confirmDrift = Mathf.Max(Drift(withPanel.ConfirmRect, panelClosed.ConfirmRect),
                Drift(withPanel.ConfirmRect, panelReopened.ConfirmRect));
            float rewardDrift = Mathf.Max(Drift(withPanel.RewardInk, panelClosed.RewardInk),
                Drift(withPanel.RewardInk, panelReopened.RewardInk));
            bool textsKept = withPanel.Texts.SequenceEqual(panelClosed.Texts) && withPanel.Texts.SequenceEqual(panelReopened.Texts);

            // P7 with the panel still open: one real pointer click.
            yield return ScreenUiProbe.WaitForEventSystem();
            bool clickable = panelReopened.Shown;
            bool openWindowTakesRaycast = false;
            string[] windowHitsAfterClick = Array.Empty<string>();
            if (clickable)
            {
                HashSet<GameObject> window = ScreenUiProbe.GroupBelowCanvas(panelReopened.Confirm);
                Vector2[] spots = { ScreenUiProbe.ScreenPoint(panelReopened.Confirm), panelReopened.WindowRect.center };
                openWindowTakesRaycast = ScreenUiProbe.RaycastHits(spots[0]).Any(window.Contains);
                yield return InventoryPanelProbe.ClickAt(_fixture.Mouse, spots[0]);
                yield return ScreenUiProbe.WaitUntil(() => Confirms() > 0, OpenSeconds, _ => { });
                yield return _fixture.Frames(5);
                windowHitsAfterClick = spots.SelectMany(ScreenUiProbe.RaycastHits).Where(window.Contains)
                    .Select(hit => hit.name).ToArray();
            }
            int confirmsAfterClick = Confirms();
            int reserved = confirmsAfterClick > 0
                ? _fixture.Peer.Last<C_DungeonClearConfirm>(PacketID.C_DungeonClearConfirm).reserved
                : -1;
            bool closedByConfirm = ScreenUiProbe.ShownButtons(ConfirmLabel).Length == 0;
            float bannerAfterConfirm = 0f;
            yield return MaxBannerAlpha(JudgeSeconds, alpha => bannerAfterConfirm = alpha);

            // The server answers the confirmation with the move to Town.
            yield return MoveTo(0);
            yield return _fixture.Frames(10);
            int townButtons = ScreenUiProbe.ShownButtons(ConfirmLabel).Length;
            int confirmsInTown = Confirms();

            // The move to Town unloads BossRoom, so the reading's 「확인」 is a destroyed object by the time it is logged. The
            // log must still print what was read in BossRoom, and a reading that finds no window must print no window values.
            bool withPanelLiveInTown = withPanel.Shown;
            string withPanelLoggedInTown = withPanel.ToString();
            string townReadingLogged = RewardView.Read(gold).ToString();

            Debug.Log($"[dungeon C9 review] BossRoom clear: screen {Screen.width}x{Screen.height}; window {withPanel}; panel " +
                $"alpha {panelAlpha:F2} rect {panel}; window ∩ panel {panelOverlap:F2}; HUD graphics {hud.Length}, shown over " +
                $"window [{string.Join("; ", shownHudOver)}], hidden by alpha over window [{string.Join("; ", hiddenHudOver)}], " +
                $"control {hudControl:F2} restored {hudControlRestored}; banner shown before 39 {bannerShown}; judged frames " +
                $"{judged.Count}, unreadable {judged.Count(sample => !sample.Readable)}, covered " +
                $"{judged.Count(sample => sample.Covered > 0f)}, first {FirstCovered(judged)}; SFX played control {controlSfx} " +
                $"by 15 {stageClearSfx} (code path only, listener muted); I closed alpha {panelAlphaClosed:F2} reopened " +
                $"{panelAlphaReopened:F2}, confirm drift {confirmDrift:F3}, reward drift {rewardDrift:F3}, texts kept {textsKept}; " +
                $"open window takes raycast {openWindowTakesRaycast}; confirms after click {confirmsAfterClick} reserved {reserved}; " +
                $"closed {closedByConfirm}; window hits after click {windowHitsAfterClick.Length}; banner alpha after confirm " +
                $"{bannerAfterConfirm:F2}; Town 「확인」 buttons {townButtons}; confirms in Town {confirmsInTown}");
            Debug.Log($"[dungeon C9 review-2] BossRoom reading logged after Town: live {withPanelLiveInTown}; at read " +
                $"[{withPanelLoggedAtRead}]; in Town [{withPanelLoggedInTown}]; Town reading [{townReadingLogged}]");

            Assert.IsTrue(opened, "precondition: the result opens the BossRoom window over the open panel");
            AssertReadableOnScreen("BossRoom, panel open", withPanel);
            Assert.AreEqual(1f, panelAlpha, "S-1 both shown: the open panel stays shown with the reward window");
            Assert.Greater(Area(panel), 0f, "fixture: the panel has a screen rectangle to compare");
            Assert.AreEqual(0f, panelOverlap, $"C9 S-1 the window {withPanel.WindowRect} shares no area with the open panel {panel}");
            Assert.Greater(hud.Count(graphic => graphic.Alpha > 0f), 0, "fixture: the real HUD draws graphics to compare");
            Assert.Greater(hudControl, 0f, "control: the window moved onto character_status is reported by the same reading");
            Assert.IsTrue(hudControlRestored, "fixture: the control restored the window rectangle");
            Assert.IsEmpty(shownHudOver, "C9 S-1 no shown HUD graphic shares screen area with the reward window");
            Assert.IsTrue(bannerShown, "precondition: the 15 shows the banner before the result arrives");
            Assert.Greater(judged.Count, 0, "fixture: frames after the fade were judged");
            Assert.AreEqual(0, judged.Count(sample => !sample.Readable), "S-2 the rewards and 「확인」 stay shown after the fade");
            Assert.AreEqual(0, judged.Count(sample => sample.Covered > 0f),
                $"C9 S-2 after the fade the banner covers no reward text or 「확인」: first {FirstCovered(judged)}");
            Assert.Greater(stageClearSfx, controlSfx, "S-2 preserved: the 15 still runs the StageClear SFX path");
            Assert.AreEqual(0f, panelAlphaClosed, "precondition: I closes the panel under the window");
            Assert.AreEqual(1f, panelAlphaReopened, "precondition: the next I reopens it");
            AssertReadableOnScreen("BossRoom, panel closed by I", panelClosed);
            AssertReadableOnScreen("BossRoom, panel reopened by I", panelReopened);
            Assert.IsTrue(textsKept, "C9 S-1 I keeps every reward text of the window");
            Assert.LessOrEqual(confirmDrift, EdgeTolerance, "C9 S-1 I moves 「확인」 by no more than half a pixel");
            Assert.LessOrEqual(rewardDrift, EdgeTolerance, "C9 S-1 I moves no reward text by more than half a pixel");
            Assert.IsTrue(openWindowTakesRaycast, "control: the open window takes the raycast at its 「확인」");
            Assert.AreEqual(1, confirmsAfterClick, "P7 one pointer click with the panel open sends C_DungeonClearConfirm once");
            Assert.AreEqual(0, reserved, "P7 reserved is 0");
            Assert.IsTrue(closedByConfirm, "P7 「확인」 closes the window");
            Assert.IsEmpty(windowHitsAfterClick, "P7 the closed window's spots take no UI raycast");
            Assert.AreEqual(0, townButtons, "P7 Town after the confirmed result shows no reward window");
            Assert.AreEqual(1, confirmsInTown, "P7 nothing more is sent on the way to Town");
            Assert.IsFalse(withPanelLiveInTown, "control: the move to Town destroyed the BossRoom 「확인」 that was read");
            StringAssert.Contains(withPanel.WindowRect.ToString(), withPanelLoggedAtRead,
                "log: the BossRoom reading prints the window rectangle it read");
            Assert.AreEqual(withPanelLoggedAtRead, withPanelLoggedInTown,
                "log: after Town destroyed its objects, the BossRoom reading still prints what it read in BossRoom");
            StringAssert.Contains("not shown", townReadingLogged, "log: a reading that finds no window says so");
            StringAssert.DoesNotContain("rect ", townReadingLogged, "log: a reading that finds no window prints no window values");
        }

        // A result that arrives while the next map loads (the F-2 path). From the first frame HuntingGround's window is
        // shown, 「확인」 stays where it appeared and the EventSystem raycast at its centre reaches the window, so a pointer
        // click where a player first saw it, once the map is ready, sends exactly one 40.
        [UnityTest]
        public IEnumerator ResultDuringTheNextMapLoad_ConfirmTakesTheRaycastAndHoldsStillWhileShown_AndAClickWhereItFirstShowedConfirmsOnce()
        {
            const int gold = 27182;
            yield return EnterReadyTown();
            _fixture.Peer.Send(new S_MapTransition { destMapId = 1, spawnX = 2, spawnY = 0 }.Write());
            SendSuccess(gold);

            var samples = new List<ShownConfirm>();
            bool hpSent = false;
            int frame = 0;
            int readyFrame = -1;
            float deadline = Time.realtimeSinceStartup + 20f;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (!hpSent && _fixture.Session.Entry.MapId == 1 && _fixture.Session.Entry.SceneReady)
                {
                    _fixture.Hp(++_hpMarker, MaxHp);
                    hpSent = true;
                }
                foreach (Component button in ScreenUiProbe.ShownButtons(ConfirmLabel)) samples.Add(ShownConfirm.Read(frame, button));
                bool ready = hpSent && _fixture.Session.Entry.MapId == 1 &&
                    _fixture.Session.Entry.State == MapEntryState.Ready && !_fixture.Loader.IsTransitioning;
                if (ready && readyFrame < 0) readyFrame = frame;
                int watched = samples.Count(sample => sample.Scene == "HuntingGround");
                if (readyFrame >= 0 && frame - readyFrame >= WatchFrames && watched >= WatchFrames) break;
                frame++;
                yield return null;
            }
            yield return _fixture.WaitMap(1);

            ShownConfirm[] huntingGround = samples.Where(sample => sample.Scene == "HuntingGround").ToArray();
            ShownConfirm[] town = samples.Where(sample => sample.Scene == "Town").ToArray();
            bool shown = huntingGround.Length > 0;
            Rect first = shown ? huntingGround[0].Rect : Rect.zero;
            int windows = huntingGround.Select(sample => sample.Button).Distinct().Count();
            float drift = shown ? huntingGround.Max(sample => Drift(first, sample.Rect)) : 0f;
            int framesWithoutEventSystem = huntingGround.Count(sample => !sample.EventSystemPresent);
            int framesMissed = huntingGround.Count(sample => !sample.Hit);
            int confirmsBeforeClick = Confirms();

            yield return ScreenUiProbe.WaitForEventSystem();
            if (shown) yield return InventoryPanelProbe.ClickAt(_fixture.Mouse, first.center);
            yield return ScreenUiProbe.WaitUntil(() => Confirms() > 0, OpenSeconds, _ => { });
            yield return _fixture.Frames(5);
            int confirms = Confirms();
            int reserved = confirms > 0 ? _fixture.Peer.Last<C_DungeonClearConfirm>(PacketID.C_DungeonClearConfirm).reserved : -1;
            bool closed = ScreenUiProbe.ShownButtons(ConfirmLabel).Length == 0;

            Debug.Log($"[dungeon C9 review] next map load: screen {Screen.width}x{Screen.height}; frames {frame}, map ready at " +
                $"frame {readyFrame}; Town shown frames {town.Length} (interactable {town.Count(sample => sample.Interactable)}); " +
                $"HuntingGround shown frames {huntingGround.Length} from {(shown ? huntingGround[0].ToString() : "never")} to " +
                $"{(shown ? huntingGround[huntingGround.Length - 1].ToString() : "never")}; windows {windows}; drift {drift:F3}; " +
                $"frames without EventSystem {framesWithoutEventSystem}; frames whose 「확인」 centre missed the window {framesMissed}" +
                $"{(framesMissed > 0 ? " first " + huntingGround.First(sample => !sample.Hit) : string.Empty)}; confirms before " +
                $"click {confirmsBeforeClick}; click at {first.center}; confirms {confirms} reserved {reserved}; closed {closed}");

            Assert.IsTrue(shown, "P7 client.md: the result kept across the load shows in HuntingGround's window");
            Assert.AreEqual(1, windows, "fixture: one HuntingGround window was watched");
            Assert.GreaterOrEqual(huntingGround.Length, WatchFrames, "fixture: the HuntingGround window was watched long enough");
            Assert.AreEqual(0, confirmsBeforeClick, "P7 nothing is sent before the click");
            Assert.AreEqual(0, framesWithoutEventSystem, "C9 the shown window always has an EventSystem to take its click");
            Assert.AreEqual(0, framesMissed, "C9 P7 every shown frame, the raycast at 「확인」's centre reaches the window");
            Assert.LessOrEqual(drift, EdgeTolerance, "C9 P7 「확인」 stays where it first appeared while the window is shown");
            Assert.AreEqual(1, confirms, "P7 a click where 「확인」 first appeared sends C_DungeonClearConfirm exactly once");
            Assert.AreEqual(0, reserved, "P7 reserved is 0");
            Assert.IsTrue(closed, "P7 「확인」 closes the window");
        }

        // S-2 with the result first: a late 15 does not cover the pending rewards but keeps its StageClear SFX path. After
        // a pointer 「확인」 the next standalone 15 shows the banner fully, plays the SFX path and opens no window.
        [UnityTest]
        public IEnumerator ResultThenStageClear_KeepsTheSfxAndTheRewardsUncovered_AndTheNextStageClearShowsAfterConfirm()
        {
            const int gold = 16180;
            yield return EnterReadyTown();
            SendSuccess(gold);
            bool opened = false;
            yield return ScreenUiProbe.WaitUntil(() => RewardView.Read(gold).Readable, OpenSeconds, met => opened = met);

            int controlSfx = -1;
            yield return SfxPlayedBy(() => { }, played => controlSfx = played);
            int lateStageClearSfx = -1;
            yield return SfxPlayedBy(() => _fixture.Peer.Send(new S_StageClear { bossEntityId = BossEntityId }.Write()),
                played => lateStageClearSfx = played);
            float judgeFrom = Time.time + BannerFadeSeconds + FadeMarginSeconds;
            var judged = new List<CoverSample>();
            yield return SampleCover(gold, judgeFrom, judgeFrom + JudgeSeconds, judged);

            yield return ScreenUiProbe.WaitForEventSystem();
            RewardView beforeClick = RewardView.Read(gold);
            if (beforeClick.Shown) yield return InventoryPanelProbe.ClickAt(_fixture.Mouse, ScreenUiProbe.ScreenPoint(beforeClick.Confirm));
            yield return ScreenUiProbe.WaitUntil(() => Confirms() > 0, OpenSeconds, _ => { });
            yield return _fixture.Frames(5);
            int confirms = Confirms();
            bool closed = ScreenUiProbe.ShownButtons(ConfirmLabel).Length == 0;
            float bannerAfterConfirm = 0f;
            yield return MaxBannerAlpha(JudgeSeconds, alpha => bannerAfterConfirm = alpha);

            int nextStageClearSfx = -1;
            yield return SfxPlayedBy(() => _fixture.Peer.Send(new S_StageClear { bossEntityId = BossEntityId + 1 }.Write()),
                played => nextStageClearSfx = played);
            float peak = 0f;
            float peakOnScreen = 0f;
            bool windowDuringNext = false;
            float until = Time.time + BannerFadeSeconds + FadeMarginSeconds + JudgeSeconds;
            float realDeadline = Time.realtimeSinceStartup + 10f;
            while (Time.time <= until && Time.realtimeSinceStartup < realDeadline)
            {
                Drawn[] banner = BannerGraphics().Where(graphic => graphic.Alpha > 0f).ToArray();
                float alpha = banner.Select(graphic => graphic.Alpha).DefaultIfEmpty(0f).Max();
                if (alpha > peak)
                {
                    peak = alpha;
                    peakOnScreen = banner.Sum(graphic => OverlapArea(graphic.Ink, ScreenArea));
                }
                windowDuringNext |= ScreenUiProbe.ShownButtons(ConfirmLabel).Length > 0;
                yield return null;
            }

            Debug.Log($"[dungeon C9 review] 39 then 15: screen {Screen.width}x{Screen.height}; SFX played control {controlSfx}, " +
                $"late 15 {lateStageClearSfx}, next 15 {nextStageClearSfx} (code path only, listener muted); judged frames " +
                $"{judged.Count}, unreadable {judged.Count(sample => !sample.Readable)}, covered " +
                $"{judged.Count(sample => sample.Covered > 0f)}, first {FirstCovered(judged)}, max banner alpha " +
                $"{judged.Select(sample => sample.BannerAlpha).DefaultIfEmpty(0f).Max():F2}; confirms {confirms}; closed {closed}; " +
                $"banner alpha after confirm {bannerAfterConfirm:F2}; next 15 peak alpha {peak:F2} on screen {peakOnScreen:F2}; " +
                $"window during next 15 {windowDuringNext}");

            Assert.IsTrue(opened, "precondition: the result opens the Town window");
            Assert.Greater(judged.Count, 0, "fixture: frames after the fade were judged");
            Assert.AreEqual(0, judged.Count(sample => !sample.Readable), "S-2 the rewards and 「확인」 stay shown after the late 15");
            Assert.AreEqual(0, judged.Count(sample => sample.Covered > 0f),
                $"C9 S-2 after the fade the late 15's banner covers no reward text or 「확인」: first {FirstCovered(judged)}");
            Assert.Greater(lateStageClearSfx, controlSfx, "S-2 preserved: the late 15 still runs the StageClear SFX path");
            Assert.AreEqual(1, confirms, "P7 one pointer click sends C_DungeonClearConfirm once");
            Assert.IsTrue(closed, "P7 「확인」 closes the window");
            Assert.Greater(nextStageClearSfx, controlSfx, "S-2 preserved: the next standalone 15 runs the StageClear SFX path");
            Assert.GreaterOrEqual(peak, FullAlpha, "S-2 preserved: after 「확인」 the next standalone 15 shows the banner fully");
            Assert.Greater(peakOnScreen, 0f, "S-2 preserved: that banner has ink on screen");
            Assert.IsFalse(windowDuringNext, "P7 a standalone 15 opens no reward window");
        }

        IEnumerator EnterReadyTown()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp(_hpMarker, MaxHp);
            yield return _fixture.WaitMap(0);
        }

        // The scripted server moves the player as a portal or 「확인」 would: S_MapTransition, then HP for the new map.
        IEnumerator MoveTo(byte map)
        {
            _fixture.Peer.Send(new S_MapTransition { destMapId = map, spawnX = 2, spawnY = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.MapId == map && _fixture.Session.Entry.SceneReady,
                "scene of map " + map, 10);
            _fixture.Hp(++_hpMarker, MaxHp);
            yield return _fixture.WaitMap(map);
        }

        void SendSuccess(int gold) => _fixture.Peer.Send(new S_DungeonClearResult
        {
            result = (byte)InventoryResult.Success,
            currency = gold,
            reward0ItemId = (int)ItemId.Material,
            reward0Count = MaterialCount,
            reward1ItemId = (int)ItemId.CoinPouch,
            reward1Count = CoinPouchCount,
        }.Write());

        // The peer writes in order on one TCP stream and the client applies packets in order on the main thread, so once
        // this HP value shows, every packet sent before it has been handled.
        IEnumerator AfterEarlierPacketsApply()
        {
            int marker = ++_hpMarker;
            _fixture.Hp(marker, MaxHp);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.CurrentHp == marker, "HP marker " + marker, 3);
        }

        // SFX started between before and after `send` and its HP marker, by AudioManager's pool cursor, which moves once per
        // played SFX. A call with nothing sent is the control for SFX the marker itself may cause. Code path only: the
        // fixture mutes the listener, so nothing is heard.
        IEnumerator SfxPlayedBy(Action send, Action<int> played)
        {
            AudioManager audio = AudioManager.Instance;
            int before = audio != null ? MapEntryPlayFixture.Field<int>(audio, "_sfxIndex") : -1;
            send();
            yield return AfterEarlierPacketsApply();
            if (audio == null || before < 0)
            {
                played(-1);
                yield break;
            }
            int after = MapEntryPlayFixture.Field<int>(audio, "_sfxIndex");
            int pool = MapEntryPlayFixture.Field<AudioSource[]>(audio, "_sfxPool").Length;
            played(pool == 0 ? -1 : ((after - before) % pool + pool) % pool);
        }

        IEnumerator PressI()
        {
            _fixture.Keys(Key.I);
            yield return _fixture.Frames(2);
            _fixture.Keys();
            yield return _fixture.Frames(3);
        }

        int Confirms() => _fixture.Peer.Count(PacketID.C_DungeonClearConfirm);

        static void AssertReadableOnScreen(string moment, RewardView view)
        {
            Assert.AreEqual(1, view.ButtonCount, $"{moment}: the reward window is shown with one 「확인」");
            Assert.IsTrue(view.GoldShown, $"{moment}: the window shows the received gold");
            Assert.IsTrue(view.MaterialShown, $"{moment}: the window shows {MaterialName} ×{MaterialCount}");
            Assert.IsTrue(view.CoinPouchShown, $"{moment}: the window shows {CoinPouchName} ×{CoinPouchCount}");
            Assert.IsTrue(view.ConfirmInteractable, $"{moment}: 「확인」 can be pressed");
            Assert.IsTrue(Contains(ScreenArea, view.WindowRect), $"{moment}: the whole window {view.WindowRect} is on screen");
            Assert.IsTrue(Contains(ScreenArea, view.ConfirmRect), $"{moment}: 「확인」 {view.ConfirmRect} is on screen");
            Assert.IsTrue(view.RewardInk.All(ink => Contains(ScreenArea, ink)), $"{moment}: every reward text is on screen");
        }

        // Every frame up to `until` (game time, the banner's clock), from `judgeFrom` on: the window's readability and the
        // shown banner's area over its reward texts and 「확인」. A real-time limit keeps a stalled clock from hanging.
        static IEnumerator SampleCover(int gold, float judgeFrom, float until, List<CoverSample> judged)
        {
            float realDeadline = Time.realtimeSinceStartup + 10f;
            while (Time.time <= until && Time.realtimeSinceStartup < realDeadline)
            {
                if (Time.time >= judgeFrom) judged.Add(CoverSample.Read(gold));
                yield return null;
            }
        }

        static IEnumerator MaxBannerAlpha(float gameSeconds, Action<float> done)
        {
            float max = 0f;
            float until = Time.time + gameSeconds;
            float realDeadline = Time.realtimeSinceStartup + 10f;
            while (Time.time <= until && Time.realtimeSinceStartup < realDeadline)
            {
                max = Mathf.Max(max, BannerAlpha());
                yield return null;
            }
            done(max);
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
                still = Drift(current, previous) <= 0f ? still + 1 : 0;
                previous = current;
            }
        }

        static string FirstCovered(List<CoverSample> samples) =>
            samples.Where(sample => sample.Covered > 0f).Select(sample => sample.ToString()).DefaultIfEmpty("none").First();

        static Rect PanelRect() => ScreenUiProbe.ScreenRect((RectTransform)InventoryPanelProbe.Root.Find("Inventory"));

        static Drawn[] BannerGraphics()
        {
            StageClearUI banner = StageClearUI.Instance;
            return banner == null ? Array.Empty<Drawn>() : DrawnUnder(banner.transform);
        }

        static float BannerAlpha() => BannerGraphics().Select(graphic => graphic.Alpha).DefaultIfEmpty(0f).Max();

        static Drawn[] HudGraphics()
        {
            Assert.IsNotNull(HudController.Instance, "fixture: the additive HUD is loaded");
            return DrawnUnder(HudController.Instance.GetComponentInParent<Canvas>().rootCanvas.transform);
        }

        // Positive control in the same frame, as HudOverlapProbe does for the panel: with the window's centre placed on
        // character_status, the same reading must report a shown HUD graphic under it. The window is put back at once.
        static float HudControlOverlap(RewardView view, Drawn[] hud, out bool restored)
        {
            restored = false;
            if (!view.Shown) return 0f;
            RectTransform status = HudController.Instance.GetComponentInParent<Canvas>().rootCanvas
                .GetComponentsInChildren<RectTransform>().FirstOrDefault(rect => rect.name == "character_status");
            if (status == null) return 0f;
            Vector2 placed = view.Window.anchoredPosition;
            Rect target = ScreenUiProbe.ScreenRect(status);
            view.Window.position = new Vector3(target.center.x, target.center.y, view.Window.position.z);
            Rect moved = ScreenUiProbe.ScreenRect(view.Window);
            float overlap = hud.Where(graphic => graphic.Alpha > 0f).Sum(graphic => OverlapArea(graphic.Ink, moved));
            view.Window.anchoredPosition = placed;
            restored = Drift(view.WindowRect, ScreenUiProbe.ScreenRect(view.Window)) < 0.01f;
            return overlap;
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
                Rect ink = Ink(graphic);
                if (Area(ink) <= 0f) continue;
                drawn.Add(new Drawn(ScreenUiProbe.PathOf(renderer.transform, root, includeRoot: true), ink, DrawnAlpha(graphic)));
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

        // TMP texts by their laid-out glyph bounds; other graphics by their rectangle, letterboxed when a Simple Image
        // preserves its sprite's aspect (uGUI's Image.PreserveSpriteAspectRatio, read by reflection: no uGUI reference).
        static Rect Ink(Component graphic)
        {
            var rect = (RectTransform)graphic.transform;
            if (graphic is TMP_Text label)
            {
                bool laidOut = !string.IsNullOrEmpty(label.text) && label.textInfo != null && label.textInfo.characterCount > 0;
                return laidOut ? ScreenUiProbe.Bounds(rect, label.textBounds) : Rect.zero;
            }
            Rect box = ScreenUiProbe.ScreenRect(rect);
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

        // The window's own object directly under its canvas: the drawn background holding every reward text and 「확인」.
        static RectTransform TopBelowCanvas(Component component)
        {
            Transform top = component.transform;
            while (top.parent != null && top.parent.GetComponent<Canvas>() == null) top = top.parent;
            return (RectTransform)top;
        }

        static object CurrentEventSystem() => Type.GetType("UnityEngine.EventSystems.EventSystem, UnityEngine.UI")
            ?.GetProperty("current", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);

        // The digits a player reads in a label, whatever the grouping separator: "받은 골드 31,415" reads 31415.
        static string Digits(string text) => new string((text ?? string.Empty).Where(char.IsDigit).ToArray());

        static bool IsItemRow(string text, string name, int count) => text != null && text.Contains(name) &&
            Digits(text.Replace(name, string.Empty)) == count.ToString(CultureInfo.InvariantCulture);

        static float Area(Rect rect) => rect.width > 0f && rect.height > 0f ? rect.width * rect.height : 0f;

        static float OverlapArea(Rect a, Rect b)
        {
            Rect shared = Rect.MinMaxRect(Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin),
                Mathf.Min(a.xMax, b.xMax), Mathf.Min(a.yMax, b.yMax));
            return shared.width > EdgeTolerance && shared.height > EdgeTolerance ? shared.width * shared.height : 0f;
        }

        static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - EdgeTolerance &&
            inner.yMin >= outer.yMin - EdgeTolerance && inner.xMax <= outer.xMax + EdgeTolerance &&
            inner.yMax <= outer.yMax + EdgeTolerance;

        // How far any edge moved between two readings of the same rectangle, in pixels.
        static float Drift(Rect a, Rect b) => Mathf.Max(Mathf.Max(Mathf.Abs(a.xMin - b.xMin), Mathf.Abs(a.yMin - b.yMin)),
            Mathf.Max(Mathf.Abs(a.xMax - b.xMax), Mathf.Abs(a.yMax - b.yMax)));

        static float Drift(Rect[] a, Rect[] b) => a.Length != b.Length || a.Length == 0
            ? float.PositiveInfinity
            : a.Zip(b, (first, second) => Drift(first, second)).Max();

        // One graphic as drawn this frame.
        readonly struct Drawn
        {
            internal readonly string Path;
            internal readonly Rect Ink;
            internal readonly float Alpha;

            internal Drawn(string path, Rect ink, float alpha)
            {
                Path = path;
                Ink = ink;
                Alpha = alpha;
            }

            public override string ToString() => $"{Path} ink {Ink} alpha {Alpha:F2}";
        }

        // One judged frame of S-2: whether the rewards and 「확인」 are readable and the shown banner's area over them.
        readonly struct CoverSample
        {
            internal readonly float Time;
            internal readonly bool Readable;
            internal readonly float BannerAlpha;
            internal readonly float Covered;

            CoverSample(float time, bool readable, float bannerAlpha, float covered)
            {
                Time = time;
                Readable = readable;
                BannerAlpha = bannerAlpha;
                Covered = covered;
            }

            internal static CoverSample Read(int gold)
            {
                RewardView view = RewardView.Read(gold);
                Drawn[] banner = BannerGraphics();
                float covered = 0f;
                if (view.Shown)
                {
                    Rect[] targets = view.RewardInk.Append(view.ConfirmRect).ToArray();
                    covered = banner.Where(graphic => graphic.Alpha > 0f)
                        .Sum(graphic => targets.Sum(target => OverlapArea(graphic.Ink, target)));
                }
                return new CoverSample(UnityEngine.Time.time, view.Readable,
                    banner.Select(graphic => graphic.Alpha).DefaultIfEmpty(0f).Max(), covered);
            }

            public override string ToString() => $"t={Time:F3} readable {Readable} banner alpha {BannerAlpha:F2} covered {Covered:F2}";
        }

        // One frame in which a 「확인」 button is shown: whose scene it belongs to, where it is, and whether the EventSystem
        // raycast at its centre reaches its window.
        readonly struct ShownConfirm
        {
            internal readonly int Frame;
            internal readonly Component Button;
            internal readonly string Scene;
            internal readonly Rect Rect;
            internal readonly bool Interactable;
            internal readonly bool EventSystemPresent;
            internal readonly bool Hit;

            ShownConfirm(int frame, Component button, Rect rect, bool interactable, bool eventSystemPresent, bool hit)
            {
                Frame = frame;
                Button = button;
                Scene = button.gameObject.scene.name;
                Rect = rect;
                Interactable = interactable;
                EventSystemPresent = eventSystemPresent;
                Hit = hit;
            }

            internal static ShownConfirm Read(int frame, Component button)
            {
                bool eventSystem = CurrentEventSystem() != null;
                bool hit = eventSystem && ScreenUiProbe.RaycastHits(ScreenUiProbe.ScreenPoint(button))
                    .Any(ScreenUiProbe.GroupBelowCanvas(button).Contains);
                return new ShownConfirm(frame, button, ScreenUiProbe.ScreenRect((RectTransform)button.transform),
                    InventoryPanelProbe.Interactable(button), eventSystem, hit);
            }

            public override string ToString() => $"frame {Frame} {Scene} {Rect} interactable {Interactable} " +
                $"EventSystem {EventSystemPresent} hit {Hit}";
        }

        // One frame's reading of the reward window, found by its shown 「확인」 as the other window tests do.
        sealed class RewardView
        {
            internal readonly int ButtonCount;
            internal readonly Component Confirm;
            internal readonly RectTransform Window;
            internal readonly Rect WindowRect;
            internal readonly Rect ConfirmRect;
            internal readonly bool ConfirmInteractable;
            internal readonly string[] Texts = Array.Empty<string>();
            internal readonly Rect[] RewardInk = Array.Empty<Rect>();
            internal readonly bool GoldShown;
            internal readonly bool MaterialShown;
            internal readonly bool CoinPouchShown;

            RewardView(int gold)
            {
                Component[] buttons = ScreenUiProbe.ShownButtons(ConfirmLabel);
                ButtonCount = buttons.Length;
                if (buttons.Length != 1) return;
                Confirm = buttons[0];
                ConfirmRect = ScreenUiProbe.ScreenRect((RectTransform)Confirm.transform);
                ConfirmInteractable = InventoryPanelProbe.Interactable(Confirm);
                Window = TopBelowCanvas(Confirm);
                WindowRect = ScreenUiProbe.ScreenRect(Window);
                TMP_Text[] texts = ScreenUiProbe.ShownTextsUnder(Window);
                Texts = texts.Select(text => text.text).ToArray();
                string goldDigits = gold.ToString(CultureInfo.InvariantCulture);
                GoldShown = texts.Any(text => Digits(text.text) == goldDigits);
                MaterialShown = texts.Any(text => IsItemRow(text.text, MaterialName, MaterialCount));
                CoinPouchShown = texts.Any(text => IsItemRow(text.text, CoinPouchName, CoinPouchCount));
                RewardInk = texts.Where(text => Digits(text.text) == goldDigits ||
                        IsItemRow(text.text, MaterialName, MaterialCount) || IsItemRow(text.text, CoinPouchName, CoinPouchCount))
                    .Select(text => Ink(text)).ToArray();
            }

            internal bool Shown => Confirm != null;

            // Gold, both item rows and 「확인」 are shown, each reward text with ink to judge.
            internal bool Readable => Shown && GoldShown && MaterialShown && CoinPouchShown && RewardInk.Length >= 3;

            internal static RewardView Read(int gold) => new RewardView(gold);

            // The scene can unload before this observation is logged. Keep the captured button count and values;
            // Shown remains a live-object guard for the click and sampling paths.
            public override string ToString() => ButtonCount == 1
                ? $"rect {WindowRect} confirm {ConfirmRect} interactable {ConfirmInteractable} texts [{string.Join(" | ", Texts)}] " +
                    $"reward ink [{string.Join("; ", RewardInk.Select(ink => ink.ToString()))}]"
                : $"not shown ({ButtonCount} 「확인」 buttons)";
        }
    }
}
