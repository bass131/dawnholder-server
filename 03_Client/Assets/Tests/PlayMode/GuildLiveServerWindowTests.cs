using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using static Dawnholder.Client.Tests.PlayMode.GuildGameplayWindowTestTools;
using static Dawnholder.Client.Tests.PlayMode.GuildLiveServerWindowTestTools;
using static Dawnholder.Client.Tests.PlayMode.GuildUiConnectionTestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Pre-C9 live-server lane, not the final PR2 verdict. Role a and role b run in two GUI editors
    // against one real in-memory GameServer at 127.0.0.1:7777 inside a main-bound window. They cover
    // the approved demo scenes 3/6/7/8 without the Art growth effect (C9), which is not connected.
    // Every observed value comes from the server through public product state, product logs or the
    // screen; the constants below are the approved scene literals, not product formulas.
    [Category(LaneCategory)]
    public sealed class GuildLiveServerWindowTests
    {
        const string GuildName = "새벽 길드";
        const int JoinedTreasury = 10;
        const int ClearedTreasury = 30;
        const int PurchasedTreasury = 0;
        const int ClearGold = 50;
        const int ClearMaterial = 1;
        const int ClearPouch = 1;
        const int EnhanceGold = 20;
        const int EnhanceMaterial = 1;
        const int EnhanceBonus = 5;

        MapEntryPlayFixture _fixture;
        LiveLane _lane;
        LiveLog _log;
        Rendezvous _sync;
        bool _completed;
        int _self;
        int _peer;
        int _firstBoss;
        int _firstBossMaxHp;
        int _damageBefore;
        int _peerDamageBefore;
        int _cheatUses;
        SyncRecord _afterRace;
#if UNITY_EDITOR
        Recorder _viewEvents;
        GameViewScope _view;
#endif

        [SetUp]
        public void Setup()
        {
            _fixture = new MapEntryPlayFixture();
            _lane = null;
            _log = null;
            _sync = null;
            _completed = false;
            _cheatUses = 0;
            _afterRace = null;
#if UNITY_EDITOR
            _viewEvents = null;
            _view = null;
#endif
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (!_completed) _sync?.Abort("role " + _lane?.Role + " stopped: " + TestContext.CurrentContext.Result.Message);
            try { yield return _fixture.Cleanup(); }
            finally
            {
                try
                {
#if UNITY_EDITOR
                    try { _view?.Dispose(); }
                    finally { _viewEvents?.Dispose(); }
#endif
                }
                finally { _log?.Dispose(); }
            }
        }

        [UnityTest, Timeout(LiveTimeoutMs), Explicit(LaneExplicitReason)]
        public IEnumerator RoleA_InvitesClearsRacesAndEnhances_PreC9LiveServer() => Scenario("a");

        [UnityTest, Timeout(LiveTimeoutMs), Explicit(LaneExplicitReason)]
        public IEnumerator RoleB_AcceptsClearsRacesAndKeepsOwnWeapon_PreC9LiveServer() => Scenario("b");

        IEnumerator Scenario(string role)
        {
            _lane = LiveLane.Read(role);
            bool leader = role == "a";
            _log = new LiveLog(_lane);
            _sync = new Rendezvous(_lane, _log);
#if UNITY_EDITOR
            // The existing GameView scope needs its recorder only as an event sink.
            _viewEvents = new Recorder(role + "-gv", 802, 451);
            _view = new GameViewScope(_viewEvents);
            yield return _view.Select(802, 451);
#else
            Assert.Fail("this contract requires the GUI editor main GameView");
#endif
            yield return JoinTownGuild();
            yield return FormParty(leader);
            yield return ReachBossGate(leader);
            yield return ClearBossAndConfirm();
            yield return RaceStronghold();
            if (leader) yield return EnhanceOwnWeapon();
            else yield return KeepOwnWeapon();
            yield return CompareDamage(leader);
            _sync.Post("15-done", new SyncRecord { note = "growth signals observed=" + _log.GrowthSignals });
            yield return _sync.Await("15-done", 300f, _ => { });
            _completed = true;
            _log.Finish();
#if UNITY_EDITOR
            // The reused sink only records missing guild/blacksmith glyphs as failures.
            _viewEvents.Finish();
#endif
        }

        // Scene 3: real login of two test accounts, auto-joined guild, treasury10/level1/members2/online2.
        IEnumerator JoinTownGuild()
        {
            yield return _fixture.Prepare(true);
            yield return _fixture.WaitMap(0);
            UnityClientSession session = _fixture.Session;
            _self = session.LocalEntityId.Value;
            _log.Event("login", "account=" + AccountLoadout.SessionAccountName + "; entity=" + _self + "; class=" +
                session.CharacterLoad.LoadedClass + "; hp=" + session.Entry.CurrentHp + "/" + session.Entry.MaxHp);
            _log.Check(session.CharacterLoad.LoadedClass == CharacterClass.Knight, "actual48 Knight authority");
            _log.Observe(session.Guild);
            _log.ObserveInventory(InventoryState.Instance);
            _sync.Post("01-town", new SyncRecord { account = AccountLoadout.SessionAccountName, entity = _self });
            SyncRecord peer = null;
            // The partner editor starts later and imports its own Library first.
            yield return _sync.Await("01-town", 1200f, record => peer = record);
            _peer = peer.entity;
            _log.Check(peer.account != AccountLoadout.SessionAccountName && _peer != _self, "two distinct test accounts and entities");

            GuildRequestController owner = session.Guild;
            yield return MapEntryPlayFixture.Wait(() => owner.State.HasSnapshot && owner.State.OnlineMemberCount == 2,
                "server44 with both members online", 30);
            yield return ScreenUiProbe.WaitForEventSystem();
            GuildWindow guild = Window<GuildWindow>();
            if (!guild.IsOpen) yield return KeyFrame(_fixture, Key.G);
            _log.Check(guild.IsOpen, "actual G opens the bootstrap guild window");
            int signals = _log.GrowthSignals;
            long sequence = owner.State.SnapshotSequence;
            yield return Click(_fixture, _log, Button(guild, "Refresh"), "guild refresh43");
            yield return MapEntryPlayFixture.Wait(() => !owner.IsPending && owner.State.SnapshotSequence > sequence,
                "refresh Query44 from the server", 10);
            yield return _fixture.Frames(3);
            GuildClientState state = owner.State;
            _log.Check(state.GuildName == GuildName && Label(guild, "GuildName") == GuildName, "server guild name shown");
            _log.Check(state.MemberCount == 2 && state.OnlineMemberCount == 2 && Digits(Label(guild, "MemberCount")) == "2" &&
                Digits(Label(guild, "OnlineCount")) == "2", "members2/online2 from server and shown");
            _log.Check(state.Treasury == JoinedTreasury && Digits(Label(guild, "Treasury")) == "10", "treasury10 from server and shown");
            _log.Check(state.StrongholdLevel == 1 && !state.BlacksmithUnlocked && Digits(Label(guild, "BaseLevel")) == "12",
                "stronghold level 1 of 2 shown, blacksmith locked");
            _log.Event("labels-town", "name=" + Label(guild, "GuildName") + "; members=" + Label(guild, "MemberCount") +
                "; online=" + Label(guild, "OnlineCount") + "; treasury=" + Label(guild, "Treasury") + "; level=" + Label(guild, "BaseLevel"));
            // C9 precursor only: no product growth signal on initial sync/Query. The Art call itself is not connected.
            _log.Event("growth-signal-initial", "before refresh=" + signals + "; after=" + _log.GrowthSignals);
            yield return _log.Capture(_fixture, "01-town", "two accounts joined; refreshed Query44");
            _sync.Post("02-joined", new SyncRecord { guildRevision = state.GuildRevision, guildSequence = state.SnapshotSequence,
                treasury = state.Treasury, level = state.StrongholdLevel, members = state.MemberCount, online = state.OnlineMemberCount });
            yield return _sync.Await("02-joined", 120f, record => peer = record);
            _log.Check(peer.guildRevision == state.GuildRevision && peer.treasury == state.Treasury,
                "both editors read the same shared revision and treasury");
            yield return KeyFrame(_fixture, Key.G);
            _log.Check(!guild.IsOpen, "actual G closes the guild window");
        }

        // Scene 6 entry: real P invite and popup accept form the two-member party in Town.
        IEnumerator FormParty(bool leader)
        {
            if (leader)
            {
                yield return MapEntryPlayFixture.Wait(() => RemoteEntityRegistry.Instance != null &&
                    RemoteEntityRegistry.Instance.TryGetTransform(_peer, out _), "partner in the Town roster", 20);
                yield return KeyFrame(_fixture, Key.P);
                yield return MapEntryPlayFixture.Wait(() => _log.ProductContains("targetEntityId=" + _peer + "."),
                    "actual P invite intent to the partner", 5);
            }
            else
            {
                yield return MapEntryPlayFixture.Wait(() => PartyState.Instance != null && PartyState.Instance.HasPendingInvite,
                    "server party invitation", 60);
                _log.Check(PartyState.Instance.PendingInviterEntityId == _peer, "invitation comes from the partner entity");
                PartyInvitePopup popup = PartyInvitePopup.Instance;
                Assert.IsNotNull(popup, "bootstrap party popup");
                Component accept = Button(popup, "AcceptButton");
                yield return MapEntryPlayFixture.Wait(() => ScreenUiProbe.IsShown(accept), "invitation popup shown", 5);
                yield return _log.Capture(_fixture, "02-invite", "server invitation popup");
                yield return Click(_fixture, _log, accept, "party accept");
            }
            yield return MapEntryPlayFixture.Wait(() => PartyState.Instance.InParty &&
                new[] { PartyState.Instance.Member0EntityId, PartyState.Instance.Member1EntityId }.OrderBy(id => id)
                    .SequenceEqual(new[] { _self, _peer }.OrderBy(id => id)), "server two-member party", 60);
            int party = PartyState.Instance.PartyId;
            _sync.Post("03-party", new SyncRecord { partyId = party });
            SyncRecord peer = null;
            yield return _sync.Await("03-party", 120f, record => peer = record);
            _log.Check(peer.partyId == party, "both editors are in the same server party");
            if (leader) yield return _log.Capture(_fixture, "03-party", "party formed");
        }

        // Boss gate in the party HuntingGround; damage, death, rewards and credit stay real either way.
        IEnumerator ReachBossGate(bool leader)
        {
            yield return _fixture.WalkIntoPortal(1, 1);
            _sync.Post("04-hg", new SyncRecord { entity = _self });
            yield return _sync.Await("04-hg", 120f, _ => { });
            yield return MapEntryPlayFixture.Wait(() => RemoteEntityRegistry.Instance.TryGetTransform(_peer, out _),
                "partner in the same party HuntingGround instance", 20);
            yield return _log.Capture(_fixture, "04-hg", "party HuntingGround instance");
            yield return EnsureBossGate(leader, "first boss");
            yield return _log.Capture(_fixture, "05-quest", "boss gate reached by " + _lane.QuestGate + "; F8 uses " + _cheatUses);
            _sync.Post("05-quest", new SyncRecord { note = QuestState.Instance.CurrentCount + "/" + QuestState.Instance.TargetCount });
            yield return _sync.Await("05-quest", 660f, _ => { });
        }

        // Main msg_70d77a899d35 (addendum v1.2): with the debug-f8 plan the party leader presses the existing F8
        // gate (C_CheatCommand 34, cheatType 0) at most once before each boss entry, two uses in total, and only
        // while the existing server Debug/AllowCheats settings open it. Without a server quest update the lane
        // switches to the normal party quest. A boss kill clears party progress, not the entity unlock, so the
        // second entry normally needs no gate at all.
        IEnumerator EnsureBossGate(bool leader, string label)
        {
            if (QuestDone())
            {
                _log.Event("quest-gate " + label, "already open: " + QuestState.Instance.CurrentCount + "/" + QuestState.Instance.TargetCount);
                yield break;
            }
            if (_lane.QuestGate == "debug-f8")
            {
                if (leader)
                {
                    Assert.Less(_cheatUses, 2, "the F8 quest gate is limited to two uses");
                    int sends = _log.CountProduct("[CheatSender] C_CheatCommand");
                    _cheatUses++;
                    _log.Event("f8-press " + label, "use=" + _cheatUses + "; account=" + AccountLoadout.SessionAccountName +
                        "; utc=" + DateTime.UtcNow.ToString("O"));
                    yield return KeyFrame(_fixture, Key.F8);
                    _log.Check(_log.CountProduct("[CheatSender] C_CheatCommand") == sends + 1, "one F8 press logged one local 34 send");
                }
                bool opened = false;
                yield return ScreenUiProbe.WaitUntil(QuestDone, 20f, done => opened = done);
                _log.Event("quest-gate " + label, "debug-f8 server quest update=" + opened + "; quest=" +
                    QuestState.Instance.CurrentCount + "/" + QuestState.Instance.TargetCount + "; product quest log=" + _log.CountProduct("[Quest]"));
                if (opened) yield break;
                _log.Event("quest-gate " + label, "existing F8 gate did not open; planned switch to the normal quest");
            }
            yield return Hunt(600f);
            _log.Event("quest-gate " + label, "normal quest " + QuestState.Instance.CurrentCount + "/" + QuestState.Instance.TargetCount);
        }

        // Scene 6: real boss fight; each member's 39/36 reward and the shared credit; confirm40 and repeat Query.
        IEnumerator ClearBossAndConfirm()
        {
            yield return _fixture.WalkIntoPortal(1, 2);
            RemoteEnemy boss = null;
            yield return MapEntryPlayFixture.Wait(() => (boss = NearestEnemy(_fixture, enemy => enemy.Kind == EnemyKind.Boss)) != null,
                "boss spawn in the party BossRoom", 20);
            _firstBoss = boss.EntityId;
            _firstBossMaxHp = boss.MaxHp;
            int rowsAtEntry = _log.InventoryRows.Count;
            _sync.Post("06-boss", new SyncRecord { bossId = _firstBoss, bossMaxHp = _firstBossMaxHp });
            SyncRecord peer = null;
            yield return _sync.Await("06-boss", 120f, record => peer = record);
            _log.Check(peer.bossId == _firstBoss, "both party members see the same boss room copy");
            yield return _log.Capture(_fixture, "06-boss", "boss " + _firstBoss + " maxHp " + _firstBossMaxHp);

            HitObservation first = default;
            yield return OwnHit(_fixture, _log, _firstBoss, 90f, hit => first = hit);
            _damageBefore = first.Damage;
            _log.Check(first.Effect == "Melee", "baseline is a Knight melee hit: " + first.Effect);
            _log.Event("damage-before", "attacker=" + _self + "; boss=" + _firstBoss + "; kind=" + boss.Kind + "; dmg=" + first.Damage +
                "; hp=" + first.Hp + "/" + first.MaxHp + "; effect=" + first.Effect + "; utc=" + first.Utc);
            _sync.Post("07-dmg1", new SyncRecord { bossId = _firstBoss, damage = _damageBefore });
            yield return _sync.Await("07-dmg1", 120f, record => _peerDamageBefore = record.damage);

            float deadline = Time.realtimeSinceStartup + 240f;
            while (!_log.ProductContains("Entity " + _firstBoss + " died") && !RewardShown())
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, "boss did not die from bounded real attacks");
                RemoteEnemy target = Enemy(_firstBoss);
                if (target == null)
                {
                    yield return null;
                    continue;
                }
                if (Mathf.Abs(target.transform.position.x - _fixture.Player.transform.position.x) > 1f)
                    yield return WalkToX(_fixture, target.transform.position.x, 0.8f, 10f, "boss");
                yield return Swing(_fixture, _log);
                yield return _fixture.Frames(6);
            }
            yield return MapEntryPlayFixture.Wait(RewardShown, "server39 reward window", 20);
            DungeonClearRewardWindow window = DungeonClearRewardWindow.Instance;
            _log.Event("reward-labels", string.Join(" | ", ScreenUiProbe.ShownTextsUnder(window.transform).Select(text => text.name + "=" + text.text)));
            bool rewardStep = false;
            yield return ScreenUiProbe.WaitUntil(() => RewardStep(rowsAtEntry), 10f, done => rewardStep = done);
            _log.Check(rewardStep, "one server36 step of +" + ClearGold + " gold/+" + ClearMaterial + " material/+" + ClearPouch + " pouch");
            yield return _log.Capture(_fixture, "08-reward", "server reward window and personal36");
            GuildRequestController owner = _fixture.Session.Guild;
            _sync.Post("08-reward", new SyncRecord { treasury = owner.State.Treasury, guildRevision = owner.State.GuildRevision,
                gold = InventoryState.Instance.Currency, inventoryRevision = InventoryState.Instance.Revision });
            yield return _sync.Await("08-reward", 120f, record => peer = record);

            yield return Click(_fixture, _log, Button(window, "Confirm"), "reward confirm40");
            yield return _fixture.WaitMap(0);
            _log.Check(!RewardShown(), "confirm consumed the reward window");
            GuildWindow guild = Window<GuildWindow>();
            if (!guild.IsOpen) yield return KeyFrame(_fixture, Key.G);
            yield return QueryGuild(guild, "after confirm40");
            GuildClientState state = owner.State;
            _log.Check(state.Treasury == ClearedTreasury && state.StrongholdLevel == 1 && Digits(Label(guild, "Treasury")) == "30",
                "shared treasury30 after two beneficiaries, shown in Town");
            yield return _log.Capture(_fixture, "09-town-query", "Town Query44 after confirm40");
            uint credited = state.GuildRevision;
            _sync.Post("09-confirmed", new SyncRecord { treasury = state.Treasury, guildRevision = credited, guildSequence = state.SnapshotSequence });
            yield return _sync.Await("09-confirmed", 180f, record => peer = record);
            _log.Check(peer.guildRevision == credited && peer.treasury == state.Treasury, "both editors read one credited revision");
            // Both confirms are now done; a further Query must not find another credit.
            yield return QueryGuild(guild, "after both confirms");
            _log.Check(owner.State.Treasury == ClearedTreasury && owner.State.GuildRevision == credited,
                "confirm40 and repeated Query44 did not credit again");
        }

        // Scene 7: both editors press level-up at one agreed instant from the same observed revision.
        IEnumerator RaceStronghold()
        {
            GuildRequestController owner = _fixture.Session.Guild;
            GuildWindow guild = Window<GuildWindow>();
            if (!guild.IsOpen) yield return KeyFrame(_fixture, Key.G);
            Component levelUp = Button(guild, "LevelUp");
            yield return MapEntryPlayFixture.Wait(() => !owner.IsPending && owner.State.StrongholdAvailability == GuildResult.Success &&
                InventoryPanelProbe.Interactable(levelUp), "level-up available at the shared treasury", 15);
            uint revision = owner.State.GuildRevision;
            InventoryState inventory = InventoryState.Instance;
            int gold = inventory.Currency;
            int material = CountItem(inventory, ItemId.Material);
            uint personal = inventory.Revision;
            yield return _log.Capture(_fixture, "10-race-ready", "both windows open at revision " + revision);
            Vector2 point = ScreenUiProbe.ScreenPoint(levelUp);
            GameObject[] hits = ScreenUiProbe.RaycastHits(point);
            _log.Check(hits.Length > 0 && hits[0].transform.IsChildOf(levelUp.transform), "race pointer targets level-up");
            var ready = new SyncRecord { guildRevision = revision, treasury = owner.State.Treasury };
            _sync.Post("10-race-ready", ready);
            SyncRecord peer = null;
            yield return _sync.Await("10-race-ready", 120f, record => peer = record);
            _log.Check(peer.guildRevision == revision, "both editors observed the same shared revision before the race");
            DateTime fire = Later(ready.utc, peer.utc).AddSeconds(2);
            int attacks = _log.CountProduct("[Attack]");
            int starts = _log.PendingStarts;
            yield return PressAndHold(_fixture, point);
            while (DateTime.UtcNow < fire) yield return null;
            uint atRelease = owner.State.GuildRevision;
            bool interactable = InventoryPanelProbe.Interactable(levelUp);
            Release(_fixture, point);
            string releaseUtc = DateTime.UtcNow.ToString("O");
            bool pendingSeen = false;
            yield return ScreenUiProbe.WaitUntil(() => _log.PendingStarts > starts, 1f, left => pendingSeen = left);
            _log.Event("race-release", "fire=" + fire.ToString("O") + "; release=" + releaseUtc + "; revisionAtRelease=" + atRelease +
                "; interactable=" + interactable + "; pendingSeen=" + pendingSeen + "; " + LiveLog.Describe(owner));
            yield return _log.Capture(_fixture, "11-race-sent", "after release; pendingSeen=" + pendingSeen);
            yield return MapEntryPlayFixture.Wait(() => !owner.IsPending && owner.State.StrongholdLevel == 2,
                "server47/44 settle the race at level2", 15);
            GuildResult? result = owner.LastResult;
            yield return _fixture.Frames(3);
            yield return _log.Capture(_fixture, "11-race-result", "own result " + result);
            _log.Check(_log.CountProduct("[Attack]") == attacks, "race pointer emitted no C_Attack");
            _afterRace = new SyncRecord
            {
                result = result.HasValue ? result.Value.ToString() : "none", requestedRevision = atRelease,
                pendingAfterClick = pendingSeen, clickUtc = releaseUtc, treasury = owner.State.Treasury,
                level = owner.State.StrongholdLevel, guildRevision = owner.State.GuildRevision, gold = inventory.Currency,
                material = CountItem(inventory, ItemId.Material), inventoryRevision = inventory.Revision,
                weaponLevel = owner.State.WeaponLevel, attackBonus = owner.State.AttackBonus,
            };
            _sync.Post("11-race", _afterRace);
            yield return _sync.Await("11-race", 120f, record => peer = record);
            bool serverRace = pendingSeen && peer.pendingAfterClick && atRelease == revision && peer.requestedRevision == revision;
            _log.Check(serverRace, "both purchase intents left from revision " + revision + " (not a local gate)");
            string[] results = { _afterRace.result, peer.result };
            _log.Check(results.Count(item => item == "Success") == 1 && results.Count(item => item == "Stale") == 1,
                "server confirmed one success and one stale: " + string.Join(",", results));
            _log.Check(owner.State.Treasury == PurchasedTreasury && owner.State.StrongholdLevel == 2 && owner.State.BlacksmithUnlocked &&
                Digits(Label(guild, "Treasury")) == "0" && Digits(Label(guild, "BaseLevel")) == "22",
                "treasury0, level 2 of 2 and blacksmith unlocked from server and shown");
            _log.Check(inventory.Currency == gold && CountItem(inventory, ItemId.Material) == material && inventory.Revision == personal,
                "the race consumed no personal gold, material or revision");
            yield return KeyFrame(_fixture, Key.G);
        }

        // Scene 8 (role a): real blacksmith entrance and enhance; 47+36+44 decide level1/bonus5.
        IEnumerator EnhanceOwnWeapon()
        {
            BlacksmithWindow smith = null;
            yield return OpenBlacksmith(window => smith = window);
            GuildRequestController owner = _fixture.Session.Guild;
            InventoryState inventory = InventoryState.Instance;
            int gold = inventory.Currency;
            int material = CountItem(inventory, ItemId.Material);
            uint personal = inventory.Revision;
            _log.Check(owner.State.WeaponLevel == 0 && owner.State.WeaponAvailability == GuildResult.Success,
                "unenhanced weapon available after the shared unlock");
            yield return _log.Capture(_fixture, "12-smith-before", "gold=" + gold + "; material=" + material);
            // LastResult still holds the race result until a new request leaves, so require that start first.
            int starts = _log.PendingStarts;
            yield return Click(_fixture, _log, Button(smith, "Enhance"), "enhance46");
            _log.Check(_log.PendingStarts > starts, "enhance intent left the client (pending started)");
            yield return MapEntryPlayFixture.Wait(() => _log.PendingStarts > starts && owner.LastResult.HasValue && !owner.IsPending,
                "server enhance result", 15);
            _log.Check(owner.LastResult == GuildResult.Success, "server enhance result " + owner.LastResult);
            yield return MapEntryPlayFixture.Wait(() => inventory.Revision > personal, "server36 after enhance", 10);
            yield return _fixture.Frames(3);
            _log.Check(inventory.Currency == gold - EnhanceGold && CountItem(inventory, ItemId.Material) == material - EnhanceMaterial &&
                inventory.Revision == personal + 1, "one36 consumed gold20/material1");
            _log.Check(owner.State.WeaponLevel == 1 && owner.State.AttackBonus == EnhanceBonus && Digits(Label(smith, "EnhanceLevel")) == "11" &&
                Digits(Label(smith, "AttackBonus")) == "5", "weapon level 1 of 1 / bonus5 from server and shown");
            _log.Event("labels-enhanced", "level=" + Label(smith, "EnhanceLevel") + "; bonus=" + Label(smith, "AttackBonus") +
                "; result=" + Label(smith, "ActionResult") + "; balance=" + Label(smith, "PersonalBalance"));
            yield return _log.Capture(_fixture, "12-enhanced", "own enhance confirmed");
            _sync.Post("12-enhanced", new SyncRecord { gold = inventory.Currency, material = CountItem(inventory, ItemId.Material),
                inventoryRevision = inventory.Revision, weaponLevel = owner.State.WeaponLevel, attackBonus = owner.State.AttackBonus });
            SyncRecord peer = null;
            yield return _sync.Await("12-peer", 300f, record => peer = record);
            _log.Check(peer.weaponLevel == 0 && peer.attackBonus == 0, "partner weapon stayed level0/bonus0");
            yield return Click(_fixture, _log, Button(smith, "Close"), "blacksmith close");
        }

        // Scene 8 (role b): after the partner enhances, this member's own server values stay unchanged.
        IEnumerator KeepOwnWeapon()
        {
            SyncRecord peer = null;
            yield return _sync.Await("12-enhanced", 300f, record => peer = record);
            BlacksmithWindow smith = null;
            yield return OpenBlacksmith(window => smith = window);
            GuildRequestController owner = _fixture.Session.Guild;
            long sequence = owner.State.SnapshotSequence;
            yield return Click(_fixture, _log, Button(smith, "Refresh"), "blacksmith refresh43");
            yield return MapEntryPlayFixture.Wait(() => !owner.IsPending && owner.State.SnapshotSequence > sequence, "own Query44", 10);
            yield return _fixture.Frames(3);
            InventoryState inventory = InventoryState.Instance;
            _log.Check(owner.State.WeaponLevel == 0 && owner.State.AttackBonus == 0 && Digits(Label(smith, "EnhanceLevel")) == "01" &&
                Digits(Label(smith, "AttackBonus")) == "0", "own weapon level 0 of 1 / bonus0 from server and shown");
            _log.Check(inventory.Currency == _afterRace.gold && CountItem(inventory, ItemId.Material) == _afterRace.material &&
                inventory.Revision == _afterRace.inventoryRevision, "partner enhance did not change own gold, material or revision");
            _log.Check(peer.weaponLevel == 1 && peer.attackBonus == EnhanceBonus, "partner reported level1/bonus5");
            yield return _log.Capture(_fixture, "12-peer", "own values after partner enhance");
            _sync.Post("12-peer", new SyncRecord { gold = inventory.Currency, material = CountItem(inventory, ItemId.Material),
                inventoryRevision = inventory.Revision, weaponLevel = owner.State.WeaponLevel, attackBonus = owner.State.AttackBonus });
            yield return Click(_fixture, _log, Button(smith, "Close"), "blacksmith close");
        }

        // Scene 8 damage: a fresh boss of the same kind/maxHp; one own melee hit each, compared with scene 6.
        IEnumerator CompareDamage(bool leader)
        {
            yield return _fixture.WalkIntoPortal(1, 1);
            yield return EnsureBossGate(leader, "second boss");
            yield return _fixture.WalkIntoPortal(1, 2);
            RemoteEnemy boss = null;
            yield return MapEntryPlayFixture.Wait(() => (boss = NearestEnemy(_fixture, enemy => enemy.Kind == EnemyKind.Boss)) != null,
                "second boss spawn", 20);
            int bossId = boss.EntityId;
            _log.Check(bossId != _firstBoss && boss.MaxHp == _firstBossMaxHp && boss.CurrentHp == boss.MaxHp,
                "fresh boss " + bossId + " of the same kind and maxHp " + boss.MaxHp);
            _sync.Post("13-boss2", new SyncRecord { bossId = bossId, bossMaxHp = boss.MaxHp });
            SyncRecord peer = null;
            yield return _sync.Await("13-boss2", 180f, record => peer = record);
            _log.Check(peer.bossId == bossId, "both members measure the same second boss");
            yield return _log.Capture(_fixture, "13-boss2", "second boss " + bossId);
            HitObservation after = default;
            yield return OwnHit(_fixture, _log, bossId, 90f, hit => after = hit);
            _log.Check(after.Effect == "Melee", "comparison is a Knight melee hit: " + after.Effect);
            _log.Event("damage-after", "attacker=" + _self + "; boss=" + bossId + "; kind=" + boss.Kind + "; dmg=" + after.Damage +
                "; hp=" + after.Hp + "/" + after.MaxHp + "; effect=" + after.Effect + "; utc=" + after.Utc + "; before=" + _damageBefore +
                " on boss " + _firstBoss);
            _sync.Post("14-dmg2", new SyncRecord { bossId = bossId, damage = after.Damage });
            yield return _sync.Await("14-dmg2", 180f, record => peer = record);
            int own = after.Damage - _damageBefore;
            int other = peer.damage - _peerDamageBefore;
            _log.Check(own == (leader ? EnhanceBonus : 0) && other == (leader ? 0 : EnhanceBonus),
                "observed damage difference own=" + own + ", partner=" + other + "; enhancer +5, other 0");
            yield return _log.Capture(_fixture, "14-dmg2", "own delta " + own + "; partner delta " + other);
        }

        IEnumerator OpenBlacksmith(Action<BlacksmithWindow> opened)
        {
            float x = SmithNpc().GetComponent<Collider2D>().bounds.center.x;
            yield return WalkToX(_fixture, x, 0.4f, 20f, "Town blacksmith");
            Component entrance = Entrance("BlacksmithEntrance");
            yield return MapEntryPlayFixture.Wait(() => ScreenUiProbe.IsShown(entrance), "entrance from the real trigger overlap", 5);
            yield return Click(_fixture, _log, entrance, "blacksmith entrance");
            BlacksmithWindow smith = Window<BlacksmithWindow>();
            _log.Check(smith.IsOpen, "entrance pointer opens the bootstrap blacksmith");
            opened(smith);
        }

        IEnumerator QueryGuild(GuildWindow guild, string reason)
        {
            GuildRequestController owner = _fixture.Session.Guild;
            long sequence = owner.State.SnapshotSequence;
            yield return Click(_fixture, _log, Button(guild, "Refresh"), "guild refresh43 " + reason);
            yield return MapEntryPlayFixture.Wait(() => !owner.IsPending && owner.State.SnapshotSequence > sequence, "Query44 " + reason, 10);
            yield return _fixture.Frames(3);
            _log.Event("query " + reason, LiveLog.Describe(owner));
        }

        IEnumerator Hunt(float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            var skipped = new Dictionary<int, float>();
            while (!QuestDone())
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, "normal-play quest gate not reached: " +
                    QuestState.Instance.CurrentCount + "/" + QuestState.Instance.TargetCount);
                RemoteEnemy target = NearestEnemy(_fixture, enemy => enemy.Kind != EnemyKind.Boss &&
                    !(skipped.TryGetValue(enemy.EntityId, out float until) && until > Time.realtimeSinceStartup));
                if (target == null)
                {
                    _fixture.Keys();
                    yield return _fixture.Frames(10);
                    continue;
                }
                int id = target.EntityId;
                // An enemy the keyboard cannot reach (for example on another height) is skipped for a while.
                float chase = Time.realtimeSinceStartup + 12f;
                while (!QuestDone() && Enemy(id) != null && Time.realtimeSinceStartup < chase)
                {
                    float dx = Enemy(id).transform.position.x - _fixture.Player.transform.position.x;
                    if (Mathf.Abs(dx) > 1f)
                    {
                        _fixture.Keys(dx > 0 ? Key.D : Key.A);
                        yield return null;
                        continue;
                    }
                    yield return Swing(_fixture, _log);
                    yield return _fixture.Frames(6);
                }
                if (Enemy(id) != null) skipped[id] = Time.realtimeSinceStartup + 10f;
            }
            _fixture.Keys();
        }

        // The reward is one personal revision step of exactly the approved amounts; ordinary kill gold is a separate step.
        bool RewardStep(int start)
        {
            IReadOnlyList<InventoryRow> rows = _log.InventoryRows;
            for (int i = Math.Max(1, start); i < rows.Count; i++)
            {
                InventoryRow before = rows[i - 1];
                InventoryRow after = rows[i];
                if (after.Revision == before.Revision + 1 && after.Gold - before.Gold == ClearGold &&
                    after.Material - before.Material == ClearMaterial && after.Pouch - before.Pouch == ClearPouch) return true;
            }
            return false;
        }

        // Digits only, as the existing HasNumbers reads labels, but recorded as a check instead of an assert.
        static string Digits(string text) => new string(text.Where(char.IsDigit).ToArray());

        static bool QuestDone() => QuestState.Instance != null && QuestState.Instance.TargetCount > 0 &&
            QuestState.Instance.CurrentCount >= QuestState.Instance.TargetCount;

        static DateTime Later(string first, string second)
        {
            DateTime a = DateTime.Parse(first, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime();
            DateTime b = DateTime.Parse(second, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime();
            return a > b ? a : b;
        }
    }
}
