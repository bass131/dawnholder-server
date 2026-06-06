namespace Shared.GameData;

/// <summary>
/// 월드 좌표 솔리드 지형 AABB. TerrainBaker(에디터) 생성 데이터의 단위 —
/// Physics가 소비 (M4.4-02). readonly struct → 결정론 안정성 + GC 압박 0.
/// </summary>
public readonly struct TerrainAabb
{
    public readonly float MinX;
    public readonly float MinY;
    public readonly float MaxX;
    public readonly float MaxY;

    public TerrainAabb(float minX, float minY, float maxX, float maxY)
    {
        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
    }
}

/// <summary>
/// one-way 발판 윗면의 수평 세그먼트 — 아래서 점프로 통과, 위에서 착지.
/// 면(face)만 데이터로 가짐: 발판은 두께 없는 착지 면으로 취급.
/// </summary>
public readonly struct TerrainPlatform
{
    public readonly float Y;
    public readonly float MinX;
    public readonly float MaxX;

    public TerrainPlatform(float y, float minX, float maxX)
    {
        Y = y;
        MinX = minX;
        MaxX = maxX;
    }
}

/// <summary>
/// 맵 1개의 솔리드 + 발판 지형 데이터 + killPlaneY.
///
/// <para>맵 로드 시 1회 생성 — 틱 루프에서는 Span 순회만 (할당 0, 헌법 #5).</para>
/// <para>방어 복사: 외부 배열 참조를 보관하지 않아 외부 변조를 차단.</para>
/// </summary>
public sealed class MapTerrain
{
    private readonly TerrainAabb[] _solids;
    private readonly TerrainPlatform[] _platforms;

    /// <summary>
    /// 구멍 낙하 경계 Y. killPlaneY 미사용 맵은 float.NegativeInfinity (= 영원히 미발동).
    /// </summary>
    public readonly float KillPlaneY;

    public System.ReadOnlySpan<TerrainAabb> Solids => _solids;
    public System.ReadOnlySpan<TerrainPlatform> Platforms => _platforms;

    public MapTerrain(TerrainAabb[] solids, TerrainPlatform[] platforms,
                      float killPlaneY = float.NegativeInfinity)
    {
        // 방어 복사 — 호출자가 배열을 나중에 수정해도 이 객체 상태는 불변.
        _solids    = solids    == null || solids.Length    == 0
            ? System.Array.Empty<TerrainAabb>()
            : (TerrainAabb[])solids.Clone();
        _platforms = platforms == null || platforms.Length == 0
            ? System.Array.Empty<TerrainPlatform>()
            : (TerrainPlatform[])platforms.Clone();
        KillPlaneY = killPlaneY;
    }

}
