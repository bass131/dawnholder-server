using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Network;
using Dawnholder.Client.Prediction;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests.PlayMode
{
    // HUD 재구성 선행 요구시험(PlayMode). 요구 원천은 .backups/verification/2026-10-13-demo-video-art/
    // hud-reconstruction-pr/pretest-01/contract.md 「요구와 독립 기대값」이다. 실제 UI 씬과 scripted TCP peer(서버 권위
    // 대역, 실서버·DB 아님)를 쓴다. 판정은 플레이어가 UI 씬에서 실제로 읽을 수 있는 것, 즉 켜져서 그려지는 TMP 글자와
    // Image sprite만 본다. 새 비공개 필드명은 강제하지 않고, gold·HP 바만 기존 serialized 필드(_goldText, _hpSlider)로 읽는다.
    // 기대값은 peer로 보낸 서버 리터럴과 고정 표(직업 이름, 직업→Q/E, mapId→지명)뿐이다. 한 시험의 요구 미충족은
    // 모두 모아 끝에 보고하고, fixture 전제 실패는 "fixture:"로 따로 멈춘다.
    public sealed class HudPresentationLifecycleTests
    {
        // UI.unity의 SkillHudController._iconSet이 가리키는 실제 아이콘 데이터 자산.
        const string SkillIconSetPath = "Assets/Prefabs/Characters/ClassVisuals/SkillIconSet.asset";
        // 기존 직업 초상 원본(Art/UI/Character_Icon). 다른 직업이나 끝난 연결의 초상은 현재 직업으로 읽힌다.
        const string KnightPortrait = "knight_icon";
        const string MagePortrait = "character_icon2";
        static readonly string[] KnightNames = { "Knight", "기사" };
        static readonly string[] MageNames = { "Mage", "마법사" };
        static readonly Regex NumberPair = new(@"(?<!\d)\d+\s*/\s*\d+(?!\d)");

        MapEntryPlayFixture _fixture;
        readonly List<string> _failures = new();

        [SetUp]
        public void Setup()
        {
            _failures.Clear();
            _fixture = new MapEntryPlayFixture();
        }

        [UnityTearDown] public IEnumerator Teardown() => _fixture.Cleanup();

        // [수신 전 HP]·[확정 직업/HP]·[실제 Q/E]·[장소/보존] 첫 Town entry: HUD Start 뒤 서버 HP 전과 후.
        [UnityTest]
        public IEnumerator FirstKnightEntry_ShowsUnconfirmedHpUntilServerHp_ThenItWithKnightClassAndQOnly()
        {
            yield return _fixture.EnterScriptedTown();
            yield return WaitHud("first Town entry");
            yield return _fixture.Frames(2);
            HudView beforeHp = Observe("Knight Town entry, HUD started, before S_PlayerHp");
            _fixture.Hp(37, 100);
            yield return _fixture.WaitMap(0);
            yield return _fixture.Frames(2);
            HudView ready = Observe("Knight Town Ready after S_PlayerHp 37/100");

            ExpectUnconfirmedHp(beforeHp, "[수신 전 HP] HUD Start 뒤 HP 수신 전");
            Expect(ready.ShowsHp(37, 100), "[확정 직업/HP] 받은 HP 37/100이 그려진다", ready);
            ExpectClass(ready, CharacterClass.Knight, "[확정 직업/HP] 확정 Knight");
            ExpectKnightSkills(ready, "[실제 Q/E] 확정 Knight");
            Expect(ready.Texts.Contains("Town"), "[장소/보존] mapId 0의 지명 Town이 그려진다", ready);
            AssertRequirements();
        }

        // [같은 연결 map 이동] 직업과 gold는 유지하고, HP는 새 entry 수신 전까지 미확정이며 옛 entry HP가 복구되지 않는다.
        [UnityTest]
        public IEnumerator SameConnectionMapMove_KeepsClassAndGold_AndHpWaitsForTheNewEntry()
        {
            yield return ReadyTownWithGold(1234);
            _fixture.Hp(41, 100); // 옛 entry의 마지막 HP
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.CurrentHp == 41, "old entry HP 41/100", 3);
            yield return _fixture.Frames(2);
            HudView town = Observe("Town Ready, HP 41/100, gold 1234");

            yield return MoveToHuntingGroundBeforeHp();
            HudView moved = Observe("HuntingGround entry on the same connection, before its S_PlayerHp");
            _fixture.Hp(52, 120);
            yield return _fixture.WaitMap(1);
            yield return _fixture.Frames(2);
            HudView ready = Observe("HuntingGround Ready after S_PlayerHp 52/120");

            Expect(town.ShownGold == "1234", "[gold] 확정 gold 1234가 그려진다", town);
            ExpectUnconfirmedHp(moved, "[같은 연결 map 이동] 새 entry HP 수신 전");
            Expect(!moved.ShowsHp(41, 100), "[같은 연결 map 이동] 옛 entry HP 41/100이 복구되지 않는다", moved);
            ExpectClass(moved, CharacterClass.Knight, "[같은 연결 map 이동] 새 HP 전에도 연결의 확정 직업 유지");
            Expect(moved.ShownGold == "1234", "[gold] 같은 연결 map 이동 뒤 확정 gold 1234 유지", moved);
            Expect(ready.ShowsHp(52, 120), "[같은 연결 map 이동] 새 entry HP 52/120만 표시를 바꾼다", ready);
            ExpectClass(ready, CharacterClass.Knight, "[같은 연결 map 이동] 새 entry Ready 뒤 확정 직업");
            ExpectKnightSkills(ready, "[실제 Q/E] 같은 연결 map 이동 뒤 Knight");
            Expect(ready.Texts.Contains("Hunting Ground"), "[장소/보존] mapId 1의 지명 Hunting Ground가 그려진다", ready);
            Expect(ready.ShownGold == "1234", "[gold] 새 entry Ready 뒤에도 gold 1234 유지", ready);
            AssertRequirements();
        }

        // [연결 종료/교체] 서버 쪽 transport close 뒤 main cleanup 전, 그리고 cleanup 뒤에도 끝난 연결의 직업·HP·아이콘을
        // 현재 확정값으로 보이지 않는다. 그 창은 실제로는 한 프레임 안팎이라, 시험 동안만 main dispatcher를 멈춰 고정한다.
        [UnityTest]
        public IEnumerator TransportClose_BeforeAndAfterMainCleanup_ShowsNoClosedSessionClassHpOrIcons()
        {
            yield return ReadyTownWithGold(1234);
            yield return _fixture.Frames(2);
            Observe("open Knight connection Ready, HP 37/100, gold 1234");
            UnityClientSession closing = _fixture.Session;

            var paused = new List<MainThreadDispatcher>();
            bool cleanupPending;
            HudView window;
            try
            {
                foreach (MainThreadDispatcher dispatcher in UnityEngine.Object.FindObjectsByType<MainThreadDispatcher>())
                {
                    if (!dispatcher.enabled) continue;
                    dispatcher.enabled = false;
                    paused.Add(dispatcher);
                }
                Assert.IsNotEmpty(paused, "fixture: the persistent main-thread dispatcher is found");
                _fixture.Peer.Dispose(); // 서버 쪽 close: 수신 스레드가 session을 닫고 cleanup을 main queue에 올린다.
                yield return MapEntryPlayFixture.Wait(() => closing.IsClosed, "transport close seen by the session", 5);
                yield return _fixture.Frames(3);
                cleanupPending = ReferenceEquals(UnityClientSession.Instance, closing) &&
                    ClassLoadout.ServerClass == CharacterClass.Knight;
                window = Observe("transport closed, main cleanup still queued");
            }
            finally
            {
                foreach (MainThreadDispatcher dispatcher in paused)
                {
                    if (dispatcher != null) dispatcher.enabled = true;
                }
            }
            yield return WaitOwnerReset();
            yield return _fixture.Frames(2);
            HudView cleaned = Observe("after main cleanup and the owner's mirror reset");

            Assert.IsTrue(cleanupPending, "fixture: the transport closed while the main cleanup was still queued");
            ExpectEndedConnection(window, "[연결 종료/교체] close 뒤 main cleanup 전", 37, 100);
            ExpectEndedConnection(cleaned, "[연결 종료/교체] main cleanup 뒤", 37, 100);
            Expect(cleaned.ShownGold == "", "[gold] 연결 reset 뒤 옛 gold 1234가 남지 않는다", cleaned);
            AssertRequirements();
        }

        // [연결 종료/교체]·[확정 직업/HP]·[실제 Q/E] 같은 Town·UI를 둔 채 연결만 Knight → Mage → Knight로 바꾼다.
        // 각 연결은 미확정에서 시작해 자기 확정 직업·HP·Q/E만 보이고, Mage 뒤 Knight에 옛 Mage E가 남지 않는다.
        [UnityTest]
        public IEnumerator NewConnections_MageThenKnight_ShowOnlyTheCurrentConnectionsClassHpAndQe()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp(37, 100);
            yield return _fixture.WaitMap(0);
            yield return WaitHud("first Knight connection Ready");
            yield return _fixture.Frames(2);
            Observe("first Knight connection Ready, HP 37/100");

            NetworkService.Instance.Disconnect();
            yield return WaitOwnerReset();
            yield return _fixture.Frames(2);
            HudView between = Observe("first connection ended, no new connection yet");

            yield return ConnectNew(CharacterClass.Mage);
            yield return EnterTownOnCurrentConnection();
            yield return _fixture.Frames(2);
            HudView mageBeforeHp = Observe("Mage connection Town entry before S_PlayerHp");
            _fixture.Hp(61, 88);
            yield return _fixture.WaitMap(0);
            yield return _fixture.Frames(2);
            HudView mage = Observe("Mage connection Ready, HP 61/88");

            NetworkService.Instance.Disconnect();
            yield return WaitOwnerReset();
            yield return ConnectNew(CharacterClass.Knight);
            yield return EnterTownOnCurrentConnection();
            yield return _fixture.Frames(2);
            HudView knightBeforeHp = Observe("second Knight connection Town entry before S_PlayerHp");
            _fixture.Hp(45, 150);
            yield return _fixture.WaitMap(0);
            yield return _fixture.Frames(2);
            HudView knight = Observe("second Knight connection Ready, HP 45/150");

            Sprite teleport = Icon(SkillId.Teleport);
            ExpectEndedConnection(between, "[연결 종료/교체] 연결 종료 뒤 새 연결 전", 37, 100);
            ExpectUnconfirmedHp(mageBeforeHp, "[연결 종료/교체] 새 Mage 연결의 HP 수신 전");
            Expect(!mageBeforeHp.ShowsHp(37, 100), "[연결 종료/교체] 새 연결에 옛 연결 HP 37/100이 없다", mageBeforeHp);
            Expect(!mageBeforeHp.ShowsAny(KnightNames), "[연결 종료/교체] 새 Mage 연결에 옛 Knight 이름이 없다", mageBeforeHp);
            Expect(mage.ShowsHp(61, 88), "[확정 직업/HP] Mage 연결의 받은 HP 61/88이 그려진다", mage);
            ExpectClass(mage, CharacterClass.Mage, "[확정 직업/HP] 확정 Mage");
            ExpectMageSkills(mage, "[실제 Q/E] 확정 Mage");
            ExpectUnconfirmedHp(knightBeforeHp, "[연결 종료/교체] 새 Knight 연결의 HP 수신 전");
            Expect(!knightBeforeHp.ShowsHp(61, 88), "[연결 종료/교체] 새 연결에 옛 Mage HP 61/88이 없다", knightBeforeHp);
            Expect(!knightBeforeHp.Shows(teleport) && !knightBeforeHp.ShowsKeyLabel("E"),
                "[실제 Q/E] 새 Knight 연결에 옛 Mage E 아이콘·키가 남지 않는다", knightBeforeHp);
            Expect(!knightBeforeHp.ShowsAny(MageNames), "[연결 종료/교체] 새 Knight 연결에 옛 Mage 이름이 없다", knightBeforeHp);
            Expect(knight.ShowsHp(45, 150), "[확정 직업/HP] 새 Knight 연결의 받은 HP 45/150이 그려진다", knight);
            ExpectClass(knight, CharacterClass.Knight, "[확정 직업/HP] Mage 뒤 새 Knight 연결");
            ExpectKnightSkills(knight, "[실제 Q/E] Mage 뒤 새 Knight 연결");
            AssertRequirements();
        }

        // [UI 생명주기] 유효 연결에서 꺼졌다 켜지거나 재생성된 HUD는 그 사이 바뀐 현재값을 다시 보이고, 끈/파괴한 HUD는 실제
        // 구독한 원천에서 해제된다. 연결이 끝난 뒤(무효 원천) 재생성한 HUD는 옛 값을 복원하지 않는다.
        [UnityTest]
        public IEnumerator ReenabledOrRecreatedHud_ShowsCurrentValues_AndAfterTheConnectionEnds_RestoresNone()
        {
            yield return ReadyTownWithGold(1234);
            InventoryState inventory = InventoryState.Instance;
            GameObject hudRoot = HudController.Instance.gameObject;

            hudRoot.SetActive(false);
            yield return _fixture.Frames(1);
            int inventoryWhileDisabled = SubscribersOf<HudController>(InventoryChanged(inventory));
            int authorityWhileDisabled = SubscribersOf<SkillHudController>(AuthorityChanged());
            _fixture.Hp(52, 100);
            _fixture.Peer.Send(Snapshot(2, 777));
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.CurrentHp == 52 && inventory.Currency == 777,
                "HP 52/100 and gold 777 arrive while the HUD is disabled", 3);
            hudRoot.SetActive(true);
            yield return _fixture.Frames(2);
            HudView reenabled = Observe("HUD re-enabled after HP 52/100 and gold 777 arrived while it was disabled");
            int inventoryAfterEnable = SubscribersOf<HudController>(InventoryChanged(inventory));
            int authorityAfterEnable = SubscribersOf<SkillHudController>(AuthorityChanged());

            yield return UnloadUi();
            int inventoryAfterDestroy = SubscribersOf<HudController>(InventoryChanged(inventory));
            int authorityAfterDestroy = SubscribersOf<SkillHudController>(AuthorityChanged());
            yield return LoadUi();
            HudView recreated = Observe("UI scene recreated on the valid connection");

            NetworkService.Instance.Disconnect();
            yield return WaitOwnerReset();
            yield return UnloadUi();
            yield return LoadUi();
            HudView invalid = Observe("UI scene recreated after the connection ended");

            Expect(reenabled.ShowsHp(52, 100), "[UI 생명주기] 다시 켠 HUD가 꺼진 동안 바뀐 현재 HP 52/100을 보인다", reenabled);
            Expect(reenabled.ShownGold == "777", "[UI 생명주기] 다시 켠 HUD가 꺼진 동안 바뀐 현재 gold 777을 보인다", reenabled);
            ExpectClass(reenabled, CharacterClass.Knight, "[UI 생명주기] 다시 켠 HUD");
            ExpectKnightSkills(reenabled, "[UI 생명주기] 다시 켠 HUD");
            Expect(inventoryWhileDisabled == 0 && authorityWhileDisabled == 0,
                $"[UI 생명주기] 꺼진 HUD는 실제 구독한 원천에서 해제된다 (inventory {inventoryWhileDisabled}, authority {authorityWhileDisabled})",
                reenabled);
            Expect(inventoryAfterEnable <= 1 && authorityAfterEnable <= 1,
                $"[UI 생명주기] 다시 켜도 같은 원천에 중복 구독하지 않는다 (inventory {inventoryAfterEnable}, authority {authorityAfterEnable})",
                reenabled);
            Expect(inventoryAfterDestroy == 0 && authorityAfterDestroy == 0,
                $"[UI 생명주기] 파괴된 HUD의 구독이 원천에 남지 않는다 (inventory {inventoryAfterDestroy}, authority {authorityAfterDestroy})",
                recreated);
            Expect(recreated.ShowsHp(52, 100), "[UI 생명주기] 유효 연결에서 재생성한 HUD가 현재 HP 52/100을 다시 보인다", recreated);
            Expect(recreated.ShownGold == "777", "[UI 생명주기] 유효 연결에서 재생성한 HUD가 현재 gold 777을 다시 보인다", recreated);
            ExpectClass(recreated, CharacterClass.Knight, "[UI 생명주기] 유효 연결에서 재생성한 HUD");
            ExpectKnightSkills(recreated, "[UI 생명주기] 유효 연결에서 재생성한 HUD");
            Expect(recreated.Texts.Contains("Town"), "[장소/보존] UI 재생성 뒤 지명 Town이 복원된다", recreated);
            ExpectEndedConnection(invalid, "[UI 생명주기] 연결이 끝난 뒤 재생성한 HUD", 52, 100);
            Expect(invalid.ShownGold == "", "[UI 생명주기] 연결이 끝난 뒤 재생성한 HUD가 옛 gold 777을 복원하지 않는다", invalid);
            AssertRequirements();
        }

        // [gold] 스냅샷 전·유효 0·상한을 구분해 그대로 보이고, 같은 연결 map 이동에는 유지하며, 연결 교체 뒤에는 옛 금액이 없다.
        [UnityTest]
        public IEnumerator Gold_SeparatesNoSnapshotZeroAndCap_KeepsItAcrossAMapMove_AndDropsItForANewConnection()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp(37, 100);
            yield return _fixture.WaitMap(0);
            yield return WaitInventoryQueries(1);
            yield return WaitHud("Town Ready");
            yield return _fixture.Frames(2);
            HudView noSnapshot = Observe("Town Ready, no inventory snapshot yet");
            _fixture.Peer.Send(Snapshot(1, 0));
            yield return WaitCurrency(0);
            HudView zero = Observe("snapshot currency 0");
            _fixture.Peer.Send(Snapshot(2, 1_000_000_000));
            yield return WaitCurrency(1_000_000_000);
            HudView cap = Observe("snapshot currency 1000000000");

            yield return MoveToHuntingGroundBeforeHp();
            _fixture.Hp(31, 100);
            yield return _fixture.WaitMap(1);
            yield return _fixture.Frames(2);
            HudView moved = Observe("HuntingGround Ready on the same connection");

            NetworkService.Instance.Disconnect();
            yield return WaitOwnerReset();
            yield return ConnectNew(CharacterClass.Knight);
            yield return EnterTownOnCurrentConnection();
            _fixture.Hp(37, 100);
            yield return _fixture.WaitMap(0);
            yield return _fixture.Frames(2);
            HudView reconnected = Observe("new Knight connection Ready, no inventory snapshot yet");

            Expect(noSnapshot.GoldDrawn && noSnapshot.ShownGold == "",
                "[gold] 스냅샷 전 gold 라벨은 그려지되 숫자가 없다 (유효 0과 구분)", noSnapshot);
            Expect(zero.ShownGold == "0", "[gold] 유효한 0은 0으로 보인다", zero);
            Expect(cap.ShownGold == "1000000000", "[gold] 상한 1,000,000,000의 모든 자릿수가 축약 없이 보인다", cap);
            Expect(moved.ShownGold == "1000000000", "[gold] 같은 연결 map 이동 뒤 확정 gold 유지", moved);
            Expect(reconnected.ShownGold == "", "[gold] 새 연결의 스냅샷 전 옛 gold가 남지 않는다", reconnected);
            AssertRequirements();
        }

        // ── 흐름 ──────────────────────────────────────────────────────────

        IEnumerator ReadyTownWithGold(int currency)
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp(37, 100);
            yield return _fixture.WaitMap(0);
            yield return WaitInventoryQueries(1);
            _fixture.Peer.Send(Snapshot(1, currency));
            yield return WaitCurrency(currency);
            yield return WaitHud("Town Ready");
        }

        // 같은 연결의 S_MapTransition. HuntingGround 장면과 그 HUD까지 기다리고 새 HP는 보내지 않는다.
        IEnumerator MoveToHuntingGroundBeforeHp()
        {
            _fixture.Peer.Send(new S_MapTransition { destMapId = 1, spawnX = 2, spawnY = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.MapId == 1 && _fixture.Session.Entry.SceneReady,
                "HuntingGround scene before its HP", 10);
            yield return WaitHud("HuntingGround entry");
            yield return _fixture.Frames(2);
        }

        // 연결 소유자의 정리 완료: 공개 session 없음, 직업 권위 reset, 그 연결의 플레이어 파괴.
        static IEnumerator WaitOwnerReset()
        {
            yield return MapEntryPlayFixture.Wait(() => UnityClientSession.Instance == null && ClassLoadout.ServerClass == null &&
                LocalPlayerMovement.Instance == null, "connection owner cleanup", 5);
        }

        // 이전 peer를 정리하고 새 scripted peer로 같은 NetworkService의 새 연결을 연다. 끝난 뒤 fixture.Cleanup이 새 peer를 닫는다.
        IEnumerator ConnectNew(CharacterClass characterClass)
        {
            EntryWirePeer previous = _fixture.Peer;
            previous.Dispose();
            yield return MapEntryPlayFixture.Wait(() => previous.Completion.IsCompleted, "previous scripted peer shutdown", 3);
            Assert.IsEmpty(previous.Errors, "fixture: previous scripted transport failures");
            _fixture.Peer = EntryWirePeer.Listen();
            MapEntryPlayFixture.Set(NetworkService.Instance, "_serverPort", _fixture.Peer.Port);
            NetworkService.Instance.Connect("127.0.0.1", (int)characterClass);
            yield return MapEntryPlayFixture.Wait(() => UnityClientSession.Instance != null && _fixture.Peer.Selected &&
                ClassLoadout.ServerClass == characterClass, "new connection loads " + characterClass, 10);
        }

        IEnumerator EnterTownOnCurrentConnection()
        {
            _fixture.Peer.Send(new S_EnterMap { entityId = 700, spawnX = 2, spawnY = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session != null && _fixture.Session.Entry.MapId == 0 &&
                _fixture.Session.Entry.State == MapEntryState.Entering && _fixture.Session.Entry.SceneReady && _fixture.Player != null,
                "new connection's Town binding before HP", 10);
            yield return WaitHud("new connection's Town entry");
            _fixture.PairInput();
        }

        IEnumerator UnloadUi()
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync("UI");
            Assert.IsNotNull(unload, "fixture: the UI scene can be unloaded");
            yield return MapEntryPlayFixture.Wait(() => unload.isDone && HudController.Instance == null, "UI scene unloaded", 10);
            yield return _fixture.Frames(1);
        }

        IEnumerator LoadUi()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("UI", LoadSceneMode.Additive);
            yield return MapEntryPlayFixture.Wait(() => load.isDone, "UI scene loaded", 10);
            yield return WaitHud("recreated UI scene");
            yield return _fixture.Frames(2);
        }

        static IEnumerator WaitHud(string stage) =>
            MapEntryPlayFixture.Wait(() => HudController.Instance != null && HudController.Instance.isActiveAndEnabled &&
                SceneManager.GetSceneByName("UI").isLoaded, "additive HUD at " + stage, 10);

        IEnumerator WaitInventoryQueries(int count) =>
            MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_InventoryRequest) == count, "C_InventoryRequest #" + count, 3);

        IEnumerator WaitCurrency(int currency)
        {
            yield return MapEntryPlayFixture.Wait(() => InventoryState.Instance != null && InventoryState.Instance.HasSnapshot &&
                InventoryState.Instance.Currency == currency, "inventory mirror currency " + currency, 3);
            yield return _fixture.Frames(2);
        }

        static ArraySegment<byte> Snapshot(uint revision, int currency) =>
            new S_InventorySnapshot { revision = revision, currency = currency }.Write();

        // ── 관측 ──────────────────────────────────────────────────────────

        // 한 시점에 플레이어가 UI 씬에서 읽을 수 있는 것. 원시 관측은 로그에도 남긴다.
        static HudView Observe(string stage)
        {
            Scene ui = SceneManager.GetSceneByName("UI");
            Assert.IsTrue(ui.IsValid() && ui.isLoaded, "fixture: the additive UI scene is loaded at " + stage);
            HudController hud = HudController.Instance;
            Assert.IsNotNull(hud, "fixture: the UI scene's HUD exists at " + stage);
            var view = new HudView(stage);
            foreach (GameObject root in ui.GetRootGameObjects())
            {
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>())
                {
                    if (Drawn(text) && !string.IsNullOrWhiteSpace(text.text)) view.Texts.Add(text.text.Trim());
                }
                foreach (Image image in root.GetComponentsInChildren<Image>())
                {
                    if (Drawn(image) && image.sprite != null && !(image.type == Image.Type.Filled && image.fillAmount <= 0f))
                        view.Sprites.Add(image.sprite);
                }
            }
            TMP_Text gold = HudField<TMP_Text>(hud, "_goldText", required: true);
            view.GoldDrawn = gold != null && Drawn(gold);
            view.GoldText = gold != null ? gold.text : null;
            Slider bar = HudField<Slider>(hud, "_hpSlider", required: false);
            view.HpBar = bar != null && bar.isActiveAndEnabled ? bar.value : (float?)null;
            Debug.Log("[HUD presentation pretest] " + view);
            return view;
        }

        // 켜진 그래픽이고, 투명하지 않으며, CanvasGroup으로 가려지지 않았다.
        static bool Drawn(Graphic graphic)
        {
            if (graphic == null || !graphic.isActiveAndEnabled || graphic.color.a <= 0f) return false;
            if (graphic.canvas == null || !graphic.canvas.isActiveAndEnabled) return false;
            for (Transform node = graphic.transform; node != null; node = node.parent)
            {
                if (node.TryGetComponent(out CanvasGroup group) && group.enabled && group.alpha <= 0f) return false;
            }
            return true;
        }

        static T HudField<T>(HudController hud, string name, bool required) where T : UnityEngine.Object
        {
            FieldInfo field = typeof(HudController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (required) Assert.IsNotNull(field, $"fixture: HudController keeps its serialized {name}");
            return field != null ? field.GetValue(hud) as T : null;
        }

        // 아이콘 sprite는 실제 데이터 자산에서 읽는다. 어느 SkillId를 기대할지는 시험의 고정 표가 정한다.
        static Sprite Icon(SkillId skill)
        {
#if UNITY_EDITOR
            var set = UnityEditor.AssetDatabase.LoadAssetAtPath<SkillIconSet>(SkillIconSetPath);
#else
            SkillIconSet set = null;
#endif
            Assert.IsNotNull(set, "fixture: the skill icon data asset " + SkillIconSetPath);
            Sprite icon = set.GetIcon(skill);
            Assert.IsNotNull(icon, $"fixture: the skill icon data has {skill}");
            return icon;
        }

        // field-like event의 delegate 저장소에서 T가 등록한 항목 수. 파괴된 T의 등록도 센다.
        static int SubscribersOf<T>(Delegate subscribers) =>
            subscribers?.GetInvocationList().Count(handler => handler.Target is T) ?? 0;

        static Delegate InventoryChanged(InventoryState inventory)
        {
            FieldInfo field = typeof(InventoryState).GetField(nameof(InventoryState.OnInventoryChanged),
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "fixture: InventoryState.OnInventoryChanged event storage");
            return field.GetValue(inventory) as Delegate;
        }

        static Delegate AuthorityChanged()
        {
            FieldInfo field = typeof(ClassLoadout).GetField(nameof(ClassLoadout.AuthorityChanged),
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "fixture: ClassLoadout.AuthorityChanged event storage");
            return field.GetValue(null) as Delegate;
        }

        // ── 요구 판정 ─────────────────────────────────────────────────────

        void Expect(bool holds, string requirement, HudView view)
        {
            if (!holds) _failures.Add(requirement + "\n    관측 " + view);
        }

        void AssertRequirements()
        {
            if (_failures.Count != 0)
                Assert.Fail($"요구 미충족 {_failures.Count}건\n" + string.Join("\n", _failures));
        }

        // HP가 아직 없는 유효 연결: HP 라벨이 그려져 읽히되 숫자·숫자 쌍·가득 찬 바로 직업 기본치를 확정값처럼 보이지 않는다.
        void ExpectUnconfirmedHp(HudView view, string requirement)
        {
            Expect(view.HpTexts.Any(), requirement + ": HP 라벨이 그려져 미확정 상태가 읽힌다", view);
            Expect(!view.HpTexts.Any(HasDigit), requirement + ": HP 라벨에 숫자가 없다", view);
            Expect(!view.ShowsNumberPair, requirement + ": 그려진 HP 라벨이 체력 숫자 쌍을 보이지 않는다", view);
            Expect(!view.ShowsFullHpBar, requirement + ": HP 바가 가득 찬 확정 체력처럼 보이지 않는다", view);
        }

        // 끝난 연결: 그 연결의 HP·직업·기술 아이콘·키가 현재값으로 남지 않는다. HUD를 숨기는 표현도 허용한다.
        void ExpectEndedConnection(HudView view, string requirement, int staleCurrent, int staleMax)
        {
            Expect(!view.ShowsHp(staleCurrent, staleMax), $"{requirement}: 끝난 연결의 HP {staleCurrent}/{staleMax}가 없다", view);
            Expect(!view.HpTexts.Any(HasDigit) && !view.ShowsNumberPair, requirement + ": HP 숫자가 없다", view);
            Expect(!view.ShowsFullHpBar, requirement + ": HP 바가 가득 찬 확정 체력처럼 보이지 않는다", view);
            Expect(!view.ShowsAny(KnightNames) && !view.ShowsAny(MageNames), requirement + ": 직업 이름이 없다", view);
            Expect(!view.ShowsPortrait(KnightPortrait) && !view.ShowsPortrait(MagePortrait), requirement + ": 직업 초상이 없다", view);
            Expect(!view.Shows(Icon(SkillId.Dash)) && !view.Shows(Icon(SkillId.Thunderbolt)) && !view.Shows(Icon(SkillId.Teleport)),
                requirement + ": 기술 아이콘이 없다", view);
            Expect(!view.ShowsKeyLabel("E"), requirement + ": E 키 글자가 없다", view);
        }

        void ExpectClass(HudView view, CharacterClass confirmed, string requirement)
        {
            bool knight = confirmed == CharacterClass.Knight;
            string[] names = knight ? KnightNames : MageNames;
            string[] others = knight ? MageNames : KnightNames;
            Expect(view.ShowsAny(names), $"{requirement}: 직업 이름({string.Join("/", names)})이 그려진다", view);
            Expect(!view.ShowsAny(others), $"{requirement}: 다른 직업 이름({string.Join("/", others)})이 없다", view);
            Expect(!view.ShowsPortrait(knight ? MagePortrait : KnightPortrait), $"{requirement}: 다른 직업의 초상이 없다", view);
        }

        // Knight: Q Dash, E 없음(키·아이콘 전체 미사용).
        void ExpectKnightSkills(HudView view, string requirement)
        {
            Expect(view.Shows(Icon(SkillId.Dash)), requirement + ": Q는 Dash 아이콘이다", view);
            Expect(view.ShowsKeyLabel("Q"), requirement + ": Q 키 글자가 그려진다", view);
            Expect(!view.ShowsKeyLabel("E"), requirement + ": 쓰지 않는 E의 키 글자가 없다", view);
            Expect(!view.Shows(Icon(SkillId.Thunderbolt)) && !view.Shows(Icon(SkillId.Teleport)), requirement + ": Mage 아이콘이 없다", view);
        }

        // Mage: Q Thunderbolt, E Teleport.
        void ExpectMageSkills(HudView view, string requirement)
        {
            Expect(view.Shows(Icon(SkillId.Thunderbolt)), requirement + ": Q는 Thunderbolt 아이콘이다", view);
            Expect(view.Shows(Icon(SkillId.Teleport)), requirement + ": E는 Teleport 아이콘이다", view);
            Expect(view.ShowsKeyLabel("Q") && view.ShowsKeyLabel("E"), requirement + ": Q·E 키 글자가 그려진다", view);
            Expect(!view.Shows(Icon(SkillId.Dash)), requirement + ": Knight Dash 아이콘이 없다", view);
        }

        static bool HasDigit(string text) => text.Any(char.IsDigit);

        sealed class HudView
        {
            internal HudView(string stage) { Stage = stage; }

            internal string Stage { get; }
            internal List<string> Texts { get; } = new();
            internal List<Sprite> Sprites { get; } = new();
            internal bool GoldDrawn { get; set; }
            internal string GoldText { get; set; }
            internal float? HpBar { get; set; }

            internal IEnumerable<string> HpTexts => Texts.Where(text => text.Contains("HP"));
            // Quest progress also renders n / m. Only the HP label is evidence about HP authority.
            internal bool ShowsNumberPair => HpTexts.Any(text => NumberPair.IsMatch(text));
            internal bool ShowsFullHpBar => HpBar.HasValue && HpBar.Value >= 0.999f;
            // 플레이어가 읽는 gold: 그려진 라벨의 숫자, 그려지지 않으면 없음.
            internal string ShownGold => GoldDrawn ? new string((GoldText ?? string.Empty).Where(char.IsDigit).ToArray()) : string.Empty;

            // 받은 current와 max가 그 순서로, 다른 숫자에 붙지 않고 읽힌다. 서식은 구현에 맡긴다.
            internal bool ShowsHp(int current, int max) =>
                HpTexts.Any(text => Regex.IsMatch(text, $@"(?<!\d){current}(?!\d)\D+(?<!\d){max}(?!\d)"));

            internal bool ShowsAny(string[] names) => Texts.Any(text => names.Any(name => text.Contains(name)));
            internal bool Shows(Sprite sprite) => Sprites.Contains(sprite);
            internal bool ShowsPortrait(string textureName) =>
                Sprites.Any(sprite => sprite.texture != null && sprite.texture.name == textureName);

            // 키 안내 글자: 글자에서 영문자만 남겼을 때 그 키 하나다("Q", "[Q]" 등).
            internal bool ShowsKeyLabel(string key) =>
                Texts.Any(text => new string(text.Where(c => c < 128 && char.IsLetter(c)).ToArray()).ToUpperInvariant() == key);

            public override string ToString()
            {
                var line = new StringBuilder(Stage);
                line.Append(" | texts [").Append(string.Join(" ¦ ", Texts.Select(text => text.Replace("\n", "\\n"))));
                line.Append("] sprites [").Append(string.Join(", ", Sprites.Select(SpriteName)));
                line.Append("] gold ").Append(GoldDrawn ? "'" + GoldText + "'" : "not drawn");
                line.Append(" hpBar ").Append(HpBar.HasValue ? HpBar.Value.ToString("F3") : "absent/hidden");
                return line.ToString();
            }

            static string SpriteName(Sprite sprite) => sprite.texture != null ? sprite.texture.name + "/" + sprite.name : sprite.name;
        }
    }
}
