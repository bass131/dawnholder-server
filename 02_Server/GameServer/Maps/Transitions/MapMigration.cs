using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Quest;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps.Transitions;

/// <summary>
/// 맵 간 플레이어 이동(migration) 로직 헬퍼. 검증 단계와 transfer 단계로 분리.
///
/// **trust-boundary invariant**: 검증 실패 시 silent drop, 성공 시 migration 실행 —
/// 동일 입력에 동일 판정 (헌법 #3).
///
/// **헌법 #5 정합**: Execute(...)는 항상 EnqueueJob 람다 *안*에서 호출됨 (tick thread).
/// 호출 위치(GameSession.SubmitEnterPortal)가 감싼다. 맵 B EnqueueJob은 Execute 내부에서
/// 호출 — Map=Actor 원칙 정합 (한 맵의 tick thread가 다른 맵을 직접 mutate 금지).
/// </summary>
internal static class MapMigration
{
    // 근접 임계 2 unit (헌법 #3 — 텔레포트 핵 차단).
    // PortalTable 좌표에서 네트워크 지연 1-2 tick(50-100ms) 내 위치 오차 최대 ~1.5 unit 흡수.
    private const float ProximityThreshold = 2f;

    /// <summary>
    /// portal 진입 처리. 반드시 tick thread(EnqueueJob 람다) 안에서 호출.
    ///
    /// **검증 단계** (trust-boundary): portal lookup → 플레이어 존재 → 근접 검증.
    ///   실패 시 silent drop (return).
    /// **transfer 단계**: 맵 A RemovePlayer + S_PlayerLeave broadcast →
    ///   맵 B AddPlayerWithId + roster/enemy 재전송 + S_PlayerJoin broadcast.
    ///
    /// 모든 맵 mutation은 해당 맵의 tick thread에서 실행 (Map=Actor 원칙, 헌법).
    /// </summary>
    /// <param name="session">이동 중인 플레이어 세션. 상태 조작 internal hooks + AddPlayerWithId owner.</param>
    /// <param name="entityId">세션의 entityId (_entityId 캡처값, tick thread 진입 전 캡처).</param>
    /// <param name="currentMap">현재 맵 (맵 A). 호출자가 EnqueueJob으로 이 맵 tick thread에서 호출.</param>
    /// <param name="portalId">클라가 보낸 portalId (untrusted — 범위 검증 후 사용).</param>
    /// <param name="getDestMap">목적지 맵 조회 delegate (virtual hook 위임 — 테스트 override 지원).</param>
    /// <param name="getKillCount">entityId → killCount 서버 권위 조회 delegate (테스트 override 지원).</param>
    public static void Execute(
        GameSession session,
        int entityId,
        GameMap currentMap,
        int portalId,
        Func<MapId, GameMap?> getDestMap,
        Func<int, int> getKillCount)
    {
        // ── 검증 단계 ────────────────────────────────────────────────────

        // 1) portal lookup — portalId가 현재 맵의 유효 portal인가
        // hot-path 일관성: LINQ FirstOrDefault 대신 foreach (클로저 할당 회피).
        Portal? portal = null;
        foreach (Portal p in currentMap.Portals)
        {
            if (p.PortalId == portalId) { portal = p; break; }
        }
        if (portal == null)
        {
            Console.WriteLine($"[Trust] Player {entityId}: invalid portalId={portalId} for map={currentMap.MapId} — silent drop");
            return;
        }

        // 2) 플레이어 존재 확인
        PlayerEntity? player = currentMap.GetPlayer(entityId);
        if (player == null) return; // 이미 없는 경우 (race)

        // 3) 근접 검증 (헌법 #3 — 텔레포트 핵 차단)
        float dx = player.Position.X - portal.Position.X;
        float dy = player.Position.Y - portal.Position.Y;
        float distSq = dx * dx + dy * dy;
        if (distSq > ProximityThreshold * ProximityThreshold)
        {
            Console.WriteLine(
                $"[Trust] Player {entityId}: portal proximity fail — dist²={distSq:F2} > threshold²={ProximityThreshold * ProximityThreshold} — silent drop");
            return;
        }

        // ── 보스 포탈 잠금 게이트 (trust-boundary, M5 Q3) ───────────────────
        //
        // RemovePlayer/SetMigrating *전*에 차단 — 거부 시 원래 맵 잔류(ghost 방지).
        // killCount는 getKillCount delegate(QuestRegistry 서버 권위) — 클라 주장 X.
        // Dest==BossRoom 진입 방향만 게이트: 역방향(Boss→HG)은 나갈 때 자유.
        if (portal.Dest == MapId.BossRoom)
        {
            int killCount = getKillCount(entityId);
            if (killCount < QuestConstants.BossUnlockKillCount)
            {
                S_PortalLocked locked = new S_PortalLocked
                {
                    requiredCount = QuestConstants.BossUnlockKillCount,
                    currentCount = killCount,
                };
                session.Send(locked.Write());
                Console.WriteLine(
                    $"[Gate] Player {entityId}: BossRoom locked killCount={killCount}<{QuestConstants.BossUnlockKillCount} — entry denied");
                return;
            }
        }

        // ── transfer 단계 ─────────────────────────────────────────────────

        // 검증 통과 → migration 시작.
        // 캡처: migration에 필요한 상태 (tick thread 안에서 읽으므로 안전)
        PlayerStats capturedStats = player.Stats;
        int capturedHp = player.Hp;
        Vector2 destSpawn = portal.DestSpawn;
        MapId destMapId = portal.Dest;

        // _migrating = 1 세팅 — 이 시점부터 GetMap() null 반환 (transient drop 시작)
        // tick thread에서 세팅하지만 GetMap()은 socket thread에서도 읽음 → SetMigrating(Volatile.Write).
        session.SetMigrating(1);

        // 맵 A: RemovePlayer + 남은 플레이어에게 S_PlayerLeave broadcast
        currentMap.RemovePlayer(entityId);
        S_PlayerLeave leaveNotice = new S_PlayerLeave { entityId = entityId };
        currentMap.BroadcastToAll(leaveNotice.Write()); // 자기 자신은 이미 _players에서 빠짐

        Console.WriteLine($"[Map] Player {entityId} left map={currentMap.MapId} → heading to {destMapId}");

        // 맵 B 조회
        GameMap? destMap = getDestMap(destMapId);
        if (destMap == null)
        {
            // 목적지 맵 없음 = config 버그. disconnect (무결성 우선).
            Console.WriteLine($"[Error] Destination map {destMapId} not found — disconnecting player {entityId}");
            session.SetMigrating(0);
            session.Disconnect();
            return;
        }

        // 맵 B: EnqueueJob으로 AddPlayerWithId 마샬링 (Map=Actor 원칙).
        // 한 맵의 tick thread가 다른 맵 상태를 직접 mutate 금지.
        int capturedEntityId = entityId;
        destMap.EnqueueJob(() =>
        {
            // closing race: 이미 disconnect된 세션이면 skip
            if (session.ReadClosing() == 1)
            {
                session.SetMigrating(0);
                return;
            }

            // 맵 B 기존 플레이어 snapshot (자기 자신 추가 전 — initial roster 정합)
            List<PlayerEntity> existingInDest = new(destMap.Players);

            // AddPlayerWithId: 기존 entity id 유지 (ADR-026 핵심)
            PlayerEntity newEntity = destMap.AddPlayerWithId(
                capturedEntityId, session, destSpawn, capturedStats, capturedHp);

            // _currentMapId 갱신 + _migrating 해제 (이 시점부터 GetMap() 정상 반환)
            session.SetCurrentMapId(destMapId);
            session.SetMigrating(0);

            // 본인에게 S_MapTransition (목적지 맵 + spawn 좌표 — entityId 없음, ADR-026)
            S_MapTransition transition = new S_MapTransition
            {
                destMapId = (byte)destMapId,
                spawnX = destSpawn.X,
                spawnY = destSpawn.Y,
            };
            session.Send(transition.Write());

            // 맵 전환 입장도 "진입" — 캐리된 HP(full 아님)를 권위 통지해 HUD 표시 미러 갭 봉합.
            // 누락 시 데미지 입은 채 맵 넘으면 클라가 Start() placeholder(full HP) 고착 (reviewer 🟡①).
            destMap.SendPlayerHp(newEntity);

            // 본인에게 맵 B 기존 roster(player + 살아있는 enemy) 1:1 Send — 통합 메서드.
            destMap.SendInitialRosterTo(session, existingInDest);

            // 맵 B 기존 플레이어에게 신규 진입자 S_PlayerJoin broadcast
            // characterClass: 서버 newEntity.Stats.Class byte cast (헌법 #3).
            S_PlayerJoin joinNotice = new S_PlayerJoin
            {
                entityId = newEntity.EntityId,
                spawnX = newEntity.Position.X,
                spawnY = newEntity.Position.Y,
                characterClass = (byte)newEntity.Stats.Class,
            };
            destMap.BroadcastToAll(joinNotice.Write(), except: session);

            Console.WriteLine(
                $"[Map] Player {capturedEntityId} arrived at map={destMapId} spawn=({destSpawn.X},{destSpawn.Y}) — hp={capturedHp}, roster:{existingInDest.Count}");
        });
    }
}
