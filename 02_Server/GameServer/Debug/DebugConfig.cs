#if DEBUG
namespace Dawnholder.Server.GameServer.Debug;

// 시연용 디버그 치트 설정 (영호 요청 2026-06-15, M5+). 클래스 전체가 #if DEBUG —
//   Release 빌드에는 부재(치트 사슬 전체 빌드타임 봉합, 헌법 #3 / SN-02).
//
// **헌법 #3 (Trust Boundary)**: C_CheatCommand는 클라가 보내는 untrusted 입력.
//   - 1차 보장 = 빌드타임 봉합: Release는 CheatCommandHandler가 HandlerRegistry에 *미등록* →
//     C_CheatCommand가 unknown PacketID로 silent drop. 런타임 플래그에 안전을 맡기지 않음.
//   - 2차 토글 = AllowCheats: DEBUG 빌드 *내부*에서 핸들러를 끄는 런타임 스위치(시연 편의).
//     "프로덕션 시 수동으로 false" 같은 운영 의존은 폐기 — 봉합은 빌드 구성이 자동 보장.
//
// 시연 편의로 DEBUG 내 기본 ON.
//   const 아닌 static readonly — 게이트 분기(if !AllowCheats)가 const dead-code 경고(CS0162) 안 나게.
public static class DebugConfig
{
    public static readonly bool AllowCheats = true;
}
#endif
