using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Independent verification of dungeon goal P7 (01_Phases/goals/2026-10-10-dungeon-clear-rewards/goal.md
    // 「던전 패킷·창 PR의 요구」) where a scene changes under an unconfirmed result, which the PR's own window tests do not
    // reach: 「확인」 sends 40 only while the connection may send gameplay requests, and a scene outside gameplay shows
    // no window. The PR's client.md also promises that the result survives the scene change on the same connection.
    // As in DungeonClearRewardWindowSceneTests, the scripted TCP peer sends 39 in the goal P2 byte order and the window
    // is found by its shown gold and its 「확인」 button only; those helpers are private there, so the few needed are
    // repeated below.
    public sealed class DungeonClearRewardWindowSceneChangeTests
    {
        const ushort ClearResultId = 39;
        const ushort ClearConfirmId = 40;
        const int ClearResultLength = 33;
        const int ClearConfirmLength = 5;
        const string ConfirmLabel = "확인";
        const float OpenSeconds = 3f;

        MapEntryPlayFixture _fixture;

        [SetUp] public void Setup() => _fixture = new MapEntryPlayFixture();
        [UnityTearDown] public IEnumerator Teardown() => _fixture.Cleanup();

        // P7 「확인」 sends only after the handshake, through the same send gate that also waits for a ready map. A press
        // while the next map loads reaches that gate (the button is pressed as its onClick, past its interactable flag)
        // and sends nothing. The next gameplay scene's window shows the kept result, and one pointer click there sends
        // exactly one five-byte 40 with reserved 0.
        [UnityTest]
        public IEnumerator ConfirmPressedWhileTheNextMapLoads_SendsNothing_ThenTheNextMapShowsTheResultAndConfirmsOnce()
        {
            const int gold = 8642;
            yield return EnterReadyTown();
            _fixture.Peer.Send(Wire(ClearResult((byte)InventoryResult.Success, gold, ((int)ItemId.Material, 1))));
            bool opened = false;
            yield return ScreenUiProbe.WaitUntil(() => WindowShows(gold), OpenSeconds, met => opened = met);
            Assert.IsTrue(opened, "precondition: a valid success result opens the Town window");
            Component townButton = ConfirmButtons().Single();

            _fixture.Peer.Send(new S_MapTransition { destMapId = 1, spawnX = 2, spawnY = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => !_fixture.Session.CanSendGameplay, "the HuntingGround entry starts", 3);
            bool pressedWhileLoading = townButton != null && ScreenUiProbe.IsShown(townButton);
            if (pressedWhileLoading) ScreenUiProbe.Press(townButton);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.MapId == 1 && _fixture.Session.Entry.SceneReady,
                "HuntingGround scene", 10);
            _fixture.Hp(41, 100);
            yield return _fixture.WaitMap(1);
            int confirmsAfterTheLoadingPress = Confirms().Count;
            bool shownAgain = false;
            yield return ScreenUiProbe.WaitUntil(() => WindowShows(gold), OpenSeconds, met => shownAgain = met);
            Debug.Log($"[dungeon P7 verify] pressed while loading {pressedWhileLoading}, confirms then {confirmsAfterTheLoadingPress}, " +
                $"HuntingGround window shows {gold} {shownAgain}");

            Assert.IsTrue(pressedWhileLoading, "precondition: the Town window is still shown when the map entry starts");
            Assert.AreEqual(0, confirmsAfterTheLoadingPress, "P7 a press while the connection cannot send gameplay requests sends no 40");
            Assert.IsTrue(shownAgain, "client.md: the next gameplay scene's window shows the unconfirmed result again");

            yield return ScreenUiProbe.WaitForEventSystem();
            yield return InventoryPanelProbe.ClickAt(_fixture.Mouse, ScreenUiProbe.ScreenPoint(ConfirmButtons().Single()));
            yield return ScreenUiProbe.WaitUntil(() => Confirms().Count > 0, OpenSeconds, _ => { });
            yield return _fixture.Frames(5);
            List<byte[]> confirms = Confirms();

            Assert.AreEqual(1, confirms.Count, "P7 the click in the next map sends C_DungeonClearConfirm exactly once");
            Assert.AreEqual(ClearConfirmLength, confirms[0].Length, "P7 the confirm frame is five bytes");
            Assert.AreEqual(0, confirms[0][4], "P7 reserved is 0");
            Assert.AreEqual(0, ConfirmButtons().Length, "P7 「확인」 closes the window");
        }

        // P7 a scene outside gameplay shows no window, although the connection still holds the unconfirmed result.
        [UnityTest]
        public IEnumerator UnconfirmedResult_ShowsNoWindowInTheEndingScene()
        {
            const int gold = 7531;
            yield return EnterReadyTown();
            _fixture.Peer.Send(Wire(ClearResult((byte)InventoryResult.Success, gold, ((int)ItemId.CoinPouch, 1))));
            bool opened = false;
            yield return ScreenUiProbe.WaitUntil(() => WindowShows(gold), OpenSeconds, met => opened = met);
            Assert.IsTrue(opened, "precondition: a valid success result opens the Town window");

            _fixture.Peer.Send(new S_MapTransition { destMapId = 3, spawnX = 0, spawnY = 0 }.Write());
            yield return _fixture.WaitMap(3);
            yield return _fixture.Frames(10);
            string[] goldTexts = UnityEngine.Object.FindObjectsByType<TMP_Text>()
                .Where(text => ScreenUiProbe.IsShown(text) && HasNumber(text.text, gold))
                .Select(text => $"{text.name}='{text.text}'")
                .ToArray();
            Debug.Log($"[dungeon P7 verify] Ending shows the result's gold in [{string.Join(", ", goldTexts)}], " +
                $"shown 「확인」 buttons {ConfirmButtons().Length}, confirms {Confirms().Count}");

            Assert.IsEmpty(goldTexts, "P7 no window shows the result in Ending, a scene outside gameplay");
            Assert.AreEqual(0, Confirms().Count, "nothing confirmed the result on the way to Ending");
        }

        IEnumerator EnterReadyTown()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp(40, 100);
            yield return _fixture.WaitMap(0);
        }

        static Component[] ConfirmButtons() => ScreenUiProbe.ShownButtons(ConfirmLabel);

        static bool WindowShows(int gold)
        {
            Component[] buttons = ConfirmButtons();
            return buttons.Length == 1 && ScreenUiProbe
                .ShownTextsUnder(buttons[0].GetComponentInParent<Canvas>().rootCanvas.transform)
                .Any(text => HasNumber(text.text, gold));
        }

        // A whole number as a player reads it: digit grouping (8,642) is allowed, abbreviation is not.
        static bool HasNumber(string text, long value)
        {
            string ungrouped = Regex.Replace(text ?? string.Empty, @"(?<=\d)[,   ](?=\d{3}(?!\d))", string.Empty);
            return Regex.IsMatch(ungrouped, $@"(?<!\d){value}(?!\d)");
        }

        List<byte[]> Confirms() => MapEntryPlayFixture.Field<ConcurrentQueue<byte[]>>(_fixture.Peer, "_received")
            .Where(frame => BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(2, 2)) == ClearConfirmId)
            .ToList();

        static ArraySegment<byte> Wire(byte[] bytes) => new ArraySegment<byte>(bytes);

        // Goal P2 wire, written independently of the generator: [ushort size incl. header][ushort id 39], byte result,
        // int gold, three (int itemId, int count) slots, LittleEndian; unused slots stay 0/0.
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
    }
}
