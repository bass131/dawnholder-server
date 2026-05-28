using Dawnholder.Server.GameServer.Combat;

namespace Dawnholder.Server.GameServer.Maps;

// M4.2 Phase 01 (결정 2 — Spawn 하드코딩 모듈화):
// 맵별 enemy spawn 정의를 한 곳에서 선언. GameMap ctor가 switch 분기 없이
// 이 테이블에서 리스트를 받아 spawn만 실행하도록 분리.
//
// **설계 결정 — record vs class**:
//   EnemySpawnDef는 순수 데이터 운반체(id 없음, 로직 없음) → record(불변 + 값 비교) 적합.
//   MapSpawnTable은 static helper → 인스턴스 불필요, static 메서드만.
//
// **C# 클래스 수준까지만** (scope 가드):
//   JSON/ScriptableObject 데이터 파일로 빼지 않음 (콘텐츠 영역 = M4.3+ 범위).
//
// **헌법 #1 (Server Authority)**:
//   enemy spawn 정의는 서버 권위 → 02_Server 안에 위치.
//   클라는 S_EntitySpawn 패킷으로만 적의 존재를 알 수 있음 (이 파일 직접 참조 X).
//
// **헌법 #5**: 이 클래스는 ctor/tick thread 동기 코드에서만 사용. await/Task.Delay 없음.

/// <summary>
/// 단일 enemy spawn 정의 — 어떤 종류의 적을 어디에, 얼마의 HP로 spawn할지 서술.
/// record: 불변 + 값 비교 + 선언적 표현 (spawn 테이블 항목이므로 identity 불필요).
/// </summary>
public record EnemySpawnDef(EnemyKind Kind, float X, float Y, int MaxHp);

/// <summary>
/// M4.2 Phase 01: MapId → EnemySpawnDef 목록 매핑 (한 곳에서 정의).
///
/// 기존 GameMap에 흩어져 있던 const 좌표/HP를 여기로 흡수.
/// GameMap.NormalEnemySpawnX/Y/MaxHp · BossSpawnX/Y/MaxHp const는 *제거*하고
/// 이 테이블이 단일 진실 공급원(single source of truth)이 된다.
///
/// **현재 테이블** (M4.2 Phase 01 시점):
///   Town          → []  (빈 맵, 플레이어 spawn 전용)
///   HuntingGround → [Normal  @ (10, 0)  HP 30]
///   BossRoom      → [Boss    @ (30, 0)  HP 100]
///   Ending        → []  (빈 맵, 결과 화면 골격)
///
/// M4.3+ 콘텐츠 확장 시 여기에 항목을 추가하면 됨 (GameMap 코드 변경 불필요).
/// </summary>
public static class MapSpawnTable
{
    // 좌표/HP 상수를 테이블 안에 private const로 집약.
    // 옛 GameMap의 NormalEnemySpawnX/Y/MaxHp · BossSpawnX/Y/MaxHp를 흡수.
    //
    // **값 유지 이유**: 이 숫자들은 아키텍처 주석에 기록된 의도를 담고 있음.
    //   Normal @ x=10 → MoveSpeed 5 units/sec 기준 player spawn(0)에서 2초 거리.
    //   Boss   @ x=30 → 3-zone 우측 zone (좌=마을 x<0 / 중=전투 x≈10 / 우=보스 x=30).

    // HuntingGround — Normal enemy 위치 (M3 Phase 06 계승)
    const float NormalX = 10f;
    const float NormalY = 0f;
    const int NormalMaxHp = 30;

    // BossRoom — Boss 위치 (M3 Phase 07 계승)
    const float BossX = 30f;
    const float BossY = 0f;
    const int BossMaxHp = 100;

    // 빈 목록 — Town / Ending 공유 (할당 0회)
    static readonly IReadOnlyList<EnemySpawnDef> Empty =
        Array.Empty<EnemySpawnDef>();

    // HuntingGround spawn 목록 — Normal 1마리 고정
    static readonly IReadOnlyList<EnemySpawnDef> HuntingGroundSpawns =
        new[] { new EnemySpawnDef(EnemyKind.Normal, NormalX, NormalY, NormalMaxHp) };

    // BossRoom spawn 목록 — Boss 1마리 고정
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
