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
