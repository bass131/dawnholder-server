using Dawnholder.Server.GameServer.Debug;
using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers;

// C_CheatCommand 핸들러 (시연용 디버그). DebugConfig.AllowCheats 게이트 + decode + session 캡슐 호출.
//
// **헌법 #3 (Trust Boundary)**:
//   - AllowCheats=false(프로덕션)면 무시 — 빌드 클라가 F8 눌러도 서버가 처리 안 함.
//   - 행위자=session._entityId 강제(SubmitCheatCommand 안) — 패킷에 대상 필드 없음, 도용 차단.
//   - class 선택(월드 진입) 전 입력은 silent drop.
internal sealed class CheatCommandHandler : IPacketHandler
{
    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        if (!DebugConfig.AllowCheats) return;  // 프로덕션 게이트 — 클라 입력 무시

        if (!session.HasSelectedClass) return;  // 월드 진입 전 — silent drop

        C_CheatCommand pkt = new C_CheatCommand();
        pkt.Read(buffer);

        session.SubmitCheatCommand(pkt.cheatType);
    }
}
