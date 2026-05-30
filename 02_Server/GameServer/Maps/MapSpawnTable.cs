using Dawnholder.Server.GameServer.Combat;

namespace Dawnholder.Server.GameServer.Maps;

// 맵별 enemy spawn 정의를 한 곳에서 선언. GameMap ctor가 switch 분기 없이
// 이 테이블에서 리스트를 받아 spawn만 실행하도록 분리.
//
// **설계 결정 — record vs class**:
//   EnemySpawnDef는 순수 데이터 운반체(id 없음, 로직 없음) → record(불변 + 값 비교) 적합.
//
// **헌법 #1 (Server Authority)**:
//   enemy spawn 정의는 서버 권위 → 02_Server 안에 위치.
//   클라는 S_EntitySpawn 패킷으로만 적의 존재를 알 수 있음 (이 파일 직접 참조 X).

/// <summary>
/// 단일 enemy spawn 정의 — 어떤 종류의 적을 어디에, 얼마의 HP로 spawn할지 서술.
/// record: 불변 + 값 비교 + 선언적 표현 (spawn 테이블 항목이므로 identity 불필요).
/// </summary>
public record EnemySpawnDef(EnemyKind Kind, float X, float Y, int MaxHp);

/// <summary>
/// MapId → EnemySpawnDef 목록 매핑 (단일 진실 공급원).
///
/// **현재 테이블**:
///   Town          → []  (빈 맵, 플레이어 spawn 전용)
///   HuntingGround → [Normal  @ (10, 0)  HP 30]
///   BossRoom      → [Boss    @ (30, 0)  HP 100]
///   Ending        → []  (빈 맵, 결과 화면 골격)
///
/// 콘텐츠 확장 시 여기에 항목을 추가하면 됨 (GameMap 코드 변경 불필요).
/// </summary>
public static class MapSpawnTable
{
    // **좌표 의도**: 이 숫자들은 아키텍처 주석에 기록된 의도를 담고 있음.
    //   Normal @ x=10 → MoveSpeed 5 units/sec 기준 player spawn(0)에서 2초 거리.
    //   Boss   @ x=30 → 3-zone 우측 zone (좌=마을 x<0 / 중=전투 x≈10 / 우=보스 x=30).

    const float NormalX = 10f;
    const float NormalY = 0f;
    const int NormalMaxHp = 30;

    const float BossX = 30f;
    const float BossY = 0f;
    const int BossMaxHp = 100;

    // 빈 목록 — Town / Ending 공유 (할당 0회)
    static readonly IReadOnlyList<EnemySpawnDef> Empty =
        Array.Empty<EnemySpawnDef>();

    static readonly IReadOnlyList<EnemySpawnDef> HuntingGroundSpawns =
        new[] { new EnemySpawnDef(EnemyKind.Normal, NormalX, NormalY, NormalMaxHp) };

    static readonly IReadOnlyList<EnemySpawnDef> BossRoomSpawns =
        new[] { new EnemySpawnDef(EnemyKind.Boss, BossX, BossY, BossMaxHp) };

    /// <summary>
    /// 지정 맵의 spawn 정의 목록 반환.
    /// 등록되지 않은 MapId는 빈 목록 반환 (fail-safe: 빈 맵으로 처리).
    ///
    /// **호출 invariant**: GameMap ctor 또는 tick thread 동기 코드에서만.
    /// 반환값은 readonly — 외부에서 변경 불가.
    /// </summary>
    public static IReadOnlyList<EnemySpawnDef> GetSpawnsFor(MapId mapId) => mapId switch
    {
        MapId.HuntingGround => HuntingGroundSpawns,
        MapId.BossRoom      => BossRoomSpawns,
        _                   => Empty,   // Town, Ending, 미래 확장 MapId 모두 빈 맵
    };
}
