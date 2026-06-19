using Dawnholder.Server.GameServer.Handlers;
using Shared.Protocol;

namespace GameServer.Tests.Handlers;

/// <summary>
/// [헌법 #3 빌드타임 봉합 증명 — SN-02] 치트 게이트가 *런타임 플래그*가 아니라 *빌드 구성*에
/// 종속됨을 정량 확인. 치트 사슬 전체(HandlerRegistry 등록 → CheatCommandHandler →
/// GameSession.SubmitCheatCommand → QuestRegistry.DebugCompleteQuest)가 #if DEBUG.
///
/// **한 테스트가 양 구성에서 PASS**:
///   - DEBUG: C_CheatCommand 등록(시연 F8 치트 유지) → #if 분기 Assert.True
///   - Release: C_CheatCommand 미등록(unknown PacketID → silent drop) → #else 분기 Assert.False
///
/// "회귀 green ≠ 위반 봉합": 통상 회귀는 DEBUG라 치트가 정상 작동 → green이어도 위반 제거를
/// 증명 못 함. Release 구성 PASS가 C_CheatCommand 미등록을 별도 증명한다.
/// </summary>
public class CheatBuildGateTests
{
    [Fact]
    public void C_CheatCommand_Registration_IsBuildGated()
    {
        bool registered = HandlerRegistry.TryGet(PacketID.C_CheatCommand, out _);
#if DEBUG
        Assert.True(registered,  "DEBUG 빌드 = 치트 등록(시연 F8)");
#else
        Assert.False(registered, "Release 빌드 = 치트 미등록 (헌법 #3 빌드타임 봉합)");
#endif
    }
}
