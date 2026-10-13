using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Independent re-verification of dungeon goal P7 (01_Phases/goals/2026-10-10-dungeon-clear-rewards/goal.md
    // 「던전 패킷·창 PR의 요구」: 「연결 종료 정리와 게임플레이 밖 씬에서는 닫혀 있다」, 「숨긴 창은 클릭과 공격 입력을 막지
    // 않는다」) and client.md 「게임플레이 밖 씬·연결 종료·disable에서는 CanvasGroup의 표시와 입력을 모두 끈다」, at the
    // window's own scene gate. Ending and map changes replace the whole scene, so the window goes away with it and those
    // tests never reach that gate. Here the real Town window stays loaded while a scene outside gameplay is the active
    // scene. The 39 wire itself is pinned by DungeonClearRewardWindowSceneTests' independent encoder, so this test sends
    // the generated packet as plain input, the way the fixture sends its other server packets, and counts 40 frames with
    // the peer's own counter.
    public sealed class DungeonClearRewardWindowActiveSceneTests
    {
        const string ConfirmLabel = "확인";
        const float OpenSeconds = 3f;

        MapEntryPlayFixture _fixture;
        Scene _outsideGameplay;

        [SetUp] public void Setup() => _fixture = new MapEntryPlayFixture();

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            // The fixture unloads only the scenes it knows by name, so this test unloads its own empty scene first.
            if (_outsideGameplay.IsValid() && _outsideGameplay.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(_outsideGameplay);
                if (unload != null) yield return MapEntryPlayFixture.Wait(() => unload.isDone, "scene outside gameplay unload", 5);
            }
            yield return _fixture.Cleanup();
        }

        // P7 while a scene outside gameplay is active, the Town window is closed: nothing of it is shown, it takes no
        // raycast, and a press of its 「확인」 (as its onClick, past the interactable flag) sends nothing although the
        // connection may still send gameplay requests. Once Town is active again the window shows the one result that was
        // sent and never confirmed, and one pointer click sends exactly one 40 with reserved 0.
        [UnityTest]
        public IEnumerator WhileASceneOutsideGameplayIsActive_TheWindowIsClosedAndSendsNothing_ThenTownShowsTheKeptResult()
        {
            yield return EnterReadyTown();
            _fixture.Peer.Send(new S_DungeonClearResult
            {
                result = (byte)InventoryResult.Success,
                currency = 9753,
                reward0ItemId = (int)ItemId.Material,
                reward0Count = 2,
            }.Write());
            bool opened = false;
            yield return ScreenUiProbe.WaitUntil(() => ConfirmButtons().Length == 1, OpenSeconds, met => opened = met);
            Assert.IsTrue(opened, "precondition: a valid success result opens the Town window");
            yield return ScreenUiProbe.WaitForEventSystem();
            Component townButton = ConfirmButtons().Single();
            Scene town = townButton.gameObject.scene;
            Transform windowRoot = townButton.GetComponentInParent<Canvas>().rootCanvas.transform;
            int windowTextsWhileTownIsActive = ScreenUiProbe.ShownTextsUnder(windowRoot).Length;
            Vector2 buttonSpot = ScreenUiProbe.ScreenPoint(townButton);
            var window = ScreenUiProbe.GroupBelowCanvas(townButton);
            // Control for the closed check below: while Town is active, the same probe sees the window at its button.
            bool openWindowTakesRaycast = ScreenUiProbe.RaycastHits(buttonSpot).Any(window.Contains);

            _outsideGameplay = SceneManager.CreateScene("DungeonWindowOutsideGameplay_" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(_outsideGameplay);
            yield return _fixture.Frames(5);
            bool canSendWhileOutside = _fixture.Session.CanSendGameplay;
            string[] shownWhileOutside = ScreenUiProbe.ShownTextsUnder(windowRoot)
                .Select(text => $"{text.name}='{text.text}'")
                .ToArray();
            bool buttonShownWhileOutside = ScreenUiProbe.IsShown(townButton);
            string[] hitsWhileOutside = ScreenUiProbe.RaycastHits(buttonSpot)
                .Where(window.Contains)
                .Select(hit => hit.name)
                .ToArray();
            ScreenUiProbe.Press(townButton);
            yield return _fixture.Frames(5);
            int confirmsAfterTheOutsidePress = Confirms();

            SceneManager.SetActiveScene(town);
            bool shownAgain = false;
            yield return ScreenUiProbe.WaitUntil(() => ConfirmButtons().Length == 1, OpenSeconds, met => shownAgain = met);
            Debug.Log($"[dungeon P7 reverify] Town active: window texts {windowTextsWhileTownIsActive}; outside gameplay: " +
                $"can send {canSendWhileOutside}, shown texts [{string.Join(", ", shownWhileOutside)}], button shown " +
                $"{buttonShownWhileOutside}, window hits [{string.Join(", ", hitsWhileOutside)}], confirms after press " +
                $"{confirmsAfterTheOutsidePress}; Town active again shows the window {shownAgain}");

            Assert.IsTrue(openWindowTakesRaycast, "control: the open window takes the raycast at its 「확인」 button");
            Assert.Greater(windowTextsWhileTownIsActive, 0, "control: the same text probe sees the open window's texts");
            Assert.IsTrue(canSendWhileOutside,
                "precondition: the connection may still send gameplay requests, so only the window's scene gate can stop the press");
            Assert.IsEmpty(shownWhileOutside, "P7 the window shows nothing while a scene outside gameplay is active");
            Assert.IsFalse(buttonShownWhileOutside, "P7 the closed window shows no 「확인」");
            Assert.IsEmpty(hitsWhileOutside, "P7 the closed window takes no UI raycast");
            Assert.AreEqual(0, confirmsAfterTheOutsidePress, "P7 a press of the closed window sends no 40");
            Assert.IsTrue(shownAgain, "client.md: the unconfirmed result is kept and shows again once Town is active");

            yield return InventoryPanelProbe.ClickAt(_fixture.Mouse, ScreenUiProbe.ScreenPoint(ConfirmButtons().Single()));
            yield return ScreenUiProbe.WaitUntil(() => Confirms() > 0, OpenSeconds, _ => { });
            yield return _fixture.Frames(5);

            Assert.AreEqual(1, Confirms(), "P7 the click sends C_DungeonClearConfirm exactly once");
            Assert.AreEqual(0, _fixture.Peer.Last<C_DungeonClearConfirm>(PacketID.C_DungeonClearConfirm).reserved,
                "P7 reserved is 0");
            Assert.AreEqual(0, ConfirmButtons().Length, "P7 「확인」 closes the window");
        }

        IEnumerator EnterReadyTown()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp(40, 100);
            yield return _fixture.WaitMap(0);
        }

        static Component[] ConfirmButtons() => ScreenUiProbe.ShownButtons(ConfirmLabel);

        int Confirms() => _fixture.Peer.Count(PacketID.C_DungeonClearConfirm);
    }
}
