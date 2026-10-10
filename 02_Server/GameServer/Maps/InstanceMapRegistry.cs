namespace Dawnholder.Server.GameServer.Maps;

// 이동 job이 월드 팩토리로 복사본을 만들고, GameWorld가 틱 끝에 빈 복사본을 정리한다.
// 항목은 맵과 입장 대기 수를 소유하며 모든 접근은 월드 틱 스레드 전용이다.
// 시험은 스케줄러 없이 틱을 직접 실행하므로 IsTickThread 하드 검사는 두지 않는다.
internal sealed class InstanceMapRegistry
{
    readonly Dictionary<(MapId MapId, InstanceKey Key), Entry> _entries = new();
    readonly List<Entry> _creationOrder = new();
    readonly Func<MapId, InstanceKey, GameMap> _createMap;

    internal InstanceMapRegistry(Func<MapId, InstanceKey, GameMap> createMap)
    {
        _createMap = createMap;
    }

    internal int Count => _entries.Count;

    internal IEnumerable<GameMap> Maps
    {
        get
        {
            foreach (Entry entry in _creationOrder) yield return entry.Map;
        }
    }

    internal bool TryGet(MapId mapId, InstanceKey key, out GameMap? map)
    {
        bool found = _entries.TryGetValue((mapId, key), out Entry? entry);
        map = entry?.Map;
        return found;
    }

    internal GameMap GetOrCreate(MapId mapId, InstanceKey key)
    {
        if (!MapKindTable.IsInstanced(mapId))
            throw new ArgumentException("공용 맵은 복사본으로 만들 수 없습니다.", nameof(mapId));
        if (_entries.TryGetValue((mapId, key), out Entry? existing)) return existing.Map;

        GameMap map = _createMap(mapId, key);
        Entry entry = new(map);
        _entries.Add((mapId, key), entry);
        _creationOrder.Add(entry);
        Console.WriteLine($"[Map] instance {mapId} {key.Owner} {key.Id} created");
        return map;
    }

    internal void FillTickSnapshot(List<GameMap> buffer)
    {
        foreach (Entry entry in _creationOrder) buffer.Add(entry.Map);
    }

    internal void EnqueueArrival(GameMap map, Action arrival)
    {
        if (map.InstanceKey is not InstanceKey key ||
            !_entries.TryGetValue((map.MapId, key), out Entry? entry) ||
            !ReferenceEquals(entry.Map, map))
        {
            map.EnqueueJob(arrival);
            return;
        }

        entry.Incoming++;
        try
        {
            map.EnqueueJob(() =>
            {
                try { arrival(); }
                finally { entry.Incoming--; }
            });
        }
        catch
        {
            entry.Incoming--;
            throw;
        }
    }

    internal void RemoveIdle()
    {
        int retained = 0;
        for (int i = 0; i < _creationOrder.Count; i++)
        {
            Entry entry = _creationOrder[i];
            if (entry.Map.Players.Count == 0 && entry.Incoming == 0)
            {
                InstanceKey key = entry.Map.InstanceKey!.Value;
                _entries.Remove((entry.Map.MapId, key));
                Console.WriteLine($"[Map] instance {entry.Map.MapId} {key.Owner} {key.Id} retired");
            }
            else
            {
                _creationOrder[retained++] = entry;
            }
        }
        _creationOrder.RemoveRange(retained, _creationOrder.Count - retained);
    }

    sealed class Entry(GameMap map)
    {
        internal readonly GameMap Map = map;
        internal int Incoming;
    }
}
