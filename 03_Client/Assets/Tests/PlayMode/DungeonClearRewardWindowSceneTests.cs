using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dawnholder.Client.Network;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dawnholder.Client.Tests.PlayMode
{
    // Dungeon goal P6·P7 (01_Phases/goals/2026-10-10-dungeon-clear-rewards/goal.md 「던전 패킷·창 PR의 요구」) on the real
    // Town scene. The scripted TCP peer sends S_DungeonClearResult (39) bytes from the independent encoder below: the
    // committed Shared.dll has no such type before the generator runs, and goal P2 fixes the wire. The window, its texts
    // and its 「확인」 button are whatever the product builds; the tests find them by shown text and uGUI Button only.
    public sealed class DungeonClearRewardWindowSceneTests
    {
        const ushort ClearResultId = 39;
        const ushort ClearConfirmId = 40;
        const int ClearResultLength = 33;
        const int ClearConfirmLength = 5;
        const int MaxHp = 100;
        const float OpenSeconds = 3f;

        // Goal P7 wording. The tests only look for these phrases, never for the whole sentence around them.
        const string ConfirmLabel = "확인";
        const string CurrencyCapPhrase = "골드가 상한";
        const string InventoryFullPhrase = "가방이 가득 차";

        // Art 06 verification: approved pieces the reward window may show, name → .meta GUID (Art/UI/Components,
        // import-spec.json). uGUI is reached by type name and reflection, as ScreenUiProbe does.
        static readonly Dictionary<string, string> ApprovedGuids = new Dictionary<string, string>
        {
            ["Panel_Emphasis"] = "7dec580f6ab94568af987e047f5a2c36",
            ["Header"] = "1371d63dc50f40ebae0726a4d60203b5",
            ["Inset_Default"] = "baf557470603430e83015dc40371610a",
            ["Inset_Selected"] = "e2c7e2ef02fc41d38527480b1023601b",
            ["Inset_Disabled"] = "dd85577f51e84c7188a5a8d8a3b03517",
            ["Button_Accept_Normal"] = "304984e555104bf5b7d8648af717c165",
            ["Button_Accept_Highlighted"] = "ce276490b85b4f7fa5f674b0f2546e87",
            ["Button_Accept_Pressed"] = "0525c0bba70a41ad8bf71baebe25bc9f",
            ["Button_Accept_Disabled"] = "296d9f9387434c4cbc07115da1352309",
        };
        static readonly Type ImageType = Type.GetType("UnityEngine.UI.Image, UnityEngine.UI");

        MapEntryPlayFixture _fixture;
        int _hpMarker;

        static int Material => (int)ItemId.Material;
        static int CoinPouch => (int)ItemId.CoinPouch;
        static string MaterialName => ItemCatalog.For(ItemId.Material).DisplayName;
        static string CoinPouchName => ItemCatalog.For(ItemId.CoinPouch).DisplayName;

        [SetUp]
        public void Setup()
        {
            _fixture = new MapEntryPlayFixture();
            _hpMarker = 40;
        }

        [UnityTearDown] public IEnumerator Teardown() => _fixture.Cleanup();

        // P6 a valid result opens the window on the main thread; P7 it shows the gold as a whole number and each
        // item's catalog name with its count. The third slot is empty (0/0), which is valid.
        [UnityTest]
        public IEnumerator ValidSuccessResult_OpensTheWindow_WithTheWholeGoldAndEachItemNameAndCount()
        {
            yield return EnterReadyTown();
            _fixture.Peer.Send(Wire(ClearResult((byte)InventoryResult.Success, 1234567, (Material, 3), (CoinPouch, 7))));
            bool opened = false;
            yield return ScreenUiProbe.WaitUntil(() => ConfirmButtons().Length == 1, OpenSeconds, met => opened = met);
            TMP_Text[] texts = opened ? WindowTexts(ConfirmButtons()[0]) : Array.Empty<TMP_Text>();
            Log("success window", texts);

            Assert.IsTrue(opened, "P6 a valid success result opens the window with one 「확인」 button");
            Assert.IsTrue(texts.Any(text => HasNumber(text.text, 1234567)), "P7 the window shows the received gold 1234567 unabbreviated");
            Assert.IsTrue(ItemRowShows(texts, MaterialName, 3), $"P7 the window shows {MaterialName} with count 3");
            Assert.IsTrue(ItemRowShows(texts, CoinPouchName, 7), $"P7 the window shows {CoinPouchName} with count 7");
        }

        // P6 every malformed or out-of-range result is dropped silently: no window, no state change, the connection
        // stays. This already holds before the implementation (39 is an unknown ID today); the success test above is
        // the paired positive check. Each case is followed by an HP marker so the window is judged after the client
        // has handled that case.
        [UnityTest]
        public IEnumerator MalformedOrOutOfRangeResults_KeepTheWindowClosed_CaseByCase()
        {
            yield return EnterReadyTown();
            _fixture.Peer.Send(new S_InventorySnapshot { revision = 1, currency = 10 }.Write());
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Status == "서버 확인 완료", "synchronized Town panel", 3);
            string currencyBefore = InventoryPanelProbe.Currency;
            var shown = new Dictionary<string, bool>();
            var closed = new Dictionary<string, bool>();
            foreach ((string name, byte[] wire) in MalformedResults())
            {
                _fixture.Peer.Send(Wire(wire));
                yield return AfterEarlierPacketsApply();
                shown[name] = ConfirmButtons().Length > 0;
                closed[name] = _fixture.Session == null || _fixture.Session.IsClosed;
            }
            string currencyAfter = InventoryPanelProbe.Currency;
            Debug.Log("[dungeon P6] " + string.Join(", ", shown.Keys.Select(name => $"{name}: shown={shown[name]} closed={closed[name]}")));

            foreach (string name in shown.Keys)
            {
                Assert.IsFalse(shown[name], $"P6 {name}: the window stays closed");
                Assert.IsFalse(closed[name], $"P6 {name}: dropped without closing the connection");
            }
            Assert.AreEqual(currencyBefore, currencyAfter, "P6 dropped results change no inventory state");
        }

        [UnityTest]
        public IEnumerator CurrencyCapResult_ShowsTheGoldCapReason_WithoutTheSentGoldOrItem() =>
            RejectionShowsOnlyItsReason((byte)InventoryResult.CurrencyCap, CurrencyCapPhrase, 5150, Material, MaterialName);

        [UnityTest]
        public IEnumerator InventoryFullResult_ShowsTheFullBagReason_WithoutTheSentGoldOrItem() =>
            RejectionShowsOnlyItsReason((byte)InventoryResult.InventoryFull, InventoryFullPhrase, 6160, CoinPouch, CoinPouchName);

        // P7 a real pointer click on 「확인」 sends one five-byte 40 with reserved 0 and closes the window, and the
        // window's former spot no longer takes a UI raycast (so it blocks neither clicks nor attacks).
        [UnityTest]
        public IEnumerator ConfirmPointerClick_SendsOneFiveByteConfirm_ClosesTheWindow_AndLeavesNoRaycastTarget()
        {
            yield return EnterReadyTown();
            bool opened = false;
            yield return OpenSuccessWindow(2468, met => opened = met);
            Assert.IsTrue(opened, "P7 precondition: a valid success result opens the window");
            yield return ScreenUiProbe.WaitForEventSystem();
            Component button = ConfirmButtons().Single();
            TMP_Text gold = WindowTexts(button).First(text => HasNumber(text.text, 2468));
            HashSet<GameObject> window = ScreenUiProbe.GroupBelowCanvas(button);
            window.UnionWith(ScreenUiProbe.GroupBelowCanvas(gold));
            Vector2[] spots = { ScreenUiProbe.ScreenPoint(button), ScreenUiProbe.ScreenPoint(gold) };
            // Control for the closed check below: while open, the same probe sees the window at its button.
            bool openWindowTakesRaycast = ScreenUiProbe.RaycastHits(spots[0]).Any(window.Contains);

            yield return InventoryPanelProbe.ClickAt(_fixture.Mouse, spots[0]);
            yield return ScreenUiProbe.WaitUntil(() => Confirms().Count > 0, OpenSeconds, _ => { });
            yield return _fixture.Frames(5);
            List<byte[]> confirms = Confirms();
            bool closedWindow = ConfirmButtons().Length == 0;
            string[] windowHits = spots.SelectMany(ScreenUiProbe.RaycastHits)
                .Where(window.Contains)
                .Select(hit => hit.name)
                .ToArray();

            Assert.IsTrue(openWindowTakesRaycast, "control: the open window takes the raycast at its 「확인」 button");
            Assert.AreEqual(1, confirms.Count, "P7 「확인」 sends C_DungeonClearConfirm exactly once");
            Assert.AreEqual(ClearConfirmLength, confirms[0].Length, "P7 the confirm frame is five bytes");
            Assert.AreEqual(ClearConfirmLength, BinaryPrimitives.ReadUInt16LittleEndian(confirms[0]), "P7 its size header says five");
            Assert.AreEqual(0, confirms[0][4], "P7 reserved is 0");
            Assert.IsTrue(closedWindow, "P7 「확인」 closes the window");
            Assert.IsEmpty(windowHits, "P7 the closed window's spot takes no UI raycast");
        }

        // P7 two presses in the same frame still send one confirm.
        [UnityTest]
        public IEnumerator ConfirmPressedTwiceInOneFrame_SendsOnlyOneConfirm()
        {
            yield return EnterReadyTown();
            bool opened = false;
            yield return OpenSuccessWindow(1357, met => opened = met);
            Assert.IsTrue(opened, "P7 precondition: a valid success result opens the window");
            Component button = ConfirmButtons().Single();

            ScreenUiProbe.Press(button);
            ScreenUiProbe.Press(button);
            yield return ScreenUiProbe.WaitUntil(() => Confirms().Count > 0, OpenSeconds, _ => { });
            yield return _fixture.Frames(5);

            Assert.AreEqual(1, Confirms().Count, "P7 two presses in one frame send C_DungeonClearConfirm once");
            Assert.AreEqual(0, ConfirmButtons().Length, "P7 the pressed window is closed");
        }

        // P7 a new result while the window is open replaces its content; it does not stack a second window.
        [UnityTest]
        public IEnumerator SecondResultWhileOpen_ReplacesTheWindowContent()
        {
            yield return EnterReadyTown();
            _fixture.Peer.Send(Wire(ClearResult((byte)InventoryResult.Success, 1111, (Material, 3))));
            bool opened = false;
            yield return ScreenUiProbe.WaitUntil(() => ConfirmButtons().Length == 1 && WindowShows(1111), OpenSeconds, met => opened = met);
            _fixture.Peer.Send(Wire(ClearResult((byte)InventoryResult.Success, 2222, (CoinPouch, 7))));
            bool replaced = false;
            yield return ScreenUiProbe.WaitUntil(() => WindowShows(2222), OpenSeconds, met => replaced = met);
            Component[] buttons = ConfirmButtons();
            TMP_Text[] texts = buttons.Length == 1 ? WindowTexts(buttons[0]) : Array.Empty<TMP_Text>();
            Log("replaced window", texts);

            Assert.IsTrue(opened, "P7 precondition: the first result opens the window");
            Assert.IsTrue(replaced, "P7 the second result's gold 2222 is shown");
            Assert.AreEqual(1, buttons.Length, "P7 one window, not a second one");
            Assert.IsFalse(texts.Any(text => HasNumber(text.text, 1111)), "P7 the first result's gold is gone");
            Assert.IsFalse(texts.Any(text => text.text.Contains(MaterialName)), $"P7 the first result's {MaterialName} is gone");
            Assert.IsTrue(ItemRowShows(texts, CoinPouchName, 7), $"P7 the second result's {CoinPouchName} 7 is shown");
        }

        // P7 the connection's end leaves the window closed.
        [UnityTest]
        public IEnumerator ConnectionEnd_LeavesTheWindowClosed()
        {
            yield return EnterReadyTown();
            bool opened = false;
            yield return OpenSuccessWindow(9753, met => opened = met);
            Assert.IsTrue(opened, "P7 precondition: a valid success result opens the window");

            NetworkService.Instance.Disconnect();
            yield return MapEntryPlayFixture.Wait(() => UnityClientSession.Instance == null, "connection cleanup", 5);
            yield return _fixture.Frames(5);

            Assert.AreEqual(0, ConfirmButtons().Length, "P7 no 「확인」 window remains after the connection ends");
            Assert.IsEmpty(ScreenUiProbe.ShownTexts("9753"), "P7 the reward text is not shown after the connection ends");
        }

        // Art 06 independent verification (verifies task_f7615fd3886f) in the real Town window. Requirement sources:
        // ui-window-application-scope.md 「제품 쓰기 경계」 (reward background, title, rows and 「확인」 use approved pieces),
        // 「보존 계약」 (decorations take no raycast; normal/highlighted/pressed/disabled are approved sprites) and goal
        // 「적용 중인 사용자 결정」 reward window user A (the title band and row backdrops lie behind the kept text).
        // The real pointer hovers, presses and releases 「확인」; only the release sends the one confirm.
        [UnityTest]
        public IEnumerator TownWindowArt_ShowsTheApprovedPieces_PassesThePointerOverItsTexts_AndShowsAcceptStatesUnderThePointer()
        {
            yield return EnterReadyTown();
            bool opened = false;
            yield return OpenSuccessWindow(3579, met => opened = met);
            Assert.IsTrue(opened, "precondition: a valid success result opens the window");
            yield return ScreenUiProbe.WaitForEventSystem();
            yield return _fixture.Frames(2);
            Component button = ConfirmButtons().Single();
            Transform canvas = button.GetComponentInParent<Canvas>().rootCanvas.transform;
            HashSet<GameObject> window = ScreenUiProbe.GroupBelowCanvas(button);
            string[] shownPieces = ShownImages(canvas).Select(image => PieceFamily(ImageSprite(image, "sprite"))).ToArray();
            // Title, summary and the three rows (the third is empty here); the caption belongs to the button itself.
            TMP_Text[] windowTexts = canvas.GetComponentsInChildren<TMP_Text>().Where(text => !text.transform.IsChildOf(button.transform)).ToArray();
            TMP_Text title = windowTexts.Single(text => text.text == "던전 클리어");
            string[] textSpotHits = windowTexts.Select(text => ScreenUiProbe.ScreenPoint(text))
                .SelectMany(ScreenUiProbe.RaycastHits).Where(window.Contains).Select(hit => hit.name).ToArray();
            Vector2 onButton = ScreenUiProbe.ScreenPoint(button);
            bool buttonTakesRaycast = ScreenUiProbe.RaycastHits(onButton).Any(window.Contains);

            yield return InventoryPanelProbe.PointAt(_fixture.Mouse, ScreenUiProbe.ScreenPoint(title));
            string normal = PieceName(ShownButtonSprite(button));
            yield return InventoryPanelProbe.PointAt(_fixture.Mouse, onButton);
            string highlighted = PieceName(ShownButtonSprite(button));
            InputSystem.QueueStateEvent(_fixture.Mouse, new MouseState { position = onButton }.WithButton(MouseButton.Left));
            yield return _fixture.Frames(2);
            string pressed = PieceName(ShownButtonSprite(button));
            int confirmsWhileHeld = Confirms().Count;
            InputSystem.QueueStateEvent(_fixture.Mouse, new MouseState { position = onButton });
            yield return ScreenUiProbe.WaitUntil(() => Confirms().Count > 0, OpenSeconds, _ => { });
            yield return _fixture.Frames(5);
            Debug.Log($"[reward art verify] pieces [{string.Join(", ", shownPieces)}], window hits over texts [{string.Join(", ", textSpotHits)}], " +
                $"states normal={normal} highlighted={highlighted} pressed={pressed}, confirms held={confirmsWhileHeld} after={Confirms().Count}");

            Assert.IsTrue(buttonTakesRaycast, "control: the open window takes the raycast at its 「확인」 button");
            CollectionAssert.AreEquivalent(new[] { "Panel_Emphasis", "Header", "Inset_*", "Inset_*", "Inset_*", "Button_Accept_Normal" }, shownPieces,
                "scope reward window: the shown window draws Panel_Emphasis, one title Header, three row Inset_* and the Accept button only");
            Assert.IsEmpty(textSpotHits, "scope 「장식은 raycastTarget=false」: the title, summary and rows with their backdrops pass the pointer");
            Assert.AreEqual("Button_Accept_Normal", normal, "scope: 「확인」 away from the pointer shows the approved normal sprite");
            Assert.AreEqual("Button_Accept_Highlighted", highlighted, "scope: the pointer over 「확인」 shows the approved highlighted sprite");
            Assert.AreEqual("Button_Accept_Pressed", pressed, "scope: the held press shows the approved pressed sprite");
            Assert.AreEqual(0, confirmsWhileHeld, "P7 holding the press sends nothing yet");
            Assert.AreEqual(1, Confirms().Count, "P7 the release sends C_DungeonClearConfirm exactly once");
            Assert.AreEqual(0, ConfirmButtons().Length, "P7 「확인」 closes the window");
        }

        IEnumerator RejectionShowsOnlyItsReason(byte result, string reason, int sentGold, int itemId, string itemName)
        {
            yield return EnterReadyTown();
            _fixture.Peer.Send(Wire(ClearResult(result, sentGold, (itemId, 3))));
            bool opened = false;
            yield return ScreenUiProbe.WaitUntil(() => ConfirmButtons().Length == 1, OpenSeconds, met => opened = met);
            TMP_Text[] texts = opened ? WindowTexts(ConfirmButtons()[0]) : Array.Empty<TMP_Text>();
            Log("rejection " + result, texts);

            Assert.IsTrue(opened, $"P7 result {result} opens the window");
            Assert.IsTrue(texts.Any(text => text.text.Contains(reason)), $"P7 result {result} shows 「{reason}」");
            Assert.IsFalse(texts.Any(text => HasNumber(text.text, sentGold)), $"P7 result {result} does not show the sent gold {sentGold}");
            Assert.IsFalse(texts.Any(text => text.text.Contains(itemName)), $"P7 result {result} does not show {itemName}");
        }

        IEnumerator EnterReadyTown()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp(_hpMarker, MaxHp);
            yield return _fixture.WaitMap(0);
        }

        IEnumerator OpenSuccessWindow(int gold, Action<bool> done)
        {
            _fixture.Peer.Send(Wire(ClearResult((byte)InventoryResult.Success, gold, (Material, 1), (CoinPouch, 1))));
            yield return ScreenUiProbe.WaitUntil(() => ConfirmButtons().Length == 1 && WindowShows(gold), OpenSeconds, done);
        }

        // The peer writes in order on one TCP stream and the client applies packets in order on the main thread, so
        // once this HP value shows, every packet sent before it has been handled. A few frames follow for UI updates.
        IEnumerator AfterEarlierPacketsApply()
        {
            int marker = ++_hpMarker;
            _fixture.Hp(marker, MaxHp);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.CurrentHp == marker, "HP marker " + marker, 3);
            yield return _fixture.Frames(5);
        }

        static Component[] ConfirmButtons() => ScreenUiProbe.ShownButtons(ConfirmLabel);

        static Component[] ShownImages(Transform canvas)
        {
            Assert.IsNotNull(ImageType, "fixture: uGUI type UnityEngine.UI.Image");
            return canvas.GetComponentsInChildren<Component>().Where(component => component != null &&
                component.GetType() == ImageType && ScreenUiProbe.IsShown(component)).ToArray();
        }

        // "sprite" is the Image's own sprite; "overrideSprite" reads what it draws now (a button state's sprite).
        static Sprite ImageSprite(Component image, string property) => (Sprite)image.GetType().GetProperty(property).GetValue(image);

        static Sprite ShownButtonSprite(Component button)
        {
            var graphic = (Component)button.GetType().GetProperty("targetGraphic").GetValue(button);
            return graphic != null ? ImageSprite(graphic, "overrideSprite") : null;
        }

        // The approved name when the sprite is the asset behind that name's GUID; otherwise what it is.
        static string PieceName(Sprite sprite)
        {
            if (sprite == null) return "nothing";
#if UNITY_EDITOR
            string guid = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string found, out long _) ? found : null;
            bool approved = ApprovedGuids.TryGetValue(sprite.name, out string expected) && expected == guid;
            return approved ? sprite.name : $"non-approved '{sprite.name}' (GUID {guid ?? "none"})";
#else
            return sprite.name;
#endif
        }

        // Any Inset_* piece is an approved row backdrop.
        static string PieceFamily(Sprite sprite)
        {
            string name = PieceName(sprite);
            return name.StartsWith("Inset_", StringComparison.Ordinal) ? "Inset_*" : name;
        }

        // The texts drawn on the window's canvas.
        static TMP_Text[] WindowTexts(Component confirmButton) =>
            ScreenUiProbe.ShownTextsUnder(confirmButton.GetComponentInParent<Canvas>().rootCanvas.transform);

        static bool WindowShows(int gold)
        {
            Component[] buttons = ConfirmButtons();
            return buttons.Length == 1 && WindowTexts(buttons[0]).Any(text => HasNumber(text.text, gold));
        }

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

        // The peer's raw frames: its Last<T> needs a generated packet type, which does not exist before the generator runs.
        List<byte[]> Confirms() => MapEntryPlayFixture.Field<ConcurrentQueue<byte[]>>(_fixture.Peer, "_received")
            .Where(frame => BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(2, 2)) == ClearConfirmId)
            .ToList();

        static void Log(string label, TMP_Text[] texts) =>
            Debug.Log($"[dungeon P7] {label}: " + string.Join(" | ", texts.Select(text => $"{text.name}='{text.text}'")));

        static ArraySegment<byte> Wire(byte[] bytes) => new ArraySegment<byte>(bytes);

        // Goal P6's wrong inputs, one per check. Each carries its own gold so a wrongly opened window would show it.
        static IEnumerable<(string name, byte[] wire)> MalformedResults()
        {
            byte success = (byte)InventoryResult.Success;
            byte[] valid = ClearResult(success, 4100, (Material, 1));
            yield return ("one byte short", Resized(valid, ClearResultLength - 1));
            yield return ("one byte extra", Resized(valid, ClearResultLength + 1));
            yield return ("result Stale(1)", ClearResult((byte)InventoryResult.Stale, 4201, (Material, 1)));
            yield return ("result NotOwned(2)", ClearResult((byte)InventoryResult.NotOwned, 4202, (Material, 1)));
            yield return ("result NotUsable(3)", ClearResult((byte)InventoryResult.NotUsable, 4203, (Material, 1)));
            yield return ("result RevisionExhausted(6)", ClearResult((byte)InventoryResult.RevisionExhausted, 4206, (Material, 1)));
            yield return ("negative gold", ClearResult(success, -1, (Material, 1)));
            yield return ("gold above MaxCurrency", ClearResult(success, InventoryLimits.MaxCurrency + 1, (Material, 1)));
            yield return ("item outside the catalog", ClearResult(success, 4300, (3, 1)));
            yield return ("count zero", ClearResult(success, 4400, (Material, 0)));
            yield return ("count above MaxStack", ClearResult(success, 4500, (Material, InventoryLimits.MaxStack + 1)));
            yield return ("item id zero with a count", ClearResult(success, 4600, (0, 1)));
        }

        // Independent wire for goal P2: [ushort size incl. header][ushort id 39], byte result, int gold, then three
        // (int itemId, int count) slots, all LittleEndian. Unused slots stay 0/0.
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

        // The same frame one byte shorter or longer. The size header follows the delivered length, as the client's
        // framing layer guarantees before a handler sees a frame.
        static byte[] Resized(byte[] wire, int length)
        {
            var resized = new byte[length];
            Buffer.BlockCopy(wire, 0, resized, 0, Math.Min(length, wire.Length));
            BinaryPrimitives.WriteUInt16LittleEndian(resized.AsSpan(0, 2), (ushort)length);
            return resized;
        }
    }
}
