using Dawnholder.Server.GameServer.Maps;

namespace GameServer.Tests.Combat;

/// <summary>
/// CombatSystem.ValidateRewind 단위 테스트.
///
/// 검증 목표:
///   (a) clientTick 음수 → false
///   (b) clientTick > serverTick(미래) → false
///   (c) serverTick - clientTick > MaxRewindTicks(4) → false
///   경계값: diff==4 통과(>4만 reject), diff==0/1 통과
/// </summary>
public class ValidateRewindTests
{
    // (a) 음수 clientTick — 즉시 reject
    [Fact]
    public void NegativeClientTick_ReturnsFalse()
    {
        Assert.False(CombatSystem.ValidateRewind(-1, 100));
    }

    [Fact]
    public void LargeNegativeClientTick_ReturnsFalse()
    {
        Assert.False(CombatSystem.ValidateRewind(long.MinValue, 100));
    }

    // (b) 미래 clientTick (clientTick > serverTick) — reject
    [Fact]
    public void FutureClientTick_ReturnsFalse()
    {
        Assert.False(CombatSystem.ValidateRewind(101, 100));
    }

    [Fact]
    public void ClientTickOneAheadOfServer_ReturnsFalse()
    {
        Assert.False(CombatSystem.ValidateRewind(11, 10));
    }

    // (c) diff > 4 (상한 초과) — reject
    [Fact]
    public void DiffFive_ReturnsFalse()
    {
        Assert.False(CombatSystem.ValidateRewind(95, 100)); // diff=5
    }

    [Fact]
    public void DiffLarge_ReturnsFalse()
    {
        Assert.False(CombatSystem.ValidateRewind(0, 100)); // diff=100
    }

    // 유효 범위 — true
    [Fact]
    public void DiffZero_ReturnsTrue()
    {
        Assert.True(CombatSystem.ValidateRewind(100, 100)); // diff=0
    }

    [Fact]
    public void DiffOne_ReturnsTrue()
    {
        Assert.True(CombatSystem.ValidateRewind(99, 100)); // diff=1
    }

    [Fact]
    public void DiffFour_ReturnsTrue()
    {
        // 경계값: diff==4는 통과 (>4만 reject)
        Assert.True(CombatSystem.ValidateRewind(96, 100)); // diff=4
    }

    [Fact]
    public void ServerTickZero_ClientTickZero_ReturnsTrue()
    {
        Assert.True(CombatSystem.ValidateRewind(0, 0));
    }
}
