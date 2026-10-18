using System.Collections;
using System.Linq;
using Dawnholder.Client.Gameplay;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using static Dawnholder.Client.Tests.PlayMode.GuildGameplayWindowTestTools;
using static Dawnholder.Client.Tests.PlayMode.GuildUiConnectionTestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Live PR2 screen/play evidence, not the final PR2 independent verdict. Every view is
    // created by the real scene bootstrap. Only the finite TCP server and input devices
    // are stand-ins; runtime repositioning is explicitly recorded and never saved.
    public sealed class GuildGameplayWindowTests
    {
        MapEntryPlayFixture _fixture;
        Recorder _record;
#if UNITY_EDITOR
        GameViewScope _view;
#endif

        [SetUp]
        public void Setup()
        {
            _fixture = new MapEntryPlayFixture();
            _record = null;
#if UNITY_EDITOR
            _view = null;
#endif
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            try { yield return _fixture.Cleanup(); }
            finally
            {
                try
                {
#if UNITY_EDITOR
                    _view?.Dispose();
#endif
                }
                finally { _record?.Dispose(); }
            }
        }

        [UnityTest]
        public IEnumerator SceneOverlay709_NormalUpperAndPriorityWindows() => ScreenScenario(709, 399);

        [UnityTest]
        public IEnumerator SceneOverlay802_NormalUpperAndPriorityWindows() => ScreenScenario(802, 451);

        [UnityTest]
        public IEnumerator SceneOverlay1920_NormalUpperAndPriorityWindows() => ScreenScenario(1920, 1080);

        [UnityTest]
        public IEnumerator TownTrigger_LocalRemotePointerAndExistingE_QSkills()
        {
            yield return Begin(802, 451);
            yield return EnterTown(_fixture);
            yield return Synchronize(_fixture, unlocked: true);
            yield return ScreenUiProbe.WaitForEventSystem();
            yield return PlaceLocal(_fixture, false, _record);
            Component entrance = Entrance("BlacksmithEntrance");
            _record.Check(!ScreenUiProbe.IsShown(entrance), "far local player has no blacksmith entrance");
            Collider2D trigger = SmithNpc().GetComponent<Collider2D>();
            _fixture.Peer.Send(new S_PlayerJoin { entityId = 701, characterClass = 0,
                spawnX = trigger.bounds.center.x, spawnY = trigger.bounds.center.y }.Write());
            yield return MapEntryPlayFixture.Wait(() => RemoteEntityRegistry.Instance.TryGetTransform(701, out _), "real remote roster");
            yield return _fixture.Frames(6);
            RemoteEntityRegistry.Instance.TryGetTransform(701, out Transform remote);
            _record.Event("remote-near-local-far", "remote=" + remote.position + "; local=" + _fixture.Player.transform.position +
                "; entrance shown=" + ScreenUiProbe.IsShown(entrance));
            _record.Check(!ScreenUiProbe.IsShown(entrance), "a remote player cannot expose the local blacksmith entrance");
            yield return _record.Capture(_fixture, "remote-only");
            _fixture.Peer.Send(new S_PlayerLeave { entityId = 701 }.Write());
            yield return _fixture.Frames(8);

            _record.Check(_fixture.Session.CharacterLoad.LoadedClass == CharacterClass.Knight, "actual48 Knight authority");
            int skills = _fixture.Peer.Count(PacketID.C_SkillUse);
            yield return KeyFrame(_fixture, Key.E);
            _record.Check(_fixture.Peer.Count(PacketID.C_SkillUse) == skills, "Knight E=None outside NPC, no invented skill");
            _record.Check(!NpcDialogPanel.IsShown, "far E does not open the NPC dialogue");
            yield return KeyFrame(_fixture, Key.Q);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_SkillUse) == skills + 1, "baseline real Q skill", 5);
            _record.Check(_fixture.Peer.Last<C_SkillUse>(PacketID.C_SkillUse).skillId == 2, "baseline Knight Q wire is literal Dash2");

            yield return PlaceLocal(_fixture, true, _record);
            _record.Check(ScreenUiProbe.IsShown(entrance), "actual local overlap exposes entrance");
            yield return _record.Click(_fixture, entrance, "near blacksmith entrance");
            BlacksmithWindow smith = Window<BlacksmithWindow>();
            _record.Check(smith.IsOpen, "actual entrance pointer opens the bootstrap blacksmith");
            yield return _record.Click(_fixture, Button(smith, "Close"), "blacksmith close");
            _record.Check(!smith.IsOpen, "close pointer closes blacksmith");
            yield return _record.Click(_fixture, entrance, "blacksmith reopen");
            _record.Check(smith.IsOpen, "near entrance remains usable after close");
            yield return KeyFrame(_fixture, Key.G);
            GuildWindow guild = Window<GuildWindow>();
            _record.Check(guild.IsOpen, "G still opens guild beside blacksmith");
            yield return _record.Capture(_fixture, "near-both");
            int beforeE = _fixture.Peer.Count(PacketID.C_SkillUse);
            yield return KeyFrame(_fixture, Key.E);
            _record.Check(NpcDialogPanel.IsShown, "existing actual E interaction opens NPC dialogue");
            _record.Check(!guild.IsOpen && !smith.IsOpen, "NPC dialogue priority closes owned guild windows");
            _record.Check(_fixture.Peer.Count(PacketID.C_SkillUse) == beforeE, "near E preserves Knight None skill mapping");
            yield return _record.Capture(_fixture, "existing-E-dialogue");
            yield return MapEntryPlayFixture.Wait(() => _fixture.Player.CanUseDash && !_fixture.Player.IsActionLocked,
                "normal Q cooldown, no test reset", 8);
            int beforeQ = _fixture.Peer.Count(PacketID.C_SkillUse);
            yield return KeyFrame(_fixture, Key.Q);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_SkillUse) == beforeQ + 1,
                "existing Q path with dialogue", 5);
            _record.Check(_fixture.Peer.Last<C_SkillUse>(PacketID.C_SkillUse).skillId == 2, "existing Q remains Dash2 beside NPC");
            if (NpcDialogPanel.IsShown) yield return KeyFrame(_fixture, Key.E);
            _record.Check(!NpcDialogPanel.IsShown, "existing E closes dialogue");
            yield return PlaceLocal(_fixture, false, _record);
            _record.Check(!ScreenUiProbe.IsShown(entrance), "leaving the actual trigger hides entrance again");
            _record.Check(_fixture.Peer.Count(PacketID.C_StrongholdUpgrade) == 0 &&
                _fixture.Peer.Count(PacketID.C_WeaponEnhance) == 0, "entrance/close/E/Q never buy");
            yield return _record.Capture(_fixture, "far-again");
            _record.Finish();
        }

        [UnityTest]
        public IEnumerator EntryGates_BeforeReadyAndDeathNoPurchases()
        {
            yield return Begin(802, 451);
            yield return EnterTown(_fixture, ready: false);
            Send(_fixture.Peer, Snapshot(unlocked: true));
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Guild.State.HasSnapshot, "beforeReady actual44", 5);
            yield return ScreenUiProbe.WaitForEventSystem();
            GuildWindow guild = Window<GuildWindow>();
            BlacksmithWindow smith = Window<BlacksmithWindow>();
            yield return KeyFrame(_fixture, Key.G);
            _record.Check(!guild.IsOpen && !smith.IsOpen, "beforeReady G cannot enter either window");
            _record.Check(!InventoryPanelProbe.Interactable(Entrance("GuildEntrance")), "beforeReady entrance disabled");
            yield return _record.Click(_fixture, Entrance("GuildEntrance"), "beforeReady disabled entrance");
            yield return _record.Capture(_fixture, "before-ready");
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            yield return OpenBoth();
            _fixture.Hp(0, 100);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.CurrentHp == 0, "actual HP0", 5);
            yield return _fixture.Frames(4);
            _record.Check(!InventoryPanelProbe.Interactable(Button(guild, "LevelUp")), "HP0 level-up disabled");
            _record.Check(!InventoryPanelProbe.Interactable(Button(smith, "Enhance")), "HP0 enhancement disabled");
            yield return _record.Capture(_fixture, "dead");
            yield return _record.Click(_fixture, Button(guild, "LevelUp"), "HP0 disabled purchase45");
            yield return _record.Click(_fixture, Button(smith, "Enhance"), "HP0 disabled purchase46");
            _record.Check(_fixture.Peer.Count(PacketID.C_StrongholdUpgrade) == 0 &&
                _fixture.Peer.Count(PacketID.C_WeaponEnhance) == 0, "beforeReady and HP0 emit no purchases");
            _record.Check(_fixture.Session.Guild.State.HasSnapshot, "HP0 preserves confirmed display state");
            _fixture.Hp(37, 100);
            yield return _fixture.Frames(6);
            _record.Check(InventoryPanelProbe.Interactable(Button(smith, "Enhance")), "fresh actual HP restores permitted action");
            yield return _record.Capture(_fixture, "alive-again");
            _record.Finish();
        }

        [UnityTest]
        public IEnumerator MenuTransitionAndOutsideTown_NoPurchases()
        {
            yield return Begin(802, 451);
            yield return EnterTown(_fixture);
            yield return Synchronize(_fixture, unlocked: true);
            yield return ScreenUiProbe.WaitForEventSystem();
            yield return OpenBoth();
            GuildWindow guild = Window<GuildWindow>();
            BlacksmithWindow smith = Window<BlacksmithWindow>();
            var owner = _fixture.Session.Guild;
            yield return KeyFrame(_fixture, Key.Escape);
            _record.Check(Time.timeScale == 0f, "actual Escape opens existing pause menu");
            _record.Check(!InventoryPanelProbe.Interactable(Button(guild, "LevelUp")) &&
                !InventoryPanelProbe.Interactable(Button(smith, "Enhance")), "menu blocks both purchase actions");
            yield return KeyFrame(_fixture, Key.G);
            yield return _record.Capture(_fixture, "menu");
            yield return _record.Click(_fixture, Button(guild, "LevelUp"), "menu pointer over purchase45", expectHit: false);
            if (Time.timeScale == 0f)
                yield return _record.Click(_fixture, Button(smith, "Enhance"), "menu pointer over purchase46", expectHit: false);
            _record.Check(Time.timeScale == 0f, "purchase coordinates do not activate menu resume");
            if (Time.timeScale == 0f) yield return KeyFrame(_fixture, Key.Escape);
            _record.Check(Time.timeScale == 1f, "actual Escape resumes");
            long epoch = _fixture.Session.Entry.Epoch;
            _fixture.Peer.Send(new S_MapTransition { destMapId = 1, spawnX = 2, spawnY = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.MapId == 1 && _fixture.Session.Entry.SceneReady,
                "actual transition before fresh HP");
            _record.Check(!_fixture.Session.CanSendGameplay && _fixture.Session.Entry.Epoch > epoch,
                "new entry epoch requires fresh HP");
            yield return KeyFrame(_fixture, Key.G);
            yield return _record.Capture(_fixture, "transition-before-hp");
            _fixture.Hp(31, 100);
            yield return _fixture.WaitMap(1);
            guild = Window<GuildWindow>();
            smith = Window<BlacksmithWindow>();
            yield return KeyFrame(_fixture, Key.G);
            _record.Check(guild.IsOpen, "outside Town guild information can still open");
            _record.Check(!InventoryPanelProbe.Interactable(Button(guild, "LevelUp")) &&
                !InventoryPanelProbe.Interactable(Button(smith, "Enhance")), "outside Town both purchase gates closed");
            yield return _record.Click(_fixture, Button(guild, "LevelUp"), "outsideTown disabled purchase45");
            _record.Check(!Resources.FindObjectsOfTypeAll<TownBlacksmithPrompt>()
                .Any(prompt => prompt.gameObject.scene == guild.gameObject.scene), "outside Town has no blacksmith entrance");
            _record.Check(ReferenceEquals(owner, _fixture.Session.Guild) && owner.State.HasSnapshot,
                "transition preserves connection-owned confirmed guild state");
            yield return _record.Capture(_fixture, "outside-town");
            _record.Check(_fixture.Peer.Count(PacketID.C_StrongholdUpgrade) == 0 &&
                _fixture.Peer.Count(PacketID.C_WeaponEnhance) == 0, "menu/transition/outsideTown emit no45/46");
            _record.Finish();
        }

        IEnumerator Begin(int width, int height)
        {
            _record = new Recorder(TestContext.CurrentContext.Test.Name, width, height);
#if UNITY_EDITOR
            _view = new GameViewScope(_record);
            yield return _view.Select(width, height);
#else
            Assert.Fail("this contract requires the GUI editor main GameView");
            yield break;
#endif
        }

        IEnumerator OpenBoth()
        {
            GuildWindow guild = Window<GuildWindow>();
            BlacksmithWindow smith = Window<BlacksmithWindow>();
            if (!guild.IsOpen) yield return KeyFrame(_fixture, Key.G);
            yield return PlaceLocal(_fixture, true, _record);
            if (!smith.IsOpen) yield return _record.Click(_fixture, Entrance("BlacksmithEntrance"), "open near blacksmith");
            _record.Check(guild.IsOpen && smith.IsOpen, "real G and entrance allow both product windows");
        }

        IEnumerator ScreenScenario(int width, int height)
        {
            yield return Begin(width, height);
            yield return EnterTown(_fixture);
            yield return Synchronize(_fixture);
            yield return ScreenUiProbe.WaitForEventSystem();
            yield return _record.Click(_fixture, Entrance("GuildEntrance"), "actual guild entrance");
            _record.Check(Window<GuildWindow>().IsOpen, "guild entrance pointer opens current view");
            yield return KeyFrame(_fixture, Key.G);
            _record.Check(!Window<GuildWindow>().IsOpen, "actual G closes guild");
            yield return KeyFrame(_fixture, Key.G);
            yield return OpenBoth();
            GuildWindow guild = Window<GuildWindow>();
            BlacksmithWindow smith = Window<BlacksmithWindow>();
            yield return _record.Capture(_fixture, "normal-locked");
            _record.Check(Label(guild, "GuildName") == "새벽 길드" && Label(guild, "Treasury").Contains("40"),
                "normal actual44 literals: guild name and treasury40");
            _record.Check(Label(smith, "GoldCost").Contains("20") && Label(smith, "PersonalBalance").Contains("71"),
                "actual44 price20 and independent36 balance71");
            Component panel = guild.transform.Find("Guild");
            Vector2 background = ScreenUiProbe.ScreenPoint(panel);
            yield return _record.ClickAt(_fixture, background, "drawn panel background", panel);
            yield return _record.Click(_fixture, guild.GetComponentsInChildren<TMPro.TMP_Text>()
                .Single(text => text.name == "GuildName"), "drawn Korean label", expectHit: false);
            yield return _record.Click(_fixture, Button(smith, "Enhance"), "locked disabled purchase46");
            _record.Check(_fixture.Peer.Count(PacketID.C_WeaponEnhance) == 0, "locked facility emits no46");

            int queries = _fixture.Peer.Count(PacketID.C_GuildRequest);
            yield return _record.Click(_fixture, Button(guild, "Refresh"), "actual refresh43");
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_GuildRequest) == queries + 1,
                "real pointer refresh43", 5);
            Send(_fixture.Peer, Snapshot(sequence: 20));
            yield return MapEntryPlayFixture.Wait(() => !_fixture.Session.Guild.IsPending, "refresh Query44", 5);
            yield return _record.Click(_fixture, Button(guild, "LevelUp"), "actual levelup45");
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_StrongholdUpgrade) == 1, "real purchase45", 5);
            _record.Check(_fixture.Peer.Last<C_StrongholdUpgrade>(PacketID.C_StrongholdUpgrade).expectedGuildRevision == 7,
                "purchase45 carries authoritative revision7");
            yield return _record.Capture(_fixture, "purchase-pending");
            Send(_fixture.Peer, Result(1, 7, 8, 3, 30));
            Send(_fixture.Peer, Snapshot(sequence: 30, guildRevision: 8, kind: 3, treasury: 10, unlocked: true));
            yield return MapEntryPlayFixture.Wait(() => !_fixture.Session.Guild.IsPending, "confirmed47+44 upgrade", 5);
            yield return _record.Capture(_fixture, "confirmed-unlocked");
            yield return _record.Click(_fixture, Button(smith, "Enhance"), "actual enhance46");
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_WeaponEnhance) == 1, "real purchase46", 5);
            _record.Check(_fixture.Peer.Last<C_WeaponEnhance>(PacketID.C_WeaponEnhance).expectedInventoryRevision == 3,
                "purchase46 carries authoritative personal revision3");
            Send(_fixture.Peer, Result(2, 3, 8, 4, 40));
            _fixture.Peer.Send(new S_InventorySnapshot { revision = 4, currency = 51, slot0ItemId = 1, slot0Count = 3 }.Write());
            Send(_fixture.Peer, Snapshot(sequence: 40, guildRevision: 8, inventoryRevision: 4,
                kind: 4, treasury: 10, unlocked: true, enhanced: true));
            yield return MapEntryPlayFixture.Wait(() => !_fixture.Session.Guild.IsPending, "confirmed47+36+44 enhance", 5);
            yield return _record.Capture(_fixture, "confirmed-enhanced");
            _record.Check(Label(smith, "AttackBonus").Contains("5") && Label(smith, "ActionResult").Contains("완료"),
                "confirmed server bonus5 and result are visible");
            Send(_fixture.Peer, UpperSnapshot(50));
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Guild.State.SnapshotSequence == 50, "legal upper44", 5);
            yield return _record.Capture(_fixture, "upper-name-and-counts");
            _record.Check(Label(guild, "GuildName") == "가나다라마바사아자차카타파하길드", "16-code-unit literal remains exact");

            yield return KeyFrame(_fixture, Key.I);
            _record.Check(InventoryShown() && !guild.IsOpen && !smith.IsOpen,
                "actual I inventory priority closes product windows");
            yield return _record.Capture(_fixture, "inventory-priority");
            int inventoryQueries = _fixture.Peer.Count(PacketID.C_InventoryRequest);
            yield return _record.Click(_fixture, InventoryPanelProbe.Refresh, "existing inventory refresh35");
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_InventoryRequest) == inventoryQueries + 1,
                "existing inventory refresh still works", 5);
            _fixture.Peer.Send(new S_InventorySnapshot { revision = 4, currency = 51, slot0ItemId = 1, slot0Count = 3 }.Write());
            yield return KeyFrame(_fixture, Key.I);
            yield return OpenBoth();
            int skillBefore = _fixture.Peer.Count(PacketID.C_SkillUse);
            yield return KeyFrame(_fixture, Key.E);
            _record.Check(NpcDialogPanel.IsShown && !guild.IsOpen && !smith.IsOpen, "actual E dialogue has priority");
            _record.Check(_fixture.Peer.Count(PacketID.C_SkillUse) == skillBefore, "existing Knight E=None preserved");
            yield return _record.Capture(_fixture, "npc-priority");
            if (NpcDialogPanel.IsShown) yield return KeyFrame(_fixture, Key.E);
            _record.Check(!NpcDialogPanel.IsShown, "actual E dismisses existing dialogue");
            yield return OpenBoth();
            _fixture.Peer.Send(new S_DungeonClearResult { result = 0, currency = 50,
                reward0ItemId = 1, reward0Count = 1, reward1ItemId = 2, reward1Count = 1 }.Write());
            yield return MapEntryPlayFixture.Wait(RewardShown, "actual39 reward window shown", 5);
            yield return _fixture.Frames(5);
            _record.Check(!guild.IsOpen && !smith.IsOpen, "server39 reward priority closes product windows");
            yield return KeyFrame(_fixture, Key.I);
            yield return _record.Capture(_fixture, "reward-and-inventory-priority");
            Component confirm = Button(DungeonClearRewardWindow.Instance, "Confirm");
            yield return _record.Click(_fixture, confirm, "existing reward confirm40");
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_DungeonClearConfirm) == 1,
                "existing reward confirmation still reachable", 5);
            _record.Check(!RewardShown(), "confirmation consumes the reward and hides its window");
            if (InventoryShown()) yield return KeyFrame(_fixture, Key.I);
            yield return OpenBoth();
            Vector2 closedPoint = ScreenUiProbe.ScreenPoint(Button(guild, "LevelUp"));
            yield return _record.Click(_fixture, Button(smith, "Close"), "smith close after other windows");
            yield return _record.Click(_fixture, Button(guild, "Close"), "guild close after other windows");
            _record.Check(!guild.IsOpen && !smith.IsOpen, "both close controls work after priority windows");
            _record.Check(!ScreenUiProbe.RaycastHits(closedPoint).Any(hit => hit.transform.IsChildOf(guild.transform) ||
                hit.transform.IsChildOf(smith.transform)), "closed windows have zero raycast hits");
            Vector2 world = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            _record.Check(ScreenUiProbe.RaycastHits(world).Length == 0, "world-click control is outside all active UI");
            yield return InventoryPanelProbe.AttackReady(_fixture.Player);
            int attacks = _fixture.Peer.Count(PacketID.C_Attack);
            yield return InventoryPanelProbe.ClickAt(_fixture.Mouse, world);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_Attack) == attacks + 1,
                "actual world pointer attack restored", 5);
            _record.Event("closed-world-control", "before attacks=" + attacks + "; after=" + _fixture.Peer.Count(PacketID.C_Attack) +
                ";45=" + _fixture.Peer.Count(PacketID.C_StrongholdUpgrade) + ";46=" + _fixture.Peer.Count(PacketID.C_WeaponEnhance));
            yield return _record.Capture(_fixture, "closed-gameplay-restored");
            _record.Finish();
        }
    }
}
