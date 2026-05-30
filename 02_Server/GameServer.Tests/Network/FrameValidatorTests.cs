using Dawnholder.Server.Network;

namespace GameServer.Tests.Network;

/// <summary>
/// FrameValidator 단위 테스트.
///
/// FrameValidator는 04_ClientNet/FrameValidator.cs와 동기화 약속된 서버 측 helper.
/// 본 테스트가 상수·분기 회귀를 commit 시점에 검출.
/// </summary>
public class FrameValidatorTests
{
    // ── 거부 케이스 ─────────────────────────────────────────────────────────

    [Fact]
    public void TryValidateFrameHeader_ZeroSize_Rejects()
    {
        // dataSize=0 → MinFrameSize(4) 미달 → false + reason 박힘
        bool result = FrameValidator.TryValidateFrameHeader(0, out var reason);

        Assert.False(result);
        Assert.NotNull(reason);
        Assert.Equal("too small", reason);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void TryValidateFrameHeader_BelowMin_Rejects(ushort dataSize)
    {
        // dataSize < MinFrameSize(4) → false + reason "too small"
        bool result = FrameValidator.TryValidateFrameHeader(dataSize, out var reason);

        Assert.False(result);
        Assert.Equal("too small", reason);
    }

    [Theory]
    [InlineData(4097)]
    [InlineData(8192)]
    [InlineData(65535)]
    public void TryValidateFrameHeader_AboveMax_Rejects(ushort dataSize)
    {
        // dataSize > MaxFrameSize(4096) → false + reason "too large"
        bool result = FrameValidator.TryValidateFrameHeader(dataSize, out var reason);

        Assert.False(result);
        Assert.Equal("too large", reason);
    }

    // ── 수락 케이스 ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(4)]      // 경계 최솟값 (MinFrameSize 정확히)
    [InlineData(100)]    // 중간 값
    [InlineData(4096)]   // 경계 최댓값 (MaxFrameSize 정확히)
    public void TryValidateFrameHeader_ValidSize_Accepts(ushort dataSize)
    {
        // MinFrameSize <= dataSize <= MaxFrameSize → true + reason null
        bool result = FrameValidator.TryValidateFrameHeader(dataSize, out var reason);

        Assert.True(result);
        Assert.Null(reason);
    }

    // ── 상수 정합 검증 ──────────────────────────────────────────────────────

    [Fact]
    public void MinFrameSize_Equals_HeaderPlusPacketId()
    {
        // MinFrameSize = HeaderSize(2) + PacketIdSize(2) = 4.
        // 이 값이 바뀌면 PacketSession 및 클라 FrameValidator와 동기화 필요.
        Assert.Equal(4, FrameValidator.MinFrameSize);
    }

    [Fact]
    public void MaxFrameSize_MatchesSharedConstants_DriftGuard()
    {
        // FrameValidator.MaxFrameSize와 Shared.GameData.Constants.MaxPacketSize는
        // 동기화 약속 — drift commit 시점에 즉시 검출.
        Assert.Equal(Shared.GameData.Constants.MaxPacketSize, FrameValidator.MaxFrameSize);
    }

    [Fact]
    public void PacketSession_Constants_DelegateToFrameValidator()
    {
        // PacketSession 상수가 FrameValidator 인용으로 바뀌었음 — 값 일치 보장.
        // 기존 PacketSession 참조 테스트가 FrameValidator drift 없이 통과하는지 재확인.
        Assert.Equal(FrameValidator.MinFrameSize, PacketSession.MinFrameSize);
        Assert.Equal(FrameValidator.MaxFrameSize, PacketSession.MaxFrameSize);
    }
}
