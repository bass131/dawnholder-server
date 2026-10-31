#nullable enable
using System;
using System.Collections.Generic;
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // 처치 보상 글의 계산 규칙 — Unity 객체 없이 값만 다룬다. 구독·시계·표시는 KillRewardFloatText가 맡는다.
    //
    // 서버는 「이 처치의 보상」을 따로 알리지 않는다(서버·패킷 무변경 안). 그래서 서버가 확정한 인벤토리
    // 스냅숏을 직전 사본과 비교한 증가분을, 실제 시간 창 안의 사망 기록과 짝짓는다.
    //   - 첫 스냅숏과 Reset 뒤 첫 스냅숏은 기준만 잡는다. 줄어든 값은 무시한다.
    //   - 던전 클리어 Success 결과의 몫(서버가 싣는 값은 합계가 아니라 증가분)은 바로 다음 스냅숏의 증가에서 뺀다.
    //     Success가 아닌 결과 뒤에는 스냅숏이 오지 않으므로 기록하지 않는다 — 다음 처치의 증가를 깎지 않는다.
    //   - 창 안의 사망이 여럿이면 로컬 플레이어에 가장 가까운 것을 쓰고 지운다. 사망 기록이 없으면 글도 없다.
    // 같은 순간의 다른 출처 증가(재화 주머니 사용 등)는 구분할 패킷이 없어 막지 않는다(goal 「남은 우려」).
    public sealed class KillRewardPairing
    {
        // 서버는 같은 틱에 사망 방송을 인벤토리 전송보다 먼저 보내고, 클라이언트는 둘을 같은 FIFO로 적용한다.
        // 창은 그 사이 frame 지연을 덮는 여유다. 이보다 늦게 늘어난 값은 그 처치의 보상으로 보지 않는다.
        public const float PairingWindowSeconds = 1.0f;

        readonly List<Death> _deaths = new();
        SortedDictionary<ItemId, int> _baselineItems = new();
        SortedDictionary<ItemId, int> _clearShareItems = new();
        bool _hasBaseline;
        int _baselineCurrency;
        int _clearShareCurrency;

        public static string GoldLine(int amount) => $"+금화 {amount}";

        public static string ItemLine(InventorySlot grown) => $"+{ItemCatalog.For(grown.ItemId).DisplayName} {grown.Count}";

        // 연결 종료·미러 교체: 기준·사망 기록·클리어 몫을 모두 버린다.
        public void Reset()
        {
            _hasBaseline = false;
            _baselineCurrency = 0;
            _baselineItems = new SortedDictionary<ItemId, int>();
            _deaths.Clear();
            DropClearShare();
        }

        public void SetBaseline(int currency, IReadOnlyList<InventorySlot> slots)
        {
            _hasBaseline = true;
            _baselineCurrency = currency;
            _baselineItems = Count(slots);
            // 기준보다 앞선 사망의 보상은 이미 기준 값에 들어 있다. 뒤에 오는 다른 증가와 짝짓지 않는다.
            _deaths.Clear();
            DropClearShare();
        }

        public void RecordDeath(Vector3 spot, float now)
        {
            DropExpiredDeaths(now);
            _deaths.Add(new Death(spot, now));
        }

        public void ExpectDungeonClearShare(int currency, IReadOnlyList<InventorySlot> items)
        {
            _clearShareCurrency = Math.Max(0, currency);
            _clearShareItems = Count(items);
        }

        // 적용된 스냅숏 하나를 받아 기준을 옮긴다. 늘어난 값과 창 안의 사망 기록이 둘 다 있을 때만 true다.
        public bool TryTakeReward(int currency, IReadOnlyList<InventorySlot> slots, float now, Vector3? player, out Reward reward)
        {
            reward = default;
            if (!_hasBaseline)
            {
                SetBaseline(currency, slots);
                return false;
            }

            SortedDictionary<ItemId, int> counts = Count(slots);
            int gold = Grown(currency, _baselineCurrency, _clearShareCurrency);
            var items = new List<InventorySlot>();
            foreach (KeyValuePair<ItemId, int> item in counts)
            {
                _baselineItems.TryGetValue(item.Key, out int before);
                _clearShareItems.TryGetValue(item.Key, out int share);
                int grown = Grown(item.Value, before, share);
                if (grown > 0) items.Add(new InventorySlot(item.Key, grown));
            }
            _baselineCurrency = currency;
            _baselineItems = counts;
            DropClearShare();

            if (gold == 0 && items.Count == 0) return false;
            if (!TryTakeNearestDeath(now, player, out Vector3 spot)) return false;
            reward = new Reward(spot, gold, items);
            return true;
        }

        // 줄어든 값은 0이다. 클리어 몫은 증가에서만 뺀다.
        static int Grown(int current, int before, int clearShare) => Math.Max(0, current - before - clearShare);

        static SortedDictionary<ItemId, int> Count(IReadOnlyList<InventorySlot> slots)
        {
            // 같은 아이템이 여러 칸에 나뉘어도 합계로 비교한다. 키 순서(아이템 ID 오름차순)가 줄 순서다.
            var counts = new SortedDictionary<ItemId, int>();
            foreach (InventorySlot slot in slots)
            {
                if (slot.ItemId == ItemId.None || slot.Count <= 0) continue;
                counts.TryGetValue(slot.ItemId, out int count);
                counts[slot.ItemId] = count + slot.Count;
            }
            return counts;
        }

        void DropClearShare()
        {
            _clearShareCurrency = 0;
            _clearShareItems = new SortedDictionary<ItemId, int>();
        }

        void DropExpiredDeaths(float now) => _deaths.RemoveAll(death => now - death.At > PairingWindowSeconds);

        bool TryTakeNearestDeath(float now, Vector3? player, out Vector3 spot)
        {
            DropExpiredDeaths(now);
            spot = default;
            if (_deaths.Count == 0) return false;

            // 플레이어 위치를 모르면 가장 최근 사망을 쓴다.
            int chosen = _deaths.Count - 1;
            if (player.HasValue)
            {
                float bestSq = float.PositiveInfinity;
                for (int i = 0; i < _deaths.Count; i++)
                {
                    // EnemyRegistry.TryGetNearest와 같은 x·y 평면 거리.
                    Vector3 diff = _deaths[i].Spot - player.Value;
                    float dSq = diff.x * diff.x + diff.y * diff.y;
                    if (dSq < bestSq)
                    {
                        bestSq = dSq;
                        chosen = i;
                    }
                }
            }
            spot = _deaths[chosen].Spot;
            _deaths.RemoveAt(chosen);
            return true;
        }

        // 사망 자리 하나와 그 위에 띄울 값. Items는 아이템 ID 오름차순의 증가분(아이템과 늘어난 개수)이다.
        public readonly struct Reward
        {
            public Reward(Vector3 spot, int gold, IReadOnlyList<InventorySlot> items)
            {
                Spot = spot;
                Gold = gold;
                Items = items;
            }

            public Vector3 Spot { get; }

            public int Gold { get; }

            public IReadOnlyList<InventorySlot> Items { get; }
        }

        readonly struct Death
        {
            public readonly Vector3 Spot;
            public readonly float At;

            public Death(Vector3 spot, float at)
            {
                Spot = spot;
                At = at;
            }
        }
    }
}
