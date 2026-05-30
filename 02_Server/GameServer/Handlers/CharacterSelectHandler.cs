using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers;

// `C_CharacterSelect` 수신 → 입력 검증 → `GameSession.SetCharacterClass` 호출.
//
// **헌법 #1 (Server Authority)**:
//   클라가 보낸 것은 `characterClass` byte(선택 의도)만.
//   서버가 PlayerStats 팩토리로 스탯 박음 — 클라가 스탯 수치를 보낼 경로 없음.
//
// **헌법 #3 (Trust Boundary)**: 중복 선택 차단 + 범위 검증(Warrior/Ranger 외 silent drop).
internal sealed class CharacterSelectHandler : IPacketHandler
{
    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        // 중복 선택 차단 (헌법 #3) — 기존 stats 교체 X, silent drop.
        if (session.HasSelectedClass)
        {
            Console.WriteLine($"[Trust] CharacterSelect: already selected — duplicate dropped");
            return;
        }

        C_CharacterSelect pkt = new C_CharacterSelect();
        pkt.Read(buffer);

        // 범위 검증 (헌법 #3): Warrior=0, Ranger=1 외 = cheat-flag + silent drop.
        if (pkt.characterClass != (byte)CharacterClass.Warrior
            && pkt.characterClass != (byte)CharacterClass.Ranger)
        {
            Console.WriteLine(
                $"[Trust] CharacterSelect: invalid characterClass=0x{pkt.characterClass:X2} — cheat-flag, dropped");
            return;
        }

        // 통과: 서버가 stats 박음 (헌법 #1).
        session.SetCharacterClass(pkt.characterClass);

        // handshake + class 선택 양쪽 충족 시에만 EnterGameWorld() 실행 (idempotent).
        // 클라가 C_CharacterSelect 없이 다른 입력을 보내도 _enteredWorld=false라 월드 진입 안 됨.
        session.EnterGameWorldIfReady();
    }
}
