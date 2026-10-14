using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using NUnit.Framework;
using Shared.GameData;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests
{
    // Re-verification of the main merge (goal 2026-10-11-multi-account-login 「PR 경계와 점검」: Content's dungeon
    // packets took 39·40, so login moved to 41·42). G2: in the client dispatch table the login result (42) and the
    // dungeon clear result (39) each reach only their own handler, and the server-bound 40·41 reach none. Every input
    // is literal wire bytes fed to UnityClientSession.OnRecvPacket, and main-thread work goes to a queue the test
    // drains. Observed: the session's LoginResultReceived calls and the DungeonClearRewardState the handler fills.
    public sealed class LoginAndDungeonResultDispatchTests
    {
        // S_DungeonClearResult, ID 39, 33 bytes (dungeon goal P1): Success, 50 gold, Material x1, CoinPouch x1, empty.
        static readonly byte[] DungeonClearSuccessWire =
        {
            33, 0, 39, 0,
            0,
            50, 0, 0, 0,
            1, 0, 0, 0, 1, 0, 0, 0,
            2, 0, 0, 0, 1, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
        };

        // Server-bound packets arriving at the client: C_DungeonClearConfirm (40, reserved 0) and C_Login (41, "abc").
        static readonly byte[] ConfirmWire = { 5, 0, 40, 0, 0 };
        static readonly byte[] LoginRequestWire = { 12, 0, 41, 0, 6, 0, (byte)'a', 0, (byte)'b', 0, (byte)'c', 0 };

        ManualConnectionQueue _queue;
        UnityClientSession _session;
        PublishedSingleton<DungeonClearRewardState> _reward;
        List<LoginResult> _loginResults;
        int _rewardChanges;

        [SetUp]
        public void Setup()
        {
            _queue = new ManualConnectionQueue();
            _session = new UnityClientSession(() => true, _queue.Post);
            _loginResults = new List<LoginResult>();
            _session.LoginResultReceived += _loginResults.Add;
            _reward = new PublishedSingleton<DungeonClearRewardState>();
            _rewardChanges = 0;
            _reward.Value.Changed += () => _rewardChanges++;
        }

        [TearDown]
        public void Teardown()
        {
            _session.Cleanup();
            _reward.Dispose();
        }

        // G2: S_LoginResult as the literal bytes 05 00 2A 00 <result>, for Accepted (0, main gate path) and
        // AlreadyOnline (2, rejection path). Observed after main drains: that one result is raised, and the dungeon
        // reward state is untouched (no Changed, nothing pending for this session).
        [TestCase((byte)0, LoginResult.Accepted)]
        [TestCase((byte)2, LoginResult.AlreadyOnline)]
        public void G2_LoginResult42_RaisesOnlyTheLoginResult_AndLeavesTheDungeonRewardEmpty(byte wireResult,
            LoginResult expected)
        {
            _session.OnRecvPacket(new byte[] { 5, 0, 42, 0, wireResult });
            _queue.Drain();

            bool rewardPending = _reward.Value.HasPendingFor(_session);
            CollectionAssert.AreEqual(new[] { expected }, _loginResults);
            Assert.AreEqual(0, _rewardChanges, "a login result reached the dungeon reward state");
            Assert.IsFalse(rewardPending);
        }

        // G2: the 33 literal bytes of S_DungeonClearResult (39). Observed after main drains: the reward state changes
        // once and holds Success, 50 gold and Material x1, CoinPouch x1, empty for this session; no login result.
        [Test]
        public void G2_DungeonClearResult39_FillsOnlyTheDungeonReward_AndRaisesNoLoginResult()
        {
            _session.OnRecvPacket(DungeonClearSuccessWire);
            _queue.Drain();

            DungeonClearRewardState reward = _reward.Value;
            bool rewardPending = reward.HasPendingFor(_session);
            var items = reward.Items.Select(slot => (slot.ItemId, slot.Count)).ToArray();
            Assert.AreEqual(1, _rewardChanges);
            Assert.IsTrue(rewardPending, "the dungeon result is not pending for the session that received it");
            Assert.AreEqual(InventoryResult.Success, reward.Result);
            Assert.AreEqual(50, reward.Currency);
            CollectionAssert.AreEqual(
                new[] { (ItemId.Material, 1), (ItemId.CoinPouch, 1), (ItemId.None, 0) }, items);
            CollectionAssert.IsEmpty(_loginResults, "a dungeon result reached the login result handler");
        }

        // G2: one session receives 42 (Accepted) and then 39. Observed: exactly one login result and exactly one
        // reward change, so neither ID is routed twice or to the other's handler.
        [Test]
        public void G2_OneSession_Login42ThenDungeon39_EachReachesItsOwnHandlerOnce()
        {
            _session.OnRecvPacket(new byte[] { 5, 0, 42, 0, 0 });
            _session.OnRecvPacket(DungeonClearSuccessWire);
            _queue.Drain();

            bool rewardPending = _reward.Value.HasPendingFor(_session);
            CollectionAssert.AreEqual(new[] { LoginResult.Accepted }, _loginResults);
            Assert.AreEqual(1, _rewardChanges);
            Assert.IsTrue(rewardPending);
        }

        // G2: the server-bound IDs 40 (confirm) and 41 (login request) have no client handler. Observed: each is
        // dropped with the session's unknown-ID warning, and neither a login result nor a reward change happens.
        [Test]
        public void G2_ServerBoundIds40And41_AreDroppedAsUnknown_WithoutLoginResultOrReward()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"Unknown PacketId 40\b"));
            LogAssert.Expect(LogType.Warning, new Regex(@"Unknown PacketId 41\b"));

            _session.OnRecvPacket(ConfirmWire);
            _session.OnRecvPacket(LoginRequestWire);
            _queue.Drain();

            CollectionAssert.IsEmpty(_loginResults);
            Assert.AreEqual(0, _rewardChanges);
        }
    }
}
