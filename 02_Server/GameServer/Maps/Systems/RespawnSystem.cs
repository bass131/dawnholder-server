using Dawnholder.Server.GameServer.Combat;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps;

/// <summary>
/// §2.2 RespawnSystem — GameMap(컨테이너)에서 Normal enemy respawn 로직 추출.
///
/// **단일 책임**: Normal enemy respawn 대기 큐 관리 + tick 카운트다운 + 재출현 처리.
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
/// </summary>
internal sealed class RespawnSystem
{
    // Normal enemy respawn 대기 큐.
    // **살아있는 적만 _enemies** invariant(컨테이너 주석)를 유지하기 위해 별도 보관.
    readonly List<EnemyEntity> _respawnQueue = new();

    // Normal enemy respawn 대기 틱 수 (tick 기반 타이머 — 헌법 #5 await 금지).
    // 20 TPS 기준 5초 = 100 tick.
    // **설계 결정**: 5초는 데모 반복 시연 시 자연스러운 재출현 간격 (1초=너무 짧음, 10초=흐름 끊김).
    internal const int NormalEnemyRespawnTicks = 100; // 5초 @ 20TPS

    /// <summary>
    /// respawn 대기 큐에 사망한 enemy 등록.
    /// GameMap.EnqueueRespawn(enemy) 내부에서 호출됨 — RespawnTicksRemaining 세팅 포함.
    /// </summary>
    internal void Enqueue(EnemyEntity dead)
    {
        dead.RespawnTicksRemaining = NormalEnemyRespawnTicks;
        _respawnQueue.Add(dead);
    }

    /// <summary>
    /// Normal enemy respawn 처리 1틱.
    /// GameMap.ProcessRespawns(tickNumber) 본문을 그대로 옮김 — 동작 완전 보존.
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

                // 새 entity 생성 (새 entityId, SpawnX/SpawnY 위치, 원본 MaxHp + Stats)
                EnemyEntity respawned = map.SpawnEnemy(dead.Kind, dead.SpawnX, dead.SpawnY, dead.MaxHp, dead.Stats);

                Console.WriteLine($"[Map] Enemy respawned: newId={respawned.EntityId} at ({respawned.SpawnX},{respawned.SpawnY})");

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
