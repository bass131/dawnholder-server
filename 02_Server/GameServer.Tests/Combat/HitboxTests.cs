using System.Numerics;
using Dawnholder.Server.GameServer.Combat;

namespace GameServer.Tests.Combat;

/// <summary>
/// AABB hitbox 단위 테스트 3건.
///
/// **검증 대상** (Hitbox.cs의 AABB.Intersects + AABB.Contains):
///   1. AABB_Intersects_HappyPath — 겹치는 두 박스 → Intersects=true
///   2. AABB_NoIntersect_OutOfRange — 완전히 분리된 두 박스 → Intersects=false
///   3. AABB_EdgeContact — 경계값(edge) 접촉 → Intersects=true (≤ 비교 정합)
///
/// **테스트 원칙**:
///   - 순수 기하 검증 — IO 없음, GameMap 의존 없음. AABB struct만 사용.
///   - Deterministic: 같은 input → 같은 output (헌법 #4 Formulas.cs 정합).
///   - edge-contact는 경계값 테스트의 핵심 — off-by-one 버그 방지.
/// </summary>
public class HitboxTests
{
    /// <summary>
    /// 1. AABB_Intersects_HappyPath: 확실히 겹치는 두 박스 → Intersects=true.
    ///
    /// **시나리오**:
    ///   - attackBox: center=(0,0), halfExtent=(1.5, 1.5) → x[-1.5, 1.5], y[-1.5, 1.5]
    ///   - targetBox: center=(1,0), halfExtent=(0.5, 0.5) → x[0.5, 1.5], y[-0.5, 0.5]
    ///   - 겹침: x 방향 [0.5, 1.5] 겹침, y 방향 [-0.5, 0.5] 겹침 → true.
    ///
    /// **게임 시나리오 매핑**:
    ///   attacker=(0,0), enemy=(1,0) → AttackHalfExtent=1.5f 박스가 enemy 1×1 박스 포함.
    /// </summary>
    [Fact]
    public void AABB_Intersects_HappyPath()
    {
        // 3×3 attack box (center 0,0)
        AABB attackBox = new AABB(new Vector2(0f, 0f), new Vector2(1.5f, 1.5f));
        // 1×1 enemy box (center 1,0 — attack box 안에 완전히 포함)
        AABB targetBox = new AABB(new Vector2(1f, 0f), new Vector2(0.5f, 0.5f));

        Assert.True(attackBox.Intersects(targetBox));
        Assert.True(targetBox.Intersects(attackBox)); // 대칭성 검증
    }

    /// <summary>
    /// 2. AABB_NoIntersect_OutOfRange: 분리된 두 박스 → Intersects=false.
    ///
    /// **시나리오**:
    ///   - attackBox: center=(0,0), halfExtent=(1.5, 1.5) → x[-1.5, 1.5]
    ///   - targetBox: center=(5,0), halfExtent=(0.5, 0.5) → x[4.5, 5.5]
    ///   - 분리: |Δx|=5, sumHalfX=2.0 → 5 > 2 → false.
    ///
    /// **게임 시나리오 매핑**:
    ///   player=(0,0), enemy=(5,0) → 완전히 사거리 밖(AttackRange=3.0f 박스 한참 초과).
    /// </summary>
    [Fact]
    public void AABB_NoIntersect_OutOfRange()
    {
        // 3×3 attack box (center 0,0)
        AABB attackBox = new AABB(new Vector2(0f, 0f), new Vector2(1.5f, 1.5f));
        // 1×1 enemy box (center 5,0 — 완전 분리)
        AABB targetBox = new AABB(new Vector2(5f, 0f), new Vector2(0.5f, 0.5f));

        Assert.False(attackBox.Intersects(targetBox));
        Assert.False(targetBox.Intersects(attackBox)); // 대칭성 검증
    }

    /// <summary>
    /// 3. AABB_EdgeContact: 정확히 경계값(edge) 접촉 → Intersects=true.
    ///
    /// **시나리오**:
    ///   - attackBox: center=(0,0), halfExtent=(1.5, 1.5) → x[-1.5, 1.5]
    ///   - targetBox: center=(2,0), halfExtent=(0.5, 0.5) → x[1.5, 2.5]
    ///   - 접촉: |Δx|=2.0, sumHalfX=1.5+0.5=2.0 → 2.0 ≤ 2.0 → true (= 포함).
    ///
    /// **경계값 정책 (≤ 비교 선택 이유)**:
    ///   게임에서 edge-contact는 "간신히 닿음" = hit으로 판정하는 것이 직관적 (클라 체감 정합).
    ///   &lt; 비교(strict)였다면 edge = miss → 억울한 miss 발생.
    ///
    /// **also 검증**: Contains(Vector2) 경계값.
    /// </summary>
    [Fact]
    public void AABB_EdgeContact_IsIntersecting()
    {
        // 3×3 attack box (center 0,0) → x[-1.5, 1.5]
        AABB attackBox = new AABB(new Vector2(0f, 0f), new Vector2(1.5f, 1.5f));
        // 1×1 enemy box (center 2,0) → x[1.5, 2.5] : edge-contact at x=1.5
        AABB targetBox = new AABB(new Vector2(2f, 0f), new Vector2(0.5f, 0.5f));

        // edge 접촉 → Intersects=true (≤ 비교 약속).
        Assert.True(attackBox.Intersects(targetBox));
        Assert.True(targetBox.Intersects(attackBox));

        // Contains edge 점: (1.5, 0) = attackBox 오른쪽 경계 → Contains=true (≤ 비교).
        Assert.True(attackBox.Contains(new Vector2(1.5f, 0f)));

        // Contains 경계 밖 점: (1.51, 0) → false.
        Assert.False(attackBox.Contains(new Vector2(1.51f, 0f)));
    }
}
