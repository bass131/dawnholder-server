using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dawnholder.Client.Network.CharacterMenu;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Phase = Dawnholder.Client.UI.CharacterMenuViewState.Phase;

namespace Dawnholder.Client.Tests
{
    // CharacterMenuPresenter over the real CharacterMenuClient and a fake transport. Requirement source: goal
    // 2026-10-24-character-select-create-ui 「만들 것」, 「10-28 승인으로 더한 것」, World's 10-28 reply in 「고정 소비
    // 계약과 의존」 and completion rows 1, 2 and 5. Replies are Shared.Protocol packets' Write() read by
    // CharacterMenuReplyReader; requests are read back from the sent bytes. Expected texts are copied literally from
    // the goal's approved table and the post-merge stage contract. No server, NetworkService or scene runs here.
    public sealed class CharacterMenuPresenterTests
    {
        const string KnightId = "00112233-4455-6677-8899-aabbccddeeff";
        const string MageId = "11112233-4455-6677-8899-aabbccddeeff";
        const string ThirdId = "22112233-4455-6677-8899-aabbccddeeff";
        const string CreatedId = "33112233-4455-6677-8899-aabbccddeeff";
        static readonly Saved s_knight = new(KnightId, "가람", 0);
        static readonly Saved s_mage = new(MageId, "한별", 1);
        static readonly Saved s_third = new(ThirdId, "세번째", 1);
        static readonly Saved s_created = new(CreatedId, "새벽", 1);
        // The log line of a list holding s_knight and s_mage, which several log cases open with.
        const string KnightMageListLine = "[CharacterMenu] 50 목록 성공: 1=00112233-4455-6677-8899-aabbccddeeff/가람/0 " +
            "2=11112233-4455-6677-8899-aabbccddeeff/한별/1 3=빈칸";

        readonly List<CharacterMenuViewState> _states = new();
        readonly List<string> _calls = new();
        FakeConnection _connection;
        CharacterMenuPresenter _presenter;

        CharacterMenuViewState Last => _states[_states.Count - 1];

        [SetUp]
        public void SetUp()
        {
            _states.Clear();
            _calls.Clear();
            _connection = new FakeConnection();
            _presenter = new CharacterMenuPresenter(_states.Add, () => _calls.Add("disconnect"),
                exit => _calls.Add("exit " + exit));
        }

        [TearDown]
        public void TearDown()
        {
            _presenter?.Dispose();
            _connection?.Client.Dispose();
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        public void ListOfZeroOneOrThree_KeepsSlotOrderAndShowsEmptySlotsAsEmpty(int count)
        {
            Assert.IsTrue(_connection.Client.TryList(), "fixture: the facade's first list");
            _presenter.Observe(null, false);
            _presenter.Observe(null, true);
            Assert.IsEmpty(_states, "nothing to show before the connection's menu client exists");
            _presenter.Observe(_connection.Client, true);
            Assert.AreEqual(Phase.LoadingList, Last.CurrentPhase);

            _connection.Reply(List(_connection.LastId, count >= 1 ? s_knight : null,
                count >= 3 ? s_mage : null, count >= 3 ? s_third : null));

            Assert.AreEqual(Phase.Ready, Last.CurrentPhase);
            Assert.AreEqual("", Last.Message);
            AssertSlot(Last, 1, count >= 1 ? "가람" : null, 0);
            AssertSlot(Last, 2, count >= 3 ? "한별" : null, 1);
            AssertSlot(Last, 3, count >= 3 ? "세번째" : null, 1);
            Assert.AreEqual(1, _connection.Sent.Count, "the facade's list is the only request; the menu adds none");
        }

        [Test]
        public void ListAnsweredBeforeTheMenuSeesTheClient_IsStillShown()
        {
            Assert.IsTrue(_connection.Client.TryList());
            _connection.Reply(List(_connection.LastId, s_knight));

            _presenter.Observe(_connection.Client, true);

            Assert.AreEqual(Phase.Ready, Last.CurrentPhase);
            AssertSlot(Last, 1, "가람", 0);
        }

        [TestCase(11, 0, "서버가 잠시 응답하지 못했습니다. 다시 시도해 주세요.")]
        [TestCase(1, 1, "선택을 다시 확인해 주세요. 목록을 새로고침합니다.")]
        public void RefusedList_IsAFailedListNotAnEmptyAccountAndIsNotAskedAgainByItself(int result, int field,
            string expected)
        {
            Assert.IsTrue(_connection.Client.TryList());
            _presenter.Observe(_connection.Client, true);

            _connection.Reply(ListRefused(_connection.LastId, (byte)result, (byte)field));

            Assert.AreEqual(Phase.ListFailed, Last.CurrentPhase);
            Assert.AreEqual(expected, Last.Message);
            Assert.AreEqual(1, _connection.Count(PacketID.C_CharacterListRequest), "a refused list never loops");
            _presenter.Refresh();
            Assert.AreEqual(Phase.LoadingList, Last.CurrentPhase);
            _connection.Reply(List(_connection.LastId));
            Assert.AreEqual(Phase.Ready, Last.CurrentPhase, "only a successful list is an empty account");
            Assert.AreEqual("", Last.Message);
            for (byte slot = 1; slot <= 3; slot++) AssertSlot(Last, slot, null, 0);
        }

        [Test]
        public void CreateSuccess_ListsTheCharacterWithoutEntering_AndItsIdIsWhatALaterSelectSends()
        {
            OpenWithList(s_knight);
            const string rawName = " 새 벽 ";

            _presenter.Create(2, 1, rawName);

            Assert.AreEqual(Phase.Creating, Last.CurrentPhase);
            byte[] request = _connection.Last;
            Assert.AreEqual(PacketID.C_CharacterCreateRequest, Kind(request));
            Assert.AreEqual(2, request[8], "slot");
            Assert.AreEqual(1, request[9], "class");
            Assert.AreEqual(rawName, CreateName(request), "the typed name goes out unchanged");
            _connection.Reply(Created(_connection.LastId, 2, s_created));

            Assert.AreEqual(Phase.Ready, Last.CurrentPhase);
            Assert.AreEqual("슬롯 2: 캐릭터를 만들었습니다. 입장할 캐릭터를 선택하세요.", Last.Message);
            AssertSlot(Last, 1, "가람", 0);
            AssertSlot(Last, 2, "새벽", 1);
            Assert.Zero(_connection.Count(PacketID.C_CharacterSelectRequest), "creating never selects");
            Assert.IsEmpty(_calls, "creating neither leaves nor disconnects");
            _presenter.Select(2);
            Assert.AreEqual(CreatedId, SelectedId(_connection.Last));
        }

        [TestCase(51, 3, 6, 0, "슬롯 3: 이미 쓰는 이름입니다. 다른 이름을 입력해 주세요.")]
        [TestCase(51, 3, 1, 2, "슬롯 3: 이름은 2~12자의 한글·영문·숫자만 쓸 수 있습니다.")]
        [TestCase(51, 3, 5, 0, "슬롯 3: 캐릭터 슬롯 3칸이 모두 찼습니다.")]
        [TestCase(53, 1, 8, 0, "슬롯 1: 이 캐릭터가 다른 곳에서 접속 중입니다. 잠시 뒤 다시 시도해 주세요.")]
        [TestCase(53, 2, 9, 0, "슬롯 2: 캐릭터 상태를 확인하는 중이라 지금은 입장할 수 없습니다.")]
        [TestCase(53, 2, 13, 0, "슬롯 2: 서버 오류로 처리하지 못했습니다(코드 13).")]
        public void RefusedCreateOrSelect_NamesTheRequestedSlotAndStaysReady(int request, int slot, int result,
            int field, string expected)
        {
            OpenWithList(s_knight, s_mage);

            SendAndRefuse(request, (byte)slot, (byte)result, (byte)field);

            Assert.AreEqual(Phase.Ready, Last.CurrentPhase);
            Assert.AreEqual(expected, Last.Message);
            AssertSlot(Last, 1, "가람", 0);
            AssertSlot(Last, 2, "한별", 1);
            Assert.AreEqual(1, _connection.Count(PacketID.C_CharacterListRequest),
                "only slot, class and ID input refusals ask for the list");
            Assert.IsEmpty(_calls);
        }

        [TestCase(51, 3, 1, "슬롯 3: 선택을 다시 확인해 주세요. 목록을 새로고침합니다.")]
        [TestCase(51, 3, 3, "슬롯 3: 선택을 다시 확인해 주세요. 목록을 새로고침합니다.")]
        [TestCase(53, 2, 4, "슬롯 2: 선택을 다시 확인해 주세요. 목록을 새로고침합니다.")]
        [TestCase(53, 1, 1, "슬롯 1: 선택을 다시 확인해 주세요. 목록을 새로고침합니다.")]
        public void SlotClassOrIdRefusal_AsksForTheListExactlyOnceAndKeepsItsText(int request, int slot, int field,
            string expected)
        {
            OpenWithList(s_knight, s_mage);

            SendAndRefuse(request, (byte)slot, 1, (byte)field);

            Assert.AreEqual(Phase.LoadingList, Last.CurrentPhase);
            Assert.AreEqual(expected, Last.Message);
            Assert.AreEqual(2, _connection.Count(PacketID.C_CharacterListRequest));
            Assert.AreEqual(PacketID.C_CharacterListRequest, Kind(_connection.Last));
            _connection.Reply(List(_connection.LastId, s_knight));

            Assert.AreEqual(Phase.Ready, Last.CurrentPhase);
            Assert.AreEqual(expected, Last.Message, "the refusal stays readable after the refresh it asked for");
            AssertSlot(Last, 2, null, 0);
            Assert.AreEqual(2, _connection.Count(PacketID.C_CharacterListRequest), "one refresh, no loop");
            Assert.AreEqual(3, _connection.Sent.Count);
        }

        [Test]
        public void SelectSuccess_WaitsLockedForTheEntryAndOpensNothing()
        {
            OpenWithList(s_knight, s_mage);

            _presenter.Select(2);

            Assert.AreEqual(Phase.Selecting, Last.CurrentPhase);
            Assert.AreEqual(MageId, SelectedId(_connection.Last), "the view's slot becomes that slot's ID here");
            _connection.Reply(Selected(_connection.LastId, MageId));
            Assert.AreEqual(Phase.Entering, Last.CurrentPhase);
            Assert.AreEqual("", Last.Message);
            AssertSlot(Last, 2, "한별", 1);
            int sent = _connection.Sent.Count;
            _presenter.Select(1);
            _presenter.Create(3, 0, "새벽");
            _presenter.Refresh();
            Assert.AreEqual(sent, _connection.Sent.Count, "entry wait accepts no request");
            Assert.AreEqual(Phase.Entering, Last.CurrentPhase);
            Assert.IsEmpty(_calls, "the network code's map entry opens Town, not the menu");
        }

        [Test]
        public void OutcomeUnknown_KeepsItsTextWhileTheClientClosesTheConnectionOnce()
        {
            const string expected = "슬롯 3: 결과를 확인하지 못해 연결을 끊습니다. 다시 접속해 목록을 먼저 확인해 주세요.";
            OpenWithList(s_knight);
            _presenter.Create(3, 1, "새벽");

            _connection.Reply(CreateRefused(_connection.LastId, 12));

            Assert.AreEqual(Phase.Disconnected, Last.CurrentPhase);
            Assert.AreEqual(expected, Last.Message);
            Assert.AreEqual(1, _connection.Disconnects, "the client closes its connection");
            int sent = _connection.Sent.Count;
            _presenter.Observe(_connection.Client, _connection.Open);
            _presenter.Create(3, 1, "새벽");
            _presenter.Refresh();
            Assert.AreEqual(sent, _connection.Sent.Count, "nothing is sent again on that connection");
            Assert.AreEqual(1, _connection.Disconnects);
            Assert.IsEmpty(_calls, "the menu does not close it a second time");
            Assert.AreEqual(Phase.Disconnected, Last.CurrentPhase);
            Assert.AreEqual(expected, Last.Message);
            _presenter.Back();
            CollectionAssert.AreEqual(new[] { "disconnect", "exit MainMenu" }, _calls, "back still leaves");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void LegacyFlowOnly_DisconnectsThenHandsOverToTheClassButtonsWithoutTextOrMainMenu(bool beforeTheMenu)
        {
            Assert.IsTrue(_connection.Client.TryList());
            if (!beforeTheMenu) _presenter.Observe(_connection.Client, true);
            int states = _states.Count;

            _connection.Reply(ListRefused(_connection.LastId, 16));
            if (beforeTheMenu) _presenter.Observe(_connection.Client, true);

            CollectionAssert.AreEqual(new[] { "disconnect", "exit LegacyClasses" }, _calls);
            Assert.AreEqual(states, _states.Count, "no text is shown");
            Assert.IsTrue(_states.All(state => state.Message == ""));
            Assert.Zero(_connection.Disconnects, "the menu, not the client, closes a legacy connection");
            _presenter.Observe(_connection.Client, true);
            _presenter.Refresh();
            _presenter.Back();
            Assert.AreEqual(states, _states.Count);
            CollectionAssert.AreEqual(new[] { "disconnect", "exit LegacyClasses" }, _calls);
        }

        [Test]
        public void WhileARequestIsPending_InputsSendNothingButStillRender()
        {
            Assert.IsTrue(_connection.Client.TryList());
            _presenter.Observe(_connection.Client, true);
            int states = _states.Count;

            _presenter.Refresh();
            _presenter.Select(1);
            _presenter.Create(1, 0, "새벽");

            Assert.AreEqual(1, _connection.Sent.Count, "the facade's pending list blocks every request");
            Assert.AreEqual(states + 3, _states.Count, "each refused input renders so the view's click latch opens");
            Assert.IsTrue(_states.Skip(states).All(state => state.CurrentPhase == Phase.LoadingList));
            _connection.Reply(List(_connection.LastId, s_knight));
            _presenter.Create(2, 0, "새벽");
            Assert.AreEqual(Phase.Creating, Last.CurrentPhase);
            _presenter.Create(3, 1, "다른이름");
            _presenter.Select(1);
            _presenter.Refresh();
            Assert.AreEqual(2, _connection.Sent.Count, "the pending create blocks every other request");
            Assert.AreEqual(Phase.Creating, Last.CurrentPhase);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void ChangedOrClosedConnection_EndsTheMenuAndTheOldClientsLateReplyChangesNothing(int change)
        {
            OpenWithList(s_knight);
            _presenter.Create(2, 0, "새벽");
            var replacement = new FakeConnection();
            try
            {
                if (change == 0) _presenter.Observe(replacement.Client, true);
                else if (change == 1) _presenter.Observe(null, true);
                else _presenter.Observe(_connection.Client, false);

                Assert.AreEqual(Phase.Disconnected, Last.CurrentPhase);
                Assert.AreEqual("", Last.Message);
                int states = _states.Count;
                _connection.Reply(Created(_connection.LastId, 2, s_created));
                Assert.AreEqual(states, _states.Count, "the old client's reply does not reach the menu");
                Assert.IsTrue(replacement.Client.TryList());
                _presenter.Observe(replacement.Client, true);
                replacement.Reply(List(replacement.LastId, s_mage));
                Assert.AreEqual(states, _states.Count, "an ended menu does not adopt another connection");
                Assert.IsEmpty(_calls, "a lost connection is shown; leaving stays with the player");
            }
            finally
            {
                replacement.Client.Dispose();
            }
        }

        [Test]
        public void ConnectionLostBeforeTheMenuClientExists_IsDisconnected()
        {
            _presenter.Observe(null, false);
            _presenter.Observe(null, true);
            Assert.IsEmpty(_states);

            _presenter.Observe(null, false);

            Assert.AreEqual(Phase.Disconnected, Last.CurrentPhase);
            Assert.AreEqual(1, _states.Count);
        }

        [Test]
        public void Back_DisconnectsThenOpensTheMainMenuAndDropsTheSubscription()
        {
            OpenWithList(s_knight);
            _presenter.Create(2, 0, "새벽");
            int states = _states.Count;

            _presenter.Back();

            CollectionAssert.AreEqual(new[] { "disconnect", "exit MainMenu" }, _calls);
            _connection.Reply(Created(_connection.LastId, 2, s_created));
            _presenter.Observe(_connection.Client, true);
            _presenter.Back();
            Assert.AreEqual(states, _states.Count);
            CollectionAssert.AreEqual(new[] { "disconnect", "exit MainMenu" }, _calls);
        }

        [Test]
        public void Dispose_DropsTheSubscriptionButKeepsTheConnectionForTheEntry()
        {
            OpenWithList(s_knight);
            _presenter.Create(2, 0, "새벽");
            int states = _states.Count;

            _presenter.Dispose();

            _connection.Reply(Created(_connection.LastId, 2, s_created));
            _presenter.Observe(_connection.Client, true);
            _presenter.Select(1);
            _presenter.Back();
            Assert.AreEqual(states, _states.Count);
            Assert.IsEmpty(_calls, "disposal neither disconnects nor leaves");
            Assert.Zero(_connection.Disconnects);
            Assert.IsTrue(_connection.Open);
        }

        [Test]
        public void CreateTheClientRefusesToSend_ReturnsToReadyWithATextInsteadOfStayingLocked()
        {
            OpenWithList(s_knight);

            _presenter.Create(2, 0, new string('가', 37));

            Assert.AreEqual(1, _connection.Sent.Count, "the client sent nothing");
            Assert.AreEqual(Phase.Ready, Last.CurrentPhase);
            Assert.AreEqual("슬롯 2: 요청을 보내지 못했습니다. 입력을 확인하고 다시 시도해 주세요.", Last.Message);
            _presenter.Create(2, 0, "새벽");
            Assert.AreEqual(Phase.Creating, Last.CurrentPhase);
        }

        [Test]
        public void FirstListNeverSent_IsAFailedListThatTheRefreshCanAskAgain()
        {
            _presenter.Observe(_connection.Client, true);

            Assert.AreEqual(Phase.ListFailed, Last.CurrentPhase);
            Assert.AreEqual("목록 요청을 보내지 못했습니다. 다시 시도해 주세요.", Last.Message);
            _presenter.Refresh();
            Assert.AreEqual(Phase.LoadingList, Last.CurrentPhase);
            Assert.AreEqual(1, _connection.Count(PacketID.C_CharacterListRequest));
        }

        [Test]
        public void WithTheRealView_ClicksBecomeRequestsAndRepliesBecomeItsText()
        {
            var root = new GameObject("Character menu presenter under test", typeof(RectTransform));
            CharacterMenuView view = null;
            var presenter = new CharacterMenuPresenter(state => view.Render(state), () => _calls.Add("disconnect"),
                exit => _calls.Add("exit " + exit));
            view = new CharacterMenuView(root, presenter.Select, presenter.Create, presenter.Refresh, presenter.Back);
            try
            {
                Transform panel = root.transform.Find("CharacterMenu/Panel");
                Assert.IsTrue(_connection.Client.TryList());
                presenter.Observe(_connection.Client, true);
                _connection.Reply(List(_connection.LastId, s_knight));
                Assert.AreEqual("슬롯 1 · Knight\n가람", panel.Find("Slot1/Label").GetComponent<TMP_Text>().text);

                Click(panel, "Slot2");
                panel.Find("CreateForm/Name").GetComponent<TMP_InputField>().text = "새벽";
                Click(panel, "CreateForm/Mage");
                Click(panel, "Submit");
                Assert.AreEqual("새벽", CreateName(_connection.Last));
                Assert.AreEqual(1, _connection.Last[9], "Mage");
                _connection.Reply(Created(_connection.LastId, 2, s_created));
                Assert.AreEqual("슬롯 2: 캐릭터를 만들었습니다. 입장할 캐릭터를 선택하세요.",
                    panel.Find("Result").GetComponent<TMP_Text>().text);
                Assert.IsTrue(panel.Find("Slot2").GetComponent<Button>().interactable, "the reply released the view");

                Click(panel, "Slot2");
                Click(panel, "Submit");
                Assert.AreEqual(CreatedId, SelectedId(_connection.Last));
                _connection.Reply(Selected(_connection.LastId, CreatedId));
                Assert.AreEqual("선택이 승인되었습니다. 서버 입장을 기다리고 있습니다.",
                    panel.Find("Status").GetComponent<TMP_Text>().text);
                Assert.AreEqual("새벽\n직업: Mage\n이 캐릭터로 입장을 기다리는 중입니다.",
                    panel.Find("Details").GetComponent<TMP_Text>().text);
                Assert.IsFalse(panel.Find("Submit").GetComponent<Button>().interactable);
                Assert.IsEmpty(_calls);

                Click(panel, "Back");
                CollectionAssert.AreEqual(new[] { "disconnect", "exit MainMenu" }, _calls);
            }
            finally
            {
                presenter.Dispose();
                view.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        // The Editor log line per applied reply, the only place a run shows character IDs (goal 「병합 뒤 1단 결과」,
        // the ID check of completion row 4). Expected lines are the lead contract's literal format with the IDs of
        // the constants above.
        [Test]
        public void ListSuccessLog_NamesEachSlotsIdNameAndClassAndMarksEmptySlots()
        {
            using var log = new MenuLogCapture();

            OpenWithList(s_knight, null, s_third);

            log.AssertLines("[CharacterMenu] 50 목록 성공: 1=00112233-4455-6677-8899-aabbccddeeff/가람/0 2=빈칸 " +
                "3=22112233-4455-6677-8899-aabbccddeeff/세번째/1");
        }

        [Test]
        public void CreateSuccessLog_NamesTheNewCharactersSlotIdNameAndClass()
        {
            using var log = new MenuLogCapture();
            OpenWithList(s_knight);

            _presenter.Create(2, 1, "새벽");
            _connection.Reply(Created(_connection.LastId, 2, s_created));

            log.AssertLines("[CharacterMenu] 50 목록 성공: 1=00112233-4455-6677-8899-aabbccddeeff/가람/0 2=빈칸 3=빈칸",
                "[CharacterMenu] 52 생성 성공: 슬롯 2 33112233-4455-6677-8899-aabbccddeeff/새벽/1");
        }

        [Test]
        public void SelectSuccessLog_NamesTheSelectedSlotAndTheConfirmedId()
        {
            using var log = new MenuLogCapture();
            OpenWithList(s_knight, s_mage);

            _presenter.Select(2);
            _connection.Reply(Selected(_connection.LastId, MageId));

            log.AssertLines(KnightMageListLine,
                "[CharacterMenu] 54 선택 성공: 슬롯 2 11112233-4455-6677-8899-aabbccddeeff");
        }

        [TestCase(11, 0, "[CharacterMenu] 50 거절: 슬롯 0 결과 11/0")]
        [TestCase(1, 1, "[CharacterMenu] 50 거절: 슬롯 0 결과 1/1")]
        [TestCase(16, 0, "[CharacterMenu] 50 거절: 슬롯 0 결과 16/0")]
        public void RefusedListLog_NamesSlotZeroWithTheResultAndField(int result, int field, string expected)
        {
            using var log = new MenuLogCapture();
            Assert.IsTrue(_connection.Client.TryList(), "fixture: the facade's first list");
            _presenter.Observe(_connection.Client, true);

            _connection.Reply(ListRefused(_connection.LastId, (byte)result, (byte)field));

            log.AssertLines(expected);
        }

        [TestCase(51, 3, 6, 0, "[CharacterMenu] 52 거절: 슬롯 3 결과 6/0")]
        [TestCase(51, 3, 1, 2, "[CharacterMenu] 52 거절: 슬롯 3 결과 1/2")]
        [TestCase(51, 3, 12, 0, "[CharacterMenu] 52 거절: 슬롯 3 결과 12/0")]
        [TestCase(53, 1, 8, 0, "[CharacterMenu] 54 거절: 슬롯 1 결과 8/0")]
        public void RefusedCreateOrSelectLog_NamesTheRequestedSlotWithTheResultAndField(int request, int slot,
            int result, int field, string expected)
        {
            using var log = new MenuLogCapture();
            OpenWithList(s_knight, s_mage);

            SendAndRefuse(request, (byte)slot, (byte)result, (byte)field);

            log.AssertLines(KnightMageListLine, expected);
        }

        void OpenWithList(Saved slot1 = null, Saved slot2 = null, Saved slot3 = null)
        {
            Assert.IsTrue(_connection.Client.TryList(), "fixture: the facade's first list");
            _presenter.Observe(_connection.Client, true);
            _connection.Reply(List(_connection.LastId, slot1, slot2, slot3));
            Assert.AreEqual(Phase.Ready, Last.CurrentPhase, "fixture: list ready");
        }

        void SendAndRefuse(int request, byte slot, byte result, byte field)
        {
            if (request == 51) _presenter.Create(slot, 0, "새벽");
            else _presenter.Select(slot);
            Assert.AreEqual(request == 51 ? PacketID.C_CharacterCreateRequest : PacketID.C_CharacterSelectRequest,
                Kind(_connection.Last), "fixture: request sent");
            _connection.Reply(request == 51
                ? CreateRefused(_connection.LastId, result, field)
                : SelectRefused(_connection.LastId, result, field));
        }

        static void AssertSlot(CharacterMenuViewState state, byte slot, string name, byte classId)
        {
            CharacterMenuViewState.Slot shown = state.GetSlot(slot);
            if (name == null)
            {
                Assert.IsNull(shown, $"slot {slot} is empty");
                return;
            }
            Assert.IsNotNull(shown, $"slot {slot} is occupied");
            Assert.AreEqual(name, shown.Name, $"slot {slot} name");
            Assert.AreEqual(classId, shown.ClassId, $"slot {slot} class");
        }

        static void Click(Transform panel, string path) => panel.Find(path).GetComponent<Button>().onClick.Invoke();

        static PacketID Kind(byte[] frame) => (PacketID)BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(2));

        // C_CharacterSelectRequest: header 4, requestId 4, reserved 1, then the UTF-16 ID with its byte count.
        static string SelectedId(byte[] frame) =>
            Encoding.Unicode.GetString(frame, 11, BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(9)));

        // C_CharacterCreateRequest: header 4, requestId 4, slot, class, reserved, then the UTF-16 name.
        static string CreateName(byte[] frame) =>
            Encoding.Unicode.GetString(frame, 13, BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(11)));

        static ArraySegment<byte> List(uint requestId, Saved slot1 = null, Saved slot2 = null, Saved slot3 = null) =>
            new S_CharacterListResult
            {
                requestId = requestId,
                occupiedMask = (byte)((slot1 != null ? 1 : 0) | (slot2 != null ? 2 : 0) | (slot3 != null ? 4 : 0)),
                slot1CharacterIdD = slot1?.Id ?? "",
                slot1Name = slot1?.Name ?? "",
                slot1ClassId = slot1?.ClassId ?? 0,
                slot2CharacterIdD = slot2?.Id ?? "",
                slot2Name = slot2?.Name ?? "",
                slot2ClassId = slot2?.ClassId ?? 0,
                slot3CharacterIdD = slot3?.Id ?? "",
                slot3Name = slot3?.Name ?? "",
                slot3ClassId = slot3?.ClassId ?? 0,
            }.Write();

        static ArraySegment<byte> ListRefused(uint requestId, byte result, byte field = 0) => new S_CharacterListResult
        {
            requestId = requestId,
            result = result,
            invalidField = field,
            slot1CharacterIdD = "",
            slot1Name = "",
            slot2CharacterIdD = "",
            slot2Name = "",
            slot3CharacterIdD = "",
            slot3Name = "",
        }.Write();

        static ArraySegment<byte> Created(uint requestId, byte slot, Saved character) => new S_CharacterCreateResult
        {
            requestId = requestId,
            slot = slot,
            characterIdD = character.Id,
            name = character.Name,
            classId = character.ClassId,
        }.Write();

        static ArraySegment<byte> CreateRefused(uint requestId, byte result, byte field = 0) => new S_CharacterCreateResult
        {
            requestId = requestId,
            result = result,
            invalidField = field,
            characterIdD = "",
            name = "",
        }.Write();

        static ArraySegment<byte> Selected(uint requestId, string characterId) => new S_CharacterSelectResult
        {
            requestId = requestId,
            selectedCharacterIdD = characterId,
        }.Write();

        static ArraySegment<byte> SelectRefused(uint requestId, byte result, byte field = 0) => new S_CharacterSelectResult
        {
            requestId = requestId,
            result = result,
            invalidField = field,
            selectedCharacterIdD = "",
        }.Write();

        sealed class Saved
        {
            internal Saved(string id, string name, byte classId)
            {
                Id = id;
                Name = name;
                ClassId = classId;
            }

            internal string Id { get; }
            internal string Name { get; }
            internal byte ClassId { get; }
        }

        // One connection's transport. Sends are recorded; the client's own disconnect closes it.
        sealed class FakeConnection
        {
            internal readonly List<byte[]> Sent = new();
            internal readonly CharacterMenuClient Client;
            internal bool Open = true;
            internal int Disconnects;

            internal FakeConnection()
            {
                Client = new CharacterMenuClient(() => Open, frame => Sent.Add(frame.ToArray()),
                    () =>
                    {
                        Disconnects++;
                        Open = false;
                    }, _ => { });
            }

            internal byte[] Last => Sent[Sent.Count - 1];

            internal uint LastId => BinaryPrimitives.ReadUInt32LittleEndian(Last.AsSpan(4));

            internal int Count(PacketID kind) => Sent.Count(frame => Kind(frame) == kind);

            internal void Reply(ArraySegment<byte> frame)
            {
                Assert.IsTrue(CharacterMenuReplyReader.TryRead(frame, out CharacterMenuReply reply), "fixture: valid reply");
                Assert.IsTrue(Client.TryReceive(reply), "fixture: the client takes the reply to its pending request");
            }
        }

        // The presenter's [CharacterMenu] log lines and levels, in order, while it is alive. The presenter logs on
        // the test's thread, so each line is recorded before the call that applied the reply returns.
        sealed class MenuLogCapture : IDisposable
        {
            readonly List<string> _lines = new();
            readonly List<LogType> _levels = new();

            internal MenuLogCapture() => Application.logMessageReceivedThreaded += Record;

            public void Dispose() => Application.logMessageReceivedThreaded -= Record;

            internal void AssertLines(params string[] expected)
            {
                CollectionAssert.AreEqual(expected, _lines, "one line per applied reply, nothing else");
                Assert.IsTrue(_levels.All(level => level == LogType.Log), "plain log level");
            }

            void Record(string message, string stackTrace, LogType type)
            {
                if (!message.StartsWith("[CharacterMenu]", StringComparison.Ordinal)) return;
                _lines.Add(message);
                _levels.Add(type);
            }
        }
    }
}
