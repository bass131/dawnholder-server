using System.Numerics;

namespace Dawnholder.Server.GameServer.Maps;

// portal 정의 + 맵별 portal 목록 테이블 (portal 정의의 단일 진실 공급원).
//
// **Portal record 선택 이유 (EnemySpawnDef와 같은 이유)**:
//   Portal은 순수 데이터 운반체(로직 없음, identity 없음) → record(불변 + 값 비교) 적합.
//
// **좌표 기준**: 각 맵의 출입구와 도착 지점은 아래 테이블에서 함께 정의한다.
//   DestSpawn은 목적지 맵의 되돌아오는 portal과 떨어뜨려 즉시 재겹침을 막는다.
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
/// Town          → HuntingGround: Town 출정문(x=25) → HuntingGround 좌측 spawn(x=2)<br/>
/// HuntingGround → BossRoom:      HuntingGround 유적(56,1) → BossRoom 우측 spawn(30,0)<br/>
/// BossRoom      → Ending:        BossRoom 출구 단(-9.5,1) → Ending spawn(0,0) [결과 화면]<br/>
/// Ending        → Town:          Ending 우측(x=5) → Town 좌측 spawn(x=0) [루프]<br/>
/// </para>
///
/// <para>
/// <b>역방향 portal (M5 B1, portalId=2):</b><br/>
/// HuntingGround → Town:          HuntingGround 좌측(x=-2) → Town spawn(x=21)<br/>
/// BossRoom      → HuntingGround: BossRoom 우측(x=34) → HuntingGround spawn(x=50)<br/>
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
    // 도착점은 사냥터 귀환 포탈(x=-2)에서 4만큼 떨어진다.
    static readonly IReadOnlyList<Portal> TownPortals = new[]
    {
        new Portal(PortalId: 1,
                   Position: new Vector2(25f, 0f),
                   Dest: MapId.HuntingGround,
                   DestSpawn: new Vector2(2f, 0f)),
    };

    // HuntingGround portal: 유적 단 → BossRoom 우측 통로 spawn.
    // 도착점은 보스방 귀환 포탈(x=34)에서 4만큼 떨어진다.
    //
    // 정방향(id=1)을 [0]에 유지 — 기존 인덱스 lookup(테스트/시나리오) 호환. 역방향(id=2)은 append.
    static readonly IReadOnlyList<Portal> HuntingGroundPortals = new[]
    {
        new Portal(PortalId: 1,
                   Position: new Vector2(56f, 1f),
                   Dest: MapId.BossRoom,
                   DestSpawn: new Vector2(30f, 0f)),
        // 역방향(M5 B1): HuntingGround → Town. portalId=2.
        // 도착점은 Town 출정문(x=25) 안쪽 4 — 도착 즉시 정방향 재겹침 방지.
        new Portal(PortalId: 2,
                   Position: new Vector2(-2f, 0f),
                   Dest: MapId.Town,
                   DestSpawn: new Vector2(21f, 0f)),
    };

    // BossRoom portal: 좌측 출구 단에서 Ending으로.
    // DestSpawn x=0: Ending 맵 기본 spawn.
    //
    // 정방향(id=1)을 [0]에 유지 — 기존 인덱스 lookup 호환. 역방향(id=2)은 append.
    static readonly IReadOnlyList<Portal> BossRoomPortals = new[]
    {
        new Portal(PortalId: 1,
                   Position: new Vector2(-9.5f, 1f),
                   Dest: MapId.Ending,
                   DestSpawn: new Vector2(0f, 0f)),
        // 역방향(M5 B1): BossRoom → HuntingGround. portalId=2.
        // 도착점은 HG 유적 포탈(56,1) 안쪽 — 도착 즉시 정방향(→Boss) 재겹침 방지.
        new Portal(PortalId: 2,
                   Position: new Vector2(34f, 0f),
                   Dest: MapId.HuntingGround,
                   DestSpawn: new Vector2(50f, 0f)),
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
        MapId.Town => TownPortals,
        MapId.HuntingGround => HuntingGroundPortals,
        MapId.BossRoom => BossRoomPortals,
        MapId.Ending => EndingPortals,
        _ => Empty,
    };
}
