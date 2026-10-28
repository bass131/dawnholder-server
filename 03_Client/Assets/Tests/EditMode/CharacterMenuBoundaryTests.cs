using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
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
    // Independent boundary checks: goal 2026-10-24-character-select-create-ui, approved wording table and
    // completion rows 2/5; packet-contract-v1 request correlation and 12/16 lifetimes. Expectations are literals.
    // Packet Write -> reply reader -> real client -> presenter -> real view; only transport is a test double.
    public sealed class CharacterMenuBoundaryTests
    {
        const string CreatedId = "72800111-2233-4455-6677-8899aabbccdd";
        readonly List<byte[]> _sent = new();
        readonly List<CharacterMenuViewState> _states = new();
        readonly List<string> _calls = new();
        readonly List<string> _logs = new();
        CharacterMenuClient _client;
        CharacterMenuPresenter _presenter;
        CharacterMenuView _view;
        GameObject _root;
        Action<byte[]> _duringSend;
        Action _duringDisconnect;
        Action<CharacterMenuViewState> _duringRender;
        bool _open;

        Transform Panel => _root.transform.Find("CharacterMenu/Panel");
        CharacterMenuViewState Last => _states[_states.Count - 1];
        uint RequestId => BinaryPrimitives.ReadUInt32LittleEndian(_sent[_sent.Count - 1].AsSpan(4));

        [SetUp]
        public void SetUp()
        {
            _sent.Clear();
            _states.Clear();
            _calls.Clear();
            _logs.Clear();
            _duringSend = null;
            _duringDisconnect = null;
            _duringRender = null;
            _open = true;
            _client = new CharacterMenuClient(() => _open, frame =>
            {
                byte[] bytes = frame.ToArray();
                _sent.Add(bytes);
                _duringSend?.Invoke(bytes);
            }, () =>
            {
                _calls.Add("client disconnect");
                _open = false;
            }, id => _calls.Add("accepted " + id));
            _root = new GameObject("Character menu boundary fixture", typeof(RectTransform));
            _presenter = new CharacterMenuPresenter(state =>
            {
                _states.Add(state);
                _view?.Render(state);
                _duringRender?.Invoke(state);
            }, () =>
            {
                _calls.Add("menu disconnect");
                _open = false;
                _duringDisconnect?.Invoke();
            }, exit => _calls.Add("exit " + exit));
            _view = new CharacterMenuView(_root, _presenter.Select, _presenter.Create,
                _presenter.Refresh, _presenter.Back);
            Application.logMessageReceivedThreaded += RecordLog;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceivedThreaded -= RecordLog;
            _presenter?.Dispose();
            _view?.Dispose();
            _client?.Dispose();
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
        }

        [TestCase(1, 2, "이름은 2~12자의 한글·영문·숫자만 쓸 수 있습니다.")]
        [TestCase(2, 0, "앞의 요청을 처리하는 중입니다. 잠시 기다려 주세요.")]
        [TestCase(3, 0, "지금은 이 요청을 할 수 없습니다. 메뉴로 돌아가 다시 접속해 주세요.")]
        [TestCase(4, 0, "이미 캐릭터가 있는 슬롯입니다. 목록을 새로고침해 주세요.")]
        [TestCase(5, 0, "캐릭터 슬롯 3칸이 모두 찼습니다.")]
        [TestCase(6, 0, "이미 쓰는 이름입니다. 다른 이름을 입력해 주세요.")]
        [TestCase(7, 0, "이 캐릭터를 찾을 수 없습니다. 목록을 새로고침해 주세요.")]
        [TestCase(8, 0, "이 캐릭터가 다른 곳에서 접속 중입니다. 잠시 뒤 다시 시도해 주세요.")]
        [TestCase(9, 0, "캐릭터 상태를 확인하는 중이라 지금은 입장할 수 없습니다.")]
        [TestCase(10, 0, "서버가 잠시 응답하지 못했습니다. 다시 시도해 주세요.")]
        [TestCase(11, 0, "서버가 잠시 응답하지 못했습니다. 다시 시도해 주세요.")]
        [TestCase(13, 0, "서버 오류로 처리하지 못했습니다(코드 13).")]
        [TestCase(14, 0, "서버 오류로 처리하지 못했습니다(코드 14).")]
        [TestCase(15, 0, "서버 오류로 처리하지 못했습니다(코드 15).")]
        public void RefusalBytesReachTheVisibleResultWithoutASecondRequest(int result, int field, string text)
        {
            OpenEmptyMenu();
            Click("Slot3");
            NameInput().SetTextWithoutNotify("새벽");
            Click("Submit");

            Assert.IsTrue(Receive(Refused(RequestId, (byte)result, (byte)field)));

            Assert.AreEqual("슬롯 3: " + text, Label("Result").text);
            Assert.AreEqual(Phase.Ready, Last.CurrentPhase);
            Assert.AreEqual(2, _sent.Count);
            Assert.IsTrue(Control("Submit").interactable);
            Assert.IsEmpty(_calls);
        }

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(4)]
        public void SynchronousInputRefusalAndListRefreshKeepOneLiteralRefusal(int field)
        {
            OpenEmptyMenu();
            _duringSend = bytes =>
            {
                uint id = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4));
                if (Kind(bytes) == 51) Assert.IsTrue(Receive(Refused(id, 1, (byte)field)));
                else if (Kind(bytes) == 49) Assert.IsTrue(Receive(EmptyList(id)));
            };

            _presenter.Create(2, 1, "새벽");

            CollectionAssert.AreEqual(new ushort[] { 49, 51, 49 }, _sent.Select(Kind).ToArray());
            Assert.IsFalse(_client.HasPending);
            Assert.AreEqual(Phase.Ready, Last.CurrentPhase);
            Assert.AreEqual("슬롯 2: 선택을 다시 확인해 주세요. 목록을 새로고침합니다.", Label("Result").text);
            CollectionAssert.AreEqual(new[]
            {
                "[CharacterMenu] 50 목록 성공: 1=빈칸 2=빈칸 3=빈칸",
                "[CharacterMenu] 52 거절: 슬롯 2 결과 1/" + field,
                "[CharacterMenu] 50 목록 성공: 1=빈칸 2=빈칸 3=빈칸",
            }, _logs);
        }

        [Test]
        public void CreateReplyForAnotherSlotCannotPublishOrLogACharacter()
        {
            OpenEmptyMenu();
            _presenter.Create(2, 0, "새벽");
            uint pending = RequestId;
            int renders = _states.Count;

            Assert.IsFalse(Receive(Created(pending, 3)));

            Assert.AreEqual(pending, _client.PendingRequestId);
            Assert.AreEqual(renders, _states.Count);
            Assert.AreEqual(1, _logs.Count);
            Assert.IsNull(Last.GetSlot(2));
            Assert.IsNull(Last.GetSlot(3));
            Assert.IsTrue(Receive(Created(pending, 2)));
            Assert.AreEqual("새벽", Last.GetSlot(2).Name);
            Assert.IsNull(Last.GetSlot(3));
            Assert.IsEmpty(_calls, "creation grants no entry authority");
            Assert.AreEqual("[CharacterMenu] 52 생성 성공: 슬롯 2 72800111-2233-4455-6677-8899aabbccdd/새벽/0", _logs[1]);
            _presenter.Select(2);
            var selected = new C_CharacterSelectRequest();
            selected.Read(new ArraySegment<byte>(_sent[_sent.Count - 1]));
            Assert.AreEqual(CreatedId, selected.characterIdD);
        }

        [Test]
        public void SynchronousCreateSuccessUnlocksTheViewButDoesNotSelectOrEnter()
        {
            OpenEmptyMenu();
            _duringSend = bytes =>
            {
                if (Kind(bytes) == 51)
                    Assert.IsTrue(Receive(Created(BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4)), 2)));
            };
            Click("Slot2");
            NameInput().SetTextWithoutNotify("새벽");

            Click("Submit");

            CollectionAssert.AreEqual(new ushort[] { 49, 51 }, _sent.Select(Kind).ToArray());
            Assert.AreEqual(Phase.Ready, Last.CurrentPhase);
            Assert.IsFalse(_client.HasPending);
            Assert.AreEqual("슬롯 2: 캐릭터를 만들었습니다. 입장할 캐릭터를 선택하세요.", Label("Result").text);
            Assert.IsFalse(Control("Submit").interactable, "a new explicit slot choice is required");
            Assert.IsEmpty(_calls);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OutcomeUnknownBlocksReentryDuringItsOwnRender(bool synchronous)
        {
            OpenEmptyMenu();
            int reentries = 0;
            _duringRender = state =>
            {
                if (state.CurrentPhase != Phase.Disconnected || reentries != 0) return;
                reentries++;
                _presenter.Create(2, 0, "다시");
                _presenter.Select(1);
                _presenter.Refresh();
            };
            if (synchronous)
                _duringSend = bytes => Assert.IsTrue(Receive(Refused(
                    BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4)), 12)));

            _presenter.Create(2, 0, "새벽");
            if (!synchronous) Assert.IsTrue(Receive(Refused(RequestId, 12)));

            Assert.AreEqual(1, reentries);
            Assert.AreEqual(2, _sent.Count);
            CollectionAssert.AreEqual(new[] { "client disconnect" }, _calls);
            Assert.AreEqual(Phase.Disconnected, Last.CurrentPhase);
            Assert.AreEqual("슬롯 2: 결과를 확인하지 못해 연결을 끊습니다. 다시 접속해 목록을 먼저 확인해 주세요.", Label("Result").text);
            Assert.IsFalse(Control("Submit").interactable);
            Assert.IsFalse(Control("Refresh").interactable);
            Assert.IsTrue(Control("Back").interactable);
        }

        [Test]
        public void LegacyDisconnectCallbackCannotReenterOrSendASecondExit()
        {
            OpenEmptyMenu();
            _duringDisconnect = () =>
            {
                _presenter.Back();
                _presenter.Refresh();
                _presenter.Create(1, 0, "다시");
                _presenter.Dispose();
            };
            _presenter.Create(2, 0, "새벽");
            int renders = _states.Count;

            Assert.IsTrue(Receive(Refused(RequestId, 16)));

            CollectionAssert.AreEqual(new[] { "menu disconnect", "exit LegacyClasses" }, _calls);
            Assert.AreEqual(2, _sent.Count);
            Assert.AreEqual(renders, _states.Count, "legacy has no refusal screen");
            Assert.AreEqual("[CharacterMenu] 52 거절: 슬롯 2 결과 16/0", _logs[_logs.Count - 1]);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StaleNotificationAlreadyCapturedByTheClientCannotRenderAfterTeardown(bool replace)
        {
            using var other = new CharacterMenuClient(() => true, _ => { }, () => { }, _ => { });
            // Registered before Attach, so the client's invocation snapshot still contains the old presenter.
            _client.Changed += () =>
            {
                if (_client.LastReply == null) return;
                if (replace) _presenter.Observe(other, true);
                else _presenter.Dispose();
            };
            Assert.IsTrue(_client.TryList());
            _presenter.Observe(_client, true);
            int renders = _states.Count;

            Assert.IsTrue(Receive(EmptyList(RequestId)));

            Assert.AreEqual(replace ? renders + 1 : renders, _states.Count);
            if (replace) Assert.AreEqual(Phase.Disconnected, Last.CurrentPhase);
            Assert.IsEmpty(_logs, "the disposed subscription must not apply the captured reply");
            Assert.IsEmpty(_calls);
        }

        [Test]
        public void LocallyUnsendableRawNameReleasesTheClickLatchAndKeepsTheDraft()
        {
            OpenEmptyMenu();
            Click("Slot1");
            string tooLong = new string('a', 37);
            NameInput().SetTextWithoutNotify(tooLong);
            Click("Submit");

            Assert.AreEqual(1, _sent.Count);
            Assert.IsFalse(_client.HasPending);
            Assert.AreEqual(Phase.Ready, Last.CurrentPhase);
            Assert.AreEqual(tooLong, NameInput().text);
            Assert.IsTrue(Control("Submit").interactable);
            Assert.AreEqual("슬롯 1: 요청을 보내지 못했습니다. 입력을 확인하고 다시 시도해 주세요.", Label("Result").text);

            NameInput().SetTextWithoutNotify(" a ");
            Click("Submit");
            Assert.AreEqual(2, _sent.Count);
            var request = new C_CharacterCreateRequest();
            request.Read(new ArraySegment<byte>(_sent[1]));
            Assert.AreEqual(" a ", request.rawName, "UI must not trim or reproduce server name policy");
            Assert.AreEqual(Phase.Creating, Last.CurrentPhase);
            Assert.IsFalse(Control("Submit").interactable);
        }

        void OpenEmptyMenu()
        {
            Assert.IsTrue(_client.TryList());
            _presenter.Observe(_client, true);
            Assert.IsTrue(Receive(EmptyList(RequestId)));
        }

        bool Receive(ArraySegment<byte> frame)
        {
            Assert.IsTrue(CharacterMenuReplyReader.TryRead(frame, out CharacterMenuReply reply), "valid wire fixture");
            return _client.TryReceive(reply);
        }

        static ArraySegment<byte> EmptyList(uint id) => new S_CharacterListResult
        {
            requestId = id,
            slot1CharacterIdD = "", slot1Name = "",
            slot2CharacterIdD = "", slot2Name = "",
            slot3CharacterIdD = "", slot3Name = "",
        }.Write();

        static ArraySegment<byte> Created(uint id, byte slot) => new S_CharacterCreateResult
        {
            requestId = id, slot = slot, characterIdD = CreatedId, name = "새벽", classId = 0,
        }.Write();

        static ArraySegment<byte> Refused(uint id, byte result, byte field = 0) => new S_CharacterCreateResult
        {
            requestId = id, result = result, invalidField = field, characterIdD = "", name = "",
        }.Write();

        static ushort Kind(byte[] frame) => BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(2));
        Button Control(string path) => Panel.Find(path).GetComponent<Button>();
        TMP_Text Label(string path) => Panel.Find(path).GetComponent<TMP_Text>();
        TMP_InputField NameInput() => Panel.Find("CreateForm/Name").GetComponent<TMP_InputField>();
        void Click(string path) => Control(path).onClick.Invoke();

        void RecordLog(string message, string stackTrace, LogType type)
        {
            if (message.StartsWith("[CharacterMenu]", StringComparison.Ordinal)) _logs.Add(message);
        }
    }
}
