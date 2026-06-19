#if DEBUG
using Dawnholder.Server.GameServer.Debug;
using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers;

// C_CheatCommand 핸들러 (시연용 디버그). 클래스 전체가 #if DEBUG —
//   Release 빌드에는 *물리적으로 부재*하고 HandlerRegistry에도 미등록(헌법 #3 빌드타임 봉합, SN-02).
//   DebugConfig.AllowCheats는 DEBUG 빌드 *내부*의 2차 런타임 토글일 뿐, 봉합의 1차는 이 빌드 게이트.
//
// **헌법 #3 (Trust Boundary)**:
//   - DEBUG 내 AllowCheats=false면 무시 — 클라가 F8 눌러도 서버가 처리 안 함(2차 토글).
//   - 행위자=session._entityId 강제(SubmitCheatCommand 안) — 패킷에 대상 필드 없음, 도용 차단.
//   - class 선택(월드 진입) 전 입력은 silent drop.
internal sealed class CheatCommandHandler : IPacketHandler
{
    // 치트 입력 — class 선택(월드 진입) 전 = silent drop. dispatch 일괄 게이트가 담당
    //   (옛 silent drop → 이제 [Trust] 로그됨: DEBUG 전용 의도된 미세 차이, P04 박제).
    public bool RequiresSelectedClass => true;

    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        if (!DebugConfig.AllowCheats) return;  // DEBUG 내 2차 토글 — 클라 입력 무시

        C_CheatCommand pkt = new C_CheatCommand();
        pkt.Read(buffer);

        session.SubmitCheatCommand(pkt.cheatType);
    }
}
#endif
