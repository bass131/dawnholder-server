using System.Numerics;
using Dawnholder.Server.GameServer.Sessions;

namespace Dawnholder.Server.GameServer.Maps;

// Phase 02 (M2): 단일 GameMap actor. 모든 PlayerEntity가 이 안에 살고,
// Tick() 호출은 단일 thread에서만 — *lock 없음*이 actor 패턴의 핵심.
//
// ARCHITECTURE "Map = Actor": 한 맵의 모든 mutation을 단일 thread에 가두면
// 동시성 버그의 90%가 사라진다. 외부에서 변경 필요 시 message channel(향후 JobQueue).
//
// Phase 02는 entity 컬렉션만 가지고 매 tick "Tick #N (Δ=Xms)" 로그만 찍는다.
// Phase 03부터 AddPlayer/RemovePlayer가 채워지고, Phase 04부터 위치 적분이 들어온다.
public class GameMap
{
    readonly List<PlayerEntity> _players = new();
    int _nextEntityId = 1;

    public IReadOnlyList<PlayerEntity> Players => _players;

    // 외부에서 사용. Phase 02에선 직접 호출 없음(테스트용 보존).
    // Phase 03부터 GameSession.OnConnected 핸들러가 JobQueue로 호출.
    public PlayerEntity AddPlayer(GameSession? owner = null, Vector2 spawnPos = default)
    {
        PlayerEntity entity = new PlayerEntity(_nextEntityId++, spawnPos, owner);
        _players.Add(entity);
        return entity;
    }

    public bool RemovePlayer(int entityId)
        => _players.RemoveAll(p => p.EntityId == entityId) > 0;

    // TickScheduler가 매 50ms마다 호출. tickNumber는 서버 시작 후 누적.
    // 이번 Phase에선 body가 비어있어도 OK (entity 0개라 할 일 없음).
    public void Tick(long tickNumber)
    {
        // Phase 04+: 각 player의 _pendingIntent를 적용해 Position 갱신.
        // 지금은 entity 컬렉션 살아있다는 사실만으로 충분.
    }
}
