using Dawnholder.Server.GameServer.Combat;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps;

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
/// **골렘 1층 교차 스폰(M6)**: 골렘은 항상 1마리 유지하되 처치 시 1층 좌↔우 지점을 번갈아 재출현.
/// </summary>
internal sealed class RespawnSystem
{
    // Normal enemy respawn 대기 틱 수 (tick 기반 타이머 — 헌법 #5 await 금지).
    // **설계 결정**: 5초는 데모 반복 시연 시 자연스러운 재출현 간격 (1초=너무 짧음, 10초=흐름 끊김).
    internal const int NormalEnemyRespawnTicks = 100; // 5초 @ 20TPS

    // 골렘 respawn 대기 틱 — 슬라임보다 약간 느리게(영호 튜닝 지점). 골렘은 1마리 유지 + 위치 교차.
    internal const int GolemRespawnTicks = 120; // 6초 @ 20TPS

    // 골렘 "1층 교차 스폰" — 처치 시 좌↔우를 번갈아 재출현(영호 튜닝 지점).
    const float GolemSpawnLeftX  = -8.5f;
    const float GolemSpawnRightX =  9.5f;
    const float GolemFloor1Y     =  0f;
    bool _golemSpawnAtLeft = true; // 첫 재스폰은 좌측(원본 골렘이 중앙우측이라 반대편부터)

    // enemy respawn 대기 큐.
    // **살아있는 적만 _enemies** invariant(컨테이너 주석)를 유지하기 위해 별도 보관.
    readonly List<EnemyEntity> _respawnQueue = new();

    /// <summary>
    /// respawn 대기 큐에 사망한 enemy 등록 — RespawnTicksRemaining 세팅 포함(kind별 타이머).
    /// </summary>
    internal void Enqueue(EnemyEntity dead)
    {
        dead.RespawnTicksRemaining =
            dead.Kind == EnemyKind.Golem ? GolemRespawnTicks : NormalEnemyRespawnTicks;
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

                // 골렘은 1층 좌↔우 교차 위치, 그 외는 원래 스폰 지점.
                float spawnX = dead.SpawnX, spawnY = dead.SpawnY;
                if (dead.Kind == EnemyKind.Golem)
                {
                    spawnX = _golemSpawnAtLeft ? GolemSpawnLeftX : GolemSpawnRightX;
                    spawnY = GolemFloor1Y;
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
