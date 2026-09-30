using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Entities;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps.Systems;

/// <summary>
/// §2.2 RespawnSystem — GameMap(컨테이너)에서 enemy respawn 로직 추출.
///
/// **단일 책임**: enemy respawn 대기 큐 관리 + tick 카운트다운 + 재출현 처리.
/// **호출 규율(§1.1)**: GameMap.Tick 안에서만 호출 (직접 호출, tick 루프 동기).
/// **데이터 소유**: _respawnQueue는 본 System이 소유. GameMap은 EnqueueRespawn(enemy) mutator만 노출.
/// **System 간 직접 호출 X(§2.2)**: AllocId/AddEnemy는 map 인자 경유.
///
/// **tick 카운트다운 패턴(헌법 #5 정합)**:
///   await/Task.Delay/Thread.Sleep 금지. RespawnTicksRemaining 필드를 매 tick 감소 — 0 도달 시 respawn.
///
/// **새 entityId 발급**:
///   respawn = 논리적으로 새 적 출현. 기존 entityId는 S_EntityDeath로 이미 클라에서 despawn.
///   헌법 #2 "은퇴 ID 재사용 금지" 정합 — AllocId()로 새 id 발급.
///
/// 골렘 재출현마다 주입된 좌/우 지점을 교대한다. 골렘 수는 content와 등록된 대기 항목에 따른다.
/// </summary>
internal sealed class RespawnSystem
{
    readonly EnemyRespawnPlacement _placement;
    bool _golemSpawnAtLeft = true; // 맵별 첫 재스폰은 좌측.

    // enemy respawn 대기 큐.
    // **살아있는 적만 _enemies** invariant(컨테이너 주석)를 유지하기 위해 별도 보관.
    readonly List<EnemyEntity> _respawnQueue = new();

    internal RespawnSystem(EnemyRespawnPlacement placement)
    {
        _placement = placement;
    }

    /// <summary>
    /// respawn 대기 큐에 사망한 enemy 등록 — RespawnTicksRemaining 세팅 포함(kind별 타이머).
    /// </summary>
    internal void Enqueue(EnemyEntity dead)
    {
        dead.RespawnTicksRemaining = EnemyCatalog.For(dead.Kind).RespawnTicks;
        _respawnQueue.Add(dead);
    }

    /// <summary>
    /// enemy respawn 처리 1틱.
    /// </summary>
    internal void Process(GameMap map, long tickNumber)
    {
        // 역방향 순회 — 리스트에서 항목 제거 시 인덱스 어긋남 방지
        for (int i = _respawnQueue.Count - 1; i >= 0; i--)
        {
            EnemyEntity dead = _respawnQueue[i];
            dead.RespawnTicksRemaining--;

            if (dead.RespawnTicksRemaining <= 0)
            {
                _respawnQueue.RemoveAt(i);

                // 골렘은 주입된 좌/우 위치, 그 외는 원래 스폰 지점.
                float spawnX = dead.SpawnX, spawnY = dead.SpawnY;
                if (dead.Kind == EnemyKind.Golem)
                {
                    Vector2 spawn = _golemSpawnAtLeft ? _placement.GolemLeft : _placement.GolemRight;
                    spawnX = spawn.X;
                    spawnY = spawn.Y;
                    _golemSpawnAtLeft = !_golemSpawnAtLeft;
                }

                // 새 entity 생성 (새 entityId, 위치, 원본 MaxHp + Stats)
                EnemyEntity respawned = map.SpawnEnemy(dead.Kind, spawnX, spawnY, dead.MaxHp, dead.Stats);

                Console.WriteLine($"[Map] Enemy respawned: newId={respawned.EntityId} kind={respawned.Kind} at ({respawned.SpawnX},{respawned.SpawnY})");

                // 전원에게 S_EntitySpawn — 클라는 새 적 sprite 생성
                S_EntitySpawn spawnPacket = new S_EntitySpawn
                {
                    entityId = respawned.EntityId,
                    entityKind = (byte)respawned.Kind,
                    x = respawned.X,
                    y = respawned.Y,
                    currentHp = respawned.Hp,
                    maxHp = respawned.MaxHp,
                };
                map.BroadcastToAll(spawnPacket.Write());
            }
        }
    }
}
