#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dawnholder.Client.Demo;
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
    // PR268 RED contract v1 R2-R5 (goal B2/B3): the step drives the real view, never the presenter directly.
    // Packet bytes, exact button paths and exit callbacks are observed independently of the step's decisions.
    public sealed class DemoCharacterMenuStepTests
    {
        // Contract R2/R3 fixtures: Knight is class 0, Mage is class 1; expected slots/IDs below are fixed inputs.
        const string KnightId = "00112233-4455-6677-8899-aabbccddeeff";
        const string MageId = "11112233-4455-6677-8899-aabbccddeeff";
        const string OtherMageId = "22112233-4455-6677-8899-aabbccddeeff";
        const string ThirdMageId = "44112233-4455-6677-8899-aabbccddeeff";
        const string CreatedId = "33112233-4455-6677-8899-aabbccddeeff";
        const string RawKnightName = "  데모 기사 "; // R3: whitespace is part of the request, judged by the server.
        static readonly Saved s_knight = new(KnightId, "기존기사", 0);
        static readonly Saved s_mage = new(MageId, "기존법사", 1);
        static readonly Saved s_otherMage = new(OtherMageId, "두번째법사", 1);
        static readonly Saved s_thirdMage = new(ThirdMageId, "세번째법사", 1);
        static readonly Saved s_created = new(CreatedId, "데모기사", 0);

        readonly List<string> _clicks = new();
        readonly List<string> _viewCalls = new();
        readonly List<CharacterMenuPresenter.Exit> _exits = new();
        GameObject _root = null!;
        CharacterMenuView _view = null!;
        CharacterMenuPresenter _presenter = null!;
        FakeConnection _connection = null!;
        CharacterMenuViewState? _last;
        int _menuDisconnects;

        Transform Panel => _root.transform.Find("CharacterMenu/Panel");
        CharacterMenuViewState Last => _last!;

        [SetUp]
        public void SetUp()
        {
            _clicks.Clear();
            _viewCalls.Clear();
            _exits.Clear();
            _last = null;
            _menuDisconnects = 0;
            _connection = new FakeConnection();
            _root = new GameObject("Demo character menu under test", typeof(RectTransform));
            _presenter = new CharacterMenuPresenter(state =>
            {
                _last = state;
                _view.Render(state);
            }, () => _menuDisconnects++, exit => _exits.Add(exit));
            _view = new CharacterMenuView(_root,
                slot =>
                {
                    _viewCalls.Add("select " + slot);
                    _presenter.Select(slot);
                },
                (slot, classId, name) =>
                {
                    _viewCalls.Add("create " + slot);
                    _presenter.Create(slot, classId, name);
                },
                () =>
                {
                    _viewCalls.Add("refresh");
                    _presenter.Refresh();
                },
                () =>
                {
                    _viewCalls.Add("back");
                    _presenter.Back();
                });

            foreach (string path in new[]
            {
                "Slot1", "Slot2", "Slot3", "Submit", "Refresh", "Back",
                "CreateForm/Knight", "CreateForm/Mage", "CreateForm/Create", "CreateForm/Cancel"
            })
            {
                string clickedPath = path;
                Panel.Find(path).GetComponent<Button>().onClick.AddListener(() => _clicks.Add(clickedPath));
            }
            // The contract requires panel-relative lookup even with Legacy buttons of the same names present.
            foreach (string name in new[] { "Knight", "Mage" })
            {
                var legacy = new GameObject(name, typeof(RectTransform), typeof(Button));
                legacy.transform.SetParent(_root.transform, false);
                string clickedPath = "Legacy/" + name;
                legacy.GetComponent<Button>().onClick.AddListener(() => _clicks.Add(clickedPath));
            }
        }

        [TearDown]
        public void TearDown()
        {
            _presenter?.Dispose();
            _view?.Dispose();
            _connection?.Client.Dispose();
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
        }

        [Test]
        public void ExistingKnightInSlotThree_SelectsItsIdOnceAcrossPendingAndEntering()
        {
            // R2: slot 1 Mage, slot 3 Knight. A matching character needs no creation name.
            OpenWithList(s_mage, null, s_knight);
            var step = new DemoCharacterMenuStep(DemoRole.Knight, null);

            var outcome = step.Advance(Last, Panel);

            Assert.AreEqual(DemoCharacterMenuOutcome.Selecting, outcome);
            Assert.AreEqual(Phase.Selecting, Last.CurrentPhase);
            AssertPacketKinds(PacketID.C_CharacterListRequest, PacketID.C_CharacterSelectRequest);
            Assert.AreEqual(KnightId, SelectedId(_connection.Last));
            Assert.Zero(_connection.Count(PacketID.C_CharacterCreateRequest));
            Assert.AreEqual(string.Empty, step.StopReason);

            var pendingOutcome = step.Advance(Last, Panel);
            _connection.Reply(Selected(_connection.LastId, KnightId));
            Assert.AreEqual(Phase.Entering, Last.CurrentPhase, "fixture: selection was approved");
            var enteringOutcome = step.Advance(Last, Panel);

            Assert.AreEqual(DemoCharacterMenuOutcome.Selecting, pendingOutcome);
            Assert.AreEqual(DemoCharacterMenuOutcome.Selecting, enteringOutcome);
            AssertPacketKinds(PacketID.C_CharacterListRequest, PacketID.C_CharacterSelectRequest);
            CollectionAssert.AreEqual(new[] { "Slot3", "Submit" }, _clicks);
            CollectionAssert.AreEqual(new[] { "select 3" }, _viewCalls);
            AssertNoExitOrDisconnect();
        }

        [Test]
        public void TwoMatchingMages_SelectsTheLowerSlotWithoutCreating()
        {
            // R2: fixed occupied slots 2 and 3 are both Mage; slot 2 must win over empty slot 1.
            OpenWithList(null, s_mage, s_otherMage);
            var step = new DemoCharacterMenuStep(DemoRole.Mage, "使わない名前");

            var outcome = step.Advance(Last, Panel);

            Assert.AreEqual(DemoCharacterMenuOutcome.Selecting, outcome);
            Assert.AreEqual(Phase.Selecting, Last.CurrentPhase);
            AssertPacketKinds(PacketID.C_CharacterListRequest, PacketID.C_CharacterSelectRequest);
            Assert.AreEqual(MageId, SelectedId(_connection.Last));
            Assert.Zero(_connection.Count(PacketID.C_CharacterCreateRequest));
            CollectionAssert.AreEqual(new[] { "Slot2", "Submit" }, _clicks);
            CollectionAssert.AreEqual(new[] { "select 2" }, _viewCalls);
            AssertNoExitOrDisconnect();
        }

        [Test]
        public void MissingKnight_CreatesInFirstEmptySlotWithRawNameThenSelectsTheCreatedId()
        {
            // R3: only slot 1 is occupied; expected slot 2 and class 0 come from the contract, not a search helper.
            OpenWithList(s_mage);
            var step = new DemoCharacterMenuStep(DemoRole.Knight, RawKnightName);

            var createOutcome = step.Advance(Last, Panel);

            Assert.AreEqual(DemoCharacterMenuOutcome.Creating, createOutcome);
            Assert.AreEqual(Phase.Creating, Last.CurrentPhase);
            AssertPacketKinds(PacketID.C_CharacterListRequest, PacketID.C_CharacterCreateRequest);
            Assert.AreEqual(2, _connection.Last[8], "R3 slot");
            Assert.AreEqual(0, _connection.Last[9], "R3 Knight class");
            Assert.AreEqual(RawKnightName, CreateName(_connection.Last));
            Assert.AreEqual(RawKnightName, Panel.Find("CreateForm/Name").GetComponent<TMP_InputField>().text);
            Assert.AreEqual(string.Empty, step.StopReason);

            var waitingOutcome = step.Advance(Last, Panel);
            Assert.AreEqual(DemoCharacterMenuOutcome.Waiting, waitingOutcome);
            AssertPacketKinds(PacketID.C_CharacterListRequest, PacketID.C_CharacterCreateRequest);
            CollectionAssert.AreEqual(new[] { "Slot2", "CreateForm/Knight", "CreateForm/Create" }, _clicks);

            _connection.Reply(Created(_connection.LastId, 2, s_created));
            Assert.AreEqual(Phase.Ready, Last.CurrentPhase, "fixture: creation returned the filled slot");
            var selectOutcome = step.Advance(Last, Panel);

            Assert.AreEqual(DemoCharacterMenuOutcome.Selecting, selectOutcome);
            Assert.AreEqual(Phase.Selecting, Last.CurrentPhase);
            AssertPacketKinds(PacketID.C_CharacterListRequest, PacketID.C_CharacterCreateRequest,
                PacketID.C_CharacterSelectRequest);
            Assert.AreEqual(CreatedId, SelectedId(_connection.Last));
            Assert.AreEqual(1, _connection.Count(PacketID.C_CharacterCreateRequest));
            CollectionAssert.AreEqual(new[]
            {
                "Slot2", "CreateForm/Knight", "CreateForm/Create", "Slot2", "Submit"
            }, _clicks);
            CollectionAssert.AreEqual(new[] { "create 2", "select 2" }, _viewCalls);
            AssertNoExitOrDisconnect();
        }

        [TestCase(null)]
        [TestCase("")]
        public void MissingCharacterName_StopsBeforeAnyClickAndRemainsStopped(string? characterName)
        {
            // R4(a), plus the fixed design's empty-string case: no invented fallback name.
            OpenWithList(s_mage);
            var step = new DemoCharacterMenuStep(DemoRole.Knight, characterName);

            var outcome = step.Advance(Last, Panel);

            Assert.AreEqual(DemoCharacterMenuOutcome.Stopped, outcome);
            Assert.IsNotEmpty(step.StopReason);
            string reason = step.StopReason;
            var repeatedOutcome = step.Advance(null, null);
            Assert.AreEqual(DemoCharacterMenuOutcome.Stopped, repeatedOutcome, "Stopped precedes Legacy");
            Assert.AreEqual(reason, step.StopReason);
            Assert.AreEqual(Phase.Ready, Last.CurrentPhase);
            AssertPacketKinds(PacketID.C_CharacterListRequest);
            Assert.IsEmpty(_clicks);
            Assert.IsEmpty(_viewCalls);
            AssertNoExitOrDisconnect();
        }

        [Test]
        public void ThreeOtherClassSlots_StopsWithoutCreatingOrSelecting()
        {
            // R4(b): all three slots are Mage while Knight is requested.
            OpenWithList(s_mage, s_otherMage, s_thirdMage);
            var step = new DemoCharacterMenuStep(DemoRole.Knight, RawKnightName);

            var outcome = step.Advance(Last, Panel);

            Assert.AreEqual(DemoCharacterMenuOutcome.Stopped, outcome);
            Assert.IsNotEmpty(step.StopReason);
            AssertPacketKinds(PacketID.C_CharacterListRequest);
            Assert.Zero(_connection.Count(PacketID.C_CharacterCreateRequest));
            Assert.Zero(_connection.Count(PacketID.C_CharacterSelectRequest));
            Assert.IsEmpty(_clicks);
            Assert.IsEmpty(_viewCalls);
            AssertNoExitOrDisconnect();
        }

        [Test]
        public void RefusedCreation_StopsWithTheVisibleMessageAndNeverRetries()
        {
            OpenWithList(s_mage);
            var step = new DemoCharacterMenuStep(DemoRole.Knight, RawKnightName);
            var createOutcome = step.Advance(Last, Panel);
            Assert.AreEqual(DemoCharacterMenuOutcome.Creating, createOutcome);

            // R4(c): result 6 is NameTaken, as in CharacterMenuPresenterTests.RefusedCreateOrSelect_*.
            _connection.Reply(CreateRefused(_connection.LastId, 6));
            Assert.AreEqual(Phase.Ready, Last.CurrentPhase);
            string visibleMessage = Panel.Find("Result").GetComponent<TMP_Text>().text;
            Assert.IsNotEmpty(visibleMessage, "fixture: refusal was actually drawn");
            Assert.AreEqual(Last.Message, visibleMessage);

            var stoppedOutcome = step.Advance(Last, Panel);
            Assert.AreEqual(DemoCharacterMenuOutcome.Stopped, stoppedOutcome);
            Assert.That(step.StopReason, Does.Contain(visibleMessage));
            var repeatedOutcome = step.Advance(Last, Panel);
            Assert.AreEqual(DemoCharacterMenuOutcome.Stopped, repeatedOutcome);
            AssertPacketKinds(PacketID.C_CharacterListRequest, PacketID.C_CharacterCreateRequest);
            Assert.AreEqual(1, _connection.Count(PacketID.C_CharacterCreateRequest));
            CollectionAssert.AreEqual(new[] { "Slot2", "CreateForm/Knight", "CreateForm/Create" }, _clicks);
            CollectionAssert.AreEqual(new[] { "create 2" }, _viewCalls);
            AssertNoExitOrDisconnect();
        }

        [Test]
        public void SlotRefusal_WaitsForThePresentersRefreshThenStopsWithOneCreation()
        {
            OpenWithList(s_mage);
            var step = new DemoCharacterMenuStep(DemoRole.Knight, RawKnightName);
            var createOutcome = step.Advance(Last, Panel);
            Assert.AreEqual(DemoCharacterMenuOutcome.Creating, createOutcome);

            // R4(d): result 1 / field 1 is InvalidInput / Slot and the presenter itself refreshes once.
            _connection.Reply(CreateRefused(_connection.LastId, 1, 1));
            Assert.AreEqual(Phase.LoadingList, Last.CurrentPhase);
            var waitingOutcome = step.Advance(Last, Panel);
            Assert.AreEqual(DemoCharacterMenuOutcome.Waiting, waitingOutcome);
            AssertPacketKinds(PacketID.C_CharacterListRequest, PacketID.C_CharacterCreateRequest,
                PacketID.C_CharacterListRequest);

            _connection.Reply(List(_connection.LastId, s_mage));
            Assert.AreEqual(Phase.Ready, Last.CurrentPhase);
            var stoppedOutcome = step.Advance(Last, Panel);
            Assert.AreEqual(DemoCharacterMenuOutcome.Stopped, stoppedOutcome);
            Assert.IsNotEmpty(step.StopReason);
            Assert.That(step.StopReason, Does.Contain(Last.Message));
            AssertPacketKinds(PacketID.C_CharacterListRequest, PacketID.C_CharacterCreateRequest,
                PacketID.C_CharacterListRequest);
            Assert.AreEqual(1, _connection.Count(PacketID.C_CharacterCreateRequest));
            Assert.Zero(_connection.Count(PacketID.C_CharacterSelectRequest));
            CollectionAssert.AreEqual(new[] { "Slot2", "CreateForm/Knight", "CreateForm/Create" }, _clicks);
            CollectionAssert.AreEqual(new[] { "create 2" }, _viewCalls);
            AssertNoExitOrDisconnect();
        }

        [Test]
        public void RefusedList_StopsInsteadOfTreatingTheAccountAsEmpty()
        {
            BeginList();
            // R4(e): result 11 is the existing transient-list-refusal fixture, never an empty successful list.
            _connection.Reply(ListRefused(_connection.LastId, 11));
            Assert.AreEqual(Phase.ListFailed, Last.CurrentPhase);
            var step = new DemoCharacterMenuStep(DemoRole.Knight, RawKnightName);

            var outcome = step.Advance(Last, Panel);

            Assert.AreEqual(DemoCharacterMenuOutcome.Stopped, outcome);
            Assert.IsNotEmpty(step.StopReason);
            AssertPacketKinds(PacketID.C_CharacterListRequest);
            Assert.Zero(_connection.Count(PacketID.C_CharacterCreateRequest));
            Assert.Zero(_connection.Count(PacketID.C_CharacterSelectRequest));
            Assert.IsEmpty(_clicks);
            Assert.IsEmpty(_viewCalls);
            AssertNoExitOrDisconnect();
        }

        [Test]
        public void NoMenuState_ReturnsLegacyWithoutTouchingEitherButtonTree()
        {
            // R5/B3: no presenter state, no initial list request; real view and Legacy-name traps both exist.
            Assert.IsNull(_last);
            var step = new DemoCharacterMenuStep(DemoRole.Mage, "unused");

            var outcome = step.Advance(_last, Panel);

            Assert.AreEqual(DemoCharacterMenuOutcome.Legacy, outcome);
            Assert.AreEqual(string.Empty, step.StopReason);
            Assert.IsEmpty(_connection.Sent);
            Assert.IsEmpty(_clicks);
            Assert.IsEmpty(_viewCalls);
            AssertNoExitOrDisconnect();
        }

        [Test]
        public void BeforeTheListReply_WaitsWithoutSendingOrClicking()
        {
            BeginList();
            Assert.AreEqual(Phase.LoadingList, Last.CurrentPhase);
            var step = new DemoCharacterMenuStep(DemoRole.Knight, RawKnightName);

            var outcome = step.Advance(Last, Panel);

            Assert.AreEqual(DemoCharacterMenuOutcome.Waiting, outcome);
            Assert.AreEqual(string.Empty, step.StopReason);
            AssertPacketKinds(PacketID.C_CharacterListRequest);
            Assert.IsEmpty(_clicks);
            Assert.IsEmpty(_viewCalls);
            AssertNoExitOrDisconnect();
        }

        void BeginList()
        {
            Assert.IsTrue(_connection.Client.TryList(), "fixture: connection facade starts the initial list");
            _presenter.Observe(_connection.Client, true);
            Assert.IsNotNull(_last, "fixture: state is captured from the actual presenter render callback");
        }

        void OpenWithList(Saved? slot1 = null, Saved? slot2 = null, Saved? slot3 = null)
        {
            BeginList();
            _connection.Reply(List(_connection.LastId, slot1, slot2, slot3));
            Assert.AreEqual(Phase.Ready, Last.CurrentPhase, "fixture: list ready");
        }

        void AssertPacketKinds(params PacketID[] expected) =>
            CollectionAssert.AreEqual(expected, _connection.Sent.Select(Kind).ToArray(),
                "exact packet order/count also excludes an unsolicited refresh");

        void AssertNoExitOrDisconnect()
        {
            Assert.IsEmpty(_exits, "neither Back nor a Legacy exit was requested");
            Assert.Zero(_menuDisconnects, "presenter disconnect callback");
            Assert.Zero(_connection.Disconnects, "transport disconnect callback");
            Assert.IsTrue(_connection.Open, "the same connection remains open");
        }

        // Copied only as needed from CharacterMenuPresenterTests.WithTheRealView_ClicksBecomeRequestsAndRepliesBecomeItsText
        // and that file's private wire helpers/FakeConnection: private members cannot be shared; existing support is read-only.
        static PacketID Kind(byte[] frame) => (PacketID)BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(2));

        // Select request layout: header 4, requestId 4, reserved 1, UTF-16 byte length 2, then ID.
        static string SelectedId(byte[] frame) =>
            Encoding.Unicode.GetString(frame, 11, BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(9)));

        // Create request layout: header 4, requestId 4, slot/class/reserved 3, UTF-16 byte length 2, then name.
        static string CreateName(byte[] frame) =>
            Encoding.Unicode.GetString(frame, 13, BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(11)));

        static ArraySegment<byte> List(uint requestId, Saved? slot1 = null, Saved? slot2 = null, Saved? slot3 = null) =>
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

        static ArraySegment<byte> ListRefused(uint requestId, byte result) => new S_CharacterListResult
        {
            requestId = requestId,
            result = result,
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

        sealed class FakeConnection
        {
            internal readonly List<byte[]> Sent = new();
            internal readonly CharacterMenuClient Client;
            internal bool Open = true;
            internal int Disconnects;

            internal FakeConnection()
            {
                Client = new CharacterMenuClient(() => Open, frame => Sent.Add(frame.ToArray()), () =>
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
                bool validReply = CharacterMenuReplyReader.TryRead(frame, out CharacterMenuReply reply);
                Assert.IsTrue(validReply, "fixture: valid reply bytes");
                bool replyAccepted = Client.TryReceive(reply);
                Assert.IsTrue(replyAccepted, "fixture: reply matches this connection's pending request");
            }
        }
    }
}
