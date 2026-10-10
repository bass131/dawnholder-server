using System.Collections;
using System.Linq;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Separate CLI filter. Requires an exclusively owned real GameServer at 127.0.0.1:7777.
    // Rewards come only from a real keyboard kill; no scripted peer, bot or direct grant can satisfy it.
    public sealed class InventoryServerIntegrationTests
    {
        const float MeleeGap = 0.8f; // inside the headless bot's 1.0 melee distance
        MapEntryPlayFixture _fixture;
        [SetUp] public void Setup() => _fixture = new MapEntryPlayFixture();
        [UnityTearDown] public IEnumerator Teardown() => _fixture.Cleanup();

        static int Held(ItemId item)
        {
            var mirror = InventoryState.Instance;
            int count = 0;
            for (int i = 0; i < InventoryLimits.MaxSlots; i++)
                if (mirror.GetSlot(i).ItemId == item) count += mirror.GetSlot(i).Count;
            return count;
        }

        static int SlotOf(ItemId item)
        {
            for (int i = 0; i < InventoryLimits.MaxSlots; i++)
                if (InventoryState.Instance.GetSlot(i).ItemId == item) return i;
            return -1;
        }

        IEnumerator WaitSynchronized(string label) => MapEntryPlayFixture.Wait(() =>
            _fixture.Session.Inventory.IsSynchronized && InventoryPanelProbe.Status == "서버 확인 완료", label, 10);

        [UnityTest]
        public IEnumerator ProductionServer_KeyboardGolemKill_ShowsTheReward_AndAPointerUseIsConfirmedBySnapshot()
        {
            yield return _fixture.Prepare(true);
            yield return _fixture.WaitMap(0);
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            yield return WaitSynchronized("initial Town snapshot");
            Debug.Log($"[PR2 verifier real server] screen {Screen.width}x{Screen.height}, Town rev={InventoryState.Instance.Revision} " +
                $"currency={InventoryState.Instance.Currency} material={Held(ItemId.Material)} pouch={Held(ItemId.CoinPouch)}");
            yield return HudOverlapProbe.AssertPanelClearOfHud("real server Town");

            yield return _fixture.WalkIntoPortal(1, 1);
            yield return WaitSynchronized("HuntingGround requery");
            yield return HudOverlapProbe.AssertPanelClearOfHud("real server HuntingGround");
            uint revision = InventoryState.Instance.Revision;
            int currency = InventoryState.Instance.Currency;
            int material = Held(ItemId.Material);
            int pouch = Held(ItemId.CoinPouch);

            yield return MapEntryPlayFixture.Wait(() => EnemyRegistry.Instance.EnemyTransforms
                .Any(e => EnemyRegistry.Instance.TryGetKind(e.entityId, out EnemyKind k) && k == EnemyKind.Golem), "server Golem spawn", 20);
            int golem = EnemyRegistry.Instance.EnemyTransforms
                .First(e => EnemyRegistry.Instance.TryGetKind(e.entityId, out EnemyKind k) && k == EnemyKind.Golem).entityId;
            // #2 R9 control on the real server: the pointer rests on a panel button during the whole keyboard kill.
            yield return InventoryPanelProbe.PointAt(_fixture.Mouse, InventoryPanelProbe.ScreenPoint(InventoryPanelProbe.Refresh));
            yield return KillWithKeyboard(golem, 120f);

            yield return MapEntryPlayFixture.Wait(() => Held(ItemId.CoinPouch) > pouch, "server reward push after the kill", 10);
            yield return WaitSynchronized("reward shown as synchronized");
            int materialGain = Held(ItemId.Material) - material;
            int pouchGain = Held(ItemId.CoinPouch) - pouch;
            int currencyGain = InventoryState.Instance.Currency - currency;
            Debug.Log($"[PR2 verifier real server] golem={golem} killed; rev {revision}->{InventoryState.Instance.Revision} " +
                $"currency +{currencyGain} material +{materialGain} pouch +{pouchGain}; panel '{InventoryPanelProbe.Currency}' " +
                $"rows '{InventoryPanelProbe.Slot(0)}' '{InventoryPanelProbe.Slot(1)}'");
            Assert.AreEqual(1, pouchGain, "PR1 P-C: a Golem kill grants one CoinPouch");
            Assert.AreEqual(10 * (materialGain + pouchGain), currencyGain, "PR1 P-C: each normal/Golem kill grants 10 currency");
            Assert.AreEqual($"재화 {InventoryState.Instance.Currency:N0}", InventoryPanelProbe.Currency, "R6 server currency shown");
            int pouchSlot = SlotOf(ItemId.CoinPouch);
            Assert.AreEqual($"재화 주머니 ×{Held(ItemId.CoinPouch)}", InventoryPanelProbe.Slot(pouchSlot), "R6 server stack shown");

            uint rewardRevision = InventoryState.Instance.Revision;
            int rewardCurrency = InventoryState.Instance.Currency;
            int rewardPouch = Held(ItemId.CoinPouch);
            Component use = InventoryPanelProbe.Use(pouchSlot);
            Assert.IsTrue(InventoryPanelProbe.Interactable(use), "R6 the owned pouch is usable on the synchronized entry");
            yield return InventoryPanelProbe.AttackReady(_fixture.Player);
            yield return InventoryPanelProbe.Click(_fixture.Mouse, use);
            bool cooldownByUse = !_fixture.Player.CanAttack;
            bool lockByUse = _fixture.Player.IsActionLocked;
            Assert.IsFalse(cooldownByUse, "#2 a pointer Use click predicts no attack cooldown on the real server path");
            Assert.IsFalse(lockByUse, "#2 a pointer Use click starts no attack commit lock");
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Inventory.LastResult.HasValue, "server S_ItemUseResult", 10);
            Assert.AreEqual(InventoryResult.Success, _fixture.Session.Inventory.LastResult.Value);
            Assert.AreEqual(rewardRevision + 1, _fixture.Session.Inventory.ResultRevision);
            yield return MapEntryPlayFixture.Wait(() => InventoryState.Instance.Revision == rewardRevision + 1, "server snapshot after the use", 10);
            yield return WaitSynchronized("use confirmed by snapshot");
            Assert.AreEqual(rewardCurrency + 50, InventoryState.Instance.Currency, "PR1 CoinPouch use grants 50");
            Assert.AreEqual(rewardPouch - 1, Held(ItemId.CoinPouch));
            Assert.AreEqual($"재화 {InventoryState.Instance.Currency:N0}", InventoryPanelProbe.Currency);
            Assert.AreEqual("아이템 사용이 완료되었습니다.", InventoryPanelProbe.Result);
            yield return HudOverlapProbe.AssertPanelClearOfHud("real server use result");
            Debug.Log($"[PR2 verifier real server] pointer use Success rev={InventoryState.Instance.Revision} currency={InventoryState.Instance.Currency} " +
                $"pouch={Held(ItemId.CoinPouch)} panel '{InventoryPanelProbe.Currency}' result '{InventoryPanelProbe.Result}' " +
                $"hp={_fixture.Session.Entry.CurrentHp}/{_fixture.Session.Entry.MaxHp}");

            // #2 R9 control on the real server: a mouse click on the game world still starts an attack prediction,
            // which the client starts only after TryAttack sent C_Attack.
            yield return InventoryPanelProbe.AttackReady(_fixture.Player);
            Vector2 world = InventoryPanelProbe.PlayerScreenPoint(_fixture.Player);
            yield return InventoryPanelProbe.ClickAt(_fixture.Mouse, world);
            yield return MapEntryPlayFixture.Wait(() => !_fixture.Player.CanAttack, "world click attack prediction", 2);
            Debug.Log($"[PR2 verifier real server] world click at {world} started an attack; keyboard kill ran with the pointer on Refresh");
        }

        // Real keyboard only: A/D to close the gap, Enter edges for attacks once the cooldown mirror allows.
        IEnumerator KillWithKeyboard(int target, float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            Key facing = Key.D;
            while (EnemyRegistry.Instance.TryGetTransform(target, out Transform enemy) && enemy != null)
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, "keyboard could not kill the Golem");
                Assert.AreEqual(1, _fixture.Session.Entry.MapId, "the player must stay in HuntingGround");
                float gap = enemy.position.x - _fixture.Player.transform.position.x;
                Key toward = gap > 0 ? Key.D : Key.A;
                if (Mathf.Abs(gap) > MeleeGap || (toward != facing && Mathf.Abs(gap) > 0.2f))
                {
                    _fixture.Keys(toward);
                    facing = toward;
                    yield return null;
                    continue;
                }
                _fixture.Keys();
                yield return null;
                if (!_fixture.Player.CanAttack || _fixture.Player.IsActionLocked) continue;
                _fixture.Keys(Key.Enter);
                yield return null;
                _fixture.Keys();
                yield return null;
            }
            _fixture.Keys();
        }
    }
}
