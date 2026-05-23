using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers;

// M3.8 Phase 03 (캡스톤 1 데모 — 캐릭터 선택):
// `C_CharacterSelect` 수신 → 입력 검증 → `GameSession.SetCharacterClass` 호출.
//
// **헌법 #1 (Server Authority)**:
//   클라가 보낸 것은 `characterClass` byte(선택 의도)만.
//   서버가 PlayerStats 팩토리로 스탯 박음 — 클라가 스탯 수치를 보낼 경로 없음.
//
// **헌법 #3 (Trust Boundary) — 3단계 검증**:
//   1. 중복 선택 차단: `session.HasSelectedClass == true`이면 silent drop
//      (이미 선택한 클라가 다시 보내도 기존 stats 유지).
//   2. 범위 검증: Warrior(0)·Ranger(1) 외 값 = silent drop + [Trust] 로그.
//      현재 enum 확장 시 여기서만 업데이트 (단일 검증 지점).
//   3. 통과 시: `session.SetCharacterClass(characterClass)` 호출.
//
// **헌법 #5 (No Blocking)**: sync `void Handle(...)` 유지. async/await/Task.Delay 없음.
//   stats 박힘은 단순 필드 할당 — tick 영향 없음 (tick 루프 밖, 핸들러 직접 처리).
internal sealed class CharacterSelectHandler : IPacketHandler
{
    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        // 중복 선택 차단 (헌법 #3 step 1).
        // 이미 선택한 클라가 다시 보낸 경우 — 기존 stats 교체 X, silent drop.
        if (session.HasSelectedClass)
        {
            Console.WriteLine($"[Trust] CharacterSelect: already selected — duplicate dropped");
            return;
        }

        // 패킷 디코드.
        C_CharacterSelect pkt = new C_CharacterSelect();
        pkt.Read(buffer);

        // 범위 검증 (헌법 #3 step 2): Warrior=0, Ranger=1 외 = cheat-flag + silent drop.
        // byte 타입이라 음수 없음. 0/1 이외 2~255 = 잘못된 입력.
        if (pkt.characterClass != (byte)CharacterClass.Warrior
            && pkt.characterClass != (byte)CharacterClass.Ranger)
        {
            Console.WriteLine(
                $"[Trust] CharacterSelect: invalid characterClass=0x{pkt.characterClass:X2} — cheat-flag, dropped");
            return;
        }

        // 통과 (헌법 #3 step 3): 서버가 stats 박음 (헌법 #1).
        session.SetCharacterClass(pkt.characterClass);

        // M4.1 Phase 02 (P0-2 봉합): class 선택 완료 후 월드 진입 게이트 호출.
        // handshake + class 선택 양쪽 충족 시에만 EnterGameWorld() 실행 (idempotent).
        // 클라가 C_CharacterSelect 없이 다른 입력을 보내도 _enteredWorld=false라 월드 진입 안 됨.
        session.EnterGameWorldIfReady();
    }
}
