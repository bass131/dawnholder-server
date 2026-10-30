using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dawnholder.Client.Combat;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Kill reward floating text (01_Phases/goals/2026-10-30-kill-reward-float-text/goal.md 「만들 것」 1~7, 완료조건 1·2) in the
    // real Town scene through the scripted TCP peer. A black box (goal 「시험 층」): the text is found only by
    // KillRewardFloatTextProbe's reading of the screen, never by a product type name. The numbers in comments and
    // messages are the pretest contract's requirement 3 items; thresholds and their sources are in
    // .backups/verification/2026-10-30-kill-reward-float-text/pretest-01/observation-contract.md 「3. 판정 임계와 출처」.
    public sealed class KillRewardFloatTextSceneTests
    {
        const int MaxHp = 100;
        const float AboveSpotMaxXOffset = 1.0f;
        const float AppearSeconds = 3f;
        const float QuietSeconds = 0.6f;
        const float RiseFromSeconds = 0.2f;
        const float RiseToSeconds = 1.0f;
        const float MinRise = 0.1f;
        const float StillShownSeconds = 1.4f;
        const float MinFade = 0.1f;
        const float GoneSeconds = 5f;
        const float GoneWatchMarginSeconds = 0.5f;
        const float SampleSeconds = 0.02f;
        const int SamplesPerLogLine = 60;
        const float LateSnapshotSeconds = 1.3f;
        const float WindowExpirySeconds = 1.2f;
        const float PairingWindowSeconds = 1.0f;
        const int BaseGold = 25;
        const int BaseMaterial = 2;
        const int KillGold = 10;
        const int KillMaterial = 1;
        const int ClearGold = 50;
        const int ClearMaterial = 1;
        const int ClearCoinPouch = 1;
        const float EnemyX = 8f;
        const float SecondEnemyX = 6f;
        const float NearX = 5f;
        const float FarX = 12f;
        const string FontName = "LiberationSans SDF";

        // goal 「만들 것」 5 lines; the item names are ItemCatalog.cs :8~:9 written out, never read from the catalog.
        const string GoldTen = "+금화 10";
        const string MaterialOne = "+재료 1";
        const string CoinPouchOne = "+재화 주머니 1";

        MapEntryPlayFixture _fixture;
        int _hpMarker;
        uint _revision;
        int _gold;
        int _material;
        int _coinPouch;
        int _enemyId;
        int _serverTick;
        int _lastSpawnedId;
        int _lastKilledId;
        TextMeshPro[] _shown = Array.Empty<TextMeshPro>();

        [SetUp]
        public void Setup()
        {
            _fixture = new MapEntryPlayFixture();
            _hpMarker = 40;
            _enemyId = 800;
        }

        [UnityTearDown] public IEnumerator Teardown() => _fixture.Cleanup();

        // 3.1 and addendum 01: a non-zero baseline (gold 25, material 2), then two kills. Floating the running total
        // fails the first check (「+금화 35」); differencing against the first baseline fails the second (「+금화 20」).
        [UnityTest]
        public IEnumerator KillThenGoldAndMaterialSnapshot_ShowsTheIncreaseAboveTheDeathSpot_AndASecondKillShowsOnlyItsOwnIncrease()
        {
            yield return EnterTownWithBaseline();
            yield return SpawnSettled(EnemyX);
            Vector3 first = Kill(_lastSpawnedId);
            Grant(KillGold, KillMaterial, 0);
            yield return WaitForText();
            AssertShownAbove("3.1 첫 처치(금화 25→35, 재료 2→3)", first, GoldTen, MaterialOne);

            yield return WaitUntilGone("3.1 첫 처치 글");
            yield return LetTheLastDeathLeaveThePairingWindow();
            yield return SpawnSettled(SecondEnemyX);
            Vector3 second = Kill(_lastSpawnedId);
            Grant(KillGold, 0, 0);
            yield return WaitForText();
            AssertShownAbove("3.1 둘째 처치(금화 35→45, 재료 그대로)", second, GoldTen);
        }

        // 3.2 goal 「만들 것」 6: rises slowly, stays past 1.4 s (goal 「1.5초 이상」), fades near its end, gone within 5 s.
        [UnityTest]
        public IEnumerator KillRewardText_RisesSlowly_StaysPastOnePointFourSeconds_FadesAndIsGoneWithinFiveSeconds()
        {
            yield return EnterTownWithBaseline();
            yield return SpawnSettled(EnemyX);
            Kill(_lastSpawnedId);
            Grant(KillGold, KillMaterial, 0);
            var samples = new List<Sample>();
            yield return SampleTheText(samples);
            if (samples.Count == 0) Debug.Log("[kill reward text] 3.2 samples: none");
            for (int i = 0; i < samples.Count; i += SamplesPerLogLine)
            {
                Debug.Log($"[kill reward text] 3.2 samples {i}..{Math.Min(i + SamplesPerLogLine, samples.Count) - 1} of {samples.Count}: " +
                    string.Join(" ", samples.Skip(i).Take(SamplesPerLogLine).Select(s => $"[{s.Dt:F3}s n={s.Count} y={s.Y:F3} a={s.Opacity:F3}]")));
            }

            Assert.IsNotEmpty(samples, $"3.2 처치 글이 없다 ({AppearSeconds}초 안에 보이는 처치 글이 생기지 않았다)");
            int riseFrom = samples.FindIndex(s => s.Count > 0 && s.Dt >= RiseFromSeconds);
            int riseTo = samples.FindLastIndex(s => s.Count > 0 && s.Dt <= RiseToSeconds);
            Assert.IsTrue(riseFrom >= 0 && riseTo > riseFrom,
                $"3.2 처치 글이 {RiseFromSeconds}~{RiseToSeconds}초 사이에 표본 둘 이상 떠 있다 (from {riseFrom}, to {riseTo})");
            Assert.GreaterOrEqual(samples[riseTo].Y - samples[riseFrom].Y, MinRise,
                $"3.2 {samples[riseFrom].Dt:F2}초에서 {samples[riseTo].Dt:F2}초 사이 y 상승");
            int still = samples.FindIndex(s => s.Dt >= StillShownSeconds);
            Assert.IsTrue(still >= 0 && samples[still].Count > 0 && samples[still].Opacity > 0f, $"3.2 처음 보인 뒤 {StillShownSeconds}초에도 보인다");
            float peak = samples.Where(s => s.Count > 0).Max(s => s.Opacity);
            int peakAt = samples.FindIndex(s => s.Count > 0 && s.Opacity == peak);
            Assert.IsTrue(samples.Skip(peakAt + 1).Any(s => s.Count > 0 && s.Opacity <= peak - MinFade),
                $"3.2 투명도 최댓값 {peak:F2}을 찍은 뒤 {MinFade} 이상 흐려진 frame이 있다");
            Sample last = samples[samples.Count - 1];
            Assert.IsTrue(samples.Any(s => s.Count == 0 && s.Dt <= GoneSeconds),
                $"3.2 처음 보인 뒤 {GoneSeconds}초 안에 처치 글이 0개가 된다 (마지막 표본 {last.Dt:F2}초 n={last.Count})");
        }

        // 3.3 goal 「만들 것」 5: an item line carries the catalog name; gold that did not grow has no line.
        [UnityTest]
        public IEnumerator KillThenCoinPouchSnapshot_ShowsTheCatalogName()
        {
            yield return EnterTownWithBaseline();
            yield return SpawnSettled(EnemyX);
            Vector3 spot = Kill(_lastSpawnedId);
            Grant(0, 0, 1);
            yield return WaitForText();
            AssertShownAbove("3.3 처치 뒤 재화 주머니 +1(금화 그대로)", spot, CoinPouchOne);
        }

        // 3.4 goal 「만들 것」 2: the first snapshot after connecting only sets the baseline, even right after a kill.
        [UnityTest]
        public IEnumerator FirstSnapshotAfterAKill_OnlySetsTheBaseline()
        {
            yield return EnterReadyTown();
            yield return SpawnSettled(EnemyX);
            Kill(_lastSpawnedId);
            SendInventory(BaseGold, BaseMaterial, 0);
            yield return AssertNoTextAfterEarlierPacketsApply("3.4 처치 뒤 첫 스냅숏(기준만)");
            yield return PositiveControl("3.4 첫 스냅숏 시험");
        }

        // 3.4 goal 「만들 것」 2: decreases are ignored.
        [UnityTest]
        public IEnumerator KillThenDecreasedSnapshot_ShowsNoText()
        {
            yield return EnterTownWithBaseline();
            yield return SpawnSettled(EnemyX);
            Kill(_lastSpawnedId);
            SendInventory(BaseGold - KillGold, BaseMaterial - KillMaterial, 0);
            yield return AssertNoTextAfterEarlierPacketsApply("3.4 처치 뒤 금화·재료가 줄어든 스냅숏");
            yield return PositiveControl("3.4 감소 스냅숏 시험");
        }

        // 3.4 goal 「만들 것」 4: without a death record there is no text.
        [UnityTest]
        public IEnumerator IncreaseWithoutAKill_ShowsNoText()
        {
            yield return EnterTownWithBaseline();
            Grant(KillGold, KillMaterial, 0);
            yield return AssertNoTextAfterEarlierPacketsApply("3.4 사망 없이 늘어난 스냅숏");
            yield return PositiveControl("3.4 사망 없는 증가 시험");
        }

        // 3.4 goal 「만들 것」 4: only deaths inside the 1.0 s window pair; 1.3 s is that window plus a 0.3 s margin.
        [UnityTest]
        public IEnumerator IncreaseArrivingOnePointThreeSecondsAfterTheKill_ShowsNoText()
        {
            yield return EnterTownWithBaseline();
            yield return SpawnSettled(EnemyX);
            int enemy = _lastSpawnedId;
            Kill(enemy);
            yield return WaitDeathApplied(enemy);
            // The requirement itself is a time, so this is a deliberate wait from the frame the death was applied.
            float late = Time.realtimeSinceStartup + LateSnapshotSeconds;
            yield return MapEntryPlayFixture.Wait(() => Time.realtimeSinceStartup >= late, "1.3 s after the death", LateSnapshotSeconds + 1);
            Grant(KillGold, KillMaterial, 0);
            yield return AssertNoTextAfterEarlierPacketsApply("3.4 사망 1.3초 뒤 도착한 증가 스냅숏");
            yield return PositiveControl("3.4 늦은 스냅숏 시험");
        }

        // 3.5 goal 「만들 것」 4: of two deaths in the window, the one nearest the local player (spawnX 2) wins. Both death
        // orders run, so neither 「first death」 nor 「latest death」 passes by chance.
        [UnityTest]
        public IEnumerator TwoKillsInTheWindow_ShowOneTextAboveTheEnemyNearestThePlayer()
        {
            yield return EnterTownWithBaseline();
            foreach (bool nearDiesFirst in new[] { true, false })
            {
                yield return SpawnSettled(NearX);
                int near = _lastSpawnedId;
                yield return SpawnSettled(FarX);
                int far = _lastSpawnedId;
                Vector3 nearSpot;
                Vector3 farSpot;
                if (nearDiesFirst)
                {
                    nearSpot = Kill(near);
                    farSpot = Kill(far);
                }
                else
                {
                    farSpot = Kill(far);
                    nearSpot = Kill(near);
                }
                Grant(KillGold, KillMaterial, 0);
                yield return WaitForText();
                string order = nearDiesFirst ? "가까운 적이 먼저 죽음" : "먼 적이 먼저 죽음";
                AssertShownAbove($"3.5 {order}(플레이어 x {_fixture.Player.transform.position.x:F2}, 먼 적 x {farSpot.x:F2})",
                    nearSpot, GoldTen, MaterialOne);
                if (!nearDiesFirst) yield break;
                yield return WaitUntilGone("3.5 첫 판의 처치 글");
                yield return LetTheLastDeathLeaveThePairingWindow();
            }
        }

        // 3.6 goal 「만들 것」 3: the Success result's share (an increment, lead facts 「던전 클리어」) is not a kill reward.
        [UnityTest]
        public IEnumerator KillThenDungeonClearShareOnly_ShowsNoText()
        {
            yield return EnterTownWithBaseline();
            yield return SpawnSettled(EnemyX);
            Kill(_lastSpawnedId);
            SendClearResult(InventoryResult.Success);
            Grant(ClearGold, ClearMaterial, ClearCoinPouch);
            yield return AssertNoTextAfterEarlierPacketsApply("3.6 처치 뒤 클리어 Success와 그 몫만 늘어난 스냅숏");
            yield return PositiveControl("3.6 클리어 몫 시험");
        }

        // 3.6 and addendum 01: from the non-zero baseline, 「total minus the clear share」 (85 − 50) cannot read 「+금화 10」.
        [UnityTest]
        public IEnumerator KillThenDungeonClearPlusTen_ShowsOnlyTheKillsGold()
        {
            yield return EnterTownWithBaseline();
            yield return SpawnSettled(EnemyX);
            Vector3 spot = Kill(_lastSpawnedId);
            SendClearResult(InventoryResult.Success);
            Grant(ClearGold + KillGold, ClearMaterial, ClearCoinPouch);
            yield return WaitForText();
            AssertShownAbove("3.6 처치 뒤 클리어 Success(금화 50)와 금화 25→85 스냅숏", spot, GoldTen);
        }

        // 3.6 goal 「만들 것」 3: no snapshot follows CurrencyCap, so its share must not shrink the next kill's increase.
        [UnityTest]
        public IEnumerator CurrencyCapResultWithoutSnapshot_DoesNotShrinkTheNextKill()
        {
            yield return EnterTownWithBaseline();
            SendClearResult(InventoryResult.CurrencyCap);
            yield return AfterEarlierPacketsApply();
            yield return SpawnSettled(EnemyX);
            Vector3 spot = Kill(_lastSpawnedId);
            Grant(KillGold, 0, 0);
            yield return WaitForText();
            AssertShownAbove("3.6 CurrencyCap 결과(뒤 스냅숏 없음) 다음 처치의 금화 +10", spot, GoldTen);
        }

        // 3.7 goal 「만들 것」 7: a map transition removes a text that is still floating.
        [UnityTest]
        public IEnumerator MapTransition_ClearsAShownText()
        {
            yield return EnterTownWithBaseline();
            yield return SpawnSettled(EnemyX);
            Kill(_lastSpawnedId);
            Grant(KillGold, KillMaterial, 0);
            yield return WaitForText();
            Assert.IsNotEmpty(_shown, $"3.7 맵 이동 전 처치 글이 없다 ({AppearSeconds}초 안에 생기지 않았다)");
            float shownAt = Time.realtimeSinceStartup;
            _fixture.Peer.Send(new S_MapTransition { destMapId = 1, spawnX = 2, spawnY = 0 }.Write());
            _fixture.Hp(++_hpMarker, MaxHp);
            yield return _fixture.WaitMap(1);
            float readyAfter = Time.realtimeSinceStartup - shownAt;
            yield return _fixture.Frames(2);
            TextMeshPro[] left = KillRewardFloatTextProbe.Find();
            Debug.Log($"[kill reward text] 3.7 map transition: new map ready {readyAfter:F2}s after the text showed, left {KillRewardFloatTextProbe.Describe(left)}");

            Assert.IsEmpty(left, $"3.7 맵 이동 뒤 처치 글이 남았다 — {KillRewardFloatTextProbe.Describe(left)}");
            // Longer than the shortest lifetime, and a natural expiry could not be told from clearing.
            Assert.Less(readyAfter, StillShownSeconds, $"시험 전제: 처치 글이 보인 뒤 새 맵 준비까지 {readyAfter:F2}초 — {StillShownSeconds}초보다 짧아야 지움을 가른다");
        }

        // 3.7 goal 「만들 것」 7: the old map's death records are cleared with the transition.
        [UnityTest]
        public IEnumerator KillInTheOldMap_IsForgottenAfterTheMapTransition()
        {
            yield return EnterTownWithBaseline();
            yield return SpawnSettled(EnemyX);
            Kill(_lastSpawnedId);
            float diedAt = Time.realtimeSinceStartup;
            _fixture.Peer.Send(new S_MapTransition { destMapId = 1, spawnX = 2, spawnY = 0 }.Write());
            _fixture.Hp(++_hpMarker, MaxHp);
            yield return _fixture.WaitMap(1);
            Grant(KillGold, KillMaterial, 0);
            float increaseAfter = Time.realtimeSinceStartup - diedAt;
            yield return AssertNoTextAfterEarlierPacketsApply($"3.7 옛 맵 사망 {increaseAfter:F2}초 뒤 새 맵에서 늘어난 스냅숏");
            yield return PositiveControl("3.7 옛 맵 사망 시험(새 맵)");
            // Past the pairing window, the negative check above would hold even if the record survived.
            Assert.Less(increaseAfter, PairingWindowSeconds, $"시험 전제: 옛 맵 사망에서 증가 스냅숏까지 {increaseAfter:F2}초 — {PairingWindowSeconds}초 창 안이어야 음성 단정이 판별력을 가진다");
        }

        // 3.7 goal 「만들 것」 2·7: after the connection ends, the next connection's first snapshot only sets the baseline.
        // The connection is ended and made again through the fixture only, as RepeatedFixtureCycles_… does.
        [UnityTest]
        public IEnumerator FirstSnapshotOfANewConnection_OnlySetsTheBaseline()
        {
            yield return EnterTownWithBaseline();
            MapEntryPlayFixture first = _fixture;
            _fixture = new MapEntryPlayFixture(); // TearDown then cleans the second connection.
            yield return first.Cleanup();
            _revision = 0;
            yield return EnterReadyTown();
            yield return SpawnSettled(EnemyX);
            Kill(_lastSpawnedId);
            SendInventory(BaseGold + KillGold, BaseMaterial + KillMaterial, 0);
            yield return AssertNoTextAfterEarlierPacketsApply("3.7 연결 종료 뒤 새 연결의 첫 스냅숏(금화 25→35)");
            yield return PositiveControl("3.7 새 연결 시험");
        }

        // 3.8 goal 「만들 것」 7 and 완료조건 2: the text takes no UI raycast and Left Ctrl (decision 65 A) still attacks.
        [UnityTest]
        public IEnumerator WhileTheTextIsShown_LeftCtrlStillAttacks_AndTheTextTakesNoRaycast()
        {
            yield return EnterTownWithBaseline();
            yield return ScreenUiProbe.WaitForEventSystem();
            yield return SpawnSettled(EnemyX);
            Kill(_lastSpawnedId);
            Grant(KillGold, KillMaterial, 0);
            yield return WaitForText();
            Assert.IsNotEmpty(_shown, $"3.8 처치 글이 없다 ({AppearSeconds}초 안에 생기지 않았다)");
            string[] textHits = _shown.SelectMany(text =>
            {
                HashSet<GameObject> own = ScreenUiProbe.GroupBelowCanvas(text);
                return ScreenUiProbe.RaycastHits(ScreenUiProbe.ScreenPoint(text)).Where(own.Contains);
            }).Select(hit => hit.name).ToArray();
            var player = _fixture.Player;
            yield return MapEntryPlayFixture.Wait(() => player.CanAttack && !player.IsActionLocked, "attack ready", 3);
            int attacks = _fixture.Peer.Count(PacketID.C_Attack);
            bool shownAtPress = KillRewardFloatTextProbe.Find().Length > 0;
            _fixture.Keys(Key.LeftCtrl);
            bool attacked = false;
            yield return ScreenUiProbe.WaitUntil(() => _fixture.Peer.Count(PacketID.C_Attack) == attacks + 1, 3f, met => attacked = met);
            _fixture.Keys();
            Debug.Log($"[kill reward text] 3.8 raycast hits on the text [{string.Join(", ", textHits)}], shown at press {shownAtPress}, " +
                $"C_Attack {attacks} → {_fixture.Peer.Count(PacketID.C_Attack)}");

            Assert.IsEmpty(textHits, "3.8 처치 글 화면 위치의 EventSystem.RaycastAll hit에 처치 글 객체가 있다");
            Assert.IsTrue(shownAtPress, "3.8 전제: 왼쪽 Ctrl을 누를 때 처치 글이 떠 있다");
            Assert.IsTrue(attacked, "3.8 처치 글이 떠 있는 동안 왼쪽 Ctrl이 C_Attack을 1건 더 보낸다");
        }

        // 3.9 goal 「파트 간 계약」: the font asset only; colour and outline are judged on the captures (goal 「시험 층」).
        [UnityTest]
        public IEnumerator KillRewardText_UsesTheLiberationSansFontAsset()
        {
            yield return EnterTownWithBaseline();
            yield return SpawnSettled(EnemyX);
            Kill(_lastSpawnedId);
            Grant(KillGold, KillMaterial, 0);
            yield return WaitForText();
            string[] fonts = _shown.Select(text => text.font != null ? text.font.name : "null").ToArray();
            Debug.Log($"[kill reward text] 3.9 fonts [{string.Join(", ", fonts)}]");

            Assert.IsNotEmpty(_shown, $"3.9 처치 글이 없다 ({AppearSeconds}초 안에 생기지 않았다)");
            CollectionAssert.AreEqual(Enumerable.Repeat(FontName, fonts.Length).ToArray(), fonts, "3.9 처치 글의 font asset 이름");
        }

        IEnumerator EnterReadyTown()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp(_hpMarker, MaxHp);
            yield return _fixture.WaitMap(0);
        }

        IEnumerator EnterTownWithBaseline()
        {
            yield return EnterReadyTown();
            SendInventory(BaseGold, BaseMaterial, 0);
            yield return AfterEarlierPacketsApply();
        }

        // As in DungeonClearRewardWindowSceneTests: the peer writes in order on one TCP stream and the client applies
        // packets in order on the main thread, so once this HP value shows, every packet sent before it has been handled.
        IEnumerator AfterEarlierPacketsApply()
        {
            int marker = ++_hpMarker;
            _fixture.Hp(marker, MaxHp);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.CurrentHp == marker, "HP marker " + marker, 3);
            yield return _fixture.Frames(5);
        }

        // Compact stacks sorted by item ID, as the authority sends them (InventorySnapshotState rejects anything else).
        void SendInventory(int gold, int material, int coinPouch)
        {
            _gold = gold;
            _material = material;
            _coinPouch = coinPouch;
            var stacks = new List<(int itemId, int count)>();
            if (material > 0) stacks.Add(((int)ItemId.Material, material));
            if (coinPouch > 0) stacks.Add(((int)ItemId.CoinPouch, coinPouch));
            var snapshot = new S_InventorySnapshot { revision = ++_revision, currency = gold };
            if (stacks.Count > 0)
            {
                snapshot.slot0ItemId = stacks[0].itemId;
                snapshot.slot0Count = stacks[0].count;
            }
            if (stacks.Count > 1)
            {
                snapshot.slot1ItemId = stacks[1].itemId;
                snapshot.slot1Count = stacks[1].count;
            }
            _fixture.Peer.Send(snapshot.Write());
            Debug.Log($"[kill reward text] snapshot revision {_revision}: gold {gold}, material {material}, coin pouch {coinPouch}");
        }

        void Grant(int gold, int material, int coinPouch) => SendInventory(_gold + gold, _material + material, _coinPouch + coinPouch);

        // The server's DungeonClearRewards.cs :9·:13~:14 values written out: 50 gold, one material, one coin pouch.
        void SendClearResult(InventoryResult result) => _fixture.Peer.Send(new S_DungeonClearResult
        {
            result = (byte)result,
            currency = ClearGold,
            reward0ItemId = (int)ItemId.Material,
            reward0Count = ClearMaterial,
            reward1ItemId = (int)ItemId.CoinPouch,
            reward1Count = ClearCoinPouch,
        }.Write());

        // MapEntrySceneLifecycleTests 93~98 and 111~114: spawn, one authoritative state at the same x, then wait for the
        // interpolated transform to settle there.
        IEnumerator SpawnSettled(float x)
        {
            int id = ++_enemyId;
            _fixture.Peer.Send(new S_EntitySpawn { entityId = id, entityKind = 0, x = x, y = 0, currentHp = MaxHp, maxHp = MaxHp }.Write());
            _fixture.Peer.Send(new S_EntityState { entityId = id, serverTick = ++_serverTick, x = x, y = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => EnemyRegistry.Instance != null && EnemyRegistry.Instance.TryGetTransform(id, out Transform enemy) &&
                Mathf.Abs(enemy.position.x - x) < 0.01f, $"enemy {id} settled at x {x}", 5);
            _lastSpawnedId = id;
        }

        // The death spot is the settled transform just before S_EntityDeath; the product reads the same transform before
        // it removes the enemy (goal 「만들 것」 1). The caller sends the snapshot right after, as the server does.
        Vector3 Kill(int id)
        {
            Assert.IsTrue(EnemyRegistry.Instance.TryGetTransform(id, out Transform enemy), $"fixture: enemy {id} is alive before its death");
            Vector3 spot = enemy.position;
            _fixture.Peer.Send(new S_EntityDeath { entityId = id }.Write());
            _lastKilledId = id;
            return spot;
        }

        IEnumerator WaitDeathApplied(int id) => MapEntryPlayFixture.Wait(() => EnemyRegistry.Instance == null ||
            !EnemyRegistry.Instance.TryGetTransform(id, out _), $"death {id} applied", 3);

        // Counted from the frame the last death is seen applied, so its record has surely left the 1.0 s window.
        IEnumerator LetTheLastDeathLeaveThePairingWindow()
        {
            if (_lastKilledId == 0) yield break;
            yield return WaitDeathApplied(_lastKilledId);
            float until = Time.realtimeSinceStartup + WindowExpirySeconds;
            yield return MapEntryPlayFixture.Wait(() => Time.realtimeSinceStartup >= until, "pairing window expiry", WindowExpirySeconds + 1);
        }

        IEnumerator WaitForText()
        {
            float deadline = Time.realtimeSinceStartup + AppearSeconds;
            _shown = KillRewardFloatTextProbe.Find();
            while (_shown.Length == 0 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                _shown = KillRewardFloatTextProbe.Find();
            }
        }

        IEnumerator WaitUntilGone(string what) =>
            MapEntryPlayFixture.Wait(() => KillRewardFloatTextProbe.Find().Length == 0, what + " gone", GoneSeconds + GoneWatchMarginSeconds);

        IEnumerator AssertNoTextAfterEarlierPacketsApply(string what)
        {
            yield return AfterEarlierPacketsApply();
            string seen = null;
            float until = Time.realtimeSinceStartup + QuietSeconds;
            while (Time.realtimeSinceStartup < until)
            {
                TextMeshPro[] texts = KillRewardFloatTextProbe.Find();
                if (texts.Length > 0 && seen == null) seen = KillRewardFloatTextProbe.Describe(texts);
                yield return null;
            }
            Debug.Log($"[kill reward text] {what}: {QuietSeconds}s watch saw {seen ?? "none"}");
            Assert.IsNull(seen, $"{what}: 처치 글이 떴다 — {seen}");
        }

        // The same test's positive check (pretest contract 3.4): a fresh kill and increase must show the text, so the
        // negative check before it cannot pass merely because nothing is implemented.
        IEnumerator PositiveControl(string what)
        {
            yield return LetTheLastDeathLeaveThePairingWindow();
            yield return SpawnSettled(EnemyX);
            Vector3 spot = Kill(_lastSpawnedId);
            Grant(KillGold, KillMaterial, 0);
            yield return WaitForText();
            AssertShownAbove(what + "의 양성 대조", spot, GoldTen, MaterialOne);
        }

        void AssertShownAbove(string what, Vector3 deathSpot, params string[] lines)
        {
            Vector3 center = KillRewardFloatTextProbe.Center(_shown);
            Debug.Log($"[kill reward text] {what}: death ({deathSpot.x:F2}, {deathSpot.y:F2}), shown {KillRewardFloatTextProbe.Describe(_shown)}; " +
                $"world TMP {KillRewardFloatTextProbe.DescribeWorldTexts()}");
            Assert.IsNotEmpty(_shown, $"{what}에서 처치 글이 없다 ({AppearSeconds}초 안에 「+」 줄이 있는 활성 월드 TMP가 생기지 않았다)");
            CollectionAssert.AreEqual(lines, KillRewardFloatTextProbe.Lines(_shown), $"{what}: 처치 글 줄");
            Assert.LessOrEqual(Mathf.Abs(center.x - deathSpot.x), AboveSpotMaxXOffset, $"{what}: 처치 글 x {center.x:F2}와 사망 위치 x {deathSpot.x:F2}의 차이");
            Assert.Greater(center.y, deathSpot.y, $"{what}: 처치 글 y가 사망 위치 y보다 위다");
        }

        // From the first frame the text is visible: one sample every SampleSeconds and on every count change, until it
        // is gone or GoneSeconds plus a margin has passed.
        IEnumerator SampleTheText(List<Sample> samples)
        {
            float deadline = Time.realtimeSinceStartup + AppearSeconds;
            TextMeshPro[] texts = KillRewardFloatTextProbe.Find();
            while ((texts.Length == 0 || KillRewardFloatTextProbe.Opacity(texts) <= 0f) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                texts = KillRewardFloatTextProbe.Find();
            }
            if (texts.Length == 0 || KillRewardFloatTextProbe.Opacity(texts) <= 0f) yield break;
            float start = Time.realtimeSinceStartup;
            float lastSample = float.NegativeInfinity;
            int lastCount = -1;
            while (true)
            {
                float dt = Time.realtimeSinceStartup - start;
                if (texts.Length != lastCount || dt - lastSample >= SampleSeconds)
                {
                    samples.Add(new Sample(dt, texts.Length, KillRewardFloatTextProbe.Center(texts).y, KillRewardFloatTextProbe.Opacity(texts)));
                    lastSample = dt;
                    lastCount = texts.Length;
                }
                if (texts.Length == 0 || dt > GoneSeconds + GoneWatchMarginSeconds) yield break;
                yield return null;
                texts = KillRewardFloatTextProbe.Find();
            }
        }

        readonly struct Sample
        {
            internal readonly float Dt;
            internal readonly int Count;
            internal readonly float Y;
            internal readonly float Opacity;

            internal Sample(float dt, int count, float y, float opacity)
            {
                Dt = dt;
                Count = count;
                Y = y;
                Opacity = opacity;
            }
        }
    }
}
