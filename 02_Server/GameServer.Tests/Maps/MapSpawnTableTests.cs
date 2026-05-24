using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// M4.2 Phase 01 (결정 2 — Spawn 모듈화): MapSpawnTable 단위 테스트.
//
// **검증 범위**: MapSpawnTable.GetSpawnsFor(MapId) 반환값 정합.
//   GameMapContentTests와 층위 분리:
//     - 본 파일 = MapSpawnTable 자체 (spawn 정의 선언 정합) — GameMap 없음.
//     - GameMapContentTests = GameMap 인스턴스가 spawn table을 올바르게 사용했는지 (통합적).
//
// **헌법 #1**: spawn 정의는 서버 권위 — 이 테스트가 서버가 올바른 값을 선언했는지 보장.
public class MapSpawnTableTests
{
    // --- Town / Ending (빈 맵) ---

    [Fact]
    public void Town_ReturnsEmpty()
    {
        IReadOnlyList<EnemySpawnDef> spawns = MapSpawnTable.GetSpawnsFor(MapId.Town);
        Assert.Empty(spawns);
    }

    [Fact]
    public void Ending_ReturnsEmpty()
    {
        IReadOnlyList<EnemySpawnDef> spawns = MapSpawnTable.GetSpawnsFor(MapId.Ending);
        Assert.Empty(spawns);
    }

    // --- HuntingGround ---

    [Fact]
    public void HuntingGround_ReturnsOneNormalEntry()
    {
        IReadOnlyList<EnemySpawnDef> spawns = MapSpawnTable.GetSpawnsFor(MapId.HuntingGround);
        EnemySpawnDef def = Assert.Single(spawns);
        Assert.Equal(EnemyKind.Normal, def.Kind);
    }

    [Fact]
    public void HuntingGround_NormalEntry_HasExpectedCoordinates()
    {
        // 좌표 약속: MoveSpeed=5 units/sec 기준 player spawn(0,0)에서 2초 거리.
        // 값 변경 시 이 테스트가 즉시 실패 → 의도치 않은 값 변경 감지.
        EnemySpawnDef def = MapSpawnTable.GetSpawnsFor(MapId.HuntingGround)[0];
        Assert.Equal(10f, def.X);
        Assert.Equal(0f, def.Y);
    }

    [Fact]
    public void HuntingGround_NormalEntry_HasExpectedMaxHp()
    {
        // HP 30 = 설계 약속 (damage 10 × 3회 사망 기준, M4.1에서 Warrior 25 dmg로 2회로 변경됨).
        EnemySpawnDef def = MapSpawnTable.GetSpawnsFor(MapId.HuntingGround)[0];
        Assert.Equal(30, def.MaxHp);
    }

    // --- BossRoom ---

    [Fact]
    public void BossRoom_ReturnsOneBossEntry()
    {
        IReadOnlyList<EnemySpawnDef> spawns = MapSpawnTable.GetSpawnsFor(MapId.BossRoom);
        EnemySpawnDef def = Assert.Single(spawns);
        Assert.Equal(EnemyKind.Boss, def.Kind);
    }

    [Fact]
    public void BossRoom_BossEntry_HasExpectedCoordinates()
    {
        // 좌표 약속: 3-zone 우측 (좌=마을 x<0 / 중=전투 x≈10 / 우=보스 x=30).
        EnemySpawnDef def = MapSpawnTable.GetSpawnsFor(MapId.BossRoom)[0];
        Assert.Equal(30f, def.X);
        Assert.Equal(0f, def.Y);
    }

    [Fact]
    public void BossRoom_BossEntry_HasExpectedMaxHp()
    {
        // HP 100 = 설계 약속 (M3 Phase 07 박힘).
        EnemySpawnDef def = MapSpawnTable.GetSpawnsFor(MapId.BossRoom)[0];
        Assert.Equal(100, def.MaxHp);
    }

    // --- 미등록 MapId (미래 확장 안전망) ---

    [Fact]
    public void UnknownMapId_ReturnsEmpty()
    {
        // 등록되지 않은 MapId는 빈 목록 반환 (fail-safe: 빈 맵으로 처리).
        // 미래에 새 MapId가 추가됐을 때 MapSpawnTable에 항목을 추가하지 않아도 서버가 죽지 않음.
        // (int)99 캐스팅 = 현재 enum에 없는 값.
        IReadOnlyList<EnemySpawnDef> spawns = MapSpawnTable.GetSpawnsFor((MapId)99);
        Assert.Empty(spawns);
    }
}
