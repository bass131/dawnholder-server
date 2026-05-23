using System.Numerics;

namespace Dawnholder.Server.GameServer.Combat;

// M4.1 Phase 06 (5단계): precision hitbox — AABB (Axis-Aligned Bounding Box).
//
// **AABB vs capsule trade-off**:
//   - AABB = 축 정렬 직사각형 박스. 계산 ~5 비교, 단순, 빠름.
//     단점: 회전하는 충돌체(무기 스윙 호 등)는 부정확. 2D 사이드스크롤 점프 판정 시
//     박스가 실루엣과 어긋날 수 있음.
//   - capsule = 선분 + 반지름. 점프·회전 정합 ↑. 계산 ~20 비교. 구현 복잡도 ↑.
//   → 현재 등급: AABB. capsule은 M4.3 backlog (보스 AI + 점프 정합 가치 ↑ 시점).
//
// **중심 + half-extent 표현**:
//   - 다른 표현 후보: (min, max) 쌍. 둘 다 동등하지만 "중심에서 반경" 직관이 스폰 코드에서 자연스러움.
//   - Intersects 구현: 각 축별 |Δcenter| ≤ halfExtentA + halfExtentB.
//
// **readonly struct 선택 이유**:
//   - struct: GC 부담 없음. 게임 엔진 표준 패턴 (Unity Bounds, AABB 모두 struct).
//   - readonly: 불변 보장 → 캐시 + 실수로 mut 방지.
//   - class였다면: tick thread에서 매 attack마다 heap 할당 → GC pressure ↑.
public readonly struct AABB
{
    /// <summary>박스 중심.</summary>
    public readonly Vector2 Center;

    /// <summary>
    /// 각 축 반-길이(half-extent). X = 너비의 절반, Y = 높이의 절반.
    /// 예: 3×3 박스 → HalfExtent = (1.5f, 1.5f).
    /// </summary>
    public readonly Vector2 HalfExtent;

    public AABB(Vector2 center, Vector2 halfExtent)
    {
        Center = center;
        HalfExtent = halfExtent;
    }

    /// <summary>
    /// 점 <paramref name="point"/>가 이 AABB 안에 있는지 검사.
    /// 경계값(edge)도 포함 (≤ 비교).
    /// </summary>
    public bool Contains(Vector2 point)
    {
        float dx = Math.Abs(point.X - Center.X);
        float dy = Math.Abs(point.Y - Center.Y);
        return dx <= HalfExtent.X && dy <= HalfExtent.Y;
    }

    /// <summary>
    /// 다른 AABB와 겹치는지 검사 (교차 판정).
    ///
    /// **알고리즘**: SAT(Separating Axis Theorem) 2D 특수화.
    ///   두 박스가 *겹치지 않으려면* 한 축에서 분리 갭이 있어야 함.
    ///   즉, |Δcenter.X| > sumHalfX 또는 |Δcenter.Y| > sumHalfY 이면 미교차.
    ///   그 외에는 교차.
    ///
    /// 경계값(edge-contact)도 교차로 처리 (≤ 비교 → ≥로 분리 판정).
    /// </summary>
    public bool Intersects(AABB other)
    {
        float dx = Math.Abs(other.Center.X - Center.X);
        float dy = Math.Abs(other.Center.Y - Center.Y);
        float sumHalfX = HalfExtent.X + other.HalfExtent.X;
        float sumHalfY = HalfExtent.Y + other.HalfExtent.Y;
        return dx <= sumHalfX && dy <= sumHalfY;
    }
}
