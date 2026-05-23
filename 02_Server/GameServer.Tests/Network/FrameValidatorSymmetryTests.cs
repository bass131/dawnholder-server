using ServerValidator = Dawnholder.Server.Network.FrameValidator;
using ClientValidator = Dawnholder.Client.Net.FrameValidator;

namespace GameServer.Tests.Network;

/// <summary>
/// M4.1 Phase 03 옵션 B 변형의 contract test —
/// 서버/클라 두 FrameValidator helper가 *같은 상수 + 같은 결과*를 보장하는지
/// commit 시점에 자동 검출.
///
/// <para>
/// <b>배경</b>: Phase 03에서 옵션 B 변형(서버/클라 각자 박되 동기화 약속 + drift guard)
/// 을 채택했습니다. 옛 drift guard 2건(FrameValidatorTests)은 서버 측만 검증 — 클라 측
/// FrameValidator가 따로 변경되어도 서버 테스트가 잡지 못하는 비대칭 잠복이 있었습니다.
/// </para>
///
/// <para>
/// Codex β cross-review(2026-05-23) 보강 = 본 symmetry test가 그 마지막 10%를 봉합.
/// 두 helper의 상수 + 행동을 직접 cross-validation해서 헌법 #4 "복사-붙여넣기 금지"
/// 정신의 진짜 *동등 보호*를 달성.
/// </para>
///
/// <para>
/// <b>ADR-012 Y2 분리 정합</b>: 본 테스트의 04_ClientNet 참조는 *contract test 전용*.
/// production code 흐름에서는 서버 측이 클라 어셈블리 참조 X 그대로 유지.
/// </para>
/// </summary>
public class FrameValidatorSymmetryTests
{
    // ── 상수 동기화 ─────────────────────────────────────────────────────────

    [Fact]
    public void MinFrameSize_ServerAndClient_AreEqual()
    {
        // 동기화 약속 = 두 helper의 MinFrameSize는 반드시 같은 값.
        // 어느 한쪽 상수가 바뀌면 본 테스트가 commit 시점에 잡음.
        Assert.Equal(ServerValidator.MinFrameSize, ClientValidator.MinFrameSize);
    }

    [Fact]
    public void MaxFrameSize_ServerAndClient_AreEqual()
    {
        Assert.Equal(ServerValidator.MaxFrameSize, ClientValidator.MaxFrameSize);
    }

    // ── 행동 동기화 — 대표 입력별 양쪽 결과 일치 ──────────────────────────

    [Theory]
    [InlineData((ushort)0)]      // zero
    [InlineData((ushort)1)]      // below-min
    [InlineData((ushort)3)]      // below-min boundary
    [InlineData((ushort)4)]      // valid boundary (min)
    [InlineData((ushort)100)]    // valid middle
    [InlineData((ushort)4096)]   // valid boundary (max)
    [InlineData((ushort)4097)]   // above-max boundary
    [InlineData((ushort)8192)]   // above-max
    [InlineData((ushort)65535)]  // above-max extreme
    public void TryValidateFrameHeader_ServerAndClient_AgreeOnResult(ushort dataSize)
    {
        bool serverResult = ServerValidator.TryValidateFrameHeader(dataSize, out var serverReason);
        bool clientResult = ClientValidator.TryValidateFrameHeader(dataSize, out var clientReason);

        // bool 결과 일치
        Assert.Equal(serverResult, clientResult);

        // reason 문자열 일치 (둘 다 null 또는 둘 다 같은 reason)
        Assert.Equal(serverReason, clientReason);
    }
}
