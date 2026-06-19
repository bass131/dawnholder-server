using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.GameServer.Entities;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps;

// 맵 시뮬레이션의 wire-format 표현 책임 (§2.2 분리).
//
//   GameMap = 시뮬레이션(틱/물리/사망/스폰) + actor 경계.
//   MapPacketPublisher = "그 시뮬 상태를 어떤 패킷으로 조립해 누구에게 보낼지" 결정.
//
// M7.7 P4a 추출 동기: GameMap이 S_Snapshot/S_PlayerHp/S_EntityDeath 등 wire format을
//   직접 알면 시뮬↔표현 결합 = SRP 위반 + M8 영속화 훅이 얹힐 자리가 패킷 조립과 뒤섞임.
//   조립 책임만 여기로 빼고, 송신 메커니즘(BroadcastToAll / Session.Send)은 GameMap이 보유.
//
// **byte 동치 계약 (§2)**: 추출 전후 패킷 ID·필드·순서·수신자·송신 시점이 1bit도 안 바뀐다.
//   조립 코드는 GameMap 원본에서 그대로 옮긴 것 — 동작 불변(behavior-invariant).
//
// **tick thread invariant (§1.1 / §5)**: 모든 메서드는 tick thread에서만 호출
//   (GameMap.Tick 안 또는 EnqueueJob 람다 안). publisher 자체는 무상태 — GameMap 참조만 보유.
//   송신은 Session.Send → lock + queue enqueue 이므로 논블로킹 (헌법 #5).
internal sealed class MapPacketPublisher
{
    readonly GameMap _map;

    internal MapPacketPublisher(GameMap map) => _map = map;

    /// <summary>
    /// SnapshotTickInterval 마다 player별 S_Snapshot 조립 + 전원 broadcast.
    /// per-entity 발송 구조 보존 — player 1명당 패킷 1개, 전원 수신(자기 포함, remote view 정합).
    /// animState는 서버 권위 결정(헌법 #1) — ActionFsm 현재 상태에서 계산.
    /// </summary>
    internal void BroadcastSnapshots(long tickNumber)
    {
        foreach (PlayerEntity p in _map.Players)
        {
            byte animState = ComputePlayerAnimState(p);

            S_Snapshot pkt = new S_Snapshot
            {
                entityId = p.EntityId,
                x = p.Position.X,
                y = p.Position.Y,
                vx = p.Velocity.X,
                vy = p.Velocity.Y,
                serverTick = (int)tickNumber,
                lastAckedClientTick = p.LastClientTick,
                animState = animState
            };
            _map.BroadcastToAll(pkt.Write());
        }
    }

    /// <summary>
    /// 적 사망 시 S_EntityDeath 전원 broadcast. HandleEnemyDeath / DespawnEnemyByFall 공통.
    /// </summary>
    internal void BroadcastEntityDeath(int entityId)
    {
        S_EntityDeath death = new S_EntityDeath { entityId = entityId };
        _map.BroadcastToAll(death.Write());
    }

    /// <summary>
    /// 보스 처치 시 S_StageClear 전원 broadcast.
    /// **순서 계약(BossStageClearTests)**: 호출처(HandleEnemyDeath)가 S_EntityDeath → S_StageClear 순으로 호출.
    /// </summary>
    internal void BroadcastStageClear(int bossEntityId)
    {
        S_StageClear stageClear = new S_StageClear { bossEntityId = bossEntityId };
        _map.BroadcastToAll(stageClear.Write());
    }

    /// <summary>
    /// 플레이어 본인에게만 S_PlayerHp 1:1 송신.
    /// currentHp는 Math.Max(0, p.Hp) floor — 음수 방어 (표시 전용, 사망 lifecycle은 S_EntityDeath 채널).
    /// closing-skip: Owner null / IsClosing 둘 다 skip (BroadcastToAll 정책 정합).
    /// </summary>
    internal void SendPlayerHp(PlayerEntity p)
    {
        if (p.Owner == null || p.Owner.IsClosing) return;
        S_PlayerHp pkt = new S_PlayerHp
        {
            entityId  = p.EntityId,
            currentHp = Math.Max(0, p.Hp),
            maxHp     = p.MaxHp,
        };
        p.Owner.Send(pkt.Write());
    }

    /// <summary>
    /// 새 진입 세션에게 현재 roster를 1:1 Send — 기존 player(S_PlayerJoin) + 살아있는 enemy(S_EntitySpawn).
    /// existingPlayers: 호출부가 AddPlayer *전에* 찍은 snapshot(자기 자신 제외). 순서 의존성은 호출부 책임.
    /// closing-skip: Owner null / IsClosing 둘 다 skip.
    /// **§2 wire**: S_PlayerJoin/S_EntitySpawn 필드·순서·발송 순서 통합 전과 byte 단위 동일.
    /// </summary>
    internal void SendInitialRoster(GameSession target, List<PlayerEntity> existingPlayers)
    {
        foreach (PlayerEntity existing in existingPlayers)
        {
            if (existing.Owner == null) continue;
            if (existing.Owner.IsClosing) continue;
            S_PlayerJoin rosterEntry = new S_PlayerJoin
            {
                entityId = existing.EntityId,
                spawnX = existing.Position.X,
                spawnY = existing.Position.Y,
                characterClass = (byte)existing.Stats.Class,
            };
            target.Send(rosterEntry.Write());
        }

        foreach (EnemyEntity enemy in _map.Enemies.Values)
        {
            if (enemy.IsDead) continue;
            S_EntitySpawn enemySpawn = new S_EntitySpawn
            {
                entityId = enemy.EntityId,
                entityKind = (byte)enemy.Kind,
                x = enemy.X,
                y = enemy.Y,
                currentHp = enemy.Hp,
                maxHp = enemy.MaxHp,
            };
            target.Send(enemySpawn.Write());
        }
    }

    /// <summary>
    /// 플레이어의 현재 시각 애니메이션 상태 계산. 서버 권위 (헌법 #1).
    /// ActionFsm이 단일 출처 — Death/Hit/Attack/Jump/Walk/Idle 우선순위는 FSM 전이 규칙으로 보장.
    /// </summary>
    static byte ComputePlayerAnimState(PlayerEntity p)
        => (byte)p.ActionFsm.AnimState;
}
