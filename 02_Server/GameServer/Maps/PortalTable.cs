using System.Numerics;

namespace Dawnholder.Server.GameServer.Maps;

// portal 정의 + 맵별 portal 목록 테이블 (portal 정의의 단일 진실 공급원).
//
// **Portal record 선택 이유 (EnemySpawnDef와 같은 이유)**:
//   Portal은 순수 데이터 운반체(로직 없음, identity 없음) → record(불변 + 값 비교) 적합.
//
// **좌표 기준**: 3-zone 경계 좌표 계승.
//   Town 우측 portal = x=20 (마을 끝 / 전투구역 경계).
//   HuntingGround 우측 portal = x=25 (전투구역 끝 / 보스방 경계 — boss는 x=30).
//   DestSpawn은 목적지 맵의 좌측 입구 지점 (플레이어가 오른쪽에서 진입하는 자연스러운 흐름).
//
// **헌법 #1 (Server Authority)**:
//   portal 목적지/spawn 좌표는 서버 권위 — 클라가 C_EnterPortal(portalId)만 보내면 서버가 결정.
//   클라는 이 파일을 직접 참조할 수 없음 (02_Server 안, DLL 공유 대상 아님).

/// <summary>
/// 맵 사이를 잇는 portal 단일 정의.
///
/// <para>
/// <b>필드 설명:</b><br/>
/// - <see cref="PortalId"/> — 맵 안에서 unique한 portal 식별자. 클라가 C_EnterPortal.portalId로 참조.
///   전체 서버 globally-unique 불필요 (맵 범위 내 unique로 충분, Phase 03 검증 시 맵+portalId 조합으로 lookup).<br/>
/// - <see cref="Position"/> — 서버 권위 portal 위치 (충돌 판정용, Phase 03에서 근접 검증). 클라 렌더링 힌트도 됨.<br/>
/// - <see cref="Dest"/> — 목적지 맵 MapId. 서버가 결정 — 클라는 모름.<br/>
/// - <see cref="DestSpawn"/> — 목적지 맵의 입장 spawn 좌표. S_MapTransition.spawnX/Y에 박힘.
/// </para>
///
/// <para>
/// record: 불변 + 값 비교 + 선언적 표현 (portal 테이블 항목이므로 object identity 불필요).
/// </para>
/// </summary>
public record Portal(int PortalId, Vector2 Position, MapId Dest, Vector2 DestSpawn);

/// <summary>
/// MapId → Portal 목록 매핑 (단일 진실 공급원).
///
/// <para>
/// <b>현재 portal 흐름:</b><br/>
/// Town          → HuntingGround: Town 우측(x=20) → HuntingGround 좌측 spawn(x=2)<br/>
/// HuntingGround → BossRoom:      HuntingGround 우측(x=25) → BossRoom 좌측 spawn(x=22)<br/>
/// BossRoom      → Ending:        BossRoom 우측(x=35) → Ending spawn(x=0) [결과 화면]<br/>
/// Ending        → Town:          Ending 우측(x=5) → Town 좌측 spawn(x=0) [루프]<br/>
/// </para>
///
/// <para>
/// <b>역방향 portal (M5 B1, portalId=2):</b><br/>
/// HuntingGround → Town:          HuntingGround 좌측(x=5) → Town spawn(x=17)<br/>
/// BossRoom      → HuntingGround: BossRoom 좌측(x=18) → HuntingGround spawn(x=22)<br/>
/// 도착 spawn은 정방향 포탈 안쪽이되 근접(2)보다 떨어뜨려 재겹침/무한왕복 방지.
/// </para>
///
/// <para>
/// <b>데모 핵심 흐름</b>: Town → HuntingGround → BossRoom (전진 3단계).
/// 콘텐츠 확장 시 이 테이블에 항목 추가 (GameMap 코드 변경 불필요).
/// </para>
/// </summary>
public static class PortalTable
{
    // 빈 목록 — 포탈 없는 맵 공유 (할당 0회)
    static readonly IReadOnlyList<Portal> Empty = Array.Empty<Portal>();

    // Town portal: 우측 끝 → HuntingGround 좌측 spawn.
    // x=20: M3 3-zone 기준 마을 구역 끝 경계.
    // DestSpawn x=2: HuntingGround 진입 직후 좌측 — Normal enemy(x=10)와 충분한 거리.
    static readonly IReadOnlyList<Portal> TownPortals = new[]
    {
        new Portal(PortalId: 1,
                   Position: new Vector2(20f, 0f),
                   Dest: MapId.HuntingGround,
                   DestSpawn: new Vector2(2f, 0f)),
    };

    // HuntingGround portal: 우측 끝 → BossRoom 좌측 spawn.
    // x=25: Normal enemy(x=10) 너머 전투구역 끝.
    // DestSpawn x=22: BossRoom 진입 직후 좌측 — Boss(x=30)와 충분한 거리.
    //
    // 정방향(id=1)을 [0]에 유지 — 기존 인덱스 lookup(테스트/시나리오) 호환. 역방향(id=2)은 append.
    static readonly IReadOnlyList<Portal> HuntingGroundPortals = new[]
    {
        new Portal(PortalId: 1,
                   Position: new Vector2(25f, 0f),
                   Dest: MapId.BossRoom,
                   DestSpawn: new Vector2(22f, 0f)),
        // 역방향(M5 B1): HuntingGround → Town. portalId=2.
        // Position x=5: 좌측 입구 안쪽 — Town 도착 spawn(x=2)과 거리 3(>근접2)으로 재겹침 방지, enemy(x=10)와 거리 5.
        // DestSpawn x=17: Town 정방향 포탈(x=20) 안쪽 3 — 도착 즉시 정방향 재겹침 방지.
        new Portal(PortalId: 2,
                   Position: new Vector2(5f, 0f),
                   Dest: MapId.Town,
                   DestSpawn: new Vector2(17f, 0f)),
    };

    // BossRoom portal: 보스 클리어 후 Ending으로.
    // x=35: Boss(x=30) 너머 보스방 끝.
    // DestSpawn x=0: Ending 맵 기본 spawn.
    //
    // 정방향(id=1)을 [0]에 유지 — 기존 인덱스 lookup 호환. 역방향(id=2)은 append.
    static readonly IReadOnlyList<Portal> BossRoomPortals = new[]
    {
        new Portal(PortalId: 1,
                   Position: new Vector2(35f, 0f),
                   Dest: MapId.Ending,
                   DestSpawn: new Vector2(0f, 0f)),
        // 역방향(M5 B1): BossRoom → HuntingGround. portalId=2.
        // Position x=18: 보스방 좌측 — HG 도착 spawn(x=22)과 거리 4, Boss(x=30)/정방향(x=35)과 충분한 거리.
        // DestSpawn x=22: HG 정방향 포탈(x=25) 안쪽 3 — 도착 즉시 정방향(→Boss) 재겹침 방지.
        //   (보스 클리어 후 killCount 리셋되므로 재진입은 다시 40킬 필요 — 의도된 새 런 동선.)
        new Portal(PortalId: 2,
                   Position: new Vector2(18f, 0f),
                   Dest: MapId.HuntingGround,
                   DestSpawn: new Vector2(22f, 0f)),
    };

    // Ending portal: Town으로 루프.
    // x=5: Ending 맵 짧은 결과 화면 공간 끝.
    // DestSpawn x=0: Town 기본 spawn 지점 (플레이어 시작 위치).
    static readonly IReadOnlyList<Portal> EndingPortals = new[]
    {
        new Portal(PortalId: 1,
                   Position: new Vector2(5f, 0f),
                   Dest: MapId.Town,
                   DestSpawn: new Vector2(0f, 0f)),
    };

    /// <summary>
    /// 지정 맵의 portal 목록 반환.
    /// 등록되지 않은 MapId는 빈 목록 반환 (fail-safe).
    ///
    /// <para>
    /// <b>호출 invariant</b>: GameMap ctor 또는 tick thread 동기 코드에서만.
    /// 반환값은 readonly — 외부에서 변경 불가.
    /// </para>
    /// </summary>
    public static IReadOnlyList<Portal> GetPortalsFor(MapId mapId) => mapId switch
    {
        MapId.Town           => TownPortals,
        MapId.HuntingGround  => HuntingGroundPortals,
        MapId.BossRoom       => BossRoomPortals,
        MapId.Ending         => EndingPortals,
        _                    => Empty,
    };
}
